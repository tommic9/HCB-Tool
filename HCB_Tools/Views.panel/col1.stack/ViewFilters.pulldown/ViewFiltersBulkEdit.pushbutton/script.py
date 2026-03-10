# -*- coding: utf-8 -*-
"""Bulk edit view filters across multiple views."""
__title__ = "ViewFilters: Bulk Edit"
__author__ = "HellCold"
__version__ = "Version: 1.0"

import os
import traceback

from Autodesk.Revit.DB import BuiltInCategory, FilteredElementCollector, ViewType

from pyrevit import forms

from GUI.forms import my_WPF, ListItem
from Snippets._context_manager import ef_Transaction

import clr
clr.AddReference("System.Windows.Forms")
clr.AddReference("System")
from System.Collections.Generic import List
from System.Windows.Controls import ComboBoxItem
import wpf

PATH_SCRIPT = os.path.dirname(__file__)

uidoc = __revit__.ActiveUIDocument
app = __revit__.Application
doc = uidoc.Document
app_year = int(app.VersionNumber)


def _get_view_filters_ids(view):
    """Return a list of filter ids applied to given view."""
    try:
        if app_year >= 2021:
            return list(view.GetOrderedFilters())
        return list(view.GetFilters())
    except Exception:
        return []


def _collect_views():
    """Collect all project views."""
    return FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).ToElements()


all_views = _collect_views()
all_views_with_filters = [v for v in all_views if _get_view_filters_ids(v)]
if not all_views_with_filters:
    forms.alert(u"Brak widoków lub szablonów widoków z filtrami.\nDodaj filtry i spróbuj ponownie.", exitscript=True)


def get_dict_views():
    """Return dictionary mapping decorated names to view elements."""
    dict_views = {}
    for view in all_views:
        prefix = '[?]'
        if view.ViewType == ViewType.FloorPlan:
            prefix = '[FLOOR]'
        elif view.ViewType == ViewType.CeilingPlan:
            prefix = '[CEIL]'
        elif view.ViewType == ViewType.ThreeD:
            prefix = '[3D]'
        elif view.ViewType == ViewType.Section:
            prefix = '[SEC]'
        elif view.ViewType == ViewType.Elevation:
            prefix = '[EL]'
        elif view.ViewType == ViewType.DraftingView:
            prefix = '[DRAFT]'
        elif view.ViewType == ViewType.AreaPlan:
            prefix = '[AREA]'
        elif view.ViewType == ViewType.Rendering:
            prefix = '[CAM]'
        elif view.ViewType == ViewType.Legend:
            prefix = '[LEG]'
        elif view.ViewType == ViewType.EngineeringPlan:
            prefix = '[STR]'
        elif view.ViewType == ViewType.Walkthrough:
            prefix = '[WALK]'
        dict_views['{} {}'.format(prefix, view.Name)] = view
    return dict_views


def create_list_items(dict_elements):
    """Convert dictionary into python list of ListItem."""
    return [ListItem(name, element, False) for name, element in sorted(dict_elements.items())]


def to_wpf_list(items):
    dotnet_list = List[type(ListItem())]()
    for item in items:
        dotnet_list.Add(item)
    return dotnet_list


dict_all_views = get_dict_views()
dict_views_only = {k: v for k, v in dict_all_views.items() if not v.IsTemplate and _get_view_filters_ids(v)}
dict_templates_only = {k: v for k, v in dict_all_views.items() if v.IsTemplate and _get_view_filters_ids(v)}


