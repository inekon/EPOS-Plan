using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Punkt einer Kesselkurve: die Laststufe β (Anteil der Nennleistung) und der Wirkungsgrad dort (Faktor, Hi).</summary>
    public readonly record struct Kesselkurvenpunkt(double Laststufe, double Wirkungsgrad);

    /// <summary>
    /// Eine Kurve η(β) des Heizkessels: bei der Brennwertkennlinie je Rücklauf eine (<see cref="RuecklaufC"/> in °C),
    /// sonst die eine Teillastkennlinie (<see cref="RuecklaufC"/> = <c>null</c>).
    /// </summary>
    public sealed record Kesselkurve(double? RuecklaufC, IReadOnlyList<Kesselkurvenpunkt> Punkte);

    /// <summary>
    /// <b>Die Kurve des Katalogeditors</b> (Konzept Kesselkennlinie 5, erster Punkt): der Wirkungsgrad über der Last aus
    /// DENSELBEN Funktionen wie der Lauf — Nennwirkungsgrad nach Brennstoff, wirksames η₃₀ (gepflegt oder Normvorgabe nach
    /// 7.1), Teillastkurve <see cref="Eta"/> und Brennwertkennlinie <see cref="EtaBrennwert"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Eine Auskunft ruft den Rechenweg des Laufs.</b> <c>SimulationSPK</c> nimmt denselben
    /// <see cref="Nennwirkungsgrad"/> und dieselbe Prozentregel (<see cref="WirkungsgradAlsFaktor"/>); die Kurve rechnet
    /// je Stützstelle, was eine Laufstunde mit dieser Laststufe und diesem Rücklauf rechnete.</para>
    /// <para><b>Kurven.</b> Mit Brennwertkennlinie eine je Rücklauf aus <see cref="KURVE_RUECKLAEUFE_C"/> (30, 50 und
    /// 60 °C), sonst genau eine — ohne Brennwertkennlinie wirkt der Rücklauf nicht. Der Elektrokessel rechnet ohne
    /// Kennlinie: eine flache Kurve bei η₁₀₀.</para>
    /// <para><b>Stützstellen</b> bei 10 %, 20 % … 100 % der Nennleistung (<see cref="KURVE_STUETZSTELLEN"/>); der Knick der
    /// Teillastkurve bei 30 % ist eine davon.</para>
    /// </remarks>
    public static partial class Kesselkennlinie
    {
        /// <summary>Der Nennwirkungsgrad, wenn der Katalog keinen führt (Wert ≤ 0) — der Rückfall des Laufs.</summary>
        public const double NENNWIRKUNGSGRAD_RUECKFALL = 0.90;

        /// <summary>Die Rückläufe der Editorkurven mit Brennwertkennlinie [°C] (Konzept 5): Prüfpunkt, Rückfall, über dem Taupunkt.</summary>
        public static readonly IReadOnlyList<double> KURVE_RUECKLAEUFE_C = new[] { 30.0, 50.0, 60.0 };

        /// <summary>Stützstellen einer Editorkurve: β = 0,1, 0,2 … 1,0.</summary>
        public const int KURVE_STUETZSTELLEN = 10;

        /// <summary>Ist der Brennstoff ein Heizöl (<c>Tab_Brennstoff_Stamm</c> 6–9, 18–22)? Dann gilt das Ölfeld des Katalogs.</summary>
        public static bool IstOel(int brennstoffArt)
            => brennstoffArt >= 6 && brennstoffArt <= 9 || brennstoffArt >= 18 && brennstoffArt <= 22;

        /// <summary>
        /// Ein Wirkungsgrad des Katalogs als Faktor: über <see cref="KesselKennlinieWerte.PROZENTSCHWELLE"/> gilt er als
        /// Prozentangabe und wird durch 100 geteilt — die Regel, mit der der Lauf die Wirkungsgrade einliest.
        /// </summary>
        public static double WirkungsgradAlsFaktor(double wert)
            => wert > KesselKennlinieWerte.PROZENTSCHWELLE ? wert / 100.0 : wert;

        /// <summary>
        /// Der NENNWIRKUNGSGRAD η₁₀₀, mit dem ein Kessel rechnet: das Ölfeld beim Heizöl, sonst das Gasfeld (beide schon als
        /// Faktor), <see cref="NENNWIRKUNGSGRAD_RUECKFALL"/> für einen Wert ≤ 0.
        /// </summary>
        public static double Nennwirkungsgrad(double wirkungsgradGas, double wirkungsgradOel, int brennstoffArt)
        {
            double eta100 = IstOel(brennstoffArt) ? wirkungsgradOel : wirkungsgradGas;
            return eta100 <= 0 ? NENNWIRKUNGSGRAD_RUECKFALL : eta100;
        }

        /// <summary>
        /// DIE KURVEN DES KATALOGEDITORS aus den Feldern eines Katalogsatzes — auch ungespeicherten: η über der Laststufe,
        /// mit Brennwertkennlinie je Rücklauf aus <see cref="KURVE_RUECKLAEUFE_C"/>, sonst eine.
        /// </summary>
        /// <param name="wirkungsgradGas">Wirkungsgrad Gas des Katalogs (Faktor oder Prozent, <see cref="WirkungsgradAlsFaktor"/>).</param>
        /// <param name="wirkungsgradOel">Wirkungsgrad Öl des Katalogs (Faktor oder Prozent).</param>
        /// <param name="brennstoffArt">Energieträger (<c>Tab_Brennstoff_Stamm</c>).</param>
        /// <param name="brennwert">Schalter Brennwertkessel.</param>
        /// <param name="beschreibung">Beschreibung — sie trägt die Bauart „Standard…“ (<see cref="Bauart"/>).</param>
        /// <param name="eta30Gepflegt">η₃₀ des Katalogs; leer = Normvorgabe (7.1).</param>
        /// <param name="kennlinieBrennwert">Schalter Brennwertkennlinie.</param>
        public static IReadOnlyList<Kesselkurve> Kurven(double wirkungsgradGas, double wirkungsgradOel, int brennstoffArt,
                                                        bool brennwert, string beschreibung, double? eta30Gepflegt,
                                                        bool kennlinieBrennwert)
        {
            double eta100 = Nennwirkungsgrad(WirkungsgradAlsFaktor(wirkungsgradGas), WirkungsgradAlsFaktor(wirkungsgradOel),
                                             brennstoffArt);
            if (!RechnetMitKennlinie(brennstoffArt))
                return new[] { new Kesselkurve(null, Stuetzstellen(b => eta100)) };

            double eta30 = Eta30Wirksam(eta30Gepflegt, eta100, Bauart(brennwert, beschreibung), brennstoffArt);
            if (!RechnetMitBrennwertkennlinie(brennwert, kennlinieBrennwert, brennstoffArt))
                return new[] { new Kesselkurve(null, Stuetzstellen(b => Eta(b, eta100, eta30))) };

            var kurven = new List<Kesselkurve>(KURVE_RUECKLAEUFE_C.Count);
            foreach (double ruecklauf in KURVE_RUECKLAEUFE_C)
            {
                double t = ruecklauf;
                kurven.Add(new Kesselkurve(t, Stuetzstellen(b => EtaBrennwert(b, eta100, eta30, t, brennstoffArt, out _))));
            }
            return kurven;
        }

        /// <summary>Die Stützstellen β = i / <see cref="KURVE_STUETZSTELLEN"/> für i = 1 … <see cref="KURVE_STUETZSTELLEN"/>.</summary>
        private static IReadOnlyList<Kesselkurvenpunkt> Stuetzstellen(Func<double, double> eta)
        {
            var punkte = new Kesselkurvenpunkt[KURVE_STUETZSTELLEN];
            for (int i = 1; i <= KURVE_STUETZSTELLEN; i++)
            {
                double beta = (double)i / KURVE_STUETZSTELLEN;
                punkte[i - 1] = new Kesselkurvenpunkt(beta, eta(beta));
            }
            return punkte;
        }
    }
}
