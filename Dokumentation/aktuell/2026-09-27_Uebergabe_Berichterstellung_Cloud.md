# Übergabe: Sitzung „EPOS-Plan Berichterstellung“ — Stand 27.09.2026, nur noch Cloud

Die Sitzung „EPOS-Plan Berichterstellung“ (Berichtsvorlagen BV-E0–E9, Platzhaltermarken,
Diagramm- und Ergebnisbilder, „Zum Bericht ›“, Navigation „Berichte & Kosten“) wird in einer
neuen Cloud-Sitzung fortgeführt; die Desktop-App arbeitet danach nur noch mit der Cloud. Dort
gibt es nur, was im Repositorium liegt und was das Einrichtungsskript der Umgebung installiert.
Dieses Papier nennt den Stand, das Offene, die Regeln und die Einrichtung.

## 1 Stand

- Alle Commits der Vorgängersitzung (Zweig `claude/intelligent-bohr-hthrk8`, letzter
  `67f22b24`) stecken in `origin/ios_migration_september` (`8bda803e` beim Wechsel); es gibt
  keinen ungepushten Rest.
- Das Konzept Berichtsvorlagen ist mit BV-E9 (#566) umgesetzt und liegt unter
  [`../ueberholt/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../ueberholt/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md);
  gültig sind Code, [`Werkzeuge/Berichtsvorlage/LIESMICH.md`](../../Werkzeuge/Berichtsvorlage/LIESMICH.md)
  und die Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki`.
  Protokolle BV-E0 … BV-E9 samt Nachzügen unter [`../ueberholt/Protokolle/Bericht/`](../ueberholt/Protokolle/Bericht/).
- Mitgelieferte Vorlagen auf Katalogfassung 10 (`c04045bc`), Ergebnisbilder, Speicherlauf und
  solare Deckung im Katalog (`ef888e71`), Platzhaltermarken an den Ergebnisreitern (#581),
  „Zum Bericht ›“ der Wirtschaftlichkeit (#582), Rennen auf der Berichtsseite behoben (`fe3d6593`).
- Letztes Gate der Vorgängersitzung: 16.410 Tests grün, Referenzlauf PASS gegen R23,
  ChartProben 220/0 gegen die Linux-Messlatte `Proben/ChartProben/Messlatte_2026-09-26.sha256`.
- Referenzbasis **R23** `Referenzlaeufe/2026-09-26_R23_KesselBereitschaft`, Schemastand 151,
  nächster Schemaschritt **152** (Gebäudesimulation hat ihn angemeldet), höchste Statusnummer
  **#587**. Nummern vor jeder Vergabe auf `origin` messen:
  `git show origin/ios_migration_september:Dokumentation/aktuell/Status_iOS_Migration.md | grep -oE '^\| \*\*#5[89][0-9]' | sort -u | tail -1`
  — die Vorgängersitzung hat Nummern ohne Abstimmung vergeben (#566, #581), das hat Kreuzungen
  mit „Dialoge und Korrekturen“ erzeugt.

## 2 Was aussteht

### 2.1 Entscheide des Anwenders

| Thema | Frage | Empfehlung |
|---|---|---|
| Berichte & Kosten (Nach #574) | BN-Q1–Q3 des [Konzepts](Konzept_BerichteKosten_Navigation_EPOS-Plan.md), Mockups A/B/C | Variante A |
| Wirtschaftlichkeit (Nach #582) | Folgt der Bericht dem gewählten Szenario der Ergebnisseite (heute: Erwartet plus Bandbreite aller drei)? | ja, eigener Auftrag |
| Kostenkapitel (Nach #555 b) | Weist der Bericht die Gruppenzahl „Strom ohne Verwendung“ statt der Einzelzahl aus? | Gruppenzahl, wie im Vergleich |
| Repowurzel | `Berichtsvorlagen/` (samt `Mitgeliefert/`, `.berichtsvorlagen.json`) kam mit dem Sync-Commit `117758e1` herein — Laufzeiterzeugnis | entfernen und in `.gitignore` |
| Logbuch | Versionsnummer für die zwei Sätze in `Werkzeuge/WikiUpload/logbuch_bv_offen.wiki` (BV-E1/BV-E2) | 1.2.0.5 wie die übrigen |
| Wiki | Sammel-Upload mit „Berichtsvorlagen“ (Neuanlage) und den Logbuchsätzen 1.2.0.5 in [`Wiki_Update_2026-09-26.md`](Wiki_Update_2026-09-26.md) — nur durch den Anwender | höchstens einmal je Woche |
| iOS | Lauf zu den Berichtsvorlagen (Nach #566 d, Nach #556 b) | nur nach Rückfrage |

### 2.2 Abnahmen, die nur der Anwender unter Windows leisten kann

Word-/Excel-Vorlage, Schnellbausteine und Sprache aus der Vorlage (Nach #566 a); Meldung
„Überwachter Ordnerzugriff“ (Nach #566 b); Platzhaltermarken (Nach #581 d, BV-E6 a); Sichtprobe
Excel-Diagramme (Nach #558 a); Tippprobe Word und Nachweis mit Word 365 (Nach #500 a, b).

### 2.3 Ohne Entscheid möglich

- Nach #581 (b): Positionsform auch auf der Wirtschaftlichkeitsseite (Sensitivität,
  Mehrjahrestafel, Zahlungsstrom).
- Nach #558: Platzhalteranzeige `tabelle.*` → Zellmarke; Reste aus Nach #549/#556/#558, soweit
  nicht mit BV-E9 erledigt.
- Nach #500 (e): `WirtschaftlichkeitCtrl.StelleTabellenSicher` als eigener kleiner Auftrag.

## 3 Regeln dieser Sitzung

- Es gelten `CLAUDE.md` und die Projekt-`CLAUDE.md`. Modell ausdrücklich je Agent
  (`sonnet` Suchen und Papiere, `opus` Umsetzung und Fehlersuche, `haiku` Zählungen).
- **Welle:** Befund → Entscheide „nach Empfehlung“ → Bau in Commits je Schritt → Merge
  → Gate `Werkzeuge/Gate/gate_linux.sh <Nr>` → Statuszeile, „Nach #…“-Block und Protokoll unter
  `Dokumentation/ueberholt/Protokolle/Bericht/` → `git fetch`, origin mergen, Nachtest → Push
  → Kern-Lauf beobachten und die Kennung nachtragen.
- **Push** nur auf Zuruf (CLAUDE.md); der Anwender entscheidet, ob wie bei „Dialoge und
  Korrekturen“ nach grünem Gate ohne Rückfrage auf `ios_migration_september` gepusht wird.
  Nie `main`, nie Force, keine Tags. **Kein iOS-, macOS- oder Setup-Lauf ohne Rückfrage.**
- **Bilder und Vorlagen:** Nach jeder Änderung am Renderer `dotnet run --project Proben/ChartProben -c Release`
  gegen die Linux-Messlatte; eine gewollte Bildänderung schreibt die Messlatte im selben
  Schritt fort. Nach jeder Vorlagenänderung `Werkzeuge/Berichtsvorlage` ziehen (nur bei grünem
  `OpenXmlValidator`); `BerichtsvorlageDateiWacheTests` und `AuslieferungsvorlagenWacheTests`
  halten Dateien und Lieferwege.
- **Gate bei Schemaschritten** zusätzlich `dotnet test Werkzeuge/Auslieferungsvorlage/Auslieferungsvorlage.sln -c Release`.
- Konflikte in `.resx`: beide Seiten vereinigen, XML auf Wohlgeformtheit prüfen, Designer
  (`python3 Werkzeuge/ResourceDesigner/designer_neu.py`) im Prüfmodus ziehen. Statusdatei:
  origin-Fassung nehmen, eigene Zeilen gezielt einfügen.

## 4 Nachbarsitzungen (alle in der Cloud)

„Dialoge und Korrekturen“, „Gebäudesimulation“ (Schema ab 152), „Zapfprofil“, „EPOS Plan
Wirtschaftlichkeit“ — je mit Übergabepapier in `Dokumentation/aktuell/`. Abstimmung allein über
`origin`: Statusnummern, Schemaschritte, LFS-oid der Testdatenbank; vor jedem Push `git fetch`
und mergen, bei Konflikt der Testdatenbank die origin-Fassung nehmen und eigene Skripte unter
`Referenzlaeufe/Skripte/` darauf neu anwenden.

## 5 Einrichtung der Cloud-Umgebung

Der frische Container hat weder `dotnet` noch `git-lfs`; die Testdatenbank ist dann eine
Zeigerdatei von 133 Byte. Einmalig in der Umgebung (Menü der Umgebung in der Titelzeile der
Sitzung → *Edit* → *Setup script*) hinterlegen:

```bash
#!/bin/bash
set -e
curl -sSfL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --version 10.0.400 --install-dir "$HOME/.dotnet"
ln -sf "$HOME/.dotnet/dotnet" /usr/local/bin/dotnet
apt-get update -qq && apt-get install -y -qq git-lfs
git lfs install
```

In der Sitzung dann einmal `git lfs pull --include Referenzlaeufe/Kenndaten_Test.sqlite`
(71 MB). Die Netzregel muss `dot.net`/`builds.dotnet.microsoft.com`, `nuget.org`,
`api.github.com` und die LFS-Ablage von GitHub zulassen. Python-Werkzeuge laufen mit `python3`.
Die Windows-Schale baut mit `-p:EnableWindowsTargeting=true`; ein Anwender-Build entsteht nicht.

**Nicht im Repositorium** und in der Cloud nicht vorhanden: das Gesprächsgedächtnis der
Vorgängersitzung (sein fachlicher Inhalt steht in Statusdatei, Protokollen und diesem Papier),
lokale Materialordner (`C:\Waermeplan`, `Z:`), die Windows-Messlatte der ChartProben
(`gate_windows.sh`) und Musterberichte wie `Bericht_1019_final.xlsx`.

## 6 Einstieg in der neuen Sitzung

1. Dieses Papier und `CLAUDE.md` lesen; `git fetch`, Arbeitszweig auf
   `origin/ios_migration_september` bringen, `git lfs pull`.
2. Probe: `dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly`.
3. Anwender nach den Entscheiden aus 2.1 fragen; ohne Entscheid mit 2.3 beginnen.
