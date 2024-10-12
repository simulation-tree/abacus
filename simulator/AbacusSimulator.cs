using Automations.Events;
using Automations.Systems;
using Cameras.Systems;
using Data.Events;
using Data.Systems;
using Fonts.Events;
using Fonts.Systems;
using InputDevices.Events;
using InputDevices.Systems;
using InteractionKit.Events;
using InteractionKit.Systems;
using Models.Events;
using Models.Systems;
using Physics.Events;
using Physics.Systems;
using Rendering.Events;
using Rendering.Systems;
using Rendering.Vulkan;
using Shaders.Events;
using Shaders.Systems;
using Simulation;
using System;
using Textures.Events;
using Textures.Systems;
using Transforms.Events;
using Transforms.Systems;
using Windows.Events;
using Windows.Systems;

namespace AbacusSimulator
{
    public struct AbacusSimulator
    {
        public DataImportSystem data;
        public AutomationPlayingSystem automations;
        public StateMachineSystem stateMachines;
        public StateAutomationSystem stateAutomation;
        public ModelImportSystem models;
        public TransformSystem transforms;
        public WindowSystem windows;
        public GlobalKeyboardAndMouseSystem globalDevices;
        public WindowDevicesSystems windowDevices;
        public TextureImportSystem textures;
        public ShaderImportSystem shaders;
        public FontImportSystem fonts;
        public TextRasterizationSystem textMeshes;
        public PhysicsSystem physics;
        public CameraSystem cameras;
        public InteractionSystems interactions;
        public RenderingSystems rendering;

        public void Start(World world)
        {
            data = new(world);
            automations = new(world);
            stateMachines = new(world);
            stateAutomation = new(world);
            models = new(world);
            transforms = new(world);
            windows = new(world);
            globalDevices = new(world);
            windowDevices = new(world);
            textures = new(world);
            shaders = new(world);
            fonts = new(world);
            textMeshes = new(world);
            physics = new(world);
            cameras = new(world);
            interactions = new(world);
            rendering = new(world);
            rendering.renderEngine.RegisterSystem<VulkanRendererType>();
        }

        public readonly void Dispose(World world)
        {
            rendering.Dispose();
            interactions.Dispose();
            cameras.Dispose();
            physics.Dispose();
            textMeshes.Dispose();
            fonts.Dispose();
            shaders.Dispose();
            textures.Dispose();
            windowDevices.Dispose();
            globalDevices.Dispose();
            windows.Dispose();
            transforms.Dispose();
            models.Dispose();
            stateAutomation.Dispose();
            stateMachines.Dispose();
            automations.Dispose();
            data.Dispose();
        }

        public readonly void Update(World world, TimeSpan delta)
        {
            world.Submit(new WindowUpdate());
            world.Submit(new InputUpdate());
            world.Submit(new StateUpdate());
            world.Submit(new AutomationUpdate(delta));
            world.Submit(new MixingUpdate());
            world.Submit(new TransformUpdate());
            world.Submit(new PhysicsUpdate(delta));
            world.Submit(new TransformUpdate());
            world.Submit(new DataUpdate());
            world.Submit(new ModelUpdate());
            world.Submit(new ShaderUpdate());
            world.Submit(new TextureUpdate());
            world.Submit(new FontUpdate());
            world.Submit(new InteractionUpdate());
            world.Submit(new CameraUpdate());
            world.Submit(new RenderUpdate());
            world.Poll();
        }
    }
}