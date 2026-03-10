# -*- coding: utf-8 -*-
__title__ = "RenameLevels"
__doc__   = """To narzędzie pozwala na masową zmianę nazw poziomów poprzez dodanie prefiksu, zamianę tekstu oraz sufiksu.

Jak korzystać:
- Możesz wybrać pojedyncze poziomy w modelu, wtedy skrypt zmieni nazwy tylko tych.
- Jeśli nie wybierzesz nic, skrypt operuje na wszystkich poziomach w projekcie.
- Podaj prefiks, tekst do znalezienia, tekst zastępczy oraz sufiks.

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# Imports
import os, sys
from Autodesk.Revit.DB import *
from System.Collections.Generic import List
from pyrevit import revit, forms
import clr
from Snippets._selection import get_selected_elements

# --- Dialog z polami do wprowadzenia tekstu ---
def _prompt_for_value(label):
    value = forms.ask_for_string(
        prompt=label,
        title=__title__,
        default=""
    )
    if value is None:
        forms.alert(
            'Działanie przerwane przez użytkownika.',
            title=__title__,
            exitscript=True
        )
    return value or ""

inputs = {
    'prefix': _prompt_for_value('Prefiks:'),
    'find': _prompt_for_value('Znajdź:'),
    'replace': _prompt_for_value('Zamień na:'),
    'suffix': _prompt_for_value('Sufiks:')
}

prefix  = inputs.get('prefix', '')
find    = inputs.get('find', '')
replace = inputs.get('replace', '')
suffix  = inputs.get('suffix', '')

# Zmienne globalne
doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
app   = __revit__.Application
PATH_SCRIPT = os.path.dirname(__file__)

# Pobierz wybrane elementy i przefiltruj poziomy
selected = get_selected_elements()
selected_levels = [el for el in selected if isinstance(el, Level)]

# Jeśli nic nie wybrano, pobierz wszystkie poziomy
if selected_levels:
    target_levels = selected_levels
else:
    target_levels = FilteredElementCollector(doc) \
        .OfCategory(BuiltInCategory.OST_Levels) \
        .WhereElementIsNotElementType() \
        .ToElements()

# Start transakcji
autoTrans = not doc.IsModifiable
if autoTrans:
    t = Transaction(doc, __title__)
    t.Start()

# Iteracja i zmiana nazw dla poziomów z listy target_levels
for lvl in target_levels:
    current_name = lvl.Name
    # zamiana tekstu i dodanie prefiksu/sufiksu
    new_base = current_name.replace(find, replace)
    new_name = prefix + new_base + suffix

    # próba zmiany z retry (na wypadek konfliktów)
    for i in range(20):
        try:
            lvl.Name = new_name
            print("{0} -> {1}".format(current_name, new_name))
            break
        except:
            new_name += "*"

# Zakończ transakcję
if autoTrans:
    t.Commit()
