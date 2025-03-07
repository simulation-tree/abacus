using Materials;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds;

namespace VoxelGame
{
    public readonly struct TerrainGenerator : IDisposable
    {
        public readonly RandomGenerator meshRng;

        private readonly GCHandle noise;
        private readonly RandomGenerator terrainRng;

        private readonly FastNoiseLite Noise => (FastNoiseLite)(noise.Target ?? throw new());

        public TerrainGenerator(ASCIIText256 seed)
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
            byte chunkSize = world.GetFirstComponent<VoxelSettings>().chunkSize;
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
}