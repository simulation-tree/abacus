using Collections;

namespace Abacus.Manager.Commands
{
    public readonly struct Fetch : ICommand
    {
        readonly string ICommand.Name => "fetch";
        readonly string ICommand.Description => "Fetches commits for all projects";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, $"git fetch");
                repository.Dispose();
            }
        }
    }
}