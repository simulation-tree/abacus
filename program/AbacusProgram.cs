using Data;
using DefaultPresentationAssets;
using Fonts;
using Meshes;
using Models;
using Programs;
using Rendering;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Windows;
using Windows.Components;

namespace Abacus
{
    public struct AbacusProgram : IDisposable, IProgram
    {
        private readonly World world;

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
        private readonly Texture waveImage;
        private readonly Texture testImage;
        private readonly TextMesh exampleTextMesh;

        public unsafe AbacusProgram(World world)
        {
            this.world = world;
            moveSpeed = 4f;
            lookSensitivity = 2f;
            invertY = true;
            positionLerpSpeed = 12f;

            //load scene built in unity
            try
            {
                //DataRequest scene = new(world, "*/Assets/Cav.world");
                //using BinaryReader reader = new(scene.GetBytes());
                //using World sceneWorld = reader.ReadObject<World>();
                //world.Clear();
                //world.Append(sceneWorld);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load scene, skipping that because of {ex}");
            }

            //build host
            window = new(world, "Window", new(100, 100), new(900, 720), "vulkan", new(&WindowClosed));

            //find existing camera or create new one
            if (!world.TryGetFirst<IsCamera>(out eint cameraEntity))
            {
                camera = new(world, window, CameraFieldOfView.FromDegrees(90f));
            }
            else
            {
                camera = new(world, cameraEntity);
                camera.Destination = window;
            }

            //build scene
            Transform cameraTransform = ((Entity)camera).Become<Transform>();
            cameraTransform.Position = new(0f, 0f, -10f);
            cameraPosition = cameraTransform.Position;

            Mesh manuallyBuiltMesh = new(world);
            Mesh.Collection<Vector3> positions = manuallyBuiltMesh.CreatePositions();
            Mesh.Collection<Vector2> uvs = manuallyBuiltMesh.CreateUVs();
            Mesh.Collection<Vector3> normals = manuallyBuiltMesh.CreateNormals();
            Mesh.Collection<Color> colors = manuallyBuiltMesh.CreateColors();

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

            manuallyBuiltMesh.AddTriangle(0, 1, 2);
            manuallyBuiltMesh.AddTriangle(2, 3, 0);

            Model quadModel = new(world, Address.Get<QuadMesh>());
            Mesh quadMesh = new(world, quadModel);
            testImage = new(world, "*/Assets/Textures/texture.jpg");
            //Shader shader = new(world, "*/Assets/Shaders/unlit.vertex.glsl", "*/Assets/Shaders/unlit.fragment.glsl");

            Material material = new(world, Address.Get<UnlitTexturedMaterial>());
            material.AddPushBinding(RuntimeType.Get<Color>());
            material.AddPushBinding(RuntimeType.Get<LocalToWorld>());
            material.AddComponentBinding(0, 0, camera, RuntimeType.Get<CameraProjection>());
            material.AddTextureBinding(1, 0, testImage);

            dummyRenderer = new(world, quadMesh, material, camera);
            ((Entity)dummyRenderer).AddComponent(Color.Yellow);
            ((Entity)dummyRenderer).Become<Transform>();

            waveImage = new(world, "*/Assets/Textures/wave.png");
            Material testMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            testMaterial.AddPushBinding(RuntimeType.Get<Color>());
            testMaterial.AddPushBinding(RuntimeType.Get<LocalToWorld>());
            testMaterial.AddComponentBinding(0, 0, camera, RuntimeType.Get<CameraProjection>());
            testMaterial.AddTextureBinding(1, 0, waveImage);

            Font cascadiaMono = new(world, Address.Get<CascadiaMonoFont>());
            exampleTextMesh = new TextMesh(world, "hiii <3", cascadiaMono);
            Material textMaterial = new(world, Address.Get<TextMaterial>());
            textMaterial.AddComponentBinding(1, 0, camera, RuntimeType.Get<CameraProjection>());
            textMaterial.AddPushBinding(RuntimeType.Get<Color>());
            textMaterial.AddPushBinding(RuntimeType.Get<LocalToWorld>());

            TextRenderer text = new(world, exampleTextMesh, textMaterial, camera);
            ((Entity)text).AddComponent(Color.Green);
            Transform textTransform = ((Entity)text).Become<Transform>();
            textTransform.Position = new(-10, 2, 0);

            //to verify 2 renderers + 1 material + 1 shader + 2 meshes
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

            testRenderer = new(world, manuallyBuiltMesh, testMaterial, camera);
            ((Entity)testRenderer).AddComponent(Color.White);
            Transform testTransform = ((Entity)testRenderer).Become<Transform>();
            testTransform.Position = new(-7, -4, -2);
            testTransform.Scale = new(8f, 8f, 1f);

            [UnmanagedCallersOnly]
            static void WindowClosed(World world, eint windowEntity)
            {
                world.DestroyEntity(windowEntity);
            }
        }

