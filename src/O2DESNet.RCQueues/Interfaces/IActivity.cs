using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IActivity
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Renames the activity.
        /// </summary>
        /// <param name="newName">The new name.</param>
        void RenameActivity(string newName);

        /// <summary>
        /// Gets the requirements.
        /// </summary>
        IReadOnlyList<IRequirement> Requirements { get; }

        /// <summary>
        /// Inclusive minimum and maximum of the batch size
        /// </summary>
        BatchSizeRange BatchSizeRange { get; }
    }
}
