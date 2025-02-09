using InputDevices;
using Rendering;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Transforms;
using UI;
using UI.Components;
using UI.Functions;
using Unmanaged;
using Worlds;

public static class SharedFunctions
{
    private static bool invertY;
    private static Vector2 lastPointerPosition;
    private static bool hasLastPointerPosition;
    private static readonly System.Collections.Generic.List<double> frameTimes = new();
    private static double currentFps;
    private static readonly DateTime firstTime;
    private static DateTime nextFpsUpdateTime;
    private static readonly System.Collections.Generic.List<double> fpsHistory = new();

    static SharedFunctions()
    {
        firstTime = DateTime.UtcNow;
    }

    public static void DestroyTemporaryEntities(this World world, TimeSpan deltaSpan)
    {
        using Operation operation = new();
        ComponentQuery<DestroyAfterTime> query = new(world);
        float delta = (float)deltaSpan.TotalSeconds;
        foreach (var e in query)
        {
            ref DestroyAfterTime destroy = ref e.component1;
            destroy.time -= delta;
            if (destroy.time <= 0)
            {
                operation.SelectEntity(e.entity);
            }
        }

        if (operation.Count > 0)
        {
            operation.DestroySelected();
            operation.Perform(world);
        }
    }

    public static void MoveCameraAround(this World world, Transform cameraTransform, TimeSpan delta, ref Vector3 position, ref Vector2 pitchYaw, Vector2 lookSensitivity)
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

        position += Vector3.Transform(moveDirection, rotation) * (float)delta.TotalSeconds;
        currentPosition = Vector3.Lerp(currentPosition, position, (float)delta.TotalSeconds * positionLerpSpeed);

        //look around with mice
        if (world.TryGetFirst(out Mouse mouse))
        {
            Vector2 pointerPosition = mouse.Position;
            if (!hasLastPointerPosition && pointerPosition != default)
            {
                lastPointerPosition = pointerPosition;
                hasLastPointerPosition = true;
            }

            Vector2 pointerMoveDelta = (pointerPosition - lastPointerPosition) * lookSensitivity;
            if (invertY)
            {
                pointerMoveDelta.Y *= -1;
            }

            lastPointerPosition = pointerPosition;
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
        DateTime now = DateTime.UtcNow;
        frameTimes.Add(now.TimeOfDay.TotalSeconds);

        if (now >= nextFpsUpdateTime)
        {
            nextFpsUpdateTime = now + TimeSpan.FromSeconds(0.5f);
            currentFps = 0f;
            if (frameTimes.Count > 1)
            {
                double first = frameTimes[0];
                double last = frameTimes[^1];
                double total = last - first;
                currentFps = frameTimes.Count / total;
                frameTimes.Clear();
            }

            TimeSpan timeStartStart = now - firstTime;
            if (timeStartStart > TimeSpan.FromSeconds(10f))
            {
                fpsHistory.Add(currentFps);
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
                Resizable resizable = new Entity(world, r.entity).As<Resizable>();
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
        USpan<Keyboard.Button> pressedBuffer = stackalloc Keyboard.Button[128];
        Settings settings = world.GetFirst<Settings>();
        ref PressedCharacters pressed = ref settings.PressedCharacters;
        pressed.Clear();
        foreach (Keyboard keyboard in world.GetAll<Keyboard>())
        {
            uint pressedCount = keyboard.GetPressedControls(pressedBuffer);
            for (uint i = 0; i < pressedCount; i++)
            {
                Keyboard.Button pressedControl = pressedBuffer[i];
                char character = pressedControl.GetCharacter();
                pressed.Press(character);
            }
        }
    }

    public unsafe static void AddLabelProcessors(this World world)
    {
        new LabelProcessor(world, new(&TryHandleCurrentFPS));
        new LabelProcessor(world, new(&TryHandleAverageFPS));
    }

    [UnmanagedCallersOnly]
    private static UI.Boolean TryHandleCurrentFPS(TryProcessLabel.Input input)
    {
        const string CurrentFPS = "{{currentFps}}";
        if (input.OriginalText.Contains(CurrentFPS.AsSpan()))
        {
            USpan<char> replacement = stackalloc char[128];
            uint replacementLength = currentFps.ToString(replacement);
            replacementLength = Math.Min(replacementLength, 6);
            uint newLength = input.OriginalText.Length - (uint)CurrentFPS.Length + 64;
            USpan<char> destination = stackalloc char[(int)newLength];
            newLength = Text.Replace(input.OriginalText, CurrentFPS.AsSpan(), replacement.Slice(0, replacementLength), destination);
            USpan<char> newText = destination.Slice(0, newLength);
            if (!input.OriginalText.SequenceEqual(newText))
            {
                input.SetResult(newText);
                return true;
            }
        }

        return false;
    }

    [UnmanagedCallersOnly]
    private static UI.Boolean TryHandleAverageFPS(TryProcessLabel.Input input)
    {
        const string AverageFPS = "{{averageFps}}";
        if (input.OriginalText.Contains(AverageFPS.AsSpan()))
        {
            USpan<char> replacement = stackalloc char[128];
            double averageFps = 0;
            for (int i = 0; i < fpsHistory.Count; i++)
            {
                averageFps += fpsHistory[i];
            }

            averageFps /= fpsHistory.Count;
            uint replacementLength = averageFps.ToString(replacement);
            replacementLength = Math.Min(replacementLength, 6);
            uint newLength = input.OriginalText.Length - (uint)AverageFPS.Length + 64;
            USpan<char> destination = stackalloc char[(int)newLength];
            newLength = Text.Replace(input.OriginalText, AverageFPS.AsSpan(), replacement.Slice(0, replacementLength), destination);
            input.SetResult(destination.Slice(0, newLength));
            USpan<char> newText = destination.Slice(0, newLength);
            if (!input.OriginalText.SequenceEqual(newText))
            {
                input.SetResult(newText);
                return true;
            }
        }

        return false;
    }
}