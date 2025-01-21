using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

public static class Functions
{
    public static ReadOnlySpan<char> GetSolutionPath()
    {
        string? path = Environment.CurrentDirectory;
        string? lastSolutionPath = null;
        while (path is not null)
        {
            string[] files = Directory.GetFiles(path, "*.sln");
            if (files.Length > 0)
            {
                lastSolutionPath = files[0];
            }

            path = Directory.GetParent(path)?.FullName;
        }

        return lastSolutionPath ?? throw new Exception("Solution file not found");
    }

    public static IEnumerable<Project> GetProjects(ReadOnlySpan<char> rootDirectory)
    {
        return GetProjects(rootDirectory.ToString());
    }

    public static IEnumerable<Project> GetProjects(string rootDirectory)
    {
        Stack<string> stack = new();
        stack.Push(rootDirectory);

        Dictionary<string, Project> projectFiles = new();
        while (stack.Count > 0)
        {
            string currentDirectory = stack.Pop();
            foreach (string directory in Directory.GetDirectories(currentDirectory))
            {
                stack.Push(directory);
            }

            foreach (string file in Directory.GetFiles(currentDirectory))
            {
                if (Path.GetExtension(file) == ".csproj")
                {
                    Project project = new(file);
                    string projectName = project.Name.ToString();
                    projectFiles.Add(projectName, project);
                }
            }
        }

        List<(string dependent, List<string> prerequisites)> dependencies = new();
        foreach (Project project in projectFiles.Values)
        {
            List<string> prerequisites = new();
            string projectName = project.Name.ToString();
            foreach (Project.ProjectReference projectReference in project.ProjectReferences)
            {
                string referencedProjectName = Path.GetFileNameWithoutExtension(projectReference.Include.ToString());
                if (projectFiles.ContainsKey(referencedProjectName))
                {
                    prerequisites.Add(referencedProjectName);
                }
                else
                {
                    throw new Exception($"Project `{projectName}` references project `{referencedProjectName}` which does not exist");
                }
            }

            dependencies.Add((projectName, prerequisites));
        }

        List<string> sorted = TopologicalSortItems(projectFiles.Keys, dependencies);
        foreach (string projectName in sorted)
        {
            yield return projectFiles[projectName];
        }
    }

    public static string? Call(ReadOnlySpan<char> command)
    {
        ProcessStartInfo startInfo = new();
        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C {command.ToString()}";
        }
        else if (OperatingSystem.IsLinux())
        {
            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = command.ToString();
        }
        else
        {
            throw new Exception($"Unsupported operating system `{Environment.OSVersion}`");
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        StringBuilder output = new();
        StringBuilder error = new();
        using AutoResetEvent outputWaitHandle = new(false);
        using AutoResetEvent errorWaitHandle = new(false);
        using Process? process = Process.Start(startInfo);
        if (process is not null)
        {
            Console.WriteLine($"{startInfo.FileName} {startInfo.Arguments}");
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is null)
                {
                    try
                    {
                        outputWaitHandle.Set();
                    }
                    catch { }
                }
                else
                {
                    output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is null)
                {
                    try
                    {
                        errorWaitHandle.Set();
                    }
                    catch { }
                }
                else
                {
                    error.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            const int TimeoutMS = 20000;
            if (process.WaitForExit(TimeoutMS) && outputWaitHandle.WaitOne(TimeoutMS) && errorWaitHandle.WaitOne(TimeoutMS))
            {
                if (!string.IsNullOrEmpty(error.ToString()))
                {
                    return error.ToString();
                }
                else
                {
                    return output.ToString();
                }
            }
            else
            {
                throw new Exception("Program timed out");
            }
        }
        else
        {
            return null;
        }
    }

    public static List<string> TopologicalSortItems(IReadOnlyCollection<string> items, List<(string dependent, List<string> prerequisites)> dependencies)
    {
        Dictionary<string, List<string>> graph = new();
        Dictionary<string, int> inDegree = new();

        foreach (string item in items)
        {
            graph[item] = new();
            inDegree[item] = 0;
        }

        foreach (var dependency in dependencies)
        {
            string dependent = dependency.dependent;
            List<string> prerequisites = dependency.prerequisites;
            foreach (string prerequisite in prerequisites)
            {
                graph[prerequisite].Add(dependent);
                inDegree[dependent]++;
            }
        }

        Queue<string> queue = new();
        foreach (string item in items)
        {
            if (inDegree[item] == 0)
            {
                queue.Enqueue(item);
            }
        }

        List<string> sortedOrder = new();
        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            sortedOrder.Add(current);

            foreach (string neighbor in graph[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (sortedOrder.Count != items.Count)
        {
            throw new InvalidOperationException("Cycle detected! Topological sorting not possible");
        }

        return sortedOrder;
    }
}