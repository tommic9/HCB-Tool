namespace HCB.RevitAddin.Features.Estimate.Models;

public sealed record EstimateUnmatchedRow(
    long ElementId,
    string Category,
    string TypeName,
    string Size,
    int? Angle,
    string Reason);
