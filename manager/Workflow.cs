using System;
using Unmanaged;

namespace Abacus.Manager;

public readonly struct Workflow : IEquatable<Workflow>
{
    public static readonly Workflow Test = new("Test");
    public static readonly Workflow Publish = new("Publish");

    private readonly ASCIIText32 name;

    [Obsolete("Not supported", true)]
    public Workflow() { }

    private Workflow(ASCIIText32 name)
    {
        this.name = name;
    }

    public readonly override string ToString()
    {
        return name.ToString();
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Workflow workflow && Equals(workflow);
    }

    public readonly bool Equals(Workflow other)
    {
        return name.Equals(other.name);
    }

    public readonly override int GetHashCode()
    {
        return name.GetHashCode();
    }

    public static bool operator ==(Workflow left, Workflow right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Workflow left, Workflow right)
    {
        return !(left == right);
    }
}