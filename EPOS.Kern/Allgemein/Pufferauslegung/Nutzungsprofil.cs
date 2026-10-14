using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Das abgeleitete Nutzungsprofil samt Herkunft (Konzept 3.5) als <see cref="Textbaustein"/>.</summary>
    public sealed record PufferNutzungsprofilAbleitung(PufferNutzungsprofil Profil, Textbaustein HerkunftBaustein, bool Vorgabe)
    {
        /// <summary>Die Herkunft als deutscher Klartext.</summary>
        public string Herkunft => HerkunftBaustein?.Klartext ?? "";
    }

    /// <summary>
    /// Die Ableitung des Nutzungsprofils aus übergebenen Fakten (Konzept 3.5, keine Datenbank) über die
    /// Zuordnung <see cref="NutzungsprofilZuordnung"/> (V32: ganzer Schlüssel, kein Teiltext): zuerst die
    /// Zapf-Nutzungsart der Zonen (Quelle ZAPF), dann Prozesswärme im Projekt → Gewerbe/Prozess, dann die
    /// Konditionierungsnutzung (Quelle KONDITIONIERUNG, ausgeliefert BUERO/SCHULE → Büro/Schule), sonst
    /// Wohnen als Vorgabe. Wirkung nur auf Beispielwerte und Hinweistexte (E-P20).
    /// </summary>
    public static class Nutzungsprofil
    {
        /// <summary>Die Zuordnung einer Zapf-Nutzungsart (Bezeichner); <c>null</c> = ohne Zuordnung.</summary>
        /// <param name="zuordnung">Die Zuordnung (<see cref="NutzungsprofilZuordnung.Lesen"/>); <c>null</c> = die Vorgabe im Code.</param>
        public static PufferNutzungsprofil? AusZapfnutzung(string bezeichner, IReadOnlyDictionary<string, PufferNutzungsprofil> zuordnung = null)
            => NutzungsprofilZuordnung.Finden(zuordnung, NutzungsprofilQuelle.ZAPF, bezeichner);

        /// <summary>Die Zuordnung einer Gebäudeart (Freitext, ganzer Schlüssel); <c>null</c> = ohne Zuordnung.</summary>
        public static PufferNutzungsprofil? AusGebaeudeart(string gebaeudeart, IReadOnlyDictionary<string, PufferNutzungsprofil> zuordnung = null)
            => NutzungsprofilZuordnung.Finden(zuordnung, NutzungsprofilQuelle.GEBAEUDEART, gebaeudeart);

        /// <summary>Die Ableitung in der Reihenfolge des Konzepts.</summary>
        /// <param name="zapfnutzungen">Bezeichner der Zapf-Nutzungsarten der Zonen, in Zonenreihenfolge.</param>
        /// <param name="prozesswaermeVorhanden">Trägt das Projekt Prozesswärme?</param>
        /// <param name="konditionierungNutzungen">Die <c>Nutzung</c> der Kalendervorlagen (WOHNEN, BUERO, SCHULE, SONSTIGE).</param>
        /// <param name="zuordnung">Die Zuordnung (<see cref="NutzungsprofilZuordnung.Lesen"/>); <c>null</c> = die Vorgabe im Code.</param>
        public static PufferNutzungsprofilAbleitung Ableiten(IEnumerable<string> zapfnutzungen, bool prozesswaermeVorhanden,
                                                             IEnumerable<string> konditionierungNutzungen,
                                                             IReadOnlyDictionary<string, PufferNutzungsprofil> zuordnung = null)
        {
            if (zapfnutzungen != null)
                foreach (string n in zapfnutzungen)
                {
                    PufferNutzungsprofil? p = AusZapfnutzung(n, zuordnung);
                    if (p.HasValue) return new PufferNutzungsprofilAbleitung(p.Value,
                        Textbaustein.T("PAUS_HERK_NP_ZAPF", "Zapf-Nutzungsart „{0}“", n), false);
                }
            if (prozesswaermeVorhanden)
                return new PufferNutzungsprofilAbleitung(PufferNutzungsprofil.GEWERBE,
                    Textbaustein.T("PAUS_HERK_NP_PROZESS", "Prozesswärme im Projekt"), false);
            if (konditionierungNutzungen != null)
                foreach (string n in konditionierungNutzungen)
                {
                    PufferNutzungsprofil? p = NutzungsprofilZuordnung.Finden(zuordnung, NutzungsprofilQuelle.KONDITIONIERUNG, n);
                    if (p.HasValue)
                        return new PufferNutzungsprofilAbleitung(p.Value,
                            Textbaustein.T("PAUS_HERK_NP_KONDITIONIERUNG", "Konditionierungsnutzung {0}", n), false);
                }
            return new PufferNutzungsprofilAbleitung(PufferNutzungsprofil.WOHNEN,
                Textbaustein.T("PAUS_HERK_NP_VORGABE", "Vorgabe"), true);
        }
    }
}
