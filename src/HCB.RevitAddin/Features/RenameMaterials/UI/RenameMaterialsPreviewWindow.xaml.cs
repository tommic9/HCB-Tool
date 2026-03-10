using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HCB.RevitAddin.Features.RenameMaterials.Models;

namespace HCB.RevitAddin.Features.RenameMaterials.UI;

public partial class RenameMaterialsPreviewWindow : Window
{
    private readonly List<RenameMaterialsPreviewItem> _allItems;

    public RenameMaterialsPreviewWindow(IReadOnlyList<RenameMaterialsPreviewItem> items)
    {
        InitializeComponent();
        _allItems = items.ToList();
        FooterBar.StatusText = $"Pozycji: {_allItems.Count}";
        ApplyFilter();
    }

    private void ProblemsOnlyCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ApplyFilter()
    {
        bool problemsOnly = ProblemsOnlyCheckBox.IsChecked == true;
        List<RenameMaterialsPreviewItem> items = problemsOnly
            ? _allItems.Where(item => item.Status != "OK").ToList()
            : _allItems;
        PreviewListView.ItemsSource = items;
    }
}
