using Collections.Generic;
using System;
using System.IO;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Repository : IDisposable, IEquatable<Repository>
    {
        private readonly Text path;
        private readonly Text remote;
        private readonly Array<Project> projects;

        /// <summary>
        /// Name of the repository based on the remote URL.
        /// </summary>
        public readonly ReadOnlySpan<char> Name => System.IO.Path.GetFileNameWithoutExtension(remote.ToString());

        /// <summary>
        /// The remote URL.
        /// </summary>
        public readonly ReadOnlySpan<char> Remote => remote.AsSpan();

        /// <summary>
        /// Path to the repository folder.
        /// </summary>
        public readonly ReadOnlySpan<char> Path => path.AsSpan();

        /// <summary>
        /// Projects part of this repository.
        /// </summary>
        public readonly ReadOnlySpan<Project> Projects => projects.AsSpan();

        /// <summary>
        /// Greatest semantic version tag in the repository.
        /// </summary>
        public readonly SemanticVersion GreatestVersion
        {
            get
            {
                ReadOnlySpan<char> allTags = Terminal.Execute(Path, "git tag -l --sort=-creatordate");
                SemanticVersion greatestVersion = SemanticVersion.Parse("0.0.0");
                int index = 0;
                int start = 0;
                while (index < allTags.Length)
                {
                    char c = allTags[index];
                    if (c == '\n')
                    {
                        ReadOnlySpan<char> tag = allTags.Slice(start, index - start).TrimEnd('\r');
                        if (tag.StartsWith('v'))
                        {
                            tag = tag.Slice(1);
                        }

                        if (SemanticVersion.TryParse(tag, out SemanticVersion version))
                        {
                            if (version > greatestVersion)
                            {
                                greatestVersion = version;
                            }
                        }

                        start = index + 1;
                    }

                    index++;
                }

                return greatestVersion;
            }
        }

        public readonly bool IsDisposed => path.IsDisposed;

        [Obsolete("Default constructor not supported", true)]
        public Repository() { }

        public Repository(ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> remote = Terminal.Execute(path, "git remote get-url origin");
            remote = remote.TrimEnd('\n');
            remote = remote.TrimEnd('\r');

            this.path = new(path);
            this.remote = new(remote);
            string[] projectPaths = Directory.GetFiles(this.path.ToString(), "*.csproj", SearchOption.AllDirectories);
            Span<uint> projectPathIndicesBuffer = stackalloc uint[projectPaths.Length];
            int projectCount = 0;
            for (uint i = 0; i < projectPaths.Length; i++)
            {
                string projectPath = projectPaths[i];
                string projectDirectory = System.IO.Path.GetDirectoryName(projectPath) ?? string.Empty;
                if (Directory.GetFiles(projectDirectory, "*.slnx").Length == 0)
                {
                    projectPathIndicesBuffer[projectCount++] = i;
                }
            }

            projects = new(projectCount);
            for (int i = 0; i < projectCount; i++)
            {
                string projectPath = projectPaths[projectPathIndicesBuffer[i]];
                projects[i] = new(projectPath);
            }
        }

        public readonly void Dispose()
        {
            foreach (Project project in projects)
            {
                project.Dispose();
            }

            projects.Dispose();
            remote.Dispose();
            path.Dispose();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Repository repository && Equals(repository);
        }

        public readonly bool Equals(Repository other)
        {
            if (path.IsDisposed != other.path.IsDisposed)
            {
                return false;
            }
            else if (path.IsDisposed && other.path.IsDisposed)
            {
                return true;
            }

            return path.Equals(other.path);
        }

        public readonly override int GetHashCode()
        {
            return path.GetHashCode();
        }

        /// <summary>
        /// Tries to fetch the repository that contains this <paramref name="path"/>.
        /// </summary>
        public static bool TryGetRepository(ReadOnlySpan<char> path, out Repository repository)
        {
            // check if path is a directory or a file first
            string? directory = path.ToString();
            if (System.IO.Path.HasExtension(directory))
            {
                directory = System.IO.Path.GetDirectoryName(directory);
                if (directory is null)
                {
                    repository = default;
                    return false;
                }
            }

            do
            {
                string[] directories = Directory.GetDirectories(directory, ".git", SearchOption.TopDirectoryOnly);
                if (directories.Length > 0)
                {
                    repository = new(directories[0]);
                    return true;
                }

                directory = System.IO.Path.GetDirectoryName(directory) ?? string.Empty;
            }
            while (directory is not null);

            repository = default;
            return false;
        }

        public static bool operator ==(Repository left, Repository right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Repository left, Repository right)
        {
            return !(left == right);
        }
    }
}