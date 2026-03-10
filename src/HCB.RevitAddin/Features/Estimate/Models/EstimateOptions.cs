namespace HCB.RevitAddin.Features.Estimate.Models;

public sealed class EstimateOptions
{
    public string CatalogPath { get; init; } = string.Empty;

    public bool AddFoil { get; init; }
}
