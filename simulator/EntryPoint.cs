using Abacus;
using Abacus.Simulator;
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
using VoxelGameProgram program = new(application.Simulator);
double deltaTime;
do
{
    application.Update(out deltaTime);
}
while (program.Update(application.Simulator, deltaTime));

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