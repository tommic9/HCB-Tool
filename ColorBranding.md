# Kolorystyka HellCold

## Kolory podstawowe

| Rola koloru | HEX | RGB | Zastosowanie UI |
|---|---|---|---|
| Biały | `#FFFFFF` | 255,255,255 | tło główne strony, powierzchnie kart, formularze |
| HellCold Navy | `#2E466F` | 46,70,111 | nagłówki, topbar, stopka, elementy nawigacji |
| HellCold Blue | `#1F8ECE` | 31,142,206 | przyciski główne (CTA), aktywne elementy, linki |

---

# Zalecany schemat użycia w UI

## Tło strony
`#FFFFFF` – główne tło aplikacji.

## Nagłówki i struktura
`#2E466F` – pasek nawigacji, nagłówki sekcji, elementy strukturalne.

## Akcenty i interakcje
`#1F8ECE`

Stosować dla:
- przycisków głównych
- aktywnych zakładek
- linków
- wskaźników stanu (focus)

---

# Hierarchia wizualna

1. **Primary (akcje)**  
   `#1F8ECE`

2. **Structural (layout)**  
   `#2E466F`

3. **Background (czytelność)**  
   `#FFFFFF`

---

# Zalecenia UX

- **CTA zawsze w HellCold Blue (`#1F8ECE`)** – zwiększa widoczność głównej akcji.
- **Topbar w HellCold Navy (`#2E466F`)** – wzmacnia identyfikację marki.
- **Treść na białym tle (`#FFFFFF`)** – zapewnia maksymalną czytelność na urządzeniach mobilnych.

---

# Przykładowa mapa kolorów UI

| Element | Kolor |
|---|---|
| Navbar | `#2E466F` |
| Tło strony | `#FFFFFF` |
| Primary button | `#1F8ECE` |
| Hover button | ciemniejszy odcień `#1F8ECE` |
| Link | `#1F8ECE` |
| Nagłówek H1/H2 | `#2E466F` |
| Karta (card) | `#FFFFFF` |

---

# TailwindCSS – propozycja tokenów kolorów

```js
colors: {
  "hellcold-navy": "#2E466F",
  "hellcold-blue": "#1F8ECE",
  "hellcold-white": "#FFFFFF"
}