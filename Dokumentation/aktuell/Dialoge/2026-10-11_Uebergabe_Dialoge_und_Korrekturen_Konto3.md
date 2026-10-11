# Übergabe der Sitzung „Dialoge und Korrekturen“ (Konto 3, 11.10.2026)

Dieses Papier übergibt die Sitzung „Dialoge und Korrekturen“, Konto 3, nach Welle 4 an eine neue
Sitzung. Es nennt den Stand, die geltenden Entscheide, die Regeln der Sitzung über `CLAUDE.md`
hinaus, die Cloud-Umgebung und die offenen Punkte; der Einstiegs-Prompt steht in Abschnitt 5.
Regelquelle bleibt `CLAUDE.md`. Es löst das Papier der Vorgängersitzung
([`2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md`](../../ueberholt/2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md))
ab; dessen Abschnitt 2 und das Papier vom 08.10.2026
([`2026-10-08_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md`](../../ueberholt/2026-10-08_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md))
bleiben für die dort genannten Entscheide gültig und werden nicht abgeschrieben.

## 1 Stand

- **Zweige:** Arbeitszweig `ios_migration_september`; Sitzungszweige `claude/dialoge-korrekturen-okt`
  (vom alten Konto, wird weiter mitgepusht) und `claude/nifty-wright-nrx0z6` (dieses Konto). Alle drei
  standen beim Schreiben auf `d48d44fb`. Keine laufenden Agenten, keine Worktrees.
- **Kennung** der Statuszeilen: `Sitzung „Dialoge und Korrekturen“, Konto 3`.
- **Statuszeilen dieser Sitzung** (11.10.2026):
  - #966 IM-4: eigene Vorschau für Quellprofil, Spotpreise und TWW-Messreihen im Optionendialog „Format und Vorschau“.
  - #967: Wiki Kosten, Absatz zum Kostenprofil-Bild.
  - #976 FP-1 / IM-3T: Fensterprobe-Fall Meldungsbanner mit Hinweisliste, Test des Excel-Lesefensters.
  - #977 KT-7: Übernehmen-Rückrufe, Schlossweg und Importgaben der Katalogdialoge fangen Ausnahmen.
  - #978 HB-3: Bannerprobe „Gebäude“ wieder grün, `scroll-padding-top` folgt der Bannerhöhe.
  - #979 VL-2: Langtexte der Wärmepumpen-Katalogliste.
  - #991 KT-8: restliche Fangwege, KI-Sperrgründe, Kältemaschinen-Anlage.
  - #992 HZ-1: Hilfeziel des Kostenprofil-Dialogs.
  - #1000 KT-9: Vergleichsgabe gefangen, Ladestand der Gebäudeverwaltung.
- **CI-Vermerke:** gesetzt für #966/#967 (Lauf 38138076403), #976–#979 (38151816390), #991/#992
  (38166058525); **#1000 offen** (Läufe 38178858765, 38178861579, 38178868690 auf `d48d44fb`). Die
  Vermerke #949–#955 der Vorgängersitzung hat diese Sitzung mit 38129974682 gesetzt.
- **Basis** R52 `2026-10-11_R52_Vorheizen` (von einer anderen Sitzung eingefroren). Die Kopfzeile
  „Schemaschritt angemeldet“ ist von dieser Sitzung nicht belegt.
- **Logbuch:** Satz 1.2.0.9 nur für #966; alles Übrige sind Fangwege, Tests, Langtexte oder Hilfeziele
  ohne sichtbare Bedienänderung (Regel 13.4).

## 2 Geltende Entscheide des Anwenders (11.10.2026)

Die Entscheide aus Abschnitt 2 der beiden abgelösten Papiere (siehe oben) gelten weiter. Neu:

- Konto-Kennung 3.
- Die Kennzahlenzeile „Jahressumme · Höchstleistung“ beim Lastgang im Optionendialog bleibt.
- Bannerprobe „Gebäude“ untersuchen: Ergebnis, die Probe war veraltet (der Wirt gab Projektkopien
  heraus). `scroll-padding-top` folgt der Bannerhöhe (Token `--epos-meldung-hoehe`, Modul
  `epos-warnbanner.js`, Regel in `EPOS.UI/CLAUDE.md`).
