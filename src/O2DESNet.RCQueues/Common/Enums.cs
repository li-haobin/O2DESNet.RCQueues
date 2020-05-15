using System.ComponentModel;

namespace O2DESNet.RCQueues.Common
{
    /// <summary>
    /// Batch Phase Enumeration
    /// </summary>
    [DefaultValue(BatchPhase.Batching)]
    public enum BatchPhase : int
    {
        /// <summary>
        /// Batch created but yet to attempt to start due to batch size constraint
        /// </summary>
        [Description("Batching activity")]
        Batching,

        /// <summary>
        /// Attempted to start activity, however failed due to lack of resource
        /// </summary>
        [Description("Pending activity")]
        Pending,

        /// <summary>
        /// Started the activity, waiting to be completed
        /// </summary>
        [Description("Activity started")]
        Started,

        /// <summary>
        /// Completed the activity, shall be immediately switched to either Passive or Disposed
        /// by the AtmptStart event scheduled at the same time
        /// </summary>
        [Description("Activity completed")]
        Finished,

        /// <summary>
        /// Activity completed, blocked due to 
        /// 1. lack of resource for the next activity of some load
        /// 2. batch size constraint of the next activity of some load
        /// </summary>
        [Description("Activity passive")]
        Passive,

        /// <summary>
        /// All loads have proceeded to the next activity and removed
        /// </summary>
        [Description("Activity disposed")]
        Disposed,
    }
}
