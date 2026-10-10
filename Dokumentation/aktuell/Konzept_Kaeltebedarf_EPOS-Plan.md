# Konzept Kältebedarf — Reiter, CSV-Lastgang, Kältebedarfsprofile, Deckungsart und Schema 213

Stand 10.10.2026, Rev. 1 · Sitzung Kälteanlage · Gegenstand: die **Bedarfsseite** der Kälte in Startseite, Dialogen,
Rechenweg und Simulationskonfiguration, gebaut „mit denselben Bausteinen wie die Wärme“ (E21). Die Kälteerzeuger, die
Kältefolge und das Raumklimagerät gehören nicht hierher (Abschnitt 1.2).

## 1. Anlass und Entscheide

Anwenderauftrag vom 10.10.2026: „Erstelle für den Kältebedarf analog zum Wärmebedarf Kacheln und Dialoge mit CSV
einlesen, Profile erstellen … Mit Einbindung in die Simulationskonfiguration, analog Wärme, mit Spezifika wie
Split-Kälte …“.

### 1.1 Entscheide des Anwenders (10.10.2026, je nach Empfehlung)

| Kennung | Wortlaut |
|---|---|
| **E-K1** | Eigene Gruppe „Kältebedarf“ auf der Startseite mit drei Kacheln: „Kältebedarf extern“ (CSV-Lastgang), „Kältebedarfsprofile“ (Typkatalog, Jahressumme, Temperatur, Kalender), „Gebäudekühlung“ (Sprung zu den Kühleinstellungen der Gebäude). Die Startseite kennt Reiter (Projekt, Wärmebedarf, Strombedarf, Energieerzeuger, Simulation, Berichte & Kosten), also wird es ein **siebter Reiter „Kältebedarf“**. |
| **E-K2** | Deckungsart je Zuordnungszeile (CSV-Lastgang und Profil): „zentral“ (Vorgabe, geht in die Kältefolge wie heute) oder „dezentral Split“ mit EER (fest oder linear über der Außentemperatur), Kühlträger und eigenem Zähler; der Split-Anteil wird **neben** der Kaskade gebucht und als Strom gezählt — ohne Gerätegrenze, ohne Inneneinheiten (die gehören zum Raumklimagerät Typ 14 des Entwurfs K-D; Kennfeld später von dort). |
| **E-K3** | Kältebedarfsprofile tragen Vorlauf/Rücklauf nur als Angabe ohne Wirkung auf die Erzeuger; Prozesskälte mit Vorrang (Naht `Kaelteangebot.Prozesskaelte`) ist eine spätere Welle. |

Schemaschritt **213** ist für die Welle K1 vorgesehen; er hängt an 212 `KaelteRangSchema` (Welle KB-D der Sitzung
Gebäudesimulation). Vor dem Bau gilt die Regel aus `CLAUDE.md`: Nummer in der Zeile „Schemaschritt angemeldet“ im Kopf von
[`Status_iOS_Migration.md`](Status_iOS_Migration.md) gegen origin prüfen bzw. anmelden, Kette über `+ 1` an der Vorgängerklasse.

### 1.2 Abgrenzung

| Liegt in der Übergabe Kälteanlagen bzw. bei der Sitzung Gebäudesimulation | Liegt in diesem Konzept |
|---|---|
| **K-D** Split/Multisplit als Anlagenart Typ 14 „Raumklimagerät“ mit Außengerät im Kältekatalog, Inneneinheiten je Gebäude/Zone, Gerätegrenze im Stundenrand, Buchung `Kaeltebedarf_Raumgeraete` ([Entwurf](Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md)) | Kältebedarf **ohne Gebäudemodell**: CSV-Lastgang und Profil, je Zeile zentral oder dezentral (Split) gedeckt |
| **K-E** VRF, **K-F** Rückkühlwerk, **K-B** Startkatalog, **K-H** EPREL ([Übergabe Programm](Kälteanlagen/2026-10-10_Uebergabe_Kaelteanlagen_Programm.md)) | — |
| **KB-B** Bereich „Kälte“ in `SimulationKonfigSeite.razor`, **KB-D** pflegbare Kältefolge `Kaelte_Rang` (Schema 212, E117) — Sitzung Gebäudesimulation | Anzeige und Sprung im Bereich „Kälte“ nach dem Push von KB-B (Abschnitt 6) |
| Gebäudekühlung: Rechenweg, Zonen, Kühlübergabe, Kühlkurve | nur die Kachel „Gebäudekühlung“ als Sprung und Zustandsanzeige |

**Bindungen aus E117–E121 und dem Kühlkonzept**, die dieses Konzept übernimmt:

- Eine Zone mit Inneneinheit ist ausschließlich Raumgerätezone (E120 Q2). Die Deckungsart der Bedarfsebene greift **nie**
  in Gebäude oder Zonen; sie gilt allein für Lastgang- und Profilzeilen.
- Begriff auf der Bedarfsebene: **„dezentral (Split)“**, nie „Raumklimagerät“ — der Begriff gehört Typ 14.
- Nur sensible Kälte, keine Latentlast (E31 K5); die Grenze steht an jeder Kältezahl.
- Kältefolge und `Kaelte_Rang` gehören der Gebäudesimulation. Ein Split liefert nie Kaltwasser und geht nie in die Kaskade.
- Kältenetzverluste bleiben ausgegrenzt ([Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md), Abschnitt 14).
- Kältebedarf wird mit denselben Bausteinen dargestellt wie Wärme (E21, [Statusdatei Gebäudesimulation](Status_Gebaeudesimulation_VDI6007.md)).

## 2. Befund

### 2.1 Bedarfsseite

