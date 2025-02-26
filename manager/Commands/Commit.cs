using Collections.Generic;
using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct Commit : ICommand
    {
        readonly string ICommand.Name => "commit";
        readonly string ICommand.Description => "Creates a commit for all projects";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            if (arguments.Count != 1)
            {
                runner.WriteErrorLine("A commit message is expected as a parameter");
                return;
            }


            USpan<char> message = arguments.RawText;
            if (message.StartsWith('"') && message.EndsWith('"'))
            {
                message = message.Slice(1, message.Length - 2);
            }

            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, $"git commit -a -m \"{message.ToString()}\"");
                repository.Dispose();
            }
        }
    }
}