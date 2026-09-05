using System;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Stromganglinie als Stundenreihe samt ihren drei Kennzahlen</b>
    /// (iU9-W12, Anwenderwunsch <b>W12‑E‑2</b> der Windows-Abnahme vom 05.09.2026:
    /// „Stelle die importierte Stromganglinie als Grafik dar").
    ///
    /// <para><b>Die Reihe liegt in kW</b> — so, wie sie in der Datenbank steht und wie
    /// der Lauf sie liest. Daraus fallen Jahresarbeit (MWh), Spitze (kW) und die
    /// Vollbenutzungsstunden (h/a).</para>
    /// </summary>
    internal sealed class StromganglinieAuswertung
    {
        /// <summary>Wurde die Ganglinie gefunden und trägt sie Werte?</summary>
        internal bool Erfolgreich;

        /// <summary>Der Name, unter dem die Ganglinie geführt wird.</summary>
        internal string Bezeichner = "";

        /// <summary>
        /// Die <b>8 760 Stundenwerte in kW</b>. Eine Viertelstundenreihe ist hier
        /// bereits verdichtet (siehe <see cref="StromganglinieAuswertungCtrl"/>).
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
    /// <b>Der Leseweg hinter der Grafik des Dialogs „Stromganglinien"</b>
    /// (iU9-W12, Anwenderwunsch <b>W12‑E‑2</b> vom 05.09.2026).
    ///
    /// <para><b>Was er tut.</b> Er holt die Werte einer Ganglinie — wahlweise aus dem
    /// KATALOG (<c>Tab_Stromganglinie_STAMM</c> + <c>…Daten_STAMM</c>) oder aus der
    /// PROJEKTKOPIE (<c>Tab_Stromganglinie</c> + <c>…Daten</c>) —, bringt sie auf das
    /// Stundenraster und rechnet die drei Kennzahlen. Mehr nicht: <b>gelesen, nicht
    /// geschrieben</b>, der Referenzlauf ist unberührt.</para>
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
    /// (float)wert</c>). Wer sie hier in <c>double</c> führte, zeigte im Dialog eine
    /// Zahl, die der Lauf so nie sieht.</para>
    ///
    /// <para><b>Das Raster ergibt sich aus der WERTZAHL</b>, nicht aus dem Feld
    /// <c>Zeitinterval</c>. Der Lauf hält beides gegeneinander und bricht ab, wenn es
    /// nicht zusammenpasst (<c>IMPORT_GANGLINIE_RASTER_PASST_NICHT</c>); eine
    /// ANZEIGE soll auch einen Altbestand noch zeigen können, dessen Kennzeichen
    /// nicht stimmt. Was weder 8 760 noch 35 040 Werte hat, gilt als unbrauchbar und
    /// liefert <see cref="StromganglinieAuswertung.Erfolgreich"/> = <c>false</c>.</para>
    /// </summary>
    internal static class StromganglinieAuswertungCtrl
    {
        /// <summary>Das feste Stundenraster des Rechenkerns.</summary>
        internal const int STUNDEN_JAHR = 8760;

        /// <summary>Das Viertelstundenraster der Engine.</summary>
        internal const int VIERTELSTUNDEN_JAHR = STUNDEN_JAHR * 4;

        /// <summary>
        /// Die Auswertung eines KATALOGsatzes über seinen Bezeichner — der Weg der
        /// rechten Spalte des Dialogs.
        /// </summary>
        internal static StromganglinieAuswertung AusKatalog(string bezeichner)
        {
            var ergebnis = new StromganglinieAuswertung { Bezeichner = bezeichner ?? "" };
            if (string.IsNullOrEmpty(bezeichner)) return ergebnis;

            int id = new StromganglinieStammCtrl().GetStammId(bezeichner);
            if (id <= 0) return ergebnis;

            return Auswerten(StromganglinieStammCtrl.DATA_STAMM, id, bezeichner);
        }

        /// <summary>
        /// Die Auswertung einer PROJEKTKOPIE über <c>Tab_Stromganglinie.ID</c> — der
        /// Weg der linken Spalte des Dialogs.
        ///
        /// <para><b>Der Rückfall auf den Katalog ist der Normalfall, kein Notnagel:</b>
        /// Eine im Dialog eben erst zugeordnete Zeile trägt noch KEINE Projektkopie
        /// (<c>GanglinieId</c> = 0, Zähler ab <c>StromganglinieDialog.StartIndex</c>) —
        /// die legt erst <c>ApplyGanglinieToProjekt</c> beim Speichern an. Gezeigt wird
        /// dann der Katalogsatz, aus dem die Kopie entstehen wird; es sind dieselben
        /// Werte.</para>
        /// </summary>
        /// <param name="idGanglinie">Die Kopf-Id der Projektkopie; 0 = es gibt noch keine.</param>
        /// <param name="bezeichner">Der Name — zugleich der Rückfallweg über den Katalog.</param>
        internal static StromganglinieAuswertung AusProjekt(int idGanglinie, string bezeichner)
        {
            if (idGanglinie <= 0) return AusKatalog(bezeichner);

            StromganglinieAuswertung ergebnis =
                Auswerten(StromganglinieStammCtrl.DATA_PROJ, idGanglinie, bezeichner);

            return ergebnis.Erfolgreich ? ergebnis : AusKatalog(bezeichner);
        }

        // ==================================================================
        //  Der eine Leseweg
        // ==================================================================

        /// <summary>
        /// Liest die Wertzeilen einer Ganglinie, bringt sie auf Stunden und rechnet die
        /// Kennzahlen.
        /// </summary>
        /// <param name="datentabelle">
        /// <c>Tab_StromganglinieDaten_STAMM</c> oder <c>Tab_StromganglinieDaten</c>.
        /// </param>
        /// <param name="idGanglinie">Die Kopf-Id in der zugehörigen Kopftabelle.</param>
        /// <param name="bezeichner">Der Anzeigename.</param>
        private static StromganglinieAuswertung Auswerten(string datentabelle, int idGanglinie,
                                                          string bezeichner)
        {
            var ergebnis = new StromganglinieAuswertung { Bezeichner = bezeichner ?? "" };

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