- **Startseite.** [`Startseite.razor`](../../EPOS.UI/Seiten/Start/Startseite.razor) setzt je Reiter einen Baustein
  (`WaermebedarfReiter`, `StrombedarfReiter`, `ErzeugerReiter` …) und reicht ihm `KachelnVon(reiter)`; die Reiter stehen in
  `Reiterschluessel.Alle` (sechs), die 21 Kacheln in `Kachelschluessel`
  ([`Kachelschluessel.cs`](../../EPOS.UI/Seiten/Start/Kachelschluessel.cs)), die Bilder in
  [`Kachelbilder.cs`](../../EPOS.UI/Seiten/Start/Kachelbilder.cs) (Ordner `EPOS.UI/wwwroot/bilder/start/`, dort liegt
  `PKuehlung_Symbol.png`). Kachelliste, Statuspunkte und Verteilung `Kachelweg` (ein `switch`) stehen in der Hülle
  [`StartseiteHuelle.cs`](../../WindowsFormsApplication1/Views/Hauptformular/StartseiteHuelle.cs); die Statusbits liefert
  [`KomponentenBestandCtrl.cs`](../../EPOS.Kern/Controller/KomponentenBestandCtrl.cs) (dreizehn Bitwerte 1 … 4096, frei ab
  8192). Die Kachel „Kühlung und Kälteanlagen“ des Reiters Energieerzeuger bezieht ihren Zustand **ohne Bit** aus der
  plattformfreien Hülle [`KuehlungKachelBau.cs`](../../EPOS.UI.Daten/Erzeuger/KuehlungKachelBau.cs) — das Muster für die
  Kachel „Gebäudekühlung“. iOS kennt nur `IProjektQuelle.Startkacheln`. Hilfe:
  [`help_mapping.txt`](../../WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt) (`Form_Start.btn_Help_Waermebedarf`).
- **Kühllastgang aus CSV.** [`WaermebedarfExternDialog.razor`](../../EPOS.UI/Dialoge/Bedarf/WaermebedarfExternDialog.razor)
  führt die Kanalwahl samt Kanal „Kühlung“ (`IstKuehlkanal`, Herleitungszeile `WBX_HRL_KANAL_KUEHLUNG`); die Zeile steht in
  `Z_ProjektWaermebedarf.Kanal` (`DbWerte.KANAL_KUEHLUNG = "Kuehlung"`, `Kanal.AusText` in
  [`SimulationKanaele.cs`](../../EPOS.Kern/Allgemein/Simulation/SimulationKanaele.cs)), der Import läuft über
  `GanglinienDatei`. Mehrere Kühllastgänge je Projekt sind möglich.
  **Falle:** [`WizardCtrl.cs`](../../EPOS.Kern/Controller/WizardCtrl.cs) `Del_WaermebedarfExtern` löscht **alle**
  `Z_ProjektWaermebedarf`-Zeilen des Projekts, `Add_…` schreibt die ganze Liste. Ein zweiter Dialog, der nur die
  Kühlzeilen kennt, löschte die Wärmezeilen.
- **Bedarfsprofile.** [`BedarfsProfileDialog.razor`](../../EPOS.UI/Dialoge/Bedarf/BedarfsProfileDialog.razor) bedient
  über `BedarfsArt` ([`BedarfsArt.cs`](../../EPOS.Kern/Model/BedarfsArt.cs): Stromverbraucher, Prozesswaerme,
  Brauchwasser) drei Ausprägungen; Verteilung in `BedarfStammCtrl`, `TypProfilCtrl`, `TypStammDaten`, `TypProfilDaten`,
  `BedarfAdminDialog`, Hülle `WindowsFormsApplication1/Views/Bedarf/BedarfsProfileHuelle.cs`. Vorbild Prozesswärme: Kopf
  `Tab_Prozesswaerme_STAMM` (`Bezeichner`, `Typ`, `Beschreibung`, `Monat_1` … `Monat_12`, `ReadOnly`, dazu
  `Vorlauf`/`Ruecklauf` aus [`ProzesswaermeTemperaturSchema.cs`](../../EPOS.Kern/Allgemein/Update/ProzesswaermeTemperaturSchema.cs)),
  Typ `Tab_Prozesstyp_STAMM` mit 168 Wochenstunden `"1"` … `"168"`, Projektkopien `Tab_Prozesswaerme`/`Tab_Prozesstyp`
  (Schlüssel `Typname`, `ID_Prozesswaerme`), Zuordnung `Z_Projekt_Prozesswaerme` mit `Summe` und `ID_Betriebskalender`
  ([`BedarfNetzKalenderSchema.cs`](../../EPOS.Kern/Allgemein/Update/BedarfNetzKalenderSchema.cs)), Ursprung `ID_Stamm`
  ([`KatalogkostenUrsprungSchema.cs`](../../EPOS.Kern/Allgemein/Update/KatalogkostenUrsprungSchema.cs)), Saat
  [`ProzesstypSaat.cs`](../../EPOS.Kern/Allgemein/Update/ProzesstypSaat.cs). Der Rechenweg
  [`ProfilBedarf.cs`](../../EPOS.Kern/Allgemein/Simulation/ProfilBedarf.cs) ist datengetrieben: `ProfilQuelle` mit den
  Fabriken `Brauchwasser`, `Prozesswaerme`, `Strom` je `ProfilQuellmodus` (Projektrechnung, Projektvorschau,
  Katalogvorschau) — eine Fabrik `Kaelte(modus)` genügt.
- **Gebäudekühlung.** Die Kühlfelder stehen in
  [`GebaeudeKatalogDialog.razor`](../../EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor) in der Gruppe „Kühlung“, dazu
  Konditionierung (Kühlsollwertkalender) und Zonen. Der Projektschalter `Tab_Einstellungen.Kuehlbetrieb` steht in
  [`SimulationKonfigSeite.razor`](../../EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor) (K10/E27) und bleibt dort.
  Navigation: `INavigation.OeffneMaske(maske, args)` mit [`Masken.cs`](../../EPOS.Kern/Allgemein/Dienste/Masken.cs) und
  [`WinFormsNavigation.cs`](../../WindowsFormsApplication1/Dienste/WinFormsNavigation.cs); Sprungmarken in einen Dialog
  gibt es nicht.

