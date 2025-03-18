using Automations.Systems;
using Cameras.Systems;
using Data.Systems;
using FileDialogs.Systems;
using Fonts.Systems;
using InputDevices.Systems;
using Materials.Systems;
using Meshes.NineSliced.Systems;
using Models.Systems;
using Physics.Systems;
using Rendering.Systems;
using Rendering.Vulkan;
using Shaders.Systems;
using Simulation;
using System;
using TextRendering.Systems;
using Textures.Systems;
using Transforms.Systems;
using UI.Systems;
using Windows.Systems;
using Worlds;

namespace AbacusSimulator
{
    public readonly struct AbacusSimulator : IDisposable
    {
        private readonly Simulator simulator;

        [Obsolete("Default constructor not supported", true)]
        public AbacusSimulator()
        {
        }

        public AbacusSimulator(World world)
        {
            simulator = new(world);
            simulator.AddSystem(new DataImportSystem());
            simulator.AddSystem(new AutomationPlayingSystem());
            simulator.AddSystem(new StateMachineSystem());
            simulator.AddSystem(new StateAutomationSystem());
            simulator.AddSystem(new ModelImportSystem());
            simulator.AddSystem(new UISystems());
            simulator.AddSystem(new TransformSystem());
            simulator.AddSystem(new Mesh9SliceUpdateSystem());
            simulator.AddSystem(new WindowSystem());
            simulator.AddSystem(new GlobalKeyboardAndMouseSystem());
            simulator.AddSystem(new WindowDevicesSystems());
            simulator.AddSystem(new MaterialImportSystem());
            simulator.AddSystem(new TextureImportSystem());
            simulator.AddSystem(new ShaderImportSystem());
            simulator.AddSystem(new FontImportSystem());
            simulator.AddSystem(new TextRasterizationSystem());
            simulator.AddSystem(new PhysicsSystem());
            simulator.AddSystem(new CameraSystem());
            simulator.AddSystem(new FileDialogSystem());

            RenderingSystems renderingSystems = simulator.AddSystem(new RenderingSystems());
            renderingSystems.RegisterRenderingBackend<VulkanBackend>();
        }

        public readonly void Dispose()
        {
            simulator.RemoveSystem<FileDialogSystem>();
            simulator.RemoveSystem<RenderingSystems>();
            simulator.RemoveSystem<CameraSystem>();
            simulator.RemoveSystem<PhysicsSystem>();
            simulator.RemoveSystem<TextRasterizationSystem>();
            simulator.RemoveSystem<FontImportSystem>();
            simulator.RemoveSystem<ShaderImportSystem>();
            simulator.RemoveSystem<TextureImportSystem>();
            simulator.RemoveSystem<MaterialImportSystem>();
            simulator.RemoveSystem<WindowDevicesSystems>();
            simulator.RemoveSystem<GlobalKeyboardAndMouseSystem>();
            simulator.RemoveSystem<WindowSystem>();
            simulator.RemoveSystem<Mesh9SliceUpdateSystem>();
            simulator.RemoveSystem<TransformSystem>();
            simulator.RemoveSystem<UISystems>();
            simulator.RemoveSystem<ModelImportSystem>();
            simulator.RemoveSystem<StateAutomationSystem>();
            simulator.RemoveSystem<StateMachineSystem>();
            simulator.RemoveSystem<AutomationPlayingSystem>();
            simulator.RemoveSystem<DataImportSystem>();
            simulator.Dispose();
        }

        public readonly void Update()
        {
            simulator.Update();
        }
    }
}