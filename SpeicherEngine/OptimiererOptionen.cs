using System;

namespace SpeicherEngine
{
    /// <summary>Physikalische Groesse der Zeilenachse der Auslegungsoptimierung.</summary>
    public enum OptimiererGroessenachse
    {
        /// <summary>Nennkapazitaet des Speichers [kWh].</summary>
        KapazitaetKwh = 0,

        /// <summary>Nominale AC-Lade-/Entladeleistung des Speichers [kW].</summary>
        LeistungKw = 1
    }

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
    /// Die Lastspitzenkappung stand bis 09.09.2026 NICHT in der Liste: Sie folgt
    /// einer anderen Zielgroesse (Lastspitze statt Residuallast) und hat eine eigene
    /// Maske (AP7). Der Anwenderentscheid W11b-E-3 (10.09.2026) nimmt sie auf, weil
    /// ein Projekt OHNE Erzeugung sonst gar keine auswertbare Optimierung hat - ohne
    /// PV und BHKW bewerten Dauer- und Nachtnutzung nichts, und alle Rasterpunkte
    /// liefern denselben Wert (Befund W11b-B-25, Projekt 1050). Die eigene Maske
    /// bleibt daneben bestehen (Fachkonzept 6.4); geteilt wird der Parametersatz
    /// (<see cref="PeakShavingParameter.Nachziehend"/>).
    /// </remarks>
    public enum OptimiererStrategie
    {
        /// <summary>Dauernutzung im energetischen Produktivmodus (Fachkonzept 6.2).</summary>
        Dauernutzung = 0,

        /// <summary>Nachtnutzung im energetischen Produktivmodus (Fachkonzept 6.1).</summary>
        Nachtnutzung = 1,

        /// <summary>
        /// Lastspitzenkappung mit nachziehender Schwelle (Fachkonzept 6.4,
        /// Anwenderentscheid W11b-E-3 vom 10.09.2026). Bewertet wird der gesparte
        /// Leistungspreis statt des genutzten Erzeugungsueberschusses; die Strategie
        /// braucht deshalb <see cref="OptimiererOptionen.LeistungspreisEurProKwA"/>.
        /// </summary>
        Lastspitzenkappung = 2
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
        private const int MaxAchsenwerte = 10000;
        private const int MaxPunkteJePhaseIntern = 100000;

        // ---------------------------------------------------------- Kapazitaetsachse

        /// <summary>Groesse der Zeilenachse; Default ist die bisherige Kapazitaetsachse.</summary>
        public OptimiererGroessenachse Groessenachse { get; init; } = OptimiererGroessenachse.KapazitaetKwh;

        /// <summary>Untere Grenze der Kapazitaetsachse C_min [kWh], Default 500.</summary>
        public double CMinKwh { get; init; } = 500.0;

        /// <summary>Obere Grenze der Kapazitaetsachse C_max [kWh], Default 5.000.</summary>
        public double CMaxKwh { get; init; } = 5000.0;

        /// <summary>
        /// Optionale Schrittweite der Kapazitaetsachse [kWh]. <c>null</c> verwendet
        /// weiterhin <see cref="Stuetzstellen"/> gleichmaessige Werte.
        /// </summary>
        public double? CSchrittKwh { get; init; }

        // ------------------------------------------------------------- Leistungsachse

        /// <summary>Untere Grenze der Leistungsachse [kW], Default 50.</summary>
        public double PMinKw { get; init; } = 50.0;

        /// <summary>Obere Grenze der Leistungsachse [kW], Default 500.</summary>
        public double PMaxKw { get; init; } = 500.0;

        /// <summary>
        /// Optionale Schrittweite der Leistungsachse [kW]. <c>null</c> verwendet
        /// <see cref="Stuetzstellen"/> gleichmaessige Werte.
        /// </summary>
        public double? PSchrittKw { get; init; }

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
        /// Lastspitzenkappung am Netzanschluss mit Last minus PV/BHKW und
        /// intervallgenauer Energiebewertung. Default <c>false</c> bewahrt den
        /// bisherigen reinen Lastgangpfad.
        /// </summary>
        public bool NetzanschlussAuslegung { get; init; }

        /// <summary>
        /// Leistungspreis L_P [EUR/(kW*a)] der Berechnungsart
        /// <see cref="OptimiererStrategie.Lastspitzenkappung"/>, Vorgabe 0
        /// (Anwenderentscheid W11b-E-3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// <b>Vorgabe 0 heisst "nicht gepflegt", nicht "kostenlos".</b> Ein erfundener
        /// Erfahrungswert wuerde die Wirtschaftlichkeit unbemerkt verfaelschen - das ist
        /// dieselbe Begruendung, mit der die Peak-Shaving-Maske L_P bei 0 belaesst
        /// (Fachkonzept 4.4, offener Punkt 3). <see cref="Pruefe"/> verlangt deshalb bei
        /// Lastspitzenkappung einen Wert groesser 0: Mit L_P = 0 waere die
        /// Leistungspreisersparnis jedes Rasterpunktes 0 und die Zielfunktion allein der
        /// negative Kapitaldienst - das Optimum laege zwangslaeufig am kleinsten
        /// Speicher, ohne dass die Anzeige den Grund nennen koennte.
        /// </remarks>
        public double LeistungspreisEurProKwA { get; init; }

