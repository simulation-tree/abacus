using Cameras;
using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using Fonts;
using InputDevices;
using InteractionKit;
using InteractionKit.Components;
using Meshes;
using Models;
using Physics;
using Physics.Components;
using Physics.Events;
using Programs;
using Programs.Functions;
using Rendering;
using Rendering.Components;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Windows;

namespace Abacus
{
    public struct ButtonsAndProjectiles : IProgram
    {
        private readonly Window window;
        private readonly Camera worldCamera;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        unsafe readonly StartFunction IProgram.Start => new(&Start);
        unsafe readonly UpdateFunction IProgram.Update => new(&Update);
        unsafe readonly FinishFunction IProgram.Finish => new(&Finish);

        [UnmanagedCallersOnly]
        private static void Start(Simulator simulator, Allocation allocation, World world)
        {
            allocation.Write(new ButtonsAndProjectiles(world));
        }

        [UnmanagedCallersOnly]
        private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            ref ButtonsAndProjectiles program = ref allocation.Read<ButtonsAndProjectiles>();
            return program.Update(simulator, world, delta);
        }

        [UnmanagedCallersOnly]
        private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
            ref ButtonsAndProjectiles program = ref allocation.Read<ButtonsAndProjectiles>();
            program.CleanUp();
        }

        private unsafe ButtonsAndProjectiles(World world)
        {
            //window to render everything
            window = new(world, "Fly", default, new(900, 600), "vulkan", new(&WindowClosed));
            window.IsResizable = true;
            window.BecomeMaximized();

            worldCamera = new(world, window.destination, CameraFieldOfView.FromDegrees(90));
            Transform cameraTransform = worldCamera.AsEntity().Become<Transform>();
            cameraTransform.LocalPosition = new(0, 0, -10);
            cameraPosition = cameraTransform.LocalPosition;

            Camera uiCamera = new(world, window.destination, new CameraOrthographicSize(1f));

            //global references
            Texture squareTexture = new(world, Address.Get<SquareTexture>());
            Model cubeModel = new(world, Address.Get<CubeModel>());
            Mesh cubeMesh = new(world, cubeModel.entity);
            Font robotoFont = new(world, Address.Get<RobotoFont>());

            Material unlitWorldMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            unlitWorldMaterial.AddPushBinding<Color>();
            unlitWorldMaterial.AddPushBinding<LocalToWorld>();
            unlitWorldMaterial.AddComponentBinding<CameraMatrices>(0, 0, worldCamera);
            unlitWorldMaterial.AddTextureBinding(1, 0, squareTexture);

            Settings settings = new(world);
            Canvas canvas = new(world, uiCamera);

            Material textMaterial = new(world, Address.Get<TextMaterial>());
            textMaterial.AddComponentBinding<CameraMatrices>(1, 0, uiCamera);
            textMaterial.AddPushBinding<Color>();
            textMaterial.AddPushBinding<LocalToWorld>();

            //crate test cube
            MeshRenderer waveRenderer = new(world, cubeMesh, unlitWorldMaterial, worldCamera.Mask);
            Transform waveTransform = waveRenderer.AsEntity().Become<Transform>();
            waveRenderer.AddComponent(Color.Red);
            waveRenderer.AddComponent(new IsBody(new CubeShape(0.5f), IsBody.Type.Static));

            //create ui boxes
            Button testWindow = new(world, new(&TestBoxPressed), canvas);
            testWindow.Size = new(300, 300);
            testWindow.Position = new(20, 20);

            Button anotherBox = new(world, new(&AnotherBoxPressed), canvas);
            anotherBox.Size = new(190, 100);
            anotherBox.Position = new(0, 0);
            anotherBox.Anchor = Anchor.Centered;
            anotherBox.Pivot = new(0.5f, 0.5f, 0f);
            anotherBox.Color = Color.SkyBlue;

            TextMesh textMesh = new(world, "abacus 123 hiii", robotoFont);
            TextRenderer textRenderer = new(world, textMesh, textMaterial, uiCamera.Mask);
            textRenderer.Parent = anotherBox.AsEntity();
            Transform textTransform = textRenderer.entity.Become<Transform>();
            textTransform.LocalPosition = new(4f, -4f, 0.1f);
            textTransform.LocalScale = Vector3.One * 32f;
            textRenderer.AddComponent(Color.Orange);
            textRenderer.AddComponent(Anchor.TopLeft);
            textRenderer.AddComponent(Pivot.TopLeft);

            [UnmanagedCallersOnly]
            static void TestBoxPressed(Entity entity)
            {
                Debug.WriteLine("Test box pressed");
            }

            [UnmanagedCallersOnly]
            static void AnotherBoxPressed(Entity entity)
            {
                Debug.WriteLine("Another box pressed");
            }

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

        private unsafe uint Update(Simulator simulator, World world, TimeSpan delta)
        {
            if (window.IsDestroyed())
            {
                return 1;
            }

            Transform cameraTransform = worldCamera.AsEntity().Become<Transform>();
            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));

            if (world.TryGetFirst(out Mouse mouse))
            {
                if (!mouse.device.entity.ContainsComponent<IsPointer>())
                {
                    mouse.device.entity.AddComponent(new IsPointer(mouse.Position, default));
                }

                ref IsPointer pointer = ref mouse.device.entity.GetComponentRef<IsPointer>();
                pointer.position = mouse.Position;
                pointer.action = default;
                pointer.scroll = mouse.Scroll;
                if (mouse.IsPressed(Mouse.Button.LeftButton))
                {
                    pointer.HasPrimaryIntent = true;
                }

                if (mouse.IsPressed(Mouse.Button.RightButton))
                {
                    pointer.HasSecondaryIntent = true;
                }

                if (mouse.WasPressed(Mouse.Button.LeftButton))
                {
                    Vector2 screenPoint = worldCamera.Destination.GetScreenPointFromPosition(mouse.Position);
                    (Vector3 origin, Vector3 direction) ray = worldCamera.GetMatrices().GetRayFromScreenPoint(screenPoint);
                    simulator.TryHandleMessage(new RaycastRequest(ray.origin, ray.direction, new(&OnRaycastHit)));

                    [UnmanagedCallersOnly]
                    static void OnRaycastHit(World world, RaycastRequest raycast, RaycastHit* hits, uint hitsCount)
                    {
                        for (uint i = 0; i < hitsCount; i++)
                        {
                            RaycastHit hit = hits[i];
                            ref Position position = ref world.TryGetComponentRef<Position>(hit.entity, out bool contains);
                            if (contains)
                            {
                                position.value.X += 0.1f;
                            }
                        }
                    }
                }
            }

            return default;
        }
    }
}