        public void Dispose()
        {
            if (!window.IsDestroyed)
            {
                window.Dispose();
            }
        }

        public bool Update(TimeSpan deltaSpan)
        {
            time += deltaSpan;
            if (time.TotalSeconds > 120f || window.IsDestroyed)
            {
                Console.WriteLine("Conditions reached for finishing the demo");
                return false; //source of "shutdown" event
            }

            float delta = (float)deltaSpan.TotalSeconds;
            TestMouseInputs();
            AnimateTestRenderer(delta);
            MoveCameraAround(delta);
            ModifyText();
            if (!TestWindowEntity(delta))
            {
                //propagating upwards
                return false;
            }

            return true;
        }

        private readonly void ModifyText()
        {
            foreach (eint keyboardEntity in world.GetAll<IsKeyboard>())
            {
                Keyboard keyboard = new(world, keyboardEntity);
                if (keyboard.IsPressed(Keyboard.Button.G))
                {
                    exampleTextMesh.SetText(Guid.NewGuid().ToString());
                }
                
                if (keyboard.WasPressed(Keyboard.Button.T))
                {
                    exampleTextMesh.SetText(DateTime.Now.ToString());
                }
            }
        }

        private readonly void AnimateTestRenderer(float delta)
        {
            foreach (eint keyboardEntity in world.GetAll<IsKeyboard>())
            {
                Keyboard keyboard = new(world, keyboardEntity);
                if (keyboard.WasPressed(Keyboard.Button.J))
                {
                    dummyRenderer.IsEnabled = !dummyRenderer.IsEnabled;
                }

                if (keyboard.IsPressed(Keyboard.Button.O))
                {
                    Mesh mesh = dummyRenderer.Mesh;
                    Mesh.Collection<Vector3> positions = mesh.Positions;
                    float revolveSpeed = 2f;
                    float revolveDistance = 0.7f;
                    float x = MathF.Sin((float)time.TotalSeconds * revolveSpeed) * revolveDistance;
                    float y = MathF.Cos((float)time.TotalSeconds * revolveSpeed) * revolveDistance;
                    positions[0] = Vector3.Lerp(positions[0], new(x, y, 0), delta * 3f);
                    positions[1] = Vector3.Lerp(positions[1], new(x + 1, y, 0), delta * 4f);
                    positions[2] = Vector3.Lerp(positions[2], new(x + 1, y + 1, 0), delta * 2f);
                    positions[3] = Vector3.Lerp(positions[3], new(x, y + 1, 0), delta * 5f);
                    //would look a lot lot cooler with many more vertices :o
                }

                if (keyboard.WasPressed(Keyboard.Button.E))
                {
                    Material dummyMaterial = dummyRenderer.Material;
                    ref MaterialTextureBinding textureBinding = ref dummyMaterial.GetTextureBindingRef(1, 0);
                    bool shouldToggle = textureBinding.TextureEntity == waveImage;
                    textureBinding.SetTexture(shouldToggle ? testImage : waveImage);

                    using RandomGenerator rng = new();
                    float x = rng.NextFloat(0.666f);
                    float y = rng.NextFloat(0.666f);
                    float w = 0.333f;
                    float h = 0.333f;
                    textureBinding.SetRegion(x, y, w, h);
                }
            }

            ref Color color = ref ((Entity)dummyRenderer).GetComponent<Color>();
            float hue = color.H;
            hue += delta * 0.2f;
            while (hue > 1f)
            {
                hue -= 1f;
            }

            color.H = hue;
        }

        private readonly bool TestWindowEntity(float delta)
        {
            Vector2 windowPosition = window.Position;
            Vector2 windowSize = window.Size;
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
                    window.Dispose();
                    return true; //finished this function early, theres already a conditional check for window destruction
                }

                if (keyboard.WasPressed(Keyboard.Button.R))
                {
                    bool isResizable = !window.IsResizable;
                    Console.WriteLine($"Resizable {isResizable}");
                    window.IsResizable = isResizable;
                }

