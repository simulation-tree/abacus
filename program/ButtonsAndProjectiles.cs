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
    public struct ButtonsAndProjectiles : IDisposable, IProgramType
    {
        private readonly World world;
        private readonly Window window;
        public readonly InteractiveContext context;
        public readonly Camera worldCamera;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        public unsafe ButtonsAndProjectiles(World world)
        {
            this.world = world;

            //window to render everything
            window = new(world, "Fly", default, new(900, 600), "vulkan", new(&WindowClosed));
            window.IsResizable = true;
            window.BecomeMaximized();

            worldCamera = new(world, window.destination, CameraFieldOfView.FromDegrees(90));
            Transform cameraTransform = worldCamera.entity.Become<Transform>();
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
            unlitWorldMaterial.AddComponentBinding<CameraProjection>(0, 0, worldCamera.entity);
            unlitWorldMaterial.AddTextureBinding(1, 0, squareTexture);

            Canvas canvas = new(world, uiCamera);
            context = new(canvas);

            Material textMaterial = new(world, Address.Get<TextMaterial>());
            textMaterial.AddComponentBinding<CameraProjection>(1, 0, uiCamera.entity);
            textMaterial.AddPushBinding<Color>();
            textMaterial.AddPushBinding<LocalToWorld>();

            //crate test cube
            Renderer waveRenderer = new(world, cubeMesh, unlitWorldMaterial, worldCamera);
            Transform waveTransform = waveRenderer.entity.Become<Transform>();
            waveRenderer.entity.AddComponent(Color.Red);
            waveRenderer.entity.AddComponent(new IsBody(new CubeShape(0.5f), IsBody.Type.Static));

            //create ui boxes
            Button testWindow = new(world, new(&TestBoxPressed), context);
            testWindow.Size = new(300, 300);
            testWindow.Position = new(20, 20);

            Button anotherBox = new(world, new(&AnotherBoxPressed), context);
            anotherBox.Size = new(190, 100);
            anotherBox.Position = new(0, 0);
            anotherBox.Anchor = Anchor.Centered;
            anotherBox.Pivot = new(0.5f, 0.5f, 0f);
            anotherBox.Color = Color.SkyBlue;

            TextMesh textMesh = new(world, "abacus 123 hiii", robotoFont, new(0f, 1f));
            TextRenderer textRenderer = new(world, textMesh, textMaterial, uiCamera);
            textRenderer.Parent = anotherBox.AsEntity();
            Transform textTransform = textRenderer.entity.Become<Transform>();
            textTransform.LocalPosition = new(4f, -4f, 0.1f);
            textTransform.LocalScale = Vector3.One * 32f;
            textRenderer.entity.AddComponent(Color.Orange);
            textRenderer.entity.AddComponent(Anchor.TopLeft);

            [UnmanagedCallersOnly]
            static void TestBoxPressed(World world, uint entity)
            {
                Debug.WriteLine("Test box pressed");
            }

            [UnmanagedCallersOnly]
            static void AnotherBoxPressed(World world, uint entity)
            {
                Debug.WriteLine("Another box pressed");
            }

            [UnmanagedCallersOnly]
            static void WindowClosed(World world, uint windowEntity)
            {
                world.DestroyEntity(windowEntity);
            }
        }

        public void Dispose()
        {
            if (!window.IsDestroyed())
            {
                window.Destroy();
            }
        }

        public unsafe uint Update(TimeSpan delta)
        {
            if (window.IsDestroyed())
            {
                return default;
            }

            Transform cameraTransform = worldCamera.entity.Become<Transform>();
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
                    (Vector3 origin, Vector3 direction) ray = worldCamera.GetProjection().GetRayFromScreenPoint(screenPoint);
                    world.Submit(new Raycast(ray.origin, ray.direction, new(&OnRaycastHit)));

                    [UnmanagedCallersOnly]
                    static void OnRaycastHit(World world, Raycast raycast, RaycastHit* hits, uint hitsCount)
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

            return 1;
        }

        readonly unsafe (StartFunction, FinishFunction, UpdateFunction) IProgramType.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                ButtonsAndProjectiles program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref ButtonsAndProjectiles program = ref allocation.Read<ButtonsAndProjectiles>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref ButtonsAndProjectiles program = ref allocation.Read<ButtonsAndProjectiles>();
                return program.Update(delta);
            }
        }
    }
}