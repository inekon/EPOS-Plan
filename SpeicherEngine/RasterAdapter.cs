using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Adapterschicht zwischen dem Hausdatentyp <c>float[]</c> von EPOS-Plan und dem
    /// internen <c>double[]</c> der Engine (Fachkonzept 3.3, Umsetzungskonzept AP2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// EPOS-Plan fuehrt zwei Raster parallel: 8.760 Stundenwerte im Physik- und
    /// BHKW-Kernel, 35.040 Viertelstundenwerte im Strompfad und in den Charts. Die
    /// Engine rechnet ausschliesslich im Viertelstundenraster in <c>double</c>.
    /// Konvertiert wird deshalb nur hier, an den Raendern.
    /// </para>
    /// <para>
    /// <b>Expansionsregel.</b> Ein Stundenwert wird als ueber die Stunde konstante
    /// Leistung auf vier Viertelstunden gelegt - <b>Wertwiederholung ohne
    /// Interpolation</b>, semantisch identisch zu
    /// <c>SimulationControl.Stundenwerte_zu_viertelstunden</c> (und den
    /// gleichnamigen Kopien in <c>SimulationPV</c> und <c>SimulationStrombedarf</c>):
    /// <c>v[i*4+0..3] = w[i]</c>. Die frueher daneben stehende, linear
    /// interpolierende Variante <c>Stundenwerte_zu_viertelstunden_Interpoliert</c>
    /// ist ausdruecklich <b>nicht</b> gemeint; sie glaettete allein die Treppenstufen
    /// des stuendlich gerechneten Ladezustands und ist mit AP2b entfallen, weil die
    /// Engine den SoC nativ viertelstuendlich liefert.
    /// </para>
    /// <para>
    /// <b>Verlustfreiheit.</b> <c>float</c> nach <c>double</c> ist eine erweiternde
    /// Umwandlung und damit exakt. Der Rueckweg <see cref="ZuFloat"/> rundet auf die
    /// naechste <c>float</c>-Zahl - er ist ausschliesslich fuer Charts und CSV-Export
    /// gedacht, nie fuer Rechenwege.
    /// </para>
    /// <para>
    /// <b>Schaltjahr.</b> Bewusst noch nicht unterstuetzt: 8.784 / 35.136 lehnt die
    /// Klasse ab. Der Bestand kennt ausschliesslich 8.760 / 35.040 (feste Feldgroessen
    /// des Rechenkerns), und der Schaltjahresfall kommt mit der Importerweiterung
    /// (AP5) samt eigener Pruefkette. Bis dahin ist ein harter Fehler besser als eine
    /// stillschweigend falsche Jahreslaenge.
    /// </para>
    /// </remarks>
    public static class RasterAdapter
    {
        /// <summary>Stundenwerte eines Normaljahres.</summary>
        public const int StundenJahr = 8760;

        /// <summary>Viertelstundenwerte eines Normaljahres.</summary>
        public const int ViertelstundenJahr = StundenJahr * 4;   // 35.040

        /// <summary>
        /// Bringt eine Reihe des Hauptprojekts auf das Viertelstundenraster der Engine.
        /// </summary>
        /// <param name="reihe">
        /// Stundenreihe (8.760 Werte) oder bereits viertelstuendliche Reihe
        /// (35.040 Werte).
        /// </param>
        /// <returns>
        /// Neues Array mit 35.040 Werten. Bei 8.760 Eingangswerten per
        /// Wertwiederholung, bei 35.040 als 1:1-Kopie nach <c>double</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="reihe"/> <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Bei jeder anderen Laenge.</exception>
        public static double[] ZuViertelstundenDouble(float[] reihe)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));

            if (reihe.Length == ViertelstundenJahr)
            {
                double[] kopie = new double[ViertelstundenJahr];
                for (int i = 0; i < ViertelstundenJahr; i++) kopie[i] = reihe[i];
                return kopie;
            }

            if (reihe.Length != StundenJahr)
            {
                throw new ArgumentException(
                    "Die Reihe muss " + StundenJahr + " (stuendlich) oder " + ViertelstundenJahr +
                    " (viertelstuendlich) Werte haben, hat aber " + reihe.Length + ".",
                    nameof(reihe));
            }

            double[] viertel = new double[ViertelstundenJahr];
            for (int i = 0; i < StundenJahr; i++)
            {
                double w = reihe[i];
                int b = i * 4;
                viertel[b] = w;
                viertel[b + 1] = w;
                viertel[b + 2] = w;
                viertel[b + 3] = w;
            }
            return viertel;
        }

        /// <summary>
        /// Rueckweg fuer Charts und CSV-Export: 1:1-Kopie nach <c>float</c>, ohne
        /// jede Rasteraenderung.
        /// </summary>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="reihe"/> <c>null</c> ist.</exception>
        public static float[] ZuFloat(double[] reihe)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));

            float[] ziel = new float[reihe.Length];
            for (int i = 0; i < reihe.Length; i++) ziel[i] = (float)reihe[i];
            return ziel;
        }

        /// <summary>
        /// 1:1-Kopie nach <c>double</c> ohne Laengenpruefung - fuer Reihen, deren
        /// Raster der Aufrufer bereits kennt (z. B. Teilstuecke in Tests).
        /// </summary>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="reihe"/> <c>null</c> ist.</exception>
        public static double[] ZuDouble(float[] reihe)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));

            double[] ziel = new double[reihe.Length];
            for (int i = 0; i < reihe.Length; i++) ziel[i] = reihe[i];
            return ziel;
        }

        /// <summary>
        /// Addiert <paramref name="summand"/> auf <paramref name="ziel"/> (elementweise,
        /// in-place) - der Lastpfad des Hauptprojekts setzt sich aus mehreren Reihen
        /// zusammen (Fachkonzept 3.1: Profil/Ganglinie + WP + Heizstab + Kesselstrom).
        /// </summary>
        /// <exception cref="ArgumentNullException">Bei <c>null</c>-Argumenten.</exception>
        /// <exception cref="ArgumentException">Bei unterschiedlicher Laenge.</exception>
        public static void Addiere(double[] ziel, double[] summand)
        {
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (summand == null) throw new ArgumentNullException(nameof(summand));
            if (ziel.Length != summand.Length)
                throw new ArgumentException("Beide Reihen muessen gleich lang sein.", nameof(summand));

            for (int i = 0; i < ziel.Length; i++) ziel[i] += summand[i];
        }
    }
}
