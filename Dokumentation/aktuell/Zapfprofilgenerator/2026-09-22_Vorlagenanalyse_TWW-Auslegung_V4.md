# Vorlagenanalyse „TWW-Auslegung_V4.xlsx" für das Mockup Zapfprofilgenerator

Arbeitspapier (Scratch), Stand 22.09.2026. Quellen: Excel-Mappe V4 (Blattköpfe „Version 2.1.2"), Word-Dokumentation
„Wärmespeicher-Tool" (Version 1.1, Stand 29.07.2026, Teile A–D), KONTEXT/README/DESIGN des Streamlit-Tools —
alle in der Ablage des Anwenders (Z:, Ordner Wärmespeicher); im Repository `Konzept_TWW-Zapfprofile_WP-Plan_1.md`
(Inhalt V1.2) und `KONTEXT_Brauchwassertypen_VDI6002.md`.

## 1. Kurzfassung

- Die Vorlage ist eine **reine Formelmappe** (keine Makros, kein Blattschutz, rund 5.400 Formelzellen) zur
  **Auslegung eines Trinkwarmwasser-Speichers** mit **typisierten Wochen-Zapfprofilen**: 11 Nutzungstypen
  (+ Benutzerdefiniert), je drei Tagtypen Werktag/Samstag/Sonntag × 24 h, Wochenfaktoren Mo–So, ein Saisonfaktor.
- Sechs Blätter: Eingaben → Zapfprofil → DIN 4708-2 → Berechnung → Ergebnis → Hilfe. Version 2.1.2, Juli 2026, INEKON.
- Rechenkern: 336-h-Stundenbilanz (zwei gleiche Wochen) mit Lindley-Rekursion → maximales Energiedefizit D_max;
  daneben DIN 4708 (N, W_z), GLF-Faustwert (Gleichzeitigkeit nach lpagg) und klassischer Faustwert (nur nachrichtlich).
  Empfehlung = Maximum aus DIN 4708, profilbasiert und GLF, gerundet auf marktübliche Speichergrößen.
- Verhältnis zum **Streamlit-Wärmespeicher-Tool**: gleiche Methodik (Formeln gegen dessen Python-Module auf 0,00 %
  gegengerechnet), aber ohne Wetter/Heizung/Jahreslastgang; die Mappe ergänzt DIN 4708-2 (Wohnungstyp-Tabelle),
  GLF-Faustwert, Wochenprofile Werktag/Sa/So und Schätzhilfen. Sie ist „selbst die prüfbare Unterlage".
- Verhältnis zum **Konzept Zapfprofilgenerator** (Basis Konzept TWW-Zapfprofile V1.2): Die Mappe liefert die
  **deterministische Zeitstruktur-Schicht** (Tagesgang × Wochenfaktor × Saison) und ein bewährtes Bedienmuster;
  sie ist zugleich ein **Anschlussverbraucher** (Speicherauslegung), den der Generator später speisen soll.
  Die 11 Typen der Mappe liegen bereits als Brauchwasserkatalog in EPOS-Plan (168-h-Wochenprofile + Monatswerte).

## 2. Aufbau je Blatt

Farblegende der Mappe: gelb hinterlegt mit blauer Schrift = Eingabe; schwarz = Formel; grün = Verweis auf ein
anderes Blatt; hellblau hinterlegt, fett = „angesetzter Wert, der weiterverwendet wird".

### 2.1 Blatt „Eingaben" (neun Abschnitte)

| Abschnitt | Feld | Einheit | Default (Mappe) | Steuerung / Hinweistext |
|---|---|---|---|---|
| 1 Projektdaten | Objektname, Bearbeiter, Datum | – | Musterprojekt …, INEKON, 29.07.2026 | frei |
| 2 Gebäude/Nutzung | Nutzungstyp | Auswahl | Büro/Verwaltung | Liste = 11 Typen der Richtwerttabelle; „steuert Richtwert und Standard-Zapfprofil" |
| | Anzahl Einheiten | WE/Zimmer/Betten/Pers. | 60 | 1–100.000; „neutrale Bezugsgröße je nach Nutzungstyp" |
| | Personen je Einheit | P/Einheit | 2,2 | 0,1–50; „mittlere Belegung" |
| | Personen gesamt | P | Formel = Einheiten × P/Einheit | – |
| 3 Warmwasserbedarf | Bedarfsermittlung | auto/manuell | auto | „'auto' übernimmt den Richtwert aus der Tabelle unten" |
| | Richtwert lt. Tabelle | l/(Einheit·d) | Nachschlag | über Nutzungstyp, auf die Einheit umgerechnet |
| | Manueller Wert | l/(Einheit·d) | 77 | 0–2.000; wirkt nur bei „manuell" |
| | Angesetzter spez. Tagesbedarf | l/(Einheit·d) | Formel (hellblau) | „maßgebender Wert für alle Berechnungen (bei 60 °C)" |
| 4 Temperaturen | Speichertemperatur T_Sp | °C | 60 | 40–95; „DVGW W 551: ≥ 60 °C" |
| | Kaltwassertemperatur T_KW | °C | 10 | 2–25; Jahresmittel |
| | Zapftemperatur | °C | 45 | 30–60; „nur informativ — geht nicht in die Rechnung ein" |
| | nutzbare Spreizung ΔT | K | Formel = T_Sp − T_KW | – |
| 5 Anlagentechnik/Sicherheiten | WP-Ladeleistung P_lade | kW | 4 (hellblau) | „konstante Ladeleistung im Bilanzmodell — Wert aus Abschnitt 8" |
| | Zirkulationsverlust P_zirk | kW | 0,2 (hellblau) | „Dauerlast rund um die Uhr; 0 = ohne Zirkulation — Wert aus Abschnitt 9" |
| | nutzbarer Speicheranteil f_nutz | – | 0,80 | 0,3–1,0; „Schichtung/Totvolumen; üblich 0,7 … 0,85" |
| | Sicherheitszuschlag | – | 0,15 | 0–100 %; „10–20 %; bei kleinen Anlagen eher 20 %" (Einzeltagesspitzen) |
| 6 Richtwerttabelle | 11 Zeilen: Nutzungstyp, Richtwert, Bezug (Auswahl Person/Einheit), je Einheit·d (Formel), Bemerkung | l/(…·d) @60 °C | s. u. | Kopftext nennt die Quellen je Zeilengruppe; alle Werte editierbar |
| 7 N-Ermittlung DIN 4708 | Umschalter | Auswahl | DIN 4708-2 (Tabelle) | „vereinfacht (WE × Personen / 3,5)" oder „DIN 4708-2 (Tabelle)" |
| | N vereinfacht / N nach DIN 4708-2 / angesetztes N | – | Formeln | Kontrollzeile als Klartext: beide N, Abweichung in %, angesetzter Weg |
| | Personenzahl für Faustwerte | P | Formel | bei Tabellenwahl Σ(n·p), sonst Einheiten × P/Einheit |
| 8 Schätzhilfe P_lade | Ermittlung | auto/manuell | auto | „'auto' übernimmt den Vorschlag" |
| | Ladezeitfenster | h | 8 | > 0; „effektive TWW-Ladezeit, Sperr-/Heizzeiten abziehen" |
| | Tagesbedarf Zapfung (Mittel), max. Tagesbedarf inkl. Zirkulation, Vorschlag P_lade | kWh/d, kW | Formeln | Vorschlag = max. Tag / Fenster |
| | manueller Wert | kW | 20 | ≥ 0 |
| | Rechenweg (Anzeige) | Text | Formel | ein Satz mit allen Zahlen, endet mit „angesetzt: … kW (auto)" |
| 9 Schätzhilfe P_zirk | Methode | Auswahl | Leitungslänge | Leitungslänge / Anteil am Tagesbedarf / manuell |
| | Leitungslänge (Vor- + Rücklauf) | m | 100 | nur Methode Leitungslänge |
| | spez. Verlust je Meter | W/m | 10 | „gedämmte Leitung 60 °C; üblich 7–15 W/m" |
| | Anteil am Tagesbedarf | – | 0,20 | „MFH-üblich 15–30 %" |
| | manueller Wert | kW | 0 | nur Methode manuell |
| | Zwischenwerte, angesetzter Wert, Rechenweg | kW, Text | Formeln | Rechenweg z. B. „100 m × 10 W/m / 1000 = 1 kW = 24 kWh/d" |

Richtwerttabelle (Abschnitt 6), Zeilen 36–42 „in Anlehnung an DIN V 18599-10 / VDI-Praxis" (INEKON-Werte):
MFH Wohnen 35 je Person (30–45); EFH Wohnen 35 je Person (30–45); Hotel 60 je Einheit = Zimmer (40–100);
Pflegeheim 50 je Einheit = Bett (40–70, inkl. Pflegebäder); Büro/Verwaltung 8 je Person (5–12, Handwaschbecken);
Sportstätte/Duschen 30 je Person = Duschender (25–45); Schule 5 je Person (ohne Duschen). Zeilen 43–46:
Wohnen groß (VDI 6002) je Person, Studentenwohnheim/Seniorenheim/Krankenhaus (VDI 6002-2) je Einheit
(Vollbelegungsperson bzw. Bett) — **Werte aus VDI 6002 Blatt 1 Anhang D bzw. Blatt 2 Tabellen 2/4/5 übernommen**
(Jahresmittel; die Bemerkung nennt zusätzlich Winterspitze und Sommerschwachlast). Umrechnung je Zeile:
„je Einheit·d = Richtwert × Personen je Einheit, falls Bezug = Person, sonst Richtwert".

Referenzfall (Zeile 49, als Klartext): 30 Einheiten × 2,2 Personen, 35 l/(P·d), 60/10 °C, f_nutz 0,80, Zuschlag 15 %,
P_lade 20 kW (manuell), Zirkulation 0 kW (manuell), N vereinfacht.

### 2.2 Blatt „Zapfprofil"

- **Abschnitt 1 — Profilauswahl, Saisonfaktor, Kontrollen:** Auswahl „(automatisch)" (folgt dem Nutzungstyp) oder
  jeder der 11 Typen oder „Benutzerdefiniert"; Anzeige „verwendetes Profil". Saisonfaktor (Eingabe, Default 1,00,
  0,10–3,00; Hinweistext nennt ein Februar-Maximum und eine Schwachlast für Wohnen nach VDI 6002 Bl. 1 Bild D1).
  Vier **Summenkontrollen** (Werktag, Samstag, Sonntag, Wochenfaktoren) mit Klartext „OK — summiert zu 100 %." bzw.
  „WARNUNG: Summe ≠ 100 % — korrigieren!", Toleranz 0,5 %, bedingt grün/rot formatiert.
- **Abschnitt 2 — Profilbibliothek:** Stunde 0–23 in Zeilen; je Nutzungstyp ein Block aus drei Spalten
  Werktag/Samstag/Sonntag (Anteil des Tagesbedarfs je Stunde, Summe je Spalte = 100 %); Reihenfolge: Wohnen groß
  (VDI 6002), Studentenwohnheim, Seniorenheim, Krankenhaus (VDI 6002-2), MFH, EFH, Hotel, Pflegeheim, Büro, Sportstätte,
  Schule, **Benutzerdefiniert** (gelb, Startwerte = MFH), **Aktives Profil** (Formel, folgt der Auswahl).
  Unter jedem Block eine Summenzeile (bedingt markiert), eine Zeile **„Quelle / Herleitung"** und eine Zeile
  **„Status"**: „VDI-Wert (Tabellenwerte)", „abgelesen (Bild …)", „generisch, anpassbar", „Eingabe — frei editierbar".
  Krankenhaus: alle drei Tagtypen gleich; Seniorenheim: Sa = So. Büro und Schule: Sa/So nahe null (Grundlast).
- **Abschnitt 3 — Wochenfaktoren:** Tabelle Nutzungstyp × Mo…So + Σ + Quelle (12 Zeilen inkl. Benutzerdefiniert),
  Zeile „Aktiv" wird von der Berechnung gelesen. Beispiel generisch: Büro Mo–Fr je 19 %, Sa/So je 2,5 %;
  Schule Mo–Fr je 19,4 %, Sa/So je 1,5 %.
- **Abschnitt 4 — Referenz (nur informativ):** VDI 6002 Bl. 2 Tab. 3 mit fünf Tagtypen (Mo / Di–Do / Fr / Sa / So-Feiertag)
  zum manuellen Übertragen in „Benutzerdefiniert" — Normtabelle, **im Mockup nicht abbilden**.
- **Diagramm 1:** gruppiertes Säulendiagramm „Aktives Zapfprofil — Anteil des Tagesbedarfs je Stunde (drei Tagtypen)".
- Schlusshinweis: abgelesene Profile summieren exakt zu 100 %; generische Profile sind Richtwerte, keine Messwerte;
  Hallenbad- und Campingprofile der VDI 6002 Bl. 2 bewusst nicht aufgenommen.

### 2.3 Blatt „DIN 4708-2"

- Wirksam nur bei Umschalter „DIN 4708-2 (Tabelle)". Kopf: Formel N = Σ[n·p·(v·w_v)] / (p_b·w_b) mit p_b = 3,5 und
  w_b = 5820 Wh (Einheitswohnung nach DIN 4708-1); v = Anzahl gleichwertiger Zapfstellen (Regelfall 1).
- **Wohnungstypen-Tabelle (bis 10 Zeilen):** Anzahl n, Zimmerzahl r, p aus Tabelle, p manuell (überschreibt),
  Belegungszahl p, Ausstattung (Auswahl, maßgebende Zapfstelle), w_v, v, Personen n·p, Beitrag n·p·v·w_v, Bemerkung.
  Summen Σn, Σ(n·p), Σ Beiträge; Ergebnisblock: Bezugswert 20.370 Wh, **N**, Σ Personen (Bezugsgröße GLF),
  mittlere Belegung. **Kontrollzelle** mit drei Klartextfällen (keine Zeile / fehlende Ausstattung / p = 0) sonst
  „OK: 30 Wohnungen, 75 Personen, N = 17,19."; darunter eine Handrechnung zur Kontrolle (Beispielbestand
  20 × 2-Zi. mit Brause + 10 × 4-Zi. mit Normalwanne → N = 17,19).
