# H13 — Hilferubrik „Berechnung" (Umsetzungsprotokoll Teil A, 06.09.2026)

Vorgänger: `H1H2_Umsetzung_Protokoll.md` (Wiki-Katalog, Anker-Durchlass A3, Abschaltlogik),
`H7_InfoButtons_Protokoll.md` (`InfoKnopf`, Flächendeckung), `H11_Sammelpaket_Protokoll.md`
(Popup-Kurzbeschreibungen, Rangfolge der Bezugsquellen), `H12_FeldHilfe_Protokoll.md`
(feldgenaue Hilfe, Trennlinie der Abschaltlogik).

**Ergebnis in einem Satz:** Die Rechenwege stehen ab sofort in einer **eigenen Wikirubrik**
`Programm Dokumentation/Berechnung`, der Katalog erreicht sie auch, solange das Wiki sie noch
nicht führt, der KI-Assistent kennt sie **ohne Netz**, und sieben Infoknöpfe in sechs Dialogen
führen dorthin. Teil A liefert die Infrastruktur und **sechs** der dreizehn Seiten; Teil B
(`H13b_…`) liefert die sieben Erzeugerseiten.

---

## 1. Der Anwenderwunsch (06.09.2026, wörtlich, drei Nachrichten)

> „Erweiterung Hilfe: Erläutere in der Hilfe jeweils die Berechnungswege, Warmwasser,
> Brauchwasser, Pufferspeicher … PV-Module-Berechnung, optional mit Wechselrichter,
> Solarthermie. Setze entsprechende Hilfen an die Info-Buttons in den relevanten Dialogen."
>
> „und weitere Komponenten (mit Hilfe Berechnung)."
>
> „(die Details der Berechnung sollten in einer Separaten Hilferubrik auf der wiki sein und
> nicht in der allgemeine Erklärung der Funktionen. Die Erläuterung sollte aber aufrufbar
> sein aus den allgemeinen Erklärungen mit Bezügen)."

Daraus die drei Auflagen, gegen die dieses Paket zu messen ist:

| | Auflage | Wo eingelöst |
|---|---|---|
| **A** | Die Rechenwege stehen in einer **eigenen Rubrik**, nicht in den allgemeinen Seiten | § 3 (Katalog), § 4 (die Texte) |
| **B** | Aus den allgemeinen Seiten führen **Bezüge** dorthin | `_Bezuege.wiki` — 18 kopierfertige Abschnitte, § 5 |
| **C** | Die **Infoknöpfe der Dialoge** führen dorthin | § 6, sieben Schlüssel in sechs Dialogen |

---

## 2. Die Bauform — was für JEDE Seite gilt

**Ort.** `EPOS.Kern/Allgemein/Hilfe/Berechnung/<Seitenname>.wiki`, eine Datei je Wikiseite.
Der Dateiname ist der Seitenname mit Umlauten wie im Wiki. Eine Datei mit **führendem
Unterstrich** ist keine Seite, sondern Beiwerk für den Anwender.

**Inhalt.** MediaWiki-Markup, unverändert in die Wikiseite kopierbar. Formeln stehen als
vorformatierte Zeilen (führendes Leerzeichen), nicht als `<math>` — das setzt keine
Wiki-Erweiterung voraus.

**Kopfblock** in Zeile 1 bis 4, als Wiki-Kommentar:

```
<!-- EPOS-Plan Hilferubrik Berechnung | Seite: Photovoltaik | Stand: 2026-09-06 | Rechenkern:
     EPOS.Kern/Allgemein/Simulation/SimulationPV.cs, … -->
```

Er ist im Wiki unsichtbar, wird beim Kopieren mitgenommen und ist die **Beleglage** des Textes.
Im Klartext für den Assistenten ist er entfernt — Quelltextpfade gehören nicht in einen Prompt
und nicht in den sichtbaren Text.

**Sechs Abschnitte, überall dieselben:**

1. `== Was berechnet wird ==` — zwei, drei Sätze, die Ergebnisgrößen.
2. `== Eingangsgrößen ==` — Tabelle: Größe | Einheit | Herkunft im Programm (welcher Dialog,
   welches Feld, welcher Katalog).
