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

## 7 Emissions- und Primärenergiefaktoren

### 7.1 Der Rechtsrahmen hat gewechselt

Das Gebäudeenergiegesetz ist zum **Gebäudemodernisierungsgesetz (GModG)**
umbenannt worden — ausgefertigt 23.07.2026, verkündet 28.07.2026 in
**BGBl. 2026 I Nr. 226**. Das Inkrafttreten ist gestaffelt: Die
Heizungstausch-Regeln gelten seit 29.07.2026, die **neuen Anlagen 4 und 9 aber
erst ab 01.01.2027**. Bis dahin gelten die alten Faktoren weiter.

**Für eine Software, die Projekte über zwanzig Jahre rechnet, müssen beide
Faktorensätze parallel mit Gültig-ab-Datum vorliegen.** Vier Gründe:

1. Nachweise bis 31.12.2026 laufen nach altem Recht, ab 01.01.2027 nach neuem.
2. Ein 2026 gerechneter Variantenvergleich muss 2029 dieselben Zahlen liefern.
3. Der Bruch ist groß, nicht kosmetisch — Holz 0,2 → 0,7 (Faktor 3,5), Strom
   560 → 100 g CO₂-Äq/kWh (Faktor 5,6).
4. Der Wegfall des Verdrängungsstrommix ist ein **Methodenwechsel**, kein
   Parameterwechsel (siehe 7.4).

### 7.2 Primärenergiefaktoren (Anlage 4), nicht erneuerbarer Anteil

| Energieträger | bis 31.12.2026 | ab 01.01.2027 |
|---|---|---|
| Heizöl, Erdgas, Flüssiggas, Steinkohle | 1,1 | 1,1 |
| Braunkohle | 1,2 | 1,2 |
| **Strom netzbezogen** | **1,8** | **1,5** |
| Strom gebäudenah (PV, Wind) | 0,0 | 0,0 |
| **Holz, feste Biomasse** | **0,2** | **0,7** |
| Biogas, Biomethan, biogenes Flüssiggas, Bioöl | 1,1 | **0,7** |
| Wasserstoff und Derivate, synthetisches Heizöl | — | **0,7** |
| Fernwärme (Standardwert) | — | **0,7** |
| **Verdrängungsstrommix KWK** | **2,8** | **entfällt ersatzlos** |
| Erdwärme, Solarthermie, Umgebungswärme, Abwärme | 0,0 | 0,0 |

Zwei Sonderregeln bleiben: Für flüssige oder gasförmige Biomasse, die im
unmittelbaren räumlichen Zusammenhang erzeugt wird, darf **0,3** angesetzt werden
(§ 22 Abs. 1 Satz 2). Fernwärme darf unter 0,7 sinken — um **0,002 je
Prozentpunkt** erneuerbarer Anteil, Minimum 0,5 (§ 22 Abs. 6 neu).

### 7.3 Emissionsfaktoren für den Nachweis (Anlage 9), g CO₂-Äquivalent/kWh

| Energieträger | bis 31.12.2026 | ab 01.01.2027 |
|---|---|---|
| Heizöl / Erdgas / Flüssiggas | 310 / 240 / 270 | unverändert |
| Steinkohle / Braunkohle | 400 / 430 | unverändert |
| **Strom netzbezogen** | **560** | **100** |
| Biogas (gebäudenah) | 140 (75) | **80 (70)** |
| Biomethan / biogenes Flüssiggas / Bioöl | 240 / 180 / 210 | **je 80** |
| Holz, feste Biomasse | 20 | 20 |
| **Verdrängungsstrommix** | **860** | **entfällt ersatzlos** |
| Abwärme aus Prozessen | 40 | **10** |
| Fernwärme aus KWK ≥ 70 % (Kohle / gas+flüssig / erneuerbar) | 300 / 180 / 40 | unverändert |
| Fernwärme aus Heizwerken (Kohle / gas+flüssig / erneuerbar) | 400 / 300 / 60 | unverändert |

Neu ab 2027: Für Fernwärme sind Vorkette und Netzverluste zu berücksichtigen —
zulässig ist ein pauschaler Aufschlag von **20 %, mindestens 40 g CO₂-Äq/kWh**.

