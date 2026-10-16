using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Kältezeile, wie sie der Dialog gerade zeigt: Name, Jahressumme [MWh] und Betriebskalender.
    /// </summary>
    public sealed record Kaeltegrafikzeile(string Name, double SummeMwh, int? KalenderId);

    /// <summary>
    /// <b>Die Grafik einer Kältezeile im Dialog der Bedarfsprofile</b> (Konzept Kältebedarfsprofile, Abschnitt 5; Welle
    /// KBP-3b): rechnet je Zeile die Stundenreihe über die Projektvorschau (<see cref="BedarfsVorschauCtrl.ProjektVorschau"/>,
    /// <see cref="ProfilQuelle.Kaelte"/>, mit den Jahressummen des Dialogs) und legt daraus über
    /// <see cref="Kaelteprofilgrafik.Bilder"/> Kennzahlen und Bilder an. Die Woche nach Tagesarten liest das
    /// Wochentagsraster des Projekts (dasselbe wie die Profilroutine) und den Betriebskalender der markierten Zeile.
    /// Schreibt nichts.
    /// </summary>
    internal static class KaelteprofilgrafikCtrl
    {
        /// <summary>
        /// Kennzahlen und Bilder der Zeile <paramref name="markiert"/> unter den <paramref name="zeilen"/>;
        /// <c>null</c>, wenn sie kein Profil liefert.
        /// </summary>
        internal static Kaelteprofilbilder Rechnen(int idProjekt, IReadOnlyList<Kaeltegrafikzeile> zeilen, Kaeltegrafikzeile markiert)
        {
            if (markiert == null || string.IsNullOrEmpty(markiert.Name) || zeilen == null) return null;

            var summen = new Dictionary<string, double>(System.StringComparer.Ordinal);
            foreach (Kaeltegrafikzeile z in zeilen)
                if (z != null && !string.IsNullOrEmpty(z.Name)) summen[z.Name] = z.SummeMwh;

            double[] reiheMarkiert = null;
            var reihen = new List<(string Name, double[] StundeKwh)>();
            foreach (Kaeltegrafikzeile z in zeilen)
            {
                if (z == null || string.IsNullOrEmpty(z.Name)) continue;
                BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Kaelte, idProjekt, new[] { z.Name }, null, summen);
                double[] stunde = v.Erfolgreich ? v.KaelteStunde : null;
                if (stunde == null) continue;
                reihen.Add((z.Name, stunde));
                if (ReferenceEquals(z, markiert) || (reiheMarkiert == null && z.Name == markiert.Name)) reiheMarkiert = stunde;
            }
            if (reiheMarkiert == null) return null;

            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(idProjekt, ProfilBedarf.WOCHENTAG_ALTKONVENTION);
            byte[] arten = null;
            if (markiert.KalenderId is int id && id > 0)
                arten = BetriebskalenderCtrl.Lies(id)?.Tagesarten(raster);

            return Kaelteprofilgrafik.Bilder(reiheMarkiert, reihen, raster.W0, arten);
        }
    }
}
