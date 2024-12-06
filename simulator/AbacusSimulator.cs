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
using TextRendering.Systems;
using Textures.Systems;
using Transforms.Systems;
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
            simulator.AddSystem(new DataImportSystem());
            simulator.AddSystem(new AutomationPlayingSystem());
            simulator.AddSystem(new StateMachineSystem());
            simulator.AddSystem(new StateAutomationSystem());
            simulator.AddSystem(new ModelImportSystem());
            simulator.AddSystem(new TransformSystem());
            simulator.AddSystem(new WindowSystem());
            simulator.AddSystem(new GlobalKeyboardAndMouseSystem());
            simulator.AddSystem(new WindowDevicesSystems());
            simulator.AddSystem(new TextureImportSystem());
            simulator.AddSystem(new ShaderImportSystem());
            simulator.AddSystem(new FontImportSystem());
            simulator.AddSystem(new TextRasterizationSystem());
            simulator.AddSystem(new PhysicsSystem());
            simulator.AddSystem(new CameraSystem());
            simulator.AddSystem(new InteractionSystems());

            ref RenderingSystems renderingSystems = ref simulator.AddSystem(new RenderingSystems()).Value;
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

        public readonly void Update()
        {
            simulator.Update();
        }
    }
}