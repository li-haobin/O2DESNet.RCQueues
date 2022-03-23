using NUnit.Framework;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace O2DESNet.RCQueues.UnitTest
{
    public class ConditionalActivityLoad : ILoad
    {
        public int Index { get; set; }

        public string Id { get; set; }

        public string Processor { get; set; }
    }

    public class ConditionalActivitySimulator : RCQsContext
    {
        public new class Statics : RCQsContext.Statics
        {
            public readonly List<ConditionalActivityLoad> Loads;
            public IResource Resource1 { get; set; }
            public IResource Resource2 { get; set; }

            public Statics()
            {
                Resource1 = new Resource { Name = "Server 1", Capacity = 1 };
                Resource2 = new Resource { Name = "Server 2", Capacity = 1 };

                Resources = new List<IResource> { Resource1, Resource2 };

                Loads = new List<ConditionalActivityLoad>
                {
                    new ConditionalActivityLoad {Id = "First", Processor = "Processor 1"},
                    new ConditionalActivityLoad {Id = "Second", Processor = "Processor 2"},
                    new ConditionalActivityLoad {Id = "Third", Processor = "Processor 1"},
                };
            }
        }

        private int _currentLoadIndex = 0;

        public Statics Config { get; }
        public readonly ActivityHandler<ConditionalActivityLoad> Queue;
        public readonly ActivityHandler<ConditionalActivityLoad> Processor1;
        public readonly ActivityHandler<ConditionalActivityLoad> Processor2;

        public Dictionary<string, List<ConditionalActivityLoad>> ProcessedLoad;

        public ConditionalActivitySimulator(Statics config, int seed = 0) : base(config, seed)
        {
            Config = config;
            ProcessedLoad = new Dictionary<string, List<ConditionalActivityLoad>>();

            var queueStatic = new ActivityHandler<ConditionalActivityLoad>.Statics
            {
                Name = "Queue",
            };
            Queue = new ActivityHandler<ConditionalActivityLoad>(queueStatic, DefaultRS.Next());
            AddChild(Queue);

            var processorStatic1 = new ActivityHandler<ConditionalActivityLoad>.Statics
            {
                Name = "Process 1",
                RequirementList = new List<Requirement>
                {
                    new Requirement { Pool = new List<IResource>{Config.Resource1 }, Quantity = 1 }
                },
                Duration = (load, rs) => TimeSpan.FromMinutes(10),
                Conditions = new Dictionary<string, object> { { "Processor", "Processor 1" } },
            };
            Processor1 = new ActivityHandler<ConditionalActivityLoad>(processorStatic1, DefaultRS.Next());
            AddChild(Processor1);
            ProcessedLoad[Processor1.Config.Name] = new List<ConditionalActivityLoad>();
            Processor1.OnReadyToDepart += (load) => ProcessedLoad[Processor1.Config.Name].Add(load);

            var processorStatic2 = new ActivityHandler<ConditionalActivityLoad>.Statics
            {
                Name = "Process 2",
                RequirementList = new List<Requirement>
                {
                    new Requirement { Pool = new List<IResource>{Config.Resource2 }, Quantity = 1 }
                },
                Duration = (load, rs) => TimeSpan.FromMinutes(10),
                Conditions = new Dictionary<string, object> { { "Processor", "Processor 2" } },
            };
            Processor2 = new ActivityHandler<ConditionalActivityLoad>(processorStatic2, DefaultRS.Next());
            AddChild(Processor2);
            ProcessedLoad[Processor2.Config.Name] = new List<ConditionalActivityLoad>();
            Processor2.OnReadyToDepart += (load) => ProcessedLoad[Processor2.Config.Name].Add(load);

            Queue
                .FlowTo(Processor1)
                .FlowTo(Exit);

            Queue
                .FlowTo(Processor2)
                .FlowTo(Exit);

            Schedule(Generate, DateTime.Today);
        }

        void Generate()
        {
            if(_currentLoadIndex < Config.Loads.Count)
            {
                var load = Config.Loads[_currentLoadIndex];
                Queue.RequestToArrive(load);
                ++_currentLoadIndex;
                Schedule(Generate, TimeSpan.FromMinutes(4));
            }
        }
    }

    public class Activity_Condition_Tests
    {
        [Test]
        public void RunConditionalActivity()
        {
            ConditionalActivitySimulator simulator = new ConditionalActivitySimulator(new ConditionalActivitySimulator.Statics());
            simulator.Run(DateTime.Today.AddDays(1));

            if (!(simulator.ProcessedLoad["Process 1"][0].Id == "First"
                && simulator.ProcessedLoad["Process 1"][1].Id == "Third"
                && simulator.ProcessedLoad["Process 2"][0].Id == "Second"))
            {
                Assert.Fail();
            }
        }
    }
}
