using Collections.Generic;

namespace Abacus.Manager.Commands
{
    public readonly struct List : ICommand
    {
        readonly string ICommand.Name => "list";
        readonly string ICommand.Description => "General list command (--projects, --repositories)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            bool listProjects = arguments.Contains("--projects");
            bool listRepositories = arguments.Contains("--repositories");
            if (listRepositories)
            {
                using TableBuilder table = new("Remote", "Projects", "Commits", "Changes", "Version");
                using Array<Repository> repositories = runner.GetRepositories();
                runner.WriteInfoLine($"Found {repositories.Length} repositories");
                foreach (Repository repository in repositories)
                {
                    string remote = repository.Remote.ToString();
                    if (remote.StartsWith("https://github.com/"))
                    {
                        remote = remote.Substring(19);
                    }

                    string projects = string.Empty;
                    bool hasCommits = Terminal.Execute(repository.Path, "git log --branches --not --remotes").Length > 0;
                    bool hasChanges = Terminal.Execute(repository.Path, "git status --porcelain=v1").Length > 0;
                    foreach (Project project in repository.Projects)
                    {
                        projects += project.Name.ToString() + ", ";
                    }

                    if (projects.Length > 0)
                    {
                        projects = projects.Substring(0, projects.Length - 2);
                    }

                    table.AddRow(remote, projects, hasCommits ? "Yes" : string.Empty, hasChanges ? "Yes" : string.Empty, repository.GreatestVersion.ToString());
                    repository.Dispose();
                }

                runner.WriteInfoLine(table.ToString());
            }
            else if (listProjects)
            {
                using TableBuilder table = new("Name", "Test", "Generator");
                using Array<Project> projects = runner.GetProjects();
                runner.WriteInfoLine($"Found {projects.Length} projects");
                foreach (Project project in projects)
                {
                    string name = project.Name.ToString();
                    string isTest = project.isTestProject ? "Yes" : "No";
                    string isGenerator = project.isGeneratorProject ? "Yes" : "No";
                    table.AddRow(name, isTest, isGenerator);
                    project.Dispose();
                }

                runner.WriteInfoLine(table.ToString());

            }
            else
            {
                runner.WriteErrorLine("No list option specified");
            }
        }
    }
}