using Cameras;
using Cameras.Components;
using Collections;
using Data;
using DefaultPresentationAssets;
using InputDevices;
using InteractionKit;
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
        private readonly Window window;
        private readonly Camera camera;
        private readonly Material chunkMaterial;
        private readonly Mesh quadMesh;
        private readonly AtlasTexture chunkAtlas;
        private readonly World world;
        private readonly TerrainGenerator terrainGenerator;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        private VoxelGameProgram(Simulator simulator, World world)
        {
            this.world = world;
            terrainGenerator = new("goomba");
            window = CreateWindow(world);

            camera = new(world, window, CameraFieldOfView.FromDegrees(90f));
            Transform cameraTransform = camera.AsEntity().Become<Transform>();
            cameraTransform.LocalPosition = new(0f, 1f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

            chunkAtlas = GetChunkAtlas(simulator, world);

            chunkMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            chunkMaterial.AddPushBinding<Color>();
            chunkMaterial.AddPushBinding<LocalToWorld>();
            chunkMaterial.AddComponentBinding<CameraMatrices>(0, 0, camera);
            chunkMaterial.AddTextureBinding(1, 0, chunkAtlas, TextureFiltering.Nearest);

            Model quadModel = new(world, Address.Get<QuadModel>());
            quadMesh = new(world, quadModel);

            MeshRenderer quadRenderer = new(world, quadMesh, chunkMaterial);
            quadRenderer.AddComponent(Color.White);
            Transform ballTransform = quadRenderer.AsEntity().Become<Transform>();
            ballTransform.LocalPosition = new(0f, 4f, 0f);

            int chunkRadius = 3;
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
                chunk.UpdateMeshToMatchBlocks(chunkAtlas, terrainGenerator.meshRng);
            }

            Settings settings = new(world);
            Camera uiCamera = Camera.CreateOrthographic(world, window, 1f);
            Canvas canvas = new(world, settings, uiCamera);

            Label fpsLabel = new(canvas, "{{fps}}");
            fpsLabel.Anchor = Anchor.TopLeft;
            fpsLabel.Pivot = new(0f, 1f, 0f);

            using List<char> stringBuilder = new();
            stringBuilder.AddRange("F3 = Invert mouse".AsUSpan());
            stringBuilder.Add('\n');
            stringBuilder.AddRange("WASD = Move".AsUSpan());
            Label controlsLabel = new(canvas, stringBuilder.AsSpan());
            controlsLabel.Anchor = Anchor.TopLeft;
            controlsLabel.Pivot = new(0f, 1f, 0f);
            controlsLabel.Position = new(0f, -20f);

            SharedFunctions.Initialize(world);
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
            if (!window.IsDestroyed())
            {
                window.Dispose();
            }

            terrainGenerator.Dispose();
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

            Transform cameraTransform = camera.AsEntity().As<Transform>();
            SharedFunctions.TrackFramesPerSecond();
            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            return StatusCode.Continue;
        }

        private static unsafe Window CreateWindow(World world)
        {
            return new(world, "Voxel Game", new Vector2(400, 200), new(900, 720), "vulkan", new(&WindowClosed));

            [UnmanagedCallersOnly]
            static void WindowClosed(Window window)
            {
                window.Dispose();
            }
        }

        private readonly AtlasTexture GetChunkAtlas(Simulator simulator, World world)
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

            AtlasTexture atlasTexture = new(world, sprites);
            return atlasTexture;
        }

        private static bool AnyWindowOpen(World world)
        {
            return world.CountEntities<Window>() > 0;
        }
    }
}