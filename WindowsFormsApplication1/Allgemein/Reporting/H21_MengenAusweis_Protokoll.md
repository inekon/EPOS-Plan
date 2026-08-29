# H2-1 — Mengen-Ausweis beim Dialog-Speichern, Frisch vor Konserve (Umsetzungsprotokoll)

Etappe der H-Serie (Konzept `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` § 4.5).
Stand 30.08.2026, Branch `Pufferspeicher`. Vorgänger: `H2_Endenergie_Protokoll.md`
(dort als offener Punkt H2-1 definiert), `H4a_Bezugsgroessen_Protokoll.md`,
`H4b_Investitionsraster_Protokoll.md`.

## 1. Auftrag und Leitplanke

H2-1 laut H2-Protokoll: „Dialog schreibt beim Speichern den Laufstand nach
`Tab_ProjektWerte.Menge` (‚Stand des Laufs vom …')". Die Leitplanke dazu steht im
Konzept § 4.5 wörtlich:

> Die Spalte `Tab_ProjektWerte.Menge` bleibt **Ausweisgröße** („Stand des Laufs
> vom …"), sie ist **nicht die Rechenwahrheit** — sonst rechnet die Anwendung nach
> einer neuen Simulation stillschweigend mit der alten Bezugsgröße weiter.

Daraus folgen ZWEI Bausteine, nicht einer: Der Ausweis beim Speichern — und die
**Vorrang-Umkehr** an den Lesestellen. Ohne sie würde der frisch geschriebene
Ausweis beim nächsten Lesen als „gepflegte Menge" (H4a-Vorrang) die Ableitung
einfrieren und wäre genau der Fehler, den das Konzept verbietet.

## 2. Umsetzung

### 2.1 Vorrang-Umkehr: FRISCH vor Konserve (drei Lesestellen)

- **Betriebsseite** (`LiesBetriebskosten` + `LiesBetriebskostenPositionen`):
  aus `else if (!menge.HasValue && IstRueckfallErmittelbareArt(bem))` wird
  „frisch immer versuchen, Konserve nur wenn frisch null":
  `frisch = RueckfallMenge(...); if (frisch.HasValue) menge = frisch;`
  Die Endenergie-Arten lasen schon immer frisch (H2) — jetzt gilt dieselbe
  Ordnung für alle ermittelbaren Arten.
- **Investseite** (`InvestBetrag`, H4b-Kaskade): Mengenreihenfolge jetzt
  Kaskadenbasis → Gerätewelt (`BaugroesseSumme`) → **zuletzt** `z.Menge`.
- Der VALERI-Vorrang (gepflegter Best-/Worst-Szenariowert schlägt jede
  Ableitung) bleibt an allen drei Stellen unverändert davor.

### 2.2 Gerätewelt-Arten auch auf der Betriebsseite ermittelbar

`IstRueckfallErmittelbareArt` um die sechs Gerätewelt-Arten erweitert
(je kW Leistung/Heizleistung/elektrisch, je kWp, je kWh Kapazität, je m²
Kollektorfläche); `RueckfallMenge` leitet sie über
`TechnikPlanwertCtrl.BaugroesseSumme` (H4b) ab — eine Wartungszeile
„je kW Heizleistung" zieht ihre kW damit auch in Kategorie 2 selbst.
„% der Erzeugerkosten" bleibt bewusst Kaskadenmaterie der Investseite.

### 2.3 Der Ausweis: `WirtschaftlichkeitCtrl.MengeAusweisen`

`MengeAusweisen(positionsId, out menge)` liest Projekt/Kategorie/Komponente/
Anlage/Bemessung der Zeile, ermittelt für ermittelbare Arten die frische
Bezugsgröße über die VORHANDENEN Helfer (`EndenergieMenge`/`RueckfallMenge` —
keine zweite Ermittlungslogik) und schreibt sie nach `Tab_ProjektWerte.Menge`.
Geschrieben wird auch NULL (nichts ermittelbar = ehrlich kein Stand). Kein
Ausweis für: nicht ermittelbare Arten (deren Menge bleibt Eingabewert, z. B.
„je Stunde") und „% der Investition" in **Kategorie 1** (dort bemisst die
H4b-Kaskade Runde 3, nicht die Kostenwelt-Summe — ein Einzelzeilen-Ausweis
wäre eine zweite, abweichende Zahl).

Aufrufpunkt: `KostenProjektPositionenCtrl.Speichern` — der EINE Speicherweg
der Kostendialoge — nach dem Sichern; `z.Menge`/`zusatz.Menge` werden auf den
Ausweis gestellt, damit der zurückgegebene `BetragNetto` den frischen Stand
zeigt.

## 3. Nachweise (Harness `..\dev\h21\`, Reflection auf `EPOS_Plan.dll`)

Produktiv-DB **nur lesend**; Schreibproben auf frischer Scratchpad-Kopie.
Hauptbuild x64 exit 0, `<<<<<<<`-Sweep ohne Treffer.

### [0] Bestandsneutralität (Produktiv, lesend)

22 Bestandszeilen ermittelbarer Arten (15 in Kategorie 1, 7 in Kategorie 2) —
**keine einzige mit gepflegter Menge**, und alle 7 Kategorie-2-Zeilen (P1018,
`EUR_PRO_KWH_ELEKTRISCH`/`PROZENT_INVESTITION`) ohne Satz → weder die
Vorrang-Umkehr noch die Gerätewelt-Erweiterung ändert einen Bestandsbetrag.
Regressionsanker: Betrieb 1024 = **99,00**; Invest 1018/1024/1042 =
**45.312,50 / 12.001,00 / 13.000,00** — alle exakt. Der frühere Anker
„Betrieb 1042 = 2.055,70" ist auf der Produktiv-DB inzwischen 0,00 — geprüft:
das Projekt hat dort **keine Kategorie-2-Zeilen mehr** (Anwenderarbeit seit dem
H4a-Stand; jüngster Lauf 206 existiert). Datenlage, kein Codeeffekt — mit
leerer Zeilenmenge liefert auch der Altcode 0.

### [1] Dialog-Speichern an der 1042-Kopie

| Probe | Ergebnis |
|---|---|
| a) „je kW Heizleistung", Satz 12, über `Speichern` | DB-Menge **26,00** (= Σ WP-Nennleistung), `z.Menge` 26,00, `BetragNetto` **312,00**, Betriebs-Delta 312,00 — alles exakt |
| b) „je Stunde", Anwender-Menge 100 × Satz 50 | DB-Menge bleibt **100** (kein Ausweis), Delta 5.000,00 |
| E7-Probe | Positionsliste 5.312,00 == Summenschleife 5.312,00 — **GLEICH** |

### [2] Endenergie-Ausweis und Konserven-Verdrängung an der 1024-Kopie

- c) „% der Endenergiekosten", Satz 3, über `Speichern`: DB-Menge **7.154,06 €**
  == `EndenergieAufloeser.FuerPosition(BHKW).KostenEuro` (frischer Laufstand);
  Betriebs-Delta **214,62** = 3 % davon — exakt.