### 2.2 Simulation

- [`SimulationKaeltebedarf.cs`](../../EPOS.Kern/Allgemein/Simulation/SimulationKaeltebedarf.cs) ist die Fassade neben
  `SimulationWaermebedarf`: Gebäude (`GebaeudeBuchen`) und Kühllastgänge (`GanglinieBuchen`, gerufen aus
  [`SimulationWaermebedarf.cs`](../../EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs)) gehen in den **einen**
  Kühlkanal `Kanal.KUEHLUNG`, in `Kaeltebedarf_Gebaeude`/`Kaeltebedarf_Extern` und in die Bedarfsprobe. Ohne
  `Kuehlbetrieb = 1` bleibt alles leer, der Lauf nennt die ungenutzten Lastgänge.
- Deckung: [`Kaeltekaskade.cs`](../../EPOS.Kern/Allgemein/Simulation/Kaeltekaskade.cs) und
  [`SimulationControl.Kaelte.cs`](../../EPOS.Kern/Allgemein/Simulation/SimulationControl.Kaelte.cs) — Folge `Tool_1` …
  `Tool_6` (freie Kühlung, Kältespeicher, Wärmepumpe im Kühlbetrieb, Kältemaschine), mit 212 über `Kaelte_Rang`. Ein
  Bedarfsvektor, keine Zuordnung Bedarf → Erzeuger; Kältestrom in `Stromverbrauch_Kuehlung_stuendlich`, abgerechnet über
  `Kuehl_ID_Carrier`/`Kuehl_EigenerZaehler` ([`KuehlungSchema.cs`](../../EPOS.Kern/Allgemein/Update/KuehlungSchema.cs)).
- Ergebnisse: [`KaeltegangReiter.razor`](../../EPOS.UI/Seiten/Simulation/KaeltegangReiter.razor),
  [`BedarfReiter.razor`](../../EPOS.UI/Seiten/Simulation/BedarfReiter.razor) (Kältelast als eigenes Bild),
  `Tab_ErgebnisKaeltemaschine`, Bericht [`KaelteProduktionBild.cs`](../../EPOS.Kern/Allgemein/Bericht/KaelteProduktionBild.cs).

## 3. Zielbild

### 3.1 Reiter „Kältebedarf“

Siebter Reiter `Reiterschluessel.Kaeltebedarf = "KAELTEBEDARF"`, eingereiht **hinter „Wärmebedarf“** (Bedarf Wärme → Kälte
→ Strom, dann Erzeuger; offener Punkt OP-1). Neuer Baustein `EPOS.UI/Seiten/Start/KaeltebedarfReiter.razor` nach dem
Muster `WaermebedarfReiter` (Kacheln, Tippkasten, „Weiter“). Drei neue Kachelschlüssel, `Kachelschluessel.Anzahl` 21 → 24:

| Schlüssel | Titel de / en (Vorschlag) | Beschreibung de / en (Vorschlag) | Bild | Statuspunkt grün, wenn … |
|---|---|---|---|---|
| `KAELTEBEDARF_EXTERN` | „Kältebedarf extern“ / „External cooling demand“ | „Kältelastgang als CSV einlesen und zuordnen“ / „Import and assign a cooling load profile (CSV)“ | neues Bild (Ableitung von `PStromMessdaten.jpg` mit Kältefarbe) | mindestens eine `Z_ProjektWaermebedarf`-Zeile mit Kanal „Kuehlung“ (neues Bit 8192) |
| `KAELTEBEDARFSPROFILE` | „Kältebedarfsprofile“ / „Cooling demand profiles“ | „Kältebedarf aus Typprofil, Jahressumme und Kalender“ / „Cooling demand from type profile, annual total and calendar“ | neues Bild (Ableitung von `PStdLastProfil.jpg`) | mindestens eine Zeile in `Z_Projekt_Kaeltebedarf` (neues Bit 16384) |
| `GEBAEUDEKUEHLUNG` | „Gebäudekühlung“ / „Building cooling“ | „Kühlung der Projektgebäude einstellen“ / „Set up cooling of the project buildings“ | `PKuehlung_Symbol.png` | mindestens ein Projektgebäude mit `Kuehlung_Aktiv = 1` (ohne Bit, Zustand über eine Hülle nach `KuehlungKachelBau`) |

- **Statuspunkte.** `KomponentenBestandCtrl` bekommt zwei Einträge mit den Bitwerten 8192 und 16384, `SeitenIndex =
  OHNE_SEITE` (der Projektassistent bekommt keine Kälteseite; `ANZAHL` 13 → 15). Die Gebäudekühlung braucht kein Bit,
  weil sie keine eigene Komponente ist, sondern ein Merkmal der Gebäude.
- **Hinweiszeile im Reiter.** Steht `Tab_Einstellungen.Kuehlbetrieb` auf 0, zeigt der Reiter über den Kacheln eine
  Herleitungszeile „Das Projekt rechnet keine Kälte — einschalten in der Simulationskonfiguration“ mit Sprung dorthin. Der
  Schalter selbst bleibt in der Konfiguration (K10/E27).
- **Hülle.** `StartseiteHuelle.Kacheln()` liefert die drei Kacheln, `Kachelweg` verteilt sie in drei neue Handler
  (`KaeltebedarfExtern`, `Kaeltebedarfsprofile`, `Gebaeudekuehlung`). iOS bekommt sie über `IProjektQuelle.Startkacheln`
  mit; die Handler der iOS-Hülle öffnen dieselben Razor-Dialoge.
