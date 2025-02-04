using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Unmanaged;

namespace Abacus.Manager
{
    public static class Terminal
    {
        public static USpan<char> Execute(string workingDirectory, string command)
        {
            return Execute(workingDirectory.AsSpan(), command.AsSpan());
        }

        public static USpan<char> Execute(string workingDirectory, USpan<char> command)
        {
            return Execute(workingDirectory.AsSpan(), command);
        }

        public static USpan<char> Execute(USpan<char> workingDirectory, string command)
        {
            return Execute(workingDirectory, command.AsSpan());
        }

        public static USpan<char> Execute(USpan<char> workingDirectory, USpan<char> command)
        {
            ProcessStartInfo startInfo = new();
            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/C {command.ToString()}";
            }
            else if (OperatingSystem.IsLinux())
            {
                startInfo.FileName = "/bin/bash";
                startInfo.Arguments = command.ToString();
            }
            else
            {
                throw new Exception($"Unsupported operating system `{Environment.OSVersion}`");
            }

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = workingDirectory.ToString();

            StringBuilder output = new();
            StringBuilder error = new();
            using AutoResetEvent outputWaitHandle = new(false);
            using AutoResetEvent errorWaitHandle = new(false);
            using Process? process = Process.Start(startInfo);
            if (process is not null)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data is null)
                    {
                        try
                        {
                            outputWaitHandle.Set();
                        }
                        catch { }
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data is null)
                    {
                        try
                        {
                            errorWaitHandle.Set();
                        }
                        catch { }
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                TimeSpan timeout = TimeSpan.FromSeconds(60);
                if (process.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout) && errorWaitHandle.WaitOne(timeout))
                {
                    string? errorString = error.ToString();
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        return errorString.AsSpan();
                    }
                    else
                    {
                        return output.ToString().AsSpan();
                    }
                }
                else
                {
                    throw new Exception("Program timed out");
                }
            }
            else
            {
                return default;
            }
        }
    }
}