- Alle drei Nachzüge aus Abschnitt 5 der Übergabe vom 11.10. sind gebaut (Fensterprobe-Fall und
  Excel-Test, ungefangene Wege, Langtexte).
- KT-8 (Restwege, KI-Sperrgründe) und HZ-1 (Hilfeziel `Kosten#kostenprofil`) sind gebaut.
- KT-9 (Vergleichsgabe, Ladestand der Gebäudeverwaltung) ist gebaut.

## 3 Regeln der Sitzung (über `CLAUDE.md` hinaus)

Die Regeln aus Abschnitt 3 und dem Einstiegs-Prompt der abgelösten Papiere gelten weiter, ergänzt um:

**Agenten**

- Fable orchestriert nur (Anwendervorgabe). Umsetzung `opus-umsetzung`, Mechanik `sonnet-mechanik`,
  Zählungen `haiku-pruefung`; höchstens zwei Agenten parallel.
- Worktree-Agenten starten auf einem veralteten Commit und setzen zuerst auf
  `origin/ios_migration_september` oder den genannten Zweig zurück. Hauptbaum-Agenten beginnen jeden
  Befehl mit `cd <repo> &&` und legen `AGENT_LAEUFT` an.

**Wellenfolge**

- Merge → Bau beider Schalen → voller UI-Lauf → Gate `Werkzeuge/Gate/gate_rest_linux.sh` (REST79xx,
  rund 11 Minuten) → voller `dotnet test` des Kern-Filters (rund 70 Minuten) → Statuszeilen →
  Push dreier Zweige → CI-Vermerk als Routine über `send_later` → Worktrees und Zweige entfernen.
- Statusnummer unmittelbar vor dem Commit gegen origin messen; fünf Sitzungen nummerieren parallel,
  Umnummerierung ist die Regel.
- CI-Vermerk rund 85 Minuten nach dem Push; es zählt der Lauf des Sitzungszweigs, weil fremde Pushes
  die Läufe auf `ios_migration_september` laufend abbrechen.
- Vor dem Push bis zu 30 Minuten auf laufende Kern-Läufe warten, dann trotzdem pushen.
- Merges mit Fachkonflikt (Dialoge, Tests) löst Opus. `.resx` vereinigt Sonnet eintragsweise
  (schneidet der Hunk ein `<data>`-Element, fehlt ein `</data>`). Statusdatei-Konflikte: origin-Fassung
  nehmen und die eigenen Zeilen anhängen.
- Logbuch nur für sichtbare Bedienänderungen (Regel 13.4); Fangwege, Tests, Langtexte und Hilfeziele
  bekommen keinen Satz.
- Trailer `Co-Authored-By: Claude <Modell>` und `Claude-Session`.

### Cloud-Umgebung

- **.NET-SDK:** Die Netzwerkregel sperrt `builds.dotnet.microsoft.com` und
  `download.visualstudio.microsoft.com`. Das SDK kommt aus conda-forge (`conda.anaconda.org` ist
  erreichbar): Pakete `dotnet-sdk-10.0.401`, `dotnet-runtime-10.0.12`, `dotnet-aspnetcore-10.0.12`
  (linux-64, `.conda`-Format, ein Zip mit `pkg-*.tar.zst`, entpackt mit Python `zstandard` aus PyPI)
  nach `/root/.dotnet-prefix/lib/dotnet`, Verknüpfung `/usr/local/bin/dotnet` und
  `/etc/dotnet/install_location`. `global.json` (10.0.400, `latestFeature`) ist damit erfüllt;
  Ubuntu-apt bietet nur 10.0.112 und reicht nicht. Empfehlung: diesen Bezugsweg ins Setup-Skript der
  Umgebung aufnehmen oder die zwei Hosts freigeben.
- **LFS:** `git lfs fetch origin origin/<zweig> --include=Referenzlaeufe/Kenndaten_Test.sqlite`
  (Ref mit `origin/`-Präfix), dann `git lfs checkout`.
- **Browser:** Playwright/Chromium sind vorinstalliert (`PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers`),
  nie `playwright install`.
- **GitHub:** `gh` ist nicht verfügbar; CI über `mcp__github__actions_list` und `get_job_logs`.
- **Neustart:** Ein Neustart des Arbeitsprozesses lässt Agenten und Routinen weiterlaufen.
- **Stop-Hook:** Er mahnt ungepushte Commits an; während einer Welle ist das der laufende Merge,
  nicht pushen.

