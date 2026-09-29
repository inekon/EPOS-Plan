# Protokoll KP1b — Konditionierungsprofile, zweite Hälfte der Stufe KP1

**Datum** 27.–29.09.2026 · **Sitzung** Gebäudesimulation in der Cloud, Arbeitszweig `claude/inspiring-bell-b8wq90`,
Integration `kp1b-integration` · **Stufe** KP1 der [Konditionierungsprofile](../../../aktuell/Konzept_Konditionierungsprofile_EPOS-Plan.md),
zweite Hälfte (KP1b) · **Entwurf** [`Entwurf_KP1b_Konditionierungsprofile.md`](../../Entwurf_KP1b_Konditionierungsprofile.md) ·
**Entscheid** E54 ([N1.62](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) · **Festlegungen der Umsetzung**
N1.63 · **Statuszeile** #596

Vorausgegangen: [Protokoll KP1a](2026-09-27_KP1_Konditionierungskalender.md) (Schemaschritt 151, Kalendermodell,
Standardfahrplan, fünf Reihen, `KonditionierungCtrl`).

## 1. Verfahren

Entwurf vor Bau: zwei Leser (Sonnet), zwei unabhängige Entwürfe — „Datenmodell und Kopierwege zuerst" und „Rechenweg
und Risiko zuerst" — und eine Gegenprüfung (Opus), die Synthese als Entwurfspapier, der Entscheid E54 zu den zwei
offenen Fragen. Umsetzung in sieben Wellen durch Agenten in eigenen Worktrees: W1 (Opus 5.5), danach zwei Spuren
parallel — Rechenweg R1 → R2 → R3 und Daten D1 → D3/D2 (Opus 5). Jede Welle mit Abnahme im eigenen Worktree und einem
Gate der Orchestrierung nach dem Merge; drei Zwischenstände gingen gegatet auf `ios_migration_september` (W1 `5925ec08`,
D1 + R1 `f5972ef9`, der Rest mit #596).

## 2. Was gebaut ist

### W1 — Schemaschritt 152 (`KP-S1v`, `KonditionierungVorlagenSchema`)
Tabelle `Tab_Konditionierungsvorlage_STAMM` (STRICT, sechs Spalten, Namensregel als benannter Index
`(Groesse, Bezeichner COLLATE NOCASE)`, `CHECK trim`), Fremdschlüssel `ID_Vorlage` mit `ON DELETE CASCADE` in Kalender- und
Vorgabetabelle per Tabellenneubau nach dem Rezept von Schritt 96 (Zieltext aus `sqlite_master`, `legacy_alter_table`,
`VorgangOhneFremdschluessel`), acht Teilindizes der Eindeutigkeit (die ersten im Schema), `Nachtauskuehlstunden_H` an
`Tab_ErgebnisGebaeude` und `Tab_ErgebnisZone`. Ein Vorgang, wiederholbar; die Testdatenbank ohne Datenänderung auf 152.

### R1 — Korrekturen im Rechenweg
Vorlaufstart bei „aus" als unbeheizte Zone auf allen drei Startwegen (Einzone, Zonenlauf, Zonenschleife samt
Jacobi-Schritten), Schwelle der Sommerlüftung je Stunde mit Kühlkalender (`Sommerlueftungsregel` mit einer
Schaltstelle), Zuluft gekoppelter Zonen mit dem Zusatz des Lüftungskalenders, konstante Kühlprüfung nur ohne
Kühlkalender, Hinweis auf den wirksamen Kühl-Nachtwert (R14), Vorlauf der Übergabe nur in Stunden mit Heizung samt
getrennter Zählung `StundenOhneHeizungH`.

### R2 — Nachtauskühlung bedingt nach P9 (b)
`Nachtauskuehlvorgabe` am `Konditionierungssatz` (Fenster, Tagwert, ΔT) aus der Matrix des Eigentümers, dessen
Lüftungskalender gilt; Teilung der Nutzerreihe in unbedingten und bedingten Anteil; zweites Regelexemplar je Zone;
`ZusatzleitwertWK(h, sommer, nacht)` mit wörtlichem Nullzweig; Kennzahl bis in beide Ergebnistabellen; Zwischenspeicher
des freien Falls mit vier Plätzen (R7).

### R3 — Nutzungszeit, Auslegung, F21, Untertemperatur, Infiltration
Nutzungsmaske aus dem Personenkalender nur in den Kennzahlen (F16), Auslegungsraumtemperatur der Übergabe und der Kälte
(beide Stellen), Auslegungsheizlast mit dem höchsten unbedingten Luftwechsel, F21 gegen den Heizkalender, Hinweis auf
Untertemperatur außerhalb der Heizperiode, Infiltration im Kalenderweg aus der Matrixzelle Lüftung/`NENNWERT`.

### D1 — Kopierwege
`Konditionierungskopie` (ein SQL-Kopierer für alle Wege, Alt→Neu-Zuordnung im selben Vorgang, Vorlagenfilter nach E54),
`Matrixzellenort` samt Zellenort-Weiche auf allen Schreibwegen, Schloss, `Eigner.Vorlage`, `CopyFromStamm` in einem
Vorgang, „Konditionierung erneut übernehmen", `GebaeudeStammCtrl.SpeichernUnter` samt Bindung in der Hülle (Razor
unverändert), Katalogbau duplizieren, Vorschau des Arbeitsstands; Proben für Duplikat, Variante, `.wpx`-Rundlauf und
Paketanhebung.

