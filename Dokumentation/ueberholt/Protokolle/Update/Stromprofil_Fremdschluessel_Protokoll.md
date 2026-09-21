# FK‑1 — Fremdschlüsselfehler beim Standard-Stromprofil, Schemaschritt 100 (Protokoll, 21.09.2026)

Statuszeile #426 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Vorgänger
[`FK2_Projektfremdschluessel_Protokoll.md`](FK2_Projektfremdschluessel_Protokoll.md) (Schritt 96).
Zweig `fk-1`, Commits `3eac1d79`, `e5ef10ec`, `02f94c59`, `490717f4`; Merge `f8d2c627`.
Anwenderauftrag 21.09.2026: „FK beheben, vor allen anderen Aufgaben".

## Anlass

Gerätemeldung (Windows, Bildschirmfotos vom 21.09.2026): Nach der Auswahl eines Standard-Stromprofils
(„Standard Stromprofil", EFH_3_Pers) und dem Verlassen des Dialogs meldet der Projekt-Assistent
„SQLite Error 19: FOREIGN KEY constraint failed" bei `INSERT INTO Z_Projekt_Stromverbraucher`.

## Befund

Auf einer `VACUUM INTO`-Kopie der Live-Datenbank und an der Testdatenbank (gleiche Struktur) geprüft:

- `StromverbraucherStammCtrl.CopyFromStamm` legte das Typprofil in `Tab_Stromverbrauchertyp` ohne
  `ID_Projekt` an. Die Spalte trug `DEFAULT 0` und seit Schritt 96 einen Fremdschlüssel auf
  `Tab_Projekt(ID)`; ein Projekt 0 gibt es nicht. Die Kopie wurde im Ganzen zurückgerollt, die
  Meldung landete nur auf der Konsole. Die Schwesterwege für Brauchwasser und Prozesswärme
  setzten die Projektspalte bereits.
- `WizardCtrl.Add_Projekt_Stromverbraucher` nahm nach der gescheiterten Kopie stillschweigend
  die Katalog-ID und schrieb sie in die Zuordnungstabelle, deren Fremdschlüssel auf die
  Projekttabelle zeigt — die sichtbare Meldung. Dasselbe Rückfallmuster stand bei Prozesswärme
  und Brauchwasser.
- Muster dahinter: 41 Fremdschlüsselspalten in 25 Tabellen trugen `DEFAULT 0`, und keine
  Elterntabelle hat eine Zeile 0. Jeder Schreibweg, der eine solche Spalte weglässt, verstößt.
  Über alle wörtlichen Einfügebefehle geprüft war nur `WizardCtrl.Add_SP` betroffen, ohne Aufrufer.
- Der Assistententest fuhr den Kopierweg nicht: Er fügte einen leeren Verbraucher ohne Typprofil ein.

## Änderung

1. Der Typprofil-Insert der Stromverbraucher-Kopie schreibt `ID_Projekt` (Spaltenliste `ID,
   ID_Stromverbraucher, ID_Projekt, Typname, Beschreibung`). Alle drei Kopierer setzen die
   Projektspalte; `ProjektDuplizierenCtrl` versetzt sie beim Duplizieren mit dem Projekt-Offset.
2. `Add_Projekt_Prozess`, `Add_Projekt_Stromverbraucher` und `Add_Projekt_Brauchwasser` brechen bei
   gescheiterter Kopie benannt ab (`DataRepository.FehlerMelden` mit Gewerk und Bezeichner,
   Rückgabe `false`, nichts geschrieben). Eine Katalog-ID geht nie mehr in eine Projektzuordnung.
3. `WizardCtrl.Add_SP` entfernt (kein Aufrufer, keine Tests).
4. **Schemaschritt 100** `FremdschluesselVorgabe`: Der Katalog wird gemessen
   (`pragma_foreign_key_list` × `pragma_table_info`, `dflt_value = '0'`), der Zieltext entsteht aus
   dem Bestands-DDL (nur die genannte Spaltendefinition verliert ihr `DEFAULT 0`, NOT NULL bleibt),
   Neubau mit `VorgangOhneFremdschluessel`, je Tabelle eine Transaktion, wiederholbar; eine Zeile
   mit dem Wert 0 hält den Schritt benannt an. Registriert in `SchemaStand.Zielversion = 100`,
   `SchemaMigration`, `Werkzeuge/Testdatenbankschema` und `EPOS.Kern.Tests/TestDatenbank`.
   Betroffen sind 41 Spalten in 25 Tabellen, darunter acht Gerätespalten von `Tab_Energieanlagen`,
   die Projektspalten der Bedarfs-, Klima- und Ganglinientabellen, die Zuordnungstabellen und
   `energy_project_settings`. Weggelassen heißt jetzt NULL („kein") oder ein sofortiger
   NOT‑NULL‑Fehler, nicht mehr „Projekt 0".
5. Testdatenbank auf Stand 100 nachgezogen (reines Nachziehen: Tabellen, Indizes, Sichten,
   Zeilenzahlen und Sequenzen gleich, `integrity_check` und `foreign_key_check` sauber), mit LFS
   committet; `Referenzlaeufe/LIESMICH.md` um den Schritt ergänzt, Basis
   `2026-09-19_R10_BhkwWirkungsgrad` bleibt.

## Zeugen

`KatalogKopieProjektbezugTests` (8 Fälle): die drei Kopierer mit Stammsatz samt Typprofil, Kopf und
Profil mit richtiger `ID_Projekt`, `foreign_key_check` leer; je Gewerk der benannte Abbruch ohne
Katalogsatz; die Zuordnung zeigt auf die Projektkopie. `FremdschluesselVorgabeTests` (7 Fälle):
Zielstand 100, Zieltext samt STRICT-Wache, Wache über alle Fremdschlüsselspalten ohne `DEFAULT 0`,
Umbau an einer Probetabelle, Wiederholbarkeit, Abbruch bei einer Zeile mit 0.

## Zahlen und Abnahme

Worktree: Kern-Filter 0 Fehler, Windows-Schale 0 Fehler, 10 311 Tests grün (1 übersprungen),
SqlDialektPruefer 0 Fundstellen bei 1 566 Texten, Referenzlauf der fünf CI-Projekte PASS
(1 656 417 Werte, byte-gleich). Gate im Hauptbaum auf `f8d2c627`: siehe Statuszeile #426.
Die Live-Datenbank migriert beim nächsten Programmstart auf Stand 100 (`migration_protokoll.txt`,
eine Zeile je Tabelle); die Abnahme am Gerät steht aus.
