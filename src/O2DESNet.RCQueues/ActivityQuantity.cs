using O2DESNet.RCQueues.Interfaces;

namespace O2DESNet.RCQueues
{

    public struct ActivityQuantity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityQuantity" /> struct.
        /// </summary>
        /// <param name="activity">The activity.</param>
        /// <param name="quantity">The quantity.</param>
        public ActivityQuantity(IActivity activity, double quantity)
        {
            Activity = activity;
            Quantity = quantity;
        }

        /// <summary>
        /// Gets the activity.
        /// </summary>
        public IActivity Activity { get; }

        /// <summary>
        /// Gets the quantity.
        /// </summary>
        public double Quantity { get; }
    }
}
