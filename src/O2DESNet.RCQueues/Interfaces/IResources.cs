using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IResources
    {
        /// <summary>
        /// Map resource to all relevant activities, i.e., those include the resource in their requirement
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyList<IActivity>> ResourceToActivities { get; }

        /// <summary>
        /// Gets the resource batch quantity active.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IBatch, double>> ResourceBatchQuantityActive { get; }

        /// <summary>
        /// Gets the resource batch quantity passive.
        /// </summary>
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
    }
}
