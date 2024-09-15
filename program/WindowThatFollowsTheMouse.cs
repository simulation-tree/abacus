using InputDevices;
using Programs;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;

namespace Abacus
{
    public struct WindowThatFollowsTheMouse : IDisposable, IProgramType
    {
        private readonly World world;
        private readonly Window followerWindow;

        public unsafe WindowThatFollowsTheMouse(World world)
        {
            this.world = world;
            followerWindow = new(world, "Fly", default, new(100, 100), "vulkan", new(&WindowClosed));
            followerWindow.IsBorderless = true;
            followerWindow.AlwaysOnTop = true;

            new GlobalMouse(world);

            [UnmanagedCallersOnly]
            static void WindowClosed(World world, uint windowEntity)
            {
                world.DestroyEntity(windowEntity);
            }
        }

        public uint Update(TimeSpan delta)
        {
            if (followerWindow.IsDestroyed())
            {
                return 0;
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

            return 1;
        }

        public void Dispose()
        {
            if (!followerWindow.IsDestroyed())
            {
                followerWindow.Destroy();
            }
        }

        readonly unsafe (StartFunction, FinishFunction, UpdateFunction) IProgramType.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                WindowThatFollowsTheMouse program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref WindowThatFollowsTheMouse program = ref allocation.Read<WindowThatFollowsTheMouse>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref WindowThatFollowsTheMouse program = ref allocation.Read<WindowThatFollowsTheMouse>();
                return program.Update(delta);
            }
        }
    }
}
