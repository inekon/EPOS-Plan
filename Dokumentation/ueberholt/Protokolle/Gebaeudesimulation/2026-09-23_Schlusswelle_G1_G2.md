# Protokoll: Schlusswelle G1 + G2 der Gebäudesimulation (23.09.2026)

**Auftrag** (vom Anwender am 23.09.2026 beauftragt): Umschaltung des Normalfalls auf den
VDI-6007-Weg, Anbindung des Ergebnisexports, ein Referenzprojekt auf dem Altweg (A15), Abnahme
nach Leitkonzept 10.4 und neues Einfrieren der Referenzbasis. Maßgeblich:
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
Kapitel 1.8, 1.9 und 4 (Zeile G1 + G2), [Leitkonzept](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
5.5, 9 und 10.4, [Systementwurf](../../../aktuell/Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 8.4,
[ADR-006](../../../aktuell/ADR-006_Trennung_Altweg_VDI6007.md).

## 1 Umsetzung

| Teil | Stand |
|---|---|
| NULL-Regel der Weiche | `SimulationWaermebedarf.MODELL_OHNE_ANGABE = VDI6007`. Der Katalogeditor zeigt ein Gebäude ohne Angabe ohne weiteres Zutun als „VDI 6007" (er liest dieselbe Konstante über `Gebaeuderechenweg`) |
| Ergebnisexport | `Referenzlauf/Ergebnisexport.cs` ruft `GebaeudeErgebnisexport.Saetze`: je VDI-Gebäude `raumtemperatur_<n>.csv`, `operative_temperatur_<n>.csv` (°C), `kuehlbedarf_<n>.csv` (kWh) und die Skalare `Geb[n].ID_Gebaeude`, `Geb[n].Modell` und acht Kennzahlen in `aggregate.csv`. Kein `InternalsVisibleTo` nötig: die Kernseite ist öffentlich und liest den Träger intern |
| A15 | Projekt **1040** (Gebäude 10645, „EFH-A-U-347s", 201 m²) steht in der Testdatenbank auf `Gebaeude_Modell = 'TAGESBILANZ'` — eine Zelle, Skript `Referenzlaeufe/Skripte/gebaeude_1040_tagesbilanz.py`, Sicherung vorher außerhalb des Repositoriums; Zellvergleich 11 964 203 Zellen, genau diese Abweichung; LFS-Zeiger geprüft |
| Rückweg-Test | `EPOS.Kern.Tests/GebaeudeRueckwegTests`: genau ein Referenzgebäude auf dem Altweg (10645 in 1040), und dessen Stundenreihe `waermebedarf_gebaeude.csv` ist zeichengleich mit der aktuellen Basis. Läuft im Test-Gate und in der CI; endet mit GA |
| Tests nachgezogen | Weiche (NULL → VDI-Weg), Bestandsbefunde (Zeilen im Speicher ausdrücklich auf `TAGESBILANZ`), Schemaprobe (die A15-Zelle als einzige Ausnahme), Kesselkaskade 1007 (Restwärme ≤ 0,05 MWh/a statt 0, siehe 3) |
| Neue Basis | `Referenzlaeufe/2026-09-23_R12_Gebaeudemodell`, 399 CSV, 2 239 Skalare; R11 mit Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`. Tabelle je Projekt und die Erklärung der Auffälligkeiten: [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md), Abschnitt „Aktuelle Basis" |

Vor dem Einfrieren `git fetch origin`: origin hatte gegenüber dem Arbeitsstand keinen neuen
Commit — weder Testdatenbank noch `Referenzlaeufe/` noch Rechenweg.

## 2 Abnahmekriterien (Leitkonzept 10.4)

| Kriterium | Ergebnis |
|---|---|
| (1) Normtests | lokal mit den gitignorierten Normzahlen: alle Fälle der Klasse `GebaeudeModellNormfallTests` grün (Stand G0: elf der zwölf im Band, Fall 11 mit bekannter Abweichung); in der CI schweigend (U8) |
| (2) Prüfmodus | `GebaeudePruefmodusTests`: derselbe Löser mit dem Parametersatz der Prototyp-Konvention gegen Rechenschritte 9.1–9.5. Ersatzparameter auf 1e-5 relativ; Eigenwerte beider Betriebsfälle sechsstellig gleich (frei −2,50723·10⁻⁴ / −2,67151·10⁻⁵, geregelt −2,73175·10⁻⁴ / −6,11906·10⁻⁵ 1/s); Spitzenstunde 1 399: 36 440,65 W gegen 36 440,60 W (1,4·10⁻⁶ relativ), θ_op 15,698 °C gleich, Endzustände 4,8281 / 12,9641 °C gegen 4,8281 / 12,9642 °C; Umschaltstunde 2 221: 207,64 W gegen 207,66 W, θ_air 20,012 °C gleich, zwei Abschnitte. Der Prüfmodus steht als Parametersatz im Test und nicht als Schalter im Eingangsbauer, weil seine isotropen `Sol_*`-Spalten Altweg-Bezeichner sind, die die `Modultrennungswache` im Modul `Gebaeude/` verbietet. **Nicht nachgewiesen:** der Jahreswert 9.6 (71 916 kWh) und die 1e-6 gegen den Prototyp je Referenzprojekt — der Prototyp und sein Klimaadapter liegen nicht mehr vor (siehe 5) |
| (3) Tagessummen-Korrelation | r = 0,983 (10632) bis 0,996 (10643) für alle vierzehn Gebäudezeilen der Referenzprojekte; Schwelle 0,98 jetzt als Prüfung in `GebaeudeVdi6007DatenbankTests` |
| (4) Katalogkennzahl | mit dem Auslieferungsweg neu gemessen: 10614 95,8 %, 10576 99,6 %, 10599 102,7 %, 10632 100,4 %, 10628 96,4 %, 10642 109,3 %, 10643 109,4 % (10645 ohne Katalogwert). **Fenster neu: 90–115 %** (Messbereich 95,8–109,4 % mit rund 5 Prozentpunkten Rand); als Prüfung in `GebaeudeVdi6007DatenbankTests` |

**Ausweis des Abzugs R_si/A je Referenzgebäude** (Jahresheizwärme mit gegen ohne Abzug,
Kataloglauf unskaliert): 10614/10577 +5,3 %, 10576 +2,7 %, 10599 +3,0 %, 10632 +4,5 %,
10628/10644 +4,2 %, 10642 +12,9 %, 10643 +18,9 %, 10645 +12,5 %; über alle zehn Zeilen +5,6 %
(Heizwärme ohne Abzug −5,3 %). Die „rund 12 %" aus Rechenschritte 9.6 gelten für 10645; bei den
großen Gebäuden mit hohem Fenster- und Lüftungsanteil ist der Posten kleiner.

## 3 Wirkung auf die Basis

Elf Projekte bewegen sich, 1030 (ohne Gebäude) und 1040 (Altweg) bleiben byte-gleich.
Gebäudewärme +28,0 % (EFH-A-U-347s) bis +45,6 % (1018); Spitzenlast bis +76 % (1017) wegen der
Morgenspitze des idealen Heizers; mehr ungedeckte Restwärme in den knapp ausgelegten Projekten;
in 1018 mehr BHKW- und weniger Kesselwärme wegen der Nachtgrundlast. Einzelheiten und Tabelle:
`Referenzlaeufe/LIESMICH.md`. Determinismus: zweiter Lauf 399/399 CSV byte-gleich.

**Rechenzeit:** Lauf aller dreizehn Projekte 4–5 s (Windows, Release). Je Gebäude und Jahr
samt Eingangsbau 15–25 ms warm, der erste Aufruf eines Prozesses rund 40 ms (Messung in
`GebaeudeVdi6007DatenbankTests`).

## 4 Vergleichsläufe (einmalig, zehn Gebäudezeilen der Referenzprojekte)

Gerechnet mit einer nicht versionierten Messklasse gegen den Auslieferungsweg; Summen über die
zehn verschiedenen Gebäudezeilen (Kataloglauf, unskaliert):

| Lauf (Rechenschritte 11) | Heizwärme | Summe der Spitzen | Kühlbedarf | Überhitzungsstunden |
|---|---:|---:|---:|---:|
| Basis (Auslieferungsweg), absolut | 1 548,17 MWh | 982,6 kW | 68,38 MWh | 3 561 h |
| Zeile 15 — Fenstersolar mit A_v = Fensterfläche der Orientierung (45)/(46) | −0,02 % | ±0,00 % | +0,2 % | +0,1 % |
| Zeile 16 — Σψ·L in der Außenwandgruppe statt im masselosen Zweig (θ_eq mitgewichtet) | −0,56 % | −0,91 % | −1,9 % | −1,8 % |
| Zeile 17 — R_rad aus beiden Oberflächengruppen (1/(α·A_AW,ges) + 1/(α·A_IW)) statt min(A) | −0,16 % | −0,03 % | +0,3 % | +0,7 % |

Alle drei Festlegungen des Klassenwegs wirken unter 1 % auf Heizwärme und Spitze; keine verlangt
eine Umstellung vor G3. Zu Zeile 17: Die Richtlinie liegt nicht im Repositorium; die Vergleichsbildung ist
die Reihenschaltung der Strahlungsübergänge beider Flächen, eine Näherung an Gl. (29)/(31), nicht
deren Wortlaut.

## 5 Verschoben und offen

- **Bericht (Leitkonzept 9)** — Rechenweg, drei Spitzenwerte und Kühlkennzahlen je Gebäude im
  Kennzahlenkatalog: **verschoben.** Der Bericht liest allein die gespeicherten Ergebnistabellen
  (`VariantenDaten.Ergebnis`); die Gebäudekennzahlen gehen nach Umsetzungskonzept 1.8 bewusst nicht
  in die Datenbank. Ein Ausweis verlangt entweder eine Rechnung im Berichtsweg (über
  `GebaeudeBedarfCtrl`, je Gebäude und Variante) oder eine Persistenz — beides ist ein eigener
  Schritt mit eigener Entscheidung, kein Nachtrag dieser Welle.
- **ChartProben:** grün auf Windows (strukturelle Probe). Die drei Bilder der Raumtemperatur
  (`raumtemperatur_gebaeude`, zwei Bilder der Gegenprobe) kommen erst mit dem nächsten Einfrieren
  auf dem Linux-Läufer in die Messlatte — die Messlatte ist plattformgebunden
  (`Proben/ChartProben/LIESMICH.md`).
- **Kriterium (2), Jahreswert:** braucht die Klimaaufbereitung des Prototyps (isotrope `Sol_*`,
  UTC-Reihenfolge) als Prüfwerkzeug außerhalb des Moduls `Gebaeude/`; offen bis ein solches
  Werkzeug gebaut ist oder mit GA entfällt.
- **Strahlungsweg nach VDI 2078** (Testbeispiel 7.1/7.2) aus der Abnahmezeile G1 + G2: nicht Teil
  dieser Welle.
- **Wiki:** Seite „Gebäudemodell VDI 6007" an den neuen Normalfall angepasst (Repo-Quelle); Upload
  und Logbuch-Eintrag mit der Versionsnummer beim Anwender stehen aus.
