using Cameras;
using Collections;
using Data;
using InputDevices;
using InteractionKit;
using InteractionKit.Components;
using InteractionKit.Functions;
using Rendering;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Windows;
using Worlds;

namespace Editor
{
    public partial struct EditorProgram : IProgram
    {
        private readonly World world;
        private readonly Simulator simulator;
        private readonly Settings settings;
        private readonly Array<Text> args;
        private EditorWindow<LaunchWindow> launcherWindow;
        private EditorWindow<WorldWindow> worldWindow;
        private EditorWindow<EntityWindow> entityWindow;
        private bool? loaded;
        private State state;

        public enum State
        {
            Idle,
            DestroyingWindows,
            CreatingWindows
        }

        private EditorProgram(Simulator simulator, World world, Array<Text> args)
        {
            this.simulator = simulator;
            this.world = world;
            this.args = args;
            settings = new(world);

            uint editorStateEntity = world.CreateEntity();
            ref EditorState editorState = ref world.AddComponent<EditorState>(editorStateEntity);
            editorState.editingWorld = new();

            //try to load a world from the first argument
            if (args.Length > 0)
            {
                Text firstArg = args[0];
                DataRequest request = new(world, firstArg);
                simulator.UpdateSystems(TimeSpan.MinValue, world);
                if (request.TryGetData(out USpan<byte> data))
                {
                    Trace.WriteLine($"Loaded world from `{firstArg}`");

                    using BinaryReader reader = new(data);
                    using World loadedWorld = reader.ReadObject<World>();
                    editorState.LoadWorld(loadedWorld);
                }
                else
                {
                    Trace.WriteLine($"Failed to load world from `{firstArg}`");
                }
            }
            else
            {
                Trace.WriteLine("No world to load");
            }
        }

        public EditorProgram(string[] args)
        {
            this.args = new((uint)args.Length);
            for (uint i = 0; i < args.Length; i++)
            {
                this.args[i] = args[i];
            }
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
            ref EditorState editorState = ref settings.GetEditorState();
            editorState.editingWorld.Dispose();

            foreach (Text arg in args)
            {
                arg.Dispose();
            }

            args.Dispose();
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            ref EditorProgram program = ref allocation.Read<EditorProgram>();
            program = new(simulator, world, program.args);
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            ref EditorState editorState = ref settings.GetEditorState();
            if (state == State.DestroyingWindows)
            {
                if (!editorState.loaded)
                {
                    if (worldWindow != default)
                    {
                        worldWindow.Dispose();
                        worldWindow = default;
                    }

                    if (entityWindow != default)
                    {
                        entityWindow.Dispose();
                        entityWindow = default;
                    }
                }
                else
                {
                    if (launcherWindow != default)
                    {
                        launcherWindow.Dispose();
                        launcherWindow = default;
                    }
                }

                state = State.CreatingWindows;
            }
            else if (state == State.CreatingWindows)
            {
                if (!editorState.loaded)
                {
                    if (launcherWindow == default)
                    {
                        launcherWindow = new(world, settings, new(200, 200), new(200, 200));
                    }
                }
                else
                {
                    if (worldWindow == default)
                    {
                        worldWindow = new(world, settings, new(0, 200), new(200, 200));
                    }

                    if (entityWindow == default)
                    {
                        entityWindow = new(world, settings, new(400, 200), new(200, 200));
                    }
                }

                state = State.Idle;
            }

            if (loaded != editorState.loaded)
            {
                loaded = editorState.loaded;
                state = State.DestroyingWindows;
            }
            else if (state == State.Idle)
            {
                if (!IsAnyWindowOpen(world))
                {
                    return StatusCode.Success(0);
                }
            }

            if (world.TryGetFirst(out Mouse mouse))
            {
                UpdatePointer(world, mouse);
            }

            SetPressedCharacters(world);
            return StatusCode.Continue;
        }

        private static bool IsAnyWindowOpen(World world)
        {
            return world.TryGetFirst(out Window _);
        }

