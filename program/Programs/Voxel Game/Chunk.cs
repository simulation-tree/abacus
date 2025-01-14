using Collections;
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
        public readonly ref uint this[byte x, byte y, byte z] => ref this[MeshGenerator.GetIndex(x, y, z, ChunkSize)];
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
            chunkRenderer.RenderMask = new LayerMask().Set(1);

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
            MeshGenerator generation = new(blocks, blocksLeft, blocksRight, blocksDown, blocksUp, blocksBackward, blocksForward, chunkSize, vertices, uvs, colors, triangles, meshRng, capacity, chunkAtlas);
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
}