using O2DESNet.RCQueues.Common;
using O2DESNet.RCQueues.Interfaces;

using System.Collections.Generic;

namespace O2DESNet.RCQueues
{
    public class ReadOnlyAllocation : IAllocation
    {
        private readonly Allocation _allocation;

        public IReadOnlyDictionary<IRequirement, IReadOnlyList<ResourceQuantity>> RequirementResourceQuantityList => _allocation.RequirementResourceQuantityList;

        public IReadOnlyDictionary<IResource, double> ResourceQuantityAggregated => _allocation.ResourceQuantityAggregated;

        internal ReadOnlyAllocation(Allocation allocation)
        {
            _allocation = allocation;
        }
    }
}
