# Befund: Kalenderbedienung im Reiter Konditionierung (09.10.2026)

Anlass ist der Anwenderwunsch vom 08.10.2026 (Welle E, Punkt E2): ein einfach bedienbarer Kalender, in dem man
(1) fertige Profile verwendet, (2) feste Zeiten und Einstellungen — Wochenende, Ferien, Saison — schnell festlegt,
und zwar als **Datumsbereiche** statt als Zeilen einer Matrix, (3) **einzelne Tage** konfiguriert und (4) Wochen-
und Monatsprofile **kopiert**; dazu die Frage, ob es eine **grafische** Bearbeitung gibt. Dieses Papier hält den
heutigen Stand dagegen, beschreibt zwei Varianten und stellt die Fragen an den Anwender. Es legt nichts fest und
enthält keinen Umsetzungscode. Das spielbare Mockup zu beiden Varianten liegt unter
[`../Mockups/Konditionierung_Kalender.html`](../Mockups/Konditionierung_Kalender.html).

## 1 Heutiger Stand

Grundlage ist das [Konzept Konditionierungsprofile](../Konzept_Konditionierungsprofile_EPOS-Plan.md):

- **Kalendermodell (3.2):** je Größe ein Kalender aus drei Ebenen — Grundangabe, Standardwoche (168 Zellen, Zahl
  oder „aus") und Perioden. Eine Periode gilt ganze Tage von Beginn bis Ende (Tag 1…365 im Gemeinjahr, über den
  Jahreswechsel erlaubt) oder ist eine Feiertagsregel (die neun bundeseinheitlichen Feiertage, gegen das
  Referenzjahr aufgelöst); sie trägt einen Wert, „aus“, eine eigene Woche oder „wie Wochentag X“. Die ranghöchste
  Periode gewinnt. Saison (Heiz- und Kühlperiode) ist ein Datumspaar je Spalte, aus dem der Generator eine Periode
  „aus“ über die Tage außerhalb bildet.
- **Vorgabe-Matrix (3.3, 7.2):** fünf Spalten (Heizen, Kühlen, Lüftung, Geräte, Personen), Zeilen Nennwert, Tag,
  Nacht mit Zeiten, Wochenende, Ferien, Saison; daneben die **Ferienzeiträume des Gebäudes** (vier Datumspaare
  `Ferienbeginn/-ende_1…4`), die für alle Spalten gelten. Der Generator macht daraus Kalender; ein angelegter
  Kalender geht vor.
- **Vorlagen (3.5, 7.4):** je Größe eine Liste (ausgeliefert: Wohnen, Büro, Schule u. a.), „Übernehmen“ je Karte,
  „alle Größen“ in der Kopfzelle der Zeile „Vorlage“ (E57), „Als Vorlage speichern…“, „Kopieren nach …“ zwischen
  Größen (Heizen → Kühlen, Geräte ↔ Personen).
- **Kalenderkarte im Einzelnen (7.5):** Zeitfenster-Werkzeug (Tage, von, bis, Wert), Wochenraster 7 × 24, Periodenliste
  mit Bearbeiten, Werkzeuge Feiertage und Zeitstruktur, Wochenvorschau und Teppichbild des Jahres.
- **Datenmodell (5.1, 5.2):** `Tab_Konditionierungskalender` (je Eigentümer und Größe; Woche als 168-Werte-Text) und
  `Tab_Konditionierungsperiode` (Rang, Art `ZEITRAUM`/`FERIEN`/`FEIERTAG`/`BETRIEBSPAUSE`, Beginn/Ende als Jahrestag
  oder Feiertagsregel, Angabe Wert/aus/Woche/`WieWochentag`).

**Was die vier Wünsche davon schon abdecken:**

| Wunsch | heute vorhanden | Stelle |
|---|---|---|
| (1) fertige Profile | Vorlagen je Größe, „alle Größen“ übernimmt eine gleichnamige Vorlage in einem Schritt | 3.5, 7.4, E57 |
| (2) Wochenende, Ferien, Saison schnell | Saison als Datumspaar je Spalte; Ferien als vier Datumspaare des Gebäudes für alle Spalten; Wochenende als Matrixzeile (fest Sa+So) | 3.2, 3.3, 7.2 |
| (3) einzelne Tage | Perioden mit Beginn = Ende, Feiertagsregeln, „wie Wochentag X“ — aber nur in der Periodenliste der aufgeklappten Karte und je Größe | 3.2, 5.1, 7.5 |
| (4) Kopieren | „Kopieren nach …“ einer Vorlage in eine andere Größe; im Wochenraster keine Tageskopie, keine Wochen- oder Monatskopie | 3.5, 7.4 |
| grafisch | Wochenraster 7 × 24 (Zellen), Teppichbild als Anzeige; kein grafisches Bearbeiten im Jahr | 7.5 |

## 2 Die vier Wünsche und die Lücken

1. **Fertige Profile** sind da, stehen aber an zwei Orten (Liste je Karte, Kopfzelle der Matrix). Lücke: Der Weg
   „ein Profil für das ganze Gebäude“ ist eine Abkürzung neben der Matrix, nicht der Einstieg.
