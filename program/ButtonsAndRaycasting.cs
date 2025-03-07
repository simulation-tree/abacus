using Cameras;
using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using Fonts;
using InputDevices;
using Materials;
using Meshes;
using Models;
using Physics;
using Physics.Components;
using Physics.Events;
using Rendering;
using Shapes.Types;
using Simulation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms;
using Transforms.Components;
using UI;
using UI.Components;
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

        private readonly World World => window.world;

        void IProgram.Start(in Simulator simulator, in MemoryAddress allocation, in World world)
        {
            allocation.Write(new ButtonsAndRaycasting(simulator, world));
        }

        unsafe StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (window.IsDestroyed)
            {
                return StatusCode.Success(0);
            }

            Transform cameraTransform = worldCamera.Become<Transform>();
            SharedFunctions.MoveCameraAround(World, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));

            if (World.TryGetFirst(out Mouse mouse))
            {
                if (!mouse.ContainsComponent<IsPointer>())
                {
                    mouse.AddComponent(new IsPointer(mouse.Position));
                }

                ref IsPointer pointer = ref mouse.GetComponent<IsPointer>();
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
                    (Vector3 origin, Vector3 direction) ray = worldCamera.Matrices.GetRayFromScreenPoint(screenPoint);
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

        void IProgram.Finish(in StatusCode statusCode)
        {
            if (!window.IsDestroyed)
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

            worldCamera = new(world, window, CameraSettings.CreatePerspectiveDegrees(90));
            Transform cameraTransform = worldCamera.Become<Transform>();
            cameraTransform.LocalPosition = new(0, 0, -10);
            cameraPosition = cameraTransform.LocalPosition;

            Camera uiCamera = new(world, window, CameraSettings.CreateOrthographic(1f));

            //global references
            Texture squareTexture = new(world, EmbeddedResource.GetAddress<SquareTexture>());
            Model cubeModel = new(world, EmbeddedResource.GetAddress<CubeModel>());
            Mesh cubeMesh = new(world, cubeModel);
            Font robotoFont = new(world, EmbeddedResource.GetAddress<RobotoFont>());

            Material unlitWorldMaterial = new(world, EmbeddedResource.GetAddress<UnlitTexturedMaterial>());
            unlitWorldMaterial.AddInstanceBinding<Color>();
            unlitWorldMaterial.AddInstanceBinding<LocalToWorld>();
            unlitWorldMaterial.AddComponentBinding<CameraMatrices>(new(0, 0), worldCamera);
            unlitWorldMaterial.AddTextureBinding(new(1, 0), squareTexture);

            Settings settings = new(world);
            Canvas canvas = new(settings, uiCamera);

            Material textMaterial = new(world, EmbeddedResource.GetAddress<TextMaterial>());
            textMaterial.AddComponentBinding<CameraMatrices>(new(1, 0), uiCamera);
            textMaterial.AddInstanceBinding<Color>();
            textMaterial.AddInstanceBinding<LocalToWorld>();

            //crate test cube
            MeshRenderer waveRenderer = new(world, cubeMesh, unlitWorldMaterial, worldCamera.RenderMask);
            Transform waveTransform = waveRenderer.Become<Transform>();
            waveRenderer.AddComponent(Color.Red);
            waveRenderer.AddComponent(new IsBody(new CubeShape(0.5f), BodyType.Static));

            //create ui boxes
            Button testWindow = new(new(&TestBoxPressed), canvas);
            testWindow.Size = new(300, 300);
            testWindow.Position = new(20, 20);

            Button anotherBox = new(new(&AnotherBoxPressed), canvas);
            anotherBox.Size = new(190, 100);
            anotherBox.Position = new(0, 0);
            anotherBox.Anchor = Anchor.Centered;
            anotherBox.Pivot = new(0.5f, 0.5f, 0f);
            anotherBox.Color = new(0, 0.5f, 1, 1);

            TextMesh textMesh = new(world, "abacus 123 hiii", robotoFont);
            TextRenderer textRenderer = new(world, textMesh, textMaterial, uiCamera.RenderMask);
            textRenderer.SetParent(anotherBox);
            Transform textTransform = textRenderer.Become<Transform>();
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