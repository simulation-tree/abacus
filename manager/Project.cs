using Collections.Generic;
using System;
using System.IO;
using Unmanaged;
using XML;

namespace Abacus.Manager;

public readonly struct Project : IDisposable
{
    public readonly bool isTestProject;
    public readonly bool isGeneratorProject;
    public readonly XMLNode rootNode;

    private readonly Text path;
    private readonly List<ProjectReference> projectReferences;
    private readonly List<PackageReference> packageReferences;
    private readonly List<TargetFramework> targetFrameworks;
    private readonly List<Analyzer> analyzers;
    private readonly XMLNode targetFrameworkNode;
    private readonly XMLNode projectPropertyGroup;
    private readonly XMLNode projectReferencesReferencesItemGroup;
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
    public readonly OutputType? OutputType
    {
        get
        {
            if (Enum.TryParse(outputTypeNode.Content.AsSpan(), out OutputType outputType))
            {
                return outputType;
            }
            else
            {
                return null;
            }
        }
        set
        {
            if (value is OutputType outputType)
            {
                outputTypeNode.Content.CopyFrom(outputType.ToString());
            }
            else
            {
                outputTypeNode.Content.Clear();
            }
        }
    }

    public readonly ReadOnlySpan<TargetFramework> TargetFrameworks => targetFrameworks.AsSpan();
    public readonly ReadOnlySpan<Analyzer> Analyzers => analyzers.AsSpan();

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
            int lastSlashIndex = path.LastIndexOf('\\');
            if (lastSlashIndex != 0)
            {
                return path.AsSpan().Slice(0, lastSlashIndex);
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
        analyzers = new();

        using Stack<XMLNode> stack = new();
        stack.Push(rootNode);

        while (stack.TryPop(out XMLNode node))
        {
            if (node.Name.Equals("ItemGroup"))
            {
                foreach (XMLNode child in node.Children)
                {
                    if (child.Name.Equals("ProjectReference"))
                    {
                        OutputType? outputItemType = null;
                        bool? referenceOutputAssembly = null;
                        child.TryGetAttribute("Include", out ReadOnlySpan<char> include);
                        if (child.TryGetAttribute("ReferenceOutputAssembly", out ReadOnlySpan<char> referenceOutputAssemblyText))
                        {
                            if (referenceOutputAssemblyText.Equals("true", StringComparison.OrdinalIgnoreCase))
                            {
                                referenceOutputAssembly = true;
                            }
                            else if (referenceOutputAssemblyText.Equals("false", StringComparison.OrdinalIgnoreCase))
                            {
                                referenceOutputAssembly = false;
                            }
                        }

                        if (child.TryGetAttribute("OutputItemType", out ReadOnlySpan<char> outputItemTypeText))
                        {
                            if (Enum.TryParse(outputItemTypeText, out OutputType parsedOutputType))
                            {
                                outputItemType = parsedOutputType;
                            }
                        }

                        projectReferences.Add(new ProjectReference(include, referenceOutputAssembly, outputItemType));
                        projectReferencesReferencesItemGroup = node;
                    }
                    else if (child.Name.Equals("PackageReference"))
                    {
                        child.TryGetAttribute("Include", out ReadOnlySpan<char> include);
                        child.TryGetAttribute("Version", out ReadOnlySpan<char> version);
                        packageReferences.Add(new PackageReference(include, version));
                    }
                    else if (child.Name.Equals("Analyzer"))
                    {
                        child.TryGetAttribute("Include", out ReadOnlySpan<char> include);
                        analyzers.Add(new Analyzer(include));
                        node.TryRemove(child);
                    }
                }
            }
            else if (node.Name.Equals("PropertyGroup"))
            {
                foreach (XMLNode child in node.Children)
                {
                    if (child.Name.Equals(nameof(PackageId)))
                    {
                        packageId = child;
                    }
                    else if (child.Name.Equals(nameof(Company)))
                    {
                        company = child;
                    }
                    else if (child.Name.Equals(nameof(OutputType)))
                    {
                        outputTypeNode = child;
                    }
                    else if (child.Name.Equals(nameof(RepositoryUrl)))
                    {
                        repositoryUrlNode = child;
                    }
                    else if (child.Name.Equals(nameof(IncludeBuildOutput)))
                    {
                        includeBuildOutput = child;
                    }
                    else if (child.Name.Equals(nameof(EmbedAllSources)))
                    {
                        embedAllSources = child;
                    }
                    else if (child.Name.Equals(nameof(SuppressDependenciesWhenPacking)))
                    {
                        suppressDependenciesWhenPacking = child;
                    }
                    else if (child.Name.Equals(nameof(OutDir)))
                    {
                        outDir = child;
                    }
                    else if (child.Name.Equals(nameof(TargetFramework)))
                    {
                        targetFrameworkNode = child;
                        projectPropertyGroup = node;
                        targetFrameworks.Add(TargetFramework.Parse(child.Content.AsSpan()));
                    }
                    else if (child.Name.Equals(nameof(TargetFramework) + "s"))
                    {
                        targetFrameworkNode = child;
                        projectPropertyGroup = node;
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
                    }
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

        if (targetFrameworkNode == default)
        {
            throw new InvalidOperationException($"TargetFramework node not found in {path.ToString()}");
        }

        if (packageId == default)
        {
            packageId = new(nameof(PackageId));
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
            repositoryUrlNode = new(nameof(RepositoryUrl));
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
            suppressDependenciesWhenPacking = new(nameof(SuppressDependenciesWhenPacking));
            projectPropertyGroup.Add(suppressDependenciesWhenPacking);
        }

        if (outDir == default)
        {
            outDir = new(nameof(OutDir));
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
        for (int i = analyzers.Count - 1; i >= 0; i--)
        {
            analyzers[i].Dispose();
        }

        for (int i = projectReferences.Count - 1; i >= 0; i--)
        {
            projectReferences[i].Dispose();
        }

        for (int i = packageReferences.Count - 1; i >= 0; i--)
        {
            packageReferences[i].Dispose();
        }

        analyzers.Dispose();
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

    public readonly void ClearAnalyzers()
    {
        analyzers.Clear();
    }

    public readonly void AddProjectReference(ReadOnlySpan<char> include, bool? referenceOutputAssembly = null, OutputType? outputItemType = null)
    {
        if (projectReferencesReferencesItemGroup == default)
        {
            throw new InvalidOperationException("An ItemGroup containing ProjectReferences was not found");
        }

        projectReferences.Add(new ProjectReference(include, referenceOutputAssembly, outputItemType));
        XMLNode projectReferenceNode = new("ProjectReference");
        projectReferenceNode.SetAttribute("Include", include);
        if (referenceOutputAssembly is not null)
        {
            projectReferenceNode.SetAttribute("ReferenceOutputAssembly", referenceOutputAssembly.Value ? "true" : "false");
        }

        if (outputItemType is not null)
        {
            projectReferenceNode.SetAttribute("OutputItemType", outputItemType.Value.ToString());
        }

        projectReferencesReferencesItemGroup.Add(projectReferenceNode);
    }

    /// <summary>
    /// Writes the state of the project to the .csproj file.
    /// </summary>
    public readonly void WriteToFile()
    {
        targetFrameworkNode.Name.CopyFrom(nameof(TargetFramework));
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
        settings.flags |= SerializationSettings.Flags.SpaceBeforeClosingNode;
        rootNode.ToString(buffer, settings);
        File.WriteAllText(path.ToString(), buffer.AsSpan());
    }

    public readonly string GetFullPath(ReadOnlySpan<char> relativePath)
    {
        using Text path = new Text(Directory);

        // handle .. at the beginning
        while (relativePath.StartsWith("..\\"))
        {
            int lastSlashIndex = path.LastIndexOf('\\');
            if (lastSlashIndex == -1)
            {
                throw new InvalidOperationException($"Could not find project file from `{relativePath.ToString()}`");
            }

            path.SetLength(lastSlashIndex);
            relativePath = relativePath[3..];
        }

        // append the rest  
        if (path.Length > 0)
        {
            path.Append('\\');
        }

        path.Append(relativePath);
        return path.ToString();
    }

    public readonly string GetRelativePath(ReadOnlySpan<char> fullPath)
    {
        string difference = System.IO.Path.GetRelativePath(Directory.ToString(), fullPath.ToString());
        return difference;
    }

    public readonly struct PackageReference : IDisposable
    {
        private readonly Text text;
        private readonly int includeLength;

        public readonly ReadOnlySpan<char> Include => text.Slice(0, includeLength);
        public readonly ReadOnlySpan<char> Version => text.Slice(includeLength);


        [Obsolete("Not supported", true)]
        public PackageReference() { }

        public PackageReference(ReadOnlySpan<char> include, ReadOnlySpan<char> version)
        {
            text = new Text(include);
            text.Append(version);
            includeLength = include.Length;
        }

        public readonly void Dispose()
        {
            text.Dispose();
        }

        public readonly override string ToString()
        {
            return Include.ToString();
        }
    }

    public readonly struct ProjectReference : IDisposable
    {
        private readonly Text include;
        private readonly bool referenceOutputAssembly;
        private readonly OutputType outputItemType;
        private readonly Flags flags;

        public readonly ReadOnlySpan<char> Include => include.AsSpan();
        public readonly bool? ReferenceOutputAssembly => (flags & Flags.HasReferenceOutputAssembly) != 0 ? referenceOutputAssembly : null;
        public readonly OutputType? OutputItemType => (flags & Flags.HasOutputItemType) != 0 ? outputItemType : null;

        [Obsolete("Not supported", true)]
        public ProjectReference() { }

        public ProjectReference(ReadOnlySpan<char> include, bool? referenceOutputAssembly, OutputType? outputItemType)
        {
            this.include = new Text(include);
            this.referenceOutputAssembly = referenceOutputAssembly ?? false;
            this.outputItemType = outputItemType ?? default;
            flags = Flags.None;
            if (referenceOutputAssembly is not null)
            {
                flags |= Flags.HasReferenceOutputAssembly;
            }

            if (outputItemType is not null)
            {
                flags |= Flags.HasOutputItemType;
            }
        }

        public readonly void Dispose()
        {
            include.Dispose();
        }

        public readonly override string ToString()
        {
            return include.ToString();
        }

        [Flags]
        public enum Flags
        {
            None = 0,
            HasReferenceOutputAssembly = 1,
            HasOutputItemType = 2,
        }
    }

    public readonly struct Analyzer : IDisposable
    {
        private readonly Text include;

        public readonly ReadOnlySpan<char> Include => include.AsSpan();

        [Obsolete("Not supported", true)]
        public Analyzer() { }

        public Analyzer(ReadOnlySpan<char> include)
        {
            this.include = new Text(include);
        }

        public readonly void Dispose()
        {
            include.Dispose();
        }

        public readonly override string ToString()
        {
            return Include.ToString();
        }
    }
}