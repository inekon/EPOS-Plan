using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Reine Rechenlogik des Preis- und Verguetungsmodells (Fachkonzept 4.1/4.2,
    /// Umsetzungskonzept AP4): Jahresprofil aus Monats- und Wochenwerten,
    /// Rasterwechsel Stunde -> Viertelstunde und die additive Aufschlagsrechnung.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was hier steht und was nicht.</b> Die Klasse ist wie die uebrige Engine
    /// UI- und datenbankfrei und nimmt ausschliesslich Zahlen entgegen, keine
    /// Zeichenketten - das Zerlegen der <c>";"</c>-Zeichenketten aus
    /// <c>Tab_Kostenprofil</c> bleibt Sache des Hauptprojekts (Kulturregel:
    /// Datei und Datenbank <c>InvariantCulture</c>, Fachkonzept 8.5). Umgekehrt
    /// steht hier die vollstaendige Kalenderarithmetik, damit sie testbar ist,
    /// ohne dass eine Oberflaeche laeuft.
    /// </para>
    /// <para>
    /// <b>Einheit.</b> Jede Reihe dieser Klasse fuehrt ct/kWh - dieselbe Einheit,
    /// die <see cref="SpeicherEingang.PreisCtKwh"/> erwartet. Die Klasse rechnet
    /// nirgends in Euro um.
    /// </para>
    /// </remarks>
    public static class PreisModell
    {
        /// <summary>Monate eines Jahres - die Laenge des Monatswertvektors.</summary>
        public const int MonateJahr = 12;

        /// <summary>Tage einer Woche.</summary>
        public const int TageWoche = 7;

        /// <summary>Stunden eines Tages.</summary>
        public const int StundenTag = 24;

        /// <summary>Laenge des Wochenwertvektors (7 x 24), Montag 0 Uhr bis Sonntag 23 Uhr.</summary>
        public const int WochenwerteJahr = TageWoche * StundenTag;   // 168

        /// <summary>
        /// Wochentag des 1. Januar im Rechenkalender des Bestands, Montag = 0.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum Sonntag (6) und nicht das laufende Kalenderjahr.</b> Der
        /// Rechenkern richtet die Woche-nach-Jahr-Expansion fest aus: In
        /// <c>BhkwPlan.StromWocheToJahr</c> steht vor der ersten vollen Woche der
        /// Sonntagsblock <c>wo[144..167]</c>, danach folgen 52 volle Wochen
        /// (24 + 52*168 = 8760). Der 1. Januar ist damit ein Sonntag - und zwar in
        /// jedem Lauf, unabhaengig vom Systemdatum. Alle Lastgaenge und
        /// Bedarfsprofile eines Projekts stehen auf dieser Ausrichtung; eine
        /// Preisreihe mit anderer Wochenlage wuerde die Wochenendstunden gegen die
        /// Werktagsstunden der Last verschieben.
        /// </para>
        /// <para>
        /// <c>WaermequelleClass.ProfilAusMonatsUndWochenwerten</c> leitet den
        /// Wochentag dagegen aus dem naechsten Nicht-Schaltjahr ab und liefert
        /// deshalb je nach Systemdatum ein anderes Profil. Fuer eine
        /// Quelltemperatur ist das folgenlos (der Wochenanteil ist dort eine kleine
        /// Abweichung in K), fuer einen Preis waere es ein nicht reproduzierbares
        /// Ergebnis. Diese Klasse uebernimmt deshalb die SEMANTIK des Bestands
        /// (Monatswert + Wochenwert) und die KALENDERAUSRICHTUNG von
        /// <c>StromWocheToJahr</c> - bewusst nicht beides von derselben Stelle.
        /// </para>
        /// </remarks>
        public const int WOCHENTAG_JAHRESANFANG = 6;

        /// <summary>
        /// Tage je Monat im Normaljahr. Der Rechenkern kennt ausschliesslich 8.760
        /// Stunden (CLAUDE.md: feste Feldgroessen), ein Schaltjahr kommt hier nicht vor.
        /// </summary>
        private static readonly int[] TageProMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        // =================================================================
        // Jahresprofil aus Monats- und Wochenwerten (Fachkonzept 4.1 b)
        // =================================================================

        /// <summary>
        /// Baut das Jahresprofil (8.760 Stundenwerte) aus 12 Monatswerten und
        /// 7 x 24 Wochenwerten - mit der Kalenderausrichtung des Rechenkerns
        /// (<see cref="WOCHENTAG_JAHRESANFANG"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Rechenvorschrift</b>, zeichengetreu die des Bestands
        /// (<c>WaermequelleClass.ProfilAusMonatsUndWochenwerten</c>):
        /// <c>p[h] = monat[m] + woche[wochentag * 24 + stunde]</c>. Der Monatswert
        /// traegt das Niveau, der Wochenwert die Abweichung; beide in ct/kWh. Ein
        /// reines Tages- oder HT/NT-Profil ist der Sonderfall "alle sieben Tage
        /// gleich" (Fachkonzept 4.1).
        /// </para>
        /// <para>
        /// <b>Additiv, nicht multiplikativ</b> - anders als die Monatsnormierung in
        /// <c>StromWocheToJahr</c>, die eine Verbrauchsmenge auf einen Monatswert
        /// skaliert. Ein Preis ist keine Menge: Eine Normierung wuerde den
        /// Monatswert zur Jahressumme machen und den eingegebenen ct/kWh-Wert
        /// unkenntlich.
        /// </para>
        /// </remarks>
        /// <param name="monatswerte">12 Monatswerte [ct/kWh].</param>
        /// <param name="wochenwerte">
        /// 168 Wochenwerte [ct/kWh] ab Montag 0 Uhr, oder <c>null</c> fuer "keine
        /// Abweichung" (dann ist das Profil je Monat konstant).
        /// </param>
        /// <returns>Neues Array mit 8.760 Stundenwerten.</returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="monatswerte"/> <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Bei falscher Laenge eines Vektors.</exception>
        public static double[] AusMonatsUndWochenwerten(double[] monatswerte, double[]? wochenwerte)
        {
            return AusMonatsUndWochenwerten(monatswerte, wochenwerte, WOCHENTAG_JAHRESANFANG);
        }

        /// <summary>
        /// Wie <see cref="AusMonatsUndWochenwerten(double[], double[])"/>, aber mit
        /// frei gewaehltem Wochentag des 1. Januar - ausschliesslich fuer Tests, die
        /// die Kalenderlage selbst festlegen muessen.
        /// </summary>
        /// <param name="monatswerte">12 Monatswerte [ct/kWh].</param>
        /// <param name="wochenwerte">168 Wochenwerte [ct/kWh], oder <c>null</c>.</param>
        /// <param name="wochentagJahresanfang">Montag = 0 ... Sonntag = 6.</param>
        /// <exception cref="ArgumentOutOfRangeException">Wenn der Wochentag ausserhalb 0..6 liegt.</exception>
        public static double[] AusMonatsUndWochenwerten(double[] monatswerte, double[]? wochenwerte,
                                                        int wochentagJahresanfang)
        {
            if (monatswerte == null) throw new ArgumentNullException(nameof(monatswerte));
            if (monatswerte.Length != MonateJahr)
                throw new ArgumentException(
                    "Es werden genau " + MonateJahr + " Monatswerte erwartet, uebergeben wurden " +
                    monatswerte.Length + ".", nameof(monatswerte));

            if (wochenwerte != null && wochenwerte.Length != WochenwerteJahr)
                throw new ArgumentException(
                    "Es werden genau " + WochenwerteJahr + " Wochenwerte erwartet, uebergeben wurden " +
                    wochenwerte.Length + ".", nameof(wochenwerte));

            if (wochentagJahresanfang < 0 || wochentagJahresanfang >= TageWoche)
                throw new ArgumentOutOfRangeException(nameof(wochentagJahresanfang),
                    "Der Wochentag des Jahresanfangs muss zwischen 0 (Montag) und 6 (Sonntag) liegen.");

            double[] profil = new double[RasterAdapter.StundenJahr];
            int wochentag = wochentagJahresanfang;
            int index = 0;

            for (int m = 0; m < MonateJahr; m++)
            {
                for (int tag = 0; tag < TageProMonat[m]; tag++)
                {
                    for (int h = 0; h < StundenTag; h++)
                    {
                        double abweichung = wochenwerte != null ? wochenwerte[wochentag * StundenTag + h] : 0.0;
                        profil[index++] = monatswerte[m] + abweichung;
                    }
                    wochentag = (wochentag + 1) % TageWoche;
                }
            }

            return profil;
        }

        // =================================================================
        // Rasterwechsel
        // =================================================================

        /// <summary>
        /// Bringt eine Stundenreihe auf das Viertelstundenraster der Engine -
        /// Wertwiederholung ohne Interpolation, semantisch identisch zu
        /// <see cref="RasterAdapter.ZuViertelstundenDouble"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum eine eigene Ueberladung.</b> Der <see cref="RasterAdapter"/>
        /// nimmt <c>float[]</c> entgegen - er sitzt an der Grenze zum Hauptprojekt,
        /// dessen Zeitreihen <c>float</c> sind. Eine Preisreihe entsteht dagegen
        /// bereits in <c>double</c> (Profilrechnung, CSV-Import) und wuerde ueber
        /// den Umweg <c>double -&gt; float -&gt; double</c> gerundet. Bei Preisen um
        /// 0,001 ct/kWh - die Spotdatei fuehrt genau solche Werte - waere das eine
        /// vermeidbare Ungenauigkeit im Geldwert.
        /// </para>
        /// <para>
        /// Die Rasterkonstanten kommen unveraendert aus dem Adapter; es gibt also
        /// weiterhin genau EINE Wahrheit ueber 8.760 und 35.040.
        /// </para>
        /// </remarks>
        /// <param name="stundenreihe">
        /// 8.760 Stundenwerte, oder bereits 35.040 Viertelstundenwerte (dann eine
        /// 1:1-Kopie).
        /// </param>
        /// <returns>Neues Array mit 35.040 Werten.</returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="stundenreihe"/> <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Bei jeder anderen Laenge.</exception>
        public static double[] ZuViertelstunden(double[] stundenreihe)
        {
            if (stundenreihe == null) throw new ArgumentNullException(nameof(stundenreihe));

            if (stundenreihe.Length == RasterAdapter.ViertelstundenJahr)
            {
                double[] kopie = new double[RasterAdapter.ViertelstundenJahr];
                Array.Copy(stundenreihe, kopie, RasterAdapter.ViertelstundenJahr);
                return kopie;
            }

            if (stundenreihe.Length != RasterAdapter.StundenJahr)
            {
                throw new ArgumentException(
                    "Die Reihe muss " + RasterAdapter.StundenJahr + " (stuendlich) oder " +
                    RasterAdapter.ViertelstundenJahr + " (viertelstuendlich) Werte haben, hat aber " +
                    stundenreihe.Length + ".", nameof(stundenreihe));
            }

            double[] viertel = new double[RasterAdapter.ViertelstundenJahr];
            for (int i = 0; i < RasterAdapter.StundenJahr; i++)
            {
                double w = stundenreihe[i];
                int b = i * 4;
                viertel[b] = w;
                viertel[b + 1] = w;
                viertel[b + 2] = w;
                viertel[b + 3] = w;
            }
            return viertel;
        }

        // =================================================================
        // Aufschlag (Fachkonzept 4.2)
        // =================================================================

        /// <summary>
        /// Addiert einen konstanten Aufschlag [ct/kWh] auf jeden Wert der Reihe und
        /// liefert das Ergebnis als NEUES Array - die Eingangsreihe bleibt
        /// unveraendert.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>p_bezug[i] = p_energie[i] + a</c> (Fachkonzept 4.2). Der Aufschlag
        /// wirkt auch auf negative Spotpreise: Ein Boersenpreis von -1 ct/kWh mit
        /// 11,746 ct/kWh Aufschlag ergibt 10,746 ct/kWh Bezugspreis - der Anwender
        /// zahlt Netzentgelt und Steuern auch dann. Die Reihe wird deshalb
        /// ausdruecklich NICHT bei 0 abgeschnitten.
        /// </para>
        /// <para>
        /// Bewusst kein In-Place-Betrieb: Die Rohreihe (Spotimport, Profil) wird an
        /// mehreren Stellen weiterverwendet - Anzeige, Export, zweiter Lauf mit
        /// anderem Aufschlag. Ein ueberschriebenes Array waere genau die stille
        /// Doppelwirkung, die <c>StdWerte</c> im Altkern vorfuehrt.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="reihe"/> <c>null</c> ist.</exception>
        public static double[] MitAufschlag(double[] reihe, double aufschlagCtKwh)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));

            double[] ziel = new double[reihe.Length];
            for (int i = 0; i < reihe.Length; i++) ziel[i] = reihe[i] + aufschlagCtKwh;
            return ziel;
        }

        /// <summary>
        /// Kleinster und groesster Wert sowie das arithmetische Mittel einer Reihe -
        /// die Kennzahlen des Validierungsprotokolls (Fachkonzept 4.1).
        /// </summary>
        /// <remarks>
        /// Die Summe laeuft ueber <see cref="Numerik.SummeSequenziell(double[])"/>, damit das
        /// Mittel bitgenau dem entspricht, was die Engine an anderer Stelle rechnet.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="reihe"/> <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Bei leerer Reihe.</exception>
        public static void Spannweite(double[] reihe, out double min, out double max, out double mittel)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));
            if (reihe.Length == 0) throw new ArgumentException("Die Reihe ist leer.", nameof(reihe));

            min = reihe[0];
            max = reihe[0];
            for (int i = 1; i < reihe.Length; i++)
            {
                if (reihe[i] < min) min = reihe[i];
                if (reihe[i] > max) max = reihe[i];
            }
            mittel = Numerik.SummeSequenziell(reihe) / reihe.Length;
        }

        /// <summary>Anzahl der negativen Werte einer Reihe (Spotpreise, Fachkonzept 4.1).</summary>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="reihe"/> <c>null</c> ist.</exception>
        public static int AnzahlNegativ(double[] reihe)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));

            int n = 0;
            for (int i = 0; i < reihe.Length; i++) if (reihe[i] < 0.0) n++;
            return n;
        }
    }
}