2. **Datumsbereiche statt Zeilen:** Die Ferien sind schon Datumsbereiche, aber höchstens vier und nur in den
   Zusatzzeilen unter der Matrix; Wochenende ist keine Datumsangabe, sondern eine Matrixzeile mit festem Sa+So; die
   Saison steht als Zeile je Spalte. Lücken: (a) beliebig viele benannte Zeiträume, (b) das Wochenende wählbar
   (Tage), (c) eine Stelle, an der Wochenende, Ferien und Saison **einmal für alle Größen** gesetzt werden — heute
   wiederholt jede Spalte ihre Wochenend- und Ferienzelle.
3. **Tagesebene:** Einzelne Tage gehen nur über die Periodenliste einer Karte, je Größe getrennt. Lücke: eine Liste
   der Ausnahmetage für das ganze Gebäude („Brückentag 02.05.: wie Sonntag“, Feiertage eines Bundeslands) und eine
   Sicht auf das Jahr, in der man einen Tag anklickt.
4. **Kopieren:** Lücke für Tagesprofil → andere Tage, Woche → andere Woche oder Größe, Monat → anderer Monat.
5. **Grafisch:** Das Wochenraster ist bereits eine Zellenbearbeitung; es fehlen ein Jahresraster zum Malen von
   Tagesarten und ziehbare Stundenbalken für einen Tag.

## 3 Zwei Varianten

Beide Varianten setzen **Wochenende, Ferien, Feiertage und Ausnahmetage einmal für alle Größen** und lassen je Größe
nur die Werte; beide haben „Vorlage übernehmen“ für alle Größen und eine Mini-Jahresvorschau (Teppichbild).

**V1 „Jahreskalender“ (Tagesarten).** Oben Schnellfelder: Wochenende als Tageswahl (Sa+So per Knopf), Ferien als
Liste benannter Datumsbereiche (+/✕), Saison von–bis je Größe, Feiertage „Bundesland laden“. Darunter das Jahr als
Monatsraster (12 × 31), jeder Tag in der Farbe seiner **Tagesart** (Werktag, Wochenende, Ferien, Feiertag, Saison
aus, Ausnahme); Klick oder Ziehen mit einem Pinsel macht Tage zu Ausnahmen, ein Radierer gibt sie der Regel
zurück. Rechts das **Tagesprofil** der gewählten Tagesart: 24 ziehbare Balken, „ganzer Tag“, „aus“, „Profil aus
Vorlage“, „Tagesprofil kopieren nach …“ (andere Tagesart, alle übrigen, gleiche Tagesart einer Größe gleicher Einheit).

- Vorteile: Das Jahr ist sichtbar und direkt bearbeitbar — die Antwort auf „grafisch“; Wünsche 2 und 3 werden ein
  Blick und ein Klick; je Größe nur sechs Tagesprofile statt einer Woche plus Periodenliste; das Kopieren ist ein
  Tagesprofil, das Tagesarten verbindet.
- Nachteile: Unterschiedliche Werktage (Freitag kürzer) brauchen eine weitere Tagesart oder Ausnahmen; die
  Tagesart ist ein neuer Begriff neben Standardwoche und Periode; ein gemalter Tag hängt am Bezugsjahr (Wochentag
  wandert mit dem Jahr) — Ausnahmen müssen als Datum, nicht als Wochentag gespeichert werden; ein Monatsraster mit
  365 Zellen braucht auf dem Telefon Querrollen in seinem Kasten.

**V2 „Wochenprofile“ (Zuordnung nach Datum).** Links die Wochenprofile der Größe (Standardwoche, Ferienwoche,
Saison-aus-Woche, Ausnahme, eigene), jedes ein 7 × 24-Raster mit Pinsel, „Montag nach Di–Fr“, „Samstag nach
Sonntag“, „Woche kopieren“ (auch in eine Größe gleicher Einheit). Rechts die **Zuordnung** „von–bis →
Wochenprofil, gilt für alle Größen oder eine“ als Tabelle mit Datumsfeldern, darüber ein **Jahresband**, das die
Zeiträume farbig zeigt (Klick wählt Zeile und Profil); „Monat kopieren“; darunter die **Einzeltage** als Liste
(Datum, Bezeichnung, „gilt wie Sonntag“ oder „aus“) mit „Feiertage laden“.

- Vorteile: liegt nah am heutigen Modell — Wochenprofil = Standardwoche bzw. Woche einer Periode, Zeile =
  Periode, Einzeltag = Periode mit Beginn = Ende oder Feiertagsregel; jede Woche bleibt frei gestaltbar; Kopieren
  von Woche und Monat ist natürlich; das Jahresband zeigt die Zuordnung grafisch.
- Nachteile: weniger direkt — ein Tag wird über eine Tabelle zugeordnet, nicht angeklickt; mehr Profile je
  Größe (vier und mehr Wochen à 168 Zellen); „Monat kopieren“ erzeugt Zeilen, deren Rang der Anwender verstehen
  muss.

