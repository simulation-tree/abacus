using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using Meshes;
using Models;
using Physics;
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
    public struct EditorProgram : IDisposable, IProgram
    {
        private readonly World world;
        private readonly Window window;
        private readonly Canvas canvas;
        private readonly Box testWindow;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        public unsafe EditorProgram(World world)
        {
            this.world = world;

            //window to render everything
            window = new(world, "Fly", default, new(900, 600), "vulkan", new(&WindowClosed));
            window.IsResizable = true;
            window.BecomeMaximized();

            Camera worldCamera = new(world, window, CameraFieldOfView.FromDegrees(90));
            Entity worldCameraEntity = worldCamera;
            Transform cameraTransform = worldCameraEntity.Become<Transform>();
            cameraTransform.LocalPosition = new(0, 0, -10);
            cameraPosition = cameraTransform.LocalPosition;

            Camera uiCamera = new(world, window, new CameraOrthographicSize(1f));

            //global references
            Texture squareTexture = new(world, Address.Get<SquareTexture>());
            Model quadModel = new(world, Address.Get<QuadModel>());
            Mesh quadMesh = new(world, quadModel);
            
            Material unlitUiMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            unlitUiMaterial.AddPushBinding<Color>();
            unlitUiMaterial.AddPushBinding<LocalToWorld>();
            unlitUiMaterial.AddComponentBinding<CameraProjection>(0, 0, uiCamera);
            unlitUiMaterial.AddTextureBinding(1, 0, squareTexture);
            
            Material unlitWorldMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            unlitWorldMaterial.AddPushBinding<Color>();
            unlitWorldMaterial.AddPushBinding<LocalToWorld>();
            unlitWorldMaterial.AddComponentBinding<CameraProjection>(0, 0, worldCamera);
            unlitWorldMaterial.AddTextureBinding(1, 0, squareTexture);
            Transform canvasTransform = new(world);
            canvas = new(quadMesh, unlitUiMaterial, unlitWorldMaterial, worldCamera, uiCamera, squareTexture, canvasTransform);

            //create window
            testWindow = new(world, canvas);
            testWindow.Size = new(300, 300);
            testWindow.Position = new(20, 20);

            //crate test image
            Renderer waveRenderer = new(world, canvas.quadMesh, canvas.unlitWorldMaterial, worldCamera);
            Entity waveEntity = waveRenderer;
            Transform waveTransform = waveEntity.Become<Transform>();
            waveTransform.LocalScale = new(2, 2, 1);
            waveTransform.LocalPosition = new(2, 2, 1);
            waveEntity.AddComponent(Color.Red);

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
            if (window.IsDestroyed)
            {
                return false;
            }

            UpdateCanvasToMatchWindow();
            Transform cameraTransform = ((Entity)canvas.worldCamera).Become<Transform>();
            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            return true;
        }

        private void UpdateCanvasToMatchWindow()
        {
            Vector2 size = window.Size;
            canvas.transform.LocalScale = new(size, 1);
            canvas.transform.LocalPosition = new(0, 0, canvas.uiCamera.Depth.min + 0.1f);
        }

        readonly unsafe (StartFunction, FinishFunction, UpdateFunction) IProgram.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                EditorProgram program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref EditorProgram program = ref allocation.Read<EditorProgram>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref EditorProgram program = ref allocation.Read<EditorProgram>();
                return program.Update(delta) ? 0u : 1u;
            }
        }

        public readonly struct Canvas
        {
            public readonly Mesh quadMesh;
            public readonly Material unlitUiMaterial;
            public readonly Material unlitWorldMaterial;
            public readonly Camera worldCamera;
            public readonly Camera uiCamera;
            public readonly Texture squareTexture;
            public readonly Transform transform;

            public Canvas(Mesh quadMesh, Material unlitUiMaterial, Material unlitWorldMaterial, Camera worldCamera, Camera uiCamera, Texture squareTexture, Transform canvas)
            {
                this.quadMesh = quadMesh;
                this.unlitUiMaterial = unlitUiMaterial;
                this.unlitWorldMaterial = unlitWorldMaterial;
                this.worldCamera = worldCamera;
                this.uiCamera = uiCamera;
                this.squareTexture = squareTexture;
                this.transform = canvas;
            }
        }

        public readonly struct Box : IEntity
        {
            private readonly Entity entity;

            public readonly Vector2 Size
            {
                get
                {
                    Transform transform = entity.As<Transform>();
                    Vector3 scale = transform.LocalScale;
                    return new(scale.X, scale.Y);
                }
                set
                {
                    Transform transform = entity.Become<Transform>();
                    Vector3 scale = transform.LocalScale;
                    transform.LocalScale = new(value.X, value.Y, scale.Z);
                }
            }

            public readonly Vector2 Position
            {
                get
                {
                    Transform transform = entity.As<Transform>();
                    Vector3 position = transform.LocalPosition;
                    return new(position.X, position.Y);
                }
                set
                {
                    Transform transform = entity.Become<Transform>();
                    Vector3 position = transform.LocalPosition;
                    transform.LocalPosition = new(value.X, value.Y, position.Z);
                }
            }

            uint IEntity.Value => entity;
            World IEntity.World => entity;

#if NET
            [Obsolete("Not available", true)]
            public Box()
            {

            }
#endif

            public Box(World world, Canvas canvas)
            {
                entity = new(world);
                entity.Become<Box>();
                entity.Become<Transform>();
                entity.Become<Body>();
                entity.AddComponent(Color.White);

                ref IsRenderer renderer = ref entity.AddComponentRef<IsRenderer>();
                renderer.mesh = entity.AddReference(canvas.quadMesh);
                renderer.material = entity.AddReference(canvas.unlitUiMaterial);
                renderer.camera = entity.AddReference(canvas.uiCamera);
            }

            Query IEntity.GetQuery(World world)
            {
                return new(world, RuntimeType.Get<IsBox>());
            }

            public static implicit operator Entity(Box window)
            {
                return window.entity;
            }

            public static implicit operator Transform(Box window)
            {
                return window.entity.As<Transform>();
            }

            public static implicit operator Body(Box window)
            {
                return window.entity.As<Body>();
            }
        }

        public struct IsBox
        {

        }
    }
}