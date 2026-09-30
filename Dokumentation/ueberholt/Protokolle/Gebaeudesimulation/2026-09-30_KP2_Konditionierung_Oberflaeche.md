# Protokoll KP2 — Konditionierungsprofile, Oberfläche und erste Kernwellen

**Datum** 29.09.2026 ff. · **Sitzung** Gebäudesimulation in der Cloud, Arbeitszweig `claude/inspiring-bell-b8wq90` ·
**Stufe** KP2 der [Konditionierungsprofile](../../../aktuell/Konzept_Konditionierungsprofile_EPOS-Plan.md) ·
**Entwurf** [`2026-09-29_Entwurf_KP2.md`](../../../aktuell/Gebaeudesimulation/2026-09-29_Entwurf_KP2.md) ·
**Entscheid** E56 ([N1.65](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) · **Festlegungen der
Umsetzung** Entwurf Abschnitt 5, nach dem Abschluss als N1.66 · **Statuszeilen** #618 (K1, U0a, U0b), #619 (K3), #621 (K2, K4), #623 (U1), #626 (U2), #634 (U4, U3)

Vorausgegangen: [Protokoll KP1b](2026-09-29_KP1b_Konditionierung_zweite_Haelfte.md) (Schemaschritt 152, Kopierwege,
Vorlagen, Nachtauskühlung). Das Protokoll wächst mit jeder Welle; jede Welle bekommt einen Abschnitt unter 2 und eine
Zeile unter 5.

## 1. Verfahren

Entwurf vor Bau wie bei KP1b: zwei Leser (Sonnet), zwei unabhängige Entwürfe — vom Arbeitsablauf des Anwenders her und
von den Lücken in Kern und Daten her — und eine Gegenprüfung (Opus); die Synthese steht im Entwurfspapier, der Entscheid
E56 beantwortet dessen fünf Fragen (alle nach Empfehlung). Umsetzung in zehn Wellen, zwei Spuren: **K** = Kern und
Daten, **U** = Oberfläche. Jede Welle arbeitet ein Agent im eigenen Worktree ab (K und U0b Opus 5, U0a Sonnet), mit
Abnahme im Worktree und einem Gate der Orchestrierung nach dem Merge.

## 2. Was gebaut ist

### K1 — Befunde „Speichern unter" und erste Kernwelle

- **„Speichern unter" (B1, B2, E27):** Der Gebäudedialog übergab den neuen Namen als Quelle der Konditionierung — die
  Hülle fand keine — und schloss danach. Jetzt ist die Quelle der Ursprungssatz, der Dialog bleibt offen und meldet
  „Katalogsatz „…" angelegt." in der Erfolgsstufe. Im Katalogmodus arbeitet er danach am neuen Satz (das nächste OK trifft
  nicht das Original), im Projektmodus bleibt der Projektzustand unberührt. Hinweistext und Wiki-Quelle „Gebäude" nennen
  das Verhalten.
- **gbXML `SollHeizenC` (Festlegung 9):** Mit angelegtem Heizkalender (Zone vor Gebäude) der häufigste endliche Wert der
  Standardwoche Mo–Fr außerhalb der Nachtzeit (E55), bei Gleichstand der höhere; „Kalender" in der Verlustliste; ohne
  Kalender wörtlich der Bestand.
- **Ferien des Zapfprofils (Festlegung 10):** Mit Heizkalender des gebundenen Gebäudes dessen FERIEN-Perioden, höchstens
  vier nach Rang, mehr mit Vorhinweis; sonst der Bestandszweig (Projekt 1045 unverändert).
- **„Zeitstruktur übernehmen" (Festlegung 11):** reine Methode `Kalenderwerkzeuge.ZeitstrukturUebernehmen`, „wie
  Heizung" oder „wie Anwesenheit", ersetzt nur die Standardwoche; Fehlfälle benannt abgelehnt.
- **B13 (Festlegung 12):** `Ferienzeit` rechnet im Gemeinjahr über `Feiertage.Gemeinjahrestag/Datum`; der 29.02. ist eine
  Fehleingabe; der falsche Kommentar in `Feiertage` ist berichtigt.
- Proben: bunit über den Dialogweg samt Hüllenfall, `GebaeudeExportKalenderTests`, `ZapfprofilFerienKalenderTests`,
  `ZeitstrukturTests`, `FerienzeitTests` — jede am Ausgangsstand rot.

### U0a — Glossar und Texte

- Glossar § 13, Block „Konditionierungsprofile" mit 63 Zeilen (Deutsch, Englisch, Anmerkung).
- Textbündel `KonditionierungTexte` mit 139 Beschriftungen (`KOND_LBL_*` 74, `KOND_BTN_*` 24, `KOND_TXT_*` 41) in beiden
  Sprachen, `KonditionierungTexteHuelle` (nicht verdrahtet), Probe `KonditionierungTexteTests` (14 Fälle, Glossartreue,
  mit fünf Störungen gegengeprobt).

### U0b — Grundlagen der Oberfläche

- **Vertrag:** DTO `KonditionierungDaten` ohne Kerntypen und Delegatenbündel `KonditionierungWeg` (je Handlung ein
  Delegat, „kein Delegat, kein Knopf"); tief kopiert, `Fassung` im Abdruck des Arbeitsstands. Nicht verdrahtet — das
  trägt K2.
- **Wochenraster:** abschaltbare Zusätze „aus" (NaN als eigener Zustand) und Umbruch nach Behälterbreite (4 × 6,
  2 × 12 ab 600 px, 7 × 24 ab 1 150 px); ohne Zusätze ist das Markup Zeichen für Zeichen das des Bestands.
- **Standardfeld `Gemeinjahrdatum`** (TT.MM. ↔ Jahrestag 1…365, 29.02. als Fehleingabe).
- **Breite Überlagerung** für den Katalogeditor in allen Modi (Festlegung 7).
- **`Modus.Admin` entfernt** (B11); die Regel „aus der Verwaltung kein Zapfprofil-Behälter" steht jetzt in
  `GebaeudeAdminHuelle`.
- **Schloss im Editor (B11):** Ein ausgelieferter Satz steht im Modus Bearbeiten mit Grundzeile, OK ist weich gesperrt
  (auch für den Assistenten), „Speichern unter" bleibt frei.
- **Wirtseite `/konditionierungsprobe`** im Rasterprobe-Wirt samt Browserprobe (390, 820, 1 180, 1 300 px). Sie fand
  drei Befunde, die bunit nicht sieht, alle behoben: Querrollen des Reiters 1 bei 390 px (Klappliste als Flexkind),
  Überlagerung beim Öffnen gerollt (Fokus ohne `preventScroll`), Zellmaß an der Umbruchschwelle (Tagesspalte 46 px).

### K3 — Saat der 14 ausgelieferten Vorlagen

- **Saattabelle** `KonditionierungsvorlagenSaat` als eine Quelle für Schritt, Testdatenbank und Wache: 14 Vorlagen in
  fünf Listen, 46 Vorgabezeilen nach Abschnitt 4 des Entwurfs; Büro und Schule tragen je einen Kalender ohne Woche mit
  den neun bundeseinheitlichen Feiertagen „wie Sonntag" (neun FEIERTAG-Perioden, Wochentag Sonntag; E56 F1 b).
- **Schritt** `KonditionierungsvorlagenSaatSchema`: ein Vorgang je Vorlage, Schlüssel (Größe, Name) ohne
  Groß-/Kleinschreibung; angelegt wird nur, was fehlt; eine eigene gleichnamige Vorlage bleibt und steht im Bericht.
- **Wache** `KonditionierungsvorlagenWacheTests`: Saat gleich Tabelle, gesperrt, eindeutig, ohne Produktnamen, Grenzen,
  E54-Filter, Nachtfenster, Wiederholbarkeit, Stand davor, Verdrahtung; jede Vorlage auf ein Probegebäude übernommen
  ergibt einen gültigen Kalender über 8 760 Stunden (am 1. Mai gilt bei Büro und Schule der Sonntagswert). Vor dem
  Schritt fünf von zwölf Fällen rot.
- **Nachgezogen:** zehn Tests, die die Saat rot machte (nicht drei, wie der Entwurf zählte) — ihr Sinn bleibt; der
  Prüfbericht der Auslieferungsvorlage verlangt 14 gesperrte Vorlagen der Saat.
- **Testdatenbank** auf 157 (71 634 944 Byte, LFS `005f3660…`); neu allein die Vorlagenzeilen, Basis unverändert.

### K2 — Arbeitsstand, eine Schreibstelle, Hülle

- **Reiner Arbeitsstand** `Konditionierungsstand` und `Konditionierungsarbeit` ohne Datenbank: Zelle setzen (Zellenort
  samt Heiz-Nachtzeit und Merker), Anlegen, Verwerfen, Matrix erneut anwenden (P12), Vorlage übernehmen,
  Als-Vorlage-Inhalt (E54), die drei Werkzeuge; jeder Schritt liefert einen neuen Stand und einen benannten Befund —
  „Zurücknehmen" ist der vorige Stand. Die Controller aus KP1 rufen nur noch den Kern.
- **Befunde B4–B8:** Anlegen am Gebäude legt die Kalender der Zonen mit eigenen Zellen mit an; Personen-Nennwert und
  Geräte-Nennwert nach P1 beim Übergang der Personenspalte; die Heiz-Nachtzeit steht an einer Stelle, eine eigene
  Nachtzeile der Zone macht die Spalte wirksam; Merker folgen jedem Schreibweg; `Bemerkung` = Herkunft · letzter
  Werkzeugvermerk. **E56:** ein unveränderter angelegter Kalender folgt der Matrix (F2 a); die Gesamtangabe
  `Luftwechselrate` wird aufgeteilt, Infiltration = min(0,3 1/h; Rate), Nutzerlüftung = Rest (F5 a).
- **Rückfragen vor dem Schreiben** aus dem Arbeitsstand, Zonen mit Namen; „aus dem Katalog erneut übernehmen" ersetzt die
  ganze Gebäudeebene samt Bestandszellen, Nachtzeiten und Ferienzeiträumen (B9).
- **Eine Schreibstelle im OK-Weg:** Katalog neu, bearbeiten und „Speichern unter" in einem Vorgang; im Projekt Schritt 1
  Gebäude, Schritt 3 Zonen mit Id-Zuordnung — neue Zonen behalten ihre Ids, ein zweites OK schreibt nichts doppelt (B10);
  verschachtelte Vorgänge als Sicherungspunkt der Vorgangsklammer.
- **Feiertagsregeln einer Vorlage** stehen beim Übernehmen im Feiertagsband (100–108) unter den Ferien; ein belegter Rang
  wird benannt.
- **Hülle:** `KonditionierungHuelle` füllt den Vertrag und trägt die Delegaten (Prozent ↔ Anteil, Fassung je Ebene);
  Katalog- und Verwaltungshülle reichen den Weg an den Dialog. Vertragsänderungen: `GebaeudeZonenweg.Speichern` liefert
  ein `ZonenSchreibergebnis` mit der Id-Zuordnung, neue Handlungen `LuftwechselAufteilen` und `SpeichernUnterRueckfrage`.

### K4 — Bilder und Auskünfte

- **`Kalenderteppich`:** Rohreihe über 8 760 Stunden mit NaN für „aus" bei jeder Größe und die Quelle je Tag (Grundangabe,
  Standardwoche, Zeitraum, Ferien, Feiertag, Betriebspause, Saison); er ruft dieselbe Entscheidung wie der Lauf,
  `Auswerten` bleibt unberührt, der Rundlauf ohne „aus" ist bitgleich. Bezugsjahr im Projekt das des Laufs, im Katalog 2025.
