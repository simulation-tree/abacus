using Collections.Generic;
using System;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Repository : IDisposable, IEquatable<Repository>
    {
        private readonly Text path;
        private readonly Text remote;
        private readonly Text name;
        private readonly Array<Project> projects;

        /// <summary>
        /// Name of the repository.
        /// </summary>
        public readonly ReadOnlySpan<char> Name => name.AsSpan();

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

        public readonly bool IsDisposed => path.IsDisposed;

        [Obsolete("Default constructor not supported")]
        public Repository()
        {
            throw new NotSupportedException();
        }

        public Repository(ReadOnlySpan<char> path, ReadOnlySpan<char> remote)
        {
            this.path = new(path);
            this.remote = new(remote);
            this.name = new(System.IO.Path.GetFileNameWithoutExtension(path));
            string[] projectPaths = System.IO.Directory.GetFiles(this.path.ToString(), "*.csproj", System.IO.SearchOption.AllDirectories);
            Span<uint> projectPathIndicesBuffer = stackalloc uint[projectPaths.Length];
            int projectCount = 0;
            for (uint i = 0; i < projectPaths.Length; i++)
            {
                string projectPath = projectPaths[i];
                string projectDirectory = System.IO.Path.GetDirectoryName(projectPath) ?? string.Empty;
                if (System.IO.Directory.GetFiles(projectDirectory, "*.sln").Length == 0)
                {
                    projectPathIndicesBuffer[projectCount++] = i;
                }
            }

            this.projects = new(projectCount);
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
            name.Dispose();
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