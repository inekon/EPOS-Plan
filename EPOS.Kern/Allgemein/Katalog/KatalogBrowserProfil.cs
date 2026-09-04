using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welcher der vier Erzeuger-Katalogbrowser gemeint ist (iU9-W14a.0a).
    ///
    /// <para>Ein AUFZAEHLUNGSTYP und keine Zeichenkette — dasselbe Muster wie
    /// <c>BedarfsArt</c> (W8) und <see cref="KatalogImportArt"/> (W13.0a), und aus
    /// demselben Grund: Waere die Auspraegung ein Text, koennte eine Uebersetzung oder
    /// ein Tippfehler sie still ins Leere laufen lassen. Er liegt im Kern, weil ihn
    /// BEIDE Seiten brauchen — der Controller waehlt danach Tabelle und Filterweg, die
    /// Razor-Komponente ihre Felder.</para>
    /// </summary>
    public enum KatalogBrowserArt
    {
        /// <summary><c>Tab_Heizkessel_STAMM</c> — Vorlaeufer <c>Form_Heizkessel_Admin</c>.</summary>
        Heizkessel,

        /// <summary><c>Tab_BHKW_STAMM</c> — Vorlaeufer <c>Form_BHKWAdmin</c>.</summary>
        Bhkw,

        /// <summary><c>Tab_Solarkollektoren_STAMM</c> — Vorlaeufer <c>Form_SolarKollektorenAdmin</c>.</summary>
        Solarkollektoren,

        /// <summary><c>Tab_Pufferspeicher_STAMM</c> — Vorlaeufer <c>Form_PufferSp_Admin</c>.</summary>
        Pufferspeicher
    }

    /// <summary>
    /// Welche Filterleiste ein Browser fuehrt — 0, 1 oder 2 Klapplisten.
    /// </summary>
    public enum KatalogFilterArt
    {
        /// <summary>Keine Filterleiste (Solarkollektoren).</summary>
        Keiner,

        /// <summary>Brennstoffgruppe und Leistungsstufe (Heizkessel, BHKW).</summary>
        BrennstoffUndLeistung,

        /// <summary>Hersteller und Volumenstufe (Pufferspeicher).</summary>
        HerstellerUndVolumen
    }

    /// <summary>
    /// Was fuer ein Feld ein Detailfeld des Browsers ist — davon haengt das
    /// Standardfeld der Oberflaeche ab und, beim Speicherweg, die Zahlregel.
    /// </summary>
    public enum BrowserFeldArt
    {
        /// <summary><c>Textfeld</c>.</summary>
        Text,

        /// <summary><c>Textfeld</c> mit <c>Mehrzeilig</c>.</summary>
        Mehrzeilig,

        /// <summary><c>Zahlenfeld</c> (Komma und Punkt, leer = 0).</summary>
        Zahl,

        /// <summary><c>Ganzzahlfeld</c>.</summary>
        Ganzzahl,

        /// <summary><c>Schalter</c> (nur der Brennwertkessel).</summary>
        Schalter
    }

    /// <summary>
    /// Ein Detailfeld des Katalogbrowsers: Schluessel, Beschriftung, Einheit, Feldart
    /// und ob der Speicherweg es zurueckschreibt.
    /// </summary>
    /// <remarks>
    /// <para><b>Der Schluessel ist sprachneutral</b> und zugleich der Schluessel, unter
    /// dem <c>…StammCtrl.KatalogsatzAnzeige</c> den Wert liefert. So bleibt die
    /// Zuordnung zwischen Datenbankspalte, Anzeigefeld und Speicherweg an EINEM Ort.</para>
    /// <para><b><see cref="Editierbar"/> ist der Speicherweg</b>: Genau die Felder mit
    /// <c>true</c> schreibt der Browser zurueck — beim Heizkessel sechs (Beschreibung,
    /// Leistung, Investitionskosten, Brennwert, Vorlauf, Ruecklauf), beim BHKW sechs
    /// (Hersteller, thermische und elektrische Leistung, untere Grenzleistung, Vorlauf,
    /// Ruecklauf). Solarkollektoren und Pufferspeicher haben keinen Speicherweg; dort
    /// ist jedes Feld <c>false</c>.</para>
    /// </remarks>
    public sealed class BrowserDetailfeld
    {
        public BrowserDetailfeld(string schluessel, string bezeichnung, string einheit = "",
                                 BrowserFeldArt art = BrowserFeldArt.Text, bool editierbar = false)
        {
            Schluessel = schluessel;
            Bezeichnung = bezeichnung;
            Einheit = einheit ?? "";
            Art = art;
            Editierbar = editierbar;
        }

        /// <summary>Sprachneutraler ASCII-Schluessel — zugleich der Zugriff auf den Wert.</summary>
        public string Schluessel { get; }

        /// <summary>Beschriftung, bereits uebersetzt (der Aufrufer reicht den Text herein).</summary>
        public string Bezeichnung { get; }

        /// <summary>Einheit hinter dem Feld; leer, wenn die Maske keine fuehrt.</summary>
        public string Einheit { get; }

        /// <summary>Textfeld, Zahlenfeld, Ganzzahlfeld oder Schalter.</summary>
        public BrowserFeldArt Art { get; }

        /// <summary>Schreibt der Speicherweg des Browsers dieses Feld zurueck?</summary>
        public bool Editierbar { get; }

        /// <summary>Der Feldname, den eine Pruefmeldung nennt — die Beschriftung ohne „:".</summary>
        public string Feldname => (Bezeichnung ?? "").TrimEnd(' ', ':');
    }

    /// <summary>
    /// <b>Die Auspraegung eines Erzeuger-Katalogbrowsers</b> (iU9-W14a.0a) — alles, worin
    /// sich die vier Admin-Masken <c>Form_Heizkessel_Admin</c>, <c>Form_BHKWAdmin</c>,
    /// <c>Form_SolarKollektorenAdmin</c> und <c>Form_PufferSp_Admin</c> unterscheiden,
    /// als DATEN.
    ///
    /// <para><b>Warum es das gibt.</b> Der Bauplan der vier Masken ist derselbe: Liste
    /// links, 0–2 Filterklapplisten, ein Detailblock rechts, „Neu…" / „Bearbeiten…" /
    /// „Löschen" / „OK". Was sie trennt, sind ACHT Werte — Stammtabelle, ein- oder
    /// zweispaltige Liste samt Textbauplan, Filterart, Detailfeldliste, Editorschluessel,
    /// Speicherweg, Loeschtext und Hilfeziel. Sie stehen hier; die Komponente gibt es
    /// einmal.</para>
    ///
    /// <para><b>Die Beschriftungen kommen von aussen.</b> Der Kern kennt keine
    /// Anzeigetexte; <see cref="Finde"/> nimmt einen Uebersetzer entgegen
    /// (Schluessel → Text), damit dieselbe Auspraegung unter Windows und auf iOS
    /// dieselben Felder in derselben Reihenfolge liefert, nur eben uebersetzt. Genau das
    /// Vorgehen von <see cref="KatalogImportProfil"/>.</para>
    ///
    /// <para><b>Die Einheiten stehen sprachneutral hier</b> (kW, €, °C, m², l, kWh/d, %,
    /// kWel, kWth) — dieselbe Aufteilung wie in <c>Form_AdminStromspeicher
    /// .InitGeraetefelder</c>: Wortmarke aus dem Katalog, Symbol am Feld.</para>
    /// </summary>
    public sealed class KatalogBrowserProfil
    {
        /// <summary>Welche der vier Auspraegungen.</summary>
        public KatalogBrowserArt Art { get; private set; }

        /// <summary>Die Stammtabelle — zugleich die Antwort auf „welcher Katalog".</summary>
        public string Stammtabelle { get; private set; }

        /// <summary>
        /// Zeigt die Liste eine zweite Spalte mit einem mehrzeiligen Eigenschaftentext?
        /// BHKW und Solarkollektoren ja (dort ein <c>DataGridView</c>), Heizkessel und
        /// Pufferspeicher nein (dort eine <c>ListBox</c>).
        /// </summary>
        public bool Zweispaltig { get; private set; }

        /// <summary>Welche Filterleiste (0, 2 Klapplisten).</summary>
        public KatalogFilterArt Filterart { get; private set; }

        /// <summary>Die Detailfelder in der Reihenfolge der Maske (8 / 8 / 8 / 6).</summary>
        public IReadOnlyList<BrowserDetailfeld> Detailfelder { get; private set; }

        /// <summary>
        /// Schreibt der Browser selbst zurueck? Heizkessel und BHKW ja (der Speicherweg
        /// vom 18.08.2026, im Vorlaeufer die <c>SpeichernLeiste</c>), die beiden anderen
        /// nein.
        /// </summary>
        public bool HatSpeicherweg { get; private set; }

        /// <summary>
        /// Zeichnet die Liste schreibgeschuetzte Saetze grau und fragt beim
        /// Ueberschreiben nach? Nur das BHKW (Vorlaeufer <c>:202</c> und <c>:418</c>) —
        /// in der Auslieferungsdatenbank sind dort ALLE Saetze geschuetzt.
        /// </summary>
        public bool ZeigtSchreibschutz { get; private set; }

        /// <summary>
        /// Der Schluessel des Infoknopfs — die ZEILE LINKS in <c>help_mapping.txt</c>
        /// (<c>Form_X.btn_Help</c>), nicht das Ziel rechts. Die Zuordnung bleibt damit
        /// unveraendert, obwohl die Maske dahinter nicht mehr existiert; dasselbe
        /// Vorgehen wie bei den vier Einlesemasken der Welle 13.
        /// </summary>
        public string HilfeSchluessel { get; private set; }

        /// <summary>Fenstertitel, bereits uebersetzt.</summary>
        public string Titel { get; private set; }

        /// <summary>Beschriftung ueber der Liste, bereits uebersetzt.</summary>
        public string Listenbeschriftung { get; private set; }

        /// <summary>Ueberschrift des Detailblocks, bereits uebersetzt.</summary>
        public string Detailueberschrift { get; private set; }

        /// <summary>
        /// Meldung, wenn ein Knopf ohne Auswahl gedrueckt wird; LEER bei Heizkessel und
        /// Pufferspeicher, die im Bestand still zurueckkehren (Regel F3).
        /// </summary>
        public string MeldungOhneAuswahl { get; private set; }

        /// <summary>
        /// Die Schluessel der beiden Filterklapplisten, bereits uebersetzt;
        /// <c>null</c> bei <see cref="KatalogFilterArt.Keiner"/>.
        /// </summary>
        public string FilterEinsBezeichnung { get; private set; }

        /// <summary>Beschriftung der zweiten Filterklappliste.</summary>
        public string FilterZweiBezeichnung { get; private set; }

        /// <summary>
        /// Die Bauteile des zweispaltigen Zeilentexts, bereits uebersetzt und in der
        /// Reihenfolge des Vorlaeufers; leer, wenn die Liste einspaltig ist.
        /// </summary>
        /// <remarks>
        /// BHKW <c>:196-200</c>: Firma, „Brennstoff: ", „Ptherm: " + „ kW", „Pel: " + „ kW".
        /// Solarkollektoren <c>:96</c>: Firma, „Kollektortyp: ", „Aperturfläche: " + „ m²".
        /// Die drei bzw. zwei Beschriftungen standen als deutsche Literale IM DATENSTROM
        /// (Befunde W14-B11 und W14-B17) und sind mit W14a.0g Ressourcen geworden.
        /// </remarks>
        public IReadOnlyList<string> Zeilenbauplan { get; private set; }

        /// <summary>Spaltenkopf der ersten Rasterspalte, bereits uebersetzt.</summary>
        public string SpalteName { get; private set; }

        /// <summary>Spaltenkopf der zweiten Rasterspalte, bereits uebersetzt.</summary>
        public string SpalteEigenschaften { get; private set; }

        // ==================================================================
        // Die Schluessel der Detailfelder
        // ==================================================================

        /// <summary>Der Bezeichner — in allen vier der Listenschluessel, nie editierbar.</summary>
        public const string FeldBezeichner = "BEZEICHNER";

        public const string FeldBeschreibung = "BESCHREIBUNG";
        public const string FeldFirma = "FIRMA";
        public const string FeldBrennstoff = "BRENNSTOFF";
        public const string FeldPtherm = "PTHERM";
        public const string FeldPel = "PEL";
        public const string FeldGrenzleistung = "GRENZLEISTUNG";
        public const string FeldInvestitionskosten = "INVESTITIONSKOSTEN";
        public const string FeldBrennwert = "BRENNWERT";
        public const string FeldVorlauf = "VORLAUF";
        public const string FeldRuecklauf = "RUECKLAUF";
        public const string FeldKollektortyp = "KOLLEKTORTYP";
        public const string FeldModulflaeche = "MODULFLAECHE";
        public const string FeldAperturflaeche = "APERTURFLAECHE";
        public const string FeldSpeichertyp = "SPEICHERTYP";
        public const string FeldVerluste = "VERLUSTE";
        public const string FeldVolumen = "VOLUMEN";

        // ==================================================================
        // Die vier Auspraegungen
        // ==================================================================

        /// <summary>
        /// Die Auspraegung zu einer Browserart. <paramref name="text"/> uebersetzt einen
        /// Beschriftungsschluessel; <c>null</c> liefert den Schluessel selbst zurueck
        /// (fuer Tests und fuer eine Umgebung ohne Katalog).
        /// </summary>
        public static KatalogBrowserProfil Finde(KatalogBrowserArt art, Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            switch (art)
            {
                case KatalogBrowserArt.Heizkessel:
                    return new KatalogBrowserProfil
                    {
                        Art = art,
                        Stammtabelle = HeizkesselStammCtrl.TABLE,
                        Zweispaltig = false,
                        Filterart = KatalogFilterArt.BrennstoffUndLeistung,
                        HatSpeicherweg = true,
                        ZeigtSchreibschutz = false,
                        HilfeSchluessel = "Form_Heizkessel_Admin.btn_Help",
                        Titel = t("KBROW_TITEL_HEIZKESSEL"),
                        Listenbeschriftung = t("KBROW_LISTE_HEIZKESSEL"),
                        Detailueberschrift = t("KBROW_GRUPPE_HEIZKESSEL"),
                        MeldungOhneAuswahl = "",
                        FilterEinsBezeichnung = t("KBROW_FILTER_BRENNSTOFF"),
                        FilterZweiBezeichnung = t("KBROW_FILTER_LEISTUNG"),
                        Zeilenbauplan = new string[0],
                        SpalteName = t("KBROW_SPALTE_NAME"),
                        SpalteEigenschaften = "",
                        Detailfelder = new[]
                        {
                            new BrowserDetailfeld(FeldBezeichner,   t("KBROW_LBL_NAME")),
                            new BrowserDetailfeld(FeldBeschreibung, t("KBROW_LBL_BESCHREIBUNG"), "",
                                                  BrowserFeldArt.Mehrzeilig, editierbar: true),
                            new BrowserDetailfeld(FeldBrennstoff,   t("KBROW_LBL_BRENNSTOFF")),
                            new BrowserDetailfeld(FeldPtherm,       t("KBROW_LBL_LEISTUNG"), "kW",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldInvestitionskosten, t("KBROW_LBL_INVEST"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldBrennwert,    t("KBROW_LBL_BRENNWERT"), "",
                                                  BrowserFeldArt.Schalter, editierbar: true),
                            new BrowserDetailfeld(FeldVorlauf,      t("KBROW_LBL_VORLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldRuecklauf,    t("KBROW_LBL_RUECKLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true)
                        }
                    };

                case KatalogBrowserArt.Bhkw:
                    return new KatalogBrowserProfil
                    {
                        Art = art,
                        Stammtabelle = BHKWStammCtrl.TABLE,
                        Zweispaltig = true,
                        Filterart = KatalogFilterArt.BrennstoffUndLeistung,
                        HatSpeicherweg = true,
                        ZeigtSchreibschutz = true,
                        HilfeSchluessel = "Form_BHKWAdmin.btn_Help",
                        Titel = t("KBROW_TITEL_BHKW"),
                        Listenbeschriftung = t("KBROW_LISTE_BHKW"),
                        Detailueberschrift = t("KBROW_GRUPPE_BHKW"),
                        MeldungOhneAuswahl = t("KBROW_MSG_AUSWAHL_BHKW"),
                        FilterEinsBezeichnung = t("KBROW_FILTER_BRENNSTOFF"),
                        FilterZweiBezeichnung = t("KBROW_FILTER_LEISTUNG"),
                        Zeilenbauplan = new[]
                        {
                            t("KBROW_ZEILE_BRENNSTOFF"), t("KBROW_ZEILE_PTHERM"), t("KBROW_ZEILE_PEL")
                        },
                        SpalteName = t("KBROW_SPALTE_NAME"),
                        SpalteEigenschaften = t("KBROW_SPALTE_EIGENSCHAFTEN"),
                        Detailfelder = new[]
                        {
                            new BrowserDetailfeld(FeldBezeichner,   t("KBROW_LBL_MODULNAME")),
                            new BrowserDetailfeld(FeldFirma,        t("KBROW_LBL_HERSTELLER"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldBeschreibung, t("KBROW_LBL_BESCHREIBUNG"), "",
                                                  BrowserFeldArt.Mehrzeilig),
                            new BrowserDetailfeld(FeldPtherm,       t("KBROW_LBL_PTHERM"), "kWth",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldPel,          t("KBROW_LBL_PEL"), "kWel",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldGrenzleistung, t("KBROW_LBL_GRENZLEISTUNG"), "%",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldVorlauf,      t("KBROW_LBL_VORLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldRuecklauf,    t("KBROW_LBL_RUECKLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true)
                        }
                    };

                case KatalogBrowserArt.Solarkollektoren:
                    return new KatalogBrowserProfil
                    {
                        Art = art,
                        Stammtabelle = SolarkollektorenStammCtrl.TABLE,
                        Zweispaltig = true,
                        Filterart = KatalogFilterArt.Keiner,
                        HatSpeicherweg = false,
                        ZeigtSchreibschutz = false,
                        HilfeSchluessel = "Form_SolarKollektorenAdmin.btn_Help",
                        Titel = t("KBROW_TITEL_SOLAR"),
                        Listenbeschriftung = t("KBROW_LISTE_SOLAR"),
                        Detailueberschrift = t("KBROW_GRUPPE_SOLAR"),
                        MeldungOhneAuswahl = t("KBROW_MSG_AUSWAHL_KOLLEKTOR"),
                        FilterEinsBezeichnung = "",
                        FilterZweiBezeichnung = "",
                        Zeilenbauplan = new[]
                        {
                            t("KBROW_ZEILE_KOLLEKTORTYP"), t("KBROW_ZEILE_APERTUR")
                        },
                        SpalteName = t("KBROW_SPALTE_NAME"),
                        SpalteEigenschaften = t("KBROW_SPALTE_EIGENSCHAFTEN"),
                        Detailfelder = new[]
                        {
                            new BrowserDetailfeld(FeldBezeichner,    t("KBROW_LBL_NAME")),
                            new BrowserDetailfeld(FeldKollektortyp,  t("KBROW_LBL_KOLLEKTOR")),
                            new BrowserDetailfeld(FeldFirma,         t("KBROW_LBL_HERSTELLER")),
                            new BrowserDetailfeld(FeldBeschreibung,  t("KBROW_LBL_BESCHREIBUNG"), "",
                                                  BrowserFeldArt.Mehrzeilig),

                            // Befund W14a-B78: textBox_Kollektor_A („Kollektorfläche") wird im
                            // Bestand NIE gefuellt - die Modulflaeche wird gelesen und sofort
                            // von der Aperturflaeche ueberschrieben (W14-B15). Das Feld bleibt
                            // deshalb woertlich leer; Entscheid E-11.
                            new BrowserDetailfeld(FeldModulflaeche,   t("KBROW_LBL_KOLLEKTORFLAECHE"), "m²"),
                            new BrowserDetailfeld(FeldAperturflaeche, t("KBROW_LBL_APERTURFLAECHE"), "m²"),
                            new BrowserDetailfeld(FeldVorlauf,        t("KBROW_LBL_VORLAUF"), "°C"),
                            new BrowserDetailfeld(FeldRuecklauf,      t("KBROW_LBL_RUECKLAUF"), "°C")
                        }
                    };

                case KatalogBrowserArt.Pufferspeicher:
                    return new KatalogBrowserProfil
                    {
                        Art = art,
                        Stammtabelle = PufferSpStammCtrl.TABLE,
                        Zweispaltig = false,
                        Filterart = KatalogFilterArt.HerstellerUndVolumen,
                        HatSpeicherweg = false,
                        ZeigtSchreibschutz = false,
                        HilfeSchluessel = "Form_PufferSp_Admin.btn_Help",
                        Titel = t("KBROW_TITEL_PUFFERSP"),
                        Listenbeschriftung = t("KBROW_LISTE_PUFFERSP"),
                        Detailueberschrift = t("KBROW_GRUPPE_PUFFERSP"),
                        MeldungOhneAuswahl = "",
                        FilterEinsBezeichnung = t("KBROW_FILTER_HERSTELLER"),
                        FilterZweiBezeichnung = t("KBROW_FILTER_VOLUMEN"),
                        Zeilenbauplan = new string[0],
                        SpalteName = t("KBROW_SPALTE_NAME"),
                        SpalteEigenschaften = "",
                        Detailfelder = new[]
                        {
                            new BrowserDetailfeld(FeldBezeichner, t("KBROW_LBL_NAME")),
                            new BrowserDetailfeld(FeldFirma,      t("KBROW_LBL_HERSTELLER")),
                            new BrowserDetailfeld(FeldSpeichertyp, t("KBROW_LBL_SPEICHERTYP")),
                            new BrowserDetailfeld(FeldVerluste,   t("KBROW_LBL_VERLUSTE"), "kWh/d"),
                            new BrowserDetailfeld(FeldVolumen,    t("KBROW_LBL_VOLUMEN"), "l"),
                            new BrowserDetailfeld(FeldInvestitionskosten, t("KBROW_LBL_INVEST"), "€")
                        }
                    };
            }

            throw new ArgumentOutOfRangeException(nameof(art));
        }

        /// <summary>Alle vier Auspraegungen — fuer Stapelpruefungen.</summary>
        public static IEnumerable<KatalogBrowserArt> AlleArten
        {
            get
            {
                yield return KatalogBrowserArt.Heizkessel;
                yield return KatalogBrowserArt.Bhkw;
                yield return KatalogBrowserArt.Solarkollektoren;
                yield return KatalogBrowserArt.Pufferspeicher;
            }
        }
    }
}