                if (keyboard.WasPressed(Keyboard.Button.B))
                {
                    bool isBorderless = !window.IsBorderless;
                    Console.WriteLine($"Borderless {isBorderless}");
                    window.IsBorderless = isBorderless;
                }

                if (keyboard.WasPressed(Keyboard.Button.F))
                {
                    Console.WriteLine("Window fullscreen state toggled");
                    if (window.IsFullscreen)
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
                    window.IsMinimized = !window.IsMinimized;
                    Console.WriteLine($"Window hidden state toggled to {window.IsMinimized}");
                }

                if (keyboard.WasPressed(Keyboard.Button.M))
                {
                    if (window.IsMaximized)
                    {
                        window.BecomeWindowed();
                    }
                    else
                    {
                        window.BecomeMaximized();
                    }

                    Console.WriteLine($"Window maximized state toggled {window.IsMaximized}");
                }

                ButtonState left = keyboard.GetButtonState(Keyboard.Button.Left);
                ButtonState right = keyboard.GetButtonState(Keyboard.Button.Right);
                ButtonState up = keyboard.GetButtonState(Keyboard.Button.Up);
                ButtonState down = keyboard.GetButtonState(Keyboard.Button.Down);
                ButtonState shift = keyboard.GetButtonState(Keyboard.Button.LeftShift);
                ButtonState alt = keyboard.GetButtonState(Keyboard.Button.LeftAlt);
                ButtonState reset = keyboard.GetButtonState(Keyboard.Button.V);
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
                if (alt.IsPressed)
                {
                    windowSize += direction * delta;
                    windowPosition -= direction * delta * 0.5f;
                }
                else
                {
                    windowPosition += direction * delta;
                }

                //lerp window to a fixed position and size when holding the V key
                if (reset.IsPressed)
                {
                    float resetSpeed = 2f;
                    if (alt.IsPressed)
                    {
                        resetSpeed *= 3f;
                    }

                    windowPosition = Vector2.Lerp(windowPosition, new(300, 300), delta * resetSpeed);
                    windowSize = Vector2.Lerp(windowSize, new(400, 400), delta * resetSpeed);
                }
            }

            window.Position = windowPosition;
            window.Size = windowSize;
            return true;
        }

        private readonly void TestMouseInputs()
        {
            foreach (eint mouseEntity in world.GetAll<IsMouse>())
            {
                Mouse mouse = new(world, mouseEntity);
                Vector2 position = mouse.Position;
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

        private void MoveCameraAround(float delta)
        {
            Transform cameraTransform = ((Entity)camera).Become<Transform>();
            Vector3 position = cameraTransform.Position;
            Quaternion rotation = cameraTransform.Rotation;

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

            cameraPosition += Vector3.Transform(moveDirection, rotation) * delta;
            position = Vector3.Lerp(position, cameraPosition, delta * positionLerpSpeed);

            //look around with mice
            foreach (eint mouseEntity in world.GetAll<IsMouse>())
            {
                Mouse mouse = new(world, mouseEntity);
                Vector2 pointerPosition = mouse.Position;
                if (lastPointerPosition == default)
                {
                    lastPointerPosition = pointerPosition;
                }

                Vector2 pointerMoveDelta = (pointerPosition - lastPointerPosition) * lookSensitivity;
                if (invertY)
                {
                    pointerMoveDelta.Y *= -1;
                }

                lastPointerPosition = pointerPosition;
                cameraPitchYaw.X += pointerMoveDelta.X * 0.01f;
                cameraPitchYaw.Y += pointerMoveDelta.Y * 0.01f;
                cameraPitchYaw.Y = Math.Clamp(cameraPitchYaw.Y, -MathF.PI * 0.5f, MathF.PI * 0.5f);
            }

            Quaternion pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitY, cameraPitchYaw.X);
            Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitX, cameraPitchYaw.Y);
            rotation = pitch * yaw;

            cameraTransform.Position = position;
            cameraTransform.Rotation = rotation;
        }

        unsafe (StartFunction, FinishFunction, UpdateFunction) IProgram.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                AbacusProgram program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref AbacusProgram program = ref allocation.Read<AbacusProgram>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref AbacusProgram program = ref allocation.Read<AbacusProgram>();
                return program.Update(delta) ? 0u : 1u;
            }
        }
    }
}