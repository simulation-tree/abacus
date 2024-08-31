using Data;
using DefaultPresentationAssets;
using Fonts;
using InputDevices;
using InputDevices.Components;
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

namespace Abacus
{
    public struct AbacusProgram : IDisposable, IProgram
    {
        private readonly World world;

        private TimeSpan time;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        private readonly Window window;
        private readonly Camera camera;
        private readonly Renderer dummyRenderer;
        private readonly Renderer testRenderer;
        private readonly Texture waveImage;
        private readonly Texture testImage;
        private readonly TextMesh exampleTextMesh;
        private readonly Renderer squareBox;

        public unsafe AbacusProgram(World world)
        {
            this.world = world;

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
            if (!world.TryGetFirst<IsCamera>(out uint cameraEntity))
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
            cameraTransform.LocalPosition = new(0f, 0f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

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

            Model quadModel = new(world, Address.Get<QuadModel>());
            Mesh quadMesh = new(world, quadModel);
            testImage = new(world, "*/Assets/Textures/texture.jpg");
            //Shader shader = new(world, "*/Assets/Shaders/unlit.vertex.glsl", "*/Assets/Shaders/unlit.fragment.glsl");

            Material material = new(world, Address.Get<UnlitTexturedMaterial>());
            material.AddPushBinding<Color>();
            material.AddPushBinding<LocalToWorld>();
            material.AddComponentBinding<CameraProjection>(0, 0, camera);
            material.AddTextureBinding(1, 0, testImage);

            dummyRenderer = new(world, quadMesh, material, camera);
            ((Entity)dummyRenderer).AddComponent(Color.Yellow);
            ((Entity)dummyRenderer).Become<Transform>();

            waveImage = new(world, "*/Assets/Textures/wave.png");
            Material testMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            testMaterial.AddPushBinding<Color>();
            testMaterial.AddPushBinding<LocalToWorld>();
            testMaterial.AddComponentBinding<CameraProjection>(0, 0, camera);
            testMaterial.AddTextureBinding(1, 0, waveImage);

            Texture squareTexture = new(world, Address.Get<SquareTexture>());
            Material defaultSquareMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            defaultSquareMaterial.AddPushBinding<Color>();
            defaultSquareMaterial.AddPushBinding<LocalToWorld>();
            defaultSquareMaterial.AddComponentBinding<CameraProjection>(0, 0, camera);
            defaultSquareMaterial.AddTextureBinding(1, 0, squareTexture);
            squareBox = new(world, quadMesh, defaultSquareMaterial, camera);
            ((Entity)squareBox).AddComponent(Color.Red);
            Transform squareTransform = ((Entity)squareBox).Become<Transform>();
            squareTransform.LocalPosition = new(8, 4, -2);
            squareTransform.LocalScale = new(4, 4, 1);

            //font entity (reusable)
            Font cascadiaMono = new(world, Address.Get<CascadiaMonoFont>());

            //material entity (reusable)
            Material textMaterial = new(world, Address.Get<TextMaterial>());
            textMaterial.AddComponentBinding<CameraProjection>(1, 0, camera);
            textMaterial.AddPushBinding<Color>();
            textMaterial.AddPushBinding<LocalToWorld>();

            //mesh entity (reusable if text is the same)
            exampleTextMesh = new TextMesh(world, "hiii <3", cascadiaMono);

            //render request itself (not reusable)
            TextRenderer text = new(world, exampleTextMesh, textMaterial, camera);
            text.Parent = squareBox;
            Entity textEntity = text;
            textEntity.AddComponent(Color.Green);
            textEntity.AddComponent(Anchor.BottomLeft);
            Transform textTransform = textEntity.Become<Transform>();
            textTransform.LocalPosition = new(0f, 0f, -0.1f);

            TextMesh anotherTextMesh = new(world, "top right corner?", cascadiaMono, new(1f, 1f));
            TextRenderer anotherText = new(world, anotherTextMesh, textMaterial, camera);
            anotherText.Parent = squareBox;
            Entity anotherTextEntity = anotherText;
            anotherTextEntity.AddComponent(Color.Blue);
            anotherTextEntity.AddComponent(Anchor.TopRight);
            Transform anotherTextTransform = anotherTextEntity.Become<Transform>();
            anotherTextTransform.LocalPosition = new(0f, 0f, -0.1f);

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
            testTransform.LocalPosition = new(-7, -4, -2);
            testTransform.LocalScale = new(8f, 8f, 1f);

            [UnmanagedCallersOnly]
            static void WindowClosed(World world, uint windowEntity)
            {
                world.DestroyEntity(windowEntity);
            }
        }

        public void Dispose()
        {
            if (!window.IsDestroyed)
            {
                window.Destroy();
            }
        }

        public bool Update(TimeSpan delta)
        {
            time += delta;
            if (time.TotalSeconds > 120f || window.IsDestroyed)
            {
                Console.WriteLine("Conditions reached for finishing the demo");
                return false; //source of "shutdown" event
            }

            float deltaSeconds = (float)delta.TotalSeconds;
            TestMouseInputs();
            AnimateTestRenderer(deltaSeconds);
            Transform cameraTransform = ((Entity)camera).Become<Transform>();
            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            ModifyText();
            if (!TestWindowEntity(deltaSeconds))
            {
                //propagating upwards
                return false;
            }

            return true;
        }

        private readonly void ModifyText()
        {
            if (Entity.TryGetFirst(world, out Keyboard keyboard))
            {
                if (keyboard.IsPressed(Keyboard.Button.G))
                {
                    exampleTextMesh.SetText(Guid.NewGuid().ToString());
                }

                if (keyboard.WasPressed(Keyboard.Button.T))
                {
                    exampleTextMesh.SetText(DateTime.Now.ToString());
                }
                else if (keyboard.WasPressed(Keyboard.Button.L))
                {
                    string[] options = [
                        "hey boo",
                        "hi sugar",
                        "hello sweetie",
                        "sup hun",
                        "yo babe",
                        "aye fam",
                        "hiya love",
                        "hey darling",
                        "hi dear"
                    ];
                    using RandomGenerator rng = new();
                    exampleTextMesh.SetText(options[rng.NextInt(options.Length)]);
                }
            }
        }

        private readonly void AnimateTestRenderer(float delta)
        {
            foreach (uint keyboardEntity in world.GetAll<IsKeyboard>())
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
                    bool shouldToggle = textureBinding.TextureEntity == (Entity)waveImage;
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
            foreach (uint keyboardEntity in world.GetAll<IsKeyboard>())
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
                    direction.Y += 1;
                }

                if (down.IsPressed)
                {
                    direction.Y -= 1;
                }

                if (shift.IsPressed)
                {
                    direction *= 3;
                }

                if (keyboard.IsPressed(Keyboard.Button.B))
                {
                    float speed = 20f;
                    Transform squareBoxTransform = ((Entity)squareBox).As<Transform>();
                    if (alt.IsPressed)
                    {
                        squareBoxTransform.LocalPosition += new Vector3(direction, 0) * delta * speed;
                    }
                    else
                    {
                        squareBoxTransform.LocalScale += new Vector3(direction, 0) * delta * speed;
                    }
                }
                else
                {
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
            }

            window.Position = windowPosition;
            window.Size = windowSize;
            return true;
        }

        private readonly void TestMouseInputs()
        {
            foreach (uint mouseEntity in world.GetAll<IsMouse>())
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