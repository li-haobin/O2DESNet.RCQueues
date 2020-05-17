using O2DESNet;
using O2DESNet.RCQueues.Common;
using O2DESNet.RCQueues.Interfaces;
using O2DESNet.Standard;

using System;
using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public class RCQueuesModel : Sandbox<IRCQueuesModelStatics>, IRCQueuesModel
    {
        public class Statics : IRCQueuesModelStatics
        {
            public string Id { get; }
            public IReadOnlyList<IResource> Resources { get; private set; }
            /// <summary>
            /// List of Activities to be traced
            /// </summary>
            public IReadOnlyList<IActivity> Activities { get; private set; }
            public Statics(IEnumerable<IResource> resources, IEnumerable<IActivity> activities = null)
            {
                Id = $"RCQsModel#{{0:N}}{Guid.NewGuid()}";
                Resources = resources.ToList().AsReadOnly();
                Activities = activities == null ? new List<IActivity>().AsReadOnly() : activities.ToList().AsReadOnly();
            }

            public RCQueuesModel GetSandbox(int seed = 0) { return new RCQueuesModel(this, seed); }
        }

        #region Dynamic Properties  

        #region by Load/Batch

        public IReadOnlyList<ILoad> AllLoads => _allLoads.ToList().AsReadOnly();
        private readonly HashSet<ILoad> _allLoads = new HashSet<ILoad>();

        public IReadOnlyList<ILoad> LoadsPendingToEnter => _loadsPendingToEnter.ToList().AsReadOnly();
        private readonly HashSet<ILoad> _loadsPendingToEnter = new HashSet<ILoad>();

        public IReadOnlyList<ILoad> LoadsReadyToExit => _loadsReadyToExit.ToList().AsReadOnly();
        private readonly HashSet<ILoad> _loadsReadyToExit = new HashSet<ILoad>();

        public IReadOnlyDictionary<ILoad, IBatch> LoadToBatchCurrent { get { return _loadToBatchCurrent.AsReadOnly(b => (IBatch)b); } }
        private readonly Dictionary<ILoad, Batch> _loadToBatchCurrent = new Dictionary<ILoad, Batch>();

        public IReadOnlyDictionary<ILoad, IBatch> LoadToBatchMovingTo => _loadToBatchMovingTo.AsReadOnly();
        private readonly Dictionary<ILoad, IBatch> _loadToBatchMovingTo = new Dictionary<ILoad, IBatch>();

        public IReadOnlyDictionary<IBatch, ReadOnlyAllocation> BatchToAllocation
        {
            get { return _batchToAllocation.AsReadOnly(a => a.AsReadOnly()); }
        }
        private readonly Dictionary<IBatch, Allocation> _batchToAllocation = new Dictionary<IBatch, Allocation>();

        /// <summary>
        /// The stage indices's which is strictly increasing for each successful move, 
        /// for all loads in the system.
        /// </summary>
        private readonly Dictionary<ILoad, int> _stageIndices = new Dictionary<ILoad, int>();
        #endregion

        #region by Activities
        public IReadOnlyList<IActivity> AllActivities => _allActivities.AsReadOnly();
        private readonly HashSet<IActivity> _allActivities = new HashSet<IActivity>();
        private readonly HashSet<IActivity> _activitiesToTrace = new HashSet<IActivity>();

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoads => _activityToLoads.AsReadOnly();
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoads;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoadsPending
        {
            get
            {
                return _activityToLoadsPending.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoadsPending;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoadsActive
        {
            get
            {
                return _activityToLoadsActive.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoadsActive;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoadsPassive
        {
            get
            {
                return _activityToLoadsPassive.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value).AsReadOnly();
            }
        }
        private readonly Dictionary<IActivity, HashSet<ILoad>> _activityToLoadsPassive;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<(IBatch Batch, DateTime Time)>> ActivityToBatchTimesPending
        {
            get { return _activityToBatchTimesPending.AsReadOnly(l => l.AsReadOnly(t => ((IBatch)t.Batch, t.Time))); }
        }
        private readonly Dictionary<IActivity, List<(Batch Batch, DateTime Time)>> _activityToBatchTimesPending;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<IBatch>> ActivityToBatchesBatching
        {
            get { return _activityToBatchesBatching.AsReadOnly(l => l.AsReadOnly(b => (IBatch)b)); }
        }
        private readonly Dictionary<IActivity, List<Batch>> _activityToBatchesBatching;

        public IReadOnlyDictionary<IActivity, IReadOnlyList<IResource>> ActivityToResources => _activityToResources.AsReadOnly();
        private readonly Dictionary<IActivity, List<IResource>> _activityToResources;
        #endregion

        #region by Resources

        public IReadOnlyDictionary<IResource, IReadOnlyList<IActivity>> ResourceToActivities => _resourceToActivities.AsReadOnly();
        private readonly Dictionary<IResource, HashSet<IActivity>> _resourceToActivities;

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IBatch, double>> ResourceBatchQuantityActive
        {
            get
            {
                return _resourceBatchQuantityActive.ToDictionary(i => i.Key, i => i.Value.AsReadOnly()).AsReadOnly();
            }
        }
        private readonly Dictionary<IResource, Dictionary<IBatch, double>> _resourceBatchQuantityActive;

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IBatch, double>> ResourceBatchQuantityPassive
        {
            get
            {
                return _resourceBatchQuantityPassive.ToDictionary(i => i.Key, i => i.Value.AsReadOnly()).AsReadOnly();
            }
        }
        private readonly Dictionary<IResource, Dictionary<IBatch, double>> _resourceBatchQuantityPassive;

        public IReadOnlyDictionary<IResource, double> ResourceQuantityOccupied
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => ResourceHourCounterOccupied[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> ResourceQuantityAvailable
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => ResourceHourCounterAvailable[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> ResourceQuantityDynamicCapacity
        {
            get { return Assets.Resources.ToReadOnlyDictionary(r => r, r => ResourceHourCounterDynamicCapacity[r].LastCount); }
        }

        public IReadOnlyDictionary<IResource, double> ResourceQuantityPendingLock => _resourceQuantityPendingLock.AsReadOnly();
        private readonly Dictionary<IResource, double> _resourceQuantityPendingLock;
        #endregion

        #region Statistics
        public int CountOfLoadsEntered { get; private set; } = 0;

        // Stage index = -1 implies called but not entered
        public int CountOfLoadsProcessing => _allLoads.Count(load => _stageIndices[load] > 0);
        public int CountOfLoadsExited => CountOfLoadsEntered - CountOfLoadsProcessing;

        private readonly Dictionary<OutputType, Dictionary<IActivity, HourCounter>> _activitiesHourCounter;

        //private readonly Dictionary<IActivity, HourCounter> _activityHourCounterPending;
        //private readonly Dictionary<IActivity, HourCounter> _activityHourCounterActive;
        //private readonly Dictionary<IActivity, HourCounter> _activityHourCounterPassive;

        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterPending;
        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterActive;
        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterPassive;
        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterOccupied;
        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterAvailable;
        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterDynamicCapacity;
        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterPendingLockActive;
        private readonly Dictionary<IResource, HourCounter> _resourceHourCounterPendingLockPassive;

        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHourCounterPending;
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHourCounterActive;
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHourCounterPassive;
        private readonly Dictionary<IResource, Dictionary<IActivity, HourCounter>> _resourceActivityHourCounterOccupied;

        public IReadOnlyDictionary<OutputType, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ActivitiesHourCounter
        {
            get
            {
                var temp = new Dictionary<OutputType, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>>
                {
                    { OutputType.Pending, _activitiesHourCounter[OutputType.Pending].AsReadOnly(hc => hc.AsReadOnly()) },
                    { OutputType.Active, _activitiesHourCounter[OutputType.Active].AsReadOnly(hc => hc.AsReadOnly()) },
                    { OutputType.Passive, _activitiesHourCounter[OutputType.Passive].AsReadOnly(hc => hc.AsReadOnly()) },
                };

                return temp;
            }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPending
        {
            get { return _resourceHourCounterPending.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterActive
        {
            get { return _resourceHourCounterActive.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPassive
        {
            get { return _resourceHourCounterPassive.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterOccupied
        {
            get { return _resourceHourCounterOccupied.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterAvailable
        {
            get { return _resourceHourCounterAvailable.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterDynamicCapacity
        {
            get { return _resourceHourCounterDynamicCapacity.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPendingLockActive
        {
            get { return _resourceHourCounterPendingLockActive.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPendingLockPassive
        {
            get { return _resourceHourCounterPendingLockPassive.AsReadOnly(hc => hc.AsReadOnly()); }
        }

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterPending
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHourCounterPending[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterActive
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHourCounterActive[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterPassive
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHourCounterPassive[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }

        public IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterOccupied
        {
            get
            {
                return _resourceToActivities.OrderBy(i => i.Key.Id)
                    .ToDictionary(i => i.Key, i => i.Value.OrderBy(j => j.Name)
                    .ToDictionary(act => act, act => _resourceActivityHourCounterOccupied[i.Key][act]))
                    .AsReadOnly(d => d.AsReadOnly(hc => hc.AsReadOnly()));
            }
        }

        #endregion
        #endregion

        #region Methods / Events

        /// <summary>
        /// Enquiry for remaining capacity of given resource, for given load and activity
        /// </summary>
        private double RemainingCapcity(IResource res, Batch toMove)
        {
            var available = _resourceHourCounterAvailable[res].LastCount;
            var remaining = available - _resourceHourCounterOccupied[res].LastCount;
            foreach (var curr in toMove.Select(load => _loadToBatchCurrent[load]).Distinct())
            {
                if (curr != null && curr.IsSubsetOf(toMove) && _batchToAllocation[curr].ResourceQuantityAggregated.ContainsKey(res))
                    /// recover resources currently occupied by subset batches
                    remaining += _batchToAllocation[curr].ResourceQuantityAggregated[res];
            }
            return Math.Max(0, remaining - _resourceQuantityPendingLock[res]);
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
                var toRqst = new List<ResourceQuantity>();
                while (toRqst.Sum(i => i.Quantity) < req.Quantity)
                {
                    var qtt = Math.Min(pool.First().Qtt, req.Quantity - toRqst.Sum(i => i.Quantity));
                    toRqst.Add(new ResourceQuantity(pool.First().Resource, qtt));
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

            _activityToBatchTimesPending.Add(act, new List<(Batch, DateTime)>());
            _activityToBatchesBatching.Add(act, new List<Batch>());
        }

        private void RemoveActivityIfNotUsed(IActivity act)
        {
            if (_activityToLoads[act].Count > 0 || _activitiesToTrace.Contains(act)) return;
            _activityToLoads.Remove(act);
            _allActivities.Remove(act);

            foreach (var res in _activityToResources[act].ToList()) _resourceToActivities[res].Remove(act);

            _activityToBatchTimesPending.Remove(act);
            _activityToBatchesBatching.Remove(act);

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
                    foreach (var i in _batchToAllocation[curr].ResourceQuantityAggregated)
                    {
                        var res = i.Key;
                        var qtt = i.Value;
                        var act = curr.Activity;

                        if (!released.ContainsKey(res)) released.Add(res, 0);
                        released[res] += qtt;

                        _resourceBatchQuantityPassive[res].Remove(curr);
                        _resourceHourCounterPassive[res].ObserveChange(-qtt);
                        _resourceHourCounterOccupied[res].ObserveChange(-qtt);
                        if (_activitiesToTrace.Contains(act))
                        {
                            _resourceActivityHourCounterPassive[res][act].ObserveChange(-qtt);
                            _resourceActivityHourCounterOccupied[res][act].ObserveChange(-qtt);
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
                if (_resourceQuantityPendingLock[res] > 0)
                {
                    released[res] -= AttemptToLock(res);
                    if (released[res] == 0) released.Remove(res);
                }
            }
            return released;
        }

        private void UpdHourCounter_Resource_Pending(IResource res)
        {
            var qtts = _resourceToActivities[res].SelectMany(a => _activityToBatchTimesPending[a])
                .Select(i => (i.Batch.Activity, i.Batch.Activity
                .Requirements.Where(req => req.Pool.Contains(res)).Sum(req => req.Quantity)))
                .GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Sum(t => t.Item2));

            foreach (var act in _resourceActivityHourCounterPending[res].Keys)
                _resourceActivityHourCounterPending[res][act].ObserveCount(qtts.ContainsKey(act) ? qtts[act] : 0);
            _resourceHourCounterPending[res].ObserveCount(qtts.Values.Sum());
        }

        /// <summary>
        /// Attempt to move for loads pending for released resources
        /// </summary>
        /// <param name="released">Map resource to its released quantity</param>
        protected void RecallForPending(IEnumerable<IResource> released)
        {
            /// Activities that have pending load and need to be reviewed once resource has capacity
            var activitiesPendingToReview = new HashSet<IActivity>();
            /// add relavent activities for review
            foreach (var a in released.SelectMany(i => _resourceToActivities[i]).Distinct()
                .Where(a => _activityToBatchTimesPending[a].Count > 0)) activitiesPendingToReview.Add(a);
            /// loop until the review list is clear or there is one load identified to attempt to move
            while (activitiesPendingToReview.Count > 0)
            {
                /// get the earliest pending activity, and the earliest pending load
                var pendingActivity = activitiesPendingToReview
                    .OrderBy(a => _activityToBatchTimesPending[a].First().Time).First();
                var moveTo = _activityToBatchTimesPending[pendingActivity].First().Batch;
                /// attempt to move to next activity or remove from pending
                if (moveTo.Phase == BatchPhase.Pending)
                {
                    Schedule(() => AttemptToStart(moveTo));
                    activitiesPendingToReview.Remove(moveTo.Activity);
                    //break;
                }
                else
                {
                    _activityToBatchTimesPending[pendingActivity].RemoveAt(0);
                    if (_activityToBatchTimesPending[pendingActivity].Count == 0)
                        activitiesPendingToReview.Remove(pendingActivity);
                }
            }
        }

        private Batch GetMoveTo(ILoad load, IActivity next)
        {
            Batch moveTo;

            #region Add the load to the pending batch
            if (_activityToBatchTimesPending[next].Count > 0 && _activityToBatchTimesPending[next].Last().Batch.Count < next.BatchSizeRange.Max)
            {
                moveTo = _activityToBatchTimesPending[next].Last().Batch;
                moveTo.Add(load);
                //if (Assets.TraceActivities)
                //    Act_HC_Pending_Dict[moveTo.Activity].ObserveChange(1, ClockTime);
            }
            #endregion

            #region Add the load to the non-pending (batching) batch
            else
            {
                if (_activityToBatchesBatching[next].Count == 0 || _activityToBatchesBatching[next].Last().Count >= next.BatchSizeRange.Max)
                    _activityToBatchesBatching[next].Add(new Batch { Activity = next });
                moveTo = _activityToBatchesBatching[next].Last();
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
            _loadsPendingToEnter.Add(load);
            _loadToBatchCurrent[load] = null;
            _stageIndices.Add(load, 0);

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
            if (moveTo.Phase == BatchPhase.Batching) _activityToBatchesBatching[moveTo.Activity].Remove(moveTo);

            var request = RequestResources(moveTo);
            #region Has sufficient requested resources   
            if (request != null)
            {
                /// Empty/partially clear the current batches due to successful start of move-to activity
                var currents = moveTo.Select(load => _loadToBatchCurrent[load]).Distinct()
                    .Where(curr => curr != null).ToList();
                foreach (var curr in currents) curr.RemoveWhere(load => moveTo.Contains(load));
                var released = ReleaseResources(currents);

                /// create resource occupation                        
                _batchToAllocation.Add(moveTo, request);
                foreach (var i in request.RequirementResourceQuantityList)
                {
                    foreach (ResourceQuantity item in i.Value)
                    {
                        if (released.ContainsKey(item.Resource))
                        {
                            released[item.Resource] -= item.Quantity;
                            if (released[item.Resource] <= 0) released.Remove(item.Resource);
                        }
                    }
                }

                /// clear from pending list
                if (moveTo.Phase == BatchPhase.Pending)
                {
                    var idx = _activityToBatchTimesPending[moveTo.Activity].FindIndex(t => t.Batch.Equals(moveTo));
                    if (idx < 0) throw new Exception("Could not find the batch in the pending list");
                    _activityToBatchTimesPending[moveTo.Activity].RemoveAt(idx);
                    foreach (var res in _activityToResources[moveTo.Activity])
                        UpdHourCounter_Resource_Pending(res);
                }

                /// Update statistics   
                if (_activitiesToTrace.Contains(moveTo.Activity))
                {
                    if (moveTo.Phase == BatchPhase.Pending)
                    {
                        _activitiesHourCounter[OutputType.Pending][moveTo.Activity].ObserveChange(-moveTo.Count);
                        foreach (var load in moveTo) _activityToLoadsPending[moveTo.Activity].Remove(load);
                    }

                    _activitiesHourCounter[OutputType.Passive][moveTo.Activity].ObserveChange(moveTo.Count);

                    foreach (var load in moveTo) _activityToLoadsActive[moveTo.Activity].Add(load);
                }

                foreach (var load in moveTo)
                {
                    var curr = _loadToBatchCurrent[load];
                    if (curr != null && _activitiesToTrace.Contains(curr.Activity))
                    {
                        _activitiesHourCounter[OutputType.Passive][curr.Activity].ObserveChange(-1);
                        _activityToLoadsPassive[curr.Activity].Remove(load);
                    }
                }

                foreach (var i in _batchToAllocation[moveTo].ResourceQuantityAggregated)
                {
                    var res = i.Key;
                    var act = moveTo.Activity;
                    var qtt = i.Value;
                    _resourceBatchQuantityActive[res].Add(moveTo, qtt);
                    _resourceHourCounterActive[res].ObserveChange(qtt);
                    _resourceHourCounterOccupied[res].ObserveChange(qtt);
                    if (_activitiesToTrace.Contains(act))
                    {
                        _resourceActivityHourCounterActive[res][act].ObserveChange(qtt);
                        _resourceActivityHourCounterOccupied[res][act].ObserveChange(qtt);
                    }
                }

                /// Update activity record for the load                    
                Schedule(() => Start(moveTo));
                foreach (var load in moveTo)
                {
                    if (_stageIndices[load] == 0)
                    /// moved to the first activity
                    {
                        CountOfLoadsEntered++;
                        _loadsPendingToEnter.Remove(load);
                        Schedule(() => Enter(load, moveTo.Activity));
                    }
                    else
                    /// clear from previous activity
                    {
                        var prev = _loadToBatchCurrent[load].Activity;
                        _activityToLoads[prev].Remove(load);
                        RemoveActivityIfNotUsed(prev);
                    }
                    _stageIndices[load]++;
                    _loadToBatchCurrent[load] = moveTo;
                    _loadToBatchMovingTo[load] = null;
                }

                moveTo.Phase = BatchPhase.Started;

                /// check for the next batch in the same activity
                if (_activityToBatchTimesPending[moveTo.Activity].Count > 0)
                    Schedule(() => AttemptToStart(_activityToBatchTimesPending[moveTo.Activity].First().Batch));

                /// check for released resource
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
                    if (moveTo.Count(l => _loadToBatchCurrent[l] != null && _loadToBatchCurrent[l].Activity == moveTo.Activity) > 0)
                        clockTime = DateTime.MinValue; /// consider the case of immediate re-work having highest priority, to avoid deadlock
                    _activityToBatchTimesPending[moveTo.Activity].Add((moveTo, clockTime));
                    _activityToBatchTimesPending[moveTo.Activity].Sort((t1, t2) => t1.Time.CompareTo(t2.Time));

                    foreach (var res in _activityToResources[moveTo.Activity])
                    {
                        UpdHourCounter_Resource_Pending(res);
                        if (_resourceQuantityPendingLock[res] > 0) UpdateHourCounter_ResourcePendingLock(res);
                    }

                    /// Update statistics
                    foreach (var load in moveTo)
                    {
                        if (_loadToBatchCurrent[load] != null)
                            _loadToBatchCurrent[load].Phase = BatchPhase.Passive;
                    }
                    if (_activitiesToTrace.Contains(moveTo.Activity))
                    {
                        _activitiesHourCounter[OutputType.Pending][moveTo.Activity].ObserveChange(moveTo.Count);
                        foreach (var load in moveTo) _activityToLoadsPending[moveTo.Activity].Add(load);
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
        public void Finish(IBatch batch, Dictionary<ILoad, IActivity> next)
        {
            if (batch.Phase != BatchPhase.Started) return; /// prevent duplicated events
            Log("Finish", batch);
            ((Batch)batch).Phase = BatchPhase.Finished;

            /// Statistics                
            if (_activitiesToTrace.Contains(batch.Activity))
            {
                _activitiesHourCounter[OutputType.Active][batch.Activity].ObserveChange(-batch.Count);
                _activitiesHourCounter[OutputType.Passive][batch.Activity].ObserveChange(batch.Count);

                foreach (var load in batch)
                {
                    _activityToLoadsActive[batch.Activity].Remove(load);
                    _activityToLoadsPassive[batch.Activity].Add(load);
                }
            }
            foreach (var i in _batchToAllocation[batch].ResourceQuantityAggregated)
            {
                var res = i.Key;
                var act = batch.Activity;
                var qtt = i.Value;
                _resourceBatchQuantityActive[res].Remove(batch);
                _resourceBatchQuantityPassive[res].Add(batch, qtt);
                _resourceHourCounterActive[res].ObserveChange(-qtt);
                _resourceHourCounterPassive[res].ObserveChange(qtt);
                if (_activitiesToTrace.Contains(act))
                {
                    _resourceActivityHourCounterActive[res][act].ObserveChange(-qtt);
                    _resourceActivityHourCounterPassive[res][act].ObserveChange(qtt);
                }
                if (ResourceQuantityPendingLock[res] > 0) UpdateHourCounter_ResourcePendingLock(res);
            }

            foreach (var load in batch.ToList())
            {
                #region Move out from the last activity & the system
                if (next == null || !next.ContainsKey(load) || next[load] == null)
                {
                    _loadToBatchMovingTo[load] = null;
                    _loadsReadyToExit.Add(load);
                    Schedule(() => ReadyToExit(load));
                }
                #endregion
                #region Prepare to move to the next activity
                else
                {
                    AddActivityIfNotExist(next[load]);
                    _activityToLoads[next[load]].Add(load);
                    var moveTo = GetMoveTo(load, next[load]);
                    if (moveTo.Phase == BatchPhase.Batching && moveTo.Count >= next[load].BatchSizeRange.Min)
                        Schedule(() => AttemptToStart(moveTo));
                }
                #endregion
            }
        }
        public void Exit(ILoad load)
        {
            Log("Exit", load);
            if (!_loadsReadyToExit.Contains(load)) return;
            _loadsReadyToExit.Remove(load);
            var curr = _loadToBatchCurrent[load];
            curr.Remove(load);
            _allLoads.Remove(load);
            _stageIndices.Remove(load);
            _loadToBatchCurrent.Remove(load);
            _loadToBatchMovingTo.Remove(load);
            if (_activitiesToTrace.Contains(curr.Activity))
            {
                _activitiesHourCounter[OutputType.Passive][curr.Activity].ObserveChange(-1);
                _activityToLoadsPassive[curr.Activity].Remove(load);
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
            _resourceQuantityPendingLock[resource] = Math.Min(
                _resourceQuantityPendingLock[resource] + quantity,
                _resourceHourCounterAvailable[resource].LastCount);
            _resourceHourCounterDynamicCapacity[resource].ObserveCount(
                Math.Max(0, _resourceHourCounterDynamicCapacity[resource].LastCount - quantity));
            AttemptToLock(resource);
        }

        public void RequestToUnlock(IResource resource, double quantity)
        {
            Log("Request Unlock", resource, quantity);
            var qtt = quantity;
            var removeFromPending = Math.Min(_resourceQuantityPendingLock[resource], qtt);
            _resourceQuantityPendingLock[resource] -= removeFromPending;
            qtt -= removeFromPending;
            _resourceHourCounterAvailable[resource].ObserveChange(qtt);
            _resourceHourCounterDynamicCapacity[resource].ObserveCount(
                _resourceHourCounterDynamicCapacity[resource].LastCount + quantity);
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
                _resourceHourCounterAvailable[resource].LastCount - _resourceHourCounterOccupied[resource].LastCount,
                _resourceQuantityPendingLock[resource]);
            _resourceHourCounterAvailable[resource].ObserveChange(-quantity);
            _resourceQuantityPendingLock[resource] -= quantity;
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
                _resourceQuantityPendingLock[resource],
                _resourceHourCounterActive[resource].LastCount);
            /// Remark: the active occupied resource has higher priority to be counted as it is more likely to be released and locked
            var pendingLockPassive = _resourceQuantityPendingLock[resource] - pendingLockActive;

            _resourceHourCounterPendingLockActive[resource].ObserveCount(pendingLockActive);
            _resourceHourCounterPendingLockPassive[resource].ObserveCount(pendingLockPassive);
        }
        #endregion

        #region Output Events - Reference to Getters
        public event Action<ILoad, IActivity> OnEntered = (load, activity) => { };
        public event Action<ILoad> OnReadyToExit = load => { };
        public event Action<IResource, double> OnLocked = (resource, quantity) => { };
        public event Action<IResource, double> OnUnlocked = (resource, quantity) => { };
        public event Action<IBatch> OnStarted = batch => { };
        #endregion

        public RCQueuesModel(IRCQueuesModelStatics assets, int seed, string id = null) : base(assets, seed, id)
        {
            _resourceToActivities = Assets.Resources.ToDictionary(r => r, r => new HashSet<IActivity>());
            _resourceBatchQuantityActive = Assets.Resources.ToDictionary(res => res, res => new Dictionary<IBatch, double>());
            _resourceBatchQuantityPassive = Assets.Resources.ToDictionary(res => res, res => new Dictionary<IBatch, double>());
            _resourceQuantityPendingLock = Assets.Resources.ToDictionary(res => res, res => 0d);

            _activityToResources = new Dictionary<IActivity, List<IResource>>();
            _activityToLoads = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToLoadsPending = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToLoadsActive = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToLoadsPassive = new Dictionary<IActivity, HashSet<ILoad>>();
            _activityToBatchTimesPending = new Dictionary<IActivity, List<(Batch Batch, DateTime Time)>>();
            _activityToBatchesBatching = new Dictionary<IActivity, List<Batch>>();

            //_activityHourCounterActive = new Dictionary<IActivity, HourCounter>();
            //_activityHourCounterPassive = new Dictionary<IActivity, HourCounter>();
            //_activityHourCounterPending = new Dictionary<IActivity, HourCounter>();
            _resourceHourCounterActive = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHourCounterAvailable = Assets.Resources.ToDictionary(r => r, r =>
            {
                var hc = AddHourCounter();
                hc.ObserveCount(r.Capacity);
                return hc;
            });
            _resourceHourCounterDynamicCapacity = Assets.Resources.ToDictionary(r => r, r =>
            {
                var hc = AddHourCounter();
                hc.ObserveCount(r.Capacity);
                return hc;
            });
            _resourceHourCounterOccupied = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHourCounterPassive = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHourCounterPending = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHourCounterPendingLockActive = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceHourCounterPendingLockPassive = Assets.Resources.ToDictionary(r => r, r => AddHourCounter());
            _resourceActivityHourCounterPending = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            _resourceActivityHourCounterActive = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            _resourceActivityHourCounterPassive = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());
            _resourceActivityHourCounterOccupied = Assets.Resources.ToDictionary(r => r, r => new Dictionary<IActivity, HourCounter>());

            _activitiesHourCounter = new Dictionary<OutputType, Dictionary<IActivity, HourCounter>>();
            _activitiesHourCounter.Add(OutputType.Pending, new Dictionary<IActivity, HourCounter>());
            _activitiesHourCounter.Add(OutputType.Active, new Dictionary<IActivity, HourCounter>());
            _activitiesHourCounter.Add(OutputType.Passive, new Dictionary<IActivity, HourCounter>());

            foreach (var act in Assets.Activities)
            {
                _activitiesToTrace.Add(act);
                foreach (var res in Assets.Resources)
                {
                    _resourceActivityHourCounterPending[res].Add(act, AddHourCounter());
                    _resourceActivityHourCounterActive[res].Add(act, AddHourCounter());
                    _resourceActivityHourCounterPassive[res].Add(act, AddHourCounter());
                    _resourceActivityHourCounterOccupied[res].Add(act, AddHourCounter());
                }

                _activitiesHourCounter[OutputType.Pending].Add(act, AddHourCounter(keepHistory: true));
                _activitiesHourCounter[OutputType.Active].Add(act, AddHourCounter(keepHistory: true));
                _activitiesHourCounter[OutputType.Passive].Add(act, AddHourCounter(keepHistory: true));

                _activityToLoadsPending.Add(act, new HashSet<ILoad>());
                _activityToLoadsActive.Add(act, new HashSet<ILoad>());
                _activityToLoadsPassive.Add(act, new HashSet<ILoad>());
            }
        }

        public string Snapshot()
        {
            string str = "[Loads]\n";
            foreach (var load in _allLoads.OrderBy(l => l.Index))
            {
                str +=
                    $"Id: {load}\tAct_Id: {(_loadToBatchCurrent[load] == null ? Guid.Empty : _loadToBatchCurrent[load].Activity.Id)}\t";
                if (_loadToBatchCurrent[load] != null && _batchToAllocation.ContainsKey(_loadToBatchCurrent[load]))
                    foreach (ResourceQuantity item in _batchToAllocation[_loadToBatchCurrent[load]].RequirementResourceQuantityList.SelectMany(i => i.Value))
                        str += $"Res#{item.Resource.Id}({item.Quantity}) ";
                str += "\n";
            }

            str += "[Resources]\n";
            foreach (var res in Assets.Resources)
            {
                str += $"Id: {res.Id}\tOccupied: ";
                foreach (var current in _loadToBatchCurrent.Values.Distinct().Where(curr => curr != null &&
                    _batchToAllocation[curr].ResourceQuantityAggregated.ContainsKey(res) && _batchToAllocation[curr].ResourceQuantityAggregated[res] > 0))
                    str += $"{current} ";
                str += string.Format("\tPending:");
                foreach (var toMove in _resourceToActivities[res].SelectMany(a => _activityToBatchTimesPending[a].Select(i => i.Batch)))
                    str += $"{toMove} ";
                str += "\n";
            }

            return str;
        }

        protected override void WarmedUpHandler()
        {
            CountOfLoadsEntered = 0;
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (Action<ILoad, IActivity> i in OnEntered.GetInvocationList()) OnEntered -= i;
            foreach (Action<ILoad> i in OnReadyToExit.GetInvocationList()) OnReadyToExit -= i;
            foreach (Action<IResource, double> i in OnLocked.GetInvocationList()) OnLocked -= i;
            foreach (Action<IResource, double> i in OnUnlocked.GetInvocationList()) OnUnlocked -= i;
            foreach (Action<IBatch> i in OnStarted.GetInvocationList()) OnStarted -= i;
        }
    }
}