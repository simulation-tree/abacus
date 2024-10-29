using Automations.Systems;
using Cameras.Systems;
using Data.Systems;
using Fonts.Systems;
using InputDevices.Systems;
using InteractionKit.Systems;
using Models.Systems;
using Physics.Systems;
using Rendering.Systems;
using Rendering.Vulkan;
using Shaders.Systems;
using Simulation;
using System;
using Textures.Systems;
using Transforms.Systems;
using Windows.Systems;

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
            simulator.AddSystem<TransformSystem>();
            simulator.AddSystem<WindowSystem>();
            simulator.AddSystem<GlobalKeyboardAndMouseSystem>();
            simulator.AddSystem<WindowDevicesSystems>();
            simulator.AddSystem<TextureImportSystem>();
            simulator.AddSystem<ShaderImportSystem>();
            simulator.AddSystem<FontImportSystem>();
            simulator.AddSystem<TextRasterizationSystem>();
            simulator.AddSystem<PhysicsSystem>();
            simulator.AddSystem<CameraSystem>();
            simulator.AddSystem<InteractionSystems>();

            ref RenderingSystems renderingSystems = ref simulator.AddSystem<RenderingSystems>().Value;
            renderingSystems.RegisterRenderSystem<VulkanRenderer>();
        }

        public readonly void Dispose()
        {
            simulator.RemoveSystem<RenderingSystems>();
            simulator.RemoveSystem<InteractionSystems>();
            simulator.RemoveSystem<CameraSystem>();
            simulator.RemoveSystem<PhysicsSystem>();
            simulator.RemoveSystem<TextRasterizationSystem>();
            simulator.RemoveSystem<FontImportSystem>();
            simulator.RemoveSystem<ShaderImportSystem>();
            simulator.RemoveSystem<TextureImportSystem>();
            simulator.RemoveSystem<WindowDevicesSystems>();
            simulator.RemoveSystem<GlobalKeyboardAndMouseSystem>();
            simulator.RemoveSystem<WindowSystem>();
            simulator.RemoveSystem<TransformSystem>();
            simulator.RemoveSystem<ModelImportSystem>();
            simulator.RemoveSystem<StateAutomationSystem>();
            simulator.RemoveSystem<StateMachineSystem>();
            simulator.RemoveSystem<AutomationPlayingSystem>();
            simulator.RemoveSystem<DataImportSystem>();
            simulator.Dispose();
        }

        public readonly void Update(TimeSpan delta)
        {
            simulator.Update(delta);
        }
    }
}