using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static Functions;

public readonly struct Test : ICommand
{
    ReadOnlySpan<char> ICommand.Name => "test";
    ReadOnlySpan<char> ICommand.Description => "Tests all projects (--release, --coverage-xplat --coverage-coverlet, --no-build --generate-reports)";

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

        bool noBuild = false;
        if (arguments.IndexOf("--no-build") != -1)
        {
            noBuild = true;
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

                if (noBuild)
                {
                    command += " --no-build";
                }

                if (coverletCoverage)
                {
                    command += " /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura";
                }

                if (xplatCoverage)
                {
                    command += " --collect:\"XPlat Code Coverage;Format=cobertura\"";
                }

                Call(command);

                if (generateReports)
                {
                    string projectFolder = Path.GetDirectoryName(project.Path.ToString()) ?? throw new();
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