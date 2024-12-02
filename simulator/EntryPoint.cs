using Abacus;
using Data;
using Simulation;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds;

namespace AbacusSimulator
{
    public static class EntryPoint
    {
        private static int Main(string[] args)
        {
            RuntimeHelpers.RunClassConstructor(typeof(TypeTable).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(EmbeddedAddressTable).TypeHandle);

            Trace.Listeners.Add(new TextWriterTraceListener($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log", "listener"));
            Trace.AutoFlush = true;
            Trace.WriteLine("Starting simulator program");

            uint returnCode;
            using (World world = new())
            {
                using (AbacusSimulator simulator = new(world))
                {
                    using (Program program = Program.Create<VoxelGame>(world))
                    {
                        while (!program.IsFinished(out returnCode))
                        {
                            simulator.Update();
                        }
                    }
                }
            }

            Allocations.ThrowIfAny();
            return (int)returnCode;
        }
    }
}