- **Hilfe.** Neuer Schlüssel `Form_Start.btn_Help_Kaeltebedarf = Kältebedarf` in `help_mapping.txt`, Wiki-Seite
  „Kältebedarf“ (Abschnitt 7, Stufe K6).
- **Ressourcen** beider Sprachen: Reitertitel, drei Titel, drei Beschreibungen, Tippkasten, Hinweiszeile; danach
  `Werkzeuge/ResourceDesigner` ziehen.

### 3.2 Dialog „Kältebedarf extern“

Kein neuer Dialog: `WaermebedarfExternDialog` bekommt einen Parameter `Modus` mit den Werten `Alle` (Vorgabe, wie heute)
und `NurKuehlung`.

- **Im Modus `NurKuehlung`** zeigt der Projektbereich nur die Zeilen mit Kanal „Kuehlung“, die Kanalwahl ist fest auf
  „Kühlung“ und ausgeblendet, Titel und Kopfleiste heißen „Kältebedarf extern“. Der Katalog (`Tab_Waermebedarf_STAMM`)
  bleibt derselbe; die Katalogliste filtert nicht nach Kanal, weil ein Katalogsatz keinen Kanal trägt.
- **Merge-Schreibweg.** Der Speicherweg der Hülle liest vor dem Schreiben alle Zeilen des Projekts, ersetzt nur die Zeilen
  des eigenen Modus und schreibt die Wärmezeilen unverändert zurück. Besser noch: `WizardCtrl` bekommt
  `Del_WaermebedarfExtern(projektID, kanal)` mit Kanalfilter (`DELETE … WHERE ID_Projekt = ? AND Kanal = ?`), und der
  Modus `NurKuehlung` löscht und schreibt nur seine Kanalzeilen. Ein Test hält: eine Speicherung im Modus `NurKuehlung`
  lässt jede Wärmezeile (ID, Ganglinie, Bezeichner, Kanal) unverändert. Im Modus `Alle` ändert sich nichts.
- **Katalogauswahl V1.** Der Dialog folgt dem Gerüst „Gerahmt und gestapelt“ der Stufe 4 des
  [Katalogauswahlkonzepts](Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md) (Zeitreihen); die Ganglinie steht in der
  Detailzeile „gewählter Satz“ als Kälteganglinie (Kältefarbe, Einheit kW).
- **Deckungsart** steht als Feldgruppe in der Detailzeile der Projektzeile (3.4).

### 3.3 Dialog „Kältebedarfsprofile“

Neue Bedarfsart `BedarfsArt.Kaelte` im `BedarfsProfileDialog`: Katalog, Typkatalog, Projektbereich wie bei Prozesswärme.

- **Kopf** (`Tab_Kaeltebedarf_STAMM`): Bezeichner, Typ (Typprofil), Beschreibung, Monatsanteile `Monat_1` … `Monat_12`,
  Vorlauf/Rücklauf als **Angabe** (Herleitungszeile: „Angabe ohne Wirkung auf die Erzeuger“, E-K3), `ReadOnly`.
- **Typkatalog** (`Tab_Kaeltetyp_STAMM`): 168 Wochenstunden wie `Tab_Prozesstyp_STAMM`. Neutrale Saat
  (`KaeltetypSaat` nach `ProzesstypSaat`, `ReadOnly = 1`, wiederholbar, nie überschreibend), je Satz Typ und Kopf:

| Satz | Wochenbild (Typ) | Monatsgang (Kopf) | Vorlauf/Rücklauf (Angabe) |
|---|---|---|---|
| Raumkühlung Büro | Mo–Fr 7–19 Uhr, Spitze nachmittags, Wochenende 0 | Mai–September, Spitze Juli/August | 16/19 °C |
| Raumkühlung Handel | Mo–Sa 8–21 Uhr, Sonntag 0 | April–Oktober, flacher als Büro | 16/19 °C |
| Prozesskälte Dauerlast | 168 Stunden gleich | zwölf Monate gleich | 6/12 °C |
| Kühlraum | 168 Stunden, tags leicht erhöht (Türöffnungen) | leichter Sommeranstieg | −2/+4 °C |
| Tiefkühlraum | 168 Stunden, tags leicht erhöht | leichter Sommeranstieg | −30/−24 °C |
| Serverraum | 168 Stunden gleich | zwölf Monate gleich | 18/24 °C |

  Die Werte sind Formangaben ohne Produktbezug; die Zahlenreihen legt die Welle K1 fest und hält sie mit einer Saatwache.
- **Projektzeile** (`Z_Projekt_Kaeltebedarf`): Jahressumme `Summe` [MWh], Betriebskalender `ID_Betriebskalender` wie
  Prozesswärme (Feiertage, Ferien), Deckungsart (3.4). Vorschau über `BedarfsVorschauCtrl` mit `ProfilQuelle.Kaelte`:
  Wochenbild, Monatsbalken und Jahresreihe in Kältefarbe.
- **Verwaltung**: `BedarfAdminDialog` bekommt die Art Kälte (Katalog und Typkatalog pflegen, Rückweg Projekt → Katalog nach
  der Regel des Katalogauswahlkonzepts, Abschnitt 5.2).

### 3.4 Deckungsart je Zuordnung

Eine Feldgruppe „Deckung“ in beiden Dialogen, an der Projektzeile (Detailzeile bzw. Bearbeiten-Überlagerung):

