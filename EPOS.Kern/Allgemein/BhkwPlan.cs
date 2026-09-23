using System;

namespace WPPlan.Core
{
    /// <summary>
    /// Verwalteter C#-Port des nativen Rechenkerns <c>BHKWPLAN.DLL</c> (Borland C, x86, __stdcall).
    ///
    /// Portiert wurden ausschließlich die 15 Funktionen, die WP-Plan tatsächlich über
    /// <c>[DllImport("bhkwplan.dll")]</c> (CSExeCOMServer\SimpleObject.cs) aufruft. Die im
    /// Reverse-Engineering-Dossier zusätzlich gefundenen BHKW-/Kessel-/Strommarkt-Funktionen
    /// (bhkw_sys_*, heizkessel_betrieb, eigennutz, rest_strombezug, verguetungsstunden_c,
    /// tarifcodes_c, strom_ht_nt) sind in diesem WP-Plan-Zweig NICHT eingebunden – ihre Logik
    /// liegt bereits nativ in C# (SimulationSPK/SimulationPV/SimulationControl) vor und ist für
    /// den Port irrelevant.
    ///
    /// Treue-Prinzipien:
    ///  * Feldgrößen fest wie im Binär: 8760 (Jahresstunden), 168 (Wochenstunden),
    ///    365 (Tage), 12 (Monate), 24 (Tagesstunden).
    ///  * Datentyp der Vektoren ist seit dem Anwenderentscheid W8‑O‑5d (07.09.2026) durchgehend
    ///    <c>double</c>. Die DLL führte die Vektoren in <c>float</c> (Single) und rundete nach
    ///    jeder Rechnung auf die Speicherzelle zurück; dieses FPU-Verhalten wurde bis dahin
    ///    nachgebildet. Es ist bewusst aufgegeben: der ganze Rechenweg rechnet und speichert in
    ///    <c>double</c>, Zwischenwert und Speicherzelle haben dieselbe Breite.
    ///  * Arrays werden IN-PLACE überschrieben – exakt wie die native Seite (die Rückgabe-int
    ///    wird vom Aufrufer fast überall ignoriert).
    ///  * Die vier Funktionen des Tagesbilanz-Wegs (StdWerte und die drei Physik-Funktionen)
    ///    stehen nicht mehr hier, sondern in
    ///    <c>WindowsFormsApplication1.Altweg.TagesbilanzPhysik</c> (Stufe G1.0 der
    ///    Gebäudesimulation, E20).
    ///
    /// Jede Methode nennt in der Doku die RVA der Originalfunktion und die belegten Konstanten.
    /// Vor produktivem Einsatz gegen die Original-DLL golden-mastern (siehe README).
    /// </summary>
    public static class BhkwPlan
    {
        public const int Hours = 8760;       // 0x2238
        public const int WeekHours = 168;    // 0xA8
        public const int Days = 365;         // 0x16D
        public const int Months = 12;        // 0xC
        public const int HoursPerDay = 24;   // 0x18

        // ----- Zustand -----
        // Diese Klasse ist zustandslos. Die Physikfunktionen des Tagesbilanz-Wegs (StdWerte,
        // SolareGewinneC, SpezWaermeverlusteC, TaeglHeizlastWG) samt der Vortemperatur des
        // Kapazitätsmodells stehen im Modul Simulation/Altweg (Stufe G1.0, E20); hier bleiben
        // die Vektorhelfer, die jeder Bedarfs- und Erzeugerzweig ruft.

        // =========================================================================================
        // Gruppe A – Vektor-/Struktur-Primitive (trivial, direkt aus Disassembly)
        // =========================================================================================

        /// <summary>vector_init @0x4163CC – nullt die 8760 Elemente. ret 4.</summary>
        public static int VectorInit(double[] v)
        {
            for (int i = 0; i < Hours; i++) v[i] = 0.0;
            return 0;
        }

        /// <summary>
        /// Watt_To_kW @0x41600F – multipliziert jedes der 8760 Elemente mit 0.001 (W→kW). ret 4.
        /// Konstante 0.001 (f80 @0x416033).
        /// </summary>
        public static int WattToKw(double[] v)
        {
            for (int i = 0; i < Hours; i++) v[i] = v[i] * 0.001;
            return 0;
        }

        /// <summary>
        /// vectoren_addieren @0x416362 – ziel[i] += quelle[i] über 8760 Elemente. ret 8.
        /// Native fld [ziel]; fadd [quelle]; fstp [ziel]. Argumentreihenfolge des Wrappers
        /// CSharp_I_vectoren_addieren(Quelle, Ziel): Quelle wird addiert, Ziel modifiziert.
        /// </summary>
        public static long VectorenAddieren(double[] quelle, double[] ziel)
        {
            for (int i = 0; i < Hours; i++) ziel[i] = ziel[i] + quelle[i];
            return 0;
        }

