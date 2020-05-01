using O2DESNet.Standard;
using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues
{
    public interface IActivity
    {
        string Id { get; }
        string Name { get; }
        IReadOnlyList<IRequirement> Requirements { get; }
        /// <summary>
        /// Inclusive minimum and maxsimum of the batch size
        /// </summary>
        BatchSizeRange BatchSizeRange { get; }
    }

    /// <summary>
    /// Default Activity class
    /// </summary>
    public class Activity : IActivity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IReadOnlyList<IRequirement> Requirements { get; set; }
        public BatchSizeRange BatchSizeRange { get; set; } = new BatchSizeRange();
        public Func<Random, IEnumerable<ILoad>, IAllocation, TimeSpan> Duration { get; set; }
        public Func<Random, ILoad, IActivity> Succeedings { get; set; }
    }
}
