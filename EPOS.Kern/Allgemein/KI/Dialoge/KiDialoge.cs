using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die sprachneutralen Schluessel des Dialogkatalogs — die EINE Stelle, an der ein
    /// Razor-Dialog erfaehrt, unter welchem Namen er in der Maskenbruecke steht
    /// (Auftrag #200, Stufe S2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum die Namen bleiben, was sie waren.</b> <c>Form_Heizkessel_Bearbeiten</c>,
    /// <c>Form_PV</c>, <c>Form_PufferSp_Bearbeiten</c> und <c>Form_WP</c> sind Typnamen
    /// gefallener WinForms-Masken; als KATALOGSCHLUESSEL sind sie trotzdem richtig. Sie
    /// stehen im Aktionsprotokoll, im Werkzeugvertrag des Modells
    /// (<c>dialog_lesen(maske: "Form_PV")</c>) und in den Hilfeschluesseln der Dialoge
    /// (<c>Form_PV.btn_Help</c>) — sie umzubenennen hiesse, alle drei zugleich zu
    /// brechen, und zwar ohne fachlichen Gewinn. Die FUENFTE Maske ist deshalb auch die
    /// erste ohne <c>Form_</c>-Vorsilbe: Sie hatte nie eine WinForms-Fassung.
    /// </para>
    /// <para>
    /// <b>Die Zuordnung Razor-Dialog → Maskenname steht hier und nicht im Dialog.</b>
    /// Jede der fuenf Komponenten nennt beim Anmelden ihre Konstante; eine Zeichenkette
    /// im Markup waere die naechste Stelle, an der sich ein Tippfehler erst zur Laufzeit
    /// zeigt.
    /// </para>
    /// </remarks>
    public static class KiMaskennamen
    {
        /// <summary>Heizkessel-Katalogeditor (<c>HeizkesselKatalogDialog</c>).</summary>
        public const string HEIZKESSEL = "Form_Heizkessel_Bearbeiten";

        /// <summary>Photovoltaik-Projektdialog (<c>PhotovoltaikDialog</c>).</summary>
        public const string PHOTOVOLTAIK = "Form_PV";

        /// <summary>Pufferspeicher-Katalogeditor (<c>PufferSpKatalogDialog</c>).</summary>
        public const string PUFFERSPEICHER = "Form_PufferSp_Bearbeiten";

        /// <summary>Waermepumpen-Katalogeditor (<c>WaermepumpeStammDialog</c>).</summary>
        public const string WAERMEPUMPE = "Form_WP";

        /// <summary>Stromspeicher-Auslegung (<c>StromspeicherAuslegungSeite</c>).</summary>
        public const string STROMSPEICHER_AUSLEGUNG = "StromspeicherAuslegung";

        /// <summary>
        /// Die Ansicht „Simulation" (<c>SimulationSeite</c>, Auftrag #221) — die SECHSTE
        /// Maske und die zweite ohne <c>Form_</c>-Vorsilbe.
        /// </summary>
        public const string SIMULATION = "Simulation";

        /// <summary>
        /// Die Kostenverwaltung einer Komponente (<c>KostenKomponenteDialog</c>) — die
        /// SIEBTE Maske und die erste mit einem RASTER (Anwenderbefund vom 14.09.2026).
        /// </summary>
        /// <remarks>
        /// Sie ist der Anlass fuer die Spaltenform des Eigenschaftspfades
        /// (<see cref="KiEigenschaftspfad.Sammlungszeichen"/>): Ihre Werte stehen nicht
        /// in Einzelfeldern, sondern in einer Liste von Positionen. Mit den bisherigen
        /// zwei Pfadstufen liess sich davon kein Wert benennen - der Assistent
        /// antwortete deshalb auf „wie ist die Nutzungsdauer in diesem Projekt?" mit
        /// einem VORGABEWERT aus der Dokumentation, waehrend in der offenen Maske
        /// etwas anderes stand.
        /// </remarks>
        public const string KOSTENVERWALTUNG = "Kostenverwaltung";

        // =================================================================
        //  Welle KI-F1: die Erzeugermasken des PROJEKTS
        // =================================================================
        //
        // Sie stehen neben den KATALOGeditoren gleichen Gewerks und sind
        // etwas anderes: Der Editor pflegt einen Satz des Katalogs, diese
        // Maske pflegt die ANLAGE im Projekt - Vorlauf, Ruecklauf,
        // Grenzleistung, Modulzahl. Deshalb tragen beide ihren eigenen
        // Schluessel, und beide sind die WinForms-Maskennamen des Bestands
        // (Form_Heizkessel gegen Form_Heizkessel_Bearbeiten).

        /// <summary>Heizkessel im Projekt (<c>HeizkesselDialog</c>).</summary>
        public const string HEIZKESSEL_PROJEKT = "Form_Heizkessel";

        /// <summary>BHKW im Projekt (<c>BhkwDialog</c>).</summary>
        public const string BHKW_PROJEKT = "Form_BHKWEing";

        /// <summary>Pufferspeicher im Projekt (<c>PufferspeicherDialog</c>).</summary>
        public const string PUFFERSPEICHER_PROJEKT = "Form_PufferSp";

        /// <summary>Stromspeicher im Projekt (<c>StromspeicherDialog</c>).</summary>
        public const string STROMSPEICHER_PROJEKT = "Form_Stromspeicher";

        /// <summary>Solarkollektoren im Projekt (<c>SolarkollektorenDialog</c>).</summary>
        public const string SOLARKOLLEKTOREN_PROJEKT = "Form_SolarKollektoren";

        /// <summary>
        /// Die Waermepumpen-ANLAGE eines Projekts (<c>WaermepumpeAnlageDialog</c>)
        /// samt ihren Teilbausteinen Konfiguration und Stammfelder.
        /// </summary>
        /// <remarks>
        /// Sie steht neben <see cref="WAERMEPUMPE"/>, der Stammverwaltung: Die pflegt
        /// einen Satz des Katalogs, diese hier die Anlage im Projekt.
        /// </remarks>
        public const string WAERMEPUMPE_ANLAGE = "Form_WP_Anlage";

        // =================================================================
        //  Welle KI-F2: die Masken der SIMULATIONSKONFIGURATION
        // =================================================================
        //
        // Sie gehen aus Schritt ① der Ansicht "Simulation" auf - je Karte
        // die Maske, die zu ihr gehoert: die Pufferverwaltung des Projekts,
        // die zwei Quellenmasken der Waermepumpe, das Quellprofil, die
        // Waermesenken und die Konfiguration einer Komponente. Alle sechs
        // brauchen eine gewaehlte Komponente und lassen sich nicht
        // kontextfrei oeffnen; ihr Ziel ist deshalb die Ansicht selbst.

        /// <summary>
        /// Die Pufferspeicher-Verwaltung des Projekts
        /// (<c>PufferSpProjektDialog</c>).
        /// </summary>
        /// <remarks>
        /// Sie steht neben <see cref="PUFFERSPEICHER"/> (dem Katalogeditor) und
        /// <see cref="PUFFERSPEICHER_PROJEKT"/> (der Erzeugerkarte des Projekts) und
        /// ist etwas Drittes: Hier entstehen und vergehen die Speicher des Projekts
        /// samt Schichtmodell, Schwellen und Leistungsgrenzen. Alle drei tragen die
        /// WinForms-Maskennamen des Bestands.
        /// </remarks>
        public const string PUFFERSPEICHER_VERWALTUNG = "Form_PufferSp_Projekt";

        /// <summary>Waermequelle Erdreich einer Waermepumpe (<c>QuelleErdreichDialog</c>).</summary>
        public const string QUELLE_ERDREICH = "Form_QuelleErdreich";

        /// <summary>Waermequelle Pufferspeicher (<c>QuellePufferspeicherDialog</c>).</summary>
        public const string QUELLE_PUFFERSPEICHER = "Form_QuellePufferspeicher";

        /// <summary>Das Quellprofil einer Waermepumpe (<c>QuellprofilDialog</c>).</summary>
        public const string QUELLPROFIL = "Form_Quellprofil";

        /// <summary>Die Waermesenken einer Anlage (<c>WaermesenkeDialog</c>).</summary>
        public const string WAERMESENKE = "Form_Waermesenke";

        /// <summary>
        /// Die Konfiguration EINER Komponente der Simulation
        /// (<c>KomponentenKonfigurationDialog</c>) — die dritte Maske ohne
        /// <c>Form_</c>-Vorsilbe: Sie hatte nie eine WinForms-Fassung.
        /// </summary>
        public const string KOMPONENTENKONFIGURATION = "KomponentenKonfiguration";

        // =================================================================
        //  Welle KI-F3: die Masken des BEDARFS und der KLIMADATEN
        // =================================================================
        //
        // Sie gehen aus dem Reiter „Waermebedarf" bzw. „Strombedarf" der
        // Startseite und aus dem Menue „Administration" auf. Die Schluessel
        // sind die WinForms-Maskennamen des Bestands - auch dort, wo eine
        // Razor-Komponente heute mehrere von ihnen bedient.

        /// <summary>
        /// Die Gebaeudemaske (<c>GebaeudeDialog</c>) — Projektliste und Katalog
        /// nebeneinander.
        /// </summary>
        /// <remarks>
        /// <b>EINE Maske, zwei Betriebsarten.</b> Im Projekt fuehrt sie beide Listen, in
        /// der Katalogverwaltung nur den Katalog; die Felder sind dieselben, und welche
        /// Betriebsart gilt, steht als Feld darin. Ein zweiter Katalogeintrag haette
        /// zwei Wahrheiten ueber ein und dieselbe gezeichnete Maske gefuehrt.
        /// </remarks>
        public const string GEBAEUDE = "Form_Gebaeude";

        /// <summary>
        /// Die Wohn-/Nutzflaechenangabe eines Projektgebaeudes
        /// (<c>GebaeudeWohnflaecheDialog</c>).
        /// </summary>
        public const string GEBAEUDE_WOHNFLAECHE = "Form_GebWohnflaeche";

        /// <summary>Der Gebaeude-Katalogeditor (<c>GebaeudeKatalogDialog</c>).</summary>
        public const string GEBAEUDE_KATALOG = "Form_Gebaeude1";

        /// <summary>Der Waermebedarf EINES Gebaeudes (<c>GebaeudeBedarfDialog</c>).</summary>
        public const string GEBAEUDE_BEDARF = "Form_Gebaeude_Bedarf";

        /// <summary>Die Gebaeudetypen-Verwaltung (<c>GebaeudetypDialog</c>).</summary>
        public const string GEBAEUDETYP = "Form_EingGebTyp";

        /// <summary>
        /// Das Wochen-Stundenprofil eines Bedarfstyps (<c>TypProfilDialog</c>).
        /// </summary>
        /// <remarks>
        /// <b>EINE Komponente, DREI Auspraegungen.</b> Dieselbe Maske pflegt die
        /// Stromverbraucher-, die Prozess- und die Brauchwassertypen; der
        /// Katalogschluessel ist der WinForms-Maskenname der Stromfassung, und welche
        /// Auspraegung offen ist, sagt der Typ, den sie fuehrt.
        /// </remarks>
        public const string TYPPROFIL = "Form_EingStromTyp";

        /// <summary>
        /// Der Kopfsatz eines Bedarfskatalogs samt seinen zwoelf Monatswerten
        /// (<c>TypStammDialog</c>) — ebenfalls EINE Komponente mit drei Auspraegungen.
        /// </summary>
        public const string TYPSTAMM = "Form_EingDBStromverbraucher";
    }

    /// <summary>
    /// Der gefuellte Dialogkatalog: die vier Startmasken der Etappe 3b und — seit
    /// Auftrag #200 — die Stromspeicher-Ansicht (Fachkonzept 11.3/11.6, Konzept
    /// „Der Hilfe-Assistent im Dialog" 3.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum hier und nicht in KiKern.</b> Nur an dieser Stelle darf Wissen ueber die
    /// Masken stehen (Fachkonzept 3.7); <c>KiKern</c> haelt die Verwaltung und die
    /// Bauartsperre gegen Loeschknoepfe, kennt aber weder ein Control noch eine
    /// Datenklasse. Dieselbe Arbeitsteilung wie zwischen <see cref="KiRegister"/> und
    /// <c>KiAktionen</c>.
    /// </para>
    /// <para>
    /// <b>Der zweite Parameter ist seit Auftrag #200 der EIGENSCHAFTSPFAD</b>
    /// (<c>HeizkesselKatalogDaten.Ptherm</c>) und nicht mehr der WinForms-Controlname
    /// (<c>tb_th_Leistung</c>). Der alte Weg war tot: Die vier Masken sind seit iU9
    /// Razor-Komponenten, <c>Application.OpenForms</c> fuehrt sie nicht mehr, und
    /// <c>FindControlRecursive</c> fand nichts. An seine Stelle tritt die
    /// <see cref="KiMaskenbruecke"/> — der offene Dialog meldet je Feld einen Getter auf
    /// die Eigenschaft seines Daten-Objekts an. Der Typname VOR dem Punkt ist die Probe,
    /// dass Katalog und Daten-Objekt zusammengehoeren; ein Waechter haelt jeden
    /// Eigenschaftsnamen per Reflection gegen den Typ
    /// (<c>EPOS.UI.Tests/Dialoge/Hilfe/KiDialogkatalogTests</c>).
    /// </para>
    /// <para>
    /// <b>Die Maskenkoordinaten sind mit dieser Umstellung entfallen.</b> Drei der vier
    /// Eintraege trugen eine gemessene <c>KiKnopfposition</c> — der Platz, an dem der
    /// Aufrufknopf im Client-Bereich der WinForms-Maske noch frei war. Seit iU9‑W15b.5
    /// zeichnet den Knopf der Baustein <c>KiKnopf</c> im Kopf jedes Dialogs (Auftrag
    /// #199, Weg 1); es gibt keinen freien Platz mehr zu suchen und keine Maske mehr, in
    /// der man ihn suchen koennte. Die Zahlen standen sonst als gemessene Wahrheit ueber
    /// Fenster, die es nicht mehr gibt.
    /// </para>
    /// <para>
    /// <b>Der Feldumfang folgt der SICHTBAREN Maske — nach oben wie nach unten.</b> Seine
    /// Herkunft ist Fachkonzept 11.6 („Feldumfang v1 = die von der Knopfpruefung
    /// erfassten Eingabefelder"); das ist der Grund, warum drei der Katalogmasken so
    /// wenige Felder fuehren. Ihn zu ERWEITERN bleibt eine fachliche Entscheidung mit
    /// eigener Abnahme. Verliert eine Maske dagegen ein Feld, verliert es der Katalog im
    /// selben Schritt: Sonst boete der Assistent an, eine Zahl zu setzen, die in der
    /// offenen Maske niemand nachlesen kann. Der Waechter
    /// <c>EPOS.UI.Tests/Dialoge/Hilfe/KiDialogkatalogTests</c> haelt jeden Feldpfad gegen
    /// das Markup seiner Maske; ohne diese Probe stehen nur die beiden Ansichten, die
    /// ueber eine SICHTKLASSE binden (Stromspeicher, Simulation).
    /// </para>
    /// <para>
    /// <b>Kein Feld traegt einen Hilfe-Slug.</b> Die Zuordnung Feld → Slug las
    /// <c>HelpExtender.RegisterControl</c> aus <c>help_mapping.txt</c>; diese Datei liegt
    /// nicht im Repository, und es gibt im ganzen Baum keinen Aufruf von
    /// <c>SetHelpKey</c>. Der Weg dorthin steht trotzdem — deklariert wird aber nur, was
    /// belegt ist.
    /// </para>
    /// </remarks>
    public static class KiDialoge
    {
        private static KiDialogKatalog _katalog;
        private static readonly object _sperre = new object();

        /// <summary>
        /// Der Katalog dieser Sitzung - einmal gebaut, dann fest.
        /// </summary>
        /// <remarks>
        /// Dieselbe Bauart wie <c>KiAusfuehrer.Register</c>: Der Katalog entsteht beim
        /// ersten Zugriff und wird danach nur noch gelesen. Ein Katalog, dem zur Laufzeit
        /// eine Maske zuwachsen koennte, waere genau der Weg, auf dem eine nicht
        /// freigegebene Maske doch noch steuerbar wuerde (<see cref="KiDialogKatalog"/>).
        /// </remarks>
        public static KiDialogKatalog Katalog
        {
            get
            {
                if (_katalog != null) return _katalog;
                lock (_sperre)
                {
                    if (_katalog == null) _katalog = Erzeuge();
                }
                return _katalog;
            }
        }

        /// <summary>Baut den vollstaendigen Katalog.</summary>
        public static KiDialogKatalog Erzeuge()
        {
            return new KiDialogKatalog(
                Heizkessel(),
                Photovoltaik(),
                Pufferspeicher(),
                Waermepumpe(),
                Stromspeicherauslegung(),
                Simulation(),
                Kostenverwaltung(),
                HeizkesselProjekt(),
                BhkwProjekt(),
                PufferspeicherProjekt(),
                StromspeicherProjekt(),
                SolarkollektorenProjekt(),
                WaermepumpeAnlage(),
                PufferspeicherVerwaltung(),
                QuelleErdreich(),
                QuellePufferspeicher(),
                Quellprofil(),
                Waermesenke(),
                Komponentenkonfiguration(),
                Gebaeude(),
                GebaeudeWohnflaeche(),
                GebaeudeKatalog(),
                GebaeudeBedarf(),
                Gebaeudetyp(),
                Typprofil(),
                Typstamm());
        }

        // =====================================================================
        // Form_Gebaeude_Bedarf  ->  GebaeudeBedarfDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Der Waermebedarf EINES Gebaeudes — sechs Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeBedarfKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Maske, die RECHNET und nicht schreibt.</b> Drei Kennzahlen, eine
        /// Monatsuebersicht und ein Bild; eingestellt werden koennen nur die
        /// ANZEIGEEINHEIT und der Schalter zwischen Jahresganglinie und Dauerlinie.
        /// Beide liegen in den lebenden Feldern der Maske, die Kennzahlen im
        /// eingefrorenen Ergebnis — deshalb eine Sichtklasse ueber beides.
        /// </para>
        /// <para>
        /// <b>Die Kennzahlen stehen in MWh und kW</b>, so wie der Rechenkern sie
        /// liefert; die Maske rechnet nur fuer die Anzeige um.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben die zwoelf Monatssummen</b> (eine Zahlenfolge ohne
        /// Zeilentyp) und das Bild.
        /// </para>
        /// </remarks>
        private static KiDialog GebaeudeBedarf()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_BEDARF,
                anzeigename: KiDialogTexte.MaskeGebaeudeBedarf,
                felder: new[]
                {
                    new KiDialogFeld("einheit", "GebaeudeBedarfKiSicht.Einheit",
                                     KiDialogTexte.GebbEinheitName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebbEinheitErl),
                    new KiDialogFeld("sortiert", "GebaeudeBedarfKiSicht.Sortiert",
                                     KiDialogTexte.GebbSortiertName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebbSortiertErl),
                    new KiDialogFeld("gebaeude", "GebaeudeBedarfKiSicht.Gebaeude",
                                     KiDialogTexte.GebbGebaeudeName, KiParameterTyp.Text,
                                     KiDialogTexte.GebbGebaeudeErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("heizwaerme", "GebaeudeBedarfKiSicht.HeizwaermeMwh",
                                     KiDialogTexte.GebbHeizwaermeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebbHeizwaermeErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("max_last", "GebaeudeBedarfKiSicht.MaxLastKw",
                                     KiDialogTexte.GebbMaxLastName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebbMaxLastErl,
                                     einheit: KiDialogTexte.EINHEIT_KW,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("vollbenutzungsstunden",
                                     "GebaeudeBedarfKiSicht.VollbenutzungsstundenH",
                                     KiDialogTexte.GebbVollbenutzungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebbVollbenutzungErl,
                                     einheit: KiDialogTexte.EINHEIT_H_A,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk)
                });
        }

        // =====================================================================
        // Form_EingGebTyp  ->  GebaeudetypDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Gebaeudetypen-Verwaltung — drei Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudetypKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der NAME ist eine WAHL und kein Textfeld</b> (KI-D-Q6): Die Liste links
        /// waehlt den Typ, und mit ihm laedt die Maske einen anderen Satz samt seinen
        /// fuenf oder acht Tageskurven. Ebenso die KURVE — sie sagt, welche 24
        /// Stundenwerte gerade dastehen.
        /// </para>
        /// <para>
        /// <b>Die 24 Stundenwerte je Kurve bleiben draussen:</b> ein Raster mit eigenem
        /// Editor. Gepflegt werden sie Feld fuer Feld und gespeichert als Kurve.
        /// </para>
        /// </remarks>
        private static KiDialog Gebaeudetyp()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDETYP,
                anzeigename: KiDialogTexte.MaskeGebaeudetyp,
                felder: new[]
                {
                    new KiDialogFeld("typ", "GebaeudetypKiSicht.Typ",
                                     KiDialogTexte.GtypTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GtypTypErl, leerErlaubt: true),
                    new KiDialogFeld("kurve", "GebaeudetypKiSicht.Kurve",
                                     KiDialogTexte.GtypKurveName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GtypKurveErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "GebaeudetypKiSicht.Beschreibung",
                                     KiDialogTexte.GtypBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GtypBeschreibungErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_EingStromTyp  ->  TypProfilDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Das Wochen-Stundenprofil eines Bedarfstyps — drei Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.TypProfilKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der TYP ist eine WAHL</b> (KI-D-Q6): Die Liste laedt einen anderen Satz
        /// samt seinen 168 Wochenwerten. Der WOCHENTAG ist es ebenfalls — er sagt,
        /// welche 24 Felder gerade dastehen.
        /// </para>
        /// <para>
        /// <b>Die 7 x 24 Wochenwerte bleiben draussen:</b> ein Raster mit eigenem
        /// Editor samt Kopierweg von Tag zu Tag.
        /// </para>
        /// </remarks>
        private static KiDialog Typprofil()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.TYPPROFIL,
                anzeigename: KiDialogTexte.MaskeTypprofil,
                felder: new[]
                {
                    new KiDialogFeld("typ", "TypProfilKiSicht.Typ",
                                     KiDialogTexte.TprofTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TprofTypErl, leerErlaubt: true),
                    new KiDialogFeld("wochentag", "TypProfilKiSicht.Wochentag",
                                     KiDialogTexte.TprofWochentagName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TprofWochentagErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "TypProfilKiSicht.Beschreibung",
                                     KiDialogTexte.TprofBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.TprofBeschreibungErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_EingDBStromverbraucher  ->  TypStammDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Der Kopfsatz eines Bedarfskatalogs — drei Felder an
        /// <c>EPOS.UI.Dialoge.Bedarf.TypStammDaten</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die einzige Maske dieser Welle OHNE Sichtklasse.</b> Ihr Satz ist
        /// veraenderlich, und jedes der drei Felder bindet unmittelbar ans Markup — der
        /// Katalog haengt sich also an dasselbe Objekt, das die Maske zeigt. Genau
        /// dafuer ist der Weg da; eine Sichtklasse waere hier eine Schicht ohne Zweck.
        /// </para>
        /// <para>
        /// <b>Der VERBRAUCHERTYP ist ein WAHLFELD</b> (KI-D-Q6) und traegt als
        /// Schluessel den Namen des Typkatalogsatzes. Seine Eintraege kennt nur der
        /// Dialog; er reicht sie beim Anmelden als Lieferant herein.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben die zwoelf MONATSWERTE:</b> eine Zahlenfolge ohne
        /// Zeilentyp — ein Katalogfeld traegt EINEN Wert, eine Katalogspalte braucht
        /// Zeilen mit benannten Eigenschaften. Die Pflichtpruefung ueber alle zwoelf
        /// steht dem Assistenten trotzdem offen: Sie ist der Haken „Pruefen".
        /// </para>
        /// </remarks>
        private static KiDialog Typstamm()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.TYPSTAMM,
                anzeigename: KiDialogTexte.MaskeTypstamm,
                felder: new[]
                {
                    new KiDialogFeld("name", "TypStammDaten.Name",
                                     KiDialogTexte.TstammNameName, KiParameterTyp.Text,
                                     KiDialogTexte.TstammNameErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("typ", "TypStammDaten.Typ",
                                     KiDialogTexte.TstammTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.TstammTypErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "TypStammDaten.Beschreibung",
                                     KiDialogTexte.TstammBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.TstammBeschreibungErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // Form_Gebaeude  ->  GebaeudeDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Gebaeudemaske — zehn Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>EINE Maske, zwei Betriebsarten.</b> Im Projekt stehen Projektliste und
        /// Katalog nebeneinander, in der Katalogverwaltung nur der Katalog; gezeichnet
        /// wird dieselbe Komponente mit denselben Bedienelementen. Welche Betriebsart
        /// gilt, sagt das Feld <c>verwaltung</c> — so muss der Assistent nicht raten und
        /// der Katalog nicht zweimal dasselbe fuehren.
        /// </para>
        /// <para>
        /// <b>Setzbar sind die vier FILTERFELDER</b> (Verwendung, Gebaeudeart, Baujahr,
        /// Suchmuster): Sie sind die Eingabefelder dieser Maske, und mit ihnen findet der
        /// Anwender den Satz, den er uebernehmen will. Die fuenf Felder des Detailblocks
        /// sind nur lesbar — sie zeigen, was am markierten Satz steht.
        /// </para>
        /// <para>
        /// <b>Die Werte der Zuordnung stehen NICHT hier</b>, sondern unter
        /// <see cref="KiMaskennamen.GEBAEUDE_WOHNFLAECHE"/>: Wohnflaeche,
        /// Jahresnutzungsgrad, Art der Angabe und die dezentrale Warmwasserbereitung
        /// pflegt der Knopf „Aendern…" in jener Maske. Sie hier zu setzen hiesse, eine
        /// Zahl zu aendern, die der Anwender auf dieser Maske nicht nachlesen kann.
        /// </para>
        /// </remarks>
        private static KiDialog Gebaeude()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE,
                anzeigename: KiDialogTexte.MaskeGebaeude,
                felder: new[]
                {
                    // ---- Der Filter ueber die Katalogliste --------------------------
                    new KiDialogFeld("verwendung", "GebaeudeKiSicht.Verwendung",
                                     KiDialogTexte.GebVerwendungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebVerwendungErl),
                    new KiDialogFeld("filter_gebaeudeart", "GebaeudeKiSicht.FilterGebaeudeart",
                                     KiDialogTexte.GebFilterArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebFilterArtErl, leerErlaubt: true),
                    new KiDialogFeld("filter_baujahr", "GebaeudeKiSicht.FilterBaujahr",
                                     KiDialogTexte.GebFilterBaujahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebFilterBaujahrErl, leerErlaubt: true),
                    new KiDialogFeld("suche", "GebaeudeKiSicht.Suche",
                                     KiDialogTexte.GebSucheName, KiParameterTyp.Text,
                                     KiDialogTexte.GebSucheErl, leerErlaubt: true),

                    // ---- Der Detailblock des markierten Satzes ----------------------
                    new KiDialogFeld("name", "GebaeudeKiSicht.Name",
                                     KiDialogTexte.GebNameName, KiParameterTyp.Text,
                                     KiDialogTexte.GebNameErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("gebaeudeart", "GebaeudeKiSicht.Gebaeudeart",
                                     KiDialogTexte.GebArtName, KiParameterTyp.Text,
                                     KiDialogTexte.GebArtErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("beschreibung", "GebaeudeKiSicht.Beschreibung",
                                     KiDialogTexte.GebBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GebBeschreibungErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("wohnflaeche", "GebaeudeKiSicht.Wohnflaeche",
                                     KiDialogTexte.GebWohnflaecheName, KiParameterTyp.Text,
                                     KiDialogTexte.GebWohnflaecheErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("angabeart", "GebaeudeKiSicht.Angabeart",
                                     KiDialogTexte.GebAngabeartName, KiParameterTyp.Text,
                                     KiDialogTexte.GebAngabeartErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("verwaltung", "GebaeudeKiSicht.Verwaltung",
                                     KiDialogTexte.GebVerwaltungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebVerwaltungErl, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_GebWohnflaeche  ->  GebaeudeWohnflaecheDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Die Wohn-/Nutzflaechenangabe eines Projektgebaeudes — neun Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeWohnflaecheKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Hier stehen die vier Werte der ZUORDNUNG</b>, die
        /// <see cref="KiMaskennamen.GEBAEUDE"/> nur anzeigt: die Bedarfsart, die Zahl
        /// dazu, der Jahresnutzungsgrad des Kessels und die dezentrale
        /// Warmwasserbereitung. Die fuenf Kopfangaben sind nur lesbar — sie kommen aus
        /// dem Katalogsatz und werden dort gepflegt.
        /// </para>
        /// <para>
        /// <b>Die BEDARFSART ist ein WAHLFELD</b> (KI-D-Q6) und traegt als Schluessel
        /// ihren Listenplatz. Sie entscheidet die Einheit der Zahl daneben und den
        /// Rechenweg: Wohnflaeche oder Ruecktrechnung aus einem Verbrauch.
        /// </para>
        /// <para>
        /// <b>Die Maske SCHREIBT nicht, sie entscheidet</b> — der einzige Weg zum Wirt
        /// ist OK, und OK schliesst sie; <c>dialog_speichern</c> lehnt deshalb benannt ab.
        /// </para>
        /// </remarks>
        private static KiDialog GebaeudeWohnflaeche()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_WOHNFLAECHE,
                anzeigename: KiDialogTexte.MaskeGebaeudeWohnflaeche,
                felder: new[]
                {
                    new KiDialogFeld("bedarfsart", "GebaeudeWohnflaecheKiSicht.Bedarfsart",
                                     KiDialogTexte.GebwBedarfsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebwBedarfsartErl, leerErlaubt: true),
                    new KiDialogFeld("wert", "GebaeudeWohnflaecheKiSicht.Wert",
                                     KiDialogTexte.GebwWertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebwWertErl),
                    new KiDialogFeld("jahresnutzungsgrad",
                                     "GebaeudeWohnflaecheKiSicht.Jahresnutzungsgrad",
                                     KiDialogTexte.GebwNutzungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebwNutzungsgradErl),
                    new KiDialogFeld("dezentral_warmwasser",
                                     "GebaeudeWohnflaecheKiSicht.DezentralWarmwasser",
                                     KiDialogTexte.GebwDezentralName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.GebwDezentralErl),

                    new KiDialogFeld("gebaeudename", "GebaeudeWohnflaecheKiSicht.Gebaeudename",
                                     KiDialogTexte.GebwNameName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwNameErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("gebaeudeart", "GebaeudeWohnflaecheKiSicht.Gebaeudeart",
                                     KiDialogTexte.GebwArtName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwArtErl, leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("beschreibung", "GebaeudeWohnflaecheKiSicht.Beschreibung",
                                     KiDialogTexte.GebwBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwBeschreibungErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("baujahr", "GebaeudeWohnflaecheKiSicht.Baujahr",
                                     KiDialogTexte.GebwBaujahrName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwBaujahrErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("angabeart", "GebaeudeWohnflaecheKiSicht.Angabeart",
                                     KiDialogTexte.GebwAngabeartName, KiParameterTyp.Text,
                                     KiDialogTexte.GebwAngabeartErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Gebaeude1  ->  GebaeudeKatalogDialog   (Welle KI-F3)
        // =====================================================================

        /// <summary>
        /// Der Gebaeude-Katalogeditor — 37 Felder aus
        /// <c>EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogKiSicht</c>: die groesste Maske des
        /// Katalogs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Zwei Reiterblaetter, ein Feldsatz.</b> Kenngroessen, Flaechen und U-Werte
        /// binden unmittelbar an den Satz; Raumtemperaturen, Waermebruecken,
        /// Anschlussmasse und Luftwechsel fuehrt die Maske in einem EIGENEN Stand und
        /// gibt sie erst mit „Werte uebernehmen" in den Satz. Die Sichtklasse legt beide
        /// unter einem Namen zusammen — dieselbe Bauart wie bei der
        /// Komponentenkonfiguration.
        /// </para>
        /// <para>
        /// <b>Fuenf Klapplisten sind WAHLFELDER</b> (KI-D-Q6): Gebaeudetyp und
        /// Gebaeudeart tragen den Namen des Katalogsatzes als Schluessel, Baujahr und
        /// Bauart ihren Listenplatz, die VERWENDUNG ihren Steuerwert. Die Bauart zieht
        /// die Bauweise nach — derselbe Weg, den die Klappliste geht.
        /// </para>
        /// <para>
        /// <b>Draussen bleiben die sechzehn FERIENZAHLEN</b> (je vier Zeitraeume Beginn
        /// und Ende, Tag und Monat): Sie sind zwei Zahlenfolgen ohne Zeilentyp, und ein
        /// Katalogfeld traegt EINEN Wert. Geprueft werden sie ohnehin nur im Verbund —
        /// vier Regeln ueber alle acht Paare. Ebenso draussen: die BAUWEISE, die aus
        /// Bauart und Wohnflaeche gerechnet wird, und die Liste der
        /// Brauchwasserprofile, die eine eigene Maske pflegt.
        /// </para>
        /// </remarks>
        private static KiDialog GebaeudeKatalog()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.GEBAEUDE_KATALOG,
                anzeigename: KiDialogTexte.MaskeGebaeudeKatalog,
                felder: new[]
                {
                    // ---- Kenngroessen ----------------------------------------------
                    new KiDialogFeld("name", "GebaeudeKatalogKiSicht.Name",
                                     KiDialogTexte.GebkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.GebkNameErl),
                    new KiDialogFeld("gebaeudetyp", "GebaeudeKatalogKiSicht.Typ",
                                     KiDialogTexte.GebkTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkTypErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "GebaeudeKatalogKiSicht.Beschreibung",
                                     KiDialogTexte.GebkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.GebkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("gebaeudeart", "GebaeudeKatalogKiSicht.Gebaeudeart",
                                     KiDialogTexte.GebkArtName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkArtErl, leerErlaubt: true),
                    new KiDialogFeld("baualtersklasse", "GebaeudeKatalogKiSicht.Baualtersklasse",
                                     KiDialogTexte.GebkBaujahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkBaujahrErl),
                    new KiDialogFeld("verwendung", "GebaeudeKatalogKiSicht.Verwendung",
                                     KiDialogTexte.GebkVerwendungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkVerwendungErl, leerErlaubt: true),
                    new KiDialogFeld("bauart", "GebaeudeKatalogKiSicht.Bauart",
                                     KiDialogTexte.GebkBauartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.GebkBauartErl),
                    new KiDialogFeld("wohnflaeche", "GebaeudeKatalogKiSicht.WohnflaecheGesamt",
                                     KiDialogTexte.GebkWohnflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWohnflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("flaeche_nutzer", "GebaeudeKatalogKiSicht.FlaecheNutzer",
                                     KiDialogTexte.GebkFlaecheNutzerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFlaecheNutzerErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("waermegewinne", "GebaeudeKatalogKiSicht.Waermegewinne",
                                     KiDialogTexte.GebkWaermegewinneName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWaermegewinneErl,
                                     einheit: KiDialogTexte.EINHEIT_W),
                    new KiDialogFeld("fensterdurchlassgrad",
                                     "GebaeudeKatalogKiSicht.Fensterdurchlassgrad",
                                     KiDialogTexte.GebkDurchlassgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkDurchlassgradErl),
                    new KiDialogFeld("raumhoehe", "GebaeudeKatalogKiSicht.Raumhoehe",
                                     KiDialogTexte.GebkRaumhoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkRaumhoeheErl,
                                     einheit: KiDialogTexte.EINHEIT_METER),

                    // ---- Flaechen ---------------------------------------------------
                    new KiDialogFeld("fensterflaeche_nord",
                                     "GebaeudeKatalogKiSicht.FensterflaecheNord",
                                     KiDialogTexte.GebkFfNordName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFfNordErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("fensterflaeche_sued",
                                     "GebaeudeKatalogKiSicht.FensterflaecheSued",
                                     KiDialogTexte.GebkFfSuedName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFfSuedErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("fensterflaeche_ostwest",
                                     "GebaeudeKatalogKiSicht.FensterflaecheOstWest",
                                     KiDialogTexte.GebkFfOstWestName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFfOstWestErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("flaeche_aussenwand",
                                     "GebaeudeKatalogKiSicht.FlaecheAussenwand",
                                     KiDialogTexte.GebkAussenwandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAussenwandErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("dachflaeche", "GebaeudeKatalogKiSicht.Dachflaeche",
                                     KiDialogTexte.GebkDachflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkDachflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("grundflaeche", "GebaeudeKatalogKiSicht.Grundflaeche",
                                     KiDialogTexte.GebkGrundflaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkGrundflaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),
                    new KiDialogFeld("sonstige_flaechen",
                                     "GebaeudeKatalogKiSicht.SonstigeFlaechen",
                                     KiDialogTexte.GebkSonstFlaechenName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkSonstFlaechenErl,
                                     einheit: KiDialogTexte.EINHEIT_M2),

                    // ---- U-Werte ----------------------------------------------------
                    new KiDialogFeld("u_aussenwand", "GebaeudeKatalogKiSicht.UWertAussenwand",
                                     KiDialogTexte.GebkUAussenwandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUAussenwandErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_fenster", "GebaeudeKatalogKiSicht.UWertFenster",
                                     KiDialogTexte.GebkUFensterName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUFensterErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_dachflaeche", "GebaeudeKatalogKiSicht.UWertDachflaeche",
                                     KiDialogTexte.GebkUDachName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUDachErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_grundflaeche", "GebaeudeKatalogKiSicht.UWertGrundflaeche",
                                     KiDialogTexte.GebkUGrundName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUGrundErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),
                    new KiDialogFeld("u_sonstiges", "GebaeudeKatalogKiSicht.UWertSonstiges",
                                     KiDialogTexte.GebkUSonstigesName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkUSonstigesErl,
                                     einheit: KiDialogTexte.EINHEIT_W_M2K),

                    // ---- Raumtemperaturen (zweites Reiterblatt) ---------------------
                    new KiDialogFeld("soll_tag", "GebaeudeKatalogKiSicht.SollTag",
                                     KiDialogTexte.GebkSollTagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkSollTagErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("nachtabsenkung", "GebaeudeKatalogKiSicht.Nachtabsenkung",
                                     KiDialogTexte.GebkNachtName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkNachtErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("max_temperatur", "GebaeudeKatalogKiSicht.MaxTemperatur",
                                     KiDialogTexte.GebkMaxTemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkMaxTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("wochenendabsenkung",
                                     "GebaeudeKatalogKiSicht.Wochenendabsenkung",
                                     KiDialogTexte.GebkWochenendeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWochenendeErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("soll_ferien", "GebaeudeKatalogKiSicht.SollFerien",
                                     KiDialogTexte.GebkSollFerienName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkSollFerienErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Waermebruecken und Anschlussmasse --------------------------
                    new KiDialogFeld("wbvk_fenster_wand",
                                     "GebaeudeKatalogKiSicht.WbvkFensterWand",
                                     KiDialogTexte.GebkFensterWandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkFensterWandErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("wbvk_wand_dach", "GebaeudeKatalogKiSicht.WbvkWandDach",
                                     KiDialogTexte.GebkWandDachName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWandDachErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("wbvk_aussenwand_keller",
                                     "GebaeudeKatalogKiSicht.WbvkAussenwandKeller",
                                     KiDialogTexte.GebkWandKellerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkWandKellerErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("anschluss_fenster_wand",
                                     "GebaeudeKatalogKiSicht.AnschlussFensterWand",
                                     KiDialogTexte.GebkAnschlussFensterName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAnschlussFensterErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("anschluss_wand_dach",
                                     "GebaeudeKatalogKiSicht.AnschlussWandDach",
                                     KiDialogTexte.GebkAnschlussDachName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAnschlussDachErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("anschluss_aussenwand_keller",
                                     "GebaeudeKatalogKiSicht.AnschlussAussenwandKeller",
                                     KiDialogTexte.GebkAnschlussKellerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkAnschlussKellerErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("luftwechselrate", "GebaeudeKatalogKiSicht.Luftwechselrate",
                                     KiDialogTexte.GebkLuftwechselName, KiParameterTyp.Zahl,
                                     KiDialogTexte.GebkLuftwechselErl,
                                     einheit: KiDialogTexte.EINHEIT_1_H, leerErlaubt: true),

                    new KiDialogFeld("betriebsart", "GebaeudeKatalogKiSicht.Betriebsart",
                                     KiDialogTexte.GebkBetriebsartName, KiParameterTyp.Text,
                                     KiDialogTexte.GebkBetriebsartErl, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("werte_uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfWerteUebernehmen),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("speichern", "btn_Speichern",
                                      KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("beenden", "btn_Beenden", KiDialogTexte.KnopfBeenden)
                });
        }

        // =====================================================================
        // KomponentenKonfiguration  ->  KomponentenKonfigurationDialog (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Konfiguration EINER Komponente der Simulation — elf Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.KomponentenKonfigurationKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die dritte Maske ohne <c>Form_</c>-Vorsilbe:</b> Sie hatte nie eine
        /// WinForms-Fassung. Bis zum 16.09.2026 standen ihre Felder offen an den
        /// Erzeugerkarten; seither stehen sie hinter einem Knopf je Karte.
        /// </para>
        /// <para>
        /// <b>EINE Maske, ZWEI Arbeitskopien — und deshalb ein Sichtmodell.</b> Beim
        /// Heizkessel und beim BHKW zeigt sie die projektweiten Laufparameter
        /// (<c>Tab_Einstellungen</c>), bei der Waermepumpe die Konfiguration DIESER
        /// Anlage. Der Katalog nennt EIN Daten-Objekt vor dem Punkt; die Sichtklasse
        /// legt beide Staende unter einem Namen zusammen. Hinzu kommt, dass
        /// <c>ParameterDaten</c> oeffentliche FELDER traegt und keine Eigenschaften —
        /// die Maskenbruecke loest ueber <c>GetProperty</c> auf und faende dort nichts.
        /// </para>
        /// <para>
        /// <b>Die acht Felder der Waermepumpe stehen ZWEIMAL im Katalog</b> — hier
        /// und unter <see cref="KiMaskennamen.WAERMEPUMPE_ANLAGE"/>. Das ist richtig:
        /// Eine Maske ist, was offen ist, und der Anwender sieht dieselben Werte
        /// einmal im Anlagendialog und einmal in dieser Konfiguration. Welche Maske
        /// gerade gilt, sagt die Anmeldung und nicht der Katalog.
        /// </para>
        /// <para>
        /// <b>Die drei Klapplisten sind WAHLFELDER</b> (KI-F1b, KI-D-Q6): die
        /// BHKW-Betriebsart (Steuerwert 0/1/2 des Bestands), die Betriebsart der
        /// Waermepumpe (Steuerwert als Schluessel UND Text) und der ENERGIETRAEGER
        /// (<c>CarrierId</c>, Schluessel ist die Id des Katalogsatzes). Ihre Eintraege
        /// liefert die Maske zur Laufzeit — der Traegerkatalog haengt am Gewerk.
        /// </para>
        /// </remarks>
        private static KiDialog Komponentenkonfiguration()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KOMPONENTENKONFIGURATION,
                anzeigename: KiDialogTexte.MaskeKomponentenkonfiguration,
                felder: new[]
                {
                    // ---- Die projektweiten Laufparameter ----------------------------
                    new KiDialogFeld("kessel_bereitschaft",
                                     "KomponentenKonfigurationKiSicht.Bereitschaft",
                                     KiDialogTexte.KkonfBereitschaftName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KkonfBereitschaftErl,
                                     einheit: KiDialogTexte.EINHEIT_H_A),
                    new KiDialogFeld("bhkw_betriebsart",
                                     "KomponentenKonfigurationKiSicht.BhkwBetriebsart",
                                     KiDialogTexte.KkonfBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KkonfBetriebsartErl),
                    new KiDialogFeld("bhkw_leistungsgrenze",
                                     "KomponentenKonfigurationKiSicht.BhkwLeistungsgrenze",
                                     KiDialogTexte.KkonfGrenzeName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.KkonfGrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),

                    // ---- Die Konfiguration DIESER Waermepumpe -----------------------
                    new KiDialogFeld("heizstab", "KomponentenKonfigurationKiSicht.Heizstab",
                                     KiDialogTexte.WpaHeizstabName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaHeizstabErl),
                    new KiDialogFeld("sperrzeit", "KomponentenKonfigurationKiSicht.Sperrung",
                                     KiDialogTexte.WpaSperrungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaSperrungErl),
                    new KiDialogFeld("sperrzeit_von",
                                     "KomponentenKonfigurationKiSicht.SperrzeitVon",
                                     KiDialogTexte.WpaSperrzeitVonName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitVonErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("sperrzeit_bis",
                                     "KomponentenKonfigurationKiSicht.SperrzeitBis",
                                     KiDialogTexte.WpaSperrzeitBisName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitBisErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("bivalenter_betrieb",
                                     "KomponentenKonfigurationKiSicht.BivalenterBetrieb",
                                     KiDialogTexte.WpaBivalentName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaBivalentErl),
                    new KiDialogFeld("betriebsart", "KomponentenKonfigurationKiSicht.Betriebsart",
                                     KiDialogTexte.WpaBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBetriebsartErl, leerErlaubt: true),
                    new KiDialogFeld("energietraeger",
                                     "KomponentenKonfigurationKiSicht.Energietraeger",
                                     KiDialogTexte.KkonfTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KkonfTraegerErl),
                    new KiDialogFeld("bivalenztemperatur",
                                     "KomponentenKonfigurationKiSicht.Abschaltpunkt",
                                     KiDialogTexte.WpaAbschaltpunktName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaAbschaltpunktErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Quellprofil  ->  QuellprofilDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Der KOPF eines Quellprofils — vier Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.QuellprofilKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nur der Kopf, und das ist die Aussage.</b> Die zwoelf Monatswerte und die
        /// 365 bzw. 8 760 Reihenwerte sind Zahlenfolgen ohne Zeilentyp: Ein Katalogfeld
        /// traegt EINEN Wert, eine Katalogspalte braucht Zeilen mit benannten
        /// Eigenschaften (<c>KiFeldsammlung</c>). Gepflegt werden sie ohnehin ueber
        /// „Alle Werte gleich setzen…" und den CSV-Weg und nicht Zelle fuer Zelle.
        /// </para>
        /// <para>
        /// <b>Das gewaehlte PROFIL ist ein WAHLFELD</b> (KI-F1b, KI-D-Q6): Seine
        /// Eintraege sind die Profile des Projekts samt dem Eintrag 0 „neues Profil" —
        /// auch er ist eine gueltige Wahl der Maske. Sie LAEDT den Profilkopf samt
        /// Werten; das ist derselbe Weg, den die Klappliste nimmt.
        /// </para>
        /// <para>
        /// <b>Die BETRIEBSART ist ein WAHLFELD</b> und traegt als Schluessel den
        /// Steuerwert des Bestands (Monat, Tag, Stunde), als Text den Eintrag der
        /// Klappliste. Sie entscheidet, wie viele Werte das Profil fuehrt.
        /// </para>
        /// </remarks>
        private static KiDialog Quellprofil()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.QUELLPROFIL,
                anzeigename: KiDialogTexte.MaskeQuellprofil,
                felder: new[]
                {
                    new KiDialogFeld("bezeichnung", "QuellprofilKiSicht.Bezeichnung",
                                     KiDialogTexte.QprofBezeichnungName, KiParameterTyp.Text,
                                     KiDialogTexte.QprofBezeichnungErl),
                    new KiDialogFeld("beschreibung", "QuellprofilKiSicht.Beschreibung",
                                     KiDialogTexte.QprofBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.QprofBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("betriebsart", "QuellprofilKiSicht.Betriebsart",
                                     KiDialogTexte.QprofBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QprofBetriebsartErl, leerErlaubt: true),
                    new KiDialogFeld("profil", "QuellprofilKiSicht.Profil",
                                     KiDialogTexte.QprofProfilName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QprofProfilErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Waermesenke  ->  WaermesenkeDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Waermesenken einer Anlage — neun Felder der GEWAEHLTEN Zeile aus
        /// <c>EPOS.UI.Dialoge.Simulation.WaermesenkeKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Daten-Objekt ist eine ZEILE und kein Dialogstand</b> — dieselbe
        /// Bauart wie bei <see cref="Photovoltaik"/>: Der Dialog fuehrt eine Liste, und
        /// angemeldet ist, was in der gewaehlten Zeile steht. Ist keine gewaehlt, sind
        /// die Felder leer — derselbe Zustand, den der Anwender sieht.
        /// </para>
        /// <para>
        /// <b>Der gewaehlte SPEICHER ist ein WAHLFELD</b> (KI-F1b, KI-D-Q6): Seine
        /// Eintraege sind die Pufferspeicher des Projekts, die der Dialog zur Laufzeit
        /// liefert. Die gesperrten GRUPPENKOEPFE der Klappliste (negative Id) stehen
        /// NICHT darin — sie sind Ueberschriften und waren nie eine gueltige Wahl.
        /// </para>
        /// <para>
        /// <b>Der PARALLELVERBUND bleibt draussen</b> — eine MENGE von Verweisen; ein
        /// Katalogfeld traegt EINEN Wert. Der RANG der Zeile bleibt ebenfalls draussen:
        /// Er wird nicht eingegeben, sondern ueber „nach oben"/„nach unten" verschoben,
        /// und er traegt keine eigene Beschriftung.
        /// </para>
        /// <para>
        /// <b>„Senke hinzufuegen", „nach oben", „nach unten" und „Pufferspeicher
        /// anlegen…" sind keine Knoepfe dieser Liste</b> — sie aendern die LISTE und
        /// nicht einen Wert; „Entfernen" ist ueberdies ein Loeschknopf und damit nicht
        /// deklarierbar (Bauartsperre in <see cref="KiDialogKnopf"/>).
        /// </para>
        /// </remarks>
        private static KiDialog Waermesenke()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WAERMESENKE,
                anzeigename: KiDialogTexte.MaskeWaermesenke,
                felder: new[]
                {
                    new KiDialogFeld("ziel", "WaermesenkeKiSicht.Ziel",
                                     KiDialogTexte.WsenZielName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenZielErl, leerErlaubt: true),
                    new KiDialogFeld("bedarfsart", "WaermesenkeKiSicht.Bedarfsart",
                                     KiDialogTexte.WsenBedarfsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenBedarfsartErl, leerErlaubt: true),
                    new KiDialogFeld("speicher", "WaermesenkeKiSicht.Speicher",
                                     KiDialogTexte.WsenSpeicherName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenSpeicherErl),
                    new KiDialogFeld("ladeprioritaet", "WaermesenkeKiSicht.Ladeprioritaet",
                                     KiDialogTexte.WsenLadeprioName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenLadeprioErl),
                    new KiDialogFeld("ladeprioritaet_pv", "WaermesenkeKiSicht.LadeprioritaetPv",
                                     KiDialogTexte.WsenLadeprioPvName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WsenLadeprioPvErl),
                    new KiDialogFeld("ladegrenze_aktiv", "WaermesenkeKiSicht.LadegrenzeAktiv",
                                     KiDialogTexte.WsenLadegrenzeAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WsenLadegrenzeAktivErl),
                    new KiDialogFeld("ladegrenze", "WaermesenkeKiSicht.Ladegrenze",
                                     KiDialogTexte.WsenLadegrenzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WsenLadegrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("einspeisehoehe_aktiv",
                                     "WaermesenkeKiSicht.EinspeisehoeheAktiv",
                                     KiDialogTexte.WsenHoeheAktivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WsenHoeheAktivErl),
                    new KiDialogFeld("einspeisehoehe", "WaermesenkeKiSicht.Einspeisehoehe",
                                     KiDialogTexte.WsenHoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WsenHoeheErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_QuelleErdreich  ->  QuelleErdreichDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Waermequelle ERDREICH einer Sole-/Wasser-Waermepumpe — acht Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.QuelleErdreichKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ZWEI Quellsysteme, ein Feldsatz.</b> Der Erdkollektor fuehrt Verlegetiefe
        /// und Flaeche, die Erdsonde Laenge je Sonde und Anzahl; die Wahl zwischen
        /// beiden ist ein Wahrheitswert (<c>Erdsonde</c>). Sie SPERRT nur — der andere
        /// Zweig behaelt seine Werte, und deshalb stehen alle vier Zahlen im Katalog
        /// und nicht nur die des gewaehlten Zweigs.
        /// </para>
        /// <para>
        /// <b>BODENTYP und KLIMAZONE sind WAHLFELDER</b> (KI-F1b, KI-D-Q6): Ihre
        /// Eintraege liefert die Maske zur Laufzeit — der Bodenkatalog nach VDI 4640
        /// und die Zonenliste. Der Bodentyp traegt dabei den KATALOGSCHLUESSEL und
        /// nicht seinen Listenplatz (Abweichung A-3): Wird der Katalog umsortiert,
        /// zeigte ein Platz danach auf den falschen Boden. Die Klimazone traegt ihre
        /// Nummer (1…15, 0 = nicht zugeordnet).
        /// </para>
        /// <para>
        /// <b>„Simulation starten" ist kein Knopf dieser Liste.</b> Er rechnet lange,
        /// meldet Fortschritt und laesst sich abbrechen; solche Wege gehoeren in das
        /// Aktionsregister (Stufe 2), nicht in die Positivliste einer Maske. Der
        /// Kartenknopf „…" traegt nur ein Zeichen und keine Beschriftung.
        /// </para>
        /// </remarks>
        private static KiDialog QuelleErdreich()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.QUELLE_ERDREICH,
                anzeigename: KiDialogTexte.MaskeQuelleErdreich,
                felder: new[]
                {
                    new KiDialogFeld("erdsonde", "QuelleErdreichKiSicht.Erdsonde",
                                     KiDialogTexte.QerdSondeName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.QerdSondeErl),
                    new KiDialogFeld("verlegetiefe", "QuelleErdreichKiSicht.Verlegetiefe",
                                     KiDialogTexte.QerdTiefeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdTiefeErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("flaeche", "QuelleErdreichKiSicht.Flaeche",
                                     KiDialogTexte.QerdFlaecheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdFlaecheErl,
                                     einheit: KiDialogTexte.EINHEIT_M2, leerErlaubt: true),
                    new KiDialogFeld("laenge_sonde", "QuelleErdreichKiSicht.LaengeSonde",
                                     KiDialogTexte.QerdLaengeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdLaengeErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("anzahl_sonden", "QuelleErdreichKiSicht.AnzahlSonden",
                                     KiDialogTexte.QerdAnzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.QerdAnzahlErl, leerErlaubt: true),
                    new KiDialogFeld("spreizung", "QuelleErdreichKiSicht.Spreizung",
                                     KiDialogTexte.QerdSpreizungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QerdSpreizungErl,
                                     einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true),

                    // ---- Die zwei Klapplisten des Standorts (KI-F1b, KI-D-Q6) ----
                    new KiDialogFeld("bodentyp", "QuelleErdreichKiSicht.Bodentyp",
                                     KiDialogTexte.QerdBodentypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QerdBodentypErl),
                    new KiDialogFeld("klimazone", "QuelleErdreichKiSicht.Klimazone",
                                     KiDialogTexte.QerdKlimazoneName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QerdKlimazoneErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_QuellePufferspeicher  ->  QuellePufferspeicherDialog  (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Waermequelle PUFFERSPEICHER — neun Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.QuellePufferspeicherKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ZWEI Erzeugerarten in einer Maske.</b> Die Waermepumpe zieht
        /// Verdampferwaerme aus dem Speicher und pflegt dazu Quelltemperatur,
        /// Spreizung, Regeneration und „Quelle unbegrenzt"; der Heizkessel nimmt seine
        /// Eintrittstemperatur aus dem Puffer und pflegt dafuer den Temperaturbezug
        /// samt festem Vorlauf und Ruecklauf. Die Entnahmehoehe gilt beiden. Welche
        /// Felder sichtbar sind, entscheidet die Erzeugerart; die jeweils andere Seite
        /// bleibt unangetastet.
        /// </para>
        /// <para>
        /// <b>Der gewaehlte PUFFER ist ein WAHLFELD</b> (KI-F1b, KI-D-Q6): Seine
        /// Eintraege sind die Pufferliste DIESES Projekts, und die kennt nur der
        /// Dialog — er liefert sie zur Laufzeit. Der Schluessel ist die Id der Zeile,
        /// der Text ihre Anzeige.
        /// </para>
        /// <para>
        /// <b>„Pufferspeicher anlegen…" ist kein Knopf dieser Liste.</b> Er oeffnet
        /// eine zweite Maske als Ueberlagerung, setzt aber keinen Wert; die
        /// Positivliste dieser Welle fuehrt nur OK und Abbrechen.
        /// </para>
        /// </remarks>
        private static KiDialog QuellePufferspeicher()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.QUELLE_PUFFERSPEICHER,
                anzeigename: KiDialogTexte.MaskeQuellePuffer,
                felder: new[]
                {
                    new KiDialogFeld("quelltemperatur",
                                     "QuellePufferspeicherKiSicht.Quelltemperatur",
                                     KiDialogTexte.QpufTemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufTemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("spreizung", "QuellePufferspeicherKiSicht.Spreizung",
                                     KiDialogTexte.QpufSpreizungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufSpreizungErl,
                                     einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true),
                    new KiDialogFeld("regeneration", "QuellePufferspeicherKiSicht.Regeneration",
                                     KiDialogTexte.QpufRegenerationName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufRegenerationErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("unbegrenzt", "QuellePufferspeicherKiSicht.Unbegrenzt",
                                     KiDialogTexte.QpufUnbegrenztName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.QpufUnbegrenztErl),
                    new KiDialogFeld("temperatur_fest",
                                     "QuellePufferspeicherKiSicht.TemperaturFest",
                                     KiDialogTexte.QpufFestName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.QpufFestErl),
                    new KiDialogFeld("vorlauf", "QuellePufferspeicherKiSicht.Vorlauf",
                                     KiDialogTexte.QpufVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.QpufVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "QuellePufferspeicherKiSicht.Ruecklauf",
                                     KiDialogTexte.QpufRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.QpufRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("anschlusshoehe",
                                     "QuellePufferspeicherKiSicht.Anschlusshoehe",
                                     KiDialogTexte.QpufAnschlusshoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.QpufAnschlusshoeheErl, leerErlaubt: true),

                    // ---- Der gewaehlte Speicher (KI-F1b, KI-D-Q6) ------------------
                    new KiDialogFeld("puffer", "QuellePufferspeicherKiSicht.Puffer",
                                     KiDialogTexte.QpufPufferName, KiParameterTyp.Wahl,
                                     KiDialogTexte.QpufPufferErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PufferSp_Projekt  ->  PufferSpProjektDialog   (Welle KI-F2)
        // =====================================================================

        /// <summary>
        /// Die Pufferspeicher-Verwaltung des Projekts — zweiundzwanzig Felder aus
        /// <c>EPOS.UI.Dialoge.Simulation.PufferSpProjektKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ein SICHTMODELL und nicht das Daten-Objekt.</b> Dieser Dialog
        /// bekommt keinen veraenderlichen Satz herein und gibt keinen heraus: Was
        /// hereinkommt, sind zwei Ids, und was der Anwender bearbeitet, steht bis zum
        /// „Uebernehmen" in den Eingabefeldern der Maske — erst dort entsteht der
        /// unveraenderliche Record <c>PspEingaben</c>. Ein daran angemeldeter Katalog
        /// zeigte dem Assistenten den Stand von vorhin und setzte ins Leere. Dieselbe
        /// Lage wie bei der Stromspeicher- und der Simulationsansicht, dieselbe
        /// Antwort: eine flache Sichtklasse ueber die lebenden Felder.
        /// </para>
        /// <para>
        /// <b>Die drei ENTNAHMEHOEHEN stehen im Katalog</b> (KI-F1b): Die Maske
        /// beschriftet sie seither in beiden Sprachen
        /// (<c>PSP_LABEL_ENTNAHME_HEIZUNG</c> und die zwei Geschwister), und damit ist
        /// ihr Anzeigename abgelesen und nicht erfunden.
        /// </para>
        /// <para>
        /// <b>Die NUTZUNG steht als DREI Wahrheitswerte da.</b> Auf der Maske ist sie
        /// EINE Mehrfachwahl aus drei Klassen; ein Katalogfeld traegt EINEN Wert
        /// (<c>KiDialogFeld</c>). Deshalb fragt der Katalog je Klasse „ist sie im
        /// Set?" — und das Setzen nimmt sie ueber denselben Rueckruf hinein oder
        /// heraus, den ein Klick nimmt.
        /// </para>
        /// <para>
        /// <b>Der Katalogsatz „Aus Katalog" bleibt draussen.</b> Seine Id ist nur der
        /// Anzeigeindex der Klappliste, und das Waehlen KOPIERT Volumen und Verluste
        /// in die Eingabefelder — das ist ein Ladevorgang und kein Feldwert. Wer ihn
        /// als Feld deklarierte, boete dem Assistenten an, „einen Wert zu setzen", der
        /// in Wahrheit zwei andere Felder ueberschreibt.
        /// </para>
        /// <para>
        /// <b>Knoepfe: OK, Abbrechen und UEBERNEHMEN.</b> „Entfernen" ist ein
        /// Loeschknopf und damit nicht deklarierbar (Bauartsperre in
        /// <see cref="KiDialogKnopf"/>); „Neuer Pufferspeicher" und „Katalog
        /// ansehen…" wechseln den Stand der Maske, ohne einen Wert zu setzen, und
        /// gehoeren nicht in die Positivliste dieser Welle.
        /// </para>
        /// </remarks>
        private static KiDialog PufferspeicherVerwaltung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PUFFERSPEICHER_VERWALTUNG,
                anzeigename: KiDialogTexte.MaskePufferSpVerwaltung,
                felder: new[]
                {
                    new KiDialogFeld("bezeichner", "PufferSpProjektKiSicht.Bezeichner",
                                     KiDialogTexte.PspvBezeichnerName, KiParameterTyp.Text,
                                     KiDialogTexte.PspvBezeichnerErl),
                    new KiDialogFeld("volumen", "PufferSpProjektKiSicht.Volumen",
                                     KiDialogTexte.PspvVolumenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvVolumenErl,
                                     einheit: KiDialogTexte.EINHEIT_LITER, leerErlaubt: true),
                    new KiDialogFeld("bereitschaftsverluste",
                                     "PufferSpProjektKiSicht.Bereitschaftsverluste",
                                     KiDialogTexte.PspvVerlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvVerlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH_24H, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "PufferSpProjektKiSicht.Vorlauf",
                                     KiDialogTexte.PspvVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "PufferSpProjektKiSicht.Ruecklauf",
                                     KiDialogTexte.PspvRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("einschaltschwelle",
                                     "PufferSpProjektKiSicht.Einschaltschwelle",
                                     KiDialogTexte.PspvSchwelleEinName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvSchwelleEinErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("abschaltschwelle",
                                     "PufferSpProjektKiSicht.Abschaltschwelle",
                                     KiDialogTexte.PspvSchwelleAusName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvSchwelleAusErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("schwelle_nachrangig",
                                     "PufferSpProjektKiSicht.SchwelleNachrangig",
                                     KiDialogTexte.PspvSchwelleNachrangName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvSchwelleNachrangErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("mindestfuellstand",
                                     "PufferSpProjektKiSicht.Mindestfuellstand",
                                     KiDialogTexte.PspvMindestfuellstandName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvMindestfuellstandErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("schichten", "PufferSpProjektKiSicht.Schichten",
                                     KiDialogTexte.PspvSchichtenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspvSchichtenErl, leerErlaubt: true),
                    new KiDialogFeld("hoehe", "PufferSpProjektKiSicht.Hoehe",
                                     KiDialogTexte.PspvHoeheName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvHoeheErl,
                                     einheit: KiDialogTexte.EINHEIT_METER, leerErlaubt: true),
                    new KiDialogFeld("lambda", "PufferSpProjektKiSicht.Lambda",
                                     KiDialogTexte.PspvLambdaName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvLambdaErl,
                                     einheit: KiDialogTexte.EINHEIT_W_MK, leerErlaubt: true),
                    new KiDialogFeld("nutztemperatur_bw",
                                     "PufferSpProjektKiSicht.NutztemperaturBw",
                                     KiDialogTexte.PspvNutztemperaturName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvNutztemperaturErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ladeleistung", "PufferSpProjektKiSicht.Ladeleistung",
                                     KiDialogTexte.PspvLadeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvLadeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("entladeleistung", "PufferSpProjektKiSicht.Entladeleistung",
                                     KiDialogTexte.PspvEntladeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntladeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("entladeprioritaet",
                                     "PufferSpProjektKiSicht.Entladeprioritaet",
                                     KiDialogTexte.PspvEntladeprioName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PspvEntladeprioErl, leerErlaubt: true),

                    // ---- Die NUTZUNG: drei Wahrheitswerte statt einer Mehrfachwahl --
                    new KiDialogFeld("nutzung_heizung",
                                     "PufferSpProjektKiSicht.NutzungHeizung",
                                     KiDialogTexte.PspvNutzungHeizungName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PspvNutzungHeizungErl),
                    new KiDialogFeld("nutzung_brauchwasser",
                                     "PufferSpProjektKiSicht.NutzungBrauchwasser",
                                     KiDialogTexte.PspvNutzungBwName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PspvNutzungBwErl),
                    new KiDialogFeld("nutzung_prozess",
                                     "PufferSpProjektKiSicht.NutzungProzess",
                                     KiDialogTexte.PspvNutzungProzessName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PspvNutzungProzessErl),

                    // ---- Die drei Entnahmehoehen -----------------------------------
                    new KiDialogFeld("entnahmehoehe_heizung",
                                     "PufferSpProjektKiSicht.EntnahmehoeheHeizung",
                                     KiDialogTexte.PspvEntnahmeHeizungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntnahmeHeizungErl, leerErlaubt: true),
                    new KiDialogFeld("entnahmehoehe_brauchwasser",
                                     "PufferSpProjektKiSicht.EntnahmehoeheBrauchwasser",
                                     KiDialogTexte.PspvEntnahmeBwName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntnahmeBwErl, leerErlaubt: true),
                    new KiDialogFeld("entnahmehoehe_prozess",
                                     "PufferSpProjektKiSicht.EntnahmehoeheProzess",
                                     KiDialogTexte.PspvEntnahmeProzessName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspvEntnahmeProzessErl, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfUebernehmen),
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_WP_Anlage  ->  WaermepumpeAnlageDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Die Waermepumpen-ANLAGE eines Projekts — einundzwanzig Felder aus
        /// <c>EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>DREI Bloecke, EIN Daten-Objekt.</b> Die Maske besteht aus dem Dialog und
        /// zwei Bausteinen: der KONFIGURATION (Heizstab, Sperrzeit, bivalenter Betrieb,
        /// Betriebsart, Bivalenztemperatur) und den STAMMFELDERN (Hersteller, Typ,
        /// Nennleistung …). Beide schreiben in denselben Satz — die Stammfelder ueber
        /// ein Abbild, das der Dialog bei jeder Eingabe zurueckschreibt und nach einer
        /// Feldsetzung des Assistenten neu aufbaut. Deshalb steht hier EIN
        /// Katalogeintrag und nicht drei.
        /// </para>
        /// <para>
        /// <b>Die BETRIEBSART ist eine Aufzaehlung.</b> Ihre Werte sind Steuerwerte des
        /// Bestands (<c>DbWerte.WP_BETRIEBSART_*</c>: alternativ, parallel,
        /// teilparallel) und stehen so in <c>Tab_Energieanlagen.Betriebsart</c> — nicht
        /// der Anzeigetext der Klappliste. Was sie bedeuten, steht in der Erlaeuterung.
        /// </para>
        /// <para>
        /// <b>Die MODULKOSTEN sind Anzeige.</b> Der Stammfeldblock zeigt sie als
        /// Lesewert; gepflegt werden Geraetekosten in der Kostenverwaltung. Ohne
        /// <c>nurLesen</c> boete der Assistent an, eine Zahl zu setzen, die der naechste
        /// Kostenlauf wortlos ueberschriebe.
        /// </para>
        /// <para>
        /// <b>Der ENERGIETRAEGER fehlt mit Absicht</b> — er steht im Daten-Objekt allein
        /// als Id (<c>CarrierId</c>); dieselbe Regel wie bei Kessel, BHKW und
        /// Stromspeicher. Die KENNLINIEN fehlen ebenfalls: Sie sind eine Tabelle von
        /// Stuetzstellen mit eigenem Editor, kein Maskenfeld.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe ausser OK und Abbrechen.</b> „Kennlinien aus dem Katalog
        /// uebernehmen" und „In Stamm uebernehmen…" sind datenbankwirksam und gehoeren
        /// in das Aktionsregister mit Bestaetigung und Sicherungspunkt.
        /// </para>
        /// </remarks>
        private static KiDialog WaermepumpeAnlage()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WAERMEPUMPE_ANLAGE,
                anzeigename: KiDialogTexte.MaskeWpAnlage,
                felder: new[]
                {
                    // ---- Woran der Anwender gerade arbeitet -------------------------
                    new KiDialogFeld("anlage", "WaermepumpeAnlageDaten.Bezeichner",
                                     KiDialogTexte.WpaAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaAnlageErl,
                                     leerErlaubt: true, nurLesen: true),

                    // ---- Auslegung fuer die Verteilung ------------------------------
                    new KiDialogFeld("vorlauf", "WaermepumpeAnlageDaten.Vorlauf",
                                     KiDialogTexte.WpaVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "WaermepumpeAnlageDaten.Ruecklauf",
                                     KiDialogTexte.WpaRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("nutzungsdauer", "WaermepumpeAnlageDaten.Nutzungszeit",
                                     KiDialogTexte.WpaNutzungsdauerName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaNutzungsdauerErl,
                                     einheit: KiDialogTexte.EINHEIT_JAHR, leerErlaubt: true),

                    // ---- Der Block „Konfiguration" ---------------------------------
                    new KiDialogFeld("heizstab", "WaermepumpeAnlageDaten.Heizstab",
                                     KiDialogTexte.WpaHeizstabName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaHeizstabErl),
                    new KiDialogFeld("sperrzeit", "WaermepumpeAnlageDaten.Sperrung",
                                     KiDialogTexte.WpaSperrungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaSperrungErl),
                    new KiDialogFeld("sperrzeit_von", "WaermepumpeAnlageDaten.SperrzeitVon",
                                     KiDialogTexte.WpaSperrzeitVonName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitVonErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("sperrzeit_bis", "WaermepumpeAnlageDaten.SperrzeitBis",
                                     KiDialogTexte.WpaSperrzeitBisName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaSperrzeitBisErl,
                                     einheit: KiDialogTexte.EINHEIT_H_TAG, leerErlaubt: true),
                    new KiDialogFeld("bivalenter_betrieb", "WaermepumpeAnlageDaten.BivalenterBetrieb",
                                     KiDialogTexte.WpaBivalentName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.WpaBivalentErl),
                    new KiDialogFeld("energietraeger", "WaermepumpeAnlageDaten.CarrierId",
                                     KiDialogTexte.WpaTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaTraegerErl),
                    new KiDialogFeld("betriebsart", "WaermepumpeAnlageDaten.Betriebsart",
                                     KiDialogTexte.WpaBetriebsartName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBetriebsartErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("bivalenztemperatur", "WaermepumpeAnlageDaten.Abschaltpunkt",
                                     KiDialogTexte.WpaAbschaltpunktName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaAbschaltpunktErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Die Felder des Geraets (Stammfeldblock) --------------------
                    new KiDialogFeld("hersteller", "WaermepumpeAnlageDaten.Firma",
                                     KiDialogTexte.WpaFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "WaermepumpeAnlageDaten.Beschreibung",
                                     KiDialogTexte.WpaBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("typ", "WaermepumpeAnlageDaten.Typ",
                                     KiDialogTexte.WpaTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaTypErl, leerErlaubt: true),
                    new KiDialogFeld("leistungsstufen", "WaermepumpeAnlageDaten.Regelung",
                                     KiDialogTexte.WpaRegelungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaRegelungErl, leerErlaubt: true),
                    new KiDialogFeld("aufstellung", "WaermepumpeAnlageDaten.Aufstellung",
                                     KiDialogTexte.WpaAufstellungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaAufstellungErl, leerErlaubt: true),
                    new KiDialogFeld("baujahr", "WaermepumpeAnlageDaten.Baujahr",
                                     KiDialogTexte.WpaBaujahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBaujahrErl),
                    new KiDialogFeld("nennleistung", "WaermepumpeAnlageDaten.Nennleistung",
                                     KiDialogTexte.WpaNennleistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaNennleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("heizstab_leistung", "WaermepumpeAnlageDaten.HeizstabLeistung",
                                     KiDialogTexte.WpaHeizstabLeistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaHeizstabLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("kuehlleistung", "WaermepumpeAnlageDaten.Kuehlleistung",
                                     KiDialogTexte.WpaKuehlleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpaKuehlleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("modulkosten", "WaermepumpeAnlageDaten.Modulkosten",
                                     KiDialogTexte.WpaModulkostenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaModulkostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_SolarKollektoren  ->  SolarkollektorenDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Solarkollektoren im Projekt — die fuenf Zahlen der Kollektorgruppe
        /// (<c>EPOS.UI.Dialoge.Solarthermie.SolarkollektorenEingaben</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Daten-Objekt ist der ARBEITSSTAND und nicht die Projektzeile.</b>
        /// Diese Maske schreibt die Zeile erst beim Knopf „Uebernehmen"; bis dahin
        /// fuehrt sie die Eingaben fuer sich, und genau die sieht der Anwender. Eine
        /// Setzung in die Zeile bliebe auf der Maske unsichtbar, und das naechste
        /// „Uebernehmen" ueberschriebe sie wortlos — deshalb zeigt der Katalog auf den
        /// Stand, an dem auch die Eingabefelder haengen.
        /// </para>
        /// <para>
        /// <b>Die APERTURFLAECHE fehlt.</b> Sie ist Anzeige und faellt aus Modulflaeche
        /// mal Anzahl; ein eigenes Feld traegt sie im Arbeitsstand nicht, und eine
        /// Groesse, die es nur als gerechnete Zeichenkette gibt, laesst sich weder
        /// benennen noch pruefen. Wer die Flaeche aendern will, aendert die Anzahl.
        /// </para>
        /// </remarks>
        private static KiDialog SolarkollektorenProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT,
                anzeigename: KiDialogTexte.MaskeSolarkollektoren,
                felder: new[]
                {
                    new KiDialogFeld("anzahl_module", "SolarkollektorenEingaben.Anzahl",
                                     KiDialogTexte.SkAnzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkAnzahlErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("neigung", "SolarkollektorenEingaben.Neigung",
                                     KiDialogTexte.SkNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("azimut", "SolarkollektorenEingaben.Azimut",
                                     KiDialogTexte.SkAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "SolarkollektorenEingaben.Vorlauf",
                                     KiDialogTexte.SkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "SolarkollektorenEingaben.Ruecklauf",
                                     KiDialogTexte.SkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("uebernehmen", "btn_Uebernehmen",
                                      KiDialogTexte.KnopfUebernehmen),
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PufferSp  ->  PufferspeicherDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Pufferspeicher im Projekt — die gewaehlte Zeile der Projektliste
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>), mit EINEM Feld.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Ein Feld, und das ist kein Versehen.</b> Diese Maske fuehrt keine
        /// Einstellwerte der ANLAGE: Sie waehlt den Speicher aus dem Katalog, zeigt
        /// seine Werte und laesst den KATALOGSATZ im Aufklapper „Alle Daten"
        /// bearbeiten. Der Name der gewaehlten Zeile ist damit alles, was der Assistent
        /// hier lesen kann — und genau das soll er koennen: „welcher Pufferspeicher ist
        /// im Projekt gewaehlt?" beantwortet er dann aus der Maske und nicht aus der
        /// Dokumentation.
        /// </para>
        /// <para>
        /// <b>Der Aufklapper bleibt draussen</b> — dieselbe Begruendung wie beim
        /// Heizkessel: Er zeigt die Spalten des KATALOGsatzes ueber ein Profil und
        /// nicht ueber benannte Eigenschaften der Zeile.
        /// </para>
        /// </remarks>
        private static KiDialog PufferspeicherProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PUFFERSPEICHER_PROJEKT,
                anzeigename: KiDialogTexte.MaskePufferSpProjekt,
                felder: new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerZeile.Bezeichner",
                                     KiDialogTexte.PspAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.PspAnlageErl,
                                     leerErlaubt: true, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Stromspeicher  ->  StromspeicherDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Stromspeicher im Projekt — die gewaehlte Zeile der Projektliste
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>), mit EINEM Feld.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Feldumfang wie beim Pufferspeicher.</b> Die Maske waehlt das Geraet und
        /// zeigt seine Katalogwerte; die Betriebsfuehrung der Speicher steht in der
        /// Ansicht „Stromspeicher-Auslegung"
        /// (<see cref="KiMaskennamen.STROMSPEICHER_AUSLEGUNG"/>) und dort im Katalog
        /// mit siebenundzwanzig Feldern.
        /// </para>
        /// <para>
        /// <b>Der ENERGIETRAEGER fehlt mit Absicht.</b> Er steht als Wahl ueber Gruppe
        /// und Art auf der Maske, im Daten-Objekt aber allein als Id
        /// (<c>ErzeugerZeile.CarrierId</c>); gelesen waere er eine nackte Zahl,
        /// gesetzt eine geratene — dieselbe Regel wie bei der Brennstoffvariante der
        /// Kessel- und BHKW-Maske.
        /// </para>
        /// </remarks>
        private static KiDialog StromspeicherProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.STROMSPEICHER_PROJEKT,
                anzeigename: KiDialogTexte.MaskeStromspeicherProjekt,
                felder: new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerZeile.Bezeichner",
                                     KiDialogTexte.StspAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.StspAnlageErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("energietraeger", "ErzeugerZeile.CarrierId",
                                     KiDialogTexte.StspTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.StspTraegerErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_Heizkessel  ->  HeizkesselDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// Heizkessel im Projekt — die Werte, die der Projektdialog an der GEWAEHLTEN
        /// Zeile fuehrt (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Daten-Objekt ist eine ZEILE und kein Dialogstand</b> — dieselbe
        /// Bauart wie bei <see cref="Photovoltaik"/>: Der Dialog fuehrt eine
        /// Projektliste, angemeldet wird die gewaehlte Zeile, und der Getter holt sie
        /// bei jedem Lesen neu. Ist keine gewaehlt, sind die Felder leer — derselbe
        /// Zustand, den der Anwender auf der Maske sieht.
        /// </para>
        /// <para>
        /// <b>Die BRENNSTOFFVARIANTE fehlt mit Absicht.</b> Sie ist ein Verweis in eine
        /// kontextabhaengige Liste (<c>ErzeugerZeile.CarrierId</c>, gefuellt aus den
        /// Varianten der Traegergruppe); sie ueber ihre rohe Id setzen zu lassen hiesse,
        /// das Modell eine Zahl raten zu lassen, deren Bedeutung nur die Maske kennt —
        /// dieselbe Regel wie bei der Bemessung der Kostenverwaltung. Sie kommt in den
        /// Katalog, sobald es dafuer eine benannte Auswahl gibt.
        /// </para>
        /// <para>
        /// <b>Der Aufklapper „Alle Daten" bleibt draussen.</b> Er zeigt die Spalten des
        /// gewaehlten KATALOGsatzes ueber ein Profil
        /// (<c>EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs</c>) und nicht ueber
        /// benannte Eigenschaften der Zeile; was dort steht, gehoert dem Katalog und
        /// nicht der Anlage.
        /// </para>
        /// </remarks>
        private static KiDialog HeizkesselProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.HEIZKESSEL_PROJEKT,
                anzeigename: KiDialogTexte.MaskeHeizkesselProjekt,
                felder: new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerZeile.Bezeichner",
                                     KiDialogTexte.HkpAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.HkpAnlageErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("vorlauf", "ErzeugerZeile.Vorlauf",
                                     KiDialogTexte.HkpVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkpVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "ErzeugerZeile.CarrierId",
                                     KiDialogTexte.HkpTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.HkpTraegerErl),
                    new KiDialogFeld("ruecklauf", "ErzeugerZeile.Ruecklauf",
                                     KiDialogTexte.HkpRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkpRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_BHKWEing  ->  BhkwDialog   (Welle KI-F1)
        // =====================================================================

        /// <summary>
        /// BHKW im Projekt — die Werte der gewaehlten Projektzeile
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>).
        /// </summary>
        /// <remarks>
        /// <b>Die untere GRENZLEISTUNG ist der Unterschied zum Heizkessel.</b> Sie sagt,
        /// bis wohin das Modul moduliert; 0 heisst „Projektvorgabe"
        /// (<c>Tab_Einstellungen.Leistungsgrenze</c>), und genau das steht in ihrer
        /// Erlaeuterung. Die Brennstoffvariante fehlt aus demselben Grund wie beim
        /// Heizkessel.
        /// </remarks>
        private static KiDialog BhkwProjekt()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.BHKW_PROJEKT,
                anzeigename: KiDialogTexte.MaskeBhkwProjekt,
                felder: new[]
                {
                    new KiDialogFeld("anlage", "ErzeugerZeile.Bezeichner",
                                     KiDialogTexte.BhkwAnlageName, KiParameterTyp.Text,
                                     KiDialogTexte.BhkwAnlageErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("grenzleistung", "ErzeugerZeile.Grenzleistung",
                                     KiDialogTexte.BhkwGrenzleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.BhkwGrenzleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "ErzeugerZeile.Vorlauf",
                                     KiDialogTexte.BhkwVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.BhkwVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "ErzeugerZeile.CarrierId",
                                     KiDialogTexte.BhkwTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.BhkwTraegerErl),
                    new KiDialogFeld("ruecklauf", "ErzeugerZeile.Ruecklauf",
                                     KiDialogTexte.BhkwRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.BhkwRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Kostenverwaltung  ->  KostenKomponenteDialog
        // =====================================================================

        /// <summary>
        /// Kostenverwaltung einer Komponente — die erste Maske des Katalogs, die mit
        /// SPALTEN arbeitet (Anwenderbefund vom 14.09.2026).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drei Kopffelder und vier Spalten.</b> Die Kopffelder sagen, WORAN der
        /// Anwender gerade arbeitet (Komponente, Projekt, Art der Vorlage); sie sind
        /// samt und sonders <c>nurLesen</c> - die Huelle fuellt sie, der Anwender
        /// aendert sie nicht. Die Spalten sind das Raster: je Position eine Zeile, und
        /// aus jeder Spaltendeklaration wird je vorhandener Zeile ein gewoehnliches
        /// Feld (<c>nutzungsdauer_1</c>, <c>nutzungsdauer_2</c>, …).
        /// </para>
        /// <para>
        /// <b>Der Betrag ist Anzeige, kein Eingabefeld.</b> Er faellt aus Satz und
        /// Bemessung und traegt im Daten-Objekt nur deshalb einen Setzer, weil die
        /// Huelle ihn fuellt. Ohne <c>nurLesen</c> boete der Assistent an, ihn zu
        /// setzen - und die naechste Neuberechnung ueberschriebe es wortlos.
        /// </para>
        /// <para>
        /// <b>Die Bemessung fehlt mit Absicht.</b> Sie ist ein Verweis in eine
        /// kontextabhaengige Liste (<c>KostenKomponenteStand.Bemessungen</c>); sie ueber
        /// ihre rohe Id setzen zu lassen hiesse, das Modell eine Zahl raten zu lassen,
        /// deren Bedeutung nur die Maske kennt. Sie kommt in den Katalog, sobald es
        /// dafuer eine benannte Auswahl gibt - dieselbe Regel wie ueberall:
        /// Aufzaehlungswerte stammen aus dem Bestand, nie aus Modelltext.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe.</b> „Speichern" und „OK" sind datenbankwirksam und laufen
        /// deshalb ueber <c>dialog_speichern</c> mit Bestaetigung UND Sicherungspunkt
        /// (Anwenderentscheid KI-D-Q4) - nicht als Formularaktion.
        /// </para>
        /// </remarks>
        private static KiDialog Kostenverwaltung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.KOSTENVERWALTUNG,
                anzeigename: KiDialogTexte.MaskeKostenverwaltung,
                felder: new[]
                {
                    // ---- Woran der Anwender gerade arbeitet -------------------------
                    new KiDialogFeld("komponente", "KostenKomponenteStand.Titel",
                                     KiDialogTexte.KvTitelName, KiParameterTyp.Text,
                                     KiDialogTexte.KvTitelErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("bezug", "KostenKomponenteStand.Untertitel",
                                     KiDialogTexte.KvUntertitelName, KiParameterTyp.Text,
                                     KiDialogTexte.KvUntertitelErl,
                                     leerErlaubt: true, nurLesen: true),
                    new KiDialogFeld("auslieferungsvorlage", "KostenKomponenteStand.NurLesen",
                                     KiDialogTexte.KvNurLesenName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.KvNurLesenErl,
                                     nurLesen: true),

                    // ---- Das Raster: je Position eine Zeile --------------------------
                    new KiDialogFeld("position", "KostenKomponenteStand.Zeilen[].Bezeichnung",
                                     KiDialogTexte.KvPositionName, KiParameterTyp.Text,
                                     KiDialogTexte.KvPositionErl,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("bemessung", "KostenKomponenteStand.Zeilen[].BemessungId",
                                     KiDialogTexte.KvBemessungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.KvBemessungErl, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("satz", "KostenKomponenteStand.Zeilen[].Satz",
                                     KiDialogTexte.KvSatzName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KvSatzErl,
                                     leerErlaubt: true, zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("nutzungsdauer", "KostenKomponenteStand.Zeilen[].Nutzungsdauer",
                                     KiDialogTexte.KvNutzungsdauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.KvNutzungsdauerErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN),
                    new KiDialogFeld("betrag", "KostenKomponenteStand.Zeilen[].BetragText",
                                     KiDialogTexte.KvBetragName, KiParameterTyp.Text,
                                     KiDialogTexte.KvBetragErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true,
                                     zeilenkennzeichen: ZEILENKENNZEICHEN, nurLesen: true)
                });
        }

        /// <summary>
        /// Die Eigenschaft, aus der eine Rasterzeile ihren Klartextnamen bekommt -
        /// „Nutzungsdauer (Zubehoer)" statt „Nutzungsdauer 2".
        /// </summary>
        private const string ZEILENKENNZEICHEN = "Bezeichnung";

        /// <summary>
        /// Dasselbe fuer die Straenge einer Photovoltaikanlage: „Dach Sued · Module in
        /// Reihe" statt „Module in Reihe 2". Ein Strang ohne Bezeichner faellt auf
        /// seine Nummer zurueck - so, wie ihn auch die Maske zeigt.
        /// </summary>
        private const string STRANGKENNZEICHEN = "Bezeichner";

        // =====================================================================
        // Form_Heizkessel_Bearbeiten  ->  HeizkesselKatalogDialog
        // =====================================================================

        /// <summary>
        /// Heizkessel bearbeiten — die SECHS Felder, die <c>HeizkesselKatalogDialog</c>
        /// sichtbar traegt, als Eigenschaften von
        /// <c>EPOS.UI.Dialoge.Erzeuger.HeizkesselKatalogDaten</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>NEUN Felder sind mit dem Anwenderentscheid vom 15.09.2026 entfallen</b>
        /// („Der Dialog ueber Button Bearbeiten soll keine Kosten und Emissionen
        /// enthalten"): <c>investitionskosten</c>, <c>wartungskosten</c>,
        /// <c>raumbedarf</c>, <c>nutzungsdauer</c>, <c>co2</c>, <c>so2</c>, <c>nox</c>,
        /// <c>co</c> und <c>staub</c>. Die Maske zeigt sie nicht mehr; gepflegt werden
        /// sie seit Phase 1 im AUFKLAPPER „Alle Daten" des Projektdialogs (Profil in
        /// <c>EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs</c>). Dorthin fuehrt
        /// KEIN Weg des Assistenten: Der Aufklapper ist keine angemeldete Katalogmaske,
        /// und ein Katalogfeld, das in der offenen Maske niemand sieht, waere genau die
        /// stille Setzung, die Fachkonzept 11.6 ausschliesst — der Anwender bestaetigte
        /// eine Zahl, die er nirgends nachlesen kann.
        /// </para>
        /// <para>
        /// Zwei Namen fallen auf, und beide sind Absicht: Die thermische Leistung heisst
        /// im Daten-Objekt <c>Ptherm</c> (nicht <c>ThLeistung</c>), und der
        /// Bereitschaftsverlust <c>Betriebsbereitschaftverlust</c> — so stehen sie in der
        /// Oberflaeche, und der Katalog schreibt keine zweite Schreibweise daneben.
        /// </para>
        /// </remarks>
        private static KiDialog Heizkessel()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.HEIZKESSEL,
                anzeigename: KiDialogTexte.MaskeHeizkessel,
                felder: new[]
                {
                    new KiDialogFeld("th_leistung", "HeizkesselKatalogDaten.Ptherm",
                                     KiDialogTexte.HkLeistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_gas", "HeizkesselKatalogDaten.Wirkungsgrad_Gas",
                                     KiDialogTexte.HkWgGasName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWgGasErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_oel", "HeizkesselKatalogDaten.Wirkungsgrad_Oel",
                                     KiDialogTexte.HkWgOelName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWgOelErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("bereitschaftsverlust",
                                     "HeizkesselKatalogDaten.Betriebsbereitschaftverlust",
                                     KiDialogTexte.HkBbVerlustName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkBbVerlustErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "HeizkesselKatalogDaten.Vorlauf",
                                     KiDialogTexte.HkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "HeizkesselKatalogDaten.Ruecklauf",
                                     KiDialogTexte.HkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),

                    // ---- Die uebrigen Eingabefelder der Maske (KI-F1b, KI-D-Q6) ----
                    new KiDialogFeld("name", "HeizkesselKatalogDaten.Name",
                                     KiDialogTexte.HkNameName, KiParameterTyp.Text,
                                     KiDialogTexte.HkNameErl),
                    new KiDialogFeld("hersteller", "HeizkesselKatalogDaten.Firma",
                                     KiDialogTexte.HkFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.HkFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "HeizkesselKatalogDaten.Beschreibung",
                                     KiDialogTexte.HkBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.HkBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "HeizkesselKatalogDaten.Brennstoff",
                                     KiDialogTexte.HkTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.HkTraegerErl, leerErlaubt: true),
                    new KiDialogFeld("brennwert", "HeizkesselKatalogDaten.Brennwert",
                                     KiDialogTexte.HkBrennwertName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.HkBrennwertErl)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PV  ->  PhotovoltaikDialog
        // =====================================================================

        /// <summary>
        /// Photovoltaik — die drei Werte, die der Projektdialog neben der Geraetewahl
        /// fuehrt; sie stehen an der gewaehlten Zeile
        /// (<c>EPOS.UI.Dialoge.Erzeuger.ErzeugerZeile</c>).
        /// </summary>
        /// <remarks>
        /// <b>Das Daten-Objekt ist hier eine ZEILE und kein Dialogstand.</b> Der Dialog
        /// fuehrt eine Projektliste; angemeldet wird die GEWAEHLTE Zeile, und der Getter
        /// holt sie bei jedem Lesen neu. Ist keine gewaehlt, sind die Felder leer —
        /// derselbe Zustand, den der Anwender auf der Maske sieht.
        /// <para>
        /// <b>Die Maske besteht aus DREI Dateien</b> (Welle KI-F1): der Projektdialog
        /// selbst, der Baustein der MODELLFELDER (Rechenmodell,
        /// Wechselrichter-Wirkungsgrad, Systemverluste) und der Baustein der STRAENGE.
        /// Alle drei binden an dieselbe Zeile, deshalb steht hier EIN Katalogeintrag.
        /// </para>
        /// <para>
        /// <b>Die STRAENGE sind eine LISTE</b> und damit die zweite Maske mit Spalten
        /// (nach der Kostenverwaltung): Je Strangzeile wird aus jeder Spaltendeklaration
        /// ein gewoehnliches Feld, und das Zeilenkennzeichen ist der Bezeichner des
        /// Strangs („Dach Sued") statt seiner Nummer. Das MODUL und das GERAET eines
        /// Strangs fehlen dabei: Beide sind Verweise in kontextabhaengige Katalogauswahlen
        /// und stehen im Daten-Objekt allein als Id — dieselbe Regel wie ueberall.
        /// </para>
        /// <para>
        /// <b>Die Anlagenwerte des Wechselrichters bleiben draussen</b>
        /// (<c>WrNennleistungKw</c>, <c>WrEta10/50/100</c>). Sie stehen in einer eigenen
        /// Ueberlagerung mit eigenem Arbeitsstand und eigenem OK; auf der offenen Maske
        /// sieht der Anwender sie nicht, und ein Katalogfeld, das dort niemand nachlesen
        /// kann, waere genau die stille Setzung, die Fachkonzept 11.6 ausschliesst.
        /// </para>
        /// </remarks>
        private static KiDialog Photovoltaik()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PHOTOVOLTAIK,
                anzeigename: KiDialogTexte.MaskePv,
                felder: new[]
                {
                    // ---- Die Anlage -------------------------------------------------
                    new KiDialogFeld("neigung", "ErzeugerZeile.Neigung",
                                     KiDialogTexte.PvNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("azimut", "ErzeugerZeile.Azimut",
                                     KiDialogTexte.PvAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("energietraeger", "ErzeugerZeile.CarrierId",
                                     KiDialogTexte.PvTraegerName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PvTraegerErl),
                    new KiDialogFeld("anzahl_module", "ErzeugerZeile.AnzahlModule",
                                     KiDialogTexte.PvAnzahlName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvAnzahlErl,
                                     leerErlaubt: true),

                    // ---- Die Modellfelder (PvModellFelder) --------------------------
                    new KiDialogFeld("modell_erweitert", "ErzeugerZeile.ModellErweitert",
                                     KiDialogTexte.PvModellName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PvModellErl),
                    new KiDialogFeld("wr_wirkungsgrad", "ErzeugerZeile.WrWirkungsgrad",
                                     KiDialogTexte.PvWrWirkungsgradName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvWrWirkungsgradErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("systemverluste", "ErzeugerZeile.Systemverluste",
                                     KiDialogTexte.PvSystemverlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvSystemverlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),

                    // ---- Wechselrichter und Straenge (PvStraengeFelder) -------------
                    new KiDialogFeld("mit_wechselrichter", "ErzeugerZeile.MitWechselrichter",
                                     KiDialogTexte.PvMitWrName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.PvMitWrErl),
                    new KiDialogFeld("strang", "ErzeugerZeile.Straenge[].Bezeichner",
                                     KiDialogTexte.PvStrangName, KiParameterTyp.Text,
                                     KiDialogTexte.PvStrangErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_geraet", "ErzeugerZeile.Straenge[].Geraetenummer",
                                     KiDialogTexte.PvStrangGeraetName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangGeraetErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_mppt", "ErzeugerZeile.Straenge[].Mppt",
                                     KiDialogTexte.PvStrangMpptName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangMpptErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_module_reihe", "ErzeugerZeile.Straenge[].ModuleReihe",
                                     KiDialogTexte.PvStrangReiheName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangReiheErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_parallel", "ErzeugerZeile.Straenge[].StraengeParallel",
                                     KiDialogTexte.PvStrangParallelName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangParallelErl,
                                     leerErlaubt: true, zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_neigung", "ErzeugerZeile.Straenge[].Neigung",
                                     KiDialogTexte.PvStrangNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true,
                                     zeilenkennzeichen: STRANGKENNZEICHEN),
                    new KiDialogFeld("strang_azimut", "ErzeugerZeile.Straenge[].Azimut",
                                     KiDialogTexte.PvStrangAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvStrangAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true,
                                     zeilenkennzeichen: STRANGKENNZEICHEN)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_PufferSp_Bearbeiten  ->  PufferSpKatalogDialog
        // =====================================================================

        /// <summary>
        /// Pufferspeicher bearbeiten — das eine Feld der Knopfpruefung
        /// (<c>EPOS.UI.Dialoge.Erzeuger.PufferSpKatalogDaten</c>).
        /// </summary>
        /// <remarks>
        /// <b>Nur ein Feld — und das ist kein Versehen.</b> Die uebrigen Eingaben der
        /// Maske (Hersteller, Speichertyp, Bereitschaftsverluste) liefen im Vorlaeufer
        /// ueber stille Parser und gehoeren nach Fachkonzept 11.7 erst nach ihrer
        /// Umstellung in den Katalog. Ob die Razor-Fassung das anders sieht, ist eine
        /// fachliche Frage mit eigener Abnahme.
        /// <para>
        /// <b>Die INVESTITIONSKOSTEN kommen nicht mehr in Frage.</b> Sie verlassen die
        /// Maske mit dem Anwenderentscheid vom 15.09.2026 (kein Kosteneintrag im
        /// Bearbeiten-Dialog) und sind seither im Aufklapper „Alle Daten" des
        /// Projektdialogs zu pflegen — ohne Weg des Assistenten, wie beim Heizkessel.
        /// Der Katalog hat sie nie gefuehrt; hier steht nur, dass das so bleibt.
        /// </para>
        /// </remarks>
        private static KiDialog Pufferspeicher()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PUFFERSPEICHER,
                anzeigename: KiDialogTexte.MaskePufferSp,
                felder: new[]
                {
                    new KiDialogFeld("gesamtvolumen", "PufferSpKatalogDaten.Gesamtvolumen",
                                     KiDialogTexte.PspVolumenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspVolumenErl,
                                     einheit: KiDialogTexte.EINHEIT_LITER, leerErlaubt: true),

                    // ---- Die uebrigen Eingabefelder der Maske (KI-F1b, KI-D-Q6) ----
                    new KiDialogFeld("name", "PufferSpKatalogDaten.Name",
                                     KiDialogTexte.PspNameName, KiParameterTyp.Text,
                                     KiDialogTexte.PspNameErl),
                    new KiDialogFeld("hersteller", "PufferSpKatalogDaten.Firma",
                                     KiDialogTexte.PspFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.PspFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("speichertyp", "PufferSpKatalogDaten.SpeichertypIndex",
                                     KiDialogTexte.PspSpeichertypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.PspSpeichertypErl, leerErlaubt: true),
                    new KiDialogFeld("bereitschaftsverluste",
                                     "PufferSpKatalogDaten.Bereitschaftsverluste",
                                     KiDialogTexte.PspVerlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PspVerlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH_24H, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_WP  ->  WaermepumpeStammDialog
        // =====================================================================

        /// <summary>
        /// Waermepumpen verwalten — das eine Feld der Knopfpruefung
        /// (<c>EPOS.UI.Dialoge.Waermepumpe.WaermepumpeStammDaten</c>).
        /// </summary>
        /// <remarks>
        /// Begruendung fuer den Feldumfang wie beim Pufferspeicher (Fachkonzept 11.7):
        /// <c>btn_Speichern_Click</c> prueft ausschliesslich die Modulkosten, und zwar
        /// mit <c>leerErlaubt: false</c>.
        /// </remarks>
        private static KiDialog Waermepumpe()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.WAERMEPUMPE,
                anzeigename: KiDialogTexte.MaskeWp,
                felder: new[]
                {
                    new KiDialogFeld("modulkosten", "WaermepumpeStammDaten.Modulkosten",
                                     KiDialogTexte.WpModulkostenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpModulkostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: false),

                    // ---- Die Stammfelder des Bausteins (KI-F1b, KI-D-Q6) ----------
                    new KiDialogFeld("name", "WaermepumpeStammDaten.Name",
                                     KiDialogTexte.WpNameName, KiParameterTyp.Text,
                                     KiDialogTexte.WpNameErl),
                    new KiDialogFeld("hersteller", "WaermepumpeStammDaten.Firma",
                                     KiDialogTexte.WpaFirmaName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaFirmaErl, leerErlaubt: true),
                    new KiDialogFeld("beschreibung", "WaermepumpeStammDaten.Beschreibung",
                                     KiDialogTexte.WpaBeschreibungName, KiParameterTyp.Text,
                                     KiDialogTexte.WpaBeschreibungErl, leerErlaubt: true),
                    new KiDialogFeld("typ", "WaermepumpeStammDaten.Typ",
                                     KiDialogTexte.WpaTypName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaTypErl, leerErlaubt: true),
                    new KiDialogFeld("leistungsstufen", "WaermepumpeStammDaten.Regelung",
                                     KiDialogTexte.WpaRegelungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaRegelungErl, leerErlaubt: true),
                    new KiDialogFeld("aufstellung", "WaermepumpeStammDaten.Aufstellung",
                                     KiDialogTexte.WpaAufstellungName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaAufstellungErl, leerErlaubt: true),
                    new KiDialogFeld("baujahr", "WaermepumpeStammDaten.Baujahr",
                                     KiDialogTexte.WpaBaujahrName, KiParameterTyp.Wahl,
                                     KiDialogTexte.WpaBaujahrErl, leerErlaubt: true),
                    new KiDialogFeld("nennleistung", "WaermepumpeStammDaten.Nennleistung",
                                     KiDialogTexte.WpaNennleistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaNennleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("heizstab_leistung", "WaermepumpeStammDaten.Heizstab",
                                     KiDialogTexte.WpaHeizstabLeistungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpaHeizstabLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("kuehlleistung", "WaermepumpeStammDaten.Kuehlleistung",
                                     KiDialogTexte.WpKuehlleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.WpKuehlleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, nurLesen: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("ok", "btn_Beenden", KiDialogTexte.KnopfOk)
                });
        }

        // =====================================================================
        // StromspeicherAuslegung  ->  StromspeicherAuslegungSeite
        // =====================================================================

        /// <summary>
        /// Die FUENFTE Maske (Auftrag #200, Anwenderentscheid KI‑D‑Q3): die
        /// Stromspeicher-Ansicht — siebzehn Felder aus
        /// <c>EPOS.UI.Seiten.Strom.StromspeicherKiSicht</c>; das siebzehnte,
        /// <c>peak_ziel_adaptiv</c>, kam mit der kausalen Ratsche dazu
        /// (Spezifikation 5.1.1, Auftrag #215).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum gerade sie und warum als erste Nicht-Katalogmaske.</b> Sie ist die
        /// Ansicht mit der haeufigsten unverstandenen Meldung des Hauses („Die Flotte hat
        /// im gesamten Zeitraum weder geladen noch entladen", Kennung
        /// <c>FLOTTE_ARBEITSLOS</c>) — und die Antwort darauf steht in ihren eigenen
        /// Zaehlern. Ohne Feldwerte kann der Assistent sie nur allgemein erklaeren; mit
        /// ihnen nennt er das Peak-Ziel, die Netzladefreigabe und die Zahl der
        /// Intervalle, in denen der Ladedeckel auf 0 stand.
        /// </para>
        /// <para>
        /// <b>Sechs Felder sind ABGELEITET und nur lesbar</b> (Diagnose und Ergebnis der
        /// letzten Bewertung). Sie sind trotzdem Felder und keine zweite Gattung: Die
        /// Deklaration traegt Anzeigename, Art und Erlaeuterung, und der Feldblock zeigt
        /// sie neben den Eingaben — der Anwender liest sie auf derselben Ansicht ebenso
        /// nebeneinander. Die Setzseite bleibt bei ihnen leer, und die Stufe S3 wird sie
        /// darum ablehnen; das ist der Unterschied, den sie brauchen.
        /// </para>
        /// <para>
        /// <b>Eine Flotte hat MEHRERE Einheiten, ein Maskenfeld traegt EINEN Wert</b>
        /// (<see cref="KiDialogFeld"/>). Die drei Summenfelder nennen deshalb die Flotte
        /// als Ganzes, und <c>einheiten_liste</c> traegt die Aufstellung je Einheit als
        /// Text — genau die Zeile, die die Ansicht zeigt. Eine Deklaration je Einheit
        /// ginge nicht: Ihre Zahl steht erst zur Laufzeit fest.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe.</b> Die Ansicht fuehrt „Berechnen", „Peak-Ziel
        /// bestimmen…" und „Speichern" — das sind rechnende und datenbankwirksame
        /// Aktionen der Stufen 2 und 3 des Aufgabensteuerungskonzepts. Sie gehoeren in
        /// das Aktionsregister mit Bestaetigung und Sicherungspunkt (Auftrag #201) und
        /// nicht in eine Knopfliste, die eine Formularaktion ausloest.
        /// </para>
        /// </remarks>
        private static KiDialog Stromspeicherauslegung()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.STROMSPEICHER_AUSLEGUNG,
                anzeigename: KiDialogTexte.MaskeSpeicherauslegung,
                felder: new[]
                {
                    // ---- Wo der Anwender steht (Auftrag #224) -----------------------
                    new KiDialogFeld("schritt", "StromspeicherKiSicht.Schritt",
                                     KiDialogTexte.SpaSchrittName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaSchrittErl, leerErlaubt: true),

                    // ---- Die Flotte -------------------------------------------------
                    new KiDialogFeld("einheiten", "StromspeicherKiSicht.Einheitenzahl",
                                     KiDialogTexte.SpaEinheitenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SpaEinheitenErl),
                    new KiDialogFeld("einheiten_liste", "StromspeicherKiSicht.EinheitenListe",
                                     KiDialogTexte.SpaListeName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaListeErl, leerErlaubt: true),
                    new KiDialogFeld("kapazitaet_gesamt", "StromspeicherKiSicht.KapazitaetGesamtKWh",
                                     KiDialogTexte.SpaKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),
                    new KiDialogFeld("ladeleistung_gesamt", "StromspeicherKiSicht.LadeleistungGesamtKw",
                                     KiDialogTexte.SpaLadeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaLadeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("entladeleistung_gesamt",
                                     "StromspeicherKiSicht.EntladeleistungGesamtKw",
                                     KiDialogTexte.SpaEntladeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaEntladeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),

                    // ---- Die Betriebsfuehrung ---------------------------------------
                    new KiDialogFeld("betriebsziel", "StromspeicherKiSicht.Betriebsziel",
                                     KiDialogTexte.SpaBetriebszielName, KiParameterTyp.Wahl,
                                     KiDialogTexte.SpaBetriebszielErl),
                    new KiDialogFeld("peak_ziel", "StromspeicherKiSicht.PeakZielKw",
                                     KiDialogTexte.SpaPeakZielName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaPeakZielErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("peak_ziel_adaptiv", "StromspeicherKiSicht.PeakZielAdaptiv",
                                     KiDialogTexte.SpaAdaptivName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaAdaptivErl),
                    new KiDialogFeld("netzladung", "StromspeicherKiSicht.NetzladungErlaubt",
                                     KiDialogTexte.SpaNetzladungName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaNetzladungErl),
                    new KiDialogFeld("start_soc", "StromspeicherKiSicht.StartSocProzent",
                                     KiDialogTexte.SpaStartSocName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaStartSocErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("peak_reserve", "StromspeicherKiSicht.PeakReserveKWh",
                                     KiDialogTexte.SpaReserveName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaReserveErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),

                    // ---- Station 4 „Optimierung" (Auftrag #224) ---------------------
                    new KiDialogFeld("groessen_optimieren", "StromspeicherKiSicht.GroessenOptimieren",
                                     KiDialogTexte.SpaSucheName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaSucheErl),
                    // Seit Auftrag #247 (SD-E-10) sagt die METHODE, was variiert wird —
                    // Groesse oder Stueckzahl. Nur lesend: Sie haengt an zwei Feldern
                    // desselben Standes, und sie zu setzen ist die Aufgabe der
                    // Optionsgruppe, die beide gleichzieht.
                    new KiDialogFeld("suchmethode", "StromspeicherKiSicht.Suchmethode",
                                     KiDialogTexte.SpaMethodeName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaMethodeErl),
                    new KiDialogFeld("feinraster", "StromspeicherKiSicht.Feinraster",
                                     KiDialogTexte.SpaFeinrasterName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaFeinrasterErl),
                    new KiDialogFeld("maximale_kandidaten", "StromspeicherKiSicht.MaximaleKandidaten",
                                     KiDialogTexte.SpaMaxKandidatenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SpaMaxKandidatenErl),
                    new KiDialogFeld("kandidatenzahl", "StromspeicherKiSicht.Kandidatenzahl",
                                     KiDialogTexte.SpaKandidatenzahlName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SpaKandidatenzahlErl),

                    // ---- Das beste Ergebnis der Suche (nur lesend) ------------------
                    new KiDialogFeld("bestes_kapitalwert", "StromspeicherKiSicht.BesterKapitalwertEuro",
                                     KiDialogTexte.SpaBestwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaBestwertErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("bestes_kapazitaet", "StromspeicherKiSicht.BesteKapazitaetKWh",
                                     KiDialogTexte.SpaBestkapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaBestkapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH, leerErlaubt: true),
                    new KiDialogFeld("bestes_ersparnis", "StromspeicherKiSicht.BesteErsparnisEuroJahr",
                                     KiDialogTexte.SpaBestersparnisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaBestersparnisErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("bestes_phase", "StromspeicherKiSicht.BestePhase",
                                     KiDialogTexte.SpaBestphaseName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaBestphaseErl, leerErlaubt: true),

                    // ---- Die Diagnose (nur lesend) ----------------------------------
                    new KiDialogFeld("diagnose_arbeitslos", "StromspeicherKiSicht.Arbeitslos",
                                     KiDialogTexte.SpaArbeitslosName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SpaArbeitslosErl),
                    new KiDialogFeld("diagnose_gruende", "StromspeicherKiSicht.DiagnoseGruende",
                                     KiDialogTexte.SpaGruendeName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaGruendeErl, leerErlaubt: true),
                    new KiDialogFeld("pruefhinweise", "StromspeicherKiSicht.Pruefhinweise",
                                     KiDialogTexte.SpaHinweiseName, KiParameterTyp.Text,
                                     KiDialogTexte.SpaHinweiseErl, leerErlaubt: true),

                    // ---- Das Ergebnis der letzten Bewertung (nur lesend) -------------
                    new KiDialogFeld("ergebnis_bezugsspitze",
                                     "StromspeicherKiSicht.BezugsspitzeKw",
                                     KiDialogTexte.SpaSpitzeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaSpitzeErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("ergebnis_netzbezug", "StromspeicherKiSicht.NetzbezugKWh",
                                     KiDialogTexte.SpaNetzbezugName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaNetzbezugErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH, leerErlaubt: true),
                    new KiDialogFeld("ergebnis_kapitalwert", "StromspeicherKiSicht.KapitalwertEuro",
                                     KiDialogTexte.SpaKapitalwertName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SpaKapitalwertErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true)
                });
        }

        // =====================================================================
        // Simulation  ->  SimulationSeite
        // =====================================================================

        /// <summary>
        /// Die SECHSTE Maske (Auftrag #221, Anwenderentscheid KI‑D‑E‑1): die Ansicht
        /// „Simulation" — achtunddreissig Felder aus
        /// <c>EPOS.UI.Seiten.Simulation.SimulationKiSicht</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum sie dazukommt.</b> Der Anwender hat am 11.09.2026 gemeldet: „Die
        /// KI-Buttons haben keine unterschiedliche Funktion im Kontext. Daher ist es
        /// nicht sinnvoll, auf einer Sicht zwei KI-Buttons zu sehen. Es muss einen
        /// Kontext in der KI-Funktion der zweiten Sicht geben." Bis hierher unterschied
        /// die Simulationsansicht vom Hauptfenster genau EINE Zeichenkette — der Bereich
        /// aus dem Hilfeschluessel. Sie meldet jetzt an, WAS auf ihr steht.
        /// </para>
        /// <para>
        /// <b>Drei Gruppen, drei Rollen.</b> Schritt ① traegt die KASKADE (lesend — sie
        /// ist eine Reihenfolge und kein Eingabefeld), danach stehen die FUENF
        /// LAUFPARAMETER (lesbar UND setzbar; sie sind die einzigen Zahlen der Ansicht,
        /// die der Anwender selbst eintraegt), und Schritt ③ traegt die KENNZAHLEN des
        /// Laufs (lesend — sie sind gerechnet). Die Setzseite folgt wie ueberall der
        /// Schreibbarkeit der Eigenschaft (<c>KiFeldzugang.Setzbar</c>); eine zweite
        /// Liste „was ist setzbar" gibt es nicht.
        /// </para>
        /// <para>
        /// <b>Die Betriebsart ist eine GANZZAHL und keine Aufzaehlung.</b> 0, 1 und 2
        /// stehen so in <c>Tab_Einstellungen</c> (Vorlaeufer <c>RadioButton.Tag</c>,
        /// <c>bhkwSimulationsArt</c>); ein Aufzaehlungstyp brauchte eine zweite
        /// Namensliste, die es nur fuer den Assistenten gaebe. Was 0, 1 und 2 bedeuten,
        /// steht in der Erlaeuterung.
        /// </para>
        /// <para>
        /// <b>Keine Knoepfe.</b> „Simulation starten ▶" und „Ergebnis speichern" sind
        /// rechnende bzw. datenbankwirksame Aktionen der Stufen 2 und 3 und gehoeren in
        /// das Aktionsregister mit Bestaetigung und Sicherungspunkt — dieselbe
        /// Begruendung wie bei der Stromspeicher-Ansicht.
        /// </para>
        /// <para>
        /// <b>Welle KI-F2: die BLAETTER der Ansicht kommen dazu.</b> Einundzwanzig
        /// Einstellwerte des Reiters „Stromspeicher" von Schritt ③ und der Lesepunkt
        /// aus der Fusszeile von Schritt ①. Sie gehen nicht auf, sie STEHEN auf der
        /// Ansicht — eine Maske ist, was offen ist, und eine eigene Maske je Reiterblatt
        /// waere eine Maske, die der Anwender nie oeffnet. Jedes dieser Felder schreibt
        /// SOFORT, ueber denselben Weg wie das Feld auf dem Bildschirm
        /// (<c>SpeicherfeldSchreiben</c> je Feldschluessel bzw.
        /// <c>LesepunktSchreiben</c>); der Speicherknopf, den
        /// <c>dialog_speichern</c> druecken koennte, fehlt hier weiterhin.
        /// </para>
        /// <para>
        /// <b>Die PREISREIHE bleibt draussen</b> — ein Verweis in eine
        /// kontextabhaengige Liste (rohe Id, deren Inhalt mit der Preisquelle
        /// wechselt); dieselbe Regel wie bei der Brennstoffvariante.
        /// </para>
        /// </remarks>
        private static KiDialog Simulation()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.SIMULATION,
                anzeigename: KiDialogTexte.MaskeSimulation,
                felder: new[]
                {
                    // ---- Wo der Anwender steht --------------------------------------
                    new KiDialogFeld("schritt", "SimulationKiSicht.Ansichtsschritt",
                                     KiDialogTexte.SimSchrittName, KiParameterTyp.Text,
                                     KiDialogTexte.SimSchrittErl, leerErlaubt: true),
                    new KiDialogFeld("reiter", "SimulationKiSicht.Reiter",
                                     KiDialogTexte.SimReiterName, KiParameterTyp.Text,
                                     KiDialogTexte.SimReiterErl, leerErlaubt: true),

                    // ---- Schritt ① : Kaskade und Reihenfolge (nur lesend) -----------
                    new KiDialogFeld("kaskade", "SimulationKiSicht.Kaskade",
                                     KiDialogTexte.SimKaskadeName, KiParameterTyp.Text,
                                     KiDialogTexte.SimKaskadeErl, leerErlaubt: true),
                    new KiDialogFeld("nicht_aufgenommen", "SimulationKiSicht.NichtAufgenommen",
                                     KiDialogTexte.SimOhnePlatzName, KiParameterTyp.Text,
                                     KiDialogTexte.SimOhnePlatzErl, leerErlaubt: true),

                    // ---- Die vier Laufparameter (lesbar und setzbar) -----------------
                    new KiDialogFeld("netzverluste", "SimulationKiSicht.Netzverluste",
                                     KiDialogTexte.SimNetzverlusteName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimNetzverlusteErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("bhkw_betriebsart", "SimulationKiSicht.BhkwBetriebsart",
                                     KiDialogTexte.SimBetriebsartName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SimBetriebsartErl),
                    new KiDialogFeld("bhkw_leistungsgrenze", "SimulationKiSicht.BhkwLeistungsgrenze",
                                     KiDialogTexte.SimLeistungsgrenzeName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.SimLeistungsgrenzeErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    // 16.09.2026 (Auftrag #299): Hier stand das Feld "wp_heizstab" -
                    // der PROJEKTweite Heizstabschalter. Er gehoert seither der
                    // WAERMEPUMPE (Tab_Energieanlagen.Heizstab je Anlage), und die
                    // Simulationsmaske fuehrt keinen projektweiten Wert mehr, den die KI
                    // setzen koennte. Aus fuenf Laufparametern sind damit VIER geworden.
                    new KiDialogFeld("kessel_bereitschaft", "SimulationKiSicht.KesselBereitschaft",
                                     KiDialogTexte.SimBereitschaftName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimBereitschaftErl,
                                     einheit: KiDialogTexte.EINHEIT_H_A),

                    // ---- Schritt ③ : die Kennzahlen des Laufs (nur lesend) ----------
                    new KiDialogFeld("waermebedarf", "SimulationKiSicht.WaermebedarfMwh",
                                     KiDialogTexte.SimWaermebedarfName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimWaermebedarfErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("waermedeckung", "SimulationKiSicht.WaermedeckungProzent",
                                     KiDialogTexte.SimWaermedeckungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimWaermedeckungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("restwaerme", "SimulationKiSicht.RestwaermeMwh",
                                     KiDialogTexte.SimRestwaermeName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimRestwaermeErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("strombedarf", "SimulationKiSicht.StrombedarfMwh",
                                     KiDialogTexte.SimStrombedarfName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimStrombedarfErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("stromdeckung", "SimulationKiSicht.StromdeckungProzent",
                                     KiDialogTexte.SimStromdeckungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimStromdeckungErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("reststrom", "SimulationKiSicht.ReststromMwh",
                                     KiDialogTexte.SimReststromName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimReststromErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("speicher_entladung", "SimulationKiSicht.SpeicherentladungMwh",
                                     KiDialogTexte.SimSpeicherEntladungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpeicherEntladungErl,
                                     einheit: KiDialogTexte.EINHEIT_MWH_A, leerErlaubt: true),
                    new KiDialogFeld("speicher_soc_band", "SimulationKiSicht.SpeicherSocBand",
                                     KiDialogTexte.SimSpeicherSocName, KiParameterTyp.Text,
                                     KiDialogTexte.SimSpeicherSocErl, leerErlaubt: true),
                    new KiDialogFeld("laufhinweise", "SimulationKiSicht.Laufhinweise",
                                     KiDialogTexte.SimHinweiseName, KiParameterTyp.Text,
                                     KiDialogTexte.SimHinweiseErl, leerErlaubt: true),

                    // ---- Der Lesepunkt der Fusszeile von ① (Welle KI-F2) -----------
                    new KiDialogFeld("lesepunkt_davor", "SimulationKiSicht.LesepunktDavor",
                                     KiDialogTexte.SimLesepunktName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimLesepunktErl),

                    // ---- Der Reiter „Stromspeicher" von ③ (Welle KI-F2) ------------
                    //
                    // Einundzwanzig EINSTELLWERTE der aktiven Speichervariante. Sie
                    // stehen auf einem BLATT der Ansicht und nicht in einem Dialog -
                    // eine Maske ist, was offen ist, und offen ist die Simulation.
                    // Jedes Feld schreibt SOFORT, ueber denselben Weg wie das Feld auf
                    // dem Bildschirm (SpeicherfeldSchreiben je Feldschluessel).
                    new KiDialogFeld("speicher_soc_min", "SimulationKiSicht.SpeicherSocMin",
                                     KiDialogTexte.SimSpSocMinName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpSocMinErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("speicher_soc_max", "SimulationKiSicht.SpeicherSocMax",
                                     KiDialogTexte.SimSpSocMaxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpSocMaxErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("speicher_ladeleistung",
                                     "SimulationKiSicht.SpeicherLadeleistung",
                                     KiDialogTexte.SimSpLadeleistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpLadeleistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("speicher_kapazitaet",
                                     "SimulationKiSicht.SpeicherKapazitaet",
                                     KiDialogTexte.SimSpKapazitaetName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpKapazitaetErl,
                                     einheit: KiDialogTexte.EINHEIT_KWH),
                    new KiDialogFeld("speicher_ladeschwelle",
                                     "SimulationKiSicht.SpeicherLadeschwelle",
                                     KiDialogTexte.SimSpLadeschwelleName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpLadeschwelleErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("speicher_betriebsart",
                                     "SimulationKiSicht.SpeicherBetriebsart",
                                     KiDialogTexte.SimSpBetriebsartName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpBetriebsartErl, leerErlaubt: true),
                    new KiDialogFeld("speicher_berechnungsart",
                                     "SimulationKiSicht.SpeicherBerechnungsart",
                                     KiDialogTexte.SimSpBerechnungsartName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpBerechnungsartErl, leerErlaubt: true),
                    new KiDialogFeld("speicher_peakziel", "SimulationKiSicht.SpeicherPeakZiel",
                                     KiDialogTexte.SimSpPeakZielName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpPeakZielErl,
                                     einheit: KiDialogTexte.EINHEIT_KW),
                    new KiDialogFeld("speicher_peakziel_adaptiv",
                                     "SimulationKiSicht.SpeicherPeakZielAdaptiv",
                                     KiDialogTexte.SimSpPeakAdaptivName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpPeakAdaptivErl),
                    new KiDialogFeld("speicher_kompatibilitaet",
                                     "SimulationKiSicht.SpeicherKompatibilitaet",
                                     KiDialogTexte.SimSpKompatibilitaetName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpKompatibilitaetErl),
                    new KiDialogFeld("speicher_laden_pv", "SimulationKiSicht.SpeicherLadenAusPv",
                                     KiDialogTexte.SimSpLadenPvName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpLadenPvErl),
                    new KiDialogFeld("speicher_laden_bhkw",
                                     "SimulationKiSicht.SpeicherLadenAusBhkw",
                                     KiDialogTexte.SimSpLadenBhkwName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpLadenBhkwErl),
                    new KiDialogFeld("speicher_netzentladung",
                                     "SimulationKiSicht.SpeicherNetzentladung",
                                     KiDialogTexte.SimSpNetzentladungName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpNetzentladungErl),

                    // Sichtbar, aber dauerhaft gesperrt (Ausbaustufe 11) - deshalb
                    // nurLesen: Der Assistent soll nicht anbieten, einen Schalter zu
                    // legen, den der Anwender nicht legen kann.
                    new KiDialogFeld("speicher_bhkw_stromgefuehrt",
                                     "SimulationKiSicht.SpeicherBhkwStromgefuehrt",
                                     KiDialogTexte.SimSpStromgefuehrtName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpStromgefuehrtErl, nurLesen: true),

                    new KiDialogFeld("speicher_kapitalzins",
                                     "SimulationKiSicht.SpeicherKapitalzins",
                                     KiDialogTexte.SimSpKapitalzinsName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpKapitalzinsErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT),
                    new KiDialogFeld("speicher_nutzungsdauer",
                                     "SimulationKiSicht.SpeicherNutzungsdauer",
                                     KiDialogTexte.SimSpNutzungsdauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpNutzungsdauerErl,
                                     einheit: KiDialogTexte.EINHEIT_JAHR),
                    new KiDialogFeld("speicher_leistungspreis",
                                     "SimulationKiSicht.SpeicherLeistungspreis",
                                     KiDialogTexte.SimSpLeistungspreisName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpLeistungspreisErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO_KW_A),
                    new KiDialogFeld("speicher_netzladeaufschlag",
                                     "SimulationKiSicht.SpeicherNetzladeaufschlag",
                                     KiDialogTexte.SimSpNetzaufschlagName, KiParameterTyp.Zahl,
                                     KiDialogTexte.SimSpNetzaufschlagErl,
                                     einheit: KiDialogTexte.EINHEIT_CT_KWH),
                    new KiDialogFeld("speicher_preisquelle",
                                     "SimulationKiSicht.SpeicherPreisquelle",
                                     KiDialogTexte.SimSpPreisquelleName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpPreisquelleErl, leerErlaubt: true),
                    new KiDialogFeld("speicher_preisreihe",
                                     "SimulationKiSicht.SpeicherPreisreihe",
                                     KiDialogTexte.SimSpPreisreiheName,
                                     KiParameterTyp.Wahl,
                                     KiDialogTexte.SimSpPreisreiheErl),
                    new KiDialogFeld("speicher_aufschlag",
                                     "SimulationKiSicht.SpeicherAufschlag",
                                     KiDialogTexte.SimSpAufschlagName,
                                     KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimSpAufschlagErl)
                });
        }
    }
}
