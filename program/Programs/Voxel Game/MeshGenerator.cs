using Collections;
using System;
using System.Numerics;
using Textures;
using Unmanaged;

namespace VoxelGame
{
    public ref struct MeshGenerator
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
        private readonly Dictionary<BlockTextureKey, BlockTexture> blockTextures;

        public uint verticeIndex;
        public uint triangleIndex;

        public MeshGenerator(USpan<uint> blocks, USpan<uint> blocksLeft, USpan<uint> blocksRight, USpan<uint> blocksDown, USpan<uint> blocksUp, USpan<uint> blocksBackward, USpan<uint> blocksForward, byte chunkSize, Array<Vector3> vertices, Array<Vector2> uvs, Array<Vector4> colors, Array<uint> triangles, RandomGenerator rng, uint capacity, AtlasTexture chunkAtlas, Dictionary<BlockTextureKey, BlockTexture> blockTextures)
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
            this.blockTextures = blockTextures;
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
            int rotation;
            AtlasSprite sprite;

            if (blockTextures.TryGetValue(new(blockId, direction), out BlockTexture blockTexture))
            {
                sprite = chunkAtlas[blockTexture.name];
                if (blockTexture.rotation == BlockTexture.Rotation.Random)
                {
                    rotation = rng.NextInt(4);
                }
                else
                {
                    rotation = (byte)blockTexture.rotation;
                }
            }
            else
            {
                throw new InvalidOperationException($"No texture mapping for `{blockId}` with direction `{direction}`");
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
}