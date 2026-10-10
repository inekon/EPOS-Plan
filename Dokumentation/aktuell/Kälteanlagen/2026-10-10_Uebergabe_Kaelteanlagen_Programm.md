# Übergabe Kälteanlagen, Teil Programm — Sitzung „Gebäudesimulation“ an Sitzung „Kälteanlage“, Stand 10.10.2026

Dieses Papier hält, was die neue Sitzung „Kälteanlage“ braucht, um den Kälteteil ohne Verlust fortzuführen: Abgrenzung, Stand,
nächste Arbeit, Papiere, Arbeitsweise und am Ende den Einstiegs-Prompt. Regelquelle bleibt [`CLAUDE.md`](../../../CLAUDE.md) mit
den Projekt-`CLAUDE.md`; der dauerhafte Stand steht in der [Statusdatei iOS-Migration](../Status_iOS_Migration.md) und in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (Entscheide E117–E121, Stufe KAE). Die Arbeitsweise
folgt der [Übergabe vom 08.10.2026](../Gebaeudesimulation/2026-10-08_Uebergabe_Gebaeudesimulation_Kontowechsel.md), Abschnitt 6.

**Zwei Übergabepapiere.** Am selben Tag hat eine zweite, gleichnamige Sitzung „Gebäudesimulation“ (Konto 1, Statuszeile #912) die
[Übergabe des gebauten Kältestands](2026-10-10_Uebergabe_Kaelteanlagen.md) geschrieben (KU3, AK3-K, Kühlkurve, KM1–KM3, Code-Landkarte).
Dieses Papier ergänzt sie um das **Kälteprogramm** der Sitzung „Gebäudesimulation“ auf Konto 2 (session_01X2CX69qnhXXtc6epxnPUv6):
Entscheide E117–E121, die Wellen K-A, KB-A, VDI-K, K-C und die Wellenfolge. Die Kopfzeilen **R51** (eingefroren mit #916) und die
Schemaschritte **211** (gebaut mit #919) und **212** (KB-D, wird von dieser Sitzung gebaut) gehören dieser Sitzung, nicht der Sitzung
„Kälteanlage“; ab 213 meldet die Sitzung „Kälteanlage“ selbst an.

## 1. Anlass und Abgrenzung

Der Anwender hat am 10.10.2026 den Kälteteil aus der Sitzung „Gebäudesimulation“ in die Sitzung „Kälteanlage“ ausgegliedert
(eigener Container für Rechenleistung und Kontext; gleicher Arbeitszweig `ios_migration_september`, gleiche Regeln).

- **Sitzung Kälteanlage:** K-B, K-D, K-E, K-F, K-H samt Aufwand und Entscheiden dazu.
- **Sitzung Gebäudesimulation:** behält Gebäudemodell und Aufheizung (Entwurf [Vorheizrampe](../Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md),
  Fassung 2 in Arbeit) und schließt KB-B und KB-D ab; K-C ist gebaut (#923).
- **Sitzung Dialoge und Korrekturen:** baut Stufe 5 der Katalogauswahl (Dialogumbau Kältemaschine; E117 F6).

## 2. Stand

| Welle | Inhalt | Stand |
|---|---|---|
| FK | Freie Kühlung, Referenzprojekt 1064, Basis R51 | gebaut (#916) |
| K-A | Katalogfelder der Kälteerzeuger, Schemaschritt 211 (E118) | gebaut (#919) |
| KB-A, KB-1 | Kältefolge als eine Quelle, Daten des Kältebereichs, eigene Projektkopie je Anlage (E117) | gebaut (#920) |
| VDI-K, VDI-K2 | Import Kälteanlagen VDI 3805 (E119, E121) | gebaut (#921) |
| Entscheide | E117–E121, Entwurf Split/VRF/Rückkühlwerk | #922 |
| KB-B | Bereich „Kälte“ in `SimulationKonfigSeite.razor` | in Arbeit (Gebäudesimulation) |
| KB-D | Schemaschritt 212 `KaelteRangSchema` (pflegbare Kältefolge) | in Arbeit (Gebäudesimulation) |
| K-C | CSV-Varianten Nennwerte und Ökodesign A–D, Menüpunkt „Import Kältemaschinen (CSV, Copper)“ | gebaut (#923) |

Referenzbasis **R51** (29 Projekte), Testdatenbank auf Schemaschritt **211** (nach KB-D 212), nächster freier Schemaschritt
**213**, nächste Basis **R52**. Maßgeblich ist der Kopf der Statusdatei, vor jeder Vergabe gegen origin messen.

## 3. Nächste Arbeit der Sitzung Kälteanlage

Reihenfolge nach E118 und E120 (KD-Q10). Aufwand je Stufe: [Entwurf Split/VRF/Rückkühlwerk](2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md)
(33,5–47 PT) und [Konzeptprüfung](2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md).

1. **K-F1** Rückkühlwerk als Glied mit Festwerten, Referenzlauf byte-gleich; danach K-F2 bis K-F5.
2. **K-B** neutraler Startkatalog aus den EnergyPlus-Kurven (raw.githubusercontent.com ist erreichbar; Lizenz BSD prüfen;
   keine Hersteller- und Modellnamen ausliefern).
3. **K-D1 bis K-D5** Split und Multisplit, Typ 14 „Raumklimagerät“ (nutzt den A–D-Leser aus K-C).
4. **K-E1 bis K-E4** VRF.
5. **K-H** EPREL-Abruf erst nach K-D; den API-Schlüssel beantragt der Anwender.
6. **KB-C** Proben und Wiki des Kältebereichs, nach KB-B.

**Offene Punkte:** VDI-3805-Dateien holen, sobald der Anwender die Hosts freigibt (vdi3805-bim.de, bim4hvac.com, vdi3805.org,
Herstellerseiten; Quellenliste in `VDI-3805-Daten/Quelle Daten.txt`, Zeile #921 der Statusdatei). Die Luftfeuchte der
Klimadaten in der Testdatenbank ist leer (Befund aus dem Entwurf Split/VRF/Rückkühlwerk); das betrifft den Nasskühler (K-F2).

## 4. Papiere

- [Konzeptprüfung Kälteanlagen, Katalog und Import](2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md)
- [Recherche Herstellerdaten und Rechenmodelle (08.10.)](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md)
- [Umsetzung KM1 Typkennfelder](2026-10-09_Umsetzung_KM1_Typkennfelder.md)
- [Entwurf Split, VRF, Rückkühlwerk](2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md)
- [Entwurf Kältebereich](../Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md)
- [Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (§5, §14 mit Fortschreibung E118)
- [Konzept Katalogauswahl](../Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md) (Stufe 5)
- Protokolle: [K-A](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_K-A_Katalogfelder_Kaelte.md),
  [KB-A](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_KB-A_Kaeltefolge_Projektkopie.md),
  [VDI-K](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_VDI-K_Kaelteimport.md)

## 5. Arbeitsweise

Wie in Abschnitt 6 der Übergabe vom 08.10.2026:

- **Rollen:** Die Sitzung orchestriert; `opus-umsetzung` für Rechenweg, Schema, Tests, Hüllen, Dialoge; `sonnet-mechanik` für
  Statuszeilen, Protokolle, Merges ohne Fachkonflikt; `haiku-pruefung` für Zählungen. Modell bei jedem Aufruf setzen. Je Welle
  ein Worktree, Agenten committen dort und pushen nie.
- **Wellenfolge:** Entwurf mit Fragen, Anwenderentscheid als E-Zeile, Schemaschritt bzw. Basis im Kopf anmelden, Wellen,
  Merge auf aktuellem origin, Gate, Statuszeile und Protokoll, Push, CI-Vermerk.
- **Gate in der Cloud:** `Werkzeuge/Gate/gate_rest_linux.sh` lokal plus Kern-CI auf dem Sitzungszweig.
- **Push:** `git -c "lfs.https://github.com/inekon/EPOS-Plan.git/info/lfs.locksverify=false" push origin <sha>:refs/heads/<zweig>`.
- **Nummern** (Statuszeile, Schemaschritt, Basis) unmittelbar vor dem Merge gegen origin messen; mehrere Sitzungen vergeben
  gleichzeitig.
- **Abstimmung** zwischen den Sitzungen per Nachricht.
- **.NET-SDK fehlt im Container:** Bezug über das Debian-12-Paketarchiv von packages.microsoft.com (`dotnet-sdk-10.0` 10.0.401
  und Abhängigkeiten per `dpkg -x` nach `/root/.dotnet`), weil builds.dotnet.microsoft.com gesperrt ist.
- **Plattenplatz ist begrenzt:** fertige Worktrees sofort entfernen.

## 6. Einstiegs-Prompt für die Sitzung Kälteanlage

```
Du übernimmst die Sitzung „Kälteanlage“ für EPOS-Plan (Repo inekon/EPOS-Plan, Arbeitszweig ios_migration_september).
Lies zuerst CLAUDE.md und Dokumentation/aktuell/Kälteanlagen/2026-10-10_Uebergabe_Kaelteanlagen.md vollständig, dann
Dokumentation/aktuell/Kälteanlagen/2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md, den Entwurf
Dokumentation/aktuell/Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md, die Entscheide E117–E121 und die Stufe KAE in
Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md, den Kopf und die letzten zwanzig Zeilen der Tabelle in
Dokumentation/aktuell/Status_iOS_Migration.md sowie `git log --format='%h %<(72,trunc)%s' -n 25 origin/ios_migration_september`.

Rolle: Du orchestrierst — zerlegen, abnehmen, zusammenführen, Rechenwegentscheide, Antworten an mich. Umsetzung über die
Agentendefinitionen unter .claude/agents/: opus-umsetzung, sonnet-mechanik, haiku-pruefung; Modell bei jedem Aufruf
ausdrücklich setzen. Je Welle ein Worktree, Agenten committen und pushen nicht. Wellenfolge: Merge auf aktuellem origin → Gate
(Werkzeuge/Gate/gate_rest_linux.sh lokal plus Kern-Lauf der CI auf dem Sitzungszweig) → Statuszeile und Protokoll → Push →
CI-Vermerk. Statusnummer, Schemaschritt und Basis unmittelbar vor dem Merge gegen origin messen. Nie ohne meine Freigabe:
macOS-, iOS- und Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags. Keine Hersteller- und Modellnamen in Auslieferung
und Wiki. Antworten auf Deutsch, knapp, Zahlen in Tabellen. Abschnitt 5 der Übergabe gilt (SDK-Bezug, Plattenplatz).

Erste Schritte: (1) Stand prüfen — git status sauber, keine Konfliktmarker, keine liegen gebliebene AGENT_LAEUFT, Kopf der
Statusdatei (Schemaschritt 211/212, nächster freier 213, Basis R51, nächste R52). (2) Den Stand in höchstens fünf Zeilen melden.
(3) Auf den Push von KB-B und KB-D durch die Sitzung Gebäudesimulation warten bzw. danach origin mergen. (4) K-F1
(Rückkühlwerk als Glied mit Festwerten, byte-gleich) mit Aufwandsschätzung vorschlagen; ohne meinen Auftrag keine neue Welle
beginnen.
```
