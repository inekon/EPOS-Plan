using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Projekteinstellung der Aufheizoptimierung</b> (Entwurf KP3, Grundsatz 5; Schemaschritt
    /// KP-S2, <see cref="AufheizvorgabeSchema"/>) — unveränderlich: Schalter, Bemessung, Abzug, Reserve,
    /// Art. Gelesen je Lauf und Auskunft einmal (<see cref="KonfigurationCtrl.AufheizvorgabeLesen"/>),
    /// geschrieben in einem <c>UPDATE</c> (<see cref="KonfigurationCtrl.AufheizvorgabeSchreiben"/>).
    ///
    /// <para><b>Normalisierung (Festlegung 24).</b> Der Konstruktor hält die Form, in der die Datenbank
    /// die Einstellung führt: Die Bemessung (a) „kälteste Stunde" und die Art „täglich" werden
    /// <c>null</c> — dasselbe wie ihre Vorgabe, gespeichert als NULL (Muster Anlagenkopplung); ein
    /// leerer Text wird <c>null</c>; ein leeres Zahlenfeld (<c>null</c>, NaN, ±∞) bleibt bzw. wird
    /// <c>null</c>, ein getippter Wert bleibt, auch wenn er der Vorgabe gleicht (Muster
    /// <c>Kessel_Heizgrenze</c>). Der Schalter „aus" behält die übrigen Werte. Werte außerhalb der
    /// Prüfklauseln lässt der Konstruktor stehen — das Schreiben scheitert dann an der Spalte.</para>
    ///
    /// <para><b>Wirksame Werte.</b> <see cref="BemessungWirksam"/>, <see cref="AbzugWirksamK"/>,
    /// <see cref="ReserveWirksam"/> und <see cref="ArtWirksam"/> setzen die Vorgaben ein; die
    /// Rechnung liest nur sie.</para>
    /// </summary>
    public sealed record Aufheizvorgabe
    {
        /// <summary>Die Einstellung „aus" ohne gepflegte Werte — so liest sich eine fehlende Zeile oder Spalte.</summary>
        public static readonly Aufheizvorgabe Aus = new Aufheizvorgabe(false, null, null, null, null);

        /// <summary>Legt eine Einstellung an und normalisiert sie nach Festlegung 24.</summary>
        /// <param name="an">Der Projektschalter.</param>
        /// <param name="bemessung"><see cref="DbWerte.AUFHEIZ_BEMESSUNGEN"/>; <c>null</c> = (a).</param>
        /// <param name="abzugK">ΔT_K [K] der Bemessung (b); <c>null</c> = Vorgabe.</param>
        /// <param name="reserve">Die Aufheizreserve ρ als Anteil; <c>null</c> = Vorgabe.</param>
        /// <param name="art"><see cref="DbWerte.AUFHEIZ_ARTEN"/>; <c>null</c> = täglich.</param>
        public Aufheizvorgabe(bool an, string bemessung, double? abzugK, double? reserve, string art)
        {
            An = an;
            Bemessung = Text(bemessung, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE);
            AbzugK = Zahl(abzugK);
            Reserve = Zahl(reserve);
            Art = Text(art, DbWerte.AUFHEIZ_ART_TAEGLICH);
        }

        /// <summary>Der Projektschalter (<c>Tab_Einstellungen.Aufheizoptimierung</c>).</summary>
        public bool An { get; }

        /// <summary>Die Bemessung, wie gespeichert: <c>null</c> = (a) kälteste Stunde, sonst <see cref="DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG"/>.</summary>
        public string Bemessung { get; }

        /// <summary>ΔT_K [K], wie gespeichert; <c>null</c> = <see cref="AufheizvorgabeSchema.ABZUG_VORGABE_K"/>.</summary>
        public double? AbzugK { get; }

        /// <summary>Die Aufheizreserve ρ als Anteil, wie gespeichert; <c>null</c> = <see cref="AufheizvorgabeSchema.RESERVE_VORGABE"/>.</summary>
        public double? Reserve { get; }

        /// <summary>Die Art, wie gespeichert: <c>null</c> = täglich, sonst <see cref="DbWerte.AUFHEIZ_ART_FEST"/>.</summary>
        public string Art { get; }

        /// <summary>Die wirksame Bemessung (<see cref="DbWerte.AUFHEIZ_BEMESSUNG_STUNDE"/> statt <c>null</c>).</summary>
        public string BemessungWirksam => Bemessung ?? DbWerte.AUFHEIZ_BEMESSUNG_STUNDE;

        /// <summary>Bemisst die Einstellung an der kältesten Stunde abzüglich ΔT_K (Variante (b))?</summary>
        public bool MitAbzug => BemessungWirksam == DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG;

        /// <summary>Die wirksame ΔT_K [K].</summary>
        public double AbzugWirksamK => AbzugK ?? AufheizvorgabeSchema.ABZUG_VORGABE_K;

        /// <summary>Die wirksame Aufheizreserve ρ als Anteil.</summary>
        public double ReserveWirksam => Reserve ?? AufheizvorgabeSchema.RESERVE_VORGABE;

        /// <summary>Die wirksame Art (<see cref="DbWerte.AUFHEIZ_ART_TAEGLICH"/> statt <c>null</c>).</summary>
        public string ArtWirksam => Art ?? DbWerte.AUFHEIZ_ART_TAEGLICH;

        /// <summary>Gilt jeden Tag die bemessene Aufheizzeit (Art „fest")?</summary>
        public bool IstFest => ArtWirksam == DbWerte.AUFHEIZ_ART_FEST;

        /// <summary>Ein Text der Datenbank: leer und die Vorgabe werden <c>null</c>, sonst ohne Rand.</summary>
        private static string Text(string wert, string vorgabe)
        {
            if (string.IsNullOrWhiteSpace(wert)) return null;
            string t = wert.Trim();
            return string.Equals(t, vorgabe, StringComparison.Ordinal) ? null : t;
        }

        /// <summary>Eine Zahl der Datenbank: <c>null</c>, NaN und ±∞ heißen „leer".</summary>
        private static double? Zahl(double? wert)
            => wert.HasValue && !double.IsNaN(wert.Value) && !double.IsInfinity(wert.Value) ? wert : null;
    }
}
