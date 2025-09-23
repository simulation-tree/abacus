using Collections.Generic;
using System.IO;

namespace Abacus.Manager.Commands
{
    public readonly struct Clean : ICommand
    {
        readonly string ICommand.Name => "clean";
        readonly string ICommand.Description => "Cleans project of artifacts (--source-control, --ide, --test-results, --builds, --meta --nuget)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            bool sourceControlArtifacts = false;
            if (arguments.Contains("--source-control"))
            {
                sourceControlArtifacts = true;
            }

            bool ideArtifacts = false;
            if (arguments.Contains("--ide"))
            {
                ideArtifacts = true;
            }

            bool testResults = false;
            if (arguments.Contains("--test-results"))
            {
                testResults = true;
            }

            bool builds = false;
            if (arguments.Contains("--builds"))
            {
                builds = true;
            }

            bool meta = false;
            if (arguments.Contains("--meta"))
            {
                meta = true;
            }

            bool nuget = false;
            if (arguments.Contains("--nuget"))
            {
                nuget = true;
            }

            if (!sourceControlArtifacts && !ideArtifacts && !testResults && !builds && !meta && !nuget)
            {
                runner.WriteErrorLine("At least one clean filter is required: --source-control, --ide, --test-results, --builds, --meta, --nuget");
                return;
            }

            using Array<Repository> repositories = runner.GetRepositories();
            System.Collections.Generic.Stack<string> stack = new();
            foreach (Repository repository in repositories)
            {
                stack.Push(repository.Path.ToString());
                while (stack.Count > 0)
                {
                    string currentFolder = stack.Pop();
                    string folderName = Path.GetFileName(currentFolder);
                    if (sourceControlArtifacts)
                    {
                        if (folderName == ".github" || folderName == ".git")
                        {
                            DeleteDirectory(runner, currentFolder);
                            continue;
                        }

                        string gitIgnore = Path.Combine(currentFolder, ".gitignore");
                        DeleteFile(runner, gitIgnore);
                    }

                    if (ideArtifacts)
                    {
                        if (folderName == ".vs" || folderName == ".idea" || folderName == ".vscode")
                        {
                            DeleteDirectory(runner, currentFolder);
                            continue;
                        }
                    }

                    if (testResults)
                    {
                        if (folderName == "TestResults")
                        {
                            DeleteDirectory(runner, currentFolder);
                            continue;
                        }
                    }

                    if (builds)
                    {
                        if (folderName == "bin" || folderName == "obj")
                        {
                            DeleteDirectory(runner, currentFolder);
                            continue;
                        }
                    }

                    if (meta)
                    {
                        string[] metaFiles = Directory.GetFiles(currentFolder, "*.meta", SearchOption.TopDirectoryOnly);
                        foreach (string metaFile in metaFiles)
                        {
                            DeleteFile(runner, metaFile);
                        }
                    }

                    if (nuget)
                    {
                        string[] nupkgFiles = Directory.GetFiles(currentFolder, "*.nupkg", SearchOption.TopDirectoryOnly);
                        foreach (string nupkgFile in nupkgFiles)
                        {
                            DeleteFile(runner, nupkgFile);
                        }
                    }

                    foreach (string directory in Directory.GetDirectories(currentFolder, "*", SearchOption.TopDirectoryOnly))
                    {
                        stack.Push(directory);
                    }
                }

                repository.Dispose();
            }
        }

        private static void DeleteDirectory(Runner runner, string path)
        {
            try
            {
                Directory.Delete(path, true);
                runner.WriteInfoLine($"Deleted directory `{path}`");
            }
            catch { }
        }

        private static void DeleteFile(Runner runner, string path)
        {
            try
            {
                File.Delete(path);
                runner.WriteInfoLine($"Deleted file `{path}`");
            }
            catch { }
        }
    }
}