using O2DESNet.RCQueues.Interfaces;

namespace O2DESNet.RCQueues.Common
{

    public struct ResourceQuantity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceQuantity"/> struct.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="quantity">The quantity.</param>
        public ResourceQuantity(IResource resource, double quantity)
        {
            Resource = resource;
            Quantity = quantity;
        }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        public IResource Resource { get; }

        /// <summary>
        /// Gets the quantity.
        /// </summary>
        public double Quantity { get; }
    }
}
