# -*- coding: utf-8 -*-
__title__ = "DuctFittingsArea MAGICAD"
__doc__ = """Date = 15.10.2024
_____________________________________________________________________
Komentarz:
Skrypt obliczający powierzchnię DuctFittings zgodnie z wzorami normy DIN dla kształtek MagiCAD

Działanie:
-Skrypt oblicza powierzchnię kształtek w całym projekcie nadpisując parametr "HC_Area"

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# Imports
import math
import clr
import time

# Autodesk i .NET Imports
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import *
from Autodesk.Revit.DB.Mechanical import *
from pyrevit import forms, script

clr.AddReference("System")
from System.Collections.Generic import List

# Mierzenie czasu rozpoczęcia
start_time = time.clock()

# def convert_internal_units(value, get_internal=True, unit_type='length', doc=None):
#     """
#     Function to convert Internal units to meters or square meters, or vice versa.
#     :param value:        Value to convert
#     :param get_internal: True to get internal units, False to get meters or square meters
#     :param unit_type:    'length' for meters, 'area' for square meters
#     :param doc:          Revit document object
#     :return:             Length or area in Internal units or meters/square meters
#     """
#     if doc is None:
#         doc = __revit__.ActiveUIDocument.Document
#
#     units = doc.GetUnits()
#
#     if unit_type == 'length':
#         if get_internal:
#             return UnitUtils.ConvertToInternalUnits(value, units.GetFormatOptions(SpecTypeId.Length).GetUnitTypeId())
#         else:
#             return UnitUtils.ConvertFromInternalUnits(value, units.GetFormatOptions(SpecTypeId.Length).GetUnitTypeId())
#     elif unit_type == 'area':
#         if get_internal:
#             return UnitUtils.ConvertToInternalUnits(value, units.GetFormatOptions(SpecTypeId.Area).GetUnitTypeId())
#         else:
#             return UnitUtils.ConvertFromInternalUnits(value, units.GetFormatOptions(SpecTypeId.Area).GetUnitTypeId())
#     else:
#         raise ValueError("Unsupported unit_type. Use 'length' or 'area'.")

#Funkcja konwersji jednostek
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

    if get_internal:
        return UnitUtils.ConvertToInternalUnits(value, units)
    return UnitUtils.ConvertFromInternalUnits(value, units)

# Funkcja do pobierania wartości parametru
def get_param_value(param):
    """Get a value from a Parameter based on its StorageType."""
    if not param:
        return None

    storage_type = param.StorageType
    if storage_type == StorageType.Double:
        return param.AsDouble()
    elif storage_type == StorageType.ElementId:
        return param.AsElementId()
    elif storage_type == StorageType.Integer:
        return param.AsInteger()
    elif storage_type == StorageType.String:
        return param.AsString()
    else:
        print('Parameter [{}] is not available for this element'.format(param.Definition.Name))
        return None

# Definicja wymaganych parametrów dla każdego typu kształtki

required_params = {
        'BA': ['a', 'b', 'd', 'e', 'f', 'r', 'Alpha'],
        'BO': ['a', 'b'],
        'BS': ['a', 'b', 'e', 'f', 'r', 'Alpha'],
        'ES': ['l', 'a', 'b', 'e'],
        'OA': ['l', 'a', 'b', 'c', 'd', 'e', 'f'],
        'OS': ['l', 'a', 'b', 'c', 'd', 'e', 'f'],
        'RA': ['l', 'a', 'b', 'd', 'e', 'f'],
        'RS': ['a', 'b', 'd', 'e', 'f'],
        'SU': ['l', 'a', 'b', 'd', 'r'],
        'SU-Fase': ['RLT_DIN_l', 'a', 'b', 'd'],
        'TD': ['l', 'a', 'b', 'c', 'd', 'h', 'r'],
        'TG': ['l', 'a', 'b', 'd', 'h', 'm'],
        'UA': ['l', 'a', 'b', 'c', 'd', 'e'],
        'US': ['l', 'a', 'b', 'c', 'd', 'e', 'f'],
        'WA': ['a', 'b', 'd', 'e', 'f', 'r', 'Alpha'],
        'WS': ['a', 'b', 'd', 'e', 'f', 'r', 'Alpha'],
        'HS': ['a', 'b', 'd', 'e', 'm', 'l', 'h']
    }

