using System;
using static Functions;

public readonly struct Build : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "build";
    ReadOnlySpan<char> ICommand.Description => "Builds all project (--release)";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        bool releaseMode = false;
        if (arguments.IndexOf("--release") != -1)
        {
            releaseMode = true;
        }

        foreach (Project project in GetProjects(workingDirectory))
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

                Call(command);
            }
        }
    }
}