using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Abacus.Manager.Commands
{
    public readonly struct Generate : ICommand
    {
        readonly string ICommand.Name => "gen";
        readonly string ICommand.Description => $"Generates files (--gh-test-workflow, --gh-publish-workflow, --clone-script)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            Branch branch = default;
            if (arguments.Contains("--gh-test-workflow"))
            {
                branch = Branch.GitHubTestWorkflow;
            }

            if (arguments.Contains("--gh-publish-workflow"))
            {
                branch = Branch.GitHubPublishWorkflow;
            }

            if (arguments.Contains("--clone-script"))
            {
                branch = Branch.CloneScript;
            }

            if (branch != default)
            {
                HashSet<string> projectFolders = new();
                foreach (string projectPath in projectFolders)
                {
                    if ((branch & Branch.GitHubTestWorkflow) == Branch.GitHubTestWorkflow)
                    {
                        Trace.WriteLine("Generating GitHub test workflow");
                    }

                    if ((branch & Branch.GitHubPublishWorkflow) == Branch.GitHubPublishWorkflow)
                    {
                        Trace.WriteLine("Generating GitHub publish workflow");
                    }

                    if ((branch & Branch.CloneScript) == Branch.CloneScript)
                    {
                        Trace.WriteLine("Generating clone script");
                    }
                }
            }
            else
            {
                Trace.WriteLine("No generation branch was selected");
            }
        }

        private static void GenerateGitHubTestWorkflow()
        {

        }

        [Flags]
        public enum Branch : byte
        {
            Unknown = 0,
            GitHubTestWorkflow = 1,
            GitHubPublishWorkflow = 2,
            CloneScript = 4
        }
    }
}