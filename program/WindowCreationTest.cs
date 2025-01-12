using Cameras;
using InteractionKit;
using InteractionKit.Components;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public unsafe partial struct WindowCreationTest : IProgram
    {
        private readonly World world;
        private TimeSpan time;
        private byte state;

        private WindowCreationTest(World world)
        {
            this.world = world;
            new Settings(world);
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new WindowCreationTest(world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            time -= delta;
            if (time.TotalSeconds <= 0f)
            {
                state++;
                if (state == 1)
                {
                    CreateWindow("First Window", new(100, 100), new(1f, 0f, 0f, 1f));
                    time = TimeSpan.FromSeconds(2f);
                }
                else if (state == 2)
                {
                    DestroyAllWindows();
                    time = TimeSpan.FromSeconds(0.5f);
                }
                else if (state == 3)
                {
                    CreateWindow("Second Window", new(200, 200), new(0f, 1f, 0f, 1f));
                    CreateWindow("Third Window", new(400, 200), new(0f, 0f, 1f, 1f));
                    time = TimeSpan.FromSeconds(15f);
                }
                else if (state == 4)
                {
                    return StatusCode.Success(1);
                }
            }

            if (state == 1 || state == 3)
            {
                if (world.CountEntities<Window>() == 0)
                {
                    return StatusCode.Success(2);
                }
            }

            SharedFunctions.UpdateUISettings(world);
            return StatusCode.Continue;
        }

        private readonly void CreateWindow(FixedString title, Vector2 position, Vector4 color)
        {
            Window window = new(world, title, position, new(200, 200), "vulkan", new(&OnWindowClosed));
            window.GetClearColor() = Vector4.Lerp(new(0f, 0f, 0f, 1f), color, 0.4f);
            window.IsResizable = true;

            Camera camera = Camera.CreateOrthographic(world, window, 1f);
            camera.Mask = state;

            Canvas canvas = new(world, camera, state, state);

            Image square = new(canvas);
            square.Size = new(100, 100);
            square.Position = new(50, 50);
            square.Color = color;

            Resizable resizable = square.AsEntity().Become<Resizable>();
            resizable.Boundary = IsResizable.Boundary.All;
            resizable.Mask = state;
        }

        private readonly void DestroyAllWindows()
        {
            USpan<uint> toDestroy = stackalloc uint[8];
            uint count = 0;
            foreach (Window window in world.GetAll<Window>())
            {
                toDestroy[count++] = window.GetEntityValue();
            }

            for (uint i = 0; i < count; i++)
            {
                world.DestroyEntity(toDestroy[i]);
            }
        }

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }
    }
}