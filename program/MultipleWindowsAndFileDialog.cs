using Cameras;
using FileDialogs;
using FileDialogs.Functions;
using InputDevices;
using UI;
using UI.Components;
using Rendering;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Transforms.Components;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly partial struct MultipleWindowsAndFileDialog : IProgram
    {
        private readonly Window firstWindow;
        private readonly Window secondWindow;
        private readonly Canvas firstCanvas;
        private readonly Canvas secondCanvas;
        private readonly World world;

        private unsafe MultipleWindowsAndFileDialog(World world)
        {
            this.world = world;

            LayerMask firstLayer = new LayerMask().Set(1);
            LayerMask secondLayer = new LayerMask().Set(2);

            firstWindow = new(world, "First Window", new(200, 200), new(300, 300), "vulkan", new(&OnWindowClosed));
            firstWindow.ClearColor = new(0.2f, 0.2f, 0.4f, 1.0f);
            firstWindow.IsResizable = true;

            secondWindow = new(world, "Second Window", new(500, 200), new(300, 300), "vulkan", new(&OnWindowClosed));
            secondWindow.ClearColor = new(0.4f, 0.2f, 0.2f, 1.0f);
            secondWindow.IsResizable = true;

            Settings settings = new(world);

            Camera firstCamera = Camera.CreateOrthographic(world, firstWindow, 1f);
            firstCamera.RenderMask = firstLayer;
            firstCamera.SetParent(firstWindow);

            Camera secondCamera = Camera.CreateOrthographic(world, secondWindow, 1f);
            secondCamera.RenderMask = secondLayer;
            secondCamera.SetParent(secondWindow);

            firstCanvas = new(world, settings, firstCamera, firstLayer, firstLayer);
            firstCamera.SetParent(firstWindow);

            secondCanvas = new(world, settings, secondCamera, secondLayer, secondLayer);
            secondCamera.SetParent(secondWindow);

            Image firstSquare = new(firstCanvas);
            firstSquare.Size = new(100, 100);
            firstSquare.Position = new(50, 50);
            firstSquare.Color = new(1, 0.5f, 0.5f, 1.0f);

            Resizable resizable = firstSquare.Become<Resizable>();
            resizable.Boundary = IsResizable.Boundary.All;
            resizable.SelectionMask = firstLayer;

            Label label = new(firstCanvas, "Z = open multiple files\nX = open file\nC = save file\nV = choose directory");
            label.Position = new(50, 250);
            label.Color = Color.Black;
            label.Z = Settings.ZScale * 2f;

            Image secondSquare = new(secondCanvas);
            secondSquare.Size = new(100, 100);
            secondSquare.Position = new(150, 150);
            secondSquare.Color = new(0.5f, 0.5f, 1, 1.0f);

            resizable = secondSquare.Become<Resizable>();
            resizable.Boundary = IsResizable.Boundary.All;
            resizable.SelectionMask = secondLayer;
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new MultipleWindowsAndFileDialog(world));
        }

        unsafe StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (firstWindow.IsDestroyed && secondWindow.IsDestroyed)
            {
                return StatusCode.Success(0);
            }

            foreach (Keyboard keyboard in world.GetAll<Keyboard>())
            {
                if (keyboard.WasPressed(Keyboard.Button.Z))
                {
                    FileDialog.OpenMultipleFiles(world, new(&ChosenFile), userData: secondCanvas.value);
                }
                else if (keyboard.WasPressed(Keyboard.Button.X))
                {
                    FileDialog.OpenFile(world, new(&ChosenFile), userData: secondCanvas.value);
                }
                else if (keyboard.WasPressed(Keyboard.Button.C))
                {
                    FileDialog.SaveFile(world, new(&ChosenFile), userData: secondCanvas.value);
                }
                else if (keyboard.WasPressed(Keyboard.Button.V))
                {
                    FileDialog.ChooseDirectory(world, new(&ChosenFile), userData: secondCanvas.value);
                }
            }

            SharedFunctions.UpdateUISettings(world);
            return StatusCode.Continue;
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
        }

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }

        [UnmanagedCallersOnly]
        private static void ChosenFile(Chosen.Input input)
        {
            Canvas secondCanvas = new Entity(input.world, (uint)input.userData).As<Canvas>();

            DestroyAllLabels(secondCanvas);

            Label title = new(secondCanvas, input.type.ToString());
            title.Position = new(10, -10);
            title.Color = Color.Black;
            title.Anchor = Anchor.TopLeft;
            title.Pivot = new(0, 1, 0);

            Vector2 position = new(10, -50);
            if (input.status == FileDialogStatus.Failed)
            {
                Label label = new(secondCanvas, input.Paths[0]);
                label.Position = position;
                label.Color = Color.Black;
                label.Anchor = Anchor.TopLeft;
                label.Pivot = new(0, 1, 0);
            }
            else if (input.status == FileDialogStatus.Cancelled)
            {
                Label label = new(secondCanvas, "Cancelled");
                label.Position = position;
                label.Color = Color.Black;
                label.Anchor = Anchor.TopLeft;
                label.Pivot = new(0, 1, 0);
            }
            else
            {
                foreach (Text path in input.Paths)
                {
                    Label label = new(secondCanvas, path);
                    label.Position = position;
                    label.Color = Color.Black;
                    label.Anchor = Anchor.TopLeft;
                    label.Pivot = new(0, 1, 0);
                    position.Y -= 20f;
                }
            }
        }

        private static void DestroyAllLabels(Canvas canvas)
        {
            World world = canvas.world;
            USpan<uint> toDestroy = stackalloc uint[256];
            uint count = 0;
            foreach (Label label in world.GetAll<Label>())
            {
                Canvas labelCanvas = label.GetCanvas();
                if (labelCanvas == canvas)
                {
                    toDestroy[count++] = label.value;
                }
            }

            for (uint i = 0; i < count; i++)
            {
                world.DestroyEntity(toDestroy[i]);
            }
        }
    }
}