using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using InputDevices;
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
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Windows;

namespace Abacus
{
    public struct PhysicsDemo : IDisposable, IProgramType
    {
        private readonly World world;
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

        public unsafe PhysicsDemo(World world)
        {
            this.world = world;
            window = new(world, "Physics Demo", new Vector2(400, 200), new(900, 720), "vulkan", new(&WindowClosed));
            window.IsResizable = true;

            camera = new(world, window.destination, CameraFieldOfView.FromDegrees(90f));
            cameraTransform = camera.entity.Become<Transform>();
            cameraTransform.LocalPosition = new(-1f, 2f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

            Texture squareTexture = new(world, Address.Get<SquareTexture>());

            unlitMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            unlitMaterial.AddPushBinding<Color>();
            unlitMaterial.AddPushBinding<LocalToWorld>();
            unlitMaterial.AddComponentBinding<CameraProjection>(0, 0, camera.entity);
            unlitMaterial.AddTextureBinding(1, 0, squareTexture);

            Model cubeModel = new(world, Address.Get<CubeModel>());
            cubeMesh = new Mesh(world, cubeModel.entity);
            Model sphereModel = new(world, Address.Get<SphereModel>());
            sphereMesh = new(world, sphereModel.entity);

            //create ball
            ballBody = new(world, new SphereShape(0.5f), IsBody.Type.Dynamic, new Vector3(0f, 3f, 0f));
            Entity ballEntity = ballBody.transform.entity;
            Renderer ballRenderer = ballEntity.Become<Renderer>();
            ballRenderer.Mesh = sphereMesh;
            ballRenderer.Material = unlitMaterial;
            ballRenderer.Camera = camera;
            ballEntity.AddComponent(Color.Red);
            Transform ballTransform = ballEntity.Become<Transform>();
            ballTransform.LocalPosition = new(0f, 4f, 0f);

            //create floor
            floorBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), IsBody.Type.Static);
            Entity floorEntity = floorBody.transform.entity;
            Renderer floorRenderer = floorEntity.Become<Renderer>();
            floorRenderer.Mesh = cubeMesh;
            floorRenderer.Material = unlitMaterial;
            floorRenderer.Camera = camera;
            floorEntity.AddComponent(Color.Green);

            //create directional gravity
            DirectionalGravity downGravity = new(world, -Vector3.UnitY);

            //create floating quad
            Model quadModel = new(world, Address.Get<QuadModel>());
            Mesh quadMesh = new(world, quadModel.entity);
            quadEntity = new(world);
            Renderer quadRenderer = quadEntity.Become<Renderer>();
            quadRenderer.Mesh = quadMesh;
            quadRenderer.Material = unlitMaterial;
            quadRenderer.Camera = camera;
            quadEntity.AddComponent(Color.Blue);
            Transform quadTransform = quadEntity.Become<Transform>();
            quadTransform.LocalPosition = new(-2f, 2f, 0f);

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

        public uint Update(TimeSpan delta)
        {
            if (window.IsDestroyed())
            {
                return default;
            }

            if (world.TryGetFirst(out Keyboard keyboard))
            {
                Transform floorTransform = floorBody.transform;
                if (keyboard.WasPressed(Keyboard.Button.G))
                {
                    using RandomGenerator rng = new();
                    ballBody.LinearVelocity += new Vector3(rng.NextFloat(-1f, 1f), 4, rng.NextFloat(-1f, 1f));
                }

                //tilt floor
                float tiltSpeed = 0.2f;
                if (keyboard.IsPressed(Keyboard.Button.Left))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, tiltSpeed * (float)delta.TotalSeconds);
                }

