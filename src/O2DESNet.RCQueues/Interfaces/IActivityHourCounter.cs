using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IActivityHourCounter : IReadOnlyDictionary<IActivity, ReadOnlyHourCounter> { }
}
