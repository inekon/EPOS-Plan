# Protokoll MA-1a — Modellprüfung, Rechenstatus und Vorschlagsregeln (Kern)

Welle MA-1a aus dem [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
(Abschnitte 2.2 bis 2.4, Wellenplan Zeile MA-1, Kernhälfte). Grundlage ist Entscheid E137 (13.10.2026, nach Empfehlung):
F1 Ampel je Prüfgruppe mit Zählung und Rechenstatus, F2 „Alle Vorschläge übernehmen“ und je Gruppe mit Rücknahme, F3
Aufbaurangfolge, F5 Ferien und Saison je Projekt, F6 Profilregel. Die Oberfläche (Ampelleiste) baut MA-1b auf der
Schnittstelle unten.

## Befund

Rechenfähigkeit wurde bisher nur an einer Stelle geprüft: `GebaeudeZonenCtrl.Pruefen` lehnt beim Schreiben ab, der Lauf meldet
`GebaeudeModellFehler`. Was ein Modell mit Annahmen rechnet (Bauteil nur mit U, geerbte Kalender, fehlende Heizperiode), sah
der Anwender nicht, und einen Vorschlag mit Übernahme gab es nur im Importvorschlag (Ersatzaufbau, Zonenvorbesetzung).

## Ergebnis

Neuer Baustein `EPOS.Kern/Allgemein/Simulation/Gebaeude/Modellpruefung/` und Controller `ModellpruefungCtrl`. Der Controller
liest einen `Modellpruefstand` (reine Daten), `Modellpruefung.Pruefen` prüft ihn in fünf Gruppen und ruft den Bestand
(`GebaeudeZonenCtrl.Pruefen`, `BrauchtAzimut`, `LeerHeisstInnen`, `Ersatzaufbau`, `Zonenvorbesetzung`,
`Raumnutzungsvorbelegung`, `BauteilaufbauCtrl.Kennwerte`). Ampel der Gruppe = schärfster Befund; Rechenstatus rot, sobald eine
Gruppe rot ist, gelb, sobald eine gelb ist, sonst „vollständig“.

### Prüfgruppen und Befundcodes

| Gruppe | Befundcode | Ampel | Vorschlag |
|---|---|---|---|
| 1 Geometrie und Bauteile | `ZONE_OHNE_BAUTEILE` | rot | — |
| | `ZONE_OHNE_AUSSENBAUTEIL` (beheizte Zone) | gelb | — (MA-3) |
| | `HUELLE_OHNE_AUSSENWAND`, `HUELLE_OHNE_DACH`, `HUELLE_OHNE_BODEN` (je Gebäude) | gelb | — (MA-3) |
| | `BAUTEILART_UNBEKANNT` (Art unbekannt oder `SONSTIGES`) | gelb | — |
| | `FLAECHE_FEHLT` | rot | — |
| | `RANDBEDINGUNG_FEHLT` (leer, wo leer nicht „innen“ heißt) | gelb | — |
| | `AZIMUT_FEHLT` (`BrauchtAzimut`) | rot | — |
| | `EINZONE_FLAECHE_FEHLT`, `EINZONE_U_FEHLT` (Klassenweg: Außenwand, Fenster, Dach, Grundfläche) | gelb / rot | — |
| 2 Aufbauten und Stoffwerte | `NUR_U_WERT` (opakes Hüllbauteil, `Ersatzaufbau.Erhaelt`) | gelb | Aufbau F3 |
| | `OHNE_U_OHNE_AUFBAU` | rot | Aufbau F3 (opak) |
| | `AUFBAU_UNGEEIGNET` (`Simulation_Ungeeignet`) | gelb | — |
| | `SCHICHT_OHNE_STOFFWERT` (λ, ρ oder c fehlt) | gelb, ohne U rot | — (MA-4) |
| | `KAPAZITAET_FEHLT` | gelb | — |
| | `EINZONE_BAUWEISE_FEHLT` | gelb | — |
| 3 Nutzung und Konditionierung | `ZONE_OHNE_PROFIL` (beheizt, ohne Nutzung und ohne Kalender) | gelb | Profil F6 |
| | `HEIZKALENDER_GEERBT`, `LUEFTUNG_GEERBT` | gelb | — |
| | `UNBEHEIZT_MIT_HEIZKALENDER` | gelb | — |
| | `KUEHLKALENDER_FEHLT` (Kühlung aktiv, weder Kalender noch Sollwert) | gelb | — |
| | `FERIEN_FEHLEN`, `HEIZPERIODE_FEHLT` (nur bei Kalendern eines Profils) | gelb | Vorgabezeilen eines anderen Gebäudes des Projekts (F5) |
| 4 Ort und Lage | `KLIMAREGION_FEHLT` | rot | — (Pflichteingabe) |
| | `NORDRICHTUNG_ANGENOMMEN` (Herkunft `ANNAHME` der jüngsten Importquelle) | gelb | — (Bestätigung, MA-1b) |
| | `FEIERTAGSLAND_FEHLT` (nur mit Kalendern) | gelb | Feiertagsland eines anderen Gebäudes des Projekts |
| 5 Rechenfähigkeit | `PFLICHTGROESSE_FEHLT` (Nutzfläche, Raumhöhe, Fläche je Nutzer, Luftwechselrate) | rot | — |
| | `RECHENWEG_ABGELEHNT` (Text von `GebaeudeZonenCtrl.Pruefen` samt Luftströmen) | rot | — |

31 Codes; je Code ein Text `MP_BEFUND_<Code>`.

### Vorschlagsregeln

- **Aufbau (F3):** (1) Projektaufbau gleicher Bauteilart und Randbedingung mit der meisten Fläche im Gebäude, bei Gleichstand
  der mit dem nächsten U; (2) geeigneter Katalogaufbau gleicher Bauteilart, nächstes U, mit U nur im Band ±25 %
  (`KATALOG_U_BAND`), ohne U in Katalogfolge; (3) Typaufbau nach `Ersatzaufbau.Bilden`, auf das U abgeglichen (nur mit U). Das U
  des Bauteils bleibt (E95 Nr. 1), der Aufbau liefert Schichten und Kapazität. Ungeeignete Aufbauten und solche mit Schicht ohne
  Stoffwert scheiden aus.
- **Profil (F6):** DIN-Nummer → Raumtyp der Quelle (`HOTTCAD_RAUMTYP`) bzw. Nutzungsklasse (`IFC_KLASSE`) → Gebäudeart
  (`Zonenvorbesetzung`) → EPOS-Muster „Wohnen“ (Wohngebäude) bzw. „Sonstige“. DIN-Nummer und Raumtyp kennt nur der Import; der
  Aufrufer reicht sie mit `QuelleSetzen` herein. Die Kalender legt `ZonenplanCtrl.NutzungUebernehmen` an (Generator).
- **Ferien und Saison (F5):** Vorgabezeilen `FERIEN` bzw. `SAISON` eines anderen Gebäudes des Projekts; fehlen sie dort, bleibt
  der Befund ohne Vorschlag (es gilt die Vorgabe: keine Ferien, Heizen ganzjährig nach Kalender).
- **Ort und Lage:** Feiertagsland eines anderen Gebäudes des Projekts; Klimaregion und Nordrichtung ohne Vorschlag.

### Übernahme und Rücknahme

`Uebernehmen(gruppe)` und `AlleUebernehmen()` lesen den Stand neu, führen alle Vorschläge einer Gruppe in einem Vorgang samt
`Vorgangsklammer` aus (Projektaufbau zuweisen, `CopyFromStamm`, `ProjektaufbauEinfuegen` für den Typaufbau — ein Aufbau je
Bauteilart, Typ und U —, Profil über den Generator, Vorgabezeilen kopieren, Feiertagsland) und prüfen danach neu. Je Gruppe merkt
sich die Instanz die letzte Übernahme; `Zuruecknehmen(gruppe)` stellt `ID_Aufbau`, `Nutzungsprofil` und `Feiertagsland` zurück
und löscht angelegte Aufbauten, Kalender, Wochen und Vorgabezeilen. Kopierte Katalogstoffe bleiben im Projekt (ungenutzt).
Befunde ohne Vorschlag erscheinen in `Verworfen` mit dem Grund „Eingabe nötig“.

### Schnittstelle für MA-1b

| Typ | Art | Inhalt |
|---|---|---|
| `ModellpruefungCtrl` | Klasse | `new(idGebaeude)`, `Pruefen()`, `Uebernehmen(Pruefgruppe)`, `AlleUebernehmen()`, `Zuruecknehmen(Pruefgruppe)`, `KannZuruecknehmen(Pruefgruppe)`, `QuelleSetzen(idZone, din, raumtyp, klasse)`, `Gruppenname(Pruefgruppe)`, `Statusname(Rechenstatus)` |
| `Modellpruefergebnis` | record | `IdGebaeude`, `Mehrzonen`, `Status`, `Gruppen`, `Gruppe(g)`, `Befunde` |
| `Pruefgruppenergebnis` | record | `Gruppe`, `Ampel`, `Befunde`, `Sprungziel`, `Anzahl`, `Vorschlaege`, `Flaeche`, `JeCode` |
| `Modellbefund` | record | `Gruppe`, `Code`, `Ampel`, `Text`, `IdZone`, `IdBauteil`, `Flaeche`, `Vorschlag` |
| `Pruefvorschlag` | record | `Art`, `Herkunft`, `Text`, `IdAufbau`, `Typaufbau`, `IdProfil`, `Name`, `IdQuelle`, `U` |
| `Uebernahmeergebnis` | record | `Ok`, `Meldung`, `Gruppen`, `Uebernommen`, `Verworfen`, `RuecknahmeMoeglich`, `Neu` |
| `Pruefgruppe`, `Pruefampel`, `Rechenstatus` | enum | fünf Gruppen; Grün/Gelb/Rot; Vollständig/RechnetMitVorgaben/NichtRechenfaehig |
| `Pruefsprungziel` | enum | Zonen, Randbedingung, Aufbau, Befund (Gruppe 1 → Randbedingung, 2 → Aufbau, 3 → Zonen, 4 und 5 → Befund) |
| `Vorschlagsart`, `Vorschlagsherkunft` | enum | Aufbau, Profil, FerienSaison, Feiertagsland; Projektaufbau, Katalogaufbau, Typaufbau, DinNummer, Raumtyp, Gebaeudeart, EposMuster, Projekt |

Alle Ergebnistypen sind reine Daten ohne Datenbankbindung.

## Dateien

- neu: `EPOS.Kern/Allgemein/Simulation/Gebaeude/Modellpruefung/Modellpruefungstypen.cs`, `Modellpruefung.cs`,
  `Modellvorschlagsregeln.cs`; `EPOS.Kern/Controller/ModellpruefungCtrl.cs`; `EPOS.Kern.Tests/ModellpruefungTests.cs`
- geändert: `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` (je 65 Schlüssel `MP_*`), `Resource.Designer.cs`
  (Designer, wiederholbar)

Kein Schemaschritt, kein Rechenwegbaustein angefasst, Basis R52 unberührt.

## Nachweis

| Projekt | Status | Gruppe 1 | Gruppe 2 | Gruppe 3 | Gruppe 4 | Gruppe 5 |
|---|---|---|---|---|---|---|
| 1007 (Einzone) | vollständig | 0 | 0 | 0 | 0 | 0 |
| 1052 (Zonen) | rechnet mit Vorgaben | gelb 8 (`BAUTEILART_UNBEKANNT`) | gelb 12 (`NUR_U_WERT`, 12 Vorschläge) | gelb 4 (`LUEFTUNG_GEERBT` 2, `FERIEN_FEHLEN`, `HEIZPERIODE_FEHLT`) | gelb 1 (`FEIERTAGSLAND_FEHLT`) | grün |
| 1054 (Zonen) | rechnet mit Vorgaben | wie 1052 | wie 1052 | wie 1052 | wie 1052 | grün |

Nach „Alle übernehmen“ (Arbeitskopie) trägt 1054 in Gruppe 2 keinen Befund mehr; der Status bleibt „rechnet mit Vorgaben“.

Tests `ModellpruefungDatenbankTests` (7) und `ModellpruefungRegelTests` (6): Referenzprojekte nie rot, nur-U-Bauteil →
Katalog- oder Typaufbau im U-Band mit Rücknahme, fehlendes Außenbauteil → Projektaufbau mit der meisten Fläche, Zone ohne Profil
→ EPOS-Muster mit Rücknahme, Alle übernehmen, Gruppenampeln, Aufbau- und Profilrangfolge, Texte beider Sprachen. SQL-Prüfer
0 Fundstellen.

## Offen für MA-1b und später

- Ampelleiste im Importdialog und im Reiter „Zonen“ (MA-1b) auf `ModellpruefungCtrl`; der Importdialog reicht DIN-Nummer und
  Raumtyp je Zone mit `QuelleSetzen` herein. Die Prüfung läuft auf dem gespeicherten Gebäude; für den Importvorschlag vor OK
  braucht MA-1b einen `Modellpruefstand` aus `GebaeudeBauteilvorschlag` (Leser fehlt noch).
- Nordrichtung bestätigen und Klimaregion wählen sind Pflichteingaben ohne Vorschlag; ihr Knopf gehört in MA-1b.
- Hülle geschlossen und fehlendes Hüllbauteil aus der Körperfläche: MA-3. Die Hüllprüfung steht hier je Gebäude, nicht je Zone
  (Zwischengeschosse haben weder Dach noch Boden).
- Schicht ohne Stoffwert → Namensabgleich und Baualtersklasse für Stufe C: MA-4.
- Ferien und Heizperiode ohne Quelle im Projekt bleiben gelb; „Vorgabe bestätigen“ ließe sich ohne Feld nicht halten und ist
  verworfen. `BAUTEILART_UNBEKANNT` meldet auch `SONSTIGES` der Referenzprojekte.
- Die Rücknahme gilt für die Sitzung (Instanz des Controllers).
