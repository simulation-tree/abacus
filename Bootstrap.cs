using Data.Events;
using Data.Systems;
using Rendering.Events;
using Rendering.Systems;
using Rendering.Vulkan;
using Shaders.Events;
using Shaders.Systems;
using Simulation;
using Simulation.Systems;
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
            ShaderImportSystem shaders = new(world);
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
                world.Submit(new RenderUpdate());
                world.Submit(new CameraUpdate());
                world.Poll();
            }
            while (program.Update());
            program.Dispose();

            //finish
            rendering.Dispose();
            shaders.Dispose();
            windows.Dispose();
            transforms.Dispose();
            data.Dispose();
        }
    }
}