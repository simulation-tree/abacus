using Collections;
using System;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Repository : IDisposable
    {
        private readonly Text path;
        private readonly Text remote;
        private readonly Text name;
        private readonly Array<Project> projects;

        public readonly USpan<char> Name => name.AsSpan();
        public readonly USpan<char> Remote => remote.AsSpan();
        public readonly USpan<char> Path => path.AsSpan();
        public readonly USpan<Project> Projects => projects.AsSpan();

        [Obsolete("Default constructor not supported")]
        public Repository()
        {
            throw new NotSupportedException();
        }

        public Repository(USpan<char> path, USpan<char> remote)
        {
            this.path = new(path);
            this.remote = new(remote);
            this.name = new(System.IO.Path.GetFileNameWithoutExtension(path));
            string[] projectPaths = System.IO.Directory.GetFiles(this.path, "*.csproj", System.IO.SearchOption.AllDirectories);
            this.projects = new((uint)projectPaths.Length);
            for (uint i = 0; i < projectPaths.Length; i++)
            {
                string projectPath = projectPaths[i];
                projects[i] = new(projectPath.AsSpan());
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
    }
}