| Feld | Werte | Vorgabe | Sichtbar |
|---|---|---|---|
| Deckungsart | „zentral (Kältefolge)“ / „dezentral (Split)“ | zentral | immer |
| EER-Weg | „fest“ / „linear über der Außentemperatur“ | fest | bei dezentral |
| EER (fest) bzw. EER am Punkt 1, Außentemperatur Punkt 1 | EER > 0; −20 … 50 °C | 3,0 · 35 °C | bei dezentral |
| EER am Punkt 2, Außentemperatur Punkt 2 | EER > 0; −20 … 50 °C, T₂ ≠ T₁ | 4,0 · 25 °C | bei linear |
| Kühlträger | Stromträger des Projekts, leer = Stromträger des Projekts | leer | bei dezentral |
| Eigener Zähler | Haken | aus | bei dezentral |

- Die Vorgaben 3,0 und 4,0 sind runde Ordnungswerte für Kleinsplitgeräte, keine Produktwerte; die Herleitungszeile nennt
  sie als „Vorgabe, bitte mit Gerätedaten ersetzen“.
- Bei „zentral“ sind die Split-Felder leer (NULL), nicht 0. Wechsel auf „zentral“ leert sie beim Speichern.
- Ein Hinweis unter der Gruppe: „Dezentral heißt: eigene Geräte ohne Kaltwasser, ohne Leistungsgrenze. Raumklimageräte
  mit Inneneinheiten je Zone folgen als eigene Anlagenart.“

### 3.5 Kachel „Gebäudekühlung“

- **Sprung.** Die Kachel öffnet den Projektgebäudedialog (`GebaeudeKatalogDialog` im Projektmodus) mit dem neuen Argument
  `Sprungziel = "KUEHLUNG"`: der Dialog wählt das erste Projektgebäude (bzw. das zuletzt gewählte), klappt die Gruppe
  „Kühlung“ auf und rollt sie in Sicht (`scrollIntoView` über ein kleines JS-Modul, Fokus auf das erste Feld). Die Konstante
  steht als `Sprungziele.Kuehlung` neben `Masken` im Kern; `OeffneMaske(Masken.…, args)` trägt sie. Ohne Projektgebäude
  öffnet der Dialog normal mit Banner „Noch kein Gebäude im Projekt“.
- **Zustandsanzeige** auf der Kachel (zweite Beschreibungszeile): „n von m Gebäuden gekühlt“, bei `Kuehlbetrieb = 0` dazu
  „Projekt rechnet keine Kälte“. Quelle ist eine plattformfreie Hülle `GebaeudekuehlungKachelBau` in `EPOS.UI.Daten`.
- Der Projektschalter `Kuehlbetrieb` bleibt in der Simulationskonfiguration.

## 4. Rechenweg

### 4.1 Profil → Stundenreihe

`ProfilBedarf.Rechnen(ProfilQuelle.Kaelte(ProfilQuellmodus.Projektrechnung), idProjekt, …)` rechnet wie Prozesswärme:
Wochenprofil (168) auf das Wochentagsraster der Klimaregion, Feiertage und Ferien über den Betriebskalender, Monatsanteile,
Normierung auf `Summe` je Zeile. Ergebnis je Zuordnungszeile eine Reihe von 8 760 Stundenwerten [kWh]. Kein neuer
Kalenderweg, keine neue Feiertagsregel.

### 4.2 Buchung

`SimulationKaeltebedarf` bekommt einen Eingang `BedarfBuchen(double[] werte, Kaeltedeckung deckung)` für Lastgang- und
Profilzeilen (der Lastgangweg `GanglinieBuchen` wird ihm vorgeschaltet):

- **zentral:** wie `GanglinieBuchen` heute — Kühlkanal, `Kaeltebedarf_Extern` (Lastgang) bzw. neu `Kaeltebedarf_Profil`
  (Profil), Bedarfsprobe. Die Kaskade deckt ihn in der Kältefolge.
- **dezentral (Split):** die Reihe geht in den Kühlkanal (der Kanal bleibt die Summe aller Kälte, Bedarfsprobe
  unverändert) **und** in den neuen Posten `Kaeltebedarf_Dezentral`. Die Kaskade bekommt **Kühlkanal minus
  `Kaeltebedarf_Dezentral`** — dieselbe Stelle, an der K-D `Kaeltebedarf_Raumgeraete` abzieht; beide Posten werden dort
  gemeinsam abgezogen, damit K-D ohne zweite Naht andockt. Die Deckungsprobe Kälte zählt `Kaeltebedarf_Dezentral` als
  Deckung.
- Lieferung je Stunde = Bedarf (keine Gerätegrenze, kein Rest).
- **Strom je Stunde:** `P_el(h) = Q_dez(h) / EER(θ_e(h))` mit
  - fest: `EER = Split_EER_1`;
  - linear: `EER(θ) = EER₁ + (EER₂ − EER₁) · (θ − T₁) / (T₂ − T₁)`, außerhalb [min(T₁,T₂), max(T₁,T₂)] auf den Randwert
    geklemmt, nie unter 1,0 (Untergrenze als Festwert `KaeltebedarfFestwerte.EER_MIN`).
  θ_e ist die Außenlufttemperatur der Klimaregion des Laufs (dieselbe Reihe wie Kältemaschine und Gebäude).
- Der Strom geht je Zeile mit ihrem Kühlträger und Zählerkennzeichen in `Stromverbrauch_Kuehlung_stuendlich` und damit in
  Strombilanz, Eigenverbrauch und Tarif — derselbe Weg wie Typ 13 (E34, E35): leer = Stromträger des Projekts, eigener
  Zähler = gesonderte Abrechnung.

### 4.3 Ergebnisgrößen

