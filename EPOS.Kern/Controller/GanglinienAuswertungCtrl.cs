using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Ganglinie als Stundenreihe samt ihren drei Kennzahlen</b>
    /// (iU9-W12, Anwenderwunsch <b>W12‑E‑2</b> der Windows-Abnahme vom 05.09.2026;
    /// seit dem Anwenderwunsch <b>W9‑E‑3</b> vom selben Tag auch für den
    /// Wärmebedarf).
    ///
    /// <para><b>Die Reihe liegt in kW</b> — so, wie sie in der Datenbank steht und wie
    /// der Lauf sie liest. Daraus fallen Jahresarbeit (MWh), Spitze (kW) und die
    /// Vollbenutzungsstunden (h/a).</para>
    /// </summary>
    internal sealed class GanglinienAuswertung
    {
        /// <summary>Wurde die Ganglinie gefunden und trägt sie Werte?</summary>
        internal bool Erfolgreich;

        /// <summary>Der Name, unter dem die Ganglinie geführt wird.</summary>
        internal string Bezeichner = "";

        /// <summary>
        /// Die <b>8 760 Stundenwerte in kW</b>. Eine Viertelstundenreihe ist hier
        /// bereits verdichtet (siehe <see cref="GanglinienAuswertungCtrl"/>).
        /// </summary>
        internal double[] Stundenwerte = new double[0];

        /// <summary>Die Jahresarbeit in <b>MWh</b> — Σ der Stundenleistungen ÷ 1 000.</summary>
        internal double JahresarbeitMwh;

        /// <summary>
        /// Die <b>Spitze der GEZEIGTEN Stundenreihe</b> in kW — also genau die
        /// 100 %-Linie des Bildes: <c>ChartRenderer.GanglinieNormiert</c> normiert auf
        /// den Höchstwert der gezeichneten Reihe.
        ///
        /// <para><b>Nicht zu verwechseln mit <c>Strombedarf_Max</c> des Laufs.</b> Der
        /// misst die Spitze im VIERTELSTUNDENraster über alle Ganglinien und Profile
        /// eines Projekts zusammen; hier steht die Spitze EINER Ganglinie in dem
        /// Raster, in dem sie gezeichnet wird. Bei einer Viertelstundenreihe ist die
        /// Stundenspitze naturgemäß die kleinere Zahl — die Kennzahl gehört zum Bild,
        /// nicht zum Lauf.</para>
        /// </summary>
        internal double SpitzeKw;

        /// <summary>
        /// Die Vollbenutzungsstunden [h/a] — Jahresarbeit durch Spitze. Ohne Spitze
        /// gibt es sie nicht (<c>null</c>), statt durch null zu teilen; dieselbe Regel
        /// wie in <see cref="GebaeudeBedarfErgebnis.VollbenutzungsstundenH"/>.
        /// </summary>
        internal double? VollbenutzungsstundenH
            => SpitzeKw > 0 ? JahresarbeitMwh * 1000.0 / SpitzeKw : (double?)null;
    }

    /// <summary>
    /// <b>Welche Tabellen eine Ganglinienart führt</b> (iU9‑W9, Anwenderwunsch
    /// <b>W9‑E‑3</b> vom 05.09.2026).
    ///
    /// <para><b>Wozu.</b> Strom und Wärme unterscheiden sich in DREI Tabellennamen und
    /// in der Frage, wie ein Bezeichner zur Kopf-Id wird — im Rechenweg selbst in
    /// nichts. Ohne diese Ausprägung stünde <see cref="GanglinienAuswertungCtrl"/>
    /// zweimal im Haus, und die zwei Fassungen liefen beim ersten Schemawechsel
    /// auseinander. Genau derselbe Zuschnitt wie <c>KatalogImportProfil</c> (W13)
    /// und <c>KatalogBrowserProfil</c> (W14a): die Ausprägung sind DATEN.</para>
    /// </summary>
    internal sealed class GanglinienQuelle
    {
        private GanglinienQuelle(Zeitreihenart art, string kopfStamm,
                                 string datenStamm, string datenProjekt,
                                 Func<string, int> stammId)
        {
            Art = art;
            KopfStamm = kopfStamm;
            DatenStamm = datenStamm;
            DatenProjekt = datenProjekt;
            StammId = stammId;
        }

        /// <summary>Welcher der drei Zeitreihenkataloge (Stufe S3.2).</summary>
        internal Zeitreihenart Art { get; }

        /// <summary>Die KOPFtabelle des Auslieferungskatalogs (<c>Tab_*_STAMM</c>).</summary>
        internal string KopfStamm { get; }

        /// <summary>Die Werttabelle des Auslieferungskatalogs.</summary>
        internal string DatenStamm { get; }

        /// <summary>Die Werttabelle der Projektkopien.</summary>
        internal string DatenProjekt { get; }

        /// <summary>Der Bezeichner → die Kopf-Id im Katalog; <c>0</c> = es gibt ihn nicht.</summary>
        internal Func<string, int> StammId { get; }

        /// <summary>Die Stromganglinien (<c>Tab_Stromganglinie*</c>).</summary>
        internal static readonly GanglinienQuelle Strom = new GanglinienQuelle(
            Zeitreihenart.Stromganglinie,
            StromganglinieStammCtrl.HEAD_STAMM,
            StromganglinieStammCtrl.DATA_STAMM,
            StromganglinieStammCtrl.DATA_PROJ,
            name => new StromganglinieStammCtrl().GetStammId(name));

        /// <summary>Der externe Wärmebedarf (<c>Tab_Waermebedarf*</c>).</summary>
        internal static readonly GanglinienQuelle Waermebedarf = new GanglinienQuelle(
            Zeitreihenart.Waermebedarf,
            WaermebedarfStammCtrl.HEAD_STAMM,
            WaermebedarfStammCtrl.DATA_STAMM,
            WaermebedarfStammCtrl.DATA_PROJ,
            name => new WaermebedarfStammCtrl().GetStammId(name));

        /// <summary>
        /// Die Solarthermieganglinie (<c>Tab_Solarganglinie*</c>) — seit Stufe
        /// <b>S3.2</b> des <c>Konzept_Katalogfilter</c>.
        ///
        /// <para><b>Sie hat keine Grafik im Dialog</b> (anders als Strom und
        /// Wärmebedarf); gebraucht wird sie für die zwei Spalten Jahresarbeit und
        /// Spitze der Katalogliste. Sie steht trotzdem HIER und nicht als vierte
        /// Sonderabfrage in <c>SolarganglinieStammCtrl</c>: Es gibt EINEN Leseweg für
        /// eine Ganglinie, und der ist dieser.</para>
        /// </summary>
        internal static readonly GanglinienQuelle Solarganglinie = new GanglinienQuelle(
            Zeitreihenart.Solarganglinie,
            SolarganglinieStammCtrl.HEAD_STAMM,
            SolarganglinieStammCtrl.DATA_STAMM,
            SolarganglinieStammCtrl.DATA_PROJ,
            name => new SolarganglinieStammCtrl().GetStammId(name));

        /// <summary>Die Auspraegung zu einer <see cref="Zeitreihenart"/> (Stufe S3.2).</summary>
        internal static GanglinienQuelle Zu(Zeitreihenart art)
        {
            switch (art)
            {
                case Zeitreihenart.Stromganglinie: return Strom;
                case Zeitreihenart.Solarganglinie: return Solarganglinie;
                default:                           return Waermebedarf;
            }
        }
    }

    /// <summary>
    /// <b>Die zwei Kennzahlen EINER Ganglinie für die Katalogliste</b> (Stufe
    /// <b>S3.2</b>, Konzept_Katalogfilter 4.10).
    /// </summary>
    /// <param name="Stunden">
    /// Wie viele Stundenwerte die Reihe ergibt — 8 760 bei einer brauchbaren Reihe.
    /// Alles andere heißt: Die Reihe passt nicht ins Raster des Rechenkerns, und
    /// beide Kennzahlen bleiben Leerwerte (dieselbe Regel wie
    /// <c>GanglinienAuswertungCtrl.AufStunden</c>).
    /// </param>
    /// <param name="JahresarbeitMwh">Σ der Stundenleistungen [kW] ÷ 1 000.</param>
    /// <param name="SpitzeKw">Der Höchstwert der STUNDENreihe.</param>
    internal readonly struct GanglinienKennzahl
    {
        internal GanglinienKennzahl(int stunden, double jahresarbeitMwh, double spitzeKw)
        {
            Stunden = stunden;
            JahresarbeitMwh = jahresarbeitMwh;
            SpitzeKw = spitzeKw;
        }

        internal int Stunden { get; }
        internal double JahresarbeitMwh { get; }
        internal double SpitzeKw { get; }

        /// <summary>Passt die Reihe ins Stundenraster des Rechenkerns?</summary>
        internal bool Brauchbar => Stunden == GanglinienAuswertungCtrl.STUNDEN_JAHR;
    }

    /// <summary>
    /// <b>Der Leseweg hinter der Grafik der Ganglinien-Dialoge</b>
    /// (iU9-W12, Anwenderwunsch <b>W12‑E‑2</b>; seit <b>W9‑E‑3</b> auch für den
    /// Dialog „Wärmebedarf Extern").
    ///
    /// <para><b>Was er tut.</b> Er holt die Werte einer Ganglinie — wahlweise aus dem
    /// KATALOG (<c>…Daten_STAMM</c>) oder aus der PROJEKTKOPIE (<c>…Daten</c>) —,
    /// bringt sie auf das Stundenraster und rechnet die drei Kennzahlen. Mehr nicht:
    /// <b>gelesen, nicht geschrieben</b>, der Referenzlauf ist unberührt.</para>
    ///
    /// <para><b>Die Verdichtung ist KEINE zweite Rechnung.</b> Eine Reihe mit 35 040
    /// Viertelstundenwerten geht durch
    /// <see cref="SimulationControl.Viertelstunden_zu_Stundenwerte_Mittelwert"/> —
    /// dieselbe Methode, die der Lauf für seine Stundenausgaben benutzt und die auch
    /// <c>ZeitreihenExtraktor</c> ruft. Eine eigene Mittelwertschleife stünde sonst
    /// zum zweiten Mal im Haus und liefe beim nächsten Schemawechsel auseinander.
    /// Der Preis ist ein <see cref="SimulationControl"/>-Objekt je Verdichtung; es
    /// entsteht nur bei einer Viertelstundenreihe und wird sofort wieder frei.</para>
    ///
    /// <para><b>Warum die Werte als <c>double</c> gelesen werden.</b> Genau so liest
    /// sie der Lauf (<c>SimulationStrombedarf</c>: <c>Stromganglinie[index] =
    /// (double)wert</c>, <c>SimulationWaermebedarf</c>: <c>ganglinie_roh[index] =
    /// (double)wert</c>). Wer sie hier in <c>double</c> führte, zeigte im Dialog eine
    /// Zahl, die der Lauf so nie sieht.</para>
    ///
    /// <para><b>Das Raster ergibt sich aus der WERTZAHL</b>, nicht aus dem Feld
    /// <c>Zeitinterval</c> — beim Wärmebedarf gibt es dieses Feld überhaupt nicht
    /// (<c>SimulationWaermebedarf</c> leitet es dort ebenfalls aus der Wertzahl ab).
    /// Der Lauf hält beides gegeneinander und bricht ab, wenn es nicht zusammenpasst
    /// (<c>IMPORT_GANGLINIE_RASTER_PASST_NICHT</c>); eine ANZEIGE soll auch einen
    /// Altbestand noch zeigen können, dessen Kennzeichen nicht stimmt. Was weder
    /// 8 760 noch 35 040 Werte hat, gilt als unbrauchbar und liefert
    /// <see cref="GanglinienAuswertung.Erfolgreich"/> = <c>false</c>.</para>
    /// </summary>
    internal static class GanglinienAuswertungCtrl
    {
        /// <summary>Das feste Stundenraster des Rechenkerns.</summary>
        internal const int STUNDEN_JAHR = 8760;

        /// <summary>Das Viertelstundenraster der Engine.</summary>
        internal const int VIERTELSTUNDEN_JAHR = STUNDEN_JAHR * 4;

        /// <summary>
        /// Die Auswertung eines KATALOGsatzes über seinen Bezeichner — der Weg der
        /// rechten Spalte der Dialoge.
        /// </summary>
        internal static GanglinienAuswertung AusKatalog(GanglinienQuelle quelle, string bezeichner)
        {
            var ergebnis = new GanglinienAuswertung { Bezeichner = bezeichner ?? "" };
            if (quelle == null || string.IsNullOrEmpty(bezeichner)) return ergebnis;

            int id = quelle.StammId(bezeichner);
            if (id <= 0) return ergebnis;

            return Auswerten(quelle.DatenStamm, id, bezeichner);
        }

        /// <summary>
        /// Die Auswertung einer PROJEKTKOPIE über die Kopf-Id — der Weg der linken
        /// Spalte der Dialoge.
        ///
        /// <para><b>Der Rückfall auf den Katalog ist der Normalfall, kein Notnagel:</b>
        /// Eine im Dialog eben erst zugeordnete Zeile trägt noch KEINE Projektkopie
        /// (Id = 0 bzw. der Zähler ab <c>StartIndex</c>) — die legt erst
        /// <c>ApplyGanglinieToProjekt</c> beim Speichern an. Gezeigt wird dann der
        /// Katalogsatz, aus dem die Kopie entstehen wird; es sind dieselben
        /// Werte.</para>
        /// </summary>
        /// <param name="quelle">Die Ausprägung (Strom oder Wärmebedarf).</param>
        /// <param name="idGanglinie">Die Kopf-Id der Projektkopie; 0 = es gibt noch keine.</param>
        /// <param name="bezeichner">Der Name — zugleich der Rückfallweg über den Katalog.</param>
        internal static GanglinienAuswertung AusProjekt(GanglinienQuelle quelle, int idGanglinie,
                                                        string bezeichner)
        {
            if (quelle == null) return new GanglinienAuswertung { Bezeichner = bezeichner ?? "" };
            if (idGanglinie <= 0) return AusKatalog(quelle, bezeichner);

            GanglinienAuswertung ergebnis =
                Auswerten(quelle.DatenProjekt, idGanglinie, bezeichner);

            return ergebnis.Erfolgreich ? ergebnis : AusKatalog(quelle, bezeichner);
        }

        // ==================================================================
        //  S3.2 - die Kennzahlen des GANZEN Katalogs in EINER Abfrage
        // ==================================================================

        /// <summary>
        /// <b>Jahresarbeit und Spitze ALLER Ganglinien eines Katalogs</b>
        /// (Anwenderentscheid W14a-E-10, Konzept_Katalogfilter 4.10 und Stufe S3.2) —
        /// EINE Gruppenabfrage beim Aufbau der Liste, nicht eine je Zeile.
        ///
        /// <para><b>Warum das noetig ist.</b> Die zwei fachlich richtigen
        /// Filtergroessen einer Zeitreihe stehen nicht am Kopfsatz, sondern in der
        /// Wertetabelle: 78 840 Zeilen beim Strom, 35 040 beim Waermebedarf. Je Zeile
        /// zu fragen hiesse, die ganze Tabelle so oft zu lesen, wie der Katalog
        /// Saetze hat — dasselbe, was <see cref="WPStammCtrl.KatalogZeilen"/> fuer die
        /// Vorlaufgrenzen der Waermepumpe schon vermeidet.</para>
        ///
        /// <para><b>Die Abfrage rechnet die VERDICHTUNG mit</b>, und das ist der Punkt,
        /// an dem eine einfache Gruppierung nicht genuegt haette. Eine
        /// Viertelstundenreihe wird im Dialog ueber
        /// <c>Viertelstunden_zu_Stundenwerte_Mittelwert</c> auf Stunden gebracht; ihre
        /// Spitze ist danach der Hoechstwert der STUNDENmittel und nicht der der
        /// Viertelstundenwerte (gemessen: 1 513,5 kW statt 4 590 kW bei der
        /// Ganglinie 21 der Testdatenbank). Der innere Teil der Abfrage bildet deshalb
        /// zuerst Stunden — die Stundennummer ist
        /// <c>(Platz − 1) · 8760 ÷ Wertzahl</c>, was bei 8 760 Werten jeden Wert
        /// allein laesst und bei 35 040 je vier zusammenfasst —, der aeussere summiert
        /// und maximiert darueber. Damit stehen in der Liste dieselben Zahlen, die die
        /// Grafik der Dialoge seit W9-E-3/W12-E-2 zeigt; es gibt keinen zweiten
        /// Rechenweg.</para>
        ///
        /// <para><b>Der Platz kommt aus <c>ROW_NUMBER</c>, nicht aus der Id.</b> Eine
        /// Luecke in den Ids — ein geloeschter und neu eingelesener Satz — verschoebe
        /// sonst die Stundengrenzen. <c>ORDER BY ID</c> ist dabei dieselbe Bedingung,
        /// die <c>CopyGanglinieToProjekt</c> und der Leseweg unten stellen: Die
        /// Zeitreihe steht in Einfuegereihenfolge.</para>
        ///
        /// <para><b>Gelesen, nicht gerechnet.</b> Der Rechenweg der Simulation ist
        /// unberuehrt; das hier ist eine Anzeige.</para>
        /// </summary>
        /// <param name="quelle">Die Ausprägung (Strom, Wärmebedarf, Solarganglinie).</param>
        /// <param name="ausProjekt">
        /// <c>true</c> liest die Projektkopien statt des Auslieferungskatalogs.
        /// </param>
        internal static IReadOnlyDictionary<int, GanglinienKennzahl> Kennzahlen(
            GanglinienQuelle quelle, bool ausProjekt = false)
        {
            var werte = new Dictionary<int, GanglinienKennzahl>();
            if (quelle == null) return werte;

            string tabelle = ausProjekt ? quelle.DatenProjekt : quelle.DatenStamm;

            // Der Tabellenname kommt aus GanglinienQuelle und nicht aus einer Eingabe.
            DataTable dt = StilleDb.Tabelle(
                "SELECT ID_Ganglinie, COUNT(*) AS Stunden, SUM(Stundenwert) AS Summe, " +
                "MAX(Stundenwert) AS Spitze FROM (" +
                "  SELECT ID_Ganglinie, AVG(Wert) AS Stundenwert FROM (" +
                "    SELECT ID_Ganglinie, Wert, " +
                "           ((ROW_NUMBER() OVER (PARTITION BY ID_Ganglinie ORDER BY ID) - 1) * " +
                STUNDEN_JAHR.ToString(CultureInfo.InvariantCulture) + ") " +
                "           / COUNT(*) OVER (PARTITION BY ID_Ganglinie) AS Stunde " +
                "    FROM [" + tabelle + "]" +
                "  ) GROUP BY ID_Ganglinie, Stunde" +
                ") GROUP BY ID_Ganglinie");
            if (dt == null) return werte;

            foreach (DataRow r in dt.Rows)
            {
                int id = Katalogfeld.Ganzzahl(r, "ID_Ganglinie");
                if (id <= 0) continue;

                werte[id] = new GanglinienKennzahl(
                    Katalogfeld.Ganzzahl(r, "Stunden"),
                    (Katalogfeld.Zahl(r, "Summe") ?? 0.0) / 1000.0,
                    Katalogfeld.Zahl(r, "Spitze") ?? 0.0);
            }
            return werte;
        }

        // ==================================================================
        //  Der eine Leseweg
        // ==================================================================

        /// <summary>
        /// Liest die Wertzeilen einer Ganglinie, bringt sie auf Stunden und rechnet die
        /// Kennzahlen.
        /// </summary>
        /// <param name="datentabelle">Die Werttabelle — Katalog oder Projektkopie.</param>
        /// <param name="idGanglinie">Die Kopf-Id in der zugehörigen Kopftabelle.</param>
        /// <param name="bezeichner">Der Anzeigename.</param>
        private static GanglinienAuswertung Auswerten(string datentabelle, int idGanglinie,
                                                      string bezeichner)
        {
            var ergebnis = new GanglinienAuswertung { Bezeichner = bezeichner ?? "" };

            // ORDER BY ID: die Zeitreihe steht in Einfuegereihenfolge - dieselbe
            // Bedingung, die CopyGanglinieToProjekt und KopiereStamm stellen.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Wert FROM " + datentabelle + " WHERE ID_Ganglinie = ? ORDER BY ID",
                new DbParam("@g", DbParamTyp.Integer) { Wert = idGanglinie });

            if (dt == null || dt.Rows.Count == 0) return ergebnis;

            double[] roh = new double[dt.Rows.Count];
            for (int i = 0; i < roh.Length; i++)
            {
                object v = dt.Rows[i][0];
                roh[i] = v != DBNull.Value ? Convert.ToDouble(v) : 0.0;
            }

            double[] stunden = AufStunden(roh);
            if (stunden == null) return ergebnis;

            ergebnis.Stundenwerte = stunden;
            ergebnis.JahresarbeitMwh = Jahresarbeit(stunden);
            ergebnis.SpitzeKw = Hoechstwert(stunden);
            ergebnis.Erfolgreich = true;
            return ergebnis;
        }

        /// <summary>
        /// Eine gelesene Reihe auf dem Stundenraster: 8 760 Werte bleiben, 35 040 werden
        /// über <see cref="SimulationControl.Viertelstunden_zu_Stundenwerte_Mittelwert"/>
        /// verdichtet, alles andere ergibt <c>null</c>.
        /// </summary>
        private static double[] AufStunden(double[] roh)
        {
            if (roh.Length == STUNDEN_JAHR) return roh;
            if (roh.Length != VIERTELSTUNDEN_JAHR) return null;

            return new SimulationControl().Viertelstunden_zu_Stundenwerte_Mittelwert(roh);
        }

        /// <summary>
        /// Σ der Stundenleistungen [kW] × 1 h ÷ 1 000 = MWh. Summiert wird in
        /// <c>double</c>: 8 760 Additionen in <c>double</c> verlieren am Ende Stellen,
        /// die der Anwender abliest.
        /// </summary>
        private static double Jahresarbeit(double[] stunden)
        {
            double summe = 0;
            for (int i = 0; i < stunden.Length; i++) summe += stunden[i];
            return summe / 1000.0;
        }

        /// <summary>Der Höchstwert der Reihe — wie <c>Maximaler_Strombedarf</c>.</summary>
        private static double Hoechstwert(double[] werte)
        {
            double max = 0;
            for (int i = 0; i < werte.Length; i++) if (max < werte[i]) max = werte[i];
            return max;
        }
    }
}
