using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace O2DESNet.RCQueues.UnitTest
{
    public class General_Tests
    {
        [Test]
        public void Two_Flows_with_Ten_Resources()
        {
            var assets = Batch_Processing_Samples.Sample1();
            var sim = assets.Sandbox();
            sim.Run(TimeSpan.FromDays(10));
            if (sim.RCQsModel.AllLoads.Count > 9) Assert.Fail("Need to check if the RCQ is stationary.");
            if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
        }

        [Test]
        public void Statistics_On_Activities()
        {
            foreach (var assets in new Testbed.Statics[]
            {
                General_Test_Samples.Sample1(),
                new Testbed.Statics(SimpleRCQs_Samples.Sample1()),
                new Testbed.Statics(SimpleRCQs_Samples.Sample2()),
            })
            {
                var sim = assets.Sandbox();
                sim.Run(TimeSpan.FromDays(1));
                var stats_activities = sim.RCQsModel.AllActivities.Select(act => new List<double>
                {
                    sim.RCQsModel.Activity_HC_Pending[act].AverageCount,
                    sim.RCQsModel.Activity_HC_Active[act].AverageCount,
                    sim.RCQsModel.Activity_HC_Passive[act].AverageCount,
                    sim.RCQsModel.Activity_HC_Pending[act].AverageDuration.TotalHours,
                    sim.RCQsModel.Activity_HC_Active[act].AverageDuration.TotalHours,
                    sim.RCQsModel.Activity_HC_Passive[act].AverageDuration.TotalHours,
                }).ToList();
                foreach (var d in stats_activities.SelectMany(l => l))
                    if (double.IsNaN(d) || d < 0) Assert.Fail();
                if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
            }
        }

        [Test]
        public void Statistics_On_Resources()
        {
            foreach (var assets in new Testbed.Statics[]
            {
                General_Test_Samples.Sample1(),
                new Testbed.Statics(SimpleRCQs_Samples.Sample1()),
                new Testbed.Statics(SimpleRCQs_Samples.Sample2()),
            })
            {
                var sim = assets.Sandbox();
                sim.Run(TimeSpan.FromDays(1));
                var stats_resources = sim.RCQsModel.Assets.Resources.Select(res => new List<double>
                {
                    sim.RCQsModel.Resource_HC_Pending[res].AverageCount,
                    sim.RCQsModel.Resource_HC_Active[res].AverageCount,
                    sim.RCQsModel.Resource_HC_Passive[res].AverageCount,
                }).ToList();
                foreach (var d in stats_resources.SelectMany(l => l))
                    if (double.IsNaN(d) || d < 0) Assert.Fail();
                if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
            }
        }

        [Test]
        public void Generate_Ouput_Statistics()
        {
            foreach (var assets in new Testbed.Statics[]
            {
                General_Test_Samples.Sample1(),
                new Testbed.Statics(SimpleRCQs_Samples.Sample1()),
                new Testbed.Statics(SimpleRCQs_Samples.Sample2()),
            })
            {
                var sim = assets.Sandbox();
                sim.Run(TimeSpan.FromDays(1));

                var dir = Guid.NewGuid().ToString();
                var file1 = dir + "\\" + "Statistics_Activities.csv";
                var file2 = dir + "\\" + "Statistics_Resources.csv";

                sim.RCQsModel.Output_Statistics_CSVs(dir);
                if (!File.Exists(file1)) Assert.Fail();
                if (!File.Exists(file2)) Assert.Fail();

                /// clear the directory
                File.Delete(file1);
                File.Delete(file2);
                Directory.Delete(dir);

                if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
            }
        }

        [Test]
        public void Generate_Output_Snapshot()
        {
            foreach (var assets in new Testbed.Statics[]
            {
                General_Test_Samples.Sample1(),
                new Testbed.Statics(SimpleRCQs_Samples.Sample1()),
                new Testbed.Statics(SimpleRCQs_Samples.Sample2()),
            })
            {
                var sim = assets.Sandbox();
                var dir = Guid.NewGuid().ToString();

                for (int i = 0; i < 5; i++)
                {
                    sim.Run(TimeSpan.FromHours(2));
                    sim.RCQsModel.Output_Snapshot_CSVs(sim.ClockTime, dir);
                }

                foreach (var file in Directory.GetFiles(dir))
                {
                    /// clear the directory
                    File.Delete(file);
                }
                Directory.Delete(dir);

                if (sim.RCQsModel.CountLoads_Exited == 0) Assert.Fail();
            }
        }
    }
}