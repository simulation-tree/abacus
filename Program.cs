using Data;
using Meshes;
using Rendering;
using Rendering.Components;
using Shaders;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Windows;
using Windows.Components;

public struct Program : IDisposable
{
    private readonly World world;

    private DateTime lastTime;
    private TimeSpan time;
    private Vector2 lastPointerPosition;
    private Vector3 cameraPosition;
    private Vector2 cameraPitchYaw;
    private float moveSpeed;
    private float positionLerpSpeed;
    private float lookSensitivity;
    private bool invertY;

    private readonly Window window;
    private readonly Camera camera;
    private readonly Renderer dummyRenderer;
    private readonly Renderer testRenderer;

    public unsafe Program(World world)
    {
        this.world = world;
        lastTime = DateTime.Now;
        moveSpeed = 4f;
        lookSensitivity = 2f;
        invertY = true;
        positionLerpSpeed = 12f;

        //build host
        window = new(world, "Window", new(100, 100), new(900, 720), "vulkan", new(&WindowClosed));

        //build scene
        camera = new(world, window.AsDestination(), false, MathF.PI * 0.5f);
        camera.SetPosition(0f, 0f, -10f);
        cameraPosition = camera.GetPosition();

        Mesh mesh = new(world);
        Mesh.Collection<Vector3> positions = mesh.CreatePositions();
        Mesh.Collection<Vector2> uvs = mesh.CreateUVs();
        Mesh.Collection<Vector3> normals = mesh.CreateNormals();
        Mesh.Collection<Vector4> colors = mesh.CreateColors();

        //simple quad
        positions.Add(new(0, 0, 0));
        positions.Add(new(1, 0, 0));
        positions.Add(new(1, 1, 0));
        positions.Add(new(0, 1, 0));

        uvs.Add(new(0, 0));
        uvs.Add(new(1, 0));
        uvs.Add(new(1, 1));
        uvs.Add(new(0, 1));

        normals.Add(new(0, 0, 1));
        normals.Add(new(0, 0, 1));
        normals.Add(new(0, 0, 1));
        normals.Add(new(0, 0, 1));

        colors.Add(new(1, 1, 1, 1));
        colors.Add(new(1, 1, 1, 1));
        colors.Add(new(1, 1, 1, 1));
        colors.Add(new(1, 1, 1, 1));

        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(2, 3, 0);

        Texture texture = new(world, "Tester/Assets/Textures/texture.jpg");
        Shader shader = new(world, "Tester/Assets/Shaders/unlit.vert", "Tester/Assets/Shaders/unlit.frag");

        Material material = new(world, shader);
        material.AddPushBinding(RuntimeType.Get<Color>());
        material.AddPushBinding(RuntimeType.Get<LocalToWorld>());
        material.AddComponentBinding(2, 0, camera, RuntimeType.Get<CameraProjection>(), ShaderStage.Vertex);
        material.AddTextureBinding(3, 0, texture);

        dummyRenderer = new(world, mesh, material, camera);
        dummyRenderer.AddComponent(Color.Yellow);
        dummyRenderer.BecomeTransform();

        //to verify 2 renderers + 2 materials + 1 shader + 1 mesh
        //Material testMaterial = new(world, shader);
        //testMaterial.AddPushBinding(RuntimeType.Get<Color>());
        //testMaterial.AddPushBinding(RuntimeType.Get<LocalToWorld>());
        //testMaterial.AddComponentBinding(2, 0, camera, RuntimeType.Get<CameraProjection>(), ShaderStage.Vertex);
        //testMaterial.AddTextureBinding(3, 0, texture);

        //to very 2 renderers + 1 material + 1 shader + 2 meshes
        //Mesh testMesh = new(world);
        //Mesh.Collection<Vector3> testPositions = testMesh.CreatePositions();
        //Mesh.Collection<Vector2> testUVs = testMesh.CreateUVs();
        //Mesh.Collection<Vector4> testColors = testMesh.CreateColors();
        //testPositions.Add(new(-1, 0, 0));
        //testPositions.Add(new(1, 0, 0));
        //testPositions.Add(new(0, 1, 0));
        //testUVs.Add(new(0, 0));
        //testUVs.Add(new(1, 0));
        //testUVs.Add(new(0.5f, 1));
        //testColors.Add(new(1, 1, 1, 1));
        //testColors.Add(new(1, 1, 1, 1));
        //testColors.Add(new(1, 1, 1, 1));
        //testMesh.AddTriangle(0, 1, 2);

        testRenderer = new(world, mesh, material, camera);
        testRenderer.AddComponent(Color.White);
        Transform testTransform = testRenderer.BecomeTransform();
        testTransform.SetPosition(0, 2, 0);

        [UnmanagedCallersOnly]
        static void WindowClosed(World world, eint windowEntity)
        {
            world.DestroyEntity(windowEntity);
        }
    }

