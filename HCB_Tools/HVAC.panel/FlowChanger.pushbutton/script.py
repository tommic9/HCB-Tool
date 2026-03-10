# -*- coding: utf-8 -*-

__title__ = "FlowExchanger"
__doc__ = """Narzędzie które różnicuje wartości "Actual Flow" w zakresie 2% dla "Air Terminal" w aktwynym widoku, na podstawie parametru "Flow""

How-to:
- Ustaw wiodok z terminalami do zmiany
- Click on the button
- Change settings (optional)

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
# Regular + Autodesk
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import *
from Autodesk.Revit.DB.Mechanical import *
# pyRevit
from pyrevit import forms


# .NET Imports
import clr
clr.AddReference("System")
import random
from System.Collections.Generic import List

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ VARIABLES
# ==================================================
doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
app   = __revit__.Application

# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ MAIN
# ==================================================

def convert_internal_units(value, get_internal=True, units='mm'):
    # type: (float, bool, str) -> float
    """Funkcja do konwersji jednostek wewnętrznych na metry lub odwrotnie.
    :param value:        Wartość do konwersji
    :param get_internal: True, aby uzyskać jednostki wewnętrzne, False, aby uzyskać metry
    :param units:        Wybierz pożądane jednostki: ['m', 'm2', 'cm', 'mm']
    :return:             Długość w jednostkach wewnętrznych lub metrach."""

    from Autodesk.Revit.DB import UnitTypeId
    if units == 'm':
        units = UnitTypeId.Meters
    elif units == "m2":
        units = UnitTypeId.SquareMeters
    elif units == 'cm':
        units = UnitTypeId.Centimeters
    elif units == 'mm':
        units = UnitTypeId.Millimeters
    elif units == 'm3/h':
        units = UnitTypeId.CubicMetersPerHour

    if get_internal:
        return UnitUtils.ConvertToInternalUnits(value, units)
    return UnitUtils.ConvertFromInternalUnits(value, units)

# Function to get air terminals in the active view
def get_air_terminals_in_active_view(doc):
    active_view = doc.ActiveView
    collector = FilteredElementCollector(doc, active_view.Id)
    return collector.OfCategory(BuiltInCategory.OST_DuctTerminal).WhereElementIsNotElementType().ToElements()

from rpw.ui.forms import (FlexForm, Label, TextBox, Separator, Button)
components = [  Label('Project Parameter Name for new value:'),   TextBox('param_name'),
                Separator(),
                Button('Set parameter name')]
form = FlexForm('Title', components)
form.show()

user_inputs = form.values
actual_flow_param_name = user_inputs['param_name']
#actual_flow_param_name = "Actual Flow"

air_terminals = get_air_terminals_in_active_view(doc)

 # Start a transaction
with Transaction(doc, __title__) as t:
    t.Start()

    # Iterate through each air terminal and update the flow parameter
    for el in air_terminals:
        flow_param = el.get_Parameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM)
        if flow_param and flow_param.StorageType == StorageType.Double:
            flow_value_internal = flow_param.AsDouble()

            # Convert from internal units to m3/h
            flow_value = convert_internal_units(flow_value_internal, get_internal=False, units='m3/h')

            # Add variation within ±2%
            variation = random.triangular(0.97, 1.02,1)
            new_actual_flow_value = flow_value * variation

            # Round to the nearest integer and convert to double
            new_actual_flow_value_int = int(new_actual_flow_value)
            actualflow_dbl_nonint = float(new_actual_flow_value_int)
            #actualflow_dbl_nonint = float(round(new_actual_flow_value))
            #actualflow_dbl_nonint = float(new_actual_flow_value)


            actualflow_dbl = convert_internal_units(actualflow_dbl_nonint, get_internal=True, units='m3/h')

            # Set the new value for the "Actual Flow" parameter
            actualflow_param = el.LookupParameter(actual_flow_param_name)
            if actualflow_param and actualflow_param.StorageType == StorageType.Double:
                actualflow_param.Set(actualflow_dbl)

            #printing the results
            print ("Flow {} m3/h --> Actual Flow {} m3/h (* {})".format(flow_value, actualflow_dbl_nonint,variation))
            print ()
    t.Commit()

