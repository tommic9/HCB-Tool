namespace HCB.RevitAddin.Features.CadLinkGraphicsCopy.Models;

public sealed class CadLinkGraphicsCopyResult
{
    public int SourceLayerCount { get; set; }

    public int MatchedLayerCount { get; set; }

    public int UpdatedCategoryCount { get; set; }

    public int MissingLayerCount { get; set; }

    public List<string> Messages { get; } = [];
}
