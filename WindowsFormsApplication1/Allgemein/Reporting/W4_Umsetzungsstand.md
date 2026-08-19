# Umsetzungsstand W4 — BHKW-Betriebskosten und -Erlöse

**Stand: 19.08.2026 — die Ausbaustufe ist mit Etappe E8 abgenommen und abgeschlossen.**
Fortschrittsdokument der Ausbaustufe W4. Es hält fest, was entschieden ist, welche Etappe was
bewirkt hat und was offen bleibt.

> **Abnahmeurteil (E8):** abgenommen, **unter vier Vorbehalten** — die Zahlenprobe gegen die
> Altanwendung wurde nie gerechnet, L12 (Methodenwechsel 2027) liegt nur als Katalogdatenseite ohne
> Leser vor, L13 (Biomasse-Konvention) ist gar nicht umgesetzt, und **keine der neuen Rechenklassen
> hat einen dauerhaften Test**. Einzelheiten in
> [`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md).
>
> **Zwei dieser vier Vorbehalte sind am 19.08.2026 erledigt:** **L12 und L13 sind umgesetzt**
> (Migrationsschritt 23, Katalog-Generation 4, drei Rechenwege der Emissionsbilanz, zwei
> Biomasse-Angaben, Ausweis in Reiter, Word und Excel) — ergebnisneutral für Bestandsprojekte,
> 216/216 byte-gleich gegen B6 und 972/972 Wirtschaftlichkeitswerte identisch gegen `3307378`.
> Belege in [`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md).
> **Offen bleiben** die Zahlenprobe gegen die Altanwendung (A8) und die fehlenden dauerhaften
> Tests (A1).

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
| [`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md) | Etappe E8: **Abnahme gegen das Konzept**, Schließung der vier Prüflücken mit Zahlen, neue Referenzbasis **B6**, ehrliche Restliste — **und die acht Befunde A1 bis A8**, darunter die nie gerechnete Zahlenprobe gegen die Altanwendung |
| [`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md) | Nacharbeit zu **A3 und A4**: Methodenwechsel zum 01.01.2027 (drei Rechenwege, umgeschaltet über das Gültig-ab-Datum des Katalogs) und Bilanzierungskonvention für Biomasse samt Nachhaltigkeitsnachweis (Migrationsschritt 23) — **und der Nachweis, dass die stille Konvention nicht im Code stand, sondern in den Katalogwerten des Brennstoffs** |
| [`W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md`](W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md) | Etappe E7: Rückgabekanal der Einzelpositionen im `Zahlungsbild`, **Mehrjahrestabelle** in Word und Excel, KWK-Zuschlag **je Modul als Tabelle**, Betriebskosten nach **Kostenart**, eine Zeilendefinition statt dreier, sechs Divergenzen Word/Excel — **und die erste Messung, die die Wertgleichheit von Word und Excel belegt statt sie zu behaupten** |

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
| **E7** | Bericht (Word und Excel), Mehrjahrestabelle; Rückgabekanal der Einzelpositionen, KWK-Zuschlag je Modul als Tabelle, Betriebskosten nach Kostenart, eine Zeilendefinition für alle drei Ausgaben | **keine** — Ausgabe; der Rückgabekanal ist rein additiv und der Rechenweg der Summen zeichengleich | **umgesetzt** (216/216 byte-gleich, **864/864 Wirtschaftlichkeitswerte identisch**, Word ↔ Excel erstmals **gemessen** zeichengleich) |
| **E8** | Abnahme gegen das Konzept, Schließung der vier Prüflücken, neue Referenzbasis **B6** | **keine** — keine Codezeile geändert | **umgesetzt** (216/216 byte-gleich gegen B5, 40/40 Harnisch-Proben ohne Fehlschlag, 0 unerwartete Dialoge; 8 Befunde dokumentiert) |
| **L12/L13** | Methodenwechsel 01.01.2027 mit drei Rechenwegen; Bilanzierungskonvention Biomasse und Nachhaltigkeitsnachweis (Migrationsschritt 23, Katalog-Generation 4) | **keine für Bestandsprojekte** — `Bilanz_Jahr` bleibt NULL (⇒ Rechtsstand bis 31.12.2026 ⇒ Stromgutschrift wie bisher), `NULLANSATZ` und `NACHWEIS_JA` sind die Annahmen, die der Bestand still traf | **umgesetzt** (9/9 PASS, 216/216 byte-gleich gegen B6, **972/972 Wirtschaftlichkeitswerte identisch**, 8/8 Rundproben, 5/5 Handrechnungen, Ausweis in Word und Excel gemessen) |

