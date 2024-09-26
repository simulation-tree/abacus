using Abacus;
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
using Programs;
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

public static class Simulator
{
    private static int Main(string[] args)
    {
        uint returnCode = 0;
        using (World world = new())
        {
            //systems part of the simulation
            DataImportSystem data = new(world);
            AutomationPlayingSystem automations = new(world);
            StateMachineSystem stateMachines = new(world);
            StateAutomationSystem stateAutomation = new(world);
            ModelImportSystem models = new(world);
            TransformSystem transforms = new(world);
            WindowSystem windows = new(world);
            GlobalKeyboardAndMouseSystem globalDevices = new(world);
            WindowDevicesSystems windowDevices = new(world);
            TextureImportSystem textures = new(world);
            ShaderImportSystem shaders = new(world);
            FontImportSystem fonts = new(world);
            TextRasterizationSystem textMeshes = new(world);
            PhysicsSystem physics = new(world);
            CameraSystem cameras = new(world);
            InteractionSystems interactions = new(world);
            RenderingSystems rendering = new(world);
            rendering.renderEngine.RegisterSystem<VulkanRendererType>();

            //play the simulation
            using (Program program = Program.Create<ControlsTest>(world))
            {
                DateTime time = DateTime.UtcNow;
                TimeSpan delta = TimeSpan.Zero;
                do
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

                    DateTime now = DateTime.UtcNow;
                    delta = now - time;
                    time = now;
                    returnCode = program.Update(delta);
                }
                while (returnCode != default);
            }

            //finish
            windows.Dispose();
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
            transforms.Dispose();
            models.Dispose();
            stateAutomation.Dispose();
            stateMachines.Dispose();
            automations.Dispose();
            data.Dispose();
        }

        return (int)returnCode;
    }
}