### D3 — Vorlagen und Werkzeuge der Karte
`KonditionierungsvorlageCtrl` (Liste je Größe, als Vorlage speichern nach E54, übernehmen nach P12, umbenennen, löschen,
duplizieren, Namensregel), `Kalenderwerkzeuge` (Zeitfenster, Feiertage als Regel), Rangbänder an einer Stelle in
`Standardfahrplan`.

### D2 — Auslieferungsvorlage
Achte Frage „Konditionierung" im Prüfbericht: Vorlagen je Größe gesperrt/eigen, Kalender, Vorgaben und Perioden je
Eigentümerart, Prüfungen auf fremde Größe, Nennwert/Saison (E54), Waisen und eigene Vorlagen nach `--kataloge readonly`.

### Nachzüge der Orchestrierung
Fehlendes `</data>` nach der eigenen Konfliktauflösung des D1-Merges eingesetzt (`d89100f7`); die Probe der
Paketanhebung 151 nimmt den Zielstand statt Schritt 152 (seit Schritt 153 der Zapfprofil-Sitzung); der Mehrzonenweg
meldet Kühl-Nachtwert und Nachtauskühlung je Zone; „außerhalb der Heizperiode" heißt allein die Saisonperiode.

## 3. Schemaschritte

| Nummer | Name | Inhalt | Ergebnisneutral |
|---|---|---|---|
| **152** | KP-S1v (`KonditionierungVorlagenSchema`) | Vorlagentabelle, Fremdschlüssel `ID_Vorlage` per Neubau, acht Teilindizes, `Nachtauskuehlstunden_H` | ja — die Tabellen sind leer, die Spalten nullbar |

Die Nummer ist vor dem Schemacommit und vor jedem Push gegen `origin` geprüft. Die Nachbarsitzungen haben ihre Schritte
danach als 153 (Bezugsart Zimmer) und 154 (Heizgrenze der Kesselbereitschaft) angehängt.

## 4. Festlegungen der Umsetzung

Nummeriert als Nachtrag [N1.63](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) im Leitkonzept.

## 5. Befunde der Umsetzung

