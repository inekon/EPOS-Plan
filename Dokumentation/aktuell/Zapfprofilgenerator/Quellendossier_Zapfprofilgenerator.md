# Quellendossier: Kataloge des Zapfprofilgenerators

**Stand 23.09.2026 — Posten P13 der Stufe Z0** des
[Umsetzungskonzepts](../Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md) (Kapitel 6, Kapitel 7
Zeile Z0, Anhang A). Das Dossier sagt je Wertgruppe des Katalogs, **woher** ein Wert kommen darf,
**wie** seine Herkunft zu vermerken ist und **wo** er nie stehen darf. Es enthält **keinen Wert**:
keine Kennzahl, keine Bandbreite, keinen Formvektor, keine Bezugstemperatur, keinen
Nachschlagewert — nur Regelwerk, Ausgabe, Fundstellenart und Regel.

**Geltung.** Das Schema steht in `EPOS.Kern/Allgemein/Update/TwwSchema.cs` (Schemaschritt T1, im
Bestand Schritt 102); die Tabellen und Spalten beschreibt Abschnitt 3.1 des Umsetzungskonzepts. Wo
dieses Dossier und das Umsetzungskonzept auseinanderlaufen, gilt das Umsetzungskonzept; die
Methodik steht im [Konzept](../Konzept_TWW-Zapfprofile_WP-Plan_1.md), die Auswertung der
Regelwerke in den Grundlagenpapieren 1, 2, 3 und 5. Deren Zahlenteile werden nach dem Ergebnis von
K8 bereinigt (A9, Anhang A P14); dieses Dossier zitiert sie nicht.

---

## 1 Die Wertemengen

**`Status`** einer Katalogzeile (`TwwSchema.STATUS_*`, CHECK in jeder Kopftabelle):

| Wert | Bedeutung | Entsteht durch | In der Auslieferungsvorlage |
|---|---|---|---|
| `AUSLIEFERUNG` | gehört zum ausgelieferten Katalog; unveränderlich (`ReadOnly = 1`) | Katalogpaket (`--katalogpaket`), später Katalogimport der Verwaltung (Z4, ZU14) | bleibt, `ReadOnly = 1` wird gesetzt |
| `EIGEN` | Anwenderkopie oder eigene Zeile; auch jede Zeile des fiktiven Testkatalogs | `TwwNutzungsartCtrl` (Neu, Speichern unter, Tagesgang speichern), Testkatalog-Skript | fällt |
| `IMPORT` | mit einem Projektpaket (`.wpx`) mitgenommene Zeile, die am Ziel fehlte | `ProjektExportImportCtrl` | fällt; der Prüfbericht nennt das Beispielpaket |

**`Herkunftsart`** einer Wertgruppe (`TwwSchema.HERKUNFT_*`, CHECK in jeder Provenienzgruppe):

| Wert | Bedeutung (Lesart dieses Dossiers) | In der Auslieferungsvorlage |
|---|---|---|
| `VERFAHREN` | aus einem Regelwerk übernommen oder nach dessen Verfahren gerechnet; `Quelle` nennt das Regelwerk, `Ausgabe` die Ausgabe | zulässig, wenn K8 die Kapselung trägt |
| `EIGENKONSTRUKTION` | von INEKON oder dem Anwender gesetzt (Formvektor, INEKON-Setzung, geänderte Wertgruppe); `Quelle` „Eigenkonstruktion", ohne Ausgabe | zulässig |
| `FREI` | aus einer frei verfügbaren Quelle (Rechtsakt, frei lizenzierte Parametrik oder Datenreihe) | zulässig, mit Attribution nach der Lizenz der Quelle |
| `IMPORT` | vom lizenzierten Anwender aus einem Regelwerk eingespielt (Normimport, Z4b) | nie — die Vorlage entfernt jede solche Zeile |
| `FIKTIV` | erfundener, runder Wert des Testkatalogs | nie — Prüfposten der Vorlage |

`Status` und `Herkunftsart` sind unabhängig: Eine Zeile mit `Status = 'IMPORT'` kann Wertgruppen
jeder Herkunftsart tragen; eine Wertgruppe mit `Herkunftsart = 'IMPORT'` kann in einer Zeile
`EIGEN` stehen. Die Auslieferungsvorlage verlangt beides: nur `AUSLIEFERUNG`, nie `FIKTIV` oder
`IMPORT` als Herkunftsart.

