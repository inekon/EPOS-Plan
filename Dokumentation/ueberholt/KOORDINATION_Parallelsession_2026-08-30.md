# Koordinationsübergabe — laufende B5-Vorbereitungssession (30.08.2026)

Zweck: Diese Datei wird in eine ANDERE Claude-Task importiert, damit sie der hier
laufenden Session nicht in die Quere kommt. Sie beschreibt Stand, reservierte
Bereiche, bindende Regeln und freie Flächen. Temporäres Dokument — nach Abschluss
der B5-Etappe löschbar.

## ⛔ HÖCHSTE PRIORITÄT DIESER SESSION: WIRTSCHAFTLICHKEIT

**Das gesamte Themenfeld Wirtschaftlichkeit — Dialoge, Eingaben, Berechnung — ist
der aktive Hochprioritätsbereich dieser Session und für jede andere Task GESPERRT.**
Das umfasst inhaltlich (nicht nur dateiweise):

1. **Berechnung**: Kapitalwert-Rahmen (`KapitalwertRechner`), Invest-Kaskade
   (`LiesInvestitionen`/`TechnikPlanwertCtrl`), Betriebskosten-Bemessungsarten
   (`BetriebskostenCtrl.Betrag`, `EndenergieAufloeser`), Energiekosten/CO₂
   (`KostenEmissionRechner`, `EmissionsFaktorLader`, Aufschlagsblock), Vergütungen
   (KWKG-Satz/-Kontingent/-Reihe, `HilfsstromRechner`, PV: `EegSatzRechner`/
   `PvErloesRechner`, `StromTarifRechner`), Steuern (`SteuerGutschriftRechner`,
   `GesetzKatalog`), Kohärenz (`KohaerenzPruefung`).
2. **Dialoge/Eingabe**: `Form_WirtschaftlichkeitParameter`, `Form_KwkgModule`,
   `Form_PhotovoltaikVerguetung`, `UcWirtschaftlichkeit`, `ucErtragBonus`,
   `ucStromAufschlaege`, `ucBrennstoffBestandteile`, `ucFuelSettings` — plus der
   in B5 NEU entstehende `Form_BhkwWirtschaftlichkeit` (BW9) samt Schreibweg der
   drei B3-Anlagenspalten (`KwkgAnlagenCtrl`-Erweiterung).
3. **Persistenz dieses Felds**: `Tab_ProjektWirtschaftlichkeit`,
   Wirtschaftlichkeits-Spalten an `Tab_Energieanlagen` (KWKG_*, Energiesteuer_Wahl,
   Aufteilung_Methode, Hilfsenergie_Anteil), `Tab_ProjektWerte` Kategorien 1/2,
   `energy_project_settings`-Aufschlags-/Bestandteilspalten, `Tab_Gesetzesparameter`.

**Grundlagendokumente dieser Session** (für die andere Task NUR Lektüre, nie Basis
eigener Änderungen in diesem Feld):
- Formelkarte der Rechenwege (vollständig, mit Befunden I-1…R-3):
  `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan-WP-Plan-WindowsFormsApplication1\665cd065-9d77-4183-9e12-3625de8389e3\scratchpad\rechenwege_formelkarte.md`
- Feldkarte der B5-Dialoge (alle Felder, Lücken K1–K11):
  ebd. `…\scratchpad\b5_feldkarte.md`
