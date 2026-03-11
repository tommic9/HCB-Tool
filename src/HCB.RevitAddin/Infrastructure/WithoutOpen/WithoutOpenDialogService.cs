using System.Collections.Generic;
using Microsoft.Win32;

namespace HCB.RevitAddin.Infrastructure.WithoutOpen;

public sealed class WithoutOpenDialogService
{
    public IReadOnlyList<string> PickRevitFiles()
    {
        OpenFileDialog dialog = new()
        {
            Title = "Wybierz pliki Revit",
            Filter = "Pliki Revit (*.rvt;*.rfa)|*.rvt;*.rfa|Projekty (*.rvt)|*.rvt|Rodziny (*.rfa)|*.rfa|Wszystkie pliki (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true
            ? dialog.FileNames
            : [];
    }

    public string? PickCsvOutputPath(string suggestedFileName)
    {
        SaveFileDialog dialog = new()
        {
            Title = "Zapisz raport CSV",
            Filter = "CSV (*.csv)|*.csv",
            FileName = suggestedFileName,
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}

