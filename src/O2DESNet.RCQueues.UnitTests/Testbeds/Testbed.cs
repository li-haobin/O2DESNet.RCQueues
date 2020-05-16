using O2DESNet;
using O2DESNet.Distributions;
using O2DESNet.RCQueues;
using O2DESNet.RCQueues.Interfaces;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace O2DESNet.RCQueues.UnitTests.Testbeds
{
    public class Testbed : Sandbox<Testbed.Statics>
    {
        #region Statics
        public class Statics : IAssets
        {
            public string Id { get; } = "";
            public RCQueuesModel.Statics RCQueuesModel { get; set; }
            public PatternGenerator.Statics Generator { get; set; }
            public List<Activity> Activities { get; set; }
            public Dictionary<IResource, (TimeSpan MTTF, TimeSpan MTTR)> ResourceCycles { get; set; }
            public Statics() { }
            public Statics(SimpleRCQs.Statics simpleRCQs)
            {
                RCQueuesModel = new RCQueuesModel.Statics(simpleRCQs.Resources, simpleRCQs.Activities);
                Generator = simpleRCQs.Generator;
                Activities = simpleRCQs.Activities;
            }

            public Testbed Sandbox(int seed = 0) { return new Testbed(this, seed); }
        }
        #endregion

        #region Dynamics        
        internal int CountLoads { get; private set; } = 0;
        internal RCQueuesModel RCQsModel { get; private set; }
        internal PatternGenerator Generator { get; private set; }
        internal Dictionary<Activity, Random> RS { get; private set; }
        #endregion

        #region Events
        private void Arrive()
        {
            Log("Arrive");
            var load = new Load();
            var starter = Assets.Activities[0];
            RCQsModel.RequestToEnter(load, starter);
        }
        private void Enter(ILoad load)
        {
            Log("Enter", load);
        }

        private void Fail(IResource resource)
        {
            //Debug.Write("{0}\t", ClockTime.ToString());
            //Debug.WriteLine("(Event) Fail {0}", Resource);

            Log("Fail", resource);
            RCQsModel.RequestToLock(resource, resource.Capacity);
            Schedule(() => Repair(resource),
                Exponential.Sample(DefaultRS, Assets.ResourceCycles[resource].MTTR));
        }
        private void Repair(IResource resource)
        {
            //Debug.Write("{0}\t", ClockTime.ToString());
            //Debug.WriteLine("(Event) Repair {0}", Resource);

            Log("Repair", resource);
            RCQsModel.RequestToUnlock(resource, resource.Capacity);
            ScheduleToFail(resource);
        }
        private void ScheduleToFail(IResource resource)
        {
            Log("ScheduleToFail", resource);
            Schedule(() => Fail(resource),
                Exponential.Sample(DefaultRS, Assets.ResourceCycles[resource].MTTF));
        }
        private void Start(IBatch batch)
        {
            Log("Start", batch);
            var act = (Activity)batch.Activity;
            Schedule(() => Finish(batch),
                act.Duration(RS[act], batch, RCQsModel.BatchToAllocation[batch]));
        }
        private void Finish(IBatch Batch)
        {
            var act = (Activity)Batch.Activity;
            var nexts = Batch.ToDictionary(load => load, load => act.Succeedings(RS[act], load));

            RCQsModel.Finish(Batch, nexts);
        }
        private void Exit(ILoad load)
        {
            RCQsModel.Exit(load);
        }
        #endregion

        public Testbed(Statics assets, int seed, string tag = null) : base(assets, seed, tag)
        {
            RCQsModel = AddChild(new RCQueuesModel(Assets.RCQueuesModel, DefaultRS.Next()));
            Generator = AddChild(new PatternGenerator(Assets.Generator, DefaultRS.Next()));

            Generator.OnArrive += Arrive;
            RCQsModel.OnEntered += (load, activity) => Enter(load);
            RCQsModel.OnReadyToExit += Exit;
            RCQsModel.OnStarted += Start;

            RS = Assets.Activities.ToDictionary(act => act, act => new Random(DefaultRS.Next()));

            Generator.Start();
            if (Assets.ResourceCycles != null)
                foreach (var i in Assets.ResourceCycles)
                    ScheduleToFail(i.Key);
        }
    }
}
