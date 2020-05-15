using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleTester
{
    public static class Extensions
    {
        public static void StepRun(this O2DESNet.Simulator sim)
        {
            while (true)
            {
                //var clk = sim.ClockTime;
                //while (sim.ClockTime == clk)
                sim.Run(1);
                Console.ReadKey();
                Console.WriteLine("\n");
                Console.WriteLine(sim.ClockTime);
                sim.WriteToConsole();
            }
        }
    }
}
