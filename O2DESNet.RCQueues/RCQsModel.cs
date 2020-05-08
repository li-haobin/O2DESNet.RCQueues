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

        public IReadOnlyList<ILoad> AllLoads { get { return _allLoads.ToList().AsReadOnly(); } }
        private readonly HashSet<ILoad> _allLoads = new HashSet<ILoad>();

        public IReadOnlyList<ILoad> Loads_PendingToEnter { get { return _loads_PendingToEnter.ToList().AsReadOnly(); } }
        private readonly HashSet<ILoad> _loads_PendingToEnter = new HashSet<ILoad>();

        public IReadOnlyList<ILoad> Loads_ReadyToExit { get { return _loads_ReadyToExit.ToList().AsReadOnly(); } }
        private readonly HashSet<ILoad> _loads_ReadyToExit = new HashSet<ILoad>();

        public IReadOnlyDictionary<ILoad, IBatch> LoadToBatch_Current { get { return _loadToBatch_Current.AsReadOnly(b => (IBatch)b); } }
        private readonly Dictionary<ILoad, Batch> _loadToBatch_Current = new Dictionary<ILoad, Batch>();

        public IReadOnlyDictionary<ILoad, IBatch> LoadToBatch_MovingTo { get { return _loadToBatch_MovingTo.AsReadOnly(); } }
        private readonly Dictionary<ILoad, IBatch> _loadToBatch_MovingTo = new Dictionary<ILoad, IBatch>();

        public IReadOnlyDictionary<IBatch, ReadOnlyAllocation> BatchToAllocation
        {
            get { return _batchToAllocation.AsReadOnly(a => a.AsReadOnly()); }
        }
        private readonly Dictionary<IBatch, Allocation> _batchToAllocation = new Dictionary<IBatch, Allocation>();

        /// <summary>
        /// The stage indices which is strictly increasing for each successful move, 
        /// for all loads in the system.
        /// </summary>
        private readonly Dictionary<ILoad, int> StageIndices = new Dictionary<ILoad, int>();
        #endregion

        #region by Activities
        public IReadOnlyList<IActivity> AllActivities { get { return _allActivities.AsReadOnly(); } }
        private readonly HashSet<IActivity> _allActivities = new HashSet<IActivity>();
        private readonly HashSet<IActivity> _activitiesToTrace = new HashSet<IActivity>();

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoads { get { return _activityToLoads.AsReadOnly(); } }        
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoads;
        
        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoads_Pending
        {
            get
            {
                return _activityToLoads_Pending.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoads_Pending;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoads_Active {
            get
            {
                return _activityToLoads_Active.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoads_Active;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoads_Passive {
            get
            {
                return _activityToLoads_Passive.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoads_Passive;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<(IBatch Batch, DateTime Time)>> ActivityToBatchTimes_Pending
        {
            get { return _activityToBatchTimes_Pending.AsReadOnly(l => l.AsReadOnly(t => ((IBatch)t.Batch, t.Time))); }
        }
        private readonly Dictionary<IActivity, List<(Batch Batch, DateTime Time)>> _activityToBatchTimes_Pending;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<IBatch>> ActivityToBatches_Batching
        {
            get { return _activityToBatches_Batching.AsReadOnly(l => l.AsReadOnly(b => (IBatch)b)); }
        }
        private readonly Dictionary<IActivity, List<Batch>> _activityToBatches_Batching;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<IResource>> ActivityToResources
        {
            get { return _activityToResources.AsReadOnly(); }
        }
        private readonly Dictionary<IActivity, List<IResource>> _activityToResources;                
        #endregion

        #region by Resources

        public IReadOnlyDictionary<IResource, IReadOnlyList<IActivity>> ResourceToActivities
        {
            get { return _resourceToActivities.AsReadOnly(); }
        }
        private readonly Dictionary<IResource, HashSet<IActivity>> _resourceToActivities;

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IBatch, double>> ResourceBatchQuantity_Active
        {
            get
            {
                return _resourceBatchQuantity_Active.ToDictionary(i => i.Key, i => i.Value.AsReadOnly()).AsReadOnly();
            }
        }
        private readonly Dictionary<IResource, Dictionary<IBatch, double>> _resourceBatchQuantity_Active;

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IBatch, double>> ResourceBatchQuantity_Passive
        {
            get
            {
                return _resourceBatchQuantity_Passive.ToDictionary(i => i.Key, i => i.Value.AsReadOnly()).AsReadOnly();
            }
        }
        private readonly Dictionary<IResource, Dictionary<IBatch, double>> _resourceBatchQuantity_Passive;

        public IReadOnlyDictionary<IResource, double> ResourceQuantity_Occupied
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => ResourceHC_Occupied[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> ResourceQuantity_Available
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => ResourceHC_Available[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> ResourceQuantity_DynamicCapacity
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => ResourceHC_DynamicCapacity[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> ResourceQuantity_PendingLock
        {
            get { return _resourceQuantity_PendingLock.AsReadOnly(); }
        }
        private readonly Dictionary<IResource, double> _resourceQuantity_PendingLock;
        #endregion

        #region Statistics
        public int CountOfLoads_Entered { get; private set; } = 0;
        public int CountOfLoads_Processing
        {
            /// Stage index = -1 implies called but not entered
            get { return _allLoads.Count(load => StageIndices[load] > 0); }
        }
        public int CountOfLoads_Exited { get { return CountOfLoads_Entered - CountOfLoads_Processing; } }

        public IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHC_Pending
        {
            get { return _activityHC_Pending.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IActivity, HourCounter> _activityHC_Pending;
        public IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHC_Active
        {
            get { return _activityHC_Active.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IActivity, HourCounter> _activityHC_Active;
        public IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHC_Passive
        {
            get { return _activityHC_Passive.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IActivity, HourCounter> _activityHC_Passive;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_Pending
        {
            get { return _resourceHc_Pending.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHc_Pending;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_Active
        {
            get { return _resourceHC_Active.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHC_Active;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_Passive
        {
            get { return _resourceHC_Passive.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHC_Passive;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_Occupied
        {
            get { return _resourceHC_Occupied.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHC_Occupied;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_Available
        {
            get { return _resourceHC_Available.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHC_Available;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_DynamicCapacity
        {
            get { return _resourceHC_DynamicCapacity.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHC_DynamicCapacity;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_PendingLock_Active
        {
            get { return _resourceHC_PendingLock_Active.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHC_PendingLock_Active;
        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHC_PendingLock_Passive
        {
            get { return _resourceHC_PendingLock_Passive.AsReadOnly(hc => hc.AsReadOnly()); }
        }
        private readonly Dictionary<IResource, HourCounter> _resourceHC_PendingLock_Passive;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHC_Pending
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHC_Pending[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHC_Pending;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHC_Active
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHC_Active[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHC_Active;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHC_Passive
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHC_Passive[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHC_Passive;
        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHC_Occupied
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHC_Occupied[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHC_Occupied;
        #endregion        
        #endregion

        #region Methods / Events
        
        /// <summary>
        /// Enquiry for remaining capacity of given resource, for given load and activity
        /// </summary>
        private double RemainingCapcity(IResource res, Batch toMove)
        {
            var available = _resourceHC_Available[res].LastCount;
            var remaining = available - _resourceHC_Occupied[res].LastCount;
            foreach (var curr in toMove.Select(load => _loadToBatch_Current[load]).Distinct())
            {
                if (curr != null && curr.IsSubsetOf(toMove) && _batchToAllocation[curr].ResourceQuantity_Aggregated.ContainsKey(res))
                    /// recover resources currently occupied by subset batches
                    remaining += _batchToAllocation[curr].ResourceQuantity_Aggregated[res];
            }
            return Math.Max(0, remaining - _resourceQuantity_PendingLock[res]);
        }

        /// <summary>
        /// Request resources for a given to-move batch
        /// </summary>
        /// <returns>Map the requirement to the list of resources and corresponding quantity to occupy</returns>
        private Allocation RequestResources(Batch toMove)
        {
            var resources = new Allocation();
            var activity = toMove.Activity;
            var staged = _activityToResources[activity].ToDictionary(res => res, res => 0d);
            foreach (var req in activity.Requirements)
            {
                var pool = _activityToResources[activity].Where(res => req.Pool.Contains(res))
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
            if (_allActivities.Contains(act)) return;

            _allActivities.Add(act);
            _activityToLoads.Add(act, new HashSet<ILoad>());
            _activityToResources.Add(act, act.Requirements.SelectMany(req => req.Pool).Distinct().ToList());
            foreach (var res in _activityToResources[act]) _resourceToActivities[res].Add(act);

            _activityToBatchTimes_Pending.Add(act, new List<(Batch, DateTime)>());
            _activityToBatches_Batching.Add(act, new List<Batch>());
        }

        private void RemoveActivityIfNotUsed(IActivity act)
        {
            if (_activityToLoads[act].Count > 0 || _activitiesToTrace.Contains(act)) return;
            _activityToLoads.Remove(act);
            _allActivities.Remove(act);            

            foreach (var res in _activityToResources[act].ToList()) _resourceToActivities[res].Remove(act);

            _activityToBatchTimes_Pending.Remove(act);
            _activityToBatches_Batching.Remove(act);

            _activityToResources.Remove(act);
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
                    foreach (var i in _batchToAllocation[curr].ResourceQuantity_Aggregated)
                    {
                        var res = i.Key;
                        var qtt = i.Value;
                        var act = curr.Activity;

                        if (!released.ContainsKey(res)) released.Add(res, 0);
                        released[res] += qtt;

                        _resourceBatchQuantity_Passive[res].Remove(curr);
                        _resourceHC_Passive[res].ObserveChange(-qtt);
                        _resourceHC_Occupied[res].ObserveChange(-qtt);
                        if (_activitiesToTrace.Contains(act))
                        {
                            _resourceActivityHC_Passive[res][act].ObserveChange(-qtt);
                            _resourceActivityHC_Occupied[res][act].ObserveChange(-qtt);
                        }
                    }
                    #endregion

                    _batchToAllocation.Remove(curr);
                    curr.Phase = BatchPhase.Disposed;
                }
            }
            /// Lock pending-lock resource
            foreach (var res in released.Keys.ToList())
            {
                if (_resourceQuantity_PendingLock[res] > 0)
                {
                    released[res] -= AttemptToLock(res);
                    if (released[res] == 0) released.Remove(res);
                }
            }
            return released;
        }

        private void UpdHourCounter_Resource_Pending(IResource res)
        {
            var qtts = _resourceToActivities[res].SelectMany(a => _activityToBatchTimes_Pending[a])
                .Select(i => (i.Batch.Activity, i.Batch.Activity
                .Requirements.Where(req => req.Pool.Contains(res)).Sum(req => req.Quantity)))
                .GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Sum(t => t.Item2));
            
            foreach (var act in _resourceActivityHC_Pending[res].Keys)                
                _resourceActivityHC_Pending[res][act].ObserveCount(qtts.ContainsKey(act) ? qtts[act] : 0);
            _resourceHc_Pending[res].ObserveCount(qtts.Values.Sum());
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
            foreach (var a in released.SelectMany(i => _resourceToActivities[i]).Distinct()
                .Where(a => _activityToBatchTimes_Pending[a].Count > 0)) activities_PendingToReview.Add(a);
            /// loop until the review list is clear or there is one load identified to attempt to move
            while (activities_PendingToReview.Count > 0)
            {
                /// get the earliest pending activity, and the earliest pending load
                var pendingActivity = activities_PendingToReview
                    .OrderBy(a => _activityToBatchTimes_Pending[a].First().Time).First();
                var moveTo = _activityToBatchTimes_Pending[pendingActivity].First().Batch;
                /// attempt to move to next activity or remove from pending
                if (moveTo.Phase == BatchPhase.Pending)
                {
                    Schedule(() => AttemptToStart(moveTo));
                    activities_PendingToReview.Remove(moveTo.Activity);
                    //break;
                }
                else
                {
                    _activityToBatchTimes_Pending[pendingActivity].RemoveAt(0);
                    if (_activityToBatchTimes_Pending[pendingActivity].Count == 0)
                        activities_PendingToReview.Remove(pendingActivity);
                }
            }
        }

        private Batch GetMoveTo(ILoad load, IActivity next)
        {
            Batch moveTo;

            #region Add the load to the pending batch
            if (_activityToBatchTimes_Pending[next].Count > 0 && _activityToBatchTimes_Pending[next].Last().Batch.Count < next.BatchSizeRange.Max)
            {
                moveTo = _activityToBatchTimes_Pending[next].Last().Batch;
                moveTo.Add(load);
                //if (Assets.TraceActivities)
                //    Act_HC_Pending_Dict[moveTo.Activity].ObserveChange(1, ClockTime);
            }
            #endregion

            #region Add the load to the non-pending (batching) batch
            else
            {
                if (_activityToBatches_Batching[next].Count == 0 || _activityToBatches_Batching[next].Last().Count >= next.BatchSizeRange.Max)
                    _activityToBatches_Batching[next].Add(new Batch { Activity = next });
                moveTo = _activityToBatches_Batching[next].Last();
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
        public void RequestToEnter(ILoad load, IActivity init)
        {
            Log("RqstEnter", load, init);

            AddActivityIfNotExist(init);
            _activityToLoads[init].Add(load);
            _allLoads.Add(load);
            _loads_PendingToEnter.Add(load);
            _loadToBatch_Current[load] = null;
            StageIndices.Add(load, 0);

            var moveTo = GetMoveTo(load, init);
            if (moveTo.Phase == BatchPhase.Batching && moveTo.Count >= init.BatchSizeRange.Min)
                Schedule(() => AttemptToStart(moveTo));
        }
        private void Enter(ILoad load, IActivity init)
        {
            Log("Enter", load, init);            
            OnEntered.Invoke(load, init);
        }
        private void AttemptToStart(Batch moveTo)
        {
            /// prevent redundant event
            if (!(moveTo.Phase == BatchPhase.Batching || moveTo.Phase == BatchPhase.Pending)) return;

            Log("AtmptStart", moveTo);

            /// Remove from batching list (to be handled by pending list)
            if (moveTo.Phase == BatchPhase.Batching) _activityToBatches_Batching[moveTo.Activity].Remove(moveTo);

            var request = RequestResources(moveTo);
            #region Has sufficient requested resources   
            if (request != null)
            {
                /// Empty/partially clear the current batches due to successful start of move-to activity
                var currents = moveTo.Select(load => _loadToBatch_Current[load]).Distinct()
                    .Where(curr => curr != null).ToList();
                foreach (var curr in currents) curr.RemoveWhere(load => moveTo.Contains(load));
                var released = ReleaseResources(currents);

                /// create resource occupation                        
                _batchToAllocation.Add(moveTo, request);
                foreach (var i in request.Requirement_ResourceQuantityList)
                {
                    foreach (var (res, qtt) in i.Value)
                    {
                        if (released.ContainsKey(res))
                        {
                            released[res] -= qtt;
                            if (released[res] <= 0) released.Remove(res);
                        }
                    }
                }
                 
                /// clear from pending list
                if (moveTo.Phase == BatchPhase.Pending)
                {
                    var idx = _activityToBatchTimes_Pending[moveTo.Activity].FindIndex(t => t.Batch.Equals(moveTo));
                    if (idx < 0) throw new Exception("Could not find the batch in the pending list");
                    _activityToBatchTimes_Pending[moveTo.Activity].RemoveAt(idx);
                    foreach (var res in _activityToResources[moveTo.Activity])
                        UpdHourCounter_Resource_Pending(res);
                }

                /// Update statistics   
                if (_activitiesToTrace.Contains(moveTo.Activity))
                {
                    if (moveTo.Phase == BatchPhase.Pending)
                    {
                        _activityHC_Pending[moveTo.Activity].ObserveChange(-moveTo.Count);
                        foreach (var load in moveTo) _activityToLoads_Pending[moveTo.Activity].Remove(load);
                    }
                    _activityHC_Active[moveTo.Activity].ObserveChange(moveTo.Count);
                    foreach (var load in moveTo) _activityToLoads_Active[moveTo.Activity].Add(load);
                }

                foreach (var load in moveTo)
                {
                    var curr = _loadToBatch_Current[load];
                    if (curr != null && _activitiesToTrace.Contains(curr.Activity))
                    {
                        _activityHC_Passive[curr.Activity].ObserveChange(-1);
                        _activityToLoads_Passive[curr.Activity].Remove(load);
                    }
                }

                foreach (var i in _batchToAllocation[moveTo].ResourceQuantity_Aggregated)
                {
                    var res = i.Key;
                    var act = moveTo.Activity;
                    var qtt = i.Value;
                    _resourceBatchQuantity_Active[res].Add(moveTo, qtt);
                    _resourceHC_Active[res].ObserveChange(qtt);
                    _resourceHC_Occupied[res].ObserveChange(qtt);
                    if (_activitiesToTrace.Contains(act))
                    {
                        _resourceActivityHC_Active[res][act].ObserveChange(qtt);
                        _resourceActivityHC_Occupied[res][act].ObserveChange(qtt);
                    }
                }

                /// Update activity record for the load                    
                Schedule(() => Start(moveTo));
                foreach (var load in moveTo)
                {
                    if (StageIndices[load] == 0)
                    /// moved to the first activity
                    {
                        CountOfLoads_Entered++;
                        _loads_PendingToEnter.Remove(load);
                        Schedule(() => Enter(load, moveTo.Activity));
                    }
                    else
                    /// clear from previous activity
                    {
                        var prev = _loadToBatch_Current[load].Activity;
                        _activityToLoads[prev].Remove(load);
                        RemoveActivityIfNotUsed(prev);
                    }
                    StageIndices[load]++;
                    _loadToBatch_Current[load] = moveTo;
                    _loadToBatch_MovingTo[load] = null;
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

                    var clockTime = ClockTime;
                    if (moveTo.Count(l => _loadToBatch_Current[l] != null && _loadToBatch_Current[l].Activity == moveTo.Activity) > 0)
                        clockTime = DateTime.MinValue; /// consider the case of immediate re-work having highest priority, to avoid deadlock
                    _activityToBatchTimes_Pending[moveTo.Activity].Add((moveTo, clockTime));
                    _activityToBatchTimes_Pending[moveTo.Activity].Sort((t1, t2) => t1.Time.CompareTo(t2.Time));

                    foreach (var res in _activityToResources[moveTo.Activity])
                    {
                        UpdHourCounter_Resource_Pending(res);
                        if (_resourceQuantity_PendingLock[res] > 0) UpdateHourCounter_ResourcePendingLock(res);
                    }

                    /// Update statistics
                    foreach (var load in moveTo)
                    {
                        if (_loadToBatch_Current[load] != null)
                            _loadToBatch_Current[load].Phase = BatchPhase.Passive;
                    }
                    if (_activitiesToTrace.Contains(moveTo.Activity))
                    {
                        _activityHC_Pending[moveTo.Activity].ObserveChange(moveTo.Count);
                        foreach (var load in moveTo) _activityToLoads_Pending[moveTo.Activity].Add(load);
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
            if (_activitiesToTrace.Contains(batch.Activity))
            {
                _activityHC_Active[batch.Activity].ObserveChange(-batch.Count);
                _activityHC_Passive[batch.Activity].ObserveChange(batch.Count);
                foreach (var load in batch)
                {
                    _activityToLoads_Active[batch.Activity].Remove(load);
                    _activityToLoads_Passive[batch.Activity].Add(load);
                }
            }
            foreach (var i in _batchToAllocation[batch].ResourceQuantity_Aggregated)
            {
                var res = i.Key;
                var act = batch.Activity;
                var qtt = i.Value;
                _resourceBatchQuantity_Active[res].Remove(batch);
                _resourceBatchQuantity_Passive[res].Add(batch, qtt);
                _resourceHC_Active[res].ObserveChange(-qtt);                
                _resourceHC_Passive[res].ObserveChange(qtt);
                if (_activitiesToTrace.Contains(act))
                {
                    _resourceActivityHC_Active[res][act].ObserveChange(-qtt);
                    _resourceActivityHC_Passive[res][act].ObserveChange(qtt);
                }
                if (ResourceQuantity_PendingLock[res] > 0) UpdateHourCounter_ResourcePendingLock(res);
            }

            foreach (var load in batch.ToList())
            {
                #region Move out from the last activity & the system
                if (nexts == null || !nexts.ContainsKey(load) || nexts[load] == null)
                {
                    _loadToBatch_MovingTo[load] = null;
                    _loads_ReadyToExit.Add(load);
                    Schedule(() => ReadyToExit(load));
                }
                #endregion
                #region Prepare to move to the next activity
                else
                {
                    AddActivityIfNotExist(nexts[load]);
                    _activityToLoads[nexts[load]].Add(load);
                    var moveTo = GetMoveTo(load, nexts[load]);
                    if (moveTo.Phase == BatchPhase.Batching && moveTo.Count >= nexts[load].BatchSizeRange.Min)
                        Schedule(() => AttemptToStart(moveTo));
                }
                #endregion
            }
        }
        public void Exit(ILoad load)
        {
            Log("Exit", load);
            if (!_loads_ReadyToExit.Contains(load)) return;
            _loads_ReadyToExit.Remove(load);
            var curr = _loadToBatch_Current[load];
            curr.Remove(load);
            _allLoads.Remove(load);
            StageIndices.Remove(load);
            _loadToBatch_Current.Remove(load);
            _loadToBatch_MovingTo.Remove(load);
            if (_activitiesToTrace.Contains(curr.Activity))
            {
                _activityHC_Passive[curr.Activity].ObserveChange(-1);
                _activityToLoads_Passive[curr.Activity].Remove(load);
            }
            DisposeIfEmpty(curr);
            _activityToLoads[curr.Activity].Remove(load);
            RemoveActivityIfNotUsed(curr.Activity);
        }
        private void ReadyToExit(ILoad load)
        {
            Log("ReadyToExit", load);
            OnReadyToExit.Invoke(load);
        }

        public void RequestToLock(IResource resource, double quantity)
        {
            Log("RqstLock", resource, quantity);
            _resourceQuantity_PendingLock[resource] = Math.Min(
                _resourceQuantity_PendingLock[resource] + quantity,
                _resourceHC_Available[resource].LastCount);
            _resourceHC_DynamicCapacity[resource].ObserveCount(
                Math.Max(0, _resourceHC_DynamicCapacity[resource].LastCount - quantity));
            AttemptToLock(resource);
        }

        public void RequestToUnlock(IResource resource, double quantity)
        {
            Log("Request Unlock", resource, quantity);
            var qtt = quantity;
            var removeFromPending = Math.Min(_resourceQuantity_PendingLock[resource], qtt);
            _resourceQuantity_PendingLock[resource] -= removeFromPending;
            qtt -= removeFromPending;
            _resourceHC_Available[resource].ObserveChange(qtt);
            _resourceHC_DynamicCapacity[resource].ObserveCount(
                _resourceHC_DynamicCapacity[resource].LastCount + quantity);
            UpdateHourCounter_ResourcePendingLock(resource);
            RecallForPending(new List<IResource> { resource }); /// changing from passive pending to unlocked shall also trigger starting of new activities
            if (qtt > 0)
            {
                Log("Unlocked", resource, qtt);                               
                OnUnlocked.Invoke(resource, qtt);
            }
        }

        /// <summary>
        /// Atempt to lock resource for the pending quantity
        /// </summary>
        private double AttemptToLock(IResource resource)
        {
            Log("AtmptLock", resource);
            var quantity = Math.Min(
                _resourceHC_Available[resource].LastCount - _resourceHC_Occupied[resource].LastCount,
                _resourceQuantity_PendingLock[resource]);
            _resourceHC_Available[resource].ObserveChange(-quantity);
            _resourceQuantity_PendingLock[resource] -= quantity;
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
                _resourceQuantity_PendingLock[resource],
                _resourceHC_Active[resource].LastCount);
            /// Remark: the active occupied resource has higher priority to be counted as it is more likely to be released and locked
            var pendingLockPassive = _resourceQuantity_PendingLock[resource] - pendingLockActive;

            _resourceHC_PendingLock_Active[resource].ObserveCount(pendingLockActive);
            _resourceHC_PendingLock_Passive[resource].ObserveCount(pendingLockPassive);
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
            _resourceToActivities = Assets.Resources.ToDictionary(r => r, r => new HashSet<IActivity>());
            _resourceBatchQuantity_Active = Assets.Resources.ToDictionary(res => res, res => new Dictionary<IBatch, double>());
            _resourceBatchQuantity_Passive = Assets.Resources.ToDictionary(res => res, res => new Dictionary<IBatch, double>());
            _resourceQuantity_PendingLock = Assets.Resources.ToDictionary(res => res, res => 0d);

            _activityToResources = new Dictionary<IActivity, List<IResource>>();            
            _activityToLoads = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToLoads_Pending = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToLoads_Active = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToLoads_Passive = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToBatchTimes_Pending = new Dictionary<IActivity, List<(Batch Batch, DateTime Time)>>();
            _activityToBatches_Batching = new Dictionary<IActivity, List<Batch>>();            

            _activityHC_Active = new Dictionary<IActivity, HourCounter>();
            _activityHC_Passive = new Dictionary<IActivity, HourCounter>();
            _activityHC_Pending = new Dictionary<IActivity, HourCounter>();
            _resourceHC_Active = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHC_Available = Assets.Resources.ToDictionary(r => r, r =>
            {
                var hc = AddHourCounter();
                hc.ObserveCount(r.Capacity);
                return hc;
            });
            _resourceHC_DynamicCapacity = Assets.Resources.ToDictionary(r => r, r =>
            {
                var hc = AddHourCounter();
                hc.ObserveCount(r.Capacity);
                return hc;
            });
            _resourceHC_Occupied = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHC_Passive = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHc_Pending = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHC_PendingLock_Active = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHC_PendingLock_Passive = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceActivityHC_Pending = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            _resourceActivityHC_Active = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            _resourceActivityHC_Passive = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            _resourceActivityHC_Occupied = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());

            foreach (var act in Assets.Activities)
            {
                _activitiesToTrace.Add(act);
                foreach (var res in Assets.Resources)
                {
                    _resourceActivityHC_Pending[res].Add(act, AddHourCounter());
                    _resourceActivityHC_Active[res].Add(act, AddHourCounter());
                    _resourceActivityHC_Passive[res].Add(act, AddHourCounter());
                    _resourceActivityHC_Occupied[res].Add(act, AddHourCounter());
                }
                _activityHC_Pending.Add(act, AddHourCounter(keepHistory: true));
                _activityHC_Active.Add(act, AddHourCounter(keepHistory: true));
                _activityHC_Passive.Add(act, AddHourCounter(keepHistory: true));
                _activityToLoads_Pending.Add(act, new HashSet<ILoad>());
                _activityToLoads_Active.Add(act, new HashSet<ILoad>());
                _activityToLoads_Passive.Add(act, new HashSet<ILoad>());
            }
        }

        public string Snapshot()
        {
            string str = "[Loads]\n";
            foreach (var load in _allLoads.OrderBy(l => l.Index))
            {
                str += string.Format("Id: {0}\tAct_Id: {1}\t", load, _loadToBatch_Current[load] == null ? "null" : _loadToBatch_Current[load].Activity.Id);
                if (_loadToBatch_Current[load] != null && _batchToAllocation.ContainsKey(_loadToBatch_Current[load]))
                    foreach (var (res, qtt) in _batchToAllocation[_loadToBatch_Current[load]].Requirement_ResourceQuantityList.SelectMany(i => i.Value))
                        str += string.Format("Res#{0}({1}) ", res.Id, qtt);
                str += "\n";
            }

            str += "[Resources]\n";
            foreach (var res in Assets.Resources)
            {
                str += string.Format("Id: {0}\tOccupied: ", res.Id);
                foreach (var current in _loadToBatch_Current.Values.Distinct().Where(curr => curr != null && 
                    _batchToAllocation[curr].ResourceQuantity_Aggregated.ContainsKey(res) && _batchToAllocation[curr].ResourceQuantity_Aggregated[res] > 0))
                    str += string.Format("{0} ", current);
                str += string.Format("\tPending:");
                foreach (var toMove in _resourceToActivities[res].SelectMany(a => _activityToBatchTimes_Pending[a].Select(i => i.Batch)))
                    str += string.Format("{0} ", toMove);
                str += "\n";
            }

            return str;
        }

        protected override void WarmedUpHandler()
        {
            CountOfLoads_Entered = 0;
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