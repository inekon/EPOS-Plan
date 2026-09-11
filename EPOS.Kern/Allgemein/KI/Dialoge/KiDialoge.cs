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
    /// <b>Der Feldumfang der vier Masken ist UNVERAENDERT.</b> Er stammt aus Fachkonzept
    /// 11.6 („Feldumfang v1 = die von der Knopfpruefung erfassten Eingabefelder") und ist
    /// der Grund, warum drei der vier Masken so wenige Felder fuehren. Ihn zu erweitern
    /// ist eine fachliche Entscheidung mit eigener Abnahme und gehoert nicht in einen
    /// Schritt, der den Aufloesungsweg austauscht — sonst liesse sich hinterher nicht
    /// sagen, was den Feldblock veraendert hat.
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
                Simulation());
        }

        // =====================================================================
        // Form_Heizkessel_Bearbeiten  ->  HeizkesselKatalogDialog
        // =====================================================================

        /// <summary>
        /// Heizkessel bearbeiten — die 15 Felder der Knopfpruefung, seit Auftrag #200
        /// als Eigenschaften von <c>EPOS.UI.Dialoge.Erzeuger.HeizkesselKatalogDaten</c>.
        /// </summary>
        /// <remarks>
        /// Der Feldsatz ist derselbe wie zuvor; nur die Aufloesung hat gewechselt. Zwei
        /// Namen fallen dabei auf, und beide sind Absicht: Die thermische Leistung heisst
        /// im Daten-Objekt <c>Ptherm</c> (nicht <c>ThLeistung</c>), und der
        /// Bereitschaftsverlust <c>Betriebsbereitschaftverlust</c> — so stehen sie in der
        /// Oberflaeche, und der Katalog schreibt keine zweite Schreibweise daneben.
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
                    new KiDialogFeld("investitionskosten", "HeizkesselKatalogDaten.Investitionskosten",
                                     KiDialogTexte.HkInvestName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkInvestErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("wartungskosten", "HeizkesselKatalogDaten.Wartungskosten",
                                     KiDialogTexte.HkWartungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWartungErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("raumbedarf", "HeizkesselKatalogDaten.Raumbedarf",
                                     KiDialogTexte.HkRaumbedarfName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkRaumbedarfErl,
                                     einheit: KiDialogTexte.EINHEIT_M3, leerErlaubt: true),
                    new KiDialogFeld("nutzungsdauer", "HeizkesselKatalogDaten.Nutzungsdauer",
                                     KiDialogTexte.HkNutzungsdauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkNutzungsdauerErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true),
                    new KiDialogFeld("co2", "HeizkesselKatalogDaten.CO2",
                                     KiDialogTexte.HkCo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkCo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("so2", "HeizkesselKatalogDaten.SO2",
                                     KiDialogTexte.HkSo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkSo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("nox", "HeizkesselKatalogDaten.NOx",
                                     KiDialogTexte.HkNoxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkNoxErl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("co", "HeizkesselKatalogDaten.CO",
                                     KiDialogTexte.HkCoName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkCoErl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("staub", "HeizkesselKatalogDaten.Staub",
                                     KiDialogTexte.HkStaubName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkStaubErl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "HeizkesselKatalogDaten.Vorlauf",
                                     KiDialogTexte.HkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "HeizkesselKatalogDaten.Ruecklauf",
                                     KiDialogTexte.HkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
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
        /// holt sie bei jedem Lesen neu. Ist keine gewaehlt, sind die drei Felder leer —
        /// derselbe Zustand, den der Anwender auf der Maske sieht.
        /// </remarks>
        private static KiDialog Photovoltaik()
        {
            return new KiDialog(
                maskenname: KiMaskennamen.PHOTOVOLTAIK,
                anzeigename: KiDialogTexte.MaskePv,
                felder: new[]
                {
                    new KiDialogFeld("neigung", "ErzeugerZeile.Neigung",
                                     KiDialogTexte.PvNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("azimut", "ErzeugerZeile.Azimut",
                                     KiDialogTexte.PvAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("anzahl_module", "ErzeugerZeile.AnzahlModule",
                                     KiDialogTexte.PvAnzahlName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvAnzahlErl,
                                     leerErlaubt: true)
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
        /// Maske (Hersteller, Speichertyp, Bereitschaftsverluste, Investitionskosten)
        /// liefen im Vorlaeufer ueber stille Parser und gehoeren nach Fachkonzept 11.7
        /// erst nach ihrer Umstellung in den Katalog. Ob die Razor-Fassung das anders
        /// sieht, ist eine fachliche Frage mit eigener Abnahme — sie gehoert nicht in
        /// den Schritt, der den Aufloesungsweg austauscht.
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
                                     einheit: KiDialogTexte.EINHEIT_LITER, leerErlaubt: true)
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
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: false)
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
                                     KiDialogTexte.SpaBetriebszielName, KiParameterTyp.Aufzaehlung,
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
        /// „Simulation" — achtzehn Felder aus
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

                    // ---- Die fuenf Laufparameter (lesbar und setzbar) ----------------
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
                    new KiDialogFeld("wp_heizstab", "SimulationKiSicht.WpHeizstab",
                                     KiDialogTexte.SimHeizstabName, KiParameterTyp.Wahrheitswert,
                                     KiDialogTexte.SimHeizstabErl),
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
                                     KiDialogTexte.SimHinweiseErl, leerErlaubt: true)
                });
        }
    }
}
