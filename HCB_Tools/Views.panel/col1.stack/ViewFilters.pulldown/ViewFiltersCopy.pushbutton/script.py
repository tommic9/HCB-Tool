# -*- coding: utf-8 -*-
__title__ = "ViewFilters: Copy to Another View"
__author__ = "Erik Frits"
__version__ = "Version: 1.2-HellCold"
__doc__ = """Version = 1.2
Date    = 13.11.2025
_____________________________________________________________________
Description:

Copy Filters from another View/ViewTemplate with an option
to add them or override (replace current with new ones only).
Added support for 'Active View' as source.
_____________________________________________________________________
How-to:

-> Click on the button
-> Tick 'Active View' OR select Source View/ViewTemplate from list
-> Select Filters
-> Select Destination Views/ViewTemplates
_____________________________________________________________________
Last update:
[13.11.2025] - 1.2 Added 'Active View' and full override copy
[17.01.2023] - 1.1 Bug: ViewPlan.GetOrderedFilters() isn't available 
before RVT 21
[22.09.2022] - 1.0 Release
_____________________________________________________________________
Author: Erik Frits / HellCold edit"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ====================================================================================================
import os, traceback
from Autodesk.Revit.DB import *

# pyRevit
from pyrevit import forms

# Custom Imports
from GUI.forms                  import select_from_dict
from Snippets._context_manager  import ef_Transaction
from GUI.forms                  import my_WPF, ListItem

# .NET IMPORTS
import clr
clr.AddReference("System.Windows.Forms")
clr.AddReference("System")
from System.Collections.Generic import List
from System.Windows.Controls import ComboBoxItem
import wpf

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ VARIABLES
# ====================================================================================================
PATH_SCRIPT = os.path.dirname(__file__)

uidoc = __revit__.ActiveUIDocument
app   = __revit__.Application
doc   = __revit__.ActiveUIDocument.Document
app_year = int(app.VersionNumber)

active_view = doc.ActiveView

# VIEWS
all_views = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).ToElements()

all_views_with_filters      = [v for v in all_views if v.GetOrderedFilters() and not v.IsTemplate]
all_templates_with_filters  = [v for v in all_views if v.GetOrderedFilters() and v.IsTemplate]
all_with_filters            = all_views_with_filters + all_templates_with_filters

if not all_with_filters:
    forms.alert("There are no Views or ViewTemplates with Filters applied to them! "
                "\nPlease add some View Filters and Try Again.", exitscript=True)

def get_dict_views(mode='all'):
    """ Function to get and sort Views in a dict based on a mode setting.
    :param mode: modes( 'all', 'views', 'viewtempaltes'
    :return:     dict of views with [ViewType] as a prefix."""

    all_views_local = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).ToElements()
    dict_views = {}

    for view in all_views_local:
        if view.ViewType == ViewType.FloorPlan:
            dict_views['[FLOOR] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.CeilingPlan:
            dict_views['[CEIL] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.ThreeD:
            dict_views['[3D] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.Section:
            dict_views['[SEC] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.Elevation:
            dict_views['[EL] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.DraftingView:
            dict_views['[DRAFT] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.AreaPlan:
            dict_views['[AREA] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.Rendering:
            dict_views['[CAM] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.Legend:
            dict_views['[LEG] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.EngineeringPlan:
            dict_views['[STR] {}'.format(view.Name)] = view

        elif view.ViewType == ViewType.Walkthrough:
            dict_views['[WALK] {}'.format(view.Name)] = view

        else:
            dict_views['[?] {}'.format(view.Name)] = view

    return dict_views


# Create Dict of Views
dict_all_views   = get_dict_views()
dict_views_f     = {k:v for k,v in dict_all_views.items() if not v.IsTemplate and v.GetOrderedFilters() }
dict_templates_f = {k:v for k,v in dict_all_views.items() if     v.IsTemplate and v.GetOrderedFilters() }

dict_views_and_templates_f = dict_views_f.copy()
dict_views_and_templates_f.update(dict_templates_f)


def select_destination_views(dict_views):
    """Function to select Destination Views/ViewTemplates."""
    dest_views = None

    try:
        dest_views = select_from_dict(dict_all_views,
                                      title      = __title__,
                                      label      = 'Select Destination View/ViewTemplates',
                                      button_name= 'Copy View Filters',
                                      version    = __version__)
    except:
        forms.alert('No Destination Views/ViewTemplates were selected. \nPlease Try Again', exitscript=True)

    if not dest_views:
        forms.alert("No Destination Views were Selected.\n"
                    "Please Try Again.", exitscript=True)

    return dest_views


def create_List(dict_elements):
    """Function to create a List<ListItem> to parse it in GUI ComboBox.
    :param dict_elements:   dict of views ({name:view})
    :return:                List<ListItem>"""
    list_of_views = List[type(ListItem())]()
    for name, view in sorted(dict_elements.items()):
        list_of_views.Add(ListItem(name, view))
    return list_of_views

# Convert Dict into List[ListItem]() for ListBox
List_all_views_and_templates = create_List(dict_views_and_templates_f)
List_all_views               = create_List(dict_views_f)
List_all_templates           = create_List(dict_templates_f)



# ╔═╗╦  ╔═╗╔═╗╔═╗
# ║  ║  ╠═╣╚═╗╚═╗
# ╚═╝╩═╝╩ ╩╚═╝╚═╝

class SelectFilters(my_WPF):
    src_view = None
    views    = List_all_views_and_templates
    filters  = {}
    dest_views = {}

    def __init__(self):
        # Load Resources
        self.add_wpf_resource()
        path_xaml_file = os.path.join(PATH_SCRIPT, 'CopyFilters.xaml')
        wpf.LoadComponent(self, path_xaml_file)

        # Update Text
        self.main_title.Text = __title__

        # Update ListBoxes
        # Update ListBoxes
        self.UI_ListBox_Src_Views.ItemsSource = self.views

        # lista widoków docelowych – wszystkie widoki (tak jak w select_from_dict)
        self.UI_ListBox_Dest_Views.ItemsSource = create_List(dict_all_views)
        self.dest_views = {}

        self.ShowDialog()

    #>>>>>>>>>> INHERIT WPF RESOURCES
    def add_wpf_resource(self):
        """Function to get resources from super()"""
        super(SelectFilters, self).add_wpf_resource()

    # ╔═╗╦ ╦╦  ╔═╗╦  ╦╔═╗╔╗╔╔╦╗╔═╗
    # ║ ╦║ ║║  ║╣ ╚╗╔╝║╣ ║║║ ║ ╚═╗
    # ╚═╝╚═╝╩  ╚═╝ ╚╝ ╚═╝╝╚╝ ╩ ╚═╝
    #==================================================

    def UI_event_checked_views(self, sender, e):
        """EventHandler to filter Views and ViewTemplate in the ListBox when checkboxes clicked."""
        #VIEWS + TEMPLATE
        if self.UI_checkbox_views.IsChecked and self.UI_checkbox_view_templates.IsChecked:
            self.views = List_all_views_and_templates

        #VIEWS
        elif self.UI_checkbox_views.IsChecked:
            self.views = List_all_views

        #TEMPLATES
        elif self.UI_checkbox_view_templates.IsChecked:
            self.views = List_all_templates

        #NONE
        else:
            self.views = List[type(ListItem())]()

        #UPDATE LISTBOX
        self.UI_ListBox_Src_Views.ItemsSource = self.views
        # self.UI_text_filter_updated(0,0)

    def UI_text_filter_updated(self, sender, e):
        """Function to filter items in the UI_ListBox_Src_Views."""
        filtered_list_of_items = List[type(ListItem())]()
        filter_keyword = self.textbox_filter.Text

        #RESTORE ORIGINAL LIST
        if not filter_keyword:
            self.UI_ListBox_Src_Views.ItemsSource = self.views
            return

        # FILTER ITEMS
        for item in self.views:
            if filter_keyword.lower() in item.Name.lower():
                filtered_list_of_items.Add(item)

        # UPDATE LIST OF ITEMS
        self.UI_ListBox_Src_Views.ItemsSource = filtered_list_of_items

    # -----------------------------------------------------------------------------------
    # ACTIVE VIEW CHECKBOX
    # -----------------------------------------------------------------------------------
    def UI_event_active_view_checked(self, sender, e):
        """Use ActiveView as source and show ONLY this view in source list."""
        global active_view

        self.src_view = active_view
        self.filters = {}

        # zbuduj nową listę z JEDNYM elementem - ActiveView
        single_list = List[type(ListItem())]()
        active_item = None

        # Szukamy ListItem odpowiadającego ActiveView w pełnej liście
        for item in List_all_views_and_templates:
            try:
                if item.element.Id == active_view.Id:
                    item.IsChecked = True
                    active_item = item
                    break
            except:
                pass

        if active_item:
            single_list.Add(active_item)

        # ustaw widoki na listę 1-elementową
        self.views = single_list
        self.UI_ListBox_Src_Views.ItemsSource = self.views

        # załaduj filtry z aktywnego widoku
        try:
            if app_year >= 2021:
                filter_ids = self.src_view.GetOrderedFilters()
            else:
                filter_ids = self.src_view.GetFilters()
        except:
            filter_ids = []

        filters = [doc.GetElement(e_id) for e_id in filter_ids]
        dict_filters = {f.Name: f for f in filters}
        List_filters = create_List(dict_filters)
        self.UI_ListBox_Filters.ItemsSource = List_filters

    def UI_event_active_view_unchecked(self, sender, e):
        """Restore normal views list and uncheck any selected view when Active View is unchecked."""
        # wyczyść źródło i filtry
        self.src_view = None
        self.filters = {}

        # przywróć listę zgodnie z checkboxami Views / ViewTemplates
        # (ta metoda ustawi self.views i ItemsSource)
        self.UI_event_checked_views(self, None)

        # upewnij się, że ŻADEN widok nie jest zaznaczony
        cleaned_list = List[type(ListItem())]()
        for item in self.UI_ListBox_Src_Views.Items:
            item.IsChecked = False
            cleaned_list.Add(item)

        self.UI_ListBox_Src_Views.ItemsSource = cleaned_list

        # wyczyść listę filtrów w UI
        self.UI_ListBox_Filters.ItemsSource = []

    # -----------------------------------------------------------------------------------
    # WYBÓR WIDOKU Z LISTY (manualnie)
    # -----------------------------------------------------------------------------------
    def UIe_ViewUnchecked(self, sender, e):
        self.UI_ListBox_Filters.ItemsSource = []

    def UIe_ViewChecked(self, sender, e):
        # jeśli użytkownik wybiera widok ręcznie, wyłącz 'Active View'
        try:
            self.UI_checkbox_active_view.IsChecked = False
        except:
            pass

        # Clear Selected Filters
        self.filters = {}

        filtered_list_of_items = List[type(ListItem())]()

        # Single Selection
        for item in self.UI_ListBox_Src_Views.Items:
            if sender.Content.Text != item.Name:
                item.IsChecked = False
            else:
                item.IsChecked = True
                self.src_view = item.element
            filtered_list_of_items.Add(item)
        self.UI_ListBox_Src_Views.ItemsSource = filtered_list_of_items

        # Update Filter's ListBox
        if self.src_view:
            if app_year >= 2021:
                filter_ids = self.src_view.GetOrderedFilters()
            else:
                filter_ids = self.src_view.GetFilters()
        else:
            filter_ids = []

        filters      = [doc.GetElement(e_id) for e_id in filter_ids]
        dict_filters = {f.Name: f for f in filters}
        List_filters = create_List(dict_filters)
        self.UI_ListBox_Filters.ItemsSource = List_filters

    # -----------------------------------------------------------------------------------
    # FILTRY
    # -----------------------------------------------------------------------------------
    def UIe_FilterChecked(self, sender, e):
        for item in self.UI_ListBox_Filters.Items:
            if sender.Content.Text == item.Name:
                f = item.element
                break
        self.filters[sender.Content.Text] = f

    def UIe_FilterUnchecked(self, sender, e):
        self.filters.pop(sender.Content.Text, None)

    # -----------------------------------------------------------------------------------
    # DESTINATION VIEWS (lista w tym samym oknie)
    # -----------------------------------------------------------------------------------
    def UIe_DestViewChecked(self, sender, e):
        """Dodanie widoku do listy docelowej po zaznaczeniu CheckBoxa."""
        for item in self.UI_ListBox_Dest_Views.Items:
            if sender.Content.Text == item.Name:
                v = item.element
                self.dest_views[item.Name] = v
                break

    def UIe_DestViewUnchecked(self, sender, e):
        """Usunięcie widoku z listy docelowej po odznaczeniu CheckBoxa."""
        self.dest_views.pop(sender.Content.Text, None)


    def select_mode(self, mode):
        """Helper function for following buttons:
        - button_select_all
        - button_select_none"""

        list_of_items = List[type(ListItem())]()
        checked = True if mode=='all' else False
        for item in self.UI_ListBox_Filters.ItemsSource:
            item.IsChecked = checked
            list_of_items.Add(item)

        self.UI_ListBox_Filters.ItemsSource = list_of_items

    def button_select_all(self, sender, e):
        self.select_mode(mode='all')

        for item in self.UI_ListBox_Filters.ItemsSource:
            self.filters[item.Name] = item.element

    def button_select_none(self, sender, e):
        self.select_mode(mode='none')
        self.filters = {}

    # ╦═╗╦ ╦╔╗╔
    # ╠╦╝║ ║║║║
    # ╩╚═╚═╝╝╚╝ RUN
    #==================================================

    def button_run(self, sender, e):
        # NIE zamykamy okna przed walidacją
        if not self.src_view:
            forms.alert("No Source View/ViewTemplate selected.\nTick 'Active View' or pick a view from the list.",
                        exitscript=True)

        if not self.filters:
            forms.alert("No Filters were selected.\nPlease select at least one filter.",
                        exitscript=True)

        # zbuduj listę widoków docelowych na podstawie zaznaczonych w ListBoxie
        dest_views = list(self.dest_views.values())
        if not dest_views:
            forms.alert("No Destination Views were selected.\nPlease select at least one destination view.",
                        exitscript=True)

        # jak wszystko OK – zamknij okno
        self.Close()

        print('*** Source View: {} ***'.format(self.src_view.Name))
        print('*** Selected Filters: ***')
        for f_name in self.filters.keys():
            print('- {}'.format(f_name))

        with ef_Transaction(doc, __title__, debug=True):

            print('*** Destination Views/ViewTemplates:')
            for view in dest_views:
                print('- {}'.format(view.Name))

                # pobierz listę filtrów w widoku docelowym
                try:
                    if app_year >= 2021:
                        dest_filter_ids = list(view.GetOrderedFilters())
                    else:
                        dest_filter_ids = list(view.GetFilters())
                except:
                    dest_filter_ids = []

                for f in self.filters.values():
                    try:
                        # 1. jeśli filtra nie ma w widoku docelowym – dodaj go
                        if f.Id not in dest_filter_ids:
                            view.AddFilter(f.Id)
                            dest_filter_ids.append(f.Id)

                        # 2. skopiuj overrides ze źródłowego widoku (nadpisze istniejące)
                        overrides = self.src_view.GetFilterOverrides(f.Id)
                        view.SetFilterOverrides(f.Id, overrides)

                        # 3. skopiuj widoczność filtra (pokaż/ukryj tak jak w źródle)
                        visible = self.src_view.GetFilterVisibility(f.Id)
                        view.SetFilterVisibility(f.Id, visible)

                    except:
                        print(traceback.format_exc())

        print('\nExecution Completed.')


# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝  MAIN
if __name__ == '__main__':
    SelectFilters()
