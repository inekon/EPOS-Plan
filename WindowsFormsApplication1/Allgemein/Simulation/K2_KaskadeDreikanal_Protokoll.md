# Paket K2 — Kaskade dreikanalig: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 4.1/4.3/6.1, Schritt 49, Entscheidungen **F5** (Klassen-Set), **F10**
(Knappheitsreihenfolge übersteuerbar). Build x64 Debug: 0 Fehler.

## 1. Umfang

Die zweikanalige Speicherstufe rechnet jetzt **dreikanalig indiziert**: Der Prozesskanal läuft
durch die komplette Stundenschleife A–G, alle Kanalstrukturen sind über `Kanal.HEIZUNG/
BRAUCHWASSER/PROZESS` indiziert statt boolesch verdrahtet. K2 ist ein **reiner Strukturumbau** —
zwei zentrale Interimsregeln halten das Verhalten bis Paket S1 exakt stabil. Der einkanalige
Altpfad blieb vollständig unangetastet (fällt mit A1).

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **SenkeAbziehen mit Kanalmaske** | `SenkeAbziehen(bool[] maske, double menge, double[] rest, int[] reihenfolge)` + Kompatibilitätsfassung `(string wsTyp, double, double[])`; die `ref rest_ww/rest_heiz`-Fassung bleibt ausschließlich für den Altpfad (Abriss A1) | `Kaskadenschleife.cs:1602-1690` |
| **Knappheitsreihenfolge (F10)** | `Kanal.KnappheitsReihenfolge(string)`/`KnappheitVorgabe()` parsen die sprachneutralen ASCII-Schlüssel (`DbWerte.KNAPPHEIT_*`); Projekteinstellung `Tab_Einstellungen.Kanal_Knappheitsreihenfolge` (Default `BRAUCHWASSER;PROZESS;HEIZUNG`), von `SimulationControl` aufgelöst und in Kaskadenkontext/Vektorstufen gereicht; ungültige Eingaben fallen vollständig auf die Vorgabe zurück (kein halber Zustand) + Protokollwarnung | `SimulationKanaele.cs:462-560`, `SimulationControl.cs:764ff` |
| **Entladeordnungen/Durchsatz indiziert** | `Kaskadenkontext.Entladen[Kanal.ANZAHL]`, `Entladeordnung(int)`; Durchsatzbudget `absehbar[Kanal.ANZAHL]`, `DurchlassBudget`/`DurchlassBuchen` kanalindiziert; Entlade-/Durchsatzphasen laufen in Knappheitsreihenfolge (die Kombi-Regel K-1 geht darin auf) | `SimulationKanaele.cs:1209-1298`, `Kaskadenschleife.cs:546-1440` |
| **Klassen-Set am Speicher** | `SimulationPufferspeicher.NutztKanal[3]`, `BedientKanal(int)` (leeres Set → Ableitung aus `Verwendung` — Objekte außerhalb des Registry-Aufbaus verhalten sich exakt wie bisher), `IstKombi = H&&B`, `KlassenSetSetzen/-Text` | `SimulationPufferspeicher.cs:645-770` |
| **Kanalindizierte Zurechnung** | `_entladungJeArtKanal[ART][KANAL]` neben dem unveränderten Art-Aggregat (Runner liest unverändert); Module erhalten `Direktdeckung_Kanal`/`Speicherentladung_Kanal` (+ `Heizstab_Kanal` WP) als **zusätzliche** Aufschlüsselung — alle Bestandsskalare werden exakt wie bisher befüllt (Voraussetzung für Paket E1) | `Kaskadenschleife.cs:333-460`, alle vier Module |
| **Module dreikanalig** | Alle zweikanaligen Stundenmethoden der vier Erzeuger auf `double[] rest` und `Kanalsatz` umgestellt (WP `Zweikanalig_*`, Kessel/Solar/BHKW `Stunde_*`/`Berechnung_Zweikanalig`); Kanal-Aufschlüsselung als rest-Differenz um `SenkeAbziehen` (keine eigene Verteilregel); Hilfsklasse `Kanalabzug` (Maske/Offen/Abziehen) | `SimulationWaermepumpe.cs`, `SimulationSPK.cs`, `SimulationSolarthermie.cs`, `SimulationBHKW.cs` |
| **Schritt 49** | `Tab_Pufferspeicher` + `Nutzung_Heizung/_Brauchwasser/_Prozess` (YESNO) mit DML aus `Verwendung` (case-insensitiv; Heizung→{H}, Brauchwasser→{B}, Kombi→{H,B}, sonst {H}); `Tab_Einstellungen.Kanal_Knappheitsreihenfolge` TEXT(100) + Default-DML; `ZIEL_VERSION` 48 → 49. Idempotenz über „alle drei Flags falsch" (Access belegt YESNO-Spalten mit FALSCH, nicht NULL — bewusst gekoppelte Bedingung) | `SchemaMigration.cs:1495-6249`, `SchemaKatalog.cs` (Namenskonstanten) |
| **Klassen-Set durchgängig** | `PufferSpCtrl.KlassenSet`-API (eine Ableitungsregel für alle sieben `Verwendung`-Schreiber), spaltentolerante Leser, `KonfigurationCtrl.Knappheitsreihenfolge*` mit **zielgenauem UPDATE** (Ordinal-Lesekette) | `PufferSpCtrl.cs:639-946`, `KonfigurationCtrl.cs:437-575`, Modelle |
| **Dialog** | `Form_PufferSp_Projekt`: drei programmatische Häkchen (Klassen-Set, führend beim Speichern) mit Zwei-Wege-Synchronisation zur bestehenden Verwendungs-ComboBox; Sets ohne Alt-Entsprechung ({H,P} …) mit Tooltip; leeres Set gesperrt | `Form_PufferSp_Projekt.cs:275-420` |

