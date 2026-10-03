using System;
using System.Collections.Generic;
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
    /// <para><b>Aufheizoptimierung</b> (Entwurf KP3, Welle D2; Abschnitt 4, Grundsatz 4): Die Aufheizwerte des
    /// Laufs (<see cref="GebaeudeModellErgebnis.Aufheizung"/>, je Gebäude und je Zone) gehen nach den NULL-Regeln
    /// der Festlegung 25 in die Zeile (<see cref="Aufheizwerte"/>) — NULL heißt „Schalter aus" oder
    /// Tagesbilanz-Weg. Die <b>Sommerlüftungsstunden</b> sind NULL ohne Sommerlüftung (Festlegung 26, B17), wie
    /// die Nachtauskühlstunden; das Modell behält seine Zahl und trägt das Kennzeichen
    /// <see cref="GebaeudeModellErgebnis.SommerlueftungGesetzt"/>.</para>
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
                            NachtauskuehlstundenH = z.Ergebnis.StundenMitNachtauskuehlung,
                            // Stufe KP3 (Festlegung 26, E54 je Zone): NULL ohne Sommerlueftung.
                            SommerlueftungsstundenH = Sommerlueftungsstunden(z.Ergebnis),
                        }.MitAufheizwerten(Aufheizwerte(z.Ergebnis.Aufheizung, zone: true)));

                e.KuehlenergieMwh = vdi.KuehlenergieMwh;
                e.KuehlstundenH = vdi.StundenMitKuehlbedarf;
                e.MittlereRaumtemperaturC = vdi.MittlereRaumtemperaturHeizzeit;
                e.UeberhitzungsstundenH = vdi.Ueberhitzungsstunden;
                // Stufe KP3 (Festlegung 26, B17): NULL ohne Sommerlueftung wie die Nachtauskuehlstunden -
                // das Modell behaelt seine Zahl, die Zeile unterscheidet „nicht gesetzt" von „0 h".
                e.SommerlueftungsstundenH = Sommerlueftungsstunden(vdi);
                // Stufe KP1b (Konzept 3.7): NULL heisst "keine Nachtauskuehlung gesetzt" (E30).
                e.NachtauskuehlstundenH = vdi.StundenMitNachtauskuehlung;
                e.ObereRaumtemperaturC = vdi.ThetaMax;

                // Stufe KP3 (Entwurf Abschnitt 4, Festlegung 25): die Aufheizwerte - NULL heisst Schalter aus.
                e.MitAufheizwerten(Aufheizwerte(vdi.Aufheizung, zone: false));

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
        /// Die Sommerlüftungsstunden der Ergebniszeile (Entwurf KP3, Festlegung 26, B17): die Zahl des Modells,
        /// wenn eine Sommerlüftung gesetzt ist, sonst <c>null</c> — wie die Nachtauskühlstunden (E30).
        /// </summary>
        internal static int? Sommerlueftungsstunden(GebaeudeModellErgebnis vdi)
            => vdi != null && vdi.SommerlueftungGesetzt ? vdi.StundenMitSommerlueftung : (int?)null;

        /// <summary>
        /// <b>Die vierzehn Aufheizwerte einer Ergebniszeile</b> (Entwurf KP3, Abschnitt 4, Grundsatz 4,
        /// Festlegung 25) aus den Aufheizwerten des Laufs — die EINE Stelle der NULL-Regeln, aus der
        /// Gebäudezeile, Zonenzeile und Ergebnisexport lesen. <c>null</c> heißt „Schalter aus" oder
        /// Tagesbilanz-Weg: dann bleibt jede Spalte NULL.
        /// <list type="bullet">
        /// <item><c>Aufheiz_Bemessung</c> nur am Gebäude; GEKOPPELT und UNBEHEIZT tragen nur Zustand und
        /// <c>HeizleistungMax_H</c> (so liefert sie schon der Lauf); UNERREICHBAR ohne t_auf,max.</item>
        /// <item><b>P_auf nur endlich und über null</b> (Spalte <c>CHECK (&gt; 0)</c>): Die Testnaht der
        /// Grenzfallprobe (N-AH8) setzt P_auf = +∞ — das ist keine Leistung, die Spalte bleibt NULL; ebenso
        /// eine Grenze <c>Heizleistung_Max</c> = 0.</item>
        /// <item><b>Eine gekoppelte Zone</b> (Mehrzonenweg mit AK1 als idealer Last) trägt den Zustand GEKOPPELT
        /// und <c>HeizleistungMax_H</c> wie das gekoppelte Gebäude — die Zustandsspalte von <c>Tab_ErgebnisZone</c>
        /// kennt GEKOPPELT seit dem Schritt KP-S4 (<see cref="AufheizManuellSchema"/>, Befund D2). Ein Gebäude hat
        /// keinen Zustand UNBEHEIZT und eine Zone keine Quelle GEMISCHT — beides entsteht im Lauf nicht.</item>
        /// <item><b>E59/E60</b> (Festlegungen 39, 41): <c>Aufheiz_Art</c> an Gebäude und Zone (TAEGLICH, FEST oder
        /// MANUELL; die Zone erbt die Art ihres Gebäudes), bei MANUELL <c>Aufheizzeit_Max_H</c> = der manuelle Wert;
        /// <c>Auslegungsheizlast_Kw</c> (&gt; 0) und <c>Aufheizzuschlag_Kw</c> (≥ 0) nur am Gebäude und nur endlich;
        /// die manuelle Aufheizzeit für den Ergebnisexport.</item>
        /// <item><c>HeizleistungMax_H</c> nur endlich.</item>
        /// </list>
        /// </summary>
        /// <param name="a">Die Aufheizwerte des Gebäudes bzw. der Zone (skaliert wie die Spitzen).</param>
        /// <param name="zone">Die Zeile einer Zone (<c>Tab_ErgebnisZone</c>)?</param>
        internal static Aufheizkennzahlen Aufheizwerte(Aufheizergebnis a, bool zone)
        {
            if (a == null || a.AufheizZustand == null) return null;
            string zustand = a.AufheizZustand;
            if (!zone && zustand == DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT) return null;
            string quelle = a.AufheizLeistungsquelle;
            if (zone && quelle == DbWerte.AUFHEIZ_QUELLE_GEMISCHT) quelle = null;
            return new Aufheizkennzahlen
            {
                AufheizZustand = zustand,
                AufheizBemessung = zone ? null : a.AufheizBemessung,
                AufheizArt = a.AufheizArt,
                AufheizzeitManuellH = zone ? null : a.AufheizzeitManuellH,
                AuslegungsheizlastKw = zone ? null : a.AuslegungsheizlastKw is double hl && hl > 0.0 && !double.IsInfinity(hl) ? hl : (double?)null,
                AufheizzuschlagKw = zone ? null : a.AufheizzuschlagKw is double rh && rh >= 0.0 && !double.IsInfinity(rh) ? rh : (double?)null,
                AufheizzeitMaxH = zustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR ? null : a.AufheizzeitMaxH,
                AufheizAussenC = Endlich(a.AufheizAussenC),
                AufheizLeistungKw = a.AufheizLeistungKw is double p && p > 0.0 && !double.IsInfinity(p) ? p : (double?)null,
                AufheizLeistungsquelle = quelle,
                Aufheiztage = a.Aufheiztage,
                AufheiztageBegrenzt = a.AufheiztageBegrenzt,
                AufheiztageUnerreichbar = a.AufheiztageUnerreichbar,
                AufheiztageNachweisband = a.AufheiztageNachweisband,
                AufheizstundenH = a.AufheizstundenH,
                AufheizzeitLaengsteH = a.AufheizzeitLaengsteH,
                AufheizspruengeAus = a.AufheizspruengeAus,
                HeizleistungMaxStundenH = Endlich(a.HeizleistungMaxStundenH),
            };
        }

        private static double? Endlich(double? x)
            => x is double w && !double.IsNaN(w) && !double.IsInfinity(w) ? w : (double?)null;

        /// <summary>Setzt die Aufheizwerte in die Gebäudezeile; <c>null</c> lässt sie NULL.</summary>
        private static ErgebnisGebaeudeModel MitAufheizwerten(this ErgebnisGebaeudeModel e, Aufheizkennzahlen a)
        {
            if (a == null) return e;
            e.AufheizZustand = a.AufheizZustand;
            e.AufheizBemessung = a.AufheizBemessung;
            e.AufheizArt = a.AufheizArt;
            e.AuslegungsheizlastKw = a.AuslegungsheizlastKw;
            e.AufheizzuschlagKw = a.AufheizzuschlagKw;
            e.AufheizzeitMaxH = a.AufheizzeitMaxH;
            e.AufheizAussenC = a.AufheizAussenC;
            e.AufheizLeistungKw = a.AufheizLeistungKw;
            e.AufheizLeistungsquelle = a.AufheizLeistungsquelle;
            e.Aufheiztage = a.Aufheiztage;
            e.AufheiztageBegrenzt = a.AufheiztageBegrenzt;
            e.AufheiztageUnerreichbar = a.AufheiztageUnerreichbar;
            e.AufheiztageNachweisband = a.AufheiztageNachweisband;
            e.AufheizstundenH = a.AufheizstundenH;
            e.AufheizzeitLaengsteH = a.AufheizzeitLaengsteH;
            e.AufheizspruengeAus = a.AufheizspruengeAus;
            e.HeizleistungMaxStundenH = a.HeizleistungMaxStundenH;
            return e;
        }

        /// <summary>Setzt die Aufheizwerte in die Zonenzeile (ohne Bemessung); <c>null</c> lässt sie NULL.</summary>
        private static ErgebnisZoneModel MitAufheizwerten(this ErgebnisZoneModel z, Aufheizkennzahlen a)
        {
            if (a == null) return z;
            z.AufheizZustand = a.AufheizZustand;
            z.AufheizArt = a.AufheizArt;
            z.AufheizzeitMaxH = a.AufheizzeitMaxH;
            z.AufheizAussenC = a.AufheizAussenC;
            z.AufheizLeistungKw = a.AufheizLeistungKw;
            z.AufheizLeistungsquelle = a.AufheizLeistungsquelle;
            z.Aufheiztage = a.Aufheiztage;
            z.AufheiztageBegrenzt = a.AufheiztageBegrenzt;
            z.AufheiztageUnerreichbar = a.AufheiztageUnerreichbar;
            z.AufheiztageNachweisband = a.AufheiztageNachweisband;
            z.AufheizstundenH = a.AufheizstundenH;
            z.AufheizzeitLaengsteH = a.AufheizzeitLaengsteH;
            z.AufheizspruengeAus = a.AufheizspruengeAus;
            z.HeizleistungMaxStundenH = a.HeizleistungMaxStundenH;
            return z;
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

    /// <summary>
    /// <b>Die Aufheizwerte einer Ergebniszeile</b> (Entwurf KP3, Abschnitt 4; Schritt 161) nach den NULL-Regeln
    /// der Festlegung 25 (<see cref="GebaeudeKennzahlen.Aufheizwerte"/>) — Feldnamen wie
    /// <see cref="ErgebnisGebaeudeModel"/>. Gebäudezeile, Zonenzeile und Ergebnisexport lesen nur sie.
    /// </summary>
    internal sealed record Aufheizkennzahlen
    {
        internal string AufheizZustand { get; init; }
        internal string AufheizBemessung { get; init; }
        internal string AufheizArt { get; init; }
        internal int? AufheizzeitManuellH { get; init; }
        internal double? AuslegungsheizlastKw { get; init; }
        internal double? AufheizzuschlagKw { get; init; }
        internal int? AufheizzeitMaxH { get; init; }
        internal double? AufheizAussenC { get; init; }
        internal double? AufheizLeistungKw { get; init; }
        internal string AufheizLeistungsquelle { get; init; }
        internal int? Aufheiztage { get; init; }
        internal int? AufheiztageBegrenzt { get; init; }
        internal int? AufheiztageUnerreichbar { get; init; }
        internal int? AufheiztageNachweisband { get; init; }
        internal int? AufheizstundenH { get; init; }
        internal int? AufheizzeitLaengsteH { get; init; }
        internal int? AufheizspruengeAus { get; init; }
        internal double? HeizleistungMaxStundenH { get; init; }

        /// <summary>
        /// Die Zahlen der Zeile in fester Reihenfolge, nur die gesetzten — Schlüssel = Feldname (Ergebnisexport,
        /// Festlegung 28). Die drei Texte (Zustand, Bemessung, Quelle) gehen getrennt.
        /// </summary>
        internal IEnumerable<KeyValuePair<string, double>> Zahlen()
        {
            if (AufheizzeitMaxH is int t) yield return Paar(nameof(AufheizzeitMaxH), t);
            if (AufheizAussenC is double ta) yield return Paar(nameof(AufheizAussenC), ta);
            if (AufheizLeistungKw is double p) yield return Paar(nameof(AufheizLeistungKw), p);
            if (Aufheiztage is int d) yield return Paar(nameof(Aufheiztage), d);
            if (AufheiztageBegrenzt is int w2) yield return Paar(nameof(AufheiztageBegrenzt), w2);
            if (AufheiztageUnerreichbar is int w1) yield return Paar(nameof(AufheiztageUnerreichbar), w1);
            if (AufheiztageNachweisband is int w3) yield return Paar(nameof(AufheiztageNachweisband), w3);
            if (AufheizstundenH is int sh) yield return Paar(nameof(AufheizstundenH), sh);
            if (AufheizzeitLaengsteH is int l) yield return Paar(nameof(AufheizzeitLaengsteH), l);
            if (AufheizspruengeAus is int w4) yield return Paar(nameof(AufheizspruengeAus), w4);
            if (HeizleistungMaxStundenH is double k) yield return Paar(nameof(HeizleistungMaxStundenH), k);
        }

        private static KeyValuePair<string, double> Paar(string k, double v) => new KeyValuePair<string, double>(k, v);
    }
}
