using NUnit.Framework;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace O2DESNet.RCQueues.UnitTest
{
    public class CarrierLoad : ILoad
    {
        public int Index { get; set; }

        public string Id { get; set; }

        public List<PassengerLoad> Passengers { get; set; } = new List<PassengerLoad>();
    }

    public class PassengerLoad : ILoad
    {
        public int Index { get; set; }

        public string Id { get; set; }

        public CarrierLoad Carrier { get; set; }

        public Dictionary<string, string> ResourceMap { get; } = new Dictionary<string, string>();

    }

    public class CarrierHandler : ActivityHandler<CarrierLoad>
    {
        public new class Statics : ActivityHandler<CarrierLoad>.Statics
        {
            public bool OnStartEnabled { get; set; } = false;
            public bool Passenger_OnStartEnabled { get; set; } = false;
            public bool Passenger_TryFinishEnabled { get; set; } = false;
        }

        public new Statics Config { get; private set; }

        private readonly Dictionary<CarrierLoad, HashSet<PassengerLoad>> _carrierToPassenger = new Dictionary<CarrierLoad, HashSet<PassengerLoad>>();
        private readonly Dictionary<PassengerLoad, CarrierLoad> _passengerToCarrier = new Dictionary<PassengerLoad, CarrierLoad>();

        private readonly HashSet<CarrierLoad> _carriersProcessed = new HashSet<CarrierLoad>();
        private readonly HashSet<PassengerLoad> _passengersCallback = new HashSet<PassengerLoad>();

        protected override void StartActivity(CarrierLoad carrier)
        {
            if (Config.OnStartEnabled)
                OnStart.Invoke(carrier);

            if (Config.Passenger_OnStartEnabled || Config.Passenger_TryFinishEnabled)
            {
                _carrierToPassenger.Add(carrier, new HashSet<PassengerLoad>());
                if (carrier.Passengers != null)
                {
                    foreach (var passesnger in carrier.Passengers)
                    {
                        if (!_passengersCallback.Contains(passesnger))
                        {
                            _carrierToPassenger[carrier].Add(passesnger);
                            _passengerToCarrier[passesnger] = carrier;
                        }
                        else
                        {
                            _passengersCallback.Remove(passesnger);
                        }


                        if (Config.Passenger_OnStartEnabled)
                            Passenger_OnStart.Invoke(passesnger);
                    }
                }
            }

            Schedule(() => EndProcess(carrier), Config.Duration(carrier, DefaultRS));
        }

        private void EndProcess(CarrierLoad carrier)
        {
            _carriersProcessed.Add(carrier);
            AttemptToFinish(carrier);
        }

        public void Passenger_TryFinish(PassengerLoad passenger)
        {
            if (!Config.Passenger_TryFinishEnabled)
                throw new Exception("Passenger_TryFinish is Disabled ");
            if(_passengerToCarrier.ContainsKey(passenger))
            {
                var carrier = _passengerToCarrier[passenger];
                _carrierToPassenger[carrier].Remove(passenger);
                _passengerToCarrier.Remove(passenger);
                if (_carrierToPassenger[carrier].Count == 0)
                    AttemptToFinish(carrier);
            }
            else
            {
                _passengersCallback.Add(passenger);
            }
        }

        private void AttemptToFinish(CarrierLoad carrier)
        {
            if(_carriersProcessed.Contains(carrier) &&
                (!Config.Passenger_TryFinishEnabled || _carrierToPassenger[carrier].Count == 0))
            {
                _carriersProcessed.Remove(carrier);
                _carrierToPassenger.Remove(carrier);
                Finish(carrier);
            }
        }

        public event Action<CarrierLoad> OnStart = ｐassenger => { };
        public event Action<PassengerLoad> Passenger_OnStart = ｐassenger => {};

        public CarrierHandler(Statics config, int seed = 0):base(config, seed)
        {
            Config = config;
        }
    }

    public class PassengerHandler : ActivityHandler<PassengerLoad>
    {
        public new class Statics : ActivityHandler<PassengerLoad>.Statics
        {
            public bool OnStartEnabled { get; set; } = false;
            public string LinkedResourceKey { get; set; }
            public string LinkedResourceName { get; set; }
        }

        public new Statics Config { get; private set; }

        public override void RequestToArrive(PassengerLoad passenger)
        {
            if(!string.IsNullOrEmpty(Config.LinkedResourceKey) && !string.IsNullOrEmpty(Config.LinkedResourceName))
            {
                if (passenger.ResourceMap[Config.LinkedResourceKey] != Config.LinkedResourceName) return;
            }
            base.RequestToArrive(passenger);
        }

        protected override void StartActivity(PassengerLoad passenger)
        {
            if (Config.OnStartEnabled)
                OnStart.Invoke(passenger);

            Schedule(() => Finish(passenger), Config.Duration(passenger, DefaultRS));
        }

        public event Action<PassengerLoad> OnStart = passenger => { };

        public PassengerHandler(Statics config, int seed = 0) : base(config, seed)
        {
            Config = config;
        }
    }

    public class LoadAllocationSimulator : RCQsContext
    {
        public new class Statics : RCQsContext.Statics
        {
            public List<IResource> Gates { get; }

            public List<IResource> Doors { get; }

            public readonly List<(string CarrierName, int Passengers)> CarrierInfos;
            public Statics()
            {
                Gates = new List<IResource>
                {
                    new Resource { Name = "Gate 1", Capacity = 1 },
                    new Resource { Name = "Gate 2", Capacity = 1 }
                };
                Doors = new List<IResource>
                {
                    new Resource { Name = "Door 1", Capacity = 1 },
                    new Resource { Name = "Door 2", Capacity = 1 }
                };

                Resources = new List<IResource>();
                Resources.AddRange(Gates);
                Resources.AddRange(Doors);

                CarrierInfos = new List<(string CarrierName, int Passengers)>
                {
                    (CarrierName : "First Carrier", Passengers : 50),
                    (CarrierName : "Second Carrier", Passengers : 60),
                    (CarrierName : "Third Carrier", Passengers : 70),
                };
            }
        }

        private int _currentLoadIndex = 0;

        public Dictionary<string, string> LoadResourceMap { get; } = new Dictionary<string, string>();

        public Statics Config { get; }
        public readonly CarrierHandler CarrierArrival;
        public readonly PassengerHandler PassengerArrival;
        public readonly List<PassengerHandler> PassengerAlight;
        public readonly PassengerHandler PassengerLeave;

        public LoadAllocationSimulator(Statics config, int seed = 0) : base(config, seed)
        {
            Config = config;

            var carrierArrivalStatic = new CarrierHandler.Statics
            {
                Name = "Carrier Arrival",
                RequirementList = new List<Requirement>
                {
                    new Requirement{Pool = Config.Gates, Quantity = 1}
                },
                OnStartEnabled = true,
                Passenger_OnStartEnabled = true,
                Passenger_TryFinishEnabled = true,
            };
            CarrierArrival = new CarrierHandler(carrierArrivalStatic, DefaultRS.Next());
            AddChild(CarrierArrival);

            var passengerArrivalStatic = new PassengerHandler.Statics
            {
                Name = "Passenger Arrival",
                OnStartEnabled = true,
            };
            PassengerArrival = new PassengerHandler(passengerArrivalStatic, DefaultRS.Next());
            AddChild(PassengerArrival);


            // Gate and Door are mapped for passengers 
            // | Carrier | Passenger |
            // |---------|-----------|
            // | Gate 1  | Door 1    |
            // | Gate 2  | Door 2    |
            PassengerAlight = new List<PassengerHandler>();
            foreach (var door in Config.Doors)
            {
                var passengerAlightStatic = new PassengerHandler.Statics
                {
                    Name = $"Passenger Aligth at {door.Name}",
                    RequirementList = new List<Requirement>
                    {
                        new Requirement{Pool = new List<IResource>{door}, Quantity = 1}
                    },
                    Duration = (passenger, rs) => TimeSpan.FromSeconds(3),
                    //File linked resource information to determine activity execution
                    LinkedResourceKey = "Gate",
                    LinkedResourceName = door.Name.Replace("Door", "Gate"),
                    OnStartEnabled = true,
                };
                var passengerAlight = new PassengerHandler(passengerAlightStatic, DefaultRS.Next());
                PassengerAlight.Add(passengerAlight);
                AddChild(passengerAlight);
            }

            var passengerLeaveStatic = new PassengerHandler.Statics
            {
                Name = "Passenger Leave",
                OnStartEnabled = true,
            };
            PassengerLeave = new PassengerHandler(passengerLeaveStatic, DefaultRS.Next());
            AddChild(PassengerLeave);

            CarrierArrival.FlowTo(Exit);
            // Flow to corresponding door by gate assigned
            foreach(var alight in PassengerAlight)
            {
                PassengerArrival
                    .FlowTo(alight)
                    .FlowTo(PassengerLeave);
            }
            PassengerLeave.FlowTo(Exit);

            CarrierArrival.OnStart += RecordResource;
            CarrierArrival.Passenger_OnStart += Passenger_Arrival;
            PassengerArrival.OnStart += LocateDoor;
            foreach(var alight in PassengerAlight)
            {
                alight.OnStart += RecordResource;
            }
            PassengerLeave.OnStart += CarrierArrival.Passenger_TryFinish;

            Schedule(Generate, DateTime.Today);
        }

        void Generate()
        {
            if(_currentLoadIndex < Config.CarrierInfos.Count)
            {
                var info = Config.CarrierInfos[_currentLoadIndex];
                var carrier = new CarrierLoad
                {
                    Id = info.CarrierName,
                    Passengers = new List<PassengerLoad>(),
                };
                for(int i = 0; i < info.Passengers; i++)
                {
                    var passenger = new PassengerLoad
                    {
                        Id = $"{carrier.Id} {i + 1}",
                        Carrier = carrier,
                    };
                    carrier.Passengers.Add(passenger);
                }

                CarrierArrival.RequestToArrive(carrier);
                ++_currentLoadIndex;
                Schedule(Generate, TimeSpan.FromMinutes(2));
            }
        }

        void RecordResource(ILoad load)
        {
            var allocation = LoadToAllocation[load];
            var resource = allocation.ResourceQuantity_Aggregated.Keys.First();
            LoadResourceMap[load.Id] = resource.Name;
        }

        void Passenger_Arrival(PassengerLoad passenger)
        {
            PassengerArrival.RequestToArrive(passenger);
        }

        void LocateDoor(PassengerLoad passenger)
        {
            var allocation = LoadToAllocation[passenger.Carrier];
            var gate = allocation.ResourceQuantity_Aggregated.Keys.First();
            //File the carrer gate information
            passenger.ResourceMap["Gate"] = gate.Name;
        }
    }

    public class Load_Allocation_Tests
    {
        [Test]
        public void RunResourceAllocationMap()
        {
            // Gate and Door are mapped for passengers once carrier gate is assigned
            // | Carrier | Passenger |
            // |---------|-----------|
            // | Gate 1  | Door 1    |
            // | Gate 2  | Door 2    |
            LoadAllocationSimulator simulator = new LoadAllocationSimulator(new LoadAllocationSimulator.Statics());
            simulator.Run(DateTime.Today.AddDays(1));

            foreach(var info in simulator.Config.CarrierInfos)
            {
                var gate = simulator.LoadResourceMap[info.CarrierName];
                var doors = (from resource in simulator.LoadResourceMap
                             where (!resource.Key.Equals(info.CarrierName) &&
                             resource.Key.StartsWith(info.CarrierName))
                             select resource.Value).Distinct().ToList();
                // Check for more than 1 assignment
                if (doors.Count > 1)
                {
                    Assert.Fail();
                }
                else
                {
                    // Check for correnct assignment
                    var door = doors[0];
                    var gateIndex = gate.Replace("Gate", "").Trim();
                    var doorIndex = door.Replace("Door", "").Trim();
                    if (gateIndex != doorIndex)
                        Assert.Fail();
                }
            }
        }
    }
}