                if (keyboard.IsPressed(Keyboard.Button.Right))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -tiltSpeed * (float)delta.TotalSeconds);
                }

                if (keyboard.IsPressed(Keyboard.Button.Up))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, tiltSpeed * (float)delta.TotalSeconds);
                }

                if (keyboard.IsPressed(Keyboard.Button.Down))
                {
                    floorTransform.LocalRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, -tiltSpeed * (float)delta.TotalSeconds);
                }

                //resize floor
                float sizeChangeSpeed = 4f;
                if (keyboard.IsPressed(Keyboard.Button.J))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X - sizeChangeSpeed * (float)delta.TotalSeconds, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z);
                }

                if (keyboard.IsPressed(Keyboard.Button.L))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X + sizeChangeSpeed * (float)delta.TotalSeconds, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z);
                }

                if (keyboard.IsPressed(Keyboard.Button.I))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z + sizeChangeSpeed * (float)delta.TotalSeconds);
                }

                if (keyboard.IsPressed(Keyboard.Button.K))
                {
                    floorTransform.LocalScale = new(floorTransform.LocalScale.X, floorTransform.LocalScale.Y, floorTransform.LocalScale.Z - sizeChangeSpeed * (float)delta.TotalSeconds);
                }

                //reset ball
                if (keyboard.WasPressed(Keyboard.Button.R))
                {
                    ballBody.LinearVelocity = Vector3.Zero;
                    ballBody.AngularVelocity = Vector3.Zero;
                    Transform ballTransform = ballBody.transform;
                    ballTransform.WorldPosition = new Vector3(0f, 4f, 0f) + floorTransform.WorldPosition;
                }

                //reset floor tilt
                if (keyboard.WasPressed(Keyboard.Button.T))
                {
                    floorTransform.LocalRotation = Quaternion.Identity;
                }
            }

            if (world.TryGetFirst(out Mouse mouse))
            {
                Vector2 screenPoint = camera.Destination.GetScreenPointFromPosition(mouse.Position);
                CameraProjection cameraProjection = camera.GetProjection();
                (Vector3 origin, Vector3 direction) = cameraProjection.GetRayFromScreenPoint(screenPoint);
                unsafe
                {
                    Raycast raycast = new(origin, direction, new(&RaycastHitCallback), 5f, (ulong)delta.Ticks);
                    world.Submit(raycast);
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
                    Body projectile = new(world, new SphereShape(0.5f), IsBody.Type.Dynamic, launchForce);
                    Entity projectileEntity = projectile.transform.entity;
                    Renderer projectileRenderer = projectileEntity.Become<Renderer>();
                    projectileRenderer.Mesh = sphereMesh;
                    projectileRenderer.Material = unlitMaterial;
                    projectileRenderer.Camera = camera;
                    projectileEntity.AddComponent(Color.White);
                    projectileEntity.AddComponent(new DestroyAfterTime(5f));
                    Transform projectileTransform = projectile.transform;
                    projectileTransform.LocalPosition = cameraTransform.WorldPosition;
                    projectileTransform.LocalScale = new(0.4f, 0.4f, 0.4f);
                }
            }

            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            SharedFunctions.DestroyTemporaryEntities(world, delta);
            return 1;
        }

        [UnmanagedCallersOnly]
        private unsafe static void RaycastHitCallback(World world, Raycast raycast, RaycastHit* hitsPointer, uint hitCount)
        {
            TimeSpan delta = new((long)raycast.identifier);
            USpan<RaycastHit> hits = new(hitsPointer, hitCount);
            foreach (RaycastHit hit in hits)
            {
                uint entityHit = hit.entity;
                ref Color color = ref world.TryGetComponentRef<Color>(entityHit, out bool contains);
                if (contains)
                {
                    float hue = color.H;
                    hue += 0.3f * (float)delta.TotalSeconds;
                    while (hue > 1f)
                    {
                        hue -= 1f;
                    }

                    color.H = hue;
                }

                if (world.TryGetFirst(out Mouse mouse))
                {
                    if (mouse.IsPressed(Mouse.Button.LeftButton))
                    {
                        Transform transform = new(world, hit.entity);
                        transform.WorldPosition -= hit.normal * (float)delta.TotalSeconds * 4f;
                    }
                }
            }
        }

        readonly unsafe (StartFunction, FinishFunction, UpdateFunction) IProgramType.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                PhysicsDemo program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref PhysicsDemo program = ref allocation.Read<PhysicsDemo>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref PhysicsDemo program = ref allocation.Read<PhysicsDemo>();
                return program.Update(delta);
            }
        }
    }
}