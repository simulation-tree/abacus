using Data;
using InputDevices;
using InteractionKit;
using Programs;
using Programs.Functions;
using Rendering;
using Rendering.Components;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;

namespace Abacus
{
    public struct SelectionTest : IDisposable, IProgramType
    {
        private readonly World world;

        public unsafe SelectionTest(World world)
        {
            this.world = world;
            Window window = new(world, "Selection Test", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.IsResizable = true;

            Settings settings = new(world);
            Camera camera = new(world, window, new CameraOrthographicSize(1f));
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
            static void Pressed(World world, uint buttonEntity)
            {
                Debug.WriteLine($"Button {buttonEntity} pressed");
            }
        }

        public readonly void Dispose()
        {
        }

        public readonly uint Update(TimeSpan delta)
        {
            if (!IsAnyWindowOpen())
            {
                return 0;
            }

            MakeFirstMouseAPointer();
            UpdateInteractiveContext();
            return 1;
        }

        private readonly bool IsAnyWindowOpen()
        {
            return world.TryGetFirst(out Window _);
        }

        private readonly void MakeFirstMouseAPointer()
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

        private readonly void UpdateInteractiveContext()
        {
            if (world.TryGetFirst(out Keyboard keyboard))
            {
                //context.SelectMultiple = keyboard.IsPressed(Keyboard.Button.LeftShift);
            }
        }

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(World world, uint windowEntity)
        {
            world.DestroyEntity(windowEntity);
        }

        unsafe readonly (StartFunction, FinishFunction, UpdateFunction) IProgramType.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                SelectionTest program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref SelectionTest program = ref allocation.Read<SelectionTest>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref SelectionTest program = ref allocation.Read<SelectionTest>();
                return program.Update(delta);
            }
        }
    }
}