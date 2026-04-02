using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using HCB.RevitAddin.Features.Splitter.Models;

namespace HCB.RevitAddin.Features.Splitter;

public sealed class SplitterService
{
    private const double GeometryTolerance = 1e-6;

    public bool IsSupported(Element element, SplitterOptions options)
    {
        return element switch
        {
            Duct => options.SplitDucts,
            Pipe => options.SplitPipes,
            _ => false
        };
    }

    public SplitterResult SplitSelectedElement(Document document, Reference reference, SplitterOptions options)
    {
        Element? element = document.GetElement(reference);
        if (element is not MEPCurve curve)
        {
            return BuildFailure(reference.ElementId.Value, "Wybrany element nie jest obslugiwanym ducts lub pipe.");
        }

        if (!IsSupported(curve, options))
        {
            return BuildFailure(curve.Id.Value, "Wybrany element jest poza aktualnym zakresem narzedzia.");
        }

        if (!TryGetOrientedLine(curve, reference, out _, out XYZ selectedStartPoint, out XYZ selectedEndPoint, out string? errorMessage))
        {
            return BuildFailure(curve.Id.Value, errorMessage ?? "Nie udalo sie odczytac geometrii elementu.");
        }

        StraightRunScan runScan = CollectStraightRun(curve, selectedStartPoint, selectedEndPoint);
        if (!runScan.Success)
        {
            return BuildFailure(curve.Id.Value, runScan.Message);
        }

        MEPCurve workingCurve = curve;
        XYZ workingStart = runScan.StartPoint;
        XYZ workingEnd = runScan.EndPoint;
        int rebuiltRuns = 0;
        int removedConnectorCount = 0;

        if (runScan.RequiresRebuild)
        {
            StraightRunRebuild rebuild = RebuildStraightRun(document, runScan);
            if (!rebuild.Success || rebuild.Curve == null)
            {
                return BuildFailure(curve.Id.Value, rebuild.Message);
            }

            workingCurve = rebuild.Curve;
            workingStart = rebuild.StartPoint;
            workingEnd = rebuild.EndPoint;
            rebuiltRuns = 1;
            removedConnectorCount = rebuild.RemovedElementCount;
        }

        Line orientedLine;
        if (!TryGetOrientedLine(workingCurve, workingStart, workingEnd, out orientedLine, out string? orientedErrorMessage))
        {
            return BuildFailure(workingCurve.Id.Value, orientedErrorMessage ?? "Nie udalo sie przygotowac ciagu do podzialu.");
        }

        if (orientedLine.Length <= options.SegmentLengthInternal + GeometryTolerance)
        {
            return rebuiltRuns > 0
                ? BuildRebuildOnly(workingCurve.Id.Value, removedConnectorCount, "ciag przebudowano bez nowych podzialow")
                : BuildSkipped(workingCurve.Id.Value, options, "za krotki na jeden pelny odcinek");
        }

        SplitCalibration calibration = Calibrate(document, workingCurve, orientedLine);
        if (!calibration.Success)
        {
            return BuildFailure(workingCurve.Id.Value, calibration.Message);
        }

        double minimumLengthForNextSplit = options.SegmentLengthInternal + calibration.NearTrim + calibration.FarTrim + GeometryTolerance;
        if (orientedLine.Length <= minimumLengthForNextSplit)
        {
            return rebuiltRuns > 0
                ? BuildRebuildOnly(workingCurve.Id.Value, removedConnectorCount, "ciag przebudowano bez nowych podzialow")
                : BuildSkipped(workingCurve.Id.Value, options, "za krotki na odcinki z lacznikiem");
        }

        MEPCurve currentCurve = workingCurve;
        XYZ currentStart = workingStart;
        XYZ currentEnd = workingEnd;
        int splitCount = 0;
        int fittingCount = 0;

        while (true)
        {
            Line currentLine;
            if (!TryGetOrientedLine(currentCurve, currentStart, currentEnd, out currentLine, out string? loopErrorMessage))
            {
                return BuildFailure(workingCurve.Id.Value, loopErrorMessage ?? "Podzial zostal przerwany.");
            }

            double remainingLength = currentLine.Length;
            if (remainingLength <= minimumLengthForNextSplit)
            {
                break;
            }

            XYZ breakPoint = currentLine.GetEndPoint(0) + currentLine.Direction.Multiply(options.SegmentLengthInternal + calibration.NearTrim);
            SplitOperation splitOperation = BreakAndConnect(document, currentCurve, breakPoint, currentStart, currentEnd);

            splitCount++;
            fittingCount++;
            currentCurve = splitOperation.FarCurve;
            currentStart = splitOperation.FarStartPoint;
            currentEnd = splitOperation.FarEndPoint;
        }

        if (splitCount == 0)
        {
            return rebuiltRuns > 0
                ? BuildRebuildOnly(workingCurve.Id.Value, removedConnectorCount, "ciag przebudowano bez nowych podzialow")
                : BuildSkipped(workingCurve.Id.Value, options, "za krotki na odcinki z lacznikiem");
        }

        string rebuildSuffix = rebuiltRuns > 0
            ? $" Ciag przebudowano (usunieto {removedConnectorCount} wewnetrznych elementow)."
            : string.Empty;

        return new SplitterResult
        {
            Success = true,
            HasChanges = true,
            ElementId = workingCurve.Id.Value,
            SplitCount = splitCount,
            FittingCount = fittingCount,
            Message = $"Element {workingCurve.Id.Value}: wykonano {splitCount} podzial(ow) i wstawiono {fittingCount} lacznik(i).{rebuildSuffix}"
        };
    }

