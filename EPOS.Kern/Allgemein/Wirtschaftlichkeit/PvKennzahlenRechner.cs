using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kennzahlen der PV-Vorschau (Etappe P6, PV-Konzept N.3 Nr. 3).
    /// </summary>
    public sealed class PvKennzahlen
    {
        /// <summary>Stromgestehungskosten UNDISKONTIERT (KZS = 0) [ct/kWh] — nur
        /// dieser Wert ist mit einem Vergütungssatz vergleichbar (N.3).</summary>
        public double? Lcoe0Ct;

        /// <summary>Stromgestehungskosten diskontiert [ct/kWh] — Ausgaben UND
        /// Menge abgezinst (pv@now-Definition).</summary>
        public double? LcoeDiskCt;

        /// <summary>Eigenverbrauchsquote [%] — STETS als Paar mit
        /// <see cref="AutarkieProzent"/> anzeigen (N.3: einzeln entsteht ein
        /// falscher Eindruck, Fallbeispiel 83 % zu 26 %).</summary>
        public double? EvQuoteProzent;

        /// <summary>Autarkiegrad [%].</summary>
        public double? AutarkieProzent;

        /// <summary>true = Quoten aus der Speicherrechnung (mit Speicher);
        /// false = aus dem PV-Aggregat allein.</summary>
        public bool QuotenMitSpeicher;

        /// <summary>„Vorteil durch PV je Jahr" [€/a] — pv@now-Definition:
        /// undiskontierter Liquiditätsüberschuss über die Laufzeit
        /// (Ersparnis + Vergütung − Betrieb − Investition) geteilt durch die
        /// Jahre.</summary>
        public double? VorteilJeJahrEur;

        /// <summary>Vermiedener Netzbezug im Jahr 1 [€/a] (EV-Menge × Arbeitspreis).</summary>
        public double? VermiedenerBezugJahr1Eur;

        /// <summary>Fehlende Grundlagen im Klartext; leer = alles da.</summary>
        public string Hinweis = "";
    }

    /// <summary>
    /// ETAPPE P6 (PV-Konzept N.3 Nr. 3): die Kennzahlzeile der Dialog-Vorschau —
    /// UI-frei und gegen Handrechnungen testbar. KEINE Zweitrechnung des
    /// Erlöswegs: Die Vergütungsreihe kommt fertig aus dem
    /// <see cref="PvErloesRechner"/>, Kosten aus der Kostenwelt, Mengen aus dem
    /// Simulationsergebnis, Parameter aus der Wirtschaftlichkeit.
    ///
    /// <para><b>Mengenkonvention:</b> Die Erzeugung geht KONSTANT über die
    /// Laufzeit ein — EPOS-Plan führt keine Moduldegradation (dokumentierte
    /// Abweichung zur pv@now-Minderung; Sensitivität lt. Modellabgleich
    /// ~5–7 % auf den LCOE).</para>
    /// </summary>
    public static class PvKennzahlenRechner
    {
        /// <param name="erzeugungMWh">PV-Stromproduktion [MWh/a] (Simulationsergebnis)</param>
        /// <param name="ueberschussMWh">PV-Überschuss = Einspeisung [MWh/a] (nach V2)</param>
        /// <param name="bedarfMWh">Strombedarf des Projekts [MWh/a]; 0 = unbekannt</param>
        /// <param name="evQuoteSpeicherProzent">EV-Quote MIT Speicher [%] aus der
        /// Speicherrechnung; null = kein Speicherlauf</param>
        /// <param name="autarkieSpeicherProzent">Autarkiegrad MIT Speicher [%]; null = keiner</param>
        /// <param name="investEur">PV-Investition [€] aus der Kostenwelt; null/0 = nicht gepflegt</param>
        /// <param name="betriebEurJahr">PV-Betriebskosten [€/a]; null = nicht gepflegt (0 ist gepflegt)</param>
        /// <param name="zinsProzent">Kalkulationszins [%] der Wirtschaftlichkeit</param>
        /// <param name="jahre">Betrachtungszeitraum T [a]</param>
        /// <param name="preissteigerungEnergieProzent">Preissteigerung Energie [%/a]</param>
        /// <param name="strompreisEurKwh">Arbeitspreis Strom [€/kWh]; null = nicht gepflegt</param>
        /// <param name="verguetungJeJahr">Vergütung [€] je Jahr (Index 1..T) aus dem
        /// PvErloesRechner; null = Dialog inaktiv/keine Reihe</param>
        public static PvKennzahlen Rechne(double erzeugungMWh, double ueberschussMWh,
                                          double bedarfMWh,
                                          double? evQuoteSpeicherProzent,
                                          double? autarkieSpeicherProzent,
                                          double? investEur, double? betriebEurJahr,
                                          double zinsProzent, int jahre,
                                          double preissteigerungEnergieProzent,
                                          double? strompreisEurKwh,
                                          double[] verguetungJeJahr,
                                          CultureInfo kultur)
        {
            var k = new PvKennzahlen();
            if (jahre < 1) jahre = 1;
            double q = 1.0 + zinsProzent / 100.0;
            double pE = 1.0 + preissteigerungEnergieProzent / 100.0;
            double evMWh = Math.Max(0, erzeugungMWh - ueberschussMWh);

            // --- EV-Quote und Autarkie: STETS als Paar (N.3) ------------------
            if (evQuoteSpeicherProzent.HasValue && autarkieSpeicherProzent.HasValue)
            {
                k.EvQuoteProzent = evQuoteSpeicherProzent;
                k.AutarkieProzent = autarkieSpeicherProzent;
                k.QuotenMitSpeicher = true;
            }
            else if (erzeugungMWh > 0)
            {
                k.EvQuoteProzent = evMWh / erzeugungMWh * 100.0;
                if (bedarfMWh > 0) k.AutarkieProzent = evMWh / bedarfMWh * 100.0;
            }

            // --- LCOE (pv@now-Definition; Menge konstant, s. Klassendoku) -----
            bool kosten = investEur.HasValue && investEur.Value > 0 && betriebEurJahr.HasValue;
            if (kosten && erzeugungMWh > 0)
            {
                double ausgaben0 = investEur.Value, ausgabenD = investEur.Value;
                double menge0 = 0, mengeD = 0;
                for (int t = 1; t <= jahre; t++)
                {
                    double ab = Math.Pow(q, -t);
                    ausgaben0 += betriebEurJahr.Value;
                    ausgabenD += betriebEurJahr.Value * ab;
                    menge0 += erzeugungMWh * 1000.0;
                    mengeD += erzeugungMWh * 1000.0 * ab;
                }
                k.Lcoe0Ct = ausgaben0 / menge0 * 100.0;
                k.LcoeDiskCt = ausgabenD / mengeD * 100.0;
            }
            else if (!kosten)
                k.Hinweis = Anh(k.Hinweis, T("PVW_KZ_OHNE_KOSTEN",
                    "PV-Kosten (Investition/Betrieb) nicht gepflegt — LCOE und Vorteil entfallen."));

            // --- Vermiedener Bezug Jahr 1 -------------------------------------
            if (strompreisEurKwh.HasValue && evMWh > 0)
                k.VermiedenerBezugJahr1Eur = evMWh * 1000.0 * strompreisEurKwh.Value;
            else if (!strompreisEurKwh.HasValue && evMWh > 0)
                k.Hinweis = Anh(k.Hinweis, T("PVW_KZ_OHNE_PREIS",
                    "Strom-Arbeitspreis nicht gepflegt — Ersparnisanteil entfällt."));

            // --- Vorteil je Jahr (pv@now: undiskontiert über die Laufzeit) ----
            if (kosten && strompreisEurKwh.HasValue)
            {
                double summe = -investEur.Value;
                for (int t = 1; t <= jahre; t++)
                {
                    double verguetung = verguetungJeJahr != null && t < verguetungJeJahr.Length
                                        ? verguetungJeJahr[t] : 0;
                    double ersparnis = evMWh * 1000.0 * strompreisEurKwh.Value
                                       * Math.Pow(pE, t - 1);
                    summe += ersparnis + verguetung - betriebEurJahr.Value;
                }
                k.VorteilJeJahrEur = summe / jahre;
            }
            return k;
        }

        /// <summary>Die fertige Anzeigezeile der Vorschau (zwei Zeilen Text).</summary>
        public static string Anzeige(PvKennzahlen k, CultureInfo kultur)
        {
            if (k == null) return "—";
            string lcoe = k.Lcoe0Ct.HasValue
                ? string.Format(kultur, T("PVW_KZ_LCOE",
                      "Stromgestehungskosten: {0:N2} ct/kWh (LCOE₀, mit Satz vergleichbar) · {1:N2} ct/kWh (diskontiert)"),
                      k.Lcoe0Ct, k.LcoeDiskCt)
                : "";
            string quoten = k.EvQuoteProzent.HasValue
                ? string.Format(kultur, T("PVW_KZ_QUOTEN",
                      "Eigenverbrauchsquote {0:N1} % · Autarkiegrad {1} %{2}"),
                      k.EvQuoteProzent,
                      k.AutarkieProzent.HasValue ? k.AutarkieProzent.Value.ToString("N1", kultur) : "—",
                      k.QuotenMitSpeicher ? T("PVW_KZ_MIT_SPEICHER", " (mit Speicher)") : "")
                : "";
            string vorteil = k.VorteilJeJahrEur.HasValue
                ? string.Format(kultur, T("PVW_KZ_VORTEIL", "Vorteil durch PV: {0:N0} €/a"),
                      k.VorteilJeJahrEur)
                : "";
            string z1 = Verbinde(quoten, vorteil, " · ");
            string z2 = Verbinde(lcoe, k.Hinweis, "  —  ");
            return Verbinde(z1, z2, Environment.NewLine);
        }

        private static string Verbinde(string a, string b, string trenner)
        {
            if (string.IsNullOrEmpty(a)) return b ?? "";
            if (string.IsNullOrEmpty(b)) return a;
            return a + trenner + b;
        }

        private static string Anh(string bisher, string neu)
        {
            return string.IsNullOrEmpty(bisher) ? neu : bisher + " " + neu;
        }

        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
