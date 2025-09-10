using System;
using Unmanaged;

namespace Abacus.Manager;

public readonly struct TargetFramework : IEquatable<TargetFramework>, ISpanFormattable
{
    public static readonly TargetFramework[] All;
    public static readonly TargetFramework NetStandard10 = new("netstandard1.0");
    public static readonly TargetFramework NetStandard11 = new("netstandard1.1");
    public static readonly TargetFramework NetStandard12 = new("netstandard1.2");
    public static readonly TargetFramework NetStandard13 = new("netstandard1.3");
    public static readonly TargetFramework NetStandard14 = new("netstandard1.4");
    public static readonly TargetFramework NetStandard15 = new("netstandard1.5");
    public static readonly TargetFramework NetStandard16 = new("netstandard1.6");
    public static readonly TargetFramework NetStandard20 = new("netstandard2.0");
    public static readonly TargetFramework NetStandard21 = new("netstandard2.1");
    public static readonly TargetFramework NetCoreApp10 = new("netcoreapp1.0");
    public static readonly TargetFramework NetCoreApp11 = new("netcoreapp1.1");
    public static readonly TargetFramework NetCoreApp20 = new("netcoreapp2.0");
    public static readonly TargetFramework NetCoreApp21 = new("netcoreapp2.1");
    public static readonly TargetFramework NetCoreApp22 = new("netcoreapp2.2");
    public static readonly TargetFramework NetCoreApp30 = new("netcoreapp3.0");
    public static readonly TargetFramework NetCoreApp31 = new("netcoreapp3.1");
    public static readonly TargetFramework Net5 = new("net5.0");
    public static readonly TargetFramework Net6 = new("net6.0");
    public static readonly TargetFramework Net7 = new("net7.0");
    public static readonly TargetFramework Net8 = new("net8.0");
    public static readonly TargetFramework Net9 = new("net9.0");
    public static readonly TargetFramework Net10 = new("net10.0");

    static TargetFramework()
    {
        All =
        [
            NetStandard10,
            NetStandard11,
            NetStandard12,
            NetStandard13,
            NetStandard14,
            NetStandard15,
            NetStandard16,
            NetStandard20,
            NetStandard21,
            NetCoreApp10,
            NetCoreApp11,
            NetCoreApp20,
            NetCoreApp21,
            NetCoreApp22,
            NetCoreApp30,
            NetCoreApp31,
            Net5,
            Net6,
            Net7,
            Net8,
            Net9,
            Net10
        ];
    }

    private readonly ASCIIText16 value;

    public readonly bool IsDotNet => value.StartsWith("net") && !value.StartsWith("netstandard") && !value.StartsWith("netcoreapp");
    public readonly bool IsNetStandard => value.StartsWith("netstandard");
    public readonly bool IsNetCoreApp => value.StartsWith("netcoreapp");

    private TargetFramework(ASCIIText16 value)
    {
        this.value = value;
    }

    public readonly override string ToString()
    {
        return value.ToString();
    }

    public readonly SemanticVersion GetSemanticVersion()
    {
        if (value.StartsWith("netcoreapp"))
        {
            return SemanticVersion.Parse(value[10..].ToString() + ".x");
        }
        else if (value.StartsWith("netstandard"))
        {
            return SemanticVersion.Parse(value[11..].ToString() + ".x");
        }
        else if (value.StartsWith("net"))
        {
            return SemanticVersion.Parse(value[3..].ToString() + ".x");
        }
        else
        {
            throw new($"Cannot get semantic version for target framework `{value}`");
        }
    }

    public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        value.CopyTo(destination);
        charsWritten = value.Length;
        return true;
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return value.ToString();
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is TargetFramework framework && Equals(framework);
    }

    public readonly bool Equals(TargetFramework other)
    {
        return value.Equals(other.value);
    }

    public readonly override int GetHashCode()
    {
        return value.GetHashCode();
    }

    public static bool TryParse(ReadOnlySpan<char> targetFramework, out TargetFramework value)
    {
        value = new(targetFramework);
        if (Array.IndexOf(All, value) != -1)
        {
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public static TargetFramework Parse(ReadOnlySpan<char> targetFramework)
    {
        // trim start of whitespace
        while (char.IsWhiteSpace(targetFramework[0]))
        {
            targetFramework = targetFramework[1..];
        }

        // trim end of whitespace
        while (char.IsWhiteSpace(targetFramework[^1]))
        {
            targetFramework = targetFramework[..^1];
        }

        if (TryParse(targetFramework, out TargetFramework value))
        {
            return value;
        }
        else
        {
            throw new($"`{targetFramework}` is not a valid target framework");
        }
    }

    public static bool operator ==(TargetFramework left, TargetFramework right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TargetFramework left, TargetFramework right)
    {
        return !(left == right);
    }

    public static bool operator >(TargetFramework left, TargetFramework right)
    {
        return left.GetSemanticVersion() > right.GetSemanticVersion();
    }

    public static bool operator <(TargetFramework left, TargetFramework right)
    {
        return left.GetSemanticVersion() < right.GetSemanticVersion();
    }
}