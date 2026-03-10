using System.Collections.Generic;

namespace HCB.RevitAddin.Features.ViewFiltersBulkEdit.Models;

public sealed class ViewFiltersBulkEditResult
{
    public int UpdatedViewsCount { get; set; }

    public int UpdatedFiltersCount { get; set; }

    public List<string> Messages { get; } = new();
}
