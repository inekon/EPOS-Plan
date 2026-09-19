# KI-1 — Klimadaten im eingebauten Wissen des Assistenten (Umsetzungsprotokoll, 19.09.2026)

Der KI-Assistent kennt die Klimadatenbedienung: die drei Quellen (PVGIS-TMY, DWD-Testreferenzjahr
aus Datei, TRY-Regionaldaten), was beim Import gespeichert und was verworfen wird, die Rubrik
Klimadaten der Einstellungen — und vier Meldungen des TRY-Imports mit Bedeutung, Ursache und
Abhilfe. Dazu ein Wächter, der eine solche Lücke künftig meldet, bevor sie ein Anwender findet.
Ausgangsstand: `3ba48bed` (KL-3), Zweig `ki1`.

---

## 1 Die Meldung

> „Der KI-Assistent kann die try Daten noch nicht finden bzw. kennt diese nicht."
> (Anwender, 19.09.2026)

## 2 Der Befund

Der Assistent beschafft sein Wissen an einer Stelle: `KiChatService.AbschnitteBeschaffenAsync`
mischt die Online-Seiten (`WikiWissen`) mit dem eingebauten Wissen (`HilfeWissen`). Beide Wege
waren für die Klimadaten leer:

* **Eingebautes Wissen.** `HilfeWissen.Basiswissen()` nannte zur Klimaregion einen einzigen Satz
  („wird oben im Hauptfenster gewählt", Bereich „Projekt"). Die Wörter *TRY*, *Testreferenzjahr*,
  *DWD*, *PVGIS* und *Regionaldaten* kamen im ganzen Einbauwissen nicht vor. Der Bereich
  `KiChatKontext.B_KLIMADATEN` war zugeordnet (Maskentabelle, Wiki-Seitentabelle) und trug **null**
  Abschnitte.
* **Wiki.** Die Quelle `Projekte/Wiki/Programm Dokumentation - Klimadaten.wiki` beschreibt alles,
  geht aber erst mit dem Sammel-Upload am 28.09.2026 online. Bis dahin — und bei jedem Anwender
  ohne Netz — half sie nicht.
* **Meldungserklärungen.** `KiMeldungskennung` führte 17 Kennungen (Speicherflotte,
  Simulationslauf, Strangampel), keine davon aus dem Klimaimport. Am Banner des Klimadialogs steht
  deshalb kein „erklären lassen".
* **Die eigentliche Lücke.** Es gab keinen Wächter, der prüft, ob ein Bedienbereich überhaupt
  Inhalt hat. Eine leere Rubrik fällt von selbst nicht auf: Der Assistent antwortet ja — nur eben
  ohne Kenntnis der Sache.

## 3 Die Umsetzung

### 3.1 Drei Abschnitte Bedienwissen (`HilfeWissen.Basiswissen`)

