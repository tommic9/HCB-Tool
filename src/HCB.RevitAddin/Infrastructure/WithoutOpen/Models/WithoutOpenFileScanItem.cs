namespace HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

public sealed class WithoutOpenFileScanItem
{
    public string FilePath { get; init; } = string.Empty;

    public WithoutOpenFileKind FileKind { get; init; }

    public WithoutOpenOperationStatus Status { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    public string FileName => Path.GetFileName(FilePath);

    public long FileSizeBytes { get; init; }

    public bool IsLocalPath { get; init; }

    public bool IsCloudPath { get; init; }

    public bool Exists { get; init; }

    public string Format { get; init; } = string.Empty;

    public bool IsSavedInCurrentVersion { get; init; }

    public bool IsSavedInLaterVersion { get; init; }

    public bool IsWorkshared { get; init; }

    public bool IsCentral { get; init; }

    public bool IsLocal { get; init; }

    public bool IsCreatedLocal { get; init; }

    public bool AllLocalChangesSavedToCentral { get; init; }

    public string CentralPath { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string LanguageWhenSaved { get; init; } = string.Empty;

    public string ClientAppName { get; init; } = string.Empty;

    public bool CanUseTransmissionData => FileKind == WithoutOpenFileKind.Project && Exists && IsLocalPath && !IsCloudPath;

    public bool CanOpenInBackground => Exists && IsLocalPath && !IsCloudPath && FileKind is WithoutOpenFileKind.Project or WithoutOpenFileKind.Family;
}