# Funkcja do obliczania obwodu (OB) i długości (L) na podstawie typu kształtki i parametrów
def calculate_l_obw(dim_type, params):
    l, obw = None, None

    if dim_type == 'BS':
        obw = 2 * (params['a'] + params['b'])
        l = (params['Alpha'] * (params['r'] + params['b'])) + params['e'] + params['f']
    elif dim_type == 'BO':
        obw = (2 * (params['a'] + params['b']))
        l = 1
    elif dim_type == 'BA':
        if params['b'] >= params['d']:
            obw = 2 * (params['a'] + params['b'])
            l = (params['Alpha'] * (params['r'] + params['b'])) + params['e'] + params['f']
        else:
            obw = 2 * (params['a'] + params['d'])
            l = (params['Alpha'] * (params['r'] + params['b'])) + params['e'] + params['f']
    elif dim_type == 'WS':
        obw = 2 * (params['a'] + params['b'])
        l = 2 * params['b'] + params['e'] + params['f']
    elif dim_type == 'WA':
        if params['b'] >= params['d']:
            obw = 2 * (params['a'] + params['b'])
            l = params['b'] + params['d'] + params['e'] + params['f']
        else:
            obw = 2 * (params['a'] + params['d'])
            l = params['b'] + params['d'] + params['e'] + params['f']
    elif dim_type == 'US':
        if params['a'] + params['b'] >= params['c'] + params['d']:
            obw = 2 * (params['a'] + params['b'])
            l = math.sqrt(params['l'] ** 2 + (params['e'] ** 2))
        else:
            obw = 2 * (params['c'] + params['d'])
            l = math.sqrt(params['l'] ** 2 + (params['f'] ** 2))
    elif dim_type == 'SU-Fase':
        if params['a'] + params['b'] >= params['c'] + params['d']:
            obw = 2 * (params['a'] + params['b'])
            l = math.sqrt(params['RLT_DIN_l'])
        else:
            obw = 2 * (params['c'] + params['d'])
            l = math.sqrt(params['RLT_DIN_l'])
    elif dim_type == 'UA':
        if params['a'] + params['b'] >= params['c'] + params['d']:
            obw = 2 * (params['a'] + params['b'])
            l = math.sqrt(params['l'] ** 2 + (params['b'] - params['d'] + params['e']) ** 2)
        else:
            obw = 2 * (params['c'] + params['d'])
            l = math.sqrt(params['l'] ** 2 + (params['e'] ** 2))
    elif dim_type == 'OS':
        if params['a'] + params['b'] >= 2 * math.pi * math.sqrt((2 * params['d'] + 2 * params['c']) / 2):
            obw = 2 * (params['a'] + params['b'])
            l = math.sqrt(params['l'] ** 2 + (params['e'] ** 2))
        else:
            obw = 2 * math.pi * math.sqrt((2 * params['d'] + 2 * params['c']) / 2)
            l = math.sqrt(params['l'] ** 2 + (params['f'] ** 2))
    elif dim_type == 'OA':
        if params['a'] + params['b'] >= (2 * math.pi * math.sqrt((2 * params['d'] + 2 * params['c']) / 2)) / 2:
            obw = 2 * (params['a'] + params['b'])
        else:
            obw = 2 * math.pi * math.sqrt((2 * params['d'] + 2 * params['c']) / 2)
        if params['b'] - params['d'] + params['e'] >= params['e']:
            l = math.sqrt(params['l'] ** 2 + (params['b'] - params['d'] + params['e']) ** 2)
        elif params['a'] - params['d'] + params['f'] >= params['f']:
            l = math.sqrt(params['l'] ** 2 + (params['a'] - params['d'] + params['f']) ** 2)
        else:
            l = math.sqrt(params['l'] ** 2 + max(params['e'], params['f']) ** 2)
    elif dim_type == 'RS':
        if params['a'] + params['b'] >= (math.pi * params['d']) / 2:
            obw = 2 * (params['a'] + params['b'])
        else:
            obw = math.pi * params['d']
        if params['e'] >= params['f']:
            l = math.sqrt(params['l'] ** 2 + (params['e'] ** 2))
        else:
            l = math.sqrt(params['l'] ** 2 + (params['f'] ** 2))
    elif dim_type == 'RA':
        if params['a'] + params['b'] >= (math.pi * params['d']) / 2:
            obw = 2 * (params['a'] + params['b'])
        else:
            obw = math.pi * params['d']
        if params['b'] - params['d'] + params['e'] >= params['e']:
            l = math.sqrt(params['l'] ** 2 + (params['b'] - params['d'] + params['e']) ** 2)
        elif params['a'] - params['d'] + params['f'] >= params['f']:
            l = math.sqrt(params['l'] ** 2 + (params['a'] - params['d'] + params['f']) ** 2)
        else:
            l = math.sqrt(params['l'] ** 2 + max(params['e'], params['f']) ** 2)
    elif dim_type == 'ES':
        obw = 2 * (params['a'] + params['b'])
        l = math.sqrt(params['l'] ** 2 + params['e'] ** 2)
    elif dim_type == 'EA':
        if params['b'] >= params['d']:
            obw = 2 * (params['a'] + params['b'])
            l = math.sqrt(params['l'] ** 2 + (params['b'] - params['d'] + params['e']) ** 2)
        else:
            obw = 2 * (params['c'] + params['d'])
            l = math.sqrt(params['l'] ** 2 + (params['e'] ** 2))
    elif dim_type == 'TG':
        if params['a'] + params['b'] >= params['a'] + params['d']:
            obw1 = 2 * (params['a'] + params['b'])
        else:
            obw1 = 2 * (params['a'] + params['d'])
        l1 = params['l']
        obw2 = 2 * (params['a'] + params['h'])
        if params['d'] + params['m'] - params['b'] >= params['m']:
            l2 = params['d'] + params['m'] - params['b']
        else:
            l2 = params['m']
        l = l1 * obw1 + l2 * obw2
        obw = 1
    elif dim_type == 'TA':
        if params['b'] >= params['d']:
            obw1 = 2 * (params['a'] + params['b'])
        else:
            obw1 = 2 * (params['c'] + params['d'])
        l1 = math.sqrt(params['l'] ** 2 + (params['e'] ** 2))
        obw2 = 2 * (params['a'] + params['h'])
        if params['d'] + params['m'] - params['b'] - params['e'] >= params['m']:
            l2 = params['d'] + params['m'] - params['b'] - params['e']
        else:
            l2 = params['m']
        l = l1 * obw1 + l2 * obw2
        obw = 1
    elif dim_type == 'HS':
        params['m'] = max(params['m'], 100)  # ensure m is at least 100
        if params['b'] >= params['d'] + params['m'] + params['h']:
            obw = 2 * (params['a'] + params['b'])
            l = math.sqrt(params['l'] ** 2 + (params['b'] - params['h'] - params['m'] - params['d'] + params['e']) ** 2)
        else:
            obw = 2 * (params['c'] + params['d'] + params['m'] - params['h'])
            l = math.sqrt(params['l'] ** 2 + (params['e'] ** 2))
    else:
        raise ValueError("Nieznany typ kształtki: {}".format(dim_type))

    return l, obw

