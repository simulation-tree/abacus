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
            TypeLayoutRegistry.RegisterAll();
            RuntimeHelpers.RunClassConstructor(typeof(EmbeddedAddressTable).TypeHandle);

            Trace.Listeners.Add(new TextWriterTraceListener($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log", "listener"));
            Trace.Listeners.Add(new TextWriterTraceListener("latest.log", "listener"));
            Trace.AutoFlush = true;
            Trace.WriteLine("Starting simulator program");

            StatusCode statusCode;
            Schema schema = SchemaRegistry.Get();
            using (World world = new(schema))
            {
                using (AbacusSimulator simulator = new(world))
                {
#if EDITOR
                    Program editorProgram = Program.Create(world, new ControlsTest());
#endif

                    using (Program program = Program.Create(world, new ControlsTest()))
                    {
                        bool finished = program.IsFinished(out statusCode);
#if EDITOR
                        finished |= editorProgram.IsFinished(out statusCode);
#endif
                        while (!program.IsFinished(out statusCode))
                        {
                            simulator.Update();
                        }
                    }

#if EDITOR
                    editorProgram.Dispose();
#endif
                }
            }

            Allocations.ThrowIfAny();
            if (statusCode.IsSuccess)
            {
                Trace.WriteLine($"Program finished successfully with status code {statusCode.Code}");
                return 0;
            }
            else
            {
                Trace.WriteLine($"Program failed with status code {statusCode.Code}");
                return statusCode.Code;
            }
        }
    }
}