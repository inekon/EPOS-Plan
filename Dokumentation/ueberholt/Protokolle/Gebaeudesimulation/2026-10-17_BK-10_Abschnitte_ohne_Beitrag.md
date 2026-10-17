# Protokoll BK-10 — Abschnitte ohne Beitrag

Auftrag BK-10, Nachzug zu [BK-9](2026-10-17_BK-9_Vorschauzaehlung_Teilkatalog.md) und
[BK-8b](2026-10-16_BK-8b_Normgerechte_Teilung.md). Arbeitsort Worktree `zaehl`, Zweig `zaehl`, Basis `61ee18040`. Kein
Schemaschritt, kein Rechenwegschritt; `InhomogeneSchichten.Pruefen` und der Dialogweg bleiben unverändert streng.

## Befund (nur Zählungen, Export des Anwenderkatalogs)

- 7 Aufbauten verworfen („inhomogen: Abschnitt ohne Flächenanteil“), je 8 Abschnitte als Streifen über 6 Schichtebenen
  (48 Schichten = 6 × 8).
- Die Abschnitte 1, 2, 7, 8 tragen Anteile, die zusammen 100 % ergeben; die Abschnitte 3 bis 6 haben den Anteil genau 0
  (nicht leer) und einen eigenen, anderen Schichtstapel.
- Im übrigen Katalog hat kein Aufbau einen Abschnitt mit Anteil 0 oder leer; 202 inhomogene Aufbauten werden übernommen.

## Normlage

DIN EN ISO 6946, 6.7.2: Ein Abschnitt geht in den oberen und den unteren Grenzwert des Wärmedurchlasswiderstands nur mit
seinem Flächenanteil ein. Ein Anteil von 0 trägt nichts bei; Weglassen ist rechengleich.

## Entscheid

Anwenderentscheid vom 17.10.2026: „Weglassen mit Protokoll“.

## Lösung

`BauteilkatalogImport.AbschnittePruefen` ruft vor der Anteilsprüfung `OhneBeitragWeglassen`: Haben alle Abschnitte eines
Aufbaus einen Anteil (keiner leer), sind einige genau 0 und ergeben die übrigen 1 ± `InhomogeneSchichten.ANTEIL_TOLERANZ`,
werden die Abschnitte mit Anteil 0 samt ihren Schichten aus dem Plan genommen. Je Aufbau eine Protokollzeile
(`BKI_PROT_OHNE_BEITRAG`, beide Sprachen), die Vorschau zählt die weggelassenen Abschnitte in `AbschnitteOhneBeitrag`.
Danach laufen Anteilsprüfung und Teilung (BK-8, BK-8b) auf den verbliebenen Abschnitten. Bleibt nur ein Abschnitt, wird der
Aufbau homogen (Schichten ohne Abschnitt, wie ein Aufbau der Quelle ohne Abschnitte) — rechengleich zu einem Abschnitt mit
Anteil 1.

Unverändert: ein einzelner leerer Anteil wird als Rest ergänzt, mehrere leere führen zur Ablehnung, Anteil 0 neben leerem
Anteil wird abgelehnt, eine Restsumme außerhalb der Toleranz wird abgelehnt. Ein einzelner Abschnitt mit Anteil 0, dessen
übrige mehr als die Toleranz offen lassen, bekommt wie bisher den Rest (Anteil 0 gilt dort als fehlend). Die Zählung der
Vorschau „Abschnitte neu“ bleibt eine Zählung der Quellzeilen (wie bei den Schichten); die Teilkatalog-Zählungen aus BK-9
gelten für die übernommenen Sätze.

Folge für die bestehende Probe „nur die Toleranz offen“ (99,5 % und 0): Sie ist jetzt ein Fall ohne Beitrag und wird
homogen übernommen; die Ablehnungsprobe nimmt dafür 99,5 % und leer.

Wiki-Quelle „Programm Dokumentation - Baustoffe und Bauteilaufbauten“ (Abschnitt Inhomogene Aufbauten) um die Regel ergänzt.

## Tests

In `EPOS.Kern.Tests/BauteilkatalogInhomogenTests` vier neue Fälle:

- vier Abschnitte × zwei Schichten, Anteile 0,5 / 0,5 / 0 / 0: übernommen mit 2 Abschnitten und 4 Schichten,
  `AbschnitteOhneBeitrag` 2, Protokollzeile, R'_T, R''_T und U gleich der Handrechnung ohne die Null-Abschnitte;
- 100 % und 0: homogen übernommen, U gleich der Handrechnung;
- 0 neben leer und 0 mit Restsumme 0,9: abgelehnt wie bisher;
- Wiederimport über die Testdatenbank: zweite Vorschau 0 neu, 1 wiedererkannt, 0 neu geteilt, 0 mit U-Änderung; Schichten,
  Abschnitte und U nach dem zweiten Lauf gleich.

Gegenprobe ohne Weglassen: 3 von 19 Tests der Klasse rot (die Ablehnungsprobe bleibt erwartungsgemäß grün).
`EPOS.Kern.Tests` mit Filter Bauteilkatalog/Teilkatalog/Bauteilaufbau/InhomogeneSchichten/Ressource: 201 von 201 grün;
`EPOS.UI.Tests` mit Filter Bauteilkatalog: 9 von 9 grün; `WP-Plan.Kern.slnf` baut mit 0 Fehlern; Ressourcen-Designer
wiederholbar. Keine Hülle berührt. Kein Referenzlauf: geändert ist der Importweg des Bauteilkatalogs, nicht der Rechenweg
der Simulation.

## Logbuch-Vorschlag

„Der Bauteilkatalog-Import übernimmt inhomogene Aufbauten, deren Abschnitte mit Flächenanteil 0 keinen Beitrag leisten,
ohne diese Abschnitte.“