- **Renderer:** `KalenderteppichModell` fasst Läufe und Folgetage zu Rechtecken, höchstens 2 000 Elemente (sonst benannt
  gröber: weniger Farbstufen, zuletzt Blöcke zu 14 Tagen); „aus" als eigene Rolle mit Schraffur; `data-wert` nennt
  Zeitraum, Stunden, Wert und Quelle. Das Stundenprofil der Woche bricht bei NaN die Fläche, ohne NaN derselbe Rumpf.
- **ChartProben:** drei neue Bilder (Teppich Heizen Büro 169 Elemente, Teppich Lüftung mit „aus" 521, Grenzbild
  vergröbert 121) samt Gegenproben; neue `Messlatte_2026-09-30.sha256` mit 194 Zeilen, die 185 alten unverändert.
- **Spalte „Kalender"** der Gebäude-Katalogauswahl und der Verwaltung: Zahl der angelegten Kalender je Katalogbau.
- **`NachtauskuehlstundenH`** in Bedarfsauskunft, Hülle und DTO (Gebäude und Zone), noch ohne sichtbare Zeile.

### U1 — Reiter „Konditionierung" und Matrix

- **Reiter** in allen Modi des Gebäudeeditors: Vorgabe-Matrix der fünf Größen, am Behälter umbrechend (breit Tabelle
  und fünf Karten, schmal fünf Reiter je Größe); Karten eingeklappt mit Zustandszeile und den Handlungen Kalender
  anlegen, Verwerfen, „Matrix erneut anwenden…", Zurücknehmen; Lesemodus mit Grund; ohne Konditionierungstabellen
  benannt gesperrt.
