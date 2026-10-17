# Zapfprofil: Methoden als Bereiche, Tagtypen, Topologie-Ergebnis (W9, ZC5, ZC4 a/c, 17.10.2026)

Protokoll der Welle **W9** (ZC5, ZC4 a und c) des
[Konzepts Zapfprofil — Bereiche und Methoden](../../../aktuell/Konzept_Zapfprofil_Bereiche_und_Methoden_EPOS-Plan.md) (Statuszeile #1205).

**Rahmen.** Sitzung Zapfprofilgenerator Cloud, zwei Worktree-Agenten. Integration auf dem Arbeitszweig: Merges
`0134d2023` (W9a, Ressourcen vereinigt) und `e6f09e19c` (W9b, Fast-Forward), origin ohne Konflikt. Kein Schemaschritt,
Testdatenbank unverändert, Rechenweg der Simulation unverändert, Referenzbasis R58 bleibt. Annahme bis zum Anwenderentscheid: ZC‑Q5 A.

## 1 Auftrag

Aus dem Konzept: Die Eingaben der Stufe Experte sollen nach ihrer Wirkung auf den Bedarf gruppiert und benannt sein (ZC5);
der Kalender soll zeigen, wie er den Jahresbedarf auf Tagtypen verteilt (ZC4 a), und die Auslegung je Topologie ein
Ergebnis nennen (ZC4 c).

## 2 Umsetzung

**W9a — ZC5** (`5c7bcf6eb`, `73e100b90`, `e5a620593`). In Experte steht unter dem Block der Bereiche die Gruppe
„Methoden“ mit sechs Klappgruppen in fester Reihenfolge. Jede trägt im Kopf eine Wirkungsmarke als Pille, einen Satz
darunter und ist zugeklappt zusammengefasst; zuerst folgen die Felder des markierten Bereichs unter „Bereich „…““, dann
die des Projekts unter „Gebäude und Anlage“.

| Nr. | Gruppe | Marke | Inhalt |
|---|---|---|---|
| 1 | Bedarf | Summe | Wohnungstabelle, Personen/Fläche je WE, spezifischer Bedarf, Zapftemperatur, Tagesbedarf |
| 2 | Kalender | Verteilung | bisherige Karte |
| 3 | Messwert | Summe | bisherige Karte, Zusammenfassung „kalibriert auf den Jahresmesswert“ |
| 4 | Jahresreihe | Jahresreihe | Wahl Norm-Jahresgang / stochastisch / Typtage VDI 4655; Seed und Realisierungen (bei Norm und Typtagen mit der Zeile „Wirkt hier nur auf das Ensemble der Auslegung“); Klimazone und Gebäudeart nur bei Typtagen; Kaltwasser Mittel/Amplitude, Tagesgangsatz, Auslastung je Monat, Editoren |
| 5 | Zirkulation und Anlage | Simulation | Zirkulation ja/nein; Methode, Lage, Länge, Verlust, Anteil, Leistung, Kennwert, Fläche, Laufzeit, Leitungsinhalt |
| 6 | Auslegung | nur Auslegung | Topologie je Bereich; Ladeleistung, Ladefenster, Beginn, Kaltwasser und Speichertemperatur der Auslegung, Knopf „Auslegung…“ |

Die Karten Belegung, Tagesbedarf, Fachwerte, Fachwerte · Gebäude und die Gruppe „Gebäude und Anlage“ gehen ohne
Dopplung darin auf; die Detailzeile nennt nur noch Anteil und Rechenweg. Ohne eingespielte Typtage ist „Typtage“ weich
gesperrt; Einfach bleibt unverändert. 20 neue Schlüssel (`ZPG_GRP_METHODEN`, `ZPG_MTH_*`, `ZPG_WIRKUNG_*`,
`ZPG_MTH_SATZ_*`, `ZPG_OPT_NORM_JAHRESGANG`, `ZPG_OPT_TYPTAGE`, `ZPG_HINW_NUR_ENSEMBLE`); Stilblatt: Pille aus Tokens,
Kontrastmodus. Tests: `ZapfprofilDialogStufenTests.Methoden.cs` (5), umgestellt Stufen-, Karten-, Typtage-, Editoren-
und Dialog-Tests, `KiMaskenabdeckungWacheTests` mit 55 Eingaben.

**W9b — ZC4 (a) und (c)** (`e6f09e19c`). Kern: `Zapfkalender.Zaehlen` (Tage je Tagtyp) und `Zapfauswertung.Verteilung`
(Record `Tagtypverteilung`: Tage, ZapfungKwh, SummeKwh) zählen über den gerechneten Kalender je Bereich, keine neue
Rechnung. Die Gruppe Kalender nennt zugeklappt „… · 251 Werktage · 52 Samstage · 57 Sonn- und Feiertage“ (Ferientage nur
bei > 0), aufgeklappt unter dem Jahresraster „Der Kalender verteilt den Jahresbedarf um, er ändert die Summe nicht.“ und
die Verteilung in kWh mit Anteil je Tagtyp; ohne Vorschau leer. Auslegung: der Delegat `Auslegungskurz` rechnet einen
deterministischen Lauf (wie `Ladevorschlag`, rund 1 s bei 1071) und liefert je Topologiegruppe eine Kurzzeile unter der
Topologie-Klappliste („Speicher: Nenninhalt 300 l · Ladeleistung 12 kW“, „Frischwasserstation: Minutenspitze 48 kW“,
Wohnungsstation mit Summe und Kernhinweis, sonst „nicht rechenbar — Grund“), entprellt mit der Vorschau neu gerechnet,
auch nach Topologiewechsel; Zusammenfassung „Topologie · Hauptergebnis · Ladung“. `ZapfprofilAuslegung.cs` unverändert
(`AlsGruppe` setzt zusätzlich `Topologieart`). 16 neue Schlüssel (`ZPG_KAL_*`, `ZPG_AUSKURZ_*`).

## 3 Abnahme

- EPOS.UI.Tests vollständig 9781 grün (Stand W9a); neue Tests: `ZapfprofilDialogStufenTests.Methoden.cs` (5),
  `ZapfauswertungTagtypTests` (4), `ZapfprofilHuelleEditorenTests` (+3, darunter 1071 mit vier rechenbaren Gruppen),
  bunit Kalender (4) und `…Auslegung.cs` (3).
- Referenzprojekt-Wachen grün, Rechenweg unverändert.
- Dokumentationswachen (`DokumentationLinkWache`, `RepositoryOrdnungWache`) grün auf dem Stand dieses Protokolls.
- CI-Vermerk: offen.

## 4 Offen

- **Ferienmarke im Monatsbild:** nicht gebaut, braucht neue Zeichenelemente im Renderer.
- **Anwenderfrage:** In der Maske schließen sich stochastisch und Typtage aus (der Kern erlaubt beides; ein Altstand mit
  beidem zeigt Typtage plus Seed und Realisierungen). Soll der Ausschluss bleiben?
- **W10** (ZC4 b, ZC‑Q4) und **W12** (Wiki-Quelle und Logbuch: Methodengruppen, Tagtypzeile, Topologie-Ergebnis) offen.
- Windows-Sichtabnahme: drei Prüfpunkte in der Statusdatei, Abschnitt 4.1.
