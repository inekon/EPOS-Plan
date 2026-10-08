# Entwurf AK3-K — die Kälteseite im geschlossenen Kreis

**Stand 08.10.2026 · K0–K6 gebaut, Basis R43 (`2026-10-07_R43_Kaelteseite_AK3K`); offen: Sichtabnahme.** Wie gebaut
und wo der Bau vom Plan abweicht: Abschnitt 7.1.

**Stand 07.10.2026 · vorgelegt; die Fragen Q-AK3K-1 bis Q-AK3K-7 sind mit E104 entschieden (Abschnitt 7), dazu zwei
Vorgaben des Anwenders: Tagesbetriebsart je Zone (Abschnitt 3) und Kalenderfreigabe für Heizen und Kühlen (3.3).** Auftrag
aus **E103** (Anwender, 07.10.2026: „setze AK3-K um (Rückwärtswirkung)“); die Vertagung aus E102 (Q-AK3-4 (a)) entfällt.
Gelesen und gemessen auf `feb8db420` (Basis R42 `2026-10-07_R42_Vorlaufinterpolation_AK3`, dreiundzwanzig Projekte;
Testdatenbank Schemastand 198). **Nummern laut Kopf der Statusdatei:** die Basis **R43** ist für AK3-K angemeldet;
Schemaschritt 199 ist von der Sitzung IFC angemeldet, **200** ist frei. Dieses Papier vergibt keine Schemanummer: der eine
Schemaschritt heißt hier **S1** (Ergebnisspalten, Welle K4; gebaut als Schritt **201**), das neue Referenzprojekt **RP-AK3K**; angemeldet wird vor dem
Bau. **Verfahren:** zwei Leser (L1: Laufordnung der Kälte, Nähte, Zustand, Rechenzeit mit Messung, Fehler im gebauten AK3;
L2: Papiere, Widersprüche, Fragen, Basis, Tests, Wellen), Stichproben ihrer Aussagen am Code, an `kern.yml` und an der
Basis R42, Synthese in diesem Papier; wo die Leser sich widersprachen, gilt der Code (1058 steht in der CI-Auswahl).
**Grundlage:** [Anlagenkopplung](../Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 5.3, 7.1–7.4, 8.1, 10.5;
[Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.5, 5.1–5.5, 12.2;
[Konditionierungsprofile](../Konzept_Konditionierungsprofile_EPOS-Plan.md) 3.1, 3.2, 5.1;
[ADR-005](../ADR-005_Zonenkopplung_Mehrzonenmodell.md); [Entwurf AK3](2026-10-07_Entwurf_AK3.md) Festlegungen 4, 10, 18;
[Register](../Status_Gebaeudesimulation_VDI6007.md) E37, E53, E102, E103, E104. **Die Konzepte werden mit diesem Papier
nicht geändert;** ihre Berichtigungen (Abschnitt 1.3) zieht die Welle K0 nach.

## 0. Das Ergebnis in Punkten

- **In einer Zone wird an einem Tag nie geheizt und gekühlt** (E104, Q-AK3K-7): Die Tagesbetriebsart gilt **je Zone** und
  auf **allen Stufen**; am Kühltag ist die Raumheizung der Zone gesperrt, am Heiztag die Raumkühlung. Verschiedene Zonen
  dürfen am selben Tag verschiedene Betriebsarten haben. Prozesswärme, Prozesskälte und Brauchwasser sind ausgenommen.
- **Der Konditionierungskalender gibt Heizen und Kühlen frei.** Das Datenmodell kann das schon: Heiz- und Kühlsollwert
  kennen „aus“ je Periode, je Standardwoche und als Heiz- bzw. Kühlperiode (E53). Die Tagesbetriebsart wählt nur innerhalb
  der Freigabe; **kein Schemaschritt dafür**.
- **Der Gebäudeschritt rechnet die Kälte schon mit.** Der Stepper von AK3 löst Heizen und Kühlen im selben Schritt; der
  Löserfall `Kuehlgrenze` kappt die Kälte physikalisch. Es fehlt allein die **anlagenseitige Grenze**.
- **AK3-K spiegelt AK3 auf die Kälteseite:** zustandsfreie **Kälteangebotsfunktion** (Kälteerzeuger am Kühlvorlauf,
  Kältespeicher am Stundenbeginn), **Kälteschranke** im `Stundenrand`, **Kältekaskade als Stundenschritt** nach der
  Wärmestunde. Die Kaskade wird befragt, nicht zurückgenommen. Die Rückwirkung gilt nur auf AK3.
- **Die reversible Wärmepumpe bleibt je Tag Heiz- oder Kältemaschine** (K8a, projektweit); am Kühltag decken Kessel und BHKW
  Brauchwasser, Prozesswärme und die Raumheizung anderer Zonen.
- **AK3-K behebt zuerst zwei Fehler des gebauten AK3:** Der Kühlkanal folgt dem Kreis nicht (1058: 249 Stunden,
  +1,95 kWh), und die Wärmeschranke übersieht die Heizsperre der reversiblen Wärmepumpe am Kühltag.
- **Basis R43 bewegt fünf Projekte:** 1017, 1047, 1055, 1056 durch die Zonensperre, 1058 durch Zonensperre und Kreis; dazu
  RP-AK3K neu. Bis zur Basiswelle liegen beide neuen Wege hinter Kernschaltern ohne Schema, Vorgabe aus — 1017, 1047 und
  1058 stehen in der CI-Auswahl.
- **Rechenzeit:** Kreis Faktor 1,05–1,15 gegen AK3 heute (oben 1,3); Zonensperre rund +2–5 % des Gebäudelaufs.
- **Aufwand 11,5–19,5 PT** in acht Wellen K0, K1, KZ, K2–K6.

## 1. Befunde

### 1.1 Zwei Fehler im gebauten AK3 (AK3-K behebt sie zuerst)

**(a) `Ak3Nachfuehren` führt die Kälte nicht nach.** `SimulationWaermebedarf.Ak3Nachfuehren` übernimmt die
Gebäudeergebnisse des Steppers samt dessen Kühlreihe, führt aber weder `Kanal.KUEHLUNG` noch `Kaeltebedarf`,
`Kaeltebedarf_Gebaeude`, den Prüfakkumulator der Bedarfsprobe noch `Kuehlkreis` nach (Stichprobe: die Datei
`SimulationWaermebedarf.Ak3.cs` nennt keine Kühlgröße). Gemessen bei 1058: `Geb[0].KuehlenergieMwh` 3,85180 MWh (Stepper)
gegen `Kaeltebedarf_Gesamt` 3,84984 MWh (Pass 1); 249 Stunden weichen ab, in Summe +1,95 kWh. Die Kaskade deckt die Reihe
aus Pass 1, Bericht und Gebäudezeile zeigen die Reihe aus dem Kreis. **Wer den Kanal nachführt, zieht `Kaeltebedarf`,
`Kaeltebedarf_Gebaeude` und die Bedarfsprobe im selben Zug mit** — sonst bricht die Bedarfsprobe (FEHLER, Lauf ungültig)
oder die Deckungsprobe Kälte. Die Bedarfsprobe läuft dann ein zweites Mal nach dem Kreis.

**(b) `WaermepumpeKapazitaet` übersieht die Heizsperre am Kühltag (K8a).** Die Wärmeschranke prüft Fahrplan, Sperrzeit,
Zeitprogramm und Abschaltpunkt, nicht `HeizkanalGesperrt` (Stichprobe: `Stundenangebot.cs` kennt den Kühltag nicht). An
Kühltagen bietet sie Heizleistung der reversiblen Wärmepumpe an, die die Kaskadenschleife dann verweigert; die Schranke ist
an solchen Tagen zu groß. Bei 1058 bleibt das folgenlos, weil Kessel und BHKW die 0,02 MWh Heizbedarf an Kühltagen decken;
mit der Zonensperre (Abschnitt 3) entfällt dieser Bedarf ganz, der Fehler bleibt für Brauchwasser, Prozess und Mehrzonen.

**Nebenbefund — widersprüchliche Laufmeldung.** Das Lauflog von 1058 meldet zugleich „Anlagenkopplung AK3 (Kernstufe): …
geschlossener Kreis“ und „Das Projekt steht auf der Stufe AK3; gebaut ist die Stufe AK1 — gerechnet wird der Heizkreis als
Randbedingung (AK1)“. Die zweite Meldung (Ressource in `MyResource/Resource.resx`) ist auf AK3 falsch.

### 1.2 Die Kälteseite heute

- **Bedarf (Pass 1):** Das Gebäudejahr rechnet unbegrenzt; `SimulationKaeltebedarf.GebaeudeBuchen` bucht den Kühlbedarf in
  Kühlkanal, `Kaeltebedarf_Gebaeude` und Prüfakkumulator; `Kaelteseite.Abschliessen` bildet `Kaeltebedarf` samt Monat,
  Dauerlinie, Maximum, Gesamt und fährt die Bedarfsprobe. `Kuehlkreis` entsteht aus Pass 1.
- **Tagesbetriebsart (K8a):** `Kaeltekaskade.TagesbetriebsartBestimmen(Heizkanal, Kühlkanal)` bildet sie **projektweit**
  aus den Tagessummen der Projektkanäle von Pass 1: Kühltag, wenn die Kühlsumme die Heizsumme übersteigt; Gleichstand und
  ein Tag ohne Bedarf sind Heiztage. Sie gilt für den Heizkanal der reversiblen Maschine; Brauchwasser und Prozess bleiben
  bedienbar. **Auf der Bedarfsseite gibt es heute keine Tagesbetriebsart** — eine Zone kann am selben Tag heizen und kühlen.
- **Anlage (nach der Wärme):** `KaelteerzeugerVorbereiten` bildet die Kühlkennlinie einmal je Gerät und Jahr am
  `Kuehl_Vorlauf` (K21 mit I-3); `KaeltekaskadeRechnen` ist eine **eigene Stundenschleife über 8760 h** (Ladewunsch der
  Kältespeicher an Kühltagen, freie Kühlung, Entladung, Erzeuger in Listenfolge mit Taktverlust, Bereitschaftsverlust). Der
  Kältestrom geht danach als Jahresreihe in den Reststrom (ohne Anlagen mit eigenem Zähler, E34).
- **Rückwirkung:** keine. Die Kaskade liest allein `Kaeltebedarf` und die Tagessummen; nichts wirkt aufs Gebäude.
- **Erdsonde:** Die Kühlwärme des ersten Laufs geht als Jahresreihe in einen zweiten, vollständigen Feldlauf (1058:
  4 460 kWh/a).
- **Tagesbilanz-Weg (Altweg):** rechnet keine Kühlung (`Altweg/Tagesbilanz*` ohne Kühlgröße); die Zonensperre greift dort
  nicht, Projekt 1040 bleibt unberührt.
- **Gleichzeitigkeit in den Referenzprojekten** (Basis R42, Raumheizung `waermebedarf_gebaeude` gegen Kühlung
  `waermebedarf_kuehlung`, Tagessummen): alle fünf Kälteprojekte sind Einzonengebäude mit 60 Kühltagen; Heizstunden an
  Kühltagen und Kühlstunden an Heiztagen gibt es so:

  | Projekt | CI | Kühltage mit Heizstunden | Heizstunden | Raumheizung dort [kWh] | Heiztage mit Kühlstunden | Kühlstunden | Kühlung dort [kWh] |
  |---|---|---|---|---|---|---|---|
  | 1017 | ja | 7 | 14 | 50,2 | 5 | 12 | 12,2 |
  | 1047 | ja | 6 | 14 | 24,0 | 4 | 10 | 14,3 |
  | 1055 | nein | 7 | 14 | 50,2 | 5 | 12 | 12,2 |
  | 1056 | nein | 6 | 15 | 24,6 | 3 | 9 | 14,1 |
  | 1058 | ja | 6 | 14 | 24,0 | 4 | 10 | 14,3 |

  L1 zählt bei 1058 aus dem Heizkanal von Pass 1 (mit Netzverlust) 64 Kühltage, 10 davon mit Heizstunden, und 0 Stunden
  mit Heiz- und Kühlbedarf zugleich. Je Projekt wechseln also rund 10 Tage und 25 Stunden ihre Seite; bewegt werden
  zusammen 0,04–0,06 MWh von 64–75 MWh Raumheizung und 3,8–4,1 MWh Kühlung.

### 1.3 Widersprüche in den Papieren und ihre Auflösung

| # | Stelle A | Stelle B | Auflösung (K0 zieht nach) |
|---|---|---|---|
| W1 | Anlagenkopplung 7.3: am anderen Betriebstag Verfügbarkeit **null** | Kühlkonzept 5.2: nur der Heizkanal **der reversiblen Wärmepumpe** ist gesperrt | **Nach E104 zugunsten von 7.3 für die Zone:** am Kühltag der Zone ist ihre Raumheizung ganz gesperrt (alle Erzeuger), am Heiztag ihre Raumkühlung. **5.2 bleibt für die Erzeugerseite:** die reversible Wärmepumpe ist je Tag Heiz- oder Kältemaschine; Brauchwasser, Prozesswärme und Prozesskälte laufen weiter (Festlegungen 1–6) |
| W2 | Anlagenkopplung 7.3 „Umschaltung greift nicht in die Kopplung ein“ | Anlagenkopplung 7.4 AK2-2b „`UMSCHALTUNG` gehört zur echten Kopplung (AK3)“ | Die Zonensperre wirkt auf der Bedarfsseite auf allen Stufen; der Anlagengrund `UMSCHALTUNG` (Anteil der Wärmepumpe an der Schranke) nur auf AK3 |
| W3 | Anlagenkopplung 5.3: `UMSCHALTUNG` in beiden Aufzählungen | Code: nur im `Verfuegbarkeitsgrund`, nirgends gesetzt; im `Begrenzungsgrund` fehlt er | Der Gebäudeseite wird der Wert gegeben (Festlegung 13) |
| W4 | Kühlkonzept 5.2: Kühltage aus dem Projektbedarf vor jedem Erzeuger | Entwurf AK3 F10: im AK3-Weg aus Pass 1 | Die Zonentagesart entsteht in Pass 1 aus dem unbegrenzten Probetag (Festlegung 3), die Erzeugertagesart aus den Kanälen von Pass 1 (Festlegung 6); beide benannt „aus Pass 1“ |
| W5 | Entwurf AK3 1.2 „keine neue Naht ins Gebäude“ | AK3-K braucht eine Kälteschranke im Schritt K | Gilt nur für die Wärme; AK3-K bringt die Kälteseite des `Stundenrand` (4.2) |
| W6 | Q-AK3-4 (b): „Kühlkennlinie am Kühlvorlauf je Stunde“ | Anlagenkopplung 7.4 Nr. 5: Kühlvorlauf fest | Kühlvorlauf bleibt fest (E104, Q-AK3K-3 (b)); eine feste Stützstelle je Gerät |
| W7 | Entwurf AK3 / Code: Angebot kennt den Kühltag nicht | Kaskade sperrt den Heizkanal am Kühltag | Fehler 1.1 (b); AK3-K schließt die Lücke |
| W8 | Kühlkonzept 3.5 (K6/E31): gleichzeitiges Heizen und Kühlen je Zone ausgewiesen, nicht saldiert | E104: in einer Zone nie beides am selben Tag | Innerhalb einer Zone entfällt die Gleichzeitigkeit; die Kennzahl „Stunden mit gleichzeitigem Heizen und Kühlen“ bleibt für das Gebäude über verschiedene Zonen |

## 2. Was AK3-K nicht verändert

Der Profilweg AK1/AK2 (bis auf die Zonensperre, Abschnitt 3), die Kältekaskade außerhalb von AK3, die Kühlkennlinie K21, die
Erzeugerreihenfolge der Kälte (Kühlkonzept 5.5), die freie Kühlung (5.3, 5.4), die Kühlübergabe (Schritt K), der feste
Kühlvorlauf, das Datenmodell der Konditionierungskalender und der Tagesbilanz-Weg.

## 3. Tagesbetriebsart je Zone und Kalenderfreigabe

### 3.1 Regel

Je Zone z und Tag d:

```
H(z,d) = Raumheizung freigegeben   ⇔ der Heizsollwert der Zone ist an mindestens einer Stunde des Tags nicht „aus“
K(z,d) = Raumkühlung freigegeben   ⇔ Kühlbetrieb des Projekts, Kuehlung_Aktiv, und der Kühlsollwert ist an
                                     mindestens einer Stunde des Tags nicht „aus“
beide frei      → Tagesart aus den Tagessummen des unbegrenzten Probetags der Zone: Kühltag, wenn Σ Kühlen > Σ Heizen;
                  Gleichstand und ein Tag ohne Bedarf sind Heiztage (K8a, je Zone)
nur H frei      → Heiztag        nur K frei → Kühltag        keine frei → keine Raumkonditionierung
Kühltag         → Heizsollwert der Zone für den ganzen Tag „aus“ (NaN)
Heiztag         → Kühlsollwert der Zone für den ganzen Tag „aus“ (+∞)
```

Die Sperre nutzt also die vorhandene Semantik von „aus“ (Konditionierungsprofile 3.1) und gilt ganze Tage wie jede Periode
(3.2). Prozesswärme, Prozesskälte und Brauchwasser sind keine Raumkonditionierung und bleiben unberührt.

### 3.2 Laufordnung

- Die Zonentagesart entsteht **in Pass 1**, am Tagesbeginn: Der Stepper (W1 von AK3; der Jahreslauf ruft ihn) sichert den
  Zustand, rechnet den Tag unbegrenzt mit der Kalenderfreigabe, bildet je Zone die Tagesart und rechnet den Tag **nur dann
  neu**, wenn eine Zone mit beiden Freigaben beide Seiten gezeigt hat. Sonst ist der Probetag schon die Lösung.
- Mehrzonengebäude rechnen den Tag gemeinsam neu (die Zonen sind thermisch gekoppelt, ADR-005); jede Zone trägt ihre eigene
  Tagesart.
- Pass 1 trägt danach die Sperre: AK1 und AK2 rechnen mit diesem Bedarf, der Kreis von AK3 übernimmt die Zonentagesart als
  feste Vorgabe (keine Iteration über den Tag).

### 3.3 Kalenderfreigabe

Der Kalender kann Heizen und Kühlen schon heute je Periode ein- und ausschalten: Heiz- und Kühlsollwert haben die Angabe
„aus“ als Grundangabe, in der Standardwoche, je Periode (Rang, Datum oder Feiertagsregel) und als **Heiz- bzw.
Kühlperiode** (Saisonzeile der Vorgabe-Matrix, E53: „aus“ außerhalb), an Gebäude und Zone (`Tab_Konditionierungskalender`,
`Tab_Konditionierungsperiode`, `Tab_Konditionierungsvorgabe`); der Reiter Konditionierung bietet „aus“ in Periodenliste,
Kalenderkarte und Wochenraster. **Neu ist allein die Lesart als Freigabe** (3.1). Damit gilt:

- keine neue Spalte und kein Schemaschritt für die Freigabe; keine Kopierwege;
- die Kalenderkarte zeigt je Zone ein Jahresband „Heizen frei / Kühlen frei / beides / keines“ und die Zahl der Tage, an
  denen die Tagesart nach Bedarf entscheidet (Welle KZ);
- Projekte ohne Kühlung haben nie beide Freigaben und bleiben byte-gleich; ein Projekt, dessen Kalender nur eine Seite
  freigibt, rechnet wie heute.

### 3.4 Zonentagesart und Erzeugertagesart

Die Erzeugertagesart der reversiblen Wärmepumpe bleibt **projektweit nach K8a**, gebildet aus Heiz- und Kühlkanal von Pass 1
**nach** der Zonensperre. Bei einem Einzonengebäude ohne fremde Heizlastgänge stimmen beide überein (nach der Sperre trägt der
Tag nur eine Seite). Sonst kann eine Zone im Heizbetrieb an einem Kühltag der Wärmepumpe stehen: Dann decken die übrigen
Wärmeerzeuger (Kessel, BHKW, Heizstab); was offen bleibt, ist auf AK1/AK2 Restbedarf, auf AK3 wirkt es über die Schranke —
die Zone wird kühler (Grund `UMSCHALTUNG`). Spiegelbildlich für eine kühlende Zone an einem Heiztag: Kältemaschine oder
Kältespeicher, sonst Kälte-Restbedarf bzw. auf AK3 wärmer.

### 3.5 Kennzahlen und Rechenzeit

Neu je Gebäude und Projekt: **Tage mit Sperre der Gegenseite** (Probetag zeigte beide Seiten) und die gesperrte Energie des
Probetags (Raumheizung an Kühltagen, Raumkühlung an Heiztagen). Rechenzeit: Sicherung am Tagesbeginn (gering) und Neurechnung
der Mischtage — bei den Referenzprojekten 9–12 von 365 Tagen, also rund +2–5 % des Gebäudelaufs, nur bei Projekten mit Kühlung.

## 4. Architektur des Kreises

### 4.1 Laufordnung mit AK3-K

Je Stunde im AK3-Weg, im selben Iterationsrahmen `Anlagenkopplung`:

1. **Bedarfsnaht:** Wärmeschranke (wie AK3, jetzt mit Kühltag der Wärmepumpe) und **Kälteschranke** stellen; beide lesen
   den Zustand am Stundenbeginn und schreiben nichts. Die Zonentagesart aus Pass 1 gilt als Freigabe.
2. **Gebäudeschritt** mit beiden Schranken — der Stepper rechnet Heizen und Kühlen zugleich.
3. Abbruch prüfen (4.4); sonst nächster Durchlauf mit nachgezogenen Schranken.
4. Nach der Konvergenz: **Wärmestunde** (Phasen A–G der Kaskade) wie in AK3.
5. **Kältestunde:** die aus `Kaeltekaskade.Rechnen` herausgelöste `StundeRechnen(h)` mit dem eben gerechneten
   `Heizzeitanteil[h]` der Wärmestunde.
6. **Festschreiben** von Kältespeicher, Takt und Zählern der Kälteerzeuger.

Außerhalb von AK3 bleibt der Jahreslauf der Kältekaskade; er ruft dieselbe Stunde (Festlegung 16).

### 4.2 Kälteschranke und Naht ins Gebäude

- **`Stundenrand.MitKaelteverfuegbarkeit(schrankeW, grund, kuehlVorlaufC)`** als Gegenstück zu `MitVerfuegbarkeit`;
  `KuehlleistungMaxW` wird dafür `init`, die wirksame Grenze ist min(Gebäudegrenze, Schranke).
- Der Löser bekommt den Kühlgrund `Verfuegbarkeit` neben `KuehlleistungMax`; `Begrenzungsgrund` bekommt `Umschaltung`.
- Wirkt die Schranke, rechnet der Abschnitt den vorhandenen Fall `Kuehlgrenze`: der Raum wird wärmer, die
  Komfortkennzahlen der Kühlseite steigen. Neu sind der Zähler „Kälteschranke gegriffen“ (analog `StundenAnDerSchranke`)
  und der Kühlgrund in `FaelleZaehlen`.

### 4.3 Kälteangebot (Kapazitätsnaht)

**`IKaelteerzeugerkapazitaet.Abfragen(h, kuehlVorlaufC)`**, zustandsfrei wie `IErzeugerkapazitaet`:

- Wärmepumpe im Kühlbetrieb (nur am Kühltag der Wärmepumpe): `Zeitanteil[h] · Kennlinie.Auswerten(Quelltemperatur[h]).Pkuehl`
  am festen Kühlvorlauf, dazu die freie Sole-Kühlung bis `Kuehl_Frei_Leistung_kW`;
- Kältemaschine: `Maschine.Stunde(h, last)` (zustandsfrei, liest nur die Rückkühlreihe), freie Kühlung des Rückkühlers als
  Kapazität;
- **Kältespeicherleser** am Stundenbeginn: min(SOC, Entladeleistung) je Kältespeicher — eine neue lesende Methode neben
  `Speicherleser`, der heute auf `BedientKanal(HEIZUNG)` filtert;
- Vorrang auf der Kälteseite: Prozesskälte vor Raumkühlung, sofern ein Prozesskältekanal gerechnet wird; der Vorrang des
  Brauchwassers am Kühltag wirkt über den Zeitanteil der Wärmepumpe (4.5).

### 4.4 Abbruch, Fallwechsel, Fehler

Zum Abbruchmaß der Wärme (`dS ≤ 0,1 W`, `dV ≤ 0,05 K`) kommt **`dS_kaelte ≤ 0,1 W`** (ADR-005, Kühlleistung wie Heizlast);
`Abweichung` vergleicht auch den Kühlgrund. Pendelregel und Höchstzahl 20 aus AK3 gelten für beide Seiten; Fallwechsel
Kühlen/Heizen zählen in dieselbe Statistik. Nichtkonvergenz bleibt ein benannter Fehler. Weil die Zonentagesart fest ist,
pendelt keine Zone zwischen Heizen und Kühlen.

### 4.5 Umschaltung der Wärmepumpe als Verfügbarkeitsgrenze

- Am Kühltag der Wärmepumpe mindert sich die Wärmeschranke um ihren Anteil; am Heiztag ist ihr Kälteanteil null. Der
  Anlagengrund `UMSCHALTUNG` steht nur, wenn gerade dieser Wegfall die Schranke bindet; gebäudeseitig trägt die Stunde nach
  der Paarungsregel `VERFUEGBARKEIT`, der Anlagengrund reist daneben (Anlagenkopplung 5.3).
- **Zirkularität des Zeitanteils:** Der Heizzeitanteil der Stunde (Brauchwasser zuerst) steht erst nach der Wärmestunde
  fest. Die Kälteschranke arbeitet deshalb mit einer **Vorrangschätzung** aus `Stundenvorrang.BrauchwasserKw`/`ProzessKw`
  am Stundenbeginn; die echte Kältestunde rechnet mit dem wirklichen Heizzeitanteil. Eine Abweichung erscheint als
  Kälte-Restbedarf und wird gezählt, nicht nachiteriert.

### 4.6 Zustand im Probeschritt

| Zustand | Regel in AK3-K |
|---|---|
| SOC und Ladephase der Kältespeicher | gelesen am Stundenbeginn, geschrieben erst in der Kältestunde |
| Takt je Kälteerzeuger (`LetzteKuehlstunde`, `Starts`, `Taktstunden`, Summen) | Abfrage ohne Schreiben; `+=` nur in `StundeRechnen` |
| Tagesart der Zonen und der Wärmepumpe | aus Pass 1, fest vor der Schleife |
| Erdsonde mit Rückspeisung | Rückspeisung bleibt Jahresreihe aus Lauf 1 für Lauf 2 (Vorjahresgröße); `Sondenquelle` liest am Stundenbeginn wie in AK3 |
| freie Kühlung (Sole, Rückkühler) | zustandsfrei aus Quelltemperatur bzw. Rückkühlreihe |

### 4.7 Kältestrom und BHKW

Der Kältestrom geht im AK3-Weg **wie heute** nach der Kaskadenschleife als Jahresreihe in den Reststrom
(`Kaeltestrom_Stufenrechnung_stuendlich`, ohne eigenen Zähler); ein stromgeführtes BHKW in der Schleife sieht ihn nicht.
Ihn je Stunde ins BHKW zu führen, änderte Stromreihen, PV-Eigenverbrauch und Stromspeicher und ist ein eigener Gegenstand
(Abschnitt 10). Die Regel aus `FeldvorgabeAusLauf` (Taktstrom und Hilfsstrom herausgerechnet) bleibt.

### 4.8 Rechenzeit

Gemessen (Release-Referenzlauf, je Projekt dreimal, Wandzeit): Die Kälteseite kostet heute 0,05–0,3 s je Projekt (3–15 %);
AK3 kostet bei 1058 gegen 1056 +0,7 s (Faktor 1,4; 3,21 Durchläufe im Mittel, höchstens 7, 407 Fallwechsel, zwei
Feldläufe). Für AK3-K neu: Kälteabfrage je Durchlauf (vernachlässigbar), Zusatzdurchläufe in den 626 Kühlstunden (7 %) dort,
wo die Schranke greift (+1–2 je Stunde), die Zerlegung der Kältekaskade (gleich teuer). **Erwartet Faktor 1,05–1,15 gegen
AK3 heute (1058: +0,05 bis +0,3 s), obere Schätzung 1,3**; die feste Zonentagesart nimmt der Umschaltung das Pendeln. Dazu
die Zonensperre (3.5). Die Grenze aus E102 (Faktor 3) bleibt weit.

## 5. Festlegungen (benannt, Widerspruch möglich)

Was ohne weiteren Anwenderentscheid festgelegt wird; Widerspruch ist möglich, bis die zugehörige Welle beauftragt ist.

**Tagesbetriebsart je Zone (E104, Q-AK3K-4 und -7)**

1. In einer Zone wird an einem Tag nie geheizt und gekühlt; die Sperre gilt **auf allen Stufen** (ohne Kopplung, AK1, AK2,
   AK3) und **je Zone**. Der Tagesbilanz-Weg rechnet keine Kühlung und bleibt unberührt.
2. Die Tagesart wählt innerhalb der Kalenderfreigabe (3.1): beide frei → Tagessummen des unbegrenzten Probetags, nur eine
   frei → diese, keine → keine Raumkonditionierung.
3. Die Zonentagesart entsteht in Pass 1 am Tagesbeginn aus dem unbegrenzten Probetag; neu gerechnet wird nur ein Tag, an dem
   eine Zone mit beiden Freigaben beide Seiten zeigt (3.2). *Alternative, benannt nicht gewählt:* Pass 1 zweimal je Jahr —
   doppelte Gebäudezeit, und die Tagessummen stammten aus einem anderen Verlauf.
4. **Prozesswärme und Prozesskälte** sind nicht gesperrt und dürfen in derselben Stunde laufen.
5. **Brauchwasser wird wie Prozesswärme behandelt: nicht gesperrt** (Festlegung ohne Anwenderwort; Widerspruch möglich).
6. Die Erzeugertagesart bleibt projektweit nach K8a, gebildet aus den Kanälen von Pass 1 nach der Zonensperre; eine heizende
   Zone am Kühltag der Wärmepumpe decken die übrigen Wärmeerzeuger, sonst Restbedarf (AK1/AK2) bzw. kühler (AK3) (3.4).
7. Die Freigabe liest das vorhandene „aus“ der Heiz- und Kühlsollwertkalender samt Heiz- und Kühlperiode; kein Schemaschritt,
   keine Kopierwege; neu ist das Jahresband der Freigabe im Reiter Konditionierung.

**Stufe und Fehler**

8. Die **Rückwirkung** (Kälteschranke, Kältestunde im Kreis, Anlagengrund `UMSCHALTUNG`) wirkt **nur auf der Stufe AK3**
   (E104, Q-AK3K-5).
9. Die Fehlerbehebungen 1.1 (a) und (b) gehören zu AK3-K, wirken nur auf AK3 und bewegen nur 1058.
10. Die Laufmeldung „gebaut ist die Stufe AK1“ entfällt auf AK3; der Lauf meldet „geschlossener Kreis mit Kälteseite“; die
    Ressource wird in beiden Sprachen nachgezogen.

**Angebot**

11. **Kälteschranke** (E104, Q-AK3K-2 als Festlegung nach Empfehlung): Kapazität der am Tag verfügbaren Kälteerzeuger am
    festen Kühlvorlauf — Wärmepumpe aus der Kühlkennlinie an der Quelltemperatur der Stunde mal Zeitanteil, Kältemaschine
    aus ihrer Kennlinie an Rückkühl- und Kaltwassertemperatur, freie Kühlung als Kapazität — plus Kältespeicher am
    Stundenbeginn (min(SOC, Entladeleistung)) über eine neue lesende Methode ohne Nebenwirkung.
12. Ein Projekt **nur mit Kältemaschine** bekommt seinen Anlagen-Kühlvorlauf aus deren `Kuehl_Vorlauf` (heute NaN, weil
    `WPCtrl.KuehlVorlaufDesKaeltekanals` nur Wärmepumpen liest).
13. `UMSCHALTUNG` wird im `Begrenzungsgrund` der Gebäudeseite angelegt; die Paarungsregel bleibt (W3). **Wie gebaut:** Der
    Wert `Begrenzungsgrund.Umschaltung` ist angelegt, wird nach der Paarungsregel gebäudeseitig aber nicht gesetzt — die
    Gebäudestunde trägt `VERFUEGBARKEIT`, der Anlagengrund `UMSCHALTUNG` reist daneben (4.5).
14. Der Zeitanteil der Kälte geht als Vorrangschätzung am Stundenbeginn in die Schranke; eine Abweichung zur echten
    Kältestunde wird als Kälte-Restbedarf gezählt, nicht nachiteriert.

**Kreis**

15. Abbruch `dS_kaelte ≤ 0,1 W` neben den Maßen der Wärme; Kühlgrund in `Abweichung`; Pendelregel und Höchstzahl wie AK3.
16. Die Kältekaskade wird in `StundeRechnen(h)` zerlegt; der Jahreslauf ruft dieselbe Stunde, sodass außerhalb von AK3
    bitgleich gerechnet wird.
17. Der Kältestrom bleibt eine Jahresreihe nach der Kaskadenschleife; das BHKW in der Schleife sieht ihn nicht (4.7).
18. Die Rückspeisung der Erdsonde bleibt Jahresreihe aus Lauf 1 für Lauf 2; die Kältestunde liefert sie im AK3-Weg.

**Leser, Kennzahlen, Basis**

19. **Leser der Kältereihe aus Pass 1** werden in K1 inventarisiert und je Leser auf Pass 1 oder Kreisreihe festgelegt:
    `Kaeltekaskade.Rechnen(Kaeltebedarf)`, `TagesbetriebsartBestimmen`, `HeizkanalAnKuehltagenMelden`, `Kuehlkreis`, Monats-
    und Dauerlinienreihen, `SimulationRunner`-Reihen der Kälte.
20. Kennzahlen (Tage mit Sperre der Gegenseite und gesperrte Energie; auf AK3 Stunden an der Kälteschranke, Umschaltstunden,
    Kälte-Restbedarf) stehen wie die Kreiskennzahlen von AK3 (Schritt 198) als Ergebnisspalten im Projektergebnis — **S1** (Schritt 201),
    NULL außerhalb ihres Geltungsbereichs.
21. Bis zur Basiswelle K5 rechnen Zonensperre und Kreis nur mit je einem **Kernschalter** (ohne Schema, Vorgabe aus, Muster
    W-I); jedes Gate bis dahin ist „Referenzlauf 23/23 byte-gleich gegen R42“.

## 6. Referenzprojekt und Basis

- **Basis R43** `2026-10-07_R43_Kaelteseite_AK3K` ist eingefroren (vierundzwanzig Projekte, 759 CSV, 5 196 Skalare;
  gegen R42 596 von 718 CSV byte-gleich, abweichend allein 1017, 1047, 1055, 1056 und 1058, 1059 neu; der Bivalenzpunkt
  von 1047 und 1056 fällt von 17,79 auf −10,0 °C, weil an Kühltagen kein Raumheizbedarf mehr offen bleibt; 1059 steht
  190 Stunden an der Kälteschranke; Einzelheiten in `Referenzlaeufe/LIESMICH.md`). Die Planung dazu: Es ändern sich **1017, 1047, 1055, 1056** (Zonensperre: rund 10 Tage und 25 Stunden je
  Projekt, 1.2) und **1058** (Zonensperre und Kreis: Kühlkanal folgt der Kreisreihe, +1,95 kWh; Kälteschranke, Komfort der
  Kühlseite, heute 43 Kh; Erdsonde und Rückspeisung); neu kommt **RP-AK3K**; die übrigen **18** Projekte bleiben
  byte-gleich. **1017, 1047 und 1058 stehen in der CI-Auswahl** von `kern.yml`; Einschalten beider Schalter und Basiswechsel
  fallen in denselben Push.
- **RP-AK3K** (E104, Q-AK3K-6 (a)): Kopie von 1058 auf dem Kopierweg des Programms (Skriptmuster
  `Referenzlaeufe/Skripte/referenzprojekt_1058_ak3.py`) mit Kältemaschine und Kältespeicher aus 1055, die Wärmepumpe heizt
  nur, die Kältemaschine bewusst **unterdimensioniert** (sonst greift die Schranke nie: 1055 deckt 100 %). Nummer bei der
  Anlage; **nicht** in der CI-Auswahl.
- **Neue Einfrierregeln** (in `CLAUDE.md` und `Referenzlaeufe/LIESMICH.md` mit K5):
  - **Tagesbetriebsart je Zone und Kalenderfreigabe:** die Regel 3.1 (Kriterium, Gleichstand, Probetag), an den Gebäuden und
    Zonen der Referenzprojekte mit Kühlung die Heiz- und Kühlsollwertkalender samt „aus“-Perioden, Heiz- und Kühlperiode;
  - was die Kälteschranke eines AK3-Referenzprojekts bildet — Kühlkennlinie samt Stützstellen, `Kuehl_Frei*`, Kältemaschine
    und Kältespeicher eines **gekoppelten** Projekts; die Erzeugertagesart (K8a aus Pass 1 nach der Zonensperre);
  - das Anlegen oder Entfernen von RP-AK3K.
- **Tests und Wachen:** mitzuziehen `Ak3ReferenzprojektWacheTests` (Werte 1058), die Wachen der
  Referenzprojekte mit Kühlung (`ReferenzprojektKaelteerzeugerTests`, `KaeltemaschineReferenzprojektWacheTests`,
  `FahrplanReferenzprojektWacheTests` für 1056), `KaeltegangLaufTests` und `KaelteerzeugerTests` (Kältekaskade je Stunde
  bitgleich zum Jahreslauf), `AnlagenkopplungAngebotTests`, `AnlagenkopplungOrakelTests` (O1k/O2k),
  `AnlagenkopplungKaelteseiteTests` (Schranke in Schritt K, Grund `VERFUEGBARKEIT`/`UMSCHALTUNG`), `KuehlungJeZoneTests`
  und `KomfortkennzahlenTests` (Zonensperre); neu `ZonentagesartTests` (Regel 3.1, Mehrzonen A heizt/B kühlt, Freigabe,
  Prozess und Brauchwasser frei) und `Ak3KaelteReferenzprojektWacheTests`.

## 7. Wellenplan

Je Welle ein Opus-Auftrag mit höchstens rund 150 Werkzeugaufrufen; das Gate fährt die Orchestrierung nach dem Merge. Bis K5
ist jedes Gate „Referenzlauf 23/23 byte-gleich gegen R42“ mit beiden Schaltern aus. Schemaschritt: allein **S1**
(Ergebnisspalten nach Festlegung 20) in K4, gebaut als Schritt **201**.

| Welle | Inhalt | Abnahme | Basis | PT |
|---|---|---|---|---|
| **K0** Papiere | E104 eintragen; Anlagenkopplung 5.3, 7.3, 7.4, Kühlkonzept 3.5, 5.2 und Konditionierungsprofile 3.1 nachziehen (W1–W8, Freigabe) | Registerzeile; `DokumentationLinkWacheTests` grün | nein | 0,5–1 |
| **K1** Fehler und Kältestunde | Kernschalter AK3-K; Fehler 1.1 (a) und (b) samt Laufmeldung; Leserinventar (Festlegung 19); `Kaeltekaskade.StundeRechnen(h)`, Aufruf je Stunde nach der Wärmestunde im AK3-Weg | „Kältestunde bitgleich zum Jahreslauf“; Probe „Kühlkanal = Kreisreihe, Bedarfsprobe grün“; Kälteklassen grün; Referenzlauf 23/23 byte-gleich | nein | 2–3 |
| **KZ** Zonentagesart und Freigabe | Kernschalter Zonensperre; Freigabe aus den Kalendern (3.1, 3.3); Probetag am Stepper, Neurechnung der Mischtage, Sperre über „aus“ je Tag; Erzeugertagesart nach der Sperre; Kennzahlen im Lauf; Jahresband der Freigabe im Reiter Konditionierung samt Ressourcen beider Sprachen | `ZonentagesartTests`; „ohne Kühlung bitgleich“; „nur eine Freigabe bitgleich“; Probelauf mit Schalter ein: nur 1017, 1047, 1055, 1056, 1058 weichen ab; bunit; Referenzlauf 23/23 byte-gleich (Schalter aus) | nein | 2–3,5 |
| **K2** Kälteangebot und Naht | `IKaelteerzeugerkapazitaet`, Kältespeicherleser; `Stundenrand.MitKaelteverfuegbarkeit`, Kühlgrund `Verfuegbarkeit`, `Begrenzungsgrund.Umschaltung`; Wärmeschranke kennt den Kühltag; Kühlvorlauf aus der Kältemaschine | Proben „ohne Grenze bitgleich“, „Kühltag: Wärmepumpe fehlt der Heizseite, Kessel deckt Brauchwasser“, „Kältespeicher überbrückt“; Referenzlauf byte-gleich | nein | 2–3,5 |
| **K3** Kreis mit Kälte | Kälte im Durchlauf von `Anlagenkopplung`, Abbruch 0,1 W, Fallwechsel, benannter Fehler; Orakel O1k/O2k; Zonentagesart als feste Vorgabe | Orakel; „Kälte-Restbedarf 0“ im einfachen Fall; Laufzeit Faktor 3 (E102); Referenzlauf byte-gleich | nein | 1,5–2,5 |
| **K4** Schema, Oberfläche, Bericht | S1 (Ergebnisspalten), Kennzahlen in Bedarfsdialog und Bericht, Ressourcen beider Sprachen samt `designer_neu.py` | Schema-, bunit-Tests; Windows-Schale auf Linux kompiliert; `SqlDialektPruefer` | nein | 1–2 |
| **K5** Referenzprojekt und R43 | beide Schalter ein und entfernt; RP-AK3K (Skript, Wache); Einfrierregeln; Basis R43 | abweichend allein 1017, 1047, 1055, 1056, 1058 (erklärt), RP-AK3K neu, übrige 18 byte-gleich; CI-Auswahl unverändert | **ja** | 2–3 |
| **K6** Papiere und Wiki | Konzepte wie gebaut, Register, Status, Protokoll, Wiki-Quellen (Konditionierung, Kühlung), Logbuch-Entwurf | Link-Wache, Wiki-Gegenlesemuster | nein | 0,5–1 |
| | **Summe** | | | **11,5–19,5** |

K1 kommt zuerst, weil die Fehlerbehebung (a) die Kältereihe festlegt; KZ hängt an keiner Datei von K1 und kann neben ihr
laufen, muss aber vor K3 stehen (der Kreis übernimmt die Zonentagesart); K2 setzt K1 voraus, K3 setzt K2 und KZ voraus; K4 kann
nach K3 neben K6 beginnen. **Gegenüber dem ersten Schnitt (9–16 PT):** die Zonensperre mit Freigabe (+2–3,5 PT) und die
größere Basis (+0,5 PT) kommen hinzu; die Kühlkurve entfällt (E104).

### 7.1 Wie gebaut

Alle Wellen K0 bis K6 sind gebaut; die Kernschalter Zonensperre und AK3-K sind mit K5 entfallen, beide Teile rechnen fest.
Basis **R43** `2026-10-07_R43_Kaelteseite_AK3K` (vierundzwanzig Projekte, neu **1059** = RP-AK3K: Kopie von 1058, die
Wärmepumpe heizt nur, Kältemaschine 10 kW mit Trocken-Rückkühler und Kältespeicher, 190 Stunden an der Kälteschranke;
nicht in der CI-Auswahl). Abweichungen vom Plan:

- **Sperrzeit vor Umschaltung:** In einer Sperrstunde der Wärmepumpe bleibt der Anlagengrund `SPERRZEIT`; `UMSCHALTUNG`
  steht nur außerhalb der Sperrstunden.
- **Schemaschritt S1** ist als Schritt **201** gebaut (`Ak3KSchema` = `ProjektdateiImportSchema.SCHRITT + 1`, sieben nullbare
  Kennzahlen an `Tab_ErgebnisEnergiebedarf`).
- **Vorrangschätzung verbessert:** Ohne Freigabe der Heizseite (Sperre, Zeitprogramm, Umschaltung) rechnet sie der
  Wärmepumpe keinen Vorrang zu, und Erzeuger vor ihr in der Kaskade tragen den Vorrang zuerst; die Probe mit Brauchwasser
  am Kombipuffer ist danach weder zu knapp noch zu weit.
- **Kälte-Bestandstests:** Zwölf Bestandstests der Kälte rechnen ihre Werte mit der Zonensperre in neuer Fallbildung.
- **Begrenzungsgrund:** `Begrenzungsgrund.Umschaltung` ist angelegt, gebäudeseitig gilt die Paarungsregel (Festlegung 13).

## 8. Fragen an den Anwender

**Entschieden mit E104 (Anwender, 07.10.2026), alle wie empfohlen:** Q-AK3K-1 (a) Umschaltung je Tag aus Pass 1 (K8a);
Q-AK3K-2 (a) bleibt Festlegung nach Empfehlung (Festlegung 11); Q-AK3K-3 (b) keine raumgeführte Kühlkurve jetzt, später als
eigener Gegenstand, kein Schemaschritt dafür; Q-AK3K-4 neu gefasst nach der Vorgabe des Anwenders „Das Konzept sperrt an
einem Kühltag die ganze Heizseite: Das sollte so sein, da Kühlung und Heizung gleichzeitig in einer Zone nicht sinnvoll ist.
Für Prozesskälte und Prozesswärme soll Heizen und Kühlen gleichzeitig möglich sein.“; Q-AK3K-5 (a) Rückwirkung nur auf AK3;
Q-AK3K-6 (a) 1058 und neu RP-AK3K, nicht in der CI; Q-AK3K-7 (b) „Sperre der Raumheizung am Kühltag soll immer gelten,
solange nicht verschiedene Zonen gekühlt/geheizt werden.“ Dazu die Vorgabe „Der Kalender für Kühlen und Heizen soll die
Möglichkeit haben, die Kühlung/Heizung ein/aus zu schalten.“ (Abschnitt 3.3). Die Tabelle bleibt als Grundlage stehen.

| # | Frage | Optionen und Folgen | Empfehlung | Entscheid (E104) |
|---|---|---|---|---|
| **Q-AK3K-1** | **Wie wird die reversible Wärmepumpe im Kreis umgeschaltet?** | (a) je Tag nach K8a, Tagesart aus Pass 1; keine Iteration über den Tag · (b) je Tag aus den gekoppelten Summen des Vortags: kausal, ein Tag Verzug, Tagesart hängt von der Lösung ab · (c) je Stunde nach dem größeren Bedarf: weicht von K8a ab, alle Kälteprojekte müssten folgen, bis zu zwölf Umschaltungen am Tag | (a) | **(a)** |
| **Q-AK3K-2** | **Was ist die Kälteschranke?** | (a) Kapazität der am Tag verfügbaren Kälteerzeuger am Kühlvorlauf samt freier Kühlung plus Kältespeicher am Stundenbeginn · (b) wie (a) ohne freie Kühlung: unterschätzt Trockenkühler-Nächte · (c) Nennkälteleistung: ohne Vorlauf- und Quellenbezug | (a) | **(a)** als Festlegung 11 |
| **Q-AK3K-3** | **Raumgeführte Kühlkurve** (Gegenstück zu H2)? | (a) ja: Spalte `Kuehlkurve_Raumeinfluss` am Gebäude, nur AK3, Schemaschritt, Dialogfeld, Einfrierregel, +1–1,5 PT · (b) nein: fester Vorlauf, Kälteseite je Stunde explizit lösbar, kein Schema | (b) jetzt, (a) später | **(b)**; (a) als eigener Gegenstand |
| **Q-AK3K-4** | **Gleichzeitiger Heiz- und Kühlbedarf** | (a) beide Schranken unabhängig, die Wärmepumpe dient der Seite ihres Tags · (b) Rang Heizung vor Kälte in derselben Stunde | (a) | **neu gefasst:** in einer Zone nie beides am selben Tag, die Tagesart je Zone entscheidet; verschiedene Zonen dürfen verschieden; Prozesswärme, Prozesskälte (und als Festlegung Brauchwasser) frei; die Wärmepumpe bleibt je Tag Heiz- oder Kältemaschine (Abschnitt 3, Festlegungen 1–6) |
| **Q-AK3K-5** | **Wirkt die Rückwirkung nur auf AK3?** | (a) nur AK3: AK1/AK2 ohne Schranke · (b) `UMSCHALTUNG` auch im Profilweg AK2: Pass 1 muss immer laufen und die Kälte einbeziehen | (a) | **(a)**; die Zonensperre gilt davon getrennt auf allen Stufen (Q-AK3K-7) |
| **Q-AK3K-6** | **Welches Referenzprojekt hält den Kühlfall des Kreises?** | (a) 1058 und neu RP-AK3K (Kopie von 1058 mit Kältemaschine und Kältespeicher aus 1055, Kältemaschine zu klein), RP-AK3K nicht in der CI · (b) Kopie von 1055: es fehlen Stufe, Heizkreis, Kühlübergabe, Puffer · (c) nur 1058 erweitern: bewegt das CI-Projekt doppelt | (a) | **(a)**, nicht in der CI |
| **Q-AK3K-7** | **Gilt die Sperre der Raumheizung am Kühltag nur auf AK3 oder auf allen Stufen?** | (a) nur AK3 (wie die Rückwirkung): allein 1058 ändert sich · (b) auf allen Stufen: 1017, 1047, 1055, 1056, 1058 ändern sich an je 9–12 Tagen mit rund 25 Stunden (1.2), darunter die CI-Projekte 1017, 1047, 1058 | (a) | **(b), je Zone** („solange nicht verschiedene Zonen gekühlt/geheizt werden“) |

## 9. Risiken

| Risiko | Wirkung | Gegenmittel |
|---|---|---|
| **Leser der Kältereihe aus Pass 1 nach dem Kreis** (Hauptrisiko) | Kaskade, Bericht oder Meldung rechnen still mit der falschen Reihe | Inventar in K1 (Festlegung 19); Probe „Kühlkanal = Kreisreihe“ |
| Zonensperre bewegt CI-Projekte vor K5 | `kern.yml` rot (1017, 1047, 1058) | Kernschalter bis K5 (Festlegung 21) |
| Probetag und Neurechnung verlieren Zustand | Tag beginnt falsch, Jahresreihe springt | Sicherung am Tagesbeginn über die Zustandssicherung des Steppers; Probe „Tag ohne Mischung bitgleich“ |
| Zonentagesart und Erzeugertagesart weichen ab (Mehrzonen, fremde Lastgänge) | Restbedarf bzw. kühlere Zone an Kühltagen der Wärmepumpe | benannt (3.4), gezählt; Kessel/BHKW decken |
| Brauchwasser frei am Kühltag (Festlegung 5) | Wärmepumpe teilt den Tag zwischen Brauchwasser und Kälte wie heute | Festlegung benannt, Widerspruch möglich |
| Kühlkanal ohne Bedarfsprobe nachgeführt | Bedarfs- oder Deckungsprobe bricht, Lauf ungültig | Nachführen in einem Zug (1.1 (a)); Probe nach dem Kreis |
| Zustand der Kälteerzeuger im Probeschritt geschrieben | Doppelzählung von Starts, Takt, Kälte | Abfrage ohne Schreiben, `+=` nur in `StundeRechnen` (4.6) |
| Vorrangschätzung des Zeitanteils weicht ab | Kälte-Restbedarf trotz gekoppelter Stunde | gezählt und berichtet (Festlegung 14) |
| Basis-, Schritt- oder Projektnummer kollidiert mit einer anderen Sitzung | Merge-Konflikt | R43 angemeldet; S1 und Projektnummer vor dem Bau anmelden |

## 10. Nicht in AK3-K

Raumgeführte Kühlkurve (E104, eigener Gegenstand), Umschaltung der Wärmepumpe je Stunde, Tagesart je Zone innerhalb des
Tages, Kältestrom je Stunde ins BHKW und in die Stromreihen, stündliche Rückspeisung der Erdsonde ohne zweiten Feldlauf
(Rechenwegentscheid, bis Faktor 0,5 Rechenzeit), Vorlaufbezug der Entnahme aus Kältespeichern, Kältenetz und Pumpen,
Kälte-Rückwirkung auf AK1/AK2, Kühlung im Tagesbilanz-Weg.

## 11. Logbuch-Entwürfe

Je ein Satz, veröffentlicht mit dem gebündelten Wiki-Upload (Regel:
[Konzept Hilfesystem](../Konzept_Hilfesystem_Wikidokumentation.md) 13.3); Datum und Versionsnummer setzt der Anwender.

- (a) Eine Zone wird an einem Tag entweder geheizt oder gekühlt, nie beides; der Kalender schaltet Heizen und Kühlen
  frei, das Jahresband der Freigabe steht im Reiter Konditionierung.
- (b) Auf der Kopplungsstufe AK3 wirkt die Kälteanlage auf das Gebäude zurück: Eine zu kleine Kälteanlage zeigt sich als
  wärmerer Raum.
- (c) Der Bedarfsdialog und der Bericht zeigen die Kennzahlen der Zonensperre und der Kälteseite im Kreis.
