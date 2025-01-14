using Cameras;
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
using Textures;
using Transforms;
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
        private readonly Dropdown enumDropdown;

        private readonly World World => window.GetWorld();

        private unsafe ControlsTest(World world)
        {
            window = new(world, "Editor", new(200, 200), new(900, 720), "vulkan", new(&OnWindowClosed));
            window.SetClearColor(new(0.5f, 0.5f, 0.5f, 1));
            window.IsResizable = true;

            Settings settings = new(world);
            Camera camera = Camera.CreateOrthographic(world, window, 1f);
            Canvas canvas = new(world, settings, camera);

            VirtualWindow box = VirtualWindow.Create<ControlsDemoWindow>(world, canvas);
            box.Size = new(300, 300);
            box.Position = new(40, 40);
            box.Z += Settings.ZScale;
            //box.Anchor = new(new(2f, true), new(2f, true), new(0f, false), new(2f, true), new(2f, true), new(0f, false));
            
            Image image = new(canvas);
            image.Size = new(200, 200);
            image.Position = new(100, 100);
            Resizable resizable = image.AsEntity().Become<Resizable>();
            resizable.Boundary = IsResizable.Boundary.All;
            DropShadow dropShadow = new(canvas, image);
            
            Label anotherLabel = new(canvas, "Hello there\nWith another line\nNice");
            anotherLabel.Position = new(105, 150);
            anotherLabel.Color = new(0, 0, 0, 1);
            anotherLabel.Z = Settings.ZScale;

            enumDropdown = new Dropdown<DropdownOptions>(canvas, new(180f, settings.SingleLineHeight));
            enumDropdown.Position = new(5, -5);
            enumDropdown.Size = new(180f, settings.SingleLineHeight);
            enumDropdown.Anchor = Anchor.TopLeft;
            enumDropdown.Pivot = new(0f, 1f, 0f);
            enumDropdown.BackgroundColor = new(0.2f, 0.2f, 0.2f, 1);
            enumDropdown.LabelColor = new(1, 1, 1, 1);
            enumDropdown.TriangleColor = new(1, 1, 1, 1);
            enumDropdown.Z = Settings.ZScale;

            Vector2 optionSize = new(100, settings.SingleLineHeight);
            rightClickMenu = new(canvas, optionSize, new(&ChoseMenuOption));
            rightClickMenu.AddOption("Stop");
            rightClickMenu.AddOption("And Listen");
            rightClickMenu.AddOption("OoOoh");
            rightClickMenu.AddOption("Deep.../First");
            rightClickMenu.AddOption("Deep.../Second");
            rightClickMenu.AddOption("Deep.../Third");
            rightClickMenu.AddOption("Deep.../Apple");
            rightClickMenu.AddOption("Deep.../Banana");
            rightClickMenu.AddOption("Deep.../Car");
            rightClickMenu.Position = new(200, 300);
            rightClickMenu.Pivot = new(0, 1f, 0f);
            rightClickMenu.IsExpanded = false;
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
                return StatusCode.Success(0);
            }

            if (!IsAnyWindowOpen(World))
            {
                return StatusCode.Success(1);
            }

            ToggleRightClickMenu(World);
            SharedFunctions.UpdateUISettings(World);
            return StatusCode.Continue;
        }

        private readonly void ToggleRightClickMenu(World world)
        {
            if (world.TryGetFirst(out Mouse mouse))
            {
                if (mouse.WasPressed(Mouse.Button.RightButton))
                {
                    rightClickMenu.Position = mouse.Position;
                    rightClickMenu.IsExpanded = true;
                }
            }
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

        [UnmanagedCallersOnly]
        private static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }

        public readonly struct ControlsDemoWindow : IVirtualWindow
        {
            readonly FixedString IVirtualWindow.Title => "Controls Demo";
            readonly unsafe VirtualWindowClose IVirtualWindow.CloseCallback => new(&Closed);

            readonly unsafe void IVirtualWindow.OnCreated(Transform container, Canvas canvas)
            {
                float singleLineHeight = canvas.Settings.SingleLineHeight;
                float gap = 4f;
                float y = -gap;
                float indent = 0f;

                Label testLabel = new(canvas, "Hello, World!");
                testLabel.SetParent(container);
                testLabel.Anchor = Anchor.TopLeft;
                testLabel.Color = new(0, 0, 0, 1);
                testLabel.Position = new(gap, y);
                testLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;

                Button testButton = new(new(&PressedTestButton), canvas);
                testButton.SetParent(container);
                testButton.Color = new(0.2f, 0.2f, 0.2f, 1);
                testButton.Anchor = Anchor.TopLeft;
                testButton.Pivot = new(0f, 1f, 0f);
                testButton.Size = new(180f, singleLineHeight);
                testButton.Position = new(gap, y);

                Label testButtonLabel = new(canvas, "Press count: 0");
                testButtonLabel.SetParent(testButton);
                testButtonLabel.Anchor = Anchor.TopLeft;
                testButtonLabel.Position = new(gap, -4f);
                testButtonLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;

                Toggle testToggle = new(canvas);
                testToggle.SetParent(container);
                testToggle.Position = new(gap, y);
                testToggle.Size = new(24, singleLineHeight);
                testToggle.Anchor = Anchor.TopLeft;
                testToggle.Pivot = new(0f, 1f, 0f);
                testToggle.BackgroundColor = new(0.2f, 0.2f, 0.2f, 1);
                testToggle.CheckmarkColor = new(1, 1, 1, 1);

                y -= singleLineHeight + gap;

                ScrollBar horizontalScrollBar = new(canvas, Vector2.UnitX, 0.25f);
                horizontalScrollBar.SetParent(container);
                horizontalScrollBar.Position = new(gap, y);
                horizontalScrollBar.Size = new(180f, singleLineHeight);
                horizontalScrollBar.Anchor = Anchor.TopLeft;
                horizontalScrollBar.Pivot = new(0f, 1f, 0f);
                horizontalScrollBar.BackgroundColor = new(0.2f, 0.2f, 0.2f, 1);
                horizontalScrollBar.ScrollHandleColor = new(1, 1, 1, 1);

                y -= singleLineHeight + gap;

                Dropdown testDropdown = new(canvas, new(180f, singleLineHeight));
                testDropdown.SetParent(container);
                testDropdown.Position = new(gap, y);
                testDropdown.Size = new(180f, singleLineHeight);
                testDropdown.Anchor = Anchor.TopLeft;
                testDropdown.Pivot = new(0f, 1f, 0f);
                testDropdown.BackgroundColor = new(0.2f, 0.2f, 0.2f, 1);
                testDropdown.LabelColor = new(1, 1, 1, 1);
                testDropdown.TriangleColor = new(1, 1, 1, 1);

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
                testTextField.SetParent(container);
                testTextField.Position = new(gap, y);
                testTextField.Size = new(180f, singleLineHeight);
                testTextField.Anchor = Anchor.TopLeft;
                testTextField.Pivot = new(0f, 1f, 0f);
                testTextField.BackgroundColor = new(0.2f, 0.2f, 0.2f, 1f);
                testTextField.TextColor = new(1, 1, 1, 1);

                y -= singleLineHeight + gap;

                Label headerLabel = new(canvas, "Fields");
                headerLabel.SetParent(container);
                headerLabel.Anchor = Anchor.TopLeft;
                headerLabel.Color = new(0, 0, 0, 1);
                headerLabel.Position = new(gap, y);
                headerLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;
                indent += 20f;
                {
                    World world = canvas.GetWorld();
                    world.Schema.RegisterComponent<bool>();
                    world.Schema.RegisterComponent<float>();
                    world.Schema.RegisterComponent<FixedString>();

                    Entity dataEntity = new(canvas.GetWorld());
                    dataEntity.AddComponent(true);
                    dataEntity.AddComponent(0f);
                    dataEntity.AddComponent(new FixedString("babash"));

                    ControlField numberField = ControlField.Create<float, NumberTextEditor>(canvas, "Number", dataEntity);
                    numberField.LabelColor = new(0, 0, 0, 1);
                    numberField.SetParent(container);
                    numberField.Anchor = Anchor.TopLeft;
                    numberField.Pivot = new(0f, 1f, 0f);
                    numberField.Position = new(gap + indent, y);
                    numberField.Size = new(180f, singleLineHeight);

                    y -= singleLineHeight + gap;

                    ControlField textField = ControlField.Create<FixedString, TextEditor>(canvas, "Text", dataEntity);
                    textField.LabelColor = new(0, 0, 0, 1);
                    textField.SetParent(container);
                    textField.Anchor = Anchor.TopLeft;
                    textField.Pivot = new(0f, 1f, 0f);
                    textField.Position = new(gap + indent, y);
                    textField.Size = new(180f, singleLineHeight);

                    y -= singleLineHeight + gap;

                    ControlField toggleField = ControlField.Create<bool, BooleanEditor>(canvas, "Toggle", dataEntity);
                    toggleField.LabelColor = new(0, 0, 0, 1);
                    toggleField.SetParent(container);
                    toggleField.Anchor = Anchor.TopLeft;
                    toggleField.Pivot = new(0f, 1f, 0f);
                    toggleField.Position = new(gap + indent, y);
                    toggleField.Size = new(180f, singleLineHeight);

                    y -= singleLineHeight + gap;
                }

                indent -= 20f;

                Label treeLabel = new(canvas, "Tree of objects");
                treeLabel.SetParent(container);
                treeLabel.Anchor = Anchor.TopLeft;
                treeLabel.Color = new(0, 0, 0, 1);
                treeLabel.Position = new(gap, y);
                treeLabel.Pivot = new(0f, 1f, 0f);

                y -= singleLineHeight + gap;

                Tree testTree = new(canvas);
                testTree.SetParent(container);
                testTree.Position = new(gap, y);
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
                    box.SetParent(container);
                    box.Size = new(60f, 20f);
                    box.Position = new(200f, i * 26f);
                    box.Color = new Vector4((i * 0.1f) % 1, 1, 1, 1).FromHSV();
                }

                TextField multiLineTextField = new(canvas);
                multiLineTextField.SetParent(container);
                multiLineTextField.Position = new(gap, y);
                multiLineTextField.Size = new(180f, singleLineHeight * 3);
                multiLineTextField.Anchor = Anchor.TopLeft;
                multiLineTextField.Pivot = new(0f, 1f, 0f);
                multiLineTextField.BackgroundColor = new(0.2f, 0.2f, 0.2f, 1f);
                multiLineTextField.TextColor = new(1, 1, 1, 1);
            }

            [UnmanagedCallersOnly]
            private static void PressedTestButton(Entity buttonEntity)
            {
                World world = buttonEntity.GetWorld();
                Entity containerEntity = buttonEntity.GetParent();
                USpan<uint> children = containerEntity.GetChildren();
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

                children = buttonEntity.GetChildren();
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
                        textValue.Length = (byte)(startIndex + 1);
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

        public enum DropdownOptions
        {
            First,
            Second,
            Third,
            Yo,
            What,
            Is,
            Up
        }
    }
}