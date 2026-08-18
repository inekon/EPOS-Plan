# Grundlagen: KWK-Gesetz, Energiesteuer, Stromsteuer

**Rechtsstand: 18.08.2026.** Alle Quellen an diesem Tag abgerufen. Dieses Dokument
ist die Faktenbasis für
[`Konzept_BHKW_Kosten_Erloese.md`](WindowsFormsApplication1/Allgemein/Reporting/Konzept_BHKW_Kosten_Erloese.md)
und für die Pflege des Katalogs `Tab_Gesetzesparameter`.

> **Wartungshinweis.** Diese Zahlen ändern sich häufig. Sie gehören deshalb
> **nicht** in den Programmcode, sondern in den Parameterkatalog mit „gültig ab
> Jahr". Dieses Dokument dokumentiert Herkunft und Stand — bei einer Novelle wird
> hier der Abschnitt ergänzt (nicht überschrieben) und im Katalog eine neue
> Jahreszeile angelegt.

---

## 1 KWK-Gesetz

### 1.1 Fassung und Bezeichnung

| | |
|---|---|
| Amtlicher Titel | Gesetz für die Erhaltung, die Modernisierung und den Ausbau der Kraft-Wärme-Kopplung (**KWKG 2025**) |
| Ausfertigung | 21.12.2015, BGBl. I S. 2498 |
| Zuletzt geändert | Art. 24 des Gesetzes vom 18.12.2025, BGBl. 2025 I Nr. 347, in Kraft 23.12.2025 |
| Umbenennung | Das KWKG-Änderungsgesetz (in Kraft 01.04.2025) hat die Kurzbezeichnung von „KWKG 2023" auf „KWKG 2025" geändert |

„KWKG 2016 / 2020 / 2023 / 2025" sind **keine verschiedenen Gesetze**, sondern
Zitierbezeichnungen desselben Stammgesetzes von 2015. Welche Fassung für welches
Inbetriebnahmedatum gilt, regelt § 35 (Übergangsbestimmungen): Inbetriebnahme bis
31.12.2016 → Fassung 2016, bis 13.08.2020 → Fassung 2020, bis 31.03.2025 →
Fassung vom 31.03.2025.

### 1.2 Geltungsdauer — die Förderung endet nicht Ende 2026

§ 6 Abs. 1 verlangt Aufnahme des Dauerbetriebs bis **31.12.2026**. Die Novelle
2025 hat eine Ausnahme eingezogen: Anlagen, die bis zum 31.12.2026 über eine
BImSchG-Genehmigung verfügen **oder verbindlich beauftragt** wurden, dürfen bis zu
**vier Jahre später** in Dauerbetrieb gehen (also bis Ende 2030) und bleiben
zuschlagsberechtigt. Für Wärme-/Kältenetze und -speicher gilt Analoges.

Der beihilferechtliche Genehmigungsvorbehalt für Anlagen > 300 MW wurde zum
01.04.2025 aufgehoben.

### 1.3 Zuschlagssätze (§ 7)

**Eingespeister KWK-Strom (Abs. 1):**

| Leistungsanteil | ct/kWh |
|---|---|
| bis 50 kW | 8 |
| > 50 bis 100 kW | 6 |
| > 100 bis 250 kW | 5 |
| > 250 kW bis 2 MW | 4,4 |
| > 2 MW (neu/modernisiert) | 3,4 |
| > 2 MW (nachgerüstet) | 3,1 |

**Sonderregel neue Kleinanlagen (Abs. 3a)** — geht Abs. 1 und 2 vor:
**16 ct/kWh** für eingespeisten und **8 ct/kWh** für nicht eingespeisten Strom
aus **neuen** Anlagen bis 50 kWel.

**Selbst genutzter Strom (Abs. 2)** — nur in den drei Fällen des § 6 Abs. 3:

| Leistungsanteil | Nr. 1 (Anlagen ≤ 100 kW) | Nr. 2 (Kundenanlage / geschl. Verteilernetz) | Nr. 3 (stromkostenintensiv) |
|---|---|---|---|
| bis 50 kW | 4 | 4 | 5,41 |
| > 50 bis 100 kW | 3 | 3 | 4 (bis 250 kW) |
| > 100 bis 250 kW | — | 2 | 4 |
| > 250 kW bis 2 MW | — | 1,5 | 2,4 |
| > 2 MW | — | 1 | 1,8 |

