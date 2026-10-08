# Entwurf KK — die raumgeführte Kühlkurve (Gegenstück zu H2)

**Stand 08.10.2026 · gebaut (KK0–KK6, KZ1, KZ2), Basis R44 `2026-10-08_R44_Kuehlkurve`; offen: Sichtabnahme unter Windows.**
Wie gebaut und wo der Bau vom Entwurf abweicht: Abschnitt 6.2. **E106 entschieden (Abschnitt 7): alle Fragen nach Empfehlung außer Q-KK-4 — der Mehrzonenweg gehört dazu,
seine Kälteseite wird gekoppelt; Q-KK-6 erweitert um ein Mehrzonen-Referenzprojekt. Q-KK-7 nachgereicht und entschieden: (a) die
Kühlübergabe je Zone gilt ab AK1 (Abschnitt 7).** Auftrag aus **E105** (Anwender, 08.10.2026: „Starte im Anschluss die raumgeführte
Kühlkurve“): Q-AK3K-3 wird als eigener Gegenstand aufgenommen, Variante (a) des [Entwurfs AK3-K](2026-10-07_Entwurf_AK3-K.md)
(Spalte am Gebäude, nur AK3, Schemaschritt, Dialogfeld, Einfrierregel) ist die Ausgangsskizze. Gelesen auf `fc601d202`,
Stichproben auf `76c32d5f0`, der Mehrzonenweg auf `3cd7e01e7` (Basis R43 `2026-10-07_R43_Kaelteseite_AK3K`, vierundzwanzig
Projekte; Testdatenbank Schemastand 201). **Nummern:** Schemaschritt **202** ist angemeldet (origin `41c237274`, Kopfzeile der
Statusdatei: „Eingabespalten am Gebäude und, wo der Mehrzonenweg sie braucht, an der Zone, dazu Ergebnisspalten“); hier heißt er
weiter **S1**. Die Projektnummern der beiden Referenzprojekte **RP-KK** und **RP-KKZ** und die Basis **R44** werden beim Bau
angemeldet; die Projektnummer 1060 ist vom [Konzept Übergabegrenze](../Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md)
vorgemerkt. **Verfahren:** zwei Leser (L1: Code mit `Datei:Zeile`; L2: Papiere, Widersprüche, Fragen, Folgen), Stichproben am
Code, Synthese hier; wo die Leser sich widersprachen, gilt der Code; der Mehrzonenweg (1.4) ist nach E106 eigens gelesen.
**Grundlage:** [Anlagenkopplung](../Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 7.1, 7.2, 7.4 Nr. 5 und Nr. 10;
[Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) K21; [Entwurf AK3](2026-10-07_Entwurf_AK3.md) H2,
Festlegungen 23 und 24; [Entwurf AK3-K](2026-10-07_Entwurf_AK3-K.md) 4.2, 4.3, Q-AK3K-3; [Register](../Status_Gebaeudesimulation_VDI6007.md)
E37, E102, E104, E105, E106. Die Berichtigung der Konzepte (1.3, WK1–WK3) ist mit KK0 nachgezogen.

## 0. Das Ergebnis in Punkten

- **Der Kühlvorlauf wird eine Stundenreihe.** Heute ist er ein fester Wert je Gebäude und je Erzeuger; KK macht ihn zu einer
  Kurve über die Außentemperatur mit **Fußpunkt** (nie „aus“) und einer **Absenkung nach der Raumtemperatur** — der Spiegel
  von Heizkurve und Raumeinfluss H2 (E106, Q-KK-3 (a)).
- **Der Gebäudelöser bleibt unberührt.** Er liest den Kühlvorlauf schon je Stundenrand; neu sind die Reihe im Eingangsbauer,
  die Absenkung im Kreis und die Erzeuger am Stundenvorlauf.
- **Gebäude und Erzeuger gleiten** (E106, Q-KK-1 (a)): Der Kälteerzeuger fährt je Stunde den kältesten verlangten Vorlauf,
  Wärmepumpe und Kältemaschine rechnen am Stundenvorlauf; erst das bringt die bessere Leistungszahl an milden Kühltagen.
- **Nur Stufe AK3** für Kurve und Raumeinfluss (E106, Q-KK-2 (a)); der Kältespeicher bleibt an seinem festen Paar.
- **Der Mehrzonenweg gehört dazu** (E106, Q-KK-4 (b)): Seine Kälteseite wird gekoppelt — Kühlübergabe je Zone am gemeinsamen
  Kühlvorlauf des Gebäudes, Führungsgröße des Raumeinflusses die Zone mit der größten Überschreitung ihres Kühlsollwerts.
  Der Kreis von AK3 rechnet Mehrzonengebäude schon; was fehlt, ist die Kühlübergabe je Zone (1.4). Ob sie wie im Einzonenweg ab
  AK1 gilt oder nur auf AK3, entscheidet **Q-KK-7 (a)**: ab AK1 (E106).
- **Leere Spalten rechnen wie heute:** Ohne wirksamen Wert entsteht kein Objekt, die Basis R43 bleibt byte-gleich bis zur
  Basiswelle; Schemaschritt S1 (= 202) mit fünf Eingabespalten am Gebäude (drei der Kurve, zwei des Auslegungswegs, E107) und drei Ergebnisspalten. Die Zone braucht **keine**
  neue Eingabespalte: Ihre drei Kühlübergabespalten bestehen seit Schritt 137 und werden nur noch nicht gerechnet.
- **Zwei neue Referenzprojekte**, beide nicht in der CI: **RP-KK** als Kopie von 1058 (Einzonenweg) und **RP-KKZ** als Kopie
  von RP-KK mit zwei Zonen (Mehrzonenweg, Abschnitt 4). R44 bewegt kein bestehendes Projekt.
- **Rechenzeit:** Einzonenweg mittlere Durchläufe des Kreises von 3,21 auf etwa 3,4–3,5 (+6–9 %), Mehrzonenweg je Durchlauf
  die Zonenschleife dazu; geschätzt, gemessen in KK3 und KZ2.
- **Aufwand 15,5–22 PT** in neun Wellen KK0–KK6 mit KZ1 und KZ2 (Entwurfsstand ohne Mehrzonenweg 10–15 PT; Abschnitt 6.1).

## 1. Befunde

### 1.1 Die Heizseite wie gebaut (H2)

- **Zwei Bausteine.** Die außentemperaturgeführte Heizkurve (Klasse `Heizkurve`, ab AK1) wird im Eingangsbauer einmal als
  Jahresreihe `VorlaufC[h]` gerechnet (`GebaeudeModellEingang.cs:1691-1704`); der Raumeinfluss H2 (Klasse `Raumeinfluss`,
  nur AK3) hebt im Kreis je Durchlauf an: θ_V = HK(θ_out) + k_R·max(0, max_z(θ_soll,z − θ_air,z)), gekappt am
  Auslegungsvorlauf und an `Vorlauf_Max` (`Raumeinfluss.cs:7-22, 103-113`), gebaut allein in `SimulationControl.Ak3.cs:141`.
- **Ohne wirksamen Wert kein Objekt:** `Raumeinfluss.AusGebaeuden` liefert `null`, wenn kein Gebäude `Heizkurve_Aktiv` und
  k_R > 0 trägt (`Raumeinfluss.cs:48-63`) — darum blieb die Basis mit Schritt 198 unverändert.
- **Heizgrenze:** `Heizkurve.VorlaufC` gibt jenseits der Heizgrenze NaN (`Waermeuebergabe.cs:494-500`).
- **Kreis:** Durchlauf 1 ohne Anhebung, jeder weitere mit der Anhebung aus der Lösung derselben Stunde; Abbruch unter
  anderem |ΔV| ≤ 0,05 K und |ΔH2| ≤ 0,05 K, Höchstzahl 20, Mehrzonen Zonen × Durchläufe ≤ 120, Pendelregel der Stützstelle
  (`Anlagenkopplung.cs:166-178, 503-531`). Der Projektvorlauf ist bedarfsgewichtet (`Anlagenkopplung.cs:656-675`).
- **Erzeuger:** Auf AK3 interpoliert `WaermepumpeKapazitaet` Leistung und Leistungszahl über den Vorlauf je Durchlauf
  (`Stundenangebot.cs:238-330`, `VorlaufInterpolation.cs`); auf AK1/AK2 wählt der gerechnete Heizkreisvorlauf die Kennlinie
  nachträglich (`SimulationControl.cs:1633-1636`).
- **Ergebnis:** Kennzahlen des Kreises an `Tab_ErgebnisEnergiebedarf` (Schritt 198); die Anhebung selbst wird nicht gespeichert.

### 1.2 Die Kälteseite heute

- **Gebäude:** ein skalarer, **fester** Kühlvorlauf = max(Anlagenvorlauf, `Kuehl_Vorlaufgrenze`) — „kälter als die Anlage wird
  der Vorlauf nie“ (`GebaeudeModellEingang.cs:2117-2124`, stichprobengeprüft). Die Vorlaufgrenze ist eine **Vorgabe statt
  einer Taupunktrechnung** (`GebaeudeModellEingang.cs:2097-2103`); keine Feuchtebilanz.
- **Löser:** Er liest den Kühlvorlauf schon je Stundenrand (`Stundenrand.cs:62, 267`; `Zonenmodell2K.cs:2006-2008`), Heiz-
  und Kälteseite teilen den Löser über den Spiegel (`Kuehluebergabe.cs:96-111`).
- **Erzeuger fest:** Anlagenvorlauf = kältester `Kuehl_Vorlauf` der Wärmepumpen im Kühlbetrieb, sonst der Kältemaschinen
  (`WPCtrl.cs:520-550`), einmal je Lauf gelesen. Die Kühlkennlinie der Wärmepumpe wird **einmal je Gerät** für einen Vorlauf
  gebildet (`SimulationControl.Kaelte.cs:179`), ganzzahlige Vorlaufachse; die Kältemaschine rechnet mit dem festen Feld
  `Kaltwassertemperatur`, obwohl ihre Kennlinie die Achse schon trägt (`Kaeltemaschine.cs:29-130, 228, 285`).
- **Kälteschranke:** am festen Kühlvorlauf, einmal am Stundenbeginn befragt (`Kaelteangebot.cs:250-291`); die
  Kapazitätsnaht `IKaelteerzeugerkapazitaet.Abfragen(stunde, kuehlVorlaufC)` nimmt den Vorlauf schon entgegen, die Wärmepumpe
  nutzt ihn aber nur für die freie Kühlung, die Kältemaschine gar nicht (`Kaelteangebot.cs:88-131`, stichprobengeprüft). Die
  Naht `Kaeltekorrektur` (`Anlagenkopplung.cs:96-103`) ist im Lauf nicht belegt, nur in den Orakeln. Die Schranke ändert sich
  über die Durchläufe nicht — darum kostet die Kälteseite heute keinen Durchlauf (1058 und 1059 je 3,21 im Mittel).
- **Kältespeicher:** eigenes festes Temperaturpaar, einschichtig, Leser am Stundenbeginn (`SimulationPufferspeicher.cs:80-104`).
- **Mehrzonenweg:** keine gekoppelte Kälteseite (`GebaeudeModellEingang.cs:1301`); Einzelheiten in 1.4.
- **Referenzprojekte mit Kühlübergabe:** 1047 (AK1), 1056 (AK2), 1058 (AK3, Wärmepumpe, Gebäudevorlauf 18 °C über der Grenze
  16 °C) und 1059 (AK3, Kältemaschine mit Kältespeicher, Gebäudevorlauf an der Grenze 16 °C), alle im Einzonenweg. In 1058 und
  1059 ist die Reihe `kuehlvorlauf_0.csv` konstant. Kein Referenzprojekt rechnet ein Mehrzonengebäude mit Kühlung gekoppelt.

### 1.3 Widersprüche in den Papieren und ihre Auflösung

| # | Widerspruch | Auflösung |
|---|---|---|
| WK1 | Anlagenkopplung 7.4 Nr. 5 sagt „keine Kühlkurve und keine Kennlinienwahl je Stunde“ und verweist zugleich auf „die raumgeführte Kurve in AK3“ als belastbare Führung — der Verweis ist uneingelöst | KK löst ihn ein; Nr. 5 ist mit KK0 auf „fester Vorlauf ohne Kühlkurve, raumgeführte Kühlkurve auf AK3 nach Entwurf KK“ berichtigt |
| WK2 | Anlagenkopplung 7.1 und E37 (A3) nennen die Kühlkurve „vertagt“; Kühlkurve und Kennlinie setzen dort einen festen Vorlauf je Stunde voraus | E105 hebt die Vertagung für AK3 auf; ohne Kühlkurve gilt der feste Vorlauf weiter (mit KK0 nachgezogen) |
| WK3 | Q-AK3K-3 (b) „kein Schemaschritt dafür“ gegen Variante (a) „Schemaschritt“ | (b) galt dem Entwurf AK3-K; KK hat seinen eigenen Schritt S1 (= 202) |
| WK4 | Die gespiegelte Heizkurve schaltete bei kühler Außenluft ab (Anlagenkopplung 7.4 Nr. 5; Code: NaN jenseits der Heizgrenze) | Kurvenform mit Fußpunkt statt Grenze (Festlegung 2) |
| WK5 | Wiki Gebäudemodell und Wiki Kühlung sagen „Kühlkurve gibt es nicht“, „Vorlauf der Kühlübergabe ist fest“ | gilt bis KK5; Nachzug in KK6 |
| WK6 | Mehrzonenmodell (Schritt 137): die Kühlübergabespalten der Zone werden „erst ab G6 gerechnet“ — der Mehrzonenweg rechnet die Kälte aber ideal (1.4) | KZ1 löst es ein: die Spalten werden gerechnet (E106, Q-KK-4 (b)) |

### 1.4 Der Mehrzonenweg heute

- **Heizseite gekoppelt ab AK1 (AK1z, E63).** Schritt H je Zone am **gemeinsamen Vorlauf** des Gebäudes: Art je Zone, sonst die
  des Gebäudes; eine Zone mit `IDEAL` oder leer, eine unbeheizte Zone und der adiabate Vorlauf der 4-K-Regel rechnen ohne
  Übergabe (`GebaeudeModellEingang.cs:1233-1247`). Auslegungspunkt, Heizkurve und Nennleistung kommen vom Gebäude, die Übergabe
  je Zone aus den sieben Übergabespalten von `Tab_Zone` (Referenzprojekt 1054); die Heizkurve wird einmal je Stunde am
  **höchsten** Heizsollwert der gekoppelten Zonen gerechnet, dieselbe Reihe für jede Zone (`GebaeudeModellEingang.cs:1740`,
  `1850-1874`).
- **AK3 rechnet Mehrzonengebäude im Kreis.** Für ein gekoppeltes Mehrzonengebäude entsteht aus denselben Zonen eine zweite
  Zonenschleife als Stepper des Kreises — „Anlage außen, Zonen innen“ (`Vdi6007Rechenweg.cs:254-259`, `GebaeudeStepper.cs:76-80`);
  Schutzgrenze Zonen × Durchläufe ≤ 120 (`Anlagenkopplung.cs:177-178, 560-564`). Der Raumeinfluss H2 führt schon über die
  Zonen: Führungsgröße je Gebäude die Zone mit der größten Unterschreitung (`Raumeinfluss.cs:10-13, 103-113`). **Kein
  Referenzprojekt hält AK3 im Mehrzonenweg** (1054 rechnet AK1, 1052 ungekoppelt).
- **Kälteseite ideal auf allen Stufen.** `KuehlKopplungWirksam = kuehlKopplung && !Mehrzonenweg`; „im Mehrzonenweg bleibt die
  Kälteseite ideal“ (`GebaeudeModellEingang.cs:1297-1306`); die Warnung `SIMENG_G6_AK1_IDEAL` sagt es dem Anwender
  (`Vdi6007Rechenweg.cs:379-380`). Die Kühlübergabe wird nur im Einzonenweg aufgelöst (`KuehlKopplungAufloesen`,
  `GebaeudeModellEingang.cs:2070`).
- **Kälteschranke wirkt schon je Zone.** Auf AK3 verteilt der Kreis die Kälteschranke wie die Wärmeschranke nach dem
  unbegrenzten Kühlbedarf je Zone (`Anlagenkopplung.cs:374-405`) und kappt mit `Stundenrand.MitKaelteverfuegbarkeit` die
  Kühlleistung der Zone (`Stundenrand.cs:99-111`) — bei idealer Kühlung nur als Leistungsgrenze; das Vorlaufangebot
  `KuehlVorlaufAngebotC` bleibt ohne Übergabe wirkungslos.
- **Kühlübergabe je Zone: Spalten da, Rechnung nicht.** `Tab_Zone` trägt `Kuehl_Uebergabe_Art`, `Kuehl_Uebergabe_Exponent`,
  `Kuehl_Uebergabe_Leistung_Nenn` (Schritt 137; `ZoneModel.cs:101-107`; NULL = Wert des Gebäudes bzw. Anteil der Zonenfläche),
  der Controller liest, schreibt und prüft sie (`GebaeudeZonenCtrl.cs:344-345, 1250-1252, 1367-1369`), der Rechenkern übernimmt
  sie aber nicht: `Zoneneingaben` kennt nur die Heizübergabe und die Kühlschalter (`Zonenvorgaben.cs:87-109`). Der Zonendialog
  zeigt Übergabefelder nur für die Heizseite (`ZonenDialog.razor:149-190`). Kühl-Auslegungspunkt und `Kuehl_Vorlaufgrenze`
  gibt es nur am Gebäude.
- **Zonensperre und Tagesart je Zone** (E104, Q-AK3K-7 (b); `Zonensperre.cs:65`): Verschiedene Zonen dürfen am selben Tag heizen
  und kühlen (`GebaeudeErgebnisexport.cs:157`); die reversible Wärmepumpe bleibt projektweit je Tag Heiz- oder Kältemaschine
  (K8a), die andere Seite trägt dann den Grund „Umschaltung“.
- **Folge für Q-KK-4 (b):** Die Voraussetzung „Kälteseite im Mehrzonenweg koppeln“ ist die Kühlübergabe je Zone am gemeinsamen
  Kühlvorlauf — ein Spiegel von AK1z, kein neuer Kreis. Sie ist **gebäudeseitig und stufenunabhängig** wie die Kühlübergabe im
  Einzonenweg (ab AK1); E106 beschränkt aber nur die Kühlkurve auf AK3. Ob die Kühlübergabe je Zone ab AK1 oder nur auf AK3
  gilt, war deshalb als **Q-KK-7** offen; entschieden (a) ab AK1 (E106).

## 2. Architektur

### 2.1 Kurvenform

Der Spiegel der Heizkurve taugt nicht: Kühllast entsteht auch bei kühler Außenluft (Sonne, innere Lasten), die Kurve darf
daher nie „aus“ liefern. Die Kühlkurve ist eine **Zwei-Punkt-Kurve** über die Außentemperatur:

- **Fußpunkt** θ_V,F: der Vorlauf bei Außentemperatur am Kühlsollwert der Stunde und darunter (waagrecht, nie NaN);
- **Auslegungspunkt** θ_V,K,N (vorhandene Spalte `Kuehl_Auslegung_Vorlauf`) bei der Auslegungs-Außentemperatur der Kühlung
  θ_out,K,N nach dem gewählten Auslegungsweg (E107, Festlegung 3): höchste Stundentemperatur, wärmstes Tagesmittel mindestens
  Kühlsollwert + Mindestspanne (Vorgabe) oder Eingabe am Gebäude (`Kuehlkurve.Bilden`);
- **Fußpunktregel** (Festlegung 18): ein Fußpunkt kälter als der Auslegungsvorlauf wird auf ihn geklemmt — die Kurve fällt nie
  mit steigender Außentemperatur an;
- dazwischen linear, darüber waagrecht am Auslegungspunkt;
- **Grenzen:** unten die Vorlaufgrenze des Gebäudes und der kälteste erreichbare Erzeugervorlauf (2.3), oben der Fußpunkt,
  der stets einen Mindestabstand unter dem Kühlsollwert der Stunde hält (sonst wäre die Übergabeleistung null,
  `Kuehluebergabe.cs:101-107`).

Ohne Raumeinfluss ist die Reihe explizit und wird wie `VorlaufC` einmal im Eingangsbauer gerechnet.

### 2.2 Raumeinfluss

θ_V,K = KK(θ_out) − k_K·max(0, θ_air − θ_max), mit k_K aus `Kuehlkurve_Raumeinfluss` [K/K], θ_max dem Kühlsollwert der Stunde,
θ_air der Raumluft derselben Stunde; unten gekappt an der Untergrenze aus 2.1. **Führungsgröße** im Einzonenweg die Zone selbst,
im Mehrzonenweg die Zone mit der größten Überschreitung (2.8). **Nachführung im Kreis wie H2:** Durchlauf 1 ohne Absenkung, jeder
weitere mit der Absenkung aus der Lösung derselben Stunde; neues Abbruchglied |ΔK2| ≤ 0,05 K neben ΔH2; Pendelregel und
Höchstzahl wie AK3. Am Tagesbeginn und an jedem Stundenbeginn steht die Absenkung auf null (Muster
`Raumeinfluss.StundeBeginnen`). **Stärke beim Einschalten** (E106, Q-KK-5 (b)): Der Dialog trägt beim Einschalten der Kühlkurve
einen Vorgabewert ein, den eine Probe in KK3 bestimmt; die Rechnung selbst kennt keine Vorgabe — leer heißt aus (Festlegung 1).

### 2.3 Erzeuger gleitend

Nach E106 (Q-KK-1 (a)) fährt der Kälteerzeuger je Stunde den **kältesten** verlangten Vorlauf der gekoppelten Gebäude (jedes
wärmere Gebäude mischt in seiner Mischgruppe hoch; ein bedarfsgewichteter Vorlauf wie auf der Heizseite ließe das kälteste
Gebäude über seiner Kurve). Untergrenze des Erzeugers: kleinste Stützstelle der Kühlkennlinie der Wärmepumpe bzw.
`Kaltwasser_Vorlauf_Min` der Kältemaschine. Ein Mehrzonengebäude meldet einen Vorlauf — den seines Kühlkreises (2.8).

### 2.4 Kennlinie am Stundenvorlauf

- **Wärmepumpe:** alle Kühlblöcke je Gerät halten und je Stunde und Durchlauf über den Vorlauf interpolieren wie die Heizseite
  (AK3-I, `VorlaufInterpolation`); `Kuehlkennlinie.Bilden` bekommt eine Überladung mit gebrochenem Vorlauf. Außerhalb der
  Stützstellen gilt der Randwert.
- **Kältemaschine:** `Kaltwassertemperatur` wird je Stunde gesetzt statt einmal je Lauf; die zweidimensionale Kennlinie trägt
  die Achse schon.

### 2.5 Kälteschranke und Kältekaskade

**Gewählt: der Vorlauf als Argument** der Kälteschranke (`Kaelteschranke.Angebot(h, vorrang, kuehlVorlaufC)`), durchgereicht an
die vorhandene Kapazitätsnaht `Abfragen(stunde, kuehlVorlaufC)`, die dann auch den Verdichter am Vorlauf wertet. Begründung: Die
Naht nimmt den Vorlauf schon entgegen, die Heizseite geht denselben Weg (`Angebotsfunktion.Angebot(h, v, …)`), und die Schranke
bleibt zustandsfrei. **Nicht gewählt:** die Naht `Kaeltekorrektur` — sie ist die Prüfnaht der Orakel O1k/O2k; mit ihr im Lauf
vermischten sich Physik und Prüfung, und das Orakel verlöre seine unabhängige Kennlinie. Die **Kältekaskade** bekommt in
`StundeRechnen(h)` den Vorlauf der konvergierten Stunde; die freie Kühlung wird gegen diesen Vorlauf geprüft. Weil die Schranke
nun vom Vorlauf abhängt, wird die Kälteseite ein Fixpunkt wie die Heizseite (Abbruchglied ΔS_kälte besteht schon).

### 2.6 Kältespeicher

Der Kältespeicher bleibt an seinem **festen Paar**: Er lädt am eigenen Vorlauf, das Gebäude mischt die Entnahme hoch. Ist ein
Kältespeicher an der Kälteseite, gleitet der Erzeugervorlauf **nie wärmer als der Speichervorlauf** — sonst lüde der Erzeuger
den Speicher nicht mehr. Ein Gleiten des Speicherpaars ist nicht Gegenstand (Abschnitt 9).

### 2.7 Zonensperre und Tagesart

Unverändert: Die Tagesart je Zone entscheidet der unbegrenzte Probetag, ein anderer Kühlvorlauf ändert sie nicht. Heiz- und
Kühlstunden einer Zone fallen nie auf denselben Tag; die beiden Raumeinflüsse iterieren für eine Zone daher kaum gleichzeitig.
Im Mehrzonenweg können verschiedene Zonen am selben Tag heizen und kühlen; dann laufen Heiz- und Kühlkreis des Gebäudes
nebeneinander, und beide Raumeinflüsse können in derselben Stunde nachgeführt werden (2.8).

### 2.8 Mehrzonenweg

Nach E106 (Q-KK-4 (b)) wird die Kälteseite des Mehrzonenwegs gekoppelt. Der Bau hat zwei Teile: die **Voraussetzung KZ1** — die
Kühlübergabe je Zone am festen Vorlauf, der Spiegel von AK1z — und **KZ2**, die Kühlkurve samt Raumeinfluss im Mehrzonenweg.

- **Was gekoppelt wird.** Je Gebäude **ein Kühlkreis** mit gemeinsamem Kühlvorlauf; je gekühlter Zone Schritt H der Kälteseite
  mit der Kühlübergabe der Zone (Art, Exponent, Nennleistung aus `Tab_Zone`, sonst die Werte des Gebäudes bzw. der Anteil der
  Zonenfläche). Auslegungspunkt (`Kuehl_Auslegung_*`), `Kuehl_Vorlaufgrenze` und die Kühlkurve kommen vom Gebäude — wie auf der
  Heizseite Auslegungspunkt und Heizkurve (1.4). Eine Zone mit `IDEAL` oder leerer Art (und leerer Gebäudeart), eine Zone
  ohne wirksame Kühlung und der adiabate Vorlauf der 4-K-Regel rechnen ideal wie heute.
- **Kurvenreihe.** Die Kühlkurve wird einmal je Stunde am **niedrigsten** Kühlsollwert der gekühlten, gekoppelten Zonen
  gerechnet (Spiegel des höchsten Heizsollwerts, `GebaeudeModellEingang.cs:1850-1874`), dieselbe Reihe für jede Zone; eine Zone
  mit „aus“ (θ_max = +∞, KP1) zählt nicht.
- **Führungsgröße.** Überschreitung ü = max über die gekühlten, gekoppelten Zonen in Kühltagesart (θ_air,z − θ_max,z(h)), nach
  unten bei 0 begrenzt, θ_max,z der Kühlsollwert der Zone in dieser Stunde, θ_air,z die Raumluft derselben Stunde; **je Stunde im
  Kreis**, je Durchlauf aus der Lösung derselben Stunde — der Spiegel der Unterschreitung von H2 (`Raumeinfluss.cs:10-13`).
  Die Absenkung k_K·ü gilt für den Kühlkreis des Gebäudes; jede wärmer verlangende Zone mischt hoch.
- **Grenzen.** Wie im Einzonenweg (2.1): unten `Kuehl_Vorlaufgrenze` des Gebäudes und der kälteste Erzeugervorlauf; eine eigene
  Vorlaufgrenze je Zone gibt es nicht (Festlegung 15).
- **Zonensperre.** Unverändert je Zone (2.7). Eine Zone in Heiztagesart nimmt am Kühlkreis nicht teil; ein Tag mit Heiz- und
  Kühlzonen führt beide Kreise. Die reversible Wärmepumpe bleibt je Tag projektweit eine Seite (K8a); die andere Seite
  begrenzt die Kälteschranke mit dem Grund „Umschaltung“ wie heute.
- **Kälteschranke je Gebäude, verteilt je Zone.** Die Schranke bleibt eine Größe der Erzeuger; der Kreis verteilt sie wie
  heute nach dem unbegrenzten Kühlbedarf je Zone (`Anlagenkopplung.cs:374-405`). Neu wirkt mit der Kühlübergabe auch das
  Vorlaufangebot der Schranke in jeder Zone (`Stundenrand.cs:99-111`).
- **Stufe.** Kühlkurve und Raumeinfluss nur auf AK3 (E106, Q-KK-2 (a)). Die Kühlübergabe je Zone (KZ1) nach **Q-KK-7 (a)**:
  ab AK1 wie im Einzonenweg (E106); die Warnung „Kühlung je Zone ideal“ entfällt.
- **Meldung.** `SIMENG_G6_AK1_IDEAL` entfällt, wo die Kälteseite gekoppelt rechnet; sie bleibt (umformuliert) für die Stufen,
  auf denen sie nach Q-KK-7 ideal bleibt.
- **Rechenzeit.** Jeder Durchlauf des Kreises rechnet die Zonenschleife; die Schutzgrenze Zonen × Durchläufe ≤ 120 begrenzt
  zwei Zonen auf 60 Durchläufe, die Höchstzahl 20 greift vorher. Geschätzt +10–20 % gegenüber demselben Gebäude ungekoppelt
  gekühlt, an Tagen mit Heiz- und Kühlzonen am meisten; gemessen in KZ2.

### 2.9 Rechenzeit

Geschätzt, nicht gemessen (L1): Kurve allein ±0 (Jahresreihe vorab); mit Raumeinfluss am Gebäude in den rund 590 Kühlstunden
von 1058 +1 bis +2 Durchläufe → Mittel ≈ 3,3; mit gleitendem Erzeuger +2 bis +3 in Kühlstunden, Höchstwert 8–10 möglich →
Mittel ≈ 3,4–3,5 (+6–9 %). Die Höchstzahl 20 bleibt fern; gemessen wird in KK3, der Mehrzonenweg in KZ2 (2.8).

## 3. Festlegungen (benannt, Widerspruch möglich)

Was ohne weiteren Anwenderentscheid festgelegt wird; Widerspruch ist möglich, bis die zugehörige Welle beauftragt ist.

1. **Leere Spalte = fester Vorlauf wie heute.** `Kuehlkurve_Aktiv` 0 nimmt den heutigen Skalarpfad wörtlich; ein Raumeinfluss
   mit NULL oder 0 baut kein Objekt (Muster `Raumeinfluss.AusGebaeuden`, Schritt 198) — die Basis bleibt byte-gleich.
2. **Kurvenform:** Zwei-Punkt-Kurve mit Fußpunkt (2.1), keine gespiegelte Heizkurve (WK4), kein Exponent.
3. **Auslegungs-Außentemperatur der Kühlung** nach einem von drei Wegen, wählbar je Gebäude in `Kuehlkurve_Auslegung_Weg`
   (E107, „1, 2, 3 wählbar, Default 2“): 1 `stunde` — die höchste Stundentemperatur der Klimareihe; 2 `tagesmittel` — die
   **Vorgabe**, auch bei leerer Spalte — das wärmste Tagesmittel (Tag der Auslegungskühllast), mindestens Kühlsollwert +
   Mindestspanne `KUEHLKURVE_AUSLEGUNG_SPANNE_K` = 8 K (Festwert in `GebaeudeFestwerte`, kein Normwert; gemessen über die
   Klimaregionen der Testdatenbank: mitteleuropäisch wärmstes Tagesmittel 24,4–27,8 °C, höchste Stunde 30,1–34,5 °C — bei
   Sollwerten 24–26 °C landet der Punkt bei 32–34 °C, nahe den höchsten Stunden); 3 `eingabe` — die Eingabe
   `Kuehlkurve_Auslegung_Aussen` am Gebäude; fehlt sie oder liegt sie nicht über Kühlsollwert + 1 K
   (`KUEHLKURVE_AUSLEGUNG_EINGABE_ABSTAND_K`), gilt Weg 2 mit Laufhinweis. Ein unbekannter Weg bricht den Lauf des Gebäudes ab.
   Befund KK1: Das wärmste Tagesmittel allein lag oft unter dem Kühlsollwert, dann sprang die Kurve am Sollwert.
4. **Fußpunkt leer** bei aktiver Kurve: Vorgabe ist der Auslegungsrücklauf der Kühlübergabe (`Kuehl_Auslegung_Ruecklauf`
   bzw. dessen Vorgabe je Art); der Mindestabstand zum Kühlsollwert ist ein Festwert in `GebaeudeFestwerte`, bestimmt per
   Probe in KK1.
5. **Untergrenze** des Gebäudevorlaufs ist immer `Kuehl_Vorlaufgrenze`; die Taupunktgrenze bleibt eine Vorgabe, der Grund
   `VORLAUFGRENZE_KUEHLUNG` bleibt an die gesättigte Übergabe an der Grenze gebunden.
6. **Raumeinfluss nur mit `Kuehlkurve_Aktiv`** und Kühlübergabe (Muster H2: Raumeinfluss nur mit `Heizkurve_Aktiv`); der
   Vorgabewert beim Einschalten ist ein Dialogwert, kein Rechenwert (E106, Q-KK-5 (b); 2.2).
7. **Bereiche:** `Kuehlkurve_Raumeinfluss` 0–10 K/K wie `Heizkurve_Raumeinfluss`; `Kuehlkurve_Fusspunkt` im Bereich der
   Vorlaufgrenze (4–22 °C).
8. **Abbruchmaß** |ΔK2| ≤ 0,05 K (gleiches Maß wie ΔV und ΔH2); Pendelregel und Höchstzahl wie AK3.
9. **Mehrere Gebäude:** der Erzeuger fährt den kältesten verlangten Vorlauf (2.3); ein Mehrzonengebäude zählt mit dem Vorlauf
   seines Kühlkreises.
10. **Kältespeicher** bleibt am festen Paar; der Erzeugervorlauf gleitet nie wärmer als der Speichervorlauf (2.6).
11. **Kälteschranke** am Stundenvorlauf über das Vorlauf-Argument; `Kaeltekorrektur` bleibt Prüfnaht (2.5).
12. **Ergebnisspalten** an `Tab_ErgebnisEnergiebedarf` (S1, NULL außerhalb des Geltungsbereichs): mittlerer Kühlvorlauf der
    Kühlstunden, Summe der Absenkung [Kh], Stunden an der Vorlaufgrenze — je Gebäude, auch im Mehrzonenweg; die Stundenreihe
    `kuehlvorlauf_0.csv` trägt den gleitenden Vorlauf.
13. **Kernschalter bis zur Basiswelle:** Bis KK5 rechnen Kurve, Erzeuger am Stundenvorlauf und die gekoppelte Kälteseite des
    Mehrzonenwegs (KZ1, KZ2) nur mit einem Kernschalter (Vorgabe aus, Muster W-I); jedes Gate bis dahin ist „Referenzlauf 24/24
    byte-gleich gegen R43“.
14. **Mehrzonenweg gekoppelt** (E106, Q-KK-4 (b)): je Gebäude ein Kühlkreis mit gemeinsamem Vorlauf, Kühlübergabe je Zone,
    Kurvenreihe am niedrigsten Kühlsollwert der gekühlten Zonen, Führungsgröße die Zone mit der größten Überschreitung ihres
    Kühlsollwerts, je Stunde im Kreis (2.8). Die Stufe der Kühlübergabe je Zone entscheidet Q-KK-7.
15. **Keine neue Eingabespalte an der Zone:** Die Kühlübergabe je Zone nimmt die drei Spalten aus Schritt 137; Auslegungspunkt,
    Vorlaufgrenze und Kühlkurve stehen allein am Gebäude. S1 bleibt bei fünf Eingabespalten an `Tab_Gebaeude` und
    `Tab_Gebaeude_STAMM`; die Kopfzeile von Schritt 202 lässt Zonenspalten zu, dieser Entwurf braucht keine.
16. **Ideale Zonen** im Mehrzonenweg wie auf der Heizseite: `IDEAL` oder leere Art bei leerer Gebäudeart, Zone ohne wirksame
    Kühlung, adiabater Vorlauf der 4-K-Regel (`ZonenEingang.cs:51-56`).
17. **Kälteschranke im Mehrzonenweg** bleibt je Gebäude und wird nach dem unbegrenzten Kühlbedarf je Zone verteilt (2.8).
18. **Fußpunktregel** (E107, offener Punkt aus KK1): Ist der Fußpunkt kälter als der Auslegungsvorlauf, stiege der Vorlauf mit
    der Außentemperatur; der Kern klemmt den Fußpunkt auf den Auslegungsvorlauf (waagrechte Kurve) und meldet es je Gebäude und
    Lauf einmal. Die Eingabeprüfung im Dialog kommt mit KK4.

## 4. Referenzprojekte und Basis

- **RP-KK** (E106, Q-KK-6 (a)): Kopie von 1058 auf dem Kopierweg des Programms (Skriptmuster
  `Referenzlaeufe/Skripte/referenzprojekt_1058_ak3.py`) mit gesetzter Kühlkurve (Fußpunkt über dem Vorlauf von 1058,
  Raumeinfluss gesetzt), damit die Wärmepumpe an milden Kühltagen wärmer und an heißen kälter fährt. Einzonenweg, **nicht in der
  CI-Auswahl.**
- **RP-KKZ** (E106, Q-KK-6 erweitert): **Kopie von RP-KK, das Gebäude über die Wege des Zonendialogs in zwei Zonen geteilt**
  (Skriptmuster `Referenzlaeufe/Skripte/referenzprojekt_1052_zonen.cs` samt Bauplan: „Gebäude als eine Zone übernehmen“ und der
  OK-Weg der Zonen), die Zonen so geschnitten, dass die führende Zone wechselt — eine mit hohem Fensteranteil nach Süden und
  Westen, eine nach Norden mit geringeren Lasten —, an einer Zone eine andere Kühlübergabeart als am Gebäude (etwa
  `GEBLAESEKONVEKTOR` neben der Kühldecke des Gebäudes). Mehrzonenweg auf AK3, **nicht in der CI-Auswahl.**
  **Begründung der Grundlage** (der Weg mit den wenigsten neuen Einfrierdaten):
  - Kopie von RP-KK: Anlagenseite (Stufe AK3, Wärmepumpe mit Kühlkennlinie, Heizungspuffer, Kaskade, Fahrplan, Kühlkurve am
    Gebäude) kommt unverändert mit; neu sind allein die Zonendaten (bestehende Einfrierregel „gesäte Zonendaten“) und bis zu drei
    Zonenzellen der Kühlübergabe.
  - Kopie von 1054 (Zonen mit Heizkreis, AK1): brächte die Zonen, aber neu wären Kühlbetrieb, Kühleingaben, Kühlübergabe,
    Kälteerzeuger samt Kühlkennlinie, Stufe AK3, Heizungspuffer und Kühlkurve — sechs Einfrierbereiche statt einem.
  - Kopie von 1059 (Kältemaschine mit Kältespeicher): die Speicherregel (2.6) begrenzt das Gleiten; der Mehrzonenweg wäre gehalten,
    die Kühlkurve kaum.
  Folge der Wahl: Mit der reversiblen Wärmepumpe von 1058 trifft RP-KKZ an Tagen mit Heiz- und Kühlzonen die Umschaltung je Tag
  (K8a); die Heizseite decken dann BHKW und Elektrokessel. Das ist gewollt — es hält den Grund „Umschaltung“ im Mehrzonenweg —
  und steht unter den Risiken.
- **Nummern:** Beide Projektnummern werden beim Bau nach den Kopfzeilen der Statusdatei gewählt und angemeldet (1060 ist
  vorgemerkt).
- **Basis R44** wird beim Bau angemeldet: abweichend allein RP-KK und RP-KKZ (neu); die vierundzwanzig Projekte von R43 bleiben
  byte-gleich, weil keines die Spalten setzt und keines ein Mehrzonengebäude mit Kühlung gekoppelt rechnet (1052 und 1054 kühlen
  nicht). Die Testdatenbank bekommt Schritt S1 und die beiden Projekte (LFS-Commit).
- **Einfrierregeln (Vorschlag im Wortlaut, mit KK5 in `CLAUDE.md` und `Referenzlaeufe/LIESMICH.md`):**
  - in „gesäte Auslegungsdaten der Übergabe“ nach `Kuehl_Vorlaufgrenze` ergänzt: „die Kühlkurve am Gebäude
    (`Kuehlkurve_Aktiv`, `Kuehlkurve_Fusspunkt`, `Kuehlkurve_Raumeinfluss`, `Kuehlkurve_Auslegung_Weg`,
    `Kuehlkurve_Auslegung_Aussen`)“;
  - in „gesäte Zonendaten eines Referenzprojekts“ nach den Luftströmen ergänzt: „an seinen Zonen die drei Kühlübergabespalten
    von `Tab_Zone` (`Kuehl_Uebergabe_Art`, `Kuehl_Uebergabe_Exponent`, `Kuehl_Uebergabe_Leistung_Nenn`)“;
  - neu: „gesäte Kühlkurvendaten eines Referenzprojekts mit Kühlkurve: die Kühlkennlinie seiner Kälteerzeuger samt aller
    Vorlauf-Stützstellen (`Tab_Kenndaten_Kuehlung`, `Tab_Kenndaten_Kaeltemaschine`), `Kaltwasser_Vorlauf_Min`, das
    Temperaturpaar seines Kältespeichers, die Festwerte der Kühlkurve in `GebaeudeFestwerte` (Mindestabstand zum Kühlsollwert,
    Mindestspanne der Auslegungs-Außentemperatur über dem Kühlsollwert, Abstand der Eingabe über dem Kühlsollwert, Bereiche) und das Abbruchmaß ΔK2, dazu das Anlegen oder Entfernen eines Referenzprojekts mit Kühlkurve, auch eines
    Mehrzonen-Referenzprojekts mit gekoppelter Kälteseite“.
- **Tests und Wachen:** neu `KuehlkurveTests` (Form, Fußpunkt, Grenzen, „aus bitgleich“), Orakel **O1kk/O2kk** nach Muster
  O1k/O2k (stetige Kälteleistung über den Vorlauf, Fixpunkt eindeutig; Absenkung mit Kappung), `KuehlkurveSchemaTests`
  (Spalten, Sicht, vier Kopierwege NULL-erhaltend, Wiederholprobe), bunit des Dialogfelds, `KuehlkurveReferenzprojektWacheTests`;
  für den Mehrzonenweg neu `ZonenKuehluebergabeTests` (Kühlübergabe je Zone, Rückfall auf das Gebäude, ideale Zonen, „ohne
  Kühlübergabe bitgleich“), Orakel **O3kz** (Führungsgröße: größte Überschreitung, Zonen mit „aus“ ausgenommen),
  bunit der Kühlübergabefelder im Zonendialog, `KuehlkurveZonenReferenzprojektWacheTests`; mitzuziehen
  `Ak3ReferenzprojektWacheTests`, `Ak3KReferenzprojektWacheTests` (unverändert grün), `AnlagenkopplungOrakelTests`,
  `AnlagenkopplungZonenmodellTests`, `ZonenHeizkreisReferenzprojektWacheTests` (unverändert grün), `KaelteerzeugerTests`
  (Kennlinie am festen Vorlauf bitgleich), `GebaeudeRundlaufTests`; dazu `SqlDialektPruefer` und `designer_neu.py`.
  **CI-Auswahl unverändert.**

## 5. Was KK nicht verändert

Die Heizseite samt H2 (auch im Mehrzonenweg), die Zonensperre und die Tagesart, die Umschaltung der Wärmepumpe je Tag (K8a), die
Kühlkurve auf AK1 und AK2 (E106, Q-KK-2 (a)), den Tagesbilanz-Weg und alle Projekte ohne gesetzte Kühlkurve und ohne gekoppeltes
Mehrzonengebäude mit Kühlung.

## 6. Wellenplan

Je Welle ein Opus-Auftrag mit höchstens rund 150 Werkzeugaufrufen; das Gate fährt die Orchestrierung nach dem Merge. Bis KK5 ist
jedes Gate „Referenzlauf 24/24 byte-gleich gegen R43“. Schemaschritt: allein **S1 = 202** in KK4 (angemeldet).

| Welle | Inhalt | Abnahme | Basis | PT |
|---|---|---|---|---|
| **KK0** Papiere | E106 eintragen; Anlagenkopplung 7.1, 7.4 Nr. 5, Kühlkonzept K21 und E37-Verweis nachziehen (WK1–WK3); Mehrzonenweg gelesen (1.4), Entwurf auf E106 | Registerzeile; `DokumentationLinkWacheTests` grün | nein | 0,5–1 |
| **KK1** Kurve als Reihe | Kernschalter; `KuehlVorlaufC` als Jahresreihe, Klasse `Kuehlkurve` (2.1), Rand liest `[h]`; Festwert Mindestabstand per Probe | `KuehlkurveTests`; „Reihe konstant = heute bitgleich“; Referenzlauf 24/24 byte-gleich | nein | 1–1,5 |
| **KK2** Erzeuger am Stundenvorlauf | Kühlkennlinie mit gebrochenem Vorlauf, alle Blöcke je Gerät; Kältemaschine je Stunde; Vorlauf-Argument der Kälteschranke; Kältekaskade am Stundenvorlauf; kältester verlangter Vorlauf; Speicherregel | Orakel O1kk; „fester Vorlauf bitgleich“; Kälteklassen grün; Referenzlauf byte-gleich | nein | 2,5–3,5 |
| **KK3** Raumeinfluss im Kreis | Spiegel von `Raumeinfluss`, Nachführung in `Anlagenkopplung`, Abbruchglied ΔK2, Pendelregel; Rechenzeit messen; Vorgabewert der Stärke per Probe (Q-KK-5 (b)) | Orakel O2kk; Durchläufe gemessen; Vorgabewert benannt; Referenzlauf byte-gleich | nein | 2–3 |
| **KZ1** Kühlübergabe je Zone | Voraussetzung des Mehrzonenwegs am festen Vorlauf: `Zoneneingaben` um die drei Kühlspalten; Kühlkreis je Gebäude als Spiegel von `ZonenkopplungAufloesen`; `KuehlKopplungWirksam` im Mehrzonenweg frei (Stufe nach Q-KK-7); Kühlübergabe je Zone im Rand, Vorlaufangebot der Kälteschranke je Zone; Meldung `SIMENG_G6_AK1_IDEAL` neu gefasst (beide Sprachen); hinter dem Kernschalter | `ZonenKuehluebergabeTests`; „ohne Kühlübergabe bitgleich“; `AnlagenkopplungZonenmodellTests` grün; Referenzlauf byte-gleich | nein | 2–3 |
| **KK4** Schema, Oberfläche | S1 (fünf Eingabespalten an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` — `Kuehlkurve_Aktiv`, `Kuehlkurve_Fusspunkt`, `Kuehlkurve_Raumeinfluss`, dazu nach E107 `Kuehlkurve_Auslegung_Weg` (`TEXT CHECK IN ('stunde','tagesmittel','eingabe')`, leer = `tagesmittel`) und `Kuehlkurve_Auslegung_Aussen`; Sicht `Abfrage_Projektgebaeude`, Kopierwege, drei Ergebnisspalten); Modelle, Controller, Katalog, Export, Hülle, Feld in `GebaeudeKuehluebergabeFelder`, Herleitung, Vorgabewert beim Einschalten; im Zonendialog die drei Kühlübergabefelder der Zone (Spalten bestehen); Ressourcen beider Sprachen | Schema- und bunit-Tests; `SqlDialektPruefer`; `designer_neu.py`; Windows-Schale auf Linux kompiliert | nein | 2,5–3,5 |
| **KZ2** Kühlkurve im Mehrzonenweg | Kurvenreihe am niedrigsten Kühlsollwert der gekühlten Zonen; Führungsgröße größte Überschreitung im Kreis; Tage mit Heiz- und Kühlzonen; Rechenzeit des Mehrzonenwegs messen | Orakel O3kz; Durchläufe gemessen; Referenzlauf byte-gleich | nein | 1,5–2 |
| **KK5** Referenzprojekte und R44 | Kernschalter entfernt; RP-KK und RP-KKZ (Skripte, Wachen); Einfrierregeln; Basis R44 | allein RP-KK und RP-KKZ neu, übrige 24 byte-gleich; CI-Auswahl unverändert | **ja** | 2,5–3 |
| **KK6** Papiere und Wiki | Konzepte wie gebaut, Register, Status, Protokoll, Wiki-Quellen (Gebäudemodell, Kühlung, Zonen), Logbuch-Entwurf | Link-Wache, Wiki-Gegenlesemuster | nein | 1–1,5 |
| | **Summe** | | | **15,5–22** |

**Reihenfolge und Abhängigkeiten:** KK1 und KK2 hängen an verschiedenen Dateien und können nebeneinander laufen; KZ1 beginnt nach
KK1 (beide im Eingangsbauer `GebaeudeModellEingang.cs`) und kann neben KK2 und KK3 laufen; KK3 setzt KK1 und KK2 voraus; KK4 kann
nach KK1 beginnen, die Zonenfelder nach KZ1; KZ2 setzt KK3 und KZ1 voraus; KK5 setzt KK3, KK4 und KZ2 voraus. **Bei Antwort (b)
auf Q-KK-7** (Kühlübergabe je Zone nur AK3) entfallen die AK1/AK2-Proben in KZ1 (−0,5 PT). Fällt der Mehrzonenweg zeitlich
zurück, kann KK5 allein mit RP-KK einfrieren; RP-KKZ folgt dann mit eigener Basis (R45) nach KZ2 — die Orchestrierung entscheidet.

### 6.1 Warum 15,5–22 PT statt +1–1,5 PT

Die Skizze in Q-AK3K-3 (a) dachte an eine Spalte und eine gespiegelte Absenkung am Gebäude **bei festem Erzeuger und fester
Schranke** — eine Kopie des H2-Pfads. Sie enthielt nicht: die Kurvenform mit Fußpunkt (die gespiegelte Heizkurve schaltet ab),
den gleitenden Erzeuger mit Kennlinie am Stundenvorlauf für Wärmepumpe und Kältemaschine (ohne ihn wirkt die Kurve nicht, 1.2),
die vorlaufabhängige Kälteschranke und damit den Fixpunkt der Kälteseite samt Orakeln, die Speicherregel, rund zwei Dutzend
Leserstellen einer Gebäudespalte (Schema, Sicht, vier Kopierwege, Modelle, Katalog, Export, Hülle, Dialog), das Referenzprojekt
mit Basis und die Papiere. L2 schätzte 8–12 PT ohne eigene Kurvenwelle und ohne die Kältemaschine am Stundenvorlauf; der
Entwurfsstand vor E106 nahm beides auf (10–15 PT). **Der Mehrzonenweg (E106, Q-KK-4 (b)) kommt mit +5,5–7 PT dazu** — KZ1
2–3, KZ2 1,5–2, Zonenfelder im Dialog 0,5, RP-KKZ 1, Wiki 0,5 —, etwas über den +4–6 PT der Option, weil der Zonendialog keine
Kühlübergabefelder hat und RP-KKZ das erste Referenzprojekt ist, das AK3 im Mehrzonenweg hält.

### 6.2 Wie gebaut

Alle Wellen KK0 bis KK6 samt KZ1 und KZ2 sind gebaut; der Kernschalter ist mit KK5a entfallen, die Kühlkurve wirkt auf Stufe
AK3 überall, wo `Kuehlkurve_Aktiv` gesetzt ist, die Kühlübergabe je Zone rechnet im Mehrzonenweg ab AK1. Basis **R44**
`2026-10-08_R44_Kuehlkurve` (sechsundzwanzig Projekte, neu **1061** = RP-KK und **1062** = RP-KKZ, beide nicht in der
CI-Auswahl; die übrigen vierundzwanzig byte-gleich zu R43).

| Welle | Commits | Gebaut, Befunde und Zahlen |
|---|---|---|
| **KK0** | (Papiere) | E106 eingetragen, Anlagenkopplung 7.1, 7.4 Nr. 5 und Kühlkonzept K21 nachgezogen, Mehrzonenweg gelesen (1.4) |
| **KK1** | `4b6b63597`, `8d91b253b`, `1012a7af2`, `bc0ca883a` | Klasse `Kuehlkurve` (Zwei-Punkt-Kurve, Fußpunkt bis zum Kühlsollwert der Stunde, nie NaN); `KuehlVorlaufC` als Jahresreihe, der feste Vorlauf lebt als `KuehlVorlaufFestC`; Rand liest `[h]`. Mindestabstand **2,5 K** per Probe: schwächster Fall Konvektor bei 30 °C, 2,0 K → 8,7 %, 2,5 K → 10,9 % der Nennleistung. `KuehlkurveTests` 7 Proben |
| **KK1b** | `90c2175fe`, `0e1bfe747`, `0bbfdd953` | **E107:** Auslegungsweg wählbar (`stunde`, `tagesmittel` — Vorgabe —, `eingabe`); Mindestspanne **8 K** über dem Kühlsollwert, gemessen an acht Klimaregionen der Testdatenbank (wärmstes Tagesmittel 24,4–27,8 °C, höchste Stunde 30,1–34,5 °C); Weg 3 unter Sollwert + 1 K oder ohne Eingabe fällt mit Hinweis auf Weg 2 zurück; Fußpunktregel (Festlegung 18) klemmt einen zu kalten Fußpunkt auf den Auslegungsvorlauf. `KuehlkurveTests` 11/11 |
| **KK2** | `07e441319`, `4582e6b84`, `03657f4e7` | Kühlkennlinie als Schar aller Blöcke, ausgewertet am gebrochenen Vorlauf; Kältemaschine je Stunde mit Untergrenze `Kaltwasser_Vorlauf_Min`; Kälteschranke mit Vorlauf-Argument, Speicherregel, kältester verlangter Vorlauf; Kältekaskade am Stundenvorlauf. Fester Vorlauf bitgleich; Orakel **O1kk** trifft je Stunde die Bisektionslösung |
| **KK3** | `4ed08d2fb`, `a8c4a99fd`, `ff2941f5c`, `565de0c16` | Raumeinfluss im Kreis (`KuehlRaumeinfluss`, Abbruchglied ΔK2 ≤ 0,05 K, Pendelregel), Erzeuger gleitet, Untergrenze am kältesten erreichbaren Erzeugervorlauf. **O2kk:** \|Δ\| ≤ 0,0003 K, Kappung an 16 °C in 33 von 196 Stunden, höchstens 7 Durchläufe. Vorgabewert **3 K/K** per Messreihe an 1058 (fester Vorlauf 34 h / 45,9 Kh, EER 5,238; k_K 3 32 h / 45,1 Kh, EER 5,089; darüber sättigt der Komfortgewinn). **Befund:** an 1058 liegt der Vorlauf auf der oberen Stützstelle, der Erzeuger kann nicht wärmer gleiten — deshalb rechnet RP-KK mit einem Kühlvorlauf von 12 °C |
| **KZ1** | `9a78570c7`, `da5502b1f`, `9f3c43848` | Kühlübergabe je Zone im Mehrzonenweg (Spalten aus Schritt 137, Kühlkreis je Gebäude, Rücklauf massenstromgewichtet, Kälteschranke je Zone); Meldung `SIMENG_G6_AK1_IDEAL` neu gefasst. **Befund:** `Zonenmodell2K` hält Schritt K im festen Muster nicht — Stunden mit Kühlübergabe je Zone rechnen in der Gauß-Seidel-Schleife frei. `ZonenKuehluebergabeTests` 15 |
| **KK4** | `391d426ba`, `fae5af820`, `7d299d455`, `6b3617bc3`, `ed3a99ebb`, `0c56775fb`, `5b78ba026` | **Schemaschritt 202:** fünf Eingabespalten an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`, Sicht `Abfrage_Projektgebaeude` (110 Spalten), drei Ergebnisspalten als **Projektkennzahlen** an `Tab_ErgebnisEnergiebedarf`; Testdatenbank gehoben (Basis R43 blieb). Kopierwege und Katalog NULL-erhaltend, Katalogübernahme 96 → 101 Fachspalten; IFC- und gbXML-Export benennen die Spalten als Verlust. Gebäudedialog: Haken, Fußpunkt, Raumeinfluss, Auslegungsweg, Außentemperatur (Bereich 0–60 °C) nur mit Weg Eingabe; Zonendialog: Kühlübergabe je Zone (Eingabebilanz 19 → 22) |
| **KZ2** | `62da93155`, `38808ad3c`, `0a90ad33b` | Kühlkurve im Mehrzonenweg am niedrigsten Kühlsollwert der gekühlten, gekoppelten Zonen; Führungsgröße die größte Überschreitung. **Befund:** das Vorlaufangebot der Kälteschranke kappte den abgesenkten Vorlauf auf den des vorigen Durchlaufs; am gleitenden Erzeuger gilt jetzt der verlangte Vorlauf dieses Durchlaufs. **O3kz:** \|Δ\| ≤ 0,0004 K, höchstens 11 Durchläufe, 1317 Stunden mit Heiz- und Kühlzone. Feldlauf 1058 zweizonig: 594 Kühlstunden, Vorlauf Mittel 17,53 °C, Durchläufe Mittel 3,320 / max 7 |
| **KK5a** | `3b3cb6ec8`, `d38dd1295`, `fd98d2581`, `0e08df6f8`, `7856be854` | Kernschalter und Probenaht entfallen; der Lauf schreibt die drei Kennzahlen; ein Einzonengebäude allein mit gekoppelter Kälteseite kommt in den Kreis. **RP-KK 1061** (Kopie von 1058, Kühlkurve mit 3 K/K, Weg Tagesmittel, Kühlvorlauf der Wärmepumpe 12 °C): EER fest 4,46 → mit Kurve 5,09, Überschreitung 42 → 32 h. **RP-KKZ 1062** (Kopie von 1061, zwei Zonen Süd/West und Nord/Ost, Nord/Ost mit Gebläsekonvektor). Wachen `KuehlkurveReferenzprojektWacheTests`, `ZonenKuehlkurveReferenzprojektWacheTests` |
| **KK5b** | (Basis) | Basis R44 eingefroren, Einfrierregeln um die Kühlkurve erweitert |
| **KK6** | (Papiere) | Konzepte, Register, Wiki-Quellen, Logbuch-Entwürfe, Protokoll |

**Abweichungen vom Entwurf:** (a) Die Ergebnisspalten hängen als **Projektkennzahlen** am Projektergebnis, nicht am Gebäude
(Muster AK3 und AK3-K). (b) Der Mehrzonenweg brauchte die freie Rechnung der Kühlübergabestunden (KZ1) und die Kappungsregel
am gleitenden Erzeuger (KZ2). (c) Der Kreis nimmt auch ein Einzonengebäude allein mit gekoppelter Kälteseite auf (KK5a).
(d) Der Vorgabewert der Stärke ist 3 K/K statt des vorläufigen 1 K/K im Dialog von KK4.

**Offene Punkte:**

- **Führende Zone ohne Kennzahl:** Welche Zone den Raumeinfluss im Mehrzonenweg führt, weist das Ergebnis nicht aus.
- **Rechenzeitreihe des Mehrzonenwegs entfallen:** Die Messreihe hing an der Probenaht (`ZonenKuehlkurveFeldlaufTests`) und
  ist mit ihr in KK5a entfallen; gemessen bleibt der Feldlauf aus KZ2.
- **Werkzeug Testdatenbankschema:** Ein zweiter Lauf meldet Schritt 202 als offen, obwohl die Testdatenbank ihn trägt.
- **Sichtabnahme** der Dialogfelder unter Windows.

## 7. Fragen an den Anwender

Q-KK-1 bis Q-KK-7 sind mit **E106** entschieden (Anwender, 08.10.2026; Q-KK-7 nachgereicht).

| # | Frage | Optionen und Folgen | Empfehlung | Entschieden |
|---|---|---|---|---|
| **Q-KK-1** | **Gleitet nur das Gebäude oder auch der Erzeuger?** | (a) Gebäude und Erzeuger: kältester verlangter Vorlauf, Wärmepumpe und Kältemaschine am Stundenvorlauf, bessere Leistungszahl an milden Kühltagen · (b) nur Gebäude, Erzeuger fest: Kurve kann den Vorlauf nur anheben, kein Gewinn beim Erzeuger, −2–3 PT · (c) nur die Wärmepumpe gleitet, Kältemaschine fest: −0,5 PT, Projekte mit Kältemaschine ohne Wirkung | (a) | **E106: (a)** |
| **Q-KK-2** | **Auf welchen Stufen gilt die Kühlkurve?** | (a) Kurve und Raumeinfluss nur AK3 (wie E104, Q-AK3K-5) · (b) außentemperaturgeführte Kurve ab AK1, Raumeinfluss nur AK3 (wie die Heizseite): Kaskade auf AK1/AK2 nachträglich mit Vorlaufreihe, +1–1,5 PT, 1047 und 1056 bleiben ohne gesetzte Werte byte-gleich | (a) | **E106: (a)** |
| **Q-KK-3** | **Woraus besteht die Führung?** | (a) Kurve über die Außentemperatur mit Fußpunkt plus Absenkung nach der Raumtemperatur (zwei Bausteine wie Heizkurve und H2) · (b) nur Absenkung vom Fußpunkt nach der Raumtemperatur, ohne Außentemperaturglied: eine Spalte weniger, der Kreis trägt die ganze Führung, mehr Durchläufe · (c) nur Kurve über die Außentemperatur ohne Raumeinfluss: explizit, kein Fixpunkt, aber nicht „raumgeführt“ | (a) | **E106: (a)** |
| **Q-KK-4** | **Gehört der Mehrzonenweg dazu?** | (a) nein, benannt abgelehnt mit Meldung, fester Vorlauf · (b) ja: die Kälteseite im Mehrzonenweg wird gekoppelt (Nachzug G6), Führungsgröße die Zone mit der größten Überschreitung, +4–6 PT, eigener Gegenstand | (a) | **E106: (b)**, abweichend von der Empfehlung — 2.8, KZ1 und KZ2 |
| **Q-KK-5** | **Welche Stärke gilt beim Einschalten des Raumeinflusses?** | (a) keine Vorgabe: leer heißt aus, der Anwender trägt den Wert ein · (b) Vorgabewert im Dialog beim Einschalten, per Probe in KK3 bestimmt · (c) der Wert von `Heizkurve_Raumeinfluss` desselben Gebäudes | (b) | **E106: (b)** |
| **Q-KK-6** | **Welches Referenzprojekt hält die Kühlkurve?** | (a) neu RP-KK als Kopie von 1058 (Wärmepumpe mit Kühlkennlinie), nicht in der CI; R44 bewegt kein bestehendes Projekt · (b) neu als Kopie von 1059 (Kältemaschine mit Kältespeicher): die Speicherregel begrenzt das Gleiten, wenig Wirkung · (c) 1058 selbst umstellen: bewegt ein CI-Projekt, Basiswechsel für die CI · (d) 1059 selbst umstellen: die reine Schrankenmessung von AK3-K geht verloren | (a) | **E106: (a), erweitert** um ein Mehrzonen-Referenzprojekt mit Kühlung auf AK3 (Grundlage wählt der Entwurf: RP-KKZ, Abschnitt 4); beide nicht in der CI |
| **Q-KK-7** | **Ab welcher Stufe gilt die Kühlübergabe je Zone im Mehrzonenweg?** Q-KK-4 (b) setzt sie voraus (1.4); sie ist gebäudeseitig und im Einzonenweg ab AK1 wirksam, Q-KK-2 (a) beschränkt aber nur die Kühlkurve | (a) ab AK1 wie im Einzonenweg und wie die Heizseite des Mehrzonenwegs (AK1z): eine Regel für beide Wege, die Warnung „Kühlung je Zone ideal“ entfällt ganz; kein Referenzprojekt bewegt sich (keines rechnet ein Mehrzonengebäude mit Kühlung gekoppelt) · (b) nur AK3: auf AK1/AK2 bleibt der Mehrzonenweg ideal mit Warnung, −0,5 PT, aber eine Stufenausnahme allein im Mehrzonenweg | (a) | **entschieden (a)**, E106 |

## 8. Risiken

| Risiko | Wirkung | Gegenmittel |
|---|---|---|
| **Kennlinienrand** (Stützstellen ganzzahlig, oft nur zwei) | Vorlauf außerhalb nimmt den Randwert; Pendeln zwischen Stützstellen | Randwert benannt; Pendelregel wie `Anlagenkopplung.cs:503-519` |
| Fixpunkt der Kälteseite konvergiert schlechter als H2 | mehr Durchläufe, in Schrankenstunden am meisten | Orakel O1kk/O2kk; Messung in KK3; Höchstzahl 20 |
| Fußpunkt zu nah am Kühlsollwert | Übergabeleistung null, Raum überhitzt still | Mindestabstand (Festlegung 4); Prüfung im Dialog |
| Raumeinfluss senkt unter die Taupunktvorgabe | Kondensat in der Wirklichkeit | harte Untergrenze `Kuehl_Vorlaufgrenze` (Festlegung 5) |
| Kältespeicher und gleitender Erzeuger | Speicher wird nicht geladen | Regel 2.6 (Festlegung 10) |
| Leser des festen Kühlvorlaufs übersehen (`VorlaufFestC`, Kaskade, Bericht) | still falscher Vorlauf in Ergebnis oder Bericht | Leserinventar in KK1 und KK2; Probe „Reihe konstant = heute“ |
| **AK3 im Mehrzonenweg bisher ungehalten** (kein Referenzprojekt, 1.4) | Fehler im Kreis mit Zonenschleife zeigen sich erst mit RP-KKZ | Orakel O3kz und `AnlagenkopplungZonenmodellTests` in KZ1/KZ2; RP-KKZ zuerst ohne Kühlkurve gegen den festen Vorlauf prüfen |
| Heiz- und Kühlkreis eines Gebäudes in derselben Stunde, zwei Raumeinflüsse | mehr Durchläufe, Pendeln zwischen beiden Seiten | Abbruchglieder ΔH2 und ΔK2 getrennt; Messung in KZ2; Schutzgrenze 120 |
| Umschaltung der reversiblen Wärmepumpe in RP-KKZ (K8a) | Kühltage einer Zone an Heiztagen der anderen gekappt, Wirkung der Kurve überdeckt | gewollt als Prüfung des Grundes „Umschaltung“; Wache zählt die Stunden je Grund getrennt |
| Kühlübergabespalten der Zone seit Schritt 137 ungerechnet | Bestandsprojekte mit gepflegten Zonenwerten rechnen nach KZ1 anders | Kernschalter bis KK5 (Festlegung 13); Inventar der Projekte mit Zonen und Kühlübergabe in KZ1 |
| Parallele Arbeit an `Angebot(h, V)` (Umsetzung Übergabegrenze, UB-E2) | Merge-Konflikt in Angebot und Kaskade | Reihenfolge mit der Orchestrierung abstimmen; KK2 berührt nur die Kälteseite |
| Schritt-, Basis- oder Projektnummer kollidiert | Merge-Konflikt | 202 angemeldet; R44, RP-KK und RP-KKZ vor dem Bau anmelden; 1060 ist vergeben |

## 9. Nicht in KK

Gleitendes Temperaturpaar des Kältespeichers, gerechnete Taupunktgrenze und Feuchtebilanz, eine Vorlaufgrenze oder Kühlkurve je
Zone (ein Kühlkreis je Gebäude), mehrere Kühlkreise in einem Gebäude, Kühlkurve auf AK1/AK2 (E106, Q-KK-2 (a)), Umschaltung der
Wärmepumpe je Stunde, Kältenetz und Pumpen, Kühlung im Tagesbilanz-Weg, Änderungen an der Heizseite.

## 10. Logbuch-Entwürfe

Je ein Satz, veröffentlicht mit dem gebündelten Wiki-Upload (Regel:
[Konzept Hilfesystem](../Konzept_Hilfesystem_Wikidokumentation.md) 13.3 und 13.4); Datum und Versionsnummer setzt der Anwender.

- (a) Auf der Kopplungsstufe AK3 kann das Gebäude eine raumgeführte Kühlkurve fahren; der Auslegungspunkt der Kurve ist wählbar.
- (b) Im Mehrzonenmodell rechnet die Kühlübergabe je Zone; der Zonendialog zeigt ihre Felder.
