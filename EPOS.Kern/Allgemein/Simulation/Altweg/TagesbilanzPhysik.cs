using System;

namespace WindowsFormsApplication1.Altweg
{
    /// <summary>
    /// <b>Die vier Physikfunktionen des Tagesbilanz-Wegs</b> — Stundenverteilung
    /// (<see cref="StdWerte"/>), solare Gewinne, spezifischer Wärmeverlust und tägliche
    /// Heizlast des Kapazitätsmodells. Sie stammen aus dem verwalteten Port des nativen
    /// Rechenkerns <c>BHKWPLAN.DLL</c> (<c>WPPlan.Core.BhkwPlan</c>) und sind von dort
    /// <b>Zeichen für Zeichen</b> in das Modul <c>Altweg/</c> verschoben (Stufe G1.0,
    /// Entscheid E20, ADR-006). Die neun Vektorhelfer bleiben in <c>BhkwPlan</c>, weil jeder
    /// Bedarfs- und Erzeugerzweig sie ruft.
    ///
    /// <para><b>Eingefroren.</b> Der Tagesbilanz-Weg ist der Bestandsweg des Übergangs; er
    /// bekommt keine neue Funktion und fällt mit der Stufe GA (Umsetzungskonzept
    /// Gebäudesimulation, Löschliste 6.1). Die Rückgabe ist <c>double</c> und schneidet
    /// nicht ab (Anwenderentscheid W8-O-5d-Q2); Nachweis <c>BhkwPlanRueckgabeTests</c>.</para>
    ///
    /// <para>Die RVA der Originalfunktion und ihre Konstanten nennt jede Methode selbst.</para>
    /// </summary>
    public static class TagesbilanzPhysik
    {
        private const int Days = 365;         // 0x16D
        private const int HoursPerDay = 24;   // 0x18

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
        public static int StdWerte(double[] waermebedarf, int[] tagTyp, double[] tagesgang, double[] tageslast)
        {
            // 1. maximaler Tagtyp
            int maxTyp = 0;
            for (int d = 0; d < Days; d++)
                if (maxTyp < tagTyp[d]) maxTyp = tagTyp[d];

            // 2. Tagesprofile je Typ auf Summe 1 normieren (in-place)
            for (int t = 1; t <= maxTyp; t++)
            {
                double sumcol = 0.0;
                int baseIdx = (t - 1) * HoursPerDay;
                for (int h = 0; h < HoursPerDay; h++)
                    sumcol = sumcol + tagesgang[baseIdx + h];
                for (int h = 0; h < HoursPerDay; h++)
                    tagesgang[baseIdx + h] = tagesgang[baseIdx + h] / sumcol;
            }

            // 3. Verteilung, additiv auf vorhandenen Inhalt
            for (int d = 0; d < Days; d++)
            {
                for (int h = 0; h < HoursPerDay; h++)
                {
                    double basewert = waermebedarf[d * HoursPerDay + h];
                    int profIdx = (tagTyp[d] - 1) * HoursPerDay + h;
                    double val = tageslast[d] * tagesgang[profIdx];
                    waermebedarf[d * HoursPerDay + h] = val + basewert;
                }
            }
            return 0;
        }

        // =========================================================================================
        // Gruppe B – Physik (Wärmebedarf). Rückgabe double: die Trunkierung der DLL
        // (Borland _ftol, Richtung 0) ist mit W8-O-5d-Q2 gefallen.
        // =========================================================================================

