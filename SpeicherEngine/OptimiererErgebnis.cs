using System;
using System.Collections.Generic;

namespace SpeicherEngine
{
    /// <summary>
    /// Ein Rasterpunkt der Auslegungsoptimierung: die Auslegung (C, r, P), der Wert der
    /// Zielfunktion und die Sekundaerkennzahlen nach Fachkonzept 6.3.
    /// </summary>
    /// <remarks>
    /// Der Typ ist ein unveraenderlicher <c>record</c>. Er haelt <b>keine</b>
    /// Zeitreihen: 120 Jahreslaeufe zu je 35.040 Werten waeren rund 270 MB, und fuer
    /// Heatmap, Schnittkurve und Kennzahlenblock wird keine einzige davon gebraucht.
    /// Wer den Verlauf des Bestpunkts sehen will, rechnet ihn mit
    /// <see cref="OptimiererErgebnis.BestParameter"/> in einem einzelnen Lauf nach.
    /// </remarks>
    public sealed record OptimiererPunkt
    {
        // ---------------------------------------------------------------- Auslegung

        /// <summary>Nennkapazitaet C_nom [kWh] dieses Punktes.</summary>
        public double CNomKwh { get; init; }

        /// <summary>C-Rate r [1/h] dieses Punktes.</summary>
        public double CRate { get; init; }

        /// <summary>Lade-/Entladeleistung P = r * C_nom [kW].</summary>
        public double PKw { get; init; }

        // -------------------------------------------------------------- Zielfunktion

        /// <summary>
        /// Wert der Zielfunktion [EUR/a] - die Groesse, nach der maximiert wird:
        /// <c>dJ = E_a,aeq - I * a(i_z, N)</c>, abzueglich K_ver, falls
        /// <see cref="OptimiererOptionen.KVerInZielfunktion"/> gesetzt ist
        /// (Fachkonzept 6.3).
        /// </summary>
        public double ZielfunktionEur { get; init; }

        /// <summary>
        /// Jahresueberschuss nach Kapitaldienst dJ = E_a,aeq - A [EUR/a] <b>ohne</b>
        /// Verschleissterm, nach Abzug der laufenden Betriebskosten.
        /// </summary>
        /// <remarks>
        /// Identisch mit <see cref="ZielfunktionEur"/>, solange die Option
        /// <see cref="OptimiererOptionen.KVerInZielfunktion"/> aus ist. Beide Groessen
        /// stehen nebeneinander, damit der Ergebnisblock zeigen kann, was die Option
        /// kostet.
        /// </remarks>
        public double JahresueberschussEur { get; init; }

        /// <summary>Jahresueberschuss des Wirtschaftlichkeitsblocks vor laufenden Betriebskosten [EUR/a].</summary>
        public double JahresueberschussVorBetriebskostenEur { get; init; }

        /// <summary>Jaehrliche Betriebskosten aus installierter AC-Nennleistung [EUR/a].</summary>
        public double BetriebskostenLeistungEurProA { get; init; }

        /// <summary>Jaehrliche Betriebskosten aus installierter Nennkapazitaet [EUR/a].</summary>
        public double BetriebskostenKapazitaetEurProA { get; init; }

        /// <summary>Variable Betriebskosten der AC-seitig entladenen Energie [EUR/a].</summary>
        public double BetriebskostenEntladungEurProA { get; init; }

        /// <summary>Summe aller laufenden Betriebskosten [EUR/a].</summary>
        public double BetriebskostenEurProA { get; init; }

        // ---------------------------------------------------------- Wirtschaftlichkeit

        /// <summary>Ertrag des Referenzjahres E_a,1 [EUR/a] (unskaliert).</summary>
        public double ErtragReferenzjahrEur { get; init; }

        /// <summary>Degradationsaequivalenter Jahresertrag E_a,aeq [EUR/a].</summary>
        public double ErtragAequivalentEur { get; init; }

        /// <summary>Degradationsaequivalenter Jahresertrag nach laufenden Betriebskosten [EUR/a].</summary>
        public double ErtragAequivalentNachBetriebskostenEur { get; init; }

        /// <summary>Investition I = c_cap*C + c_pow*P + I_fix [EUR].</summary>
        public double InvestitionEur { get; init; }

        /// <summary>Annuitaet (Kapitaldienst) A = I * a(i_z, N) [EUR/a].</summary>
        public double AnnuitaetEur { get; init; }

        /// <summary>Kapitalwert NPV = E_a,1 * RBF_deg - I [EUR].</summary>
        public double KapitalwertEur { get; init; }

