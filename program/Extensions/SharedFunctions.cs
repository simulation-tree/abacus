using InputDevices;
using Rendering;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Transforms;
using UI;
using UI.Components;
using UI.Functions;
using Unmanaged;
using Worlds;

namespace Abacus
{
    public static class SharedFunctions
    {
        private static bool invertY;
        private static readonly System.Collections.Generic.List<double> frameTimes = new();
        private static double currentFps;
        private static readonly double firstTime;
        private static double nextFpsUpdateTime;
        private static readonly System.Collections.Generic.List<double> fpsHistory = new();

        static SharedFunctions()
        {
            firstTime = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
        }

        public static void DestroyTemporaryEntities(this World world, double deltaTime)
        {
            using Operation operation = new(world);
            ComponentQuery<DestroyAfterTime> query = new(world);
            foreach (var e in query)
            {
                ref DestroyAfterTime destroy = ref e.component1;
                destroy.time -= (float)deltaTime;
                if (destroy.time <= 0)
                {
                    operation.AppendEntityToSelection(e.entity);
                }
            }

            if (operation.Count > 0)
            {
                operation.DestroySelectedEntities();
                operation.Perform();
            }
        }

        public static void MoveCameraAround(this World world, Transform cameraTransform, double deltaTime, ref Vector3 position, ref Vector2 pitchYaw, Vector2 lookSensitivity)
        {
            Vector3 currentPosition = cameraTransform.LocalPosition;
            Quaternion rotation = cameraTransform.LocalRotation;

            //move around with keyboard or gamepad
            bool moveLeft = false;
            bool moveRight = false;
            bool moveForward = false;
            bool moveBackward = false;
            bool moveUp = false;
            bool moveDown = false;
            if (world.TryGetFirst(out Keyboard keyboard))
            {
                ButtonState left = keyboard.GetButtonState(Keyboard.Button.A);
                ButtonState right = keyboard.GetButtonState(Keyboard.Button.D);
                ButtonState forward = keyboard.GetButtonState(Keyboard.Button.W);
                ButtonState backward = keyboard.GetButtonState(Keyboard.Button.S);
                ButtonState up = keyboard.GetButtonState(Keyboard.Button.Space);
                ButtonState down = keyboard.GetButtonState(Keyboard.Button.LeftControl);
                moveLeft |= left.IsPressed;
                moveRight |= right.IsPressed;
                moveForward |= forward.IsPressed;
                moveBackward |= backward.IsPressed;
                moveUp |= up.IsPressed;
                moveDown |= down.IsPressed;

                //invert mouse with this key
                if (keyboard.WasPressed(Keyboard.Button.F3))
                {
                    invertY = !invertY;
                }
            }

            Vector3 moveDirection = default;
            if (moveLeft)
            {
                moveDirection.X -= 1;
            }

            if (moveRight)
            {
                moveDirection.X += 1;
            }

            if (moveForward)
            {
                moveDirection.Z += 1;
            }

            if (moveBackward)
            {
                moveDirection.Z -= 1;
            }

            if (moveUp)
            {
                moveDirection.Y += 1;
            }

            if (moveDown)
            {
                moveDirection.Y -= 1;
            }

            float moveSpeed = 4f;
            float positionLerpSpeed = 12f;
            if (moveDirection.LengthSquared() > 0)
            {
                moveDirection = Vector3.Normalize(moveDirection) * moveSpeed;
            }

            position += Vector3.Transform(moveDirection, rotation) * (float)deltaTime;
            currentPosition = Vector3.Lerp(currentPosition, position, (float)deltaTime * positionLerpSpeed);

            //look around with mice
            if (world.TryGetFirst(out Mouse mouse))
            {
                Vector2 pointerMoveDelta = mouse.Delta * lookSensitivity;
                if (invertY)
                {
                    pointerMoveDelta.Y *= -1;
                }

                pitchYaw.X += pointerMoveDelta.X * 0.01f;
                pitchYaw.Y += pointerMoveDelta.Y * 0.01f;
                pitchYaw.Y = Math.Clamp(pitchYaw.Y, -MathF.PI * 0.5f, MathF.PI * 0.5f);
            }

            Quaternion pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitY, pitchYaw.X);
            Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitchYaw.Y);
            rotation = pitch * yaw;

