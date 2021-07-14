using NUnit.Framework;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace O2DESNet.RCQueues.UnitTest
{
    public class PriorityLoad : ILoad
    {
        public int Index { get; set; }

        public string Id { get; set; }

        public DateTime DueTime { get; set; }
    }

    public class PrioritySimulator : RCQsContext
    {
        public new class Statics : RCQsContext.Statics
        {
            public readonly List<PriorityLoad> Loads;
            public Statics()
            {
                Resources = new List<IResource> 
                { 
                    new Resource { Name = "Server", Capacity = 1 } 
                };
                Loads = new List<PriorityLoad>
                {
                    new PriorityLoad {Id = "First", DueTime = DateTime.Today.AddHours(2)},
                    new PriorityLoad {Id = "Second", DueTime = DateTime.Today.AddHours(3)},
                    new PriorityLoad {Id = "Third", DueTime = DateTime.Today.AddHours(1)},
                };
            }
        }

        private int _currentLoadIndex = 0;

        public Statics Config { get; }
        public readonly ActivityHandler<PriorityLoad> Queue;
        public readonly ActivityHandler<PriorityLoad> Processor;

        public List<PriorityLoad> ProcessedLoad;

        public PrioritySimulator(Statics config, int seed = 0) : base(config, seed)
        {
            Config = config;
            ProcessedLoad = new List<PriorityLoad>();

            var queueStatic = new ActivityHandler<PriorityLoad>.Statics
            {
                Name = "Queue",
            };
            Queue = new ActivityHandler<PriorityLoad>(queueStatic, DefaultRS.Next());
            AddChild(Queue);

            var processorStatic = new ActivityHandler<PriorityLoad>.Statics
            {
                Name = "Process",
                RequirementList = new List<Requirement> 
                { 
                    new Requirement { Pool = Config.Resources, Quantity = 1 } 
                },
                Duration = (load, rs) => TimeSpan.FromMinutes(10),

                //Set up activity batch process order by load due time
                BatchOrder = (request1, request2) => 
                {
                    var load1 = request1.Batch.First() as PriorityLoad;
                    var load2 = request2.Batch.First() as PriorityLoad;

                    //Give higher priority for near due time first, the consider request time, 
                    if (load1.DueTime != load2.DueTime)
                        return load1.DueTime.CompareTo(load2.DueTime);
                    else
                        return request1.Time.CompareTo(request2.Time);
                }
            };
            Processor = new ActivityHandler<PriorityLoad>(processorStatic, DefaultRS.Next());
            AddChild(Processor);

            Processor.OnReadyToDepart += (load) => ProcessedLoad.Add(load);

            Queue
                .FlowTo(Processor)
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

    public class Priority_Queue_Tests
    {
        [Test]
        public void RunPriorityQueue()
        {
            PrioritySimulator simulator = new PrioritySimulator(new PrioritySimulator.Statics());
            simulator.Run(DateTime.Today.AddDays(1));

            if (!(simulator.ProcessedLoad[0].Id == "First"
                && simulator.ProcessedLoad[1].Id == "Third"
                && simulator.ProcessedLoad[2].Id == "Second"))
            {
                Assert.Fail();
            }
        }
    }
}
