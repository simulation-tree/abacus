using Cameras;
using Data;
using Rendering;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using UI;
using UI.Components;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public class WindowCreationTest : Program
    {
        private readonly Settings settings;
        private double time;
        private byte state;

        public WindowCreationTest(Application application) : base(application)
        {
            settings = new(world);
            time = 0.5f;
        }

        public override void Dispose()
        {
        }

        public override bool Update(double deltaTime)
        {
            time -= deltaTime;
            if (time <= 0f)
            {
                state++;
                if (state == 1)
                {
                    CreateWindow("First Window", new(300, 300), new(50, 50), new(1f, 0f, 0f, 1f), 0);
                    time = 2f;
                }
                else if (state == 2)
                {
                    DestroyAllWindows();
                    time = 0.5f;
                }
                else if (state == 3)
                {
                    CreateWindow("Second Window", new(200, 300), new(0, 0), new(0f, 1f, 0f, 1f), 1);
                    CreateWindow("Third Window", new(400, 300), new(100, 100), new(0f, 0f, 1f, 1f), 2);
                    time = 2f;
                }
                else if (state == 4)
                {
                    DestroyAllWindows();
                    time = 0.5f;
                }
                else if (state == 5)
                {
                    CreateWindow("Fourth Window", new(300, 300), new(50, 50), new(1f, 1f, 0f, 1f), 1);
                    time = 2f;
                }
                else if (state == 6)
                {
                    return false;
                }
            }

            if (state == 1 || state == 3)
            {
                if (world.CountEntities<Window>() == 0)
                {
                    return false;
                }
            }

            SharedFunctions.UpdateUISettings(world);
            return true;
        }

        private unsafe void CreateWindow(ASCIIText256 title, Vector2 windowPosition, Vector2 squarePosition, Color color, Layer layer)
        {
            LayerMask layerMask = new(layer);

            Window window = new(world, title, windowPosition, new(200, 200), "vulkan", new(&OnWindowClosed));
            window.ClearColor = Vector4.Lerp(new(0f, 0f, 0f, 1f), color.AsVector4(), 0.3f);
            window.IsResizable = true;

            Camera camera = Camera.CreateOrthographic(world, window, 1f, layerMask);
            camera.SetParent(window);

            Canvas canvas = new(settings, camera, layerMask, layerMask);
            canvas.SetParent(window);

            Image square = new(canvas);
            square.Size = new(100, 100);
            square.Position = squarePosition;
            square.Color = color;

            Resizable resizable = square.Become<Resizable>();
            resizable.Boundary = IsResizable.EdgeMask.All;
            resizable.SelectionMask = layerMask;
        }

        private void DestroyAllWindows()
        {
            Span<uint> toDestroy = stackalloc uint[8];
            int count = 0;
            foreach (Window window in world.GetAll<Window>())
            {
                toDestroy[count++] = window.value;
            }

            for (int i = 0; i < count; i++)
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