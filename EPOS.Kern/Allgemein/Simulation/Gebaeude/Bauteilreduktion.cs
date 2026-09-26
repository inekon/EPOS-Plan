using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Richtung des Wärmestroms durch ein Bauteil. Sie wählt den inneren
    /// Wärmeübergangswiderstand (DIN EN ISO 6946, 6.8, Tabelle 7) und den Widerstand einer
    /// ruhenden Luftschicht (6.9.2, Tabelle 8) und folgt aus der Neigung des Bauteils
    /// (<see cref="Bauteilreduktion.RichtungAusNeigung"/>).
    /// </summary>
    internal enum Waermestromrichtung
    {
        /// <summary>Wärmestrom aufwärts: Neigung unter 60° (Dach, Decke).</summary>
        Aufwaerts,

        /// <summary>Wärmestrom waagerecht: Neigung 60° … 120° (Wand; ±30° um die Waagerechte).</summary>
        Horizontal,

        /// <summary>Wärmestrom abwärts: Neigung über 120° (Boden).</summary>
        Abwaerts,
    }

    /// <summary>
    /// Was auf der Außenseite eines Bauteils liegt (Stufe G3; Mehrzonenkonzept 2.2). Die
    /// Randbedingung entscheidet die Gruppe im 2-K-Modell und den äußeren
    /// Wärmeübergangswiderstand.
    /// </summary>
    internal enum Bauteilrand
    {
        /// <summary>Außenluft — Außenbauteilgruppe; R_se nach Tabelle 7.</summary>
        Aussenluft,

        /// <summary>Erdreich — Außenbauteilgruppe; kein äußerer Übergang.</summary>
        Erdreich,

        /// <summary>Unbeheizter Raum — Außenbauteilgruppe; R_se = R_si (DIN EN ISO 6946, 6.7.1.2).</summary>
        Unbeheizt,

        /// <summary>Innerhalb der Zone — Innenbauteilgruppe, symmetrisch beaufschlagt.</summary>
        Innen,

        /// <summary>
        /// Nachbarzone (Trennfläche, Stufe G6b) — im Einzonenweg benannt abgelehnt; im Mehrzonenweg
        /// nach der Zuordnung (<see cref="Trennflaechenzuordnung"/>) in der Außen- oder der
        /// Innenbauteilgruppe, nachbarseitig mit dem Übergang nach <see cref="Nachbaruebergang"/>.
        /// </summary>
        Zone,
    }

    /// <summary>
    /// Die Gruppe einer Trennfläche (Stufe G6b; Mehrzonenkonzept 2.2, Anwenderentscheid A1 = M3 (b)):
    /// <c>Tab_Bauteil.Trennflaeche_Zuordnung</c> NULL heißt „die 4-K-Regel entscheidet"
    /// (<see cref="Regel"/>), sonst die ausdrückliche Übersteuerung.
    /// </summary>
    internal enum Trennflaechenzuordnung
    {
        /// <summary>
        /// Die 4-K-Regel entscheidet (adiabater Vorlauf der Zonenschleife); solange sie nicht
        /// entschieden hat, rechnet die Trennfläche adiabat in der Innenbauteilgruppe.
        /// </summary>
        Regel = 0,

        /// <summary>Innenbauteilgruppe (IW): symmetrisch beaufschlagt, adiabat.</summary>
        Innen,

        /// <summary>Außenbauteilgruppe (AW): einseitig beaufschlagt, über θ_NR,eq an die Nachbarzone gekoppelt.</summary>
        Aussen,
    }

    /// <summary>
    /// <b>Der nachbarseitige Übergang einer Trennfläche</b> (Stufe G6b, Anwenderfrage A7;
    /// Mehrzonenkonzept 2.2 Punkt 4) — gilt nur, wenn die Trennfläche ein α_kon,a trägt; ohne es
    /// gelten beide Male die Vorgaben des unbeheizten Raums.
    /// </summary>
    internal enum Nachbaruebergang
    {
        /// <summary>
        /// Wie am unbeheizten Raum: 1/(α_kon,a + α_str) — der Übergang, mit dem Testbeispiel 10 im
        /// Einzonenweg rechnet (A7 (b)).
        /// </summary>
        WieUnbeheizt = 0,

        /// <summary>Nur konvektiv: 1/α_kon,A;NR wie in Gl. (40) (A7 (a)).</summary>
        NurKonvektiv,
    }

    /// <summary>
    /// Eine thermisch homogene Schicht eines Bauteils in SI-Einheiten: Dicke [m],
    /// Wärmeleitfähigkeit λ [W/(mK)], Rohdichte ρ [kg/m³], spezifische Wärmekapazität c_p
    /// [J/(kgK)] — c_p in <b>J</b>/(kgK), nicht in kJ/(kgK) wie die Bauteiltabellen der
    /// Richtlinie (Mehrzonenkonzept 3.4, „Einheitenfalle").
    ///
    /// <para><b>Luftschichten</b> (EPOS-Regel mit Quelle DIN EN ISO 6946, Mehrzonenkonzept 3.2):
    /// Eine Luftschicht <b>ohne</b> λ ist eine ruhende Luftschicht; ihr Widerstand kommt aus
    /// Tabelle 8, eine Kapazität trägt sie nicht (<see cref="RuhendeLuft"/>). Eine Luftschicht
    /// <b>mit</b> λ (äquivalente Leitfähigkeit) ist eine normale Schicht; nur ihre Rohdichte darf
    /// unter dem Band liegen, und ohne ρ oder c_p trägt sie keine Kapazität.</para>
    /// </summary>
    internal readonly record struct Schicht(double Dicke_M, double Lambda_WmK, double Rohdichte_KgM3, double Cp_JkgK, bool IstLuftschicht = false)
    {
        /// <summary>Eine ruhende Luftschicht ohne λ: Widerstand nach DIN EN ISO 6946 Tabelle 8, keine Kapazität.</summary>
        internal static Schicht RuhendeLuft(double dicke_M) => new Schicht(dicke_M, double.NaN, double.NaN, double.NaN, true);

        /// <summary>Wahr für eine ruhende Luftschicht, deren Widerstand aus Tabelle 8 kommt.</summary>
        internal bool IstRuhendeLuftschicht => IstLuftschicht && double.IsNaN(Lambda_WmK);
    }

    /// <summary>
    /// Die Kennwerte eines Schichtaufbaus je Flächeneinheit — die Grundlage des U-Werts aus
    /// Schichten (Mehrzonenkonzept 3.4): Wärmedurchlasswiderstand R = Σ d/λ einschließlich
    /// ruhender Luftschichten [m²K/W], flächenbezogene Kapazität Σ ρ·c_p·d [J/(m²K)] und die
    /// beiden Übergangswiderstände [m²K/W]; U = 1/(R_si + R + R_se) [W/(m²K)].
    /// </summary>
    internal readonly record struct Schichtkennwerte(double R_M2KW, double Kapazitaet_JM2K, double R_si_M2KW, double R_se_M2KW)
    {
        /// <summary>Wärmedurchgangskoeffizient U = 1/(R_si + R + R_se) [W/(m²K)] — DIN EN ISO 6946 Gl. (4).</summary>
        internal double U_WM2K => 1.0 / (R_si_M2KW + R_M2KW + R_se_M2KW);
    }

    /// <summary>
    /// Die Ersatzgrößen EINES Bauteils nach VDI 6007 Blatt 1 Gl. (12)–(17) bei der Bezugsperiode
    /// <see cref="Periode_d"/>, auf die Bauteilfläche bezogen: Widerstände in K/W, Kapazitäten
    /// in J/K. <see cref="RW_KW"/> ist der stationäre Durchlasswiderstand (1/A)·Σ(d/λ) ohne
    /// Übergänge; <see cref="C1korr_Jk"/> die Kapazität bei einseitiger Belastung (Gl. (17)).
    /// </summary>
    internal readonly record struct Bauteilkennwerte(
        double Periode_d, double R1_KW, double R2_KW, double R3_KW, double RW_KW,
        double C1_Jk, double C2_Jk, double C1korr_Jk);

    /// <summary>
    /// Die Wahl der Bezugsperiode eines Bauteils nach VDI 6007 Blatt 1 Gl. (10a)–(10d): die beiden
    /// Verhältnisse R₁;rel = R₁(2 d)/R₁(7 d) und C₁;rel = C₁(2 d)/C₁(7 d), die gewählte Periode und
    /// die Ersatzgrößen bei ihr. Die Wahl wird je Bauteil ausgewiesen.
    /// </summary>
    internal readonly record struct Bezugsperiodenwahl(double Periode_d, double R1rel, double C1rel, Bauteilkennwerte Kennwerte)
    {
        /// <summary>Wahr, wenn (10a) oder (10b) greift und das Bauteil mit 2 Tagen rechnet (10c).</summary>
        internal bool Abgedeckt => Periode_d == GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D;
    }

    /// <summary>
    /// Die Kettenmatrix eines Schichtaufbaus je Flächeneinheit (VDI 6007 Blatt 1 Gl. (1)–(11)),
    /// <b>als Abweichung von der Einheitsmatrix geführt</b>: <see cref="D11"/> = a₁₁ − 1 und
    /// <see cref="D22"/> = a₂₂ − 1 stehen ungekürzt, weil Gl. (12)–(17) genau diese Differenzen
    /// brauchen. Für dünne oder leichte Schichten liegt a₂₂ dicht bei 1; die Differenz aus
    /// a₂₂ − 1 zu bilden, verlöre dort alle Stellen.
    /// </summary>
    internal readonly struct Kettenmatrix
    {
        internal Kettenmatrix(Complex d11, Complex a12, Complex a21, Complex d22)
        {
            D11 = d11;
            A12 = a12;
            A21 = a21;
            D22 = d22;
        }

        /// <summary>a₁₁ − 1 [–].</summary>
        internal Complex D11 { get; }

        /// <summary>a₁₂ [m²K/W].</summary>
        internal Complex A12 { get; }

        /// <summary>a₂₁ [W/(m²K)].</summary>
        internal Complex A21 { get; }

        /// <summary>a₂₂ − 1 [–].</summary>
        internal Complex D22 { get; }

        /// <summary>a₁₁ [–].</summary>
        internal Complex A11 => Complex.One + D11;

        /// <summary>a₂₂ [–].</summary>
        internal Complex A22 => Complex.One + D22;

        /// <summary>Die Einheitsmatrix (keine Schicht).</summary>
        internal static Kettenmatrix Einheit => new Kettenmatrix(Complex.Zero, Complex.Zero, Complex.Zero, Complex.Zero);

        /// <summary>
        /// Das Produkt this · L (Gl. (11): von der raumseitigen Schicht nach außen). Mit
        /// M = I + D und L = I + E ist M·L = I + D + E + D·E; so bleiben die Diagonalen als
        /// Abweichung von 1 exakt.
        /// </summary>
        internal Kettenmatrix Mal(in Kettenmatrix l)
        {
            Complex e11 = l.D11, e12 = l.A12, e21 = l.A21, e22 = l.D22;
            return new Kettenmatrix(
                D11 + e11 + (D11 * e11 + A12 * e21),
                A12 + e12 + (D11 * e12 + A12 * e22),
                A21 + e21 + (A21 * e11 + D22 * e21),
                D22 + e22 + (A21 * e12 + D22 * e22));
        }
    }

    /// <summary>
    /// <b>Der Bauteilweg nach VDI 6007 Blatt 1, Gl. (1)–(24)</b> (Stufe G3; Mehrzonenkonzept 3.1–3.4,
    /// Rechenschritte Kapitel 3, Softwarearchitektur 1.3): Kettenmatrix je Schicht und je
    /// Bauteil, Ersatzgrößen R₁, R₂, R₃, C₁, C₂, C₁,korr, die Bezugsperiode je Bauteil, die
    /// Parallelschaltung zum Raum und der U-Wert aus Schichten. Ohne Datenbank, ohne Zustand,
    /// durchgehend <c>double</c> und <see cref="Complex"/> (<c>double</c>-basiert).
    ///
    /// <para><b>Numerik.</b> Je Schicht gilt R = d/λ, C = c_p·ρ·d, ω = 2π/(86 400·T),
    /// k = (1 + j)·√(ωRC/2) und a₁₁ = a₂₂ = cosh k, a₁₂ = R·sinh(k)/k, a₂₁ = (k²/R)·sinh(k)/k
    /// (Gl. (3)–(8)). Wegen k² = jωRC ist a₂₁ = jωC·sinh(k)/k — es wird <b>nie durch R oder k
    /// geteilt</b>; sinh(k)/k läuft für kleines |k| über die Reihe, cosh k − 1 über 2·sinh²(k/2).
    /// So bleiben eine Schicht mit R → 0 (Metallblech) und eine mit C → 0 (Luft) endlich.</para>
    ///
    /// <para><b>Die EINE Stelle „U-Wert aus Schichten"</b> im Kern ist
    /// <see cref="UWertAusSchichten"/> mit den Übergangswiderständen aus
    /// <see cref="Uebergangswiderstaende"/>; Importe und Dialoge rufen sie, statt Σ d/λ selbst
    /// zu bilden.</para>
    /// </summary>
    internal static class Bauteilreduktion
    {
        /// <summary>Kriterium (10a): R₁;rel oberhalb dieses Werts [–].</summary>
        internal const double KRITERIUM_R1REL_OBEN = 0.99;

        /// <summary>Kriterien (10a)/(10b): C₁;rel (und in (10b) R₁;rel) unterhalb dieses Werts [–].</summary>
        internal const double KRITERIUM_REL_UNTEN = 0.95;

        /// <summary>Kriterium (10b): Betrag der Differenz R₁;rel − C₁;rel oberhalb dieses Werts [–].</summary>
        internal const double KRITERIUM_ABSTAND = 0.30;

        /// <summary>Unter diesem |k| läuft sinh(k)/k über die Reihe [–].</summary>
        private const double REIHE_BIS = 1e-2;

        // =====================================================================
        //  Übergänge und U-Wert aus Schichten (Mehrzonenkonzept 3.4, DIN EN ISO 6946)
        // =====================================================================

        /// <summary>
        /// Die Wärmestromrichtung aus der Neigung β des Bauteils (0° = waagerecht nach oben,
        /// 90° = senkrecht, 180° = waagerecht nach unten): aufwärts für β &lt; 60°, waagerecht für
        /// 60° ≤ β ≤ 120°, abwärts für β &gt; 120° (DIN EN ISO 6946, 6.8: ±30° um die Waagerechte).
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.BauteilUngueltig"/>, wenn β nicht in 0° … 180° liegt.</exception>
        internal static Waermestromrichtung RichtungAusNeigung(double neigungGrad, string wer = null)
        {
            if (!(neigungGrad >= 0.0 && neigungGrad <= 180.0))
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_NEIGUNG, Wer(wer), Text(neigungGrad)));
            if (neigungGrad < GebaeudeFestwerte.NEIGUNG_HORIZONTAL_MIN_GRAD) return Waermestromrichtung.Aufwaerts;
            if (neigungGrad > GebaeudeFestwerte.NEIGUNG_HORIZONTAL_MAX_GRAD) return Waermestromrichtung.Abwaerts;
            return Waermestromrichtung.Horizontal;
        }

        /// <summary>Der innere Wärmeübergangswiderstand R_si einer Richtung [m²K/W] — DIN EN ISO 6946, Tabelle 7.</summary>
        internal static double InnererUebergang(Waermestromrichtung richtung)
        {
            switch (richtung)
            {
                case Waermestromrichtung.Aufwaerts: return GebaeudeFestwerte.R_SI_AUFWAERTS;
                case Waermestromrichtung.Abwaerts: return GebaeudeFestwerte.R_SI_ABWAERTS;
                default: return GebaeudeFestwerte.R_SI_HORIZONTAL;
            }
        }

        /// <summary>
        /// Die Bemessungswerte der Übergangswiderstände eines Bauteils [m²K/W] nach
        /// DIN EN ISO 6946, Tabelle 7: R_si aus der Neigung (<see cref="RichtungAusNeigung"/>);
        /// R_se = 0,04 an Außenluft, 0 an Erdreich, an einem unbeheizten Raum, innerhalb der Zone
        /// und an einer Nachbarzone R_se = R_si (6.7.1.2: R_si auf beiden Seiten).
        /// </summary>
        internal static (double R_si_M2KW, double R_se_M2KW) Uebergangswiderstaende(double neigungGrad, Bauteilrand rand, string wer = null)
        {
            double rSi = InnererUebergang(RichtungAusNeigung(neigungGrad, wer));
            double rSe;
            switch (rand)
            {
                case Bauteilrand.Aussenluft: rSe = GebaeudeFestwerte.R_SE_AUSSENLUFT; break;
                case Bauteilrand.Erdreich: rSe = GebaeudeFestwerte.R_SE_ERDREICH; break;
                default: rSe = rSi; break;
            }
            return (rSi, rSe);
        }

        /// <summary>
        /// <b>Der U-Wert aus Schichten</b> — die eine Stelle im Kern (Mehrzonenkonzept 3.4):
        /// R = Σ d/λ einschließlich ruhender Luftschichten, flächenbezogene Kapazität
        /// Σ ρ·c_p·d, U = 1/(R_si + R + R_se) mit den Übergängen aus
        /// <see cref="Uebergangswiderstaende"/>. Jede Schicht wird gegen das Plausibilitätsband
        /// geprüft.
        /// </summary>
        /// <param name="schichten">Die Schichten, beginnend mit der raumseitigen.</param>
        /// <param name="neigungGrad">Neigung β des Bauteils [°]: 0 = Dach, 90 = Wand, 180 = Boden.</param>
        /// <param name="rand">Randbedingung der Außenseite.</param>
        /// <param name="wer">Bezeichnung für Meldungen.</param>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.SchichtUngueltig"/> oder <see cref="GebaeudeModellFehler.BauteilUngueltig"/>.</exception>
        internal static Schichtkennwerte UWertAusSchichten(IReadOnlyList<Schicht> schichten, double neigungGrad, Bauteilrand rand, string wer = null)
        {
            Waermestromrichtung richtung = RichtungAusNeigung(neigungGrad, wer);
            (double rSi, double rSe) = Uebergangswiderstaende(neigungGrad, rand, wer);
            return Kennwerte(schichten, richtung, rSi, rSe, wer);
        }

        /// <summary>
        /// Wie <see cref="UWertAusSchichten"/>, mit vorgegebenen Übergangswiderständen — für ein
        /// Bauteil, das seine Übergangskoeffizienten selbst trägt (Bauteilweg, Testräume).
        /// </summary>
        internal static Schichtkennwerte Kennwerte(IReadOnlyList<Schicht> schichten, Waermestromrichtung richtung,
                                                   double r_si_M2KW, double r_se_M2KW, string wer = null)
        {
            Pruefen(schichten, wer);
            double r = 0.0, c = 0.0;
            for (int i = 0; i < schichten.Count; i++)
            {
                r += Waermedurchlasswiderstand(schichten[i], richtung, wer, i + 1);
                c += FlaechenbezogeneKapazitaet(schichten[i]);
            }
            return new Schichtkennwerte(r, c, r_si_M2KW, r_se_M2KW);
        }

        // =====================================================================
        //  Schichten
        // =====================================================================

        /// <summary>
        /// Prüft jede Schicht gegen das Plausibilitätsband (Mehrzonenkonzept 3.5): Dicke
        /// 0,001 … 1,0 m, λ 0,005 … 500 W/(mK), ρ 5 … 8 000 kg/m³, c_p 100 … 5 000 J/(kgK); eine
        /// ruhende Luftschicht höchstens 0,3 m (DIN EN ISO 6946, 6.9.1); eine Luftschicht mit λ darf
        /// eine Rohdichte unter dem Band tragen.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.SchichtUngueltig"/>.</exception>
        internal static void Pruefen(IReadOnlyList<Schicht> schichten, string wer = null)
        {
            if (schichten == null || schichten.Count == 0)
                throw new GebaeudeModellException(GebaeudeModellFehler.SchichtUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_AUFBAU_LEER, Wer(wer)));
            for (int i = 0; i < schichten.Count; i++) SchichtPruefen(schichten[i], wer, i + 1);
        }

        private static void SchichtPruefen(in Schicht s, string wer, int nummer)
        {
            if (!ImBand(s.Dicke_M, GebaeudeFestwerte.SCHICHT_DICKE_MIN_M, GebaeudeFestwerte.SCHICHT_DICKE_MAX_M))
                throw SchichtFehler(wer, nummer, "d", s.Dicke_M, "m", GebaeudeFestwerte.SCHICHT_DICKE_MIN_M, GebaeudeFestwerte.SCHICHT_DICKE_MAX_M);

            if (s.IstRuhendeLuftschicht)
            {
                if (s.Dicke_M > GebaeudeFestwerte.LUFTSCHICHT_DICKE_MAX_M)
                    throw new GebaeudeModellException(GebaeudeModellFehler.SchichtUngueltig,
                        Format(MyResource.Resource.SIMENG_G3_LUFTSCHICHT_DICKE, Wer(wer), Text(nummer),
                               Text(s.Dicke_M), Text(GebaeudeFestwerte.LUFTSCHICHT_DICKE_MAX_M)));
                return;
            }

            if (!ImBand(s.Lambda_WmK, GebaeudeFestwerte.LAMBDA_MIN_WMK, GebaeudeFestwerte.LAMBDA_MAX_WMK))
                throw SchichtFehler(wer, nummer, "λ", s.Lambda_WmK, "W/(mK)", GebaeudeFestwerte.LAMBDA_MIN_WMK, GebaeudeFestwerte.LAMBDA_MAX_WMK);

            if (s.IstLuftschicht)
            {
                // Luftschicht mit äquivalenter Leitfähigkeit: ρ darf unter dem Band liegen,
                // ohne ρ oder c_p trägt sie keine Kapazität.
                if (!double.IsNaN(s.Rohdichte_KgM3) && !ImBand(s.Rohdichte_KgM3, 0.0, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3))
                    throw SchichtFehler(wer, nummer, "ρ", s.Rohdichte_KgM3, "kg/m³", 0.0, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3);
                if (!double.IsNaN(s.Cp_JkgK) && !ImBand(s.Cp_JkgK, GebaeudeFestwerte.CP_MIN_JKGK, GebaeudeFestwerte.CP_MAX_JKGK))
                    throw SchichtFehler(wer, nummer, "c_p", s.Cp_JkgK, "J/(kgK)", GebaeudeFestwerte.CP_MIN_JKGK, GebaeudeFestwerte.CP_MAX_JKGK);
                return;
            }

            if (!ImBand(s.Rohdichte_KgM3, GebaeudeFestwerte.ROHDICHTE_MIN_KGM3, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3))
                throw SchichtFehler(wer, nummer, "ρ", s.Rohdichte_KgM3, "kg/m³", GebaeudeFestwerte.ROHDICHTE_MIN_KGM3, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3);
            if (!ImBand(s.Cp_JkgK, GebaeudeFestwerte.CP_MIN_JKGK, GebaeudeFestwerte.CP_MAX_JKGK))
                throw SchichtFehler(wer, nummer, "c_p", s.Cp_JkgK, "J/(kgK)", GebaeudeFestwerte.CP_MIN_JKGK, GebaeudeFestwerte.CP_MAX_JKGK);
        }

        /// <summary>
        /// Der Wärmedurchlasswiderstand einer Schicht [m²K/W]: d/λ, für eine ruhende Luftschicht
        /// der Wert aus DIN EN ISO 6946 Tabelle 8 (<see cref="Luftschichtwiderstand"/>).
        /// </summary>
        internal static double Waermedurchlasswiderstand(in Schicht s, Waermestromrichtung richtung, string wer = null, int nummer = 0)
            => s.IstRuhendeLuftschicht ? Luftschichtwiderstand(s.Dicke_M, richtung, wer, nummer) : s.Dicke_M / s.Lambda_WmK;

        /// <summary>
        /// Die flächenbezogene Wärmekapazität einer Schicht C = c_p·ρ·d [J/(m²K)] — Gl. (9);
        /// null für eine ruhende Luftschicht und für eine Luftschicht ohne ρ oder c_p.
        /// </summary>
        internal static double FlaechenbezogeneKapazitaet(in Schicht s)
        {
            if (s.IstLuftschicht && (double.IsNaN(s.Lambda_WmK) || double.IsNaN(s.Rohdichte_KgM3) || double.IsNaN(s.Cp_JkgK)))
                return 0.0;
            return s.Cp_JkgK * s.Rohdichte_KgM3 * s.Dicke_M;
        }

        /// <summary>
        /// Der Wärmedurchlasswiderstand einer ruhenden Luftschicht [m²K/W] — DIN EN ISO 6946,
        /// 6.9.2, Tabelle 8, linear nach der Dicke interpoliert, je Wärmestromrichtung. Über
        /// 0,3 m benannt abgelehnt (6.9.1).
        /// </summary>
        internal static double Luftschichtwiderstand(double dicke_M, Waermestromrichtung richtung, string wer = null, int nummer = 0)
        {
            if (!(dicke_M >= 0.0) || dicke_M > GebaeudeFestwerte.LUFTSCHICHT_DICKE_MAX_M)
                throw new GebaeudeModellException(GebaeudeModellFehler.SchichtUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_LUFTSCHICHT_DICKE, Wer(wer), Text(nummer),
                           Text(dicke_M), Text(GebaeudeFestwerte.LUFTSCHICHT_DICKE_MAX_M)));
            ReadOnlySpan<double> d = GebaeudeFestwerte.LUFTSCHICHT_DICKE_MM;
            ReadOnlySpan<double> r = richtung == Waermestromrichtung.Aufwaerts ? GebaeudeFestwerte.LUFTSCHICHT_R_AUFWAERTS
                                   : richtung == Waermestromrichtung.Abwaerts ? GebaeudeFestwerte.LUFTSCHICHT_R_ABWAERTS
                                   : GebaeudeFestwerte.LUFTSCHICHT_R_HORIZONTAL;
            double mm = dicke_M * 1000.0;
            int letzte = d.Length - 1;
            if (mm >= d[letzte]) return r[letzte];
            int i = 0;
            while (i < letzte - 1 && mm > d[i + 1]) i++;
            return r[i] + (r[i + 1] - r[i]) * (mm - d[i]) / (d[i + 1] - d[i]);
        }

        // =====================================================================
        //  Kettenmatrix und Ersatzgrößen (Gl. (1)–(17))
        // =====================================================================

        /// <summary>Die Kreisfrequenz ω = 2π/(86 400·T) [1/s] einer Bezugsperiode T in Tagen — Gl. (9).</summary>
        internal static double Kreisfrequenz(double periode_d)
        {
            if (!(periode_d > 0.0) || double.IsInfinity(periode_d))
                throw new ArgumentOutOfRangeException(nameof(periode_d), periode_d, "Die Bezugsperiode muss endlich und größer null sein.");
            return 2.0 * Math.PI / (GebaeudeFestwerte.SEKUNDEN_JE_TAG * periode_d);
        }

        /// <summary>
        /// Die Kettenmatrix EINER Schicht je Flächeneinheit (Gl. (1)–(8)) aus R [m²K/W],
        /// C [J/(m²K)] und ω [1/s]: a₁₁ = a₂₂ = cosh k, a₁₂ = R·sinh(k)/k, a₂₁ = jωC·sinh(k)/k mit
        /// k = (1 + j)·√(ωRC/2) — ohne Division durch R oder k.
        /// </summary>
        internal static Kettenmatrix Schichtmatrix(double r_M2KW, double c_JM2K, double omega)
        {
            double x = Math.Sqrt(0.5 * omega * r_M2KW * c_JM2K);
            var k = new Complex(x, x);
            Complex sk = SinhDurchArgument(k);
            Complex halb = Complex.Sinh(new Complex(0.5 * x, 0.5 * x));
            Complex ch1 = 2.0 * halb * halb;                                   // cosh k − 1
            return new Kettenmatrix(ch1, r_M2KW * sk, new Complex(0.0, omega * c_JM2K) * sk, ch1);
        }

        /// <summary>sinh(k)/k, für kleines |k| über die Reihe 1 + k²/6 + k⁴/120 + k⁶/5040 + k⁸/362 880.</summary>
        internal static Complex SinhDurchArgument(Complex k)
        {
            if (Complex.Abs(k) < REIHE_BIS)
            {
                Complex k2 = k * k;
                return Complex.One + k2 / 6.0 * (Complex.One + k2 / 20.0 * (Complex.One + k2 / 42.0 * (Complex.One + k2 / 72.0)));
            }
            return Complex.Sinh(k) / k;
        }

        /// <summary>
        /// Die Gesamtmatrix eines Aufbaus (Gl. (11)): das Produkt der Schichtmatrizen,
        /// <b>beginnend mit der raumseitigen Schicht</b> — die Reihenfolge ist nicht vertauschbar.
        /// </summary>
        internal static Kettenmatrix Gesamtmatrix(IReadOnlyList<Schicht> schichten, Waermestromrichtung richtung, double omega, string wer = null)
        {
            Pruefen(schichten, wer);
            Kettenmatrix m = Kettenmatrix.Einheit;
            for (int i = 0; i < schichten.Count; i++)
            {
                Schicht s = schichten[i];
                Kettenmatrix l = Schichtmatrix(Waermedurchlasswiderstand(s, richtung, wer, i + 1), FlaechenbezogeneKapazitaet(s), omega);
                m = m.Mal(l);
            }
            return m;
        }

        /// <summary>
        /// Die Ersatzgrößen eines Bauteils der Fläche A bei der Bezugsperiode T (Gl. (12)–(17)),
        /// mit a = Elemente der Gesamtmatrix je Flächeneinheit:
        /// <code>
        /// R₁ = (1/A)·((Re a₂₂ − 1)·Re a₁₂ + Im a₂₂·Im a₁₂) / ((Re a₂₂ − 1)² + (Im a₂₂)²)
        /// R₂ = (1/A)·((Re a₁₁ − 1)·Re a₁₂ + Im a₁₁·Im a₁₂) / ((Re a₁₁ − 1)² + (Im a₁₁)²)
        /// C₁ = A·((Re a₂₂ − 1)² + (Im a₂₂)²) / (ω·(Re a₁₂·Im a₂₂ − (Re a₂₂ − 1)·Im a₁₂))
        /// C₂ = A·((Re a₁₁ − 1)² + (Im a₁₁)²) / (ω·(Re a₁₂·Im a₁₁ − (Re a₁₁ − 1)·Im a₁₂))
        /// R₃ = (1/A)·Σ(d/λ) − R₁ − R₂,  R_W = R₁ + R₂ + R₃
        /// C₁,korr = (1/(ω·R₁))·(R_W·A − Re a₁₂·Re a₂₂ − Im a₁₂·Im a₂₂) / (Re a₂₂·Im a₁₂ − Re a₁₂·Im a₂₂)
        /// </code>
        /// so, wie sie die quelloffene Referenzumsetzung TEASER (RWTH-EBC) rechnet.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.BauteilreduktionUngueltig"/>,
        /// wenn R₁ oder C₁ nicht endlich und positiv ist (Aufbau ohne wirksame Speichermasse);
        /// <see cref="GebaeudeModellFehler.SchichtUngueltig"/>, <see cref="GebaeudeModellFehler.BauteilUngueltig"/>.</exception>
        internal static Bauteilkennwerte Reduzieren(IReadOnlyList<Schicht> schichten, double flaeche_M2, double periode_d,
                                                    Waermestromrichtung richtung, string wer = null)
        {
            if (!(flaeche_M2 > 0.0) || double.IsInfinity(flaeche_M2))
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_BAUTEIL_BEREICH, Wer(wer), "A", Text(flaeche_M2), "(0; ∞) m²"));
            double omega = Kreisfrequenz(periode_d);
            Kettenmatrix m = Gesamtmatrix(schichten, richtung, omega, wer);
            double summeR = 0.0;
            for (int i = 0; i < schichten.Count; i++) summeR += Waermedurchlasswiderstand(schichten[i], richtung, wer, i + 1);

            double a = flaeche_M2;
            double p11 = m.D11.Real, q11 = m.D11.Imaginary;
            double p22 = m.D22.Real, q22 = m.D22.Imaginary;
            double r12 = m.A12.Real, i12 = m.A12.Imaginary;
            double n11 = p11 * p11 + q11 * q11;
            double n22 = p22 * p22 + q22 * q22;

            double r1 = (1.0 / a) * (p22 * r12 + q22 * i12) / n22;
            double r2 = (1.0 / a) * (p11 * r12 + q11 * i12) / n11;
            double c1 = a * n22 / (omega * (r12 * q22 - p22 * i12));
            double c2 = a * n11 / (omega * (r12 * q11 - p11 * i12));
            double rw = summeR / a;
            double r3 = rw - r1 - r2;
            double re22 = 1.0 + p22;
            double c1korr = (1.0 / (omega * r1)) * ((rw * a - r12 * re22 - i12 * q22) / (re22 * i12 - r12 * q22));

            if (!IstPositivEndlich(r1) || !IstPositivEndlich(c1))
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilreduktionUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_REDUKTION_UNGUELTIG, Wer(wer), Text(r1), Text(c1)));

            return new Bauteilkennwerte(periode_d, r1, r2, r3, rw, c1, c2, c1korr);
        }

        /// <summary>
        /// Die Bezugsperiode eines Bauteils nach Gl. (10a)–(10d): R₁;rel = R₁(2 d)/R₁(7 d),
        /// C₁;rel = C₁(2 d)/C₁(7 d); T_BT = 2 d, wenn (R₁;rel &gt; 0,99 und C₁;rel &lt; 0,95) oder
        /// (R₁;rel &lt; 0,95 und C₁;rel &lt; 0,95 und |R₁;rel − C₁;rel| &gt; 0,30), sonst 7 d. Die Wahl
        /// fällt je Bauteil; sie entscheidet raumseitig abgedeckte Speichermassen (abgehängte
        /// Decken, Vorsatzschalen mit Luftschicht).
        /// </summary>
        internal static Bezugsperiodenwahl BezugsperiodeWaehlen(IReadOnlyList<Schicht> schichten, double flaeche_M2,
                                                                Waermestromrichtung richtung, string wer = null)
        {
            Bauteilkennwerte k7 = Reduzieren(schichten, flaeche_M2, GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D, richtung, wer);
            Bauteilkennwerte k2 = Reduzieren(schichten, flaeche_M2, GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D, richtung, wer);
            double rRel = k2.R1_KW / k7.R1_KW;
            double cRel = k2.C1_Jk / k7.C1_Jk;
            bool abgedeckt = (rRel > KRITERIUM_R1REL_OBEN && cRel < KRITERIUM_REL_UNTEN)
                             || (rRel < KRITERIUM_REL_UNTEN && cRel < KRITERIUM_REL_UNTEN && Math.Abs(rRel - cRel) > KRITERIUM_ABSTAND);
            return abgedeckt
                ? new Bezugsperiodenwahl(GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D, rRel, cRel, k2)
                : new Bezugsperiodenwahl(GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D, rRel, cRel, k7);
        }

        // =====================================================================
        //  Parallelschaltung zum Raum (Gl. (19)–(22))
        // =====================================================================

        /// <summary>
        /// Die Parallelschaltung der Bauteile einer Gruppe über ihre <b>komplexen</b> Widerstände
        /// (Gl. (19)–(22)) mit der Bezugsperiode des Raums T_RA (Gl. (10e), Vorgabe 5 d):
        /// Z = R₁ + 1/(jω_RA·C₁) je Bauteil, 1/Z = Σ 1/Z_μ, R₁ = Re Z, C₁ = 1/(ω_RA·(−Im Z)).
        /// Gleichwertig mit der Zweierform Gl. (23)/(24), nacheinander angewandt. Ein einzelnes
        /// Bauteil gibt seine Werte unverändert zurück.
        /// </summary>
        /// <param name="zweige">Je Bauteil R₁ [K/W] und C₁ [J/K] (für einseitig belastete Bauteile C₁,korr).</param>
        internal static (double R1_KW, double C1_Jk) Parallel(IReadOnlyList<(double R1_KW, double C1_Jk)> zweige,
                                                              double periode_d = GebaeudeFestwerte.BEZUGSPERIODE_RAUM_D)
        {
            if (zweige == null || zweige.Count == 0)
                throw new ArgumentException("Die Parallelschaltung braucht mindestens ein Bauteil.", nameof(zweige));
            if (zweige.Count == 1) return zweige[0];
            double omega = Kreisfrequenz(periode_d);
            Complex y = Complex.Zero;
            foreach ((double r, double c) in zweige)
                y += Complex.One / new Complex(r, -1.0 / (omega * c));
            Complex z = Complex.One / y;
            return (z.Real, 1.0 / (omega * -z.Imaginary));
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        private static GebaeudeModellException SchichtFehler(string wer, int nummer, string groesse, double wert, string einheit, double min, double max)
            => new GebaeudeModellException(GebaeudeModellFehler.SchichtUngueltig,
                Format(MyResource.Resource.SIMENG_G3_SCHICHT_STOFFWERT, Wer(wer), Text(nummer), groesse, Text(wert), einheit, Text(min), Text(max)));

        private static bool ImBand(double w, double min, double max) => w >= min && w <= max;

        private static bool IstPositivEndlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w) && w > 0.0;

        internal static string Wer(string wer) => string.IsNullOrEmpty(wer) ? "—" : wer;

        internal static string Format(string muster, params object[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);

        internal static string Text(double w) => w.ToString("G6", CultureInfo.InvariantCulture);

        private static string Text(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
