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
using Rendering;
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
using Worlds;

namespace Abacus
{
    public partial struct ButtonsAndRaycasting : IProgram
    {
        private readonly Simulator simulator;
        private readonly Window window;
        private readonly Camera worldCamera;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        private readonly World World => window.GetWorld();

        void IProgram.Initialize(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new ButtonsAndRaycasting(simulator, world));
        }

        unsafe StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (window.IsDestroyed())
            {
                return StatusCode.Success(1);
            }

            Transform cameraTransform = worldCamera.AsEntity().Become<Transform>();
            SharedFunctions.MoveCameraAround(World, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));

            if (World.TryGetFirst(out Mouse mouse))
            {
                if (!mouse.AsEntity().ContainsComponent<IsPointer>())
                {
                    mouse.AddComponent(new IsPointer(mouse.Position, default));
                }

                ref IsPointer pointer = ref mouse.AsEntity().GetComponent<IsPointer>();
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
                    simulator.TryHandleMessage(new RaycastRequest(World, ray.origin, ray.direction, new(&OnRaycastHit)));

                    [UnmanagedCallersOnly]
                    static void OnRaycastHit(World world, RaycastRequest raycast, RaycastHit* hits, uint hitsCount)
                    {
                        for (uint i = 0; i < hitsCount; i++)
                        {
                            RaycastHit hit = hits[i];
                            ref Position position = ref world.TryGetComponent<Position>(hit.entity, out bool contains);
                            if (contains)
                            {
                                position.value.X += 0.1f;
                            }
                        }
                    }
                }
            }

            return StatusCode.Continue;
        }

        void IDisposable.Dispose()
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }
        }

        private unsafe ButtonsAndRaycasting(Simulator simulator, World world)
        {
            this.simulator = simulator;

            //window to render everything
            window = new(world, "Fly", default, new(900, 600), "vulkan", new(&WindowClosed));
            window.IsResizable = true;
            window.BecomeMaximized();

            worldCamera = new(world, window, CameraFieldOfView.FromDegrees(90));
            Transform cameraTransform = worldCamera.AsEntity().Become<Transform>();
            cameraTransform.LocalPosition = new(0, 0, -10);
            cameraPosition = cameraTransform.LocalPosition;

            Camera uiCamera = new(world, window, new CameraOrthographicSize(1f));

            //global references
            Texture squareTexture = new(world, Address.Get<SquareTexture>());
            Model cubeModel = new(world, Address.Get<CubeModel>());
            Mesh cubeMesh = new(world, cubeModel);
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
            MeshRenderer waveRenderer = new(world, cubeMesh, unlitWorldMaterial, worldCamera.GetMask());
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
            TextRenderer textRenderer = new(world, textMesh, textMaterial, uiCamera.GetMask());
            textRenderer.SetParent(anotherBox);
            Transform textTransform = textRenderer.AsEntity().Become<Transform>();
            textTransform.LocalPosition = new(4f, -4f, 0.1f);
            textTransform.LocalScale = Vector3.One * 32f;
            textRenderer.AddComponent(Color.Orange);
            textRenderer.AddComponent(Anchor.TopLeft);
            textRenderer.AddComponent(Pivot.TopLeft);

            [UnmanagedCallersOnly]
            static void TestBoxPressed(Entity entity)
            {
                Trace.WriteLine("Test box pressed");
            }

            [UnmanagedCallersOnly]
            static void AnotherBoxPressed(Entity entity)
            {
                Trace.WriteLine("Another box pressed");
            }

            [UnmanagedCallersOnly]
            static void WindowClosed(Window window)
            {
                window.Dispose();
            }
        }
    }
}