    private static StraightRunScan CollectStraightRun(MEPCurve seedCurve, XYZ selectedStartPoint, XYZ selectedEndPoint)
    {
        XYZ axisDirection = (selectedEndPoint - selectedStartPoint).Normalize();

        Connector startConnector = GetClosestEndConnector(seedCurve, selectedStartPoint);
        Connector endConnector = GetClosestEndConnector(seedCurve, selectedEndPoint);

        RunSideScan startSide = WalkRunSide(seedCurve, startConnector, axisDirection);
        if (!startSide.Success)
        {
            return new StraightRunScan { Success = false, Message = startSide.Message };
        }

        RunSideScan endSide = WalkRunSide(seedCurve, endConnector, axisDirection);
        if (!endSide.Success)
        {
            return new StraightRunScan { Success = false, Message = endSide.Message };
        }

        List<MEPCurve> orderedSegments = [];
        orderedSegments.AddRange(startSide.Segments.AsEnumerable().Reverse());
        orderedSegments.Add(seedCurve);
        orderedSegments.AddRange(endSide.Segments);

        return new StraightRunScan
        {
            Success = true,
            OrderedSegments = orderedSegments,
            InternalElementIds = startSide.InternalElementIds.Concat(endSide.InternalElementIds).Distinct().ToList(),
            StartBoundaryConnector = startSide.BoundaryConnector,
            EndBoundaryConnector = endSide.BoundaryConnector,
            StartPoint = startSide.TerminalPoint,
            EndPoint = endSide.TerminalPoint
        };
    }

    private static RunSideScan WalkRunSide(MEPCurve seedCurve, Connector seedConnector, XYZ axisDirection)
    {
        List<MEPCurve> segments = [];
        List<ElementId> internalElementIds = [];
        HashSet<long> visitedElementIds = [seedCurve.Id.Value];

        Connector currentConnector = seedConnector;

        while (true)
        {
            List<Connector> externalReferences = GetExternalReferences(currentConnector);
            if (externalReferences.Count == 0)
            {
                return new RunSideScan
                {
                    Success = true,
                    Segments = segments,
                    InternalElementIds = internalElementIds,
                    TerminalPoint = currentConnector.Origin
                };
            }

            if (externalReferences.Count > 1)
            {
                return new RunSideScan
                {
                    Success = false,
                    Message = $"Element {seedCurve.Id.Value}: znaleziono rozgalezienie na prostym ciagu."
                };
            }

            Connector neighborConnector = externalReferences[0];
            Element neighborOwner = neighborConnector.Owner;
            if (!visitedElementIds.Add(neighborOwner.Id.Value))
            {
                return new RunSideScan
                {
                    Success = false,
                    Message = $"Element {seedCurve.Id.Value}: wykryto petle w analizowanym ciagu."
                };
            }

            if (neighborOwner is MEPCurve nextCurve)
            {
                if (!IsCurveContinuation(nextCurve, neighborConnector, currentConnector, axisDirection))
                {
                    return new RunSideScan
                    {
                        Success = true,
                        Segments = segments,
                        InternalElementIds = internalElementIds,
                        BoundaryConnector = neighborConnector,
                        TerminalPoint = neighborConnector.Origin
                    };
                }

                segments.Add(nextCurve);
                currentConnector = GetOppositeCurveConnector(nextCurve, neighborConnector);
                continue;
            }

            if (IsInlinePassThroughFitting(neighborOwner, neighborConnector, currentConnector, axisDirection, out Connector? exitConnector))
            {
                internalElementIds.Add(neighborOwner.Id);
                currentConnector = exitConnector!;
                continue;
            }

            return new RunSideScan
            {
                Success = true,
                Segments = segments,
                InternalElementIds = internalElementIds,
                BoundaryConnector = neighborConnector,
                TerminalPoint = neighborConnector.Origin
            };
        }
    }

