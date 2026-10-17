using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Projektgebäude als Quelle für „Aus Gebäude übernehmen…“ im Betriebskalender (Konzept Kältebedarfsprofile 7.2,
    /// Welle KBP-1): Feiertagsland und ALLE Ferienzeiträume des Gebäudes in ihrer Reihenfolge — die vier Gebäudespalten,
    /// dann die Ferienliste des gemeinsamen Kalenders. Wie viele davon der Kalender aufnimmt, entscheidet der Kalender.
    /// </summary>
    /// <param name="IdGebaeude">Die Projektkopie in <c>Tab_Gebaeude</c> (nur zur Wahl; sie wird nie am Kalender vermerkt).</param>
    /// <param name="Bezeichner">Der Gebäudename.</param>
    /// <param name="Feiertagsland">Länderkennung oder <c>null</c> (nur die bundeseinheitlichen Feiertage).</param>
    /// <param name="Ferien">Die Ferienzeiträume als Jahrestage im Gemeinjahr.</param>
    public sealed record BetriebskalenderGebaeudequelle(long IdGebaeude, string Bezeichner, string Feiertagsland,
                                                        IReadOnlyList<Ferienzeitraum> Ferien);

    /// <summary>
    /// <b>Liest die Projektgebäude als Quellen eines Betriebskalenders</b> (Konzept Kältebedarfsprofile 7.2, OE-6). Der
    /// Betriebskalender übernimmt daraus eine KOPIE von Feiertagsland und Ferien — nie einen Verweis (Festlegung KP1b 6:
    /// Herkunft nie als ID am Ziel); dieser Leser schreibt nichts.
    ///
    /// <para><b>Die Ferien</b> folgen der Regel der Kalenderbedienung: die Spalten <c>Ferienbeginn/-ende_1…4</c> (eine Grenze
    /// außerhalb 1 … 365, also 0 oder 366, heißt „aus“ und fällt weg), danach die Ferienliste des gemeinsamen Kalenders
    /// (<see cref="Kalendergemeinschaft.GemeinsamLesen"/>, ab Rang 204).</para>
    /// </summary>
    public static class BetriebskalenderGebaeudeCtrl
    {
        /// <summary>Die Gebäude des Projekts nach Name; leer ohne Projekt oder ohne Gebäude.</summary>
        public static List<BetriebskalenderGebaeudequelle> Quellen(int idProjekt)
        {
            var liste = new List<BetriebskalenderGebaeudequelle>();
            if (idProjekt <= 0) return liste;
            bool mitLand = DataRepository.SpalteVorhanden("Tab_Gebaeude", KalenderbedienungSchema.SPALTE_FEIERTAGSLAND);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT g.ID, g.Gebaeudename, " + (mitLand ? "g.Feiertagsland" : "NULL AS Feiertagsland") + ", " +
                "g.Ferienbeginn_1, g.Ferienende_1, g.Ferienbeginn_2, g.Ferienende_2, " +
                "g.Ferienbeginn_3, g.Ferienende_3, g.Ferienbeginn_4, g.Ferienende_4 " +
                "FROM Tab_Gebaeude g INNER JOIN Z_ProjektGebaeude z ON z.ID = g.ID_ProjektGebaeude " +
                "WHERE z.ID_Projekt = ? ORDER BY g.Gebaeudename, g.ID",
                new DbParam("?", idProjekt));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                var ferien = new List<Ferienzeitraum>();
                for (int k = 1; k <= Matrixeingang.FERIENZEITRAEUME; k++)
                {
                    int? von = Jahrestag(r["Ferienbeginn_" + k]);
                    int? bis = Jahrestag(r["Ferienende_" + k]);
                    if (von.HasValue && bis.HasValue) ferien.Add(new Ferienzeitraum(von.Value, bis.Value));
                }
                Kalendergemeinschaft.GemeinsamLesen(new Kalendergemeinschaft.Schluessel(id, null, null, null, null),
                                                    out _, out List<Ferienzeile> weitere);
                ferien.AddRange(weitere.Where(f => Gueltig(f.Beginn) && Gueltig(f.Ende))
                                       .Select(f => new Ferienzeitraum(f.Beginn, f.Ende)));
                liste.Add(new BetriebskalenderGebaeudequelle(
                    id,
                    r["Gebaeudename"] == DBNull.Value ? "" : Convert.ToString(r["Gebaeudename"], CultureInfo.InvariantCulture),
                    Land(r["Feiertagsland"]),
                    ferien));
            }
            return liste;
        }

        /// <summary>Ein Jahrestag 1 … 365; sonst (leer, 0, 366) <c>null</c> = „aus“.</summary>
        private static int? Jahrestag(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            double d = Convert.ToDouble(wert, CultureInfo.InvariantCulture);
            if (double.IsNaN(d)) return null;
            int t = (int)Math.Round(d);
            return Gueltig(t) ? t : (int?)null;
        }

        private static bool Gueltig(int tag) => tag >= 1 && tag <= 365;

        /// <summary>Eine bekannte Länderkennung; eine leere oder fremde gilt als „nur bundeseinheitlich“.</summary>
        private static string Land(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            string k = Convert.ToString(wert, CultureInfo.InvariantCulture)?.Trim();
            return !string.IsNullOrEmpty(k) && Landesfeiertage.BUNDESLAENDER.Contains(k) ? k : null;
        }
    }
}
