using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SpeicherEngine
{
    /// <summary>
    /// Auslegungsoptimierung (Fachkonzept 6.3): zweistufige Rastersuche ueber
    /// Kapazitaet C und C-Rate r, mit dem Jahresueberschuss nach Kapitaldienst als
    /// Zielfunktion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zielfunktion - eindeutig festgelegt.</b>
    /// </para>
    /// <code>
    /// max  dJ(C, P) = E_a,aeq(C, P) - [ c_cap*C + c_pow*P + I_fix ] * a(i_z, N)  [ - K_ver(C,P) ]
    /// </code>
    /// <para>
    /// also der degradationsbereinigte Jahresueberschuss nach Kapitaldienst in EUR/a.
    /// Der Verschleissterm K_ver ist die waehlbare Option aus Fachkonzept 5.4,
    /// <b>Default aus</b>. Die Amortisationszeit ist <b>nicht</b> Zielfunktion - sie
    /// ignoriert die Nutzungsdauer und liefert systematisch zu kleine Speicher; sie
    /// erscheint als Sekundaerkennzahl je Rasterpunkt. Hintergrund der Eindeutigkeit:
    /// In der V7-Mappe waren drei Zielgroessen im Umlauf, die Ergebnisse waren dadurch
    /// nicht interpretierbar.
    /// </para>
    /// <para>
    /// <b>Zweistufig.</b> Erst das Grobraster ueber den vollen Suchraum, dann ein
    /// Feinraster um das Grob-Optimum. Die Bereichslogik der zweiten Stufe ist
    /// zeichengetreu aus <c>speicher_sim.py:optimiere_speicher</c> uebernommen
    /// (plus/minus ein Groessenschritt, auf den Suchraum geklemmt, Mindestbreite
    /// 1 kWh); verfeinert wird ausschliesslich die <b>Kapazitaets</b>achse, die
    /// C-Raten-Achse bleibt in beiden Phasen dieselbe.
    /// </para>
    /// <para>
    /// <b>Nebenlaeufigkeit.</b> Die Rasterpunkte sind unabhaengig und laufen ueber
    /// <see cref="Parallel.For(int,int,ParallelOptions,Action{int})"/>. Zulaessig ist
    /// das, weil <see cref="SpeicherEingang"/> und <see cref="SpeicherParameter"/>
    /// unveraenderlich sind und die Strategien zustandsfrei (Fachkonzept 8.1, seit
    /// AP6/AP7 getestet). Jeder Punkt schreibt ausschliesslich in sein eigenes Feld;
    /// der Bestpunkt wird <b>nach</b> dem Lauf in fester Reihenfolge bestimmt. Das
    /// Ergebnis ist deshalb bitgleich unabhaengig von der Parallelitaet - siehe
    /// <see cref="OptimiererOptionen.MaxParallel"/>.
    /// </para>
    /// <para>
    /// <b>Die Klasse bleibt synchron und UI-frei.</b> Die Nebenlaeufigkeit gegenueber
    /// dem Bedienfaden (<c>Task.Run</c>) liegt allein in der aufrufenden
    /// Formularschicht; hier stehen nur <see cref="IProgress{T}"/> und
    /// <see cref="CancellationToken"/> als Uebergabepunkte. <b>Kein Datenbankzugriff
    /// innerhalb des Laufs</b> - der Aufrufer beschafft Reihen und Parameter
    /// vollstaendig vorher (<c>DataRepository.EngineModus</c> ist prozessweit und
    /// nicht threadgebunden).
    /// </para>
    /// <para>Die Klasse ist zustandslos und damit selbst thread-sicher.</para>
    /// </remarks>
    public sealed class SpeicherOptimierer
    {
        /// <summary>
        /// Mindestbreite des Feinrasters [kWh] - Vorlagenwert aus
        /// <c>speicher_sim.py</c> (<c>if s2_max - s2_min &lt; 1: s2_max = s2_min + 1</c>).
        /// </summary>
        private const double FeinrasterMindestbreiteKwh = 1.0;

        /// <summary>
        /// Rechnet die Rastersuche.
        /// </summary>
        /// <param name="eingang">
        /// Zeitreihen des Projekts (Last, Erzeugung, Preise). Werden von allen
        /// Rasterpunkten gemeinsam gelesen und nicht veraendert.
        /// </param>
        /// <param name="basis">
        /// Parametersatz der aktiven Auslegung. Kapazitaet und Leistung werden je
        /// Rasterpunkt ersetzt, das SoC-Band anteilig mitskaliert
        /// (<see cref="Rasterpunkt"/>); alles Uebrige - Wirkungsgrad, Quellen-Flags,
        /// Kostensaetze, Zins, Nutzungsdauer, Degradation, c_ver - gilt unveraendert.
        /// </param>
        /// <param name="optionen">Suchraum und Schalter; <c>null</c> = Vorbelegung des Fachkonzepts.</param>
        /// <param name="fortschritt">
        /// Meldung je fertigem Rasterpunkt, oder <c>null</c>. Der Rueckruf erfolgt aus
        /// dem rechnenden Thread; das Marshalling in den UI-Faden leistet der
        /// <c>Progress&lt;T&gt;</c> des Aufrufers.
        /// </param>
        /// <param name="abbruch">
        /// Abbruchmarke. Wird zu Beginn und vor jedem Rasterpunkt geprueft; ein Abbruch
        /// endet mit <see cref="OperationCanceledException"/> und ohne Teilergebnis -
        /// ein halbes Raster waere weder als Heatmap noch als Bestpunkt brauchbar.
        /// </param>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="eingang"/> oder <paramref name="basis"/> <c>null</c> ist.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Bei unbrauchbaren Optionen oder Parametern.</exception>
        /// <exception cref="OperationCanceledException">Bei Abbruch ueber <paramref name="abbruch"/>.</exception>
        public OptimiererErgebnis Optimiere(
            SpeicherEingang eingang,
            SpeicherParameter basis,
            OptimiererOptionen? optionen = null,
            IProgress<OptimiererFortschritt>? fortschritt = null,
            CancellationToken abbruch = default)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (basis == null) throw new ArgumentNullException(nameof(basis));

            // Vor jeder Zuteilung: ein bereits gesetztes Abbruchsignal beendet den
            // Aufruf sofort und ohne Rechenarbeit.
            abbruch.ThrowIfCancellationRequested();

            OptimiererOptionen opt = optionen ?? new OptimiererOptionen();
            opt.Pruefe();
            basis.Pruefe();

            if (!(basis.CNomKwh > 0.0))
                throw new ArgumentOutOfRangeException(nameof(basis), basis.CNomKwh,
                    "Die Basisauslegung braucht eine Kapazitaet groesser 0 - das SoC-Band wird anteilig dazu skaliert.");

            Stopwatch uhr = Stopwatch.StartNew();
            ISpeicherStrategie strategie = BaueStrategie(opt.Strategie);
            double[] cRaten = opt.CRaten();

            int erledigt = 0;
            int gesamt = opt.PunkteGesamt;

            OptimiererRaster grob = RechnePhase(
                false, opt.CMinKwh, opt.CMaxKwh, cRaten,
                eingang, basis, opt, strategie, fortschritt, ref erledigt, gesamt, abbruch);

            OptimiererRaster? fein = null;
            if (opt.Feinraster)
            {
                double untenKwh, obenKwh;
                FeinrasterBereich(opt, grob.BestPunkt.CNomKwh, out untenKwh, out obenKwh);

                fein = RechnePhase(
                    true, untenKwh, obenKwh, cRaten,
                    eingang, basis, opt, strategie, fortschritt, ref erledigt, gesamt, abbruch);
            }

            // Strenger Groesser-Vergleich: bei Gleichstand bleibt das Grobraster
            // massgeblich (Vorlage: "best = best2 if best2[0] > best1[0] else best1").
            OptimiererPunkt best =
                fein != null && fein.BestPunkt.ZielfunktionEur > grob.BestPunkt.ZielfunktionEur
                    ? fein.BestPunkt
                    : grob.BestPunkt;

            uhr.Stop();

            return new OptimiererErgebnis(
                grob, fein, best,
                Rasterpunkt(basis, best.CNomKwh, best.CRate),
                Randlage(best, opt, cRaten),
                basis.CPowEurProKw == 0.0,
                opt, erledigt, uhr.Elapsed);
        }

        // ==================================================================
        // Suchraum
        // ==================================================================

        /// <summary>
        /// Bereich der zweiten Stufe um das Grob-Optimum
        /// (<c>speicher_sim.py:optimiere_speicher</c>).
        /// </summary>
        /// <remarks>
        /// <code>
        /// schritt = (C_max - C_min) / (n - 1)          # ein Groessenschritt des Grobrasters
        /// unten   = max(C_min, C_best - schritt)
        /// oben    = min(C_max, C_best + schritt)
        /// if oben - unten &lt; 1: oben = unten + 1      # Mindestbreite, Vorlagenwert
        /// </code>
        /// Die Klemmung auf den Suchraum ist der Grund, warum ein Grob-Optimum am Rand
        /// ein einseitiges Feinraster bekommt - die Suche laeuft nicht ueber die vom
        /// Anwender gesetzten Grenzen hinaus. Genau darauf zielt die Randlagenwarnung.
        /// Die Mindestbreite kann den Suchraum nach oben um bis zu 1 kWh verlassen; das
        /// ist Vorlagentreue und praktisch bedeutungslos, weil sie nur bei einem
        /// Suchraum unter 1 kWh Breite greift.
        /// </remarks>
        public static void FeinrasterBereich(OptimiererOptionen optionen, double cBestKwh,
                                             out double untenKwh, out double obenKwh)
        {
            if (optionen == null) throw new ArgumentNullException(nameof(optionen));

            double schritt = (optionen.CMaxKwh - optionen.CMinKwh) / (optionen.Stuetzstellen - 1);

            untenKwh = Math.Max(optionen.CMinKwh, cBestKwh - schritt);
            obenKwh = Math.Min(optionen.CMaxKwh, cBestKwh + schritt);

            if (obenKwh - untenKwh < FeinrasterMindestbreiteKwh)
                obenKwh = untenKwh + FeinrasterMindestbreiteKwh;
        }

        /// <summary>
        /// Parametersatz eines Rasterpunkts: Kapazitaet und Leistung gesetzt, SoC-Band
        /// und Start-Ladezustand anteilig mitskaliert.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Das SoC-Band steht im <see cref="SpeicherParameter"/> in kWh, an der Maske
        /// aber in Prozent der Nennkapazitaet. Ein Rasterpunkt mit anderer Kapazitaet
        /// muss deshalb dieselben <b>Prozentsaetze</b> behalten, nicht dieselben
        /// kWh-Werte: Ein 10/90-%-Band bleibt bei 5.000 kWh ein 10/90-%-Band und wird
        /// nicht zum 1/9-%-Band. Umgesetzt als Streckung mit
        /// <c>f = C_neu / C_basis</c> - rechnerisch identisch zur Rueckrechnung ueber
        /// die Prozentsaetze, aber ohne den Umweg ueber zwei Divisionen.
        /// </para>
        /// <para>
        /// Der Start-Ladezustand wird mitskaliert, wenn er gesetzt ist; <c>null</c>
        /// bleibt <c>null</c> und bedeutet weiterhin SoC_min (Produktivstandard nach
        /// Entscheid AP0, Frage 8).
        /// </para>
        /// <para>
        /// Alles Uebrige bleibt unveraendert - insbesondere die Kostensaetze c_cap
        /// [EUR/kWh] und c_pow [EUR/kW], deren Produkt mit C beziehungsweise P die
        /// Investition dieses Punktes ergibt (<see cref="SpeicherParameter.InvestitionEur"/>).
        /// </para>
        /// </remarks>
        /// <param name="basis">Basisauslegung; Kapazitaet muss groesser 0 sein.</param>
        /// <param name="cNomKwh">Nennkapazitaet des Rasterpunkts [kWh].</param>
        /// <param name="cRate">C-Rate des Rasterpunkts [1/h]; P = r * C.</param>
        public static SpeicherParameter Rasterpunkt(SpeicherParameter basis, double cNomKwh, double cRate)
        {
            if (basis == null) throw new ArgumentNullException(nameof(basis));
            if (!(basis.CNomKwh > 0.0))
                throw new ArgumentOutOfRangeException(nameof(basis), basis.CNomKwh,
                    "Die Basisauslegung braucht eine Kapazitaet groesser 0.");

            double f = cNomKwh / basis.CNomKwh;

            return basis with
            {
                CNomKwh = cNomKwh,
                PKw = cRate * cNomKwh,
                SoCMinKwh = basis.SoCMinKwh * f,
                SoCMaxKwh = basis.SoCMaxKwh * f,
                StartSoCKwh = basis.StartSoCKwh.HasValue ? basis.StartSoCKwh.Value * f : (double?)null
            };
        }

        // ==================================================================
        // Rasterlauf
        // ==================================================================

        /// <summary>
        /// Rechnet eine Phase (ein vollstaendiges Raster) ueber
        /// <c>Parallel.For</c>.
        /// </summary>
        private static OptimiererRaster RechnePhase(
            bool istFeinraster, double cUntenKwh, double cObenKwh, double[] cRaten,
            SpeicherEingang eingang, SpeicherParameter basis, OptimiererOptionen opt,
            ISpeicherStrategie strategie, IProgress<OptimiererFortschritt>? fortschritt,
            ref int erledigt, int gesamt, CancellationToken abbruch)
        {
            int zeilen = opt.Stuetzstellen;
            int spalten = cRaten.Length;

            double[] kapazitaeten = new double[zeilen];
            for (int i = 0; i < zeilen; i++)
                kapazitaeten[i] = cUntenKwh + (cObenKwh - cUntenKwh) * i / (zeilen - 1);

            OptimiererPunkt[][] punkte = new OptimiererPunkt[zeilen][];
            for (int i = 0; i < zeilen; i++) punkte[i] = new OptimiererPunkt[spalten];

            ParallelOptions po = new ParallelOptions { CancellationToken = abbruch };
            if (opt.MaxParallel > 0) po.MaxDegreeOfParallelism = opt.MaxParallel;

            // Der Zaehler wird von mehreren Threads hochgezaehlt und deshalb ueber
            // Interlocked gefuehrt. Er dient AUSSCHLIESSLICH der Fortschrittsanzeige -
            // in kein Ergebnis geht er ein, und deshalb macht seine Reihenfolge das
            // Ergebnis auch nicht abhaengig von der Parallelitaet.
            int zaehler = erledigt;

            Parallel.For(0, zeilen * spalten, po, index =>
            {
                abbruch.ThrowIfCancellationRequested();

                int iKapazitaet = index / spalten;
                int iCRate = index - iKapazitaet * spalten;

                punkte[iKapazitaet][iCRate] = RechnePunkt(
                    eingang, basis, opt, strategie, kapazitaeten[iKapazitaet], cRaten[iCRate]);

                if (fortschritt != null)
                {
                    int stand = Interlocked.Increment(ref zaehler);
                    fortschritt.Report(new OptimiererFortschritt
                    {
                        Erledigt = stand,
                        Gesamt = gesamt,
                        IstFeinraster = istFeinraster
                    });
                }
                else
                {
                    Interlocked.Increment(ref zaehler);
                }
            });

            erledigt = zaehler;
            return new OptimiererRaster(istFeinraster, cUntenKwh, cObenKwh, kapazitaeten, cRaten, punkte);
        }

        /// <summary>
        /// Rechnet einen einzelnen Rasterpunkt: vollstaendiger Jahreslauf, Zielfunktion
        /// und Sekundaerkennzahlen (Fachkonzept 6.3).
        /// </summary>
        private static OptimiererPunkt RechnePunkt(
            SpeicherEingang eingang, SpeicherParameter basis, OptimiererOptionen opt,
            ISpeicherStrategie strategie, double cNomKwh, double cRate)
        {
            SpeicherParameter p = Rasterpunkt(basis, cNomKwh, cRate);
            SpeicherErgebnis erg = strategie.Berechne(eingang, p);

            WirtschaftlichkeitErgebnis w = erg.Wirtschaftlichkeit;
            SpeicherKennzahlen k = erg.Kennzahlen;

            double kVer = k.VerschleisskostenEurProA;
            double ziel = opt.KVerInZielfunktion
                ? w.JahresueberschussEur - kVer
                : w.JahresueberschussEur;

            double zyklenNutzungsdauer = k.AequivalenteVollzyklen * p.NutzungsdauerA;

            return new OptimiererPunkt
            {
                CNomKwh = cNomKwh,
                CRate = cRate,
                PKw = p.PKw,

                ZielfunktionEur = ziel,
                JahresueberschussEur = w.JahresueberschussEur,

                ErtragReferenzjahrEur = w.ErtragReferenzjahrEur,
                ErtragAequivalentEur = w.ErtragAequivalentEur,
                InvestitionEur = w.InvestitionEur,
                AnnuitaetEur = w.AnnuitaetEur,
                KapitalwertEur = w.KapitalwertEur,
                StatischeAmortisation = w.StatischeAmortisation,
                DynamischeAmortisation = w.DynamischeAmortisation,

                AequivalenteVollzyklen = k.AequivalenteVollzyklen,
                ZyklenNutzungsdauer = zyklenNutzungsdauer,
                ZyklenbudgetUeberschritten =
                    opt.ZyklenZugesichert > 0.0 && zyklenNutzungsdauer > opt.ZyklenZugesichert,
                VerschleisskostenEurProA = kVer,

                EigenverbrauchsquoteMitSpeicher = k.EigenverbrauchsquoteMitSpeicher,
                AutarkiegradMitSpeicher = k.AutarkiegradMitSpeicher,
                LadeenergieKwh = erg.LadeenergieKwh,
                EntladeenergieKwh = erg.EntladeenergieKwh,
                SpeicherverlusteKwh = k.SpeicherverlusteKwh
            };
        }

        /// <summary>
        /// Strategie-Instanz zur gewaehlten Betriebsart - immer im energetischen
        /// Produktivmodus.
        /// </summary>
        /// <remarks>
        /// Der Excel-Kompatibilitaetsmodus wird bewusst nicht angeboten: Er rechnet
        /// ohne Verlustmodell, mit Start-SoC 0 und ohne Quellen-Matrix und ist damit
        /// keine Grundlage fuer eine Auslegungsentscheidung. Sein Zweck ist der
        /// Nachweis gegen die V7-Mappe, nicht die Planung.
        /// </remarks>
        private static ISpeicherStrategie BaueStrategie(OptimiererStrategie strategie)
        {
            switch (strategie)
            {
                case OptimiererStrategie.Nachtnutzung:
                    return new Nachtnutzung(SpeicherModus.Energetisch);
                case OptimiererStrategie.Dauernutzung:
                    return new Dauernutzung(SpeicherModus.Energetisch);
                default:
                    throw new ArgumentOutOfRangeException(nameof(strategie), strategie,
                        "Fuer die Rastersuche ist diese Strategie nicht vorgesehen.");
            }
        }

        // ==================================================================
        // Randlage
        // ==================================================================

        /// <summary>
        /// Prueft, auf welcher Kante des Suchraums der Bestpunkt liegt
        /// (Fachkonzept 6.3).
        /// </summary>
        /// <remarks>
        /// Bezugsgroesse der Kapazitaetsachse sind die Optionsgrenzen C_min/C_max, die
        /// der C-Raten-Achse die tatsaechlichen Achsenenden: Bei einer Schrittweite,
        /// die nicht auf r_max aufgeht, endet die Achse unterhalb von r_max, und
        /// "am Rand" heisst dann das letzte gerechnete Achsenglied. Verglichen wird mit
        /// relativer Toleranz, weil die Achsenwerte aus einer Interpolation stammen und
        /// den Endwert nur bis auf wenige ULP treffen.
        /// </remarks>
        private static OptimiererRandlage Randlage(OptimiererPunkt best, OptimiererOptionen opt, double[] cRaten)
        {
            return new OptimiererRandlage
            {
                KapazitaetUnten = FastGleich(best.CNomKwh, opt.CMinKwh),
                KapazitaetOben = FastGleich(best.CNomKwh, opt.CMaxKwh),
                CRateUnten = FastGleich(best.CRate, cRaten[0]),
                CRateOben = FastGleich(best.CRate, cRaten[cRaten.Length - 1])
            };
        }

        /// <summary>Vergleich mit relativer Toleranz (1e-9), fuer Achsenendpunkte.</summary>
        private static bool FastGleich(double a, double b)
        {
            double schranke = 1e-9 * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
            return Math.Abs(a - b) <= schranke;
        }
    }
}
