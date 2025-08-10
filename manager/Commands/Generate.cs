using Abacus.Manager.Constants;
using Collections.Generic;
using System;
using System.IO;
using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct Generate : ICommand
    {
        readonly string ICommand.Name => "generate";
        readonly string ICommand.Description => $"Generates files (--gh-test-workflow, --gh-publish-workflow, --clone-script --build-targets)";

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

            if (arguments.Contains("--build-targets"))
            {
                branch |= Branch.Targets;
            }

            if (branch != default)
            {
                if ((branch & Branch.CloneScript) == Branch.CloneScript)
                {
                    GenerateCloneScript(runner);
                }

                if ((branch & Branch.Targets) == Branch.Targets || (branch & Branch.GitHubTestWorkflow) == Branch.GitHubTestWorkflow || (branch & Branch.GitHubPublishWorkflow) == Branch.GitHubPublishWorkflow)
                {
                    using Array<Repository> repositories = runner.GetRepositories();
                    Span<Repository> repositoriesSpan = repositories.AsSpan();
                    foreach (Repository repository in repositoriesSpan)
                    {
                        if ((branch & Branch.GitHubTestWorkflow) == Branch.GitHubTestWorkflow)
                        {
                            GenerateGitHubTestWorkflow(repository, repositoriesSpan);
                        }

                        if ((branch & Branch.GitHubPublishWorkflow) == Branch.GitHubPublishWorkflow)
                        {
                            GenerateGitHubPublishWorkflow(repository, repositoriesSpan);
                        }
                    }

                    if ((branch & Branch.Targets) == Branch.Targets)
                    {
                        GenerateTargetsFile(runner, repositoriesSpan);
                    }

                    foreach (Repository repository in repositoriesSpan)
                    {
                        repository.Dispose();
                    }
                }
            }
            else
            {
                runner.WriteErrorLine("No generation branch was selected");
            }
        }

        private static void GenerateGitHubTestWorkflow(Repository repository, ReadOnlySpan<Repository> repositories)
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
            string githubFolder = Path.Combine(rootFolder, ".github");
            if (!Directory.Exists(githubFolder))
            {
                Directory.CreateDirectory(githubFolder);
            }

            string workflowsFolder = Path.Combine(githubFolder, "workflows");
            if (!Directory.Exists(workflowsFolder))
            {
                Directory.CreateDirectory(workflowsFolder);
            }

            string filePath = Path.Combine(workflowsFolder, "test.yml");
            string source = GetSource(repository, repositories, BuildMode.Debug, Workflow.Test);
            File.WriteAllText(filePath, source);
        }

        private static void GenerateGitHubPublishWorkflow(Repository repository, ReadOnlySpan<Repository> repositories)
        {
            string rootFolder = repository.Path.ToString();
            string githubFolder = Path.Combine(rootFolder, ".github");
            if (!Directory.Exists(githubFolder))
            {
                Directory.CreateDirectory(githubFolder);
            }

            string workflowsFolder = Path.Combine(githubFolder, "workflows");
            if (!Directory.Exists(workflowsFolder))
            {
                Directory.CreateDirectory(workflowsFolder);
            }

            string filePath = Path.Combine(workflowsFolder, "publish.yml");
            string source = GetSource(repository, repositories, BuildMode.Release, Workflow.Publish);
            File.WriteAllText(filePath, source);
        }

        private static void GenerateCloneScript(Runner runner)
        {
            string solutionPath = runner.SolutionPath.ToString();
            string solutionDirectory = Path.GetDirectoryName(solutionPath) ?? string.Empty;
            string cloneScript = Path.Combine(solutionDirectory, "clone-dependencies.bat");
            using List<long> repositoryHashes = new();
            if (Repository.TryGetRepository(runner.SolutionPath, out Repository solutionRepository))
            {
                repositoryHashes.Add(solutionRepository.Remote.GetLongHashCode());
            }

            using Solution solution = new(solutionPath);
            using Text builder = new();
            builder.Append("cd ..");
            builder.Append('\n');
            foreach (Project project in solution.Projects)
            {
                if (Repository.TryGetRepository(project.Directory, out Repository repository))
                {
                    long hash = repository.Remote.GetLongHashCode();
                    if (repositoryHashes.TryAdd(hash))
                    {
                        builder.Append("git clone ");
                        builder.Append(repository.Remote.ToString());
                        builder.Append('\n');
                    }

                    repository.Dispose();
                }
            }

            File.WriteAllText(cloneScript, builder.ToString());
        }

        private static void GenerateTargetsFile(Runner runner, ReadOnlySpan<Repository> repositories)
        {
            foreach (Repository repository in repositories)
            {
                foreach (Project project in repository.Projects)
                {
                    if (!project.isTestProject && project.SourceFiles > 0 && !project.Name.Contains("Generator", StringComparison.Ordinal))
                    {
                        string buildFolder = Path.Combine(project.Directory.ToString(), "build");
                        if (Directory.Exists(buildFolder))
                        {
                            Directory.Delete(buildFolder, true);
                        }

                        buildFolder = Path.Combine(project.Directory.ToString(), "buildTransitive");
                        if (!Directory.Exists(buildFolder))
                        {
                            Directory.CreateDirectory(buildFolder);
                        }

                        string packageId = project.PackageId.ToString();
                        string projectName = project.Name.ToString();
                        if (string.IsNullOrEmpty(packageId))
                        {
                            packageId = projectName;
                        }

                        string targetsPath = Path.Combine(buildFolder, packageId + ".targets");
                        string content = EmbeddedResources.Get("targets.xml") ?? throw new("Targets template is missing");
                        content = content.Replace("{{ProjectName}}", projectName);
                        content = content.Replace("{{PackageId}}", packageId);
                        File.WriteAllText(targetsPath, content);
                    }
                }
            }
        }

        private static string GetSource(Repository repository, ReadOnlySpan<Repository> repositories, BuildMode buildMode, Workflow workflow)
        {
            using List<SemanticVersion> dotNetVersions = new();
            TargetFramework latestTargetFramework = default;
            string source = workflow == Workflow.Test ? GitHubWorkflowTemplate.TestSource : GitHubWorkflowTemplate.PublishSource;
            bool containsTestProject = false;
            foreach (Project project in repository.Projects)
            {
                if (project.isTestProject)
                {
                    containsTestProject = true;
                    if (workflow == Workflow.Test)
                    {
                        foreach (TargetFramework targetFramework in project.TargetFrameworks)
                        {
                            if (targetFramework.IsDotNet)
                            {
                                dotNetVersions.TryAdd(targetFramework.GetSemanticVersion());
                                if (latestTargetFramework == default || targetFramework > latestTargetFramework)
                                {
                                    latestTargetFramework = targetFramework;
                                }
                            }
                            else
                            {
                                // ignore non dotnet version because the only other version in use is netstandard2.0 for source generators
                            }
                        }
                    }
                }
                else
                {
                    if (workflow == Workflow.Publish)
                    {
                        foreach (TargetFramework targetFramework in project.TargetFrameworks)
                        {
                            if (targetFramework.IsDotNet)
                            {
                                dotNetVersions.TryAdd(targetFramework.GetSemanticVersion());
                                if (latestTargetFramework == default || targetFramework > latestTargetFramework)
                                {
                                    latestTargetFramework = targetFramework;
                                }
                            }
                            else
                            {
                                // ignore non dotnet version because the only other version in use is netstandard2.0 for source generators
                            }
                        }
                    }
                }
            }

            if (dotNetVersions.Count == 0)
            {
                throw new InvalidOperationException($"No .NET versions recognized in any projects of `{repository.Name}` repository");
            }

            source = source.TrimStart('\r');
            source = source.TrimStart('\n');
            using Array<Repository> dependencies = GetDependencies(repository, repositories);
            int indent = GetIndentation(source, "{{CheckoutDependenciesStep}}");
            source = source.Replace("{{CheckoutDependenciesStep}}", GetIndented(GetCheckoutDependencies(dependencies), indent));
            if (containsTestProject)
            {
                SemanticVersion latestDotNetVersion = dotNetVersions[0];
                foreach (SemanticVersion version in dotNetVersions)
                {
                    if (version > latestDotNetVersion)
                    {
                        latestDotNetVersion = version;
                    }
                }

                string testStep = GetIndented(GitHubWorkflowTemplate.TestStep, indent);
                if (dotNetVersions.Count > 1)
                {
                    testStep += $" --framework {latestTargetFramework}";
                }

                source = source.Replace("{{TestStep}}", testStep);
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
                using Text setupStep = new(GetIndented(GitHubWorkflowTemplate.SetupStep, indent));
                if (dotNetVersions.Count == 1)
                {
                    setupStep.Append(' ');
                    setupStep.Append(dotNetVersions[0].ToString());
                }
                else
                {
                    setupStep.Append(" |");
                    setupStep.AppendLine();
                    for (int i = 0; i < dotNetVersions.Count; i++)
                    {
                        if (i > 0)
                        {
                            setupStep.AppendLine();
                        }

                        setupStep.Append(' ', indent + 6);
                        setupStep.Append(dotNetVersions[i].ToString());
                    }
                }

                source = source.Replace("{{SetupStep}}", setupStep.ToString());
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
                source = source.Replace("{{BuildMode}}", buildMode.ToString());
            }

            source = source.Replace("{{RepositoryName}}", repository.Name.ToString());
            return source;
        }

        private static Array<Repository> GetDependencies(Repository repository, ReadOnlySpan<Repository> repositories)
        {
            using List<Repository> referencedRepositories = new();
            foreach (Project project in repository.Projects)
            {
                foreach (Project.ProjectReference projectReference in project.ProjectReferences)
                {
                    string projectName = Path.GetFileNameWithoutExtension(projectReference.Include.ToString());
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
                            string projectName = Path.GetFileNameWithoutExtension(currentProjectReference.Include.ToString());
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
                text += GetSource(GitHubWorkflowTemplate.BuildStep, project).Replace("{{BuildMode}}", "Debug") + '\n';
                text += GetSource(GitHubWorkflowTemplate.BuildStep, project).Replace("{{BuildMode}}", "Release") + '\n';
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
                string folderName = Path.GetFileNameWithoutExtension(project.Directory.ToString());
                source = source.Replace("{{ProjectFolderName}}", folderName);
            }

            if (source.Contains("{{PackageId}}"))
            {
                Text.Borrowed packageId = project.PackageId;
                if (packageId.IsEmpty)
                {
                    packageId.CopyFrom(project.Name);
                }

                source = source.Replace("{{PackageId}}", packageId.ToString());
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
            CloneScript = 4,
            Targets = 8
        }
    }
}