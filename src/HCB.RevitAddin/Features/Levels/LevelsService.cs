using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.Levels.Models;

namespace HCB.RevitAddin.Features.Levels;

public sealed class LevelsService
{
    private static readonly Regex ElevationSuffixRegex = new(@"\s(?:\([+-]?\d+(?:[.,]\d{2})\)|\[[+-]?\d+(?:[.,]\d{2})\])$", RegexOptions.Compiled);

    public LevelsRenameResult RenameLevels(Document document, LevelsRenameOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        List<Level> levels = new FilteredElementCollector(document)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .OrderBy(level => level.Elevation)
            .ToList();

        LevelsRenameResult result = new()
        {
            TotalLevelsCount = levels.Count
        };

        if (levels.Count == 0)
        {
            result.Messages.Add("Nie znaleziono poziomów do aktualizacji.");
            return result;
        }

        using Transaction transaction = new(document, "Add Levels Elevation");
        transaction.Start();

        foreach (Level level in levels)
        {
            string newName = BuildLevelName(level, options);
            if (string.Equals(level.Name, newName, StringComparison.Ordinal))
            {
                result.UnchangedLevelsCount++;
                continue;
            }

            try
            {
                string previousName = level.Name;
                level.Name = newName;
                result.RenamedLevelsCount++;
                result.Messages.Add($"{previousName} -> {newName}");
            }
            catch (Exception ex)
            {
                result.UnchangedLevelsCount++;
                result.Messages.Add($"Nie udało się zmienić nazwy poziomu '{level.Name}': {ex.Message}");
            }
        }

        transaction.Commit();
        return result;
    }

    private static string BuildLevelName(Level level, LevelsRenameOptions options)
    {
        string currentName = level.Name ?? string.Empty;
        string baseName = ElevationSuffixRegex.Replace(currentName, string.Empty).TrimEnd();
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "Level";
        }

        double elevationInMeters = UnitUtils.ConvertFromInternalUnits(level.Elevation, UnitTypeId.Meters);
        double roundedElevation = Math.Round(elevationInMeters, options.DecimalPlaces, MidpointRounding.AwayFromZero);

        double zeroThreshold = 0.5d / Math.Pow(10d, options.DecimalPlaces);
        if (Math.Abs(roundedElevation) < zeroThreshold)
        {
            roundedElevation = 0;
        }

        string numericFormat = "0" + (options.DecimalPlaces > 0 ? "." + new string('0', options.DecimalPlaces) : string.Empty);
        string elevationText = roundedElevation.ToString(numericFormat, System.Globalization.CultureInfo.InvariantCulture);

        if (options.ShowPlusForPositiveValues && roundedElevation > 0)
        {
            elevationText = "+" + elevationText;
        }

        return $"{baseName} ({elevationText})";
    }
}
