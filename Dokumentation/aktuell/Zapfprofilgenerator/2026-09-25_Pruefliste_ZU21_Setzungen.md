# Prüfliste ZU21 — Setzungen des freien Paketteils des Zapfprofilgenerators

Stand 25.09.2026. Anlass: Anwenderentscheid ZU21 vom 25.09.2026 (Nachtrag N16 des
[Umsetzungskonzepts](../Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md)): **Die Setzungen des
freien Paketteils bleiben bis zur fachlichen Durchsicht durch den Anwender ungeliefert.** Diese Liste
ist die Durchsicht. Sie führt jede Setzung, die **keine Norm vorgibt** — gesetzt von INEKON oder als
Modellannahme —, mit ihrem heutigen Wert und der Fundstelle im Arbeitsbaum.

**So wird sie benutzt.** Der Anwender trägt in der letzten Spalte je Zeile ein: „bestätigt" oder den
neuen Wert. Erst wenn jede Zeile der Tabelle 1 entschieden ist, geht der freie Paketteil
(`Referenzlaeufe/Katalogpaket_frei/`) in die Auslieferung; bis dahin bleibt er Testgut. Tabelle 2
steht zur Kenntnis: numerische Grenzen und Toleranzen im Rechenkern, die kein Katalogwert sind —
sie wandern nur auf ausdrücklichen Wunsch.

**Keine Normzahl.** Werte, die aus VDI 6002, VDI 4655 oder DIN EN 12831-3 abgeleitet sind, stehen
nicht in dieser Liste; sie hängen an ZU19, ZU20 und ZU22. Die beiden Quantile
`Zapfprofil.Stochastik.Quantil.P95` und `…P99` (1,6448536 und 2,3263479,
`Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:5`–`6`) sind Quantile der
Standardnormalverteilung — rechnerisch festgelegt, kein Ermessen, daher kein Eintrag.

**Verweis.** Die Nachträge zitieren die Setzungen des Paketteils mehrfach als „N12 (u)"; N12 endet
bei (t). Gemeint sind N12 **(p)**, **(q)** und **(r)**; die Spalte „Begründung" nennt sie richtig.

**Entscheid.** Der Anwender hat am 26.09.2026 wörtlich entschieden: „Abschnitt 1,2:
entschieden", ergänzt um „Abschnitt 1: Ecodesign - erweitere Profil". **Lesart:** alle
Setzungen der Abschnitte 1 (34 Zeilen des freien Paketteils) und 2 (17 numerische Setzungen
im Rechenkern) sind bestätigt, wie sie stehen — mit einer Ausnahme: die drei
Ecodesign-Zeilen des Abschnitts 1 (Dauer der 24 Ecodesign-Zapfungen, Ecodesign-Profil L
Bezug, Auswahl der Ecodesign-Profile) sind nicht bestätigt, sondern erweitert: alle
Zapfprofile der Verordnung (EU) Nr. 814/2013 (XXS bis 4XL) werden aufgenommen, Rechenregel
und Bezug bleiben unverändert, als eigener Folgeposten. Abschnitt 3 (Setzungen ohne
belegten Auslieferungswert) und Abschnitt 4 sind von diesem Entscheid nicht berührt.
**Folge:** Der freie Paketteil (`Referenzlaeufe/Katalogpaket_frei/`) darf mit den
bestätigten Werten ausgeliefert werden; die Ecodesign-Erweiterung läuft als eigener
Folgeposten. Änderungen an einzelnen Werten sind künftig ein eigener Anwenderentscheid,
etwa aus den Folgen V1–V5 des
[Validierungslaufs](2026-09-26_Validierung_offene_Messreihen.md) im selben Ordner.

---

## 1. Setzungen des freien Paketteils (zu entscheiden)

