using System.Collections.Generic;

namespace HCB.RevitAddin.Features.Estimate.Models;

public sealed class EstimateResult
{
    public int ProcessedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int UnmatchedCount => UnmatchedRows.Count;

    public double TotalCost { get; set; }

    public double TotalAreaSquareMeters { get; set; }

    public double TotalLengthMeters { get; set; }

    public List<string> Messages { get; } = [];

    public List<EstimateAppliedRow> AppliedRows { get; } = [];

    public List<EstimateUnmatchedRow> UnmatchedRows { get; } = [];

    public HashSet<long> MissingAngleElementIds { get; } = [];
}

public sealed record EstimateAppliedRow(
    long ElementId,
    string Category,
    string TypeName,
    string Size,
    int? Angle,
    string PricingBasis,
    double Quantity,
    string QuantityUnit,
    double UnitPrice,
    double Cost);
