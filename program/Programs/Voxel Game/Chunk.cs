using Collections.Generic;
using Materials;
using Meshes;
using Meshes.Components;
using Rendering;
using System;
using System.Numerics;
using Textures;
using Transforms;
using Transforms.Components;
using Unmanaged;
using Worlds;

namespace VoxelGame
{
    public readonly partial struct Chunk : IEntity
    {
        private const int VerticesPerFace = 4;
        private const int TrianglesPerFace = 6;
        private const int FacesPerBlock = 6;

        public unsafe readonly Span<uint> Blocks => GetArray<BlockID>().AsSpan<uint>();
        public readonly int Capacity => GetArrayLength<BlockID>();
        public readonly ref BlockID this[int index] => ref GetArrayElement<BlockID>(index);
        public readonly ref BlockID this[byte x, byte y, byte z] => ref this[MeshGenerator.GetIndex(x, y, z, ChunkSize)];
        public readonly byte ChunkSize => world.GetFirstComponent<VoxelSettings>().chunkSize;

        public Chunk(World world, int cx, int cy, int cz, byte chunkSize, Material unlitMaterial)
        {
            int capacity = chunkSize * chunkSize * chunkSize;
            this.world = world;
            Mesh mesh = new(world);
            value = mesh.value;

            mesh.AddTag<IsChunk>();
            mesh.CreatePositions(0);
            mesh.CreateColors(0);
            mesh.CreateUVs(0);

            Span<BlockID> blocks = mesh.CreateArray<BlockID>(capacity).AsSpan();
            blocks.Clear();

            MeshRenderer chunkRenderer = mesh.Become<MeshRenderer>();
            chunkRenderer.Mesh = mesh;
            chunkRenderer.Material = unlitMaterial;
            chunkRenderer.RenderMask = VoxelGameProgram.worldMask;

            chunkRenderer.AddComponent(Color.White);
            Transform chunkTransform = chunkRenderer.Become<Transform>();
            chunkTransform.LocalPosition = new(cx * chunkSize, cy * chunkSize, cz * chunkSize);
        }

        readonly void IEntity.Describe(ref Archetype archetype)
        {
            archetype.AddComponentType<IsMesh>();
            archetype.AddArrayType<uint>();
            archetype.AddArrayType<BlockID>();
            archetype.AddTagType<IsChunk>();
        }

        public readonly void UpdateMeshToMatchBlocks(AtlasTexture chunkAtlas, Dictionary<BlockTextureKey, BlockTexture> blockTextures, RandomGenerator meshRng)
        {
            Mesh mesh = As<Mesh>();
            byte chunkSize = ChunkSize;
            Vector3 chunkPosition = As<Transform>().WorldPosition;
            int cx = (int)MathF.Floor(chunkPosition.X / chunkSize);
            int cy = (int)MathF.Floor(chunkPosition.Y / chunkSize);
            int cz = (int)MathF.Floor(chunkPosition.Z / chunkSize);
            Span<uint> blocks = Blocks;
            Span<uint> blocksLeft = GetBlocks(world, cx - 1, cy, cz);
            Span<uint> blocksRight = GetBlocks(world, cx + 1, cy, cz);
            Span<uint> blocksDown = GetBlocks(world, cx, cy - 1, cz);
            Span<uint> blocksUp = GetBlocks(world, cx, cy + 1, cz);
            Span<uint> blocksBackward = GetBlocks(world, cx, cy, cz - 1);
            Span<uint> blocksForward = GetBlocks(world, cx, cy, cz + 1);
            int capacity = Capacity;
            using Array<Vector3> vertices = new(capacity * VerticesPerFace * FacesPerBlock);
            using Array<Vector2> uvs = new(capacity * VerticesPerFace * FacesPerBlock);
            using Array<Vector4> colors = new(capacity * VerticesPerFace * FacesPerBlock);
            using Array<uint> triangles = new(capacity * TrianglesPerFace * FacesPerBlock);
            MeshGenerator generation = new(blocks, blocksLeft, blocksRight, blocksDown, blocksUp, blocksBackward, blocksForward, chunkSize, vertices, uvs, colors, triangles, meshRng, capacity, chunkAtlas, blockTextures);
            generation.Generate();
            Mesh.Collection<Vector3> meshPositions = mesh.Positions;
            Mesh.Collection<Vector2> meshUVs = mesh.UVs;
            Mesh.Collection<Vector4> meshColors = mesh.Colors;
            Mesh.Collection<uint> meshTriangles = mesh.Indices;
            meshPositions.CopyFrom(vertices.GetSpan(generation.verticeIndex));
            meshUVs.CopyFrom(uvs.GetSpan(generation.verticeIndex));
            meshColors.CopyFrom(colors.GetSpan(generation.verticeIndex));
            meshTriangles.CopyFrom(triangles.GetSpan(generation.triangleIndex));
        }

        public static Span<uint> GetBlocks(World world, int cx, int cy, int cz)
        {
            byte chunkSize = world.GetFirstComponent<VoxelSettings>().chunkSize;
            int chunkType = world.Schema.GetTagType<IsChunk>();
            int positionType = world.Schema.GetComponentType<Position>();
            foreach (Worlds.Chunk chunk in world.Chunks)
            {
                Definition key = chunk.Definition;
                if (key.ContainsTag(chunkType) && key.ContainsComponent(positionType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<Position> components = chunk.GetComponents<Position>(positionType);
                    for (int i = 0; i < components.length; i++)
                    {
                        ref Position position = ref components[i];
                        Vector3 worldPosition = position.value;
                        int chunkX = (int)MathF.Floor(worldPosition.X / chunkSize);
                        int chunkY = (int)MathF.Floor(worldPosition.Y / chunkSize);
                        int chunkZ = (int)MathF.Floor(worldPosition.Z / chunkSize);
                        if (chunkX == cx && chunkY == cy && chunkZ == cz)
                        {
                            uint entity = entities[i];
                            return world.GetArray<BlockID>(entity).AsSpan<uint>();
                        }
                    }
                }
            }

            return default;
        }

        public static implicit operator Mesh(Chunk chunk)
        {
            return chunk.As<Mesh>();
        }

        public static implicit operator Transform(Chunk chunk)
        {
            return chunk.As<Transform>();
        }
    }
}