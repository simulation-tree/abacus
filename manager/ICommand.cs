namespace Abacus.Manager
{
    public interface ICommand
    {
        string Name { get; }
        string? Description { get; }

        void Execute(Runner runner, Arguments arguments);
    }
}