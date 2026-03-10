using System.Collections.Generic;

namespace HCB.RevitAddin.Features.NumberingSystemElements.Models;

public sealed class NumberingSystemElementsResult
{
    public int UpdatedCount { get; set; }

    public int GroupCount { get; set; }

    public List<string> Messages { get; } = [];
}
