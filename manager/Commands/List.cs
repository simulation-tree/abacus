using Collections;

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
                using TableBuilder table = new("Name", "Remote", "Projects", "Commits?", "Changes?");
                using Array<Repository> repositories = runner.GetRepositories();
                foreach (Repository repository in repositories)
                {
                    string name = repository.Name.ToString();
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

                    table.AddRow(name, remote, projects, hasCommits ? "Yes" : string.Empty, hasChanges ? "Yes" : string.Empty);
                    repository.Dispose();
                }

                runner.WriteInfoLine(table.ToString());
            }
            else if (listProjects)
            {
                using TableBuilder table = new("Name", "Is Test?");
                using Array<Project> projects = runner.GetProjects();
                foreach (Project project in projects)
                {
                    string name = project.Name.ToString();
                    string isTest = project.isTestProject ? "Yes" : "No";
                    table.AddRow(name, isTest);
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