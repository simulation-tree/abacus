using System.Threading;

namespace Abacus.Manager.Commands
{
    public readonly struct Wait : ICommand
    {
        readonly string ICommand.Name => "wait";
        readonly string? ICommand.Description => "Waits some amount of time";

        void ICommand.Execute(Runner runner, Arguments arguments)
        {
            if (arguments.IsEmpty)
            {
                runner.WriteErrorLine("A time in milliseconds is expected as as the only parameter");
                return;
            }

            if (!int.TryParse(arguments[0], out int milliseconds))
            {
                runner.WriteErrorLine("The time in milliseconds must be a valid integer");
                return;
            }

            Thread.Sleep(milliseconds);
        }
    }
}