3. `== Rechenweg ==` — nummerierte Schritte, je Schritt die Formel in Worten UND als Zeile.
4. `== Grenzen und Annahmen ==` — was der Rechenkern **nicht** tut.
5. `== Ergebnisse und wo sie stehen ==` — Reiter, Bericht, Kennzahlen, Laufprotokoll.
6. `== Bezüge ==` — die allgemeine Seite und die verwandten Rechenwege.

**Jede Zahl kommt aus dem Rechenkern.** Vorgabewerte stehen mit ihrer Zahl da und sind als
Vorgabe gekennzeichnet. Was ein Konzeptdokument erst vorsieht, ist als **Ausblick** markiert:
„in Umsetzung, Stand 06.09.2026".

---

## 3. Der Katalog kennt die Rubrik (A1)

### 3.1 Der Abruf — nichts zu tun, aber gemessen

`WikiHelpCatalog` lädt über `api.php?action=query&list=allpages&apprefix=Programm Dokumentation/`.
`apprefix` ist ein **reiner Zeichenketten-Präfix**, kein Namensraumfilter — am 06.09.2026 gegen
`wiki.epos-plan.de` gemessen: `apprefix=Programm Dokumentation/W` liefert genau die vier Seiten
mit W (Wirtschaftlichkeit, Wärmebedarf, Wärmepumpe, Wärmequelle Erdreich). Eine Seite **zweiter
Stufe** kommt damit von selbst mit, sobald sie im Wiki steht; `EintragAusTitel` nimmt sie auf,
ihr Kurzname lautet dann `Berechnung/Photovoltaik`.

Derselbe Abruf zeigte: Das Wiki führt heute **32** Seiten, keine davon in der neuen Rubrik.

### 3.2 Der Zustand DAVOR — hier war etwas zu tun

Die Rangfolge F6 lautet: Online **schlägt** lokale Sicherung **schlägt** mitgelieferten
Startbestand — und zwar vollständig, nicht feldweise. Für die 32 allgemeinen Seiten ist das
richtig: Wer eine davon im Wiki löscht, soll sie nicht durch eine veraltete Beilage
wiederauferstehen sehen.

Für die neue Rubrik ist es falsch. Ihre Seiten legt der Anwender **erst noch** an (§ 8); bis
dahin antwortet das Wiki erfolgreich und ohne sie, der Startbestand fällt weg, und jeder Knopf
„Berechnungsweg …" wäre stumm — unter Windows **abgeschaltet** (F3), im Blazor-Dialog
folgenlos.

**Die Regel, eng gefasst** (`WikiHelpCatalog.BerechnungsRueckfallErgaenzen`):

> Nach einem erfolgreichen Abruf wird ausschließlich das ergänzt, was unter
> `Programm Dokumentation/Berechnung/` liegt **und** im Abruf fehlt. Sobald der Anwender eine
> Berechnungsseite anlegt, gewinnt die Wikifassung — sie steht schon im Abruf, und ergänzt
> wird nur Fehlendes. Für alle übrigen Seiten bleibt F6 unangetastet.

Derselbe Rückfall greift beim Laden der **lokalen Sicherung**: Eine Sicherung aus der Zeit vor
diesem Paket stammt aus einem Abruf, den es damals noch nicht gab.

Die ergänzten Einträge wandern mit in die Sicherung — der nächste Start ohne Netz kennt die
Rubrik dann ebenfalls.

### 3.3 Die Auflösung eines Ziels `Berechnung/<Seite>`

Ein Ziel mit Schrägstrich läuft über den **Pfadweg**: `PfadNormalisieren` macht
`/berechnung/photovoltaik/` daraus, `UeberPfad` findet den Katalogpfad
`/wiki/programm_dokumentation/berechnung/photovoltaik/` über sein eindeutiges Pfadende. Der
Slugweg bleibt unberührt — der Slug `berechnung/photovoltaik` kollidiert mit keinem der
bestehenden.

**Eine Lücke war dabei.** MediaWiki bildet Leerzeichen im Titel auf Unterstriche in der Adresse
ab. Das Ziel `Berechnung/Wärmequelle Erdreich` hätte die Seite
`…/Berechnung/W%C3%A4rmequelle_Erdreich` nie getroffen. `PfadNormalisieren` ebnet Leerzeichen
jetzt zu Unterstrichen ein; **keine** der 32 vorhandenen Adressen führt ein Leerzeichen, für
sie ändert sich nichts. Der Slugweg tat das längst — er vergleicht den Kurznamen unverändert.

