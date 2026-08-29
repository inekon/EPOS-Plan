# Betriebshinweise für Installer und Auslieferung

Stand: 29.08.2026. Regeln, die bei Installer-Bau und Auslieferung einzuhalten sind —
Verstöße haben nachweislich Fehlerbilder erzeugt (Fundstellen jeweils genannt).

## 1. Niemals `help_mapping.txt` neben die EXE legen

Die Zuordnung der Info-Buttons ist seit H2 (29.08.2026) als **eingebettete Ressource**
in der Anwendung enthalten. Eine Datei `help_mapping.txt` im Programmordner wirkt als
**Auflage** für einzelne Zeilen (gewollt, für Sonderfälle im Feld) — eine dort liegende
**Altdatei** friert aber die von ihr genannten Zuordnungen ein und veraltet unbemerkt.

Vorfall: Eine Restdatei vom 28.08. im `bin`-Ordner hat am 29.08. **24 von 26
Info-Buttons abgeschaltet** (damals galt die Datei noch als vollständiger Ersatz;
seither Auflage-Semantik). Ursache, Beweis und Fix:
[`WindowsFormsApplication1/Allgemein/Hilfe/H1H2_Umsetzung_Protokoll.md`](WindowsFormsApplication1/Allgemein/Hilfe/H1H2_Umsetzung_Protokoll.md),
Abschnitt 14.

**Regel:** Der Installer packt **keine** `help_mapping.txt` und **keine**
`help_cache.json` in den Programmordner; bei Updates vorhandene Exemplare dort
**entfernen**.

## 2. Benutzerprofil-Ablagen nicht mit ausliefern

Alles unter `%APPDATA%` ist je Benutzer selbstheilend und gehört **nicht** in den
Installer:

| Ablage | Inhalt | Verhalten |
|---|---|---|
| `%APPDATA%\<Produktname>\help_cache.json` | Sicherung des Hilfe-Katalogs | wird vom ersten erfolgreichen Onlinelauf geschrieben/erneuert |
| `%APPDATA%\wp-plan\wiki-wissen\` | Textauszüge für den Hilfe-Assistenten (24-h-Gültigkeit) | baut sich selbst auf |
| `%APPDATA%\wp-plan\semantik\` | Semantik-Modell (117 MiB) + Einbettungsindex | einmaliger, SHA-256-geprüfter Download beim ersten Gebrauch; ohne Netz stiller Rückfall auf die Stichwortsuche |
| `%APPDATA%\wp-plan\ki-schluessel.dat` | DPAPI-verschlüsselter API-Schlüssel | benutzergebunden, niemals kopieren/verteilen |

## 3. Netzziele der Anwendung (für Firewall-Freigaben beim Kunden)

| Ziel | Zweck |
|---|---|
| `https://wiki.epos-plan.de` | Hilfe-Katalog, Wiki-Suche, Textauszüge (anonym, nur lesend) |
| `https://translate.goog` (Host `wiki-epos--plan-de.translate.goog`) | englische Anzeige der Hilfeseiten (nur bei englischer Oberfläche, beim Öffnen im Browser) |
| `https://generativelanguage.googleapis.com` | Hilfe-Assistent (nur mit Einwilligung und eigenem Schlüssel) |
| `https://huggingface.co` (versionsgepinnte Datei) | einmaliger Bezug des Semantik-Modells (siehe `Allgemein/KI/H10_SemantikIndex_Protokoll.md`) |
| `https://epos-plan.de` | Lizenzserver, AGB-Abgleich, Lizenzportal |

## 4. Bestehende Regeln (Querverweise, hier nicht wiederholt)

- `C:\ProgramData\EPOS_PLAN`-ACL und Mehrbenutzer-Sperrdatei:
  [`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md).
- Build zwingend **x64**, ACE-OLEDB 64-Bit:
  [`Konzept_Umstellung_64Bit_EPOS-Plan.md`](Konzept_Umstellung_64Bit_EPOS-Plan.md).
- Vor Releases `dotnet list package --include-transitive` prüfen (Lizenzregel
  SixLabors, `WindowsFormsApplication1/CLAUDE.md`); seit H10 zusätzlich an Bord:
  `Microsoft.ML.OnnxRuntime` und `Microsoft.ML.Tokenizers` (beide MIT) sowie das
  Semantik-Modell (Apache-2.0, nicht Teil des Installers).