        /// <summary>Kapitalwert vor laufenden Betriebskosten [EUR].</summary>
        public double KapitalwertVorBetriebskostenEur { get; init; }

        /// <summary>Statische Amortisation [a] (Sekundaerkennzahl, nie Zielgroesse).</summary>
        public Amortisation StatischeAmortisation { get; init; }

        /// <summary>Dynamische Amortisation [a] (Sekundaerkennzahl, nie Zielgroesse).</summary>
        public Amortisation DynamischeAmortisation { get; init; }

        // ---------------------------------------------------------------- Speicher

        /// <summary>Aequivalente Vollzyklen n_zyk [1/a].</summary>
        public double AequivalenteVollzyklen { get; init; }

        /// <summary>
        /// Ueber die Nutzungsdauer hochgerechnete Zyklen <c>n_zyk * N</c> [1].
        /// </summary>
        public double ZyklenNutzungsdauer { get; init; }

        /// <summary>
        /// <c>true</c>, wenn <see cref="ZyklenNutzungsdauer"/> die zugesicherten
        /// Volladezyklen N_zyk uebersteigt. Immer <c>false</c>, wenn N_zyk nicht
        /// gepflegt ist (<see cref="OptimiererOptionen.ZyklenZugesichert"/> = 0).
        /// </summary>
        public bool ZyklenbudgetUeberschritten { get; init; }

        /// <summary>Jahres-Verschleisskosten K_ver = n_zyk * C_nom * c_ver [EUR/a].</summary>
        public double VerschleisskostenEurProA { get; init; }

        // ----------------------------------------------------------------- Energie

        /// <summary>Eigenverbrauchsquote mit Speicher [-].</summary>
        public double EigenverbrauchsquoteMitSpeicher { get; init; }

        /// <summary>Autarkiegrad mit Speicher [-].</summary>
        public double AutarkiegradMitSpeicher { get; init; }

        /// <summary>AC-seitig geladene Energie [kWh/a].</summary>
        public double LadeenergieKwh { get; init; }

        /// <summary>AC-seitig entnommene Energie [kWh/a].</summary>
        public double EntladeenergieKwh { get; init; }

        /// <summary>Speicherverluste des Jahres [kWh/a].</summary>
        public double SpeicherverlusteKwh { get; init; }

        // ------------------------------------------------------ Lastspitzenkappung

        // Die vier Groessen fuellt AUSSCHLIESSLICH die Berechnungsart
        // Lastspitzenkappung (Anwenderentscheid W11b-E-3, 10.09.2026); bei
        // Dauer- und Nachtnutzung bleiben sie 0 beziehungsweise false. Sie stehen
        // hier und nicht in einem eigenen Typ, weil der Rasterpunkt EINE Zeile der
        // Kennzahlentabelle und der CSV-Ausgabe ist - ein zweiter Typ zwaenge Anzeige
        // und Export zu einer Fallunterscheidung, die nichts erklaert.

        /// <summary>Lastspitze ohne Speicher P_alt,max [kW].</summary>
        public double SpitzeOhneSpeicherKw { get; init; }

        /// <summary>Lastspitze mit Speicher P_neu,max [kW].</summary>
        public double SpitzeMitSpeicherKw { get; init; }

        /// <summary>Kappung der Jahresspitze <c>P_alt,max - P_neu,max</c> [kW].</summary>
        public double KappungKw => SpitzeOhneSpeicherKw - SpitzeMitSpeicherKw;

        /// <summary>
        /// Leistungspreisersparnis <c>Kappung * L_P</c> [EUR/a] - der erste Term der
        /// Monetarisierung nach Fachkonzept 6.4 und damit der ganze Ertrag dieser
        /// Berechnungsart, bevor die Verschiebeverluste abgezogen sind.
        /// </summary>
        public double LeistungspreisersparnisEur { get; init; }

        /// <summary>Am Ende des Laufs gueltige Schwelle [kW].</summary>
        public double ErreichteSchwelleKw { get; init; }

        /// <summary>
        /// <c>true</c>, wenn die Schwelle gerissen wurde. Im nachziehenden Modus, mit
        /// dem die Rastersuche rechnet, immer <c>false</c> - der Wert bleibt trotzdem
        /// im Punkt, damit ein spaeterer fester Modus ihn nicht nachtragen muss.
        /// </summary>
        public bool SchwelleGerissen { get; init; }
    }

