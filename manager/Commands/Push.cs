using Collections.Generic;

namespace Abacus.Manager.Commands
{
    public readonly struct Push : ICommand
    {
        readonly string ICommand.Name => "push";
        readonly string? ICommand.Description => "Pushes all commits";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, "git push");
                repository.Dispose();
            }
        }
    }
}