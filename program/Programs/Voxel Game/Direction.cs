using System;
using System.Numerics;

namespace VoxelGame
{
    public readonly struct Direction : IEquatable<Direction>
    {
        public static readonly Direction Left = new(0);
        public static readonly Direction Right = new(1);
        public static readonly Direction Down = new(2);
        public static readonly Direction Up = new(3);
        public static readonly Direction Backward = new(4);
        public static readonly Direction Forward = new(5);

        public readonly byte value;

        public readonly Vector3 Vector
        {
            get
            {
                int axis = value / 2;
                int sign = value % 2;
                Vector3 vector = default;
                vector[axis] = sign * 2 - 1;
                return vector;
            }
        }

        public Direction(byte value)
        {
            this.value = value;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Direction direction && Equals(direction);
        }

        public readonly bool Equals(Direction other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public static bool operator ==(Direction left, Direction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Direction left, Direction right)
        {
            return !(left == right);
        }
    }
}