*(ct/kWh)*

**Wichtig:** Ein Zuschlag auf selbst genutzten Strom besteht **nicht generell**,
sondern nur bei Erfüllung eines der drei Tatbestände des § 6 Abs. 3.

### 1.4 Dauer der Zuschlagszahlung (§ 8)

**Neue Anlagen (Abs. 1): 30.000 Vollbenutzungsstunden.** Die früher für Anlagen
≤ 50 kW geltenden 60.000 Vbh gibt es seit dem KWKG 2020 nicht mehr — die Dauer
wurde halbiert und die Sätze im Gegenzug verdoppelt (8 → 16 bzw. 4 → 8 ct/kWh).

**Modernisierte Anlagen (Abs. 2):**

| Vbh | Kostenschwelle (Anteil Neuherstellungskosten) | Mindestabstand zur Inbetriebnahme |
|---|---|---|
| 6.000 | ≥ 10 % | 2 Jahre — nur Dampfsammelschienen-KWK > 50 MW |
| 15.000 | ≥ 25 % | 5 Jahre |
| 30.000 | ≥ 50 % | 10 Jahre |

Kosten einer Umstellung auf Wasserstoff werden nicht angerechnet.

**Nachgerüstete Anlagen (Abs. 3):** 10.000 Vbh (10 bis < 25 %), 15.000 Vbh
(25 bis < 50 %), 30.000 Vbh (≥ 50 %).

**Jahresdeckel (Abs. 4)** — der Zuschlag wird je Kalenderjahr höchstens für so
viele Vollbenutzungsstunden gezahlt:

| ab Kalenderjahr | 2021 | 2023 | 2025 | 2026 | 2027 | 2028 | 2029 | 2030 |
|---|---|---|---|---|---|---|---|---|
| max. Vbh/a | 5.000 | 4.000 | 3.500 | **3.300** | 3.100 | 2.900 | 2.700 | 2.500 |

Der Einleitungssatz nennt keine Leistungsgrenze, der Deckel gilt also für alle
Anlagen. Folge: 30.000 Vbh lassen sich frühestens in etwa 10 bis 12 Kalenderjahren
ausschöpfen; ein Grundlast-BHKW mit 6.000 Betriebsstunden erhält 2026 nur für
3.300 h Zuschlag.

**Sonderregel ≤ 2 kWel (§ 9):** auf Antrag pauschale Vorauszahlung von
**4 ct/kWh für 60.000 Vbh**, Auszahlung binnen zwei Monaten; damit entfällt die
Einzelabrechnung.

**Eine zeitliche Höchstdauer in Jahren gibt es nicht** — die Begrenzung wirkt
ausschließlich über Vollbenutzungsstunden (Kontingent plus Jahresdeckel).

### 1.5 Verfahren

Einmalige Zulassung beim BAFA, seit dem KWKG 2025 **zwingend elektronisch** über
das DKWKG-Antragsportal (Post und E-Mail nicht mehr zulässig). Frist: 31.12. des
auf die Aufnahme des Dauerbetriebs folgenden Kalenderjahres (Ausschreibungssegment
12 Monate). Die Abrechnung läuft danach laufend über den Netzbetreiber.

---

## 2 Stromsteuer (StromStG)

Fassung des Dritten Gesetzes zur Änderung des Energiesteuer- und des
Stromsteuergesetzes vom 22.12.2025 (BGBl. 2025 I Nr. 340), gültig ab 01.01.2026.

| Sachverhalt | Wert |
|---|---|
| Regelsteuersatz (§ 3) | **20,50 €/MWh** (2,05 ct/kWh) |
| Entlastung produzierendes Gewerbe (§ 9b) | **20,00 €/MWh** |
| verbleibende Belastung | 0,50 €/MWh (EU-Mindestsatz) |
| Sockelbetrag (§ 9b) | **250 €/Kalenderjahr** — entspricht 12,5 MWh/a |
| Antrag | Formular 1453, Frist 31.12. des Folgejahres |

Die 2024/2025 befristete Absenkung wurde zum 01.01.2026 **dauerhaft** ins Gesetz
übernommen (unter Vorbehalt der AGVO-Freistellungsanzeige).

