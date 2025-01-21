using System;

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
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));
        foreach (ICommand command in CommandsRegistry.Commands)
        {
            Console.WriteLine($"{command.Name.ToString().PadRight(namePadding)} | {command.Description.ToString().PadRight(descriptionPadding)}");
        }
    }
}