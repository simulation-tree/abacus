using System;

public interface ICommand
{
    ReadOnlySpan<char> Name { get; }
    ReadOnlySpan<char> Description { get; }

    void Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments);
}