# Główna funkcja
def main():
    global df_linkify
    doc = __revit__.ActiveUIDocument.Document
    uidoc = __revit__.ActiveUIDocument
    app = __revit__.Application
    output = script.get_output()

    # Pobranie wszystkich kształtek wentylacyjnych
    all_duct_fittings = FilteredElementCollector(doc) \
        .OfCategory(BuiltInCategory.OST_DuctFitting) \
        .OfClass(FamilyInstance) \
        .ToElements()

    # Filtracja elementów z parametrem Size zawierającym znak "x"
    all_duct_fittings = [df for df in all_duct_fittings if df.LookupParameter("Size").AsString().Contains("x")]

    # Mapowanie nazw parametrów
    sparameter = ['a', 'b', 'c', 'd', 'e', 'f', 'h', 'l', 'm', 'm2', 'n', 'r', 'r1', 'r2', 'r3', 'r4', 't']
    parameter_names = ['DIN_' + p for p in sparameter]
    parameter_names.append('Alpha')
    parameter_names.append('RLT_DIN_l')

    # Przetwarzanie każdej kształtki wentylacyjnej
    for df in all_duct_fittings:
        # Pobieranie wartości parametrów
        param_values = {p: get_param_value(df.LookupParameter('DIN_' + p)) for p in sparameter}
        param_values['Alpha'] = get_param_value(df.LookupParameter('Alpha'))

        HC_Area = df.LookupParameter("HC_Area")
        dim_type = get_param_value(df.LookupParameter("RLT_DIN_KZ"))
        df_linkify = output.linkify(df.Id, df.Symbol.Family.Name)

        # Sprawdzanie brakujących parametrów przed obliczeniami
        if dim_type not in required_params:
            print("Nieznany typ kształtki: {}".format(dim_type))
            continue

        missing_params = [p for p in required_params[dim_type] if param_values.get(p) is None]
        if missing_params:
            print("Nie znaleziono parametru {} dla elementu {} o ID {}: {}".format(missing_params, dim_type, df.Id, df_linkify))
            continue

        # Sprawdzanie brakujących parametrów przed obliczeniami
        try:
            l, obw = calculate_l_obw(dim_type, param_values)
            L_m = convert_internal_units(l,True, 'm')
            Obw_m = convert_internal_units(obw, True, 'm')
            area = L_m * Obw_m
            round_up_area = round(area, 2) / 10


            with Transaction(doc, __title__) as t:
                t.Start()

                if HC_Area:
                    if round_up_area<1:
                        HC_Area.Set(1)
                    else:
                        HC_Area.Set(round_up_area)
                else:
                    print("Parameter 'HC_Area' not found in element {}".format(df.Id))
                t.Commit()
        except ValueError as e:
            print(e)
            continue

        # Drukowanie szczegółów tylko jeśli którykolwiek parametr ma wartość inną niż None
        if any(value is not None for value in param_values.values()):
            print("TYP: {} , Fam_Name: {}, ID: {}".format(dim_type, df_linkify, df.Id))
            if HC_Area:
                print("HC_Area: {} m²".format(round_up_area/10 if round_up_area/10 >= 1 else 1))
            print('-' * 50)

if __name__ == "__main__":
    main()

# Mierzenie czasu zakończenia
end_time = time.clock()
execution_time = end_time - start_time
print('-' * 50)
print("Time of script:{} sec.".format(execution_time))