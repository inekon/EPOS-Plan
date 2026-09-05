# Konzept: Projektstammdaten — Datumspflege, Kunde und Bearbeiter (EPOS-Plan)

**Rev. 1 (zur Abnahme)** · 02.09.2026

Auftrag (Screenshot „Projekt Speichern unter"): Erstell- und Änderungsdatum
fehlen im Dialog und müssen in der Datenbank ergänzt und gepflegt werden;
Kunde und Bearbeiter brauchen mehr Datenfelder (Adresse, E-Mail, …) — wichtig
für die Berichterstellung; die Angaben können in einem separaten Dialog
liegen; das Design soll insgesamt besser werden.

---

## 1. Ziel

Jedes Projekt trägt belastbare Zeitstempel (angelegt / zuletzt geändert), die
bei JEDER Änderung nachgeführt werden, und verweist auf **Stammdatensätze**
für Kunde und Bearbeiter (Firma, Ansprechpartner, Adresse, Telefon, E-Mail).
Die Berichte (Word/Excel) zeigen daraus einen Adress- und Kontaktblock statt
zweier Freitextzeilen. Die Projektdialoge (Assistent „Neues Projekt", „Projekt
Speichern unter") wählen Kunde und Bearbeiter aus den Stammdaten und pflegen
sie über einen gemeinsamen Stammdatendialog.

## 2. Bestand — Befunde

- **Datumsspalten existieren:** `Tab_Projekt.Erstelldatum` und
  `Aenderungsdatum` sind da; `ProjektCtrl` schreibt beide bei Insert/Update.
  Beim LESEN kaschiert ein Fallback jeden NULL-Wert mit „heute"
  (`ProjektCtrl` Zeile ~297) — ein fehlendes Datum sieht aus wie ein
  aktuelles.
- **Nicht gepflegt:** Das Änderungsdatum setzen nur der Assistent
  (`Wizard_Projekt.GetDatum` = jetzt) und „Speichern unter". Alle anderen
  Änderungswege — Anlagen über Startkarten/Kontextmenüs/Simulationsdetail
  (laufen alle durch `WizardCtrl.Add_WP_Waermeerzeuger`), Kostenpositionen,
  Energieträger/Tarife, Simulationsergebnisse, Import, Varianten — lassen es
  stehen. Das Datum ist damit heute nicht belastbar.
- **Kopien erben das Erstelldatum:** „Speichern unter" (Duplizierer) und
  Varianten übernehmen `Erstelldatum` des Originals (bewusster
  Bestandsentscheid, Kommentar in `Form_ProjektSpeichernUnter`); für ein
  neues Projekt fachlich fragwürdig (PF1).
- **Kunde/Bearbeiter sind Freitext** (`Tab_Projekt.Kunde`, `.Bearbeiter`),
  ohne Stammdaten, ohne Adresse/Kontakt; keine Tabellen `Tab_Kunde`/
  `Tab_Bearbeiter`.
- **Dialog „Speichern unter":** eigene Liste (Name/Beschreibung, ohne Suche,
  ohne Datum, Spalten auf Inhalt gekappt), vier Freitextfelder, Doppelprüfung
  nur gegen die Liste, keine Datumsanzeige — das Bild des Screenshots.
- **Berichte:** `BausteineProjekt` („Projektbeschreibung"),
  `BausteineStandard` und `ExcelBerichtGenerator` schreiben Kunde und
  Bearbeiter als je EIN Textfeld; „Angelegt"/„Zuletzt geändert" werden im
  Word-Bericht bereits ausgegeben — mit dem unbelastbaren Datum.

## 3. Zielbild

### 3.1 Datenmodell — Migrationsschritt 62 (SQLite-DDL über `Ddl`/`SpaltenAnlegen` wie Schritt 61)

| Tabelle / Spalte | Inhalt |
|---|---|
| **`Tab_Kunde`** | `ID`, `Firma`, `Anrede`, `Vorname`, `Nachname`, `Strasse`, `PLZ`, `Ort`, `Land`, `Telefon`, `Mobil`, `EMail`, `Notiz`, `Aktiv`, `Erstellt`, `Geaendert` |
| **`Tab_Bearbeiter`** | `ID`, `Nachname`, `Vorname`, `Kuerzel`, `Firma`, `Abteilung`, `Telefon`, `EMail`, `Rolle`, `WindowsBenutzer` (für die Vorbelegung), `Aktiv` |
| **`Tab_Projekt`** | `ID_Kunde`, `ID_Bearbeiter` (nullable, weiche Verweise) |

- Die Freitextspalten `Kunde`/`Bearbeiter` **bleiben** als Anzeigetext und
  Fallback für Altwege und Berichte; **der Stammsatz führt**, der Text wird
  aus ihm nachgeführt (Anzeigename Kunde = Firma, sonst „Nachname, Vorname").
- **Datenübernahme im Schritt 62:** je eindeutigem Freitext (getrimmt,
  Groß/Klein egal) ein Stammsatz, Verknüpfung gesetzt, Anzahl im
  Migrationsbericht; leerer Text → keine Verknüpfung (PF6).
- **Duplizierer/Varianten:** `ID_Kunde`/`ID_Bearbeiter` sind Katalogverweise
  (`KATALOG_SPALTEN`, nicht versetzen). **Projekttransfer:** Beipack mit
  natürlichem Schlüssel (`KATALOG_SPALTE_ZU_TABELLE`/`KATALOG_NATURALKEY`:
  Kunde = Firma+Nachname+Ort, Bearbeiter = Kürzel, sonst Name) — Ziel
  gewinnt bei Gleichheit, fehlende Sätze werden angelegt (PF7).

### 3.2 Datumspflege — eine Wahrheit

- `ProjektCtrl.AenderungMerken(idProjekt)` schreibt `Aenderungsdatum = jetzt`
  und wird an den ZENTRALEN Schreibwegen aufgerufen: Anlagen
  (`WizardCtrl.Add_WP_Waermeerzeuger`), Gebäude/Bedarfe/Verbraucher
  (Assistenten-Speichern), Kostenpositionen (`KostenProjektPositionenCtrl`
  Neu/Update/Löschen/Übernahme), Energieträger-/Tarifpflege im Projekt,
  Simulationsergebnisse (`ErgebnisCtrl.Save`), Varianten-Anlegen und Import
  (jeweils für die Kopie). Eine Aufrufliste im Code-Kommentar hält die
  Wahrheit an einer Stelle.
- **Kopien** (Speichern unter, Variante, Import): `Erstelldatum = jetzt`,
  `Aenderungsdatum = jetzt`; das Original bleibt unberührt (PF1).
- **Lesen ohne Maske:** NULL-Datum wird als NULL geführt und in Listen und
  Berichten als „—" gezeigt, nicht als „heute" (PF2). Schritt 62 setzt ein
  fehlendes Erstelldatum auf das älteste bekannte Datum des Projekts
  (Änderungsdatum), sonst bleibt es leer.

### 3.3 Dialoge

- **Stammdatendialoge** „Kunden verwalten…" und „Bearbeiter verwalten…"
  (Administration): gemeinsames Muster — Liste mit Suche links, Formular
  rechts, Anlegen/Ändern/Deaktivieren; Löschen nur, wenn kein Projekt
  verweist, sonst Deaktivieren (PF8). Designer-Dialoge (Ä6), App-Design,
  resx de/en, Dreischichtenregel.
- **Projektseiten** (Assistent „Neues Projekt"/Bearbeiten, „Speichern
  unter"): Kunde und Bearbeiter als **Klapplisten** der aktiven Stammsätze
  mit „…"-Knopf (Neu/Bearbeiten im Stammdatendialog, Rückkehr mit Vorwahl);
  Bearbeiter-Vorbelegung über die Windows-Benutzerzuordnung des Stammsatzes,
  sonst wie heute der Windows-Benutzername (PF5). Kunde bleibt optional (PF3).
- **„Speichern unter" neu:** Projektliste über das gemeinsame
  `ProjektAuswahl`-UserControl (Suche, Sortierung, Stamm/Varianten-
  Gruppierung, Tooltips, waagerechter Bildlauf — Stand 02.09.2026) mit den
  Spalten Name/Kunde/Geändert; darunter Neuer Name (Pflicht, Live-
  Doppelprüfung wie im Assistenten), Beschreibung, Kunde/Bearbeiter
  (Klapplisten, vorbelegt aus der Vorlage), Datumszeile „Vorlage angelegt am
  … · zuletzt geändert am …" und der Hinweis „Die Kopie erhält das heutige
  Datum"; Fortschritt und OK/Abbrechen wie heute.

### 3.4 Berichte

- Word und Excel erhalten einen **Adressblock Kunde** (Firma, Ansprechpartner,
  Straße, PLZ Ort, Telefon, E-Mail) und den **Bearbeiter-Kontakt** (Name,
  Kürzel, Firma/Abteilung, Telefon, E-Mail) in `BausteineProjekt`,
  `BausteineStandard`, `ExcelBerichtGenerator`; Texte de/en in
  `BerichtTexte`; Fallback auf den Freitext, wenn kein Stammsatz verknüpft
  ist. Deckblatt/Kopfzeile nutzen dieselben Felder.
- „Angelegt" und „Zuletzt geändert" werden mit 3.2 belastbar.

## 4. Prüfstand (kd1runner)

Migration 62 (Tabellen, Spalten, Übernahme: n Stammsätze aus m Freitexten,
Verknüpfungsquote), `AenderungMerken` an jedem Schreibweg (Datum steigt),
Kopie-Datum bei Speichern unter/Variante/Import, Transfer-Roundtrip mit
Kunde/Bearbeiter-Beipack, Berichtsbausteine (Adressblock erscheint, Fallback
greift), Sweep für die neuen Dialoge, Dumps als Sichtbelege.

## 5. Etappen

| Etappe | Inhalt | Abnahmekriterium |
|---|---|---|
| PS1 | Datumspflege: `AenderungMerken` an allen zentralen Schreibwegen, Kopie-Datum, NULL ehrlich; Datumsanzeige in „Speichern unter" und Öffnen-Liste | Smoke: jede Änderung hebt das Datum; Kopien tragen heutiges Datum; keine „heute"-Maske mehr |
| PS2 | Datenmodell + Migration 62 (Tab_Kunde, Tab_Bearbeiter, Verweise, Übernahme aus Freitext) | Migration auf Produktivkopie grün, Übernahmequote im Bericht, kd-Smoke |
| PS3 | Stammdatendialoge Kunde/Bearbeiter + Klapplisten in Assistent und Speichern unter | Sweep grün, Sichtbelege, Deaktivieren-statt-Löschen |
| PS4 | Berichte: Adress-/Kontaktblock Word+Excel, Texte de/en, Fallback | Berichtsprobe beider Generatoren |
| PS5 | „Speichern unter" auf `ProjektAuswahl` und neues Layout | Sichtbeleg, Sweep grün |
| PS6 | Duplizierer/Transfer-Beipack, Prüfstand dauerhaft, Doku, Sichtabnahme | transfer-Roundtrip grün; Abnahme durch Nutzer |

## 6. Entscheidungspunkte (zur Abnahme)

| Nr. | Frage | Vorschlag |
|---|---|---|
| PF1 | Erstelldatum einer Kopie (Speichern unter/Variante/Import) = heute? | ja — eine Kopie ist ein neues Projekt; das Original behält seine Daten |
| PF2 | Fehlendes Datum als „—" statt als „heute" anzeigen? | ja |
| PF3 | Kunde Pflichtfeld? | nein — optional; Bericht zeigt „—" |
| PF4 | Umfang der Adressfelder | wie § 3.1; Land als Freitext, keine Adressvalidierung |
| PF5 | Bearbeiter-Vorbelegung über Windows-Benutzer des Stammsatzes | ja |
| PF6 | Freitext beim Migrieren automatisch in Stammsätze überführen | ja; Dubletten später im Stammdatendialog zusammenführen |
| PF7 | Kunde/Bearbeiter im Projekttransfer als Beipack | ja (natürlicher Schlüssel, Ziel gewinnt) |
| PF8 | Verknüpfte Stammsätze löschen? | nein — deaktivieren |

## 7. Abgrenzung

Kein Mehrfachkunde je Projekt, kein CRM (Angebote, Rechnungen, Historie),
keine externe Adressprüfung. Die Freitextspalten werden nicht entfernt.