- **Nachschlagetabelle p über r:** 13 Stufen r = 1 … 7 in halben Zimmern, „aufsteigend halten", Zuordnung nächstkleiner;
  4 Zi. = Einheitswohnung. **Nachschlagetabelle w_v:** fünf Ausstattungen (Normalwanne = Bezugszapfstelle, Kleinwanne,
  Brause, Großwanne, Wanne + separate Brause). Beide Tabellen tragen den Vermerk „Werte nach Sekundärliteratur zu
  DIN 4708-2 — vor Verwendung gegen den Normtext prüfen" (Normtabellen: Zahlen nicht übernehmen, s. Kap. 7).

### 2.4 Blatt „Berechnung"

- **1 Grundwerte:** Personen maßgebend; spez. Tagesbedarf; V_60 = Einheiten × spez. Bedarf [l/d]; Bezugstemperatur 60 °C
  (Konstante); Q_zapf [kWh/d]; Zirkulation × 24 h; Tagesbedarf gesamt; P_lade; Ladeenergie P_lade × 24 h;
  **Plausibilität Ladeleistung** (Klartext OK/WARNUNG „Speichervolumen ersetzt keine Ladeleistung!");
  rechnerische Mindest-Ladeleistung; Saisonfaktor; Q_woche = 7 × Q_zapf.
- **2 Profilbasiert — 336-h-Bilanz** (Zeilen 22–357, Woche 1 „Einschwingen", Woche 2 „maßgebend"), Spalten:
  t · Tag (1–14) · WT-Nr · Wochentag · Stunde · Tagtyp · Wochenfaktor · Profilanteil · Zapfbedarf [kWh/h] ·
  Zirkulation [kWh/h] · Bedarf ges. · Ladung · Netto · **Defizit kum.** (Lindley) · Label „Mo 17:00" · **Speicherfüllstand**.
  Ergebnis: D_max, Zeitpunkt (Tag 1–14, Wochentag, Stunde, Tagtyp), Hinweis Wochenende/Werktag bzw.
  „kein Defizit — die Ladeleistung deckt jede Zapfspitze", V_nutz, **V_profil**; Kontrolle „Zapfenergie Woche 2 =
  Q_woche × Saisonfaktor" (Klartext, bedingt rot).
- **3 DIN 4708:** p = 3,5; N (aus Eingaben); z = 1/6 h; W_b = 5820 Wh; u1, u2, K(u1), K(u2); W_z(N) in Wh und kWh;
  V_nutz; **V_DIN** (ohne Zuschlag — „Norm enthält Reserven").
- **4 Klassischer Faustwert:** 35 l/(P·d) bei 60/10 °C; V_60 = Personen × 35; **V_Faust** energiegleich auf T_Sp
  umgerechnet, mit Zuschlag; Hinweis „unterstellt EINE Speicherladung je Tag".
- **5 GLF-Faustwert:** Personen, N; u1/u2/K für N = 1; W_z(1); **GLF(N)** mit Anzeige „GLF(17,2) = 0,26";
  Spitzenbedarf je Person W_z(1)/3,5; V_spitze,p; V_nutz; **V_GLF**; Hinweis zur Nichtlinearität.
- **6 Hilfsgrößen:** D_max als Zahl, Schalter 0/1 „profilbasiert vorhanden" (Logik nie über Textvergleich).
- **7 Speicherfüllstand:** Auswahl der Speichergröße für die Grafik (Empfehlung / DIN 4708 / Profilbasiert / GLF /
  klassisch), Bezugsvolumen (Rückfall auf Empfehlung), V_nutz, **C_sp**, minimaler Füllstand der Woche 2 absolut und relativ.

### 2.5 Blatt „Ergebnis"

- **Kopf:** Objekt, Datum, Nutzungstyp, Umfang als Satz („60 Einheiten × 2,2 P = 132 Personen"), Zapfprofil, Saisonfaktor.
- **1 Verfahrensvergleich** (Spalten Verfahren · Volumen [l] · Kennwert · Methodik-Kurzfassung · Hilfsspalte Zahlwert):
  DIN 4708 (Kennwert W_z) · Profilbasiert (Kennwert D_max; bei D_max = 0 Anzeige „–") · Faustwert mit Gleichzeitigkeit
  (Kennwert GLF und V_spitze,p) · **klassischer Faustwert — grau, „NUR NACHRICHTLICH — geht nicht in die Empfehlung ein"**.
  Darunter Gleichzeitigkeitsfaktor mit Erklärung und „Hinweis profilbasiert" (Zapfspitzen < 1 h unsichtbar → DIN 4708 maßgebend).
- **2 Empfehlung:** maßgebendes Verfahren (Name), maßgebender Tag „Mo (Tag 1 von 14), 17:00 Uhr", Hinweis Wochenende,
  Maximum ungerundet, **Empfehlung — nächstgrößere marktübliche Größe** (hervorgehoben, dunkelrot fett auf hellorange),
  **Leistungskennzahl** „Speicher mit NL ≥ 17,2 wählen", minimaler Speicherfüllstand mit Satz zu C_sp und Restreserve.
- **3 Hinweise und Warnungen** (Klartext je Zeile, WARNUNG bedingt rot): Legionellenschutz (Empfehlung > 400 l →
  Großanlage DVGW W 551: ≥ 60 °C Austritt, Zirkulationsrücklauf ≥ 55 °C, wöchentliche Erwärmung, bei WP
  Frischwasserstation prüfen; sonst Kleinanlagen-Satz); Zapfprofil (Summenkontrollen); Ladeleistung (P_lade × 24 h <
  Tagesbedarf → Mindestleistung nennen, „Volumen ersetzt keine Leistung"; sonst „x Speicherladungen je Tag möglich");
  Spitzentag (Wochenfaktor des maßgebenden Tagtyps in %); klassischer Faustwert (> 3 × Maximum der Verfahren →
  Einordnung); Speichertemperatur < 60 °C (thermische Desinfektion/FriWa nachweisen); Bedarfskennzahl N (Vergleichstext);
  Schätzhilfen (angesetzte P_lade, P_zirk mit Methode, Zuschlag).
- **4 Speichergrößen-Liste** (anpassbar, aufsteigend): 100, 150, 200, 300, 400, 500, 800, 1.000, 1.500, 2.000, 3.000,
  5.000, 8.000, 10.000 l; darüber auf volle 1.000 l („Mehrspeicheranlage prüfen").
- **5 Diagramm 2 „maßgebende Woche (Tag 8–14)":** gestapelte Säulen Zirkulation + Zapfbedarf [kWh/h], Linie Ladung
  [kWh/h], auf zweiter Achse Linien Defizit kum. und Speicherfüllstand [kWh]; Kopfzeile „V_graf = 800 l → C_sp = 37,2 kWh
  | min. Füllstand 5,9 kWh (15,9 %)".

### 2.6 Blatt „Hilfe"

Kurzanleitung in acht Schritten (Blattfolge), Methodik-Kurzfassung aller Verfahren (Kap. 3), Abschnitt DIN 4708-2 mit
Prüfvermerk, Schätzhilfen, Abschnitt „Darstellung bei D_max = 0", **Hinweise und Grenzen** (N vereinfacht bei gehobener
Ausstattung auf der unsicheren Seite; generische Profile keine Messwerte; keine Schichtung/Speicherverluste; Ladeleistung
konstant über 24 h, kein Sperrzeitenmodell; bei zu kleiner Ladeleistung wächst das Defizit monoton — dann ist D_max
kein Volumen, sondern ein Leistungssignal; Saisonfaktor ersetzt kein Jahresprofil; GLF-Faustwert setzt die Wannen-
Zapfperiode an und ist für Büro/Schule nur eingeschränkt aussagefähig; kein Ersatz für Hydraulik/FriWa-Planung),
**Quellen** (DIN 4708-1/-2/-3, DIN V 18599-10, VDI 6002 Bl. 1 und 2, DVGW W 551, VDI 6023, lpagg `din4708.py` MIT,
Wärmespeicher-Tool `sizing_dhw.py`), Stand „Juli 2026, Version 2.1.2 — alle Werte vom Bearbeiter fachlich zu verantworten".

### 2.7 Zahlenstand der Mappe und Auffälligkeiten (nicht ins Mockup übernehmen)

- Gespeicherter Stand (Büro, 60 Einheiten, N aus der Beispiel-Wohnungstabelle): Q_zapf 61,4 kWh/d; D_max 31,3 kWh
  (Mo 17 Uhr); V_DIN 489 l; V_profil 774 l; V_GLF 791 l (maßgebend); klassisch 3.019 l; Empfehlung 800 l.
- Referenzfall Zeile 49 nachgerechnet (MFH-Profil): N 18,86; V_DIN 515 l; D_max = 0 → „–"; V_GLF 661 l (GLF 0,24);
  klassisch 2.656 l; Empfehlung 800 l — deckt sich mit der Word-Dokumentation („800 l statt 3.000 l").
- **Umschalter P_lade/P_zirk nicht verdrahtet:** die hellblauen Zellen „angesetzt" (4 kW, 0,2 kW) sind feste Zahlen;
  die Vorschläge der Schätzhilfen (10,8 kW; 1,0 kW) gehen nicht ein, der Rechenweg-Text zeigt den Widerspruch
  („Vorschlag … 10,8 kW — angesetzt: 4 kW (auto)"). Im Mockup: angesetzter Wert immer sichtbar aus dem Umschalter abgeleitet.
- **Gemischtes Mengengerüst:** V_60 rechnet mit Einheiten × spez. Bedarf (132 P), die Faustwerte bei Tabellenwahl mit
  Σ(n·p) (75 P) — Verfahren vergleichen unterschiedliche Gebäude. Im Mockup: ein Mengengerüst je Zone.
- Maßgebender Tag wird als Tag 1 (Woche 1) ausgewiesen, obwohl Woche 2 „maßgebend" heißt (erste Fundstelle).
- Überschriften „drei Verfahren" bei vier Tabellenzeilen; Hilfe-Grenzen beschreiben noch nur N vereinfacht.
- GLF ist im gespeicherten Büro-Fall maßgebend, obwohl die Hilfe ihn für Büro als eingeschränkt aussagefähig nennt —
  eine Gültigkeitswarnung je Verfahren fehlt.

## 3. Formelsammlung (aus „Hilfe" und „Berechnung")

```
Energie <-> Volumen   c_w = 1,163 Wh/(l*K);  dT = T_Sp - T_KW
                      Q[kWh] = V[l] * 1,163 * dT / 1000;   V[l] = Q[kWh] * 1000 / (1,163 * dT)
Tagesbedarf           V_60 = Einheiten * spez. Tagesbedarf [l/d @60 °C]
                      Q_zapf = V_60 * (60 - T_KW) * 1,163 / 1000   [kWh/d]
DIN 4708              N_vereinfacht = Einheiten * Personen je Einheit / 3,5          (v = w = 1)
                      N_Tabelle     = Summe(n * p * v * w_v) / (3,5 * 5820 Wh)        (DIN 4708-2)
                      z = 1/6 h;  u1 = 0,244 * z * (1 + sqrt N) / sqrt N;  u2 = 3,599 * z * (1 + sqrt N) / sqrt N
                      K(u) = erf(u) fuer u < 1,81, sonst 1
                      W_z(N) = 5820 Wh * [ N * K(u1) + sqrt N * K(u2) ]
                      V_DIN = W_z / (1,163 * dT) / f_nutz                             (ohne Zuschlag)
Profilbasiert         Q_woche = 7 * Q_zapf
                      Bedarf(t) = Q_woche * WF(Wochentag) * s(Tagtyp, Stunde) * f_Saison + P_zirk * 1 h
                      Tagtyp: Mo-Fr Werktag, Sa Samstag, So Sonntag;  Summe WF = 1;  Summe_h s = 1
                      Defizit(t) = max(0; Defizit(t-1) + Bedarf(t) - P_lade * 1 h),  Defizit(0) = 0   (Lindley)
                      D_max = max ueber 336 h;  V_profil = D_max * 1000 / (1,163 * dT) / f_nutz * (1 + Zuschlag)
                      Kontrolle: Summe Zapfbedarf Woche 2 = Q_woche * f_Saison
GLF-Faustwert         W_z(1) = 5820 Wh * [K(u1,N=1) + K(u2,N=1)]  (ca. 5831 Wh)
                      GLF(N) = W_z(1) / W_z(N)                     (lpagg calc_GLF, nicht Teil der DIN 4708)
                      V_spitze,p = W_z(1) / 3,5 / (1,163 * dT) / f_nutz
                      V_GLF = Personen * V_spitze,p * GLF(N) * (1 + Zuschlag)
Klassischer Faustwert V_klass = Personen * 35 l/(P*d) * (60 - 10) / dT * (1 + Zuschlag)      (nur nachrichtlich)
Empfehlung            V_max = max(V_DIN; V_profil; V_GLF)
                      V_empf = kleinste Listengroesse >= V_max;  > 10.000 l: auf volle 1.000 l aufrunden
                      Leistungskennzahl: Speicher mit N_L >= N waehlen
Fuellstand            C_sp = V_graf * f_nutz * 1,163 * dT / 1000;  SOC(t) = max(0; C_sp - Defizit(t))
                      Restreserve = min SOC(Woche 2) / C_sp
Schaetzhilfen         Q_max,d = Q_zapf * 7 * max(WF) * f_Saison + P_zirk * 24 h;   P_lade,Vorschlag = Q_max,d / t_Lade
                      P_zirk = L * q' / 1000  |  a * Q_zapf / 24 h  |  manuell
Plausibilitaet        P_lade * 24 h >= Q_zapf + P_zirk * 24 h;   P_lade,min = (Q_zapf + P_zirk * 24 h) / 24 h
```

## 4. Bedienlogik und Gestaltung als Vorlage für den Dialog

- **Farbsemantik statt Hilfetext:** Eingabe / berechnet / Verweis / „angesetzt und weiterverwendet" sind optisch
  unterscheidbar. Im Mockup: Eingabefeld, Anzeigefeld, Verweisfeld (Herkunft anklickbar), hervorgehobener Übernahmewert.
- **auto/manuell-Umschalter** (Tagesbedarf, P_lade) und **Methodenwahl** (P_zirk, N-Ermittlung): Vorschlagswert und
  manueller Wert stehen nebeneinander, der angesetzte Wert ist eigene Zeile, der **Rechenweg** steht als ein Satz mit
  allen Zahlen darunter. Muster direkt übertragbar (aufklappbarer Rechenweg).
- **Schätzhilfen** als Unterabschnitte mit eigenen Eingaben (Ladezeitfenster; Leitungslänge × W/m; Anteil am Tagesbedarf).
- **Summenkontrollen** mit Klartext „OK …" / „WARNUNG … korrigieren!" und farbiger Markierung; Kontrollen sind nicht
  blockierend, das Ergebnisblatt fasst sie noch einmal zusammen.
- **Plausibilitätsgrenzen** je Eingabe (Datenüberprüfung mit Klartext-Fehlermeldung, z. B. „40 bis 95 °C").
- **Herkunft je Profil:** Quellzeile und Status (VDI-Wert / abgelesen / generisch / Eingabe) direkt unter den Daten.
- **Verfahrensvergleich nebeneinander**: je Verfahren Volumen, Kennwert, Methodik in einem Satz; das maßgebende
  Verfahren wird benannt; der klassische Faustwert bleibt sichtbar, aber grau und „nur nachrichtlich".
- **Empfehlung gerundet** auf eine editierbare Größenliste, dazu die **N_L-Kennzahl** als Auswahlkriterium.
- **Hinweise als Klartext-Sätze** mit eingesetzten Zahlen (keine Codes), WARNUNG rot hervorgehoben.
- **„D_max = 0"-Erklärung**: statt „0 l" ein Strich und der Satz „Ladeleistung deckt jede Stundenlast … maßgebend ist
  dann das DIN-4708-Verfahren". Logik über Zahl-Hilfsgrößen, nie über Textvergleich.
- **Maßgebender Zeitpunkt** als Satz („Mo (Tag … von 14), 17:00 Uhr") plus Hinweis, wenn er auf ein Wochenende fällt.
- **Referenzfall** als Beispielzeile auf dem Eingabeblatt und Handrechnung auf dem DIN-Blatt — nachrechenbar.
- **Diagramme:** (1) Tagesgang drei Tagtypen als gruppierte Säulen; (2) Woche mit gestapelten Lasten, Ladelinie,
  Defizit und Füllstand auf Sekundärachse, Speichergröße für die Füllstandskurve wählbar.

## 5. Abgleich mit dem Konzept (TWW-Zapfprofile V1.2 als Grundlage des Zapfprofilgenerators)

| Element | Vorlage (Excel V4) | Konzept (V1.2) | Folgerung für Mockup |
|---|---|---|---|
| Nutzungsarten-Katalog | 11 Typen (4 VDI 6002, 7 generisch INEKON) + Benutzerdefiniert; Richtwert je Person oder Einheit mit Bandbreite im Bemerkungstext | S0: Start ~15, Ausbau ~25 Typen; Leitquelle A100 Tab. NA.4 (~27 Nutzungsarten), Provenienz + Bandbreite je Wert, Bedarfsniveau niedrig/mittel/hoch; EPOS-Bestand: die 11 V4-Typen als `Tab_Brauchwassertyp_STAMM` (168 h) + 13 Monatswertsätze | Katalogauswahl mit Spalten Herkunft/Status/Bezugsgröße; die 11 Typen als heutiger Bestand, Erweiterung andeuten |
| Tagesprofil | 3 Tagtypen × 24 h, deterministisch, Anteile Σ 100 % | S2 deterministischer Formvektor je Tagtyp + S3 stochastische Zapfereignisse (Jordan/Vajen, Seed, Ensemble) | Tagesgang-Editor 3 × 24 als deterministische Schicht; Stochastik als eigener Schalter (Experte) |
| Wochenfaktoren | 7 Werte Mo–So je Typ, Σ 100 %, Summenkontrolle | Teil von S2 (Wochenfaktoren) | übernehmen, inkl. Summenkontrolle im Klartext |
| Saison/Jahresgang | ein Skalar (linear, „ersetzt kein Jahresprofil") | S2 Jahresgang × Ferien-/Belegungskalender (Bundesland) × Kaltwasser-Saisonalität | 12 Monatsfaktoren/Kalender statt Saisonfaktor; maßgebende Periode als Ergebnis |
| Zeitraster | 336 h (2 × Woche, Woche 2 maßgebend) | 8760 h Bilanz, optional 1-min-Feinauflösung | Wochendiagramm als Ausschnitt aus 8760 h mit Wochenwahl |
| Zirkulation | Dauerlast P_zirk **innerhalb** der Bilanz (addiert zum Bedarf), Schätzhilfe mit 3 Methoden | S5 eigener additiver Kanal, getrennt vom Zapfprofil ausgewiesen (außerhalb der Bilanzgrenze des Zapfprofils); DELTA-Q-Defaults, Experte Netzlänge × U × ΔT; W-551-Automatik | Zirkulation als eigene Serie/Kanal; Schätzhilfe (Länge × W/m, Anteil, manuell) als Eingabehilfe übernehmbar |
| Speicherauslegung | Kern der Mappe (4 Verfahren, Empfehlung, Füllstand) | S4 bewusst getrennt vom Bilanzprofil (Summenlinie DIN EN 12831-3, DIN-4708-N, Perzentile); im Generator nicht enthalten — Anschlussverbraucher | nur als Ausblick/Übergabe „Auslegung"; Verfahrensvergleich als Muster für den späteren Auslegungsdialog |
| Eingabetiefe | flach, Umschalter auto/manuell, Schätzhilfen | Schnellauslegung / Standard / Experte (2.4) | Umschalter-Muster in die drei Stufen einsortieren; Schätzhilfen ab Standard |
| Ergebnisse/Kennzahlen | Volumen je Verfahren, D_max, maßgebender Tag, C_sp, min. Füllstand, N_L | kWh/a, l/d @60 °C, Zirkulationsanteil, P50–P99, Volllaststunden, Provenienzlog | Kennzahlenkarte Profil; Auslegungskennzahlen getrennt |
| Warnungen | 8 Klartextzeilen + Summen-/Tabellenkontrollen | 2.5: Plausibilität, W 551, Zirkulation „nein" bei Großanlage, DIN-4708-Gültigkeit, stochastisch > 1,5 × Summenlinie, BWP-Hinweis N_L, Messwert > 40 % | gemeinsame, nicht blockierende Warnliste mit Klartextsätzen |
| Provenienz | Quellzeile + Status je Profilblock | Provenienzpflicht je Wert, Status Default/überschrieben/kalibriert | Herkunftsmarke je Profil und je Kennwert |
| Referenzfall | Beispielzeile (30 WE) + DIN-4708-2-Handrechnung | 3.6: Excel-Referenz 0 Abweichung, Ecodesign-Summen, VDI-4655-Anker | Beispiel mit neuen, runden, fiktiven Zahlen; Rechenweg sichtbar |

## 6. Bestand außerhalb des Repositoriums (für „Was es schon gibt")

- **Streamlit-Wärmespeicher-Tool** (Python, lokal, fünf Reiter: Gebäude & Profil, Analyse, TWW-Speicher, Pufferspeicher,
  Export): TWW mit drei Verfahren (DIN 4708 vereinfacht, profilbasiert auf dem Jahreslastgang, Faustwert mit
  3×-Warnung); DIN 4708-2-Tabelle und GLF-Faustwert gibt es nur in der Excel-Mappe (dort vier Zeilen, eine nachrichtlich).
  Pufferspeicher mit vier Kriterien (Abtauung, Taktung/Mindestlaufzeit, EVU-Sperrzeit, Lastgang-Simulation mit
  Bisektion; Praxisgrenze 100 m³) und Zweipunkt-Betriebssimulation (Ladezyklen), gegen eine INEKON-Lastgangauswertung
  mit 0 Abweichungen über 8.760 h validiert. Lastgänge synthetisch (VDI 4655 über demandlib, BDEW-GHD, DWD-TRY-2010)
  oder gemessen (CSV/XLSX-Import); Excel-Bericht mit 7 Blättern; 146 Tests grün.
  Fremdcode: lpagg (MIT; `din4708.py` unverändert übernommen, TRY-Wetter 15 Regionen), demandlib 0.2.2 (MIT).
  Grenzen: keine Schaltjahre (VDI-4655-Profile), Energiebilanz ohne Schichtung und Speicherverluste, N ohne
  Zapfstellenwertigkeiten, synthetische Spitzen ≈ 2,4 × Messspitze (Messspitze ≈ P90 der synthetischen Dauerlinie),
  kein Abgleich mit Hersteller-N_L-Daten.
- **Excel-Mappe TWW-Auslegung V4** (Version 2.1.2, Juli 2026) — diese Analyse.
- **Word-Dokumentation Wärmespeicher-Tool** (Version 1.1, 29.07.2026): A Beschreibung, B Bedienung je Reiter,
  C Methodik (C.5 TWW-Verfahren 1–4, C.5.5 DIN 4708-2, C.5.6 Empfehlung/Hygiene, C.5.7 Verfahrenswahl mit
  Eignungstabelle inkl. „stochastisch (DHWcalc) extern für < ~15 WE", C.6 Puffer, C.7 Validierung, C.8 Grenzen),
  D Excel-Tool (Zweck, 6 Blätter, Qualitätssicherung, Zuordnung Verfahren ↔ Werkzeug).
- **Grundlagenberichte und Konzept:** Grundlagen 1, 2, 3, 5 und das Konzept (Inhalt V1.2, Datei `_1`) stehen im Repo
  unter `Dokumentation/aktuell/`; **Grundlagen 4 (Repo-Analyse) und das Konzept V1.0 stehen im Repo unter
  `Dokumentation/ueberholt/`** — Grundlagen 4 ist also nicht nur auf Z:. Die Fassungen auf Z: weichen byteweise ab,
  aber **nur in den Zeilenenden** (Z: LF, Arbeitsbaum CRLF); nach Entfernen der CR sind alle sieben Dateien inhaltsgleich.
- In derselben Ablage: lizenzierte Norm-PDFs (DIN EN 12831-3 samt Entwürfen A1/A100, DIN V 18599-10, VDI 4655,
  VDI 6002 Bl. 1/2), ein Planungshandbuch Solarthermie, eine Hersteller-Planungsunterlage (nicht verwenden),
  zwei Lastgangauswertungen (Excel) und die Altbibliothek des Rechenkerns — nichts davon ins Mockup.

## 7. Lizenz- und Datenregeln für das Mockup

- **VDI 4655:** nicht vervielfältigen, auch nicht innerbetrieblich; keine Formvektoren, Faktortabellen, Typtagfolgen.
  Nur Methodik nennen („Typtagsystematik, Daten per Import des lizenzierten Anwenders").
- **VDI 6002 Bl. 1/2:** Stundenwerte (u. a. Bl. 2 Tab. 3), abgelesene Diagrammwerte, Wochenanteile und Tagesbedarfs-
  kennwerte (Studentenwohnheim/Seniorenheim/Krankenhaus, Wohnen groß nach ZfS-Messungen) **nicht wörtlich übernehmen** —
  im Mockup nur fiktive, runde Beispielzahlen und Quellenverweise („nach VDI 6002 Bl. 2").
- **DIN 4708:** die Formeln (N, u1, u2, K(u), W_z, 3,5 Personen, 5820 Wh, z = 1/6 h) dürfen stehen; die Nachschlagetabellen
  p(r) und w_v nicht in Tabellenform abdrucken (Konzept 3.4) — im Mockup als Felder mit Platzhalter „Katalogwert".
- **Keine Hersteller- und Produktdaten**: keinen Speicher-Herstellernamen, keine Baureihe, keine N_L-Werte eines
  Produkts; „Speicher mit N_L ≥ N" nur als Kriterium. Speichergrößenliste als neutrale Nenninhalte zulässig.
- **demandlib, lpagg** (MIT) nur als Quellenhinweis/Attribution; kein Code, keine Daten ins Mockup.
- Generische INEKON-Werte der Mappe (z. B. Büro 8 l/(P·d), Wochenfaktoren Büro/Schule) dürfen als Beispiel dienen,
  besser sind trotzdem neue runde Zahlen, damit das Mockup nicht als Datenquelle gelesen wird.

## 8. Empfehlung für das Mockup

Übernehmen:
1. **Dialogaufbau nach der Blattfolge** als Schritte/Reiter: Eingaben (Nutzung, Menge, Bedarf, Temperaturen) →
   Zapfprofil (Tagesgang 3 × 24, Wochenfaktoren, Jahresgang) → optional DIN 4708-2 (nur Wohnen, Stufe Experte) → Ergebnis.
2. **Katalogauswahl** mit Herkunft/Status je Typ (VDI-Verweis / generisch / Anwender) und „Benutzerdefiniert" als
   Kopie eines Katalogtyps; Summenkontrollen als grüne/rote Klartextzeilen unter Tagesgang und Wochenfaktoren.
3. **auto/manuell-Muster** für Tagesbedarf, Ladeleistung und Zirkulation: Vorschlag, manueller Wert, angesetzter Wert,
   darunter der **aufklappbare Rechenweg** als Satz mit Zahlen (Schätzhilfen P_lade und P_zirk wie in V4).
4. **Ergebniskarte Verfahrensvergleich** (DIN 4708 · profilbasiert · GLF · klassisch grau „nachrichtlich") mit
   markiertem maßgebenden Verfahren, gerundeter Empfehlung und N_L-Kriterium — als **Ausblick** auf den
   Anschlussverbraucher „Speicherauslegung", klar getrennt von den Profilkennzahlen.
5. **Warnungsliste** im Klartext: Summen ≠ 100 %, Ladeleistung < Tagesbedarf, maßgebender Tag am Wochenende,
   Legionellen > 400 l (DVGW W 551), Speichertemperatur < 60 °C, klassischer Faustwert > 3 × Verfahren,
   Verfahren außerhalb seiner Gültigkeit (DIN 4708 für Nichtwohnen, GLF ohne Wannen) — die letzte ist neu gegenüber V4.
6. **Diagramm maßgebende Woche**: gestapelt Zapfung + Zirkulation, Ladelinie, kumuliertes Defizit und Füllstand
   (Sekundärachse), Speichergröße wählbar; dazu Tagesgang-Diagramm der drei Tagtypen.
7. **„D_max = 0"-Fall** und **maßgebender Zeitpunkt** als erklärende Sätze statt nackter Zahlen.
8. **Referenzfall** als Beispielwerte mit **neuen, runden, fiktiven Zahlen** (z. B. „Wohnhaus 1, 20 WE × 2 P,
   40 l/(P·d), Ladeleistung 15 kW") und nachrechenbarem Rechenweg.
9. Zirkulation im Mockup als **eigener Kanal** neben dem Zapfprofil (Konzept S5), nicht in das Profil eingerechnet.

Bewusst nicht übernehmen: Zahlen der VDI-6002-Profile und -Kennwerte, die Referenztabelle mit fünf Tagtypen,
die DIN-4708-2-Nachschlagewerte p(r)/w_v, VDI-4655-Daten, Herstellerbezüge; ebenso die V4-Schwächen aus 2.7
(fest eingetragene „angesetzte" Werte, gemischtes Mengengerüst, Saisonfaktor als einziger Jahresgang, 336-h-Raster).
