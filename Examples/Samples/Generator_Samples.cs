using O2DESNet.Standard;

namespace Examples.Samples
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
