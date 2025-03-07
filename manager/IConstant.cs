using Unmanaged;

namespace Abacus.Manager
{
    public interface IConstant
    {
        static abstract ASCIIText256 Value { get; }
    }
}