using O2DESNet.RCQueues.Common;
using O2DESNet.Standard;

using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IBatch : ICollection<ILoad>
    {
        /// <summary>
        /// Gets the activity.
        /// </summary>
        IActivity Activity { get; }

        /// <summary>
        /// Gets the phase.
        /// </summary>
        BatchPhase Phase { get; }
    }
}
