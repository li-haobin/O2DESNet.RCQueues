using O2DESNet.RCQueues.Common;
using O2DESNet.RCQueues.Interfaces;

using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public class Allocation : IAllocation
    {
        private ReadOnlyAllocation _readOnly = null;

        public IReadOnlyDictionary<IRequirement, IReadOnlyList<ResourceQuantity>> RequirementResourceQuantityList => _rqmtResQttListDict.AsReadOnly();

        private readonly Dictionary<IRequirement, List<ResourceQuantity>> _rqmtResQttListDict = new Dictionary<IRequirement, List<ResourceQuantity>>();

        public IReadOnlyDictionary<IResource, double> ResourceQuantityAggregated =>
            _rqmtResQttListDict.Values.SelectMany(list => list).GroupBy(t => t.Resource)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity)).AsReadOnly(d => d);

        /// <summary>
        /// Add the mapping from requirement to the list of resource-quantity tuples allocated to it
        /// </summary>
        public void Add(IRequirement key, IEnumerable<ResourceQuantity> value)
        {
            _rqmtResQttListDict.Add(key, value.ToList());
        }

        public ReadOnlyAllocation AsReadOnly()
        {
            if (_readOnly == null) _readOnly = new ReadOnlyAllocation(this);
            return _readOnly;
        }
    }
}