**Nie ohne Rückfrage:** iOS-, macOS- und Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags.
Keine Zugangsdaten ins Repository. **Antworten:** auf Deutsch, knapp.

## 4 Offene Punkte (mit Empfehlung)

**(a) Beim Anwender**

| Punkt | Empfehlung |
|---|---|
| Sichtabnahmen unter Windows: Importdialog Quellprofil/Spotpreise/Messreihen mit Kennzahlenzeile; Fehlerfälle der Fangwege KT-7 bis KT-9; Banner mit aufgeklappter Hinweisliste und Tabulator; Langtexte der Wärmepumpen-Katalogliste | Der Anwender prüft; Befunde als eigener Auftrag |
| Fachliche Korrektur des Wiki-Absatzes zum Kostenprofil-Bild (vier Punkte aus #967) und des Wortlauts „Wärmequelle (Luft, Sole, Wasser)“ (#979) | Der Anwender liest gegen; Korrektur danach in einem Zug |
| Wiki-Sammelupload: sechs geänderte Seiten (Kosten, Wärmepumpe, Brauchwasser-Zapfprofil, Photovoltaik, Programm Dokumentation (Hilfetabelle), Logbuchsatz #966) | Nur auf Zuruf, mit Versionsnummer |
| iPad-Abnahme oder iOS-Lauf (#978: Token nicht in WebView2 gemessen) | Nur nach Freigabe |

**(b) Technisch, aus den Offen-Teilen der Statuszeilen**

| Punkt | Empfehlung |
|---|---|
| Spaltenwahl im Optionendialog bei Spotpreis und Messreihe wirkt nur auf die Vorschau (#966) | Belassen, bis die Sichtabnahme etwas anderes zeigt |
| Ein geändertes Jahr legt die Preise nach Tageszählung auf das andere Jahr (#966) | Belassen |
| Leere Lückenschwelle liest den Parametersatz kurz auf dem Oberflächenfaden (#966) | Eigener Auftrag, klein |
| Monatsmarken der Zeitachse und Speicherauslegung ohne Vorschau (#967) | Belassen |
| Wärmepumpen-Anlagendialog sperrt bei Satzfehlern keine Felder; Träger-Rückschreiben beim Abbrechen kann scheitern (#991) | Eigener Auftrag, wenn die Sichtabnahme es als störend zeigt |
| Vergleichsfehler steht im Meldungsbanner statt im Satzbanner (#1000) | Belassen (jede Satzwahl leert den Satzbanner) |

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Dialoge und Korrekturen“ für EPOS-Plan (Repo inekon/EPOS-Plan,
Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Dialoge/2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Konto3.md,
dann den Kopf und die letzten zwanzig Zeilen der Tabelle in
Dokumentation/aktuell/Status_iOS_Migration.md und
`git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Kennung der Statuszeilen: „Sitzung „Dialoge und Korrekturen“, Konto 3“ oder die Kennung, die der
Anwender nennt. Rolle, Agenten, Wellenfolge und Regeln stehen in Abschnitt 3 des Papiers und gelten
vollständig. Gepusht werden drei Zweige: ios_migration_september,
claude/dialoge-korrekturen-okt und claude/nifty-wright-nrx0z6 (oder der Sitzungszweig, den die
Umgebung vorgibt). Nie ohne Rückfrage: iOS-, macOS-, Setup-Läufe, Wiki-Upload, Pull Requests,
Force-Push, Tags. Antworten auf Deutsch, knapp.

Cloud-Umgebung prüfen: `dotnet --version` muss 10.0.401 zeigen, sonst den conda-forge-Weg aus
Abschnitt 3 gehen; LFS-Testdatenbank holen (kein 130-Byte-Zeiger); `df -h` auf freien Platz.

Erste Schritte: (1) Stand prüfen: `git status` sauber, keine Konfliktmarker, keine liegen gebliebene
AGENT_LAEUFT. (2) CI-Vermerk der Zeile #1000 setzen, falls er noch offen ist. (3) Den Stand in
höchstens fünf Zeilen melden. (4) Mit dem Anwender die offenen Punkte aus Abschnitt 4 abstimmen.
```