        private static void UpdatePointer(World world, Mouse mouse)
        {
            Schema schema = world.Schema;
            Definition pointerDefinition = Archetype.Get<Pointer>(schema).definition;
            if (!mouse.Is(pointerDefinition))
            {
                mouse.Become(pointerDefinition);
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

            bool setCursor = false;
            ComponentQuery<IsResizable> resizableQuery = new(world);
            foreach (var r in resizableQuery)
            {
                Resizable resizable = new(world, r.entity);
                IsResizable.Boundary boundary = resizable.GetBoundary(pointer.Position);
                if (boundary != default)
                {
                    mouse.State.cursor = boundary switch
                    {
                        IsResizable.Boundary.Top => Mouse.Cursor.ResizeVertical,
                        IsResizable.Boundary.Bottom => Mouse.Cursor.ResizeVertical,
                        IsResizable.Boundary.Left => Mouse.Cursor.ResizeHorizontal,
                        IsResizable.Boundary.Right => Mouse.Cursor.ResizeHorizontal,
                        IsResizable.Boundary.TopLeft => Mouse.Cursor.ResizeNWSE,
                        IsResizable.Boundary.TopRight => Mouse.Cursor.ResizeNESW,
                        IsResizable.Boundary.BottomLeft => Mouse.Cursor.ResizeNESW,
                        IsResizable.Boundary.BottomRight => Mouse.Cursor.ResizeNWSE,
                        _ => Mouse.Cursor.Default,
                    };

                    setCursor = true;
                }
            }

            Entity hoveringOver = pointer.HoveringOver;
            if (hoveringOver != default && world.ContainsEntity(hoveringOver))
            {
                if (hoveringOver.Is<TextField>())
                {
                    mouse.State.cursor = Mouse.Cursor.Text;
                }
                else
                {
                    mouse.State.cursor = Mouse.Cursor.Hand;
                }

                setCursor = true;
            }

            if (!setCursor)
            {
                mouse.State.cursor = Mouse.Cursor.Default;
            }
        }

        private void SetPressedCharacters(World world)
        {
            if (world.TryGetFirst(out Keyboard keyboard))
            {
                USpan<Keyboard.Button> pressedBuffer = stackalloc Keyboard.Button[128];
                uint pressedCount = keyboard.GetPressedControls(pressedBuffer);
                ref PressedCharacters pressed = ref settings.PressedCharacters;
                pressed = default;
                for (uint i = 0; i < pressedCount; i++)
                {
                    Keyboard.Button pressedControl = pressedBuffer[i];
                    char character = pressedControl.GetCharacter();
                    pressed.Press(character);
                }
            }
        }
    }

    public readonly struct EditorWindow<T> : IEntity, IEquatable<EditorWindow<T>> where T : unmanaged, IVirtualWindow
    {
        private readonly Entity entity;

        readonly World IEntity.World => entity.GetWorld();
        readonly uint IEntity.Value => entity.GetEntityValue();

        readonly void IEntity.Describe(ref Archetype archetype)
        {
            archetype.AddComponentType<IsEditorWindow>();
            archetype.Add<Window>();
        }

        public unsafe EditorWindow(World world, Settings settings, Vector2 position, Vector2 size)
        {
            FixedString title = default(T).Title;
            Window window = new(world, title, position, size, "vulkan", new(&CloseFunction.OnWindowClosed));
            window.GetClearColor() = new(0.5f, 0.5f, 0.5f, 1);
            window.IsResizable = true;

            Camera camera = Camera.CreateOrthographic(world, window, 1f);
            camera.SetParent(window);

            Canvas canvas = new(world, settings, camera);
            canvas.SetParent(window);

            Transform container = new(world);
            container.SetParent(canvas);

            rint canvasReference = window.AddReference(canvas);
            rint containerReference = window.AddReference(container);
            window.AddComponent(new IsEditorWindow(canvasReference, containerReference));
            entity = window;

            default(T).OnCreated(container, canvas);
        }

        public readonly void Dispose()
        {
            entity.Dispose();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is EditorWindow<T> window && Equals(window);
        }

        public readonly bool Equals(EditorWindow<T> other)
        {
            return entity.Equals(other.entity);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(entity);
        }

        public static bool operator ==(EditorWindow<T> left, EditorWindow<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EditorWindow<T> left, EditorWindow<T> right)
        {
            return !(left == right);
        }
    }

