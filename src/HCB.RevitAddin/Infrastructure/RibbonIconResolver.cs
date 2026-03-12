using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin
{
    internal static class RibbonIconResolver
    {
        private static readonly ConcurrentDictionary<string, string?> ResolvedPathCache = new(StringComparer.OrdinalIgnoreCase);

        public static void ApplyTo(ButtonData buttonData, Type commandType, string? resourceDirectoryOverride = null)
        {
            string assemblyDir = Path.GetDirectoryName(commandType.Assembly.Location) ?? string.Empty;
            string featureResourcesDir = string.IsNullOrWhiteSpace(resourceDirectoryOverride)
                ? GetFeatureResourcesDir(assemblyDir, commandType)
                : Path.Combine(assemblyDir, resourceDirectoryOverride);
            string sharedResourcesDir = Path.Combine(assemblyDir, "Resources");

            string? largeIconPath = ResolveIconPath(featureResourcesDir, sharedResourcesDir, "icon32.png", "icon.png");
            string? smallIconPath = ResolveIconPath(featureResourcesDir, sharedResourcesDir, "icon16.png", "icon32.png", "icon.png");

            if (largeIconPath != null)
            {
                buttonData.LargeImage = ImageLoader.LoadPng(largeIconPath, 32);
            }

            if (smallIconPath != null)
            {
                buttonData.Image = ImageLoader.LoadPng(smallIconPath, 16);
            }
        }

        private static string GetFeatureResourcesDir(string assemblyDir, Type commandType)
        {
            string? namespaceName = commandType.Namespace;
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return Path.Combine(assemblyDir, "Resources");
            }

            string[] parts = namespaceName.Split('.');
            int featuresIndex = Array.IndexOf(parts, "Features");
            if (featuresIndex < 0 || featuresIndex >= parts.Length - 1)
            {
                return Path.Combine(assemblyDir, "Resources");
            }

            return Path.Combine(assemblyDir, "Features", parts[featuresIndex + 1], "Resources");
        }

        private static string? ResolveIconPath(string primaryDir, string fallbackDir, params string[] fileNames)
        {
            string cacheKey = string.Join("|", new[] { primaryDir, fallbackDir }.Concat(fileNames));
            return ResolvedPathCache.GetOrAdd(cacheKey, _ =>
            {
                foreach (string fileName in fileNames)
                {
                    string primaryPath = Path.Combine(primaryDir, fileName);
                    if (File.Exists(primaryPath))
                    {
                        return primaryPath;
                    }

                    string fallbackPath = Path.Combine(fallbackDir, fileName);
                    if (File.Exists(fallbackPath))
                    {
                        return fallbackPath;
                    }
                }

                return null;
            });
        }
    }
}
