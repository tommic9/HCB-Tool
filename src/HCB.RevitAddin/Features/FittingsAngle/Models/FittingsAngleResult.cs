using System.Collections.Generic;

namespace HCB.RevitAddin.Features.FittingsAngle.Models;

public sealed class FittingsAngleResult
{
    public int CandidateCount { get; set; }

    public int UpdatedCount { get; set; }

    public int MissingSourceCount { get; set; }

    public int MissingTargetCount { get; set; }

    public List<long> FailedElementIds { get; } = new();
}