| Befund | Ursache | Behebung |
|---|---|---|
| **Laufabbruch** bei „aus" in der ersten Vorlaufstunde (G1) | `Zonenmodell2K.Zuruecksetzen(NaN)` wirft; die Regel aus 3.6 fehlte | Startwert der unbeheizten Zone auf allen drei Wegen (R1) |
| Nachtzeile der Lüftung wirkte **unbedingt** | `BedingtK` wurde nur durchgereicht | Nachtauskühlung nach P9 (R2); der KP1a-Test, der die unbedingte Wirkung hielt, als Mechanikprobe umgeschrieben |
| **Vorlauf außerhalb der Heizperiode** bei festem Anlagenvorlauf | `Zonenmodell2K.Schritt` setzte den Vorlauf mit Übergabe auch in Stunden ohne Heizung | nur mit Heizung (R1); die Probe war vorher im Fall „fester Anlagenvorlauf" rot |
| **Infiltration 0** mit Lüftungskalender | gelesen aus dem Kalender-Nennwert, den die Lüftung nicht führen darf | aus der Matrixzelle Lüftung/`NENNWERT` (R3) |
| Zellenort nur Konvention (NB9) | `Vorgabematrix.Bestandszelle` nahm den Wert der Vorgabezeile vor der Bestandsspalte | Weiche auf allen Schreibwegen, Leser nach 5.6 (D1) |
| Schloss fehlte | kein Schreibweg prüfte `ReadOnly` | jeder Schreibweg benannt abgelehnt (D1) |
| Vorschau des Arbeitsstands ohne Konditionierung (NB3) | Id 0 fand keine Zeile | Überladung des Datenwegs je Eigentümer (D1) |
| Zwischenspeicher des freien Falls (R7) | Ein-Platz-Speicher, 2 418 Neubauten im freien Lauf | vier Plätze, bitgenauer Schlüssel: 2 Neubauten, rund 17 % schneller, bitgleich (R2) |
| `RENAME` schrieb den Verweis der Periodentabelle um | SQLite 3.53 ohne `legacy_alter_table` | Pragma im Neubau, Gegenprobe (W1) |
| Eigene Betriebspause zählte als „außerhalb der Heizperiode" | Prüfung nur über die Art `BETRIEBSPAUSE` | Saisonrang 900 zusätzlich (Nachzug) |
| Mehrzonenweg ohne Hinweise aus R1/R2 | `MeldenMehrzonen` rief nur zwei der vier Hinweise | alle vier je Zone (Nachzug) |
| Ungültige `.resx` nach dem D1-Merge | die eigene Konfliktauflösung verlor ein gemeinsames `</data>` | eingesetzt, XML-Prüfung nach jedem Merge; das Gate fand es vor jedem Push |
| Probe „Paket auf Stand 151" rot nach Schritt 153 | Ziel fest auf Schritt 152 | Ziel `SchemaStand.Zielversion` |

**Umgebungsbefunde der Cloud, nicht aus KP1b:** `TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` war unter
Python 3.11 rot (von der Zapfprofil-Sitzung mit `math.fsum` behoben); gegen die unter Windows eingefrorene Basis R23
wichen 1008, 1023 und 1042 unter Linux in Erzeuger- und Speicherreihen ab (deterministisch, vor Schritt 152 vorhanden) —
gegen die unter Linux eingefrorene Basis R24 rechnen alle fünfzehn Projekte byte-gleich.

## 6. Nachweise

| Nachweis | Ergebnis |
|---|---|
| Abnahme je Welle im Worktree | W1 1103/1104 (rot nur der Tww-Umgebungsfall), D1 2120/2121, R1 1529/1529, R2 1763/1763, D3/D2 948/949 und Auslieferungsvorlage 40/40, R3 2016/2017 (rot die berichtigte Paketprobe); je Welle Referenzlauf 460 von 460 CSV byte-gleich gegen den eigenen Ausgangsstand |
| Gate W1 (`4ea55d1e`) | Tests grün bis auf den Tww-Fall; SqlDialektPruefer 0; Windows-Schale 0 Fehler; Auslieferungsvorlage 38/38 |
| Gate D1 + R1 auf Schritt 154 und R24 (`f5972ef9`) | **alle Tests grün** (EPOS.Kern.Tests 8 970, EPOS.UI.Tests 6 887, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27, 1 übersprungen); Referenzlauf **GESAMT PASS, 460/460 byte-gleich gegen R24** und 460/460 gegen `origin` ohne D1/R1; ChartProben gleich der Messlatte; SqlDialektPruefer 0; Windows-Schale 0; Auslieferungsvorlage 39/39 |
| Schluss-Gate (`09ae080c`) | siehe Statuszeile #596 |

## 7. Offen — Übergabe an KP2 und KP3

- **KP2:** Oberfläche (Reiter „Konditionierung", Matrix, Kalenderkarten, Vorlagenverwaltung), Rückfragen zu „Speichern
  unter" und „erneut übernehmen" (der Befund `ZonenZurueck` liefert die Zahl), `Nachtauskuehlstunden_H` in Hülle und
  Dialog, Saat der 14 ausgelieferten Vorlagen (KP-S1b) samt `KonditionierungsvorlagenWacheTests`, KI-Aktionswissen; in
  der ersten Kernwelle gbXML `SollHeizenC`, Ferien des Zapfprofils und „Zeitstruktur übernehmen".
- **KP3:** Bericht, CSV, KI-Sicht und Variantenvergleich der Nachtauskühl- und Sommerlüftungsstunden (E54), Rampenstunden
  in der Nutzungszeit, Aufheizoptimierung, neues Referenzprojekt, Einfrierregel, neue Basis.
- **Benannt, Widerspruch möglich:** Auslegung und F21 nehmen als Nutzungszeit die Nachtzeit, nicht die Personenmaske
  (N1.63); die Umstellung wäre je Auflöser eine Zeile.