**Provenienzgruppe** (Konzept 3.1): `Quelle` TEXT NOT NULL, `Ausgabe` TEXT, `Version` TEXT NOT NULL
(Katalogversion, in der die Gruppe zuletzt gesetzt wurde), `Herkunftsart` TEXT NOT NULL — mit
Präfix (`Bedarf_`, `Jahresgang_`, `Wochengang_`) in `Tab_TwwNutzungsart_STAMM`, ohne Präfix in den
übrigen Kopftabellen. Dazu je Zeile `Beleg` (intern) und in der Nutzungsart `Freigabe` (K7).

---

## 2 Die Provenienzpflicht (Kapitel 6 (e))

1. **Jede Wertgruppe trägt ihre Provenienz**, auch eine Eigenkonstruktion; `Quelle`, `Version`
   und `Herkunftsart` sind Pflicht, `Ausgabe` ist Pflicht, sobald die Quelle ein Regelwerk ist.
2. **`Quelle` nennt nur Norm, Verfahren oder Eigenkonstruktion** samt Ausgabe — nie einen
   Hersteller, nie ein Produkt, nie eine Zahl. Die Wache `WikiProduktdatenWacheTests` wird
   sinngemäß auf die Katalogtexte der `Tab_Tww*_STAMM` (ohne `Beleg`) erweitert.
3. **Sekundärquellen stehen in `Beleg`.** Ist eine Bandbreite oder ein DIN-4708-2-Kennwert nur
   über eine Sekundärquelle zitierfähig (etwa frei publizierte Herstellerunterlagen, Konzept 3.4),
   steht diese allein in `Beleg`. Oberfläche, Bericht und KiSicht zeigen `Beleg` nie; ein
   Projektpaket führt `Beleg` und `Freigabe` nicht mit.
4. **Eine Änderung führt die Provenienz nach.** Ändert der Anwender eine Wertgruppe, bekommt sie
   eine neue Katalogversion und die Herkunftsart `EIGENKONSTRUKTION` mit neutraler Quelle
   „Eigenkonstruktion"; der Beleg bleibt nur ohne Änderung stehen (`TwwNutzungsartCtrl`, N2).
5. **Eine benutzte oder ausgelieferte Zeile ist unveränderlich**; Änderungen entstehen als neue
   Zeile („Speichern unter", Konzept 3.2, ZU12).

---

## 3 Die Wertgruppen

Die Spalte „Bilanzgrenze" nennt den Code von `Bilanzgrenze` (1 Zapfstelle; 2 mit Verteil- und
Zirkulationsverlust; 3 mit Speicherverlust). „Temperaturbezug" sagt, woher `Bezug_Zapftemperatur`
und `Bezug_Kaltwasser` zu nehmen sind; der Wert selbst steht nur im Katalog.

### 3.1 Bedarf mit Bandbreite

`Tab_TwwNutzungsart_STAMM`, Gruppe `Bedarf_*`: die drei Niveaus, die sechs Bandbreitenspalten, die
Bezugstemperaturen und `Bilanzgrenze` (N2 (f)).

| Quelle | Ausgabe (Beleg im Repo) | Fundstelle | Bilanzgrenze | Temperaturbezug | Herkunftsart |
|---|---|---|---|---|---|
| DIN V 18599-10 | 2018-09 (Grundlagen 1) | Tabelle (Richtwerte Nutzenergie, Wohnen und Nichtwohnen) | 1 | nicht ausdrücklich genannt, abgeleitet (Grundlagen 1, 1.3) — die Ableitung als Beleg vermerken | `VERFAHREN` |
| E DIN EN 12831-3/A100 | 2021-09, Entwurf (Grundlagen 3) | Tabelle des nationalen Anhangs | 1 | in der Fundstelle ausdrücklich | `VERFAHREN`, nur nach K1/K8 |
| VDI 6002 Blatt 1 und 2 | 2014-03 (Grundlagen 1) | Tabelle (Kennwert-Bandbreiten) | 1 | Bezugstemperatur der Richtlinie, Umrechnung nach Konzept 4.1 | `VERFAHREN`, nur Bandbreite und nur nach K8/ZU15 |
| VDI 4655 | 2021-07 (Grundlagen 5) | Text und Tabelle (Jahresenergie je Person bzw. Wohneinheit) | 2 | ohne Temperaturbezug (Energie) | `IMPORT` (Z4b), nie ausgeliefert |
| INEKON-Setzung | — | — | wie gesetzt | wie gesetzt | `EIGENKONSTRUKTION` |
| Messdaten | — | — | wie gemessen | nach Konzept 4.1 | nur nach K5 (Z5) |

