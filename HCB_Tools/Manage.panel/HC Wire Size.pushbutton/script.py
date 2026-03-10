# -*- coding: utf-8 -*-
from __future__ import unicode_literals

__title__ = "HC Wire"
__doc__ = """Date  = 03.2024
_____________________________________________________________________
Komentarz:
Automatyczne generowanie skróconego opisu przewodu na podstawie parametrów obwodów elektrycznych.

Jak korzystać:
- Uruchom w projekcie Revit zawierającym obwody o parametrze System Type = Power.
- Parametr docelowy (tekstowy) HC_WireSize musi być dodany do obwodów przed startem narzędzia.
- Po uruchomieniu wartość HC_WireSize jest nadpisywana skróconym opisem typu „5×6 mm²”.

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
import re
import time

from Autodesk.Revit.DB import (
    BuiltInCategory,
    FilteredElementCollector,
    StorageType,
    Transaction,
)
from Autodesk.Revit.DB.Electrical import ElectricalSystemType

from pyrevit import forms, script

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ CONFIG & VARIABLES
# ==================================================
uidoc = __revit__.ActiveUIDocument
doc = uidoc.Document
logger = script.get_logger()
output = script.get_output()

TARGET_PARAM_NAME = "HC_WireSize"
WIRE_SIZE_PARAM_NAME = "Wire Size"
POLES_PARAM_NAME = "NumberOfPoles"
NEUTRAL_PARAM_NAME = "NeutralConductorsNumber"
GROUND_PARAM_NAME = "GroundConductorsSize"
CIRCUIT_NUMBER_PARAM_NAME = "Circuit Number"

# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ FUNKCJE POMOCNICZE (MINIMUM)
# ==================================================

def get_int(param):
    """Zwraca wartość całkowitą z parametru Revit (None jeśli brak/błąd)."""
    if not param:
        return None
    try:
        if param.StorageType == StorageType.Integer:
            return param.AsInteger()
        elif param.StorageType == StorageType.Double:
            return int(round(param.AsDouble()))
    except Exception:
        pass
    return None


def get_string(param):
    """Zwraca tekst z parametru (AsString lub AsValueString)."""
    if not param:
        return None
    try:
        value = param.AsString()
        if not value:
            value = param.AsValueString()
        if value:
            return value.strip()
    except Exception:
        pass
    return None


def extract_wire_section_after_hash(raw_value):
    """Zwraca pierwszą wartość liczbową PO znaku '#' (np. '2#6,0 + 1#10' -> '6.0')."""
    if not raw_value:
        return None
    match = re.search(r"#\s*([0-9]+(?:[\.,][0-9]+)?)", raw_value)
    if not match:
        return None
    section = match.group(1)
    return section.replace(",", ".")


def format_wire_description(total_cores, section_value):
    """Formatuje wynikowy opis przewodu."""
    if total_cores is None or section_value is None:
        return None
    return u"{0}×{1} mm²".format(total_cores, section_value)


# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ LOGIKA GŁÓWNA
# ==================================================

start_time = time.time()

# Zbierz obwody
all_circuits = FilteredElementCollector(doc)\
    .OfCategory(BuiltInCategory.OST_ElectricalCircuit)\
    .WhereElementIsNotElementType()\
    .ToElements()

power_circuits = [
    circuit for circuit in all_circuits
    if hasattr(circuit, "SystemType") and circuit.SystemType == ElectricalSystemType.PowerCircuit
]

if not power_circuits:
    forms.alert(u"Brak obwodów elektrycznych typu Power w projekcie.", title=__title__, exitscript=True)

# Sprawdź, czy parametr docelowy istnieje
sample_param = power_circuits[0].LookupParameter(TARGET_PARAM_NAME)
if sample_param is None or sample_param.StorageType != StorageType.String:
    message = (
        u"Parametr \"{0}\" nie został znaleziony w projekcie lub nie jest typu tekstowego.\n"
        u"Dodaj parametr współdzielony do obwodów i uruchom skrypt ponownie."
    ).format(TARGET_PARAM_NAME)
    forms.alert(message, title=__title__, exitscript=True)

updated = 0
skipped = []

t = Transaction(doc, __title__)
t.Start()

try:
    for circuit in power_circuits:
        element_link = output.linkify(circuit.Id)

        target_param = circuit.LookupParameter(TARGET_PARAM_NAME)
        if target_param is None or target_param.StorageType != StorageType.String:
            skipped.append((circuit.Id, u"Brak parametru docelowego HC_WireSize."))
            continue

        # Bezpośrednie pobranie parametrów, które nas interesują
        poles_param = circuit.LookupParameter(POLES_PARAM_NAME)
        neutral_param = circuit.LookupParameter(NEUTRAL_PARAM_NAME)
        ground_param = circuit.LookupParameter(GROUND_PARAM_NAME)
        wire_param = circuit.LookupParameter(WIRE_SIZE_PARAM_NAME)
        circuit_number_param = circuit.LookupParameter(CIRCUIT_NUMBER_PARAM_NAME)

        poles = get_int(poles_param)
        neutral_count = get_int(neutral_param)
        ground_count = get_int(ground_param)
        wire_value_raw = get_string(wire_param)
        circuit_number = get_string(circuit_number_param) or u"(brak Circuit Number)"

        if poles is None:
            skipped.append((circuit.Id, u"Brak wartości NumberOfPoles."))
            continue

        # Suma: NumberOfPoles + NeutralConductors + GroundConductors
        total_cores = 0
        for val in (poles, neutral_count, ground_count):
            if val is not None:
                total_cores += val

        if total_cores <= 0:
            skipped.append((circuit.Id, u"Suma żył (NumberOfPoles + NeutralConductors + GroundConductors) ≤ 0."))
            continue

        # Średnica: pierwsza liczba po znaku '#'
        section_value = extract_wire_section_after_hash(wire_value_raw)
        if section_value is None:
            skipped.append((circuit.Id, u"Nie można odczytać przekroju (średnicy) z Wire Size po znaku '#'."))
            continue

        description = format_wire_description(total_cores, section_value)
        if not description:
            skipped.append((circuit.Id, u"Nie udało się złożyć opisu przewodu."))
            continue

        # Jeśli już jest ustawione na tę samą wartość – tylko log
        if target_param.AsString() == description:
            logger.info(
                u"Obwód {0} | CircuitNumber: {1} | HC_WireSize już ustawione na '{2}'".format(
                    element_link, circuit_number, description
                )
            )
            continue

        try:
            target_param.Set(description)
            updated += 1
            logger.info(
                u"Zaktualizowano obwód {0} | CircuitNumber: {1} | HC_WireSize = '{2}'".format(
                    element_link, circuit_number, description
                )
            )
        except Exception as err:
            skipped.append((circuit.Id, unicode(err)))
except Exception:
    t.RollBack()
    raise
else:
    t.Commit()

elapsed = time.time() - start_time
logger.info(u"Zaktualizowano parametry HC_WireSize dla obwodów: {0}".format(updated))
logger.info(u"Czas działania skryptu: {0:.2f} s".format(elapsed))

if skipped:
    for element_id, reason in skipped:
        element_link = output.linkify(element_id)
        logger.warning(u"Pominięto obwód {0}: {1}".format(element_link, reason))
else:
    logger.info(u"Wszystkie obwody przetworzono poprawnie.")
