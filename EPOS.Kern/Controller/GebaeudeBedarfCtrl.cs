using System;
using System.Data;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Wärmebedarf EINES Gebäudes</b> (iU9-W9.8, Anwenderwunsch <b>W9‑E‑2</b> vom
    /// 05.09.2026) — die Zahlen hinter dem Knopf „Simulation…" des Gebäudedialogs.
    ///
    /// <para><b>Nur HEIZUNG.</b> Kein Brauchwasser, keine Prozesswärme, keine
    /// Netzverluste und keine Summe über die Bedarfsarten — der Anwender hat das
    /// ausdrücklich so gewünscht („ohne Brauchwasser und ohne gesamt"). Was hier
    /// herauskommt, ist genau der Anteil, den dieses Gebäude im Lauf in den HEIZKANAL
    /// legt.</para>
    ///
    /// <para><b>Die Reihe liegt in kW</b> — dieselbe Umrechnung wie im Lauf
    /// (<c>BhkwPlan.WattToKw</c> auf dem Heizkanal). Daraus fallen Jahressumme (MWh),
    /// Höchstlast (kW) und die zwölf Monatswerte (MWh).</para>
    /// </summary>
    internal sealed class GebaeudeBedarfErgebnis
    {
        /// <summary>Wurde die Zuordnung gefunden und gerechnet?</summary>
        internal bool Erfolgreich;

        /// <summary>Der Gebäudename der Projektkopie.</summary>
        internal string Name = "";

        /// <summary>Die 8 760 Stundenwerte der Heizwärme in <b>kW</b>.</summary>
        internal float[] Stundenwerte = new float[8760];

        /// <summary>Die Jahressumme in <b>MWh</b>.</summary>
        internal double HeizwaermeMwh;

        /// <summary>Die höchste Stundenlast in <b>kW</b>.</summary>
        internal double MaxLastKw;

        /// <summary>Die zwölf Monatssummen in <b>MWh</b>.</summary>
        internal float[] MonatswerteMwh = new float[12];

        /// <summary>
        /// Die Vollbenutzungsstunden [h/a] — Jahresarbeit durch Höchstlast. Bei
        /// Höchstlast 0 gibt es sie nicht (<c>null</c>), statt durch null zu teilen.
        /// </summary>
        internal double? VollbenutzungsstundenH
            => MaxLastKw > 0 ? HeizwaermeMwh * 1000.0 / MaxLastKw : (double?)null;
    }

    /// <summary>
    /// <b>Die Bedarfsrechnung für EIN Gebäude</b> (iU9-W9.8) — der Rechenweg hinter dem
    /// Knopf „Simulation…" im Detailblock „Gebäude: Verbrauch"
    /// (<c>EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor</c>).
    ///
    /// <para><b>Das Vorbild.</b> Der WinForms-Bestand kannte KEINEN solchen Knopf: Die
    /// gelöschte <c>Form_Gebaeude</c> führte im Detailblock nur „Ändern", und
    /// <c>Form_Simulation_Kurz</c> (mit iF29 stillgelegt) rechnete das GANZE Projekt —
    /// Konfiguration, Wärme- und Strombedarf, Kaskade. Der Anwender wünscht die Auskunft
    /// je Gebäude, analog zu dem, was der Bedarfsreiter der Ergebnisseite für das Projekt
    /// zeigt. Neu ist also die AUSKUNFT, nicht die Rechnung.</para>
    ///
    /// <para><b>Es ist dieselbe Rechnung, kein Zwilling.</b> Gerechnet wird über
    /// <see cref="SimulationWaermebedarf.KlimakalenderLesen"/> und
    /// <see cref="SimulationWaermebedarf.HeizwaermeEinesGebaeudes"/> — dieselben zwei
    /// Methoden, die <c>Waermebedarf_berechnen</c> in seiner Gebäudeschleife ruft. Damit
    /// gilt: <b>Σ über alle Gebäude eines Projekts = <c>Waermebedarf_Gebaeude_Gesamt</c>
    /// des Laufs</b>, und bei einem Projekt mit genau EINEM Gebäude sind beide Zahlen
    /// gleich. Der Nachweis steht in
    /// <c>EPOS.Kern.Tests/GebaeudeBedarfCtrlTests.cs</c>.</para>
    ///
    /// <para><b>Was NICHT eingeht</b>, weil es nicht am Gebäude hängt: die externen
    /// Wärmelastgänge (<c>Z_ProjektWaermebedarf</c>), Brauchwasser- und Prozessprofile
    /// und die anteilig verteilten Netzverluste. Die Zahl des Dialogs ist deshalb der
    /// reine Gebäudeanteil und nicht der ganze Heizkanal des Laufs.</para>
    ///
    /// <para><b>Gelesen, nicht geschrieben.</b> Der Controller fasst die Datenbank nur
    /// lesend an; der Referenzlauf ist unberührt.</para>
    /// </summary>
    internal static class GebaeudeBedarfCtrl
    {
        /// <summary>Das feste Stundenraster des Rechenkerns.</summary>
        private const int STUNDEN_JAHR = 8760;

        /// <summary>
        /// Rechnet die Heizwärme der Zuordnung <paramref name="idZ"/> im Projekt
        /// <paramref name="idProjekt"/>.
        /// </summary>
        /// <param name="idProjekt">Das Projekt (<c>Z_ProjektGebaeude.ID_Projekt</c>).</param>
        /// <param name="idKlimaregion">Die Klimaregion des Projekts
        /// (<c>Tab_Projekt.ID_Klimaregion</c>). 0 heißt „keine" — dann gibt es kein
        /// Ergebnis, wie im Lauf.</param>
        /// <param name="idZ">Der Schlüssel der ZUORDNUNG (<c>Z_ProjektGebaeude.ID</c>) —
        /// nicht die Stamm-Id: Zwei gleiche Gebäude im Projekt teilen sich eine
        /// Stamm-Id.</param>
        internal static GebaeudeBedarfErgebnis Rechnen(int idProjekt, int idKlimaregion, int idZ)
        {
            var ergebnis = new GebaeudeBedarfErgebnis();
            if (idProjekt <= 0 || idKlimaregion <= 0 || idZ <= 0) return ergebnis;

            int idTabGebaeude = TabGebaeudeId(idZ);
            if (idTabGebaeude == 0) return ergebnis;

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);

            ProjektGebaeudeModel gebaeude = null;
            for (int i = 0; i < ctrl.rows; i++)
                if (ctrl.items[i].ID_Gebaeude == idTabGebaeude) { gebaeude = ctrl.items[i]; break; }

            if (gebaeude == null) return ergebnis;

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(idKlimaregion);

            // Der Merkplatz 0 in HeizwaermebedarfGeb - eine Rechnung fuer EIN Gebaeude
            // braucht keinen Rang, siehe HeizwaermeEinesGebaeudes.
            var werte = new float[STUNDEN_JAHR];
            if (!sim.HeizwaermeEinesGebaeudes(gebaeude, 0, werte)) return ergebnis;

            // Dieselbe Umrechnung wie im Lauf: der Heizkanal geht als WATT in die
            // Schleife und wird danach EINMAL nach kW gebracht.
            WPPlan.Core.BhkwPlan.WattToKw(werte);

            ergebnis.Name = gebaeude.Gebaeudename ?? "";
            ergebnis.Stundenwerte = werte;

            // ZEICHENGLEICH zum Lauf: dort steht "kanalHeizung.Sum() / 1000" - eine
            // float-Summe durch eine GANZE Zahl, also eine float-Division. Ein
            // "/ 1000.0" waere eine double-Division und ergaebe eine andere neunte
            // Stelle; der Anwender legt die zwei Zahlen nebeneinander.
            ergebnis.HeizwaermeMwh = werte.Sum() / 1000;
            ergebnis.MaxLastKw = Hoechstwert(werte);
            WPPlan.Core.BhkwPlan.MonatsSumme(werte, ergebnis.MonatswerteMwh,
                                             sim.mo_anfang, sim.mo_ende);
            ergebnis.Erfolgreich = true;
            return ergebnis;
        }

        /// <summary>
        /// Die Zeile in <c>Tab_Gebaeude</c>, die an dieser Zuordnung hängt.
        /// <c>Tab_Gebaeude.ID_ProjektGebaeude</c> IST der Verweis auf
        /// <c>Z_ProjektGebaeude.ID</c> (derselbe Weg wie
        /// <c>Z_ProjGebCtrl.LiesProjekt</c>); die Sicht <c>Abfrage_Projektgebaeude</c>
        /// gibt dagegen nur <c>Tab_Gebaeude.ID</c> aus, und genau die braucht der
        /// Vergleich — sie ist auch der Schlüssel, mit dem der Lauf die Tagesverteilung
        /// sucht.
        /// </summary>
        private static int TabGebaeudeId(int idZ)
        {
            const string sql = "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?";

            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idZ));
            if (dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        /// <summary>Der Höchstwert der Stundenreihe — wie <c>Maximaler_Waermebedarf</c>.</summary>
        private static double Hoechstwert(float[] werte)
        {
            float max = 0;
            for (int i = 0; i < werte.Length; i++) if (max < werte[i]) max = werte[i];
            return max;
        }
    }
}
