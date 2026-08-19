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

---

## 1 Etappenübersicht

| # | Inhalt | Ergebniswirkung | Status |
|---|---|---|---|
| **E1** | Katalog `Tab_Gesetzesparameter`, Seed, Lesefassade, Pflegemaske; Überführung `Tab_KWKG_Staffel` | **keine** (nur Überführung, wertgleich) | **umgesetzt** (8/8 PASS, 194/194 byte-gleich) |
| **E2** | Vollbenutzungsstunden elektrisch, Vbh je Modul persistieren (Migrationsschritt 18) | **ja — Korrektur** zweier Rechenfehler | **umgesetzt** (8/8 PASS; Wirkung nur bei gepflegtem KWKG-Satz) |
| **E2-N** | *Nachtrag 1:* Ausschreibungsgrenze (500 kW) **je Anlage** statt je Projektsumme; Grenzwert aus dem Katalog | **ja — Korrektur**, wirkt nur bei mehr als einer Anlage | **umgesetzt** (8/8 PASS ×2, 194/194 byte-gleich; Wirkung an präparierten Kopien belegt) |
| **E2-N2** | *Nachtrag 2:* Heizöl-Ausschluss **je Anlage** und über die **installierten Anlagen** statt über die Gerätezeilen; Brennstoffart vorrangig aus `Tab_Energieanlagen.ID_Carrier` | **ja — Korrektur**, wirkt bei mehr als einer Anlage und bei verwaisten Öl-Gerätezeilen | **umgesetzt** (8/8 PASS ×2, 194/194 byte-gleich, 8/8 Wirtschaftlichkeitswerte gleich; Wirkung an präparierten Kopien belegt) |
| **E3** | Kostenposition um Kostenart, Bemessung, Erlös-Kennzeichen, Menge und Einheitpreis erweitern (Migrationsschritt 19); Betriebskosten-Dialog nach VDI 2067 mit elf Positionen in drei Spalten | **keine für Bestandsprojekte** — Schritt 19b belegt jede Bestandszeile mit `BETRAG`, und diese Bemessungsart ist zeilengleich der Rechenweg vor E3 | **umgesetzt** (9/9 PASS, 216/216 byte-gleich, 27/27 Betriebskosten- und Kapitalwertwerte identisch, 47 Harnisch-Proben ohne Fehlschlag) |
| **E4** | Energiesteuer- und Stromsteuergutschrift | nur bei gepflegten Angaben | offen |
| **E5** | Tarife mit drei Leistungspreismodellen, vermiedener Strombezug | nur bei gepflegten Tarifen | offen |
| **E6** | KWK-Zuschlag je Modul mit Katalogvorschlag | **ja** bei Mehrmodulanlagen | offen |
| **E7** | Bericht (Word und Excel), Mehrjahrestabelle | Ausgabe | offen |
| **E8** | Abnahme, neue Referenzbasis, Protokoll | eingefroren | offen |

