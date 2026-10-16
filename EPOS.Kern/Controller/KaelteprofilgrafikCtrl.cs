using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Kältezeile, wie sie der Dialog gerade zeigt — der ungespeicherte Arbeitsstand: Name, Jahressumme [MWh],
    /// Betriebskalender (<c>null</c> = keiner) und, sofern im Dialog bearbeitet, Monatsanteile [%] und Wochenprofil
    /// (168 Werte, Montag 0 Uhr zuerst); <c>null</c> = der Stand der Projektkopie.
    /// </summary>
    public sealed record Kaeltegrafikzeile(string Name, double SummeMwh, int? KalenderId,
                                           double[] Monatsanteile = null, double[] Wochenprofil = null);

    /// <summary>
    /// <b>Die Grafik einer Kältezeile im Dialog der Bedarfsprofile</b> (Konzept Kältebedarfsprofile, Abschnitt 5; Welle
    /// KBP-3b): rechnet je Zeile die Stundenreihe über die Projektvorschau (<see cref="BedarfsVorschauCtrl.ProjektVorschau"/>,
    /// <see cref="ProfilQuelle.Kaelte"/>) mit dem Arbeitsstand des Dialogs — Jahressumme, Monatswerte, Wochenprofil und
    /// Betriebskalender, auch ungespeichert (<see cref="ProfilArbeitsstand"/>) — und legt daraus über
    /// <see cref="Kaelteprofilgrafik.Bilder"/> Kennzahlen und Bilder an. Die Tagesarten der Woche lesen das
    /// Wochentagsraster des Projekts (dasselbe wie die Profilroutine) und denselben Kalender wie die Stundenreihe.
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
            var arbeit = new Dictionary<string, ProfilArbeitsstand>(System.StringComparer.Ordinal);
            foreach (Kaeltegrafikzeile z in zeilen)
            {
                if (z == null || string.IsNullOrEmpty(z.Name)) continue;
                summen[z.Name] = z.SummeMwh;
                arbeit[z.Name] = Arbeitsstand(z);
            }

            double[] reiheMarkiert = null;
            var reihen = new List<(string Name, double[] StundeKwh)>();
            foreach (Kaeltegrafikzeile z in zeilen)
            {
                if (z == null || string.IsNullOrEmpty(z.Name)) continue;
                BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Kaelte, idProjekt, new[] { z.Name }, null,
                                                                       summen, arbeit);
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

        /// <summary>
        /// Der Arbeitsstand einer Zeile für die Profilroutine: der Kalender des Dialogs gilt immer (auch „keiner“ —
        /// sonst rechnete die Stundenreihe mit dem gespeicherten und die Woche mit dem gewählten), Monate und Woche nur,
        /// wenn sie vollständig vorliegen.
        /// </summary>
        internal static ProfilArbeitsstand Arbeitsstand(Kaeltegrafikzeile z) => new ProfilArbeitsstand
        {
            Monatswerte = z.Monatsanteile != null && z.Monatsanteile.Length == ProfilBedarf.MONATE
                ? (double[])z.Monatsanteile.Clone() : null,
            Wochenwerte = z.Wochenprofil != null && z.Wochenprofil.Length == ProfilBedarf.WOCHEN_STUNDEN
                ? (double[])z.Wochenprofil.Clone() : null,
            KalenderGesetzt = true,
            KalenderId = z.KalenderId is int k && k > 0 ? k : 0
        };
    }
}
