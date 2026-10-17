# Protokoll BK-11 — Übernahme wiedererkannter Aufbauten

Auftrag BK-11, Nachzug zu [BK-10](2026-10-17_BK-10_Abschnitte_ohne_Beitrag.md) und
[BK-9](2026-10-17_BK-9_Vorschauzaehlung_Teilkatalog.md). Arbeitsort Worktree `bk11`, Zweig `bk11`, Basis `1a51e2e61`.
Kein Schemaschritt, kein Rechenwegschritt, kein Referenzlauf.

## Befund (Vorschau gegen die produktive Datenbank, nur Zählungen)

- Baustoffe: 0 neu, 10 zusammengeführt, 7 364 wiedererkannt, 77 abgelehnt.
- Aufbauten: 0 neu, 8 427 wiedererkannt, 0 abgelehnt; Schichten 9 296, Abschnitte 538 wiedererkannt.
- Aufbauten mit Teilkatalog: 7 913 in der Quelle, 7 906 in der Datenbank (Abweichung 7).
- `AbschnitteOhneBeitrag` 28 (7 Aufbauten × 4), Wiederimport 0/0/0/0.
- Die 7 Aufbauten stehen aus einem Importlauf vor BK-8 im Katalog: je 8 Abschnitte (davon 4 mit Flächenanteil 0) und
  48 Schichten, `Quelle` ohne Teilkatalog. Die Übernahmen mit BK-8 und BK-9 haben sie als „inhomogen: Abschnitt ohne
  Flächenanteil“ verworfen; der Katalogsatz blieb deshalb unverändert.

## Ursache

- **Übernahme:** richtig. Ein wiedererkannter Aufbau mit Herkunft `IMPORT` (Aktion `AKTUALISIEREN`) bekommt den Kopf neu
  (samt `Quelle` mit Teilkatalog), seine Schichten und Abschnitte werden gelöscht und aus dem Plan neu geschrieben
  (`BauteilkatalogImport.Schreiben`, `KopfSchreiben`). Ein Satz anderer Herkunft (`ERGAENZEN`) behält Bezeichner, Schichten
  und Abschnitte; nur leere Kopffelder werden gefüllt und die Quelle angehängt. Das ist der Schutz von Hand-Änderungen:
  wer einen Importsatz ändern will, legt eine eigene Kopie an (Herkunft `EIGEN`).
- **Vorschau:** zählte falsch. `OhneBeitragWeglassen` nahm Abschnitte mit Anteil 0 samt Schichten aus dem Plan, die
  Zählung „wiedererkannt“ (bei neuen Aufbauten „neu“) blieb aber stehen; ebenso der letzte Abschnitt eines homogen
  gewordenen Aufbaus. 538 Abschnitte enthielten die 28 weggelassenen, 9 296 Schichten die 168 weggelassenen (28 × 6).
- **Keine Zählung „aktualisiert“:** Die Vorschau unterschied nicht zwischen wiedererkannten Aufbauten, die die Übernahme
  ändert, und solchen, die gleich bleiben. Dass die 7 Aufbauten nachgezogen werden, war nicht zu sehen.

## Lösung

- `OhneBeitragWeglassen` und der homogene Rest in `AbschnittePruefen` ziehen die weggelassenen Abschnitte und Schichten
  von „neu“ bzw. „wiedererkannt“ ab (`Abziehen`). Vorschau und Übernahme zählen dieselben Sätze.
- Neue Zählung `BauteilkatalogVorschau.AufbautenAktualisiert` (`AktualisierteZaehlen`, nach `AbschnittePruefen`):
  ein Importsatz zählt, wenn `Quelle` (samt Teilkatalog), Bezeichner, Schichtzahl, Abschnittzahl oder Gesamtdicke der
  Schichten (auf 1e-6 m) vom Katalog abweichen; ein Satz anderer Herkunft, wenn seine Quelle angehängt wird.
  `StandLesen` liest dafür je Aufbau Schichtzahl, Gesamtdicke und Abschnittzahl (zwei `GROUP BY`-Abfragen).
  Andere Kopffelder (etwa `U_Quelle`) gehen nicht in den Vergleich ein.
- Ressource `BKI_AKTUALISIERT` in beiden Sprachen; die Hülle setzt sie, sobald es wiedererkannte Aufbauten gibt, der
  Importdialog zeigt sie über den Vorschauzahlen (`p.epos-bki-aktualisiert`).
- Der Schreibweg ist unverändert.

## Tests

- `BauteilkatalogInhomogenTests.Ein_wiedererkannter_Importaufbau_wird_ersetzt_bekommt_den_Teilkatalog_und_konvergiert`:
  Altbestand nachgebildet (Import ohne Teilkatalog, dazu zwei Abschnitte mit Anteil 0 und vier Schichten per SQL),
  Quelle mit Teilkatalog über `Katalog_Zuordnung.csv`. Vorschau: 1 wiedererkannt, 1 aktualisiert, Abschnitte 2 und
  Schichten 4 wiedererkannt. Übernahme: 2 Abschnitte, 4 Schichten, `Quelle` endet auf „, Teilkatalog Teil A“. Zweite
  Vorschau: 0 neu, 0 zusammengeführt, 0 aktualisiert, Teilkatalog in Quelle und Katalog gleich.
- `…Ein_eigener_Aufbau_mit_derselben_Quellkennung_behaelt_seine_Schichten_und_bekommt_nur_den_Teilkatalog`: derselbe
  Altbestand mit Herkunft `EIGEN` behält 4 Abschnitte und 8 Schichten, bekommt den Teilkatalog, zweite Vorschau 0
  aktualisiert.
- `BauteilkatalogImportDialogTests.Die_Vorschau_zeigt_die_Zahl_der_aktualisierten_Aufbauten_nur_wenn_die_Huelle_sie_nennt`.
- Gelaufen: `EPOS.Kern.Tests` Filter `Bauteilkatalog` 92 bestanden, `EPOS.UI.Tests` Filter `BauteilkatalogImport`
  10 bestanden; Kernfilter und Windows-Schale bauen mit 0 Fehlern; SQL-Dialekt-Prüfer 0 Fundstellen.

## Erwartete Zahlen der lokalen Übernahme

Vorschau vor der Übernahme:

- Baustoffe unverändert: 0 neu, 10 zusammengeführt, 7 364 wiedererkannt, 77 abgelehnt.
- Aufbauten: 0 neu, 8 427 wiedererkannt, 0 abgelehnt; aktualisiert 7 (mehr nur, wenn weitere Sätze vom Katalog abweichen).
- Abschnitte 510 wiedererkannt (538 − 28), Schichten 9 128 wiedererkannt (9 296 − 168).
- `AbschnitteOhneBeitrag` 28, Wiederimport 0/0/0/0, Aufbauten mit Teilkatalog in der Quelle 7 913.

Nach der Übernahme im Katalog: die 7 Aufbauten mit je 4 Abschnitten und 24 Schichten, `Quelle` mit Teilkatalog;
7 913 Aufbauten mit Teilkatalog.

Zweite Vorschau: 0 neu, Aufbauten 0 zusammengeführt, aktualisiert 0, Teilkatalog-Abweichung 0; Abschnitte 510,
Schichten 9 128 wiedererkannt. `AbschnitteOhneBeitrag` bleibt 28 (eine Eigenschaft der Quelle), die 10 zusammengeführten
Baustoffe ebenso (Dublettenregel, stabil seit BK-3d).
