using System;
using System.Collections.Generic;
namespace HCB.RevitAddin.Infrastructure.WithoutOpen;

public sealed class WithoutOpenFileDiscoveryService
{
    public IReadOnlyList<string> Normalize(IEnumerable<string> filePaths)
    {
        HashSet<string> uniquePaths = new(StringComparer.OrdinalIgnoreCase);
        List<string> normalized = [];

        foreach (string? filePath in filePaths)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(filePath.Trim());
            }
            catch
            {
                continue;
            }

            if (uniquePaths.Add(fullPath))
            {
                normalized.Add(fullPath);
            }
        }

        return normalized;
    }
}