| Größe | Ort |
|---|---|
| `Kaeltebedarf_Dezentral` [kWh je Stunde], Jahressumme [MWh], Spitze [kW] | Fassade; Kältegang (eigene Fläche „dezentral (Split)“ im Deckungsbild), Bedarfsreiter (Kältelastbild unterscheidet Gebäude, Lastgang, Profil) |
| `Strom_Dezentral` [kWh je Stunde], Jahressumme, Jahres-EER = Σ Q / Σ P | Kältegang, Strombilanz (Posten „Kühlung dezentral“) |
| `Kaeltebedarf_Profil` Jahressumme | Bedarfsreiter, Bericht |
| neue Spalten an `Tab_ErgebnisEnergiebedarf` (Abschnitt 5) | Persistenz, Export, Variantenvergleich |
| Bericht | `KaelteProduktionBild` mit Fläche „dezentral (Split)“; Bedarfstafel mit Zeilen „Kältebedarf aus Profilen“ und „davon dezentral“ (`ProjektDetails`) |

Jede Kältezahl trägt die Grenze „nur sensible Kälte“ (`KAELTE_GRENZE_FEUCHTE`).

### 4.4 Grenzen und Verhalten ohne Kühlbetrieb

- Keine Gerätegrenze, keine Teillast, kein Takten, keine Latentlast, keine Rückwirkung auf Raumtemperatur oder
  Komfortkennzahlen (das leistet K-D für Zonen). Vorlauf/Rücklauf der Profile wirkt nicht (E-K3).
- **Ohne `Kuehlbetrieb = 1`** bleibt die Kälteseite leer wie heute: kein Kanal, kein Split-Strom; der Lauf nennt die Zahl
  ungenutzter Kältezeilen (Lastgänge und Profile getrennt) in derselben Meldung wie heute für Lastgänge.
- **Warnkriterien:** neues Kriterium „dezentrale Kälte ohne EER“ (Split-Zeile mit leerem EER → benannte Ablehnung beim
  Speichern, im Lauf Fehlertext), Hinweis „Jahres-EER unter 2“.

## 5. Schema 213

Eine Klasse `KaeltebedarfSchema` (Schritt `KaelteRangSchema.SCHRITT + 1`), eingetragen in `SchemaStand.Zielversion`, nach
[ADR-001](ADR-001_Schema-Ausrollung.md); alle neuen Tabellen `STRICT`, Boolean `INTEGER NOT NULL DEFAULT 0 CHECK (… IN
(0,1))`, Regeln aus [BETRIEB_SQLITE.md](BETRIEB_SQLITE.md) Abschnitt 6. Kein DML an Bestandszeilen außer der Saat.

| Tabelle | Spalten |
|---|---|
| `Tab_Kaeltebedarf_STAMM` | `ID`, `Bezeichner` TEXT NOT NULL, `Typ`, `Beschreibung`, `Monat_1` … `Monat_12` REAL, `Vorlauf`, `Ruecklauf` REAL nullbar (−40 … 100 °C, beide oder keiner, `Vorlauf ≤ Ruecklauf`), `ReadOnly` 0/1, Katalogspalten der Fassung wie die übrigen Bedarfskataloge (`Katalog_Schluessel`, `Katalog_Pruefsumme`, `Katalog_Ausgelaufen` 0/1, `Katalogfassung`); kein `ID_KostenVorlage` (kein Kostenkatalog) |
| `Tab_Kaeltetyp_STAMM` | `ID`, `Bezeichner` TEXT NOT NULL, `Beschreibung`, `"1"` … `"168"` REAL, `ReadOnly` 0/1, Katalogspalten der Fassung |
| `Tab_Kaeltebedarf` | Projektkopie des Kopfs: `ID`, `ID_Projekt` (FK, CASCADE), alle Kopfspalten, `ID_Stamm` INTEGER REFERENCES `Tab_Kaeltebedarf_STAMM` ON DELETE SET NULL |
| `Tab_Kaeltetyp` | Projektkopie des Typs: `ID`, `ID_Projekt`, `ID_Kaeltebedarf` (FK, CASCADE), `Typname`, `"1"` … `"168"` |
| `Z_Projekt_Kaeltebedarf` | `ID`, `ID_Projekt` (FK, CASCADE), `ID_Kaeltebedarf` (FK, CASCADE), `Bezeichner`, `Summe` REAL DEFAULT 0, `ID_Betriebskalender` (FK, SET NULL), Deckungsspalten (unten) |

**Deckungsspalten** — gleich an `Z_Projekt_Kaeltebedarf` (in `CREATE TABLE`) und an `Z_ProjektWaermebedarf` (per `ALTER TABLE
ADD COLUMN`, Prüfklauseln an der jeweils zweiten Spalte eines Paars):

| Spalte | Typ | Bedeutung |
|---|---|---|
| `Deckung` | TEXT NOT NULL DEFAULT 'zentral' CHECK (`Deckung` IN ('zentral','split')) | Deckungsart; an `Z_ProjektWaermebedarf` nur für Kanal „Kuehlung“ wirksam, für andere Kanäle steht stets 'zentral' (Prüfung im Controller, nicht im Schema, weil `Kanal` nullbar ist) |
| `Split_EER_Weg` | TEXT CHECK IN ('fest','linear') nullbar | leer bei zentral |
| `Split_EER_1`, `Split_Taussen_1` | REAL nullbar, EER > 0, −20 … 50 °C | fester EER bzw. Punkt 1 |
| `Split_EER_2`, `Split_Taussen_2` | REAL nullbar | Punkt 2, nur bei linear |
| `Kuehl_ID_Carrier` | INTEGER REFERENCES `energy_carrier` (`id`) ON DELETE SET NULL | leer = Stromträger des Projekts (Bauart aus `KuehlungSchema`) |
| `Kuehl_EigenerZaehler` | INTEGER NOT NULL DEFAULT 0 CHECK IN (0,1) | eigener Zähler |

