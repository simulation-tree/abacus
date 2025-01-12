using Abacus;
using Data;
using Editor;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;
using SimulationProgram = Simulation.Program;
using Simulator = AbacusSimulator.AbacusSimulator;

TypeLayoutRegistry.RegisterAll();
EmbeddedAddressTable.RegisterAll();

Trace.Listeners.Add(new TextWriterTraceListener($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log", "listener"));
Trace.Listeners.Add(new TextWriterTraceListener("latest.log", "listener"));
Trace.AutoFlush = true;
Trace.WriteLine("Starting simulator program");

StatusCode statusCode;
Schema schema = SchemaRegistry.Get();
using (World world = new(schema))
{
    using (Simulator simulator = new(world))
    {
#if EDITOR
        SimulationProgram editorProgram = SimulationProgram.Create(world, new ControlsTest());
#endif

        using (SimulationProgram program = SimulationProgram.Create(world, new WindowCreationTest()))
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