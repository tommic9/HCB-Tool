using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HCB.RevitAddin.UI.Controls;

public partial class ColorSelectionWindow : Window
{
    private const string AllFilterValue = "__ALL__";
    private readonly List<ColorSelectionItem> _allItems;
    private readonly HashSet<object> _currentSelection;
    private bool _isUpdatingSelection;

    public ColorSelectionWindow(
        string title,
        string sectionTitle,
        IEnumerable<ColorSelectionItem> items,
        IEnumerable<object> initiallySelectedValues,
        bool overrideDisplayLines,
        string primaryButtonText = "Wybierz",
        string? statusText = null)
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        ItemsGroupBox.Header = sectionTitle;
        _allItems = items.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        _currentSelection = new HashSet<object>(initiallySelectedValues);

        BlackLinesCheckBox.IsChecked = overrideDisplayLines;
        FooterBar.PrimaryButtonText = primaryButtonText;
        FooterBar.StatusText = statusText ?? $"Pozycji do wyboru: {_allItems.Count}";

        InitializeFilterGroups();
        ApplyFilters();

        Loaded += ColorSelectionWindow_OnLoaded;
        ItemsListBox.SelectionChanged += ItemsListBox_OnSelectionChanged;
    }

    public IReadOnlyList<object> SelectedValues { get; private set; } = Array.Empty<object>();

    public bool OverrideDisplayLines => BlackLinesCheckBox.IsChecked == true;

    private void ColorSelectionWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus();
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void SelectAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (ColorSelectionItem item in ItemsListBox.Items.Cast<ColorSelectionItem>())
        {
            _currentSelection.Add(item.Value);
        }

        ItemsListBox.SelectAll();
    }

    private void SelectNoneButton_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (ColorSelectionItem item in ItemsListBox.Items.Cast<ColorSelectionItem>())
        {
            _currentSelection.Remove(item.Value);
        }

        ItemsListBox.UnselectAll();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        List<object> selected = _currentSelection.ToList();
        if (selected.Count == 0)
        {
            FooterBar.StatusText = "Wybierz co najmniej jedna pozycje.";
            return;
        }

        SelectedValues = selected;
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ItemsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection)
        {
            return;
        }

        foreach (ColorSelectionItem item in e.RemovedItems.Cast<ColorSelectionItem>())
        {
            _currentSelection.Remove(item.Value);
        }

        foreach (ColorSelectionItem item in e.AddedItems.Cast<ColorSelectionItem>())
        {
            _currentSelection.Add(item.Value);
        }
    }

    private void FilterGroupComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyFilters();
    }

    private void InitializeFilterGroups()
    {
        List<string> groups = _allItems
            .Select(item => item.FilterGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group, StringComparer.CurrentCultureIgnoreCase)
            .ToList()!;

        if (groups.Count <= 1)
        {
            return;
        }

        List<SelectionListItem> filterOptions = new()
        {
            new SelectionListItem(AllFilterValue, "Wszystkie grupy")
        };

        filterOptions.AddRange(groups.Select(group => new SelectionListItem(group!, group!)));
        FilterGroupComboBox.ItemsSource = filterOptions;
        FilterGroupComboBox.SelectedValue = AllFilterValue;
        FilterGroupComboBox.Visibility = Visibility.Visible;
    }

    private void ApplyFilters()
    {
        string searchText = SearchTextBox.Text?.Trim() ?? string.Empty;
        string? selectedGroup = FilterGroupComboBox.Visibility == Visibility.Visible
            ? FilterGroupComboBox.SelectedValue as string
            : null;

        List<ColorSelectionItem> filteredItems = _allItems
            .Where(item => string.IsNullOrWhiteSpace(searchText)
                || item.DisplayName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
            .Where(item => string.IsNullOrWhiteSpace(selectedGroup)
                || string.Equals(selectedGroup, AllFilterValue, StringComparison.Ordinal)
                || string.Equals(item.FilterGroup, selectedGroup, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        ItemsListBox.ItemsSource = filteredItems;
        ApplySelectionToVisibleItems(filteredItems);
    }

    private void ApplySelectionToVisibleItems(IEnumerable<ColorSelectionItem> visibleItems)
    {
        _isUpdatingSelection = true;
        try
        {
            ItemsListBox.SelectedItems.Clear();
            foreach (ColorSelectionItem item in visibleItems)
            {
                if (_currentSelection.Contains(item.Value))
                {
                    ItemsListBox.SelectedItems.Add(item);
                }
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
}
