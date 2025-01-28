using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static Functions;

public readonly struct Test : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "test";
    ReadOnlySpan<char> ICommand.Description => "Tests all projects (--release, --coverage-xplat --coverage-coverlet, --generate-reports)";

    void ICommand.Execute(ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> arguments)
    {
        bool releaseMode = false;
        if (arguments.IndexOf("--release") != -1)
        {
            releaseMode = true;
        }

        bool xplatCoverage = false;
        if (arguments.IndexOf("--coverage-xplat") != -1)
        {
            xplatCoverage = true;
        }

        bool coverletCoverage = false;
        if (arguments.IndexOf("--coverage-coverlet") != -1)
        {
            coverletCoverage = true;
        }

        bool generateReports = false;
        if (arguments.IndexOf("--generate-reports") != -1)
        {
            generateReports = true;
        }

        string reportTypes = "Html";
        foreach (Project project in GetProjects(workingDirectory))
        {
            if (project.isTestProject)
            {
                string command = $"dotnet test \"{project.Path.ToString()}\"";
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

                if (releaseMode)
                {
                    Call($"dotnet build \"{project.Path.ToString()}\" -c Release");
                }
                else
                {
                    Call($"dotnet build \"{project.Path.ToString()}\" -c Debug");
                }

                string? result = Call($"{command} --no-build");
                if (result is not null)
                {
                    int index = result.IndexOf("Starting test execution");
                    if (index != -1)
                    {
                        Trace.WriteLine(result.Substring(index));
                    }
                    else
                    {
                        Trace.TraceError(result);
                    }
                }

                if (generateReports)
                {
                    string projectFolder = project.WorkingDirectory.ToString();
                    if (Directory.Exists(projectFolder))
                    {
                        string testResultsFolder = Path.Combine(projectFolder, "TestResults");
                        if (TryGetTestResultsFile(testResultsFolder, out string? testResultsPath))
                        {
                            string targetDir = Path.Combine(testResultsFolder, "report");
                            command = $"reportgenerator -reports:\"{testResultsPath}\" -targetdir:\"{targetDir}\" -reporttypes:{reportTypes}";
                            Call(command);
                        }
                    }
                }
            }
        }
    }

    private static bool TryGetTestResultsFile(string directory, [NotNullWhen(true)] out string? testResultsPath)
    {
        DateTime lastModifiedTime = DateTime.MinValue;
        string[] files = Directory.GetFiles(directory, "*.xml", SearchOption.AllDirectories);
        testResultsPath = null;
        foreach (string file in files)
        {
            DateTime modifiedTime = File.GetLastWriteTimeUtc(file);
            if (modifiedTime > lastModifiedTime)
            {
                lastModifiedTime = modifiedTime;
                testResultsPath = file;
            }
        }

        return testResultsPath is not null;
    }
}