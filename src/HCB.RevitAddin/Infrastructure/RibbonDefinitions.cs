using System;
using System.Collections.Generic;

namespace HCB.RevitAddin
{
    internal static class RibbonDefinitions
    {
        public const string TabName = "HCB Tools";
        private static readonly IReadOnlyList<RibbonPanelDefinition> CachedDefinitions = CreateDefinitions();

        public static IReadOnlyList<RibbonPanelDefinition> Create()
        {
            return CachedDefinitions;
        }

        private static IReadOnlyList<RibbonPanelDefinition> CreateDefinitions()
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
                            "Co zawiera: narzedzia do kopiowania i masowej edycji filtrow widokow. Jak uzyc: wybierz polecenie dla kopiowania filtrow albo dla zbiorczej zmiany ich stanu na wielu widokach. Zakres: widoki i view templates.",
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbCopyFiltersButton",
                                    "Copy Filters",
                                    typeof(Features.CopyFilters.CopyFiltersCommand),
                                    "Kopiuje wybrane filtry z jednego widoku lub szablonu do innych widokow i szablonow.",
                                    "Co robi: kopiuje filtry z widoku lub view template do innych widokow i szablonow. Jak uzyc: 1. Wskaz zrodlo. 2. Wybierz cele. 3. Potwierdz kopiowanie. Zakres: przenosi brakujace filtry, nadpisania i obsluguje konflikty."),
                                new RibbonPushButtonDefinition(
                                    "HcbViewFiltersBulkEditButton",
                                    "Bulk Edit",
                                    typeof(Features.ViewFiltersBulkEdit.ViewFiltersBulkEditCommand),
                                    "Masowo zmienia stan i widocznosc wspolnych filtrow na wielu widokach.",
                                    "Co robi: masowo zmienia stan i widocznosc wspolnych filtrow na wielu widokach. Jak uzyc: 1. Wybierz widoki. 2. Zaznacz wspolne filtry. 3. Ustaw akcje, np. ON, OFF albo ukrycie. Zakres: szybkie porzadkowanie wielu widokow jednoczesnie.")
                            ]),
                        new RibbonStackDefinition(
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbLevelsButton",
                                    "Levels",
                                    typeof(Features.Levels.LevelsCommand),
                                    "Dodaje lub aktualizuje rzedne poziomow w nazwach poziomow.",
                                    "Co robi: dodaje albo aktualizuje rzedne poziomow w nazwach poziomow. Jak uzyc: uruchom polecenie w projekcie z poziomami. Zakres: dopisuje wysokosc w metrach w okraglym nawiasie i aktualizuje istniejace oznaczenia."),
                                new RibbonPulldownDefinition(
                                    "HcbRenamePulldown",
                                    "Rename",
                                    typeof(Features.RenameViews.RenameViewsCommand),
                                    null,
                                    "Narzedzia do zmiany nazw.",
                                    "Co zawiera: narzedzia do seryjnej zmiany nazw. Jak uzyc: wybierz odpowiednie polecenie, ustaw reguly prefiksu, zamiany tekstu albo sufiksu i sprawdz podglad. Zakres: widoki i arkusze.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbRenameViewsButton",
                                            "Rename Views",
                                            typeof(Features.RenameViews.RenameViewsCommand),
                                            "Zmienia nazwy wybranych widokow.",
                                            "Co robi: seryjnie zmienia nazwy widokow. Jak uzyc: 1. Wybierz widoki. 2. Ustaw prefiks, zamiane tekstu i lub sufiks. 3. Zatwierdz podglad. Zakres: tylko wybrane widoki."),
                                        new RibbonPushButtonDefinition(
                                            "HcbRenameSheetsButton",
                                            "Rename Sheets",
                                            typeof(Features.RenameSheets.RenameSheetsCommand),
                                            "Zmienia nazwy i numery wybranych arkuszy.",
                                            "Co robi: seryjnie zmienia nazwy i numery arkuszy. Jak uzyc: 1. Wybierz arkusze. 2. Ustaw osobno zasady dla numeru i nazwy. 3. Zatwierdz podglad. Zakres: tylko wybrane arkusze.")
                                    ]),
                                new RibbonPulldownDefinition(
                                    "HcbNumberingPulldown",
                                    "Numbering",
                                    typeof(Features.ManualNumbering.ManualNumberingCommand),
                                    "Resources\\Ribbon\\Numbering\\Pulldown",
                                    "Narzedzia do numeracji elementow.",
                                    "Co zawiera: narzedzia do recznej i automatycznej numeracji elementow. Jak uzyc: wybierz odpowiednie polecenie, ustaw parametr docelowy i numer startowy. Zakres: jesli przed uruchomieniem masz zaznaczenie, narzedzia numeracji pracuja tylko na nim; bez zaznaczenia zbieraja elementy z projektu wedlug obslugiwanych kategorii.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbManualNumberingButton",
                                            "Manual Num",
                                            typeof(Features.ManualNumbering.ManualNumberingCommand),
                                            "Numeruje recznie wskazane elementy w kolejnosci klikanej przez uzytkownika.",
                                            "Co robi: numeruje elementy recznie w kolejnosci klikanej przez uzytkownika. Jak uzyc: 1. Zaznacz elementy. 2. Wybierz parametr docelowy i format numeru. 3. Klikaj elementy w zadanej kolejnosci. Zakres: pelna kontrola kolejnosci numeracji.",
                                            null,
                                            "Resources\\Ribbon\\Numbering\\Manual"),
                                        new RibbonPushButtonDefinition(
                                            "HcbDuctFittingNumberingButton",
                                            "Duct&Fit Num",
                                            typeof(Features.DuctFittingNumbering.DuctFittingNumberingCommand),
                                            "Numeruje kanaly i ksztaltki wedlug kluczy grupujacych i System Abbreviation.",
                                            "Co robi: numeruje kanaly i ksztaltki kanalowe wedlug kluczy grupujacych i System Abbreviation. Jak uzyc: 1. Wybierz parametr docelowy. 2. Ustaw numer startowy i ewentualny parametr dlugosci. 3. Zatwierdz numeracje. Zakres: przy zaznaczeniu dziala tylko na zaznaczonych ducts i fittings; bez zaznaczenia szuka ich w modelu. Uwagi: grupy z roznymi System Abbreviation dostaja numer bez prefiksu systemowego.",
                                            null,
                                            "Resources\\Ribbon\\Numbering\\DuctAndFit"),
                                        new RibbonPushButtonDefinition(
                                            "HcbDuctNumberingButton",
                                            "Duct Num",
                                            typeof(Features.DuctFittingNumbering.DuctNumberingCommand),
                                            "Numeruje tylko kanaly wedlug kluczy grupujacych i System Abbreviation.",
                                            "Co robi: numeruje tylko ducts. Jak uzyc: 1. Wybierz parametr docelowy. 2. Ustaw numer startowy i sposob grupowania. 3. Zatwierdz numeracje. Zakres: przy zaznaczeniu dziala tylko na zaznaczonych ducts; bez zaznaczenia zbiera wszystkie obslugiwane ducts z projektu. Uwagi: grupy z roznymi System Abbreviation dostaja numer bez prefiksu systemu.",
                                            null,
                                            "Resources\\Ribbon\\Numbering\\Duct"),
                                        new RibbonPushButtonDefinition(
                                            "HcbDuctFittingsOnlyNumberingButton",
                                            "DuctFit Num",
                                            typeof(Features.DuctFittingNumbering.DuctFittingsNumberingCommand),
                                            "Numeruje tylko ksztaltki kanalowe wedlug wymiarow LIN_VE i System Abbreviation.",
                                            "Co robi: numeruje tylko duct fittings. Jak uzyc: 1. Wybierz parametr docelowy. 2. Ustaw numer startowy. 3. Zatwierdz numeracje. Zakres: przy zaznaczeniu dziala tylko na zaznaczonych duct fittings; bez zaznaczenia zbiera wszystkie obslugiwane ksztaltki z projektu. Uwagi: grupowanie uwzglednia wymiary LIN_VE, kat i System Abbreviation, a grupy mieszane systemowo dostaja numer bez prefiksu systemu.",
                                            null,
                                            "Resources\\Ribbon\\Numbering\\DuctFitting"),
                                        new RibbonPushButtonDefinition(
                                            "HcbAccessoryTerminalNumberingButton",
                                            "MEP Item Numbering",
                                            typeof(Features.AccessoryTerminalNumbering.AccessoryTerminalNumberingCommand),
                                            "Numeruje elementy MEP wedlug systemu, kategorii i cech grupujacych.",
                                            "Co robi: numeruje Duct Accessory, Pipe Accessory i Air Terminal. Jak uzyc: 1. Wybierz parametr docelowy. 2. Ustaw prefiksy i numer startowy. 3. Zatwierdz numeracje. Zakres: przy zaznaczeniu dziala tylko na zaznaczonych elementach tych kategorii; bez zaznaczenia zbiera je z projektu. Uwagi: grupuje po systemie, kategorii i cechach typu oraz pomija typy FabricAir.",
                                            null,
                                            "Resources\\Ribbon\\Numbering\\MepItems")
                                    ])
                            ]),
                        new RibbonPushButtonDefinition(
                            "HcbEstimateButton",
                            "Estimate",
                            typeof(Features.Estimate.EstimateCommand),
                            "Wylicza ceny jednostkowe i koszty elementow wentylacji wedlug pliku cennika.",
                            "Co robi: wylicza ceny jednostkowe i koszty elementow wentylacji. Jak uzyc: 1. Wskaz plik cennika albo uzyj domyslnej konfiguracji. 2. Uruchom przeliczenie. 3. Sprawdz raport dopasowan. Zakres: zapisuje HC_Cena_Jednostkowa oraz HC_Koszt dla obslugiwanych elementow wentylacji.")
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
                            "Co zawiera: narzedzia do uzupelniania danych pomieszczen i przestrzeni. Jak uzyc: wybierz polecenie zalezne od zrodla danych, czyli rooms z linku albo spaces z modelu lokalnego. Zakres: parametry LIN_ROOM_*.",
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbSpaceParamLinkedButton",
                                    "Space Params",
                                    typeof(Features.SpaceParamLinked.SpaceParamLinkedCommand),
                                    "Uzupelnia parametry LIN_ROOM_* na podstawie pomieszczen z podlinkowanych modeli.",
                                    "Co robi: uzupelnia LIN_ROOM_* na podstawie rooms z podlinkowanego modelu. Jak uzyc: 1. Uruchom narzedzie w projekcie z odpowiednim linkiem. 2. Wskaz albo potwierdz link. 3. Zapisz wynik. Zakres: glownie Mechanical Equipment."),
                                new RibbonPushButtonDefinition(
                                    "HcbSpaceToElementButton",
                                    "Space To Element",
                                    typeof(Features.SpaceToElement.SpaceToElementCommand),
                                    "Uzupelnia LIN_ROOM_* na podstawie wszystkich lokalnych przestrzeni MEP w modelu.",
                                    "Co robi: uzupelnia LIN_ROOM_* na podstawie wszystkich lokalnych przestrzeni MEP w modelu. Jak uzyc: 1. Otworz odpowiedni widok. 2. Uruchom narzedzie. 3. Zapisz wynik. Zakres: wszystkie spaces w modelu oraz elementy z obslugiwanych kategorii. Uwagi: to alternatywa dla pracy na roomach z linku.")
                            ])
                        ,
                        new RibbonStackDefinition(
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbColorUniqueSystemsButton",
                                    "Unique Colors",
                                    typeof(Features.ColorUniqueSystems.ColorUniqueSystemsCommand),
                                    "Naklada kolory dla wybranych systemow wentylacyjnych i rurowych z konfiguracji CSV.",
                                    "Co robi: naklada kolory na wybrane systemy wentylacyjne i rurowe. Jak uzyc: 1. Uruchom narzedzie w aktywnym widoku. 2. Wybierz systemy z pliku CSV. 3. Zatwierdz nadpisania. Zakres: tylko aktywny widok."),
                                new RibbonPushButtonDefinition(
                                    "HcbFlowChangerButton",
                                    "Flow",
                                    typeof(Features.FlowChanger.FlowChangerCommand),
                                    "Losowo roznicuje przeplywy dla terminali w aktywnym widoku.",
                                    "Co robi: losowo roznicuje przeplywy dla terminali. Jak uzyc: 1. Uruchom narzedzie w aktywnym widoku. 2. Wybierz parametr docelowy i zakres zmiany. 3. Zatwierdz. Zakres: szybkie testowanie wariantow przeplywu na terminalach."),
                                new RibbonPulldownDefinition(
                                    "HcbHvacToolsPulldown",
                                    "HVAC Tools",
                                    typeof(Features.FittingsAngle.FittingsAngleCommand),
                                    null,
                                    "Dodatkowe narzedzia pomocnicze dla elementow HVAC i MEP.",
                                    "Co zawiera: narzedzia pomocnicze dla HVAC i MEP. Jak uzyc: wybierz polecenie zalezne od zadania, np. kat, poziom, system albo masa. Zakres: operacje na wskazanych elementach albo obslugiwanych kategoriach.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbFittingsAngleButton",
                                            "Fittings Angle",
                                            typeof(Features.FittingsAngle.FittingsAngleCommand),
                                            "Kopiuje i zaokragla katy ksztaltek do parametru HC_Kat.",
                                            "Co robi: kopiuje i zaokragla katy ksztaltek do HC_Kat. Jak uzyc: 1. Zaznacz ksztaltki albo pozwol narzedziu pobrac obslugiwane elementy. 2. Uruchom polecenie. 3. Zapisz wynik. Zakres: prostokatne ksztaltki kanalowe sa zaokraglane do 1 stopnia, pozostale do 5 stopni."),
                                        new RibbonPushButtonDefinition(
                                            "HcbLevelFromHvacButton",
                                            "Level From HVAC",
                                            typeof(Features.LevelFromHVACElements.LevelFromHVACElementsCommand),
                                            "Kopiuje Level lub Reference Level do wybranego parametru instancyjnego.",
                                            "Co robi: kopiuje Level albo Reference Level do wybranego parametru. Jak uzyc: 1. Zaznacz elementy HVAC. 2. Wybierz parametr docelowy. 3. Zatwierdz. Zakres: tylko zaznaczenie; obsluguje parametry String i ElementId."),
                                        new RibbonPushButtonDefinition(
                                            "HcbSystemAssignerButton",
                                            "System Assigner",
                                            typeof(Features.SystemAssigner.SystemAssignerCommand),
                                            "Propaguje wartosc HC_System z urzadzenia na elementy przypisanych systemow.",
                                            "Co robi: propaguje wartosc parametru z urzadzenia na elementy przypisanych systemow. Jak uzyc: 1. Zaznacz Mechanical Equipment. 2. Wybierz parametr do propagacji. 3. Zatwierdz. Zakres: dobrze polaczone systemy przypisane do wskazanych urzadzen."),
                                        new RibbonPushButtonDefinition(
                                            "HcbMassOfDuctsFittingsButton",
                                            "Mass",
                                            typeof(Features.MassOfDuctsFittings.MassOfDuctsFittingsCommand),
                                            "Oblicza HC_Masa dla kanalow, ksztaltek i akcesoriow wentylacyjnych.",
                                            "Co robi: oblicza HC_Masa dla kanalow, ksztaltek i akcesoriow wentylacyjnych. Jak uzyc: 1. Wybierz parametr docelowy, jesli jest wymagany. 2. Uruchom obliczenie. 3. Zapisz wynik. Zakres: przy zaznaczeniu dziala tylko na nim; bez zaznaczenia zbiera obslugiwane elementy wentylacyjne z projektu.")
                                    ])
                            ]),
                        new RibbonPushButtonDefinition(
                            "HcbSplitterButton",
                            "Splitter",
                            typeof(Features.Splitter.SplitterCommand),
                            "Dzieli proste ducts i pipes na odcinki produkcyjne z lacznikami.",
                            "Co robi: dzieli proste ducts i pipes na odcinki produkcyjne z lacznikami. Jak uzyc: 1. Wpisz dlugosc produkcyjna i wybierz ducts, pipes albo oba typy. 2. Klikaj elementy blizej strony startowej. 3. ESC konczy. Zakres: dzieli od wskazanej strony i zostawia reszte na przeciwnym koncu. Uwagi: jesli klikniesz juz pocity prosty ciag, narzedzie przebuduje go od najblizszej granicy fittingu lub accessory i przeliczy rozstaw od nowa."),

                        new RibbonPulldownDefinition(
                            "HcbAreaPulldown",
                            "Area",
                            typeof(Features.DuctFittingsArea.DuctFittingsAreaLinearCommand),
                            "Resources\\Ribbon\\Area",
                            "Narzedzia do obliczania powierzchni ksztaltek wentylacyjnych.",
                            "Co zawiera: narzedzia do obliczania powierzchni ksztaltek wentylacyjnych. Jak uzyc: wybierz wariant zgodny ze standardem danych rodziny, np. LINEAR albo MagiCAD. Zakres: zapis wyniku do HC_Area.",
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbDuctFittingsAreaLinearButton",
                                    "Area LIN",
                                    typeof(Features.DuctFittingsArea.DuctFittingsAreaLinearCommand),
                                    "Oblicza HC_Area dla ksztaltek wentylacyjnych LINEAR.",
                                    "Co robi: oblicza HC_Area dla ksztaltek wentylacyjnych LINEAR. Jak uzyc: 1. Uruchom narzedzie dla rodzin przygotowanych pod LINEAR. 2. Wybierz zakres albo ustawienia. 3. Zapisz wynik. Zakres: raport i zapis do HC_Area."),
                                new RibbonPushButtonDefinition(
                                    "HcbDuctFittingsAreaMagicadButton",
                                    "Area MC",
                                    typeof(Features.DuctFittingsArea.DuctFittingsAreaMagicadCommand),
                                    "Oblicza HC_Area dla ksztaltek wentylacyjnych MagiCAD.",
                                    "Co robi: oblicza HC_Area dla ksztaltek wentylacyjnych MagiCAD. Jak uzyc: 1. Uruchom narzedzie dla rodzin przygotowanych pod MagiCAD. 2. Wybierz zakres albo ustawienia. 3. Zapisz wynik. Zakres: raport i zapis do HC_Area.")
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
                                    "Co robi: odslania wszystkie elementy w aktywnym widoku. Jak uzyc: uruchom polecenie w biezacym widoku. Zakres: tylko elementy ukryte poleceniem Hide in View w aktywnym widoku."),
                                new RibbonPushButtonDefinition(
                                    "HcbViewsDuplicateButton",
                                    "Duplicate Views",
                                    typeof(Features.ViewsDuplicate.ViewsDuplicateCommand),
                                    "Duplikuje wiele widokow naraz.",
                                    "Co robi: duplikuje wiele widokow naraz. Jak uzyc: 1. Wybierz widoki. 2. Ustaw liczbe kopii i tryb duplikacji. 3. Zatwierdz. Zakres: tworzy serie kopii wybranych widokow."),
                                new RibbonPulldownDefinition(
                                    "HcbTemplatesPulldown",
                                    "Templates",
                                    typeof(Features.TransferViewTemplates.TransferViewTemplatesCommand),
                                    null,
                                    "Udostepnia narzedzia do kopiowania szablonow widokow i zestawien miedzy otwartymi projektami.",
                                    "Co robi: udostepnia osobne polecenia do kopiowania szablonow widokow i szablonow zestawien miedzy otwartymi projektami. Jak uzyc: wybierz odpowiednie polecenie z listy, wskaz projekt zrodlowy i docelowy, a potem zaznacz szablony do transferu. Zakres: synchronizacja standardu widokow i zestawien pomiedzy otwartymi modelami.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbTransferViewTemplatesButton",
                                            "View Templates",
                                            typeof(Features.TransferViewTemplates.TransferViewTemplatesCommand),
                                            "Kopiuje wybrane szablony widokow miedzy otwartymi projektami.",
                                            "Co robi: kopiuje szablony widokow miedzy otwartymi projektami. Jak uzyc: 1. Otworz projekt zrodlowy i docelowy. 2. Wybierz view templates do skopiowania. 3. Zdecyduj o nadpisaniu istniejacych pozycji. Zakres: szybka synchronizacja standardu widokow bez szablonow zestawien."),
                                        new RibbonPushButtonDefinition(
                                            "HcbTransferScheduleTemplatesButton",
                                            "Schedule Templates",
                                            typeof(Features.TransferViewTemplates.TransferScheduleTemplatesCommand),
                                            "Kopiuje wybrane szablony zestawien miedzy otwartymi projektami.",
                                            "Co robi: kopiuje szablony zestawien miedzy otwartymi projektami. Jak uzyc: 1. Otworz projekt zrodlowy i docelowy. 2. Wybierz szablony zestawien do skopiowania. 3. Zdecyduj o nadpisaniu istniejacych pozycji. Zakres: szybka synchronizacja standardu zestawien pomiedzy otwartymi modelami.")
                                    ])
                            ]),
                        new RibbonPushButtonDefinition(
                            "HcbViewFiltersLegendButton",
                            "Filters Legend",
                            typeof(Features.ViewFiltersLegend.ViewFiltersLegendCommand),
                            "Tworzy legendy dla filtrow przypisanych do wybranych widokow i szablonow.",
                            "Co robi: tworzy legende filtrow dla wybranych widokow i szablonow. Jak uzyc: 1. Wybierz widoki albo view templates. 2. Uruchom polecenie. 3. Utworzona zostanie legenda z probkami i opisami filtrow. Zakres: generuje nowy widok legendy."),
                        new RibbonPushButtonDefinition(
                            "HcbCadLinkGraphicsCopyButton",
                            "CAD Link VG",
                            typeof(Features.CadLinkGraphicsCopy.CadLinkGraphicsCopyCommand),
                            "Kopiuje ustawienia widocznosci i nadpisan warstw z jednego linku CAD do drugiego w aktywnym widoku.",
                            "Co robi: kopiuje ustawienia widocznosci i nadpisan warstw z jednego linku CAD do drugiego. Jak uzyc: 1. Uruchom narzedzie w aktywnym widoku. 2. Wybierz link zrodlowy i docelowy. 3. Zatwierdz kopiowanie. Zakres: aktywny widok; dopasowanie po nazwach subkategorii.")
                    ]),
                new RibbonPanelDefinition(
                    "WithoutOpen",
                    [
                        new RibbonPulldownDefinition(
                            "HcbWithoutOpenPulldown",
                            "WithoutOpen",
                            typeof(Features.BatchFileScan.BatchFileScanCommand),
                            null,
                            "Narzedzia do pracy na plikach Revit bez otwierania ich w interfejsie uzytkownika.",
                            "Co zawiera: narzedzia WithoutOpen do pracy na plikach bez otwierania ich w interfejsie Revita. Jak uzyc: wskaz pliki albo folder, uruchom analize albo modyfikacje i zapisz raport. Zakres: projekty i rodziny poza UI Revita.",
                            [
                                new RibbonPushButtonDefinition(
                                    "HcbBatchFileScanButton",
                                    "Scan Files",
                                    typeof(Features.BatchFileScan.BatchFileScanCommand),
                                    "Skanuje wybrane pliki .rvt i .rfa bez otwierania ich w UI Revita.",
                                    "Co robi: skanuje pliki .rvt i .rfa bez otwierania ich w UI Revita. Jak uzyc: 1. Wybierz pliki albo folder. 2. Uruchom skanowanie. 3. Sprawdz raport. Zakres: wersja Revita, typ pliku, worksharing i przydatnosc do dalszych operacji batchowych.",
                                    typeof(AlwaysAvailableCommandAvailability)),
                                new RibbonPushButtonDefinition(
                                    "HcbUnloadLinksButton",
                                    "Unload Links",
                                    typeof(Features.UnloadLinks.UnloadLinksCommand),
                                    "Odlinkowuje zewnetrzne referencje w lokalnych projektach .rvt bez otwierania ich w UI Revita.",
                                    "Co robi: ustawia linki zewnetrzne jako niezaladowane przy kolejnym otwarciu modelu. Jak uzyc: 1. Wybierz lokalne pliki .rvt. 2. Uruchom polecenie. 3. Zapisz wynik. Zakres: wykorzystuje TransmissionData bez otwierania projektu w UI.",
                                    typeof(AlwaysAvailableCommandAvailability)),
                                new RibbonPushButtonDefinition(
                                    "HcbFamilyParameterReportButton",
                                    "Family Report",
                                    typeof(Features.FamilyParameterReport.FamilyParameterReportCommand),
                                    "Odczytuje parametry rodzin .rfa przez otwarcie ich w tle, bez pokazywania w UI Revita.",
                                    "Co robi: odczytuje parametry rodzin .rfa i tworzy raport CSV. Jak uzyc: 1. Wybierz rodziny. 2. Uruchom raport. 3. Odbierz CSV. Zakres: szybka analiza rodzin bez recznego otwierania kazdej z nich.",
                                    typeof(AlwaysAvailableCommandAvailability)),
                                new RibbonPushButtonDefinition(
                                    "HcbUpgradeAndCopyButton",
                                    "Upgrade Copy",
                                    typeof(Features.UpgradeAndCopy.UpgradeAndCopyCommand),
                                    "Aktualizuje lokalne pliki .rvt i .rfa do wersji uruchomionego Revita i zapisuje kopie do wskazanego folderu.",
                                    "Co robi: aktualizuje pliki .rvt i .rfa do wersji uruchomionego Revita i zapisuje kopie. Jak uzyc: 1. Wybierz pliki i folder docelowy. 2. Uruchom upgrade. 3. Sprawdz wynik. Zakres: starsze pliki sa aktualizowane, a juz aktualne kopiowane.",
                                    typeof(AlwaysAvailableCommandAvailability)),
                                new RibbonPushButtonDefinition(
                                    "HcbBatchAddSharedFamilyParametersButton",
                                    "Add Shared",
                                    typeof(Features.BatchAddSharedFamilyParameters.BatchAddSharedFamilyParametersCommand),
                                    "Dodaje wybrane shared parameters z wskazanego pliku do rodzin .rfa przez otwarcie ich w tle.",
                                    "Co robi: dodaje wybrane shared parameters do rodzin .rfa. Jak uzyc: 1. Wybierz plik Shared Parameters i rodziny. 2. Zaznacz definicje, ustaw instance albo type i grupe. 3. Zapisz zmiany. Zakres: batchowo, bez otwierania rodzin w UI.",
                                    typeof(AlwaysAvailableCommandAvailability)),
                                new RibbonPushButtonDefinition(
                                    "HcbRenameFamilyContentButton",
                                    "Rename Family Params",
                                    typeof(Features.RenameFamilyContent.RenameFamilyContentCommand),
                                    "Zmienia nazwy parametrow rodzinnych mozliwych do edycji wedlug wspolnych regu.",
                                    "Co robi: zmienia nazwy parametrow rodzinnych mozliwych do edycji. Jak uzyc: 1. Wybierz rodziny .rfa. 2. Ustaw reguly zmiany nazw. 3. Zapisz wynik. Zakres: dziala w tle i pomija parametry shared, systemowe oraz kolizje nazw.",
                                    typeof(AlwaysAvailableCommandAvailability))
                            ],
                            typeof(AlwaysAvailableCommandAvailability))
                    ]),
                new RibbonPanelDefinition(
                    "Manage",
                    [
                        new RibbonStackDefinition(
                            [
                                new RibbonPulldownDefinition(
                                    "HcbParametersPulldown",
                                    "Parameters",
                                    typeof(Features.SharedParameters.SharedParametersCommand),
                                    "Resources\\Ribbon\\Parameters",
                                    "Narzedzia do pracy z parametrami projektu.",
                                    "Co zawiera: narzedzia do sprawdzania i uzupelniania parametrow wspoldzielonych. Jak uzyc: wybierz odpowiednie polecenie po ustawieniu poprawnego pliku Shared Parameters. Zakres: parametry HC_* w projekcie.",
                                    [
                                        new RibbonPushButtonDefinition(
                                            "HcbSharedParametersButton",
                                            "Shared Params",
                                            typeof(Features.SharedParameters.SharedParametersCommand),
                                            "Sprawdza i wczytuje wymagane parametry wspoldzielone.",
                                            "Co robi: sprawdza i dodaje brakujace parametry wspoldzielone HC_*. Jak uzyc: 1. Ustaw poprawny plik Shared Parameters. 2. Uruchom polecenie. 3. Zapisz wynik. Zakres: odpowiednie kategorie projektu.")
                                    ]),
                                new RibbonPushButtonDefinition(
                                    "HcbPurgeAnnotationsButton",
                                    "Purge Anno",
                                    typeof(Features.PurgeAnnotations.PurgeAnnotationsCommand),
                                    "Usuwa nieuzywane style adnotacji.",
                                    "Co robi: usuwa nieuzywane style adnotacji. Jak uzyc: 1. Uruchom narzedzie. 2. Sprawdz liste nieuzywanych stylow. 3. Zatwierdz usuniecie. Zakres: typy kategorii Annotation bez instancji.")
                            ]),
                        new RibbonPushButtonDefinition(
                            "HcbHCWireSizeButton",
                            "HC Wire",
                            typeof(Features.HCWireSize.HCWireSizeCommand),
                            "Generuje skrocony opis przewodu dla obwodow elektrycznych typu Power.",
                            "Co robi: generuje skrocony opis przewodu dla obwodow elektrycznych typu Power. Jak uzyc: uruchom polecenie w projekcie z obwodami. Zakres: nadpisuje HC_WireSize na podstawie liczby zyl i przekroju odczytanego z Wire Size."),
                        new RibbonPushButtonDefinition(
                            "HcbRenameMaterialsButton",
                            "Rename Materials",
                            typeof(Features.RenameMaterials.RenameMaterialsCommand),
                            "Zmienia nazwy wybranych materialow.",
                            "Co robi: seryjnie zmienia nazwy materialow. Jak uzyc: 1. Wybierz materialy. 2. Ustaw prefiks, zamiane tekstu i lub sufiks. 3. Zatwierdz podglad. Zakres: tylko wybrane materialy.")
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
        string LongDescription,
        Type? AvailabilityType = null,
        string? IconResourceDirectory = null) : RibbonStackItemDefinition;

    internal sealed record RibbonPulldownDefinition(
        string Name,
        string Text,
        Type IconCommandType,
        string? IconResourceDirectory,
        string ToolTip,
        string LongDescription,
        IReadOnlyList<RibbonPushButtonDefinition> Buttons,
        Type? AvailabilityType = null) : RibbonStackItemDefinition;

    internal sealed record RibbonStackDefinition(
        IReadOnlyList<RibbonStackItemDefinition> Items) : RibbonItemDefinition;
}
























