using O2DESNet.RCQueues.Interfaces;

using System.Collections.Generic;

namespace O2DESNet.RCQueues
{
    public class Requirement : IRequirement
    {
        private HashSet<IResource> Pool_HashSet;

        public IEnumerable<IResource> Pool
        {
            get { return Pool_HashSet; }
            set { Pool_HashSet = new HashSet<IResource>(value); }
        }

        public double Quantity { get; set; }
        public bool IsFit(IResource resource)
        {
            return Pool_HashSet.Contains(resource);
        }
    }
}
