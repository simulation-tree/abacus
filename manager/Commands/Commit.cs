using System;
using System.Diagnostics;
using static Functions;

public readonly struct Commit : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "commit";
    ReadOnlySpan<char> ICommand.Description => "Creates a commit for all projects";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        if (arguments.Length == 0)
        {
            Trace.TraceError("A commit message is expected as a parameter");
        }

        foreach (Project project in GetProjects(workingDirectory))
        {
            Call(project.WorkingDirectory, $"git commit -m \"{arguments.ToString()}\"");
        }
    }
}