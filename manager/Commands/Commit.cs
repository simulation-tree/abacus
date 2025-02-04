using Collections;

namespace Abacus.Manager.Commands
{
    public readonly struct Commit : ICommand
    {
        readonly string ICommand.Name => "commit";
        readonly string ICommand.Description => "Creates a commit for all projects";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            if (arguments.IsEmpty)
            {
                runner.WriteErrorLine("A commit message is expected as a parameter");
                return;
            }

            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, $"git commit -m \"{arguments.ToString()}\"");
                repository.Dispose();
            }
        }
    }
}