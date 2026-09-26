using System;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennzahlen EINES Gebäudes aus seiner Heizreihe</b> (Entscheid E30, Konzept
    /// Gebäudesimulation N1.35) — die eine Stelle, an der Wärmebedarf und die drei
    /// Spitzenwerte je Gebäude gebildet werden. Zwei Aufrufer, eine Rechnung: der Lauf
    /// (<c>SimulationWaermebedarf.Waermebedarf_berechnen</c>, er legt das Ergebnis in
    /// <c>Tab_ErgebnisGebaeude</c>) und die Auskunft des Gebäudedialogs
    /// (<see cref="GebaeudeBedarfCtrl"/>). So steht im Bericht dieselbe Zahl wie im Dialog.
    ///
    /// <para><b>Die Reihe liegt in kW</b> — nach derselben Umrechnung wie der Heizkanal des
    /// Laufs (<c>BhkwPlan.WattToKw</c>). Die Größen, die es nur auf dem VDI-Weg gibt, kommen
    /// aus dem <see cref="GebaeudeModellErgebnis"/> des Merkplatzes (skaliert nach E8); fehlt
    /// es, bleiben sie <c>null</c>.</para>
    ///
    /// <para>Ohne Datenbank, ohne Zustand. Der Rechenweg des Laufs bleibt unberührt: gelesen
    /// wird eine Kopie der Reihe.</para>
    /// </summary>
    internal static class GebaeudeKennzahlen
    {
        /// <summary>
        /// Die Ergebniszeile eines Gebäudes.
        /// </summary>
        /// <param name="merkplatz">Der Merkplatz im Lauf (ab 0).</param>
        /// <param name="idGebaeude"><c>Tab_Gebaeude.ID</c> der Projektkopie.</param>
        /// <param name="gebaeudename">Der Name der Projektkopie.</param>
        /// <param name="rechenweg">Der wirksame Rechenweg (<c>DbWerte.GEBAEUDE_MODELL_*</c>).</param>
        /// <param name="reiheKw">Die 8 760 Stundenwerte der Heizwärme in kW.</param>
        /// <param name="vdi">Das Ergebnis des VDI-Wegs für diesen Merkplatz; <c>null</c> auf dem Tagesbilanz-Weg.</param>
        internal static ErgebnisGebaeudeModel Bilden(int merkplatz, int idGebaeude, string gebaeudename,
                                                     string rechenweg, double[] reiheKw,
                                                     GebaeudeModellErgebnis vdi)
        {
            if (reiheKw == null) throw new ArgumentNullException(nameof(reiheKw));

            var e = new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = idGebaeude,
                Merkplatz = merkplatz,
                Gebaeudename = gebaeudename ?? "",
                Rechenweg = rechenweg ?? "",
                // ZEICHENGLEICH zum Lauf: dort steht "kanalHeizung.Sum() / 1000" - eine
                // double-Summe durch eine GANZE Zahl (GebaeudeBedarfCtrl, W9-E-2).
                HeizwaermeMwh = reiheKw.Sum() / 1000,
                SpitzeKw = Hoechstwert(reiheKw),
                SpitzeTagesmittelKw = GroesstesTagesmittel(reiheKw),
                Spitze95Kw = Quantil95(reiheKw),
            };

            if (vdi != null)
            {
                // Stufe G6b (W5, A6): je Zone eine Zeile für Tab_ErgebnisZone, nur ab zwei Zonen.
                if (vdi.Zonen != null)
                    foreach (GebaeudeZonenergebnis z in vdi.Zonen)
                        e.Zonen.Add(new ErgebnisZoneModel
                        {
                            ID_Zone = z.ZonenId > 0 ? z.ZonenId : (int?)null,
                            Rang = Math.Max(1, z.Rang),
                            Bezeichner = z.Bezeichnung ?? "",
                            IstBeheizt = z.IstBeheizt,
                            HeizwaermeMwh = z.IstBeheizt ? z.Ergebnis.JahresheizwaermeMwh : (double?)null,
                            SpitzeKw = z.IstBeheizt ? z.Ergebnis.SpitzeKw : (double?)null,
                            KuehlenergieMwh = z.Ergebnis.KuehlenergieMwh,
                            MittlereRaumtemperaturC = z.Ergebnis.MittlereRaumtemperaturHeizzeit,
                            UeberhitzungsstundenH = z.Ergebnis.Ueberhitzungsstunden,
                            DeltaThetaMaxK = double.IsNaN(z.DeltaThetaMaxK) ? (double?)null : z.DeltaThetaMaxK,
                            DurchlaeufeMax = z.DurchlaeufeMax,
                            MusterwechselH = z.MusterwechselH,
                        });

                e.KuehlenergieMwh = vdi.KuehlenergieMwh;
                e.KuehlstundenH = vdi.StundenMitKuehlbedarf;
                e.MittlereRaumtemperaturC = vdi.MittlereRaumtemperaturHeizzeit;
                e.UeberhitzungsstundenH = vdi.Ueberhitzungsstunden;
                e.SommerlueftungsstundenH = vdi.StundenMitSommerlueftung;
                e.ObereRaumtemperaturC = vdi.ThetaMax;

                // Anlagenkopplung (AK1): die Kennzahlen des Heizkreises je Gebäude.
                HeizkreisErgebnis hk = vdi.Heizkreis;
                if (hk != null)
                {
                    e.UebergabeArt = hk.UebergabeArt;
                    e.VorlaufMittelC = double.IsNaN(hk.VorlaufMittelC) ? (double?)null : hk.VorlaufMittelC;
                    e.RuecklaufMittelC = double.IsNaN(hk.RuecklaufMittelC) ? (double?)null : hk.RuecklaufMittelC;
                    e.UebergabeBegrenztStundenH = hk.UebergabeBegrenztStundenH;
                }

                // Kälteseite (E37): die Kennzahlen des Kältekreises je Gebäude.
                KuehlkreisErgebnis kk = vdi.Kuehlkreis;
                if (kk != null)
                {
                    e.KuehlUebergabeArt = kk.UebergabeArt;
                    e.KuehlVorlaufMittelC = double.IsNaN(kk.VorlaufMittelC) ? (double?)null : kk.VorlaufMittelC;
                    e.KuehlRuecklaufMittelC = double.IsNaN(kk.RuecklaufMittelC) ? (double?)null : kk.RuecklaufMittelC;
                    e.KuehlUebergabeBegrenztStundenH = kk.UebergabeBegrenztStundenH;
                    e.KuehlVorlaufgrenzeStundenH = kk.VorlaufgrenzeStundenH;
                }
            }
            return e;
        }

        /// <summary>
        /// Das größte gleitende Mittel über 24 Stunden [kW] — dieselbe Bildung wie
        /// <c>GebaeudeModellErgebnis.SpitzeTagesmittelKw</c>, hier auf der Reihe beider Wege.
        /// </summary>
        internal static double GroesstesTagesmittel(double[] werte)
        {
            double fenster = 0.0;
            for (int h = 0; h < 24 && h < werte.Length; h++) fenster += werte[h];
            double bestes = fenster;
            for (int h = 24; h < werte.Length; h++)
            {
                fenster += werte[h] - werte[h - 24];
                if (fenster > bestes) bestes = fenster;
            }
            return bestes / 24.0;
        }

        /// <summary>Das 95-%-Quantil nach nächstgelegenem Rang (1-basiert) [kW].</summary>
        internal static double Quantil95(double[] werte)
        {
            double[] sortiert = (double[])werte.Clone();
            Array.Sort(sortiert);
            int rang = (int)Math.Ceiling(0.95 * sortiert.Length);
            return sortiert[Math.Max(rang, 1) - 1];
        }

        /// <summary>Der Höchstwert der Stundenreihe — wie <c>Maximaler_Waermebedarf</c>.</summary>
        internal static double Hoechstwert(double[] werte)
        {
            double max = 0;
            for (int i = 0; i < werte.Length; i++) if (max < werte[i]) max = werte[i];
            return max;
        }
    }
}
