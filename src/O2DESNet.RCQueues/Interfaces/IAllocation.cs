using O2DESNet.RCQueues.Common;

using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    /// <summary>
    /// Describes the resource allocation for a batch
    /// </summary>
    public interface IAllocation
    {
        /// <summary>
        /// Map requirement to list of resource-quantity tuples allocated for it
        /// </summary>
        IReadOnlyDictionary<IRequirement, IReadOnlyList<ResourceQuantity>> RequirementResourceQuantityList { get; }

        /// <summary>
        /// Map resource to aggregated allocated quantity across all requirement
        /// </summary>
        IReadOnlyDictionary<IResource, double> ResourceQuantityAggregated { get; }
    }
}
