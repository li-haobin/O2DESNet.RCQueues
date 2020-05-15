using O2DESNet.RCQueues.Common;
using O2DESNet.RCQueues.Interfaces;
using O2DESNet.Standard;

using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public class Batch : HashSet<ILoad>, IBatch
    {
        private BatchPhase _phase = BatchPhase.Batching;

        public IActivity Activity { get; set; }

        public BatchPhase Phase
        {
            get { return _phase; }
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

        public Batch() : base() { }
        public override string ToString()
        {
            return string.Format("({0})", string.Join(",", this.OrderBy(l => l.Index)));
        }
    }    
}
