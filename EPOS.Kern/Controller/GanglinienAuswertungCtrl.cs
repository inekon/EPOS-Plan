using System;
using System.Data;

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
        internal float[] Stundenwerte = new float[0];

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
        private GanglinienQuelle(string datenStamm, string datenProjekt, Func<string, int> stammId)
        {
            DatenStamm = datenStamm;
            DatenProjekt = datenProjekt;
            StammId = stammId;
        }

        /// <summary>Die Werttabelle des Auslieferungskatalogs.</summary>
        internal string DatenStamm { get; }

        /// <summary>Die Werttabelle der Projektkopien.</summary>
        internal string DatenProjekt { get; }

        /// <summary>Der Bezeichner → die Kopf-Id im Katalog; <c>0</c> = es gibt ihn nicht.</summary>
        internal Func<string, int> StammId { get; }

        /// <summary>Die Stromganglinien (<c>Tab_Stromganglinie*</c>).</summary>
        internal static readonly GanglinienQuelle Strom = new GanglinienQuelle(
            StromganglinieStammCtrl.DATA_STAMM,
            StromganglinieStammCtrl.DATA_PROJ,
            name => new StromganglinieStammCtrl().GetStammId(name));

        /// <summary>Der externe Wärmebedarf (<c>Tab_Waermebedarf*</c>).</summary>
        internal static readonly GanglinienQuelle Waermebedarf = new GanglinienQuelle(
            WaermebedarfStammCtrl.DATA_STAMM,
            WaermebedarfStammCtrl.DATA_PROJ,
            name => new WaermebedarfStammCtrl().GetStammId(name));
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
    /// <para><b>Warum die Werte als <c>float</c> gelesen werden.</b> Genau so liest
    /// sie der Lauf (<c>SimulationStrombedarf</c>: <c>Stromganglinie[index] =
    /// (float)wert</c>, <c>SimulationWaermebedarf</c>: <c>ganglinie_roh[index] =
    /// (float)wert</c>). Wer sie hier in <c>double</c> führte, zeigte im Dialog eine
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

            float[] roh = new float[dt.Rows.Count];
            for (int i = 0; i < roh.Length; i++)
            {
                object v = dt.Rows[i][0];
                roh[i] = v != DBNull.Value ? Convert.ToSingle(v) : 0f;
            }

            float[] stunden = AufStunden(roh);
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
        private static float[] AufStunden(float[] roh)
        {
            if (roh.Length == STUNDEN_JAHR) return roh;
            if (roh.Length != VIERTELSTUNDEN_JAHR) return null;

            return new SimulationControl().Viertelstunden_zu_Stundenwerte_Mittelwert(roh);
        }

        /// <summary>
        /// Σ der Stundenleistungen [kW] × 1 h ÷ 1 000 = MWh. Summiert wird in
        /// <c>double</c>: 8 760 Additionen in <c>float</c> verlieren am Ende Stellen,
        /// die der Anwender abliest.
        /// </summary>
        private static double Jahresarbeit(float[] stunden)
        {
            double summe = 0;
            for (int i = 0; i < stunden.Length; i++) summe += stunden[i];
            return summe / 1000.0;
        }

        /// <summary>Der Höchstwert der Reihe — wie <c>Maximaler_Strombedarf</c>.</summary>
        private static double Hoechstwert(float[] werte)
        {
            float max = 0;
            for (int i = 0; i < werte.Length; i++) if (max < werte[i]) max = werte[i];
            return max;
        }
    }
}
