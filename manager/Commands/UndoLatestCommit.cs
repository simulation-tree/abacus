using Collections.Generic;

namespace Abacus.Manager.Commands
{
    public readonly struct UndoLatestCommit : ICommand
    {
        readonly string ICommand.Name => "undo-latest-commit";
        readonly string ICommand.Description => "Undoes the latest commit in the current repository";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, "git reset --soft HEAD^");
                repository.Dispose();
            }
        }
    }
}