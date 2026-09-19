# Prüfpunkt „Konsistenz" — die sechs Mockups gegen ihre Papiere

Prüfung vom 19.09.2026 · Repository EPOS-Plan · **keine Datei geändert**

---

## 1 Kurzbefund (fünf Sätze)

Die **Zahlen** des konsolidierten Mockups `Dialog_Formel_Zahlenprobe.html` stimmen durchgängig mit
`Beispielprojekt.md`, den acht Rechenwegen und der § 2.12-Tabelle des konsolidierten Konzepts überein —
geprüft wurden 235 Beträge der Papiere gegen 1 100 Beträge des Mockups, ohne eine einzige Abweichung.
Das **zweite Mockup `Ergebnis_Bandbreite_Herkunft.html` ist zahlenmäßig abgehängt**: Es trägt in
mindestens 22 Zeilen die Zahlen von vor der PV-Neurechnung (#351), darunter die Kapitalwertdifferenz
V3 1.877.827 € statt 1.842.695 € und die PV-Vergütung 10.396,34 € statt 9.001,44 €.
Das **konsolidierte Konzept hinkt dem Mockup und dem Code hinterher**: Kopfzeile (`ZIEL_VERSION = 61`,
Schemaschritt 62 — tatsächlich 94 bzw. nächster freier 95), § 2.2/§ 7 beschreiben den längst gebauten
BHKW-Dialog als „neu (BW9)" mit sechs statt acht Gruppen und mit dem Vorschlag „am Feld, nicht als
Sammelknopf", § 2.13 (3) und § 6.3 (9h) nennen den mit #357 gebauten Knopf „Nutzungsdauern vorbelegen"
weiterhin „nicht gebaut".
Im Mockup selbst fand die Prüfung **drei innere Brüche**: den Marker „umgesetzt U40" ohne Anhangzeile
(der Anhang endet bei U39), zwei Ressourcenschlüssel mit „geplant" ohne U-Nummer und die
Ressourcentafel, die die Klappliste weiter „Stammprojekt:" nennt, obwohl Bild und Text nach VV‑Q7
„Projekt:" sagen.
Von den vier übrigen Mockups beschreibt **keines mehr einen offenen Zustand**: Katalogfilter S1–S3,
Wechselrichter S1–S3 und die Stromspeicher-Pakete P1–P9 sind umgesetzt, das Wechselrichter-Mockup
behauptet trotzdem „Solange Stufe S3 aussteht, rechnet der Kern die Stränge nicht" —
`Entwurf_Hydraulikuebersicht_Konfiguration.html` hat überhaupt kein zugehöriges Konzept mehr.

---

## 2 Methode

* **Quellen.** Textabzüge `SP\Dialog_Formel_Zahlenprobe.txt` (5 490 Z.) und
  `SP\Ergebnis_Bandbreite_Herkunft.txt` (1 996 Z.) für den Fließtext; die sechs HTML-Dateien unter
  `Dokumentation/aktuell/Mockups/` für Anker, Klassen (`gestrichen`, `rs-geplant`, `zk umg/vor/bsp/beleg/abl`)
  und Zeilennummern. **Alle Mockup-Zeilennummern in diesem Bericht sind HTML-Zeilen der Repo-Datei.**
* **Papiere** nach Überschriftenraster gelesen (`grep -n '^#'`), dann abschnittsweise:
  Konzept § 2.2–2.16, § 3.6, § 5, § 6.1–6.3, § 7 · `LIESMICH.md` · `Beispielprojekt.md` ·
  `Rechenweg/01…08` · `Konzept_Nutzungsdauer_AfA`, `Konzept_Wirtschaftlichkeit_Szenarien_VALERI`,
  `Grundlagen_KWKG_…` · `Status_iOS_Migration.md` Z. 264–287 (#343–#366) und Z. 392–426 („4 Offen") ·
  `Dokumentation/LIESMICH.md`.
* **Zahlenprobe (a)** maschinell: alle Beträge im Muster `1.234,56` aus Beispielprojekt + acht
  Rechenwegen (235 verschiedene) gegen die Mengen beider Mockups gestellt (`comm`), jede Differenz
  einzeln aufgesucht.
* **Schemastand (b)** gemessen: `EPOS.Kern/Allgemein/Update/SchemaStand.cs:265` → `Zielversion = 94`;
  `Referenzlaeufe/LIESMICH.md:153` „**Schemastand 94**", Schritte 92/93/94 dort beschrieben.
  Nächster freier Schritt damit **95**.
* **U-Nummern (c)** aus dem HTML extrahiert: 39 Anhangzeilen, davon 20 mit `<tr class="gestrichen">`
  (erledigt: U8, U16–U21, U23, U24, U26, U28–U31, U33–U38) und 19 offen (U1–U7, U9–U15, U22, U25,
  U27, U32, U39); Marker im Haupttext über `<span class="zk umg|vor">` (36 Treffer) und
  `rs-geplant` (52 Spannen, davon 50 mit U-Nummer).
* **Verweise (g)**: alle `href=` der sechs Mockups gegen die `id=`-Menge derselben Datei; alle
  Code-Spannen `Mockups/*.html` aus `Dokumentation/**` und `CLAUDE.md` gegen den Dateibestand —
  dieselbe Regel, die `EPOS.Kern.Tests/DokumentationLinkWacheTests` prüft.
* **Wächtermuster (h)**: `perl -ne` mit dem Regex des CLAUDE.md-Abschnitts „Dokumentation" über alle
  sechs Dateien, Treffer mit Zeilennummer und entkleidetem Text bewertet.
* **Nicht getan:** kein Bau, kein Test, kein `git`-Befehl, keine Datei geändert; Artifact-Links nur
  genannt, nicht abgerufen.

---

## 3 Befunde

Spalten: **Nr** | **Ort** (Datei · Abschnitt/Anker · Zeile) | **Befund** | **Vorgeschlagene Änderung** |
**Ziel** | **Schwere**

### (a) Zahlen — Beispielprojekt ↔ Mockup ↔ Rechenwege ↔ § 2.12 ↔ Ergebnis-Mockup

Im **konsolidierten** Strang (Beispielprojekt ↔ `Dialog_Formel_Zahlenprobe.html` ↔ Rechenweg 01–08 ↔
Konzept § 2.12) wurde **keine einzige Abweichung** gefunden. Stichproben, die stimmen:
Versionstafel 560.016/409.435/535.392/384.811 und 31.230/84.436/38.521/91.727 (Beispielprojekt Z. 127–130 ↔
Mockup Z. 573–612); I₀ 234.772,40 / 192.150,00 / 426.922; Betriebskosten 57.164,21 + 5.731,13 + 2.400 =
59.564,21 / 8.131,13 / 65.295,34; Erlösrubrik 91.727,0 und 339.753,6 − 23.594,0 = 316.159,6
(Rechenweg 07 Z. 24/35/37 ↔ Mockup Z. 3172/3286); Kapitalwerte −7.902.712 / −6.242.507 / −7.720.222 /
−6.060.017 und die Bandbreite 1.506.740 … 1.811.714 · 129.296 … 236.921 · 1.636.035 … 2.048.635
(Beispielprojekt § 4c ↔ Mockup Z. 3499–3508); BEHG 872,3 t × 65,00 = 56.699,50 €/a; PV-Reihe
nominal 150.118 / Barwert 113.800.

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| a‑1 | `Mockups/Ergebnis_Bandbreite_Herkunft.html` · „1 Lohnt es sich?" · Z. 422, 454, 507, 560, 577 | Kapitalwertdifferenz V2 **+217.622 €**; gültig ist **+182.491 €** (`Beispielprojekt.md` Z. 149, `Dialog_Formel_Zahlenprobe.html` Z. 3422) | Zahl nachziehen — oder Datei nach `ueberholt/` (siehe § 4) | Mockup | hoch |
| a‑2 | ebenda · Z. 428, 436, 455, 511, 566, 665, 746, 986, 1015 | Kapitalwertdifferenz V3 **+1.877.827 €** (neunmal); gültig **+1.842.695 €** | wie a‑1 | Mockup | hoch |
| a‑3 | ebenda · „Die Kennzahlen dazu" · Z. 458/459 | Annuität V2 **14.628 €/a**, V3 **126.219 €/a**; gültig **12.266** und **123.858 €/a** (Beispielprojekt Z. 150) | wie a‑1 | Mockup | hoch |
| a‑4 | ebenda · Z. 462 | Dyn. Amortisation V2 **7,99 a**, V3 **2,62 a**; gültig **8,64** und **2,64 a** (Beispielprojekt Z. 151) | wie a‑1 | Mockup | mittel |
| a‑5 | ebenda · Z. 465 | Interner Zinsfuß V2 **12,8 %**, V3 **39,7 %**; gültig **11,5 %** und **39,2 %** (Beispielprojekt Z. 152) | wie a‑1 | Mockup | mittel |
| a‑6 | ebenda · Z. 468/469 | Wärmegestehungskosten V2 **26,44**, V3 **20,73 ct/kWh**; gültig **26,56** und **20,85 ct/kWh** (Beispielprojekt Z. 153) | wie a‑1 | Mockup | mittel |
| a‑7 | ebenda · Z. 472/473, 745 | Nettobarwert V2 **−7.685.091 €**, V3 **−6.024.886 €**; gültig **−7.720.222** und **−6.060.017 €** (Beispielprojekt Z. 148) | wie a‑1 | Mockup | hoch |
| a‑8 | ebenda · „2 Wie sicher ist das?" · Z. 503/504, 507/508, 511/512 | Bandbreite: V1 **1.501.405 / 1.817.126**, V2 **155.617 / 282.202**, V3 **1.657.023 / 2.099.328**; gültig **1.506.740 / 1.811.714**, **129.296 / 236.921**, **1.636.035 / 2.048.635** (Beispielprojekt Z. 162–164) | wie a‑1 | Mockup | hoch |
| a‑9 | ebenda · Z. 505, 509, 513 und Fließtext Z. 520 | Spannen **315.721 / 126.585 / 442.305 €**; aus den gültigen Werten folgen **304.974 / 107.625 / 412.600 €** (Mockup `Dialog_Formel_Zahlenprobe.html` Z. 3500/3504/3508) | wie a‑1 | Mockup | mittel |
| a‑10 | ebenda · „Was sich ändert" Z. 386/390 und „5 BHKW" Z. 925 | Erlöse Block A V2 **39.916,0**, V3 **93.121,9 €/a**; gültig **38.521,1** und **91.727,0 €/a** (Rechenweg 07 Z. 24, Beispielprojekt Z. 129/130) | wie a‑1 | Mockup | hoch |
| a‑11 | ebenda · „6 Photovoltaik" · Z. 1671, 1688, 1715 | PV-Vergütung Jahr 1 **10.396,34 €**; gültig **9.001,44 €** (Beispielprojekt Z. 135, Rechenweg 06) | wie a‑1 | Mockup | hoch |
| a‑12 | ebenda · Z. 1729 | PV-Reihe **nominal 199.545 € / Barwert 148.931 €**; gültig **150.118 / 113.800 €** (Beispielprojekt Z. 137) | wie a‑1 | Mockup | hoch |
| a‑13 | ebenda · „3 Woraus entsteht die Zahl?" · Z. 727–729 | Erlöse-Barwert V2 **588.108 (nominal 789.937)**, V3 **1.149.436 (1.504.725)**; gültig **552.977 (740.510)** und **1.114.304 (1.455.297)** (Beispielprojekt Z. 154) | wie a‑1 | Mockup | mittel |
| a‑14 | ebenda · Z. 640 | „Restwert-Barwert: **1.585.381** bzw. **1.979.864 €**" als Endwerte der Ungünstig-/Günstig-Kurve; im konsolidierten Mockup (Z. 3590/3632) stehen dafür **1.564.393 / 1.744.663 / 1.929.171 €** | wie a‑1 | Mockup | gering |
| a‑15 | `Status_iOS_Migration.md` · „4 Offen" · Z. 411 („Nach #348 (a)") | „Abschnitt 8 rechnet noch mit dem alten PV-Barwert 148.931 €" — **mit #351 erledigt**, der Eintrag steht noch offen | Eintrag streichen oder als erledigt kennzeichnen | Status | gering |
| a‑16 | `Status_iOS_Migration.md` · Z. 411 („Nach #348 (b)") | „Das konsolidierte Konzept führt in Z. ~841 die alten PV-Zahlen (10.396,34 usw.)" — **im Konzept nicht mehr auffindbar** (0 Treffer für `10.396,34`, `148.931`, `199.545` in `Wirtschaftlichkeit_Kosten/**`) | Eintrag streichen | Status | gering |

### (b) Schemaschritte — Stand 94, nächster freier 95

Gemessen: `EPOS.Kern/Allgemein/Update/SchemaStand.cs:265` `Zielversion = 94`;
`Referenzlaeufe/LIESMICH.md:153–160` „Schemastand 94 … Schritt 92 legt … Schritt 93 legt …
Schritt 94 trägt kein DDL".

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| b‑1 | `Konzept_…_konsolidiert.md` · Kopfzeile · Z. 3 | „`SchemaMigration.ZIEL_VERSION` = **61** · Schemaschritt **62** vergeben (U‑1), neue **ab 63**" — **überholt**: Zielversion 94, nächster freier 95; auch der Klassenname lautet heute `SchemaStand.Zielversion` | Kopfzeile auf „Zielversion 94 · Schritte 90–94 vergeben · neue ab 95" setzen | Konzept | hoch |
| b‑2 | `Konzept_…_konsolidiert.md` · § 3.6 Befund K‑1 · Z. 1851 | „nächster freier Schemaschritt ist **92** (90 ist BK1a, …)" — **überholt**: 92 (`ID_Referenzprojekt`, #358), 93 (`Uebernahme_Stamm`, #359) und 94 (Hilfsstrom-Vorlage, #365) sind vergeben | auf **95** setzen | Konzept | hoch |
| b‑3 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Anhang Umsetzungsstand, Zeile U1 · Z. 4360 | „**Schemaschritt 92** (90 und 91 sind vergeben)" — **überholt** (dieselbe Ursache wie b‑2); U1 ist offen, braucht also den nächsten freien Schritt | auf „Schemaschritt 95 (90–94 sind vergeben)" setzen | Mockup | mittel |
| b‑4 | `Konzept_…_konsolidiert.md` · § 6.3 Nr. 9b · Z. 2270 | „die **Zielversion bleibt 89**" — **überholt** (94). Die Aussage selbst („B7P braucht keinen Schemaschritt") bleibt richtig | Satz auf „die Zielversion wird davon nicht bewegt" umformulieren | Konzept | mittel |
| b‑5 | `Konzept_Nutzungsdauer_AfA_EPOS-Plan.md` · § 2 · Z. 76 | „Schemastand: **Zielversion 74**" — **überholt** (94) | auf 94 setzen oder Zahl streichen | Konzept | mittel |
| b‑6 | `Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md` · § 10.3 · Z. 465 | „**Zielstand:** `SchemaStand.Zielversion = 72`" — als Etappenbeschreibung richtig, liest sich aber wie der geltende Stand | „Zielstand **dieser Etappe**" ergänzen | Konzept | gering |
| b‑7 | `Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md` · § 2/§ 3/§ 10.3 · Z. 34, 146, 314, 438 | „Migrationsschritt **71**/**72**" — **richtig als Geschichte** (beide vergeben und gelaufen); keine Änderung nötig | — | — | — |
| b‑8 | `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` · Nachtrag 6 · Z. 632 | „die Strangzuordnung `Z_AnlageStrang` (**Migrationsschritte ab 65**)" — **überholt**; der Schritt ist längst gelaufen (Wechselrichter S1–S3 umgesetzt) | Klammer streichen, auf `Konzept_Wechselrichter` Kap. 8 verweisen | Konzept | mittel |
| b‑9 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Kat. 1 Ressourcentafel · Z. 1187; `Rechenweg/02` Z. 7/15; `Rechenweg/03` Z. 57 | „Pflichtpositionen (**Schemaschritt 59**)" — **richtig** (Historie, Saat der Pflichtzeilen) | — | — | — |
| b‑10 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Anhang · Z. 4696, 4709, 4717 | „Schemaschritt **92** (NULL = Stamm)" und „Schemaschritt **93**" in den erledigten Zeilen U37/U38 — **richtig** und deckungsgleich mit `Referenzlaeufe/LIESMICH.md` Z. 154–158 | — | — | — |
| b‑11 | `Konzept_…_konsolidiert.md` · § 5 U‑1 · Z. 2178–2181 | „**Schemaschritt 62** … Folge für den Schema-Nummernraum: 62 ist damit vergeben — neue Schritte anderer Etappen **ab 63**" — als Entscheidprotokoll vom 30.08.2026 richtig, als Regel **überholt** | Merksatz „ab 63" durch „ab 95" ersetzen oder ausdrücklich als historisch kennzeichnen | Konzept | mittel |

### (c) U-Nummern und Status

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| c‑1 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Kat. 2, Hilfsenergiezeile · **Z. 1069** | Marker „**umgesetzt U40**" — der Anhang Umsetzungsstand endet bei **U39** (Z. 4730). `U40` kommt in der Datei genau einmal vor und in keinem Papier | Anhangzeile U40 anlegen (Gegenstand: Vorlage „Standard" auf „% des Endenergiebedarfs", Schemaschritt 94, #365/#366) — oder den Marker auf eine bestehende Nummer ziehen | Mockup | hoch |
| c‑2 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Kat. 5 Ressourcentafel · **Z. 2713** | `WIRT_KWKG_ERSATZ_GEWICHTET · WIRT_KWKG_KONTINGENT_LEER · **geplant**` — zwei Schlüssel mit `rs-geplant`, **ohne U-Nummer**; die Anhangregel (Z. 4350) verlangt, dass „geplant" auf einen Punkt der Anhangtafel zeigt | U-Nummer ergänzen oder die zwei Schlüssel als „ohne Ressource" führen | Mockup | mittel |
| c‑3 | `Konzept_…_konsolidiert.md` · § 2.13 (3) Nr. 2 · Z. 941–943 | „der im Nutzungsdauer-Konzept vorgesehene Knopf „Nutzungsdauern vorbelegen" für bestehende Positionen ist **nicht gebaut**" — widerspricht Anhang **U8 erledigt** (HTML Z. 4407, Begründung Z. 4408–4420) und Statuszeile **#357** („Knopf … als vierter der Rasterleiste") | Punkt als erledigt kennzeichnen, Rest auf U39 einschränken | Konzept | hoch |
| c‑4 | `Konzept_…_konsolidiert.md` · § 2.13 (3) Nr. 3 · Z. 944–945 | „ein **Pflegeort für die Positionsart** — `NutzungsdauerID` wird ausschließlich bei der Vorlagenübernahme gesetzt, kein Dialog lässt sie wählen" — mit #357 gebaut („Positionsart als Klappliste im Zeileneditor") | streichen bzw. als erledigt kennzeichnen | Konzept | hoch |
| c‑5 | `Konzept_…_konsolidiert.md` · § 6.3 Nr. 9h · Z. 2316–2320 | „Nutzungsdauer, Ersatz, Restwert — **fünf** fehlende Stücke" nennt weiterhin „Nachpflege des Bestands (Knopf …)" und „Pflegeort der Positionsart"; nach dem Anhang bleibt allein **U39** (Entkopplung, geräteeigene Spalten, Speicherflotte, plattformfreie Hinweiszeile) offen | auf drei Stücke kürzen, U39 nennen | Konzept | hoch |
| c‑6 | `Konzept_…_konsolidiert.md` · § 6.3 B5-Kernaufgaben · Z. 2246–2248 | Nr. 1 „Schreibweg der drei B3a-Anlagenspalten — `KwkgAnlagenCtrl.Speichere` von 8 auf 11 Spalten (K7)" ist gebaut (`EPOS.Kern/Allgemein/Wirtschaftlichkeit/KwkgAnlagenCtrl.cs:256` `Speichere(g, mitSteuerangaben)`); Nr. 2 „Live-Frisch-Anzeige … im Kostendialog" ist mit **U31 erledigt** (HTML Z. 4609) und #347/#364 | beide Punkte abräumen | Konzept | mittel |
| c‑7 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Anhang · alle Zeilen | 39 Zeilen, **20 gestrichen** (U8, U16–U21, U23, U24, U26, U28–U31, U33–U38), **19 offen**. Gegenprobe: jeder Marker „umgesetzt Un" im Haupttext trifft eine gestrichene Zeile, jeder „Vorschlag Un" (U9, U22, U25, U27) eine offene — **einzige Ausnahme U40** (c‑1) | — | — | — |
| c‑8 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Haupttext | Neun **offene** Anhangpunkte haben im Haupttext weder Marker noch Ressourcenzeile: **U3, U4, U5, U11, U12, U14, U15, U32, U39**. Kein Regelbruch (die Anhangregel fordert nur die Gegenrichtung), aber sie sind im Bild unsichtbar | prüfen, ob je Punkt eine Stelle im Bild benannt werden kann | Mockup | gering |
| c‑9 | `Status_iOS_Migration.md` · Z. 422 („Nach #362 (a)") | „Die **49** Tafelzeilen mit „geplant · Un"" — die Datei trägt **50** solche Zeilen (plus die zwei aus c‑2 ohne Nummer, zusammen 52 `rs-geplant`) | Zahl auf 50 berichtigen oder Zahl weglassen | Status | gering |
| c‑10 | `Status_iOS_Migration.md` · Z. 417 („Nach #352 (a)") | „Mockup Abschnitt 7 zeigt weiter eine Spalte „Satz · Herkunft"" — **mit #356 erledigt**: das Mockup führt sie als Textzeilen „Satz Einspeisung · Herkunft" (HTML Z. 3143 und 3147) | Eintrag streichen | Status | gering |
| c‑11 | `Status_iOS_Migration.md` · Z. 417 („Nach #352 (b)") | „Ressourcentafel des Mockups nennt `BHW_SATZ_EIGENER_WERT`" — 0 Treffer in der Datei, mit #362 erledigt | Eintrag streichen | Status | gering |
| c‑12 | `Status_iOS_Migration.md` · Z. 419 („Nach #355 (b)") | „Mockup-Klappliste heißt weiter „Stammprojekt:" (VV‑Q7 offen)" — **halb erledigt**: Bild und Fließtext sagen „Projekt:" (HTML Z. 1542, 1563), die **Ressourcentafel Z. 1652 sagt weiter „Stammprojekt:"** | Eintrag auf die Ressourcentafel verengen; im Mockup Z. 1652 nachziehen | Status + Mockup | mittel |
| c‑13 | `Konzept_…_konsolidiert.md` · § 6.1 · Z. 2209–2228 | Die Etappentabelle führt BK1, BK1a, BK1b, VG und B7P, aber **weder „VV" (§ 2.16, Schemaschritt 93, #359) noch die Hilfsstrom-Umstellung (Schemaschritt 94, #365/#366)** | zwei Zeilen ergänzen | Konzept | mittel |

### (d) Begriffe und Bedienelemente

| Nr | Ort | Befund (beide Wortlaute) | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| d‑1 | Konzept § 2.2 Überschrift + § 7 Z. 119 / 2409 ↔ Mockup Kat. 5 Z. 2017 | Konzept: „`Form_BhkwWirtschaftlichkeit` — **neu (BW9)** … **Sechs Gruppen** an einem Ort"; § 7 führt B5 weiter als *vorgeschlagene* Etappe. Mockup: „`BhkwWirtschaftlichkeitDialog` … Das Formular führt die **acht Gruppen** des Dialogs in seiner Reihenfolge". Gebaut: `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor`, 1 293 Zeilen. Die zwei zusätzlichen Gruppen sind „**Angaben der gewählten Anlage**" (HTML Z. 2053, aus der Aufklappzeile der Konzept-Gruppe 1) und „**Kohärenzprüfung (Energie- und Stromsteuer)**" (Z. 2145) | § 2.2 auf acht Gruppen und auf den gebauten Dialog umstellen; § 7 B5 als umgesetzt kennzeichnen | Konzept | hoch |
| d‑2 | Konzept § 2.2 Z. 178–187 ↔ Mockup Kat. 5 Z. 2054, 2190, 2024 | Konzept: „**Der Vorschlag steht am Feld, nicht als Sammelknopf** (Anwenderwunsch 17.09.2026): Unter jedem der drei Felder … steht die Grundlage im Klartext und daneben der Knopf „Vorschlag übernehmen"". Mockup: Gruppenkopf trägt den **Sammelknopf** „**Sätze und Herkunft…**" (Z. 2054) bzw. „Wahl und Herkunft…"; „**Ein Knopf übernimmt alles in die Felder des Formulars**" (Z. 2194); die sechs Wahlfelder sind Anzeigezeilen, „die Wahl trifft die Überlagerung Vorschlag U22" (Z. 2024) | Entscheid klären (siehe § 5 Frage 3) und die unterlegene Fassung angleichen | Konzept **oder** Mockup | hoch |
| d‑3 | Konzept § 2.2 Codeblock Z. 184 ↔ Mockup Z. 2057/2061 | Konzept: „Einspeisung **5,57 ct/kWh** — 50 kW × 8,00 + …"; Mockup: „Einspeisung **5,5667 ct/kWh** — § 7 Abs. 1 KWKG 2025 …" (vier Nachkommastellen nach **U26**, umgesetzt mit #352) | Konzeptbeispiel auf 5,5667 / 2,4167 ziehen | Konzept | mittel |
| d‑4 | Konzept § 2.2 Gruppe 1 Z. 155 ↔ Mockup Z. 2075/2077 | Konzept: „**Hilfsenergieanteil [%]** … 0 = keine; Vorschlag BHKW 2–4 %"; Mockup: „Hilfsenergieanteil **[% des Endenergiebedarfs]** … Bemessen wird am **Endenergiebedarf (Brennstoff)** dieser Anlage — nicht an den Kosten" (Folge von Schemaschritt 94, #365/#366) | Feldbeschriftung und Bemessungssatz ins Konzept übernehmen | Konzept | mittel |
| d‑5 | Konzept § 2.2 Gruppe 4 Z. 205 ↔ Mockup Z. 2138 | Konzept nennt **einen** Sprungknopf „Strombezug…"; Mockup zeigt **zwei**: „Strombezug…" und „**BHKW-Tarif…**" | zweiten Knopf ins Konzept aufnehmen | Konzept | gering |
| d‑6 | Konzept § 2.3 Z. 227 ↔ Mockup Kat. 6 Z. 2748 | Konzept: „914 × 724, festes Fenster, Kopfband `#0F1F3D` …, **zwei Spalten**"; Mockup: „**Sieben Gruppen untereinander** in der Reihenfolge des Dialogs" | Konzept auf eine Spalte/untereinander ziehen (Razor-Port) | Konzept | mittel |
| d‑7 | Konzept § 2.3 Gruppe „Anlage" Z. 231 ↔ Mockup Z. 2772 | Konzept nennt vier Felder (Leistung · Override · Inbetriebnahme · Radio Einspeiseart); Mockup führt zusätzlich „**Degradation [%/a]: 0,50**" — mit #348 aus dem Kostenbild genommen und hier verortet | Feld in § 2.3 ergänzen | Konzept | mittel |
| d‑8 | Konzept § 2.3 Z. 229–237 ↔ Mockup Kat. 6 | Gruppenreihenfolge und -namen weichen ab: Konzept „Anlage · **Vermarktung** · **Anzulegender Wert** · § 51/§ 51a · Bezugsbewertung · 60‑%‑Begrenzung · Vorschau"; Mockup „Anlage · **Anzulegender Wert** · **Vermarktung** · **Vergütungsausfall (§ 51 / § 51a)** · **Strompreis / Bezugsbewertung** · 60‑%‑Wirkleistungsbegrenzung (§ 9 Abs. 2 EEG) · Σ Vorschau" | Namen und Reihenfolge im Konzept angleichen | Konzept | gering |
| d‑9 | Konzept § 2.3 Z. 239 ↔ Mockup Z. 3101/3109 | Fußleiste stimmt: „Marktwerte importieren… · Einspeise-Tarif… · Übernehmen · Abbrechen" ↔ Mockup „Marktwerte importieren… · Einspeise-Tarif…" + „Abbrechen · Übernehmen" | — | — | — |
| d‑10 | Konzept § 2.4 Z. 243–251 ↔ Mockup Kat. 8 „Dialog — Parameter" Z. 3305–3340 | Konzept: vier Gruppen **Allgemein · Strom · BEHG · Bilanzierung**; Mockup zeigt **Rahmen · Preissteigerungen · Szenarien** und im Rahmen zusätzlich „**Vergleichsprojekt**" (§ 2.9, Schemaschritt 92). Die Gruppen Strom/BEHG/Bilanzierung fehlen im Bild, die Szenariotafel fehlt im Konzept | § 2.4 um Vergleichsprojekt und Szenarien-Parametersatz ergänzen; im Mockup vermerken, dass nur ein Ausschnitt gezeigt wird | Konzept + Mockup | mittel |
| d‑11 | Konzept § 2.7 Z. 468–472 ↔ Mockup Z. 3340 / Anhang U2 (Z. 4369) | Konzept: „**Die Fußleiste von `UcWirtschaftlichkeit` ist voll** — **sieben Knöpfe**, ein achter läge bei x = −50 (Lücke K8)"; Mockup: „Die Fußleiste führt **vier Knöpfe** — Photovoltaik, BHKW, Strombezug, Berechnen", U2: „der Knopf „Verlauf…" fällt aus der Fußleiste, die dann **vier** Knöpfe trägt". Gemessen an `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor` Z. 371–390: **fünf** Knöpfe (Photovoltaik, BHKW, Strombezug, Verlauf…, Berechnen) → nach dem Wegfall vier | § 2.7 auf die Razor-Seite umschreiben (fünf Knöpfe, K8 gegenstandslos) | Konzept | mittel |
| d‑12 | Konzept § 2.12 Z. 836–838 und `Wirtschaftlichkeit_Kosten/LIESMICH.md` Z. 64–65 ↔ Mockup Z. 684/685 | Papiere: „Raster mit **Position · Kostenart · Bemessung · Satz · Menge mit Herleitungszeile · Betrag · Runde**"; Mockup-Rasterkopf: „**Aktionen · Position · Bemessung · Satz · Betrag netto [€] · Nutzungsdauer [a] · Worst/Best**" (Kostenart und Runde stehen im Zeileneditor bzw. in der Herleitungszeile) | Spaltenliste in Konzept und LIESMICH nachziehen | Konzept + LIESMICH | mittel |
| d‑13 | Konzept § 2.12 Z. 836 und LIESMICH Z. 64 ↔ Mockup Z. 233/693 | Papiere: „Reiter **Investition / Betrieb / Ertrag**" bzw. „Reiter **Investition / Betrieb / Ertrag-Bonus**" (drei); Mockup: **Optionsgruppe** „Betriebskosten / Investitionskosten" + **zwei Reiter** „**Kosten Invest/Betrieb**" und „**Ertrag/Bonus**" (`KDLG_TAB_KOSTEN`/`KDLG_TAB_ERTRAG`, #343) | in beiden Papieren nachziehen | Konzept + LIESMICH | mittel |
| d‑14 | Konzept § 2.8 Z. 525 und LIESMICH Z. 67 ↔ Mockup Z. 769/770 | Papiere: „Fußknöpfe: „Aus Vorlage übernehmen…" · „+ Position hinzufügen" · „Speichern"" (drei); Mockup: **vier** Rasterknöpfe „+ Position hinzufügen · Aus Vorlage übernehmen… · **Positionskatalog…** · **Nutzungsdauern vorbelegen…**" und darunter „Abbrechen · Speichern · **OK**" | Knopfliste nachziehen | Konzept + LIESMICH | mittel |
| d‑15 | Konzept § 2.8 Punkt 2 Z. 490 ↔ Mockup Z. 674 | Konzept: „Unter dem **Satz** steht die Herleitung im Klartext"; Mockup: „Die Herleitungszeile unter dem **Betrag** … sie nennt hier keine Runde" (U28, umgesetzt #345) | Bezugsort im Konzept berichtigen | Konzept | gering |
| d‑16 | Mockup Z. 784–786 ↔ Statuszeile #363 | Mockup: „„Aus Vorlage übernehmen…" öffnet die Überlagerung „Übernahme ins Projekt" (**Zielprojekt · Vorlage/Variante · Quellprojekt · Quellanlage**)"; gebaut ist seit #363 der **Katalogblock** (Komponente · Kategorie · Variante · Positionsvorschau mit Spalte „Ziel"). Die **Ressourcentafel** ist nachgezogen (`KUEB_SP_ZIEL`, `KUEB_POS_VORHANDEN`, `KUEB_KATALOG_LEER` stehen in Z. 985), der **Fließtext nicht** | Fließtext Z. 784–786 nachziehen | Mockup | mittel |
| d‑17 | Mockup Z. 1652 ↔ Konzept § 2.16 VV‑Q7 Z. 1359 | Ressourcentafel: „Reiter „Ertrag/Bonus": …, Klappliste „**Stammprojekt:**""; Entscheid VV‑Q7 und Dialogbild (Z. 1563): „**Projekt:**" | Ressourcentafel nachziehen | Mockup | mittel |
| d‑18 | Konzept § 2.11.7 Wortlaut Z. 816–822 ↔ Mockup Z. 4095–4101 | Konzept: „Was ein Szenario **heute** variiert …" mit Schlusssatz „Die vollständigen Parametersätze je Szenario … kommen nach dieser Darstellung; bis dahin steht dieser Hinweis unter der Tafel."; Mockup: „Was ein Szenario variiert …" mit anderem Schlusssatz „**Trägt das Projekt gepflegte Sätze, nennt der Hinweis die gepflegten Werte statt der Vorgaben.**" | einen Wortlaut führen (Ressource `WIRT_SZEN_HINWEIS`) | Konzept **oder** Mockup | mittel |
| d‑19 | Konzept § 2.13 Z. 898 ↔ § 2.13 (1)–(6) | „die **fünf** Punkte und ihre Messung" — der Abschnitt führt **(1) bis (6)**; § 2.15 Z. 1066 erklärt das („kein sechster Punkt in § 2.13"), die Zählung bleibt trotzdem stolprig | „fünf Punkte, dazu (6) als Verweis" | Konzept | gering |
| d‑20 | Konzept § 2.13 Z. 1011 | „der **Kopfabschnitt „Was sich ändert"** führt alle fünf Punkte" — dieser Abschnitt steht **nur im alten Mockup** `Ergebnis_Bandbreite_Herkunft.html` (Z. 300 ff.); `Dialog_Formel_Zahlenprobe.html` hat ihn nicht | Satz streichen oder auf das abzulösende Mockup beziehen | Konzept | mittel |
| d‑21 | Konzept § 2.2 Gruppe 3 Z. 199 ↔ Mockup Z. 2171 | Herleitungslabel-Beispiel im Konzept rechnet mit **2.480 MWh = 10.962 €/a** (fremdes Projekt); Mockup zeigt das Beispielprojekt **4.797,2 MWh × 4,42 € = 21.203,4 €** | Konzeptbeispiel auf das Musterprojekt ziehen | Konzept | gering |
| d‑22 | Konzept § 2.2/2.3/2.4 Überschriften ↔ Mockup | Durchgängig WinForms-Namen (`Form_BhkwWirtschaftlichkeit`, `Form_PhotovoltaikVerguetung`, `Form_WirtschaftlichkeitParameter`, `Form_KostenKomponente`) gegen die gebauten Razor-Dialoge (`BhkwWirtschaftlichkeitDialog.razor`, `PhotovoltaikVerguetungDialog.razor`, `WirtschaftlichkeitParameterDialog.razor`, `KostenKomponenteDialog.razor` — alle vorhanden) | Namen als Paar führen („`Form_…` → `…Dialog.razor`") | Konzept | gering |

### (e) `Ergebnis_Bandbreite_Herkunft.html` gegen das konsolidierte Mockup

Zahlenabweichungen siehe (a‑1) bis (a‑14). Darüber hinaus:

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| e‑1 | `Ergebnis_Bandbreite_Herkunft.html` · Z. 276 und 7 Bildtitel (Z. 534, 594, 983, 1032, 1412, 1685, 1727) | Kennzeichnung „**neu** steht an jeder Darstellung, die es heute noch nicht gibt" — 8 Marken `marke neu`. Das konsolidierte Mockup benutzt dieselbe Vokabel „neu" in **anderer Bedeutung** (neu angelegter **Ressourcenschlüssel**, 17 Stellen, z. B. Z. 1654–1656) und führt den Umsetzungsstand stattdessen über `umgesetzt Un` / `Vorschlag Un` / `geplant · Un` plus Anhang | eine Vokabel je Bedeutung; beim Ablösen entfällt die Doppelbelegung | Mockup | mittel |
| e‑2 | `Ergebnis_Bandbreite_Herkunft.html` · Z. 640, 1908 | „**interpolierte** Zwischenwerte" der beiden äußeren Verlaufskurven — im konsolidierten Mockup ist derselbe Umstand über die Klasse `zk abl` („abgeleitet", 9 Stellen) und den Anhang Herkunft geregelt; das alte Mockup hat keinen Anhang Herkunft | beim Ablösen fällt die Sonderregel weg | Mockup | gering |
| e‑3 | `Ergebnis_Bandbreite_Herkunft.html` · „Was der Tafel heute noch fehlt" Z. 817–826 | „Vorbelegt wird die Dauer aber nur beim Anlegen einer Position; **für bestehende Positionen gibt es keinen Handgriff**" — mit **U8/#357** überholt | Absatz nachziehen oder Datei ablösen | Mockup | mittel |
| e‑4 | `Ergebnis_Bandbreite_Herkunft.html` · „5 Blockheizkraftwerk" Z. 1325 ff. und „6 Photovoltaik" Z. 1615 ff. | **Zwei anlagenscharfe Ergebnisansichten** („woraus Erlös und Kosten dieser Anlage entstehen"), die das konsolidierte Mockup **nicht hat** — dort stehen an derselben Stelle die *Dialoge* (Kat. 5/6) und die BHKW-Vorschau. Die Ergebnisseite trägt aber die Knöpfe „Photovoltaik… · BHKW… · Strombezug…" (Kat. 8, Z. 3348), deren Zielansicht damit nirgends mehr gezeichnet ist; Konzept § 2.13 (1) Z. 915 verweist ausdrücklich auf „die **BHKW-Ansicht**" | vor dem Ablösen die zwei Ansichten ins konsolidierte Mockup übernehmen | Mockup | **hoch** |
| e‑5 | `Ergebnis_Bandbreite_Herkunft.html` · „Was sich gegenüber der heutigen Seite ändert" Z. 300 ff. | Der von Konzept § 2.13 Z. 1011 zitierte Kopfabschnitt steht nur hier (siehe d‑20) | vor dem Ablösen einen gleichwertigen Absatz im konsolidierten Mockup anlegen oder den Konzeptsatz streichen | Mockup + Konzept | mittel |
| e‑6 | `Ergebnis_Bandbreite_Herkunft.html` · „Was der Entwurf an Daten braucht" Z. 1831 ff.; „Der Restwert wiegt bei der Photovoltaik schwerer als der Ersatz" Z. 1814 | Zwei Textabschnitte ohne Gegenstück im konsolidierten Mockup (dort steht „Der Restwert ist eine **deklarierte Modellannahme**", Z. 961) | Inhalt sichten, Übernahmewürdiges mitnehmen | Mockup | gering |
| e‑7 | `Ergebnis_Bandbreite_Herkunft.html` gesamt | **Fünf** Abschnitte stehen wortgleich oder fast wortgleich in beiden Dateien („Warum die Spanne bei der Photovoltaik enger wirkt …", „Warum Strichart und nicht Band …", „Warum die Komponente innen gliedert …", „Eine Vereinfachung, die im Bericht steht …", „Die Differenz zweier Zahlungsbilder ist kein Zahlungsbild") — zwei Wahrheiten mit unterschiedlichen Zahlen darin | siehe § 4 | Mockup | hoch |
| e‑8 | `Status_iOS_Migration.md` · Z. 404 („Nach #344") | „`Ergebnis_Bandbreite_Herkunft.html` liegt weiter unter `aktuell/Mockups/`; **seine Abnahme ist nicht erklärt.** Wird es abgenommen oder durch das konsolidierte Mockup abgelöst, wandert es nach `ueberholt/`." — der Eintrag ist **zutreffend und weiterhin offen**; der Zahlenstand hat sich seither weiter auseinanderentwickelt | siehe § 4 und § 5 Frage 1 | Status | hoch |

### (f) Kopfzeilen und Stände

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| f‑1 | `Konzept_…_konsolidiert.md` · Z. 3 | „**Stand 02.09.2026** · Codestand `922228a` (Branch `ios_migration`) · `SchemaMigration.ZIEL_VERSION` = 61 · Schemaschritt 62 vergeben" — die Datei trägt inhaltlich den Stand **19.09.2026** (§ 2.15/§ 2.16 „umgesetzt", § 2.11.6/2.11.7 vom 18.09.2026) | Kopfzeile auf 19.09.2026 / Zielversion 94 / Schritte ab 95 setzen; Codestand entweder pflegen oder streichen | Konzept | hoch |
| f‑2 | `Dokumentation/LIESMICH.md` · Z. 129–139 | Alle **elf** Indexzeilen des Ordners `Wirtschaftlichkeit_Kosten/` tragen das Datum **2026‑09‑02**; die Dateien sind zuletzt am 18./19.09.2026 geändert worden (Konzept, Rechenweg 02, 03, 06, 08) | Daten nachziehen | Index | mittel |
| f‑3 | `Dokumentation/LIESMICH.md` · Z. 104 | Katalogfilter-Zeile „Stufen S1–S3 | 2026‑09‑13" — der Gegenstand ist vollständig umgesetzt (siehe j‑1) | nach dem Umzug in die `ueberholt`-Tabelle verschieben | Index | mittel |
| f‑4 | `Rechenweg/01…08` | **Kein** Rechenweg trägt eine Stand-/Codestandzeile; die Kopfzeile nennt nur Dialog, Mockup-Anker, Recht und Code. Das ist in sich stimmig (Stand kommt aus dem Index) | — | — | — |
| f‑5 | `Beispielprojekt.md` | Ebenfalls ohne Standzeile; die Datei ist inhaltlich auf dem Stand nach #351/#359 (PV-Reihe 150.118/113.800, Z. 137) | — | — | — |
| f‑6 | `Konzept_Katalogfilter_EPOS-Plan.md` · Z. 7–12 | „**Stand 07.09.2026: STUFE S1 IST UMGESETZT** … Die Stufen **S2** … und **S3** … **stehen aus**" — widerspricht dem eigenen Kapitel 9 („**Stufe S3 ist umgesetzt**", „**Stufe S2 ist umgesetzt**", mit Commits) | Kopfblock nachziehen | Konzept | hoch |
| f‑7 | `Konzept_Wechselrichter_EPOS-Plan.md` · Z. 26–27 ↔ Z. 3 und Z. 36–39 | Z. 3: „Rev. 5 … Stufen **S1, S2 und S3 UMGESETZT**"; Z. 26–27: „seit dem Entscheid vom 06.09.2026 ist Stufe S1 umgesetzt (Kapitel 8), **S2 und S3 sind es nicht**"; Z. 37: „**Alle drei Stufen sind umgesetzt**" | Z. 26–27 berichtigen | Konzept | hoch |
| f‑8 | `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` · Nachtrag 6 · Z. 636–637 | „**Nichts davon ist umgesetzt** — zehn Entscheidungsfragen W6‑E‑2‑Q1…Q10 liegen beim Anwender" — alle zehn sind am 06.09.2026 entschieden, S1–S3 umgesetzt | Absatz nachziehen | Konzept | hoch |
| f‑9 | `Konzept_…_konsolidiert.md` · § 6.2 Z. 2238 | Referenzbasis `Referenzlaeufe\2026-09-18_R9_Kesselbrennstoff` — **stimmt** mit `Referenzlaeufe/LIESMICH.md` Z. 150 überein | — | — | — |

### (g) Verweise

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| g‑1 | `Wirtschaftlichkeit_Kosten/LIESMICH.md` · „Verwandte Dokumente" · Z. 92 und Z. 93 | Zwei **tote** relative Verweise: `../KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` und `../Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` — beide liegen in `Dokumentation/**ueberholt**/` (per `find` belegt), nicht in `aktuell/` | Pfade auf `../../ueberholt/…` ziehen | LIESMICH | hoch |
| g‑2 | alle sechs Mockups | **Alle 21 internen Anker treffen.** `Dialog_Formel_Zahlenprobe.html`: 12 `href="#…"` gegen 13 `id=` (nur `#vergleichssicht` wird nicht verlinkt, aber von außen gebraucht); `Ergebnis_Bandbreite_Herkunft.html`: 8 gegen 8; `Katalogfilter_Vorschlag.html`: `#sym-trichter` vorhanden; die drei übrigen ohne `href` | — | — | — |
| g‑3 | `Rechenweg/01…08`, `Konzept § 2.12/2.13/2.15/2.16`, beide LIESMICH | **Alle** aus den Papieren referenzierten Mockup-Anker existieren: `#invest` `#betrieb` `#pvkosten` `#energie` `#bhkw` `#pv` `#erloese` `#valeri` `#sicht2` — je genau einmal im HTML | — | — | — |
| g‑4 | `Konzept_…_konsolidiert.md` · § 2.16 · Z. 1363–1364 | „Mockup: Kategorie 3 (Reiter Ertrag/Bonus) **und Kategorie 6 (Kopfzeile)** in `../Mockups/Dialog_Formel_Zahlenprobe.html**#pvkosten**`" — der Anker führt nur zu Kategorie 3 (Z. 1289); Kategorie 6 liegt unter `#pv` (Z. 2737) | zweiten Anker `#pv` ergänzen | Konzept | gering |
| g‑5 | `Mockups/Dialog_Formel_Zahlenprobe.html` · Z. 7 | Einziges Mockup mit **externer Ressource**: `https://fonts.googleapis.com/css2?family=IBM+Plex…`. `Wirtschaftlichkeit_Kosten/LIESMICH.md` Z. 34 sagt „beide **lokal** im Browser öffnen"; `Konzept_Katalogfilter` Z. 988 formuliert für sein Mockup ausdrücklich „eine Datei, **kein CDN**, keine Schriftdatei von außen" — die anderen fünf Mockups halten das ein | Schrift einbetten oder auf Systemschriften zurückfallen, damit die Seite offline gleich aussieht | Mockup | mittel |
| g‑6 | `Konzept_…_konsolidiert.md` · „Begleitende Artifacts" · Z. 47–52 und `Wirtschaftlichkeit_Kosten/LIESMICH.md` Z. 37 | **Sechs Artifact-Links** (nur genannt, nicht abgerufen): `e928091e-…` (B5-Dialogmockup), `588f6e21-…` (Rechenwege), `d924b2ec-…` (Erlösrubrik BHKW), `236c8a8a-…` (Pflichtpositionen, zweimal: Z. 50 und Z. 476), `f8968739-…` (ValERI Höfingen), `739d3cca-…` (Dialog, Formel, Zahlenprobe — dreimal: Konzept Z. 52 und Z. 830, LIESMICH Z. 37). Laut `Status_iOS_Migration.md` Z. 418 („Nach #354 (c)") ist **das Artifact 739d3cca nicht neu veröffentlicht**; es trägt damit den Zahlenstand **vor** #351 | am Link vermerken, dass die Repo-Datei führt und das Artifact älter ist — oder redeployen | Konzept + LIESMICH | mittel |
| g‑7 | `EPOS.Kern.Tests/DokumentationLinkWacheTests.cs` · Z. 398/402 und Z. 474–491 | Die Wache verlangt für jede Code-Spanne `…Mockups/<name>.html…` in `Dokumentation/aktuell/**/*.md`, im Index und in `CLAUDE.md` eine **vorhandene** Datei **genau** in `Dokumentation/aktuell/Mockups`. Zusätzlich nennt die **Gegenprobe** `Katalogfilter_Vorschlag.html` namentlich als positive Existenzprobe | vor jedem Umzug eines Mockups beachten (siehe j‑1 und § 4) | Test | hoch |

