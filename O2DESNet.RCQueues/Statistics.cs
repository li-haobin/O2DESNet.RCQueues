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
        public static void Output_Statistics_CSVs(this RCQsModel rcq, string dir = null)
        {
            if (dir == null) dir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var sw = new StreamWriter(dir + "\\Statistics_Activities.csv"))
            {
                sw.Write("Activity Id,Avg.# Pending,Avg.# Active,Avg.# Passive,Avg.Hrs Pending,Avg.Hrs Active,Avg.Hrs Passive,");
                sw.WriteLine("Ttl.# Arrived,Ttl.# Processing,Ttl.# Processed,");
                foreach (var act in rcq.AllActivities)
                {
                    sw.Write("{0},", act.Id);
                    sw.Write("{0},", rcq.ActivityHC_Pending[act].AverageCount);
                    sw.Write("{0},", rcq.ActivityHC_Active[act].AverageCount);
                    sw.Write("{0},", rcq.ActivityHC_Passive[act].AverageCount);
                    sw.Write("{0},", rcq.ActivityHC_Pending[act].AverageDuration.TotalHours);
                    sw.Write("{0},", rcq.ActivityHC_Active[act].AverageDuration.TotalHours);
                    sw.Write("{0},", rcq.ActivityHC_Passive[act].AverageDuration.TotalHours);
                    sw.Write("{0},", rcq.ActivityHC_Active[act].TotalIncrement);
                    sw.Write("{0},", rcq.ActivityHC_Active[act].TotalIncrement - rcq.ActivityHC_Passive[act].TotalDecrement);
                    sw.Write("{0},", rcq.ActivityHC_Passive[act].TotalDecrement);
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

                    sw.Write("{0},", res.Id);
                    sw.Write("{0},", res.Capacity);
                    sw.Write("{0},", rcq.ResourceHC_Available[res].AverageCount);
                    sw.Write("{0},", rcq.ResourceHC_Pending[res].AverageCount);
                    sw.Write("{0},", rcq.ResourceHC_Active[res].AverageCount);
                    sw.Write("{0},", rcq.ResourceHC_Passive[res].AverageCount);
                    sw.Write("{0},", (rcq.ResourceHC_Active[res].AverageCount + rcq.ResourceHC_Passive[res].AverageCount)
                        / rcq.ResourceHC_Available[res].AverageCount);
                    sw.WriteLine();
                    if (rcq.Assets.Activities.Count > 0)
                    {
                        sw.WriteLine("Act.Id,------ by Activities ------");
                        foreach (var act in rcq.ResourceToActivities[res])
                        {
                            sw.Write("{0},", act.Id);
                            sw.Write("{0},", res.Capacity);
                            sw.Write("{0},", rcq.ResourceHC_Available[res].AverageCount);
                            sw.Write("{0},", rcq.ResourceActivityHC_Pending[res][act].AverageCount);
                            sw.Write("{0},", rcq.ResourceActivityHC_Active[res][act].AverageCount);
                            sw.Write("{0},", rcq.ResourceActivityHC_Passive[res][act].AverageCount);
                            sw.Write("{0},", (rcq.ResourceActivityHC_Active[res][act].AverageCount 
                                + rcq.ResourceActivityHC_Passive[res][act].AverageCount)
                                / rcq.ResourceHC_Available[res].AverageCount);
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
        public static void Output_Snapshot_CSVs(this RCQsModel rcq, DateTime clockTime, string dir = null)
        {
            if (dir == null) dir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var str_clockTime = clockTime.ToString("yyyy-MM-dd-HH-mm-ss");
            using (var sw = new StreamWriter(string.Format("{0}\\Snapshot_{1}.csv", dir, str_clockTime)))
            {
                sw.WriteLine("Load,Activity,Resrouce,Quantity,Type");
                foreach(var load in rcq.AllLoads)
                {
                    var pending = rcq.LoadToBatch_MovingTo[load] != null; /// MoveTos[load] is set to null if the load is processed in activity
                    var batch = rcq.LoadToBatch_Current[load];
                    foreach (var i in rcq.BatchToAllocation[batch].Res_AggregatedQuantity)
                    {
                        var res = i.Key;
                        var qtt = i.Value;
                        sw.WriteLine("{0},{1},{2},{3},{4}", load, batch.Activity, res, qtt, pending ? "passive" : "active");
                    }
                    if (pending)
                    {
                        batch = rcq.LoadToBatch_MovingTo[load];
                        foreach (var res in rcq.ActivityToResources[batch.Activity])
                        {
                            var qtt = batch.Activity.Requirements.Where(req => req.Pool.Contains(res)).Sum(req => req.Quantity);
                            sw.WriteLine("{0},{1},{2},{3},{4}", load, batch.Activity, res, qtt, "pending");
                        }
                    }
                }

                sw.WriteLine();
                sw.WriteLine(",# of Loads");
                sw.WriteLine("Arrived,{0}", rcq.CountOfLoads_Entered);
                sw.WriteLine("Processing,{0}", rcq.CountOfLoads_Processing);
                sw.WriteLine("Processed,{0}", rcq.CountOfLoads_Exited);
            }
        }
    }
}
