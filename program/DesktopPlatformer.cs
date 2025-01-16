using Automations;
using Cameras;
using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using InputDevices;
using InteractionKit;
using Meshes;
using Models;
using Physics;
using Physics.Components;
using Physics.Events;
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
using Worlds;

namespace Abacus
{
    public readonly partial struct DesktopPlatformer : IProgram
    {
        private const float Gravity = 14f;
        private const float DisplayScale = 0.01f;

        private readonly Simulator simulator;
        private readonly Window window;
        private readonly Camera camera;
        private readonly Body floorBody;
        private readonly Body leftWallBody;
        private readonly Body rightWallBody;

        private readonly World World => window.GetWorld();

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new DesktopPlatformer(simulator, world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (window.IsDestroyed())
            {
                return StatusCode.Success(0);
            }

            UpdateCollidersToMatchDisplay(World);
            (Vector2 direction, bool jump) = ReadInput(World);
            MovePlayer(World, direction, jump, (float)delta.TotalSeconds);
            AnimatePlayerParameters(World);
            CheckIfPlayersAreGrounded(simulator, World);
            MakeCameraFollowPlayer(World);
            MakeWindowFollowCamera(World);
            UpdateRegionToMatchAnimatedSprite(World);
            TeleportPlayerToMousePosition(World);
            CloseWindow(World);
            return StatusCode.Continue;
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }
        }

