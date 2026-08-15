# Protokollkanal-Nachzug — Engine-Konsolenmeldungen kanalisieren

Folgepaket zu Paket 9 (Befund N9 a/b, Kapitel 25.11 des
[`Paket9_Lokalisierung_Protokoll.md`](Paket9_Lokalisierung_Protokoll.md)) und Umsetzung der dort
formulierten Regel: **erst kanalisieren, dann katalogisieren.**

Ausgangslage: Rund fünfzig `Console.WriteLine` unter `Allgemein/Simulation/` liefen am
dreistufigen Protokollkanal aus Paket 8 (`SimulationProtokoll`) vorbei. Sie erschienen weder in
der Fußzeile von `Form_Simulation_Detail`, noch zählte `Referenzlauf/Protokoll.cs` sie als
Warnungen, noch konnten sie je lokalisiert werden. Zwei davon meldeten **stille
Datenkorrekturen** (`WaermesenkeClass.cs:459/465`), deren eigener Codekommentar seit Paket 5
sagte „Sie gehört ins Lauf-Protokoll" — umgesetzt war das nie.

Dieses Paket ändert **keine Rechenlogik**. Es ändert, wohin eine Meldung geht.

---

## 1. Inventar

Gezählt wurde `Console.Write*` in `Allgemein/Simulation/*.cs`. Die Rohzählung liefert 54
Fundstellen; abzuziehen sind zwei Kommentar-Erwähnungen in `SimulationControl.cs` und
`SimulationProtokoll.cs` sowie der Kanal-Schreiber `SimulationProtokoll.Eintragen` selbst.

**51 echte Meldestellen im Ausgangsstand.**

Die Paket-9-Schätzung „~50, davon `SimulationControl` 24" trifft die Größenordnung; die genaue
Verteilung ist geringfügig anders (26 in `SimulationControl`). Die im Auftrag zusätzlich
genannten Dateien `SimulationPufferspeicher.cs` und `Ladeordnung.cs` enthalten **keine**
`Console.WriteLine` — dort war nichts nachzuziehen.

### 1.1 Klassifizierung

| Kategorie | Definition | Zahl |
|---|---|---:|
| **(a) Warnung** | Gerechnet wurde, aber mit einer Ersatzannahme: Datenkorrektur, Rückfall, Unwirksamkeitsregel, unvollständiger Bedarf | **23** |
| **(b) Hinweis** | Lauf vollwertig, Randbedingung erwähnenswert: Auflösungsketten, Registry-/Zwischenstufen-Meldungen | **11** |
| **(c) Konsole pur** | Entwickler-/Infrastrukturdiagnose ohne Anwenderaussage, teils außerhalb eines Laufs erreichbar | **17** |
| | **Summe** | **51** |

### 1.2 Kategorie (a) — 23 Stellen, jetzt `Warnung` / `WarnungEinmal`

