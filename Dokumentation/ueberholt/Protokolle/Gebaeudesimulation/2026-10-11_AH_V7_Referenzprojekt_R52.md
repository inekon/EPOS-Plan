# Protokoll AH-V7 — Referenzprojekt 1065 „Vorheizen“, Wache, Einfrierregel, Basis R52

**11.10.2026 · Sitzung Gebäudesimulation · Welle V7 der Aufheizoptimierung Fassung 2.** Grundlage:
[Entwurf Vorheizrampe](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md), Abschnitt 4 und
Entscheid F9 („neues Referenzprojekt als Kopie von 1051 mit dem neuen Verfahren, 1051 bleibt beim Bestand“), F13 (a),
F15 (a), F17 bis F19; aufgesetzt auf [AH-V4](2026-10-11_AH_V4_Schema215.md) (Schritt 215) und
[AH-V6](2026-10-11_AH_V6_Bericht_Export.md) (Exportschlüssel `Geb[n].Vorheiz*` nur bei Verfahren ≠ Rampe).

## Referenzprojekt 1065

- **Saatskript** `Referenzlaeufe/Skripte/referenzprojekt_1065_vorheizen.cs` (dotnet ab SDK 10, Prüfmodus `--trocken`,
  wiederholbar): Kopie 1051 → 1065 auf dem Kopierweg des Programms (`ProjektDuplizierenCtrl.Duplizieren`), dann
  `Aufheiz_Verfahren` = `BERECHNET`, die übrigen sechs Vorheizzellen leer (Vorgaben: Toleranz 20 % Prozent, ε 1 K, Geltung
  Gebäude, Heizkreis einbezogen), `Aufheizoptimierung` 1 wie 1051, Beschreibung, `Kosten_Geaendert` leer. Geschrieben in
  eine Arbeitsdatei, geprüft (jede gesäte Zelle, je Projekttabelle Zeilenzahl gleich 1051, Zonen, sechs Gebäudekalender,
  `integrity_check`, `foreign_key_check`), dann ersetzt sie das Original.
- **Abweichung vom Auftrag:** C#-Skript statt Python. Der Python-Kopierweg des Musters 1064 bildet `ProjektDuplizierenCtrl`
  auf dem Stand des Schemaschritts 210 nach; 1051 trägt Gebäude- und Zonenkalender, Perioden und Vorgabezeilen, die der
  Programmweg auf Schema 215 sicher mitnimmt. Muster ist das C#-Skript von 1063.
- **Zellvergleich** gegen den Stand vorher (`ATTACH`, `EXCEPT` je Tabelle): 9 449 neue Zeilen in 30 Tabellen, alle mit
  `ID_Projekt` 1065 bzw. Kinder davon, **keine geänderte oder entfernte Zeile**; `integrity_check` ok, `foreign_key_check`
  leer. Testdatenbank: Schemastand 215, **96 546 816 Byte, LFS-SHA-256
  `832fda5088ad3bb21b3e7ec55dec715b573afe22a9c952d2e2730d3a78595f3f`** (vorher 95 711 232 Byte, `cc9222a8…`).

## Kennzahlen (Lauf aus der Datenbank, Basis R52)

| Größe | 1051 (Rampe) | 1065 (berechnet) |
|---|---|---|
| Vorheizzeit t_V (95-%-Quantil) / größter Bedarf | — | 2 h / 5 h |
| Φ_K,max / Deckel P_K / P_V | — | 23,617 / 28,340 / 28,340 kW |
| Spitze des Gebäudes | 30,00 kW | 28,34 kW |
| Tage über t_V / ohne Ankunft / unerreichbar | — | 5 / 5 / 0 |
| Größte Unterschreitung / Unterschreitungsstunden | — | 1,54 K / 5 h |
| Deckelstunden / Floorstunden / Nächte ohne Absenkung | — | 26 / 0 / 0 |
| Heizwärme | 24,224 MWh | 24,673 MWh (+0,450 MWh, +1,9 %) |
| Mehrwärme gegen den Vorlauf (`Vorheiz_Mehrwaerme_Mwh`) | — | 0,998 MWh |

Die Zahlen decken sich mit der Messung V3c (t_V 2 h, Tage über t_V 5, Spitze 28,3 kW, +450 kWh).

## Wache

`EPOS.Kern.Tests/VorheizReferenzprojektWacheTests` (5 Fälle): jede gesäte Zelle; alle übrigen Zellen von
`Tab_Einstellungen` und `Tab_Gebaeude` sowie die Gebäudekalender wie 1051, 1051 auf der Sollwertrampe; 1065 einziges Projekt
mit Vorheizzellen; ein Lauf beider Projekte (t_V, Bedarf, Tage über t_V, Tage ohne Ankunft, Nächte 0 exakt — ganzzahlig;
Φ_K,max, Deckel und Heizwärme relativ 1e-4 wie der Referenzvergleich; Deckel = 1,2 · Φ_K,max; Spitze am Deckel und unter
1051; Mehrwärme gegen 1051 ±0,01 MWh; Spalten `Vorheiz*` in `Tab_ErgebnisGebaeude` geschrieben, bei 1051 leer); die 18
Schlüssel `Geb[0].Vorheiz*` eines Laufs gegen die Basis.

