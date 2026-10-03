using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Das abgeleitete Nutzungsprofil samt Herkunft (Konzept 3.5).</summary>
    public sealed record PufferNutzungsprofilAbleitung(PufferNutzungsprofil Profil, string Herkunft, bool Vorgabe);

    /// <summary>
    /// Die Ableitung des Nutzungsprofils aus übergebenen Fakten (Konzept 3.5, keine Datenbank): zuerst
    /// die Zapf-Nutzungsart der Zonen (Hotel → Beherbergung; Senioren/Pflege/Krankenhaus →
    /// Pflege/Krankenhaus; Wohnen/Ein- und Zweifamilienhaus/Studentenwohnheim → Wohnen), dann
    /// Prozesswärme im Projekt → Gewerbe/Prozess, dann die Konditionierungsnutzung BUERO/SCHULE →
    /// Büro/Schule, sonst Wohnen als Vorgabe. Wirkung nur auf Beispielwerte und Hinweistexte (E-P20).
    /// </summary>
    public static class Nutzungsprofil
    {
        private static bool Enthaelt(string text, params string[] teile)
        {
            foreach (string t in teile)
                if (text.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>Die Zuordnung einer Zapf-Nutzungsart (Bezeichner); <c>null</c> = ohne Zuordnung.</summary>
        public static PufferNutzungsprofil? AusZapfnutzung(string bezeichner)
        {
            if (string.IsNullOrWhiteSpace(bezeichner)) return null;
            if (Enthaelt(bezeichner, "Hotel", "Beherbergung", "Pension")) return PufferNutzungsprofil.BEHERBERGUNG;
            if (Enthaelt(bezeichner, "Senioren", "Pflege", "Krankenhaus", "Klinik")) return PufferNutzungsprofil.PFLEGE;
            if (Enthaelt(bezeichner, "Wohn", "familienhaus", "Studenten", "EFH", "MFH")) return PufferNutzungsprofil.WOHNEN;
            return null;
        }

        /// <summary>Die Ableitung in der Reihenfolge des Konzepts.</summary>
        /// <param name="zapfnutzungen">Bezeichner der Zapf-Nutzungsarten der Zonen, in Zonenreihenfolge.</param>
        /// <param name="prozesswaermeVorhanden">Trägt das Projekt Prozesswärme?</param>
        /// <param name="konditionierungNutzungen">Die <c>Nutzung</c> der Kalendervorlagen (WOHNEN, BUERO, SCHULE, SONSTIGE).</param>
        public static PufferNutzungsprofilAbleitung Ableiten(IEnumerable<string> zapfnutzungen, bool prozesswaermeVorhanden,
                                                             IEnumerable<string> konditionierungNutzungen)
        {
            if (zapfnutzungen != null)
                foreach (string n in zapfnutzungen)
                {
                    PufferNutzungsprofil? p = AusZapfnutzung(n);
                    if (p.HasValue) return new PufferNutzungsprofilAbleitung(p.Value, "Zapf-Nutzungsart „" + n + "“", false);
                }
            if (prozesswaermeVorhanden)
                return new PufferNutzungsprofilAbleitung(PufferNutzungsprofil.GEWERBE, "Prozesswärme im Projekt", false);
            if (konditionierungNutzungen != null)
                foreach (string n in konditionierungNutzungen)
                    if (n == "BUERO" || n == "SCHULE")
                        return new PufferNutzungsprofilAbleitung(PufferNutzungsprofil.BUERO_SCHULE, "Konditionierungsnutzung " + n, false);
            return new PufferNutzungsprofilAbleitung(PufferNutzungsprofil.WOHNEN, "Vorgabe", true);
        }
    }
}
