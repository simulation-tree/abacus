using Data.Events;
using Data.Systems;
using Rendering.Events;
using Rendering.Systems;
using Rendering.Vulkan;
using Shaders.Events;
using Shaders.Systems;
using Simulation;
using Simulation.Systems;
using Textures.Events;
using Textures.Systems;
using Transforms.Events;
using Windows.Events;
using Windows.Systems;

public static class Bootstrap
{
    public static void Main()
    {
        using (World world = new())
        {
            //systems part of the simulation
            DataImportSystem data = new(world);
            TransformSystem transforms = new(world);
            WindowSystem windows = new(world);
            TextureImportSystem textures = new(world);
            ShaderImportSystem shaders = new(world);
            CameraSystem cameras = new(world);
            RenderEngineSystem rendering = new(world);
            rendering.RegisterSystem<VulkanRendererType>();

            //play the simulation
            Program program = new(world);
            do
            {
                world.Submit(new WindowUpdate());
                world.Submit(new TransformUpdate());
                world.Submit(new DataUpdate());
                world.Submit(new ShaderUpdate());
                world.Submit(new TextureUpdate());
                world.Submit(new CameraUpdate());
                world.Submit(new RenderUpdate());
                world.Poll();
            }
            while (program.Update());
            program.Dispose();

            //finish
            rendering.Dispose();
            cameras.Dispose();
            shaders.Dispose();
            textures.Dispose();
            windows.Dispose();
            transforms.Dispose();
            data.Dispose();
        }
    }
}