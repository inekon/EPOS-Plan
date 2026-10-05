# Konzept: Nutzungsprofile — Kataloge, Kategorien und Zuordnungen (Stufe NP)

> **Rev. 1, 05.10.2026.** Grundlage ist der Entscheid **E90 (Q37)** des Anwenders vom 05.10.2026 („Weg 3 erweitert“): frei
> definierbare Nutzungsprofile als Katalog mit Katalogkategorien (EPOS‑Muster, DIN V 18599‑10, SIA 2024, VDI 2078, eigene Kataloge),
> dazu eine änderbare Zuordnung der DIN‑Profilnummern und IFC‑Nutzungsklassen auf Profile, Zugang aus Gebäudedialog, Zonenbaum und
> Zonendialog. Normwerte werden weiterhin nicht ausgeliefert (Konditionierungskonzept B15, P4): die Kategorien liefern Struktur und
> Nummern, Werte trägt der Anwender ein oder importiert sie. Die feste Liste `DbWerte.KOND_NUTZUNGEN` verschwindet aus dem Kern.
> Das Papier ist die Arbeitsgrundlage der Wellen NP0–NP3; umgesetzt wandert es nach `ueberholt/`.

## 0. Das Ergebnis in acht Punkten

1. **Zwei Kataloge** statt einer Konstante: „Nutzungskategorien“ (`Tab_Nutzungskategorie_STAMM`) und „Nutzungsprofile“
   (`Tab_Nutzungsprofil_STAMM`). Ein Profil gehört zu genau einer Kategorie und trägt Nummer, Name und Beschreibung — Struktur, keine
   Normwerte.
2. **Die Werte eines Profils sind Konditionierungsvorlagen je Größe** (`Z_Nutzungsprofil_Vorlage`). Ein Profil ohne Vorlagen ist
   erlaubt; der Anwender füllt es über „Als Vorlage speichern“ an der Kalenderkarte oder über den Import einer Projektdatei.
3. **Ausgeliefert** werden die Kategorie „EPOS‑Muster“ mit den vier bisherigen Nutzungen (Wohnen, Büro, Schule, Sonstige, mit den
   heutigen ausgelieferten Vorlagen als Werten) und die Kategorien DIN V 18599‑10, SIA 2024 und VDI 2078 mit Nummern und Kurznamen
   ihrer Profile, ohne Werte. Eigene Kategorien und Profile legt der Anwender an.
4. **Verweise statt Textspalten:** Vorlage, Kalenderkopie, Zone und Gebäude verweisen über `ID_Nutzungsprofil` auf ein Profil; die
   Textspalten `Nutzung` und ihre CHECK‑Bedingungen entfallen mit Umschlüsselung im Schemaschritt.
5. **Zuordnungen als Daten:** `Z_Nutzungsquelle` bildet DIN‑Profilnummern, IFC‑Nutzungsklassen und Raumtypen auf Profile ab, gesät mit
   der heutigen festen Abbildung, im Katalogdialog pflegbar; die Importleser lesen die Tabelle.
6. **Zugang:** Katalogdialog „Nutzungsprofile“ im Katalogmenü; Feld „Nutzungsprofil“ im Gebäudedialog, im Zonendialog und als
   Klappliste im Zonenbaum des Imports, jeweils mit Sprung in den Katalog.
7. **Eine Quelle:** Pufferauslegung und Zapf‑Nutzungsarten lesen das Profil (`Zapf_Nutzungsart`), nicht mehr den Text.
8. **Ergebnisneutral:** Der Rechenweg liest kein Profil, nur die Kalender; die Basis R38 bleibt byte‑gleich.

## 1. Auftrag, Einordnung, Befund heute

