using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die Grenzwerte des Wärmedurchgangswiderstands eines inhomogenen Aufbaus [m²K/W] (DIN EN ISO 6946, 6.7).</summary>
    /// <param name="RT_Oben_M2KW">Oberer Grenzwert R'_T (Streifen parallel).</param>
    /// <param name="RT_Unten_M2KW">Unterer Grenzwert R''_T (Ebenen in Reihe, je Ebene parallel).</param>
    public sealed record Abschnittsgrenzen(double RT_Oben_M2KW, double RT_Unten_M2KW)
    {
        /// <summary>Der Wärmedurchgangswiderstand R_T = (R'_T + R''_T) / 2.</summary>
        public double RT_M2KW => (RT_Oben_M2KW + RT_Unten_M2KW) / 2.0;

        /// <summary>
        /// Die größte mögliche relative Abweichung e = (R'_T − R''_T) / (2 R_T) des Mittels R_T vom wahren Wert
        /// (DIN EN ISO 6946, 6.7.2.5) — eine Schranke, kein Zuschlag: R_T bleibt das Mittel.
        /// </summary>
        public double Fehlerschranke => RT_M2KW > 0 ? (RT_Oben_M2KW - RT_Unten_M2KW) / (2.0 * RT_M2KW) : 0.0;

        /// <summary>
        /// Liegt <see cref="Fehlerschranke"/> über <see cref="InhomogeneSchichten.FEHLERSCHRANKE_GRENZE"/>? Dann trägt der
        /// vereinfachte Weg die Genauigkeit nicht, die die Norm ihm zuschreibt — ein Hinweis, keine Ablehnung.
        /// </summary>
        public bool FehlerschrankeUeberschritten => Fehlerschranke > InhomogeneSchichten.FEHLERSCHRANKE_GRENZE + 1e-12;
    }

    /// <summary>
    /// <b>Die inhomogene Schicht</b> (BK-5, Entwurf Bauteilkatalog 6a, E132 F7) — der Rechenweg eines Aufbaus mit Abschnitten
    /// nach der Methode von DIN EN ISO 6946 Abschnitt 6.7 und die Ersatzschichten für die dynamische Reduktion.
    ///
    /// <para><b>Modell.</b> Ein Abschnitt m ist ein Streifen mit dem Flächenanteil f_m (am Abschnitt, Summe 1 ± 0,01; gerechnet
    /// mit f_m / Σf). Eine Schicht ohne Abschnitt ist eine homogene Ebene über die ganze Fläche. Schichten mit Abschnitt, die mit
    /// aufsteigender Abschnittsnummer aufeinander folgen, liegen nebeneinander in einer inhomogenen Ebene j (<see cref="Ebenen"/>);
    /// jede inhomogene Ebene führt jeden Abschnitt des Aufbaus genau einmal.</para>
    ///
    /// <para><b>Grenzwerte.</b> Oberer: 1/R'_T = Σ_m f_m / R_T,m mit R_T,m = R_si + Σ_j R_mj + R_se (homogene Ebenen in jedem
    /// Streifen). Unterer: R''_T = R_si + Σ_j R_j + R_se mit 1/R_j = Σ_m f_m / R_mj für eine inhomogene Ebene. R_T ist das Mittel,
    /// U = 1 / R_T.</para>
    ///
    /// <para><b>Ersatzschicht.</b> Für die Reduktion nach VDI 6007 wird jede inhomogene Ebene eine homogene Schicht: Dicke
    /// d_j = Σ f_m d_mj, Widerstand R_j·k mit k = (R_T − R_si − R_se − Σ R_hom) / Σ R_j (so trägt die Reihe genau R_T; k ≥ 1),
    /// λ_eff = d_j / (k·R_j), ρ_eff = Σ f_m ρ_m d_mj / d_j und c_eff = Σ f_m ρ_m c_m d_mj / (ρ_eff d_j) — die Kapazität der Ebene
    /// flächengewichtet. Eine Ebene mit einer Luftschicht ist eine Luftschicht mit äquivalenter Leitfähigkeit.</para>
    ///
    /// <para><b>Fehlerschranke.</b> e = (R'_T − R''_T) / (2 R_T) nach 6.7.2.5 (<see cref="Abschnittsgrenzen.Fehlerschranke"/>);
    /// über <see cref="FEHLERSCHRANKE_GRENZE"/> nennt der Summenfuß einen Hinweis. Der Rechenweg bleibt das Mittel.</para>
    ///
    /// <para><b>Ein Aufbau ohne Abschnitt</b> läuft nicht hierher; sein Weg bleibt unverändert.</para>
    /// </summary>
    public static class InhomogeneSchichten
    {
        /// <summary>Die zulässige Abweichung der Summe der Flächenanteile von 1.</summary>
        public const double ANTEIL_TOLERANZ = 0.01;

        /// <summary>
        /// Die Höchstgrenze der relativen Abweichung e für den Hinweis (DIN EN ISO 6946, 6.7.2.5): 20 %. Sie entspricht dem
        /// Verhältnis R'_T / R''_T = 1,5 — bis dahin gilt das vereinfachte Verfahren nach 6.7 als anwendbar; darüber (etwa
        /// Metall, das die Dämmung durchdringt) verweist die Norm auf eine numerische Rechnung nach DIN EN ISO 10211.
        /// </summary>
        public const double FEHLERSCHRANKE_GRENZE = 0.20;

        /// <summary>Führt der Aufbau eine Schicht mit Abschnitt?</summary>
        public static bool Inhomogen(BauteilaufbauModel m) => m?.Schichten != null && m.Schichten.Any(s => s?.Abschnitt != null);

        /// <summary>
        /// <b>Die Schichtebenen</b> in Listenreihenfolge, je Ebene die Indizes ihrer Schichten: eine Schicht ohne Abschnitt ist
        /// eine Ebene; eine Schicht mit Abschnitt gehört zur Ebene ihrer Vorgängerin, wenn auch diese einen Abschnitt mit
        /// kleinerer Nummer trägt, sonst beginnt sie eine neue.
        /// </summary>
        public static List<List<int>> Ebenen(IReadOnlyList<BauteilschichtModel> schichten)
        {
            var ebenen = new List<List<int>>();
            int? vorher = null;
            for (int i = 0; i < (schichten?.Count ?? 0); i++)
            {
                int? a = schichten[i]?.Abschnitt;
                if (a.HasValue && vorher.HasValue && a.Value > vorher.Value) ebenen[ebenen.Count - 1].Add(i);
                else ebenen.Add(new List<int> { i });
                vorher = a;
            }
            return ebenen;
        }

        /// <summary>Die Abschnittsnummern einer Ebene, aufsteigend; leer für eine homogene Ebene.</summary>
        private static List<int> Nummern(IReadOnlyList<BauteilschichtModel> schichten, List<int> ebene) =>
            ebene.Where(i => schichten[i]?.Abschnitt != null).Select(i => schichten[i].Abschnitt.Value).ToList();

        /// <summary>
        /// <b>Die Prüfung des Abschnittsaufbaus</b> ohne Datenbank — <c>null</c> = rechenbar, sonst die erste Lücke als Meldung:
        /// jede inhomogene Ebene mit denselben Abschnitten, jeder Abschnitt mit Flächenanteil größer null, Summe 1 ± 0,01.
        /// Ein homogener Aufbau ist immer in Ordnung.
        /// </summary>
        public static string Pruefen(BauteilaufbauModel m, string wer = null)
        {
            if (!Inhomogen(m)) return null;
            wer = string.IsNullOrWhiteSpace(wer) ? (string.IsNullOrWhiteSpace(m.Bezeichner) ? MyResource.Resource.BTA_WER_AUFBAU : m.Bezeichner.Trim()) : wer;
            List<BauteilschichtModel> zeilen = m.Schichten.Where(s => s != null).ToList();
            List<List<int>> ebenen = Ebenen(zeilen);
            List<int> satz = null;
            for (int j = 0; j < ebenen.Count; j++)
            {
                List<int> n = Nummern(zeilen, ebenen[j]);
                if (n.Count == 0) continue;
                if (satz == null) { satz = n; continue; }
                if (!n.SequenceEqual(satz))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BTA_ABS_MSG_EBENE, wer, j + 1,
                                         string.Join(", ", n), string.Join(", ", satz));
            }
            double summe = 0.0;
            foreach (int nr in satz)
            {
                BauteilabschnittModel a = (m.Abschnitte ?? new List<BauteilabschnittModel>()).FirstOrDefault(x => x != null && x.Nummer == nr);
                if (a == null || !(a.Flaechenanteil > 0))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BTA_ABS_MSG_ANTEIL, wer, nr);
                summe += a.Flaechenanteil.Value;
            }
            if (Math.Abs(summe - 1.0) > ANTEIL_TOLERANZ + 1e-12)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BTA_ABS_MSG_SUMME, wer, summe);
            return null;
        }

        /// <summary>Die Flächenanteile der Abschnitte, auf Σ = 1 genormt, je Nummer (nach bestandener <see cref="Pruefen"/>).</summary>
        private static Dictionary<int, double> Anteile(BauteilaufbauModel m, IEnumerable<int> nummern)
        {
            var roh = nummern.ToDictionary(nr => nr, nr => m.Abschnitte.First(x => x != null && x.Nummer == nr).Flaechenanteil.Value);
            double summe = roh.Values.Sum();
            return roh.ToDictionary(p => p.Key, p => p.Value / summe);
        }

        /// <summary>
        /// <b>Grenzwerte und Ersatzschichten</b> eines inhomogenen Aufbaus. <paramref name="schichten"/> sind die Schichten des
        /// Bauteilwegs zu <c>m.Schichten</c> (ohne <c>null</c>), Index für Index.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.SchichtUngueltig"/>, wenn
        /// <see cref="Pruefen"/> eine Lücke nennt.</exception>
        internal static (Abschnittsgrenzen Grenzen, IReadOnlyList<Schicht> Ersatz) Rechnen(
            BauteilaufbauModel m, IReadOnlyList<Schicht> schichten, Waermestromrichtung richtung, double rSi, double rSe, string wer = null)
        {
            if (Pruefen(m, wer) is string befund) throw new GebaeudeModellException(GebaeudeModellFehler.SchichtUngueltig, befund);
            List<BauteilschichtModel> zeilen = m.Schichten.Where(s => s != null).ToList();
            if (schichten == null || schichten.Count != zeilen.Count)
                throw new ArgumentException("Die Schichten passen nicht zu den Zeilen des Aufbaus.", nameof(schichten));
            List<List<int>> ebenen = Ebenen(zeilen);
            List<int> satz = ebenen.Select(e => Nummern(zeilen, e)).First(n => n.Count > 0);
            Dictionary<int, double> f = Anteile(m, satz);

            double rHom = 0.0, rInhUnten = 0.0;
            var rStreifen = satz.ToDictionary(nr => nr, nr => 0.0);
            var rEbene = new double[ebenen.Count];
            for (int j = 0; j < ebenen.Count; j++)
            {
                List<int> e = ebenen[j];
                if (zeilen[e[0]].Abschnitt == null)
                {
                    double r = Bauteilreduktion.Waermedurchlasswiderstand(schichten[e[0]], richtung, wer, e[0] + 1);
                    rHom += r;
                    rEbene[j] = r;
                    continue;
                }
                double leitwert = 0.0;
                foreach (int i in e)
                {
                    double r = Bauteilreduktion.Waermedurchlasswiderstand(schichten[i], richtung, wer, i + 1);
                    rStreifen[zeilen[i].Abschnitt.Value] += r;
                    leitwert += f[zeilen[i].Abschnitt.Value] / r;
                }
                rEbene[j] = 1.0 / leitwert;
                rInhUnten += rEbene[j];
            }

            double obenLeitwert = satz.Sum(nr => f[nr] / (rSi + rHom + rStreifen[nr] + rSe));
            var grenzen = new Abschnittsgrenzen(1.0 / obenLeitwert, rSi + rHom + rInhUnten + rSe);
            double k = rInhUnten > 0 ? (grenzen.RT_M2KW - rSi - rSe - rHom) / rInhUnten : 1.0;

            var ersatz = new List<Schicht>(ebenen.Count);
            for (int j = 0; j < ebenen.Count; j++)
            {
                List<int> e = ebenen[j];
                if (zeilen[e[0]].Abschnitt == null) { ersatz.Add(schichten[e[0]]); continue; }
                double d = 0.0, rhoD = 0.0, c = 0.0;
                bool luft = false, ohneRho = true;
                foreach (int i in e)
                {
                    Schicht s = schichten[i];
                    double fi = f[zeilen[i].Abschnitt.Value];
                    d += fi * s.Dicke_M;
                    luft |= s.IstLuftschicht;
                    if (!double.IsNaN(s.Rohdichte_KgM3)) { rhoD += fi * s.Rohdichte_KgM3 * s.Dicke_M; ohneRho = false; }
                    c += fi * Bauteilreduktion.FlaechenbezogeneKapazitaet(s);
                }
                double lambda = d / (k * rEbene[j]);
                double rho = ohneRho ? double.NaN : rhoD / d;
                double cp = ohneRho || !(rho > 0) || !(c > 0) ? double.NaN : c / (rho * d);
                ersatz.Add(new Schicht(d, lambda, rho, cp, luft));
            }
            return (grenzen, ersatz);
        }
    }
}
