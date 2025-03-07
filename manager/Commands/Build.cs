using Collections.Generic;

namespace Abacus.Manager.Commands
{
    public readonly struct Build : ICommand
    {
        readonly string ICommand.Name => "build";
        readonly string ICommand.Description => "Builds all project (--release)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            bool releaseMode = false;
            if (arguments.Contains("--release"))
            {
                releaseMode = true;
            }

            using Array<Project> projects = runner.GetProjects();
            foreach (Project project in projects)
            {
                if (!project.isTestProject)
                {
                    string command = $"dotnet build \"{project.Path.ToString()}\"";
                    if (releaseMode)
                    {
                        command += " -c Release";
                    }
                    else
                    {
                        command += " -c Debug";
                    }

                    Terminal.Execute(runner.WorkingDirectory, command);
                }

                project.Dispose();
            }
        }
    }
}