**Regel für ergebniswirksame Etappen (E2, E6):** A/B-Nachweis gegen den
Vorgängerstand, Wirkungsbeleg mit Zahlen, danach neuer Basis-Freeze — dasselbe
Vorgehen wie bei der Bivalenzumstellung K-3.

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
| **Stichtag und Inbetriebnahme** sind ein Datumspaar je Projekt; § 6 KWKG gilt je Anlage — und dasselbe Datum entscheidet für alle Anlagen zugleich über Neuanlage/Bestandsanlage, also auch über den Heizöl-Ausschluss | `WirtschaftlichkeitCtrl.cs:958-982` | offen, E6 — **der gravierendste Restbefund der Reihe** |
| Energiesteuer- und Stromsteuererstattung fehlen vollständig | — | E4 |
| Vermiedener Strombezug ist keine Erlöszeile; die Bezugsgröße „Bedarf ohne Anlage" wird nirgends geführt | `StromMatrix.cs:35-42` | E5 |
| Ohne Photovoltaik im Projekt bekommt eingespeister BHKW-Strom **keinen Strompreis**, nur den Zuschlag | `Form_WirtschaftlichkeitParameter.cs:62-66` | E5 |
| ~~Bemessungsarten nach VDI 2067 (Prozent, je Stunde, je kWh) fehlen; Kostenpositionen kennen nur einen Eurobetrag~~ — **behoben mit E3**: `Tab_ProjektWerte` führt `Kostenart`, `Bemessung`, `IstErloes`, `Menge` und `Einheitpreis` (Migrationsschritt 19); `WirtschaftlichkeitCtrl.LiesBetriebskosten` wertet die Bemessung aus | `Tab_ProjektWerte`, `WirtschaftlichkeitCtrl.cs:1851` | **E3, erledigt** |
| ~~Negative Beträge für Erlöse sind nicht eingebbar~~ — **behoben mit E3**: Für Positionen mit `IstErloes` klemmt die Eingabe auf ≤ 0 statt auf ≥ 0, und der Rechenweg erzwingt das negative Vorzeichen | `ucKostenItem.cs:23-66`, `BetriebskostenCtrl.cs:261` | **E3, erledigt** |
| Aufschläge (Netzentgelt, Umlagen, Stromsteuer, Konzession, Vertrieb) erreichen die Jahreskostenrechnung nicht | `KostenEmissionRechner.cs:106-123` | E5 |
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
4. **Nachsaat fehlender Katalogschlüssel** (aus dem E2-Nachtrag, 19.08.2026):
   `GesetzKatalog.StelleKatalogSicher` sät nur bei leerer Tabelle ein. Ein Schlüssel,
   der nach dem ersten Seed hinzukommt, erreicht eine bereits gefüllte
   `Tab_Gesetzesparameter` deshalb nie — beim neuen `KWKG_AUSSCHREIBUNG_GRENZE_KW` fängt
   das die Code-Konstante auf, bei einem Schlüssel ohne Rückfallebene fiele es aus.
   Eine additive Nachsaat wäre die allgemeine Lösung, würde aber auch bewusst gelöschte
   Zeilen wieder auferstehen lassen. Zu entscheiden, bevor E4 bis E6 weitere Schlüssel
   anlegen.
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

- **BHKW-Einspeisevergütung an drei Orten**: `energy_project_settings.Verguetung_BHKW`
  (wirkt nur in der Speichersimulation), `Tab_ProjektTarif.Einsp_*` (HT/NT),
  `WirtschaftlichkeitParameter.Einspeiseverguetung` (nur Photovoltaik).
- **Vorrangregel Projekt vor Katalog** in drei Implementierungen
  (`KostenEmissionRechner`, `StromPreisCtrl`, eine gespeicherte Access-Abfrage).
- **14 Kennzahlzeilen doppelt** in Word- und Excel-Generator.
- **Zwei Migrationsmechanismen**: `SchemaMigration` mit Versionsmarker einerseits,
  eigenes DDL in `WirtschaftlichkeitCtrl` andererseits.
- **Komponenten-IDs an zwei Orten** (benannt mit E3): `Form_Kosten.GetKomponentenID`
  verdrahtet 1…7 hart, `UcBkKosten` und `KomponentenUebernahmeCtrl` lesen dieselbe
  Zuordnung dynamisch aus `Tab_KostenKomponente`. `BetriebskostenCtrl` musste sich für
  einen Weg entscheiden und führt die beiden gebrauchten IDs als benannte Konstanten.
- **Zwei Lesewege auf die Kostenposition** (entstanden mit E3): `Abfrage_Kostenfaktoren`
  liegt als gespeicherte Access-Abfrage außerhalb des Repos und kennt die fünf neuen
  Spalten nicht; `Form_Kosten.LoadKostenFaktoren` holt sie deshalb über einen zweiten,
  direkten Zugriff auf `Tab_ProjektWerte` und führt sie über die ID zusammen.
