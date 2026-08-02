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
    ///  * Datentyp der Vektoren ist float (Single) – wie in der DLL. Zwischenrechnungen laufen
    ///    in double, das Ergebnis wird jeweils auf float zurückgeschrieben, um das FPU-Verhalten
    ///    (80-bit-Zwischenwert, float-Speicherung) möglichst genau nachzubilden.
    ///  * Arrays werden IN-PLACE überschrieben – exakt wie die native Seite (die Rückgabe-int
    ///    wird vom Aufrufer fast überall ignoriert).
    ///  * Die drei Physik-Funktionen geben int zurück (Borland _ftol = Abschneiden Richtung Null,
    ///    entspricht dem C#-(int)-Cast). Der WP-Plan-Aufrufer teilt SpezWaermeverlusteC und
    ///    SolareGewinneC anschließend durch 100 (siehe XML-Doc der jeweiligen Methode).
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

        // ----- Globaler Zustand -----
        // Die DLL hält in 0x4211F8 die "Vortemperatur" des Kapazitätsmodells und nullt sie in
        // DllMain (DLL_PROCESS_ATTACH). TaeglHeizlastWG liest/schreibt diese Variable über die
        // 24-Stunden-Schleife UND über aufeinanderfolgende Tagesaufrufe hinweg. Für bit-nahe
        // Ergebnisse muss dieser Zustand exakt so mitgeführt werden.
        private static float _prevRoomTemp; // Spiegelt DATA:0x4211F8

        /// <summary>Setzt den globalen Zustand zurück (entspricht DllMain/DLL_PROCESS_ATTACH: 0).</summary>
        public static void ResetState() => _prevRoomTemp = 0f;

        // =========================================================================================
        // Gruppe A – Vektor-/Struktur-Primitive (trivial, direkt aus Disassembly)
        // =========================================================================================

        /// <summary>vector_init @0x4163CC – nullt die 8760 Elemente. ret 4.</summary>
        public static int VectorInit(float[] v)
        {
            for (int i = 0; i < Hours; i++) v[i] = 0f;
            return 0;
        }

        /// <summary>
        /// Watt_To_kW @0x41600F – multipliziert jedes der 8760 Elemente mit 0.001 (W→kW). ret 4.
        /// Konstante 0.001 (f80 @0x416033).
        /// </summary>
        public static int WattToKw(float[] v)
        {
            for (int i = 0; i < Hours; i++) v[i] = (float)((double)v[i] * 0.001);
            return 0;
        }

        /// <summary>
        /// vectoren_addieren @0x416362 – ziel[i] += quelle[i] über 8760 Elemente. ret 8.
        /// Native fld [ziel]; fadd [quelle]; fstp [ziel]. Argumentreihenfolge des Wrappers
        /// CSharp_I_vectoren_addieren(Quelle, Ziel): Quelle wird addiert, Ziel modifiziert.
        /// </summary>
        public static long VectorenAddieren(float[] quelle, float[] ziel)
        {
            for (int i = 0; i < Hours; i++) ziel[i] = (float)((double)ziel[i] + quelle[i]);
            return 0;
        }

        /// <summary>
        /// vector_summe @0x41603F – Summe aller 8760 Elemente, danach ×0.001. ret 8.
        /// WICHTIG: Die DLL akkumuliert in einer float-Speicherzelle (jede Addition rundet auf
        /// float). Das wird hier bewusst nachgebildet. Konstante 0.001 (f80 @0x41606F).
        /// </summary>
        public static int VectorSumme(float[] v, ref float summe)
        {
            float acc = 0f; // float-Akkumulator wie im Binär
            for (int i = 0; i < Hours; i++) acc = (float)((double)acc + v[i]);
            summe = (float)((double)acc * 0.001);
            return 0;
        }

        /// <summary>
        /// normieren @0x415FE3 – v[i] = v[i] / maxWert * 100 (Prozent). ret 8.
        /// Konstante 100.0 (f32 @0x41600B).
        /// </summary>
        public static int Normieren(float[] v, float maxWert)
        {
            for (int i = 0; i < Hours; i++) v[i] = (float)((double)v[i] / maxWert * 100.0);
            return 0;
        }

        /// <summary>
        /// netzverlustec @0x4153C0 – addiert den konstanten stündlichen Netzverlust auf alle
        /// 8760 Elemente (Grundlast-Offset). ret 8.
        /// </summary>
        public static int NetzverlusteC(float[] v, float stundlNetzverluste)
        {
            for (int i = 0; i < Hours; i++) v[i] = (float)((double)v[i] + stundlNetzverluste);
            return 0;
        }

        /// <summary>
        /// monats_summe @0x416266 – Summiert Stundenwerte je Monat in sum[12], jeweils ×0.001.
        /// moAnfang/moEnde sind Stundenindizes [0..8759]; die obere Grenze ist INKLUSIVE
        /// (native: while d &lt;= moEnde). Akkumulation in float. Konstante 0.001 (f80 @0x4162AE).
        /// ret 0x10.
        /// </summary>
        public static int MonatsSumme(float[] value, float[] sum, int[] moAnfang, int[] moEnde)
        {
            for (int m = 0; m < Months; m++)
            {
                sum[m] = 0f;
                for (int d = moAnfang[m]; d <= moEnde[m]; d++)
                    sum[m] = (float)(0.001 * value[d] + sum[m]);
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
        public static int Heapsort(float[] src, float[] dst)
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
        /// Phase 1 (Kachelung): out[0..23] = wo[144..167] (Sonntag zuerst → Kalenderausrichtung
        /// 1. Januar), danach 52× wo[0..167] angehängt (24 + 52·168 = 8760).
        /// Phase 2 (Monatsnormierung): pro Monat sum = Σ out[Monat]; out[h] = out[h]/sum ·
        /// monatsverbrauch[m] · 1000. sum in float akkumuliert. Konstante 1000.0 (f32 @0x41635E).
        /// Monatsgrenzen (moAnfang/moEnde) sind Stundenindizes, obere Grenze inklusive.
        /// </summary>
        public static int StromWocheToJahr(float[] wo, float[] monatsverbrauch, float[] outJahr,
                                           int[] moAnfang, int[] moEnde)
        {
            // Phase 1 – Kachelung
            int c = 0;
            for (int h = WeekHours - HoursPerDay; h < WeekHours; h++) // wo[144..167] (Sonntag)
                outJahr[c++] = wo[h];
            for (int week = 1; week <= 52; week++)
                for (int h = 0; h < WeekHours; h++)
                    outJahr[c++] = wo[h];

            // Phase 2 – Monatsnormierung
            for (int m = 0; m < Months; m++)
            {
                float sum = 0f;
                for (int h = moAnfang[m]; h <= moEnde[m]; h++)
                    sum = (float)((double)sum + outJahr[h]);
                for (int h = moAnfang[m]; h <= moEnde[m]; h++)
                    outJahr[h] = (float)((double)outJahr[h] / sum * monatsverbrauch[m] * 1000.0);
            }
            return 0;
        }

        // =========================================================================================
        // Tages→Stunden-Disaggregation (Std-Werte, "nach VDI 2067")
        // =========================================================================================

        /// <summary>
        /// StdWerte @0x4153DF – verteilt 365 Tageslasten über typtag-spezifische 24h-Profile
        /// auf die 8760h-Ganglinie. ret 0x10.
        ///
        /// Ablauf (exakt nach Disassembly):
        ///  1. Maximaler Tagtyp-Index = max(tagTyp[0..364]).
        ///  2. Für jeden Typ t=1..maxTyp: das 24h-Profil tagesgang[(t-1)*24 + h] wird auf
        ///     Tagessumme 1 normiert (IN-PLACE-Nebeneffekt! Das übergebene tagesgang-Array wird
        ///     verändert).
        ///  3. Für jeden Tag d=0..364, Stunde h=0..23:
        ///     waermebedarf[d*24+h] = tageslast[d] · tagesgang[(tagTyp[d]-1)*24 + h] + (bisheriger Wert)
        ///     → additiv auf den vorhandenen Inhalt von waermebedarf.
        /// </summary>
        public static int StdWerte(float[] waermebedarf, int[] tagTyp, float[] tagesgang, float[] tageslast)
        {
            // 1. maximaler Tagtyp
            int maxTyp = 0;
            for (int d = 0; d < Days; d++)
                if (maxTyp < tagTyp[d]) maxTyp = tagTyp[d];

            // 2. Tagesprofile je Typ auf Summe 1 normieren (in-place)
            for (int t = 1; t <= maxTyp; t++)
            {
                float sumcol = 0f;
                int baseIdx = (t - 1) * HoursPerDay;
                for (int h = 0; h < HoursPerDay; h++)
                    sumcol = (float)((double)sumcol + tagesgang[baseIdx + h]);
                for (int h = 0; h < HoursPerDay; h++)
                    tagesgang[baseIdx + h] = (float)((double)tagesgang[baseIdx + h] / sumcol);
            }

            // 3. Verteilung, additiv auf vorhandenen Inhalt
            for (int d = 0; d < Days; d++)
            {
                for (int h = 0; h < HoursPerDay; h++)
                {
                    float basewert = waermebedarf[d * HoursPerDay + h];
                    int profIdx = (tagTyp[d] - 1) * HoursPerDay + h;
                    float val = (float)((double)tageslast[d] * tagesgang[profIdx]);
                    waermebedarf[d * HoursPerDay + h] = (float)((double)val + basewert);
                }
            }
            return 0;
        }

        // =========================================================================================
        // Gruppe B – Physik (Wärmebedarf). Rückgabe int (Borland _ftol = Trunkierung Richtung 0).
        // =========================================================================================

        /// <summary>
        /// SolareGewinneC @0x41526C – nutzbare solare Gewinne eines Tages (×100). ret 0x20.
        /// Ergebnis = ( En·An + ((Eo+Ew)·0.5)·Awo + Es·u_As ) · Transmissionsgrad · 100
        /// Konstanten 0.5 (f32 @0x4152A8), 100.0 (f32 @0x4152AC).
        ///
        /// Bemerkung: Ost- und West-Einstrahlung (Eo, Ew) werden gemittelt und mit EINER
        /// Ost-/West-Fensterfläche (Awo) multipliziert; die West-Fensterfläche existiert nicht
        /// als eigenes Argument. Der WP-Plan-Aufrufer teilt das Ergebnis anschließend durch 100.
        /// </summary>
        public static int SolareGewinneC(float en, float an, float ew, float eo,
                                         float awo, float es, float uAs, float transmissionsgrad)
        {
            double tmp = ((double)eo + ew) * 0.5;
            double s = (double)en * an + tmp * awo + (double)es * uAs;
            s = s * transmissionsgrad * 100.0;
            return (int)s; // _ftol: Trunkierung Richtung Null
        }

        /// <summary>
        /// SpezWaermeverlusteC @0x4152B0 – spezifischer Wärmeverlustkoeffizient (×100). ret 0x50.
        ///
        /// Transmission = 0.83·Kw·Aw + Kf·Af + 0.95·Kd·Ad + 0.45·Kg·Ag + Ks·As
        /// Wärmebrücken = (Kwb1·Lwb1 + Kwb2·Lwb2 + Kwb3·Lwb3) · 0.83
        /// Lüftung      = f · (Wohnflaeche · Raumhoehe) · 1.2 · LWR · 0.277777…
        ///   mit f = (AussenTemp·0.025 + 1.0)  falls AussenTemp &lt; 0, sonst f = 1.0
        /// Ergebnis = (Transmission + Wärmebrücken + Lüftung) · 100
        /// Konstanten: 0.83/0.95/0.45 (f64 @0x415380/0x415388/0x415390), 0.025 (f64 @0x41539C),
        ///   1.0 (f32 @0x4153A4), 1.2 (f64 @0x4153A8), 0.2777777777777778 (f80 @0x4153B0, = 1/3.6·1,
        ///   spez. Wärmekapazität Luft ≈ 0,28 Wh/(kg·K)), 100.0 (f32 @0x4153BC).
        ///
        /// Argumentnamen wie in SimpleObject.cs; "u_As" ist die FLÄCHE sonstiger Bauteile.
        /// Der WP-Plan-Aufrufer teilt das Ergebnis anschließend durch 100.
        /// </summary>
        public static int SpezWaermeverlusteC(
            float kw, float aw, float kf, float af, float kd, float ad, float kg, float ag,
            float ks, float uAs, float kwb1, float lwb1, float kwb2, float lwb2, float kwb3,
            float lwb3, float aussenTemp, float wohnflaeche, float raumhoehe, float lwr)
        {
            double transmission = 0.83 * kw * aw
                                + (double)kf * af
                                + 0.95 * kd * ad
                                + 0.45 * kg * ag
                                + (double)ks * uAs;

            double bruecken = ((double)kwb1 * lwb1 + (double)kwb2 * lwb2 + (double)kwb3 * lwb3) * 0.83;

            double lueftung;
            if (aussenTemp < 0f)
                lueftung = ((double)aussenTemp * 0.025 + 1.0) * wohnflaeche * raumhoehe * 1.2 * lwr * 0.2777777777777778;
            else
                lueftung = (double)wohnflaeche * raumhoehe * 1.2 * lwr * 0.2777777777777778;

            return (int)((transmission + bruecken + lueftung) * 100.0); // _ftol
        }

        /// <summary>
        /// TaeglHeizlastWG @0x4150E0 – tägliche Heizlast eines Wohngebäudes über ein instationäres
        /// 24-Stunden-Kapazitätsmodell (Handbuch Gl. 1.1.12/1.1.13). Rückgabe int (Wh/Tag ·
        /// Gesamtflaeche/Wohnflaeche). ret 0x3C (15 Argumente).
        ///
        /// Zustandsführung: Die "Vortemperatur" wird in einer globalen Variablen (0x4211F8,
        /// hier <see cref="_prevRoomTemp"/>) über Stunden UND Tagesaufrufe hinweg mitgeführt.
        /// Bei day == 1 wird sie mit raumsolltempNacht initialisiert; sonst aus dem globalen Wert
        /// übernommen. Vor dem eigentlichen Jahreslauf ruft WP-Plan die Funktion für Vorlauftage
        /// (350..364) zum Einschwingen auf. Deshalb <see cref="ResetState"/> nur bewusst nutzen.
        ///
        /// Konstanten: 4.0 (f32 @0x41525C – Solar-Faktor für die Tagesstunden), 0.0/1.0/-1.0.
        /// Setpoint-Logik je Stunde h (1..24):
        ///   WE-Absenkung && !Ferien → WETemp; Ferien → FerienTemp;
        ///   sonst 7 &lt;= h &lt;= 22 → Tag-Sollwert; sonst Nacht-Sollwert.
        /// Solargewinn wirkt nur in den Stunden 9..14 (mit Faktor 4.0).
        /// </summary>
        public static int TaeglHeizlastWG(
            int day, int weAbsenkung, float weTemp, int ferienAbsenkung, float ferienTemp,
            float raumsolltempTag, float raumsolltempNacht, float innereGewinne, float solareGewinne,
            float spezWaermeverluste, float gebaeudeKapazitaet, float aussenTemp, float maxRaumtemp,
            float gesamtflaeche, float wohnflaeche)
        {
            double L = spezWaermeverluste;
            double C = gebaeudeKapazitaet;

            double acc = 0.0;                              // ebp-0x10: Summe der Stunden-Heizlast
            double tPrev = (day == 1) ? raumsolltempNacht  // ebp-0xc: Vortemperatur
                                      : _prevRoomTemp;

            for (int h = 1; h <= 24; h++)
            {
                // --- Sollwert der Stunde (ebp-4) ---
                double tSoll;
                if (weAbsenkung != 0 && ferienAbsenkung == 0) tSoll = weTemp;
                else if (ferienAbsenkung != 0) tSoll = ferienTemp;
                else if (h >= 7 && h <= 22) tSoll = raumsolltempTag;
                else tSoll = raumsolltempNacht;

                // --- Heizleistung dieser Stunde (ebp-8) ---
                double pHzg;
                if (tSoll < tPrev)
                {
                    pHzg = 0.0; // Raum wärmer als Sollwert → keine Heizung
                }
                else
                {
                    pHzg = (tPrev - aussenTemp) * L + (tSoll - tPrev) * C - innereGewinne;
                    if (h > 8 && h < 15)           // Stunden 9..14: solare Entlastung
                        pHzg = pHzg - 4.0 * solareGewinne;
                    if (pHzg < 0.0) pHzg = 0.0;    // keine negative Heizlast
                    acc += pHzg;
                }

                // --- Fortschreibung der Raumtemperatur (RC-Modell) ---
                double a = 1.0 - Math.Exp(-L / C);            // 1 - exp(-L/C)
                int solarFlag = (h > 8 && h < 15) ? 1 : 0;    // Solar nur tagsüber
                double pGesTerm = a * (4.0 * solareGewinne * solarFlag + L * aussenTemp + innereGewinne + pHzg) / L;
                tPrev = Math.Exp(-L / C) * tPrev + pGesTerm;
                if (tPrev > maxRaumtemp) tPrev = maxRaumtemp; // Kappung auf Maximaltemperatur
            }

            _prevRoomTemp = (float)tPrev; // globalen Zustand fortschreiben (0x4211F8)

            return (int)(acc * gesamtflaeche / wohnflaeche); // _ftol
        }
    }
}
