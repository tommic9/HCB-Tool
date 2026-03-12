using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.FamilyParameterReport.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;
using System.Text;

namespace HCB.RevitAddin.Features.FamilyParameterReport;

public sealed class FamilyParameterReportService
{
    private readonly WithoutOpenFileDiscoveryService _fileDiscoveryService = new();
    private readonly WithoutOpenFileClassifier _fileClassifier = new();
    private readonly WithoutOpenDocumentService _documentService = new();

    public FamilyParameterReportResult BuildReport(Application application, IEnumerable<string> filePaths)
    {
        IReadOnlyList<string> normalizedPaths = _fileDiscoveryService.Normalize(filePaths);
        List<FamilyParameterReportRow> rows = new();
        List<string> failedFiles = new();

        foreach (string filePath in normalizedPaths)
        {
            if (_fileClassifier.GetFileKind(filePath) != WithoutOpenFileKind.Family)
            {
                continue;
            }

            Document? document = null;
            try
            {
                document = _documentService.OpenDocument(application, filePath);
                if (!document.IsFamilyDocument)
                {
                    failedFiles.Add(filePath);
                    continue;
                }

                FamilyManager familyManager = document.FamilyManager;
                int typeCount = familyManager.Types.Size;
                foreach (FamilyParameter parameter in familyManager.Parameters)
                {
                    bool isBuiltIn = IsBuiltInParameter(parameter);
                    bool canRename = CanRenameParameter(parameter);

                    rows.Add(new FamilyParameterReportRow
                    {
                        FilePath = filePath,
                        FamilyName = document.Title,
                        TypeCount = typeCount,
                        ParameterName = parameter.Definition?.Name ?? string.Empty,
                        IsShared = parameter.IsShared,
                        IsInstance = parameter.IsInstance,
                        IsBuiltIn = isBuiltIn,
                        CanRename = canRename,
                        ParameterSource = GetParameterSourceLabel(parameter, isBuiltIn),
                        GroupTypeId = parameter.Definition?.GetGroupTypeId()?.TypeId ?? string.Empty,
                        SpecTypeId = parameter.Definition?.GetDataType()?.TypeId ?? string.Empty,
                        Formula = parameter.Formula ?? string.Empty
                    });
                }
            }
            catch
            {
                failedFiles.Add(filePath);
            }
            finally
            {
                if (document != null)
                {
                    _documentService.CloseWithoutSave(document);
                }
            }
        }

        return new FamilyParameterReportResult
        {
            Rows = rows,
            FailedFiles = failedFiles
        };
    }

    public void ExportCsv(IEnumerable<FamilyParameterReportRow> rows, string outputPath)
    {
        StringBuilder builder = new();
        builder.AppendLine("FilePath,FamilyName,TypeCount,ParameterName,ParameterSource,IsShared,IsInstance,IsBuiltIn,CanRename,GroupTypeId,SpecTypeId,Formula");

        foreach (FamilyParameterReportRow row in rows)
        {
            builder.AppendLine(string.Join(",",
                Escape(row.FilePath),
                Escape(row.FamilyName),
                row.TypeCount.ToString(CultureInfo.InvariantCulture),
                Escape(row.ParameterName),
                Escape(row.ParameterSource),
                row.IsShared.ToString(),
                row.IsInstance.ToString(),
                row.IsBuiltIn.ToString(),
                row.CanRename.ToString(),
                Escape(row.GroupTypeId),
                Escape(row.SpecTypeId),
                Escape(row.Formula)));
        }

        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
    }

    private static bool CanRenameParameter(FamilyParameter parameter)
    {
        return !parameter.IsShared && !IsBuiltInParameter(parameter);
    }

    private static bool IsBuiltInParameter(FamilyParameter parameter)
    {
        return parameter.Definition is InternalDefinition internalDefinition &&
               internalDefinition.BuiltInParameter != BuiltInParameter.INVALID;
    }

    private static string GetParameterSourceLabel(FamilyParameter parameter, bool isBuiltIn)
    {
        if (parameter.IsShared)
        {
            return "Shared";
        }

        if (isBuiltIn)
        {
            return "System";
        }

        return "Family";
    }

    private static string Escape(string? value)
    {
        string normalized = value ?? string.Empty;
        return $"\"{normalized.Replace("\"", "\"\"")}\"";
    }
}
