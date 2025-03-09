using Collections;
using System;
using System.IO;

namespace Abacus.Manager.Commands
{
    public readonly struct Test : ICommand
    {
        readonly string ICommand.Name => "test";
        readonly string ICommand.Description => "Tests all projects (--release, --coverage-xplat --coverage-coverlet, --generate-reports)";

        readonly void ICommand.Execute(Runner runner, Arguments arguments)
        {
            bool releaseMode = false;
            if (arguments.Contains("--release"))
            {
                releaseMode = true;
            }

            bool xplatCoverage = false;
            if (arguments.Contains("--coverage-xplat"))
            {
                xplatCoverage = true;
            }

            bool coverletCoverage = false;
            if (arguments.Contains("--coverage-coverlet"))
            {
                coverletCoverage = true;
            }

            bool generateReports = false;
            if (arguments.Contains("--generate-reports"))
            {
                generateReports = true;
            }

            string reportTypes = "Html";
            string command = "dotnet test";
            if (releaseMode)
            {
                command += " -c Release";
            }
            else
            {
                command += " -c Debug";
            }

            if (coverletCoverage)
            {
                command += " /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura";
            }

            if (xplatCoverage)
            {
                command += " --collect:\"XPlat Code Coverage;Format=cobertura\"";
            }

            string solutionFolder = Directory.GetParent(runner.SolutionPath.ToString())?.FullName ?? string.Empty;
            if (releaseMode)
            {
                Terminal.Execute(solutionFolder, "dotnet build -c Release");
            }
            else
            {
                Terminal.Execute(solutionFolder, "dotnet build -c Debug");
            }

            ReadOnlySpan<char> result = Terminal.Execute(solutionFolder, $"{command} --no-build");
            if (!result.IsEmpty)
            {
                if (result.TryIndexOf("Starting test execution", out int index))
                {
                    runner.WriteInfoLine(result.Slice(index));
                }
                else
                {
                    runner.WriteErrorLine(result);
                }
            }

            if (generateReports)
            {
                string targetDir = Path.Combine(runner.WorkingDirectory.ToString(), "report");
                command = $"reportgenerator -reports:\"{runner.WorkingDirectory.ToString()}/**/coverage.cobertura.xml\" -targetdir:\"{targetDir}\" -reporttypes:{reportTypes}";
                Terminal.Execute(runner.WorkingDirectory, command);
            }
        }
    }
}