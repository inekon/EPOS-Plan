using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welcher Geräteimport gemeint ist (Anwenderentscheid <b>W6‑O‑1</b> vom
    /// 06.09.2026: „baue daher den Modulimport schon jetzt um (Modulimport und
    /// Wechselrichter Import zwei Masken)").
    ///
    /// <para><b>Zwilling zu <see cref="ModulKatalogArt"/>.</b> Der Katalog wird seit
    /// iU9‑W14a von EINER Komponente mit drei Ausprägungen gepflegt; eingelesen wurde er
    /// bis W6‑O‑1 von ZWEI Masken. Beide Seiten tragen jetzt denselben Namen und
    /// dieselbe Bauart: <c>ModulKatalog*</c> pflegt, <c>ModulImport*</c> liest ein.</para>
    /// </summary>
    public enum ModulImportArt
    {
        /// <summary><c>Tab_PV_STAMM</c> aus der CEC-Modulliste und aus PVsyst-<c>.pan</c>.</summary>
        Photovoltaik,

        /// <summary>
        /// <c>Tab_Wechselrichter_STAMM</c> aus der CEC-Wechselrichterliste und aus
        /// PVsyst-<c>.OND</c> (Konzept Wechselrichter 5.1 und 5.2).
        /// </summary>
        Wechselrichter
    }

    /// <summary>
    /// EINE Quelle, aus der eine Ausprägung lesen kann — ein Knopf in der Quellenleiste.
    /// </summary>
    /// <remarks>
    /// <b>Kein Delegat, kein Knopf.</b> Eine Quelle erscheint nur, wenn die Hülle den
    /// dazugehörigen Weg mitliefert (Netzabruf bzw. Dateiwähler) — dieselbe Hausregel,
    /// nach der der PAN-Knopf ohne Dateiwähler wegblieb.
    /// </remarks>
    public sealed class ImportQuelle
    {
        public ImportQuelle(string schluessel, string beschriftung, bool ausDemNetz,
                            bool primaer = false, string dateifilter = "",
                            string unterordner = "")
        {
            Schluessel = schluessel;
            Beschriftung = beschriftung;
            AusDemNetz = ausDemNetz;
            Primaer = primaer;
            Dateifilter = dateifilter ?? "";
            Unterordner = unterordner ?? "";
        }

        /// <summary>Sprachneutraler Schlüssel — <c>CEC</c>, <c>CEC_DATEI</c>, <c>PAN</c>, <c>OND</c>.</summary>
        public string Schluessel { get; }

        /// <summary>Knopfbeschriftung, bereits übersetzt.</summary>
        public string Beschriftung { get; }

        /// <summary>Netzabruf mit Fortschritt und Abbruch (sonst: Dateiwähler).</summary>
        public bool AusDemNetz { get; }

        /// <summary>Der hervorgehobene Knopf der Leiste.</summary>
        public bool Primaer { get; }

        /// <summary>Filter des Dateiwählers, z. B. <c>(*.ond)|*.ond</c>; leer beim Netzabruf.</summary>
        public string Dateifilter { get; }

        /// <summary>
        /// Unterordner unter dem Herstellerdatenpfad, in dem der Dateiwähler aufmacht —
        /// <c>PV</c> für die zwei CEC-Listen und die <c>.OND</c>-Dateien,
        /// <c>PAN</c> für die Modul-Datenblätter (so lag es im Bestand).
        /// </summary>
        public string Unterordner { get; }
    }

    /// <summary>Eine Spalte des Rasters.</summary>
    public sealed class ImportSpalte
    {
        public ImportSpalte(string schluessel, string titel)
        {
            Schluessel = schluessel;
            Titel = titel;
        }

        /// <summary>Sprachneutraler Schlüssel — zugleich der Zugriff auf den Zellwert.</summary>
        public string Schluessel { get; }

        /// <summary>Spaltenkopf, bereits übersetzt.</summary>
        public string Titel { get; }
    }

    /// <summary>Ein Reiter des Detailblocks.</summary>
    public sealed class ImportReiter
    {
        public ImportReiter(string schluessel, string titel, string hinweis = "")
        {
            Schluessel = schluessel;
            Titel = titel;
            Hinweis = hinweis ?? "";
        }

        public string Schluessel { get; }
        public string Titel { get; }

        /// <summary>Herleitungszeile unter dem Feldblock; leer = keine.</summary>
        public string Hinweis { get; }
    }

    /// <summary>Ein Detailfeld — nur lesbar, wie im Bestand.</summary>
    public sealed class ImportFeld
    {
        public ImportFeld(string schluessel, string bezeichnung, int reiter)
        {
            Schluessel = schluessel;
            Bezeichnung = bezeichnung;
            Reiter = reiter;
        }

        public string Schluessel { get; }
        public string Bezeichnung { get; }

        /// <summary>Der Platz in <see cref="ModulImportProfil.Reiter"/>.</summary>
        public int Reiter { get; }
    }

    /// <summary>
    /// Ein Zahlenbereichsfilter der Filterleiste („von … bis"). Eine Obergrenze von 0
    /// zählt als „keine Obergrenze" — wörtlich <c>ApplyFilter</c> des Vorläufers.
    /// </summary>
    public sealed class ImportZahlenfilter
    {
        public ImportZahlenfilter(string bezeichnungVon, string bezeichnungBis,
                                  string einheit, double min, double max,
                                  double? vorgabeVon, double? vorgabeBis,
                                  int nachkommastellen = 2)
        {
            BezeichnungVon = bezeichnungVon;
            BezeichnungBis = bezeichnungBis;
            Einheit = einheit ?? "";
            Min = min;
            Max = max;
            VorgabeVon = vorgabeVon;
            VorgabeBis = vorgabeBis;
            Nachkommastellen = nachkommastellen;
        }

        public string BezeichnungVon { get; }
        public string BezeichnungBis { get; }
        public string Einheit { get; }
        public double Min { get; }
        public double Max { get; }
        public double? VorgabeVon { get; }
        public double? VorgabeBis { get; }
        public int Nachkommastellen { get; }
    }

    /// <summary>
    /// EINE Zeile des Imports in NEUTRALER Form — Zellwerte und Detailwerte als
    /// Zeichenketten, dazu die Größen, nach denen die Filterleiste einengt.
    /// </summary>
    /// <remarks>
    /// <para><b>Das ist der Kern des einen Wirts</b> (offener Punkt W6‑O‑1, jetzt
    /// geschlossen): <c>PvModulImportDialog</c> war auf 771 Zeilen gegen
    /// <see cref="UnifiedModule"/> typisiert — Spalten, Detailfelder und Filterrechnung
    /// standen als Markup gegen konkrete Eigenschaften, und der Wechselrichterimport
    /// musste die Bauart deshalb ein zweites Mal hinschreiben. Hier ist eine Zeile
    /// DATEN, wie <c>ModulFeldwert</c> im Modulkatalog: Der Dialog kennt weder
    /// <see cref="UnifiedModule"/> noch <see cref="CecWechselrichter"/> noch
    /// <see cref="OndWechselrichter"/>.</para>
    ///
    /// <para><b>Der Satz selbst reist mit</b> (<see cref="Satz"/>): Die Hülle bekommt
    /// beim Übernehmen genau das Objekt zurück, das sie hereingereicht hat, und braucht
    /// keine zweite Zuordnung über einen Index.</para>
    /// </remarks>
    public sealed class ImportZeile
    {
        public ImportZeile(int nummer, object satz)
        {
            Nummer = nummer;
            Satz = satz;
        }

        /// <summary>Der Platz in der ANGEZEIGTEN Liste (nach dem Filtern vergeben).</summary>
        public int Nummer { get; set; }

        /// <summary>Der Satz, aus dem die Zeile entstand — <c>UnifiedModule</c> u. a.</summary>
        public object Satz { get; }

        /// <summary>Der Name, gegen den das Suchmuster läuft.</summary>
        public string Name { get; set; } = "";

        /// <summary>Der Hersteller für die erste Klappliste.</summary>
        public string Hersteller { get; set; } = "";

        /// <summary>Die Technologie für die zweite Klappliste; leer = die Ausprägung führt keine.</summary>
        public string Technologie { get; set; } = "";

        /// <summary>Die Größe des ersten Zahlenfilters (PV: Pmp; WR: AC-Nennleistung).</summary>
        public double Zahl1 { get; set; }

        /// <summary>Die Größe des zweiten Zahlenfilters (PV: Effizienz); 0, wenn es keinen gibt.</summary>
        public double Zahl2 { get; set; }

        /// <summary>Die Zellwerte je Spaltenschlüssel, fertig formatiert.</summary>
        public Dictionary<string, string> Spalten { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Die Detailwerte je Feldschlüssel, fertig formatiert.</summary>
        public Dictionary<string, string> Felder { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Der Zellwert einer Spalte; leer, wenn die Zeile sie nicht führt.</summary>
        public string Spalte(string schluessel)
        {
            return schluessel != null && Spalten.TryGetValue(schluessel, out string w) ? w : "";
        }

        /// <summary>Der Detailwert eines Feldes; leer, wenn die Zeile es nicht führt.</summary>
        public string Feld(string schluessel)
        {
            return schluessel != null && Felder.TryGetValue(schluessel, out string w) ? w : "";
        }
    }

    /// <summary>
    /// <b>Die Ausprägung eines Geräteimports</b> — alles, worin sich der Modulimport
    /// (CEC, PAN) und der Wechselrichterimport (CEC, OND) unterscheiden, als DATEN
    /// (Anwenderentscheid <b>W6‑O‑1</b> vom 06.09.2026; Konzept Wechselrichter 5.5).
    ///
    /// <para>Zwilling zu <see cref="ModulKatalogProfil"/>, dieselbe Bauart und dieselbe
    /// Begründung: Der Bauplan gibt es einmal, die Werte je Ausprägung. Gemeinsam sind
    /// damit Netzabruf mit Fortschritt und Abbruch, Zwischenspeicher, Dateiwähler,
    /// virtualisiertes Raster, Zeilenwahl, Filterleiste, Detailfeldblock, Vorprüfung,
    /// Plausibilitätsrückfrage und Konfliktdialog; verschieden sind Spaltensatz,
    /// Detailfelder, Filter, Quellen, Zieltabelle und Beschriftungen.</para>
    ///
    /// <para><b>Warum nicht eine fünfte <see cref="KatalogImportArt"/>:</b> Deren vier
    /// Ausprägungen sind VDI‑3805-Dateiimporte mit gemeinsamem Parser und gemeinsamer
    /// Einlesemaske (Konzept 5.5). CEC/PAN/OND sind etwas anderes — Netzabruf und
    /// Herstellerdateien mit je eigenem Zerleger.</para>
    /// </summary>
    public sealed class ModulImportProfil
    {
        // ==================================================================
        // Spaltenschluessel
        // ==================================================================

        public const string SpalteQuelle = "QUELLE";
        public const string SpalteName = "NAME";
        public const string SpalteHersteller = "HERSTELLER";
        public const string SpalteTechnologie = "TECHNOLOGIE";
        public const string SpaltePmp = "PMP";
        public const string SpalteEffizienz = "EFFIZIENZ";
        public const string SpalteIsc = "ISC";
        public const string SpalteBifazial = "BIFAZIAL";
        public const string SpalteVoc = "VOC";
        public const string SpalteJahr = "JAHR";

        public const string SpaltePAc = "P_AC";
        public const string SpalteEtaEuro = "ETA_EURO";
        public const string SpalteMpp = "MPP";
        public const string SpalteUDcMax = "U_DC_MAX";

        // ==================================================================
        // Feldschluessel — Photovoltaik
        // ==================================================================

        public const string FeldName = "NAME";
        public const string FeldHersteller = "HERSTELLER";
        public const string FeldTechnologie = "TECHNOLOGIE";
        public const string FeldLeistung = "LEISTUNG";
        public const string FeldEffizienz = "EFFIZIENZ";
        public const string FeldBifazial = "BIFAZIAL";
        public const string FeldFlaeche = "FLAECHE";
        public const string FeldLaenge = "LAENGE";
        public const string FeldBreite = "BREITE";
        public const string FeldBaujahr = "BAUJAHR";
        public const string FeldIsc = "ISC";
        public const string FeldVoc = "VOC";
        public const string FeldImp = "IMP";
        public const string FeldVmp = "VMP";
        public const string FeldPmp = "PMP";
        public const string FeldAlphaIsc = "ALPHA_ISC";
        public const string FeldBetaVoc = "BETA_VOC";
        public const string FeldGammaPmp = "GAMMA_PMP";
        public const string FeldStc = "STC";
        public const string FeldPtc = "PTC";
        public const string FeldTNoct = "T_NOCT";

        // ==================================================================
        // Feldschluessel — Wechselrichter
        // ==================================================================

        public const string FeldPAcNenn = "P_AC_NENN";
        public const string FeldSAcMax = "S_AC_MAX";
        public const string FeldPDcMax = "P_DC_MAX";
        public const string FeldUMppMin = "U_MPP_MIN";
        public const string FeldUMppMax = "U_MPP_MAX";
        public const string FeldUDcMax = "U_DC_MAX";
        public const string FeldUStart = "U_START";
        public const string FeldIDcMax = "I_DC_MAX";
        public const string FeldAnzahlMppt = "ANZAHL_MPPT";
        public const string FeldPStandby = "P_STANDBY";
        public const string FeldPNacht = "P_NACHT";
        public const string FeldHerkunft = "HERKUNFT";
        public const string FeldCecDatum = "CEC_DATUM";
        public const string FeldEta05 = "ETA05";
        public const string FeldEta10 = "ETA10";
        public const string FeldEta20 = "ETA20";
        public const string FeldEta30 = "ETA30";
        public const string FeldEta50 = "ETA50";
        public const string FeldEta100 = "ETA100";
        public const string FeldEtaEuro = "ETA_EURO";
        public const string FeldEtaMax = "ETA_MAX";
        public const string FeldSandiaPdco = "SANDIA_PDCO";
        public const string FeldSandiaVdco = "SANDIA_VDCO";
        public const string FeldSandiaC0 = "SANDIA_C0";

        // ==================================================================
        // Quellenschluessel
        // ==================================================================

        /// <summary>Netzabruf der CEC-Liste (Rückfallkette, 30-Tage-Zwischenspeicher).</summary>
        public const string QuelleCec = "CEC";

        /// <summary>
        /// Dieselbe CEC-Liste als DATEI — der Weg des Anwenderentscheids W6‑O‑3
        /// („Liste als Datei und dann über Import"): Der Dateiwähler macht im
        /// Herstellerdatenordner auf, in dem <c>CEC Modules.csv</c> und
        /// <c>CEC Inverters.csv</c> ausgeliefert liegen.
        /// </summary>
        public const string QuelleCecDatei = "CEC_DATEI";

        /// <summary>PVsyst-Moduldatei <c>.pan</c>.</summary>
        public const string QuellePan = "PAN";

        /// <summary>PVsyst-Wechselrichterdatei <c>.OND</c>.</summary>
        public const string QuelleOnd = "OND";

        // ==================================================================
        // Eigenschaften
        // ==================================================================

        /// <summary>Welche der beiden Ausprägungen.</summary>
        public ModulImportArt Art { get; private set; }

        /// <summary>Der Schlüssel der <see cref="KatalogRegistry"/>-Definition.</summary>
        public string Katalog { get; private set; }

        /// <summary>Fenstertitel, bereits übersetzt.</summary>
        public string Titel { get; private set; }

        /// <summary>Kopfband unter dem Titel, bereits übersetzt.</summary>
        public string Kopfband { get; private set; }

        /// <summary>
        /// Der Schlüssel des Infoknopfs — die ZEILE LINKS in <c>help_mapping.txt</c>.
        /// Beide Schlüssel gelten unverändert weiter; keiner wandert.
        /// </summary>
        public string HilfeSchluessel { get; private set; }

        /// <summary>Die Quellenknöpfe in der Reihenfolge der Leiste.</summary>
        public IReadOnlyList<ImportQuelle> Quellen { get; private set; }

        /// <summary>Die Rasterspalten in der Reihenfolge der Maske.</summary>
        public IReadOnlyList<ImportSpalte> Spalten { get; private set; }

        /// <summary>Die Reiter des Detailblocks.</summary>
        public IReadOnlyList<ImportReiter> Reiter { get; private set; }

        /// <summary>Die Detailfelder; <see cref="ImportFeld.Reiter"/> sagt, wohin.</summary>
        public IReadOnlyList<ImportFeld> Felder { get; private set; }

        /// <summary>Beschriftung der Herstellerklappliste.</summary>
        public string FilterHersteller { get; private set; }

        /// <summary>
        /// Beschriftung der Technologieklappliste; leer = die Ausprägung führt keine
        /// (der Wechselrichter hat keine Technologie).
        /// </summary>
        public string FilterTechnologie { get; private set; }

        /// <summary>Beschriftung des Suchfeldes.</summary>
        public string FilterSuche { get; private set; }

        /// <summary>Platzhalter im Suchfeld.</summary>
        public string SuchePlatzhalter { get; private set; }

        /// <summary>Die Zahlenbereichsfilter (PV: zwei, Wechselrichter: einer).</summary>
        public IReadOnlyList<ImportZahlenfilter> Zahlenfilter { get; private set; }

        /// <summary>Statuszeile vor dem ersten Laden.</summary>
        public string StatusBereit { get; private set; }

        /// <summary>Statuszeile nach dem Filtern; <c>{0}</c> nimmt die Trefferzahl auf.</summary>
        public string StatusGefunden { get; private set; }

        /// <summary>Meldung, wenn „Übernehmen" ohne Auswahl gedrückt wird.</summary>
        public string MeldungOhneAuswahl { get; private set; }

        /// <summary>Titel der Plausibilitätsrückfrage.</summary>
        public string PlausiTitel { get; private set; }

        /// <summary>
        /// Das Zeichen für „führt die Quelle nicht".
        /// </summary>
        /// <remarks>
        /// <b>BITGLEICH aus dem Bestand übernommen</b> und keine Vereinheitlichung: Der
        /// Modulimport zeigt den BINDESTRICH seines Vorläufers
        /// (<c>Form_CECImport.ShowDetail</c> :425‑427 und :438), der
        /// Wechselrichterimport den Gedankenstrich des Hauses
        /// (<see cref="ParameterVerwendung.LEER"/>). Beide Zeichen sind geprüft; sie
        /// anzugleichen ist eine Anzeigefrage und keine Portentscheidung.
        /// </remarks>
        public string Strich { get; private set; }

        /// <summary>Anzeigetext „ja" — für Wahrheitswerte in Zelle und Detailfeld.</summary>
        private string _ja = "Ja";

        /// <summary>Anzeigetext „nein".</summary>
        private string _nein = "Nein";

        /// <summary>Der erste Eintrag jeder Klappliste („(alle)") — ein STEUERWERT.</summary>
        public string TextAlle { get; private set; }

        // ==================================================================
        // Die zwei Auspraegungen
        // ==================================================================

        /// <summary>
        /// Die Ausprägung zu einer Importart. <paramref name="text"/> übersetzt einen
        /// Beschriftungsschlüssel; <c>null</c> liefert den Schlüssel selbst zurück
        /// (dasselbe Muster wie <see cref="ModulKatalogProfil.Finde"/>).
        /// </summary>
        public static ModulImportProfil Finde(ModulImportArt art, Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            switch (art)
            {
                case ModulImportArt.Photovoltaik: return Photovoltaik(t);
                case ModulImportArt.Wechselrichter: return Wechselrichter(t);
            }

            throw new ArgumentOutOfRangeException(nameof(art));
        }

        /// <summary>Beide Ausprägungen — für Stapelprüfungen.</summary>
        public static IEnumerable<ModulImportArt> AlleArten
        {
            get
            {
                yield return ModulImportArt.Photovoltaik;
                yield return ModulImportArt.Wechselrichter;
            }
        }

        private static ModulImportProfil Photovoltaik(Func<string, string> t)
        {
            return new ModulImportProfil
            {
                Art = ModulImportArt.Photovoltaik,
                Katalog = "PV",
                Titel = t("PVIMP_TITEL"),
                Kopfband = t("PVIMP_KOPFBAND"),
                HilfeSchluessel = "Main_PV_Test.btn_Help",
                Strich = "-",
                TextAlle = t("PVIMP_ALLE"),
                _ja = t("ALLG_BTN_JA"),
                _nein = t("ALLG_BTN_NEIN"),

                Quellen = new[]
                {
                    new ImportQuelle(QuelleCec, t("PVIMP_BTN_CEC"), ausDemNetz: true, primaer: true),
                    // W6-O-3: dieselbe Liste als ausgelieferte Datei - der Weg,
                    // den der Anwender am 06.09.2026 fuer die Wechselrichter
                    // benannt hat und den die Modulseite damit mitbekommt.
                    new ImportQuelle(QuelleCecDatei, t("IMP_GER_BTN_CEC_DATEI"), ausDemNetz: false,
                                     dateifilter: "(*.csv)|*.csv", unterordner: "PV"),
                    new ImportQuelle(QuellePan, t("PVIMP_BTN_PAN"), ausDemNetz: false,
                                     dateifilter: "(*.pan)|*.pan", unterordner: "PAN")
                },

                FilterHersteller = t("PVIMP_LBL_HERSTELLER"),
                FilterTechnologie = t("PVIMP_LBL_TECHNOLOGIE"),
                FilterSuche = t("PVIMP_LBL_SUCHE"),
                SuchePlatzhalter = t("PVIMP_PLATZHALTER_SUCHE"),

                // Woertlich die Vorbelegung des Vorlaeufers:
                // Nud(num_PMin, 0, 999, 0, 2), Nud(num_PMax, 0, 999, 999, 2),
                // Nud(num_EffMin, 0, 100, 0, 2), Nud(num_EffMax, 0, 100, 50, 2).
                Zahlenfilter = new[]
                {
                    new ImportZahlenfilter(t("PVIMP_LBL_LEISTUNG_VON"), t("IMP_KAT_FILTER_BIS"),
                                           t("PVIMP_EINH_W"), 0, 999, 0, 999),
                    new ImportZahlenfilter(t("PVIMP_LBL_EFFIZIENZ_VON"), t("IMP_KAT_FILTER_BIS"),
                                           t("IMP_KAT_EINH_PROZENT"), 0, 100, 0, 50)
                },

                Spalten = new[]
                {
                    new ImportSpalte(SpalteQuelle, t("PVIMP_SP_QUELLE")),
                    new ImportSpalte(SpalteName, t("PVIMP_SP_MODULNAME")),
                    new ImportSpalte(SpalteHersteller, t("PVIMP_LBL_HERSTELLER")),
                    new ImportSpalte(SpalteTechnologie, t("PVIMP_LBL_TECHNOLOGIE")),
                    new ImportSpalte(SpaltePmp, t("PVIMP_SP_PMP")),
                    new ImportSpalte(SpalteEffizienz, t("PVIMP_SP_EFFIZIENZ")),
                    new ImportSpalte(SpalteIsc, t("PVIMP_SP_ISC")),
                    new ImportSpalte(SpalteBifazial, t("PVIMP_SP_BIFAZIAL")),
                    new ImportSpalte(SpalteVoc, t("PVIMP_SP_VOC")),
                    new ImportSpalte(SpalteJahr, t("PVIMP_SP_JAHR"))
                },

                Reiter = new[]
                {
                    new ImportReiter("UEBERSICHT", t("PVIMP_REITER_UEBERSICHT")),
                    new ImportReiter("ELEKTRISCH", t("PVIMP_REITER_ELEKTRISCH")),
                    new ImportReiter("THERMISCH", t("PVIMP_REITER_THERMISCH"), t("PVIMP_HINWEIS_PAN"))
                },

                Felder = new[]
                {
                    new ImportFeld(FeldName, t("PVIMP_SP_MODULNAME"), 0),
                    new ImportFeld(FeldHersteller, t("PVIMP_LBL_HERSTELLER"), 0),
                    new ImportFeld(FeldTechnologie, t("PVIMP_LBL_TECHNOLOGIE"), 0),
                    new ImportFeld(FeldLeistung, t("PVIMP_LBL_LEISTUNG"), 0),
                    new ImportFeld(FeldEffizienz, t("PVIMP_LBL_EFFIZIENZ"), 0),
                    new ImportFeld(FeldBifazial, t("PVIMP_LBL_BIFAZIAL"), 0),
                    new ImportFeld(FeldFlaeche, t("PVIMP_LBL_FLAECHE"), 0),
                    new ImportFeld(FeldLaenge, t("PVIMP_LBL_LAENGE"), 0),
                    new ImportFeld(FeldBreite, t("PVIMP_LBL_BREITE"), 0),
                    new ImportFeld(FeldBaujahr, t("PVIMP_LBL_BAUJAHR"), 0),

                    new ImportFeld(FeldIsc, t("PVIMP_LBL_ISC"), 1),
                    new ImportFeld(FeldVoc, t("PVIMP_LBL_VOC"), 1),
                    new ImportFeld(FeldImp, t("PVIMP_LBL_IMP"), 1),
                    new ImportFeld(FeldVmp, t("PVIMP_LBL_VMP"), 1),
                    new ImportFeld(FeldPmp, t("PVIMP_LBL_PMP"), 1),
                    new ImportFeld(FeldAlphaIsc, t("PVIMP_LBL_ALPHA_ISC"), 1),
                    new ImportFeld(FeldBetaVoc, t("PVIMP_LBL_BETA_VOC"), 1),
                    new ImportFeld(FeldGammaPmp, t("PVIMP_LBL_GAMMA_PMP"), 1),
                    new ImportFeld(FeldStc, t("PVIMP_LBL_STC"), 1),
                    new ImportFeld(FeldPtc, t("PVIMP_LBL_PTC"), 1),

                    new ImportFeld(FeldTNoct, t("PVIMP_LBL_TNOCT"), 2)
                },

                StatusBereit = t("PVIMP_STATUS_BEREIT"),
                StatusGefunden = t("PVIMP_STATUS_GEFUNDEN"),
                MeldungOhneAuswahl = t("PVIMP_MSG_KEINE_AUSWAHL"),
                PlausiTitel = t("PVIMP_PLAUSI_TITEL")
            };
        }

        private static ModulImportProfil Wechselrichter(Func<string, string> t)
        {
            return new ModulImportProfil
            {
                Art = ModulImportArt.Wechselrichter,
                Katalog = "WECHSELRICHTER",
                Titel = t("WRK_IMP_TITEL"),
                Kopfband = t("WRK_IMP_KOPFBAND"),
                HilfeSchluessel = "Form_WechselrichterImport.btn_Help",
                Strich = ParameterVerwendung.LEER,
                TextAlle = t("PVIMP_ALLE"),
                _ja = t("ALLG_BTN_JA"),
                _nein = t("ALLG_BTN_NEIN"),

                Quellen = new[]
                {
                    new ImportQuelle(QuelleCec, t("WRK_IMP_BTN_CEC"), ausDemNetz: true, primaer: true),
                    new ImportQuelle(QuelleCecDatei, t("IMP_GER_BTN_CEC_DATEI"), ausDemNetz: false,
                                     dateifilter: "(*.csv)|*.csv", unterordner: "PV"),
                    new ImportQuelle(QuelleOnd, t("WRK_IMP_BTN_OND"), ausDemNetz: false,
                                     dateifilter: "(*.ond)|*.ond", unterordner: "PV")
                },

                FilterHersteller = t("WRK_LBL_FIRMA"),
                FilterTechnologie = "",
                FilterSuche = t("PVIMP_LBL_SUCHE"),
                SuchePlatzhalter = t("PVIMP_PLATZHALTER_SUCHE"),

                // Die AC-Nennleistungen der CEC-Liste reichen von 0,2 kW
                // (Modulwechselrichter) bis ueber 1 000 kW (Zentralgeraete); die
                // Obergrenze 0 heisst deshalb "keine Obergrenze".
                Zahlenfilter = new[]
                {
                    new ImportZahlenfilter(t("WRK_IMP_LBL_P_AC_VON"), t("IMP_KAT_FILTER_BIS"),
                                           "kW", 0, 10000, 0, 0)
                },

                Spalten = new[]
                {
                    new ImportSpalte(SpalteQuelle, t("PVIMP_SP_QUELLE")),
                    new ImportSpalte(SpalteName, t("WRK_IMP_SP_GERAET")),
                    new ImportSpalte(SpalteHersteller, t("WRK_LBL_FIRMA")),
                    new ImportSpalte(SpaltePAc, t("WRK_IMP_SP_P_AC")),
                    new ImportSpalte(SpalteEtaEuro, t("WRK_IMP_SP_ETA_EURO")),
                    new ImportSpalte(SpalteMpp, t("WRK_IMP_SP_MPP")),
                    new ImportSpalte(SpalteUDcMax, t("WRK_IMP_SP_U_DC_MAX"))
                },

                Reiter = new[]
                {
                    new ImportReiter("GERAET", t("WRK_GRUPPE_GERAET")),
                    new ImportReiter("KENNLINIE", t("WRK_GRUPPE_WIRKUNGSGRAD"),
                                     t("WRK_IMP_HINWEIS_KENNLINIE"))
                },

                Felder = new[]
                {
                    new ImportFeld(FeldName, t("WRK_IMP_SP_GERAET"), 0),
                    new ImportFeld(FeldHersteller, t("WRK_LBL_FIRMA"), 0),
                    new ImportFeld(FeldPAcNenn, t("WRK_LBL_P_AC_NENN"), 0),
                    new ImportFeld(FeldSAcMax, t("WRK_LBL_S_AC_MAX"), 0),
                    new ImportFeld(FeldPDcMax, t("WRK_LBL_P_DC_MAX"), 0),
                    new ImportFeld(FeldUMppMin, t("WRK_LBL_U_MPP_MIN"), 0),
                    new ImportFeld(FeldUMppMax, t("WRK_LBL_U_MPP_MAX"), 0),
                    new ImportFeld(FeldUDcMax, t("WRK_LBL_U_DC_MAX"), 0),
                    new ImportFeld(FeldUStart, t("WRK_LBL_U_START"), 0),
                    new ImportFeld(FeldIDcMax, t("WRK_LBL_I_DC_MAX"), 0),
                    new ImportFeld(FeldAnzahlMppt, t("WRK_LBL_ANZAHL_MPPT"), 0),
                    new ImportFeld(FeldPStandby, t("WRK_LBL_P_STANDBY"), 0),
                    new ImportFeld(FeldPNacht, t("WRK_LBL_P_NACHT"), 0),
                    new ImportFeld(FeldHerkunft, t("WRK_LBL_HERKUNFT"), 0),
                    new ImportFeld(FeldCecDatum, t("WRK_IMP_LBL_CEC_DATUM"), 0),

                    new ImportFeld(FeldEta05, t("WRK_LBL_ETA05"), 1),
                    new ImportFeld(FeldEta10, t("WRK_LBL_ETA10"), 1),
                    new ImportFeld(FeldEta20, t("WRK_LBL_ETA20"), 1),
                    new ImportFeld(FeldEta30, t("WRK_LBL_ETA30"), 1),
                    new ImportFeld(FeldEta50, t("WRK_LBL_ETA50"), 1),
                    new ImportFeld(FeldEta100, t("WRK_LBL_ETA100"), 1),
                    new ImportFeld(FeldEtaEuro, t("WRK_LBL_ETA_EURO"), 1),
                    new ImportFeld(FeldEtaMax, t("WRK_LBL_ETA_MAX"), 1),
                    new ImportFeld(FeldSandiaPdco, t("WRK_IMP_LBL_SANDIA_PDCO"), 1),
                    new ImportFeld(FeldSandiaVdco, t("WRK_IMP_LBL_SANDIA_VDCO"), 1),
                    new ImportFeld(FeldSandiaC0, t("WRK_IMP_LBL_SANDIA_C0"), 1)
                },

                StatusBereit = t("WRK_IMP_STATUS_BEREIT"),
                StatusGefunden = t("WRK_IMP_STATUS_GEFUNDEN"),
                MeldungOhneAuswahl = t("WRK_IMP_MSG_KEINE_AUSWAHL"),
                PlausiTitel = t("WRK_IMP_PLAUSI_TITEL")
            };
        }

        // ==================================================================
        // Vom Satz zur Zeile
        // ==================================================================

        /// <summary>
        /// Baut die neutrale Zeile zu einem Satz. Welcher Satztyp kommt, entscheidet die
        /// QUELLE, nicht die Ausprägung: Der Wechselrichterimport bekommt aus der
        /// CEC-Liste einen <see cref="CecWechselrichter"/> und aus einer
        /// <c>.OND</c>-Datei einen <see cref="OndWechselrichter"/>.
        /// </summary>
        /// <returns>Die Zeile; <c>null</c>, wenn der Satztyp nicht zur Ausprägung passt.</returns>
        public ImportZeile Zeile(int nummer, object satz)
        {
            if (satz is UnifiedModule modul) return ZeileModul(nummer, modul);
            if (satz is CecWechselrichter cec) return ZeileCec(nummer, cec);
            if (satz is OndWechselrichter ond) return ZeileOnd(nummer, ond);
            return null;
        }

        /// <summary>Die Zeile eines PV-Moduls (CEC oder PAN).</summary>
        private ImportZeile ZeileModul(int nummer, UnifiedModule m)
        {
            var z = new ImportZeile(nummer, m)
            {
                Name = m.Name ?? "",
                Hersteller = m.Manufacturer ?? "",
                Technologie = m.Technology ?? "",
                // Die Leistung des Filters ist I_mp · V_mp und nicht STC - woertlich
                // ApplyFilter :228 (Befund W13-B40, offener Punkt W13-O-3).
                Zahl1 = m.Pmp,
                Zahl2 = m.Efficiency
            };

            string bifazial = m.Bifacial ? _ja : _nein;

            z.Spalten[SpalteQuelle] = m.Database ?? "";
            z.Spalten[SpalteName] = m.Name ?? "";
            z.Spalten[SpalteHersteller] = m.Manufacturer ?? "";
            z.Spalten[SpalteTechnologie] = m.Technology ?? "";
            z.Spalten[SpaltePmp] = m.Pmp.ToString("N1");
            z.Spalten[SpalteEffizienz] = m.Efficiency.ToString("N2");
            z.Spalten[SpalteIsc] = m.Isc.ToString("N2");
            z.Spalten[SpalteBifazial] = bifazial;
            z.Spalten[SpalteVoc] = m.Voc.ToString("N2");
            z.Spalten[SpalteJahr] = m.Date.ToString(CultureInfo.InvariantCulture);

            z.Felder[FeldName] = m.Name ?? "";
            z.Felder[FeldHersteller] = m.Manufacturer ?? "";
            z.Felder[FeldTechnologie] = m.Technology ?? "";
            z.Felder[FeldLeistung] = Fest(m.Pmp, 2);
            z.Felder[FeldEffizienz] = Fest(m.Efficiency, 2);
            z.Felder[FeldBifazial] = m.Bifacial && m.BifazialFaktor > 0
                ? _ja + " (" + m.BifazialFaktor.ToString("F2") + ")"
                : bifazial;
            z.Felder[FeldFlaeche] = Fest(m.Flaeche, 2);
            z.Felder[FeldLaenge] = Fest(m.Laenge, 2);
            z.Felder[FeldBreite] = Fest(m.Breite, 2);
            z.Felder[FeldBaujahr] = m.Date.ToString(CultureInfo.InvariantCulture);

            z.Felder[FeldIsc] = Fest(m.Isc, 3);
            z.Felder[FeldVoc] = Fest(m.Voc, 3);
            z.Felder[FeldImp] = Fest(m.Imp, 3);
            z.Felder[FeldVmp] = Fest(m.Vmp, 3);
            z.Felder[FeldPmp] = Fest(m.Pmp, 2);
            z.Felder[FeldAlphaIsc] = FestOderStrich(m.AlphaSc, 6);
            z.Felder[FeldBetaVoc] = FestOderStrich(m.BetaOc, 6);
            z.Felder[FeldGammaPmp] = Fest(m.GammaPmp, 4);
            z.Felder[FeldStc] = Fest(m.Stc, 2);
            z.Felder[FeldPtc] = Fest(m.Ptc, 2);
            z.Felder[FeldTNoct] = FestOderStrich(m.TNoct, 1);

            return z;
        }

        /// <summary>Die Zeile eines CEC-Wechselrichters.</summary>
        private ImportZeile ZeileCec(int nummer, CecWechselrichter g)
        {
            double kw = g.Paco / 1000.0;
            double?[] etas = g.Stuetzstellen();
            double? euro = WechselrichterKennlinie.EuroWirkungsgrad(etas);

            var z = new ImportZeile(nummer, g)
            {
                Name = g.Name ?? "",
                Hersteller = g.Hersteller ?? "",
                Zahl1 = kw
            };

            z.Spalten[SpalteQuelle] = DbWerte.WR_HERKUNFT_CEC;
            z.Spalten[SpalteName] = g.Name ?? "";
            z.Spalten[SpalteHersteller] = g.Hersteller ?? "";
            z.Spalten[SpaltePAc] = kw.ToString("N3");
            z.Spalten[SpalteEtaEuro] = euro.HasValue ? euro.Value.ToString("N4") : Strich;
            z.Spalten[SpalteMpp] = g.MpptLow.ToString("N0") + " … " + g.MpptHigh.ToString("N0");
            z.Spalten[SpalteUDcMax] = g.Vdcmax.ToString("N0");

            z.Felder[FeldName] = g.Name ?? "";
            z.Felder[FeldHersteller] = g.Hersteller ?? "";
            z.Felder[FeldPAcNenn] = Fest(kw, 3);
            z.Felder[FeldSAcMax] = Strich;                       // Paco ist Wirkleistung
            z.Felder[FeldPDcMax] = Strich;                       // fuehrt die Liste nicht
            z.Felder[FeldUMppMin] = Fest(g.MpptLow, 1);
            z.Felder[FeldUMppMax] = Fest(g.MpptHigh, 1);
            z.Felder[FeldUDcMax] = Fest(g.Vdcmax, 1);
            z.Felder[FeldUStart] = Strich;
            z.Felder[FeldIDcMax] = Fest(g.Idcmax, 2);
            z.Felder[FeldAnzahlMppt] = Strich;                   // offener Punkt W6-O-2
            z.Felder[FeldPStandby] = Fest(g.Pso, 3);
            z.Felder[FeldPNacht] = Fest(g.Pnt, 3);
            z.Felder[FeldHerkunft] = DbWerte.WR_HERKUNFT_CEC;
            z.Felder[FeldCecDatum] = g.CecDatum ?? "";

            Kennlinienfelder(z, etas, euro, Hoechster(etas));
            z.Felder[FeldSandiaPdco] = Fest(g.Pdco, 2);
            z.Felder[FeldSandiaVdco] = Fest(g.Vdco, 1);
            z.Felder[FeldSandiaC0] = Fest(g.C0, 8);

            return z;
        }

        /// <summary>Die Zeile eines OND-Wechselrichters.</summary>
        private ImportZeile ZeileOnd(int nummer, OndWechselrichter g)
        {
            WechselrichterModel m = g.NachModell();
            double?[] etas = g.Stuetzstellen();

            var z = new ImportZeile(nummer, g)
            {
                Name = g.Name ?? "",
                Hersteller = g.Hersteller ?? "",
                Zahl1 = g.PNomConv
            };

            z.Spalten[SpalteQuelle] = DbWerte.WR_HERKUNFT_OND;
            z.Spalten[SpalteName] = g.Name ?? "";
            z.Spalten[SpalteHersteller] = g.Hersteller ?? "";
            z.Spalten[SpaltePAc] = g.PNomConv.ToString("N3");
            z.Spalten[SpalteEtaEuro] = m.m_Eta_Euro.HasValue
                ? m.m_Eta_Euro.Value.ToString("N4") : Strich;
            z.Spalten[SpalteMpp] = g.VMppMin.ToString("N0") + " … " + g.VMPPMax.ToString("N0");
            z.Spalten[SpalteUDcMax] = g.VAbsMax.ToString("N0");

            z.Felder[FeldName] = g.Name ?? "";
            z.Felder[FeldHersteller] = g.Hersteller ?? "";
            z.Felder[FeldPAcNenn] = FestOderStrich(m.m_P_AC_Nenn, 3);
            z.Felder[FeldSAcMax] = FestOderStrich(m.m_S_AC_Max, 3);
            z.Felder[FeldPDcMax] = FestOderStrich(m.m_P_DC_Max, 3);
            z.Felder[FeldUMppMin] = FestOderStrich(m.m_U_Mpp_Min, 1);
            z.Felder[FeldUMppMax] = FestOderStrich(m.m_U_Mpp_Max, 1);
            z.Felder[FeldUDcMax] = FestOderStrich(m.m_U_Dc_Max, 1);
            z.Felder[FeldUStart] = FestOderStrich(m.m_U_Start, 1);
            z.Felder[FeldIDcMax] = FestOderStrich(m.m_I_Dc_Max, 2);
            z.Felder[FeldAnzahlMppt] = m.m_Anzahl_Mppt.HasValue
                ? m.m_Anzahl_Mppt.Value.ToString(CultureInfo.CurrentCulture) : Strich;
            z.Felder[FeldPStandby] = FestOderStrich(m.m_P_Standby, 3);
            z.Felder[FeldPNacht] = FestOderStrich(m.m_P_Nacht, 3);
            z.Felder[FeldHerkunft] = DbWerte.WR_HERKUNFT_OND;
            z.Felder[FeldCecDatum] = g.Kennlinienfassung;

            Kennlinienfelder(z, etas, m.m_Eta_Euro, m.m_Eta_Max);

            // Eine OND-Datei fuehrt kein Sandia-Modell; VMppNom ist die
            // Bezugsspannung der Kennlinie und steht deshalb an der Stelle von Vdco.
            z.Felder[FeldSandiaPdco] = Strich;
            z.Felder[FeldSandiaVdco] = FestOderStrich(m.m_Sandia_Vdco, 1);
            z.Felder[FeldSandiaC0] = Strich;

            return z;
        }

        /// <summary>Die acht Wirkungsgradfelder — sechs Stützstellen, Euro und Maximum.</summary>
        private void Kennlinienfelder(ImportZeile z, double?[] etas, double? euro, double? max)
        {
            string[] schluessel = { FeldEta05, FeldEta10, FeldEta20, FeldEta30, FeldEta50, FeldEta100 };
            for (int i = 0; i < schluessel.Length; i++)
                z.Felder[schluessel[i]] = FestOderStrich(i < etas.Length ? etas[i] : null, 4);

            z.Felder[FeldEtaEuro] = FestOderStrich(euro, 4);
            z.Felder[FeldEtaMax] = FestOderStrich(max, 4);
        }

        /// <summary>Der größte vorhandene Wert der Stützstellen; <c>null</c>, wenn keine da ist.</summary>
        private static double? Hoechster(double?[] etas)
        {
            double? max = null;
            foreach (double? e in etas)
                if (e.HasValue && (!max.HasValue || e.Value > max.Value)) max = e;
            return max;
        }

        /// <summary>Eine Zahl mit fester Stellenzahl in der Kultur des Anwenders.</summary>
        private static string Fest(double wert, int stellen)
        {
            return wert.ToString("F" + stellen.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Dasselbe, aber <see cref="Strich"/>, wo die Quelle nichts führt.</summary>
        private string FestOderStrich(double? wert, int stellen)
        {
            return wert.HasValue ? Fest(wert.Value, stellen) : Strich;
        }
    }
}
