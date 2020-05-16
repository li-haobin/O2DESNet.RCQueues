using O2DESNet.RCQueues.Common;
using O2DESNet.RCQueues.Exceptions;
using O2DESNet.RCQueues.Interfaces;
using O2DESNet.Standard;

using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public class Batch : HashSet<ILoad>, IBatch
    {
        private BatchPhase _phase = BatchPhase.Batching;

        /// <summary>
        /// Gets or sets the activity.
        /// </summary>
        public IActivity Activity { get; set; }

        /// <summary>
        /// Gets or sets the phase.
        /// </summary>
        /// <exception cref="O2DESNet.RCQueues.Exceptions.BatchPhaseNotInSequenceException"></exception>
        public BatchPhase Phase
        {
            get => _phase;
            set
            {
                if ((value == BatchPhase.Batching) ||
                    (value == BatchPhase.Started && _phase != BatchPhase.Batching && _phase != BatchPhase.Pending) ||
                    (value == BatchPhase.Finished && _phase != BatchPhase.Started) ||
                    (value == BatchPhase.Passive && _phase != BatchPhase.Finished) ||
                    (value == BatchPhase.Pending && _phase != BatchPhase.Batching) ||
                    (value == BatchPhase.Disposed && _phase != BatchPhase.Finished && _phase != BatchPhase.Passive))
                    throw new BatchPhaseNotInSequenceException(this);
                _phase = value;
            }
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"({string.Join(",", this.OrderBy(l => l.Index))})";
    }    
}
