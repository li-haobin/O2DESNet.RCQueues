namespace O2DESNet.RCQueues
{
    public interface IResource
    {
        string Id { get; }
        int Index { get; }
        double Capacity { get; }
    }

    /// <summary>
    /// Default Resource class
    /// </summary>
    public class Resource : IResource
    {
        private static int _count = 0;
        public int Index { get; private set; } = _count++;
        public string Id { get; set; }
        public string Description { get; set; }
        public double Capacity { get; set; }
        public override string ToString()
        {
            var str = Id;
            if (str == null || str.Length == 0) str = string.Format("{0}#{1}", GetType().Name, Index);
            return str;
        }
    }
}
