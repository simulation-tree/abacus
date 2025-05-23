using InputDevices;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows;
using Worlds;

namespace Abacus
{
    public class WindowThatFollowsTheMouse : Program
    {
        private readonly Window followerWindow;

        public unsafe WindowThatFollowsTheMouse(Simulator simulator) : base(simulator)
        {
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

        public override void Dispose()
        {
            if (!followerWindow.IsDestroyed)
            {
                followerWindow.Dispose();
            }
        }

        public override bool Update(Simulator simulator, double deltaTime)
        {
            if (followerWindow.IsDestroyed)
            {
                return false;
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
                    followerWindow.Position = Vector2.Lerp(followerWindow.Position, desiredPosition, (float)deltaTime * 2f);
                }
                else
                {
                    Vector2 positionDelta = desiredPosition - followerWindow.Position;
                    if (positionDelta.LengthSquared() > 0)
                    {
                        positionDelta = Vector2.Normalize(positionDelta);
                    }

                    followerWindow.Position += positionDelta * (float)deltaTime * 120f;
                }
            }

            return true;
        }
    }
}
