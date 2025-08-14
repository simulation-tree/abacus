using Collections.Generic;
using System;
using System.IO;
using Unmanaged;
using XML;

namespace Abacus.Manager
{
    public readonly struct Project : IDisposable
    {
        private const string PackageIdNode = "PackageId";
        private const string RepositoryUrlNode = "RepositoryUrl";
        private const string TargetFrameworkNode = "TargetFramework";
        private const string ProjectReferenceNode = "ProjectReference";
        private const string PackageReferenceNode = "PackageReference";
        private const string IncludeAttribute = "Include";
        private const string SuppressDependenciesWhenPackingNode = "SuppressDependenciesWhenPacking";
        private const string OutDirNode = "OutDir";

        public readonly bool isTestProject;
        public readonly bool isGeneratorProject;
        public readonly XMLNode rootNode;

        private readonly Text path;
        private readonly List<ProjectReference> projectReferences;
        private readonly List<PackageReference> packageReferences;
        private readonly List<TargetFramework> targetFrameworks;
        private readonly XMLNode targetFrameworkNode;
        private readonly XMLNode projectPropertyGroup;
        private readonly XMLNode packageId;
        private readonly XMLNode company;
        private readonly XMLNode outputTypeNode;
        private readonly XMLNode repositoryUrlNode;
        private readonly XMLNode includeBuildOutput;
        private readonly XMLNode embedAllSources;
        private readonly XMLNode suppressDependenciesWhenPacking;
        private readonly XMLNode outDir;

        /// <summary>
        /// Name of the project based on the file name.
        /// </summary>
        public readonly ReadOnlySpan<char> Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        /// <summary>
        /// Path to the project file.
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
        public readonly Text.Borrowed RepositoryUrl => repositoryUrlNode.Content;

        /// <summary>
        /// The output type of the project.
        /// </summary>
        public readonly OutputType OutputType
        {
            get
            {
                if (Enum.TryParse(outputTypeNode.Content.AsSpan(), out OutputType outputType))
                {
                    return outputType;
                }
                else
                {
                    return OutputType.Unknown;
                }
            }
            set => outputTypeNode.Content.CopyFrom(value.ToString());
        }

        public readonly ReadOnlySpan<TargetFramework> TargetFrameworks => targetFrameworks.AsSpan();

        public readonly bool IncludeBuildOutput
        {
            get => includeBuildOutput.Content.Equals("true");
            set => includeBuildOutput.Content.CopyFrom(value ? "true" : "false");
        }

        public readonly bool EmbedAllSources
        {
            get => embedAllSources.Content.Equals("true");
            set => embedAllSources.Content.CopyFrom(value ? "true" : "false");
        }

        public readonly bool SuppressDependenciesWhenPacking
        {
            get => suppressDependenciesWhenPacking.Content.Equals("true");
            set => suppressDependenciesWhenPacking.Content.CopyFrom(value ? "true" : "false");
        }

        public readonly Text.Borrowed OutDir => outDir.Content;

        public readonly int SourceFiles
        {
            get
            {
                int count = 0;
                string directory = Directory.ToString();
                string objDirectory = System.IO.Path.Combine(directory, "obj");
                string binDirectory = System.IO.Path.Combine(directory, "bin");
                string[] sourceFiles = System.IO.Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
                for (int i = 0; i < sourceFiles.Length; i++)
                {
                    string filePath = sourceFiles[i];
                    if (filePath.StartsWith(objDirectory, StringComparison.OrdinalIgnoreCase) || filePath.StartsWith(binDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    count++;
                }

                return count;
            }
        }

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

            using FileStream fileStream = File.OpenRead(this.path.ToString());
            using ByteReader reader = new(fileStream);
            rootNode = reader.ReadObject<XMLNode>();
            projectReferences = new();
            packageReferences = new();
            targetFrameworks = new();

            using Stack<XMLNode> stack = new();
            stack.Push(rootNode);

            while (stack.TryPop(out XMLNode node))
            {
                if (node.Name.Equals(ProjectReferenceNode))
                {
                    if (node.TryGetAttribute(IncludeAttribute, out ReadOnlySpan<char> referencedProjectPath))
                    {
                        projectReferences.Add(new(referencedProjectPath));
                    }
                }
                else if (node.Name.Equals(PackageReferenceNode))
                {
                    if (node.TryGetAttribute(IncludeAttribute, out ReadOnlySpan<char> referencedProjectPath) && node.TryGetAttribute("Version", out ReadOnlySpan<char> version))
                    {
                        packageReferences.Add(new(referencedProjectPath, version));
                    }
                }
                else if (node.Name.Equals(PackageIdNode))
                {
                    packageId = node;
                }
                else if (node.Name.Equals(nameof(Company)))
                {
                    company = node;
                }
                else if (node.Name.Equals(nameof(OutputType)))
                {
                    outputTypeNode = node;
                }
                else if (node.Name.Equals(RepositoryUrlNode))
                {
                    repositoryUrlNode = node;
                }
                else if (node.Name.Equals(nameof(IncludeBuildOutput)))
                {
                    includeBuildOutput = node;
                }
                else if (node.Name.Equals(nameof(EmbedAllSources)))
                {
                    embedAllSources = node;
                }
                else if (node.Name.Equals(SuppressDependenciesWhenPackingNode))
                {
                    suppressDependenciesWhenPacking = node;
                }
                else if (node.Name.Equals(OutDirNode))
                {
                    outDir = node;
                }
                else
                {
                    foreach (XMLNode child in node.Children)
                    {
                        if (child.Name.Equals(TargetFrameworkNode))
                        {
                            targetFrameworkNode = child;
                            projectPropertyGroup = node;
                            targetFrameworks.Add(TargetFramework.Parse(child.Content.AsSpan()));
                        }
                        else if (child.Name.Equals(TargetFrameworkNode + "s"))
                        {
                            targetFrameworkNode = child;
                            int start = 0;
                            int index = 0;
                            int length = child.Content.Length;
                            while (index < length)
                            {
                                char c = child.Content[index];
                                if (c == ';')
                                {
                                    ReadOnlySpan<char> part = child.Content.Slice(start, index - start);
                                    targetFrameworks.Add(TargetFramework.Parse(part));
                                    start = index + 1;
                                }
                                else if (index == length - 1)
                                {
                                    ReadOnlySpan<char> part = child.Content.Slice(start);
                                    targetFrameworks.Add(TargetFramework.Parse(part));
                                }

                                index++;
                            }

                            projectPropertyGroup = node;
                        }

                        stack.Push(child);
                    }
                }
            }

            if (targetFrameworkNode == default)
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
                company = new(nameof(Company));
                projectPropertyGroup.Add(company);
            }

            if (outputTypeNode == default)
            {
                outputTypeNode = new(nameof(OutputType));
                projectPropertyGroup.Add(outputTypeNode);
            }

            if (repositoryUrlNode == default)
            {
                repositoryUrlNode = new(RepositoryUrlNode);
                projectPropertyGroup.Add(repositoryUrlNode);
            }

            if (includeBuildOutput == default)
            {
                includeBuildOutput = new(nameof(IncludeBuildOutput));
                projectPropertyGroup.Add(includeBuildOutput);
            }

            if (embedAllSources == default)
            {
                embedAllSources = new(nameof(EmbedAllSources));
                projectPropertyGroup.Add(embedAllSources);
            }

            if (suppressDependenciesWhenPacking == default)
            {
                suppressDependenciesWhenPacking = new(SuppressDependenciesWhenPackingNode);
                projectPropertyGroup.Add(suppressDependenciesWhenPacking);
            }

            if (outDir == default)
            {
                outDir = new(OutDirNode);
                projectPropertyGroup.Add(outDir);
            }

            isTestProject = ContainsTestPackages();
            isGeneratorProject = ContainsGeneratorPackages();
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

        private readonly bool ContainsGeneratorPackages()
        {
            foreach (PackageReference packageReference in packageReferences)
            {
                if (packageReference.Include.IndexOf("Microsoft.CodeAnalysis.CSharp") != -1)
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
            for (int i = projectReferences.Count - 1; i >= 0; i--)
            {
                projectReferences[i].Dispose();
            }

            for (int i = packageReferences.Count - 1; i >= 0; i--)
            {
                packageReferences[i].Dispose();
            }

            targetFrameworks.Dispose();
            projectReferences.Dispose();
            packageReferences.Dispose();
            path.Dispose();
            rootNode.Dispose();
        }

        public readonly void ClearTargetFrameworks()
        {
            targetFrameworks.Clear();
        }

        public readonly void AddTargetFramework(TargetFramework targetFramework)
        {
            for (int i = 0; i < targetFrameworks.Count; i++)
            {
                if (targetFrameworks[i].Equals(targetFramework))
                {
                    return;
                }
            }

            targetFrameworks.Add(targetFramework);
        }

        /// <summary>
        /// Writes the state of the project to the .csproj file.
        /// </summary>
        public readonly void WriteToFile()
        {
            targetFrameworkNode.Name.CopyFrom("TargetFramework");
            if (targetFrameworks.Count > 1)
            {
                targetFrameworkNode.Name.Append('s');
            }

            targetFrameworkNode.Content.Clear();
            for (int i = 0; i < targetFrameworks.Count; i++)
            {
                if (i > 0)
                {
                    targetFrameworkNode.Content.Append(';');
                }

                targetFrameworkNode.Content.Append(targetFrameworks[i]);
            }

            using Text buffer = new(0);
            SerializationSettings settings = SerializationSettings.PrettyPrinted;
            settings.flags |= SerializationSettings.Flags.RootSpacing;
            settings.flags |= SerializationSettings.Flags.SkipEmptyNodes;
            rootNode.ToString(buffer, settings);
            File.WriteAllText(path.ToString(), buffer.AsSpan());
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