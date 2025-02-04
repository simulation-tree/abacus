using Abacus.Simulator;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;
using Simulator = AbacusSimulator.AbacusSimulator;

Trace.Listeners.Add(new CustomTraceListener($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log"));
Trace.Listeners.Add(new CustomTraceListener("latest.log"));
Trace.AutoFlush = true;

TypeRegistryLoader.Load();
EmbeddedResourceRegistryLoader.Load();

Trace.WriteLine("Starting simulator program");

StatusCode statusCode;
Schema schema = SchemaLoader.Get();
using (World world = new(schema))
{
    using (Simulator simulator = new(world))
    {
#if EDITOR
        var editorProgram = new Program<ControlsTest>(world);
#endif

        using (var program = new Program<VoxelGame.VoxelGameProgram>(world))
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