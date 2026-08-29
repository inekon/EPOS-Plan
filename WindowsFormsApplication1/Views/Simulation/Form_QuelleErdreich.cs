using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Quellendialog "Erdreich" einer Wärmepumpe (Sole-Wasser / Wasser-Wasser),
    /// Aufbau nach Konzept-Mockup 4.5.
    ///
    /// Erdkollektor - Verlegetiefe und Fläche; die Quelltemperatur folgt dem
    /// gedämpften und phasenverschobenen Jahresgang nach Kusuda.
    /// Erdsonde - Länge je Sonde und Anzahl; die Quelltemperatur ist konstant,
    /// weil der Jahresgang ab der neutralen Zone abgeklungen ist.
    ///
    /// Der 8760er-Außentemperaturvektor wird vom Aufrufer übergeben und beim
    /// Öffnen einmal verwendet - die Vorschau rechnet bei Parameteränderungen nur
    /// noch aus dem gecachten Vektor, ohne erneuten Datenbankzugriff (Konzept 4.5).
    ///
    /// Die Auslegungsprüfung nach VDI 4640 Blatt 2 braucht Simulationsergebnisse.
    /// Sie kommen entweder vom Aufrufer (Ergebnisse eines früheren Laufs der Sitzung)
    /// oder aus einem Lauf, den der Anwender hier selbst anstößt - Schaltfläche
    /// „Simulation", siehe <see cref="btnSimulation_Click"/>. Liegt beides nicht vor,
    /// bleibt die Prüfung leer und sagt das an. Stehen die Eingaben nicht mehr auf dem
    /// Stand, mit dem gerechnet wurde, warnt eine Hinweiszeile
    /// (<see cref="AenderungshinweisAktualisieren"/>).
    ///
    /// Die Oberfläche steht seit der Designer-Umstellung in
    /// <c>Form_QuelleErdreich.Designer.cs</c>, weiterhin ohne eigene <c>.resx</c>: Alle
    /// sichtbaren Texte kommen aus dem Ressourcenkatalog
    /// (<c>MyResource.Resource.SIMQ_ERDREICH_*</c>, Konzept 13.6) und werden in
    /// <see cref="TexteSetzen"/> gesetzt. Im Designer stehen seit der Design-Politur vom
    /// 21.08.2026 die DEUTSCHEN Fassungen derselben Ressourcen (vorher der Feldname als
    /// Platzhalter) — allein damit die Entwurfsfläche zeigt, was der Anwender sieht;
    /// maßgeblich bleibt <see cref="TexteSetzen"/>, das jeden dieser Texte beim Öffnen in
    /// der eingestellten Sprache überschreibt. Bodentyp-Schlüssel und Quellsystem bleiben
    /// deutsche Persistenzwerte aus <see cref="DbWerte"/> (Drei-Schichten-Regel).
    ///
    /// Nicht im Designer und deshalb im Konstruktor-Nachlauf:
    ///   • das Vorschau-Diagramm (<see cref="ChartAufbauen"/>) - Migrationsregel 8,
    ///     die <c>Chart</c>-Serialisierung des portierten
    ///     <c>WinForms.DataVisualization</c> ist unter VS 2022/.NET 8 unzuverlässig;
    ///   • die Katalog- und Klimazonenlisten (<see cref="KatalogeFuellen"/>) - beides
    ///     sind Laufzeitdaten aus <c>ErdreichTemperatur</c> bzw. <c>VDI4640Pruefung</c>;
    ///   • die kulturabhängigen Vorgabewerte (<see cref="VorgabenSetzen"/>);
    ///   • das gemessene Spaltenraster der Quellsystem-Rubrik
    ///     (<see cref="QuellsystemRasterAusrichten"/>) - der Designer kann nur EINE
    ///     Sprache abbilden, die Spaltenkanten hängen aber an der Textbreite.
    /// </summary>
    public partial class Form_QuelleErdreich : Form
    {
        // ---- Übergabefelder (öffentlich, wie im Bestandsmuster) -----------

        /// <summary>Name der Wärmepumpe (nur für den Fenstertitel).</summary>
        public string WPName = "";

        /// <summary>
        /// Tab_Projekt.ID des Projekts, zu dem diese Wärmequelle gehört. Wird nur für
        /// die Schaltfläche „Simulation" gebraucht (Befund 3 vom 17.08.2026): Ohne
        /// Projektbezug gibt es keinen Lauf, den der Dialog anstoßen könnte.
        ///
        /// 0 = nicht gesetzt. Dann versucht <see cref="ProjektErmitteln"/> den Bezug
        /// über das besitzende Formular zu bestimmen; siehe dort, warum der Aufrufer
        /// dieses Feld derzeit nicht selbst belegt.
        /// </summary>
        public int ID_Projekt = 0;

        /// <summary>
        /// Tab_Energieanlagen.ID der Wärmepumpe (Muster <c>Form_Waermesenke.ID_Anlage</c>).
        /// Sie ordnet die Ergebnisse eines Laufs eindeutig dieser Anlage zu
        /// (<see cref="ErdreichAuswertung.AnlageErgebnis.ID_Anlage"/>).
        ///
        /// 0 = nicht gesetzt. Dann fällt <see cref="ErgebnisDesLaufs"/> auf den
        /// Modulnamen und, wenn das Projekt nur eine Erdreichquelle führt, auf deren
        /// einziges Ergebnis zurück.
        /// </summary>
        public int ID_Anlage = 0;

        /// <summary>Quellsystem: ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR | _SONDE.</summary>
        public string Quellsystem = ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR;

        /// <summary>Verlegetiefe des Kollektors bzw. Länge je Sonde [m].</summary>
        public double Tiefe = ErdreichTemperatur.TIEFE_DEFAULT;

        /// <summary>Kollektorfläche [m²].</summary>
        public double Flaeche = 0;

        /// <summary>Anzahl Sonden.</summary>
        public int Anzahl = 1;

        /// <summary>Katalogschlüssel des Bodentyps (VDI 4640 Blatt 1).</summary>
        public string Bodentyp = ErdreichTemperatur.BODENTYP_DEFAULT;

        /// <summary>Klimazone 1…15 nach DIN 4710; 0 = nicht zugeordnet.</summary>
        public int Klimazone = 0;

        /// <summary>
        /// Nutzbare Spreizung der Quelle [K] (WQ_Spreizung). Sie ist die Temperatur-
        /// differenz zwischen Quelleintritt und -austritt und geht in die zweite
        /// Warnbedingung aus Konzept 13.1 ein: gewarnt wird, wenn
        /// „Quelltemperatur − Spreizung" dauerhaft unter 0 °C liegt.
        ///
        /// Bis Paket 7 war der Wert nur über den Pufferspeicher-Quellendialog pflegbar -
        /// bei einer Erdreichquelle gab es gar keine Eingabemöglichkeit und die Prüfung
        /// rechnete immer mit der Vorgabe von 5 K.
        /// </summary>
        public double Spreizung = ErdreichAuswertung.SPREIZUNG_DEFAULT;

        /// <summary>
        /// Außentemperatur der Klimaregion (8760 Stundenwerte). Wird vom Aufrufer
        /// gesetzt; fehlt der Vektor, rechnet das Modell mit Ersatzwerten weiter.
        /// </summary>
        public float[] Aussentemperatur = null;

        // ---- Ergebnisse eines Simulationslaufs (Auslegungsprüfung) --------

        /// <summary>true, wenn Ergebnisse eines Simulationslaufs vorliegen.</summary>
        public bool ErgebnisseVorhanden = false;

        /// <summary>Maximale Entzugsleistung der Quelle [W].</summary>
        public double MaxEntzugW = 0;

        /// <summary>Jahresentzugsarbeit der Quelle [kWh/a].</summary>
        public double JahresentzugKWh = 0;

        /// <summary>Jahresvolllaststunden der Wärmepumpe [h/a].</summary>
        public double VolllastStunden = 0;

        /// <summary>
        /// Grund, aus dem die Prüfung nicht mit Ergebnissen versorgt werden konnte
        /// (Paket 7): entweder „noch kein Lauf" oder die Grenze der Zuordnung
        /// (mehrere Wärmepumpen mit unterschiedlichen Quellen). Leer = Vorgabetext.
        /// </summary>
        public string HinweisErgebnis = "";

        /// <summary>
        /// Vorbehalt zu belastbaren Ergebnissen (z. B. „Spitze anteilig aus der
        /// Summenganglinie geschätzt"). Wird unter die Prüfung geschrieben.
        /// </summary>
        public string HinweisVorbehalt = "";

        /// <summary>
        /// Meldung der zweiten Warnbedingung (Konzept 13.1) samt Normbasis. Steht
        /// bewusst getrennt vom Prüfergebnis: „Grenzwert eingehalten" und eine
        /// Frostmeldung schließen einander nicht aus, weil VDI 4640 Bl. 2 gegen
        /// −5 °C Soleaustritt bemisst.
        /// </summary>
        public string HinweisFrost = "";

        // ---- Steuerelemente -----------------------------------------------
        //
        // Alle übrigen Steuerelemente sind Designer-Felder und stehen in
        // Form_QuelleErdreich.Designer.cs. Nur das Vorschau-Diagramm bleibt hier:
        // Es wird nach Migrationsregel 8 nicht serialisiert, sondern in
        // ChartAufbauen() erzeugt - ein Feld bleibt es trotzdem, weil
        // Aktualisieren() bei jeder Eingabe darauf zugreift.

        private Chart _chart;

        private bool _uiAufbau = true;   // unterdrückt Ereignisse während SetControls

        /// <summary>
        /// Zustand der Eingabefelder, wie ihn <see cref="SetControls"/> vorgefunden hat -
        /// also der Stand, der in der Datenbank steht (Befund 4 vom 17.08.2026).
        ///
        /// Er ist die Bezugsgröße für zwei Aussagen, die der Dialog treffen muss:
        ///   • Weicht der aktuelle Stand ab, beruht die Auslegungsprüfung auf ANDEREN
        ///     Werten als den angezeigten - dann muss gewarnt werden.
        ///   • Ein Lauf, den die Schaltfläche „Simulation" anstößt, rechnet mit den
        ///     GESPEICHERTEN Werten (die Engine liest WQ_* aus der Datenbank, nicht aus
        ///     diesem Dialog). Auch das muss der Hinweis sagen können.
        ///
        /// Verglichen wird der Text der Steuerelemente, nicht der geparste Zahlenwert:
        /// Der Text ist genau das, was der Anwender sieht, und <see cref="SetControls"/>
        /// hat ihn aus den Datenbankwerten erzeugt - beide Seiten sind damit identisch
        /// formatiert. Stellt der Anwender einen geänderten Wert wieder auf den
        /// Ausgangswert zurück, verschwindet der Hinweis von selbst.
        /// </summary>
        private string _standGeladen = "";

        /// <summary>
        /// true, sobald in diesem Dialog ein Lauf über die Schaltfläche „Simulation"
        /// durchgelaufen ist. Nur dann darf der Änderungshinweis auf „der Lauf hat mit
        /// den gespeicherten Werten gerechnet" umschalten - vorher wäre die Aussage
        /// falsch, weil der letzte Lauf dann von woanders kommt.
        /// </summary>
        private bool _laufAusDialog = false;

        // --- Technische Serienschlüssel (Paket 9 / L7) --------------------------------
        // Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII, unveränderlich.
        // Der Anzeigetext steht ausschließlich in Series.LegendText.
        private const string S_QUELLTEMPERATUR = "QUELLTEMPERATUR";
        private const string S_AUSSENTEMPERATUR = "AUSSENTEMPERATUR";

        public Form_QuelleErdreich()
        {
            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Designer-Umstellung
            // wurde AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls
            // keine Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();
            // Erst NACH TexteSetzen(): Beschriftungen und Auswahlknöpfe der
            // Quellsystem-Rubrik tragen bis dahin nur den DEUTSCHEN Entwurfstext und
            // sind in jeder anderen Sprache falsch breit (Muster
            // Form_QuellePufferspeicher.EingabespalteAusrichten).
            QuellsystemRasterAusrichten();
            // Diagramm zuerst: Aktualisieren() greift auf _chart.Series zu, und die
            // beiden folgenden Schritte lösen über die Designer-Ereignisse bereits
            // Aufrufe aus (die _uiAufbau zwar abfängt - aber die Reihenfolge soll
            // auch ohne diese Sperre tragfähig bleiben).
            ChartAufbauen();
            KatalogeFuellen();
            VorgabenSetzen();

            // Bereich für den KI-Hilfe-Assistenten melden (nur Bedien-Kontext,
            // keine Projekt- oder Kundendaten). Muster und Platz wie am Ende des
            // Konstruktors von Form_Simulation_Config und Form_Simulation_Detail:
            // ein reiner Kontext-Setzer am Activated-Ereignis, keine eigene
            // Hilfe-Schaltfläche - die Hilfe wird im Hauptfenster geöffnet
            // (MDIMainForm -> Form_KiChat.Oeffnen) und holt sich den Bereich dort ab.
            //
            // Der Bereichsname ist bewusst ein deutsches Literal und KEIN
            // Ressourcenschlüssel: Er ist kein sichtbarer Text, sondern Eingabe an den
            // Assistenten (HilfeKontext.Beschreibung), und beide Bestandsaufrufe halten
            // es genauso. Genannt werden die drei Dinge, nach denen der Anwender in
            // dieser Maske fragen kann.
            //
            // Die Lambda bleibt eine Lambda: Migrationsregel 5 gilt dem Parser des
            // Designers und damit ausschließlich InitializeComponent - hier im
            // Konstruktor-Nachlauf sieht der Designer sie nie.
            this.Activated += (s, e) =>
                HilfeKontext.SetzeBereich("Wärmequelle Erdreich (Quellsystem, Bodentyp, Auslegungsprüfung VDI 4640)");

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        /// <summary>
        /// Zahlenwert als Feldvorbelegung — kulturneutral im Quelltext, formatiert wie
        /// alle übrigen Ausgaben dieses Dialogs (<c>ToString("0.##")</c>). Gelesen wird
        /// über <see cref="WaermequelleClass.ZahlParsen"/>, das Komma UND Punkt annimmt;
        /// <c>CurrentCulture</c> wird nicht gesetzt (Konzept 13.6). Bis Paket 9 stand
        /// hier die Zeichenkette „1,5" mit hartkodiertem Dezimalkomma. Muster aus L3
        /// (<see cref="Form_Quellprofil"/>).
        /// </summary>
        private static string Vorgabe(double wert)
        {
            return wert.ToString("0.##", CultureInfo.CurrentCulture);
        }

        // ==================================================================
        // Oberfläche — gerettete Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen seit der Designer-Umstellung in
        // Form_QuelleErdreich.Designer.cs. Designer-Code trägt keine Kommentare; die
        // Pixelentscheidungen aus den Abnahmebefunden stehen deshalb hier. Die Befunde
        // 1, 3 und 4 stammen vom 17.08.2026.
        //
        // * ClientSize 700 x 748. Die Höhe ist um eine Zeile gewachsen, weil die
        //   nutzbare Spreizung ein Eingabefeld braucht (Konzept 13.1, _tbSpreizung).
        //   BEFUNDE 1/3/4: Die Höhe wächst von 718 auf 748. Dazu gekommen sind rund
        //   56 Pixel:
        //     +14  die Bodenkennwerte brauchen zwei Zeilen (siehe _lblBoden),
        //     + 8  der Spreizungs-Hinweis bricht um (siehe _lblSpreizungHinweis),
        //     +38  die Auslegungsprüfung bekommt Hinweiszeile und Schaltfläche.
        //   Gegengerechnet sind 26 Pixel aus der Vorschau: Das Diagramm ist von 210
        //   auf 184 Pixel Höhe verkleinert (siehe ChartAufbauen).
        //   184 ÜBERHOLT durch die Nacharbeit zur Design-Politur, siehe unten: 170;
        //   die Gegenrechnung wächst damit von 26 auf 40 Pixel. Das ist Absicht und
        //   der Preis dafür, dass der Dialog NICHT über die Fensterhöhe hinauswächst,
        //   die Windows auf einem 1366×768-Gerät noch zulässt (dort endet die
        //   zulässige Fensterhöhe bei etwa 788 Pixeln; mit Titelzeile und Rahmen liegt
        //   dieser Dialog bei 787). Ohne die Gegenrechnung wären OK und Abbrechen dort
        //   unter dem unteren Bildschirmrand verschwunden - ein schlechterer Fehler als
        //   der, der hier behoben wird.
        //   Die Breite bleibt bei 700; alle Beschriftungen passen hinein (nachgemessen
        //   für Deutsch und Englisch, siehe _lblBoden und _lblSpreizungHinweis).
        //
        // * _lblBoden (28/170, 660 x 32) ist die KENNWERTZEILE des Bodens, nicht die
        //   Beschriftung „Bodentyp:" - die heißt _lblBodentyp (28/145).
        //   BEFUND 1 („Text nicht sichtbar"): Die Kennwertzeile stand in einem 530 Pixel
        //   breiten Feld ab x=150 und wurde hart abgeschnitten - gemessen brauchte sie
        //   635 Pixel, sichtbar endete sie mitten in „Bodenart nach Tabelle A1: …".
        //   Zwei Änderungen beheben das:
        //     • Sie beginnt am linken Rand (x=28) und nutzt die volle Breite von
        //       660 Pixeln statt 530.
        //     • Sie bekommt Platz für ZWEI Zeilen (32 statt 18 Pixel). Nötig ist das
        //       für den längsten Fall: Mit der Bodenart „Sandiger Ton" statt „Sand"
        //       wächst der Text auf rund 683 Pixel und läuft damit auch über die volle
        //       Breite hinaus. Bei kurzen Bodenarten bleibt es optisch eine Zeile -
        //       AutoSize=false bricht nur um, wenn es sein muss.
        //
        // * Ab _lblKlimazone (y=212) liegt jede Zeile 14 Pixel tiefer als vor Befund 1 -
        //   genau die zweite Zeile, die _lblBoden dazubekommen hat.
        //
        // * _lblSpreizung / _tbSpreizung (28/242 und 150/239): Eingangsgröße der zweiten
        //   Warnbedingung (Konzept 13.1). Ohne dieses Feld war WQ_Spreizung bei einer
        //   Erdreichquelle nicht pflegbar und die Prüfung rechnete immer mit 5 K.
        //   x = 150 ÜBERHOLT durch die Design-Politur 21.08.2026, siehe unten: 170.
        //
        // * _lblSpreizungHinweis (232/242, MaximumSize 456 x 0, AutoSize=true).
        //   Position und MaximumSize ÜBERHOLT durch die Design-Politur: 252, 436.
        //   BEFUND 1 - der Hauptbefund. Der Hinweis ist gemessen 564 Pixel breit, begann
        //   bei x=232 und endete damit bei 796 - also 96 Pixel HINTER dem rechten
        //   Dialogrand (700). Sichtbar brach er mitten in „…Quelltemperatur − Spreizung
        //   dauerh" ab.
        //   Er bleibt an seinem Platz hinter dem Eingabefeld (das ist die Zuordnung, die
        //   der Anwender erwartet) und darf UMBRECHEN: MaximumSize begrenzt die Breite
        //   auf die 456 Pixel, die bis zum rechten Rand frei sind, AutoSize lässt ihn
        //   dafür in die Höhe wachsen. Das ist das MaximumSize/AutoSize-Muster und die
        //   kleinstmögliche Änderung - Position und Reihenfolge der Steuerelemente
        //   bleiben, wie sie waren.
        //   WICHTIG bei der Designer-Pflege: MaximumSize muss gesetzt sein, BEVOR der
        //   echte Text ankommt. Das ist hier gesichert, weil im Designer nur der
        //   Platzhalter steht und TexteSetzen() erst nach InitializeComponent läuft.
        //   Deutsch und Englisch belegen zwei Zeilen (rund 30 Pixel). Bis zur
        //   Vorschau-Gruppe (y=280) sind ab y=242 aber 38 Pixel frei, und die Gruppe
        //   beginnt mit 20 Pixeln Rahmen - Reserve für längere Übersetzungen.
        //
        // * _gbPruefung (12/532, 676 x 168) ist von 130 auf 168 Pixel gewachsen: unter
        //   das Prüfergebnis kommen die Hinweiszeile (Befund 4) und die Schaltfläche
        //   „Simulation" (Befund 3) - beide gehören sachlich hierher und nirgends
        //   sonst hin.
        //   Lage und Höhe ÜBERHOLT durch die Nacharbeit zur Design-Politur, siehe
        //   unten: 12/518, 676 x 182.
        //
        // * _lblAenderung (14/128, 500 x 34).
        //   Höhe 34 ÜBERHOLT durch die Nacharbeit zur Design-Politur, siehe unten: 48.
        //   BEFUND 4: Sobald der Anwender eine Quell-Einstellung ändert, zeigt die
        //   Prüfung oben noch den Stand des LETZTEN Laufs. Ohne Hinweis liest sich das
        //   wie eine Bewertung der neuen Eingaben - sie ist es aber nicht. Die Zeile
        //   bleibt leer, solange Anzeige und Lauf zusammenpassen; welchen der beiden
        //   Texte sie sonst zeigt, entscheidet AenderungshinweisAktualisieren.
        //   AutoSize=false mit zwei Zeilen Höhe: der Text bricht dann von selbst um und
        //   schiebt die Schaltfläche daneben nicht weg (Lehre aus Befund 1).
        //   „Zwei Zeilen" ÜBERHOLT: der zweite der beiden Texte braucht auf Deutsch
        //   DREI Zeilen, siehe unten.
        //   Die ForeColor 160/96/0 ist die WARNFARBE - derselbe Bernsteinton, den
        //   Form_GanglinieProtokoll für PruefStufe.Warnung verwendet, bewusst NICHT das
        //   Firebrick der Grenzwertüberschreitung (PruefungAktualisieren): Ein
        //   veralteter Prüfstand ist ein Bedienhinweis, keine überschrittene Norm.
        //   Sie stand bis zur Designer-Umstellung als Konstante FARBE_WARNUNG im Code
        //   und ist jetzt Designer-Eigenschaft von _lblAenderung.
        //
        // * _btnSimulation (528/126, 134 x 28).
        //   BEFUND 3 („Simulation nur für diesen Bereich"): Die Prüfung war bisher nur
        //   zu füllen, indem der Anwender den Dialog verließ und den großen
        //   Simulationsweg ging. Der Knopf rechnet sie hier - was genau er tut und was
        //   er bewusst NICHT tut, steht bei btnSimulation_Click.
        //
        // * _btnOk (510/712) und _btnAbbruch (603/712) standen im Bestand als
        //   ClientSize.Width - 190 bzw. - 97; bei ClientSize.Width = 700 sind das genau
        //   diese beiden Werte. ÜBERHOLT durch die Design-Politur, siehe unten.
        //
        // ==================================================================
        // DESIGN-POLITUR 21.08.2026
        // ==================================================================
        //
        // Anlass: Im Designer standen bis dahin die Feldnamen als Platzhalter. Mit den
        // ECHTEN Texten in der Entwurfsfläche fiel eine Überdeckung auf, die kein
        // Platzhalter zeigen konnte. Alle Maße unten sind mit TextRenderer in beiden
        // Sprachen nachgemessen; ClientSize bleibt bei 700 x 748.
        //
        // * DIE EINGABESPALTE RÜCKT VON x = 150 AUF x = 170 — der eigentliche Befund.
        //   „Nutzbare Spreizung [K]:" ist 133 px breit und endete ab x = 28 bei 161,
        //   also 11 px HINTER dem Eingabefeld, das bei 150 begann. Die Beschriftung lief
        //   damit unter das Feld (englisch „Usable temperature spread [K]:" 173 px, Ende
        //   bei 201 - dort 51 px Überdeckung). Betroffen sind alle drei Steuerelemente
        //   dieser Spalte, damit sie eine Spalte BLEIBEN: _cbBoden (142), _cbZone (209)
        //   und _tbSpreizung (239) rücken von 150 auf 170. Neuer Abstand zur längsten
        //   Beschriftung: 9 px deutsch.
        // * Die beiden Klammerhinweise rechts der Auswahllisten rücken mit:
        //   _lblBodentypHinweis und _lblKlimazoneHinweis von x = 392 auf 412. Die Listen
        //   sind 230 px breit und enden jetzt bei 400; ohne das Mitrücken stünde der
        //   Hinweis 8 px INNERHALB der Liste. Bei 412 bleiben 12 px Abstand, und der
        //   längere der beiden Hinweise endet bei 664 - innerhalb der 688, die bis zum
        //   rechten Rand frei sind.
        // * Klimazonenkarte (Anwenderwunsch 29.08.2026): _btnKarte („…", 374/208,
        //   26 × 23) öffnet Form_Klimazonenkarte. Damit der Klammerhinweis bei
        //   x = 412 unverändert stehen bleibt, ist NUR _cbZone von 230 auf 200 px
        //   verkürzt (längster Eintrag „15 — 2.400 h/a" braucht rund 90 px) - der
        //   Knopf endet bündig an der alten Listenkante 400.
        // * _lblSpreizungHinweis: 232 -> 252, MaximumSize 456 -> 436. Das Eingabefeld
        //   endet jetzt bei 240; 252 hält die 12 px Abstand, und 436 ist genau der Rest
        //   bis zur rechten Kante (688). NACHGEMESSEN, weil der Hinweis vom Umbruch lebt:
        //   Bei 436 belegt er deutsch 427 x 30 und englisch 428 x 30 - unverändert ZWEI
        //   Zeilen. Die Reserve nach unten bleibt damit die bekannte knappe: Der Hinweis
        //   endet bei y = 272, die Vorschau-Gruppe beginnt bei 280. Eine dritte Zeile
        //   entsteht erst unterhalb von 416 px MaximumSize - die 436 haben also Luft,
        //   ohne dass Vorschau-Gruppe, Diagramm (ChartAufbauen) oder Fensterhöhe
        //   angefasst werden mussten.
        // * _btnOk 510/712 -> 458/708 und _btnAbbruch 603/712 -> 578/708, beide
        //   85 x 23 (WinForms-Vorgabe) -> 110 x 30. Die RECHTE KANTE der Knopfgruppe
        //   bleibt bei x = 688 und damit 12 px vor dem Fensterrand; zwischen den Knöpfen
        //   liegen 10 px. y rückt um 4 px nach oben, weil die höheren Knöpfe sonst nur
        //   6 px über dem unteren Fensterrand endeten: jetzt 8 px Abstand zur
        //   Prüfungs-Rubrik (endet bei 700) und 10 px nach unten. Die Herleitung
        //   „ClientSize.Width − 190 / − 97" trägt nicht mehr, weil die Knöpfe breiter
        //   geworden sind; maßgeblich ist jetzt die rechte Kante.
        // * _btnSimulation: Höhe 28 -> 30, einheitlich mit den Fußknöpfen. Position und
        //   Breite bleiben (528/126, 134 px); der Knopf endet damit bei y = 156 und
        //   bleibt innerhalb der Rubrik. Die 134 px sind Spaltenbreite, nicht Textbedarf
        //   („Simulation" braucht 75 px) - sie halten die rechte Kante der Rubrik.
        // * NICHT geändert: ClientSize, _gbSystem, _gbVorschau, _gbPruefung, das
        //   Diagramm in ChartAufbauen() und alle y-Werte außer denen der Fußknöpfe.
        //   ÜBERHOLT durch die Nacharbeit, siehe unten: _gbVorschau, _gbPruefung und
        //   das Diagramm sind jetzt sehr wohl geändert; unverändert bleiben allein
        //   ClientSize, _gbSystem und die Fußknöpfe.
        //
        // ==================================================================
        // NACHARBEIT ZUR DESIGN-POLITUR 21.08.2026
        // ==================================================================
        //
        // Zwei Befunde, die erst mit den Echttexten IN BEIDEN SPRACHEN sichtbar wurden.
        // Alle Maße wieder mit den echten Steuerelementen nachgemessen (Segoe UI 9 pt,
        // 96 dpi, DpiUnaware, EnableVisualStyles wie in Program.Main); ClientSize bleibt
        // bei 700 x 748 und die Fußknöpfe bleiben bei y = 708.
        //
        // ------------------------------------------------------------------
        // (A) _lblAenderung braucht eine DRITTE ZEILE — 14 px aus dem Diagramm
        // ------------------------------------------------------------------
        //
        // Gemessen bei 500 px Feldbreite (Label.GetPreferredSize, deckungsgleich mit
        // TextRenderer.MeasureText + WordBreak):
        //     SIMQ_ERDREICH_AENDERUNG_HINWEIS     deutsch 485 x 30, englisch 472 x 30
        //     SIMQ_ERDREICH_SIM_NUR_GESPEICHERT   deutsch 497 x 45, englisch 493 x 30
        // Der zweite Text („Der Lauf hat mit den GESPEICHERTEN Quelldaten gerechnet …")
        // belegt auf DEUTSCH drei Zeilen und damit 45 px; das Feld war 34 px hoch, die
        // dritte Zeile fiel also weg — und gerade sie trägt die Aussage, dass Grenzwert
        // und Sondenmeter bereits mit den geänderten Eingaben gerechnet sind.
        //
        // Die fehlenden 14 px kommen aus der Vorschau und NICHT aus der Fensterhöhe: Der
        // Dialog steht mit 787 px Außenhöhe schon dicht an der Grenze, die ein
        // 1366x768-Gerät zulässt (siehe die Begründung bei ClientSize weiter oben).
        //
        //   _gbVorschau   676 x 244 -> 676 x 230   (Lage 12/280 unverändert, Unterkante
        //                                           524 -> 510)
        //   Diagramm      652 x 184 -> 652 x 170   (ChartAufbauen, Lage 12/20; Unterkante
        //                                           innerhalb der Rubrik 204 -> 190)
        //   _lblKennwerte 14/210    -> 14/196      (650 x 20; 6 px unter dem Diagramm und
        //                                           14 px über der Rubrikkante — beide
        //                                           Abstände exakt wie vorher)
        //   _gbPruefung   12/532, 676 x 168 -> 12/518, 676 x 182
        //                                          (14 px höher bei GLEICHER Unterkante
        //                                           700; der Abstand zur Vorschau bleibt
        //                                           mit 8 px unverändert, weil beide
        //                                           Kanten um dieselben 14 px wandern)
        //   _lblAenderung 500 x 34  -> 500 x 48    (45 px Textbedarf + 3 px Reserve)
        //
        // _lblPruefung (14/22, 650 x 100, endet bei 122) und _btnSimulation (528/126,
        // 134 x 30, endet bei 156 bzw. rechts bei 662) sind nachgerechnet und bleiben
        // unangetastet: Der Hinweis beginnt weiterhin 6 px unter dem Prüfergebnis, endet
        // jetzt bei y = 176 und hält damit dieselben 6 px zur Rubrikkante wie vorher;
        // waagerecht bleiben zwischen Hinweis (endet bei 514) und Schaltfläche 14 px.
        //
        // ------------------------------------------------------------------
        // (B) Sprachrobustes Raster der Quellsystem-Rubrik
        // ------------------------------------------------------------------
        //
        // Siehe QuellsystemRasterAusrichten(). Im Designer bleiben die DEUTSCHEN
        // Entwurfswerte stehen (Beschriftungen 160/390, Felder 285/490) — die Methode
        // rechnet sie zur Laufzeit nach und kommt auf Deutsch auf genau dieselben Zahlen.

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.SIMQ_ERDREICH_TITEL;

            _gbSystem.Text = MyResource.Resource.SIMQ_ERDREICH_GB_QUELLSYSTEM;
            _rbKollektor.Text = MyResource.Resource.SIMQ_ERDREICH_RB_KOLLEKTOR;
            _lblVerlegetiefe.Text = MyResource.Resource.SIMQ_ERDREICH_VERLEGETIEFE;
            _lblFlaeche.Text = MyResource.Resource.SIMQ_ERDREICH_FLAECHE;
            _rbSonde.Text = MyResource.Resource.SIMQ_ERDREICH_RB_SONDE;
            _lblLaengeSonde.Text = MyResource.Resource.SIMQ_ERDREICH_LAENGE_SONDE;
            _lblAnzahlSonden.Text = MyResource.Resource.SIMQ_ERDREICH_ANZAHL_SONDEN;

            _lblBodentyp.Text = MyResource.Resource.SIMQ_ERDREICH_BODENTYP;
            _lblBodentypHinweis.Text = MyResource.Resource.SIMQ_ERDREICH_BODENTYP_HINWEIS;
            _lblKlimazone.Text = MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE;
            _lblKlimazoneHinweis.Text = MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE_HINWEIS;
            _lblSpreizung.Text = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG;
            _lblSpreizungHinweis.Text = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG_HINWEIS;

            _gbVorschau.Text = MyResource.Resource.SIMQ_ERDREICH_GB_VORSCHAU;
            _gbPruefung.Text = MyResource.Resource.SIMQ_ERDREICH_GB_PRUEFUNG;
            _btnSimulation.Text = MyResource.Resource.SIMQ_ERDREICH_BTN_SIMULATION;
            _tipKarte.SetToolTip(_btnKarte, MyResource.Resource.SIMQ_KARTE_KNOPF_TIP);

            _btnOk.Text = MyResource.Resource.SIM_BTN_OK;
            _btnAbbruch.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
        }

        /// <summary>
        /// Feste Pixel-Geometrie (Konzept 13.6, Hauptrisiko der programmatischen
        /// Dialoge): Die englischen Beschriftungen sind länger als die deutschen. Die
        /// Quellsystem-Rubrik richtet ihre Spalten deshalb zur Laufzeit an den GEMESSENEN
        /// Textbreiten aus - Muster
        /// <see cref="Form_QuellePufferspeicher"/>.<c>EingabespalteAusrichten</c>.
        ///
        /// Sie MUSS nach <see cref="TexteSetzen"/> laufen: Vorher tragen Auswahlknöpfe
        /// und Beschriftungen den DEUTSCHEN Entwurfstext des Designers und sind in jeder
        /// anderen Sprache falsch breit. Die Steuerelemente sind bereits Kinder von
        /// <c>_gbSystem</c> (der Designer hängt sie in <c>InitializeComponent</c> ein) -
        /// nur deshalb greift die Messung überhaupt; eine AutoSize-Beschriftung OHNE
        /// Container behält die Vorgabebreite von 100 px (BEFUND der Designer-Umstellung
        /// bei Form_QuellePufferspeicher).
        ///
        /// BEFUND der Nacharbeit zur Design-Politur. Englisch überdeckten sich drei
        /// Stellen (gemessen an den echten Steuerelementen):
        ///   • „Horizontal ground collector" endet bei 187, „Borehole heat exchanger" bei
        ///     171 - die Beschriftungsspalte beginnt aber schon bei x = 160. Beide
        ///     Auswahlknöpfe liefen also UNTER die Beschriftungen.
        ///   • „Installation depth [m]:" endet bei 284, das Eingabefeld beginnt bei 285 -
        ///     1 px Luft, faktisch auf Stoß.
        ///   • Beides zusammen schiebt die erste Eingabespalte so weit nach rechts, dass
        ///     sie ohne Nachführung in die zweite Beschriftungsspalte (x = 390) liefe.
        ///
        /// Die Rubrik ist ein Raster aus VIER Spalten - Beschriftung/Feld für den
        /// Kollektor links, Beschriftung/Feld für Fläche bzw. Anzahl rechts. Gerechnet
        /// wird deshalb von links nach rechts durch, jede Spalte gegen die vorige:
        ///   1. Beschriftungsspalte links = hinter dem breiteren der beiden Auswahlknöpfe
        ///      (+12 px),
        ///   2. Eingabespalte links = hinter der breiteren der beiden Beschriftungen
        ///      (+8 px),
        ///   3. Beschriftungsspalte rechts = hinter der linken Eingabespalte (+35 px, der
        ///      Spaltenabstand des Entwurfs),
        ///   4. Eingabespalte rechts = hinter der breiteren der beiden Beschriftungen
        ///      (+8 px).
        /// Jede Spalte ist nach unten auf ihren Entwurfswert geklemmt. Damit ist DEUTSCH
        /// pixelgleich mit dem Designer-Bild: Dort greift in allen vier Schritten die
        /// Untergrenze (gemessen 104 + 12 = 116 &lt; 160, 272 + 8 = 280 &lt; 285,
        /// 355 + 35 = 390, 479 + 8 = 487 &lt; 490).
        ///
        /// ENGLISCH rückt das Raster nach rechts: 199 / 331 / 436 / 537. Die rechte
        /// Eingabespalte endet damit bei 607 und bleibt innerhalb der Rubrik, die bei
        /// 666 (676 breit abzüglich 10 px Rand) endet - 59 px Reserve für längere
        /// Übersetzungen. Die Feldbreiten bleiben in jedem Fall unangetastet; reicht der
        /// Platz einmal nicht, wird nur die POSITION an der rechten Kante geklemmt (die
        /// beiden <c>if</c>-Zeilen unten). Das ist ein Notnagel gegen abgeschnittene
        /// Felder - er nimmt eine Überdeckung in Kauf und wird von keiner der beiden
        /// ausgelieferten Sprachen erreicht.
        /// </summary>
        private void QuellsystemRasterAusrichten()
        {
            // Entwurfswerte aus dem Designer = Untergrenzen der Rechnung.
            const int X_LABEL_LINKS = 160;
            const int X_FELD_LINKS = 285;
            const int X_LABEL_RECHTS = 390;
            const int X_FELD_RECHTS = 490;

            const int ABSTAND_KNOPF_LABEL = 12;
            const int ABSTAND_LABEL_FELD = 8;
            // Spaltenabstand des Entwurfs: 390 - (285 + 70).
            const int ABSTAND_SPALTEN = 35;
            const int RAND_RECHTS = 10;

            int grenze = _gbSystem.Width - RAND_RECHTS;   // 666

            int xLabelLinks = Math.Max(X_LABEL_LINKS,
                Math.Max(_rbKollektor.Right, _rbSonde.Right) + ABSTAND_KNOPF_LABEL);
            _lblVerlegetiefe.Left = xLabelLinks;
            _lblLaengeSonde.Left = xLabelLinks;

            int xFeldLinks = Math.Max(X_FELD_LINKS,
                Math.Max(_lblVerlegetiefe.Right, _lblLaengeSonde.Right) + ABSTAND_LABEL_FELD);
            if (xFeldLinks + _tbTiefe.Width > grenze) xFeldLinks = grenze - _tbTiefe.Width;
            _tbTiefe.Left = xFeldLinks;
            _tbLaenge.Left = xFeldLinks;

            int xLabelRechts = Math.Max(X_LABEL_RECHTS,
                xFeldLinks + _tbTiefe.Width + ABSTAND_SPALTEN);
            _lblFlaeche.Left = xLabelRechts;
            _lblAnzahlSonden.Left = xLabelRechts;

            int xFeldRechts = Math.Max(X_FELD_RECHTS,
                Math.Max(_lblFlaeche.Right, _lblAnzahlSonden.Right) + ABSTAND_LABEL_FELD);
            if (xFeldRechts + _tbFlaeche.Width > grenze) xFeldRechts = grenze - _tbFlaeche.Width;
            _tbFlaeche.Left = xFeldRechts;
            _tbAnzahl.Left = xFeldRechts;
        }

        /// <summary>
        /// Baut das Vorschau-Diagramm und hängt es in die Vorschau-Gruppe ein.
        ///
        /// Steht bewusst NICHT im Designer (Migrationsregel 8): Die Serialisierung des
        /// portierten <c>WinForms.DataVisualization</c>-<c>Chart</c> ist unter
        /// VS 2022/.NET 8 unzuverlässig, und ChartArea/Series/Legend würden beim ersten
        /// Speichern der Entwurfsfläche neu geschrieben. Muster wie
        /// <c>Form_PeakShaving</c>: das Diagramm hinter <c>InitializeComponent</c>
        /// per Code einhängen.
        ///
        /// 170 statt 210 Pixel hoch (bis zur Nacharbeit zur Design-Politur 184): die
        /// 40 Pixel gehen an die Zeilen, die Befund 1, Befund 3/4 und die dritte Zeile
        /// des Änderungshinweises unten brauchen - siehe die Begründung bei ClientSize
        /// und den Abschnitt (A) der Nacharbeit. Für einen Jahresgang über zwölf Monate
        /// bleibt das Seitenverhältnis 652×170 gut lesbar; die Zoom-Bedienung des
        /// Diagramms ist unberührt.
        /// </summary>
        private void ChartAufbauen()
        {
            _chart = new Chart
            {
                Location = new Point(12, 20),
                Size = new Size(652, 170)
            };
            ChartArea ca = new ChartArea("Jahr");
            ca.AxisX.Title = MyResource.Resource.CHART_ACHSE_MONAT;
            ca.AxisY.Title = MyResource.Resource.CHART_ACHSE_QUELLTEMPERATUR;
            ca.AxisX.Minimum = 0;
            ca.AxisX.Maximum = 12;
            ca.AxisX.Interval = 1;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.AxisX.ScaleView.Zoomable = true;
            _chart.ChartAreas.Add(ca);

            // FastLine: 8760 Punkte je Neuzeichnung (Konzept 4.5).
            // Series.Name ist der technische Schlüssel (Schicht 2), der Anzeigetext steht
            // in LegendText — Muster wie NavigatorWaerme (Paket 9 / L6).
            Series sQuelle = new Series(S_QUELLTEMPERATUR)
            {
                ChartType = SeriesChartType.FastLine,
                Color = Color.FromArgb(200, Color.SaddleBrown),
                BorderWidth = 2,
                XValueType = ChartValueType.Double,
                LegendText = MyResource.Resource.CHART_SERIE_QUELLTEMPERATUR
            };
            _chart.Series.Add(sQuelle);

            Series sAussen = new Series(S_AUSSENTEMPERATUR)
            {
                ChartType = SeriesChartType.FastLine,
                Color = Color.FromArgb(90, Color.SteelBlue),
                BorderWidth = 1,
                XValueType = ChartValueType.Double,
                LegendText = MyResource.Resource.CHART_SERIE_AUSSENTEMPERATUR
            };
            _chart.Series.Add(sAussen);

            _chart.Legends.Add(new Legend("L") { Docking = Docking.Top, Alignment = StringAlignment.Center });
            sQuelle.Legend = "L";
            sAussen.Legend = "L";

            _gbVorschau.Controls.Add(_chart);
        }

        /// <summary>
        /// Füllt die beiden Auswahllisten. Steht nicht im Designer: Die Bodenarten
        /// kommen aus <see cref="ErdreichTemperatur.KatalogAnzeige"/> (Anzeigetexte,
        /// Schicht 3 — der Persistenzschlüssel wird über den INDEX zugeordnet, siehe
        /// <see cref="AktuellerBodentyp"/>), die Klimazonen aus
        /// <see cref="VDI4640Pruefung"/>. Beides ist Laufzeitdaten und dürfte im
        /// Designer-Code nicht als Literal landen (Migrationsregel 7).
        /// </summary>
        private void KatalogeFuellen()
        {
            _cbBoden.Items.AddRange(ErdreichTemperatur.KatalogAnzeige());

            _cbZone.Items.Add(MyResource.Resource.SIMQ_ERDREICH_ZONE_NICHT_ZUGEORDNET);
            for (int z = 1; z <= VDI4640Pruefung.KLIMAZONEN; z++)
            {
                _cbZone.Items.Add(z.ToString(CultureInfo.CurrentCulture) + " — " +
                    VDI4640Pruefung.VolllaststundenZone(z).ToString("N0", CultureInfo.CurrentCulture) + " h/a");
            }
        }

        /// <summary>
        /// Vorbelegung der Eingabefelder. Steht bewusst nicht im Designer:
        /// <see cref="Vorgabe"/> und die Spreizung formatieren kulturabhängig, ein
        /// serialisiertes Literal fröre die deutsche Schreibweise ein.
        /// <see cref="SetControls"/> überschreibt die Werte unmittelbar vor dem
        /// Anzeigen erneut.
        /// </summary>
        private void VorgabenSetzen()
        {
            _tbTiefe.Text = Vorgabe(ErdreichTemperatur.TIEFE_DEFAULT);
            _tbFlaeche.Text = "0";
            _tbLaenge.Text = "90";
            _tbAnzahl.Text = "1";
            _tbSpreizung.Text = ErdreichAuswertung.SPREIZUNG_DEFAULT.ToString("0.##", CultureInfo.CurrentCulture);
        }

        // ==================================================================
        // Ereignisse der Eingabefelder
        // ==================================================================
        //
        // Bis zur Designer-Umstellung waren das sechs gleichlautende Lambdas im
        // Aufbaucode. Migrationsregel 5: In InitializeComponent darf keine Lambda
        // stehen, der Designer-Parser bricht daran ab. Weil alle closure-frei waren,
        // ist die Umstellung mechanisch - gleiche Rümpfe teilen sich eine Methode.

        /// <summary>Quellsystem gewechselt (Erdkollektor ↔ Erdsonde).</summary>
        private void rbQuellsystem_CheckedChanged(object sender, EventArgs e)
        {
            SystemUmschalten();
            Aktualisieren();
        }

        /// <summary>
        /// Zahleneingabe geändert - Verlegetiefe, Fläche, Sondenlänge, Sondenanzahl
        /// und nutzbare Spreizung teilen sich diesen Handler.
        /// </summary>
        private void eingabe_TextChanged(object sender, EventArgs e)
        {
            Aktualisieren();
        }

        /// <summary>Bodentyp oder Klimazone gewechselt.</summary>
        private void auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            Aktualisieren();
        }

        // ------------------------------------------------------------------
        // Vorbelegung
        // ------------------------------------------------------------------

        /// <summary>
        /// Belegt die Steuerelemente aus den öffentlichen Feldern und zeichnet die
        /// Vorschau ein erstes Mal. Vor ShowDialog aufzurufen.
        /// </summary>
        public void SetControls()
        {
            _uiAufbau = true;

            if (!string.IsNullOrEmpty(WPName))
                this.Text = string.Format(MyResource.Resource.SIMQ_ERDREICH_TITEL_MIT_WP, WPName);

            bool sonde = string.Equals(Quellsystem, ErdreichTemperatur.QUELLSYSTEM_SONDE,
                                       StringComparison.OrdinalIgnoreCase);
            _rbSonde.Checked = sonde;
            _rbKollektor.Checked = !sonde;

            if (sonde)
            {
                _tbLaenge.Text = (Tiefe > 0 ? Tiefe : 90).ToString("0.##", CultureInfo.CurrentCulture);
                _tbTiefe.Text = ErdreichTemperatur.TIEFE_DEFAULT.ToString("0.##", CultureInfo.CurrentCulture);
            }
            else
            {
                _tbTiefe.Text = (Tiefe > 0 ? Tiefe : ErdreichTemperatur.TIEFE_DEFAULT)
                    .ToString("0.##", CultureInfo.CurrentCulture);
                _tbLaenge.Text = "90";
            }

            _tbFlaeche.Text = Flaeche.ToString("0.##", CultureInfo.CurrentCulture);
            _tbAnzahl.Text = (Anzahl > 0 ? Anzahl : 1).ToString(CultureInfo.CurrentCulture);

            int bi = ErdreichTemperatur.KatalogIndex(Bodentyp);
            _cbBoden.SelectedIndex = bi >= 0 ? bi : ErdreichTemperatur.KatalogIndex(ErdreichTemperatur.BODENTYP_DEFAULT);

            _cbZone.SelectedIndex = (Klimazone >= 0 && Klimazone <= VDI4640Pruefung.KLIMAZONEN) ? Klimazone : 0;

            _tbSpreizung.Text = (Spreizung > 0 ? Spreizung : ErdreichAuswertung.SPREIZUNG_DEFAULT)
                .ToString("0.##", CultureInfo.CurrentCulture);

            _uiAufbau = false;

            // Ausgangsstand festhalten, BEVOR der Anwender etwas ändern kann (Befund 4).
            // Er ist der Stand der Datenbank - alles darüber siehe _standGeladen.
            _standGeladen = Eingabestand();
            _laufAusDialog = false;

            SystemUmschalten();
            Aktualisieren();
        }

        /// <summary>Aktiviert die Eingabefelder des gewählten Quellsystems.</summary>
        private void SystemUmschalten()
        {
            bool kollektor = _rbKollektor.Checked;
            _tbTiefe.Enabled = kollektor;
            _tbFlaeche.Enabled = kollektor;
            _tbLaenge.Enabled = !kollektor;
            _tbAnzahl.Enabled = !kollektor;
        }

        // ------------------------------------------------------------------
        // Vorschau und Prüfung
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeichnet Jahresgang, Kennwerte und Auslegungsprüfung neu. Rechnet
        /// ausschließlich aus dem gecachten Außentemperaturvektor.
        /// </summary>
        private void Aktualisieren()
        {
            if (_uiAufbau) return;

            string bodenSchluessel = AktuellerBodentyp();
            ErdreichTemperatur.Bodenkennwerte boden = ErdreichTemperatur.Bodentyp(bodenSchluessel);

            float tiefe, flaeche, laenge, anzahl;
            WaermequelleClass.ZahlParsen(_tbTiefe.Text, out tiefe);
            WaermequelleClass.ZahlParsen(_tbFlaeche.Text, out flaeche);
            WaermequelleClass.ZahlParsen(_tbLaenge.Text, out laenge);
            WaermequelleClass.ZahlParsen(_tbAnzahl.Text, out anzahl);

            float[] profil = _rbSonde.Checked
                ? ErdreichTemperatur.JahresprofilSonde(Aussentemperatur, laenge)
                : ErdreichTemperatur.JahresprofilKollektor(Aussentemperatur, tiefe, bodenSchluessel);

            // Kennwerte des Bodens
            // Die Formatangaben (0.0 / 0.00) kommen aus dem Quelltext; der Katalog führt
            // die Platzhalter normalisiert als {0}…{4} (Lesehinweis des Katalogs). Sie
            // werden deshalb hier auf die Werte angewandt, nicht auf die Formatzeichenkette.
            _lblBoden.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.SIMQ_ERDREICH_BODENKENNWERTE,
                boden.Lambda.ToString("0.0", CultureInfo.CurrentCulture),
                boden.RhoCp.ToString("0.00", CultureInfo.CurrentCulture),
                boden.A_mm2s.ToString("0.00", CultureInfo.CurrentCulture),
                boden.Daempfungstiefe.ToString("0.00", CultureInfo.CurrentCulture),
                VDI4640Pruefung.BodenartAusBodentyp(bodenSchluessel));

            // Chart
            _chart.Series[0].Points.Clear();
            _chart.Series[1].Points.Clear();
            for (int i = 0; i < profil.Length; i++)
            {
                double x = i * 12.0 / ErdreichTemperatur.STUNDEN_JAHR;
                _chart.Series[0].Points.AddXY(x, profil[i]);
            }
            if (Aussentemperatur != null && Aussentemperatur.Length >= ErdreichTemperatur.STUNDEN_JAHR)
            {
                for (int i = 0; i < ErdreichTemperatur.STUNDEN_JAHR; i++)
                {
                    double x = i * 12.0 / ErdreichTemperatur.STUNDEN_JAHR;
                    _chart.Series[1].Points.AddXY(x, Aussentemperatur[i]);
                }
            }

            // Kennwertzeile
            ErdreichTemperatur.Kennwerte k = ErdreichTemperatur.ProfilKennwerte(profil);
            ErdreichTemperatur.Jahresgang jg = ErdreichTemperatur.AnalysiereJahresgang(Aussentemperatur);
            _lblKennwerte.Text = k.Zeile() +
                (jg.AusKlimadaten ? "" : MyResource.Resource.SIMQ_ERDREICH_OHNE_KLIMADATEN);

            PruefungAktualisieren(bodenSchluessel, tiefe, flaeche, laenge, anzahl);

            // Befund 4: Der Hinweis hängt an DIESER Stelle und nicht an den einzelnen
            // Ereignishandlern - Aktualisieren() ist der gemeinsame Weg aller sechs
            // Eingaben (Quellsystem, Verlegetiefe/Fläche, Sondenlänge/-anzahl, Bodentyp,
            // Klimazone, Spreizung), und der Rücksprung bei _uiAufbau oben stellt
            // sicher, dass die Vorbelegung selbst nichts auslöst.
            AenderungshinweisAktualisieren();
        }

        /// <summary>Füllt den Bereich der Auslegungsprüfung (Konzept 4.5/13.1).</summary>
        private void PruefungAktualisieren(string bodenSchluessel, double tiefe, double flaeche,
                                           double laenge, double anzahl)
        {
            if (!ErgebnisseVorhanden)
            {
                _lblPruefung.Text = !string.IsNullOrEmpty(HinweisErgebnis)
                    ? HinweisErgebnis
                    : Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_PRUEFUNG_KEIN_LAUF);
                // Zurücksetzen ist neu nötig: Seit Befund 3 kann ErgebnisseVorhanden
                // während der Lebensdauer des Dialogs umschlagen. Ohne diese Zeile
                // behielte ein Hinweistext das Firebrick einer vorher angezeigten
                // Grenzwertüberschreitung.
                _lblPruefung.ForeColor = SystemColors.ControlText;
                return;
            }

            VDI4640Pruefung.Ergebnis erg;
            if (_rbSonde.Checked)
            {
                double meter = laenge * Math.Max(1, anzahl);
                double stunden = VolllastStunden > 0 ? VolllastStunden : VDI4640Pruefung.VolllaststundenZone(AktuelleZone());
                erg = VDI4640Pruefung.PruefeSonde(
                    ErdreichTemperatur.Bodentyp(bodenSchluessel).Lambda,
                    (int)Math.Max(1, anzahl), stunden, meter, MaxEntzugW, bodenSchluessel);
            }
            else
            {
                erg = VDI4640Pruefung.PruefeKollektor(
                    AktuelleZone(), VDI4640Pruefung.BodenartAusBodentyp(bodenSchluessel),
                    flaeche, MaxEntzugW, JahresentzugKWh, bodenSchluessel);
            }

            // Der Festgesteins-Vorbehalt steht jetzt als Flag im Ergebnis (für den
            // Ergebnisausweis in Paket 7); der Dialog macht ihn zusätzlich sichtbar.
            string text = erg.Anzeigetext();
            if (erg.Moeglich && erg.FestgesteinNaeherung)
                text += Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_HINWEIS_FESTGESTEIN);
            if (!string.IsNullOrEmpty(HinweisVorbehalt))
                text += string.Format(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_HINWEIS_VORBEHALT),
                    HinweisVorbehalt);
            if (!string.IsNullOrEmpty(HinweisFrost))
                text += "\r\n  " + HinweisFrost;

            _lblPruefung.Text = text;
            _lblPruefung.ForeColor = (erg.Moeglich && erg.Warnung) ? Color.Firebrick : SystemColors.ControlText;
        }

        private string AktuellerBodentyp()
        {
            int i = _cbBoden.SelectedIndex;
            if (i < 0 || i >= ErdreichTemperatur.Katalog.Length) return ErdreichTemperatur.BODENTYP_DEFAULT;
            return ErdreichTemperatur.Katalog[i].Schluessel;
        }

        private int AktuelleZone()
        {
            return _cbZone.SelectedIndex < 0 ? 0 : _cbZone.SelectedIndex;
        }

        // ------------------------------------------------------------------
        // Hinweis auf geänderte Eingaben (Befund 4 vom 17.08.2026)
        // ------------------------------------------------------------------

        /// <summary>
        /// Kennzeichnung des aktuellen Eingabestands - Vergleichsgröße für
        /// <see cref="_standGeladen"/>. Enthält alle sechs Größen, die in die
        /// Auslegungsprüfung eingehen; das trennende Zeichen ist bewusst eines, das in
        /// keinem der Werte vorkommen kann.
        /// </summary>
        private string Eingabestand()
        {
            return (_rbSonde.Checked ? "S" : "K") + "\u0001" +
                   _tbTiefe.Text + "\u0001" + _tbFlaeche.Text + "\u0001" +
                   _tbLaenge.Text + "\u0001" + _tbAnzahl.Text + "\u0001" +
                   _cbBoden.SelectedIndex.ToString(CultureInfo.InvariantCulture) + "\u0001" +
                   _cbZone.SelectedIndex.ToString(CultureInfo.InvariantCulture) + "\u0001" +
                   _tbSpreizung.Text;
        }

        /// <summary>
        /// Schreibt die Hinweiszeile der Auslegungsprüfung (Befund 4). Drei Zustände:
        ///
        ///   • Es liegt kein Lauf vor ODER die Eingaben stehen noch auf dem geladenen
        ///     Stand → kein Hinweis. Im ersten Fall sagt die Prüfung selbst schon
        ///     „(noch kein Simulationslauf)", im zweiten passen Anzeige und Lauf zusammen.
        ///
        ///   • Eingaben geändert, kein Lauf aus diesem Dialog → die Prüfung zeigt den
        ///     Stand des letzten Laufs; der Anwender muss die Simulation neu starten.
        ///
        ///   • Eingaben geändert UND hier schon gerechnet → der Lauf hat mit den
        ///     GESPEICHERTEN Werten gerechnet, weil die Engine WQ_* aus der Datenbank
        ///     liest (ErdreichAuswertung/SimulationWaermepumpe) und nicht aus diesem
        ///     Dialog. Ein Neustart des Laufs würde daran nichts ändern - deshalb steht
        ///     hier ein anderer Satz, der auf „OK" verweist. Grenzwert und Sondenmeter
        ///     rechnet PruefungAktualisieren dagegen sehr wohl mit den neuen Eingaben;
        ///     genau diese Halbheit muss der Text benennen.
        /// </summary>
        private void AenderungshinweisAktualisieren()
        {
            bool geaendert = !string.Equals(Eingabestand(), _standGeladen, StringComparison.Ordinal);

            if (!ErgebnisseVorhanden || !geaendert)
            {
                _lblAenderung.Text = "";
                return;
            }

            _lblAenderung.Text = _laufAusDialog
                ? MyResource.Resource.SIMQ_ERDREICH_SIM_NUR_GESPEICHERT
                : MyResource.Resource.SIMQ_ERDREICH_AENDERUNG_HINWEIS;
        }

        // ------------------------------------------------------------------
        // Simulationslauf aus dem Dialog (Befund 3 vom 17.08.2026)
        // ------------------------------------------------------------------

        /// <summary>
        /// Rechnet das Projekt durch und füllt die Auslegungsprüfung mit den Größen, die
        /// nur ein Simulationslauf liefern kann: maximale Entzugsleistung,
        /// Jahresentzugsarbeit und Jahresvolllaststunden.
        ///
        /// WARUM EIN VOLLSTÄNDIGER LAUF. Ein kleinerer Rechenweg gibt es nicht. Die drei
        /// Größen entstehen in <see cref="ErdreichAuswertung"/> aus der Entzugsganglinie
        /// „Wärmeproduktion − Strombedarf" der gesamten Wärmepumpenkaskade; die
        /// Wärmeproduktion setzt den Wärmebedarf des Gebäudes und den vollständigen
        /// Kaskadenlauf mit Puffer voraus, und <c>ErdreichAuswertung.AusLauf</c> nimmt
        /// folgerichtig eine fertige <c>SimulationControl</c>. Ein Teilnachbau würde
        /// Enginelogik doppeln.
        ///
        /// WAS DER LAUF ÄNDERT - und was nicht:
        ///   • <see cref="SimulationRunner.Simuliere"/> RECHNET nur. Anders als
        ///     <c>SimuliereUndSpeichere</c> ruft es <c>ErgebnisCtrl.Save</c> NICHT auf,
        ///     es entstehen also keine Zeilen in den Tab_Ergebnis*-Tabellen. Der
        ///     Rechenpfad selbst schreibt nichts in die Datenbank (nachgesehen: keine
        ///     schreibenden Anweisungen in SimulationControl/SimulationWaermepumpe/
        ///     SimulationWaermebedarf/SimulationStrombedarf).
        ///   • Der Lauf setzt aber den prozessweiten Zwischenspeicher von
        ///     <see cref="ErdreichAuswertung"/> für dieses Projekt neu - das ist gewollt
        ///     (es IST ein echter Lauf) und wirkt sich auf die Ergebnisanzeigen der
        ///     Sitzung aus, so wie jeder andere Lauf auch.
        ///   • Er rechnet mit den GESPEICHERTEN WQ_*-Werten. Der Dialog schreibt seine
        ///     Eingaben erst nach „OK" (und zwar im Aufrufer), und daran ändert dieser
        ///     Knopf bewusst nichts: Ein „Abbrechen" muss die Eingaben verwerfen können.
        ///     Weicht die Anzeige ab, sagt die Hinweiszeile das an (Befund 4).
        ///
        /// Der Lauf läuft SYNCHRON im Oberflächenfaden mit Wartecursor - Muster wie
        /// <c>Form_SpeicherOptimierung</c>. Der Dialog ist modal, es kann also nichts
        /// dazwischenkommen; der Knopf sperrt sich zusätzlich selbst, damit ein zweiter
        /// Klick nicht in denselben Lauf hineinläuft.
        /// </summary>
        private void btnSimulation_Click(object sender, EventArgs e)
        {
            int idProjekt = ProjektErmitteln();
            if (idProjekt <= 0)
            {
                // Kein Meldung(): Das setzt DialogResult auf None und ist der
                // Prüfpfad von „OK". Hier ist nichts zu bestätigen.
                MessageBox.Show(MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_OHNE_PROJEKT.Replace("\n", "\r\n"),
                                MyResource.Resource.SIMQ_ERDREICH_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fehler;
            bool ok;
            _btnSimulation.Enabled = false;
            // Wartecursor über this.Cursor (Muster FormMain) und nicht über
            // Cursor.Current: In einer von Form abgeleiteten Klasse verdeckt die
            // geerbte Eigenschaft Control.Cursor den gleichnamigen Typ, der statische
            // Zugriff wäre also nur voll qualifiziert möglich. Der Dialog ist modal -
            // seine eigene Fläche ist die einzige, die der Anwender währenddessen
            // bedienen könnte.
            this.Cursor = Cursors.WaitCursor;
            try
            {
                ok = new SimulationRunner().Simuliere(idProjekt, out fehler);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                _btnSimulation.Enabled = true;
            }

            if (!ok)
            {
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_FEHLER.Replace("\n", "\r\n"),
                                    fehler),
                                MyResource.Resource.SIMQ_ERDREICH_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ErdreichAuswertung.AnlageErgebnis erg = ErgebnisDesLaufs(idProjekt);
            if (erg == null)
            {
                MessageBox.Show(MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_OHNE_ERGEBNIS.Replace("\n", "\r\n"),
                                MyResource.Resource.SIMQ_ERDREICH_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ErgebnisUebernehmen(erg);
            _laufAusDialog = true;
            Aktualisieren();
        }

        /// <summary>ToolTip des Kartenknopfs; Text kommt aus <see cref="TexteSetzen"/>.</summary>
        private readonly ToolTip _tipKarte = new ToolTip();

        /// <summary>
        /// Öffnet die Klimazonenkarte (<see cref="Form_Klimazonenkarte"/>). „OK" oder
        /// Doppelklick auf eine Zonenfläche übernimmt die Zone in die Auswahlliste —
        /// deren SelectedIndexChanged zieht Vorschau und Prüfung nach.
        /// </summary>
        private void btnKarte_Click(object sender, EventArgs e)
        {
            using (Form_Klimazonenkarte dlg = new Form_Klimazonenkarte(_cbZone.SelectedIndex))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (dlg.GewaehlteZone >= 1 && dlg.GewaehlteZone <= VDI4640Pruefung.KLIMAZONEN)
                    _cbZone.SelectedIndex = dlg.GewaehlteZone;
            }
        }

        /// <summary>
        /// Projektbezug für den Lauf. Vorrang hat <see cref="ID_Projekt"/>; ist es nicht
        /// gesetzt, kommt der Bezug aus dem besitzenden Formular.
        ///
        /// Warum der Umweg: Der einzige Aufrufer ist
        /// <c>Form_Simulation_Config.Uebersicht.cs</c> (Zweig TYP_ERDREICH), und diese
        /// Datei wird derzeit an anderer Stelle umgebaut - sie durfte für diesen Befund
        /// nicht angefasst werden. <c>m_ID_Projekt</c> ist dort öffentlich, der Dialog
        /// wird mit <c>ShowDialog(this)</c> geöffnet und hat den Aufrufer damit als
        /// <c>Owner</c>. Sobald die Datei wieder frei ist, genügt dort
        /// <c>frmErde.ID_Projekt = m_ID_Projekt; frmErde.ID_Anlage = info.ID;</c> - dann
        /// greift der Vorrang und dieser Rückfallweg wird nicht mehr betreten.
        /// </summary>
        private int ProjektErmitteln()
        {
            if (ID_Projekt > 0) return ID_Projekt;

            Form_Simulation_Config aufrufer = this.Owner as Form_Simulation_Config;
            return (aufrufer != null && aufrufer.m_ID_Projekt > 0) ? aufrufer.m_ID_Projekt : 0;
        }

        /// <summary>
        /// Sucht das Erdreich-Ergebnis dieser Wärmepumpe aus dem eben gelaufenen
        /// Simulationslauf. Drei Stufen, absteigend nach Eindeutigkeit:
        ///
        ///   1. über <see cref="ID_Anlage"/> - eindeutig, sobald der Aufrufer sie setzt;
        ///   2. über den Modulnamen: <c>AnlageErgebnis.Modul</c> ist
        ///      <c>Tab_Energieanlagen.Bezeichner</c> (SimulationWaermepumpe:
        ///      <c>WP_Modul[i] = model.Bezeichner</c>), und genau den bekommt der Dialog
        ///      als <see cref="WPName"/> vom Aufrufer;
        ///   3. führt das Projekt nur EINE Erdreichquelle, ist auch deren einziges
        ///      Ergebnis eindeutig - dieser Fall trägt die Zuordnung, wenn der
        ///      Modulname leer war und durch einen Ersatznamen ersetzt wurde.
        ///
        /// null = der Lauf hat für diese Anlage nichts geliefert (Wärmepumpe nicht
        /// gerechnet, oder WQ_Typ steht in der Datenbank nicht auf Erdreich).
        /// </summary>
        private ErdreichAuswertung.AnlageErgebnis ErgebnisDesLaufs(int idProjekt)
        {
            ErdreichAuswertung.AnlageErgebnis einziges = null;
            int anzahl = 0;

            foreach (ErdreichAuswertung.AnlageErgebnis a in ErdreichAuswertung.FuerProjekt(idProjekt))
            {
                if (ID_Anlage > 0 && a.ID_Anlage == ID_Anlage) return a;
                if (!string.IsNullOrEmpty(WPName) &&
                    string.Equals(a.Modul, WPName, StringComparison.Ordinal)) return a;

                anzahl++;
                einziges = a;
            }

            return anzahl == 1 ? einziges : null;
        }

        /// <summary>
        /// Übernimmt die Ergebnisgrößen eines Laufs in die Felder der Auslegungsprüfung.
        ///
        /// Die Zuordnung ist Zeile für Zeile dieselbe, die der Aufrufer beim Öffnen des
        /// Dialogs vornimmt (Form_Simulation_Config.Uebersicht.cs, Zweig TYP_ERDREICH,
        /// Block „Ergebnisanbindung der Auslegungsprüfung"). Sie steht hier absichtlich
        /// noch einmal und nicht als gemeinsame Hilfsmethode: Die gemeinsame Methode
        /// gehörte in den Aufrufer oder in ErdreichAuswertung, und beide Wege hätten
        /// Dateien angefasst, die für diesen Befund gesperrt waren. Ändert sich die
        /// Zuordnung, sind beide Stellen zu pflegen - deshalb dieser Hinweis.
        /// </summary>
        private void ErgebnisUebernehmen(ErdreichAuswertung.AnlageErgebnis erg)
        {
            ErgebnisseVorhanden = erg.MaxEntzugBelastbar;
            MaxEntzugW = erg.MaxEntzugW;
            JahresentzugKWh = erg.JahresentzugKWh;
            VolllastStunden = erg.VolllastStunden;

            HinweisErgebnis = "";
            HinweisVorbehalt = "";
            HinweisFrost = "";

            if (erg.Unwirksam)
            {
                // Luft-Wasser: die Konfiguration wird gar nicht gerechnet.
                HinweisErgebnis = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMQ_ERDREICH_WIRKUNGSLOS.Replace("\n", Environment.NewLine),
                    erg.Grenze);
                return;
            }

            if (!erg.MaxEntzugBelastbar)
            {
                HinweisErgebnis = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMQ_ERDREICH_KEINE_PRUEFUNG.Replace("\n", Environment.NewLine),
                    erg.Grenze);
                return;
            }

            if (erg.MaxEntzugGeschaetzt) HinweisVorbehalt = erg.Grenze;
            if (erg.InklSpeicherladung)
                HinweisVorbehalt = (HinweisVorbehalt.Length > 0 ? HinweisVorbehalt + " " : "") +
                                   MyResource.Resource.SIMQ_ERDREICH_SPEICHERLADUNG;
            if (erg.FrostWarnung) HinweisFrost = erg.Frosttext();
        }

        // ------------------------------------------------------------------
        // Übernahme
        // ------------------------------------------------------------------

        private void btnOk_Click(object sender, EventArgs e)
        {
            string titel = MyResource.Resource.SIMQ_ERDREICH_TITEL;

            float tiefe, flaeche, laenge, anzahl;

            if (_rbKollektor.Checked)
            {
                if (!WaermequelleClass.ZahlParsen(_tbTiefe.Text, out tiefe) ||
                    !WaermequelleClass.ZahlParsen(_tbFlaeche.Text, out flaeche))
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_ZAHL_KOLLEKTOR, titel);
                    return;
                }
                if (tiefe <= 0)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_TIEFE_NULL, titel);
                    return;
                }
                if (tiefe > 10)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_TIEFE_MAX, titel);
                    return;
                }
                if (flaeche <= 0)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_FLAECHE, titel);
                    return;
                }

                Quellsystem = ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR;
                Tiefe = tiefe;
                Flaeche = flaeche;
                Anzahl = 0;
            }
            else
            {
                if (!WaermequelleClass.ZahlParsen(_tbLaenge.Text, out laenge) ||
                    !WaermequelleClass.ZahlParsen(_tbAnzahl.Text, out anzahl))
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_ZAHL_SONDE, titel);
                    return;
                }
                if (laenge <= 0)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_LAENGE_NULL, titel);
                    return;
                }
                if (anzahl < 1)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_ANZAHL_MIN, titel);
                    return;
                }

                Quellsystem = ErdreichTemperatur.QUELLSYSTEM_SONDE;
                Tiefe = laenge;
                Flaeche = 0;
                Anzahl = (int)Math.Round(anzahl);
            }

            float spreizung;
            if (!WaermequelleClass.ZahlParsen(_tbSpreizung.Text, out spreizung) || spreizung <= 0)
            {
                Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_SPREIZUNG, titel);
                return;
            }

            Spreizung = spreizung;
            Bodentyp = AktuellerBodentyp();
            Klimazone = AktuelleZone();
        }

        /// <summary>Hinweis anzeigen und den Dialog offen halten (Bestandsmuster).</summary>
        private void Meldung(string text, string titel)
        {
            MessageBox.Show(text, titel, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
        }
    }
}
