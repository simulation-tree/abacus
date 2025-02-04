namespace Abacus.Manager.Commands
{
    public readonly struct Help : ICommand
    {
        readonly string ICommand.Name => "help";
        readonly string ICommand.Description => "Lists all commands available";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using TableBuilder table = new("Name", "Description");
            foreach (ICommand command in CommandsRegistry.Commands)
            {
                string name = command.Name;
                string description = command.Description ?? string.Empty;
                table.AddRow(name, description);
            }

            runner.WriteInfoLine(table.ToString());
        }
    }
}