### 7.4 Der Verdrängungsstrommix entfällt — Folgen für KWK

Bis 31.12.2026 wird eingespeister KWK-Strom mit **2,8** primärenergetisch und
**860 g CO₂-Äq/kWh** gutgeschrieben (Herkunft: DIN V 18599-1:2011-12 über die
EnEV). **Ab 01.01.2027 gibt es keinen amtlichen Verdrängungsfaktor mehr.** Die
Stromgutschriftmethode ist abgeschafft; KWK-Wärme wird stattdessen nach
**DIN EN 15316-4-5:2017-09, Abschnitt 6.2.2.1.6.3** bewertet. Das betrifft den
Rechenweg, nicht nur eine Zahl — die Software braucht beide Wege, umgeschaltet
über dasselbe Gültig-ab-Datum.

Einen amtlichen Ersatz speziell für KWK gibt es nicht. Das Umweltbundesamt
veröffentlicht in CLIMATE CHANGE 11/2026 Substitutionsfaktoren, die aber für
erneuerbaren Strom hergeleitet sind (Photovoltaik: 685 g CO₂-Äq/kWh vermiedene
Emissionen für 2024). Wer ab 2027 eine Gutschrift für eingespeisten KWK-Strom
rechnet, trifft damit eine **methodische Wahl**, keine Rechtsvorgabe — sie gehört
als Auswahlparameter in die Software und in den Bericht.

> **Korrektur zu den Altwerten der Excel-Anwendung:** Der dort als „Nahwärme 2,8"
> geführte Wert ist mit hoher Wahrscheinlichkeit falsch beschriftet — 2,8 war nie
> ein Nahwärmefaktor, sondern immer der Verdrängungsstrommix für KWK. Ebenso
> gehört „Bio-Erdgas 0,5" heute auf **0,3** (§ 22 Abs. 1 Satz 2).

### 7.5 Zwei Faktorensätze, die nie vermischt werden dürfen

Der Nachweiswert „Strom = 100 g CO₂-Äq/kWh" ab 2027 ist **politisch gesetzt**,
nicht physikalisch. Der reale Strommix lag 2025 bei **344 g CO₂/kWh direkt**
beziehungsweise **406 g CO₂-Äq/kWh mit Vorkette** — Faktor 3,4 bis 4,1 darüber.

| Satz | Zweck | Quelle |
|---|---|---|
| **Nachweis** (GEG/GModG Anlage 9) | Energieausweis, gesetzliche Nachweisführung | Anlage 9, stichtagsabhängig |
| **Reale Bilanz** | Wirtschaftlichkeit, CO₂-Kosten, ehrliche Klimabilanz | UBA-Strommix, jährlich fortgeschrieben |

**Diese beiden dürfen im Code nie dieselbe Variable belegen** — sonst rechnet
sich jede Anlage schön.

### 7.6 Strommix Deutschland (reale Bilanz)

Quelle: Umweltbundesamt, CLIMATE CHANGE 16/2026, „Entwicklung der spezifischen
Treibhausgas-Emissionen des deutschen Strommix 1990–2025", März 2026.

| Jahr | CO₂ direkt | THG ohne Vorkette | **THG mit Vorkette** | Status |
|---|---|---|---|---|
| 2020 | 365 | 373 | 435 | gesichert |
| 2021 | 406 | 414 | 477 | gesichert |
| 2022 | 433 | 441 | 503 | gesichert |
| 2023 | 379 | 387 | 442 | gesichert |
| 2024 | 353 | 361 | 414 | vorläufig |
| **2025** | **344** | **352** | **406** | geschätzt |

*(g CO₂ bzw. g CO₂-Äquivalent je kWh)*

Für Wirtschaftlichkeit und Emissionsbilanz ist die Spalte **mit Vorkette**
maßgeblich. Die Reihe wird jährlich im März veröffentlicht, das jüngste Jahr ist
immer geschätzt und wird im Folgejahr revidiert — die Software muss Werte also
auch **rückwirkend korrigieren** können.