### 3.4 Der Startbestand `help_cache.json`: 32 → 46 Einträge

Neu sind die **Rubrikseite** `Berechnung` und die **13 Seiten** der Rubrik — die sechs aus Teil A
und die sieben aus Teil B, damit die Rubrik von der ersten Auslieferung an vollständig ist.
Form je Eintrag:

| Feld | Wert |
|---|---|
| Schlüssel | `/wiki/programm_dokumentation/berechnung/<seite>/` (Leerzeichen als Unterstrich, klein) |
| `Tooltip` | `Berechnung: <Thema>` |
| `Url` | `https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Berechnung/<Seite>` |
| `Slug` | `Berechnung/<Thema>` |
| `Beschreibung` | ein Satz aus „Was berechnet wird" |

`WikiHelpCatalog.Kapitelname` dreht `Berechnung/Photovoltaik` auch beim **online** gelesenen
Eintrag zu `Berechnung: Photovoltaik` — Beilage und Abruf zeigen denselben Kapitelnamen.

### 3.5 iOS — unverändert

`EPOS.iOS/Dienste/IosHilfeDienst` trägt die neue Adresse schon: `Adresse("Berechnung/Photovoltaik")`
läuft in `WikiWissen.SeitenUrl`, das an den Schrägstrichen trennt und je Teil kodiert — Ergebnis
`…/wiki/Programm_Dokumentation/Berechnung/Photovoltaik`. **Keine Änderung nötig.** Was iOS
weiterhin fehlt, ist der Katalog selbst (Kurztext und Beschreibung; gehört zu iU11) — der
Knopf ist sichtbar und öffnet die richtige Seite.

---

## 4. Der Kern-Baustein und das KI-Wissen (A2)

**Warum die Texte im Kern liegen und nicht nur im Wiki.** Drei Verbraucher wollen dieselben
Sätze:

1. **das Wiki** — der Anwender kopiert die Datei unverändert in die Seite; deshalb
   MediaWiki-Markup und kein eigenes Format,
2. **der KI-Assistent** — er soll den Rechenweg auch **ohne Netz** kennen und bevor die Seiten
   angelegt sind,
3. **die Prüfung** — ein Text im Wiki altert unbemerkt, einer im Quellbaum nicht.

`EPOS.Kern/Allgemein/Hilfe/BerechnungsHilfe.cs` liest die eingebetteten `*.wiki` und liefert je
Seite `Seitenname`, `Titel` (`Berechnung: <Thema>`), `Ziel` (`Berechnung/<Thema>`), `WikiTitel`,
`Stand`, `Rechenkern`, `Markup` und `Klartext`. Eingebettet werden sie über **ein** Muster in
`EPOS.Kern.csproj`:

```xml
<EmbeddedResource Include="Allgemein\Hilfe\Berechnung\*.wiki"
                  LogicalName="EPOS.Kern.Hilfe.Berechnung.%(Filename)%(Extension)" />
```

Der Leser ist plattformfrei — nur `Assembly` und Zeichenketten, kein Netz, kein `Dienste.*`.

