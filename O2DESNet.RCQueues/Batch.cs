using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public interface IBatch : ICollection<ILoad>
    {
        IActivity Activity { get; }
        BatchPhase Phase { get; }
    }
    public class Batch : HashSet<ILoad>, IBatch
    {
        public IActivity Activity { get; set; }

        private BatchPhase _phase = BatchPhase.Batching;
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
    public enum BatchPhase
    {
        /// <summary>
        /// Batch created but yet to attempt to start due to batch size constraint
        /// </summary>
        Batching,
        /// <summary>
        /// Attempted to start activity, however failed due to lack of resource
        /// </summary>
        Pending,
        /// <summary>
        /// Started the activity, waiting to be completed
        /// </summary>
        Started,
        /// <summary>
        /// Completed the activity, shall be immediately switched to either Passive or Disposed
        /// by the AtmptStart event scheduled at the same time
        /// </summary>
        Finished,
        /// <summary>
        /// Activity completed, blocked due to 
        /// 1. lack of resource for the next activity of some load
        /// 2. batch size constraint of the next activity of some load
        /// </summary>
        Passive,
        /// <summary>
        /// All loads have proceeded to the next activity and removed
        /// </summary>
        Disposed,
    }
    public class BatchPhaseNotInSequenceException : Exception
    {
        public IBatch Batch { get; private set; }
        public BatchPhaseNotInSequenceException(IBatch batch) : base("Error in batch phase sequence")
        {
            Batch = batch;
        }
    }
    public class BatchSizeRange
    {
        public int Min { get; private set; } = 1;
        public int Max { get; private set; } = 1;
        public BatchSizeRange() { }
        public BatchSizeRange(int min, int max) { Min = min; Max = max; }
    }
}
