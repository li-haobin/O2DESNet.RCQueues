namespace O2DESNet.RCQueues
{
    public class BatchSizeRange
    {
        public int Min { get; private set; } = 1;
        public int Max { get; private set; } = 1;
        public BatchSizeRange() { }
        public BatchSizeRange(int min, int max) { Min = min; Max = max; }
    }
}
