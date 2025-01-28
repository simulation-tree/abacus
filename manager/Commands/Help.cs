using System;
using System.Diagnostics;

public readonly struct Help : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "help";
    ReadOnlySpan<char> ICommand.Description => "Lists all commands available";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        int namePadding = 20;
        int descriptionPadding = 60;
        string nameTitle = "Name".PadRight(namePadding);
        string isTestProjectTitle = "Description".PadRight(descriptionPadding);
        string header = $"{nameTitle} | {isTestProjectTitle}";
        Trace.WriteLine(header);
        Trace.WriteLine(new string('-', header.Length));
        foreach (ICommand command in CommandsRegistry.Commands)
        {
            Trace.WriteLine($"{command.Name.ToString().PadRight(namePadding)} | {command.Description.ToString().PadRight(descriptionPadding)}");
        }
    }
}