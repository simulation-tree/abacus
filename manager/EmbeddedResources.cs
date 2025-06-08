using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace Abacus.Manager
{
    public static class EmbeddedResources
    {
        private static readonly string[] resourceNames;

        static EmbeddedResources()
        {
            Assembly assembly = typeof(EmbeddedResources).Assembly;
            resourceNames = assembly.GetManifestResourceNames();
        }

        public static bool TryGet(ReadOnlySpan<char> path, [NotNullWhen(true)] out string? text)
        {
            for (int i = 0; i < resourceNames.Length; i++)
            {
                string resourceName = resourceNames[i];
                if (resourceName.EndsWith(path.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    using Stream stream = typeof(EmbeddedResources).Assembly.GetManifestResourceStream(resourceName) ?? throw new($"Resource `{resourceName}` not found");
                    using StreamReader reader = new(stream);
                    text = reader.ReadToEnd();
                    return true;
                }
            }

            text = null;
            return false;
        }

        public static string? Get(ReadOnlySpan<char> path)
        {
            if (TryGet(path, out string? text))
            {
                return text;
            }
            else
            {
                return null;
            }
        }
    }
}