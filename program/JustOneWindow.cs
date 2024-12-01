using Simulation;
using Simulation.Functions;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly struct JustOneWindow : IProgram
    {
        private readonly Window window;

        unsafe readonly StartProgram IProgram.Start => new(&Start);
        unsafe readonly UpdateProgram IProgram.Update => new(&Update);
        unsafe readonly FinishProgram IProgram.Finish => new(&Finish);

        [UnmanagedCallersOnly]
        private static void Start(Simulator simulator, Allocation allocation, World world)
        {
            allocation.Write(new JustOneWindow(world));
        }

        [UnmanagedCallersOnly]
        private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            ref JustOneWindow program = ref allocation.Read<JustOneWindow>();
            return Update(world);
        }

        [UnmanagedCallersOnly]
        private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
            ref JustOneWindow program = ref allocation.Read<JustOneWindow>();
            program.CleanUp();
        }

        private unsafe JustOneWindow(World world)
        {
            window = new(world, "Just One Window", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.IsResizable = true;
        }

        private readonly void CleanUp()
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }
        }

        private static uint Update(World world)
        {
            if (!IsAnyWindowOpen(world))
            {
                return 1;
            }

            return 0;
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