# -*- coding: utf-8 -*-
__title__ = "VentCost"
__doc__ = """Data  = 08.2025
_____________________________________________________________________
Komentarz:
Przypisuje cenę jednostkową i koszt elementów instalacji wentylacji.
Okrągłe: Duct/Flex Duct (mb), Duct Fitting (szt., typ + średnica; redukcje typ + dwie średnice; kolana typ + średnica + HC_Kąt),
 Duct Accessory (typ + średnica).
 
Prostokątne: Duct (ilość z BuiltInParameter.Area [m²]) i Duct Fitting (ilość z HC_Area [m²]), cena wg kubełków max wymiaru (Size).
Foliowanie: opcjonalnie tylko dla prostokątnych (Duct, Duct Fitting); koszt pobierany z wpisu Category=Foliowanie w cenniku i dodawany jedynie po wyborze "Tak".

Jak korzystać:
- Wybierz plik cennika (CSV).
- Zaznacz czy doliczać foliowanie kształtek (koszt z Category=Foliowanie dodawany jedynie po wyborze "Tak").
- Uruchom. Skrypt przetworzy cały model i poda podsumowanie (bez raportu Excel).

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

from Autodesk.Revit.DB import *
from pyrevit import forms, script

from catalog import load_catalog, build_catalog_index
from parsers import get_round_diams, get_max_rect_dim
from processor import (
    PARAM_PRICE, PARAM_COST, PARAM_ANGLE, PARAM_AREA_CUSTOM,
    collect_elements, ensure_params_writable, get_category_name,
    get_size_string, get_length_m, get_angle_inst, get_typename,
    find_price_round_any, find_price_round_any_size, find_price_round_typename, find_price_rect_bucket
)

doc = __revit__.ActiveUIDocument.Document

def ensure_shared_params(doc):
    required = {PARAM_COST, PARAM_PRICE}
    existing = set()
    it = doc.ParameterBindings.ForwardIterator()
    while it.MoveNext():
        defn = it.Key
        if defn and defn.Name in required:
            existing.add(defn.Name)
    missing = required - existing
    if missing:
        forms.alert(u"Brak parametr\u00f3w: {0}. Dodaj je do projektu i spr\u00f3buj ponownie.".format(
            ", ".join(sorted(missing))), warn_icon=True)
        script.exit()

ensure_shared_params(doc)

def pick_catalog_file():
    return forms.pick_file(file_ext='csv', title='Wybierz plik cennika (CSV)', restore_dir=True, multi_file=False, unc_paths=False)

output = script.get_output()

catalog_path = pick_catalog_file()
if not catalog_path:
    forms.alert("Nie wybrano pliku cennika. Przerywam.", warn_icon=True)
    script.exit()

add_foil = forms.alert(
    "Czy doliczyć koszt foliowania dla Ducts i Duct Fittings?",
    options=["Tak", "Nie"],
    title="Foliowanie"
) == "Tak"

catalog_rows = load_catalog(catalog_path)
if not catalog_rows:
    forms.alert("Cennik pusty lub w złym formacie.", warn_icon=True)
    script.exit()
foil_price_pln_m2 = 0.0
if add_foil:
    for crow in catalog_rows:
        if (crow.Category == "Foliowanie" and crow.Shape == "rect" and
                crow.MatchKey == "ANY" and crow.Unit.lower() == "m2"):
            foil_price_pln_m2 = crow.UnitPrice
            break
    else:
        forms.alert("Brak pozycji foliowania w cenniku. Foliowanie wyłączone.", warn_icon=True)
        add_foil = False
catalog_idx = build_catalog_index(catalog_rows)

elements = collect_elements(doc)
processed = 0
no_match = []
missing_angle_eids = set()

tr = Transaction(doc, __title__)
tr.Start()

for el in elements:
    try:
        angle_val = get_angle_inst(el)
        if not ensure_params_writable(el):
            no_match.append([el.Id.IntegerValue, get_category_name(el) or '', get_typename(el), '', angle_val, 'Parametry tylko do odczytu'])
            continue
        cat_name = get_category_name(el)
        if not cat_name:
            no_match.append([el.Id.IntegerValue, '', get_typename(el), '', angle_val, 'Nieobsługiwana kategoria'])
            continue
        size_str = get_size_string(el)
        if not size_str:
            no_match.append([el.Id.IntegerValue, cat_name, get_typename(el), '', angle_val, 'Brak Size'])
            continue

        unit_price = cost = None
        typename = get_typename(el)

        if 'x' in size_str.lower() or u'×' in size_str:
            if cat_name in ('Duct', 'Duct Fitting'):
                if cat_name == 'Duct Fitting':
                    sym = getattr(el, 'Symbol', None)
                    fam = getattr(sym, 'FamilyName', '') if sym else ''
                    if fam == 'L_Flange_RV':
                        # output.print_md(u"[Rect Duct Fitting] Pominięto rodzinę L_Flange_RV dla ID:{0}".format(el.Id.IntegerValue))
                        continue
                max_dim = get_max_rect_dim(size_str)
                if max_dim is None:
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Nieczytelny Size (rect)'])
                    # output.print_md(u"[Rect {0}] Nieczytelny Size dla ID:{1} Type:{2} Size:{3}".format(cat_name, el.Id.IntegerValue, typename, size_str))
                    continue
                crow = find_price_rect_bucket(catalog_idx, cat_name, 'rect', max_dim)
                if not crow or crow.Unit.lower().replace(u'²','2') != 'm2':
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak ceny (rect m2)'])
                    # output.print_md(u"[Rect {0}] Brak ceny dla ID:{1} Type:{2} Size:{3} MaxDim:{4}".format(cat_name, el.Id.IntegerValue, typename, size_str, max_dim))
                    continue
                p_area = el.LookupParameter(PARAM_AREA_CUSTOM)
                if not p_area or p_area.StorageType != StorageType.Double or p_area.AsDouble() <= 0:
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak HC_Area'])
                    # output.print_md(u"[Rect {0}] Brak HC_Area dla ID:{1} Type:{2} Size:{3}".format(cat_name, el.Id.IntegerValue, typename, size_str))
                    continue
                area_m2 = float(p_area.AsDouble())
                unit_price = crow.UnitPrice
                cost = unit_price * area_m2
                if add_foil:
                    cost += area_m2 * foil_price_pln_m2
                # output.print_md(u"[Rect {0}] ID:{1} Type:{2} Size:{3} MaxDim:{4} Area:{5:.3f} UnitPrice:{6} Cost:{7}".format(cat_name, el.Id.IntegerValue, typename, size_str, max_dim, area_m2, unit_price, cost))
            elif cat_name == 'Duct Accessory':
                diams = get_round_diams(size_str)
                if not diams:
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak wymiarów'])
                    continue
                sym = getattr(el, 'Symbol', None)
                fam = getattr(sym, 'FamilyName', '') if sym else ''
                typ = getattr(sym, 'Name', '') if sym else ''
                # output.print_md(u"[Duct Accessory Rect] ID:{0} Family:'{1}' Type:'{2}' użyty TypeName:'{3}'".format(el.Id.IntegerValue, fam, typ, typename))
                crow = find_price_round_typename(catalog_idx, cat_name, typename, diams, None)
                if not crow or crow.Unit.lower() != 'szt':
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak ceny (accessory: typ+size)'])
                    # output.print_md(u"[Duct Accessory Rect] Brak ceny dla ID:{0} Type:{1} Size:{2} Diams:{3}".format(el.Id.IntegerValue, typename, size_str, diams))
                    continue
                unit_price = crow.UnitPrice
                cost = unit_price
                # output.print_md(u"[Duct Accessory Rect] ID:{0} Type:{1} Size:{2} Diams:{3} UnitPrice:{4}".format(el.Id.IntegerValue, typename, size_str, diams, unit_price))
            else:
                continue
        else:
            diams = get_round_diams(size_str)
            if not diams:
                no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak średnic'])
                continue
            if cat_name in ('Duct','Flex Duct'):
                if cat_name == 'Flex Duct':
                    crow = find_price_round_any_size(catalog_idx, diams[0])
                else:
                    crow = find_price_round_any(catalog_idx, cat_name, diams[0])
                if not crow or crow.Unit.lower() != 'm':
                    # output.print_md(u"[Round {0}] Brak ceny dla ID:{1} Type:{2} Dia:{3} Unit:{4}".format(cat_name, el.Id.IntegerValue, typename, diams[0], crow.Unit if crow else 'None'))
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak ceny round {}'.format(cat_name)])
                    continue
                unit_price = crow.UnitPrice
                length_m = get_length_m(el)
                cost = unit_price * length_m
            elif cat_name == 'Duct Fitting':
                if angle_val is None:
                    missing_angle_eids.add(el.Id.IntegerValue)
                crow = find_price_round_typename(catalog_idx, cat_name, typename, diams, angle_val)
                if not crow or crow.Unit.lower() != 'szt':
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak ceny (fitting: typ+średnica)'])
                    # output.print_md(u"[Duct Fitting] Brak ceny dla ID:{0} Type:{1} Size:{2} Diams:{3}".format(el.Id.IntegerValue, typename, size_str, diams))
                    continue
                unit_price = crow.UnitPrice
                cost = unit_price
                # output.print_md(u"[Duct Fitting] ID:{0} Type:{1} Size:{2} Diams:{3} UnitPrice:{4}".format(el.Id.IntegerValue, typename, size_str, diams, unit_price))
            elif cat_name == 'Duct Accessory':
                sym = getattr(el, 'Symbol', None)
                fam = getattr(sym, 'FamilyName', '') if sym else ''
                typ = getattr(sym, 'Name', '') if sym else ''
                # output.print_md(u"[Duct Accessory] ID:{0} Family:'{1}' Type:'{2}' użyty TypeName:'{3}'".format(el.Id.IntegerValue, fam, typ, typename))
                crow = find_price_round_typename(catalog_idx, cat_name, typename, diams, None)
                if not crow or crow.Unit.lower() != 'szt':
                    no_match.append([el.Id.IntegerValue, cat_name, typename, size_str, angle_val, 'Brak ceny (accessory: typ+średnica)'])
                    # output.print_md(u"[Duct Accessory] Brak ceny dla ID:{0} Type:{1} Size:{2} Diams:{3}".format(el.Id.IntegerValue, typename, size_str, diams))
                    continue
                unit_price = crow.UnitPrice
                cost = unit_price
                # output.print_md(u"[Duct Accessory] ID:{0} Type:{1} Dia:{2} UnitPrice:{3}".format(el.Id.IntegerValue, typename, diams, unit_price))
            else:
                continue

        if unit_price is not None and cost is not None:
            p_price = el.LookupParameter(PARAM_PRICE)
            p_cost  = el.LookupParameter(PARAM_COST)
            if p_price and not p_price.IsReadOnly:
                p_price.Set(float(unit_price))
            if p_cost and not p_cost.IsReadOnly:
                p_cost.Set(float(cost))
            processed += 1

    except Exception as ex:
        no_match.append([el.Id.IntegerValue if hasattr(el,'Id') else -1, cat_name if 'cat_name' in locals() else '', typename if 'typename' in locals() else '', size_str if 'size_str' in locals() else '', angle_val if 'angle_val' in locals() else None, 'ERR: {0}'.format(ex)])

tr.Commit()

summary = [
    "Przetworzono: {0}".format(processed),
    "Brak dopasowania: {0}".format(len(no_match))
]
if missing_angle_eids:
    summary.append("Uwaga: brak '{0}' dla {1} kształtek okrągłych.".format(PARAM_ANGLE, len(missing_angle_eids)))

if no_match:
    table_rows = []
    seen = set()
    for eid, cat, typ, size, angle, reason in no_match:
        key = (cat, typ, size, angle, reason)
        if key in seen:
            continue
        seen.add(key)
        try:
            link = output.linkify(ElementId(eid))
        except Exception:
            link = str(eid)
        angle_str = '' if angle is None else str(angle)
        table_rows.append([link, cat, typ, size, angle_str, reason])
    output.print_md("## Elementy nieprzetworzone")
    output.print_table(table_data=table_rows,
                       columns=["ID", "Kategoria", "Typ", "Rozmiar", "Kąt", "Powód"])

if missing_angle_eids:
    angle_rows = []
    for eid in sorted(missing_angle_eids):
        try:
            angle_rows.append([output.linkify(ElementId(eid))])
        except Exception:
            angle_rows.append([str(eid)])
    output.print_md("## Elementy bez parametru '{}'".format(PARAM_ANGLE))
    output.print_table(table_data=angle_rows, columns=["ID"])

forms.alert("\n".join(summary), title=__title__+" - Podsumowanie", warn_icon=False)