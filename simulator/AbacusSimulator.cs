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
    public struct AbacusSimulator : IDisposable
    {
        private readonly Simulator simulator;

        public AbacusSimulator(World world)
        {
            simulator = new(world);
            simulator.AddSystem<DataImportSystem>();
            simulator.AddSystem<AutomationPlayingSystem>();
            simulator.AddSystem<StateMachineSystem>();
            simulator.AddSystem<StateAutomationSystem>();
            simulator.AddSystem<ModelImportSystem>();
            simulator.AddSystem<UISystems>();
            simulator.AddSystem<TransformSystem>();
            simulator.AddSystem<Mesh9SliceUpdateSystem>();
            simulator.AddSystem<WindowSystem>();
            simulator.AddSystem<GlobalKeyboardAndMouseSystem>();
            simulator.AddSystem<WindowDevicesSystems>();
            simulator.AddSystem<MaterialImportSystem>();
            simulator.AddSystem<TextureImportSystem>();
            simulator.AddSystem<ShaderImportSystem>();
            simulator.AddSystem<FontImportSystem>();
            simulator.AddSystem<TextRasterizationSystem>();
            simulator.AddSystem<PhysicsSystem>();
            simulator.AddSystem<CameraSystem>();
            simulator.AddSystem<FileDialogSystem>();

            ref RenderingSystems renderingSystems = ref simulator.AddSystem<RenderingSystems>().Value;
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