- d) „je kWh thermisch" mit **Konserven-Menge 999** in der DB, Satz 0,10:
  das Lesen rechnet mit der frischen Wärmemenge **161.890 kWh → Delta
  16.189,00 €**, nicht mit der Konserve (99,90) — **FRISCH GEWINNT**; die
  Konserve selbst bleibt unangetastet stehen (kein Dialog-Speichern erfolgt).

### [3] H4b-Kaskadenregression

Dieselbe Drei-Zeilen-Kaskade wie im H4b-Nachweis: Delta **+20.927,61** —
unverändert exakt (die `InvestBetrag`-Mengenumstellung ist für Zeilen ohne
gespeicherte Menge wirkungsgleich).

## 4. Dokumentierte Grenzen

1. **Der Dialog zeigt vor dem ersten Speichern den Konservenstand** — die
   Live-Frisch-Anzeige samt Herleitungszeile („× 14.760,00 € Endenergiekosten ·
   BHKW 1") ist Materie der Dialogetappe (BW9/B5), nicht dieser Etappe.
2. **Der Ausweis darf altern**: Zwischen Speichern und neuem Lauf zeigt die
   Spalte den alten Stand — gewollt („Stand des Laufs"); die Rechenwege lesen
   frisch.
3. **H4b-Grenze 3 bleibt teiloffen**: `InvestSummeFuer` (Betriebsseiten-Rückfall
   „% der Investition") summiert weiterhin `EingegebenerWert` der Kategorie 1 —
   rein rechnerisch abgeleitete Investbeträge (Zeilen mit Satz, Wert 0) fehlen
   dort. Der Mengen-Ausweis ändert das nicht (er schreibt `Menge`, bewusst nie
   `EingegebenerWert` — der bliebe sonst nicht mehr die VALERI-Vergleichsbasis).
   Heilung wäre ein Umbau von `InvestSummeFuer` auf die abgeleitete
   Kaskadensumme — eigener Punkt, falls praxisrelevant.

## 5. Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | Vorrang-Umkehr an beiden Betriebs-Lesestellen und in `InvestBetrag`; `IstRueckfallErmittelbareArt`/`RueckfallMenge` + Gerätewelt; NEU `MengeAusweisen` |
| `Controller/KostenProjektPositionenCtrl.cs` | `Speichern`: Ausweis-Aufruf nach dem Sichern (+11 Zeilen) |
| `Allgemein/Reporting/H21_MengenAusweis_Protokoll.md` | dieses Protokoll |

Harness `..\dev\h21\` (gitignored) mit den Proben [0]–[3].
