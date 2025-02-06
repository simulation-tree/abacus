using Collections;

namespace Abacus.Manager.Commands
{
    public readonly struct Fetch : ICommand
    {
        readonly string ICommand.Name => "fetch";
        readonly string ICommand.Description => "Fetches commits for all projects (--all, --prune)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            bool all = arguments.Contains("--all");
            bool prune = arguments.Contains("--prune");
            string command = "git fetch";
            if (all)
            {
                command += " --all";
            }

            if (prune)
            {
                command += " --prune";
            }

            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, command);
                repository.Dispose();
            }
        }
    }
}