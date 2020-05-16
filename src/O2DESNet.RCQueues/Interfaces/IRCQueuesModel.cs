using O2DESNet.RCQueues.Interfaces;
using O2DESNet.Standard;

using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{

    public interface IRCQueuesModel : ISandbox
    {
        IRCQueuesModelStatics Assets { get; }

        #region by Loads/Batches
        /// <summary>
        /// All loads relevant in the model
        /// </summary>
        IReadOnlyList<ILoad> AllLoads { get; }
        /// <summary>
        /// Loads that requested but pending to enter into the model, 
        /// due to resource or batch size constraint
        /// </summary>
        IReadOnlyList<ILoad> LoadsPendingToEnter { get; }
        /// <summary>
        /// Loads that are ready to exit from the model
        /// </summary>
        IReadOnlyList<ILoad> LoadsReadyToExit { get; }
        /// <summary>
        /// Map load to the batch/activity which it is currently in, either on active or passive status
        /// Map to null if the load is not in any batch/activity, i.e., pending for the first activity.
        /// </summary>
        IReadOnlyDictionary<ILoad, IBatch> LoadToBatchCurrent { get; }
        /// <summary>
        /// Map load to the batch/activity which it is moving to, but stuck due to resource or batch size constraint;
        /// Map to null if the load is not moving to any batch/activity, i.e., on active status in the current batch/activity.
        /// </summary>
        IReadOnlyDictionary<ILoad, IBatch> LoadToBatchMovingTo { get; }
        IReadOnlyDictionary<IBatch, ReadOnlyAllocation> BatchToAllocation { get; }
        #endregion        

        #region by Activities
        /// <summary>
        /// All activities relevant in the model
        /// </summary>
        IReadOnlyList<IActivity> AllActivities { get; }
        /// <summary>
        /// Map activity to all loads that is relevant to it, i.e., pending, or active/passive processing
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoads { get; }
        /// <summary>
        /// Map activities to the loads that are pending for it due to insufficient resources, and the time of the pending start
        /// </summary>   
        IReadOnlyDictionary<IActivity, IReadOnlyList<(IBatch Batch, DateTime Time)>> ActivityToBatchTimesPending { get; }
        /// <summary>
        /// Map activity to the batch that is created but not met the batch size constraint
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<IBatch>> ActivityToBatchesBatching { get; }
        /// <summary>
        /// Map the activity to the list of quantified resource 
        /// This is to be formed and fixed at init function.
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<IResource>> ActivityToResources { get; }
        #endregion

        #region by Resources
        /// <summary>
        /// Map resource to all relevant activities, i.e., those include the resource in their requirement
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyList<IActivity>> ResourceToActivities { get; }
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IBatch, double>> ResourceBatchQuantityActive { get; }
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IBatch, double>> ResourceBatchQuantityPassive { get; }
        /// <summary>
        /// Map resource to its quantity that is occupied
        /// </summary>
        IReadOnlyDictionary<IResource, double> ResourceQuantityOccupied { get; }
        /// <summary>
        /// Map resource to its quantity that is available to be occupied
        /// </summary>
        IReadOnlyDictionary<IResource, double> ResourceQuantityAvailable { get; }        
        /// <summary>
        /// Map resource to the quantity that is pending to be locked
        /// </summary>
        IReadOnlyDictionary<IResource, double> ResourceQuantityPendingLock { get; }
        /// <summary>
        /// Map resource to its actual dynamic capacity
        /// Note: dynamic capacity - pending to lock = available quantity
        /// </summary>
        IReadOnlyDictionary<IResource, double> ResourceQuantityDynamicCapacity { get; }
        #endregion

        void RequestToEnter(ILoad load, IActivity init);
        void Finish(IBatch batch, Dictionary<ILoad, IActivity> next);
        void Exit(ILoad load);
        void RequestToLock(IResource resource, double quantity);
        void RequestToUnlock(IResource resource, double quantity);

        event Action<ILoad, IActivity> OnEntered;
        event Action<ILoad> OnReadyToExit;
        event Action<IResource, double> OnLocked;
        event Action<IResource, double> OnUnlocked;
        event Action<IBatch> OnStarted;
    }
}