Neue `DbWerte`: `KNAPPHEIT_BRAUCHWASSER/PROZESS/HEIZUNG`, `KNAPPHEIT_DEFAULT`. Neue
Ressourcenschlüssel (de+en+Designer): `PSP_FEHLER_KLASSENSET_LEER`,
`PSP_HINWEIS_KLASSENSET_OHNE_ALTWERT`, `PSP_LABEL_KLASSENSET` (Häkchen nutzen die
`KANAL_*_ANZEIGE`-Schlüssel aus K1).

## 3. Interimsregeln — die S1-Abrisspunkte

Zentral in `Kaskadenschleife.cs:165-262` („ABRISSPUNKT: PAKET S1"):

- **I1 `DirektsenkeMaske(string)`**: Bedarfsart `Beides` → {B, P, H}; `Heizung` → {P, H};
  `Warmwasser` → {B}. Der Prozesskanal war bis K1 Teil des Heizkanals — Heizungs-Direktsenken
  decken ihn übergangsweise mit, bis S1 echte Prozesssenken migriert (R-Prozess).
- **I2 `EntladetKanal(sp, kanal)`**: Ein Speicher mit HEIZUNG im Set bedient übergangsweise auch
  PROZESS. Gilt identisch für Entladeordnung **und** Durchsatzbudget (daran hängt die
  Summengleichheit); das persistierte Set bleibt unberührt.

## 4. Verifikation

Referenzlauf gegen Basis `2026-08-27_K1` (Arbeitskopie migriert auf Schemastand **49**):

**9/9 PASS — und 216/216 CSV byte-/MD5-gleich.** Der Strukturumbau ist auf der gesamten
Referenzmenge nachweisbar verhaltensneutral, einschließlich der vier Speicherstufen-Projekte
(1017, 1018, 1024, 1030). Einordnung: In der Referenzmenge führt kein Speicherstufen-Projekt
Prozesswärme (1011 rechnet bis A1 im Altpfad), und die Knappheitsreihenfolge B→(P)→H ist
deckungsgleich mit dem bisherigen Warmwasser-Vorrang — die dokumentierten Rundungsrisiken
(getrennte Kanal-Akkumulatoren, zweigeteilte `Anteil_Entladen`-Läufe) haben deshalb keinen
einzigen Bytewert verschoben. Sie werden real, sobald ein Speicherstufen-Projekt Prozesswärme
trägt — das vorgesehene Referenzprojekt „Prozesswärme mit eigenem Puffer" (Konzept 11.1) deckt
genau das ab. **Die Basis `2026-08-27_K1` bleibt unverändert gültig.**

## 5. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| K2-O1 | `Kanalabzug` liegt whitelist-bedingt in `SimulationWaermepumpe.cs:2027-2170` und gehört nach `Kaskadenschleife.cs` (dabei `Kanalabzug.Summe` mit dem privaten `Kaskadenschleife.RestSumme` zusammenlegen) | Aufräumen S1/L |
| K2-O2 | `SimulationBHKW.cs:1213` liest noch `sp.IstBrauchwasserkanal` (Verwendung-basiert) statt `BedientKanal(int)` — konsistent, weil der Registry-Aufbau Rolle und Set synchron hält | S1 |
| K2-O3 | `SimulationWaermebedarf.Kanaele()` (Übergangsabbildung aus K1) hat keinen Aufrufer mehr — löschen | S1 |
| K2-O4 | Engine-Protokolltexte des K2-Umbaus inline deutsch (Nachbarstil, Konfliktvermeidung mit paralleler resx-Arbeit) — Ressourcen-Nachzug | Paket L |
| K2-O5 | `PSP_FEHLER_VERWENDUNG_PFLICHT` ist verwaist (Set-Pflichtprüfung ersetzt sie); ComboBox-Ablösung komplett | S2 |
| K2-O6 | `EntladeleistungMax` (0 = unbegrenzt, ungenutzt): Der Zwei-Pass-Durchlauf eines Heizpuffers (P und H je Stunde) würde eine künftige Stundengrenze zweimal zulassen — bei Einführung der Leistungsgrenze je Stunde budgetieren | P1 |
| K2-O7 | Registry-Block 1 (WP-Alt-Zuordnung `Z_ProjektPufferSp`) leitet das Set bewusst aus der Block-1-`Verwendung` ab statt aus den DB-Flags (Regressionszusage der Alt-Zuordnung) — Ausnahme im Code begründet | Abriss mit A1/Schritt 51 |
| K2-O8 | `Verwendungswechsel`-Rückfrage im Puffer-Dialog greift nur bei Änderung des Altwerts; reine Set-Wechsel ({H}→{H,P}) fragen nicht nach — Warnkriterien W1–W6 kommen mit S2 | S2 |
| K2-O9 | Doku-Altlast: `<see cref="Berechnung_Zweikanalig"/>` in `SimulationWaermepumpe.cs:427/:888` zeigt auf eine nicht (mehr) existente WP-Methode | kosmetisch |
