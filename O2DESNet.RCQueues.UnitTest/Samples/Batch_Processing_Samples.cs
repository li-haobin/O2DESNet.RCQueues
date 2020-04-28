using O2DESNet.Distributions;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues.UnitTest
{
    public static class Batch_Processing_Samples
    {
        public static Testbed.Statics Sample1()
        {
            var resources = Enumerable.Range(0, 10).Select(id => (IResource)new Resource { Id = id.ToString(), Capacity = 1 }).ToList();
            HashSet<IResource> GetPool(Func<IResource, bool> condition)
            {
                return new HashSet<IResource>(resources.Where(condition));
            }
            var activities = new List<Activity>
            {
                new Activity
                {
                    Id = "0 - Starter",
                    Requirements = new List<Requirement>(),
                    Duration = (rs, load, alloc) => Exponential.Sample(rs, TimeSpan.FromMinutes(10)),
                },
                // flow 1
                new Activity
                {
                    Id = "1",
                    Requirements = new List<Requirement>
                    {
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 5), Quantity = 2 },
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) < 3), Quantity = 1 },
                    },
                    Duration = (rs, load, alloc) => Exponential.Sample(rs, TimeSpan.FromMinutes(5)),
                    BatchSizeRange = new BatchSizeRange(1, 3),
                },
                new Activity
                {
                    Id = "2 - Buffer",
                    Requirements = new List<Requirement>(),
                    Duration = (rs, load, alloc) => TimeSpan.FromSeconds(0),
                },
                new Activity
                {
                    Id = "3",
                    Requirements = new List<Requirement>
                    {
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 7), Quantity = 1 },
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 2 && int.Parse(res.Id) < 5), Quantity = 1 },
                    },
                    Duration = (rs, load, alloc) => Exponential.Sample(rs, TimeSpan.FromMinutes(5)),
                    BatchSizeRange = new BatchSizeRange(2, 3),
                },
                new Activity
                {
                    Id = "4 - Buffer",
                    Requirements = new List<Requirement>(),
                    Duration = (rs, load, alloc) => TimeSpan.FromSeconds(0),
                },
                new Activity
                {
                    Id = "5",
                    Requirements = new List<Requirement>
                    {
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 7 && int.Parse(res.Id) < 10), Quantity = 1 },
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 2 && int.Parse(res.Id) < 5), Quantity = 1 },
                    },
                    Duration = (rs, load, alloc) => Exponential.Sample(rs, TimeSpan.FromMinutes(5)),
                    BatchSizeRange = new BatchSizeRange(1, 1),
                },
                /// flow 2
                new Activity
                {
                    Id = "6",
                    Requirements = new List<Requirement>
                    {
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 6 && int.Parse(res.Id) < 8), Quantity = 0.3 },
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > -1 && int.Parse(res.Id) < 4), Quantity = 1 },
                    },
                    Duration = (rs, load, alloc) => Exponential.Sample(rs, TimeSpan.FromMinutes(5)),
                },
                new Activity
                {
                    Id = "7 - Buffer",
                    Requirements = new List<Requirement>(),
                    Duration = (rs, load, alloc) => TimeSpan.FromSeconds(0),
                },
                new Activity
                {
                    Id = "8",
                    Requirements = new List<Requirement>
                    {
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 3 && int.Parse(res.Id) < 11), Quantity = 0.2 },
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 1 && int.Parse(res.Id) < 6), Quantity = 1 },
                    },
                    Duration = (rs, load, alloc) => Exponential.Sample(rs, TimeSpan.FromMinutes(5)),
                    BatchSizeRange = new BatchSizeRange(3, 3),
                },
                new Activity
                {
                    Id = "9 - Buffer",
                    Requirements = new List<Requirement>(),
                    Duration = (rs, load, alloc) => TimeSpan.FromSeconds(0),
                },
                new Activity
                {
                    Id = "10",
                    Requirements = new List<Requirement>
                    {
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > -1 && int.Parse(res.Id) < 3), Quantity = 0.2 },
                        new Requirement{ Pool = GetPool(res => int.Parse(res.Id) > 6 && int.Parse(res.Id) < 11), Quantity = 1 },
                    },
                    Duration = (rs, load, alloc) => Exponential.Sample(rs, TimeSpan.FromMinutes(5)),
                    BatchSizeRange = new BatchSizeRange(2, 3),
                },
            };
            activities[0].Succeedings = (rs, o) => rs.NextDouble() > 0.3 ? activities[1] : activities[6];
            activities[1].Succeedings = (rs, o) => activities[2];
            activities[2].Succeedings = (rs, o) => activities[3];
            activities[3].Succeedings = (rs, o) => activities[4];
            activities[4].Succeedings = (rs, o) => activities[5];
            activities[5].Succeedings = (rs, o) => null;
            activities[6].Succeedings = (rs, o) => activities[7];
            activities[7].Succeedings = (rs, o) => activities[8];
            activities[8].Succeedings = (rs, o) => activities[9];
            activities[9].Succeedings = (rs, o) => activities[10];
            activities[10].Succeedings = (rs, o) => null;

            return new Testbed.Statics
            {
                RCQueuesModel = new RCQsModel.Statics(resources, activities),
                Generator = new PatternGenerator.Statics { MeanHourlyRate = 11 },
                Activities = activities,
            };
        }
    }
}
