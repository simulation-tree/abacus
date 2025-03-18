namespace VoxelGame
{
    public readonly struct BlockID
    {
        private readonly uint value;

        public BlockID(uint value)
        {
            this.value = value;
        }

        public static implicit operator uint(BlockID id)
        {
            return id.value;
        }

        public static implicit operator BlockID(uint value)
        {
            return new BlockID(value);
        }
    }
}