Alle drei Dokumente sind vollständig gelesen (60 + 40 + 11 Seiten). Hier der Fachbericht.

---

# Auswertung DIN EN 12831-3 (Hauptnorm 2017-09), E DIN EN 12831-3/A100:2021-09, E DIN EN 12831-3/A1:2021-04

**Zweck:** interne Parametrierung des TWW-Zapfprofil-Moduls in WP-Plan.
**Urheberrechtshinweis:** Die nachfolgend wiedergegebenen Tabellen stammen aus urheberrechtlich geschützten DIN/CEN-Dokumenten (© DIN e. V. / © CEN, Alleinvertrieb Beuth Verlag). Wiedergabe ausschließlich zur internen technischen Auswertung bei INEKON; keine Weitergabe, keine Veröffentlichung, keine Aufnahme in an Dritte gelieferte Software-Dokumentation ohne Lizenz.

**Quellendateien:**
- `/root/.claude/uploads/abe216b5-16df-50de-9ac2-cba4717d4cac/e048def0-DIN_EN_128313__DIN.pdf` (Hauptnorm, 60 S.)
- `/root/.claude/uploads/abe216b5-16df-50de-9ac2-cba4717d4cac/884d6671-DIN_EN_128313_A100_Entwurf__DIN_EN.pdf` (A100, 40 S.)
- `/root/.claude/uploads/abe216b5-16df-50de-9ac2-cba4717d4cac/cb8983d0-DIN_EN_128313_A1_Entwurf__DIN_EN.pdf` (A1, 11 S.)

Seitenangaben = gedruckte Normseiten (nicht PDF-Blattnummern).

---

## 0. Einordnung und Status

| Dokument | Stand | Status | Wirkung |
|---|---|---|---|
| DIN EN 12831-3 | 2017-09 (EN 12831-3:2017) | gültige Norm, Ersatz für DIN EN 15316-3-1:2008-06 | Hauptverfahren, Anhang A (normativ, Muster) + Anhang B (informativ, Vorgabewerte) |
| E DIN EN 12831-3/A100 | 2021-09, Einsprüche bis 2021-10-13 | **Norm-Entwurf**, nationaler Anhang NA (normativ) | ersetzt Anhang B durch nationale Werte; liefert 18 deutsche Referenz-Bedarfsprofile |
| E DIN EN 12831-3/A1 | 2021-04 (EN 12831-3:2017/prA1:2021) | **Norm-Entwurf** (CEN-Umfrage) | 3 punktuelle Korrekturen an der Hauptnorm |

Wichtig für die Software-Doku: A100 und A1 sind **Entwürfe** („Weil die beabsichtigte Norm von der vorliegenden Fassung abweichen kann, ist die Anwendung dieses Entwurfs besonders zu vereinbaren." — Anwendungswarnvermerk, jeweils Titelblatt). Ein WP-Plan-Modul, das A100-Werte verwendet, muss dies kennzeichnen.

Nationales Vorwort der Hauptnorm (S. 2) enthält den Sprengsatz: *„Derzeit ist das Normenpaket des EPBD-Mandats M/480 … in Deutschland nicht für die Zwecke des Energieeinsparrechts anwendbar."* → EN 12831-3 ist ein **Auslegungs-/Dimensionierungsverfahren**, kein GEG-Nachweisverfahren.

---

## 1. Verfahren der Bedarfsermittlung

### 1.1 Verfahrensübersicht

Die Norm trennt sauber zwei Aufgabenstellungen (Abschn. 5, S. 15–16):

**A) Bemessung der Anlage (Abschn. 5.1, 6.4) — „Verfahren der Summenkennlinie"**
Das einzige Bemessungsverfahren der Norm. Grafisch/numerischer Vergleich zweier kumulierter Kennlinien über i. d. R. 24 h:
- *Bedarfskennlinie* (3.10, S. 12): kumulierter Energiebedarf
- *Versorgungskennlinie* (3.11, S. 13): kumulierte bereitgestellte Energie inkl. Verluste
- *Restkapazitätslinie* (3.12): kumulierte nutzbare Speicherenergie am Einschaltpunkt

**B) Bestimmung des Energiebedarfs (Abschn. 5.2, 6.5) — vier alternative Verfahren:**

| Nr. | Abschnitt | Verfahren | Kernformel |
|---|---|---|---|
| B1 | 6.5.1, S. 37 | Energiebedarf aus **Abzapf-/Lastprofilen** (24-h-Zyklen; für EFH auch Zapfprogramme nach EN 13203-2) | — |
| B2 | 6.5.2, S. 38–39 | Energiebedarf aus **erforderlichem Volumen** (personen-/einheitenbezogen) | Gl. (19), (20), (21) |
| B3 | 6.5.3, S. 40 | **Flächenbezogener** Energiebedarf (lineare Beziehung zur Grundfläche) | Gl. (22) |
| B4 | 6.5.4, S. 40 | **In Tabellenform** angegebener Energiebedarf (nach Gebäudetyp/Tätigkeit/Klasse) | — |

Ein „Kennzahlverfahren" im Sinne der DIN 4708 (Bedarfskennzahl *N*, Leistungskennzahl *N_L*) **existiert in EN 12831-3 nicht**. Abschn. 5.1 a) 2) nennt lediglich „auf der Grundlage statistischer Verfahren (Kenn-Bedarf)" als eine von drei Möglichkeiten der Warmwasserbedarfs-Bestimmung, ohne sie auszuführen. Die Brücke zu DIN 4708 schlägt erst A100 (siehe Abschn. 4).

### 1.2 Vollständiger Rechengang Summenlinienverfahren

**Schritte nach 5.1 a)–f) (S. 15–16):**

- a) Warmwasserbedarf bestimmen — (1) Messung des Volumenstroms auf Minutenbasis + Warm-/Kaltwassertemperaturen, (2) statistische Verfahren, (3) veröffentlichte/akzeptierte Kenn-Lastprofile (national festzulegen; ersatzweise Anhang B)
- b) Bedarfskennlinie berechnen und darstellen
- c) Auslegungsparameter definieren: Anlagenauswahl, Typ + Leistung der Wärmequelle, Wärmeverluste Speicher/Verteilung
- d) Dimensionierung — Versorgungskennlinie berechnen, wahlweise beginnend mit (1) Energie aus verfügbarer Erzeugerleistung, (2) Speichervolumen (aus Bedarfsspitzen oder täglichem/halbtäglichem/stündlichem Energiebedarf), (3) Ausgangswert aus der **mittleren Steigung der Bedarfskennlinie**
- e) fehlenden Parameter (Volumen **oder** Leistung) durch Änderung der Versorgungskennlinie festlegen
- f) Optimierung mit Herstellerdaten und weiteren Randbedingungen (beschränkte Zeiträume, Arbeitszyklen, **hygienische Aspekte**)

**Auslegungskriterium (5.1, S. 15) — zwei Fälle:**
- *Ladespeichersysteme (minimaler Mischbereich):* Versorgungskennlinie darf die Bedarfskennlinie **nicht schneiden bzw. unterschreiten** → Q_sto,min = 0 (6.4.2.4.2, S. 29)
- *Gemischte Speichersysteme (ausgeprägter Mischbereich):* Versorgungskennlinie liegt **stets oberhalb** der Bedarfskennlinie und hält dabei einen **Mindestabstand Q_sto,min** ein

**Schrittweite:** „Beide Linien werden mit einem Zeitschritt von 1 min ermittelt" (5.1). Abschn. 6.2 (S. 17): „Der Zeitschritt der Berechnung für die Bemessung … beträgt eine Minute." Abschn. 6.4.1 (S. 18): **1 440 Kreisläufe je Tag**. Stundenbasierte Eingangsdaten werden „in gleich lange Minutenintervalle unterteilt" (5.1) bzw. per Gl. (3) umgerechnet.

**Bestimmung des Wertepaares (V_sto, Φ_eff) — Kernaussage 6.4.3.3 / Bild 14 (S. 36):**
Der Algorithmus ist ein **Nachweis-/Prüfalgorithmus**, kein Direktlöser: Für ein gegebenes Paar (Φ_eff, V_sto) wird die Versorgungskennlinie minutenweise aufgebaut. Dann:

> „Beträgt der Unterschied zwischen der Versorgungskennlinie und der Bedarfskennlinie weniger als Q_sto;min (Q_sto;min = 0 für Ladesysteme), ist die Anlage … nicht in der Lage, den … Bedarf zu decken. In diesem Fall muss entweder die Leistung des Wärmeerzeugers oder das Volumen des Speichers … so lange erhöht werden, bis die Bedingung des Ablaufdiagramms erfüllt ist." (S. 36)

→ Die **Wertepaar-Kurve** (Leistung über Volumen) entsteht durch wiederholte Anwendung dieses Nachweises unter Parametervariation. Die Norm beschreibt diese Kurve nicht explizit, verbietet sie aber auch nicht — sie ist die logische Konsequenz von 5.1 d)/e).

**Ausgangswerte, wenn Φ_eff und/oder V_sto unbekannt (6.4.3.4, S. 36; B.3.5, S. 54):**
- Φ_eff,start = **mittlere Steigung der Bedarfskennlinie** (Bild B.1)
- V_sto,start = **vertikaler Abstand zwischen den beiden die Bedarfskennlinie einhüllenden Parallelen** mit dieser Steigung (Bild B.2)
- A100 NA.5.4.8 ergänzt die Umrechnung: dieser vertikale Abstand ist Q_sto; daraus V_sto = Q_sto/(ρ_w·c_w·(θ_sto,max − θ_w,c)) — Gl. (NA.5)