**Schadstoffe außer CO₂** sind amtlich nur bis Datenjahr **2021** verfügbar
(UBA-Tabelle, Seitenstand 02.07.2024): SO₂ 0,196 · NO₂ 0,374 · CO 0,174 ·
Staub gesamt 0,00955 · PM10 0,00888 g/kWh. Eine neuere amtliche Quelle je kWh
Strom existiert nicht — diese Werte gehören mit Datenstand gekennzeichnet.

### 7.7 Brennstoff-Emissionsfaktoren

**Rechtsverbindlich für die CO₂-Bepreisung** (EBeV 2030, Anlage 2 Teil 4):

| Brennstoff | t CO₂/TJ | g CO₂/kWh (Hi) |
|---|---|---|
| **Erdgas** | 55,8 | **200,9** — brennwertbezogen **181,4** |
| **Heizöl EL** | 74,0 | 266,4 |
| Heizöl S | 79,7 | 286,9 |
| **Flüssiggas** | 65,5 | 235,8 |
| **Pflanzenöl** (auch Tierfette, Altspeiseöl) | 74,0 | 266,4 |
| Biodiesel | 74,0 | 266,4 |

> **Hi/Ho-Falle beim Erdgas.** Abgerechnet wird in Deutschland brennwertbezogen;
> die Verordnung nennt dafür den Umrechnungsfaktor 3,2508 GJ/MWh. Wer den
> Heizwert-Faktor auf eine brennwertbezogene Menge anwendet, irrt um rund 10 %.

**Biomasse-Nullregel (§ 8 EBeV 2030):** Für den Biomasseanteil darf ein
Emissionsfaktor von **null** angesetzt werden — aber **nur mit anerkanntem
Nachhaltigkeitsnachweis**. Ohne Nachweis gilt der volle fossile Standardwert.
Feste Biomasse, Biogas und Klärgas sind keine BEHG-Brennstoffe und in der EBeV
nicht enthalten.

**Ohne gesetzliche Festlegung** — belastbarster amtlicher Wert ist das
BAFA-Infoblatt zur Bundesförderung Energie- und Ressourceneffizienz, Version 3.4,
Stand 01.06.2026 (heizwertbezogen, g CO₂/kWh): Biogas 152 · Klärgas 50 ·
Deponiegas 50 · Pellets 36 · Holz trocken 27 · Biodiesel 70 · Klärschlamm 10 ·
Fernwärme 280 · Strom 435.

**Inklusive Vorkette** (UBA CLIMATE CHANGE 11/2026, biogenes Verbrennungs-CO₂
definitionsgemäß null, g CO₂-Äq/kWh): Hackschnitzel-Kessel 22,6 · Pellets-Kessel
17,4 · **Rapsöl-BHKW 143,2** · Biogas aus Energiepflanzen im BHKW 154,7 · Biogas
aus Gülle **−39,5** (Güllebonus nach RED II) · Klärgas-BHKW 37,6.

### 7.8 Wo gilt welche Bilanzierungsregel für Biomasse

Das ist die größte Fehlerquelle, weil sich die Regelwerke widersprechen:

| Regelwerk | Biogenes Verbrennungs-CO₂ | anzusetzen ist stattdessen |
|---|---|---|
| **EBeV 2030 / BEHG** | 0, **nur mit Nachhaltigkeitsnachweis** | sonst voller fossiler Standardwert |
| **GEG / GModG Anlage 9** | 0 | Vorkettenwert (Holz 20, Biogas 140 bzw. 80) |
| **UBA-Emissionsbilanz** | 0 | Vorkette + CH₄/N₂O + Hilfsenergie |
| **BAFA EEW** | 0 | Vorkettenwert (Pellets 36, Holz 27) |
| **UBA-CO₂-Rechner** | **nicht 0**, sondern 365 g/kWh | — |

Das Umweltbundesamt benennt diesen Widerspruch selbst. Für ein Planungswerkzeug
heißt das: Die Bilanzierungskonvention gehört als **ausgewiesene Einstellung** in
den Bericht, nicht als stille Annahme in den Code.

---

## 8 CO₂-Preis: nationaler Emissionshandel und EU-ETS 2

### 8.1 Festpreisphase und Versteigerung 2026

