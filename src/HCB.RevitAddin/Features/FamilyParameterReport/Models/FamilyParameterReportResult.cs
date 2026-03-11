namespace HCB.RevitAddin.Features.FamilyParameterReport.Models;

public sealed class FamilyParameterReportResult
{
    public IReadOnlyList<FamilyParameterReportRow> Rows { get; init; } = [];

    public IReadOnlyList<string> FailedFiles { get; init; } = [];
}
