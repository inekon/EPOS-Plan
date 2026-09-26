using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die <b>Festwerte des Gebäudemodells</b> (Stufe G1; Rechenschritte 1.3, Vorgaben bei
    /// NULL aus Rechenschritte 1.1). Sie stehen im Quelltext, nicht in der Datenbank, und sind
    /// Teil des Rechenwegs; jede Zahl trägt ihre Herkunft. Eine Zahl, die hier nicht steht,
    /// setzt das Modell nicht — auch nicht als Rückfall.
    /// </summary>
    internal static class GebaeudeFestwerte
    {
        // ---- Übergänge und Klassenweg (Rechenschritte 1.3, Schritt A) ------------------

        /// <summary>Innerer Strahlungsübergang α_str,i [W/(m²K)] — Gl. (30).</summary>
        internal const double ALPHA_STR_INNEN = 5.0;

        /// <summary>Innerer konvektiver Übergang der Wände α_kon,i [W/(m²K)] — Testräume der Richtlinie.</summary>
        internal const double ALPHA_KON_INNEN = 2.7;

        /// <summary>Konvektiver Anteil des äußeren Übergangs α_kon,A [W/(m²K)] (Rechenschritte E5).</summary>
        internal const double ALPHA_KON_AUSSEN = 20.0;

        /// <summary>
        /// Strahlungsanteil des äußeren Übergangs α_str,A [W/(m²K)], wenn die Gegenstrahlung
        /// fehlt (NULL-Regel E5, Rechenschritte 1.2) — und in Stufe G1 stets.
        /// </summary>
        internal const double ALPHA_STR_AUSSEN_RUECKFALL = 5.0;

        /// <summary>Äußerer Übergang gesamt α_A = α_kon,A + α_str,A [W/(m²K)] — Gl. (38), benannte Festlegung.</summary>
        internal const double ALPHA_AUSSEN = ALPHA_KON_AUSSEN + ALPHA_STR_AUSSEN_RUECKFALL;

        /// <summary>Massen-Oberflächen-Koeffizient h_ms [W/(m²K)] — EPOS-Klassenweg.</summary>
        internal const double H_MS = 9.1;

        /// <summary>Innerer Wärmeübergangswiderstand R_si [m²K/W], im U-Wert enthalten.</summary>
        internal const double R_SI = 0.13;

        /// <summary>Wärmekapazität der Luft c·ρ [Wh/(m³K)] — für die Lüftung der Zone und, derselbe Wert, für den Luftaustausch zwischen Zonen (Mehrzonenkonzept 2.7).</summary>
        internal const double C_RHO_LUFT = 0.34;

        /// <summary>
        /// Der nachbarseitige Übergang einer Trennfläche (Stufe G6b, Messentscheid A7 an Testbeispiel 10;
        /// <see cref="Nachbaruebergang"/>).
        /// </summary>
        internal const Nachbaruebergang NACHBARUEBERGANG = Nachbaruebergang.WieUnbeheizt;

        /// <summary>
        /// Die Toleranz der Zusicherung Σ B_v = 1 (Gl. (42), Mehrzonenkonzept 2.3) [–]: Rundung von
        /// höchstens einigen Dutzend Quotienten.
        /// </summary>
        internal const double GEWICHTE_SUMME_TOLERANZ = 1e-12;

        /// <summary>Umrechnung Wh → J.</summary>
        internal const double SEKUNDEN_JE_STUNDE = 3600.0;

        // ---- Klimaweg (Rechenschritte 1.3, Schritt E) ----------------------------------

        /// <summary>Winkelkorrektur Fenster F_W [–] — EPOS-Vorgabe.</summary>
        internal const double F_W = 0.9;

        /// <summary>Konvektiver Anteil des Fenstersolars a_kon [–].</summary>
        internal const double A_KON_SOLAR = 0.09;

        /// <summary>Anteil der inneren Lasten, der konvektiv an die Luft geht [–] (E4: 0,5/0,5).</summary>
        internal const double ANTEIL_INNERE_LASTEN_KONVEKTIV = 0.5;

        /// <summary>Tiefe der Erdreichtemperatur z [m] — EPOS-Ergänzung.</summary>
        internal const double ERDREICH_TIEFE_M = 1.0;

        /// <summary>Temperaturleitfähigkeit des Erdreichs α_Erd [m²/d] (E6).</summary>
        internal const double ERDREICH_TEMPERATURLEITFAEHIGKEIT_M2D = 0.06;

        /// <summary>Vorlauf des Jahreslaufs [d] (Konzept 4.6, Rechenschritte 7.2).</summary>
        internal const int VORLAUF_TAGE = 30;

        // ---- Plausibilitätsgrenzen (Konzept 4.8) ---------------------------------------

        /// <summary>Untergrenze Bauweise je Nutzfläche [Wh/(m²K)].</summary>
        internal const double BAUWEISE_JE_M2_MIN = 5.0;

        /// <summary>Obergrenze Bauweise je Nutzfläche [Wh/(m²K)].</summary>
        internal const double BAUWEISE_JE_M2_MAX = 200.0;

        /// <summary>Untergrenze eines U-Werts [W/(m²K)].</summary>
        internal const double U_MIN = 0.1;

        /// <summary>Obergrenze eines U-Werts [W/(m²K)].</summary>
        internal const double U_MAX = 6.0;

        /// <summary>Toleranz der Summe der Fensterflächen gegen die gesamte Fensterfläche [m²].</summary>
        internal const double FENSTERSUMME_TOLERANZ_M2 = 0.01;

        // ---- Vorgaben bei NULL (Rechenschritte 1.1) -------------------------------------

        /// <summary>Rahmenanteil 1 − F_F bei NULL.</summary>
        internal const double VORGABE_RAHMENANTEIL = 0.3;

        /// <summary>Verschattungsfaktor F_S bei NULL.</summary>
        internal const double VORGABE_VERSCHATTUNGSFAKTOR = 0.9;

        /// <summary>Kellertemperatur θ_NR [°C] bei NULL.</summary>
        internal const double VORGABE_KELLERTEMPERATUR = 10.0;

        /// <summary>Masseanteil der Außenbauteile a_AW bei NULL.</summary>
        internal const double VORGABE_MASSEANTEIL_AUSSEN = 0.3;

        /// <summary>Innenflächenfaktor f_IW bei NULL.</summary>
        internal const double VORGABE_INNENFLAECHENFAKTOR = 2.5;

        /// <summary>Strahlungsanteil der Heizübergabe a_str,H bei NULL.</summary>
        internal const double VORGABE_HEIZUNG_STRAHLUNGSANTEIL = 0.3;

        /// <summary>Stunden des Tagsollwerts, 1-basiert (E8): erste Stunde.</summary>
        internal const int TAG_ERSTE_STUNDE = 7;

        /// <summary>Stunden des Tagsollwerts, 1-basiert (E8): letzte Stunde.</summary>
        internal const int TAG_LETZTE_STUNDE = 22;

        /// <summary>Der Wochenendsollwert wirkt nur über diesem Wert [°C] (E8).</summary>
        internal const double WOCHENENDE_SOLLWERT_SCHWELLE = 5.0;

        /// <summary>Der Ferienfahrplan ist aktiv, wenn das Flag über diesem Wert liegt (E8).</summary>
        internal const double FERIEN_FLAG_SCHWELLE = 0.9;

        /// <summary>… und der Feriensollwert mindestens diesen Wert trägt [°C] (E8).</summary>
        internal const double FERIEN_SOLLWERT_MIN = 1.0;

        // ---- Lüftung (Stufe G2; Rechenschritte A7, 7.2; Softwarearchitektur 2.8) ------

        /// <summary>Infiltration n_inf [1/h] bei NULL (Rechenschritte 1.1).</summary>
        internal const double VORGABE_LUFTWECHSEL_INFILTRATION = 0.3;

        /// <summary>Nutzerlüftung n_nutz [1/h] bei NULL (Rechenschritte 1.1).</summary>
        internal const double VORGABE_LUFTWECHSEL_NUTZER = 0.4;

        /// <summary>Luftwechsel der Sommerlüftung [1/h] — der Luftwechsel steigt in dieser Stunde auf diesen Wert (Konzept 4.4).</summary>
        internal const double SOMMERLUEFTUNG_LUFTWECHSEL = 2.0;

        /// <summary>Einschaltschwelle der Raumluft [°C] (bis KU1 fest; Rechenschritte 7.2, F-P4).</summary>
        internal const double SOMMERLUEFTUNG_SCHWELLE = 23.0;

        /// <summary>Die Außenluft muss mindestens so viel kühler sein als die Raumluft [K] (Konzept 4.4).</summary>
        internal const double SOMMERLUEFTUNG_ABSTAND_AUSSEN = 2.0;

        /// <summary>Hysterese des Zurückschaltens [K] (Rechenschritte 7.2, F-P4).</summary>
        internal const double SOMMERLUEFTUNG_HYSTERESE = 1.0;

        // ---- Kühlung (Stufe KU1; Kühlkonzept 3.2, 3.4) ----------------------------------

        /// <summary>
        /// Mindestabstand des Kühlsollwerts über dem höchsten Heizsollwert [K] — die harte
        /// Prüfregel aus Kühlkonzept 3.2 (Q18-Regel des Stundenwegs): Ein Kühlsollwert darunter
        /// ließe Heizung und Kühlung gegeneinander arbeiten; das ist ein Eingabefehler.
        /// </summary>
        internal const double KUEHLSOLLWERT_ABSTAND_K = 1.0;

        /// <summary>
        /// Abstand der Sommerlüftungsschwelle UNTER dem Kühlsollwert [K] — mit wirksamer Kühlung
        /// schaltet die Sommerlüftung ab θ_kuehl − 3 K statt am Festwert
        /// <see cref="SOMMERLUEFTUNG_SCHWELLE"/> (Kühlkonzept 3.4, die einzige Verzahnung von
        /// Lüftungsregel und Kühlsollwert).
        /// </summary>
        internal const double SOMMERLUEFTUNG_ABSTAND_KUEHLSOLLWERT = 3.0;

        // ---- Langwelliger Austausch der Außenflächen (Stufe G2; Rechenschritte E5) ----

        /// <summary>Stefan-Boltzmann-Konstante σ [W/(m²K⁴)].</summary>
        internal const double STEFAN_BOLTZMANN = 5.67e-8;

        /// <summary>Nullpunkt der Celsius-Skala [K].</summary>
        internal const double KELVIN = 273.15;

        /// <summary>Langwelliger Emissionsgrad der Außenflächen ε_F [–] — die Testfälle rechnen 0,9 (Rechenschritte 11, Zeile 8).</summary>
        internal const double EMISSIONSGRAD_AUSSEN = 0.9;

        /// <summary>Absorptionsgrad der opaken Außenflächen für Solarstrahlung a_F [–] — EPOS-Vorgabe (Rechenschritte 1.3).</summary>
        internal const double ABSORPTIONSGRAD_OPAK = 0.6;

        /// <summary>Sichtfaktor einer senkrechten Fläche zum Himmel φ = (1 + cos 90°)/2 [–].</summary>
        internal const double SICHTFAKTOR_WAND = 0.5;

        /// <summary>Sichtfaktor einer waagerechten Fläche zum Himmel φ = (1 + cos 0°)/2 [–].</summary>
        internal const double SICHTFAKTOR_DACH = 1.0;

        // ---- Anlagenkopplung, Stufe AK1 (Konzept Anlagenkopplung 3.1, 3.4, 4.4, 8.1, 9.1) ----
        //
        // Alle Zahlen dieses Blocks sind VORGABEN VON EPOS-PLAN, keine Normwerte, und jede ist
        // am Gebäude einstellbar; NULL in der Spalte heißt „diese Vorgabe" (8.1, 8.4).

        /// <summary>Exponent der Übergabeart Radiator [–] — EPOS-Vorgabe (3.1).</summary>
        internal const double UEBERGABE_EXPONENT_RADIATOR = 1.3;

        /// <summary>Exponent der Übergabeart Flächenheizung [–] — EPOS-Vorgabe (3.1).</summary>
        internal const double UEBERGABE_EXPONENT_FLAECHE = 1.1;

        /// <summary>Exponent der Übergabeart Konvektor [–] — EPOS-Vorgabe (3.1).</summary>
        internal const double UEBERGABE_EXPONENT_KONVEKTOR = 1.4;

        /// <summary>Auslegungsvorlauf von Radiator und Konvektor [°C] — EPOS-Vorgabe (3.1).</summary>
        internal const double AUSLEGUNG_VORLAUF_RADIATOR = 55.0;

        /// <summary>Auslegungsrücklauf von Radiator und Konvektor [°C] — EPOS-Vorgabe (3.1).</summary>
        internal const double AUSLEGUNG_RUECKLAUF_RADIATOR = 45.0;

        /// <summary>Auslegungsvorlauf der Flächenheizung [°C] — EPOS-Vorgabe (3.1).</summary>
        internal const double AUSLEGUNG_VORLAUF_FLAECHE = 35.0;

        /// <summary>Auslegungsrücklauf der Flächenheizung [°C] — EPOS-Vorgabe (3.1).</summary>
        internal const double AUSLEGUNG_RUECKLAUF_FLAECHE = 28.0;

        /// <summary>Strahlungsanteil der Übergabeart Radiator [–] — EPOS-Vorgabe (3.1, H12).</summary>
        internal const double STRAHLUNGSANTEIL_RADIATOR = 0.3;

        /// <summary>Strahlungsanteil der Übergabeart Flächenheizung [–] — EPOS-Vorgabe (3.1, H12).</summary>
        internal const double STRAHLUNGSANTEIL_FLAECHE = 0.5;

        /// <summary>Strahlungsanteil der Übergabeart Konvektor [–] — EPOS-Vorgabe (3.1, H12).</summary>
        internal const double STRAHLUNGSANTEIL_KONVEKTOR = 0.1;

        /// <summary>Proportionalband des Raumreglers bei NULL [K] — EPOS-Vorgabe (4.4, H1, E25).</summary>
        internal const double VORGABE_REGLER_PROPORTIONALBAND_K = 1.0;

        /// <summary>Niveau der Heizkurve bei NULL [K] (3.4).</summary>
        internal const double VORGABE_HEIZKURVE_NIVEAU_K = 0.0;

        /// <summary>Steilheit der Heizkurve bei NULL [–] — die Kurve durch den Auslegungspunkt (3.4).</summary>
        internal const double VORGABE_HEIZKURVE_STEILHEIT = 1.0;

        // ---- Anlagenkopplung, Kälteseite (E37; Konzept Anlagenkopplung 7.2, 8.1) ----
        //
        // Ebenfalls VORGABEN VON EPOS-PLAN (A4), keine Normwerte, je Gebäude einstellbar.

        /// <summary>Exponent der Kühldecke [–] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHLUEBERGABE_EXPONENT_KUEHLDECKE = 1.1;

        /// <summary>Exponent der Flächenkühlung [–] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHLUEBERGABE_EXPONENT_FLAECHENKUEHLUNG = 1.1;

        /// <summary>Exponent des Gebläsekonvektors [–] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHLUEBERGABE_EXPONENT_GEBLAESEKONVEKTOR = 1.0;

        /// <summary>Auslegungsvorlauf von Kühldecke und Flächenkühlung [°C] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHL_AUSLEGUNG_VORLAUF_FLAECHE = 16.0;

        /// <summary>Auslegungsrücklauf von Kühldecke und Flächenkühlung [°C] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHL_AUSLEGUNG_RUECKLAUF_FLAECHE = 19.0;

        /// <summary>Auslegungsvorlauf des Gebläsekonvektors [°C] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHL_AUSLEGUNG_VORLAUF_KONVEKTOR = 7.0;

        /// <summary>Auslegungsrücklauf des Gebläsekonvektors [°C] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHL_AUSLEGUNG_RUECKLAUF_KONVEKTOR = 12.0;

        /// <summary>Strahlungsanteil von Kühldecke und Flächenkühlung [–] — EPOS-Vorgabe (E37, KU 3.2).</summary>
        internal const double KUEHL_STRAHLUNGSANTEIL_FLAECHE = STRAHLUNGSANTEIL_FLAECHE;

        /// <summary>Strahlungsanteil des Gebläsekonvektors [–] — EPOS-Vorgabe (E37).</summary>
        internal const double KUEHL_STRAHLUNGSANTEIL_KONVEKTOR = 0.0;

        /// <summary>
        /// Vorlaufgrenze von Kühldecke und Flächenkühlung [°C] — EPOS-Vorgabe als Ersatz der
        /// Taupunktgrenze (7.2, K5); der Gebläsekonvektor hat keine Grenze.
        /// </summary>
        internal const double KUEHL_VORLAUFGRENZE_FLAECHE = 16.0;

        /// <summary>Kleinster zulässiger Auslegungsvorlauf und kleinste Vorlaufgrenze der Kühlübergabe [°C] (E37).</summary>
        internal const double KUEHL_VORLAUF_MIN = 4.0;

        /// <summary>Größter zulässiger Auslegungsvorlauf und größte Vorlaufgrenze der Kühlübergabe [°C] (E37).</summary>
        internal const double KUEHL_VORLAUF_MAX = 22.0;

        /// <summary>Kleinste zulässige Auslegungs-Raumtemperatur der Kühlübergabe [°C] (E37).</summary>
        internal const double KUEHL_AUSLEGUNG_RAUM_MIN = 20.0;

        /// <summary>Größte zulässige Auslegungs-Raumtemperatur der Kühlübergabe [°C] (E37).</summary>
        internal const double KUEHL_AUSLEGUNG_RAUM_MAX = 30.0;

        // Prüfregeln der Eingaben (Dialogtabelle 9.1) — der Kern prüft dieselben Grenzen hart,
        // damit eine Eingabe, die am Dialog vorbei in die Datenbank kommt, benannt abbricht.

        /// <summary>Kleinster zulässiger Exponent der Übergabe [–] (9.1).</summary>
        internal const double UEBERGABE_EXPONENT_MIN = 1.0;

        /// <summary>Größter zulässiger Exponent der Übergabe [–] (9.1).</summary>
        internal const double UEBERGABE_EXPONENT_MAX = 1.6;

        /// <summary>Kleinster zulässiger Auslegungsvorlauf [°C] (9.1).</summary>
        internal const double AUSLEGUNG_VORLAUF_MIN = 25.0;

        /// <summary>Größter zulässiger Auslegungsvorlauf [°C] (9.1).</summary>
        internal const double AUSLEGUNG_VORLAUF_MAX = 90.0;

        /// <summary>Kleinste zulässige Auslegungs-Raumtemperatur [°C] (9.1).</summary>
        internal const double AUSLEGUNG_RAUM_MIN = 15.0;

        /// <summary>Größte zulässige Auslegungs-Raumtemperatur [°C] (9.1).</summary>
        internal const double AUSLEGUNG_RAUM_MAX = 26.0;

        /// <summary>Kleinste zulässige eingegebene Auslegungs-Außentemperatur [°C] (9.1).</summary>
        internal const double AUSLEGUNG_AUSSEN_MIN = -30.0;

        /// <summary>Größte zulässige eingegebene Auslegungs-Außentemperatur [°C] (9.1).</summary>
        internal const double AUSLEGUNG_AUSSEN_MAX = 5.0;

        /// <summary>Kleinstes zulässiges Niveau der Heizkurve [K] (9.1).</summary>
        internal const double HEIZKURVE_NIVEAU_MIN = -10.0;

        /// <summary>Größtes zulässiges Niveau der Heizkurve [K] (9.1).</summary>
        internal const double HEIZKURVE_NIVEAU_MAX = 10.0;

        /// <summary>Kleinste zulässige Steilheit der Heizkurve [–] (9.1).</summary>
        internal const double HEIZKURVE_STEILHEIT_MIN = 0.2;

        /// <summary>Größte zulässige Steilheit der Heizkurve [–] (9.1).</summary>
        internal const double HEIZKURVE_STEILHEIT_MAX = 3.0;

        /// <summary>Kleinstes zulässiges Proportionalband [K]; 0 = ideale Regelung mit Grenze (E25).</summary>
        internal const double REGLER_PROPORTIONALBAND_MIN_K = 0.0;

        /// <summary>Größtes zulässiges Proportionalband [K] (E25).</summary>
        internal const double REGLER_PROPORTIONALBAND_MAX_K = 5.0;

        /// <summary>Kleinster zulässiger Wert des Sollwert-Zeitprogramms [°C] — Plausibilitätsgrenze des Verwenders (4.3).</summary>
        internal const double SOLLWERTPROFIL_MIN_C = 0.0;

        /// <summary>Größter zulässiger Wert des Sollwert-Zeitprogramms [°C] — Plausibilitätsgrenze des Verwenders (4.3).</summary>
        internal const double SOLLWERTPROFIL_MAX_C = 30.0;

        // ---- Bauteilweg, Stufe G3 (Mehrzonenkonzept 3.1–3.5; Rechenschritte Kapitel 3) ----
        //
        // Bezugsperioden nach VDI 6007 Blatt 1 Gl. (10c)–(10e); die Kriterien (10a)/(10b) stehen
        // als Gleichungskonstanten in Bauteilreduktion.

        /// <summary>Sekunden eines Tages — Kreisfrequenz ω = 2π/(86 400 · T), VDI 6007-1 Gl. (9).</summary>
        internal const double SEKUNDEN_JE_TAG = 86400.0;

        /// <summary>Bezugsperiode eines Bauteils im Regelfall T_BT [d] — VDI 6007-1 Gl. (10d).</summary>
        internal const double BEZUGSPERIODE_BAUTEIL_D = 7.0;

        /// <summary>Bezugsperiode eines Bauteils mit raumseitig abgedeckter Speichermasse T_BT [d] — VDI 6007-1 Gl. (10c).</summary>
        internal const double BEZUGSPERIODE_ABGEDECKT_D = 2.0;

        /// <summary>Bezugsperiode der Zusammenfassung zum Raum T_RA [d] — VDI 6007-1 Gl. (10e).</summary>
        internal const double BEZUGSPERIODE_RAUM_D = 5.0;

        // Bemessungswerte der Wärmeübergangswiderstände, DIN EN ISO 6946:2018-03, 6.8, Tabelle 7.
        // „Horizontal" gilt für Wärmeströme ±30° um die Waagerechte, also für Bauteilneigungen
        // von 60° bis 120° (0° = waagerecht nach oben, 90° = senkrecht, 180° = waagerecht nach unten).

        /// <summary>Innerer Wärmeübergangswiderstand bei Wärmestrom aufwärts R_si [m²K/W] — DIN EN ISO 6946, Tabelle 7.</summary>
        internal const double R_SI_AUFWAERTS = 0.10;

        /// <summary>Innerer Wärmeübergangswiderstand bei waagerechtem Wärmestrom R_si [m²K/W] — DIN EN ISO 6946, Tabelle 7.</summary>
        internal const double R_SI_HORIZONTAL = 0.13;

        /// <summary>Innerer Wärmeübergangswiderstand bei Wärmestrom abwärts R_si [m²K/W] — DIN EN ISO 6946, Tabelle 7.</summary>
        internal const double R_SI_ABWAERTS = 0.17;

        /// <summary>Äußerer Wärmeübergangswiderstand an Außenluft R_se [m²K/W] — DIN EN ISO 6946, Tabelle 7 (alle Richtungen).</summary>
        internal const double R_SE_AUSSENLUFT = 0.04;

        /// <summary>
        /// Äußerer Wärmeübergangswiderstand an Erdreich R_se [m²K/W]: keiner — die Werte der
        /// Tabelle 7 gelten nur für Oberflächen, die Luft berühren (DIN EN ISO 6946, Tabelle 7, Anmerkung 1).
        /// </summary>
        internal const double R_SE_ERDREICH = 0.0;

        /// <summary>Kleinste Neigung mit waagerechtem Wärmestrom [°] — DIN EN ISO 6946, 6.8 (±30°).</summary>
        internal const double NEIGUNG_HORIZONTAL_MIN_GRAD = 60.0;

        /// <summary>Größte Neigung mit waagerechtem Wärmestrom [°] — DIN EN ISO 6946, 6.8 (±30°).</summary>
        internal const double NEIGUNG_HORIZONTAL_MAX_GRAD = 120.0;

        /// <summary>
        /// Stützstellen der Dicke ruhender Luftschichten [mm] — DIN EN ISO 6946:2018-03, 6.9.2,
        /// Tabelle 8; Zwischenwerte linear interpoliert (Anmerkung der Tabelle).
        /// </summary>
        internal static ReadOnlySpan<double> LUFTSCHICHT_DICKE_MM => new double[] { 0.0, 5.0, 7.0, 10.0, 15.0, 25.0, 50.0, 100.0, 300.0 };

        /// <summary>Wärmedurchlasswiderstand ruhender Luftschichten bei Wärmestrom aufwärts [m²K/W] — DIN EN ISO 6946, Tabelle 8.</summary>
        internal static ReadOnlySpan<double> LUFTSCHICHT_R_AUFWAERTS => new double[] { 0.00, 0.11, 0.13, 0.15, 0.16, 0.16, 0.16, 0.16, 0.16 };

        /// <summary>Wärmedurchlasswiderstand ruhender Luftschichten bei waagerechtem Wärmestrom [m²K/W] — DIN EN ISO 6946, Tabelle 8.</summary>
        internal static ReadOnlySpan<double> LUFTSCHICHT_R_HORIZONTAL => new double[] { 0.00, 0.11, 0.13, 0.15, 0.17, 0.18, 0.18, 0.18, 0.18 };

        /// <summary>Wärmedurchlasswiderstand ruhender Luftschichten bei Wärmestrom abwärts [m²K/W] — DIN EN ISO 6946, Tabelle 8.</summary>
        internal static ReadOnlySpan<double> LUFTSCHICHT_R_ABWAERTS => new double[] { 0.00, 0.11, 0.13, 0.15, 0.17, 0.19, 0.21, 0.22, 0.23 };

        /// <summary>Größte Dicke einer ruhenden Luftschicht mit Tabellenwert [m] — DIN EN ISO 6946, 6.9.1.</summary>
        internal const double LUFTSCHICHT_DICKE_MAX_M = 0.3;

        // Plausibilitätsband der Stoffwerte (Mehrzonenkonzept 3.5): außerhalb ist ein Stoffwert
        // „nicht geliefert" — der Kern bricht benannt ab, statt ihn zu übernehmen.

        /// <summary>Kleinste Schichtdicke [m] (Mehrzonenkonzept 3.5).</summary>
        internal const double SCHICHT_DICKE_MIN_M = 0.001;

        /// <summary>Größte Schichtdicke [m] (Mehrzonenkonzept 3.5).</summary>
        internal const double SCHICHT_DICKE_MAX_M = 1.0;

        /// <summary>Kleinste Wärmeleitfähigkeit λ [W/(mK)] (Mehrzonenkonzept 3.5).</summary>
        internal const double LAMBDA_MIN_WMK = 0.005;

        /// <summary>Größte Wärmeleitfähigkeit λ [W/(mK)] (Mehrzonenkonzept 3.5).</summary>
        internal const double LAMBDA_MAX_WMK = 500.0;

        /// <summary>Kleinste Rohdichte ρ [kg/m³] (Mehrzonenkonzept 3.5); eine Luftschicht darf darunter liegen.</summary>
        internal const double ROHDICHTE_MIN_KGM3 = 5.0;

        /// <summary>Größte Rohdichte ρ [kg/m³] (Mehrzonenkonzept 3.5).</summary>
        internal const double ROHDICHTE_MAX_KGM3 = 8000.0;

        /// <summary>Kleinste spezifische Wärmekapazität c_p [J/(kgK)] (Mehrzonenkonzept 3.5).</summary>
        internal const double CP_MIN_JKGK = 100.0;

        /// <summary>Größte spezifische Wärmekapazität c_p [J/(kgK)] (Mehrzonenkonzept 3.5).</summary>
        internal const double CP_MAX_JKGK = 5000.0;

        /// <summary>
        /// Relative Abweichung des eingetragenen vom gerechneten U-Wert, ab der die Herleitung
        /// einen Hinweis trägt [–] (Mehrzonenkonzept 3.4: „mehr als 10 %").
        /// </summary>
        internal const double UWERT_ABWEICHUNG_HINWEIS = 0.10;
    }
}
