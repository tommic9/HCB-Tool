
# -*- coding: utf-8 -*-
__title__ = "Rename Sheets"
__doc__ = """Date    =  14.11.2023
_____________________________________________________________________
Description:
Rename sheets with Replace, Prefix and Sufix

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
# Regular + Autodesk
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import ObjectType, Selection, ISelectionFilter
# pyRevit
from pyrevit import forms

# .NET Imports
import clr
clr.AddReference("System")

# Custom
from Snippets._selection import get_selected_elements
from Autodesk.Revit.UI import DockablePane, DockablePanes

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ VARIABLES
# ==================================================
doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
app   = __revit__.Application

selection = uidoc.Selection # type: Selection

# Main

# 1. GET SHEETS
selected_elements = get_selected_elements()
selected_sheets   = [el for el in selected_elements if issubclass(type(el), ViewSheet)]

if not selected_sheets:
    selected_sheets = forms.select_sheets()

    if not selected_sheets:
        forms.alert('Nie wybrano arkusza. \n'
                    'Spróbuj ponownie' , exitscript=True )

project_browser_id = DockablePanes.BuiltInDockablePane.ProjectBrowser
project_browser    = DockablePane(project_browser_id)
project_browser.Hide()
# 2. GET USER INPUT

# Window with field to insert a prefix, find, replace and suffix
from rpw.ui.forms import (FlexForm, Label, TextBox, Separator, Button)
components = [  Label('Number Prefix:'),   TextBox('number_prefix'),
                Label('Number:'),   TextBox('number'),
                Label('Prefix:'),   TextBox('prefix'),
                Label('Find:'),     TextBox('find'),
                Label('Replace:'),  TextBox('replace'),
                Label('Suffix:'),   TextBox('suffix'),
                Separator(),
                Button('Rename Sheets')]
form = FlexForm('Title', components)
form.show()

# value of inputs
user_inputs = form.values
number_prefix    = user_inputs['number_prefix']
number           = user_inputs['number']
prefix           = user_inputs['prefix']
find             = user_inputs['find']
replace          = user_inputs['replace']
suffix           = user_inputs['suffix']

# iteration for every selected sheets
for sheets in selected_sheets:

    # 3. NEW NAME AND NUMBER
    current_name = sheets.Name
    current_number = sheets.SheetNumber

    # method of rename sheets - like a manually
    new_name     = prefix + sheets.Name.replace(find, replace) + suffix
    new_number   = number_prefix + sheets.SheetNumber.replace(find, replace)

    # 4. RENAME SHEETS

    with Transaction(doc, __title__) as t:
        t.Start()

        for i in range(20):
            try:
                # printing info about old name compare to new name
                sheets.Name         = new_name
                sheets.SheetNumber  = new_number
                print ("{} -> {}".format(current_name, new_name))
                print ("{} -> {}".format(current_number, new_number))
                break
            except:
                new_name    += "*"
                new_number  += "*"

        t.Commit()

project_browser.Show()

