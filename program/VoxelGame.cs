using Cameras;
using Cameras.Components;
using Collections;
using Data;
using DefaultPresentationAssets;
using InputDevices;
using InteractionKit;
using Meshes;
using Meshes.Components;
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

namespace Abacus
{
    public partial struct VoxelGame : IProgram
    {
        private readonly Window window;
        private readonly Camera camera;
        private readonly Material chunkMaterial;
        private readonly Mesh quadMesh;
        private readonly AtlasTexture chunkAtlas;
        private readonly World world;
        private readonly VoxelTerrainGenerator terrainGenerator;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        private VoxelGame(Simulator simulator, World world)
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
            Canvas canvas = new(world, uiCamera);

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
            allocation.Write(new VoxelGame(simulator, world));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            if (!AnyWindowOpen(world))
            {
                return StatusCode.Success(1);
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

        public readonly struct Chunk : IEntity
        {
            private const uint VerticesPerFace = 4;
            private const uint TrianglesPerFace = 6;
            private const uint FacesPerBlock = 6;

            private readonly Mesh mesh;

            public unsafe readonly USpan<uint> Blocks
            {
                get
                {
                    USpan<BlockID> blocks = mesh.AsEntity().GetArray<BlockID>();
                    return blocks.As<uint>();
                }
            }

            public readonly uint Capacity => mesh.AsEntity().GetArrayLength<BlockID>();
            public readonly ref uint this[uint index] => ref Blocks[index];
            public readonly ref uint this[byte x, byte y, byte z] => ref this[VoxelMeshGeneration.GetIndex(x, y, z, ChunkSize)];
            public readonly byte ChunkSize => mesh.AsEntity().GetComponent<IsChunk>().chunkSize;

            readonly uint IEntity.Value => mesh.GetEntityValue();
            readonly World IEntity.World => mesh.GetWorld();

            readonly void IEntity.Describe(ref Archetype archetype)
            {
                archetype.AddComponentType<IsMesh>();
                archetype.AddComponentType<IsChunk>();
                archetype.AddArrayElementType<uint>();
                archetype.AddArrayElementType<BlockID>();
            }

            public Chunk(World world, int cx, int cy, int cz, byte chunkSize, Material unlitMaterial)
            {
                uint capacity = (uint)(chunkSize * chunkSize * chunkSize);
                mesh = new(world);
                mesh.AddComponent(new IsChunk(chunkSize));
                mesh.CreatePositions(0);
                mesh.CreateColors(0);
                mesh.CreateUVs(0);
                USpan<BlockID> blocks = mesh.AsEntity().CreateArray<BlockID>(capacity);
                blocks.Clear();

                MeshRenderer chunkRenderer = mesh.AsEntity().Become<MeshRenderer>();
                chunkRenderer.Mesh = mesh;
                chunkRenderer.Material = unlitMaterial;
                chunkRenderer.Mask = 1;

                chunkRenderer.AddComponent(Color.White);
                Transform chunkTransform = chunkRenderer.AsEntity().Become<Transform>();
                chunkTransform.LocalPosition = new Vector3(cx, cy, cz) * chunkSize;
            }

            public readonly void Dispose()
            {
                mesh.Dispose();
            }

            public readonly void UpdateMeshToMatchBlocks(AtlasTexture chunkAtlas, RandomGenerator meshRng)
            {
                World world = mesh.GetWorld();
                byte chunkSize = ChunkSize;
                Vector3 chunkPosition = mesh.AsEntity().As<Transform>().WorldPosition;
                int cx = (int)MathF.Floor(chunkPosition.X / chunkSize);
                int cy = (int)MathF.Floor(chunkPosition.Y / chunkSize);
                int cz = (int)MathF.Floor(chunkPosition.Z / chunkSize);
                USpan<uint> blocks = Blocks;
                USpan<uint> blocksLeft = GetBlocks(world, cx - 1, cy, cz);
                USpan<uint> blocksRight = GetBlocks(world, cx + 1, cy, cz);
                USpan<uint> blocksDown = GetBlocks(world, cx, cy - 1, cz);
                USpan<uint> blocksUp = GetBlocks(world, cx, cy + 1, cz);
                USpan<uint> blocksBackward = GetBlocks(world, cx, cy, cz - 1);
                USpan<uint> blocksForward = GetBlocks(world, cx, cy, cz + 1);
                uint capacity = Capacity;
                using Array<Vector3> vertices = new(capacity * VerticesPerFace * FacesPerBlock);
                using Array<Vector2> uvs = new(capacity * VerticesPerFace * FacesPerBlock);
                using Array<Vector4> colors = new(capacity * VerticesPerFace * FacesPerBlock);
                using Array<uint> triangles = new(capacity * TrianglesPerFace * FacesPerBlock);
                VoxelMeshGeneration generation = new(blocks, blocksLeft, blocksRight, blocksDown, blocksUp, blocksBackward, blocksForward, chunkSize, vertices, uvs, colors, triangles, meshRng, capacity, chunkAtlas);
                generation.Generate();
                USpan<Vector3> meshPositions = mesh.ResizePositions(generation.verticeIndex);
                USpan<Vector2> meshUVs = mesh.ResizeUVs(generation.verticeIndex);
                USpan<Vector4> meshColors = mesh.ResizeColors(generation.verticeIndex);
                USpan<uint> meshTriangles = mesh.ResizeIndices(generation.triangleIndex);
                vertices.AsSpan(0, generation.verticeIndex).CopyTo(meshPositions);
                uvs.AsSpan(0, generation.verticeIndex).CopyTo(meshUVs);
                colors.AsSpan(0, generation.verticeIndex).CopyTo(meshColors);
                triangles.AsSpan(0, generation.triangleIndex).CopyTo(meshTriangles);
                mesh.IncrementVersion();
            }

            public static USpan<uint> GetBlocks(World world, int cx, int cy, int cz)
            {
                ComponentQuery<IsChunk, Position> query = new(world);
                foreach (var r in query)
                {
                    ref IsChunk chunk = ref r.component1;
                    ref Position position = ref r.component2;
                    uint chunkSize = chunk.chunkSize;
                    Vector3 worldPosition = position.value;
                    int chunkX = (int)MathF.Floor(worldPosition.X / chunkSize);
                    int chunkY = (int)MathF.Floor(worldPosition.Y / chunkSize);
                    int chunkZ = (int)MathF.Floor(worldPosition.Z / chunkSize);
                    if (chunkX == cx && chunkY == cy && chunkZ == cz)
                    {
                        return world.GetArray<BlockID>(r.entity).As<uint>();
                    }
                }

                return default;
            }
        }

        public readonly struct VoxelTerrainGenerator : IDisposable
        {
            public readonly RandomGenerator meshRng;

            private readonly GCHandle noise;
            private readonly RandomGenerator terrainRng;

            private readonly FastNoiseLite Noise => (FastNoiseLite)(noise.Target ?? throw new());

            public VoxelTerrainGenerator(FixedString seed)
            {
                FastNoiseLite noise = new FastNoiseLite(seed.GetHashCode());
                noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
                this.noise = GCHandle.Alloc(noise);
                terrainRng = new(seed);
                meshRng = new();
            }

            public readonly void Dispose()
            {
                meshRng.Dispose();
                terrainRng.Dispose();
                noise.Free();
            }

            public readonly Chunk CreateChunk(World world, int cx, int cy, int cz, Material chunkMaterial)
            {
                FastNoiseLite noise = Noise;
                float frequency = 4f;
                float amplitude = 6f;
                byte chunkSize = 16;
                Chunk chunk = new(world, cx, cy, cz, chunkSize, chunkMaterial);
                for (byte x = 0; x < chunkSize; x++)
                {
                    for (byte z = 0; z < chunkSize; z++)
                    {
                        float wx = cx * chunkSize + x;
                        float wz = cz * chunkSize + z;
                        float e = (noise.GetNoise(wx * frequency, wz * frequency) + 1f) * 0.5f;
                        int wy = (int)(e * amplitude);
                        if (wy >= cy * chunkSize && wy < (cy + 1) * chunkSize)
                        {
                            byte y = (byte)(wy - cy * chunkSize);
                            uint height = 0;
                            for (; y != byte.MaxValue; y--)
                            {
                                if (height == 0)
                                {
                                    chunk[x, y, z] = 2;
                                }
                                else if (height < 4)
                                {
                                    float r = terrainRng.NextFloat();
                                    for (int p = 1; p < height; p++)
                                    {
                                        r *= r;
                                    }

                                    chunk[x, y, z] = r > 0.2f * height ? 1u : 4u;
                                }
                                else
                                {
                                    chunk[x, y, z] = 4;
                                }

                                height++;
                            }
                        }
                    }
                }

                return chunk;
            }
        }

        public ref struct VoxelMeshGeneration
        {
            private readonly USpan<uint> blocks;
            private readonly USpan<uint> blocksLeft;
            private readonly USpan<uint> blocksRight;
            private readonly USpan<uint> blocksDown;
            private readonly USpan<uint> blocksUp;
            private readonly USpan<uint> blocksBackward;
            private readonly USpan<uint> blocksForward;
            private readonly byte chunkSize;
            private readonly Array<Vector3> vertices;
            private readonly Array<Vector2> uvs;
            private readonly Array<Vector4> colors;
            private readonly Array<uint> triangles;
            private readonly RandomGenerator rng;
            private readonly uint capacity;
            private readonly AtlasTexture chunkAtlas;

            public uint verticeIndex;
            public uint triangleIndex;

            public VoxelMeshGeneration(USpan<uint> blocks, USpan<uint> blocksLeft, USpan<uint> blocksRight, USpan<uint> blocksDown, USpan<uint> blocksUp, USpan<uint> blocksBackward, USpan<uint> blocksForward, byte chunkSize, Array<Vector3> vertices, Array<Vector2> uvs, Array<Vector4> colors, Array<uint> triangles, RandomGenerator rng, uint capacity, AtlasTexture chunkAtlas)
            {
                this.blocks = blocks;
                this.blocksLeft = blocksLeft;
                this.blocksRight = blocksRight;
                this.blocksDown = blocksDown;
                this.blocksUp = blocksUp;
                this.blocksBackward = blocksBackward;
                this.blocksForward = blocksForward;
                this.chunkSize = chunkSize;
                this.vertices = vertices;
                this.uvs = uvs;
                this.colors = colors;
                this.triangles = triangles;
                this.rng = rng;
                this.capacity = capacity;
                this.chunkAtlas = chunkAtlas;
            }

            private readonly bool ShouldGenerateFace(uint index, Direction direction)
            {
                uint neighbourBlock = GetNeighbourBlock(index, direction);
                return neighbourBlock == default;
            }

            private readonly uint GetNeighbourBlock(uint index, Direction direction)
            {
                if (direction == Direction.Left)
                {
                    if (index % chunkSize != 0)
                    {
                        return blocks[index - 1];
                    }
                    else if (blocksLeft.Length > 0)
                    {
                        return default;
                        //return blocksLeft[index + chunkSize - 1];
                    }
                    else
                    {
                        return default;
                    }
                }
                else if (direction == Direction.Right)
                {
                    if ((index + 1) % chunkSize != 0)
                    {
                        return blocks[index + 1];
                    }
                    else if (blocksRight.Length > 0)
                    {
                        return default;
                        //return blocksRight[index - chunkSize + 1];
                    }
                    else
                    {
                        return default;
                    }
                }
                else if (direction == Direction.Down)
                {
                    if (index / chunkSize % chunkSize != 0)
                    {
                        return blocks[index - chunkSize];
                    }
                    else if (blocksDown.Length > 0)
                    {
                        return default;
                        //return blocksDown[(uint)(index + chunkSize * (chunkSize - 1))];
                    }
                    else
                    {
                        return default;
                    }
                }
                else if (direction == Direction.Up)
                {
                    if (index / chunkSize % chunkSize != chunkSize - 1)
                    {
                        return blocks[index + chunkSize];
                    }
                    else if (blocksUp.Length > 0)
                    {
                        return default;
                        //return blocksUp[(uint)(index - chunkSize * (chunkSize - 1))];
                    }
                    else
                    {
                        return default;
                    }
                }
                else if (direction == Direction.Backward)
                {
                    if (index / (chunkSize * chunkSize) % chunkSize != 0)
                    {
                        return blocks[(uint)(index - chunkSize * chunkSize)];
                    }
                    else if (blocksBackward.Length > 0)
                    {
                        return default;
                        //return blocksBackward[(uint)(index + chunkSize * chunkSize * (chunkSize - 1))];
                    }
                    else
                    {
                        return default;
                    }
                }
                else if (direction == Direction.Forward)
                {
                    if (index / (chunkSize * chunkSize) % chunkSize != chunkSize - 1)
                    {
                        return blocks[(uint)(index + chunkSize * chunkSize)];
                    }
                    else if (blocksForward.Length > 0)
                    {
                        return default;
                        //return blocksForward[(uint)(index - chunkSize * chunkSize * (chunkSize - 1))];
                    }
                    else
                    {
                        return default;
                    }
                }
                else
                {
                    return default;
                }
            }

            private void AddTriangles()
            {
                uint startIndex = verticeIndex - 4;
                triangles[triangleIndex++] = startIndex + 2;
                triangles[triangleIndex++] = startIndex + 1;
                triangles[triangleIndex++] = startIndex;
                triangles[triangleIndex++] = startIndex;
                triangles[triangleIndex++] = startIndex + 3;
                triangles[triangleIndex++] = startIndex + 2;
            }

            private readonly void AddUVs(uint blockId, Direction direction, RandomGenerator rng)
            {
                uint startIndex = verticeIndex - 4;
                int rotation = 0;
                AtlasSprite sprite;
                if (blockId == 1)
                {
                    sprite = chunkAtlas["Dirt"];
                    rotation = rng.NextInt(4);
                }
                else if (blockId == 2)
                {
                    if (direction == Direction.Up)
                    {
                        sprite = chunkAtlas["Grass"];
                        rotation = rng.NextInt(4);
                    }
                    else if (direction == Direction.Down)
                    {
                        sprite = chunkAtlas["Dirt"];
                    }
                    else
                    {
                        sprite = chunkAtlas["GrassSide"];
                        if (direction == Direction.Forward)
                        {
                            rotation = 0;
                        }
                        else
                        {
                            rotation = 3;
                        }
                    }
                }
                else if (blockId == 3)
                {
                    sprite = chunkAtlas["Cobblestone"];
                    rotation = rng.NextInt(4);
                }
                else if (blockId == 4)
                {
                    sprite = chunkAtlas["Stone"];
                    rotation = rng.NextInt(4);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown block {blockId}");
                }

                Vector4 rect = sprite.region;
                if (rotation == 1)
                {
                    uvs[startIndex + 1] = new(rect.X, rect.Y);
                    uvs[startIndex + 2] = new(rect.Z, rect.Y);
                    uvs[startIndex + 3] = new(rect.Z, rect.W);
                    uvs[startIndex + 0] = new(rect.X, rect.W);
                }
                else if (rotation == 2)
                {
                    uvs[startIndex + 2] = new(rect.X, rect.Y);
                    uvs[startIndex + 3] = new(rect.Z, rect.Y);
                    uvs[startIndex + 0] = new(rect.Z, rect.W);
                    uvs[startIndex + 1] = new(rect.X, rect.W);
                }
                else if (rotation == 3)
                {
                    uvs[startIndex + 3] = new(rect.X, rect.Y);
                    uvs[startIndex + 0] = new(rect.Z, rect.Y);
                    uvs[startIndex + 1] = new(rect.Z, rect.W);
                    uvs[startIndex + 2] = new(rect.X, rect.W);
                }
                else
                {
                    uvs[startIndex + 0] = new(rect.X, rect.Y);
                    uvs[startIndex + 1] = new(rect.Z, rect.Y);
                    uvs[startIndex + 2] = new(rect.Z, rect.W);
                    uvs[startIndex + 3] = new(rect.X, rect.W);
                }

                colors[startIndex + 0] = new(1, 1, 1, 1);
                colors[startIndex + 1] = new(1, 1, 1, 1);
                colors[startIndex + 2] = new(1, 1, 1, 1);
                colors[startIndex + 3] = new(1, 1, 1, 1);
            }

            public void Generate()
            {
                for (uint i = 0; i < capacity; i++)
                {
                    uint blockId = blocks[i];
                    if (blockId != default)
                    {
                        (byte x, byte y, byte z) = GetXYZ(i, chunkSize);
                        if (ShouldGenerateFace(i, Direction.Up))
                        {
                            vertices[verticeIndex++] = new(x, y + 1f, z);
                            vertices[verticeIndex++] = new(x, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new(x + 1f, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new(x + 1f, y + 1f, z);

                            AddTriangles();
                            AddUVs(blockId, Direction.Up, rng);
                        }

                        if (ShouldGenerateFace(i, Direction.Down))
                        {
                            vertices[verticeIndex++] = new Vector3(x, y, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y, z + 1f);

                            AddTriangles();
                            AddUVs(blockId, Direction.Down, rng);
                        }

                        if (ShouldGenerateFace(i, Direction.Right))
                        {
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y + 1f, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z + 1f);

                            AddTriangles();
                            AddUVs(blockId, Direction.Right, rng);
                        }

                        if (ShouldGenerateFace(i, Direction.Left))
                        {
                            vertices[verticeIndex++] = new Vector3(x, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y + 1f, z);
                            vertices[verticeIndex++] = new Vector3(x, y, z);

                            AddTriangles();
                            AddUVs(blockId, Direction.Left, rng);
                        }

                        if (ShouldGenerateFace(i, Direction.Forward))
                        {
                            vertices[verticeIndex++] = new Vector3(x, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y + 1f, z + 1f);

                            AddTriangles();
                            AddUVs(blockId, Direction.Forward, rng);
                        }

                        if (ShouldGenerateFace(i, Direction.Backward))
                        {
                            vertices[verticeIndex++] = new Vector3(x, y, z);
                            vertices[verticeIndex++] = new Vector3(x, y + 1f, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y + 1f, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z);

                            AddTriangles();
                            AddUVs(blockId, Direction.Backward, rng);
                        }
                    }
                }
            }

            public static uint GetIndex(byte x, byte y, byte z, byte chunkSize)
            {
                return (uint)(x + y * chunkSize + z * chunkSize * chunkSize);
            }

            public static (byte x, byte y, byte z) GetXYZ(uint index, byte chunkSize)
            {
                byte x = (byte)(index % chunkSize);
                byte y = (byte)((index / chunkSize) % chunkSize);
                byte z = (byte)(index / (chunkSize * chunkSize));
                return (x, y, z);
            }
        }

        [ArrayElement]
        public readonly struct BlockID
        {
            private readonly uint value;

            public BlockID(uint value)
            {
                this.value = value;
            }
        }

        [Component]
        public readonly struct IsChunk
        {
            public readonly byte chunkSize;

            public IsChunk(byte chunkSize)
            {
                this.chunkSize = chunkSize;
            }
        }

        public readonly struct Direction : IEquatable<Direction>
        {
            public static readonly Direction Left = new(0);
            public static readonly Direction Right = new(1);
            public static readonly Direction Down = new(2);
            public static readonly Direction Up = new(3);
            public static readonly Direction Backward = new(4);
            public static readonly Direction Forward = new(5);

            public readonly byte value;

            public readonly Vector3 Vector
            {
                get
                {
                    int axis = value / 2;
                    int sign = value % 2;
                    Vector3 vector = default;
                    vector[axis] = sign * 2 - 1;
                    return vector;
                }
            }

            public Direction(byte value)
            {
                this.value = value;
            }

            public readonly override bool Equals(object? obj)
            {
                return obj is Direction direction && Equals(direction);
            }

            public readonly bool Equals(Direction other)
            {
                return value == other.value;
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(value);
            }

            public static bool operator ==(Direction left, Direction right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Direction left, Direction right)
            {
                return !(left == right);
            }
        }
    }
}