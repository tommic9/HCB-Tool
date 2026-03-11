using System.Collections.Generic;

namespace HCB.RevitAddin.Features.ViewFiltersLegend.Models;

public sealed class ViewFiltersLegendResult
{
    public int LegendsCreated { get; set; }

    public long? LastLegendViewId { get; set; }

    public List<string> Messages { get; } = [];

    public List<ViewFiltersLegendItem> Items { get; } = [];
}

public sealed class ViewFiltersLegendItem
{
    public string SourceViewName { get; set; } = string.Empty;

    public string LegendViewName { get; set; } = string.Empty;

    public long LegendViewId { get; set; }

    public int FiltersCount { get; set; }
}
