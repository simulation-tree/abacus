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
        private const uint VerticesPerFace = 4;
        private const uint TrianglesPerFace = 6;
        private const uint FacesPerBlock = 6;

        public unsafe readonly USpan<uint> Blocks => GetArray<BlockID>().As<uint>();
        public readonly uint Capacity => GetArrayLength<BlockID>();
        public readonly ref uint this[uint index] => ref Blocks[index];
        public readonly ref uint this[byte x, byte y, byte z] => ref this[MeshGenerator.GetIndex(x, y, z, ChunkSize)];
        public readonly byte ChunkSize => world.GetFirstComponent<VoxelSettings>().chunkSize;

        public Chunk(World world, int cx, int cy, int cz, byte chunkSize, Material unlitMaterial)
        {
            uint capacity = (uint)(chunkSize * chunkSize * chunkSize);
            this.world = world;
            Mesh mesh = new(world);
            value = mesh.value;

            mesh.AddTag<IsChunk>();
            mesh.CreatePositions(0);
            mesh.CreateColors(0);
            mesh.CreateUVs(0);

            USpan<BlockID> blocks = mesh.CreateArray<BlockID>(capacity);
            blocks.Clear();

            MeshRenderer chunkRenderer = mesh.Become<MeshRenderer>();
            chunkRenderer.Mesh = mesh;
            chunkRenderer.Material = unlitMaterial;
            chunkRenderer.RenderMask = VoxelGameProgram.worldMask;

            chunkRenderer.AddComponent(Color.White);
            Transform chunkTransform = chunkRenderer.Become<Transform>();
            chunkTransform.LocalPosition = new Vector3(cx, cy, cz) * chunkSize;
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
            MeshGenerator generation = new(blocks, blocksLeft, blocksRight, blocksDown, blocksUp, blocksBackward, blocksForward, chunkSize, vertices, uvs, colors, triangles, meshRng, capacity, chunkAtlas, blockTextures);
            generation.Generate();
            USpan<Vector3> meshPositions = mesh.ResizePositions(generation.verticeIndex);
            USpan<Vector2> meshUVs = mesh.ResizeUVs(generation.verticeIndex);
            USpan<Vector4> meshColors = mesh.ResizeColors(generation.verticeIndex);
            USpan<uint> meshTriangles = mesh.ResizeIndices(generation.triangleIndex);
            vertices.GetSpan(generation.verticeIndex).CopyTo(meshPositions);
            uvs.GetSpan(generation.verticeIndex).CopyTo(meshUVs);
            colors.GetSpan(generation.verticeIndex).CopyTo(meshColors);
            triangles.GetSpan(generation.triangleIndex).CopyTo(meshTriangles);
            mesh.IncrementVersion();
        }

        public static USpan<uint> GetBlocks(World world, int cx, int cy, int cz)
        {
            byte chunkSize = world.GetFirstComponent<VoxelSettings>().chunkSize;
            TagType chunkType = world.Schema.GetTag<IsChunk>();
            ComponentType positionType = world.Schema.GetComponent<Position>();
            foreach (Worlds.Chunk chunk in world.Chunks)
            {
                Definition key = chunk.Definition;
                if (key.Contains(chunkType) && key.Contains(positionType))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<Position> components = chunk.GetComponents<Position>(positionType);
                    for (uint i = 0; i < components.Length; i++)
                    {
                        ref Position position = ref components[i];
                        Vector3 worldPosition = position.value;
                        int chunkX = (int)MathF.Floor(worldPosition.X / chunkSize);
                        int chunkY = (int)MathF.Floor(worldPosition.Y / chunkSize);
                        int chunkZ = (int)MathF.Floor(worldPosition.Z / chunkSize);
                        if (chunkX == cx && chunkY == cy && chunkZ == cz)
                        {
                            uint entity = entities[i];
                            return world.GetArray<BlockID>(entity).As<uint>();
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