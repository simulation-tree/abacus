using System;

namespace Abacus.Manager.Commands
{
    public readonly struct Clear : ICommand
    {
        readonly string ICommand.Name => "clear";
        readonly string? ICommand.Description => "Clears the console";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            Console.Clear();
        }
    }
}