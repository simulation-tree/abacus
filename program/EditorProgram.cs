using Data;
using InputDevices;
using InteractionKit;
using InteractionKit.Components;
using InteractionKit.Functions;
using Programs;
using Rendering;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Transforms.Components;
using Unmanaged;
using Windows;

namespace Abacus
{
    public struct EditorProgram : IDisposable, IProgramType
    {
        private readonly World world;
        private readonly InteractiveContext context;

        public unsafe EditorProgram(World world)
        {
            this.world = world;
            Window window = new(world, "Editor", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.IsResizable = true;
            //window.IsBorderless = true;
            //window.IsMaximized = true;

            Camera camera = new(world, window.destination, new CameraOrthographicSize(1f));
            Canvas canvas = new(world, camera);
            context = new(canvas);

            VirtualWindow box = VirtualWindow.Create<ControlsDemoWindow>(world, context);
            box.Size = new(300, 300);
            box.Position = new(40, 40);
            //box.Anchor = new(new(2f, true), new(2f, true), new(0f, false), new(2f, true), new(2f, true), new(0f, false));
        }

        public readonly void Dispose()
        {
        }

        public uint Update(TimeSpan delta)
        {
            if (!IsAnyVirtualWindowOpen())
            {
                return default;
            }

            if (!IsAnyWindowOpen())
            {
                return default;
            }

            MakeFirstMouseAPointer();
            return 1;
        }

        private readonly bool IsAnyVirtualWindowOpen()
        {
            return world.TryGetFirst(out VirtualWindow _);
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
                EditorProgram program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref EditorProgram program = ref allocation.Read<EditorProgram>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref EditorProgram program = ref allocation.Read<EditorProgram>();
                return program.Update(delta);
            }
        }
    }

    public unsafe readonly struct ControlsDemoWindow : IVirtualWindow
    {
        readonly FixedString IVirtualWindow.Title => "Controls Demo";
        readonly VirtualWindowCloseFunction IVirtualWindow.CloseCallback => new(&Closed);

        readonly void IVirtualWindow.OnCreated(VirtualWindow window, InteractiveContext context)
        {
            World world = window.GetWorld();

            float singleLineHeight = 24f;
            float gap = 4f;
            float y = -gap;

            Label testLabel = new(world, context, "Hello, World!");
            testLabel.Parent = window.Container;
            testLabel.Anchor = Anchor.TopLeft;
            testLabel.Color = Color.Black;
            testLabel.Position = new(4f, y);

            y -= singleLineHeight + gap;

            Button testButton = new(world, new(&PressedTestButton), context);
            testButton.Parent = window.Container;
            testButton.Color = new Color(0.2f, 0.2f, 0.2f);
            testButton.Anchor = Anchor.TopLeft;
            testButton.Pivot = new(0f, 1f, 0f);
            testButton.Size = new(180f, singleLineHeight);
            testButton.Position = new(4f, y);

            Label testButtonLabel = new(world, context, "Press count: 0");
            testButtonLabel.Parent = testButton.AsEntity();
            testButtonLabel.Anchor = Anchor.TopLeft;
            testButtonLabel.Position = new(4f, -4f);

            y -= singleLineHeight + gap;

            Toggle testToggle = new(world, context);
            testToggle.Parent = window.Container;
            testToggle.Position = new(4f, y);
            testToggle.Size = new(24, singleLineHeight);
            testToggle.Anchor = Anchor.TopLeft;
            testToggle.Pivot = new(0f, 1f, 0f);
            testToggle.BackgroundColor = new(0.2f, 0.2f, 0.2f);
            testToggle.CheckmarkColor = Color.White;

            y -= singleLineHeight + gap;

            ScrollBar horizontalScrollBar = new(world, context, Vector2.UnitX, 0.25f);
            horizontalScrollBar.Parent = window.Container;
            horizontalScrollBar.Position = new(4f, y);
            horizontalScrollBar.Size = new(180f, singleLineHeight);
            horizontalScrollBar.Anchor = Anchor.TopLeft;
            horizontalScrollBar.Pivot = new(0f, 1f, 0f);
            horizontalScrollBar.BackgroundColor = new(0.2f, 0.2f, 0.2f);
            horizontalScrollBar.ScrollHandleColor = Color.White;

            ScrollBar verticalScrollBar = new(world, context, Vector2.UnitY, 0.666f);
            verticalScrollBar.Parent = window.Container;
            verticalScrollBar.Position = new(-gap, -gap);
            verticalScrollBar.Size = new(24f, 270f - gap);
            verticalScrollBar.Anchor = Anchor.TopRight;
            verticalScrollBar.Pivot = new(1f, 1f, 0f);
            verticalScrollBar.BackgroundColor = new(0.2f, 0.2f, 0.2f);
            verticalScrollBar.ScrollHandleColor = Color.White;

            View view = new(world, context);
            view.Parent = window.Container;
            view.ViewPosition = new(0f, 0f);
            view.Anchor = new(new(0f, false), new(0, false), default, new(1f, false), new(1f, false), default);
            view.ContentSize = new(100f, 20 * 26f);
            view.SetScrollBar(verticalScrollBar);

            for (int i = 0; i < 20; i++)
            {
                Box box = new(world, context);
                box.Parent = view.Content;
                box.Size = new(60f, 20f);
                box.Position = new(200f, i * 26f);
                box.Color = Color.FromHSV((i * 0.1f) % 1, 1, 1);
            }
        }

        [UnmanagedCallersOnly]
        private static void PressedTestButton(World world, uint buttonEntity)
        {
            uint containerEntity = world.GetParent(buttonEntity);
            USpan<uint> children = world.GetChildren(containerEntity);
            bool toggleValue = default;
            for (uint i = 0; i < children.length; i++)
            {
                uint child = children[i];
                if (world.ContainsComponent<IsToggle>(child))
                {
                    Toggle toggle = new(world, child);
                    //toggle.Value = !toggle.Value;
                    toggleValue = toggle.Value;
                    break;
                }
            }

            children = world.GetChildren(buttonEntity);
            for (uint i = 0; i < children.length; i++)
            {
                uint child = children[i];
                if (world.ContainsComponent<IsLabel>(child))
                {
                    Label label = new(world, child);
                    USpan<char> text = label.Text;
                    uint startIndex = text.IndexOf(':') + 1;
                    int countValue = int.Parse(text.Slice(startIndex).AsSystemSpan());
                    USpan<char> countText = stackalloc char[10];

                    if (!toggleValue)
                    {
                        countValue++;
                    }
                    else
                    {
                        countValue--;
                    }

                    uint countTextLength = countValue.ToString(countText);
                    FixedString textValue = new(text);
                    textValue.Length = startIndex + 1;
                    textValue.Append(countText.Slice(0, countTextLength));
                    label.SetText(textValue);
                    break;
                }
            }
        }

        [UnmanagedCallersOnly]
        private static void Closed(VirtualWindow virtualWindow)
        {
            virtualWindow.Destroy();
        }
    }
}