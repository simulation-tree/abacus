using Automations;
using Cameras;
using Cameras.Components;
using Data;
using DefaultPresentationAssets;
using InputDevices;
using Materials;
using Materials.Components;
using Meshes;
using Physics;
using Physics.Events;
using Physics.Functions;
using Rendering;
using Shapes.Types;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Transforms;
using Transforms.Components;
using UI;
using Unmanaged;
using Windows;
using Worlds;

namespace Abacus
{
    public class DesktopPlatformer : Program
    {
        private const float Gravity = 14f;
        private const float DisplayScale = 0.01f;

        private readonly Window window;
        private readonly Camera camera;
        private readonly Body floorBody;
        private readonly Body leftWallBody;
        private readonly Body rightWallBody;
        private readonly StatefulAutomationPlayer playerMaterialAnimator;

        public unsafe DesktopPlatformer(Simulator simulator) : base(simulator)
        {
            window = new(world, "Fly", default, new(200, 200), "vulkan", new(&WindowClosed));
            window.Position = new(200, 200);
            window.IsTransparent = true;
            window.IsBorderless = true;
            window.AlwaysOnTop = true;
            window.ClearColor = new(0, 0, 0, 0);

            Settings settings = new(world);
            camera = new(world, window, CameraSettings.CreatePerspectiveDegrees(60), new LayerMask(1));
            Transform cameraTransform = camera.Become<Transform>();
            cameraTransform.LocalPosition = new(0, 0, -1);

            Texture squareTexture = new(world, EmbeddedResource.GetAddress<SquareTexture>());

            //double sided quad
            Mesh quadMesh = Mesh.CreateCenteredQuad(world);
            Span<uint> indices = quadMesh.SetIndexCount(quadMesh.IndexCount * 2);
            indices[6] = 2;
            indices[7] = 1;
            indices[8] = 0;
            indices[9] = 0;
            indices[10] = 3;
            indices[11] = 2;

            Material unlitMaterial = new(world, EmbeddedResource.GetAddress<UnlitTexturedMaterial>());
            unlitMaterial.AddInstanceBinding<Color>();
            unlitMaterial.AddInstanceBinding<LocalToWorld>();
            unlitMaterial.AddComponentBinding<CameraMatrices>(new(0, 0), camera);
            unlitMaterial.AddTextureBinding(new(1, 0), squareTexture);

            AtlasTexture playerAtlas = GetPlayerAtlas(simulator, world);

            Material playerMaterial = new(world, EmbeddedResource.GetAddress<UnlitTexturedMaterial>());
            playerMaterial.AddInstanceBinding<Color>();
            playerMaterial.AddInstanceBinding<LocalToWorld>();
            playerMaterial.AddComponentBinding<CameraMatrices>(new(0, 0), camera);
            playerMaterial.AddTextureBinding(new(1, 0), playerAtlas, TextureFiltering.Nearest);

            //create player
            Body playerBody = new(world, new CubeShape(0.5f), BodyType.Dynamic);
            MeshRenderer playerRenderer = playerBody.Become<MeshRenderer>();
            playerRenderer.Mesh = quadMesh;
            playerRenderer.Material = playerMaterial;
            playerRenderer.RenderMask = new(1);

            playerBody.AddComponent(Color.White);
            playerBody.AddComponent(new GroundedState());
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

            AtlasSprite idleSprite = playerAtlas["Idle.png"];
            AtlasSprite idle2Sprite = playerAtlas["Idle2.png"];
            AtlasSprite fallingSprite = playerAtlas["Falling.png"];
            AtlasSprite jumpingUpSprite = playerAtlas["JumpingUp.png"];
            AtlasSprite skidSprite = playerAtlas["Skid.png"];
            AtlasSprite walkSprite = playerAtlas["Walk.png"];
            AtlasSprite walk2Sprite = playerAtlas["Walk2.png"];
            AutomationEntity<Vector4> idleAnimation = new(world, [new(0f, idleSprite.region), new(0.6f, idle2Sprite.region), new(1f, idleSprite.region)], true);
            AutomationEntity<Vector4> movingAnimation = new(world, [new(0f, walkSprite.region), new(0.15f, walk2Sprite.region), new(0.3f, walkSprite.region)], true);
            AutomationEntity<Vector4> jumpingUpAnimation = new(world, [new(0f, jumpingUpSprite.region)]);
            AutomationEntity<Vector4> fallingAnimation = new(world, [new(0f, fallingSprite.region)]);

            playerMaterialAnimator = playerMaterial.Become<StatefulAutomationPlayer>();
            playerMaterialAnimator.StateMachine = playerAnimationStateMachine;
            playerMaterialAnimator.AddParameter("velocityX", 0f);
            playerMaterialAnimator.AddParameter("velocityY", 0f);
            playerMaterialAnimator.AddParameter("isGrounded", 0f);

            ASCIIText256 fieldName = "region";
            playerMaterialAnimator.AddOrSetLinkToArrayElement<TextureBinding>("Idle", idleAnimation, 0, fieldName);
            playerMaterialAnimator.AddOrSetLinkToArrayElement<TextureBinding>("Moving", movingAnimation, 0, fieldName);
            playerMaterialAnimator.AddOrSetLinkToArrayElement<TextureBinding>("JumpingUp", jumpingUpAnimation, 0, fieldName);
            playerMaterialAnimator.AddOrSetLinkToArrayElement<TextureBinding>("Falling", fallingAnimation, 0, fieldName);

            //create wall colliders
            floorBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), BodyType.Static);
            leftWallBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), BodyType.Static);
            rightWallBody = new(world, new CubeShape(0.5f, 0.5f, 0.5f), BodyType.Static);

            //Renderer floorRenderer = floorBody.AsEntity().Become<Renderer>();
            //floorRenderer.Mesh = cubeMesh;
            //floorRenderer.Material = unlitMaterial;
            //floorRenderer.Camera = camera;
            //floorBody.AddComponent(Color.Green);
            //Renderer leftWallRenderer = leftWallBody.Become<Renderer>();
            //leftWallRenderer.Mesh = cubeMesh;
            //leftWallRenderer.Material = unlitMaterial;
            //leftWallRenderer.Camera = camera;
            //leftWallBody.AddComponent(Color.Blue);
            //Renderer rightWallRenderer = rightWallBody.AsEntity().Become<Renderer>();
            //rightWallRenderer.Mesh = cubeMesh;
            //rightWallRenderer.Material = unlitMaterial;
            //rightWallRenderer.Camera = camera;
            //rightWallBody.AddComponent(Color.Blue);

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

        public override void Dispose()
        {
            if (!window.IsDestroyed)
            {
                window.Dispose();
            }
        }

        public override bool Update(Simulator simulator, double deltaTime)
        {
            if (window.IsDestroyed)
            {
                return false;
            }

            UpdateCollidersToMatchDisplay();
            (Vector2 direction, bool jump) = ReadInput(world);
            MovePlayer(world, direction, jump, deltaTime);
            AnimatePlayerParameters(world);
            CheckIfPlayersAreGrounded(simulator, world);
            MakeCameraFollowPlayer(world);
            MakeWindowFollowCamera();
            TeleportPlayerToMousePosition(world);
            CloseWindow(world);
            return true;
        }

        private AtlasTexture GetPlayerAtlas(Simulator simulator, World world)
        {
            Texture idle = new(world, "Assets/Textures/Spaceman/Idle.png");
            Texture idle2 = new(world, "Assets/Textures/Spaceman/Idle2.png");
            Texture falling = new(world, "Assets/Textures/Spaceman/Falling.png");
            Texture jumpingUp = new(world, "Assets/Textures/Spaceman/JumpingUp.png");
            Texture skid = new(world, "Assets/Textures/Spaceman/Skid.png");
            Texture walk = new(world, "Assets/Textures/Spaceman/Walk.png");
            Texture walk2 = new(world, "Assets/Textures/Spaceman/Walk2.png");

            simulator.Update();

            Span<AtlasTexture.InputSprite> sprites = stackalloc AtlasTexture.InputSprite[]
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

        private void CloseWindow(World world)
        {
            if (world.TryGetFirst(out Keyboard keyboard))
            {
                if (keyboard.WasPressed(Keyboard.Button.Escape))
                {
                    window.Dispose();
                }
            }
        }

        private void TeleportPlayerToMousePosition(World world)
        {
            if (world.TryGetFirst(out GlobalMouse mouse))
            {
                if (mouse.WasPressed(Mouse.Button.LeftButton))
                {
                    Display display = window.Display;
                    uint width = display.Width;
                    uint height = display.Height;
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

        private static void MovePlayer(World world, Vector2 direction, bool jump, double deltaTime)
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
                    jetpack.cooldownTime -= (float)deltaTime;
                    if (jetpack.availableTime > 0 && jetpack.cooldownTime < 0)
                    {
                        playerVelocity.Y += (float)deltaTime * 30f;
                        jetpack.availableTime -= (float)deltaTime;
                    }
                }
            }

            if (changeVelocity)
            {
                playerVelocity.X = float.Lerp(playerVelocity.X, direction.X * moveSpeed, acceleration * (float)deltaTime);
            }

            playerBody.LinearVelocity = playerVelocity;
            playerBody.AngularVelocity = Vector3.Zero;

            Transform playerTransform = playerBody;
            playerTransform.LocalPosition.Z = 0f;
        }

        private unsafe static void CheckIfPlayersAreGrounded(Simulator simulator, World world)
        {
            foreach (uint playerEntity in world.GetAllContaining<IsPlayer>())
            {
                Entity player = new(world, playerEntity);
                Transform playerTransform = player.As<Transform>();
                player.SetComponent(new GroundedState(false));
                float raycastDistance = 3f;
                simulator.Broadcast(new RaycastRequest(world, playerTransform.WorldPosition, -Vector3.UnitY, new(&GroundHitCallback), raycastDistance, player.value));
            }

            [UnmanagedCallersOnly]
            static void GroundHitCallback(RaycastHitCallback.Input input)
            {
                uint playerEntity = (uint)input.request.userData;
                ReadOnlySpan<RaycastHit> hits = input.Hits;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.entity != playerEntity)
                    {
                        input.world.SetComponent(playerEntity, new GroundedState(true));
                        break;
                    }
                }
            }
        }

        private void MakeCameraFollowPlayer(World world)
        {
            Entity player = GetMainPlayer(world);
            if (player == default)
            {
                return;
            }

            Transform playerTransform = player.As<Transform>();
            Transform cameraTransform = camera.Become<Transform>();
            Vector3 playerPosition = playerTransform.WorldPosition;
            Vector3 cameraPosition = cameraTransform.WorldPosition;
            cameraTransform.LocalPosition = new(playerPosition.X, playerPosition.Y, cameraPosition.Z);
        }

        private void MakeWindowFollowCamera()
        {
            Transform cameraTransform = camera.Become<Transform>();
            Vector3 cameraPosition = cameraTransform.WorldPosition;
            Display display = window.Display;
            uint width = display.Width;
            uint height = display.Height;
            float maxWidth = width * DisplayScale;
            float maxHeight = height * DisplayScale;
            Vector2 windowSize = window.Size;
            Vector2 windowPosition = default;
            float taskBarSize = 64f;
            windowPosition.X = float.Lerp(windowSize.X * 0.5f, width - windowSize.X, (cameraPosition.X / maxWidth) + 0.5f) - windowSize.X * 0.5f;
            windowPosition.Y = float.Lerp((windowSize.Y * 0.5f) - taskBarSize, height - windowSize.Y, (cameraPosition.Y / maxHeight) + 0.5f) + windowSize.Y * 0.5f;
            window.Position = new(windowPosition.X, height - windowPosition.Y);
        }

        private void UpdateCollidersToMatchDisplay()
        {
            Display display = window.Display;
            Transform floorTransform = floorBody;
            float width = display.Width * DisplayScale;
            float height = display.Height * DisplayScale;

            floorTransform.LocalPosition = new(0f, height * -0.5f, 0f);
            floorTransform.LocalScale = new(width, 2f, 2f);

            Transform leftWallTransform = leftWallBody;
            leftWallTransform.LocalPosition = new(width * -0.5f, 0f, 0f);
            leftWallTransform.LocalScale = new(2f, height + 1f, 2f);

            Transform rightWallTransform = rightWallBody;
            rightWallTransform.LocalPosition = new(width * 0.5f, 0f, 0f);
            rightWallTransform.LocalScale = new(2f, height + 1f, 2f);
        }

        private void AnimatePlayerParameters(World world)
        {
            foreach (uint playerEntity in world.GetAllContaining<IsPlayer>())
            {
                Entity player = new(world, playerEntity);
                Body playerBody = player.As<Body>();
                bool isGrounded = player.GetComponent<GroundedState>().value;
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

                playerMaterialAnimator.SetParameter("velocityX", velocityX);
                playerMaterialAnimator.SetParameter("velocityY", playerVelocity.Y);
                playerMaterialAnimator.SetParameter("isGrounded", isGrounded ? 1f : 0f);
            }
        }

        public struct GroundedState
        {
            public bool value;

            public GroundedState(bool value)
            {
                this.value = value;
            }
        }

        public struct IsPlayer
        {
            public bool main;

            public IsPlayer(bool main)
            {
                this.main = main;
            }
        }

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