using Cameras;
using InteractionKit;
using InteractionKit.Components;
using Rendering;
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
    public unsafe partial struct WindowCreationTest : IProgram
    {
        private readonly World world;
        private readonly Settings settings;
        private TimeSpan time;
        private byte state;

        private WindowCreationTest(World world)
        {
            this.world = world;
            settings = new(world);
            time = TimeSpan.FromSeconds(0.5f);
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
                    CreateWindow("First Window", new(300, 300), new(50, 50), new(1f, 0f, 0f, 1f), 0);
                    time = TimeSpan.FromSeconds(2f);
                }
                else if (state == 2)
                {
                    DestroyAllWindows();
                    time = TimeSpan.FromSeconds(0.5f);
                }
                else if (state == 3)
                {
                    CreateWindow("Second Window", new(200, 300), new(0, 0), new(0f, 1f, 0f, 1f), 1);
                    CreateWindow("Third Window", new(400, 300), new(100, 100), new(0f, 0f, 1f, 1f), 2);
                    time = TimeSpan.FromSeconds(2f);
                }
                else if (state == 4)
                {
                    DestroyAllWindows();
                    time = TimeSpan.FromSeconds(0.5f);
                }
                else if (state == 5)
                {
                    CreateWindow("Fourth Window", new(300, 300), new(50, 50), new(1f, 1f, 0f, 1f), 1);
                    time = TimeSpan.FromSeconds(2f);
                }
                else if (state == 6)
                {
                    return StatusCode.Success(0);
                }
            }

            if (state == 1 || state == 3)
            {
                if (world.CountEntities<Window>() == 0)
                {
                    return StatusCode.Success(1);
                }
            }

            SharedFunctions.UpdateUISettings(world);
            return StatusCode.Continue;
        }

        private readonly void CreateWindow(FixedString title, Vector2 windowPosition, Vector2 squarePosition, Vector4 color, Layer layer)
        {
            LayerMask layerMask = new LayerMask().Set(layer);

            Window window = new(world, title, windowPosition, new(200, 200), "vulkan", new(&OnWindowClosed));
            window.SetClearColor(Vector4.Lerp(new(0f, 0f, 0f, 1f), color, 0.3f));
            window.IsResizable = true;

            Camera camera = Camera.CreateOrthographic(world, window, 1f, layerMask);
            camera.SetParent(window);

            Canvas canvas = new(world, settings, camera, layerMask, layerMask);
            canvas.SetParent(window);

            Image square = new(canvas);
            square.Size = new(100, 100);
            square.Position = squarePosition;
            square.Color = color;

            Trace.WriteLine(square.GetRenderMask());

            Resizable resizable = square.AsEntity().Become<Resizable>();
            resizable.Boundary = IsResizable.Boundary.All;
            resizable.SelectionMask = layerMask;
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