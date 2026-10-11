# Protokoll: Nachzug Zapfprofil-Rubrik im Einstellungsdialog

**Entscheid des Anwenders (11.10.2026, Konzept 3.4, AS-Q4):** Die fünf Rechenvorgaben des
Zapfprofilgenerators sind Anwenderrubrik „Zapfprofil“ im Einstellungsdialog statt Entwicklerbereich.
Statuszeile #976. Zweig `claude/gracious-brown-qx5kb2`. Kein Schemaschritt, kein Rechenweg.
Konzept:
[`Konzept_Administration_Support_EPOS-Plan.md`](../../../aktuell/Konzept_Administration_Support_EPOS-Plan.md),
Handbuch:
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](../../../aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md).

## 1 Welle

Eine Welle: Opus 5.5 im Worktree, Zweig `zapf-nachzug`.

- `ea267cc5` (Code, Ressourcen, Tests): Reiterblatt `ZAPFPROFIL` „Zapfprofil“ / „Draw-off profile“
  im Einstellungsdialog nach „Klimadaten“, vor „Diagramme“, immer sichtbar, fünf Felder und eine
  Hinweiszeile. Parameter `Entwickler` des Dialogs samt Durchreichung in `EinstellungenHuelle.cs`
  entfernt; `EinstellungenCtrl.ZapfprofilVorgabenSchreiben` ohne Bedingung; Register
  `Entwicklerbereiche` 9 → 8 (`ZAPFPROFIL_VORGABEN` entfernt); Ressourcen
  `ADM_SET_RUBRIK_ZAPFPROFIL` und `ADM_SET_HINT_ZAPFPROFIL` (die alten `…_ENTWICKLUNG` entfernt).
  Tests: `EntwicklerbereicheTests` 8, `EinstellungenDialogTests` neuer Test
  `Die_Rubrik_Zapfprofil_steht_immer_und_reicht_die_Werte_durch`, Reiterzahl 6 → 7,
  `EinstellungenBerichtTests` Reiterfolge.
- `4d1ec875`: Konzept 3.4/AS-Q4/Stand, Handbuch 2.1/Stand/7.
- Merge `c30269d2`.

Davor: Umnummerierung `b0ae1bf0` (#966–#969 → #970–#973) und Merge `34d17c82` von
`origin/ios_migration_september` (Referenzbasis R52).

## 2 Abweichung

Die englische Rubrik heißt „Draw-off profile“ (nicht wörtlich „Tapping profile“), passend zur
Terminologie der übrigen en-US-Ressourcen des Generators.

## 3 Merge mit Verstümmelung und Reparatur

Der Merge-Commit `c30269d2` hatte Konzept und Handbuch bei der Konfliktauflösung auf 12 bzw. 7 Zeilen
verstümmelt; nur der Konfliktblock war geblieben. Der nächste Merge `fbb0d2c1` baute beide Papiere
aus `34d17c82`, `4d1ec875` und `as3-ueber` neu auf (danach 581 und 323 Zeilen). Seither gilt bei
jeder Konfliktauflösung in Papieren die Pflichtprüfung von Zeilenzahl und Überschriftenliste.

## 4 Gate (Orchestrierung)

Das Gate lief am 11.10. im Hauptbaum nach dem Merge aller drei Wellen (`98b15126`), über den SDK-Umweg (10.0.112 statt 10.0.400) und mit der echten Testdatenbank:

| Prüfung | Ergebnis |
|---|---|
| `WP-Plan.Kern.slnf` Release | 0 Fehler |
| Windows-Schale auf Linux (`EnableWindowsTargeting`) | 0 Fehler |
| `EPOS.UI.Tests` | 8864 von 8864 |
| `KiKern.Tests` | 553 von 553 |
| `SpeicherEngine.Tests` | 397 von 397 |
| `SpeicherPlanung.Tests` | 27 von 28, 1 übersprungen (Bestand) |
| `EPOS.Kern.Tests` nach Filter (Supportpaket, Ausnahmeprotokoll, Einstellungen, KiDialog, Zapf, Ueber, Fernwartung, Lizenz, Menue, Hilfe, Ressource, Wache, Entwickler, Diagnosedaten, Dokumentation, Wiki, HelpMapping, Auslieferungskennzeichen) | 2479 von 2479 |

Der volle Kern-Lauf (rund 57 Minuten) ist die CI; Push-SHA und CI-Kennung stehen im Statuspunkt „Nach #976“ (e).

Zahlen des Agenten: `EPOS.UI.Tests` 8825/8825, Kern-Filter 469/469, Windows-Schale auf Linux
0 Fehler.

## 5 Offene Punkte

- Sichtabnahme der Rubrik „Zapfprofil“ unter Windows.
- Wiki (Einstellungen-Seite) und Logbuch-Satz: „Die Rechenvorgaben des Zapfprofilgenerators stehen
  im Einstellungsdialog in der Rubrik ‚Zapfprofil‘.“; Versionsnummer beim Anwender erfragen.
- Entscheid des Anwenders: Soll der Hilfe-Assistent die fünf Zapf-Felder kennen? Sie stehen nicht in
  der KI-Feldkarte.
- Push-SHA und CI-Kennung nach dem Push nachtragen.