| Jahr | Preis | Grundlage |
|---|---|---|
| 2021 / 2022 / 2023 | 25 / 30 / 30 €/t | § 10 Abs. 2 BEHG |
| 2024 / 2025 | 45 / 55 €/t | § 10 Abs. 2 BEHG |
| **2026** | Korridor **55–65 €/t**, **realisiert durchgehend 65,00 €/t** | § 10 Abs. 2 BEHG; EEX-Auktionsdaten |
| 2026 ab 03.11. | 68 €/t (Verkauf, unbegrenzte Menge) | DEHSt |
| 2027 bis 31.08. | 70 €/t (Nachkauf von 2026er-Zertifikaten) | DEHSt |

**Alle sieben Versteigerungen zwischen dem 01.07. und 12.08.2026 endeten am
Höchstpreis von 65,00 €/t** — bei Geboten zwischen 292 und 546 Millionen
Zertifikaten für jeweils rund 21 Millionen zugeteilte. Die Nachfrage übersteigt
das Angebot damit um das 13- bis 26-fache; die Preisobergrenze des Korridors
greift in jeder Auktion. Für die Wirtschaftlichkeitsrechnung heißt das: **2026 ist
mit 65 €/t zu rechnen, nicht mit einem Mittelwert des Korridors.**

Quelle: EEX veröffentlicht die Auktionsergebnisse als wöchentlich aktualisierte
CSV (`nEHS_Auction_Reporting.csv`) — geeignet, um den Wert später automatisiert
nachzupflegen. Die Zahlen sind in sich schlüssig: Menge mal 65 € ergibt den
ausgewiesenen Erlös auf den Cent, und die Restmengen summieren sich exakt auf die
von der Emissionshandelsstelle veröffentlichte Jahresmenge.

### 8.2 Der europäische Emissionshandel startet 2028, nicht 2027

**Gesichert:** Verordnung (EU) 2026/667 vom 11.03.2026 (Amtsblatt vom
18.03.2026) verschiebt den Emissionshandel für Gebäude, Straßenverkehr und
weitere Sektoren auf **2028**. Erste Abgabepflicht ist damit der 31.05.2029 für
das Berichtsjahr 2028; bis einschließlich Berichtsjahr **2027** gilt
ausschließlich der nationale Emissionshandel. Wer noch 2027 als Startjahr nennt,
gibt einen überholten Stand wieder.

**Für 2027 ist die Lage im Umbruch:** Nach geltendem § 10 Abs. 3 BEHG würde ein
marktbasierter Preis gelten, abgeleitet aus dem mengengewichteten
Durchschnittspreis europäischer Versteigerungen des vorletzten Quartals — bei
einem ETS-1-Preis von rund 80 €/t also deutlich über dem bisherigen Korridor.
Der Koalitionsausschuss hat am 12.05.2026 dagegen entschieden, den Korridor von
**55 bis 65 €/t auch 2027** beizubehalten; das Kabinett hat den entsprechenden
Gesetzentwurf am **12.08.2026** beschlossen. **Bundestag und Bundesrat stehen
noch aus** — der Wert ist damit vorläufig, aber politisch gesetzt.

### 8.3 Preisstabilität im ETS 2

Rechtstext: Überschreitet der Preis **45 €/t** (Preisbasis 2020, indexiert),
werden **20 Millionen** Zertifikate aus der Marktstabilitätsreserve freigegeben.
Ein Kommissionsvorschlag vom 27.11.2025 will diese Menge auf 40 Millionen
verdoppeln (bis zu zweimal je Zwölfmonatszeitraum) und die Schwelle über 2029
hinaus verlängern; das Europäische Parlament hat dazu am 29.04.2026 in erster
Lesung Position bezogen. **Ein verkündeter Änderungsbeschluss liegt nicht vor —
das Verfahren läuft.**

### 8.4 Preispfad für eine 20-Jahres-Rechnung

**Gesichert bis einschließlich 2026** (Festpreise und realisierte Auktionen),
**vorläufig für 2027** (Korridor 55–65 €/t, Gesetz im Verfahren), **ab 2028
zwingend Prognose** — dort gibt es keinen Rechtspreis mehr, sondern europäische
Börsenpreisbildung.

Als Vorbelegung geeignet, jeweils mit Quellenausweis:

