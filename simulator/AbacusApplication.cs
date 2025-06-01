using Automations.Messages;
using Automations.Systems;
using Cameras.Messages;
using Cameras.Systems;
using Data.Messages;
using Data.Systems;
using FileDialogs.Messages;
using FileDialogs.Systems;
using Fonts.Systems;
using InputDevices.Messages;
using InputDevices.Systems;
using Materials.Systems;
using Meshes.NineSliced.Systems;
using Models.Systems;
using Physics.Messages;
using Physics.Systems;
using Rendering.Messages;
using Rendering.Systems;
using Rendering.Vulkan;
using Shaders.Systems;
using System;
using System.Diagnostics;
using System.Runtime;
using TextRendering.Systems;
using Textures.Systems;
using Transforms.Messages;
using Transforms.Systems;
using UI.Messages;
using UI.Systems;
using Windows.Messages;
using Windows.Systems;
using Worlds.Messages;

namespace Abacus.Simulator
{
    public class AbacusApplication : Application
    {
        public AbacusApplication() : base(SchemaLoader.Get())
        {
            InitializeGarbageCollector();
            InitializeTraceListeners();
            InitializeRegistries();
            AddSystems();
        }

        public override void Dispose()
        {
            RemoveSystems();
            base.Dispose();
        }

        protected override void Update(double deltaTime)
        {
            simulator.Broadcast(new DataUpdate(deltaTime));
            simulator.Broadcast(new AutomationUpdate(deltaTime));
            simulator.Broadcast(new TransformUpdate());
            simulator.Broadcast(new InputUpdate(deltaTime));
            simulator.Broadcast(new WindowUpdate());
            simulator.Broadcast(new PhysicsUpdate(deltaTime));
            simulator.Broadcast(new CameraUpdate());
            simulator.Broadcast(new FileDialogUpdate());
            simulator.Broadcast(new UIUpdate());
            simulator.Broadcast(new Update(deltaTime));
            simulator.Broadcast(new RenderUpdate());
        }

        private void AddSystems()
        {
            simulator.Add(new DataImportSystem(simulator, world));
            simulator.Add(new AutomationPlayingSystem(simulator, world));
            simulator.Add(new StateMachineSystem(simulator, world));
            simulator.Add(new StateAutomationSystem(simulator, world));
            simulator.Add(new ModelImportSystem(simulator, world));
            simulator.Add(new UISystemsBank(simulator, world));
            simulator.Add(new TransformSystem(simulator, world));
            simulator.Add(new Mesh9SliceUpdateSystem(simulator, world));
            simulator.Add(new WindowSystem(simulator, world));
            simulator.Add(new GlobalKeyboardAndMouseSystem(simulator, world));
            simulator.Add(new WindowDevicesSystems(simulator, world));
            simulator.Add(new MaterialImportSystem(simulator, world));
            simulator.Add(new TextureImportSystem(simulator, world));
            simulator.Add(new ShaderImportSystem(simulator, world));
            simulator.Add(new FontImportSystem(simulator, world));
            simulator.Add(new TextMeshGenerationSystem(simulator, world));
            simulator.Add(new PhysicsSystem(simulator, world));
            simulator.Add(new CameraSystem(simulator, world));
            simulator.Add(new FileDialogSystem(simulator, world));

            RenderingSystems rendering = new(simulator, world);
            simulator.Add(rendering);
            rendering.RegisterRenderingBackend(new VulkanBackend());
        }

        private void RemoveSystems()
        {
            RenderingSystems rendering = simulator.GetFirst<RenderingSystems>();
            rendering.UnregisterRenderingBackend<VulkanBackend>();
            simulator.Remove(rendering);
            rendering.Dispose();

            simulator.Remove<FileDialogSystem>();
            simulator.Remove<CameraSystem>();
            simulator.Remove<PhysicsSystem>();
            simulator.Remove<TextMeshGenerationSystem>();
            simulator.Remove<FontImportSystem>();
            simulator.Remove<ShaderImportSystem>();
            simulator.Remove<TextureImportSystem>();
            simulator.Remove<MaterialImportSystem>();
            simulator.Remove<WindowDevicesSystems>();
            simulator.Remove<GlobalKeyboardAndMouseSystem>();
            simulator.Remove<WindowSystem>();
            simulator.Remove<Mesh9SliceUpdateSystem>();
            simulator.Remove<TransformSystem>();
            simulator.Remove<UISystemsBank>();
            simulator.Remove<ModelImportSystem>();
            simulator.Remove<StateAutomationSystem>();
            simulator.Remove<StateMachineSystem>();
            simulator.Remove<AutomationPlayingSystem>();
            simulator.Remove<DataImportSystem>();
        }
        private static void InitializeGarbageCollector()
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }

        private static void InitializeTraceListeners()
        {
            Trace.Listeners.Add(new CustomTraceListener($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log"));
            Trace.Listeners.Add(new CustomTraceListener("latest.log"));
            Trace.AutoFlush = true;
        }

        private static void InitializeRegistries()
        {
            MetadataRegistryLoader.Load();
            EmbeddedResourceRegistryLoader.Load();
        }
    }
}