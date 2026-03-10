# -*- coding: utf-8 -*-

__title__ = "Copy Filters"
__doc__ = """
Skrypt kopiuje filtry z wybranego widoku do innego

Jak korzystać:
- Wybierz widok źródłowy
- Wybierz filtry do skopiowania
- Wybierz widok docelowy

Wskazówka
Skrypt kopiuje filtry wraz z nadpisaniami

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

__min_revit_ver = 2019
__max_revit_ver = 2026
# __context__ = ('Walls','Floors', 'Roofs') # Make your button available only whn certain categories are selected


#  ___ __  __ ____   ___  ____ _____ ____
# |_ _|  \/  |  _ \ / _ \|  _ \_   _/ ___|
#  | || |\/| | |_) | | | | |_) || | \___ \
#  | || |  | |  __/| |_| |  _ < | |  ___) |
# |___|_|  |_|_|    \___/|_| \_\|_| |____/  IMPORTS

# Custom imports
# .NET Imports
import clr
# Regular + Autodesk
from Autodesk.Revit.DB import *  # Import everything from DB
from Autodesk.Revit.DB import FilteredElementCollector

# pyRevit
from pyrevit import forms

# Custom imports
# .NET Imports
clr.AddReference("System")

#  __     ___    ____  ___    _    ____  _     _____ ____
# \ \   / / \  |  _ \|_ _|  / \  | __ )| |   | ____/ ___|
#  \ \ / / _ \ | |_) || |  / _ \ |  _ \| |   |  _| \___ \
#   \ V / ___ \|  _ < | | / ___ \| |_) | |___| |___ ___) |
#    \_/_/   \_\_| \_\___/_/   \_\____/|_____|_____|____/  VARIABLES

doc = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
app = __revit__.Application

# from pyrevit.revit import uidoc, doc, app # Alternative

#  _____ _   _ _   _  ____ _____ ___ ___  _   _ ____
# |  ___| | | | \ | |/ ___|_   _|_ _/ _ \| \ | / ___|
# | |_  | | | |  \| | |     | |  | | | | |  \| \___ \
# |  _| | |_| | |\  | |___  | |  | | |_| | |\  |___) |
# |_|    \___/|_| \_|\____| |_| |___\___/|_| \_|____/
#                                                      FUNCTIONS


#   ____ _        _    ____ ____  _____ ____
#  / ___| |      / \  / ___/ ___|| ____/ ___|
# | |   | |     / _ \ \___ \___ \|  _| \___ \
# | |___| |___ / ___ \ ___) |__) | |___ ___) |
#  \____|_____/_/   \_\____/____/|_____|____/
#                                              CLASSES


#  __  __    _    ___ _   _
# |  \/  |  / \  |_ _| \ | |
# | |\/| | / _ \  | ||  \| |
# | |  | |/ ___ \ | || |\  |
# |_|  |_/_/   \_\___|_| \_|
#                            MAIN
# Step 1: Get views/views template with filter
all_views = FilteredElementCollector(doc) \
    .OfCategory(BuiltInCategory.OST_Views) \
    .WhereElementIsNotElementType() \
    .ToElements()

# Views with Filters
views_with_filters = [v for v in all_views if v.GetFilters()]

# ✅Ensure there are views with Filters in the project:
if not views_with_filters:
    forms.alert('There are no Views/Views Template with filters applied to them! Please try again.', exitscript=True)

# Create dict of views
dict_views_with_filters = {v.Name: v for v in views_with_filters}

# Step 2 : Select source view
selected_src_view = forms.SelectFromList.show(sorted(dict_views_with_filters),
                                              title='Source view',
                                              multiselect=False,
                                              button_name='Select Source View')

# ✅Ensure src_view are selected:
if not selected_src_view:
    forms.alert('No source view was selected. Please Try Again.', exitscript=True)

src_view = dict_views_with_filters[selected_src_view]

# Step 3: Select filters to copy

filters_ids = src_view.GetFilters()
filters = [doc.GetElement(f_id) for f_id in filters_ids]
dict_filters = {f.Name: f for f in filters}

selected_filters = forms.SelectFromList.show(sorted(dict_filters),
                                             title='Select Filters to copy',
                                             multiselect=True,
                                             button_name='Select Filters to copy')

# ✅Ensure src_view are selected:
if not selected_filters:
    forms.alert('No filters was selected. Please Try Again.', exitscript=True)

filters_to_copy = [dict_filters[f_name] for f_name in selected_filters]

print (filters_to_copy)
print (selected_filters)

# Step 4: Select destination views

dict_all_view = {v.Name: v for v in all_views}

selected_dest_views = forms.SelectFromList.show(sorted(dict_all_view),
                                               title='Destination views',
                                               multiselect=True,
                                               button_name='Select Destination View')

# ✅Ensure src_view are selected:
if not selected_dest_views:
    forms.alert('No destination view was selected. Please Try Again.', exitscript=True)

dest_views = [dict_all_view[v_name] for v_name in selected_dest_views]



# Step 5: Copy view filters
with Transaction(doc, __title__) as t:
    t.Start()
    for view_filter in filters_to_copy:
        filter_overrides = src_view.GetFilterOverrides(view_filter.Id)

        for view in dest_views:
            view.SetFilterOverrides(view_filter.Id, filter_overrides)
    t.Commit()


# Filter ovverides

# Apply filter

# ovveride toggle
