using Cameras.Components;
using Data;
using Data.Events;
using DefaultPresentationAssets;
using Meshes;
using Meshes.Components;
using Models;
using Programs;
using Rendering;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Textures;
using Textures.Events;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Unmanaged.Collections;
using Windows;

namespace Abacus
{
    public struct VoxelGame : IDisposable, IProgramType
    {
        private readonly World world;
        private readonly Camera camera;
        private readonly Material chunkMaterial;
        private readonly Mesh quadMesh;
        private readonly AtlasTexture chunkAtlas;
        private Vector3 cameraPosition;
        private Vector2 cameraPitchYaw;

        public VoxelGame(World world)
        {
            this.world = world;
            Window window = CreateWindow();

            camera = new(world, window, CameraFieldOfView.FromDegrees(90f));
            Transform cameraTransform = camera.AsEntity().Become<Transform>();
            cameraTransform.LocalPosition = new(0f, 1f, -10f);
            cameraPosition = cameraTransform.LocalPosition;

            chunkAtlas = GetChunkAtlas();

            chunkMaterial = new(world, Address.Get<UnlitTexturedMaterial>());
            chunkMaterial.AddPushBinding<Color>();
            chunkMaterial.AddPushBinding<LocalToWorld>();
            chunkMaterial.AddComponentBinding<CameraProjection>(0, 0, camera.entity);
            chunkMaterial.AddTextureBinding(1, 0, chunkAtlas, TextureFiltering.Nearest);

            Model quadModel = new(world, Address.Get<QuadModel>());
            quadMesh = new(world, quadModel.entity);

            Renderer quadRenderer = new(world, quadMesh, chunkMaterial, camera);
            quadRenderer.Mesh = quadMesh;
            quadRenderer.Material = chunkMaterial;
            quadRenderer.Camera = camera;
            quadRenderer.AsEntity().AddComponent(Color.White);
            Transform ballTransform = quadRenderer.AsEntity().Become<Transform>();
            ballTransform.LocalPosition = new(0f, 4f, 0f);

            int chunkRadius = 8;
            for (int cx = -chunkRadius; cx < chunkRadius; cx++)
            {
                for (int cz = -chunkRadius; cz < chunkRadius; cz++)
                {
                    GenerateChunk(cx, 0, cz);
                }
            }
        }

        private readonly unsafe Window CreateWindow()
        {
            return new(world, "Voxel Game", new Vector2(400, 200), new(900, 720), "vulkan", new(&WindowClosed));

            [UnmanagedCallersOnly]
            static void WindowClosed(World world, uint windowEntity)
            {
                world.DestroyEntity(windowEntity);
            }
        }

        private readonly AtlasTexture GetChunkAtlas()
        {
            Texture dirt = new(world, "*/Assets/Textures/Blocks/Dirt.png");
            Texture grass = new(world, "*/Assets/Textures/Blocks/Grass.png");
            Texture stone = new(world, "*/Assets/Textures/Blocks/Stone.png");
            Texture grassSide = new(world, "*/Assets/Textures/Blocks/GrassSide.png");
            Texture cobblestone = new(world, "*/Assets/Textures/Blocks/Cobblestone.png");

            world.Submit(new DataUpdate());
            world.Submit(new TextureUpdate());
            world.Poll();

            USpan<AtlasTexture.InputSprite> sprites = stackalloc AtlasTexture.InputSprite[]
            {
                new("Dirt", dirt.Width, dirt.Height, dirt.Pixels),
                new("Grass", grass.Width, grass.Height, grass.Pixels),
                new("Stone", stone.Width, stone.Height, stone.Pixels),
                new("GrassSide", grassSide.Width, grassSide.Height, grassSide.Pixels),
                new("Cobblestone", cobblestone.Width, cobblestone.Height, cobblestone.Pixels),
            };

            AtlasTexture atlasTexture = new(world, sprites);
            return atlasTexture;
        }

