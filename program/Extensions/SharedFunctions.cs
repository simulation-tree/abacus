using InputDevices;
using InteractionKit;
using System;
using System.Numerics;
using Transforms;
using Unmanaged;
using Worlds;

public static class SharedFunctions
{
    private static bool invertY;
    private static Vector2 lastPointerPosition;
    private static bool hasLastPointerPosition;

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
            world.Perform(operation);
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

            if (keyboard.WasPressed(Keyboard.Button.Y))
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

    public static void UpdateUISettings(this World world)
    {
        if (world.TryGetFirst(out Mouse mouse))
        {
            CopyMouseIntoPointer(world, mouse);
            UpdateCursorBasedOnPointerState(world, mouse);
        }

        SetPressedCharacters(world);
    }

    public static void UpdateCursorBasedOnPointerState(this World world, Mouse mouse)
    {
        Pointer pointer = mouse.AsEntity().As<Pointer>();
        Entity hoveringOver = pointer.HoveringOver;
        if (hoveringOver != default)
        {
            if (hoveringOver.Is<TextField>())
            {
                mouse.State.cursor = Mouse.Cursor.Text;
            }
            else
            {
                mouse.State.cursor = Mouse.Cursor.Hand;
            }
        }
        else
        {
            mouse.State.cursor = Mouse.Cursor.Default;
        }
    }

    public static void CopyMouseIntoPointer(this World world, Mouse mouse)
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
    }

    public static void SetPressedCharacters(this World world)
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
}