# Entwurf KK — die raumgeführte Kühlkurve (Gegenstück zu H2)

**Stand 08.10.2026 · vorgelegt; die Fragen Q-KK-1 bis Q-KK-6 sind offen (Abschnitt 7) und werden als E106 nachgetragen.**
Auftrag aus **E105** (Anwender, 08.10.2026: „Starte im Anschluss die raumgeführte Kühlkurve“): Q-AK3K-3 wird als eigener
Gegenstand aufgenommen, Variante (a) des [Entwurfs AK3-K](2026-10-07_Entwurf_AK3-K.md) (Spalte am Gebäude, nur AK3,
Schemaschritt, Dialogfeld, Einfrierregel) ist die Ausgangsskizze. Gelesen auf `fc601d202`, Stichproben auf `76c32d5f0`
(Basis R43 `2026-10-07_R43_Kaelteseite_AK3K`, vierundzwanzig Projekte; Testdatenbank Schemastand 201). **Nummern laut Kopf
der Statusdatei:** Schemaschritt **202** und Basis **R44** sind heute frei; die Projektnummer 1060 ist vom
[Konzept Übergabegrenze](../Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md) vorgemerkt. Dieses Papier vergibt keine Nummer:
der Schemaschritt heißt hier **S1**, das Referenzprojekt **RP-KK**, die Basis **R44** (Platzhalter); angemeldet wird vor dem
Bau. **Verfahren:** zwei Leser (L1: Code mit `Datei:Zeile`; L2: Papiere, Widersprüche, Fragen, Folgen), Stichproben am Code,
Synthese hier; wo die Leser sich widersprachen, gilt der Code. **Grundlage:**
[Anlagenkopplung](../Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 7.1, 7.2, 7.4 Nr. 5 und Nr. 10;
[Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) K21; [Entwurf AK3](2026-10-07_Entwurf_AK3.md) H2,
Festlegungen 23 und 24; [Entwurf AK3-K](2026-10-07_Entwurf_AK3-K.md) 4.2, 4.3, Q-AK3K-3; [Register](../Status_Gebaeudesimulation_VDI6007.md)
E37, E102, E104, E105. **Die Konzepte werden mit diesem Papier nicht geändert;** ihre Berichtigung (1.3) zieht die Welle KK0 nach.

## 0. Das Ergebnis in Punkten

- **Der Kühlvorlauf wird eine Stundenreihe.** Heute ist er ein fester Wert je Gebäude und je Erzeuger; KK macht ihn zu einer
  Kurve über die Außentemperatur mit **Fußpunkt** (nie „aus“) und einer **Absenkung nach der Raumtemperatur** — der Spiegel
  von Heizkurve und Raumeinfluss H2.
- **Der Gebäudelöser bleibt unberührt.** Er liest den Kühlvorlauf schon je Stundenrand; neu sind die Reihe im Eingangsbauer,
  die Absenkung im Kreis und die Erzeuger am Stundenvorlauf.
- **Die Wirkung liegt beim Erzeuger.** Ohne gleitenden Erzeugervorlauf kann eine Kurve am Gebäude den Vorlauf nur anheben
  (die Mischgruppe mischt nie kälter als die Anlage); die bessere Leistungszahl entsteht erst, wenn Wärmepumpe und
  Kältemaschine am Stundenvorlauf rechnen. Empfehlung: Gebäude **und** Erzeuger gleitend (Q-KK-1).
- **Nur Stufe AK3** (Empfehlung Q-KK-2), nur Einzonenweg (Q-KK-4); der Kältespeicher bleibt an seinem festen Paar.
- **Leere Spalten rechnen wie heute:** Ohne wirksamen Wert entsteht kein Objekt, die Basis R43 bleibt byte-gleich bis zur
  Basiswelle; ein Schemaschritt S1 mit drei Eingabe- und drei Ergebnisspalten.
- **Neues Referenzprojekt RP-KK** als Kopie von 1058 (Empfehlung Q-KK-6), nicht in der CI; R44 bewegt kein bestehendes Projekt.
- **Rechenzeit:** mittlere Durchläufe des Kreises von 3,21 auf etwa 3,4–3,5 (+6–9 %), geschätzt.
- **Aufwand 10–15 PT** in sieben Wellen KK0–KK6 (die Skizze in AK3-K nannte +1–1,5 PT; Abschnitt 6.1 erklärt den Abstand).

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
- **Mehrzonenweg:** keine gekoppelte Kälteseite (`GebaeudeModellEingang.cs:1301`, stichprobengeprüft).
- **Referenzprojekte mit Kühlübergabe:** 1047 (AK1), 1056 (AK2), 1058 (AK3, Wärmepumpe, Gebäudevorlauf 18 °C über der Grenze
  16 °C) und 1059 (AK3, Kältemaschine mit Kältespeicher, Gebäudevorlauf an der Grenze 16 °C). In 1058 und 1059 ist die
  Reihe `kuehlvorlauf_0.csv` konstant.

### 1.3 Widersprüche in den Papieren und ihre Auflösung

| # | Widerspruch | Auflösung |
|---|---|---|
| WK1 | Anlagenkopplung 7.4 Nr. 5 sagt „keine Kühlkurve und keine Kennlinienwahl je Stunde“ und verweist zugleich auf „die raumgeführte Kurve in AK3“ als belastbare Führung — der Verweis ist uneingelöst | KK löst ihn ein; Nr. 5 wird in KK0 auf „fester Vorlauf ohne Kühlkurve, raumgeführte Kühlkurve auf AK3 nach Entwurf KK“ berichtigt |
| WK2 | Anlagenkopplung 7.1 und E37 (A3) nennen die Kühlkurve „vertagt“; Kühlkurve und Kennlinie setzen dort einen festen Vorlauf je Stunde voraus | E105 hebt die Vertagung für AK3 auf; ohne Kühlkurve gilt der feste Vorlauf weiter |
| WK3 | Q-AK3K-3 (b) „kein Schemaschritt dafür“ gegen Variante (a) „Schemaschritt“ | (b) galt dem Entwurf AK3-K; KK hat seinen eigenen Schritt S1 |
| WK4 | Die gespiegelte Heizkurve schaltete bei kühler Außenluft ab (Anlagenkopplung 7.4 Nr. 5; Code: NaN jenseits der Heizgrenze) | Kurvenform mit Fußpunkt statt Grenze (Festlegung 2) |
| WK5 | Wiki Gebäudemodell und Wiki Kühlung sagen „Kühlkurve gibt es nicht“, „Vorlauf der Kühlübergabe ist fest“ | gilt bis KK5; Nachzug in KK6 |

## 2. Architektur

### 2.1 Kurvenform

Der Spiegel der Heizkurve taugt nicht: Kühllast entsteht auch bei kühler Außenluft (Sonne, innere Lasten), die Kurve darf
daher nie „aus“ liefern. Die Kühlkurve ist eine **Zwei-Punkt-Kurve** über die Außentemperatur:

- **Fußpunkt** θ_V,F: der Vorlauf bei Außentemperatur am Kühlsollwert der Stunde und darunter (waagrecht, nie NaN);
- **Auslegungspunkt** θ_V,K,N (vorhandene Spalte `Kuehl_Auslegung_Vorlauf`) bei der Auslegungs-Außentemperatur der Kühlung,
  hergeleitet als wärmstes Tagesmittel wie die Nennleistung aus dem Auslegungstag (`GebaeudeModellEingang.cs:2163-2198`);
- dazwischen linear, darüber waagrecht am Auslegungspunkt;
- **Grenzen:** unten die Vorlaufgrenze des Gebäudes und der kälteste erreichbare Erzeugervorlauf (2.3), oben der Fußpunkt,
  der stets einen Mindestabstand unter dem Kühlsollwert der Stunde hält (sonst wäre die Übergabeleistung null,
  `Kuehluebergabe.cs:101-107`).

Ohne Raumeinfluss ist die Reihe explizit und wird wie `VorlaufC` einmal im Eingangsbauer gerechnet.

### 2.2 Raumeinfluss

θ_V,K = KK(θ_out) − k_K·max(0, θ_air − θ_max), mit k_K aus `Kuehlkurve_Raumeinfluss` [K/K], θ_max dem Kühlsollwert der Stunde,
θ_air der Raumluft derselben Stunde; unten gekappt an der Untergrenze aus 2.1. **Führungsgröße** im Einzonenweg die Zone selbst
(im Mehrzonenweg wäre es die Zone mit der größten Überschreitung — draußen, Q-KK-4). **Nachführung im Kreis wie H2:** Durchlauf 1
ohne Absenkung, jeder weitere mit der Absenkung aus der Lösung derselben Stunde; neues Abbruchglied |ΔK2| ≤ 0,05 K neben ΔH2;
Pendelregel und Höchstzahl wie AK3. Am Tagesbeginn und an jedem Stundenbeginn steht die Absenkung auf null (Muster
`Raumeinfluss.StundeBeginnen`).

### 2.3 Erzeuger gleitend

Bei Antwort (a) auf Q-KK-1 fährt der Kälteerzeuger je Stunde den **kältesten** verlangten Vorlauf der gekoppelten Gebäude
(jedes wärmere Gebäude mischt in seiner Mischgruppe hoch; ein bedarfsgewichteter Vorlauf wie auf der Heizseite ließe das
kälteste Gebäude über seiner Kurve). Untergrenze des Erzeugers: kleinste Stützstelle der Kühlkennlinie der Wärmepumpe bzw.
`Kaltwasser_Vorlauf_Min` der Kältemaschine.

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
Kühlstunden einer Zone fallen nie auf denselben Tag; die beiden Raumeinflüsse iterieren daher kaum gleichzeitig.

### 2.8 Mehrzonenweg

Benannt draußen (Empfehlung Q-KK-4): Der Mehrzonenweg hat keine gekoppelte Kälteseite. Ein Gebäude im Mehrzonenweg mit
gesetzter Kühlkurve bekommt eine benannte Meldung „Kühlkurve im Mehrzonenweg nicht verfügbar, fester Vorlauf“ und rechnet wie heute.

### 2.9 Rechenzeit

Geschätzt, nicht gemessen (L1): Kurve allein ±0 (Jahresreihe vorab); mit Raumeinfluss am Gebäude in den rund 590 Kühlstunden
von 1058 +1 bis +2 Durchläufe → Mittel ≈ 3,3; mit gleitendem Erzeuger +2 bis +3 in Kühlstunden, Höchstwert 8–10 möglich →
Mittel ≈ 3,4–3,5 (+6–9 %). Die Höchstzahl 20 bleibt fern; gemessen wird in KK3.

## 3. Festlegungen (benannt, Widerspruch möglich)

Was ohne weiteren Anwenderentscheid festgelegt wird; Widerspruch ist möglich, bis die zugehörige Welle beauftragt ist.

1. **Leere Spalte = fester Vorlauf wie heute.** `Kuehlkurve_Aktiv` 0 nimmt den heutigen Skalarpfad wörtlich; ein Raumeinfluss
   mit NULL oder 0 baut kein Objekt (Muster `Raumeinfluss.AusGebaeuden`, Schritt 198) — die Basis bleibt byte-gleich.
2. **Kurvenform:** Zwei-Punkt-Kurve mit Fußpunkt (2.1), keine gespiegelte Heizkurve (WK4), kein Exponent.
3. **Auslegungs-Außentemperatur der Kühlung** wird hergeleitet (wärmstes Tagesmittel), keine neue Spalte.
4. **Fußpunkt leer** bei aktiver Kurve: Vorgabe ist der Auslegungsrücklauf der Kühlübergabe (`Kuehl_Auslegung_Ruecklauf`
   bzw. dessen Vorgabe je Art); der Mindestabstand zum Kühlsollwert ist ein Festwert in `GebaeudeFestwerte`, bestimmt per
   Probe in KK1.
5. **Untergrenze** des Gebäudevorlaufs ist immer `Kuehl_Vorlaufgrenze`; die Taupunktgrenze bleibt eine Vorgabe, der Grund
   `VORLAUFGRENZE_KUEHLUNG` bleibt an die gesättigte Übergabe an der Grenze gebunden.
6. **Raumeinfluss nur mit `Kuehlkurve_Aktiv`** und Kühlübergabe (Muster H2: Raumeinfluss nur mit `Heizkurve_Aktiv`).
7. **Bereiche:** `Kuehlkurve_Raumeinfluss` 0–10 K/K wie `Heizkurve_Raumeinfluss`; `Kuehlkurve_Fusspunkt` im Bereich der
   Vorlaufgrenze (4–22 °C).
8. **Abbruchmaß** |ΔK2| ≤ 0,05 K (gleiches Maß wie ΔV und ΔH2); Pendelregel und Höchstzahl wie AK3.
9. **Mehrere Gebäude:** der Erzeuger fährt den kältesten verlangten Vorlauf (2.3).
10. **Kältespeicher** bleibt am festen Paar; der Erzeugervorlauf gleitet nie wärmer als der Speichervorlauf (2.6).
11. **Kälteschranke** am Stundenvorlauf über das Vorlauf-Argument; `Kaeltekorrektur` bleibt Prüfnaht (2.5).
12. **Ergebnisspalten** an `Tab_ErgebnisEnergiebedarf` (S1, NULL außerhalb des Geltungsbereichs): mittlerer Kühlvorlauf der
    Kühlstunden, Summe der Absenkung [Kh], Stunden an der Vorlaufgrenze; die Stundenreihe `kuehlvorlauf_0.csv` trägt den
    gleitenden Vorlauf.
13. **Kernschalter bis zur Basiswelle:** Bis KK5 rechnen Kurve und Erzeuger am Stundenvorlauf nur mit einem Kernschalter
    (Vorgabe aus, Muster W-I); jedes Gate bis dahin ist „Referenzlauf 24/24 byte-gleich gegen R43“.
14. **Mehrzonenweg** benannt abgelehnt mit Meldung (2.8), sofern Q-KK-4 nicht anders entschieden wird.

## 4. Referenzprojekt und Basis

- **RP-KK** (Empfehlung Q-KK-6 (a)): Kopie von 1058 auf dem Kopierweg des Programms (Skriptmuster
  `Referenzlaeufe/Skripte/referenzprojekt_1058_ak3.py`) mit gesetzter Kühlkurve (Fußpunkt über dem Vorlauf von 1058,
  Raumeinfluss gesetzt), damit die Wärmepumpe an milden Kühltagen wärmer und an heißen kälter fährt. **Nicht in der CI-Auswahl.**
  Die Nummer wird beim Bau nach den Kopfzeilen der Statusdatei gewählt und angemeldet (1060 ist vorgemerkt).
- **Basis R44** wird beim Bau angemeldet: abweichend allein RP-KK (neu); die vierundzwanzig Projekte von R43 bleiben byte-gleich,
  weil keines die Spalten setzt. Die Testdatenbank bekommt Schritt S1 und das neue Projekt (LFS-Commit).
- **Einfrierregeln (Vorschlag im Wortlaut, mit KK5 in `CLAUDE.md` und `Referenzlaeufe/LIESMICH.md`):**
  - in „gesäte Auslegungsdaten der Übergabe“ nach `Kuehl_Vorlaufgrenze` ergänzt: „die Kühlkurve am Gebäude
    (`Kuehlkurve_Aktiv`, `Kuehlkurve_Fusspunkt`, `Kuehlkurve_Raumeinfluss`)“;
  - neu: „gesäte Kühlkurvendaten eines Referenzprojekts mit Kühlkurve: die Kühlkennlinie seiner Kälteerzeuger samt aller
    Vorlauf-Stützstellen (`Tab_Kenndaten_Kuehlung`, `Tab_Kenndaten_Kaeltemaschine`), `Kaltwasser_Vorlauf_Min`, das
    Temperaturpaar seines Kältespeichers, die Festwerte der Kühlkurve in `GebaeudeFestwerte` (Mindestabstand zum Kühlsollwert,
    Bereiche) und das Abbruchmaß ΔK2, dazu das Anlegen oder Entfernen eines Referenzprojekts mit Kühlkurve“.
- **Tests und Wachen:** neu `KuehlkurveTests` (Form, Fußpunkt, Grenzen, „aus bitgleich“), Orakel **O1kk/O2kk** nach Muster
  O1k/O2k (stetige Kälteleistung über den Vorlauf, Fixpunkt eindeutig; Absenkung mit Kappung), `KuehlkurveSchemaTests`
  (Spalten, Sicht, vier Kopierwege NULL-erhaltend, Wiederholprobe), bunit des Dialogfelds, `KuehlkurveReferenzprojektWacheTests`;
  mitzuziehen `Ak3ReferenzprojektWacheTests`, `Ak3KReferenzprojektWacheTests` (unverändert grün), `AnlagenkopplungOrakelTests`,
  `KaelteerzeugerTests` (Kennlinie am festen Vorlauf bitgleich), `GebaeudeRundlaufTests`; dazu `SqlDialektPruefer` und
  `designer_neu.py`. **CI-Auswahl unverändert.**

## 5. Was KK nicht verändert

Die Heizseite samt H2, die Zonensperre und die Tagesart, die Umschaltung der Wärmepumpe je Tag (K8a), die Stufen AK1 und AK2
(bei Empfehlung Q-KK-2), den Tagesbilanz-Weg und alle Projekte ohne gesetzte Kühlkurve.

## 6. Wellenplan

Je Welle ein Opus-Auftrag mit höchstens rund 150 Werkzeugaufrufen; das Gate fährt die Orchestrierung nach dem Merge. Bis KK5 ist
jedes Gate „Referenzlauf 24/24 byte-gleich gegen R43“. Schemaschritt: allein **S1** in KK4 (heute wäre 202 frei; Anmeldung vor
dem Bau).

| Welle | Inhalt | Abnahme | Basis | PT |
|---|---|---|---|---|
| **KK0** Papiere | E106 eintragen; Anlagenkopplung 7.1, 7.4 Nr. 5, Kühlkonzept K21 und E37-Verweis nachziehen (WK1–WK3); Nummern anmelden | Registerzeile; `DokumentationLinkWacheTests` grün | nein | 0,5–1 |
| **KK1** Kurve als Reihe | Kernschalter; `KuehlVorlaufC` als Jahresreihe, Klasse `Kuehlkurve` (2.1), Rand liest `[h]`; Festwert Mindestabstand per Probe | `KuehlkurveTests`; „Reihe konstant = heute bitgleich“; Referenzlauf 24/24 byte-gleich | nein | 1–1,5 |
| **KK2** Erzeuger am Stundenvorlauf | Kühlkennlinie mit gebrochenem Vorlauf, alle Blöcke je Gerät; Kältemaschine je Stunde; Vorlauf-Argument der Kälteschranke; Kältekaskade am Stundenvorlauf; kältester verlangter Vorlauf; Speicherregel | Orakel O1kk; „fester Vorlauf bitgleich“; Kälteklassen grün; Referenzlauf byte-gleich | nein | 2,5–3,5 |
| **KK3** Raumeinfluss im Kreis | Spiegel von `Raumeinfluss`, Nachführung in `Anlagenkopplung`, Abbruchglied ΔK2, Pendelregel; Rechenzeit messen | Orakel O2kk; Durchläufe gemessen; Referenzlauf byte-gleich | nein | 2–3 |
| **KK4** Schema, Oberfläche | S1 (drei Eingabespalten an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`, Sicht `Abfrage_Projektgebaeude`, Kopierwege, drei Ergebnisspalten); Modelle, Controller, Katalog, Export, Hülle, Feld in `GebaeudeKuehluebergabeFelder`, Herleitung, Ressourcen beider Sprachen | Schema- und bunit-Tests; `SqlDialektPruefer`; `designer_neu.py`; Windows-Schale auf Linux kompiliert | nein | 2–3 |
| **KK5** Referenzprojekt und R44 | Kernschalter entfernt; RP-KK (Skript, Wache); Einfrierregeln; Basis R44 | allein RP-KK neu, übrige 24 byte-gleich; CI-Auswahl unverändert | **ja** | 1,5–2 |
| **KK6** Papiere und Wiki | Konzepte wie gebaut, Register, Status, Protokoll, Wiki-Quellen (Gebäudemodell, Kühlung), Logbuch-Entwurf | Link-Wache, Wiki-Gegenlesemuster | nein | 0,5–1 |
| | **Summe** | | | **10–15** |

KK1 und KK2 hängen an verschiedenen Dateien und können nebeneinander laufen; KK3 setzt beide voraus; KK4 kann nach KK1 beginnen;
KK5 setzt KK3 und KK4 voraus. **Bei Antwort (b) auf Q-KK-1** (nur Gebäude) entfällt KK2 bis auf den Rand (−2–3 PT, Summe 7,5–12).

### 6.1 Warum 10–15 PT statt +1–1,5 PT

Die Skizze in Q-AK3K-3 (a) dachte an eine Spalte und eine gespiegelte Absenkung am Gebäude **bei festem Erzeuger und fester
Schranke** — eine Kopie des H2-Pfads. Sie enthielt nicht: die Kurvenform mit Fußpunkt (die gespiegelte Heizkurve schaltet ab),
den gleitenden Erzeuger mit Kennlinie am Stundenvorlauf für Wärmepumpe und Kältemaschine (ohne ihn wirkt die Kurve nicht, 1.2),
die vorlaufabhängige Kälteschranke und damit den Fixpunkt der Kälteseite samt Orakeln, die Speicherregel, rund zwei Dutzend
Leserstellen einer Gebäudespalte (Schema, Sicht, vier Kopierwege, Modelle, Katalog, Export, Hülle, Dialog), das Referenzprojekt
mit Basis und die Papiere. L2 schätzte 8–12 PT ohne eigene Kurvenwelle und ohne die Kältemaschine am Stundenvorlauf; dieser
Entwurf nimmt beides auf.

### 6.2 Wie gebaut

(leer bis zum Bau)

## 7. Fragen an den Anwender

Je Frage höchstens vier Antworten; die Antworten werden als **E106** im Register nachgetragen.

| # | Frage | Optionen und Folgen | Empfehlung |
|---|---|---|---|
| **Q-KK-1** | **Gleitet nur das Gebäude oder auch der Erzeuger?** | (a) Gebäude und Erzeuger: kältester verlangter Vorlauf, Wärmepumpe und Kältemaschine am Stundenvorlauf, bessere Leistungszahl an milden Kühltagen · (b) nur Gebäude, Erzeuger fest: Kurve kann den Vorlauf nur anheben, kein Gewinn beim Erzeuger, −2–3 PT · (c) nur die Wärmepumpe gleitet, Kältemaschine fest: −0,5 PT, Projekte mit Kältemaschine ohne Wirkung | (a) |
| **Q-KK-2** | **Auf welchen Stufen gilt die Kühlkurve?** | (a) Kurve und Raumeinfluss nur AK3 (wie E104, Q-AK3K-5) · (b) außentemperaturgeführte Kurve ab AK1, Raumeinfluss nur AK3 (wie die Heizseite): Kaskade auf AK1/AK2 nachträglich mit Vorlaufreihe, +1–1,5 PT, 1047 und 1056 bleiben ohne gesetzte Werte byte-gleich | (a) |
| **Q-KK-3** | **Woraus besteht die Führung?** | (a) Kurve über die Außentemperatur mit Fußpunkt plus Absenkung nach der Raumtemperatur (zwei Bausteine wie Heizkurve und H2) · (b) nur Absenkung vom Fußpunkt nach der Raumtemperatur, ohne Außentemperaturglied: eine Spalte weniger, der Kreis trägt die ganze Führung, mehr Durchläufe · (c) nur Kurve über die Außentemperatur ohne Raumeinfluss: explizit, kein Fixpunkt, aber nicht „raumgeführt“ | (a) |
| **Q-KK-4** | **Gehört der Mehrzonenweg dazu?** | (a) nein, benannt abgelehnt mit Meldung, fester Vorlauf · (b) ja: die Kälteseite im Mehrzonenweg wird gekoppelt (Nachzug G6), Führungsgröße die Zone mit der größten Überschreitung, +4–6 PT, eigener Gegenstand | (a) |
| **Q-KK-5** | **Welche Stärke gilt beim Einschalten des Raumeinflusses?** | (a) keine Vorgabe: leer heißt aus, der Anwender trägt den Wert ein · (b) Vorgabewert im Dialog beim Einschalten, per Probe in KK3 bestimmt · (c) der Wert von `Heizkurve_Raumeinfluss` desselben Gebäudes | (b) |
| **Q-KK-6** | **Welches Referenzprojekt hält die Kühlkurve?** | (a) neu RP-KK als Kopie von 1058 (Wärmepumpe mit Kühlkennlinie), nicht in der CI; R44 bewegt kein bestehendes Projekt · (b) neu als Kopie von 1059 (Kältemaschine mit Kältespeicher): die Speicherregel begrenzt das Gleiten, wenig Wirkung · (c) 1058 selbst umstellen: bewegt ein CI-Projekt, Basiswechsel für die CI · (d) 1059 selbst umstellen: die reine Schrankenmessung von AK3-K geht verloren | (a) |

## 8. Risiken

| Risiko | Wirkung | Gegenmittel |
|---|---|---|
| **Kennlinienrand** (Stützstellen ganzzahlig, oft nur zwei) | Vorlauf außerhalb nimmt den Randwert; Pendeln zwischen Stützstellen | Randwert benannt; Pendelregel wie `Anlagenkopplung.cs:503-519` |
| Fixpunkt der Kälteseite konvergiert schlechter als H2 | mehr Durchläufe, in Schrankenstunden am meisten | Orakel O1kk/O2kk; Messung in KK3; Höchstzahl 20 |
| Fußpunkt zu nah am Kühlsollwert | Übergabeleistung null, Raum überhitzt still | Mindestabstand (Festlegung 4); Prüfung im Dialog |
| Raumeinfluss senkt unter die Taupunktvorgabe | Kondensat in der Wirklichkeit | harte Untergrenze `Kuehl_Vorlaufgrenze` (Festlegung 5) |
| Kältespeicher und gleitender Erzeuger | Speicher wird nicht geladen | Regel 2.6 (Festlegung 10) |
| Leser des festen Kühlvorlaufs übersehen (`VorlaufFestC`, Kaskade, Bericht) | still falscher Vorlauf in Ergebnis oder Bericht | Leserinventar in KK1 und KK2; Probe „Reihe konstant = heute“ |
| Parallele Arbeit an `Angebot(h, V)` (Umsetzung Übergabegrenze, UB-E2) | Merge-Konflikt in Angebot und Kaskade | Reihenfolge mit der Orchestrierung abstimmen; KK2 berührt nur die Kälteseite |
| Schritt-, Basis- oder Projektnummer kollidiert | Merge-Konflikt | S1, R44 und RP-KK vor dem Bau anmelden; 1060 ist vergeben |

## 9. Nicht in KK

Kühlkurve im Mehrzonenweg (bei Q-KK-4 (a)), gleitendes Temperaturpaar des Kältespeichers, gerechnete Taupunktgrenze und
Feuchtebilanz, Kühlkurve auf AK1/AK2 (bei Q-KK-2 (a)), Umschaltung der Wärmepumpe je Stunde, Kältenetz und Pumpen, Kühlung im
Tagesbilanz-Weg, Änderungen an der Heizseite.

## 10. Logbuch-Entwürfe

(leer bis zum Bau; je ein Satz, veröffentlicht mit dem gebündelten Wiki-Upload, Regel:
[Konzept Hilfesystem](../Konzept_Hilfesystem_Wikidokumentation.md) 13.3)