    public readonly struct LaunchWindow : IVirtualWindow
    {
        FixedString IVirtualWindow.Title => "Launch";
        VirtualWindowClose IVirtualWindow.CloseCallback => default;

        unsafe void IVirtualWindow.OnCreated(Transform container, Canvas canvas)
        {
            Settings settings = canvas.Settings;

            Button newButton = new(new(&PressedNew), canvas);
            newButton.SetParent(container);
            newButton.Color = new(0.2f, 0.2f, 0.2f, 1);
            newButton.Anchor = Anchor.TopLeft;
            newButton.Pivot = new(0f, 1f, 0f);
            newButton.Size = new(180f, settings.SingleLineHeight);
            newButton.Position = new(4f, -4f);

            Label newButtonLabel = new(canvas, "New World");
            newButtonLabel.SetParent(newButton);
            newButtonLabel.Anchor = Anchor.TopLeft;
            newButtonLabel.Position = new(4f, -4f);
            newButtonLabel.Pivot = new(0f, 1f, 0f);

            [UnmanagedCallersOnly]
            static void PressedNew(Entity button)
            {
                Trace.WriteLine("Pressed New");
                ref EditorState editorState = ref button.GetEditorState();
                using World newWorld = new();
                editorState.LoadWorld(newWorld);
            }
        }
    }

    public readonly struct WorldWindow : IVirtualWindow
    {
        FixedString IVirtualWindow.Title => "World";
        VirtualWindowClose IVirtualWindow.CloseCallback => default;

        void IVirtualWindow.OnCreated(Transform container, Canvas canvas)
        {
        }
    }

    public readonly struct EntityWindow : IVirtualWindow
    {
        FixedString IVirtualWindow.Title => "Entity";
        VirtualWindowClose IVirtualWindow.CloseCallback => default;

        unsafe void IVirtualWindow.OnCreated(Transform container, Canvas canvas)
        {
            Settings settings = canvas.Settings;

            Button newButton = new(new(&PressedReturn), canvas);
            newButton.SetParent(container);
            newButton.Color = new(0.2f, 0.2f, 0.2f, 1);
            newButton.Anchor = Anchor.TopLeft;
            newButton.Pivot = new(0f, 1f, 0f);
            newButton.Size = new(180f, settings.SingleLineHeight);
            newButton.Position = new(4f, -4f);

            Label newButtonLabel = new(canvas, "Return");
            newButtonLabel.SetParent(newButton);
            newButtonLabel.Anchor = Anchor.TopLeft;
            newButtonLabel.Position = new(4f, -4f);
            newButtonLabel.Pivot = new(0f, 1f, 0f);

            [UnmanagedCallersOnly]
            static void PressedReturn(Entity button)
            {
                Trace.WriteLine("Pressed Return");
                ref EditorState editorState = ref button.GetEditorState();
                editorState.Reset();
            }
        }
    }

    public static class CloseFunction
    {
        [UnmanagedCallersOnly]
        public static void OnWindowClosed(Window window)
        {
            window.Dispose();
        }
    }

    public static class EntityExtensions
    {
        public static ref EditorState GetEditorState<T>(this T entity) where T : unmanaged, IEntity
        {
            World world = entity.GetWorld();
            if (world.TryGetFirstComponent<EditorState>(out uint editorStateEntity))
            {
                return ref world.GetComponent<EditorState>(editorStateEntity);
            }

            throw new InvalidOperationException("No editor state found");
        }
    }

    [Component]
    public readonly struct IsEditorWindow
    {
        public readonly rint canvasReference;
        public readonly rint containerReference;

        public IsEditorWindow(rint canvasReference, rint containerReference)
        {
            this.canvasReference = canvasReference;
            this.containerReference = containerReference;
        }
    }

    [Component]
    public struct EditorState
    {
        public World editingWorld;
        public bool loaded;

        public EditorState(World editingWorld)
        {
            this.editingWorld = editingWorld;
        }

        public void LoadWorld(World loadedWorld)
        {
            editingWorld.Clear();
            editingWorld.Schema.CopyFrom(loadedWorld.Schema);
            editingWorld.Append(loadedWorld);
            loaded = true;
        }

        public void Reset()
        {
            editingWorld.Clear();
            loaded = false;
        }
    }
}