        private unsafe DesktopPlatformer(Simulator simulator, World world)
        {
            this.simulator = simulator;

            window = new(world, "Fly", default, new(200, 200), "vulkan", new(&WindowClosed));
            window.Position = new(200, 200);
            window.IsTransparent = true;
            window.IsBorderless = true;
            window.AlwaysOnTop = true;
            window.SetClearColor(new(0, 0, 0, 0));

            Settings settings = new(world);
            camera = new(world, window, CameraFieldOfView.FromDegrees(60));
            Transform cameraTransform = camera.AsEntity().Become<Transform>();
            cameraTransform.LocalPosition = new(0, 0, -1);

            Texture squareTexture = new(world, Address.Get<SquareTexture>());
            Model cubeModel = new(world, Address.Get<CubeModel>());
            Mesh cubeMesh = new(world, cubeModel);

            Mesh quadMesh = new(world);
            USpan<Vector3> positions = quadMesh.CreatePositions(4);
            positions[0] = new(-0.5f, -0.5f, 0);
            positions[1] = new(0.5f, -0.5f, 0);
            positions[2] = new(0.5f, 0.5f, 0);
            positions[3] = new(-0.5f, 0.5f, 0);
            USpan<Vector2> uvs = quadMesh.CreateUVs(4);
            uvs[0] = new(0, 0);
            uvs[1] = new(1, 0);
            uvs[2] = new(1, 1);
            uvs[3] = new(0, 1);
            USpan<Vector3> normals = quadMesh.CreateNormals(4);
            normals[0] = new(0, 0, 1);
            normals[1] = new(0, 0, 1);
            normals[2] = new(0, 0, 1);
            normals[3] = new(0, 0, 1);
            quadMesh.AddTriangle(0, 1, 2);
            quadMesh.AddTriangle(2, 3, 0);
            quadMesh.AddTriangle(2, 1, 0);
            quadMesh.AddTriangle(0, 3, 2);

            Material unlitMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            unlitMaterial.AddPushBinding<Color>();
            unlitMaterial.AddPushBinding<LocalToWorld>();
            unlitMaterial.AddComponentBinding<CameraMatrices>(0, 0, camera);
            unlitMaterial.AddTextureBinding(1, 0, squareTexture);

            AtlasTexture playerAtlas = GetPlayerAtlas(simulator, world);

            Material playerMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            playerMaterial.AddPushBinding<Color>();
            playerMaterial.AddPushBinding<LocalToWorld>();
            playerMaterial.AddComponentBinding<CameraMatrices>(0, 0, camera);
            playerMaterial.AddTextureBinding(1, 0, playerAtlas, TextureFiltering.Nearest);

            //create player
            Body playerBody = new(world, new CubeShape(0.5f), IsBody.Type.Dynamic);
            MeshRenderer playerRenderer = playerBody.AsEntity().Become<MeshRenderer>();
            playerRenderer.Mesh = quadMesh;
            playerRenderer.Material = playerMaterial;
            playerRenderer.RenderMask = new LayerMask().Set(1);

            playerBody.AddComponent(Color.White);
            playerBody.AddComponent(new GroundedState());
            playerBody.AddComponent(new AnimatedSprite());
            playerBody.AddComponent(new IsPlayer(true));
            playerBody.AddComponent(new Jetpack(3f));

            StateMachine playerAnimationStateMachine = new(world);
            playerAnimationStateMachine.AddState("Idle");
            playerAnimationStateMachine.AddState("Moving");
            playerAnimationStateMachine.AddState("JumpingUp");
            playerAnimationStateMachine.AddState("Falling");
            playerAnimationStateMachine.AddTransition("Idle", "Moving", "velocityX", Transition.Condition.GreaterThan, 0f);
            playerAnimationStateMachine.AddTransition("Moving", "Idle", "velocityX", Transition.Condition.LessThanOrEqual, 0f);
            playerAnimationStateMachine.AddTransition("Idle", "JumpingUp", "isGrounded", Transition.Condition.LessThan, 1f);
            playerAnimationStateMachine.AddTransition("Moving", "JumpingUp", "isGrounded", Transition.Condition.LessThan, 1f);
            playerAnimationStateMachine.AddTransition("JumpingUp", "Falling", "velocityY", Transition.Condition.LessThanOrEqual, 0f);
            playerAnimationStateMachine.AddTransition("Falling", "Idle", "isGrounded", Transition.Condition.GreaterThan, 0f);
            playerAnimationStateMachine.AddTransition("JumpingUp", "Idle", "isGrounded", Transition.Condition.GreaterThan, 0f);
            playerAnimationStateMachine.EntryState = "Idle";

            Automation<FixedString> idleAnimation = new(world, [new(0f, "Idle.png"), new(0.6f, "Idle2.png"), new(1f, "Idle.png")], true);
            Automation<FixedString> movingAnimation = new(world, [new(0f, "Walk.png"), new(0.15f, "Walk2.png"), new(0.3f, "Walk.png")], true);
            Automation<FixedString> jumpingUpAnimation = new(world, [new(0f, "JumpingUp.png")]);
            Automation<FixedString> fallingAnimation = new(world, [new(0f, "Falling.png")]);

            StatefulAutomationPlayer playerAnimator = playerBody.AsEntity().Become<StatefulAutomationPlayer>();
            playerAnimator.StateMachine = playerAnimationStateMachine;
            playerAnimator.AddParameter("velocityX", 0f);
            playerAnimator.AddParameter("velocityY", 0f);
            playerAnimator.AddParameter("isGrounded", 0f);
            playerAnimator.AddOrSetLink<AnimatedSprite>("Idle", idleAnimation);
            playerAnimator.AddOrSetLink<AnimatedSprite>("Moving", movingAnimation);
            playerAnimator.AddOrSetLink<AnimatedSprite>("JumpingUp", jumpingUpAnimation);
            playerAnimator.AddOrSetLink<AnimatedSprite>("Falling", fallingAnimation);

            //create wall colliders
            floorBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), IsBody.Type.Static);
            leftWallBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), IsBody.Type.Static);
            rightWallBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), IsBody.Type.Static);

            //Renderer floorRenderer = floorBody.AsEntity().Become<Renderer>();
            //floorRenderer.Mesh = cubeMesh;
            //floorRenderer.Material = unlitMaterial;
            //floorRenderer.Camera = camera;
            //floorBody.AsEntity().AddComponent(Color.Green);
            //Renderer leftWallRenderer = leftWallBody.AsEntity().Become<Renderer>();
            //leftWallRenderer.Mesh = cubeMesh;
            //leftWallRenderer.Material = unlitMaterial;
            //leftWallRenderer.Camera = camera;
            //leftWallBody.AsEntity().AddComponent(Color.Blue);
            //Renderer rightWallRenderer = rightWallBody.AsEntity().Become<Renderer>();
            //rightWallRenderer.Mesh = cubeMesh;
            //rightWallRenderer.Material = unlitMaterial;
            //rightWallRenderer.Camera = camera;
            //rightWallBody.AsEntity().AddComponent(Color.Blue);

            //create directional gravity
            new DirectionalGravity(world, -Vector3.UnitY, Gravity);
            new GlobalKeyboard(world);
            new GlobalMouse(world);

            [UnmanagedCallersOnly]
            static void WindowClosed(Window window)
            {
                window.Dispose();
            }
        }

        private readonly AtlasTexture GetPlayerAtlas(Simulator simulator, World world)
        {
            Texture idle = new(world, "Assets/Textures/Spaceman/Idle.png");
            Texture idle2 = new(world, "Assets/Textures/Spaceman/Idle2.png");
            Texture falling = new(world, "Assets/Textures/Spaceman/Falling.png");
            Texture jumpingUp = new(world, "Assets/Textures/Spaceman/JumpingUp.png");
            Texture skid = new(world, "Assets/Textures/Spaceman/Skid.png");
            Texture walk = new(world, "Assets/Textures/Spaceman/Walk.png");
            Texture walk2 = new(world, "Assets/Textures/Spaceman/Walk2.png");

            simulator.UpdateSystems(TimeSpan.MinValue, world);

            USpan<AtlasTexture.InputSprite> sprites = stackalloc AtlasTexture.InputSprite[]
            {
                new("Idle.png", idle),
                new("Idle2.png", idle2),
                new("Falling.png", falling),
                new("JumpingUp.png", jumpingUp),
                new("Skid.png", skid),
                new("Walk.png", walk),
                new("Walk2.png", walk2),
            };

            AtlasTexture atlasTexture = new(world, sprites);
            return atlasTexture;
        }

        private readonly void CloseWindow(World world)
        {
            if (world.TryGetFirst(out Keyboard keyboard))
            {
                if (keyboard.WasPressed(Keyboard.Button.Escape))
                {
                    window.Dispose();
                }
            }
        }

        private readonly void TeleportPlayerToMousePosition(World world)
        {
            if (world.TryGetFirst(out GlobalMouse mouse))
            {
                if (mouse.WasPressed(Mouse.Button.LeftButton))
                {
                    (uint width, uint height, uint refreshRate) = window.Display;
                    Vector2 mousePosition = mouse.Position;
                    mousePosition.X -= width * 0.5f;
                    mousePosition.Y += height * 0.5f;
                    Entity player = GetMainPlayer(world);
                    if (player != default)
                    {
                        Transform playerTransform = player.As<Transform>();
                        Vector3 worldPosition = new Vector3(mousePosition.X, height - mousePosition.Y, 0) * DisplayScale;
                        playerTransform.LocalPosition = worldPosition;

                        Body playerBody = player.As<Body>();
                        playerBody.LinearVelocity = Vector3.Zero;
                    }
                }
            }
        }

        private static (Vector2 direction, bool jump) ReadInput(World world)
        {
            if (world.TryGetFirst(out GlobalKeyboard keyboard))
            {
                bool left = keyboard.IsPressed(Keyboard.Button.A);
                bool right = keyboard.IsPressed(Keyboard.Button.D);
                bool up = keyboard.IsPressed(Keyboard.Button.W);
                bool down = keyboard.IsPressed(Keyboard.Button.S);
                bool jump = keyboard.IsPressed(Keyboard.Button.Space);
                Vector2 direction = default;
                if (left)
                {
                    direction.X -= 1;
                }

                if (right)
                {
                    direction.X += 1;
                }

                if (up)
                {
                    direction.Y += 1;
                }

                if (down)
                {
                    direction.Y -= 1;
                }

                return (direction, jump);
            }
            else
            {
                return default;
            }
        }

        private static Entity GetMainPlayer(World world)
        {
            foreach (uint playerEntity in world.GetAllContaining<IsPlayer>())
            {
                IsPlayer component = world.GetComponent<IsPlayer>(playerEntity);
                if (component.main)
                {
                    return new(world, playerEntity);
                }
            }

            return default;
        }

        private readonly void MovePlayer(World world, Vector2 direction, bool jump, float delta)
        {
            Entity player = GetMainPlayer(world);
            if (player == default)
            {
                return;
            }

            Body playerBody = player.As<Body>();
            Vector3 playerVelocity = playerBody.LinearVelocity;
            float acceleration = 1f;
            float moveSpeed = 7f;
            bool changeVelocity = false;
            bool isGrounded = player.GetComponent<GroundedState>().value;
            if (isGrounded)
            {
                if (jump)
                {
                    float jumpHeight = 3f;
                    playerVelocity.Y = MathF.Sqrt(2f * jumpHeight * Gravity);

                    ref Jetpack jetpack = ref player.GetComponent<Jetpack>();
                    jetpack.availableTime = jetpack.capacityTime;
                    jetpack.cooldownTime = 0.8f;
                }

                acceleration = 6f;
                changeVelocity = true;
            }
            else
            {
                if (direction.X != 0)
                {
                    changeVelocity = true;
                }

                if (jump)
                {
                    ref Jetpack jetpack = ref player.GetComponent<Jetpack>();
                    jetpack.cooldownTime -= delta;
                    if (jetpack.availableTime > 0 && jetpack.cooldownTime < 0)
                    {
                        playerVelocity.Y += delta * 30f;
                        jetpack.availableTime -= delta;
                    }
                }
            }

            if (changeVelocity)
            {
                playerVelocity.X = Lerp(playerVelocity.X, direction.X * moveSpeed, acceleration * delta);
            }

            playerBody.LinearVelocity = playerVelocity;
            playerBody.AngularVelocity = Vector3.Zero;

            Transform playerTransform = playerBody;
            playerTransform.LocalPosition.Z = 0f;
        }

        private unsafe readonly void CheckIfPlayersAreGrounded(Simulator simulator, World world)
        {
            foreach (uint playerEntity in world.GetAllContaining<IsPlayer>())
            {
                Entity player = new(world, playerEntity);
                Transform playerTransform = player.As<Transform>();
                player.SetComponent(new GroundedState(false));
                simulator.TryHandleMessage(new RaycastRequest(world, playerTransform.WorldPosition, -Vector3.UnitY, new(&GroundHitCallback), 0.5f, player.GetEntityValue()));
            }

            [UnmanagedCallersOnly]
            static void GroundHitCallback(World world, RaycastRequest raycast, RaycastHit* hits, uint hitCount)
            {
                uint playerEntity = (uint)raycast.userData;
                for (uint i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.entity != playerEntity)
                    {
                        world.SetComponent(playerEntity, new GroundedState(true));
                        break;
                    }
                }
            }
        }

        private readonly void MakeCameraFollowPlayer(World world)
        {
            Entity player = GetMainPlayer(world);
            if (player == default)
            {
                return;
            }

            Transform playerTransform = player.As<Transform>();
            Transform cameraTransform = camera.AsEntity().Become<Transform>();
            Vector3 playerPosition = playerTransform.WorldPosition;
            Vector3 cameraPosition = cameraTransform.WorldPosition;
            cameraTransform.LocalPosition = new(playerPosition.X, playerPosition.Y, cameraPosition.Z);
        }

        private readonly void MakeWindowFollowCamera(World world)
        {
            Transform cameraTransform = camera.AsEntity().Become<Transform>();
            Vector3 cameraPosition = cameraTransform.WorldPosition;
            (uint width, uint height, uint refreshRate) display = window.Display;
            float maxWidth = display.width * DisplayScale;
            float maxHeight = display.height * DisplayScale;
            Vector2 windowSize = window.Size;
            Vector2 windowPosition = default;
            float taskBarSize = 64f;
            windowPosition.X = Lerp(windowSize.X * 0.5f, display.width - windowSize.X, (cameraPosition.X / maxWidth) + 0.5f) - windowSize.X * 0.5f;
            windowPosition.Y = Lerp((windowSize.Y * 0.5f) - taskBarSize, display.height - windowSize.Y, (cameraPosition.Y / maxHeight) + 0.5f) + windowSize.Y * 0.5f;
            window.Position = new(windowPosition.X, display.height - windowPosition.Y);
        }

        private readonly void UpdateCollidersToMatchDisplay(World world)
        {
            (uint width, uint height, uint refreshRate) display = window.Display;
            Transform floorTransform = floorBody;
            float width = display.width * DisplayScale;
            float height = display.height * DisplayScale;

            floorTransform.LocalPosition = new(0f, height * -0.5f, 0f);
            floorTransform.LocalScale = new(width, 2f, 2f);

            Transform leftWallTransform = leftWallBody;
            leftWallTransform.LocalPosition = new(width * -0.5f, 0f, 0f);
            leftWallTransform.LocalScale = new(2f, height + 1f, 2f);

            Transform rightWallTransform = rightWallBody;
            rightWallTransform.LocalPosition = new(width * 0.5f, 0f, 0f);
            rightWallTransform.LocalScale = new(2f, height + 1f, 2f);
        }

        private readonly void AnimatePlayerParameters(World world)
        {
            foreach (uint playerEntity in world.GetAllContaining<IsPlayer>())
            {
                Entity player = new(world, playerEntity);
                Body playerBody = player.As<Body>();
                bool isGrounded = player.GetComponent<GroundedState>().value;
                StatefulAutomationPlayer playerAnimator = player.As<StatefulAutomationPlayer>();
                Vector3 playerVelocity = playerBody.LinearVelocity;
                float threshold = 0.4f;
                float velocityX = MathF.Abs(playerVelocity.X);
                if (velocityX < threshold)
                {
                    velocityX = 0;
                }
                else
                {
                    Quaternion rotation = Quaternion.Identity;
                    if (playerVelocity.X < 0)
                    {
                        rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 1f);
                    }

                    Transform playerTransform = player.As<Transform>();
                    playerTransform.LocalRotation = rotation;
                }

                playerAnimator.SetParameter("velocityX", velocityX);
                playerAnimator.SetParameter("velocityY", playerVelocity.Y);
                playerAnimator.SetParameter("isGrounded", isGrounded ? 1f : 0f);
            }
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private readonly void UpdateRegionToMatchAnimatedSprite(World world)
        {
            ComponentQuery<AnimatedSprite, IsRenderer> query = new(world);
            foreach (var r in query)
            {
                ref AnimatedSprite animatedSprite = ref r.component1;
                uint entity = r.entity;
                MeshRenderer renderer = new(world, entity);
                Material material = renderer.Material;
                ref MaterialTextureBinding binding = ref material.GetTextureBindingRef(1, 0);
                AtlasTexture atlasTexture = new(world, binding.TextureEntity);
                if (atlasTexture.TryGetSprite(animatedSprite.spriteName, out AtlasSprite sprite))
                {
                    binding.SetRegion(sprite.region);
                }
            }
        }

        [Component]
        public struct GroundedState
        {
            public bool value;

            public GroundedState(bool value)
            {
                this.value = value;
            }
        }

        [Component]
        public struct AnimatedSprite
        {
            public FixedString spriteName;

            public AnimatedSprite(FixedString spriteName)
            {
                this.spriteName = spriteName;
            }
        }

        [Component]
        public struct IsPlayer
        {
            public bool main;

            public IsPlayer(bool main)
            {
                this.main = main;
            }
        }

        [Component]
        public struct Jetpack
        {
            public float availableTime;
            public float capacityTime;
            public float cooldownTime;

            public Jetpack(float availableTime)
            {
                this.availableTime = availableTime;
                this.capacityTime = availableTime;
            }
        }
    }
}
