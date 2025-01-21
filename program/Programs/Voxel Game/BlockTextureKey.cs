using System;

namespace VoxelGame
{
    public readonly struct BlockTextureKey : IEquatable<BlockTextureKey>
    {
        public readonly uint blockId;
        public readonly Direction direction;

        public BlockTextureKey(uint blockId, Direction direction)
        {
            this.blockId = blockId;
            this.direction = direction;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is BlockTextureKey key && Equals(key);
        }

        public readonly bool Equals(BlockTextureKey other)
        {
            return blockId == other.blockId && direction.Equals(other.direction);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(blockId, direction);
        }

        public static bool operator ==(BlockTextureKey left, BlockTextureKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockTextureKey left, BlockTextureKey right)
        {
            return !(left == right);
        }
    }
}