### 3.2 Jahresgang

Gruppe `Jahresgang_*`: zwölf Monatsfaktoren (Mittel 1) samt `Kalenderart` und `Ferienfaktor`.

| Quelle | Ausgabe | Fundstelle | Herkunftsart |
|---|---|---|---|
| Eigenkonstruktion (Vorgabe) | — | — | `EIGENKONSTRUKTION` |
| VDI 4655, Typtagsystematik | 2021-07 | Tabelle | `IMPORT` über T3 in `Tab_TwwTyptag_IMPORT` (Z4b), nie als Monatsfaktoren im Katalog |
| VDI 6002 (Saisonfaktoren) | 2014-03 | Tabelle bzw. Bild | nicht zulässig: eigene Formvektoren statt Tabellen der Richtlinie (Kapitel 6) |
| frei verfügbare Schedules (DOE/ASHRAE) | nach Quelle | Datenreihe | `FREI` |

### 3.3 Wochengang

Gruppe `Wochengang_*`: sieben Wochenfaktoren (Summe 1).

| Quelle | Ausgabe | Fundstelle | Herkunftsart |
|---|---|---|---|
| Eigenkonstruktion (Vorgabe) | — | — | `EIGENKONSTRUKTION` |
| DIN V 18599-10, Nutzungsprofile (Nutzungstage) | 2018-09 | Tabelle | `VERFAHREN` — der Wochengang wird daraus gebildet, nicht abgeschrieben |
| frei verfügbare Schedules (DOE/ASHRAE) | nach Quelle | Datenreihe | `FREI` |
| VDI 6002 (Wochenanteile) | 2014-03 | Tabelle bzw. Bild | nicht zulässig (Kapitel 6) |

### 3.4 Tagesgang

`Tab_TwwTagesgang_STAMM`: je Tagesgangsatz und Tagtyp 24 Anteile (Summe 1), Provenienz je Tagtyp.
Der Kopf `Tab_TwwTagesgangsatz_STAMM` trägt nur `Status`, `Beleg` und `ReadOnly`.

| Quelle | Ausgabe | Fundstelle | Herkunftsart |
|---|---|---|---|
| Eigenkonstruktion (Vorgabe) | — | — | `EIGENKONSTRUKTION` |
| Jordan/Vajen-Parametrik | nach Quelle | Text (Parameter) | `FREI` |
| frei verfügbare Schedules (DOE/ASHRAE) | nach Quelle | Datenreihe | `FREI` |
| Ecodesign-Zapfprofile, VO (EU) 814/2013 und 812/2013 | Rechtsakt | Tabelle (Zapffolge) | `FREI` |
| VDI 4655, Typtage | 2021-07 | Tabelle | nur als Typtag-Import (T3, Z4b), nie als Tagesgangsatz |
| VDI 6002 (Tagesprofile) | 2014-03 | Bild | nicht zulässig — Digitalisate aus Bildern übernimmt der neue Katalog nicht (A9, 1.6) |

### 3.5 Bedarfstag

`Tab_TwwBedarfstag_STAMM` (Kopf mit `Quelle_Art` und Provenienz) und
`Tab_TwwBedarfstagEreignis_STAMM` (Ereignisse ohne eigene Provenienz). Bilanzgrenze 1,
Temperaturbezug: Energie je Ereignis.

| `Quelle_Art` | Quelle | Ausgabe | Fundstelle | Herkunftsart |
|---|---|---|---|---|
| 2 A100-Referenz | E DIN EN 12831-3/A100 | 2021-09, Entwurf | Referenzprofil (Datei, K1) | `VERFAHREN`, nur nach K1/K8; Ergebnis mit Entwurfsvermerk |
| 3 DIN-4708-Profil | DIN 4708-2 | 1994-04 (Grundlagen 1) | Text und Tabelle (genormte Zapfperiode) | `VERFAHREN`, nur nach K1/K8 |
| 4 Konstruktor | Eigenkonstruktion | — | — | `EIGENKONSTRUKTION` |
| 5 Ecodesign | VO (EU) 814/2013 | Rechtsakt | Tabelle | `FREI` |