Im Bereich `KiChatKontext.B_KLIMADATEN`, destilliert aus der Wiki-Quelle (Funktion, wie sie ist —
kein „seit/bisher", keine Hersteller- und Produktdaten):

| Abschnitt | Inhalt |
|---|---|
| „Klimadaten importieren: PVGIS, Testreferenzjahr (DWD-Datei) und TRY-Regionaldaten" | Menüpfad Administration › Klimadaten, Umfang einer Klimaregion, die drei Quellen mit ihren Eingaben: Ortsname oder Koordinaten; Dateiwahl `.dat` samt Standort aus dem Dateikopf (änderbar); Jahr 2015/2045, Szenario mittel/sommerwarm/winterkalt, „Region ermitteln", Teilabruf oder lokales Archiv; Abbruch und Dublettenhinweis |
| „Klimadaten: was beim Import gespeichert und was verworfen wird" | übernommene Größen und die daraus gerechneten, Gegenstrahlung/Luftfeuchte/Bedeckungsgrad je Quelle, die sechs nicht übernommenen Größen, Herkunft, Importdatum und Lizenzvermerk in den Details, Bezugsweg und Lizenz der Quellen, Näherung der Regionszuordnung |
| „Einstellungen, Rubrik Klimadaten: die Adressen von PVGIS, DWD und TRY-Regionaldaten" | die drei Adressfelder, „Standardwerte", Übernahme erst mit „Speichern", Werksvorgabe bei einer Bestandsinstallation |

**Die Suchworte stehen im Titel.** `HilfeWissen.Suchen` bewertet Titel dreifach, Bereich doppelt,
Inhalt einfach — und `Zerlegen` verwirft jedes Wort unter vier Zeichen. **„TRY" und „DWD" allein
zählen deshalb nicht**, und das bleibt so: Eine Mindestlänge von drei ließe „die", „der", „wie"
mitzählen und machte jede Frage unschärfer (die Kürzelliste aus Konzept 7.1 wäre der eigene
Schritt dafür). Getragen wird die Suche von den langen Begriffen — *Klimadaten*,
*Testreferenzjahr*, *Regionaldaten*, *PVGIS*, *importieren*, *einlesen*, *Datei* —, die in allen
drei Titeln stehen; „TRY-Daten" trägt über *Daten*, das in „Klimadaten" steckt.

**Einsprachig, wie das übrige Einbauwissen.** Die Wissensbasis ist deutsch (so halten es
`Basiswissen`, `Berechnungswissen` und `Aktionswissen`); zweisprachig sind die Oberflächentexte des
Weges. Eine englische Fassung der Wissensbasis wäre die erste ihrer Art und ist ein eigener Schritt.

### 3.2 Vier Meldungserklärungen (`KiMeldungskennung`, `HilfeWissen.Aktionswissen`)

| Kennung | Meldung | Abhilfe im Abschnitt |
|---|---|---|
| `KLIMA_TRY_KEIN_BEREICH` | Die Adresse erlaubt keine Teilabrufe | Archiv herunterladen und als Datei wählen; Adresse in den Einstellungen prüfen. Das ganze Archiv lädt das Programm nie stillschweigend |
| `KLIMA_TRY_AUSSERHALB` | nächster Regionsmittelpunkt weiter als 300 km (`TryPaketLeser.MAX_ENTFERNUNG_KM`) | Koordinaten prüfen (Verwechslung der Felder), „Region ermitteln" vorschalten; außerhalb Deutschlands PVGIS nehmen |
| `KLIMA_TRY_FORMATFEHLER` | Datei oder Datensatz nicht lesbar — mit Zeile, Spaltenzahl, Feldwert, Trennzeile, Zeitfeld, doppelter Stunde | Datei unverändert neu beziehen, nicht in einem Tabellenprogramm speichern; Archiv auf Vollständigkeit prüfen |
| `KLIMA_TRY_STANDORT_UNLESBAR` | Kopf der TRY-Datei ohne lesbaren Rechts-/Hochwert | Längen- und Breitengrad eintragen oder Ortsnamen auflösen; die eigene Eingabe gilt vor dem Dateikopf |

Muster wie `FLOTTE_*` und `PV_STRANG_*`: BEDEUTUNG, URSACHE, ABHILFE, WIKI im Inhalt, die Kennung
im Titel, `QuellUrl` auf die Wiki-Seite (`…/Programm_Dokumentation/Klimadaten#einlesen` bzw.
`#standort`). **Die Kennungen heißen wie die Ressourcenschlüssel ihrer Meldungen** — dieselbe
Zeichenkette, unter der `KlimaImportAblauf` und `TryPaketLeser` den Text bauen; wer sie aus einer
Meldung abschreibt, findet den Abschnitt. Dazu je eine Ressource `KI_FRAGE_<Kennung>` in beiden
Sprachen (`Resource.resx`, `Resource.en-US.resx`), Designer neu erzeugt.

### 3.3 Die Wache: jeder Bereich hat Inhalt (`EPOS.Kern.Tests/KiWissensdeckungTests`)

Jeder Bereich aus `KiChatKontext.Bereiche` (ohne den Ersatzwert „Unbekannter Bereich") hat
mindestens einen Abschnitt im eingebauten Wissen — **oder** steht in einer Ausnahmeliste **mit
Grund**. Gezählt wird Gleichheit des Bereichsnamens: Die Rechenwegseiten tragen alle „Berechnung",
ein örtlicher Hilfe-Cache „Online-Hilfe"; beide decken keinen Bedienbereich ab.

**Bestand am 19.09.2026: sieben von 27 Bereichen gefüllt.**

* gefüllt: Klimadaten (neu, 3 + 4 Abschnitte), Photovoltaik (8), Stromspeicher (7), Simulation
  Konfiguration (2), Wärmepumpe (2), Hilfe (1), Simulation (1);
* Ausnahmeliste, 20 Einträge: Administration, Projektassistent, Bericht, BHKW, Brauchwasser,
  Gebäude, Hauptfenster, Heizkessel, Kosten und Preise, Lizenz, Projektverwaltung, Prozesswärme,
  Pufferspeicher, Solarthermie, Stromverbraucher, Varianten, Wärmebedarf, Wirtschaftlichkeit,
  Wärmequelle Erdreich, Detaillierte Simulation. Neun davon tragen den Grund „Rechenweg vorhanden,
  Bedienwissen ausstehend" (es gibt eine Seite der Rubrik Berechnung), elf „Bedienwissen
  ausstehend".

Zwei Gegenproben halten die Liste ehrlich: Ein Eintrag muss ein Bereich der Positivliste sein und
einen Grund nennen, und er fällt rot aus, **sobald der Bereich Inhalt bekommt** — dann wird er
gestrichen, nicht gepflegt. „Klimadaten" darf nicht darin stehen.

### 3.4 Die Suchprobe

Vier Anwenderfragen — „Wie importiere ich TRY-Daten?", „Testreferenzjahr einlesen", „Klimadaten aus
einer DWD-Datei anlegen", „Woher kommen die Regionaldaten der Testreferenzjahre?" — liefern als
ersten Treffer einen Klimadaten-Abschnitt; „PVGIS Adresse in den Einstellungen" führt auf die
Rubrik der Einstellungen. Kontext ist bewusst `BEREICH_UNBEKANNT`, damit der Bereichsbonus das
Ergebnis nicht trägt, und die Kennung ausdrücklich leer. Gegenprobe: „Bivalenzpunkt und Heizstab"
findet weiterhin seinen eigenen Abschnitt — das neue Wissen drängt sich nicht vor. Die Kultur ist
über `Kulturvorrichtung` gepinnt, die Klasse steht in der seriellen Sammlung.

## 4 Was bewusst nicht geschehen ist

* **Die anderen 20 Bereiche bleiben leer.** Sie stehen mit Grund in der Ausnahmeliste; sie zu
  füllen ist Textarbeit je Gewerk und ein eigener Auftrag.
* **Keine Änderung an Suche und Gewichtung.** Kein Kürzelverzeichnis, keine kleinere
  Mindestwortlänge — beides wirkte auf jede Frage.
* **Kein Eingriff in den Klimadialog.** Das Banner des Dialogs trägt weiterhin keine Kennung, der
  Link „erklären lassen" erscheint dort also nicht; die vier Abschnitte sind über die
  Stichwortsuche und über `HilfeWissen.AbschnittFuerKennung` erreichbar (offener Punkt 6a).
* **Kein Referenzlauf.** Kein Rechenweg ist berührt: geändert sind Wissenstexte, Kennungen,
  Ressourcen und Tests.

## 5 Nachweise

| Probe | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler, 5 Warnungen (Bestand) |
| `KiWissensdeckungTests` (neu) | 9 Fälle grün |
| Filter `Ki\|HilfeWissen\|WikiProduktdatenWache\|DokumentationLinkWache`, `de-DE` | Kern 281, UI 352, KiKern 499, Engine 1 — alle grün |
| derselbe Filter, `LC_ALL=en_US.UTF-8` | dieselben Zahlen, alle grün |
| volle Suite `WP-Plan.Kern.slnf` nach dem Merge | siehe Statuszeile |
| Referenzlauf | nicht nötig (kein Rechenweg berührt) |

Kodierung: `.cs` und `.resx` UTF-8 **mit** BOM und LF wie im Bestand, Markdown ohne BOM;
`Resource.Designer.cs` ausschließlich über `Werkzeuge/ResourceDesigner/designer_neu.py schreiben`
(4 neue Blöcke, zweiter Lauf +0 — wiederholbar).

## 6 Offene Punkte

a) **„Erklären lassen" im Klimadialog.** Damit der Link am Banner erscheint, muss die Kennung von
   der Stelle, an der die Meldung entsteht, bis zum Banner reisen: ein Feld `Kennung` an
   `KlimaImportErgebnis` und `KlimaVorschauErgebnis`, gesetzt an den vier Fehlerstellen in
   `KlimaImportAblauf`/`TryPaketLeser`, und im `KlimadatenDialog` durchgereicht (`Kennung`,
   `Hilfeschluessel`, `Dialogname` am `<Warnbanner>`). Nicht in diesem Auftrag gemacht, weil der
   Auftrag KL-4 zur selben Zeit denselben Dialog umbaut.

b) **Die Wiki-Seite zieht nach.** „Programm Dokumentation/Klimadaten" geht mit dem Sammel-Upload am
   28.09.2026 online. Bis dahin ist das eingebaute Wissen die einzige Quelle des Assistenten zu
   diesem Thema — und danach die, die ohne Netz trägt.

c) **Wissen altert mit der Bedienung.** Die Wache prüft, DASS ein Bereich Inhalt hat, nicht, dass
   er stimmt. Die Regel dazu steht jetzt im Konzept (7.1): Wer eine Bedienfunktion ändert, zieht
   den Abschnitt mit.