| Datei | Zeile alt → neu | Meldung (Kurzform) | Entprellung |
|---|---|---|---|
| `WaermesenkeClass.cs` | 459 → 468 | Puffer-Hauptsenke ohne `WS_ID_Puffer` → Rückfall HEIZKREIS | je Anlage |
| `WaermesenkeClass.cs` | 465 → 476 | Zweitsenke ohne `WS_ID_Puffer2` → Zweitsenke entfällt | je Anlage |
| `SimulationControl.cs` | 499 → 502 | Kanalbildung: Brauchwasser > Gesamtbedarf, Heizkanal auf 0 gekappt | über das Jahr aggregiert |
| `SimulationControl.cs` | 1037 → 1044 | Puffer-Hauptsenke ohne Ladeauftrag → Rückfall HEIZKREIS (Paket-5-Befund N5) | je Anlage |
| `SimulationControl.cs` | 1093 → 1102 | Wärmepumpenliste nicht lesbar → Stufe ohne Module | einmal je Lauf |
| `SimulationControl.cs` | 1115 → 1124 | Heizkesselliste nicht lesbar | einmal je Lauf |
| `SimulationControl.cs` | 1139 → 1148 | Kollektorliste nicht lesbar | einmal je Lauf |
| `SimulationControl.cs` | 1173 → 1182 | BHKW-Liste nicht lesbar | einmal je Lauf |
| `SimulationControl.cs` | 1533 → 1545 | Registry-Puffer ohne Senkenreferenz → rechnet zweikanalig nicht mit | je Puffer |
| `SimulationControl.cs` | 2026 → 2041 | referenzierter Puffer existiert nicht mehr | je Puffer |
| `SimulationControl.cs` | 2032 → 2048 | Puffer gehört zu einem fremden Projekt | je Puffer |
| `SimulationControl.cs` | 2102 → 2127 | **ΔT-Rückfall** (kein Temperaturpaar → 10 K bzw. 20 K) | je Puffer |
| `SimulationControl.cs` | 2188 → 2214 | Kurzschluss Quelle = Senke in der Registry (Konzept 4.6) | je Puffer |
| `SimulationControl.cs` | 2345 → 2374 | PV-Überschuss nicht vorab bestimmbar → WP ohne PV-Vorrang | einmal je Lauf |
| `SimulationBHKW.cs` | 1230 → 1233 | abweichende Senke eines BHKW-Moduls bleibt unwirksam | je Anlage |
| `SimulationWaermebedarf.cs` | 764 → 767 | Prozesswärme-Berechnung abgebrochen, Ergebnis unvollständig | einmal je Aufruf |
| `SimulationWaermebedarf.cs` | 873 → 877 | Brauchwasser-Berechnung abgebrochen, Ergebnis unvollständig | einmal je Aufruf |
| `SimulationWaermepumpe.cs` | 843 → 845 | Quellspeicher von mehreren Modulen benutzt, Fremdparameter unwirksam | je Speicher |
| `SimulationSolarthermie.cs` | 203 → 207 | Klimaregion nicht lesbar → Wetter des Vorlaufs gilt weiter | einmal je Lauf |
| `WaermequelleClass.cs` | 535 → 547 | `WQ_Quellsystem`/`WQ_Tiefe` unstimmig → als Erdsonde gerechnet | je Anlage |
| `WaermequelleClass.cs` | 556 → 572 | Quelltemperatur nicht ermittelbar → Rückfall Außentemperatur | je Anlage |
| `WaermequelleClass.cs` | 651 → 672 | Quellspeicher nicht aufbaubar → Anlage rechnet ohne Quellspeicher | je Anlage |
| `ErdreichAuswertung.cs` | 245 → 249 | Erdreich-Auswertung fehlgeschlagen → Kennwerte bleiben leer | einmal je Lauf |

### 1.3 Kategorie (b) — 11 Stellen, jetzt `Hinweis` / `HinweisEinmal`

| Datei | Zeile alt → neu | Meldung (Kurzform) | Entprellung |
|---|---|---|---|
| `SimulationControl.cs` | 322 → 322 | Projekteinstellung `Kaskade_Zweikanalig` ist gesetzt | einmal je Lauf |
| `SimulationControl.cs` | 752 → 755 | Solarthermie als Zwischenstufe in die Speicherstufe aufgenommen | einmal je Lauf |
| `SimulationControl.cs` | 760 → 763 | Heizkessel als Zwischenstufe aufgenommen | einmal je Lauf |
| `SimulationControl.cs` | 772 → 775 | BHKW als Zwischenstufe aufgenommen | einmal je Lauf |
| `SimulationControl.cs` | 1296 → 1305 | Pendelspeicher: Temperaturpaar aus der Zuordnungszeile | je Puffer |
| `SimulationControl.cs` | 1356 → 1366 | Pendelspeicher rechnet als Zweitsenke mit | einmal je Lauf |
| `SimulationControl.cs` | 1388 → 1398 | Pendelspeicher nicht in der Entladereihenfolge → ans Ende | je Kanal + Puffer |
| `SimulationControl.cs` | 1663 → 1676 | Speicher nicht in der Entladereihenfolge → ans Ende | je Kanal + Puffer |
| `SimulationControl.cs` | 2061 → 2078 | Registry: Temperaturpaar aus der Zuordnungszeile | je Puffer |
| `WaermequelleClass.cs` | 678 → 704 | `WQ_ID_Puffer` zeigt auf keine Zeile → es gilt der Bezeichner | je Anlage |
| `WaermequelleClass.cs` | 709 → 738 | keine Projektkopie → es gilt der Katalog | je Anlage |

