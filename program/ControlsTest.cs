using Data;
using InputDevices;
using InteractionKit;
using InteractionKit.Components;
using InteractionKit.Functions;
using Programs;
using Programs.Functions;
using Rendering;
using Rendering.Components;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Transforms.Components;
using Unmanaged;
using Windows;

namespace Abacus
{
    public struct ControlsTest : IDisposable, IProgramType
    {
        private readonly World world;

        public unsafe ControlsTest(World world)
        {
            this.world = world;
            Window window = new(world, "Editor", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.IsResizable = true;

            Settings settings = new(world);
            Camera camera = new(world, window, new CameraOrthographicSize(1f));
            Canvas canvas = new(world, camera);

            VirtualWindow box = VirtualWindow.Create<ControlsDemoWindow>(world, canvas);
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
                return 0;
            }

            if (!IsAnyWindowOpen())
            {
                return 0;
            }

            MakeFirstMouseAPointer();
            UpdateInteractiveContext();
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
                if (!mouse.Is(Definition.Get<Pointer>()))
                {
                    mouse.Become(Definition.Get<Pointer>());
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
                Settings settings = world.GetFirst<Settings>();
                USpan<Keyboard.Button> pressedBuffer = stackalloc Keyboard.Button[128];
                uint pressedCount = keyboard.GetPressedControls(pressedBuffer);
                USpan<char> pressed = stackalloc char[(int)pressedCount];
                for (uint i = 0; i < pressedCount; i++)
                {
                    Keyboard.Button pressedControl = pressedBuffer[i];
                    pressed[i] = pressedControl.GetCharacter();
                }

                settings.SetPressedCharacters(pressed);
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
                ControlsTest program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref ControlsTest program = ref allocation.Read<ControlsTest>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref ControlsTest program = ref allocation.Read<ControlsTest>();
                return program.Update(delta);
            }
        }

