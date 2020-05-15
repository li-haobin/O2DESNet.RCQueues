using System.Collections.Generic;

namespace O2DESNet.RCQueues
{
    public interface IRequirement
    {
        /// <summary>
        /// The requirement can be fit by any one or many resources in the pool
        /// </summary>
        IEnumerable<IResource> Pool { get; }
        /// <summary>
        /// The total quantity of the requirement need to be satisfied
        /// </summary>
        double Quantity { get; }
        /// <summary>
        /// Check if a specified resource is fit to the requirement
        /// </summary>
        bool IsFit(IResource resource);
    }
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
