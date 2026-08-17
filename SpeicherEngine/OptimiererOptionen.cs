using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Betriebsstrategie, mit der die Rastersuche jeden Punkt rechnet
    /// (Fachkonzept 6.1 / 6.2).
    /// </summary>
    /// <remarks>
    /// Bewusst eine Aufzaehlung statt einer <see cref="ISpeicherStrategie"/>-Instanz in
    /// den Optionen: Die Rastersuche verteilt die Punkte ueber <c>Parallel.For</c> und
    /// darf deshalb nur Strategien verwenden, deren Zustandsfreiheit belegt ist
    /// (Fachkonzept 8.1). Eine von aussen hereingereichte Implementierung koennte diese
    /// Zusage nicht einhalten; der Optimierer erzeugt die Strategie deshalb selbst.
    /// Peak-Shaving fehlt in der Liste, weil es einer anderen Zielgroesse folgt
    /// (Lastspitze statt Residuallast) und eine eigene Maske hat (AP7).
    /// </remarks>
    public enum OptimiererStrategie
    {
        /// <summary>Dauernutzung im energetischen Produktivmodus (Fachkonzept 6.2).</summary>
        Dauernutzung = 0,

        /// <summary>Nachtnutzung im energetischen Produktivmodus (Fachkonzept 6.1).</summary>
        Nachtnutzung = 1
    }

    /// <summary>
    /// Suchraum und Schalter der Auslegungsoptimierung (Fachkonzept 6.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Vorbelegungen sind die Vorschlagswerte des Fachkonzepts: Kapazitaet
    /// 500 … 5.000 kWh mit 10 Stuetzstellen, C-Rate 0,5 … 3,0 in 0,5er-Schritten
    /// (6 Stuetzstellen), zweistufig. Das ergibt 2 * 10 * 6 = 120 Jahreslaeufe.
    /// </para>
    /// <para>
    /// Der Typ ist ein <c>record</c> mit ausschliesslich <c>init</c>-Settern und damit
    /// nach der Konstruktion unveraenderlich - Voraussetzung dafuer, dass die
    /// Rastersuche ihn gefahrlos ueber alle Threads liest.
    /// </para>
    /// </remarks>
    public sealed record OptimiererOptionen
    {
        // ---------------------------------------------------------- Kapazitaetsachse

        /// <summary>Untere Grenze der Kapazitaetsachse C_min [kWh], Default 500.</summary>
        public double CMinKwh { get; init; } = 500.0;

        /// <summary>Obere Grenze der Kapazitaetsachse C_max [kWh], Default 5.000.</summary>
        public double CMaxKwh { get; init; } = 5000.0;

        /// <summary>
        /// Anzahl der Stuetzstellen auf der Kapazitaetsachse, Default 10; mindestens 2.
        /// </summary>
        /// <remarks>
        /// Die Achse laeuft <c>C_i = C_min + (C_max - C_min) * i / (n - 1)</c>, endet
        /// also einschliesslich auf <see cref="CMaxKwh"/>. Bei n = 1 waere der Schritt
        /// nicht definiert - deshalb die Untergrenze 2.
        /// </remarks>
        public int Stuetzstellen { get; init; } = 10;

        // ------------------------------------------------------------- C-Raten-Achse

        /// <summary>Untere Grenze der C-Rate r_min [1/h], Default 0,5.</summary>
        public double RMin { get; init; } = 0.5;

        /// <summary>Obere Grenze der C-Rate r_max [1/h], Default 3,0.</summary>
        public double RMax { get; init; } = 3.0;

        /// <summary>Schrittweite der C-Rate [1/h], Default 0,5.</summary>
        /// <remarks>
        /// Die Achse ist schrittweiten- und nicht stuetzstellengesteuert, weil die
        /// C-Rate eine gerundete Kenngroesse ist: "0,5 / 1,0 / 1,5 …" ist die Sprache
        /// des Datenblatts, "0,5 / 0,9166 / 1,3333 …" nicht. Die Vorlage
        /// <c>speicher_sim.py</c> haelt es genauso.
        /// </remarks>
        public double RSchritt { get; init; } = 0.5;

        // ------------------------------------------------------------------ Schalter

        /// <summary>
        /// Zweite Stufe (Feinraster um das Grob-Optimum) rechnen, Default <c>true</c>
        /// (Fachkonzept 6.3 "Zweistufig").
        /// </summary>
        public bool Feinraster { get; init; } = true;

        /// <summary>
        /// Verschleisskosten K_ver in die Zielfunktion einrechnen,
        /// <b>Default <c>false</c></b> (Fachkonzept 5.4, Verwendung 3).
        /// </summary>
        /// <remarks>
        /// Annuitaet und Verschleisskosten bepreisen denselben Sachverhalt, den Verzehr
        /// der bezahlten Speicherlebensdauer. Solange c_ver aus der Investition
        /// abgeleitet ist (Default 0,025 = I / (N_zyk * C_nom)), ist die Aktivierung
        /// eine echte Doppelzaehlung. Sinnvoll ist sie nur, wenn der Anwender c_ver
        /// bewusst unabhaengig von der Investition setzt. Die Maske warnt bei
        /// Aktivierung, das Ergebnis kennzeichnet die Variante ueber
        /// <see cref="OptimiererErgebnis.KVerInZielfunktion"/>.
        /// </remarks>
        public bool KVerInZielfunktion { get; init; }

        /// <summary>Betriebsstrategie je Rasterpunkt, Default <see cref="OptimiererStrategie.Dauernutzung"/>.</summary>
        public OptimiererStrategie Strategie { get; init; } = OptimiererStrategie.Dauernutzung;

        /// <summary>
        /// Zugesicherte Volladezyklen N_zyk des Geraets [1]. 0 = nicht gepflegt; dann
        /// unterbleibt die Zyklenbudget-Bewertung je Rasterpunkt (Fachkonzept 5.4).
        /// </summary>
        /// <remarks>
        /// N_zyk gehoert zum Geraet und steht deshalb nicht im
        /// <see cref="SpeicherParameter"/>, der die Rechnung beschreibt. Die Rastersuche
        /// braucht den Wert trotzdem, weil die Zyklenzahl mit der Kapazitaet variiert -
        /// ein Kleinspeicher reisst das Budget, ein grosser nicht. Der Aufrufer reicht
        /// ihn aus dem Lauf-Kontext herein.
        /// </remarks>
        public double ZyklenZugesichert { get; init; }

        /// <summary>
        /// Hoechste Zahl gleichzeitig gerechneter Rasterpunkte; -1 = Vorgabe des
        /// Frameworks (Default).
        /// </summary>
        /// <remarks>
        /// <b>Rein technischer Schalter ohne Ergebniswirkung.</b> Die Rasterpunkte sind
        /// unabhaengig, jeder schreibt ausschliesslich in sein eigenes Feld, und der
        /// Bestpunkt wird erst nach dem Lauf in fester Reihenfolge bestimmt. Ein Lauf
        /// mit 1 liefert deshalb bitgleich dasselbe Ergebnis wie der parallele - genau
        /// das prueft der Test <c>Parallel_Und_Seriell_Sind_Gleich</c>. Der Schalter
        /// existiert fuer diesen Test und fuer den Fall, dass ein Anwender die Maschine
        /// waehrend der Suche noch benutzen will.
        /// </remarks>
        public int MaxParallel { get; init; } = -1;

        // ---------------------------------------------------------------- Abgeleitet

        /// <summary>Anzahl der Stuetzstellen auf der C-Raten-Achse.</summary>
        /// <remarks>
        /// <c>n_r = floor((r_max - r_min) / r_schritt + 1e-7) + 1</c> - dieselbe
        /// Toleranzkonstante wie in <c>speicher_sim.py</c>, damit 0,5 … 3,0 in
        /// 0,5er-Schritten trotz der binaeren Ungenauigkeit von 0,1er-Schrittweiten
        /// verlaesslich 6 Werte ergibt.
        /// </remarks>
        public int CRatenAnzahl => (int)((RMax - RMin) / RSchritt + 0.0000001) + 1;

        /// <summary>Anzahl der Rasterpunkte je Phase.</summary>
        public int PunkteJePhase => Stuetzstellen * CRatenAnzahl;

        /// <summary>Anzahl aller Rasterpunkte (eine oder zwei Phasen).</summary>
        public int PunkteGesamt => Feinraster ? 2 * PunkteJePhase : PunkteJePhase;

        /// <summary>Die C-Raten der Achse, aufsteigend.</summary>
        /// <remarks>
        /// Beide Phasen verwenden <b>dieselbe</b> C-Raten-Achse; verfeinert wird
        /// ausschliesslich die Kapazitaet (Vorlage <c>speicher_sim.py</c>).
        /// </remarks>
        public double[] CRaten()
        {
            int n = CRatenAnzahl;
            double[] werte = new double[n];
            for (int k = 0; k < n; k++) werte[k] = RMin + k * RSchritt;
            return werte;
        }

        /// <summary>
        /// Prueft die Optionen auf Brauchbarkeit und wirft bei Verstoss.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Bei unbrauchbaren Werten.</exception>
        public void Pruefe()
        {
            if (!(CMinKwh > 0.0))
                throw new ArgumentOutOfRangeException(nameof(CMinKwh), CMinKwh,
                    "Die untere Kapazitaetsgrenze muss groesser 0 sein.");
            if (!(CMaxKwh > CMinKwh))
                throw new ArgumentOutOfRangeException(nameof(CMaxKwh), CMaxKwh,
                    "Die obere Kapazitaetsgrenze muss ueber der unteren liegen.");
            if (Stuetzstellen < 2)
                throw new ArgumentOutOfRangeException(nameof(Stuetzstellen), Stuetzstellen,
                    "Die Kapazitaetsachse braucht mindestens 2 Stuetzstellen.");
            if (!(RMin > 0.0))
                throw new ArgumentOutOfRangeException(nameof(RMin), RMin,
                    "Die untere C-Rate muss groesser 0 sein.");
            if (RMax < RMin)
                throw new ArgumentOutOfRangeException(nameof(RMax), RMax,
                    "Die obere C-Rate darf nicht unter der unteren liegen.");
            if (!(RSchritt > 0.0))
                throw new ArgumentOutOfRangeException(nameof(RSchritt), RSchritt,
                    "Die Schrittweite der C-Rate muss groesser 0 sein.");
            if (MaxParallel == 0 || MaxParallel < -1)
                throw new ArgumentOutOfRangeException(nameof(MaxParallel), MaxParallel,
                    "MaxParallel muss -1 (Vorgabe) oder groesser 0 sein.");
        }
    }

    /// <summary>
    /// Fortschrittsmeldung der Rastersuche - eine je fertig gerechnetem Rasterpunkt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bewusst <b>ohne</b> den bis dahin besten Punkt: Welcher Punkt zu einem
    /// bestimmten Zeitpunkt der beste ist, haengt bei paralleler Rechnung von der
    /// Ablaufreihenfolge ab und waere damit nicht reproduzierbar. Der Bestpunkt steht
    /// nach dem Lauf im <see cref="OptimiererErgebnis"/> und wird dort in fester
    /// Reihenfolge bestimmt.
    /// </para>
    /// <para>
    /// Der <see cref="IProgress{T}"/>-Rueckruf erfolgt aus dem rechnenden Thread. Der
    /// UI-Faden entsteht erst dadurch, dass die Formularschicht einen
    /// <c>Progress&lt;T&gt;</c> anlegt, der auf dem UI-Thread erzeugt wurde - dessen
    /// <c>SynchronizationContext</c> marshallt die Meldung dann selbst.
    /// </para>
    /// </remarks>
    public sealed record OptimiererFortschritt
    {
        /// <summary>Fertig gerechnete Rasterpunkte ueber alle Phasen.</summary>
        public int Erledigt { get; init; }

        /// <summary>Rasterpunkte insgesamt (<see cref="OptimiererOptionen.PunkteGesamt"/>).</summary>
        public int Gesamt { get; init; }

        /// <summary><c>true</c>, solange die zweite Stufe (Feinraster) laeuft.</summary>
        public bool IstFeinraster { get; init; }

        /// <summary>Anteil [0 … 1]; 0, wenn <see cref="Gesamt"/> 0 ist.</summary>
        public double Anteil => Gesamt > 0 ? (double)Erledigt / Gesamt : 0.0;
    }
}
