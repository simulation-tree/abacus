using Meshes;
using Rendering;
using Shaders;
using Simulation;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows;
using Windows.Components;

public struct Program : IDisposable
{
    private DateTime lastTime;
    private TimeSpan time;
    private Vector2 windowPosition;
    private Vector2 windowSize;
    private readonly World world;
    private readonly Window window;

    public unsafe Program(World world)
    {
        this.world = world;
        lastTime = DateTime.Now;

        //build host
        window = new(world, "Window", new(100, 100), new(900, 720), "vulkan", new(&WindowClosed));
        windowPosition = window.GetPositionAsVector2();
        windowSize = window.GetSizeAsVector2();

        //build scene
        Camera camera = new(world, window.AsDestination(), false, 90f * MathF.PI / 180f);
        camera.SetPosition(0f, 0f, -10f);

        Mesh mesh = new(world);
        Mesh.Collection<Vector3> positions = mesh.CreatePositions();
        Mesh.Collection<Vector2> uvs = mesh.CreateUVs();
        Mesh.Collection<Vector3> normals = mesh.CreateNormals();

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

        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3);

        Shader shader = new(world, "Tester/Assets/Shaders/unlit.vert", "Tester/Assets/Shaders/unlit.frag");
        Material material = new(world, shader);
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
        if (time.TotalSeconds > 30f || window.IsDestroyed())
        {
            return false;
        }

        foreach (eint keyboardEntity in world.GetAll<IsKeyboard>())
        {
            Keyboard keyboard = new(world, keyboardEntity);
            if (keyboard.WasPressed((uint)Keyboard.Button.Escape))
            {
                return false;
            }

            if (keyboard.WasPressed((uint)Keyboard.Button.X))
            {
                Console.WriteLine("Closed");
                window.Destroy();
            }

            if (keyboard.WasPressed((uint)Keyboard.Button.R))
            {
                Console.WriteLine("Window resizable state toggled");
                window.SetResizable(!window.IsResizable());
            }

            if (keyboard.WasPressed((uint)Keyboard.Button.B))
            {
                Console.WriteLine("Window borderless state toggled");
                window.SetBorderless(!window.IsBorderless());
            }

            if (keyboard.WasPressed((uint)Keyboard.Button.F))
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

            if (keyboard.IsPressed((uint)Keyboard.Button.N))
            {
                Thread.Sleep(20);
                Console.WriteLine("N");
            }

            if (keyboard.IsPressed((uint)Keyboard.Button.G))
            {
                Thread.Sleep(20);
                Console.WriteLine($"G");
            }

            if (keyboard.WasPressed((uint)Keyboard.Button.N))
            {
                window.SetMinimized(!window.IsMinimized());
                Console.WriteLine($"Window hidden state toggled to {window.IsMinimized()}");
            }

            if (keyboard.WasPressed((uint)Keyboard.Button.M))
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

            ButtonState left = keyboard.GetButtonState((uint)Keyboard.Button.Left);
            ButtonState right = keyboard.GetButtonState((uint)Keyboard.Button.Right);
            ButtonState up = keyboard.GetButtonState((uint)Keyboard.Button.Up);
            ButtonState down = keyboard.GetButtonState((uint)Keyboard.Button.Down);
            ButtonState shift = keyboard.GetButtonState((uint)Keyboard.Button.LeftShift);
            ButtonState control = keyboard.GetButtonState((uint)Keyboard.Button.LeftControl);
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

            direction *= 64f;
            if (control.IsPressed)
            {
                windowSize += direction * (float)delta.TotalSeconds;
                windowPosition -= direction * (float)delta.TotalSeconds * 0.5f;
            }
            else
            {
                windowPosition += direction * (float)delta.TotalSeconds;
            }
        }

        window.SetPosition(windowPosition);
        window.SetSize(windowSize);

        foreach (eint mouseEntity in world.GetAll<IsMouse>())
        {
            Mouse mouse = new(world, mouseEntity);
            Vector2 position = mouse.GetPosition();
            ButtonState left = mouse.GetButtonState((uint)Mouse.Button.LeftButton);
            if (left.WasPressed)
            {
                Console.WriteLine($"Left button pressed at {position}");
            }
            else if (left.WasReleased)
            {
                Console.WriteLine($"Left button released at {position}");
            }

            ButtonState right = mouse.GetButtonState((uint)Mouse.Button.RightButton);
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
}