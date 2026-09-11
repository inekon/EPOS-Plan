# Konzept: Der Hilfe-Assistent im Dialog — Aufruf, Kontext, Feldzustand, Steuerung

Stand 11.09.2026 · Zweig `ios_migration_september` · Aufgabe #198 · Ablage `Projekte/` · Status: **entschieden am
11.09.2026** (Anwender: „Stufe 1 mit den Wegen 1 und 2, Weg 4 danach für die vier deklarierten Masken und die
Stromspeicher-Ansicht, Weg 5: Assistent soll steuern"). Umsetzung in drei Stufen nach Abschnitt 6. **Die Fragen KI‑D‑Q1 bis KI‑D‑Q4 sind am 11.09.2026 nach Empfehlung
entschieden** (Anwender: „KI‑D‑Q1 bis Q4: Empfehlung"), siehe Abschnitt 7. **Alle drei Stufen sind umgesetzt**
(#199, #200, #201); der Restpunkt aus #201 — Fortschrittsbalken und Abbrechen bei den Rechenaktionen — ist mit
**#214** (11.09.2026, Anwenderentscheid „Empfehlung starten") geschlossen, siehe Abschnitt 3.4.

Dieses Konzept baut auf [`Konzept_KI-Assistent_Aufgabensteuerung.md`](../Konzept_KI-Assistent_Aufgabensteuerung.md)
(Aktionsregister, drei Schutzstufen, Bestätigung, Sicherungspunkt, Protokoll) und auf
[`Konzept_Hilfesystem_Infobutton_EPOS-Plan.md`](../Konzept_Hilfesystem_Infobutton_EPOS-Plan.md) (Info-Knopf,
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

---

## 5. Was bewusst nicht Teil dieses Konzepts ist

- Kein neues Sprachmodell, keine Änderung an `KiChatService`, Wissensbasis-Formaten oder dem Semantikindex.
- Kein Assistent ohne Anwenderfrage (keine „proaktiven" Vorschläge beim Öffnen eines Dialogs).
- Keine Deklaration aller 88 Dialoge für Weg 4/5 — nur die fünf genannten; weitere je Anwenderwunsch.
- Kein Planer auf iOS (SP‑O‑3).

---

## 6. Stufenplan und Aufträge

| Stufe | Auftrag | Inhalt | Prüfmuster |
|---|---|---|---|
| **S1** (Wege 1 + 2) — **umgesetzt #199** (11.09.2026) |  **#199** | `InfoKnopf.MitAssistent` mit `KiKnopf`; `KiAufrufkontext`, `Masken.KiAssistent` in `Dienste.Navigation`, Windows-Hülle und `AppWurzel` öffnen mit Kontext; `KiChatKontext.BereichFuerHilfeschluessel` (Tabelle, Test über alle Schlüssel), `AktiverBereich` aus der Oberfläche; `Warnbanner.Kennung` + Link, Kennungen an Diagnosebanner, Prüfhinweisen, Vorprüfung, Laufwarnungen, Strangampel; `HilfeWissen`-Abschnitte je Kennung; `KI_FRAGE_*` de/en; Kontextzeile im Chat zeigt Dialog und Kennung | bunit: Knopf in einem Dialog mit und ohne Assistent, Öffnen mit Kontext, Bannerlink mit Kennung; Kern: Bereichstabelle vollständig, Aufruf setzt den Haken; Referenzlauf unberührt |
| **S2** (Weg 4) — **umgesetzt #200** (11.09.2026) | **#200** | `KiMaskenbruecke` im Kern; `KiDialogKatalog` auf Eigenschaftsnamen; die vier Dialoge und die Stromspeicher-Ansicht melden ihre Felder an; Einwilligungsstufe „Dialogdaten"; Schalter „Feldwerte mitsenden" + Vorschau im Chat; `dialog_lesen` aus der Brücke | Kern: Brücke liest die fünf Masken; bunit: Schalter, Vorschau zeigt Werte, ohne Einwilligung nichts; Protokoll |
| **S3** (Weg 5) — **umgesetzt #201** (11.09.2026, `d1bfb56`, Merge `9122812`; Wiki `Hilfe-Assistent` Rev. 542) | **#201** | `KiAktionen` in den Kern; `feld_setzen` über die Brücke mit Bestätigungsblock, Plausibilität des Dialogs, Lesemodus/ReadOnly; `dialog_oeffnen`; speichern mit Sicherungspunkt; rechnen mit `Fortschritt`; Protokoll alt → neu | Kern: Setzen, Ablehnung im Lesemodus, Sicherungspunkt; bunit: Bestätigung → Feld im Dialog; Referenzlauf unberührt |
| **S3‑Rest** (Fortschritt und Abbruch) — **umgesetzt #214** (11.09.2026) | **#214** | `KiChatDialog` hält je Anforderung eine `CancellationTokenSource` und meldet Senke und Marke über `KiChatSteuerung` an; Baustein `Fortschritt` im Chat (Balken, Schritttext, „Abbrechen"), Schlusszeile im Verlauf mit Namen und Dauer; Senden und Aktionsknöpfe während eines Laufs gesperrt; lange Aktionen laufen im Hintergrund (`KiAusfuehrung.ImHintergrund`) statt über `AufOberflaeche`; Abbruch kommt in allen drei Rechenaktionen an (`SimulationRunner` mit `IProgress`/`CancellationToken`, `Dienste.Abbrechen` der Stromspeicher-Ansicht) | bunit: Balken während einer langen Aktion, „Abbrechen" setzt die Marke, Verlaufszeile „abgebrochen"; Kern: eine lange Aktion nutzt `AufOberflaeche` NICHT (Gegenprobe: eine kurze zweimal); KiKern: `KiLaufumgebung`/`KiFortschritt`; Referenzlauf 1030/1046 byte-gleich |
| **Bedienung** (Befunde der Abnahme) — **umgesetzt #219** (11.09.2026) | **#219** | KI‑D‑B‑1 (Tastaturfokus des nicht-modalen Chatfensters): `Masken.KiAssistent` öffnet über `Blazorsprung.Verzoegert`, `BlazorDialogForm.TastaturUebergeben()` gibt der zweiten WebView2 die Eingabe (sofort und nach ihrer Initialisierung), das Textfeld trägt `autofocus` und bekommt den Schreibzeiger nach dem ersten Zeichnen sowie bei jedem neuen Aufrufkontext. KI‑D‑B‑2 (tote Verweise): eine ADRESSE geht über `Dienste.Datei.AdresseOeffnen` statt über `MitSystemOeffnen` — in `KiChatHuelle.Gaben` und im Rückfall von `WindowsHilfeDienst` | bunit: jedes Bedienelement des Chats einzeln (`KiChatBedienungTests`, 19 Fälle); Quelltextwachen `KiChatOeffnerTests` (Sprung, Fokusübergabe, Adressweg, kein leerer Delegat, kein Bedienelement ohne Weg — je mit Gegenprobe); Referenzlauf unberührt. **Am Gerät bleibt** der Fokusweg WinForms → WebView2 → DOM auf beiden Öffnungswegen |

| **Kontext** (KI‑D‑E‑1) — **umgesetzt #221** (11.09.2026) | **#221** | **Eine Pille je Bildschirm** und ein Kontext mit Substanz: `Hilfekontext`/`Hilfekontextmelder` in `EPOS.UI/Dienste`, `AppWurzel.AktiverHilfekontext` samt `HilfekontextGeaendert`, `CascadingValue HilfePilleImKopfband` aus `Hauptfenster`; die vier freien Ansichten und der Kopf von Schritt ① lassen ihre Pille unter dem Kopfband weg und melden statt dessen Ansicht · Schritt · Reiter (Format `KI_KONTEXT_STELLE`). Die **Simulationsansicht meldet sich an der Maskenbrücke an** — sechste Katalogmaske `KiMaskennamen.SIMULATION` mit **18 Feldern** aus `SimulationKiSicht` (Kaskade und nicht aufgenommene Anlagen lesend, die fünf Laufparameter lesbar UND setzbar über `SimulationParameterDienste`, die sieben Kennzahlen des Laufs samt SoC-Band, Reiterblatt und Laufhinweisen nur lesend), Ziel in `KiMaskenziele`. Dazu `KiChatKontext.AufrufGeaendert`/`AssistentStehtFuer` für das leuchtende Feld der Pille (#218) und **Startfragen je Bereich** (`KI_FRAGE_SIMULATION_KONFIG`, `KI_FRAGE_SIMULATION_ERGEBNIS`, de/en) statt der leeren Eingabezeile | bunit: `HilfePilleTests` (eine Pille, Schlüssel folgt Ansicht und Schritt, ohne Kopfband die eigene, Stelle als Dialogname, leuchtendes Feld, Startfragen samt zwei Gegenproben); `KiSimulationMaskeTests` (18 Felder, Felder je Schritt, `feld_setzen` über den Delegaten, Kennzahlen nicht setzbar, genau fünf setzbare Felder); Kern: Startfragen in beiden Sprachen, Aufrufwechsel wird gemeldet; Referenzlauf 1030/1046 byte-gleich |

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

| **KI‑D‑E‑1** Zwei Pillen auf einer Sicht — welche bleibt? | Eine je Bildschirm: die des Kopfbands, und ihr Schlüssel folgt der aktiven Ansicht. Dazu bekommt die zweite Sicht einen Kontext, der sich vom ersten unterscheidet — sie meldet ihre Felder an. | **entschieden 11.09.2026 (Empfehlung), umgesetzt #221**: `CascadingValue HilfePilleImKopfband` + `AppWurzel.AktiverHilfekontext`; die Simulationsansicht als sechste Katalogmaske mit 18 Feldern; Startfragen je Bereich |

**Entscheid 11.09.2026: alle vier nach Empfehlung.**

---

## 8. Befunde der Bedienung (Windows-Abnahme 11.09.2026)

Die drei Wege stehen seit #199/#200/#201; die Abnahme am Gerät hat zwei Dinge zurückgegeben, die
**keine** Fachfrage sind und trotzdem den ganzen Assistenten unbrauchbar machen. Beide sind mit
**Auftrag #219** behoben.

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