**Regel für ergebniswirksame Etappen (E2, E6):** A/B-Nachweis gegen den
Vorgängerstand, Wirkungsbeleg mit Zahlen, danach neuer Basis-Freeze — dasselbe
Vorgehen wie bei der Bivalenzumstellung K-3. Für **E6 war der Freeze nicht nötig**: Die
216 Simulations-CSV sind byte-identisch, und die Wirtschaftlichkeit hat ohnehin keine
eingefrorene Basis.

**Die gültige Referenzbasis ist seit E8 [`2026-08-19_B6`](../../../Referenzlaeufe/2026-08-19_B6/lauf_protokoll.md)**
— byte-identisch mit B5, gewechselt allein wegen der Zuordnung (Codestand nach E7, Quelle auf
Schemastand 21). **Was sie nicht absichert, steht in ihrem Laufprotokoll** und ist der Kern der
Restliste: Die Wirtschaftlichkeitsrechnung ist in **keiner** eingefrorenen Basis enthalten.

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
  — *Umgesetzt am 19.08.2026: drei Rechenwege, umgeschaltet über die
  2027er-Katalogzeile ohne Wert. Gemessen **−70,0 % ausgewiesene CO₂-Vermeidung**
  am Zweimodul-BHKW; der Kapitalwert bleibt unberührt.*
- **L13 — Bilanzierungskonvention für Biomasse.** Ob biogenes Verbrennungs-CO₂
  mit null angesetzt wird, widerspricht sich zwischen BEHG, GModG,
  UBA-Emissionsbilanz und UBA-CO₂-Rechner (dort 365 g/kWh); der Nullansatz des
  BEHG setzt zusätzlich einen Nachhaltigkeitsnachweis voraus.
  — *Umgesetzt am 19.08.2026 als zwei getrennte Einstellungen mit Ausweis. Die
  stille Annahme stand im Brennstoffkatalog, nicht im Code, und ist die Vorgabe
  geworden; die Alternative **dreht das Vorzeichen** der ausgewiesenen Vermeidung.*
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
| ~~Energiesteuer- und Stromsteuererstattung fehlen vollständig~~ — **behoben mit E4**; mit E8 an der vollen Kette gemessen: § 53a **21.598,65 €/a**, § 9 Abs. 1 Nr. 3 **28.564,62 €/a**, § 9b **61.150,17 €/a** an Projekt 1030 bei gepflegten Angaben, im Vorgabezustand durchgehend 0 € | `SteuerGutschriftRechner.cs` | **E4, erledigt** |
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

## 3e Was Etappe E7 entschieden hat (19.08.2026)

- **Der Engpass war ein fehlender Kanal, kein fehlender Formatierer.** Vier benannte
  Erlösreihen entstanden seit E4 jahresscharf, gingen in eine Summe ein und wurden verworfen;
  vom KWK-Zuschlag überlebte allein der Wert des ersten Jahres. `Zahlungsbild` gibt seit E7 die
  Jahresreihen der Einzelpositionen zurück. **Rein additiv:** Der Ausdruck für die Ausgaben
  behält insbesondere `(energieJahr + behgJahr)` als **eine** Klammer, und die getrennten
  Reihen gehen nicht in die Summe ein — sonst verschöbe sich das Ergebnis in der letzten
  Stelle. Gemessen: 864 von 864 Werten unverändert.
- **Das Auslaufen des KWK-Zuschlags ist jetzt sichtbar, und das ist der fachliche Zweck der
  Tabelle.** An Projekt 1030 fällt der Zuschlag von 44.265 € über die degressive Vbh-Staffel
  auf 18.563 € im Jahr 12 und **ab Jahr 13 auf null**, weil das 30.000-Stunden-Kontingent
  erschöpft ist. Im bisherigen „KWKG-Erlös Jahr 1" war davon nichts zu sehen — ein Leser
  musste annehmen, der Zuschlag laufe zwanzig Jahre.
