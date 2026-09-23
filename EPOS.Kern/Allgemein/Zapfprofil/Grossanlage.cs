using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Befund der Großanlagenerkennung (4.7): groß ja/nein, durch Speichervolumen und/oder
    /// Leitungsinhalt, die geprüften Größen [l] (<c>null</c> = unbekannt) und ein Satz.
    /// </summary>
    internal sealed record Grossanlagenbefund(bool Gross, bool DurchSpeicher, bool DurchLeitung, double? VolumenL,
                                              double? LeitungsinhaltL, string Satz);

    /// <summary>
    /// <b>Die Großanlagenerkennung nach DVGW W 551</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 4.7): Speichervolumen (<c>Nachweis_Volumen_l</c>, sonst gewählter, sonst empfohlener
    /// Auslegungspunkt) und Leitungsinhalt (<c>Leitungsinhalt_l</c>, sonst <c>Zirk_Laenge_m</c> ×
    /// Inhalt je Meter) gegen die Schwellen des Parametersatzes — keine Schwelle im Text oder
    /// Code. Groß ist eine Anlage, deren Speichervolumen ODER Leitungsinhalt die Schwelle
    /// überschreitet. Die Erkennung steuert Vorgaben (Mindesttemperatur, Zirkulation) und
    /// Hinweise.
    /// </summary>
    internal static class Grossanlage
    {
        /// <summary>Kennung: Großanlage erkannt.</summary>
        internal const string HINWEIS_GROSSANLAGE = "GROSSANLAGE";

        /// <summary>Kennung: Zirkulation „nein" bei erkannter Großanlage.</summary>
        internal const string HINWEIS_OHNE_ZIRKULATION = "GROSSANLAGE_OHNE_ZIRKULATION";

        /// <summary>
        /// Das Speichervolumen der Erkennung [l]: Nachweisvolumen, sonst gewählter
        /// Auslegungspunkt des Projekts, sonst <paramref name="empfohlenL"/>.
        /// </summary>
        internal static double? Speichervolumen(ProjektStand p, double? empfohlenL)
            => p?.NachweisVolumenL ?? p?.AuslegungVolumenL ?? empfohlenL;

        /// <summary>
        /// Der Leitungsinhalt [l]: Projektangabe, sonst Länge der Zirkulationsleitung × Inhalt je
        /// Meter (Parameter); ohne beides <c>null</c>.
        /// </summary>
        internal static double? Leitungsinhalt(ProjektStand p, Parametersatz ps)
        {
            if (p?.LeitungsinhaltL != null) return Auslegungspruefung.NichtNegativ(p.LeitungsinhaltL.Value, "der Leitungsinhalt");
            if (p?.ZirkLaengeM != null)
                return Auslegungspruefung.NichtNegativ(p.ZirkLaengeM.Value, "die Länge der Zirkulationsleitung")
                       * ps.Wert(ZapfAuslegungParameter.W551_INHALT_JE_METER);
            return null;
        }

        /// <summary>Die Erkennung: V &gt; Schwelle_V oder Leitungsinhalt &gt; Schwelle_L (beide aus dem Parametersatz).</summary>
        internal static Grossanlagenbefund Erkennen(double? speichervolumenL, double? leitungsinhaltL, Parametersatz ps)
        {
            double schwelleV = ps.Wert(ZapfAuslegungParameter.W551_GROSS_VOLUMEN);
            double schwelleL = ps.Wert(ZapfAuslegungParameter.W551_GROSS_LEITUNG);
            bool durchV = speichervolumenL.HasValue && speichervolumenL.Value > schwelleV;
            bool durchL = leitungsinhaltL.HasValue && leitungsinhaltL.Value > schwelleL;
            bool gross = durchV || durchL;
            string satz;
            if (gross)
                satz = "Großanlage nach DVGW W 551"
                       + (durchV ? ": Speichervolumen " + Auslegungstext.G(speichervolumenL.Value) + " l über "
                                   + Auslegungstext.G(schwelleV) + " l" : "")
                       + (durchV && durchL ? "," : durchL ? ":" : "")
                       + (durchL ? " Leitungsinhalt " + Auslegungstext.Z(leitungsinhaltL.Value) + " l über "
                                   + Auslegungstext.Z(schwelleL) + " l" : "")
                       + " — Austrittstemperatur, Zirkulation und thermische Desinfektion nach DVGW W 551 einhalten.";
            else if (!speichervolumenL.HasValue && !leitungsinhaltL.HasValue)
                satz = "Großanlage nicht prüfbar — weder Speichervolumen noch Leitungsinhalt bekannt.";
            else
                satz = "Kleinanlage nach DVGW W 551.";
            return new Grossanlagenbefund(gross, durchV, durchL, speichervolumenL, leitungsinhaltL, satz);
        }

        /// <summary>
        /// Die Hinweise des Befunds: Großanlage erkannt; Zirkulation „nein" bei Großanlage. Die
        /// Prüfung der Speichertemperatur gegen die Mindesttemperatur steht in der
        /// Speicherauslegung.
        /// </summary>
        internal static IReadOnlyList<Auslegungshinweis> Hinweise(Grossanlagenbefund b, bool zirkulationJa)
        {
            var h = new List<Auslegungshinweis>();
            if (b == null || !b.Gross) return h.AsReadOnly();
            h.Add(new Auslegungshinweis(HINWEIS_GROSSANLAGE, b.Satz, true));
            if (!zirkulationJa)
                h.Add(new Auslegungshinweis(HINWEIS_OHNE_ZIRKULATION,
                    "Die Anlage ist eine Großanlage, aber keine Zone nimmt an der Zirkulation teil.", true));
            return h.AsReadOnly();
        }
    }
}
