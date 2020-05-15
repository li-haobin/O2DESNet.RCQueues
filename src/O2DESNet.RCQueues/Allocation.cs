using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    /// <summary>
    /// Describes the resource allocation for a batch
    /// </summary>
    public interface IAllocation
    {
        /// <summary>
        /// Map requirement to list of resource-quantity tuples allocated for it
        /// </summary>
        IReadOnlyDictionary<IRequirement, IReadOnlyList<(IResource, double)>> Requirement_ResourceQuantityList { get; }
        /// <summary>
        /// Map resource to aggregated allocated quantity across all requirement
        /// </summary>
        IReadOnlyDictionary<IResource, double> ResourceQuantity_Aggregated { get; }
    }
    public class ReadOnlyAllocation : IAllocation
    {
        public IReadOnlyDictionary<IRequirement, IReadOnlyList<(IResource, double)>> Requirement_ResourceQuantityList
        {
            get { return Allocation.Requirement_ResourceQuantityList; }
        }
        public IReadOnlyDictionary<IResource, double> ResourceQuantity_Aggregated { get { return Allocation.ResourceQuantity_Aggregated; } }

        private readonly Allocation Allocation;
        internal ReadOnlyAllocation(Allocation allocation) { Allocation = allocation; }
    }
    public class Allocation : IAllocation
    {
        public IReadOnlyDictionary<IRequirement, IReadOnlyList<(IResource, double)>> Requirement_ResourceQuantityList { get { return Rqmt_ResQttList_Dict.AsReadOnly(); } }
        private readonly Dictionary<IRequirement, List<(IResource, double)>> Rqmt_ResQttList_Dict = new Dictionary<IRequirement, List<(IResource, double)>>();
        public IReadOnlyDictionary<IResource, double> ResourceQuantity_Aggregated 
        {
            get
            {
                return Rqmt_ResQttList_Dict.Values.SelectMany(list => list).GroupBy(t => t.Item1)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Item2)).AsReadOnly(d => d);
            }
        }
        /// <summary>
        /// Add the mapping from requirement to the list of resource-quantity tuples allocated to it
        /// </summary>
        public void Add(IRequirement key, IEnumerable<(IResource, double)> value)
        {
            Rqmt_ResQttList_Dict.Add(key, value.ToList());
        }

        private ReadOnlyAllocation ReadOnly = null;
        public ReadOnlyAllocation AsReadOnly()
        {
            if (ReadOnly == null) ReadOnly = new ReadOnlyAllocation(this);
            return ReadOnly;
        }
    }
}
