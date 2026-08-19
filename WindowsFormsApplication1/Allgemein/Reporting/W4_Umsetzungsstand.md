# Umsetzungsstand W4 — BHKW-Betriebskosten und -Erlöse

**Stand: 19.08.2026.** Fortschrittsdokument der Ausbaustufe W4. Es hält fest, was
entschieden ist, welche Etappe läuft und welche Ergebniswirkung jede Etappe hat.

| Dokument | Inhalt |
|---|---|
| [`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md) | das Konzept: Leitentscheidungen, Datenmodell, Rechenkette, Etappen |
| [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md) | Rechtsstand mit Quellen — Faktenbasis aller Seed-Werte |
| [`Analyse_Altanwendung_BHKW-Plan.md`](Analyse_Altanwendung_BHKW-Plan.md) | Rechenwege der Excel-Anwendung und ihre 17 Fehler |
| [`UMSETZUNGSSTAND.md`](UMSETZUNGSSTAND.md) | Gesamtstand Bericht und Wirtschaftlichkeit (W1–W3, Phasen 9–11) |
| [`W4_E1_Gesetzesparameter_Protokoll.md`](W4_E1_Gesetzesparameter_Protokoll.md) | Etappe E1: Katalog, Seed, Pflegemaske, Lesefassade |
| [`W4_E2_Vollbenutzungsstunden_Protokoll.md`](W4_E2_Vollbenutzungsstunden_Protokoll.md) | Etappe E2: Vbh-Korrektur, Wirkungsbeleg, Migrationsschritt 18 — **plus Nachtrag 1 „500-kW-Grenze je Anlage" und Nachtrag 2 „Heizöl-Ausschluss je Anlage" (beide 19.08.2026)** |
| [`W4_E3_Kostenarten_Betriebskosten_Protokoll.md`](W4_E3_Kostenarten_Betriebskosten_Protokoll.md) | Etappe E3: Kostenart und Bemessungsart an der Kostenposition (Migrationsschritt 19), negative Beträge für Erlöse, Betriebskosten-Dialog nach VDI 2067, **Zuordnung der VDI-Bezugsgrößen auf das EPOS-Plan-Modell** |
| [`W4_E4_Steuergutschriften_Protokoll.md`](W4_E4_Steuergutschriften_Protokoll.md) | Etappe E4: Energiesteuer- (§ 53 / § 53a) und Stromsteuergutschrift (§ 9 Abs. 1 Nr. 3, § 9b) als jahresscharfe Reihen, Projektangaben der Steuerprüfung (Migrationsschritt 20), benannte Erlösreihen (L1) — **und die Recherche, die die Annahme „§ 53 entlastet nur den Stromanteil" widerlegt hat** |
| [`W4_E5_Tarife_Strombezug_Protokoll.md`](W4_E5_Tarife_Strombezug_Protokoll.md) | Etappe E5: Tarif-Rollenmodell mit drei Leistungspreismodellen (Migrationsschritt 21), Differenzmethode „vermiedene Kosten", Preis für eingespeisten KWK-Strom, § 9b ohne BHKW — **und die Messung, die den Aufschlägen eine Wirkung von rund einem Drittel des Kapitalwerts nachweist** |
| [`W4_E6_Zuschlag_je_Modul_Protokoll.md`](W4_E6_Zuschlag_je_Modul_Protokoll.md) | Etappe E6: KWK-Zuschlag **je BHKW-Modul** — Stichtag, Inbetriebnahme, Satz, Vbh, Jahresdeckel und Kontingent je Anlage (Migrationsschritt 22), Katalogvorschlag nach § 7 als **Tranchenstaffel**, generationsweise Nachsaat des Katalogs — **und der Befund, dass die alte Rechnung bei durchgehend gedeckelten Modulen zufällig richtig war** |

---

## 1 Etappenübersicht

| # | Inhalt | Ergebniswirkung | Status |
|---|---|---|---|
| **E1** | Katalog `Tab_Gesetzesparameter`, Seed, Lesefassade, Pflegemaske; Überführung `Tab_KWKG_Staffel` | **keine** (nur Überführung, wertgleich) | **umgesetzt** (8/8 PASS, 194/194 byte-gleich) |
| **E2** | Vollbenutzungsstunden elektrisch, Vbh je Modul persistieren (Migrationsschritt 18) | **ja — Korrektur** zweier Rechenfehler | **umgesetzt** (8/8 PASS; Wirkung nur bei gepflegtem KWKG-Satz) |
| **E2-N** | *Nachtrag 1:* Ausschreibungsgrenze (500 kW) **je Anlage** statt je Projektsumme; Grenzwert aus dem Katalog | **ja — Korrektur**, wirkt nur bei mehr als einer Anlage | **umgesetzt** (8/8 PASS ×2, 194/194 byte-gleich; Wirkung an präparierten Kopien belegt) |
| **E2-N2** | *Nachtrag 2:* Heizöl-Ausschluss **je Anlage** und über die **installierten Anlagen** statt über die Gerätezeilen; Brennstoffart vorrangig aus `Tab_Energieanlagen.ID_Carrier` | **ja — Korrektur**, wirkt bei mehr als einer Anlage und bei verwaisten Öl-Gerätezeilen | **umgesetzt** (8/8 PASS ×2, 194/194 byte-gleich, 8/8 Wirtschaftlichkeitswerte gleich; Wirkung an präparierten Kopien belegt) |
| **E3** | Kostenposition um Kostenart, Bemessung, Erlös-Kennzeichen, Menge und Einheitpreis erweitern (Migrationsschritt 19); Betriebskosten-Dialog nach VDI 2067 mit elf Positionen in drei Spalten | **keine für Bestandsprojekte** — Schritt 19b belegt jede Bestandszeile mit `BETRAG`, und diese Bemessungsart ist zeilengleich der Rechenweg vor E3 | **umgesetzt** (9/9 PASS, 216/216 byte-gleich, 27/27 Betriebskosten- und Kapitalwertwerte identisch, 47 Harnisch-Proben ohne Fehlschlag) |
| **E4** | Energiesteuer- und Stromsteuergutschrift als jahresscharfe Reihen; sechs Projektangaben der Steuerprüfung (Migrationsschritt 20); `KapitalwertRechner.Rechne` auf **benannte Erlösreihen** umgestellt (L1) | **keine für Bestandsprojekte** — jede Vorbelegung von Schritt 20b ist der Wert, der keine Gutschrift auslöst | **umgesetzt** (9/9 PASS, 216/216 byte-gleich, 54/54 Wirtschaftlichkeitswerte identisch, 11/11 Handrechnungen getroffen) |
| **E5** | Tarif-**Rollenmodell** (Bezug / Reststrom / Einspeisung) mit allen drei Leistungspreismodellen (Migrationsschritt 21); Differenzmethode „vermiedene Kosten" mit negativem Leistungsanteil; Preis für eingespeisten KWK-Strom; § 9b auch ohne BHKW; **Aufschläge hinter einem Projektschalter** | **keine für Bestandsprojekte** — `Tarif_Modus` wird mit `ZONEN` vorbelegt, der Aufschlagsschalter steht auf AUS, die KWK-Vergütung bleibt NULL | **umgesetzt** (9/9 PASS, 216/216 byte-gleich, 27/27 Wirtschaftlichkeitszeilen **zeichengleich**, 8 Wirkungsfälle = Handrechnung) |
| **E6** | KWK-Zuschlag **je Modul**: Stichtag, Inbetriebnahme, Satz, Vbh, Jahresdeckel und Kontingent je Anlage (Migrationsschritt 22); Katalogvorschlag als **Tranchenstaffel** nach § 7; generationsweise Nachsaat des Katalogs | **ja bei Mehrmodulanlagen** — aber nur, wenn die Module den Jahresdeckel **unterschiedlich** treffen oder sich in Datum, Satz oder Kontingent unterscheiden. Für Einmodulprojekte **keine** | **umgesetzt** (9/9 PASS, 216/216 byte-gleich ×2, 24/27 Wirtschaftlichkeitszeilen zeichengleich, 8/8 Handrechnungen getroffen) |
| **E7** | Bericht (Word und Excel), Mehrjahrestabelle | Ausgabe | offen |
| **E8** | Abnahme, neue Referenzbasis, Protokoll | eingefroren | offen |

**Regel für ergebniswirksame Etappen (E2, E6):** A/B-Nachweis gegen den
Vorgängerstand, Wirkungsbeleg mit Zahlen, danach neuer Basis-Freeze — dasselbe
Vorgehen wie bei der Bivalenzumstellung K-3. Für **E6 ist der Freeze nicht nötig**: Die
216 Simulations-CSV sind byte-identisch, und die Wirtschaftlichkeit hat ohnehin keine
eingefrorene Basis. `2026-08-19_B5` bleibt gültig.

---

## 2 Entschiedenes

### 2.1 Nutzerentscheidungen vom 18.08.2026

| Frage | Entscheidung |
|---|---|
| Wartung BHKW: je kWh, je Betriebsstunde oder Prozent der Investition? | **Genau eine Angabe gilt**, sichtbar ausgewählt, die übrigen Felder gesperrt. Kein stilles Überschreiben wie in der Altanwendung. |
| KWK-Zuschlagssatz manuell oder automatisch? | **Vorschlag aus dem Katalog** (Inbetriebnahmedatum, Leistung, Einspeisung oder Eigennutzung), überschreibbar, Herleitung wird angezeigt. |
| Rechnung projektweit oder je Modul? | **Je BHKW-Modul** — erst damit sind die gesetzlichen Leistungsklassen abbildbar. |
| Welches Leistungspreismodell? | **Alle drei**: monatlicher Leistungspreis, vierstufige kW-Staffel, Jahreshöchstlast; je Tarif wählbar. |

### 2.2 Leitentscheidungen mit besonderer Tragweite

- **L11 — Zwei Faktorensätze, strikt getrennt.** Nachweiswerte (Gebäudeenergie-
  beziehungsweise Gebäudemodernisierungsgesetz) und reale Bilanzwerte
  (Strommix des Umweltbundesamts) dürfen nie dieselbe Variable belegen. Der
  Nachweiswert für Netzstrom beträgt ab 2027 100 g CO₂-Äquivalent je kWh, der
  reale lag 2025 bei 406 g mit Vorkette — Faktor 4.
- **L12 — Methodenwechsel zum 01.01.2027.** Der Verdrängungsstrommix (2,8
  beziehungsweise 860 g/kWh) entfällt **ersatzlos**; die Stromgutschrift für
  eingespeisten KWK-Strom wird abgeschafft. Beide Rechenwege müssen parallel
  vorliegen. **Für BHKW-Projekte die folgenreichste Änderung des Vorhabens.**
- **L2 — Ein Katalog statt Konstanten im Code.** Rund 60 bis 80 gesetzliche
  Parameter mit Gültig-ab-Jahr; eine Novelle ist eine neue Zeile, kein
  Überschreiben — sonst sind Altrechnungen nicht reproduzierbar.
- **L3 — Einheitendisziplin.** Sätze immer in der gesetzlichen Einheit
  (€/MWh, €/1.000 l, €/1.000 kg), Umrechnung nur über gepflegte Heizwerte.

### 2.3 Bewusst nicht übernommen

Aus der Altanwendung werden 17 belegte Fehler **nicht** übernommen (Liste in der
Analyse, Abschnitt 5). Die folgenreichsten: Heizöl-Steuerbasis mit falscher
Einheit (Faktor 10), Umsatzsteuer auf den KWK-Zuschlag, netto und brutto
gemischte Prozentbasis, sich still überschreibende Wartungsfelder und drei
widersprüchliche Begrenzungen des KWK-Bonus.

---

## 3 Bekannte Mängel im Bestand, die W4 behebt

| Mangel | Fundstelle | Etappe |
|---|---|---|
| ~~Vollbenutzungsstunden für die KWKG-Deckelung sind die **Summe thermischer** Vbh über alle Module und können 8.760 h überschreiten~~ — **behoben mit E2**; die Richtung war umgekehrt als angenommen: Der Zuschlag fiel bei Kaskaden zu **niedrig** aus (Protokoll, Abschnitt 1.2) | `WirtschaftlichkeitCtrl.cs:848` | **E2, erledigt** |
| ~~`LiesBhkwLeistungKW` summierte **alle Gerätezeilen** statt der installierten Anlagen — Projekt 1024 kam auf 546,4 kW statt 21 kW und verlor den Zuschlag am 500-kW-Guard~~ — **behoben mit E2** | `WirtschaftlichkeitCtrl.cs:991` | **E2, erledigt** |
| ~~Der 500-kW-Guard prüfte die **Projektsumme**; § 8a KWKG stellt auf die einzelne Anlage ab — zwei Module à 300 kW verloren den Zuschlag vollständig~~ — **behoben mit dem E2-Nachtrag** (19.08.2026) | `WirtschaftlichkeitCtrl.cs:943-985` | **E2-N, erledigt** |
| ~~**Heizöl-Ausschluss** prüft `COUNT(*)` über alle **Gerätezeilen** des Projekts: ein einziges Öl-BHKW im Katalogbestand nimmt allen Anlagen den Zuschlag~~ — **behoben mit dem E2-Nachtrag 2** (19.08.2026); die Brennstoffart kommt jetzt vorrangig aus `Tab_Energieanlagen.ID_Carrier`, ersatzweise aus der Gerätezeile | `WirtschaftlichkeitCtrl.cs:988-1076`, `:1455-1570` | **E2-N2, erledigt** |
| ~~**Stichtag und Inbetriebnahme** sind ein Datumspaar je Projekt; § 6 KWKG gilt je Anlage — und dasselbe Datum entscheidet für alle Anlagen zugleich über Neuanlage/Bestandsanlage, also auch über den Heizöl-Ausschluss~~ — **behoben mit E6** (19.08.2026): `Tab_Energieanlagen` führt beide Daten je Anlage (Migrationsschritt 22), NULL fällt auf den Projektwert zurück. Gemessene Wirkung an einer präparierten Kopie: **−83,3 % Zuschlag**, wenn eine von zwei Anlagen am eigenen Stichtag scheitert | `WirtschaftlichkeitCtrl.Anlagenauswahl` | **E6, erledigt** |
| ~~Jahresdeckel und 30.000-h-Kontingent laufen über **eine gemeinsame** leistungsgewichtete Vbh-Größe; der Zuschlagssatz ist einer je Projekt~~ — **behoben mit E6**: eine Reihe je Anlage, jahresweise summiert. Wirkung: **−25,0 %**, wenn die Module den Deckel unterschiedlich treffen; **−0,147 % Kapitalwert** bei unterschiedlichen Kontingenten | `WirtschaftlichkeitCtrl.ReiheJeAnlage` | **E6, erledigt** |
| Energiesteuer- und Stromsteuererstattung fehlen vollständig | — | E4 |
| ~~Vermiedener Strombezug ist keine Erlöszeile; die Bezugsgröße „Bedarf ohne Anlage" wird nirgends geführt~~ — **behoben mit E5**: `StromMatrix.Zone.BedarfMWh` samt Lastbildern (Jahres-, Sommer-, Winter-, zwölf Monatsmaxima); die Differenzmethode weist Arbeit, Leistung und Summe getrennt aus, der Leistungsanteil regelmäßig negativ | `StromMatrix.cs`, `StromTarifRechner.cs` | **E5, erledigt** |
| ~~Ohne Photovoltaik im Projekt bekommt eingespeister BHKW-Strom **keinen Strompreis**, nur den Zuschlag~~ — **behoben mit E5**: eigene Projektangabe `Einspeiseverguetung_KWK`, und die Gruppe „Strom — Einspeisung und Bezug" ist im Parameterdialog **immer** sichtbar | `Form_WirtschaftlichkeitParameter.cs`, `WirtschaftlichkeitCtrl.BaueEingabe` | **E5, erledigt** |
| ~~§ 9b StromStG greift nur bei Projekten **mit BHKW**, obwohl er an keiner KWK-Anlage hängt~~ — **behoben mit E5**; greift nur bei ausdrücklich erfasster Unternehmensart und ist damit ergebnisneutral | `WirtschaftlichkeitCtrl.BaueSteuerEingabe` | **E5, erledigt** |
| ~~Bemessungsarten nach VDI 2067 (Prozent, je Stunde, je kWh) fehlen; Kostenpositionen kennen nur einen Eurobetrag~~ — **behoben mit E3**: `Tab_ProjektWerte` führt `Kostenart`, `Bemessung`, `IstErloes`, `Menge` und `Einheitpreis` (Migrationsschritt 19); `WirtschaftlichkeitCtrl.LiesBetriebskosten` wertet die Bemessung aus | `Tab_ProjektWerte`, `WirtschaftlichkeitCtrl.cs:1851` | **E3, erledigt** |
| ~~Negative Beträge für Erlöse sind nicht eingebbar~~ — **behoben mit E3**: Für Positionen mit `IstErloes` klemmt die Eingabe auf ≤ 0 statt auf ≥ 0, und der Rechenweg erzwingt das negative Vorzeichen | `ucKostenItem.cs:23-66`, `BetriebskostenCtrl.cs:261` | **E3, erledigt** |
| ~~Aufschläge (Netzentgelt, Umlagen, Stromsteuer, Konzession, Vertrieb) erreichen die Jahreskostenrechnung nicht~~ — **mit E5 möglich gemacht, aber NICHT eingeschaltet**: neue Projektangabe `Aufschlaege_Anwenden`, Vorgabe AUS. Gemessene Wirkung **+32 bis 34 % Energiekosten, −30 bis 33 % Kapitalwert** (Protokoll W4_E5, Abschnitt 4) — **die Entscheidung, ob das Vorgabeverhalten werden soll, steht beim Nutzer aus** | `WirtschaftlichkeitCtrl.RechneAufschlaege` | **E5, umgesetzt — Entscheidung offen** |
| Kategorie 3 „Energiekosten" ist pflegbar, wird aber von keiner Rechnung gelesen — Beträge fallen still aus jeder Auswertung | `Form_Kosten.cs` | offen, siehe 5 |

---

## 3a Was Etappe E3 entschieden hat (19.08.2026)

- **Die VDI-Bezugsgrößen sind auf das vorhandene Modell abgebildet, ohne neue
  Investitionsgruppen zu erfinden.** Instandhaltung BHKW und Heizkessel bemessen sich an
  der Investition ihrer **Komponente**; Wärmezentrale, bauliche Anlagen und
  Stromeinspeisung an der **Investitionssumme des Projekts** — sichtbar so benannt. Der
  Freitext `Tab_ProjektWerte.Gruppe` trägt als Bezugsgröße nicht: Sein Bestand
  („test", „Arbeitspreis", „Infrastruktur" …) entsteht frei bei der Eingabe und sieht je
  Projekt anders aus. Vollständige Tabelle im E3-Protokoll, Abschnitt 3.
- **Elf Zeilen statt zwölf.** Die Altmaske führte die Wartung zweimal (je Betriebsstunde
  und je Erzeugung) und ließ die eine die andere überschreiben (Befund 6). L7 verlangt
  genau **eine** Bemessung — aus zwei Zeilen wird eine Zeile mit Auswahlliste.
- **Die Herleitung ist persistent**, nicht abgeleitet: `Menge` und `Einheitpreis` stehen in
  der Zeile. Damit rechnet die Wirtschaftlichkeit ohne zusätzlichen Datenbankzugriff
  dieselbe Zahl wie die Kostenmaske, und ein gespeicherter Betrag bewegt sich nicht hinter
  dem Rücken des Anwenders.
- **Vorzeichenkonvention:** Der gespeicherte Betrag ist immer die Zahlungswirkung in €/a —
  positiv = Ausgabe, negativ = Einnahme. Bei `IstErloes` klemmt die Eingabe auf ≤ 0 und der
  Rechenweg erzwingt das Vorzeichen; ein Erlös kann so nirgends als Kosten in eine Summe
  geraten.
- **Eine Abweichung vom Auftrag**, begründet im Protokoll: `Bemessung` ist `TEXT(30)` statt
  `TEXT(20)` — der längste Steuerwert `PROZENT_BRENNSTOFFKOSTEN` hat 24 Zeichen, und bei
  TEXT(20) scheitert das UPDATE **still**. Zweitens folgt die vorbelegte `Kostenart` der
  Kategorie (1 → kapitalgebunden, 2 → betriebsgebunden, 3 → bedarfsgebunden) statt pauschal
  „kapitalgebunden" zu lauten; die Spalte hat keine Rechenwirkung, und eine pauschale
  Einordnung wäre für jede Wartungsposition sachlich falsch.

---

## 3b Was Etappe E4 entschieden hat (19.08.2026)

- **Die tragende Annahme war falsch, und die Recherche hat sie widerlegt.** Konzept
  (4.2) und Grundlagen (3.2) gingen davon aus, § 53 EnergieStG entlaste „nur den auf die
  Stromerzeugung entfallenden Brennstoffanteil". **Für ein Motor-BHKW gibt es diese
  Aufteilung nicht:** § 53 Abs. 2 Satz 1 stellt darauf ab, ob das Energieerzeugnis
  „unmittelbar am Energieumwandlungsprozess" teilnimmt — beim Motor also der gesamte
  Brennstoff; die Dienstvorschrift Energieerzeugung sagt „Wärme – genutzt oder ungenutzt –
  wird nicht betrachtet". Der „Anteil" des Abs. 1 Satz 2 betrifft die **mechanische**
  Energie an der Welle. Abzugrenzen ist **BHKW gegen Kessel**, nicht Strom gegen Wärme.
  Fundstellen und Belastbarkeit in
  [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md),
  Abschnitt 3.5.
- **Die Aufteilungsmethode bleibt trotzdem eine Projektangabe** — mit dem rechtlich
  belegten Verfahren als Vorgabe (`VOLLER_BRENNSTOFF`) und der energetischen Aufteilung
  als bewusst konservativer Option. Der Unterschied ist Faktor 2,27.
- **Erdgas wird brennwertbezogen bemessen, die Anwendung führt Heizwerte.** Umgerechnet
  wird über die **gepflegten** Werte des Trägers (`eff_hs / eff_hi`, Projektwert vor
  Katalogwert), nicht über den pauschalen Vorschriftenfaktor 1,11; bei Erdgas E ergibt das
  1,1048. Fehlt der Brennwert, rechnet die Anwendung heizwertbezogen weiter und **sagt,
  dass die Entlastung dadurch rund 10 % zu niedrig liegt**.
- **Der CO₂-Grenzwert des § 2 StromStG bezieht sich auf den Energieertrag**, nicht auf den
  Brennstoff: `Faktor × Brennstoff / (Strom + Wärme)`. Erst dadurch scheitert Heizöl
  (303 g/kWh) und Erdgas besteht (229 g/kWh) — die reinen Brennstofffaktoren liegen mit
  266,4 und 200,9 g/kWh **beide unter 270**. Verwendet wird der EBeV-Faktor der Klasse
  `EF_BILANZ`, nicht der Nachweiswert der Anlage 9 (L11).
- **Ohne Stundenreihen keine Stromsteuerbefreiung.** Die Näherung „alles ist
  Eigenverbrauch", die `StromMatrix` für den KWK-Zuschlag macht, trägt eine gegenüber dem
  Hauptzollamt geltend gemachte Befreiung nicht.
- **Die 2-MW-Grenze wird je Anlage geprüft** — Restbefund 3 aus dem E2-Protokoll
  (Nachtrag 1, N7) ist damit erledigt.
- **`energy_carrier.density` ist im gesamten Bestand leer.** Ein je Liter abgerechnetes
  Schweröl lässt sich deshalb nicht in die gesetzliche Einheit €/1.000 kg umrechnen; die
  Gutschrift entfällt **mit einer Begründung, die die Lücke benennt** — statt mit einer
  geratenen Dichte. Das ist derselbe Sachverhalt, an dem die Altanwendung um den Faktor 10
  danebenlag.
- **Ein Formular „1131a" existiert nicht.** Konzept und Grundlagen nannten eine
  „Betriebserklärung 1131a/1131az"; zoll.de führt 1131 und 1131_25, für § 53a das
  Formular 1135. Die Grundlagen sind berichtigt, das Konzept trägt die Angabe noch.

---

## 3c Was Etappe E5 entschieden hat (19.08.2026)

- **Die Aufschläge sind gemessen, bevor sie eingeschaltet wurden — und sie sind NICHT
  eingeschaltet.** Netzentgelt, Umlagen, Stromsteuer, Konzession und Vertrieb erreichten
  die Jahreskostenrechnung nie. Die Erhebung über alle neun Referenzprojekte ergab: vier
  Projekte betroffen, Wirkung **+32 bis 34 % Energiekosten** und **−30 bis 33 %
  Kapitalwert**, an Projekt 1030 **6,39 Mio. €**. Umgesetzt als ausdrückliche
  Projektangabe `Aufschlaege_Anwenden`, **Vorgabe AUS**; der Betrag wird als eigene
  Größe und als Hinweiszeile mit Satz und Zerlegung ausgewiesen. **Ob daraus
  Vorgabeverhalten wird, entscheidet der Nutzer.**
- **Die Aktiv-Flags des Aufschlagsblocks sind kein verlässliches „Aus".** Bei Projekt
  1030 stehen alle fünf Flags auf `False` und alle Werte auf `NULL` — der Leseweg liefert
  trotzdem den vollen Vorschlagssatz von 11,746 ct/kWh, weil `NULL` dort „nicht gepflegt"
  heißt und dann der Vorgabewert samt Vorgabe-Schalter gilt. Die Regel ist im
  Speicherpaket bewusst so gebaut (Access legt `YESNO` überall mit `False` an), überrascht
  hier aber. Deshalb nennt der Hinweis den tatsächlich angesetzten Satz.
- **Zwei Modelle nebeneinander statt eines Umbaus.** Das Zonenmodell der Stufe W3 bleibt
  vollständig erhalten; `Tarif_Modus` (`ZONEN` / `ROLLEN`) entscheidet. Daran hängt die
  Ergebnisneutralität — ein leerer Modus wird wie `ZONEN` behandelt.
- **Die vier Fallen des Altkatalogs sind strukturell vermieden**, nicht bloß im Code
  umgangen: kumulierte Obergrenzen statt Stufenbreiten, geführte vierte Stufe, sichtbare
  Modellauswahl statt „Sommerpreis = 0", Feld `Tarif_GueltigAb` statt Preisstand im
  Beschreibungstext. Dazu eine fünfte stille Regel: kein Vorrang des Monatspreises vor
  der Staffel.
- **Eine Abweichung vom Auftrag**, begründet im Protokoll (Abschnitt 2.2): Die
  **Einspeiserolle bekommt keine Leistungsstaffel**. Im Altkatalog sind ihre
  Sollleistung und Reduktionsfaktoren leer oder 0, es gibt keinen aktiven Lesepfad, und
  der Leistungserlös war fest 0 (Befund 11). 16 Spalten für eine nachweislich tote
  Funktion wären Ballast; nachrüstbar bleibt sie.
- **Vermiedene Kosten sind Ausweis, kein Zahlungsstrom.** Die Einsparung steckt bereits
  in der kleineren Bezugsmenge; sie zusätzlich als Erlös zu buchen wäre eine
  Doppelzählung. In den Kapitalwert geht der **Reststrom**betrag.
- **Der negative Leistungsanteil ist die Kernaussage, nicht ein Sonderfall.** Deshalb
  prüfen alle Sichtbarkeitsbedingungen auf „ungleich 0" statt „größer 0" — eine Zeile,
  die nur bei positiven Werten erschiene, verschwiege genau diese Aussage.
- **Stromsteuer doppelt? Nein — zwei Seiten derselben Vorschrift.** Der Aufschlagsblock
  trägt den Regelsatz (20,50 €/MWh) als Belastung, § 9b die Entlastung (20,00 €/MWh) als
  Gutschrift. **Der Widerspruch liegt im umgekehrten Fall**, und den gibt es im Bestand:
  Schalter AUS und § 9b aktiv heißt Entlastung ohne Belastung. Das Ergebnis meldet es im
  Klartext, statt es stillschweigend zu korrigieren — eine Kopplung hätte E4 nachträglich
  verändert.

---

## 3d Was Etappe E6 entschieden hat (19.08.2026)

- **„Leistungsanteil" in § 7 KWKG heißt Staffel, nicht Klasse — und das ist keine Feinheit.**
  Abs. 1 und 2 meinen **marginale Tranchen**: Eine 300-kW-Anlage bekommt 50 kW zu 8,00, 50 kW
  zu 6,00, 150 kW zu 5,00 und 50 kW zu 4,40 ct/kWh, leistungsgewichtet **5,5667 ct/kWh**. Die
  naheliegende Umsetzung „Klasse suchen, Satz anwenden" hätte 4,40 ct/kWh geliefert und damit
  **21 % zu wenig**. Die angezeigte Herleitung nennt deshalb die Tranchen, nicht eine Klasse.
- **Einspeisung und Eigennutzung sind nicht symmetrisch.** Auf eingespeisten Strom besteht der
  Zuschlag ohne weitere Voraussetzung; auf selbst genutzten **nicht generell**, sondern nur in
  den drei Tatbeständen des § 6 Abs. 3 mit drei verschiedenen Satzreihen. Über allem steht
  § 7 Abs. 3a für neue Anlagen bis 50 kW (16 / 8 ct/kWh). Der Vorgabewert ist **kein
  Tatbestand** — dann schlägt der Katalog 0 ct/kWh vor **und sagt warum**. Das ist keine
  Lücke, sondern die Rechtslage.
- **Der Vorschlag ersetzt den Projektsatz nicht von selbst.** Er erscheint mit seiner
  Herleitung im Dialog; erst „Vorschlag übernehmen" schreibt ihn in die Satzfelder der
  Anlage. Ein Vorschlag, der ungefragt gilt, ist kein Vorschlag — und hätte jede gespeicherte
  Altrechnung mit gepflegtem KWKG-Satz still auf einen anderen Satz umgestellt.
- **NULL ist die Vorbelegung.** Migrationsschritt 22 ist der erste der Reihe **ohne DML**:
  Alle acht neuen Spalten bleiben leer, und jede Leseseite fällt bei NULL auf den Projektwert
  zurück. Nachgewiesen an 97 Anlagenzeilen × 8 Spalten = 0 belegte Werte.
- **Die alte Rechnung war bei durchgehend gedeckelten Modulen nicht falsch, sondern zufällig
  richtig.** Solange **jedes** Modul über dem Jahresdeckel liegt, ist die Summe der
  Modulreihen algebraisch die Projektreihe. Deshalb ändert sich am Referenzprojekt 1030 nur
  **+0,09 €/a** — Gleitkommarest, kein Rechenweg. Die Wirkung entsteht erst, wenn die Module
  den Deckel **unterschiedlich** treffen (−25,0 %), verschiedene Inbetriebnahmejahre haben
  (−9,7 %), verschiedene Kontingente (−0,147 % Kapitalwert über 20 Jahre) oder wenn eine von
  ihnen am eigenen Stichtag ausfällt (−83,3 %). **Projekt 1030 belegt die Etappe damit
  nicht** — die Referenzmenge deckt den Fall nicht ab.
- **Der projektweite Rechenweg bleibt vollständig erhalten** als Ersatzweg für Projekte, deren
  Anlagen- und Ergebnismodulzeilen sich nicht paaren lassen (im Bestand: Projekt 1023). Ihn zu
  entfernen hätte einen zweiten Rechenweg geändert, den keine Probe abdeckt.
- **Die § 6-Prüfung bleibt projektweit, solange keine Anlage ein eigenes Datum trägt.** Die
  Prüfung je Anlage wäre rechnerisch identisch, aber nicht **textgleich** — sie würde die
  Anlage benennen und auf jedem Bestandsprojekt eine neue Meldung erzeugen.
- **Offener Punkt 4 ist entschieden: generationsweise Nachsaat über eine Markerzeile.** Siehe
  Abschnitt 5, Punkt 4.

---

## 4 Etappe E1 — was gerade entsteht

- **Tabelle `Tab_Gesetzesparameter`**: Schlüssel (sprachneutral, eingefroren),
  Klasse, Gültig ab Jahr, Wert, Einheit, Status, Quelle. Angelegt über das
  bewährte „Katalog sicherstellen"-Muster, damit Bestandsinstallationen die Werte
  ohne Migrationsschritt erhalten.
- **Klassen** trennen fachlich: KWKG, Stromsteuer, Energiesteuer, CO₂-Preis,
  Emissionsfaktoren **Nachweis** gegen **Bilanz**, Primärenergiefaktoren,
  Umsatzsteuer. Die Trennung Nachweis/Bilanz setzt L11 strukturell um.
- **Beide Faktorensätze werden gleichzeitig eingepflegt** — die alten mit
  Gültigkeit bis 2026, die neuen ab 2027. Der Stichtagswechsel steckt damit
  bereits im Datenbestand, bevor die Rechenlogik ihn nutzt.
- **Lesefassade** mit Stichtagsauflösung („jüngste Zeile mit Gültig-ab ≤ Jahr"),
  Herkunftsanzeige für Masken und Bericht und einer Code-Rückfallebene. **Fehlt
  ein Wert, liefert sie „nicht gepflegt" statt null** — dieselbe Lehre wie beim
  Arbeitspreis 0, der einen scheinbar gültigen Kapitalwert erzeugte.
- **Pflegemaske** im Administrationsmenü mit sichtbarer Regel „neue Jahreszeile
  statt Ändern"; beim Bearbeiten einer vergangenen Zeile erscheint eine Rückfrage.
- **Überführung** der acht Zeilen aus `Tab_KWKG_Staffel`; die Alt-Tabelle bleibt
  stehen, wird aber nicht mehr gelesen. Wertgleichheit Jahr für Jahr nachgewiesen.

---

## 5 Offene Punkte

**Fachlich zu entscheiden:**

1. **§ 53 neben § 53a EnergieStG** (Strom- und Wärmeanteil): rechtlich ungeklärt,
   deshalb als einstellbare Option modelliert. Vor produktivem Einsatz mit dem
   Hauptzollamt klären.
2. **Kategorie 3 „Energiekosten"**: entfernen oder als Override mit sichtbarem
   Vorrang definieren.
3. **Gutschrift für eingespeisten KWK-Strom ab 2027**: Ohne amtlichen
   Verdrängungsfaktor ist jede Gutschrift eine methodische Wahl. Vorgesehen als
   Auswahlparameter mit Ausweis im Bericht.
4. ~~**Nachsaat fehlender Katalogschlüssel**~~ — **mit E6 entschieden und umgesetzt
   (19.08.2026): generationsweise Nachsaat.** Jede Zeile der `GesetzKatalog.Vorbelegung`
   trägt im **Code** eine Generationsnummer; eine **Markerzeile** in
   `Tab_Gesetzesparameter` (`Schluessel = KATALOG_GENERATION`, `Klasse = SYSTEM`) hält fest,
   bis zu welcher Generation diese Datenbank gesät wurde. Beim Start werden nur Zeilen einer
   **höheren** Generation nachgesät. Damit kommen neue Schlüssel an, und eine bewusst
   gelöschte Zeile einer älteren Generation bleibt gelöscht — der Zielkonflikt ist aufgelöst.
   Generationen: **1** = E1-Seed (182 Zeilen), **2** = `KWKG_AUSSCHREIBUNG_GRENZE_KW` aus dem
   E2-Nachtrag, **3** = die beiden Anlagengrenzen aus E6.

   *Markerzeile statt Spalte*, begründet: Eine Spalte bräuchte DDL und wirkte damit nicht auf
   Datenbanken, deren Tabelle vom E1-`CREATE TABLE` mit fester Spaltenliste angelegt wurde —
   genau die, die die Nachsaat braucht. Und die Generation ist eine Eigenschaft des **Seeds**,
   nicht der Zeile: `MAX(Generation)` über die Zeilen wäre eine falsche Wahrheit, weil das
   Löschen aller Zeilen der jüngsten Generation sie zurückholte.

   *Der Fall war nicht theoretisch:* In der produktiven `Kenndaten.accdb` vom 19.08.2026
   fehlte `KWKG_AUSSCHREIBUNG_GRENZE_KW` tatsächlich (49 KWKG-Zeilen, dieser Schlüssel nicht
   darunter). Nachgewiesen: 182 → 186 Zeilen beim ersten Start, 0 beim zweiten, und eine
   gelöschte Generation-1-Zeile kommt **nicht** zurück (E6-Protokoll, V14 bis V17).
5. **Preissteigerung der Hilfsenergie** (aus E3, 19.08.2026): Sie ist nach VDI 2067
   *bedarfs*gebunden und müsste der Energiepreisentwicklung folgen; als Kategorie-2-Position
   steigt sie bei uns mit der Betriebskosten-Preissteigerung. Eine Trennung braucht eine
   zweite Kostenreihe in `KapitalwertRechner.Rechne`. Solange beide Sätze gleich gepflegt
   sind, ist der Unterschied null.
6. **Feinere Bezugsgrößen für Wärmezentrale, bauliche Anlagen und Stromeinspeisung** (aus
   E3): Der saubere Weg wäre ein Kennzeichen an der Kostengruppe („diese Gruppe ist eine
   Investitionsgruppe im Sinn der VDI 2067") — eine Spalte an `Tab_KostenGruppenKatalog`
   und eine Pflegemaske. Eigene Entscheidung, keine Nebenwirkung von E3.

**Recherchelücken** (im Grundlagendokument als solche markiert, nicht geraten):
Auslösedauer des CO₂-Preisstabilitätsmechanismus, Wortlaut von § 10 Abs. 3 BEHG,
Zahlenreihe des Projektionsberichts 2026, Enddatum der Versteigerungsphase 2026,
Erdgassatz nach § 53a Abs. 3 EnergieStG.

**Datenbestand:** `DB-TARIF.XLS` und `DB-Kraftwerk.XLS` liefern nur die Struktur;
ihre Werte sind veraltet (Preisstand teils 1996, Emissionsdaten bis 2020) und
werden nicht übernommen.

---

## 6 Doppelte Wahrheiten, die W4 auflösen soll

- **BHKW-Einspeisevergütung an vier Orten** (E5 hat einen hinzugefügt, um den
  Bestandsmangel zu beheben): `energy_project_settings.Verguetung_BHKW` (wirkt nur in der
  Speichersimulation), `Tab_ProjektTarif.Einsp_*` (Zonenmodell, HT/NT),
  `Tab_ProjektTarif.Einsp_Arbeit` (Rollenmodell, E5),
  `WirtschaftlichkeitParameter.Einspeiseverguetung_KWK` (Flat-Pfad, E5). Die Vorrangregel
  ist eindeutig — der aktive Tarif schlägt die Parameterwerte —, aber vier Felder für
  einen Preis sind drei zu viel.
- **Vorrangregel Projekt vor Katalog** in drei Implementierungen
  (`KostenEmissionRechner`, `StromPreisCtrl`, eine gespeicherte Access-Abfrage).
- **14 Kennzahlzeilen doppelt** in Word- und Excel-Generator.
- **Zwei Migrationsmechanismen**: `SchemaMigration` mit Versionsmarker einerseits,
  eigenes DDL in `WirtschaftlichkeitCtrl` andererseits. **Mit E4 laufen für
  `Tab_ProjektWirtschaftlichkeit` erstmals beide** — Migrationsschritt 20 legt die sechs
  Spalten an und belegt sie vor, `StelleTabellenSicher` legt dieselben Spalten
  vorsorglich an (ohne Werte). Das ist dasselbe Doppel wie bei
  `Tab_ProjektWerte`/`KostenPositionCtrl` aus E3 und bewusst so gebaut; aufzulösen ist es
  trotzdem. **Mit E5 gilt dasselbe für `Tab_ProjektTarif`** (Schritt 21 gegen
  `StelleTabellenSicher`); dort greift der Migrationsschritt zusätzlich ins Leere, wenn
  die Tabelle noch gar nicht existiert — er meldet das und gilt als erledigt, statt die
  Migration dauerhaft auf Stand 20 festzuhalten. **Mit E6 trifft es erstmals eine Tabelle des
  Rechenkerns**: Schritt 22 legt acht Spalten an `Tab_Energieanlagen` an, und
  `WirtschaftlichkeitCtrl.StelleTabellenSicher` legt dieselben acht vorsorglich an. Der
  Unterschied zu E3 bis E5 ist der **Leser**, nicht die Tabelle — der Rechenkern liest keine
  dieser Spalten, deshalb stehen sie bewusst **nicht** in `SchemaKatalog.Alle` und werden von
  der stillen Rückfallebene bei jedem Simulationsstart **nicht** mitgezogen.
- **Komponenten-IDs an zwei Orten** (benannt mit E3): `Form_Kosten.GetKomponentenID`
  verdrahtet 1…7 hart, `UcBkKosten` und `KomponentenUebernahmeCtrl` lesen dieselbe
  Zuordnung dynamisch aus `Tab_KostenKomponente`. `BetriebskostenCtrl` musste sich für
  einen Weg entscheiden und führt die beiden gebrauchten IDs als benannte Konstanten.
- **Zwei Lesewege auf die Kostenposition** (entstanden mit E3): `Abfrage_Kostenfaktoren`
  liegt als gespeicherte Access-Abfrage außerhalb des Repos und kennt die fünf neuen
  Spalten nicht; `Form_Kosten.LoadKostenFaktoren` holt sie deshalb über einen zweiten,
  direkten Zugriff auf `Tab_ProjektWerte` und führt sie über die ID zusammen.
