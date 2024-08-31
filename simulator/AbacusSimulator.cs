using Abacus;
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

public static class AbacusSimulator
{
    public static int Main()
    {
        uint returnCode = 0;
        using (World world = new())
        {
            //systems part of the simulation
            DataImportSystem data = new(world);
            MaterialImportSystem materials = new(world);
            ModelImportSystem models = new(world);
            TransformSystem transforms = new(world);
            WindowSystem windows = new(world);
            GlobalKeyboardAndMouseSystem kbm = new(world);
            WindowDevicesSystems windowDevices = new(world);
            TextureImportSystem textures = new(world);
            ShaderImportSystem shaders = new(world);
            FontImportSystem fonts = new(world);
            TextRenderingSystem textMeshes = new(world);
            PhysicsSystem physics = new(world);
            CameraSystem cameras = new(world);
            InvokeTriggersSystem triggers = new(world);
            RenderEngineSystem rendering = new(world);
            rendering.RegisterSystem<VulkanRendererType>();

            //play the simulation
            using (Program program = Program.Create<EditorProgram>(world))
            {
                DateTime time = DateTime.UtcNow;
                TimeSpan delta = TimeSpan.Zero;
                do
                {
                    world.Submit(new WindowUpdate());
                    world.Submit(new InputUpdate());
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
                while (returnCode == 0);
            }

            //finish
            rendering.Dispose();
            triggers.Dispose();
            cameras.Dispose();
            physics.Dispose();
            textMeshes.Dispose();
            fonts.Dispose();
            shaders.Dispose();
            textures.Dispose();
            windowDevices.Dispose();
            kbm.Dispose();
            windows.Dispose();
            transforms.Dispose();
            models.Dispose();
            materials.Dispose();
            data.Dispose();
        }

        return (int)returnCode;
    }
}