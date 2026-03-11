using System;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Infrastructure.WithoutOpen;

public sealed class WithoutOpenFileClassifier
{
    public WithoutOpenFileKind GetFileKind(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        if (string.Equals(extension, ".rvt", StringComparison.OrdinalIgnoreCase))
        {
            return WithoutOpenFileKind.Project;
        }

        if (string.Equals(extension, ".rfa", StringComparison.OrdinalIgnoreCase))
        {
            return WithoutOpenFileKind.Family;
        }

        return WithoutOpenFileKind.Unknown;
    }

    public bool IsCloudPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        return filePath.StartsWith("Autodesk Docs://", StringComparison.OrdinalIgnoreCase) ||
               filePath.StartsWith("BIM 360://", StringComparison.OrdinalIgnoreCase) ||
               filePath.StartsWith("ACC://", StringComparison.OrdinalIgnoreCase) ||
               filePath.StartsWith("RSN://", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsLocalOrUncPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        if (IsCloudPath(filePath))
        {
            return false;
        }

        return Path.IsPathRooted(filePath);
    }
}