    /// <summary>
    /// Ein vollstaendig gerechnetes Raster (eine Phase der zweistufigen Suche).
    /// </summary>
    /// <remarks>
    /// Die Punkte liegen als <c>Punkte[iGroesse][iCRate]</c> - dieselbe
    /// Indexordnung wie <c>heat[i_size][i_c]</c> der Vorlage <c>speicher_sim.py</c>.
    /// Zeilen sind Kapazitaeten oder Leistungen, Spalten C-Raten.
    /// </remarks>
    public sealed class OptimiererRaster
    {
        /// <summary><c>true</c> fuer die zweite Stufe (Feinraster), <c>false</c> fuer das Grobraster.</summary>
        public bool IstFeinraster { get; }

        /// <summary>Physikalische Groesse der Zeilenachse.</summary>
        public OptimiererGroessenachse Groessenachse { get; }

        /// <summary>Untere Grenze der Zeilenachse in deren Einheit.</summary>
        public double GroessenMin { get; }

        /// <summary>Obere Grenze der Zeilenachse in deren Einheit.</summary>
        public double GroessenMax { get; }

        /// <summary>Werte der Zeilenachse [kWh] oder [kW], abhaengig von <see cref="Groessenachse"/>.</summary>
        public IReadOnlyList<double> Groessenwerte { get; }

        /// <summary>Untere Kapazitaetsgrenze dieses Rasters [kWh].</summary>
        public double CMinKwh { get; }

        /// <summary>Obere Kapazitaetsgrenze dieses Rasters [kWh].</summary>
        public double CMaxKwh { get; }

        /// <summary>Die Kapazitaetswerte der Achse [kWh], aufsteigend.</summary>
        public IReadOnlyList<double> KapazitaetenKwh { get; }

        /// <summary>
        /// Leistungswerte der Zeilenachse [kW]; leer bei einer Kapazitaetsachse.
        /// </summary>
        public IReadOnlyList<double> LeistungenKw { get; }

        /// <summary>Die C-Raten der Achse [1/h], aufsteigend.</summary>
        public IReadOnlyList<double> CRaten { get; }

        /// <summary>Die Rasterpunkte, <c>[iKapazitaet][iCRate]</c>.</summary>
        public OptimiererPunkt[][] Punkte { get; }

        /// <summary>
        /// Bester Punkt dieses Rasters nach <see cref="OptimiererPunkt.ZielfunktionEur"/>.
        /// </summary>
        /// <remarks>
        /// Ermittelt in fester Reihenfolge (Kapazitaet aufsteigend, darin C-Rate
        /// aufsteigend) mit strengem Groesser-Vergleich - bei Gleichstand gewinnt also
        /// der zuerst besuchte, das heisst die kleinere Kapazitaet und die kleinere
        /// C-Rate. Dieselbe Regel wie in <c>speicher_sim.py</c>, und der Grund dafuer,
        /// dass das Ergebnis von der Parallelitaet unabhaengig ist.
        /// </remarks>
        public OptimiererPunkt BestPunkt { get; }

        /// <summary>Zeilenzahl (Stuetzstellen der gewaehlten Groessenachse).</summary>
        public int Zeilen => Groessenwerte.Count;

        /// <summary>Spaltenzahl (C-Raten).</summary>
        public int Spalten => CRaten.Count;

        /// <summary>Erzeugt das Raster. Wird ausschliesslich vom <see cref="SpeicherOptimierer"/> aufgerufen.</summary>
        public OptimiererRaster(bool istFeinraster, OptimiererGroessenachse groessenachse,
                                double groessenMin, double groessenMax,
                                double[] groessenwerte, double[] cRaten, OptimiererPunkt[][] punkte)
        {
            IstFeinraster = istFeinraster;
            Groessenachse = groessenachse;
            GroessenMin = groessenMin;
            GroessenMax = groessenMax;
            Groessenwerte = groessenwerte ?? throw new ArgumentNullException(nameof(groessenwerte));
            KapazitaetenKwh = groessenachse == OptimiererGroessenachse.KapazitaetKwh
                ? groessenwerte
                : Array.Empty<double>();
            LeistungenKw = groessenachse == OptimiererGroessenachse.LeistungKw
                ? groessenwerte
                : Array.Empty<double>();
            CRaten = cRaten ?? throw new ArgumentNullException(nameof(cRaten));
            Punkte = punkte ?? throw new ArgumentNullException(nameof(punkte));
            BestPunkt = BestenSuchen(punkte);

            double cMin = double.MaxValue;
            double cMax = double.MinValue;
            foreach (OptimiererPunkt[] zeile in punkte)
                foreach (OptimiererPunkt punkt in zeile)
                {
                    if (punkt.CNomKwh < cMin) cMin = punkt.CNomKwh;
                    if (punkt.CNomKwh > cMax) cMax = punkt.CNomKwh;
                }
            CMinKwh = cMin;
            CMaxKwh = cMax;
        }

