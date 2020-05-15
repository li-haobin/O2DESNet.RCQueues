using O2DESNet.RCQueues.Common;
using O2DESNet.RCQueues.Interfaces;

using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public class Allocation : IAllocation
    {
        private ReadOnlyAllocation _readOnly = null;

        public IReadOnlyDictionary<IRequirement, IReadOnlyList<ResourceQuantity>> Requirement_ResourceQuantityList => Rqmt_ResQttList_Dict.AsReadOnly();

        private readonly Dictionary<IRequirement, List<ResourceQuantity>> Rqmt_ResQttList_Dict = new Dictionary<IRequirement, List<ResourceQuantity>>();

        public IReadOnlyDictionary<IResource, double> ResourceQuantity_Aggregated =>
            Rqmt_ResQttList_Dict.Values.SelectMany(list => list).GroupBy(t => t.Resource)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity)).AsReadOnly(d => d);

        /// <summary>
        /// Add the mapping from requirement to the list of resource-quantity tuples allocated to it
        /// </summary>
        public void Add(IRequirement key, IEnumerable<ResourceQuantity> value)
        {
            Rqmt_ResQttList_Dict.Add(key, value.ToList());
        }

        public ReadOnlyAllocation AsReadOnly()
        {
            if (_readOnly == null) _readOnly = new ReadOnlyAllocation(this);
            return _readOnly;
        }
    }
}
