using Abacus.Manager;
using Abacus.Manager.Commands;
using System;
using System.IO;
using System.Threading.Tasks;
using Unmanaged;

public static class Program
{
    public static bool requestExit;

    private static async Task Main(string[] args)
    {
        CommandsRegistry.RegisterAll();
        USpan<char> solutionPath = GetSolutionPath();
        string workingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(solutionPath.ToString())) ?? string.Empty;
        using Runner runner = new(workingDirectory.AsSpan(), solutionPath);
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
                    using Arguments arguments = new(commandArguments.AsSpan());
                    await Execute(command, runner, arguments);
                    if (requestExit)
                    {
                        break;
                    }
                }
                else
                {
                    LogMessage message = runner.WriteErrorLine($"Command `{commandName}` not found");
                    WriteMessage(message);
                    ICommand helpCommand = new Help();
                    helpCommand.Execute(runner, default);
                }
            }
            catch (Exception ex)
            {
                LogMessage message = runner.WriteErrorLine(ex.StackTrace?.ToString() ?? ex.Message);
                WriteMessage(message);
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
    }

    private static void WriteMessage(LogMessage message)
    {
        if (message.category == LogMessage.Category.Info)
        {
            Console.WriteLine(message.Message.ToString());
        }
        else if (message.category == LogMessage.Category.Error)
        {
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message.Message.ToString());
            Console.ForegroundColor = previousColor;
        }
    }

    private static async Task Execute<T>(T command, Runner runner, Arguments arguments) where T : ICommand
    {
        int animationIndex = 0;
        runner.ClearMessages();
        Task execute = Task.Run(() =>
        {
            command.Execute(runner, arguments);
        });

        int left = command.Name.Length + (int)arguments.RawText.Length + 4;
        int top = Console.CursorTop - 1;
        while (true)
        {
            Console.SetCursorPosition(left, top);
            string animationFrame = LoadingAnimation.GetFrame(animationIndex++);
            Console.Write(animationFrame);
            await Task.Delay(100);
            if (execute.IsCompleted)
            {
                Console.SetCursorPosition(left, top);
                for (int i = 0; i < animationFrame.Length; i++)
                {
                    Console.Write(' ');
                }

                Console.WriteLine();
                foreach (LogMessage message in runner.LogMessages)
                {
                    WriteMessage(message);
                }

                break;
            }
        }
    }

    private static USpan<char> GetSolutionPath()
    {
        string? path = Environment.CurrentDirectory;
        string? lastSolutionPath = null;
        while (path is not null)
        {
            string[] files = Directory.GetFiles(path, "*.sln");
            if (files.Length > 0)
            {
                lastSolutionPath = files[0];
            }

            path = Directory.GetParent(path)?.FullName;
        }

        if (lastSolutionPath is null)
        {
            throw new Exception("Solution file not found");
        }

        return lastSolutionPath.AsSpan();
    }
}