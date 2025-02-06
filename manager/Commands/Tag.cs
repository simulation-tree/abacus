using Collections;
using System;
using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct Tag : ICommand
    {
        readonly string ICommand.Name => "tag";
        readonly string ICommand.Description => "Tags all projects and pushed it to remote";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            if (arguments.IsEmpty)
            {
                runner.WriteErrorLine("Arguments are empty, expected a tag with a branch name separated by a space");
                return;
            }

            if (arguments.Count != 2)
            {
                runner.WriteErrorLine("A tag name then branch name were expected");
                return;
            }

            USpan<char> tag = arguments[0];
            USpan<char> branchName = arguments[1];
            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                Terminal.Execute(repository.Path, $"git tag {tag.ToString()} {branchName.ToString()}");
                Terminal.Execute(repository.Path, $"git push origin tag {tag.ToString()}");
                repository.Dispose();
            }
        }
    }
}