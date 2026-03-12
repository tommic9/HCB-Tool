using System;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Infrastructure.WithoutOpen;

public sealed class WithoutOpenFileMetadataService
{
    private readonly WithoutOpenFileClassifier _classifier = new();

    public WithoutOpenFileScanItem Scan(string filePath)
    {
        WithoutOpenFileKind fileKind = _classifier.GetFileKind(filePath);
        bool isCloudPath = _classifier.IsCloudPath(filePath);
        bool isLocalPath = _classifier.IsLocalOrUncPath(filePath);
        bool exists = File.Exists(filePath);
        long fileSize = exists ? new FileInfo(filePath).Length : 0L;

        if (!exists)
        {
            return new WithoutOpenFileScanItem
            {
                FilePath = filePath,
                FileKind = fileKind,
                Exists = false,
                FileSizeBytes = fileSize,
                IsLocalPath = isLocalPath,
                IsCloudPath = isCloudPath,
                Status = WithoutOpenOperationStatus.Failed,
                StatusMessage = "Plik nie istnieje."
            };
        }

        if (fileKind == WithoutOpenFileKind.Unknown)
        {
            return new WithoutOpenFileScanItem
            {
                FilePath = filePath,
                FileKind = fileKind,
                Exists = true,
                FileSizeBytes = fileSize,
                IsLocalPath = isLocalPath,
                IsCloudPath = isCloudPath,
                Status = WithoutOpenOperationStatus.Skipped,
                StatusMessage = "Nieobslugiwane rozszerzenie pliku."
            };
        }

        try
        {
            BasicFileInfo info = BasicFileInfo.Extract(filePath);
            return new WithoutOpenFileScanItem
            {
                FilePath = filePath,
                FileKind = fileKind,
                Exists = true,
                FileSizeBytes = fileSize,
                IsLocalPath = isLocalPath,
                IsCloudPath = isCloudPath,
                Format = info.Format ?? string.Empty,
                IsSavedInCurrentVersion = info.IsSavedInCurrentVersion,
                IsSavedInLaterVersion = info.IsSavedInLaterVersion,
                IsWorkshared = info.IsWorkshared,
                IsCentral = info.IsCentral,
                IsLocal = info.IsLocal,
                IsCreatedLocal = info.IsCreatedLocal,
                AllLocalChangesSavedToCentral = info.AllLocalChangesSavedToCentral,
                CentralPath = info.CentralPath ?? string.Empty,
                Username = info.Username ?? string.Empty,
                LanguageWhenSaved = info.LanguageWhenSaved.ToString(),
                ClientAppName = GetOptionalStringProperty(info, "ClientAppName"),
                Status = WithoutOpenOperationStatus.Success,
                StatusMessage = "OK"
            };
        }
        catch (Exception exception)
        {
            return new WithoutOpenFileScanItem
            {
                FilePath = filePath,
                FileKind = fileKind,
                Exists = true,
                FileSizeBytes = fileSize,
                IsLocalPath = isLocalPath,
                IsCloudPath = isCloudPath,
                Status = WithoutOpenOperationStatus.Failed,
                StatusMessage = exception.Message
            };
        }
    }

    private static string GetOptionalStringProperty(BasicFileInfo info, string propertyName)
    {
        object? value = typeof(BasicFileInfo)
            .GetProperty(propertyName)
            ?.GetValue(info);

        return value?.ToString() ?? string.Empty;
    }
}

