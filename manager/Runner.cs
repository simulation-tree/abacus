using Collections.Generic;
using System;
using System.IO;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Runner : IDisposable
    {
        private readonly Text workingDirectory;
        private readonly Text solutionPath;
        private readonly Text logText;
        private readonly List<LogMessage> logMessages;

        public readonly USpan<char> WorkingDirectory => workingDirectory.AsSpan();
        public readonly USpan<char> SolutionPath => solutionPath.AsSpan();
        public readonly USpan<LogMessage> LogMessages => logMessages.AsSpan();

        public Runner(USpan<char> workingDirectory, USpan<char> solutionPath)
        {
            this.workingDirectory = new Text(workingDirectory);
            this.solutionPath = new Text(solutionPath);
            logText = new();
            logMessages = new();
        }

        public readonly void Dispose()
        {
            logMessages.Dispose();
            logText.Dispose();
            solutionPath.Dispose();
            workingDirectory.Dispose();
        }

        public readonly LogMessage WriteInfoLine(USpan<char> text)
        {
            return Write(LogMessage.Category.Info, text, true);
        }

        public readonly LogMessage WriteInfoLine(string text)
        {
            return WriteInfoLine(text.AsSpan());
        }

        public readonly LogMessage WriteInfo(USpan<char> text)
        {
            return Write(LogMessage.Category.Info, text, false);
        }

        public readonly LogMessage WriteInfo(string text)
        {
            return WriteInfo(text.AsSpan());
        }

        public readonly LogMessage WriteErrorLine(USpan<char> text)
        {
            return Write(LogMessage.Category.Error, text, true);
        }

        public readonly LogMessage WriteErrorLine(string text)
        {
            return WriteErrorLine(text.AsSpan());
        }

        public readonly LogMessage Write(LogMessage.Category category, USpan<char> text, bool appendLine)
        {
            uint start = logText.Length;
            logText.Append(text);
            uint end = logText.Length;
            LogMessage message = new(logText, category, new(start, end), appendLine);
            logMessages.Add(message);
            return message;
        }

        public readonly void ClearMessages()
        {
            logMessages.Clear();
            logText.Clear();
        }

        public readonly Array<Repository> GetRepositories()
        {
            using Stack<Text> stack = new();
            stack.Push(new(WorkingDirectory));

            using List<Text> foundRepositories = new();
            while (stack.TryPop(out Text currentDirectory))
            {
                foreach (string directory in Directory.GetDirectories(currentDirectory.ToString(), "*", SearchOption.TopDirectoryOnly))
                {
                    if (directory.EndsWith("/.git") || directory.EndsWith("\\.git"))
                    {
                        foundRepositories.Add(new(currentDirectory.AsSpan()));
                    }
                    else
                    {
                        stack.Push(new(directory));
                    }
                }

                currentDirectory.Dispose();
            }

            Array<Repository> repositories = new(foundRepositories.Count);
            for (uint i = 0; i < foundRepositories.Count; i++)
            {
                Text repositoryPath = foundRepositories[i];
                USpan<char> remote = Terminal.Execute(repositoryPath.ToString(), "git remote get-url origin");
                remote = remote.TrimEnd('\n');
                remote = remote.TrimEnd('\r');
                repositories[i] = new(repositoryPath.AsSpan(), remote);
                repositoryPath.Dispose();
            }

            return repositories;
        }

        public readonly Array<Project> GetProjects(bool topologicallySorted = true)
        {
            using Stack<Text> stack = new();
            stack.Push(new(WorkingDirectory));

            using Dictionary<Text, Project> projectFiles = new();
            while (stack.TryPop(out Text currentDirectory))
            {
                foreach (string directory in Directory.GetDirectories(currentDirectory.ToString()))
                {
                    stack.Push(new(directory));
                }

                foreach (string filePath in Directory.GetFiles(currentDirectory.ToString()))
                {
                    if (Path.GetExtension(filePath) == ".csproj")
                    {
                        Project project;
                        try
                        {
                            project = new(filePath);
                        }
                        catch (Exception ex)
                        {
                            WriteErrorLine($"Error reading project file `{filePath}`: {ex.Message}");
                            continue;
                        }

                        Text projectName = new(project.Name);
                        projectFiles.Add(projectName, project);
                    }
                }

                currentDirectory.Dispose();
            }

            if (topologicallySorted)
            {
                using List<(Text dependent, List<Text> prerequisites)> dependencies = new();
                foreach (Project project in projectFiles.Values)
                {
                    List<Text> prerequisites = new();
                    Text projectName = new(project.Name);
                    foreach (Project.ProjectReference projectReference in project.ProjectReferences)
                    {
                        Text referencedProjectName = new(Path.GetFileNameWithoutExtension(projectReference.Include));
                        if (projectFiles.ContainsKey(referencedProjectName))
                        {
                            prerequisites.Add(referencedProjectName);
                        }
                        else
                        {
                            WriteErrorLine($"Project `{projectName}` references project `{referencedProjectName}` which does not exist");
                            continue;
                        }
                    }

                    dependencies.Add((projectName, prerequisites));
                }

                using Array<Text> sorted = TopologicalSortItems(projectFiles.Keys, dependencies);
                Array<Project> projects = new(sorted.Length);
                for (uint i = 0; i < sorted.Length; i++)
                {
                    projects[i] = projectFiles[sorted[i]];
                }

                foreach ((Text dependent, List<Text> prerequisites) entry in dependencies)
                {
                    foreach (Text prerequisite in entry.prerequisites)
                    {
                        prerequisite.Dispose();
                    }

                    entry.prerequisites.Dispose();
                    entry.dependent.Dispose();
                }

                foreach (Text key in projectFiles.Keys)
                {
                    key.Dispose();
                }

                return projects;
            }
            else
            {
                uint i = 0;
                Array<Project> projects = new(projectFiles.Count);
                foreach (Project project in projectFiles.Values)
                {
                    projects[i++] = project;
                }

                foreach (Text key in projectFiles.Keys)
                {
                    key.Dispose();
                }

                return projects;
            }
        }

        public static Array<Text> TopologicalSortItems(System.Collections.Generic.IEnumerable<Text> items, List<(Text dependent, List<Text> prerequisites)> dependencies)
        {
            using Dictionary<Text, List<Text>> graph = new();
            using Dictionary<Text, int> inDegree = new();
            uint itemCount = 0;
            foreach (Text item in items)
            {
                graph.Add(item, new());
                inDegree.Add(item, 0);
                itemCount++;
            }

            foreach ((Text dependent, List<Text> prerequisites) dependency in dependencies)
            {
                Text dependent = dependency.dependent;
                List<Text> prerequisites = dependency.prerequisites;
                foreach (Text prerequisite in prerequisites)
                {
                    graph[prerequisite].Add(dependent);
                    inDegree[dependent]++;
                }
            }

            using Queue<Text> queue = new();
            foreach (Text item in items)
            {
                if (inDegree[item] == 0)
                {
                    queue.Enqueue(item);
                }
            }

            USpan<Text> sortedOrder = stackalloc Text[(int)itemCount * 2];
            uint sortedOrderCount = 0;
            while (queue.TryDequeue(out Text current))
            {
                sortedOrder[sortedOrderCount++] = current;

                foreach (Text neighbor in graph[current])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            foreach ((Text key, List<Text> value) entry in graph)
            {
                entry.value.Dispose();
            }

            if (sortedOrderCount != itemCount)
            {
                throw new InvalidOperationException("Cycle detected! Topological sorting not possible");
            }

            return new(sortedOrder.Slice(0, sortedOrderCount));
        }
    }
}