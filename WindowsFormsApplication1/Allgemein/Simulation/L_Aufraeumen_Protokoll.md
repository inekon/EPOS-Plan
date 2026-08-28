# Paket L — Aufräumen, Lokalisierung, Dokumentation: Umsetzungsprotokoll

Stand: 28.08.2026 · Branch `Pufferspeicher` (HEAD `fab1440`, Paket P2) · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Paketzeile **L** („Lokalisierung + Dokumentation"), Kapitel 10 und **15** (Stillgelegt-Liste).
Build `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug: **0 Fehler** (5 Bestandswarnungen).
**Kein Schema-Schritt** — L fasst weder Tabelle noch Spalte an (`ZIEL_VERSION` bleibt 54).

> **Was dieses Paket ist.** Der Abschluss der Reihe K1…P2: Es schneidet die Bausteine
> heraus, die die vorangegangenen Pakete aufruferfrei zurückgelassen haben, holt die seit K2
> inline deutsch geschriebenen Engine-Meldungen in den Ressourcenkatalog, schließt drei
> Einzeltickets und stellt die Grabstein-Kommentare richtig, die inzwischen von der
> Wirklichkeit überholt sind.
>
> **Was dieses Paket ausdrücklich NICHT ist:** ein Schema-Aufräumen. Konzept Kapitel 15 führt
> `Z_ProjektPufferSp`, `Tab_Einstellungen.Kaskade_Zweikanalig`,
> `Tab_Pufferspeicher.Verwendung`, die zehn `WS_*`-Spalten, `WQ_CSV` und `WQ_Puffer` als
> **stillgelegt — Lese-Altlast nach Migration**. Sie bleiben. Zwei Kommentare im
> Migrationskatalog stellten ein Löschen für „das Aufräumpaket" in Aussicht; sie sind
> berichtigt (Abschnitt 3.4).

## 1. Umfang

| Teil | Inhalt |
|---|---|
| **A — Tote Altlast-APIs** | Die A1-O3-Liste, geschnitten mit Aufrufer-Beweis; dazu K2-O9 (toter Doku-Verweis) und P2-O1 (Serienschlüssel als Konstanten) |
| **B — Ressourcen-Nachzug** | 13 Aufrufstellen / **15 neue Schlüssel** für die seit K2 inline deutschen Engine-Protokolltexte (K2-O4, S1-O8, S2-O9, P1-O7, B1-O9, Q1-O9); FR-3 (Bauart-Änderungsweg); S2-O8 (Bauform als Anzeige statt Rohwert); zwei Mojibake-Reparaturen aus Q1 |
| **C — Kleinteile** | A1-O2 (Senkenliste bei der Komponentenübernahme), P2-O6 (Einheitentext-Überhang), Sichtung und Richtigstellung der Grabstein-Kommentare aus K1…P2 |
| **D — Dokumentation** | Katalog-Nachtrag Paket L **und der fehlende Nachtrag Paket P1** (B1-O7); dieses Protokoll mit der Abschlusstabelle aller O-Tickets |

## 2. Teil A — was entfernt wurde, und womit es belegt ist

Jede Löschung mit repo-weitem Grep über `Allgemein`, `Controller`, `Model`, `Views`
(ohne `*.Designer.cs`, ohne die vom Build ausgeschlossenen Dateien, ohne die Altkopie-Ordner
und ohne `.claude\worktrees`).

| Entfernt | Umfang | Aufrufer-Beweis |
|---|---|---|
| `KonfigurationCtrl.KaskadeZweikanaligLesen` / `-Schreiben` / `KaskadeNotwendig(int)` / `KaskadeNotwendig(int,int,SenkeDaten)` + die privaten Helfer `KaskadeErzeuger`, `ErzeugerZuTyp` | −257 Zeilen, ersetzt durch einen 22-zeiligen Grabstein | Vor dem Schnitt **nur Definitionen und Doku-Verweise**, keine Aufrufstelle. Die Helfer hatten `KaskadeNotwendig` als einzigen Aufrufer. |
| `AnlagePufferVerbundCtrl.ProjektHatVerbund` | −12 Zeilen | Einzige Fundstellen: die Definition und ein `<c>`-Verweis im eigenen Doc-Kommentar. Ihre zwei Aufrufer sind mit A1 gefallen (`SimulationControl._verbundErzwingtSpeicherstufe`, `KonfigurationCtrl.KaskadeNotwendig`). |
| `Controller/Z_ProjektPufferSpCtrl.cs` (ganze Klasse) und `Model/Z_ProjektPufferSpModel.cs` | **2 Dateien gelöscht** | Beide Typen wurden ausschließlich voneinander verwendet; alle übrigen Nennungen im Repo stehen in Kommentaren. Das `.csproj` globt (SDK-Stil) — keine Projektdatei anzufassen. |
| `SimulationControl.KaskadeZweikanalig` (Feld, konstant `true` seit A1) | −17 Zeilen | Fünf Leser, alle in `Form_Simulation_Detail` (siehe nächste Zeile). |
| **Die fünf Leser in `Form_Simulation_Detail`** | Vier Verzweigungen auf den EINEN Rechenweg vereinfacht: WP-Restwärme und WP-Deckungsgrad, BHKW-Stufeneingang, BHKW-Restwärme, BHKW-Deckungsgrad | Der jeweils entfallene Zweig war der Altpfad-Zweig; er ist seit A1 unerreichbar. Die verbleibende Rechnung ist zeichengleich mit dem bisherigen `true`-Zweig. |
| `KonfigurationModel.Kaskade_Zweikanalig` + die Lesung in `KonfigurationCtrl.ReadSingle` + die Vorbelegung im Konstruktor | −3 Codestellen | **Ordinalkette geprüft:** Die Lesung war ausdrücklich NAMENSbasiert (`dt.Columns.Contains(SPALTE_KASKADE_ZWEIKANALIG)`), die Ordinalkette endet unverändert bei `row[22]`. Der Wegfall verschiebt keine Position und berührt weder die `Insert`- noch die `Update`-Spaltenliste. |
| `ErzeugerKatalog.ZUORDENBAR` | −8 Zeilen | Repo-weit ohne Fundstelle außer der eigenen Deklaration. Es speiste die Erzeugerspalte des mit A1 gelöschten Dialogs `Form_KonfigPufferspeicher`. |

**Summe Teil A: 2 Dateien gelöscht, 7 Bausteine geschnitten, keine Verhaltensänderung.**

### 2.1 Was bewusst STEHEN BLEIBT — und warum

| Baustein | Ticket | Entscheidung |
|---|---|---|
| `DbWerte.ERZEUGER_GESAMTSYSTEM` | A1-O3 | **Bleibt.** Nicht fundstellenlos: `ErzeugerKatalog.Anzeige`/`DbWert` übersetzen ihn, `Form_Simulation_Config` führt ihn in `listErzeuger` mit. Er ist der **Persistenzwert** von `Z_ProjektPufferSp.Erzeuger` — einer Spalte, die nach Konzept 15 stehen bleibt. Ohne die beiden Übersetzungszweige liefe ein solcher Altwert unübersetzt durch die Oberfläche: eine Verhaltensänderung ohne Gewinn. |
| `simulation_wp.Pufferspeicher = puffer_wp` | **E1-O4** | **Bleibt — die Zeile ist NICHT wirkungslos.** Beleg: `ErdreichAuswertung.AusLauf` liest `sim.simulation_wp.Pufferspeicher != null` und gibt daraus das Kennzeichen „die WP lädt einen Senkenspeicher" an `Auswerten` weiter (Paket E1 hat sie genau dorthin umgestellt). Ein Schnitt hätte die Erdreich-Kennwerte jedes Laufs mit Senkenspeicher verändert. Das Ticket ist damit **beantwortet, nicht erledigt**: Der Alias fällt erst, wenn die Erdreich-Auswertung ihre Frage anders stellt. |
| `SenkenPufferDerAnlagen` liest zusätzlich `WS_ID_Puffer/2` | **A1-O4** | **Bleibt** (Vorgabe des Pakets). Der Duldungs-Kommentar an der Stelle ist geschärft: Er nennt jetzt die drei Gründe einzeln — die Spalten bleiben (Konzept 15), der Wegfall wäre ergebnisändernd (ein nur dort verzeichneter Puffer verschwände aus Registry und `Tab_ErgebnisPufferspeicher`), und auf migriertem Bestand fügt die Schleife nichts hinzu. |
| Das Referenzlauf-Werkzeug | E1-O3 / P1-O1 / B1-O3 / Q1-O2 | **Unangetastet** (Vorgabe des Pakets). Es ist das Messinstrument; geänderte Dateinamen machten jeden Vergleich mit älteren Ständen unmöglich. |
| `WaermesenkeClass.SenkeDaten.Kopie()` | neu, siehe 7 | **Bleibt.** Durch den Schnitt von `KaskadeNotwendig` ohne Aufrufer geworden. Eine Zeile, korrekt, Teil der `SenkeDaten`-Schnittstelle — der nächste Dialog mit einer „was gälte, wenn ich jetzt speichere"-Prüfung müsste sie sonst neu schreiben. Im Code als solche vermerkt. |

### 2.2 K2-O9 — der tote Doku-Verweis

`SimulationWaermepumpe.cs` verwies mit `<see cref="Berechnung_Zweikanalig"/>` auf eine Methode,
die das WP-Modul **nie hatte** (die drei anderen Erzeuger führen sie als Vektorstufe). Der
Verweis zeigt jetzt auf `Zweikanalig_Start` und die Stundenkette der `Kaskadenschleife`; die
Fehlleitung ist an Ort und Stelle vermerkt. Die zweite von K2-O9 genannte Fundstelle ist ein
gewöhnlicher Kommentar mit einer historisch **richtigen** Aussage („bis Paket 4 waren das
lokale Variablen von `Berechnung_Zweikanalig`") und bleibt.

### 2.3 P2-O1 — Serienschlüssel und Legendentexte des `ZeitreihenExtraktor`

Der Extraktor baute die Temperaturschlüssel als Zeichenketten (`"_TOBEN"`, `"_TUNTEN"`,
`"QUELLTEMP_"`) und die Legendentexte inline deutsch (`" T oben [°C]"`). Er bindet jetzt an
`ZeitreihenSatz.SUFFIX_T_OBEN`/`_T_UNTEN`/`QUELLTEMP_PRAEFIX` und an
`MyResource.Resource.SIM_REIHE_T_OBEN`/`_T_UNTEN`.

**Reine Bindung, keine Schlüsseländerung:** Die Konstanten tragen exakt dieselben Werte, der
deutsche Ressourcentext ist zeichengleich mit dem bisherigen Literal (`T oben [°C]`).

## 3. Teil C und die Kommentar-Richtigstellungen

### 3.1 A1-O2 — die Senkenliste kommt bei der Komponentenübernahme mit

`KomponentenUebernahmeCtrl` tauscht den Komponentenbestand eines Gewerks: Es löscht die
Anlagenzeilen des Ziels (die Löschweitergabe von `FK_AnlageSenke_Anlage` nimmt ihre Senken
mit) und legt sie aus der Quelle neu an. Die **Senkenliste** hängt an der Anlage, nicht am
Gerät — sie kam bisher nicht mit, und jede übernommene Komponente startete mit der
Rang-1-Vorbelegung `Heizkreis/Beides`.

| Baustein | Umsetzung |
|---|---|
| Lesen | `quellSenken` je Quell-Anlage **vor** der Transaktion (`Z_AnlageSenkeCtrl.LesenJeAnlage`) |
| Schreiben | `SenkenNachziehen` **nach dem Commit** — dieselbe Stelle wie `VariantenNachziehen`, aus demselben Grund: Die Anlagen-ID ist ein AutoWert, und `SchreibenJeAnlage` führt seine eigene Transaktion |
| Anlagen-Abbildung | **positionell**: Schritt 4 löscht ALLE Anlagenzeilen der Gewerktypen im Ziel, Schritt 7 legt genau so viele in Quellreihenfolge neu an; aufsteigend nach ID sortiert stehen sie Zeile für Zeile wie die Quelle. Stimmt die Zahl wider Erwarten nicht, fällt die Zuordnung auf `AnlageFinden` (Typ, Bezeichner) zurück — dieselbe Auflösung wie bei den Speichervarianten, mit deren dokumentierter Grenze (S1-O1) |
| Puffer-Verweise | über **dieselbe** Abbildung wie `PufferverweiseUmschreiben`; was sich nicht abbilden lässt, wird GELEERT und mitgezählt (`BK_KOMP_HINW_PUFFERVERWEIS`). Das ZIEL der Zeile bleibt unangetastet — genau wie bei den `WS_`-Spalten; die Engine normalisiert ein Puffer-Ziel ohne Puffer beim Lesen und meldet es |
| Ohne Tabelle | `Z_AnlageSenkeCtrl.SpalteVorhanden()` fängt Lesen und Schreiben still ab (Datenbank vor Schritt 50) |

**Wirkprobe** (Wegwerf-Kopie außerhalb des Repos, Schemastand 54; Harness über die
öffentlichen Methoden, DB-Pfad per Reflection und hart gegen ProgramData/`Referenzlaeufe`
geprüft). Quelle **1023** (zwei Wärmepumpen 11203/11204, präparierte Senkenkette mit drei
Rängen und eigenen Ladeparametern), Ziel **1024**:

| Runde | Konstellation | Ergebnis |
|---|---|---|
| **A** | Zielpuffer 1054164 auf den Bezeichner des Quellpuffers 1018023 gesetzt | Ziel vorher: **1** Zeile (`Heizkreis`, die Vorbelegung). Nachher: **3** Zeilen je Anlage, Ränge 1–3, `Ladeprio 3`, `Ladegrenze 55`, `Anschlusshoehe 0,4` übernommen. `ID_Puffer` der Ränge 1 und 3 = **1054164**, also der Speicher des ZIELPROJEKTS — nie eine Quell-ID. Rang 2 zeigte in der Quelle auf einen Puffer ohne Namensgleichen im Ziel und ist **auf 0 geleert**, gemeldet mit „2 Verweis(e) … bleiben leer" |
| **B** | Zielpuffer auf `ZZ-unauffindbar` umbenannt | Kette weiterhin **3** Zeilen je Anlage, **alle** `ID_Puffer` auf 0 geleert und gemeldet. Kein einziger Quell-Fremdschlüssel ist ins Zielprojekt gewandert |

Die produktive Datenbank wurde dabei nicht angefasst; die Wegwerf-Kopien sind nach der Probe
gelöscht.

### 3.2 P2-O6 — der Einheitentext der Ladeobergrenze

`SIM_LBL_LADEGRENZE_EINHEIT` steht in `Form_Waermesenke` bei x = 262 in einer 596 px breiten
Gruppe; verfügbar sind damit 318 px. Gemessen bei 96 dpi: **DE 310 px** (passt),
**EN 356 px** (ragt 38 px über den Gruppenrand und wird am Rahmen beschnitten).

Behoben mit `MaximumSize = new Size(_gbLaden.Width - 262 - 16, 0)` bei unverändertem
`AutoSize = true`: Der Text **bricht um** und wächst nach unten, statt abgeschnitten zu werden
(die nächste Zeile beginnt erst bei y = 98, zwei Zeilen enden bei y ≈ 92). Der deutsche Text
bleibt einzeilig und steht Pixel für Pixel wie bisher — **auf deutscher Oberfläche ist die
Änderung unsichtbar.** Der Einheitentext der Einspeisehöhe darunter liegt mit 259 px (DE) /
219 px (EN) innerhalb der Grenze und ist unverändert.

### 3.3 Grabsteine und TODOs aus K1…P2 — richtiggestellt (nur Kommentare)

| Stelle | war | ist |
|---|---|---|
| `SchemaKatalog.SPALTE_SENKE_ANSCHLUSSHOEHE`, `Z_AnlageSenkeModel.Anschlusshoehe` | „VORGRIFF auf P1 … gelesen wird sie erst mit dem Schichtmodell" | Vorgriff **eingelöst**: P1 liest, P2 pflegt im Senkendialog |
| `SchemaKatalog.SPALTE_PUFFER_T_OBEN_MITTEL`, `ErgebnisPufferspeicherModel.T_oben_Mittel/_Min`, `ErgebnisCtrl` | „P1-VORGRIFF … bis zum Schichtmodell immer NULL" | **Seit P1 gefüllt**; NULL bleibt die ehrliche Antwort dort, wo es keine Speichertemperatur gibt (Quellspeicher) |
| `SimulationSPK` (2 Stellen) | „auch der Speicher hat keine Lade-/Entladeleistung (vorgemerkter Parameter)" / „die Entnahmefähigkeit ist der vorgemerkte Parameter … (heute unbegrenzt)" | Seit P1 **real** (`Ladeleistung_Max`/`Entladeleistung_Max`, 0 = unbegrenzt); die fehlende Grenze des ÜBERTRAGERS ist der verbliebene Restpunkt |
| `Warnkriterien.QUELLE_NICHT_KONFIGURIERT` | „die echte Quellkopplung kommt mit Paket B1" | B1 ist geliefert; das Kriterium **bleibt** und meldet die fehlende QUELLE, nicht die fehlende Kopplung |
| `WaermesenkeClass` (WS_-Schreibweg) | „… und fallen mit Paket L" | Die Spalten **bleiben** (Konzept 15); Verweis auf die Begründung an der Mitlesestelle |
| `PufferSpCtrl` (4 Stellen), `DataRepository` | Präsens-Verweise auf `Z_ProjektPufferSpCtrl.Insert` | auf „bis Paket L, mit dem Aufräumschnitt entfallen" umgestellt; der Duplikat-Puffer-Grund einer noch bestehenden `UPDATE Z_ProjektPufferSp`-Zeile ist neu begründet (Datenwiderspruch statt Duplikatgefahr) |
| `PufferSpCtrl.KlassenSetSchreiben` | Muster-Verweis auf `KonfigurationCtrl.KaskadeZweikanaligSchreiben` | auf `ExtrapolationErlaubtSchreiben` umgehängt (sonst toter `<c>`-Verweis) |

### 3.4 Zwei Migrationskommentare, die ein Löschen in Aussicht stellten

`SchemaKatalog.Z_PROJEKTPUFFERSP` und `SchemaMigration.SCHRITT_51_ALTPFAD_STILLLEGUNG` sagten:
„Das Entfernen der Tabelle … bleibt dem Aufräumpaket vorbehalten." **Das Aufräumpaket ist
dieses hier, und es entfernt nichts.** Beide Stellen halten jetzt die Entscheidung fest:
geschnitten wurden die aufruferfreien ZUGRIFFSWEGE, Tabelle und Spalte bleiben als
Lese-Altlast nach Konzept 15 stehen.

## 4. Teil B — der Ressourcen-Nachzug

**15 neue Schlüssel an 13 Aufrufstellen**, dazu drei Anzeigenamen der Speicherbauform
(S2-O8) — Bestand danach **2618 `data`-Knoten** je `.resx`, DE/EN deckungsgleich, **2618
Designer-Eigenschaften**. Einzelnachweis mit Fundstelle je Schlüssel im
[`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md), Abschnitt „Nachtrag Paket L".

