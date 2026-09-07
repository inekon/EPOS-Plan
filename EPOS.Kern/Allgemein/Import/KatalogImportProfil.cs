using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welcher Katalogimport gemeint ist (iU9-W13.0a) — vier aus VDI 3805 und
    /// seit <b>W13-E-2</b> (07.09.2026) der Stromspeicher aus zwei TABELLARISCHEN
    /// Quellen.
    ///
    /// <para>Ein AUFZAEHLUNGSTYP und keine Zeichenkette — dasselbe Muster wie
    /// <c>BedarfsArt</c> aus Welle 8 und aus demselben Grund: Wo die Auspraegung
    /// ein Text waere, koennte eine Uebersetzung oder ein Tippfehler sie still
    /// ins Leere laufen lassen. Er liegt im Kern, weil ihn BEIDE Seiten brauchen:
    /// Der Ablauf waehlt danach Parser und Schreibweg, die Razor-Komponente ihre
    /// Beschriftungen.</para>
    /// </summary>
    public enum KatalogImportArt
    {
        /// <summary>VDI 3805 Blatt 3 — Waermeerzeuger nach <c>Tab_Heizkessel_STAMM</c>.</summary>
        Heizkessel,

        /// <summary>VDI 3805 Blatt 20 — Speicher nach <c>Tab_Pufferspeicher_STAMM</c>.</summary>
        Pufferspeicher,

        /// <summary>VDI 3805 Blatt 19 — Kollektoren nach <c>Tab_Solarkollektoren_STAMM</c>.</summary>
        Solarkollektoren,

        /// <summary>VDI 3805 Blatt 22 — Waermepumpen nach <c>Tab_WP_STAMM</c> und zwei Kennlinientabellen.</summary>
        Waermepumpe,

        /// <summary>
        /// Stromspeicher nach <c>Tab_Stromspeicher_STAMM</c> — die FUENFTE
        /// Auspraegung und die erste ohne VDI 3805 (Stufe S1 des
        /// <c>Konzept_Stromspeicherimport_EPOS-Plan.md</c>, Anwenderentscheid
        /// <b>W13-E-2</b> vom 07.09.2026, Fragen Q1…Q8 = Empfehlung).
        ///
        /// <para>Sie liest aus ZWEI Quellen: der CEC Energy Storage System List
        /// (Geraeteverzeichnis, 6 654 Saetze — Netzabruf oder Datei) und
        /// <c>bslib</c> (vier vermessene Systeme, Auslieferungsdatei). Deshalb
        /// traegt erst sie die drei Profilteile, die die vier VDI-Auspraegungen
        /// nicht brauchen: <see cref="KatalogImportProfil.Quellen"/>,
        /// <see cref="KatalogImportProfil.Listenspalten"/> und
        /// <see cref="KatalogImportProfil.Zweitfilter"/>.</para>
        /// </summary>
        Stromspeicher
    }

    /// <summary>
    /// <b>Eine Quelle, aus der eine Auspraegung lesen kann</b> (W13-E-2, Stufe S1).
    ///
    /// <para>Die vier VDI-Auspraegungen haben genau EINE Quelle — eine
    /// <c>.vdi</c>-Datei —, und dafuer genuegt der Dateiwaehler. Der
    /// Stromspeicher hat DREI: den Netzabruf der CEC-Liste, eine CEC-Datei vom
    /// Datentraeger und die mitgelieferte <c>bslib_database.csv</c>. Sie stehen
    /// als Knoepfe in der Kopfzeile der Maske — dieselbe Bauart wie beim Modul-
    /// und Wechselrichterimport (<c>ImportQuelle</c> in
    /// <see cref="ModulImportProfil"/>).</para>
    ///
    /// <para><see cref="AusDatei"/> sagt, ob der Knopf den DATEIWAEHLER oeffnet.
    /// Ist er <c>false</c>, beschafft der Wirt den Inhalt selbst (Netzabruf,
    /// Auslieferungsdatei) und der Schluessel sagt ihm, wie.</para>
    /// </summary>
    public sealed class KatalogImportQuelle
    {
        public KatalogImportQuelle(string schluessel, string beschriftung,
                                   bool ausDatei = false, string dateifilter = "")
        {
            Schluessel = schluessel ?? "";
            Beschriftung = beschriftung ?? "";
            AusDatei = ausDatei;
            Dateifilter = dateifilter ?? "";
        }

        /// <summary>Sprachneutraler ASCII-Schluessel, z. B. <c>CEC_NETZ</c>.</summary>
        public string Schluessel { get; }

        /// <summary>Beschriftung des Knopfes, bereits uebersetzt.</summary>
        public string Beschriftung { get; }

        /// <summary>Oeffnet der Knopf den Dateiwaehler?</summary>
        public bool AusDatei { get; }

        /// <summary>Dateifilter dieses Waehlers; leer = der des Profils.</summary>
        public string Dateifilter { get; }
    }

    /// <summary>
    /// Eine Spalte der Auswahlliste jenseits von Bezeichner und Hersteller
    /// (W13-E-2, Stufe S1).
    ///
    /// <para>Die vier VDI-Auspraegungen zeigen zwei Spalten — Eintrag und Firma —,
    /// und das genuegt, weil ihre Dateien je Hersteller kommen. Eine Liste mit
    /// 6 654 Geraeten von 130 Herstellern ist so nicht zu ueberblicken; sie
    /// braucht die Kennwerte in der ZEILE. Bleibt die Liste leer, zeichnet die
    /// Maske ihre zwei Bestandsspalten.</para>
    /// </summary>
    public sealed class KatalogImportSpalte
    {
        public KatalogImportSpalte(string schluessel, string titel)
        {
            Schluessel = schluessel ?? "";
            Titel = titel ?? "";
        }

        /// <summary>Schluessel des Detailwertes, den die Spalte zeigt.</summary>
        public string Schluessel { get; }

        /// <summary>Spaltenkopf, bereits uebersetzt.</summary>
        public string Titel { get; }
    }

    /// <summary>
    /// Ein ZWEITER Zahlenbereich der Filterleiste (W13-E-2, Stufe S1).
    ///
    /// <para>Eine Speicherliste von 1 bis 10 032 kWh und von 0,4 bis 4 904 kW ist
    /// mit EINER Groesse nicht einzugrenzen: Ein Heimspeicher und ein
    /// Netzspeicher unterscheiden sich in beiden. Die vier VDI-Auspraegungen
    /// fuehren keinen — dort ist <c>Zweitfilter</c> <c>null</c>, und die Maske
    /// zeichnet ihn nicht.</para>
    /// </summary>
    public sealed class KatalogFilterbereich
    {
        public KatalogFilterbereich(string bezeichnung, int nachkommastellen,
                                    double von, double bis, double maximum)
        {
            Bezeichnung = bezeichnung ?? "";
            Nachkommastellen = nachkommastellen;
            Von = von;
            Bis = bis;
            Maximum = maximum;
        }

        /// <summary>Beschriftung des Feldes „von", bereits uebersetzt.</summary>
        public string Bezeichnung { get; }

        /// <summary>Nachkommastellen beider Felder.</summary>
        public int Nachkommastellen { get; }

        /// <summary>Vorbelegung der Untergrenze.</summary>
        public double Von { get; }

        /// <summary>Vorbelegung der Obergrenze.</summary>
        public double Bis { get; }

        /// <summary>Obergrenze beider Felder.</summary>
        public double Maximum { get; }
    }

    /// <summary>
    /// Ein Detailfeld der Einlesemaske: Schluessel, Beschriftung, Einheit und ob
    /// es der Anwender aendern darf.
    ///
    /// <para><b>Nur der Bezeichner ist aenderbar.</b> In allen vier Masken des
    /// Bestands traegt jedes Detailfeld ausser <c>textBox_Name</c> ein
    /// <c>Enabled = false</c> in seiner <c>.resx</c> — die Kennwerte zeigen an,
    /// was in der Datei steht, und werden nicht von Hand korrigiert. Das Feld
    /// <see cref="Editierbar"/> haelt genau diesen Bestand fest.</para>
    /// </summary>
    public sealed class ImportDetailfeld
    {
        public ImportDetailfeld(string schluessel, string bezeichnung, string einheit = "",
                                bool editierbar = false)
        {
            Schluessel = schluessel;
            Bezeichnung = bezeichnung;
            Einheit = einheit ?? "";
            Editierbar = editierbar;
        }

        /// <summary>Sprachneutraler ASCII-Schluessel — der Zugriff auf den Wert des Satzes.</summary>
        public string Schluessel { get; }

        /// <summary>Beschriftung, bereits uebersetzt (der Aufrufer reicht den Ressourcentext herein).</summary>
        public string Bezeichnung { get; }

        /// <summary>Einheit hinter dem Feld; leer, wenn die Maske keine fuehrt.</summary>
        public string Einheit { get; }

        /// <summary>Darf der Anwender das Feld aendern? Im Bestand nur der Bezeichner.</summary>
        public bool Editierbar { get; }
    }

    /// <summary>
    /// <b>Die Auspraegung eines Katalogimports</b> (iU9-W13.0a) — alles, worin sich
    /// die vier VDI-3805-Einlesemasken unterscheiden, als DATEN.
    ///
    /// <para><b>Warum es das gibt.</b> Der Bauplan der vier Masken steht viermal
    /// wortgleich im Bestand: dieselben dreizehn Bausteine, dieselben Kommentare,
    /// bis hin zum falschen Handlernamen <c>Liste_WP_SelectedIndexChanged</c> in
    /// drei von vier (Befund W13-B15). Was sie wirklich trennt, sind sieben Werte
    /// — Katalogschluessel, Unterordner, Dateifilter, Filtergroesse samt
    /// Vorbelegung, Detailfeldliste, Vergleichswerte und Schreibweg. Sie stehen
    /// hier; der Ablauf und die Komponente gibt es je einmal.</para>
    ///
    /// <para><b>Die Vorbelegungen sind woertlich</b> aus den vier Designern
    /// uebernommen (10…200 mit einer Nachkommastelle, 0…1000 ohne, 0…5 mit zwei,
    /// 0…100 ohne) und bleiben bitgleich — sie sind das, was der Anwender beim
    /// Oeffnen sieht.</para>
    ///
    /// <para><b>Die Beschriftungen kommen von aussen.</b> Der Kern kennt keine
    /// Anzeigetexte; <see cref="Finde"/> nimmt einen Uebersetzer entgegen
    /// (Schluessel → Text), damit dieselbe Auspraegung unter Windows und auf iOS
    /// dieselben Felder in derselben Reihenfolge liefert, nur eben uebersetzt.</para>
    /// </summary>
    public sealed class KatalogImportProfil
    {
        /// <summary>Welche der vier Auspraegungen.</summary>
        public KatalogImportArt Art { get; private set; }

        /// <summary>Schluessel in der <see cref="KatalogRegistry"/> (<c>HEIZKESSEL</c>, <c>WP</c> …).</summary>
        public string Katalogschluessel { get; private set; }

        /// <summary>
        /// Unterordner unterhalb von <c>Settings.VDI3805Path</c>, in dem der
        /// Dateiwaehler startet.
        /// </summary>
        public string Unterordner { get; private set; }

        /// <summary>
        /// Der ALTE Unterordner, falls der Bestand einen anderen benutzt hat —
        /// nur beim LESEN als Rueckfall, wenn <see cref="Unterordner"/> nicht
        /// existiert. Bei der Waermepumpe hiess er schlicht <c>VDI</c>
        /// (Befund W13-B28); ein Anwender, der seine Kataloge dort abgelegt hat,
        /// soll sie weiter finden. Leer = kein Rueckfall.
        /// </summary>
        public string UnterordnerRueckfall { get; private set; }

        /// <summary>Dateifilter des Waehlers — bei allen vier <c>(*.vdi)|*.vdi</c>.</summary>
        public string Dateifilter { get; private set; }

        /// <summary>Beschriftung der Filtergroesse, z. B. „Th. Leistung [kW] von:".</summary>
        public string FilterBezeichnung { get; private set; }

        /// <summary>Nachkommastellen der beiden Filterfelder (1 / 0 / 2 / 0).</summary>
        public int FilterNachkommastellen { get; private set; }

        /// <summary>Vorbelegung der Untergrenze (10 / 0 / 0 / 0).</summary>
        public double FilterVon { get; private set; }

        /// <summary>Vorbelegung der Obergrenze (200 / 1000 / 5 / 100).</summary>
        public double FilterBis { get; private set; }

        /// <summary>Obergrenze der beiden Filterfelder — in allen vier Designern 100 000.</summary>
        public double FilterMaximum { get; private set; }

        /// <summary>Die Detailfelder in der Reihenfolge der Maske (7 / 5 / 10 / 10).</summary>
        public IReadOnlyList<ImportDetailfeld> Detailfelder { get; private set; }

        /// <summary>Der Bereichsschluessel des Infoknopfs (<c>Heizkessel</c>, <c>Wärmepumpe</c> …).</summary>
        public string HilfeSchluessel { get; private set; }

        /// <summary>
        /// Die Quellknoepfe der Kopfzeile. LEER heisst: eine Quelle, und die
        /// Maske zeigt ihren Dateiwaehler wie bisher (die vier VDI-Auspraegungen).
        /// </summary>
        public IReadOnlyList<KatalogImportQuelle> Quellen { get; private set; }
            = new KatalogImportQuelle[0];

        /// <summary>
        /// Die Spalten der Auswahlliste. LEER heisst: Eintrag und Firma, wie im
        /// Bestand.
        /// </summary>
        public IReadOnlyList<KatalogImportSpalte> Listenspalten { get; private set; }
            = new KatalogImportSpalte[0];

        /// <summary>Der zweite Zahlenbereich der Filterleiste; <c>null</c> = keiner.</summary>
        public KatalogFilterbereich Zweitfilter { get; private set; }

        /// <summary>
        /// Traegt die Filterleiste eine Herstellerklappliste? Sie lohnt erst,
        /// wenn eine Datei viele Hersteller fuehrt — die VDI-Dateien kommen JE
        /// Hersteller, die CEC-Liste bringt 130 auf einmal.
        /// </summary>
        public bool HerstellerFilter { get; private set; }

        /// <summary>
        /// Die Herleitungszeile unter den Detailfeldern; leer = keine. Sie sagt,
        /// was die Quelle NICHT liefert — beim Stromspeicher die Kosten
        /// (Entscheid W13-E-2-Q3), bei der Waermepumpe die Bedeutung der Stufe 0.
        /// </summary>
        public string Hinweis { get; private set; } = "";

        /// <summary>Die Katalogdefinition zu <see cref="Katalogschluessel"/>.</summary>
        public KatalogDefinition Katalog => KatalogRegistry.Finde(Katalogschluessel);

        // ==================================================================
        // Die vier Auspraegungen
        // ==================================================================

        /// <summary>
        /// Die Auspraegung zu einer Importart. <paramref name="text"/> uebersetzt
        /// einen Beschriftungsschluessel; <c>null</c> liefert den Schluessel selbst
        /// zurueck (fuer Tests und fuer eine Umgebung ohne Katalog).
        /// </summary>
        public static KatalogImportProfil Finde(KatalogImportArt art, Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            switch (art)
            {
                case KatalogImportArt.Heizkessel:
                    return new KatalogImportProfil
                    {
                        Art = art,
                        Katalogschluessel = "HEIZKESSEL",
                        Unterordner = "VDI_Heizkessel",
                        UnterordnerRueckfall = "",
                        Dateifilter = VdiFilter,
                        FilterBezeichnung = t("IMP_KAT_FILTER_LEISTUNG"),
                        FilterNachkommastellen = 1,
                        FilterVon = 10,
                        FilterBis = 200,
                        FilterMaximum = 100000,
                        HilfeSchluessel = "Heizkessel",
                        Detailfelder = new[]
                        {
                            new ImportDetailfeld(FeldName,        t("IMP_KAT_FELD_NAME"),        "", editierbar: true),
                            new ImportDetailfeld(FeldFirma,       t("IMP_KAT_FELD_FIRMA")),
                            new ImportDetailfeld("BAUART",        t("IMP_KAT_FELD_BAUART")),
                            new ImportDetailfeld("THLEISTUNG",    t("IMP_KAT_FELD_THLEISTUNG"),  t("IMP_KAT_EINH_KWTH")),
                            new ImportDetailfeld("BRENNSTOFF",    t("IMP_KAT_FELD_BRENNSTOFF")),
                            new ImportDetailfeld("WIRKUNGSGRAD",  t("IMP_KAT_FELD_WIRKUNGSGRAD"), t("IMP_KAT_EINH_PROZENT")),
                            new ImportDetailfeld("VERLUSTE",      t("IMP_KAT_FELD_VERLUSTE"),     t("IMP_KAT_EINH_KW"))
                        }
                    };

                case KatalogImportArt.Pufferspeicher:
                    return new KatalogImportProfil
                    {
                        Art = art,
                        Katalogschluessel = "PUFFERSPEICHER",
                        Unterordner = "VDI_Pufferspeicher",
                        UnterordnerRueckfall = "",
                        Dateifilter = VdiFilter,
                        FilterBezeichnung = t("IMP_KAT_FILTER_VOLUMEN"),
                        FilterNachkommastellen = 0,
                        FilterVon = 0,
                        FilterBis = 1000,
                        FilterMaximum = 100000,
                        HilfeSchluessel = "Pufferspeicher",
                        Detailfelder = new[]
                        {
                            new ImportDetailfeld(FeldName,     t("IMP_KAT_FELD_NAME"),  "", editierbar: true),
                            new ImportDetailfeld(FeldFirma,    t("IMP_KAT_FELD_FIRMA")),
                            new ImportDetailfeld("SPEICHERTYP", t("IMP_KAT_FELD_SPEICHERTYP")),
                            new ImportDetailfeld("VOLUMEN",     t("IMP_KAT_FELD_VOLUMEN"),  t("IMP_KAT_EINH_LITER")),
                            new ImportDetailfeld("VERLUSTE",    t("IMP_KAT_FELD_VERLUSTE"), t("IMP_KAT_EINH_KWHD"))
                        }
                    };

                case KatalogImportArt.Solarkollektoren:
                    return new KatalogImportProfil
                    {
                        Art = art,
                        Katalogschluessel = "SOLARKOLLEKTOREN",
                        Unterordner = "VDI_Solarthermie",
                        UnterordnerRueckfall = "",
                        Dateifilter = VdiFilter,
                        FilterBezeichnung = t("IMP_KAT_FILTER_APERTUR"),
                        FilterNachkommastellen = 2,
                        FilterVon = 0,
                        FilterBis = 5,
                        FilterMaximum = 100000,
                        HilfeSchluessel = "Solarthermie",
                        Detailfelder = new[]
                        {
                            new ImportDetailfeld(FeldName,      t("IMP_KAT_FELD_NAME"),  "", editierbar: true),
                            new ImportDetailfeld(FeldFirma,     t("IMP_KAT_FELD_FIRMA")),
                            new ImportDetailfeld("BAUART",      t("IMP_KAT_FELD_BAUART")),
                            new ImportDetailfeld("BESCHREIBUNG", t("IMP_KAT_FELD_BESCHREIBUNG")),
                            new ImportDetailfeld("APERTUR",     t("IMP_KAT_FELD_APERTUR"),  t("IMP_KAT_EINH_M2")),
                            new ImportDetailfeld("LEISTUNG",    t("IMP_KAT_FELD_SPITZENLEISTUNG"), t("IMP_KAT_EINH_WM2")),
                            new ImportDetailfeld("H0",          t("IMP_KAT_FELD_H0")),
                            new ImportDetailfeld("A1",          t("IMP_KAT_FELD_A1"),  t("IMP_KAT_EINH_WM2K")),
                            new ImportDetailfeld("A2",          t("IMP_KAT_FELD_A2"),  t("IMP_KAT_EINH_WM2K")),
                            new ImportDetailfeld("KDIR",        t("IMP_KAT_FELD_KDIR")),
                            new ImportDetailfeld("KDIFF",       t("IMP_KAT_FELD_KDIFF"))
                        }
                    };

                case KatalogImportArt.Waermepumpe:
                    return new KatalogImportProfil
                    {
                        Art = art,
                        // Der Bestand nimmt hier nur "VDI" (Befund W13-B28); der
                        // neue Ordner traegt wie die drei anderen sein Gewerk, der
                        // alte bleibt Rueckfall (Abweichung A-1 im Portprotokoll).
                        Katalogschluessel = "WP",
                        Unterordner = "VDI_Waermepumpe",
                        UnterordnerRueckfall = "VDI",
                        Dateifilter = VdiFilter,
                        FilterBezeichnung = t("IMP_KAT_FILTER_LEISTUNG"),
                        FilterNachkommastellen = 0,
                        FilterVon = 0,
                        FilterBis = 100,
                        FilterMaximum = 100000,
                        HilfeSchluessel = "Wärmepumpe",
                        // Seit W13-E-2 steht der Hinweis IM Profil statt als
                        // Sonderfall in der Maske: Der Stromspeicher braucht
                        // dieselbe Zeile fuer die fehlenden Kosten, und zwei
                        // Wege zu einer Zeile liefen auseinander.
                        Hinweis = t("IMP_KAT_HINWEIS_STUFEN"),
                        Detailfelder = new[]
                        {
                            new ImportDetailfeld(FeldName,       t("IMP_KAT_FELD_NAME"),  "", editierbar: true),
                            new ImportDetailfeld(FeldFirma,      t("IMP_KAT_FELD_FIRMA")),
                            new ImportDetailfeld("TYP",          t("IMP_KAT_FELD_TYP")),
                            new ImportDetailfeld("AUFSTELLUNG",  t("IMP_KAT_FELD_AUFSTELLUNG")),
                            new ImportDetailfeld("THLEISTUNG",   t("IMP_KAT_FELD_THLEISTUNG"), t("IMP_KAT_EINH_KWTH")),
                            new ImportDetailfeld("ZUSATZHEIZUNG", t("IMP_KAT_FELD_ZUSATZHEIZUNG"), t("IMP_KAT_EINH_KW")),
                            new ImportDetailfeld("STUFEN",       t("IMP_KAT_FELD_STUFEN")),
                            new ImportDetailfeld("MAXVORLAUF",   t("IMP_KAT_FELD_MAXVORLAUF")),
                            new ImportDetailfeld("WIRKUNGSGRAD", t("IMP_KAT_FELD_WIRKUNGSGRAD")),
                            new ImportDetailfeld("KUEHLLEISTUNG", t("IMP_KAT_FELD_KUEHLLEISTUNG"), t("IMP_KAT_EINH_KWCOOL"))
                        }
                    };

                case KatalogImportArt.Stromspeicher:
                    return new KatalogImportProfil
                    {
                        Art = art,
                        Katalogschluessel = "STROMSPEICHER",
                        // Kein VDI-Ordner: Die zwei Quellen sind Tabellen, und
                        // die mitgelieferte bslib-Datei liegt in genau diesem
                        // Unterordner des Herstellerdatenpfades.
                        Unterordner = "Stromspeicher",
                        UnterordnerRueckfall = "",
                        Dateifilter = TabellenFilter,
                        FilterBezeichnung = t("IMP_KAT_FILTER_ENERGIE"),
                        FilterNachkommastellen = 1,
                        // Die Vorbelegung zeigt ALLES. Bei den vier VDI-Importen
                        // stand hier die Zahl aus dem Designer; hier gibt es
                        // keinen Vorlaeufer, und eine Vorbelegung, die Zeilen
                        // verschwinden liesse, waere beim Aufmachen unerklaerlich
                        // (die CEC-Liste reicht von 1 bis 10 032 kWh).
                        FilterVon = 0,
                        FilterBis = 100000,
                        FilterMaximum = 100000,
                        HilfeSchluessel = "Stromspeicher",
                        Zweitfilter = new KatalogFilterbereich(
                            t("IMP_KAT_FILTER_LEISTUNG_KW"), 1, 0, 100000, 100000),
                        HerstellerFilter = true,
                        // ENTSCHEID W13-E-2-Q3 (07.09.2026): Keine der vier
                        // geprueften Quellen fuehrt Kosten, und eine erfundene
                        // Zahl in einer Wirtschaftlichkeitsrechnung ist
                        // schlimmer als eine fehlende. Die Felder bleiben leer -
                        // und die Maske SAGT es.
                        Hinweis = t("IMP_KAT_HINWEIS_KOSTEN"),
                        Quellen = new[]
                        {
                            // ENTSCHEID W13-E-2-Q2/Q8: Die CEC-Speicherliste wird
                            // NICHT mitgeliefert (ihre Nutzungsbedingungen
                            // untersagen die kommerzielle Nutzung); statt dessen
                            // holt der Anwender sie mit diesem Knopf selbst - so
                            // wie er es im Browser auch taete.
                            new KatalogImportQuelle(QUELLE_CEC_NETZ, t("IMP_KAT_QUELLE_CEC_NETZ")),
                            new KatalogImportQuelle(QUELLE_CEC_DATEI, t("IMP_KAT_QUELLE_CEC_DATEI"),
                                                    ausDatei: true, dateifilter: TabellenFilter),
                            // bslib DARF mitgeliefert werden (CC BY 4.0) und wird
                            // es: Der Knopf liest die Auslieferungsdatei ohne
                            // Waehler; fehlt sie, faellt der Wirt auf ihn zurueck.
                            new KatalogImportQuelle(QUELLE_BSLIB, t("IMP_KAT_QUELLE_BSLIB"))
                        },
                        Listenspalten = new[]
                        {
                            new KatalogImportSpalte(FeldQuelle,   t("IMP_KAT_SP_QUELLE")),
                            // Ein SPALTENKOPF traegt keinen Doppelpunkt - der
                            // gehoert zur Feldbeschriftung daneben, nicht zur
                            // Ueberschrift darueber.
                            new KatalogImportSpalte(FeldFirma,    t("IMP_KAT_SP_HERSTELLER")),
                            new KatalogImportSpalte("MODELL",     t("IMP_KAT_SP_MODELL")),
                            new KatalogImportSpalte("ENERGIE",    t("IMP_KAT_SP_ENERGIE")),
                            new KatalogImportSpalte("LEISTUNG",   t("IMP_KAT_SP_LEISTUNG")),
                            new KatalogImportSpalte("ETA",        t("IMP_KAT_SP_ETA")),
                            new KatalogImportSpalte("TYP",        t("IMP_KAT_SP_CHEMIE"))
                        },
                        Detailfelder = new[]
                        {
                            new ImportDetailfeld(FeldName,   t("IMP_KAT_FELD_NAME"), "", editierbar: true),
                            new ImportDetailfeld(FeldFirma,  t("IMP_KAT_FELD_FIRMA")),
                            new ImportDetailfeld("MODELL",   t("IMP_KAT_FELD_MODELL")),
                            new ImportDetailfeld("TYP",      t("IMP_KAT_FELD_CHEMIE")),
                            new ImportDetailfeld("ENERGIE",  t("IMP_KAT_FELD_ENERGIE"),  t("IMP_KAT_EINH_KWH")),
                            new ImportDetailfeld("LEISTUNG", t("IMP_KAT_FELD_LEISTUNG"), t("IMP_KAT_EINH_KW")),
                            new ImportDetailfeld("ETA",      t("IMP_KAT_FELD_ETA_RT")),
                            new ImportDetailfeld("STANDBY",  t("IMP_KAT_FELD_STANDBY"),  t("IMP_KAT_EINH_W")),
                            new ImportDetailfeld(FeldQuelle, t("IMP_KAT_FELD_QUELLE"))
                        }
                    };
            }

            throw new ArgumentOutOfRangeException(nameof(art));
        }

        /// <summary>Alle Auspraegungen — fuer Stapelpruefungen.</summary>
        public static IEnumerable<KatalogImportArt> AlleArten
        {
            get
            {
                yield return KatalogImportArt.Heizkessel;
                yield return KatalogImportArt.Pufferspeicher;
                yield return KatalogImportArt.Solarkollektoren;
                yield return KatalogImportArt.Waermepumpe;
                yield return KatalogImportArt.Stromspeicher;
            }
        }

        // ==================================================================
        // Schluessel, die alle vier teilen
        // ==================================================================

        /// <summary>Der Bezeichner — das einzige Feld, das der Anwender aendern darf.</summary>
        public const string FeldName = "NAME";

        /// <summary>Der Hersteller — in allen vier vorhanden und immer gesperrt.</summary>
        public const string FeldFirma = "FIRMA";

        /// <summary>
        /// Die QUELLE eines Satzes — nur der Stromspeicher fuehrt sie, weil nur
        /// er aus zwei Listen liest und man einer Zeile ansehen muss, aus
        /// welcher sie kommt.
        /// </summary>
        public const string FeldQuelle = "QUELLE";

        /// <summary>Der Dateifilter aller vier Einlesemasken, woertlich.</summary>
        public const string VdiFilter = "(*.vdi)|*.vdi";

        /// <summary>
        /// Der Dateifilter des Stromspeicherimports: die CEC-Mappe und jede
        /// daraus (oder aus bslib) ausgeleitete Tabelle.
        /// </summary>
        public const string TabellenFilter = "(*.xlsx;*.csv)|*.xlsx;*.csv";

        /// <summary>Quellschluessel: die CEC-Speicherliste aus dem NETZ abrufen.</summary>
        public const string QUELLE_CEC_NETZ = "CEC_NETZ";

        /// <summary>Quellschluessel: eine CEC-Speicherliste vom Datentraeger (XLSX oder CSV).</summary>
        public const string QUELLE_CEC_DATEI = "CEC_DATEI";

        /// <summary>Quellschluessel: die mitgelieferte <c>bslib_database.csv</c>.</summary>
        public const string QUELLE_BSLIB = "BSLIB";
    }
}