**§ 10 Spitzenausgleich: ausgelaufen zum 31.12.2023, ersatzlos.** Er wurde durch
die erhöhte § 9b-Entlastung ersetzt.

### 2.1 Steuerbefreiung für KWK-Strom (§ 9 Abs. 1 Nr. 3)

Befreit ist Strom aus **hocheffizienten KWK-Anlagen bis 2 MW elektrischer
Nennleistung**, der vom Betreiber als Eigenerzeuger im räumlichen Zusammenhang
selbst verbraucht oder an Letztverbraucher im räumlichen Zusammenhang geleistet
wird.

- **Räumlicher Zusammenhang:** Radius bis **4,5 km** um die Erzeugungseinheit
  (§ 12b StromStV — steht in der Verordnung, nicht im Gesetz).
- **Neu ab 2026:** „hocheffizient" ist in § 2 StromStG legaldefiniert über
  Anhang III der Richtlinie (EU) 2023/1791. Bei **fossil** betriebenen Anlagen
  müssen die direkten CO₂-Emissionen zusätzlich **unter 270 g je kWh
  Energieertrag** liegen. Erdgas-BHKW erfüllen das in der Regel, Heizöl-BHKW
  eher nicht.
- Erlaubnisschwelle für Anlagenbetreiber: **1 MWel**.

Die Befreiung ist ein **Erlaubnis-, kein Antragstatbestand** — sie wirkt laufend,
nicht als Jahresrückerstattung.

---

## 3 Energiesteuer (EnergieStG)

### 3.1 Steuersätze für Heizstoffe (§ 2 Abs. 3 Satz 1) — Bemessungsgrundlage

| Energieerzeugnis | Fundstelle | Satz | Einheit |
|---|---|---|---|
| Gasöl/Heizöl EL, S ≤ 50 mg/kg | Nr. 1 Buchst. a | **61,35 €** | je 1.000 **Liter** |
| Gasöl, S > 50 mg/kg | Nr. 1 | 76,35 € | je 1.000 Liter |
| Schweröl | Nr. 2 | 25,00 € | je 1.000 kg |
| **Erdgas** | Nr. 4 | **5,50 €** | je **MWh** |
| **Flüssiggas** | Nr. 5 | **60,60 €** | je 1.000 **kg** |

Diese Sätze sind seit 2003 unverändert.

> **Einheitenfalle.** Die drei Träger stehen auf **drei verschiedenen
> Bezugsgrößen**. Genau deren Vermischung ist die Ursache des Fehlers
> „61,35 €/MWh Öl" in der Altanwendung. Sätze immer in der gesetzlichen Einheit
> speichern, Umrechnung ausschließlich über die gepflegten Heizwerte.

### 3.2 § 53 — Entlastung für die Stromerzeugung (ab 2026 vorrangig)

Der Wortlaut ab 01.01.2026 nimmt nur noch Strom aus, der nach **§ 9 Abs. 1
Nr. 4, 5 oder 6** StromStG befreit ist. **Nummer 1 und Nummer 3 sind gestrichen
worden** — das bisherige Kumulierungsverbot ist damit entfallen: Ein BHKW bis
2 MW kann ab dem Entlastungsabschnitt 2026 **gleichzeitig** die
Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3 StromStG **und** die vollständige
Energiesteuerentlastung nach § 53 in Anspruch nehmen.

- **Umfang:** voller Steuersatz nach § 2, aber **nur für den auf die
  Stromerzeugung entfallenden Brennstoffanteil**, nicht für den Wärmeanteil.
- Erfasst sind nur Erzeugnisse, die unmittelbar am Umwandlungsprozess teilnehmen
  (Zusatzfeuerungen, Dampferzeuger ohne Stromnutzen, Abluftbehandlung sind
  ausgenommen).
- Antrag: Formular 1131, ab 2026 mit Betriebserklärung 1131a/1131az. Kein
  Sockelbetrag.

### 3.3 § 53a — teilweise Entlastung für die gekoppelte Erzeugung

Voraussetzung: Monats- oder Jahresnutzungsgrad **mindestens 70 %**. Ab 2026
beginnt Abs. 1 mit „Vorbehaltlich des § 53" — die Norm ist zur **Auffangregelung**
geworden.

