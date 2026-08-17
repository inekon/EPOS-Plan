using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Strategie (b) Dauernutzung (Fachkonzept 6.2): Laden aus Erzeugungsueberschuss,
    /// Entladen gegen die Residuallast.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Zwei Modi, gesetzt ueber den Konstruktor:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="SpeicherModus.ExcelKompatibilitaet"/> - zeichengetreue
    ///     Portierung von <c>Sub SpeicherSimulation_cont()</c> der V7-Mappe ueber
    ///     die verifizierte Python-Referenz <c>speicher_sim.py</c>. Vergleichs- und
    ///     Zuweisungsreihenfolge der Schleife sind unveraendert uebernommen, damit
    ///     die IEEE-754-Doubles bitgleich herauskommen.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="SpeicherModus.Energetisch"/> - Produktivmodus mit Quellen-Matrix
    ///     (PV / BHKW-Ueberschuss), SoC-Band und AC-seitigen Wirkungsgraden nach
    ///     Fachkonzept 5.2 und 6.
    ///   </description></item>
    /// </list>
    /// <para>
    /// Der Kompatibilitaetsmodus kennt ausschliesslich Last und PV. Eine BHKW-Reihe,
    /// die Quellenflags und c_ver im Eingang bleiben dort <b>ohne Wirkung</b> - er
    /// bildet die V7-Mappe nach, und die kannte nichts davon.
    /// </para>
    /// <para>
    /// Die Instanz haelt nur den Modus und ist damit unveraenderlich und
    /// thread-sicher; dieselbe Instanz darf in <c>Parallel.For</c> verwendet werden.
    /// </para>
    /// </remarks>
    public sealed class Dauernutzung : ISpeicherStrategie
    {
        private readonly SpeicherModus _modus;

        /// <summary>Erzeugt die Strategie im angegebenen Modus (Default: energetisch).</summary>
        public Dauernutzung(SpeicherModus modus = SpeicherModus.Energetisch)
        {
            _modus = modus;
        }

        /// <summary>Rechenmodus dieser Instanz.</summary>
        public SpeicherModus Modus => _modus;

        /// <inheritdoc/>
        public string Name => _modus == SpeicherModus.ExcelKompatibilitaet
            ? "Dauernutzung (Excel-Kompatibilitaet)"
            : "Dauernutzung";

        /// <inheritdoc/>
        public SpeicherErgebnis Berechne(SpeicherEingang eingang, SpeicherParameter p)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (p == null) throw new ArgumentNullException(nameof(p));
            p.Pruefe();

            return _modus == SpeicherModus.ExcelKompatibilitaet
                ? BerechneExcelKompatibel(eingang, p)
                : BerechneEnergetisch(eingang, p);
        }

        // ------------------------------------------------------------------
        // Excel-Kompatibilitaetsmodus
        // ------------------------------------------------------------------

        /// <summary>
        /// Portierung von <c>simulate_speicher</c> (speicher_sim.py) bzw.
        /// <c>Sub SpeicherSimulation_cont()</c> (Speicher.bas).
        /// </summary>
        /// <remarks>
        /// VBA-Eigenheiten, die bewusst uebernommen sind:
        /// <list type="bullet">
        ///   <item><description><c>prev</c> startet bei 0, nicht bei SoC_min.</description></item>
        ///   <item><description>Index 0 wird nicht simuliert (SoC[0] = F[0] = 0), das
        ///     erste Viertelstundenintervall faellt aus der Bilanz.</description></item>
        ///   <item><description>Der Preis steuert nicht die Entscheidung, nur die Bewertung.</description></item>
        ///   <item><description>Wirkungsgrad energetisch 100 %; der Verlustfaktor wirkt erst
        ///     pauschal auf die Euro-Summe.</description></item>
        /// </list>
        /// </remarks>
        private static SpeicherErgebnis BerechneExcelKompatibel(SpeicherEingang eingang, SpeicherParameter p)
        {
            double[] b = eingang.LastKw;
            double[] c = eingang.PvKw;
            double[] dCt = eingang.PreisCtKwh;
            int n = b.Length;

            double maxPower = p.PKw;
            double minLevel = p.SoCMinKwh;
            double maxLevel = p.SoCMaxKwh;
            double verguetung = p.VerguetungCtKwh;
            double dt = p.DtH;

            double[] soc = new double[n];   // arrE
            double[] eur = new double[n];   // arrF
            double[] ladung = new double[n];
            double[] entladung = new double[n];

            double ladeenergie = 0.0;
            double entladeenergie = 0.0;
            double prev = 0.0;              // VBA: prev = 0

            // VBA: For i = 2 To lastRow - 1  ->  hier k = 1 .. n-1
            for (int k = 1; k < n; k++)
            {
                double bk = b[k];
                double ck = c[k];
                double dk = dCt[k];

                double charge = 0.0;
                double discharge = 0.0;

                if (ck > bk)
                {
                    // PV-Ueberschuss: laden (nur aus PV, nie aus dem Netz)
                    charge = (ck - bk) * dt;
                    if (charge > maxPower * dt) charge = maxPower * dt;
                    if (charge > maxLevel - prev) charge = maxLevel - prev;
                    if (charge < 0) charge = 0.0;
                }
                else
                {
                    // ck <= bk -> Residuallast: entladen
                    discharge = (bk - ck) * dt;
                    if (discharge > maxPower * dt) discharge = maxPower * dt;
                    if (discharge > prev - minLevel) discharge = prev - minLevel;
                    if (discharge < 0) discharge = 0.0;
                }

                double newLevel = prev + charge - discharge;
                double delta = newLevel - prev;

                if (delta < 0) eur[k] = -delta * dk / 100.0;              // Entladung: vermiedener Bezug
                else if (delta > 0) eur[k] = -delta * verguetung / 100.0; // Ladung: entgangene Verguetung
                else eur[k] = 0.0;

                soc[k] = newLevel;
                prev = newLevel;

                ladung[k] = charge;
                entladung[k] = discharge;

                ladeenergie += charge;
                entladeenergie += discharge;
            }

            double summeF = Numerik.SummeSequenziell(eur);

            // V7: N10 = SUM(F) * (1 - J5). Der pauschale Verlustfaktor gehoert
            // ausschliesslich in diesen Modus.
            double ertragReferenzjahr = summeF * (1.0 - p.VerlustfaktorPauschal);

            WirtschaftlichkeitErgebnis w = Wirtschaftlichkeit.Berechne(new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = ertragReferenzjahr,
                InvestitionEur = p.InvestitionEur,
                Kapitalzins = p.Kapitalzins,
                NutzungsdauerA = p.NutzungsdauerA,
                DegradationProA = 0.0   // V7 kennt keine Degradation
            });

            // Kennzahlen: Der Kompatibilitaetsmodus kennt weder Quellen-Matrix noch
            // Verlustmodell. Ausgewiesen wird deshalb nur, was die V7-Logik hergibt -
            // die Ladeenergie stammt per Definition aus PV, DC- und AC-Seite fallen
            // wegen eta = 1 zusammen, und c_ver ist hier 0 (Fachkonzept 5.2).
            SpeicherKennzahlen kennzahlen = new SpeicherKennzahlen
            {
                LadeenergiePvKwh = ladeenergie,
                EntladeenergieDcKwh = entladeenergie,
                AequivalenteVollzyklen = Vollzyklen(entladeenergie, p.CNutzKwh),
                VerschleisskostenEurProA = 0.0
            };

            return new SpeicherErgebnis(soc, eur, summeF, ladeenergie, entladeenergie,
                                        SpeicherModus.ExcelKompatibilitaet, w, kennzahlen,
                                        ladung, entladung);
        }

        /// <summary>
        /// Aequivalente Vollzyklen <c>n_zyk = E_dc,entnommen / C_nutz</c>
        /// (Fachkonzept 5.4); 0, wenn kein nutzbares Band definiert ist.
        /// </summary>
        private static double Vollzyklen(double entladeenergieDcKwh, double cNutzKwh)
            => cNutzKwh > 0.0 ? entladeenergieDcKwh / cNutzKwh : 0.0;

        // ------------------------------------------------------------------
        // Energetischer Produktivmodus
        // ------------------------------------------------------------------

        /// <summary>
        /// Dauernutzung mit Quellen-Matrix, SoC-Band und AC-seitigen Wirkungsgraden
        /// (Fachkonzept 5.2, 6 und 6.2).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Je Intervall laeuft zuerst die Vorverarbeitung nach Fachkonzept 6
        /// (<see cref="SpeicherEngine.Vorverarbeitung"/>), dann der Dispatch:
        /// </para>
        /// <code>
        /// Laden:    E_ac &lt;= min(E_quelle,  P*dt, (SoC_max - SoC)/eta_ch);  SoC += E_ac*eta_ch
        /// Entladen: E_ac &lt;= min(E_defizit, P*dt, (SoC - SoC_min)*eta_dis); SoC -= E_ac/eta_dis
        /// </code>
        /// <para>
        /// Bewertet wird die AC-seitige Energie: Entladung mit dem Bezugspreis
        /// (vermiedener Netzbezug), Ladung nach <b>Merit-Order PV vor BHKW</b>
        /// (Fachkonzept 2.2) - erst E_pv_frei bis zur Ladegrenze, der Rest aus
        /// E_bhkw_frei:
        /// </para>
        /// <code>
        /// F[i] = + E_ac_dis*p_bezug[i]/100
        ///        - E_pv_anteil*v_pv[i]/100 - E_bhkw_anteil*v_bhkw[i]/100
        /// </code>
        /// <para>
        /// Reihenfolge der Begrenzungen wie im Kompatibilitaetsmodus, damit beide Modi
        /// bei eta_RT = 1 identisch rechnen.
        /// </para>
        /// <para>
        /// <b>Rueckwaertskompatibilitaet.</b> Ohne BHKW-Reihe und mit zulaessiger PV
        /// ist das Ergebnis bitgleich zur Fassung vor AP2: Mit E_bhkw = 0 gilt
        /// E_restlast = E_last, also E_quelle = max(0, E_pv - E_last) und
        /// E_defizit = max(0, E_last - E_pv). Die Fallunterscheidung
        /// <c>E_quelle &gt; 0</c> trifft damit genau dann zu, wenn frueher
        /// <c>P_pv &gt; P_last</c> galt. Die Umformung
        /// <c>P_pv*dt - P_last*dt</c> statt <c>(P_pv - P_last)*dt</c> ist bei
        /// dt = 0,25 h exakt, weil die Skalierung mit einer Zweierpotenz in IEEE-754
        /// fehlerfrei ist und deshalb mit der Subtraktion vertauscht.
        /// </para>
        /// <para>
        /// Netzladung und Netzentladung (Fachkonzept 6.2, Erweiterungsbloecke) sind
        /// hier noch nicht implementiert - sie kommen mit AP10.
        /// </para>
        /// </remarks>
        private static SpeicherErgebnis BerechneEnergetisch(SpeicherEingang eingang, SpeicherParameter p)
        {
            double[] b = eingang.LastKw;
            double[] c = eingang.PvKw;
            double[] dCt = eingang.PreisCtKwh;
            double[]? bhkwReihe = eingang.BhkwKw;
            double[]? vPvReihe = eingang.VerguetungPvCtKwh;
            double[]? vBhkwReihe = eingang.VerguetungBhkwCtKwh;
            int n = b.Length;

            double maxPower = p.PKw;
            double minLevel = p.SoCMinKwh;
            double maxLevel = p.SoCMaxKwh;
            double verguetungStandard = p.VerguetungCtKwh;
            double dt = p.DtH;
            double etaCh = p.EtaCh;
            double etaDis = p.EtaDis;
            bool pvZulaessig = p.PvZulaessig;
            bool bhkwZulaessig = p.BhkwUeberschussZulaessig;

            double[] soc = new double[n];
            double[] eur = new double[n];
            double[] ladung = new double[n];
            double[] entladung = new double[n];

            double ladeenergie = 0.0;
            double entladeenergie = 0.0;
            double ladeenergiePv = 0.0;
            double ladeenergieBhkw = 0.0;
            double entladeenergieDc = 0.0;

            double summeLast = 0.0;
            double summePv = 0.0;
            double summeBhkw = 0.0;
            double summeDirekt = 0.0;
            double summeDefizit = 0.0;
            double summeUeberschuss = 0.0;
            double netzbezugMitSpeicher = 0.0;
            double einspeisungMitSpeicher = 0.0;

            double startSoC = p.StartSoCEffektivKwh;
            double prev = startSoC;

            for (int k = 0; k < n; k++)
            {
                double bhkwKw = bhkwReihe == null ? 0.0 : bhkwReihe[k];
                IntervallEnergien e = Vorverarbeitung.Berechne(
                    b[k], c[k], bhkwKw, dt, pvZulaessig, bhkwZulaessig);

                double dk = dCt[k];

                double charge = 0.0;      // E_ac_ch  [kWh] AC-seitig
                double discharge = 0.0;   // E_ac_dis [kWh] AC-seitig

                if (e.EQuelleKwh > 0.0)
                {
                    charge = e.EQuelleKwh;
                    if (charge > maxPower * dt) charge = maxPower * dt;
                    if (charge > (maxLevel - prev) / etaCh) charge = (maxLevel - prev) / etaCh;
                    if (charge < 0) charge = 0.0;
                }
                else
                {
                    discharge = e.EDefizitKwh;
                    if (discharge > maxPower * dt) discharge = maxPower * dt;
                    if (discharge > (prev - minLevel) * etaDis) discharge = (prev - minLevel) * etaDis;
                    if (discharge < 0) discharge = 0.0;
                }

                // Merit-Order beim Laden: PV vor BHKW (Fachkonzept 2.2). Die
                // Aufteilung ist rein bewertungsrelevant - energetisch ist der
                // Speicher quellenblind.
                double anteilPv = 0.0;
                double anteilBhkw = 0.0;
                if (charge > 0.0)
                {
                    anteilPv = e.EPvQuelleKwh;
                    if (anteilPv > charge) anteilPv = charge;
                    anteilBhkw = charge - anteilPv;
                }

                double newLevel = prev + charge * etaCh - discharge / etaDis;

                if (discharge > 0)
                {
                    eur[k] = discharge * dk / 100.0;
                }
                else if (charge > 0)
                {
                    double vPv = vPvReihe == null ? verguetungStandard : vPvReihe[k];
                    double vBhkw = vBhkwReihe == null ? verguetungStandard : vBhkwReihe[k];
                    eur[k] = -anteilPv * vPv / 100.0 - anteilBhkw * vBhkw / 100.0;
                }
                else
                {
                    eur[k] = 0.0;
                }

                soc[k] = newLevel;
                prev = newLevel;

                ladung[k] = charge;
                entladung[k] = discharge;

                ladeenergie += charge;
                entladeenergie += discharge;
                ladeenergiePv += anteilPv;
                ladeenergieBhkw += anteilBhkw;
                entladeenergieDc += discharge / etaDis;

                summeLast += e.ELastKwh;
                summePv += e.EPvKwh;
                summeBhkw += e.EBhkwKwh;
                summeDirekt += e.EDirektKwh;
                summeDefizit += e.EDefizitKwh;
                summeUeberschuss += e.EUeberschussKwh;
                netzbezugMitSpeicher += e.EDefizitKwh - discharge;
                einspeisungMitSpeicher += e.EUeberschussKwh - charge;
            }

            double summeF = Numerik.SummeSequenziell(eur);

            // Kein pauschaler Verlustabschlag - die Verluste stecken bereits
            // energetisch in eta_ch / eta_dis (Fachkonzept 5.2).
            WirtschaftlichkeitErgebnis w = Wirtschaftlichkeit.Berechne(new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = summeF,
                InvestitionEur = p.InvestitionEur,
                Kapitalzins = p.Kapitalzins,
                NutzungsdauerA = p.NutzungsdauerA,
                DegradationProA = p.DegradationProA
            });

            double vollzyklen = Vollzyklen(entladeenergieDc, p.CNutzKwh);

            SpeicherKennzahlen kennzahlen = new SpeicherKennzahlen
            {
                LadeenergiePvKwh = ladeenergiePv,
                LadeenergieBhkwKwh = ladeenergieBhkw,
                EntladeenergieDcKwh = entladeenergieDc,
                AequivalenteVollzyklen = vollzyklen,
                // Reiner Ausweis - K_ver fliesst NICHT in summeF und nicht in ΔJ
                // (Fachkonzept 5.4, Verwendung 2; die Zielfunktions-Option folgt in AP3).
                VerschleisskostenEurProA = vollzyklen * p.CNomKwh * p.CVerEurProKwhZyklus,
                SpeicherverlusteKwh = ladeenergie - entladeenergie - (prev - startSoC),
                LastKwh = summeLast,
                ErzeugungPvKwh = summePv,
                ErzeugungBhkwKwh = summeBhkw,
                DirektverbrauchKwh = summeDirekt,
                NetzbezugOhneSpeicherKwh = summeDefizit,
                NetzbezugMitSpeicherKwh = netzbezugMitSpeicher,
                EinspeisungOhneSpeicherKwh = summeUeberschuss,
                EinspeisungMitSpeicherKwh = einspeisungMitSpeicher
            };

            return new SpeicherErgebnis(soc, eur, summeF, ladeenergie, entladeenergie,
                                        SpeicherModus.Energetisch, w, kennzahlen,
                                        ladung, entladung);
        }
    }
}
