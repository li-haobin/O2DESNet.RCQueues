using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues.UnitTest
{
    public class Modular_Integration_Tests
    {
        [Test]
        public void CallToEnter_and_OnEntered()
        {
            var assets = Batch_Processing_Samples.Sample1();
            var sim = assets.Sandbox();

            while (sim.ClockTime < new DateTime(1, 1, 2))
            {
                var t = sim.HeadEventTime;
                while (t == sim.HeadEventTime) sim.Run(1);
            }
            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
        }

        [Test]
        public void ResourceCycle()
        {
            var assets = Batch_Processing_Samples.Sample1();
            assets.ResourceCycles = new Dictionary<IResource, (TimeSpan MTTF, TimeSpan MTTR)>
            {
                { assets.RCQueuesModel.Resources[0], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[1], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[2], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[3], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[4], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[5], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[6], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[7], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[8], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
                { assets.RCQueuesModel.Resources[9], (TimeSpan.FromHours(1), TimeSpan.FromMinutes(2)) },
            };
            var sim = assets.Sandbox();
            while (sim.ClockTime < new DateTime(1, 1, 2))
            {
                var t = sim.HeadEventTime;
                while (t == sim.HeadEventTime) sim.Run(1);
            }
            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
        }
    }
}
