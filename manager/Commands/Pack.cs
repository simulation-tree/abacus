using Collections.Generic;
using System.IO;

namespace Abacus.Manager.Commands
{
    public readonly struct Pack : ICommand
    {
        readonly string ICommand.Name => "pack";
        readonly string ICommand.Description => "Creates a NuGet package for all non test projects";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                SemanticVersion version = repository.GreatestVersion;
                foreach (Project project in repository.Projects)
                {
                    bool isExecutable = project.OutputType == OutputType.WinExe || project.OutputType == OutputType.Exe;
                    if (!project.isTestProject && !isExecutable)
                    {
                        string name = Path.GetFileName(project.Directory.ToString());
                        Terminal.Execute(repository.Path, $"dotnet build \"{name}\" -c Release /p:Version={version}");
                        Terminal.Execute(repository.Path, $"dotnet build \"{name}\" -c Debug /p:Version={version}");
                        Terminal.Execute(repository.Path, $"dotnet pack \"{name}\" /p:Version={version} --no-build --output .");
                    }
                }

                repository.Dispose();
            }
        }
    }
}