**Berücksichtigung von Verlusten:**
| Verlustart | Gleichung | Bemerkung |
|---|---|---|
| Speicher-Bereitschaftsverlust | Gl. (6) 6.4.2.5, S. 29 | aus q_sb,sto (Herstellerdaten n. EN 12897, alternativ „S" nach VO (EU) 812/2013 via Gl. (7)); **= 0 bei Durchflussanlagen** |
| Verteilungsverluste (detailliert) | Gl. (8) 6.4.2.6, S. 30 | Summe über Rohrabschnitte, U_dis·l_dis·Δθ |
| Verteilungsverluste (vereinfacht) | Gl. (9) | q'_dis · l_dis |

Ausdrücklich gilt (6.4.2.6, S. 30): Es werden **nur Abschnitte mit Umwälzkreisläufen** (Zirkulation) berücksichtigt, einschließlich der Laderohre Erzeuger↔Speicher. **Stichleitungen** („gelegentlich genutzte Entnahmeleitungen") werden **vernachlässigt** — und sind auch in den Bedarfsprofilen des Anhang B und im Tagesbedarf nach Abschn. 6 nicht enthalten. Das ist für WP-Plan eine relevante Modellgrenze: Stichleitungsverluste müssen ggf. als eigener Zuschlag ergänzt werden.

**Bereitschaftsteil / Totvolumen** — die Norm modelliert das über vier Mechanismen:
1. **Ladungsfaktor f_l** (6.4.2.3.2, S. 27–28; Tab. B.7): „gewöhnlich nicht möglich, das gesamte Speichervolumen zu erhitzen"; f_l = 0,96 / 0,94 / 0,90 / 1,0 (s. u.)
2. **Q_sto,min** (Gl. 5, 6.4.2.4.1, S. 28): Mindest-Restkapazität im gemischten Speicher, damit während der Nachladung die Zapftemperatur gehalten wird; enthält den Term (1 − h_sensor/(2·h_sto))
3. **Q_sto,on** (Gl. 10, 6.4.2.7.1, S. 31): Restkapazität am Einschaltpunkt = Q_sto,max·(1 − h_sensor/h_sto)
4. **Bivalente Anlagen** (S. 28): „darf nur das Volumen des Speichers im Bereitschaftsbetrieb … verwendet werden"; bei unbekanntem Volumen = Volumen oberhalb der **Unterkante des oberen Wärmeübertragers**

**Temperaturen im Bemessungsverfahren:**
- θ_w,sto,max — max. Speichertemperatur für Planungszwecke (Tab. 6, S. 18; Quelle: Nationaler Anhang / Anhang B)
- θ_w,draw — Mischwassertemperatur an der Zapfstelle (**Standard 42 °C**, Tab. B.6; **A100: 45 °C**, Tab. NA.5)
- θ_w,c — Kaltwassertemperatur (**10 °C**, Tab. B.6 und Tab. NA.5); Herkunft laut Tab. Abschn. 6.3.4 „Von M1-13"
- θ_ch,HG — Ladetemperatur/Versorgungstemperatur des Wärmeerzeugers (Gl. 14, 15)
- θ_a — Umgebungstemperatur des Speichers bzw. des Rohrabschnitts
- θ_m ≈ **50 °C** — Standardwert mittlere Innentemperatur der Verteilungsrohre (B.3.3, S. 52)

---

## 2. Zapfprofile / Bedarfsprofile

### 2.1 Anhang A (normativ) — nur Muster, keine Werte

Anhang A enthält **leere Tabellenraster** (Tab. A.1 bis A.7, S. 42–45) als verbindliches Format für nationale Vorgabewerte. Tab. A.1 „Zapfprofile, nationale Vorgabewerte" hat Zeilen 00:00–24:00 in Stundenschritten und Spalten „Einfamilienhaus / Mehrfamilienhaus / … / … / …" — alle Zellen sind mit „…" gefüllt. **Anhang A liefert also keine Zahlenwerte.** A.2 (S. 42) fordert: „Vorgabewerte für die Menge an gezapftem Warmwasser müssen auf nationaler Ebene so angegeben werden, dass Berechnungen mit Zeitschritten von 1 h möglich sind."

### 2.2 Anhang B, Tabelle B.1 (S. 46) — Lastprofile nach EN 50440 (XXS…XXL)

Stundenbasierte relative Bedarfswerte, **Volumenanteil in %**.

| Zeit hh:mm | XXS | XS | S | M | L | XL | XXL |
|---|---|---|---|---|---|---|---|
| 00:00 ≤ t < 01:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 01:00 ≤ t < 02:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 02:00 ≤ t < 03:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 03:00 ≤ t < 04:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 04:00 ≤ t < 05:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 05:00 ≤ t < 06:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 06:00 ≤ t < 07:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 07:00 ≤ t < 08:00 | 10,0 | 25,0 | 10,0 | 27,5 | 14,7 | 33,8 | 33,7 |
| 08:00 ≤ t < 09:00 | 5,0 | 0,0 | 5,0 | 7,2 | 33,6 | 2,2 | 1,7 |
| 09:00 ≤ t < 10:00 | 5,0 | 0,0 | 5,0 | 3,6 | 1,8 | 1,1 | 0,9 |
| 10:00 ≤ t < 11:00 | 0,0 | 0,0 | 0,0 | 1,8 | 0,9 | 1,1 | 0,9 |
| 11:00 ≤ t < 12:00 | 10,0 | 0,0 | 10,0 | 3,6 | 1,8 | 1,7 | 1,3 |
| 12:00 ≤ t < 13:00 | 15,0 | 25,0 | 15,0 | 5,4 | 2,7 | 3,9 | 3,0 |
| 13:00 ≤ t < 14:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 14:00 ≤ t < 15:00 | 0,0 | 0,0 | 0,0 | 1,8 | 0,9 | 0,6 | 0,4 |
| 15:00 ≤ t < 16:00 | 0,0 | 0,0 | 0,0 | 1,8 | 0,9 | 1,1 | 0,9 |
| 16:00 ≤ t < 17:00 | 0,0 | 0,0 | 0,0 | 1,8 | 0,9 | 1,1 | 0,9 |
| 17:00 ≤ t < 18:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,6 | 0,4 |
| 18:00 ≤ t < 19:00 | 15,0 | 0,0 | 10,0 | 5,4 | 2,7 | 1,7 | 1,3 |
| 19:00 ≤ t < 20:00 | 10,0 | 0,0 | 0,0 | 1,8 | 0,9 | 0,6 | 0,4 |
| 20:00 ≤ t < 21:00 | 10,0 | 50,0 | 20,0 | 12,6 | 6,3 | 27,0 | 28,4 |
| 21:00 ≤ t < 22:00 | 20,0 | 0,0 | 25,0 | 25,7 | 31,8 | 23,7 | 25,9 |
| 22:00 ≤ t < 23:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |
| 23:00 ≤ t < 00:00 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 | 0,0 |

*Plausibilitätsprüfung durch mich: Spaltensummen = 100,0 / 100,0 / 100,0 / 100,0 / 99,9 / 100,2 / 100,1 % (Rundungsdifferenzen). Transkription damit verifiziert.*

**Wichtig:** Die Norm gibt für Tab. B.1 **keine** Zapf- oder Kaltwassertemperatur und **keinen** absoluten Tagesbedarf an — nur Volumenanteile. Die zugehörigen Absolutwerte (Zapfprogramme XXS…XXL) stehen in EN 50440, die hier nicht vorliegt.

### 2.3 Anhang B, Tabelle B.2 (S. 47) — Lastprofile nach Gebäudekategorie

Stundenbasierte relative Bedarfswerte, **Volumenanteil in %**.

| Zeit hh:mm | Einfamilien-häuser | Wohnungen | Wohnheime für Senioren | Studenten-wohnheime | Krankenhaus |
|---|---|---|---|---|---|
| 00:00 ≤ t < 01:00 | 1,8 | 1,0 | 0,3 | 1,4 | 0,4 |
| 01:00 ≤ t < 02:00 | 1,0 | 1,0 | 0,3 | 1,0 | 0,4 |
| 02:00 ≤ t < 03:00 | 0,6 | 1,0 | 0,4 | 0,5 | 0,5 |
| 03:00 ≤ t < 04:00 | 0,3 | 0,0 | 0,7 | 0,6 | 0,8 |
| 04:00 ≤ t < 05:00 | 0,4 | 0,0 | 1,0 | 1,3 | 1,2 |
| 05:00 ≤ t < 06:00 | 0,6 | 1,0 | 1,8 | 3,4 | 2,8 |
| 06:00 ≤ t < 07:00 | 2,4 | 3,0 | 9,3 | 5,8 | 7,5 |
| 07:00 ≤ t < 08:00 | 4,7 | 6,0 | 15,7 | 5,8 | 10,5 |
| 08:00 ≤ t < 09:00 | 6,8 | 8,0 | 8,1 | 6,2 | 8,0 |
| 09:00 ≤ t < 10:00 | 5,7 | 6,0 | 7,5 | 5,4 | 7,5 |
| 10:00 ≤ t < 11:00 | 6,1 | 5,0 | 7,0 | 5,1 | 7,5 |
| 11:00 ≤ t < 12:00 | 6,1 | 5,0 | 6,6 | 4,7 | 7,0 |
| 12:00 ≤ t < 13:00 | 6,3 | 6,0 | 7,1 | 4,2 | 7,5 |
| 13:00 ≤ t < 14:00 | 6,4 | 6,0 | 5,1 | 4,5 | 5,5 |
| 14:00 ≤ t < 15:00 | 5,1 | 5,0 | 3,8 | 4,1 | 4,3 |
| 15:00 ≤ t < 16:00 | 4,4 | 4,0 | 3,3 | 4,3 | 3,7 |
| 16:00 ≤ t < 17:00 | 4,3 | 4,0 | 4,1 | 5,3 | 4,5 |
| 17:00 ≤ t < 18:00 | 4,7 | 5,0 | 2,9 | 6,0 | 3,2 |
| 18:00 ≤ t < 19:00 | 5,7 | 6,0 | 6,1 | 6,6 | 7,0 |
| 19:00 ≤ t < 20:00 | 6,5 | 7,0 | 4,1 | 6,0 | 4,5 |
| 20:00 ≤ t < 21:00 | 6,6 | 7,0 | 1,4 | 5,6 | 2,0 |
| 21:00 ≤ t < 22:00 | 5,8 | 6,0 | 1,8 | 5,4 | 2,0 |
| 22:00 ≤ t < 23:00 | 4,5 | 5,0 | 0,9 | 3,9 | 1,2 |
| 23:00 ≤ t < 00:00 | 3,1 | 2,0 | 0,4 | 2,8 | 0,5 |

*Plausibilitätsprüfung: Spaltensummen 99,9 / 100,0 / 99,7 / 99,9 / 100,0 %. Verifiziert.*

**Modellhinweis:** Diese Profile sind glatt (Stundenmittel). Wendet man Gl. (3) an (V_t = x_h·V_day/60), erhält man einen **konstanten Minutenwert je Stunde** — d. h. eine Treppenkennlinie ohne Spitzenentnahmen. Für die Speicher-/Leistungsauslegung ist das die konservativ **unsichere** Seite; A100 sagt genau das (NA.5.2.2.4, S. 11): „Sollte diese Auflösung [1 min] nicht gewährleistet sein, können Auflösungsintervalle von bis zu 1 h verwendet werden, wobei berücksichtigt werden muss, dass **Spitzenentnahmen in diesem Fall unterschätzt werden**."

### 2.4 A100 — 18 deutsche Referenz-Bedarfsprofile (NA.5.2.6, S. 15–32)

Dies ist der eigentliche Mehrwert des A100-Entwurfs. Die Profile wurden aus realen Verbrauchsmessungen (Verfahren NA.5.2.2) gewonnen; als Referenztag wurde „in der Regel der Tag aus allen Messreihen mit dem höchsten Bedarf bzw. mit der größten Bedarfsspitze über 1 h ausgewählt" (S. 13).

**Kritische Einschränkung für die Implementierung:** Im PDF sind je Profil nur (a) eine **Grafik** des Minutenprofils, (b) eine Grafik der normierten Bedarfskennlinie und (c) eine Kennwerte-Box abgedruckt. **Die minütlichen Zahlenwerte stehen nicht im Normtext.** NA.5.2.5 (S. 13): „Die … Referenzbedarfsprofile stehen den Nutzern der DIN EN 12831-3 auf **CD-ROM bzw. als Zip-Datei zum Download** zur Verfügung." → Für WP-Plan müssen diese Datendateien separat beschafft werden. Die Grafiken sind zu grobaufgelöst, um daraus Minutenwerte zu rekonstruieren (**als unsicher gekennzeichnet**).

Ebenfalls wichtig (S. 14): „Die aufgeführten Werte für den Spitzendurchfluss … dienen lediglich der Information. **Sie sind nicht für Trinkwasserauslegungszwecke zu verwenden.**"

Alle Profile: PWC = 10 °C, PWH = 60 °C mit Zirkulation (soweit angegeben).

#### Tabelle: Referenz-Bedarfsprofile A100, Nichtwohn- und Sondernutzungen

| NA-Nr. | Bezeichnung | Gebäudetyp / Nutzung | Größenordnung | Sanitärausstattung | V_day [l/d] | Q_day [kWh/d] | bezogen (Volumen) | bezogen (Energie) | Spitzendurchfluss 1/2/5/10 min [l/s] |
|---|---|---|---|---|---|---|---|---|---|
| NA.5.2.6.1 | Hotel Messehotel 20 Zimmer | Hotel, 3 Sterne, Messehotel, Zimmer einzeln belegt (ohne Gastronomie) | 20 Zimmer | zentrale Anlage | 606 | 35,2 | 30,0 l/Bett·d | 1,8 kWh/Bett·d | 0,26 / 0,24 / 0,22 / 0,19 |
| NA.5.2.6.2 | Hotel 300 Zimmer | Hotel, 5 Sterne, überw. Doppelzimmer | 300 Zimmer, Auslastung 95 % | zentrale Anlage | 24 580 | 1 428 | 86 l/Bett·d | 5,0 kWh/Bett·d | 3,0 / 3,0 / 2,9 / 2,8 |
| NA.5.2.6.3 | Gastronomie Mensa Hochschule | Gastronomie — Mensa, Hochschulmensa | ca. 825 Mahlzeiten (MZ)/Tag | zentrale Anlage, 24 Zapfstellen | 2 218 | 129 | 2,7 l/MZ·d | 0,16 kWh/MZ·d | 0,88 / 0,70 / 0,45 / 0,38 |
| NA.5.2.6.4 | Gastronomie Hotelküche | Hotel, 5 Sterne, Küche für 300 Mahlzeiten | 300 Zimmer, Auslastung 95 % | zentrale Anlage | 5 790 | 336 | 20,3 l/MZ·d | 1,2 kWh/MZ·d | 1,05 / 1,02 / 0,90 / 0,84 |
| NA.5.2.6.5 | Krankenhaus 60 Betten | Krankenhaus; Cafeteria, Krankengymnastik, Ergotherapie | 60 Betten, Auslastung 50 % | zentrale Anlage, 37 Nasszellen | 1 098 | 64 | 36,6 l/Bett·d | 2,1 kWh/Bett·d | 0,43 / 0,39 / 0,32 / 0,25 |
| NA.5.2.6.6 | Krankenhaus 242 Betten | Betten-/OP-Trakt, Intensivstation, Anästhesie, Radiologie, Orthopädie, Urologie, 2 Abt. Innere Medizin | 242 Betten | zentrale Anlage | 12 078 | 702 | 49,9 l/Bett·d | 2,9 kWh/Bett·d | 0,97 / 0,81 / 0,65 / 0,58 |
| NA.5.2.6.7 | Krankenhaus 590 Betten | Innere Medizin (mehrere Abt.), Neurologie, Kinderklinik, Frauenheilkunde, Allgemein-/Unfallchirurgie, HNO, Psychiatrie, Radiologie, Belegabt. Augenheilkunde | 590 Betten, Auslastung 75 % | zentrale Anlage, 1 126 Waschbecken, 382 Duschen | 21 935 | 1 191 | 49,40 l/Bett·d | 2,7 kWh/Bett·d | 1,91 / 1,85 / 1,67 / 1,43 |
| NA.5.2.6.8 | Klinikum Funktionsgebäude | Krankenhaus; Funktionsgebäude mit Notfallambulanz und OP-Gebäude | 68 Betten | zentrale Anlage, 96 Waschbecken, 14 Duschen, 17 Spülen, 8 Steckbeckenspüler | 2 573 | 149 | 37,8 l/Bett·d | 2,2 kWh/Bett·d | 0,63 / 0,53 / 0,40 / 0,27 |
| NA.5.2.6.9 | JVA Zellentrakt mit festen Duschzeiten | Justizvollzugsanstalt; Zellentrakt, feste Duschzeiten ca. 1 h/Tag | 110 Häftlinge | zentrale Anlage | 2 921 | 170 | 26,6 l/P·d | 1,5 kWh/P·d | 0,62 / 0,62 / 0,60 / 0,60 |
| NA.5.2.6.10 | Schwimmbad | Schwimmbad; Freizeitbad, Sauna, Sportbad | ca. 3 000 Personen/Tag, 3 212 m² Beckenfläche | zentrale Anlage, 101 Duschen, 67 Waschbecken | 46 192 | 2 683 | 15,4 l/P·d | 0,9 kWh/P·d | 2,59 / 2,56 / 2,31 / 2,14 |

#### Tabelle: Referenz-Bedarfsprofile A100, Wohngebäude und wohnähnliche Nutzungen

| NA-Nr. | Bezeichnung | Gebäudetyp / Nutzung | Größenordnung | Sanitärausstattung | V_day [l/d] | Q_day [kWh/d] | bezogen (Volumen) | bezogen (Energie) | Spitzendurchfluss 1/2/5/10 min [l/s] |
|---|---|---|---|---|---|---|---|---|---|
| NA.5.2.6.11 | Seniorenheim | Wohngebäude / Seniorenheim | 70 Einzelzimmer, 5 Doppelzimmer | zentrale Anlage, 75 Nasszellen | 5 005 | 291 | 66,7 l/P·d | 3,9 kWh/P·d | 0,56 / 0,50 / 0,46 / 0,38 |
| NA.5.2.6.12 | Seniorenheim mit Kurzzeitpflege | Wohngebäude / Seniorenheim | 75 Zimmer, 15 Tagespflegeplätze | zentrale Anlage, 80 Nasszellen | 1 473 | 85,6 | 17,3 l/Bett·d | 1,0 kWh/Bett·d | 0,41 / 0,36 / 0,32 / 0,31 |
| NA.5.2.6.13 | Studentenheim | Wohngebäude / Studentenwohnheim | 165 Einzelapp., 33 Doppelapp., Gemeinschaftsküchen | zentrale Anlage, 396 Zapfstellen | 9 940 | 577 | 43,0 l/P·d | 2,5 kWh/P·d | 0,95 / 0,85 / 0,78 / 0,72 |
| NA.5.2.6.14 | Mehrfamilienhaus | Wohngebäude / MFH | 24 WE, 1 500 m² BGF, 35 Personen | zentrale Anlage, 100 kW Gaskessel, 500 l Ladespeicher | 689 | 40 | 19,7 l/P·d | 1,1 kWh/P·d | 0,39 / 0,33 / 0,17 / 0,13 |
| NA.5.2.6.15 | Wohngebäude n. DIN 4708, N = 2 | Verfahren nach DIN 4708:1994-04 | äquiv. Bedarf bis 2 Wohnungen à 3,5 Personen (= 7 P) | — | 488 | 19,9 | 69,7 l/P·d | 2,8 kWh/P·d | (nicht angegeben) |
| NA.5.2.6.16 | Wohngebäude n. DIN 4708, N = 4 | Verfahren nach DIN 4708:1994-04 | äquiv. Bedarf bis 4 Wohnungen à 3,5 Personen (= 14 P) | — | 858 | 34,92 | 61,3 l/P·d | 2,5 kWh/P·d | (nicht angegeben) |
| NA.5.2.6.17 | Wohngebäude n. DIN 4708, N = 10 | Verfahren nach DIN 4708:1994-04 | äquiv. Bedarf bis 10 Wohnungen à 3,5 Personen (= 35 P) | — | 1 882 | 76,6 | 53,8 l/P·d | 2,2 kWh/P·d | (nicht angegeben) |
| NA.5.2.6.18 | Wohngebäude n. DIN 4708, N = 20 | Verfahren nach DIN 4708:1994-04 | äquiv. Bedarf bis 20 Wohnungen à 3,5 Personen (= 70 P) | — | 3 500 | 142,4 | 50,0 l/P·d | 2,0 kWh/P·d | (nicht angegeben) |

**Eigene Konsistenzprüfung der DIN-4708-Profile (nicht Normtext):** V_day/(l/P·d) ergibt exakt 7 / 14 / 35 / 70 Personen. Q_day/V_day entspricht Δθ = 35 K → die Werte sind auf **45 °C / 10 °C** normiert, konsistent mit NA.5.3.1 und Tab. NA.5.

**Zeitliche Charakteristik der DIN-4708-Profile (aus den Grafiken, als teilweise unsicher gekennzeichnet):** Alle vier Profile bestehen aus **fünf Zapfblöcken zwischen ca. 00:00 und ca. 06:00 Uhr**, danach ist der Tag leer (kumulierte Kurve konstant bei 1,0). A100 (S. 14) bestätigt das: „Die Zapfungen beginnen praktischerweise bei 00:00 Uhr, die Breite des Bedarfsprofils entspricht der **Bedarfsperiode 2·T_N** in DIN 4708." Die Blöcke tragen je ca. 20 % des Tagesbedarfs bei (Stufen bei 0,2 / 0,4 / 0,6 / 0,8 / 1,0). Das ist **kein realer Tagesgang**, sondern die formale Abbildung der DIN-4708-Bedarfsperiode auf die Summenlinie.

### 2.5 A100 Tabelle NA.3 (S. 12) — Beispiel manuell konstruiertes Profil (Freizeit-Zentrum)

Vollständige Wiedergabe (Vorlage für den „Profil-Konstruktor" in WP-Plan):

| Nr. | Uhrzeit | Verbraucher | Warmwasserverbrauch [l] | Warmwasser-Zapftemperatur [°C] | Wärmemenge [kWh] |
|---|---|---|---|---|---|
| 1 | 08:00 bis 11:00 | Küche | 360 | 60 | 21 |
| 2 | 12:00 bis 13:00 | Küche | 360 | 60 | 21 |
| 3 | 15:30 bis 16:30 | Küche | 360 | 60 | 21 |
| 4 | 15:00 bis 17:00 | 100 Duschen | 100 × 8 × 5 = 4 000 | 40 | 140 |
| 5 | 17:30 bis 20:00 | Küche | 900 | 60 | 52 |
| 6 | 18:00 bis 20:00 | 50 Duschen | 50 × 8 × 5 = 2 000 | 40 | 70 |
| 7 | 20:00 bis 22:00 | Küche | 360 | 60 | 18 |
| 8 | 22:00 bis 23:00 | 50 Duschen | 50 × 8 × 5 = 2 000 | 40 | 70 |
| **Summe** | | | | | **413** |

Zu entnehmen: Duschen sind mit **8 l/min × 5 min = 40 l je Duschvorgang bei 40 °C** angesetzt. Die Zeitangaben sind Intervalle, innerhalb derer die Menge verteilt wird (Bild NA.5 zeigt die resultierende Bedarfskennlinie als Polygonzug).

*Anmerkung: Zeile 7 (360 l, 60 °C) ergibt 18 kWh statt der 21 kWh der Zeilen 1–3 bei identischer Menge/Temperatur — eine Inkonsistenz im Entwurf. Summe 413 kWh entspricht der Addition der abgedruckten Werte.*

---

## 3. Bedarfskennwerte

### 3.1 Tabelle B.3 (S. 47–48) — Netto-Energiebedarf je Tag, q_w,b,d (nutzungs- und flächenbezogen)

| Art der Nutzung | Nutzungsabhängig | Flächenbezogen | Bezugsfläche |
|---|---|---|---|
| Bürogebäude | 0,4 kWh je Person und Tag | 30 Wh/(m²·d) | Bürogrundfläche |
| Bettenstation oder Krankenzimmer | 8,0 kWh je Bett und Tag | 530 Wh/(m²·d) | Stationen und Zimmer |
| Schule ohne Duschen | 0,5 kWh je Person und Tag | 170 Wh/(m²·d) | Klassenräume |
| Schule mit Duschen | 1,5 kWh je Person und Tag | 500 Wh/(m²·d) | Klassenräume |
| Einzelhandelsgeschäft/Kaufhaus | 1,0 kWh je Angestellter und Tag | 10 Wh/(m²·d) | Verkaufsflächen |
| Werkstatt, Industriewerk (mit Wasch- und Duschgelegenheiten) | 1,5 kWh je Angestellter und Tag | 75 Wh/(m²·d) | Werkstattfläche/Werksfläche |
| Einfaches Hotel | 1,5 kWh je Bett und Tag | 190 Wh/(m²·d) | Hotelzimmer |
| Mittelklasse-Hotel | 4,5 kWh je Bett und Tag | 450 Wh/(m²·d) | Hotelzimmer |
| Luxusklasse-Hotel | 7,0 kWh je Bett und Tag | 580 Wh/(m²·d) | Hotelzimmer |
| Restaurant, Gaststätte/Schankraum | 1,5 kWh je Sitz und Tag | 1 250 Wh/(m²·d) | Öffentliche Räume |
| Heim (Seniorenheim, Waisenhaus usw.) | 3,5 kWh je Person und Tag | 230 Wh/(m²·d) | Zimmer |
| Kasernen | 1,5 kWh je Person und Tag | 150 Wh/(m²·d) | Zimmer |
| Sportstätten mit Duschen | 1,5 kWh je Person und Tag | – | – |
| Großküchen, Kantinen | 0,4 kWh je Mahlzeit | – | – |
| Bäckerei | 5,0 kWh je Angestellter und Tag | – | – |
| Friseur/Coiffeur | 8,0 kWh je Angestellter und Tag | – | – |
| Fleischerei mit eigener Herstellung | 18,0 kWh je Angestellter und Tag | – | – |
| Wäscherei | 20,0 kWh je 100 kg Wäsche | – | – |
| Brauerei | 15,0 kWh je 100 l Bier | – | – |
| Molkerei | 10,0 kWh je 100 l Milch | – | – |

ANMERKUNG (S. 48): „Die Werte in Tabelle B.3 umfassen **nicht** die durch die gelegentlich genutzten Entnahmeleitungen entstehenden Wärmeverluste."

### 3.2 Tabelle B.4 (S. 48–49) — Volumenbedarf V_W;f;day je Tag

Referenz: **Zapftemperatur 60 °C, Kaltwasser-Zulauf 13,5 °C** (S. 49; durch A1 als ausdrückliche Anmerkung unter Tab. B.4 eingefügt).

| Art der Tätigkeit | V_W;f;day [l/d] | f (Bezugsgröße) |
|---|---|---|
| Wohngebäude | siehe Gl. (B.1)–(B.5) | Anzahl äquivalenter Erwachsener |
| Unterkunft | 28 | Anzahl der Betten |
| Gesundheitseinrichtungen ohne stationären Bereich | 10 | Anzahl der Betten |
| Gesundheitseinrichtungen mit stationärem Bereich – ohne Wäscherei | 56 | Anzahl der Betten |
| Gesundheitseinrichtungen mit stationärem Bereich – mit Wäscherei | 88 | Anzahl der Betten |
| Bildungseinrichtungen | Anforderungen an das Warmwasser nicht berücksichtigt | — |
| Büros | Anforderungen an das Warmwasser nicht berücksichtigt | — |
| Theater und Hörsäle | Anforderungen an das Warmwasser nicht berücksichtigt | — |
| Läden | Anforderungen an das Warmwasser nicht berücksichtigt | — |
| Gastronomie, 2 Mahlzeiten je Tag, Traditionelle Küche | 21 | Anzahl der Gäste je Mahlzeit |
| Gastronomie, 2 Mahlzeiten je Tag, Selbstbedienung | 8 | Anzahl der Gäste je Mahlzeit |
| Gastronomie, 1 Mahlzeit je Tag, Traditionelle Küche | 10 | Anzahl der Gäste je Mahlzeit |
| Gastronomie, 1 Mahlzeit je Tag, Selbstbedienung | 4 | Anzahl der Gäste je Mahlzeit |
| Hotel, 1 Stern, ohne Wäscherei | 56 | Anzahl der Betten |
| Hotel, 1 Stern, mit Wäscherei | 70 | Anzahl der Betten |
| Hotel, 2 Sterne, ohne Wäscherei | 76 | Anzahl der Betten |
| Hotel, 2 Sterne, mit Wäscherei | 90 | Anzahl der Betten |
| Hotel, 3 Sterne, ohne Wäscherei | 97 | Anzahl der Betten |
| Hotel, 3 Sterne, mit Wäscherei | 111 | Anzahl der Betten |
| Hotel, 4 Sterne und GC (Golfclub), ohne Wäscherei | 118 | Anzahl der Betten |
| Hotel, 4 Sterne und GC, mit Wäscherei | 132 | Anzahl der Betten |
| Sportstätten | 101 | Anzahl der eingebauten Duschen |
| Lager | Anforderungen an das Warmwasser nicht berücksichtigt | — |
| Industrielle Einrichtungen | Anforderungen an das Warmwasser nicht berücksichtigt | — |
| Transport | Anforderungen an das Warmwasser nicht berücksichtigt | — |
| Sonstige | Anforderungen an das Warmwasser nicht berücksichtigt | — |

### 3.3 Tabelle B.5 (S. 50) — Wohngebäude, Liter je Person und Tag

**Achtung:** Kopfzeile der Originaltabelle lautet „V_W;f,day"; **A1 ersetzt sie durch V_W,P,day [Liter erwärmtes Trinkwasser je Person und Tag]**. A1 ergänzt außerdem: Referenz **45 °C / 10 °C**.

| Art des Gebäudes | V_W,P,day [l erw. TW je Person und Tag] |
|---|---|
| Wohngebäude (einfache Unterkunft) | 25 – 60 |
| Wohngebäude (Luxus-Unterkunft) | 60 – 100 |
| Einfamilienhäuser | 40 – 70 |
| Wohnungen | 25 – 30 |

### 3.4 Äquivalente Personenzahl (Degressionsformeln, S. 49–50)

Diese Formeln bilden die **Gleichzeitigkeit/Diversität im Wohnungsbau** ab — es gibt keine separate Gleichzeitigkeitstabelle.

**Einfamilien-/Reihenhäuser (Gl. B.1):**
```
n_P,eq,max = 1                              wenn A_h < 30 m²
           = 1,75 − 0,018 75 · (70 − A_h)   wenn 30 m² ≤ A_h < 70 m²
           = 0,025 · A_h                    wenn A_h ≥ 70 m²
```
**Gesamtanzahl (Gl. B.2):**
```
n_P,eq = n_P,eq,max                          wenn n_max < 1,75
       = 1,75 + 0,3 · (n_P,eq,max − 1,75)    wenn n_max ≥ 1,75
```
**Wohnungen (Gl. B.3):**
```
n_P,eq,max = 1                               wenn A_h < 10 m²
           = 1,75 − 0,018 75 · (50 − A_h)    wenn 10 m² < A_h < 50 m²
           = 0,035 · A_h                     wenn A_h > 50 m²
```
**Gesamtanzahl (Gl. B.4):** identisch zu (B.2), mit Bedingung auf n_P,eq,max.

**Bedarf je Person (Gl. B.5):**
```
V_W,P,day = min( x ; y · A_h / n_P,eq )      mit x = 40,71 ; y = 3,26
```

*Editorielle Auffälligkeiten (für die Implementierung relevant):* In (B.2)/(B.4) wird als Bedingung einmal „n_max", einmal „n_P,eq,max" geschrieben — gemeint ist offensichtlich n_P,eq,max. In (B.3) ist die zweite Bedingung mit „<" statt „≤" an der Untergrenze 10 m² formuliert, sodass A_h = 10 m² formal nicht abgedeckt ist. Ebenso ist die Bezeichnung „(B.1)" doppelt vergeben (n_P,eq,max **und** Dichtegleichung auf S. 55).

### 3.5 Auslegungstemperaturen

**Tabelle B.6 (S. 51) — EN-Vorgabewerte:**

| Leistungsbeschreibung | Symbol | Temperatur [°C] |
|---|---|---|
| Zapftemperatur des erwärmten Trinkwassers = minimale Temperatur des am Entnahmeventil abgezapften Mischwassers (benötigt Temperatur) | ϑ_w,draw | **42** |
| Kaltwasser-Zulauftemperatur am Gebäudeeingang | ϑ_w,c | **10** |

**Tabelle NA.5 (A100, S. 34) — nationale Anpassung:**

| Leistungsbeschreibung | Symbol | Temperatur [°C] |
|---|---|---|
| Zapftemperatur des erwärmten Trinkwassers = minimale Temperatur des am Entnahmeventil abgezapften Mischwassers | ϑ_W,draw | **45** |
| Kaltwasser-Zulauftemperatur am Gebäudeeingang | ϑ_W,c | **10** |

Zusatz A100: „Die Zapf- und Kaltwassertemperaturen können von den hier angegebenen Werten abweichen."

**Weitere Temperatur-Referenzen im Normwerk:**
- Tab. B.4: 60 °C / 13,5 °C
- Tab. B.5: 45 °C / 10 °C (per A1 klargestellt)
- Tab. NA.4 (A100): normiert auf 10 °C / 45 °C
- A100-Referenzprofile NA.5.2.6: gemessen bei PWC 10 °C / PWH 60 °C mit Zirkulation
- Standard-Rohrmitteltemperatur ϑ_m ≈ 50 °C (B.3.3)
- A100 NA.5.4.4: t_lag-Werte für WP beziehen sich auf Kaltstart 20 °C → 60 °C; für alle anderen Erzeuger 20 °C → 70 °C
- A100 Tab. NA.7 (kleine Wohngebäude): ϑ_w,sto,max = 60 °C festgelegt

### 3.6 Zuschläge Zirkulation / Verteilverluste

Es gibt **keine pauschalen Prozentzuschläge**. Verluste werden physikalisch gerechnet:

**Tabelle B.9 (S. 52) — spezifischer Wärmeverlust der Verteilung:**

| Rohrdurchmesserbereich d [mm] | Dicke der Dämmung s und Temperaturdifferenz (ϑ_m − ϑ_a) für λ = 0,035 W/(m·K) | q'_dis [W/m] |
|---|---|---|
| 10 – 150 | s = d ; (ϑ_m − ϑ_a) = 35 K | **7** |
| 10 – 150 | s = d ; (ϑ_m − ϑ_a) = 50 K | **11** |

Standardwert mittlere Innentemperatur: ϑ_m;j ≈ ϑ_m = **50 °C**.

**Tabelle B.8 (S. 52) — Bereitschaftsverluste des Speichers q_sb,sto:**

| Bruttospeichervolumen [l] | q_sb,sto [kWh/d] |
|---|---|
| 5 und weniger | 0,35 |
| 30 | 0,60 |
| 50 | 0,78 |
| 80 | 0,98 |
| 100 | 1,10 |
| 120 | 1,20 |
| 150 | 1,35 |
| 200 | 1,56 |
| 300 | 1,91 |
| 400 | 2,20 |
| 500 | 2,46 |
| 600 | 2,69 |
| 800 | 3,11 |
| 1 000 | 3,48 |
| 1 250 | 3,89 |
| 1 500 | 4,26 |
| 2 000 | 4,92 |

Hinweis (S. 52): Werte gelten für **Speicher mit zwei Verbindungsrohren**; „für jedes zusätzlich angeschlossene Rohr muss der Wert um **0,1 kWh/d** erhöht werden." *(Der Text verweist irrtümlich auf „Tabelle B.4" statt B.8 — editorieller Fehler.)*

**Tabelle B.7 (S. 51) — Lastfaktor f_l:**

| Typ des Speichers für erwärmtes Trinkwasser | | Lastfaktor f_l |
|---|---|---|
| gemischte Speichersysteme | vertikal/aufrecht | 0,96 |
| | horizontal ≤ 400 l | 0,94 |
| | horizontal > 400 l | 0,90 |
| Speicherladesysteme (Schichtladung)ᵃ | | 1,0 |

ᵃ Dieser Wert wird auch für Energiespeicher verwendet.

**Tabelle B.10 (S. 53) — Zeitverzögerung Wärmeerzeuger t_lag,HG:**

| Wärmeerzeuger | t_lag,HG [min] |
|---|---|
| Wandhängender Wärmeerzeuger und Standkessel mit Aluminium-Wärmeübertragern | 2 |
| **Wärmepumpen** | **4** |
| Standkessel und KWK-Systeme | 6 |
| Pelletkessel (mit automatischer Beschickung) | 30 |
| Holzkessel (mit manueller Beschickung) | 45 |

**Tabelle B.11 (S. 53):** t_lag,dis = **0 min** für alle Arten von Anlagen.

**Tabelle B.12 (S. 53) — f_HG,ϑ (ungleiche Temperaturverteilung):**

| Wärmeerzeuger | f_HG;ϑ [–] |
|---|---|
| Kessel (alle Typen) | 0,9 |
| **Wärmepumpen** | **0,4** |
| KWK-Systeme | 0,3 |

**Tabelle B.13 (S. 53) — f_HG,Q (Zündvorgang/Leistungsanpassung):**

| Wärmeerzeuger | f_HG,Q [–] |
|---|---|
| Pelletkessel | 0,6 – 0,8 |
| Hackschnitzelkessel | 0,5 – 0,75 |
| alle anderen Kesseltypen | 1 |

**Tabelle B.14 (S. 55) — Konstanten Auslegungsdurchfluss (Gl. B.6):**

| Art des Gebäudes | a | b | c |
|---|---|---|---|
| Wohnungen | 1,48 | 0,19 | 0,94 |
| Patientenstation in Krankenhäusern | 0,75 | 0,44 | 0,18 |
| Hotel | 0,70 | 0,48 | 0,13 |
| Schule | 0,91 | 0,31 | 0,38 |
| Bürogebäude | 0,91 | 0,31 | 0,38 |
| Seniorenheim | 1,48 | 0,19 | 0,94 |
| Pflegeheim | 1,40 | 0,14 | 0,92 |

**Allgemeine Werte (B.4, S. 55–56):**
- ρ_W = 1 000 kg/m³ bzw. 1 kg/l; genauer: ρ_W = 1 000 − 0,005 · (ϑ_W − 4)²
- c_W = **4,2 kJ/(kg·K)**
- c_M (Wärmeerzeuger-Material) = **0,5 kJ/(kg·K)**

### 3.7 Gleichzeitigkeit / Diversität — Befund

Die Norm kennt **keinen expliziten Gleichzeitigkeitsfaktor**. Diversität wird abgebildet über:
1. **Gl. (B.6)** V̇_D = a·(Σ V̇_A)^b − c — die Summenkurve der Einzelentnahmen wird degressiv verdichtet (nur für Anlagen mit direktem Durchfluss)
2. **Gl. (B.1)–(B.4)** n_P,eq — Degression der äquivalenten Personenzahl
3. Implizit im **Lastprofil selbst** (die A100-Referenzprofile sind Messungen realer Gleichzeitigkeit)
4. A100 NA.5.3 (S. 33) verlangt: „Der Einfluss der Gleichzeitigkeit auf den Tagesbedarf sollte entsprechend berücksichtigt werden." — **ohne Verfahren anzugeben**. Das ist eine offene Lücke, die WP-Plan als Nutzereingabe abbilden muss.

---

## 4. A100-Entwurf — nationale Festlegungen

### 4.1 Grundsätzliches

NA.5.1 (S. 7): *„Dieser Teil **ersetzt** DIN EN 12831-3, Anhang B (informativ)."* → Für Deutschland gelten künftig die NA-Werte anstelle von Anhang B; Anhang B wird nur dort weiterverwendet, wo NA.5.4 dies ausdrücklich sagt (siehe 4.4).

NA.1 (S. 4): „So werden **erstmals für Deutschland gültige Referenzbedarfsprofile** vorgestellt und erläutert. Auch wie Datensätze und Messprotokolle aufgebaut werden müssen, um die Datentechnische Auswertung zu vereinfachen."

**Wichtiger Haftungshinweis (NA.0, S. 4):** „Die Anwendung von DIN EN 12831-3/A100 zusammen mit EN 12831-3 ermöglicht eine sichere Auslegung … Aufgrund von außergewöhnlichen und/oder unvorhersehbaren Bedarfssituationen (z. B. unerwarteter Veranstaltungsbetrieb, Nutzungsänderung, nachträglich geänderte Ausstattung) kann es bei bestimmungsgemäßer Anwendung des Verfahrens dennoch kurzfristig zu einer **Untertemperierung von Trinkwasser warm** kommen."

### 4.2 Verhältnis zu DIN 4708: **parallel, nicht ersetzt**

DIN 4708-1/-2/-3:1994-04 sind unter **NA.2 „Normative Verweisungen"** (S. 4) gelistet — also normativ in Bezug genommen. A100 ersetzt DIN 4708 nicht, sondern **überführt sie**:

- NA.5.2.6.15–18 stellen die DIN-4708-Bedarfskennzahlen N = 2, 4, 10, 20 als **EN-12831-3-taugliche Bedarfsprofile** bereit
- S. 14: „Ersichtlich ist, dass sich das statistische Verfahren der DIN 4708 (alle Teile) (**Gaußverteilung mit Spitzenbedarfsanhebung**) mit dem Summenlinienverfahren nach DIN EN 12831-3 abbilden lässt."
- S. 14, entscheidender Satz: „**Die hohen Bedarfswerte entsprechen den Werten der Normenreihe DIN 4708:1994-04. Diese führen in der Regel zu einer großzügigen leistungsmäßigen Auslegung des Wärmeerzeugers. Durch eine realistische Anpassung des Tagesbedarfs an Trinkwarmwasser (siehe NA.5.6), kann nach dem Verfahren der EN 12831-3 die Trinkwassererwärmungsanlage kleiner dimensioniert werden.**"

### 4.3 Tabelle NA.4 (S. 33–34) — Richtwerte des Nutzenergiebedarfs

Basis laut NA.5.3.1: „Diese Werte entsprechen den Bedarfswerten nach **DIN V 18599-10** und sind auf eine Kaltwassertemperatur von 10 °C und einer Warmwasserzapftemperatur von 45 °C normiert."

| Art der Nutzung | Volumen [l/X·d] | Energie [kWh/X·d] | Bezug auf X/d | Volumen [l/m²·d] | Energie [Wh/m²·d] | Fläche/Nutzung [m²/Bezug] | Bezugsfläche |
|---|---|---|---|---|---|---|---|
| Bürogebäude | 9,8 | 0,4 | Person/d | 0,7 | 30,0 | 13,3 | Bürogrundfläche |
| Bettenzimmer/Krankenhaus | 146,9 | 6,0 | Bett/d | 9,8 | 400,0 | 15,0 | Bettenzimmer |
| Schule ohne Duschen | 9,8 | 0,4 | Person/d | 3,2 | 130,0 | 3,1 | Klassenräume |
| Schule mit Duschen | 36,7 | 1,5 | Person/d | 12,2 | 500,0 | 3,0 | Klassenräume |
| Einzelhandel/Kaufhaus | 24,5 | 1,0 | Angest./d | 0,2 | 10,0 | 100,0 | Verkaufsflächen |
| Werkstatt, Industriebetrieb (für Waschen und Duschen) | 44,1 | 1,8 | Angest./d | 2,2 | 90,0 | 20,0 | Werkstattfläche/Werksfläche |
| Hotel einfach | 46,5 | 1,9 | Bett/d | 5,9 | 240,0 | 7,9 | Hotelzimmer |
| Hotel mittel | 85,7 | 3,5 | Bett/d | 8,6 | 350,0 | 10,0 | Hotelzimmer |
| Hotel luxus | 134,7 | 5,5 | Bett/d | 11,3 | 460,0 | 12,0 | Hotelzimmer |
| Restaurant, Gaststätte | 26,9 | 1,1 | Sitz/d | 22,5 | 920,0 | 1,2 | Öffentliche Räume |
| Heim | 56,3 | 2,3 | Person/d | 3,7 | 150,0 | 15,3 | Zimmer |
| Kasernen | 44,1 | 1,8 | Person/d | 4,4 | 180,0 | 10,0 | Zimmer |
| Sportanlage mit Dusche | 44,1 | 1,8 | Person/d | — | — | — | — |
| Gewerbeküchen, Kantine | 9,8 | 0,4 | Menü/d | — | — | — | — |
| Bäckerei | 122,4 | 5,0 | Angest./d | — | — | — | — |
| Friseure | 146,9 | 6,0 | Angest./d | — | — | — | — |
| Fleischerei mit Produktion | 440,8 | 18,0 | Angest./d | — | — | — | — |
| Wäscherei | 489,8 | 20,0 | 100 kg Wäsche | — | — | — | — |
| Brauerei | 367,3 | 15,0 | 100 l Bier | — | — | — | — |
| Molkerei | 244,9 | 10,0 | 100 l Milch | — | — | — | — |
| Saunabereich | 68,6 | 2,8 | Person/d | 5,8 | 235,0 | 11,9 | Person |
| Labor | 9,8 | 0,4 | Person/d | 0,7 | 30,0 | 13,3 | Person |
| Fitnessraum | 36,7 | 1,5 | Person/d | 7,3 | 300,0 | 5,0 | Person |
| **Einfamilienhäuser** | **40** | **1,6** | Person/d | 1,3 | 54,4 | 30,0 | Nutzfläche |
| **Einfamilienhäuser gehobene Ausstattung** | **60** | **2,5** | Person/d | 1,5 | 61,3 | 40,0 | Nutzfläche |
| **Doppelhaushälfte** | **40** | **1,6** | Person/d | 1,0 | 40,8 | 40,0 | Nutzfläche |
| **Mehrfamilienhäuser** | **30** | **1,2** | Person/d | 1,0 | 40,8 | 30,0 | Nutzfläche |
| **Mehrfamilienhäuser gehobene Ausstattung** | **35** | **1,4** | Person/d | 1,0 | 40,8 | 35,0 | Nutzfläche |

Neu gegenüber Anhang B: **Saunabereich, Labor, Fitnessraum**, sowie die differenzierten Wohngebäude-Kategorien (EFH / EFH gehoben / DHH / MFH / MFH gehoben).

**Zusatzregel Wohnbau (S. 34):** „Bei Einfamilienhäusern ist in der Berechnung mindestens einmal der Wert der größten Entnahmestelle einzusetzen, (z. B. **Badewannenfüllung mit 45 °C und Nutzvolumen = 160 l**). Bei höheren Komfortansprüchen (z. B. mehrfache Wannenfüllungen in kürzester Zeit oder große Duschpaneele) ist der Wert entsprechend zu erhöhen."

### 4.4 Was A100 unverändert aus Anhang B übernimmt (NA.5.4, S. 34–35)

| Parameter | A100-Regelung |
|---|---|
| Lastfaktoren f_l | Tab. B.7 „kann unverändert angewendet werden" |
| Bereitschaftsverluste q_sb,sto | Tab. B.8 unverändert |
| q'_dis und ϑ_m | Tab. B.9 unverändert |
| Zeitverzögerung t_lag,HG | Tab. B.10–B.13 unverändert, mit dem Hinweis auf die Temperatur-Referenzen (WP: 20→60 °C; sonst 20→70 °C) |
| Dichte / spez. Wärmekapazität | Abschn. B.4 unverändert (NA.5.5) |
| V̇_D | Verfahren nach B.3.6; entspricht dem Spitzendurchfluss V̇_S nach **DIN 1988-300**, Koeffizienten identisch (NA.5.4.9) |

### 4.5 Neue Rechenregeln in A100

**NA.5.4.5 — Φ_N richtig wählen (S. 35):**
> „Demnach ist für Φ_N stets **der kleinere der beiden Werte** Nennleistung des Wärmeerzeugers nach Herstellerangabe oder die Dauerleistung des Wärmeübertragers bei festgelegter Heizmittelübertemperatur zu verwenden."

Bei unbekanntem U_HE / A_HE:

**Tabelle NA.6 — Wärmedurchgangskoeffizienten für durchmischte Speicher mit innenliegenden Wärmeübertragern:**

| Speichertyp/Material | U_HE [W/(m²·K)] |
|---|---|
| Stahl/Emailliert | 700 |
| Edelstahl | 970 |

**Wärmeübertragerfläche:**
```
Öl-, Gaskessel, BHKW:   A_HE,Kessel = 0,003 6 · V_sto + 0,293 4     (NA.1)
Wärmepumpe:             A_HE,WP     = 0,020 5 · V_sto − 2,152       (NA.2)
```
mit A_HE in m², V_sto in l.

> **Für WP-Plan besonders relevant:** Gl. (NA.2) liefert für V_sto < ca. 105 l eine **negative Fläche**. Die Formel ist erkennbar nur für größere Speicher gültig; eine Untergrenze nennt der Entwurf nicht. In der Implementierung ist ein Plausibilitäts-Guard (A_HE > 0, ggf. Warnung) zwingend.

**NA.5.4.6 — Speicherbilanz statt e-Funktion (S. 36):** A100 kritisiert Gl. (15) der Hauptnorm: sie „berücksichtigt jedoch nicht den energetischen Einfluss der Wasserentnahme während einer Ladephase, so dass das Ladeverhalten nicht korrekt wiedergegeben wird." Stattdessen:
```
Q_sto,i+1 = Q_sto,i − Q_w,b,i + Q_eff,i                              (NA.3)
ϑ_sto,m,i+1 = Q_sto,i+1 / (V_sto · ρ_w · c_w) + ϑ_w,c                (NA.4)
```
**NA.5.4.7:** Damit wird die Zeitkonstante τ nach Gl. (16) **informativ** — sie wird nicht mehr gebraucht, wenn (NA.3)/(NA.4) verwendet werden.

**NA.5.4.8 — Ausgangswerte (S. 36):**
```
V_sto = Q_sto / (ρ_w · c_w · (ϑ_sto,max − ϑ_w,c))                    (NA.5)
```

**NA.5.7.1 — Korrektur von Gl. (16) (S. 38):**
> „Der Koeffizient in EN 12831-3, Gleichung (16) für die Bestimmung der Zeitkonstanten des Speichers ist **fehlerhaft**. Die Gleichung muss lauten:
> τ = m_sto · c_w / (U_HE · A_HE) · **16,67**" (NA.6)

*Eigene Dimensionsprüfung: kg · kJ/(kg·K) / (W/(m²·K) · m²) = kJ/W = 1 000 s = 16,67 min. Die Korrektur ist rechnerisch bestätigt; der Originalwert 0,06 ist der Kehrwert-Fehler.*

**NA.5.7.2 — Klarstellung zum Flussdiagramm (S. 38):**
> „Zum besseren Verständnis des Flussdiagramms … ist die Feststellung notwendig, dass **Φ_eff auch negative Werte annehmen kann**. Dies ist dann der Fall, wenn die Verluste des Speichers und der Verteilleitungen die Ladeleistung übersteigen, z. B. auch bei Ladepausen. Des Weiteren kommt es zu Verzögerungen des Ladevorgangs durch Fühleranordnung und Trägheit des Wärmeerzeugers und der Verteilung, diese sind im Algorithmus zu berücksichtigen."

### 4.6 A100-Flussdiagramm (S. 39) — korrigierte Fassung

Das überarbeitete Ablaufdiagramm (Querformat, letzte Seite vor Literaturhinweisen) unterscheidet sich vom Bild 14 der Hauptnorm:

| Element | EN 12831-3, Bild 14 (S. 36) | A100, Bild S. 39 |
|---|---|---|
| Initialisierung | Q_sto,0 = Q_sto,Start | Q_sto,0 = Q_sto,max **und t_PowerOn = 0** |
| Schleife | „für i = 0 bis **440**" (offensichtlicher Tippfehler) | „for i = 0 bis **1440**" |
| Verzögerungsprüfung | t_po < t_lag,HG + t_lag,dis | (i − t_PowerOn) < t_lag,HG + t_lag,dis |
| Φ_eff bei „Speicher voll" bzw. Verzögerung | Φ_eff = 0 | Φ_eff,i = **− Φ_w,Sto,i − Φ_w,dis,i** (negativ, nur Verluste) |
| Bilanz | Q_sto,i+1 = Q_sto,i − Q_w,Sto − Q_w,dis + Q_eff | Q_sto,i+1 = Q_sto,i + Q_eff,i (Verluste bereits in Φ_eff) |
| Zusatzbedingung (Kasten) | — | Q_Sto und Φ_N so wählen, dass zu jedem i gilt: Q_Sto,i − Q_W,b,i ≥ Q_Sto,min (mit Q_Sto,min = 0 bei nicht gemischtem Speicher) |

**Als teilweise unsicher gekennzeichnet:** Das A100-Diagramm ist eine gedrehte Querformat-Grafik in mäßiger Auflösung. Die Zweigbedingungen und die Verzweigung „mixed storage ja/nein" habe ich sicher gelesen; die genaue Behandlung von Q_W,b,i in der Bilanz-Box ist im Diagramm **nicht** aufgeführt (dort steht nur `Q_sto,i+1 = Q_sto,i + Q_eff,i`), während Gl. (NA.3) im Fließtext den Term −Q_w,b,i enthält. **Für die Implementierung ist Gl. (NA.3) maßgeblich**, weil sie physikalisch schlüssig ist.

### 4.7 A100 — Verfahren zur Profilerhebung (NA.5.2.2, S. 8–13)

Für WP-Plan als Import-Spezifikation direkt verwertbar.

**Randbedingungen der Messung (NA.5.2.2.2, S. 10):**
- Messintervall **höchstens 1 Minute**
- Abtastrate möglichst hoch; kürzere Intervalle auf Minutenbasis mitteln
- Daten **elektronisch** aufzeichnen
- Aufzeichnungsdauer **mindestens zwei Wochen** (S. 8: „über einen Zeitraum von mindestens 2 Wochen empfehlenswert … sollte eine Nutzung des Auslegungsfalls der Anlage wiedergeben (z. B. keine Ferienzeit in Wohngebäuden)")
- Stromversorgung der Messeinrichtung gesichert
- Messort dokumentieren (Bild NA.2 durchmischter Speicher / NA.3 Ladespeicher / NA.4 Durchflusssystem)
- Zweite synchrone Messung nötig, um PWH von PWH-C (Zirkulation) zu trennen
- PWC- und PWH-Temperaturen mitprotokollieren (sonst bei zeitweise reduzierter TW-Temperatur „fälschlich höheres Messergebnis")
- Messfehler mechanischer Zähler 3 % bis 5 %

**Dateiformat (NA.5.2.4, S. 13) — vorgeschrieben CSV (DOS-Format) oder XML:**
- Header: Dateiname (korrespondierend mit Beschreibungsdatei); Zeitraum minutengenau `20150503, 0859, 20150520, 1653` (max. 1 Jahr, min. 24 Stunden); Angabe zur Datenherkunft (z. B. „Minutenwerte gemittelt aus 10 Sekundenwerten"); Angabe zur Temperaturkorrektur (z. B. „korrigiert auf 10/60 °C")
- Daten im Minutentakt in der Reihung: **Zeit / Volumenstrom / Temperatur PWC / Temperatur PWH / Temperatur PWH-C (falls vorhanden)**
- Durchfluss in l/s mit 4 Stellen und 1 Nachkommastelle (z. B. `12345.7`), Temperaturen in °C mit 1 Nachkommastelle
- 60 × 24 Datensätze je Tag, durch „;" getrennt
- Mehrere Tage je Datei zulässig („Somit kann die Software optional mit mehreren Tagen den Verlauf an einer Trinkwassererwärmungsanlage simulieren")

**Objektbeschreibung (NA.5.2.2.3, S. 11) — Pflichtfelder a) bis k):** Dateiname, Gebäudetyp, Gebäudenutzung, Größenordnung (Personen / Betten / Leerstand-Auslastung / Nutzungsfrequenz), sanitärtechnische Ausstattung (Anzahl Entnahmestellen mit PWH-Verbrauch, Standard mittel/gehoben/Luxus, Sondereinrichtungen), Trinkwassertemperaturen, Zeitraum, Ort der Messung, Messergebnisse, Besonderheiten (Feiertage/Stromausfall/Ferienzeit), Raumbuch Sanitär empfohlen. Als separate PDF-Datei mit korrespondierendem Dateinamen.

### 4.8 A100 — Vereinfachung für kleine Wohngebäude (NA.5.6, Tab. NA.7, S. 37)

Anwendbar für Wohngebäude **bis 6 Wohneinheiten**.

| Parameter bzw. Variable | Vereinfachung | Einheit |
|---|---|---|
| Referenzbedarfsprofil | Für Einfamilienhäuser, Doppelhaushälften und Mehrfamilienhäuser bis zu 6 Wohneinheiten: Referenzbedarfsprofil NA.5.2.6.16 (**N = 2**) | — |
| V_day | Der Tagesbedarf an Trinkwarmwasser ist Tabelle 5.4 zu entnehmen (Werte zwischen 30 l/Pd und 60 l/Pd) und mit der Personenanzahl zu multiplizieren | l/d |
| h_Sensor/h_sto | Dieser Wert wird auf **0,6** festgelegt. Die Position des Temperatursensors für die Nacherwärmung des Speichers wird auf 60 % der Speicherhöhe festgelegt | — |
| f_L | Der Lastfaktor ist DIN EN 12831-3:2017-09, Tabelle B.7 zu entnehmen | — |
| q_sb,sto | Die Bereitschaftswärmeverluste des Speichers sind DIN EN 12831-3:2017-09, Tabelle B.8 zu entnehmen | kWh/d |
| ϑ_w,sto,max | Die maximale Speichertemperatur ist mit **60 °C** festzulegen | °C |
| q'_dis | Die längenbezogenen Wärmeverluste der Rohrleitungen sind DIN EN 12831-3:2017-09, Tabelle B.9 zu entnehmen | W/m |
| Q_sto,0 | Q_sto,0 = Q_sto,max — der Speicher ist zu Beginn der Berechnung **zu 100 % geladen** | kWh |
| Σ t_power,on | Die Betriebszeit des Wärmeerzeugers für die Trinkwassererwärmung pro Tag ist **auszuweisen** | h/d |

**Widersprüche im Entwurf (bitte in WP-Plan als Hinweis führen):**
- Der Fließtext (S. 37) sagt „**Für diesen Fall kann das Bedarfsprofil nach DIN 4708 mit der Bedarfskennzahl N = 2 verwendet werden (siehe NA.5.2.6.15)**", die Tabelle NA.7 verweist dagegen auf „Referenzbedarfsprofil **NA.5.2.6.16**" (das ist N = 4). Gemeint ist mit hoher Wahrscheinlichkeit N = 2 (NA.5.2.6.15).
- Der Verweis „Tabelle 5.4" existiert im Dokument nicht; der Wertebereich 30–60 l/(P·d) passt zu Tab. NA.4 (MFH 30, EFH 40, EFH gehoben 60).

Die Vorgabe **Σ t_power,on ist auszuweisen** ist für ein Wärmepumpen-Planungstool bemerkenswert: sie zwingt dazu, die tägliche TWW-Betriebszeit des Erzeugers als Ergebnisgröße auszugeben (relevant für Sperrzeiten, Takten, Kombination mit Heizbetrieb).

---

## 5. A1-Entwurf — Änderungen an der Hauptnorm

Der A1-Entwurf ist sehr kurz (3 Änderungspunkte) und deckt sich inhaltlich teilweise mit A100.

**1) Zu 6.4.2.9 „Effektive Energie und effektive Leistung Q_eff und Φ_eff" (S. 4)**

- Nach Gl. (14) einfügen: *„Φ_N ist entweder geringer als die Nennleistung des Wärmeerzeugers oder die Nennleistung des Wärmetauschers."*
  (englische Originalfassung eindeutiger: *„Φ_N is the lesser of either the nominal power of the heat generator or the nominal power of the heat exchanger."* → **Φ_N = min(Erzeuger, Wärmeübertrager)**; die deutsche Übersetzung im Entwurf ist missverständlich. Inhaltsgleich mit A100 NA.5.4.5.)
- Nach Gl. (15) einfügen: *„Die Durchschnittstemperatur des Speichers ϑ_sto,m für jeden neuen Zeitschritt kann auch mithilfe der Daten der vorherigen Zeitschritte errechnet werden, indem die Gleichung (4) angemessen angewendet wird."* (→ rekursive Bilanz statt geschlossener e-Funktion; entspricht dem Ansatz A100 NA.3/NA.4)
- **Gleichung (16) ersetzen durch:** τ = m_Sto · c_w / (U_HE · A_HE) · **16,67** (identisch mit A100 Gl. NA.6)

**2) Zu 6.5.3 „Flächenbezogener Energiebedarf" (S. 4)**

- **Gleichung (22) ersetzen durch:** Q_W = **q**_W,A,day · A · n_day
  (Änderung Q_W,A,day → q_W,A,day, d. h. Kleinbuchstabe für die *spezifische* Größe — Konsistenz mit der Symbolik in Tab. B.3, wo bereits q_w,b,d steht. Rein notationell, keine inhaltliche Änderung.)

**3) Zu B.2.2 „Energiebedarf … beruhend auf dem erforderlichen Volumen" (S. 4–5)**

- Nach Tab. B.4 einfügen: *„Die Werte in Tabelle B.4 dienen als Referenz für Warm- und Kaltwassertemperaturen von **60 °C bzw. 13,5 °C** und müssen umgewandelt werden, wenn sie mit einer anderen Referenztemperatur verwendet werden."*
- **Kopfzeile Tab. B.5 ersetzen:** „Art des Gebäudes | **V_W,P,day** [Liter erwärmtes Trinkwasser je Person und Tag]" (Korrektur des falschen Index f → P)
- Nach Tab. B.5 einfügen: *„Die Werte in Tabelle B.5 dienen als Referenz für Warm- und Kaltwassertemperaturen von **45 °C bzw. 10 °C** und müssen umgewandelt werden, wenn sie mit einer anderen Referenztemperatur verwendet werden."*

**Zusammenfassend:** A1 ändert **keine Verfahrenslogik** und **keine Profile**. Er behebt drei Fehler (τ-Koeffizient, Tab.-B.5-Index, Φ_N-Definition) und stellt die Temperaturreferenzen der Tab. B.4/B.5 klar. Die Δθ-Umrechnung ist damit normativ gefordert:
```
V(θ_draw,neu) = V_Tabelle · (θ_draw,Tab − θ_c,Tab) / (θ_draw,neu − θ_c,neu)
```

---

## 6. Abgleich mit den bestehenden Konzeptannahmen

### a) „Das Summenlinienverfahren liefert Speichervolumen + Ladeleistung als Wertepaar-Kurve in Minutenschritten."

**Status: PRÄZISIERT** (teils bestätigt, teils zu korrigieren).

| Teilaussage | Bewertung | Fundstelle |
|---|---|---|
| Minutenschritte | **BESTÄTIGT** | 5.1 S. 15 („Zeitschritt von 1 min"); 6.2 S. 17; 6.4.1 S. 18 („1 440 Kreisläufe"); Index-Tabelle 3, Index *i* („ein Zyklus je Minute") |
| Ergebnisgrößen sind V_sto und Φ_eff | **BESTÄTIGT** | Tab. 4 „Ausgabedaten" S. 16: Φ_W,eff [W] → M8-8/M3-8; V_sto [m³] → M8-7 |
| „Wertepaar-Kurve" als direktes Normergebnis | **PRÄZISIERT / so nicht in der Norm** | Der Normalgorithmus (Bild 14, 6.4.3.3) ist ein **Nachweis für ein gegebenes Paar**. Die Kurve entsteht erst durch iterative Parametervariation gemäß 5.1 d)/e) und 6.4.3.3 S. 36 („so lange erhöht werden, bis die Bedingung … erfüllt ist"). A100 NA.5.7.2: „Ist einer dieser Werte (oder beide) nicht bekannt, dann ist dieser **iterativ** zu bestimmen." |

**Konsequenz für WP-Plan:** Die Wertepaar-Kurve ist eine legitime, normkonforme *Darstellung*, aber als **eigene Erweiterung** zu kennzeichnen. Implementierung: äußere Schleife über V_sto (oder Φ_N), innere Schleife = 1 440 Minutenschritte, Bisektion auf den jeweils anderen Parameter bis zum Grenzfall min_i(Versorgung_i − Bedarf_i) = Q_sto,min.

### b) „Referenzfall 11-WE-MFH: DIN 4708 ≈ 60 kW, EN 12831-3 ≈ 30 kW, realistisch ≈ 20 kW"

**Status: RICHTUNG BESTÄTIGT, ZAHLEN NICHT BELEGT.**

**Was die Dokumente belegen:**
- Die qualitative Aussage steht wörtlich in A100 (S. 14): DIN-4708-Bedarfswerte „führen in der Regel zu einer **großzügigen leistungsmäßigen Auslegung** des Wärmeerzeugers. Durch eine realistische Anpassung des Tagesbedarfs … kann nach dem Verfahren der EN 12831-3 die Trinkwassererwärmungsanlage **kleiner dimensioniert** werden."
- Der Mechanismus ist nachvollziehbar: Die DIN-4708-Profile (NA.5.2.6.15–18) konzentrieren den **gesamten** Tagesbedarf in die Bedarfsperiode 2·T_N zwischen ca. 00:00 und 06:00 Uhr — eine extrem steile Summenlinie. Das reale MFH-Messprofil (NA.5.2.6.14) verteilt den Bedarf über den ganzen Tag mit deutlich flacherer Kennlinie.
- Der Bedarfsunterschied ist quantifizierbar: DIN 4708 N = 10 → **53,8 l/(P·d)** bzw. **2,2 kWh/(P·d)**; reales MFH-Messprofil NA.5.2.6.14 → **19,7 l/(P·d)** bzw. **1,1 kWh/(P·d)**; Tab. NA.4 MFH-Richtwert → **30 l/(P·d)** bzw. **1,2 kWh/(P·d)**. Das ist ein Faktor **2 bis 2,7** allein beim Tagesbedarf.

**Was die Dokumente NICHT enthalten:**
- **Kein Referenzfall „11 WE"**, keine kW-Vergleichsrechnung, kein durchgerechnetes Auslegungsbeispiel. Der IKZ-Fachaufsatz ist keine Normquelle; die drei Zahlen 60 / 30 / 20 kW lassen sich aus den vorliegenden Dokumenten **weder bestätigen noch widerlegen**.
- Das einzige Objekt mit konkreter Anlagentechnik ist NA.5.2.6.14: **24 WE, 35 Personen, 100 kW Gaskessel, 500 l Ladespeicher, gemessener Bedarf nur 689 l/d bzw. 40 kWh/d, Spitzendurchfluss 1 min 0,39 l/s.** Das ist ein starkes Indiz für massive Überdimensionierung im Bestand (100 kW installiert bei 0,39 l/s Spitze ≈ rechnerisch 0,39 · 1 · 4,2 · 50 K ≈ 82 kW momentan, aber nur über wenige Minuten und bei vorhandenem Speicher) — **belegt aber nicht die 60/30/20-Zahlen**.

**Empfehlung:** Die IKZ-Zahlen in WP-Plan nicht als Normwerte führen. Stattdessen einen eigenen Vergleichsrechner: gleiche Anlage einmal mit dem DIN-4708-Profil (N nach WE-Zahl), einmal mit dem A100-MFH-Messprofil, einmal mit Tab.-NA.4-Richtwerten — die Norm liefert alle drei Datensätze, der Vergleich ist dann selbst gerechnet und belastbar.

### c) „Die Norm deckt auch Nichtwohngebäude ab."

**Status: BESTÄTIGT.**

Abschn. 1 Anwendungsbereich (S. 7): „Die Berechnung des Energiebedarfs für Anlagen zur Trinkwassererwärmung gilt für **Wohn- und Nichtwohngebäude**, ein sonstiges Gebäude oder für einen Bereich eines Gebäudes." Begriff 3.5 Anm. 1 (S. 11): „Die Verwendung des Begriffs erwärmtes Trinkwasser gilt auch für **Nichtwohngebäude und ihre Anlagen**."

**Abgedeckte Nichtwohn-Typen mit Profilen bzw. Kennwerten:**

| Nutzungstyp | Stundenprofil (Tab. B.2) | Minutenprofil (A100) | Kennwert Tab. B.3 | Kennwert Tab. B.4 | Kennwert Tab. NA.4 | V̇_D-Konstanten (Tab. B.14) |
|---|---|---|---|---|---|---|
| Krankenhaus / Bettenstation | ✔ | ✔ (4 Objekte: 60/242/590 Betten, Funktionsgebäude) | ✔ | ✔ (3 Varianten) | ✔ | ✔ |
| Hotel | – | ✔ (20 Zi., 300 Zi.) | ✔ (3 Klassen) | ✔ (1–4 Sterne, ±Wäscherei) | ✔ (3 Klassen) | ✔ |
| Gastronomie / Restaurant / Mensa / Großküche | – | ✔ (Mensa, Hotelküche) | ✔ | ✔ (4 Varianten) | ✔ | – |
| Seniorenheim / Pflegeheim / Heim | ✔ (Wohnheime für Senioren) | ✔ (2 Objekte) | ✔ | – | ✔ | ✔ |
| Studentenwohnheim | ✔ | ✔ | – | – | – | – |
| Schule / Bildungseinrichtung | – | – | ✔ (mit/ohne Duschen) | „nicht berücksichtigt" | ✔ | ✔ |
| Bürogebäude | – | – | ✔ | „nicht berücksichtigt" | ✔ | ✔ |
| Einzelhandel/Kaufhaus, Läden | – | – | ✔ | „nicht berücksichtigt" | ✔ | – |
| Werkstatt/Industriebetrieb | – | – | ✔ | „nicht berücksichtigt" | ✔ | – |
| Kaserne | – | – | ✔ | – | ✔ | – |
| Sportstätte mit Duschen | – | – | ✔ | ✔ (je Dusche) | ✔ | – |
| Schwimmbad | – | ✔ | – | – | – | – |
| JVA | – | ✔ | – | – | – | – |
| Sauna, Labor, Fitnessraum | – | – | – | – | ✔ (neu in A100) | – |
| Bäckerei, Friseur, Fleischerei, Wäscherei, Brauerei, Molkerei | – | – | ✔ | – | ✔ | – |
| Theater/Hörsäle, Lager, Transport, Sonstige | – | – | – | „nicht berücksichtigt" | – | – |

**Lücken:** Für Büros, Schulen, Läden, Bildungseinrichtungen gibt es zwar Tagesbedarfs-Kennwerte, aber **kein Tagesprofil** — weder in Tab. B.2 noch in A100. Für diese Nutzungen muss der Tagesgang selbst konstruiert werden (siehe d).

### d) „Auslegungs-Tagesgänge je Nutzungsart müssen als Eigenannahme konstruiert werden, weil die Norm sie nicht liefert."

**Status: ÜBERWIEGEND WIDERLEGT, aber differenziert.**

**Widerlegt für:**
- 5 Gebäudekategorien mit **Stundenprofilen** in Tab. B.2 (EFH, Wohnungen, Seniorenwohnheim, Studentenwohnheim, Krankenhaus) — sofort implementierbar, Zahlen liegen vollständig vor
- 7 Zapfprofile nach EN 50440 in Tab. B.1 (XXS…XXL) — vollständig vorhanden
- 18 **Minuten-Referenzprofile** in A100 NA.5.2.6 — normativ vorgesehen und benannt

**Präzisiert (wichtige Einschränkung):**
Die A100-Minutenprofile sind im Normtext **nur als Grafik + Kennwertebox** enthalten. Die Zahlenwerte liegen laut NA.5.2.5 „auf CD-ROM bzw. als Zip-Datei zum Download" vor. **Ohne diese Datendateien kann WP-Plan die 18 Referenzprofile nicht nutzen.** Beschaffung ist Voraussetzung. (Als unsicher gekennzeichnet: ob und in welcher Form die Dateien nach Abschluss des Entwurfsverfahrens bereitgestellt werden, geht aus dem Entwurf nicht hervor.)

**Weiterhin gültig (Eigenkonstruktion nötig) für:**
- Büro, Schule, Einzelhandel, Werkstatt, Kaserne, Bäckerei/Friseur/Fleischerei/Wäscherei/Brauerei/Molkerei, Theater, Lager — kein Profil in irgendeinem der drei Dokumente
- Gemischt genutzte Objekte

Die Norm **erlaubt und beschreibt** die Eigenkonstruktion ausdrücklich, A100 NA.5.2.2 (S. 8): „So bleibt es dem erfahrenen Planer überlassen, **eigene Bedarfsprofile** zur Auslegung der Trinkwassererwärmungsanlage heranzuziehen." und NA.5.2.3 „Manuelle Profilerstellung" (S. 12) mit dem vollständig durchgerechneten Beispiel Tab. NA.3 (siehe Abschn. 2.5 oben). Die Konzeptannahme ist also nicht falsch, sondern **zu eng** formuliert: Für die abgedeckten Nutzungsarten liefert das Normwerk Profile; für die übrigen ist die Eigenkonstruktion normkonform vorgesehen und methodisch beschrieben.

### e) Kaltwassertemperatur-Ansatz

**Status: PRÄZISIERT — fester Wert als Vorgabe, Jahresgang optional zugelassen, aber nicht beziffert.**

| Aspekt | Regelung | Fundstelle |
|---|---|---|
| EN-Vorgabewert | ϑ_w,c = **10 °C** | Tab. B.6, S. 51 |
| Nationaler Wert D | ϑ_w,c = **10 °C** (unverändert) | Tab. NA.5, A100 S. 34 |
| Ursprung im Datenfluss | „Von M1-13" (Klimadaten-Modul) | Tab. Abschn. 6.3.4, S. 18 |
| Jahresgang | **Zugelassen, nicht vorgeschrieben** | 6.5.2.3, S. 38: „In einigen Ländern sind die Schwankungen … so groß, dass sie eine signifikante Auswirkung … haben. Um örtliche Schwankungen zu berücksichtigen, können nationale Werte angewendet werden, und es kann **mehr als ein Satz von Temperaturwerten** angewendet werden, um die Unterschiede … in verschiedenen geographischen Regionen widerzuspiegeln." |
| Standardansatz für den Wert | „Ein Standardwert für Kaltwasser kann die **jährliche durchschnittliche Außenlufttemperatur** sein." | 6.5.2.3, S. 38 |
| Zeitraumbildung | „Werden in den Berechnungen unterschiedliche Kaltwasser-Zulauftemperaturen verwendet, dann sollte der wöchentliche, monatliche oder jährliche Bedarf … auf der **Anzahl der Tage der jeweils verwendeten Kaltwasser-Zulauftemperaturen** beruhen. Ein nationaler Anhang enthält die Anzahl der Tage." | 6.5.2.5, S. 39 |
| Deutscher NA | **Enthält keine Tagesaufteilung und keinen Jahresgang** — nur den festen Wert 10 °C | A100, NA.5.3.2 |

**Fazit:** Für die **Auslegung** (Summenlinie) ist ein fester Wert vorgesehen: 10 °C. Ein Jahresgang ist normativ nur für die **Bedarfsberechnung** über längere Zeiträume relevant und in Deutschland nicht ausgefüllt. Zusätzlich zu beachten: die Tabellenwerte haben unterschiedliche Referenz-Kaltwassertemperaturen (10 °C in Tab. B.5/NA.4/NA.5; **13,5 °C** in Tab. B.4) — eine Umrechnung ist nach A1 zwingend.

---

## 7. Implementierungs-Spezifikation für das C#-Modul

### 7.1 Formelsammlung

Einheiten wie in der Norm; abweichende/inkonsistente Angaben sind markiert.

**(1) Bedarfssummenlinie** — 6.4.1, S. 18
```
Q_W;b;i = Σ(t=1..i) Q_W;b;t          mit i = 1, 2, …, i_max  (i_max = 1440)
```
| Symbol | Bedeutung | Einheit |
|---|---|---|
| Q_W;b;i | kumulierter Energiebedarf von t = 1 bis i | kWh |
| Q_W;b;t | Energiebedarf im Zeitschritt t | kWh/min |
| i | Kreislauf-/Berechnungsschritt | – |

**(2) Energiebedarf je Minute** — S. 19
```
Q_W;b;t = V_t · ρ_w · c_w · (ϑ_w;draw − ϑ_w;c) · 1/3600  =  Q_W;b · x_h / 60
```
| Symbol | Bedeutung | Einheit |
|---|---|---|
| V_t | in Minute t gezapftes Volumen bei ϑ_w,draw | l |
| ρ_w | Dichte | kg/l |
| c_w | spez. Wärmekapazität (4,2) | kJ/(kg·K) |
| ϑ_w;draw | Mischwassertemperatur an der Zapfstelle | °C |
| ϑ_w;c | Kaltwassertemperatur | °C |
| x_h | Anteil des in Stunde h gezapften Volumens (Σx_h = 1) | – |

**(3) Minutenvolumen aus Stundenprofil** — S. 20
```
V_t = x_h · V_day / 60
```

**(4) Maximale Speicherkapazität** — 6.4.2.3.2, S. 27
```
Q_sto;max = V_sto · ρ_w · c_w · (ϑ_w;sto;max − ϑ_w;c) · f_l · 1/3600
```
| Symbol | Bedeutung | Einheit |
|---|---|---|
| V_sto | Speicher-Innenvolumen | l |
| ϑ_w;sto;max | max. Speichertemperatur (Auslegung) | °C |
| f_l | Ladungsfaktor (Tab. B.7) | – |

**(5) Minimale Speicherkapazität (nur gemischte Speicher)** — 6.4.2.4.1, S. 28
```
Q_sto,min = V_sto · ρ_w · c_w · (1 − h_sensor/(2·h_sto)) · (ϑ_w,draw − ϑ_c) · f_l · 1/3600
```
| Symbol | Bedeutung | Einheit |
|---|---|---|
| h_sensor | Einbauhöhe des Temperaturfühlers ab Speicherboden | m |
| h_sto | Gesamthöhe des Speichers (innen) | m |

> **Fehler im Normtext:** Die Symbolerläuterung zu Gl. (5) definiert „h_sto = die **Temperatur** des Speichers [m]" — offensichtlicher Redaktionsfehler; korrekt ist die Gesamthöhe (vgl. Gl. 10 und Bild 13, S. 31).
> **Für Ladespeichersysteme gilt Q_sto,min = 0** (6.4.2.4.2, S. 29).

**(6) Speicher-Bereitschaftsverlust je Minute** — 6.4.2.5, S. 29
```
Q_W;sto;t = q_sb;sto · (ϑ_w;sto;max − ϑ_a) / 45 · 1/1440
```
| Symbol | Bedeutung | Einheit |
|---|---|---|
| q_sb;sto | Bereitschaftsverlust je Tag (Hersteller, EN 12897, Tab. B.8) | kWh/d |
| ϑ_a | Umgebungstemperatur des Speichers | °C |

Der Nenner 45 ist die Referenz-Übertemperatur der Bereitschaftsverlust-Prüfung. **q_sb,sto = 0 bei Durchflussanlagen.**

**(7) q_sb,sto aus Ökodesign-Kennwert S** — S. 29
```
q_sb,sto = Φ_W;sto,t · 0,024          (Φ in W, entspricht "S" nach VO (EU) 812/2013)
```

**(8) Verteilungsverlust je Minute, detailliert** — 6.4.2.6, S. 30
```
Q_W;dis;t = Σ_j [ U_dis;j · l_dis;j · (ϑ_m;j − ϑ_a;j) ] · t · 1/60000
```
| Symbol | Bedeutung | Einheit |
|---|---|---|
| U_dis;j | linearer Wärmedurchgangskoeffizient Rohrabschnitt j | W/(m·K) |
| l_dis;j | Länge Rohrabschnitt j | m |
| ϑ_m;j | mittlere Wassertemperatur im Abschnitt j (≈ ϑ_m = 50 °C) | °C |
| ϑ_a;j | Umgebungstemperatur Abschnitt j | °C |

**(9) Verteilungsverlust je Minute, vereinfacht** — S. 30
```
Q_W;dis;t = q'_dis · l_dis · 1/60000
```
q'_dis nach Tab. B.9: 7 W/m (Δθ = 35 K) bzw. 11 W/m (Δθ = 50 K), bei s = d und λ = 0,035 W/(m·K).

**(10) Einschaltpunkt / Restkapazität** — 6.4.2.7.1, S. 31
```
Q_sto;on = Q_sto,max · (1 − h_sensor / h_sto)
```
Hysterese wird ausdrücklich vernachlässigt.

**(11) Gesamte Zeitverzögerung** — 6.4.2.8, S. 32
```
t_lag = t_lag,HG + t_lag,dis
```
Sprungantwort-Annahme: volle Leistung erst **nach** Ablauf von t_lag.

**(12) Zeitverzögerung Wärmeerzeuger** — S. 32
```
t_lag,HG = [ (m_W,HG · c_W + m_M,HG · c_M · f_HG,ϑ) · (ϑ_W,sto,max − ϑ_U) ] / (Φ_HG · f_HG,Q · 60)
```
| Symbol | Bedeutung | Einheit |
|---|---|---|
| m_W,HG | Wasserinhalt des Wärmeerzeugers | kg |
| m_M,HG | Masse des Wärmeerzeugers (Herstellerdaten) | kg |
| c_M | spez. Wärmekapazität Erzeugermaterial (0,5) | kJ/(kg·K) |
| f_HG,ϑ | Faktor ungleiche Temperaturverteilung (Tab. B.12) | – |
| f_HG,Q | Faktor Zündvorgang/Leistungsanpassung (Tab. B.13) | – |
| Φ_HG | Nennleistung des Wärmeerzeugers | kW |
| ϑ_U / ϑ_a | Umgebungstemperatur | °C |

> Die Symbolliste führt zusätzlich m_W,dis (Wasserinhalt Verteilleitungen) auf, das in Gl. (12) **nicht vorkommt** — Redaktionsfehler. t_lag,dis darf vernachlässigt werden, wenn Erzeuger und Speicher im gleichen Raum stehen (S. 33); Tab. B.11 setzt t_lag,dis = 0 für alle Anlagen.

**(13) Effektive Leistung, Ladespeichersystem** — 6.4.2.9, S. 33
```
Φ_eff = Φ_N − Φ_W;sto − Φ_W;dis
```

**(14) Effektive Leistung, gemischtes Speichersystem** — S. 33
```
Φ_eff = Φ_N · [ 1 − (ϑ_Sto,m(t) − ϑ_c) / (ϑ_ch,HG − ϑ_c) ] − Φ_W;Sto − Φ_W;dis
```
mit Φ_N = **min**(Nennleistung Erzeuger, Nennleistung/Dauerleistung Wärmeübertrager) — Klarstellung nach A1 Abschn. 1 und A100 NA.5.4.5.

> **KRITISCHE INKONSISTENZ — bitte bei der Implementierung beachten:**
> Gl. (14) im Fließtext enthält den Term `1 − (…)`. Das **Ablaufdiagramm Bild 14** (S. 36) **und** das korrigierte A100-Ablaufdiagramm (S. 39) zeigen dagegen `Φ_eff = Φ_N · [ (ϑ_Sto,m − ϑ_c)/(ϑ_ch,HG − ϑ_c) ] − Φ_w,Sto − Φ_w,dis` — **ohne** das führende „1 −".
> Physikalisch richtig ist die Fließtext-Fassung: Mit steigender Speichertemperatur sinkt die Wärmeaustauschrate; bei ϑ_Sto,m = ϑ_ch,HG wird der Term 0 und Φ_eff = −(Verluste). Der Normtext bestätigt das explizit (S. 26): „In gemischten Speicheranlagen reduziert sich die Wärmeaustauschrate bei graduell ansteigender Temperatur im Speicher im Ladestatus und somit verringert sich die Neigung der Linie graduell."
> **Empfehlung: Gl. (14) mit `1 −` implementieren, die Diagrammfassung als Fehler behandeln, Abweichung dokumentieren.** (Als unsicher gekennzeichnet, da beide Entwurfsfassungen der Grafik das „1 −" nicht zeigen — ein Rückfrage-/Einspruchspunkt.)

**(15) Mittlere Speichertemperatur (e-Funktion)** — S. 34
```
ϑ_Sto,m(t) = ϑ_Sto,m,t0 + (ϑ_ch,HG − ϑ_Sto,m,t0) · (1 − e^(−t/τ))
```

**(16) Zeitkonstante des Speichers — KORRIGIERTE FASSUNG** (A1 + A100 NA.6)
```
τ = m_sto · c_w / (U_HE · A_HE) · 16,67          [min]
```
Original 2017: Faktor 0,06 — **fehlerhaft**, nicht verwenden.

**(17) Effektive Energie je Zeitschritt** — S. 35
```
Q_eff = Φ_eff · t          (t = 60 s/min = 1 min)
```
A100-Diagramm konkretisiert: `Q_eff = Φ_eff · 60 s` in kWh, d. h. Q_eff[kWh] = Φ_eff[kW] / 60.

**(18) Direkter Durchfluss ohne Speicher** — 6.4.3.5, S. 37
```
Φ_eff = V̇_D · ρ_w · c_w · (ϑ_W,draw − ϑ_c)
```
V̇_D in l/s, ρ in kg/l, c in kJ/(kg·K) → Φ in kW.

**(19) Energiebedarf aus Volumen (Stundenschritt)** — 6.5.2.1, S. 38
```
Q_W,nd = V_t · c_W · ρ_W · (ϑ_W;draw − ϑ_W;c) · 1/1000
V_t = V_W,day · x_h
```
> **Einheiten-Inkonsistenz im Normtext:** die Erläuterung gibt c_W in [kWh/(kg·K)], ρ_W in [kg/m³] und V_t in [Liter/Stunde] an — damit geht der Faktor 1/1000 nicht auf. Praktisch anzuwenden: `Q[kWh] = V[l] · 1 kg/l · 4,2 kJ/(kg·K) · Δϑ / 3600`. **Empfehlung: intern durchgängig SI-konsistent mit c_w = 4,2 kJ/(kg·K), ρ = 1 kg/l, /3600 rechnen** und die Normformeln nur als Referenz dokumentieren.

**(20)/(21) Tagesvolumen** — 6.5.2.4, S. 39
```
V_W,day = V_W,P,day · n_P            (personenbezogen)
V_W,day = V_W,f,day · f              (einheitenbezogen; f = Betten/Sitze/Gäste/… oder Nutzfläche)
```

**(22) Flächenbezogener Energiebedarf — KORRIGIERTE FASSUNG** (A1 Abschn. 2)
```
Q_W = q_W,A,day · A · n_day
```

**(B.1)–(B.5)** äquivalente Personenzahl — siehe Abschn. 3.4 oben.

**(B.6) Auslegungsdurchfluss** — S. 55
```
V̇_D = a · (Σ V̇_A)^b − c
```
Konstanten Tab. B.14; V̇_A nach EN 806-3 bzw. national. A100 NA.5.4.9: entspricht dem Spitzendurchfluss V̇_S nach DIN 1988-300, gleiche Koeffizienten.
> Der Fließtext verweist irrtümlich auf „Tabelle B.2" statt Tab. B.14.

**(NA.1)/(NA.2)** Wärmeübertragerfläche — siehe 4.5.

**(NA.3)/(NA.4)** Speicherbilanz und mittlere Temperatur — **empfohlene Implementierungsvariante** statt (15)/(16):
```
Q_sto,i+1   = Q_sto,i − Q_w,b,i + Q_eff,i
ϑ_sto,m,i+1 = Q_sto,i+1 / (V_sto · ρ_w · c_w) + ϑ_w,c
```
> Einheiten-Hinweis: Damit ϑ in °C herauskommt, muss der Nenner V_sto·ρ_w·c_w in kWh/K gebildet werden (V in l, ρ in kg/l, c in kJ/(kg·K), Division durch 3600). Der Entwurf gibt ρ in kg/m³ und V in l an — **inkonsistent**, in der Implementierung selbst normalisieren.

**(NA.5)** Startwert Speichervolumen — siehe 4.5.

### 7.2 Referenz-Algorithmus (empfohlene Fassung für WP-Plan)

Basis: EN Bild 14 + A100-Korrekturen (S. 38–39) + Gl. (NA.3)/(NA.4).

```
// Vorbereitung
Q_W_b[0..1439]                       // Minutenbedarf aus Profil, Gl.(2)/(3)
Q_sto_max = f(V_sto, ϑ_sto_max, ϑ_c, f_l)      // Gl.(4)
Q_sto_min = (gemischt) ? f(V_sto, h_sensor, h_sto, ϑ_draw, ϑ_c, f_l) : 0   // Gl.(5)
Q_sto_on  = Q_sto_max * (1 − h_sensor/h_sto)   // Gl.(10)
Q_sto[0]  = Q_sto_max                 // A100: 100 % geladen
t_PowerOn = 0

for i = 0 .. 1439:
    Q_loss_sto = Gl.(6)               // konstant je Minute
    Q_loss_dis = Gl.(8) oder (9)

    if (Q_sto[i] − Q_W_b[i] >= Q_sto_max):       // Speicher voll
        t_PowerOn = i
        Φ_eff = −(Φ_loss_sto + Φ_loss_dis)       // A100: negativ, nur Verluste
    else if (Q_sto[i] − Q_W_b[i] >= Q_sto_on):   // Einschaltpunkt noch nicht erreicht
        t_PowerOn = i
        Φ_eff = −(Φ_loss_sto + Φ_loss_dis)
    else if ((i − t_PowerOn) < t_lag_HG + t_lag_dis):   // Erzeuger noch nicht da
        Φ_eff = −(Φ_loss_sto + Φ_loss_dis)
    else:
        t_PowerOn = 0
        if (gemischter Speicher):
            Φ_eff = Φ_N * (1 − (ϑ_sto_m[i] − ϑ_c)/(ϑ_ch_HG − ϑ_c))
                    − Φ_loss_sto − Φ_loss_dis          // Gl.(14), MIT "1 −"
        else:
            Φ_eff = Φ_N − Φ_loss_sto − Φ_loss_dis      // Gl.(13)

    Q_eff = Φ_eff / 60                                  // kWh je Minute
    Q_sto[i+1]   = Q_sto[i] − Q_W_b[i] + Q_eff          // Gl.(NA.3)
    ϑ_sto_m[i+1] = Q_sto[i+1]/(V_sto*ρ*c) + ϑ_c         // Gl.(NA.4)
    Q_sto[i+1]   = min(Q_sto[i+1], Q_sto_max)           // Begrenzung (implizit)

// Nachweis
ok = für alle i: (Q_sto[i] − Q_W_b[i]) >= Q_sto_min
if (!ok) → V_sto und/oder Φ_N erhöhen und wiederholen
```

Zusätzlich auszugeben (A100 Tab. NA.7): **Σ t_power,on** = Anzahl Minuten mit Φ_eff > 0, in h/d.

### 7.3 Randbedingungen und Gültigkeitsgrenzen (Checkliste)

**Verfahren:**
1. Zeitschritt **1 min** zwingend für die Bemessung (6.2). Stundendaten sind zulässig, unterschätzen aber Spitzen (A100 NA.5.2.2.4).
2. Betrachtungszeitraum in der Regel **24 h = 1 440 Schritte**. „für i = 0 bis 440" in Bild 14 ist ein Tippfehler (A100: 1440).
3. Berechnungen beruhen auf einem **täglichen** Bedarf (5.2, S. 16). Wochen-/Monats-/Jahreswerte = Tageswert × Anzahl Tage (6.5.2.5, 6.5.3, 6.5.4).
4. **Hysterese** des Temperaturfühlers wird vernachlässigt (6.4.2.7.1, S. 31) — beeinflusst laut Norm Ein-/Ausschaltpunkte **und** die maximale Speicherkapazität.
5. Erzeugerleistung erst **nach** t_lag verfügbar (Sprungantwort, 6.4.2.8, S. 31).
6. Anwendungsbereich: Wohn- und Nichtwohngebäude, gemischte Speichersysteme, Ladespeichersysteme, Durchflusssysteme mit/ohne Energiespeicher, zentrale und dezentrale Anlagen (Bilder 5–11).
7. Das Energiespeicher-Verfahren (Speichermedium ≠ Trinkwasser, 3.1) darf mit Gl. (5) gerechnet werden „dabei wird davon ausgegangen, dass die Wärme **ohne Wärmeverlust** an das erwärmte Trinkwasser übertragen wird" (S. 28).
8. Warnung A100 NA.0: auch bei bestimmungsgemäßer Anwendung kurzfristige Untertemperierung bei außergewöhnlichen Bedarfssituationen möglich.
9. Abschn. 5.2, S. 16: „Bei Anwendung dieses Ansatzes sollte berücksichtigt werden, dass der Energiebedarf und das Lastprofil **nicht unbedingt ein Worstcast-Szenario widerspiegeln**."

**Verluste:**
10. Nur zirkulierende Leitungsabschnitte + Laderohre; **Stichleitungen sind ausgeschlossen** (6.4.2.6) — auch aus Tab. B.3, B.4 und den A100-Profilen.
11. q_sb,sto = 0 bei Durchflussanlagen; Q_sto,min = 0 bei Ladespeichersystemen.
12. Tab. B.8 gilt für Speicher mit **zwei** Anschlussrohren; +0,1 kWh/d je zusätzlichem Rohr.
13. Bivalente Speicher: nur das Bereitschaftsvolumen (oberhalb Unterkante oberer WÜT) zählt.

**Temperaturreferenzen (Umrechnung nach A1 zwingend):**
14. Tab. B.4 → 60 °C / 13,5 °C
15. Tab. B.5 → 45 °C / 10 °C
16. Tab. NA.4 (A100) → 45 °C / 10 °C
17. Tab. B.6 (EN-Vorgabe) → ϑ_draw 42 °C; Tab. NA.5 (D) → 45 °C; ϑ_c beide 10 °C
18. A100-Referenzprofile: gemessen bei PWC 10 °C / PWH 60 °C mit Zirkulation

**Nationale Anwendung:**
19. A100 **ersetzt Anhang B** (NA.5.1) — mit den in NA.5.4/NA.5.5 genannten Ausnahmen (B.7–B.13, B.4 Allgemeine Werte, B.3.6 gelten unverändert weiter).
20. Vereinfachungen nach Tab. NA.7 nur für Wohngebäude **bis 6 WE**.
21. A100 und A1 sind **Entwürfe** — Anwendung ist gesondert zu vereinbaren.
22. Messprofile: Intervall ≤ 1 min, Dauer ≥ 2 Wochen, CSV (DOS) oder XML, Objektbeschreibung a)–k) verpflichtend.
23. Spitzendurchfluss-Angaben der A100-Profile sind **nicht für Auslegungszwecke** zu verwenden (S. 14).