**Abs. 5 — Gasturbinen und Verbrennungsmotoren, also der für Motor-BHKW
einschlägige Absatz:**

| Energieerzeugnis | Betrag | Einheit | Anteil am vollen Satz |
|---|---|---|---|
| Heizöl EL | **40,35 €** | je 1.000 Liter | 66 % |
| Schweröl | 4,00 € | je 1.000 kg | 16 % |
| **Erdgas** | **4,42 €** | je **MWh** | 80 % |
| **Flüssiggas** | **19,60 €** | je 1.000 kg | 32 % |
| Kohle | 0,16 € | je GJ | — |

Abs. 2 (allgemein, ohne Motorbezug): Heizöl 40,35 €/1.000 l · Schweröl
10,00 €/1.000 kg · Erdgas 4,42 €/MWh · Flüssiggas 60,60 €/1.000 kg.

**Absätze 6, 7 und 8 sind aufgehoben** — die vollständige Energiesteuerentlastung
für hocheffiziente, noch nicht abgeschriebene KWK-Anlagen ist zum 31.12.2023
entfallen und war 2024/2025 nicht verfügbar. Antrag: Formular 1135.

**§ 53b: weggefallen.**

### 3.4 § 54 — Heizstoffe im produzierenden Gewerbe (Kesselvergleich)

Heizöl 15,34 €/1.000 l · Erdgas 1,38 €/MWh · Flüssiggas 15,15 €/1.000 kg,
Sockelbetrag 250 €/Kalenderjahr.

---

## 4 Zusammenspiel

| Konstellation | Status ab 2026 |
|---|---|
| § 53 EnergieStG ↔ § 9 Abs. 1 **Nr. 1 / Nr. 3** StromStG | **kombinierbar** (Kumulierungsverbot entfallen) |
| § 53 EnergieStG ↔ § 9 Abs. 1 Nr. 4, 5, 6 StromStG | schließen sich aus |
| § 53 ↔ § 53a EnergieStG | § 53 vorrangig; § 53a nur, soweit kein Anspruch nach § 53 |
| § 53a Abs. 5 ↔ § 54 EnergieStG | ausgeschlossen |
| § 9 Abs. 1 Nr. 3 ↔ § 9b StromStG | Befreiung geht vor und ist günstiger; § 9b bleibt für den **Netzbezug** |
| KWKG-Zuschlag ↔ Steuerentlastungen | voneinander unabhängig |

**Jährlich zu beantragen** (also als laufende Gutschrift zu führen):

| Entlastung | Formular | Frist | Sockel |
|---|---|---|---|
| § 9b StromStG | 1453 | 31.12. Folgejahr | 250 €/a |
| § 53 EnergieStG | 1131 (+1131a) | 31.12. Folgejahr | — |
| § 53a EnergieStG | 1135 (+1135a) | 31.12. Folgejahr | — |
| § 54 EnergieStG | 1450 | 31.12. Folgejahr | 250 €/a |

**Abhängigkeiten:** Anlagengröße (2 kW / 50 kW / 100 kW / 250 kW / 1 MW / 2 MW /
50 MW), Nutzungsgrad ≥ 70 %, Hocheffizienz mit CO₂-Grenzwert < 270 g/kWh,
räumlicher Zusammenhang 4,5 km, Inbetriebnahmedatum (KWKG-Fassung und Vbh),
Unternehmensart (produzierendes Gewerbe, Land- und Forstwirtschaft).

---

## 5 Gegenüberstellung: Werte der Altanwendung

