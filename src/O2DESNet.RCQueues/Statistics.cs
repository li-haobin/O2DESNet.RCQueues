using System;
using System.IO;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public static partial class Statistics
    {
        /// <summary>
        /// Output statistics of stationary analysis
        /// </summary>        
        public static void Output_Statistics_CSVs(this RCQueuesModel rcq, string dir = null)
        {
            if (dir == null) dir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var sw = new StreamWriter(dir + "\\Statistics_Activities.csv"))
            {
                sw.Write("Activity Id,Avg.# Pending,Avg.# Active,Avg.# Passive,Avg.Hrs Pending,Avg.Hrs Active,Avg.Hrs Passive,");
                sw.WriteLine("Ttl.# Arrived,Ttl.# Processing,Ttl.# Processed,");
                foreach (var act in rcq.AllActivities)
                {
                    sw.Write("{0},", act.Name);
                    sw.Write("{0},", rcq.ActivityHourCounterPending[act].AverageCount);
                    sw.Write("{0},", rcq.ActivityHourCounterActive[act].AverageCount);
                    sw.Write("{0},", rcq.ActivityHourCounterPassive[act].AverageCount);
                    sw.Write("{0},", rcq.ActivityHourCounterPending[act].AverageDuration.TotalHours);
                    sw.Write("{0},", rcq.ActivityHourCounterActive[act].AverageDuration.TotalHours);
                    sw.Write("{0},", rcq.ActivityHourCounterPassive[act].AverageDuration.TotalHours);
                    sw.Write("{0},", rcq.ActivityHourCounterActive[act].TotalIncrement);
                    sw.Write("{0},", rcq.ActivityHourCounterActive[act].TotalIncrement - rcq.ActivityHourCounterPassive[act].TotalDecrement);
                    sw.Write("{0},", rcq.ActivityHourCounterPassive[act].TotalDecrement);
                    sw.WriteLine();
                }
            }
            
            using (var sw = new StreamWriter(dir + "\\Statistics_Resources.csv"))
            {
                var head = "Res.Id,Capacity,Avg.Amt. Available,Avg.Amt. Pending,Avg.Amt. Active,Avg.Amt. Passive,Util.";
                if (rcq.Assets.Activities.Count == 0) sw.WriteLine(head);
                foreach (var res in rcq.Assets.Resources)
                {
                    if (rcq.Assets.Activities.Count > 0) sw.WriteLine(head);

                    sw.Write("{0},", res.Name);
                    sw.Write("{0},", res.Capacity);
                    sw.Write("{0},", rcq.ResourceHourCounterAvailable[res].AverageCount);
                    sw.Write("{0},", rcq.ResourceHourCounterPending[res].AverageCount);
                    sw.Write("{0},", rcq.ResourceHourCounterActive[res].AverageCount);
                    sw.Write("{0},", rcq.ResourceHourCounterPassive[res].AverageCount);
                    sw.Write("{0},", (rcq.ResourceHourCounterActive[res].AverageCount + rcq.ResourceHourCounterPassive[res].AverageCount)
                        / rcq.ResourceHourCounterAvailable[res].AverageCount);
                    sw.WriteLine();
                    if (rcq.Assets.Activities.Count > 0)
                    {
                        sw.WriteLine("Act.Id,------ by Activities ------");
                        foreach (var act in rcq.ResourceToActivities[res])
                        {
                            sw.Write("{0},", act.Id);
                            sw.Write("{0},", res.Capacity);
                            sw.Write("{0},", rcq.ResourceHourCounterAvailable[res].AverageCount);
                            sw.Write("{0},", rcq.ResourceActivityHourCounterPending[res][act].AverageCount);
                            sw.Write("{0},", rcq.ResourceActivityHourCounterActive[res][act].AverageCount);
                            sw.Write("{0},", rcq.ResourceActivityHourCounterPassive[res][act].AverageCount);
                            sw.Write("{0},", (rcq.ResourceActivityHourCounterActive[res][act].AverageCount 
                                + rcq.ResourceActivityHourCounterPassive[res][act].AverageCount)
                                / rcq.ResourceHourCounterAvailable[res].AverageCount);
                            sw.WriteLine();
                        }
                        sw.WriteLine();
                    }
                }
            }
        }
        /// <summary>
        /// Output statistics on the snapshot
        /// </summary>
        public static void Output_Snapshot_CSVs(this RCQueuesModel rcq, DateTime clockTime, string dir = null)
        {
            if (dir == null) dir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var strClockTime = clockTime.ToString("yyyy-MM-dd-HH-mm-ss");
            using (var sw = new StreamWriter($"{dir}\\Snapshot_{strClockTime}.csv"))
            {
                sw.WriteLine("Load,Activity,Resource,Quantity,Type");
                foreach(var load in rcq.AllLoads)
                {
                    var pending = rcq.LoadToBatchMovingTo[load] != null; /// MoveTos[load] is set to null if the load is processed in activity
                    var batch = rcq.LoadToBatchCurrent[load];
                    foreach (var i in rcq.BatchToAllocation[batch].ResourceQuantityAggregated)
                    {
                        var res = i.Key;
                        var qtt = i.Value;
                        sw.WriteLine("{0},{1},{2},{3},{4}", load, batch.Activity, res, qtt, pending ? "passive" : "active");
                    }
                    if (pending)
                    {
                        batch = rcq.LoadToBatchMovingTo[load];
                        foreach (var res in rcq.ActivityToResources[batch.Activity])
                        {
                            var qtt = batch.Activity.Requirements.Where(req => req.Pool.Contains(res)).Sum(req => req.Quantity);
                            sw.WriteLine("{0},{1},{2},{3},{4}", load, batch.Activity, res, qtt, "pending");
                        }
                    }
                }

                sw.WriteLine();
                sw.WriteLine(",# of Loads");
                sw.WriteLine("Arrived,{0}", rcq.CountOfLoadsEntered);
                sw.WriteLine("Processing,{0}", rcq.CountOfLoadsProcessing);
                sw.WriteLine("Processed,{0}", rcq.CountOfLoadsExited);
            }
        }
    }
}
