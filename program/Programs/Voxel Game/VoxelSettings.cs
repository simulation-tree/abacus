namespace VoxelGame
{
    public readonly struct VoxelSettings
    {
        public readonly byte chunkSize;

        public VoxelSettings(byte chunkSize)
        {
            this.chunkSize = chunkSize;
        }
    }
}