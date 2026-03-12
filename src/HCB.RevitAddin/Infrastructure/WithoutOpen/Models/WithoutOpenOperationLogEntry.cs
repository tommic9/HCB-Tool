using System;
namespace HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

public sealed class WithoutOpenOperationLogEntry
{
    public string FilePath { get; init; } = string.Empty;

    public string OperationName { get; init; } = string.Empty;

    public WithoutOpenOperationStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public string OutputPath { get; init; } = string.Empty;

    public TimeSpan Duration { get; init; }
}
