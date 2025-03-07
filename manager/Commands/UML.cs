using Clipboard;
using Collections;
using Collections.Generic;
using Unmanaged;

namespace Abacus.Manager.Commands
{
    public readonly struct UML : ICommand
    {
        readonly string ICommand.Name => "uml";
        readonly string ICommand.Description => $"Generate UML diagrams (--with-tests --mode=implementations/abstractions)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            bool withTests = false;
            if (arguments.Contains("--with-tests"))
            {
                withTests = true;
            }

            bool? mode = null;
            if (arguments.Contains("--mode=implementations"))
            {
                mode = true;
            }
            else if (arguments.Contains("--mode=abstractions"))
            {
                mode = false;
            }

            using Array<Project> projects = runner.GetProjects(false);
            using Text types = new();
            using List<long> typesIncluded = new();
            using List<long> combinations = new();
            using Text dependencies = new();
            foreach (Project project in projects)
            {
                if (!withTests)
                {
                    if (project.isTestProject)
                    {
                        continue;
                    }
                }

                string projectName = project.Name.ToString();
                if (projectName == "Abacus.Manager")
                {
                    continue;
                }
                else if (projectName.EndsWith("PreGenerator"))
                {
                    continue;
                }

                if (projectName.EndsWith(".Core"))
                {
                    projectName = projectName.Substring(0, projectName.Length - 5);
                }

                if (mode == false)
                {
                    if (projectName.EndsWith(".System") || projectName.EndsWith(".Systems"))
                    {
                        continue;
                    }
                    else if (projectName == "Abacus.Simulator")
                    {
                        continue;
                    }
                }
                else if (mode == true)
                {
                    if (!projectName.EndsWith(".System") && !projectName.EndsWith(".Systems"))
                    {
                        continue;
                    }
                }

                if (projectName.EndsWith(".Generator"))
                {
                    continue;
                }

                if (mode == false)
                {
                    bool dependsOnSystems = false;
                    int dependencyCount = 0;
                    foreach (Project.ProjectReference dependency in project.ProjectReferences)
                    {
                        string dependencyName = System.IO.Path.GetFileNameWithoutExtension(dependency.Include.ToString());
                        if (dependencyName.EndsWith(".Generator"))
                        {
                            continue;
                        }

                        if (dependencyName.EndsWith(".Core"))
                        {
                            dependencyName = dependencyName.Substring(0, dependencyName.Length - 5);
                        }

                        if (dependencyName.EndsWith(".System") || dependencyName.EndsWith(".Systems"))
                        {
                            dependsOnSystems = true;
                            continue;
                        }

                        dependencyCount++;
                    }

                    if (dependsOnSystems)
                    {
                        continue;
                    }

                    if (dependencyCount == 0)
                    {
                        continue;
                    }
                }

                projectName = projectName.Replace(".", "");
                if (typesIncluded.TryAdd(projectName.GetLongHashCode()))
                {
                    types.Append("class ");
                    types.Append(projectName);
                    types.Append('\n');
                }

                foreach (Project.ProjectReference dependency in project.ProjectReferences)
                {
                    string dependencyName = System.IO.Path.GetFileNameWithoutExtension(dependency.Include.ToString());
                    if (dependencyName.EndsWith(".Generator"))
                    {
                        continue;
                    }

                    if (dependencyName.EndsWith(".Core"))
                    {
                        dependencyName = dependencyName.Substring(0, dependencyName.Length - 5);
                    }

                    dependencyName = dependencyName.Replace(".", "");
                    if (dependencyName == projectName)
                    {
                        continue;
                    }

                    long combination = projectName.GetLongHashCode() + dependencyName.GetLongHashCode();
                    if (!combinations.TryAdd(combination))
                    {
                        continue;
                    }

                    dependencies.Append(dependencyName);
                    dependencies.Append(" <|-down- ");
                    dependencies.Append(projectName);
                    dependencies.Append('\n');

                    if (typesIncluded.TryAdd(projectName.GetLongHashCode()))
                    {
                        types.Append("class ");
                        types.Append(projectName);
                        types.Append('\n');
                    }
                }
            }

            string source = UMLTemplate.Source;
            source = source.Replace("{{Title}}", "abacus");
            source = source.Replace("{{Types}}", types.ToString());
            source = source.Replace("{{Dependencies}}", dependencies.ToString());

            using Library clipboard = new();
            clipboard.Text = source;

            foreach (Project project in projects)
            {
                project.Dispose();
            }
        }
    }
}