    private static StraightRunRebuild RebuildStraightRun(Document document, StraightRunScan runScan)
    {
        if (runScan.OrderedSegments.Count == 0)
        {
            return new StraightRunRebuild { Success = false, Message = "Nie znaleziono zadnych segmentow do przebudowy." };
        }

        MEPCurve survivingCurve = runScan.OrderedSegments[0];
        List<ElementId> deleteIds = runScan.InternalElementIds
            .Concat(runScan.OrderedSegments.Skip(1).Select(segment => segment.Id))
            .Distinct()
            .ToList();

        if (deleteIds.Count > 0)
        {
            document.Delete(deleteIds);
        }

        survivingCurve = (MEPCurve)document.GetElement(survivingCurve.Id);
        if (survivingCurve.Location is not LocationCurve locationCurve)
        {
            return new StraightRunRebuild
            {
                Success = false,
                Message = $"Element {survivingCurve.Id.Value}: nie udalo sie odtworzyc geometrii po scaleniu."
            };
        }

        locationCurve.Curve = Line.CreateBound(runScan.StartPoint, runScan.EndPoint);

        survivingCurve = (MEPCurve)document.GetElement(survivingCurve.Id);
        ReconnectBoundary(survivingCurve, runScan.StartPoint, runScan.StartBoundaryConnector);
        ReconnectBoundary(survivingCurve, runScan.EndPoint, runScan.EndBoundaryConnector);

        return new StraightRunRebuild
        {
            Success = true,
            Curve = survivingCurve,
            StartPoint = runScan.StartPoint,
            EndPoint = runScan.EndPoint,
            RemovedElementCount = deleteIds.Count
        };
    }

    private static void ReconnectBoundary(MEPCurve curve, XYZ boundaryPoint, Connector? boundaryConnector)
    {
        if (boundaryConnector == null)
        {
            return;
        }

        Connector curveConnector = GetClosestEndConnector(curve, boundaryPoint);
        if (IsConnectedTo(curveConnector, boundaryConnector))
        {
            return;
        }

        curveConnector.ConnectTo(boundaryConnector);
    }

    private static bool IsConnectedTo(Connector source, Connector target)
    {
        return source.AllRefs
            .Cast<Connector>()
            .Any(reference => reference.Owner.Id == target.Owner.Id && reference.Id == target.Id);
    }

    private static bool TryGetOrientedLine(MEPCurve curve, Reference reference, out Line? orientedLine, out XYZ startPoint, out XYZ endPoint, out string? errorMessage)
    {
        orientedLine = null;
        startPoint = XYZ.Zero;
        endPoint = XYZ.Zero;
        errorMessage = null;

        if (curve.Location is not LocationCurve { Curve: Line baseLine })
        {
            errorMessage = "Element nie ma liniowej geometrii.";
            return false;
        }

        List<Connector> endConnectors = GetEndConnectors(curve);
        if (endConnectors.Count != 2)
        {
            errorMessage = "Element nie ma dwoch konektorow koncowych.";
            return false;
        }

        XYZ pickedPoint = reference.GlobalPoint ?? baseLine.Evaluate(0.5, true);
        Connector startConnector = endConnectors.OrderBy(connector => connector.Origin.DistanceTo(pickedPoint)).First();
        Connector endConnector = endConnectors.First(connector => connector.Id != startConnector.Id);

        startPoint = startConnector.Origin;
        endPoint = endConnector.Origin;
        orientedLine = CreateOrientedLine(baseLine, startPoint, endPoint);
        return true;
    }