### 7.4 Liste der Fehler/Inkonsistenzen im Normwerk (für Code-Kommentare)

| # | Fundstelle | Problem | Behandlung |
|---|---|---|---|
| 1 | Gl. (16), S. 34 | Koeffizient 0,06 falsch | → 16,67 (A1, A100 NA.6) |
| 2 | Bild 14, S. 36 | „für i = 0 bis 440" | → 1 440 (A100-Diagramm) |
| 3 | Bild 14 / A100-Diagramm | Φ_eff-Formel ohne „1 −" | → Gl. (14) mit „1 −" verwenden |
| 4 | Bild 14, S. 36 | Φ_eff = 0 bei Verzögerung | → A100: Φ_eff = −Verluste |
| 5 | Gl. (5), S. 28 | h_sto als „Temperatur des Speichers [m]" beschrieben | → Gesamthöhe innen [m] |
| 6 | Gl. (14), S. 33 | Symbolliste nennt Φ_HE, Formel verwendet Φ_N | → Φ_N = min(Erzeuger, WÜT) (A1) |
| 7 | Gl. (12), S. 32 | m_W,dis erläutert, kommt in Formel nicht vor | ignorieren |
| 8 | Tab. B.5, S. 50 | Kopf „V_W;f,day" | → V_W,P,day je Person (A1) |
| 9 | Gl. (19), S. 38 | Einheiten c_W/ρ_W/V_t und Faktor 1/1000 inkonsistent | intern SI-konsistent rechnen |
| 10 | B.3.2, S. 52 | Verweis „Tabelle B.4" | gemeint Tab. B.8 |
| 11 | B.3.6, S. 55 | „Konstanten nach Tabelle B.2" | gemeint Tab. B.14 |
| 12 | 6.4.3.2, S. 35 | „Lastprofil nach Bild 5" | gemeint Bild 4 |
| 13 | Anhang B | Gleichungsnummer (B.1) doppelt vergeben (n_P,eq,max **und** Dichte) | Kontext beachten |
| 14 | Gl. (B.3), S. 50 | Bedingung „10 m² < A_h < 50 m²" lässt A_h = 10 m² offen | Grenzfall selbst festlegen |
| 15 | Gl. (B.2)/(B.4) | Bedingung mal „n_max", mal „n_P,eq,max" | einheitlich n_P,eq,max |
| 16 | A100 Tab. NA.7 | Verweis auf NA.5.2.6.16 (N=4), Fließtext nennt N=2 | → N = 2 (NA.5.2.6.15) |
| 17 | A100 Tab. NA.7 | Verweis auf nicht existente „Tabelle 5.4" | → Tab. NA.4 |
| 18 | A100 Gl. (NA.2) | A_HE,WP negativ für V_sto < ca. 105 l | Plausibilitäts-Guard |
| 19 | A100 Tab. NA.3, Zeile 7 | 360 l bei 60 °C ergibt 18 statt 21 kWh | Beispieldaten, nicht rechnen |
| 20 | A100 Diagramm S. 39 | Bilanz-Box ohne −Q_W,b,i | → Gl. (NA.3) maßgeblich |

