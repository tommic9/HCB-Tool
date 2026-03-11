using System;
using System.Collections.Generic;

namespace HCB.RevitAddin
{
    internal static class RibbonDefinitions
    {
        public const string TabName = "HCB Tools";

        public static IReadOnlyList<RibbonPanelDefinition> Create()
        {
            return
            [
                new RibbonPanelDefinition(
                    "Modify",
                    [
                        new RibbonPulldownDefinition(
                            "HcbFiltersPulldown",
                            "Filters",
                            typeof(Features.CopyFilters.CopyFiltersCommand),
                            "Resources\\Ribbon\\Filters",
                            "Narzedzia do pracy z filtrami widokow i szablonow.",
                            "Grupa narzedzi do kopiowania, porzadkowania i dalszej pracy z filtrami.",
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbCopyFiltersButton",
                                    "Copy Filters",
                                    typeof(Features.CopyFilters.CopyFiltersCommand),
                                    "Kopiuje wybrane filtry z jednego widoku lub szablonu do innych widokow i szablonow.",
                                    "Dodaje brakujace filtry do widokow docelowych, kopiuje nadpisania i obsluguje konflikty istniejacych filtrow."),
                                new RibbonPushButtonDefinition(
                                    "HcbViewFiltersBulkEditButton",
                                    "Bulk Edit",
                                    typeof(Features.ViewFiltersBulkEdit.ViewFiltersBulkEditCommand),
                                    "Masowo zmienia stan i widocznosc wspolnych filtrow na wielu widokach.",
                                    "Pozwala wybrac widoki, wspolne filtry oraz akcje wlaczania, wylaczania i ukrywania filtrow.")
                            ]),
                        new RibbonStackDefinition(
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbLevelsButton",
                                    "Levels",
                                    typeof(Features.Levels.LevelsCommand),
                                    "Dodaje lub aktualizuje rzedne poziomow w nazwach poziomow.",
                                    "Wstawia wysokosc poziomu w metrach w okraglych nawiasach i aktualizuje istniejace oznaczenia."),
                                new RibbonPulldownDefinition(
                                    "HcbRenamePulldown",
                                    "Rename",
                                    typeof(Features.RenameViews.RenameViewsCommand),
                                    null,
                                    "Narzedzia do zmiany nazw i numerow.",
                                    "Grupa narzedzi do zmiany nazw widokow, arkuszy i innych elementow.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbRenameViewsButton",
                                            "Rename Views",
                                            typeof(Features.RenameViews.RenameViewsCommand),
                                            "Zmienia nazwy wybranych widokow.",
                                            "Pozwala dodac prefiks, zamienic tekst i dodac sufiks do nazw widokow."),
                                        new RibbonPushButtonDefinition(
                                            "HcbRenameSheetsButton",
                                            "Rename Sheets",
                                            typeof(Features.RenameSheets.RenameSheetsCommand),
                                            "Zmienia nazwy i numery wybranych arkuszy.",
                                            "Pozwala osobno modyfikowac numer arkusza i nazwe arkusza.")
                                    ]),
                                new RibbonPushButtonDefinition(
                                    "HcbEstimateButton",
                                    "Estimate",
                                    typeof(Features.Estimate.EstimateCommand),
                                    "Wylicza ceny jednostkowe i koszty elementow wentylacji wedlug pliku cennika.",
                                    "Aktualizuje HC_Cena_Jednostkowa i HC_Koszt dla kanalow, ksztaltek, akcesoriow i flexow.")
                            ])
                    ]),
                new RibbonPanelDefinition(
                    "HVAC",
                    [
                        new RibbonPulldownDefinition(
                            "HcbSpacesPulldown",
                            "Spaces",
                            typeof(Features.SpaceParamLinked.SpaceParamLinkedCommand),
                            "Resources\\Ribbon\\Spaces",
                            "Narzedzia do pracy z pomieszczeniami i przestrzeniami.",
                            "Grupa narzedzi do odczytu i uzupelniania danych powiazanych z pomieszczeniami.",
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbSpaceParamLinkedButton",
                                    "Space Params",
                                    typeof(Features.SpaceParamLinked.SpaceParamLinkedCommand),
                                    "Uzupelnia parametry LIN_ROOM_* na podstawie pomieszczen z podlinkowanych modeli.",
                                    "Dla elementow Mechanical Equipment szuka pomieszczenia w linku i wpisuje jego numer oraz nazwe."),
                                new RibbonPushButtonDefinition(
                                    "HcbSpaceToElementButton",
                                    "Space To Element",
                                    typeof(Features.SpaceToElement.SpaceToElementCommand),
                                    "Uzupelnia LIN_ROOM_* na podstawie lokalnych przestrzeni MEP.",
                                    "Mapuje elementy MEP do przestrzeni z aktywnego widoku na podstawie polozenia geometrycznego.")
                            ])
                        ,
                        new RibbonStackDefinition(
                            [
                                new RibbonPulldownDefinition(
                                    "HcbSystemColorsPulldown",
                                    "System Colors",
                                    typeof(Features.ColorVentSystems.ColorVentSystemsCommand),
                                    "Resources\\Ribbon\\SystemColors",
                                    "Narzedzia odpowiedzialne za kolorystyke systemow w projekcie.",
                                    "Grupa narzedzi do nakladania kolorow i filtrow systemowych.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbColorVentSystemsButton",
                                            "Color Systems",
                                            typeof(Features.ColorVentSystems.ColorVentSystemsCommand),
                                            "Naklada filtry widoku i kolory dla systemow wentylacyjnych.",
                                            "Tworzy lub wykorzystuje istniejace filtry systemow i ustawia nadpisania grafiki w aktywnym widoku."),
                                        new RibbonPushButtonDefinition(
                                            "HcbColorUniqueSystemsButton",
                                            "Unique Colors",
                                            typeof(Features.ColorUniqueSystems.ColorUniqueSystemsCommand),
                                            "Naklada kolory dla wybranych systemow wentylacyjnych i rurowych.",
                                            "Pozwala wskazac unikalne systemy i przypisac im nadpisania graficzne w aktywnym widoku.")
                                    ]),
                                new RibbonPushButtonDefinition(
                                    "HcbFlowChangerButton",
                                    "Flow",
                                    typeof(Features.FlowChanger.FlowChangerCommand),
                                    "Losowo roznicuje przeplywy dla terminali w aktywnym widoku.",
                                    "Aktualizuje wskazany parametr na podstawie parametru Flow z niewielka losowa zmiana."),
                                new RibbonPulldownDefinition(
                                    "HcbHvacToolsPulldown",
                                    "HVAC Tools",
                                    typeof(Features.FittingsAngle.FittingsAngleCommand),
                                    null,
                                    "Dodatkowe narzedzia pomocnicze dla elementow HVAC i MEP.",
                                    "Grupa narzedzi do kopiowania katow, poziomow i oznaczen systemowych.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbFittingsAngleButton",
                                            "Fittings Angle",
                                            typeof(Features.FittingsAngle.FittingsAngleCommand),
                                            "Kopiuje i zaokragla katy ksztaltek do parametru HC_Kat.",
                                            "Dla prostokatnych ksztaltek kanalowych zaokragla do 1 stopnia, a dla pozostalych do 5 stopni."),
                                        new RibbonPushButtonDefinition(
                                            "HcbLevelFromHvacButton",
                                            "Level From HVAC",
                                            typeof(Features.LevelFromHVACElements.LevelFromHVACElementsCommand),
                                            "Kopiuje Level lub Reference Level do wybranego parametru instancyjnego.",
                                            "Dziala dla wskazanych elementow HVAC i obsluguje parametry String oraz ElementId."),
                                        new RibbonPushButtonDefinition(
                                            "HcbSystemAssignerButton",
                                            "System Assigner",
                                            typeof(Features.SystemAssigner.SystemAssignerCommand),
                                            "Propaguje wartosc HC_System z urzadzenia na elementy przypisanych systemow.",
                                            "Wybierasz urzadzenia Mechanical Equipment, a narzedzie nadpisuje HC_System dla elementow systemu."),
                                        new RibbonPushButtonDefinition(
                                            "HcbNumberingSystemElementsButton",
                                            "Numbering System",
                                            typeof(Features.NumberingSystemElements.NumberingSystemElementsCommand),
                                            "Numeruje kanaly i ksztaltki po polaczeniach MEP od wybranego urzadzenia.",
                                            "Nadaje LIN_POSITION_NUMBER_A na podstawie grup wymiarowych i kolejnosci przejscia po sieci."),
                                        new RibbonPushButtonDefinition(
                                            "HcbMassOfDuctsFittingsButton",
                                            "Mass",
                                            typeof(Features.MassOfDuctsFittings.MassOfDuctsFittingsCommand),
                                            "Oblicza HC_Masa dla kanalow, ksztaltek i akcesoriow wentylacyjnych.",
                                            "Wylicza mase z HC_Area lub danych srednicy i dlugosci dla obslugiwanych elementow.")
                                    ])
                            ]),
                        new RibbonPulldownDefinition(
                            "HcbAreaPulldown",
                            "Area",
                            typeof(Features.DuctFittingsArea.DuctFittingsAreaLinearCommand),
                            "Resources\\Ribbon\\Area",
                            "Narzedzia do obliczania powierzchni ksztaltek wentylacyjnych.",
                            "Grupa narzedzi do zapisu HC_Area dla roznych standardow danych.",
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbDuctFittingsAreaLinearButton",
                                    "Area LIN",
                                    typeof(Features.DuctFittingsArea.DuctFittingsAreaLinearCommand),
                                    "Oblicza HC_Area dla ksztaltek wentylacyjnych LINEAR.",
                                    "Wylicza powierzchnie wedlug zestawu parametrow LINEAR i zapisuje wynik do parametru HC_Area."),
                                new RibbonPushButtonDefinition(
                                    "HcbDuctFittingsAreaMagicadButton",
                                    "Area MC",
                                    typeof(Features.DuctFittingsArea.DuctFittingsAreaMagicadCommand),
                                    "Oblicza HC_Area dla ksztaltek wentylacyjnych MagiCAD.",
                                    "Wylicza powierzchnie wedlug zestawu parametrow MagiCAD i zapisuje wynik do parametru HC_Area.")
                            ])
                    ]),
                new RibbonPanelDefinition(
                    "Views",
                    [
                        new RibbonStackDefinition(
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbUnhideAllElementsButton",
                                    "Unhide All",
                                    typeof(Features.UnhideAllElements.UnhideAllElementsCommand),
                                    "Odslania wszystkie elementy w aktywnym widoku.",
                                    "Przywraca ukryte elementy w biezacym widoku."),
                                new RibbonPushButtonDefinition(
                                    "HcbViewsDuplicateButton",
                                    "Duplicate Views",
                                    typeof(Features.ViewsDuplicate.ViewsDuplicateCommand),
                                    "Duplikuje wiele widokow naraz.",
                                    "Pozwala wybrac widoki, liczbe kopii i tryb duplikacji."),
                                new RibbonPushButtonDefinition(
                                    "HcbTransferViewTemplatesButton",
                                    "Templates",
                                    typeof(Features.TransferViewTemplates.TransferViewTemplatesCommand),
                                    "Kopiuje wybrane view templates miedzy otwartymi projektami.",
                                    "Pozwala przeniesc szablony widokow i opcjonalnie nadpisac istniejace pozycje.")
                            ])
                    ]),
                new RibbonPanelDefinition(
                    "Manage",
                    [
                        new RibbonStackDefinition(
                            [
                                new RibbonPulldownDefinition(
                                    "HcbWithoutOpenPulldown",
                                    "WithoutOpen",
                                    typeof(Features.BatchFileScan.BatchFileScanCommand),
                                    null,
                                    "Narzedzia do pracy na plikach Revit bez otwierania ich w interfejsie uzytkownika.",
                                    "Grupa narzedzi do skanowania i batchowej obrobki plikow projektow i rodzin.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbBatchFileScanButton",
                                            "Scan Files",
                                            typeof(Features.BatchFileScan.BatchFileScanCommand),
                                            "Skanuje wybrane pliki .rvt i .rfa bez otwierania ich w UI Revita.",
                                            "Pokazuje podstawowe metadane plikow, wykrywa wersje, worksharing oraz kwalifikacje do dalszych operacji WithoutOpen."),
                                        new RibbonPushButtonDefinition(
                                            "HcbUnloadLinksButton",
                                            "Unload Links",
                                            typeof(Features.UnloadLinks.UnloadLinksCommand),
                                            "Odlinkowuje zewnetrzne referencje w lokalnych projektach .rvt bez otwierania ich w UI Revita.",
                                            "Wykorzystuje TransmissionData do ustawienia referencji jako niezaladowanych przy kolejnym otwarciu modelu.")
                                    ]),
                                new RibbonPulldownDefinition(
                                    "HcbParametersPulldown",
                                    "Parameters",
                                    typeof(Features.SharedParameters.SharedParametersCommand),
                                    "Resources\\Ribbon\\Parameters",
                                    "Narzedzia do pracy z parametrami projektu.",
                                    "Grupa narzedzi do weryfikacji i uzupelniania parametrow wspoldzielonych.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbSharedParametersButton",
                                            "Shared Params",
                                            typeof(Features.SharedParameters.SharedParametersCommand),
                                            "Sprawdza i wczytuje wymagane parametry wspoldzielone.",
                                            "Weryfikuje brakujace parametry HC_* i dodaje je z aktualnie ustawionego pliku Shared Parameters.")
                                    ]),
                                new RibbonPushButtonDefinition(
                                    "HcbPurgeAnnotationsButton",
                                    "Purge Anno",
                                    typeof(Features.PurgeAnnotations.PurgeAnnotationsCommand),
                                    "Usuwa nieuzywane style adnotacji.",
                                    "Wyszukuje typy kategorii Annotation bez instancji i pozwala je usunac."),
                                new RibbonPushButtonDefinition(
                                    "HcbHCWireSizeButton",
                                    "HC Wire",
                                    typeof(Features.HCWireSize.HCWireSizeCommand),
                                    "Generuje skrocony opis przewodu dla obwodow elektrycznych typu Power.",
                                    "Nadpisuje parametr HC_WireSize na podstawie liczby zyl i przekroju odczytanego z Wire Size.")
                            ]),
                        new RibbonPushButtonDefinition(
                            "HcbRenameMaterialsButton",
                            "Materials",
                            typeof(Features.RenameMaterials.RenameMaterialsCommand),
                            "Zmienia nazwy wybranych materialow.",
                            "Pozwala wskazac materialy i zastosowac wspolne reguly prefiksu, zamiany tekstu i sufiksu.")
                    ])
            ];
        }
    }

    internal sealed record RibbonPanelDefinition(
        string Name,
        IReadOnlyList<RibbonItemDefinition> Items);

    internal abstract record RibbonItemDefinition;

    internal abstract record RibbonStackItemDefinition : RibbonItemDefinition;

    internal sealed record RibbonPushButtonDefinition(
        string Name,
        string Text,
        Type CommandType,
        string ToolTip,
        string LongDescription) : RibbonStackItemDefinition;

    internal sealed record RibbonPulldownDefinition(
        string Name,
        string Text,
        Type IconCommandType,
        string? IconResourceDirectory,
        string ToolTip,
        string LongDescription,
        IReadOnlyList<RibbonPushButtonDefinition> Buttons) : RibbonStackItemDefinition;

    internal sealed record RibbonStackDefinition(
        IReadOnlyList<RibbonStackItemDefinition> Items) : RibbonItemDefinition;
}


