using InputDevices;
using System;
using System.Numerics;
using Transforms;
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
}