using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Die Verfahren der Dreiergruppe (4.5).</summary>
    internal enum ZapfAuslegungsverfahren
    {
        /// <summary>Punkt der Summenlinie (V, Φ_N) — Topologie Speicher.</summary>
        Summenlinie = 1,

        /// <summary>Minutenspitze des Bedarfstags [kW] — Durchfluss, Frischwasser- und Wohnungsstation.</summary>
        Minutenspitze = 2,

        /// <summary>Perzentil aus dem Auslegungsensemble (Stufe Z3).</summary>
        Perzentil = 3,

        /// <summary>Normvergleich nach DIN 4708.</summary>
        Normvergleich = 4
    }

    /// <summary>Der Stand eines Werts der Dreiergruppe.</summary>
    internal enum Auslegungsstatus
    {
        /// <summary>Gerechnet.</summary>
        Gerechnet = 1,

        /// <summary>Noch nicht gerechnet — das Verfahren kommt mit einer späteren Stufe (Perzentil: Z3).</summary>
        NichtGerechnet = 2,

        /// <summary>Nicht rechenbar — eine Eingabe fehlt (Grund im Text).</summary>
        NichtRechenbar = 3,

        /// <summary>Außerhalb des Gültigkeitsbereichs des Verfahrens.</summary>
        AusserhalbGueltigkeit = 4
    }

    /// <summary>
    /// Ein Wert der Dreiergruppe: Verfahren, Stand, Volumen [l] und/oder Leistung [kW],
    /// empfohlen ja/nein, Text. Die drei Werte stehen nebeneinander, nie zu einer Zahl gemischt.
    /// </summary>
    internal sealed record Auslegungswert(ZapfAuslegungsverfahren Verfahren, Auslegungsstatus Status, double? VolumenL,
                                          double? LeistungKw, bool Empfohlen, string Text);

    /// <summary>
    /// <b>Die Empfehlung einer Topologiegruppe</b> — genau eine: bei Speicher der Punkt der
    /// Summenlinie samt nächstem Nenninhalt als Anzeige, sonst die Minutenspitze des Bedarfstags.
    /// Nicht rechenbar mit Grund, wenn der Bedarfstag oder eine Eingabe fehlt.
    /// </summary>
    internal sealed record Auslegungsempfehlung(ZapfAuslegungsverfahren Verfahren, bool Rechenbar, double? VolumenL,
                                                double? LeistungKw, double? NenninhaltL, bool Schnellauslegung,
                                                string Vermerk, string Grund);

    /// <summary>Eine Zone, die die Auslegung nicht rechnen kann, mit Grund.</summary>
    internal sealed record Auslegungsablehnung(string Zone, string Klartext);

    /// <summary>Das Ergebnis einer Topologiegruppe (ZU13: Topologie je Zone, Auslegung je Gruppe).</summary>
    internal sealed record Auslegungsgruppe
    {
        public ZapfTopologie Topologie { get; init; }
        public IReadOnlyList<string> Zonen { get; init; } = new string[0];

        /// <summary>Alle Zonen der Gruppe mit Nutzungsart Wohnen.</summary>
        public bool Wohnen { get; init; }

        public Bedarfstagwahl Bedarfstagwahl { get; init; }

        /// <summary>Der Bedarfstag; <c>null</c>, wenn der Konstruktor zu öffnen ist.</summary>
        public Bedarfstag Bedarfstag { get; init; }

        public Wochenreihe Woche { get; init; }

        /// <summary>Die Summenlinie (nur Speicher); <c>null</c> sonst oder ohne Bedarfstag.</summary>
        public Summenlinienergebnis Summenlinie { get; init; }

        /// <summary>Die Dreiergruppe: Hauptwert (Summenlinie bzw. Minutenspitze), Perzentil, Normvergleich.</summary>
        public IReadOnlyList<Auslegungswert> Dreiergruppe { get; init; } = new Auslegungswert[0];

        /// <summary>Die eine Empfehlung der Gruppe.</summary>
        public Auslegungsempfehlung Empfehlung { get; init; }

        public Din4708Ergebnis Normvergleich { get; init; }

        /// <summary>Der Verfahrensvergleich nach V4 (nur Speicher, nachrichtlich).</summary>
        public Speicherauslegungsergebnis Speicherauslegung { get; init; }

        public Grossanlagenbefund Grossanlage { get; init; }

        /// <summary>Die Warnliste der Gruppe (nie blockierend).</summary>
        public IReadOnlyList<Auslegungshinweis> Hinweise { get; init; } = new Auslegungshinweis[0];
    }

    /// <summary>
    /// <b>Das Auslegungsergebnis</b> (Umsetzungskonzept Zapfprofilgenerator 2.1, 4.5, 4.7): je
    /// Topologiegruppe die Dreiergruppe mit genau einer Empfehlung, der Verfahrensvergleich als
    /// Band, Großanlage und Warnliste; dazu abgelehnte Zonen und das Herkunftsprotokoll. Keine
    /// Bilanzreihe, kein Jahresfeld.
    /// </summary>
    internal sealed record Auslegungsergebnis
    {
        public IReadOnlyList<Auslegungsgruppe> Gruppen { get; init; } = new Auslegungsgruppe[0];
        public IReadOnlyList<Auslegungsablehnung> Ablehnungen { get; init; } = new Auslegungsablehnung[0];
        public IReadOnlyList<Auslegungshinweis> Hinweise { get; init; } = new Auslegungshinweis[0];
        public IReadOnlyList<Herkunftseintrag> Herkunft { get; init; } = new Herkunftseintrag[0];
    }

    /// <summary>
    /// Die Bausteine der Dreiergruppe: Perzentil (bis Z3 „noch nicht gerechnet"), Normvergleich,
    /// Empfehlung und die größengleiche Plausibilitätsprüfung (4.5).
    /// </summary>
    internal static class Dreiergruppe
    {
        /// <summary>Text des Perzentils bis zur Stufe Z3.</summary>
        internal const string PERZENTIL_Z3 = "noch nicht gerechnet (Z3)";

        /// <summary>Kennung der Reihenfolgeprüfung.</summary>
        internal const string HINWEIS_REIHENFOLGE = "REIHENFOLGE";

        /// <summary>Das Perzentil der Stufe Z2: noch nicht gerechnet.</summary>
        internal static Auslegungswert PerzentilOffen()
            => new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.NichtGerechnet, null, null, false, PERZENTIL_Z3);

        /// <summary>Der Normvergleich als Wert: V_DIN [l] oder „außerhalb"/„nicht rechenbar".</summary>
        internal static Auslegungswert Normvergleich(Din4708Ergebnis d)
        {
            if (d != null && d.Gueltig)
                return new Auslegungswert(ZapfAuslegungsverfahren.Normvergleich, Auslegungsstatus.Gerechnet, d.VolumenL, null, false,
                    "DIN 4708: N = " + Auslegungstext.Z(d.KennzahlN.Value) + ", V_DIN = " + Auslegungstext.G(d.VolumenL.Value)
                    + " l" + (d.Vollstaendig ? "" : " (nur Wohnzonen)"));
            bool ausserhalb = d == null || d.Fehler == ZapfAuslegungsfehler.NichtGueltig;
            return new Auslegungswert(ZapfAuslegungsverfahren.Normvergleich,
                ausserhalb ? Auslegungsstatus.AusserhalbGueltigkeit : Auslegungsstatus.NichtRechenbar, null, null, false,
                d?.Grund ?? "DIN 4708: " + Din4708Kennzahl.AUSSERHALB);
        }

        /// <summary>
        /// Die Dreiergruppe aus Hauptwert, Perzentil und Normvergleich; der Hauptwert ist genau
        /// dann empfohlen, wenn er gerechnet ist — sonst ist keiner empfohlen.
        /// </summary>
        internal static IReadOnlyList<Auslegungswert> Bilden(Auslegungswert hauptwert, Auslegungswert perzentil,
                                                             Auslegungswert norm)
        {
            Auslegungswert h = hauptwert with { Empfohlen = hauptwert.Status == Auslegungsstatus.Gerechnet };
            return new[] { h, perzentil with { Empfohlen = false }, norm with { Empfohlen = false } };
        }

        /// <summary>
        /// Die Plausibilität der Reihenfolge, größengleich (4.5): Speicher in Litern beim selben
        /// Φ_N — erwartet <c>V_Perzentil ≤ V_Summenlinie &lt; V_DIN</c>; die anderen Topologien in
        /// kW — erwartet <c>P_Perzentil ≤ P_Bedarfstag</c>. Liter werden nie gegen Kilowatt
        /// gehalten. Eine Abweichung ist ein Hinweis, kein Fehler.
        /// </summary>
        internal static IReadOnlyList<Auslegungshinweis> Reihenfolge(ZapfTopologie topologie, Auslegungswert hauptwert,
                                                                     Auslegungswert perzentil, Auslegungswert norm)
        {
            var h = new List<Auslegungshinweis>();
            bool g(Auslegungswert w) => w != null && w.Status == Auslegungsstatus.Gerechnet;
            if (topologie == ZapfTopologie.Speicher)
            {
                if (g(hauptwert) && g(norm) && hauptwert.VolumenL.HasValue && norm.VolumenL.HasValue
                    && !(hauptwert.VolumenL.Value < norm.VolumenL.Value))
                    h.Add(new Auslegungshinweis(HINWEIS_REIHENFOLGE,
                        "Der Summenlinienpunkt " + Auslegungstext.G(hauptwert.VolumenL.Value) + " l liegt nicht unter V_DIN "
                        + Auslegungstext.G(norm.VolumenL.Value) + " l."));
                if (g(perzentil) && g(hauptwert) && perzentil.VolumenL.HasValue && hauptwert.VolumenL.HasValue
                    && perzentil.VolumenL.Value > hauptwert.VolumenL.Value)
                    h.Add(new Auslegungshinweis(HINWEIS_REIHENFOLGE,
                        "Das Perzentil " + Auslegungstext.G(perzentil.VolumenL.Value) + " l liegt über dem Summenlinienpunkt "
                        + Auslegungstext.G(hauptwert.VolumenL.Value) + " l."));
            }
            else if (g(perzentil) && g(hauptwert) && perzentil.LeistungKw.HasValue && hauptwert.LeistungKw.HasValue
                     && perzentil.LeistungKw.Value > hauptwert.LeistungKw.Value)
                h.Add(new Auslegungshinweis(HINWEIS_REIHENFOLGE,
                    "Die Perzentilleistung " + Auslegungstext.Z(perzentil.LeistungKw.Value) + " kW liegt über der Minutenspitze "
                    + Auslegungstext.Z(hauptwert.LeistungKw.Value) + " kW des Bedarfstags."));
            return h.AsReadOnly();
        }
    }
}
