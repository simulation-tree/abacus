using Serialization.XML;
using System;
using System.Collections.Generic;
using Unmanaged;

public class Project
{
    public readonly bool isTestProject;

    private readonly string path;
    private readonly string name;
    private readonly List<ProjectReference> projectReferences;
    private readonly List<PackageReference> packageReferences;

    public ReadOnlySpan<char> Name => name;
    public ReadOnlySpan<char> Path => path;
    public IReadOnlyCollection<ProjectReference> ProjectReferences => projectReferences;
    public IReadOnlyCollection<PackageReference> PackageReferences => packageReferences;

    public Project(ReadOnlySpan<char> path)
    {
        this.path = path.ToString();
        this.name = System.IO.Path.GetFileNameWithoutExtension(this.path);
        this.projectReferences = new();
        this.packageReferences = new();

        using System.IO.FileStream fileStream = System.IO.File.OpenRead(this.path);
        using BinaryReader reader = new(fileStream);
        using XMLNode rootNode = reader.ReadObject<XMLNode>();
        Stack<XMLNode> stack = new();
        stack.Push(rootNode);

        while (stack.Count > 0)
        {
            XMLNode node = stack.Pop();
            if (node.Name.SequenceEqual("ProjectReference".AsSpan()))
            {
                if (node.TryGetAttribute("Include", out USpan<char> referencedProjectPath))
                {
                    projectReferences.Add(new(referencedProjectPath));
                }
            }
            else if (node.Name.SequenceEqual("PackageReference".AsSpan()))
            {
                if (node.TryGetAttribute("Include", out USpan<char> referencedProjectPath) && node.TryGetAttribute("Version", out USpan<char> version))
                {
                    packageReferences.Add(new(referencedProjectPath, version));
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

        isTestProject = ContainsTestPackages();
    }

    private bool ContainsTestPackages()
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
        return path;
    }

    public class PackageReference : Reference
    {
        private readonly string version;

        public ReadOnlySpan<char> Version => version;

        public PackageReference(ReadOnlySpan<char> include, ReadOnlySpan<char> version) : base(include)
        {
            this.version = version.ToString();
        }
    }

    public class ProjectReference : Reference
    {
        public ProjectReference(ReadOnlySpan<char> include) : base(include)
        {
        }
    }

    public class Reference
    {
        private readonly string include;

        public ReadOnlySpan<char> Include => include;

        public Reference(ReadOnlySpan<char> include)
        {
            this.include = include.ToString();
        }
    }
}