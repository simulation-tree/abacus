using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Constant
    {
        public static ASCIIText256 Get<T>() where T : unmanaged, IConstant
        {
            return T.Value;
        }
    }
}