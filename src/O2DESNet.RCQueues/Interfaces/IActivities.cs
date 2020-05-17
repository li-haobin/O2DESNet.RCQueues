using O2DESNet.Standard;

using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IActivities
    {
        /// <summary>
        /// All activities relevant in the model
        /// </summary>
        IReadOnlyList<IActivity> AllActivities { get; }
        /// <summary>
        /// Map activity to all loads that is relevant to it, i.e., pending, or active/passive processing
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<ILoad>> ActivityToLoads { get; }
        /// <summary>
        /// Map activities to the loads that are pending for it due to insufficient resources, and the time of the pending start
        /// </summary>   
        IReadOnlyDictionary<IActivity, IReadOnlyList<(IBatch Batch, DateTime Time)>> ActivityToBatchTimesPending { get; }
        /// <summary>
        /// Map activity to the batch that is created but not met the batch size constraint
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<IBatch>> ActivityToBatchesBatching { get; }
        /// <summary>
        /// Map the activity to the list of quantified resource 
        /// This is to be formed and fixed at init function.
        /// </summary>
        IReadOnlyDictionary<IActivity, IReadOnlyList<IResource>> ActivityToResources { get; }
    }
}
