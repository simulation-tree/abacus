using Abacus.Manager.Constants;
using Collections.Generic;
using Serialization.XML;
using System;
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
                    if (!project.isTestProject)
                    {
                        bool changed = false;
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

                        if (!project.IncludeBuildOutput.IsEmpty.Equals("false"))
                        {
                            project.IncludeBuildOutput.CopyFrom("false");
                            changed |= true;
                        }

                        if (!project.SuppressDependenciesWhenPacking.Equals("true"))
                        {
                            project.SuppressDependenciesWhenPacking.CopyFrom("true");
                            changed |= true;
                        }

                        changed |= EnsureBuildOutputsArePacked(project);

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
            string projectName = project.Name.ToString();
            string debugDLLpath = "bin/Debug/net9.0/" + projectName + ".dll";
            string releaseDLLpath = "bin/Release/net9.0/" + projectName + ".dll";
            string targetsPath = "build/" + projectName + ".targets";
            string packageDebugPath = "tools/debug/" + projectName + ".dll";
            string packageReleasePath = "tools/release/" + projectName + ".dll";
            XMLNode debugPackNode = default;
            XMLNode releasePackNode = default;
            XMLNode targetsPackNode = default;
            XMLNode itemGroupNode = default;
            using Stack<(XMLNode, XMLNode)> stack = new();
            stack.Push((default, project.rootNode));
            while (stack.TryPop(out (XMLNode parent, XMLNode current) entry))
            {
                XMLNode current = entry.current;
                if (current.Name.Equals("Content") && current.TryGetAttribute("Include", out ReadOnlySpan<char> include))
                {
                    XMLNode parent = entry.parent;
                    if (include.SequenceEqual(debugDLLpath))
                    {
                        debugPackNode = current;
                        itemGroupNode = parent;
                    }
                    else if (include.SequenceEqual(releaseDLLpath))
                    {
                        releasePackNode = current;
                        itemGroupNode = parent;
                    }
                    else if (include.SequenceEqual(targetsPath))
                    {
                        targetsPackNode = current;
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

            if (debugPackNode == default)
            {
                debugPackNode = new("Content");
                itemGroupNode.Add(debugPackNode);
                changed = true;
            }

            changed |= TrySetAttribute(debugPackNode, "Include", debugDLLpath);
            changed |= TrySetAttribute(debugPackNode, "Pack", "true");
            changed |= TrySetAttribute(debugPackNode, "PackagePath", packageDebugPath);
            changed |= TrySetAttribute(debugPackNode, "Visible", "false");

            if (releasePackNode == default)
            {
                releasePackNode = new("Content");
                itemGroupNode.Add(releasePackNode);
                changed = true;
            }

            changed |= TrySetAttribute(releasePackNode, "Include", releaseDLLpath);
            changed |= TrySetAttribute(releasePackNode, "Pack", "true");
            changed |= TrySetAttribute(releasePackNode, "PackagePath", packageReleasePath);
            changed |= TrySetAttribute(releasePackNode, "Visible", "false");

            if (targetsPackNode == default)
            {
                targetsPackNode = new("Content");
                itemGroupNode.Add(targetsPackNode);
                changed = true;
            }

            changed |= TrySetAttribute(targetsPackNode, "Include", targetsPath);
            changed |= TrySetAttribute(targetsPackNode, "Pack", "true");
            changed |= TrySetAttribute(targetsPackNode, "PackagePath", targetsPath);
            changed |= TrySetAttribute(targetsPackNode, "Visible", "false");
            return changed;

            static bool TrySetAttribute(XMLNode node, ReadOnlySpan<char> name, ReadOnlySpan<char> value)
            {
                if (!node.TryGetAttribute(name, out ReadOnlySpan<char> currentValue) || !currentValue.SequenceEqual(value))
                {
                    node.SetAttribute(name, value);
                    return true;
                }

                return false;
            }
        }

        private static void RenameInFiles(Runner runner, ReadOnlySpan<char> repositoryPath)
        {
            const string URLStart = "https://github.com/";
            const string RepositoryStart = "repository: ";
            const string CopyrightStart = "Copyright (c) ";
            ASCIIText256 organizationName = Constant.Get<OrganizationName>();
            string[] markdownFiles = System.IO.Directory.GetFiles(repositoryPath.ToString(), "*.md", System.IO.SearchOption.AllDirectories);
            string[] yamlFiles = System.IO.Directory.GetFiles(repositoryPath.ToString(), "*.yml", System.IO.SearchOption.AllDirectories);
            string[] allFiles = new string[markdownFiles.Length + yamlFiles.Length];
            markdownFiles.CopyTo(allFiles, 0);
            yamlFiles.CopyTo(allFiles, markdownFiles.Length);
            foreach (string filePath in allFiles)
            {
                bool isYaml = filePath.EndsWith(".yml");
                bool isLicense = System.IO.Path.GetFileNameWithoutExtension(filePath) == "LICENSE";
                string[] lines = System.IO.File.ReadAllLines(filePath);
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
                    System.IO.File.WriteAllLines(filePath, lines);
                    runner.WriteInfoLine($"Updated {filePath}");
                }
            }
        }
    }
}