### 3.6 Parameter

`Tab_TwwParameter_STAMM`: gekapselte Normkonstanten, Regelwerksgrenzen und INEKON-Setzungen,
gelesen nur über den `Parametersatz` der aktuellen Katalogversion (Kapitel 6 (a)). Provenienz je
Zeile.

| Schlüsselkreis | Quelle | Ausgabe | Fundstelle | Herkunftsart |
|---|---|---|---|---|
| `DIN4708.*` | DIN 4708-1/-2 | 1994-04 | Text (Formel) und Tabelle | `VERFAHREN`, nach K1/K8 |
| `DIN1988.*` | DIN 1988-300 | im Repo nicht belegt, bei der Erfassung eintragen | Tabelle | `VERFAHREN` |
| `A100.*` | E DIN EN 12831-3/A100 | 2021-09, Entwurf | Text (Formel) und Tabelle | `VERFAHREN`, nach K1/K8 |
| `W551.*` | DVGW W 551 | im Repo nicht belegt, bei der Erfassung eintragen | Text | `VERFAHREN` |
| `Zirkulation.*` | DIN V 4701-10 bzw. ihr Nachfolger in der Reihe DIN V 18599 | 2003-08, zurückgezogen (Grundlagen 1); beim Nachfolger die Ausgabe der Erfassung | Tabelle | `VERFAHREN` |
| INEKON-Setzungen | Eigenkonstruktion | — | — | `EIGENKONSTRUKTION`, Vermerk „INEKON-Setzung" |

### 3.7 DIN-4708-Werte

`Tab_TwwDin4708Wert_STAMM`: `Art` `BELEGUNG` (Belegung je Raumzahl) und `AUSSTATTUNG`
(Ausstattungsklasse mit neutralem Namen). Quelle DIN 4708-2, Ausgabe 1994-04, Fundstelle Tabelle;
Herkunftsart `VERFAHREN`. Eine Sekundärquelle steht nur in `Beleg`. Diese Nachschlagewerte stehen
nie im Repository und — solange K8 nichts anderes ergibt — nicht in der Auslieferung; bis dahin
rechnet die Auslegung ohne sie (Konstruktor, Konzept 4.5).

---

## 4 Was nie im Repository und nie in der Auslieferung steht

- **VDI-4655-Daten** (Typtage, Faktoren, Klimazonenzuordnung): nur beim lizenzierten Anwender,
  eingespielt über die Import-Schnittstelle (Z4b) in `Tab_TwwTyptag_IMPORT`; nie im Produkt, in der
  Auslieferungsvorlage (sie leert die Tabelle), im Repository oder in der CI (K3a, A12).
- **Tabellen der VDI 6002** (Tagesprofile, Wochenanteile, Saisonfaktoren, Kennwerttabellen als
  Ganzes): weder im Repository noch in Testdatenbank, CI oder Auslieferung; die aus den Exemplaren
  der Ablage extrahierten Tabellen bleiben lokal und werden nicht weitergegeben (ZU15).
- **Nachschlagewerte der DIN 4708-2** und die Referenzprofile der A100: nie im Repository; in der
  Auslieferung erst nach K1/K8.
- **Jede Normzahl** in Quelltext, Code-Kommentar, Testdaten, Oberfläche, Wiki, Handbuch oder
  Mockup (Kapitel 6 (a)); Tests arbeiten mit erfundenen Parametern.
- **Hersteller- und Produktdaten** in Katalogtexten; **Messobjektdaten** vor der Freigabe nach K5.

---

## 5 Lokale Testdaten

Lokale Normkopien liegen unter `Referenzlaeufe/Normzahlen/` in den Unterordnern `vdi4655/`,
`vdi6002/` und `zapfprofil/`; `vdi4655/` und `vdi6002/` tragen je eine `QUELLE.txt` (Regelwerk,
Ausgabe, Herkunft der Kopie, Datum). Der Ordner ist gitignoriert (`Referenzlaeufe/Normzahlen/*`,
ausgenommen allein `LIESMICH.md` auf oberster Ebene); die Wache `Normzahlen_stehen_im_gitignore`
in `RepositoryOrdnungWacheTests` hält die Regel, der Prüfposten ZU11 der Auslieferungsvorlage
lehnt jede Eingabe unter diesem Ordner ab. Tests schweigen ohne den Ordner; ein Nachweis gegen
diese Daten ist lokal und nennt Abweichungen, nie Absolutwerte. Ob die lokalen Kopien zulässig
sind, klären K8 und ZU15.

