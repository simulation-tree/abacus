using Simulation;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly partial struct JustOneWindow : IProgram
    {
        private readonly Window window;

        private readonly World World => window.GetWorld();

        void IProgram.Initialize(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new JustOneWindow(world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (!IsAnyWindowOpen(World))
            {
                return StatusCode.Success(1);
            }

            return StatusCode.Continue;
        }

        void IDisposable.Dispose()
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }
        }

        private unsafe JustOneWindow(World world)
        {
            window = new(world, "Just One Window", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.IsResizable = true;
        }

        private static bool IsAnyWindowOpen(World world)
        {
            return world.CountEntities<Window>() > 0;
        }

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }
    }
}