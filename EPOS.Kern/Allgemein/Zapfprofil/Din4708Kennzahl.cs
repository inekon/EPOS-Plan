using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Ausstattungsklasse aus <c>Tab_TwwDin4708Wert_STAMM</c> (Art <c>AUSSTATTUNG</c>):
    /// Id, neutraler Schlüssel und Σ v·w_v der Klasse [Wh].
    /// </summary>
    internal sealed record Din4708Ausstattung(int Id, string Schluessel, double WertWh);

    /// <summary>
    /// Die Katalogwerte der DIN-4708-Kennzahl (Konzept 3.1): Belegung p je Raumzahl (Art
    /// <c>BELEGUNG</c>, Schlüssel wie <c>Tab_TwwDin4708Wert_STAMM.Schluessel</c>) und die
    /// Ausstattungsklassen. Nie abgedruckt, nie im Quelltext.
    /// </summary>
    internal sealed record Din4708Katalog(IReadOnlyDictionary<string, double> BelegungJeRaumzahl,
                                          IReadOnlyList<Din4708Ausstattung> Ausstattungen)
    {
        /// <summary>Ein leerer Katalog.</summary>
        internal static Din4708Katalog Leer => new Din4708Katalog(null, new Din4708Ausstattung[0]);
    }

    /// <summary>Eine aufgelöste Zeile der Wohnungstabelle: Anzahl n, Belegung p, Σ v·w_v [Wh], Zone.</summary>
    internal sealed record Din4708Zeile(string Zone, int Anzahl, double Belegung, double AusstattungWh,
                                        bool AusstattungVorgabe);

    /// <summary>Das Ergebnis des Normvergleichs einer Topologiegruppe (4.5 c).</summary>
    internal sealed record Din4708Ergebnis
    {
        /// <summary>Rechenbar und im Gültigkeitsbereich (mindestens eine Wohnzone mit Speicher)?</summary>
        public bool Gueltig { get; init; }

        /// <summary>Deckt die Kennzahl die ganze Gruppe (jede Zone Wohnen mit Speicher)?</summary>
        public bool Vollstaendig { get; init; }

        /// <summary>Warum nicht gültig — „außerhalb des Gültigkeitsbereichs" oder „nicht rechenbar"; sonst leer.</summary>
        public string Grund { get; init; } = "";

        /// <summary>Der Grund als Kennung (<see cref="ZapfAuslegungsfehler"/>); <c>null</c> bei gültig.</summary>
        public ZapfAuslegungsfehler? Fehler { get; init; }

        /// <summary>Die Bedarfskennzahl N.</summary>
        public double? KennzahlN { get; init; }

        /// <summary>Σ n · p der gerechneten Wohnungen [Personen].</summary>
        public double? Personen { get; init; }

        /// <summary>Σ n der gerechneten Wohnungen.</summary>
        public int Wohnungen { get; init; }

        /// <summary>Der Wärmebedarf der Zapfperiode W_z(N) [kWh].</summary>
        public double? WzKwh { get; init; }

        /// <summary>Das Volumen V_DIN [l] ohne Zuschlag.</summary>
        public double? VolumenL { get; init; }

        /// <summary>Die aufgelösten Zeilen der Wohnungstabelle.</summary>
        public IReadOnlyList<Din4708Zeile> Zeilen { get; init; } = new Din4708Zeile[0];

        /// <summary>Die Zonen außerhalb des Gültigkeitsbereichs.</summary>
        public IReadOnlyList<string> ZonenAusserhalb { get; init; } = new string[0];

        /// <summary>Hinweise (Wärmepumpe, Teilgültigkeit).</summary>
        public IReadOnlyList<Auslegungshinweis> Hinweise { get; init; } = new Auslegungshinweis[0];
    }

    /// <summary>
    /// <b>Die Bedarfskennzahl N nach DIN 4708 und der Wärmebedarf W_z</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.5 c, 4.7) — der Normvergleich der Dreiergruppe, nur für Zonen mit
    /// Nutzungsart Wohnen (Kalenderart Wohnen) und Topologie Speicher.
    ///
    /// <code>
    /// N     = Σ_Wohnungstypen (n · p · Σ v·w_v) / (p_b · w_b)
    ///         p aus der Wohnungstabelle bzw. dem Katalog (Raumzahl), Σ v·w_v aus der
    ///         Ausstattungsklasse; ohne Klasse die Einheitswohnung (Σ v·w_v = w_b)
    /// u_i   = a_i · z · (1 + √N) / √N,   i = 1, 2
    /// K(u)  = erf(u) für u unter der Kappung, sonst 1
    /// W_z   = W_b · [ N · K(u_1) + √N · K(u_2) ]                          [Wh → kWh]
    /// V_DIN = W_z · 1000 / (c_w · Δθ_Speicher) / f_nutz                   [l], ohne Zuschlag
    /// </code>
    ///
    /// <para>a_i, z, p_b, w_b, W_b und die Kappung sind Parameter
    /// (<see cref="ZapfAuslegungParameter"/>), p und Σ v·w_v Katalogwerte — nie Konstanten der
    /// Klasse. Ohne Wohnungstabelle ist die Kennzahl „nicht rechenbar", nie geschätzt. Die
    /// Kennzahl ist für Vorlauftemperaturen einer Wärmepumpe kaum aussagefähig — Kennung
    /// <see cref="HINWEIS_WAERMEPUMPE"/>.</para>
    /// </summary>
    internal static class Din4708Kennzahl
    {
        /// <summary>Kennung des Hinweises zur Wärmepumpe.</summary>
        internal const string HINWEIS_WAERMEPUMPE = "DIN4708_WAERMEPUMPE";

        /// <summary>Kennung: die Kennzahl deckt nur einen Teil der Gruppe.</summary>
        internal const string HINWEIS_TEILGUELTIG = "DIN4708_TEILGUELTIG";

        /// <summary>Text „außerhalb des Gültigkeitsbereichs".</summary>
        internal const string AUSSERHALB = "außerhalb des Gültigkeitsbereichs";

        // =================================================================================
        // Formeln
        // =================================================================================

        /// <summary>Die Fehlerfunktion erf(x) — Reihe mit positiven Gliedern, auf double-Genauigkeit.</summary>
        internal static double Erf(double x)
        {
            if (double.IsNaN(x)) return double.NaN;
            if (x < 0) return -Erf(-x);
            if (x > 6.0) return 1.0;
            // erf(x) = 2/√π · e^(−x²) · Σ_n 2^n x^(2n+1) / (1 · 3 · … · (2n+1))
            double x2 = x * x;
            double glied = x, summe = x;
            for (int n = 1; n < 1000; n++)
            {
                glied *= 2.0 * x2 / (2 * n + 1);
                summe += glied;
                if (glied < 1e-17 * summe) break;
            }
            double wert = 2.0 / Math.Sqrt(Math.PI) * Math.Exp(-x2) * summe;
            return wert > 1.0 ? 1.0 : wert;
        }

        /// <summary>K(u) = erf(u) für u unter der Kappung, sonst 1.</summary>
        internal static double K(double u, double kappung) => u < kappung ? Erf(u) : 1.0;

        /// <summary>N = Σ n · p · Σ v·w_v / (p_b · w_b).</summary>
        internal static double Kennzahl(IReadOnlyList<Din4708Zeile> zeilen, Parametersatz ps)
        {
            double pb = Auslegungspruefung.Positiv(ps.Wert(ZapfAuslegungParameter.DIN4708_PB), "p_b");
            double wb = Auslegungspruefung.Positiv(ps.Wert(ZapfAuslegungParameter.DIN4708_WB_ZAPFSTELLE), "w_b");
            double summe = 0.0;
            foreach (Din4708Zeile z in zeilen) summe += z.Anzahl * z.Belegung * z.AusstattungWh;
            return summe / (pb * wb);
        }

        /// <summary>
        /// Der Wärmebedarf der Zapfperiode W_z(N) [kWh] =
        /// <c>W_b · [N · K(u_1) + √N · K(u_2)] / 1000</c>; N ≤ 0 ergibt 0.
        /// </summary>
        internal static double WzKwh(double n, Parametersatz ps)
        {
            Auslegungspruefung.NichtNegativ(n, "die Kennzahl N");
            double a1 = ps.Wert(ZapfAuslegungParameter.DIN4708_A1);
            double a2 = ps.Wert(ZapfAuslegungParameter.DIN4708_A2);
            double z = ps.Wert(ZapfAuslegungParameter.DIN4708_Z);
            double wbBedarf = ps.Wert(ZapfAuslegungParameter.DIN4708_WB_BEDARF);
            double kappung = ps.Wert(ZapfAuslegungParameter.DIN4708_KAPPUNG);
            if (n == 0.0) return 0.0;
            double w = Math.Sqrt(n);
            double u1 = a1 * z * (1.0 + w) / w;
            double u2 = a2 * z * (1.0 + w) / w;
            return wbBedarf * (n * K(u1, kappung) + w * K(u2, kappung)) / Mengengeruest.WH_JE_KWH;
        }

        /// <summary>V_DIN [l] = W_z · 1000 / (c_w · Δθ_Speicher) / f_nutz — ohne Zuschlag.</summary>
        internal static double VolumenL(double wzKwh, double spreizungK, double nutzanteil)
        {
            Auslegungspruefung.Positiv(nutzanteil, "der Nutzanteil");
            if (!(spreizungK > 0))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.TemperaturUngueltig,
                    "Nicht rechenbar — die Spreizung des Speichers ist nicht positiv.");
            return wzKwh * Mengengeruest.WH_JE_KWH / (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * spreizungK) / nutzanteil;
        }

        // =================================================================================
        // Wohnungstabelle und Gültigkeit
        // =================================================================================

        /// <summary>
        /// Die Zeilen der Wohnungstabelle einer Zone, aufgelöst: Belegung wie im Mengengerüst
        /// (<see cref="Mengengeruest.Belegung"/>), Ausstattung aus dem Katalog oder die
        /// Einheitswohnung. Unbekannte Klasse oder Belegung: benannte Ablehnung.
        /// </summary>
        internal static IReadOnlyList<Din4708Zeile> Zeilen(ZonenStand z, Din4708Katalog katalog, Parametersatz ps)
        {
            var zeilen = new List<Din4708Zeile>();
            string zone = z.Name ?? "";
            double wb = ps.Wert(ZapfAuslegungParameter.DIN4708_WB_ZAPFSTELLE);
            foreach (WohnungstypStand w in z.Wohnungen ?? new WohnungstypStand[0])
            {
                if (w.Anzahl <= 0)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.WohnungstypUngueltig,
                        "Nicht rechenbar — ein Wohnungstyp der Zone „" + zone + "“ hat keine positive Anzahl.");
                double p;
                try
                {
                    p = Mengengeruest.Belegung(w, z, katalog?.BelegungJeRaumzahl, zone);
                }
                catch (ZapfprofilEingabeException ex)
                {
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.WohnungstypUngueltig, ex.Message);
                }
                double wert = wb;
                bool vorgabe = true;
                if (w.IdAusstattung.HasValue)
                {
                    Din4708Ausstattung a = null;
                    if (katalog?.Ausstattungen != null)
                        foreach (Din4708Ausstattung k in katalog.Ausstattungen)
                            if (k != null && k.Id == w.IdAusstattung.Value) a = k;
                    if (a == null)
                        throw new ZapfAuslegungException(ZapfAuslegungsfehler.WohnungstypUngueltig,
                            "Nicht rechenbar — die Ausstattungsklasse " + w.IdAusstattung.Value + " eines Wohnungstyps der Zone „"
                            + zone + "“ steht nicht im Katalog.");
                    wert = Auslegungspruefung.NichtNegativ(a.WertWh, "Σ v·w_v der Ausstattung „" + a.Schluessel + "“");
                    vorgabe = false;
                }
                zeilen.Add(new Din4708Zeile(zone, w.Anzahl, p, wert, vorgabe));
            }
            return zeilen.AsReadOnly();
        }

        /// <summary>Ist die Zone im Gültigkeitsbereich — Nutzungsart Wohnen und Topologie Speicher?</summary>
        internal static bool Gilt(ZonenStand z, Nutzungsart n)
            => z != null && n != null && n.Kalender == ZapfKalenderart.Wohnen && z.Topologie == ZapfTopologie.Speicher;

        /// <summary>
        /// <b>Der Normvergleich einer Topologiegruppe:</b> N über die Wohnzonen mit Speicher, W_z
        /// und V_DIN. Zonen außerhalb des Gültigkeitsbereichs werden genannt; ohne gültige Zone
        /// „außerhalb des Gültigkeitsbereichs", ohne Wohnungstabelle „nicht rechenbar".
        /// </summary>
        internal static Din4708Ergebnis Rechnen(IReadOnlyList<(ZonenStand Zone, Nutzungsart Art)> zonen,
                                                Din4708Katalog katalog, Parametersatz ps, double spreizungK,
                                                double nutzanteil)
        {
            var ausserhalb = new List<string>();
            var gueltige = new List<ZonenStand>();
            foreach (var (zone, art) in zonen ?? new (ZonenStand, Nutzungsart)[0])
            {
                if (Gilt(zone, art)) gueltige.Add(zone);
                else ausserhalb.Add(zone?.Name ?? "");
            }
            if (gueltige.Count == 0)
                return new Din4708Ergebnis
                {
                    Gueltig = false, Fehler = ZapfAuslegungsfehler.NichtGueltig,
                    Grund = "DIN 4708: " + AUSSERHALB + " — die Kennzahl gilt nur für Wohnen mit Speicher.",
                    ZonenAusserhalb = ausserhalb.AsReadOnly()
                };

            var zeilen = new List<Din4708Zeile>();
            try
            {
                foreach (ZonenStand z in gueltige)
                {
                    IReadOnlyList<Din4708Zeile> eigene = Zeilen(z, katalog, ps);
                    if (eigene.Count == 0)
                        throw new ZapfAuslegungException(ZapfAuslegungsfehler.WohnungstabelleFehlt,
                            "DIN 4708: nicht rechenbar — die Zone „" + z.Name + "“ trägt keine Wohnungstabelle.");
                    zeilen.AddRange(eigene);
                }
            }
            catch (ZapfAuslegungException ex)
            {
                return new Din4708Ergebnis
                {
                    Gueltig = false, Fehler = ex.Fehler, Grund = ex.Message, ZonenAusserhalb = ausserhalb.AsReadOnly()
                };
            }

            double n = Kennzahl(zeilen, ps);
            double wz = WzKwh(n, ps);
            double personen = 0.0;
            int wohnungen = 0;
            foreach (Din4708Zeile z in zeilen)
            {
                personen += z.Anzahl * z.Belegung;
                wohnungen += z.Anzahl;
            }
            var hinweise = new List<Auslegungshinweis>
            {
                new Auslegungshinweis(HINWEIS_WAERMEPUMPE,
                    "Die Bedarfskennzahl nach DIN 4708 ist für die Vorlauftemperaturen einer Wärmepumpe kaum aussagefähig.")
            };
            if (ausserhalb.Count > 0)
                hinweise.Add(new Auslegungshinweis(HINWEIS_TEILGUELTIG,
                    "Die Kennzahl N = " + n.ToString("0.##", CultureInfo.InvariantCulture) + " deckt nur die Wohnzonen mit Speicher; "
                    + AUSSERHALB + ": " + string.Join(", ", ausserhalb) + "."));
            return new Din4708Ergebnis
            {
                Gueltig = true,
                Vollstaendig = ausserhalb.Count == 0,
                KennzahlN = n,
                Personen = personen,
                Wohnungen = wohnungen,
                WzKwh = wz,
                VolumenL = VolumenL(wz, spreizungK, nutzanteil),
                Zeilen = zeilen.AsReadOnly(),
                ZonenAusserhalb = ausserhalb.AsReadOnly(),
                Hinweise = hinweise.AsReadOnly()
            };
        }
    }
}
