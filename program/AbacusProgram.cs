using Cameras;
using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using Fonts;
using InputDevices;
using InputDevices.Components;
using Meshes;
using Models;
using Rendering;
using Rendering.Components;
using Simulation;
using Simulation.Functions;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public struct AbacusProgram : IProgram
    {
        private TimeSpan time;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        private readonly Window window;
        private readonly Camera camera;
        private readonly MeshRenderer dummyRenderer;
        private readonly MeshRenderer testRenderer;
        private readonly Texture waveImage;
        private readonly Texture testImage;
        private readonly TextMesh exampleTextMesh;
        private readonly MeshRenderer squareBox;

        unsafe readonly StartProgram IProgram.Start => new(&Start);
        unsafe readonly UpdateProgram IProgram.Update => new(&Update);
        unsafe readonly FinishProgram IProgram.Finish => new(&Finish);

        [UnmanagedCallersOnly]
        private static void Start(Simulator simulator, Allocation allocation, World world)
        {
            allocation.Write(new AbacusProgram(world));
        }

        [UnmanagedCallersOnly]
        private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            ref AbacusProgram program = ref allocation.Read<AbacusProgram>();
            return program.Update(world, delta);
        }

        [UnmanagedCallersOnly]
        private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
            ref AbacusProgram program = ref allocation.Read<AbacusProgram>();
            program.CleanUp();
        }

        private unsafe AbacusProgram(World world)
        {
            //load scene built in unity
            try
            {
                //DataRequest scene = new(world, "Assets/Cav.world");
                //using BinaryReader reader = new(scene.GetBytes());
                //using World sceneWorld = reader.ReadObject<World>();
                //world.Clear();
                //world.Append(sceneWorld);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to load scene, skipping that because of {ex}");
            }

            //build host
            window = new(world, "Window", new(100, 100), new(900, 720), "vulkan", new(&WindowClosed));

            //find existing camera or create new one
            if (!world.TryGetFirstEntityWithComponent<IsCamera>(out uint cameraEntity))
            {
                camera = new(world, window, CameraFieldOfView.FromDegrees(90f));
            }
            else
            {
                camera = new(world, cameraEntity);
                camera.Destination = window;
            }

            //build scene
            Transform cameraTransform = camera.AsEntity().Become<Transform>();
            cameraTransform.LocalPosition = new(0f, 0f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

            Mesh manuallyBuiltMesh = new(world);
            USpan<Vector3> positions = manuallyBuiltMesh.CreatePositions(4);
            USpan<Vector2> uvs = manuallyBuiltMesh.CreateUVs(4);
            USpan<Vector3> normals = manuallyBuiltMesh.CreateNormals(4);
            USpan<Color> colors = manuallyBuiltMesh.CreateColors(4);

            //simple quad
            positions[0] = new(0, 0, 0);
            positions[1] = new(1, 0, 0);
            positions[2] = new(1, 1, 0);
            positions[3] = new(0, 1, 0);

            uvs[0] = new(0, 0);
            uvs[1] = new(1, 0);
            uvs[2] = new(1, 1);
            uvs[3] = new(0, 1);

            normals[0] = new(0, 0, 1);
            normals[1] = new(0, 0, 1);
            normals[2] = new(0, 0, 1);
            normals[3] = new(0, 0, 1);

            colors[0] = new(1, 1, 1, 1);
            colors[1] = new(1, 1, 1, 1);
            colors[2] = new(1, 1, 1, 1);
            colors[3] = new(1, 1, 1, 1);

            manuallyBuiltMesh.AddTriangle(0, 1, 2);
            manuallyBuiltMesh.AddTriangle(2, 3, 0);

            Model quadModel = new(world, Address.Get<QuadModel>());
            Mesh quadMesh = new(world, quadModel);
            testImage = new(world, "Assets/Textures/texture.jpg");
            //Shader shader = new(world, "Assets/Shaders/unlit.vertex.glsl", "Assets/Shaders/unlit.fragment.glsl");

            Material material = new(world, Address.Get<UnlitTexturedMaterial>());
            material.AddPushBinding<Color>();
            material.AddPushBinding<LocalToWorld>();
            material.AddComponentBinding<CameraMatrices>(0, 0, camera);
            material.AddTextureBinding(1, 0, testImage);

            dummyRenderer = new(world, quadMesh, material);
            dummyRenderer.AddComponent(Color.Yellow);
            dummyRenderer.AsEntity().Become<Transform>();

            waveImage = new(world, "Assets/Textures/wave.png");
            Material testMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            testMaterial.AddPushBinding<Color>();
            testMaterial.AddPushBinding<LocalToWorld>();
            testMaterial.AddComponentBinding<CameraMatrices>(0, 0, camera);
            testMaterial.AddTextureBinding(1, 0, waveImage);

            Texture squareTexture = new(world, Address.Get<SquareTexture>());
            Material defaultSquareMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            defaultSquareMaterial.AddPushBinding<Color>();
            defaultSquareMaterial.AddPushBinding<LocalToWorld>();
            defaultSquareMaterial.AddComponentBinding<CameraMatrices>(0, 0, camera);
            defaultSquareMaterial.AddTextureBinding(1, 0, squareTexture);

            squareBox = new(world, quadMesh, defaultSquareMaterial);
            squareBox.AddComponent(Color.Red);
            Transform squareTransform = squareBox.AsEntity().Become<Transform>();
            squareTransform.LocalPosition = new(8, 4, -2);
            squareTransform.LocalScale = new(4, 4, 1);

            //font entity (reusable)
            Font cascadiaMono = new(world, Address.Get<CascadiaMonoFont>());

            //material entity (reusable)
            Material textMaterial = new(world, Address.Get<TextMaterial>());
            textMaterial.AddComponentBinding<CameraMatrices>(1, 0, camera);
            textMaterial.AddPushBinding<Color>();
            textMaterial.AddPushBinding<LocalToWorld>();

            //mesh entity (reusable if text is the same)
            exampleTextMesh = new TextMesh(world, "hiii <3", cascadiaMono);

            //render request itself (not reusable)
            TextRenderer text = new(world, exampleTextMesh, textMaterial);
            text.SetParent(squareBox);
            Entity textEntity = text;
            textEntity.AddComponent(Color.Green);
            textEntity.AddComponent(Anchor.BottomLeft);
            Transform textTransform = textEntity.Become<Transform>();
            textTransform.LocalPosition = new(0f, 0f, -0.1f);

            TextMesh anotherTextMesh = new(world, "top right corner?", cascadiaMono);
            TextRenderer anotherText = new(world, anotherTextMesh, textMaterial);
            anotherText.SetParent(squareBox);
            Entity anotherTextEntity = anotherText;
            anotherTextEntity.AddComponent(Color.Blue);
            anotherTextEntity.AddComponent(Anchor.TopRight);
            anotherTextEntity.AddComponent(new Pivot(1f, 1f));
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

            testRenderer = new(world, manuallyBuiltMesh, testMaterial);
            testRenderer.AddComponent(Color.White);
            Transform testTransform = testRenderer.AsEntity().Become<Transform>();
            testTransform.LocalPosition = new(-7, -4, -2);
            testTransform.LocalScale = new(8f, 8f, 1f);
            testRenderer.AsEntity().AddComponent(new RendererScissor(100, 100, 200, 200));

            [UnmanagedCallersOnly]
            static void WindowClosed(Window window)
            {
                window.Dispose();
            }
        }

        private readonly void CleanUp()
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }
        }

        private uint Update(World world, TimeSpan delta)
        {
            time += delta;
            if (time.TotalSeconds > 120f || window.IsDestroyed())
            {
                Trace.WriteLine("Conditions reached for finishing the demo");
                return 1; //source of "shutdown" event
            }

            float deltaSeconds = (float)delta.TotalSeconds;
            TestMouseInputs(world);
            AnimateTestRenderer(world, deltaSeconds);
            Transform cameraTransform = camera.AsEntity().Become<Transform>();
            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            ModifyText(world);
            if (TestWindowEntity(world, deltaSeconds))
            {
                //propagating upwards
                return 2;
            }

            return 0;
        }

        private readonly void ModifyText(World world)
        {
            if (world.TryGetFirst(out Keyboard keyboard))
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

        private readonly void AnimateTestRenderer(World world, float delta)
        {
            foreach (uint keyboardEntity in world.GetAll<IsKeyboard>())
            {
                Keyboard keyboard = new(world, keyboardEntity);
                if (keyboard.WasPressed(Keyboard.Button.J))
                {
                    dummyRenderer.SetEnabled(!dummyRenderer.IsEnabled());
                }

                if (keyboard.IsPressed(Keyboard.Button.O))
                {
                    Mesh mesh = dummyRenderer.Mesh;
                    USpan<Vector3> positions = mesh.Positions;
                    float revolveSpeed = 2f;
                    float revolveDistance = 0.7f;
                    float x = MathF.Sin((float)time.TotalSeconds * revolveSpeed) * revolveDistance;
                    float y = MathF.Cos((float)time.TotalSeconds * revolveSpeed) * revolveDistance;
                    positions[0] = Vector3.Lerp(positions[0], new(x, y, 0), delta * 3f);
                    positions[1] = Vector3.Lerp(positions[1], new(x + 1, y, 0), delta * 4f);
                    positions[2] = Vector3.Lerp(positions[2], new(x + 1, y + 1, 0), delta * 2f);
                    positions[3] = Vector3.Lerp(positions[3], new(x, y + 1, 0), delta * 5f);
                    mesh.IncrementVersion();
                    //would look a lot lot cooler with many more vertices :o
                }

                if (keyboard.WasPressed(Keyboard.Button.E))
                {
                    Material dummyMaterial = dummyRenderer.Material;
                    ref MaterialTextureBinding textureBinding = ref dummyMaterial.GetTextureBindingRef(1, 0);
                    bool shouldToggle = textureBinding.TextureEntity == waveImage.GetEntityValue();
                    textureBinding.SetTexture(shouldToggle ? testImage : waveImage);

                    using RandomGenerator rng = new();
                    float x = rng.NextFloat(0.666f);
                    float y = rng.NextFloat(0.666f);
                    float w = 0.333f;
                    float h = 0.333f;
                    textureBinding.SetRegion(x, y, w, h);
                }
            }

            ref Color color = ref dummyRenderer.AsEntity().GetComponentRef<Color>();
            float hue = color.H;
            hue += delta * 0.2f;
            while (hue > 1f)
            {
                hue -= 1f;
            }

            color.H = hue;
        }

        private readonly bool TestWindowEntity(World world, float delta)
        {
            Vector2 windowPosition = window.Position;
            Vector2 windowSize = window.Size;
            foreach (uint keyboardEntity in world.GetAll<IsKeyboard>())
            {
                Keyboard keyboard = new(world, keyboardEntity);
                if (keyboard.WasPressed(Keyboard.Button.Escape))
                {
                    return true; //source of "shutdown" event
                }

                if (keyboard.WasPressed(Keyboard.Button.X))
                {
                    Trace.WriteLine("Closed early");
                    window.Dispose();
                    return false; //finished this function early, theres already a conditional check for window destruction
                }

                if (keyboard.WasPressed(Keyboard.Button.R))
                {
                    bool isResizable = !window.IsResizable;
                    Trace.WriteLine($"Resizable {isResizable}");
                    window.IsResizable = isResizable;
                }

                if (keyboard.WasPressed(Keyboard.Button.B))
                {
                    bool isBorderless = !window.IsBorderless;
                    Trace.WriteLine($"Borderless {isBorderless}");
                    window.IsBorderless = isBorderless;
                }

                if (keyboard.WasPressed(Keyboard.Button.F))
                {
                    Trace.WriteLine("Window fullscreen state toggled");
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
                    Trace.WriteLine($"Window hidden state toggled to {window.IsMinimized}");
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

                    Trace.WriteLine($"Window maximized state toggled {window.IsMaximized}");
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
                    Transform squareBoxTransform = squareBox.AsEntity().As<Transform>();
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
            return false;
        }

        private readonly void TestMouseInputs(World world)
        {
            foreach (uint mouseEntity in world.GetAll<IsMouse>())
            {
                Mouse mouse = new(world, mouseEntity);
                Vector2 position = mouse.Position;
                ButtonState left = mouse.GetButtonState(Mouse.Button.LeftButton);
                if (left.WasPressed)
                {
                    Trace.WriteLine($"Left button pressed at {position}");
                }
                else if (left.WasReleased)
                {
                    Trace.WriteLine($"Left button released at {position}");
                }

                ButtonState right = mouse.GetButtonState(Mouse.Button.RightButton);
                if (right.WasPressed)
                {
                    Trace.WriteLine($"Right button pressed at {position}");
                }
                else if (right.WasReleased)
                {
                    Trace.WriteLine($"Right button released at {position}");
                }
            }
        }
    }
}