using InputDevices;
using Simulation;
using Simulation.Functions;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly struct WindowThatFollowsTheMouse : IProgram
    {
        private readonly World world;
        private readonly Window followerWindow;

        unsafe readonly StartProgram IProgram.Start => new(&Start);
        unsafe readonly UpdateProgram IProgram.Update => new(&Update);
        unsafe readonly FinishProgram IProgram.Finish => new(&Finish);

        [UnmanagedCallersOnly]
        private static void Start(Simulator simulator, Allocation allocation, World world)
        {
            allocation.Write(new WindowThatFollowsTheMouse(world));
        }

        [UnmanagedCallersOnly]
        private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            ref WindowThatFollowsTheMouse program = ref allocation.Read<WindowThatFollowsTheMouse>();
            return program.Update(delta);
        }

        [UnmanagedCallersOnly]
        private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
            ref WindowThatFollowsTheMouse program = ref allocation.Read<WindowThatFollowsTheMouse>();
            program.CleanUp();
        }

        private unsafe WindowThatFollowsTheMouse(World world)
        {
            this.world = world;
            followerWindow = new(world, "Fly", default, new(100, 100), "vulkan", new(&WindowClosed));
            followerWindow.IsBorderless = true;
            followerWindow.AlwaysOnTop = true;

            new GlobalMouse(world);

            [UnmanagedCallersOnly]
            static void WindowClosed(Window window)
            {
                window.Dispose();
            }
        }

        private readonly uint Update(TimeSpan delta)
        {
            if (followerWindow.IsDestroyed())
            {
                return 1;
            }

            bool holdingShift = false;
            if (world.TryGetFirst(out Keyboard keyboard))
            {
                holdingShift = keyboard.IsPressed(Keyboard.Button.LeftShift);
            }

            if (world.TryGetFirst(out GlobalMouse mouse))
            {
                Vector2 mousePosition = mouse.Position;
                Vector2 desiredPosition = mousePosition - followerWindow.Size * 0.5f;
                if (holdingShift)
                {
                    followerWindow.Position = Vector2.Lerp(followerWindow.Position, desiredPosition, (float)delta.TotalSeconds * 2f);
                }
                else
                {
                    Vector2 positionDelta = desiredPosition - followerWindow.Position;
                    if (positionDelta.LengthSquared() > 0)
                    {
                        positionDelta = Vector2.Normalize(positionDelta);
                    }

                    followerWindow.Position += positionDelta * (float)delta.TotalSeconds * 120f;
                }
            }

            return 0;
        }

        private readonly void CleanUp()
        {
            if (!followerWindow.IsDestroyed())
            {
                followerWindow.Dispose();
            }
        }
    }
}
