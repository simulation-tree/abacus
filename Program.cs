using Meshes;
using Rendering;
using Rendering.Components;
using Shaders;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms.Components;
using Unmanaged;
using Windows;
using Windows.Components;

public struct Program : IDisposable
{
    private DateTime lastTime;
    private TimeSpan time;
    private readonly World world;
    private readonly Window window;

    public unsafe Program(World world)
    {
        this.world = world;
        lastTime = DateTime.Now;

        //build host
        window = new(world, "Window", new(100, 100), new(900, 720), "vulkan", new(&WindowClosed));

        //build scene
        Camera camera = new(world, window.AsDestination(), false, 90f * MathF.PI / 180f);
        camera.SetPosition(0f, 0f, -10f);

        Mesh mesh = new(world);
        Mesh.Collection<Vector3> positions = mesh.CreatePositions();
        Mesh.Collection<Vector2> uvs = mesh.CreateUVs();
        Mesh.Collection<Vector3> normals = mesh.CreateNormals();
        Mesh.Collection<Vector4> colors = mesh.CreateColors();

        //simple quad
        positions.Add(new(-1, -1, 0));
        positions.Add(new(1, -1, 0));
        positions.Add(new(1, 1, 0));
        positions.Add(new(-1, 1, 0));

        uvs.Add(new(0, 0));
        uvs.Add(new(1, 0));
        uvs.Add(new(1, 1));
        uvs.Add(new(0, 1));

        normals.Add(new(0, 0, 1));
        normals.Add(new(0, 0, 1));
        normals.Add(new(0, 0, 1));
        normals.Add(new(0, 0, 1));

        colors.Add(new(1, 0, 0, 1));
        colors.Add(new(0, 1, 0, 1));
        colors.Add(new(0, 0, 1, 1));
        colors.Add(new(1, 1, 1, 1));

        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3);

        Texture texture = new(world, "Tester/Assets/Textures/texture.jpg");
        Shader shader = new(world, "Tester/Assets/Shaders/unlit.vert", "Tester/Assets/Shaders/unlit.frag");
        Material material = new(world, shader);
        material.AddComponentBinding(0, 0, default, RuntimeType.Get<Color>(), ShaderStage.Vertex);
        material.AddComponentBinding(1, 0, default, RuntimeType.Get<LocalToWorld>(), ShaderStage.Vertex);
        material.AddComponentBinding(2, 0, camera, RuntimeType.Get<CameraProjection>(), ShaderStage.Vertex);
        material.AddTextureBinding(3, 0, texture);

        Renderer renderer = new(world, mesh, material, camera);

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
            return false;
        }

        Vector2 windowPosition = window.GetPosition();
        Vector2 windowSize = window.GetSize();
        foreach (eint keyboardEntity in world.GetAll<IsKeyboard>())
        {
            Keyboard keyboard = new(world, keyboardEntity);
            if (keyboard.WasPressed(Keyboard.Button.Escape))
            {
                return false;
            }

            if (keyboard.WasPressed(Keyboard.Button.X))
            {
                Console.WriteLine("Closed early");
                window.Destroy();
                return false; //otherwise exceptions later on, this is basically `continue` for the `Update` method
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

            if (reset.IsPressed)
            {
                float resetSpeed = 2f;
                if (control.IsPressed)
                {
                    resetSpeed *= 3f;
                }

                windowPosition = Vector2.Lerp(windowPosition, new(100, 100), (float)delta.TotalSeconds * resetSpeed);
                windowSize = Vector2.Lerp(windowSize, new(400, 400), (float)delta.TotalSeconds * resetSpeed);
            }
        }

        window.SetPosition(windowPosition);
        window.SetSize(windowSize);

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

        return true;
    }

    public readonly struct Color
    {
        public readonly Vector4 value;

        public Color(Vector4 value)
        {
            this.value = value;
        }
    }
}