**Empfehlung:** V2 als Grundgerüst, weil es das vorhandene Datenmodell fast unverändert nutzt (Abschnitt 5) und
Wochen- wie Monatskopie trägt, **ergänzt um das Jahresraster aus V1 als Anzeige mit Klick auf einen Tag** (öffnet
den Einzeltag bzw. die Zeile) — damit bekommt der Anwender die grafische Sicht, ohne einen neuen Begriff
„Tagesart“ einzuführen. Die Schnellfelder aus V1 (Wochenende als Tageswahl, Ferienliste, Bundesland laden) gehören
in beide Wege und ersetzen die Zeilen Wochenende und Ferien der Matrix.

## 4 Offene Fragen an den Anwender

1. **Welche Variante** (V1, V2 oder die empfohlene Mischung V2 mit Jahresraster aus V1)?
2. **Sollen Wochenende, Ferien, Feiertage und Ausnahmetage immer für alle Größen gelten**, oder braucht es je
   Größe Abweichungen (etwa Kühlen ohne Ferien)? Empfehlung: gemeinsam, je Zeile „gilt für“ abwählbar wie in V2.
3. **Bleibt die Vorgabe-Matrix** als Kurzweg sichtbar, oder ersetzt die neue Bedienung sie im Reiter (die Matrix
   nur noch als Übersicht)? Empfehlung: Matrix bleibt Übersicht, Wochenende- und Ferienzeile entfallen dort.
4. **Wie viele Ferienzeiträume** braucht ein Gebäude? Heute vier; Schulgebäude haben sechs und mehr. Empfehlung:
   beliebig viele, als Perioden.
5. **Feiertage je Bundesland:** ausgeliefert für alle 16 Länder als Regeln (bewegliche über das Osterdatum),
   oder reicht „bundeseinheitlich + von Hand“? Empfehlung: alle Länder als Regeln, Wahl beim Gebäude.

## 5 Was das Datenmodell bräuchte (Befund, keine Festlegung)

- **Datumsbereiche:** tragen die Perioden schon (`Beginn`, `Ende` als Tag 1…365, über den Jahreswechsel). Die
  Ferienzeiträume des Gebäudes (`Ferienbeginn/-ende_1…4`) sind dagegen auf vier begrenzte Spalten in `Tab_Gebaeude`;
  „beliebig viele“ hieße Perioden am Gebäude statt Spalten — eine Tabelle oder ein Eigentümer „Gebäude, alle
  Größen“ in `Tab_Konditionierungsperiode`.
- **Für alle Größen gemeinsam:** Heute hängt jede Periode an einem Kalender einer Größe. Ein gemeinsamer Satz
  (Wochenende, Ferien, Feiertage, Ausnahmetage) braucht entweder einen Eigentümer „Gebäude ohne Größe“ mit einer
  Spalte „gilt für“ oder eine Kopie je Größe, die der Dialog synchron hält. Ersteres ist sauberer und braucht
  einen **Schemaschritt** (neue Eigentümerart samt Prüfregel), Letzteres keinen.
- **Wochenende wählbar:** Heute ist das Wochenende Sa+So aus der Wochenendmaske des Ortszeit-Kalenders; ein Gebäude
  mit anderen Ruhetagen bräuchte eine Angabe am Gebäude (eine Spalte oder eine Wochenmaske) — **Schemaschritt**.
- **V1 Tagesprofile je Tagesart:** gibt es nicht; abbildbar als Standardwoche (Werktag/Wochenende) und Perioden
  mit eigener Woche je Tagesart, ohne Schemaschritt — oder als neue Tabelle „Tagesprofil je Tagesart“ mit
  **Schemaschritt**. Gemalte Ausnahmetage sind Perioden mit Beginn = Ende (vorhanden).
- **V2 Wochenprofile:** die Standardwoche ist da; weitere benannte Wochen gibt es nur als Woche **in** einer
  Periode. Benannte, wiederverwendbare Wochen je Kalender (ein Profil, mehrere Zeilen) brauchen eine Tabelle der
  Wochen mit Verweis aus der Periode — **Schemaschritt**; ohne ihn kopiert jede Zeile ihre Woche.
- **Feiertage je Land:** Die Regelkennung kennt heute neun bundeseinheitliche Werte (`CHECK` in
  `Tab_Konditionierungsperiode.Feiertagsregel`); Länderfeiertage als Regel erweitern die Liste — **Schemaschritt**
  (Prüfregel), feste Feiertage sind auch ohne ihn als Zeitraum möglich.
- **Kopieren** (Tag, Woche, Monat) ist reine Dialoglogik über vorhandene Zeilen; kein Schemaschritt.

Zusammengefasst: Die empfohlene Mischung lässt sich in einer ersten Stufe ohne Schemaschritt bauen (gemeinsame
Angaben als synchron gehaltene Kopien je Größe, Wochen als Woche der Periode); die saubere Form — gemeinsamer
Eigentümer, benannte Wochen, wählbares Wochenende, Länderfeiertage — braucht einen Schemaschritt, den eine Sitzung
vor dem Bau anmeldet.
