using O2DESNet.Standard;

namespace O2DESNet.RCQueues.UnitTest
{
    static class Generator_Samples
    {
        internal static PatternGenerator.Statics NoSeasonality()
        {
            return new PatternGenerator.Statics
            {
                MeanHourlyRate = 1,
            };
        }
    }
}
