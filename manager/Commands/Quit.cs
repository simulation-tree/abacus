using System;

public readonly struct Quit : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "quit";
    ReadOnlySpan<char> ICommand.Description => "Quits the application";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        Environment.Exit(0);
    }
}