| Wert in `BHKW-WP-PLAN.XLSM` | heutiger Wert | Bewertung |
|---|---|---|
| Energiesteuer 5,50 €/MWh Gas | 5,50 €/MWh, als volle Entlastung § 53 wieder erreichbar, aber **nur auf den Stromanteil** | Zahl korrekt, Bezugsbasis fehlt |
| 61,35 €/MWh Öl | 61,35 € je **1.000 Liter**; § 53a Abs. 5: 40,35 €/1.000 l | **Einheit falsch**, Faktor ≈ 10 |
| 4,40 €/MWh Flüssiggas | Flüssiggas wird je 1.000 kg besteuert (60,60 voll / 19,60 nach § 53a) | **nicht zuordenbar** |
| Stromsteuer −0,50 €/MWh | 0,50 €/MWh ist die **Restbelastung**, nicht die Erstattung (20,00 €/MWh) | konzeptionelles Missverständnis |
| Sockel 250 € ≈ 49 MWh bei 5,13 €/MWh | 250 € gilt weiter, entspricht bei 20,00 €/MWh aber **12,5 MWh/a** | Sockel unverändert, Satz veraltet |
| KWK-Zuschlag Eigenstrom 0,08 €/kWh | § 7 Abs. 3a, gilt für **neue Anlagen ≤ 50 kW**, nicht eingespeist | gültig, Geltungsbereich präzisieren |
| „KWK-Gesetz gültig bis 2026" | Dauerbetrieb bis 31.12.2026 **oder** Genehmigung bis dahin + 4 Jahre | teilweise überholt |
| 60.000 Vbh bis 2 kWel pauschal | § 9 KWKG: 4 ct/kWh × 60.000 Vbh | unverändert |
| 60.000 Vbh bis 50 kWel | § 8 Abs. 1: **30.000 Vbh** für alle neuen Anlagen | **veraltet** |
| 30.000 Vbh ab 50 kWel | gilt für alle neuen Anlagen | unverändert |
| modernisiert 25 % / 50 %, nach 5 / 10 Jahren | § 8 Abs. 2: 15.000 / 30.000 Vbh, zusätzlich 6.000 Vbh (≥ 10 %, 2 J., nur Dampfsammelschienen > 50 MW) | im Kern unverändert |
| nachgerüstet 10.000 / 15.000 / 30.000 Vbh | identisch | unverändert |
| *(fehlt)* | **Jahresdeckel** § 8 Abs. 4 | wirtschaftlich erheblich |
| *(fehlt)* | **Stromsteuerbefreiung § 9 Abs. 1 Nr. 3** | größter Einzelposten für Eigenstrom |
| *(fehlt)* | Wegfall des Kumulierungsverbots ab 01.01.2026 | Systemwechsel |

---

## 6 Punkte mit verbleibender Unsicherheit

1. **§ 53 (Stromanteil) neben § 53a (Wärmeanteil)** — der Wortlaut
   („vorbehaltlich") spricht für eine anteilige Aufteilung, die Kommentarliteratur
   durchgängig für ein Entweder-oder. Entscheidet über die Größenordnung der
   Gutschrift; vor produktivem Einsatz mit dem Hauptzollamt klären. **Bis dahin
   als konfigurierbare Option modelliert.**
2. **§ 53a Abs. 3, Erdgassatz 4,96 €/MWh** für das produzierende Gewerbe liegt
   über dem allgemeinen Satz von 4,42 €/MWh — am Volltext gegenlesen.
3. **Ausschluss fossiler flüssiger Brennstoffe** aus der KWKG-Förderung: nur aus
   Sekundärquelle (BBH-Blog 13.02.2025). Falls zutreffend, entfällt der Zuschlag
   für Heizöl-BHKW vollständig.
4. **EuGH-Urteil vom 09.07.2026** zur Beihilfeeigenschaft des KWKG: nur aus einer
   Suchzusammenfassung, kein Primärbeleg.
5. **Nachfolgeregelung nach 2030** existiert derzeit nicht. Der Förderzeitraum
   gehört deshalb als Datumsparameter in den Katalog, nicht als Konstante.

---

## 7 Quellen (abgerufen 18.08.2026)

**Gesetzestexte** — gesetze-im-internet.de: KWKG 2025 §§ 7, 8, 9 und Stammfassung;
StromStG §§ 2, 3, 9; StromStV § 12b; EnergieStG §§ 2, 53, 53a, 53b (weggefallen),
54. Fassungshistorie und §§ 6, 35 KWKG über buzer.de.

**Behörden** — BAFA: Zulassung von KWK-Anlagen, Pressemitteilung zur
KWKG-Änderung zum 01.04.2025. Zoll: Steuerentlastung nach § 9b StromStG,
Steuerentlastung für die Stromerzeugung, teilweise Steuerentlastung für
KWK-Anlagen.

**Fachkommentare** — Energie und Recht (17.03.2026), Baker Tilly (27.01.2026),
EY (28.10.2025), LHM Energiesteuer (04.01. und 18.01.2026), BBH-Blog
(13.02.2025), BHKW-Infozentrum (05.01.2024).
