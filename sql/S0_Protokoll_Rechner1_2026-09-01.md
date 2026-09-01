# S0-Protokoll — Linkbereinigung Rechner 1

**01.09.2026, ca. 14:35** · Arbeitspaket S0 aus
[`Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](../Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md)
(Abschnitt 9) · gleicher Vorgang wie auf Rechner 2 am 31.08.2026
([Rev. 2, Abschnitt 2.4](../Konzept_DB-Migration_SQLite_EPOS-Plan.md))

Betroffene Datenbank: `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (Live-Bestand **Rechner 1**).
Entfernt wurden ausschließlich die vier verwaisten ODBC-Verknüpfungen (`TableDefs.Delete` per
DAO 120, **exklusiv** geöffnet — eine offene Sitzung hätte den Vorgang blockiert). Es wurden
keine Daten, keine echten Tabellen und keine Beziehungen angefasst.

## Vorbedingungen (geprüft, alle lesend)

| Prüfung | Befund |
|---|---|
| `Kenndaten.laccdb` | nicht vorhanden (keine offene Sitzung) |
| Prozess `EPOS_Plan` | läuft nicht |
| DSN `testsqlite2` (HKCU, HKLM, WOW6432Node) | in keiner Hive registriert |
| Zieldatei `C:\Ruby33-x64\bin\store\storage\development.sqlite3` | existiert nicht |
| Referenzen im C#-Code / in den 17 Abfragen / in Beziehungen | 0 / 0 / 0 (Code-Inventur 01.09., Rev. 2 Abschnitt 2.4) |

## Sicherung

`C:\ProgramData\EPOS_PLAN\Kenndaten_vor-Linkbereinigung_2026-09-01.accdb` —
151.949.312 Bytes, **byte-größengleich** zur Quelle (Stand 31.08.2026 20:10:55).

## Entfernte Verknüpfungen

`ar_internal_metadata` · `products` · `schema_migrations` · `sqlite_sequence`
→ jeweils `ODBC;DSN=testsqlite2;Database=C:\Ruby33-x64\bin\store\storage\development.sqlite3;…`
(sqliteodbc-Signatur; Reste eines früheren Rails-Experiments, vgl. Rev. 2 Abschnitt 2.4)

## Nachweis

| Kennwert | vorher | nachher |
|---|---|---|
| TableDefs ohne `MSys*` | 118 | **114** |
| davon verknüpft (`Connect <> ""`) | 4 | **0** |
| `Relations.Count` | 90 | **90** |
| Zeilen `Tab_Projekt` | 26 | 26 |
| Zeilen `Tab_Energieanlagen` | 115 | 115 |
| Zeilen `energy_carrier` | 27 | 27 |
| Zeilen `Tab_StromganglinieDaten` | 648.241 | 648.241 |

Damit melden DAO- und OLE-DB-Sicht auf **beiden** Rechnern übereinstimmend 114 echte Tabellen.
Die Skip-/Whitelist-Regel des Migrators (Implementierungskonzept Abschnitt 4, Schritt 4) bleibt
als Sicherung für Kundenbestände bestehen.
