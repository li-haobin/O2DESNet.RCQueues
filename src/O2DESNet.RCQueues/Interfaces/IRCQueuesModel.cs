using O2DESNet.Standard;

using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IRCQueuesModel : ISandbox, IBatches, IActivities, IResources
    {
        /// <summary>
        /// Gets the assets.
        /// </summary>
        IRCQueuesModelStatics Assets { get; }

        /// <summary>
        /// Requests to enter.
        /// </summary>
        /// <param name="load">The load.</param>
        /// <param name="init">The initialize.</param>
        void RequestToEnter(ILoad load, IActivity init);

        /// <summary>
        /// Finishes the specified batch.
        /// </summary>
        /// <param name="batch">The batch.</param>
        /// <param name="next">The next.</param>
        void Finish(IBatch batch, Dictionary<ILoad, IActivity> next);

        /// <summary>
        /// Exits the specified load.
        /// </summary>
        /// <param name="load">The load.</param>
        void Exit(ILoad load);

        /// <summary>
        /// Requests to lock.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="quantity">The quantity.</param>
        void RequestToLock(IResource resource, double quantity);

        /// <summary>
        /// Requests to unlock.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="quantity">The quantity.</param>
        void RequestToUnlock(IResource resource, double quantity);

        /// <summary>
        /// Occurs when [on entered].
        /// </summary>
        event Action<ILoad, IActivity> OnEntered;
        /// <summary>
        /// Occurs when [on ready to exit].
        /// </summary>
        event Action<ILoad> OnReadyToExit;
        /// <summary>
        /// Occurs when [on locked].
        /// </summary>
        event Action<IResource, double> OnLocked;
        /// <summary>
        /// Occurs when [on unlocked].
        /// </summary>
        event Action<IResource, double> OnUnlocked;
        /// <summary>
        /// Occurs when [on started].
        /// </summary>
        event Action<IBatch> OnStarted;
    }
}