| Kennung / Parameter | heutiger Wert | Einheit | Quelle (Datei:Zeile) | Begründung aus dem Nachtrag | Entscheid |
|---|---|---|---|---|---|
| `Zapfprofil.Stochastik.Urlaubsversatz` | 14 | d | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:2`; Schlüssel `EPOS.Kern/Allgemein/Zapfprofil/Zapfkategorie.cs:42` | N12 (p): größter Versatz der Ferienfenster je Einheit, der die Urlaube der Einheiten entkoppelt | bestätigt 26.09.2026 |
| `Zapfprofil.Stochastik.Auslegung.Vielfaches` | 2 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:3`; Verwendung `EPOS.Kern/Allgemein/Zapfprofil/Zapfensemble.cs:362` | N12 (n)/(p): Vielfaches der Mindestzahl 1/(1−p) als Vorgabe der Auslegungsrealisierungen | bestätigt 26.09.2026 |
| `Zapfprofil.Stochastik.Konsistenzschwelle` | 1,5 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:4`; Schlüssel `EPOS.Kern/Allgemein/Zapfprofil/Zapfkategorie.cs:55` | N12 (f)/(p): Schwelle des Konsistenzhinweises gegen Φ_N; sie entscheidet die Rechnung nicht | bestätigt 26.09.2026 |
| `Zapfprofil.Zirkulation.Hinweisverhaeltnis` | 1,5 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:7`; Schlüssel `EPOS.Kern/Allgemein/Zapfprofil/Zapfprofileingang.cs:204` | N13: Hinweis, wenn die Zirkulation mehr als dieses Vielfache der Zapfung verliert (Zirkulationshinweis) | bestätigt 26.09.2026 |
| `Zapfprofil.Anzeigetemperatur` | 45 | °C | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:8`; Schlüssel `EPOS.Kern/Allgemein/Zapfprofil/Zapfprofileingang.cs:210` | N13: Vorgabe der Literanzeige der Kennzahlen, wenn weder Dialog noch Einstellung eine nennt | bestätigt 26.09.2026 |
| `Zapfprofil.Stundenschwelle` | 0,1 | kW | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:9`; Schlüssel `EPOS.Kern/Allgemein/Zapfprofil/Zapfprofileingang.cs:216` | N13: Vorgabe der Schwelle für die Kennzahl „Stunden über der Schwelle" | bestätigt 26.09.2026 |
| `Zapfprofil.Validierung.Band.Unten` | 0,85 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:10`; Code-Vorgabe `EPOS.Kern/Allgemein/Zapfprofil/Messvergleich.cs:154` | N15 (e): untere Bandgrenze der synthetischen Dauerlinie, unter der die Messspitze als „unterhalb" gilt | bestätigt 26.09.2026 |
| `Zapfprofil.Validierung.Band.Oben` | 0,95 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:11`; Code-Vorgabe `EPOS.Kern/Allgemein/Zapfprofil/Messvergleich.cs:157` | N15 (e): obere Bandgrenze der synthetischen Dauerlinie | bestätigt 26.09.2026 |
| `Zapfprofil.Validierung.Formschwelle` | 0,01 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:12`; Code-Vorgabe `EPOS.Kern/Allgemein/Zapfprofil/Messvergleich.cs:160` | N15 (e): mittlere absolute Abweichung der 24 Stundenanteile je Tagtyp, ab der die Form als abweichend gilt | bestätigt 26.09.2026 |
| `Zapfprofil.Validierung.Lueckenanteil` | 0,05 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:13`; Schlüssel `EPOS.Kern/Allgemein/Zapfprofil/Zapfprofileingang.cs:244` | N15 (e): höchster zugelassener Anteil gefüllter Lücken einer Messreihe, darüber Ablehnung | bestätigt 26.09.2026 |
| `Zapfprofil.Validierung.Kalibrierung.MindestTage` | 30 | d | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv:14`; Code-Vorgabe `EPOS.Kern/Allgemein/Zapfprofil/Messkalibrierung.cs:62` | N15 (e): kürzeste Messreihe, aus der ein Kalibriervorschlag der Nichtwohn-Parameter entsteht | bestätigt 26.09.2026 |
| Nichtwohnen · Kurzzapfung · Volumenstrom | 2 | l/min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:6` | N15 (g): freie Modellannahme von INEKON mit runden Werten (Waschtisch); vom OpenDHW-Muster kommt nur Zahl und Art der Kategorien, kein Zahlenwert | bestätigt 26.09.2026 |
| Nichtwohnen · Kurzzapfung · Dauer | 1 | min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:6` | N15 (g): freie Modellannahme (Waschtisch) | bestätigt 26.09.2026 |
| Nichtwohnen · Kurzzapfung · Anteil | 0,6 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:6` | N15 (g): freie Modellannahme; die Anteile der Gruppe summieren auf 1 | bestätigt 26.09.2026 |
| Nichtwohnen · Kurzzapfung · Streuung σ | 1 | l/min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:6` | N15 (g): freie Modellannahme | bestätigt 26.09.2026 |
| Nichtwohnen · Kurzzapfung · Kappung | 6 | l/min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:6` | N15 (g): freie Modellannahme — was eine Armatur höchstens gibt; geht in das doppelt gestutzte Mittel der λ-Kalibrierung ein | bestätigt 26.09.2026 |
| Nichtwohnen · Duschzapfung · Volumenstrom | 8 | l/min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:7` | N15 (g): freie Modellannahme von INEKON mit runden Werten (Brause) | bestätigt 26.09.2026 |
| Nichtwohnen · Duschzapfung · Dauer | 5 | min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:7` | N15 (g): freie Modellannahme (Brause) | bestätigt 26.09.2026 |
| Nichtwohnen · Duschzapfung · Anteil | 0,4 | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:7` | N15 (g): freie Modellannahme; Summe der Gruppe = 1 | bestätigt 26.09.2026 |
| Nichtwohnen · Duschzapfung · Streuung σ | 1 | l/min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:7` | N15 (g): freie Modellannahme | bestätigt 26.09.2026 |
| Nichtwohnen · Duschzapfung · Kappung | 20 | l/min | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:7` | N15 (g): freie Modellannahme — was eine Brause höchstens gibt | bestätigt 26.09.2026 |
| Gruppenregel der beiden Vorgabesätze | Kalenderart 1 = „Wohnen", jede andere = „Nichtwohnen" | – | `EPOS.Kern/Allgemein/Update/TwwSchema.cs:163`; Paketregel `Referenzlaeufe/Katalogpaket_frei/LIESMICH.md:31` | N15 (g): eine Quelle für Vorlage, Skript, Kern, Import und Wachen; die Bezugsart trennt bewusst nicht | bestätigt 26.09.2026 |
| Bindung des Vorgabesatzes | Kategorien ohne `ID_Nutzungsart`; jede Nutzungsart mit `Status AUSLIEFERUNG` ohne eigene Kategorien bekommt den Satz ihrer Gruppe | – | `Referenzlaeufe/Katalogpaket_frei/LIESMICH.md:27`; Einspielweg `Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs:339` | N12 (p): Vorgabesatz statt je Nutzungsart gepflegter Kategorien — sonst könnte die Auslieferung nicht stochastisch rechnen | bestätigt 26.09.2026 |
| Katalogversion des Paketteils | keine eigene; die Zeilen treten der Version des Zielkatalogs bei, sonst `FREI-1` | – | `Referenzlaeufe/Katalogpaket_frei/LIESMICH.md:22`; Spalte `Version` in `Tab_TwwParameter_STAMM.csv` | N12 (p): sonst sähe der Parametersatz, der nur eine Katalogversion liest, die Stochastik-Parameter nicht | bestätigt 26.09.2026 |
| Wohnen · Kurzzapfung | 1 l/min · 1 min · Anteil 0,14 · σ 2 l/min · ohne Kappung | l/min, min, – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:2` | N12 (p): Modellannahme nach Jordan/Vajen (IEA SHC Task 26), Streuung nach dem DHWcalc-Protokoll — Fachliteratur, keine Norm | bestätigt 26.09.2026 |
| Wohnen · Mittlere Zapfung | 6 l/min · 1 min · Anteil 0,36 · σ 2 l/min · ohne Kappung | l/min, min, – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:3` | N12 (p): Modellannahme nach Jordan/Vajen — Fachliteratur, keine Norm | bestätigt 26.09.2026 |
| Wohnen · Wannenbad | 14 l/min · 10 min · Anteil 0,1 · σ 0,2 l/min · ohne Kappung | l/min, min, – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:4` | N12 (p): Modellannahme nach Jordan/Vajen — Fachliteratur, keine Norm | bestätigt 26.09.2026 |
| Wohnen · Dusche | 8 l/min · 5 min · Anteil 0,4 · σ 0,4 l/min · ohne Kappung | l/min, min, – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwZapfkategorie_STAMM.csv:5` | N12 (p): Modellannahme nach Jordan/Vajen — Fachliteratur, keine Norm | bestätigt 26.09.2026 |
| Dauer der 24 Ecodesign-Zapfungen | Rechenregel: Dauer = Volumen / Volumenstrom, Volumen = Q_tap / (c_w · (θ_Nutz − 10 °C)), θ_Nutz = Spitzentemperatur, sonst Mindesttemperatur; ganze Minuten kaufmännisch, mindestens 1 — ergibt 1 bis 10 min | min | `Referenzlaeufe/Katalogpaket_frei/LIESMICH.md:55`; Werte `Tab_TwwBedarfstagEreignis_STAMM.csv:2`–`25` | N12 (q): die Verordnung (EU) Nr. 814/2013 nennt Energie, Volumenstrom und Temperaturen, aber keine Dauer — reine Setzung der Umsetzung | erweitern 26.09.2026: alle Zapfprofile der Verordnung (EU) Nr. 814/2013 (XXS bis 4XL) aufnehmen, Rechenregel und Bezug unverändert — Folgeposten |
| Ecodesign-Profil L, Bezug | Bezugsart 2 (Wohneinheiten), ohne Bezugsmenge — nicht skaliert | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwBedarfstag_STAMM.csv:2` | N13, Folge (a): das Lastprofil beschreibt einen Haushalt; ob nach Wohneinheiten skaliert wird, ist offener Fachentscheid | erweitern 26.09.2026: alle Zapfprofile der Verordnung (EU) Nr. 814/2013 (XXS bis 4XL) aufnehmen, Rechenregel und Bezug unverändert — Folgeposten |
| Auswahl der Ecodesign-Profile | alle neun Profile (XXS bis 4XL) | – | `Referenzlaeufe/Katalogpaket_frei/Tab_TwwBedarfstag_STAMM.csv` (neun Zeilen) | N12 (q): M und XL wären ebenso frei, sind aber als Setzung weggelassen | erweitern 26.09.2026: alle Zapfprofile der Verordnung (EU) Nr. 814/2013 (XXS bis 4XL) aufnehmen, Rechenregel und Bezug unverändert — Folgeposten |
| Ein-/Zweifamilienhaus: Formen | Wochenanteile und Monatsfaktoren des großen Wohngebäudes; teilt dessen Tagesgangsatz („Wohnen groß") | – | `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py:149`, Katalogzeile `:156` | N15 (f): die Richtlinie gibt dem Ein- und Zweifamilienhaus keine Profile — Setzung (1), ohne neuen Zahlenwert | bestätigt 26.09.2026 |
| Ein-/Zweifamilienhaus: mittlerer Bedarf | (Minimum + Maximum) / 2 der abgeleiteten Spanne | kWh je Person und Tag | `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py:153`, Rechnung `:189` | N15 (f): die Quelle nennt keinen Mittelwert — Setzung (2), ausdrücklich „Mitte der abgeleiteten Spanne" | bestätigt 26.09.2026 |
| Bezugs-Kaltwassertemperatur der abgeleiteten Katalogzeilen | 12 | °C | `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py:135`, Wert `:123` | N12 (r): Bezug 60/12 °C — die 12 °C sind ausdrücklich als Setzung ohne normativen Wert vermerkt | bestätigt 26.09.2026 |

---

## 2. Numerische Setzungen im Rechenkern (zur Kenntnis, kein Katalogwert)

Diese Werte stehen nicht im Paketteil, sondern als Vorgabe, Grenze oder Toleranz im Quelltext. Sie
gehören nicht zur Auslieferungsfrage ZU21; sie stehen hier, damit die Durchsicht vollständig ist. Eine
Änderung geschieht nur auf ausdrücklichen Wunsch und trifft den Rechenweg.

| Kennung / Parameter | heutiger Wert | Einheit | Quelle (Datei:Zeile) | Begründung aus dem Nachtrag | Entscheid |
|---|---|---|---|---|---|
| Vorgabe `Tab_TwwProjekt.Seed` | 1 | – | `EPOS.Kern/Allgemein/Update/TwwSchema.cs:538` | N12 (n): Vorgabe des Seeds, damit jede Rechnung ohne Eingabe wiederholbar ist | bestätigt 26.09.2026 |
| Vorgabe `Tab_TwwProjekt.Realisierungen` | 10 | Jahre | `EPOS.Kern/Allgemein/Update/TwwSchema.cs:539` | N12 (n): Vorgabe der Jahre der Jahresreihe; die Bilanz hängt nicht von R ab, die Zahl dient der Konsistenzprobe | bestätigt 26.09.2026 |
| Untergrenze `Realisierungen` und `Realisierungen_Auslegung` | 1 | – | `EPOS.Kern/Allgemein/Update/TwwSchema.cs:188` | N12 (h): benannte Grenze des Schemas und des Schreibwegs | bestätigt 26.09.2026 |
| Vorgabe `Realisierungen_Auslegung` | ⌈Vielfaches · 1/(1 − p)⌉ — mit Vielfaches 2: P95 → 40, P99 → 200 | – | `EPOS.Kern/Allgemein/Zapfprofil/Zapfensemble.cs:360`, Mindestzahl `:345` | N12 (n): Vorgabe, wenn das Projekt keine Zahl nennt; darunter trägt das Perzentil „nicht belastbar" | bestätigt 26.09.2026 |
| Perzentilwahl der Auslegung | 95 und 99 | – | `EPOS.Kern/Allgemein/Update/TwwSchema.cs:185` | N12 (s): nur P95 und P99 werden angeboten (K3); die z-Werte selbst stammen aus der Standardnormalverteilung | bestätigt 26.09.2026 |
| Obergrenze Realisierungen des Bedarfstags | 100 000 | – | `EPOS.Kern/Allgemein/Zapfprofil/Zapfensemble.cs:330` | N12 (h): numerische Setzung — mehr Realisierungen ändern das Perzentil nicht mehr erkennbar | bestätigt 26.09.2026 |
| Obergrenze Einheitentage eines Ensembles (R · Σ n_E) | 10 000 000 | Einheitentage | `EPOS.Kern/Allgemein/Zapfprofil/Zapfensemble.cs:339`; gleiche Grenze `ZapfprofilRechner.cs:445` | N12 (h): numerische Setzung — an der Grenze rund 80 MB und linear wachsende Laufzeit | bestätigt 26.09.2026 |
| Obergrenze Jahre der Jahresreihe | 1 000 | Jahre | `EPOS.Kern/Allgemein/Zapfprofil/Jahresensemble.cs:120` | N12 (h): numerische Setzung — ab R = 1000 schärft die Konsistenzprobe nicht mehr | bestätigt 26.09.2026 |
| Obergrenze Einheiten je Zone | 1 000 000 | Einheiten | `EPOS.Kern/Allgemein/Zapfprofil/Zapfkategorie.cs:355` | N12 (h): numerische Setzung gegen eine unsinnige Laufzeit | bestätigt 26.09.2026 |
| Toleranz der Konsistenzprobe, relativer Anteil | 0,01 | – | `EPOS.Kern/Allgemein/Zapfprofil/Jahresensemble.cs:107` | N12 (g): numerische Setzung des Papiers (4.4) für den Vergleich Ensemblemittel gegen die deterministische Reihe | bestätigt 26.09.2026 |
| Toleranz der Konsistenzprobe, Streuungsfaktor | 3,0 | Faktor auf s_R/√R | `EPOS.Kern/Allgemein/Zapfprofil/Jahresensemble.cs:110` | N12 (g): numerische Setzung des Papiers (4.4) | bestätigt 26.09.2026 |
| Blockgröße der parallelen Jahresrechnung | 64 | Jahre je Block | `EPOS.Kern/Allgemein/Zapfprofil/Jahresensemble.cs:123` | N12 (h): numerische Setzung (64 Reihen zu 8760 Stunden, rund 4,5 MB); parallel und seriell bleiben bitgleich | bestätigt 26.09.2026 |
| Erkannte Auflösungen einer Messreihe | 1, 5, 10, 15, 60, 1440 | min | `EPOS.Kern/Allgemein/Import/Messreihenleser.cs:115` | N15 (b): Setzung des Lesers — die Auflösung wird aus dem kleinsten positiven Abstand auf dieses Raster gezogen | bestätigt 26.09.2026 |
| Obergrenze Datenzeilen einer Messreihe | 600 000 | Zeilen | `EPOS.Kern/Allgemein/Import/Messreihenleser.cs:122` | N15 (b): numerische Setzung — ein Minutenjahr hat 525 600 Zeilen, die Grenze lässt Schaltjahr und Vorlauf zu | bestätigt 26.09.2026 |
| Höchstgröße einer Messdatei | 64 | MiB | `EPOS.Kern/Allgemein/Import/Messreihenleser.cs:125` | N15 (b): numerische Setzung, wie im Normformvektorleser | bestätigt 26.09.2026 |
| Mindestzahl Datenzeilen einer Messreihe | 2 | Zeilen | `EPOS.Kern/Allgemein/Import/Messreihenleser.cs:128` | N15 (b): Setzung — unter zwei Zeilen lässt sich keine Auflösung messen | bestätigt 26.09.2026 |
| Jahresrand der Kalibrierung ohne Hochrechnung | 1,0 | d | `EPOS.Kern/Allgemein/Zapfprofil/Messkalibrierung.cs:65` | N15 (d): Setzung, wie weit eine Reihe von 365 Tagen abweichen darf, ehe mit dem Jahresgang hochgerechnet wird | bestätigt 26.09.2026 |

---

## 3. Setzungen ohne belegten Auslieferungswert

Drei Gruppen sind im Konzept als INEKON-Setzung ausgewiesen und trugen im Repositorium nur einen
fiktiven Testwert. Wo der Auslieferungswert noch aussteht, gehören sie nicht in Tabelle 1, sondern
auf die Liste der offenen Posten; die Speicherauslegung ist bis auf zwei Setzungen aus der Vorlage V4
ausgeliefert (N28).

- **`Zapfprofil.Messwert.Rueckfrageschwelle` und `Zapfprofil.Formvektor.Warnschwelle`** — im Konzept
  als Setzung geführt (`EPOS.Kern/Allgemein/Zapfprofil/Zapfprofileingang.cs:193` und `:196`), **nicht**
  im freien Paketteil. Belegt sind nur die fiktiven Testwerte 0,5 und 0,01
  (`Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py:237`–`238`). Der Auslieferungswert gehört in das
  externe Katalogpaket.
- **Die Setzungen der Speicherauslegung** — `Speicherauslegung.Speichertemperatur_Vorgabe`,
  `…GLF_Gueltigkeitsgrenze`, `…Nenninhalt.Raster` und `…Liste.*`, `…Nutzanteil`, `…Zuschlag`,
  `…Ladefenster.*`, `…Klassisch.*` (Konzept 4.7). **Ausgeliefert aus V4 (N28)** im freien Paketteil,
  Herkunftsart `EIGENKONSTRUKTION`, Quelle `Referenzlaeufe/Skripte/speicherauslegung_v4.json`;
  Fundstellen in der Vorlage (Version 2.1.2): Speichertemperatur 60 °C (Eingaben B22), Nutzanteil 0,80
  (Eingaben B30), Zuschlag 0,15 (Eingaben B31), Ladefenster-Länge 8 h (Eingaben B63), klassischer
  Faustwert 35 l/(P·d) (Berechnung B387), Bezugsspreizung 50 K (Berechnung B388 − B389), Warnfaktor 3
  (Ergebnis B34), Nenninhaltsliste 100 … 10 000 l in 14 Stufen (Ergebnis A40:A53), Raster 1 000 l
  (Ergebnis B25, A54). **Offen** bleiben `…Ladefenster.Beginn` und `…GLF_Gueltigkeitsgrenze`: V4 führt
  keinen Wert (die Bilanz lädt über 24 h; die GLF-Grenze steht dort nur qualitativ), sie tragen weiter
  nur fiktive Testwerte und werden nicht ausgeliefert.
- **„Ecodesign L nach Wohneinheiten skalieren"** — offener Fachentscheid (N13, Folge (a)), kein Wert.

---

## 4. Was diese Liste nicht enthält

- **Keine Normzahl.** Werte aus VDI 6002, VDI 4655 und DIN EN 12831-3 stehen nicht hier; sie hängen an
  ZU19 (Ableitung), ZU20 (Auslieferung der VDI-6002-Ableitung), ZU22 (VDI 4655 bleibt draußen) und
  ZU24 (A100 als externes Paket).
- **Keine Hersteller- oder Produktwerte.**
- **Keine Messobjektdaten** — die Freigabe von Messreihen ist mit K5 zurückgestellt.
