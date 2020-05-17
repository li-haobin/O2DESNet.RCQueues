using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IStatistics
    {
        /// <summary>
        /// Gets the count of loads entered.
        /// </summary>
        int CountOfLoadsEntered { get; }
        /// <summary>
        /// Gets the count of loads processing.
        /// </summary>
        int CountOfLoadsProcessing { get; }
        /// <summary>
        /// Gets the count of loads exited.
        /// </summary>
        int CountOfLoadsExited { get; }
        /// <summary>
        /// HourCounters by Activity for number of pending jobs (i.e., batches)
        /// </summary>
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHourCounterPending { get; }
        /// <summary>
        /// Gets the activity HC active.
        /// </summary>
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHourCounterActive { get; }
        /// <summary>
        /// Gets the activity HC passive.
        /// </summary>
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHourCounterPassive { get; }
        /// <summary>
        /// Gets the resource HC pending.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPending { get; }
        /// <summary>
        /// Gets the resource HC active.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterActive { get; }
        /// <summary>
        /// Gets the resource HC passive.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPassive { get; }
        /// <summary>
        /// Gets the resource HC occupied.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterOccupied { get; }
        /// <summary>
        /// Gets the resource HC available.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterAvailable { get; }
        /// <summary>
        /// Gets the resource HC dynamic capacity.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterDynamicCapacity { get; }
        /// <summary>
        /// Gets the resource HC pending lock active.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPendingLockActive { get; }
        /// <summary>
        /// Gets the resource HC pending lock passive.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHourCounterPendingLockPassive { get; }
        /// <summary>
        /// Gets the resource activity HC pending.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterPending { get; }
        /// <summary>
        /// Gets the resource activity HC active.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterActive { get; }
        /// <summary>
        /// Gets the resource activity HC passive.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterPassive { get; }
        /// <summary>
        /// Gets the resource activity HC occupied.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHourCounterOccupied { get; }
    }
}
