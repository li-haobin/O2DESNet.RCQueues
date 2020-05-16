using System.Collections.Generic;

namespace O2DESNet.RCQueues.Interfaces
{
    public interface IRCQueuesModelStatics : IAssets
    {
        IReadOnlyList<IResource> Resources { get; }
        IReadOnlyList<IActivity> Activities { get; }        
    }
}