        public readonly struct ControlsDemoWindow : IVirtualWindow
        {
            readonly FixedString IVirtualWindow.Title => "Controls Demo";
            readonly unsafe VirtualWindowCloseFunction IVirtualWindow.CloseCallback => new(&Closed);

            readonly unsafe void IVirtualWindow.OnCreated(VirtualWindow window, Canvas canvas)
            {
                World world = window.GetWorld();

                float singleLineHeight = 24f;
                float gap = 4f;
                float y = -gap;

                Label testLabel = new(world, canvas, "Hello, World!");
                testLabel.Parent = window.Container;
                testLabel.Anchor = Anchor.TopLeft;
                testLabel.Color = Color.Black;
                testLabel.Position = new(4f, y);
                testLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;

                Button testButton = new(world, new(&PressedTestButton), canvas);
                testButton.Parent = window.Container;
                testButton.Color = new Color(0.2f, 0.2f, 0.2f);
                testButton.Anchor = Anchor.TopLeft;
                testButton.Pivot = new(0f, 1f, 0f);
                testButton.Size = new(180f, singleLineHeight);
                testButton.Position = new(4f, y);

                Label testButtonLabel = new(world, canvas, "Press count: 0");
                testButtonLabel.Parent = testButton.AsEntity();
                testButtonLabel.Anchor = Anchor.TopLeft;
                testButtonLabel.Position = new(4f, -4f);
                testButtonLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;

                Toggle testToggle = new(world, canvas);
                testToggle.Parent = window.Container;
                testToggle.Position = new(4f, y);
                testToggle.Size = new(24, singleLineHeight);
                testToggle.Anchor = Anchor.TopLeft;
                testToggle.Pivot = new(0f, 1f, 0f);
                testToggle.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                testToggle.CheckmarkColor = Color.White;

                y -= singleLineHeight + gap;

                ScrollBar horizontalScrollBar = new(world, canvas, Vector2.UnitX, 0.25f);
                horizontalScrollBar.Parent = window.Container;
                horizontalScrollBar.Position = new(4f, y);
                horizontalScrollBar.Size = new(180f, singleLineHeight);
                horizontalScrollBar.Anchor = Anchor.TopLeft;
                horizontalScrollBar.Pivot = new(0f, 1f, 0f);
                horizontalScrollBar.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                horizontalScrollBar.ScrollHandleColor = Color.White;

                y -= singleLineHeight + gap;

                Dropdown testDropdown = new(world, canvas);
                testDropdown.Parent = window.Container;
                testDropdown.Position = new(4f, y);
                testDropdown.Size = new(180f, singleLineHeight);
                testDropdown.Anchor = Anchor.TopLeft;
                testDropdown.Pivot = new(0f, 1f, 0f);
                testDropdown.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                testDropdown.LabelColor = Color.White;
                testDropdown.TriangleColor = Color.White;

                Menu testDropdownMenu = testDropdown.Menu;
                testDropdownMenu.AddOption("Option A", canvas);
                testDropdownMenu.AddOption("Option B", canvas);
                OptionPath lastOption = testDropdownMenu.AddOption("Option C", canvas);
                testDropdownMenu.AddOption("Option D/Apple", canvas);
                testDropdownMenu.AddOption("Option D/Banana", canvas);
                testDropdownMenu.AddOption("Option D/Cherry", canvas);
                testDropdownMenu.AddOption("Option D/More.../Toyota", canvas);
                testDropdownMenu.AddOption("Option D/More.../Honda", canvas);
                testDropdownMenu.AddOption("Option D/More.../Hyndai", canvas);
                testDropdownMenu.AddOption("Option D/More.../Mitsubishi", canvas);

                testDropdown.SelectedOption = lastOption;
                testDropdown.Callback = new(&DropdownOptionChanged);

                [UnmanagedCallersOnly]
                static void DropdownOptionChanged(Dropdown dropdown, uint previous, uint current)
                {
                    MenuOption option = dropdown.Options[current];
                    Debug.WriteLine($"Selected option: {option.text}");
                }

                y -= singleLineHeight + gap;

                TextField testTextField = new(world, canvas);
                testTextField.Parent = window.Container;
                testTextField.Position = new(4f, y);
                testTextField.Size = new(180f, singleLineHeight);
                testTextField.Anchor = Anchor.TopLeft;
                testTextField.Pivot = new(0f, 1f, 0f);
                testTextField.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                testTextField.TextColor = Color.White;

                y -= singleLineHeight + gap;

                Entity dataEntity = new(world);
                dataEntity.AddComponent(false);
                dataEntity.AddComponent(0f);
                dataEntity.AddComponent(new FixedString("babash"));

                ControlField testControlField = new(world, canvas, "Test", dataEntity, RuntimeType.Get<bool>());
                testControlField.LabelColor = Color.Black;
                testControlField.Parent = window.Container;
                testControlField.Anchor = Anchor.TopLeft;
                testControlField.Pivot = new(0f, 1f, 0f);
                testControlField.Position = new(4f, y);
                testControlField.Size = new(180f, singleLineHeight);

                y -= singleLineHeight + gap;

                Tree testTree = new(world, canvas);
                testTree.Parent = window.Container;
                testTree.Position = new(4f, y);
                testTree.Size = new(180f, singleLineHeight);
                testTree.Anchor = Anchor.TopLeft;
                testTree.Pivot = new(0f, 1f, 0f);

                testTree.AddLeaf("Game Object");

                TreeNode canvasLeaf = testTree.AddLeaf("Canvas");
                canvasLeaf.AddLeaf("Button");
                canvasLeaf.AddLeaf("Toggle");

                TreeNode playerLeaf = canvasLeaf.AddLeaf("Player");
                playerLeaf.AddLeaf("Health");
                playerLeaf.AddLeaf("Score");

                testTree.AddLeaf("Game Object (1)");
                testTree.AddLeaf("Game Object (2)");

                for (int i = 0; i < 20; i++)
                {
                    Image box = new(world, canvas);
                    box.Parent = window.Container;
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
                for (uint i = 0; i < children.Length; i++)
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
                for (uint i = 0; i < children.Length; i++)
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
}