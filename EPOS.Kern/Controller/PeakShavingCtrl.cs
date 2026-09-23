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
        /// Bewusst hier ausgeschrieben statt ueber <c>RasterAdapter</c>: bis W8-O-5d
        /// erwartete dessen Ueberladung <c>float[]</c>, und der Umweg ueber <c>float</c>
        /// haette die Genauigkeit der Datenbankwerte unnoetig verkuerzt. Seit der Kern
        /// durchgehend in <c>double</c> rechnet, sind beide Wege wertgleich.
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
        // Stufe 5 der Neuordnung (V16): die Lastgaenge als Katalogliste
        // ==================================================================

        /// <summary>
        /// <b>Die Zeilen der Liste der Lastgaenge</b> (Konzept Administrationsdialoge, V16) —
        /// je Eintrag von <see cref="LeseGanglinien"/> eine Zeile mit Quelle (Stamm oder
        /// Projekt), Intervall in Minuten und Jahresmaximum in kW.
        ///
        /// <para><b>Der Schluessel ist der Platz in der Liste</b> (<c>"G" + Platz</c>), nicht
        /// der Bezeichner: Projekt und Stamm fuehren gleichnamige Ganglinien, und die Huelle
        /// liest die Werte ueber den Platz (<c>PeakShavingHuelle.Reihe</c>).</para>
        ///
        /// <para><b>Das Jahresmaximum kommt aus EINER Gruppenabfrage je Tabelle</b>
        /// (<c>MAX(Wert)</c> je Ganglinie), nicht je Zeile. Es ist der hoechste abgelegte
        /// Wert — Viertelstunden- bzw. Stundenleistung; die Vervielfachung der Stundenwerte
        /// auf Viertelstunden (<see cref="LeseWerte"/>) aendert das Maximum nicht.</para>
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen(
            IReadOnlyList<GanglinienEintrag> eintraege, int idProjekt)
        {
            var liste = new List<Katalogfilterzeile>();
            if (eintraege == null) return liste;

            Dictionary<int, double> stamm = Maxima(
                "SELECT ID_Ganglinie, MAX(Wert) AS Spitze FROM " + DataStamm + " GROUP BY ID_Ganglinie", null);
            Dictionary<int, double> projekt = idProjekt == 0
                ? new Dictionary<int, double>()
                : Maxima("SELECT d.ID_Ganglinie, MAX(d.Wert) AS Spitze FROM " + DataProjekt + " AS d " +
                         "INNER JOIN " + HeadProjekt + " AS k ON k.ID = d.ID_Ganglinie " +
                         "WHERE k.ID_Projekt = ? GROUP BY d.ID_Ganglinie",
                         new DbParam("@projekt", DbParamTyp.Integer) { Wert = idProjekt });

            for (int platz = 0; platz < eintraege.Count; platz++)
            {
                GanglinienEintrag e = eintraege[platz];
                double spitze;
                bool bekannt = (e.AusStamm ? stamm : projekt).TryGetValue(e.Id, out spitze);

                var zeile = new Katalogfilterzeile(e.Id, e.Bezeichner) { Schluessel = "G" + platz };
                zeile.MitText(Katalogfilterprofil.SpBezeichner, e.Bezeichner)
                     .MitText(Katalogfilterprofil.SpQuelle, e.AusStamm
                         ? MyResource.Resource.PEAK_QUELLE_STAMM : MyResource.Resource.PEAK_QUELLE_PROJEKT)
                     .MitZahl(Katalogfilterprofil.SpIntervallMin, IntervallMinuten(e.Zeitinterval), 0)
                     .MitZahl(Katalogfilterprofil.SpJahresmaximumKw, bekannt ? spitze : (double?)null, 1);
                liste.Add(zeile);
            }
            return liste;
        }

        /// <summary>
        /// Das Raster in Minuten: <c>Zeitinterval</c> 1 = Stunde (60 min), 4 = Viertelstunde
        /// (15 min); ein anderer Wert ist unbekannt (<c>null</c>).
        /// </summary>
        public static double? IntervallMinuten(int zeitinterval)
            => zeitinterval == 1 ? 60 : zeitinterval == 4 ? 15 : (double?)null;

        private static Dictionary<int, double> Maxima(string sql, DbParam parameter)
        {
            var ergebnis = new Dictionary<int, double>();
            DataTable dt = parameter == null
                ? DataRepository.GetDataTable(sql, null)
                : DataRepository.GetDataTable(sql, parameter);
            if (dt == null) return ergebnis;
            foreach (DataRow r in dt.Rows)
            {
                if (r[0] == DBNull.Value || r[1] == DBNull.Value) continue;
                ergebnis[Convert.ToInt32(r[0], CultureInfo.InvariantCulture)] =
                    Convert.ToDouble(r[1], CultureInfo.InvariantCulture);
            }
            return ergebnis;
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

        /// <summary>
        /// Die BERECHNUNGSART der aktiven Speichervariante eines Projekts, in der
        /// Sprache des Anwenders - oder <c>null</c>, wenn das Projekt keine aktive
        /// Variante fuehrt.
        /// </summary>
        /// <remarks>
        /// Sie traegt die RUECKFRAGE des Knopfes „In Variante uebernehmen" (LS-E-1 (a)):
        /// Fuehrt die Variante bereits die Lastspitzenkappung, wird ohne Frage
        /// geschrieben; fuehrt sie eine ANDERE Art, wird gefragt, bevor der Anwender
        /// seinen Rechenweg ungewollt austauscht.
        /// </remarks>
        /// <param name="idProjekt">Das Projekt; 0 = keines.</param>
        public static string AktiveBerechnungsart(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            try
            {
                StromspeicherVarianteModel v =
                    new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(idProjekt);
                if (v == null) return null;
                return SpeicherAnzeigeCtrl.BerechnungsartText(v.Berechnungsart);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Berechnungsart der Speichervariante konnte nicht " +
                                  "gelesen werden: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// SCHREIBT das Ergebnis der Maske „Lastspitzenkappung" in die AKTIVE
        /// Speichervariante des Projekts (Entscheid LS-E-1 (a)): Berechnungsart,
        /// Zielschwelle und Adaptiv-Flag in einem Zug.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der Ausgang der Maske.</b> Bis dahin war die Lastspitzenkappung eine
        /// Rechnung OHNE Weg in den Projektlauf: Die Maske zeigte ihr Ergebnis und
        /// vergass es. Dieser Knopf ist die Bruecke - danach rechnet der Projektlauf
        /// genau die Kappung, die der Anwender hier gefunden hat.
        /// </para>
        /// <para>
        /// <b>Ohne aktive Variante kein Schreiben.</b> Der Controller zieht sie mit
        /// <c>AktiveVarianteSicherstellen</c> nach (dieselbe Selbstheilung wie im
        /// Parameterreiter); findet er keine Speicheranlage, liefert er <c>false</c>,
        /// und der Aufrufer meldet es.
        /// </para>
        /// <para>
        /// Im ADAPTIVEN Fall wird die erreichte Schwelle MITGESCHRIEBEN, obwohl der Lauf
        /// sie dann nicht liest: Sie ist die Zahl, die der Anwender vor sich hat, und
        /// wer das Haekchen spaeter wegnimmt, findet sie vor statt eines leeren Feldes.
        /// </para>
        /// </remarks>
        /// <param name="idProjekt">Das Projekt; 0 = keines.</param>
        /// <param name="zielKw">Die Zielschwelle [kW]; 0 oder kleiner = keine.</param>
        /// <param name="adaptiv">Die Schwelle zieht sich selbst nach.</param>
        /// <returns><c>true</c>, wenn die Variante geschrieben wurde.</returns>
        public static bool InVarianteUebernehmen(int idProjekt, double zielKw, bool adaptiv)
        {
            if (idProjekt <= 0) return false;

            try
            {
                StromspeicherVarianteCtrl ctrl = new StromspeicherVarianteCtrl();
                StromspeicherVarianteModel v = ctrl.AktiveVarianteSicherstellen(idProjekt);
                if (v == null) return false;

                v.Berechnungsart = DbWerte.SP_BERECHNUNG_PEAKSHAVING;
                v.PeakZiel_kW = zielKw > 0.0 ? (double?)zielKw : null;
                v.PeakZiel_Adaptiv = adaptiv;

                // Der Excel-Kompatibilitaetsmodus gehoert der Dauernutzung (Fachkonzept
                // 5.2) - er bliebe sonst still gesetzt und die Maske boete ihn nicht an.
                v.Kompatibilitaetsmodus = false;

                return ctrl.Update(v);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Das Peak-Ziel konnte nicht in die Speichervariante " +
                                  "uebernommen werden: " + ex.Message);
                return false;
            }
        }

        private static string Text(DataTable dt, DataRow row, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return "";
            object v = row[spalte];
            return v != DBNull.Value && v != null ? v.ToString() : "";
        }
    }
}
