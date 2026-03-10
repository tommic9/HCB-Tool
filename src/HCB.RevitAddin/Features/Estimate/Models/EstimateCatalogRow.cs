using System;

namespace HCB.RevitAddin.Features.Estimate.Models;

internal sealed record EstimateCatalogRow(
    string Category,
    string Shape,
    string MatchKey,
    double? SizePatternLow,
    double? SizePatternHigh,
    int? Angle,
    string Unit,
    double UnitPrice,
    string TypeNamePattern)
{
    public int? RectUpperBoundMillimeters =>
        string.Equals(Shape, "rect", StringComparison.OrdinalIgnoreCase)
            ? (int?)Math.Round(SizePatternHigh ?? SizePatternLow ?? 0)
            : null;
}