            cameraTransform.LocalPosition = currentPosition;
            cameraTransform.LocalRotation = rotation;
        }

        public static void TrackFramesPerSecond()
        {
            double now = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
            frameTimes.Add(now);

            if (now >= nextFpsUpdateTime)
            {
                nextFpsUpdateTime = now + 0.1;
                currentFps = 0f;
                if (frameTimes.Count > 1)
                {
                    double first = frameTimes[0];
                    double last = frameTimes[^1];
                    double total = last - first;
                    currentFps = frameTimes.Count / total;
                    frameTimes.Clear();
                }

                double timeStartStart = now - firstTime;
                if (timeStartStart > 5)
                {
                    fpsHistory.Add(currentFps);
                    if (fpsHistory.Count > 60 * 16)
                    {
                        fpsHistory.RemoveAt(0);
                    }
                }
            }
        }

        public static void UpdateUISettings(this World world)
        {
            foreach (Mouse mouse in world.GetAll<Mouse>())
            {
                Entity window = mouse.Window;
                LayerMask selectionMask = new();
                foreach (Canvas canvas in world.GetAll<Canvas>())
                {
                    Cameras.Camera camera = canvas.Camera;
                    if (!camera.IsDestroyed && camera.Destination == window)
                    {
                        selectionMask |= canvas.SelectionMask;
                    }
                }

                CopyMouseIntoPointer(world, mouse, selectionMask);
                UpdateCursorIconBasedOnPointerState(world, mouse);
            }

            SetPressedCharacters(world);
        }

        public static void UpdateCursorIconBasedOnPointerState(this World world, Mouse mouse)
        {
            Pointer pointer = mouse.As<Pointer>();
            LayerMask pointerSelectionMask = pointer.SelectionMask;

            bool nonDefaultCursor = false;
            ComponentQuery<IsResizable> resizableQuery = new(world);
            foreach (var r in resizableQuery)
            {
                LayerMask resizableMask = r.component1.selectionMask;
                if (pointerSelectionMask.ContainsAny(resizableMask))
                {
                    Resizable resizable = Entity.Get<Resizable>(world, r.entity);
                    IsResizable.EdgeMask boundary = resizable.GetBoundary(pointer.Position);
                    if (boundary != default)
                    {
                        mouse.State.cursor = boundary switch
                        {
                            IsResizable.EdgeMask.Top => Mouse.Cursor.ResizeVertical,
                            IsResizable.EdgeMask.Bottom => Mouse.Cursor.ResizeVertical,
                            IsResizable.EdgeMask.Left => Mouse.Cursor.ResizeHorizontal,
                            IsResizable.EdgeMask.Right => Mouse.Cursor.ResizeHorizontal,
                            IsResizable.EdgeMask.TopLeft => Mouse.Cursor.ResizeNWSE,
                            IsResizable.EdgeMask.TopRight => Mouse.Cursor.ResizeNESW,
                            IsResizable.EdgeMask.BottomLeft => Mouse.Cursor.ResizeNESW,
                            IsResizable.EdgeMask.BottomRight => Mouse.Cursor.ResizeNWSE,
                            _ => Mouse.Cursor.Default,
                        };

                        nonDefaultCursor = true;
                    }
                }
            }

            Entity hoveringOver = pointer.HoveringOver;
            if (hoveringOver != default && !hoveringOver.IsDestroyed)
            {
                if (hoveringOver.Is<TextField>())
                {
                    mouse.State.cursor = Mouse.Cursor.Text;
                }
                else
                {
                    mouse.State.cursor = Mouse.Cursor.Hand;
                }

                nonDefaultCursor = true;
            }

            if (!nonDefaultCursor)
            {
                mouse.State.cursor = Mouse.Cursor.Default;
            }
        }

        public static void CopyMouseIntoPointer(this World world, Mouse mouse, LayerMask selectionMask)
        {
            Schema schema = world.Schema;
            Archetype pointerDefinition = Archetype.Get<Pointer>(schema);
            if (!mouse.Is(pointerDefinition))
            {
                mouse.Become(pointerDefinition);
            }

            Pointer pointer = mouse.As<Pointer>();
            pointer.SelectionMask = selectionMask;
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
        }

        public static void SetPressedCharacters(this World world)
        {
            Span<Keyboard.Button> pressedBuffer = stackalloc Keyboard.Button[128];
            Settings settings = world.GetFirst<Settings>();
            ref PressedCharacters pressed = ref settings.PressedCharacters;
            pressed.Clear();
            foreach (Keyboard keyboard in world.GetAll<Keyboard>())
            {
                int pressedCount = keyboard.GetPressedControls(pressedBuffer);
                for (int i = 0; i < pressedCount; i++)
                {
                    Keyboard.Button pressedControl = pressedBuffer[i];
                    char character = pressedControl.GetCharacter();
                    pressed.Press(character);
                }
            }
        }

        public unsafe static void AddLabelProcessors(this World world)
        {
            LabelProcessor.Create(world, new(&TryHandleFPS));
        }

        [UnmanagedCallersOnly, SkipLocalsInit]
        private static Bool TryHandleFPS(TryProcessLabel.Input input)
        {
            const string Token = "{{fps}}";
            ReadOnlySpan<char> originalText = input.OriginalText;
            if (originalText.IndexOf(Token) != -1)
            {
                double averageFps = 0;
                double maxFps = 0;
                for (int i = 0; i < fpsHistory.Count; i++)
                {
                    double fps = fpsHistory[i];
                    averageFps += fps;
                    if (fps > maxFps)
                    {
                        maxFps = fps;
                    }
                }

                averageFps /= fpsHistory.Count;

                string replacement = $"Current: {currentFps:0.00}\nAverage: {averageFps:0.00}\nMax:     {maxFps:0.00}";
                int newLength = originalText.Length - Token.Length + replacement.Length + 32;
                Span<char> destination = stackalloc char[newLength];
                newLength = Text.Replace(originalText, Token.AsSpan(), replacement, destination);
                Span<char> newText = destination.Slice(0, newLength);
                if (!originalText.SequenceEqual(newText))
                {
                    input.SetResult(newText);
                    return true;
                }
            }

            return false;
        }
    }
}