# -*- coding: utf-8 -*-
__title__ = "LinkedRoomToElements"
__doc__ = u"""Data = 06.2025
_____________________________________________________________________
Komentarz:
Pobiera numer i nazwę pomieszczeń z wybranego podlinkowanego modelu i przypisuje je do elementów MEP/HVAC
na podstawie przecięcia geometrii.

Jak korzystać:
- Zaznacz w modelu elementy, które chcesz zaktualizować.
- Wybierz linkowany model z listy.
- Wskaż parametry, do których mają trafić numer i nazwa pomieszczenia.
- Po zakończeniu zobacz komunikat z podsumowaniem.

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

import clr
from pyrevit import forms

clr.AddReference('RevitAPI')
import Autodesk.Revit.DB as DB
from Autodesk.Revit.DB import (
    Transaction,
    FilteredElementCollector,
    RevitLinkInstance,
    BuiltInCategory,
    Options,
    Solid,
    BooleanOperationsUtils,
    BooleanOperationsType,
    BuiltInParameter,
    GeometryInstance,
    SolidUtils,
    StorageType,
)

clr.AddReference('RevitAPIUI')
from Autodesk.Revit.UI import TaskDialog

# ==================================================
# Ustawienia i zmienne globalne
# ==================================================
uidoc = __revit__.ActiveUIDocument
doc   = uidoc.Document

target_categories = [
    BuiltInCategory.OST_MechanicalEquipment,
    BuiltInCategory.OST_DuctCurves,
    BuiltInCategory.OST_FlexDuctCurves,
    BuiltInCategory.OST_DuctFitting,
    BuiltInCategory.OST_DuctAccessory,
    BuiltInCategory.OST_DuctTerminal,
    BuiltInCategory.OST_PipeCurves,
    BuiltInCategory.OST_FlexPipeCurves,
    BuiltInCategory.OST_PipeFitting,
    BuiltInCategory.OST_PipeAccessory,
    BuiltInCategory.OST_PlumbingEquipment,
    BuiltInCategory.OST_CommunicationDevices,
    BuiltInCategory.OST_PlumbingFixtures,
    BuiltInCategory.OST_PlumbingEquipment,
    BuiltInCategory.OST_ElectricalEquipment,
    BuiltInCategory.OST_ElectricalFixtures,
    BuiltInCategory.OST_PlumbingFixtures,
    BuiltInCategory.OST_LightingFixtures,
]

# ==================================================
# Funkcje pomocnicze
# ==================================================
def select_link_instance():
    links = FilteredElementCollector(doc).OfClass(RevitLinkInstance).ToElements()
    if not links:
        TaskDialog.Show(u"Błąd", u"Nie znaleziono podlinkowanego modelu.")
        return None

    link_map = {l.Name: l for l in links}
    sel = forms.SelectFromList.show(link_map.keys(), title=u"Wybierz linkowany model")
    return link_map.get(sel)


def get_rooms_geometry(link_inst):
    linked_doc = link_inst.GetLinkDocument()
    rooms = FilteredElementCollector(linked_doc)\
        .OfCategory(BuiltInCategory.OST_Rooms)\
        .WhereElementIsNotElementType()\
        .ToElements()
    transform = link_inst.GetTransform()
    results = []
    opts = Options()
    opts.ComputeReferences = True

    for room in rooms:
        geo_el = room.get_Geometry(opts)
        if not geo_el:
            continue
        for geoObj in geo_el:
            if isinstance(geoObj, GeometryInstance):
                inst_geom = geoObj.GetInstanceGeometry()
                for instObj in inst_geom:
                    if isinstance(instObj, Solid) and instObj.Volume > 0:
                        solid_host = SolidUtils.CreateTransformed(instObj, transform)
                        results.append((room, solid_host))
                        break
            elif isinstance(geoObj, Solid) and geoObj.Volume > 0:
                solid_host = SolidUtils.CreateTransformed(geoObj, transform)
                results.append((room, solid_host))
                break
    return results


def get_selected_target_elements():
    """Zwraca elementy z zaznaczenia ograniczone do obsługiwanych kategorii."""
    selected_ids = list(uidoc.Selection.GetElementIds())
    if not selected_ids:
        forms.alert(
            u"Nie zaznaczono żadnych elementów. Zaznacz elementy i uruchom skrypt ponownie.",
            title=u"Brak zaznaczenia",
        )
        return []

    allowed_cat_ids = set()
    for bic in target_categories:
        try:
            category = DB.Category.GetCategory(doc, bic)
        except Exception:
            category = None
        if category is None:
            try:
                category = doc.Settings.Categories.get_Item(bic)
            except Exception:
                category = None
        if category is not None:
            allowed_cat_ids.add(category.Id.IntegerValue)

    elements = []
    for el_id in selected_ids:
        elem = doc.GetElement(el_id)
        if elem is None:
            continue
        cat = elem.Category
        if cat and cat.Id.IntegerValue in allowed_cat_ids:
            elements.append(elem)

    if not elements:
        forms.alert(
            u"Żaden z zaznaczonych elementów nie należy do obsługiwanych kategorii.",
            title=u"Brak obsługiwanych elementów",
        )

    return elements


def get_common_text_parameters(elements):
    """Zwraca listę wspólnych edytowalnych parametrów tekstowych."""
    common_names = None
    for elem in elements:
        param_names = set()
        for param in elem.Parameters:
            try:
                definition = param.Definition
            except Exception:
                continue
            if not definition:
                continue
            try:
                if param.StorageType == StorageType.String and not param.IsReadOnly:
                    param_names.add(definition.Name)
            except Exception:
                continue
        if common_names is None:
            common_names = param_names
        else:
            common_names &= param_names
        if not common_names:
            break

    if not common_names:
        return []

    return sorted(common_names)


def select_parameter(title, options, exclude=None, allow_skip=False):
    choices = [o for o in options if o != exclude]
    skip_marker = None
    if allow_skip:
        skip_marker = u"--Pomiń aktualizację--"
        choices = [skip_marker] + choices

    selection = forms.SelectFromList.show(
        choices,
        title=title,
        multiselect=False,
        button_name=u"Wybierz",
    )

    if not selection:
        return None
    if allow_skip and selection == skip_marker:
        return None
    return selection


def element_room_intersection(elem, rooms_geo):
    opts = Options()
    geom = elem.get_Geometry(opts)
    if geom is None:
        return None
    for geoObj in geom:
        if isinstance(geoObj, Solid) and geoObj.Volume > 0:
            for room, room_solid in rooms_geo:
                inter = BooleanOperationsUtils.ExecuteBooleanOperation(
                    geoObj, room_solid, BooleanOperationsType.Intersect
                )
                if inter and inter.Volume > 0:
                    return room
    return None

# ==================================================
# Główna procedura
# ==================================================
def main():
    link_inst = select_link_instance()
    if not link_inst:
        return

    selected_elements = get_selected_target_elements()
    if not selected_elements:
        return

    common_params = get_common_text_parameters(selected_elements)
    if not common_params:
        forms.alert(
            u"Brak wspólnych, edytowalnych parametrów tekstowych dla zaznaczonych elementów.",
            title=u"Brak parametrów",
        )
        return

    number_param_name = select_parameter(
        title=u"Parametr na numer pomieszczenia",
        options=common_params,
    )
    if not number_param_name:
        forms.alert(u"Nie wybrano parametru na numer pomieszczenia.", title=u"Anulowano")
        return

    name_param_name = select_parameter(
        title=u"Parametr na nazwę pomieszczenia",
        options=common_params,
        exclude=number_param_name,
        allow_skip=True,
    )

    rooms_geo    = get_rooms_geometry(link_inst)
    elements     = selected_elements
    updated      = 0
    not_assigned = 0

    t = Transaction(doc, __title__)
    t.Start()
    for elem in elements:
        room = element_room_intersection(elem, rooms_geo)
        if room:
            num_param  = room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString() or u""
            name_param = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() or u""
            if number_param_name:
                p_num = elem.LookupParameter(number_param_name)
                if p_num and not p_num.IsReadOnly:
                    p_num.Set(num_param)
            if name_param_name:
                p_name = elem.LookupParameter(name_param_name)
                if p_name and not p_name.IsReadOnly:
                    p_name.Set(name_param)
            updated   += 1
        else:
            not_assigned += 1
    t.Commit()

    TaskDialog.Show(
        u"Podsumowanie",
        u"Zaktualizowano {0} elementów.\nNie przypisano {1} elementów.".format(updated, not_assigned)
    )

if __name__ == u"__main__":
    main()
