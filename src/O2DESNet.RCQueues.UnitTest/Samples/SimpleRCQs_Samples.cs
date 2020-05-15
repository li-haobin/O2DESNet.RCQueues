using System;
using System.Collections.Generic;

namespace O2DESNet.RCQueues.UnitTests.Samples
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
            var res1 = Guid.NewGuid();
            var res2 = Guid.NewGuid();

            var act1Id = Guid.NewGuid();
            var act2Id = Guid.NewGuid();
            var act3Id = Guid.NewGuid();

            var simpleRCQs = new SimpleRCQs.Statics();
            simpleRCQs.AddResource(res1, "Res1", 10, "Resource #1");
            simpleRCQs.AddResource(res2, "Res2", 3, "Resource #1");

            simpleRCQs.AddActivity(act1Id, "Activity #1", rs => TimeSpan.FromMinutes(rs.NextDouble() * 5), null, null);
            simpleRCQs.AddActivity(act2Id, "Activity #2", rs => TimeSpan.FromMinutes(rs.NextDouble() * 4),
                new List<(Guid, double)>
                {
                    (res1, 6),
                    (res2, 2),
                }, null);
            simpleRCQs.AddActivity(act3Id, "Activity #3", rs => TimeSpan.FromMinutes(rs.NextDouble() * 4),
                new List<(Guid, double)>
                {
                    (res1, 4),
                    (res2, 1),
                }, null);
            simpleRCQs.AddSucceeding(act1Id, act2Id, 1);
            simpleRCQs.AddSucceeding(act1Id, act3Id, 1);
            simpleRCQs.Generator.MeanHourlyRate = 10;

            return simpleRCQs;
        }
    }
}
