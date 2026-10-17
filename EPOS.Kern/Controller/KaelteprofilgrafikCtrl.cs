using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Kältezeile, wie sie der Dialog gerade zeigt — der ungespeicherte Arbeitsstand: Name, Jahressumme [MWh],
    /// Betriebskalender (<c>null</c> = keiner) und, sofern im Dialog bearbeitet, Monatsanteile [%] und Wochenprofil
    /// (168 Werte, Montag 0 Uhr zuerst); <c>null</c> = der Stand der Projektkopie. KBP-4b: dazu die Eingabeart des Dialogs
    /// (<c>null</c> = die gespeicherte) und, sofern bearbeitet, die Typtage samt Matrix (<c>null</c> = die der Projektkopie).
    /// KV8: dazu die ID der Projektkopie (0 = unbekannt) — ohne gewählten Kalender gilt für eine Kopie mit Normtagen der
    /// Normkalender als Vorgabe (<see cref="NormkalenderCtrl.Wirksam"/>), auch für Tagesarten und Nutzungstage der Grafik.
    /// </summary>
    public sealed record Kaeltegrafikzeile(string Name, double SummeMwh, int? KalenderId,
                                           double[] Monatsanteile = null, double[] Wochenprofil = null,
                                           string Eingabeart = null, KaeltetyptagEingabe Typtage = null, int IdKopie = 0);

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

            // KV8-N: das Wochentagsraster des Projekts — dasselbe, in dem Vorschau und Lauf die Reihe rechnen.
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(idProjekt);
            // KV8: der wirksame Kalender — der gewählte, sonst der Normkalender als Vorgabe (derselbe Weg wie die Rechnung).
            Betriebskalender kalender = markiert.IdKopie > 0
                ? NormkalenderCtrl.Wirksam(idProjekt, markiert.IdKopie, markiert.KalenderId)
                : markiert.KalenderId is int id && id > 0 ? BetriebskalenderCtrl.Lies(id) : null;
            byte[] arten = kalender?.Tagesarten(raster);

            Kaelteprofilbilder bilder = Kaelteprofilgrafik.Bilder(reiheMarkiert, reihen, raster.W0, arten);
            // KV8: Trägt die Kopie Nutzungstage nach Norm, nennt die Kennzahlenzeile die Nutzungstage des wirksamen Kalenders
            // — gezählt im Wochentagsraster des Projekts, in dem der Normkalender gebildet ist und die Rechnung läuft.
            if (bilder != null && kalender != null && markiert.IdKopie > 0 &&
                KaeltebedarfStammCtrl.ProjektNutzungstage(markiert.IdKopie) is int norm && norm > 0)
                bilder = bilder with { Nutzungstage = NormkalenderCtrl.Nutzungstage(kalender, raster) };
            return bilder;
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
            KalenderId = z.KalenderId is int k && k > 0 ? k : 0,
            // KBP-4b: die Eingabeart des Dialogs und die dort bearbeiteten Typtage (ungespeichert).
            Eingabeart = string.IsNullOrEmpty(z.Eingabeart) ? null : KaeltebedarfsprofilVorlagenSchema.Eingabeart(z.Eingabeart),
            Typtage = z.Typtage
        };
    }
}
