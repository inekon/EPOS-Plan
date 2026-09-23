using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Beschriftungen der drei Vorschaubilder des Zapfprofils (Umsetzungskonzept
    /// Zapfprofilgenerator 5.1, 5.6). Die Vorgabewerte sind der deutsche Rückfall; die Hülle
    /// setzt die Texte der Oberflächensprache (<c>ZPG_*</c>) — kein Probebild hängt an der
    /// Sprache des Rechners.
    /// </summary>
    public sealed class ZapfprofilBildtexte
    {
        /// <summary><c>ZPG_BILD_TAGESGANG</c> — {0} = Monat.</summary>
        public string TitelTagesgang { get; set; } = "Tagesgang der Zapfung, {0}";

        /// <summary><c>ZPG_BILD_WOCHENPROFIL</c></summary>
        public string TitelWochenprofil { get; set; } = "Woche mit dem größten Tagesbedarf";

        /// <summary><c>ZPG_BILD_JAHRESGANG</c></summary>
        public string TitelJahresgang { get; set; } = "Jahresgang Zapfung und Zirkulation";

        /// <summary><c>ZPG_TAGTYP_WERKTAG</c></summary>
        public string Werktag { get; set; } = "Werktag";

        /// <summary><c>ZPG_TAGTYP_SAMSTAG</c></summary>
        public string Samstag { get; set; } = "Samstag";

        /// <summary><c>ZPG_TAGTYP_SONNTAG</c></summary>
        public string SonnFeiertag { get; set; } = "Sonn-/Feiertag";

        /// <summary><c>ZPG_REIHE_ZAPFUNG</c></summary>
        public string Zapfung { get; set; } = "Zapfung";

        /// <summary><c>ZPG_REIHE_ZIRKULATION</c></summary>
        public string Zirkulation { get; set; } = "Zirkulation";

        /// <summary><c>ZPG_ACHSE_STUNDE</c></summary>
        public string AchseStunde { get; set; } = "Stunde";

        /// <summary><c>ZPG_ACHSE_WOCHENSTUNDE</c></summary>
        public string AchseWochenstunde { get; set; } = "Wochenstunde 1–168, Teilung alle 24 h";

        /// <summary><c>ZPG_ACHSE_LEISTUNG</c></summary>
        public string AchseLeistung { get; set; } = "Leistung [kW]";
    }

    /// <summary>
    /// <b>Die Zeichenbausteine der Zapfprofil-Vorschau</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.6; Stufe Z1, Gruppe 3): Tagesgang je Tagtyp, Wochenprofil über
    /// 168 Stunden, Jahresgang mit Zirkulationsanteil — Bauform wie
    /// <see cref="PeakShavingBild"/> und <see cref="SpeicherBetriebsbild"/>.
    ///
    /// <para><b>Kein eigener Renderer.</b> Tagesgang und Wochenprofil gehen durch
    /// <see cref="ChartRenderer.StundenprofileModell"/>, der Jahresgang durch
    /// <see cref="ChartRenderer.MonatsStapelModell"/> (gestapelt, Zapfung unten). Die Bausteine
    /// nehmen fertige Reihen in ihrer Einheit — kW je Stunde bzw. MWh je Monat — und rechnen
    /// nichts; die Reihen entstehen im Kern (<c>Zapfauswertung</c>) aus der Bilanzreihe des
    /// Laufs.</para>
    ///
    /// <para><b>Die Reihen nennen ihre Farbrolle</b> (DG-E5): Zapfung und Werktag
    /// <see cref="Farbrolle.WARMWASSER"/>, Samstag <see cref="Farbrolle.SERIE_6"/>,
    /// Sonn-/Feiertag <see cref="Farbrolle.SERIE_5"/>, Zirkulation
    /// <see cref="Farbrolle.SERIE_7"/> — gestrichelt, wo sie als Linie steht. Jede der vier
    /// Rollen trägt in der Vorgabepalette einen eigenen Farbwert; die Rückwärtssuche der
    /// Legende trifft sie deshalb eindeutig.</para>
    /// </summary>
    public static class ZapfprofilBilder
    {
        /// <summary>Zapfung — und der Werktag als ihr Regelfall.</summary>
        public static readonly Farbrolle RolleZapfung = Farbrolle.WARMWASSER;

        /// <summary>Der Samstag im Tagesgang.</summary>
        public static readonly Farbrolle RolleSamstag = Farbrolle.SERIE_6;

        /// <summary>Sonn- und Feiertag im Tagesgang.</summary>
        public static readonly Farbrolle RolleSonnFeiertag = Farbrolle.SERIE_5;

        /// <summary>Die Zirkulation als eigene Teilreihe.</summary>
        public static readonly Farbrolle RolleZirkulation = Farbrolle.SERIE_7;

        /// <summary>
        /// Der Tagesgang je Tagtyp als Bild: drei Tagesgänge zu 24 Stundenwerten [kW] und die
        /// Zirkulation [kW] gestrichelt. Eine fehlende Reihe (<c>null</c>) entfällt, eine
        /// Zirkulation aus lauter Nullen ebenso.
        /// </summary>
        public static byte[] Tagesgang(string monat, double[] werktagKw, double[] samstagKw,
                                       double[] sonnFeiertagKw, double[] zirkulationKw,
                                       ZapfprofilBildtexte texte)
            => SkiaMaler.Png(TagesgangModell(monat, werktagKw, samstagKw, sonnFeiertagKw, zirkulationKw, texte));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche.</summary>
        public static Zeichenmodell TagesgangModell(string monat, double[] werktagKw, double[] samstagKw,
                                                    double[] sonnFeiertagKw, double[] zirkulationKw,
                                                    ZapfprofilBildtexte texte)
        {
            texte ??= new ZapfprofilBildtexte();
            var reihen = new List<ChartRenderer.Reihe>();
            if (werktagKw != null) reihen.Add(new ChartRenderer.Reihe(texte.Werktag, werktagKw, RolleZapfung));
            if (samstagKw != null) reihen.Add(new ChartRenderer.Reihe(texte.Samstag, samstagKw, RolleSamstag));
            if (sonnFeiertagKw != null)
                reihen.Add(new ChartRenderer.Reihe(texte.SonnFeiertag, sonnFeiertagKw, RolleSonnFeiertag));
            // Die Zirkulation kommt nur zu einem Tagesgang dazu - allein wäre sie die Fläche.
            if (reihen.Count > 0 && Belegt(zirkulationKw))
                reihen.Add(Zirkulationslinie(texte, zirkulationKw));

            return ChartRenderer.StundenprofileModell(Format(texte.TitelTagesgang, monat), reihen, 6,
                                                      texte.AchseStunde, texte.AchseLeistung);
        }

        /// <summary>
        /// Das Wochenprofil als Bild: 168 Stundenwerte der Zapfung [kW] als Fläche, die
        /// Zirkulation [kW] gestrichelt; Teilung alle 24 Stunden.
        /// </summary>
        public static byte[] Wochenprofil(double[] zapfungKw, double[] zirkulationKw, ZapfprofilBildtexte texte)
            => SkiaMaler.Png(WochenprofilModell(zapfungKw, zirkulationKw, texte));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche.</summary>
        public static Zeichenmodell WochenprofilModell(double[] zapfungKw, double[] zirkulationKw,
                                                       ZapfprofilBildtexte texte)
        {
            texte ??= new ZapfprofilBildtexte();
            var reihen = new List<ChartRenderer.Reihe>();
            if (zapfungKw != null) reihen.Add(new ChartRenderer.Reihe(texte.Zapfung, zapfungKw, RolleZapfung));
            if (reihen.Count > 0 && Belegt(zirkulationKw))
                reihen.Add(Zirkulationslinie(texte, zirkulationKw));

            return ChartRenderer.StundenprofileModell(texte.TitelWochenprofil, reihen, 24,
                                                      texte.AchseWochenstunde, texte.AchseLeistung);
        }

        /// <summary>
        /// Der Jahresgang als Bild: zwölf Monatssäulen, gestapelt aus Zapfung (unten) und
        /// Zirkulation, beide in der Einheit <paramref name="einheit"/> (die Hülle gibt MWh).
        /// </summary>
        public static byte[] Jahresgang(double[] zapfungMonate, double[] zirkulationMonate, string einheit,
                                        ZapfprofilBildtexte texte)
            => SkiaMaler.Png(JahresgangModell(zapfungMonate, zirkulationMonate, einheit, texte));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche.</summary>
        public static Zeichenmodell JahresgangModell(double[] zapfungMonate, double[] zirkulationMonate,
                                                     string einheit, ZapfprofilBildtexte texte)
        {
            texte ??= new ZapfprofilBildtexte();
            var reihen = new List<ChartRenderer.Reihe>();
            if (zapfungMonate != null)
                reihen.Add(new ChartRenderer.Reihe(texte.Zapfung, zapfungMonate, RolleZapfung));
            if (Belegt(zirkulationMonate))
                reihen.Add(new ChartRenderer.Reihe(texte.Zirkulation, zirkulationMonate, RolleZirkulation));
            return ChartRenderer.MonatsStapelModell(texte.TitelJahresgang, einheit ?? "", reihen);
        }

        private static ChartRenderer.Reihe Zirkulationslinie(ZapfprofilBildtexte texte, double[] werte)
            => new ChartRenderer.Reihe(texte.Zirkulation, werte, RolleZirkulation,
                                       ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt);

        /// <summary>Trägt die Reihe einen Wert über null?</summary>
        private static bool Belegt(double[] werte)
        {
            if (werte == null) return false;
            foreach (double w in werte) if (w > 0) return true;
            return false;
        }

        private static string Format(string muster, string wert)
            => string.IsNullOrEmpty(muster) ? "" : muster.Replace("{0}", wert ?? "");
    }
}