    public void Dispose()
    {
        if (!window.IsDestroyed())
        {
            window.Dispose();
        }
    }

    public bool Update()
    {
        DateTime now = DateTime.Now;
        TimeSpan delta = now - lastTime;
        lastTime = now;
        time += delta;
        if (time.TotalSeconds > 120f || window.IsDestroyed())
        {
            Console.WriteLine("Conditions reached for finishing the demo");
            return false; //source of "shutdown" event
        }

        TestMouseInputs();
        AnimateTestRenderer(delta);
        MoveCameraAround(delta);
        if (!TestWindowEntity(delta))
        {
            return false;
        }

        return true;
    }

    private readonly void AnimateTestRenderer(TimeSpan delta)
    {
        foreach (eint keyboardEntity in world.GetAll<IsKeyboard>())
        {
            Keyboard keyboard = new(world, keyboardEntity);
            if (keyboard.WasPressed(Keyboard.Button.J))
            {
                dummyRenderer.SetEnabledState(!dummyRenderer.IsEnabled());
            }
        }

        ref Color color = ref dummyRenderer.GetComponentRef<Renderer, Color>();
        float hue = color.Hue;
        hue += (float)delta.TotalSeconds * 0.1f;
        while (hue > 1f)
        {
            hue -= 1f;
        }

        color.Hue = hue;
    }

    private readonly bool TestWindowEntity(TimeSpan delta)
    {
        Vector2 windowPosition = window.GetPosition();
        Vector2 windowSize = window.GetSize();
        foreach (eint keyboardEntity in world.GetAll<IsKeyboard>())
        {
            Keyboard keyboard = new(world, keyboardEntity);
            if (keyboard.WasPressed(Keyboard.Button.Escape))
            {
                return false; //source of "shutdown" event
            }

            if (keyboard.WasPressed(Keyboard.Button.X))
            {
                Console.WriteLine("Closed early");
                window.Destroy();
                return true; //finished this function early, theres already a conditional check for window destruction
            }

            if (keyboard.WasPressed(Keyboard.Button.R))
            {
                bool isResizable = !window.IsResizable();
                Console.WriteLine($"Resizable {isResizable}");
                window.SetResizable(isResizable);
            }

            if (keyboard.WasPressed(Keyboard.Button.B))
            {
                bool isBorderless = !window.IsBorderless();
                Console.WriteLine($"Borderless {isBorderless}");
                window.SetBorderless(isBorderless);
            }

            if (keyboard.WasPressed(Keyboard.Button.F))
            {
                Console.WriteLine("Window fullscreen state toggled");
                if (window.IsFullscreen())
                {
                    window.BecomeWindowed();
                }
                else
                {
                    window.BecomeFullscreen();
                }
            }

            if (keyboard.WasPressed(Keyboard.Button.N))
            {
                window.SetMinimized(!window.IsMinimized());
                Console.WriteLine($"Window hidden state toggled to {window.IsMinimized()}");
            }

            if (keyboard.WasPressed(Keyboard.Button.M))
            {
                if (window.IsMaximized())
                {
                    window.BecomeWindowed();
                }
                else
                {
                    window.BecomeMaximized();
                }

                Console.WriteLine($"Window maximized state toggled {window.IsMaximized()}");
            }

            ButtonState left = keyboard.GetButtonState(Keyboard.Button.Left);
            ButtonState right = keyboard.GetButtonState(Keyboard.Button.Right);
            ButtonState up = keyboard.GetButtonState(Keyboard.Button.Up);
            ButtonState down = keyboard.GetButtonState(Keyboard.Button.Down);
            ButtonState shift = keyboard.GetButtonState(Keyboard.Button.LeftShift);
            ButtonState control = keyboard.GetButtonState(Keyboard.Button.LeftControl);
            ButtonState reset = keyboard.GetButtonState(Keyboard.Button.Space);
            Vector2 direction = default;
            if (left.IsPressed)
            {
                direction.X -= 1;
            }

            if (right.IsPressed)
            {
                direction.X += 1;
            }

            if (up.IsPressed)
            {
                direction.Y -= 1;
            }

            if (down.IsPressed)
            {
                direction.Y += 1;
            }

            if (shift.IsPressed)
            {
                direction *= 3;
            }

            //either move the window, or resize the window, controlled by the control key
            float speed = 120f;
            direction *= speed;
            if (control.IsPressed)
            {
                windowSize += direction * (float)delta.TotalSeconds;
                windowPosition -= direction * (float)delta.TotalSeconds * 0.5f;
            }
            else
            {
                windowPosition += direction * (float)delta.TotalSeconds;
            }

            //lerp window to a fixed position and size when holding space
            if (reset.IsPressed)
            {
                float resetSpeed = 2f;
                if (control.IsPressed)
                {
                    resetSpeed *= 3f;
                }

                windowPosition = Vector2.Lerp(windowPosition, new(300, 300), (float)delta.TotalSeconds * resetSpeed);
                windowSize = Vector2.Lerp(windowSize, new(400, 400), (float)delta.TotalSeconds * resetSpeed);
            }
        }

        window.SetPosition(windowPosition);
        window.SetSize(windowSize);
        return true;
    }

