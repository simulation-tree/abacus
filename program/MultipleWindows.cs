using Cameras;
using InteractionKit;
using InteractionKit.Components;
using Rendering;
using Simulation;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly partial struct MultipleWindows : IProgram
    {
        private readonly Window firstWindow;
        private readonly Window secondWindow;
        private readonly World world;

        private unsafe MultipleWindows(World world)
        {
            this.world = world;

            LayerMask firstLayer = new LayerMask().Set(1);
            LayerMask secondLayer = new LayerMask().Set(2);

            firstWindow = new(world, "First Window", new(200, 200), new(300, 300), "vulkan", new(&OnWindowClosed));
            firstWindow.SetClearColor(new(0.2f, 0.2f, 0.4f, 1.0f));
            firstWindow.IsResizable = true;

            secondWindow = new(world, "Second Window", new(500, 200), new(300, 300), "vulkan", new(&OnWindowClosed));
            secondWindow.SetClearColor(new(0.4f, 0.2f, 0.2f, 1.0f));
            secondWindow.IsResizable = true;

            Settings settings = new(world);
            Camera firstCamera = Camera.CreateOrthographic(world, firstWindow, 1f);
            firstCamera.RenderMask = firstLayer;

            Camera secondCamera = Camera.CreateOrthographic(world, secondWindow, 1f);
            secondCamera.RenderMask = secondLayer;

            Canvas firstCanvas = new(world, settings, firstCamera, firstLayer, firstLayer);
            Canvas secondCanvas = new(world, settings, secondCamera, secondLayer, secondLayer);

            Image firstSquare = new(firstCanvas);
            firstSquare.Size = new(100, 100);
            firstSquare.Position = new(50, 50);
            firstSquare.Color = new(1, 0.5f, 0.5f, 1.0f);
            Resizable resizable = firstSquare.AsEntity().Become<Resizable>();
            resizable.Boundary = IsResizable.Boundary.All;
            resizable.SelectionMask = firstLayer;

            Image secondSquare = new(secondCanvas);
            secondSquare.Size = new(100, 100);
            secondSquare.Position = new(150, 150);
            secondSquare.Color = new(0.5f, 0.5f, 1, 1.0f);
            resizable = secondSquare.AsEntity().Become<Resizable>();
            resizable.Boundary = IsResizable.Boundary.All;
            resizable.SelectionMask = secondLayer;
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new MultipleWindows(world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (firstWindow.IsDestroyed() && secondWindow.IsDestroyed())
            {
                return StatusCode.Success(0);
            }

            SharedFunctions.UpdateUISettings(world);
            return StatusCode.Continue;
        }

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }
    }
}