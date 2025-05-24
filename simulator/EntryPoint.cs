using Abacus;
using Abacus.Simulator;
using Simulation;
using System;
using System.Diagnostics;
using System.Runtime;
using VoxelGame;
using Worlds;

InitializeGarbageCollector();
InitializeTraceListeners();
InitializeRegistries();

Trace.WriteLine("Starting simulator program");
Schema schema = SchemaLoader.Get();
using Application application = new(schema);
using VoxelGameProgram program = new(application.simulator);
UpdateLoop updateLoop = new();
double deltaTime = 0;
while (program.Update(application.simulator, deltaTime))
{
    deltaTime = updateLoop.GetDeltaTime();
    application.simulator.Update(deltaTime);
}

Trace.WriteLine($"Finished simulator program");

static void InitializeGarbageCollector()
{
    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
}

static void InitializeTraceListeners()
{
    Trace.Listeners.Add(new CustomTraceListener($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log"));
    Trace.Listeners.Add(new CustomTraceListener("latest.log"));
    Trace.AutoFlush = true;
}

static void InitializeRegistries()
{
    MetadataRegistryLoader.Load();
    EmbeddedResourceRegistryLoader.Load();
}