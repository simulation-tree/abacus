using Cameras;
using Data;
using InputDevices;
using InteractionKit;
using Programs;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;

namespace Abacus
{
    public readonly struct SelectionTest : IProgram
    {
        private readonly Window window;

        unsafe readonly StartProgramFunction IProgram.Start => new(&Start);
        unsafe readonly UpdateProgramFunction IProgram.Update => new(&Update);
        unsafe readonly FinishProgramFunction IProgram.Finish => new(&Finish);

        [UnmanagedCallersOnly]
        private static void Start(Simulator simulator, Allocation allocation, World world)
        {
            allocation.Write(new SelectionTest(world));
        }

        [UnmanagedCallersOnly]
        private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            ref SelectionTest program = ref allocation.Read<SelectionTest>();
            return Update(world);
        }

        [UnmanagedCallersOnly]
        private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
            ref SelectionTest program = ref allocation.Read<SelectionTest>();
            program.CleanUp();
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

            MakeFirstMouseAPointer(world);
            return 0;
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