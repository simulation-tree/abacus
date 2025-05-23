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
using System;
using TextRendering.Systems;
using Textures.Systems;
using Transforms.Systems;
using UI.Systems;
using Windows.Systems;
using Worlds;

namespace Abacus
{
    public class Application : IDisposable
    {
        private Simulation.Simulator simulator;
        public readonly World world;

        public Simulation.Simulator Simulator => simulator;

        public Application(Schema schema)
        {
            world = new(schema);
            simulator = new(world);

            simulator.Add(new DataImportSystem());
            simulator.Add(new AutomationPlayingSystem());
            simulator.Add(new StateMachineSystem());
            simulator.Add(new StateAutomationSystem());
            simulator.Add(new ModelImportSystem());
            simulator.Add(new UISystemsBank(simulator));
            simulator.Add(new TransformSystem());
            simulator.Add(new Mesh9SliceUpdateSystem());
            simulator.Add(new WindowSystem(world));
            simulator.Add(new GlobalKeyboardAndMouseSystem());
            simulator.Add(new WindowDevicesSystems());
            simulator.Add(new MaterialImportSystem());
            simulator.Add(new TextureImportSystem());
            simulator.Add(new ShaderImportSystem());
            simulator.Add(new FontImportSystem());
            simulator.Add(new TextMeshGenerationSystem());
            simulator.Add(new PhysicsSystem(simulator));
            simulator.Add(new CameraSystem());
            simulator.Add(new FileDialogSystem());

            RenderingSystems rendering = new(simulator);
            simulator.Add(rendering);
            rendering.RegisterRenderingBackend(new VulkanBackend());
        }

        public void Dispose()
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

            simulator.Dispose();
            world.Dispose();
        }

        public void Update(out double deltaTime)
        {
            simulator.Update(out deltaTime);
        }
    }
}