        /// <summary>
        /// vector_summe @0x41603F – Summe aller 8760 Elemente, danach ×0.001. ret 8.
        /// Die DLL akkumulierte in einer float-Speicherzelle (jede Addition rundete auf float);
        /// seit W8‑O‑5d läuft die Akkumulation in <c>double</c>. Konstante 0.001 (f80 @0x41606F).
        /// </summary>
        public static int VectorSumme(double[] v, ref double summe)
        {
            double acc = 0.0;
            for (int i = 0; i < Hours; i++) acc = acc + v[i];
            summe = acc * 0.001;
            return 0;
        }

        /// <summary>
        /// normieren @0x415FE3 – v[i] = v[i] / maxWert * 100 (Prozent). ret 8.
        /// Konstante 100.0 (f32 @0x41600B).
        /// </summary>
        public static int Normieren(double[] v, double maxWert)
        {
            for (int i = 0; i < Hours; i++) v[i] = v[i] / maxWert * 100.0;
            return 0;
        }

        /// <summary>
        /// netzverlustec @0x4153C0 – addiert den konstanten stündlichen Netzverlust auf alle
        /// 8760 Elemente (Grundlast-Offset). ret 8.
        /// </summary>
        public static int NetzverlusteC(double[] v, double stundlNetzverluste)
        {
            for (int i = 0; i < Hours; i++) v[i] = v[i] + stundlNetzverluste;
            return 0;
        }

        /// <summary>
        /// monats_summe @0x416266 – Summiert Stundenwerte je Monat in sum[12], jeweils ×0.001.
        /// moAnfang/moEnde sind Stundenindizes [0..8759]; die obere Grenze ist INKLUSIVE
        /// (native: while d &lt;= moEnde). Akkumulation in double. Konstante 0.001 (f80 @0x4162AE).
        /// ret 0x10.
        /// </summary>
        public static int MonatsSumme(double[] value, double[] sum, int[] moAnfang, int[] moEnde)
        {
            for (int m = 0; m < Months; m++)
            {
                sum[m] = 0.0;
                for (int d = moAnfang[m]; d <= moEnde[m]; d++)
                    sum[m] = 0.001 * value[d] + sum[m];
            }
            return 0;
        }

        /// <summary>
        /// monats_grenzen @0x4161B3 – schreibt die Stunden-Monatsgrenzen eines NICHT-Schaltjahres.
        /// (In WP-Plan importiert, aber nicht aufgerufen – die App berechnet die Grenzen selbst.
        /// Hier aus Vollständigkeit/Referenz enthalten.) ret 8.
        /// </summary>
        public static int MonatsGrenzen(int[] anfang, int[] ende)
        {
        int[] a = { 0, 744, 1416, 2160, 2880, 3624, 4344, 5088, 5832, 6552, 7296, 8016 };
        int[] e = { 743, 1415, 2159, 2879, 3623, 4343, 5087, 5831, 6551, 7295, 8015, 8759 };
            for (int m = 0; m < Months; m++) { anfang[m] = a[m]; ende[m] = e[m]; }
            return 0;
        }

        // =========================================================================================
        // Jahresdauerlinie
        // =========================================================================================

        /// <summary>
        /// heapsort @0x414FF0 – kopiert src[8760] → dst und sortiert dst AUFSTEIGEND
        /// (internes Heapsort @0x415035, Numerical-Recipes-Stil, 1-basiert). Rückgabe 0 = OK.
        /// Der WP-Plan-Aufrufer führt anschließend Array.Reverse(dst) aus → absteigende
        /// Jahresdauerlinie. ret 8.
        /// </summary>
        public static int Heapsort(double[] src, double[] dst)
        {
            for (int i = 0; i < Hours; i++) dst[i] = src[i];
            Array.Sort(dst); // aufsteigend – identische Ordnung wie das native Heapsort
            return 0;
        }

        // =========================================================================================
        // Woche→Jahr-Expansion (Strom, Prozesswärme, Brauchwasser)
        // =========================================================================================

