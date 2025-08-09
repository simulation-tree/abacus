using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct Info : ICommand
    {
        readonly string ICommand.Name => "info";
        readonly string? ICommand.Description => "Prints information about the project";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using Text text = new();
            text.Append("Solution path: ");
            text.Append(runner.SolutionPath);
            runner.WriteInfo(text.AsSpan());
        }
    }
}