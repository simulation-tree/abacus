using Unmanaged;

namespace VoxelGame
{
    public readonly struct BlockTexture
    {
        public readonly FixedString name;
        public readonly Rotation rotation;

        public BlockTexture(FixedString name, Rotation rotation = Rotation.Random)
        {
            this.name = name;
            this.rotation = rotation;
        }

        public enum Rotation : byte
        {
            Default = 0,
            Clockwise90 = 1,
            Clockwise180 = 2,
            Clockwise270 = 3,
            Random
        }
    }
}