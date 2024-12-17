using Cameras;
using Data;
using InputDevices;
using InteractionKit;
using InteractionKit.Components;
using InteractionKit.ControlEditors;
using InteractionKit.Functions;
using Rendering;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Transforms.Components;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public readonly partial struct ControlsTest : IProgram
    {
        private readonly Window window;
        private readonly Menu rightClickMenu;

        private readonly World World => window.GetWorld();

        private unsafe ControlsTest(World world)
        {
            window = new(world, "Editor", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.GetClearColor() = Color.Grey;
            window.IsResizable = true;

            Settings settings = new(world);
            Camera camera = Camera.CreateOrthographic(world, window, 1f);
            Canvas canvas = new(world, camera);

            VirtualWindow box = VirtualWindow.Create<ControlsDemoWindow>(world, canvas);
            box.Size = new(300, 300);
            box.Position = new(40, 40);
            //box.Anchor = new(new(2f, true), new(2f, true), new(0f, false), new(2f, true), new(2f, true), new(0f, false));

            Image image = new(canvas);
            image.Size = new(200, 200);
            image.Position = new(100, 100);

            Label anotherLabel = new(canvas, "Hello there\nWith another line\nNice");
            anotherLabel.Position = new(105, 150);
            anotherLabel.Color = Color.Black;
            anotherLabel.Z = Settings.ZScale;

            rightClickMenu = new(canvas, new(&ChoseMenuOption));
            rightClickMenu.AddOption("Stop");
            rightClickMenu.AddOption("And Listen");
            rightClickMenu.AddOption("OoOoh");
            rightClickMenu.AddOption("Deep.../First");
            rightClickMenu.AddOption("Deep.../Second");
            rightClickMenu.AddOption("Deep.../Third");
            rightClickMenu.AddOption("Deep.../Apple");
            rightClickMenu.AddOption("Deep.../Banana");
            rightClickMenu.AddOption("Deep.../Car");
            rightClickMenu.Size = new(100, settings.SingleLineHeight);
            rightClickMenu.Position = new(200, 300);
            rightClickMenu.Pivot = new(0, 1f, 0f);
            rightClickMenu.IsExpanded = false;

            DropShadow dropShadow = new(canvas, image);
        }

        [UnmanagedCallersOnly]
        private static void ChoseMenuOption(MenuOption option)
        {
            Trace.WriteLine($"Chose option: {option}");
            option.rootMenu.IsExpanded = false;
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new ControlsTest(world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (!IsAnyVirtualWindowOpen(World))
            {
                return StatusCode.Success(1);
            }

            if (!IsAnyWindowOpen(World))
            {
                return StatusCode.Success(2);
            }

            MakeFirstMouseAPointer(World);
            UpdateInteractiveContext(World);
            return StatusCode.Continue;
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }
        }

        private static bool IsAnyVirtualWindowOpen(World world)
        {
            return world.TryGetFirst(out VirtualWindow _);
        }

        private static bool IsAnyWindowOpen(World world)
        {
            return world.TryGetFirst(out Window _);
        }

        private void MakeFirstMouseAPointer(World world)
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

                pointer.Scroll = scroll * 0.15f;

                if (mouse.WasPressed(Mouse.Button.RightButton))
                {
                    rightClickMenu.Position = mouse.Position;
                    rightClickMenu.IsExpanded = true;
                }
            }
        }

        private readonly void UpdateInteractiveContext(World world)
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
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }

        public readonly struct ControlsDemoWindow : IVirtualWindow
        {
            readonly FixedString IVirtualWindow.Title => "Controls Demo";
            readonly unsafe VirtualWindowClose IVirtualWindow.CloseCallback => new(&Closed);

            readonly unsafe void IVirtualWindow.OnCreated(VirtualWindow window, Canvas canvas)
            {
                float singleLineHeight = canvas.GetSettings().SingleLineHeight;
                float gap = 4f;
                float y = -gap;

                Label testLabel = new(canvas, "Hello, World!");
                testLabel.SetParent(window.Container);
                testLabel.Anchor = Anchor.TopLeft;
                testLabel.Color = Color.Black;
                testLabel.Position = new(4f, y);
                testLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;

                Button testButton = new(new(&PressedTestButton), canvas);
                testButton.SetParent(window.Container);
                testButton.Color = new Color(0.2f, 0.2f, 0.2f);
                testButton.Anchor = Anchor.TopLeft;
                testButton.Pivot = new(0f, 1f, 0f);
                testButton.Size = new(180f, singleLineHeight);
                testButton.Position = new(4f, y);

                Label testButtonLabel = new(canvas, "Press count: 0");
                testButtonLabel.SetParent(testButton);
                testButtonLabel.Anchor = Anchor.TopLeft;
                testButtonLabel.Position = new(4f, -4f);
                testButtonLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;

                Toggle testToggle = new(canvas);
                testToggle.SetParent(window.Container);
                testToggle.Position = new(4f, y);
                testToggle.Size = new(24, singleLineHeight);
                testToggle.Anchor = Anchor.TopLeft;
                testToggle.Pivot = new(0f, 1f, 0f);
                testToggle.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                testToggle.CheckmarkColor = Color.White;

                y -= singleLineHeight + gap;

                ScrollBar horizontalScrollBar = new(canvas, Vector2.UnitX, 0.25f);
                horizontalScrollBar.SetParent(window.Container);
                horizontalScrollBar.Position = new(4f, y);
                horizontalScrollBar.Size = new(180f, singleLineHeight);
                horizontalScrollBar.Anchor = Anchor.TopLeft;
                horizontalScrollBar.Pivot = new(0f, 1f, 0f);
                horizontalScrollBar.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                horizontalScrollBar.ScrollHandleColor = Color.White;

                y -= singleLineHeight + gap;

                Dropdown testDropdown = new(canvas);
                testDropdown.SetParent(window.Container);
                testDropdown.Position = new(4f, y);
                testDropdown.Size = new(180f, singleLineHeight);
                testDropdown.Anchor = Anchor.TopLeft;
                testDropdown.Pivot = new(0f, 1f, 0f);
                testDropdown.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                testDropdown.LabelColor = Color.White;
                testDropdown.TriangleColor = Color.White;

                Menu testDropdownMenu = testDropdown.Menu;
                testDropdownMenu.AddOption("Option A");
                testDropdownMenu.AddOption("Option B");
                OptionPath lastOption = testDropdownMenu.AddOption("Option C");
                testDropdownMenu.AddOption("Option D/Apple");
                testDropdownMenu.AddOption("Option D/Banana");
                testDropdownMenu.AddOption("Option D/Cherry");
                testDropdownMenu.AddOption("Option D/More.../Toyota");
                testDropdownMenu.AddOption("Option D/More.../Honda");
                testDropdownMenu.AddOption("Option D/More.../Hyndai");
                testDropdownMenu.AddOption("Option D/More.../Mitsubishi");

                testDropdown.SelectedOption = lastOption;
                testDropdown.Callback = new(&DropdownOptionChanged);

                [UnmanagedCallersOnly]
                static void DropdownOptionChanged(Dropdown dropdown, uint previous, uint current)
                {
                    IsMenuOption option = dropdown.Options[current];
                    Trace.WriteLine($"Selected option: {option.text}");
                }

                y -= singleLineHeight + gap;

                TextField testTextField = new(canvas);
                testTextField.SetParent(window.Container);
                testTextField.Position = new(4f, y);
                testTextField.Size = new(180f, singleLineHeight);
                testTextField.Anchor = Anchor.TopLeft;
                testTextField.Pivot = new(0f, 1f, 0f);
                testTextField.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                testTextField.TextColor = Color.White;

                y -= singleLineHeight + gap;

                ComponentType.Register<bool>();
                ComponentType.Register<float>();
                ComponentType.Register<FixedString>();

                Entity dataEntity = new(canvas.GetWorld());
                dataEntity.AddComponent(true);
                dataEntity.AddComponent(0f);
                dataEntity.AddComponent(new FixedString("babash"));

                ControlField testControlField = ControlField.Create<float, NumberTextEditor>(canvas, "Field 1", dataEntity);
                testControlField.LabelColor = Color.Black;
                testControlField.SetParent(window.Container);
                testControlField.Anchor = Anchor.TopLeft;
                testControlField.Pivot = new(0f, 1f, 0f);
                testControlField.Position = new(4f, y);
                testControlField.Size = new(180f, singleLineHeight);
                
                y -= singleLineHeight + gap;

                Tree testTree = new(canvas);
                testTree.SetParent(window.Container);
                testTree.Position = new(4f, y);
                testTree.Size = new(180f, singleLineHeight);
                testTree.Anchor = Anchor.TopLeft;
                testTree.Pivot = new(0f, 1f, 0f);

                testTree.AddLeaf("Game Object");

                y -= singleLineHeight;

                TreeNode canvasLeaf = testTree.AddLeaf("Canvas");
                canvasLeaf.AddLeaf("Button");
                canvasLeaf.AddLeaf("Toggle");

                y -= singleLineHeight;

                TreeNode playerLeaf = canvasLeaf.AddLeaf("Player");
                playerLeaf.AddLeaf("Health");
                playerLeaf.AddLeaf("Score");

                testTree.AddLeaf("Game Object (1)");
                testTree.AddLeaf("Game Object (2)");

                y -= singleLineHeight;
                y -= singleLineHeight + gap;

                for (int i = 0; i < 20; i++)
                {
                    Image box = new(canvas);
                    box.SetParent(window.Container);
                    box.Size = new(60f, 20f);
                    box.Position = new(200f, i * 26f);
                    box.Color = Color.FromHSV((i * 0.1f) % 1, 1, 1);
                }

                TextField multiLineTextField = new(canvas);
                multiLineTextField.SetParent(window.Container);
                multiLineTextField.Position = new(4f, y);
                multiLineTextField.Size = new(180f, singleLineHeight * 3);
                multiLineTextField.Anchor = Anchor.TopLeft;
                multiLineTextField.Pivot = new(0f, 1f, 0f);
                multiLineTextField.BackgroundColor = new(0.2f, 0.2f, 0.2f);
                multiLineTextField.TextColor = Color.White;
            }

            [UnmanagedCallersOnly]
            private static void PressedTestButton(Entity buttonEntity)
            {
                World world = buttonEntity.GetWorld();
                Entity containerEntity = buttonEntity.Parent;
                USpan<uint> children = containerEntity.Children;
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

                children = buttonEntity.Children;
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
                virtualWindow.Dispose();
            }
        }
    }
}