| Szenario | Werte | Quelle | Bewertung |
|---|---|---|---|
| konservativ | rund 80 €/t konstant | Marktkommentare 2026 | Prognose |
| mittel | 2028: 95 €/t, in Stufen bis 2030: 125 €/t | Projektionsbericht 2026 der Bundesregierung | halbamtlich, **nur sekundär belegt** |
| hoch | Ø 150 €/t für 2027–2032 | Agora Energiewende A-EW_311 | Prognose, auf altem Startjahr gerechnet |

**Einen amtlichen Pfad über 2030 hinaus gibt es nicht.** Der Preispfad gehört
deshalb als frei editierbare Stützstellenreihe in die Software, mit den
gesicherten Werten vorbelegt und einer sichtbaren Kennzeichnung, ab wann Prognose
beginnt.

---

## 9 Quellen (abgerufen 18.08.2026)

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

**Emissions- und Primärenergiefaktoren** — Gebäudemodernisierungsgesetz,
BGBl. 2026 I Nr. 226 vom 28.07.2026 (Regelungstext, Anlagen 4 und 9); GEG
Anlagen 4 und 9 in der bis 31.12.2026 geltenden Fassung; EBeV 2030 Anlage 2
Teil 4 und § 8; Umweltbundesamt CLIMATE CHANGE 16/2026 (Strommix 1990–2025,
März 2026), CLIMATE CHANGE 11/2026 (Emissionsbilanz erneuerbarer Energieträger,
Januar 2026), CLIMATE CHANGE 28/2022 (CO₂-Faktoren fossiler Brennstoffe),
UBA-Tabelle „Spezifische Emissionsfaktoren für den deutschen Strommix"
(Seitenstand 02.07.2024, Datenjahr 2021), UBA-CO₂-Rechner (Methodikumstellung
März 2024); BAFA-Infoblatt CO₂-Faktoren zur Bundesförderung Energie- und
Ressourceneffizienz, Version 3.4, Stand 01.06.2026; DEHSt, nEHS Verkauf und
Versteigerung.

**CO₂-Preis** — EEX-Auktionsdaten zum nationalen Emissionshandel (CSV,
wöchentlich aktualisiert); DEHSt, „nEHS Verkauf und Versteigerung" sowie
„EU-ETS 2 Ausblick"; Verordnung (EU) 2026/667 (Amtsblatt 18.03.2026);
Richtlinie 2003/87/EG Kapitel IVa und Beschluss (EU) 2015/1814;
Kommissionsvorschlag COM(2025) 738 vom 27.11.2025; § 10 BEHG;
Referentenentwurf und Kabinettsbeschluss zum Dritten BEHG-Änderungsgesetz
(03.07. bzw. 12.08.2026); Projektionsbericht 2026 der Bundesregierung;
Agora Energiewende A-EW_311.

---

## 10 Punkte, die bewusst offen geblieben sind

Diese Angaben ließen sich nicht aus einer Primärquelle sichern und wurden
deshalb **nicht** übernommen — lieber eine markierte Lücke als ein falscher Wert:

1. **Auslösedauer des 45-Euro-Mechanismus** im ETS 2 — zwei oder drei
   aufeinanderfolgende Monate; die Sekundärquellen widersprechen sich, der
   konsolidierte Richtlinientext war nicht auslesbar.
2. **Exakter Wortlaut von § 10 Abs. 3 BEHG** (Bezugspreis 2027) — nur über
   Sekundärportale gesichert; die Zuordnung „Berechtigungen" gegenüber
   „Emissionszertifikaten" ist auch fachlich strittig. Praktisch entschärft, falls
   das Dritte Änderungsgesetz den Korridor festschreibt.
3. **Zahlenreihe des Projektionsberichts 2026** — nur sekundär belegt, das
   Original überschritt das Abruflimit. Vor Verwendung als Vorbelegung prüfen.
4. **Enddatum der Versteigerungsphase 2026** — die Restmengenrechnung deutet auf
   Ende August, eine Sekundärquelle nennt den 09.09.2026.
5. **§ 53a Abs. 3 EnergieStG, Erdgassatz 4,96 €/MWh** (siehe Abschnitt 6).
