using Abacus.Manager.Constants;
using Collections;
using Collections.Generic;
using System;
using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct Generate : ICommand
    {
        readonly string ICommand.Name => "gen";
        readonly string ICommand.Description => $"Generates files (--gh-test-workflow, --gh-publish-workflow, --clone-script --uml)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            Branch branch = default;
            if (arguments.Contains("--gh-test-workflow"))
            {
                branch |= Branch.GitHubTestWorkflow;
            }

            if (arguments.Contains("--gh-publish-workflow"))
            {
                branch |= Branch.GitHubPublishWorkflow;
            }

            if (arguments.Contains("--clone-script"))
            {
                branch |= Branch.CloneScript;
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
                        GenerateGitHubPublishWorkflow(repository, repositories);
                    }
                }

                if ((branch & Branch.CloneScript) == Branch.CloneScript)
                {
                    GenerateCloneScript(runner, repositories);
                }

                foreach (Repository repository in repositories)
                {
                    repository.Dispose();
                }
            }
            else
            {
                runner.WriteErrorLine("No generation branch was selected");
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
            string source = GetSource(GitHubWorkflowTemplate.TestSource, repository, repositories, false);
            System.IO.File.WriteAllText(filePath, source);
        }

        private static void GenerateGitHubPublishWorkflow(Repository repository, Array<Repository> repositories)
        {
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

            string filePath = System.IO.Path.Combine(workflowsFolder, "publish.yml");
            string source = GetSource(GitHubWorkflowTemplate.PublishSource, repository, repositories, true);
            System.IO.File.WriteAllText(filePath, source);
        }

        private static void GenerateCloneScript(Runner runner, Array<Repository> repositories)
        {
            string solutionFolder = System.IO.Path.GetDirectoryName(runner.SolutionPath.ToString()) ?? string.Empty;
            string cloneScript = System.IO.Path.Combine(solutionFolder, "clone-dependencies.bat");
            using Text builder = new();
            builder.Append("cd ..");
            builder.Append('\n');
            foreach (Repository repository in repositories)
            {
                builder.Append("git clone ");
                builder.Append(repository.Remote.ToString());
                builder.Append('\n');
            }

            System.IO.File.WriteAllText(cloneScript, builder.ToString());
        }

        private static string GetSource(string source, Repository repository, Array<Repository> repositories, bool release)
        {
            bool containsTestProject = false;
            foreach (Project project in repository.Projects)
            {
                if (project.isTestProject)
                {
                    containsTestProject = true;
                    break;
                }
            }

            source = source.TrimStart('\r');
            source = source.TrimStart('\n');
            using Array<Repository> dependencies = GetDependencies(repository, repositories);
            int indent = GetIndentation(source, "{{CheckoutDependenciesStep}}");
            source = source.Replace("{{CheckoutDependenciesStep}}", GetIndented(GetCheckoutDependencies(dependencies), indent));
            if (containsTestProject)
            {
                source = source.Replace("{{TestStep}}", GetIndented(GitHubWorkflowTemplate.TestStep, indent));
            }
            else
            {
                source = source.Replace("{{TestStep}}", string.Empty);
            }

            if (source.Contains("{{ReportStep}}"))
            {
                source = source.Replace("{{ReportStep}}", GetIndented(GitHubWorkflowTemplate.ReportStep, indent));
            }

            if (source.Contains("{{SetupStep}}"))
            {
                source = source.Replace("{{SetupStep}}", GetIndented(GitHubWorkflowTemplate.SetupStep, indent));
            }

            if (source.Contains("{{BuildProjectsStep}}"))
            {
                source = source.Replace("{{BuildProjectsStep}}", GetIndented(GetBuildSteps(repository), indent));
            }

            if (source.Contains("{{PackProjectsStep}}"))
            {
                source = source.Replace("{{PackProjectsStep}}", GetIndented(GetPackSteps(repository), indent));
            }

            if (source.Contains("{{PublishProjectsStep}}"))
            {
                source = source.Replace("{{PublishProjectsStep}}", GetIndented(GetPublishSteps(repository), indent));
            }

            if (source.Contains("{{BuildMode}}"))
            {
                source = source.Replace("{{BuildMode}}", release ? "Release" : "Debug");
            }

            source = source.Replace("{{DotNetVersion}}", "'9.0.x'");
            source = source.Replace("{{RepositoryName}}", repository.Name.ToString());
            return source;
        }

        private static Array<Repository> GetDependencies(Repository repository, Array<Repository> repositories)
        {
            using List<Repository> referencedRepositories = new();
            foreach (Project project in repository.Projects)
            {
                foreach (Project.ProjectReference projectReference in project.ProjectReferences)
                {
                    string projectName = System.IO.Path.GetFileNameWithoutExtension(projectReference.Include.ToString());
                    Repository foundRepository = default;
                    foreach (Repository otherRepository in repositories)
                    {
                        for (int w = 0; w < otherRepository.Projects.Length; w++)
                        {
                            Project otherProject = otherRepository.Projects[w];
                            if (otherProject.Name.SequenceEqual(projectName))
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
                using Stack<Repository> stack = new(referencedRepositories.Count);
                stack.PushRange(referencedRepositories.AsSpan());
                while (stack.TryPop(out Repository current))
                {
                    foreach (Project currentProject in current.Projects)
                    {
                        foreach (Project.ProjectReference currentProjectReference in currentProject.ProjectReferences)
                        {
                            string projectName = System.IO.Path.GetFileNameWithoutExtension(currentProjectReference.Include.ToString());
                            Repository foundRepository = default;
                            foreach (Repository otherRepository in repositories)
                            {
                                for (int w = 0; w < otherRepository.Projects.Length; w++)
                                {
                                    Project otherProject = otherRepository.Projects[w];
                                    if (otherProject.Name.SequenceEqual(projectName))
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
                                if (referencedRepositories.TryAdd(foundRepository))
                                {
                                    stack.Push(foundRepository);
                                }
                            }
                        }
                    }
                }

                return new(referencedRepositories.AsSpan());
            }
            else
            {
                return new();
            }
        }

        private static string GetCheckoutDependencies(Array<Repository> dependencies)
        {
            if (dependencies.Length > 0)
            {
                string text = string.Empty;
                foreach (Repository dependency in dependencies)
                {
                    string source = GitHubWorkflowTemplate.CheckoutStep;
                    source = source.Replace("{{RepositoryName}}", dependency.Name.ToString());
                    source = source.Replace("{{OrganizationName}}", Constant.Get<OrganizationName>().ToString());
                    text += source + '\n';
                }

                return text.TrimEnd('\n');
            }
            else
            {
                return string.Empty;
            }
        }

        private static string GetBuildSteps(Repository repository)
        {
            string text = string.Empty;
            foreach (Project project in repository.Projects)
            {
                text += GetSource(GitHubWorkflowTemplate.BuildStep, project) + '\n';
            }

            return text.TrimEnd('\n');
        }

        private static string GetPackSteps(Repository repository)
        {
            string text = string.Empty;
            foreach (Project project in repository.Projects)
            {
                if (!project.isTestProject)
                {
                    text += GetSource(GitHubWorkflowTemplate.PackStep, project) + '\n';
                }
            }

            return text.TrimEnd('\n');
        }

        private static string GetPublishSteps(Repository repository)
        {
            string text = string.Empty;
            foreach (Project project in repository.Projects)
            {
                if (!project.isTestProject)
                {
                    text += GetSource(GitHubWorkflowTemplate.PublishStep, project) + '\n';
                }
            }

            return text.TrimEnd('\n');
        }

        private static string GetSource(string source, Project project)
        {
            if (source.Contains("{{ProjectName}}"))
            {
                source = source.Replace("{{ProjectName}}", project.Name.ToString());
            }

            if (source.Contains("{{ProjectFolderName}}"))
            {
                string folderName = System.IO.Path.GetFileNameWithoutExtension(project.Directory.ToString());
                source = source.Replace("{{ProjectFolderName}}", folderName);
            }

            if (source.Contains("{{PackageID}}"))
            {
                Text.Borrowed packageId = project.PackageId;
                if (packageId.IsEmpty)
                {
                    packageId.CopyFrom(project.Name);
                }

                source = source.Replace("{{PackageID}}", packageId.ToString());
            }

            return source;
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