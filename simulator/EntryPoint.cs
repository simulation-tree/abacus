using Abacus;
using Abacus.Simulator;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;
using VoxelGame;
using Worlds;
using Simulator = AbacusSimulator.AbacusSimulator;

InitializeTraceListeners();
InitializeRegistries();

Trace.WriteLine("Starting simulator program");
StatusCode statusCode;

Schema schema = SchemaLoader.Get();
using (World world = new(schema))
{
    using (Simulator simulator = new(world))
    {
        using (Program<VoxelGameProgram> program = new(world))
        {
            while (!program.IsFinished(out statusCode))
            {
                simulator.Update();
            }
        }
    }
}

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

static void InitializeTraceListeners()
{
    Trace.Listeners.Add(new CustomTraceListener($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log"));
    Trace.Listeners.Add(new CustomTraceListener("latest.log"));
    Trace.AutoFlush = true;
}

static void InitializeRegistries()
{
    TypeRegistryLoader.Load();
    EmbeddedResourceRegistryLoader.Load();
}