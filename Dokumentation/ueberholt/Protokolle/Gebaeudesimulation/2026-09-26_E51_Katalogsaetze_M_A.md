# Protokoll E51 — Katalogsätze der Klassen M und A, freier Rückfall nach Stein/Loga (26.09.2026)

**Auftrag.** Der letzte Teil des Auftrags vom 26.09.2026 („U-Wert-Vorgaben für Neubauten ab 2021",
[Leitkonzept](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.57): Nach E47 hatten die
Baualtersklassen **M** (ab 2021) und **A** (bis 1859) keinen Satz im Gebäudekatalog und damit keine Vorgabe für
U-Werte, g-Wert und ψ; der Bauteilvorschlag lehnte einen importierten Neubau ohne U-Werte und Schichten ab.
**E51** (Anwender, 26.09.2026, Leitkonzept N1.58): beides — eigene Katalogsätze für M und A und der freie Wert
nach Stein/Loga (2025) als Rückfall für Klassen und Standards ohne Katalogsatz; F4 des
[Konzepts Baualtersklassen](../../../aktuell/Konzept_Baualtersklassen_Energiestandard_EPOS-Plan.md) ist
aufgehoben, E27 bei U12 geändert. Arbeitsgrundlage war die
[Übergabe](../../../aktuell/Gebaeudesimulation/2026-09-26_Uebergabe_G6c_Katalog_M_A.md), Abschnitt 2.2.

## 1 Entscheide des Anwenders zur Umsetzung (26.09.2026)

Vor der Saat hat die Umsetzung den Entwurf beim Anwender bestätigen lassen (Leitkonzept N1.58, offener Punkt 2).

| Nr. | Rückfrage | Antwort |
|---|---|---|
| 1 | Die sechs Sätze (drei für M, drei für A) mit Namen, Klassen, Energiestandard und Kennwerten, wie entworfen | **wie vorgeschlagen** |
| 2 | Den freien Rückfall bauen, obwohl nach der Saat jede Klasse A–M Katalogsätze hat | **bauen** — Tabelle nach Stein/Loga, Vorrang Standard → Klasse → freier Wert |
| 3 | Woher die ψ-Werte der Sätze kommen | **aus der Quelle** — abgeleitet aus dem Wärmebrückenzuschlag ΔU_WB |
| 4 | Luftwechsel, innere Gewinne und die übrigen Nutzungsspalten | **nach dem EPOS-Muster** der Wohngebäude im Katalog |

## 2 Quellen

- **Gebäudeenergiegesetz, Anlage 1** (Referenzgebäude Wohngebäude) für `EFH-GEG-Ref`, am Normtext geprüft:
  Außenwand 0,28, Grund 0,35, Dach 0,20, Fenster 1,3 W/(m²K) bei g 0,60, Türen 1,8 W/(m²K),
  ΔU_WB 0,05 W/(m²K). Das Gesetz heißt inzwischen Gebäudemodernisierungsgesetz (GModG, zuletzt geändert am
  23.07.2026); Anlage 1 ist unverändert. Die Namen tragen weiter „GEG", der Beleg im Quelltext vermerkt es.
- **KfW-Seite „Das Effizienzhaus"** (Abruf 26.09.2026) für `EFH-GEG-EH55`: H'T höchstens 70 % des
  Referenzgebäudes. Die gleichmäßige Skalierung aller Bauteile mit 0,70 ist eine benannte EPOS-Annahme.
- **Stein, B.; Loga, T. (2025):** *Das Typgebäude-Modell zur energetischen Bewertung des
  Wohngebäudebestands*, IWU im Auftrag des BBSR, Zenodo, Record 15488271, CC BY 4.0. Genutzt sind allein
  Anhang A, Tab. 26, 27, 28, 33, 34 und 39 (differenziertes Modell, Referenzjahr 2025) und das Standardfenster
  aus Tab. 62 — nicht Tab. 60 mit den Werten der Typologie 2015. Die Quelle fasst alles vor 1918 in einer
  Klasse „bis 1918" zusammen; A und B beruhen auf derselben Quellklasse. Die PDF liegt nur lokal, nicht im
  Repositorium.
- **IWU-Wohngebäudetypologie 2015:** bleibt unfrei; von ihr stammen weiter nur die Jahresgrenzen der Klassen.

## 3 Umsetzung

**Schemaschritt 149** (`GebaeudeSaat`, `GebaeudeSaatSchema`) sät sechs Sätze in `Tab_Gebaeude_STAMM`, alle
`ReadOnly = 1`, ohne Produktdaten. Schlüssel ist der Bezeichner (eindeutiger Index), keine feste Id; der
Schritt legt nur an, was unter seinem Namen fehlt, und überschreibt nie.

| Satz | Klasse | Grundlage |
|---|---|---|
| `EFH-GEG-Ref` | M | Referenzgebäude nach GEG Anlage 1, Flächen nach Stein/Loga |
| `EFH-GEG-EH55` | M, Energiestandard Effizienzhaus 55 | U-Werte von `EFH-GEG-Ref` × 0,70 |
| `KMH-GEG-typ` | M | kleines Mehrfamilienhaus, Typgebäude 2021–2025 nach Stein/Loga |
| `EFH-bis1859-U` | A | Einfamilienhaus im Urzustand nach Stein/Loga „bis 1918" |
| `KMH-bis1859-U` | A | kleines Mehrfamilienhaus im Urzustand, ebenso |
| `EFH-bis1859-TS` | A | Einfamilienhaus, anteilig modernisiert, ohne Energiestandard, ebenso |

**Regeln für alle sechs Sätze.** Außenwand netto; Dach = Dach und oberste Geschossdecke, Grund = Kellerdecke,
Boden und Wände gegen Keller und Erdreich, jeweils flächengewichtet; Sonstige = Außentüren. Die Anschlusslänge
Fenster–Wand folgt dem Umfang des Standardfensters 1,23 m × 1,48 m (2,98 m je m² Fenster), die Längen
Wand–Dach und Außenwand–Keller dem Umfang einer quadratischen Grundfläche. ψ steht im Verhältnis
0,05 : 0,16 : 0,24 und ist so skaliert, dass Σ ψ × l gleich ΔU_WB × Hüllfläche ist. Nutzfläche = Wohnfläche,
35 m² je Nutzer, innere Gewinne 5 W/m² × Wohnfläche (E43), Bauweise 50 Wh/(m²K) × Wohnfläche (schwere
Bauart; Stein/Loga Tab. 39: 49,5 für EZFH), Sollwerte 20 / 18 / 24 °C, Luftwechsel 0,6 1/h (M) und
0,7 1/h (A), Fenster gleich auf die vier Himmelsrichtungen verteilt; Warmwasser und Ferienfahrplan nach dem
Muster der Wohngebäude im Katalog; Baujahr und die Spalten der Stufen G1, G2 und der Übergabe leer.

**Keine Kennzahl im Namen.** Die Zahl am Ende der vorhandenen Namen („EFH-A-U-338") ist die Vorläuferspalte
`spez_Waermeverbrauch`, die kein heutiger Rechenweg erzeugt (Befund D: das Tagesmodell erreicht 48 bis 88 %,
der VDI-6007-Weg 86 bis 100 % davon). Die sechs Sätze lassen sie und `Waermebedarf` leer.

**Neue Mediane** (U in W/(m²K), ψ in W/(mK)), nachgerechnet von `GebaeudeVorgabenTests` aus der
Testdatenbank:

| Zeile | Sätze | U AW | U Fe | U Dach | U Grund | U Sonst | g | ψ FW | ψ WD | ψ AK |
|---|---|---|---|---|---|---|---|---|---|---|
| Klasse A | 3 | 1,37 | 3,49 | 1,22 | 1,06 | 3,49 | 0,59 | 0,021 | 0,067 | 0,101 |
| Klasse M | 3 | 0,20 | 0,95 | 0,14 | 0,25 | 1,26 | 0,60 | 0,041 | 0,132 | 0,198 |
| Effizienzhaus 55 | 1 | 0,20 | 0,91 | 0,14 | 0,25 | 1,26 | 0,60 | 0,041 | 0,132 | 0,198 |

**Freier Rückfall.** `GebaeudeVorgaben` führt 13 Zeilen (A–M) nach Stein/Loga (2025), Anhang A, Tab. 28
(Typgebäude EZFH, nicht modernisierte Bauteile), auf zwei Stellen gerundet; ψ bleibt leer, weil die Quelle nur
einen Wärmebrückenzuschlag führt. Die Kette in `Fuer(klasse, standard)`: Standard mit Katalogsätzen → Klasse
mit Katalogsätzen → freier Wert; der Import fragt allein die Klasse. Herkunft im Speicher
`Importherkunft.VorgabeFrei`, gespeichert als `VORGABE` und unterschieden über den Beleg
`GIMP_BELEG_VORGABE_FREI`; Meldungen `KLASSE_VORGABE_FREI` und `U_VORGABE_FREI`, Texte in beiden Sprachen. Weil
jede Klasse A–M jetzt Katalogsätze hat, **ruht der Rückfall**; die Tests prüfen ihn über die Lesenaht
`GebaeudeVorgaben.KatalogOhne(...)`. Ein importierter Neubau der Klasse M ohne U-Werte und Schichten bekommt
die Katalogvorgabe und wird nicht mehr abgelehnt.

**Lizenz, Wiki, Referenzlauf.** Quellenangabe nach CC BY 4.0 in `Setup/Vorlage/Lizenzhinweise.txt`
(Abschnitt 6); die Wiki-Quellen „Gebäude" und „Gebäudeimport" nennen die Vorgabe mit einem Satz. In
[`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md) steht der Nachtrag „Schritt 149 ohne
Neufreigabe": Die sechs Sätze führt kein Referenzprojekt, kein Rechenweg liest Katalog, Klasse oder Vorgabe;
die Einfrierregel „gesäte Gebäudedaten" ist nicht berührt.

**Commits.**

| Commit | Inhalt |
|---|---|
| `6e720f86` | Katalogsätze der Klassen M und A, Schemaschritt 149, Testdatenbankschema, Testdatenbank 149, Saat-Tests |
| `48bbb0fb` | Satzzahl des Gebäudekatalogs in den Tests 269 → 275 |
| `ab09e85c` | Vorgaben A und M aus dem Katalog, freier Wert als Rückfall, Herkunft, Meldungen, Texte |
| `81782da2` | Lizenzhinweis Stein/Loga, Wiki-Quellen, Nachtrag Schritt 149 im Referenzlauf-Papier |
| `6a290c8c` | Merge von origin |
| `44dadc8d` | Saat-Anweisung am Aufruf, damit der SQL-Dialekt-Prüfer sie sieht |

Gepusht mit `ef20793f`.

## 4 Nachweise

- **Testdatenbank:** Schema 148 → 149 mit `Werkzeuge/Testdatenbankschema`, sechs Zeilen, ein zweiter Lauf 0/0;
  einzige Abweichungen `Tab_Applikation.SchemaVersion` und die sechs Zeilen (`Tab_Gebaeude_STAMM` 269 → 275);
  `integrity_check` ok, `foreign_key_check` leer.
- **Gate:** Kern 8 256 Tests (1 übersprungen), UI 6 661, SpeicherEngine 386, SpeicherPlanung 27
  (1 übersprungen), KiKern 549 — grün.
- **Referenzlauf:** 14/14 PASS gegen `2026-09-26_R20_Zapfprofil`, 432/432 CSV byte-gleich; keine neue Basis.
- **SQL-Dialekt-Prüfer:** 1 995 Texte, 0 Funde.
- **Auslieferungsvorlage:** Prüfbericht 38/38.

## 5 Offen

- **Wiki:** Die Seiten „Gebäude" und „Gebäudeimport" gehen mit dem Sammel-Upload 1.2.0.4 hinaus; der
  Logbuch-Satz der Baualtersklassen (E47) nennt dort auch die neuen Katalogsätze.
- **Windows-Sichtabnahme** der Baualtersklassen (E47) steht weiter aus.
- Einen Katalogsatz für Nichtwohngebäude der Klassen A und M gibt es nicht; die Klassenzeile umfasst alle Sätze
  der Klasse, ein Nichtwohngebäude bekommt den Median der Wohngebäudesätze.