    private static bool TryGetOrientedLine(MEPCurve curve, XYZ startPoint, XYZ endPoint, out Line? orientedLine, out string? errorMessage)
    {
        orientedLine = null;
        errorMessage = null;

        if (curve.Location is not LocationCurve { Curve: Line baseLine })
        {
            errorMessage = "Element przestal byc linia po kolejnym podziale.";
            return false;
        }

        orientedLine = CreateOrientedLine(baseLine, startPoint, endPoint);
        return true;
    }

    private static SplitCalibration Calibrate(Document document, MEPCurve curve, Line orientedLine)
    {
        double totalLength = orientedLine.Length;
        double sampleOffset = totalLength / 2d;

        using SubTransaction subTransaction = new(document);
        subTransaction.Start();

        try
        {
            XYZ breakPoint = orientedLine.GetEndPoint(0) + orientedLine.Direction.Multiply(sampleOffset);
            SplitOperation operation = BreakAndConnect(document, curve, breakPoint, orientedLine.GetEndPoint(0), orientedLine.GetEndPoint(1));

            double nearLength = GetCurveLength(operation.NearCurve);
            double farLength = GetCurveLength(operation.FarCurve);
            double nearTrim = Math.Max(0d, sampleOffset - nearLength);
            double farTrim = Math.Max(0d, (totalLength - sampleOffset) - farLength);

            subTransaction.RollBack();

            return new SplitCalibration
            {
                Success = true,
                NearTrim = nearTrim,
                FarTrim = farTrim
            };
        }
        catch (Exception ex)
        {
            subTransaction.RollBack();
            return new SplitCalibration
            {
                Success = false,
                Message = $"Nie udalo sie przygotowac podzialu dla elementu {curve.Id.Value}. {ex.Message}"
            };
        }
    }

    private static SplitOperation BreakAndConnect(Document document, MEPCurve curve, XYZ breakPoint, XYZ nearAnchor, XYZ farAnchor)
    {
        ElementId createdCurveId = BreakCurve(document, curve, breakPoint);
        MEPCurve nearCandidate = (MEPCurve)document.GetElement(curve.Id);
        MEPCurve farCandidate = (MEPCurve)document.GetElement(createdCurveId);

        bool nearCandidateIsNear = IsCurveCloserToAnchor(nearCandidate, nearAnchor, farCandidate);
        MEPCurve nearCurve = nearCandidateIsNear ? nearCandidate : farCandidate;
        MEPCurve farCurve = nearCandidateIsNear ? farCandidate : nearCandidate;

        SegmentEndpoints nearCurveEndpoints = GetSegmentEndpoints(nearCurve);
        SegmentEndpoints farCurveEndpoints = GetSegmentEndpoints(farCurve);

        Connector nearStartConnector = GetClosestConnector(nearCurveEndpoints, nearAnchor);
        Connector nearSplitConnector = GetOppositeConnector(nearCurveEndpoints, nearStartConnector);
        Connector farEndConnector = GetClosestConnector(farCurveEndpoints, farAnchor);
        Connector farSplitConnector = GetOppositeConnector(farCurveEndpoints, farEndConnector);

        document.Create.NewUnionFitting(nearSplitConnector, farSplitConnector);

        nearCurve = (MEPCurve)document.GetElement(nearCurve.Id);
        farCurve = (MEPCurve)document.GetElement(farCurve.Id);
        nearCurveEndpoints = GetSegmentEndpoints(nearCurve);
        farCurveEndpoints = GetSegmentEndpoints(farCurve);

        nearStartConnector = GetClosestConnector(nearCurveEndpoints, nearAnchor);
        farEndConnector = GetClosestConnector(farCurveEndpoints, farAnchor);
        Connector farStartConnector = GetOppositeConnector(farCurveEndpoints, farEndConnector);

        return new SplitOperation
        {
            NearCurve = nearCurve,
            FarCurve = farCurve,
            FarStartPoint = farStartConnector.Origin,
            FarEndPoint = farEndConnector.Origin
        };
    }

    private static ElementId BreakCurve(Document document, MEPCurve curve, XYZ breakPoint)
    {
        return curve switch
        {
            Duct duct => MechanicalUtils.BreakCurve(document, duct.Id, breakPoint),
            Pipe pipe => PlumbingUtils.BreakCurve(document, pipe.Id, breakPoint),
            _ => throw new InvalidOperationException("Obslugiwane sa tylko ducts i pipes.")
        };
    }