### 1.4 Kategorie (c) — 17 Stellen, bleiben `Console.WriteLine`

| Datei | Zeilen | Begründung |
|---|---|---|
| `SimulationControl.cs` (`TestePVAnlage`) | 2606, 2607, 2611, 2612, 2616 | Entwickler-Selbsttest mit festen Prüfparametern, kein Projektbezug |
| `SimulationBHKW.cs` (`Energieprobe`) | 1652, 1660 | Bilanzprobe des Moduls gegen sich selbst (Befund N8), keine Anwenderaussage |
| `WaermequelleClass.cs` | 174, 195, 231, 256, 336, 398, 426 | Schema-/Infrastrukturdiagnosen — **und** außerhalb eines Laufs erreichbar (siehe 3.2) |
| `StilleDb.cs` | 50, 77, 99 | generische Zugriffsdiagnosen; die fachliche Folge meldet jeweils der Aufrufer |

Alle vier Blöcke haben im Quelltext jetzt einen Kommentar, der die Entscheidung benennt — sonst
liest der nächste Durchgang sie wieder als Rückstand.

---

## 2. Umbau

### 2.1 Ein Aufruf statt zweier Ausgaben

`SimulationProtokoll.Eintragen` schreibt **beides**: Kanaleintrag **und** Konsolenzeile
(`"Simulation Warnung: …"` / `"Simulation Hinweis: …"`). Der Umbau ersetzt deshalb jedes
`Console.WriteLine` durch **einen** Kanalaufruf; eine zweite Konsolenausgabe wäre eine doppelte
Zeile im Lauf-Protokoll.

