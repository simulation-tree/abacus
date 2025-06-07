namespace Abacus.Manager
{
    public static class TargetsTemplate
    {
        public const string Source =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">

    <ItemGroup Condition=""'$(Configuration)' == 'Debug'"">
        <Reference Include=""{{ProjectName}}"">
            <HintPath>$(MSBuildThisFileDirectory)..\tools\debug\{{ProjectName}}.dll</HintPath>
            <Private>True</Private>
        </Reference>
    </ItemGroup>

    <ItemGroup Condition=""'$(Configuration)' == 'Release'"">
        <Reference Include=""{{ProjectName}}"">
            <HintPath>$(MSBuildThisFileDirectory)..\tools\release\{{ProjectName}}.dll</HintPath>
            <Private>True</Private>
        </Reference>
    </ItemGroup>

</Project>";
    }
}