using System;
using Unmanaged;

namespace Abacus.Manager;

public readonly struct SemanticVersion : IEquatable<SemanticVersion>
{
    private readonly ASCIIText16 first;
    private readonly ASCIIText16 second;
    private readonly ASCIIText16 third;

    private SemanticVersion(ASCIIText16 first, ASCIIText16 second, ASCIIText16 third)
    {
        this.first = first;
        this.second = second;
        this.third = third;
    }

    public readonly override string ToString()
    {
        return $"{first}.{second}.{third}";
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is SemanticVersion version && Equals(version);
    }

    public readonly bool Equals(SemanticVersion other)
    {
        return first.Equals(other.first) && second.Equals(other.second) && third.Equals(other.third);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(first, second, third);
    }

    public static bool TryParse(ReadOnlySpan<char> text, out SemanticVersion value)
    {
        int firstDot = text.IndexOf('.');
        if (firstDot < 0)
        {
            value = default;
            return false;
        }

        int secondDot = text[(firstDot + 1)..].IndexOf('.') + firstDot + 1;
        if (secondDot < 0)
        {
            value = default;
            return false;
        }

        ASCIIText16 firstPart = new(text[..firstDot]);
        ASCIIText16 secondPart = new(text.Slice(firstDot + 1, secondDot - firstDot - 1));
        ASCIIText16 thirdPart = new(text[(secondDot + 1)..]);
        value = new SemanticVersion(firstPart, secondPart, thirdPart);
        return true;
    }

    public static SemanticVersion Parse(ReadOnlySpan<char> text)
    {
        if (TryParse(text, out SemanticVersion value))
        {
            return value;
        }
        else
        {
            throw new FormatException($"Invalid semantic version format: {text.ToString()}");
        }
    }

    private static int ComparePart(ASCIIText16 left, ASCIIText16 right)
    {
        if (left.Equals("x") || right.Equals("x"))
        {
            return 0;
        }
        else
        {
            Span<char> buffer = stackalloc char[16];
            int length = left.CopyTo(buffer);
            if (int.TryParse(buffer[..length], out int leftValue))
            {
                length = right.CopyTo(buffer);
                if (int.TryParse(buffer[..length], out int rightValue))
                {
                    return leftValue.CompareTo(rightValue);
                }
                else
                {
                    throw new FormatException($"Invalid semantic version part: {right}");
                }
            }
            else
            {
                throw new FormatException($"Invalid semantic version part: {left}");
            }
        }
    }

    public static bool operator ==(SemanticVersion left, SemanticVersion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SemanticVersion left, SemanticVersion right)
    {
        return !(left == right);
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right)
    {
        int comparison = ComparePart(left.first, right.first);
        if (comparison != 0)
        {
            return comparison > 0;
        }

        comparison = ComparePart(left.second, right.second);
        if (comparison != 0)
        {
            return comparison > 0;
        }

        comparison = ComparePart(left.third, right.third);
        if (comparison != 0)
        {
            return comparison > 0;
        }

        return false;
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right)
    {
        return right > left;
    }
}