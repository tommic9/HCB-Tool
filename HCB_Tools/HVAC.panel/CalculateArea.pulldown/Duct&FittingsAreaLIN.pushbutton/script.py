# -*- coding: utf-8 -*-
__title__ = "Ducts&Fittings Area LINEAR"
__doc__ = """Date = 20.08.2025
_____________________________________________________________________
Komentarz:
Skrypt obliczający powierzchnię DuctFittings zgodnie z wzorami normy DIN dla kształtek Linear
oraz
Kopiuje Area dla Ducts do HC_Area

Działanie:
- Skrypt oblicza powierzchnię elementów Ducts i Ducts Fitting w całym projekcie nadpisując parametr "HC_Area"

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
from Snippets._convert import convert_internal_units


clr.AddReference("System")
from System.Collections.Generic import List

# Pomiar czasu startu
start_time = time.clock()

# Pobranie wartości parametru
def get_param_value(param):
    if not param: return None
    st = param.StorageType
    if st == StorageType.Double:    return param.AsDouble()
    if st == StorageType.Integer:   return param.AsInteger()
    if st == StorageType.ElementId: return param.AsElementId()
    if st == StorageType.String:    return param.AsString()
    return None

# Parametry wymagane dla typów
required_params = {
    'BA': ['A','B','D','E','F','R','LIN_VE_ANG_W'],
    'BO': ['A','B','E'],
    'BS': ['A','B','D','E','F','R','LIN_VE_ANG_W'],
    'ES': ['L','A','B','D','E','R'],
    'EA': ['L','A','B','D','E','R'],
    'OA': ['L','A','B','C','D','E','F','M','T'],
    'OS': ['L','A','B','C','D','E','F','M','T'],
    'RA': ['L','A','B','C','D','E','F','M'],
    'RS': ['L','A','B','C','D','E','F','M'],
    'SU': ['L','A','B','D','R'],
    'TD': ['L','A','B','C','D','H','R'],
    'TG': ['L','A','B','D','H','M','N','R1','R2'],
    'TA': ['L','A','B','D','E','H','M','N','R1','R2'],
    'UA': ['L','A','B','C','D','E','F'],
    'US': ['L','A','B','C','D','E','F'],
    'WA': ['A','B','D','E','F','R','LIN_VE_ANG_W'],
    'WS': ['A','B','D','E','F','R','LIN_VE_ANG_W'],
    'HS': ['A','B','D','E','H','L','M']
}

# Funkcja obliczająca pole kształtki bezpośrednio (w jednostkach wewnętrznych)
def calculate_area_internal(dim_type, params):
    # walidacja
    req = required_params.get(dim_type, [])
    missing = [p for p in req if params.get(p) is None]
    if missing:
        raise ValueError("Brak parametrów dla {}: {}".format(dim_type, missing))

    # skróty literowe
    a, b, c, d, e, f, h, l, m, n, r, r1, r2 = (
        params.get('A'), params.get('B'), params.get('C'), params.get('D'),
        params.get('E'), params.get('F'), params.get('H'), params.get('L'),
        params.get('M'), params.get('N'), params.get('R'), params.get('R1'), params.get('R2')
    )
    ang = params.get('LIN_VE_ANG_W')

    # dla typów liniowych pole = obwód * długość
    def simple_area(obw, L): return obw * L

    if dim_type == 'BS':
        obw = 2*(a + b)
        L   = (ang*(r + b)) + e + f
        return simple_area(obw, L)

    if dim_type == 'BO':
        obw = 1
        L   = (2*(a + b))*e + a*b
        return simple_area(obw, L)

    if dim_type == 'BA':
        obw = 2*(a + max(b,d))
        L   = (ang*(r + b)) + e + f
        return simple_area(obw, L)

    if dim_type == 'WS':
        obw = 2*(a + b)
        L   = 2*b + e + f
        return simple_area(obw, L)

    if dim_type == 'WA':
        obw = 2*(a + max(b,d))
        L   = b + d + e + f
        return simple_area(obw, L)

    if dim_type == 'US':
        if a + b >= c + d:
            obw = 2*(a + b)
            L   = math.sqrt(l**2 + e**2)
        else:
            obw = 2*(c + d)
            L   = math.sqrt(l**2 + f**2)
        return simple_area(obw, L)

    if dim_type == 'UA':
        if a + b >= c + d:
            obw = 2*(a + b)
            L   = math.sqrt(l**2 + (b-d+e)**2)
        else:
            obw = 2*(c + d)
            L   = math.sqrt(l**2 + e**2)
        return simple_area(obw, L)

    if dim_type == 'OS':
        pi_term = 2*math.pi*math.sqrt((2*d+2*c)/2)
        if a + b >= pi_term:
            obw = 2*(a + b)
            L   = math.sqrt(l**2 + e**2)
        else:
            obw = pi_term
            L   = math.sqrt(l**2 + f**2)
        return simple_area(obw, L)

    if dim_type == 'OA':
        pi_term = 2*math.pi*math.sqrt((2*d+2*c)/2)
        obw = 2*(a+b) if a+b >= (pi_term/2) else pi_term
        if b-d+e >= e:
            L = math.sqrt(l**2 + (b-d+e)**2)
        elif a-d+f >= f:
            L = math.sqrt(l**2 + (a-d+f)**2)
        else:
            L = math.sqrt(l**2 + max(e,f)**2)
        return simple_area(obw, L)

    if dim_type in ('RS','RA'):
        obw = 2*(a + b) if a+b >= (math.pi*d)/2 else math.pi*d
        if dim_type == 'RS':
            L = math.sqrt(l**2 + max(e,f)**2)
        else:
            if b-d+e >= e:
                L = math.sqrt(l**2 + (b-d+e)**2)
            elif a-d+f >= f:
                L = math.sqrt(l**2 + (a-d+f)**2)
            else:
                L = math.sqrt(l**2 + max(e,f)**2)
        return simple_area(obw, L)

    if dim_type == 'ES':
        obw = 2*(a + b)
        L   = math.sqrt(l**2 + e**2)
        return simple_area(obw, L)

    if dim_type == 'EA':
        if b >= d:
            obw = 2*(a + b)
            L   = math.sqrt(l**2 + (b-d+e)**2)
        else:
            obw = 2*(c + d)
            L   = math.sqrt(l**2 + e**2)
        return simple_area(obw, L)

    if dim_type == 'TG':
        obw1 = 2*(a + max(b,d))
        L1   = l
        obw2 = 2*(a + h)
        L2   = d + m - b if d+m-b >= m else m
        return (obw1*L1) + (obw2*L2)

    if dim_type == 'TA':
        obw1 = 2*(a + b) if b>=d else 2*(a + d)
        L1   = math.sqrt(l**2 + e**2)
        obw2 = 2*(a + h)
        L2   = d + m - b - e if d+m-b-e >= m else m
        return (obw1*L1) + (obw2*L2)

    # if dim_type == 'HS':
    #     m = max(m, 0.3281) #100mm w stopach
    #     if b >= d+m+h :
    #         obw = 2*(a + b)
    #         if b-h-m-d+e>=e:
    #             L = math.sqrt(l ** 2 + (b - h - m - d + e) ** 2)
    #         elif b-h-m-d+e<e:
    #             L = math.sqrt(l ** 2 + e ** 2)
    #         else:
    #             print("Kształtka nie spiełnia kryteriów")
    #         return L
    #     elif b<d+m+h:
    #         obw = 2*(c + d + m + h)
    #         if b - h - m - d + e >= e:
    #             L = math.sqrt(l ** 2 + (b - h - m - d + e) ** 2)
    #         elif b - h - m - d + e < e:
    #             L = math.sqrt(l ** 2 + e ** 2)
    #         else:
    #             print("Kształtka nie spiełnia kryteriów")
    #         return L
    #     else:
    #         print("Kształtka nie spiełnia kryteriów")
    #     return simple_area(obw, L)

    if dim_type == 'HS':
        # 1) minimalne m = 100 mm → internal units (ft)
        min_m_internal = convert_internal_units(100.0, True, 'mm')
        m = max(m, min_m_internal)

        if b >= d + m + h:
            obw = 2 * (a + b)
        else:
            obw = 2 * (a + d + m + h)

        delta = b - h - m - d + e
        if delta >= e:
            L = math.sqrt(l ** 2 + delta ** 2)
        else:
            L = math.sqrt(l ** 2 + e ** 2)

        return simple_area(obw, L)

    if dim_type == 'SU':
        obw = 2*(a + b)
        L   = l
        return simple_area(obw, L)

    # domyślnie błąd jeśli nieobsługiwany
    raise ValueError("Nieznany typ kształtki: {}".format(dim_type))


def _collect_ducts(doc):
    return (FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_DuctCurves)
            .WhereElementIsNotElementType())

def calculate_duct(doc, output):
    """Zsumuj długości kanałów wg kształtu po znaku 'x' w Size."""
    ducts = _collect_ducts(doc)

    total_length_rect = 0.0
    total_length_round = 0.0
    rect_ducts = []
    round_ducts = []

    for duct in ducts:
        length_param = duct.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)
        if not length_param:
            link = output.linkify(duct.Id)
            print(u"Brak parametru długości dla {}".format(link))
            continue

        size_param = (duct.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE)
                      or duct.LookupParameter("Size"))
        size_text = size_param.AsString() if size_param else ""
        txt = size_text.lower() if size_text else ""

        length_m = convert_internal_units(length_param.AsDouble(), False, 'm')

        if u"x" in txt or u"×" in size_text:
            rect_ducts.append(duct)
            total_length_rect += length_m
        else:
            round_ducts.append(duct)
            total_length_round += length_m

    return total_length_rect, total_length_round, rect_ducts, round_ducts


def copy_duct_area(doc, output, tx_title="Copy HC_Area", copy=True):
    """Skopiuje BuiltIn Area -> parametr 'HC_Area' dla kanałów i zlicza pola."""
    ducts = _collect_ducts(doc)
    total_duct_count = 0
    total_duct_area_m2 = 0.0
    rect_duct_area_m2 = 0.0

    for duct in ducts:
        area_param = duct.get_Parameter(BuiltInParameter.RBS_CURVE_SURFACE_AREA)
        hc_param = duct.LookupParameter('HC_Area') if copy else None
        link = output.linkify(duct.Id)
        size_param = (duct.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE)
                      or duct.LookupParameter("Size"))
        size_text = size_param.AsString() if size_param else ""
        txt = size_text.lower() if size_text else ""

        if area_param:
            area_m2 = convert_internal_units(area_param.AsDouble(), False, 'm2')
            val = round(area_m2, 3)
            if copy:
                if hc_param:
                    with Transaction(doc, tx_title) as t:
                        t.Start()
                        hc_param.Set(val)
                        t.Commit()
                    print(u"{} → HC_Area = {} m²".format(link, val))
                else:
                    print(u"Brak parametru HC_Area dla {}".format(link))
            total_duct_area_m2 += val
            if u"x" in txt or u"×" in size_text:
                rect_duct_area_m2 += val
            total_duct_count += 1
        else:
            print(u"Brak parametru Area dla {}".format(link))

    return total_duct_count, total_duct_area_m2, rect_duct_area_m2

# Główna funkcja

def main():
    doc    = __revit__.ActiveUIDocument.Document
    output = script.get_output()

    # kopiowanie pola kanałów
    total_len_rect, total_len_round, rect_ducts, round_ducts = calculate_duct ( doc, output )

    copy_choice = forms.alert(
        u"Czy kopiować wartości Area dla Ducts?",
        options=["Tak", "Nie"],
        title="Kopiowanie Area"
    )

    count_area, total_duct_area, rect_duct_area = copy_duct_area(doc, output, copy=(copy_choice == "Tak"))

    # Zliczanie i suma dla kształtek
    total_df_count = 0
    total_df_area_m2 = 0.0

    ducts_fittings = FilteredElementCollector(doc) \
          .OfCategory(BuiltInCategory.OST_DuctFitting) \
          .OfClass(FamilyInstance) \
          .ToElements()
    duct_fittings = [df for df in ducts_fittings if df.LookupParameter("Size").AsString().Contains("x")]

    # parametry LIN_VE_DIM_
    spar = ['A','B','C','D','E','F','H','L','M','N','R','R1','R2']

    for df in duct_fittings:
        # pobranie wartości
        pv = {p: get_param_value(df.LookupParameter('LIN_VE_DIM_'+p)) for p in spar}
        pv['LIN_VE_ANG_W'] = get_param_value(df.LookupParameter('LIN_VE_ANG_W'))

        HC_A = df.LookupParameter('HC_Area')
        typ   = get_param_value(df.LookupParameter('LIN_VE_DIM_TYP'))
        link  = output.linkify(df.Id)


        # #Debugger dla typu 'HS': wypisz wszystkie wartości parametrów
        # if typ == 'HS':
        #     output.insert_divider()
        #     print("=== DEBUG dla elementu ID {} ({}) ===".format(df.Id, link))
        #     for key in sorted(pv.keys()):
        #         val=pv[key]
        #         if val is None:
        #             print "{}: None".format (key)
        #         else:
        #             pv_mm = convert_internal_units(val, get_internal=False, units='mm')
        #             print("  {}: {}".format(key, pv_mm))
        #     output.insert_divider()

        if typ not in required_params:
            print("Nieznany typ {}: {}".format(typ,df.Id, link))
            continue

        try:
            area_int = calculate_area_internal(typ, pv)
            # z internalnych [ft^2] na m^2
            area_m2  = convert_internal_units(area_int, False ,'m2')
            val      = round(area_m2, 3)

            with Transaction(doc, __title__) as t:
                t.Start()
                if HC_A:
                    HC_A.Set(val*10)
                else:

                    print("Brak parametru HC_Area dla {}".format(link))
                t.Commit()

                total_df_count+= 1
                total_df_area_m2 += val

            print("{} → HC_Area = {} m²".format(link, val))
        except Exception as ex:
            print("Błąd {} dla {}: {}".format(typ, link, ex))

        # Podsumowanie
    output.insert_divider ()
    print ( u"Duct fittings count: {} szt".format ( total_df_count ) )
    print ( u"Duct fittings area: {} m²".format ( round ( total_df_area_m2, 3 ) ) )
    print ( u"Ducts round count: {} szt".format ( len ( round_ducts ) ) )
    print ( u"Ducts rectangular count: {} szt".format ( len ( rect_ducts ) ) )
    print ( u"Ducts length round: {} m".format ( round(total_len_round,3 )) )
    print ( u"Ducts length rectangular: {} m".format ( round(total_len_rect ,2) ))
    print ( u"Ducts area rectangular: {} m²".format ( round ( rect_duct_area, 2 ) ) )
    print ( u"Ducts area: {} m²".format ( round ( total_duct_area, 2 ) ) )
    output.insert_divider ()

    # Pomiar czasu zakończenia
    end_time = time.clock()
    exec_duration = end_time - start_time
    round_time = round(exec_duration, 3)
    print('-' * 50)
    print("Time of script: {} sec".format(round_time))

    output.close_others(all_open_outputs=True)

if __name__ == '__main__':
    main()

