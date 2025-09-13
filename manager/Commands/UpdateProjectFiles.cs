using Abacus.Manager.Constants;
using Collections.Generic;
using XML;
using System;
using System.IO;
using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct UpdateProjectFiles : ICommand
    {
        readonly string ICommand.Name => "update-csproj";
        readonly string? ICommand.Description => "Updates texts expecting an organization name with the current value";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            ASCIIText256 organizationName = Constant.Get<OrganizationName>();
            ASCIIText256 repositoryHost = Constant.Get<RepositoryHost>();
            using Array<Repository> repositories = runner.GetRepositories();
            foreach (Repository repository in repositories)
            {
                RenameInFiles(runner, repository.Path);

                foreach (Project project in repository.Projects)
                {
                    bool changed = false;
                    if (!project.isGeneratorProject)
                    {
                        // make sure projects use net9 only
                        project.ClearTargetFrameworks();
                        project.AddTargetFramework(TargetFramework.Net9);
                        project.ClearAnalyzers();
                        changed |= true;

                        if (!project.Company.IsEmpty)
                        {
                            project.Company.CopyFrom(organizationName);
                            changed |= true;
                        }

                        if (!project.RepositoryUrl.IsEmpty)
                        {
                            project.RepositoryUrl.CopyFrom($"{repositoryHost}/{organizationName}/{repository.Name}");
                            changed |= true;
                        }

                        if (!project.isTestProject && project.SourceFiles > 0)
                        {
                            if (project.IncludeBuildOutput)
                            {
                                project.IncludeBuildOutput = false;
                                changed |= true;
                            }

                            if (project.SuppressDependenciesWhenPacking)
                            {
                                project.SuppressDependenciesWhenPacking = false;
                                changed |= true;
                            }

                            const string OutDir = "bin/$(TargetFramework)/$(Configuration)";
                            if (!project.OutDir.Equals(OutDir))
                            {
                                project.OutDir.CopyFrom(OutDir);
                                changed |= true;
                            }

                            changed |= EnsureBuildOutputsArePacked(project);
                        }

                        // ensure generator dll analyzers are referenced
                        foreach (Project.Analyzer analyzer in project.Analyzers)
                        {
                            if (analyzer.Include.IndexOf("netstandard2.0") != -1 && analyzer.Include.IndexOf("Generator") != -1 && analyzer.Include.IndexOf(".dll") != -1)
                            {
                                string fullPathToInclude = project.GetFullPath(analyzer.Include);
                                string? directory = Path.GetDirectoryName(fullPathToInclude);
                                string includeProjectPath = string.Empty;
                                while (directory is not null)
                                {
                                    string folderName = Path.GetFileName(directory);
                                    if (folderName != "bin" && folderName != "$(Configuration)" && folderName != "netstandard2.0")
                                    {
                                        string[] projectFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
                                        if (projectFiles.Length > 0)
                                        {
                                            includeProjectPath = projectFiles[0];
                                            break;
                                        }
                                    }

                                    directory = Path.GetDirectoryName(directory);
                                }

                                string pathToProject = project.GetRelativePath(includeProjectPath);
                                bool referencesProject = false;
                                foreach (Project.ProjectReference projectReference in project.ProjectReferences)
                                {
                                    if (projectReference.ReferenceOutputAssembly == false && projectReference.OutputItemType == OutputType.Analyzer)
                                    {
                                        if (projectReference.Include.SequenceEqual(pathToProject))
                                        {
                                            referencesProject = true;
                                            break;
                                        }
                                    }
                                }

                                if (!referencesProject)
                                {
                                    project.AddProjectReference(pathToProject, false, OutputType.Analyzer);
                                    changed |= true;
                                }
                            }
                        }

                        if (changed)
                        {
                            project.WriteToFile();
                            runner.WriteInfoLine($"Updated {project.Path.ToString()}");
                        }
                    }
                }

                repository.Dispose();
            }
        }

        private static bool EnsureBuildOutputsArePacked(Project project)
        {
            bool changed = false;
            const string BinPath = "bin";
            const string BuildTransitivePath = "buildTransitive";
            XMLNode itemGroupNode = default;
            using Stack<(XMLNode, XMLNode)> stack = new();
            stack.Push((default, project.rootNode));
            while (stack.TryPop(out (XMLNode parent, XMLNode current) entry))
            {
                XMLNode current = entry.current;
                if (current.Name.Equals("Content") && current.TryGetAttribute("Include", out ReadOnlySpan<char> include))
                {
                    XMLNode parent = entry.parent;
                    if (include.StartsWith(BinPath) || include.StartsWith(BuildTransitivePath))
                    {
                        itemGroupNode = parent;
                    }
                }
                else
                {
                    foreach (XMLNode child in current.Children)
                    {
                        stack.Push((current, child));
                    }
                }
            }

            if (itemGroupNode == default)
            {
                itemGroupNode = new("ItemGroup");
                project.rootNode.Add(itemGroupNode);
                changed = true;
            }
            else
            {
                changed |= itemGroupNode.Count > 0;
                itemGroupNode.Clear();
            }

            XMLNode packBin = new("Content");
            itemGroupNode.Add(packBin);
            packBin.SetAttribute("Include", BinPath + "/**/*");
            packBin.SetAttribute("Pack", "true");
            packBin.SetAttribute("PackagePath", "lib");
            packBin.SetAttribute("Visible", "false");

            XMLNode packBuildTransitive = new("Content");
            itemGroupNode.Add(packBuildTransitive);
            packBuildTransitive.SetAttribute("Include", BuildTransitivePath + "/**/*");
            packBuildTransitive.SetAttribute("Pack", "true");
            packBuildTransitive.SetAttribute("PackagePath", "buildTransitive");
            return true;
        }

        private static void RenameInFiles(Runner runner, ReadOnlySpan<char> repositoryPath)
        {
            const string URLStart = "https://github.com/";
            const string RepositoryStart = "repository: ";
            const string CopyrightStart = "Copyright (c) ";
            ASCIIText256 organizationName = Constant.Get<OrganizationName>();
            string[] markdownFiles = Directory.GetFiles(repositoryPath.ToString(), "*.md", SearchOption.AllDirectories);
            string[] yamlFiles = Directory.GetFiles(repositoryPath.ToString(), "*.yml", SearchOption.AllDirectories);
            string[] allFiles = new string[markdownFiles.Length + yamlFiles.Length];
            markdownFiles.CopyTo(allFiles, 0);
            yamlFiles.CopyTo(allFiles, markdownFiles.Length);
            foreach (string filePath in allFiles)
            {
                bool isYaml = filePath.EndsWith(".yml");
                bool isLicense = Path.GetFileNameWithoutExtension(filePath) == "LICENSE";
                string[] lines = File.ReadAllLines(filePath);
                bool changed = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int httpIndex = line.IndexOf(URLStart);
                    if (httpIndex != -1)
                    {
                        int startIndex = httpIndex + URLStart.Length;
                        int endIndex = line.IndexOf('/', startIndex);
                        if (endIndex != -1)
                        {
                            string currentOrganization = line.Substring(startIndex, endIndex - startIndex);
                            string newLine = line.Replace(currentOrganization, organizationName.ToString());
                            if (newLine != line)
                            {
                                lines[i] = newLine;
                                changed = true;
                            }
                        }
                    }

                    if (isYaml)
                    {
                        int repositoryIndex = line.IndexOf(RepositoryStart);
                        if (repositoryIndex != -1)
                        {
                            int startIndex = repositoryIndex + RepositoryStart.Length;
                            int endIndex = line.IndexOf('/', startIndex);
                            if (endIndex != -1)
                            {
                                string currentOrganization = line.Substring(startIndex, endIndex - startIndex);
                                string newLine = line.Replace(currentOrganization, organizationName.ToString());
                                if (newLine != line)
                                {
                                    lines[i] = newLine;
                                    changed = true;
                                }
                            }
                        }
                    }
                    else if (isLicense)
                    {
                        int copyrightStart = line.IndexOf(CopyrightStart);
                        if (copyrightStart != -1)
                        {
                            int currentYear = DateTime.Now.Year;
                            string newLine = $"{CopyrightStart}{currentYear} {organizationName}";
                            if (newLine != line)
                            {
                                lines[i] = newLine;
                                changed = true;
                            }
                        }
                    }
                }

                if (changed)
                {
                    File.WriteAllLines(filePath, lines);
                    runner.WriteInfoLine($"Updated {filePath}");
                }
            }
        }
    }
}