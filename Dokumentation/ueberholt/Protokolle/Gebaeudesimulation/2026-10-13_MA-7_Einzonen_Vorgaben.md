# Protokoll MA-7 — Einzonen-Vorgaben

Welle MA-7 aus dem [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
(Zielbild 2.1 „Zwei Wege“, Einzonenmodell Nr. 1 und 2; Entscheid E137 F4 (a); Wellenplan Abschnitt 4 Zeile MA-7). Leitziel:
Einzonenmodell in einer Minute — Baualtersklasse wählen, Vorgaben übernehmen, Profil der Gebäudeart übernehmen, OK. Muster für
Vorschlag, Übernahme und Rücknahme: [MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md), [MA-2](2026-10-13_MA-2_Nutzung_und_Kalender.md).

## Befund

- Die Vorgabetafel der Baualtersklasse steht im Kern schon: `GebaeudeVorgaben` (Import, Stufe C der Vorschlagsregeln über
  `Ersatzaufbau`) mit Median der Katalogbauten je Klasse A…M für fünf U-Werte, g-Wert und drei ψ, Vorrang des Energiestandards
  mit Katalogsätzen (E47) und dem freien Wert nach Stein/Loga 2025 als ruhendem Rückfall (E51). Sie ist `internal`; die
  Oberfläche erreicht sie nicht.
- Eine Bauart bzw. Bauweise je Klasse führt keine Tafel. Im Katalog (`Tab_Gebaeude_STAMM` der Testdatenbank, ohne den
  Referenzkatalogbau von 1051) ist die häufigste Bauart in allen dreizehn Klassen „schwer“ (gelesen über
  `Bauweise / Wohnfläche`, Schwellen 30 und 75).
- Der Gebäudeeditor ist `GebaeudeKatalogDialog` (Katalogbau und Projektkopie, Reiter „Gebäude und Hülle“); er arbeitet auf dem
  Arbeitsstand `GebaeudeArbeitsstand`, geschrieben wird mit OK. Die Baualtersklasse ist dort ein Index 0…12; eine leere Spalte
  wurde beim Laden stumm zu „A“.
- Das Profil der Gebäudeart kennt der Kern als Profilregel 2.4 Stufe 3 (`Zonenvorbesetzung.AusGebaeudeart`). Der Generator steht
  im Editor schon als „Nutzungsprofil übernehmen…“ der Konditionierung bereit (`KonditionierungBearbeitung.ProfilUebernehmenFragen`,
  in den Arbeitsstand, mit Rückfrage und Rücknahme der Konditionierung).

## Ergebnis

- **Kern** `EPOS.Kern/Allgemein/Import/Gebaeude/Einzonenvorgaben.cs` (öffentlich, rein, plattformfrei): `Einzonenvorgaben.Fuer(klasse,
  energiestandard)` liest `GebaeudeVorgaben.Fuer` (dieselbe Tafel wie Import und Stufe C) und ergänzt die Bauart aus
  `BAUART_JE_KLASSE`; `Vorschau` (alt → neu je Feld mit Vorgabe), `Anwenden`, `Zuruecknehmen` (Feld, das noch so steht wie nach
  der Übernahme, bekommt den Wert von davor — Muster MA-1a) und `Sperre` (Zonen vor fehlender Klasse vor Vorgabe ohne Werte).
  Zehn Felder: U Außenwand, Fenster, Dach, Bodenplatte, Sonstige, ψ Fenster–Wand, Außenwand–Keller, Wand–Dach,
  Fensterdurchlassgrad, Bauart.
- **Arbeitsstand** `GebaeudeArbeitsstand`: `Einzonenvorgabe`, `Einzonensperre`, `Einzonenvorschau`, `EinzonenvorgabenUebernehmen`
  (Bauart über den Weg der Klappliste, die Bauweise folgt), `EinzonenvorgabenZuruecknehmen` (eine Stufe, Bauweise und Nachführung
  von davor); `Laden` verwirft den Schritt. `GebaeudeKatalogDaten.KlasseGesetzt` hält eine beim Laden leere Klasse fest
  (`GebaeudeKatalogHuelle`); jede Wahl der Klasse oder ein Baujahr mit Klasse setzt sie.
- **Editor** `GebaeudeKatalogDialog`, Reiter „Gebäude und Hülle“ unter „Beschreibung und Einordnung“: Knopf „Vorgaben der
  Baualtersklasse übernehmen…“, gesperrt mit Grund daneben und im Titel (Zonen: „es gilt der Zonenweg“; keine Klasse), im
  Lesemodus gesperrt; nach einer Übernahme „Vorgaben zurücknehmen“. Das Blatt (Überlagerung) nennt die Herkunft („Vorgabe Klasse E:
  Median von n Katalogbauten“, Energiestandard oder freier Wert) und zeigt je Feld „Jetzt“ und „Vorgabe“ (unveränderte Felder als
  „unverändert“). „Übernehmen“ trägt in den Arbeitsstand, die Meldung nennt die Zahl der geänderten Felder; Flächen, Sollwerte und
  Kalender bleiben. Gruppe „Profil der Gebäudeart“: nennt das Profil der Regel und übernimmt es über „Profil übernehmen…“ — wählt
  das Profil in der Konditionierung, stellt deren Rückfrage und wechselt in den Reiter „Konditionierung“; ohne Regel oder ohne
  Profilweg steht der Grund.
- **Naht** `KonditionierungWeg.ProfilDerGebaeudeart` (`KonditionierungHuelle`: `Zonenvorbesetzung.AusGebaeudeart` über
  `Raumnutzungsvorbelegung.Lesen`, nur wenn der Profilweg besteht; `ReinerWeg` nimmt eine Regel des Prüfstands). Kein neuer
  Generator, kein neues SQL.
- **Texte** 23 Schlüssel `GEBV_*` in `Resource.resx` und `Resource.en-US.resx`, Designer neu erzeugt (zweiter Lauf +0). Kein neues
  Blatt im Sinne der Infoknopf- und KI-Wachen (Überlagerung im bestehenden Editor). CSS `epos-gebk-vorgaben*` vor dem
  Formularraster.

Keine Leseregel des Rechenwegs geändert, kein Schemaschritt; bestehende Gebäude und Referenzprojekte werden nur auf Klick
beschrieben. Der Referenzlauf ist davon nicht berührt (Orchestrierung fährt ihn).

## Die Tafel und ihre Herkunft

| Größe | Quelle | Herkunft |
|---|---|---|
| U der fünf Hüllgruppen, g, drei ψ | `GebaeudeVorgaben` (Bestand) | Median der Katalogbauten der Klasse in `Tab_Gebaeude_STAMM` der Testdatenbank; Energiestandard mit Katalogsätzen geht vor (E47); ohne Katalogsatz der freie Wert nach Stein/Loga 2025, CC BY 4.0 (E51, ruht) |
| Bauart je Klasse A…M | `Einzonenvorgaben.BAUART_JE_KLASSE` (neu) | häufigste Bauart der Katalogbauten der Klasse (über `Bauweise / Wohnfläche`); in allen Klassen „schwer“; bei Gleichstand die schwerere |

Keine Normwerte, keine Herstellerdaten. `BaualterEinzonenvorgabenTests` rechnet die Bauart-Tafel aus der Testdatenbank nach.

## Dateien

- neu: `EPOS.Kern/Allgemein/Import/Gebaeude/Einzonenvorgaben.cs`, `EPOS.Kern.Tests/BaualterEinzonenvorgabenTests.cs`,
  `EPOS.UI.Tests/Dialoge/GebaeudeEinzonenvorgabenTests.cs`
- geändert: `EPOS.UI/Dialoge/Bedarf/GebaeudeArbeitsstand.cs`, `GebaeudeKatalogDaten.cs`, `GebaeudeKatalogDialog.razor`,
  `KonditionierungWeg.cs`; `EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs`, `KonditionierungHuelle.cs`;
  `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`; `EPOS.UI/wwwroot/epos-ui.css`

## Tests

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Windows-Schale (`EnableWindowsTargeting=true`): 0 Fehler.
- `EPOS.Kern.Tests`, Filter Baualter, Modellpruefung, GebaeudeRueckweg, Dokumentation, Repository, Resource, KiKnopf, Infoknopf,
  KiDialog, GebaeudeVorgaben: 174 bestanden, 0 rot (darin `BaualterEinzonenvorgabenTests` 6: Bauart-Tafel gegen den Katalog,
  Vorgabe gleich Import-Tafel, Vorrang Energiestandard, Vorschau alt/neu, Anwenden und Rücknahme mit Handänderung, Sperre).
- `EPOS.UI.Tests`, Filter GebaeudeEinzonenvorgaben, GebaeudeKatalogDialog, GebaeudeDialog, NutzungsprofilUebernahme,
  GebaeudeKatalogZonen: 308 bestanden, 0 rot (darin `GebaeudeEinzonenvorgabenTests` 7: Knopf und Vorschau ohne Änderung, Übernahme samt
  Bauweise, Rücknahme, Sperre ohne Klasse, Sperre mit Zonen, Profil der Gebäudeart über die Rückfrage der Konditionierung, Grund
  ohne Profilregel).
- SQL-Dialekt-Prüfer: 2922 SQL-Texte, 0 Fundstellen (kein neues SQL).

## Offene Punkte

- Eine leere Baualtersklasse bleibt nur bis zum ersten Speichern erkennbar: Der Schreibweg setzt weiterhin den Buchstaben der
  Klappliste („A“). Eine echte Wahl „keine Klasse“ in der Klappliste wäre eine eigene Änderung.
- Die Bauart-Tafel ist im Bestandskatalog überall „schwer“; sie trägt erst, wenn der Katalog leichte oder sehr schwere Klassen
  führt. Das Gebäudestammblatt der Gebäudeverwaltung (`GebaeudeStammblattFelder`) trägt den Knopf nicht — der Einzonenweg läuft
  über den Editor.
- Wiki „Gebäude“: Abschnitt zum Knopf mit der Welle Wiki/Logbuch des Entwurfs.

## Logbuch-Vorschlag

„Im Gebäudeeditor übernimmt ‚Vorgaben der Baualtersklasse übernehmen…‘ U-Werte, Wärmebrücken, Fensterdurchlassgrad, Bauart und
das Nutzungsprofil der Gebäudeart nach Vorschau mit einem Klick.“
