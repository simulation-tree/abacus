namespace Abacus.Manager.Commands
{
    public readonly struct Quit : ICommand
    {
        readonly string ICommand.Name => "quit";
        readonly string ICommand.Description => "Quits the application";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            Program.requestExit = true;
        }
    }
}