| Schlüssel | Aufrufstelle | aus Paket |
|---|---|---|
| `SIMENG_SCHICHT_INVARIANTE` | `SimulationControl.Do_Simulation_Intern` | P1 |
| `SIMENG_SENKE_OHNE_LADEAUFTRAG_RANG` + `…_RANG1` + `…_NACHRANG` | `SimulationControl.SenkeAufHeizkreisZurueck` | S1 |
| `SIMENG_PENDELSPEICHER_ENTLADEORDNUNG` | `SimulationControl.EntladeordnungEinsortieren` | K2 |
| `SIMENG_ENTLADEORDNUNG_NACHTRAG` | `SimulationControl.EntladeordnungAufbauen` | K2 |
| `SIMENG_KLASSENSET_ROLLE` | `SimulationControl.KlassenSetUebernehmen` | K2 |
| `SIMENG_VERBUND_SCHICHTUNG` | `SimulationControl.VerbundAufaddieren` | P1 |
| `SIMENG_TNUTZ_UEBER_VORLAUF` | `SimulationControl.SchichtparameterUebernehmen` | P1 |
| `SIMENG_BOOSTER_KOPPLUNG` | `SimulationControl.BoosterKopplungVorbereiten` | B1 |
| `SIMENG_KESSEL_BOOSTER_KOPPLUNG` | `SimulationControl.KesselQuellbezugSetzen` | B1 |
| `SIMENG_KNAPPHEIT_UNGUELTIG` | `SimulationKanaele.KnappheitsReihenfolge` | K2 |
| `SIMENG_QUELLPROFIL_UNLESBAR` | `WaermequelleClass.Quelltemperatur` | Q1 |
| `SIMENG_SENKENZEILE_OHNE_PUFFER` | `WaermesenkeClass.AusZuordnungstabelle` | A1 |
| `SIMENG_SENKENLISTE_LEER` | dieselbe Methode | A1 |
| `PSP_SPEICHERTYP_ANZEIGE_SOLAR/_PUFFER/_KOMBI` | `Warnkriterien.BauformAnzeige` | S2-O8 |