### (h) Wächtermuster der Wurzel-`CLAUDE.md`

Regex aus `CLAUDE.md` Z. 262 über alle sechs Dateien; die Regel gilt dort ausdrücklich für **Wiki-Entwürfe**,
nicht für Mockups — die Treffer sind deshalb nach Wirkung bewertet.

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| h‑1 | `Dialog_Formel_Zahlenprobe.html` | **4 Treffer**, davon 3 im Haupttext (Z. 2808 „Leistung oder Inbetriebnahme **geändert**", Z. 2874 „bitte **vorher** übernehmen", Z. 4064 „einer **geänderten** Projektangabe") — durchweg **normales Deutsch ohne Geschichtsbezug**; 1 Treffer im Anhang (Z. 4348 „Offene Anwenderfragen tragen „**Entscheid** ausstehend"") — **erlaubt**, der Anhang darf Geschichte tragen. Die Abnahme von #343 („Wächtermuster im Haupttext 0 Treffer") ist **dem Sinn nach gehalten**, dem Buchstaben nach nicht | Prüfregel auf „kein Treffer mit Geschichtsbezug" schärfen oder die drei Stellen umformulieren | Mockup | gering |
| h‑2 | `Ergebnis_Bandbreite_Herkunft.html` | **8 Treffer**: Z. 229 CSS-Kommentar `/* Befund */`, Z. 312/434/453/456/480/1839 „**Entscheidung**/Entscheidungskriterium" (Fachwort der Norm, unbedenklich), Z. 677 „liest sie in diesem Bild **bisher** nur nicht" (**echter Geschichtsbezug**) | Z. 677 umformulieren, falls die Datei bleibt | Mockup | gering |
| h‑3 | `Katalogfilter_Vorschlag.html` | **15 Treffer** — die meisten sind **echte** Geschichts- und Entscheidmarken: Z. 8 „MOCKUP zum Anwenderentscheid **W14a‑E‑10 vom 07.09.2026**", Z. 36/212/307/763/1465 „**bisher**/**vorher**", Z. 752/754 „**Befund D‑2**/**D‑1**", Z. 286/409/413/433 Wellenkürzel (`W9-B-2`, `iU8-E-2`, `W14a-E-7/E-8`), Z. 791/798 „Kern des **Entscheids**". Rund die Hälfte steht in HTML-/CSS-Kommentaren | bei einem Umzug nach `ueberholt/` unkritisch; bliebe es in `aktuell/`, sollte der Kopfkommentar entschlackt werden | Mockup | gering |
| h‑4 | `Wechselrichter_Mockup_2026-09-06.html` | **7 Treffer**: Z. 8 „MOCKUP zum Anwenderwunsch **W6‑E‑2 vom 06.09.2026**", Z. 303/490/492/522/1387 Wellenkürzel (`W6-O-4`, `W6-E-3`, `W16b-E-6`) mit Datum, Z. 410 „Empfehlung zu **Entscheidungsfrage** W6‑E‑2‑Q5" — dazu der sichtbare Stand-Absatz Z. 414–421 (siehe j‑3) | siehe j‑3 | Mockup | mittel |
| h‑5 | `stromspeicher-optimierung-v2.html` | **6 Treffer**: Z. 15/964/998 „**bisherigen**/**bisher**", Z. 1085 „fragt **vorher** nach", Z. 1100 „2 Speicheranlagen werden angelegt, 0 **geändert**" (Rückfragetext, unbedenklich), Z. 1119 „**Entscheid** vom 12.09.2026" | bei Umzug unkritisch | Mockup | gering |
| h‑6 | `Entwurf_Hydraulikuebersicht_Konfiguration.html` | **1 Treffer**, dafür der gewichtigste: sichtbare Kopfzeile Z. 27 „EPOS-Plan · **Stand 15.08.2026** · **Änderungen gegenüber v1**: …" — ein Entwurfsstand von vor über einem Monat, ohne zugehöriges Konzept | siehe j‑4 | Mockup | mittel |

