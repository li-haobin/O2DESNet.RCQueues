using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace O2DESNet.RCQueues.UnitTest
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

            if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
        }

        [Test]
        public void Read_SimpleRCQ_From_CSVs_Seasonality()
        {
            var sim = SimpleRCQs_Samples.Sample1s().Sandbox();
            sim.Run(TimeSpan.FromDays(7));
            if (sim.RCQsModel.AllLoads.Count > 29) Assert.Fail("Need to check if the RCQ is stationary.");
            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
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

            if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
        }

        [Test]
        public void Configure_SimpleRCQ_by_Methods_2()
        {
            var simpleRCQs = new SimpleRCQs.Statics();
            simpleRCQs.AddResource("Res1", 10);
            simpleRCQs.AddResource("Res2", 10);
            simpleRCQs.AddActivity("Act1", rs => TimeSpan.FromMinutes(rs.NextDouble() * 5), new List<(string, double)>
            {
                ("Res1", 3),
                ("Res2", 1),
            });
            simpleRCQs.AddActivity("Act2", rs => TimeSpan.FromMinutes(rs.NextDouble() * 10), new List<(string, double)>
            {
                ("Res1", 1),
                ("Res2", 2),
            });
            simpleRCQs.AddSucceeding("Act1", "Act2", 1);
            simpleRCQs.Generator.MeanHourlyRate = 4;

            var sim = simpleRCQs.Sandbox();
            sim.Run(TimeSpan.FromDays(1));

            sim.RCQsModel.Output_Statistics_CSVs();

            if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
        }
    }
}
