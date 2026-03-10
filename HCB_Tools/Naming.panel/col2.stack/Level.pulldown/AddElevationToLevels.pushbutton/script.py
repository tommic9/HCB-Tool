# -*- coding: utf-8 -*-
__title__ = "AddElevationToLevel"
__doc__   = """To narzędzie nadpisuje nazwę poziomu dodając wysokość poziomu.

Jak korzystać:
- Change settings (optional)
- Rename Levels

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# Imports
import os, sys, math, datetime, time
from Autodesk.Revit.DB import *
from pyrevit import revit, forms
import clr
from System.Collections.Generic import List
from Snippets._convert import convert_internal_units
from Snippets._selection import get_selected_elements

# Domyślne symbole
DEFAULT_PREFIX = "⌞"
DEFAULT_SUFFIX = "⌝"

# --- Pytanie do użytkownika o prefiks/sufiks ---
message = (
    "Czy chcesz użyć domyślnych prefiksu i sufiksu?\n"
    "Prefiks: {0}   \n Sufiks: {1}".format(DEFAULT_PREFIX, DEFAULT_SUFFIX)
)
use_default = forms.alert(
    message,
    ok=False, yes=True, no=True,
    title="Prefiks i sufiks"
)

if use_default:
    prefix = DEFAULT_PREFIX
    suffix = DEFAULT_SUFFIX
else:
    prefix = forms.ask_for_string(
        prompt="Podaj własny prefiks:",
        default=DEFAULT_PREFIX,
        title="Prefiks"
    ) or DEFAULT_PREFIX
    suffix = forms.ask_for_string(
        prompt="Podaj własny sufiks:",
        default=DEFAULT_SUFFIX,
        title="Sufiks"
    ) or DEFAULT_SUFFIX

# Zmienne globalne
doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
app   = __revit__.Application
PATH_SCRIPT = os.path.dirname(__file__)

# Pobierz wszystkie poziomy
all_levels = FilteredElementCollector(doc) \
    .OfCategory(BuiltInCategory.OST_Levels) \
    .WhereElementIsNotElementType() \
    .ToElements()

# Uruchom transakcję
autoTrans = not doc.IsModifiable
t = Transaction(doc, __title__)
t.Start()

for lvl in all_levels:
    elev = lvl.Elevation
    # Konwersja z jednostek wewnętrznych na metry
    m_val = round(convert_internal_units(elev, False, 'm'), 2)
    # Buduj tekst z jednostką
    val_str = "{:.2f} m".format(m_val)
    if elev > 0:
        val_str = "+" + val_str
    new_label = prefix + val_str + suffix

    # Stara i nowa nazwa
    old_name = lvl.Name
    if prefix in old_name and suffix in old_name:
        base = old_name.split(prefix)[0].rstrip()
        new_name = base + " " + new_label
    else:
        new_name = old_name + " " + new_label

    # Przypisz nową nazwę
    try:
        lvl.Name = new_name
    except Exception as e:
        print("Could not change level's name:", e)
    else:
        print("Renamed: '{0}' --> '{1}'".format(old_name, new_name))

if autoTrans:
    t.Commit()
