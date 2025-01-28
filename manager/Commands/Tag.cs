using System;
using System.Diagnostics;
using static Functions;

public readonly struct Tag : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "tag";
    ReadOnlySpan<char> ICommand.Description => "Tags all projects and pushed it to remote";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        if (arguments.Length == 0)
        {
            Trace.TraceError("Arguments are empty, expected a tag with a branch name separated by a space");
        }

        int spaceIndex = arguments.IndexOf(' ');
        if (spaceIndex == -1)
        {
            Trace.TraceError("A tag and branch name were expected");
            return;
        }

        ReadOnlySpan<char> tag = arguments.Slice(0, spaceIndex);
        ReadOnlySpan<char> branchName = arguments.Slice(spaceIndex + 1);
        if (branchName.IsEmpty)
        {
            Trace.TraceError("A branch name was not given");
            return;
        }

        foreach (Project project in GetProjects(workingDirectory))
        {
            Call(project.WorkingDirectory, $"git tag {tag.ToString()} {branchName.ToString()}");
            Call(project.WorkingDirectory, $"git push origin tag {tag.ToString()}");
        }
    }
}