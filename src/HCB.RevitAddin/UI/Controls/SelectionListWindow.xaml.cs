using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HCB.RevitAddin.UI.Controls;

public partial class SelectionListWindow : Window
{
    private const string AllFilterValue = "__ALL__";
    private readonly List<SelectionListItem> _allItems;
    private readonly HashSet<object> _currentSelection;
    private readonly object? _activeSelectionValue;
    private bool _isUpdatingSelection;

    public SelectionListWindow(string title, string sectionTitle, IEnumerable<SelectionListItem> items)
        : this(title, sectionTitle, items, Array.Empty<object>(), "Wybierz", null)
    {
    }

    public SelectionListWindow(
        string title,
        string sectionTitle,
        IEnumerable<SelectionListItem> items,
        IEnumerable<object> initiallySelectedValues,
        string primaryButtonText = "Wybierz",
        string? statusText = null,
        object? activeSelectionValue = null,
        string activeSelectionLabel = "Active View",
        string primaryFilterLabel = "Kategoria",
        string secondaryFilterLabel = "Typ")
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        ItemsGroupBox.Header = sectionTitle;
        PrimaryFilterLabelTextBlock.Text = primaryFilterLabel;
        SecondaryFilterLabelTextBlock.Text = secondaryFilterLabel;
        _allItems = items.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        _currentSelection = new HashSet<object>(initiallySelectedValues);
        _activeSelectionValue = activeSelectionValue;

        FooterBar.PrimaryButtonText = primaryButtonText;
        FooterBar.StatusText = statusText ?? $"Pozycji do wyboru: {_allItems.Count}";

        if (_activeSelectionValue != null && _allItems.Any(item => Equals(item.Value, _activeSelectionValue)))
        {
            ActiveViewCheckBox.Content = activeSelectionLabel;
            ActiveViewCheckBox.Visibility = Visibility.Visible;
        }

        InitializeFilterGroups();
        ApplyFilters();

        Loaded += SelectionListWindow_OnLoaded;
        ItemsListBox.SelectionChanged += ItemsListBox_OnSelectionChanged;
    }

    public IReadOnlyList<object> SelectedValues { get; private set; } = Array.Empty<object>();

    private void SelectionListWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus();
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void SelectAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (SelectionListItem item in ItemsListBox.Items.Cast<SelectionListItem>())
        {
            _currentSelection.Add(item.Value);
        }

        ItemsListBox.SelectAll();
    }

    private void SelectNoneButton_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (SelectionListItem item in ItemsListBox.Items.Cast<SelectionListItem>())
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

    private void FilterGroupComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyFilters();
    }

    private void SecondaryFilterGroupComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyFilters();
    }

    private void ActiveViewCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        if (_activeSelectionValue == null)
        {
            return;
        }

        _currentSelection.Clear();
        _currentSelection.Add(_activeSelectionValue);
        ApplyFilters();
    }

    private void ItemsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection)
        {
            return;
        }

        foreach (SelectionListItem item in e.RemovedItems.Cast<SelectionListItem>())
        {
            _currentSelection.Remove(item.Value);
        }

        foreach (SelectionListItem item in e.AddedItems.Cast<SelectionListItem>())
        {
            _currentSelection.Add(item.Value);
        }
    }

    private void ApplySelectionToVisibleItems(IEnumerable<SelectionListItem> visibleItems)
    {
        _isUpdatingSelection = true;
        try
        {
            ItemsListBox.SelectedItems.Clear();
            foreach (SelectionListItem item in visibleItems)
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

    private void InitializeFilterGroups()
    {
        List<string> groups = _allItems
            .Select(item => item.FilterGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (groups.Count <= 1)
        {
            PrimaryFilterPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            List<SelectionListItem> filterOptions = new()
            {
                new SelectionListItem(AllFilterValue, "Wszystkie")
            };

            filterOptions.AddRange(groups.Select(group => new SelectionListItem(group, group)));
            FilterGroupComboBox.ItemsSource = filterOptions;
            FilterGroupComboBox.SelectedValue = AllFilterValue;
            PrimaryFilterPanel.Visibility = Visibility.Visible;
        }

        List<string> secondaryGroups = _allItems
            .Select(item => item.SecondaryFilterGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (secondaryGroups.Count <= 1)
        {
            SecondaryFilterPanel.Visibility = Visibility.Collapsed;
            return;
        }

        List<SelectionListItem> secondaryFilterOptions = new()
        {
            new SelectionListItem(AllFilterValue, "Wszystkie")
        };

        secondaryFilterOptions.AddRange(secondaryGroups.Select(group => new SelectionListItem(group, group)));
        SecondaryFilterGroupComboBox.ItemsSource = secondaryFilterOptions;
        SecondaryFilterGroupComboBox.SelectedValue = AllFilterValue;
        SecondaryFilterPanel.Visibility = Visibility.Visible;
    }

    private void ApplyFilters()
    {
        string searchText = SearchTextBox.Text?.Trim() ?? string.Empty;
        string? selectedGroup = PrimaryFilterPanel.Visibility == Visibility.Visible
            ? FilterGroupComboBox.SelectedValue as string
            : null;
        string? selectedSecondaryGroup = SecondaryFilterPanel.Visibility == Visibility.Visible
            ? SecondaryFilterGroupComboBox.SelectedValue as string
            : null;

        List<SelectionListItem> filteredItems = _allItems
            .Where(item => string.IsNullOrWhiteSpace(searchText)
                || item.DisplayName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
            .Where(item => string.IsNullOrWhiteSpace(selectedGroup)
                || string.Equals(selectedGroup, AllFilterValue, StringComparison.Ordinal)
                || string.Equals(item.FilterGroup, selectedGroup, StringComparison.CurrentCultureIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(selectedSecondaryGroup)
                || string.Equals(selectedSecondaryGroup, AllFilterValue, StringComparison.Ordinal)
                || string.Equals(item.SecondaryFilterGroup, selectedSecondaryGroup, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        ItemsListBox.ItemsSource = filteredItems;
        ApplySelectionToVisibleItems(filteredItems);
    }
}
