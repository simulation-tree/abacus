using Simulation;
using System;
using System.Runtime.InteropServices;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly partial struct JustOneWindow : IProgram<JustOneWindow>
    {
        private readonly Window window;

        private readonly World World => window.world;

        readonly void IProgram<JustOneWindow>.Start(ref JustOneWindow program, in Simulator simulator, in World world)
        {
            program = new JustOneWindow(world);
        }

        readonly StatusCode IProgram<JustOneWindow>.Update(in TimeSpan delta)
        {
            if (!IsAnyWindowOpen(World))
            {
                return StatusCode.Success(0);
            }

            return StatusCode.Continue;
        }

        readonly void IProgram<JustOneWindow>.Finish(in StatusCode statusCode)
        {
            if (!window.IsDestroyed)
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