- **Jahre als Zeilen, Positionen als Spalten** — bei T = 20 passen 21 Jahresspalten nicht auf
  A4, und der Kapitalwert-Verlauf im Excel-Bericht macht es seit Phase 11 bereits so. Damit
  brauchen Word und Excel **kein zweites Layout**. Vorzeichen nach Zahlungswirkung (Ausgaben
  negativ), damit die Summe der Positionsspalten die Nettospalte ist und die Tabelle sich
  selbst prüft; die Abschlusszeile schließt mit dem Restwert-Barwert auf den Nettobarwert auf.
- **Vermiedene Kosten und Aufschlagsbetrag bekommen keine Zahlungszeile.** Beide stecken
  bereits in anderen Positionen; sie stehen als ausdrücklich beschrifteter **Nachweisblock**
  unter der Tabelle, und ihre Titel tragen seither „(Ausweis)" beziehungsweise „(in
  Energiekosten enthalten)". Das ist keine Kosmetik — ohne den Zusatz liest ein Prüfer die
  Zeilen als addierbare Erlöse.
- **Kein „Jahr 1" mehr im Zeilentitel.** Der Zeitbezug steht einmal über der Tabelle. Erst
  dadurch passt derselbe Schlüssel in Kennzahlen- **und** Mehrjahrestabelle, und aus zwei Namen
  für eine Größe („KWKG-Erlös Jahr 1" gegen die Reihe „KWK-Zuschlag") wird einer.
