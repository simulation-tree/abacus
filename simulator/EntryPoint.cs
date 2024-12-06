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

            StatusCode statusCode;
            using (World world = new())
            {
                using (AbacusSimulator simulator = new(world))
                {
                    using (Program program = Program.Create(world, new ControlsTest()))
                    {
                        while (!program.IsFinished(out statusCode))
                        {
                            simulator.Update();
                        }
                    }
                }
            }

            Allocations.ThrowIfAny();
            if (statusCode.IsSuccess)
            {
                return 0;
            }
            else
            {
                return statusCode.Code;
            }
        }
    }
}