**Der Klartext** nimmt die Auszeichnung und lässt jedes Wort und jede Zahl stehen. Tabellenzellen
werden zu einer Zeile mit „ | " dazwischen, Formelzeilen bleiben Zeichen für Zeichen. Das Muster
für HTML-Reste ist eng gefasst (`</?[A-Za-z][A-Za-z0-9]{0,15}\s*/?>`): Ein weites Muster
(„alles zwischen `<` und `>`") machte aus der Formelzeile `P < 0 und Q > 1` das sinnlose `P 1`.

**`HilfeWissen`** hängt je Seite einen `WissensAbschnitt` an — Titel `Berechnung: <Thema>`,
Bereich `Berechnung`, Inhalt = Klartext. Suche und Gewichtung sind **unverändert**; im Titel
zählt ein Wort dreifach, im Bereich doppelt, also trifft „Wie wird die Photovoltaik berechnet?"
den Rechenweg und nicht die Bedienhilfe.

---

## 5. Rubrik-Startseite und Bezüge (A3)

| Datei | Was der Anwender damit tut |
|---|---|
| `_Index.wiki` | Legt die Seite `Programm Dokumentation/Berechnung` an und fügt diesen Text ein: was die Rubrik ist, Tabelle **aller 13** Seiten, Lesehinweise (Zeitraster, Vorgabewerte, Ausblick-Kennzeichnung, Ingenieurvorbehalt). |
| `_Bezuege.wiki` | Enthält **18** kopierfertige Abschnitte `== Berechnung ==`, je einen für Wärmebedarf, Gebäude, Brauchwasser, Prozesswärme, Stromverbraucher, Strombedarf, Simulation, Wärmequelle Erdreich, Klimadaten, Heizkessel, BHKW, Wärmepumpe, Pufferspeicher, Solarthermie, Photovoltaik, Stromspeicher, Energieerzeuger und Simulationsergebnisse. Jeder Block nennt oben die Zielseite. |

Beide Dateien tragen einen führenden Unterstrich und sind damit **keine** Seiten im Sinne von
`BerechnungsHilfe` — sie stehen nicht im Wissen des Assistenten und in keiner Seitenliste.

---

## 6. Die Seiten und ihre Infoknöpfe (A4)

### 6.1 Die sechs Seiten aus Teil A

| Seite | Inhalt in einem Satz |
|---|---|
| `Simulationsablauf` | Zeitraster, Vorprüfungen, gemeinsamer Kalender, Zeitbasis Ortszeit, die fünf Laufphasen, die Stundenfolge der Kaskade, Ladeprioritäten, Schaltschwellen, Warnkriterienkatalog. |
| `Wärmebedarf` | Klimadaten und Kalender, die drei Gebäudefunktionen (solare Gewinne, spezifischer Wärmeverlustkoeffizient, tägliche Heizlast im 24‑h‑Kapazitätsmodell), Tag→Stunde, Flächenrückrechnung aus dem Verbrauch, externe Ganglinien, Netzverluste, Summenvektor und Energieprobe. |
| `Brauchwasser` | Die gemeinsame Profilroutine im Einzelnen, der VDI‑6002-Katalog (11 Wochenprofile, 13 Monatswertsätze, vier Normsätze), Zapfmenge → Jahresmenge, Kaltwasserfaktor. |
| `Prozesswärme` | Dieselbe Routine für den Prozesskanal, der Faktor‑1000-Befund und die Einheit am Wert, „monatlicher Verlauf". |
| `Strombedarf` | Profile plus Messlastgänge, Spreizung Stunde → Viertelstunde, Rasterprüfung, was der Import normalisiert (Einheit, Schaltjahr, Sommerzeit, Zeitstempelkonvention), Kennzahlen, Stellung im Lauf, Peak-Shaving als Auswertung. |
| `Wärmequelle Erdreich` | Jahresgang der Außenluft aus den Monatsmitteln, Kusuda mit Dämpfungstiefe, der 13‑zeilige Bodentypkatalog, Sondentemperatur, die Weiche Kollektor/Sonde, Entzugsgrößen nach dem Lauf, VDI‑4640-Prüfung für Kollektor und Sonde, Frostbedingung. |

### 6.2 Die Schlüssel

Muster: **ein ZWEITER Schlüssel** `<Formname>.Berechnung` neben dem bestehenden
`<Formname>.btn_Help`. Der Fensterknopf oben rechts bleibt, was er ist — die Bedienhilfe;
der neue Knopf sitzt am Kopf des Abschnitts, der die Rechnung parametriert.

| Schlüssel | Dialog | Stelle im Dialog | Ziel |
|---|---|---|---|
| `Form_Simulation_Config.Berechnung` | `SimulationKonfigSeite` | Abschnitt „Komponenten der Simulation" | `Berechnung/Simulationsablauf` |
| `Form_Gebaeude.Berechnung` | `GebaeudeDialog` | Abschnitt „Gebäude: Verbrauch" | `Berechnung/Wärmebedarf` |
| `Form_Waermebedarf.Berechnung` | `WaermebedarfExternDialog` | Kopf, neben der Fensterhilfe | `Berechnung/Wärmebedarf` |
| `Form_Brauchwasser.Berechnung` | `BedarfsProfileDialog` (Ausprägung Brauchwasser) | Kopf, neben der Fensterhilfe | `Berechnung/Brauchwasser` |
| `Form_Prozesswaerme.Berechnung` | `BedarfsProfileDialog` (Ausprägung Prozesswärme) | Kopf, neben der Fensterhilfe | `Berechnung/Prozesswärme` |
| `Form_Stromverbraucher.Berechnung` | `BedarfsProfileDialog` (Ausprägung Stromverbraucher) | Kopf, neben der Fensterhilfe | `Berechnung/Strombedarf` |
| `Form_Stromganglinie.Berechnung` | `StromganglinieDialog` | Kopf, neben der Fensterhilfe | `Berechnung/Strombedarf` |
| `Form_QuelleErdreich.Berechnung` | `QuelleErdreichDialog` | vor dem Abschnitt „Standort und Boden" | `Berechnung/Wärmequelle Erdreich` |

Die Zeilen stehen in `help_mapping.txt` in einem eigenen Abschnitt **„H13 - Rubrik
Berechnung"** am Dateiende; sein Kommentarkopf begründet die Zielform und nennt den
Rückfallmechanismus aus § 3.2.

**Keine neuen Ressourcenschlüssel.** Kurztext und Schlüssel sind `[Parameter]` mit deutschem
Rückfall; die drei Ausprägungen des `BedarfsProfileDialog` versorgt
`BedarfsProfileHuelle.BerechnungsSchluessel`/`BerechnungsKurztext` über `Text_`/`TextEinfach`,
das ohne Ressourcenschlüssel auf den deutschen Wortlaut fällt.

---

## 7. Nachweise

### 7.1 Prüfstand `EPOS.Kern.Tests/BerechnungsHilfeTests` — **17/17 grün**

| Fall | Was er festhält |
|---|---|
| Rubrik eingebettet | mindestens eine Seite — ein Tippfehler im `LogicalName` fällt hier auf |
| keine doppelten Seitennamen | zwei Seiten gleichen Namens wären im Wiki eine |
| Kopfblock vollständig | Seite, Stand und Rechenkern sind belegt |
| Stand ist ein Datum | Form `JJJJ-MM-TT` |
| Kopfblock nennt Kerndateien | mindestens ein Pfad unter `EPOS.Kern/` |
| sechs Abschnitte je Seite | die Gliederung der Bauform |
| Verweis in die allgemeine Rubrik | die Rubrik ist keine Sackgasse (Auflage B) |
| `_`-Dateien sind keine Seiten | **mit Gegenprobe**, dass es sie überhaupt gibt |
| Klartext ohne Kopfblock/Auszeichnung | kein `<!--`, kein `EPOS.Kern/`, kein `==`, kein `'''`, kein `[[` |
| Klartext **behält** Wörter, Zahlen, Formeln | Gegenprobe an einem Ausschnitt mit jeder vorkommenden Form |
| Vergleichszeichen überleben | `P < 0 und Q > 1` bleibt stehen |
| Kopfblockleser versteht den Umbruch | zwei Zeilen, drei Felder |
| ohne Kopfblock alles leer | kein Wurf, kein Raten |
| Nachschlagen über Name und Ziel | `Photovoltaik`, `photovoltaik`, `Berechnung/Photovoltaik` |
| Titel/Ziel/Wikititel | folgen der Rubrik |
| je Seite ein Wissensabschnitt | Titel, Bereich und Inhalt stimmen |
| die Suche findet ihn | „Berechnung <Thema>" trifft die Seite |

### 7.2 Wächter `EPOS.UI.Tests/BerechnungsknopfTests` — **7/7 grün**

Er hält **beide Richtungen**: Jede Zeile `<Form>.Berechnung` in `help_mapping.txt` hat einen
Knopf, und jeder Schlüssel im Quelltext hat eine Zeile. Dazu: ein Schlüssel gehört genau einer
**Razor-Komponente** (dass er zusätzlich in einer Hülle steht, ist bei einer Komponente mit
mehreren Ausprägungen der Regelfall), jedes Ziel zeigt in die Rubrik, jede Razor-Datei mit
Schlüssel trägt einen `<InfoKnopf`. Zwei Gegenproben belegen, dass der Leser das Muster findet
und den Bestand sieht.

Der Fall liest den **Quelltext**, weil `help_mapping.txt` im WinForms-Projekt liegt
(`net10.0-windows`) — derselbe Weg wie `HuellenwegTests` und `StilblattTests`.

### 7.3 Builds und Testläufe

| Lauf | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` (nach `rm -rf obj/bin`) | **0 Fehler, 7 Warnungen** — Stand der Basis, keine neue |
| `dotnet build WindowsFormsApplication1 -c Release -p:EnableWindowsTargeting=true` (nach `rm -rf obj/bin`) | **0 Fehler, 1 Warnung** (`WFO0003`, Bestand) |
| `dotnet test EPOS.Kern.Tests -c Release` | **1365/1365 grün** |
| `dotnet test EPOS.UI.Tests -c Release` | **2731/2731 grün** |

Rechenweg, SQL und Ressourcen-Designer sind **nicht** angefasst; der Referenzlauf ist damit
unberührt.

---

## 8. Was der Anwender im Wiki tun muss

Ohne diese Handgriffe bleibt die Rubrik leer, und die Infoknöpfe zeigen Kurztext und Adresse
aus dem mitgelieferten Startbestand — sie sind also **wirksam, aber ohne Seiteninhalt**.

1. **Rubrikseite anlegen:** neue Seite `Programm Dokumentation/Berechnung`, Inhalt aus
   `EPOS.Kern/Allgemein/Hilfe/Berechnung/_Index.wiki` einfügen.
2. **Je Rechenwegseite eine Unterseite anlegen:** `Programm Dokumentation/Berechnung/<Seitenname>`,
   Inhalt aus der gleichnamigen `.wiki`-Datei **unverändert** einfügen. Der Kommentarkopf bleibt
   drin — er ist im Wiki unsichtbar und sagt, gegen welchen Programmstand der Text geschrieben ist.
   Die Namen **zeichengleich** übernehmen, Umlaute eingeschlossen: `Wärmebedarf`,
   `Prozesswärme`, `Wärmequelle Erdreich`.
3. **Bezüge einfügen:** die Blöcke aus `_Bezuege.wiki` in die genannten allgemeinen Seiten
   kopieren — ans Ende, vor einem etwaigen Abschnitt „Siehe auch".
4. **Rubrikseite „Programm Dokumentation" ergänzen:** im Abschnitt „Hilfeseiten" die neue
   Unterrubrik nennen, damit die Vertragsliste vollständig bleibt.
5. **Umbenennungen nur mit bleibender Weiterleitung.** Ein ausgeliefertes Programm trägt
   `help_mapping.txt` eingebettet bei sich und altert bis zum nächsten Release.

Nach dem ersten Onlineabruf, der die Seiten sieht, gewinnt die Wikifassung; der Startbestand
tritt zurück.

---

## 9. Abnahme auf Windows

| Punkt | Was zu prüfen ist | Erwartung |
|---|---|---|
| **A‑H13‑1** | Simulationskonfiguration öffnen, im Abschnitt „Komponenten der Simulation" den zweiten Infoknopf überfahren und anklicken | Kurztext „Berechnung: Simulationsablauf"; das Popup nennt Kapitel, Einleitungssatz und die Adresse `…/Berechnung/Simulationsablauf` |
| **A‑H13‑2** | Dasselbe im Gebäudedialog (Abschnitt „Gebäude: Verbrauch") und im Dialog „Wärmebedarf Extern" | beide führen auf `…/Berechnung/Wärmebedarf` |
| **A‑H13‑3** | Die drei Bedarfsprofil-Dialoge (Brauchwasser, Prozesswärme, Standard Stromprofil) öffnen | je ein zweiter Knopf im Kopf, je eigenes Ziel; die Kurztexte lauten „Berechnung: Brauchwasser" / „: Prozesswärme" / „: Strombedarf" |
| **A‑H13‑4** | Dialog „Stromlastgang" und Dialog „Wärmequelle Erdreich" | zweiter Knopf vorhanden und wirksam |
| **A‑H13‑5** | **Ohne Netz starten** (Netzwerk trennen), dieselben acht Knöpfe prüfen | alle wirksam — der Startbestand trägt sie |
| **A‑H13‑6** | **Mit Netz starten, bevor die Wikiseiten angelegt sind** | alle acht Knöpfe weiterhin wirksam (§ 3.2); im Ausgabefenster steht die Zeile „H13: n Seite(n) … stehen im Wiki noch nicht" |
| **A‑H13‑7** | Wikiseiten nach § 8 anlegen, Programm neu starten | die Knöpfe zeigen jetzt den Einleitungssatz **der Wikiseite**; die Adresse ist unverändert |
| **A‑H13‑8** | Hilfe-Assistent: „Wie wird der Wärmebedarf berechnet?" — auch ohne Netz und mit „Nur suchen" | der Abschnitt „Berechnung: Wärmebedarf" wird gefunden und zitiert |
| **A‑H13‑9** | Die sechs Seiten im Wiki gegenlesen | jede Zahl, jeder Vorgabewert und jede Formel stimmen mit dem Programm überein; die Abschnitte „Grenzen und Annahmen" sind fachlich einverstanden |
| **A‑H13‑10** | Englische Oberfläche einstellen und einen der neuen Knöpfe drücken | die Seite geht durch den Übersetzungs-Proxy auf (Entscheid 7.1a — es gibt keine englischen Wikiseiten) |

---

## 10. Geänderte und neue Dateien (Teil A)

**Neu**

- `EPOS.Kern/Allgemein/Hilfe/BerechnungsHilfe.cs`
- `EPOS.Kern/Allgemein/Hilfe/Berechnung/_Index.wiki`, `_Bezuege.wiki`
- `EPOS.Kern/Allgemein/Hilfe/Berechnung/Simulationsablauf.wiki`, `Wärmebedarf.wiki`,
  `Brauchwasser.wiki`, `Prozesswärme.wiki`, `Strombedarf.wiki`, `Wärmequelle Erdreich.wiki`
- `EPOS.Kern.Tests/BerechnungsHilfeTests.cs`
- `EPOS.UI.Tests/BerechnungsknopfTests.cs`
- `WindowsFormsApplication1/Allgemein/Hilfe/H13_Berechnungshilfe_Protokoll.md` (diese Datei)

**Geändert**

- `WindowsFormsApplication1/Allgemein/Hilfe/HelpCatalog.cs` — `BerechnungsPraefix`,
  `Kapitelname`, `StartbestandLesen`, `BerechnungsRueckfallErgaenzen`, `IstBerechnungsseite`,
  Leerzeichen→Unterstrich in `PfadNormalisieren`
- `WindowsFormsApplication1/Allgemein/Hilfe/help_cache.json` — 32 → 46 Einträge
- `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt` — Abschnitt „H13 - Rubrik Berechnung", 8 Zeilen
- `WindowsFormsApplication1/Views/Bedarf/BedarfsProfileHuelle.cs` — Schlüssel und Kurztext je Ausprägung
- `EPOS.Kern/EPOS.Kern.csproj` — Einbettung der `.wiki`
- `EPOS.Kern/Allgemein/KI/HilfeWissen.cs` — `Berechnungswissen()`
- `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor`
- `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`, `WaermebedarfExternDialog.razor`,
  `BedarfsProfileDialog.razor`
- `EPOS.UI/Dialoge/Strom/StromganglinieDialog.razor`
- `EPOS.UI/Dialoge/Simulation/QuelleErdreichDialog.razor`

**Nicht angefasst:** Rechenweg, SQL, `Resource.resx`/`Resource.Designer.cs`,
`Umsetzungskonzept_iOS_EPOS-Plan.md`, `Menuetabelle.cs`, `ModulKatalog*`, `PvModulImport*`,
`KatalogRegistry`, `LizenzLage`, `AppWurzel`, `WaermepumpeStammDialog`.

---

## 11. Offene Punkte

| Kennung | Punkt |
|---|---|
| **H13‑O‑1** | Teil B (`#110b`) liefert die sieben Erzeugerseiten. Ihre Einträge stehen bereits im Startbestand (§ 3.4) mit einer neutralen Beschreibung — wer die Seiten schreibt, darf sie dort an den Wortlaut von „Was berechnet wird" angleichen. |
| **H13‑O‑2** | Die Rubrikseite `Programm Dokumentation` nennt die neue Unterrubrik noch nicht (§ 8, Punkt 4) — Handgriff des Anwenders. |
| **H13‑O‑3** | `_Bezuege.wiki` führt Blöcke für alle 18 allgemeinen Seiten; ob der Anwender sie alle einfügt, entscheidet er beim Einpflegen. |
| **H13‑O‑4** | Die Kurztexte der Rechenweg-Knöpfe stehen als deutscher Rückfall im Quelltext. Sobald der Katalog den Schlüssel kennt, gewinnt sein Tooltip; für eine englische Oberfläche ohne Katalog wäre je Knopf ein Ressourcenschlüssel nachzuziehen. |
