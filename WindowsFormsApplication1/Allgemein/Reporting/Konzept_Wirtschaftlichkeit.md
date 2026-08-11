# Konzept: Wirtschaftlichkeitsberechnung in EPOS-Plan

**Fassung 3.1** · Stand 11.08.2026 (abends) · Status: entschieden + **Stufe W1 umgesetzt (Phase 6)** + **unabhängig code-verifiziert (Kap. 8)**
Quellen: `goetz_test.XLS` (BHKW-Plan, alle Blätter), Kostenmodul `Views/Kosten/*`, `Controller/ErgebnisCtrl.cs`, `Views/Varianten/EnergieMengen.cs`, DIN EN 17463 (ValERI), `VALERI_Vorlage_V7.xlsx` (epos-sync, Detailanalyse ausstehend)
Bezug: `Konzept_Berichtserstellung_EPOS-Plan.md` (Kapitel Wirtschaftlichkeit) (Feldmapping-Analyse: archivierter Stand im Claude-Projekt)

> **Was sich gegenüber Fassung 3 geändert hat (Fassung 3.1, Prüf-Session 11.08.2026 abends).**
> Der W1-Rechenkern wurde **unabhängig nachgerechnet und bestätigt** (Annuität,
> Kapitalwertformel, Restwert, Ersatzbeschaffung, dynamische Amortisation — Kap. 8.1).
> Zugleich ergab die Prüfung **Korrekturbedarf K1–K10** (Kap. 8.2, u. a. Zuschüsse
> mit Nutzungsdauer, Parameternachweis, veraltete Simulationsstände). Kapitel 3
> und 4 sind an den realen Code-Stand angepasst (Befunde 3.5.1–3 behoben;
> Menge × Preis existiert inzwischen; drei Aussagen in 3.1 präzisiert), die
> W1-Rechenkonventionen sind dokumentiert (5.1a), die W1/W2-Abgrenzung der
> Szenarien ist präzisiert (5.4), Szenarionamen vereinheitlicht auf
> **Worst/Erwartet/Best** (5.5).

> **Was sich gegenüber Fassung 2 geändert hat (11.08.2026).**
> **(1) Die offenen Punkte aus Kapitel 7 sind entschieden:** Referenz =
> **Stammprojekt** (Unterlassensalternative; KW der Variante = Barwert der
> Differenz-Zahlungsströme Variante − Stamm) · Vorgabe **i = 3,0 % / T = 20 a**
> (je Stamm editierbar) · **Restwert linear** · **Strompreise aus der
> Kostenmaske** (energy_project_settings, keine Doppelpflege).
> **(2) Stufe W1 ist implementiert** (Phase 6): `Allgemein/Wirtschaftlichkeit/`
> (`KapitalwertRechner`, `WirtschaftlichkeitCtrl`, DTOs),
> `Views/Wirtschaftlichkeit/` (Reiter + Parameterdialog), Berichts-Baustein und
> Excel-Blatt; Tabellen `Tab_ProjektWirtschaftlichkeit` /
> `Tab_ErgebnisWirtschaftlichkeit` werden beim ersten Start automatisch angelegt.
> Details: `Allgemein\Bericht\LIESMICH_Phase1.md`, Abschnitt Phase 6.
> **(3) DB-Nachverifikation** (Kenndaten.accdb-Kopie 11.08.2026): Emissionsfaktoren
> in **g/kWh** bestätigt (Tab_Brennstoff_Stamm: Erdgas 240, Heizöl 310, Strom 560);
> `energy_carrier`-Kopien tragen fast durchweg co2 = 0 → CO₂-Faktor-Kette
> **Projektwert → Tab_Brennstoff_Stamm → energy_carrier** ist zwingend und so
> im `KostenEmissionRechner` umgesetzt (auch für den Netzbezug; die Konstante
> 380 g/kWh ist nur noch Fallback).

> **Was sich gegenüber Fassung 1 geändert hat.** Zwei Vorgaben sind hinzugekommen:
> **(1) Die Berechnungsmethode ist entschieden** — Kapitalwertmethode angelehnt an
> **DIN EN 17463 (ValERI)**; das Alt-Verfahren (Annuitäten-Systemvergleich) bleibt
> Quelle für das Zahlungsgerüst und Validierungsreferenz, nicht mehr Methodenvorbild
> (neues Kapitel 5). **(2) Die Ergebnisse bekommen einen eigenen Platz im UI**: ein
> weiterer Reiter „Wirtschaftlichkeit" im Bereich **Berichte & Kosten**, mit
> Prüfung/automatischer Ausführung der Vorbedingungen (neues Kapitel 6).
> Die Analyse des Alt-Verfahrens (Kapitel 2), der Bestandsaufnahme (Kapitel 3) und
> der Lückenliste (Kapitel 4) ist unverändert gültig.

> **Einordnung.** Dieses Dokument rekonstruiert das Rechenverfahren der bewährten
> Excel-Mappe (BHKW-Plan, Blattfamilie `Tab_kurz*` / `Tab_Kosten` /
> `Tab_Wirtschaftlichkeit*`), stellt ihm gegenüber, was EPOS-Plan heute an Daten
> bereits führt, benennt die Lücken und definiert das Zielbild: **Kapitalwertmethode
> nach DIN EN 17463**. Es ersetzt nicht die fachliche Vorgabe, sondern bereitet sie
> vor: Kapitel 7 listet die verbleibenden Entscheidungen.
>
> *Hinweis zur Quellenlage:* Analysiert wurden die **Werte und Beschriftungen** aus
> `goetz_test.XLS` (Chat-Upload). Die **Struktur der VALERI-Vorlage** ist über die
> Zell-Dokumentation des INEKON-Skills `valeri-bewertung` vollständig bekannt und in
> **Kapitel 5.3** eingearbeitet. Die Formeldateien auf dem Z:-Netzlaufwerk selbst
> (`BHKW-WP-PLAN.XLSM`, `VALERI_Vorlage_V7.xlsx`, `wirtschaftlichkeit(.2).html`,
> `db_Struktur_Pricing_Modell.sql`, `Energiekosten_Designvorschlag.docx`) ließen sich
> über die Dateibrücke weiterhin nicht übertragen (Auflisten geht, Lesen nicht —
> mutmaßlich die Umlaute im Z:-Pfad); die Annuitätsformel wurde numerisch verifiziert
> (2.2). Workaround bei Bedarf: Dateien in den verbundenen C:-Ordner kopieren oder
> als Chat-Anhang geben.

---

## 1. Das Alt-Verfahren im Überblick (BHKW-Plan / Excel)