**Auftrag.** Entscheid E90 (Statusdatei Gebäudesimulation). Anlass: Im Zonenbaum des Gebäudeimports (HC‑3, #736) wählt der Anwender je
Zone eine „Nutzung“ aus drei festen Werten, die beim HottCAD‑Import ohne Wirkung bleiben und sich nicht ändern lassen.

**Befund heute** (Stand `b9c920768`):

| Stelle | Datei | Rolle |
|---|---|---|
| feste Werteliste | `EPOS.Kern/Allgemein/DbWerte.cs` (`KOND_NUTZUNG_WOHNEN/BUERO/SCHULE/SONSTIGE`, `KOND_NUTZUNGEN`) | einzige Quelle; CHECK‑Bedingungen im Schema |
| Vorlage | `Allgemein/Update/KonditionierungVorlagenSchema.cs` (`Tab_Konditionierungsvorlage_STAMM.Nutzung` TEXT, CHECK), `KonditionierungsvorlagenSaat.cs`, `Controller/KonditionierungsvorlageCtrl.cs`; gewählt an der Kalenderkarte („Als Vorlage speichern“) und in `EPOS.UI/Dialoge/Bedarf/KonditionierungVorlagenverwaltung.razor` | Nutzung je Vorlage |
| Kalenderkopie | `KonditionierungNutzungSchema.cs` (Schritt 176: `Tab_Konditionierungskalender.Nutzung`), `Controller/KonditionierungCtrl.cs` (`NutzungDerHerkunft`, `NutzungSetzen`) | „Vorlage übernehmen“ kopiert die Nutzung an den Kalender |
| Zone | keine Spalte an `Tab_Zone` (`ZonenSchema.cs`); Zonenplan des Imports `Allgemein/Import/Gebaeude/Zonenplan.cs` (`Freizone.Nutzung`), `GebaeudeZonierung.cs` (Regel Z6: elf Nutzungsklassen aus Raumtypen `mrt*`, Zone → Nutzung der flächengrößten Klasse), `Controller/ZonenplanCtrl.cs` (`NutzungUebernehmen`: je Größe die erste ausgelieferte Vorlage der Nutzung als Kalenderkopie) | die Zone „hat“ eine Nutzung nur über ihre Kalenderkopien |
| Import | `Allgemein/Import/Ifc/IfcAbbildBauer.cs` (`EPOS_Zone.Nutzung`), `Allgemein/Import/Sqproj/Din18599Nutzung.cs` (Profilnummer → Nutzung, fest: 1–5 Büro, 8/9/28/29 Schule, 70/71 Wohnen, sonst keine), `SqprojAbbild.cs` | Vorbelegung im Zuordnungsdialog |
| Oberfläche | `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor` (Spalte und Klappliste), `GebaeudeImportDaten.cs`, `EPOS.UI.Daten/Bedarf/GebaeudeImportZonen.cs`, `KonditionierungHuelle.cs`; Texte `KOND_LBL_NUTZUNG_*`, `GIMP_NUTZUNG_<KLASSE>` | Klappliste „keine, Wohnen, Büro, Schule“ |
| weitere Leser | `Controller/PufferAuslegungCtrl.cs` (Vorbelegung des Pufferprofils aus der Nutzung), Schema 178 `ProzessNutzungSchema.cs` (`Z_Nutzungsprofil`, Zapf‑Nutzungsarten Büro/Schule/Gewerbe — Muster einer pflegbaren Zuordnungstabelle), `Simulation/Gebaeude/Vdi6007Rechenweg.cs` (nur Hinweis `SIMENG_KOND_NUTZUNG_LEER`) | keine Wirkung im Rechenweg |
| Export | IFC‑Export `EPOS_Zone` mit Nutzung (SQ‑3), IDS; Datenaustauschkonzept 16.3 | Rundreise |

**Einordnung.** Das Konzept ergänzt das [Konditionierungskonzept](Konzept_Konditionierungsprofile_EPOS-Plan.md) (Vorlagen je Größe,
P11; Ausschluss von Normwerten B15/P4 bleibt), das [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.4 (Zonenplan,
Zuordnungsdialog) und das Datenaustauschkonzept 16.3 (DIN‑Nutzungszonen der Projektdatei). Die Zapf‑Nutzungsarten des Schemas 178
bleiben; das Profil verweist auf sie.

## 2. Anforderungen und Abgrenzung

- A1 Der Anwender kann Nutzungsprofile anlegen, umbenennen, duplizieren und löschen; ausgelieferte Profile und Kategorien sind
  unveränderbar (`ReadOnly`), lassen sich aber duplizieren.
- A2 Ein Profil ist einer Kategorie zugeordnet und trägt eine Nummer, die in der Kategorie eindeutig ist (DIN‑Nummer, SIA‑Kennung,
  freie Nummer).
- A3 Die Werte eines Profils sind seine Konditionierungsvorlagen je Größe; ein Profil ohne Werte ist gültig.
- A4 Gebäude und Zone können ein Profil tragen; der Wechsel bietet an, die Kalender aus den Vorlagen des Profils zu übernehmen.
- A5 Die Zuordnungen DIN‑Profilnummer → Profil, IFC‑Nutzungsklasse → Profil und Raumtyp → Nutzungsklasse sind Daten und pflegbar.
- A6 Normwerte (Sollwerte, Belegung, Lasten) werden nicht ausgeliefert.
- A7 Der Rechenweg bleibt unberührt; Referenzlauf 18/18 byte‑gleich gegen R38.
- A8 Alle Texte in beiden Sprachen; Namen und Beschreibungen der Normkategorien sind Textspalten (deutsch) der Saat, keine
  Ressourcen.

**Nicht Teil von NP:** Werte der Normprofile; ein Nutzungsprofil als Rechengröße (Belegung, interne Lasten direkt im Rechenweg);
Übersetzung der Normnamen; iOS‑Besonderheiten (der Dialog ist Razor und läuft auf beiden Schalen).

## 3. Fachliches Modell

**Kategorie.** Eine Sammlung von Profilen gleicher Herkunft. Ausgeliefert: `EPOS` („EPOS‑Muster“), `DIN18599` („DIN V 18599‑10“),
`SIA2024` („SIA 2024“), `VDI2078` („VDI 2078“). Eigene Kategorien (etwa „Bestandsaufnahme Kunde X“) sind frei.

**Profil.** Nummer, Name, Beschreibung, Kategorie, optional die Zapf‑Nutzungsart (Schema 178) für die Pufferauslegung, und die
Vorlagen je Größe. Beispiele: `EPOS:WOHNEN` „Wohnen“ (mit Werten), `DIN18599:1` „Einzelbüro“ (ohne Werte), `SIA2024:3.1`
„Schulzimmer“ (ohne Werte).

**Werte.** Je Größe (HEIZSOLL, KUEHLSOLL, LUEFTUNG, GERAETE, PERSONEN) höchstens eine Vorlage. „Vorlage übernehmen“ am Profil nimmt
für jede Größe mit Vorlage den bestehenden Weg (`ZonenplanCtrl.NutzungUebernehmen`, KP2 N1.66); Größen ohne Vorlage bleiben
unberührt (Gebäudewerte).

**Herkunft.** Wie bei Kalendern (KP1b Festlegung 6) wird am Ziel nie eine Kaskade angelegt: Ein Profil, das von Vorlage, Kalender,
Zone oder Gebäude verwendet wird, ist nicht löschbar; der Katalogabgleich ersetzt ausgelieferte Profile anhand des Schlüssels
`Kategorie:Nummer`.

**Vorbelegung beim Import.** Zone → Profil über `Z_Nutzungsquelle`: (1) Projektdatei (`.sqproj`): DIN‑Profilnummer der
Nutzungszone → Profil (Saat: die heutige Tabelle in `Din18599Nutzung.cs`, zusätzlich jede DIN‑Nummer auf ihr eigenes
DIN18599‑Profil, Vorrang nach `Rang`); (2) IFC mit `EPOS_Zone`: Schlüssel `Kategorie:Nummer`; (3) IFC ohne `EPOS_Zone`: Raumtypen
der Datei → Nutzungsklasse (`IFC_RAUMTYP`, Saat aus `GebaeudeZonierung.RAUMTYP_KLASSE`) → Profil (`IFC_KLASSE`, Saat: Büro → EPOS:BUERO,
Wohnen/Schlafen/Küche/Sanitär → EPOS:WOHNEN, sonst keines), unter **jeder** Zonierungsregel aus der flächengrößten Klasse (heute nur
Z6). Ohne Treffer bleibt das Profil leer („keines“).

## 4. Datenmodell und Schemaschritt

Schemaschritt **189** `NutzungsprofilSchema` (in der Kopfzeile der Statusdatei anmelden), STRICT, Boolean als 0/1 mit CHECK:

| Tabelle | Spalten | Bemerkung |
|---|---|---|
| `Tab_Nutzungskategorie_STAMM` | `ID`, `Schluessel` TEXT eindeutig, `Name` TEXT, `Quelle` TEXT, `Sortierung` INTEGER, `ReadOnly` INTEGER 0/1 | Saat: EPOS, DIN18599, SIA2024, VDI2078 |
| `Tab_Nutzungsprofil_STAMM` | `ID`, `ID_Kategorie` → Kategorie, `Nummer` TEXT, `Name` TEXT, `Beschreibung` TEXT nullbar, `Zapf_Nutzungsart` TEXT nullbar (Schlüssel aus Schema 178), `ReadOnly` INTEGER 0/1; UNIQUE (`ID_Kategorie`, `Nummer`) | Saat: EPOS‑Muster 4 Profile mit Werten; DIN V 18599‑10 Nr. 1–41 (Anhang A, Kurznamen), SIA 2024 Raumnutzungen (Nummer x.y, Kurznamen), VDI 2078 Beispielprofile — jeweils ohne Werte; Nummern und Kurznamen nach Lizenzprüfung (Entscheid N‑2) |
| `Z_Nutzungsprofil_Vorlage` | `ID_Profil`, `Groesse` TEXT (`KOND_GROESSEN`), `ID_Konditionierungsvorlage` → `Tab_Konditionierungsvorlage_STAMM`; UNIQUE (`ID_Profil`, `Groesse`) | ersetzt `Tab_Konditionierungsvorlage_STAMM.Nutzung`; Saat aus der heutigen Spalte (je Nutzung und Größe die erste ausgelieferte Vorlage), weitere Vorlagen gleicher Nutzung bleiben ohne Profilbindung und wählbar |
| `Z_Nutzungsquelle` | `Quelle` TEXT (`DIN18599`, `IFC_KLASSE`, `IFC_RAUMTYP`, `SIA2024`), `Fremdschluessel` TEXT, `Ziel` TEXT (`PROFIL` oder `KLASSE`), `ID_Profil` nullbar, `Klasse` TEXT nullbar, `Rang` INTEGER; UNIQUE (`Quelle`, `Fremdschluessel`) | Saat aus `Din18599Nutzung.cs` und `GebaeudeZonierung` (`RAUMTYP_KLASSE`, Klasse → Profil) |
| Änderungen | `Tab_Konditionierungsvorlage_STAMM`: `Nutzung` entfällt (Tabelle neu aufbauen, Werte in `Z_Nutzungsprofil_Vorlage`); `Tab_Konditionierungskalender`: `Nutzung` → `ID_Nutzungsprofil` INTEGER nullbar (Umschlüsselung WOHNEN → EPOS:WOHNEN usw.); `Tab_Zone.ID_Nutzungsprofil` INTEGER nullbar (Saat aus den Kalenderkopien der Zone, wenn alle dasselbe Profil tragen); `Tab_Gebaeude(_STAMM).ID_Nutzungsprofil` INTEGER nullbar | keine Fremdschlüssel mit Kaskade; Verwendung prüft der Kern vor dem Löschen |

Ergebnisneutral: keine der Tabellen wird von der Simulation gelesen (wie Schema 178). Testdatenbank anheben, mit LFS committen; die
Einfrierregel „gesäte Zonendaten / Konditionierungsdaten“ wird um `ID_Nutzungsprofil` an Zone, Gebäude und Kalenderkopien der
Referenzprojekte ergänzt (keine neue Basis, da nur Verweise; die Kalenderwerte sind unverändert).

## 5. Rechenkern und Einbau

- `NutzungsprofilCtrl` (EPOS.Kern/Controller): Kategorien und Profile lesen, Profil anlegen/duplizieren/umbenennen/löschen mit
  Verwendungsprüfung, Werte je Größe setzen, `ProfilUebernehmen(idGebaeude, idZone, idProfil)` (ruft den Weg von
  `ZonenplanCtrl.NutzungUebernehmen`), Zuordnungen lesen und pflegen, Auflösung `Kategorie:Nummer` ↔ ID.
- `DbWerte.KOND_NUTZUNGEN` und `KOND_NUTZUNG_*` entfallen; `KonditionierungNutzung` (Enum der Hülle) entfällt, die Hülle führt
  `ID`/`Schluessel`/`Anzeige` je Profil.
- `Din18599Nutzung`, `GebaeudeZonierung` (Z6‑Klassen und Zone → Profil), `IfcAbbildBauer` (`EPOS_Zone`), `Zonenplan.Freizone`
  (`IdProfil` statt `Nutzung`) lesen die Zuordnung über `NutzungsprofilCtrl`; `ZonenplanCtrl` speichert `Tab_Zone.ID_Nutzungsprofil`
  und die Kalenderkopien mit `ID_Nutzungsprofil`.
- `KonditionierungCtrl.NutzungDerHerkunft` liefert das Profil der Herkunftsvorlage (über `Z_Nutzungsprofil_Vorlage`);
  `KonditionierungsvorlageCtrl` schreibt beim Speichern/Duplizieren die Profilbindung je Größe.
- `PufferAuslegungCtrl`: Vorbelegung über `Tab_Nutzungsprofil_STAMM.Zapf_Nutzungsart` der Kalenderprofile des Gebäudes (Saat: EPOS:BUERO →
  Büro, EPOS:SCHULE → Schule, sonst Gewerbe/leer wie heute).
- Katalogabgleich (Katalogfassung, Stufe 1) und Auslieferungsvorlage: die drei `_STAMM`/`Z_`‑Tabellen mit `ReadOnly`‑Regel; Projektpaket
  nimmt eigene Profile mit, die ein Projekt verwendet.
- Export: `EPOS_Zone.Nutzung` = `Kategorie:Nummer`; IDS‑Regel auf Textmuster `[A-Z0-9]+:[^:]+` umstellen.

## 6. Benutzerführung

- **Katalogdialog „Nutzungsprofile“** (Katalogmenü, `Menuetabelle.cs`): links die Kategorien (ausgelieferte mit Schloss), rechts die
  Profile der Kategorie als `Katalogliste` (Nummer, Name, Werte als fünf Haken je Größe, Verwendung), darunter das gewählte Profil mit
  Beschreibung, Zapf‑Nutzungsart und je Größe die Vorlage (Klappliste der Vorlagen dieser Größe; „Vorlage öffnen“ springt in die
  Vorlagenverwaltung). Reiter „Zuordnungen“: Tabelle `Z_Nutzungsquelle` je Quelle, bearbeitbar. Knöpfe: Neu, Duplizieren, Umbenennen,
  Löschen (nur ohne Verwendung), Kategorie neu. Vor jeder Änderung an `Katalogliste`/`Raster` die Rasterprobe ziehen.
- **Gebäudedialog:** Feld „Nutzungsprofil“ (Klappliste gruppiert nach Kategorie, Vorgabe „keines“) mit Knopf „Profile…“; beim
  Wechsel Rückfrage „Kalender aus den Vorlagen des Profils übernehmen?“.
- **Zonendialog:** dasselbe Feld je Zone (Spalte in der Zonenliste und Feld im Zonenkopf); unbeheizte Zone ohne Heiz‑/Kühlkalender wie
  heute.
- **Zonenbaum des Imports:** die Klappliste zeigt Profile gruppiert nach Kategorie statt der drei Werte; Vorbelegung nach Abschnitt 3;
  Knopf „Profile…“ in der Knopfleiste. Beim Speichern des Zonenplans: `Tab_Zone.ID_Nutzungsprofil` und Kalenderkopien.
- **Kalenderkarte „Als Vorlage speichern“ und Vorlagenverwaltung:** statt „Nutzung“ die Wahl „Profil (Kategorie:Nummer Name)“ und die
  Größe; eine Vorlage kann ohne Profil bleiben.
- Texte in `MyResource.Resource.*` (de/en): `NUTZPROF_*`; die Kurznamen der Normprofile sind Saatdaten.

## 7. Stufen und Aufwand

| Welle | Inhalt | Agent | Aufwand |
|---|---|---|---|
| NP0 | dieses Konzept, Entscheide N‑1 bis N‑3, Schemaschritt 189 anmelden | Orchestrierung | 0,5 PT |
| NP1 | Schema 189 mit Saat und Umschlüsselung, Testdatenbank (LFS), `NutzungsprofilCtrl`, Umbau der Leser (Import, Zonenplan, Konditionierung, Pufferauslegung), Entfall `KOND_NUTZUNGEN`, SqlDialektPruefer, Tests (Schema‑Rundreise, Umschlüsselung, Zuordnung, Zonenplan, Pufferauslegung), Referenzlauf 18/18 byte‑gleich | `opus-umsetzung`, Worktree `np1` | 1,5 PT |
| NP2 | Katalogdialog, Felder in Gebäude‑ und Zonendialog, Zonenbaum, Kalenderkarte und Vorlagenverwaltung, Menü, Texte, bunit, Rasterprobe, Windows‑Schale auf Linux | `opus-umsetzung`, Worktree `np2` nach NP1 | 1,5 PT |
| NP3 | Papiere (Konditionierungskonzept B15/P4, Mehrzonenkonzept 6.4, Datenaustauschkonzept 16.3, IDS), Wiki‑Quellen Gebäudeimport, Konditionierung, Katalog (nur Funktion), Logbuch‑Satz, Protokoll, Statuszeile, Gate, Push | `sonnet-mechanik` | 0,5 PT |

## 8. Entscheide des Anwenders (vor NP1)

| Nr. | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| N‑1 | Profil auch am Gebäude (Vorgabe für Zonen ohne eigenes Profil)? | ja — Einzonengebäude brauchen den Zugang, die Zone erbt das Gebäudeprofil, wenn ihres leer ist | offen |
| N‑2 | DIN V 18599‑10, SIA 2024, VDI 2078: Nummern und Kurznamen ausliefern? | ja, nur Nummern und Kurznamen (Struktur), keine Werte; Lizenz der Normen vorher prüfen; sonst leere Kategorien ausliefern | offen |
| N‑3 | Umschlüsselung bestehender Anwenderdaten | WOHNEN/BUERO/SCHULE/SONSTIGE → die vier EPOS‑Profile; Vorlagen „ohne Angabe“ bleiben ohne Profil; keine Rückfrage beim Schemaschritt | offen |

## 9. Nachweise, Abnahme, Wiki

- Tests: Schema‑Rundreise und Umschlüsselung an einer Kopie der Testdatenbank; Verwendungsprüfung beim Löschen; Zuordnung je Quelle
  (DIN‑Nummer, IFC‑Klasse, Raumtyp, `EPOS_Zone`); Zonenplan speichern mit Profil (Kalenderkopien, `Tab_Zone.ID_Nutzungsprofil`);
  Pufferauslegung liest `Zapf_Nutzungsart`; bunit für Katalogdialog, Gebäude‑ und Zonendialog, Zonenbaum; `IfcQuelldateienDurchgangTests`
  und HC‑3‑Proben 33/36/37 nachgezogen; Wachen (`WikiProduktdatenWache`, Katalogregister, Auslieferungsvorlage) grün.
- Referenzlauf 18/18 byte‑gleich gegen R38; gestörter Lauf; Windows‑Schale auf Linux; Rasterprobe, wenn `Katalogliste` berührt ist.
- Wiki (nur Funktion): Seite „Nutzungsprofile“ unter Kataloge, Abschnitte in „Gebäudeimport“ (Zonenbaum) und „Konditionierung“
  (Vorlage und Profil); Logbuch‑Satz: „Nutzungsprofile sind ein eigener Katalog mit Kategorien; Gebäude, Zonen und Vorlagen verweisen
  darauf, und der Gebäudeimport ordnet Nutzungen über pflegbare Zuordnungen zu.“ Version beim Anwender erfragen.

## 10. Risiken

- Umschlüsselung in Anwenderdatenbanken mit eigenen Vorlagen gleicher Nutzung: nur die erste ausgelieferte Vorlage je Größe wird
  Profilwert, eigene bleiben ohne Bindung (N‑3) — im Hinweis des Schemaschritts nennen.
- Lizenz der Normtabellen (N‑2): Nummern und Kurznamen gelten als Strukturangabe; im Zweifel nur Nummern.
- Parallelsitzungen am Zuordnungsdialog (IFC‑Sitzung HC‑3): Merge‑Konflikte in `GebaeudeImportDialog.razor` und `GebaeudeImportZonen.cs`
  erwartet; vor NP2 `origin` mergen.

## 11. Verweise

[Konditionierungskonzept](Konzept_Konditionierungsprofile_EPOS-Plan.md) (B15, P4, P11, N1.66), [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md)
6.4, [Statusdatei Gebäudesimulation](Status_Gebaeudesimulation_VDI6007.md) (E90), Schema 176 `KonditionierungNutzungSchema`, Schema 178
`ProzessNutzungSchema` (`Z_Nutzungsprofil`), [ADR‑001](ADR-001_Schema-Ausrollung.md), [`CLAUDE.md`](../../CLAUDE.md).