        /// <summary>
        /// SolareGewinneC @0x41526C – nutzbare solare Gewinne eines Tages (×100). ret 0x20.
        /// Ergebnis = ( En·An + ((Eo+Ew)·0.5)·Awo + Es·u_As ) · Transmissionsgrad · 100
        /// Konstanten 0.5 (f32 @0x4152A8), 100.0 (f32 @0x4152AC).
        ///
        /// Bemerkung: Ost- und West-Einstrahlung (Eo, Ew) werden gemittelt und mit EINER
        /// Ost-/West-Fensterfläche (Awo) multipliziert; die West-Fensterfläche existiert nicht
        /// als eigenes Argument. Der WP-Plan-Aufrufer teilt das Ergebnis anschließend durch 100.
        ///
        /// <para><b>Rückgabe double seit W8-O-5d-Q2</b> (07.09.2026, „keine Treue zur alten
        /// DLL"). Die DLL schnitt hier mit <c>_ftol</c> auf eine ganze Zahl ab; nach der
        /// Division durch 100 beim Aufrufer war das ein Raster von 0,01 W auf den solaren
        /// Gewinnen eines Tages. Der Faktor 100 selbst bleibt stehen — er gehört zur
        /// Schnittstelle, die der Aufrufer bedient.</para>
        /// </summary>
        public static double SolareGewinneC(double en, double an, double ew, double eo,
                                            double awo, double es, double uAs, double transmissionsgrad)
        {
            double tmp = ((double)eo + ew) * 0.5;
            double s = (double)en * an + tmp * awo + (double)es * uAs;
            s = s * transmissionsgrad * 100.0;
            return s; // W8-O-5d-Q2: kein _ftol mehr
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
        ///
        /// <para><b>Rückgabe double seit W8-O-5d-Q2</b> (07.09.2026). Die Trunkierung der DLL
        /// rasterte den Wärmeverlustkoeffizienten auf 0,01 W/K — und weil er in der
        /// Tagesheizlast mit der Temperaturdifferenz multipliziert wird, wuchs das Raster dort
        /// auf ein Vielfaches an. <b>Der Aufrufer folgt mit:</b>
        /// <c>SimulationWaermebedarf</c> teilte das int-Ergebnis mit <c>/ 100</c>, also
        /// GANZZAHLIG — zwei Abschneidungen hintereinander. Beide sind gefallen.</para>
        /// </summary>
        public static double SpezWaermeverlusteC(
            double kw, double aw, double kf, double af, double kd, double ad, double kg, double ag,
            double ks, double uAs, double kwb1, double lwb1, double kwb2, double lwb2, double kwb3,
            double lwb3, double aussenTemp, double wohnflaeche, double raumhoehe, double lwr)
        {
            double transmission = 0.83 * kw * aw
                                + (double)kf * af
                                + 0.95 * kd * ad
                                + 0.45 * kg * ag
                                + (double)ks * uAs;

            double bruecken = ((double)kwb1 * lwb1 + (double)kwb2 * lwb2 + (double)kwb3 * lwb3) * 0.83;

            double lueftung;
            if (aussenTemp < 0.0)
                lueftung = ((double)aussenTemp * 0.025 + 1.0) * wohnflaeche * raumhoehe * 1.2 * lwr * 0.2777777777777778;
            else
                lueftung = (double)wohnflaeche * raumhoehe * 1.2 * lwr * 0.2777777777777778;

            return (transmission + bruecken + lueftung) * 100.0; // W8-O-5d-Q2: kein _ftol mehr
        }

        /// <summary>
        /// TaeglHeizlastWG @0x4150E0 – tägliche Heizlast eines Wohngebäudes über ein instationäres
        /// 24-Stunden-Kapazitätsmodell (Handbuch Gl. 1.1.12/1.1.13). Rückgabe double (Wh/Tag ·
        /// Gesamtflaeche/Wohnflaeche). ret 0x3C (15 Argumente).
        ///
        /// <para><b>Rückgabe double seit W8-O-5d-Q2</b> (07.09.2026, „keine Treue zur alten
        /// DLL"). Die DLL schnitt die Tagesheizlast auf ganze Wh ab. Das klingt klein, war es
        /// aber nicht: Die Tagessumme geht über das Tagesprofil in 24 Stundenwerte, und die
        /// Schwellen des Modells (Speicherhysterese, Volllastgrenze des BHKW) tragen eine
        /// Verschiebung über Stunden weiter. In Projekt 1041 verschob eine Stelle hinter dem
        /// Komma die Tagesheizlast eines Januartags um 0,39 %.</para>
        ///
        /// Zustandsführung: Die "Vortemperatur" (in der DLL die globale Variable 0x4211F8) wird
        /// über Stunden UND Tagesaufrufe hinweg in <paramref name="zustand"/> mitgeführt — einem
        /// Instanzzustand des Aufrufers, nicht einem statischen Feld. Bei day == 1 wird sie mit
        /// raumsolltempNacht initialisiert; sonst aus dem Zustand übernommen. Vor dem eigentlichen
        /// Jahreslauf ruft WP-Plan die Funktion für Vorlauftage (350..364) zum Einschwingen auf;
        /// der Aufrufer setzt den Zustand je Gebäude über
        /// <see cref="Tagesbilanzzustand.ResetState"/> zurück.
        ///
        /// Konstanten: 4.0 (f32 @0x41525C – Solar-Faktor für die Tagesstunden), 0.0/1.0/-1.0.
        /// Setpoint-Logik je Stunde h (1..24):
        ///   WE-Absenkung && !Ferien → WETemp; Ferien → FerienTemp;
        ///   sonst 7 &lt;= h &lt;= 22 → Tag-Sollwert; sonst Nacht-Sollwert.
        /// Solargewinn wirkt nur in den Stunden 9..14 (mit Faktor 4.0).
        /// </summary>
        public static double TaeglHeizlastWG(
            Tagesbilanzzustand zustand,
            int day, int weAbsenkung, double weTemp, int ferienAbsenkung, double ferienTemp,
            double raumsolltempTag, double raumsolltempNacht, double innereGewinne, double solareGewinne,
            double spezWaermeverluste, double gebaeudeKapazitaet, double aussenTemp, double maxRaumtemp,
            double gesamtflaeche, double wohnflaeche)
        {
            double L = spezWaermeverluste;
            double C = gebaeudeKapazitaet;

            double acc = 0.0;                              // ebp-0x10: Summe der Stunden-Heizlast
            if (zustand == null) throw new ArgumentNullException(nameof(zustand));

            double tPrev = (day == 1) ? raumsolltempNacht  // ebp-0xc: Vortemperatur
                                      : zustand.Vortemperatur;

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

            zustand.Vortemperatur = tPrev; // Zustand fortschreiben (in der DLL: 0x4211F8)

            return acc * gesamtflaeche / wohnflaeche; // W8-O-5d-Q2: kein _ftol mehr
        }
    }
}