        private readonly void GenerateChunk(int cx, int cy, int cz)
        {
            using RandomGenerator rng = new();
            FastNoiseLite noise = new FastNoiseLite();
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            float frequency = 4f;
            float amplitude = 6f;
            uint chunkSize = 16;
            Chunk chunk = new(world, cx, cy, cz, chunkSize, chunkMaterial, camera);
            for (uint x = 0; x < chunkSize; x++)
            {
                for (uint z = 0; z < chunkSize; z++)
                {
                    float wx = cx * chunkSize + x;
                    float wz = cz * chunkSize + z;
                    float e = (noise.GetNoise(wx * frequency, wz * frequency) + 1f) * 0.5f;
                    int wy = (int)(e * amplitude);
                    if (wy >= cy * chunkSize && wy < (cy + 1) * chunkSize)
                    {
                        uint y = (uint)(wy - cy * chunkSize);
                        uint height = 0;
                        for (; y != uint.MaxValue; y--)
                        {
                            if (height == 0)
                            {
                                chunk[x, y, z] = 2;
                            }
                            else if (height < 4)
                            {
                                float r = rng.NextFloat();
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

            chunk.UpdateMeshToMatchBlocks(chunkAtlas);
        }

        public void Dispose()
        {
        }

        public uint Update(TimeSpan delta)
        {
            if (!AnyWindowOpen())
            {
                return 0;
            }

            Transform cameraTransform = camera.AsEntity().As<Transform>();
            SharedFunctions.MoveCameraAround(world, cameraTransform, delta, ref cameraPosition, ref cameraPitchYaw, new(1f, 1f));
            return 1;
        }

        private readonly bool AnyWindowOpen()
        {
            return world.TryGetFirst(out Window window);
        }

        readonly unsafe (StartFunction, FinishFunction, UpdateFunction) IProgramType.GetFunctions()
        {
            return (new(&Start), new(&Finish), new(&Update));

            [UnmanagedCallersOnly]
            static Allocation Start(World world)
            {
                VoxelGame program = new(world);
                return Allocation.Create(program);
            }

            [UnmanagedCallersOnly]
            static void Finish(Allocation allocation)
            {
                ref VoxelGame program = ref allocation.Read<VoxelGame>();
                program.Dispose();
                allocation.Dispose();
            }

            [UnmanagedCallersOnly]
            static uint Update(Allocation allocation, TimeSpan delta)
            {
                ref VoxelGame program = ref allocation.Read<VoxelGame>();
                return program.Update(delta);
            }
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
                    return new(blocks.pointer, blocks.Length);
                }
            }

            public readonly uint Capacity => mesh.AsEntity().GetArrayLength<BlockID>();
            public readonly ref uint this[uint index] => ref Blocks[index];
            public readonly ref uint this[uint x, uint y, uint z] => ref this[GetIndex(x, y, z, ChunkSize)];
            public readonly uint ChunkSize => mesh.AsEntity().GetComponent<IsChunk>().chunkSize;

            readonly uint IEntity.Value => mesh.GetEntityValue();
            readonly World IEntity.World => mesh.GetWorld();
            readonly Definition IEntity.Definition => new Definition().AddComponentTypes<IsMesh, IsChunk>().AddArrayTypes<uint, BlockID>();

            public Chunk(World world, int cx, int cy, int cz, uint chunkSize, Material unlitMaterial, Camera camera)
            {
                uint capacity = chunkSize * chunkSize * chunkSize;
                mesh = new(world);
                mesh.AsEntity().AddComponent(new IsChunk(chunkSize));
                mesh.CreatePositions(0);
                mesh.CreateColors(0);
                mesh.CreateUVs(0);
                USpan<BlockID> blocks = mesh.AsEntity().CreateArray<BlockID>(capacity);
                blocks.Clear();

                Renderer chunkRenderer = mesh.AsEntity().Become<Renderer>();
                chunkRenderer.Mesh = mesh;
                chunkRenderer.Material = unlitMaterial;
                chunkRenderer.Camera = camera;
                chunkRenderer.AsEntity().AddComponent(Color.White);
                Transform chunkTransform = chunkRenderer.AsEntity().Become<Transform>();
                chunkTransform.LocalPosition = new Vector3(cx, cy, cz) * chunkSize;
            }

            public readonly void UpdateMeshToMatchBlocks(AtlasTexture chunkAtlas)
            {
                uint chunkSize = ChunkSize;
                Vector3 chunkPosition = mesh.AsEntity().As<Transform>().WorldPosition;
                int cx = (int)MathF.Floor(chunkPosition.X / chunkSize);
                int cy = (int)MathF.Floor(chunkPosition.Y / chunkSize);
                int cz = (int)MathF.Floor(chunkPosition.Z / chunkSize);
                USpan<uint> blocks = Blocks;
                uint capacity = Capacity;
                using UnmanagedArray<Vector3> vertices = new(capacity * VerticesPerFace * FacesPerBlock);
                using UnmanagedArray<Vector2> uvs = new(capacity * VerticesPerFace * FacesPerBlock);
                using UnmanagedArray<Color> colors = new(capacity * VerticesPerFace * FacesPerBlock);
                using UnmanagedArray<uint> triangles = new(capacity * TrianglesPerFace * FacesPerBlock);
                using RandomGenerator rng = new();
                uint verticeIndex = 0;
                uint triangleIndex = 0;
                for (uint i = 0; i < capacity; i++)
                {
                    uint blockId = blocks[i];
                    if (blockId != default)
                    {
                        uint x = i % chunkSize;
                        uint y = (i / chunkSize) % chunkSize;
                        uint z = i / (chunkSize * chunkSize);
                        if (ShouldGenerateFace(x, y, z, blockId, Direction.Up, blocks, chunkSize))
                        {
                            vertices[verticeIndex++] = new(x, y + 1f, z);
                            vertices[verticeIndex++] = new(x, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new(x + 1f, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new(x + 1f, y + 1f, z);

                            AddTriangles();
                            AddUVs(blockId, Direction.Up, rng);
                        }

                        if (ShouldGenerateFace(x, y, z, blockId, Direction.Down, blocks, chunkSize))
                        {
                            vertices[verticeIndex++] = new Vector3(x, y, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y, z + 1f);

                            AddTriangles();
                            AddUVs(blockId, Direction.Down, rng);
                        }

                        if (ShouldGenerateFace(x, y, z, blockId, Direction.Right, blocks, chunkSize))
                        {
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y + 1f, z);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z + 1f);

                            AddTriangles();
                            AddUVs(blockId, Direction.Right, rng);
                        }

                        if (ShouldGenerateFace(x, y, z, blockId, Direction.Left, blocks, chunkSize))
                        {
                            vertices[verticeIndex++] = new Vector3(x, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y + 1f, z);
                            vertices[verticeIndex++] = new Vector3(x, y, z);

                            AddTriangles();
                            AddUVs(blockId, Direction.Left, rng);
                        }

                        if (ShouldGenerateFace(x, y, z, blockId, Direction.Forward, blocks, chunkSize))
                        {
                            vertices[verticeIndex++] = new Vector3(x, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x + 1f, y + 1f, z + 1f);
                            vertices[verticeIndex++] = new Vector3(x, y + 1f, z + 1f);

                            AddTriangles();
                            AddUVs(blockId, Direction.Forward, rng);
                        }

                        if (ShouldGenerateFace(x, y, z, blockId, Direction.Backward, blocks, chunkSize))
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

                bool ShouldGenerateFace(uint x, uint y, uint z, uint blockId, Direction direction, USpan<uint> blocks, uint chunkSize)
                {
                    if (direction == Direction.Left && x > 0)
                    {
                        return blocks[GetIndex(x - 1, y, z, chunkSize)] == default;
                    }
                    else if (direction == Direction.Right && x < chunkSize - 1)
                    {
                        return blocks[GetIndex(x + 1, y, z, chunkSize)] == default;
                    }
                    else if (direction == Direction.Down && y > 0)
                    {
                        return blocks[GetIndex(x, y - 1, z, chunkSize)] == default;
                    }
                    else if (direction == Direction.Up && y < chunkSize - 1)
                    {
                        return blocks[GetIndex(x, y + 1, z, chunkSize)] == default;
                    }
                    else if (direction == Direction.Backward && z > 0)
                    {
                        return blocks[GetIndex(x, y, z - 1, chunkSize)] == default;
                    }
                    else if (direction == Direction.Forward && z < chunkSize - 1)
                    {
                        return blocks[GetIndex(x, y, z + 1, chunkSize)] == default;
                    }

                    return true;
                }

                void AddTriangles()
                {
                    uint startIndex = verticeIndex - 4;
                    triangles[triangleIndex++] = startIndex + 2;
                    triangles[triangleIndex++] = startIndex + 1;
                    triangles[triangleIndex++] = startIndex;
                    triangles[triangleIndex++] = startIndex;
                    triangles[triangleIndex++] = startIndex + 3;
                    triangles[triangleIndex++] = startIndex + 2;
                }

                void AddUVs(uint blockId, Direction direction, RandomGenerator rng)
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
                        uvs[startIndex + 1] = new Vector2(rect.X, rect.Y);
                        uvs[startIndex + 2] = new Vector2(rect.Z, rect.Y);
                        uvs[startIndex + 3] = new Vector2(rect.Z, rect.W);
                        uvs[startIndex + 0] = new Vector2(rect.X, rect.W);
                    }
                    else if (rotation == 2)
                    {
                        uvs[startIndex + 2] = new Vector2(rect.X, rect.Y);
                        uvs[startIndex + 3] = new Vector2(rect.Z, rect.Y);
                        uvs[startIndex + 0] = new Vector2(rect.Z, rect.W);
                        uvs[startIndex + 1] = new Vector2(rect.X, rect.W);
                    }
                    else if (rotation == 3)
                    {
                        uvs[startIndex + 3] = new Vector2(rect.X, rect.Y);
                        uvs[startIndex + 0] = new Vector2(rect.Z, rect.Y);
                        uvs[startIndex + 1] = new Vector2(rect.Z, rect.W);
                        uvs[startIndex + 2] = new Vector2(rect.X, rect.W);
                    }
                    else
                    {
                        uvs[startIndex + 0] = new Vector2(rect.X, rect.Y);
                        uvs[startIndex + 1] = new Vector2(rect.Z, rect.Y);
                        uvs[startIndex + 2] = new Vector2(rect.Z, rect.W);
                        uvs[startIndex + 3] = new Vector2(rect.X, rect.W);
                    }

                    colors[startIndex + 0] = Color.White;
                    colors[startIndex + 1] = Color.White;
                    colors[startIndex + 2] = Color.White;
                    colors[startIndex + 3] = Color.White;
                }

                USpan<Vector3> meshPositions = mesh.ResizePositions(verticeIndex);
                USpan<Vector2> meshUVs = mesh.ResizeUVs(verticeIndex);
                USpan<Color> meshColors = mesh.ResizeColors(verticeIndex);
                USpan<uint> meshTriangles = mesh.ResizeTriangles(triangleIndex);
                vertices.AsSpan(0, verticeIndex).CopyTo(meshPositions);
                uvs.AsSpan(0, verticeIndex).CopyTo(meshUVs);
                colors.AsSpan(0, verticeIndex).CopyTo(meshColors);
                triangles.AsSpan(0, triangleIndex).CopyTo(meshTriangles);
                mesh.IncrementVersion();
            }

            public static uint GetIndex(uint x, uint y, uint z, uint chunkSize)
            {
                return x + y * chunkSize + z * chunkSize * chunkSize;
            }
        }

        public readonly struct BlockID
        {
            private readonly uint value;

            public BlockID(uint value)
            {
                this.value = value;
            }
        }

        public readonly struct IsChunk
        {
            public readonly uint chunkSize;

            public IsChunk(uint chunkSize)
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