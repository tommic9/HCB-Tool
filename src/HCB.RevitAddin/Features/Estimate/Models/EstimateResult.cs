using System.Collections.Generic;

namespace HCB.RevitAddin.Features.Estimate.Models;

public sealed class EstimateResult
{
    public int UpdatedCount { get; set; }

    public int UnmatchedCount => UnmatchedRows.Count;

    public List<string> Messages { get; } = [];

    public List<EstimateUnmatchedRow> UnmatchedRows { get; } = [];

    public HashSet<long> MissingAngleElementIds { get; } = [];
}
