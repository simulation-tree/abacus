using Collections.Generic;

namespace Abacus.Manager.Commands
{
    public readonly struct Normalize : ICommand
    {
        string ICommand.Name => "normalize";
        string? ICommand.Description => "Reformats .csproj files for consistency";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using Array<Project> projects = runner.GetProjects();
            foreach (Project project in projects)
            {
                project.WriteToFile();
                project.Dispose();
            }
        }
    }
}