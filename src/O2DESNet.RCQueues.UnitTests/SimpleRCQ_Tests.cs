using NUnit.Framework;

using O2DESNet.RCQueues.UnitTests.Samples;

using System;
using System.Collections.Generic;
using System.IO;

namespace O2DESNet.RCQueues.UnitTests
{
    public class SimpleRCQ_Tests
    {
        [Test]
        public void Write_Template_CSVs_at_Current_Directory()
        {
            var file1 = SimpleRCQs.Statics.Key.Resources + ".csv";
            var file2 = SimpleRCQs.Statics.Key.Activities + ".csv";
            var file3 = SimpleRCQs.Statics.Key.Arrivals + ".csv";
            SimpleRCQs.Statics.WriteTemplateCSVs();
            if (!File.Exists(file1)) Assert.Fail();
            if (!File.Exists(file2)) Assert.Fail();
            if (!File.Exists(file3)) Assert.Fail();
            /// clear the directory
            File.Delete(file1);
            File.Delete(file2);
            File.Delete(file3);
        }

        [Test]
        public void Write_Template_CSVs_at_Any_Directory()
        {
            var s1 = Guid.NewGuid().ToString();
            var s2 = Guid.NewGuid().ToString();
            var dir = s1 + "\\" + s2;
            var file1 = dir + "\\" + SimpleRCQs.Statics.Key.Resources + ".csv";
            var file2 = dir + "\\" + SimpleRCQs.Statics.Key.Activities + ".csv";
            var file3 = dir + "\\" + SimpleRCQs.Statics.Key.Arrivals + ".csv";
            SimpleRCQs.Statics.WriteTemplateCSVs(dir);
            if (!File.Exists(file1)) Assert.Fail();
            if (!File.Exists(file2)) Assert.Fail();
            if (!File.Exists(file3)) Assert.Fail();
            /// clear the directory
            File.Delete(file1);
            File.Delete(file2);
            File.Delete(file3);
            Directory.Delete(s1 + "\\" + s2);
            Directory.Delete(s1);
        }

        [Test]
        public void Read_SimpleRCQ_From_CSVs()
        {
            var sim = SimpleRCQs_Samples.Sample1().Sandbox();
            sim.Run(TimeSpan.FromDays(7));
            if (sim.RCQsModel.AllLoads.Count > 29) Assert.Fail("Need to check if the RCQ is stationary.");
            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountOfLoadsExited == 0) Assert.Fail();
        }

        [Test]
        public void Read_SimpleRCQ_From_CSVs_Seasonality()
        {
            var sim = SimpleRCQs_Samples.Sample1s().Sandbox();
            sim.Run(TimeSpan.FromDays(7));
            if (sim.RCQsModel.AllLoads.Count > 29) Assert.Fail("Need to check if the RCQ is stationary.");
            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountOfLoadsExited == 0) Assert.Fail();
        }

        [Test]
        public void Configure_SimpleRCQ_by_Methods()
        {
            var sim = SimpleRCQs_Samples.Sample2().Sandbox();
            //sim.LogFile = "log.txt";
            //sim.RCQueuesModel.LogFile = "log.txt";
            sim.Run(TimeSpan.FromDays(1));
            if (sim.RCQsModel.AllLoads.Count > 2) Assert.Fail("Need to check if the RCQ is stationary.");
            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountOfLoadsExited == 0) Assert.Fail();
        }

        [Test]
        public void Configure_SimpleRCQ_by_Methods_2()
        {
            var res1 = Guid.NewGuid();
            var res2 = Guid.NewGuid();

            var act1Id = Guid.NewGuid();
            var act2Id = Guid.NewGuid();

            var simpleRCQs = new SimpleRCQs.Statics();

            simpleRCQs.AddResource(res1, "Res1", 10, string.Empty);
            simpleRCQs.AddResource(res2, "Res2", 10, string.Empty);
            simpleRCQs.AddActivity(act1Id, "Act1",
                rs => TimeSpan.FromMinutes(rs.NextDouble() * 5),
                new List<(Guid, double)>
                {
                    (res1, 3),
                    (res2, 1),
                }, null);
            simpleRCQs.AddActivity(act2Id, "Act2",
                rs => TimeSpan.FromMinutes(rs.NextDouble() * 10),
                new List<(Guid, double)>
                {
                    (res1, 1),
                    (res2, 2),
                }, null);

            simpleRCQs.AddSucceeding(act1Id, act2Id, 1);
            simpleRCQs.Generator.MeanHourlyRate = 4;

            var sim = simpleRCQs.Sandbox();
            sim.Run(TimeSpan.FromDays(1));

            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountOfLoadsExited == 0) Assert.Fail();
        }
    }
}
