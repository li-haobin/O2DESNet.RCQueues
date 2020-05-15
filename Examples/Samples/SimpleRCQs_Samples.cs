using System;
using System.Collections.Generic;

namespace Examples.Samples
{
    public static class SimpleRCQs_Samples
    {
        public static SimpleRCQs.Statics Sample1()
        {
            return SimpleRCQs.Statics.ReadFromCSVs("SimpleRCQ\\Sample1");
        }

        public static SimpleRCQs.Statics Sample1s()
        {
            return SimpleRCQs.Statics.ReadFromCSVs("SimpleRCQ\\Sample1s");
        }

        public static SimpleRCQs.Statics Sample2()
        {
            var simpleRCQs = new SimpleRCQs.Statics();
            simpleRCQs.AddResource("Res1", 10, "Resource #1");
            simpleRCQs.AddResource("Res2", 3, "Resource #1");

            var act1Id = Guid.NewGuid();
            var act2Id = Guid.NewGuid();
            var act3Id = Guid.NewGuid();

            simpleRCQs.AddActivity(act1Id, rs => TimeSpan.FromMinutes(rs.NextDouble() * 5), name: "Activity #1");
            simpleRCQs.AddActivity(act2Id, rs => TimeSpan.FromMinutes(rs.NextDouble() * 4),
                new List<(string, double)>
                {
                    ("Res1", 6),
                    ("Res2", 2),
                });
            simpleRCQs.AddActivity(act3Id, rs => TimeSpan.FromMinutes(rs.NextDouble() * 4),
                new List<(string, double)>
                {
                    ("Res1", 4),
                    ("Res2", 1),
                });
            simpleRCQs.AddSucceeding(act1Id, act2Id, 1);
            simpleRCQs.AddSucceeding(act1Id, act3Id, 1);
            simpleRCQs.Generator.MeanHourlyRate = 10;
            return simpleRCQs;
        }
    }
}
