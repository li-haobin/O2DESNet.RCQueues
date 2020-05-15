using O2DESNet.RCQueues.Interfaces;
using O2DESNet.Standard;

using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues
{

    /// <summary>
    /// Default Activity class
    /// </summary>
    public class Activity : IActivity
    {
        private string _name;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Activity"/> class.
        /// </summary>
        public Activity()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Activity(string name)
        {
            Id = Guid.NewGuid();
            _name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity"/> class using specified Id.
        /// </summary>
        /// <param name="id">The specified identifier.</param>
        public Activity(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity"/> class using specified Id.
        /// </summary>
        /// <param name="id">The specified identifier.</param>
        /// <param name="name">The name.</param>
        public Activity(Guid id, string name)
        {
            Id = id;
            _name = name;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Renames the activity.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <exception cref="ArgumentNullException">Activity name cannot be blank or empty</exception>
        public void RenameActivity(string newName)
        {
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException("Activity name cannot be blank or empty");

            _name = newName;
        }

        /// <summary>
        /// Gets or sets the requirements.
        /// </summary>
        public IReadOnlyList<IRequirement> Requirements { get; set; }

        /// <summary>
        /// Gets or sets the batch size range.
        /// </summary>
        public BatchSizeRange BatchSizeRange { get; set; } = new BatchSizeRange();

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public Func<Random, IEnumerable<ILoad>, IAllocation, TimeSpan> Duration { get; set; }

        /// <summary>
        /// Gets or sets the succeeding.
        /// </summary>
        public Func<Random, ILoad, IActivity> Succeedings { get; set; } 
        #endregion

    }
}
