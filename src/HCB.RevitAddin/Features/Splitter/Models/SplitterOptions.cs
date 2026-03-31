namespace HCB.RevitAddin.Features.Splitter.Models;

public sealed class SplitterOptions
{
    public double SegmentLengthMillimeters { get; init; }

    public double SegmentLengthInternal { get; init; }

    public bool SplitDucts { get; init; }

    public bool SplitPipes { get; init; }
}