Der Wortlaut der Meldungen ist unverändert übernommen — bis auf vier Stellen, an denen die
FOLGE ergänzt wurde, weil der Anwender sie sonst nicht ableiten kann: PV-Überschuss („die
Wärmepumpe rechnet ohne PV-Vorrang"), Quelltemperatur („es gilt die Außentemperatur"),
Quellspeicher („die Anlage … rechnet ohne Quellspeicher"), Erdreich-Auswertung („die
Erdreich-Kennwerte dieses Laufs bleiben leer").

Neu ist das Präfix `Simulation Warnung:` / `Simulation Hinweis:` vor der Konsolenzeile. Genau
daran hängt die Zählung in `Referenzlauf/Protokoll.cs`.

### 2.2 `Protokoll` oder `SimulationProtokoll.Aktuell`

- In Instanzmethoden von `SimulationControl` gilt das Feld `Protokoll`. Es wird in
  `Do_Simulation_Intern` (Zeile 256) an `SimulationProtokoll.Aktuell` angedockt, **bevor**
  irgendeine der umgebauten Stellen erreichbar ist.
- In statischen Methoden (`SenkeAufHeizkreisZurueck`, `RueckfallMelden`) und in den übrigen
  Klassen steht `SimulationProtokoll.Aktuell`. Während eines Laufs ist das dasselbe Objekt.

### 2.3 Entprellung

Kein einziger der 51 Fundorte liegt in der Stundenschleife — nachgewiesen über die Aufrufpfade:
alle liegen im Vorlauf (Modulaufbau, Registry, Kontext) oder im Nachlauf (`Energieprobe`,
`ErdreichAuswertung.AusLauf`). Ein 8760-facher Meldungssturm ist damit strukturell
ausgeschlossen.

Trotzdem benutzen alle Stellen in Schleifen über Anlagen oder Puffer die `…Einmal`-Fassungen mit
einem **entitätsbezogenen** Schlüssel (`"deltaT-rueckfall-" + idPuffer`). So bleibt je Entität
genau eine Meldung, auch wenn ein Aufbauschritt in einem künftigen Umbau zweimal läuft. Die
Schlüssel sind über einen gemeinsamen HashSet eindeutig — sie tragen deshalb alle ein
sprechendes Präfix.

---

## 3. Zustandssicherheit

### 3.1 Einstiegspunkte

Es gibt genau zwei lebende Einstiegspunkte, die `SimulationProtokoll.NeuStarten()` rufen:
`SimulationRunner.Simuliere_Intern` (Zeile 99) und
`Form_Simulation_Detail.btn_Simulation_Click` (Zeile 1700). Beide tun es **vor** der
Bedarfsrechnung. `Form_Simulation_Kurz.cs` und `Form_Simulation_Detail - Kopie.cs` rufen zwar
ebenfalls `Do_Simulation`, sind aber laut `.csproj` vom Build ausgeschlossen.

Kein umgebauter Codepfad liegt in einem Konstruktor oder in statischer Initialisierung.

### 3.2 Zwei Stellen bleiben bewusst auf der Konsole

- **`WaermequelleClass.SchemaSicherstellen` und ihre Helfer** laufen auch aus
  `Form_Simulation_Config`, `KonfigurationCtrl` und den Senkendialogen heraus, also außerhalb
  jedes Laufs. Ein Kanaleintrag von dort landete im Protokoll des ZULETZT gelaufenen Laufs.
- **`StilleDb`** ist die gemeinsame stille DB-Fassung für Engine **und** Konfigurations-UI;
  dieselbe Begründung.

### 3.3 Eine bekannte Unschärfe, unverändert übernommen

`SimulationWaermebedarf.Prozesswaerme_berechnen` und `Brauchwasserwaerme_berechnen` werden auch
aus `Form_Prozesswaerme` und `Form_Brauchwasser` heraus gerufen (Vorschau). Die beiden neuen
Warnungen können damit außerhalb eines Laufs in den Kanal schreiben. Das ist **kein neuer**
Zustand: Dieselben Methoden melden seit Paket 8 an zwei anderen Stellen (Zeilen 734 und 847)
über genau denselben Weg. Praktisch harmlos, weil jeder Lauf mit `NeuStarten()` beginnt und
damit alles verwirft, was vorher hineingeriet. Sauber wäre eine Kennzeichnung „Kanal ist aktiv"
— siehe offene Punkte.

### 3.4 Abweichung zur Beispielliste in `SimulationProtokoll`

Der Klassenkopf von `SimulationProtokoll` nennt in seiner Beispielliste den ΔT-Rückfall und den
Senken-Rückfall unter den **Hinweisen**. Maßgeblich ist hier die STUFENDEFINITION darüber:
„Warnungen — gerechnet wurde, aber mit einer Ersatzannahme". Beide Rückfälle sind genau das, und
beide verändern die nutzbare Kapazität bzw. die Deckung erheblich. Sie sind deshalb als
**Warnung** eingestuft; ein Kommentar an `RueckfallMelden` hält die Abweichung fest. Praktische
Folge: Sie werden von `Referenzlauf/Protokoll.cs` gezählt (Hinweise werden es bewusst nicht).

---

## 4. Wirkung auf die Referenzlauf-Suite

`Referenzlauf/Protokoll.cs` bleibt **unverändert**. `AusKindprozess` zählt bereits seit der
Paket-8-Nacharbeit (Befund N13b) beide Schreibweisen `"WARNUNG:"` und `"Simulation Warnung:"`;
die neuen Meldungen treffen das vorhandene Muster ohne Erweiterung.

**Der `vergleich`-Modus vergleicht keine Warnzahlen.** `Vergleich.Ausfuehren` liest
ausschließlich die `Projekt_*`-Unterordner und deren CSV-Werte gegen die Toleranz; das
`lauf_protokoll.md` wird nicht angefasst. Der geänderte Protokollkopf kann eine Regression
deshalb weder auslösen noch verdecken. Die eingefrorenen Basen bleiben unberührt.

---

## 5. Verifikation

Gebaut und gerechnet wurde in einem **eigenen Arbeitsbaum** (`C:\Waermeplan\_wt_kanal`,
`git worktree` auf `ad973e4`) mit ausschließlich den neun geänderten `.cs` obendrauf. Der
Haupt-Checkout und sein `bin\` wurden nicht angefasst; die Anwendung durfte laufen.

### 5.1 Build

VS-MSBuild, `Debug` × `x86`:

```
WindowsFormsApplication1.csproj   BuildExit=0
Referenzlauf.csproj               BuildExit=0
```

**0 Fehler, exakt 6 Bestandswarnungen** — unverändert dieselben:
`KlimaregionStammCtrl.cs(22,24)` CS0109, `KlimaregionStammCtrl.cs(23,48)` CS0109,
`WErzeugerModel.cs(6,20)` CS0108, `StromverbraucherStammCtrl.cs(25,44)` CS0108,
`MDIMainForm.cs(281,17)` CS4014, `MDIMainForm.cs(270,28)` CS1998.

### 5.2 Ergebnisneutralität — 9/9 gegen `2026-08-15_B2`

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)
Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1024: PASS (26 Dateien, 271680 Werte)
GESAMT: PASS (2295987 Werte innerhalb der Toleranz)
```

Schärfer als die Toleranz: **MD5-Vergleich aller 208 CSV → 208 byte-gleich, 0 abweichend, 0
fehlend.**

Damit erledigt sich auch die im Auftrag vorsorglich angekündigte Datenstands-Frage: Die
Produktiv-Datenbank ist gegenüber dem B2-Stand für die neun Referenzprojekte **nicht gedriftet**
— ein A/B gegen einen unveränderten HEAD-Build war nicht nötig, weil es keine einzige Abweichung
zu erklären gab.

*Randnotiz:* Die Suite meldete beim Anlegen der Arbeitskopie
`WARNUNG: Die Quelldatenbank ist geoeffnet (Kenndaten.laccdb vorhanden)` — der Anwender hatte
die Anwendung während des Laufs offen. Gelesen wurde trotzdem nur; das byte-gleiche Ergebnis
belegt, dass die Kopie den B2-Stand trägt.

### 5.3 Meldungswirkung — Lauf-Protokollkopf

| | `2026-08-15_B2` | dieser Lauf |
|---|---:|---:|
| **Warnungen** | 0 | **4** |
| **Fehler** | 0 | 0 |

Die vier setzen sich zusammen aus drei **Engine**-Warnungen und der oben genannten
Suite-Warnung zur `laccdb`. Die drei Engine-Warnungen sind alle vom selben Typ — ΔT-Rückfall,
bisher unsichtbar:

```
Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN
  Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. …
Simulation Warnung: Speicher-Registry: Puffer 1008008 (allSTOR exclusiv VPS 800/3-7) hat KEIN
  Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 9,025 kWh. …
Simulation Warnung: Speicher-Registry: Puffer 1011007 (Vitocell 140-E 600 Liter) hat KEIN
  Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. …
```

Das ist die eigentliche Nachricht dieses Pakets: **Drei Referenzprojekte rechnen seit jeher mit
einem Ersatz-ΔT, und niemand hat es je gesehen.**

Sechs weitere Meldungen erscheinen als **Hinweise** (Temperaturpaar aus der Zuordnungszeile bei
1007/1018, WP-Kennlinien-Extrapolation bei 1011/1023, Ladeprioritäten-Vorbelegung bei 1024) und
werden bewusst nicht gezählt.

### 5.4 Meldungswirkung — präparierter Lauf (1018-Muster, Paket-5-Befund N5)

Auf einer **eigenen Kopie** der Arbeitskopie (`dev\praep\`, außerhalb des Repos, produktive DB
unberührt) wurde die halbe Puffer-Konfiguration hergestellt:

- Anlage 10369 (Kessel): `WS_Ziel = 'PufferHeizung'`, `WS_ID_Puffer = NULL`
- Anlage 10370 (BHKW): `WS_Ziel2 = 'PufferBrauchwasser'`, `WS_ID_Puffer2 = NULL`

Lauf `projekt 1018`:

```
Simulation Warnung: Wärmesenke: Die Anlage 10369 ist auf PufferHeizung gesetzt, hat aber KEINEN
  Pufferspeicher zugeordnet (WS_ID_Puffer leer). Sie rechnet deshalb auf den HEIZKREIS.
Simulation Warnung: Wärmesenke: Die Anlage 10370 hat eine Zweitsenke PufferBrauchwasser ohne
  zugeordneten Pufferspeicher (WS_ID_Puffer2 leer). Die Zweitsenke bleibt unberücksichtigt.
```

Beide Zeilen tragen das Token `Simulation Warnung:`, das `Protokoll.AusKindprozess` zählt — im
Sammellauf schlägt das als `**Warnungen:** ≥ 1` im Kopf durch (5.3 zeigt genau das für die drei
ΔT-Fälle).

**Ergebnis unverändert:** 19 von 19 CSV des präparierten 1018 sind **byte-gleich** zum
unpräparierten Lauf. Die Meldung ist reine Sichtbarmachung — `Normalisieren` hat die halbe
Konfiguration schon immer auf den Heizkreis zurückgesetzt, nur eben stumm.

**UI-Kette (Code-Nachweis):** `Form_Simulation_Detail.LaufmeldungenAnzeigen()` liest
`SimulationProtokoll.Aktuell`, prüft `AnzahlWarnungenUndHinweise` und legt
`HinweistextFuerAnzeige()` als Tooltip auf `label_Laufmeldungen`; der Abbruchdialog nutzt
`FehlertextFuerAnzeige(grund)` (Zeilen 1830 und 2192). Der Variantenbericht liest denselben
Kanal über `Form_Variantentest.cs:333` und `BerichtsDatenSammler.cs:300/302`. Alle vier
Anzeigewege bekommen die 34 nachgezogenen Meldungen ohne weitere Änderung.

### 5.5 Kein Meldungssturm — Zählprobe

Kanaleinträge je Projekt im 9er-Lauf:

| Projekt | 1007 | 1008 | 1010 | 1011 | 1017 | 1018 | 1021 | 1023 | 1024 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Einträge | 2 | 3 | **0** | 2 | 0 | 1 | 0 | 1 | 1 |

Maximum 3, Summe 10 über neun Jahresläufe à 8760 Stunden. Der Normallauf 1010 erzeugt
**null** Meldungen — keine Fehlalarme.

### 5.6 Kodierung und Zeilenenden

Byteweise nachgemessen, vorher/nachher gleich: alle neun Dateien UTF-8 **mit BOM**;
`SimulationControl.cs`, `SimulationBHKW.cs`, `SimulationWaermebedarf.cs`,
`SimulationWaermepumpe.cs`, `SimulationSolarthermie.cs`, `ErdreichAuswertung.cs` durchgehend
CRLF, `StilleDb.cs`, `WaermequelleClass.cs`, `WaermesenkeClass.cs` durchgehend LF (die drei
LF-Dateien aus Paket-9-Befund 25.11 g; `.gitattributes` normalisiert beim Einchecken).

---

## 6. Katalog-Kandidaten (nächster Schritt: katalogisieren)

`MyResource` ist in dieser Sitzung durch ein paralleles Paket belegt; **kein** Text wurde in den
Katalog aufgenommen. Alle 34 kanalisierten Meldungen sind Kandidaten für `SIMENG_*`-Schlüssel.
Vorschlag für die Schlüsselnamen, gruppiert nach Sinnzusammenhang:

| Vorschlag | Stelle | Platzhalter |
|---|---|---|
| `SIMENG_SENKE_HAUPT_OHNE_PUFFER` | `WaermesenkeClass.cs:468` | Anlage, Ziel |
| `SIMENG_SENKE_ZWEIT_OHNE_PUFFER` | `WaermesenkeClass.cs:476` | Anlage, Ziel2 |
| `SIMENG_SENKE_OHNE_LADEAUFTRAG` | `SimulationControl.cs:1044` | Anlage, Art, Ziel, Puffer |
| `SIMENG_KANALBILDUNG_KAPPUNG` | `SimulationControl.cs:502` | Stunden, kWh |
| `SIMENG_STUFE_MODULLISTE_WP` | `SimulationControl.cs:1102` | Projekt |
| `SIMENG_STUFE_MODULLISTE_KESSEL` | `SimulationControl.cs:1124` | Projekt |
| `SIMENG_STUFE_MODULLISTE_SOLAR` | `SimulationControl.cs:1148` | Projekt |
| `SIMENG_STUFE_MODULLISTE_BHKW` | `SimulationControl.cs:1182` | Projekt |
| `SIMENG_REGISTRY_OHNE_SENKENREFERENZ` | `SimulationControl.cs:1545` | Puffer, Bezeichner |
| `SIMENG_REGISTRY_PUFFER_FEHLT` | `SimulationControl.cs:2041` | Puffer |
| `SIMENG_REGISTRY_PUFFER_FREMDPROJEKT` | `SimulationControl.cs:2048` | Puffer, Projekt, Sollprojekt |
| `SIMENG_REGISTRY_DELTAT_RUECKFALL` | `SimulationControl.cs:2127` | Puffer, Bezeichner, ΔT, Q_max |
| `SIMENG_REGISTRY_QUELLE_ALS_SENKE` | `SimulationControl.cs:2214` | Puffer, Anlage, Rolle |
| `SIMENG_PV_UEBERSCHUSS_UNBESTIMMBAR` | `SimulationControl.cs:2374` | Ausnahmetext |
| `SIMENG_BHKW_SENKE_ABWEICHEND` | `SimulationBHKW.cs:1233` | Anlage, führende Anlage |
| `SIMENG_PROZESSWAERME_UNVOLLSTAENDIG` | `SimulationWaermebedarf.cs:767` | Ausnahmetext |
| `SIMENG_BRAUCHWASSER_UNVOLLSTAENDIG` | `SimulationWaermebedarf.cs:877` | Ausnahmetext |
| `SIMENG_QUELLSPEICHER_MEHRFACH` | `SimulationWaermepumpe.cs:845` | Puffer, Anlage |
| `SIMENG_SOLAR_KLIMAREGION_FEHLT` | `SimulationSolarthermie.cs:207` | Projekt, Ersatzwert |
| `SIMENG_QUELLE_TIEFE_UNSTIMMIG` | `WaermequelleClass.cs:547` | Quellsystem, Tiefe |
| `SIMENG_QUELLTEMPERATUR_RUECKFALL` | `WaermequelleClass.cs:572` | Typ, Ausnahmetext |
| `SIMENG_QUELLSPEICHER_AUFBAU_FEHLER` | `WaermequelleClass.cs:672` | Ausnahmetext, Anlage |
| `SIMENG_ERDREICH_AUSWERTUNG_FEHLER` | `ErdreichAuswertung.cs:249` | Ausnahmetext |
| `SIMENG_KASKADE_ZWEIKANALIG_AKTIV` | `SimulationControl.cs:322` | — |
| `SIMENG_ZWISCHENSTUFE_SOLAR` | `SimulationControl.cs:755` | — |
| `SIMENG_ZWISCHENSTUFE_KESSEL` | `SimulationControl.cs:763` | — |
| `SIMENG_ZWISCHENSTUFE_BHKW` | `SimulationControl.cs:775` | — |
| `SIMENG_PENDELSPEICHER_TEMP_ZUORDNUNG` | `SimulationControl.cs:1305` | Puffer, Bezeichner, VL, RL |
| `SIMENG_PENDELSPEICHER_ALS_ZWEITSENKE` | `SimulationControl.cs:1366` | 8 Werte — vor der Übernahme kürzen |
| `SIMENG_PENDELSPEICHER_ENTLADEORDNUNG` | `SimulationControl.cs:1398` | Puffer, Kanal |
| `SIMENG_ENTLADEORDNUNG_NACHTRAG` | `SimulationControl.cs:1676` | Puffer, Bezeichner, Kanal |
| `SIMENG_REGISTRY_TEMP_ZUORDNUNG` | `SimulationControl.cs:2078` | Puffer, Bezeichner, VL, RL |
| `SIMENG_QUELLSPEICHER_ID_OHNE_ZEILE` | `WaermequelleClass.cs:704` | Puffer, Anlage |
| `SIMENG_QUELLSPEICHER_AUS_KATALOG` | `WaermequelleClass.cs:738` | Anlage, Bezeichner |

Zwei Punkte für die Katalogisierung, die jetzt schon sichtbar sind:

1. **`SIMENG_PENDELSPEICHER_ALS_ZWEITSENKE` trägt acht Platzhalter** (Volumen, VL, RL, Q_max,
   Entladeprio, zwei Obergrenzen, Bezeichner). Das ist eine Diagnosezeile in Meldungsform. Vor
   der Übernahme in den Katalog gehört sie gekürzt — oder sie wird nach (c) zurückgestuft.
2. **Persistenzwerte im Text.** `WS_Ziel`-Werte (`PufferHeizung`, `PufferBrauchwasser`) und
   Spaltennamen (`WS_ID_Puffer`) erscheinen roh in den Meldungen. Nach der Drei-Schichten-Regel
   ist das zulässig (sie benennen die zu korrigierende Datenstelle), sollte aber bewusst so
   entschieden und im Glossar vermerkt werden.

---

## 7. Offene Punkte

1. **`SimulationProtokoll` kennt keinen Zustand „Lauf aktiv".** Meldungen aus der Vorschau von
   `Form_Prozesswaerme`/`Form_Brauchwasser` können in den Kanal des zuletzt gelaufenen Laufs
   fallen (3.3). Ein `IstLaufAktiv`-Flag, gesetzt zwischen `NeuStarten()` und dem Ende des
   Laufs, würde das schließen — und wäre gleichzeitig der Schalter, mit dem `StilleDb` und
   `SchemaSicherstellen` (3.2) doch noch in den Kanal könnten, wenn sie aus der Engine kommen.
2. **`TestePVAnlage` schreibt `"WARNUNG:"` auf die Konsole** und wird von
   `Referenzlauf/Protokoll.cs` als Warnung gezählt, obwohl sie ein Entwickler-Selbsttest ist.
   Der Zweig greift nur, wenn die PV-Formel wirklich falsch rechnet (in keinem dokumentierten
   Lauf bisher), deshalb hier nur als Kommentar vermerkt statt umbenannt. Sauber wäre, den
   Selbsttest ganz hinter `#if DEBUG` zu legen — er läuft heute in jedem PV-Lauf mit.
3. **Der ΔT-Rückfall der drei Referenzprojekte ist ein Datenbefund, kein Codebefund** (5.3).
   1008007, 1008008 und 1011007 haben kein Vorlauf-/Rücklaufpaar. Ob das gepflegt werden soll,
   entscheidet der Anwender — die Kapazität ändert sich dadurch.
4. **Katalogisierung** (Kapitel 6) steht aus, `MyResource` war gesperrt.
5. **Kein UI-Sichttest.** Dass die neuen Meldungen in der Fußzeile ankommen, ist über die
   Codekette belegt (5.4), nicht am Bildschirm gesehen.

---

## 8. Was dieses Paket NICHT getan hat

- **Kein Commit.** Der Stand liegt unkommittiert im Arbeitsverzeichnis.
- **Keine Rechenänderung.** 208 von 208 CSV byte-gleich, präparierter Lauf 19 von 19
  byte-gleich.
- **`Referenzlauf/Protokoll.cs` nicht angefasst** — die Zählmuster tragen schon.
- **Keine Datei außerhalb `Allgemein/Simulation/` geändert.** Die parallel bearbeiteten Stände
  (`Views/Simulation/*`, `MyResource/*`, `Views/Hauptformular/Form_Start.*`) blieben unberührt;
  Build und Regression liefen aus einem eigenen Arbeitsbaum.
- **`Referenzlaeufe/*` und `DB-Backup/` unberührt.** Ergebnisse des Regressionslaufs liegen im
  Arbeitsbaum außerhalb des Repos. Produktive Datenbank **nur lesend** (die Suite legt ihre
  eigene Arbeitskopie an); die präparierte Datenbank war eine Kopie der Kopie.
