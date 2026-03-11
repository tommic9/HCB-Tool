using System.Collections.Generic;

namespace HCB.RevitAddin.Features.DuctFittingsArea.Models;

public sealed class DuctFittingsAreaResult
{
    public int ProcessedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public double TotalAreaSquareMeters { get; set; }

    public List<string> Messages { get; } = new();

    public List<DuctFittingsAreaRow> Rows { get; } = [];
}

public sealed record DuctFittingsAreaRow(
    long ElementId,
    string Category,
    string FamilyName,
    string TypeName,
    string Size,
    string Status,
    double? AreaSquareMeters,
    string Source,
    string Reason);
