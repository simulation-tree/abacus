using InputDevices;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly partial struct WindowThatFollowsTheMouse : IProgram<WindowThatFollowsTheMouse>
    {
        private readonly World world;
        private readonly Window followerWindow;

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

        void IProgram<WindowThatFollowsTheMouse>.Start(ref WindowThatFollowsTheMouse program, in Simulator simulator, in World world)
        {
            program = new WindowThatFollowsTheMouse(world);
        }

        StatusCode IProgram<WindowThatFollowsTheMouse>.Update(in TimeSpan delta)
        {
            if (followerWindow.IsDestroyed)
            {
                return StatusCode.Success(0);
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

            return StatusCode.Continue;
        }

        readonly void IProgram<WindowThatFollowsTheMouse>.Finish(in StatusCode statusCode)
        {
            if (!followerWindow.IsDestroyed)
            {
                followerWindow.Dispose();
            }
        }
    }
}
