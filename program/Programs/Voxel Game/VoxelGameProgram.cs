using Abacus;
using Cameras;
using Cameras.Components;
using Collections.Generic;
using Data;
using Data.Messages;
using DefaultPresentationAssets;
using InputDevices;
using Materials;
using Meshes;
using Models;
using Rendering;
using Skyboxes;
using System;
using System.Numerics;
using Textures;
using Textures.Components;
using Transforms;
using Transforms.Components;
using UI;
using Windows;
using Worlds;

namespace VoxelGame
{
    public class VoxelGameProgram : Program
    {
        public static readonly LayerMask worldMask = new(0);
        public static readonly LayerMask uiMask = new(1);

        private readonly Window window;
        private readonly Camera worldCamera;
        private readonly Material chunkMaterial;
        private readonly Mesh quadMesh;
        private readonly AtlasTexture chunkAtlas;
        private readonly TerrainGenerator terrainGenerator;
        private readonly Dictionary<BlockTextureKey, BlockTexture> blockTextures;
        private readonly Settings settings;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        public VoxelGameProgram(Application application) : base(application)
        {
            terrainGenerator = new("goomba");
            Vector2 windowSize = new(900, 720);
            Vector2 windowAnchor = new(0.5f, 0.5f);
            Vector2 windowPosition = windowSize * -0.5f;
            window = new(world, "Voxel Game", windowPosition, windowSize, windowAnchor, "vulkan");

            Camera uiCamera = Camera.CreateOrthographic(world, window, 1f);
            uiCamera.Order = 1;
            uiCamera.RenderMask = uiMask;

            worldCamera = Camera.CreatePerspectiveDegrees(world, window, 90f);
            worldCamera.RenderMask = worldMask;
            Transform cameraTransform = worldCamera.Become<Transform>();
            cameraTransform.LocalPosition = new(0f, 1f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

            (chunkAtlas, blockTextures) = GetChunkAtlas();

            chunkMaterial = new(world, EmbeddedResource.GetAddress<UnlitTexturedMaterial>());
            chunkMaterial.AddPushConstant<Color>();
            chunkMaterial.AddPushConstant<LocalToWorld>();
            chunkMaterial.AddComponentBinding<CameraMatrices>(new(0, 0), worldCamera);
            chunkMaterial.AddTextureBinding(new(1, 0), chunkAtlas, TextureFiltering.Nearest);

            Model quadModel = new(world, EmbeddedResource.GetAddress<QuadModel>());
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

            settings = new(world);
            Canvas canvas = new(settings, uiCamera, uiMask, LayerMask.All);

            Label fpsLabel = new(canvas, "{{fps}}", default, 32);
            fpsLabel.Anchor = Anchor.TopLeft;
            fpsLabel.Pivot = new(0f, 1f, 0f);

            ReadOnlySpan<char> infoLabel = "F3 = Invert mouse\nWASD, Space, Control = Move";
            Label controlsLabel = new(canvas, infoLabel, default, 32);
            controlsLabel.Anchor = Anchor.TopLeft;
            controlsLabel.Pivot = new(0f, 1f, 0f);
            controlsLabel.Position = new(0f, -100f);

            IsTextureRequest.Flags flags = IsTextureRequest.Flags.None;
            Texture skyboxDown = new(world, "Assets/Skyboxes/Clouds/clouds1_down.bmp", flags: flags);
            Texture skyboxEast = new(world, "Assets/Skyboxes/Clouds/clouds1_east.bmp", flags: flags);
            Texture skyboxNorth = new(world, "Assets/Skyboxes/Clouds/clouds1_north.bmp", flags: flags);
            Texture skyboxSouth = new(world, "Assets/Skyboxes/Clouds/clouds1_south.bmp", flags: flags);
            Texture skyboxUp = new(world, "Assets/Skyboxes/Clouds/clouds1_up.bmp", flags: flags);
            Texture skyboxWest = new(world, "Assets/Skyboxes/Clouds/clouds1_west.bmp", flags: flags);
            simulator.Broadcast(new DataUpdate());

            CubemapTexture cubemap = new(world, skyboxEast, skyboxWest, skyboxUp, skyboxDown, skyboxNorth, skyboxSouth, flags);
            CubemapSkybox skybox = new(world, worldCamera, cubemap, worldMask);
            simulator.Broadcast(new DataUpdate());

            SharedFunctions.AddLabelProcessors(world);
        }

        public override bool Update(double deltaTime)
        {
            if (!AnyWindowOpen())
            {
                return false;
            }

            if (world.TryGetFirst(out Keyboard keyboard))
            {
                if (keyboard.WasPressed(Keyboard.Button.Escape))
                {
                    return false;
                }
            }

            Transform cameraTransform = worldCamera.As<Transform>();
            SharedFunctions.TrackFramesPerSecond();
            SharedFunctions.MoveCameraAround(world, cameraTransform, deltaTime, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            return true;
        }

        public override void Dispose()
        {
            settings.Dispose();
            if (!window.IsDestroyed)
            {
                window.Dispose();
            }

            terrainGenerator.Dispose();
            blockTextures.Dispose();
        }

        private (AtlasTexture, Dictionary<BlockTextureKey, BlockTexture>) GetChunkAtlas()
        {
            Texture dirt = new(world, "Assets/Textures/Blocks/Dirt.png");
            Texture grass = new(world, "Assets/Textures/Blocks/Grass.png");
            Texture stone = new(world, "Assets/Textures/Blocks/Stone.png");
            Texture grassSide = new(world, "Assets/Textures/Blocks/GrassSide.png");
            Texture cobblestone = new(world, "Assets/Textures/Blocks/Cobblestone.png");

            simulator.Broadcast(new DataUpdate());

            Span<AtlasTexture.InputSprite> sprites = stackalloc AtlasTexture.InputSprite[]
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

            IsTextureRequest.Flags flags = IsTextureRequest.Flags.BleedPixels;
            AtlasTexture atlasTexture = new(world, sprites, 4, flags);
            return (atlasTexture, blockTextures);
        }

        private bool AnyWindowOpen()
        {
            return world.CountEntities<Window>() > 0;
        }
    }
}