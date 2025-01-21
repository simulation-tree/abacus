using System;
using System.IO;
using static Functions;

CommandsRegistry.RegisterAll();
string commandInput;
if (args.Length == 0)
{
    Console.WriteLine("Enter command:");
    Console.Write("> ");
    commandInput = Console.ReadLine() ?? string.Empty;
    if (commandInput.Length == 0)
    {
        Console.WriteLine("No command entered");
        return;
    }
}
else
{
    commandInput = string.Join(" ", args);
}

do
{
    string commandName;
    string commandArguments;
    int spaceIndex = commandInput.IndexOf(' ');
    if (spaceIndex == -1)
    {
        commandName = commandInput;
        commandArguments = string.Empty;
    }
    else
    {
        commandName = commandInput.Substring(0, spaceIndex);
        commandArguments = commandInput.Substring(spaceIndex + 1);
    }

    try
    {
        if (CommandsRegistry.TryGet(commandName, out ICommand? command))
        {
            ReadOnlySpan<char> solutionPath = GetSolutionPath();
            string workingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(solutionPath.ToString())) ?? string.Empty;
            command.Execute(workingDirectory, commandArguments);
        }
        else
        {
            Console.WriteLine($"Command `{commandName}` not found");
            ICommand helpCommand = new Help();
            helpCommand.Execute(default, default);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
    }

    Console.Write("> ");
    commandInput = Console.ReadLine() ?? string.Empty;
    if (commandInput.Length == 0)
    {
        Console.WriteLine("No command entered");
        return;
    }
}
while (true);