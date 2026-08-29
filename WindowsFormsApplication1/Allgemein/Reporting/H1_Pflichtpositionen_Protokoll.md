# H1 — Pflichtpositionen und Hilfsenergie an der Endenergie (Protokoll)

Etappe H1 des [`Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](../../../Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md)
(§ 4.5, § 5) und des Vorhabens „Pflichtpositionen je Komponente", umgesetzt am **29.08.2026** auf
Branch `Pufferspeicher`. **Nicht committet, nicht gepusht.**

Grundlage sind zwei Festlegungen des Anwenders vom 29.08.2026: Hilfsenergie ist immer Strom und wird
an der **Endenergie der betrachteten Anlage** bemessen; Puffer-, Stromspeicher und Photovoltaik
bekommen sie **nur als Absolutgröße** — Solarthermie ebenso, denn die Sonne kostet nichts.

---

## 1 Umgesetzt

### 1.1 Zwei Bemessungsarten (`Allgemein/DbWerte.cs`)

| Persistenzwert | Weg | Rechnung |
|---|---|---|
| `PROZENT_ENDENERGIEKOSTEN` | A | `Betrag = Endenergiekosten(Anlage) × Satz / 100` |
| `PROZENT_ENDENERGIEBEDARF` | B | `Menge = Endenergiebedarf(Anlage) × Satz / 100`, `Betrag = Menge × Strombezugspreis` |

Beide 24 Zeichen wie `PROZENT_BRENNSTOFFKOSTEN` — die Spaltenbreite `TEXT(30)` reicht weiterhin.
`PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` bleiben als Konstanten bestehen (Altdaten),
kommen in den Seeds aber nicht mehr vor.

Der Kommentarblock hält fest, **dass die Sätze von A und B nicht austauschbar sind** (Faktor rund
3,4, das Preisverhältnis Strom zu Brennstoff) und dass die Oberfläche den Satz beim Umschalten der
Bemessung nicht stillschweigend übernehmen darf.

### 1.2 Pflichtmerkmal (`Allgemein/Update/SchemaKatalog.cs`)

- `SPALTE_KVP_IST_PFLICHT` an `Tab_KostenVorlagePosition`, `SPALTE_PW_IST_PFLICHT` an
  `Tab_ProjektWerte` — beide YESNO. Das Merkmal steht an **beiden** Tabellen, damit die Löschsperre
  ohne Rückgriff auf die Vorlage greift: Eine Projektposition darf ihre Herkunft verlieren
  (`VorlageID` ist nur Anzeige), ihre Pflichteigenschaft nicht.
- `VorlagenPositionSeed` um `IstPflicht` erweitert (optionaler letzter Parameter). **Der Seed-Katalog
  ist damit die eine Wahrheit darüber, welche Position Pflicht ist** — Migrationsschritt 59 überträgt
  das Merkmal, statt eine zweite Liste zu führen.

### 1.3 Seeds auf den Stand der Altdialoge (`SchemaKatalog.Schritt39_Vorlagen`)

Pflicht sind je Komponente Wartung, Instandhaltung der eigenen Komponente und Hilfsenergie
(Entscheidung P1). Dazu die drei Abweichungen aus dem Abgleich gegen die Dialoge der Altanwendung:

| Änderung | vorher | nachher |
|---|---|---|
| Hilfsenergie BHKW / Heizkessel | `PROZENT_BRENNSTOFFKOSTEN` | `PROZENT_ENDENERGIEKOSTEN`, Empfehlung **2–4 %** bzw. **4–8 %** |
| Hilfsenergie Wärmepumpe | `PROZENT_STROMKOSTEN` | `PROZENT_ENDENERGIEKOSTEN` |
| Hilfsenergie Solarthermie | `EUR_PRO_KWH_ELEKTRISCH` | `JAHRESBETRAG` — die Sonne kostet nichts, ein Prozentsatz hätte keine Basis |
| Hilfsenergie Pufferspeicher | `EUR_PRO_KWH_ELEKTRISCH` | `JAHRESBETRAG` |
| Hilfsenergie Photovoltaik | *fehlte* | **ergänzt**, `JAHRESBETRAG`, keine Pflicht |
| Hilfsenergie Stromspeicher | *fehlte* | **ergänzt**, `JAHRESBETRAG`, Pflicht |
| Instandhaltung Heizkessel (BHKW- **und** Kessel-Vorlage) | `JAHRESBETRAG`, ohne Bereich | `PROZENT_INVESTITION`, **1,5–2,5 %** |
| Instandhaltung Wärmezentrale (Kessel-Vorlage) | *fehlte* | **ergänzt**, `PROZENT_INVESTITION`, **1,8–2,2 %** |

Die Speicher bekommen nur `JAHRESBETRAG`, und der Grund steht als Kommentar an der Stelle: Ihre
Umwandlungsverluste stecken bereits im Wirkungsgrad der Speicherrechnung — ein Prozentsatz auf den
Durchsatz zählte sie doppelt —, und ihr Hilfsbedarf (Klimatisierung, Batteriemanagement, Standby)
hängt an der Zeit, nicht am Durchsatz.

### 1.4 Anzeigekatalog und Betragsformel

**`Controller/KostenVorlagenCtrl.cs` (`BemessungKatalog`)** — beide neuen Arten eingetragen
(„% der Endenergiekosten", „% des Endenergiebedarfs", Einheit `%`, nur Betriebsraster). **Ohne
Eintrag hätte `BemessungKatalog.Finde` null geliefert, und der Lesepfad hätte die Bemessung als
`Absolut` behandelt** — der Satz wäre als Eurobetrag interpretiert worden. `% der Brennstoffkosten`
und `% der Stromkosten` stehen jetzt auf `FuerBetrieb = false`: Bestandsdaten werden weiter angezeigt
und gerechnet, zur Neuauswahl stehen sie nicht mehr.

**`Controller/BetriebskostenCtrl.Betrag`** — beide Arten in den Prozentzweig aufgenommen; ohne das
wären sie in den `else`-Zweig gefallen und hätten den eingegebenen Wert zurückgegeben.

> **Weg B braucht keine zweite Formel.** Er liefert eine Strommenge, keine Kosten. Der
> Bezugsgrößen-Auflöser übergibt für `PROZENT_ENDENERGIEBEDARF` deshalb den **bewerteten** Bedarf
> (kWh × Strombezugspreis): `Menge × Satz/100 × Preis` ist dasselbe wie `(Menge × Preis) × Satz/100`.
> Die unbewertete Menge bleibt für die Herleitungszeile erhalten.

### 1.5 Migrationsschritt 59 (`Allgemein/Update/SchemaMigration.cs`)

`ZIEL_VERSION` 58 → **59**. Der Schritt legt beide Spalten an (Muster Schritt 45: `ALTER TABLE` im
try/catch), bringt die **Auslieferungsvorlagen** auf den Seed-Katalog und markiert die vorhandenen
Projektpositionen. Zwei neue Hilfsmittel: `SpalteYesNo` und `VorlagenpositionErgaenzen`.

**Systemgrenzen, bewusst gesetzt:**

- **Nur die Standardvariante** (`Name = 'Standard'`) wird angefasst. Benutzervarianten sind
  Anwenderdaten.
- **Empfehlungsbereiche werden nur nachgetragen**, wo keiner steht (`Empfehlung_von IS NULL`) — ein
  gepflegter Bereich bleibt.
- **An `Tab_ProjektWerte` wird ausschließlich `IstPflicht` gesetzt.** Die Bemessung vorhandener
  Projektzeilen bleibt unangetastet: Eine Zeile mit `PROZENT_BRENNSTOFFKOSTEN` rechnet weiter wie
  bisher. Der neue Weg greift erst, wenn der Anwender die Bemessung selbst umstellt.

---

## 2 Nachweise

**Build:** VS-MSBuild x64, Debug, Ausgabe nach `…\scratchpad\build59\` umgeleitet — **grün**, keine
neuen Warnungen (die fünf gemeldeten sind Bestand: CS0108/CS0109/CS1998 in `WErzeugerModel`,
`KlimaregionStammCtrl`, `StromverbraucherStammCtrl`, `MDIMainForm`).

**Migrationslauf** gegen eine **Kopie** der Produktiv-`Kenndaten.accdb` (151 MB, Stand 29.08.2026
20:04) über den Wegwerf-Harnisch `..\dev\h59\` (gitignored; Settings per Reflection auf die Kopie
umgebogen). Die Produktiv-Datenbank wurde **nicht** angefasst.

| Prüfung | Ergebnis |
|---|---|
| Schemastand | 58 → **59** |
| Spalte `Tab_KostenVorlagePosition.IstPflicht` | vorher nein → nachher **ja** |
| Spalte `Tab_ProjektWerte.IstPflicht` | vorher nein → nachher **ja** |
| Pflichtpositionen gekennzeichnet | **19**, dazu **3 ergänzt** ⇒ 20 Pflichtzeilen in den Vorlagen |
| Bemessungen auf den Seed-Katalog gebracht | **7** |
| Empfehlungsbereiche nachgetragen | **4** |
| Hilfsenergie BHKW | `PROZENT_BRENNSTOFFKOSTEN` → `PROZENT_ENDENERGIEKOSTEN`, 2–4 %, Pflicht |
| Hilfsenergie Heizkessel | `PROZENT_BRENNSTOFFKOSTEN` → `PROZENT_ENDENERGIEKOSTEN`, 4–8 %, Pflicht |
| Hilfsenergie Wärmepumpe | `PROZENT_STROMKOSTEN` → `PROZENT_ENDENERGIEKOSTEN`, Pflicht |
| Hilfsenergie Solarthermie | `EUR_PRO_KWH_ELEKTRISCH` → `JAHRESBETRAG`, Pflicht |
| Hilfsenergie Pufferspeicher | `EUR_PRO_KWH_ELEKTRISCH` → `JAHRESBETRAG`, Pflicht |
| Instandhaltung Heizkessel, beide Vorlagen | `JAHRESBETRAG` → `PROZENT_INVESTITION`, 1,5–2,5 |
| Instandhaltung Wärmezentrale beim Heizkessel | fehlte → **ergänzt**, 1,8–2,2 |
| Namenskollision | „Vollwartung / Wartung BHKW" → „Wartung BHKW": 1 Katalogzeile, 1 Vorlagenposition |
| **Projektpositionen gekennzeichnet** | **3** |
| **Projektzeilen mit `PROZENT_BRENNSTOFFKOSTEN`** | **1, unverändert** ⇒ ergebnisneutral |
| **Zweitlauf** | Schritt 59 „bereits erledigt", Stand bleibt 59 ⇒ **idempotent** |

---

## 3 Befunde

### 3.1 ACE-Falle: `UPDATE … WHERE x IN (SELECT …)` ändert stillschweigend nichts

**Der erste Lauf kennzeichnete null Projektpositionen** — obwohl der Bestand die Zeilen enthält
(Projekt 1018, Komponente BHKW, `StammID` 126, `IstPflicht = False`). Die Ursache ist keine
Datenlücke, sondern das UPDATE selbst:

```sql
UPDATE Tab_ProjektWerte SET IstPflicht = TRUE
WHERE KomponentenID = ? AND KategorieID = ? AND IstPflicht = FALSE
  AND StammID IN (SELECT StammID FROM Tab_Kostenfaktor WHERE Bezeichnung = ?)
```

**Gemessen am 29.08.2026 gegen die Kopie:** Diese Anweisung läuft **ohne Fehler** durch und meldet
**0 geänderte Zeilen**. Dieselbe Anweisung mit direkt eingesetzter `StammID = 126` trifft die Zeile
und meldet **1**. Es gibt keine Ausnahme, keine Warnung — nur einen stillen Nulltreffer.

Behoben, indem die `StammID` **vorher einzeln aufgelöst** wird (`SELECT MAX(StammID) …`) und das
UPDATE ohne Unterabfrage arbeitet. Danach: **3 Projektpositionen gekennzeichnet.**

> **Der Befund gehört über diese Etappe hinaus.** Eine Unterabfrage in einem `UPDATE` ist unter ACE
> kein Stilfehler, sondern ein Wirkungsverlust ohne Fehlermeldung. Wer sie schreibt und die Zeilenzahl
> nicht prüft, hält einen wirkungslosen Migrationsschritt für erfolgreich. **Ohne den Nachweislauf
> wäre dieser Schritt als „OK" durchgegangen.**

### 3.2 Der Bestand nutzt die Vorlagenübernahme kaum

Der Bestand führt genau **drei** Pflichtpositionen in Projekten und **eine** Zeile mit
`PROZENT_BRENNSTOFFKOSTEN`. Bei 32 verschiedenen Betriebskosten-Bezeichnungen im ganzen Bestand
stammen die meisten aus dem Altpfad (freie Positionen wie „Nebenkosten", „Wartung", Komponentennamen
als Positionsnamen).

Das bestätigt den Anlass des Vorhabens: Die Übernahme aus der Vorlage läuft ausschließlich auf
Knopfdruck (`Form_VorlagenUebernahme`) und wird kaum benutzt. Die Nachzieh-Migration für
Bestandsprojekte (M-3, Entscheidung P4) hat deshalb wenig zu tun; sie wird erst wirksam, wenn
Baustein 3 (Auto-Anlage) steht.

### 3.2 Was noch nicht rechnet

Der Schritt legt Struktur an. **Die beiden neuen Bemessungsarten haben noch keine
Bezugsgrößen-Ermittlung** — wie die zehn Bemessungsarten der Etappe KD1 auch. Eine Position mit
`PROZENT_ENDENERGIEKOSTEN` liefert bis dahin `0 €/a`, weil `BetriebskostenCtrl.Betrag` bei fehlender
`Menge` abbricht (`:281`). Das ist der **blockierende Baustein 2** und der nächste Schritt.

Bis dahin gilt: Wer heute eine Vorlage übernimmt, bekommt die richtige Struktur mit den richtigen
Empfehlungsbereichen — aber noch keinen Betrag. Gegenüber dem Zustand vorher ist das keine
Verschlechterung; `PROZENT_BRENNSTOFFKOSTEN` hatte über den Alt-Pfad `BetriebskostenCtrl` eine
Bezugsgröße, dieser Pfad bedient aber ohnehin nur den abgelösten BHKW-Dialog `Form_Betriebskosten`.

### 3.3 Namenskollision aufgelöst (Entscheidung 29.08.2026)

Vorlage „Vollwartung / Wartung BHKW" gegen Altkatalog `DbWerte.VDI_POS_WARTUNG_BHKW` — zwei
`StammID` für dieselbe VDI-Position. **Entschieden ist der Altkatalogname „Wartung BHKW".**

Umgesetzt im Seed und in `WartungBhkwVereinheitlichen` (Schritt 59d). Zwei Fälle:

- **Zielname existiert noch nicht** — der Regelfall, weil der Altkatalogeintrag erst bei Benutzung
  des abgelösten `Form_Betriebskosten` entsteht. Dann wird der vorhandene Eintrag schlicht
  **umbenannt**: Die `StammID` bleibt, alle Verweise bleiben gültig, im Projekt ändert sich nur der
  angezeigte Wortlaut. So lag der Fall in der geprüften Datenbank (StammID 126).
- **Beide existieren** — dann werden Projekt- und Vorlagenzeilen auf die Ziel-`StammID` umgehängt und
  der verwaiste Alteintrag **gemeldet, nicht gelöscht**: Ein Katalogeintrag kann anderswo
  referenziert sein.

Die Umbenennung läuft **vor** der Seed-Schleife. Das ist zwingend: Danach sucht die Schleife nach dem
neuen Namen und würde die Position sonst als fehlend ansehen und ein zweites Mal anlegen.

*Nicht mitentschieden:* Die Kessel-Vorlage führt weiterhin „Vollwartung / Wartung Kessel". Dort gibt
es keine Kollision (der Altkatalog kennt die Position nicht), die Bezeichnungen sind seither aber
asymmetrisch.

---

## 4 Offen aus dieser Etappe

| Nr. | Punkt |
|---|---|
| H1-1 | **Bezugsgrößen-Ermittlung** für `PROZENT_ENDENERGIEKOSTEN`/`_BEDARF` und die zehn KD1-Bemessungsarten — anlagenscharf. **Blockierend für alles Weitere.** |
| H1-2 | **Löschsperre** in `Form_KostenKomponente.Zeile_LoeschenAngefordert` (`:499-509`) samt Ausweg „Satz auf 0 setzen" |
| H1-3 | **Auto-Anlage** der Pflichtpositionen nach dem Anlagen-INSERT (Muster `Nebenmodus.NurAnlegen`) |
| H1-4 | **Anzeigetexte** der beiden neuen Bemessungsarten über `MyResource` (de + en) — `KostenVorlagenCtrl` führt die Bemessungsnamen (`:490-506`) |
| ~~H1-5~~ | ~~Namenskollision~~ — **erledigt mit Schritt 59d** (Entscheidung: „Wartung BHKW") |
| H1-6 | **Nachzieh-Migration** für Bestandsprojekte (M-3, Entscheidung P4) |

---

## 5 Geänderte Dateien

```
WindowsFormsApplication1/Allgemein/DbWerte.cs                  + 2 Konstanten, Kommentarblock H1
WindowsFormsApplication1/Allgemein/Update/SchemaKatalog.cs     + 2 Spaltenkonstanten, Seed-Feld
                                                                 IstPflicht, 8 Seed-Korrekturen
WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs   + Schritt 59, 2 Hilfsmittel,
                                                                 ZIEL_VERSION 58 → 59
WindowsFormsApplication1/Controller/KostenVorlagenCtrl.cs      BemessungKatalog: 2 neue Arten,
                                                                 Altarten FuerBetrieb = false (§ 1.4)
WindowsFormsApplication1/Controller/BetriebskostenCtrl.cs      Betrag: beide Arten im
                                                                 Prozentzweig (§ 1.4)
```
*(Die beiden Controller fehlten in der ersten Fassung dieser Liste — nachgetragen beim
Commit 29.08.2026.)*

Wegwerf-Harnisch `..\dev\h59\` (gitignored) und die DB-Kopie im Scratchpad gehören **nicht** zum
Lieferumfang.