        /// <summary>Kompatibilitaetskonstruktor fuer ein Raster mit Kapazitaetsachse.</summary>
        public OptimiererRaster(bool istFeinraster, double cMinKwh, double cMaxKwh,
                                double[] kapazitaetenKwh, double[] cRaten, OptimiererPunkt[][] punkte)
            : this(istFeinraster, OptimiererGroessenachse.KapazitaetKwh,
                   cMinKwh, cMaxKwh, kapazitaetenKwh, cRaten, punkte)
        {
        }

        /// <summary>
        /// Kleinster und groesster Zielfunktionswert des Rasters [EUR/a] - die Skala der
        /// Heatmap.
        /// </summary>
        public void Wertebereich(out double min, out double max)
        {
            min = double.MaxValue;
            max = double.MinValue;
            foreach (OptimiererPunkt[] zeile in Punkte)
                foreach (OptimiererPunkt p in zeile)
                {
                    if (p.ZielfunktionEur < min) min = p.ZielfunktionEur;
                    if (p.ZielfunktionEur > max) max = p.ZielfunktionEur;
                }

            if (min > max) { min = 0.0; max = 0.0; }
        }

        /// <summary>
        /// Schnittkurve dJ(C) bei der C-Rate <paramref name="iCRate"/> - die Spalte des
        /// Rasters (Fachkonzept 6.3, "Schnittkurve bei der besten C-Rate").
        /// </summary>
        public double[] Schnittkurve(int iCRate)
        {
            if (iCRate < 0 || iCRate >= Spalten) throw new ArgumentOutOfRangeException(nameof(iCRate));

            double[] werte = new double[Zeilen];
            for (int i = 0; i < Zeilen; i++) werte[i] = Punkte[i][iCRate].ZielfunktionEur;
            return werte;
        }

        /// <summary>Index der C-Rate, die dem Wert am naechsten liegt; -1 bei leerem Raster.</summary>
        public int IndexCRate(double cRate)
        {
            int treffer = -1;
            double abstand = double.MaxValue;
            for (int k = 0; k < CRaten.Count; k++)
            {
                double d = Math.Abs(CRaten[k] - cRate);
                if (d < abstand) { abstand = d; treffer = k; }
            }
            return treffer;
        }

        private static OptimiererPunkt BestenSuchen(OptimiererPunkt[][] punkte)
        {
            OptimiererPunkt? best = null;
            foreach (OptimiererPunkt[] zeile in punkte)
                foreach (OptimiererPunkt p in zeile)
                    if (best == null || p.ZielfunktionEur > best.ZielfunktionEur) best = p;

            if (best == null)
                throw new ArgumentException("Ein Raster ohne Punkte ist nicht auswertbar.", nameof(punkte));
            return best;
        }
    }

    /// <summary>
    /// Randlage des Optimums: auf welcher Kante des Suchraums der Bestpunkt liegt
    /// (Fachkonzept 6.3, Warnung "Optimum am Rand - Suchbereich erweitern").
    /// </summary>
    /// <remarks>
    /// Bezugsgroesse ist immer der <b>Suchraum der Optionen</b>, nie das Feinraster:
    /// Dass der Bestpunkt am Rand des Feinrasters liegt, ist der Normalfall und kein
    /// Befund. Aussagekraeftig ist allein, ob die Suche an den vom Anwender gesetzten
    /// Grenzen anstoesst - dort war das Optimum der gespeicherten V7-Heatmap
    /// (5.000 kWh), und genau dieser Fall soll auffallen.
    /// </remarks>
    public sealed record OptimiererRandlage
    {
        /// <summary>Bestpunkt liegt auf C_min - kleinere Speicher pruefen.</summary>
        public bool KapazitaetUnten { get; init; }

        /// <summary>Bestpunkt liegt auf C_max - groessere Speicher pruefen.</summary>
        public bool KapazitaetOben { get; init; }

        /// <summary>Bestpunkt liegt auf der unteren Grenze der gewaehlten Groessenachse.</summary>
        public bool GroesseUnten
        {
            get => KapazitaetUnten;
            init => KapazitaetUnten = value;
        }

