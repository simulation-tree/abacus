using Abacus.Manager.Constants;
using Collections.Generic;
using System;
using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct UpdateOrganizationName : ICommand
    {
        readonly string ICommand.Name => "update-org-name";
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

                    if (changed)
                    {
                        project.WriteToFile();
                        runner.WriteInfoLine($"Updated {project.Path.ToString()}");
                    }
                }

                repository.Dispose();
            }
        }

        private void RenameInFiles(Runner runner, USpan<char> repositoryPath)
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