Nachgezogen, weil die Kopie die Ausschließlichkeit von 1051 bricht: `KonditionierungReferenzprojektWacheTests` (Kalender,
Nachtauskühlung, Sommerlüftung, Ferien, Katalogbau und Aufheizoptimierung lassen 1065 als Kopie zu),
`FreieKuehlungReferenzprojektWacheTests` (1064 nicht mehr das jüngste Projekt), `Ak3KReferenzprojektWacheTests` (Projekte
nach 1059), `VorheizenSchemaTests` (1065 trägt ein Verfahren).

## Basis R52

`Referenzlaeufe/2026-10-11_R52_Vorheizen`: dreißig Projekte, 979 CSV, 6 777 Skalare, auf Linux (de-DE) gegen die
Testdatenbank `832fda50…` eingefroren. Nachweise:

- die 29 Projekte von R51 **byte-gleich** (943/943 CSV); `vergleich` R51 → R52: 29 PASS, einziger Befund „Projekt_1065 nur
  im Vergleichslauf vorhanden“; 1065 mit 36 CSV und 209 Skalaren;
- zweiter Lauf gegen R52: GESAMT PASS, 979/979 CSV byte-gleich;
- gestörter Lauf (`--stoerung ulp`): GESAMT PASS, 934/979 CSV byte-gleich (R51: 899/943; 1065: 35/36, `heizstab.csv`
  innerhalb der Toleranz).

R51 ist verabschiedet: Ordner aus dem Repository, `protokoll.txt` nach
`Dokumentation/ueberholt/Referenzbasen/2026-10-10_R51_FreieKuehlung/`, Tabellenzeile und Abschnitt „Die Basis R51 im
Einzelnen“ im Wegweiser. Verweise nachgezogen in `CLAUDE.md`, `.github/workflows/kern.yml` (Basis, Schrittname, Projektliste
um 1065), `.github/workflows/ios.yml`, `Referenzlaeufe/LIESMICH.md`, `Dokumentation/LIESMICH.md`. Die Gate-Skripte finden die
Basis selbst. Bewusst gelassen: die Kopfzeilen der Kältemittel-Konzepte und der Übergaben (Momentaufnahmen mit Codestand),
die Nachträge der Schemaschritte 211 bis 215 in `Referenzlaeufe/LIESMICH.md` und der Hinweis im Entwurf, dass die Messungen
aus R51 stammen.

## Einfrierregel und CI

Neue Regel „gesäte Vorheizdaten eines Referenzprojekts“ in `CLAUDE.md` und `Referenzlaeufe/LIESMICH.md`. Die CI-Auswahl
umfasst 1065 (zwölf Projekte).

## Zusammenführung mit Schema 216

Nach dem Einfrieren hob `origin/ios_migration_september` (`bd3f4a283`) die Testdatenbank auf Schemaschritt 216
(KM4-E1, Kältemitteltabelle, `a63c8e78…`). Zusammengeführt: Testdatenbank in der Fassung von origin genommen und 1065 mit
demselben Skript erneut angelegt — 9 449 neue Zeilen in 30 Tabellen, keine geänderte Zeile, `integrity_check` ok,
`foreign_key_check` leer; **96 571 392 Byte, LFS-SHA-256 `aaaa17565a5454d892422cf4b1f1d60335d31ad1642ca78dad5a5b92714620db`**.
Übrige Dateien ohne Konflikt. Alle dreißig Projekte gegen R52: GESAMT PASS, 979/979 CSV byte-gleich — R52 bleibt wie
eingefroren (Nachtrag in `Referenzlaeufe/LIESMICH.md`).

## Prüfungen

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler (nach der Zusammenführung).
- `EPOS.Kern.Tests` mit Filter (alle Wachen, `*Vorheiz*`, `*Aufheiz*`, `*Konditionierung*`, `*Kaeltemittel*`,
  `*Rueckkuehlwerk*` und jede Testklasse, die 1051, den Konditionierungsbestand oder die Projektzahl berührt): 1 775
  bestanden, 0 rot, 1 übersprungen.
- `Werkzeuge/Auslieferungsvorlage`: 62 bestanden.
- `SqlDialektPruefer`: 2 821 SQL-Texte, 0 Fundstellen.
- Ein voller Lauf von `EPOS.Kern.Tests` vor der Zusammenführung blieb nach einer Stunde ohne roten Fall und wurde für die
  Zusammenführung abgebrochen; das volle Gate fährt die Orchestrierung.
