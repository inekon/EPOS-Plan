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
    /// Die Beschriftungen der drei Bilder der Überlagerung „Auslegung" (Umsetzungskonzept
    /// Zapfprofilgenerator 5.6; Stufe Z2, Gruppe 2): Summenlinie des Bedarfstags, Wertepaarkurve
    /// und maßgebende Woche der Stundenbilanz. Die Vorgabewerte sind der deutsche Rückfall; die
    /// Hülle setzt die Texte der Oberflächensprache (<c>ZPG_*</c>).
    /// </summary>
    public sealed class ZapfprofilAuslegungBildtexte
    {
        /// <summary><c>ZPG_AUSBILD_SUMMENLINIE</c></summary>
        public string TitelSummenlinie { get; set; } = "Summenlinie des Bedarfstags";

        /// <summary><c>ZPG_AUSBILD_BEDARF</c></summary>
        public string Bedarf { get; set; } = "Bedarf kumuliert";

        /// <summary><c>ZPG_AUSBILD_VERSORGUNG</c></summary>
        public string Versorgung { get; set; } = "Versorgung kumuliert";

        /// <summary><c>ZPG_AUSBILD_SPEICHERINHALT</c></summary>
        public string Speicherinhalt { get; set; } = "Speicherinhalt";

        /// <summary><c>ZPG_AUSBILD_BERUEHRUNG</c></summary>
        public string Beruehrung { get; set; } = "kleinster Abstand";

        /// <summary><c>ZPG_AUSBILD_ACHSE_MINUTENTAKT</c></summary>
        public string AchseMinutentakt { get; set; } = "Stunde (Minutentakt)";

        /// <summary><c>ZPG_AUSBILD_ACHSE_ENERGIE</c></summary>
        public string AchseEnergie { get; set; } = "Energie [kWh]";

        /// <summary><c>ZPG_AUSBILD_WERTEPAARE</c></summary>
        public string TitelWertepaare { get; set; } = "Wertepaarkurve Speichervolumen über Leistung";

        /// <summary><c>ZPG_AUSBILD_REIHE_WERTEPAARE</c></summary>
        public string Wertepaare { get; set; } = "Wertepaare (Erweiterung des Nachweisverfahrens)";

        /// <summary><c>ZPG_AUSBILD_GEWAEHLT</c></summary>
        public string Gewaehlt { get; set; } = "gewählt";

        /// <summary><c>ZPG_ACHSE_LEISTUNG</c></summary>
        public string AchseLeistung { get; set; } = "Leistung [kW]";

        /// <summary><c>ZPG_AUSBILD_ACHSE_VOLUMEN</c></summary>
        public string AchseVolumen { get; set; } = "Speichervolumen [l]";

        /// <summary><c>ZPG_AUSBILD_WOCHE</c></summary>
        public string TitelWoche { get; set; } = "Maßgebende Woche der Stundenbilanz";

        /// <summary><c>ZPG_REIHE_ZAPFUNG</c></summary>
        public string Zapfung { get; set; } = "Zapfung";

        /// <summary><c>ZPG_REIHE_ZIRKULATION</c></summary>
        public string Zirkulation { get; set; } = "Zirkulation";

        /// <summary><c>ZPG_AUSBILD_LADUNG</c></summary>
        public string Ladung { get; set; } = "Ladung";

        /// <summary><c>ZPG_AUSBILD_DEFIZIT</c></summary>
        public string Defizit { get; set; } = "Defizit kumuliert";

        /// <summary><c>ZPG_AUSBILD_FUELLSTAND</c> — {0} = Volumen.</summary>
        public string Fuellstand { get; set; } = "Füllstand bei {0} l";

        /// <summary><c>ZPG_AUSBILD_MASSGEBEND</c></summary>
        public string Massgebend { get; set; } = "maßgebend";

        /// <summary><c>ZPG_ACHSE_WOCHENSTUNDE</c></summary>
        public string AchseWochenstunde { get; set; } = "Wochenstunde 1–168, Teilung alle 24 h";

        /// <summary><c>ZPG_AUSBILD_ACHSE_SPEICHER</c></summary>
        public string AchseSpeicher { get; set; } = "Speicher [kWh]";
    }

    /// <summary>Die Kurven der Summenlinie [kWh] über 1 441 Minutenwerte und die Minute des kleinsten Abstands.</summary>
    internal sealed record Summenlinienkurven(double[] BedarfKwh, double[] VersorgungKwh, int BeruehrungMinute);

    /// <summary>Die Reihen des Wochenbilds der Stundenbilanz (168 Stunden der Woche 2) und die maßgebende Stunde.</summary>
    internal sealed record Wochenbildreihen(double[] ZapfungKw, double[] ZirkulationKw, double[] LadungKw,
                                            double[] DefizitKwh, double[] FuellstandKwh, int? MassgebendStunde);

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

        // =================================================================================
        // Die Bilder der Auslegung (Stufe Z2, Gruppe 2)
        // =================================================================================

        /// <summary>Die Versorgungslinie der Summenlinie und die Ladung der Stundenbilanz.</summary>
        public static readonly Farbrolle RolleVersorgung = Farbrolle.SPEICHERLADUNG;

        /// <summary>Die Wertepaarkurve.</summary>
        public static readonly Farbrolle RolleWertepaare = Farbrolle.STAMM;

        /// <summary>Das kumulierte Defizit der Stundenbilanz (zweite Achse).</summary>
        public static readonly Farbrolle RolleDefizit = Farbrolle.SERIE_4;

        /// <summary>Der Füllstand des Speichers (zweite Achse).</summary>
        public static readonly Farbrolle RolleFuellstand = Farbrolle.SPEICHERFUELLSTAND;

        /// <summary>
        /// Die Summenlinie des Bedarfstags als Bild: die kumulierte Bedarfslinie und die
        /// Versorgungslinie (Speicherinhalt plus kumulierte Nettoversorgung) über 1 441
        /// Minutenwerte [kWh], der Speicherinhalt als Strecke am Tagesbeginn und der kleinste
        /// Abstand als Punkt bei <paramref name="beruehrungMinute"/>.
        /// </summary>
        public static byte[] Summenlinie(double[] bedarfKwh, double[] versorgungKwh, int beruehrungMinute,
                                         ZapfprofilAuslegungBildtexte texte)
            => SkiaMaler.Png(SummenlinieModell(bedarfKwh, versorgungKwh, beruehrungMinute, texte));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche.</summary>
        public static Zeichenmodell SummenlinieModell(double[] bedarfKwh, double[] versorgungKwh, int beruehrungMinute,
                                                      ZapfprofilAuslegungBildtexte texte)
        {
            texte ??= new ZapfprofilAuslegungBildtexte();
            var reihen = new List<ChartRenderer.Reihe>();
            if (bedarfKwh != null) reihen.Add(new ChartRenderer.Reihe(texte.Bedarf, bedarfKwh, RolleZapfung));
            if (versorgungKwh != null) reihen.Add(new ChartRenderer.Reihe(texte.Versorgung, versorgungKwh, RolleVersorgung));
            var marken = new List<ChartRenderer.Linienmarke>();
            if (bedarfKwh != null && versorgungKwh != null && bedarfKwh.Length > 0 && versorgungKwh.Length == bedarfKwh.Length)
            {
                marken.Add(new ChartRenderer.Linienmarke(0.0, bedarfKwh[0], versorgungKwh[0], texte.Speicherinhalt));
                if (beruehrungMinute >= 0 && beruehrungMinute < bedarfKwh.Length)
                    marken.Add(new ChartRenderer.Linienmarke(beruehrungMinute, null, bedarfKwh[beruehrungMinute],
                                                             texte.Beruehrung, true));
            }
            return ChartRenderer.SummenlinieModell(texte.TitelSummenlinie, reihen, null, 6 * Bedarfstag.MINUTEN_JE_STUNDE,
                                                   Bedarfstag.MINUTEN_JE_STUNDE, texte.AchseMinutentakt, texte.AchseEnergie,
                                                   null, null, marken);
        }

        /// <summary>
        /// Die Wertepaarkurve als Bild: das kleinste Speichervolumen [l] über der Leistung [kW]
        /// (eigene Erweiterung des Nachweisverfahrens), aufsteigend nach der Leistung geordnet,
        /// und der gewählte Punkt als Kreis. Weniger als zwei Paare: der Leerhinweis.
        /// </summary>
        public static byte[] Wertepaarkurve(double[] leistungKw, double[] volumenL, double? punktLeistungKw,
                                            double? punktVolumenL, ZapfprofilAuslegungBildtexte texte)
            => SkiaMaler.Png(WertepaarkurveModell(leistungKw, volumenL, punktLeistungKw, punktVolumenL, texte));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche.</summary>
        public static Zeichenmodell WertepaarkurveModell(double[] leistungKw, double[] volumenL, double? punktLeistungKw,
                                                         double? punktVolumenL, ZapfprofilAuslegungBildtexte texte)
        {
            texte ??= new ZapfprofilAuslegungBildtexte();
            int n = leistungKw == null || volumenL == null ? 0 : System.Math.Min(leistungKw.Length, volumenL.Length);
            var paare = new List<(double X, double Y)>(n);
            for (int i = 0; i < n; i++) paare.Add((leistungKw[i], volumenL[i]));
            paare.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            var x = new double[paare.Count];
            var y = new double[paare.Count];
            for (int i = 0; i < paare.Count; i++) { x[i] = paare[i].X; y[i] = paare[i].Y; }

            var reihen = new List<ChartRenderer.Reihe> { new ChartRenderer.Reihe(texte.Wertepaare, y, RolleWertepaare) };
            var marken = new List<ChartRenderer.Linienmarke>();
            if (punktLeistungKw.HasValue && punktVolumenL.HasValue)
                marken.Add(new ChartRenderer.Linienmarke(punktLeistungKw.Value, null, punktVolumenL.Value, texte.Gewaehlt, true));
            return ChartRenderer.SummenlinieModell(texte.TitelWertepaare, reihen, x, 0.0, 1.0, texte.AchseLeistung,
                                                   texte.AchseVolumen, null, null, marken);
        }

        /// <summary>
        /// Die maßgebende Woche der Stundenbilanz als Bild: Zapfung als Fläche, Zirkulation
        /// gestrichelt und Ladung je Stunde [kW] links; das kumulierte Defizit und der Füllstand
        /// beim Bezugsvolumen [kWh] auf der zweiten Achse; der maßgebende Zeitpunkt als Strecke.
        /// Eine Reihe <c>null</c> (oder eine Zirkulation und Ladung aus lauter Nullen) entfällt.
        /// </summary>
        public static byte[] Auslegungswoche(double[] zapfungKw, double[] zirkulationKw, double[] ladungKw,
                                             double[] defizitKwh, double[] fuellstandKwh, double? fuellstandBezugL,
                                             int? massgebendStunde, ZapfprofilAuslegungBildtexte texte)
            => SkiaMaler.Png(AuslegungswocheModell(zapfungKw, zirkulationKw, ladungKw, defizitKwh, fuellstandKwh,
                                                   fuellstandBezugL, massgebendStunde, texte));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche.</summary>
        public static Zeichenmodell AuslegungswocheModell(double[] zapfungKw, double[] zirkulationKw, double[] ladungKw,
                                                          double[] defizitKwh, double[] fuellstandKwh, double? fuellstandBezugL,
                                                          int? massgebendStunde, ZapfprofilAuslegungBildtexte texte)
        {
            texte ??= new ZapfprofilAuslegungBildtexte();
            var links = new List<ChartRenderer.Reihe>();
            if (zapfungKw != null)
                links.Add(new ChartRenderer.Reihe(texte.Zapfung, zapfungKw, RolleZapfung, ChartRenderer.Stapelart.Flaeche));
            if (links.Count > 0 && Belegt(zirkulationKw))
                links.Add(new ChartRenderer.Reihe(texte.Zirkulation, zirkulationKw, RolleZirkulation,
                                                  ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt));
            if (links.Count > 0 && Belegt(ladungKw))
                links.Add(new ChartRenderer.Reihe(texte.Ladung, ladungKw, RolleVersorgung));

            var rechts = new List<ChartRenderer.Reihe>();
            if (defizitKwh != null) rechts.Add(new ChartRenderer.Reihe(texte.Defizit, defizitKwh, RolleDefizit));
            if (fuellstandKwh != null)
            {
                string bezug = fuellstandBezugL.HasValue
                    ? fuellstandBezugL.Value.ToString("N0", System.Globalization.CultureInfo.CurrentCulture) : "";
                rechts.Add(new ChartRenderer.Reihe(Format(texte.Fuellstand, bezug), fuellstandKwh, RolleFuellstand));
            }

            int n = zapfungKw?.Length ?? 0;
            var x = new double[n];
            for (int i = 0; i < n; i++) x[i] = i + 1;
            var marken = new List<ChartRenderer.Linienmarke>();
            if (massgebendStunde.HasValue && massgebendStunde.Value >= 1 && massgebendStunde.Value <= n)
            {
                double oben = 0.0;
                foreach (double w in zapfungKw) if (w > oben) oben = w;
                marken.Add(new ChartRenderer.Linienmarke(massgebendStunde.Value, 0.0, oben, texte.Massgebend));
            }
            return ChartRenderer.SummenlinieModell(texte.TitelWoche, links, x, Zapfkalender.STUNDEN_TAG, 1.0,
                                                   texte.AchseWochenstunde, texte.AchseLeistung, rechts,
                                                   texte.AchseSpeicher, marken);
        }

        // --- Die Reihen der Bilder aus den Ergebnissen des Kerns (die Hülle rechnet nichts) ----

        /// <summary>
        /// Die Kurven der Summenlinie aus Bedarfstag und Nachweis am Auslegungspunkt:
        /// Bedarf(i) = Σ q(k) für k &lt; i, Versorgung(i) = Q(i) + Bedarf(i) für i = 0 … 1440, und
        /// die Minute nach dem kleinsten Abstand Q(i) − q(i) (dort berührt der Bedarf die
        /// Versorgung). <c>null</c>, wenn der Nachweis keinen Verlauf trägt.
        /// </summary>
        internal static Summenlinienkurven SummenlinieKurven(Bedarfstag tag, Summenliniennachweis nachweis)
        {
            if (tag == null || nachweis?.InhaltKwh == null || nachweis.InhaltKwh.Count != Bedarfstag.MINUTEN + 1) return null;
            IReadOnlyList<double> q = tag.MinutenKwh;
            var bedarf = new double[Bedarfstag.MINUTEN + 1];
            var versorgung = new double[Bedarfstag.MINUTEN + 1];
            double summe = 0.0, kleinster = double.PositiveInfinity;
            int beruehrung = 0;
            for (int i = 0; i <= Bedarfstag.MINUTEN; i++)
            {
                bedarf[i] = summe;
                versorgung[i] = nachweis.InhaltKwh[i] + summe;
                if (i < Bedarfstag.MINUTEN)
                {
                    double abstand = nachweis.InhaltKwh[i] - q[i];
                    if (abstand < kleinster)
                    {
                        kleinster = abstand;
                        beruehrung = i + 1;
                    }
                    summe += q[i];
                }
            }
            return new Summenlinienkurven(bedarf, versorgung, beruehrung);
        }

        /// <summary>
        /// Die Reihen des Wochenbilds aus Wochenreihe und Speicherauslegung: Zapfung [kW] der
        /// 168 Stunden, Zirkulation und Ladung je Stunde aus angesetzter Leistung und Fenster, das
        /// Defizit D(t) der Woche 2, der Füllstand max(0, C_sp − D(t)) beim Bezugsvolumen (ohne
        /// Bezug <c>null</c>) und die maßgebende Stunde 1 … 168 der Woche 2 (ohne Defizit <c>null</c>).
        /// </summary>
        internal static Wochenbildreihen Wochenreihen(Wochenreihe woche, Speicherauslegungsergebnis sa)
        {
            if (woche == null || sa == null || sa.DefizitKwh == null
                || sa.DefizitKwh.Count != 2 * Wochenreihe.STUNDEN) return null;
            int n = Wochenreihe.STUNDEN;
            var zapfung = new double[n];
            var zirk = new double[n];
            var ladung = new double[n];
            var defizit = new double[n];
            double[] fuell = sa.KapazitaetKwh.HasValue ? new double[n] : null;
            double zirkKw = sa.Zirkulation.Angesetzt, ladeKw = sa.Ladeleistung.Angesetzt;
            for (int t = 0; t < n; t++)
            {
                int stunde = t % Zapfkalender.STUNDEN_TAG;
                zapfung[t] = woche.StundenKwh[t];
                zirk[t] = zirkKw * sa.ZirkulationLaufzeit.AnteilStunde(stunde);
                ladung[t] = ladeKw * sa.Ladefenster.AnteilStunde(stunde);
                defizit[t] = sa.DefizitKwh[n + t];
                if (fuell != null)
                {
                    double soc = sa.KapazitaetKwh.Value - defizit[t];
                    fuell[t] = soc > 0 ? soc : 0.0;
                }
            }
            int? massgebend = sa.ZeitpunktStunde.HasValue ? sa.ZeitpunktStunde.Value - n : (int?)null;
            return new Wochenbildreihen(zapfung, zirk, ladung, defizit, fuell, massgebend);
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