class BulkEditFilters(my_WPF):
    views_list = create_list_items({k: v for k, v in dict_all_views.items() if _get_view_filters_ids(v)})
    views_only = create_list_items(dict_views_only)
    templates_only = create_list_items(dict_templates_only)

    def __init__(self):
        super(BulkEditFilters, self).add_wpf_resource()
        xaml_path = os.path.join(PATH_SCRIPT, 'BulkEditFilters.xaml')
        wpf.LoadComponent(self, xaml_path)

        self.main_title.Text = __title__
        self.selected_views = {}
        self.selected_filters = {}
        self.view_items = list(self.views_list)
        self.filter_items = []

        self.enable_action = 'no_change'
        self.visibility_action = 'no_change'

        self._bind_view_items()
        self.UI_ListBox_Filters.ItemsSource = to_wpf_list([])

        self.UI_Enable_Action.SelectedIndex = 0
        self.UI_Visibility_Action.SelectedIndex = 0

        self.ShowDialog()

    # -------------------- helpers --------------------
    def _reset_filters(self):
        self.selected_filters = {}
        self.filter_items = []
        self.UI_ListBox_Filters.ItemsSource = to_wpf_list([])

    def _bind_view_items(self, items=None):
        if items is None:
            items = self.view_items
        self.UI_ListBox_Views.ItemsSource = to_wpf_list(items)

    def _apply_current_view_filter(self):
        keyword = (self.textbox_filter.Text or '').lower()
        if not keyword:
            self._bind_view_items()
            return
        filtered = [item for item in self.view_items if keyword in item.Name.lower()]
        self._bind_view_items(filtered)

    def _update_view_list(self, new_list):
        self.view_items = list(new_list)
        for item in self.view_items:
            item.IsChecked = False
        self.selected_views = {}
        self._bind_view_items()
        self._reset_filters()
        self._apply_current_view_filter()

    def _refresh_filter_items(self):
        if not self.selected_views:
            self._reset_filters()
            return

        preserved = dict(self.selected_filters)
        total_views = len(self.selected_views)
        filter_hits = {}

        for view in self.selected_views.values():
            for f_id in _get_view_filters_ids(view):
                element = doc.GetElement(f_id)
                if element is None:
                    continue
                key = element.Id.IntegerValue
                entry = filter_hits.get(key)
                if entry:
                    entry[1] += 1
                else:
                    filter_hits[key] = [element, 1]

        common_items = []
        new_selection = {}
        for key in sorted(filter_hits.keys(), key=lambda x: filter_hits[x][0].Name.lower()):
            element, count = filter_hits[key]
            if count != total_views:
                continue
            is_checked = key in preserved
            common_items.append(ListItem(element.Name, element, is_checked))
            if is_checked:
                new_selection[key] = element

        self.filter_items = common_items
        self.selected_filters = new_selection
        self.UI_ListBox_Filters.ItemsSource = to_wpf_list(self.filter_items)

    def _set_view_checked_state(self, item_name, checked):
        for item in self.view_items:
            if item.Name == item_name:
                item.IsChecked = checked
        self.selected_views = {
            item.element.Id.IntegerValue: item.element
            for item in self.view_items
            if getattr(item, 'IsChecked', False)
        }
        self._apply_current_view_filter()
        self._refresh_filter_items()

    # -------------------- UI events --------------------
    def UI_event_checked_views(self, sender, e):
        if self.UI_checkbox_views.IsChecked and self.UI_checkbox_view_templates.IsChecked:
            self._update_view_list(self.views_list)
        elif self.UI_checkbox_views.IsChecked:
            self._update_view_list(self.views_only)
        elif self.UI_checkbox_view_templates.IsChecked:
            self._update_view_list(self.templates_only)
        else:
            self._update_view_list([])

    def UI_text_filter_updated(self, sender, e):
        self._apply_current_view_filter()

    def UIe_ViewChecked(self, sender, e):
        self._set_view_checked_state(sender.Content.Text, True)

    def UIe_ViewUnchecked(self, sender, e):
        self._set_view_checked_state(sender.Content.Text, False)

    def UIe_FilterChecked(self, sender, e):
        for item in self.filter_items:
            if item.Name == sender.Content.Text:
                item.IsChecked = True
                self.selected_filters[item.element.Id.IntegerValue] = item.element
                break
        self._refresh_filter_selection_visual()

    def UIe_FilterUnchecked(self, sender, e):
        for item in self.filter_items:
            if item.Name == sender.Content.Text:
                item.IsChecked = False
                self.selected_filters.pop(item.element.Id.IntegerValue, None)
                break
        self._refresh_filter_selection_visual()

    def _refresh_filter_selection_visual(self):
        for item in self.filter_items:
            item.IsChecked = item.element.Id.IntegerValue in self.selected_filters
        self.UI_ListBox_Filters.ItemsSource = to_wpf_list(self.filter_items)

    def button_select_all(self, sender, e):
        for item in self.filter_items:
            item.IsChecked = True
            self.selected_filters[item.element.Id.IntegerValue] = item.element
        self.UI_ListBox_Filters.ItemsSource = to_wpf_list(self.filter_items)

    def button_select_none(self, sender, e):
        self.selected_filters = {}
        self._refresh_filter_selection_visual()

    def UI_Enable_Changed(self, sender, e):
        selection = sender.SelectedItem
        if isinstance(selection, ComboBoxItem):
            self.enable_action = selection.Tag

    def UI_Visibility_Changed(self, sender, e):
        selection = sender.SelectedItem
        if isinstance(selection, ComboBoxItem):
            self.visibility_action = selection.Tag

    def button_run(self, sender, e):
        if not self.selected_views:
            forms.alert(u"Wybierz co najmniej jeden widok.")
            return
        if not self.selected_filters:
            forms.alert(u"Wybierz co najmniej jeden filtr.")
            return

        if self.enable_action == 'no_change' and self.visibility_action == 'no_change':
            forms.alert(u"Nie wybrano żadnej akcji do wykonania.")
            return

        self.Close()

        missing_filters = {}

        with ef_Transaction(doc, __title__, debug=True):
            for view_id, view in self.selected_views.items():
                existing = {fid.IntegerValue for fid in _get_view_filters_ids(view)}
                for filter_id, filt in self.selected_filters.items():
                    if filter_id not in existing:
                        bucket_view, names = missing_filters.get(view_id, (view, []))
                        names.append(filt.Name)
                        missing_filters[view_id] = (bucket_view, names)
                        continue
                    try:
                        if self.enable_action != 'no_change' and hasattr(view, 'SetIsFilterEnabled'):
                            view.SetIsFilterEnabled(filt.Id, self.enable_action == 'enable')
                        if self.visibility_action != 'no_change':
                            view.SetFilterVisibility(filt.Id, self.visibility_action == 'visible')
                    except Exception:
                        print(traceback.format_exc())

        if missing_filters:
            message = [u"Filtry nie znalezione we wszystkich widokach:"]
            ordered = sorted(missing_filters.values(), key=lambda item: item[0].Name.lower())
            for view, filters in ordered:
                message.append(u"{}: {}".format(view.Name, ', '.join(sorted(filters))))
            forms.alert('\n'.join(message))
        else:
            forms.alert(u"Zakończono edycję filtrów.")


if __name__ == '__main__':
    BulkEditFilters()
