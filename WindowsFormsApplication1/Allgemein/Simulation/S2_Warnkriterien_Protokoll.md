# Paket S2 — Warnkriterienkatalog und freie Zuordnung: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 6.2 (Warnkriterien W1–W6), 6.3, 8.4, 10, Paketzeile **S2**; Vorleistung
[`S1_Senkentabelle_Protokoll.md`](S1_Senkentabelle_Protokoll.md) (Senkentabelle `Z_AnlageSenke`,
Schema `ZIEL_VERSION = 50`). Entscheidung **F6** (freie Zuordnung mit Ausschlusskriterien).
Build x64 Debug: 0 Fehler. **Kein Schema-Schritt** — S2 fasst die Datenbank nicht an.

## 1. Umfang

Die **sperrende Pufferfilterung fällt**. Bis S1 durfte eine Senkenzeile nur auf einen Speicher
zeigen, dessen `Verwendung` genau zum Ziel passte; das Speicher-Dropdown zeigte nur diese, und
`WaermesenkeClass.Pruefen` wies alles andere ab. An ihre Stelle tritt ein **zentraler
Warnkriterienkatalog**: Zuordnungen sind frei, unplausible bekommen eine begründete Warnung —
im Dialog beim Speichern und als Zeile im Laufprotokoll. Hart gesperrt bleiben nur Kurzschluss,
Ring und das leere Klassen-Set.

