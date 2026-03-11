using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ViewFiltersLegend.Models;

namespace HCB.RevitAddin.Features.ViewFiltersLegend;

public sealed class ViewFiltersLegendService
{
    public IReadOnlyList<View> GetViewsWithFilters(Document document)
    {
        return new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_Views)
            .Cast<View>()
            .Where(view => view.GetFilters().Count > 0)
            .ToList();
    }

    public IReadOnlyList<TextNoteType> GetTextNoteTypes(Document document)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(TextNoteType))
            .Cast<TextNoteType>()
            .ToList();
    }

    public bool HasLegendBaseView(Document document)
    {
        return GetLegendBaseView(document) != null;
    }

    public ViewFiltersLegendResult Generate(Document document, IEnumerable<View> sourceViews, ViewFiltersLegendOptions options)
    {
        ViewFiltersLegendResult result = new();
        View? baseLegend = GetLegendBaseView(document);
        if (baseLegend == null)
        {
            result.Messages.Add("W projekcie musi istniec co najmniej jeden widok legendy.");
            return result;
        }

        FilledRegionType? filledRegionType = new FilteredElementCollector(document)
            .OfClass(typeof(FilledRegionType))
            .Cast<FilledRegionType>()
            .FirstOrDefault();

        if (filledRegionType == null)
        {
            result.Messages.Add("Nie znaleziono FilledRegionType.");
            return result;
        }

        using Transaction transaction = new(document, "View Filters Legend");
        transaction.Start();

        foreach (View sourceView in sourceViews)
        {
            List<FilterElement> filters = GetFilters(document, sourceView);
            View legendView = CreateLegendView(document, baseLegend, $"Legend_{sourceView.Name}");
            CreateLegendContent(document, legendView, filters, new ElementId(options.TextTypeId), filledRegionType.Id, options, sourceView);
            result.LegendsCreated++;
            result.LastLegendViewId = legendView.Id.Value;
            result.Items.Add(new ViewFiltersLegendItem
            {
                SourceViewName = sourceView.Name,
                LegendViewName = legendView.Name,
                LegendViewId = legendView.Id.Value,
                FiltersCount = filters.Count
            });
        }

        transaction.Commit();
        result.Messages.Add("Legenda utworzona wedlug zaznaczonych typow probek.");
        return result;
    }

    private static View? GetLegendBaseView(Document document)
    {
        return new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_Views)
            .Cast<View>()
            .FirstOrDefault(view => view.ViewType == ViewType.Legend && !view.IsTemplate);
    }

    private static View CreateLegendView(Document document, View baseLegend, string preferredName)
    {
        ElementId duplicateId = baseLegend.Duplicate(ViewDuplicateOption.Duplicate);
        View legendView = (View)document.GetElement(duplicateId);
        legendView.Scale = 100;

        string candidate = preferredName;
        for (int attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                legendView.Name = candidate;
                break;
            }
            catch
            {
                candidate += "*";
            }
        }

        return legendView;
    }

    private static void CreateLegendContent(
        Document document,
        View legendView,
        IReadOnlyList<FilterElement> filters,
        ElementId textTypeId,
        ElementId filledRegionTypeId,
        ViewFiltersLegendOptions options,
        View sourceView)
    {
        double y = 0.0;
        double regionWidth = ToInternalMillimeters(options.SampleWidthMillimeters);
        double regionHeight = ToInternalMillimeters(options.SampleHeightMillimeters);
        double regionSpacing = ToInternalMillimeters(options.SpacingMillimeters);
        double lineWidth = ToInternalMillimeters(options.LineLengthMillimeters);
        double textOffset = Math.Max(regionWidth, lineWidth) + regionSpacing;
        double headerY = y + regionHeight + regionSpacing;
        double titleY = headerY + regionHeight + regionSpacing;

        List<LegendColumn> columns = BuildColumns(options);
        if (columns.Count > 0)
        {
            double titleX = ((columns.Count - 1) * textOffset) / 2.0;
            CreateTextNote(document, legendView, new XYZ(titleX, titleY, 0), "LEGENDA", textTypeId);
        }

        foreach (LegendColumn column in columns)
        {
            double columnX = column.Index * textOffset;
            if (!string.IsNullOrWhiteSpace(column.Header))
            {
                CreateTextNote(document, legendView, new XYZ(columnX, headerY, 0), column.Header, textTypeId);
            }
        }

        foreach (FilterElement filter in filters)
        {
            OverrideGraphicSettings overrides = sourceView.GetFilterOverrides(filter.Id);
            foreach (LegendColumn column in columns)
            {
                double columnX = column.Index * textOffset;
                switch (column.Kind)
                {
                    case LegendColumnKind.ProjectionLine:
                    {
                        DetailCurve projectionLine = CreateHorizontalLine(document, legendView, columnX, y - regionHeight / 2, lineWidth);
                        ApplyProjectionLineOverrides(legendView, projectionLine.Id, overrides);
                        break;
                    }
                    case LegendColumnKind.CutLine:
                    {
                        DetailCurve cutLine = CreateHorizontalLine(document, legendView, columnX, y - regionHeight / 2, lineWidth);
                        ApplyCutLineOverrides(legendView, cutLine.Id, overrides);
                        break;
                    }
                    case LegendColumnKind.SurfaceFill:
                    {
                        FilledRegion surfaceRegion = CreateRegion(document, legendView, filledRegionTypeId, columnX, y, regionWidth, regionHeight);
                        ApplySurfaceOverridesToRegion(legendView, surfaceRegion.Id, overrides);
                        break;
                    }
                    case LegendColumnKind.CutFill:
                    {
                        FilledRegion cutRegion = CreateRegion(document, legendView, filledRegionTypeId, columnX, y, regionWidth, regionHeight);
                        ApplyCutOverridesToRegion(legendView, cutRegion.Id, overrides);
                        break;
                    }
                    case LegendColumnKind.FilterName:
                        CreateTextNote(document, legendView, new XYZ(columnX, y, 0), filter.Name, textTypeId);
                        break;
                }
            }

            y -= regionHeight + regionSpacing;
        }
    }

    private static List<FilterElement> GetFilters(Document document, View sourceView)
    {
        return sourceView
            .GetFilters()
            .Select(id => document.GetElement(id))
            .OfType<FilterElement>()
            .OrderBy(filter => filter.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static List<LegendColumn> BuildColumns(ViewFiltersLegendOptions options)
    {
        List<LegendColumn> columns = [];
        int index = 0;

        void AddColumn(LegendColumnKind kind, string header)
        {
            columns.Add(new LegendColumn(kind, header, index));
            index++;
        }

        if (options.IncludeProjectionLine)
        {
            AddColumn(LegendColumnKind.ProjectionLine, "Projection line");
        }

        if (options.IncludeCutLine)
        {
            AddColumn(LegendColumnKind.CutLine, "Cut line");
        }

        if (options.IncludeSurfaceFill)
        {
            AddColumn(LegendColumnKind.SurfaceFill, "Projection pattern");
        }

        if (options.IncludeCutFill)
        {
            AddColumn(LegendColumnKind.CutFill, "Cut pattern");
        }

        if (options.IncludeFilterName)
        {
            AddColumn(LegendColumnKind.FilterName, "Nazwa filtra");
        }

        return columns;
    }

    private static double ToInternalMillimeters(double value)
    {
        return UnitUtils.ConvertToInternalUnits(value, UnitTypeId.Millimeters);
    }

    private static TextNote CreateTextNote(Document document, View view, XYZ position, string text, ElementId textTypeId)
    {
        TextNoteOptions options = new(textTypeId);
        return TextNote.Create(document, view.Id, position, text, options);
    }

    private static DetailCurve CreateHorizontalLine(Document document, View view, double x, double y, double width)
    {
        Line line = Line.CreateBound(new XYZ(x, y, 0), new XYZ(x + width, y, 0));
        return document.Create.NewDetailCurve(view, line) as DetailCurve
            ?? throw new InvalidOperationException("Nie udalo sie utworzyc DetailCurve.");
    }

    private static FilledRegion CreateRegion(Document document, View view, ElementId filledRegionTypeId, double x, double y, double width, double height)
    {
        XYZ p1 = new(x, y, 0);
        XYZ p2 = new(x + width, y, 0);
        XYZ p3 = new(x + width, y - height, 0);
        XYZ p4 = new(x, y - height, 0);

        CurveLoop loop = new();
        loop.Append(Line.CreateBound(p1, p2));
        loop.Append(Line.CreateBound(p2, p3));
        loop.Append(Line.CreateBound(p3, p4));
        loop.Append(Line.CreateBound(p4, p1));

        return FilledRegion.Create(document, filledRegionTypeId, view.Id, [loop]);
    }

    private static void ApplySurfaceOverridesToRegion(View view, ElementId elementId, OverrideGraphicSettings source)
    {
        OverrideGraphicSettings settings = new();
        if (source.SurfaceForegroundPatternId != ElementId.InvalidElementId)
        {
            settings.SetSurfaceForegroundPatternId(source.SurfaceForegroundPatternId);
        }

        if (source.SurfaceBackgroundPatternId != ElementId.InvalidElementId)
        {
            settings.SetSurfaceBackgroundPatternId(source.SurfaceBackgroundPatternId);
        }

        settings.SetSurfaceForegroundPatternColor(source.SurfaceForegroundPatternColor);
        settings.SetSurfaceBackgroundPatternColor(source.SurfaceBackgroundPatternColor);

        if (source.ProjectionLinePatternId != ElementId.InvalidElementId)
        {
            settings.SetProjectionLinePatternId(source.ProjectionLinePatternId);
        }

        settings.SetProjectionLineColor(source.ProjectionLineColor);
        if (source.ProjectionLineWeight > 0)
        {
            settings.SetProjectionLineWeight(source.ProjectionLineWeight);
        }

        view.SetElementOverrides(elementId, settings);
    }

    private static void ApplyCutOverridesToRegion(View view, ElementId elementId, OverrideGraphicSettings source)
    {
        OverrideGraphicSettings settings = new();
        if (source.CutForegroundPatternId != ElementId.InvalidElementId)
        {
            settings.SetCutForegroundPatternId(source.CutForegroundPatternId);
        }

        if (source.CutBackgroundPatternId != ElementId.InvalidElementId)
        {
            settings.SetCutBackgroundPatternId(source.CutBackgroundPatternId);
        }

        settings.SetCutForegroundPatternColor(source.CutForegroundPatternColor);
        settings.SetCutBackgroundPatternColor(source.CutBackgroundPatternColor);

        if (source.CutLinePatternId != ElementId.InvalidElementId)
        {
            settings.SetProjectionLinePatternId(source.CutLinePatternId);
        }

        settings.SetProjectionLineColor(source.CutLineColor);
        if (source.CutLineWeight > 0)
        {
            settings.SetProjectionLineWeight(source.CutLineWeight);
        }

        view.SetElementOverrides(elementId, settings);
    }

    private static void ApplyProjectionLineOverrides(View view, ElementId elementId, OverrideGraphicSettings source)
    {
        OverrideGraphicSettings settings = new();
        if (source.ProjectionLinePatternId != ElementId.InvalidElementId)
        {
            settings.SetProjectionLinePatternId(source.ProjectionLinePatternId);
        }

        settings.SetProjectionLineColor(source.ProjectionLineColor);
        if (source.ProjectionLineWeight > 0)
        {
            settings.SetProjectionLineWeight(source.ProjectionLineWeight);
        }

        view.SetElementOverrides(elementId, settings);
    }

    private static void ApplyCutLineOverrides(View view, ElementId elementId, OverrideGraphicSettings source)
    {
        OverrideGraphicSettings settings = new();
        if (source.CutLinePatternId != ElementId.InvalidElementId)
        {
            settings.SetProjectionLinePatternId(source.CutLinePatternId);
        }

        settings.SetProjectionLineColor(source.CutLineColor);
        if (source.CutLineWeight > 0)
        {
            settings.SetProjectionLineWeight(source.CutLineWeight);
        }

        view.SetElementOverrides(elementId, settings);
    }

    private enum LegendColumnKind
    {
        ProjectionLine,
        CutLine,
        SurfaceFill,
        CutFill,
        FilterName
    }

    private sealed class LegendColumn(LegendColumnKind kind, string header, int index)
    {
        public LegendColumnKind Kind { get; } = kind;

        public string Header { get; } = header;

        public int Index { get; } = index;
    }
}
