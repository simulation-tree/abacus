using Abacus;
using Programs;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;

namespace AbacusSimulator
{
    public static class EntryPoint
    {
        private static int Main(string[] args)
        {
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
                        DateTime lastTime = DateTime.UtcNow;
                        do
                        {
                            DateTime now = DateTime.UtcNow;
                            TimeSpan delta = now - lastTime;
                            lastTime = now;

                            simulator.Update(delta);
                        }
                        while (!program.IsFinished(out returnCode));
                    }
                }
            }

            Allocations.ThrowIfAny();
            return (int)returnCode;
        }
    }
}