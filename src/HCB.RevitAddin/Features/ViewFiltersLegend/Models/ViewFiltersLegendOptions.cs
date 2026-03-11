namespace HCB.RevitAddin.Features.ViewFiltersLegend.Models;

public sealed class ViewFiltersLegendOptions
{
    public long TextTypeId { get; set; }

    public double SampleWidthMillimeters { get; set; } = 150;

    public double SampleHeightMillimeters { get; set; } = 80;

    public double SpacingMillimeters { get; set; } = 50;

    public double LineLengthMillimeters { get; set; } = 200;

    public bool IncludeProjectionLine { get; set; } = true;

    public bool IncludeCutLine { get; set; } = true;

    public bool IncludeSurfaceFill { get; set; } = true;

    public bool IncludeCutFill { get; set; } = true;

    public bool IncludeFilterName { get; set; } = true;
}
