# TWW-Zapfprofile in WP-Plan — Methodik und Umsetzungsplan

**Projekt:** WP-Plan (C#/.NET Framework 4.8, WinForms) — Modul Trinkwarmwasser-Zapfprofile
**Stand:** 29.07.2026 · **Version 1.1** · INEKON
**Grundlage:** Rechercheberichte „Grundlagen 1–5" (Normen/Regelwerke; Modelle/Generatoren/Daten; DIN EN 12831-3 + A1/A100; WP-Plan-Repo-Analyse; VDI 4655) sowie das Reverse-Engineering-Dossier `BHKWPLAN.DLL` (alle beigefügt); Erfahrungen aus dem Python-Wärmespeicher-Tool (Vorprojekt)

## Änderungsstand V1.2 (gegenüber V1.1 vom selben Tag)

1. **VDI 4655:2021 liegt lizenziert vor und ist ausgewertet (Grundlagen 5).** Die Richtlinie wird nun **berücksichtigt** — aber rollengetrennt: **(a) Formvektor- und Jahresgangquelle für den deterministischen Bilanzpfad Wohnen** (S2), **(b) Wetter-/Typtag-Kopplung** (stärkster Mehrwert: fertige Zuordnungsregel Jahreszeit×Bewölkung×Wochentag, Feiertag = Sonntag, 15 TRY-Zonen, PLZ-Tabelle), **(c) Validierungsreferenz** (Jahresanker 500 kWh/(Pers·a) EFH, 1000 kWh/(WE·a) MFH; Beispielrechnung Abschnitt 8 als Regressionstest). **NICHT für Auslegungsspitzen** — dort bleibt DIN EN 12831-3/A100 allein zuständig.
2. **Zwei Korrekturen an der bisherigen 2,4×-Begründung** (V1.0/V1.1 waren hier ungenau): VDI 4655:2021 nutzt **reale Einzeltage, keine Mittelwertprofile** (S. 4 Anm. 1 — bewusst gegen Glättung), und der Normtext behauptet **nicht** „100 % Gleichzeitigkeit". Die belastbare, normgestützte Begründung, warum die Richtlinie keine Spitzen liefert: MFH-Profile nur **15-min-Mittelwerte**, **lineare N_WE-Skalierung ohne Gleichzeitigkeitsfunktion** (WE-Zahl der Messobjekte in der Norm nicht angegeben → implizite Gleichzeitigkeit unbekannt), **nur 10 Tagesformen für 365 Tage** (Tagesmaximum wiederholt sich vielfach → Dauerlinie im oberen Bereich zu flach; erklärt „Messspitze ≈ P90"), **keine Streuungs-/Überschreitungsangaben**, ausdrückliche Abgrenzung von genormten Zapfprofilen (DIN EN 15450, S. 4). Der 2,4×-Befund bleibt ein **eigener empirischer Beitrag** (nicht Normaussage) — als solcher gekennzeichnet.
3. **Bilanzgrenzen-Warnung (wichtig gegen Doppelzählung):** VDI 4655 Q_TWE enthält **Verteil-/Zirkulationsverluste, aber keine Speicherverluste** (S. 9). Wird ein VDI-4655-Jahreswert unreflektiert als „Nutzenergie" verwendet und Zirkulation (S5) separat aufgeschlagen, entsteht Doppelzählung. Der Katalog führt daher je Kennwert die **Bilanzgrenze** als Provenienz-Attribut; S5 wird bei VDI-4655-Quelle automatisch entschärft.
4. **Lizenzlage VDI 4655 ist restriktiver als erwartet:** Seitenvermerk „Vervielfältigung — **auch für innerbetriebliche Zwecke** — nicht gestattet"; die Nutzdaten (Formvektoren, 45 Faktortabellen, Typtag-Reihenfolgen, PV-Profile) liegen auf **CD-ROM** (im PDF nicht enthalten). **Empfohlenes Produktdesign: WP-Plan implementiert nur die Methodik (Gleichungen/Typtagsystematik — nicht schutzfähig) und bietet eine Import-Schnittstelle für die VDI-Datensätze, die der lizenzierte Anwender selbst einspielt** — keine Auslieferung der VDI-Daten im Produkt. Vor Codierung mit VDI/Beuth klären.
5. **BHKWPLAN.DLL reverse-engineert (eigenes Dossier):** Der native Rechenkern ist **Borland-C, `__stdcall`, 29 Exporte, 8760/168/365/12 fix**, Ursprung „BHKW-Plan" (Steinborn). Für das Zapfprofil-Modul zentral: WP-Plan verteilt Warmwasser bisher **konstant über 365 Tage** (kein Zapfprofil) und disaggregiert Tageswerte generisch über `StdWerte` (typtag-normierte 24h-Profile, „nach VDI 2067"). **Das ist die exakte Andockstelle:** Das neue TWW-Profil ersetzt den WW-Anteil an dieser Stelle bzw. wird per `vectoren_addieren` (trivial, sofort in C# nachbaubar) auf die Wärme-Ganglinie addiert — **ohne Eingriff in den x86-Kern**. Die Additions-/Verteil-/Normier-Bausteine sind trivial portierbar; das bestätigt die V1.1-Architekturentscheidung (Feinrechnung in C#, Übergabe als `float[8760]`).

## Änderungsstand V1.1 (gegenüber V1.0 vom selben Tag)

1. **DIN EN 12831-3 (2017-09) + Entwürfe A1 (2021-04) und A100 (2021-09) liegen vor und sind ausgewertet** — der wichtigste Beschaffungspunkt aus V1.0 ist erledigt. Kernbefunde: Der A100-Entwurf ersetzt Anhang B national und enthält **18 gemessene deutsche Referenz-Bedarfsprofile in Minutenauflösung** (Hotel 20/300 Zi., Mensa, Hotelküche, 4× Krankenhaus, JVA, Schwimmbad, 2× Seniorenheim, Studentenheim, MFH 24 WE, DIN-4708-Profile N = 2/4/10/20). **Aber:** Die Minutenwerte stehen nur als Grafik im Normtext; die Zahlendateien gibt es laut NA.5.2.5 als CD-ROM/ZIP — **separat zu beschaffen (neuer Beschaffungspunkt höchster Priorität)**. DIN 4708 wird durch A100 **nicht ersetzt, sondern überführt** (normative Verweisung; N-Profile als Summenlinien-Input). Die V1.0-Annahme „Auslegungs-Tagesgänge sind durchweg Eigenannahmen" ist damit **überwiegend widerlegt** — Eigenkonstruktion bleibt nur für Büro, Schule, Handel, Werkstatt u. ä. nötig und ist dort per NA.5.2.3 (manuelle Profilerstellung, Beispiel Tab. NA.3) ausdrücklich normkonform.
2. **Der 60/30/20-kW-IKZ-Anker ist kein Normwert.** A100 bestätigt die Richtung wörtlich (DIN-4708-Werte „führen in der Regel zu einer großzügigen leistungsmäßigen Auslegung"), enthält aber keinen 11-WE-Referenzfall. Der V&V-Plan ersetzt den IKZ-Anker durch einen **selbst gerechneten Dreifachvergleich** (DIN-4708-Profil vs. A100-MFH-Messprofil vs. Tab.-NA.4-Richtwerte — alle drei Datensätze liefert das Normwerk). Der Bedarfsunterschied ist norm-belegbar: DIN 4708 N = 10 → 53,8 l/(P·d) vs. A100-MFH-Messung → 19,7 l/(P·d) vs. NA.4-MFH → 30 l/(P·d).
3. **Rechenregeln des Summenlinienverfahrens präzisiert:** 1-min-Schrittweite (1 440 Zyklen/Tag) bestätigt; der Normalgorithmus ist ein *Nachweis* für ein gegebenes Paar (V_sto, Φ_eff) — die im Konzept vorgesehene **Wertepaar-Kurve ist eine legitime eigene Erweiterung** durch Parametervariation und wird als solche gekennzeichnet. Zu implementieren nach A100/A1: rekursive Speicherbilanz (Gl. NA.3/NA.4) statt e-Funktion, korrigierte Zeitkonstante τ = m·c_w/(U_HE·A_HE)·**16,67**, Φ_N = **min**(Erzeugerleistung, Wärmeübertragerleistung), Φ_eff darf negativ werden, U_HE-Defaults 700/970 W/(m²·K), A_HE-Schätzformeln mit Plausibilitäts-Guard (WP-Formel wird < ~105 l negativ), Temperatur-Renormierung von Tabellenwerten (B.4: 60/13,5 °C; B.5/NA.4: 45/10 °C) ist nach A1 **verpflichtend**. NA.5.6 liefert ein normiertes **Vereinfachungsverfahren für Wohngebäude ≤ 6 WE** (N-2-Profil, h_sensor/h_sto = 0,6, 60 °C, Speicher voll bei Start) — ideale Basis der Stufe „Schnellauslegung"; auszuweisen ist dabei Σ t_power,on [h/d] (für WP-Sperrzeiten/Taktung hochrelevant). Kaltwasser 10 °C fest für die Auslegung (Tab. NA.5) — der Konzept-Default ist bestätigt; Jahresgang bleibt Bilanz-Feature.
4. **Teil 3 (Architektur) ist vollständig auf die reale WP-Plan-Codebasis umgeschrieben** (Repo-Analyse über die Dateibrücke): Integration als Model/Ctrl/View-Tripel in `WindowsFormsApplication1` statt eigener Bibliothek, Katalog in `Kenndaten.accdb` **statt JSON** (revidierte Entscheidung — folgt der durchgängigen Praxis des Bestands inkl. Update-/Backup-Mechanismus), Andocken über eine Weiche in `SimulationWaermebedarf`, Feinauflösung in reinem C# mit Aggregation auf `float[8760]` am Übergabepunkt (der native Rechenkern `bhkwplan.dll` ist auf 8760/168/365/12 fixiert). Neue Risiken aus dem Bestand aufgenommen (DSN-Pflicht, zwei DB-Schichten, `UpdateDB.ini`-Lücke beim Brauchwasser, belegter Spaltennamen-Bug `M1…M12` vs. `Monat_n`).
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
| **S2 Zeitstruktur** | Normierter Formvektor: Tagesgang je Tagtyp × Wochenfaktoren × Jahresgang × Ferien-/Belegungskalender × Kaltwasser-Saisonalität (Sinus ±10 % Default). **Wohnen:** wahlweise **VDI-4655-Typtagsystematik** (10 Typtage EFH/MFH, Wetterzuordnung Jahreszeit×Bewölkung×Wochentag, Feiertag=Sonntag, 15 TRY-Zonen, PLZ→Zone) — bei lizenziert eingespielten VDI-Datensätzen die bevorzugte Wohnquelle für den Bilanzpfad. **NWG:** eigene Formen aus 18599-Nutzungszeitfenstern (Tab. 5) + n_SP ∈ {1,2} + DOE/ASHRAE-Schedules + qualitative VDI-6002-Merkmale | Die NWG-Stützstellen sind **eigene, dokumentierte Modellannahmen** — kein 1:1-Abdruck geschützter Tabellen (Rechtsstrategie, s. 3.4). VDI-4655-Formvektoren nur über Anwender-Import (Lizenz, s. Änderungsstand V1.2 Punkt 4); MFH-Vektoren liegen nur in 15-min-Auflösung vor → nicht spitzentauglich, entsprechend geflaggt. Feiertage: eigener Kalender je Bundesland. **Bilanzgrenze** je Kennwert mitführen (VDI 4655: inkl. Zirkulation, exkl. Speicher) |
| **S3 Stochastik/Zapfereignisse** | Zapfereignis-Generator nach Jordan/Vajen-Parametrik (frei dokumentiert, IEA SHC Task 26): 4 Kategorien WG (1/6/14/8 l/min; 1/1/10/5 min; DIN-4708-kompatible Maximalzapfung ~5,8 kWh), 2 Kategorien NWG (à la OpenDHW); p = p(Jahr)·p(Wochentag)·p(Tageszeit aus S2)·p(Ferien); N unabhängige Einheiten superponiert; Entnahmeraten plausibilisiert an VDI 6003 | **Wann nötig:** 1-min-Auslegungsnachweis (Summenlinie mit realistischer Diversität), Speicher-Be-/Entladesimulation, Frischwasserstationen, Perzentilanalyse. **Wann verzichtbar:** reine 8760-h-Jahresbilanz großer Objekte (dort konvergiert die Superposition ohnehin gegen den Formvektor — deterministischer Pfad ist schneller und reproduzierbar). Seed immer explizit → reproduzierbare Ergebnisse für Prüfer |
| **S4 Auslegungsfall** | Bewusst NICHT aus dem Bilanzprofil: (a) Summenlinienverfahren nach DIN EN 12831-3 in 1-min-Schritten (1 440 Zyklen/Tag), mit A100-Rechenregeln (rekursive Speicherbilanz NA.3/NA.4, τ-Korrektur 16,67, Φ_N = min(Erzeuger, WT), Φ_eff auch negativ); Bedarfstag wahlweise **A100-Referenzprofil** (18 gemessene Minutenprofile, sobald Datendateien beschafft), **Tab.-B.2-Stundenprofil** (per Gl. (3) auf Minuten expandiert, geflaggt „Spitzen unterschätzt"), **DIN-4708-Profil N = 2/4/10/20** (A100 NA.5.2.6.15–18) oder **manuell konstruiertes Profil** nach NA.5.2.3 (Konstruktor-UI nach Muster Tab. NA.3; Dusche = 8 l/min × 5 min @40 °C); die Wertepaar-Kurve (V_sto, Φ_N) entsteht durch Parametervariation des Norm-Nachweises — als eigene Erweiterung gekennzeichnet; (b) für Wohngebäude zusätzlich DIN-4708-Kennzahl N als Vergleichs-/Nachweiswert (inkl. echter Wertigkeiten w_V und Belegungszahlen p — behebt den offenen Punkt des Vorprojekts); für ≤ 6 WE das A100-Vereinfachungsverfahren NA.5.6 als Schnellauslegungspfad; (c) Perzentil-Auslegung P95–P99 der stochastisch superponierten Last (IAPMO-Praxis), Plausibilisierung gegen μ + z·σ/√N; (d) Rohrnetz-Spitzendurchfluss DIN 1988-300 (a·(ΣV̇_R)^b − c) nur nachrichtlich (identisch mit V̇_D nach B.3.6, A100 NA.5.4.9); A100-Spitzendurchflüsse der Referenzprofile sind laut Norm NICHT für Trinkwasserauslegung zu verwenden | Erwartungshaltung als eingebauter Plausibilitätscheck: stochastische Auslegung < DIN 4708, ≈ oder leicht < EN 12831-3 (belegt über den Bedarfsvergleich 53,8 / 30 / 19,7 l/(P·d), s. Änderungsstand Punkt 2). DIN-4708-Grenzen respektieren: für Hotels, Heime, Wohnheime ist N ausdrücklich ungültig → dort Summenlinie mit A100-Profil. Hinweis „NL-Verfahren für WP-Vorlauftemperaturen kaum anwendbar" (BWP-Leitfaden) im Ergebnis ausweisen; Σ t_power,on [h/d] als Ergebnisgröße (NA.5.6) |
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

**Benannte Nachteile der Empfehlung (bewusst in Kauf genommen):** (a) Zwei Rechenpfade müssen konsistent gehalten werden (Gegenmaßnahme: automatischer Konsistenztest Energiesumme/Erwartungswert in der CI). (b) Die NWG-Ereignisparameter (Kategorien, σ, Blockspitzen Sporthalle/Küche) sind teilweise eigene Setzungen ohne Messvalidierung — sie müssen als solche gekennzeichnet und in S6 nachschärfbar sein; **neu in V1.1:** die 18 A100-Referenzprofile werden, sobald die Datendateien vorliegen, zum Kalibrier-/Validierungsanker der NWG-Parameter. (c) Stochastische Ergebnisse erfordern Seed-Disziplin und Ensemble-Kommunikation (P95/P99 statt „die Spitze") — das UI muss das tragen. (d) ~~DIN EN 12831-3/A100 noch nicht beschafft~~ **erledigt in V1.1** — Normtexte liegen ausgewertet vor; offen bleiben die **Zahlendateien der 18 A100-Referenzprofile** (CD-ROM/ZIP nach NA.5.2.5) und die Prüfung, ob A100/A1 inzwischen als Weißdruck erschienen sind (beides Entwürfe von 2021; im Ergebnisausdruck als „Entwurfsstand, Anwendung besonders zu vereinbaren" kennzeichnen).

---

## Teil 2 — Eingabe- und Defaultkonzept

### 2.1 Prinzip

Jede Zahl im System hat eine **Provenienz** (Quelle, Ausgabejahr, Bandbreite) und einen **Status** (Default / vom Nutzer überschrieben / aus Messdaten kalibriert). Das UI zeigt bei jedem Default die Herkunft als Kurztext („nach DIN V 18599-10:2018-09, Tab. 7" — Verfahrensnennung, kein Tabellenabdruck). Überschreiben außerhalb der Plausibilitätsgrenzen erzeugt eine Warnung, blockiert aber nicht (Ingenieurwerkzeug).

**Neu in V1.1 — primäre Kennwertquelle für die Defaults:** Tab. NA.4 des A100-Entwurfs (Richtwerte des Nutzenergiebedarfs, normiert auf 45/10 °C, inhaltlich DIN-V-18599-10-basiert) wird die Leitquelle des Katalogs — sie deckt inkl. der neuen Einträge (Sauna, Labor, Fitnessraum, EFH/EFH gehoben/DHH/MFH/MFH gehoben, Bäckerei, Friseur, Fleischerei, Wäscherei, Brauerei, Molkerei) rund 27 Nutzungsarten ab und harmoniert mit den Hotel-Dreistufen (einfach/mittel/luxus 1,9/3,5/5,5 kWh/(Bett·d)), die V1.0 aus VDI 6002 ansetzte. Zwei normative Zusatzregeln fließen in die Warnlogik ein: bei EFH ist mindestens einmal die größte Einzelentnahme anzusetzen (z. B. Wannenfüllung 160 l @45 °C), und Tabellenwerte mit abweichender Referenztemperatur (Tab. B.4: 60/13,5 °C) werden beim Import nach A1 zwingend umgerechnet: V_neu = V_Tab · Δθ_Tab / Δθ_neu.

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

## Teil 3 — Umsetzungsplan C#/WinForms (V1.1: an die reale WP-Plan-Codebasis angepasst)

### 3.0 Befund aus der Repo-Analyse (Kurzfassung; Details in „Grundlagen 4")

- **Technik:** .NET Framework 4.8, WinForms/MDI, x86-Default; Charting über MS Chart (`System.Windows.Forms.DataVisualization`), gekapselt in `Allgemein\GrafikTools\ChartManagerNeu.cs` (15-min-fähig via `IsQuarterHourly`); MathNet.Numerics 5.0 vorhanden; ScottPlot 5 referenziert, aber ungenutzt.
- **Daten:** Keine Projektdatei — Kataloge UND Projekte liegen in `Kenndaten.accdb`. Konventionen: `Tab_*` (Stamm/Katalog), `Z_*` (Projekt-Zuordnung mit `ID_Projekt`, `ID_<Katalog>`, `Bezeichner`, `Summe`), `Abfrage_*` (Access-Queries). Migration über `UpdateDB.ini` + `Form_Update`.
- **Zwei DB-Schichten:** legacy ODBC (`Program.DBConnection`/`RecordSet`, DSN „TEST" ist harte Startvoraussetzung) und neu OleDb/ACE über `Allgemein\DataRepository.cs` (parametrisiert). Neues Modul ausschließlich über `DataRepository`.
- **Rechenkern:** numerische Kernroutinen liegen in einer nativen `bhkwplan.dll` hinter dem Out-of-Proc-COM-Server `CSExeCOMServer.exe` — **alle Signaturen fest auf 8760/168/365/12 Elemente** (Borland-C, `__stdcall`, 29 Exporte; Reverse-Engineering-Dossier beigefügt). Wärme durchgängig `float[8760]`, Strom `float[35040]`. **Warmwasser heute konstant über 365 Tage** verteilt (kein Zapfprofil), Tages→Stunde-Disaggregation über `StdWerte` (typtag-normierte 24h-Profile, „nach VDI 2067") — **exakte Andockstelle für das neue Modul** (WW-Anteil ersetzen bzw. `vectoren_addieren`, beides trivial in C#). Die Übergabe-Bausteine (`vectoren_addieren`, `Watt_To_kW`, `normieren`, `heapsort`, `monats_summe`) sind laut Dossier sofort portierbar → die Zapfprofil-Zeitreihe kann vor `heapsort`/BHKW-Einsatzrechnung in die Pipeline gesetzt werden, ohne den x86-Kern zu ändern.
- **Brauchwasser heute:** Katalog `Tab_Brauchwasser` (12 Monatswerte) × `Tab_Brauchwassertyp` (168-Wochen-Stundenprofil) → `I_strom_wochetojahr` → 8760; projektspezifische Jahressumme wird linear skaliert. **Altlasten:** Brauchwasser-Tabellen fehlen in `UpdateDB.ini` (Datenverlust bei Update!), belegter Spaltennamen-Bug in `BrauchwasserCtrl.Insert/Update` (`M1…M12` statt `Monat_n`), kein Wizard-Eintrag, keine Lokalisierungs-resx, teils `DELETE`-SQL mit ungültiger Syntax.

### 3.1 Architektur

Kein eigenes Bibliotheksprojekt-Ökosystem, sondern **konventionskonforme Integration** in `WindowsFormsApplication1` — mit einer wichtigen Ausnahme: Der **Rechenkern des Moduls entsteht als eigenes Klassenbibliotheks-Projekt in der Solution** (`WPPlan.Zapfprofil`, .NET Framework 4.8, keine WinForms-/DB-Referenz), damit er erstmals im Projekt unit-testbar ist (`WPPlan.Zapfprofil.Tests`, MSTest/NUnit). Die App konsumiert ihn über Model/Ctrl/View-Tripel nach Bestandsmuster:

```
WPPlan.Zapfprofil                  (neue Klassenbibliothek, .NET FW 4.8, rein rechnend)
├── Demand         S1: Mengengerüst, Temperaturmodell (c_w = 1,163 Wh/(l·K), Renormierung n. A1)
├── Shape          S2: Tagesgang/Wochenfaktoren/Jahresgang, Kalender (Feiertage/Ferien je Bundesland)
├── Stochastic     S3: Zapfereignis-Generator (seedbar), Superposition
├── Sizing         S4: Summenlinie EN 12831-3 (1 min, NA.3/NA.4-Bilanz, Bild-14-Nachweis,
│                  Wertepaar-Iteration), Din4708 (N, w_V, p), NA.5.6-Vereinfachung ≤ 6 WE,
│                  Perzentil-Auslegung, DIN-1988-300-V̇_S (nachrichtlich)
├── Circulation    S5: Kennwert- und Detailmodus (Zuschlag Stichleitungen: Norm vernachlässigt sie!)
├── Calibration    S6: Skalierung, Vergleichsbericht synthetisch vs. gemessen
└── Results        Zeitreihe (intern 1 min, Ausgabe float[8760]), Kennzahlen, Provenienzlog
WPPlan.Zapfprofil.Tests            (Unit-/Regressionstests, Referenzdaten)

WindowsFormsApplication1           (Bestand — neue Dateien nach Bestandskonvention)
├── Model\      ZapfprofilModel.cs, ZapfprofilDatenModel.cs, NutzungsartModel.cs,
│               Z_ProjektZapfprofilModel.cs                     (m_-Präfixe wie Bestand)
├── Controller\ ZapfprofilCtrl.cs, ZapfprofilDatenCtrl.cs, NutzungsartCtrl.cs,
│               Z_ProjektZapfprofilCtrl.cs                      (nur DataRepository/OleDb;
│               Massen-Insert in einer Transaktion nach Vorlage StromganglinieDatenCtrl)
│               WizardCtrl.cs  → Add_/Del_Projekt_Zapfprofil ergänzen
├── Views\Zapfprofil\  Form_Zapfprofil (Projektzuordnung, Muster Form_Brauchwasser inkl.
│               SetControls(szProjekt, bWizard)), Form_EingDBZapfprofil (Katalogpflege),
│               Form_ErgZapfprofil (Ergebnis, ChartManagerNeu), Form_ZapfprofilKonstruktor
│               (manuelles Profil n. NA.5.2.3) — von Anfang an mit .de-DE/.en-US.resx
└── Allgemein\Simulation\SimulationWaermebedarf.cs → Weiche (s. 3.2a)
```

**(a) Andockpunkt Bilanz:** In `Waermebedarf_berechnen()` wird `Brauchwasserwaerme_berechnen()` um eine Weiche ergänzt: je Projekt entweder Alt-Pfad (Monats-×-Wochenprofil) **oder** Zapfprofil-Pfad — Konfigurationsflag, Default „alt" (regressionsfrei für Bestandsprojekte, TWW zählt nie doppelt). Die Ergebnisfelder `Waermebedarf_Brauchwasser` und `Waermebedarf_Brauchwasser_Monat[12]` bleiben erhalten, damit alle nachgelagerten Formulare/Navigatoren unverändert weiterlaufen. Die projektspezifische Jahressummen-Skalierung (`× pjv/jv`) wird beibehalten — bekanntes Bedienverhalten.

**(b) Auflösungsstrategie:** Der native Kern kann nur 8760 — daher rechnet `WPPlan.Zapfprofil` intern in 1 min (Auslegung) bzw. 1 h (Bilanz) in reinem C# und übergibt an die Bestandskette ausschließlich aggregierte `float[8760]`. Die gesamte nachgelagerte Bilanz-/Dauerlinien-/Erzeugerkette (inkl. `I_heapsort`-Jahresdauerlinie) bleibt unangetastet. Für die Minuten-Charts wird `ChartManagerNeu.IsQuarterHourly` zu einem allgemeinen Minuten-Intervallparameter verallgemeinert (kleiner, lokaler Eingriff).

### 3.2 Datenhaltung Nutzungsarten-Katalog — **revidiert: Access statt JSON**

V1.0 empfahl eine JSON-Ressource; die Repo-Analyse dreht die Entscheidung: **Alle** Fachkataloge liegen in `Kenndaten.accdb`, nur DB-Tabellen werden vom Update- (`UpdateDB.ini [IMPORT]`) und Backup-Mechanismus erfasst, und die gesamte Admin-UI ist auf Recordsets ausgelegt. Neue Tabellen nach Bestandsmuster:

- `Tab_Zapf_Nutzungsart` — Katalog der Nutzungsarten: Bezugsgröße, spezifischer Bedarf {niedrig/mittel/hoch} mit Temperaturbezug, **Quelle + Katalogversion je Wert (Provenienzpflicht als Spalten)**, Tagesgang-Referenz, Stochastik-Parametersatz, Zirkulations-Defaultklasse, Spitzenklasse
- `Tab_Zapfprofil` (Kopf, inkl. `Zeitinterval` in Minuten — Vorbild: `StromganglinieModel.m_Zeitinterval`) + `Tab_ZapfprofilDaten` (Werte, Header/Daten-Muster wie alle Ganglinien)
- `Z_Projekt_Zapfprofil` (Zuordnung, Schema wie `Z_Projekt_Brauchwasser` + optional `ID_Gebaeude`)

**Zwingend:** `UpdateDB.ini` um die neuen Tabellen erweitern (`[TABELLEN]`-DDL, `[IMPORT]`/`[DELETE]`, `ANZAHL`-Zähler) — und bei der Gelegenheit die **fehlenden Brauchwasser-Tabellen nachtragen** (behebt den bestehenden stillen Datenverlust). Die 18 A100-Referenzprofile (nach Beschaffung der Datendateien) und die Ecodesign-Profile werden als ausgelieferte `Tab_Zapfprofil`-Einträge mit Quellenkennung mitinstalliert; INEKON-Werte sind damit ohne Neukompilierung pflegbar, Anwenderprofile koexistieren im selben Schema.
Bewusst **kein** Abdruck der Normtabellen in auslieferbarer Doku: Der Katalog enthält Werte mit Quellen-*Verweis*; das Handbuch beschreibt nur Verfahren und Quellenangaben (s. 3.4).

### 3.3 Öffentliche API-Skizze (V1.1: klassische Klassen — C#-`record`-Syntax steht unter .NET FW 4.8 nicht ohne Shims zur Verfügung)

```csharp
public sealed class ZoneInput {
    public string NutzungsartId;            // Katalogschlüssel aus Tab_Zapf_Nutzungsart
    public double Bezugswert;               // Personen, WE, Betten, … (Einheit aus Katalog)
    public DemandLevel Niveau = DemandLevel.Mittel;
    public double? PersonenJeWE;            // nur Wohnen
    public double? JahresMesswert_kWh;      // Kalibrierung S6
    public SystemTopologie Topologie = SystemTopologie.Speicher;
    public CirculationSpec Zirkulation;     // null = Katalog-/W551-Default
    public CalendarSpec Kalender;           // Bundesland, Ferien, Betriebstage
    public Dictionary<string, double> ExpertOverrides;
}

public sealed class ProfileRequest {
    public List<ZoneInput> Zonen;
    public int Jahr;
    public Aufloesung Aufloesung;           // Stunde8760 | Minute1
    public ProfilModus Modus;               // Deterministisch | Stochastisch
    public int? Seed; public int EnsembleGroesse = 1;
    public TemperaturSpec Temperaturen;     // θ_zapf, θ_kalt fest 10 °C (Auslegung) / Jahresgang (Bilanz)
}

public sealed class ProfileResult {
    public float[] ZapfEnergie8760;         // kW je Stunde — Übergabeformat an SimulationWaermebedarf
    public float[] ZirkulationsVerlust8760; // kW, getrennter Kanal
    public MinuteSeries FeinAufloesung;     // 1-min, nur bei Bedarf gefüllt (Auslegung/FriWa)
    public KeyFigures Kennzahlen;           // kWh/a, l/d@60°C, P50/P90/P95/P99, Volllaststunden
    public List<ZoneResult> JeZone;
    public ProvenanceLog Provenienz;        // jeder Kennwert: Quelle, Katalogversion, Status
}

public interface IProfileEngine {
    ProfileResult Generate(ProfileRequest request);
    SizingResult  Size(SizingRequest request);       // unabhängig vom Bilanzprofil!
}

public sealed class SizingResult {
    public List<StorageDesignPoint> Summenlinie;     // (V_sto, Φ_N)-Wertepaare, 1-min-Nachweis n. Bild 14
    public double BetriebszeitTWW_h_d;               // Σ t_power,on nach A100 NA.5.6 — WP-Taktung/Sperrzeit
    public PercentileSizing Stochastisch;            // P95/P99 + Streuband aus Ensemble
    public Din4708Result N;                          // nur WG; inkl. Gültigkeitsflag; null sonst
    public Din1988Result Rohrnetz;                   // nachrichtlich (V̇_S; nicht aus A100-Spitzenwerten!)
    public List<DesignNote> Hinweise;                // BWP, W551, Gültigkeit, Entwurfsstand A100
}
```

### 3.4 Beschaffen / Portieren / Selbst entwickeln — mit Lizenzstrategie

| Baustein | Weg | Rechtslage |
|---|---|---|
| Zapfereignis-Generator | **Selbst entwickeln in C#** nach der frei publizierten Jordan/Vajen-Parametrik (IEA SHC Task 26); OpenDHW (MIT) als Referenz-/Testorakel, ggf. einzelne Algorithmen sinngemäß portiert mit MIT-Attribution | grün; DHWcalc-EXE (Lizenz unklar) wird **nicht** eingebettet |
| Referenz-Testprofile | `DHWcalc_Files/` aus dem OpenDHW-Repo (MIT) übernehmen | grün (Ursprungsstatus der Dateien mit Restunsicherheit — im Testprojekt, nicht im Auslieferpaket) |
| NWG-Tagesgänge | Eigene Stützstellen, konstruiert aus 18599-Nutzungszeitfenstern + n_SP + DOE/ASHRAE-Schedules (US-Regierungswerk, frei) + qualitativen VDI-6002-Merkmalen; deutsche Niveaus aus S1 | grün, als Modellannahme dokumentiert |
| Normkennwerte (18599 Tab. 4/7, 4708-w_V/p, 1988-300-a/b/c) | Im Code als gekapselte Parameter implementieren (branchenüblich); ~~12831-3 beschaffen~~ **liegt vor (V1.1)**; noch beschaffen: DIN 4708-2/-3, ggf. EN 50440 / EN 13203-2 / CEN-TR 12831-4; DIN-Media-Softwarelizenz anfragen | gelb: implementieren ja, **Tabellen nirgends abdrucken** (nicht in UI-Tabellenform, Handbuch, Marketing); 4708-Kennwerte über frei publizierte Herstellerunterlagen zitierfähig |
| **A100-Referenzprofile (18 Minutenprofile)** | Datendateien (CD-ROM/ZIP nach NA.5.2.5) über DIN Media beschaffen; als `Tab_Zapfprofil`-Einträge mit Quellenkennung ausliefern; Weißdruck-Status A100/A1 prüfen (beides Entwürfe 2021) | gelb: Nutzung als Rechengrundlage in lizenzierter Software vor Auslieferung mit DIN Media klären; Profil-*Grafiken* nicht reproduzieren; Ergebnisse mit Entwurfsstand-Hinweis kennzeichnen |
| VDI 6002 Bl. 1/2 | Kennwerte (l/(vp·d)-Bandbreiten) mit Sekundärquellen-Zitat (Buderus) verwenden; **Tagesgang-Tabellen/Diagramme nicht reproduzieren** — eigene Formvektoren, die die beschriebenen Merkmale qualitativ abbilden | gelb/rot ohne VDI-Lizenz → durch Eigenkonstruktion umgangen |
| **VDI 4655:2021** (V1.2) | **Nur Methodik** implementieren (Gleichungen 1–6, Typtagsystematik, Wetterzuordnung — als solche nicht schutzfähig); die **Datensätze (Formvektoren, 45 Faktortabellen, Typtag-Reihenfolgen, PV-Profile) NICHT ausliefern**, sondern **Import-Schnittstelle** für den lizenzierten Anwender (CD-ROM-Daten). Vermerk „auch innerbetrieblich nicht vervielfältigen" beachten — auch bei den Grundlagenberichten | rot bei Auslieferung der Daten → durch Import-Design umgangen; vor Codierung mit VDI/Beuth klären |
| Ecodesign-Profile 3XS–4XL (VO (EU) 814/2013) | Vollständig einbetten, auch sichtbar im UI (Geräte-Rückrechnung η_wh/COP_DHW, EFH-Plausibilisierung M ≈ 5,845 / L ≈ 11,655 kWh/d) | **grün, einzige frei abdruckbare Zapffolge** |
| Kein Python zur Laufzeit | Python (OpenDHW/LPG) nur intern als Entwicklungs-/Validierungswerkzeug; optionaler Offline-Einsatz von pylpg (MIT) zur Erzeugung interner Vergleichsensembles | grün; keine Auslieferungsabhängigkeit |
| Ausschluss | StROBe (keine Lizenz), pysimdeum-Einbettung (EUPL-Copyleft), DHWcalc-EXE-Einbettung | rot |

### 3.5 Phasen und Aufwand

Schätzung in Personentagen (PT), erfahrener .NET-Entwickler + fachliche Begleitung; Unsicherheit ±30 %.

| Phase | Inhalt | PT | Ergebnis |
|---|---|---|---|
| **P0 Grundlagen** | ~~12831-3-Beschaffung~~ erledigt; noch: **A100-Profildatendateien (ZIP/CD) + Weißdruck-Status klären**, DIN 4708-2/-3 beschaffen; Katalog-Schema (Access-Tabellen), Erstbefüllung ~15 Typen aus Tab. NA.4 mit Provenienz; DB-Hygiene: `UpdateDB.ini` um Zapfprofil- **und** fehlende Brauchwasser-Tabellen ergänzen, `BrauchwasserCtrl`-Spaltenbug (`M1…M12`/`Monat_n`) beheben | 7–9 | Katalog v1.0 in `Kenndaten.accdb`, Quellendossier, DB-Migration sauber |
| **P1 MVP Bilanz (deterministisch)** | Projekt `WPPlan.Zapfprofil` + Tests aufsetzen; S1 + S2 + S5: 8760-h-Profil Wohnen EFH/MFH + 6 wichtigste NWG-Typen, Zirkulationskanal, Kalender, Provenienz; **Weiche in `SimulationWaermebedarf`** (Flag, Default alt), Excel-Referenz-Abgleich | 13–16 | **MVP:** nutzbares Jahresprofil für WP-JAZ/Bivalenz in der Bestands-Bilanzkette |
| **P2 Auslegung deterministisch** | Summenlinie n. Bild 14 mit A100-Regeln (NA.3/NA.4-Bilanz, τ = 16,67, Φ_N = min, Guards), Wertepaar-Iteration, NA.5.6-Schnellpfad ≤ 6 WE, DIN-4708-N mit echten Wertigkeiten, DIN 1988-300 nachrichtlich, Ergebnis-Tripel + Σ t_power,on, Profil-Import (A100-Dateien, B.2-Expansion mit Flag, Temperatur-Renormierung n. A1), Konstruktor-Maske n. NA.5.2.3 | 12–14 | Speicher-/Erzeugerauslegung ohne Stochastik, normbasiert |
| **P3 Stochastik** | Zapfereignis-Generator (4+2 Kategorien), Superposition N Einheiten, Urlaubs-Dekorrelation, Perzentil-Auslegung, Ensemble, Regressionstests gegen DHWcalc-Files, Konsistenztest gegen deterministischen Pfad, Kalibrierung der NWG-Parameter an den A100-Referenzprofilen | 15–20 | Realistische Spitzen, topologieabhängige Gleichzeitigkeit |
| **P4 UI** | Masken nach Bestandsmuster (Form_Zapfprofil/Form_EingDBZapfprofil/Form_ErgZapfprofil + Konstruktor, 3 Stufen, Live-Vorschau via `ChartManagerNeu` inkl. Minuten-Verallgemeinerung, Warnlogik, Auslegungsdialog), Wizard-Eintrag (`ZAPFPROFIL_ITEM` ans Listenende!), Mischnutzung, Export; von Anfang an de-DE/en-US-resx | 12–15 | Vollintegration WinForms |
| **P5 Kalibrierung + Validierung** | Messdatenimport (CSV-Format n. A100 NA.5.2.4), Skalierung, Vergleichsbericht (P90/√N-Check), Validierung gegen Carleton-Daten + INEKON-Messprojekte, Katalogausbau auf ~25–27 Typen (Tab. NA.4 voll) | 10–12 | Version 1.0 des Moduls |
| **Summe** | | **≈ 69–86 PT** | |

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
| Norm-Rechenregeln (neu V1.1) | τ-Formel: Dimensionsprobe 16,67 min-Koeffizient; NA.3/NA.4-Rekursion gegen Handrechnung; Φ_N = min-Regel; A_HE-Guard (WP-Formel < ~105 l → Warnung statt negativer Fläche); Temperatur-Renormierung B.4 (60/13,5 °C) ↔ NA.4 (45/10 °C) | exakt |
| Auslegungsvergleich MFH (ersetzt IKZ-Anker, V1.1) | **Eigener Dreifachvergleich** derselben Anlage: DIN-4708-Profil (N n. WE-Zahl) vs. A100-MFH-Messprofil (NA.5.2.6.14) vs. Tab.-NA.4-Richtwert; Sollrelation P(4708) > P(NA.4) > P(Messprofil), Bedarfsrelation 53,8/30/19,7 l/(P·d) als Anker; Konsistenz A100-Profile: V_day/(l/P·d) = Personenzahl exakt, Q/V ↔ Δθ 35 K | Relationen zwingend; Bedarfswerte exakt |
| Ecodesign-Profile | Q_ref-Summen exakt (M 5,845 / L 11,655 kWh/d …) | exakt |
| VDI-4655-Anker (neu V1.2) | Jahresenergie EFH = 500·N_Pers kWh, MFH = 1000·N_WE kWh **bei angeglichener Bilanzgrenze** (Zirkulation inkl., Speicher exkl.); Prüfsumme Σ n_TT·F_TWE,TT ≈ 0; F_TWE,SWX auf 0 geklemmt; Sommer-Heizlast = reines TWW; Beispielrechnung Abschnitt 8 (EFH 110 m², 3 Pers., TRY05, Q_TWE,a 1500 kWh) als Regressionstest | Anker ±Toleranz; stochastische Streuung SOLL breiter sein als VDI-Tagesband (−17 %/+36 %) |
| NA.5.6-Schnellpfad | Nachrechnung des Vereinfachungsverfahrens ≤ 6 WE (N-2-Profil; Hinweis: Entwurfs-Widerspruch Fließtext NA.5.2.6.15 vs. Tabelle NA.5.2.6.16 — als N = 2 implementiert, im Code kommentiert) | exakt |
| Messvalidierung | Carleton/Québec 5-min-Profile (73 EFH, frei): Tagesgangform, Ereignisdauern; INEKON-611-MWh-Datensatz: Messspitze im P85–P95-Band der synthetischen Dauerlinie, √N-Skalierungstest der Überschätzung | Bandkriterien, dokumentierter Bericht |
| Zirkulation | DELTA-Q-Kennwertreproduktion; SIA-Anker „Verluste ≈ +50 % Nutzenergie" als Plausibilitätsfenster kleiner MFH | ±20 % Fenster |

Alle Tests als Unit-/Integrationstests in `WPPlan.Profiles.Tests`, CI-Pflicht; Katalogänderungen triggern den kompletten Referenzlauf.

### 3.7 Risiken und Gegenmaßnahmen

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| A100-Profildatendateien nicht/verzögert beschaffbar; A100/A1 bleiben Entwürfe oder Weißdruck weicht ab | 18 Referenzprofile nicht nutzbar; Nacharbeit an Rechenregeln | Tab.-B.2-Stundenprofile + DIN-4708-Profile + Konstruktor als vollwertiger Fallback (alle Zahlen liegen vor); Profile nur im Katalog (Daten), nicht im Code → Austausch ohne Release; Entwurfsstand im Ergebnis ausweisen |
| Urheberrecht Normtabellen (DIN/VDI) | Abmahnung/Vertriebsrisiko | Strategie 3.4 strikt: implementieren, nicht abdrucken; DIN-Media-Softwarelizenz anfragen (inkl. A100-Datendateien); VDI-Tabellen durch Eigenkonstruktion ersetzt; juristische Prüfung vor Release |
| Nativer Kern `bhkwplan.dll`/COM (x86, 8760 fix, kein Quellcode, nicht testbar) | feinere Auflösung unmöglich im Bestandskern; Deployment-/Registrierungsfehler | Zapfprofil-Rechnung komplett in `WPPlan.Zapfprofil` (reines C#), Übergabe nur als `float[8760]`; kein Eingriff in die DLL |
| DSN „TEST"-Pflicht, zwei DB-Schichten (ODBC + OleDb), `Bezeichner`-verknüpfte SQL-Strings | Laufzeitfehler, Injektions-/Sonderzeichenprobleme | Neues Modul nur über `DataRepository` (parametrisiert), Verknüpfung ausschließlich über IDs; Mischbetrieb dokumentieren |
| `UpdateDB.ini`-Lücken (heute schon beim Brauchwasser) | stiller Datenverlust beim Software-Update | P0-Pflichtaufgabe: alle neuen UND die fehlenden Brauchwasser-Tabellen eintragen; Update-Testfall in CI |
| Wizard-Konstanten sind Listenindizes | Verschieben aller Seiten bei falscher Einfügeposition | `ZAPFPROFIL_ITEM` ans Ende (= 14); in `ProjektNeu()` UND `ProjektBearbeiten()` ergänzen |
| NWG-Ereignisparameter unvalidiert | falsche NWG-Spitzen | Kennzeichnung „Modellannahme" in Provenienz; Auslegung primär über Summenlinie mit Katalog-Auslegungstag; S6-Kalibrierpfad; Messkooperation anstreben |
| Stochastik-Ergebnisse für Prüfer schwer vermittelbar | Akzeptanzproblem | deterministischer Pfad als Nachweisebene, fester dokumentierter Seed, Normvergleich immer im Ergebnisblatt |
| Zwei Pfade divergieren bei Wartung | Inkonsistenz | automatischer Konsistenztest (Energiesumme, Erwartungswert) in CI |
| Performance 1-min × N Einheiten in WinForms | UI-Blockade | Rechenkern async/Task-basiert; Vorschau nur deterministisch; Stochastik als expliziter Rechenlauf mit Fortschrittsanzeige |
| Katalogpflege erodiert (Werte ohne Quelle geändert) | Verlust der Nachvollziehbarkeit | Schema erzwingt Quelle+Version je Wert; Katalog-Changelog; Referenztestlauf bei jeder Änderung |
| Unterschätzung realer Einzeltagesspitzen (VDI-6002-Warnung „fast doppelt") | knappe Auslegung | Perzentil-Auslegung P95–P99 statt Mittelprofil-Maximum; Sicherheitsband im Auslegungsdialog ausgewiesen |

---

## Entscheidungen, die INEKON noch treffen muss (Stand V1.1)

1. **Beschaffung:** **A100-Referenzprofil-Datendateien** (ZIP/CD-ROM nach NA.5.2.5) bei DIN Media anfragen + Weißdruck-Status von A100/A1 klären — höchste Priorität; dazu DIN 4708-2/-3, optional EN 50440, EN 13203-2, CEN/TR 12831-4; DIN-Media-Lizenzanfrage für Softwarehersteller ja/nein; VDI-Lizenz (6002/4655) bewusst *nicht* — bestätigen.
2. **Typenumfang v1.0:** Startumfang ~15 aus Tab. NA.4 (Vorschlag: EFH/EFH gehoben/DHH/MFH/MFH gehoben, Hotel 3 Niveaus, Heim, Krankenhaus, Büro, Schule ± Duschen, Sportanlage mit Dusche, Fitness/Sauna, Werkstatt/Industrie, Gastronomie) — bestätigen/ändern.
3. **Auslegungs-Default:** P95 oder P99 als Standard-Perzentil der stochastischen Auslegung; Position zur Erzeuger-Auslegung nach Jahresdauerlinie/Bivalenz (Vorprojekt-Praxis) als dokumentierter Standardweg.
3a. **VDI-4655-Datenstrategie (V1.2):** Import-Schnittstelle für die anwendereigenen CD-ROM-Datensätze (empfohlen) vs. Klärung einer Verwertungslizenz mit VDI/Beuth zur Mitauslieferung — Richtungsentscheidung; und ob VDI 4655 die bevorzugte Wohn-Formvektorquelle wird oder gleichrangig neben Eigenkonstruktion/Ecodesign steht.
3b. **BHKWPLAN.DLL-Zukunft (V1.2):** Nur andocken (x86-Kern bleibt) oder schrittweise C#-Ablösung nach dem Golden-Master-Plan des Dossiers (Gruppen A→D)? Beeinflusst, wie viel Testinfrastruktur das Zapfprofil-Projekt gleich mitbringt.
4. **Kaltwassertemperatur:** Auslegung fest 10 °C (Norm, gesetzt); für die **Bilanz**: fester Jahresgang (10 °C ± 3 K) oder Kopplung an die vorhandenen Klimaregion-/TRY-Daten von WP-Plan (`Klima\`, `Tab_Klimadaten`)?
5. **Messdatenstrategie:** Freigabe des 611-MWh-Datensatzes (und weiterer Projekte) für den √N-Validierungstest; ggf. Kooperation mit Heizkostenabrechnern für deutsche Messdaten.
6. **Alt-Pfad Brauchwasser:** Weiche mit Default „alt" ist gesetzt (Regressionsschutz) — aber: soll der Alt-Pfad mittelfristig abgekündigt werden (Migrationsassistent Monatsprofil → Zapfprofil), oder dauerhaft koexistieren?
7. **Katalog-Redaktionsprozess:** Wer bei INEKON pflegt die neuen `Tab_Zapf_*`-Tabellen in `Kenndaten.accdb`, wer gibt Änderungen frei (Vier-Augen-Prinzip wegen Provenienzpflicht)? Sollen die INEKON-Katalogwerte für Anwender schreibgeschützt sein?
8. **Juristische Prüfung** der Lizenzstrategie (Abschnitt 3.4) vor Erstauslieferung — insbesondere DHWcalc-Referenzdateien im Testumfang, Normwerte-Kapselung und Nutzung der A100-Datendateien in verkaufter Software.
9. **Testprojekt in der Solution:** Zustimmung, dass `WPPlan.Zapfprofil` + `WPPlan.Zapfprofil.Tests` als erste Testinfrastruktur des Repos aufgenommen werden (heute existieren keine Tests).

**Offene fachliche Prüfpunkte (gekennzeichnet):** VDI-6002-Bl.-2-Inkonsistenz Krankenhaus 33 vs. 35 l/(vp·d) (Bandbreite hinterlegen); Digitalisierung der VDI-6002-Diagrammprofile (nur interne Nutzung); die √N-Erklärung des 2,4×-Befunds ist Hypothese und wird erst durch den Validierungstest (3.6) belegt; die 45-°C-Bezugstemperatur der 18599-Tab.-7-Werte ist aus der Bagatellklausel abgeleitet, nicht explizit normiert; **neu V1.1:** Diskrepanz Fließtext/Ablaufdiagramm bei Gl. (14) der Hauptnorm (ggf. Rückfrage NA 041-05-01 AA), Entwurfs-Widerspruch NA.5.6 (N = 2 vs. N = 4), Inkonsistenz Tab. NA.3 Zeile 7 (18 statt 21 kWh), Einheit der Bestands-Monatswerte in `Tab_Brauchwasser` (implizit kWh, im Code nicht belegt — vor P1 am Bestand verifizieren).
