using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Text;

namespace O2DESNet.RCQueues
{
    /// <summary>
    /// Interface for RCQsContext
    /// </summary>
    public interface IActivityHandler
    {
        IActivity Activity { get; }
        void Start(ILoad load);
        void Depart(ILoad load);
        event Action<ILoad> OnRequestToStart;
    }

    /// <summary>
    /// Interface for general activity handler
    /// </summary>
    public interface IActivityHandler<TLoad> : IActivityHandler
    {
        void RequestToArrive(TLoad load);
        event Action<TLoad> OnReadyToDepart;
        IActivityHandler<TLoad> FlowTo(IActivityHandler<TLoad> nextActivityHandler);
        void FlowTo(Action<TLoad> targetEvent);
        bool IsActivityFit(TLoad load);
    }

    public class ActivityHandler<TLoad> : Sandbox, IActivityHandler<TLoad>
        where TLoad : ILoad
    {
        #region Statics
        public class Statics : IActivity
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public IReadOnlyList<IRequirement> Requirements { get { return RequirementList.AsReadOnly(); } }
            public List<Requirement> RequirementList { get; set; } = new List<Requirement>();
            public BatchSizeRange BatchSizeRange { get; } = new BatchSizeRange(1, 1);
            public Func<TLoad, Random, TimeSpan> Duration { get; set; } = (load, rs) => TimeSpan.Zero;
            public Func<(IBatch Batch, DateTime Time), (IBatch Batch, DateTime Time), int> BatchOrder { get; set; } = (t1, t2) => t1.Time.CompareTo(t2.Time);
            public Dictionary<string, object> Conditions { get; set; } = new Dictionary<string, object>();
        }

        public Statics Config { get; protected set; }
        public IActivity Activity { get { return Config; } }
        #endregion

        #region Dynamics
        public HashSet<TLoad> PendingLoads { get; } = new HashSet<TLoad>();
        public HashSet<TLoad> ActiveLoads { get; } = new HashSet<TLoad>();
        public HashSet<TLoad> PassiveLoads { get; } = new HashSet<TLoad>();
        private readonly HourCounter _hc_Occupied;
        public double AverageOccupation { get { return _hc_Occupied.AverageCount; } }
        #endregion

        #region Envents
        /// <summary>
        /// A load requests to start the activity
        /// In its extension, the filtering condition can be added, i.e., loads can be ignored if the condition check fails
        /// </summary>
        /// <param name="load">The load flow through the activity</param>
        public virtual void RequestToArrive(TLoad load)
        {
            if (IsActivityFit(load))
            {
                PendingLoads.Add(load);
                OnRequestToStart.Invoke(load);
            }
        }

        /// <summary>
        /// Check whether that load is fit into that activity
        /// </summary>
        /// <param name="load">The load flow through the activity</param>
        /// <returns>true if that load can fit into the activity</returns>
        public bool IsActivityFit(TLoad load)
        {
            if (Config.Conditions != null && Config.Conditions.Count > 0)
            {
                foreach (var key in Config.Conditions.Keys)
                {
                    if (Config.Conditions[key] != null)
                    {
                        object loadValue;
                        try
                        {
                            loadValue = load.GetType().GetProperty(key).GetValue(load);
                        }
                        catch
                        {
                            loadValue = null;
                        }

                        if (loadValue == null || !Config.Conditions[key].Equals(loadValue))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// The external Start event to be called by RCQsModel to start the activity when requested resources are allocated
        /// Note: this is supposed to be internal event, calling from other modules shall be forbidden
        /// </summary>
        /// <param name="load">The load which starts to process on the activty</param>
        public void Start(ILoad load)
        {
            if (!(load is TLoad)) throw new Exception("Load type is inconsistent.");
            var tLoad = (TLoad)load;
            if (!PendingLoads.Contains(tLoad)) throw new Exception("Load does not exists in the activity handler.");
            PendingLoads.Remove(tLoad);
            ActiveLoads.Add(tLoad);
            _hc_Occupied.ObserveChange(1);
            StartActivity((TLoad)load);
        }

        public void Depart(ILoad load)
        {
            PassiveLoads.Remove((TLoad)load);
            _hc_Occupied.ObserveChange(-1);
        }

        /// <summary>
        /// The internal Start event
        /// </summary>
        /// <param name="load">The load which starts to process on the activty</param>
        protected virtual void StartActivity(TLoad load)
        {
            Schedule(() => Finish(load), Config.Duration(load, DefaultRS));
        }

        protected void Finish(TLoad load)
        {
            ActiveLoads.Remove(load);
            PassiveLoads.Add(load);
            OnReadyToDepart.Invoke(load);
        }

        /// <summary>
        /// The output event when an entity finishes (not exits) its process on the activity
        /// </summary>
        public event Action<TLoad> OnReadyToDepart = load => { };

        public event Action<ILoad> OnRequestToStart = load => { };
        #endregion

        public ActivityHandler(Statics config, int seed = 0): base(seed)
        {
            Config = config;
            _hc_Occupied = AddHourCounter();
        }

        public IActivityHandler<TLoad> FlowTo(IActivityHandler<TLoad> nextActivityHandler)
        {
            OnReadyToDepart += nextActivityHandler.RequestToArrive;
            return nextActivityHandler;
        }

        public void FlowTo(Action<TLoad> targetEvent)
        {
            OnReadyToDepart += targetEvent;
        }
    }
}
