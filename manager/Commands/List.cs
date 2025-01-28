using System;
using System.Diagnostics;
using static Functions;

public readonly struct List : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "list";
    ReadOnlySpan<char> ICommand.Description => "Lists all projects";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        int titlePadding = 50;
        int isTestPadding = 10;
        string nameTitle = "Name".PadRight(titlePadding);
        string isTestProjectTitle = "Is Test?".PadRight(isTestPadding);
        string header = $"{nameTitle} | {isTestProjectTitle}";
        Trace.WriteLine(header);
        Trace.WriteLine(new string('-', header.Length));
        foreach (Project project in GetProjects(workingDirectory))
        {
            Trace.WriteLine($"{project.Name.ToString().PadRight(titlePadding)} | {project.isTestProject.ToString().PadRight(isTestPadding)}");
        }
    }
}