        /// <summary>Bestpunkt liegt auf der oberen Grenze der gewaehlten Groessenachse.</summary>
        public bool GroesseOben
        {
            get => KapazitaetOben;
            init => KapazitaetOben = value;
        }

        /// <summary>Bestpunkt liegt auf r_min - kleinere C-Raten pruefen.</summary>
        public bool CRateUnten { get; init; }

        /// <summary>Bestpunkt liegt auf r_max - groessere C-Raten pruefen.</summary>
        public bool CRateOben { get; init; }

        /// <summary><c>true</c>, wenn der Bestpunkt auf irgendeiner Kante liegt.</summary>
        public bool Vorhanden => GroesseUnten || GroesseOben || CRateUnten || CRateOben;
    }

    /// <summary>
    /// Gesamtergebnis der Auslegungsoptimierung (Fachkonzept 6.3).
    /// </summary>
    public sealed class OptimiererErgebnis
    {
        /// <summary>Erste Stufe ueber den vollen Suchraum. Nie <c>null</c>.</summary>
        public OptimiererRaster Grobraster { get; }

        /// <summary>
        /// Zweite Stufe um das Grob-Optimum, oder <c>null</c>, wenn
        /// <see cref="OptimiererOptionen.Feinraster"/> aus war.
        /// </summary>
        public OptimiererRaster? Feinraster { get; }

        /// <summary>
        /// Bester Punkt beider Phasen. Bei Gleichstand gewinnt das <b>Grobraster</b> -
        /// dieselbe Regel wie in <c>speicher_sim.py</c> (<c>best2 if best2 &gt; best1</c>).
        /// </summary>
        public OptimiererPunkt BestPunkt { get; }

        /// <summary>
        /// Vollstaendiger Parametersatz des Bestpunkts - fertig fuer einen Einzellauf
        /// oder fuer die Uebernahme in die Geraetedaten.
        /// </summary>
        public SpeicherParameter BestParameter { get; }

        /// <summary>Randlage des Bestpunkts im Suchraum.</summary>
        public OptimiererRandlage Randlage { get; }

        /// <summary>
        /// <c>true</c>, wenn weder Investition noch laufende Betriebskosten von der
        /// installierten Leistung abhaengen.
        /// </summary>
        public bool CPowNeutral { get; }

        /// <summary>Ob K_ver in die Zielfunktion eingerechnet wurde (Fachkonzept 5.4).</summary>
        public bool KVerInZielfunktion { get; }

        /// <summary>Die verwendeten Optionen.</summary>
        public OptimiererOptionen Optionen { get; }

        /// <summary>Zahl der gerechneten Rasterpunkte ueber alle Phasen.</summary>
        public int PunkteGerechnet { get; }

        /// <summary>Rechenzeit der Rastersuche.</summary>
        public TimeSpan Dauer { get; }

        /// <summary>Erzeugt das Ergebnis. Wird ausschliesslich vom <see cref="SpeicherOptimierer"/> aufgerufen.</summary>
        public OptimiererErgebnis(OptimiererRaster grobraster, OptimiererRaster? feinraster,
                                  OptimiererPunkt bestPunkt, SpeicherParameter bestParameter,
                                  OptimiererRandlage randlage, bool cPowNeutral,
                                  OptimiererOptionen optionen, int punkteGerechnet, TimeSpan dauer)
        {
            Grobraster = grobraster ?? throw new ArgumentNullException(nameof(grobraster));
            Feinraster = feinraster;
            BestPunkt = bestPunkt ?? throw new ArgumentNullException(nameof(bestPunkt));
            BestParameter = bestParameter ?? throw new ArgumentNullException(nameof(bestParameter));
            Randlage = randlage ?? throw new ArgumentNullException(nameof(randlage));
            CPowNeutral = cPowNeutral;
            Optionen = optionen ?? throw new ArgumentNullException(nameof(optionen));
            KVerInZielfunktion = optionen.KVerInZielfunktion;
            PunkteGerechnet = punkteGerechnet;
            Dauer = dauer;
        }

        /// <summary>
        /// Das Raster, aus dem der Bestpunkt stammt - Bezugsgroesse der Schnittkurve
        /// und der markierten Zelle in der Heatmap.
        /// </summary>
        public OptimiererRaster BestRaster
            => Feinraster != null && ReferenceEquals(Feinraster.BestPunkt, BestPunkt) ? Feinraster : Grobraster;
    }
}
