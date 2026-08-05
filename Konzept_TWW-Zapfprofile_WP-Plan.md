# TWW-Zapfprofile in WP-Plan — Methodik und Umsetzungsplan

**Projekt:** WP-Plan (C#/.NET, WinForms) — Modul Trinkwarmwasser-Zapfprofile
**Stand:** 29.07.2026 · INEKON
**Grundlage:** Rechercheberichte „Grundlagen 1 — Normen/Regelwerke" und „Grundlagen 2 — Modelle, Generatoren, Daten" (beide beigefügt); Erfahrungen aus dem Python-Wärmespeicher-Tool (Vorprojekt)
**Hinweis:** Das Repository `github.com/inekon/WP-Plan` ist privat und konnte nicht eingesehen werden. Die Architekturvorschläge sind daher generisch für C#/.NET formuliert und beim Einbau an die vorhandene Projektstruktur anzupassen.
**Konventionen (aus Vorprojekt übernommen):** Leistung in kW je Zeitschritt, c_w = 1,163 Wh/(l·K), V[l] = Q[kWh]·1000/(1,163·ΔT). Intern wird durchgängig in **kWh Nutzenergie** gerechnet; Liter sind nur Anzeige-/Eingabegröße (vermeidet die 45-°C-/60-°C-/Normliter-Verwechslung zwischen DIN V 18599, VDI 6002 und SIA).

---

## Teil 1 — Empfohlene Methodik

### 1.1 Drei Lehren, die das Design bestimmen

**Lehre 1 — Bilanzprofil und Auslegungsfall sind zwei verschiedene Produkte.** Der 2,4×-Befund des Vorprojekts (611 MWh/a: 298 kW synthetisch vs. 125 kW gemessen; Messspitze ≈ P90 der synthetischen Dauerlinie) ist kein Ausreißer, sondern liegt exakt im publizierten Korridor: DIN 4708 vs. DIN EN 12831-3 ergibt beim 11-WE-Referenzgebäude 60 kW vs. 30 kW vs. 20 kW (Faktor 2–3, IKZ); IAPMO misst einen Abfall der Spitze je WE um Faktor 5,8 zwischen 1 und 27 WE; Braas et al. finden Faktor 3 und mehr bei der Erzeugerleistung. Mechanistische Erklärung (Hypothese aus dem Tools-Bericht, plausibel, aber nicht Literaturbefund): deterministische Typtagprofile (VDI 4655) geben allen Einheiten dasselbe Profil → implizite Gleichzeitigkeit 100 %; real fällt die Spitze auf μ + z·σ/√N. Bei 6–20 WE ist √N ≈ 2,4–4,5 — das trifft den Faktor 2,4 quantitativ. **Konsequenz:** Die Spitze des Bilanzprofils darf im Tool strukturell *nirgends* als Auslegungsgröße auftauchen. Auslegung ist eine eigene Schicht mit eigener Physik (Summenlinie, Perzentile, Normkennzahlen).

**Lehre 2 — Gleichzeitigkeit muss als Ergebnis anfallen, nicht als Faktor eingebaut werden.** Ein 20-WE-Gebäude darf niemals als 20 × Einzelprofil entstehen, sondern nur als Superposition von 20 unabhängigen Ziehungen. Zusätzlich ist die Gleichzeitigkeit **topologieabhängig** (Braas et al. 2020: EFH ohne Speicher 42 kW Spitze, mit korrekt geladenem Speicher 3 kW; GLF bei 20 Gebäuden 40–50 % mit Speicher, 15–20 % ohne). Ein GLF, der nur f(N) ist, wäre falsch — er muss f(N, Anlagentopologie) sein.

**Lehre 3 — Ohne Zirkulationsmodell ist das Profil für WP-JAZ-Aussagen wertlos.** In kleinen MFH sind die Zirkulations-/Verteilverluste (10–15 kWh/(m²·a), DELTA-Q nach DIN V 4701-10) größer als der TWW-Nutzenergiebedarf (8,5–13 kWh/(m²·a)). Die Zirkulation ist zudem eine Konstantlast auf 55–60 °C-Niveau (DVGW W 551) — für Bivalenzpunkt und JAZ der dominierende Term im Bestand.

### 1.2 Das Schichtenmodell

Jede Nutzungszone wird durch **eine Menge + einen Formvektor + einen Stochastik-Parametersatz + einen Auslegungsdatensatz** beschrieben. Die Schichten sind strikt getrennt implementiert und einzeln testbar.

| Schicht | Inhalt | Primärquellen | Anmerkung |
|---|---|---|---|
| **S0 Nutzungsarten-Katalog** | Typenliste mit allen Kennwerten, versioniert, als Datenpaket | eigene Kompilation aus DIN V 18599-10 Tab. 4/7, VDI 6002 Bl. 2, SIA 2024, DOE/ASHRAE, DIN 18032-1 (Sporthalle), Logalux-Systematik | Realistisch: **Start mit ~15 Typen**, Ausbau auf ~25 (die 22 Kategorien der Tab. 7 plus Wohnen EFH/MFH, Hallenbad, Wohnheim). Jeder Kennwert trägt Herkunft + Bandbreite |
| **S1 Mengengerüst** | Jahres-/Tagesnutzenergie je Zone aus Bezugsgröße | WG: Q_w,b = max[16,5 − 0,05·A_NGF,WE; 8,5] kWh/(m²·a) (18599 Tab. 4), plausibilisiert an BBSR 17/2017 (MFH 11,1; EFH 9,2 kWh/(m²·a)); alternativ 40 l/(P·d) @60 °C (DHWcalc/SIA-Konsens). NWG: Tab.-7-Werte (kWh je Person/Bett/Sitzplatz/Beschäftigtem·d), VDI 6002 Bl. 2 (l/(vp·d) @60 °C mit Schwachlast/Mittel/Winterspitze), SIA 2024 als Zweitquelle | Bezugsgrößen je Typ: Personen, WE + Belegung, Betten, Duschplätze/Übungseinheiten, Sitzplätze, Beschäftigte, m² NGF (nur Fallback). Interner Träger: kWh Nutzenergie; Temperaturbezug je Quelle sauber dokumentiert (18599 implizit ΔT 35 K, VDI 6002 60 °C) |
| **S2 Zeitstruktur** | Normierter Formvektor: Tagesgang je Tagtyp (Werktag/Sa/So-Feiertag, ggf. Mo/Fr-Varianten) × Wochenfaktoren × Jahresgang (Wochenauflösung, VDI-6002-Empfehlung) × Ferien-/Belegungskalender × Kaltwasser-Saisonalität (Sinus ±10 % als Default, Bandbreite bis ±25 % nach Mack98) | Formen selbst erstellt bzw. abgeleitet: 18599-Nutzungszeitfenster (Tab. 5) + n_SP ∈ {1,2} als Konstruktionsregel (Rechteck vs. Doppelspitze), DOE/ASHRAE-Schedules (frei) als Vorlage für NWG-Tagesgänge, qualitative Merkmale aus VDI 6002 (Morgen-/Abendspitze gleich hoch, Nachmittagsminimum 15./16. h, Sa/So-Verschiebung, Nachtsockel nicht null) | Die konkreten Stützstellen sind **eigene, dokumentierte Modellannahmen** — kein 1:1-Abdruck geschützter VDI-Tabellen (Rechtsstrategie, s. 3.4). Feiertage: eigener Kalender je Bundesland |
| **S3 Stochastik/Zapfereignisse** | Zapfereignis-Generator nach Jordan/Vajen-Parametrik (frei dokumentiert, IEA SHC Task 26): 4 Kategorien WG (1/6/14/8 l/min; 1/1/10/5 min; DIN-4708-kompatible Maximalzapfung ~5,8 kWh), 2 Kategorien NWG (à la OpenDHW); p = p(Jahr)·p(Wochentag)·p(Tageszeit aus S2)·p(Ferien); N unabhängige Einheiten superponiert; Entnahmeraten plausibilisiert an VDI 6003 | **Wann nötig:** 1-min-Auslegungsnachweis (Summenlinie mit realistischer Diversität), Speicher-Be-/Entladesimulation, Frischwasserstationen, Perzentilanalyse. **Wann verzichtbar:** reine 8760-h-Jahresbilanz großer Objekte (dort konvergiert die Superposition ohnehin gegen den Formvektor — deterministischer Pfad ist schneller und reproduzierbar). Seed immer explizit → reproduzierbare Ergebnisse für Prüfer |
| **S4 Auslegungsfall** | Bewusst NICHT aus dem Bilanzprofil: (a) Summenlinienverfahren nach DIN-EN-12831-3-Methodik in Minutenschritten (Speichervolumen + Ladeleistung als Wertepaar); Bedarfstag = Auslegungstagesmenge (z. B. VDI-6002-Winterspitzenwert bzw. Maximalbelegung) × Auslegungs-Tagesgang; (b) für Wohngebäude zusätzlich DIN-4708-Kennzahl N als Vergleichs-/Nachweiswert (inkl. echter Wertigkeiten w_V und Belegungszahlen p — behebt den offenen Punkt des Vorprojekts); (c) Perzentil-Auslegung P95–P99 der stochastisch superponierten Last (IAPMO-Praxis), Plausibilisierung gegen μ + z·σ/√N; (d) Rohrnetz-Spitzendurchfluss DIN 1988-300 (a·(ΣV̇_R)^b − c) nur nachrichtlich | Erwartungshaltung als eingebauter Plausibilitätscheck: stochastische Auslegung < DIN 4708, ≈ oder leicht < EN 12831-3 (IKZ-Anker 60/30/20 kW). DIN-4708-Grenzen respektieren: für Hotels, Heime, Wohnheime ist N ausdrücklich ungültig → dort nur Summenlinie. Hinweis „NL-Verfahren für WP-Vorlauftemperaturen kaum anwendbar" (BWP-Leitfaden) im Ergebnis ausweisen |
| **S5 Zirkulation/Verteilverluste** | Eigener, additiver Lastkanal, getrennt vom Zapfprofil ausgewiesen: Default aus DELTA-Q-Kennwerten (6,6–14,6 kWh/(m²·a) f(A_N, Lage innerhalb/außerhalb Hülle, mit/ohne Zirkulation)); zeitlich als 16–24-h-Konstantlast (W 551: max. 8 h Abschaltung); Temperaturniveau 60/55 °C; Experte: Netzlänge × U-Wert × ΔT; SIA-Faustwert „+50 % des Nutzwarmwasserbedarfs" als Plausibilitätsanker | Großanlagen-Kriterium W 551 (Speicher > 400 l oder Leitungsinhalt > 3 l) steuert automatisch 60-°C-Pflicht und Zirkulationsannahme; Hinweislogik im UI |
| **S6 Kalibrierung/Plausibilisierung** | Falls Messdaten vorhanden: (a) Jahres-/Monatsverbrauch → Skalierung von S1 (ein Faktor, ausgewiesen); (b) gemessene Lastgänge → Vergleich Dauerlinien (P50/P90/P99, Spitze), Formabgleich Tagesgang, optional Anpassung Formvektor; (c) automatischer Bericht „synthetisch vs. gemessen" mit dem P90/√N-Check aus dem Vorprojekt | Kein Zwang zur Kalibrierung; jede Kalibrierung wird in der Provenienz protokolliert („Wert überschrieben durch Messdaten vom …") |

