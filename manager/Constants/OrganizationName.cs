using Unmanaged;

namespace Abacus.Manager.Constants
{
    public readonly struct OrganizationName : IConstant
    {
        static ASCIIText256 IConstant.Value => "simulation-tree";
    }
}