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
    }
}