### (i) Innere Konsistenz je Mockup

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| i‑1 | `Dialog_Formel_Zahlenprobe.html` · Navigation Z. 524–535 | **11 Navigationseinträge** ↔ **11 Abschnitte** (`projekt, invest, betrieb, pvkosten, energie, bhkw, pv, erloese, valeri, umsetzung, zahlen`) — vollständig und in der Reihenfolge der Seite | — | — | — |
| i‑2 | ebenda · Versionstafel „Die vier Versionen" Z. 573–612 | Geht gegen die Kategorien auf: Betriebskosten 2.400 / 59.564 / 8.131 / 65.295 = Kat. 2 (57.164,21 + 5.731,13 + 2.400, HTML Z. 1247/1254); Energiekosten 384.811 = Kat. 4 (Z. 1897); Erlöse 91.727 = Kat. 7 (Z. 3172) | — | — | — |
| i‑3 | ebenda · neun Abnahmezeilen (Z. 639, 998, 1279, 1661, 2002, 2718, 3106, 3285, 4330) | Jede Abnahmezeile nennt Zahlen, die im Abschnitt darüber stehen — **keine Abweichung** gefunden (Stichproben: 234.772,40 · 57.164,21/68.025,41 · 192.150,00/228.658,50 · 0,7560 €/m³ · 5,5667/2,4167 ct · 6,04 ct/9.001,44 € · 91.727,0 · +1.842.695) | — | — | — |
| i‑4 | ebenda · Legende Z. 631 ↔ Anhang Herkunft Z. 4749 ff. | Drei Klassen **Beispielzahl** (`zk bsp`, 10×) · **Beleg** (`zk beleg`, 12×) · **abgeleitet** (`zk abl`, 9×) stimmen mit dem Register des Anhangs überein; die Belege (Kaskadenprobe 1042 +20.927,61 · Mischsatz 5,5667/2,4167 · AW 6,04 · Aufschlagsmessung 1030 · Höfingen 65.259) decken sich mit `Beispielprojekt.md` § 6 und `Wirtschaftlichkeit_Kosten/LIESMICH.md` Z. 79–81 | — | — | — |
| i‑5 | ebenda · Ressourcentafeln | **52** Spannen `rs-geplant`, davon **50** mit U-Nummer (U1 ×4, U2 ×10, U6 ×6, U7 ×2, U10, U13, U22 ×16, U25 ×2, U27 ×8) — **alle neun genannten U sind im Anhang offen**; die zwei ohne Nummer sind c‑2 | siehe c‑2 | Mockup | mittel |
| i‑6 | ebenda · Kat. 5 Ressourcentafel Z. 2705–2711 | Die „geplant · U22"-Schlüssel der Überlagerung stehen der Konzeptregel § 2.2 („Vorschlag am Feld") entgegen — dieselbe Sache wie d‑2, hier als Ressourcenfolge | siehe d‑2 | Mockup/Konzept | mittel |
| i‑7 | `Ergebnis_Bandbreite_Herkunft.html` · Navigation Z. 285–292 | **8 Einträge** ↔ **8 Abschnitte** (`heute, lohnt, sicher, woraus, annahmen, bhkw, pv, daten`) — vollständig | — | — | — |
| i‑8 | `Ergebnis_Bandbreite_Herkunft.html` | Hat **keine** Versionstafel „Die vier Versionen", **keinen** Anhang Umsetzungsstand und **keinen** Anhang Herkunft; die Herkunft steht als Fließtext (Z. 1831 ff.). Die Marken `neu` ersetzen die U-Systematik — deshalb ist die Datei gegen den Umsetzungsstand **nicht prüfbar** | siehe § 4 | Mockup | mittel |

