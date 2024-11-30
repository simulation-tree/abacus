using Simulation;
using Simulation.Functions;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds;

namespace Abacus
{
    public struct SimpleProgram : IProgram
    {
        private TimeSpan time;

        unsafe readonly StartProgram IProgram.Start => new(&Start);
        unsafe readonly UpdateProgram IProgram.Update => new(&Update);
        unsafe readonly FinishProgram IProgram.Finish => new(&Finish);

        [UnmanagedCallersOnly]
        private static void Start(Simulator simulator, Allocation allocation, World world)
        {
            allocation.Write(new SimpleProgram());
        }

        [UnmanagedCallersOnly]
        private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            ref SimpleProgram program = ref allocation.Read<SimpleProgram>();
            return program.Update(delta);
        }

        [UnmanagedCallersOnly]
        private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
        }

        private uint Update(TimeSpan delta)
        {
            time += delta;
            if (time >= TimeSpan.FromSeconds(5f))
            {
                return 1;
            }

            return 0;
        }
    }
}