- **Rückfragen** aus dem Befund vor dem Schreiben; „aufteilen" der Gesamtangabe (F5); „aus dem Katalog erneut
  übernehmen…" im Reiterkopf; „Speichern unter" im Projekt stellt eine Frage für Zonen, Bauteile und Konditionierung.
  Vor OK wird nichts geschrieben (Integrationsproben für Anlegen, Verwerfen, Zurücknehmen, „aufteilen").
- **Altfelder (F3 a)** nur noch im Reiter; Reiter 1 behält Luftwechselrate, „Kühlung aktiv", Kühlleistung,
  Kühlübergabe und eine Herleitungszeile.
- **Projektbezug** des Wegs: Referenzjahr, Kühlbetrieb und Anlagenkopplung aus dem Projekt
  (`Waermeuebergabe.KopplungWirksamFuer`); Vorschautitel je Größe.
- **Hausweit:** `Zahlen.ZahlParsen` nur endlich; Hilfepille und KI-Ring 44 px; breiter Editor ohne Deckel.
- **Assistent:** Feldkarte der Matrix aus `KiKonditionierungsfelder` (36 Felder, `IKiFeldtafel`); Aktionswissen
  „Matrix", „Kalender anlegen", „Nachtauskühlung".
- **Bedarfsdialog:** Zeile „Stunden mit Nachtauskühlung" und Zonenspalte nur mit Wert.
- **Konditionierungsprobe:** reiner Weg ohne Datenbank (`KonditionierungHuelle.ReinerWeg`), neue Fälle; 24 Läufe ohne
  Verstoß. Zwei Fotobefunde behoben: Knöpfe brachen im Wort („Zurückneh|men" bei 390 px, „Verwerfe|n" bei 1 180 px) —
  die Knopfzeilen brechen jetzt zwischen den Knöpfen.

### U2 — Vorlagen je Karte und Vorlagenverwaltung

- **Auswahlliste** der Vorlagen je Karte (ausgelieferte zuerst, mit Schloss; dieselbe Reihenfolge in allen Listen) und
  entprellte **Vorschau der Woche** vor dem Übernehmen. Im Kern `IKonditionierungsvorlagen` mit der reinen
  `Konditionierungsvorlagenablage` (Saat ohne Datenbank) und einer gemeinsamen Namensregel.
- **Übernehmen** nach P12: Rückfrage am angelegten Kalender, „aufteilen" an der Gesamtangabe; danach folgt der Kalender
  der Matrix (F2 a). Die Zeile „Vorlage" zeigt die Herkunft.
- **„Als Vorlage speichern…"** inline, sofort mit eigenem OK (Festlegung 13), ohne Nennwert und Saison (E54).
- **Vorlagenverwaltung** als Blatt im Katalogeditor aus jeder Karte (F4 a) — von U4 hierher genommen, damit nur eine
  Welle die Karte anfasst.
- **Zählfall KN6** („Büro" in allen fünf Größen): breit 11, mit Gesamtangabe der Lüftung 12, mit fünf angelegten
  Kalendern 16, schmal 15 Handgriffe.
- **Assistent:** `kond_<größe>_vorlage` wählt und übernimmt wie der Knopf; eine Rückfrage bleibt im Reiter stehen.
- **Probe:** Zoomleiste der Vorschau unter 44 px, „Übernehmen" überdeckte die Liste, Kopf des Verwaltungsblatts ragte
  bei 390 px heraus — behoben; 28 Läufe ohne Verstoß.

### U4 — Zonenmatrix und Konditionierung in der Verwaltung

- **Zonendialog als breites Blatt** mit der Zonenmatrix (derselbe Baustein wie am Gebäude): leer = „wie Gebäude"; folgt
  die Zone einem angelegten Gebäudekalender, steht „vom Gebäude" mit Grund, die Spalte ist ohne Wirkung; „vom Gebäude
  übernehmen und anpassen" legt eine Kopie an (`VomGebaeudeUebernehmen`); Kühlspalte bis KU3 gesperrt, „aufteilen" an
  der Zone benannt abgelehnt; neue Zonen behalten nach OK ihre Zuordnung, ein zweites OK schreibt nichts.
- **Personen-Nennwert der Zone:** eigener Wert oder Flächenanteil (Teilkonzept 3.4) statt des vollen Gebäudewerts —
  vorher fünf von sieben Proben rot.
- **Zone rechnet auf ihrem Bestand:** `Konditionierungseingang.ZonenBestand` für Vorschau und Lauf — eigene Sollwerte,
  Lüftung und Bewohner vor denen des Gebäudes, innere Gewinne nach Flächenanteil, Gesamtangabe der Lüftung neu bestimmt.
  Vorher rechnete der Lauf eine Zone mit eigenem Heizsoll 23 °C mit den 20 °C des Gebäudes. `Tab_Zone` ist in der
  Testdatenbank leer, der Referenzlauf bleibt byte-gleich.
- **Gebäudeverwaltung:** Gruppe „Konditionierung" mit fünf Zustandszeilen, „Konditionierung…" öffnet ein breites Blatt
  am selben Arbeitsstand; „Speichern" schreibt mit (die Fassung zählt als Abweichung), „Verwerfen" nimmt alles zurück;
  eine Regel an Matrix, Nachtzeit oder Ferien öffnet das Blatt; die Altfelder sind aus Stammblatt und „Alle Daten"
  in das Blatt gewandert.
- **Assistent:** Feldtafeln der Verwaltung (41 Felder samt Vorlagen) und der Zone (27, ohne Kühlspalte), die Logik
  einmal in `KonditionierungKiTafel`; Aktionswissen.

### U3 — Kalenderkarte im Einzelnen

- **Grundangabe und Standardwoche** schließen sich aus; „Standardwoche verwerfen" übernimmt den häufigsten Wert; die
  Werkzeuge greifen nur am angelegten Kalender, sonst weich gesperrt mit Grund.
- **Zeitfenster** mit sieben Tagesknöpfen, von, bis, Wert oder „aus"; **Feiertage** als Regel; **„Zeitstruktur
  übernehmen"** wie Heizen oder wie Anwesenheit — je mit Vermerk der Herkunft.
- **Periodenliste** (Festlegung 15): Matrixbereich nur lesbar, eigene Perioden (Zeitraum, Feiertag) im Eigenband
  310–899, Rang nur dort verschiebbar, Feiertagsband 100–108 weich gesperrt. Unter 600 px entfallen die Spalten „Art"
  und „Von–Bis" (vorher 20 Wortbrüche bei 390 px).
- **Teppichbild** über den Delegat der Hülle, entprellt, Wert und Quelle am Zeiger.
- **„In den Kalender übernehmen"** an der Wärmeübergabe: das Sollwertprofil wird die Standardwoche des Heizkalenders,
  mit Rückfrage an einem angelegten Kalender und einem Schritt Zurücknehmen.
- **Zeile „Vorlage"** öffnet die Auswahlliste; Geräte und Personen ohne Anteile zeigen einen benannten Leerzustand.
  Ein `@ref` am Baustein `Auswahlfeld` hatte das Markup aller Auswahllisten geändert (rot in `WochenrasterTests`) —
  zurückgenommen, der Fokus geht auf die benannte Gruppe.
- **Assistent:** `kond_<größe>_woche` (168 Werte oder die Grundangabe), Aktionswissen „Zeitfenster", „Periodenliste",
  „Feiertage", „Teppichbild".

## 3. Schemaschritte

**157** (K3, `KonditionierungsvorlagenSaatSchema`): reines DML, die 14 Vorlagen samt Feiertagskalendern; Nummer als `KesselKennlinieSchema.SCHRITT + 1` — 156 hat die Kessel-Kennlinie (#616) belegt.

## 4. Befunde der Umsetzung

- **Merge der Ressourcen:** Parallele Wellen hängen ihre Schlüssel als Block an das Ende beider `.resx`; beim Merge
  fehlte an der Naht jeweils das gemeinsame `</data>`. Nach jedem Merge: Blöcke beider Seiten übernehmen, `</data>`
  ergänzen, XML-Prüfung, Doppelprüfung der Namen, `designer_neu.py schreiben`.
- **Präfixregel mit Ausnahme:** Die Wache `ZapfprofilHuelleZ4Tests` verlangt je Hinweiskennung den Titel
  `ZPG_WARN_<Kennung>`; K1 trägt deshalb einen `ZPG_WARN_*`-Schlüssel im eigenen Block.
- **K1 × U0b — das Schloss blieb nach „Speichern unter" stehen:** K1 führt „Speichern unter" über einen eigenen Weg
  und lässt den Dialog offen; U0b hob das Schloss nur im gemeinsamen Schreibweg auf. Beide Wellen waren je für sich
  grün, erst das Gate nach dem Merge zeigte den roten Test: An der eigenen Kopie eines ausgelieferten Satzes wäre OK weich
  gesperrt geblieben, auch für den Assistenten. Behoben mit einer Zeile im Katalogzweig von `SpeichernUnterSchreiben`;
  die U0b-Probe erwartet jetzt den Ursprungssatz als Quelle und hält fest, dass das nächste OK die Kopie trifft.
- **Rangband der Feiertagsregeln (K3):** `KonditionierungsvorlageCtrl.Uebernehmen` legt die Feiertagsregeln einer
  Vorlage ins Eigenband (Rang 310 und mehr), über die Ferien; das Teilkonzept stellt sie darunter. Bei der Saat ohne
  Wirkung (Ferienwert gleich Sonntagswert); an K2 übergeben, das die Übernahme als reinen Schritt neu fasst.
- **Festlegungen der Welle U1:** (1) Die Kette steht nach Teilkonzept 3.4 wörtlich: Unter einem angelegten
  Gebäudekalender wirken Zonenzelle und Heiz-Nachtzeile erst, wenn die Zone einen eigenen Kalender führt („vom Gebäude
  übernehmen und anpassen"). (2) Auf dem Tagesbilanz-Weg zeigt die Matrix nur die Altweg-Felder (Teilkonzept 2.2), der
  Kühlsollwert ist dort nicht bearbeitbar. (3) Die Kühlspalte sperrt allein „Kühlung aktiv"; der Projektbezug schließt
  den Kühlbetrieb ein wie die Rechnung. (4) Das Aktionswissen ist deutsch, die englischen Suchwörter stehen in den
  Titeln. (5) Das Stammblatt der Verwaltung behält die Altfelder bis U4. (6) Die Setzer des Assistenten für die
  Bestandsfelder laufen im Editor über den Weg.
- **Offene Punkte aus K2 für U1** (erledigt mit U1, siehe Festlegung 1 und Projektbezug): Eine Zelle an einer Zone, die den angelegten Kalender des Gebäudes erbt, wirkt erst
  mit eigenem Zonenkalender; eine eigene Heiz-Nachtzeile der Zone wird übergangen, wenn das Gebäude einen angelegten
  Heizkalender hat; die Hülle nimmt die Anlagenkopplung als unwirksam und das Referenzjahr fest mit 2025.
- **Verwaltung ohne Spalte:** Das gemeinsame Profil der Gebäudeauswahl trägt seit K4 die Spalte „Kalender"; die
  Verwaltung las ihre Zeilen noch ohne sie (K4 durfte ihre Hülle neben K2 nicht anfassen) — nachgezogen beim Merge.
- **Nebenbefunde für U1:** Hilfepille 28 × 26 px (unter 44 px); Deckel der `.epos-dialog` bei 1 160 px, Vorbild
  `.epos-wp-anlage`; `Zahlen.ZahlParsen` nimmt „NaN" und „Infinity" an (neun Aufrufer).

## 5. Nachweise

| Nachweis | Ergebnis |
|---|---|
| Abnahme K1 im Worktree | EPOS.Kern.Tests 9 127, EPOS.UI.Tests 6 927, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün, zusammen 2 übersprungen; Wachen 35/35; Referenzlauf GESAMT PASS, 460/460 CSV byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0 Fehler |
| Abnahme U0b im Worktree | EPOS.Kern.Tests 9 108, EPOS.UI.Tests 6 989, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün, zusammen 2 übersprungen; Wachen 35/35; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; Konditionierungsprobe 20 Läufe ohne Verstoß |
| Kern-Läufe auf dem Arbeitszweig | U0a-Merge grün (36635134183), K1-Merge grün (36640302824) |
| Gate KP2a auf dem Merge mit `origin` (#611–#615) und U0b | Kern 9 210, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; **UI 7 015 von 7 016 — rot der Befund K1 × U0b** (Abschnitt 4); Wachen 35/35; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; ChartProben gleich der Messlatte. Nach der Behebung: UI 7 016/7 016, Windows-Schale 0 Fehler, SqlDialektPruefer 0 Fundstellen, Auslieferungsvorlage 41/41 |
| Gate KP2b auf dem Merge mit `origin` (#616, #617: Kessel-Kennlinie mit Schemaschritt 156, neue Testdatenbank) | Kern 9 269, UI 7 019, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün, zusammen 2 übersprungen; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf GESAMT PASS, 460/460 CSV byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0 Fehler; SqlDialektPruefer 0 Fundstellen (2 135 Texte); Auslieferungsvorlage 43/43 |
| Kern-Lauf des Arbeitszweigs auf dem U0b-Merge | rot (36647131587), derselbe Test wie im Gate KP2a |
| Kern-Lauf auf `94f91fed` (#618) | grün (36654571061) |
| Abnahme K3 im Worktree (`b65040a9`) | Kern 9 281, UI 7 019, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; Auslieferungsvorlage 44/44; Windows-Schale 0; SqlDialektPruefer 0 |
| Abnahme K2 im Worktree (`9bd0d01b`) | Kern 9 339, UI 7 019, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0; SqlDialektPruefer 0 |
| Abnahme K4 im Worktree (`60c213d1`) | Kern 9 307, UI 7 019, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben 231 Bilder gleich der neuen Messlatte; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0; SqlDialektPruefer 0 |
| Gate KP2c auf K2 und K4 zusammen (`77a3e06f`) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte `Messlatte_2026-09-30` (194), Tests Kern 9 365, UI 7 019, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen) — alle grün; Wachen 35/35; Referenzlauf 15/15 **PASS, 460/460 CSV byte-gleich gegen R26**, gestörter Lauf PASS; `SqlDialektPruefer` 0 Fundstellen (2 142 Texte); Windows-Schale 0 Fehler; Auslieferungsvorlage 44/44. |
| Abnahme U4 im Worktree nach den Merges von U2 und `origin` #624/#625 (`7a251720`) | Kern 9 465, UI 7 135, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf 16/16 PASS, 487/487 byte-gleich gegen R27, gestörter Lauf PASS; Windows-Schale 0; SqlDialektPruefer 0; Konditionierungsprobe 32 Läufe ohne Verstoß; Rasterprobe GD1–GD3, Katalogprobe N16 ohne Befund |
| Gate KP2e auf U4 mit `origin` #627/#628 (`c00d37fe`) | Kern 9 496, UI 7 138, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf 16/16 PASS, 487/487 byte-gleich gegen R28, gestörter Lauf PASS; Windows-Schale 0; SqlDialektPruefer 0; Auslieferungsvorlage 44/44 |
| Gate KP2f nach dem Merge `origin` #629/#630 (`dde818b6`) | KP2f auf `dde818b6` (U4 mit `origin` bis #630, **gegen R29**): Kern-Filter 0 Fehler, ChartProben gleich `Messlatte_2026-09-30` (194), Tests Kern 9 538, UI 7 141, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen) — alle grün; Wachen 35/35; Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R29**, gestörter Lauf PASS; `SqlDialektPruefer` 0 Fundstellen (2 145 Texte); Windows-Schale 0 Fehler. Davor KP2e auf `c00d37fe` gegen R28 grün (Kern 9 496, UI 7 138). Nach den Merges `origin` #630–#633 (`281200f7`) und U3 (`22c04ad1`) gezielt: UI 7 143 bzw. 7 175, Kern 2 903 bzw. 2 316 grün, Referenzlauf je 487/487 byte-gleich gegen R29, Windows-Schale 0. U3 im Worktree gegen R29: Kern 9 556, UI 7 173, Referenzlauf 487/487, gestörter Lauf PASS. |
| Abnahme U3 im Worktree gegen R29 (`75180194`) | Kern 9 556, UI 7 173, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf 16/16 PASS, 487/487 byte-gleich, gestörter Lauf PASS; Windows-Schale 0; Konditionierungsprobe 36 Läufe ohne Verstoß |
| Nach den Merges `origin` #630–#633 (`281200f7`) und U3 | Kern-Filter 0 Fehler; UI 7 175; Kern 2 316 gezielt (Konditionierung, Kalender, Gebäude, Zone, KI, Wachen, Wärmeübergabe) grün; Referenzlauf 16/16 PASS, 487/487 byte-gleich gegen R29; Windows-Schale 0 Fehler |
| Kern-Lauf auf `2d8dbde2` (#626) | grün (36689699460, Arbeitszweig) |
| Abnahme U2 im Worktree (`12b9b6f1`) | Kern 9 379, UI 7 110, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0; Konditionierungsprobe 28 Läufe ohne Verstoß |
| Gate KP2d auf U2 mit `origin` #624/#625 (`57901bba`) | Kern-Filter 0 Fehler, ChartProben gleich `Messlatte_2026-09-30` (194), Tests Kern 9 446, UI 7 115, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen) — alle grün; Wachen 35/35; Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R27**, gestörter Lauf PASS; `SqlDialektPruefer` 0 Fundstellen (2 142 Texte); Windows-Schale 0 Fehler; Auslieferungsvorlage 44/44. Abnahme im Worktree auf `12b9b6f1`: Kern 9 379, UI 7 110, Referenzlauf 460/460 gegen R26, Konditionierungsprobe 28 Läufe ohne Verstoß. |
| Kern-Lauf auf `15b33156` (#623) | grün (36673572516) |
| Abnahme U1 im Worktree (`ad742770`) | Kern 9 372, UI 7 082, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0; Konditionierungsprobe 24 Läufe ohne Verstoß, 76 Fotos außerhalb des Repositorys |
| Nach dem Merge U1 (`4d82a032`) | Kern-Filter 0; UI 7 082; Kern 1 413 gezielt grün; Windows-Schale 0 |
| Kern-Lauf auf `d8adcfe2` (#621) | grün (36664696918) |
| Kern-Lauf auf `2a767569` (#619) | grün (36657553771) |
| Nach dem Merge K3 (`a221dd41`) | Kern-Filter 0 Fehler; 328 gezielte Tests (Konditionierung, Schema, Wachen) grün; Referenzlauf 15/15 PASS, 460/460 byte-gleich gegen R26 |

## 6. Offen

- **Wellen** **SA1** nach U2,
  **U5** (Abschluss, SA2).
- **Windows-Sichtprobe** der schon sichtbaren Änderungen, mit SA1: „Speichern unter" bleibt offen (Katalog und Projekt),
  breite Überlagerung des Katalogeditors, Grundzeile und weich gesperrtes OK an einem ausgelieferten Satz.
- **Wiki:** Quelle „Gebäude" nachgezogen („Speichern unter"); Upload mit dem nächsten Sammel-Upload, Logbuch-Satz in der
  Statuszeile.
