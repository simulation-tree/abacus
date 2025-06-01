using Cameras;
using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using InputDevices;
using Materials;
using Meshes;
using Models;
using Physics;
using Physics.Functions;
using Physics.Messages;
using Rendering;
using Shapes.Types;
using System;
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
    public class PhysicsDemo : Program
    {
        private readonly Window window;
        private readonly Camera camera;
        private readonly Transform cameraTransform;
        private readonly Body ballBody;
        private readonly Body floorBody;
        private readonly Entity quadEntity;
        private readonly Mesh cubeMesh;
        private readonly Mesh sphereMesh;
        private readonly Material unlitMaterial;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        public unsafe PhysicsDemo(Application application) : base(application)
        {
            LayerMask firstLayer = new(0);

            window = new(world, "Physics Demo", new Vector2(400, 200), new(900, 720), "vulkan", new(&WindowClosed));
            window.IsResizable = true;
            window.CursorState = CursorState.HiddenAndConfined;

            camera = new(world, window, CameraSettings.CreatePerspectiveDegrees(90f), firstLayer);
            cameraTransform = camera.Become<Transform>();
            cameraTransform.LocalPosition = new(-1f, 2f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

            Texture squareTexture = new(world, EmbeddedResource.GetAddress<SquareTexture>());

            unlitMaterial = new(world, EmbeddedResource.GetAddress<UnlitTexturedMaterial>());
            unlitMaterial.AddInstanceBinding<Color>();
            unlitMaterial.AddInstanceBinding<LocalToWorld>();
            unlitMaterial.AddComponentBinding<CameraMatrices>(new(0, 0), camera);
            unlitMaterial.AddTextureBinding(new(1, 0), squareTexture);

            Model cubeModel = new(world, EmbeddedResource.GetAddress<CubeModel>());
            cubeMesh = new Mesh(world, cubeModel);
            Model sphereModel = new(world, EmbeddedResource.GetAddress<SphereModel>());
            sphereMesh = new(world, sphereModel);

            //create ball
            ballBody = new(world, new SphereShape(0.5f), BodyType.Dynamic, new Vector3(0f, 3f, 0f));
            Entity ballEntity = ballBody;
            MeshRenderer ballRenderer = ballEntity.Become<MeshRenderer>();
            ballRenderer.Mesh = sphereMesh;
            ballRenderer.Material = unlitMaterial;
            ballRenderer.RenderMask = firstLayer;

            ballEntity.AddComponent(Color.Red);
            Transform ballTransform = ballEntity.Become<Transform>();
            ballTransform.LocalPosition = new(0f, 4f, 0f);

            //create floor
            floorBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), BodyType.Static);
            Entity floorEntity = floorBody;
            MeshRenderer floorRenderer = floorEntity.Become<MeshRenderer>();
            floorRenderer.Mesh = cubeMesh;
            floorRenderer.Material = unlitMaterial;
            floorRenderer.RenderMask = firstLayer;

            floorEntity.AddComponent(Color.Green);

            //create directional gravity
            DirectionalGravity downGravity = new(world, -Vector3.UnitY);

            //create floating quad
            Model quadModel = new(world, EmbeddedResource.GetAddress<QuadModel>());
            Mesh quadMesh = new(world, quadModel);
            quadEntity = new(world);
            MeshRenderer quadRenderer = quadEntity.Become<MeshRenderer>();
            quadRenderer.Mesh = quadMesh;
            quadRenderer.Material = unlitMaterial;
            quadRenderer.RenderMask = firstLayer;

            quadEntity.AddComponent(Color.Blue);
            Transform quadTransform = quadEntity.Become<Transform>();
            quadTransform.LocalPosition = new(-2f, 2f, 0f);

            [UnmanagedCallersOnly]
            static void WindowClosed(Window window)
            {
                window.Dispose();
            }
        }

        public override void Dispose()
        {
        }

        public override bool Update(double deltaTime)
        {
            if (window.IsDestroyed)
            {
                return false;
            }

            if (world.TryGetFirst(out Keyboard keyboard))
            {
                if (keyboard.WasPressed(Keyboard.Button.Escape))
                {
                    return false;
                }

                Transform floorTransform = floorBody;
                if (keyboard.WasPressed(Keyboard.Button.G))
                {
                    using RandomGenerator rng = new();
                    ballBody.LinearVelocity += new Vector3(rng.NextFloat(-1f, 1f), 4, rng.NextFloat(-1f, 1f));
                }

                //tilt floor
                float tiltSpeed = 0.2f;
                if (keyboard.IsPressed(Keyboard.Button.Left))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, tiltSpeed * (float)deltaTime);
                }

                if (keyboard.IsPressed(Keyboard.Button.Right))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -tiltSpeed * (float)deltaTime);
                }

                if (keyboard.IsPressed(Keyboard.Button.Up))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, tiltSpeed * (float)deltaTime);
                }

                if (keyboard.IsPressed(Keyboard.Button.Down))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, -tiltSpeed * (float)deltaTime);
                }

                //resize floor
                float sizeChangeSpeed = 4f;
                if (keyboard.IsPressed(Keyboard.Button.J))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X - sizeChangeSpeed * (float)deltaTime, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z);
                }

                if (keyboard.IsPressed(Keyboard.Button.L))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X + sizeChangeSpeed * (float)deltaTime, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z);
                }

                if (keyboard.IsPressed(Keyboard.Button.I))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z + sizeChangeSpeed * (float)deltaTime);
                }

                if (keyboard.IsPressed(Keyboard.Button.K))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z - sizeChangeSpeed * (float)deltaTime);
                }

                //reset ball
                if (keyboard.WasPressed(Keyboard.Button.R))
                {
                    ballBody.LinearVelocity = Vector3.Zero;
                    ballBody.AngularVelocity = Vector3.Zero;

                    Transform ballTransform = ballBody;
                    ballTransform.LocalPosition = new Vector3(0f, 4f, 0f) + floorTransform.LocalPosition;
                }

                //reset floor tilt
                if (keyboard.WasPressed(Keyboard.Button.T))
                {
                    floorTransform.LocalRotation = Quaternion.Identity;
                }
            }

            if (world.TryGetFirst(out Mouse mouse))
            {
                //Vector2 screenPoint = camera.Destination.GetScreenPointFromPosition(mouse.Position);
                Vector2 screenPoint = new(0.5f, 0.5f);
                CameraMatrices cameraProjection = camera.Matrices;
                (Vector3 origin, Vector3 direction) = cameraProjection.GetRayFromScreenPoint(screenPoint);
                unsafe
                {
                    RaycastRequest raycast = new(origin, direction, new(&RaycastHitCallback), 5f, (ulong)(deltaTime * 10000));
                    simulator.Broadcast(raycast);
                }

                if (mouse.WasPressed(Mouse.Button.RightButton))
                {
                    //debug line
                    //Renderer startCube = new(world, sphereMesh, unlitMaterial, camera);
                    //((Entity)startCube).AddComponent(Color.Yellow);
                    //((Entity)startCube).AddComponent(new DestroyAfterTime(1f));
                    //Transform debugTransform = ((Entity)startCube).Become<Transform>();
                    //debugTransform.LocalPosition = origin;
                    //debugTransform.LocalScale = new(0.1f, 0.1f, 0.1f);
                    //
                    //Renderer endCube = new(world, sphereMesh, unlitMaterial, camera);
                    //((Entity)endCube).AddComponent(Color.Red);
                    //((Entity)endCube).AddComponent(new DestroyAfterTime(1f));
                    //Transform debugTransform2 = ((Entity)endCube).Become<Transform>();
                    //debugTransform2.LocalPosition = origin + direction;
                    //debugTransform2.LocalScale = new(0.1f, 0.1f, 0.1f);

                    Vector3 launchForce = Vector3.Normalize(cameraTransform.WorldForward + Vector3.UnitY * 0.2f) * 8f;
                    Body projectile = new(world, new SphereShape(0.5f), BodyType.Dynamic, launchForce);
                    Entity projectileEntity = projectile;
                    MeshRenderer projectileRenderer = projectileEntity.Become<MeshRenderer>();
                    projectileRenderer.Mesh = sphereMesh;
                    projectileRenderer.Material = unlitMaterial;
                    projectileRenderer.RenderMask = new LayerMask(1);

                    projectileEntity.AddComponent(Color.White);
                    projectileEntity.AddComponent(new DestroyAfterTime(5f));
                    Transform projectileTransform = projectile;
                    projectileTransform.LocalPosition = cameraTransform.WorldPosition;
                    projectileTransform.LocalScale = new(0.4f, 0.4f, 0.4f);
                }
            }

            SharedFunctions.MoveCameraAround(world, cameraTransform, deltaTime, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            SharedFunctions.DestroyTemporaryEntities(world, deltaTime);
            return true;
        }

        [UnmanagedCallersOnly]
        private unsafe static void RaycastHitCallback(RaycastHitCallback.Input input)
        {
            World world = input.world;
            double deltaTime = input.request.userData / 10000.0;
            ReadOnlySpan<RaycastHit> hits = input.Hits;
            foreach (RaycastHit hit in hits)
            {
                uint entityHit = hit.entity;
                ref Color color = ref world.TryGetComponent<Color>(entityHit, out bool contains);
                if (contains)
                {
                    float hue = color.Hue;
                    hue += 0.3f * (float)deltaTime;
                    while (hue > 1f)
                    {
                        hue -= 1f;
                    }

                    color.Hue = hue;
                }

                if (world.TryGetFirst(out Mouse mouse))
                {
                    if (mouse.IsPressed(Mouse.Button.LeftButton))
                    {
                        Transform transform = Entity.Get<Transform>(world, hit.entity);
                        transform.LocalPosition -= hit.normal * (float)deltaTime * 4f;
                    }
                }
            }
        }
    }
}