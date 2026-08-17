using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Strategie (a) Start Nachtnutzung (Fachkonzept 6.1): Entladen ausschliesslich,
    /// wenn die PV-Erzeugung null ist; solange die PV erzeugt, wird nur geladen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zweck.</b> Der Speicher soll fuer die Nutzung nach Sonnenuntergang nicht
    /// geleert sein. Die Regel braucht weder Klimadaten noch Sonnenstand - das
    /// Kriterium ist die PV-Reihe selbst.
    /// </para>
    /// <code>
    /// fuer jedes Intervall i:
    ///     E_ac_ch = 0 ; E_ac_dis = 0
    ///     if P_pv[i] &gt; eps:                     # Tag: nur laden
    ///         E_ac_ch  = min(E_quelle,  P*dt, (SoC_max - SoC)/eta_ch)
    ///     else:                                 # PV = 0: entladen erlaubt
    ///         E_ac_dis = min(E_defizit, P*dt, (SoC - SoC_min)*eta_dis)
    ///         if E_quelle &gt; 0:                  # BHKW-Ueberschuss nachts
    ///             E_ac_ch = min(E_quelle, P*dt, (SoC_max - SoC)/eta_ch)
    ///     SoC += E_ac_ch*eta_ch - E_ac_dis/eta_dis
    ///     bewerte(i, E_ac_ch, E_ac_dis)
    /// </code>
    /// <para>
    /// <b>Laden und Entladen schliessen einander weiterhin aus.</b> Der Nachtzweig
    /// sieht so aus, als koennten beide Zweige zugleich greifen; das kann nicht
    /// eintreten, weil E_quelle und E_defizit konstruktiv disjunkt sind
    /// (<see cref="Vorverarbeitung"/>): Ist E_defizit &gt; 0, gilt
    /// E_last &gt; E_pv + E_bhkw, damit sind E_pv_frei und E_bhkw_frei beide 0.
    /// </para>
    /// <para>
    /// <b>Einordnung (Fachkonzept 6.1).</b> Die V7-Mappe hinterlegte fuer den Button
    /// "Start Nachtnutzung" nur eine Altversion, deren Entladezweig die volle Last
    /// statt der Residuallast ansetzte und die weder aus BHKW noch aus dem Netz laden
    /// konnte. Diese Fassung ist deshalb eine <b>Neudefinition, kein Port</b>: Sie ist
    /// nicht gegen Excel-Werte verifizierbar und traegt eigene Tests
    /// (<c>NachtnutzungTests</c>). Aus demselben Grund kennt sie
    /// <see cref="SpeicherModus.ExcelKompatibilitaet"/> nicht - siehe
    /// <see cref="SchluesselOhneExcelReferenz"/>.
    /// </para>
    /// <para>
    /// <b>Verhaeltnis zur <see cref="Dauernutzung"/>.</b> Ohne PV-Erzeugung
    /// (P_pv identisch 0 ueber alle Intervalle) greift ausschliesslich der
    /// Nachtzweig, und der rechnet Ausdruck fuer Ausdruck wie die Dauernutzung -
    /// beide Strategien liefern dann bitgleiche Ergebnisse. Der Unterschied entsteht
    /// erst dort, wo PV erzeugt: Die Dauernutzung entlaedt auch tagsueber gegen die
    /// Residuallast, die Nachtnutzung nicht.
    /// </para>
    /// <para>
    /// Die Instanz haelt nur den Modus und ist damit unveraenderlich und
    /// thread-sicher; dieselbe Instanz darf in <c>Parallel.For</c> verwendet werden.
    /// </para>
    /// <para>
    /// <b>Zur Verdopplung der Dispatch-Schleife.</b> Alles, was ohne Aenderung an
    /// bestehenden Dateien wiederverwendbar war, wird wiederverwendet:
    /// <see cref="Vorverarbeitung"/> (Intervallzerlegung samt Quellen-Matrix),
    /// <see cref="Numerik.SummeSequenziell(double[])"/>, <see cref="Wirtschaftlichkeit"/>,
    /// <see cref="SpeicherKennzahlen"/> und <see cref="SpeicherErgebnis"/>. Der
    /// Schleifenrumpf selbst steht in <c>Dauernutzung.BerechneEnergetisch</c> als
    /// <c>private static</c> und laesst sich nur durch eine Aenderung genau dieser
    /// Datei teilen. Er ist deshalb hier nachgebildet - zeichengetreu, damit die
    /// Bitgleichheit im PV-freien Fall beweisbar bleibt. Fundstelle der Vorlage:
    /// <c>Dauernutzung.cs</c>, Methode <c>BerechneEnergetisch</c>
    /// (Begrenzungsreihenfolge, Merit-Order, SoC-Fortschreibung, Bewertung,
    /// Kennzahlenaufbau). Wer eine der beiden Schleifen aendert, muss die andere
    /// mitziehen; zusammenlegen laesst sie ein Paket, das beide Dateien anfassen darf.
    /// </para>
    /// </remarks>
    public sealed class Nachtnutzung : ISpeicherStrategie
    {
        /// <summary>
        /// Sprachneutraler Schluessel der Ausnahme, die der Excel-Kompatibilitaetsmodus
        /// wirft; gleichlautend als Ressourcenschluessel <c>NACHT_OHNE_EXCEL_REFERENZ</c>
        /// im Hauptprojekt (Muster <c>GanglinienPruefung</c>).
        /// </summary>
        /// <remarks>
        /// Die Engine bleibt sprachneutral: Sie liefert den Schluessel, den Text holt
        /// erst die Oberflaeche aus <c>MyResource.Resource</c> (Drei-Schichten-Regel
        /// der Projekt-CLAUDE.md).
        /// </remarks>
        public const string SchluesselOhneExcelReferenz = "NACHT_OHNE_EXCEL_REFERENZ";

        /// <summary>
        /// Schwelle eps [kW], ab der ein Intervall als "PV erzeugt" gilt
        /// (Fachkonzept 6.1: <c>P_pv[i] &gt; eps</c>).
        /// </summary>
        /// <remarks>
        /// Reihen aus der Simulationskette fuehren nachts exakte Nullen; die Schwelle
        /// faengt nur Rundungsreste importierter oder auf 15 min expandierter Reihen
        /// ab. 1e-9 kW = 1 Mikrowatt liegt so weit unter jeder realen Erzeugung, dass
        /// die Schwelle keinen Tagesbetrieb ausblenden kann - und negative Restwerte
        /// zaehlen damit als Nacht.
        /// </remarks>
        public const double PvSchwelleKw = 1e-9;

        private readonly SpeicherModus _modus;

        /// <summary>Erzeugt die Strategie im angegebenen Modus (Default: energetisch).</summary>
        /// <remarks>
        /// Der Konstruktor nimmt <see cref="SpeicherModus.ExcelKompatibilitaet"/>
        /// entgegen, ohne zu werfen - die Ausnahme kommt erst aus
        /// <see cref="Berechne"/>. So bleibt die Strategieauswahl der Aufrufer
        /// symmetrisch zur <see cref="Dauernutzung"/>, und <see cref="Name"/> bleibt
        /// in jedem Fall abfragbar.
        /// </remarks>
        public Nachtnutzung(SpeicherModus modus = SpeicherModus.Energetisch)
        {
            _modus = modus;
        }

        /// <summary>Rechenmodus dieser Instanz.</summary>
        public SpeicherModus Modus => _modus;

        /// <inheritdoc/>
        public string Name => "Nachtnutzung";

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">
        /// Wenn die Instanz im Modus <see cref="SpeicherModus.ExcelKompatibilitaet"/>
        /// steht. Fuer die Nachtnutzung existiert <b>keine</b> Excel-Referenz: Die
        /// Altversion der V7-Mappe war als Dauernutzungssimulation unbrauchbar und
        /// wurde bewusst nicht portiert (Fachkonzept 6.1). Ein Kompatibilitaetsmodus
        /// haette hier nichts, wogegen er kompatibel waere - er wuerde nur ein
        /// Ergebnis liefern, das nichts nachstellt. Die Meldung traegt den
        /// sprachneutralen Schluessel <see cref="SchluesselOhneExcelReferenz"/>; die
        /// Oberflaeche bietet die Kombination gar nicht erst an.
        /// </exception>
        public SpeicherErgebnis Berechne(SpeicherEingang eingang, SpeicherParameter p)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (p == null) throw new ArgumentNullException(nameof(p));

            if (_modus == SpeicherModus.ExcelKompatibilitaet)
                throw new NotSupportedException(SchluesselOhneExcelReferenz);

            p.Pruefe();
            return BerechneEnergetisch(eingang, p);
        }

        // ------------------------------------------------------------------
        // Energetischer Produktivmodus
        // ------------------------------------------------------------------

        /// <summary>
        /// Nachtnutzung mit Quellen-Matrix, SoC-Band und AC-seitigen Wirkungsgraden
        /// (Fachkonzept 5.2, 6 und 6.1).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Aufbau, Begrenzungsreihenfolge, Merit-Order, SoC-Fortschreibung, Bewertung
        /// und Kennzahlenblock sind unveraendert aus
        /// <c>Dauernutzung.BerechneEnergetisch</c> uebernommen. Der <b>einzige</b>
        /// fachliche Unterschied ist die Fallunterscheidung am Kopf der Schleife:
        /// </para>
        /// <code>
        /// Dauernutzung:  if (E_quelle &gt; 0) laden   else entladen
        /// Nachtnutzung:  if (P_pv &gt; eps)  laden   else { entladen; bei E_quelle &gt; 0 zusaetzlich laden }
        /// </code>
        /// <para>
        /// Daraus folgt unmittelbar die Kerneigenschaft: <b>Solange P_pv &gt; eps ist,
        /// bleibt E_ac_dis = 0.</b> Der Tageszweig kennt keinen Entladepfad.
        /// </para>
        /// <para>
        /// Netzladung und Netzentladung (Fachkonzept 6.2, Erweiterungsbloecke) sind
        /// hier - wie in der Dauernutzung - noch nicht implementiert; sie kommen mit
        /// AP10.
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

                if (c[k] > PvSchwelleKw)
                {
                    // Tag: NUR laden. Auch ein Defizit bleibt hier ungedeckt - genau
                    // das ist der Zweck der Strategie (Fachkonzept 6.1).
                    if (e.EQuelleKwh > 0.0)
                    {
                        charge = e.EQuelleKwh;
                        if (charge > maxPower * dt) charge = maxPower * dt;
                        if (charge > (maxLevel - prev) / etaCh) charge = (maxLevel - prev) / etaCh;
                        if (charge < 0) charge = 0.0;
                    }
                }
                else
                {
                    // PV = 0: entladen erlaubt.
                    discharge = e.EDefizitKwh;
                    if (discharge > maxPower * dt) discharge = maxPower * dt;
                    if (discharge > (prev - minLevel) * etaDis) discharge = (prev - minLevel) * etaDis;
                    if (discharge < 0) discharge = 0.0;

                    // Nachts steht kein PV-Ueberschuss zur Verfuegung; E_quelle kann
                    // hier nur BHKW-Ueberschuss sein. Weil E_quelle und E_defizit
                    // disjunkt sind, ist discharge in diesem Zweig zwingend 0 - beide
                    // Richtungen zugleich bleiben ausgeschlossen.
                    if (e.EQuelleKwh > 0.0)
                    {
                        charge = e.EQuelleKwh;
                        if (charge > maxPower * dt) charge = maxPower * dt;
                        if (charge > (maxLevel - prev) / etaCh) charge = (maxLevel - prev) / etaCh;
                        if (charge < 0) charge = 0.0;
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

            double vollzyklen = p.CNutzKwh > 0.0 ? entladeenergieDc / p.CNutzKwh : 0.0;

            SpeicherKennzahlen kennzahlen = new SpeicherKennzahlen
            {
                LadeenergiePvKwh = ladeenergiePv,
                LadeenergieBhkwKwh = ladeenergieBhkw,
                EntladeenergieDcKwh = entladeenergieDc,
                AequivalenteVollzyklen = vollzyklen,
                // Reiner Ausweis - K_ver fliesst NICHT in summeF und nicht in dJ
                // (Fachkonzept 5.4, Verwendung 2).
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
