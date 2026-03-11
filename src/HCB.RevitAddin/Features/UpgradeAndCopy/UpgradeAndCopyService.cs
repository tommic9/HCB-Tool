using System;
using System.Text.RegularExpressions;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.UpgradeAndCopy.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.UpgradeAndCopy;

public sealed class UpgradeAndCopyService
{
    private static readonly Regex ExistingRevitSuffixRegex = new(@"(?:[_\- ]?R\d{2})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly WithoutOpenFileDiscoveryService _fileDiscoveryService = new();
    private readonly WithoutOpenFileMetadataService _fileMetadataService = new();
    private readonly WithoutOpenDocumentService _documentService = new();

    public UpgradeAndCopyResult Process(Application application, IEnumerable<string> filePaths, string outputFolderPath)
    {
        IReadOnlyList<string> normalizedPaths = _fileDiscoveryService.Normalize(filePaths);
        Directory.CreateDirectory(outputFolderPath);
        List<WithoutOpenOperationLogEntry> entries = new(normalizedPaths.Count);

        foreach (string filePath in normalizedPaths)
        {
            entries.Add(ProcessSingle(application, filePath, outputFolderPath));
        }

        return new UpgradeAndCopyResult
        {
            Entries = entries
        };
    }

    public UpgradeAndCopyResult ProcessUsingSourceFolders(Application application, IEnumerable<string> filePaths)
    {
        IReadOnlyList<string> normalizedPaths = _fileDiscoveryService.Normalize(filePaths);
        List<WithoutOpenOperationLogEntry> entries = new(normalizedPaths.Count);

        foreach (string filePath in normalizedPaths)
        {
            string? sourceFolderPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(sourceFolderPath))
            {
                entries.Add(new WithoutOpenOperationLogEntry
                {
                    FilePath = filePath,
                    OperationName = "Upgrade And Copy",
                    Status = WithoutOpenOperationStatus.Failed,
                    Message = "Nie udalo sie ustalic folderu zrodlowego."
                });
                continue;
            }

            Directory.CreateDirectory(sourceFolderPath);
            entries.Add(ProcessSingle(application, filePath, sourceFolderPath));
        }

        return new UpgradeAndCopyResult
        {
            Entries = entries
        };
    }

    private WithoutOpenOperationLogEntry ProcessSingle(Application application, string filePath, string outputFolderPath)
    {
        DateTime startedAt = DateTime.UtcNow;
        WithoutOpenFileScanItem scan = _fileMetadataService.Scan(filePath);
        string targetPath = BuildTargetPath(application, filePath, outputFolderPath);

        try
        {
            if (!scan.Exists)
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Failed, scan.StatusMessage, startedAt);
            }

            if (scan.IsCloudPath)
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Skipped, "Pominieto model chmurowy.", startedAt);
            }

            if (!scan.IsLocalPath)
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Skipped, "Pominieto sciezke nielokalna.", startedAt);
            }

            if (scan.IsSavedInLaterVersion)
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Skipped, "Pominieto plik zapisany w nowszej wersji Revita.", startedAt);
            }

            if (scan.IsSavedInCurrentVersion)
            {
                File.Copy(filePath, targetPath, true);
                return CreateLog(filePath, WithoutOpenOperationStatus.Success, $"Skopiowano bez upgrade: {targetPath}", startedAt, targetPath);
            }

            Document? document = null;
            try
            {
                document = _documentService.OpenDocument(application, filePath);
                SaveAsOptions saveAsOptions = new()
                {
                    OverwriteExistingFile = true
                };

                if (document.IsWorkshared)
                {
                    WorksharingSaveAsOptions worksharingOptions = new()
                    {
                        SaveAsCentral = false
                    };
                    saveAsOptions.SetWorksharingOptions(worksharingOptions);
                }

                document.SaveAs(targetPath, saveAsOptions);
            }
            finally
            {
                if (document != null)
                {
                    _documentService.CloseWithoutSave(document);
                }
            }

            return CreateLog(filePath, WithoutOpenOperationStatus.Success, $"Zapisano kopie po upgrade: {targetPath}", startedAt, targetPath);
        }
        catch (Exception exception)
        {
            return CreateLog(filePath, WithoutOpenOperationStatus.Failed, exception.Message, startedAt);
        }
    }

    private static string BuildTargetPath(Application application, string sourcePath, string outputFolderPath)
    {
        string extension = Path.GetExtension(sourcePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePath);
        string suffix = GetRevitSuffix(application);
        string baseName = ExistingRevitSuffixRegex.Replace(fileNameWithoutExtension, string.Empty);

        return Path.Combine(outputFolderPath, baseName + suffix + extension);
    }

    private static string GetRevitSuffix(Application application)
    {
        string versionNumber = application.VersionNumber ?? string.Empty;
        if (versionNumber.Length >= 4 && int.TryParse(versionNumber[^2..], out _))
        {
            return $"_R{versionNumber[^2..]}";
        }

        return "_RVT";
    }

    private static WithoutOpenOperationLogEntry CreateLog(string filePath, WithoutOpenOperationStatus status, string message, DateTime startedAt, string outputPath = "")
    {
        return new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            OperationName = "Upgrade And Copy",
            Status = status,
            Message = message,
            OutputPath = outputPath,
            Duration = DateTime.UtcNow - startedAt
        };
    }
}
