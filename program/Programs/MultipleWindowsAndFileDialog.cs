using Cameras;
using Data;
using FileDialogs;
using FileDialogs.Functions;
using InputDevices;
using Rendering;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Transforms.Components;
using UI;
using UI.Components;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public class MultipleWindowsAndFileDialog : Program
    {
        private readonly Window firstWindow;
        private readonly Window secondWindow;
        private readonly Canvas firstCanvas;
        private readonly Canvas secondCanvas;

        public MultipleWindowsAndFileDialog(Application application) : base(application)
        {
            LayerMask firstLayer = new(1);
            LayerMask secondLayer = new(2);

            firstWindow = new(world, "First Window", new(200, 200), new(300, 300), "vulkan");
            firstWindow.ClearColor = new(0.2f, 0.2f, 0.4f, 1.0f);
            firstWindow.IsResizable = true;

            secondWindow = new(world, "Second Window", new(500, 200), new(300, 300), "vulkan");
            secondWindow.ClearColor = new(0.4f, 0.2f, 0.2f, 1.0f);
            secondWindow.IsResizable = true;

            Settings settings = new(world);

            Camera firstCamera = Camera.CreateOrthographic(world, firstWindow, 1f);
            firstCamera.RenderMask = firstLayer;
            firstCamera.SetParent(firstWindow);

            Camera secondCamera = Camera.CreateOrthographic(world, secondWindow, 1f);
            secondCamera.RenderMask = secondLayer;
            secondCamera.SetParent(secondWindow);

            firstCanvas = new(settings, firstCamera, firstLayer, firstLayer);
            firstCamera.SetParent(firstWindow);

            secondCanvas = new(settings, secondCamera, secondLayer, secondLayer);
            secondCamera.SetParent(secondWindow);

            Image firstSquare = new(firstCanvas);
            firstSquare.Size = new(100, 100);
            firstSquare.Position = new(50, 50);
            firstSquare.Color = new(1, 0.5f, 0.5f, 1.0f);

            Resizable resizable = firstSquare.Become<Resizable>();
            resizable.Boundary = IsResizable.EdgeMask.All;
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
            resizable.Boundary = IsResizable.EdgeMask.All;
            resizable.SelectionMask = secondLayer;
        }

        public override void Dispose()
        {
        }

        public unsafe override bool Update(double deltaTime)
        {
            if (firstWindow.IsDestroyed && secondWindow.IsDestroyed)
            {
                return false;
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
            return true;
        }

        [UnmanagedCallersOnly]
        private static void ChosenFile(Chosen.Input input)
        {
            Canvas secondCanvas = Entity.Get<Canvas>(input.world, (uint)input.userData);

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
            Span<uint> toDestroy = stackalloc uint[256];
            int count = 0;
            foreach (Label label in world.GetAll<Label>())
            {
                Canvas labelCanvas = label.GetCanvas();
                if (labelCanvas == canvas)
                {
                    toDestroy[count++] = label.value;
                }
            }

            for (int i = 0; i < count; i++)
            {
                world.DestroyEntity(toDestroy[i]);
            }
        }
    }
}