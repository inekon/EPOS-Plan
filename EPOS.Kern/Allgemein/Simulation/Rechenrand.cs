using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der ZAHLENRAND des Rechenkerns — <b>Anwenderentscheid W8‑O‑5d‑Q1 vom 07.09.2026</b>
    /// („Empfehlung"): Ein Vergleich an einer BETRIEBSSCHWELLE darf nicht am letzten Bit
    /// entscheiden.
    ///
    /// <para><b>Der Befund, aus dem er entstanden ist.</b> Mit W8‑O‑5d rechnet der ganze
    /// Kern in <c>double</c>. Die Jahressummen des Wärmebedarfs änderten sich dadurch nur
    /// um rund 1e‑5 relativ — elf der zwölf Referenzprojekte rissen trotzdem die Toleranz
    /// iF15, weil drei Schwellen des Modells eine Differenz im letzten Bit in eine ganze
    /// Fahrweise übersetzten und sie über Stunden weitertrugen (Begründung mit Zahlen im
    /// <c>protokoll.txt</c> der Basis <c>2026-09-07_R4_Double</c>). Zwei davon sind
    /// Vergleiche, die dieser Rand entschärft; die dritte — die drei <c>int</c>-Rückgaben
    /// in <see cref="WPPlan.Core.BhkwPlan"/> — ist mit W8‑O‑5d‑Q2 ganz gefallen.</para>
    ///
    /// <para><b>Was der Rand ist und was er nicht ist.</b> Er ist ein ZAHLENRAND, keine
    /// Fachtoleranz — dieselbe Lesart wie beim <c>1e-9</c> in
    /// <see cref="SimulationPufferspeicher.EntladefaehigkeitKanal"/>. Er ändert eine
    /// Entscheidung nur dort, wo zwei <c>double</c>-Werte um WENIGER als den Rand
    /// auseinanderliegen; jede fachlich unterscheidbare Differenz bleibt unberührt. Der
    /// Rand ist keine Rundung: Die Größen selbst werden nicht angefasst, nur der
    /// Vergleich.</para>
    ///
    /// <para><b>Warum absolut UND relativ.</b> Die Schwellen tragen Energien in kWh und
    /// spannen mehrere Größenordnungen — von der Abschaltschwelle eines 300‑l‑Puffers
    /// (rund 10 kWh) bis zur nutzbaren Kapazität eines Nahwärmespeichers (mehrere
    /// 10 000 kWh). Ein rein ABSOLUTER Rand wäre am oberen Ende zu knapp (bei 1e5 kWh
    /// misst ein ulp bereits 1,5e‑11, und die Schwelle entsteht aus mehreren Rechnungen),
    /// ein rein RELATIVER verschwände an einer Schwelle von 0. Deshalb beides:
    /// <c>Rand = <see cref="ABSOLUT"/> + <see cref="RELATIV"/> · |Schwelle|</c>.</para>
    ///
    /// <para><b>Die zwei Zahlen und ihre Begründung.</b>
    /// <list type="bullet">
    ///   <item><see cref="ABSOLUT"/> = 1e‑9 kWh = 3,6 µJ — die Untergrenze für Schwellen
    ///         nahe 0, zahlengleich zum bereits im Bestand stehenden Rand der
    ///         Schichttemperatur. Kleiner als jede Energiemenge, die in diesem Haus je
    ///         eine Rolle spielt: Der kleinste Betrag, den eine Referenz‑CSV ausweist,
    ///         liegt bei 1e‑9 kWh noch neun Stellen unter der Ausgabegenauigkeit
    ///         (<c>G9</c>).</item>
    ///   <item><see cref="RELATIV"/> = 1e‑12 — rund 4 500 ulp einer <c>double</c>-Zahl
    ///         (ein ulp ist 2,2e‑16 relativ). Das ist reichlich Luft für die Handvoll
    ///         Rechenschritte, aus denen eine Schwelle entsteht, und liegt zugleich vier
    ///         Größenordnungen unter der schärfsten Vergleichstoleranz der Referenzsuite
    ///         (rel. 1e‑4, <c>Vergleich.TOLERANZ_RELATIV</c>) — der Rand kann also keine
    ///         Abweichung erzeugen, die der Referenzvergleich noch sähe.</item>
    /// </list></para>
    ///
    /// <para><b>Hausregel dazu</b> steht in <c>EPOS.Kern/CLAUDE.md</c>, Abschnitt „Typen:
    /// der Rechenkern rechnet in <c>double</c>": Vergleiche an Betriebsschwellen tragen
    /// den Zahlenrand. Wer eine neue Schwelle einführt, nimmt
    /// <see cref="SchwelleErreicht"/> — nicht <c>&gt;=</c>.</para>
    /// </summary>
    public static class Rechenrand
    {
        /// <summary>
        /// Untergrenze des Rands [kWh bzw. Einheit der Schwelle] — sie trägt den
        /// Vergleich, wenn die Schwelle selbst 0 oder sehr klein ist. Zahlengleich zum
        /// Rand, den <see cref="SimulationPufferspeicher.EntladefaehigkeitKanal"/> seit
        /// dem Schichtmodell führt.
        /// </summary>
        public const double ABSOLUT = 1e-9;

        /// <summary>
        /// Anteil der Schwelle, der zum Rand beiträgt [–]. 1e‑12 ist rund 4 500 ulp und
        /// bleibt vier Größenordnungen unter der relativen Vergleichstoleranz der
        /// Referenzsuite.
        /// </summary>
        public const double RELATIV = 1e-12;

        /// <summary>
        /// Der Rand ZU einer Schwelle — <c><see cref="ABSOLUT"/> + <see cref="RELATIV"/>
        /// · |schwelle|</c>, immer positiv.
        /// </summary>
        /// <param name="schwelle">Die Schwelle, gegen die verglichen wird.</param>
        public static double Zu(double schwelle)
        {
            return ABSOLUT + RELATIV * Math.Abs(schwelle);
        }

        /// <summary>
        /// Hat <paramref name="wert"/> die <paramref name="schwelle"/> erreicht — bis auf
        /// den Zahlenrand? Das ist <c>wert &gt;= schwelle</c> mit dem Rand nach
        /// <see cref="Zu"/> auf der Schwellenseite.
        ///
        /// <para>Die Umkehrung („der Wert bleibt unter der Schwelle") ist die Verneinung
        /// dieses Ausdrucks — bewusst kein zweites Verfahren: Zwei getrennte Verfahren
        /// könnten in dem schmalen Band um die Schwelle beide wahr oder beide falsch
        /// werden.</para>
        /// </summary>
        /// <param name="wert">Der gemessene Stand (Füllstand, Wärmeraum, …).</param>
        /// <param name="schwelle">Die Schaltschwelle, in derselben Einheit.</param>
        public static bool SchwelleErreicht(double wert, double schwelle)
        {
            return wert >= schwelle - Zu(schwelle);
        }
    }
}
