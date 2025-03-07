using Unmanaged;

namespace Abacus.Manager.Constants
{
    public readonly struct RepositoryHost : IConstant
    {
        static ASCIIText256 IConstant.Value => "https://github.com";
    }
}