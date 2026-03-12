using System.Collections.Generic;

namespace HCB.RevitAddin.Features.DuctFittingNumbering.Models;

public sealed class DuctFittingNumberingResult
{
    public int SystemsCount { get; set; }

    public int DuctCount { get; set; }

    public int FittingCount { get; set; }

    public int TotalCount { get; set; }

    public int SharedNumberCount { get; set; }

    public string LengthParameterName { get; set; } = string.Empty;

    public string TargetParameterName { get; set; } = string.Empty;

    public List<string> Messages { get; } = [];
}
