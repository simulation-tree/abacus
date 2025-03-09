using Collections.Generic;
using Serialization.XML;
using System;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Project : IDisposable
    {
        private const string PackageIdNode = "PackageId";
        private const string CompanyNode = "Company";
        private const string RepositoryUrlNode = "RepositoryUrl";
        private const string TargetFrameworkNode = "TargetFramework";
        private const string ProjectReferenceNode = "ProjectReference";
        private const string PackageReferenceNode = "PackageReference";
        private const string IncludeAttribute = "Include";

        public readonly bool isTestProject;

        private readonly XMLNode rootNode;
        private readonly Text path;
        private readonly Text name;
        private readonly Array<ProjectReference> projectReferences;
        private readonly Array<PackageReference> packageReferences;
        private readonly XMLNode targetFramework;
        private readonly XMLNode projectPropertyGroup;
        private readonly XMLNode packageId;
        private readonly XMLNode company;
        private readonly XMLNode repositoryUrl;

        /// <summary>
        /// Name of the project based on the file name.
        /// </summary>
        public readonly ReadOnlySpan<char> Name => name.AsSpan();

        /// <summary>
        /// Path to the .csproj file.
        /// </summary>
        public readonly ReadOnlySpan<char> Path => path.AsSpan();

        /// <summary>
        /// NuGet package ID.
        /// <para>
        /// Also the name of the generated zip containing the package.
        /// </para>
        /// <para>
        /// May be empty if unassigned.
        /// </para>
        /// </summary>
        public readonly Text.Borrowed PackageId => packageId.Content;

        /// <summary>
        /// Company name value.
        /// </summary>
        public readonly Text.Borrowed Company => company.Content;

        /// <summary>
        /// RepositoryUrl value.
        /// </summary>
        public readonly Text.Borrowed RepositoryUrl => repositoryUrl.Content;

        /// <summary>
        /// The directory that the project is in.
        /// </summary>
        public readonly ReadOnlySpan<char> Directory
        {
            get
            {
                Span<char> buffer = stackalloc char[(int)path.Length];
                path.CopyTo(buffer);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == '\\')
                    {
                        buffer[i] = '/';
                    }
                }

                if (buffer.TryLastIndexOf('/', out int index))
                {
                    return path.AsSpan().Slice(0, index);
                }
                else
                {
                    return path.AsSpan();
                }
            }
        }

        public readonly ReadOnlySpan<ProjectReference> ProjectReferences => projectReferences.AsSpan();
        public readonly ReadOnlySpan<PackageReference> PackageReferences => packageReferences.AsSpan();

        public Project(ReadOnlySpan<char> path)
        {
            this.path = new(path);
            this.name = new(System.IO.Path.GetFileNameWithoutExtension(path));

            using System.IO.FileStream fileStream = System.IO.File.OpenRead(this.path.ToString());
            using ByteReader reader = new(fileStream);
            rootNode = reader.ReadObject<XMLNode>();

            using Stack<XMLNode> stack = new();
            stack.Push(rootNode);

            Span<ProjectReference> projectReferences = stackalloc ProjectReference[128];
            int projectReferencesCount = 0;
            Span<PackageReference> packageReferences = stackalloc PackageReference[128];
            int packageReferencesCount = 0;
            while (stack.TryPop(out XMLNode node))
            {
                if (node.Name.Equals(ProjectReferenceNode))
                {
                    if (node.TryGetAttribute(IncludeAttribute, out ReadOnlySpan<char> referencedProjectPath))
                    {
                        projectReferences[projectReferencesCount++] = new(referencedProjectPath);
                    }
                }
                else if (node.Name.Equals(PackageReferenceNode))
                {
                    if (node.TryGetAttribute(IncludeAttribute, out ReadOnlySpan<char> referencedProjectPath) && node.TryGetAttribute("Version", out ReadOnlySpan<char> version))
                    {
                        packageReferences[packageReferencesCount++] = new(referencedProjectPath, version);
                    }
                }
                else if (node.Name.Equals(PackageIdNode))
                {
                    packageId = node;
                }
                else if (node.Name.Equals(CompanyNode))
                {
                    company = node;
                }
                else if (node.Name.Equals(RepositoryUrlNode))
                {
                    repositoryUrl = node;
                }
                else
                {
                    foreach (XMLNode child in node.Children)
                    {
                        if (child.Name.Equals(TargetFrameworkNode))
                        {
                            targetFramework = child;
                            projectPropertyGroup = node;
                        }

                        stack.Push(child);
                    }
                }
            }

            if (targetFramework == default)
            {
                throw new InvalidOperationException($"TargetFramework node not found in {path.ToString()}");
            }

            if (packageId == default)
            {
                packageId = new(PackageIdNode);
                projectPropertyGroup.Add(packageId);
            }

            if (company == default)
            {
                company = new(CompanyNode);
                projectPropertyGroup.Add(company);
            }

            if (repositoryUrl == default)
            {
                repositoryUrl = new(RepositoryUrlNode);
                projectPropertyGroup.Add(repositoryUrl);
            }

            this.projectReferences = new(projectReferences.Slice(0, projectReferencesCount));
            this.packageReferences = new(packageReferences.Slice(0, packageReferencesCount));
            isTestProject = ContainsTestPackages();
        }

        private readonly bool ContainsTestPackages()
        {
            foreach (PackageReference packageReference in packageReferences)
            {
                if (packageReference.Include.IndexOf("NUnit") != -1)
                {
                    return true;
                }
                else if (packageReference.Include.IndexOf("xunit") != -1)
                {
                    return true;
                }
                else if (packageReference.Include.IndexOf("Microsoft.NET.Test.Sdk") != -1)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return path.ToString();
        }

        public readonly void Dispose()
        {
            for (int i = 0; i < projectReferences.Length; i++)
            {
                projectReferences[i].Dispose();
            }

            for (int i = 0; i < packageReferences.Length; i++)
            {
                packageReferences[i].Dispose();
            }

            projectReferences.Dispose();
            packageReferences.Dispose();
            name.Dispose();
            path.Dispose();
            rootNode.Dispose();
        }

        /// <summary>
        /// Writes the state of the project to the .csproj file.
        /// </summary>
        public readonly void WriteToFile()
        {
            using Text buffer = new(0);
            ToStringFlags flags = ToStringFlags.CarriageReturn | ToStringFlags.LineFeed | ToStringFlags.RootSpacing;
            rootNode.ToString(buffer, "    ", flags);
            System.IO.File.WriteAllText(path.ToString(), buffer.AsSpan());
        }

        public readonly struct PackageReference : IDisposable
        {
            private readonly Text include;
            private readonly Text version;

            public readonly ReadOnlySpan<char> Include => include.AsSpan();
            public readonly ReadOnlySpan<char> Version => version.AsSpan();

            public PackageReference(ReadOnlySpan<char> include, ReadOnlySpan<char> version)
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

            public readonly ReadOnlySpan<char> Include => include.AsSpan();

            public ProjectReference(ReadOnlySpan<char> include)
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