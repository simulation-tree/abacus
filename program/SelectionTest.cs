using Cameras;
using Data;
using InputDevices;
using InteractionKit;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly partial struct SelectionTest : IProgram
    {
        private readonly Window window;

        private readonly World World => window.GetWorld();

        void IProgram.Initialize(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new SelectionTest(world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (!IsAnyWindowOpen(World))
            {
                return StatusCode.Success(1);
            }

            MakeFirstMouseAPointer(World);
            return StatusCode.Continue;
        }

        private unsafe SelectionTest(World world)
        {
            window = new(world, "Selection Test", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.IsResizable = true;

            Settings settings = new(world);
            Camera camera = Camera.CreateOrthographic(world, window, 1f);
            Canvas canvas = new(world, camera);

            Button buttonA = new(world, new(&Pressed), canvas);
            buttonA.Position = new(100, 60);
            buttonA.Size = new(32, 32);
            buttonA.Color = Color.Red;

            Button buttonB = new(world, new(&Pressed), canvas);
            buttonB.Position = new(200, 60);
            buttonB.Size = new(32, 32);
            buttonB.Color = Color.Green;

            Button buttonC = new(world, new(&Pressed), canvas);
            buttonC.Position = new(200 + 16, 60 + 16);
            buttonC.Size = new(32, 32);
            buttonC.Color = Color.Blue * new Color(1f, 1f, 1f, 0.5f);

            [UnmanagedCallersOnly]
            static void Pressed(Entity buttonEntity)
            {
                Trace.WriteLine($"Button {buttonEntity} pressed");
            }
        }

        void IDisposable.Dispose()
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }
        }

        private static bool IsAnyWindowOpen(World world)
        {
            return world.CountEntities<Window>() > 0;
        }

        private static void MakeFirstMouseAPointer(World world)
        {
            if (world.TryGetFirst(out Mouse mouse))
            {
                if (!mouse.AsEntity().Is<Pointer>())
                {
                    mouse.AsEntity().Become<Pointer>();
                }

                Pointer pointer = mouse.AsEntity().As<Pointer>();
                pointer.Position = mouse.Position;
                pointer.HasPrimaryIntent = mouse.IsPressed(Mouse.Button.LeftButton);
                pointer.HasSecondaryIntent = mouse.IsPressed(Mouse.Button.RightButton);
                Vector2 scroll = mouse.Scroll;
                if (scroll.X > 0)
                {
                    scroll.X = 1;
                }
                else if (scroll.X < 0)
                {
                    scroll.X = -1;
                }

                if (scroll.Y > 0)
                {
                    scroll.Y = 1;
                }
                else if (scroll.Y < 0)
                {
                    scroll.Y = -1;
                }

                pointer.Scroll = scroll * 0.1f;
            }
        }

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }
    }
}