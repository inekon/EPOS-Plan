using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Ergebnisbilder je Stand</b> (Katalog v10): die Bilder der Reiter des Simulationsergebnisses — Bedarf,
    /// Wärmepumpe, Heizkessel, Solarthermie, BHKW, Photovoltaik — als Berichtsbilder aus dem Zeitreihensatz des Laufs.
    /// Dieselben Renderer-Aufrufe wie die Ergebnisseite (<see cref="ChartRenderer.ErzeugerStapelModell"/>,
    /// <see cref="ChartRenderer.GanglinieNormiertModell"/>, <see cref="ChartRenderer.JahresverlaufModell(string, double[], string, Farbrolle, Achsenfenster)"/>,
    /// <see cref="ChartRenderer.StreuwolkeModell"/>), dieselben Titel, Legenden und Farbrollen.
    ///
    /// <para><b>Der Bericht zeigt jedes Bild mit allen Reihen im Jahresverlauf</b> — ohne die Schalter der Reiter
    /// (Reihenwahl, „sortiert“) — und aus den Stundenreihen des Zeitreihensatzes: Wo die Seite eine Stufengröße der
    /// Kaskade zeigt (Stufeneingang, Restwärme der Stufe), steht im Bericht die Projektgröße
    /// (<see cref="ZeitreihenSatz.WAERMEBEDARF"/>, <see cref="ZeitreihenSatz.WAERMEREST"/>); die Photovoltaik steht
    /// im Stundenraster und ohne die zweite Achse des Speicherfüllstands. Die Marke der Seite sagt darum „ähnlich im
    /// Bericht“.</para>
    ///
    /// <para><b>Ein Plan für Bild und Excel-Diagramm:</b> <see cref="ErgebnisbildPlan"/> nennt Titel, Achse und Reihen;
    /// <see cref="Ergebnisbild"/> zeichnet daraus das Modell, die Excel-Quelle überträgt dieselben Reihen. Ohne die
    /// Reihen des Bildes (der Lauf führte den Erzeuger nicht) ist der Plan <c>null</c> — kein Bild.</para>
    /// </summary>
    public static partial class Berichtsbilder
    {
        /// <summary>Die Namen der Ergebnisbilder in der Folge der Reiter (Schlüssel <c>stand.bild.&lt;name&gt;</c>).</summary>
        public static readonly IReadOnlyList<string> Ergebnisbilder = new[]
        {
            "bedarf_waerme", "bedarf_strom", "bedarf_kaelte",
            "waermepumpe", "waermepumpe_strom", "waermepumpe_streuwolke",
            "heizkessel", "solarthermie", "bhkw", "photovoltaik",
        };

        /// <summary>
        /// Das Ergebnisbild ohne Excel-Diagramm: die Streuwolke trägt Punkte über der Außentemperatur, und die
        /// Excel-Diagramme kennen keine Punktwolke (<c>Excelreihenart</c>) — sie bleibt ein Bild des Word-Berichts.
        /// </summary>
        public const string ERGEBNISBILD_STREUWOLKE = "waermepumpe_streuwolke";

        /// <summary>Die Form eines Ergebnisbilds.</summary>
        public enum Ergebnisbildform
        {
            /// <summary>Stapel und Linien über dem Jahr (<see cref="ChartRenderer.ErzeugerStapelModell"/>).</summary>
            Stapel,

            /// <summary>Linien in Prozent des gemeinsamen Höchstwerts (<see cref="ChartRenderer.GanglinieNormiertModell"/>).</summary>
            Normiert,

            /// <summary>Eine Linie über dem Jahr (<see cref="ChartRenderer.JahresverlaufModell(string, double[], string, Farbrolle, Achsenfenster)"/>).</summary>
            Einzellinie,

            /// <summary>Punkte über der Außentemperatur (<see cref="ChartRenderer.StreuwolkeModell"/>).</summary>
            Streuwolke,
        }

        /// <summary>Titel, Achsen und Reihen eines Ergebnisbilds — gemeinsam für Bild und Excel-Diagramm.</summary>
        public sealed class Ergebnisbildplan
        {
            /// <summary>Die Form des Bildes.</summary>
            public Ergebnisbildform Form;

            /// <summary>Der Titel, den das Bild zeichnet.</summary>
            public string Titel = "";

            /// <summary>Die Beschriftung der y-Achse.</summary>
            public string YTitel = "";

            /// <summary>Die Beschriftung der x-Achse (nur Streuwolke).</summary>
            public string XTitel = "";

            /// <summary>Die Einheit der Werte für Excel („kW“, „%“).</summary>
            public string Einheit = "kW";

            /// <summary>Monatsgrenzen oder Jahresstunden.</summary>
            public ChartRenderer.Achse Achse = ChartRenderer.Achse.Monate;

            /// <summary>Die gestapelten Reihen in Kaskadenfolge.</summary>
            public List<ChartRenderer.Reihe> Stapel = new List<ChartRenderer.Reihe>();

            /// <summary>Die Linien über dem Stapel, die letzte ganz oben.</summary>
            public List<ChartRenderer.Reihe> Linien = new List<ChartRenderer.Reihe>();

            /// <summary>Die Punktreihen der Streuwolke.</summary>
            public List<ChartRenderer.Punktreihe> Punkte = new List<ChartRenderer.Punktreihe>();
        }

        /// <summary>Ist <paramref name="name"/> ein Ergebnisbild (<see cref="Ergebnisbilder"/>)?</summary>
        public static bool IstErgebnisbild(string name) => Ergebnisbilder.Contains(name ?? "", StringComparer.Ordinal);

        /// <summary>Das Zeichenmodell eines Ergebnisbilds; <c>null</c> ohne die Reihen des Bildes.</summary>
        public static Zeichenmodell Ergebnisbild(string name, ZeitreihenSatz z)
        {
            Ergebnisbildplan p = ErgebnisbildPlan(name, z);
            if (p == null) return null;
            switch (p.Form)
            {
                case Ergebnisbildform.Normiert:
                    return ChartRenderer.GanglinieNormiertModell(p.Titel, p.Linien, p.YTitel, p.Achse, false);
                case Ergebnisbildform.Einzellinie:
                    ChartRenderer.Reihe r = p.Linien[0];
                    return ChartRenderer.JahresverlaufModell(p.Titel, (double[])r.Werte.Clone(), p.YTitel, Farbrolle.BEDARF);
                case Ergebnisbildform.Streuwolke:
                    return ChartRenderer.StreuwolkeModell(p.Titel, p.XTitel, p.YTitel, p.Punkte);
                default:
                    return ChartRenderer.ErzeugerStapelModell(p.Titel, p.Stapel, p.Linien, null, p.YTitel, p.Achse, false);
            }
        }

        /// <summary>
        /// Der Plan eines Ergebnisbilds aus dem Zeitreihensatz; <c>null</c> ohne Satz, ohne die Reihen des Bildes oder
        /// für einen unbekannten Namen.
        /// </summary>
        public static Ergebnisbildplan ErgebnisbildPlan(string name, ZeitreihenSatz z)
        {
            if (z == null || string.IsNullOrEmpty(name)) return null;
            switch (name)
            {
                case "bedarf_waerme": return BedarfWaerme(z);
                case "bedarf_strom": return Normiertes(z, ZeitreihenSatz.STROMBEDARF, R.CHART_TITEL_STROMBEDARF_JAHRESGANGLINIE,
                                                       R.CHART_ACHSE_STROMBEDARF, R.CHART_ACHSE_STROMBEDARF);
                case "bedarf_kaelte": return Normiertes(z, ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG),
                                                        R.CHART_TITEL_KAELTELAST_JAHRESGANGLINIE, R.CHART_ACHSE_KAELTELAST,
                                                        R.CHART_ACHSE_KAELTELAST);
                case "waermepumpe": return Waermepumpe(z);
                case "waermepumpe_strom": return WaermepumpeStrom(z);
                case ERGEBNISBILD_STREUWOLKE: return Streuwolke(z);
                case "heizkessel": return Heizkessel(z);
                case "solarthermie": return Solarthermie(z);
                case "bhkw": return Bhkw(z);
                case "photovoltaik": return Photovoltaik(z);
                default: return null;
            }
        }

        // =====================================================================
        //  Die Pläne
        // =====================================================================

        private static Ergebnisbildplan BedarfWaerme(ZeitreihenSatz z)
        {
            double[] summe = z.Hole(ZeitreihenSatz.WAERMEBEDARF);
            if (!z.Hat(ZeitreihenSatz.WAERMEBEDARF)) return null;
            var p = Normiert(R.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE, R.CHART_ACHSE_WAERMELAST);
            p.Linien.Add(new ChartRenderer.Reihe(R.CHART_LEGENDE_SUMME_WAERMEBEDARF, Kopie(summe), Farbrolle.BEDARF));
            Farbrolle[] rollen = { Farbrolle.HEIZWAERME, Farbrolle.WARMWASSER, Farbrolle.PROZESSWAERME };
            foreach (int k in Kanal.KANAELE_WAERME)
            {
                string schluessel = ZeitreihenSatz.BedarfSchluessel(k);
                if (!z.Hat(schluessel)) continue;
                p.Linien.Add(new ChartRenderer.Reihe(Warnkriterien.KanalAnzeige(k), Kopie(z.Hole(schluessel)),
                                                     rollen[k % rollen.Length]));
            }
            return p;
        }

        private static Ergebnisbildplan Normiertes(ZeitreihenSatz z, string schluessel, string titel, string yTitel, string legende)
        {
            if (!z.Hat(schluessel)) return null;
            var p = Normiert(titel, yTitel);
            p.Linien.Add(new ChartRenderer.Reihe(legende, Kopie(z.Hole(schluessel)), Farbrolle.BEDARF));
            return p;
        }

        private static Ergebnisbildplan Normiert(string titel, string yTitel)
        {
            return new Ergebnisbildplan
            {
                Form = Ergebnisbildform.Normiert, Titel = titel, YTitel = yTitel, Einheit = "%",
                Achse = ChartRenderer.Achse.Monate,
            };
        }

        private static Ergebnisbildplan Waermepumpe(ZeitreihenSatz z)
        {
            if (z.Hole(ZeitreihenSatz.WP_WAERME) == null) return null;
            var p = Stapelbild(R.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE, R.CHART_ACHSE_WAERMELAST, ChartRenderer.Achse.Jahresstunden);
            p.Stapel.Add(Saeule(R.CHART_LEGENDE_WAERMEPRODUKTION, z.Hole(ZeitreihenSatz.WP_WAERME), Farbrolle.WAERME_WP));
            if (z.Hole(ZeitreihenSatz.HEIZSTAB) != null)
                p.Stapel.Add(Saeule(R.CHART_SEGMENT_HEIZSTAB, z.Hole(ZeitreihenSatz.HEIZSTAB), Farbrolle.HEIZSTAB));
            Bedarfslinie(p, z, R.CHART_LEGENDE_WAERMEBEDARF);
            return p;
        }

        private static Ergebnisbildplan WaermepumpeStrom(ZeitreihenSatz z)
        {
            double[] wp = z.Hole(ZeitreihenSatz.WP_STROM);
            if (z.Hole(ZeitreihenSatz.WP_WAERME) == null || wp == null) return null;
            double[] heizstab = z.Hole(ZeitreihenSatz.HEIZSTAB);
            var gesamt = new double[wp.Length];
            for (int i = 0; i < wp.Length; i++)
                gesamt[i] = wp[i] + (heizstab != null && i < heizstab.Length ? heizstab[i] : 0.0);
            var p = new Ergebnisbildplan
            {
                Form = Ergebnisbildform.Einzellinie, Titel = R.CHART_TITEL_STROMBEDARF_JAHRESGANGLINIE,
                YTitel = R.CHART_ACHSE_STROMBEDARF, Achse = ChartRenderer.Achse.Monate,
            };
            p.Linien.Add(new ChartRenderer.Reihe(R.CHART_ACHSE_STROMBEDARF, gesamt, Farbrolle.BEDARF));
            return p;
        }

        private static Ergebnisbildplan Streuwolke(ZeitreihenSatz z)
        {
            double[] t = z.Hole(ZeitreihenSatz.TEMPERATUR);
            double[] prod = z.Hole(ZeitreihenSatz.WP_WAERME);
            if (t == null || prod == null) return null;
            double[] bed = z.Hole(ZeitreihenSatz.WAERMEBEDARF);
            double[] hs = z.Hole(ZeitreihenSatz.HEIZSTAB);
            var bedarf = new List<(double, double)>();
            var produktion = new List<(double, double)>();
            var heizstab = new List<(double, double)>();
            for (int n = 0; n < ZeitreihenSatz.Stunden && n < t.Length && n < prod.Length; n++)
            {
                double x = Math.Round(t[n], 1);
                if (bed != null && n < bed.Length) bedarf.Add((x, bed[n]));
                produktion.Add((x, prod[n]));
                double h = hs != null && n < hs.Length ? hs[n] : 0.0;
                heizstab.Add((x, h > 0 ? prod[n] + h : 0.0));
            }
            var p = new Ergebnisbildplan
            {
                Form = Ergebnisbildform.Streuwolke, Titel = R.CHART_TITEL_LEISTUNG_UEBER_AUSSENTEMPERATUR,
                XTitel = R.CHART_ACHSE_TEMPERATUR, YTitel = R.SIM_SPALTE_LEISTUNG,
            };
            if (bedarf.Count > 0)
                p.Punkte.Add(new ChartRenderer.Punktreihe(R.CHART_LEGENDE_WAERMEBEDARF, bedarf, Farbrolle.BEDARF, 120));
            p.Punkte.Add(new ChartRenderer.Punktreihe(R.CHART_SEGMENT_HEIZSTAB, heizstab, Farbrolle.HEIZSTAB, 120));
            p.Punkte.Add(new ChartRenderer.Punktreihe(R.CHART_LEGENDE_WAERMEPRODUKTION, produktion, Farbrolle.WAERME_WP, 120));
            return p;
        }

        private static Ergebnisbildplan Heizkessel(ZeitreihenSatz z)
        {
            if (z.Hole(ZeitreihenSatz.KESSEL_WAERME) == null) return null;
            var p = Stapelbild(R.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE, R.CHART_ACHSE_WAERMELAST, ChartRenderer.Achse.Monate);
            p.Stapel.Add(Saeule(R.CHART_LEGENDE_WAERMEPRODUKTION_HEIZKESSEL, z.Hole(ZeitreihenSatz.KESSEL_WAERME),
                                Farbrolle.WAERME_KESSEL));
            if (z.Hole(ZeitreihenSatz.WAERMEREST) != null)
                p.Linien.Add(new ChartRenderer.Reihe(R.CHART_SEGMENT_RESTWAERME, Kopie(z.Hole(ZeitreihenSatz.WAERMEREST)),
                                                     Farbrolle.REST));
            Bedarfslinie(p, z, R.CHART_LEGENDE_WAERMEBEDARF_GESAMT);
            return p;
        }

        private static Ergebnisbildplan Solarthermie(ZeitreihenSatz z)
        {
            if (z.Hole(ZeitreihenSatz.SOLAR_WAERME) == null) return null;
            var p = Stapelbild(R.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE, R.CHART_ACHSE_WAERMELAST, ChartRenderer.Achse.Jahresstunden);
            Bedarfslinie(p, z, R.CHART_LEGENDE_WAERMEBEDARF);
            p.Linien.Add(new ChartRenderer.Reihe(R.CHART_LEGENDE_WAERMEPRODUKTION, Kopie(z.Hole(ZeitreihenSatz.SOLAR_WAERME)),
                                                 Farbrolle.WAERME_SOLAR));
            return p;
        }

        private static Ergebnisbildplan Bhkw(ZeitreihenSatz z)
        {
            if (z.Hole(ZeitreihenSatz.BHKW_WAERME) == null) return null;
            var p = Stapelbild(R.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE, R.CHART_ACHSE_WAERMELAST, ChartRenderer.Achse.Monate);
            p.Stapel.Add(Saeule(R.CHART_LEGENDE_WAERMEPRODUKTION, z.Hole(ZeitreihenSatz.BHKW_WAERME), Farbrolle.WAERME_BHKW));
            if (z.Hole(ZeitreihenSatz.WAERMEREST) != null)
                p.Linien.Add(new ChartRenderer.Reihe(R.CHART_SEGMENT_RESTWAERME, Kopie(z.Hole(ZeitreihenSatz.WAERMEREST)),
                                                     Farbrolle.REST));
            Bedarfslinie(p, z, R.CHART_LEGENDE_WAERMEBEDARF);
            return p;
        }

        private static Ergebnisbildplan Photovoltaik(ZeitreihenSatz z)
        {
            double[] genutzt = z.Hole(ZeitreihenSatz.PV_GENUTZT);
            if (genutzt == null) return null;
            double[] ueberschuss = z.Hole(ZeitreihenSatz.PV_UEBERSCHUSS);
            var p = Stapelbild(R.CHART_TITEL_STROMBEDARF_PV_JAHRESGANGLINIE, R.CHART_ACHSE_LEISTUNG, ChartRenderer.Achse.Monate);
            if (ueberschuss != null)
                p.Linien.Add(new ChartRenderer.Reihe(R.CHART_LEGENDE_UEBERSCHUSS, Kopie(ueberschuss), Farbrolle.UEBERSCHUSS));
            if (z.Hole(ZeitreihenSatz.STROMBEDARF) != null)
                p.Linien.Add(new ChartRenderer.Reihe(R.CHART_ACHSE_STROMBEDARF, Kopie(z.Hole(ZeitreihenSatz.STROMBEDARF)),
                                                     Farbrolle.BEDARF));
            // Die Erzeugung der Module: genutzt und eingespeist zusammen.
            var erzeugung = new double[genutzt.Length];
            for (int i = 0; i < genutzt.Length; i++)
                erzeugung[i] = genutzt[i] + (ueberschuss != null && i < ueberschuss.Length ? ueberschuss[i] : 0.0);
            p.Linien.Add(new ChartRenderer.Reihe(R.SIM_PHOTOVOLTAIK, erzeugung, Farbrolle.STROM_PV));
            return p;
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static Ergebnisbildplan Stapelbild(string titel, string yTitel, ChartRenderer.Achse achse)
        {
            return new Ergebnisbildplan { Form = Ergebnisbildform.Stapel, Titel = titel, YTitel = yTitel, Achse = achse };
        }

        private static ChartRenderer.Reihe Saeule(string name, double[] werte, Farbrolle rolle)
            => new ChartRenderer.Reihe(name, Kopie(werte), rolle, ChartRenderer.Stapelart.Saeule);

        /// <summary>Der Wärmebedarf des Projekts als oberste Linie (die Bezugsgröße), wenn der Satz ihn führt.</summary>
        private static void Bedarfslinie(Ergebnisbildplan p, ZeitreihenSatz z, string legende)
        {
            double[] bedarf = z.Hole(ZeitreihenSatz.WAERMEBEDARF);
            if (bedarf != null) p.Linien.Add(new ChartRenderer.Reihe(legende, Kopie(bedarf), Farbrolle.BEDARF));
        }

        private static double[] Kopie(double[] werte) => werte == null ? new double[0] : (double[])werte.Clone();
    }
}
