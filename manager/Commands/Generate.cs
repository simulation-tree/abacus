using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

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
                using Array<Repository> repositories = runner.GetRepositories();
                foreach (Repository repository in repositories)
                {
                    if ((branch & Branch.GitHubTestWorkflow) == Branch.GitHubTestWorkflow)
                    {
                        GenerateGitHubTestWorkflow(repository, repositories);
                    }

                    if ((branch & Branch.GitHubPublishWorkflow) == Branch.GitHubPublishWorkflow)
                    {
                        GenerateGitHubPublishWorkflow(repository);
                    }
                }

                foreach (Repository repository in repositories)
                {
                    repository.Dispose();
                }
            }
            else
            {
                Trace.WriteLine("No generation branch was selected");
            }
        }

        private static void GenerateGitHubTestWorkflow(Repository repository, Array<Repository> repositories)
        {
            //check if theres any test project present
            bool hasTestProject = false;
            foreach (Project project in repository.Projects)
            {
                if (project.isTestProject)
                {
                    hasTestProject = true;
                    break;
                }
            }

            if (!hasTestProject)
            {
                return;
            }

            string rootFolder = repository.Path.ToString();
            string githubFolder = System.IO.Path.Combine(rootFolder, ".github");
            if (!System.IO.Directory.Exists(githubFolder))
            {
                System.IO.Directory.CreateDirectory(githubFolder);
            }

            string workflowsFolder = System.IO.Path.Combine(githubFolder, "workflows");
            if (!System.IO.Directory.Exists(workflowsFolder))
            {
                System.IO.Directory.CreateDirectory(workflowsFolder);
            }

            string filePath = System.IO.Path.Combine(workflowsFolder, "test.yml");
            string source = GitHubWorkflowTemplate.Source.TrimStart('\r', '\n');
            int indent = GetIndentation(source, "{{CheckoutDependenciesStep}}");
            string checkoutDependencies = GetCheckoutDependencies(repository, repositories);
            source = source.Replace("{{CheckoutDependenciesStep}}", GetIndented(checkoutDependencies, indent));
            source = source.Replace("{{TestStep}}", GetIndented(GitHubWorkflowTemplate.TestStep, indent));
            source = source.Replace("{{ReportStep}}", GetIndented(GitHubWorkflowTemplate.ReportStep, indent));
            source = source.Replace("{{SetupStep}}", GetIndented(GitHubWorkflowTemplate.SetupStep, indent));
            source = source.Replace("{{BuildMode}}", "Debug");
            source = source.Replace("{{DotNetVersion}}", "'9.0.x'");
            source = source.Replace("{{RepositoryName}}", repository.Name.ToString());
            System.IO.File.WriteAllText(filePath, source);
        }

        private static void GenerateGitHubPublishWorkflow(Repository repository)
        {
        }

        private static string GetCheckoutDependencies(Repository repository, Array<Repository> repositories)
        {
            using List<Repository> referencedRepositories = new();
            for (uint x = 0; x < repository.Projects.Length; x++)
            {
                Project project = repository.Projects[x];
                for (uint y = 0; y < project.ProjectReferences.Length; y++)
                {
                    Project.ProjectReference projectReference = project.ProjectReferences[y];
                    string projectName = System.IO.Path.GetFileNameWithoutExtension(projectReference.Include.ToString());
                    Repository foundRepository = default;
                    for (uint z = 0; z < repositories.Length; z++)
                    {
                        Repository otherRepository = repositories[z];
                        for (uint w = 0; w < otherRepository.Projects.Length; w++)
                        {
                            Project otherProject = otherRepository.Projects[w];
                            if (otherProject.Name.SequenceEqual(projectName.AsSpan()))
                            {
                                foundRepository = otherRepository;
                                break;
                            }
                        }

                        if (foundRepository != default)
                        {
                            break;
                        }
                    }

                    if (foundRepository != default && repository != foundRepository)
                    {
                        referencedRepositories.TryAdd(foundRepository);
                    }
                }
            }

            if (referencedRepositories.Count > 0)
            {
                string text = string.Empty;
                foreach (Repository projectReference in referencedRepositories)
                {
                    string source = GitHubWorkflowTemplate.CheckoutStep;
                    source = source.Replace("{{RepositoryName}}", projectReference.Name.ToString());
                    text += source + '\n';
                }

                return text.TrimEnd('\n');
            }
            else
            {
                return string.Empty;
            }
        }

        private static int GetIndentation(string text, string keyword)
        {
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int index = line.IndexOf(keyword);
                if (index >= 0)
                {
                    return index;
                }
            }

            return 0;
        }

        private static string GetIndented(string text, int indent)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string prefix = new(' ', indent);
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = prefix + lines[i];
            }

            return string.Join('\n', lines);
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