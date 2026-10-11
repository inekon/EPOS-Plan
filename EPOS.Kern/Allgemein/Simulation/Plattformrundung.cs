using System;
using System.Runtime.CompilerServices;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Naht der plattformabhängigen Rundung.</b> <c>Math.Exp</c>, <c>Sin</c>, <c>Cos</c>,
    /// <c>Asin</c> und <c>Acos</c> reicht .NET an die C‑Laufzeit der Plattform durch, und die
    /// rundet nicht überall gleich: Unter Windows (UCRT) liegen 0,7–5 % der Ergebnisse ein ulp
    /// neben denen unter Linux (glibc). Die Rechnung muss das aushalten — keine
    /// Betriebsentscheidung darf an diesem letzten Bit hängen (Zahlenrand, <see cref="Rechenrand"/>).
    ///
    /// <para><b>Wofür die Naht da ist.</b> Den Nachweis, dass die Rechnung es aushält, führt
    /// der Referenzlauf mit <c>lauf … --stoerung ulp</c>: Er schaltet <see cref="UlpStoerung"/>
    /// ein, und jede Funktion hier verschiebt dann ungefähr jedes sechzehnte Ergebnis um genau
    /// ein ulp nach oben oder unten — deterministisch, nach dem Bitmuster des Arguments, also
    /// wie eine andere C‑Bibliothek. Der gestörte Lauf muss mit dem ungestörten innerhalb der
    /// Toleranz übereinstimmen (<c>kern.yml</c>). Ohne den Schalter liefert jede Funktion
    /// bitgleich <c>Math.*</c>.</para>
    ///
    /// <para><b>Warum hier und nicht überall.</b> Die Naht sitzt an den Aufrufstellen, an denen
    /// die Messung des Plattformbefunds PB‑1 (625 757 Argumente aller fünfzehn Referenzprojekte,
    /// gegen korrekt gerundete Werte gehalten) abweichende Rundungen fand, die bis in die
    /// Ergebnisse reichen: die Gebäudematrix nach VDI 6007 (<c>Matrix2</c>), der Sonnenstand
    /// (<c>SolarPVGISCalculator</c>), die Erdreichtemperatur (<c>ErdreichTemperatur</c>), der
    /// Einfallswinkel des Kollektors (<c>SimulationSolarthermie</c>) und der Tagesbilanz‑Weg
    /// (<c>TagesbilanzPhysik</c>). <c>Pow</c> und <c>Log</c> fehlen mit Absicht: Sie runden
    /// ebenfalls verschieden, wirken aber in keinem Projekt. Die übrigen Aufrufstellen des Laufs
    /// (Neigungsfaktor im Klimaweg, Kaltwassergang, Zonenkopplung) stehen nicht in der
    /// Messtabelle von PB‑1 und rechnen mit wenigen festen Argumenten — alle nicht aufgeführten
    /// Stellen zusammen 68 der 625 757; der Rest des Kerns rechnet Import, Zeichnung und
    /// Auslegung. Eine Umstellung ALLER Aufrufer wäre eine breite Änderung ohne Messgrund; eine
    /// Klasse <c>Math</c>, die <c>System.Math</c> im Namensraum verdeckt, eine Falle für jeden
    /// Leser.</para>
    ///
    /// <para><b>Was sie im Normalbetrieb kostet: nichts Messbares, und sie ändert nichts.</b>
    /// <see cref="UlpStoerung"/> ist ein <c>static readonly bool</c>, einmal beim ersten Zugriff
    /// aus dem <see cref="AppContext"/>‑Schalter <see cref="SCHALTER_ULP"/> gelesen. Der JIT
    /// behandelt ein solches Feld nach der Typinitialisierung als Konstante und streicht den
    /// Zweig in der optimierten Fassung ganz; es bleibt der Aufruf von <c>Math.*</c>. Die
    /// Anwendung und die Tests setzen den Schalter nie. Wache:
    /// <c>EPOS.Kern.Tests/PlattformrundungTests</c>.</para>
    /// </summary>
    internal static class Plattformrundung
    {
        /// <summary>
        /// Name des <see cref="AppContext"/>‑Schalters. Gesetzt wird er allein vom Referenzlauf
        /// (<c>EPOS.Referenzlauf lauf … --stoerung ulp</c>), und zwar vor der ersten Rechnung —
        /// danach ist <see cref="UlpStoerung"/> gelesen und bleibt, wie es ist.
        /// </summary>
        internal const string SCHALTER_ULP = "EPOS.Plattformrundung.UlpStoerung";

        /// <summary>
        /// Anteil der verschobenen Ergebnisse als Maske auf den Mischwert: 4 Bit, also eines von
        /// sechzehn (6,25 %) — am oberen Rand der gemessenen 0,7–5 %, damit die Probe eher
        /// strenger ist als eine echte zweite C‑Bibliothek.
        /// </summary>
        private const ulong ANTEIL_MASKE = 0xF;

        /// <summary>Das Bit des Mischwerts, das die Richtung wählt.</summary>
        private const ulong RICHTUNG_BIT = 0x10;

        // Je Funktion ein eigenes Salz: Sin und Cos desselben Arguments sind unabhängig
        // gestört, wie in einer echten Bibliothek, in der jede Funktion ihre eigenen schweren
        // Rundungsfälle hat.
        private const ulong SALZ_EXP = 0x4578700000000001UL;
        private const ulong SALZ_SIN = 0x53696E0000000002UL;
        private const ulong SALZ_COS = 0x436F730000000003UL;
        private const ulong SALZ_ASIN = 0x4173696E00000004UL;
        private const ulong SALZ_ACOS = 0x41636F7300000005UL;

        /// <summary>
        /// Ist die ±1‑ulp‑Störung eingeschaltet? Einmal gelesen, danach unveränderlich (siehe
        /// Klassenkommentar zu den Kosten).
        /// </summary>
        internal static readonly bool UlpStoerung =
            AppContext.TryGetSwitch(SCHALTER_ULP, out bool an) && an;

        /// <summary><c>Math.Exp</c>, unter dem Schalter um ±1 ulp gestört.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Exp(double x)
        {
            double y = Math.Exp(x);
            return UlpStoerung ? Verschieben(y, x, SALZ_EXP) : y;
        }

        /// <summary><c>Math.Sin</c>, unter dem Schalter um ±1 ulp gestört.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Sin(double x)
        {
            double y = Math.Sin(x);
            return UlpStoerung ? Verschieben(y, x, SALZ_SIN) : y;
        }

        /// <summary><c>Math.Cos</c>, unter dem Schalter um ±1 ulp gestört.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Cos(double x)
        {
            double y = Math.Cos(x);
            return UlpStoerung ? Verschieben(y, x, SALZ_COS) : y;
        }

        /// <summary><c>Math.Asin</c>, unter dem Schalter um ±1 ulp gestört.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Asin(double x)
        {
            double y = Math.Asin(x);
            return UlpStoerung ? Verschieben(y, x, SALZ_ASIN) : y;
        }

        /// <summary><c>Math.Acos</c>, unter dem Schalter um ±1 ulp gestört.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Acos(double x)
        {
            double y = Math.Acos(x);
            return UlpStoerung ? Verschieben(y, x, SALZ_ACOS) : y;
        }

        /// <summary>
        /// <b>Die Störung selbst</b>, ohne Schalter prüfbar: Verschiebt <paramref name="wert"/>
        /// um genau ein ulp, wenn das gemischte Bitmuster von <paramref name="argument"/> und
        /// <paramref name="salz"/> es verlangt (eines von sechzehn), sonst bleibt er.
        ///
        /// <para>Unberührt bleiben 0, ±1 und nicht endliche Werte: Diese Ergebnisse (etwa
        /// <c>Exp(0)</c>, <c>Cos(0)</c>) sind exakt darstellbar, jede Bibliothek trifft sie, und
        /// eine Verschiebung von ±1 könnte aus dem Wertebereich von Sinus und Kosinus
        /// hinausführen.</para>
        /// </summary>
        internal static double Verschieben(double wert, double argument, ulong salz)
        {
            if (wert == 0.0 || wert == 1.0 || wert == -1.0 || !double.IsFinite(wert)) return wert;

            ulong h = Mischen((ulong)BitConverter.DoubleToInt64Bits(argument) ^ salz);
            if ((h & ANTEIL_MASKE) != 0) return wert;

            return (h & RICHTUNG_BIT) != 0 ? Math.BitIncrement(wert) : Math.BitDecrement(wert);
        }

        /// <summary>Der Finalisierer von SplitMix64: verteilt benachbarte Bitmuster gleichmäßig.</summary>
        private static ulong Mischen(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