Die Mappe rechnet einen **Systemvergleich**: das geplante System (KWK-System bzw.
„Alternatives System" bei der WP) gegen eine **Vergleichsheizung** (Referenz-Heizkessel).
Drei Kurzbericht-Varianten teilen sich dieselbe Kostenbasis:

| Blatt | Variante | Besonderheit |
|---|---|---|
| `Tab_kurz` / `Tab_kurz_KWKG2016` | BHKW nach KWKG 2016 | Bonus bis 50 kW |
| `Tab_kurz_KWKG2020` | BHKW nach KWKG 2020 | Bonus je kWh mit **Vollbenutzungsstunden-Kontingent 30.000 Vbh**, jährlich gedeckelt |
| `Tab_kurz_WP` | Wärmepumpe | eigener Zinssatz/Zinsreduktion, Zuschuss, „Energiekosten" statt „Brennstoffkosten" |

Ergebnisgrößen in allen Varianten: Summe Investitionen (System/Vergleich),
Mehrinvestition, Kapitalkosten (Annuität), Betriebskosten, Brennstoff-/Energiekosten,
Erlöse, Gesamtkosten, **Überschuss gegenüber Vergleichsheizung** (€/a, mit und ohne
Tilgung), **spez. Wärmegestehungskosten** (nach Stromgutschrift),
**spez. Stromgestehungskosten** (nach Wärmegutschrift), **Amortisationszeit**
(„–"/9999 = amortisiert sich nicht; mit und ohne Energiesteuer-Effekte).

---

## 2. Rechenschema des Alt-Verfahrens im Detail

*(Bleibt maßgeblich als **Zahlungsgerüst-Katalog** für die Kapitalwertrechnung —
jede hier aufgeführte Kosten-/Erlöszeile wird in Kapitel 5 zu einem Zahlungsstrom.)*

### 2.1 Investitionen (`Tab_Kosten`, Zeilen 1–75)

Struktur je Block: **Position · Investition [€] · Nutzungsdauer [a] → Kapitalkosten [€/a]**.

| Block | Positionen |
|---|---|
| BHKW-Module | bis 10 Module einzeln; **reduzierter Zins** (Zinssatz − Zinsreduktion BHKW, z. B. 1,5 % − 1,15 %) |
| Spitzenkessel | bis 6 Kessel; voller Zinssatz |
| Heizraum | spez. Kosten €/m³ × Raumbedarf (BHKW, Kessel, Puffer) |
| Nahwärmenetz | Verteilnetz, Hausanschluss, Hausstation (× Anzahl) |
| Heizzentrale | BHKW-Einbindung, Heizungstechnik, Stromeinspeisung, Heizöllagerung, Erdgasanschluss, Schornstein, Abgasanlage, Pufferspeicher, bauliche Maßnahmen, Sonstiges 1–3 |
| Nebenkosten | wahlweise % der Investition oder Betrag |
| Zuschuss | mindert die Investitionssumme |
| Referenzsystem | eigener Block „Heizkessel" (Vergleichsheizung), gleiche Systematik |

### 2.2 Kapitalkosten: Annuität (numerisch verifiziert)

```
a(i, n) = i·(1+i)^n / ((1+i)^n − 1)          Kapitalkosten = Investition · a
```

Verifikation an den Blattwerten: BHKW 17 150 € · a(0,35 %; 13,33 a) = 1 319 €/a
(0,35 % = Zinssatz 1,5 % − Zinsreduktion 1,15 %); Kessel 3 731 € · a(1,5 %; 20 a) = 217 €/a. ✓
Die Nutzungsdauer des BHKW ist abgeleitet (z. B. aus Vbh-Kontingent/Betriebsstunden:
13,33 a), nicht frei eingegeben.

### 2.3 Betriebskosten (`Tab_Kosten`, Zeilen 77–91)

| Position | Bemessung |
|---|---|
| Wartung BHKW | €/h Betriebsstunden **oder** €/kWh_el Erzeugung |
| Instandhaltung BHKW / Spitzenkessel / Wärmezentrale / bauliche Anlagen / Stromeinspeisung | **% der jeweiligen Investition p. a.** (Staffel 2–6 %) |
| Personalkosten, Verwaltungskosten | % der Gesamtinvestition p. a. |
| Hilfsenergiekosten | % der Brennstoffkosten p. a. |
| Reserveleistung, Sonstige | Betrag |

### 2.4 Brennstoff-/Energiekosten (`Tab_Kosten`, Zeilen 96–108)

Je Träger: **Menge (Ho) × Preis** — Erdgas mit Arbeits- **und** Leistungspreis
(€/kWh_Ho, €/kW_Ho), Heizöl in l × €/l, ferner Biogas, Rapsöl, Holz, Flüssiggas,
Bioerdgas (Arbeit + Leistung), Sonstige, Strom (€/kWh). Die Mengen kommen aus der
Simulation (Verbrauch über Heizwert in die Abrechnungseinheit umgerechnet — exakt
das, was `EnergieMengen.Menge()` in EPOS-Plan heute schon tut).

### 2.5 Erlöse (`Tab_Erloese`, `Tab_Tarifstruktur`)

Alle Strommengen werden in einer **Matrix Winter/Sommer × HT/NT** geführt
(Strombedarf, Reststrombezug, Stromeinspeisung, Stromeigenverbrauch):

1. **Vermiedene Strombezugskosten** = Strombedarfskosten (Bedarf × Bezugspreis)
   − Reststrombezugskosten (Restbezug × Reststrompreis), zzgl. eingesparter
   Leistungspreis (max. Bedarf vs. max. Restbezug, kW × €/kW).
2. **Einspeiseerlös** = Einspeisemenge × Einspeise-Arbeitspreis je Tarifzone,
   zzgl. Leistungspreis-Komponente (P_soll, erfüllte Stunden).
3. **KWK-Bonus** nach Einspeisemodell getrennt für *Eigenstromnutzung* und
   *Einspeisung* (ct/kWh; KWKG 2020 mit Vbh-Kontingent, siehe 2.7).
4. **Energiesteuerrückerstattung** (§ 53a EnergieStG-Logik: Rückerstattungssatz
   auf KWK-Brennstoff; Blattwerte 4,4 / Gas 5,5 / Öl 61,35 [€/MWh]).
5. **Eingesparte Stromsteuer** für Eigenerzeugung (Blatt: −20,50 €/MWh,
   Staffel bis/ab 25 MWh).

Die `Tab_Tarifstruktur` definiert dazu **drei Preisregelungen** (Bezug, Einspeisung,
Restbezug) mit Sommer-/Winterzeitraum, HT/NT-Zeiten je Wochentag,
Leistungspreis-Staffeln („für die ersten … kW / die weiteren …") und dem
vereinfachten Modus **„Durchschnitt"** (ein Preis für alle Zonen — so ist die
Beispieldatei gerechnet).

### 2.6 Ergebnisbildung (`Tab_Wirtschaftlichkeit`)

```
Gesamtkosten  = Kapital + Betrieb + Brennstoff (+ Solaranlage)     je System
Gesamterlös   = Energiesteuerrückerstattung + eingesparte Stromsteuer
              + Einspeisung/Boni + vermiedene Strombezugskosten
Nettokosten   = Gesamtkosten − Gesamterlös
Überschuss    = Nettokosten(Vergleichsheizung) − Nettokosten(System)   [€/a]
Wärmegestehungskosten  = Nettokosten nach Stromgutschrift ÷ Nutzwärme   [€/kWh]
Stromgestehungskosten  = Nettokosten nach Wärmegutschrift ÷ KWK-Strom   [€/kWh]
```

Der Überschuss wird zweifach ausgewiesen: *mit Tilgung entsprechend der
Abschreibungszeit* und *ohne Tilgung, nur Zinsen*.

### 2.7 Kapitalisierte Betrachtung (`Tab_Wirtschaftlichkeit_kap`)

Jahresreihe über den Betrachtungszeitraum, Startwert = −Mehrinvestition:

- **Preissteigerung** getrennt für Brennstoffkosten und weitere Kostenarten, **Kapitalzins** zur Abzinsung der jährlichen Einsparung.
- Einnahmen-/Ausgabenzeilen je Jahr: vermiedener Strombezug, Einspeisung, Wärmeerlöse (Betriebs-/Brennstoffkosten der Vergleichsheizung), eingesparte Stromsteuer, Energiesteuerrückerstattung, Brennstoff- und Betriebskosten KWK, **Bonus EEG**, **Bonus KWK-Strom**.
- **KWKG-2020-Bonus mit Vbh-Kontingent:** je Jahr *erreichte* vs. *vergütete* Vollbenutzungsstunden (Jahres-Deckel laut Blatt-Tabelle: 2020–22 je 5 000, 2023/24 je 4 000, ab 2025 3 500 Vbh), **kumuliert bis 30 000 Vbh**, danach entfällt der Bonus.
- **BEHG/CO₂-Abgabe:** CO₂-Preis in €/t; Abgabe KWK-System, Abgabe Vergleichsheizung, Bilanzzeile.
- Amortisationszeit = erstes Jahr mit kumuliertem Überschuss ≥ 0.

→ Diese Jahresreihe **ist bereits eine Kapitalwertrechnung in Rohform** — das
Alt-Verfahren und die ValERI-Methode treffen sich hier (Kapitel 5).

### 2.8 Emissionsbilanz (`Tab_Energiebilanz`, `Tab_Emissionen`)

Brennstoffbilanz gekoppelte vs. getrennte Erzeugung: die getrennte Referenz erzeugt
dieselbe Wärme im Heizkessel und denselben Strom in einem **Referenz-Kraftwerkspark**
(Name, Wirkungsgrad, Faktoren CO₂/CO/SO₂/NOx/Staub in mg/kWh Brennstoff, inkl.
Netzverluste). Emissionsbilanz je Schadstoff: KWK-Erzeugung (BHKW + Kessel) vs.
getrennte Erzeugung (Heizkessel + Kraftwerk).

---

## 3. Was EPOS-Plan heute schon liefert

### 3.1 Mengengerüst — weitgehend vollständig (`Tab_Ergebnis*` via `ErgebnisCtrl.Load`)

Wärme-/Strombedarf und Spitzenlasten, Rest-Wärme/Rest-Strom (= Netzbezug), Erzeugung
je Gewerk und **je Modul**, Deckungsgrade, Jahresnutzungsgrad je Kesselmodul.
Einheiten MWh/a, kW, %, h/a. Stundenwerte für eine spätere HT/NT-Zerlegung
existieren in der Simulation. **Drei Präzisierungen aus der Code-Verifikation
(Fassung 3.1):**

1. **Vollbenutzungsstunden** führt nur die **WP** (`Tab_ErgebnisWaermepumpe`);
   das BHKW hat `Betriebsstunden_Gesamt/_Durchschnitt`, der Kessel keine Stunden.
2. **Brennstoffverbräuche in 9 Arten** existieren nur im **Aggregat** (BHKW/Kessel);
   je **Modul** gibt es genau *einen* Träger (`Brennstoff`-String + `carrier_id`)
   mit anteilig verteiltem Verbrauch — für Mischbetrieb (Modul 1 Gas, Modul 2 Öl)
   ist das Datenmodell zu grob.
3. **Einspeisemenge:** nur die **PV** persistiert einen Strom-`Ueberschuss`.
   `Tab_ErgebnisBHKW` führt ausschließlich einen **Wärme**überschuss — ein
   KWK-Strom-Einspeisewert fehlt (relevant für EEG/KWKG; Kap. 4 und K6 in Kap. 8).

### 3.2 Preisgerüst (`energy_carrier` / `energy_project_settings` / `energy_price`)

Je Projekt-Energieträger: Arbeits-, Grund-, Leistungspreis, Hi/Hs (kWh je Einheit),
Abrechnungseinheit + Umrechnung (`energy_conversion.factor`), Emissionsfaktoren
CO₂/SO₂/NOx, **Preishistorie** mit `valid_from` (tagesgenau, ein Satz je Tag,
Zukunftsdaten möglich — verwendbar als **Preisszenario-Stützstellen** für die
Kapitalwertrechnung). Katalogbasis `Tab_Brennstoff_Stamm` mit Standardpreisen,
`pricing_model` (FUEL, LIQUID_FUEL, SOLID_FUEL, GASEOUS_FUEL, ELECTRICITY, HEAT, …)
steuert, welche Felder gelten. Einzige vorhandene Rechnung: `Preis/kWh = Arbeitspreis ÷ Hi`.

### 3.3 Investitions-/Betriebskosten (`Tab_ProjektWerte` + `Tab_Kostenfaktor`)

Kostenpositionen je Projekt mit `KomponentenID` (WP=1, Heizkessel=2, PV=3, Solar=4,
Stromspeicher=5, Puffer=6, BHKW=7), `KategorieID` (1=Investition, 2=Betrieb,
3=Energie), Gruppe, Betrag, **Nutzungsdauer [a]** sowie **Best-/Worst-Case für
Betrag und Nutzungsdauer** — die geforderte **Sensitivitäts-/Szenariobasis nach
DIN EN 17463 ist damit strukturell schon angelegt**. Technik-Planwerte werden per
`Abfrage_ProjektKostenKomponenten` automatisch als Hauptposition übernommen.

### 3.4 Bindeglied Menge → Preis

`EnergieMengen.Menge()`: `Menge [Einheit] = Verbrauch [MWh] × 1000 ÷ eff. Heizwert
[kWh/Einheit]` über `Abfrage_Energietraeger_Effektiv` (custom-Werte mit
Katalog-Fallback). ~~Die Multiplikation Menge × Preis existiert nirgends~~
**Überholt (11.08.2026):** `Allgemein/Bericht/KostenEmissionRechner.cs` rechnet
seit Phase 5 Menge × Arbeitspreis (+ Grundpreis p. a.) je Träger über die
`carrier_id` der Ergebnis-Module, mit Preis-Fallback-Kette
`energy_project_settings` → `energy_carrier` und CO₂-Kette Projektwert →
`Tab_Brennstoff_Stamm` → `energy_carrier`. **Noch nicht verrechnet:** der
**Leistungspreis** (`custom_price_power` wird gepflegt, aber nicht gelesen) und
die `energy_price`-Historie (`valid_from` ist reine Anzeige — für Preisszenarien
in W2 anzubinden).

### 3.5 Relevante Codebefunde — Status nach Code-Verifikation 11.08.2026

**Behoben (Phase 1, verifiziert):**

1. ~~Brennstoff-Identität gebrochen~~ **teilweise behoben:** `carrier_id` ist
   echte Spalte in beiden Ergebnis-Modultabellen (selbstanlegend via
   `StelleModulSpaltenSicher()`), wird beim `Save()` **projektbezogen** über
   `CarrierIdFuerProjekt()` gesetzt (Vorrang `energy_project_settings`, dann
   Katalog-Fallback). **Restbefunde:** (a) `EnergieMengen.CarrierFor()` (Kapitel
   „Brennstoffmengen" im Bericht) rät weiterhin heuristisch ohne Projektbezug und
   konkateniert SQL; (b) die Semantik von `Tab_BHKW/Tab_Heizkessel.Brennstoff`
   ist im Code **widersprüchlich** (mal FK auf `energy_carrier.id`, mal auf
   `Tab_Brennstoff_Stamm.ID`/`id_brennstoff`) — an der realen DB zu klären;
   (c) je Modul weiterhin nur *ein* Träger (3.1).
2. ~~`ErgebnisCtrl.Delete()` funktionsunfähig~~ **behoben:** `Delete(int idProjekt)`
   mit Parameterbindung, Transaktion, Commit; alte parameterlose Überladung als
   `[Obsolete]`-Stub stillgelegt.
3. ~~Heizkessel-Modul-INSERT~~ **behoben:** `Waermeproduktion` wird persistiert,
   `Verbrauch` gerundet.

**Weiterhin offen (bestätigt):**

4. `Form_KostenAdmin`: Insert neuer Kostenfaktoren defekt (C#-Variable als
   SQL-Bezeichner, Parameter ohne Platzhalter, kein try/catch; `IsMainComponent`
   bleibt NULL → Satz erscheint nicht in der Liste); eigener
   Registry-Connection-String (`HKCU\…\ODBC.INI\TEST`) statt `DataRepository` —
   dieselbe Krücke auch in `UpdateDatabaseFromScript` (aktiv) und
   `Form_KostenfaktorItem` (tote Methode).
5. `energy_price`-Ersteintrag lässt `leistungspreis` leer, obwohl der Default
   ermittelt wird. **Zusatzbefund:** der Ersteintrag schreibt `valid_from` mit
   Uhrzeit (`DateTime.Now`), `ucFuelSettings` tagesgenau (`.Date`) — die
   tagesgenaue Duplikatprüfung findet den Ersteintrag dadurch nie
   (Doppeleinträge am selben Tag möglich).
6. Kostenmodul speichert Energiepreise nur über den Save-Button im
   `ucFuelSettings`-Control (kein Speichern beim Formular-Schließen, kein
   Dirty-Check beim Trägerwechsel); Investitions-/Betriebskosten speichern
   dagegen sofort — inkonsistente Speicherlogik im selben Dialog.

**Neue Befunde (Prüf-Session 11.08.2026):**

7. **Doppelkopie beim Variantenanlegen:** `ProjektDuplizierenCtrl` kopiert
   `energy_project_settings`/`energy_price` bereits generisch (leere
   Ausnahmeliste), `VariantenCtrl.KopiereEnergieEinstellungen()` kopiert danach
   **erneut** → doppelte Trägerzuordnungen je Variante bzw. abgefangener
   Index-Fehler mit MessageBox. Eine der beiden Kopien entfernen.
8. `energy_carrier.price_power` wird weder beim Anlegen geschrieben noch gelesen
   (`GetAllCarriers`) — Katalog-Leistungspreis kommt im UI immer als 0 an;
   Spaltenexistenz an der DB prüfen.
9. `Tab_ProjektWerte.KategorieID` wird beim Insert als `tabMain.SelectedIndex + 1`
   gesetzt — positionsabhängig; eine Reiter-Umsortierung verschiebt stillschweigend
   alle Kategorien. Auf namensbasierte Zuordnung umstellen.

---

## 4. Lückenliste: Alt-Verfahren vs. EPOS-Plan (Status nach W1, 11.08.2026)

| Größe | Status | Anker in EPOS-Plan |
|---|---|---|
| Kalkulationszinssatz | **W1 ✓** (i = 3,0 % Default, je Stamm editierbar) | `Tab_ProjektWirtschaftlichkeit.Zinssatz` |
| Zinsreduktion je Gewerk | offen (W2) | einheitlicher Projektzins in W1 |
| Betrachtungszeitraum T | **W1 ✓** (20 a Default) | `Tab_ProjektWirtschaftlichkeit.Betrachtungszeitraum` |
| Preissteigerungsraten (Energie/Betrieb) | **W1 ✓** (ein Satz, alle Szenarien gleich) | `Preissteigerung_Energie/_Betrieb`; `energy_price.valid_from` als W2-Stützstellen ungenutzt |
| Kapitalwert-/Barwertrechnung | **W1 ✓** (nachgerechnet, Kap. 8.1) | `KapitalwertRechner` |
| Restwertansatz (Nutzungsdauer > T) | **W1 ✓** (linear, letzte Ersatzgeneration) | `KapitalwertRechner` |
| Ersatzbeschaffung (Nutzungsdauer < T) | **W1 ✓** (nominal, Jahr = round(k·n)) | `KapitalwertRechner` |
| Zuschuss/Förderung (Vorzeichen/Typ) | **offen — Bug K1:** negative Position mit Nutzungsdauer wird je Ersatzjahr erneut gutgeschrieben | Spalte `IstErloes` (5.5) fehlt |
| KWKG-Bonus (ct/kWh, Vbh-Kontingent 30 000, Jahresdeckel) | offen (W2) | `Betriebsstunden_Gesamt`, `Stromproduktion` vorhanden |
| EEG-/Einspeisevergütung | **W1 ✓ nur PV** (`Ueberschuss` × Vergütung); **BHKW-Stromüberschuss fehlt als Feld** (3.1) | `Einspeiseverguetung`; K6 |
| Vermiedener Strombezug (Bedarf − Restbezug) | **W1 ✓ implizit**: geringerer `Stromrestbedarf` senkt A_t; keine eigene Erlöszeile (5.1a — bei W2-Ausbau Doppelzählung vermeiden!) | `KostenEmissionRechner` |
| Leistungspreis (laufend + eingespart) | **offen, stille Lücke:** `custom_price_power` gepflegt, aber nicht verrechnet | `Waermelast_Max`/`Strombedarf_Max` vorhanden |
| HT/NT-/Saison-Tarifstruktur | offen (W3) | Stundenwerte in der Simulation vorhanden |
| Energiesteuer/-rückerstattung, Stromsteuer | offen (W2) | — |
| CO₂-Preis (BEHG) €/t | offen (W2); CO₂-**Mengen** rechnet `KostenEmissionRechner` bereits | Emissionsfaktoren g/kWh, DB-verifiziert |
| Referenzszenario | **entschieden ✓**: Stamm = Referenz (Differenzrechnung) | `KapitalwertDiff` |
| Wärmegestehungskosten | **W1 ✓** (annuisierter Nettobarwert ÷ Wärme**bedarf** — Konvention 5.1a) | `WirtschaftlichkeitCtrl` |
| Stromgestehungskosten | offen (W2) | — |
| Amortisation (dynamisch) | **W1 ✓** (Differenzreihe, ohne Restwert, interpoliert) | `KapitalwertRechner` |
| MwSt-/Netto-Brutto-Kennzeichen | offen | `Einheit` ist Freitext |
| Referenz-Kraftwerkspark (Emissionsgutschrift) | offen (W3) | CO₂/SO₂/NOx je Träger vorhanden |

---

## 5. Zielbild: Kapitalwertmethode nach DIN EN 17463 (ValERI)

**Vorgabe (10.08.2026):** Die Wirtschaftlichkeitsberechnung erfolgt mit der
**Kapitalwertmethode, angelehnt an DIN EN 17463** („Bewertung energiebezogener
Investitionen", ValERI). Kern der Norm: der **Kapitalwert (Net Present Value)** als
zentrales Bewertungskriterium; Erfassung **aller relevanten Zahlungsströme** über
den Betrachtungszeitraum; **transparente Dokumentation** der Annahmen; **Szenario-
und Sensitivitätsanalysen** zur Abbildung von Unsicherheiten. (Die Norm wird u. a.
in EnSimiMaV und EnFG referenziert — für Förder-/Nachweiszwecke relevant.)
Als Arbeitsvorlage dient **`VALERI_Vorlage_V7.xlsx`** (epos-sync-Tools; Struktur
wird nachgetragen, sobald die Datei lesbar ist — siehe Quellenhinweis oben).

### 5.1 Rechenkern

```
KW = −I₀ + Σ_{t=1..T} ( E_t − A_t ) / (1+i)^t  +  RW_T / (1+i)^T

I₀    Investitionsauszahlung abzgl. Zuschüsse (Tab_ProjektWerte, Kategorie 1)
E_t   Einzahlungen im Jahr t: Erlöszeilen aus 2.5/2.7 (vermiedener Bezug,
      Einspeisung, KWKG-/EEG-Bonus, Steuererstattungen), preisgesteigert
A_t   Auszahlungen im Jahr t: Betriebskosten (2.3), Energiekosten (2.4),
      CO₂-Abgabe (BEHG), Ersatzbeschaffungen (Position mit Nutzungsdauer < T)
RW_T  Restwert linear: Investition · (Restnutzungsdauer / Nutzungsdauer)
i     Kalkulationszinssatz;  T  Betrachtungszeitraum
```

Abgeleitete Kennzahlen: **Kapitalwert KW** (Hauptkriterium; > 0 = wirtschaftlich),
**Annuität des Kapitalwerts** `KW · a(i,T)` (vergleichbar mit dem €/a-Überschuss des
Alt-Verfahrens), **dynamische Amortisationszeit** (erstes t mit kumuliertem Barwert
≥ 0; „–" wenn nie), optional **interner Zinsfuß** (Nullstelle KW(i)) und
**Kapitalwertrate** KW/I₀. Wärme-/Stromgestehungskosten weiterhin nach 2.6.

**Verhältnis zum Alt-Verfahren:** identisches Zahlungsgerüst (Kapitel 2 = Katalog
der E_t/A_t-Zeilen), aber Barwertsummen statt Annuitätenvergleich. Die
`Tab_Wirtschaftlichkeit_kap`-Jahresreihe des Alt-Excel dient als
**Validierungsreferenz** für die Jahreswerte vor Abzinsung; die Zinsreduktion je
Gewerk (2.2) wird zum positionsbezogenen Zinssatz-Override.

### 5.1a Rechenkonventionen W1 (aus dem Code dokumentiert, Fassung 3.1)

Diese Setzungen sind implementiert und gelten, bis W2 sie erweitert:

- **Preissteigerung:** Faktor `(1+p)^(t−1)` — **Jahr 1 = Basispreis** ohne
  Steigerung (nicht `(1+p)^t`).
- **Einspeiseerlöse** sind in W1 **nominal konstant** (keine Preissteigerung).
- **Ersatzbeschaffung** wird im Jahr `round(k·Nutzungsdauer)` gebucht, nominal
  in Höhe der (szenariogewichteten) Erstinvestition, abgezinst.
- **Nutzungsdauer < 1 a** (bzw. nicht gepflegt) wird wie Nutzungsdauer = T
  behandelt: kein Ersatz, kein Restwert.
- **Vermiedener Strombezug:** in W1 **keine eigene Erlöszeile** — er entsteht
  implizit, weil die Variante einen geringeren `Stromrestbedarf` (Netzbezug) in
  den Auszahlungen A_t hat und differenziell gegen den Stamm gerechnet wird.
  **Beim W2-Ausbau (Erlöszeile nach 2.5.1) unbedingt beachten, sonst
  Doppelzählung.**
- **Wärmegestehungskosten W1** = `(−KW · a(i,T)) ÷ (Wärmebedarf_Gesamt × 1000)`
  [€/kWh] — Bezugsgröße ist der Wärme**bedarf**, nicht die Nutzwärme-Erzeugung;
  bei KW > 0 wird der Wert negativ.
- **Differenzrechnung:** jedes Projekt wird absolut gerechnet,
  `KapitalwertDiff = KW(Variante) − KW(Stamm)` — mathematisch identisch mit dem
  Barwert der Differenz-Zahlungsströme (gleiches i/T, Linearität der Abzinsung).

### 5.2 Szenarien und Sensitivität (Normanforderung)

- **Szenarien Best/Real/Worst** direkt aus `Tab_ProjektWerte` (Best-/Worst-Case für
  Betrag und Nutzungsdauer sind vorhanden, 3.3) plus Preisszenarien
  (Preissteigerungsrate ± Delta).
- **Sensitivität** mindestens über: Kalkulationszins, Energiepreissteigerung,
  Investitionshöhe, Vbh/Erzeugungsmengen. Darstellung als Tabelle
  (Parameter · −Δ · Basis · +Δ → KW) im Reiter und im Bericht.
- **Dokumentation:** jeder Rechenlauf persistiert seinen Parametersatz
  (Zeitstempel, Zins, T, Preisannahmen, Szenario) — Nachvollziehbarkeit ist
  Normbestandteil und zugleich der Nachweisblock im Bericht.

### 5.3 Die VALERI-Vorlage als Strukturvorbild *(nachgetragen)*

Die im Haus verwendete **VALERI-Auswertung** (Vorlage V7; Zellstruktur dokumentiert
im INEKON-Skill `valeri-bewertung`) konkretisiert, wie die Norm bei INEKON gelebt
wird — und liefert damit das Vorbild für Datenmodell und Darstellung:

- **Ein Tabellenblatt je Maßnahme** (hier: je **Variante**), dazu ein globales Blatt
  **„Allgemeine Unternehmensdaten"** mit Strompreis, Gaspreis, Zinssatz/WACC,
  Einspeisevergütung und Preis des flexiblen Kostenfaktors — **je Szenario**.
  → Entspricht `Tab_ProjektWirtschaftlichkeit` (5.5): globale Parameter zentral,
  nie im Einzelblatt.
- **Durchgängig drei Szenarien Worst / Erwartet / Best** — jede Eingabezelle
  dreifach; Default-Streuung als Formel um den Erwartungswert (z. B. ±10 %,
  Investition +30/−25 %), je Faktor überschreibbar.
  → Deckt sich mit Best-/Worst-Feldern in `Tab_ProjektWerte` (3.3); die
  faktorweise Schwankungsbreite wird als optionales Feld je Kostenposition geführt.
- **Zahlungsströme als Faktorlisten:** *Nutzen*-Faktoren (Einsparung Strom/Gas/
  flexibler Kostenfaktor in %, Eigennutzung/Einspeisung Strom in kWh/a, eingesparte
  Arbeitskraft, Mehrproduktion, Entsorgungsgewinn, frei belegbare €/a-Zeilen) und
  *Lasten*-Faktoren (Mehrverbräuche, Entsorgungskosten, Jahresbetriebskosten,
  **Investition zum Zeitpunkt t**, Abzahlung, zusätzliche Arbeitskraft,
  Minderproduktion) — je Faktor mit **Start-/Endjahr, Degradation und
  Preisänderungsrate**.
  → Das ist die generalisierte Form des Zahlungsgerüsts aus Kapitel 2; EPOS-Plan
  befüllt diese Faktoren automatisch aus Simulation (`Tab_Ergebnis*`), Preisen
  (`energy_*`) und Kostenpositionen (`Tab_ProjektWerte`), statt sie von Hand
  einzutragen.
- **Betriebszeit-Logik:** erstes Betriebsjahr als **relativer Index** (0 = ab dem
  ersten Betrachtungsjahr) plus Nutzungszeit — wichtige Übernahme für den
  Rechenkern: Cashflows beginnen erst ab dem Startindex.
- **Energiekosten rechnet VALERI selbst** aus kWh × globalen Preisen — übertragen
  werden Mengen, keine €-Beträge. Gleiches Prinzip im EPOS-Rechenmodul
  (`EnergieMengen` liefert Mengen, Preise kommen aus `energy_project_settings`).
- **Ergebniszellen:** Kapitalwert und Amortisationszeit **je Szenario**; darunter
  ein weitgehend formelgenerierter **Bewertungsbericht** mit definierten
  Freitextstellen (qualitative, nicht monetarisierbare Wirkungen).
  → Vorbild für den UI-Reiter (Kapitel 6) und das Berichtskapitel: Kennzahlen je
  Szenario + kurzer Empfehlungstext + Freitextfeld für qualitative Wirkungen.

Für Maßnahmen ohne eigene Energieträger-Zeile (z. B. Heizöl) nutzt die Vorlage
einen **flexiblen Kostenfaktor** mit eigenem €/kWh-Preis — EPOS-Plan braucht diese
Krücke nicht, da alle Träger über `energy_carrier` typisiert sind; das ist ein
struktureller Vorteil der App-Umsetzung gegenüber der Excel-Vorlage.

### 5.4 Ausbaustufen (aktualisiert)

| Stufe | Inhalt | Referenz |
|---|---|---|
| **W1 — Kapitalwert Basis** *(umgesetzt 11.08.2026)* | Zahlungsgerüst aus Kapitel 2 mit **Durchschnittspreisen** (Arbeits-+Grundpreis; ohne Leistungspreis); KW, Annuität des KW, dynamische Amortisation, Restwert, Ersatzbeschaffung, Preissteigerung Energie/Betrieb; **Kosten-Szenarien Worst/Erwartet/Best** aus den Best-/Worst-Spalten von `Tab_ProjektWerte` (Preise/Zins/T in allen Szenarien gleich); Referenz = Stamm (Differenzrechnung); Wärmegestehungskosten | DIN EN 17463 Kern; `Tab_kurz*` als Zahlenquelle |
| **W2 — Szenarien/Detail** | **Preis-/Zins-Szenarien** (jede Eingabe dreifach nach VALERI-Vorbild 5.3 inkl. Default-Streuung um den Erwartungswert), Leistungspreis (laufend + eingespart), Stromgestehungskosten, KWKG-Vbh-Kontingent jahresscharf, BEHG-CO₂, Sensitivitätstabelle, interner Zinsfuß, Zinsreduktion je Gewerk, `Bemessung`-Spalte (%_Investition, €/h, €/kWh) | `Tab_Wirtschaftlichkeit_kap`, Norm-Szenarioanforderung |
| **W3 — Tarife/Emission** | HT/NT-Saisonmatrix aus Stundenwerten, Leistungspreis-Staffeln, Emissionsbilanz mit Kraftwerkspark | `Tab_Tarifstruktur`, `Tab_Emissionen` |

Schon **W1** füllt den neuen UI-Reiter (Kapitel 6) und das Berichtskapitel
„Wirtschaftlichkeit" des Berichts vollständig.

### 5.5 Neue Datenstrukturen (additiv, Access-kompatibel)

*Umsetzungsstand W1 (Phase 6):* beide Tabellen werden von
`WirtschaftlichkeitCtrl.StelleTabellenSicher()` automatisch angelegt — in W1 mit
dem tatsächlich benötigten Spaltenumfang (Parameter: `Zinssatz`,
`Betrachtungszeitraum`, `Preissteigerung_Energie`, `Preissteigerung_Betrieb`,
`Einspeiseverguetung`; eine Zeile je **Stamm**, gilt für die Gruppe). Die unten
stehende Vollausstattung (KWKG/EEG/Steuern/Tarife) bleibt das W2/W3-Zielbild;
Spalten werden dann additiv nachgerüstet.

**`Tab_ProjektWirtschaftlichkeit`** (1 Zeile je Projekt — Parameter des Rechenlaufs):
`ID_Projekt, Zinssatz, Zinsreduktion_BHKW, Zinsreduktion_WP, Betrachtungszeitraum,
Preissteigerung_Energie, Preissteigerung_Betrieb, CO2_Preis, USt_Satz,
KWKG_Bonus_Eigenstrom, KWKG_Bonus_Einspeisung, KWKG_Vbh_Kontingent,
EEG_Verguetung, Strompreis_Bezug, Strompreis_Restbezug, Strompreis_Einspeisung,
Leistungspreis_Bezug, Stromsteuer_Satz, Energiesteuer_Gas, Energiesteuer_Oel,
Energiesteuer_Rueckerstattung, Inbetriebnahme`
(Strompreise als Vorbelegung aus `energy_project_settings` des Trägers „Strom";
W3 ersetzt die Einzelpreise durch eine Tarifstruktur-Tabelle.)

**`Tab_ErgebnisWirtschaftlichkeit`** *(neu in Fassung 2)* — persistiertes Ergebnis
je Projekt und Szenario, damit der UI-Reiter (Kapitel 6) und der Bericht ohne
Neuberechnung anzeigen können: `ID, ID_Projekt, ID_Ergebnis (FK auf den
Simulationslauf!), Szenario (**Worst/Erwartet/Best** — so die implementierten
DB-Werte; früherer Arbeitsname „Real" = „Erwartet"), Zeitstempel, Kapitalwert,
AnnuitaetKW, AmortisationJahre, IRR, Investition, Restwert, ...` plus die
Jahreskosten-/Erlösfelder aus der Ergebnisklasse (6.1 im Berichtskonzept).
*Hinweis W1:* die Parametertabelle führt **eine Zeile je Stamm** (Spaltenname
`ID_Projekt` meint die Stamm-ID); der persistierte Parametersatz je Rechenlauf
muss auch **zurückgelesen** werden — siehe K2 in Kap. 8.
Der FK auf `Tab_Ergebnis.ID` schließt die heutige Lücke „Kosten ohne Bezug zum
Simulationsstand".

**Erweiterung `Tab_ProjektWerte`** (Spalten additiv): `Kostenart`
(Kapital/Betrieb/Bedarf/Erlös nach VDI-2067-Systematik), `Bemessung`
(Betrag | %_Investition | €_pro_h | €_pro_kWh), `IstErloes` (Bool, für Zuschüsse).

**`carrier_id`** in `Tab_ErgebnisBHKWModul` und `Tab_ErgebnisHeizkesselModul`
(Befund 3.5.1), gesetzt beim Speichern des Simulationslaufs.

### 5.6 Vergleichsheizung im Variantenmodell

Das Alt-Verfahren vergleicht **innerhalb einer Rechnung** System vs. Referenzkessel.
EPOS-Plan hat zusätzlich den **Variantenvergleich über Projekte** (`Tab_Variante`).
Vorschlag, beides zu verbinden:

- Die Vergleichsheizung ist **je Projekt** definiert (Referenzkessel: Investition,
  Nutzungsdauer, Wirkungsgrad, Träger) — als Kostenpositionen mit eigener
  Komponente „Vergleichsheizung" oder als Feldgruppe in `Tab_ProjektWirtschaftlichkeit`.
  In der ValERI-Logik ist sie das **Referenzszenario („Unterlassensalternative")**,
  gegen das die Zahlungsströme der Investition differenziell gebildet werden.
- Der Variantenvergleich zeigt dann je Projekt den internen Vergleich (System vs.
  Referenz, ausgedrückt im Kapitalwert) **und** quer über die Varianten die
  Kapitalwerte/Amortisationen.
- Alternative (einfacher): das Stammprojekt selbst ist die Referenz; KW der Variante
  = Barwert der Differenz-Zahlungsströme Variante − Stamm. → Entscheidung Kapitel 7.

### 5.7 Rechenmodul und Ergebnisklasse

`Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` implementiert
`IWirtschaftlichkeitProvider` (Schnittstelle im Berichtskonzept, Kapitel 6):
liest ausschließlich `Tab_Ergebnis*`, `Tab_ProjektWerte`, `energy_*`,
`Tab_ProjektWirtschaftlichkeit`; schreibt `Tab_ErgebnisWirtschaftlichkeit`; keine
UI-Abhängigkeit. Testbar über Referenzprojekte: Jahreswerte gegen die
Excel-Blattwerte (goetz_test: Kapitalkosten 1 536,08 €/a, Vergleich 217 €/a …),
Kapitalwerte gegen die `VALERI_Vorlage_V7.xlsx` (sobald lesbar).
`WirtschaftlichkeitErgebnisModel` wird um die Kapitalwert-Kennzahlen ergänzt
(Kapitalwert, AnnuitaetKW, AmortisationJahre dynamisch, IRR, Restwert, Szenario).

---

## 6. UI-Integration: Reiter „Wirtschaftlichkeit" unter Berichte & Kosten

*Umsetzungsstand (verifiziert 11.08.2026):* `Form_Wirtschaftlichkeit` +
`Form_WirtschaftlichkeitParameter` existieren und lesen/schreiben die
persistierten Ergebnisse; Einstieg derzeit **nur** über den Button im
Varianten-Dialog — der dritte Button in „Berichte & Kosten"/`Form_Start` ist ein
offener Designer-Handgriff. **Abweichungen von der Soll-Prüfkette unten:**
veraltete (nicht fehlende) Simulationsstände werden **nicht** automatisch
nachgerechnet (K3), der Parameterstand wird beim Aktualitätscheck nicht
verglichen (K2), Fehlgründe decken nur fehlende Energiepreise ab — „keine
Kostenpositionen" fehlt ebenso wie der Absprung in `Form_Kosten` (K7).

**Ist-Zustand:** Der Hauptbereich **„Berichte & Kosten"** enthält die Schaltflächen
**[Kosten]** (→ `Form_Kosten`) und **[Varianten]** (→ Dialog „Projektvarianten":
Stamm-Combo, Variantenliste, Bezeichner/Anlegen/Löschen, „Simulation starten",
„Projektvergleich + Bericht").

**Soll:** Ein **weiterer Reiter/Bereich „Wirtschaftlichkeit"** zeigt die
Wirtschaftlichkeits-Ergebnisse der Vergleichsgruppe direkt in der App — vor und
unabhängig von der Berichtserzeugung:

```
┌─ Berichte & Kosten ──────────────────────────────────────────────────┐
│  [Kosten]   [Varianten]   [Wirtschaftlichkeit]                       │
│                                                                      │
│  Wirtschaftlichkeit — Stamm „Wöhler" + 2 Varianten   Szenario:[Real▾]│
│  ┌────────────────────────────┬─────────┬─────────┬─────────┐        │
│  │ Kennzahl                   │ Stamm   │ Test1   │ Test2   │        │
│  ├────────────────────────────┼─────────┼─────────┼─────────┤        │
│  │ Investition [€]            │ 20 881  │ 28 898  │ …       │        │
│  │ Kapitalwert [€]            │ −12 400 │ +3 150  │ …       │        │
│  │ Annuität des KW [€/a]      │ −1 030  │ +262    │ …       │        │
│  │ Amortisation [a]           │ –       │ 11,2    │ …       │        │
│  │ Gestehungskosten [€/kWh]   │ 0,14    │ 0,11    │ …       │        │
│  └────────────────────────────┴─────────┴─────────┴─────────┘        │
│  Parameter: i=1,5 % · T=20 a · Preise Stand 08/2026  [Berechnen]     │
└──────────────────────────────────────────────────────────────────────┘
```

**Ablauf und Vorbedingungen** (Vorgabe: „Varianten müssen zuvor ausgewählt und
berechnet worden sein — Berechnung prüfen und ggf. automatisch ausführen"):

1. **Vergleichsgruppe** = die im Varianten-Dialog gewählte Gruppe (Stamm + angehakte
   Varianten; gleiche Quelle wie der Bericht — `VergleichsDaten`).
2. Beim Öffnen des Reiters läuft die **Prüfkette je Projekt**:
   a) Simulationsergebnis vorhanden und aktuell? (Kriterien wie Aktualitätsprüfung
      im Berichtskonzept 3.4) — sonst Angebot „Jetzt simulieren" (headless
      `SimulationRunner`).
   b) Wirtschaftlichkeits-Ergebnis vorhanden (`Tab_ErgebnisWirtschaftlichkeit`) und
      **auf dem aktuellen Simulationslauf** (FK `ID_Ergebnis`) und Parameterstand? —
      sonst automatisch `WirtschaftlichkeitCtrl.Berechne()` (Sekundenbereich, kein
      Dialog nötig; Fortschritt in der Statuszeile).
   c) Fehlende Eingaben (kein Zinssatz, keine Kostenpositionen, kein Energiepreis
      für einen verwendeten Träger) → Zeile wird mit Begründung gezeigt
      („Wirtschaftlichkeit nicht berechenbar: kein Arbeitspreis für ‚Heizöl'"),
      mit Absprung in `Form_Kosten` bzw. den Parameterdialog.
3. Anzeige: Kennzahlen-Vergleichstabelle (Zeilen wie Skizze, Spalten = Projekte),
   Szenario-Umschalter (Best/Real/Worst, W2), Parameterzeile als Nachweis,
   Schaltfläche **[Berechnen]** für manuelles Neurechnen.
4. **Parameterpflege** (Zins, T, Preissteigerung, Boni): eigener kleiner Dialog
   „Wirtschaftlichkeits-Parameter" (`Tab_ProjektWirtschaftlichkeit`), erreichbar aus
   dem Reiter; Vorbelegung projektübergreifend, Overrides je Projekt.
5. Der **Variantenbericht** (Baustein „Wirtschaftlichkeit") und die Excel-Ausgabe
   lesen dieselben persistierten Ergebnisse — Reiter, Word und Excel zeigen
   garantiert identische Zahlen.

Umsetzungsdateien (Ergänzung zur Dateiliste im Berichtskonzept 7.1):
`Views/Wirtschaftlichkeit/Form_Wirtschaftlichkeit.*` (Reiter-Inhalt),
`Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.*`,
`Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs`,
`Model/WirtschaftlichkeitErgebnisModel.cs` (erweitert um KW-Kennzahlen und Szenario).

---

## 7. Offene Punkte für die fachliche Vorgabe

1. ~~Methode-Grundsatz~~ **entschieden:** Kapitalwertmethode nach DIN EN 17463.
   ~~Zins/T/Restwert~~ **entschieden (11.08.2026):** Vorgabe **i = 3,0 %**,
   **T = 20 a** (beides je Stamm in `Tab_ProjektWirtschaftlichkeit` editierbar),
   **Restwert linear** (5.1). Offen bleibt die **Zinsreduktion je Gewerk**
   (W1 rechnet mit einheitlichem Projektzins; positionsbezogene Zins-Overrides
   sind W2-Kandidat).
2. ~~VALERI-Vorlagenstruktur~~ **nachgetragen (5.3)** aus der Zell-Dokumentation des
   Skills `valeri-bewertung`. Verbleibend: Formel-Feinheiten der Vorlage (exakte
   Barwert-/Aktivierungslogik, Degradationsverrechnung) direkt in
   `VALERI_Vorlage_V7.xlsx` gegenprüfen, sobald die Datei lesbar ist; Beispiele in
   `60-Tools\20-Sammung_Tools\Wirtschaftlichkeit` sichten (Ordner nicht verbunden).
3. ~~Vergleichsheizung/Referenzszenario~~ **entschieden (11.08.2026): Stammprojekt
   als Referenz** (5.6, einfache Alternative): KW der Variante = Barwert der
   Differenz-Zahlungsströme Variante − Stamm; der Stamm zeigt seinen
   Nettokosten-Barwert. Ein Referenzkessel je Projekt bleibt W2/W3-Option.
4. ~~Parameterherkunft Strompreise~~ **entschieden (11.08.2026): aus der
   Kostenmaske** (`energy_project_settings`, Träger Strom) — keine Doppelpflege.
   In `Tab_ProjektWirtschaftlichkeit` stehen nur Zins, T, Preissteigerungen und
   die Einspeisevergütung.
5. **Steuersätze/Boni als Stammdaten:** Energiesteuer-, Stromsteuer-, KWKG-, EEG-Sätze
   ändern sich gesetzlich — Katalogtabelle mit Gültigkeitsdatum (analog `energy_price`)
   oder Projekteingabe?
6. **Gültigkeit je Erzeugertyp:** Blattfamilie ist BHKW-zentriert; `Tab_kurz_WP` zeigt
   die WP-Variante. Was gilt für PV-Only-/Hybrid-Varianten (EEG statt KWKG,
   Eigenverbrauchsquote)?
7. **MwSt:** Alt-Verfahren rechnet „ohne MwSt" — bleibt Netto verbindlich?
8. **Emissionsbilanz W3:** Referenz-Kraftwerkspark als Katalog — wer pflegt die Datensätze?
9. **Validierungsfälle:** `goetz_test.XLS` (BHKW/Öl), `englmar haus der
   gastlichkeit.XLS` (Realprojekt) für das Zahlungsgerüst; `VALERI_Vorlage_V7.xlsx`
   für die Kapitalwert-Kennzahlen.

---

## 8. Unabhängige Code-Verifikation W1 (Prüf-Session 11.08.2026, Fassung 3.1)

### 8.1 Bestätigt (numerisch nachgerechnet)

Der Rechenkern `KapitalwertRechner` wurde unabhängig reimplementiert und
gegengerechnet: Annuität `a(i,n)` inkl. Grenzfall i=0 (→ 1/n) ✓ ·
Konzept-Validierungswerte 17 150 € · a(0,35 %; 13,33 a) = **1 319,07 €/a** und
3 731 € · a(1,5 %; 20 a) = **217,31 €/a** ✓ · KW-Formel 5.1 exakt reproduziert ✓ ·
Restwert linear auf die **letzte Ersatzgeneration** ✓ · dynamische Amortisation
(kumulierter Barwert der Differenzreihe, ohne Restwert, interpoliert) ✓ ·
Differenzrechnung ≡ Barwert der Differenz-Zahlungsströme ✓ · SQL durchgängig
parametrisiert, keine Kultur-/Dezimalfalle ✓ · Reiter, Word-Baustein und
Excel-Blatt lesen dieselben persistierten Ergebnisse, keiner rechnet selbst ✓ ·
Tabellenschema entspricht dem W1-Umfang aus 5.5 ✓ · Defaults i=3,0 %/T=20 a ✓.

### 8.2 Korrekturliste K1–K10 (Code, Priorität absteigend)

| # | Befund | Korrektur |
|---|---|---|
| **K1** | **Zuschüsse:** negative Kat.-1-Position mit Nutzungsdauer < T wird in jedem Ersatzjahr erneut „beschafft" (= erneut gutgeschrieben); Beispiel −5 000 €, n=10, T=20, i=3 % → KW +8 720 € statt +5 000 € | Ersatz/Restwert nur für Betrag > 0 bilden oder Spalte `IstErloes` (5.5) einführen |
| **K2** | **Parameternachweis:** Parametersatz wird je Lauf persistiert, aber nie zurückgelesen; Nachweiszeilen in Reiter/Word/Excel zeigen die *aktuellen* Parameter — nach Parameteränderung ohne Neuberechnung behauptet der Bericht falsche Annahmen (Normanforderung 5.2!). `ErgebnisAktuell()` prüft nur `ID_Ergebnis`, nicht den Parameterstand (6.2b) | `WirtschaftlichkeitErgebnis` um Parameterfelder ergänzen, in `LadeErgebnisse()` mappen, Nachweis aus dem Ergebnis speisen; Parametervergleich in `ErgebnisAktuell()` |
| **K3** | **Veraltete Simulationen:** Reiter rechnet mit `neuRechnen:false` — veraltete (nicht fehlende) Stände werden nicht nachsimuliert, nur rot markiert; Ergebnis gilt danach als „aktuell" | im Sammler `ErgebnisVeraltet` in die Simulationsbedingung aufnehmen (wirkt auch im Bericht) |
| **K4** | **Verwaiste Ergebnisse:** Persistieren löscht nur die beteiligten Projekte; abgewählte Varianten behalten Alt-Ergebnisse anderer Läufe — Bericht mischt Rechenstände | je Lauf die ganze Stammgruppe löschen oder Lauf-ID mitführen |
| **K5** | **Leistungspreis still ignoriert** (`custom_price_power` gepflegt, nie gelesen) | in W2 verrechnen; bis dahin Hinweis „Leistungspreis nicht berücksichtigt" in Reiter/Bericht |
| **K6** | **BHKW-Einspeisung:** nur `Photovoltaik.Ueberschuss` wird vergütet; BHKW hat kein Stromüberschuss-Feld → BHKW-Varianten systematisch unterbewertet | Feld `Stromueberschuss` in `Tab_ErgebnisBHKW` (StelleXSpaltenSicher-Muster) + Bewertung; bis dahin Warnung bei BHKW-Projekten |
| **K7** | Fehlgrund nur bei fehlenden Energiepreisen; Projekt ohne Kostenpositionen liefert scheinbar valide KW mit I₀=0; `Investition` nicht nullable → „0" statt „—" | Fehlgrund „keine Kostenpositionen", `Investition` nullable, Absprung in `Form_Kosten` (6.2c) |
| **K8** | Stamm-Erkennung hängt an Listenreihenfolge („Stamm zuerst"); scheitert der Stamm, zeigen alle Varianten kommentarlos „—" | Stamm explizit suchen, Fehlgrund an Varianten durchreichen |
| **K9** | ~15 neue Wirtschaftlichkeits-Labels fehlen im `BerichtTexte`-Wörterbuch → englischer Bericht bleibt dort deutsch (LIESMICH-Behauptung „+Übersetzungen" nicht eingelöst) | `_en`-Einträge nachtragen |
| **K10** | Lesepfade der Hintergrund-Berechnung laufen über `DataRepository` (MessageBox aus Worker-Thread möglich); Szenarien ohne gepflegte Best-/Worst-Werte zeigen dreimal dieselbe Zahl ohne Hinweis; keine Plausibilitätsprüfung Worst/Erwartet/Best | stilles Fehlerhandling im Rechenpfad; Hinweis „keine Szenariowerte gepflegt"; Prüfung beim Speichern in `Form_Kosten` |

*Ergänzende Dokument-Korrekturen sind in dieser Fassung 3.1 bereits eingearbeitet
(3.1, 3.4, 3.5, 4, 5.1a, 5.4, 5.5, 6). LIESMICH_Phase1: die Behauptungen „App-Start
legt Tabellen an" (tatsächlich lazy beim ersten Reiter-/Rechenzugriff, harmlos),
„veraltete Simulationen werden nachgerechnet" (K3) und „BerichtTexte +Übersetzungen"
(K9) sind zu korrigieren.*
