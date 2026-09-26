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
        Schalter,

        /// <summary>
        /// <c>Auswahlfeld</c> ueber die <c>Optionen</c> des Feldes (Wert = Datenbankcode,
        /// Text = Beschriftung); der Wert des Feldes ist der CODE. Erster Einsatz: die
        /// Zelltechnologie der PV-Module (Paket B, Merge 5).
        /// </summary>
        Auswahl
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
    /// <c>true</c> schreibt <c>…StammCtrl.AnzeigefelderSchreiben</c> zurueck. Seit dem
    /// Anwenderentscheid vom 15.09.2026 ist das JEDE fachliche Spalte ausser dem
    /// Bezeichner — er ist der Schluessel des <c>UPDATE</c> und wird im Aufklapper nicht
    /// umbenannt. Eine begruendete Ausnahme: Die Investition je kWel des BHKW ist
    /// abgeleitet (<c>BHKWKosten.JeKWel</c> aus den fuenf Posten, W14a-E-8-B3) und
    /// bleibt Lesewert.</para>
    /// </remarks>
    public sealed class BrowserDetailfeld
    {
        public BrowserDetailfeld(string schluessel, string bezeichnung, string einheit = "",
                                 BrowserFeldArt art = BrowserFeldArt.Text, bool editierbar = false,
                                 string hinweis = "")
        {
            Schluessel = schluessel;
            Bezeichnung = bezeichnung;
            Einheit = einheit ?? "";
            Art = art;
            Editierbar = editierbar;
            Hinweis = hinweis ?? "";
        }

        /// <summary>
        /// ETAPPE E10 (Kennzeichnung A8): ein Vermerk zum Feld, bereits uebersetzt — die
        /// Oberflaeche zeigt ihn als Tooltip der Beschriftung; leer = keiner. Er traegt,
        /// was die Beschriftung nur andeuten kann (etwa „Geraetedaten — nicht
        /// rechenwirksam; massgeblich ist die Nutzungsdauertabelle").
        /// </summary>
        public string Hinweis { get; }

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
    /// zweispaltige Liste samt Textbauplan, Spaltenprofil, Detailfeldliste, Editorschluessel,
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

        /// <summary>
        /// <b>Die Spalten der Liste</b> (Anwenderentscheid <b>W14a-E-10</b> vom
        /// 07.09.2026) — <see cref="Katalogfilterprofil"/> zur passenden
        /// <see cref="Anlagenart"/>.
        /// </summary>
        /// <remarks>
        /// <para><b>Es ersetzt die zwei Klapplisten.</b> Bis hierher fuehrte das Profil
        /// eine <c>Filterart</c> (keine / Brennstoff+Leistung / Hersteller+Volumen) und
        /// zwei Beschriftungen („Filtern nach Brennstoffart:", „Filtern nach
        /// Leistung:"); die Liste zeigte dazu EINE Spalte. Seit W14a-E-10 sitzt der
        /// Filter an der SPALTE, und was gefiltert werden kann, sagt genau diese
        /// Spaltenliste — „filterbar ist nur, was als Spalte dasteht" (Konzept 5.6).</para>
        /// <para>Die sechs festen Leistungs- bzw. Volumenstufen entfallen damit als
        /// Bedienung (Frage Q5): Sortieren nach P_th und der Ausdruck <c>10..60</c>
        /// leisten dasselbe genauer. <c>HeizkesselStammCtrl.LEISTUNG_SQL</c> und
        /// <c>PufferSpStammCtrl.VOLUMEN_SQL</c> bleiben stehen — die PROJEKTdialoge
        /// laufen erst mit Stufe S2 auf das Spaltenmodell.</para>
        /// </remarks>
        public Katalogfilterprofil Filterprofil { get; private set; }

        /// <summary>
        /// Die Detailfelder in Anzeigereihenfolge — seit dem Anwenderentscheid vom
        /// 15.09.2026 der VOLLE Feldbestand des Katalogsatzes (21 / 25 / 14 / 6):
        /// jede fachliche Spalte, ohne <c>ID</c> und <c>ReadOnly</c>.
        /// </summary>
        /// <remarks>
        /// Die Felder der ersten Fassung (8 / 8 / 8 / 6, der Detailblock der vier
        /// Vorlaeufer-Masken) stehen unveraendert an ihrem Platz; der volle Satz
        /// haengt hinten an, gruppiert Technik → Kosten → Emissionen.
        /// </remarks>
        public IReadOnlyList<BrowserDetailfeld> Detailfelder { get; private set; }

        /// <summary>
        /// Traegt der KATALOGBROWSER seine eigene Speicherleiste? Seit dem 15.09.2026 alle
        /// vier Auspraegungen (der Speicherweg vom 18.08.2026, im Vorlaeufer die
        /// <c>SpeichernLeiste</c>) — Solarkollektoren und Pufferspeicher kamen zuletzt dazu.
        /// </summary>
        /// <remarks>
        /// <b>Nicht zu verwechseln mit <see cref="BrowserDetailfeld.Editierbar"/>.</b>
        /// Seit dem 15.09.2026 hat JEDER der vier Kataloge im Kern einen Schreibweg
        /// (<c>…StammCtrl.AnzeigefelderSchreiben</c>) — den benutzt der Aufklapper „Alle
        /// Daten anzeigen" des Projektdialogs. Diese Kennzeichnung sagt nur, ob die
        /// Maske des Browsers ihren Knopf „Speichern" zeigt; sie folgt der Belegung von
        /// <c>KatalogBrowserWege.Speichern</c> in der jeweiligen Huelle und wird mit ihr
        /// zusammen umgestellt.
        /// </remarks>
        public bool HatSpeicherweg { get; private set; }

        /// <summary>
        /// Der Schluessel des Infoknopfs — die ZEILE LINKS in <c>help_mapping.txt</c>
        /// (<c>Form_X.btn_Help</c>), nicht das Ziel rechts. Die Zuordnung bleibt damit
        /// unveraendert, obwohl die Maske dahinter nicht mehr existiert; dasselbe
        /// Vorgehen wie bei den vier Einlesemasken der Welle 13.
        /// </summary>
        public string HilfeSchluessel { get; private set; }

        /// <summary>
        /// Der Schluessel des ZWEITEN Infoknopfs — der Weg in die Wikirubrik
        /// <c>Programm Dokumentation/Berechnung</c> (H13, Anwenderentscheid
        /// O-H13b-5 vom 07.09.2026).
        /// </summary>
        /// <remarks>
        /// <para>Er steht NEBEN <see cref="HilfeSchluessel"/> und ersetzt ihn nicht:
        /// Der Fensterknopf oben rechts bleibt die Bedienhilfe des Katalogs, der
        /// zweite Knopf sitzt am Kopf des Detailblocks — also dort, wo die Werte
        /// stehen, die der Rechenweg spaeter liest.</para>
        /// <para>Er ist SPRACHNEUTRAL wie <see cref="Stammtabelle"/> und
        /// <see cref="HilfeSchluessel"/>; die Anzeigetexte kommen weiterhin ueber
        /// den Uebersetzer von <see cref="Finde"/>.</para>
        /// </remarks>
        public string BerechnungsSchluessel { get; private set; }

        /// <summary>
        /// Die Seite der Rubrik, auf die der Berechnungsknopf fuehrt — ohne Rubrik
        /// und ohne Anker (z. B. <c>Heizkessel</c>, <c>Solarthermie</c>).
        /// </summary>
        public string BerechnungsSeite { get; private set; }

        /// <summary>
        /// Der Kurztext des Berechnungsknopfs, falls der Hilfekatalog den Schluessel
        /// (noch) nicht kennt — WORTGLEICH zum Tooltip des mitgelieferten
        /// Startbestandes, damit beide Wege denselben Namen zeigen.
        /// </summary>
        public string BerechnungsKurztext =>
            BerechnungsHilfe.RUBRIK_KURZ + ": " + (BerechnungsSeite ?? "");

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

        // -----------------------------------------------------------------
        // Der VOLLE Feldbestand (Anwenderentscheid 15.09.2026)
        //
        // Bis hierher fuehrte das Profil je Katalog nur die Felder, die der
        // Vorlaeufer-Detailblock zeigte (8 / 8 / 8 / 6). Seit dem Entscheid
        // zeigt und BEARBEITET der Aufklapper „Alle Daten anzeigen" JEDE
        // fachliche Spalte des Katalogsatzes - sonst entstuende genau die
        // Pflegeluecke, die der Wegfall der Kosten- und Emissionsgruppen im
        // Bearbeiten-Dialog hinterlaesst. Nicht fachlich und deshalb nicht
        // hier: ID (Schluessel der Tabelle) und ReadOnly (Kennzeichen der
        // Auslieferung, kein Geraetewert).
        // -----------------------------------------------------------------

        public const string FeldWirkungsgradGas = "WIRKUNGSGRAD_GAS";
        public const string FeldWirkungsgradOel = "WIRKUNGSGRAD_OEL";
        public const string FeldBBVerlust = "BBVERLUST";
        public const string FeldRaumbedarf = "RAUMBEDARF";
        public const string FeldWartungskosten = "WARTUNGSKOSTEN";
        public const string FeldWartungEinheit = "WARTUNG_EINHEIT";
        public const string FeldNutzungsdauer = "NUTZUNGSDAUER";
        public const string FeldCo2 = "CO2";
        public const string FeldSo2 = "SO2";
        public const string FeldNox = "NOX";
        public const string FeldCo = "CO";
        public const string FeldStaub = "STAUB";

        public const string FeldWirkungsgrad = "WIRKUNGSGRAD";
        public const string FeldWirkungsgradEl = "WIRKUNGSGRAD_EL";
        public const string FeldWirkungsgradTh = "WIRKUNGSGRAD_TH";
        public const string FeldMotortyp = "MOTORTYP";
        public const string FeldInvestitionJeKwel = "INVESTITION_KWEL";
        public const string FeldKostenModul = "KOSTEN_MODUL";
        public const string FeldKostenMontage = "KOSTEN_MONTAGE";
        public const string FeldKostenLieferung = "KOSTEN_LIEFERUNG";
        public const string FeldKostenSchallschutz = "KOSTEN_SCHALLSCHUTZ";
        public const string FeldKostenAbgasreinigung = "KOSTEN_ABGASREINIGUNG";
        public const string FeldWartungJeKwhel = "WARTUNG_KWHEL";

        public const string FeldH0 = "H0";
        public const string FeldK1 = "K1";
        public const string FeldK2 = "K2";
        public const string FeldKdir = "KDIR";
        public const string FeldKdiff = "KDIFF";

        // -----------------------------------------------------------------
        // Die Gruppe KOSTEN des Stammblatts (Konzept Administrationsdialoge,
        // Stufe 3, Vorschlag V9): Das Stammblatt einer Verwaltung teilt die
        // Felder eines Satzes in "Kenndaten" und "Kosten". Welches Feld ein
        // Kostenfeld ist, ist Fachwissen und steht deshalb hier, neben den
        // Schluesseln - nicht in der Oberflaeche.
        // -----------------------------------------------------------------

        private static readonly HashSet<string> KOSTENFELDER = new HashSet<string>(StringComparer.Ordinal)
        {
            FeldInvestitionskosten, FeldWartungskosten, FeldWartungEinheit, FeldNutzungsdauer,
            FeldInvestitionJeKwel, FeldKostenModul, FeldKostenMontage, FeldKostenLieferung,
            FeldKostenSchallschutz, FeldKostenAbgasreinigung, FeldWartungJeKwhel
        };

        /// <summary>
        /// <b>Gehoert das Detailfeld in die Gruppe „Kosten" des Stammblatts?</b>
        /// (Stufe 3, V9) — Investition, Wartung und Nutzungsdauer, beim BHKW dazu die
        /// fuenf Kostenposten und die zwei spezifischen Kosten. Alle uebrigen Felder
        /// stehen unter „Kenndaten".
        /// </summary>
        public static bool IstKostenfeld(string schluessel)
            => schluessel != null && KOSTENFELDER.Contains(schluessel);

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
                        Filterprofil = Katalogfilterprofil.Finde(Anlagenart.Heizkessel, t),
                        HatSpeicherweg = true,
                        HilfeSchluessel = "Form_Heizkessel_Admin.btn_Help",
                        BerechnungsSchluessel = "Form_Heizkessel_Admin.Berechnung",
                        BerechnungsSeite = "Heizkessel",
                        Titel = t("KBROW_TITEL_HEIZKESSEL"),
                        Listenbeschriftung = t("KBROW_LISTE_HEIZKESSEL"),
                        Detailueberschrift = t("KBROW_GRUPPE_HEIZKESSEL"),
                        MeldungOhneAuswahl = "",
                        Zeilenbauplan = new string[0],
                        SpalteName = t("KBROW_SPALTE_NAME"),
                        SpalteEigenschaften = "",
                        Detailfelder = new[]
                        {
                            new BrowserDetailfeld(FeldBezeichner,   t("KBROW_LBL_NAME")),
                            new BrowserDetailfeld(FeldBeschreibung, t("KBROW_LBL_BESCHREIBUNG"), "",
                                                  BrowserFeldArt.Mehrzeilig, editierbar: true),
                            new BrowserDetailfeld(FeldBrennstoff,   t("KBROW_LBL_BRENNSTOFF"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldPtherm,       t("KBROW_LBL_LEISTUNG"), "kW",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldInvestitionskosten, t("KBROW_LBL_INVEST"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldBrennwert,    t("KBROW_LBL_BRENNWERT"), "",
                                                  BrowserFeldArt.Schalter, editierbar: true),
                            new BrowserDetailfeld(FeldVorlauf,      t("KBROW_LBL_VORLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldRuecklauf,    t("KBROW_LBL_RUECKLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),

                            // Der volle Satz (15.09.2026): Technik, Kosten, Emissionen.
                            new BrowserDetailfeld(FeldFirma,        t("HZKK_LBL_HERSTELLER"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldWirkungsgradGas, t("HZKK_LBL_WG_GAS"), "",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldWirkungsgradOel, t("HZKK_LBL_WG_OEL"), "",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldBBVerlust,    t("HZKK_LBL_BBVERLUST"), "%",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldRaumbedarf,   t("HZKK_LBL_RAUMBEDARF"), "m³",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldWartungskosten, t("KESSEL_WARTUNG_LBL") + ":", "",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldWartungEinheit, t("KESSEL_WARTUNG_EINHEIT_LBL") + ":", "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            // ETAPPE E10 (Kennzeichnung A8, Empfehlung E10-Q4 a): Geraetedaten,
                            // nicht rechenwirksam - der Rechenweg nimmt die Nutzungsdauer-
                            // tabelle. Pflegbar bleibt die Spalte; Beschriftung und Vermerk
                            // sagen, dass sie nicht rechnet.
                            new BrowserDetailfeld(FeldNutzungsdauer, t("HZKK_LBL_NUTZUNGSDAUER"),
                                                  t("HZKK_EINHEIT_JAHRE"),
                                                  BrowserFeldArt.Zahl, editierbar: true,
                                                  hinweis: t("KBROW_ND_GERAETEDATEN_HINWEIS")),

                            // Emissionen: Herstellerangabe des Katalogsatzes. Der Lauf
                            // rechnet sie NICHT (W14a-E-8-B1) - er nimmt den
                            // Emissionskatalog des Energietraegers -, gepflegt werden
                            // sie trotzdem hier, denn sonst nirgends mehr.
                            new BrowserDetailfeld(FeldCo2,   "CO2:",  "g / MWh",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldSo2,   "SO2:",  "g / MWh",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldNox,   "NOx:",  "g / MWh",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldCo,    "CO:",   "g / MWh",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldStaub, t("HZKK_LBL_STAUB"), "g / MWh",
                                                  BrowserFeldArt.Zahl, editierbar: true)
                        }
                    };

                case KatalogBrowserArt.Bhkw:
                    return new KatalogBrowserProfil
                    {
                        Art = art,
                        Stammtabelle = BHKWStammCtrl.TABLE,
                        Zweispaltig = true,
                        Filterprofil = Katalogfilterprofil.Finde(Anlagenart.Bhkw, t),
                        HatSpeicherweg = true,
                        HilfeSchluessel = "Form_BHKWAdmin.btn_Help",
                        BerechnungsSchluessel = "Form_BHKWAdmin.Berechnung",
                        BerechnungsSeite = "BHKW",
                        Titel = t("KBROW_TITEL_BHKW"),
                        Listenbeschriftung = t("KBROW_LISTE_BHKW"),
                        Detailueberschrift = t("KBROW_GRUPPE_BHKW"),
                        MeldungOhneAuswahl = t("KBROW_MSG_AUSWAHL_BHKW"),
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
                                                  BrowserFeldArt.Mehrzeilig, editierbar: true),
                            new BrowserDetailfeld(FeldPtherm,       t("KBROW_LBL_PTHERM"), "kWth",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldPel,          t("KBROW_LBL_PEL"), "kWel",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldGrenzleistung, t("KBROW_LBL_GRENZLEISTUNG"), "%",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldVorlauf,      t("KBROW_LBL_VORLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldRuecklauf,    t("KBROW_LBL_RUECKLAUF"), "°C",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),

                            // Der volle Satz (15.09.2026): Technik, Kosten, Emissionen.
                            new BrowserDetailfeld(FeldBrennstoff,   t("BHKWK_LBL_ENERGIETRAEGER"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            // DIE ZWEI ANTEILE SIND DIE EINGABE, DIE SUMME IST ANZEIGE
                            // (Anwenderentscheid 20.09.2026). Bis hierher nahm der
                            // Aufklapper den GESAMTwert entgegen und verteilte ihn im
                            // Verhaeltnis der Leistungen auf die zwei Anteile - eine
                            // dritte Zahl, die die zwei gepflegten ueberschrieb. Jetzt
                            // steht hier dasselbe wie im Katalogeditor: gepflegt werden
                            // el und th, der Gesamtwirkungsgrad laeuft als ihre Summe
                            // mit (BhkwWirkungsgrad.Gesamt, Schemaschritt 99) und ist
                            // deshalb NICHT editierbar - wie die Investition je kWel.
                            new BrowserDetailfeld(FeldWirkungsgradEl,
                                                  t("BHKWK_LBL_WIRKUNGSGRAD_EL"),
                                                  t("BHKWK_HINT_WIRKUNGSGRAD_EL"),
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldWirkungsgradTh,
                                                  t("BHKWK_LBL_WIRKUNGSGRAD_TH"),
                                                  t("BHKWK_HINT_WIRKUNGSGRAD_TH"),
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldWirkungsgrad, t("BHKWK_LBL_WIRKUNGSGRAD"), "",
                                                  BrowserFeldArt.Zahl),
                            new BrowserDetailfeld(FeldMotortyp,     t("BHKWK_LBL_MOTORTYP"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldRaumbedarf,   t("BHKWK_LBL_RAUMBEDARF"), "m³",
                                                  BrowserFeldArt.Zahl, editierbar: true),

                            // Die fuenf Kostenposten sind das, was gespeichert wird und
                            // was die Kostenplanung liest (TechnikPlanwertCtrl:317-325).
                            new BrowserDetailfeld(FeldKostenModul,  t("BHKWK_LBL_MODUL"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldKostenMontage, t("BHKWK_LBL_MONTAGE"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldKostenLieferung, t("BHKWK_LBL_LIEFERUNG"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldKostenSchallschutz, t("BHKWK_LBL_SCHALLSCHUTZ"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldKostenAbgasreinigung, t("BHKWK_LBL_ABGASREINIGUNG"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true),

                            // ABGELEITET und deshalb nicht editierbar (W14a-E-8-B3):
                            // BHKWKosten.JeKWel aus den fuenf Posten. Zwei Eingabewege
                            // fuer dasselbe Geld liessen sich widersprechen; die Posten
                            // sind die Wahrheit, diese Zeile rechnet sie nur um.
                            new BrowserDetailfeld(FeldInvestitionJeKwel, t("BHKWK_LBL_INVEST"), "€ / kWel",
                                                  BrowserFeldArt.Zahl),
                            new BrowserDetailfeld(FeldWartungJeKwhel, t("BHKWK_LBL_WARTUNG"), "€ / kWhel",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            // ETAPPE E10 (Kennzeichnung A8): wie beim Heizkessel.
                            new BrowserDetailfeld(FeldNutzungsdauer, t("BHKWK_LBL_NUTZUNGSDAUER"),
                                                  t("HZKK_EINHEIT_JAHRE"),
                                                  BrowserFeldArt.Ganzzahl, editierbar: true,
                                                  hinweis: t("KBROW_ND_GERAETEDATEN_HINWEIS")),

                            // Emissionen: Herstellerangabe, seit W14a-E-8-B1 „nur
                            // Anzeige" im Rechenweg - gepflegt werden sie hier. Die
                            // fuenf Spalten sind INTEGER, deshalb Ganzzahlfelder.
                            new BrowserDetailfeld(FeldNox,   "NOx:",  "g / MWh",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldSo2,   "SO2:",  "g / MWh",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldCo,    "CO:",   "g / MWh",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldCo2,   "CO2:",  "g / MWh",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldStaub, t("HZKK_LBL_STAUB"), "g / MWh",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true)
                        }
                    };

                case KatalogBrowserArt.Solarkollektoren:
                    return new KatalogBrowserProfil
                    {
                        Art = art,
                        Stammtabelle = SolarkollektorenStammCtrl.TABLE,
                        Zweispaltig = true,
                        Filterprofil = Katalogfilterprofil.Finde(Anlagenart.Solarkollektoren, t),
                        HatSpeicherweg = true,
                        HilfeSchluessel = "Form_SolarKollektorenAdmin.btn_Help",
                        BerechnungsSchluessel = "Form_SolarKollektorenAdmin.Berechnung",
                        BerechnungsSeite = "Solarthermie",
                        Titel = t("KBROW_TITEL_SOLAR"),
                        Listenbeschriftung = t("KBROW_LISTE_SOLAR"),
                        Detailueberschrift = t("KBROW_GRUPPE_SOLAR"),
                        MeldungOhneAuswahl = t("KBROW_MSG_AUSWAHL_KOLLEKTOR"),
                        Zeilenbauplan = new[]
                        {
                            t("KBROW_ZEILE_KOLLEKTORTYP"), t("KBROW_ZEILE_APERTUR")
                        },
                        SpalteName = t("KBROW_SPALTE_NAME"),
                        SpalteEigenschaften = t("KBROW_SPALTE_EIGENSCHAFTEN"),
                        Detailfelder = new[]
                        {
                            new BrowserDetailfeld(FeldBezeichner,    t("KBROW_LBL_NAME")),
                            new BrowserDetailfeld(FeldKollektortyp,  t("KBROW_LBL_KOLLEKTOR"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldFirma,         t("KBROW_LBL_HERSTELLER"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldBeschreibung,  t("KBROW_LBL_BESCHREIBUNG"), "",
                                                  BrowserFeldArt.Mehrzeilig, editierbar: true),

                            // Befund W14a-B78 / Entscheid E-11 ABGELOEST am 15.09.2026:
                            // Der Vorlaeufer liess „Kollektorfläche" leer (die Modulflaeche
                            // wurde gelesen und sofort von der Aperturflaeche ueberschrieben,
                            // W14-B15), und das war richtig, solange der Block NUR ANZEIGTE.
                            // Ein leeres Feld mit Speicherweg wuerde die gespeicherte
                            // Modulflaeche beim ersten Speichern auf 0 setzen - deshalb
                            // zeigt der Block sie jetzt und schreibt sie zurueck.
                            new BrowserDetailfeld(FeldModulflaeche,   t("KBROW_LBL_KOLLEKTORFLAECHE"), "m²",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldAperturflaeche, t("KBROW_LBL_APERTURFLAECHE"), "m²",
                                                  BrowserFeldArt.Zahl, editierbar: true),

                            // Der volle Satz (15.09.2026): die Kennlinie und der Preis.
                            // Kdfu heisst im Editor „Kdiff" und hat im Rechenweg keinen
                            // Leser (ParameterVerwendung); gepflegt wird er trotzdem.
                            new BrowserDetailfeld(FeldH0,   "h0:",   "",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldK1,   "k1:",   "W/(m²*K)",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldK2,   "k2:",   "W/(m²*K²)",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldKdir, "Kdir:", "",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldKdiff, "Kdiff:", "50°",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldInvestitionskosten, t("KBROW_LBL_INVEST"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true)
                        }
                    };

                case KatalogBrowserArt.Pufferspeicher:
                    return new KatalogBrowserProfil
                    {
                        Art = art,
                        Stammtabelle = PufferSpStammCtrl.TABLE,
                        Zweispaltig = false,
                        Filterprofil = Katalogfilterprofil.Finde(Anlagenart.Pufferspeicher, t),
                        HatSpeicherweg = true,
                        HilfeSchluessel = "Form_PufferSp_Admin.btn_Help",
                        BerechnungsSchluessel = "Form_PufferSp_Admin.Berechnung",
                        BerechnungsSeite = "Pufferspeicher",
                        Titel = t("KBROW_TITEL_PUFFERSP"),
                        Listenbeschriftung = t("KBROW_LISTE_PUFFERSP"),
                        Detailueberschrift = t("KBROW_GRUPPE_PUFFERSP"),
                        MeldungOhneAuswahl = "",
                        Zeilenbauplan = new string[0],
                        SpalteName = t("KBROW_SPALTE_NAME"),
                        SpalteEigenschaften = "",
                        Detailfelder = new[]
                        {
                            // Der Katalog fuehrt nur diese sechs Geraetewerte - alles
                            // Weitere (Schichten, Schwellen, Entnahmehoehen) steht erst
                            // in der Projektkopie. Der Feldbestand bleibt deshalb am
                            // 15.09.2026 unveraendert; hinzu kommt der Speicherweg.
                            new BrowserDetailfeld(FeldBezeichner, t("KBROW_LBL_NAME")),
                            new BrowserDetailfeld(FeldFirma,      t("KBROW_LBL_HERSTELLER"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldSpeichertyp, t("KBROW_LBL_SPEICHERTYP"), "",
                                                  BrowserFeldArt.Text, editierbar: true),
                            new BrowserDetailfeld(FeldVerluste,   t("KBROW_LBL_VERLUSTE"), "kWh/d",
                                                  BrowserFeldArt.Zahl, editierbar: true),
                            new BrowserDetailfeld(FeldVolumen,    t("KBROW_LBL_VOLUMEN"), "l",
                                                  BrowserFeldArt.Ganzzahl, editierbar: true),
                            new BrowserDetailfeld(FeldInvestitionskosten, t("KBROW_LBL_INVEST"), "€",
                                                  BrowserFeldArt.Zahl, editierbar: true)
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
