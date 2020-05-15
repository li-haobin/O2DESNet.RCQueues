using System;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IResource
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
        /// Gets the capacity.
        /// </summary>
        double Capacity { get; }
    }
}
