using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Strategie (d) Preissteuerung und Arbitrage (Fachkonzept 6.5, Arbeitspaket
    /// AP10): Dauernutzung plus die Netzpfade aus dem
    /// <see cref="ArbitragePlaner"/> - Netzladung zu <c>p_netzlade</c>, Verkauf zu
    /// <c>erloes</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zwei Schritte, klar getrennt.</b> Zuerst plant der
    /// <see cref="ArbitragePlaner"/> die Netzpfade des ganzen Jahres
    /// (Rolling-Horizon-Greedy ueber 24-h-Fenster mit vollstaendiger Pfadpruefung),
    /// dann faehrt diese Strategie den Plan ab. Der Dispatch selbst entscheidet nichts
    /// mehr - er setzt um.
    /// </para>
    /// <code>
    /// fuer jedes Intervall i:                                   # Fachkonzept 6.2
    ///     E_ac_ch = 0 ; E_ac_dis = 0
    ///     if E_quelle &gt; 0:  E_ac_ch  = min(E_quelle,  P*dt, (SoC_max - SoC)/eta_ch)
    ///     else:                E_ac_dis = min(E_defizit, P*dt, (SoC - SoC_min)*eta_dis)
    ///
    ///     if E_ac_ch == 0 and E_ac_dis == 0:                    # Erweiterungsbloecke
    ///         if ladefenster(i):     E_ch_netz = min(P*dt, (SoC_max - SoC)/eta_ch, Plan)
    ///         elif verkaufsfenster(i): E_verk  = min(P*dt, (SoC - SoC_min)*eta_dis, Plan)
    ///
    ///     SoC += (E_ac_ch + E_ch_netz)*eta_ch - (E_ac_dis + E_verk)/eta_dis
    ///     F[i] = + E_dis_last*p_bezug[i]/100
    ///            - E_ch_pv*v_pv[i]/100 - E_ch_bhkw*v_bhkw[i]/100
    ///            - E_ch_netz*p_netzlade[i]/100 + E_verk*erloes[i]/100
    /// </code>
    /// <para>
    /// <b>Der Eigenverbrauchsfluss hat Vorrang</b> und laeuft unveraendert wie in der
    /// <see cref="Dauernutzung"/>; die Netzpfade bekommen nur, was danach im Intervall
    /// frei bleibt (Fachkonzept 6.2, Erweiterungsbloecke, und 2.2, Merit-Order).
    /// </para>
    /// <para>
    /// <b>Anker: ohne Netzpfade identisch zur Dauernutzung.</b> Sind weder Netzladung
    /// noch Netzentladung zugelassen (oder ist <c>optionen</c> <c>null</c>), bleiben
    /// <c>E_ch_netz</c> und <c>E_verk</c> in jedem Intervall 0. Die beiden
    /// Erweiterungen haengen an <c>if (… &gt; 0.0)</c> und werden dann nicht
    /// ausgefuehrt - Ladezustandspfad, Geldwertreihe und Jahressumme sind
    /// <b>bitgleich</b> zur Dauernutzung. Der Testfall
    /// <c>ArbitrageTests.OhneNetzpfade_Ist_Bitgleich_Zur_Dauernutzung</c> haelt das
    /// ueber volle Jahreslaeufe fest.
    /// </para>
    /// <para>
    /// <b>Kein Excel-Kompatibilitaetsmodus.</b> Die Arbitragelogik der V7-Mappe war
    /// nicht ausfuehrbar und ihre Ergebnisse mit keinem Datenstand reproduzierbar
    /// (Fachkonzept 6.5); es gibt also nichts, wogegen ein Kompatibilitaetsmodus
    /// kompatibel waere. Dieselbe Lage wie bei der <see cref="Nachtnutzung"/> -
    /// entsprechend dieselbe Behandlung ueber
    /// <see cref="SchluesselOhneExcelReferenz"/>.
    /// </para>
    /// <para>
    /// <b>Zur Verdopplung der Dispatch-Schleife.</b> Wiederverwendet wird alles, was
    /// ohne Aenderung an bestehenden Dateien wiederverwendbar war:
    /// <see cref="Vorverarbeitung"/>, <see cref="Numerik.SummeSequenziell(double[])"/>,
    /// <see cref="Wirtschaftlichkeit"/>, <see cref="SpeicherKennzahlen"/> und
    /// <see cref="SpeicherErgebnis"/>. Der Schleifenrumpf steht in
    /// <c>Dauernutzung.BerechneEnergetisch</c> als <c>private static</c> und laesst sich
    /// nur durch eine Aenderung genau dieser Datei teilen. Er ist deshalb hier
    /// nachgebildet - zeichengetreu, damit die Bitgleichheit ohne Netzpfade beweisbar
    /// bleibt. Fundstelle der Vorlage: <c>Dauernutzung.cs</c>, Methode
    /// <c>BerechneEnergetisch</c> (Begrenzungsreihenfolge, Merit-Order,
    /// SoC-Fortschreibung, Bewertung, Kennzahlenaufbau); dieselbe Vorlage nutzt bereits
    /// <c>Nachtnutzung.cs</c>. Wer eine der drei Schleifen aendert, muss die anderen
    /// mitziehen - und dazu die Fenstersimulation in
    /// <c>ArbitragePlaner.Lauf.Simuliere</c>.
    /// </para>
    /// <para>
    /// Die Instanz haelt nur Modus und Optionen und ist damit unveraenderlich und
    /// thread-sicher.
    /// </para>
    /// </remarks>
    public sealed class Arbitrage : ISpeicherStrategie
    {
        /// <summary>
        /// Sprachneutraler Schluessel der Ausnahme, die der Excel-Kompatibilitaetsmodus
        /// wirft; gleichlautend als Ressourcenschluessel <c>ARB_OHNE_EXCEL_REFERENZ</c>
        /// im Hauptprojekt (Muster <see cref="Nachtnutzung.SchluesselOhneExcelReferenz"/>).
        /// </summary>
        public const string SchluesselOhneExcelReferenz = "ARB_OHNE_EXCEL_REFERENZ";

        private readonly SpeicherModus _modus;
        private readonly ArbitrageOptionen? _optionen;

        /// <summary>Erzeugt die Strategie.</summary>
        /// <param name="optionen">
        /// Preisreihen und Schalter der Preissteuerung. <c>null</c> = keine Netzpfade;
        /// die Strategie rechnet dann bitgleich zur <see cref="Dauernutzung"/>.
        /// </param>
        /// <param name="modus">Rechenmodus; der Kompatibilitaetsmodus wird abgelehnt.</param>
        public Arbitrage(ArbitrageOptionen? optionen = null, SpeicherModus modus = SpeicherModus.Energetisch)
        {
            _optionen = optionen;
            _modus = modus;
        }

        /// <summary>Rechenmodus dieser Instanz.</summary>
        public SpeicherModus Modus => _modus;

        /// <summary>Optionen dieser Instanz, oder <c>null</c>.</summary>
        public ArbitrageOptionen? Optionen => _optionen;

        /// <inheritdoc/>
        public string Name => "Arbitrage";

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">
        /// Im Modus <see cref="SpeicherModus.ExcelKompatibilitaet"/> - fuer die
        /// Arbitrage existiert keine brauchbare Excel-Referenz (Fachkonzept 6.5).
        /// </exception>
        public SpeicherErgebnis Berechne(SpeicherEingang eingang, SpeicherParameter p)
        {
            return BerechneMitPlan(eingang, p).Ergebnis;
        }

        /// <summary>
        /// Wie <see cref="Berechne"/>, liefert aber zusaetzlich Netzpfadreihen, Plan
        /// und Kennzahlen der Preissteuerung.
        /// </summary>
        /// <remarks>
        /// Der Rueckgabetyp kann nicht in <see cref="ISpeicherStrategie"/> stehen -
        /// dessen Signatur ist von allen Strategien geteilt. Aufrufer, die die
        /// Netzpfade brauchen (Ergebnisseite, Persistenz), rufen deshalb diese Methode
        /// direkt auf; alle anderen bleiben bei der Schnittstelle.
        /// </remarks>
        public ArbitrageErgebnis BerechneMitPlan(SpeicherEingang eingang, SpeicherParameter p)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (p == null) throw new ArgumentNullException(nameof(p));

            if (_modus == SpeicherModus.ExcelKompatibilitaet)
                throw new NotSupportedException(SchluesselOhneExcelReferenz);

            p.Pruefe();

            if (_optionen != null && _optionen.Anzahl != eingang.Anzahl)
                throw new ArgumentException(
                    "Die Preisreihen der Preissteuerung muessen so lang sein wie die Eingangsreihen.",
                    nameof(eingang));

            ArbitragePlan plan = _optionen == null
                ? ArbitragePlan.Leer(eingang.Anzahl)
                : new ArbitragePlaner().Plane(eingang, p, _optionen);

            return BerechneEnergetisch(eingang, p, _optionen, plan);
        }

        // ------------------------------------------------------------------
        // Energetischer Produktivmodus
        // ------------------------------------------------------------------

        /// <summary>
        /// Dauernutzung mit Quellen-Matrix, SoC-Band, AC-seitigen Wirkungsgraden und
        /// den beiden Netzpfaden (Fachkonzept 5.2, 6, 6.2 und 6.5).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Aufbau, Begrenzungsreihenfolge, Merit-Order, SoC-Fortschreibung, Bewertung
        /// und Kennzahlenblock sind unveraendert aus
        /// <c>Dauernutzung.BerechneEnergetisch</c> uebernommen. Ergaenzt sind genau drei
        /// Stellen, jeweils hinter einem <c>if (… &gt; 0.0)</c>, damit der Fall ohne
        /// Netzpfade bitgleich bleibt: der Erweiterungsblock nach dem Dispatch, die
        /// beiden Zusatzterme in SoC-Fortschreibung und Bewertung, und die
        /// Netzpfadanteile in Bilanz und Kennzahlen.
        /// </para>
        /// <para>
        /// <b>Reservepuffer.</b> Der Verkauf darf nur bis <c>SoC_min + Reserve</c>
        /// gehen; mit dem gesetzten Default 0 (siehe
        /// <see cref="ArbitrageOptionen.ReservepufferKwhStandard"/>) ist das genau die
        /// Bandgrenze der Eigenverbrauchsentladung.
        /// </para>
        /// </remarks>
        private static ArbitrageErgebnis BerechneEnergetisch(
            SpeicherEingang eingang, SpeicherParameter p, ArbitrageOptionen? o, ArbitragePlan plan)
        {
            double[] b = eingang.LastKw;
            double[] c = eingang.PvKw;
            double[] dCt = eingang.PreisCtKwh;
            double[]? bhkwReihe = eingang.BhkwKw;
            double[]? vPvReihe = eingang.VerguetungPvCtKwh;
            double[]? vBhkwReihe = eingang.VerguetungBhkwCtKwh;
            int n = b.Length;

            // Ohne Optionen fuehrt der Plan lauter Nullen; die Preisreihen werden dann
            // nie gelesen. Ein Nullvektor statt null spart die Fallunterscheidung in
            // der Schleife - und die Schleife ist der Ort, an dem es auf jede Zeile
            // ankommt (Bitgleichheit zur Dauernutzung).
            double[] pNetzReihe = o != null ? o.NetzladepreisCtKwh : new double[n];
            double[] erloesReihe = o != null ? o.ErloesCtKwh : new double[n];
            double reserve = o == null ? 0.0 : o.ReservepufferKwh;
            double[] planLaden = plan.NetzladungAcKwh;
            double[] planVerkauf = plan.VerkaufAcKwh;

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
            double[] ladungNetz = new double[n];
            double[] verkaufReihe = new double[n];

            double ladeenergie = 0.0;
            double entladeenergie = 0.0;
            double ladeenergiePv = 0.0;
            double ladeenergieBhkw = 0.0;
            double entladeenergieDc = 0.0;
            double ladeenergieNetz = 0.0;
            double verkaufenergie = 0.0;

            double bezugsersparnis = 0.0;
            double verguetungAbzug = 0.0;
            double ladekosten = 0.0;
            double netzerloes = 0.0;
            double planabweichung = 0.0;

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

                // Erweiterungsbloecke Netzladung und Netzentladung (Fachkonzept 6.2):
                // Sie greifen nur, wenn der Eigenverbrauchsfluss im Intervall nichts tut.
                double chNetz = 0.0;
                double verkauf = 0.0;

                if (charge == 0.0 && discharge == 0.0)
                {
                    if (planLaden[k] > 0.0)
                    {
                        chNetz = maxPower * dt;
                        if (chNetz > (maxLevel - prev) / etaCh) chNetz = (maxLevel - prev) / etaCh;
                        if (chNetz > planLaden[k]) chNetz = planLaden[k];
                        if (chNetz < 0) chNetz = 0.0;
                    }
                    else if (planVerkauf[k] > 0.0)
                    {
                        verkauf = maxPower * dt;
                        if (verkauf > (prev - minLevel - reserve) * etaDis) verkauf = (prev - minLevel - reserve) * etaDis;
                        if (verkauf > planVerkauf[k]) verkauf = planVerkauf[k];
                        if (verkauf < 0) verkauf = 0.0;
                    }
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
                if (chNetz > 0.0) newLevel += chNetz * etaCh;
                if (verkauf > 0.0) newLevel -= verkauf / etaDis;

                if (discharge > 0)
                {
                    eur[k] = discharge * dk / 100.0;
                    bezugsersparnis += eur[k];
                }
                else if (charge > 0)
                {
                    double vPv = vPvReihe == null ? verguetungStandard : vPvReihe[k];
                    double vBhkw = vBhkwReihe == null ? verguetungStandard : vBhkwReihe[k];
                    eur[k] = -anteilPv * vPv / 100.0 - anteilBhkw * vBhkw / 100.0;
                    verguetungAbzug -= eur[k];
                }
                else
                {
                    eur[k] = 0.0;
                }

                if (chNetz > 0.0) eur[k] -= chNetz * pNetzReihe[k] / 100.0;
                if (verkauf > 0.0) eur[k] += verkauf * erloesReihe[k] / 100.0;

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

                // --- Netzpfade: Reihen, Summen, Geld und Bilanz ---
                if (chNetz > 0.0)
                {
                    ladungNetz[k] = chNetz;
                    ladeenergieNetz += chNetz;
                    ladekosten += chNetz * pNetzReihe[k] / 100.0;
                    // Netzladung ERHOEHT den Bezug - anders als die Eigenverbrauchsladung,
                    // die nur die Einspeisung mindert.
                    netzbezugMitSpeicher += chNetz;
                }

                if (verkauf > 0.0)
                {
                    verkaufReihe[k] = verkauf;
                    verkaufenergie += verkauf;
                    entladeenergieDc += verkauf / etaDis;
                    netzerloes += verkauf * erloesReihe[k] / 100.0;
                    // Verkauf ERHOEHT die Einspeisung.
                    einspeisungMitSpeicher += verkauf;
                }

                // Nachweis gegen den V7-Fehler G3: Was der Planer geprueft hat, muss
                // ungekuerzt gefahren worden sein. Beide Differenzen sind konstruktiv
                // nicht negativ (der Dispatch begrenzt nur nach unten).
                planabweichung += (planLaden[k] - chNetz) + (planVerkauf[k] - verkauf);
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

            double vollzyklen = p.CNutzKwh > 0.0 ? entladeenergieDc / p.CNutzKwh : 0.0;

            SpeicherKennzahlen kennzahlen = new SpeicherKennzahlen
            {
                LadeenergiePvKwh = ladeenergiePv,
                LadeenergieBhkwKwh = ladeenergieBhkw,
                EntladeenergieDcKwh = entladeenergieDc,
                AequivalenteVollzyklen = vollzyklen,
                // Reiner Ausweis - K_ver fliesst NICHT in summeF und nicht in dJ
                // (Fachkonzept 5.4, Verwendung 2). Als STEUERGROESSE wirkt c_ver
                // dagegen sehr wohl: in der Spread-Bedingung des Planers (5.4,
                // Verwendung 1) - dort ist es nicht abschaltbar.
                VerschleisskostenEurProA = vollzyklen * p.CNomKwh * p.CVerEurProKwhZyklus,
                // Die Verlustbilanz muss ueber ALLE vier Pfade gebildet werden, sonst
                // schliesst sie bei aktiven Netzpfaden nicht.
                SpeicherverlusteKwh = ladeenergie + ladeenergieNetz
                                      - entladeenergie - verkaufenergie
                                      - (prev - startSoC),
                LastKwh = summeLast,
                ErzeugungPvKwh = summePv,
                ErzeugungBhkwKwh = summeBhkw,
                DirektverbrauchKwh = summeDirekt,
                NetzbezugOhneSpeicherKwh = summeDefizit,
                NetzbezugMitSpeicherKwh = netzbezugMitSpeicher,
                EinspeisungOhneSpeicherKwh = summeUeberschuss,
                EinspeisungMitSpeicherKwh = einspeisungMitSpeicher
            };

            SpeicherErgebnis ergebnis = new SpeicherErgebnis(soc, eur, summeF, ladeenergie, entladeenergie,
                                                            SpeicherModus.Energetisch, w, kennzahlen,
                                                            ladung, entladung);

            ArbitrageKennzahlen arb = new ArbitrageKennzahlen
            {
                LadungNetzKwh = ladeenergieNetz,
                VerkaufKwh = verkaufenergie,
                EntladeenergieDcGesamtKwh = entladeenergieDc,
                BezugsersparnisEur = bezugsersparnis,
                EntgangeneVerguetungEur = verguetungAbzug,
                LadekostenEur = ladekosten,
                NetzerloesEur = netzerloes,
                ZyklenbudgetDcKwhProA = plan.ZyklenbudgetDcKwhProA,
                BudgetErschoepft = plan.BudgetErschoepft,
                VerschleissCtKwh = plan.VerschleissCtKwh,
                PaareAngenommen = plan.PaareAngenommen,
                VerkaufsslotsAngenommen = plan.VerkaufsslotsAngenommen,
                VerworfenPfad = plan.VerworfenPfad,
                AbweichungVomPlanKwh = planabweichung
            };

            return new ArbitrageErgebnis(ergebnis, ladungNetz, verkaufReihe, plan, arb);
        }
    }
}