Dazu kommen die Anzeigearbeiten aus Konzept 10: der **dritte Abnehmerknoten Prozesswärme** im
Schema, Speicherbadges und Versorgungskanten aus dem **Klassen-Set** statt aus der
Alt-Verwendung, Ladekanten über **alle Senkenränge**, die **Kessel-Quellkette** und ein
**Warn-Chip** auf der Erzeugerkarte. Zwei offene K2-Tickets sind miterledigt (K2-O5, K2-O8).

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Warnkriterienkatalog** | `Warnbefund` (Kriterium, Hart, ID_Anlage, ID_Puffer, Text) + `Warnkriterien` mit `PruefeProjekt(idProjekt)`, `PruefeSenke(idProjekt, idAnlage, senke)` und der Listenfassung `PruefeSenken(…)`; Schlüssel `W1`…`W6`, `HART_KURZSCHLUSS`, `HART_RING`, `HART_LEERES_SET` sprachneutral, Texte über `MyResource.Resource.SIMWARN_*`. Ein internes `Projektbild` liest je Aufruf **vier** Abfragen (Verschaltung, Pufferzeilen, Puffer-Bestandsdarstellung, Senkenlisten) und bedient daraus alle Kriterien | NEU `Allgemein/Simulation/Warnkriterien.cs` |
| **W1 Ziel ∉ Klassen-Set** | Ziel → Kanäle (`PufferHeizung`→{H}, `PufferBrauchwasser`→{B}, `PufferProzess`→{P}, `PufferKombi`→{H,B}) gegen das Klassen-Set des gewählten Speichers; der Text nennt die **fehlenden** Kanäle | `Warnkriterien.ZeilePruefen`, `Warnkriterien.ZielKanaele` |
| **W2 Bauform ≠ Klassen-Set** | Eine auf Warmwasser ausgelegte **Bauform** (`Speichertyp` = `Kombispeicher` oder `Solarspeicher`) ohne Brauchwasser im Set. Die Gegenrichtung (Pufferspeicher-Bauform **mit** Brauchwasser) ist ausdrücklich **kein** Befund — ein Puffer mit Frischwasserstation ist der Regelfall. Leere Bauform: kein Befund | `Warnkriterien.SpeicherPruefen`, `Pufferdaten.BauformWarmwasserseitig` |
| **W3 Vorlauf < VL_eff** | Erzeuger-Vorlauf (`Tab_Energieanlagen.Vorlauf` > 0) gegen `VL_eff` des Zielspeichers nach der Bestandsregel: gepflegtes Paar → `Vorlauf`; sonst `Ruecklauf + ΔT` mit ΔT = 10 K (20 K am BHKW-Pendelspeicher) — dieselbe Rückfallregel wie `SimulationPufferspeicher.Init` | `Warnkriterien.WirksamerVorlauf` |
| **W5 Quellpuffer ohne Lader** | Puffer mit `WQ_Typ='Pufferspeicher'` + `WQ_ID_Puffer` (Engine-Auflösung: nur WP/Kessel), auf den **keine** Senkenzeile irgendeiner Projektanlage zeigt | `Warnkriterien.QuelleOhneLaderPruefen` |
| **Harte Kriterien** | Kurzschluss (Quelle = eigenes Ladeziel, über **alle** Ränge, Anzeige-Auflösung des Quellpuffers), Ring (dieselbe Ebenen-Relaxation wie Dialog und Engine, `Hydraulikbild.Ebenen`), leeres Klassen-Set | `Warnkriterien.ZeilePruefen/RingPruefen/SpeicherPruefen` |
| **Laufstart** | `WarnkriterienMelden()` nach dem Registry-Aufbau und vor beiden Rechenwegen; jeder Befund als `Simulation Warnung:` über `WarnungEinmal` mit Schlüssel `warnkriterium-<Kriterium>-<Anlage>-<Puffer>`. **Kein Abbruch, auch nicht bei harten Befunden** | `SimulationControl.cs` (Aufruf hinter dem Rechenweg-Hinweis, Methode bei `KesselQuelleOhneWirkungMelden`) |
| **Senkendialog: freie Auswahl** | `PufferlisteZuZiel` liefert für **jedes** Puffer-Ziel dieselbe Liste — alle Projekt-Puffer; `FuelleCombo` gruppiert nach Klassen-Set (Gruppenkopf `— Heizung + Brauchwasser —`, Ordnung nach der sprachneutralen Bitmaske H=1/B=2/P=4, kein Kopf bei nur einem Set); `Puffer_Ausgewaehlt` verhindert, dass ein Kopf gewählt bleibt | `Views/Simulation/Form_Waermesenke.cs` |
| **Senkendialog: Prüfung** | `ListePruefen` gibt den Kurzschluss an den Katalog ab (`ErsterHarter` blockiert); die **weichen** Befunde erscheinen NACH dem Speichern in derselben MessageBox wie die bisherigen Hinweise, nur mit Warnsymbol (`SIMWARN_DIALOG_KOPF` + Aufzählung) | `Form_Waermesenke.ListePruefen`, `.btnOk_Click`, `.WeicheBefunde` |
| **Sperre abgeräumt** | Die dritte Prüfung in `PufferPasst` (Verwendung ≠ Ziel) ist entfallen; die beiden echten Blocker (kein Speicher gewählt, Speicher eines fremden Projekts) bleiben samt Absprung in die Pufferverwaltung. `PasstZuFilter` und die gefilterte `ProjektPufferListe` bleiben — die **Entladeordnung** (`Ladeordnung.Entladereihenfolge`, Kanalsicht) und die **Verbund-Kandidatenliste** brauchen sie weiter | `WaermesenkeClass.cs`, `Form_Waermesenke.PufferlisteVerbund` |
| **Schema: Prozessknoten** | `ABNEHMER_PROZESS` als dritter Abnehmerknoten; `SchemaModell` liest die **Senkenlisten** (still über `Z_AnlageSenkeCtrl`, mit Rückfall auf die zwei Altslots) und die **Klassen-Sets**. Daraus: beteiligte Speicher über alle Ränge, Badges je Kanal des Sets, Versorgungskanten je Kanal des Sets, Direktkanten je Ziel/Bedarfsart (`DirektsenkeBedient` als Anzeige-Gegenstück zu `Kaskadenschleife.SenkenMaske`), Ladekanten über alle Ränge, `HauptsenkePuffer` aus Rang 1 der Liste | `Allgemein/Simulation/SchemaModell.cs` |
| **Schema: Kessel-Quellkette** | **Bestand, jetzt belegt und dokumentiert.** `Hydraulikbild.QuellpufferAnzeige` kennt keinen Anlagentyp; ein Heizkessel mit `WQ_Typ='Pufferspeicher'` bekommt seit D5a denselben Speicherknoten, dieselbe gestrichelte Kaskadenkante und dieselbe Kettenbildung wie die Wärmepumpe. S2 ändert daran nichts und ergänzt nur den Kommentar an der Aufbaustelle — das Rechenwerk kommt mit Paket B1 (Konzept 8.4) | `SchemaModell.Aufbauen` |
| **Karten: Warn-Chip** | `WarnbefundeSammeln()` einmal je Auffrischung, `WarnChip()` setzt bei Anlagen mit Befund ein Amber-Chip (`ChipStil.Warnung`) mit den Befunden im Mouseover und `ChipZiel.Senke` als Doppelklickziel. **Kein Modaldialog beim Öffnen.** Auch die Schema-Ansicht frischt die Befunde auf, weil der Chip Teil der Kartenkurzinfo ist | `Form_Simulation_Config.Karten.cs`, `.Schema.cs` |
| **K2-O5** | `PSP_FEHLER_VERWENDUNG_PFLICHT` repo-weit ohne Fundstelle → aus `Resource.resx`, `Resource.en-US.resx` und `Resource.Designer.cs` entfernt | `MyResource/` |
| **K2-O8** | Die Rückfrage vor dem Nutzungswechsel vergleicht die drei **Flags** statt des abgeleiteten Altwerts; {H}→{H,P} schlägt damit an (der Altwert bleibt in beiden Fällen „Heizung"). Neuer Text `PSP_MELDUNG_KLASSENSETWECHSEL` — er sagt jetzt „wird gewarnt" statt „muss neu gesetzt werden" | `Form_PufferSp_Projekt.KlassenSetWechselBestaetigt` |
| **Hilfsmittel** | `Zeilenumbruch.Einzeilig` — bringt einen für die MessageBox geschriebenen Text in die EINZEILE des Protokollkanals (eingebettete Umbrüche zerlegten eine Meldung sonst in zwei) · `PufferSpCtrl.KlassenSetsJeProjekt` — die Klassen-Sets aller Projekt-Puffer in EINER Abfrage, für Senkendialog und Schemamodell (bei Projekt 1023 mit 80 Pufferkopien wäre die Einzelfassung eine Abfrage je Listeneintrag) | `Allgemein/Zeilenumbruch.cs`, `Controller/PufferSpCtrl.cs` |

**Neue Ressourcenschlüssel: 14** (`SIMWARN_*` × 10, `PSP_KLASSENSET_LEER`,
`SIM_PUFFERGRUPPE_KOPF`, `PSP_MELDUNG_KLASSENSETWECHSEL`, `PSP_TITEL_KLASSENSET_AENDERN`),
**1 entfernt**; Bestand 2571, DE und EN deckungsgleich, alle mit Designer-Eigenschaft.
Einzelnachweis in [`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md), Abschnitt
„Nachtrag Paket S2".

## 3. Eine Wahrheit statt vier — was mit den Bestandsguards geschah

| Bestandsguard | Was S2 damit macht |
|---|---|
| `Form_Waermesenke.ListePruefen` — Kurzschluss über alle Ränge | **Umgestellt.** Die Zeile ist ersatzlos entfallen; der Dialog holt den Befund aus `Warnkriterien.PruefeSenken` und zeigt denselben Text (`SIM_PUFFER_QUELLE_UND_SENKE`, unverändert wiederverwendet). Auch die Auflösung des Quellpuffers ist dieselbe geblieben: Fremdschlüssel, sonst Alt-Bezeichner. |
| `WaermesenkeClass.Pruefen`, Punkt 4 (Kurzschluss auf Rang 1/2) | **Bleibt.** Er sitzt tiefer (auf der gespiegelten `SenkeDaten`-Sicht) und deckt Aufrufer ab, die nicht über die Senkenliste gehen. Doppelt geprüft ist harmlos — `ListePruefen` läuft ohnehin zuerst. |
| `WaermesenkeClass.QuellePruefen` (Kurzschluss + Ring von der Quellenseite) | **Bleibt.** Andere Blickrichtung: dort ist die Senke gespeichert und die Quelle neu. |
| `SimulationControl.QuellbezuegeAufbauen`, E-K2-1 | **Bleibt** (Engine). Der Quellbezug entsteht bei Kurzschluss gar nicht erst. |
| `Kaskadenschleife.EbenenRelaxieren`, Ring | **Bleibt** (Engine, **bricht ab**). Der Katalog rechnet den Ring über *dieselbe* Relaxation (`Hydraulikbild.Ebenen`) und meldet ihn **vorab als Warnung**; er bricht **nicht** ab. Zwei Abbrüche für dieselbe Sache hätten dem Anwender die genauere Meldung genommen. |

**Befund S2-B1 (Blindstelle im Engine-Kurzschlussguard).** In der Wirkprobe (Abschnitt 4.3) hat
der Katalog den Kurzschluss gemeldet, der Engine-Guard E-K2-1 **nicht**: Er liest
`IstEigenerSenkenPuffer` ausschließlich aus `WS_ID_Puffer`/`WS_ID_Puffer2`
(`SimulationControl.cs:3539`) und sieht Senkenzeilen ohne Altspalten-Spiegelung — also alles ab
Rang 3 und alles, was programmatisch nur in `Z_AnlageSenke` steht — nicht. Nicht in S2 behoben:
Die Umstellung wäre ein Eingriff in den Rechenweg und gehört zum Abriss der Spiegelung (Paket A1,
S1-O5). Der Katalog schließt die Lücke bis dahin auf der Meldeebene.

## 4. Verifikation

### 4.1 Build

`WP-Plan.sln`, Debug × x64: **0 Fehler**; die vier verbliebenen Warnungen (CS0108/CS0109/CS1998)
sind Bestand und liegen außerhalb dieses Pakets.

### 4.2 Referenzlauf — 9/9 PASS, 216/216 CSV byte-gleich

Lauf auf der migrierten Arbeitskopie (Schemastand 50) über die Referenzmenge
1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030; Vergleich gegen die Basis
`2026-08-27_K1`:

```
Projekt_1007..1030: 9 × PASS   (2 366 177 Werte innerhalb der Toleranz)
MD5-Vergleich:      216 von 216 CSV byte-gleich, 0 Abweichungen
```

Das ist die zugesagte Messlatte: **S2 ist reine Warn- und Anzeigeschicht** und rührt keinen
gerechneten Wert an. Der Probeordner `Referenzlaeufe\2026-08-27_S2_Probe` ist nach dem Vergleich
gelöscht — er wäre byte-gleich mit der Basis und gehört nicht ins Git. **Die Basis
`2026-08-27_K1` bleibt unverändert gültig.**

### 4.3 Zwei Findlinge auf der Referenzmenge — dokumentiert, nicht weggefiltert

Die Vorgabe lautete „auf den neun Referenzprojekten darf keine neue Warnung erscheinen". Es sind
**zwei** erschienen, beide inhaltlich richtig:

| Projekt | Kriterium | Meldung und Befund |
|---|---|---|
| **1021** | **W5** | „Anlage *CS7800iLW 12*: Der Speicher *allSTOR exclusiv VPS 800/3-7* ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen." Nachgeprüft: Anlage 10361 (Wasser-Wasser-WP) trägt `WQ_Typ='Pufferspeicher'`, `WQ_ID_Puffer=1018014`; **keine** der drei Anlagen des Projekts hat eine Puffer-Senke (alle drei Rang-1-Ziele lauten `Heizkreis`). Die Quelle rechnet also nur mit ihrer Startfüllung. Anwender-Datenpflege, kein Codefehler. |
| **1023** | **W3** | „Anlage *CS6800iAW MB + AW 10 OR-T*: Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers *Vitocell 140-E 600 Ltr*." Nachgeprüft: Anlage 11203 `Vorlauf = 45`, Puffer 1018023 `Vorlauf/Ruecklauf = 65/45`. **Diese Konstellation ist nicht neu** — die Erzeugerkarte und der Erzeugerknoten des Schemas zeigen sie seit Etappe D2/D3 als amberfarbene Temperatur-Warnung (`SchemaModell.ErzeugerKnotenAnlegen`, Regel „Erzeuger-Vorlauf < Puffer-Vorlauf der Hauptsenke"). Neu ist allein, dass sie auch im **Laufprotokoll** steht. |

Beide Meldungen sind Protokollzeilen; die CSV-Ergebnisse sind davon unberührt (siehe 4.2).

### 4.4 Wirkprobe der Kriterien (Wegwerf-Arbeitskopie)

Auf `Referenzlaeufe\Arbeitskopie` — **nach** dem Referenzlauf — wurden Konstellationen per SQL
hergestellt und je Runde mit `Referenzlauf.exe projekt 1024 <ziel> <arbeitskopie>` belegt
(die Ausgabeordner liegen außerhalb des Repos). Danach wurde die Arbeitskopie über
`Referenzlauf.exe migration` frisch aus der produktiven Datenbank neu angelegt und nachgeprüft,
dass keine der Änderungen zurückblieb. **Die produktive `Kenndaten.accdb` wurde nicht
beschrieben.**

| Runde | Konstellation (Projekt 1024) | Erwartet | Ergebnis |
|---|---|---|---|
| 1 | Puffer 1054164 auf {H, B} gesetzt, Senkenzeile 69 (Anlage 11257, Rang 2) auf Ziel `PufferProzess`; Puffer 1036082 Bauform `Kombispeicher` bei Set {H}; Anlage 11262 (WP) `WQ_Typ='Pufferspeicher'`, `WQ_ID_Puffer=1036082` | W1, W2, W5 | **alle drei gemeldet.** W1: „Anlage *A-Tron_21_F* (Rang 2): Der Speicher *Vitocell 140-E 600 Ltr (2)* wird als Pufferspeicher Prozesswärme geladen, sein Klassen-Set lautet aber Heizung + Brauchwasser. Der Kanal Prozesswärme fehlt …" · W2: „Speicher *Vitocell 140-E 600 Liter*: Die Bauform *Kombispeicher* ist auf Warmwasser ausgelegt, das Klassen-Set lautet aber Heizung …" · W5: „Der Speicher *Vitocell 140-E 600 Liter* ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen." Exit 0 |
| 2 | zusätzlich: Anlage 11262 bekommt eine Senkenzeile Rang 2 auf **ihren eigenen** Quellpuffer 1036082 (nur in `Z_AnlageSenke`, ohne Altspalten-Spiegelung) | HART_KURZSCHLUSS | **gemeldet** („Der Pufferspeicher *Vitocell 140-E 600 Liter* ist bereits die WÄRMEQUELLE dieser Anlage …"). W5 verschwindet folgerichtig — der Puffer hat jetzt einen Lader. Der Engine-Guard E-K2-1 schwieg (Befund S2-B1). Exit 0 |
| 3 | Ring: WP 11262 Quelle = Puffer 1036082, lädt Puffer 1054164; Heizkessel 11255 Quelle = Puffer 1054164, lädt Puffer 1036082 | HART_RING **und genau ein** Abbruch | **beides.** `Simulation Warnung:` „Die Quellbezüge der Pufferspeicher bilden einen RING: CS6800iAW MB + AW 10 OR-T (Quelle: Vitocell 140-E 600 Liter) …" **vor** dem Lauf, danach der unveränderte Engine-Abbruch `Simulation FEHLER: Kaskade: … der Lauf bricht ab.` Exit 3. Kein doppelter Abbruch. |

Ein Zwischenversuch mit dem **BHKW** als Ringglied blieb folgerichtig ohne Ringmeldung: Für
Erzeugerarten ohne Quellenwahl baut die Engine keinen Quellbezug auf (E-K2-2), und der Katalog
fragt für W5 und den Ring dieselbe Engine-Auflösung — was nie entsteht, kann weder leerlaufen
noch einen Ring schließen. Die Engine hat den Fall wie bisher als „Eintrag bleibt WIRKUNGSLOS"
gemeldet.

### 4.5 Datenlage der Referenzmenge (Begründung der W2-Regel)

Sämtliche Puffer der neun Projekte tragen die Bauform `Pufferspeicher` (bzw. leer, in einem Fall
den Altdatenrest `blabla`); **kein einziger** ist als `Kombispeicher` oder `Solarspeicher`
gepflegt. Die W2-Regel „warmwasserseitige Bauform ohne Brauchwasser im Set" kann auf der
Referenzmenge deshalb nicht anschlagen — was die Wirkprobe (Runde 1) durch eine eigens gesetzte
Bauform belegen musste. Die verworfene Gegenrichtung hätte dagegen sofort gefeuert: Projekt 1024
führt einen Puffer der Bauform `Pufferspeicher` mit Klassen-Set {B}, also genau den Regelfall
„Puffer mit Frischwasserstation".

### 4.6 Schema-Probe (Modell statt Pixel)

Ein Wegwerf-Konsolenprogramm außerhalb des Repos (Projektreferenz auf die App, DB-Pfad per
Reflection auf die Arbeitskopie) baut `SchemaModell.Aufbauen(id, null)` für alle neun Projekte,
lässt die eingebaute Selbstprüfung `SchemaModell.Pruefen()` laufen und zählt Knoten, Kanten und
Ketten — die für Etappe D4 vorgesehene Prüfform „Knoten- und Kantenliste gegen die Datenbank,
statt Pixel gegen ein Bild".

```
1007  Knoten  6  Kanten  6  Abnehmer: Heizkreis · Warmwasser
1008  Knoten  8  Kanten  7  Abnehmer: Heizkreis                     Speicher 1008007 [Heizung]
1011  Knoten 17  Kanten 28  Abnehmer: Heizkreis · Warmwasser · PROZESSWÄRME
1017  Knoten  7  Kanten  6  Abnehmer: Heizkreis
1018  Knoten  6  Kanten  5  Abnehmer: Heizkreis                     Speicher 1054175 [Heizung]
1021  Knoten  5  Kanten  5  Abnehmer: Heizkreis   Kaskadenkanten 1  Speicher 1018014 [Heizung]
1023  Knoten  9  Kanten  8  Abnehmer: Heizkreis · Warmwasser        Speicher 1018023 [Heizung]
1024  Knoten  9  Kanten 11  Abnehmer: Heizkreis · Warmwasser        Speicher 1054164 [Warmwasser]
1030  Knoten  8  Kanten  7  Abnehmer: Heizkreis                     Speicher 1054170 [Heizung]

Selbstpruefung: keine Beanstandung.
```

Zwei Aussagen daraus:

- **Der Prozessknoten steht.** Genau **Projekt 1011** trägt ihn — das einzige Projekt der
  Referenzmenge mit Prozesswärme, dem Migrationsregel R-Prozess in S1 acht `Prozesswaerme`-Zeilen
  angelegt hat. Die übrigen acht Projekte bekommen ihn nicht, weil sie keinen Prozessanteil haben.
- **Invariante S-1 hält.** Keine Kante Speicher → Speicher, kein doppelter Knotenschlüssel, keine
  Kante ohne Endknoten, kein Speicherglied direkt hinter einem Speicherglied — über alle neun
  Modelle.

**Kessel-Quellkette, gezielt belegt.** Auf der Arbeitskopie bekam der Heizkessel *ecoVIT VKK 186/5*
(Anlage 11205, Projekt 1023) `WQ_Typ='Pufferspeicher'`, `WQ_ID_Puffer=1018023`; das Feld wurde
unmittelbar danach zurückgesetzt und der Ausgangszustand nachgeprüft.

| | Knoten | Kanten | Ketten | Kaskadenkanten |
|---|---|---|---|---|
| 1023 ohne Kessel-Quelle | 9 | 8 | 0 | 0 |
| 1023 **mit** Kessel-Quelle | 8 | 8 | **2** | **1** |

Der Kessel verliert seinen eigenen Quellkasten („Systemrücklauf") und bekommt stattdessen die
gestrichelte Kaskadenkante **Puffer → Kessel** samt zweier Kaskadenketten — Zeichen für Zeichen
dieselbe Darstellung wie bei einer Wärmepumpe mit Quellpuffer. Das Rechenwerk dahinter bleibt
unverändert und kommt mit Paket B1 (Konzept 8.4).

## 5. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| S2-O1 | **W4** (`T_Nutz_BW` > `VL_eff`), **W6** (`Schichten_Anzahl` > 1 am Leitspeicher eines Verbunds) und der **`T_Nutz`-Anteil von W3** sind bewusst vertagt: Beide Spalten entstehen erst mit Schema-Schritt 53. Die Schlüssel `W4`/`W6` sind in `Warnkriterien` bereits reserviert und im Klassenkopf begründet; W6 wird zudem **abgewiesen** statt gewarnt und gehört deshalb in den Speicherdialog von P1 | P1 |
| S2-O2 | `HART_LEERES_SET` ist auf heutigen Daten **nicht erreichbar**: `PufferSpCtrl.KlassenSetAusZeile` fällt bei fehlenden oder durchweg falschen Flags geordnet auf die Ableitung aus `Verwendung` zurück, und die liefert mindestens {Heizung}. Der Guard ist das Netz für die programmatischen Schreibwege und für den Tag, an dem die Verwendungs-Altlast stillgelegt wird | A1 |
| S2-O3 | **Befund S2-B1**: `SimulationControl.IstEigenerSenkenPuffer` liest nur die beiden Altspalten und übersieht Kurzschlüsse, die allein in `Z_AnlageSenke` stehen (ab Rang 3 bzw. ohne Spiegelung). Der Katalog meldet sie; der Engine-Guard wird mit dem Abriss der Spiegelung nachgezogen | A1 (mit S1-O5) |
| S2-O4 | Drei Ressourcenschlüssel der abgelösten Verwendungs-Sperre stehen ohne Fundstelle da (`SIM_PUFFER_VERWENDUNG_PASST_NICHT`, `PSP_MELDUNG_VERWENDUNGSWECHSEL`, `PSP_TITEL_VERWENDUNG_AENDERN`). Bewusst **nicht** entfernt — die Alt-Verwendung selbst fällt erst mit Schritt 51, und der Aufräumschnitt gehört in dasselbe Paket | A1 |
| S2-O5 | Die **Verbund-Kandidatenliste** behält die Filterung nach `Verwendung`: `AnlagePufferVerbundCtrl.KonfliktPruefen` weist einen Verbund aus gemischten Verwendungen beim Speichern weiterhin ab (`GRUND_PASST_NICHT`), und eine Auswahl anzubieten, die die Prüfung zurückweist, wäre eine Sackgasse. Öffnen erst, wenn der Verbund selbst auf das Klassen-Set umgestellt wird | P1/P2 |
| S2-O6 | Die Ladeposition an den Schema-Kanten (Kreisziffer, „lädt als n von m") kommt weiterhin aus der **altspaltenbasierten** `Ladeordnung.Ladereihenfolge(idProjekt, idPuffer)`. Für eine Senke ab Rang 3 liefert sie 0 — die Kante wird gezeichnet, trägt aber keine Ziffer. Die Senkenlisten-Fassung der Ladeordnung existiert (`…, List<Senkenliste>`), verlangt aber `WaermesenkeClass.SenkenlistenLaden`, und die schreibt in den Protokollkanal — aus einem Dialog heraus landeten die Zeilen im Protokoll des nächsten Laufs | A1 |
| S2-O7 | Die Kanten**farbe** des Prozessknotens ist die gemeinsame Versorgungsfarbe; Konzept 10 sieht eine eigene vor. `SchemaAnsicht` färbt nach `Kantenart`, eine vierte Art wäre ein Eingriff in die Legende — verschoben, bis E1 die Kanalfarben festlegt | E1 |
| S2-O8 | Die Bauform erscheint im W2-Text als **roher Persistenzwert** (`Speichertyp`). Das ist Bestandsverhalten (`Form_PufferSp_Bearbeiten` zeigt sie ebenso) und hängt an Befund L0-1: Ältere Stände haben den lokalisierten ComboBox-Text in die Spalte geschrieben. Heilung mit L5 | Paket L |
| S2-O9 | Engine- und Dialogtexte des S2-Umbaus sind vollständig lokalisiert; die **Protokollrahmen** ringsum (`SimulationControl`, `Kaskadenschleife`) bleiben inline deutsch wie im Nachbarbestand | Paket L |
