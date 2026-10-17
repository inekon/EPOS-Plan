using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Teillastpunkte A–D eines Ökodesign-Produktdatenblatts</b> (K-C; Datenweg <c>OEKODESIGN</c> des Entwurfs K-D):
    /// liest je Punkt den Namen, die Außentemperatur T_j, die deklarierte Kälteleistung P_dc, den deklarierten EER_d, das
    /// Teillastverhältnis der Prüfung und eine zweite Bedingung, prüft die Punkte und bildet sie gegen ein
    /// Volllastkennfeld auf die Teillastkurve von KM3 ab.
    ///
    /// <para><b>Neutral.</b> Der Leser kennt keine Geräteart: Die zweite Bedingung ist beim Kaltwassersatz die
    /// Rückkühltemperatur, beim Split- oder Multisplitgerät (K-D) die Raumluft; das Volllastkennfeld kommt als Delegat
    /// herein. <b>Keine Normwerte im Code:</b> Lage, Teillastverhältnis und Bedingungen der Punkte stehen in der Datei
    /// (sie stammen aus dem Datenblatt des Herstellers); der Leser setzt keinen Punkt aus einer Tabelle ein.</para>
    ///
    /// <para><b>Zeilenform:</b> <c>Punkt; Name; T_j [°C]; P_dc [kW]; EER_d [-]; Teillastverhältnis [%]; zweite
    /// Bedingung [°C]</c> — die letzten beiden Felder dürfen fehlen. Eine Zeile „Punkt“ ohne Zahl im dritten Feld ist eine
    /// Spaltenkopfzeile.</para>
    /// </summary>
    public static class OekodesignPunkteLeser
    {
        /// <summary>Kopfschlüssel einer Punktzeile (normiert).</summary>
        public const string SCHLUESSEL = "PUNKT";

        /// <summary>
        /// Relativer Spielraum, ab dem ein Punkt als taktend gilt: P_dc liegt um mehr als diesen Anteil über der Last der
        /// Prüfung (Teillastverhältnis mal P_design). EPOS-Vorgabe gegen Rundungen im Datenblatt, kein Normwert.
        /// </summary>
        public const double TOLERANZ_TAKT = 0.02;

        /// <summary>Höchstzahl der Punkte je Gerät (A–D und Reserve).</summary>
        public const int MAX_PUNKTE = 8;

        /// <summary>Ein Teillastpunkt des Datenblatts.</summary>
        /// <param name="Name">Name des Punkts („A“ … „D“), wie in der Datei.</param>
        /// <param name="Aussentemperatur">Außentemperatur T_j [°C].</param>
        /// <param name="LeistungKw">Deklarierte Kälteleistung P_dc [kW].</param>
        /// <param name="Eer">Deklarierter EER_d [-].</param>
        /// <param name="LastverhaeltnisProzent">Teillastverhältnis der Prüfung [%]; <c>null</c> = keine Angabe.</param>
        /// <param name="Zweittemperatur">Zweite Bedingung [°C] (Rückkühltemperatur bzw. Raumluft); <c>null</c> = keine Angabe.</param>
        /// <param name="Zeile">Zeilennummer in der Datei (1-basiert), für Meldungen.</param>
        public sealed record Punkt(string Name, double Aussentemperatur, double LeistungKw, double Eer,
                                   double? LastverhaeltnisProzent, double? Zweittemperatur, int Zeile);

        /// <summary>Ein abgebildeter Punkt: Lastgrad gegen die Volllast und EER-Verhältnis gegen den Volllast-EER.</summary>
        public readonly record struct Teillastpunkt(string Name, double Lastgrad, double EerVerhaeltnis, bool Taktet);

        /// <summary>Das Ergebnis der Abbildung auf die Teillastkurve von KM3.</summary>
        public sealed class Teillastabbildung
        {
            /// <summary>Die abgebildeten Punkte in der Folge der Datei.</summary>
            public List<Teillastpunkt> Punkte { get; } = new List<Teillastpunkt>();

            /// <summary>Die normierte Kurve EIRFPLR; <c>null</c> = nicht ableitbar oder nicht plausibel.</summary>
            public KaeltemaschineTeillastkurve.Kurve? Kurve { get; set; }

            /// <summary>Untere Gültigkeit x_u der Kurve: der kleinste Lastgrad der Punkte.</summary>
            public double? LastgradMin { get; set; }

            /// <summary>Kleinste Dauerleistung als Anteil der Bezugsleistung aus den taktenden Punkten; <c>null</c> = kein Punkt taktet.</summary>
            public double? MindestteillastAnteil { get; set; }

            /// <summary><c>true</c> = mindestens ein Punkt taktet (P_dc über der Last der Prüfung).</summary>
            public bool Taktet => Punkte.Any(p => p.Taktet);

            /// <summary>Hinweise zur Abbildung.</summary>
            public List<string> Hinweise { get; } = new List<string>();
        }

        /// <summary>
        /// Liest eine Punktzeile (die Felder ohne den Schlüssel „Punkt“ an erster Stelle sind <paramref name="felder"/>[1…]).
        /// Gibt <c>null</c> zurück, wenn die Zeile eine Spaltenkopfzeile ist (<paramref name="fehler"/> leer) oder ungültig
        /// (<paramref name="fehler"/> nennt den Grund).
        /// </summary>
        public static Punkt ZeileLesen(IReadOnlyList<string> felder, char trenn, int zeile, out string fehler)
        {
            fehler = null;
            if (felder == null || felder.Count < 2) { fehler = "Teillastpunkt ohne Angaben"; return null; }
            string name = (felder[1] ?? "").Trim();
            double? tj = Feld(felder, 2, trenn);
            if (!tj.HasValue && Enumerable.Range(2, Math.Max(0, felder.Count - 2)).All(i => !Feld(felder, i, trenn).HasValue))
                return null; // Spaltenkopfzeile
            double? pdc = Feld(felder, 3, trenn), eer = Feld(felder, 4, trenn);
            double? lv = Feld(felder, 5, trenn), zweit = Feld(felder, 6, trenn);
            if (name.Length == 0) { fehler = "Teillastpunkt ohne Namen"; return null; }
            if (!tj.HasValue || !pdc.HasValue || !eer.HasValue)
            {
                fehler = "Teillastpunkt " + name + " unvollständig (Außentemperatur, Leistung und EER sind Pflicht)";
                return null;
            }
            if (!(pdc.Value > 0)) { fehler = "Teillastpunkt " + name + ": Leistung nicht größer 0"; return null; }
            if (!(eer.Value > 0)) { fehler = "Teillastpunkt " + name + ": EER nicht größer 0"; return null; }
            if (UngueltigesFeld(felder, 5, trenn, lv) || (lv.HasValue && (!(lv.Value > 0) || lv.Value > 100)))
            {
                fehler = "Teillastpunkt " + name + ": Teillastverhältnis außerhalb 0 bis 100 %";
                return null;
            }
            if (UngueltigesFeld(felder, 6, trenn, zweit))
            {
                fehler = "Teillastpunkt " + name + ": zweite Temperatur keine Zahl";
                return null;
            }
            return new Punkt(name.ToUpperInvariant(), tj.Value, pdc.Value, eer.Value, lv, zweit, zeile);
        }

        /// <summary>
        /// Prüft die Punkte eines Geräts: mindestens einer, höchstens <see cref="MAX_PUNKTE"/>, Namen verschieden;
        /// <paramref name="pdesignKw"/> ≠ <c>null</c> muss größer 0 sein. <c>null</c> = in Ordnung, sonst der Grund.
        /// </summary>
        public static string Pruefen(IReadOnlyList<Punkt> punkte, double? pdesignKw)
        {
            if (punkte == null || punkte.Count == 0) return "keine Teillastpunkte";
            if (punkte.Count > MAX_PUNKTE)
                return "mehr als " + MAX_PUNKTE.ToString(CultureInfo.InvariantCulture) + " Teillastpunkte";
            string doppelt = punkte.GroupBy(p => p.Name).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            if (doppelt != null) return "Teillastpunkt " + doppelt + " doppelt";
            if (pdesignKw.HasValue && !(pdesignKw.Value > 0)) return "Pdesignc nicht größer 0";
            return null;
        }

        /// <summary>
        /// Der Bezugspunkt der Volllast: der Punkt „A“, sonst der Punkt mit dem größten Teillastverhältnis, sonst der mit
        /// der höchsten Außentemperatur (bei Gleichstand der erste der Datei).
        /// </summary>
        public static Punkt Bezugspunkt(IReadOnlyList<Punkt> punkte)
        {
            if (punkte == null || punkte.Count == 0) return null;
            Punkt a = punkte.FirstOrDefault(p => p.Name == "A");
            if (a != null) return a;
            if (punkte.Any(p => p.LastverhaeltnisProzent.HasValue))
                return punkte.Where(p => p.LastverhaeltnisProzent.HasValue).OrderByDescending(p => p.LastverhaeltnisProzent.Value).First();
            return punkte.OrderByDescending(p => p.Aussentemperatur).First();
        }

        /// <summary>
        /// <b>Die Abbildung der Punkte auf die Teillastkurve von KM3.</b> Je Punkt liefert <paramref name="volllast"/> die
        /// Volllastleistung Q_VL und den Volllast-EER des Kennfelds bei den Bedingungen des Punkts; daraus
        /// Lastgrad x = min(1, P_dc / Q_VL) und EER-Verhältnis g = EER_d / EER_VL. Ab
        /// <see cref="KaeltemaschineTeillastkurve.MIN_ZEILEN"/> verschiedenen Lastgraden die Kurve nach kleinsten Quadraten
        /// (<see cref="KaeltemaschineTeillastkurve.AusEerVerhaeltnis"/>), normiert und auf Plausibilität geprüft;
        /// x_u = kleinster Lastgrad.
        ///
        /// <para><b>Takten.</b> Liegt P_dc um mehr als <see cref="TOLERANZ_TAKT"/> über der Last der Prüfung
        /// (Teillastverhältnis mal <paramref name="pdesignKw"/>), läuft das Gerät an diesem Punkt mit seiner kleinsten
        /// Dauerleistung und taktet darunter: Der Punkt zählt mit x = P_dc / Q_VL zur Kurve (EER_d ist der Dauerwert dort),
        /// und die kleinste solche Leistung, bezogen auf <paramref name="bezugKw"/>, ist die Mindestteillast. Den Verlust des
        /// Taktens trägt der Taktverlustfaktor C_d, nicht die Kurve.</para>
        /// </summary>
        public static Teillastabbildung Teillast(IReadOnlyList<Punkt> punkte, double? pdesignKw, double bezugKw,
                                                 Func<Punkt, (double LeistungKw, double Eer)> volllast)
        {
            var e = new Teillastabbildung();
            if (punkte == null || punkte.Count == 0 || volllast == null) return e;
            bool ohneLast = false;
            var zeilen = new List<(double Lastgrad, double Verhaeltnis)>();
            double? kleinsteDauer = null;
            foreach (Punkt p in punkte)
            {
                (double qv, double ev) = volllast(p);
                if (!(qv > 0) || !(ev > 0))
                {
                    e.Hinweise.Add("Teillastpunkt " + p.Name + ": Kennfeld ohne Volllastwert bei diesen Bedingungen, Punkt übergangen");
                    continue;
                }
                double x = p.LeistungKw / qv;
                if (x > 1 + TOLERANZ_TAKT)
                    e.Hinweise.Add("Teillastpunkt " + p.Name + ": Leistung über der Volllast des Kennfelds (" + Zahl(x * 100) +
                                   " %), Lastgrad auf 100 % begrenzt");
                x = Math.Min(1.0, x);
                double g = p.Eer / ev;
                bool taktet = false;
                if (p.LastverhaeltnisProzent.HasValue && pdesignKw.HasValue)
                    taktet = p.LeistungKw > p.LastverhaeltnisProzent.Value / 100.0 * pdesignKw.Value * (1 + TOLERANZ_TAKT);
                else if (p.LastverhaeltnisProzent.HasValue)
                    ohneLast = true;
                if (taktet && bezugKw > 0)
                    kleinsteDauer = Math.Min(kleinsteDauer ?? double.MaxValue, p.LeistungKw / bezugKw);
                e.Punkte.Add(new Teillastpunkt(p.Name, x, g, taktet));
                zeilen.Add((x, g));
            }
            if (ohneLast) e.Hinweise.Add("Teillastverhältnis ohne Pdesignc: Takten der Punkte nicht beurteilt");
            if (kleinsteDauer.HasValue)
                e.MindestteillastAnteil = Math.Round(Math.Min(1.0, kleinsteDauer.Value), KaeltemaschineTeillastkurve.NACHKOMMA_LASTGRAD,
                                                     MidpointRounding.AwayFromZero);
            if (zeilen.Count == 0) return e;
            double xu = Math.Round(zeilen.Min(z => z.Lastgrad), KaeltemaschineTeillastkurve.NACHKOMMA_LASTGRAD, MidpointRounding.AwayFromZero);
            KaeltemaschineTeillastkurve.Kurve? k = KaeltemaschineTeillastkurve.AusEerVerhaeltnis(zeilen);
            if (!k.HasValue)
            {
                e.Hinweise.Add("weniger als " + KaeltemaschineTeillastkurve.MIN_ZEILEN.ToString(CultureInfo.InvariantCulture) +
                               " Teillastpunkte mit verschiedenem Lastgrad, keine Teillastkurve");
                return e;
            }
            if (!KaeltemaschineTeillastkurve.IstLinear(k.Value) &&
                (!KaeltemaschineTeillastkurve.Bereich(k.Value) || !KaeltemaschineStammCtrl.KurvePlausibel(k.Value.A, k.Value.B, k.Value.C, xu)))
            {
                e.Hinweise.Add("Teillastkurve aus den Punkten nicht plausibel, keine Teillastkurve");
                return e;
            }
            e.Kurve = k;
            e.LastgradMin = xu;
            return e;
        }

        private static double? Feld(IReadOnlyList<string> f, int i, char trenn) =>
            i < f.Count ? KaeltemaschineCsvLeser.Zahl(f[i], trenn) : null;

        private static bool UngueltigesFeld(IReadOnlyList<string> f, int i, char trenn, double? wert) =>
            i < f.Count && !string.IsNullOrWhiteSpace(f[i]) && !wert.HasValue;

        private static string Zahl(double x) => x.ToString("0", CultureInfo.InvariantCulture);
    }
}