### 7.5 Umsetzungsempfehlung Datenmodell

- **ProfilQuelle**: Enum { EN50440_Tab_B1, EN_Tab_B2, A100_Referenz, A100_Manuell, Messung_CSV, EN13203_2 }
- **Profil**: 1 440 Minutenwerte `x_min` (Anteile, Σ = 1) + Metadaten (ϑ_draw, ϑ_c der Referenz, Bezugsgröße, Objektbeschreibung a–k)
- Stundenprofile (Tab. B.1/B.2) beim Import per Gl. (3) auf 1 440 Werte expandieren und **als „stundenbasiert, Spitzen unterschätzt" flaggen**
- Temperatur-Renormierung beim Import: `x_energie = x_volumen · (ϑ_draw,Profil − ϑ_c,Profil) / (ϑ_draw,Auslegung − ϑ_c,Auslegung)` — nach A1 verpflichtend, wenn Referenztemperaturen abweichen
- CSV-Importer strikt nach A100 NA.5.2.4 (Header + `Zeit;Volumenstrom;T_PWC;T_PWH;T_PWH-C`, Minutentakt, l/s mit 1 Nachkommastelle)
- Ergebnisausgabe: Bedarfskennlinie, Versorgungskennlinie, Q_sto,ON-Linie, Q_sto,min-Linie (Bild 12), **Wertepaar-Kurve V_sto ↔ Φ_N**, Σ t_power,on [h/d]

