using O2DESNet;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public class RCQsModel : Sandbox<IRCQsModelStatics>, IRCQsModel
    {
        public class Statics : IRCQsModelStatics
        {
            public string Id { get; }
            public IReadOnlyList<IResource> Resources { get; private set; }
            /// <summary>
            /// List of Activities to be traced
            /// </summary>
            public IReadOnlyList<IActivity> Activities { get; private set; }
            public Statics(IEnumerable<IResource> resources, IEnumerable<IActivity> activities = null)
            {
                Id = "RCQsModel#{0:N}" + Guid.NewGuid();
                Resources = resources.ToList().AsReadOnly();
                if (activities == null) Activities = new List<IActivity>().AsReadOnly();
                else Activities = activities.ToList().AsReadOnly();
            }

            public RCQsModel GetSandbox(int seed = 0) { return new RCQsModel(this, seed); }
        }

        #region Dynamic Properties  

        #region by Load/Batch

        public IReadOnlyList<ILoad> AllLoads { get { return AllLoads_Set.ToList().AsReadOnly(); } }
        private readonly HashSet<ILoad> AllLoads_Set = new HashSet<ILoad>();

        public IReadOnlyList<ILoad> Loads_PendingToEnter { get { return Loads_PendingToEnter_Set.ToList().AsReadOnly(); } }
        private readonly HashSet<ILoad> Loads_PendingToEnter_Set = new HashSet<ILoad>();

        public IReadOnlyList<ILoad> Loads_ReadyToExit { get { return Loads_ReadyToExit_Set.ToList().AsReadOnly(); } }
        private readonly HashSet<ILoad> Loads_ReadyToExit_Set = new HashSet<ILoad>();

        public IReadOnlyDictionary<ILoad, IBatch> Load_Batch_Current { get { return Load_Batch_Current_Dict.AsReadOnly(b => (IBatch)b); } }
        private readonly Dictionary<ILoad, Batch> Load_Batch_Current_Dict = new Dictionary<ILoad, Batch>();

        public IReadOnlyDictionary<ILoad, IBatch> Load_Batch_MovingTo { get { return Load_Batch_MovingTo_Dict.AsReadOnly(); } }
        private readonly Dictionary<ILoad, IBatch> Load_Batch_MovingTo_Dict = new Dictionary<ILoad, IBatch>();

        public IReadOnlyDictionary<IBatch, ReadOnlyAllocation> Batch_Allocation
        {
            get { return Batch_Allocation_Dict.AsReadOnly(a => a.AsReadOnly()); }
        }
        private readonly Dictionary<IBatch, Allocation> Batch_Allocation_Dict = new Dictionary<IBatch, Allocation>();

        /// <summary>
        /// The stage indices which is strictly increasing for each successful move, 
        /// for all loads in the system.
        /// </summary>
        private readonly Dictionary<ILoad, int> StageIndices = new Dictionary<ILoad, int>();
        #endregion

        #region by Activities
        public IReadOnlyList<IActivity> AllActivities { get { return AllActivities_Set.AsReadOnly(); } }
        private readonly HashSet<IActivity> AllActivities_Set = new HashSet<IActivity>();
        private readonly HashSet<IActivity> ActivitiesToTrace = new HashSet<IActivity>();

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> Activity_Loads { get { return Act_Loads_Dict.AsReadOnly(); } }        
        private readonly Dictionary<IActivity, HashSet<ILoad>> Act_Loads_Dict;
        
        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> Activity_Loads_Pending
        {
            get
            {
                return Act_Loads_Pending_Dict.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> Act_Loads_Pending_Dict;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> Activity_Loads_Active {
            get
            {
                return Act_Loads_Active_Dict.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> Act_Loads_Active_Dict;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> Activity_Loads_Passive {
            get
            {
                return Act_Loads_Passive_Dict.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> Act_Loads_Passive_Dict;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<(IBatch Batch, DateTime Time)>> Activity_BatchTimes_Pending
        {
            get { return Act_BatchTimes_Pending_Dict.AsReadOnly(l => l.AsReadOnly(t => ((IBatch)t.Batch, t.Time))); }
        }
        private readonly Dictionary<IActivity, List<(Batch Batch, DateTime Time)>> Act_BatchTimes_Pending_Dict;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<IBatch>> Activity_Batches_Batching
        {
            get { return Act_Batches_Batching_Dict.AsReadOnly(l => l.AsReadOnly(b => (IBatch)b)); }
        }
        private readonly Dictionary<IActivity, List<Batch>> Act_Batches_Batching_Dict;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<IResource>> Activity_Resources
        {
            get { return Act_Resources_Dict.AsReadOnly(); }
        }
        private readonly Dictionary<IActivity, List<IResource>> Act_Resources_Dict;                
        #endregion

        #region by Resources

        public IReadOnlyDictionary<IResource, IReadOnlyList<IActivity>> Resource_Activities
        {
            get { return Res_Activities_Dict.AsReadOnly(); }
        }
        private readonly Dictionary<IResource, HashSet<IActivity>> Res_Activities_Dict;

        public IReadOnlyDictionary<IResource, double> Resource_Quantity_Occupied
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => Resource_HC_Occupied[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> Resource_Quantity_Available
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => Resource_HC_Available[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> Resource_Quantity_DynamicCapacity
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => Resource_HC_DynamicCapacity[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> Resource_Quantity_PendingLock
        {
            get { return Resource_Quantity_PendingLock_Dict.AsReadOnly(); }
        }
        private readonly Dictionary<IResource, double> Resource_Quantity_PendingLock_Dict;
        #endregion

        #region Statistics
        public int CountLoads_Entered { get; private set; } = 0;
        public int CountLoads_Processing
        {
            /// Stage index = -1 implies called but not entered
            get { return AllLoads_Set.Count(load => StageIndices[load] > 0); }
        }
        public int CountLoads_Exited { get { return CountLoads_Entered - CountLoads_Processing; } }

        public IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> Activity_HC_Pending
        {
            get { return Act_HC_Pending_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IActivity, HourCounter> Act_HC_Pending_Dict;
        public IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> Activity_HC_Active
        {
            get { return Act_HC_Active_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IActivity, HourCounter> Act_HC_Active_Dict;
        public IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> Activity_HC_Passive
        {
            get { return Act_HC_Passive_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IActivity, HourCounter> Act_HC_Passive_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Pending
        {
            get { return Res_HC_Pending_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_Pending_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Active
        {
            get { return Res_HC_Active_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_Active_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Passive
        {
            get { return Res_HC_Passive_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_Passive_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Occupied
        {
            get { return Res_HC_Occupied_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_Occupied_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Available
        {
            get { return Res_HC_Available_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_Available_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_DynamicCapacity
        {
            get { return Res_HC_DynamicCapacity_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_DynamicCapacity_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_PendingLock_Active
        {
            get { return Res_HC_PendingLock_Active_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_PendingLock_Active_Dict;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_PendingLock_Passive
        {
            get { return Res_HC_PendingLock_Passive_Dict.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> Res_HC_PendingLock_Passive_Dict;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Pending
        {
            get
            {
                return Res_Activities_Dict.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Id)
                    .ToDictionary(act => act, act => Res_Act_HC_Pending_Dict[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> Res_Act_HC_Pending_Dict;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Active
        {
            get
            {
                return Res_Activities_Dict.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Id)
                    .ToDictionary(act => act, act => Res_Act_HC_Active_Dict[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> Res_Act_HC_Active_Dict;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Passive
        {
            get
            {
                return Res_Activities_Dict.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Id)
                    .ToDictionary(act => act, act => Res_Act_HC_Passive_Dict[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> Res_Act_HC_Passive_Dict;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Occupied
        {
            get
            {
                return Res_Activities_Dict.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Id)
                    .ToDictionary(act => act, act => Res_Act_HC_Occupied_Dict[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> Res_Act_HC_Occupied_Dict;
        #endregion        
        #endregion

        #region Methods / Events
        
        /// <summary>
        /// Enquiry for remaining capacity of given resource, for given load and activity
        /// </summary>
        private double RemainingCapcity(IResource res, Batch toMove)
        {
            var available = Res_HC_Available_Dict[res].LastCount;
            var remaining = available - Res_HC_Occupied_Dict[res].LastCount;
            foreach (var curr in toMove.Select(load => Load_Batch_Current_Dict[load]).Distinct())
            {
                if (curr != null && curr.IsSubsetOf(toMove) && Batch_Allocation_Dict[curr].Res_AggregatedQuantity.ContainsKey(res))
                    /// recover resources currently occupied by subset batches
                    remaining += Batch_Allocation_Dict[curr].Res_AggregatedQuantity[res];
            }
            return Math.Max(0, remaining - Resource_Quantity_PendingLock_Dict[res]);
        }

        /// <summary>
        /// Request resources for a given to-move batch
        /// </summary>
        /// <returns>Map the requirement to the list of resources and corresponding quantity to occupy</returns>
        private Allocation RequestResources(Batch toMove)
        {
            var resources = new Allocation();
            var activity = toMove.Activity;
            var staged = Act_Resources_Dict[activity].ToDictionary(res => res, res => 0d);
            foreach (var req in activity.Requirements)
            {
                var pool = Act_Resources_Dict[activity].Where(res => req.Pool.Contains(res))
                    .Select(res => (Resource: res, Qtt: RemainingCapcity(res, toMove) - staged[res]))
                    .OrderByDescending(i => i.Qtt).ToList(); /// map all available resource to its remaining capacity

                if (pool.Sum(i => i.Qtt) < req.Quantity) return null; /// no sufficient resource
                var toRqst = new List<(IResource, double)>();
                while (toRqst.Sum(i => i.Item2) < req.Quantity)
                {
                    var qtt = Math.Min(pool.First().Qtt, req.Quantity - toRqst.Sum(i => i.Item2));
                    toRqst.Add((pool.First().Resource, qtt));
                    staged[pool.First().Resource] += qtt;
                    pool.RemoveAt(0);
                }
                resources.Add(req, toRqst);
            }
            return resources;
        }

        private void AddActivityIfNotExist(IActivity act)
        {
            if (AllActivities_Set.Contains(act)) return;

            AllActivities_Set.Add(act);
            Act_Loads_Dict.Add(act, new HashSet<ILoad>());
            Act_Resources_Dict.Add(act, act.Requirements.SelectMany(req => req.Pool).Distinct().ToList());
            foreach (var res in Act_Resources_Dict[act]) Res_Activities_Dict[res].Add(act);

            Act_BatchTimes_Pending_Dict.Add(act, new List<(Batch, DateTime)>());
            Act_Batches_Batching_Dict.Add(act, new List<Batch>());
        }

        private void RemoveActivityIfNotUsed(IActivity act)
        {
            if (Act_Loads_Dict[act].Count > 0 || ActivitiesToTrace.Contains(act)) return;
            Act_Loads_Dict.Remove(act);
            AllActivities_Set.Remove(act);            

            foreach (var res in Act_Resources_Dict[act].ToList()) Res_Activities_Dict[res].Remove(act);

            Act_BatchTimes_Pending_Dict.Remove(act);
            Act_Batches_Batching_Dict.Remove(act);

            Act_Resources_Dict.Remove(act);
        }

        /// <summary>
        /// Release resource occupied by any empty current batches
        /// </summary>
        private Dictionary<IResource, double> ReleaseResources(IEnumerable<Batch> currents)
        {
            var released = new Dictionary<IResource, double>();
            foreach (var curr in currents)
            {
                if (curr.Count == 0)
                {
                    #region update statistics
                    foreach (var i in Batch_Allocation_Dict[curr].Res_AggregatedQuantity)
                    {
                        var res = i.Key;
                        var qtt = i.Value;
                        var act = curr.Activity;

                        if (!released.ContainsKey(res)) released.Add(res, 0);
                        released[res] += qtt;

                        Res_HC_Passive_Dict[res].ObserveChange(-qtt);
                        Res_HC_Occupied_Dict[res].ObserveChange(-qtt);
                        if (ActivitiesToTrace.Contains(act))
                        {
                            Res_Act_HC_Passive_Dict[res][act].ObserveChange(-qtt);
                            Res_Act_HC_Occupied_Dict[res][act].ObserveChange(-qtt);
                        }
                    }
                    #endregion

                    Batch_Allocation_Dict.Remove(curr);
                    curr.Phase = BatchPhase.Disposed;
                }
            }
            /// Lock pending-lock resource
            foreach (var res in released.Keys.ToList())
            {
                if (Resource_Quantity_PendingLock_Dict[res] > 0)
                {
                    released[res] -= AtmptLock(res);
                    if (released[res] == 0) released.Remove(res);
                }
            }
            return released;
        }

        private void UpdHourCounter_Resource_Pending(IResource res)
        {
            var qtts = Res_Activities_Dict[res].SelectMany(a => Act_BatchTimes_Pending_Dict[a])
                .Select(i => (i.Batch.Activity, i.Batch.Activity
                .Requirements.Where(req => req.Pool.Contains(res)).Sum(req => req.Quantity)))
                .GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Sum(t => t.Item2));
            
            foreach (var act in Res_Act_HC_Pending_Dict[res].Keys)                
                Res_Act_HC_Pending_Dict[res][act].ObserveCount(qtts.ContainsKey(act) ? qtts[act] : 0);
            Res_HC_Pending_Dict[res].ObserveCount(qtts.Values.Sum());
        }

        /// <summary>
        /// Attempt to move for loads pending for released resources
        /// </summary>
        /// <param name="released">Map resource to its released quantity</param>
        protected void RecallForPending(IEnumerable<IResource> released)
        {
            /// Activities that have pending load and need to be reviewed once resource has capacity
            var activities_PendingToReview = new HashSet<IActivity>();
            /// add relavent activities for review
            foreach (var a in released.SelectMany(i => Res_Activities_Dict[i]).Distinct()
                .Where(a => Act_BatchTimes_Pending_Dict[a].Count > 0)) activities_PendingToReview.Add(a);
            /// loop until the review list is clear or there is one load identified to attempt to move
            while (activities_PendingToReview.Count > 0)
            {
                /// get the earliest pending activity, and the earliest pending load
                var pendingActivity = activities_PendingToReview
                    .OrderBy(a => Act_BatchTimes_Pending_Dict[a].First().Time).First();
                var moveTo = Act_BatchTimes_Pending_Dict[pendingActivity].First().Batch;
                /// attempt to move to next activity or remove from pending
                if (moveTo.Phase == BatchPhase.Pending)
                {
                    Schedule(() => AtmptStart(moveTo));
                    activities_PendingToReview.Remove(moveTo.Activity);
                    //break;
                }
                else
                {
                    Act_BatchTimes_Pending_Dict[pendingActivity].RemoveAt(0);
                    if (Act_BatchTimes_Pending_Dict[pendingActivity].Count == 0)
                        activities_PendingToReview.Remove(pendingActivity);
                }
            }
        }

        private Batch GetMoveTo(ILoad load, IActivity next)
        {
            Batch moveTo;

            #region Add the load to the pending batch
            if (Act_BatchTimes_Pending_Dict[next].Count > 0 && Act_BatchTimes_Pending_Dict[next].Last().Batch.Count < next.BatchSizeRange.Max)
            {
                moveTo = Act_BatchTimes_Pending_Dict[next].Last().Batch;
                moveTo.Add(load);
                //if (Assets.TraceActivities)
                //    Act_HC_Pending_Dict[moveTo.Activity].ObserveChange(1, ClockTime);
            }
            #endregion

            #region Add the load to the non-pending (batching) batch
            else
            {
                if (Act_Batches_Batching_Dict[next].Count == 0 || Act_Batches_Batching_Dict[next].Last().Count >= next.BatchSizeRange.Max)
                    Act_Batches_Batching_Dict[next].Add(new Batch { Activity = next });
                moveTo = Act_Batches_Batching_Dict[next].Last();
                moveTo.Add(load);
            }
            #endregion

            return moveTo;
        }

        private void DisposeIfEmpty(Batch batch)
        {
            if (batch.Count == 0)
            {
                var released = ReleaseResources(new Batch[] { batch });                
                if (released.Count > 0) RecallForPending(released.Keys);
            }
        }
        public void RequestEnter(ILoad load, IActivity init)
        {
            Log("RqstEnter", load, init);

            AddActivityIfNotExist(init);
            Act_Loads_Dict[init].Add(load);
            AllLoads_Set.Add(load);
            Loads_PendingToEnter_Set.Add(load);
            Load_Batch_Current_Dict[load] = null;
            StageIndices.Add(load, 0);

            var moveTo = GetMoveTo(load, init);
            if (moveTo.Phase == BatchPhase.Batching && moveTo.Count >= init.BatchSizeRange.Min)
                Schedule(() => AtmptStart(moveTo));
        }
        private void Enter(ILoad load, IActivity init)
        {
            Log("Enter", load, init);            
            OnEntered.Invoke(load, init);
        }
        private void AtmptStart(Batch moveTo)
        {
            /// prevent redundant event
            if (!(moveTo.Phase == BatchPhase.Batching || moveTo.Phase == BatchPhase.Pending)) return;

            Log("AtmptStart", moveTo);

            /// Remove from batching list (to be handled by pending list)
            if (moveTo.Phase == BatchPhase.Batching) Act_Batches_Batching_Dict[moveTo.Activity].Remove(moveTo);

            var request = RequestResources(moveTo);
            #region Has sufficient requested resources   
            if (request != null)
            {
                /// Empty/partially clear the current batches due to successful start of move-to activity
                var currents = moveTo.Select(load => Load_Batch_Current_Dict[load]).Distinct()
                    .Where(curr => curr != null).ToList();
                foreach (var curr in currents) curr.RemoveWhere(load => moveTo.Contains(load));
                var released = ReleaseResources(currents);

                /// create resource occupation                        
                Batch_Allocation_Dict.Add(moveTo, request);
                foreach (var i in request.Requirement_ResourceQuantityList)
                    foreach (var (res, qtt) in i.Value)
                    {
                        if (released.ContainsKey(res))
                        {
                            released[res] -= qtt;
                            if (released[res] <= 0) released.Remove(res);
                        }
                    }
                 
                /// clear from pending list
                if (moveTo.Phase == BatchPhase.Pending)
                {
                    var idx = Act_BatchTimes_Pending_Dict[moveTo.Activity].FindIndex(t => t.Batch.Equals(moveTo));
                    if (idx < 0) throw new Exception("Could not find the batch in the pending list");
                    Act_BatchTimes_Pending_Dict[moveTo.Activity].RemoveAt(idx);
                    foreach (var res in Act_Resources_Dict[moveTo.Activity])
                        UpdHourCounter_Resource_Pending(res);
                }

                /// Update statistics   
                if (ActivitiesToTrace.Contains(moveTo.Activity))
                {
                    if (moveTo.Phase == BatchPhase.Pending)
                    {
                        Act_HC_Pending_Dict[moveTo.Activity].ObserveChange(-moveTo.Count);
                        foreach (var load in moveTo) Act_Loads_Pending_Dict[moveTo.Activity].Remove(load);
                    }
                    Act_HC_Active_Dict[moveTo.Activity].ObserveChange(moveTo.Count);
                    foreach (var load in moveTo) Act_Loads_Active_Dict[moveTo.Activity].Add(load);
                }

                foreach (var load in moveTo)
                {
                    var curr = Load_Batch_Current_Dict[load];
                    if (curr != null && ActivitiesToTrace.Contains(curr.Activity))
                    {
                        Act_HC_Passive_Dict[curr.Activity].ObserveChange(-1);
                        Act_Loads_Passive_Dict[curr.Activity].Remove(load);
                    }
                }
                
                foreach (var i in Batch_Allocation_Dict[moveTo].Res_AggregatedQuantity)
                {
                    var res = i.Key;
                    var act = moveTo.Activity;
                    var qtt = i.Value;
                    Res_HC_Active_Dict[res].ObserveChange(qtt);
                    Res_HC_Occupied_Dict[res].ObserveChange(qtt);
                    if (ActivitiesToTrace.Contains(act))
                    {
                        Res_Act_HC_Active_Dict[res][act].ObserveChange(qtt);
                        Res_Act_HC_Occupied_Dict[res][act].ObserveChange(qtt);
                    }
                }

                /// Update activity record for the load                    
                Schedule(() => Start(moveTo));
                foreach (var load in moveTo)
                {
                    if (StageIndices[load] == 0)
                    /// moved to the first activity
                    {
                        CountLoads_Entered++;
                        Loads_PendingToEnter_Set.Remove(load);
                        Schedule(() => Enter(load, moveTo.Activity));
                    }
                    else
                    /// clear from previous activity
                    {
                        var prev = Load_Batch_Current_Dict[load].Activity;
                        Act_Loads_Dict[prev].Remove(load);
                        RemoveActivityIfNotUsed(prev);
                    }
                    StageIndices[load]++;
                    Load_Batch_Current_Dict[load] = moveTo;
                    Load_Batch_MovingTo_Dict[load] = null;
                }

                moveTo.Phase = BatchPhase.Started;
                                        
                RecallForPending(released.Keys);
            }
            #endregion

            #region Has insufficient requested resources
            else
            {
                if (moveTo.Phase != BatchPhase.Pending)
                {
                    moveTo.Phase = BatchPhase.Pending;
                    Act_BatchTimes_Pending_Dict[moveTo.Activity].Add((moveTo, ClockTime));
                    foreach (var res in Act_Resources_Dict[moveTo.Activity])
                    {
                        UpdHourCounter_Resource_Pending(res);
                        if (Resource_Quantity_PendingLock_Dict[res] > 0) UpdateHourCounter_ResourcePendingLock(res);
                    }

                    /// Update statistics
                    foreach (var load in moveTo)
                    {
                        if (Load_Batch_Current_Dict[load] != null)
                            Load_Batch_Current_Dict[load].Phase = BatchPhase.Passive;
                    }
                    if (ActivitiesToTrace.Contains(moveTo.Activity))
                    {
                        Act_HC_Pending_Dict[moveTo.Activity].ObserveChange(moveTo.Count);
                        foreach (var load in moveTo) Act_Loads_Pending_Dict[moveTo.Activity].Add(load);
                    }
                }
            }
            #endregion
        }
        private void Start(IBatch batch)
        {
            Log("Start", batch);
            OnStarted.Invoke(batch);
        }
        public void Finish(IBatch batch, Dictionary<ILoad, IActivity> nexts)
        {
            if (batch.Phase != BatchPhase.Started) return; /// prevent duplicated events
            Log("Finish", batch);
            ((Batch)batch).Phase = BatchPhase.Finished;

            /// Statistics                
            if (ActivitiesToTrace.Contains(batch.Activity))
            {
                Act_HC_Active_Dict[batch.Activity].ObserveChange(-batch.Count);
                Act_HC_Passive_Dict[batch.Activity].ObserveChange(batch.Count);
                foreach (var load in batch)
                {
                    Act_Loads_Active_Dict[batch.Activity].Remove(load);
                    Act_Loads_Passive_Dict[batch.Activity].Add(load);
                }
            }
            foreach (var i in Batch_Allocation_Dict[batch].Res_AggregatedQuantity)
            {
                var res = i.Key;
                var act = batch.Activity;
                var qtt = i.Value;
                Res_HC_Active_Dict[res].ObserveChange(-qtt);                
                Res_HC_Passive_Dict[res].ObserveChange(qtt);
                if (ActivitiesToTrace.Contains(act))
                {
                    Res_Act_HC_Active_Dict[res][act].ObserveChange(-qtt);
                    Res_Act_HC_Passive_Dict[res][act].ObserveChange(qtt);
                }
                if (Resource_Quantity_PendingLock[res] > 0) UpdateHourCounter_ResourcePendingLock(res);
            }

            foreach (var load in batch.ToList())
            {
                #region Move out from the last activity & the system
                if (nexts == null || !nexts.ContainsKey(load) || nexts[load] == null)
                {
                    Load_Batch_MovingTo_Dict[load] = null;
                    Loads_ReadyToExit_Set.Add(load);
                    Schedule(() => ReadyToExit(load));
                }
                #endregion
                #region Prepare to move to the next activity
                else
                {
                    AddActivityIfNotExist(nexts[load]);
                    Act_Loads_Dict[nexts[load]].Add(load);
                    var moveTo = GetMoveTo(load, nexts[load]);
                    if (moveTo.Phase == BatchPhase.Batching && moveTo.Count >= nexts[load].BatchSizeRange.Min)
                        Schedule(() => AtmptStart(moveTo));
                }
                #endregion
            }
        }
        public void Exit(ILoad load)
        {
            Log("Exit", load);
            if (!Loads_ReadyToExit_Set.Contains(load)) return;
            Loads_ReadyToExit_Set.Remove(load);
            var curr = Load_Batch_Current_Dict[load];
            curr.Remove(load);
            AllLoads_Set.Remove(load);
            StageIndices.Remove(load);
            Load_Batch_Current_Dict.Remove(load);
            Load_Batch_MovingTo_Dict.Remove(load);
            if (ActivitiesToTrace.Contains(curr.Activity))
            {
                Act_HC_Passive_Dict[curr.Activity].ObserveChange(-1);
                Act_Loads_Passive_Dict[curr.Activity].Remove(load);
            }
            DisposeIfEmpty(curr);
            Act_Loads_Dict[curr.Activity].Remove(load);
            RemoveActivityIfNotUsed(curr.Activity);
        }
        private void ReadyToExit(ILoad load)
        {
            Log("ReadyToExit", load);
            OnReadyToExit.Invoke(load);
        }

        public void RequestLock(IResource resource, double quantity)
        {
            Log("RqstLock", resource, quantity);
            Resource_Quantity_PendingLock_Dict[resource] = Math.Min(
                Resource_Quantity_PendingLock_Dict[resource] + quantity,
                Res_HC_Available_Dict[resource].LastCount);
            Res_HC_DynamicCapacity_Dict[resource].ObserveCount(
                Math.Max(0, Res_HC_DynamicCapacity_Dict[resource].LastCount - quantity));
            AtmptLock(resource);
        }

        public void RequestUnlock(IResource resource, double quantity)
        {
            Log("Request Unlock", resource, quantity);
            var qtt = quantity;
            var removeFromPending = Math.Min(Resource_Quantity_PendingLock_Dict[resource], qtt);
            Resource_Quantity_PendingLock_Dict[resource] -= removeFromPending;
            qtt -= removeFromPending;
            Res_HC_Available_Dict[resource].ObserveChange(qtt);
            Res_HC_DynamicCapacity_Dict[resource].ObserveCount(
                Res_HC_DynamicCapacity_Dict[resource].LastCount + quantity);
            UpdateHourCounter_ResourcePendingLock(resource);
            if (qtt > 0)
            {
                Log("Unlocked", resource, qtt);
                RecallForPending(new List<IResource> { resource });                
                OnUnlocked.Invoke(resource, qtt);
            }
        }

        /// <summary>
        /// Atempt to lock resource for the pending quantity
        /// </summary>
        private double AtmptLock(IResource resource)
        {
            Log("AtmptLock", resource);
            var quantity = Math.Min(
                Res_HC_Available_Dict[resource].LastCount - Res_HC_Occupied_Dict[resource].LastCount,
                Resource_Quantity_PendingLock_Dict[resource]);
            Res_HC_Available_Dict[resource].ObserveChange(-quantity);
            Resource_Quantity_PendingLock_Dict[resource] -= quantity;
            UpdateHourCounter_ResourcePendingLock(resource);
            if (quantity > 0)
            {
                Log("Locked", resource, quantity);
                OnLocked.Invoke(resource, quantity);
            }
            return quantity;
        }

        private void UpdateHourCounter_ResourcePendingLock(IResource resource)
        {
            var pendingLockActive = Math.Min(
                Resource_Quantity_PendingLock_Dict[resource],
                Res_HC_Active_Dict[resource].LastCount);
            /// Remark: the active occupied resource has higher priority to be counted as it is more likely to be released and locked
            var pendingLockPassive = Resource_Quantity_PendingLock_Dict[resource] - pendingLockActive;

            Res_HC_PendingLock_Active_Dict[resource].ObserveCount(pendingLockActive);
            Res_HC_PendingLock_Passive_Dict[resource].ObserveCount(pendingLockPassive);
        }
        #endregion

        #region Output Events - Reference to Getters
        public event Action<ILoad, IActivity> OnEntered = (load, activity) => { };
        public event Action<ILoad> OnReadyToExit = load => { };
        public event Action<IResource, double> OnLocked = (resource, quantity) => { };
        public event Action<IResource, double> OnUnlocked = (resource, quantity) => { };
        public event Action<IBatch> OnStarted = batch => { };
        #endregion

        public RCQsModel(IRCQsModelStatics assets, int seed, string id = null) : base(assets, seed, id)
        {
            Res_Activities_Dict = Assets.Resources.ToDictionary(r => r, r => new HashSet<IActivity>());
            Resource_Quantity_PendingLock_Dict = Assets.Resources.ToDictionary(res => res, res => 0d);

            Act_Resources_Dict = new Dictionary<IActivity, List<IResource>>();            
            Act_Loads_Dict = new Dictionary<IActivity, HashSet<ILoad>>();
            Act_Loads_Pending_Dict = new Dictionary<IActivity, HashSet<ILoad>>();
            Act_Loads_Active_Dict = new Dictionary<IActivity, HashSet<ILoad>>();
            Act_Loads_Passive_Dict = new Dictionary<IActivity, HashSet<ILoad>>();
            Act_BatchTimes_Pending_Dict = new Dictionary<IActivity, List<(Batch Batch, DateTime Time)>>();
            Act_Batches_Batching_Dict = new Dictionary<IActivity, List<Batch>>();            

            Act_HC_Active_Dict = new Dictionary<IActivity, HourCounter>();
            Act_HC_Passive_Dict = new Dictionary<IActivity, HourCounter>();
            Act_HC_Pending_Dict = new Dictionary<IActivity, HourCounter>();
            Res_HC_Active_Dict = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            Res_HC_Available_Dict = Assets.Resources.ToDictionary(r => r, r =>
            {
                var hc = AddHourCounter();
                hc.ObserveCount(r.Capacity);
                return hc;
            });
            Res_HC_DynamicCapacity_Dict = Assets.Resources.ToDictionary(r => r, r =>
            {
                var hc = AddHourCounter();
                hc.ObserveCount(r.Capacity);
                return hc;
            });
            Res_HC_Occupied_Dict = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            Res_HC_Passive_Dict = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            Res_HC_Pending_Dict = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            Res_HC_PendingLock_Active_Dict = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            Res_HC_PendingLock_Passive_Dict = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            Res_Act_HC_Pending_Dict = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            Res_Act_HC_Active_Dict = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            Res_Act_HC_Passive_Dict = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            Res_Act_HC_Occupied_Dict = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());

            foreach (var act in Assets.Activities)
            {
                ActivitiesToTrace.Add(act);
                foreach (var res in Assets.Resources)
                {
                    Res_Act_HC_Pending_Dict[res].Add(act, AddHourCounter());
                    Res_Act_HC_Active_Dict[res].Add(act, AddHourCounter());
                    Res_Act_HC_Passive_Dict[res].Add(act, AddHourCounter());
                    Res_Act_HC_Occupied_Dict[res].Add(act, AddHourCounter());
                }
                Act_HC_Pending_Dict.Add(act, AddHourCounter(keepHistory: true));
                Act_HC_Active_Dict.Add(act, AddHourCounter(keepHistory: true));
                Act_HC_Passive_Dict.Add(act, AddHourCounter(keepHistory: true));
                Act_Loads_Pending_Dict.Add(act, new HashSet<ILoad>());
                Act_Loads_Active_Dict.Add(act, new HashSet<ILoad>());
                Act_Loads_Passive_Dict.Add(act, new HashSet<ILoad>());
            }
        }

        public string Snapshot()
        {
            string str = "[Loads]\n";
            foreach (var load in AllLoads_Set.OrderBy(l => l.Index))
            {
                str += string.Format("Id: {0}\tAct_Id: {1}\t", load, Load_Batch_Current_Dict[load] == null ? "null" : Load_Batch_Current_Dict[load].Activity.Id);
                if (Load_Batch_Current_Dict[load] != null && Batch_Allocation_Dict.ContainsKey(Load_Batch_Current_Dict[load]))
                    foreach (var (res, qtt) in Batch_Allocation_Dict[Load_Batch_Current_Dict[load]].Requirement_ResourceQuantityList.SelectMany(i => i.Value))
                        str += string.Format("Res#{0}({1}) ", res.Id, qtt);
                str += "\n";
            }

            str += "[Resources]\n";
            foreach (var res in Assets.Resources)
            {
                str += string.Format("Id: {0}\tOccupied: ", res.Id);
                foreach (var current in Load_Batch_Current_Dict.Values.Distinct().Where(curr => curr != null && 
                    Batch_Allocation_Dict[curr].Res_AggregatedQuantity.ContainsKey(res) && Batch_Allocation_Dict[curr].Res_AggregatedQuantity[res] > 0))
                    str += string.Format("{0} ", current);
                str += string.Format("\tPending:");
                foreach (var toMove in Res_Activities_Dict[res].SelectMany(a => Act_BatchTimes_Pending_Dict[a].Select(i => i.Batch)))
                    str += string.Format("{0} ", toMove);
                str += "\n";
            }

            return str;
        }

        protected override void WarmedUpHandler()
        {
            CountLoads_Entered = 0;
        }

        public override void Dispose()
        {
            foreach (Action<ILoad, IActivity> i in OnEntered.GetInvocationList()) OnEntered -= i;
            foreach (Action<ILoad> i in OnReadyToExit.GetInvocationList()) OnReadyToExit -= i;
            foreach (Action<IResource, double> i in OnLocked.GetInvocationList()) OnLocked -= i;
            foreach (Action<IResource, double> i in OnUnlocked.GetInvocationList()) OnUnlocked -= i;
            foreach (Action<IBatch> i in OnStarted.GetInvocationList()) OnStarted -= i;
        }
    }
}