        // ------------------------------------------------------- Laufende Betriebskosten

        /// <summary>Jaehrliche Betriebskosten je installierter AC-Nennleistung [EUR/(kW*a)].</summary>
        public double BetriebEurProKwJahr { get; init; }

        /// <summary>Jaehrliche Betriebskosten je installierter Nennkapazitaet [EUR/(kWh*a)].</summary>
        public double BetriebEurProKwhJahr { get; init; }

        /// <summary>Variable Betriebskosten je AC-seitig entladener Energie [EUR/kWh].</summary>
        public double BetriebEurProKwhEntladen { get; init; }

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
        public int CRatenAnzahl => AnzahlMitSchritt(RMin, RMax, RSchritt);

        /// <summary>Anzahl der Werte auf der gewaehlten Groessenachse.</summary>
        public int GroessenAnzahl => Groessenachse == OptimiererGroessenachse.LeistungKw
            ? AnzahlGroessenwerte(PMinKw, PMaxKw, PSchrittKw)
            : AnzahlGroessenwerte(CMinKwh, CMaxKwh, CSchrittKwh);

        /// <summary>Anzahl der Rasterpunkte je Phase.</summary>
        public int PunkteJePhase => checked(GroessenAnzahl * CRatenAnzahl);

        /// <summary>Anzahl aller Rasterpunkte (eine oder zwei Phasen).</summary>
        public int PunkteGesamt => Feinraster ? checked(2 * PunkteJePhase) : PunkteJePhase;

        /// <summary>Die C-Raten der Achse, aufsteigend.</summary>
        /// <remarks>
        /// Beide Phasen verwenden <b>dieselbe</b> C-Raten-Achse; verfeinert wird
        /// ausschliesslich die Kapazitaet (Vorlage <c>speicher_sim.py</c>).
        /// </remarks>
        public double[] CRaten()
        {
            return WerteMitSchritt(RMin, RMax, RSchritt);
        }

        /// <summary>Die Werte der gewaehlten Groessenachse, einschliesslich Obergrenze.</summary>
        public double[] Groessenwerte()
        {
            return Groessenachse == OptimiererGroessenachse.LeistungKw
                ? Groessenwerte(PMinKw, PMaxKw, PSchrittKw)
                : Groessenwerte(CMinKwh, CMaxKwh, CSchrittKwh);
        }

        /// <summary>
        /// Prueft die Optionen auf Brauchbarkeit und wirft bei Verstoss.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Bei unbrauchbaren Werten.</exception>
        public void Pruefe()
        {
            if (!Enum.IsDefined(typeof(OptimiererGroessenachse), Groessenachse))
                throw new ArgumentOutOfRangeException(nameof(Groessenachse), Groessenachse,
                    "Die Groessenachse ist nicht vorgesehen.");
            if (!IstEndlichPositiv(CMinKwh))
                throw new ArgumentOutOfRangeException(nameof(CMinKwh), CMinKwh,
                    "Die untere Kapazitaetsgrenze muss groesser 0 sein.");
            if (!double.IsFinite(CMaxKwh) || CMaxKwh < CMinKwh)
                throw new ArgumentOutOfRangeException(nameof(CMaxKwh), CMaxKwh,
                    "Die obere Kapazitaetsgrenze darf nicht unter der unteren liegen.");
            PruefeOptionalenSchritt(CSchrittKwh, nameof(CSchrittKwh));
            if (!IstEndlichPositiv(PMinKw))
                throw new ArgumentOutOfRangeException(nameof(PMinKw), PMinKw,
                    "Die untere Leistungsgrenze muss groesser 0 sein.");
            if (!double.IsFinite(PMaxKw) || PMaxKw < PMinKw)
                throw new ArgumentOutOfRangeException(nameof(PMaxKw), PMaxKw,
                    "Die obere Leistungsgrenze darf nicht unter der unteren liegen.");
            PruefeOptionalenSchritt(PSchrittKw, nameof(PSchrittKw));
            if (Stuetzstellen < 2)
                throw new ArgumentOutOfRangeException(nameof(Stuetzstellen), Stuetzstellen,
                    "Die Kapazitaetsachse braucht mindestens 2 Stuetzstellen.");
            if (!IstEndlichPositiv(RMin))
                throw new ArgumentOutOfRangeException(nameof(RMin), RMin,
                    "Die untere C-Rate muss groesser 0 sein.");
            if (!double.IsFinite(RMax) || RMax < RMin)
                throw new ArgumentOutOfRangeException(nameof(RMax), RMax,
                    "Die obere C-Rate darf nicht unter der unteren liegen.");
            if (!IstEndlichPositiv(RSchritt))
                throw new ArgumentOutOfRangeException(nameof(RSchritt), RSchritt,
                    "Die Schrittweite der C-Rate muss groesser 0 sein.");
            if (MaxParallel == 0 || MaxParallel < -1)
                throw new ArgumentOutOfRangeException(nameof(MaxParallel), MaxParallel,
                    "MaxParallel muss -1 (Vorgabe) oder groesser 0 sein.");
            if (!IstEndlichNichtNegativ(LeistungspreisEurProKwA))
                throw new ArgumentOutOfRangeException(nameof(LeistungspreisEurProKwA), LeistungspreisEurProKwA,
                    "Der Leistungspreis darf nicht negativ sein.");
            // Die Lastspitzenkappung bewertet AUSSCHLIESSLICH gesparten Leistungspreis.
            // Ohne L_P haette jeder Rasterpunkt denselben Ertrag 0 - die Suche liefe,
            // ohne etwas zu unterscheiden.
            if (Strategie == OptimiererStrategie.Lastspitzenkappung && !(LeistungspreisEurProKwA > 0.0))
                throw new ArgumentOutOfRangeException(nameof(LeistungspreisEurProKwA), LeistungspreisEurProKwA,
                    "Die Lastspitzenkappung braucht einen Leistungspreis groesser 0.");
            PruefeKosten(BetriebEurProKwJahr, nameof(BetriebEurProKwJahr));
            PruefeKosten(BetriebEurProKwhJahr, nameof(BetriebEurProKwhJahr));
            PruefeKosten(BetriebEurProKwhEntladen, nameof(BetriebEurProKwhEntladen));
            if (!IstEndlichNichtNegativ(ZyklenZugesichert))
                throw new ArgumentOutOfRangeException(nameof(ZyklenZugesichert), ZyklenZugesichert,
                    "Die zugesicherten Zyklen muessen endlich und nicht negativ sein.");

            int groessen = GroessenAnzahl;
            int raten = CRatenAnzahl;
            if (groessen > MaxAchsenwerte || raten > MaxAchsenwerte || (long)groessen * raten > MaxPunkteJePhaseIntern)
                throw new ArgumentOutOfRangeException(nameof(Stuetzstellen),
                    "Der Suchraum ist zu gross; hoechstens 100.000 Rasterpunkte je Phase sind zulaessig.");
            double groessteKapazitaet = Groessenachse == OptimiererGroessenachse.LeistungKw
                ? PMaxKw / RMin
                : CMaxKwh;
            double groessteLeistung = Groessenachse == OptimiererGroessenachse.LeistungKw
                ? PMaxKw
                : CMaxKwh * RMax;
            if (!double.IsFinite(groessteKapazitaet) || !double.IsFinite(groessteLeistung))
                throw new ArgumentOutOfRangeException(nameof(Groessenachse),
                    "Kapazitaet und Leistung der Rasterpunkte muessen endlich sein.");
        }

