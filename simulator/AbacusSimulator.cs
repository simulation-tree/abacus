using Abacus;
using Data.Events;
using Data.Systems;
using Models.Events;
using Models.Systems;
using Programs;
using Rendering.Events;
using Rendering.Systems;
using Rendering.Vulkan;
using Shaders.Events;
using Shaders.Systems;
using Simulation;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Textures.Events;
using Textures.Systems;
using Transforms.Events;
using Transforms.Systems;
using Windows.Events;
using Windows.Systems;

public static class AbacusSimulator
{
    public static void Main()
    {
        RerouteConsoleOutputToDebug();

        using (World world = new())
        {
            //systems part of the simulation
            DataImportSystem data = new(world);
            MaterialImportSystem materials = new(world);
            ModelImportSystem models = new(world);
            TransformSystem transforms = new(world);
            WindowSystem windows = new(world);
            TextureImportSystem textures = new(world);
            ShaderImportSystem shaders = new(world);
            CameraSystem cameras = new(world);
            RenderEngineSystem rendering = new(world);
            rendering.RegisterSystem<VulkanRendererType>();

            //play the simulation
            using (Program program = Program.Create<AbacusProgram>(world))
            {
                DateTime time = DateTime.UtcNow;
                TimeSpan delta;
                do
                {
                    world.Submit(new WindowUpdate());
                    world.Submit(new TransformUpdate());
                    world.Submit(new DataUpdate());
                    world.Submit(new ModelUpdate());
                    world.Submit(new ShaderUpdate());
                    world.Submit(new TextureUpdate());
                    world.Submit(new CameraUpdate());
                    world.Submit(new RenderUpdate());
                    world.Poll();

                    DateTime now = DateTime.UtcNow;
                    delta = now - time;
                    time = now;
                }
                while (program.Update(delta) == 0);
            }

            //finish
            rendering.Dispose();
            cameras.Dispose();
            shaders.Dispose();
            textures.Dispose();
            windows.Dispose();
            transforms.Dispose();
            models.Dispose();
            materials.Dispose();
            data.Dispose();
        }
    }

    private static void RerouteConsoleOutputToDebug()
    {
        TextWriter consoleWriter = new StringWriter();
        _ = Task.Run(() =>
        {
            string lastOutput = string.Empty;
            while (true)
            {
                string output = consoleWriter.ToString() ?? string.Empty;
                if (lastOutput != output)
                {
                    Debug.WriteLine(output);
                }

                lastOutput = output;
                consoleWriter.Flush();
                Thread.Sleep(8);
            }
        });

        Console.SetOut(consoleWriter);
    }
}