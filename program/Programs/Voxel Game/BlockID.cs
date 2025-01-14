using Worlds;

namespace VoxelGame
{
    [ArrayElement]
    public readonly struct BlockID
    {
        private readonly uint value;

        public BlockID(uint value)
        {
            this.value = value;
        }
    }
}