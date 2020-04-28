﻿using O2DESNet.Standard;
using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues
{
    public interface IRCQsModelStatics : IAssets
    {
        IReadOnlyList<IResource> Resources { get; }
        IReadOnlyList<IActivity> Activities { get; }        
    }

    public interface IRCQsModel : ISandbox
    {
        IRCQsModelStatics Assets { get; }

        #region by Loads/Batches
        /// <summary>
        /// All loads relevant in the model
        /// </summary>
        IReadOnlyList<ILoad> AllLoads { get; }
        /// <summary>
        /// Loads that requested but pending to enter into the model, 
        /// due to resource or batch size constraint
        /// </summary>
        IReadOnlyList<ILoad> Loads_PendingToEnter { get; }
        /// <summary>
        /// Loads that are ready to exit from the model
        /// </summary>
        IReadOnlyList<ILoad> Loads_ReadyToExit { get; }
        /// <summary>
        /// Map load to the batch/activity which it is currently in, either on active or passive status
        /// Map to null if the load is not in any batch/activity, i.e., pending for the first activity.
        /// </summary>
        IReadOnlyDictionary<ILoad, IBatch> Load_Batch_Current { get; }
        /// <summary>
        /// Map load to the batch/activity which it is moving to, but stuck due to resource or batch size constraint;
        /// Map to null if the load is not moving to any batch/activity, i.e., on active status in the current batch/activity.
        /// </summary>
        IReadOnlyDictionary<ILoad, IBatch> Load_Batch_MovingTo { get; }
        IReadOnlyDictionary<IBatch, ReadOnlyAllocation> Batch_Allocation { get; }
        #endregion        

        #region by Activities
        /// <summary>
        /// All activities relevant in the model
        /// </summary>
        IReadOnlyList<IActivity> AllActivities { get; }
        /// <summary>
        /// Map activity to all loads that is relevant to it, i.e., pending, or active/passive processing
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> Activity_Loads { get; }
        /// <summary>
        /// Map activities to the loads that are pending for it due to insufficient resources, and the time of the pending start
        /// </summary>   
        IReadOnlyDictionary<IActivity, IReadOnlyList<(IBatch Batch, DateTime Time)>> Activity_BatchTimes_Pending { get; }
        /// <summary>
        /// Map activity to the batch that is created but not met the batch size constraint
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<IBatch>> Activity_Batches_Batching { get; }
        /// <summary>
        /// Map the activity to the list of quantified resource 
        /// This is to be formed and fixed at init function.
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<IResource>> Activity_Resources { get; }
        #endregion

        #region by Resources
        /// <summary>
        /// Map resource to all relevant activities, i.e., those include the resource in their requirement
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyList<IActivity>> Resource_Activities { get; }
        /// <summary>
        /// Map resource to its quantity that is occupied
        /// </summary>
        IReadOnlyDictionary<IResource, double> Resource_Quantity_Occupied { get; }
        /// <summary>
        /// Map resource to its quantity that is available to be occupied
        /// </summary>
        IReadOnlyDictionary<IResource, double> Resource_Quantity_Available { get; }        
        /// <summary>
        /// Map resource to the quantity that is pending to be locked
        /// </summary>
        IReadOnlyDictionary<IResource, double> Resource_Quantity_PendingLock { get; }
        /// <summary>
        /// Map resource to its actual dynamic capacity
        /// Note: dynamic capacity - pending to lock = available quantity
        /// </summary>
        IReadOnlyDictionary<IResource, double> Resource_Quantity_DynamicCapacity { get; }
        #endregion

        #region Statistics
        int CountLoads_Entered { get; }
        int CountLoads_Processing { get; }
        int CountLoads_Exited { get; }
        /// <summary>
        /// HourCounters by Activity for number of pending jobs (i.e., batches)
        /// </summary>
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> Activity_HC_Pending { get; }
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> Activity_HC_Active { get; }
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> Activity_HC_Passive { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Pending { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Active { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Passive { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Occupied { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_Available { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_DynamicCapacity { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_PendingLock_Active { get; }
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> Resource_HC_PendingLock_Passive { get; }
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Pending { get; }
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Active { get; }
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Passive { get; }
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> Resource_Activity_HC_Occupied { get; }
        #endregion

        void RequestEnter(ILoad load, IActivity init);
        void Finish(IBatch batch, Dictionary<ILoad, IActivity> nexts);
        void Exit(ILoad load);
        void RequestLock(IResource resource, double quantity);
        void RequestUnlock(IResource resource, double quantity);

        event Action<ILoad, IActivity> OnEntered;
        event Action<ILoad> OnReadyToExit;
        event Action<IResource, double> OnLocked;
        event Action<IResource, double> OnUnlocked;
        event Action<IBatch> OnStarted;
    }
}