**Prefix-Regel beibehalten:** `SIMENG_*` ist im Bestand der Präfix der Engine-Protokolltexte
(61 Schlüssel), `SIMWARN_*` gehört dem Warnkriterienkatalog W1–W6 (14 Schlüssel). Die
Engine-Warnungen dieses Pakets tragen deshalb `SIMENG_`, nicht `SIMWARN_`.

### 4.1 FR-3 — der Bauart-Änderungsweg

`SIMQ_MSG_LUFT_WASSER` (DE + EN) ist um den Weg erweitert, den die Fehlerrunde vom 27.08.
als Anwenderlösung festgehalten hat (Befund G: „Booster-WP: Quelle Pufferspeicher nicht
wählbar"). **Hier ist die Textänderung gewollt** — es ist ein Dialogtext, kein Laufprotokoll.

### 4.2 S2-O8 — die Bauform als Anzeige

Der W2-Text nannte die Bauform als **rohen Persistenzwert**. Sie geht jetzt durch
`Warnkriterien.BauformAnzeige` — dieselbe Regel wie beim Klassen-Set eine Zeile weiter.
**Unbekannte Werte laufen roh durch**, und das ist hier Pflicht: Befund L0-1 hat in Beständen
englische Anzeigetexte („Buffer storage") und in einem Fall den Altdatenrest `blabla` in die
Spalte geschrieben. Ein solcher Wert soll sichtbar bleiben, nicht auf eine der drei bekannten
Bauformen geraten werden. Die Auswahlliste des Speicherdialogs bleibt unangetastet
(Projektregel: Designer und Formular-Ressourcen nicht von Hand pflegen).

### 4.3 Zwei Mojibake-Werte aus Paket Q1

`SIMQ_PUFFER_MSG_ANSCHLUSSHOEHE` und `SIMQ_QUELLPROFIL_MSG_WERTE_FEHLEN` trugen doppelt
UTF-8-kodierte Umlaute (`EntnahmehÃ¶he`, `heiÃŸt`, `FÃ¼r`) und zeigten sie so im Dialog.
Behoben ohne Änderung am Wortlaut; beide sind reine MessageBox-Texte, kein Steuerwert hängt
daran. Prüfung **P5** der [Prüfrezeptur](Lokalisierung_Pruefung.md) meldet den Katalog danach
wieder sauber.

## 5. Verifikation

### 5.1 Referenzlauf — die zugesagte Messlatte

Dreizehn Projekte (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039, 1040, 1041,
1042) gegen die Basis **`2026-08-28_P1`**, gerechnet auf dem Endstand des Pakets:

```
Projekt_1007 … Projekt_1042 : 13 × PASS   (3 532 029 Werte innerhalb der Toleranz)
MD5-Vergleich                : 329 von 329 CSV byte-gleich, 0 abweichend,
                               0 fehlend, 0 zusätzlich
pruefen                      : GESAMT plausibel (keine NaN/Inf)
Laufprotokoll                : 60 Meldungszeilen / 45 verschiedene vorher,
                               60 / 45 nachher — Mengenvergleich CASE-SENSITIV leer
```

Der Lauf wurde **zweimal** gefahren: einmal nach den Code- und Ressourcenänderungen, einmal
nach den reinen Kommentar-Richtigstellungen. Beide Male dasselbe Ergebnis; damit ist der Lauf
zugleich sein eigener Selbstvergleich.

Datenquelle: produktive `Kenndaten.accdb` (Zeitstempel **27.08.2026 23:34:25**, Größe
**151 949 312 Bytes**, **nur gelesen** — beides vor dem ersten und nach dem letzten Schritt
identisch, keine `Kenndaten.laccdb`, kein Access- oder Anwendungsprozess); Arbeitskopie
migriert auf Schemastand **54**. Beide Probeordner sind nach dem Vergleich gelöscht.
**Die Basis `2026-08-28_P1` bleibt unverändert gültig.**

### 5.2 Warum der Referenzlauf die Textgleichheit NICHT allein belegt

**Keine der 15 umgezogenen Meldungen feuert in der Referenzmenge** (mechanisch geprüft: für
jede Vorlage das längste platzhalterfreie Bruchstück gegen das Laufprotokoll — 0 Treffer). Das
zeichengleiche Protokoll beweist deshalb, dass der Umzug **keine neue oder veränderte Meldung
eingeschleppt** hat, nicht aber die Gleichheit der umgezogenen Texte selbst. Dafür stehen die
drei folgenden Nachweise.

### 5.3 Textgleichheit — drei unabhängige Nachweise

**(1) Mechanischer Abgleich Vorlage ↔ Quelltext VOR dem Umbau.** Ein Skript zerlegt den
C#-Verkettungsausdruck jeder Aufrufstelle zeichenweise (Stringliterale mit
Escape-Auflösung, Kommentare übersprungen) und baut daraus die Vorlage: Literale verkettet,
jede nicht-literale Lücke als `{n}`. Ergebnis gegen den `.resx`-Wert, **case- und
zeichensensitiv**:

```
12 von 15 Vorlagen sofort ZEICHENGLEICH
 3 nach Zusammenziehen der ToString-Formatzeichenketten ("0.#", "0.######"),
   die der Zerleger als eigene Literale sieht — danach ebenfalls zeichengleich
```

Die Argumentreihenfolge wurde aus demselben Zerleger gegen den **HEAD-Stand** der Dateien
gelistet und mit den neuen `string.Format`-Argumentlisten abgeglichen.

**(2) Laufzeitprobe an den KOMPILIERTEN Ressourcen.** `System.Resources.ResourceReader` über
`obj\x64\Debug\net8.0-windows\WindowsFormsApplication1.MyResource.Resource[.en-US].resources`:
**2618 Schlüssel je Kultur, 0 Abweichungen** gegen die Sollwerte — der Weg `.resx` → `.resources`
verändert kein Zeichen.

**(3) Provozierte Meldungen auf einer Wegwerf-Kopie.** Sechs der 15 Meldungen wurden durch
gezielte Datenänderungen zum Feuern gebracht und im Laufprotokoll gelesen:

| Schlüssel | Provokation | Protokollzeile (gekürzt) |
|---|---|---|
| `SIMENG_KNAPPHEIT_UNGUELTIG` | `Kanal_Knappheitsreihenfolge = 'unsinn;quatsch'` (1023) | *Knappheitsreihenfolge: Die Projekteinstellung „unsinn;quatsch" ist unbrauchbar … Vorbelegung Brauchwasser -> Prozesswaerme -> Heizung.* |
| `SIMENG_TNUTZ_UEBER_VORLAUF` | Puffer 1018023 auf N = 5, `T_Nutz_BW = 95` (VL_eff 65) | *Schichtmodell: Am Puffer 1018023 (Vitocell 140-E 600 Ltr) liegt die Mindest-Nutztemperatur Brauchwasser mit 95 °C ÜBER … Gerechnet wird mit 65 °C …* |
| `SIMENG_KLASSENSET_ROLLE` | Klassen-Set {H,B} gegen Alt-Verwendung `Heizung` | *Speicher 1018023 (…): Das Klassen-Set Heizung + Brauchwasser passt nicht zur Alt-Verwendung „Heizung". … lautet „Kombi".* |
| `SIMENG_ENTLADEORDNUNG_NACHTRAG` | Folge desselben Set-Wechsels | *Speicher 1018023 (…) steht nicht in der Entladereihenfolge des Kanals Brauchwasser - er wird ans Ende gestellt.* |
| `SIMENG_SENKENLISTE_LEER` | `DELETE FROM Z_AnlageSenke WHERE ID_Anlage = 11204` | *Wärmesenke: Für die Anlage 11204 steht in Z_AnlageSenke keine einzige Zeile. Der Lauf rechnet die Vorbelegung Heizkreis/Beides.* |
| `SIMENG_VERBUND_SCHICHTUNG` | Leitspeicher 1054187 (Projekt 1040) auf 4 Schichten | *Parallelverbund: Der Leitspeicher 1054187 (Puffer 3000Ltr) ist mit 4 Schichten gepflegt … Gerechnet wird ungeschichtet (1 Schicht) …* |

Alle sechs lesen sich Zeichen für Zeichen wie vor dem Umzug. Die Wegwerf-Kopie ist nach der
Probe gelöscht.

### 5.4 Was vom Rückstand bleibt

Erhebung über alle Protokollkanal-Aufrufe in `Allgemein/Simulation/`
(`Protokoll.Warnung|Hinweis|Fehlermeldung`, `…Einmal`, `SimulationProtokoll.Aktuell.*`), je
Fundstelle mit `git blame` datiert:

| | Zahl |
|---|---|
| Aufrufstellen gesamt (ohne Definitionen und Kommentare) | 92 |
| davon bereits über `MyResource.Resource.*` | 34 |
| davon INLINE deutsch (Ausgangslage) | **58** |
| davon **seit Paket K2 entstanden** → mit L umgezogen | **13** |
| davon **Bestand vor K2** → bleibt | **45** |

**Gegenprobe nach dem Umzug** (mechanisch, Acht-Zeilen-Fenster hinter jeder Aufrufzeile):
92 Aufrufstellen, **39 mit `MyResource.Resource.` im Fenster** (vorher 34 — genau die 5
Dateien, in denen die 13 umgezogenen Stellen liegen), 53 ohne. Von diesen 53 reichen **5**
einen Text aus fremder Quelle durch (`kontext.Hinweise`, zwei `Fehlertext` aus `SIMENG_*`,
`Warnbefund.Text` aus `SIMWARN_*`, `ctrl.LetzterHinweis`) und **3** in `ProfilBedarf` eine
Variable, die weiter oben aus einer `SIMENG_*`-Ressource gefüllt wird. **53 − 8 = 45** — die
Zahl stimmt mit der Handinventur überein.

Die 45 verbliebenen sind ein eigener Schnitt: Sie stammen aus den Paketen 8/9 und älter, sind
keinem offenen Ticket zugeordnet, und ihr Umzug risikierte den Protokollvergleich über eine
mehr als dreimal so große Textmenge. **Neuer Ticket-Vorschlag: L-R1** (Abschnitt 7).

### 5.5 Build

`WP-Plan.sln` und `Referenzlauf.csproj`, Debug × x64: **0 Fehler**; die fünf verbliebenen
Warnungen (CS0108/CS0109/CS1998) sind Bestand und liegen außerhalb dieses Pakets.

**Katalog-Gleichstand (Prüfung P4):** `Resource.resx` 2618 · `Resource.en-US.resx` 2618 ·
`Resource.Designer.cs` 2618 — drei Mengen deckungsgleich, 0 Abweichungen, **keine
CS0102-Dublette**. (Der Generator von Visual Studio hat nach dem `.resx`-Schnitt selbst
angezogen; die Hand-Einfügung entfiel dadurch — geprüft und die generierte Fassung behalten,
wie es die VS-Falle verlangt.)

**Kodierung (Prüfung P5):** alle 25 geänderten `.cs`/`.resx` gültiges UTF-8; die drei Dateien
ohne BOM (`ZeitreihenExtraktor.cs`, `Warnkriterien.cs`, `Z_AnlageSenkeModel.cs`) waren auch in
`HEAD` ohne BOM — **kein BOM gesetzt, keines entfernt**. Kein Mojibake, kein `U+FFFD`.

## 6. Abschlusstabelle — alle O-Tickets der Pakete K2 … P2

Legende: **✔ L** = mit diesem Paket erledigt · **✔ früher** = vor L erledigt ·
**○** = bewusst offen, mit Ziel.

| # | Punkt (Kurzfassung) | Stand |
|---|---|---|
| **K2-O1** | `Kanalabzug` gehört nach `Kaskadenschleife` | **✔ früher** (S1) |
| **K2-O2** | `SimulationBHKW` liest `IstBrauchwasserkanal` statt `BedientKanal` | **✔ früher** (S1) |
| **K2-O3** | `SimulationWaermebedarf.Kanaele()` ohne Aufrufer | **✔ früher** (S1) |
| **K2-O4** | Engine-Protokolltexte des K2-Umbaus inline deutsch | **✔ L** — 4 Texte umgezogen (`SIMENG_PENDELSPEICHER_ENTLADEORDNUNG`, `…_ENTLADEORDNUNG_NACHTRAG`, `…_KLASSENSET_ROLLE`, `…_KNAPPHEIT_UNGUELTIG`) |
| **K2-O5** | `PSP_FEHLER_VERWENDUNG_PFLICHT` verwaist | **✔ früher** (S2) |
| **K2-O6** | `EntladeleistungMax`-Zwei-Pass je Stunde budgetieren | **✔ früher** (P1) |
| **K2-O7** | Registry-Block 1 leitet das Set aus der Alt-`Verwendung` ab | **✔ früher** (A1) |
| **K2-O8** | Set-Wechsel-Rückfrage im Pufferdialog | **✔ früher** (S2) |
| **K2-O9** | toter `<see cref="Berechnung_Zweikanalig"/>` in der WP | **✔ L** (2.2) |
| **S1-O1** | Senkenrettung erkennt Anlagen über (Typ, Bezeichner) — Umbenennen verliert Senken | ○ **dokumentiert, bleibt** — dieselbe Grenze trifft jetzt auch den Rückfallweg von `SenkenNachziehen` (3.1) |
| **S1-O2** | `Ladeprio_PV` nur an Rang 1 migriert | ○ dokumentiert, bleibt |
| **S1-O3** | `Anschlusshoehe` angelegt, nicht gelesen | **✔ früher** (P1 liest, P2 pflegt) |
| **S1-O4** | Warnkriterienkatalog W1–W6 | **✔ früher** (S2 + P1) |
| **S1-O5** | `WS_*`-Spiegelung der Ränge 1/2 | **✔ früher** (A1) |
| **S1-O6** | Ergebnis-Buchführung je Kanal ohne Persistenz | **✔ früher** (E1) |
| **S1-O7** | Sammelzeile auf K2-O5/O6/O8 | **✔ früher** |
| **S1-O8** | Engine-/Dialogtexte des S1-Umbaus inline deutsch | **✔ L** — `SIMENG_SENKE_OHNE_LADEAUFTRAG_RANG` samt seinen zwei Rangzusätzen |
| **S2-O1** | W4, W6 und der T_Nutz-Anteil von W3 vertagt | **✔ früher** (P1) |
| **S2-O2** | `HART_LEERES_SET` auf heutigen Daten nicht erreichbar | ○ **bleibt** — das Netz für die programmatischen Schreibwege; die Verwendungs-Altlast ist mit L nicht gefallen (Konzept 15) |
| **S2-O3** | Befund S2-B1: Engine-Kurzschlussguard las nur die Altspalten | **✔ früher** (A1) |
| **S2-O4** | drei Ressourcen der abgelösten Verwendungs-Sperre | **✔ früher** (A1) |
| **S2-O5** | Verbund-Kandidatenliste weiter verwendungsgefiltert | ○ **P1/P2 → offen** — der Verbund selbst ist nicht auf das Klassen-Set umgestellt; eine Auswahl anzubieten, die die Prüfung zurückweist, wäre eine Sackgasse |
| **S2-O6** | Ladeposition der Schema-Kanten aus der altspaltenbasierten Ladeordnung | **✔ früher** (A1) |
| **S2-O7** | eigene Kantenfarbe des Prozessknotens | **✔ früher** (E1) |
| **S2-O8** | Bauform im W2-Text als roher Persistenzwert | **✔ L** (4.2) |
| **S2-O9** | Protokollrahmen ringsum inline deutsch | **✔ L** für die seit K2 entstandenen; der Altbestand bleibt (5.4, Ticket **L-R1**) |
| **A1-O1** | Produktiv-Migration lief vorzeitig, Neustart nötig | ○ **Anwender** |
| **A1-O2** | `KomponentenUebernahmeCtrl` kopiert `Z_AnlageSenke` nicht | **✔ L** (3.1, mit Wirkprobe) |
| **A1-O3** | aufruferfrei gewordene Bausteine | **✔ L** — 6 von 7 geschnitten; `DbWerte.ERZEUGER_GESAMTSYSTEM` bleibt begründet stehen (2.1) |
| **A1-O4** | `SenkenPufferDerAnlagen` liest zusätzlich `WS_ID_Puffer/2` | ○ **bleibt** — Duldungs-Kommentar geschärft (2.1); fällt erst mit den Spalten, und die bleiben |
| **A1-O5** | `Waermekanaele` bleibt (Invariantenprüfung im Debug-Selbsttest) | ○ dokumentiert, bleibt |
| **A1-O6** | Verbund-Kandidatenliste (= S2-O5) | ○ offen, siehe S2-O5 |
| **A1-O7** | 1042 mit unkonfigurierter Quelle eingefroren | ○ **Anwender** — unverändert; `QUELLE_FEHLT` steht im Laufprotokoll dieses Pakets |
| **E1-O1** | Referenzbasis neu setzen | **✔ früher** (Basis `2026-08-28_P1`) |
| **E1-O2** | V0-O1-Fix in der Referenzmenge nicht messbar | ○ **Referenzprojekt** — es fehlt ein Projekt mit Solarthermie an nachgelagerter Kaskadenposition |
| **E1-O3** | Referenzlauf-Werkzeug benutzt `sim.puffer_wp` | ○ **Basiswechsel** — Messinstrument, in L ausdrücklich tabu |
| **E1-O4** | `simulation_wp.Pufferspeicher = puffer_wp` | **✔ L beantwortet** — die Zeile ist **nicht** wirkungslos (`ErdreichAuswertung` liest sie) und bleibt; Beleg in 2.1 |
| **E1-O5** | `T_oben_*` angelegt und NULL | **✔ früher** (P1) |
| **E1-O6** | Kanalaufteilung einer Quellentnahme ist eine Näherung | ○ **B1/Aufräumen → offen** — der Schnitt bräuchte eine einheitliche `Stunde_*`-Schnittstelle über alle vier Module; das ist ein Engine-Umbau, kein Aufräumen |
| **E1-O7** | Deckungsgrad je Kanal aus zwei Größen im `KennzahlenKatalog` | ○ bei Bedarf |
| **E1-O8** | Legende der `SchemaAnsicht` an ihrer Zeilengrenze | ○ **P2 → offen** |
| **E1-O9** | „PufferHeizung ohne `WS_ID_Puffer`: 2" (V0-O6) | ○ **Anwender** — Datenlage, kein Codefehler; im Migrationsnachweis dieses Pakets erneut gemeldet |
| **P1-O1** | Referenzlauf-Werkzeug exportiert keine `T_oben`-CSV | ○ **Basiswechsel** (Messinstrument, tabu) |
| **P1-O2** | `Anschlusshoehe` überall NULL, Dialogpflege fehlt | **✔ früher** (P2) |
| **P1-O3** | `T_Nutz` nur für Brauchwasser | ○ Konzept, bleibt |
| **P1-O4** | Booster-Temperaturkopplung | **✔ früher** (B1) |
| **P1-O5** | kein T_oben-Diagramm | **✔ früher** (P2) |
| **P1-O6** | `Entnahme_*` nicht kanalweise gelesen | **✔ früher** (war bereits mit P1 erledigt, belegt in B1-O5 — hiermit geschlossen) |
| **P1-O7** | Engine-Protokolltexte inline deutsch | **✔ L** — `SIMENG_SCHICHT_INVARIANTE`, `…_VERBUND_SCHICHTUNG`, `…_TNUTZ_UEBER_VORLAUF` |
| **P1-O8** | Pufferdialog 826 px Client-Höhe | ○ kosmetisch |
| **B1-O1** | Quell-Entnahmehöhe fest „oben" | **✔ früher** (Q1, Schritt 54) |
| **B1-O2** | Lesezeitpunkt nach der Ladephase der Vorebene | ○ **Rückfrage Produktverantwortlicher** |
| **B1-O3** | Werkzeug exportiert keine `QUELLTEMP`-CSV | ○ **Basiswechsel** (tabu) |
| **B1-O4** | Schema-Kaskadenkante unterscheidet geteilt/eigenständig nicht | ○ **P2/Bericht → offen** |
| **B1-O5** | P1-O6 ist erledigt, Ticket schließen | **✔ L** (in dieser Tabelle geschlossen) |
| **B1-O6** | 8.3-Restposten (`potenzialTherm/El`, `WP_Betriebsart`, Heapsort-Reste, `Rest_Speicher`, `MAX_WP`-Fehlertext, COP-Guard) | ○ **offen** — siehe Ticket **L-R2** (Abschnitt 7): kein Restposten ist aufruferfrei, jeder verlangt eine eigene Wirkungsanalyse im Rechenkern |
| **B1-O7** | Lokalisierungskatalog ohne Nachtrag für P1 | **✔ L** — Nachtrag mit allen 27 Schlüsseln nachgetragen |
| **B1-O8** | Booster-Fall in der Referenzmenge nicht abgedeckt | ○ **Anwender / Orchestrator** |
| **B1-O9** | Protokolltexte des Laufaufbaus inline deutsch | **✔ L** — `SIMENG_BOOSTER_KOPPLUNG`, `SIMENG_KESSEL_BOOSTER_KOPPLUNG` |
| **B1-O10** | `Tab_Heizkessel.Vorlauf/Ruecklauf` im Bestand ungepflegt | ○ **Anwender** |
| **Q1-O1** | Quellprofil-Fall in der Referenzmenge nicht abgedeckt | ○ **Anwender / Orchestrator** |
| **Q1-O2** | Werkzeug exportiert keine Profil-Ganglinien | ○ **Basiswechsel** (tabu) |
| **Q1-O3** | `WQ_CSV` nicht stillgelegt; Vorschlag „Angebot beim Öffnen" | ○ **nicht umgesetzt** — es wäre neue Funktion (ein Dialog mit Datenänderung), nicht Aufräumen; im produktiven Bestand ist **keine** Anlage betroffen (0 von 131) |
| **Q1-O4** | additiver Wochengang nicht mehr anlegbar | ○ **Rückfrage Produktverantwortlicher** |
| **Q1-O5** | `Tab_Quellprofil.Einheit` wird nicht ausgewertet | ○ dokumentiert, bleibt |
| **Q1-O6** | Spaltenname `Index` ist in Access reserviert | ○ dokumentiert |
| **Q1-O7** | Kalenderwert 3 des Altwegs | ○ dokumentiert |
| **Q1-O8** | Werteraster behält Zeilen beim Betriebsartwechsel | ○ kosmetisch |
| **Q1-O9** | Protokollzeile der Anschlusshöhe inline deutsch | ○ **bleibt** — `AnschlusshoeheText` ist ein Text**baustein**, kein Meldungsrumpf; er geht als Platzhalter in die beiden umgezogenen Booster-Meldungen ein |
| **Q1-O10** | `P1-O2` bleibt offen | **✔ früher** (P2) |
| **P2-O1** | `ZeitreihenExtraktor` schreibt Schlüssel als Zeichenketten, Legenden inline | **✔ L** (2.3) |
| **P2-O2** | Diagrammseite hängt an der WP-Registerkarte | ○ **offen** — gemeinsam mit der Speicher-Ergebnistabelle zu lösen |
| **P2-O3** | Berichts-Temperaturverlauf fehlt im Variantenbericht | ○ **Variantenbericht** |
| **P2-O4** | `ChartRenderer` beschriftet deutsch hart | ○ **offen** — gehört zum Altbestand aus 5.4 (Ticket **L-R1**); der `ChartRenderer` liegt zudem außerhalb des lokalisierten Bereichs der Prüfrezeptur |
| **P2-O5** | `Form_Waermesenke` 825 px Client-Höhe | ○ kosmetisch |
| **P2-O6** | Einheitentext der Ladeobergrenze ragt über die Gruppe | **✔ L** (3.2) |
| **P2-O7** | `Kanal_Knappheitsreihenfolge` hat keine Oberfläche | ○ **K2-Restpunkt** — die Vorbelegung gilt; der Kombi-Hinweis des Senkendialogs stimmt damit immer |
| **P2-O8** | Einspeisehöhe ohne Warnkriterium | ○ **S2-Nachtrag** |
| **FR-3** | `SIMQ_MSG_LUFT_WASSER` um den Bauart-Änderungsweg ergänzen | **✔ L** (4.1) |

**Bilanz: 15 Tickets mit Paket L erledigt oder abschließend beantwortet**, 24 waren vor L
erledigt, 27 bleiben bewusst offen (davon 6 beim Anwender, 4 am Messinstrument/Basiswechsel,
2 als Rückfrage an den Produktverantwortlichen).

## 7. Neue Punkte aus diesem Paket

| # | Punkt | Ziel |
|---|---|---|
| **L-R1** | **45 inline-deutsche Protokolltexte des Altbestands** (vor K2, Pakete 8/9 und älter) stehen weiter im Quelltext — Fundstellen und Zählung in 5.4. Ein Umzug ist mechanisch dieselbe Arbeit wie in diesem Paket, aber über die dreifache Textmenge und ohne Ticketbezug | eigenes Lokalisierungspaket |
| **L-R2** | Die 8.3-Restposten aus B1-O6 sind **nicht** aufruferfrei und deshalb hier nicht geschnitten worden. `potenzialTherm/El`, `WP_Betriebsart`, die Heapsort-Reste und die `Rest_Speicher`-Gruppe stehen im Rechenkern; jeder Schnitt braucht eine eigene Wirkungsanalyse und einen eigenen Byte-Nachweis | eigenes Paket |
| **L-B1** | **Befund, gemessen: Der Gewerkaustausch „Pufferspeicher" scheitert an `FK_AnlageSenke_Puffer`.** `KomponentenUebernahmeCtrl.PufferverweiseLoesen` leert vor dem Löschen der Speicherzeilen nur die vier `Tab_Energieanlagen`-Spalten (`PUFFER_VERWEISE`). Eine `Z_AnlageSenke`-Zeile, die auf einen zu löschenden Zielspeicher zeigt, blockiert das DELETE über die **restriktive** Beziehung aus Schritt 50 — die Transaktion rollt zurück, die Übernahme schlägt fehl.<br><br>**Wirkprobe** (Wegwerf-Kopie, Schemastand 54): Puffer **1008007** wird von einer `Z_AnlageSenke`-Zeile referenziert; nach dem Leeren aller vier Anlagenspalten meldet `DELETE FROM Tab_Pufferspeicher WHERE ID = 1008007` → *„Der Datensatz kann nicht gelöscht oder geändert werden, da die Tabelle 'Z_AnlageSenke' in Beziehung stehende Datensätze enthält."*<br><br>Der Befund ist mit S1 entstanden (die Beziehung kam mit Schritt 50), ist **unabhängig von A1-O2** und in L bewusst nicht mitbehoben: Die symmetrische Lösung wäre, `PufferbezuegeSichern`/`PufferverweiseLoesen`/`-Wiederherstellen` auf `Z_AnlageSenke.ID_Puffer` auszudehnen — ein eigener Eingriff in einen transaktionalen Löschweg, der eine eigene Wirkprobe verdient | Einzelfix |
| **L-B2** | **Befund: `Form_Simulation_Config.listErzeuger` ist tot.** Das private Feld wird geleert und gefüllt (`Clear`, zwei `Add`), aber **nirgends gelesen** — repo-weit belegt. Es stammt aus dem Speicherweg der Alt-Zuordnung. In L stehen gelassen, weil es außerhalb des Ticketumfangs liegt und der Schnitt eine Formulardatei anfasst, die dieses Paket sonst nicht berührt | Aufräumen |
| **L-B3** | `WaermesenkeClass.SenkeDaten.Kopie()` ist mit dem Schnitt von `KaskadeNotwendig` **aufruferfrei** geworden. Bewusst stehen gelassen und im Code begründet (2.1) | dokumentiert, bleibt |

## 8. Was NICHT geändert wurde

- **Die Datenbank.** Kein Schema-Schritt, keine Tabelle, keine Spalte, kein DML.
  `ZIEL_VERSION` bleibt **54**.
- **Der Rechenkern.** Keine rechnende Zeile ist angefasst; die vier vereinfachten
  Verzweigungen in `Form_Simulation_Detail` liegen in der Anzeige und nehmen jeweils genau den
  Zweig, den sie seit A1 ohnehin genommen haben.
- **Die deutschen Protokolltexte.** Zeichengenau unverändert (5.3).
- **Das Referenzlauf-Werkzeug** und die eingefrorenen Laufordner.
- **Formular-Designer und Formular-`.resx`.** Die einzige Layout-Änderung (P2-O6) steht
  programmatisch im Formularcode.
