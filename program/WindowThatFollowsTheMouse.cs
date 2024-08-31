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
    public struct WindowThatFollowsTheMouse : IDisposable, IProgram
    {
        private readonly World world;
        private readonly Window followerWindow;

        public unsafe WindowThatFollowsTheMouse(World world)
        {
            this.world = world;
            followerWindow = new(world, "Fly", default, new(100, 100), "vulkan", new(&WindowClosed));
            followerWindow.IsBorderless = true;

            [UnmanagedCallersOnly]
            static void WindowClosed(World world, uint windowEntity)
            {
                world.DestroyEntity(windowEntity);
            }
        }

        public void Dispose()
        {
            if (!followerWindow.IsDestroyed)
            {
                followerWindow.Destroy();
            }
        }

        public bool Update(TimeSpan delta)
        {
            if (followerWindow.IsDestroyed)
            {
                return false;
            }

            float speed = 1f;
            if (Entity.TryGetFirst(world, out Keyboard keyboard))
            {
                if (keyboard.IsPressed(Keyboard.Button.LeftShift))
                {
                    speed *= 6f;
                }
            }

            if (Entity.TryGetFirst(world, out Mouse mouse))
            {
                Vector2 mousePosition = mouse.Position;
                mousePosition.Y = followerWindow.Size.Y - mousePosition.Y;
                Vector2 desiredPosition = mousePosition - followerWindow.Size * 0.5f;
                Vector2 windowPosition = followerWindow.Position;
                windowPosition = Vector2.Lerp(windowPosition, desiredPosition, (float)delta.TotalSeconds * speed);
                followerWindow.Position = windowPosition;
            }

            return true;
        }

        readonly unsafe (StartFunction, FinishFunction, UpdateFunction) IProgram.GetFunctions()
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
                return program.Update(delta) ? 0u : 1u;
            }
        }
    }
}
