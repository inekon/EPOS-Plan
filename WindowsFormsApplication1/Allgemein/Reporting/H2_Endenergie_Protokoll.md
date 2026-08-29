# H2 — Bezugsgrößen-Auflöser der Endenergie-Bemessungen (Protokoll)

Etappe H2 des [`Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](../../../Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md)
(§ 4.5) — der als **blockierend** geführte offene Punkt **H1-1** aus
[`H1_Pflichtpositionen_Protokoll.md`](H1_Pflichtpositionen_Protokoll.md). Umgesetzt am
**29.08.2026** auf Branch `Pufferspeicher`; Commit ohne Push (Etappenregel § 8).

Kern der Festlegung vom 29.08.2026: **Die Menge ist ein Ergebniswert, kein Eingabewert.**
Eine Position mit `PROZENT_ENDENERGIEKOSTEN` (Weg A) oder `PROZENT_ENDENERGIEBEDARF`
(Weg B) holt ihre Bezugsgröße bei **jedem Lesen frisch aus dem jüngsten Simulationslauf**;
`Tab_ProjektWerte.Menge` bleibt Ausweisgröße. Ohne Lauf gibt es keine Menge und damit
den dokumentierten Betrag 0.

---

## 1 Umgesetzt

### 1.1 `EndenergieAufloeser` (neu, `Allgemein/Wirtschaftlichkeit/`)

| Komponente | Endenergie | Quelle |
|---|---|---|
| BHKW (7) | Brennstoff | `ErgebnisBHKWModulModel.Verbrauch` × Arbeitspreis des `CarrierId` |
| Heizkessel (2) | Brennstoff | `ErgebnisHeizkesselModulModel` ebenso |
| Wärmepumpe (1) | Strom | (`Stromverbrauch` + `Heizstab`) × Strombezugspreis |
| Solar/Puffer/Stromspeicher/PV | keine (§ 4.5) | Auflöser liefert null — nur Weg C zulässig |

- **Anlagenscharf mit Komponentensumme als Rückfall:** Trägt die Position eine
  `ID_Anlage` (Schritt 45), zählen nur die Modulzeilen dieser Anlage — Zuordnung über
  den **Bezeichner** (die Ergebnis-Modulzeilen führen keinen Anlagenschlüssel; dasselbe
  Verfahren wie `ErdreichAuswertung`). Ist die Anlage im Lauf nicht vertreten, gibt es
  bewusst **keine** Bezugsgröße statt der Summe einer fremden Anlage.
- **Kosten und Bedarf getrennt tragfähig:** Fehlt der Arbeitspreis eines beteiligten
  Trägers, bleibt `KostenEuro` null (Weg A ohne Basis), der `BedarfKwh` bleibt
  bestimmbar (Weg B funktioniert weiter).
- Der jüngste Lauf kommt über `ErgebnisCtrl.Load` — derselbe Weg wie
  `BetriebskostenCtrl.LiesBrennstoffkosten`.

### 1.2 Preise aus der EINEN Wahrheit (`KostenEmissionRechner`)

Zwei schmale `internal`-Zugänge **neben** der in E3 referenzgeprüften Kostenschleife
(sie selbst bleibt unangetastet — Rechenweg-Disziplin):

- `ArbeitspreisJeKwh(idProjekt, carrierId)` — Direktabrechnung oder Umrechnung über
  `eff_hi`; algebraisch gleich der Schleifenformel `Verbrauch × 1000 / eff_hi × Preis`.
  **Ohne** Grund- und Leistungspreis: trägerweite Fixbeträge lassen sich keiner Anlage
  zurechnen („Verbrauch des Moduls × Trägerpreis", § 4.5).
- `StromTraegerId(idProjekt)` — Hülle um das vorhandene `FindeStromTraeger`.

### 1.3 Einbau in den Rechenweg (`WirtschaftlichkeitCtrl`)

In `LiesBetriebskosten` (Summenschleife) **und** `LiesBetriebskostenPositionen`
(E7-Nachweis, muss die Summe treffen):

- SELECT um `KomponentenID` erweitert; `ID_Anlage` nur, wo Schritt 45 gelaufen ist
  (`AnlagenSpalteVorhanden()`, gleiches Probenmuster wie `StartjahrSpalteVorhanden`).
- Bei den zwei Endenergie-Arten ersetzt `EndenergieMenge(...)` die DB-Menge: Weg A
  liefert die Arbeitskosten [€/a], Weg B den **bewerteten** Bedarf
  (kWh × Strombezugspreis) — `Menge × Satz / 100` ergibt so ohne zweite Formel den
  Betrag (Kommutativität, Begründung bei `BetriebskostenCtrl.Betrag`).
- **Alle übrigen Bemessungsarten lesen unverändert die persistierte Herleitung** —
  der Zweig wird von Bestandszeilen nicht betreten (Nachweis 2.1).
- Der Auflöser wird je Aufruf höchstens einmal gebaut, und nur wenn eine
  Endenergie-Position vorkommt.
- Szenarienvorfahrt (VALERI-Muster) und Startjahr-Behandlung (KD6) unverändert.

---

## 2 Nachweise

**Build:** VS-MSBuild x64 Debug, OutDir umgeleitet — **grün**; Warnungsprofil exakt der
Altbestand (2× CS0108, 2× CS0109, 1× CS1998).

**Harness `..\dev\h2endenergie\`** (gitignored, **nur lesend** gegen die
Produktivdatenbank; Reflection auf die frisch gebaute DLL; die App war während des
Laufs geöffnet — ausschließlich SELECTs):

### 2.1 Ergebnisneutralität

`SELECT COUNT(*) … WHERE Bemessung IN ('PROZENT_ENDENERGIEKOSTEN','PROZENT_ENDENERGIEBEDARF')`
→ **0 Zeilen** im gesamten Bestand. Der neue Zweig wird von keiner Bestandszeile
betreten; zusätzlich lief `LiesBetriebskosten` für **alle 23 Projekte mit
Simulationslauf** fehlerfrei durch (Summen u. a. 0 / 99 / 275 / 600 €/a — Bestandswerte).

### 2.2 Bedarfs-Gegenrechnung (unabhängiges SQL)

Für alle 23 Projekte: Auflöser-Bedarf gegen `SUM(Verbrauch)` bzw.
`SUM(Stromverbrauch)+SUM(Heizstab)` der Modulzeilen des jüngsten Laufs — **durchgehend
GLEICH**, u. a.:

| Projekt | Komponente | Bedarf [kWh/a] | Kosten [€/a] |
|---|---|---|---|
| 1024 | BHKW | 228.930 | 7.154,06 (Arbeitspreis der Trägerkette) |
| 1024 | Wärmepumpe | 43.030 | 15.060,50 (× 0,35 €/kWh) |
| 1035 | Wärmepumpe | 448.080 | 156.828,00 |
| 1019/1023 | Wärmepumpe | 112.020 | 39.207,00 — Handrechnung 112.020 × 0,35 exakt |
| 1018 | BHKW | 1.530 | — (kein Trägerpreis gepflegt → Weg A bewusst ohne Basis) |

### 2.3 Reine Funktionsproben `Betrag()`

| Probe | Ergebnis | Soll |
|---|---|---|
| Weg A: 6 % von 14.760 € | **885,60** | 885,60 — die Kessel-Probe aus Konzept § 4.5 |
| Weg B: 2 % von 205.000 kWh, bewertet 0,246 €/kWh | **1.008,60** | 1.008,60 |
| Satz ohne Menge | **0,00** | 0 („nicht gepflegt") |

---

## 3 Grenzen und Befunde

1. **Heizkessel-Zweig nur strukturgeprüft.** Kein Projekt des Bestands führt im
   jüngsten Lauf Kessel-Modulzeilen mit `Verbrauch > 0` — der Zweig ist codegleich mit
   dem BHKW-Zweig (gemeinsames `Brennstoffsumme`), eine Bestandszahl gibt es dafür
   aber nicht.
2. **Anlagenscharfer Zweig (`ID_Anlage` > 0) strukturell umgesetzt, ohne
   Bestandsprobe** — es existiert noch keine Position mit Anlagenbezug und
   Endenergie-Art. Die erste entsteht mit dem Dialog der Etappe B5; die
   Bezeichner-Zuordnung ist das erprobte Verfahren der `ErdreichAuswertung`.
3. **Basis-Texte des Auflösers sind Herleitungsprosa** (deutsch, unlokalisiert) — sie
   werden erst mit der Herleitungstafel (B6) sichtbar und dort über `MyResource`
   nachgezogen.
4. Der Strompreis „0,35 €/kWh" einiger Projekte ist der gepflegte Arbeitspreis des
   Stromträgers — Projekte mit „—" haben schlicht keinen Stromträgerpreis gepflegt;
   dort bleibt Weg A für die Wärmepumpe ohne Basis (gewollt, keine Fantasiezahl).

## 4 Offen (Fortschreibung der H1-Liste)

| Nr. | Punkt |
|---|---|
| ~~H1-1~~ | ~~Bezugsgrößen-Ermittlung Endenergie~~ — **erledigt mit dieser Etappe** |
| H1-1b | Ermittlung für die übrigen KD1-Bemessungsarten (je kWh therm./el. aus dem Lauf; je kW/kWp/m² aus den Gerätedaten) — eigene Etappe, braucht die Katalog-Leseketten je Gewerk |
| H1-2 | Löschsperre der Pflichtpositionen im Komponenten-Kostendialog |
| H1-3 | Auto-Anlage der Pflichtpositionen nach dem Anlagen-INSERT |
| H1-4 | Anzeigetexte der beiden Bemessungsarten über `MyResource` (de + en) |
| H1-6 | Nachzieh-Migration für Bestandsprojekte (M-3, Entscheidung P4) |
| H2-1 | Mengen-Ausweis: Dialog schreibt beim Speichern den Laufstand nach `Tab_ProjektWerte.Menge` („Stand des Laufs vom …") — gehört zur Dialogetappe |

## 5 Geänderte Dateien

```
WindowsFormsApplication1/Allgemein/Wirtschaftlichkeit/EndenergieAufloeser.cs   NEU
WindowsFormsApplication1/Allgemein/Bericht/KostenEmissionRechner.cs            + ArbeitspreisJeKwh,
                                                                                 StromTraegerId (§ 1.2)
WindowsFormsApplication1/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs  Einbau § 1.3, Helfer
                                                                                 IstEndenergieArt/
                                                                                 EndenergieMenge,
                                                                                 AnlagenSpalteVorhanden
```

Harness `..\dev\h2endenergie\` gehört nicht zum Lieferumfang (gitignored).
