using System;
using static Functions;

public readonly struct Fetch : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "fetch";
    ReadOnlySpan<char> ICommand.Description => "Fetches commits for all projects";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        foreach (Project project in GetProjects(workingDirectory))
        {
            Call(project.WorkingDirectory, $"git fetch");
        }
    }
}