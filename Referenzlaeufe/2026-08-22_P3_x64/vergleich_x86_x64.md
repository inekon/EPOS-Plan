# P3-Vergleichslauf x86 ↔ x64 (x64-Umstellung)

Datum: 22.08.2026, 21:59–22:00 Uhr. Beleg zu Paket P3 aus
`Konzept_Umstellung_64Bit_EPOS-Plan.md`; Prüfpunkt 4 der Abnahme.

## Aufbau

| Seite | Codestand | Build | Arbeitsverzeichnis |
|---|---|---|---|
| x86 | Tag `letzter-x86-stand` (= `3f126f4`) + resx-Konfliktauflösung aus `8da9875` | VS-MSBuild, `Platform=x86` | Git-Worktree `C:\Waermeplan\wt-x86` |
| x64 | `8da9875` (P2-Stand + resx-Fix) | VS-MSBuild, `Platform=x64` | Git-Worktree `C:\Waermeplan\wt-x64` |

Beide Läufe: identische Quelle `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`
(LastWriteTime vor/nach den Läufen identisch, `UNVERAENDERT=True`), je eigene
Arbeitskopie, Schema-Migration idempotent (Stand 36 → 36, alle 36 Schritte
„bereits erledigt"), identische Projektliste (x64 per `--projekte` auf die
Auswahl des x86-Laufs festgenagelt).

## Ergebnis

**GESAMT: PASS — 2.427.467 Werte innerhalb der Toleranz**
(relativ 0,0001 ab Betrag 1, sonst absolut 0,01; Werkzeug `Referenzlauf.exe vergleich`).

| Projekt | Name | Dateien | Werte | Ergebnis |
|---|---|---|---|---|
| 1007 | Laurentiuskirche | 29 | 324.210 | PASS |
| 1011 | test1 | 29 | 324.232 | PASS |
| 1012 | test2 | 18 | 201.540 | PASS |
| 1017 | WP_PV-Speicher | 21 | 254.143 | PASS |
| 1021 | TestSpeichernUnter | 21 | 227.840 | PASS |
| 1023 | Wöhler - Test1 | 25 | 262.918 | PASS |
| 1024 | Wöhler - Test2 | 26 | 271.695 | PASS |
| 1026 | Beispiel WP WG 1 | 29 | 324.239 | PASS |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | 22 | 236.650 | PASS |

Beide Läufe 9 von 9 erfolgreich; Simulations-Hinweise/-Warnungen der Projekte
sind auf beiden Seiten identisch (Energieträger-Zuordnung, Kennlinien-
Extrapolation — Bestandsthemen, keine Bitness-Effekte). Die im Konzept
(Abschnitt 4) für möglich gehaltenen FMA-/AVX-Abweichungen blieben unterhalb
der Toleranz — es war keine `DOTNET_EnableFMA=0`-Diagnose nötig.

Der x64-Lauf belegt zugleich den Schreibpfad über den 64-bit-ACE-Provider:
Schema-Migrations-Bootstrap und neun geschriebene Ergebnisköpfe
(IDs 181–189) in der Arbeitskopie.

## Ablage

Dieser Ordner (`2026-08-22_P3_x64`) ist der eingefrorene Stand des x64-Laufs
und die neue Vergleichsbasis. Der x86-Gegenlauf (`2026-08-22_P3_x86`) wurde
nach dem Vergleich nicht eingecheckt — er ist jederzeit reproduzierbar:
Worktree vom Tag `letzter-x86-stand`, die beiden `MyResource\Resource*.resx`
aus `8da9875` übernehmen (Konfliktauflösung), Referenzlauf mit VS-MSBuild
`Platform=x86` bauen, `lauf --projekte 1007,1011,1012,1017,1021,1023,1024,1026,1030`.
