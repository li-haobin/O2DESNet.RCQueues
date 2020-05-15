using O2DESNet.RCQueues.Interfaces;

using System;

namespace O2DESNet.RCQueues
{
    /// <summary>
    /// Default Resource class
    /// </summary>
    public class Resource : IResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Resource"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Resource(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Resource"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        public Resource(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the capacity.
        /// </summary>
        public double Capacity { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{GetType().Name}#{Id}";
        }
    }
}
