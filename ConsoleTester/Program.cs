using O2DESNet.RCQueues.UnitTest;
using O2DESNet.RCQueues;
using System;

namespace ConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var sim = SimpleRCQ_Tests.Sample3.Simulator();
            sim.StepRun();
        }

        
    }
}