---

## 8. Offene Punkte / Beschaffungsbedarf

1. **A100-Referenzprofil-Datendateien** (CD-ROM / ZIP nach NA.5.2.5) — ohne sie sind die 18 Minutenprofile nicht nutzbar. Höchste Priorität.
2. **Aktueller Status von A100 und A1**: Beide sind Entwürfe von 2021 (Einspruchsfristen 2021-10-13 bzw. 2021-04-26). Ob und in welcher Fassung sie inzwischen als Weißdruck erschienen sind, geht aus den vorliegenden Dokumenten nicht hervor und ist zu prüfen.
3. **EN 50440** — für die Absolutwerte der Zapfprogramme XXS…XXL zu Tab. B.1.
4. **EN 13203-2** — für die EFH-Zapfprogramme, auf die 6.5.1 verweist.
5. **CEN/TR 12831-4** — der begleitende Technische Bericht mit „weiteren informativen Inhalten"; enthält vermutlich Rechenbeispiele (die die Hauptnorm nicht hat).
6. **DIN 1988-300** — für V̇_A und den Spitzendurchfluss-Abgleich nach A100 NA.5.4.9.
7. **DIN V 18599-10** — Ursprung der Tab.-NA.4-Werte; für die Konsistenzprüfung mit der GEG-Bilanzseite von WP-Plan.
8. **Klärung Gl. (14)** — Diskrepanz Fließtext/Ablaufdiagramm; ggf. Rückfrage beim NA 041-05-01 AA oder Abgleich mit einer Referenzimplementierung.agentId: ac64ff6b77cfb19d5 (use SendMessage with to: 'ac64ff6b77cfb19d5', summary: '<5-10 word recap>' to continue this agent)
<usage>subagent_tokens: 235074
tool_uses: 7
duration_ms: 674735</usage>