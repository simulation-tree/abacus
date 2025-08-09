using Collections.Generic;
using XML;
using System;
using System.IO;
using Unmanaged;

namespace Abacus.Manager
{
    public readonly struct Solution : IDisposable
    {
        private const string ProjectNode = "Project";
        private const string PathAttribute = "Path";

        public readonly XMLNode rootNode;

        private readonly Text path;
        private readonly List<Project> projects;

        /// <summary>
        /// Name of the solution based on the file name.
        /// </summary>
        public readonly ReadOnlySpan<char> Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        /// <summary>
        /// Path to the solution file.
        /// </summary>
        public readonly ReadOnlySpan<char> Path => path.AsSpan();

        /// <summary>
        /// All projects part of this solution.
        /// </summary>
        public readonly ReadOnlySpan<Project> Projects => projects.AsSpan();

        public Solution(ReadOnlySpan<char> path)
        {
            this.path = new(path);
            string directoryPath = System.IO.Path.GetDirectoryName(this.path.ToString()) ?? string.Empty;

            using FileStream fileStream = File.OpenRead(this.path.ToString());
            using ByteReader reader = new(fileStream);
            rootNode = reader.ReadObject<XMLNode>();
            projects = new();

            using Stack<XMLNode> stack = new();
            stack.Push(rootNode);

            while (stack.TryPop(out XMLNode node))
            {
                if (node.Name.Equals(ProjectNode))
                {
                    if (node.TryGetAttribute(PathAttribute, out ReadOnlySpan<char> relativePath))
                    {
                        string absolutePath = System.IO.Path.Combine(directoryPath, relativePath.ToString());
                        projects.Add(new Project(absolutePath));
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
        }

        public readonly void Dispose()
        {
            foreach (Project project in projects)
            {
                project.Dispose();
            }

            projects.Dispose();
            rootNode.Dispose();
            path.Dispose();
        }

        public readonly override string ToString()
        {
            return path.ToString();
        }
    }
}