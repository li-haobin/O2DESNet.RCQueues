using O2DESNet.RCQueues.Interfaces;

using System;

namespace O2DESNet.RCQueues.Exceptions
{
    /// <summary>
    /// Batch phase not in sequence exception
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class BatchPhaseNotInSequenceException : Exception
    {
        /// <summary>
        /// Gets the batch.
        /// </summary>
        public IBatch Batch { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchPhaseNotInSequenceException"/> class.
        /// </summary>
        /// <param name="batch">The batch.</param>
        public BatchPhaseNotInSequenceException(IBatch batch) : base("Error in batch phase sequence")
        {
            Batch = batch;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchPhaseNotInSequenceException"/> class.
        /// </summary>
        /// <param name="batch">The batch.</param>
        /// <param name="message">The message.</param>
        public BatchPhaseNotInSequenceException(IBatch batch, string message) : base(message)
        {
            Batch = batch;
        }
    }
}