**Ergebnisspalten** an `Tab_ErgebnisEnergiebedarf` (nullbar REAL): `Kaeltebedarf_Profil_MWh`, `Kaeltebedarf_Dezentral_MWh`,
`Kaelte_Dezentral_Spitze_kW`, `Strom_Dezentral_MWh`, `Jahres_EER_Dezentral`.

**Mitzuziehen im selben Schritt:** Saat `KaeltetypSaat` (sechs Sätze, Abschnitt 3.3), Katalogregister
([`KatalogRegistry.cs`](../../EPOS.Kern/Allgemein/Katalog/KatalogRegistry.cs), Verwendungsprüfung) und
[`Katalogfassung.cs`](../../EPOS.Kern/Allgemein/Katalog/Katalogfassung.cs) (zwei `Katalogtabelle`-Einträge nach dem Muster `PW`/`PWT`: Kopf mit Bezeichner, Typ, Beschreibung, zwölf Monaten, Vorlauf, Rücklauf; Typ mit 168 Stunden; Anzeigeschlüssel in beiden Sprachen; Prüfsumme ändert sich; Wachen
`KatalogfassungWache`, `AuslieferungsvorlagenWacheTests` nachziehen), `ProjektFremdschluessel`, `ProjektDuplizierenCtrl`,
`WizardCtrl` (Del/Add der neuen Zuordnung), `AssistentAbgleich`, `ProjektDetails`, `Senkenvorbelegung` (Kälte bleibt ohne
Senke), `Warnkriterien`, `EnergietraegerKatalogCtrl.Loeschen` (zählt die beiden neuen `Kuehl_ID_Carrier`-Spalten mit),
Projekttransfer und Projektpaket, `Werkzeuge/Testdatenbankschema/Program.cs` (Testdatenbank auf 213), `sql/schema`
(`001_grundschema.sql`, `inventar.json`, `typkatalog.json`), Ressourcen beider Sprachen, SQL-Dialekt-Prüfer.

**Byte-Gleichheit:** Die neuen Tabellen sind in den Referenzprojekten leer, kein Referenzprojekt erhält eine Zuordnung, die
Deckungsspalten an `Z_ProjektWaermebedarf` stehen auf 'zentral' — damit rechnet jedes Referenzprojekt Zeichen für Zeichen
wie zuvor; die neuen Ergebnisspalten bleiben leer, solange keine Profil- oder Split-Zeile rechnet (leer, nicht 0, damit der
Vergleich der Basis keine neuen Werte sieht).

## 6. Simulationskonfiguration

- Im Bereich „Kälte“ (Welle KB-B) eine Gruppe **„Dezentrale Kältedeckung“**, nur Anzeige: je dezentraler Zeile Bezeichner,
  Quelle (Lastgang/Profil), Jahressumme, EER-Weg, Kühlträger, Zähler; dazu Summe und Sprung „Bearbeiten…“ in den Dialog
  der Zeile. Sie entsteht erst nach dem Push von KB-B, weil dort die Sitzung Gebäudesimulation baut.
- Daneben eine Zeile „Kältebedarf zentral“ mit den Summen aus Gebäude, Lastgang und Profil — die Eingangsgröße der
  Kältefolge.
- **Anlagenschema:** ein Knoten „Kältebedarf dezentral (Split)“ als Senke ohne Leitung zur Kältebahn (`KERZEUGER_` …), mit
  eigener Stromlinie zum Kühlträger; ein Knoten „Kältebedarf Profile/Lastgänge“ an der Kältebahn wie der Gebäudeknoten.
- **Nicht hierher:** Kältefolge und `Kaelte_Rang` (KB-D), Typ 14 und Inneneinheiten (K-D), der Schalter `Kuehlbetrieb`
  (bleibt, wo er ist; die Gruppe liest ihn nur).

## 7. Stufenplan

| Stufe | Inhalt | Nachweis | Aufwand |
|---|---|---|---|
| **K0** | dieses Papier, Entscheide E-K1–E-K3 | Dokumentationswachen | 0,5 PT |
| **K1** | Schema 213 `KaeltebedarfSchema` mit Saat, Testdatenbank 213, `sql/schema`, Katalogregister und -fassung, Verwendungsprüfung | Schematests (Anlage, Wiederholbarkeit, CHECK-Klauseln, STRICT), Saatwache, `KatalogfassungWache`, `AuslieferungsvorlagenWacheTests`, SQL-Dialekt-Prüfer, Referenzlauf byte-gleich | 2–3 PT |
| **K2** | Kernwege der Bedarfsart Kälte: `BedarfsArt.Kaelte`, `KaeltebedarfStammCtrl`, `Z_ProjektKaeltebedarfCtrl`, Zweige in `BedarfStammCtrl`/`TypProfilCtrl`, `ProfilQuelle.Kaelte`, `BedarfsVorschauCtrl`; Projektkopie, Duplizieren, Fremdschlüssel, Abgleich, Transfer; `Del_WaermebedarfExtern` mit Kanalfilter | Controllertests je Weg, Duplizier- und Transfertests, Test „Kühlzeilen speichern lässt Wärmezeilen unverändert“, Referenzlauf byte-gleich | 3–4 PT |
| **K3** | Rechenweg: Profilbuchung zentral, Split-Buchung neben der Kaskade, Strom über Kühlträger und Zähler, Ergebnisspalten, Warnkriterien, Laufmeldungen | Rechentests (EER fest/linear, Klemmung, Summen, Deckungs- und Bedarfsprobe ohne Verletzung, Strom in der Strombilanz, ohne Kühlbetrieb leer), Referenzlauf byte-gleich, `ModultrennungswacheTests` | 3–4 PT |
| **K4** | Startseite: siebter Reiter, drei Kacheln, Bilder, Statusbits, Hinweiszeile, Hülle und iOS-Quelle, Hilfe, Ressourcen | `StartseiteTests`, `KachelbilderTests`, `StartseiteAnmutungTests`, Bitmaskentest des Komponentenbestands, Windows-Schale auf Linux kompiliert | 2–3 PT |
| **K5** | Dialoge: Modus `NurKuehlung` im Wärmebedarf-extern-Dialog, Bedarfsart Kälte im Bedarfsprofile- und Verwaltungsdialog, Feldgruppe „Deckung“, Sprungziel „Kühlung“ im Gebäudedialog | bunit-Dialogtests, Rasterprobe und `fensterprobe.mjs` (Katalogauswahl V1), KI-Feldkarte der neuen Felder | 4–5 PT |
| **K6** | Ergebnisse und Bericht (Kältegang, Bedarfsreiter, Strombilanz, `KaelteProduktionBild`, `ProjektDetails`, Export), Gruppe „Dezentrale Kältedeckung“ und Anlagenschema-Knoten (nach KB-B), Referenzprojekt mit Kälteprofil und Split-Zeile (nächste freie Nummer) mit neuer Basis, Wiki-Quelle „Kältebedarf“ und Logbuch-Entwurf | `ChartProben` (Messlatte neu, wo ein Bild die Split-Fläche trägt), `diagrammprobe.mjs`, `tabellenprobe.mjs`, Referenzlauf: alle bisherigen Projekte byte-gleich, neues Projekt in der neuen Basis mit Wache; `WikiProduktdatenWacheTests` | 3–4 PT |

