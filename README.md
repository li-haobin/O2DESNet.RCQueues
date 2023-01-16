# O2DESNet.RCQueues
(O²DES.NET Resource Constrained Queues)

An O²DES.NET library for modeling flexible queueing system with consideration of resource constraints. 
The NuGet package can be found at https://www.nuget.org/packages/O2DESNet.RCQueues/.

# Change Log
## Version 3.11
- Implement activity condition in RCQ model
- Implement load attribute update through FlowTo function implementation. Activity handler flow set up must be the latest.

## Version 3.10
- Provide load to resource allocation mapping in RCQsContext

## Version 3.9 
- Provide BatchOrder function property to set up resource assignment priority function by batch and request time

## Version 3.8
- Provide Activity Handler by Load type
- Provide RCQContext base class for simulation modeling 

## Version 3.7
- Enable query for Active/Passive batches by Resource

## Version 3.6
- Update with O2DESNet v3.6 for enhanced HourCounter
- Refined the code and follows the naming convention

## v3.5.2

Bug fixed for HourCounter on PendingLock resources

## v3.5.1

```diff
+ Add HourCounters for pending resources caused by both active and passive occupation
```
This is for a more precise statistics when calculating the utilization of resources, when resource cycles, such as shifts, occur.

## v3.4.1

- Solved HourCounter warm-up issues on tracing Activities 
  Previously, when tracing activities is enabled, the hour-counter corresponding to the activites are dynamicly added into RCQsModel. Therefore, they are not warmed-up, which causes the statistics to be biased.

## v3.3.1

- Simplified following dynamic properties of RCQsModel class so that for each _Resource_ the statistics only include _Activities_ utilized it. 
  - Resource_Activity_HC_Occupied
  - Resource_Activity_HC_Active
  - Resource_Activity_HC_Passive
  - Resource_Activity_HC_Pending

- Added following dynamic properties to group loads in each activity according to their status
  - Activity_Loads_Pending
  - Activity_Loads_Active
  - Activity_Loads_Passive

 - Fixed bug in recall for pending loads upon resource release
 
...

## v1.0.7
1. Bug fixed in HourCounter update

## v1.0.6
1. Collect and output statistics on activities and resources, using HourCounters

## v1.0.5
1. Fixed the bug of generating redundant arrival when a load is stucked at the Starter activity.
1. Implemented methods to configure a SimpleRCQ by code, i.e., AddResrouce, AddActivity, AddSucceeding, SetStarter.

## v1.0.4
1. Use only a single starter activity instead of multiple starters in a list.
1. Inter-arrival time is not specified as a static property of the Statics, whereas, the Duration of the single starter is used instead. 
