using Cameras;
using Cameras.Components;
using Collections;
using Data;
using DefaultPresentationAssets;
using InputDevices;
using UI;
using Materials;
using Meshes;
using Models;
using Rendering;
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

namespace VoxelGame
{
    public partial struct VoxelGameProgram : IProgram
    {
        public static readonly LayerMask worldMask = new LayerMask().Set(0);
        public static readonly LayerMask uiMask = new LayerMask().Set(1);

        private readonly Window window;
        private readonly Camera camera;
        private readonly Material chunkMaterial;
        private readonly Mesh quadMesh;
        private readonly AtlasTexture chunkAtlas;
        private readonly World world;
        private readonly TerrainGenerator terrainGenerator;
        private readonly Dictionary<BlockTextureKey, BlockTexture> blockTextures;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        private unsafe VoxelGameProgram(Simulator simulator, World world)
        {
            this.world = world;
            terrainGenerator = new("goomba");
            window = new(world, "Voxel Game", new Vector2(400, 200), new(900, 720), "vulkan", new(&WindowClosed));

            [UnmanagedCallersOnly]
            static void WindowClosed(Window window)
            {
                window.Dispose();
            }

            camera = new(world, window, CameraSettings.CreatePerspectiveDegrees(90f));
            camera.RenderMask = worldMask;
            Transform cameraTransform = camera.Become<Transform>();
            cameraTransform.LocalPosition = new(0f, 1f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

            (chunkAtlas, blockTextures) = GetChunkAtlas(simulator, world);

            chunkMaterial = new(world, EmbeddedResourceRegistry.Get<UnlitTexturedMaterial>());
            chunkMaterial.AddPushBinding<Color>();
            chunkMaterial.AddPushBinding<LocalToWorld>();
            chunkMaterial.AddComponentBinding<CameraMatrices>(new(0, 0), camera);
            chunkMaterial.AddTextureBinding(new(1, 0), chunkAtlas, TextureFiltering.Nearest);

            Model quadModel = new(world, EmbeddedResourceRegistry.Get<QuadModel>());
            quadMesh = new(world, quadModel);

            MeshRenderer quadRenderer = new(world, quadMesh, chunkMaterial);
            quadRenderer.AddComponent(Color.White);
            Transform ballTransform = quadRenderer.Become<Transform>();
            ballTransform.LocalPosition = new(0f, 4f, 0f);

            world.CreateEntity(new VoxelSettings(16));

            int chunkRadius = 5;
            using List<Chunk> generatedChunks = new();
            for (int cx = -chunkRadius; cx < chunkRadius; cx++)
            {
                for (int cz = -chunkRadius; cz < chunkRadius; cz++)
                {
                    Chunk chunk = terrainGenerator.CreateChunk(world, cx, 0, cz, chunkMaterial);
                    generatedChunks.Add(chunk);
                }
            }

            foreach (Chunk chunk in generatedChunks)
            {
                chunk.UpdateMeshToMatchBlocks(chunkAtlas, blockTextures, terrainGenerator.meshRng);
            }

            Settings settings = new(world);
            Camera uiCamera = Camera.CreateOrthographic(world, window, 1f);
            uiCamera.RenderMask = uiMask;
            Canvas canvas = new(world, settings, uiCamera, uiMask, LayerMask.All);

            Label fpsLabel = new(canvas, "{{fps}}");
            fpsLabel.Anchor = Anchor.TopLeft;
            fpsLabel.Pivot = new(0f, 1f, 0f);

            USpan<char> infoLabel = "F3 = Invert mouse\nWASD, Space, Control = Move".AsSpan();
            Label controlsLabel = new(canvas, infoLabel);
            controlsLabel.Anchor = Anchor.TopLeft;
            controlsLabel.Pivot = new(0f, 1f, 0f);
            controlsLabel.Position = new(0f, -20f);

            SharedFunctions.AddLabelProcessors(world);
        }

        readonly void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new VoxelGameProgram(simulator, world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (!AnyWindowOpen(world))
            {
                return StatusCode.Success(0);
            }

            if (world.TryGetFirst(out Keyboard keyboard))
            {
                if (keyboard.WasPressed(Keyboard.Button.Escape))
                {
                    return StatusCode.Success(1);
                }
            }

            Transform cameraTransform = camera.As<Transform>();
            SharedFunctions.TrackFramesPerSecond();
            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            return StatusCode.Continue;
        }

        readonly void IProgram.Finish(in StatusCode statusCode)
        {
            if (!window.IsDestroyed)
            {
                window.Dispose();
            }

            terrainGenerator.Dispose();
            blockTextures.Dispose();
        }

        private readonly (AtlasTexture, Dictionary<BlockTextureKey, BlockTexture>) GetChunkAtlas(Simulator simulator, World world)
        {
            Texture dirt = new(world, "Assets/Textures/Blocks/Dirt.png");
            Texture grass = new(world, "Assets/Textures/Blocks/Grass.png");
            Texture stone = new(world, "Assets/Textures/Blocks/Stone.png");
            Texture grassSide = new(world, "Assets/Textures/Blocks/GrassSide.png");
            Texture cobblestone = new(world, "Assets/Textures/Blocks/Cobblestone.png");

            simulator.UpdateSystems(TimeSpan.MinValue, world);

            USpan<AtlasTexture.InputSprite> sprites = stackalloc AtlasTexture.InputSprite[]
            {
                new("Dirt", dirt),
                new("Grass", grass),
                new("Stone", stone),
                new("GrassSide", grassSide),
                new("Cobblestone", cobblestone),
            };

            Dictionary<BlockTextureKey, BlockTexture> blockTextures = new();
            blockTextures.Add(new(1, Direction.Right), new("Dirt"));
            blockTextures.Add(new(1, Direction.Left), new("Dirt"));
            blockTextures.Add(new(1, Direction.Up), new("Dirt"));
            blockTextures.Add(new(1, Direction.Down), new("Dirt"));
            blockTextures.Add(new(1, Direction.Forward), new("Dirt"));
            blockTextures.Add(new(1, Direction.Backward), new("Dirt"));

            blockTextures.Add(new(2, Direction.Right), new("GrassSide", BlockTexture.Rotation.Clockwise270));
            blockTextures.Add(new(2, Direction.Left), new("GrassSide", BlockTexture.Rotation.Clockwise270));
            blockTextures.Add(new(2, Direction.Up), new("Grass"));
            blockTextures.Add(new(2, Direction.Down), new("Dirt"));
            blockTextures.Add(new(2, Direction.Forward), new("GrassSide", BlockTexture.Rotation.Default));
            blockTextures.Add(new(2, Direction.Backward), new("GrassSide", BlockTexture.Rotation.Clockwise270));

            blockTextures.Add(new(3, Direction.Right), new("Cobblestone"));
            blockTextures.Add(new(3, Direction.Left), new("Cobblestone"));
            blockTextures.Add(new(3, Direction.Up), new("Cobblestone"));
            blockTextures.Add(new(3, Direction.Down), new("Cobblestone"));
            blockTextures.Add(new(3, Direction.Forward), new("Cobblestone"));
            blockTextures.Add(new(3, Direction.Backward), new("Cobblestone"));

            blockTextures.Add(new(4, Direction.Right), new("Stone"));
            blockTextures.Add(new(4, Direction.Left), new("Stone"));
            blockTextures.Add(new(4, Direction.Up), new("Stone"));
            blockTextures.Add(new(4, Direction.Down), new("Stone"));
            blockTextures.Add(new(4, Direction.Forward), new("Stone"));
            blockTextures.Add(new(4, Direction.Backward), new("Stone"));

            AtlasTexture atlasTexture = new(world, sprites);
            return (atlasTexture, blockTextures);
        }

        private static bool AnyWindowOpen(World world)
        {
            return world.CountEntities<Window>() > 0;
        }
    }
}