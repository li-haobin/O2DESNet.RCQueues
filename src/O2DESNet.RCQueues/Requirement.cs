using O2DESNet.RCQueues.Interfaces;

using System.Collections.Generic;

namespace O2DESNet.RCQueues
{
    public class Requirement : IRequirement
    {
        private HashSet<IResource> _poolHashSet;

        public IEnumerable<IResource> Pool
        {
            get => _poolHashSet;
            set => _poolHashSet = new HashSet<IResource>(value);
        }

        public double Quantity { get; set; }
        public bool IsFit(IResource resource)
        {
            return _poolHashSet.Contains(resource);
        }
    }
}
