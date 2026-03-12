using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using HCB.RevitAddin.Features.UpgradeAndCopy.Models;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.UpgradeAndCopy;

[Transaction(TransactionMode.Manual)]
public sealed class UpgradeAndCopyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        WithoutOpenDialogService dialogService = new();
        IReadOnlyList<string> filePaths = dialogService.PickRevitFiles();
        if (filePaths.Count == 0)
        {
            return Result.Cancelled;
        }

        UpgradeAndCopyService service = new();
        UpgradeAndCopyResult? result = AskForOutputMode(commandData, dialogService, service, filePaths);
        if (result == null)
        {
            return Result.Cancelled;
        }

        List<ReportPreviewColumn> columns =
        [
            new() { Key = "FileName", Header = "Plik" },
            new() { Key = "Status", Header = "Status" },
            new() { Key = "Message", Header = "Komunikat" },
            new() { Key = "OutputPath", Header = "Plik wynikowy" },
            new() { Key = "Duration", Header = "Czas [s]" },
            new() { Key = "FilePath", Header = "Sciezka" }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = result.Entries
            .Select(ToRow)
            .ToList();

        string summary =
            $"Pliki: {result.Entries.Count} | Sukces: {result.SuccessCount} | Pominiete: {result.SkippedCount} | Bledy: {result.FailedCount}";

        ReportPreviewWindow previewWindow = new(
            "WithoutOpen - Upgrade Copy",
            summary,
            columns,
            rows,
            "withoutopen-upgrade-copy.csv",
            outputPath =>
            {
                WithoutOpenBatchLogService logService = new();
                logService.ExportOperationsToCsv(result.Entries, outputPath);
            },
            "Otworz folder",
            OpenOutputFolder);

        previewWindow.ShowDialog();
        return Result.Succeeded;
    }

    private static UpgradeAndCopyResult? AskForOutputMode(
        ExternalCommandData commandData,
        WithoutOpenDialogService dialogService,
        UpgradeAndCopyService service,
        IReadOnlyList<string> filePaths)
    {
        TaskDialog dialog = new("WithoutOpen - Upgrade Copy")
        {
            MainInstruction = "Wybierz miejsce zapisu kopii po upgrade.",
            MainContent = "Mozesz zapisac kopie w tym samym folderze co pliki zrodlowe albo wskazac osobny folder docelowy.",
            CommonButtons = TaskDialogCommonButtons.Cancel
        };

        dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Zapisz w folderze zrodlowym");
        dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Wybierz osobny folder docelowy");

        TaskDialogResult decision = dialog.Show();
        if (decision == TaskDialogResult.CommandLink1)
        {
            return service.ProcessUsingSourceFolders(commandData.Application.Application, filePaths);
        }

        if (decision != TaskDialogResult.CommandLink2)
        {
            return null;
        }

        string? outputFolderPath = dialogService.PickFolderPath("Wybierz folder docelowy dla kopii po upgrade.");
        if (string.IsNullOrWhiteSpace(outputFolderPath))
        {
            return null;
        }

        return service.Process(commandData.Application.Application, filePaths, outputFolderPath);
    }

    private static IReadOnlyDictionary<string, string> ToRow(WithoutOpenOperationLogEntry entry)
    {
        return new Dictionary<string, string>
        {
            ["FileName"] = Path.GetFileName(entry.FilePath),
            ["Status"] = entry.Status.ToString(),
            ["Message"] = entry.Message,
            ["OutputPath"] = entry.OutputPath,
            ["Duration"] = entry.Duration.TotalSeconds.ToString("0.###"),
            ["FilePath"] = entry.FilePath
        };
    }

    private static void OpenOutputFolder(IReadOnlyDictionary<string, string> row)
    {
        string candidatePath = row.TryGetValue("OutputPath", out string? outputPath) && !string.IsNullOrWhiteSpace(outputPath)
            ? outputPath
            : row.TryGetValue("FilePath", out string? filePath) ? filePath ?? string.Empty : string.Empty;

        string? directoryPath = File.Exists(candidatePath)
            ? Path.GetDirectoryName(candidatePath)
            : Directory.Exists(candidatePath) ? candidatePath : Path.GetDirectoryName(candidatePath);

        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            TaskDialog.Show("WithoutOpen - Upgrade Copy", "Nie udalo sie otworzyc folderu dla wybranego wiersza.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = directoryPath,
            UseShellExecute = true
        });
    }
}

