using O2DESNet.Standard;

using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IBatches
    {
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

        /// <summary>
        /// Gets the batch to allocation.
        /// </summary>
        IReadOnlyDictionary<IBatch, ReadOnlyAllocation> BatchToAllocation { get; }
    }
}