### 1.3 Methodik-Alternativen

**A — Normbasiert-deterministisch (Typtage/Formvektoren).** S1 + S2, keine Stochastik. Jahresprofil = Menge × Formvektor; Auslegung über Normkennzahlen (4708) und tabellierte Spitzenfaktoren (ASHRAE Max-Hour, n_SP). Stärken: minimaler Aufwand, deterministisch, prüferfreundlich, sehr geringe Eingaben. Schwächen: Diversität nur per Faktor (genau der Mechanismus hinter dem 2,4×-Fehler); keine Minutenspitzen; Speichersimulation gegen geglättete Profile unterschätzt Zyklik; die VDI-6002-Warnung („Einzeltagesspitzen fast doppelt so hoch wie Profilmaxima") bleibt unbehandelt.

**B — Stochastischer Zapfereignis-Generator (DHWcalc/OpenDHW-Ansatz, nach C# portiert).** Zapfereignisse je Einheit unabhängig ziehen, superponieren; Bilanz und Auslegung aus einer Modellphysik; Gleichzeitigkeit fällt als Ergebnis an und ist gegen DIN 1988-300/4708 validierbar. Stärken: höchste Realitätsnähe, einzige Methode, die Speicher-/FriWa-Auslegung und Sommer-Schwachlast (versetzte Urlaubsperioden!) konsistent liefert. Schwächen: höherer Implementierungsaufwand; Nicht-Reproduzierbarkeit ohne Seed-Disziplin; für NWG-Typen existieren keine validierten Ereignisparameter (nur OpenDHW-Setzungen, gegen DHWcalc verifiziert, **nicht messvalidiert** — Unsicherheit); erklärungsbedürftig gegenüber Prüfern.

**C — Vorgerechnete Profilbibliothek als Datenpaket.** Offline (z. B. mit OpenDHW/LPG, beide MIT) Ensembles erzeugen, als komprimierte Ressource ausliefern, zur Laufzeit nur samplen/skalieren. Stärken: kein Rechenkern-Risiko, schnelle Laufzeit, LPG-Qualität (Anwesenheits-/Stromkorrelation) nutzbar. Schwächen: kombinatorische Explosion (Nutzungsart × Größe × Belegung × Kalender × Klimaregion); **Skalierung fremder Profile auf andere N reproduziert exakt den Gleichzeitigkeitsfehler**, den wir vermeiden wollen; Ferien-/Feiertagskalender des Projekts nicht abbildbar; Datenvolumen; Herkunft der Profile schwerer nachweisbar; Python-Toolchain bleibt (als Build-Schritt) im Spiel.

**D — Hybrid (A als Rückgrat + B als Auslegungs- und Feinauflösungsmotor).** Deterministischer Pfad für die 8760-h-Bilanz; stochastischer Pfad für Auslegung, Speicher-/FriWa-Simulation und optional als „realistische" Zeitreihe. Beide Pfade teilen S0–S2 und werden gegeneinander konsistenzgeprüft (Energiesumme identisch, Formvektor = Erwartungswert des Generators).

### 1.4 Bewertungsmatrix

Skala 1 (schlecht) – 5 (sehr gut):

| Kriterium | A deterministisch | B stochastisch | C Bibliothek | D Hybrid |
|---|---|---|---|---|
| Realitätsnähe Bilanz (8760 h) | 4 | 4 | 4 | **4** |
| Realitätsnähe Spitzen/Auslegung | 2 | **5** | 2–3 | **5** |
| Nutzungsartenbreite | **5** (Kennwerte reichen) | 3 (Ereignisparameter fehlen tlw.) | 2 | **4–5** |
| Eingabeaufwand Nutzer | **5** | 4 | 5 | **5** |
| Lizenz-/Rechtsrisiko | 4 (Normwerte kapseln) | **5** (Jordan/Vajen frei dokumentiert; Eigenimplementierung) | 3 (Herkunft der Ensembles, LPG-DB) | **4–5** |
| C#-Implementierungsaufwand | **5** (gering) | 3 | 4 | 3 |
| Wartbarkeit/Erweiterbarkeit | 4 | 4 | 2 | **4** |
| Nachvollziehbarkeit ggü. Prüfern | **5** | 3 (nur mit Seed + Normabgleich) | 3 | **4** (deterministischer Pfad als Nachweisebene) |

### 1.5 Entscheidung

**Empfohlen wird D: Hybrid mit deterministischem Rückgrat (A) und eigenem, in C# implementiertem Zapfereignis-Generator (B) als Auslegungs- und Feinauflösungsmotor.** Begründung:

1. Die Aufgabenstellung verlangt *beides* — Bilanz und Auslegung. A allein kann die Auslegung nicht ohne den Fehler des Vorprojekts; B allein ist für 20+ Nutzungsarten nicht parametrisierbar und prüferisch schwer vermittelbar; C erbt die Schwächen beider ohne deren Stärken.
2. Der Mehraufwand des Hybrids ist klein, weil beide Pfade S0–S2 teilen: Der Generator konsumiert den Formvektor aus S2 als p(Tageszeit) — genau das Konstruktionsprinzip von DHWcalc, und identisch mit dem OpenDHW-Muster (`prob_nonresidential.json`), das sich 1:1 als JSON-Katalogstruktur übernehmen lässt.
3. Rechtlich ist B der sauberste Kern: Die Jordan/Vajen-Parametrik (IEA SHC Task 26) ist frei dokumentiert; OpenDHW (MIT) dient als Referenzimplementierung und Testorakel, ohne dass Python in das Produkt gelangt.
4. Die Trennung Bilanz/Auslegung (Lehre 1) ist im Hybrid strukturell erzwungen: Der Nutzer bekommt nie „die Spitze des Jahresprofils" als Auslegungswert, sondern immer das Tripel {Summenlinien-Ergebnis, Perzentil-Ergebnis, Normkennzahl-Vergleich}.

**Benannte Nachteile der Empfehlung (bewusst in Kauf genommen):** (a) Zwei Rechenpfade müssen konsistent gehalten werden (Gegenmaßnahme: automatischer Konsistenztest Energiesumme/Erwartungswert in der CI). (b) Die NWG-Ereignisparameter (Kategorien, σ, Blockspitzen Sporthalle/Küche) sind teilweise eigene Setzungen ohne Messvalidierung — sie müssen als solche gekennzeichnet und in S6 nachschärfbar sein. (c) Stochastische Ergebnisse erfordern Seed-Disziplin und Ensemble-Kommunikation (P95/P99 statt „die Spitze") — das UI muss das tragen. (d) DIN EN 12831-3/A100 (Anhang-B-Profile, nationale Zapfprofile) ist noch nicht beschafft; bis dahin sind die Auslegungs-Tagesgänge Eigenannahmen (offener Prüfpunkt).

---

## Teil 2 — Eingabe- und Defaultkonzept

### 2.1 Prinzip

Jede Zahl im System hat eine **Provenienz** (Quelle, Ausgabejahr, Bandbreite) und einen **Status** (Default / vom Nutzer überschrieben / aus Messdaten kalibriert). Das UI zeigt bei jedem Default die Herkunft als Kurztext („nach DIN V 18599-10:2018-09, Tab. 7" — Verfahrensnennung, kein Tabellenabdruck). Überschreiben außerhalb der Plausibilitätsgrenzen erzeugt eine Warnung, blockiert aber nicht (Ingenieurwerkzeug).

### 2.2 Nutzereingaben je Nutzungszone

Sensitivität: H = hoch, M = mittel, G = gering (bezogen auf Jahresenergie bzw. Auslegung).

| Feld | Typ / Einheit | Pflicht | Vorgabewert | Herkunft Default | Plausibilitätsbereich | Sensitivität |
|---|---|---|---|---|---|---|
| Nutzungsart | Auswahl (Katalog S0) | **Pflicht** | — | — | — | H |
| Bezugsgröße (Personen / WE / Betten / Duschplätze / Sitzplätze / Beschäftigte) | int/decimal, typabhängig | **Pflicht** | — | — | typabhängig, z. B. Betten 5–2000 | H |
| Belegung/Personen je WE (nur Wohnen) | decimal [P/WE] | optional | 2,0–3,5 nach Wohnungsgröße (DIN-4708-p-Logik) | DIN 4708-2 Belegungszahlen (via Herstellerunterlagen) | 1,0–6,0 | H (Auslegung), M (Bilanz) |
| NGF je WE (nur Wohnen, für 18599-Formel) | decimal [m²] | optional | 70 m² | Annahme; Formelträger 18599 Tab. 4 | 25–250 | M |
| Bedarfsniveau | Auswahl niedrig/mittel/hoch | optional | mittel | Bandbreiten der Quelle (z. B. VDI 6002 Bl. 2: Pflegeheim 33/36/40 l/(vp·d) @60 °C; Hotel einfach/mittel/Luxus 1,9/3,5/5,5 kWh/(Bett·d)) | Katalogbandbreite | H |
| Spezifischer Bedarf (Override) | decimal [kWh/(Einheit·d)] | Experte | Katalogwert | s. o. | ±60 % um Katalogwert, darüber Warnung | H |
| Jahres-Messverbrauch (Kalibrierung) | decimal [kWh/a oder m³/a] | optional | leer | Nutzer | > 0; Warnung bei Abweichung > ±40 % vom Katalog | H |
| Zapf-/Speichertemperatur | decimal [°C] | optional | 60 °C (Großanlage W 551) / 50 °C Kleinanlage wählbar | DVGW W 551 | 45–70 | M |
| Kaltwassertemperatur (Jahresmittel + Amplitude) | decimal [°C] / [K] | Experte | 10 °C / ±3 K sinusförmig | 18599-Konvention; Saisonalität nach Jordan/Vajen (±10 % Last), Mack98 als obere Bandbreite | 5–18 / 0–6 | G–M |
| Anlagentopologie | Auswahl Speicher / Frischwasserstation / Durchfluss / Wohnungsstationen | optional | Speicher | Erforderlich für topologieabhängige Gleichzeitigkeit (Braas et al. 2020) | — | H (nur Auslegung) |
| Zirkulation vorhanden | ja/nein | optional | ja, wenn Großanlage (W-551-Kriterium automatisch) | DVGW W 551 | — | H (JAZ) |
| Zirkulationsverlust | decimal [kWh/(m²·a)] oder [W] | Experte | f(A_N, Lage): 6,6–14,6 kWh/(m²·a) | DELTA-Q-Kennwerte (DIN V 4701-10) | 2–25 | H (JAZ) |
| Betriebs-/Ferienkalender | Auswahl + Bundesland | optional | typabhängig (Schule: Ferien; Büro: Wochenende/Feiertage; Hotel: Auslastungsgang) | Katalog S0; Feiertage eigener Kalender | — | M |
| Auslastung/Belegungsgrad Jahresgang | Kurve oder 3 Stützwerte | Experte | typabhängig (z. B. Wohnheim Schwachlast:Spitze 1:2,3 sinngemäß VDI 6002 Bl. 2) | Katalog | 0–120 % | M |
| Zufalls-Seed / Ensemblegröße | int / int | Experte | fest (dokumentiert) / 10 Läufe | Reproduzierbarkeit | — | G (Bilanz), M (Perzentile) |

### 2.3 Minimale Eingabemenge (Ziel-Ideal)

Für ein belastbares Ergebnis genügen **3 Felder je Zone**: Nutzungsart, Bezugsgröße, Bedarfsniveau (Default „mittel" vorbelegt → faktisch 2). Für ein *gutes* Ergebnis kommen dazu: Anlagentopologie und Zirkulation ja/nein (beide vorbelegt) sowie — wenn vorhanden — der Jahres-Messverbrauch. Damit liegt die Schnellauslegung bei **2–3 aktiven Eingaben**, der Standardfall bei **4–6**.

### 2.4 Gestaffelte Tiefe

| Stufe | Sichtbar/aktiv | Zweck |
|---|---|---|
| **Schnellauslegung** | Nutzungsart, Bezugsgröße, Bedarfsniveau; alles andere Default. Ausgabe: Jahresenergie, Tagesmenge, Vorschau-Tagesgang, grobe Auslegungswerte mit Kennzeichnung „Schnellauslegung" | Akquise, Erstgespräch, Variantenvergleich |
| **Standard** | zusätzlich: Belegung, Topologie, Zirkulation, Kalender/Bundesland, Messverbrauch-Kalibrierung | Regelfall Projektbearbeitung |
| **Experte** | zusätzlich: Bedarfs-Override, Temperaturen, Zirkulationsdetail, Auslastungsgang, Formvektor-Editor (Tagesgang-Stützstellen), Stochastik-Parameter (Kategorien, σ, Seed, Ensemble), Auslegungs-Perzentil | Sonderfälle, Kalibrierung, Gutachten |

### 2.5 WinForms-UI-Vorschlag

- **Hauptmaske „TWW-Profil"** als dreispaltiges Layout: links Zonenliste (Gebäude = Liste von Nutzungszonen; Buttons Hinzufügen/Duplizieren/Löschen → **Mischnutzung ist der Normalfall**, z. B. MFH + Gewerbe-EG + Arztpraxis); Mitte Eingabepanel der gewählten Zone (Stufenumschalter Schnell/Standard/Experte als ToggleButtons oben; Defaultfelder grau mit Herkunfts-Tooltip und Info-Icon, überschriebene Felder farblich markiert mit „Zurücksetzen"-Link); rechts **Live-Vorschau**.
- **Live-Vorschau** (aktualisiert bei jeder Eingabeänderung, deterministischer Pfad, < 100 ms): Tab 1 Tagesgang (Werktag/Sa/So übereinander, kW), Tab 2 Jahresdauerlinie mit Markern P90/P95/P99 und — falls berechnet — den Auslegungswerten als horizontale Linien, Tab 3 Wochen-/Jahresgang, Tab 4 Kennzahlen (kWh/a, l/d @60 °C, Zirkulationsanteil %, Spitzen). Charting: vorhandene WP-Plan-Chartkomponente weiterverwenden; sonst genügt ein leichtgewichtiger eigener Renderer.
- **Summenansicht Gebäude:** Superposition aller Zonen inkl. Zirkulation, getrennte Flächen-/Linienserien „Zapfenergie" vs. „Zirkulation" (JAZ-Argumentation sichtbar machen).
- **Warn-/Hinweislogik (nicht blockierend):** Eingabe außerhalb Plausibilitätsbereich; W-551-Großanlage erkannt → 60-°C-Hinweis + Zirkulationspflicht; Zirkulation „nein" bei Großanlage → Warnung; DIN-4708-N angefordert für Hotel/Heim/Wohnheim → Hinweis „außerhalb Gültigkeitsbereich DIN 4708, Auslegung erfolgt nach Summenlinienverfahren"; stochastische Spitze > 1,5 × Summenlinienleistung → Konsistenzhinweis; NL-Kennzahl bei WP-System → BWP-Hinweis; kalibrierter Messwert weicht > 40 % vom Katalog ab → Rückfrage.
- **Ergebnisdialog Auslegung:** immer das Tripel Summenlinie (V/P-Wertepaar-Kurve, Nutzer wählt Punkt), Perzentil-Auslegung (P95/P99 mit Ensemble-Streuband), Normvergleich (N nach DIN 4708 für WG, DIN-1988-300-V̇_S nachrichtlich) — nebeneinander, mit kurzer Bewertung („empfohlener Auslegungspunkt", Begründung).

---

## Teil 3 — Umsetzungsplan C#/WinForms

### 3.1 Architektur

Strikte Trennung: **Rechenkern als reine .NET-Klassenbibliothek** (netstandard2.0 oder net8.0, keine WinForms-Referenz, deterministisch testbar), UI nur als Konsument.

```
WPPlan.Profiles                    (Klassenbibliothek, kein UI)
├── Catalog        UsageType, CatalogEntry, CatalogRepository (JSON-Ressource), Provenance
├── Demand         Mengengerüst S1: DemandCalculator, TemperatureModel (c_w, ΔT, Kaltwassergang)
├── Shape          S2: DayShape, WeekFactors, YearShape, CalendarService (Feiertage/Ferien je Bundesland)
├── Stochastic     S3: DrawEventGenerator, DrawCategory, Superposition, RandomSource (seedbar)
├── Sizing         S4: SummationLineMethod (12831-3-Methodik), Din4708Index (N, w_V, p),
│                  PercentileSizing, Din1988FlowRate, SizingComparator
├── Circulation    S5: CirculationModel (Kennwert- und Detailmodus)
├── Calibration    S6: MeasurementImport, ScalingCalibrator, ValidationReport
├── Results        TimeSeries (8760 h / 1 min), KeyFigures, SizingResult, ProvenanceLog
└── IO             Export CSV/Excel, Projektserialisierung
WPPlan.Profiles.Tests              (Unit-/Regressionstests, Referenzdaten)
WPPlan.UI.Profiles                 (WinForms-Masken, Preview-Controls)
```

### 3.2 Datenhaltung Nutzungsarten-Katalog

- **Format:** JSON, als eingebettete Ressource kompiliert **und** parallel als Datei in `%ProgramData%\INEKON\WP-Plan\Catalog\` ladbar — Dateiversion überschreibt eingebettete Version, wenn `catalogVersion` höher. Damit kann INEKON Typen ergänzen/korrigieren **ohne Neukompilierung**; jede Berechnung protokolliert die verwendete Katalogversion in der Provenienz.
- **Struktur je Eintrag:** Bezugsgröße(n), spezifischer Bedarf {niedrig, mittel, hoch} mit Temperaturbezug und Quelle, Tagesgang-Stützstellen je Tagtyp, Wochenfaktoren, Jahresgang/Auslastung, Kalenderregel, n_SP-analoge Spitzenklasse, Stochastik-Parametersatz (Kategorien mit V̇, Dauer, Häufigkeit, σ), Auslegungs-Tagesprofil, Zirkulations-Defaultklasse. Schema-Validierung beim Laden; Versionsfeld + Changelog im Katalog selbst.
- Bewusst **kein** Abdruck der Normtabellen in auslieferbarer Doku: Der Katalog enthält Werte mit Quellen-*Verweis*; das Handbuch beschreibt nur Verfahren und Quellenangaben (s. 3.4).

### 3.3 Öffentliche API-Skizze

```csharp
public sealed record ZoneInput(
    string UsageTypeId,                    // Katalogschlüssel, z. B. "residential.mfh", "hotel.mid"
    double ReferenceValue,                 // Personen, WE, Betten, ... (Einheit aus Katalog)
    DemandLevel Level = DemandLevel.Medium,
    double? OccupantsPerUnit = null,
    double? AnnualMeasuredDemandKWh = null,   // Kalibrierung S6
    SystemTopology Topology = SystemTopology.Storage,
    CirculationSpec? Circulation = null,      // null = Katalog-/W551-Default
    CalendarSpec? Calendar = null,            // Bundesland, Ferien, Betriebstage
    IReadOnlyDictionary<string, double>? ExpertOverrides = null);

public sealed record ProfileRequest(
    IReadOnlyList<ZoneInput> Zones,
    int Year,
    Resolution Resolution,                 // Hourly8760 | Minute1
    ProfileMode Mode,                      // Deterministic | Stochastic
    int? RandomSeed = null, int EnsembleSize = 1,
    TemperatureSpec Temperatures = default);   // T_zapf, T_kalt(Jahresgang)

public sealed record ProfileResult(
    TimeSeries TapEnergy,                  // kW je Zeitschritt, Zapf-Nutzenergie
    TimeSeries CirculationLoss,            // kW, getrennter Kanal
    KeyFigures Figures,                    // kWh/a, l/d@60°C, P50/P90/P95/P99, Volllaststunden
    IReadOnlyList<ZoneResult> PerZone,
    ProvenanceLog Provenance);             // jeder Kennwert: Quelle, Version, Status

public interface IProfileEngine {
    ProfileResult Generate(ProfileRequest request);
    SizingResult  Size(SizingRequest request);       // unabhängig vom Bilanzprofil!
}

public sealed record SizingResult(
    IReadOnlyList<StorageDesignPoint> SummationLine,  // (V_Speicher, P_Lade)-Wertepaare, 1-min-Bilanz
    PercentileSizing Stochastic,                      // P95/P99 + Streuband aus Ensemble
    Din4708Result? N,                                 // nur WG; inkl. Gültigkeitsflag
    Din1988Result PipeFlow,                           // nachrichtlich
    IReadOnlyList<DesignNote> Notes);                 // Warnungen/Hinweise (BWP, W551, Gültigkeit)
```

### 3.4 Beschaffen / Portieren / Selbst entwickeln — mit Lizenzstrategie

| Baustein | Weg | Rechtslage |
|---|---|---|
| Zapfereignis-Generator | **Selbst entwickeln in C#** nach der frei publizierten Jordan/Vajen-Parametrik (IEA SHC Task 26); OpenDHW (MIT) als Referenz-/Testorakel, ggf. einzelne Algorithmen sinngemäß portiert mit MIT-Attribution | grün; DHWcalc-EXE (Lizenz unklar) wird **nicht** eingebettet |
| Referenz-Testprofile | `DHWcalc_Files/` aus dem OpenDHW-Repo (MIT) übernehmen | grün (Ursprungsstatus der Dateien mit Restunsicherheit — im Testprojekt, nicht im Auslieferpaket) |
| NWG-Tagesgänge | Eigene Stützstellen, konstruiert aus 18599-Nutzungszeitfenstern + n_SP + DOE/ASHRAE-Schedules (US-Regierungswerk, frei) + qualitativen VDI-6002-Merkmalen; deutsche Niveaus aus S1 | grün, als Modellannahme dokumentiert |
| Normkennwerte (18599 Tab. 4/7, 4708-w_V/p, 1988-300-a/b/c) | Im Code als gekapselte Parameter implementieren (branchenüblich); Normen beschaffen: **DIN EN 12831-3 + A100-Entwurf (prioritär), DIN 4708-2/-3, DIN V 18599-10**; DIN-Media-Softwarelizenz anfragen | gelb: implementieren ja, **Tabellen nirgends abdrucken** (nicht in UI-Tabellenform, Handbuch, Marketing); 4708-Kennwerte über frei publizierte Herstellerunterlagen zitierfähig |
| VDI 6002 Bl. 1/2, VDI 4655 | Kennwerte (l/(vp·d)-Bandbreiten) mit Sekundärquellen-Zitat (Buderus) verwenden; **Tagesgang-Tabellen/Diagramme nicht reproduzieren** — eigene Formvektoren, die die beschriebenen Merkmale qualitativ abbilden; VDI 4655 nicht als Datenbasis übernehmen (strengste Lizenzlage; zudem methodisch Quelle des 2,4×-Problems) | gelb/rot ohne VDI-Lizenz → durch Eigenkonstruktion umgangen |
| Ecodesign-Profile 3XS–4XL (VO (EU) 814/2013) | Vollständig einbetten, auch sichtbar im UI (Geräte-Rückrechnung η_wh/COP_DHW, EFH-Plausibilisierung M ≈ 5,845 / L ≈ 11,655 kWh/d) | **grün, einzige frei abdruckbare Zapffolge** |
| Kein Python zur Laufzeit | Python (OpenDHW/LPG) nur intern als Entwicklungs-/Validierungswerkzeug; optionaler Offline-Einsatz von pylpg (MIT) zur Erzeugung interner Vergleichsensembles | grün; keine Auslieferungsabhängigkeit |
| Ausschluss | StROBe (keine Lizenz), pysimdeum-Einbettung (EUPL-Copyleft), DHWcalc-EXE-Einbettung | rot |

### 3.5 Phasen und Aufwand

Schätzung in Personentagen (PT), erfahrener .NET-Entwickler + fachliche Begleitung; Unsicherheit ±30 %.

| Phase | Inhalt | PT | Ergebnis |
|---|---|---|---|
| **P0 Grundlagen** | Normbeschaffung (12831-3+A100, 4708-2/3), Katalog-Schema, Erstbefüllung 15 Typen mit Provenienz, VDI-6002-Diagramm-Digitalisierung intern | 8–10 | Katalog v1.0 (JSON), Quellendossier |
| **P1 MVP Bilanz (deterministisch)** | S1 + S2 + S5: 8760-h-Profil Wohnen EFH/MFH + 6 wichtigste NWG-Typen, Zirkulationskanal, Kalender, API + Provenienz, Excel-Referenz-Abgleich | 12–15 | **MVP:** nutzbares Jahresprofil für WP-JAZ/Bivalenz in WP-Plan |
| **P2 Auslegung deterministisch** | Summenlinienverfahren (1-min), DIN-4708-N mit echten Wertigkeiten, DIN 1988-300 nachrichtlich, Ergebnis-Tripel, IKZ-Referenzfall | 10–12 | Speicher-/Erzeugerauslegung ohne Stochastik |
| **P3 Stochastik** | Zapfereignis-Generator (4+2 Kategorien), Superposition N Einheiten, Urlaubs-Dekorrelation, Perzentil-Auslegung, Ensemble, Regressionstests gegen DHWcalc-Files, Konsistenztest gegen deterministischen Pfad | 15–20 | Realistische Spitzen, topologieabhängige Gleichzeitigkeit |
| **P4 UI** | Masken (Zonenliste, 3 Stufen, Live-Vorschau, Warnlogik, Auslegungsdialog), Mischnutzung, Export | 12–15 | Vollintegration WinForms |
| **P5 Kalibrierung + Validierung** | Messdatenimport, Skalierung, Vergleichsbericht (P90/√N-Check), Validierung gegen Carleton-Daten + INEKON-Messprojekte, Katalogausbau auf ~25 Typen | 10–12 | Version 1.0 des Moduls |
| **Summe** | | **≈ 67–84 PT** | |

Reihenfolge nach Nutzen/Aufwand: P1 liefert sofort den JAZ-relevanten Kern; P2 vor P3, weil die Summenlinie schon ohne Stochastik den 2,4×-Fehler behebt; P3 hebt dann Auslegungsqualität und Diversität.

### 3.6 Verifikations- und Validierungsplan

Ziel analog Vorprojekt: **0 Abweichung gegen eine unabhängige Excel-Referenz über 8760 h** für den deterministischen Pfad; statistische Toleranzen für den stochastischen.

| Test | Referenz/Sollwert | Toleranz |
|---|---|---|
| Einheiten/Umrechnung | V = Q·1000/(1,163·ΔT); Stichproben: 0,4 kWh/(P·d) ≈ 9,8 l @45 °C; L-Profil 11,655 kWh ≈ 200 l @60 °C | exakt (Rundung) |
| 18599-Wohnformel | 70 m² → 13,0; 100 m² → 11,5; ≥160 m² → 8,5 kWh/(m²·a) | exakt |
| Jahresenergie deterministisch | Excel-Referenzblatt je Nutzungsart, 8760 h | 0 (bitgleiche Summen) |
| Formvektor | Σ Tagesgang = 1; Σ Jahr = Jahresmenge; Kalendertage korrekt (Schaltjahr, Feiertage je Bundesland) | exakt |
| Stochastik-Energieerhaltung | E[Jahresenergie Generator] = deterministische Jahresenergie | ±1 % (Ensemble ≥ 10) |
| Generator-Verteilungen | Zapfvolumen-/Ereignisstatistik gegen DHWcalc-Referenzdateien (OpenDHW `DHWcalc_Files/`, 1/15/60 min) | KS-Test bzw. Momentenvergleich ±5 % |
| Gleichzeitigkeit | Spitze/WE fällt ~1/√N; GLF-Stützstellen Braas 2020 (20 Geb.: 40–50 % mit Speicher, 15–20 % ohne) | qualitativ + Korridor |
| Auslegung Referenz-MFH 11 WE | IKZ-Anker: DIN 4708 ≈ 60 kW; Summenlinie ≈ 30 kW (hoher Bedarf) bzw. ≈ 20 kW (40 l/(P·d)) | ±15 % |
| Ecodesign-Profile | Q_ref-Summen exakt (M 5,845 / L 11,655 kWh/d …) | exakt |
| Messvalidierung | Carleton/Québec 5-min-Profile (73 EFH, frei): Tagesgangform, Ereignisdauern; INEKON-611-MWh-Datensatz: Messspitze im P85–P95-Band der synthetischen Dauerlinie, √N-Skalierungstest der Überschätzung | Bandkriterien, dokumentierter Bericht |
| Zirkulation | DELTA-Q-Kennwertreproduktion; SIA-Anker „Verluste ≈ +50 % Nutzenergie" als Plausibilitätsfenster kleiner MFH | ±20 % Fenster |

Alle Tests als Unit-/Integrationstests in `WPPlan.Profiles.Tests`, CI-Pflicht; Katalogänderungen triggern den kompletten Referenzlauf.

### 3.7 Risiken und Gegenmaßnahmen

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| DIN EN 12831-3/A100-Zapfprofile weichen von Eigenannahmen ab | Nacharbeit an Auslegungs-Tagesgängen | Norm früh beschaffen (P0); Tagesgänge nur im Katalog (Daten), nicht im Code → Austausch ohne Release |
| Urheberrecht Normtabellen (DIN/VDI) | Abmahnung/Vertriebsrisiko | Strategie 3.4 strikt: implementieren, nicht abdrucken; DIN-Media-Softwarelizenz anfragen; VDI-Tabellen durch Eigenkonstruktion ersetzt; juristische Prüfung vor Release |
| NWG-Ereignisparameter unvalidiert | falsche NWG-Spitzen | Kennzeichnung „Modellannahme" in Provenienz; Auslegung primär über Summenlinie mit Katalog-Auslegungstag; S6-Kalibrierpfad; Messkooperation anstreben |
| Stochastik-Ergebnisse für Prüfer schwer vermittelbar | Akzeptanzproblem | deterministischer Pfad als Nachweisebene, fester dokumentierter Seed, Normvergleich immer im Ergebnisblatt |
| Zwei Pfade divergieren bei Wartung | Inkonsistenz | automatischer Konsistenztest (Energiesumme, Erwartungswert) in CI |
| Performance 1-min × N Einheiten in WinForms | UI-Blockade | Rechenkern async/Task-basiert; Vorschau nur deterministisch; Stochastik als expliziter Rechenlauf mit Fortschrittsanzeige |
| Katalogpflege erodiert (Werte ohne Quelle geändert) | Verlust der Nachvollziehbarkeit | Schema erzwingt Quelle+Version je Wert; Katalog-Changelog; Referenztestlauf bei jeder Änderung |
| Unterschätzung realer Einzeltagesspitzen (VDI-6002-Warnung „fast doppelt") | knappe Auslegung | Perzentil-Auslegung P95–P99 statt Mittelprofil-Maximum; Sicherheitsband im Auslegungsdialog ausgewiesen |

---

## Entscheidungen, die INEKON noch treffen muss

1. **Normbeschaffung/-budget:** DIN EN 12831-3 + A100-Entwurf, DIN 4708-2/-3, DIN V 18599-10 kaufen; DIN-Media-Lizenzanfrage für Softwarehersteller ja/nein; VDI-Lizenz (6002/4655) bewusst *nicht* — bestätigen.
2. **Typenumfang v1.0:** Welche ~15 Nutzungsarten sind Startumfang (Vorschlag: EFH, MFH, Hotel 3 Niveaus, Pflegeheim, Wohnheim, Krankenhaus, Büro, Schule ± Duschen, Sporthalle/Duschanlage, Hallenbad, Fitness/Sauna, Gewerbe/Werkstatt)?
3. **Auslegungs-Default:** P95 oder P99 als Standard-Perzentil der stochastischen Auslegung; Position zur Erzeuger-Auslegung nach Jahresdauerlinie/Bivalenz (Vorprojekt-Praxis) als dokumentierter Standardweg.
4. **Kaltwassertemperatur:** fester Jahresgang (10 °C ± 3 K) oder Kopplung an Klimaregion/TRY-Daten von WP-Plan.
5. **Messdatenstrategie:** Freigabe des 611-MWh-Datensatzes (und weiterer Projekte) für den √N-Validierungstest; ggf. Kooperation mit Heizkostenabrechnern für deutsche Messdaten.
6. **UI-Charting:** vorhandene WP-Plan-Komponente oder Neuentwicklung der Vorschau-Controls.
7. **Katalog-Redaktionsprozess:** Wer bei INEKON pflegt den JSON-Katalog, wer gibt Änderungen frei (Vier-Augen-Prinzip wegen Provenienzpflicht)?
8. **Juristische Prüfung** der Lizenzstrategie (Abschnitt 3.4) vor Erstauslieferung — insbesondere DHWcalc-Referenzdateien im Testumfang und Normwerte-Kapselung.

**Offene fachliche Prüfpunkte (gekennzeichnet):** VDI-6002-Bl.-2-Inkonsistenz Krankenhaus 33 vs. 35 l/(vp·d) (Bandbreite hinterlegen); Digitalisierung der VDI-6002-Diagrammprofile (nur interne Nutzung); DIN-EN-12831-3-Anhang-B-Profile unverifiziert bis Normkauf; die √N-Erklärung des 2,4×-Befunds ist Hypothese und wird erst durch den Validierungstest (3.6) belegt; die 45-°C-Bezugstemperatur der 18599-Tab.-7-Werte ist aus der Bagatellklausel abgeleitet, nicht explizit normiert.
