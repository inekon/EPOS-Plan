using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein waehlbarer Lastgang der Peak-Shaving-Maske: entweder eine Ganglinie aus
    /// der Datenbank (Projekt oder Stamm) oder eine ad hoc importierte Datei.
    /// </summary>
    public sealed class GanglinienEintrag
    {
        /// <summary>Schluessel der Kopfzeile; 0 bei einer importierten Datei.</summary>
        public int Id;

        /// <summary>Anzeigename.</summary>
        public string Bezeichner = "";

        /// <summary>Raster der abgelegten Werte: 1 = Stunde, 4 = Viertelstunde.</summary>
        public int Zeitinterval;

        /// <summary>true = Stammganglinie, false = Projektganglinie.</summary>
        public bool AusStamm;

        /// <summary>
        /// Werte einer importierten Datei [kW] im Viertelstundenraster.
        /// <c>null</c> bei Datenbank-Ganglinien - die werden erst beim Rechnen gelesen.
        /// </summary>
        /// <remarks>
        /// Ein Import aus dieser Maske wird bewusst <b>nicht</b> in die Datenbank
        /// geschrieben (Umsetzungskonzept 2.2, Aufrufweg 2): die Maske rechnet ad hoc.
        /// Der Weg in den Stammkatalog bleibt <c>Form_Stromganglinie_Admin</c>.
        /// </remarks>
        public double[] ImportWerte;

        /// <summary>true, wenn der Eintrag aus einem Dateiimport stammt.</summary>
        public bool IstImport => ImportWerte != null;
    }

    /// <summary>
    /// Vorbelegung der Speicher- und Bewertungsparameter fuer die
    /// Peak-Shaving-Maske.
    /// </summary>
    /// <remarks>
    /// Stammt aus Geraet (<c>Tab_Stromspeicher</c>) und aktiver Variante
    /// (<c>Tab_StromspeicherVariante</c>), wenn das Projekt einen Speicher hat;
    /// sonst gelten die Vorgaben der Modelle (Fachkonzept 5.1). Der Leistungspreis
    /// L_P und der mittlere Bezugspreis bleiben bewusst bei 0, solange die Variante
    /// nichts anderes sagt - fuer L_P ist der Erfahrungswert offener Punkt 3 des
    /// Fachkonzepts, und ein erfundener Default wuerde die Wirtschaftlichkeit
    /// unbemerkt verfaelschen.
    /// </remarks>
    public sealed class PeakShavingVorbelegung
    {
        /// <summary>true, wenn Geraet und Variante des Projekts gelesen werden konnten.</summary>
        public bool AusProjekt;

        /// <summary>Name des Speichers, soweit bekannt.</summary>
        public string Bezeichner = "";

        /// <summary>Lade- und Entladeleistung P [kW].</summary>
        public double PKw = 100.0;

        /// <summary>Nutzbare Nennkapazitaet C_nom [kWh].</summary>
        public double KapazitaetKwh = 200.0;

        /// <summary>Untere Bandgrenze [%].</summary>
        public double SoCMinProzent = StromspeicherVarianteModel.SOC_MIN_VORGABE;

        /// <summary>Obere Bandgrenze [%].</summary>
        public double SoCMaxProzent = StromspeicherVarianteModel.SOC_MAX_VORGABE;

        /// <summary>Start-Ladezustand [%].</summary>
        public double StartSoCProzent = StromspeicherVarianteModel.SOC_MIN_VORGABE;

        /// <summary>Round-Trip-Wirkungsgrad eta_RT [-].</summary>
        public double WirkungsgradRt = StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE;

        /// <summary>Leistungspreis L_P [EUR/(kW*a)].</summary>
        public double LeistungspreisEurProKwA;

        /// <summary>Mittlerer Bezugspreis [ct/kWh].</summary>
        public double BezugspreisMittelCtKwh;

        /// <summary>Kapitalzins [%].</summary>
        public double KapitalzinsProzent = StromspeicherVarianteModel.KAPITALZINS_VORGABE;

        /// <summary>Nutzungsdauer [a].</summary>
        public double NutzungsdauerA = StromspeicherVarianteModel.NUTZUNGSDAUER_VORGABE;

        /// <summary>Kapazitaetsbezogene Investition c_cap [EUR/kWh].</summary>
        public double CCapEurProKwh;

        /// <summary>Leistungsbezogene Investition c_pow [EUR/kW].</summary>
        public double CPowEurProKw;

        /// <summary>Leistungsunabhaengiger Investitionsanteil I_fix [EUR].</summary>
        public double IFixEur;

        /// <summary>Excel-Kompatibilitaetsmodus der Variante.</summary>
        public bool Kompatibilitaetsmodus;
    }

    /// <summary>
    /// Datenschicht der Peak-Shaving-Maske (AP7): Ganglinienauswahl, Werteabruf und
    /// Parametervorbelegung.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Klasse haelt die Drei-Schichten-Regel ein: <c>Form_PeakShaving</c> kennt
    /// keine Tabelle und kein SQL, die Engine kennt weder Datenbank noch UI.
    /// Zugegriffen wird ausschliesslich ueber <see cref="DataRepository"/> mit
    /// parametrisierten Abfragen - kein <c>RecordSet</c> (Projektregel fuer neuen
    /// Code).
    /// </para>
    /// <para>
    /// <b>Nur lesend.</b> Die Maske schreibt nichts in die Datenbank: weder die
    /// importierte Ganglinie noch die Ergebnisse (Fachkonzept offener Punkt 10, in
    /// dieser Stufe bewusst nicht entschieden - der Exportweg ist CSV).
    /// </para>
    /// </remarks>
    public static class PeakShavingCtrl
    {
        private const string HeadStamm = "Tab_Stromganglinie_STAMM";
        private const string DataStamm = "Tab_StromganglinieDaten_STAMM";
        private const string HeadProjekt = "Tab_Stromganglinie";
        private const string DataProjekt = "Tab_StromganglinieDaten";

        // ==================================================================
        // Ganglinienauswahl
        // ==================================================================

        /// <summary>
        /// Liefert die waehlbaren Ganglinien: erst die des Projekts, dann die des
        /// Stammkatalogs. Ohne Projekt (<paramref name="idProjekt"/> = 0) bleiben die
        /// Stammganglinien - die Maske ist ausdruecklich auch ohne geoeffnetes
        /// Projekt nutzbar (Fachkonzept 6.4, Abgrenzung Rev. 4).
        /// </summary>
        public static List<GanglinienEintrag> LeseGanglinien(int idProjekt)
        {
            List<GanglinienEintrag> liste = new List<GanglinienEintrag>();

            if (idProjekt != 0)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Zeitinterval FROM " + HeadProjekt +
                    " WHERE ID_Projekt = ? ORDER BY Bezeichner",
                    new DbParam("@projekt", DbParamTyp.Integer) { Wert = idProjekt });
                Uebernehmen(liste, dt, false);
            }

            DataTable dtStamm = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Zeitinterval FROM " + HeadStamm + " ORDER BY Bezeichner", null);
            Uebernehmen(liste, dtStamm, true);

            return liste;
        }

        private static void Uebernehmen(List<GanglinienEintrag> ziel, DataTable dt, bool ausStamm)
        {
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                GanglinienEintrag e = new GanglinienEintrag();
                e.AusStamm = ausStamm;
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    e.Id = Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture);
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    e.Bezeichner = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("Zeitinterval") && row["Zeitinterval"] != DBNull.Value)
                    e.Zeitinterval = Convert.ToInt32(row["Zeitinterval"], CultureInfo.InvariantCulture);
                if (e.Id != 0) ziel.Add(e);
            }
        }

        /// <summary>
        /// Liest den Lastgang eines Eintrags als Viertelstundenreihe [kW].
        /// </summary>
        /// <returns>
        /// 35.040 Werte, oder die abgelegte Laenge, wenn sie keinem bekannten Raster
        /// entspricht. <c>null</c>, wenn nichts zu lesen war.
        /// </returns>
        /// <remarks>
        /// Stundenwerte werden nach der Expansionsregel der Engine
        /// (<see cref="RasterAdapter"/>) auf Viertelstunden gelegt:
        /// <b>Wertwiederholung ohne Interpolation</b>, <c>v[i*4+0..3] = w[i]</c>.
        /// Bewusst hier ausgeschrieben statt ueber <c>RasterAdapter</c>, weil dessen
        /// Ueberladung <c>float[]</c> erwartet und der Umweg ueber <c>float</c> die
        /// Genauigkeit der Datenbankwerte unnoetig verkuerzen wuerde.
        /// </remarks>
        public static double[] LeseWerte(GanglinienEintrag eintrag)
        {
            if (eintrag == null) throw new ArgumentNullException(nameof(eintrag));
            if (eintrag.IstImport) return eintrag.ImportWerte;

            string tabelle = eintrag.AusStamm ? DataStamm : DataProjekt;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Wert FROM " + tabelle + " WHERE ID_Ganglinie = ? ORDER BY ID",
                new DbParam("@g", DbParamTyp.Integer) { Wert = eintrag.Id });

            if (dt == null || dt.Rows.Count == 0) return null;

            double[] roh = new double[dt.Rows.Count];
            for (int i = 0; i < roh.Length; i++)
            {
                object v = dt.Rows[i][0];
                roh[i] = v != DBNull.Value ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : 0.0;
            }

            if (eintrag.Zeitinterval != 1) return roh;

            double[] fein = new double[roh.Length * 4];
            for (int i = 0; i < roh.Length; i++)
            {
                int b = i * 4;
                fein[b] = roh[i];
                fein[b + 1] = roh[i];
                fein[b + 2] = roh[i];
                fein[b + 3] = roh[i];
            }
            return fein;
        }

        // ==================================================================
        // Parametervorbelegung
        // ==================================================================

        /// <summary>
        /// Baut die Vorbelegung der Maske. Hat das Projekt einen Stromspeicher mit
        /// aktiver Variante, kommen die Werte von dort; sonst gelten die Vorgaben der
        /// Modelle (Fachkonzept 5.1).
        /// </summary>
        /// <remarks>
        /// <b>Nur lesend</b> - insbesondere wird die Variante nicht angelegt und nicht
        /// veraendert. Fehlt eine der Tabellen (Datenbank vor der Migration), bleibt
        /// es bei den Vorgaben; die Maske ist damit auch ohne Speicherprojekt
        /// arbeitsfaehig.
        /// </remarks>
        public static PeakShavingVorbelegung LeseVorbelegung(int idProjekt)
        {
            PeakShavingVorbelegung v = new PeakShavingVorbelegung();
            if (idProjekt == 0) return v;

            try
            {
                // Geraetedaten des ersten Stromspeichers des Projekts. "sp.*" statt
                // einer Spaltenliste, damit eine noch nicht migrierte Datenbank die
                // Abfrage nicht scheitern laesst - die Columns.Contains-Wache in
                // Zahl() faengt fehlende Spalten ab.
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT sp.* FROM Tab_Energieanlagen AS a INNER JOIN Tab_Stromspeicher AS sp " +
                    "ON a.ID_SP = sp.ID WHERE a.ID_Projekt = ? AND a.ID_Type = ?",
                    new DbParam("@projekt", DbParamTyp.Integer) { Wert = idProjekt },
                    new DbParam("@typ", DbParamTyp.Integer) { Wert = WizardItemClass.SP_TYP });

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    double energie = Zahl(dt, row, "Energie");
                    double leistung = Zahl(dt, row, "Leistung");
                    if (energie > 0.0) v.KapazitaetKwh = energie;
                    // Fehlt die Leistungsgrenze in Altdaten, gilt 1 C - dieselbe
                    // Annahme wie im Simulationsweg.
                    v.PKw = leistung > 0.0 ? leistung : v.KapazitaetKwh;

                    double eta = Zahl(dt, row, "Wirkungsgrad_RT");
                    if (eta > 0.0 && eta <= 1.0) v.WirkungsgradRt = eta;

                    double ladezustand = Zahl(dt, row, "Ladezustand");
                    if (ladezustand > 0.0) v.StartSoCProzent = ladezustand;

                    v.CCapEurProKwh = Zahl(dt, row, "Modulkosten");
                    v.CPowEurProKw = Zahl(dt, row, "Leistungskosten");
                    v.IFixEur = Zahl(dt, row, "Investition_Fix");
                    v.Bezeichner = Text(dt, row, "Bezeichner");
                    v.AusProjekt = true;
                }

                // Betriebsfuehrung aus der aktiven Variante - nur gelesen.
                StromspeicherVarianteModel variante =
                    new StromspeicherVarianteCtrl().ReadAktiveVariante(idProjekt);
                if (variante != null)
                {
                    v.SoCMinProzent = variante.SoC_Min_Prozent;
                    v.SoCMaxProzent = variante.SoC_Max_Prozent;
                    v.KapitalzinsProzent = variante.Kapitalzins;
                    v.NutzungsdauerA = variante.Nutzungsdauer;
                    v.Kompatibilitaetsmodus = variante.Kompatibilitaetsmodus;
                    v.LeistungspreisEurProKwA = variante.L_P;
                    v.AusProjekt = true;

                    if (v.StartSoCProzent < v.SoCMinProzent) v.StartSoCProzent = v.SoCMinProzent;
                    if (v.StartSoCProzent > v.SoCMaxProzent) v.StartSoCProzent = v.SoCMaxProzent;
                }
            }
            catch (Exception)
            {
                // Tabellen einer noch nicht migrierten Datenbank - die Vorgaben
                // stehen bereits im Objekt, die Maske bleibt bedienbar.
                //
                // BIS iU9-W12.0a stand hier catch (OleDbException). Seit der
                // SQLite-Umstellung (6486c36) wirft der Zugriff SqliteException, der
                // Rueckfall griff also gar nicht mehr (Befund W12-B25) - und
                // EPOS.Kern nennt System.Data.OleDb ueberhaupt nicht mehr. Gefangen
                // wird deshalb der Oberbegriff: Was diese Methode kann, ist
                // Vorbelegen; kein Fehler von hier darf die Maske verhindern.
            }

            return v;
        }

        private static double Zahl(DataTable dt, DataRow row, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return 0.0;
            object v = row[spalte];
            if (v == DBNull.Value || v == null) return 0.0;
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
            catch (InvalidCastException) { return 0.0; }
            catch (FormatException) { return 0.0; }
        }

        private static string Text(DataTable dt, DataRow row, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return "";
            object v = row[spalte];
            return v != DBNull.Value && v != null ? v.ToString() : "";
        }
    }
}
