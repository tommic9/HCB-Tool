using System.Collections.Generic;

namespace HCB.RevitAddin.Features.TransferViewTemplates.Models;

public sealed class TransferViewTemplatesResult
{
    public int CopiedCount { get; set; }

    public List<string> Messages { get; } = new();
}
