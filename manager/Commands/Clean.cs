using System;
using System.Collections.Generic;
using System.IO;
using static Functions;

public readonly struct Clean : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "clean";
    ReadOnlySpan<char> ICommand.Description => "Cleans project of artifacts (--source-control, --ide, --test-results, --builds)";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        bool sourceControlArtifacts = false;
        if (arguments.IndexOf("--source-control") != -1)
        {
            sourceControlArtifacts = true;
        }

        bool ideArtifacts = false;
        if (arguments.IndexOf("--ide") != -1)
        {
            ideArtifacts = true;
        }

        bool testResults = false;
        if (arguments.IndexOf("--test-results") != -1)
        {
            testResults = true;
        }

        bool builds = false;
        if (arguments.IndexOf("--builds") != -1)
        {
            builds = true;
        }

        if (!sourceControlArtifacts && !ideArtifacts && !testResults && !builds)
        {
            Console.WriteLine("At least one clean filter is requried: --source-control, --ide, --test-results, --builds");
            return;
        }

        foreach (Project project in GetProjects(workingDirectory))
        {
            Stack<string> stack = new();
            string folder = Path.GetDirectoryName(project.Path.ToString()) ?? throw new();
            stack.Push(folder);

            while (stack.Count > 0)
            {
                string currentFolder = stack.Pop();
                string folderName = Path.GetFileName(currentFolder);
                if (sourceControlArtifacts)
                {
                    if (folderName == ".github" || folderName == ".git")
                    {
                        DeleteDirectory(currentFolder);
                        continue;
                    }

                    string gitIgnore = Path.Combine(currentFolder, ".gitignore");
                    DeleteFile(gitIgnore);
                }

                if (ideArtifacts)
                {
                    if (folderName == ".vs" || folderName == ".idea" || folderName == ".vscode")
                    {
                        DeleteDirectory(currentFolder);
                        continue;
                    }
                }

                if (testResults)
                {
                    if (folderName == "TestResults")
                    {
                        DeleteDirectory(currentFolder);
                        continue;
                    }
                }

                if (builds)
                {
                    if (folderName == "bin" || folderName == "obj")
                    {
                        DeleteDirectory(currentFolder);
                        continue;
                    }
                }

                foreach (string directory in Directory.GetDirectories(currentFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    stack.Push(directory);
                }
            }
        }
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
            Console.WriteLine($"Deleted directory `{path}`");
        }
        catch { }
    }

    private static void DeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            Console.WriteLine($"Deleted file `{path}`");
        }
        catch { }
    }
}