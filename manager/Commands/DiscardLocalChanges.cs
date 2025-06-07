using Collections.Generic;

namespace Abacus.Manager.Commands
{
    public readonly struct DiscardLocalChanges : ICommand
    {
        readonly string ICommand.Name => "discard-local-changes";
        readonly string ICommand.Description => "Discards all local changes that haven't been committed yet";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, "git restore .");
                Terminal.Execute(repository.Path, "git clean -fd");
                repository.Dispose();
            }
        }
    }
}