    private readonly void TestMouseInputs()
    {
        foreach (eint mouseEntity in world.GetAll<IsMouse>())
        {
            Mouse mouse = new(world, mouseEntity);
            Vector2 position = mouse.GetPosition();
            ButtonState left = mouse.GetButtonState(Mouse.Button.LeftButton);
            if (left.WasPressed)
            {
                Console.WriteLine($"Left button pressed at {position}");
            }
            else if (left.WasReleased)
            {
                Console.WriteLine($"Left button released at {position}");
            }

            ButtonState right = mouse.GetButtonState(Mouse.Button.RightButton);
            if (right.WasPressed)
            {
                Console.WriteLine($"Right button pressed at {position}");
            }
            else if (right.WasReleased)
            {
                Console.WriteLine($"Right button released at {position}");
            }
        }
    }

    private void MoveCameraAround(TimeSpan delta)
    {
        ref Vector3 position = ref camera.GetPositionRef();
        ref Quaternion rotation = ref camera.GetRotationRef();

        //move around with keyboard or gamepad
        bool moveLeft = false;
        bool moveRight = false;
        bool moveForward = false;
        bool moveBackward = false;
        bool moveUp = false;
        bool moveDown = false;
        foreach (eint keyboardEntity in world.GetAll<IsKeyboard>())
        {
            Keyboard keyboard = new(world, keyboardEntity);
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

        if (moveDirection.LengthSquared() > 0)
        {
            moveDirection = Vector3.Normalize(moveDirection) * moveSpeed;
        }

        cameraPosition += Vector3.Transform(moveDirection, rotation) * (float)delta.TotalSeconds;
        position = Vector3.Lerp(position, cameraPosition, (float)delta.TotalSeconds * positionLerpSpeed);

        //look around with mice
        foreach (eint mouseEntity in world.GetAll<IsMouse>())
        {
            Mouse mouse = new(world, mouseEntity);
            Vector2 pointerPosition = mouse.GetPosition();
            if (lastPointerPosition == default)
            {
                lastPointerPosition = pointerPosition;
            }

            Vector2 pointerMoveDelta = pointerPosition - lastPointerPosition;
            lastPointerPosition = pointerPosition;
            cameraPitchYaw.X += pointerMoveDelta.X * 0.01f;
            cameraPitchYaw.Y += pointerMoveDelta.Y * 0.01f;
            cameraPitchYaw.Y = Math.Clamp(cameraPitchYaw.Y, -MathF.PI * 0.5f, MathF.PI * 0.5f);
        }

        Quaternion pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitY, cameraPitchYaw.X);
        Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitX, cameraPitchYaw.Y);
        rotation = pitch * yaw;
    }
}