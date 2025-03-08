using Abacus.Manager.Constants;
using Collections.Generic;
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
            ASCIIText256 organizationName = Constant.Get<OrganizationName>();
            string[] markdownFiles = System.IO.Directory.GetFiles(repositoryPath.ToString(), "*.md", System.IO.SearchOption.AllDirectories);
            foreach (string markdownFile in markdownFiles)
            {
                string[] lines = System.IO.File.ReadAllLines(markdownFile);
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
                            if (currentOrganization == organizationName.ToString())
                            {
                                continue;
                            }

                            string newLine = line.Replace(currentOrganization, organizationName.ToString());
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
                    System.IO.File.WriteAllLines(markdownFile, lines);
                    runner.WriteInfoLine($"Updated {markdownFile}");
                }
            }
        }
    }
}