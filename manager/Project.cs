using Collections;
using Serialization.XML;
using System;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Project : IDisposable
    {
        public readonly bool isTestProject;

        private readonly Text path;
        private readonly Text name;
        private readonly Array<ProjectReference> projectReferences;
        private readonly Array<PackageReference> packageReferences;

        /// <summary>
        /// Name of the project based on the file name.
        /// </summary>
        public readonly USpan<char> Name => name.AsSpan();

        /// <summary>
        /// Path to the .csproj file.
        /// </summary>
        public readonly USpan<char> Path => path.AsSpan();

        /// <summary>
        /// The directory that the project is in.
        /// </summary>
        public readonly USpan<char> Directory
        {
            get
            {
                USpan<char> buffer = stackalloc char[(int)path.Length];
                path.CopyTo(buffer);
                for (uint i = 0; i < buffer.Length; i++)
                {
                    ref char c = ref buffer[i];
                    if (c == '\\')
                    {
                        c = '/';
                    }
                }

                if (buffer.TryLastIndexOf('/', out uint index))
                {
                    return path.AsSpan().Slice(0, index);
                }
                else
                {
                    return path.AsSpan();
                }
            }
        }

        public readonly USpan<ProjectReference> ProjectReferences => projectReferences.AsSpan();
        public readonly USpan<PackageReference> PackageReferences => packageReferences.AsSpan();

        public Project(USpan<char> path)
        {
            this.path = new(path);
            this.name = new(System.IO.Path.GetFileNameWithoutExtension(path));

            using System.IO.FileStream fileStream = System.IO.File.OpenRead(this.path);
            using BinaryReader reader = new(fileStream);
            using XMLNode rootNode = reader.ReadObject<XMLNode>();
            using Stack<XMLNode> stack = new();
            stack.Push(rootNode);

            USpan<ProjectReference> projectReferences = stackalloc ProjectReference[128];
            uint projectReferencesCount = 0;
            USpan<PackageReference> packageReferences = stackalloc PackageReference[128];
            uint packageReferencesCount = 0;
            while (stack.Count > 0)
            {
                XMLNode node = stack.Pop();
                if (node.Name.SequenceEqual("ProjectReference".AsSpan()))
                {
                    if (node.TryGetAttribute("Include", out USpan<char> referencedProjectPath))
                    {
                        projectReferences[projectReferencesCount++] = new(referencedProjectPath);
                    }
                }
                else if (node.Name.SequenceEqual("PackageReference".AsSpan()))
                {
                    if (node.TryGetAttribute("Include", out USpan<char> referencedProjectPath) && node.TryGetAttribute("Version", out USpan<char> version))
                    {
                        packageReferences[packageReferencesCount++] = new(referencedProjectPath, version);
                    }
                }
                else
                {
                    foreach (XMLNode child in node.Children)
                    {
                        stack.Push(child);
                    }
                }
            }

            this.projectReferences = new(projectReferences.Slice(0, projectReferencesCount));
            this.packageReferences = new(packageReferences.Slice(0, packageReferencesCount));
            isTestProject = ContainsTestPackages();
        }

        private readonly bool ContainsTestPackages()
        {
            foreach (PackageReference packageReference in packageReferences)
            {
                if (packageReference.Include.Contains("NUnit".AsSpan()))
                {
                    return true;
                }
                else if (packageReference.Include.Contains("xunit".AsSpan()))
                {
                    return true;
                }
                else if (packageReference.Include.Contains("Microsoft.NET.Test.Sdk".AsSpan()))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return path;
        }

        public readonly void Dispose()
        {
            for (uint i = 0; i < projectReferences.Length; i++)
            {
                projectReferences[i].Dispose();
            }

            for (uint i = 0; i < packageReferences.Length; i++)
            {
                packageReferences[i].Dispose();
            }

            projectReferences.Dispose();
            packageReferences.Dispose();
            name.Dispose();
            path.Dispose();
        }

        public readonly struct PackageReference : IDisposable
        {
            private readonly Text include;
            private readonly Text version;

            public readonly USpan<char> Include => include.AsSpan();
            public readonly USpan<char> Version => version.AsSpan();

            public PackageReference(USpan<char> include, USpan<char> version)
            {
                this.include = new Text(include);
                this.version = new Text(version);
            }

            public readonly void Dispose()
            {
                version.Dispose();
                include.Dispose();
            }
        }

        public readonly struct ProjectReference : IDisposable
        {
            private readonly Text include;

            public readonly USpan<char> Include => include.AsSpan();

            public ProjectReference(USpan<char> include)
            {
                this.include = new Text(include);
            }

            public readonly void Dispose()
            {
                include.Dispose();
            }
        }
    }
}