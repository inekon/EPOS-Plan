using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welcher der vier VDI-3805-Katalogimporte gemeint ist (iU9-W13.0a).
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
        Waermepumpe
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
            }

            throw new ArgumentOutOfRangeException(nameof(art));
        }

        /// <summary>Alle vier Auspraegungen — fuer Stapelpruefungen.</summary>
        public static IEnumerable<KatalogImportArt> AlleArten
        {
            get
            {
                yield return KatalogImportArt.Heizkessel;
                yield return KatalogImportArt.Pufferspeicher;
                yield return KatalogImportArt.Solarkollektoren;
                yield return KatalogImportArt.Waermepumpe;
            }
        }

        // ==================================================================
        // Schluessel, die alle vier teilen
        // ==================================================================

        /// <summary>Der Bezeichner — das einzige Feld, das der Anwender aendern darf.</summary>
        public const string FeldName = "NAME";

        /// <summary>Der Hersteller — in allen vier vorhanden und immer gesperrt.</summary>
        public const string FeldFirma = "FIRMA";

        /// <summary>Der Dateifilter aller vier Einlesemasken, woertlich.</summary>
        public const string VdiFilter = "(*.vdi)|*.vdi";
    }
}