- **Eine Zeilendefinition, drei Renderer.** Die Kennzahlenliste stand dreimal im Code. Die
  Zahlen liefen dabei nicht auseinander — das Drumherum schon. Von den sechs belegten
  Divergenzen zwischen Word und Excel sind **vier behoben** (Tarifnachweis, Hinweise,
  Aktualitätswarnung, Laufwarnungen), **eine auf einen begründeten Fall reduziert** (Excel
  lässt die Stammzelle leer, weil die Wertspalten numerisch bleiben müssen) und **eine bewusst
  belassen** (Word zeigt nur „Erwartet" — ein Dokument ist keine Datenablage).
- **Die Wertgleichheit von Word und Excel ist erstmals gemessen**, nicht behauptet: Jede Zeile
  mit einer Zahl ist in beiden Ausgaben zeichengleich; die Nur-in-Word-Zeilen tragen
  ausnahmslos „—" oder „(Referenz)".
- **Die Kostenart aus E3 hat ihren Zweck bekommen.** Der Betriebskostenblock gliedert nach
  VDI 2067 und zeigt je Position Bemessungsart und Herleitung Menge × Einheitpreis. Er
  **rechnet nicht mit**, sondern beschreibt — und meldet, wenn seine Summe von der angesetzten
  abweicht, statt zwei Zahlen nebeneinanderzustellen und zu schweigen.
- **Die Herleitung des KWK-Satzes je Modul steht im Bericht.** Sie macht an Projekt 1030
  sichtbar, dass der Katalog für die 250-kW-Anlage auf Eigenstrom **0 ct/kWh** vorschlägt
  (kein Tatbestand des § 6 Abs. 3), während der Projektsatz von 4,00 ct/kWh angesetzt wird —
  eine Abweichung, die vorher nirgends stand.
- **Ein Bestandsfehler ist benannt, nicht nebenbei behoben:** Die Meldung „Differenzdiagramm
  entfällt — für das Stammprojekt konnte keine Zahlungsreihe gerechnet werden" erscheint auch
  dann, wenn es schlicht **keine Varianten** gibt. Der Befund stammt aus Phase 11; die
  Berichtigung gehört in einen eigenen Vorgang.

---

## 3f Was Etappe E8 entschieden hat (19.08.2026)

- **Die Ausbaustufe ist abgenommen — aber nicht ohne Vorbehalt, und der wichtigste ist eine
  Lücke im Nachweis, nicht im Code.** Konzept Abschnitt 8 verlangt als ersten Verifikationsschritt
  die **Zahlenprobe gegen die Altanwendung** (das Beispiel des Erlös-Screenshots: vermiedene Kosten
  3.657 / −341 / 3.316 €, Einspeiseerlös 1.028 €, Zuschlag 5.488 und 3.059 €). **Keine der sieben
  Etappen hat sie gerechnet**, und keine begründet den Verzicht — die Suche nach jeder der sechs
  Zahlen über alle sieben Protokolle liefert null Treffer. Damit prüft jede Handrechnung der
  Ausbaustufe ihre Formel gegen die eigene Herleitung, aber keine prüft die Kette gegen das Vorbild,
  das sie ablöst. Eine systematisch falsche Gesetzesauslegung könnte in allen Proben gleichzeitig
  „stimmen". Das ist der schwerwiegendste Abnahmebefund (A8).
- **Die vier Prüflücken sind geschlossen — und die wichtigste hat den Beleg geliefert, den die
  Referenzmenge nicht liefern kann.** Der Wirkungsfall der Etappe E6 ist erstmals gemessen: Treffen
  die Module den Jahresdeckel **unterschiedlich**, weicht die modulscharfe Rechnung um
  **−25,04 %** vom projektweiten Weg ab (26.199,93 statt 34.951,20 €/a) — und über die **gepflegte**
  E6-Spalte `KWKG_Vbh_Jahresdeckel`, auf frisch simuliertem Ergebnis, um **+61,43 %** (71.456,97
  statt 44.265,22 €/a). Bis dahin war E6 nur an präparierten Ergebniszeilen belegt.
- **Die volle E4/E5-Kette reproduziert die Handrechnungen des E4-Protokolls auf den Cent** —
  21.598,65 € Energiesteuer, 28.564,62 € Stromsteuerbefreiung, 61.150,17 € Entlastung. Die
  Etappenzahlen sind damit auf dem heutigen Stand **nachvollzogen**, nicht nur zitiert. Im
  Vorgabezustand bleiben alle drei bei 0 € — die Ergebnisneutralität ist erneut gemessen.
- **Der Ergebnisreiter hält, was E7 zugesagt hat, und zeigt zugleich einen Anzeigefehler.**
  39 von 39 Werten sind zeichengleich zur zentralen Zeilendefinition. Dabei ist aufgefallen, dass
  `UcWirtschaftlichkeit` **zwei** Zeilen mit demselben Titel „Hinweis" beschriftet — eine für den
  Hinweis, eine für den Fehlgrund. Im Regelfall einer Vergleichsgruppe stehen beide untereinander
  und sind nicht unterscheidbar.
- **Drei der fünf neuen Masken sind nicht lokalisiert, und niemand hat es aufgeschrieben.**
  `Form_WirtschaftlichkeitParameter` (23 deutsche Literale), `Form_Tarifstruktur` (30) und
  `Form_KwkgModule` (10) greifen **kein einziges Mal** auf `MyResource` zu, obwohl Konzept
  Abschnitt 5 „ausschließlich über `MyResource`" verlangt. Die 9 beziehungsweise 81
  Ressourcenschlüssel aus E6 und E7 bedienen den **Bericht**, nicht die Dialoge. `Form_Betriebskosten`
  (41 Zugriffe, 0 Literale) und `Form_Gesetzesparameter` (48/0) zeigen, dass es auch anders geht.
- **Zwei Konzeptzeilen sind nie gebaut worden und waren in keiner Offene-Punkte-Liste geführt:**
  `Tab_BHKW/_STAMM.Wartungsbemessung` (sachlich entbehrlich — die Bemessung sitzt seit E3 an der
  Kostenposition, das ist der bessere Ort; das **Konzept** ist berichtigt) und
  `Tab_Kraftwerkspark.Bezugsbasis` samt vier weiteren Spalten (**nicht** entbehrlich — der
  Definitionsbruch des Altkatalogs, Faktoren je kWh Brennstoff und je kWh Strom in derselben Spalte,
  besteht fort). Beides ist ab jetzt in Abschnitt 5 geführt.
- **L12 und L13 sind die eigentliche Restarbeit der Ausbaustufe.** L12 („Methodenwechsel zum
  01.01.2027", vom Konzept selbst als „für BHKW-Projekte die folgenreichste Änderung des gesamten
  Vorhabens" bezeichnet) existiert ausschließlich als **Datenseite**: Die Katalogzeilen für 2027
  stehen mit Wert `null` bereit, aber **keine Codezeile liest sie**, es gibt keinen zweiten
  Rechenweg, keinen Auswahlparameter und keinen Berichtsausweis. L13 ist gar nicht umgesetzt und war
  bis heute nicht einmal als offener Punkt geführt.

  > **Erledigt am 19.08.2026** ([`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md)).
  > Die Einschätzung der Abnahme war richtig — und der Befund noch etwas schärfer, als er hier
  > steht: Die Stromgutschriftmethode war **kein fehlender Parameter**, sondern die Systemgrenze
  > des `EmissionsBilanzRechner` selbst, und die Biomasse-Konvention stand nicht im Code, sondern
  > in den Katalogwerten von `Tab_Brennstoff_Stamm`. Beides ist jetzt Einstellung mit Ausweis.
- **Keine der neuen Rechenklassen hat einen Test.** L9 verlangt „Tests im vorhandenen Testprojekt";
  `SteuerGutschriftRechner`, `StromTarifRechner`, `KwkgSatzRechner` und `KapitalwertRechner` kommen
  in `SpeicherEngine.Tests` und `KiKern.Tests` **nicht vor**. Jede Messung dieser Ausbaustufe —
  einschließlich der 40 Proben dieser Etappe — ist ein Einzelnachweis aus einem Wegwerf-Harnisch,
  der beim nächsten Build nicht mitläuft. Ohne Rechenwirkung, aber es ist der Grund, warum die
  Referenzbasis über die Wirtschaftlichkeit nichts aussagt.
- **E8 hat keine Codezeile geändert.** Alles Gefundene ist als Befund ausgewiesen. Auch der kleine
  Anzeigefehler B1 ist bewusst nicht behoben: Sein sauberer Weg braucht einen Ressourcenschlüssel in
  beiden `.resx` **und** im Designer, und ein zweites deutsches Literal hätte den Lokalisierungsrest
  vertieft, statt ihn abzubauen.
- **Die neue Basis B6 ist byte-identisch mit B5 — und gerade das ist der Nachweis.** Zwischen beiden
  Ständen hat die Sitzung des Anwenders die produktive Datenbank von Schemastand 17 auf 21 gezogen
  und komprimiert (96 436 224 → 92 700 672 Byte, MD5 `66F4806A…` → `0873B892…`). Dass 216 von 216
  CSV trotzdem gleich sind, belegt die Ergebnisneutralität der Migrationsschritte 18 bis 21 stärker,
  als ein Lauf auf unveränderter Quelle es gekonnt hätte.

---

## 4 Etappe E1 — was mit ihr entstanden ist

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

**Stand nach der Abnahme E8.** Die Punkte 1 bis 6 sind die Liste aus E1 bis E7, einzeln auf den
heutigen Stand gebracht; 7 bis 11 sind mit der Abnahme neu hinzugekommen.

**Fachlich zu entscheiden:**

1. **§ 53 neben § 53a EnergieStG** (Strom- und Wärmeanteil): rechtlich ungeklärt,
   deshalb als einstellbare Option modelliert. Vor produktivem Einsatz mit dem
   Hauptzollamt klären. — **Unverändert offen.** E8 hat die Option in der vollen Kette gemessen
   (§ 53a Abs. 5 auf Projekt 1030: **21.598,65 €/a**); die Rechtsfrage berührt das nicht. Dazu
   gehört weiterhin, dass die **Dienstvorschrift Energieerzeugung** nur in der Fassung von 2014
   vorlag (E4, offener Punkt 10).
2. **Kategorie 3 „Energiekosten"**: entfernen oder als Override mit sichtbarem
   Vorrang definieren. — **Unverändert offen.** W4 hat die Kategorie nicht angefasst; erfasste
   Beträge fallen weiterhin still aus jeder Auswertung.
3. ~~**Gutschrift für eingespeisten KWK-Strom ab 2027**~~ (= die Rechenseite von **L12**) —
   **am 19.08.2026 umgesetzt** ([`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md)).
   Drei Rechenwege (`STROMGUTSCHRIFT`, `OHNE_GUTSCHRIFT`, `SUBSTITUTION`), umgeschaltet über
   **dasselbe Gültig-ab-Datum aus dem Katalog** — die 2027er-Zeile ohne Wert, die seit E1 niemand
   las. Gemessen: **−963,24 t CO₂/a ausgewiesene Vermeidung (−70,0 %)** an Projekt 1030, sobald
   das Bilanzjahr auf 2027 steht.

   > **Was mit dem Punkt neu geworden ist — und am 19.08.2026 vom Anwender entschieden wurde:** Der
   > Stichtag hängt an der **eigenen Projektangabe `Bilanz_Jahr`** mit festem Rückfall auf **2026**,
   > nicht am Förderjahr und nicht an der Systemuhr. Grund: `Foerderbeginn` fällt ohne
   > Inbetriebnahme auf „aktuelles Jahr + 1" (heute 2027) und hätte jedes Bestandsprojekt sofort
   > umgestellt; die Systemuhr bräche die Reproduzierbarkeit, die Grundlagen 7.1 ausdrücklich
   > verlangt („ein 2026 gerechneter Variantenvergleich muss 2029 dieselben Zahlen liefern").
   > **Entscheidung: Es bleibt dabei — der Methodenwechsel greift nicht von selbst, sondern wenn
   > das Bilanzjahr gepflegt wird.** Die automatische Umstellung zum 01.01.2027 ist ausdrücklich
   > verworfen. **Praktische Folge:** Projekte, die nach neuem Rechtsstand bewertet werden sollen,
   > brauchen ein gepflegtes Bilanzjahr; Dialoghinweis und Berichtsausweis nennen den tatsächlich
   > angesetzten Stand, damit das ungepflegte Feld nicht als stille Annahme durchgeht.
   >
   > **Und der Normtext fehlt weiterhin:** Abgebildet ist der **Wegfall** der Gutschrift, nicht das
   > Zuteilungsverfahren der DIN EN 15316-4-5. Deren Text gehört nicht zur Faktenbasis; liegt er
   > vor, ist er ein vierter Rechenweg neben den drei heutigen.
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
   sind, ist der Unterschied null. — **Unverändert offen.** E7 hat `KapitalwertRechner` nur um den
   Rückgabekanal erweitert, die Kostenreihen nicht getrennt.
6. **Feinere Bezugsgrößen für Wärmezentrale, bauliche Anlagen und Stromeinspeisung** (aus
   E3): Der saubere Weg wäre ein Kennzeichen an der Kostengruppe („diese Gruppe ist eine
   Investitionsgruppe im Sinn der VDI 2067") — eine Spalte an `Tab_KostenGruppenKatalog`
   und eine Pflegemaske. Eigene Entscheidung, keine Nebenwirkung von E3. — **Unverändert offen.**
   E7 zeigt die Bezugsgröße im Bericht jetzt ausdrücklich benannt an; grob bleibt sie.

**Mit der Abnahme E8 hinzugekommen** (Belege im
[`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md), Abschnitt 5.2):

7. **Die Zahlenprobe gegen die Altanwendung fehlt** (Befund **A8**). Konzept Abschnitt 8 verlangt
   sie als ersten Verifikationsschritt; keine der sieben Etappen hat sie gerechnet, keine begründet
   den Verzicht. **Es gibt damit keinen Nachweis, dass die neue Kette dieselbe Aufgabe löst wie die
   abgelöste Excel-Anwendung.** Der Vorgang braucht die Eingangsgrößen des Erlös-Screenshots als
   Projekt (Bedarf 100 MWh, Restbezug 62, Einspeisung 34, Eigenverbrauch 38) und eine Bewertung
   jeder Abweichung gegen die 17 Befunde der Analyse. **Der gewichtigste offene Punkt der
   Ausbaustufe.**
8. ~~**L13 — Bilanzierungskonvention für Biomasse**~~ (Befund **A4**) — **am 19.08.2026
   umgesetzt** ([`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md)).
   Zwei getrennte Angaben: die **Konvention** (`NULLANSATZ` / `VERBRENNUNG`) wirkt auf die
   Klimabilanz, der **Nachhaltigkeitsnachweis** (§ 8 EBeV 2030) auf die BEHG-Abgabe. Beide werden
   in Reiter, Word und Excel ausgewiesen.

   > **Die stille Annahme stand nicht im Code, sondern im Brennstoffkatalog:** Holz und Pellets 20,
   > Biogas 140, Rapsöl und Tierische Fette 210 g/kWh — reine Vorkettenwerte, also biogenes
   > Verbrennungs-CO₂ = 0. Sie ist die Vorgabe geworden. Gemessen an einem präparierten
   > Biomasseprojekt **dreht die Konventionswahl das Vorzeichen** (+44,89 → −38,67 t/a
   > Vermeidung); der fehlende Nachweis kostet **3.964,15 €/a** (Barwert 58.976,57 €).
   >
   > **Was dabei offen bleibt:** Die Bio-Heizöl-Mischungen (Kategorie 2) sind ausgenommen — ihr
   > biogener Anteil steckt im Katalogfaktor, das Datenmodell führt ihn nicht als eigene Größe.
   > Und `VariantenDaten.CO2Gesamt` bleibt die katalogbasierte Kennzahl; bei gewählter Konvention
   > `VERBRENNUNG` zeigen Kennzahl und Emissionsbilanz verschiedene CO₂-Zahlen für dasselbe
   > Projekt (im Bericht je benannt).
9. **`Tab_Kraftwerkspark` ohne `Bezugsbasis`** (Befund **A5**): Die fünf Konzeptspalten (`CO`,
   `Staub`, `GueltigAb`, `Quelle`, `ReadOnly` und vor allem **`Bezugsbasis TEXT(12)`**) sind nie
   angelegt worden. Damit besteht der Definitionsbruch des Altkatalogs fort — Faktoren je kWh
   **Brennstoff** und je kWh **Strom** stehen in derselben Spalte. Das Konzept nennt den Punkt in
   seinem Abschnitt 9 selbst als offen („Etappe E6 oder später"); er war nur nie in diese Liste
   übernommen worden.
10. **Keine Tests für die neuen Rechenklassen** (Befund **A1**): L9 verlangt „Tests im vorhandenen
    Testprojekt". `SteuerGutschriftRechner`, `StromTarifRechner`, `KwkgSatzRechner` und
    `KapitalwertRechner` kommen in `SpeicherEngine.Tests` und `KiKern.Tests` nicht vor. Jede Messung
    der Ausbaustufe ist ein Einzelnachweis aus einem Wegwerf-Harnisch. Ohne Rechenwirkung — aber der
    Grund, warum kein Regressionslauf über die Wirtschaftlichkeit existiert.
11. **Lokalisierung der drei neuen Masken** (Befund **A6**) samt der doppelten Zeilenbeschriftung
    „Hinweis" im Ergebnisreiter (Befund **B1**, `UcWirtschaftlichkeit.cs:573-576`). 63 Anzeigetexte
    in `Form_WirtschaftlichkeitParameter`, `Form_Tarifstruktur` und `Form_KwkgModule` gehören in
    beide `.resx` plus Designer; B1 sollte im selben Vorgang mit erledigt werden.

**Zur Referenzmenge:** Für eine *dauerhafte* Regressionsabdeckung des E6-Wirkungsfalls, der
E4/E5-Kette und des Variantenpfads fehlt ein Referenzprojekt mit gepflegten Angaben. Ein
ausgearbeiteter Vorschlag (Projekt **1031**, Feld für Feld mit den erwarteten Zahlen) liegt im
[`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md), Abschnitt 7 — **zur Entscheidung, nicht
ausgeführt**, weil er die produktive Datenbank verändern würde.

**Recherchelücken** (im Grundlagendokument als solche markiert, nicht geraten):
Auslösedauer des CO₂-Preisstabilitätsmechanismus, Wortlaut von § 10 Abs. 3 BEHG,
Zahlenreihe des Projektionsberichts 2026, Enddatum der Versteigerungsphase 2026,
Erdgassatz nach § 53a Abs. 3 EnergieStG.

**Datenbestand:** `DB-TARIF.XLS` und `DB-Kraftwerk.XLS` liefern nur die Struktur;
ihre Werte sind veraltet (Preisstand teils 1996, Emissionsdaten bis 2020) und
werden nicht übernommen.

---

## 6 Doppelte Wahrheiten — Bilanz nach W4

**Erledigt:** eine (die dreifache Kennzahlenliste, mit E7). **Fortbestehend:** vier. **Neu
entstanden:** zwei — beide bewusst, beide benannt. Damit hat W4 in dieser Rubrik weniger aufgeräumt,
als der Abschnittstitel bis E7 versprach; das ist hier korrigiert.

- **Der Stromsteuersatz an zwei Orten** — *neu benannt mit E8, Befund A7*. Der Katalog führt
  `STROMST_REGELSATZ = 20,50 €/MWh` (gelesen von der Steuerrechnung der E4), `Model/StromAufschlagModel.cs`
  führt `STROMSTEUER_REGELFALL = 2.050 ct/kWh` als **`const double`** (gelesen vom Aufschlagsblock
  der E5, als Bestandteil der Vorschlagssumme 11,746 ct/kWh). Heute wertgleich — aber nichts hält
  sie zusammen: Wird im Katalog ein neues Jahr gepflegt, rechnet der Aufschlagsblock still mit dem
  alten Satz weiter. Konzept Abschnitt 6 hatte genau diese Konstanten als „pflegbar zu machen"
  benannt; das ist nicht geschehen.
- **BHKW-Einspeisevergütung an vier Orten** (E5 hat einen hinzugefügt, um den
  Bestandsmangel zu beheben): `energy_project_settings.Verguetung_BHKW` (wirkt nur in der
  Speichersimulation), `Tab_ProjektTarif.Einsp_*` (Zonenmodell, HT/NT),
  `Tab_ProjektTarif.Einsp_Arbeit` (Rollenmodell, E5),
  `WirtschaftlichkeitParameter.Einspeiseverguetung_KWK` (Flat-Pfad, E5). Die Vorrangregel
  ist eindeutig — der aktive Tarif schlägt die Parameterwerte —, aber vier Felder für
  einen Preis sind drei zu viel.
- **Vorrangregel Projekt vor Katalog** in drei Implementierungen
  (`KostenEmissionRechner`, `StromPreisCtrl`, eine gespeicherte Access-Abfrage).
- ~~**14 Kennzahlzeilen doppelt** in Word- und Excel-Generator.~~ — die Zahl war der Stand
  **vor E2**; seither waren es +1 (E2) +3 (E4) +4 (E5) = **22 Zeilen**, und mit dem
  Ergebnisreiter **drei** Kopien, nicht zwei. **Mit E7 aufgelöst:**
  `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs` führt die Definition einmal, die
  drei Ausgaben rendern nur noch.
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
- **Zwei Lesewege auf die Kostenposition** — *neu entstanden mit E3*: `Abfrage_Kostenfaktoren`
  liegt als gespeicherte Access-Abfrage außerhalb des Repos und kennt die fünf neuen
  Spalten nicht; `Form_Kosten.LoadKostenFaktoren` holt sie deshalb über einen zweiten,
  direkten Zugriff auf `Tab_ProjektWerte` und führt sie über die ID zusammen. E7 geht denselben
  Weg für den Betriebskostenblock des Berichts — der zweite Lesepfad ist damit nicht mehr die
  Ausnahme, sondern der Normalfall für die neuen Spalten.

> **Bilanz:** W4 hat in dieser Rubrik **eine** Doppelung aufgelöst (die Kennzahlenliste, E7),
> **zwei** neue geschaffen (die zwei Lesewege der E3, den Stromsteuersatz an zwei Orten) und die
> Migrationsdoppelung von drei auf **vier** Tabellen ausgeweitet. Jede davon ist begründet und
> benannt — aber keine dieser Begründungen macht sie zu weniger als dem, was sie sind.
