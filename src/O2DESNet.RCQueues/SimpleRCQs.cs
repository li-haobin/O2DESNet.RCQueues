using O2DESNet.Distributions;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace O2DESNet.RCQueues
{
    public class SimpleRCQs : Sandbox<SimpleRCQs.Statics>
    {
        public class Statics : IAssets
        {
            public string Id { get; protected set; }
            public PatternGenerator.Statics Generator { get; protected set; }
            public List<Resource> Resources { get; protected set; }
            public List<Activity> Activities { get; protected set; }

            public Statics(string id = null)
            {
                Id = id;
                Generator = new PatternGenerator.Statics();
                Resources = new List<Resource>();
                Activities = new List<Activity>();
            }
            public SimpleRCQs Sandbox(int seed = 0) { return new SimpleRCQs(this, seed); }

            public struct Key
            {
                public const string Id = "Id";
                public const string Description = "Description";
                public const string Capacity = "Capacity";
                public const string Duration_MeanInMinutes = "Mean of Duration (min.)";
                public const string Duration_CV = "CV of Duration";
                public const string Requirements = "Requirements";
                public const string Succeedings = "Succeedings";
                public const string Resources = "Resources";
                public const string Activities = "Activities";
                public const string BatchSize_Min = "Batch Size (Min)";
                public const string BatchSize_Max = "Batch Size (Max)";
                public static readonly List<string> FieldsOfResource = new List<string> { Id, Capacity, Description };
                public static readonly List<string> FieldsOfActivity = new List<string> { Id, Description, Duration_MeanInMinutes, Duration_CV, BatchSize_Min, BatchSize_Max, Requirements, Succeedings };

                public static readonly string Arrivals = "Arrivals";

                public const string MeanHourlyRate = "Mean Hourly Rate";
                public const string SeasonalFactors_HoursOfDay = "Seasonal Factors (Hours of Day)";
                public const string SeasonalFactors_DaysOfWeek = "Seasonal Factors (Days of Week)";
                public const string SeasonalFactors_DaysOfMonth = "Seasonal Factors (Days of Month)";
                public const string SeasonalFactors_MonthsOfYear = "Seasonal Factors (Months of Year)";
                public const string SeasonalFactors_Years = "Seasonal Factors (Years)";
                public static readonly List<string> FieldsOfArrivals = new List<string> { MeanHourlyRate, SeasonalFactors_HoursOfDay, SeasonalFactors_DaysOfWeek, SeasonalFactors_DaysOfMonth, SeasonalFactors_MonthsOfYear, SeasonalFactors_Years };
            }

            public static void WriteTemplateCSVs(string dir = null)
            {
                if (dir == null) dir = Directory.GetCurrentDirectory();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using (var sw = new StreamWriter(string.Format("{0}\\{1}.csv", dir, Key.Activities)))
                {
                    foreach (var key in Key.FieldsOfActivity) sw.Write("{0},", key);
                    sw.WriteLine();
                }
                using (var sw = new StreamWriter(string.Format("{0}\\{1}.csv", dir, Key.Resources)))
                {
                    foreach (var key in Key.FieldsOfResource) sw.Write("{0},", key);
                    sw.WriteLine();
                }
                using (var sw = new StreamWriter(string.Format("{0}\\{1}.csv", dir, Key.Arrivals)))
                {
                    foreach (var key in Key.FieldsOfArrivals) sw.WriteLine("{0},", key);
                    sw.WriteLine();
                }
            }

            /// <summary>
            /// Read the configuration of a simple RCQueues model from CSV files
            /// </summary>
            /// <param name="dir">The directory of CSV files, i.e., Resource.csv and Activities.csv</param>
            public static Statics ReadFromCSVs(string dir)
            {
                var simpleRCQs = new Statics();

                #region Read Resources
                foreach (var r in ReadDataDictFromCSV(string.Format("{0}\\{1}.csv", dir, Key.Resources)))
                {
                    simpleRCQs.AddResource(r[Key.Id], Convert.ToDouble(r[Key.Capacity]), r[Key.Description]);
                }
                #endregion

                #region Read Activities
                var data_activities = ReadDataDictFromCSV(string.Format("{0}\\{1}.csv", dir, Key.Activities));
                foreach (var r in data_activities)
                {
                    #region Get range of batch size
                    int? minBatchSize = null, maxBatchSize = null;
                    BatchSizeRange batchSizeRange = new BatchSizeRange();
                    if (r.ContainsKey(Key.BatchSize_Min) && r[Key.BatchSize_Min] != null && r[Key.BatchSize_Min].Length > 0)
                        minBatchSize = int.Parse(r[Key.BatchSize_Min]);
                    if (r.ContainsKey(Key.BatchSize_Max) && r[Key.BatchSize_Max] != null && r[Key.BatchSize_Max].Length > 0)
                        maxBatchSize = int.Parse(r[Key.BatchSize_Max]);
                    if (minBatchSize != null && maxBatchSize != null)
                    {
                        batchSizeRange = new BatchSizeRange(minBatchSize.Value, maxBatchSize.Value);
                    }
                    else if (minBatchSize != null)
                    {
                        batchSizeRange = new BatchSizeRange(minBatchSize.Value, int.MaxValue);
                    }
                    else if (maxBatchSize != null)
                    {
                        batchSizeRange = new BatchSizeRange(1, maxBatchSize.Value);
                    }
                    #endregion

                    simpleRCQs.AddActivity(
                        id: r[Key.Id],
                        duration: rs => TimeSpan.FromMinutes(
                            Gamma.Sample(rs, Convert.ToDouble(r[Key.Duration_MeanInMinutes]), Convert.ToDouble(r[Key.Duration_CV]))),
                        batchSizeRange: batchSizeRange,
                        requirements: r[Key.Requirements].Split(';').Where(str => str.Length > 0).Select(str =>
                        {
                            var splits = str.Split(':');
                            return (splits[0], splits.Length > 1 ? Convert.ToDouble(splits[1]) : 1.0);
                        }).ToList(),
                        description: r[Key.Description]
                        );
                }
                foreach (var r in data_activities)
                {
                    if (r[Key.Succeedings].Length > 0)
                    {
                        foreach (var succStr in r[Key.Succeedings].Split(';'))
                        {
                            var splits = succStr.Split(':');
                            if (splits.Length > 1) simpleRCQs.AddSucceeding(r[Key.Id], splits[1], Convert.ToDouble(splits[0]));
                            else simpleRCQs.AddSucceeding(r[Key.Id], splits[0], 1);
                        }
                    }
                }
                #endregion

                #region Read Arrivals
                foreach (var r in File.ReadAllLines(string.Format("{0}\\{1}.csv", dir, Key.Arrivals)))
                {
                    var line = r.Split(',').Where(s => s != null && s.Length > 0).ToList();
                    switch (line[0])
                    {
                        case Key.MeanHourlyRate:
                            simpleRCQs.Generator.MeanHourlyRate = double.Parse(line[1]);
                            break;
                        case Key.SeasonalFactors_HoursOfDay:
                            simpleRCQs.Generator.SeasonalFactors_HoursOfDay = line.GetRange(1, line.Count - 1)
                                .Select(str => double.Parse(str)).ToList();
                            break;
                        case Key.SeasonalFactors_DaysOfWeek:
                            simpleRCQs.Generator.SeasonalFactors_DaysOfWeek = line.GetRange(1, line.Count - 1)
                                .Select(str => double.Parse(str)).ToList();
                            break;
                        case Key.SeasonalFactors_DaysOfMonth:
                            simpleRCQs.Generator.SeasonalFactors_DaysOfMonth = line.GetRange(1, line.Count - 1)
                                .Select(str => double.Parse(str)).ToList();
                            break;
                        case Key.SeasonalFactors_MonthsOfYear:
                            simpleRCQs.Generator.SeasonalFactors_MonthsOfYear = line.GetRange(1, line.Count - 1)
                                .Select(str => double.Parse(str)).ToList();
                            break;
                        case Key.SeasonalFactors_Years:
                            simpleRCQs.Generator.SeasonalFactors_Years = line.GetRange(1, line.Count - 1)
                                .Select(str => double.Parse(str)).ToList();
                            break;
                    }
                }
                #endregion

                return simpleRCQs;
            }

            private static List<Dictionary<string, string>> ReadDataDictFromCSV(string file)
            {
                var rows = File.ReadAllLines(file).Select(l => l.Split(',')).ToList();
                int nCols = rows[0].Count(s => s.Length > 0);
                return Enumerable.Range(1, rows.Count - 1).Select(i => Enumerable.Range(0, nCols).ToDictionary(j => rows[0][j], j => rows[i][j])).ToList();
            }

            private readonly Dictionary<string, Resource> ResourceDict = new Dictionary<string, Resource>();
            private readonly Dictionary<string, Activity> ActivityDict = new Dictionary<string, Activity>();
            private readonly Dictionary<Activity, List<Tuple<Activity, double>>> SucceedingDict
                = new Dictionary<Activity, List<Tuple<Activity, double>>>();

            /// <summary>
            /// Add a new resource
            /// </summary>
            /// <param name="id">Id of the resource</param>
            /// <param name="capacity">Capacity of the resource</param>
            /// <param name="description">Description of the resource</param>
            public void AddResource(string id, double capacity, string description = null)
            {
                ResourceDict.Add(id, new Resource { Id = id, Capacity = capacity, Description = description });
                Resources.Add(ResourceDict[id]);
            }
            /// <summary>
            /// Add a new activity
            /// </summary>
            /// <param name="id">Id of the activity</param>
            /// <param name="description">Description of the activity</param>
            /// <param name="duration">The random generator for duration timespan</param>
            /// <param name="requirements">List of tuples of resource Id and the quantity</param>
            public void AddActivity(string id, Func<Random, TimeSpan> duration, List<(string, double)> requirements = null, string description = null, BatchSizeRange batchSizeRange = null)
            {
                if (requirements == null) requirements = new List<(string, double)>();
                ActivityDict.Add(id, new Activity
                {
                    Id = id,
                    Name = description,
                    Duration = (rs, load, alloc) => duration(rs),
                    Requirements = requirements.Select(req => new Requirement
                    {
                        Pool = new HashSet<IResource> { ResourceDict[req.Item1] },
                        Quantity = req.Item2,
                    }).ToList(),
                    Succeedings = (rs, load) => SucceedingDict[ActivityDict[id]].Count > 0 ?
                        SucceedingDict[ActivityDict[id]][Empirical.Sample(rs, SucceedingDict[ActivityDict[id]].Select(t => t.Item2))].Item1 : null,
                });
                if (batchSizeRange != null) ActivityDict[id].BatchSizeRange = batchSizeRange;
                Activities.Add(ActivityDict[id]);
                SucceedingDict.Add(ActivityDict[id], new List<Tuple<Activity, double>>());
            }
            /// <summary>
            /// Add succeeding relationship between two activities
            /// </summary>
            /// <param name="from">The Id of the "from" activity</param>
            /// <param name="to">The Id of the "to" activity</param>
            /// <param name="weight">Relative weight of the relationship (when there are multiple succeedings)</param>
            public void AddSucceeding(string from, string to, double weight)
            {
                SucceedingDict[ActivityDict[from]].Add(new Tuple<Activity, double>(ActivityDict[to], weight));
            }
        }

        #region Dynamic Properties        
        public readonly RCQsModel RCQsModel;
        private readonly PatternGenerator Generator;
        private readonly Dictionary<Activity, Random> RS;
        #endregion

        #region Events
        private void Arrive()
        {
            Log("Arrive");
            var load = new Load();
            var starter = Assets.Activities.First();
            RCQsModel.RequestToEnter((ILoad)load, starter);
        }

        private void Enter(ILoad load)
        {
            Log("Enter", load);
        }

        private void Start(IBatch batch)
        {
            Log("Start", batch);
            var act = (Activity)batch.Activity;
            Schedule(() => Finish(batch), 
                act.Duration(RS[act], batch, RCQsModel.BatchToAllocation[batch]));
        }

        private void Finish(IBatch batch)
        {
            var act = (Activity)batch.Activity;
            var nexts = batch.ToDictionary(load => load, load => act.Succeedings(RS[act], load));
            RCQsModel.Finish(batch, nexts);
        }

        private void Exit(ILoad load)
        {
            RCQsModel.Exit(load);
        }
        #endregion

        public SimpleRCQs(Statics assets, int seed, string id = null) : base(assets, seed, id)
        {
            RCQsModel = AddChild(new RCQsModel(new RCQsModel.Statics(Assets.Resources, Assets.Activities), DefaultRS.Next()));
            Generator = AddChild(new PatternGenerator(Assets.Generator, DefaultRS.Next()));


            Generator.OnArrive += Arrive;
            RCQsModel.OnEntered += (load, act) => Enter(load);
            RCQsModel.OnReadyToExit += Exit;
            RCQsModel.OnStarted += Start;

            RS = Assets.Activities.ToDictionary(act => act, act => new Random(DefaultRS.Next()));

            /// Init
            Generator.Start();
        }

        protected override void WarmedUpHandler() { }

        public override void Dispose() { }
    }
}