        private double[] Groessenwerte(double min, double max, double? schritt)
        {
            if (min == max) return new[] { min };
            if (schritt.HasValue) return WerteMitSchritt(min, max, schritt.Value);

            double[] werte = new double[Stuetzstellen];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = min + (max - min) * i / (werte.Length - 1);
            werte[werte.Length - 1] = max;
            return werte;
        }

        private int AnzahlGroessenwerte(double min, double max, double? schritt)
            => min == max ? 1 : schritt.HasValue ? AnzahlMitSchritt(min, max, schritt.Value) : Stuetzstellen;

        private static int AnzahlMitSchritt(double min, double max, double schritt)
        {
            if (min == max) return 1;
            double quotient = (max - min) / schritt;
            if (!double.IsFinite(quotient) || quotient >= MaxAchsenwerte)
                return MaxAchsenwerte + 1;

            int ganzeSchritte = (int)Math.Floor(quotient + 1e-10);
            double letzter = min + ganzeSchritte * schritt;
            bool maxGetroffen = FastGleich(letzter, max);
            return ganzeSchritte + 1 + (maxGetroffen ? 0 : 1);
        }

        private static double[] WerteMitSchritt(double min, double max, double schritt)
        {
            int n = AnzahlMitSchritt(min, max, schritt);
            if (n > MaxAchsenwerte)
                throw new ArgumentOutOfRangeException(nameof(schritt), schritt, "Die Achse enthaelt zu viele Werte.");

            double[] werte = new double[n];
            for (int i = 0; i < n - 1; i++) werte[i] = min + i * schritt;
            werte[n - 1] = max;
            return werte;
        }

        private static void PruefeOptionalenSchritt(double? wert, string name)
        {
            if (wert.HasValue && !IstEndlichPositiv(wert.Value))
                throw new ArgumentOutOfRangeException(name, wert, "Die Schrittweite muss endlich und groesser 0 sein.");
        }

        private static void PruefeKosten(double wert, string name)
        {
            if (!IstEndlichNichtNegativ(wert))
                throw new ArgumentOutOfRangeException(name, wert, "Betriebskosten muessen endlich und nicht negativ sein.");
        }

        private static bool IstEndlichPositiv(double wert) => double.IsFinite(wert) && wert > 0.0;
        private static bool IstEndlichNichtNegativ(double wert) => double.IsFinite(wert) && wert >= 0.0;

        private static bool FastGleich(double a, double b)
        {
            double schranke = 1e-10 * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
            return Math.Abs(a - b) <= schranke;
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