Summe rund **18–23,5 PT**.

**Reihenfolge gegenüber den anderen Sitzungen:**

- K1 hängt an Schema 212 (KB-D) auf origin; vorher wird nicht gebaut.
- K5 setzt auf die Gerüste der Stufe 4 der Katalogauswahl auf (Wärmebedarf extern und Bedarfsprofile werden dort umgebaut);
  ist Stufe 4 nicht fertig, baut K5 die Felder in die heutige Detailzeile und zieht sie mit Stufe 4 um — teurer, deshalb
  Empfehlung: K5 nach Stufe 4.
- Die Konfigurationsgruppe in K6 erst nach dem Push von KB-B; K2–K4 sind davon frei und können parallel zu KB-B laufen.
- K-D (Typ 14) dockt an der Abzugsstelle aus 4.2 an; baut K-D zuerst, übernimmt K3 dessen Stelle statt sie anzulegen.

## 8. Einfrierregeln und offene Punkte

### 8.1 Einfrierregeln

Neue Saatdaten frieren erst dann eine Basis neu ein, wenn ein Referenzprojekt sie nutzt (Stufe K6). Ab dann gilt eine neue
Regel in `CLAUDE.md` und [Referenzlaeufe/LIESMICH.md](../../Referenzlaeufe/LIESMICH.md), Vorschlag:

- **gesäte Kältebedarfsdaten eines Referenzprojekts:** seine Zuordnungszeilen `Z_Projekt_Kaeltebedarf` (ID der
  Projektkopie, `Summe`, `ID_Betriebskalender`), die Projektkopien `Tab_Kaeltebedarf`/`Tab_Kaeltetyp` samt Monatsanteilen
  und 168 Wochenstunden, die Katalogzeilen der Saat, die sie benutzen, die Deckungsspalten (`Deckung`, `Split_EER_Weg`,
  `Split_EER_1`, `Split_Taussen_1`, `Split_EER_2`, `Split_Taussen_2`, `Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler`) an
  `Z_Projekt_Kaeltebedarf` und an den Kühlzeilen von `Z_ProjektWaermebedarf`, der Festwert `KaeltebedarfFestwerte.EER_MIN`,
  dazu das Anlegen oder Entfernen eines Referenzprojekts mit Kältebedarfsprofil oder dezentraler Deckung.

Die bestehenden Regeln „gesäte Kältedaten“ (Kanal „Kühlung“ eines Lastgangs) und „gesäte Bedarfsdaten“ bleiben; die
Deckungsspalte eines Kühllastgangs gehört ab K1 zur Regel „gesäte Kältedaten“.

### 8.2 Offene Punkte

| Nr. | Frage | Empfehlung |
|---|---|---|
| **OP-1** | Lage des Reiters: hinter „Wärmebedarf“ (Platz 3) oder hinter „Strombedarf“ (Platz 4)? | **hinter „Wärmebedarf“** — Wärme und Kälte sind die thermischen Bedarfe, der Assistent führt in derselben Folge; Tests auf Reiterindex werden nachgezogen |
| **OP-2** | Bleibt der Kanal „Kühlung“ im Dialog „Wärmebedarf extern“ (Modus `Alle`) wählbar, wenn es die Kachel „Kältebedarf extern“ gibt? | **ja, aber mit Hinweis** „Kältelastgänge pflegen Sie unter Kältebedarf extern“; ein Entzug würde bestehende Projekte im Wärmedialog unsichtbar machen |
| **OP-3** | Hilfsstrom des Split (Ventilatoren, Bereitschaft) als eigenes Feld? | **nein** in K3 — der EER des Datenblatts schließt die Ventilatoren ein; Bereitschaft kommt mit K-D (Typ 14) |
| **OP-4** | Kosten der dezentralen Geräte in der Wirtschaftlichkeit (Investition, Wartung)? | **nicht in K1–K6**; die Wirtschaftlichkeit zählt den Strom über Träger und Zähler. Gerätekosten gehören an die Anlage Typ 14; wer sie braucht, rechnet mit K-D |
| **OP-5** | Kältebedarfsprofile auf iOS: Profilpflege mit 168-Stunden-Raster auf dem kleinen Schirm? | **gleicher Dialog wie Prozesswärme** auf iOS (kein eigener Weg, E31 K12); kein iOS-Lauf für K1–K6, weil die iOS-Hülle selbst nicht berührt wird |
