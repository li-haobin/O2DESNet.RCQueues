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
        /// <summary>
        /// Provide batch ordering for resource allocation
        /// First parameter is first batch 
        /// Second parameter is next batch
        /// Result, -1 means first parameter is processed first, 1 means first parameter last
        /// </summary>
        Func<(IBatch Batch, DateTime Time), (IBatch Batch, DateTime Time), int> BatchOrder { get; set; }
        /// <summary>
        /// Activity Conditions Dictionary.
        /// The key is the load object property name.
        /// The value is the matched load property value for that activity.
        /// All conditions much be matched for processing that activity
        /// </summary>
        Dictionary<string, object> Conditions { get; set; }
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
        public Func<(IBatch Batch, DateTime Time), (IBatch Batch, DateTime Time), int> BatchOrder { get; set; } = (t1, t2) => t1.Time.CompareTo(t2.Time);
        public Dictionary<string, object> Conditions { get; set; } = new Dictionary<string, object>();
    }
}
