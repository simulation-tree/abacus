using Unmanaged;

namespace Abacus.Manager.Constants
{
    public readonly struct CompanyName : IConstant
    {
        static ASCIIText256 IConstant.Value => "Simulation Tree";
    }
}