---

## 6 Der fiktive Testkatalog

Die Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite` führt nur den fiktiven Katalog, den
`Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` wiederholbar einspielt: ein Tagesgangsatz mit
vier Tagesgängen, drei Nutzungsarten, drei Parameter mit Präfix `Test.`, ein Bedarfstag mit
Ereignissen, DIN-4708-Werte — erfunden und rund. **Kennzeichnung jeder Zeile:** `Status = 'EIGEN'`,
`ReadOnly = 0`, jede Herkunftsart `FIKTIV`, jede Quelle „Testkatalog (fiktiv)", Katalogversion
`TEST-1`, kein Beleg. Die erfundenen Bezugstemperaturen sind so gewählt, dass sie mit keinem
normativen Wert zusammenfallen. Die Wache `TwwKatalogWacheTests` prüft die Kennzeichnung, das
Fehlen jeder Zeile `AUSLIEFERUNG` und die Wiederholbarkeit des Skripts.

---

## 7 Der Weg für Auslieferungs- und Anwenderdaten

| Weg | Wofür | Status / Herkunftsart | Stufe |
|---|---|---|---|
| **Katalogpaket** `--katalogpaket <ordner>` der Auslieferungsvorlage | Auslieferungswerte einer neuen Installation; je Tww-Tabelle eine CSV-Datei, außerhalb des Repositoriums | `AUSLIEFERUNG`, `ReadOnly = 1` | Z0 (Werkzeug), Inhalt nach K8 |
| **Katalogimport der Verwaltung** | derselbe Katalog für Bestandsinstallationen, nie über den Schemaschritt (ZU14) | `AUSLIEFERUNG` | Z4 |
| **Katalogpflege** (`TwwNutzungsartCtrl`, später Katalogdialog) | eigene Nutzungsarten und Tagesgänge | `EIGEN`, geänderte Gruppen `EIGENKONSTRUKTION` | Z0 (Controller), Z4 (Dialog) |
| **VDI-4655-Import** mit Typtagzuordnung | Typtage des lizenzierten Anwenders, lokal | Herkunftsart `IMPORT`, `Tab_TwwTyptag_IMPORT` | Z4b, nach K3a/K8 |
| **Projektpaket** (`.wpx`) | fehlende Katalogzeile reist mit dem Projekt | `IMPORT`, ohne Beleg und Freigabe | Z0 |
| **Messdaten** | Kalibrierung eines Projekts | Zone, nicht Katalog | Z5, nach K5 |

Das Paketformat und die Prüfposten beschreibt das
[Setup-Konzept](../Konzept_Setup_InnoSetup_EPOS-Plan.md), Abschnitt 6.1.

---

## 8 Lizenzstand

| Punkt | Gegenstand | Stand |
|---|---|---|
| K1 | A100-Profildateien, Weißdruck-Status, DIN 4708-2/-3 anfragen; Verzicht auf eine Verwertungslizenz zur Mitauslieferung | entschieden nach Empfehlung (N1); Anfrage beim Anwender, Ergebnis offen |
| K8 | juristische Prüfung der Kapselung, der lokalen Normkopien und der Digitalisate des Bestandskatalogs | entschieden nach Empfehlung (N1); Prüfung beauftragt, Ergebnis offen |
| ZU15 | VDI-6002-Exemplare der Ablage mit fremdem Lizenzstempel: eigene Lizenz prüfen oder beschaffen | entschieden nach Empfehlung (N1); bis zum Ergebnis keine Weitergabe der extrahierten Tabellen |
| K3a | VDI-4655-Datenstrategie, Klärung mit VDI/Beuth vor der Codierung | Empfehlung vorausgesetzt, nicht entschieden |
| K5 | Freigabe von Messdaten | Empfehlung vorausgesetzt, nicht entschieden |

**Bis zum Ergebnis von K8** bleibt der Auslieferungskatalog leer: Die Vorlage führt nur, was ein
Katalogpaket mit `Status = 'AUSLIEFERUNG'` bringt, und ein solches Paket wird erst nach K8 erstellt.
Die Stufen Z1 und Z2 sind mit dem fiktiven Testkatalog abnehmbar (Kapitel 6 (d)).
