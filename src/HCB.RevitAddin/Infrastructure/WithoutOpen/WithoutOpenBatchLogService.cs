using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Infrastructure.WithoutOpen;

public sealed class WithoutOpenBatchLogService
{
    public void ExportScanToCsv(IEnumerable<WithoutOpenFileScanItem> items, string outputPath)
    {
        StringBuilder builder = new();
        builder.AppendLine("FilePath,FileKind,Status,StatusMessage,FileSizeBytes,Format,IsSavedInCurrentVersion,IsSavedInLaterVersion,IsWorkshared,IsCentral,IsLocal,IsCreatedLocal,AllLocalChangesSavedToCentral,IsLocalPath,IsCloudPath,CentralPath,Username,LanguageWhenSaved,ClientAppName");

        foreach (WithoutOpenFileScanItem item in items)
        {
            builder.AppendLine(string.Join(",",
                Escape(item.FilePath),
                Escape(item.FileKind.ToString()),
                Escape(item.Status.ToString()),
                Escape(item.StatusMessage),
                item.FileSizeBytes.ToString(CultureInfo.InvariantCulture),
                Escape(item.Format),
                item.IsSavedInCurrentVersion.ToString(),
                item.IsSavedInLaterVersion.ToString(),
                item.IsWorkshared.ToString(),
                item.IsCentral.ToString(),
                item.IsLocal.ToString(),
                item.IsCreatedLocal.ToString(),
                item.AllLocalChangesSavedToCentral.ToString(),
                item.IsLocalPath.ToString(),
                item.IsCloudPath.ToString(),
                Escape(item.CentralPath),
                Escape(item.Username),
                Escape(item.LanguageWhenSaved),
                Escape(item.ClientAppName)));
        }

        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
    }

    public void ExportOperationsToCsv(IEnumerable<WithoutOpenOperationLogEntry> entries, string outputPath)
    {
        StringBuilder builder = new();
        builder.AppendLine("FilePath,OperationName,Status,Message,OutputPath,DurationSeconds");

        foreach (WithoutOpenOperationLogEntry entry in entries)
        {
            builder.AppendLine(string.Join(",",
                Escape(entry.FilePath),
                Escape(entry.OperationName),
                Escape(entry.Status.ToString()),
                Escape(entry.Message),
                Escape(entry.OutputPath),
                entry.Duration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
    }

    private static string Escape(string? value)
    {
        string normalized = value ?? string.Empty;
        return $"\"{normalized.Replace("\"", "\"\"")}\"";
    }
}
