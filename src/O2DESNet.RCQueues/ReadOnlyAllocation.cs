using O2DESNet.RCQueues.Common;
using O2DESNet.RCQueues.Interfaces;

using System.Collections.Generic;

namespace O2DESNet.RCQueues
{
    public class ReadOnlyAllocation : IAllocation
    {
        private readonly Allocation _allocation;

        public IReadOnlyDictionary<IRequirement, IReadOnlyList<ResourceQuantity>> Requirement_ResourceQuantityList => _allocation.Requirement_ResourceQuantityList;

        public IReadOnlyDictionary<IResource, double> ResourceQuantity_Aggregated => _allocation.ResourceQuantity_Aggregated;

        internal ReadOnlyAllocation(Allocation allocation)
        {
            _allocation = allocation;
        }
    }
}
