# Backlog Narzedzi HCB

## Otwarte issues

### 1. Numerowanie ksztaltek HVAC
Status: do dopracowania

Cel:
Narzedzie do numerowania ksztaltek powinno nadawac ten sam numer / ten sam parametr wszystkim elementom, ktore maja identyczny komplet parametrow `LIN_...`.

Do ustalenia przy poprawce:
- ktore dokladnie parametry `LIN_...` wchodza do porownania,
- czy porownanie ma byc 1:1 po wszystkich wartosciach,
- jak obslugiwac braki, puste wartosci i roznice formatowania,
- czy numer ma byc wspolny tylko w obrebie systemu, czy globalnie.

Oczekiwany efekt:
Jesli dwa lub wiecej elementow maja te same wartosci wszystkich wymaganych parametrow `LIN_...`, powinny dostac identyczna wartosc parametru numeracyjnego.

### 2. Numerowanie akcesoriow i Air Terminals
Status: bug

Problem:
Narzedzie do numerowania akcesoriow i `Air Terminal` obecnie nie dziala poprawnie.

Zakres do sprawdzenia:
- selekcja i filtrowanie obslugiwanych elementow,
- wykrywanie systemu,
- budowanie klucza grupujacego,
- zapis do parametru docelowego,
- obsluga pustych / tylko do odczytu parametrow.

Oczekiwany efekt:
Numerowanie powinno poprawnie obslugiwac `Duct Accessory`, `Pipe Accessory` i `Air Terminal` zgodnie z logika narzedzia.

### 3. System Assigner
Status: bug

Problem:
Narzedzie uruchamia sie, ale nie przypisuje wartosci do elementow systemu.

Zakres do sprawdzenia:
- odczyt wartosci z urzadzenia zrodlowego,
- wykrycie wlasciwego systemu / systemow,
- iteracja po elementach podpietych do systemu,
- znalezienie i zapis do parametru `HC_System`,
- przypadki, w ktorych parametr istnieje tylko na czesci elementow lub jest read-only.

Oczekiwany efekt:
Po wskazaniu urzadzenia narzedzie powinno wpisac oczekiwana wartosc `HC_System` do elementow nalezacych do jego systemu.
