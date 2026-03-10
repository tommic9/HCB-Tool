# -*- coding: utf-8 -*-
__title__   = "PurgeAnnotations"
__author__  = "Tomasz Michałek"
__version__ = "1.5"
__doc__ = """
Usuń nieużywane style elementów 2D (Annotation) w projekcie:
1) Zbiera wszystkie typy Annotation 
2) Sprawdza, które nie mają żadnych instancji
3) Pokazuje listę nazw do wielokrotnego wyboru
4) Prosi o potwierdzenie
5) Usuwa zaznaczone typy, logując w konsoli
"""

from pyrevit import revit, forms
from Autodesk.Revit.DB import (
    FilteredElementCollector,
    Transaction,
    CategoryType,
    BuiltInParameter,
    ElementId,
)

# aktywny dokument Revit
doc = revit.doc

def get_display_name(t):
    """Próbuje zwrócić najbardziej czytelną nazwę typu."""
    # najpierw parametr ALL_MODEL_TYPE_NAME
    try:
        p = t.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)
        if p:
            s = p.AsString()
            if s:
                return s
    except:
        pass
    # potem właściwość Name
    try:
        return t.Name
    except:
        pass
    # potem rodzina, jeśli istnieje
    try:
        return t.Family.Name
    except:
        pass
    # ostatecznie Id
    return "Id:{}".format(t.Id.IntegerValue)

# 1️⃣ Zbierz wszystkie typy Annotation
all_types = FilteredElementCollector(doc) \
                .WhereElementIsElementType() \
                .ToElements()
annotation_types = [
    t for t in all_types
    if t.Category and t.Category.CategoryType == CategoryType.Annotation
]
print("Znaleziono typów 2D (Annotation): {}".format(len(annotation_types)))

# 2️⃣ Zbierz Id typów używanych przez instancje
used_type_ids = set()
for inst in FilteredElementCollector(doc).WhereElementIsNotElementType():
    tid = inst.GetTypeId()
    if tid != ElementId.InvalidElementId:
        used_type_ids.add(tid)

# 3️⃣ Wybierz typy bez instancji
unused = [t for t in annotation_types if t.Id not in used_type_ids]
print("Nieużywanych typów 2D: {}".format(len(unused)))

if not unused:
    forms.alert(
        "Brak nieużywanych stylów elementów 2D w projekcie.",
        title=__title__,
        exitscript=True
    )

# 4️⃣ Przygotuj słownik nazw -> typ (db)
dict_styles = {}
for t in unused:
    name = get_display_name(t)
    # zapewnij unikalność kluczy
    if name in dict_styles:
        name = "{} (Id:{})".format(name, t.Id.IntegerValue)
    dict_styles[name] = t

# 5️⃣ Lista nazw posortowana
choices = sorted(dict_styles.keys())

print("Dostępnych do usunięcia pozycji:", len(choices))
# 6️⃣ Dialog wielokrotnego wyboru
selected_names = forms.SelectFromList.show(
    choices,
    title       = __title__,
    button_name = "Usuń wybrane",
    multiselect = True
)
if not selected_names:
    print("Nie wybrano żadnych stylów do usunięcia.")
    forms.alert(
        "Nie wybrano żadnych stylów do usunięcia.",
        title=__title__,
        exitscript=True
    )

# Mapowanie z powrotem na obiekty
to_delete = [ dict_styles[n] for n in selected_names ]
print("Wybrano do usunięcia: {}".format(len(to_delete)))

# 7️⃣ Potwierdzenie
if not forms.alert(
    "Czy na pewno usunąć {} stylów 2D?".format(len(to_delete)),
    ok=True, cancel=True
):
    print("Anulowano usuwanie.")
    forms.alert("Anulowano usuwanie stylów 2D.", title=__title__, exitscript=True)

# 8️⃣ Usunięcie w transakcji
with Transaction(doc, "Purge Unused 2D Styles") as t:
    t.Start()
    for ttype in to_delete:
        try:
            doc.Delete(ttype.Id)
            print("✔️ Usunięto: {}".format(get_display_name(ttype)))
        except Exception as ex:
            print("✖️ Błąd usunięcia {}: {}".format(get_display_name(ttype), ex))
    t.Commit()

print("Operacja zakończona.")