- Etappenprotokolle: `WindowsFormsApplication1\Allgemein\Reporting\`
  (H1–H4b, H21, B1–B4a/b, BK1/BK2, HB1, W4_E3–E7, K5/K6).

Wer in einer anderen Task einen BERÜHRUNGSPUNKT mit diesem Feld findet (z. B. eine
Datei, die auch Wirtschaftlichkeits-Werte liest), ändert dort NICHTS, sondern
vermerkt den Punkt als offene Frage an diese Session.

## 1. Stand dieser Session

- Branch **`Pufferspeicher`**, HEAD **b2ad3e3** („F2/B4: resx-Sammelnachtrag …").
  Committet und abgeschlossen heute: B2 (60c018a), B3a (a3c4b35), B3b (e02aab3),
  B4 (ac296e3), HB1 (dc2e224), BK1 (bb19452), BK2 (109e71f), F2 (25c0f05),
  resx-Nachträge (bad41f8, b2ad3e3), neue Referenzbasis-LIESMICH (9912228).
- **Konzeptstand BHKW-Wirtschaftlichkeit: B1–B4 fertig.** Aktuell läuft die
  **B5-VORBEREITUNG** (Anwenderauftrag): zwei Opus-Agenten erstellen (a) die
  Rechenwege-Formelkarte der Wirtschaftlichkeit (rein lesend) und (b) das
  Dialog-Mockup `scratchpad\b5_mockup.html`. Danach: zwei Artifacts zur
  Anwenderprüfung. **Die B5-IMPLEMENTIERUNG ist noch nicht gestartet** und wartet
  auf die Prüfung.
- Neue eingefrorene Referenzbasis: **`Referenzlaeufe\2026-08-30_B3-Kaskade`**
  (Codebasis bad41f8; 1030 wieder Zwei-Modul-BHKW). Der Basis-Ordner ist
  **untracked und bleibt unversioniert** (nur das LIESMICH ist committet).

## 2. Von DIESER Session reserviert — NICHT anfassen

| Bereich | Grund |
|---|---|
| `WindowsFormsApplication1\Allgemein\Wirtschaftlichkeit\*` | B5-Kernbaustelle (KwkgAnlagenCtrl-Erweiterung um die drei B3-Spalten kommt hier) |
| `WindowsFormsApplication1\Views\Wirtschaftlichkeit\*` (inkl. `Form_KwkgModule`, `Form_WirtschaftlichkeitParameter`, `UcWirtschaftlichkeit`, `ucErtragBonus`) | B5 baut hier den neuen Dialog `Form_BhkwWirtschaftlichkeit`, zieht Gruppen um, ändert die Fußleiste |
| `WindowsFormsApplication1\Views\Kosten\ucStromAufschlaege.*`, `ucBrennstoffBestandteile.*`, `ucFuelSettings.*` | frisch aus B2/B4, B5/B6 hängen daran |
| `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` (Repo-Wurzel) | wird je Etappe fortgeschrieben |
| `WindowsFormsApplication1\Allgemein\Reporting\B*_*.md`, `H*_*.md`, `BK*_*.md`, `HB1_*.md` | Protokollreihe dieser Serie |
| `MyResource\Resource.resx` + `Resource.en-US.resx` | Sammelnachtrags-Verfahren dieser Session (Konfliktgefahr bei parallelen Einträgen) |
| `Allgemein\Update\SchemaKatalog.cs` + `SchemaMigration.cs` | Schema-Nummernraum (siehe § 4) |

## 3. Bekannte FREMDE, unkommittete Arbeit im Arbeitsbaum (auch nicht anfassen, nicht committen)

- `WindowsFormsApplication1\Allgemein\BhkwPlan.cs` — eine andere Session arbeitet
  am **Rechenkern** (MonatsGrenzen-Block). Die neue Referenzbasis
  `2026-08-30_B3-Kaskade` ist der Maßstab, gegen den DIESER Umbau zu prüfen ist.
- `WindowsFormsApplication1\MyResource\Resource.Designer.cs` — von Visual Studio
  regeneriert, trägt gemischte Keys (u. a. fremdes `WIZ_BTN_SPEICHERN`). Wird
  bewusst NICHT gezielt committet; der Anwender-Sync sammelt sie ein.
- `WindowsFormsApplication1\Views\Wizard\*`, `Views\Projekt*` — Wizard-/
  Projektdialog-Session.
- **BK3 liegt fertig auf Seitenzweig `claude/nostalgic-matsumoto-481128`
  (2ab47b1, Basis b2ad3e3): Merge nach `Pufferspeicher` ist OFFEN.** Nicht
  duplizieren; Merge nur koordiniert, wenn keine Parallelschreiber aktiv sind
  (Details im Memory `traegerzuordnung-kostenseite-befund`).

## 4. Bindende Regeln für JEDE Task in diesem Repo

1. **Kein `git push`** — pushen macht ausschließlich der Anwender-Sync
   (GitHub_Sync.bat). Je Etappe ein eigener Commit-Block mit Protokoll-`.md`.
2. **Neue Schemaschritte ab 62** (`ZIEL_VERSION` = 61). Reihenfolge zwingend:
   erst Schrittmethode + SCHRITTE-Eintrag implementieren, DANN `ZIEL_VERSION`
   erhöhen — nie umgekehrt (E6-Vorfall).
3. **Modellwahl**: Fable 5 nur Orchestrierung; Implementierung, Erhebung,
   Reviews, Dokumente → Opus-5-Subagenten (`model: "opus"`).
4. **Produktiv-DB** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` NIE beschreiben;
   lesen nur `Mode=Read` (die Anwender-App kann laufen); Schreibproben gegen
   frische Kopien im Session-Scratchpad; Harnesse nur unter `..\dev\`
   (gitignored — eine `.cs` unterhalb `WindowsFormsApplication1\` bricht den
   Build).
5. Build nur VS-MSBuild x64 (`dotnet build` scheitert an COM); läuft die App,
   `-p:OutDir=<außerhalb>` umleiten. Bekannte Altwarnungen: CS0108×2, CS0109×2,
   CS1998.
6. **Kodierung**: vor jedem Edit prüfen; nicht-UTF-8-Dateien nur mit
   ASCII-Einfügungen. Neue Anzeigetexte über GetString-Rückfallmuster (TKd4),
   `Resource.Designer.cs` nie von Hand (VS läuft und regeneriert).
7. Vor jedem Commit `<<<<<<<`-Sweep; gezieltes `git add` (nie `add -A` — der
   Arbeitsbaum trägt Fremdarbeit).

## 5. Stabile Regressionsanker (Stand heute)

- `LiesBetriebskosten(1024)` = **99,00 €/a**; Kapitalwert 1024 = **−2.220.322,32**.
- `LiesInvestitionen`: 1018 = 45.312,50 · 1024 = 12.001,00 · 1042 = 13.000,00.
- 1030-Werte sind NICHT mehr ankerfähig (Anwender-Umbau der Kaskade heute);
  Referenz = Basis `2026-08-30_B3-Kaskade`.

## 6. Offene Anwenderentscheide (geparkt, nicht von einer Task „lösen")

- B5: K3 (BW3-Modusfeld vorziehen oder B6) und K8 (Fußleistenplatz) — warten auf
  Mockup-Prüfung; K1–K11 vollständig in `scratchpad\b5_feldkarte.md` bzw. im
  kommenden Mockup-Artifact.
- BK3-Protokoll § 6: fünf Entscheide (Bestands-0-Preiszeilen, Wizard-Preiskopie,
  Grundpreis-Schattierung, Einheitenbruch m³/Nm³, Dialog-Rückschreiben).
- A8-Altanwendungsprobe: wartet auf Zulieferung Alt-Excel.
- HB1-O1: Engine-Sortierung (`ORDER BY Prioritaet` im Rechenweg) — nur mit
  vollem Referenzlauf.
- Sichtabnahmen: B2-Brennstoffblock, BK1-Kosten-Seite, F2-Badge, B4-Hervorhebung.

## 7. Freie Flächen für eine andere Task

Alles außerhalb von § 2/§ 3 — z. B. Klimadaten/Klimazonen, Hilfe/KI-Module,
Berichts-Layout (`Allgemein\Bericht\Word/Excel`-Generatoren; Vorsicht:
`BerichtsDaten*.cs`/`KostenEmissionRechner.cs` sind frisch committet, mergefähig
bleiben), Simulation außerhalb von `Hydraulikbild`/`Warnkriterien`/
`Ladeordnung` (heute geändert, aber committet), Import-Module, Lizenz. Bei
Überschneidungsverdacht: zuerst `git log --oneline -15` und diese Datei prüfen.