    private static bool IsCurveCloserToAnchor(MEPCurve candidate, XYZ anchor, MEPCurve other)
    {
        double candidateDistance = GetEndConnectors(candidate).Min(connector => connector.Origin.DistanceTo(anchor));
        double otherDistance = GetEndConnectors(other).Min(connector => connector.Origin.DistanceTo(anchor));
        return candidateDistance <= otherDistance;
    }

    private static bool IsCurveContinuation(MEPCurve curve, Connector incomingConnector, Connector currentConnector, XYZ axisDirection)
    {
        List<Connector> endConnectors = GetEndConnectors(curve);
        if (endConnectors.Count != 2)
        {
            return false;
        }

        Connector? oppositeConnector = endConnectors.FirstOrDefault(connector => connector.Id != incomingConnector.Id);
        if (oppositeConnector == null)
        {
            return false;
        }

        if (!AreConnectorsCompatible(currentConnector, incomingConnector))
        {
            return false;
        }

        XYZ segmentDirection = (oppositeConnector.Origin - incomingConnector.Origin).Normalize();
        return AreDirectionsParallel(segmentDirection, axisDirection);
    }

    private static bool IsInlinePassThroughFitting(Element element, Connector incomingConnector, Connector currentConnector, XYZ axisDirection, out Connector? exitConnector)
    {
        exitConnector = null;

        long? categoryId = element.Category?.Id?.Value;
        bool isInlineFitting = categoryId == (long)BuiltInCategory.OST_DuctFitting || categoryId == (long)BuiltInCategory.OST_PipeFitting;
        if (!isInlineFitting)
        {
            return false;
        }

        List<Connector> connectors = GetElementConnectors(element)
            .Where(connector => connector.ConnectorType == ConnectorType.End)
            .ToList();

        if (connectors.Count != 2)
        {
            return false;
        }

        exitConnector = connectors.FirstOrDefault(connector => connector.Id != incomingConnector.Id);
        if (exitConnector == null)
        {
            return false;
        }

        if (!AreConnectorsCompatible(incomingConnector, exitConnector) || !AreConnectorsCompatible(currentConnector, incomingConnector))
        {
            exitConnector = null;
            return false;
        }

        XYZ fittingDirection = (exitConnector.Origin - incomingConnector.Origin).Normalize();
        if (!AreDirectionsParallel(fittingDirection, axisDirection))
        {
            exitConnector = null;
            return false;
        }

        return true;
    }

    private static bool AreConnectorsCompatible(Connector first, Connector second)
    {
        if (first.Shape != second.Shape)
        {
            return false;
        }

        return first.Shape switch
        {
            ConnectorProfileType.Round => Math.Abs(first.Radius - second.Radius) <= GeometryTolerance,
            ConnectorProfileType.Rectangular or ConnectorProfileType.Oval =>
                Math.Abs(first.Width - second.Width) <= GeometryTolerance && Math.Abs(first.Height - second.Height) <= GeometryTolerance,
            _ => true
        };
    }

    private static bool AreDirectionsParallel(XYZ first, XYZ second)
    {
        double dot = Math.Abs(first.Normalize().DotProduct(second.Normalize()));
        return dot >= 1d - GeometryTolerance;
    }

    private static SegmentEndpoints GetSegmentEndpoints(MEPCurve curve)
    {
        List<Connector> connectors = GetEndConnectors(curve);
        if (connectors.Count != 2)
        {
            throw new InvalidOperationException($"Element {curve.Id.Value} nie ma dwoch konektorow koncowych.");
        }

        return new SegmentEndpoints(connectors[0], connectors[1]);
    }

    private static List<Connector> GetEndConnectors(MEPCurve curve)
    {
        return curve.ConnectorManager.Connectors
            .Cast<Connector>()
            .Where(connector => connector.ConnectorType == ConnectorType.End)
            .ToList();
    }

    private static List<Connector> GetExternalReferences(Connector connector)
    {
        return connector.AllRefs
            .Cast<Connector>()
            .Where(reference => reference.Owner.Id != connector.Owner.Id)
            .ToList();
    }

    private static IEnumerable<Connector> GetElementConnectors(Element element)
    {
        if (element is MEPCurve curve)
        {
            return curve.ConnectorManager.Connectors.Cast<Connector>();
        }

        if (element is FamilyInstance familyInstance)
        {
            return familyInstance.MEPModel?.ConnectorManager?.Connectors?.Cast<Connector>() ?? Enumerable.Empty<Connector>();
        }

        return Enumerable.Empty<Connector>();
    }

