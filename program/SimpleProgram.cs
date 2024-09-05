using Programs;
using Simulation;
using System;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Abacus
{
    public struct SimpleProgram : IProgram, IDisposable
    {
        private readonly World world;
        private TimeSpan time;

        public SimpleProgram(World world)
        {
            this.world = world;
        }

        public readonly void Dispose()
        {

        }

        private bool Update(TimeSpan delta)
        {
            time += delta;
            if (time >= TimeSpan.FromSeconds(5f))
            {
                return false;
            }

            return true;
        }

        readonly unsafe (StartFunction, FinishFunction, UpdateFunction) IProgram.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                SimpleProgram program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref SimpleProgram program = ref allocation.Read<SimpleProgram>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref SimpleProgram program = ref allocation.Read<SimpleProgram>();
                return program.Update(delta) ? 0u : 1u;
            }
        }
    }
}