using NUnit.Framework;

using O2DESNet.RCQueues.UnitTests.Samples;

using System;
using System.Diagnostics;

namespace O2DESNet.RCQueues.UnitTests
{
    public class BatchProcessing_Tests
    {
        [Test]
        public void Two_Flows_with_Ten_Resources()
        {
            var assets = Batch_Processing_Samples.Sample1();
            var sim = assets.Sandbox();            
            //state.Display = true;
            while (sim.ClockTime < DateTime.MinValue.AddDays(10))
            {
                if (sim.DebugMode) Debug.WriteLine(sim.HeadEventTime);
                sim.Run(1);
                if (sim.DebugMode) Debug.WriteLine(sim.ToString());
            }
            if (sim.RCQsModel.AllLoads.Count > 9) Assert.Fail("Need to check if the RCQ is stationary.");

            sim.RCQsModel.Output_Statistics_CSVs();
            sim.RCQsModel.Output_Snapshot_CSVs(sim.ClockTime);
            if (sim.RCQsModel.CountOfLoadsExited == 0) Assert.Fail();
        }
    }
}