        /// <summary>
        /// strom_wochetojahr @0x4162BA – expandiert ein 168h-Wochenprofil auf 8760h und
        /// normiert je Monat auf die 12 Monatsverbräuche (×1000, kWh→Wh). ret 0x14.
        ///
        /// ALTKONVENTION, unverändert: Das Jahr beginnt mit einem SONNTAG. Diese Fassung
        /// delegiert deshalb mit <c>wochentagJan1 = 6</c> und ist Anweisung für Anweisung
        /// das bisherige Verhalten — für jeden Aufrufer außerhalb der Bedarfsrechnung
        /// bleibt das Ergebnis bitgleich.
        ///
        /// Phase 1 (Kachelung): out[0..23] = wo[144..167] (Sonntag zuerst → Kalenderausrichtung
        /// 1. Januar), danach 52× wo[0..167] angehängt (24 + 52·168 = 8760).
        /// Phase 2 (Monatsnormierung): pro Monat sum = Σ out[Monat]; out[h] = out[h]/sum ·
        /// monatsverbrauch[m] · 1000. sum in double akkumuliert. Konstante 1000.0 (f32 @0x41635E).
        /// Monatsgrenzen (moAnfang/moEnde) sind Stundenindizes, obere Grenze inklusive.
        /// </summary>
        public static int StromWocheToJahr(double[] wo, double[] monatsverbrauch, double[] outJahr,
                                           int[] moAnfang, int[] moEnde)
        {
            return StromWocheToJahr(wo, monatsverbrauch, outJahr, moAnfang, moEnde, 6);
        }

        /// <summary>
        /// Dieselbe Expansion mit FREIEM KALENDERSTART (Konzept-Entscheidung F3,
        /// Paket K1): Das 168-Stunden-Wochenprofil wird ab dem tatsächlichen Wochentag
        /// des 1. Januar gekachelt statt fest ab Sonntag.
        ///
        /// HERKUNFT DER ALTKONVENTION. Die native DLL kachelte hart „Sonntag zuerst":
        /// 24 Stunden aus <c>wo[144..167]</c>, danach 52 volle Wochen. Damit fiel der
        /// Profilpfad (Strom, Prozesswärme, Brauchwasser) mit keinem der beiden anderen
        /// Kalender des Programms zusammen — weder mit dem Gebäudepfad
        /// (<c>Tab_Klimadaten.WE</c>) noch mit dem WP-Quellprofil
        /// (<c>WaermequelleClass.cs:934-938</c>, „nächstes Nicht-Schaltjahr"). F3 zieht
        /// alle drei auf den Klimadaten-Kalender zusammen.
        ///
        /// KONVENTION: <c>wochentagJan1</c> zählt <b>Montag = 0 … Sonntag = 6</b> —
        /// identisch mit der Umrechnung in <c>WaermequelleClass</c>
        /// (<c>((int)DayOfWeek + 6) % 7</c>). Das Wochenprofil ist entsprechend gelesen:
        /// <c>wo[0..23]</c> ist Montag, <c>wo[144..167]</c> ist Sonntag.
        ///
        /// Phase 1 wird damit zur MODULO-KACHELUNG:
        /// <code>outJahr[h] = wo[((wochentagJan1 · 24) + h) mod 168]</code>
        /// Für <c>wochentagJan1 = 6</c> ist das nachweislich EXAKT die bisherige Sequenz
        /// (<c>wo[144..167]</c>, danach 52 × <c>wo[0..167]</c>), denn 8760 = 24 + 52·168.
        ///
        /// Phase 2 (Monatsnormierung) bleibt unverändert. Die Kalenderumstellung ist
        /// deshalb ENERGIEWIRKUNGSFREI je Monat: Sie verschiebt allein die
        /// Stundenverteilung innerhalb des Monats, nicht die Monatsmenge.
        /// </summary>
        /// <param name="wochentagJan1">Wochentag des 1. Januar, Montag = 0 … Sonntag = 6.</param>
        public static int StromWocheToJahr(double[] wo, double[] monatsverbrauch, double[] outJahr,
                                           int[] moAnfang, int[] moEnde, int wochentagJan1)
        {
            // Phase 1 – Kachelung ab dem Wochentag des 1. Januar. Der Modulo auf 7 faengt
            // einen Fremdwert ab, ohne zu werfen: Der Rechenkern bricht nirgends ab.
            int start = (((wochentagJan1 % 7) + 7) % 7) * HoursPerDay;
            for (int h = 0; h < Hours; h++)
                outJahr[h] = wo[(start + h) % WeekHours];

            // Phase 2 – Monatsnormierung
            for (int m = 0; m < Months; m++)
            {
                double sum = 0.0;
                for (int h = moAnfang[m]; h <= moEnde[m]; h++)
                    sum = sum + outJahr[h];
                for (int h = moAnfang[m]; h <= moEnde[m]; h++)
                    outJahr[h] = outJahr[h] / sum * monatsverbrauch[m] * 1000.0;
            }
            return 0;
        }
    }
}
