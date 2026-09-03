using System;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Jahresscharfe PV-Einspeiseerlöse eines Projekts (PV-Konzept § 4.4/§ 4.6,
    /// Etappe P4) samt Ausweisgrößen. Alle Beträge in €, Sätze in ct/kWh.
    /// </summary>
    public sealed class PvErloesErgebnis
    {
        /// <summary>Nominale Jahresbeträge [€/a], Index 1…T — die Reihe
        /// <c>ErloesReihe.PV_VERGUETUNG</c> der Kapitalwertrechnung.</summary>
        public double[] JeJahr;

        /// <summary>Angewandter anzulegender Wert (Mix bzw. Override) [ct/kWh].</summary>
        public double AwMixCt;

        /// <summary>Installierte Leistung der Rechnung [kWp] (Override oder V3).</summary>
        public double Kwp;

        /// <summary>Vermarktungsform (DbWerte.PV_VERMARKTUNG_*).</summary>
        public string Vermarktungsform = "";

        /// <summary>Vergütungsfähige Einspeisung des Basisjahres [MWh] (nach V2).</summary>
        public double EinspeisungMWh;

        /// <summary>§ 51-Vergütungsausfall des Basisjahres [kWh] / [€].</summary>
        public double VerguetungsausfallKwh;
        public double VerguetungsausfallEur;

        /// <summary>true, wenn der Ausfallanteil aus einer Spotreihe GEMESSEN wurde
        /// (Stufe 2); false = Stufe-1-Pauschale.</summary>
        public bool AusfallGemessen;

        /// <summary>Angewandter Ausfallanteil [%] (gemessen oder Pauschale).</summary>
        public double AusfallanteilProzent;

        /// <summary>§ 51a-Gutschrift [€], dem LETZTEN Vergütungsjahr der Reihe
        /// zugeschlagen (vereinfachte Barwert-Abbildung der Laufzeitverlängerung).</summary>
        public double Kompensation51aEur;

        /// <summary>60-%-Kappungsverlust des Basisjahres [kWh] (nur Stufe 2 messbar).</summary>
        public double KappungsverlustKwh;

        /// <summary>Marktprämie des ersten Jahres [€] (nur Marktprämien-Fall).</summary>
        public double MarktpraemieEurJahr1;

        /// <summary>Führender Vergütungssatz des ersten Jahres [ct/kWh] — der
        /// V4-Umschluss für die Speicherbewertung (<c>v_pv</c>, Entscheidung F7).</summary>
        public double SatzJahr1Ct;

        /// <summary>Letztes Betrachtungsjahr mit EEG-Vergütung (1-basiert).</summary>
        public int LetztesVerguetungsjahr;

        public bool Par51Angewendet;
        public bool KappungAngewendet;
        public string Herleitung = "";
        public bool Unvollstaendig;
    }

    /// <summary>
    /// Rechnet die PV-Vergütung nach EEG (PV-Konzept § 4, Etappe P4) — UI-frei,
    /// Katalog und Marktwerte als Delegaten (Muster <see cref="EegSatzRechner"/>).
    ///
    /// <para><b>Stufe 1</b> (immer möglich): Jahreseinspeisung × Sätze;
    /// § 51-Ausfall als Pauschale „Ausfallanteil der Einspeisearbeit [%]".
    /// <b>Stufe 2</b> (Stundenreihen vorhanden): Ausfallanteil aus der Spotreihe
    /// GEMESSEN (Einspeisearbeit in Negativpreisstunden), Spoterlös der
    /// Direktvermarktung zeitaufgelöst, 60-%-Kappung aus der Einspeisereihe
    /// (Stundenmittel als dokumentierte Näherung der Viertelstundenregel).</para>
    ///
    /// <para><b>Marktprämie mit JAHRESmarktwert (N2):</b>
    /// MP = max(0, AW_mix − JW_Jahr); der Spoterlös bleibt zeitaufgelöst (Stufe 2)
    /// bzw. wird in Stufe 1 durch den Jahresmarktwert ersetzt. Fehlende künftige
    /// Jahreswerte werden vom letzten bekannten Wert mit
    /// <c>MarktwertEntwicklung [%/a]</c> fortgeschrieben (Szenarioparameter);
    /// der AW bleibt szenariofest.</para>
    ///
    /// <para><b>Vergütungsdauer:</b> 20 Jahre zzgl. der Restmonate des
    /// Inbetriebnahmejahres (§ 25 Abs. 1). Danach fällt der Erlös auf den reinen
    /// Marktwert (Direktvermarktung/PPA) bzw. 0 (feste EV, keine).</para>
    /// </summary>
    public static class PvErloesRechner
    {
        /// <summary>Stichtag des Solarspitzengesetzes (§ 51 n. F.).</summary>
        public static readonly DateTime Par51Stichtag = new DateTime(2025, 2, 25);

        /// <summary>
        /// V4-Umschluss (Entscheidung F7): der führende Vergütungssatz [ct/kWh] für
        /// die SPEICHERBEWERTUNG (<c>v_pv</c> der StromPreisCtrl-Welt) — Stufe-1-Satz
        /// des ersten Jahres, mengenunabhängig. null = Dialog inaktiv/fehlt: dann
        /// bleibt die bisherige Quelle (<c>Verguetung_PV</c> des Aufschlagsblocks).
        /// </summary>
        public static double? VpvCtKwh(ProjektPhotovoltaikModel pv, double kwpRechnerisch,
                                       Func<string, int, double?> katalog,
                                       Func<int, double?> jahresmarktwert)
        {
            if (pv == null || !pv.Aktiv || katalog == null) return null;

            if (string.Equals(pv.Vermarktungsform, DbWerte.PV_VERMARKTUNG_KEINE, StringComparison.Ordinal))
                return 0;

            double kwp = pv.KwpOverride ?? kwpRechnerisch;
            EegSatzErgebnis satz = EegSatzRechner.AnzulegenderWert(
                Math.Max(0.001, kwp), pv.Inbetriebnahme, pv.Einspeiseart, katalog,
                CultureInfo.InvariantCulture);
            double aw = pv.AwOverride ?? satz.AwMixCt;

            if (string.Equals(pv.Vermarktungsform, DbWerte.PV_VERMARKTUNG_SONSTIGE_DV, StringComparison.Ordinal))
            {
                if (pv.PpaPreis.HasValue) return pv.PpaPreis.Value;
                double? jwPpa = jahresmarktwert != null ? jahresmarktwert(pv.Inbetriebnahme.Year) : null;
                return Math.Max(0, (jwPpa ?? 0) + (pv.PpaSpotAufschlag ?? 0));
            }

            if (string.Equals(pv.Vermarktungsform, DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE, StringComparison.Ordinal))
            {
                double? jw = jahresmarktwert != null ? jahresmarktwert(pv.Inbetriebnahme.Year) : null;
                double dv = pv.DvEntgelt ?? 0.40;
                if (!jw.HasValue) return Math.Max(0, aw - dv);   // AW als Untergrenze der DV
                return Math.Max(0, jw.Value + Math.Max(0, aw - jw.Value) - dv);
            }

            double abschlag = katalog(DbWerte.GESETZ_EEG_EV_ABSCHLAG, pv.Inbetriebnahme.Year) ?? 0.4;
            return Math.Max(0, aw - abschlag);
        }

        /// <summary>
        /// Die Erlösreihe eines Projekts.
        /// </summary>
        /// <param name="pv">Vergütungsangaben (Aktiv wird hier NICHT geprüft — das
        /// entscheidet der Aufrufer; Abnahmekriterium: inaktiv = Bestand)</param>
        /// <param name="kwpRechnerisch">kWp aus den Anlagen (V3); Override geht vor</param>
        /// <param name="einspeisungMWh">vergütungsfähige Jahreseinspeisung nach V2 [MWh]</param>
        /// <param name="einspeisungStundenKwh">Stundenreihe der Einspeisung [kWh], 8760;
        /// null = Stufe 1</param>
        /// <param name="spotCtKwh">Spotpreisreihe [ct/kWh], 8760; null = Stufe 1</param>
        /// <param name="betrachtungsJahre">T der Kapitalwertrechnung</param>
        /// <param name="katalog">Gesetzeskatalog (Schlüssel, Jahr) → Wert</param>
        /// <param name="jahresmarktwert">Kalenderjahr → amtlicher JW Solar [ct/kWh]
        /// (Projekt-Override eingerechnet); null = unbekannt</param>
        /// <param name="kultur">Zahlenformat der Herleitung</param>
        public static PvErloesErgebnis Rechne(ProjektPhotovoltaikModel pv,
                                              double kwpRechnerisch,
                                              double einspeisungMWh,
                                              double[] einspeisungStundenKwh,
                                              double[] spotCtKwh,
                                              int betrachtungsJahre,
                                              Func<string, int, double?> katalog,
                                              Func<int, double?> jahresmarktwert,
                                              CultureInfo kultur)
        {
            var e = new PvErloesErgebnis();
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            if (pv == null || katalog == null || betrachtungsJahre < 1)
            {
                e.Unvollstaendig = true;
                e.Herleitung = "Keine Vergütungsangaben — keine PV-Erlösreihe.";
                return e;
            }

            int T = betrachtungsJahre;
            e.JeJahr = new double[T + 1];
            e.Vermarktungsform = pv.Vermarktungsform ?? DbWerte.PV_VERMARKTUNG_EV;
            e.Kwp = pv.KwpOverride ?? kwpRechnerisch;
            e.EinspeisungMWh = einspeisungMWh;

            var sb = new StringBuilder();
            int ibnJahr = pv.Inbetriebnahme.Year;

            // --- Anzulegender Wert / feste EV ------------------------------------
            EegSatzErgebnis satz = EegSatzRechner.AnzulegenderWert(
                Math.Max(0.001, e.Kwp), pv.Inbetriebnahme, pv.Einspeiseart, katalog, kultur);
            e.AwMixCt = pv.AwOverride ?? satz.AwMixCt;
            if (pv.AwOverride.HasValue)
                sb.AppendFormat(kultur, "AW-Override {0:0.00} ct/kWh. ", e.AwMixCt);
            else
            {
                sb.Append(satz.Herleitung).Append(" ");
                if (satz.Unvollstaendig) e.Unvollstaendig = true;
            }
            double evAbschlag = katalog(DbWerte.GESETZ_EEG_EV_ABSCHLAG, ibnJahr) ?? 0.4;
            double evCt = Math.Round(Math.Max(0, e.AwMixCt - evAbschlag), 2, MidpointRounding.AwayFromZero);

            // --- Vergütungsdauer: 20 Jahre + Restmonate des IBN-Jahres -----------
            double dauerJahre = katalog(DbWerte.GESETZ_EEG_VERGUETUNGSDAUER, ibnJahr) ?? 20.0;
            int restMonate = pv.Inbetriebnahme == DateTime.MinValue
                ? 0 : 13 - pv.Inbetriebnahme.Month;
            int verguetungsMonate = (int)Math.Round(dauerJahre * 12) + restMonate;
            e.LetztesVerguetungsjahr = Math.Min(T, (verguetungsMonate + 11) / 12);

            // --- § 51: Anwendungsregel (4.4) -------------------------------------
            double grenze51 = katalog(DbWerte.GESETZ_EEG_51_GRENZE_KW, ibnJahr) ?? 100.0;
            bool par51Basis = pv.Inbetriebnahme >= Par51Stichtag;
            // AUTO je Betrachtungsjahr: unter der Grenze erst ab dem Jahr NACH dem
            // iMSys-Einbau; JA/NEIN übersteuern.
            Func<int, bool> par51ImJahr = kalenderjahr =>
            {
                if (string.Equals(pv.Par51_Anwenden, DbWerte.PV_SCHALTER_JA, StringComparison.Ordinal)) return true;
                if (string.Equals(pv.Par51_Anwenden, DbWerte.PV_SCHALTER_NEIN, StringComparison.Ordinal)) return false;
                if (!par51Basis) return false;
                if (e.Kwp >= grenze51) return true;
                return pv.IMSys_Einbaujahr.HasValue && kalenderjahr > pv.IMSys_Einbaujahr.Value;
            };

            // --- Ausfallanteil: gemessen (Stufe 2) oder Pauschale ----------------
            double anteil = (pv.AusfallanteilProzent ?? 20.0) / 100.0;
            if (einspeisungStundenKwh != null && spotCtKwh != null &&
                einspeisungStundenKwh.Length >= 8760 && spotCtKwh.Length >= 8760)
            {
                double einspGesamt = 0, einspNegativ = 0;
                for (int h = 0; h < 8760; h++)
                {
                    einspGesamt += einspeisungStundenKwh[h];
                    if (spotCtKwh[h] < 0) einspNegativ += einspeisungStundenKwh[h];
                }
                if (einspGesamt > 0)
                {
                    anteil = einspNegativ / einspGesamt;
                    e.AusfallGemessen = true;
                }
            }
            e.AusfallanteilProzent = Math.Round(anteil * 100.0, 2);

            // --- 60-%-Kappung (AUTO: feste EV ohne iMSys; nur Stufe 2 messbar) ---
            bool kappung =
                string.Equals(pv.Kappung60_Anwenden, DbWerte.PV_SCHALTER_JA, StringComparison.Ordinal) ||
                (string.Equals(pv.Kappung60_Anwenden, DbWerte.PV_SCHALTER_AUTO, StringComparison.Ordinal) &&
                 string.Equals(e.Vermarktungsform, DbWerte.PV_VERMARKTUNG_EV, StringComparison.Ordinal) &&
                 !pv.IMSys_Einbaujahr.HasValue);
            double kappGrenze = (katalog(DbWerte.GESETZ_EEG_KAPPUNG_PROZENT, ibnJahr) ?? 60.0) / 100.0;
            if (kappung && einspeisungStundenKwh != null && einspeisungStundenKwh.Length >= 8760 && e.Kwp > 0)
            {
                double kapp = 0, deckel = kappGrenze * e.Kwp;   // kWh je Stunde = kW-Mittel
                for (int h = 0; h < 8760; h++)
                    kapp += Math.Max(0, einspeisungStundenKwh[h] - deckel);
                e.KappungsverlustKwh = kapp;
                e.KappungAngewendet = kapp > 0;
            }

            // Vergütungsfähige Basisjahres-Arbeit [kWh] nach Kappung.
            double basisKwh = Math.Max(0, einspeisungMWh * 1000.0 - e.KappungsverlustKwh);

            // --- Spoterlös des Basisjahres (Stufe 2, Direktvermarktung/PPA) ------
            double? spotErloesEurBasis = null;
            if (einspeisungStundenKwh != null && spotCtKwh != null &&
                einspeisungStundenKwh.Length >= 8760 && spotCtKwh.Length >= 8760)
            {
                double summe = 0;
                for (int h = 0; h < 8760; h++)
                    if (spotCtKwh[h] >= 0)   // Negativstunden: Abregelung (4.4 Stufe 2)
                        summe += einspeisungStundenKwh[h] * spotCtKwh[h];
                spotErloesEurBasis = summe / 100.0;
            }

            // --- Jahresmarktwert-Fortschreibung (N2) -----------------------------
            double? letzterJw = null; int letzterJwJahr = 0;
            for (int j = ibnJahr; j >= ibnJahr - 6 && letzterJw == null; j--)
            {
                double? jw = jahresmarktwert(j);
                if (jw.HasValue) { letzterJw = jw; letzterJwJahr = j; }
            }
            Func<int, double?> jwImJahr = kalenderjahr =>
            {
                double? amtlich = jahresmarktwert(kalenderjahr);
                if (amtlich.HasValue) return amtlich;
                if (!letzterJw.HasValue) return null;
                int n = Math.Max(0, kalenderjahr - letzterJwJahr);
                return letzterJw.Value * Math.Pow(1.0 + pv.MarktwertEntwicklung / 100.0, n);
            };

            double dv = pv.DvEntgelt ?? 0.40;
            double ausfallKwhBasis = 0, ausfallEurBasis = 0;

            // --- Jahresschleife ---------------------------------------------------
            for (int t = 1; t <= T; t++)
            {
                int kalenderjahr = ibnJahr + t - 1;
                bool inVerguetung = t <= e.LetztesVerguetungsjahr;
                bool p51 = par51ImJahr(kalenderjahr);
                double a = p51 ? anteil : 0;

                double erloes = 0, ausfallKwh = 0, ausfallEur = 0, praemie = 0;

                if (string.Equals(e.Vermarktungsform, DbWerte.PV_VERMARKTUNG_KEINE, StringComparison.Ordinal))
                {
                    erloes = 0;
                }
                else if (string.Equals(e.Vermarktungsform, DbWerte.PV_VERMARKTUNG_EV, StringComparison.Ordinal))
                {
                    if (inVerguetung)
                    {
                        ausfallKwh = basisKwh * a;
                        erloes = (basisKwh - ausfallKwh) * evCt / 100.0;
                        ausfallEur = ausfallKwh * evCt / 100.0;
                    }
                    // nach Ablauf: keine Vergütung (Fall a → 0).
                }
                else   // Marktprämie oder sonstige DV
                {
                    bool ppa = string.Equals(e.Vermarktungsform, DbWerte.PV_VERMARKTUNG_SONSTIGE_DV,
                                             StringComparison.Ordinal);
                    double? jw = jwImJahr(kalenderjahr);

                    ausfallKwh = basisKwh * a;
                    double arbeitKwh = basisKwh - ausfallKwh;

                    // Spoterlös: zeitaufgelöst (Basisjahr, § 51 über die Reihe schon
                    // heraus) oder Stufe-1-Ersatz über den Jahresmarktwert.
                    double spotEur;
                    if (spotErloesEurBasis.HasValue)
                        spotEur = spotErloesEurBasis.Value *
                                  (jw.HasValue && letzterJw.HasValue && letzterJw.Value > 0
                                      ? (jwImJahr(kalenderjahr) ?? letzterJw.Value) / letzterJw.Value
                                      : 1.0);
                    else
                        spotEur = arbeitKwh * (jw ?? 0) / 100.0;

                    if (ppa)
                    {
                        if (pv.PpaPreis.HasValue)
                            erloes = arbeitKwh * pv.PpaPreis.Value / 100.0;
                        else if (pv.PpaSpotAufschlag.HasValue)
                            erloes = spotEur + arbeitKwh * pv.PpaSpotAufschlag.Value / 100.0;
                        else { erloes = spotEur; e.Unvollstaendig = true; }
                    }
                    else
                    {
                        if (!jw.HasValue) e.Unvollstaendig = true;
                        double mpCt = inVerguetung ? Math.Max(0, e.AwMixCt - (jw ?? e.AwMixCt)) : 0;
                        praemie = arbeitKwh * mpCt / 100.0;
                        erloes = spotEur + praemie - arbeitKwh * dv / 100.0;
                        ausfallEur = ausfallKwh * (e.AwMixCt) / 100.0;   // entgangener AW (Abregelung)
                    }
                }

                e.JeJahr[t] = erloes;
                if (t == 1)
                {
                    e.MarktpraemieEurJahr1 = praemie;
                    ausfallKwhBasis = ausfallKwh;
                    ausfallEurBasis = ausfallEur;
                    e.Par51Angewendet = p51;
                    e.SatzJahr1Ct = basisKwh > 0 ? erloes * 100.0 / basisKwh : 0;
                }
            }

            e.VerguetungsausfallKwh = ausfallKwhBasis;
            e.VerguetungsausfallEur = ausfallEurBasis;

            // --- § 51a-Kompensation (4.4): Gutschrift im letzten Vergütungsjahr --
            if (pv.Par51a_Kompensieren && ausfallKwhBasis > 0 && e.LetztesVerguetungsjahr >= 1)
            {
                double faktor = katalog(DbWerte.GESETZ_EEG_51A_FAKTOR_SOLAR, ibnJahr) ?? 0.5;
                e.Kompensation51aEur = ausfallKwhBasis * faktor * e.AwMixCt / 100.0;
                e.JeJahr[e.LetztesVerguetungsjahr] += e.Kompensation51aEur;
            }

            sb.AppendFormat(kultur,
                "Vergütung {0} Monate (bis Jahr {1}); § 51 {2} (Anteil {3:0.##} %{4}); " +
                "Einspeisung {5:N1} MWh/a; Satz Jahr 1: {6:0.00} ct/kWh.",
                verguetungsMonate, e.LetztesVerguetungsjahr,
                e.Par51Angewendet ? "angewendet" : "nicht angewendet",
                e.AusfallanteilProzent, e.AusfallGemessen ? ", gemessen" : ", Pauschale",
                einspeisungMWh, e.SatzJahr1Ct);
            e.Herleitung = sb.ToString();
            return e;
        }
    }
}
