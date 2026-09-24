# Konzept: Der Hilfe-Assistent im Dialog — Aufruf, Kontext, Feldzustand, Steuerung

Stand 11.09.2026 · Zweig `ios_migration_september` · Aufgabe #198 · Ablage `Projekte/` · Status: **entschieden am
11.09.2026** (Anwender: „Stufe 1 mit den Wegen 1 und 2, Weg 4 danach für die vier deklarierten Masken und die
Stromspeicher-Ansicht, Weg 5: Assistent soll steuern"). Umsetzung in drei Stufen nach Abschnitt 6. **Die Fragen KI‑D‑Q1 bis KI‑D‑Q4 sind am 11.09.2026 nach Empfehlung
entschieden** (Anwender: „KI‑D‑Q1 bis Q4: Empfehlung"), siehe Abschnitt 7. **Alle drei Stufen sind umgesetzt**
(#199, #200, #201); der Restpunkt aus #201 — Fortschrittsbalken und Abbrechen bei den Rechenaktionen — ist mit
**#214** (11.09.2026, Anwenderentscheid „Empfehlung starten") geschlossen, siehe Abschnitt 3.4.

Dieses Konzept baut auf [`Konzept_KI-Assistent_Aufgabensteuerung.md`](Konzept_KI-Assistent_Aufgabensteuerung.md)
(Aktionsregister, drei Schutzstufen, Bestätigung, Sicherungspunkt, Protokoll) und auf
[`Konzept_Hilfesystem_Infobutton_EPOS-Plan.md`](../ueberholt/Konzept_Hilfesystem_Infobutton_EPOS-Plan.md) (Info-Knopf,
Hilfeschlüssel, Wiki-Anker) auf. Es beschreibt nur, wie der Assistent **aus einem Dialog heraus** erreicht wird und was
er dort wissen und tun darf. Was er grundsätzlich darf und nicht darf, steht im Aufgabensteuerungskonzept, Kapitel 1.2
und 4, und wird hier nicht wiederholt.

---

## 1. Ist-Stand (gemessen am 11.09.2026, Stand `ef55097`)

| Baustein | Was er heute tut | Befund |
|---|---|---|
| `InfoKnopf` (`EPOS.UI/Bausteine/InfoKnopf.razor`, 88 Einsätze) | löst `Schluessel` über `IHilfeDienst.Aufloesen` auf und öffnet die Wiki-Seite samt Anker | trägt in jedem Dialog den Hilfeschlüssel — die einzige Stelle, die den Dialog fachlich benennt |
| `KiKnopf` (`EPOS.UI/Bausteine/KiKnopf.razor`, seit iU9‑W15b.5) | Knopf mit Beschriftung, Kurztext, `Gewaehlt` | **nirgends eingebaut**; zwei Katalogdialoge tragen nur den Kommentar „gehört links neben den InfoKnopf" |
| `KiChatDialog` (`EPOS.UI/Dialoge/Hilfe/`) | Gesprächsverlauf, Eingabezeile, Werkzeugliste, Bestätigungsblock, Einstellungen; Parameter `Kontext`, `Fragen`, `Suchen`, `Einwilligen`, `Vorschau` | unter Windows nicht-modal mit Besitzer (`KiChatHuelle`), auf iOS die Ansicht `KiAssistent` der `AppWurzel` |
| Kontext | Windows: `HilfeKontext.Beschreibung()` aus der WinForms-Hülle (Positivliste der Bereiche); Kern: `KiChatKontext.AktiverBereich` als Haken | der Haken wird **in keinem Produktcode gesetzt** — auf iOS ist der Bereich immer „Unbekannter Bereich" |
| Wissensbasis | `HilfeWissen.Suchen(frage, kontext)` über Hilfe- und Wiki-Abschnitte, `WikiWissen` mit `SemantikIndex` (OnnxRuntime, nicht auf iOS) | der Bereich ordnet die Treffer; ein Dialogname oder eine Meldungskennung ist noch kein Suchbegriff |
| Aktionsregister | `KiRegister` aus `KiKern`; gefüllt in `EPOS.Kern/Allgemein/KI/Aktionen/KiAktionen.cs` | lag bis #200 in der Windows-Hülle — iOS und die Razor-Dialoge hatten kein Register. **Seit #201 im Kern**, 29 Aktionen; die Hülle behält zwei Haken (Modalität, Feldhilfe) |
| Dialogkatalog | `KiDialogKatalog` mit fünf Masken (`Form_Heizkessel_Bearbeiten`, `Form_PV`, `Form_PufferSp_Bearbeiten`, `Form_WP`, `StromspeicherAuslegung`), Felder als `KiDialogFeld(schluessel, eigenschaftspfad, …)` | war gebunden an **WinForms-Controlnamen** (`tb_th_Leistung`); die Masken sind seit iU9 Razor-Dialoge — der Setz- und Leseweg war tot. **Mit #200 behoben:** der zweite Parameter ist der Eigenschaftspfad (`HeizkesselKatalogDaten.Ptherm`), aufgelöst über die `KiMaskenbruecke`; dazu die fünfte Maske. **Seit #201 trägt die Brücke auch die HAKEN** der Maske (`KiMaskenhaken`: Auffrischen, Prüfen, Schreibschutz, Speichern, Rechenwege) — damit setzt, speichert und rechnet der Assistent über denselben Weg |
| Warn- und Diagnosebanner | `Warnbanner` (95 Einsätze: Stufe, Text, Verfall), `FlottenDiagnosebanner` (P3), Prüfhinweise mit `Kennung` (P1), Laufwarnungen `LAUF_W_*`, Strangampel P1–P8 | jede Meldung hat eine Kennung oder einen Ressourcenschlüssel, aber keinen Weg zum Assistenten |

Kurz: Alle Teile sind da, sie sind nur nicht miteinander verbunden, und zwei davon (Register, Dialogkatalog) hängen
noch an der WinForms-Vergangenheit.

---

## 2. Zielbild in einem Satz

Jeder Dialog bietet neben dem Info-Knopf den Assistenten an; der Assistent weiß, in welchem Dialog er gerufen wurde,
kann die dortige Meldung erklären, auf Wunsch die Feldwerte lesen und, nach Bestätigung, Felder setzen und Aktionen
ausführen — auf Windows und iOS über denselben Kern.

```
┌──────────────────────────────────────────────────────────────────────┐
│ Heizkessel bearbeiten                                     [?] [KI]   │  ← Dialogkopf: Info-Knopf + KI-Knopf
├──────────────────────────────────────────────────────────────────────┤
│ ⚠ Der Wirkungsgrad liegt über 100 %.            [erklären lassen]   │  ← Warnbanner mit Kennung → Weg 2
│ Leistung [kW]  [ 120 ]     Wirkungsgrad [%] [ 104 ]  …               │
└──────────────────────────────────────────────────────────────────────┘
                 │ KI-Knopf                     │ erklären lassen
                 ▼                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│ Assistent · Bereich: Heizkessel · Dialog: Heizkessel bearbeiten      │  ← Kontextzeile
│ Frage: „Was bedeutet: Der Wirkungsgrad liegt über 100 %?"           │  ← vorbelegt aus der Kennung
│ [Feldwerte mitsenden ☐]  (Stufe 2, nur mit Einwilligung)             │
│ Antwort … Quellen: Heizkessel › Rechenweg › Wirkungsgrad             │
│ [Wirkungsgrad auf 92 % setzen] → Bestätigung → Feld im Dialog        │  ← Stufe 3
└──────────────────────────────────────────────────────────────────────┘
```

---

## 3. Die Wege im Einzelnen

### 3.1 Weg 1 — KI-Knopf im Dialogkopf (Stufe 1)

**Wo.** Nicht in 88 Dialogen einzeln, sondern **im `InfoKnopf`**: Der Baustein bekommt den Parameter
`MitAssistent` (Vorgabe `true`) und zeichnet rechts neben sich den `KiKnopf`, sobald der Assistent auf dieser
Plattform grundsätzlich möglich ist (Lizenz erlaubt KI, kein Lesemodus-Verbot für Lesen). Damit bekommen alle
Dialoge und Überlagerungen mit Info-Knopf den Assistenten in einem Schritt; ein Dialog, der ihn nicht will
(Lizenzdialog, Erststart), setzt `MitAssistent="false"`.

**Gestalt seit #218 (11.09.2026, `b9dc087`, Merge `bec51ec`; Anwenderentscheid „KI-Knopf: Variante C + Variante D"):** Info-Knopf
und KI-Knopf sind EINE **Hilfe-Pille** (`.epos-hilfepille`, 28 px hoch, ein Rahmen, Trennlinie) — links das „i" als Inline-SVG, rechts
die nachgezeichnete EPOS-Plan-Marke (drei Felder PV-Blau/Grün/Orange, weiße Mitte, blauer Blitz; Token `--epos-ki-marke-*`) statt der
Beschriftung „KI". Der `KiKnopf` bleibt als Ring mit derselben Marke für Wirte ohne Info-Knopf; beide tragen `Aktiv` (Vorgabe `false`,
noch von keinem Wirt gesetzt — vorgesehen, sobald eine Ansicht `KI_ASSISTENT` offen ist). Die Selektoren `.epos-infoknopf` und
`.epos-kiknopf` bleiben für die Dialogproben bestehen; `KI_KNOPF_HILFE` und `KI_KNOPF_DIALOG` sind ohne Leser entfernt.

**Regel seit #221 (Anwenderentscheid KI‑D‑E‑1 vom 11.09.2026): EINE PILLE JE BILDSCHIRM.**
Der Befund war: „Die KI-Buttons haben keine unterschiedliche Funktion im Kontext. Daher ist es nicht
sinnvoll, auf einer Sicht zwei KI-Buttons zu sehen. Es muss einen Kontext in der KI-Funktion der
zweiten Sicht geben, der sich von dem anderen KI-Button unterscheidet." Gemessen: Das Kopfband des
`Hauptfenster`s zeichnete seine Pille mit dem **festen** Schlüssel `Hauptfenster.btn_Help` über
JEDER Ansicht, und die Ansicht darunter zeichnete eine zweite mit ihrem eigenen — beide führten in
denselben Chat, nur mit verschiedener Bereichszeichenkette. Daraus zwei Sätze, die zusammengehören:

1. **Die Pille des Bildschirms steht dort, wo der Bildschirm anfängt.** Unter Windows ist das das
   Kopfband; die Ansichten (`SimulationSeite`, `StromspeicherAuslegungSeite`, `AssistentSeite`,
   `BerichteKostenSeite` als freie Ansicht, dazu der Kopf von `SimulationKonfigSeite` als Schritt ①)
   zeichnen ihre eigene nur, wenn **kein** Kopfband sie führt — ein `CascadingValue`
   `HilfePilleImKopfband`, den `Hauptfenster` auf `true` setzt und der auf iOS (AppWurzel ohne
   Hauptfenster) fehlt. **Überlagerungsdialoge behalten ihre Pille** (sie verdecken die Seite und
   haben eigene Felder), ebenso die INLINE-Knöpfe im Inhalt („Berechnungsweg…"): Sie tragen einen
   anderen Schlüssel und führen auf eine andere Hilfeseite.
2. **Die Pille oben FOLGT der Ansicht.** Die `AppWurzel` führt einen `AktiverHilfekontext`
   (`EPOS.UI/Dienste/Hilfekontext.cs`: Hilfeschlüssel, Ansicht, Schritt, Reiterblatt); jede Ansicht
   meldet ihn über den `Hilfekontextmelder` nach oben, und `Hauptfenster` bindet Schlüssel, Dialognamen
   und `Aktiv` daran. Ohne meldende Ansicht (Startseite, Projektliste) gilt der bisherige
   Fensterschlüssel. Damit wechselt in der Simulation auch der **Bereich** mit dem Schritt:
   Schritt ① → `Form_Simulation_Config.btn_Help` → `B_SIM_KONFIG`, Schritt ③ →
   `Form_Simulation_Detail.btn_Help` → `B_SIM_DETAIL`.

Wache: `EPOS.UI.Tests/Seiten/HilfePilleTests` — genau eine Pille im Kopfbereich, kein Hilfeschlüssel
zweimal auf demselben Bildschirm, der Schlüssel folgt Ansicht und Schritt, und ohne Kopfband
zeichnet die Ansicht ihre eigene (die iOS-Gegenprobe).

**Was der Knopf weiß.** Denselben `Schluessel` wie der Info-Knopf. Daraus leitet der Kern den Bereich ab:
`KiChatKontext.BereichFuerHilfeschluessel(schluessel)` — eine Tabelle Hilfeschlüssel-Präfix → Bereich der
Positivliste (`Form_Heizkessel*` → `B_HEIZKESSEL`, `KDLG_*`/`Form_Kosten*` → `B_KOSTEN`, …), mit `BEREICH_UNBEKANNT`
als Rückfall. Die Tabelle ist Daten im Kern und wird von einem Test gegen alle 88 Schlüssel gehalten (kein Schlüssel
darf unbekannt bleiben).

**Wie er öffnet.** `Dienste.Navigation.OeffneMaske(Masken.KiAssistent, aufruf)` mit einem neuen Kern-Typ
`KiAufruf­kontext { Bereich, Dialogname, Hilfeschluessel, Frage, Kennung }`. Windows öffnet die nicht-modale
`KiChatHuelle` (wie heute aus dem Menü, aber mit Kontext); iOS wechselt die `AppWurzel` auf die Ansicht `KiAssistent`
und kehrt danach in den Dialog zurück (Muster #62b). Zugleich setzt der Aufruf `KiChatKontext.AktiverBereich` im Kern
— damit ist der Bereich auch auf iOS bekannt, und `HilfeKontext` der Windows-Hülle wird zum zweiten Lieferanten
desselben Hakens statt zur einzigen Quelle.

**Ohne Einrichtung.** Der Knopf bleibt sichtbar; ohne API-Schlüssel oder ohne Einwilligung führt er in den
`KiEinstellungenDialog` mit dem Hinweis, was fehlt (KI‑D‑Q1). Ein unsichtbarer Knopf wäre nicht erklärbar.

**Die Startzeile (Auftrag #227, Anwenderhinweis 11.09.2026).** Der Knopf allein sagt nicht, WOZU er gut ist — der
Anwender hat genau das bemängelt: „Es könnte im Hilfedialog ‚erkläre aktuellen Dialog' oder ähnliches stehen. Sonst
sind die beiden KI Buttons nicht einsichtig." Öffnet sich `KiChatDialog` mit einem Aufrufkontext (der
`Hilfeschluessel`-Parameter der Komponente ist gesetzt — beim Menüweg „Hilfe › Assistent" bleibt er leer, obwohl auch
er einen `Bereich` mitbringt), zeigt der leere Verlauf über dem Eingabefeld eine STARTZEILE: links dieselbe
Kontextzeile, rechts die zwei Knöpfe „Aktuellen Dialog erklären" und „Was kann ich hier tun?". Ein Klick schickt sofort
eine Anwendernachricht ab — „Aktuellen Dialog erklären" die vorbereitete `KI_FRAGE_*` des Kontexts, wenn es eine gibt,
sonst die neue, mit dem Bildschirmnamen formatierte Ressource `KI_CHAT_STARTZEILE_FRAGE_ERKLAEREN`; „Was kann ich hier
tun?" immer die zweite neue Ressource `KI_CHAT_STARTZEILE_FRAGE_MOEGLICH`. Die Startzeile verschwindet, sobald der
Verlauf eine erste Nachricht führt, und bleibt beim Menüweg ganz weg. Ohne Einrichtung (KI‑D‑Q1) tragen beide Knöpfe
dieselbe WEICHE Sperre wie andere gesperrte Bedienelemente des Hauses (`aria-disabled` + `title`, kein `disabled`) —
der bestehende Weg über den Fußleistenknopf „Einstellungen…" bleibt unverändert. Dieselbe Pille bekommt an ihrem
rechten Feld außerdem einen `title`/`aria-label` mit dem Bildschirmnamen, sobald einer bekannt ist („Simulation vom
Hilfe-Assistenten erklären lassen", Ressource `KI_KNOPF_DIALOG_TOOLTIP_MIT_NAME`) — vorher trug sie nur den
allgemeinen Satz. Wache: `EPOS.UI.Tests/Dialoge/Hilfe/KiChatStartzeileTests` und zwei Fälle in
`EPOS.UI.Tests/Bausteine/KiDialogwegTests`.

### 3.2 Weg 2 — „erklären lassen" an Warn- und Diagnosebannern (Stufe 1)

**Wo.** `Warnbanner` bekommt den optionalen Parameter `Kennung`. Ist er gesetzt und der Assistent möglich, zeigt das
Banner rechts den Link „erklären lassen". `FlottenDiagnosebanner` und die Prüfhinweisliste der Stromspeicher-Ansicht
setzen die Kennung aus `FlottenHinweis.Kennung` bzw. `FlottenDiagnose`; die Vorprüfung vor „Berechnen" ebenso. Die
Laufwarnungen der Simulation (`LAUF_W_*`) und die Strangampel (P1–P8) folgen im selben Auftrag, wo ihr Banner den
Baustein benutzt.

**Was gefragt wird.** Der Link ruft denselben Weg wie der KI-Knopf, mit `Frage` = Ressource `KI_FRAGE_<Kennung>`,
falls vorhanden, sonst der allgemeine Satz „Was bedeutet die Meldung „<Bannertext>" und was kann ich tun?" — und
`Kennung` als Suchbegriff. `HilfeWissen` bekommt dafür je Kennung einen Abschnitt (Aufgabensteuerungskonzept 7.1,
„Aktionswissen"): Bedeutung, Ursache, Abhilfe, Verweis auf die Wiki-Seite. Diese Abschnitte sind Daten, keine
Modellantworten — sie kommen auch ohne Modell als Treffer der Stichwortsuche.

**Datenschutz.** Weg 1 und 2 übertragen nur Bereich, Dialogname, Kennung und den Bannertext — keine Projektdaten.
Sie brauchen keine zusätzliche Einwilligung über die heutige hinaus.

### 3.3 Weg 4 — Feldzustand mitgeben (Stufe 2)

**Die Brücke.** Der tote Controlnamen-Weg des `KiDialogKatalog` wird durch eine **Maskenbrücke im Kern** ersetzt:
`KiMaskenbruecke` hält je offenem Dialog eine Feldliste (`KiDialogFeld` mit Schlüssel, Anzeigename, Typ, Einheit,
Getter, Setter, Pflicht/leer erlaubt), die der Razor-Dialog beim Öffnen anmeldet und beim Schließen abmeldet. Die
Deklaration bleibt **eine** (Fachkonzept 11.3: Feldliste für das Modell, Prüfung des Setzwegs, Klartext der
Bestätigung), nur der zweite Parameter ist nicht mehr der Controlname, sondern der Name der Eigenschaft im
Daten-Objekt des Dialogs (`HeizkesselDaten.ThLeistung`). Die vier Deklarationen (`Heizkessel`, `PV`, `PufferSp`,
`WP`) werden so umgestellt; `KiDialogTexte` bleibt.

**Fünfte Deklaration: die Stromspeicher-Ansicht.** `STROMSPEICHER_AUSLEGUNG` meldet die Flottenkonfiguration
(Einheiten mit Kapazität und Leistungen), das Betriebsziel, Peak-Ziel, Netzladung, Start-SoC — und **die
Diagnose** (`Arbeitslos`, Gründe, Prüfhinweise). Die Diagnose macht die Erklärung stark: „Warum ist die Flotte
arbeitslos?" wird mit den echten Zählern beantwortet, nicht mit einer Vermutung.

**Was das Modell sieht.** Im Chat erscheint der Schalter „Feldwerte mitsenden"; erst mit ihm gehen die Werte als
`dialog_lesen`-Ergebnis in die Anfrage, und die `Vorschau` zeigt vorher wörtlich, was gesendet wird
(`KiChatDialog.Vorschau` existiert). Die Einwilligung dafür ist eine eigene Stufe „Dialogdaten" in `KiEinwilligung`
(KI‑D‑Q2): einmal je Installation, jederzeit zurücknehmbar, im Protokoll vermerkt.

### 3.4 Weg 5 — der Assistent steuert (Stufe 3)

**Register in den Kern.** `KiAktionen.cs` zieht von der Windows-Hülle nach `EPOS.Kern/Allgemein/KI/Aktionen/`; die
Hülle behält nur, was die Plattform beisteuert (Dateiwähler, Fensterbesitz, `Task.Run`). Damit steht dasselbe
Register auf iOS — ohne Planer, aber mit allen übrigen Aktionen.

**Felder setzen.** `feld_setzen` schreibt über die Maskenbrücke **in den offenen Dialog**, nicht in die Datenbank:
Der Dialog zeigt den neuen Wert, prüft ihn wie eine Eingabe von Hand (Plausibilität, Pflicht, Einheit), und der
Anwender speichert wie immer. Vorher steht der `KiBestaetigungBlock` mit dem Feldblock (Klartext aus der
Deklaration, alt → neu). Ein Dialog im Lesemodus (iF30) oder ein `ReadOnly`-Katalogsatz lehnt das Setzen mit der
benannten Meldung ab. **Restpunkt aus Bericht #201, erledigt mit Auftrag #211 (11.09.2026):** Der Haken
`Schreibgeschuetzt` war zunächst nur an der Wärmepumpe verdrahtet, weil nur ihr Daten-Objekt
(`WaermepumpeStammDaten.NurLesen`) das `ReadOnly` des Auslieferungskatalogs führte — bei Heizkessel,
Photovoltaik und Pufferspeicher lehnte bis dahin erst der Speicherweg des Controllers beim „Überschreiben"
ab, der Anwender bestätigte also eine Feldsetzung und scheiterte erst beim Speichern. Seit #211 tragen auch
`HeizkesselKatalogDaten`, `ErzeugerZeile` (Photovoltaik) und `PufferSpKatalogDaten` ein `NurLesen` — aus dem
geladenen Katalog- bzw. Gerätesatz befüllt, ohne den Speicherweg selbst anzufassen —, und alle vier Masken
melden `Schreibgeschuetzt` an.

**Aktionen aus dem Dialog.** Zusätzlich zu den Feldern die Aktionen der Stufen 1 bis 3 des Aufgabensteuerungskonzepts,
bezogen auf den offenen Dialog: navigieren (`dialog_oeffnen` über `Dienste.Navigation`), speichern (Stufe 2,
Bestätigung **und** Sicherungspunkt, weil datenbankwirksam), rechnen (Stufe 3: Simulationslauf, „Peak-Ziel bestimmen",
Flotte bewerten — nebenläufig mit `Fortschritt`, abbrechbar). Speichern durch den Assistenten ist nach KI‑D‑Q4
erlaubt, aber nie ohne Bestätigung und Sicherungspunkt.

**Nebenläufig, mit Fortschritt und Abbruch — seit #214 wirklich.** Auftrag #201 hatte den Weg gebaut und die
zwei Enden offengelassen: `KiLaufumgebung` trug Melder und Abbruchmarke bis in die drei Rechenaktionen, aber die
Chat-Hülle reichte `CancellationToken.None` herein und belegte die Senke `KiAusfuehrung.Fortschritt` nicht — ein
Simulationslauf über den Assistenten lief minutenlang ohne Rückmeldung und war nicht abbrechbar. Drei Dinge hat
**#214** nachgezogen: (a) Der **Dialog** (`KiChatDialog`, plattformfrei) hält je laufender Anforderung eine
`CancellationTokenSource`, meldet beide Enden über `KiChatSteuerung` an den Wirt und zeigt den Baustein
`Fortschritt` mit Balken, Schritttext und „Abbrechen"; nach Ende oder Abbruch verschwindet er, und im Verlauf
steht die Sache benannt und mit Dauer. Die Windows-Hülle reicht nur durch, was sie ohnehin hat — iOS erbt es ohne
Hüllenarbeit. (b) Eine Aktion mit `AusfuehrenLang` läuft im **Hintergrund** (`KiAusfuehrung.ImHintergrund`) statt
über `AufOberflaeche`: Auf dem Bedienfaden wären Balken und Abbruchknopf eine Zusage, die niemand einlösen kann,
weil der Faden für die Dauer des Laufs belegt ist. Die 19 kurzen Aktionen bleiben, wo sie waren. (c) Der Abbruch
kommt an: `simulation_rechnen` reicht Melder und Marke in `SimulationRunner`/`SimulationControl.Do_Simulation`
(Prüfung zwischen den fünf Phasen, W11a), `peak_ziel_bestimmen` und `flotte_bewerten` hängen `umgebung.Abbruch`
an denselben Abbruchweg wie der Knopf der Ansicht (`Dienste.Abbrechen`). Ein Abbruch ist eine **benannte
Ablehnung** im Protokoll, kein Fehler.

**Was ausdrücklich nicht geht** (bleibt beim Aufgabensteuerungskonzept 5.4): löschen ohne Rückfrage, Lizenz,
Einstellungen des Assistenten selbst, Projektübergreifendes, alles außerhalb des Registers.

---

## 4. Rahmen, der für alle Wege gilt

- **Verfügbarkeit.** Der Assistent braucht Netz, API-Schlüssel, Einwilligung und läuft unter Tageslimit
  (`AnfragenHeute`/`Tageslimit`). Ohne das bleibt der Info-Knopf der Weg; der KI-Knopf führt zur Einrichtung.
- **Drei Schutzstufen** (Aufgabensteuerung 4.1): Weg 1 und 2 sind Stufe „lesen", Weg 4 „lesen mit Projektdaten",
  Weg 5 „schreiben/rechnen mit Bestätigung". Jede Stufe hat ihre Einwilligung.
- **iOS.** Weg 1, 2 und 4 laufen dort ohne Sonderweg; die semantische Suche fehlt, die Stichwortsuche trägt. Weg 5
  läuft mit dem Register im Kern, ohne die drei planenden Betriebsziele (SP‑O‑3).
- **Eine Wahrheit.** Der Dialogkatalog ist die einzige Feldbeschreibung; Chat-Feldliste, Setzprüfung und
  Bestätigungstext entstehen aus ihr. Kein zweiter Katalog in der Oberfläche.
- **Protokoll.** Jeder Aufruf aus einem Dialog trägt Dialogname und Kennung in die Protokollzeile (`KiProtokoll`);
  Feldsetzungen nennen alt und neu.
- **Abdeckung.** Eine Razor-Maske mit Eingabefeldern ist beim Assistenten angemeldet, gehört als Baustein zu einem Wirt,
  der anmeldet, oder steht mit Grund in `KiDialogAusnahmen` (`EPOS.Kern/Allgemein/KI/Dialoge/KiDialogAusnahmen.cs`,
  Gruppen `KiAusnahmegrund`; `Offen` nur mit Auftrag). Der Wächter
  `EPOS.UI.Tests/Dialoge/Hilfe/KiMaskenabdeckungWacheTests` hält das über alle Komponenten unter `EPOS.UI/Dialoge` und
  `EPOS.UI/Seiten` (Eingabebausteine aus `Standards/` und `Bausteine/`, nackte `input`/`select`/`textarea`, auch über
  den `RenderTreeBuilder`; `Dateiwahl` und Listen zählen nicht), jede Ausnahme gegen ihre Datei und jeden Wirt gegen
  seine Anmeldung. Die Gegenrichtung ist die **Eingabebilanz**: Bei Masken mit Markup-Probe steht jede gebundene
  Eingabe im Katalog oder mit Grund in `BewusstDraussen`, bei allen übrigen angemeldeten und gehosteten Dateien ist die
  Zahl der Eingabestellen festgeschrieben — jede neue Eingabe erzwingt einen Entscheid. Wird der Assistent aus einer
  ausgenommenen Maske gerufen und `feld_setzen` trifft keine angemeldete Maske, nennt die Absage den Grund („Diese
  Maske ist bewusst nicht steuerbar: …", bei `Offen` „noch nicht steuerbar"); erkannt wird die Maske am Hilfeschlüssel
  des Aufrufs (`KiChatKontext.Aufruf`), den der Eintrag führt.
- **Stand der Abdeckung.** Angemeldet sind alle Masken mit Einstellwerten; die Ausnahmeliste führt keinen
  `Offen`-Eintrag, und kein Vermerk der Eingabebilanz wartet mehr auf einen Auftrag: Die drei Zapfprofil-Überlagerungen
  und die Rechenweg-Wahl der Bedarfsprofile stehen im Katalog (Stufe 3a), die Zahlenfolgen (Monatswerte eines
  Bedarfstyps, eines Kostenprofils, eines Quellprofils und einer Leistungspreisreihe, die Stundenwerte des Gebäudetyps,
  die Wochenwerte eines Typ- und eines Kostenprofils) sind Zahlenreihen (Stufe 3b, nächster Punkt), und die
  Gebäudeverwaltung ist eine eigene, steuerbare Maske (`Form_Gebaeude_Admin`, #465: Satzwahl und jedes Feld ihres
  Stammblatts, dieselbe Feldliste und Sichtklasse wie der Gebäudekatalog; `Form_Gebaeude` gehört allein dem
  Projektdialog). Draußen bleibt
  allein, was Eingabebilanz und `BewusstDraussen` mit Grund benennen (Anzeigeschalter, Mengen von Verweisen, Felder
  einer Aktion, Zeitreihen des Dateiwegs). **Überlagerungen mit eigenem Arbeitsstand** (Kennlinieneditor,
  „Anlagenwerte", Zapfprofil samt Auslegung und Bedarfstag-Konstruktor) melden sich als eigene Maske an, solange sie
  offen stehen — die zuletzt angemeldete ist die aktive —, und bieten keinen Speicherweg: Übernommen wird mit ihrem
  OK, geschrieben mit dem OK des Wirts; Prüfen ist, wo die Maske eine Prüfung am OK trägt, deren Befund. Gesetzt wird auf den Wegen
  der Eingabefelder (Neuberechnung, überholter Punkt); was die Maske gerade verdeckt oder sperrt (Stufe, Schalter,
  Zeilenart, gesperrter Katalogeintrag) und Zahlen außerhalb der Grenzen des Feldes lehnt die Maske benannt ab.
  **Führt eine Maske Felder als Daten eines Profils** (die Verwaltungen, „Alle Daten" der
  Erzeugermasken, die Diagrammfarben der Einstellungen), steht die Feldkarte im Kern aus demselben Profil erzeugt, die
  Sichtklasse beantwortet sie als `IKiFeldtafel`, und ein Profilwächter ersetzt Markup- und Reflection-Probe. **Die
  Anzeigeschalter eines Ergebnisblattes** (sortiert, Reihen ein/aus) sind eine Spalte seines Wirts: Jedes Blatt meldet
  beim Aufbau seine gezeichneten Schalter beim Register der Seite an, nie eine eigene Maske — eine im Blatt
  angemeldete Maske verdrängte die Maske der Ansicht als aktive.
- **Zahlenfolgen: Tabelle oder Zahlenreihe** (#458 Stufe 3b). Der Rahmen kennt zwei Formen, und die Wahl folgt der
  Maske. Eine **Tabelle mit benannten Zeilen** bleibt die Spaltenform (`Typ.Zeilen[].Eigenschaft` mit
  Zeilenkennzeichen): Jede Zeile wird ein eigenes Feld mit eigener Bestätigungszeile, gesetzt mit `feld_setzen` oder
  in einem Block mit `formular_ausfuellen` — so die Kostenpositionen, die Stützstellen einer Kennlinie und die vier
  Ferienzeiträume des Gebäudekatalogs (Beginn und Ende je Tag und Monat, Zeilenkennzeichen „Zeitraum"). Eine
  **gleichartige Folge von Zahlen** trägt die Spaltenform nicht: Eine Wochenreihe wären 168 Felder, 168 Zuweisungen in
  `formular_ausfuellen` (weit über dessen 2 000 Zeichen), 168 Zeilen in `dialog_lesen` und in der Bestätigung, und die
  Stellen (Monat, Stunde, Wochenstunde) sind keine Zeilen eines Datenobjekts, sondern Plätze einer Liste. Sie ist
  deshalb **ein Feld vom Typ Zahlenreihe** (`KiParameterTyp.ZahlListe` mit der Form `KiZahlenreihe`: feste Länge,
  benannte Stellen „Januar" bis „Dezember", „Stunde 1" bis „Stunde 24", „Montag, Stunde 1" bis „Sonntag, Stunde 24");
  gelesen wird sie als eine Liste (Strichpunkt getrennt, ein leerer Wert benannt), gesetzt mit der Aktion
  **`reihe_setzen`** — die ganze Reihe als Zahlenliste oder ein Ausschnitt ab einer Stelle (`ab`, bei 1 beginnend;
  Dienstag einer Wochenreihe = Stelle 25). Die Länge prüft der Kern vor dem Setzen, die Grenzen sind die des
  Eingabefeldes der Maske (`KiDialogFeld.Min`/`Max`, je Wert, mit dem Namen der Stelle in der Absage), alles Weitere
  prüft der Dialog danach (Haken `Pruefen`). Die Bestätigung zeigt die geänderten Stellen „alt → neu" gekürzt (die
  ersten zwölf und die Gesamtzahl), die Protokollzeile trägt die volle Liste. `feld_setzen` und `formular_ausfuellen`
  lehnen eine Zahlenreihe benannt ab und nennen den Weg. **Die Reihen kommen aus den Datenobjekten der Masken**, eine
  zweite Feldliste gibt es nicht: `TypStammDaten.Monat` unmittelbar, sonst eine Eigenschaft der Sichtklasse
  (`double?[]`) über den lebenden Stand. Zahlenreihen führen: Bedarfskopfsatz (`monatswerte`), Wochen-Stundenprofil
  (`wochenwerte`, die übernommenen 168 Werte), Gebäudetyp (`stundenwerte` der gewählten Kurve im Arbeitsstand; die Maske
  hält den Kurvenwechsel bis zum Speichern an), Kostenprofil (`monatswerte`, `wochenwerte`), Leistungspreisreihe
  (`monatssaetze`, 0 bis 100 000) und Quellprofil (`monatswerte`, nur in der Betriebsart „Monat"; die 365 bzw. 8 760
  Werte von Tag und Stunde bleiben Zeitreihen des Dateiwegs). Das **Hüll-Raster** des Gebäudekatalogs braucht keine
  eigene Form: Kennwert und Größe jeder Zeile sind die Felder `u_*`, `flaeche_*`, `wbvk_*` und `anschluss_*`; neu ist die
  Randbedingung der Bodenplatte als Wahlfeld. Die zwölf Monatswerte der Bedarfsverwaltungen (#456) bleiben zwölf
  Einzelfelder — sie tragen je eigenen Namen und sind mit `formular_ausfuellen` in einem Block gesetzt.

---

## 5. Was bewusst nicht Teil dieses Konzepts ist

- Kein neues Sprachmodell, keine Änderung an `KiChatService`, Wissensbasis-Formaten oder dem Semantikindex.
- Kein Assistent ohne Anwenderfrage (keine „proaktiven" Vorschläge beim Öffnen eines Dialogs).
- Keine Deklaration aller Dialoge auf einen Schlag — die Freigabe folgt den Stufen S4 und S5 (Abschnitt 6); was nicht
  steuerbar ist, steht mit Grund auf der Ausnahmeliste (Abschnitt 4, „Abdeckung").
- Kein Planer auf iOS (SP‑O‑3).

---

## 6. Stufenplan und Aufträge

| Stufe | Auftrag | Inhalt | Prüfmuster |
|---|---|---|---|
| **S1** (Wege 1 + 2) — **umgesetzt #199** (11.09.2026) |  **#199** | `InfoKnopf.MitAssistent` mit `KiKnopf`; `KiAufrufkontext`, `Masken.KiAssistent` in `Dienste.Navigation`, Windows-Hülle und `AppWurzel` öffnen mit Kontext; `KiChatKontext.BereichFuerHilfeschluessel` (Tabelle, Test über alle Schlüssel), `AktiverBereich` aus der Oberfläche; `Warnbanner.Kennung` + Link, Kennungen an Diagnosebanner, Prüfhinweisen, Vorprüfung, Laufwarnungen, Strangampel; `HilfeWissen`-Abschnitte je Kennung; `KI_FRAGE_*` de/en; Kontextzeile im Chat zeigt Dialog und Kennung | bunit: Knopf in einem Dialog mit und ohne Assistent, Öffnen mit Kontext, Bannerlink mit Kennung; Kern: Bereichstabelle vollständig, Aufruf setzt den Haken; Referenzlauf unberührt |
| **S2** (Weg 4) — **umgesetzt #200** (11.09.2026) | **#200** | `KiMaskenbruecke` im Kern; `KiDialogKatalog` auf Eigenschaftsnamen; die vier Dialoge und die Stromspeicher-Ansicht melden ihre Felder an; Einwilligungsstufe „Dialogdaten"; Schalter „Feldwerte mitsenden" + Vorschau im Chat; `dialog_lesen` aus der Brücke | Kern: Brücke liest die fünf Masken; bunit: Schalter, Vorschau zeigt Werte, ohne Einwilligung nichts; Protokoll |
| **S3** (Weg 5) — **umgesetzt #201** (11.09.2026, `d1bfb56`, Merge `9122812`; Wiki `Hilfe-Assistent` Rev. 542) | **#201** | `KiAktionen` in den Kern; `feld_setzen` über die Brücke mit Bestätigungsblock, Plausibilität des Dialogs, Lesemodus/ReadOnly; `dialog_oeffnen`; speichern mit Sicherungspunkt; rechnen mit `Fortschritt`; Protokoll alt → neu | Kern: Setzen, Ablehnung im Lesemodus, Sicherungspunkt; bunit: Bestätigung → Feld im Dialog; Referenzlauf unberührt |
| **S3‑Rest** (Fortschritt und Abbruch) — **umgesetzt #214** (11.09.2026) | **#214** | `KiChatDialog` hält je Anforderung eine `CancellationTokenSource` und meldet Senke und Marke über `KiChatSteuerung` an; Baustein `Fortschritt` im Chat (Balken, Schritttext, „Abbrechen"), Schlusszeile im Verlauf mit Namen und Dauer; Senden und Aktionsknöpfe während eines Laufs gesperrt; lange Aktionen laufen im Hintergrund (`KiAusfuehrung.ImHintergrund`) statt über `AufOberflaeche`; Abbruch kommt in allen drei Rechenaktionen an (`SimulationRunner` mit `IProgress`/`CancellationToken`, `Dienste.Abbrechen` der Stromspeicher-Ansicht) | bunit: Balken während einer langen Aktion, „Abbrechen" setzt die Marke, Verlaufszeile „abgebrochen"; Kern: eine lange Aktion nutzt `AufOberflaeche` NICHT (Gegenprobe: eine kurze zweimal); KiKern: `KiLaufumgebung`/`KiFortschritt`; Referenzlauf 1030/1046 byte-gleich |
| **S4** (Freigabe aller Masken mit Einstellwerten, KI‑D‑Q5) — **KI‑F1 umgesetzt #416, KI‑F2 umgesetzt #419, KI‑F1b umgesetzt #420, KI‑F3 umgesetzt #421, KI‑F4 umgesetzt #423, KI‑F5 umgesetzt #424, KI‑F6 umgesetzt #425** (20./21.09.2026; Katalog 63 Masken, 724 Felder) — die Stufe ist abgeschlossen | **#416 ff.** | Je Maske ein Katalogeintrag mit allen sichtbaren Feldern, ein Navigationsziel und die Anmeldung an der Maskenbrücke; F1 Erzeuger im Projekt (Heizkessel, BHKW, Pufferspeicher, Stromspeicher, Solarkollektoren, Wärmepumpe Anlage, Photovoltaik mit Modellfeldern und Strängen), F2 Simulationskonfiguration, F3 Bedarf und Klima, F4 Kosten und Wirtschaftlichkeit, F5 Erzeugerkataloge, F6 Strom, Berichte, Projekt; ausgenommen die Masken des Assistenten, Lizenz, Administration, Auswahl‑, Übernahme‑ und Importmasken, Startseite und Assistentenschritte | `KiDialogkatalogTests` (jeder Feldpfad am Daten-Objekt und im Markup, Zählung), `KiRegisterS3Tests` (jede Katalogmaske hat ein Ziel), je Maske ein bunit-Fall (angemeldet, Feld lesen und setzen) |
| **S5** (Verwaltungen, KI‑D‑Q11) — **Erzeugerverwaltungen und Bedarfsverwaltungen umgesetzt #456** (23.09.2026; Katalog 68 Masken); **Folgewelle: übrige Masken nach Inventar** | **#456** | Die vier Verwaltungen der Erzeugerkataloge (`KatalogBrowserDialog`: Heizkessel, BHKW, Solarkollektoren, Pufferspeicher) melden sich unter ihren Navigationsschlüsseln an (`Form_Heizkessel_Admin`, `Form_BHKWAdmin`, `Form_SolarKollektorenAdmin`, `Form_PufferSp_Admin`); ihre Feldkarte ERZEUGT der Kern aus dem `KatalogBrowserProfil` (Feldname = Profilschlüssel klein, Anzeigename und Einheit aus dem Profil, nur lesbar = nicht editierbar) samt Wahlfeld `satz`, die Sichtklasse `KatalogBrowserKiSicht` beantwortet sie als **Feldtafel** (`IKiFeldtafel`, zweite Anmeldeart der `KiMaskenanmeldung`: Schlüssel statt Eigenschaft). Haken: Auffrischen, Schreibschutz = Lesemodus oder Auslieferungssatz mit eigenem Schutzgrund (`KiMaskenhaken.Schreibschutzgrund`, Weg „Duplizieren…"), Prüfen = Eingabenprüfung, Speichern = Weg des Knopfes. Der Satz ist **Satzwahl** (`KiDialogFeld.Satzwahl`): Der Schutz eines Auslieferungssatzes gilt für ihn nicht; ungespeicherte Änderungen lehnen den Wechsel benannt ab. Die Bedarfsverwaltungen führen Typ (Wahl), Beschreibung und die zwölf Monatswerte als Eingaben, mit Schutz, Prüfen und Speichern. Die Absage ohne offene Maske nennt bei Editor und Verwaltung (dasselbe Öffnungsziel) die Verwaltung; die Anleitung spricht von der Satzwahl. Öffnungsziele der vier Verwaltungen in `KiMaskenziele` | `KiDialogkatalogTests` (68 Masken, Profilwächter je Ausprägung, Bedarfsfelder, Ziele), `KiMaskenwegTests` (Editor und Verwaltung ein Weg, Projektmasken bleiben mehrdeutig, Schutzgrund und Satzwahl), `KatalogBrowserDialogTests` (je Ausprägung An-/Abmelden, Wert lesen und setzen, Speicherhaken, Auslieferungssatz, Lesemodus, Satzwechsel), `BedarfAdminDialogTests`, `KiFeldSetzenTests` (Administration Heizkessel: Vorlauf); Referenzlauf unberührt |
| **S5, #458 Stufe 1** (Abdeckung) — **umgesetzt #458** (23.09.2026; Katalog 68 Masken) | **#458** | Wächter `KiMaskenabdeckungWacheTests` (Abschnitt 4, „Abdeckung"): 151 Komponenten, 62 Anmeldungen, 18 Wirte, Ausnahmeliste `KiDialogAusnahmen` vervollständigt (40 Einträge: 13 Anzeige, 5 Import, 1 Export, 4 Aktion, 1 Rückfrage, 1 Werkzeug, 2 Lizenz/Schlüssel, 3 Assistent, 1 Anlegen, 1 Feld des Wirts, 8 Offen mit Auftrag „#458 Stufe 2" bzw. „#458 Stufe 3 nach Z3"), Eingabebilanz (Markup-Masken gegen den Katalog, 66 Dateien mit festgeschriebener Zahl der Eingabestellen); benannte Absage aus einer ausgenommenen Maske. Nachzüge: `SIMULATION` 46 Felder (Kühlbetrieb; je gewählter Karte Wärmequelle, konstante Quelltemperatur, WP-Priorität, Betriebsmodus über die Wege der Überlagerungen — der `BetriebsmodusDialog` bekommt deshalb keine eigene Maske, Grund `FeldDesWirts`), `WAERMEPUMPE_ANLAGE` über `WaermepumpeAnlageKiSicht` mit dem Extrapolationsschalter (23 Felder), `GEBAEUDETYP` Beschreibung setzbar, `ENERGIETRAEGER` Leistungspreismodus über den Weg der Optionsgruppe, Peak-Shaving-Quelle benannt abgelehnt ohne eingelesene Datei; widersprüchliche Kommentare berichtigt | `KiMaskenabdeckungWacheTests` (Abdeckung, Ausnahmen, Wirte, Schranken, Gegenprobe des Zählers, Eingabebilanz, Hilfeschlüssel), `KiDialogAusnahmenTests`, `KiMaskenwegTests` (Absage aus der Ausnahme samt Gegenproben), `SimulationKonfigKiTests`, `KiSimulationMaskeTests`, Dialogtests Gebäudetyp, Wärmepumpe (Anlage), Energieträger, Peak-Shaving; Referenzlauf unberührt |
| **S5, #458 Stufe 2** (übrige Masken mit Einstellwerten) — **umgesetzt #458 Stufe 2** (24.09.2026; Katalog 72 Masken) | **#458** | Neu im Katalog: **Kenndaten** (`KennlinienEditorDialog`, Überlagerung der Wärmepumpen-Verwaltung und -Anlage; `KennlinienKiSicht`: Vorlaufstufe als Wahlfeld/Satzwahl, Temperatur, COP und Ptherm der gewählten Stufe als Spalten mit Kennzeichen „Vorlauf/Temperatur", neue Vorlauftemperatur und neue Stützstelle; Schreibschutz = Auslieferungssatz, kein Speicherweg — OK bleibt beim Anwender), **Wizard_Projekt** (`ProjektKopfSeite`, `ProjektKopfKiSicht`: Name, Klimaregion als Wahlfeld mit Id und Name, Kunde, Bearbeiter, Beschreibung; Prüfen = Kopfregel, Name im Bearbeiten-Modus benannt fest, kein Speicherweg), **Form_Start** (`Startseite` als Wirt von `ErzeugerReiter`, `StartseiteKiSicht`: Klimaregion und Solarart; Speichern = Knopf neben der Klimaregion, ohne offenes Projekt schreibgeschützt; die Projekt-/Variantenwahl bleibt als Navigation draußen; die Solarweiche ist die `Optionsgruppe`), **Form_AdminSettings** (`EinstellungenDialog`, `EinstellungenKiSicht`: fünf Adressen, Kühlungsvorgabe neuer Projekte, die Diagrammfarben als Feldtafel je Farbrolle aus `Diagrammfarben.Gruppen`; Ordner, Datenbankname und KI-Abschalter bleiben draußen; Prüfen = Befund des Dialogs, Speichern = Weg von OK ohne Schließen). **„Alle Daten"** der sechs Erzeugermasken des Projekts als Feldtafel (`AlleDatenTafel`; Feldkarte aus `KatalogBrowserProfil` bzw. `ModulKatalogProfil`, Pfad `…KiSicht.Katalog_<SCHLÜSSEL>`; Sichtklassen `ErzeugerProjektKiSicht`, `SolarkollektorenKiSicht`, `PhotovoltaikKiSicht`; Schreibweg = Knopf des Aufklappers; Absagen: zugeklappt, ohne Speicherweg, Auslieferungssatz mit Weg „Duplizieren…"). **Anzeigeschalter** der neun Ergebnisblätter als Spalte `anzeige` der Maske `Simulation` (Register `Ergebnisanzeige`, Grundklasse `Ergebnisblattwirt`), das Blatt als Wahlfeld `reiter`; Zeitraum und Haken des Kapitalwertverlaufs an der Wirtschaftlichkeitsseite. Wächter: 151 Komponenten, 66 Anmeldungen, 29 Wirte, 25 Ausnahmen (davon 3 `Offen`), 81 Dateien mit festgeschriebener Zahl der Eingabestellen; Markup-Probe und Eingabebilanz kennen die Feldtafel (`IstTafelfeld`, `FuehrtFeldtafel`) | `KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests` (Profilwächter „Alle Daten", Farbrollen), Dialog- und Seitentests Kennlinieneditor, Projektkopf, Startseite, Einstellungen, Heizkessel, Puffer- und Stromspeicher, Ergebnisreiter, Kapitalwertverlauf, `KiSimulationMaskeTests`; Referenzlauf unberührt |
| **S5, #458 Stufe 3a** (Zapfprofil-Masken) — **umgesetzt #458 Stufe 3a** (24.09.2026; Katalog 75 Masken) | **#458** | Neu im Katalog die drei Überlagerungen des Brauchwasser-Zapfprofils, je über eine Sichtklasse auf ihren Arbeitsstand: **Form_Zapfprofil** (`ZapfprofilDialog`, `ZapfprofilKiSicht`, 11 Felder: Stufe, Zonenwahl als Satzwahl, die Zonen als Spalten Zonenname/Nutzungsart/Bezugsgröße/Niveau/Jahresbedarf mit dem Zonennamen als Kennzeichen, „Anzeigen für", Rechenweg der Jahresreihe, Seed, Realisierungen), **ZapfprofilAuslegung** (`ZapfprofilAuslegungDialog`, `ZapfprofilAuslegungKiSicht`, 12 Felder: Bedarfstag als EINE Wahl aus Quelle und Katalogtag, Speichertemperatur, Erzeuger- und Übertragerleistung, Speicherart, Sensorhöhe, Erzeugerart, Werkstoff, „Stochastisch rechnen", Perzentil, Realisierungen, der empfohlene Punkt nur lesbar) und **BedarfstagKonstruktor** (`BedarfstagKonstruktorKiSicht`, 8 Felder: Name und die Zeilen als Spalten Beginn/Ende/Zapfregel/Anzahl/Volumen/Zapftemperatur/Verbraucher). Jede meldet sich an, solange sie offen steht; Haken Auffrischen und Prüfen (Befund des OK), kein Speicherweg. Gesetzt wird auf den Wegen der Eingabefelder (Vorschau und Karten rechnen entprellt neu, ein übernommener Punkt wird überholt); Verdecktes (Stufe Experte, Rechenweg „stochastisch", Schalter „Stochastisch rechnen", Zeilenart) und Gesperrtes (Stufe „Erweitert", gesperrte Nutzungsart, gesperrter Bedarfstag) lehnen die Masken mit Grund ab, Zahlen in den Grenzen der Felder. Ziel Startseite, Reiter „Wärmebedarf". In den **Bedarfsprofilen** ist die Optionsgruppe „Rechenweg Brauchwasser" das Wahlfeld `rechenweg` (Weg des Klicks; „Zapfprofil" ohne Zone und die Weiche außerhalb des Brauchwassers benannt abgelehnt); die sechs Anzeigen des Infoblocks bleiben nur lesbar. Rechenwege als Haken gibt es nicht — keine Aktion ruft sie. Wächter: 152 Komponenten, 69 Anmeldungen, 29 Wirte, 22 Ausnahmen (keine `Offen`), 84 Dateien mit festgeschriebener Zahl der Eingabestellen | `KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests` (75 Masken), `KiFeldwerteTests`, `KiMaskenwegTests` (offene Absage ohne Listeneintrag über `AbsageFuer`), Dialogtests Zapfprofil, Auslegung samt Konstruktor und Bedarfsprofile (An- und Abmelden, aktive Maske, Lesen und Setzen, Spalten, Ablehnungen, Prüfen, kein Speicherweg); Referenzlauf unberührt |
| **S5, #458 Stufe 3b** (Zahlenfolgen) — **umgesetzt #458 Stufe 3b** (24.09.2026; Katalog 75 Masken, sieben Zahlenreihen) | **#458** | Rahmenentscheid: Tabellen mit benannten Zeilen bleiben Spalten, gleichartige Zahlenfolgen werden **Zahlenreihen** (Abschnitt 4, „Zahlenfolgen: Tabelle oder Zahlenreihe"). Kern des Assistenten: Parameter- und Feldtyp `ZahlListe` (Schema `array` aus `number`, Prüfung je Glied, `KiAufruf.ZahlListe`), Form `KiZahlenreihe` (Länge, Stellennamen, Liste und Kurzfassung), `KiDialogFeld.Reihe`, `Min`, `Max`, Reihenblock `KiFeldBlock.Reihe` (geänderte Stellen alt → neu, gekürzt); neue Formularaktion `reihe_setzen` (`maske`, `feld`, `werte`, `ab`) über `KiFeldwandler.WandleReihe` (Länge, Ausschnitt, Grenzen je Wert); die Grenzen der Eingabefelder gelten auch für Einzel- und Spaltenfelder; `feld_setzen` und `formular_ausfuellen` lehnen eine Reihe mit Weg ab; `dialog_lesen` zeigt die Reihe als Liste samt Umfang, `dialog_parameter_erklaeren` Umfang und Bereich (seine Vorbedingung lehnte bis dahin auch jedes vorhandene Feld ab — berichtigt); Zeile im Systemtext des Chats. Masken: Bedarfskopfsatz `monatswerte`, Wochen-Stundenprofil `wochenwerte` (168, der übernommene Stand), Gebäudetyp `stundenwerte` (24, gewählte Kurve im Arbeitsstand), Kostenprofil `monatswerte` und `wochenwerte`, Leistungspreisreihe `monatssaetze` (0 bis 100 000), Quellprofil `monatswerte` (nur Betriebsart Monat); Gebäudekatalog: Ferien als vier Spalten mit Zeilenkennzeichen „Zeitraum" (Tag 1–31, Monat 1–12) und die Randbedingung der Bodenplatte als Wahlfeld (59 Felder). Wächter: die fünf Vermerke „#458 Stufe 3 (Zahlenfolgen)" der Eingabebilanz und der `BewusstDraussen`-Eintrag des Bedarfskopfsatzes aufgelöst | `KiZahlenreiheTests` (KiKern), `KiReiheSetzenTests` (Kern: ganz, ab Stelle, je Stelle, Länge, Überlauf, Grenzen, Schutz, Lesemodus, falscher Weg, Bestätigung gekürzt, Befund, Lesen, Erklären, Protokoll, Werkzeugliste), `KiRegisterS3Tests`, `KiWerkzeugkatalogTests`, `KiDialogkatalogTests` (Zählliste der Reihen, Gebäudekatalog), `KiFeldwerteTests`, Dialogtests der sieben Masken (lesen, ganz und je Stelle setzen, Länge/Bereich abgelehnt, Speicher- bzw. OK-Weg); Referenzlauf unberührt |
| **S5, #465** (Gebäudeverwaltung) — **umgesetzt #465** (24.09.2026; Katalog 76 Masken) | **#465** | Die Gebäudeverwaltung (`GebaeudeAdminDialog`) ist eine eigene Maske **Form_Gebaeude_Admin** — ihr Navigationsschlüssel (`Masken.GebaeudeAdmin`, bis dahin `Form_Gebaeude` wie die Projektmaske). Feldkarte: Wahlfeld `satz` (Satzwahl) und die 58 Felder des Gebäudekatalogs aus derselben Methode `KiDialoge.GebaeudeKatalogFelder` (Name nur lesbar, ohne `betriebsart`), 59 Felder, an derselben Sichtklasse `GebaeudeKatalogKiSicht`, die der Arbeitsstand selbst baut (`GebaeudeArbeitsstand.KiSicht`) — Editor und Stammblatt teilen Arbeitsstand, Prüfung und Schreibweg. Haken: Auffrischen, Schreibschutz (Auslieferungssatz; Grund nennt „duplizieren oder das Schloss aufheben"), Prüfen = Prüfung des Speicherknopfs, Speichern = Weg des Knopfes. Ziele: `Form_Gebaeude_Admin` und `Form_Gebaeude1` → Verwaltung, `Form_Gebaeude` → Startseite. Nicht über den Assistenten: Neu…, Duplizieren…, Schloss, Löschen | `KiDialogkatalogTests` (76 Masken, Feldliste gleich der des Katalogs, Ziele), `KiMaskenabdeckungWacheTests` (Wirt `GebaeudeStammblattFelder` → `GebaeudeAdminDialog`, Eingabestellen 7 und 36), `KiFeldwerteTests` (Abmelden), `GebaeudeAdminDialogTests` (An-/Abmelden, Wert setzen → Speichern frei, Prüfung und Speicherhaken, Auslieferungssatz abgelehnt, `satz` wechselt); Referenzlauf 1030/1045 unberührt |
| **Bedienung** (Befunde der Abnahme) — **umgesetzt #219** (11.09.2026) | **#219** | KI‑D‑B‑1 (Tastaturfokus des nicht-modalen Chatfensters): `Masken.KiAssistent` öffnet über `Blazorsprung.Verzoegert`, `BlazorDialogForm.TastaturUebergeben()` gibt der zweiten WebView2 die Eingabe (sofort und nach ihrer Initialisierung), das Textfeld trägt `autofocus` und bekommt den Schreibzeiger nach dem ersten Zeichnen sowie bei jedem neuen Aufrufkontext. KI‑D‑B‑2 (tote Verweise): eine ADRESSE geht über `Dienste.Datei.AdresseOeffnen` statt über `MitSystemOeffnen` — in `KiChatHuelle.Gaben` und im Rückfall von `WindowsHilfeDienst` | bunit: jedes Bedienelement des Chats einzeln (`KiChatBedienungTests`, 19 Fälle); Quelltextwachen `KiChatOeffnerTests` (Sprung, Fokusübergabe, Adressweg, kein leerer Delegat, kein Bedienelement ohne Weg — je mit Gegenprobe); Referenzlauf unberührt. **Am Gerät bleibt** der Fokusweg WinForms → WebView2 → DOM auf beiden Öffnungswegen |

| **Kontext** (KI‑D‑E‑1) — **umgesetzt #221** (11.09.2026) | **#221** | **Eine Pille je Bildschirm** und ein Kontext mit Substanz: `Hilfekontext`/`Hilfekontextmelder` in `EPOS.UI/Dienste`, `AppWurzel.AktiverHilfekontext` samt `HilfekontextGeaendert`, `CascadingValue HilfePilleImKopfband` aus `Hauptfenster`; die vier freien Ansichten und der Kopf von Schritt ① lassen ihre Pille unter dem Kopfband weg und melden statt dessen Ansicht · Schritt · Reiter (Format `KI_KONTEXT_STELLE`). Die **Simulationsansicht meldet sich an der Maskenbrücke an** — sechste Katalogmaske `KiMaskennamen.SIMULATION` mit **18 Feldern** aus `SimulationKiSicht` (Kaskade und nicht aufgenommene Anlagen lesend, die fünf Laufparameter lesbar UND setzbar über `SimulationParameterDienste`, die sieben Kennzahlen des Laufs samt SoC-Band, Reiterblatt und Laufhinweisen nur lesend), Ziel in `KiMaskenziele`. Dazu `KiChatKontext.AufrufGeaendert`/`AssistentStehtFuer` für das leuchtende Feld der Pille (#218) und **Startfragen je Bereich** (`KI_FRAGE_SIMULATION_KONFIG`, `KI_FRAGE_SIMULATION_ERGEBNIS`, de/en) statt der leeren Eingabezeile | bunit: `HilfePilleTests` (eine Pille, Schlüssel folgt Ansicht und Schritt, ohne Kopfband die eigene, Stelle als Dialogname, leuchtendes Feld, Startfragen samt zwei Gegenproben); `KiSimulationMaskeTests` (18 Felder, Felder je Schritt, `feld_setzen` über den Delegaten, Kennzahlen nicht setzbar, genau fünf setzbare Felder); Kern: Startfragen in beiden Sprachen, Aufrufwechsel wird gemeldet; Referenzlauf 1030/1046 byte-gleich |

| **Öffnen** (KI‑D‑B‑3) — **umgesetzt #228** (11.09.2026) | **#228** | Der Assistent geht wieder aus dem **Hauptmenü** und über **F1** auf. Zwei Ursachen: (1) der Riegel von `Blazorsprung` fiel erst am ENDE des Sprungs, und seit #219 verzögert der Menüweg ZWEIMAL (`HauptfensterHuelle.Weg` → `MaskeOeffnen` → `WinFormsNavigation`) — der innere Ruf wurde stumm verworfen; (2) `KeyPreview`/`KeyDown` sieht keine Taste, die in der WebView2 anfällt. Behoben: Riegel fällt vor dem Sprung, `RiegelSteht` protokolliert und lässt einen verwaisten Riegel verfallen, `Blazorsprung.Wirtsfenster` fällt auf das Hauptfenster zurück (Schlange UND Besitzer), `Hauptfensterrahmen.ProcessCmdKey` statt `KeyPreview`; dazu trägt `KiChatHuelle._offene` nur noch ein fertig gebautes Fenster | Quelltextwachen `KiChatOeffnerTests` (Riegel vor dem Sprung samt Gegenprobe, Protokoll und Verfall, Wirtsfenster-Rückfall, F1 über `ProcessCmdKey`, Lebenszyklus der Hülle); Kern und Oberfläche unverändert, Referenzlauf unberührt. **Am Gerät bleibt** beides: Menü, F1, Pille — je nach vorherigem Öffnen und Schließen |

Reihenfolge S1 → S2 → S3; S2 und S3 können getrennt abgenommen werden. Jeder Auftrag: Doku in
`Konzept_KI-Assistent_Aufgabensteuerung.md` (Kapitel 8, Etappen) und `EPOS.UI/CLAUDE.md`; Wiki-Seite
„Hilfe-Assistent" nach S1 und S3 nachziehen (Upload durch die Orchestrierung).

---

## 7. Fragen mit Empfehlung

| Frage | Empfehlung | Stand |
|---|---|---|
| **KI‑D‑Q1** Ist der KI-Knopf auch ohne Einrichtung sichtbar? | Ja; er führt in die Einstellungen mit Hinweis, was fehlt. Ein fehlender Knopf ist nicht erklärbar. | **entschieden 11.09.2026 (Empfehlung), umgesetzt #199**: `KiVerfuegbarkeit.Moeglich` fragt allein den Abschalter der Installation — Netz, Schlüssel und Einwilligung sind ausdrücklich keine Bedingung |
| **KI‑D‑Q2** Wie wird das Mitsenden von Feldwerten eingewilligt? | Eigene Stufe „Dialogdaten" einmal je Installation, zurücknehmbar, dazu je Anfrage der Schalter und die Vorschau. | **entschieden 11.09.2026 (Empfehlung), umgesetzt #200**: eigener Merker samt `FASSUNG_DIALOGDATEN` und Datum in `KiEinwilligung`; gefragt wird EINMAL beim ersten Einschalten des Schalters, zurückgenommen wird im `KiEinstellungenDialog`. Ohne eingehängten Haken gibt es keinen Weg zu ihr — ein Lauf ohne Oberfläche überträgt keine Feldwerte |
| **KI‑D‑Q3** Welche Masken zuerst für Weg 5? | Heizkessel, PV, Pufferspeicher, Wärmepumpe (deklariert), dann die Stromspeicher-Ansicht. | **entschieden 11.09.2026 (Empfehlung), umgesetzt #200 (lesen) und #201 (setzen)**: die vier auf Eigenschaftsnamen ihres Razor-Daten-Objekts umgestellt (Feldumfang unverändert 15/3/1/1), die Stromspeicher-Ansicht als fünfte Deklaration mit 16 Feldern. Mit #201 melden alle fünf ihre `KiMaskenhaken` an und sind in dieser Reihenfolge verdrahtet; je Maske führt `EPOS.UI.Tests/Dialoge/Hilfe/KiFeldSetzenTests` einen Fall. **`Schreibgeschuetzt` trug dabei zunächst nur die Wärmepumpe** (Bericht #201, Restpunkt); Heizkessel, Photovoltaik und Pufferspeicher folgen mit **#211** |
| **KI‑D‑Q4** Darf der Assistent speichern oder nur Felder füllen? | Beides, Speichern nur mit Bestätigung und Sicherungspunkt (datenbankwirksam, Aufgabensteuerung 4.4). | **entschieden 11.09.2026 (Empfehlung), umgesetzt #201**: `dialog_speichern` ruft den Speicherweg der offenen Maske — Stufe 2, `datenbankwirksam`, damit mit Sicherungspunkt VOR der Bestätigung; der Pfad steht in der Bestätigung und im Ergebnis. Ohne Freigabe wird der Speicherweg nicht einmal gerufen |
| **KI‑D‑Q5** Welche Masken bekommt der Assistent über die ersten vier hinaus? | Alle Masken mit Einstellwerten, in sechs Wellen nach Nutzen (F1 Erzeuger im Projekt, F2 Simulationskonfiguration, F3 Bedarf und Klima, F4 Kosten und Wirtschaftlichkeit, F5 Erzeugerkataloge, F6 Strom, Berichte, Projekt); ausgenommen die Masken des Assistenten selbst, die Lizenzmasken, die Administration, reine Auswahl‑, Übernahme‑ und Importmasken sowie Startseite und Assistentenschritte | **entschieden 20.09.2026 (Empfehlung), KI‑F1 umgesetzt #416, KI‑F2 #419, KI‑F1b #420, KI‑F3 #421, KI‑F4 #423, KI‑F5 #424, KI‑F6 #425 — alle sechs Wellen umgesetzt**; Bestandsaufnahme: 119 Razor-Masken mit Eingabefeldern, davor 7 angemeldet, nach F6 63 im Katalog (der Rest sind Bausteine ohne eigenes Fenster, Anzeigen und die genannten Ausnahmen) |
| **KI‑D‑Q6** Welche Felder einer freigegebenen Maske darf der Assistent setzen — nur Zahlen, Texte und Schalter, oder jedes Eingabefeld? | Jedes Eingabefeld: Auswahlfelder (Energieträger, Gerät, Modul, Betriebsart …) bekommen den Feldtyp „Wahl" mit den Einträgen der Maske und werden über den angezeigten Text gesetzt; nur errechnete Anzeigen und Tabellen mit eigenem Editor bleiben lesbar. Dazu tolerante Feldnamen (Schlüssel, Anzeigename, eindeutiger Anfang, Umlaute), damit „vorlauftemperatur" auf „vorlauf" trifft statt abzulehnen | **entschieden 20.09.2026 (Empfehlung, vom Anwender bestätigt: „Es sollen alle Eingabefelder der Dialoge gesetzt werden können"), umgesetzt #420 (KI‑F1b)**: `KiParameterTyp.Wahl` mit Einträgen der Maske zur Laufzeit, eine Namensregel für Feldnamen und Wahlwerte (`KiKern/KiWahl.cs`), Katalog 218 Felder mit 36 Wahl-Feldern über 19 Masken; außen vor bleiben Tabellen mit eigenem Editor, Ladevorgänge und Mengen von Verweisen |
| **KI‑D‑Q7** Was geschieht mit den Abweichungen, die die Wellen F4 und F5 offen ließen — Wärmepumpe `modulkosten` setzbar statt nur lesbar, Photovoltaik ohne Auslegungstemperaturen, Überlagerung „Anlagenwerte" ausgeschlossen, `modell_erweitert` als Wahrheitswert statt Wahl, drei Felder des Reiters „Ertrag" der Kostenverwaltung? | Alle umsetzen: `modulkosten` nur lesbar, `Form_PV` auf eine Sichtklasse mit den Auslegungstemperaturen, „Anlagenwerte" als eigene Maske (eigenes Fenster), `modell_erweitert` als Wahl, Kostenverwaltung auf eine Sichtklasse mit Komponentenwahl, PV‑Wahl und PV‑Projekt — je mit Zeuge; die Markup-Probe entfällt dort, wo die Sichtklasse sie ersetzt | **entschieden 21.09.2026 (Anwender: „Umsetzen"), umgesetzt #427 (KI‑F7)**: `Form_PV` und Kostenverwaltung über Sichtklassen, `Form_PV_Anlagenwerte` als eigene Maske, `modell_erweitert` als Wahl, `modulkosten` nur lesbar; Katalog 64 Masken, 733 Felder |
| **KI‑D‑Q8** Wie erreicht `dialog_oeffnen` die Masken, die auf einer Plattform kein Ziel haben — die vier Blätter der Ansicht „Berichte und Kosten" und die Projektvariante unter Windows, die Katalog- und Strommasken auf iOS, die Kostenkataloge auf beiden, der Klimadaten-Dialog auf iOS? | Ein Sammelauftrag in zwei Teilen: Windows — `WinFormsNavigation.OeffneMaske` bekommt Fälle für die Ansicht „Berichte und Kosten", die Projektvariante und die Startseite mit Reiter Energieerzeuger über den Weg der Hauptfensterhülle, die Kostenkataloge über ihre Menüeinträge; iOS — `IosNavigation.Uebersetze` übersetzt die Verwaltungsschlüssel der Kataloge, Peak-Shaving, Stromganglinien-Verwaltung und Projektkopie auf ihre Seitenschlüssel, `AppWurzel` öffnet Klimadaten und Projektvariante. Eine Welle, Windows-Schale wird gebaut | **entschieden 21.09.2026 (Empfehlung), umgesetzt #428 (KI‑F8)**: Windows acht ausdrückliche Fälle in `WinFormsNavigation` — Kostenkataloge, Klimadaten und Projektvariante über den Menüweg der Hauptfensterhülle (`Springe`), „Berichte und Kosten" und Startseite über die Wurzel; Reiter- und Blattwunsch als Argument in `KiMaskenziele`, `dialog_oeffnen` reicht es durch. iOS ohne Übersetzung (die Schlüssel sind textgleich): `AppWurzel` öffnet Klimadaten, Projektvariante, Projektkopie, Peak-Shaving und Stromganglinien-Verwaltung über Nähte in `IProjektQuelle`; die Katalogverwaltungen bleiben auf iOS benannt abgelehnt — KI‑D‑Q10 |
| **KI‑D‑Q9** Zwei Bestandsbefunde im Speicher-Zeitreihen-Dialog: sechzehn deutsche Beschriftungen als Literale im Markup, und die Intervallkonvention wird über einen Listenindex gebunden (1 = Ende, sonst Anfang), obwohl die Aufzählung mit „Automatisch" beginnt — die Vorgabe lässt sich in der Maske nicht wählen | Beschriftungen in Ressourcen überführen (de und en); die Klappliste an die Aufzählung binden statt an einen Index, mit „Automatisch" als erstem Eintrag; ein bunit-Zeuge je Befund | **entschieden 21.09.2026 (Empfehlung), umgesetzt #428 (KI‑F8)**: 32 Ressourcenschlüssel je Sprache (`SZR_*`) über ein Texte-Bündel, Klappliste aus `GanglinienOptionenModell` mit „Automatisch" zuerst, Erkennungsregel einmal in `SpeicherEngine.IntervallKonventionErkennung` (Ganglinien-Prüfung und Zeitreihen-Import rufen sie), Kern lässt „Automatisch" zu; Zeugen je Befund, Vorgabe bleibt `Anfang` |
| **KI‑D‑Q10** Sollen die neun Katalogverwaltungen (Heizkessel, Pufferspeicher, Wärmepumpe, BHKW, Solarkollektoren, Modulkatalog PV/Stromspeicher/Wechselrichter, Stromverbraucher) auf iOS erreichbar werden? `dialog_oeffnen` lehnt sie dort benannt ab, weil die Katalogverwaltung auf iOS als Funktion fehlt (kein Menü, `Katalogwege` nur unter Windows belegt) | Wenn ja: die Windows-Hüllen nach dem Muster der vier in #428 umgezogenen (Klimadaten, Projektkopie, Peak-Shaving, Stromganglinien) plattformfrei nach `EPOS.UI.Daten` bringen und der Wurzel je Maske einen Zweig geben — mechanisch, eine Welle; wenn nein: die benannte Absage bleibt die richtige Angabe | **offen (Empfehlung: erst mit iU11, wenn die Startseite auf iOS steht)** |
| **KI‑D‑Q11** Welche Masken steuert der Assistent — bleiben die Administrationsdialoge (Katalogpflege) draußen, wie es KI‑D‑Q5 und Aufgabensteuerung 11.7 bisher sagten? Auslöser: In der „Administration Heizkessel" lehnte „setze die Vorlauftemperatur auf 55 °C" ab („keine steuerbare Maske geöffnet"), obwohl die Verwaltung seit der Neuordnung die Pflegemaske mit editierbarem Stammblatt ist | Steuerbar ist jede Maske mit Einstellwerten, Projekt- wie Administrationsdialoge. Nicht steuerbar bleiben: reine Anzeigen (Ergebnis-, Berichts-, Übersichtsseiten), Verwaltungen ohne Einstellwerte (nur Zeitreihen/Herkunft), Auslieferungssätze (Schloss — Weg: Duplizieren), die Aktionen Neu/Duplizieren/Löschen/Import/Export, Dateidialoge, Rückfragen, Lizenz- und Schlüsseleingaben, der Hilfe-Assistent selbst; die Gebäude-Verwaltung bleibt offen, solange ihre Hülle nur liest | **entschieden 23.09.2026 (Anwender: „alle Masken außer den nicht sinnvoll steuerbaren sollen steuerbar sein")**, **umgesetzt #456** für die vier Erzeugerverwaltungen und die drei Bedarfsverwaltungen (Stufe S5); die übrigen noch nicht angemeldeten Masken mit Einstellwerten folgen nach Inventar. Die Ausnahmeliste steht als Daten in `KiDialogAusnahmen.Alle` (`EPOS.Kern/Allgemein/KI/Dialoge/KiDialogAusnahmen.cs`, Gruppen `KiAusnahmegrund`); **mit #458 Stufe 1 vollständig und bewacht** (`KiMaskenabdeckungWacheTests`, Abschnitt 4 „Abdeckung"); **mit #458 Stufe 2 angemeldet** Kennlinieneditor, Projektkopf, Startseite, Programmeinstellungen, „Alle Daten" der sechs Erzeugermasken und die Anzeigeschalter der Ergebnisblätter; offen bleiben allein die drei Zapfprofil-Masken (Stufe 3 nach Z3); Aufgabensteuerung 11.7 führt die Gruppen als Liste |

| **KI‑D‑E‑1** Zwei Pillen auf einer Sicht — welche bleibt? | Eine je Bildschirm: die des Kopfbands, und ihr Schlüssel folgt der aktiven Ansicht. Dazu bekommt die zweite Sicht einen Kontext, der sich vom ersten unterscheidet — sie meldet ihre Felder an. | **entschieden 11.09.2026 (Empfehlung), umgesetzt #221**: `CascadingValue HilfePilleImKopfband` + `AppWurzel.AktiverHilfekontext`; die Simulationsansicht als sechste Katalogmaske mit 18 Feldern; Startfragen je Bereich |

**Entscheid 11.09.2026: alle vier nach Empfehlung.**

---

## 8. Befunde der Bedienung (Windows-Abnahme 11.09.2026)

Die drei Wege stehen seit #199/#200/#201; die Abnahme am Gerät hat zwei Dinge zurückgegeben, die
**keine** Fachfrage sind und trotzdem den ganzen Assistenten unbrauchbar machen. Beide sind mit
**Auftrag #219** behoben. Ein drittes kam am selben Tag hinterher — es war die **Nebenwirkung**
der ersten Behebung und ist mit **Auftrag #228** behoben (KI‑D‑B‑3).

### KI‑D‑B‑1 — „die Eingabe funktioniert nicht"

**Befund (Anwender, Bildschirmfoto).** Aus der Simulationsansicht (Werkzeugleiste, Hilfe-Pille)
öffnet der Assistent als eigenes Fenster mit der richtigen Kontextzeile („Bereich: Detaillierte
Simulation | Dialog: Simulation"). Eingabefeld und Knöpfe sehen aktiv aus — **Tippen kommt nicht
an.**

**Nicht die Ursache: die Sperre des Dialogs.** `KiChatDialog.Gesperrt` hätte Feld UND Knöpfe
gesperrt gezeichnet; auf dem Foto ist nichts gesperrt. Nachgestellt und festgehalten in
`EPOS.UI.Tests/Dialoge/Hilfe/KiChatBedienungTests` (Kontext aus einer Ansicht, kein `Belegt`,
kein Lauf → kein `disabled`), samt Gegenprobe für den umgekehrten Fall.

**Die Ursache: der TASTATURFOKUS.** Das Chatfenster ist das einzige nicht-modale Fenster des
Hauses (Entscheid E‑6) und trägt eine ZWEITE WebView2. Zwei Dinge trafen zusammen:

1. **Der Öffnungsweg lief am `Blazorsprung` vorbei.** Die Pille ruft
   `KiAssistentWeg.AusDialog` → `Dienste.Navigation.OeffneMaske(Masken.KiAssistent, …)` →
   `WinFormsNavigation`, und dort stand bis #219 der blanke Aufruf `KiChatHuelle.Oeffnen(…)`.
   Damit entstand das zweite Fenster samt zweiter WebView2 **synchron im
   `WebMessageReceived`-Rückruf der ersten** — genau die Lage der Befunde W16b‑B‑1 (leere
   Startkacheldialoge), W13‑B‑1 (Dateiwähler) und W15b‑B‑1 (Einstellungen). Der MENÜweg tat das
   nie: `HauptfensterHuelle.Weg` verzögert seit W16b jeden Punkt.
2. **Niemand übergab die Tastatur.** Ein `ShowDialog` bringt seine eigene Nachrichtenschleife mit
   und holt die Eingabe von selbst; ein `Show(besitzer)` tut das nicht. In `KiChatHuelle` stand
   hinter `Show` nichts, und beim Nach-vorn-Holen eines offenen Fensters nur ein `Activate()` —
   das stellt das Fenster vor die anderen und lässt den Tastaturzeiger, wo er war.

**Behoben (#219) an drei Stellen, und alle drei werden gebraucht:**

| Stelle | Was | Nachweis ohne Gerät |
|---|---|---|
| `WinFormsNavigation`, Fall `Masken.KiAssistent` | öffnet über `Blazorsprung.Verzoegert`; Besitzer und Aufrufkontext werden VORHER geholt und mitgegeben, die Rückgabe `true` bleibt sofort | `KiChatOeffnerTests.Der_Assistent_geht_ueber_den_Blazorsprung_auf` samt Gegenprobe |
| `BlazorDialogForm<T>.TastaturUebergeben()` (neu), gerufen aus `KiChatHuelle` an BEIDEN Wegen | `Activate()`, dann Fokus auf die innere WebView2 — zweimal: sofort und nach `CoreWebView2InitializationCompleted`, weil die WebView sich asynchron aufbaut | `KiChatOeffnerTests.Die_Chathuelle_uebergibt_die_Tastatur_auf_beiden_Wegen`, `…Die_Dialoghuelle_fuehrt_die_Fokusuebergabe` |
| `KiChatDialog` / `KiEingabezeile` | das Textfeld trägt `autofocus` und bekommt den Schreibzeiger nach dem ersten Zeichnen (`ElementReference.FocusAsync`) — einmal, nur wenn nicht gesperrt; ein NEUER Aufrufkontext setzt die Marke erneut (das zweite Öffnen kennt kein erstes Zeichnen) | `KiChatBedienungTests` (vier Fälle samt Gegenprobe bei laufender Aktion) |

**Was am Gerät bleibt.** Ob die Tastatur beim Anwender wirklich in der zweiten WebView2 landet,
sagt nur Windows: bunit kennt kein Fenster, und der Fokusweg WinForms → WebView2 → DOM ist keine
Sache dieser Bibliothek. Die Abnahme prüft deshalb beide Öffnungswege — Pille aus einer Ansicht
und Menü Hilfe → Assistent — und dazu das zweite Öffnen bei stehendem Fenster.

### KI‑D‑B‑2 — „Online-Dokumentation öffnen tut nichts"

**Befund (Anwender, während des Laufs von #219).** Der Fußleistenverweis des Chats reagiert nicht.

**Die Ursache ist EINE Zeile, und sie ist im Haus schon einmal benannt worden.**
`KiChatHuelle.Gaben.AdresseOeffnen` rief `Dienste.Datei.MitSystemOeffnen`, und dessen
Windows-Fassung beginnt mit `if (!File.Exists(pfad)) return false;` — für eine Adresse also immer
`false`, ohne Wirkung und ohne Meldung. Genau diesen Fall beschreibt `IDateiDienst.AdresseOeffnen`
seit iU9‑W16c.3 in seiner eigenen Dokumentation; der Menüpunkt „Hilfe → Dokumentation" geht seither
richtig, der Chat ging weiter am Dateiweg. **Betroffen war nicht nur der Fußleistenverweis**,
sondern JEDER Verweis des Chats: die Wikitreffer der Suche und die Verweise aus einer
Modellantwort laufen über denselben Rückweg `AdresseGewaehlt`. Dieselbe Verwechslung stand ein
zweites Mal im Rückfall von `WindowsHilfeDienst.Oeffnen` (der Weg, den der i-Knopf nimmt, wenn das
angeheftete Popup nicht aufgeht).

**Behoben (#219):** beide Stellen rufen `Dienste.Datei.AdresseOeffnen`. Die Prüfung „nur http und
https" bleibt, wo sie war — in derselben Anzeige landet Modelltext, und der ist Fremdtext.
**Wache:** `KiChatOeffnerTests.Eine_Adresse_geht_nie_ueber_MitSystemOeffnen` liest jede
`.cs`-Datei der Windows-Anwendung und meldet jedes `MitSystemOeffnen`, dessen Argument nach einer
Adresse aussieht; dazu `…Kein_Weg_des_Assistenten_ist_ein_leerer_Delegat` und
`…Jedes_Bedienelement_des_Assistenten_hat_seinen_Weg` — beide beantworten die Frage, die bis #219
niemand gestellt hatte: **kommt der Klick überhaupt irgendwo an?**

**Kein Befund waren** (geprüft, Auftrag #219): „Was wird gesendet?", „Protokoll anzeigen",
„Verlauf kopieren" (den Text liefert die Komponente, die Zwischenablage schreibt die Hülle —
`navigator.clipboard` kommt nicht vor), „Rechtshinweis anzeigen", „Einstellungen…", „Werkzeuge…",
„Aktionen zulassen", „Fragen", „Nur suchen", „Schließen", der i-Knopf, der Kontextlink und der
Tageszähler. Jedes dieser Elemente hat seitdem einen bunit-Fall.

### KI‑D‑B‑3 — „der Hilfe-Assistent lässt sich nicht mehr aus dem Hauptmenü aufrufen (auch mit F1 nicht)"

**Befund (Anwender, 11.09.2026, unmittelbar nach #219).** Der Assistent ist aus einer Ansicht
(Hilfe-Pille) aufgegangen und wieder geschlossen worden. Danach tut der Menüpunkt
**Hilfe → KI-Assistent** nichts, und **F1** ebenso wenig. Keine Meldung, kein Absturz — es
passiert schlicht nichts.

**Zwei Ursachen, die nichts miteinander zu tun haben.** Dass beide am selben Tag auffallen, ist
kein Zufall: Die eine ist die Nebenwirkung von #219, die andere lag seit W16c da und hatte bis
dahin niemand geprüft (der Punkt „Menü und F1" stand im Umsetzungskonzept ausdrücklich als
**Windows-Abnahme steht aus**).

**Ursache 1 — der SPRUNG IM SPRUNG.** `Blazorsprung` führt einen prozessweiten Riegel
`_angefordert`: „ein Sprung zur Zeit", damit nicht zwei schnelle Kachelklicks zwei modale Fenster
in die Schlange stellen. Der Klassenkopf sagt seit W16b, der Riegel gelte **nur bis zum Beginn**
des Sprungs — der Programmtext löste ihn aber erst im `finally` von `Ausfuehren`, also am **Ende**.
Solange jeder Sprung genau eine Ebene tief war, fiel das nicht auf. Seit #219 ist er zwei Ebenen
tief, und zwar auf genau diesem Weg:

1. `Seitenschluessel.KiAssistent` **ist** ein Wert von `Masken` (`"KI_ASSISTENT"`);
   `HauptfensterHuelle.Weg` erkennt ihn deshalb in `Maskenschluessel` und verzögert ihn
   (`HauptfensterHuelle.cs:183‑187`). Der eigens dafür gebaute Fall
   `case Seitenschluessel.KiAssistent` in `Ablauf(…)` wird auf dem Menüweg **nie erreicht** — die
   Schlüsseltabelle greift vorher.
2. Der geposteten Nachricht folgt `MaskeOeffnen` → `Dienste.Navigation.OeffneMaske` →
   `WinFormsNavigation`, Fall `Masken.KiAssistent` — und der ruft seit #219 selbst
   `Blazorsprung.Verzoegert` (`WinFormsNavigation.cs:213‑219`).
3. Der innere Ruf traf den Riegel des äußeren, der noch stand, und kehrte bei
   `if (_angefordert) return;` (`Blazorsprung.cs:85`) **stumm** zurück. Kein Protokolleintrag,
   kein Fenster.

**Ursache 2 — F1 aus der WebView2.** `Hauptfensterrahmen` fing die Taste mit
`KeyPreview = true` und einem `KeyDown`-Handler. `KeyPreview` wirkt aber nur für Tasten, die im
`WndProc` eines **WinForms**-Steuerelements ankommen: Erst `Control.ProcessKeyMessage` fragt
`parent.ProcessKeyPreview`. Seit W16c ist das ganze Fenster **eine** `BlazorSeite`, der
Tastaturzeiger sitzt also praktisch immer in der WebView2 — und deren Tastenmeldungen gehen an das
Browserfenster, ein natives Kindfenster ohne WinForms-`WndProc`. Der `KeyDown` des Rahmens wurde
damit nie ausgelöst. Vor W16c hing F1 als **Menükürzel** an einem `ToolStripMenuItem`, und
Menükürzel laufen über `Form.ProcessCmdKey` — deshalb ging es früher.

**Behoben (#228) an vier Stellen:**

| Stelle | Was | Nachweis ohne Gerät |
|---|---|---|
| `Blazorsprung.Ausfuehren` | Der Riegel fällt als **ERSTES**, nicht im `finally` — genau das, was der Klassenkopf seit W16b verspricht. Ein Sprung, den dieser Sprung anstößt, reiht sich damit regulär ein (eine Nachricht später) statt verschluckt zu werden. Er läuft ausdrücklich **nicht** unmittelbar: Der äußere Sprung kann inzwischen in einer verschachtelten Nachrichtenschleife stehen, und dann käme der innere Ruf wieder aus einem WebView2-Rückruf | `KiChatOeffnerTests.Der_Riegel_des_Sprungs_faellt_vor_dem_Sprung` samt Gegenprobe |
| `Blazorsprung.RiegelSteht` (neu) | Ein abgewiesener Sprung steht im **Protokoll** (`Debug`/`Trace`), und ein **verwaister** Riegel verfällt nach fünf Sekunden. Der Verfall ist die zweite Hälfte derselben Sache: Eine mit `BeginInvoke` eingereihte Nachricht läuft nie, wenn ihr Wirtsfenster vorher abgebaut wird — ohne Frist bliebe der Riegel für die restliche Sitzung stehen, und dann wären Menü, Kacheln **und** Pillen auf einen Schlag stumm | `KiChatOeffnerTests.Ein_abgewiesener_Sprung_steht_im_Protokoll_und_der_Riegel_verfaellt` |
| `Blazorsprung.Wirtsfenster` (neu), benutzt in `Verzoegert` und in `WinFormsNavigation` | Rückfall auf das **Hauptfenster**: `Form.ActiveForm` kann `null` sein, und dann lief der Sprung bis dahin unmittelbar (also doch im Rückruf) **und** das nicht-modale Chatfenster ging ohne Besitzer auf — ohne Besitzer und ohne Taskleisteneintrag (`ShowInTaskbar = false`) ist es hinter dem Hauptfenster nicht wiederzufinden. EINE Ermittlung trägt jetzt beides, Nachrichtenschlange und Besitzer | `KiChatOeffnerTests.Der_Assistentenweg_faellt_auf_das_Hauptfenster_zurueck` |
| `Hauptfensterrahmen.ProcessCmdKey` (ersetzt `KeyPreview` + `KeyDown`) | `ProcessCmdKey` erreicht der Tastendruck auch aus der WebView2: `Application.ThreadContext.PreTranslateMessage` sucht über `Control.FromChildHandle` das nächste verwaltete Steuerelement — das ist die `WebView2`, deren Kindfenster das Browserfenster ist —, ruft dort `PreProcessMessage`, und das reicht `ProcessCmdKey` die Elternkette hinauf. Der Weg deckt den bisherigen mit ab (er läuft auch, wenn ein gewöhnliches WinForms-Kind den Zeiger hat) | `KiChatOeffnerTests.Das_Hauptfenster_faengt_F1_ueber_ProcessCmdKey` |

**Dazu der Lebenszyklus der Hülle**, weil er dieselbe Art von stiller Sperre tragen konnte:
`KiChatHuelle.Einhaengen()` setzte `_offene = this` — und das läuft **vor** `Show()`. Bricht der
Aufbau danach ab, stünde `_offene` auf einer Hülle, deren Fenster nie erscheint und deshalb nie
ein `FormClosed` meldet; jedes weitere Öffnen „holte es nach vorn" und täte sichtbar nichts, auf
**jedem** Weg. Seit #228 trägt das Feld nur, was fertig gebaut ist (gesetzt in `Oeffnen`), ein
Fehlschlag hängt aus und meldet sich, und `Steht` prüft zusätzlich `IsHandleCreated`.
**Wache:** `KiChatOeffnerTests.Die_Chathuelle_merkt_sich_nur_ein_gebautes_Fenster`.

**Was am Gerät bleibt.** Beide Ursachen sind WinForms und Windows-Nachrichtenschleife; der
Quelltextzeuge belegt, dass der Weg gebaut ist, nicht dass Windows ihn geht. Die Abnahme prüft
deshalb: **Menü Hilfe → KI-Assistent** (auch zweimal hintereinander), **F1** mit dem Zeiger in der
WebView2, die **Pille** aus einer Ansicht und aus einem Dialog — jeweils nach einem vorherigen
Öffnen und Schließen des Assistenten.

### KI‑D‑E‑1 — „zwei KI-Buttons ohne unterschiedliche Funktion"

**Befund (Anwender, Bildschirmfoto der Simulationsansicht, 11.09.2026).** „Die KI-Buttons haben
keine unterschiedliche Funktion im Kontext. Daher ist es nicht sinnvoll, auf einer Sicht zwei
KI-Buttons zu sehen. Es muss einen Kontext in der KI-Funktion der zweiten Sicht geben, der sich von
dem anderen KI-Button unterscheidet."

**Gemessen.** `Hauptfenster.razor:66` zeichnete die Kopfband-Pille mit dem festen Schlüssel
`Hauptfenster.btn_Help` — über JEDER Ansicht. `SimulationSeite.razor:93`,
`Strom/StromspeicherAuslegungSeite.razor:76` und der Kopf von `SimulationKonfigSeite` zeichneten
eine zweite mit eigenem Schlüssel. Mehr Unterschied gab es nicht: An der Maskenbrücke meldeten nur
die vier Erzeugerdialoge und die Stromspeicher-Auslegung Felder an; die Simulationsansicht meldete
**nichts** — `dialog_lesen` und „Feldwerte mitsenden" hatten dort nichts zu zeigen.

**Behoben (#221) in drei Schritten:**

| Schritt | Was | Nachweis |
|---|---|---|
| **Eine Pille je Bildschirm** | `CascadingValue HilfePilleImKopfband` aus `Hauptfenster`; die vier freien Ansichten und der Kopf von Schritt ① lassen ihre eigene weg. Inline-Knöpfe im Inhalt („Berechnungsweg…") und Überlagerungsdialoge behalten sie — sie tragen andere Schlüssel | `HilfePilleTests`: genau eine Pille im Kopfbereich, KEIN Schlüssel zweimal auf demselben Bildschirm, und die iOS-Gegenprobe (AppWurzel ohne Kopfband zeichnet die eigene) |
| **Die Pille folgt der Ansicht** | `Hilfekontext` (Schlüssel, Ansicht, Schritt, Reiter) + `Hilfekontextmelder` als CascadingValue der `AppWurzel`; `Hauptfenster` bindet Schlüssel, Dialognamen und `Aktiv` daran. Der Schlüssel bestimmt zweierlei: die Hilfeseite UND den Bereich des Assistenten | `HilfePilleTests`: Simulation Schritt ① → `B_SIM_KONFIG`, Schritt ③ → `B_SIM_DETAIL`, Auslegung → ihr Schlüssel, zurück zur Startansicht → Fensterschlüssel |
| **Der Kontext bekommt Substanz** | Die Simulationsansicht ist die **sechste** Katalogmaske: 18 Felder aus `SimulationKiSicht` — Kaskade und nicht aufgenommene Anlagen (lesend), die fünf Laufparameter (lesbar und setzbar über `SimulationParameterDienste`, mit Plausibilitätsgrenzen im Maskenhaken), die sieben Kennzahlen des Laufs samt SoC-Band, offenem Reiterblatt und Laufhinweisen (lesend). Dazu die Startfragen `KI_FRAGE_SIMULATION_KONFIG` / `KI_FRAGE_SIMULATION_ERGEBNIS` statt der leeren Eingabezeile | `KiSimulationMaskeTests`: 18 Felder, Werte je Schritt, `feld_setzen` geht über den Delegaten, genau fünf setzbare Felder, eine Kennzahl wird benannt abgelehnt |

**Warum die Daten aus den vorhandenen DTO kommen.** `SimulationKiSicht` löst die Ketten über
`SimulationKonfigDaten`, `ParameterDaten` und `SimulationErgebnisDaten` auf; den Ergebnisstand gibt
`SimulationErgebnisHuelle.LetzterStand` her — das, was die Seite gerade zeigt. Ein eigener Ladeweg
wäre ein zweiter Stand derselben Zahlen und je Leseanfrage ein weiterer Datenbankzugriff.

**Was am Gerät bleibt.** Ob die Pille im Kopfband im laufenden Programm mit dem Schritt umspringt
und ob der Chat die neue Kontextzeile („Simulation · 3 Ergebnis · Stromspeicher") zeigt, sagt die
Windows-Abnahme; bunit kennt kein zweites Fenster.