    private static Connector GetClosestConnector(SegmentEndpoints endpoints, XYZ point)
    {
        return endpoints.StartConnector.Origin.DistanceTo(point) <= endpoints.EndConnector.Origin.DistanceTo(point)
            ? endpoints.StartConnector
            : endpoints.EndConnector;
    }

    private static Connector GetClosestEndConnector(MEPCurve curve, XYZ point)
    {
        SegmentEndpoints endpoints = GetSegmentEndpoints(curve);
        return GetClosestConnector(endpoints, point);
    }

    private static Connector GetOppositeConnector(SegmentEndpoints endpoints, Connector connector)
    {
        return connector.Id == endpoints.StartConnector.Id ? endpoints.EndConnector : endpoints.StartConnector;
    }

    private static Connector GetOppositeCurveConnector(MEPCurve curve, Connector connector)
    {
        SegmentEndpoints endpoints = GetSegmentEndpoints(curve);
        return GetOppositeConnector(endpoints, connector);
    }

    private static Line CreateOrientedLine(Line baseLine, XYZ startPoint, XYZ endPoint)
    {
        XYZ first = baseLine.GetEndPoint(0);
        XYZ second = baseLine.GetEndPoint(1);

        double direct = first.DistanceTo(startPoint) + second.DistanceTo(endPoint);
        double reversed = first.DistanceTo(endPoint) + second.DistanceTo(startPoint);
        return direct <= reversed
            ? Line.CreateBound(first, second)
            : Line.CreateBound(second, first);
    }

    private static double GetCurveLength(MEPCurve curve)
    {
        return curve.Location is LocationCurve { Curve: Curve segmentCurve }
            ? segmentCurve.Length
            : 0d;
    }

    private static SplitterResult BuildRebuildOnly(long elementId, int removedConnectorCount, string reason)
    {
        return new SplitterResult
        {
            Success = true,
            HasChanges = true,
            ElementId = elementId,
            Message = $"Element {elementId}: {reason}. Usunieto {removedConnectorCount} wewnetrznych elementow."
        };
    }

    private static SplitterResult BuildFailure(long elementId, string message)
    {
        return new SplitterResult
        {
            Success = false,
            HasChanges = false,
            ElementId = elementId,
            Message = $"Element {elementId}: {message}"
        };
    }

    private static SplitterResult BuildSkipped(long elementId, SplitterOptions options, string reason)
    {
        return new SplitterResult
        {
            Success = true,
            HasChanges = false,
            ElementId = elementId,
            Message = $"Element {elementId}: {reason} dla dlugosci {options.SegmentLengthMillimeters:0.##} mm."
        };
    }

    private readonly record struct SegmentEndpoints(Connector StartConnector, Connector EndConnector);

    private sealed class RunSideScan
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public List<MEPCurve> Segments { get; init; } = [];

        public List<ElementId> InternalElementIds { get; init; } = [];

        public Connector? BoundaryConnector { get; init; }

        public XYZ TerminalPoint { get; init; } = XYZ.Zero;
    }

    private sealed class StraightRunScan
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public List<MEPCurve> OrderedSegments { get; init; } = [];

        public List<ElementId> InternalElementIds { get; init; } = [];

        public Connector? StartBoundaryConnector { get; init; }

        public Connector? EndBoundaryConnector { get; init; }

        public XYZ StartPoint { get; init; } = XYZ.Zero;

        public XYZ EndPoint { get; init; } = XYZ.Zero;

        public bool RequiresRebuild => OrderedSegments.Count > 1 || InternalElementIds.Count > 0;
    }

    private sealed class StraightRunRebuild
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public MEPCurve? Curve { get; init; }

        public XYZ StartPoint { get; init; } = XYZ.Zero;

        public XYZ EndPoint { get; init; } = XYZ.Zero;

        public int RemovedElementCount { get; init; }
    }

    private sealed class SplitCalibration
    {
        public bool Success { get; init; }

        public double NearTrim { get; init; }

        public double FarTrim { get; init; }

        public string Message { get; init; } = string.Empty;
    }

    private sealed class SplitOperation
    {
        public required MEPCurve NearCurve { get; init; }

        public required MEPCurve FarCurve { get; init; }

        public required XYZ FarStartPoint { get; init; }

        public required XYZ FarEndPoint { get; init; }
    }
}



