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
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHcPending { get; }
        /// <summary>
        /// Gets the activity HC active.
        /// </summary>
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHcActive { get; }
        /// <summary>
        /// Gets the activity HC passive.
        /// </summary>
        IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> ActivityHcPassive { get; }
        /// <summary>
        /// Gets the resource HC pending.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcPending { get; }
        /// <summary>
        /// Gets the resource HC active.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcActive { get; }
        /// <summary>
        /// Gets the resource HC passive.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcPassive { get; }
        /// <summary>
        /// Gets the resource HC occupied.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcOccupied { get; }
        /// <summary>
        /// Gets the resource HC available.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcAvailable { get; }
        /// <summary>
        /// Gets the resource HC dynamic capacity.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcDynamicCapacity { get; }
        /// <summary>
        /// Gets the resource HC pending lock active.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcPendingLockActive { get; }
        /// <summary>
        /// Gets the resource HC pending lock passive.
        /// </summary>
        IReadOnlyDictionary<IResource, ReadOnlyHourCounter> ResourceHcPendingLockPassive { get; }
        /// <summary>
        /// Gets the resource activity HC pending.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHcPending { get; }
        /// <summary>
        /// Gets the resource activity HC active.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHcActive { get; }
        /// <summary>
        /// Gets the resource activity HC passive.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHcPassive { get; }
        /// <summary>
        /// Gets the resource activity HC occupied.
        /// </summary>
        IReadOnlyDictionary<IResource, IReadOnlyDictionary<IActivity, ReadOnlyHourCounter>> ResourceActivityHcOccupied { get; }
    }
}