### (j) Die vier übrigen Mockups

| Nr | Ort | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|
| j‑1 | `Mockups/Katalogfilter_Vorschlag.html` ↔ `Konzept_Katalogfilter_EPOS-Plan.md` | **Umsetzungsstand laut Konzept:** Kapitel 9 — S1 (`78b0f1e`, `ce43d2a`, `f842465`), **S2** (sechs Commits, u. a. `5370b5a`), **S3** (vier Commits, u. a. `2186ab9`) — „21 Dialoge in 16 Komponenten tragen die eine `Katalogliste`". Das Mockup zeigt genau dieses Spaltenmodell (Trichter im Spaltenkopf, eine Zeile über der Liste, Liste über die ganze Breite) → **es beschreibt den gebauten Stand**, gehört nach der CLAUDE.md-Regel („umgesetzt oder abgelöst → `ueberholt/`") mitsamt seinem Konzept dorthin. **Aber:** `DokumentationLinkWacheTests` Z. 398/402 nennt die Datei namentlich als **positive Existenzprobe**, und `Konzept_Katalogfilter` Z. 50/988 verweist in Code-Spannen darauf — ein Umzug ohne Nachzug macht den Test rot | Umzug nur im Bündel: Konzept + Mockup nach `ueberholt/`, Code-Spannen mitziehen, Gegenprobe in `DokumentationLinkWacheTests` auf eine bleibende Datei umhängen, Indexzeile Z. 104 in die zweite Tabelle | Mockup + Konzept + Index + Test | hoch |
| j‑2 | `Konzept_Katalogfilter_EPOS-Plan.md` · Z. 7–12 | Kopfblock behauptet „S2 und S3 stehen aus" gegen das eigene Kapitel 9 (siehe f‑6) | Kopfblock nachziehen | Konzept | hoch |
| j‑3 | `Mockups/Wechselrichter_Mockup_2026-09-06.html` · **Z. 414–421** ↔ `Konzept_Wechselrichter_EPOS-Plan.md` Z. 29–34 | Mockup (sichtbarer Absatz, kein Kommentar): „**Solange Stufe S3 aussteht, rechnet der Kern die Stränge nicht**; … Die Zeile wird mit S3 wieder entfernt." Konzept: „in **S3 unverändert** … und der **S3-Hinweis ist dort fort**" — und Kapitel 8 Z. 1492/1569: „Stufe S3 — Rechenweg, Kennzahlen, Kosten — **UMGESETZT in `d88243e`**", „Nachtrag zu S3 … **UMGESETZT in `35a48eb`**". Das Mockup behauptet also einen Zustand, den das Konzept für beendet erklärt | Absatz entfernen (dann stimmt der Konzeptsatz) — und die Datei mitsamt Konzept nach `ueberholt/` | Mockup + Konzept | hoch |
| j‑4 | `Mockups/Entwurf_Hydraulikuebersicht_Konfiguration.html` · Z. 27 | **Kein zugehöriges Konzept in `aktuell/`.** Einziger Verweis: `Konzept_Repository_Aufraeumen_EPOS-Plan.md` Z. 38, und der nennt noch den **alten** Pfad `WindowsFormsApplication1/Allgemein/Simulation/…` mit der Einstufung „**Entwurf, Überreste** … verschieben (Stufe 1)". `Konzept_Simulationsablauf_EPOS-Plan.md` behandelt weder Hydraulikschema noch Wärmesenken-Dialog (0 Treffer). Die Datei trägt „Stand 15.08.2026 · Änderungen gegenüber v1" und vier Ansichten (Hydraulikschema, Kaskadenkette, Dialog „Wärmesenke", Dialog-Redesign „Simulation Konfiguration") | Anwender fragen, ob der Entwurf noch verfolgt wird (§ 5 Frage 5); wenn nein → `ueberholt/`; wenn ja → eigenes Konzept mit Indexzeile | Mockup | mittel |
| j‑5 | `Mockups/stromspeicher-optimierung-v2.html` ↔ `Konzept_Stromspeicher_Dialoge_EPOS-Plan.md` Z. 982 | Konzept § 8.4: „Das Mockup `Dokumentation/aktuell/Mockups/stromspeicher-optimierung-v2.html` (**Fassung vor dem Entscheid**: Herkunftspillen, Methode an die Herkunft gebunden) **wird auf diesen Stand nachgezogen**". Das Mockup ist **längst nachgezogen** — Kopf Z. 22–28: „NACHGEZOGEN AUF DEN ENTSCHEID (Auftrag #247, 12.09.2026): Die Einheiten tragen **KEINE Herkunftsmarkierung** mehr (SD‑Q15) — die Pillen … sind gefallen" und „NACHGEZOGEN AUF #273 (14.09.2026, Konzept 7.10)" | Konzeptsatz in die Vergangenheitsform ziehen („ist nachgezogen") | Konzept | mittel |
| j‑6 | `Konzept_Stromspeicher_Dialoge_EPOS-Plan.md` · § 5 Stufenplan Z. 544–553 | **Alle** Pakete umgesetzt: P1 (#183), P2 (#184), P3 (#192), P4 (#193), P4a (#226), P5 (#206), P6 (#224), P7 (#215), P8 (#224), P9 (#239) → Konzept und Mockup sind Kandidaten für `ueberholt/` | siehe § 4 | Konzept + Mockup + Index | mittel |
| j‑7 | `Mockups/stromspeicher-optimierung-v2.html` | Prüfung auf **entfallene Dinge**: „Nachtnutzung" (mit #288 entfernt) kommt in **keinem** der sechs Mockups vor (0 Treffer); der Einzelspeicher-Optimierer ist im Mockup nicht dargestellt. Der Knopf „Verlauf…" erscheint nur im konsolidierten Mockup, und dort als **entfallend** markiert (U2) | — | — | — |
| j‑8 | `Mockups/Wechselrichter_Mockup_2026-09-06.html` | Nachtrag W6‑O‑4/O‑6 ist eingearbeitet: Herstellerfilter (Z. 522) und Spalte „Modul" vorhanden — nur der S3-Hinweis blieb (j‑3) | siehe j‑3 | Mockup | — |

---

## 4 Empfehlung zu den Mockups

### 4.1 `Ergebnis_Bandbreite_Herkunft.html` — **ablösen, in zwei Schritten**

Die Datei ist kein zweiter Blickwinkel mehr, sondern eine **zweite Wahrheit mit falschen Zahlen**:
22 Beträge weichen ab (a‑1 … a‑14), fünf Textabschnitte stehen doppelt (e‑7), ein Absatz behauptet
einen mit #357 überholten Zustand (e‑3), und die Kennzeichnung „neu" kollidiert mit der Bedeutung
derselben Vokabel im konsolidierten Mockup (e‑1). Ein Nachziehen der Zahlen wäre Arbeit an einer
Datei, die danach immer noch dieselbe Doppelung trüge.

**Empfohlener Weg**

1. **Vor dem Umzug** die zwei Stücke retten, die nur hier stehen:
   die **anlagenscharfen Ergebnisansichten** „5 Blockheizkraftwerk" und „6 Photovoltaik"
   (e‑4 — sie sind die Zielansicht der Knöpfe „BHKW… · Photovoltaik…" der Ergebnisseite und
   werden von Konzept § 2.13 (1) ausdrücklich gebraucht) und, falls gewünscht, den Kopfabschnitt
   „Was sich gegenüber der heutigen Seite ändert" (e‑5, wegen Konzept § 2.13 Z. 1011).
   Beim Übernehmen die Zahlen aus `Beispielprojekt.md` nehmen, nicht die vorhandenen.
2. **Dann** `git mv` nach `Dokumentation/ueberholt/Mockups/` (oder `ueberholt/`) mit Indexzeile in
   der zweiten Tabelle von `Dokumentation/LIESMICH.md` und dem Grund „abgelöst durch
   `aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 8".
   Nachzuziehen sind dabei: `Dokumentation/LIESMICH.md` Z. 144, `Wirtschaftlichkeit_Kosten/LIESMICH.md`
   Z. 34, Konzept § 2.13 Z. 1011 (d‑20) und der Statuseintrag „Nach #344" (e‑8).
   **Kein Test bricht** — die Datei wird in keiner Code-Spanne mit `Mockups/`-Präfix genannt
   (geprüft gegen die Regel von `DokumentationLinkWacheTests`).

*Alternative, falls der Anwender die Seite als eigenständigen Entwurf behalten will:* Zahlen aus
`Beispielprojekt.md` durchziehen (22 Stellen, Zeilennummern in (a)), Absatz Z. 817–826 nachziehen
und im Kopf vermerken, dass bei Abweichung das konsolidierte Mockup gilt. Das ist die teurere
Variante und beseitigt die Doppelung nicht.

### 4.2 Die vier übrigen Mockups

| Mockup | Stand des Gegenstands | Empfehlung |
|---|---|---|
| `Katalogfilter_Vorschlag.html` | S1, S2, S3 **umgesetzt** (Konzept Kap. 9, 14 Commits) | **Nach `ueberholt/` — aber nur im Bündel** mit `Konzept_Katalogfilter_EPOS-Plan.md`, den zwei Code-Spannen (Z. 50, 988), der Indexzeile (Z. 104) **und** der Gegenprobe in `EPOS.Kern.Tests/DokumentationLinkWacheTests.cs` Z. 398/402, die die Datei namentlich als Existenzprobe benutzt (g‑7, j‑1). Vorher den Kopfblock Z. 7–12 berichtigen, damit die Geschichte stimmt, die nach `ueberholt/` wandert. |
| `Wechselrichter_Mockup_2026-09-06.html` | S1, S2, S3 + Nachtrag **umgesetzt** (Konzept Kap. 8) | **Erst nachziehen, dann nach `ueberholt/`.** Nachziehen: Absatz Z. 414–421 („Solange Stufe S3 aussteht …") entfernen — er widerspricht dem eigenen Konzept (j‑3). Im selben Schritt `Konzept_Wechselrichter` Z. 26–27 (f‑7) und `Konzept_Photovoltaik_Ertragsmodell` Z. 636–637 (f‑8) berichtigen; die Code-Spannen in beiden Konzepten mitziehen. |
| `stromspeicher-optimierung-v2.html` | P1–P9 **umgesetzt** (Konzept § 5) | **Nach `ueberholt/`**, zusammen mit `Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`. Vorher § 8.4 Z. 982 in die Vergangenheitsform ziehen (j‑5) — der Satz ist der einzige inhaltliche Fehler. Das Mockup selbst ist auf dem Stand #247/#273 und enthält nichts Entfallenes (j‑7). |
| `Entwurf_Hydraulikuebersicht_Konfiguration.html` | **Kein Konzept, kein Auftrag** — Entwurf v2 vom 15.08.2026, mit Stufe 1 (#242) aus dem Quellbaum hierher verschoben | **Anwenderfrage** (§ 5 Frage 5). Bis zur Antwort **bleiben**, aber mit einer Indexzeile bzw. einem Satz in `Dokumentation/LIESMICH.md`, der sagt, wozu er gehört. Wird er nicht weiterverfolgt: nach `ueberholt/`. |

`Dialog_Formel_Zahlenprobe.html` **bleibt** und ist das eine Mockup des Themas; es braucht nur die
kleinen Nachzüge c‑1, c‑2, d‑16, d‑17, b‑3 und g‑5.

---

## 5 Offene Fragen an den Anwender (mit Empfehlung)

1. **Wird `Ergebnis_Bandbreite_Herkunft.html` abgenommen oder abgelöst?**
   (Der offene Statuspunkt „Nach #344" fragt genau das; seither sind 22 Zahlen auseinandergelaufen.)
   *Empfehlung:* **ablösen** nach dem Weg in § 4.1 — erst die zwei anlagenscharfen Ansichten ins
   konsolidierte Mockup holen, dann `git mv` nach `ueberholt/`.

2. **Soll die Kopfzeile des konsolidierten Konzepts einen Codestand führen?**
   Sie nennt `922228a` vom 02.09.2026; der Stand wird offenkundig nicht gepflegt.
   *Empfehlung:* Codestand **streichen**, Datum und Zielversion pflegen — die Zielversion ist
   maschinell prüfbar (`SchemaStand.Zielversion`), ein Commit-Kürzel nicht.

3. **§ 2.2 „Vorschlag am Feld, nicht als Sammelknopf" gegen die Überlagerung „Sätze und Herkunft" (U22).**
   Das Konzept hält den Anwenderwunsch vom **17.09.2026** fest, das Mockup die Überlagerung.
   Beides zugleich gibt es nicht.
   *Empfehlung:* **beides** — die Grundlagenzeile samt Knopf „Vorschlag übernehmen" bleibt am Feld
   (sie ist im Mockup bereits gezeichnet, Z. 2057/2061), die Überlagerung ergänzt sie für die
   **Wahl** der sechs Klapplisten und die Herleitung. Dann ist § 2.2 nur um einen Satz zu erweitern
   statt zu kippen.

4. **Katalogfilter, Wechselrichter und Stromspeicher-Dialoge nach `ueberholt/`?**
   Alle drei Gegenstände sind vollständig umgesetzt; die CLAUDE.md-Regel verlangt den Umzug.
   *Empfehlung:* **ja**, aber als **ein** Auftrag je Thema mitsamt Konzept, Index, Code-Spannen und —
   beim Katalogfilter — der Test-Gegenprobe (g‑7). Sonst wird `DokumentationLinkWacheTests` rot.

5. **Wird der Entwurf „Hydraulik-Übersicht und Dialog-Redesign" (15.08.2026) noch verfolgt?**
   Er hat kein Konzept, keine Indexzeile und wird nur vom Aufräum-Konzept unter seinem alten Pfad
   erwähnt.
   *Empfehlung:* Wenn er Grundlage für den Konfigurationsdialog bleiben soll, bekommt er ein
   eigenes Konzept mit Indexzeile; sonst nach `ueberholt/`.

6. **Braucht `Dialog_Formel_Zahlenprobe.html` die Google-Schrift?**
   Es ist das einzige Mockup mit einer externen Quelle, während die LIESMICH „lokal im Browser
   öffnen" sagt (g‑5).
   *Empfehlung:* auf eine Systemschrift-Kette zurückfallen (wie die anderen fünf Mockups) — die
   Seite wird dann offline und im Ausdruck reproduzierbar.

7. **Soll das Artifact `739d3cca-…` neu veröffentlicht werden?**
   Es trägt nach `Status_iOS_Migration.md` Z. 418 den Stand vor #351 und wird an drei Stellen als
   Lesefassung angeboten (g‑6).
   *Empfehlung:* entweder redeployen (der Link bleibt stabil, `url` wiederverwenden) oder an allen
   drei Stellen vermerken, dass die Repo-Datei führt.

8. **Wie wird U40 geführt?**
   Der Marker steht im Bild (Z. 1069), die Anhangtafel endet bei U39 (c‑1).
   *Empfehlung:* Anhangzeile **U40** anlegen und **gestrichen** (erledigt) eintragen — Gegenstand
   „Vorlage „Standard": Hilfsenergie auf % des Endenergiebedarfs, Schemaschritt 94 (#365), Preis des
   eigenen Stromträgers je Anlage (#366)"; damit trägt jede Nummer im Bild ihre Zeile.
