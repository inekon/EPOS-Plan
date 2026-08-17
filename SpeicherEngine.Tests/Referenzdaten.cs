using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Zugriff auf die Referenzdaten der V7-Mappe unter <c>TestData\</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Beide Dateien sind unveraenderte Kopien aus
    /// <c>Documents\Stromspeicher\Claude_Analyse_V7\referenzdaten\</c>.
    /// </para>
    /// <para><b>Format <c>psim_daten.csv</c></b> (35.138 Zeilen, CRLF, UTF-8 ohne BOM):
    /// Kopfzeile <c>row,A,B,C,D,E,F,G</c>, Trennzeichen Komma, Dezimaltrenner Punkt,
    /// keine Anfuehrungszeichen, keine Tausendertrenner, immer 8 Felder. Spalte
    /// <c>row</c> ist die Blattzeile (2 .. 35138), also gilt Index = row - 2 und
    /// n = 35.137. Spalten: B = Lastgang [kW], C = PV [kW], D = Preis [ct/kWh],
    /// E = Soll-SoC [kWh], F = Soll-Geldwert [EUR], G = Graustromertrag (unbenutzt).
    /// In den beiden letzten Zeilen (35137/35138) sind A .. D leer; leere Zellen
    /// werden - wie in VBA - als 0 gelesen (siehe <see cref="Zahl"/>).</para>
    /// <para><b>Format <c>psim_param.csv</c></b> (105 Zeilen, CRLF, UTF-8 ohne BOM):
    /// Kopfzeile <c>ref,value,formula</c>. Die Spalte <c>formula</c> enthaelt
    /// Kommata und ist deshalb in Anfuehrungszeichen gesetzt - der Parser muss
    /// CSV-Quoting koennen. <c>value</c> ist teils Zahl (invariant, teils
    /// E-Notation wie <c>1E-3</c>), teils Text (<c>&gt; Nutzungsdauer</c>), teils leer.</para>
    /// <para>
    /// Beide Dateien werden genau einmal je Testlauf gelesen; <see cref="Lazy{T}"/>
    /// mit Vollsynchronisation macht das fuer parallel laufende Testklassen sicher.
    /// </para>
    /// </remarks>
    internal static class Referenzdaten
    {
        /// <summary>Sollwert der Jahressumme Sigma F [EUR] (Fachkonzept 6.2, Verifikationsanker).</summary>
        public const double SummeGeldwertSollEur = 60616.562388122424;

        private static readonly Lazy<Zeitreihen> _reihen =
            new Lazy<Zeitreihen>(LiesZeitreihen, isThreadSafe: true);

        private static readonly Lazy<IReadOnlyDictionary<string, string>> _parameter =
            new Lazy<IReadOnlyDictionary<string, string>>(LiesParameter, isThreadSafe: true);

        /// <summary>Zeitreihen aus <c>psim_daten.csv</c>.</summary>
        public static Zeitreihen Reihen => _reihen.Value;

        /// <summary>Pfad einer Datei im Ausgabeverzeichnis <c>TestData\</c>.</summary>
        public static string Pfad(string dateiname)
            => Path.Combine(AppContext.BaseDirectory, "TestData", dateiname);

        // ------------------------------------------------------------------ Parameter

        /// <summary>Rohwert einer Blattzelle aus <c>psim_param.csv</c> (z. B. "J3", "N16").</summary>
        public static string Text(string zelle)
        {
            if (!_parameter.Value.TryGetValue(zelle, out string? wert))
                throw new InvalidOperationException($"Zelle '{zelle}' steht nicht in {ParameterDatei}.");
            return wert;
        }

        /// <summary>Zahlwert einer Blattzelle aus <c>psim_param.csv</c>, invariant geparst.</summary>
        public static double Wert(string zelle)
        {
            string s = Text(zelle);
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                throw new InvalidOperationException($"Zelle '{zelle}' ist keine Zahl: '{s}'.");
            return d;
        }

        // ------------------------------------------------------------------ Parametersatz

        /// <summary>
        /// Baut den Parametersatz des V7-Referenzfalls ausschliesslich aus
        /// <c>psim_param.csv</c> - nichts ist im Testcode hart kodiert.
        /// </summary>
        /// <remarks>
        /// Zuordnung: J3 -&gt; C_nom, J4 -&gt; P, J6 -&gt; SoC_min, J7 -&gt; SoC_max,
        /// J5 -&gt; pauschaler Verlustfaktor, J23 -&gt; Einspeiseverguetung,
        /// N4 -&gt; Kapitalzins, N5 -&gt; c_cap, N7 -&gt; Nutzungsdauer.
        /// c_pow und I_fix sind 0, damit I = J3 * N5 = N6 herauskommt.
        /// Die Degradation (J8) bleibt 0: die V7-Mappe rechnet ohne Degradation.
        /// </remarks>
        public static SpeicherParameter V7Parameter()
        {
            return new SpeicherParameter
            {
                CNomKwh = Wert("J3"),
                PKw = Wert("J4"),
                SoCMinKwh = Wert("J6"),
                SoCMaxKwh = Wert("J7"),
                VerlustfaktorPauschal = Wert("J5"),
                VerguetungCtKwh = Wert("J23"),
                CCapEurProKwh = Wert("N5"),
                CPowEurProKw = 0.0,
                IFixEur = 0.0,
                Kapitalzins = Wert("N4"),
                NutzungsdauerA = Wert("N7"),
                DegradationProA = 0.0,
                DtH = 0.25
            };
        }

        /// <summary>Eingang B/C/D des Referenzfalls.</summary>
        public static SpeicherEingang V7Eingang()
        {
            Zeitreihen r = Reihen;
            return new SpeicherEingang(r.LastKw, r.PvKw, r.PreisCtKwh);
        }

        // ------------------------------------------------------------------ Einlesen

        private const string DatenDatei = "psim_daten.csv";
        private const string ParameterDatei = "psim_param.csv";

        private static Zeitreihen LiesZeitreihen()
        {
            string pfad = Pfad(DatenDatei);
            if (!File.Exists(pfad))
                throw new FileNotFoundException($"Referenzdaten fehlen: {pfad}", pfad);

            string[] zeilen = File.ReadAllLines(pfad, new UTF8Encoding(false));
            if (zeilen.Length < 2)
                throw new InvalidOperationException($"{DatenDatei} enthaelt keine Datenzeilen.");

            string[] kopf = ZerlegeCsv(zeilen[0]);
            int iB = SpaltenIndex(kopf, "B");
            int iC = SpaltenIndex(kopf, "C");
            int iD = SpaltenIndex(kopf, "D");
            int iE = SpaltenIndex(kopf, "E");
            int iF = SpaltenIndex(kopf, "F");

            int n = zeilen.Length - 1;
            // Die Datei kann mit einer Leerzeile enden - die zaehlt nicht mit.
            if (zeilen[zeilen.Length - 1].Length == 0) n--;

            var last = new double[n];
            var pv = new double[n];
            var preis = new double[n];
            var sollSoc = new double[n];
            var sollGeld = new double[n];

            for (int k = 0; k < n; k++)
            {
                string[] f = ZerlegeCsv(zeilen[k + 1]);
                last[k] = Zahl(f, iB);
                pv[k] = Zahl(f, iC);
                preis[k] = Zahl(f, iD);
                sollSoc[k] = Zahl(f, iE);
                sollGeld[k] = Zahl(f, iF);
            }

            return new Zeitreihen(last, pv, preis, sollSoc, sollGeld);
        }

        private static IReadOnlyDictionary<string, string> LiesParameter()
        {
            string pfad = Pfad(ParameterDatei);
            if (!File.Exists(pfad))
                throw new FileNotFoundException($"Referenzparameter fehlen: {pfad}", pfad);

            var werte = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] zeilen = File.ReadAllLines(pfad, new UTF8Encoding(false));
            for (int i = 1; i < zeilen.Length; i++)
            {
                if (zeilen[i].Length == 0) continue;
                string[] f = ZerlegeCsv(zeilen[i]);
                if (f.Length < 2 || f[0].Length == 0) continue;
                werte[f[0]] = f[1];
            }
            return werte;
        }

        private static int SpaltenIndex(string[] kopf, string name)
        {
            for (int i = 0; i < kopf.Length; i++)
                if (string.Equals(kopf[i], name, StringComparison.Ordinal)) return i;
            throw new InvalidOperationException($"Spalte '{name}' fehlt in der Kopfzeile von {DatenDatei}.");
        }

        /// <summary>
        /// Feldwert als <c>double</c>. Leere Zellen liefern 0 - das ist die
        /// VBA-Semantik (<c>Empty</c> bzw. Text an <c>As Double</c>), nach der die
        /// beiden letzten Blattzeilen ohne B/C/D-Werte gerechnet wurden.
        /// </summary>
        private static double Zahl(string[] felder, int index)
        {
            if (index >= felder.Length) return 0.0;
            string s = felder[index];
            if (s.Length == 0) return 0.0;
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return 0.0;
            return d;
        }

        /// <summary>
        /// Minimaler CSV-Zerleger: Komma als Trenner, <c>"</c> als Feldbegrenzer,
        /// <c>""</c> als eingebettetes Anfuehrungszeichen. Mehr braucht keine der
        /// beiden Dateien.
        /// </summary>
        private static string[] ZerlegeCsv(string zeile)
        {
            var felder = new List<string>(8);
            var puffer = new StringBuilder(32);
            bool inQuotes = false;

            for (int i = 0; i < zeile.Length; i++)
            {
                char c = zeile[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < zeile.Length && zeile[i + 1] == '"') { puffer.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else puffer.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { felder.Add(puffer.ToString()); puffer.Clear(); }
                else puffer.Append(c);
            }
            felder.Add(puffer.ToString());
            return felder.ToArray();
        }

        /// <summary>Die fuenf gebrauchten Spalten der Referenzdatei.</summary>
        internal sealed class Zeitreihen
        {
            public Zeitreihen(double[] lastKw, double[] pvKw, double[] preisCtKwh,
                              double[] sollSoCKwh, double[] sollGeldwertEur)
            {
                LastKw = lastKw;
                PvKw = pvKw;
                PreisCtKwh = preisCtKwh;
                SollSoCKwh = sollSoCKwh;
                SollGeldwertEur = sollGeldwertEur;
            }

            /// <summary>Blattspalte B [kW].</summary>
            public double[] LastKw { get; }

            /// <summary>Blattspalte C [kW].</summary>
            public double[] PvKw { get; }

            /// <summary>Blattspalte D [ct/kWh].</summary>
            public double[] PreisCtKwh { get; }

            /// <summary>Blattspalte E [kWh] - Sollwert des Ladezustands.</summary>
            public double[] SollSoCKwh { get; }

            /// <summary>Blattspalte F [EUR] - Sollwert des Geldwerts.</summary>
            public double[] SollGeldwertEur { get; }

            /// <summary>Anzahl der Intervalle n (Referenzfall 35.137).</summary>
            public int Anzahl => LastKw.Length;
        }
    }
}
