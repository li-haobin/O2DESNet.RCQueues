using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace O2DESNet.RCQueues
{
    public class RCQsContext : Sandbox
    {
        #region Statics
        public class Statics
        {
            public List<IResource> Resources { get; set; }
        }
        private Statics Config { get; set; }
        #endregion

        #region Dynamics
        private readonly RCQsModel _rcqModel;
        private readonly List<IActivityHandler> _activityHandlers = new List<IActivityHandler>();

        /// <summary>
        /// Contains all loads in the system (entered but not exited), mapping to the activity handler they are currently in
        /// </summary>
        private readonly Dictionary<ILoad, IActivityHandler> _occupying = new Dictionary<ILoad, IActivityHandler>();
        /// <summary>
        /// Contains all loads requesting to start an activity, including requesting to enter for the 1st activity 
        /// </summary>
        private readonly Dictionary<ILoad, IActivityHandler> _requesting = new Dictionary<ILoad, IActivityHandler>();
        /// <summary>
        /// Map load to its batch
        /// </summary>
        private readonly Dictionary<ILoad, IBatch> _loadToBatch = new Dictionary<ILoad, IBatch>();

        public Dictionary<IResource, double> ResourceOccupied
        {
            get
            {
                return Config.Resources.ToDictionary(res => res,
                    res => _rcqModel.ResourceHC_Occupied[res].LastCount);
            }
        }
        public Dictionary<IResource, double> ResourceUtilization
        {
            get
            {
                return Config.Resources.ToDictionary(res => res, 
                    res => _rcqModel.ResourceHC_Occupied[res].AverageCount / 
                    _rcqModel.ResourceHC_Available[res].AverageCount);
            }
        }
        public Dictionary<IResource, double> ResourceAvailability
        {
            get
            {
                return Config.Resources.ToDictionary(res => res,
                    res => _rcqModel.ResourceHC_Available[res].AverageCount);
            }
        }
        protected Dictionary<ILoad, ReadOnlyAllocation> LoadToAllocation
        {
            get
            {
                return _rcqModel.LoadToBatch_Current
                    .Join(_rcqModel.BatchToAllocation, p => p.Value, q => q.Key, (p, q) => new { p.Key, q.Value })
                    .ToDictionary(p => p.Key, p => p.Value);
            }
        }
        protected IActivityHandler AddChild(IActivityHandler activityHandler)
        {
            AddChild(activityHandler as Sandbox);
            _activityHandlers.Add(activityHandler);
            activityHandler.OnRequestToStart += load => RequestToStart(load, activityHandler);
            return activityHandler;
        }
        #endregion

        #region Events
        private void Start(IBatch batch)
        {          
            var load = batch.First();

            // for debug
            //if (load.Index == 11 && _requesting[load].Activity.Name == "PreparingToDismount") ;
            //if (load.Index == 11 && _requesting[load].Activity.Name == "Dismounting") ;

            if (!_occupying.ContainsKey(load))
            {
                /// new to the system   
                _occupying.Add(load, _requesting[load]);
                _loadToBatch.Add(load, batch);
            }
            else
            {
                /// entered in the system                
                _occupying[load].Depart(load);
                _occupying[load] = _requesting[load];
                _loadToBatch[load] = batch;
            }            
            _occupying[load].Start(load);
            _requesting.Remove(load);
        }

        private void RequestToStart(ILoad load, IActivityHandler activityHandler)
        {
            // for debug
            //if (load.Index == 11 && activityHandler.Activity.Name == "PreparingToDismount") ;
            //if (load.Index == 11 && activityHandler.Activity.Name == "Dismounting") ;

            _requesting.Add(load, activityHandler);
            var activity = activityHandler.Activity;
            if (!_occupying.ContainsKey(load))
            {
                /// new to the system   
                _rcqModel.RequestToEnter(load, activity);
            }
            else
            {
                /// entered in the system
                _rcqModel.Finish(_loadToBatch[load], new Dictionary<ILoad, IActivity> { { load, activity } });
            }
        }

        protected void Exit(ILoad load)
        {
            if (!_occupying.ContainsKey(load)) throw new Exception("The load does not exist.");
            _rcqModel.Finish(_loadToBatch[load], null);
            _occupying[load].Depart(load);
            _occupying.Remove(load);
            _loadToBatch.Remove(load);
        }
        #endregion

        public RCQsContext(Statics config, int seed = 0) : base(seed)
        {
            Config = config;
            _rcqModel = AddChild(new RCQsModel(new RCQsModel.Statics(Config.Resources), DefaultRS.Next()));
            _rcqModel.OnStarted += Start;
            _rcqModel.OnReadyToExit += _rcqModel.Exit;
        }
    }
}
