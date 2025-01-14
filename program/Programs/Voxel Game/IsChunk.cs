using Worlds;

namespace VoxelGame
{
    [Component]
    public readonly struct IsChunk
    {
        public readonly byte chunkSize;

        public IsChunk(byte chunkSize)
        {
            this.chunkSize = chunkSize;
        }
    }
}