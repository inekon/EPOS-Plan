using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Eingang des Gebäudemodells</b> — die aufgelösten Gebäudedaten, der Satz
    /// <see cref="ErsatzparameterRC"/> und die sieben Randreihen zu 8 760 Stunden (Stufe G1;
    /// Umsetzungskonzept 1.4, Rechenschritte Kapitel 1, Schritte A und E).
    ///
    /// <para><b>Der Eingangsbauer ist die statische Fabrik <see cref="Bauen"/></b> und die
    /// eine Stelle, an der <see cref="GebaeudeKlimaweg"/> gerufen wird (A18). Er liest die
    /// Gebäudezeile <b>nach</b> dem Vorbereitungsschritt und die Klimareihen, die dieser
    /// bereitgestellt hat; er liest keine Tabelle selbst.</para>
    ///
    /// <para><b>Vorgaben bei NULL</b> sind die des Eingangsbauers (Rechenschritte 1.1), nicht
    /// DDL-Vorgaben; sie stehen in <see cref="GebaeudeFestwerte"/>. <b>Harte Prüfungen</b>
    /// (Konzept 4.8) brechen mit einem benannten <see cref="GebaeudeModellFehler"/> ab — die
    /// des Klassenwegs in <see cref="ErsatzparameterRC.AusKlassenweg"/>, die des Fahrplans
    /// und der Fensterflächen hier.</para>
    ///
    /// <para><b>Die Lastaufteilung liegt hier, nicht im Löser</b> (F-P2): solare und innere
    /// Lasten werden auf die beiden Oberflächenknoten und die Luft verteilt; der Löser bekommt
    /// genau die drei Lasten seiner Knotenbilanzen. Die Zwischengrößen bleiben hier prüfbar
    /// (<see cref="PhiSolar"/>, <see cref="ThetaGrund"/>).</para>
    ///
    /// <para>Unveränderlich nach <see cref="Bauen"/>; ohne Datenbank, ohne Protokoll.</para>
    /// </summary>
    internal sealed class GebaeudeModellEingang
    {
        private GebaeudeModellEingang() { }

        // =====================================================================
        //  Die aufgelösten Gebäudedaten (Rechenschritte 1.1)
        // =====================================================================

        /// <summary>Bezeichnung des Gebäudes für Meldungen.</summary>
        internal string Bezeichnung { get; private set; }

        /// <summary>Nutzfläche des Katalogbaus A_f [m²] (E13).</summary>
        internal double Nutzflaeche_M2 { get; private set; }
        /// <summary>Raumhöhe H [m].</summary>
        internal double Raumhoehe_M { get; private set; }
        /// <summary>Speichermasse C_ges [Wh/K] (<c>Bauweise</c>).</summary>
        internal double Bauweise_WhK { get; private set; }

        /// <summary>U-Wert Außenwand [W/(m²K)].</summary>
        internal double U_Aussenwand { get; private set; }
        /// <summary>U-Wert Fenster [W/(m²K)].</summary>
        internal double U_Fenster { get; private set; }
        /// <summary>U-Wert Dach [W/(m²K)].</summary>
        internal double U_Dach { get; private set; }
        /// <summary>U-Wert Grundfläche [W/(m²K)].</summary>
        internal double U_Grund { get; private set; }
        /// <summary>U-Wert Sonstiges [W/(m²K)].</summary>
        internal double U_Sonstige { get; private set; }

        /// <summary>Fläche Außenwand [m²].</summary>
        internal double A_Aussenwand_M2 { get; private set; }
        /// <summary>Gesamte Fensterfläche A_w [m²].</summary>
        internal double A_Fenster_M2 { get; private set; }
        /// <summary>Dachfläche [m²].</summary>
        internal double A_Dach_M2 { get; private set; }
        /// <summary>Grundfläche [m²].</summary>
        internal double A_Grund_M2 { get; private set; }
        /// <summary>Sonstige Flächen [m²].</summary>
        internal double A_Sonstige_M2 { get; private set; }
        /// <summary>Fensterfläche Süd [m²].</summary>
        internal double A_FensterSued_M2 { get; private set; }
        /// <summary>Fensterfläche Ost [m²].</summary>
        internal double A_FensterOst_M2 { get; private set; }
        /// <summary>Fensterfläche West [m²].</summary>
        internal double A_FensterWest_M2 { get; private set; }
        /// <summary>Fensterfläche Nord [m²].</summary>
        internal double A_FensterNord_M2 { get; private set; }

        /// <summary>Gesamtenergiedurchlassgrad g [–].</summary>
        internal double GWert { get; private set; }
        /// <summary>Rahmenanteil 1 − F_F [–].</summary>
        internal double Rahmenanteil { get; private set; }
        /// <summary>Verschattungsfaktor F_S [–].</summary>
        internal double Verschattungsfaktor { get; private set; }
        /// <summary>Σψ·L der drei Wärmebrückenpaare [W/K].</summary>
        internal double SummePsiL_WK { get; private set; }
        /// <summary>
        /// Der wirksame Grundluftwechsel n [1/h] (Stufe G2): Infiltration + Nutzerlüftung, bei
        /// beiden leer die Luftwechselrate, sonst die Vorgaben
        /// (<see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?, out Luftwechselherkunft)"/>).
        /// </summary>
        internal double Luftwechselrate_h { get; private set; }
        /// <summary>Woher <see cref="Luftwechselrate_h"/> kommt.</summary>
        internal Luftwechselherkunft LuftwechselHerkunft { get; private set; }
        /// <summary>Ist die Sommerlüftungsregel eingeschaltet (Spalte <c>Sommerlueftung</c>)?</summary>
        internal bool Sommerlueftung { get; private set; }
        /// <summary>
        /// Der Zusatzleitwert einer Stunde mit Sommerlüftung [W/K]: der Luftwechsel steigt von
        /// <see cref="Luftwechselrate_h"/> auf 2,0 1/h, (2,0 − n)·A_f·H·c·ρ, nie negativ.
        /// </summary>
        internal double SommerlueftungZusatzleitwertWK { get; private set; }
        /// <summary>Innere Wärmegewinne des Katalogbaus [W], zeitlich konstant (G1).</summary>
        internal double InnereGewinne_W { get; private set; }

        /// <summary>Tagsollwert [°C].</summary>
        internal double SollTag { get; private set; }
        /// <summary>Nachtsollwert [°C].</summary>
        internal double SollNacht { get; private set; }
        /// <summary>Wochenendsollwert [°C]; wirksam nur über 5 °C.</summary>
        internal double SollWochenende { get; private set; }
        /// <summary>Feriensollwert [°C].</summary>
        internal double SollFerien { get; private set; }
        /// <summary>
        /// Obere Raumtemperatur θ_max [°C] (<c>Maximaleraumtemperatur</c>) — allein die Grenze
        /// der Überhitzungskennzahl (Kühlkonzept 6.4, 7.1). Der Löser regelt nicht auf sie:
        /// Ohne wirksame Kühlung läuft das Gebäude frei und darf über θ_max steigen (Entscheid
        /// E32), mit wirksamer Kühlung regelt der Kühlsollwert (<see cref="KuehlSollwert"/>).
        /// </summary>
        internal double ThetaMaxWert { get; private set; }

        /// <summary>
        /// <b>Ist die Kühlung dieses Gebäudes wirksam?</b> (Stufe KU1; Kühlkonzept 3.2, 7.1, 7.2)
        /// Genau dann, wenn das PROJEKT Kälte rechnet (<c>Tab_Einstellungen.Kuehlbetrieb</c>),
        /// das Gebäude gekühlt wird (<c>Kuehlung_Aktiv</c>) und einen Kühlsollwert trägt
        /// (<c>Kuehl_Sollwert</c>; NULL heißt „Kühlung aus", F-K1 — der Rückfall auf θ_max ist
        /// eine ausdrückliche Eingabe, kein stiller Wert). Nur dann kühlt der Löser, und nur
        /// dann gibt es eine Kühlreihe, die in den Kühlkanal geht (Entscheid E32).
        /// </summary>
        internal bool KuehlungWirksam { get; private set; }

        /// <summary>
        /// Die obere Regelgrenze des Lösers [°C]: mit wirksamer Kühlung der Kühlsollwert θ_kuehl
        /// (Kühlkonzept 3.2, Totband zwischen Heiz- und Kühlsollwert), sonst +∞ — keine obere
        /// Grenze, keine Kühlung: Das Gebäude läuft frei (Entscheid E32).
        /// </summary>
        internal double KuehlSollwert { get; private set; }

        /// <summary>
        /// Kühlleistungsgrenze [W] (<c>Kuehlleistung_Max</c> in kW, K11): Gegenstück zu
        /// <see cref="HeizleistungMaxW"/>; NaN = unbegrenzt. Nur mit wirksamer Kühlung gesetzt —
        /// ohne sie kühlt der Löser nicht (E32).
        /// </summary>
        internal double KuehlleistungMaxW { get; private set; } = double.NaN;
        /// <summary>Ferientage (Index 0 … 364), nur bei aktivem Fahrplan belegt.</summary>
        internal bool[] Ferientage { get; private set; }

        /// <summary>Masseanteil der Außenbauteile a_AW [–].</summary>
        internal double MasseanteilAussen { get; private set; }
        /// <summary>Innenflächenfaktor f_IW [–].</summary>
        internal double Innenflaechenfaktor { get; private set; }
        /// <summary>Strahlungsanteil der Heizübergabe [–].</summary>
        internal double HeizungStrahlungsanteil { get; private set; }
        /// <summary>Heizleistungsgrenze [W]; NaN = unbegrenzt.</summary>
        internal double HeizleistungMaxW { get; private set; }
        /// <summary>Randbedingung der Grundfläche (<c>DbWerte.GRUND_*</c>).</summary>
        internal string GrundRandbedingung { get; private set; }
        /// <summary>Kellertemperatur [°C].</summary>
        internal double Kellertemperatur { get; private set; }
        /// <summary>Schalter „Strahlung auf Außenbauteile": opake Flächen mit kurz- und langwelligem Term (E5, Stufe G2).</summary>
        internal bool AussenbauteileStrahlung { get; private set; }

        // =====================================================================
        //  Anlagenkopplung, Stufe AK1 (Konzept Anlagenkopplung 3.4, 4.3, 6.1, 8.1, 8.4)
        // =====================================================================

        /// <summary>Die Kopplungsstufe des PROJEKTS, wie gelesen (<c>Tab_Einstellungen.Anlagenkopplung</c>); <c>null</c> = aus.</summary>
        internal string AnlagenkopplungStufe { get; private set; }

        /// <summary>Der Schalter <c>Heizkreis_Aktiv</c> des Gebäudes — die Absicht, unabhängig von der Projektstufe.</summary>
        internal bool HeizkreisAktiv { get; private set; }

        /// <summary>Die Übergabeart des Gebäudes (<c>DbWerte.UEBERGABE_*</c>), wie gelesen; <c>null</c> = ideal.</summary>
        internal string UebergabeArt { get; private set; }

        /// <summary>
        /// <b>Ist die Kopplung für dieses Gebäude wirksam?</b> Projektstufe AK1 oder höher,
        /// <c>Heizkreis_Aktiv</c> und eine Übergabeart, die nicht „ideal" ist
        /// (<see cref="Waermeuebergabe.KopplungWirksamFuer"/>). Nur dann gelten die dreizehn
        /// Spalten der Übergabe samt Zeitprogramm; sonst rechnet das Gebäude byte-gleich wie
        /// vorher (N-A3).
        /// </summary>
        internal bool KopplungWirksam { get; private set; }

        /// <summary>Die Kennwerte der Übergabe in W und W/K; <c>null</c> ohne wirksame Kopplung.</summary>
        internal Uebergabekennwerte Uebergabe { get; private set; }

        /// <summary>Die Heizkurve des Auslegungspunkts (auch bei festem Vorlauf gebildet, für die Herleitung).</summary>
        internal Heizkurve Heizkurve { get; private set; }

        /// <summary>Fährt das Gebäude die Heizkurve (<c>Heizkurve_Aktiv</c>)? Sonst gilt ein fester Vorlauf.</summary>
        internal bool HeizkurveAktiv { get; private set; }

        /// <summary>Woher der Vorlauf kommt.</summary>
        internal Vorlaufquelle Vorlaufquelle { get; private set; }

        /// <summary>Der feste Vorlauf [°C] bei ausgeschalteter Heizkurve; NaN mit Heizkurve.</summary>
        internal double VorlaufFestC { get; private set; } = double.NaN;

        /// <summary>Vorlauf je Stunde [°C] (Schritt E, 10.1); NaN jenseits der Heizgrenze; <c>null</c> ohne Kopplung.</summary>
        internal double[] VorlaufC { get; private set; }

        /// <summary>Proportionalband des Raumreglers [K] (H1, E25).</summary>
        internal double ReglerbandK { get; private set; }

        /// <summary>Die Auslegungs-Außentemperatur [°C] — eingegeben oder hergeleitet (H10).</summary>
        internal double AuslegungAussentemperaturC { get; private set; } = double.NaN;

        /// <summary>Ist die Auslegungs-Außentemperatur aus der Klimareihe hergeleitet (kältestes Tagesmittel, abgerundet)?</summary>
        internal bool AuslegungAussentemperaturHergeleitet { get; private set; }

        /// <summary>
        /// Die hergeleitete Auslegungsheizlast des Katalogbaus [W] — die stationäre Last des
        /// vorhandenen Modells am Auslegungspunkt (8.4), ausdrücklich kein Normnachweis (H-F12).
        /// </summary>
        internal double AuslegungsheizlastW { get; private set; } = double.NaN;

        /// <summary>Ist die Nennleistung der Übergabe aus der Auslegungsheizlast hergeleitet (NULL, H7)?</summary>
        internal bool UebergabeNennleistungHergeleitet { get; private set; }

        /// <summary>Gilt ein Sollwert-Zeitprogramm (Wochenprofil) statt der vier Bestandssollwerte (4.3, H8)?</summary>
        internal bool SollwertprofilWirksam { get; private set; }

        // =====================================================================
        //  Ersatzparameter und Randreihen (Schritte A und E)
        // =====================================================================

        /// <summary>Die RC-Größen des Klassenwegs (Schritt A).</summary>
        internal ErsatzparameterRC Parameter { get; private set; }

        /// <summary>Der Zeitbezug, mit dem die Fassadenstrahlung gerechnet ist (U6).</summary>
        internal Zeitbezug Zeitbezug { get; private set; }

        /// <summary>Außenlufttemperatur [°C].</summary>
        internal double[] ThetaOut { get; private set; }
        /// <summary>U·A-gewichtete äquivalente Außentemperatur [°C] (E7).</summary>
        internal double[] ThetaEq { get; private set; }
        /// <summary>Strahlungslast Außenbauteiloberfläche [W].</summary>
        internal double[] PhiRadAW { get; private set; }
        /// <summary>Strahlungslast Innenbauteiloberfläche [W].</summary>
        internal double[] PhiRadIW { get; private set; }
        /// <summary>Konvektive Last an der Raumluft [W].</summary>
        internal double[] PhiConv { get; private set; }
        /// <summary>Heizsollwert [°C] (E8).</summary>
        internal double[] ThetaSoll { get; private set; }
        /// <summary>Obere Regelgrenze je Stunde [°C]: der Kühlsollwert, ohne wirksame Kühlung +∞ (E32).</summary>
        internal double[] ThetaMax { get; private set; }

        /// <summary>Zwischengröße: Fenstersolareintrag gesamt [W] (E3).</summary>
        internal double[] PhiSolar { get; private set; }
        /// <summary>Zwischengröße: Temperatur an der Grundfläche [°C] (E6).</summary>
        internal double[] ThetaGrund { get; private set; }
        /// <summary>Zwischengröße: die Fassadenstrahlung [W/m²] (E2).</summary>
        internal Fassadenstrahlung Strahlung { get; private set; }
        /// <summary>Fiel die Erdreichrechnung auf Ersatzwerte des Jahresgangs zurück?</summary>
        internal bool ErdreichErsatzwerte { get; private set; }
        /// <summary>Zahl der Stunden mit Gegenstrahlung — nur in ihnen rechnet der langwellige Term (NULL-Regel E5).</summary>
        internal int StundenMitGegenstrahlung { get; private set; }

        /// <summary>
        /// Die Randbedingung der Stunde <paramref name="h"/> für den Löser;
        /// <paramref name="sommerlueftung"/> legt den Zusatzleitwert der Sommerlüftung parallel
        /// zum Lüftungszweig (Rechenschritte 7.2 — der Zustand gilt die ganze Stunde).
        /// </summary>
        internal Stundenrand Rand(int h, bool sommerlueftung = false)
        {
            // Die Kühlleistung wirkt in KU1 rein konvektiv am Luftknoten (Kühlkonzept 3.2):
            // kein Anteil an der Innenfläche, keine eigene Übergabeart vor der Anlagenkopplung.
            // Mit wirksamer Kopplung (AK1) trägt die Stunde Übergabe, Vorlauf und Reglerband —
            // die Kälteseite der Kopplung ist benannt vertagt (H9) und bleibt ideal.
            if (!KopplungWirksam)
                return new Stundenrand(ThetaOut[h], ThetaEq[h], ThetaSoll[h], ThetaMax[h],
                                       PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                       heizleistungMaxW: HeizleistungMaxW,
                                       kuehlleistungMaxW: KuehlleistungMaxW,
                                       heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                       zusatzleitwertWK: sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0);
            return new Stundenrand(ThetaOut[h], ThetaEq[h], ThetaSoll[h], ThetaMax[h],
                                   PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                   heizleistungMaxW: HeizleistungMaxW,
                                   kuehlleistungMaxW: KuehlleistungMaxW,
                                   heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                   zusatzleitwertWK: sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0,
                                   uebergabe: Uebergabe,
                                   vorlaufC: VorlaufC[h],
                                   reglerbandK: ReglerbandK);
        }

        /// <summary>Ist Stunde <paramref name="h"/> (0 … 8759) Nutzungszeit — Stunde des Tages 7 … 22, 1-basiert (Rechenschritte 8.2, E8)?</summary>
        internal static bool Nutzungszeit(int h)
        {
            int stundeDesTages = h % 24 + 1;
            return stundeDesTages >= GebaeudeFestwerte.TAG_ERSTE_STUNDE
                && stundeDesTages <= GebaeudeFestwerte.TAG_LETZTE_STUNDE;
        }

        // =====================================================================
        //  Der Eingangsbauer
        // =====================================================================

        /// <summary>
        /// <b>Der Eingangsbauer</b> (Umsetzungskonzept 1.4): Gebäudedaten auflösen und prüfen,
        /// Ersatzparameter nach dem Klassenweg, Klimaweg, Lastaufteilung und Sollwertfahrplan.
        /// </summary>
        /// <param name="gebaeude">Die Gebäudezeile (nach dem Vorbereitungsschritt).</param>
        /// <param name="solarOrtszeit">Die Klimazeilen in Ortszeit, 8 760, mit UTC-Herkunft.</param>
        /// <param name="wochenende">Die Wochenendmaske des Ortszeit-Kalenders, 365 Tage (U7).</param>
        /// <param name="laengengrad">Längengrad der Klimaregion [°].</param>
        /// <param name="breitengrad">Breitengrad der Klimaregion [°].</param>
        /// <param name="zeitbezug">Zeitbezug der Sonnengeometrie (U6).</param>
        /// <param name="kuehlbetrieb">Rechnet das PROJEKT Kälte (<c>Tab_Einstellungen.Kuehlbetrieb</c>)?
        /// Ohne ihn wird kein Gebäude gekühlt; es läuft frei (<see cref="KuehlungWirksam"/>, E32).</param>
        /// <param name="anlagenkopplung">Die Kopplungsstufe des PROJEKTS (<c>Tab_Einstellungen.Anlagenkopplung</c>);
        /// <c>null</c> = aus. Nur mit ihr gilt die Wärmeübergabe (<see cref="KopplungWirksam"/>).</param>
        /// <param name="vorlaufAnlageC">Der feste Vorlauf der Anlage [°C] für ein Gebäude ohne Heizkurve —
        /// der höchste projektierte Vorlauf der Wärmeerzeuger des Heizkanals, von der Fassade gelesen
        /// (das Modul liest keine Anlagendaten); NaN = keiner.</param>
        /// <param name="nennleistungSkalierung">Verhältnis wirkliches Gebäude : Katalogbau (E8) für eine
        /// FEST eingetragene Nennleistung der Übergabe — sie gilt dem wirklichen Gebäude und wird auf den
        /// Katalogbau umgerechnet (H7); NaN = die Nennleistung wird auch dann hergeleitet (erster Lauf
        /// der Verhältnisrechnung).</param>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Prüfung.</exception>
        internal static GebaeudeModellEingang Bauen(
            ProjektGebaeudeModel gebaeude,
            IReadOnlyList<SolardatenModel> solarOrtszeit,
            bool[] wochenende,
            double laengengrad, double breitengrad,
            Zeitbezug zeitbezug = GebaeudeKlimaweg.ZEITBEZUG_VORGABE,
            bool kuehlbetrieb = false,
            string anlagenkopplung = null,
            double vorlaufAnlageC = double.NaN,
            double nennleistungSkalierung = 1.0)
        {
            GebaeudeModellEingang e = Daten(gebaeude);
            e.Parameter = ErsatzparameterRC.AusKlassenweg(e);
            e.Zeitbezug = zeitbezug;

            GebaeudeKlimaweg.Pruefen(solarOrtszeit, laengengrad, breitengrad);
            if (wochenende == null || wochenende.Length != 365)
                throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig,
                    "Die Wochenendmaske des Ortszeit-Kalenders fehlt (365 Tage erwartet).");

            // ---- Klimaweg (E1, E2, E6) — die eine Aufrufstelle (A18) ----
            e.ThetaOut = GebaeudeKlimaweg.Aussentemperatur(solarOrtszeit);
            e.Strahlung = GebaeudeKlimaweg.Fassaden(solarOrtszeit, laengengrad, breitengrad, zeitbezug);
            bool erdreichAusKlima;
            e.ThetaGrund = GebaeudeKlimaweg.Grundtemperatur(e.GrundRandbedingung, e.Kellertemperatur,
                                                             e.ThetaOut, out erdreichAusKlima);
            e.ErdreichErsatzwerte = string.Equals(e.GrundRandbedingung, DbWerte.GRUND_ERDREICH, StringComparison.Ordinal)
                                    && !erdreichAusKlima;
            e.StundenMitGegenstrahlung = GebaeudeKlimaweg.StundenMitGegenstrahlung(solarOrtszeit);

            ErsatzparameterRC p = e.Parameter;

            // ---- Flächengewichte der Lastaufteilung (E3, E4) ----
            double aAwGes = p.A_AW_gesamt_M2;
            double aIw = p.A_IW_M2;
            double aRaum = aAwGes + aIw;
            double anteilAW = aAwGes / aRaum;
            double anteilIW = aIw / aRaum;

            double solarFaktor = e.GWert * (1.0 - e.Rahmenanteil) * e.Verschattungsfaktor * GebaeudeFestwerte.F_W;
            double aKon = GebaeudeFestwerte.A_KON_SOLAR;
            double innenKonv = GebaeudeFestwerte.ANTEIL_INNERE_LASTEN_KONVEKTIV * e.InnereGewinne_W;
            double innenRad = e.InnereGewinne_W - innenKonv;

            // ---- Gewichte der äquivalenten Außentemperatur (E7) ----
            double uaLuftseitig = e.U_Aussenwand * e.A_Aussenwand_M2 + e.U_Dach * e.A_Dach_M2
                                  + e.U_Sonstige * e.A_Sonstige_M2;
            // Mit Schalter: Wand und Sonstiges senkrecht, Dach waagerecht (Klassenweg, Rechenschritte E5).
            double uaSenkrecht = e.U_Aussenwand * e.A_Aussenwand_M2 + e.U_Sonstige * e.A_Sonstige_M2;
            double uaDach = e.U_Dach * e.A_Dach_M2;
            double uaGrund = e.U_Grund * e.A_Grund_M2;
            double uaFenster = p.UA_Fenster_WK;
            double uaSumme = p.SummeUA_opak_WK + uaFenster;

            var thetaEq = new double[8760];
            var phiRadAW = new double[8760];
            var phiRadIW = new double[8760];
            var phiConv = new double[8760];
            var phiSolar = new double[8760];
            Fassadenstrahlung s = e.Strahlung;

            for (int h = 0; h < 8760; h++)
            {
                // E3 — Fenstersolareintrag, flächenproportional verteilt (A_v = 0 im Klassenweg).
                double sol = (e.A_FensterSued_M2 * s.Sued[h] + e.A_FensterOst_M2 * s.Ost[h]
                              + e.A_FensterWest_M2 * s.West[h] + e.A_FensterNord_M2 * s.Nord[h]) * solarFaktor;
                phiSolar[h] = sol;
                double solLuft = aKon * sol;
                double solRad = sol - solLuft;

                // E4 — innere Lasten, 0,5/0,5; die drei Summen für den Löser.
                phiRadAW[h] = solRad * anteilAW + innenRad * anteilAW;
                phiRadIW[h] = solRad * anteilIW + innenRad * anteilIW;
                phiConv[h] = solLuft + innenKonv;

                // E5/E7 (G2) — Fenster θ_out + Δθ_lw nach Gl. (39); opake Flächen mit Schalter
                // θ_out + Δθ_lw + Δθ_kw nach Gl. (32), ohne Schalter θ_out. NULL-Regel: ohne
                // Gegenstrahlung ist Δθ_lw = 0. Die Außenwand des Klassenwegs hat keine
                // Orientierung; ihre Einstrahlung ist das Mittel der vier Fassaden, das Dach
                // liegt waagerecht und bekommt die Globalstrahlung (benannte Festlegung E5).
                double tOut = e.ThetaOut[h];
                double eA = GebaeudeKlimaweg.Gegenstrahlung(solarOrtszeit[h]);
                double tFenster = tOut + GebaeudeKlimaweg.DeltaThetaLangwellig(eA, tOut, GebaeudeFestwerte.SICHTFAKTOR_WAND);
                if (!e.AussenbauteileStrahlung)
                {
                    // Ohne Schalter dieselbe Bildung wie in G1 (bitgleich bei fehlender Gegenstrahlung).
                    thetaEq[h] = uaSumme > 0.0
                        ? (uaLuftseitig * tOut + uaGrund * e.ThetaGrund[h] + uaFenster * tFenster) / uaSumme
                        : tOut;
                }
                else
                {
                    double iWand = 0.25 * (s.Sued[h] + s.Ost[h] + s.West[h] + s.Nord[h]);
                    double iDach = Math.Max(solarOrtszeit[h].Globalstrahlung, 0.0);
                    double tSenkrecht = tOut + GebaeudeKlimaweg.DeltaThetaLangwellig(eA, tOut, GebaeudeFestwerte.SICHTFAKTOR_WAND)
                                        + GebaeudeKlimaweg.DeltaThetaKurzwellig(iWand, eA, tOut);
                    double tDach = tOut + GebaeudeKlimaweg.DeltaThetaLangwellig(eA, tOut, GebaeudeFestwerte.SICHTFAKTOR_DACH)
                                   + GebaeudeKlimaweg.DeltaThetaKurzwellig(iDach, eA, tOut);
                    thetaEq[h] = uaSumme > 0.0
                        ? (uaSenkrecht * tSenkrecht + uaDach * tDach + uaGrund * e.ThetaGrund[h] + uaFenster * tFenster) / uaSumme
                        : tOut;
                }
            }

            e.ThetaEq = thetaEq;
            e.PhiRadAW = phiRadAW;
            e.PhiRadIW = phiRadIW;
            e.PhiConv = phiConv;
            e.PhiSolar = phiSolar;

            // Anlagenkopplung (AK1): ob sie wirkt, entscheidet sich VOR dem Sollwertfahrplan —
            // das Zeitprogramm gilt nur mit ihr (4.3, F-A17). Ohne sie bleibt jede Zeile wie bisher.
            e.AnlagenkopplungStufe = anlagenkopplung;
            e.HeizkreisAktiv = gebaeude.Heizkreis_Aktiv;
            e.UebergabeArt = gebaeude.Uebergabe_Art;
            e.KopplungWirksam = Waermeuebergabe.KopplungWirksamFuer(gebaeude, anlagenkopplung);
            e.ThetaSoll = e.KopplungWirksam
                ? e.SollwertfahrplanMitProfil(gebaeude, wochenende)
                : Sollwertfahrplan(e, wochenende);

            // KU1 (Kühlkonzept 3.2, K11): Kühlsollwert und Kühlleistungsgrenze - nur mit
            // wirksamer Kühlung. Ohne sie gibt es keine obere Grenze (+∞): Das Gebäude läuft
            // frei, und die Raumluft darf über θ_max steigen (Entscheid E32).
            e.KuehlungAufloesen(gebaeude, kuehlbetrieb);
            e.ThetaMax = new double[8760];
            for (int h = 0; h < 8760; h++) e.ThetaMax[h] = e.KuehlSollwert;

            if (e.KopplungWirksam)
                e.KopplungAufloesen(gebaeude, uaLuftseitig, uaGrund, uaFenster, uaSumme,
                                    vorlaufAnlageC, nennleistungSkalierung);
            return e;
        }

        // =====================================================================
        //  Anlagenkopplung (AK1): Auslegungspunkt, hergeleitete Vorgaben, Vorlaufreihe
        // =====================================================================

        /// <summary>
        /// Löst die Wärmeübergabe eines gekoppelten Gebäudes auf (Anlagenkopplung 3.1, 3.4, 8.1,
        /// 8.4): Vorgaben der Übergabeart bei NULL, harte Prüfregeln mit benanntem Fehler (9.1,
        /// 9.5), die hergeleiteten Vorgaben — Auslegungs-Außentemperatur aus der Klimareihe
        /// (H10), Nennleistung aus der stationären Auslegungsheizlast des Katalogbaus (8.4, H7),
        /// Strahlungsanteil der Übergabeart (H12) — und die Vorlaufreihe aus Heizkurve oder
        /// festem Vorlauf. Die Umrechnung kW → W geschieht hier, einmal (3.3).
        /// </summary>
        private void KopplungAufloesen(ProjektGebaeudeModel g, double uaLuftseitig, double uaGrund,
                                       double uaFenster, double uaSumme, double vorlaufAnlageC,
                                       double nennleistungSkalierung)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            string art = g.Uebergabe_Art;
            if (!Waermeuebergabe.ArtBekannt(art))
                Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                       string.Format(k, MyResource.Resource.SIMENG_AK_UEBERGABEART_UNBEKANNT, art));

            double n = g.Uebergabe_Exponent ?? Waermeuebergabe.VorgabeExponent(art);
            Bereich(GebaeudeSchema.SPALTE_UEBERGABE_EXPONENT, n,
                    GebaeudeFestwerte.UEBERGABE_EXPONENT_MIN, GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX);

            double vN = g.Auslegung_Vorlauf ?? Waermeuebergabe.VorgabeVorlaufC(art);
            double rN = g.Auslegung_Ruecklauf ?? Waermeuebergabe.VorgabeRuecklaufC(art);
            double iN = g.Auslegung_Raumtemperatur ?? SollTag;
            if (g.Auslegung_Vorlauf.HasValue)
                Bereich(GebaeudeSchema.SPALTE_AUSLEGUNG_VORLAUF, vN,
                        GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MIN, GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MAX);
            if (g.Auslegung_Raumtemperatur.HasValue)
                Bereich(GebaeudeSchema.SPALTE_AUSLEGUNG_RAUMTEMPERATUR, iN,
                        GebaeudeFestwerte.AUSLEGUNG_RAUM_MIN, GebaeudeFestwerte.AUSLEGUNG_RAUM_MAX);
            if (!Endlich(vN) || !Endlich(iN) || !(vN > iN))
                Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                       string.Format(k, MyResource.Resource.SIMENG_AK_VORLAUF_UNTER_RAUM, Text(vN), Text(iN)));
            if (!Endlich(rN) || !(rN > iN) || !(rN < vN))
                Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                       string.Format(k, MyResource.Resource.SIMENG_AK_RUECKLAUF_AUSSERHALB, Text(rN), Text(iN), Text(vN)));

            // H12: Mit einer Übergabeart heißt ein leerer Strahlungsanteil „Vorgabe der Art".
            if (!g.Heizung_Strahlungsanteil.HasValue)
                HeizungStrahlungsanteil = Waermeuebergabe.VorgabeStrahlungsanteil(art);

            double xp = g.Regler_Proportionalband ?? GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K;
            Bereich(GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND, xp,
                    GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MIN_K, GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MAX_K);
            ReglerbandK = xp;

            HeizkurveAktiv = g.Heizkurve_Aktiv;
            double niveau = g.Heizkurve_Niveau ?? GebaeudeFestwerte.VORGABE_HEIZKURVE_NIVEAU_K;
            double steilheit = g.Heizkurve_Steilheit ?? GebaeudeFestwerte.VORGABE_HEIZKURVE_STEILHEIT;
            Bereich(GebaeudeSchema.SPALTE_HEIZKURVE_NIVEAU, niveau,
                    GebaeudeFestwerte.HEIZKURVE_NIVEAU_MIN, GebaeudeFestwerte.HEIZKURVE_NIVEAU_MAX);
            Bereich(GebaeudeSchema.SPALTE_HEIZKURVE_STEILHEIT, steilheit,
                    GebaeudeFestwerte.HEIZKURVE_STEILHEIT_MIN, GebaeudeFestwerte.HEIZKURVE_STEILHEIT_MAX);

            // H10: die Auslegungs-Außentemperatur — kältestes Tagesmittel der Klimareihe,
            // auf ganze Grad abgerundet; das Feld überschreibt.
            int auslegungstag = KaeltesterTag(ThetaOut, out double kaeltestesMittel);
            AuslegungAussentemperaturHergeleitet = !g.Auslegung_Aussentemperatur.HasValue;
            double aN = g.Auslegung_Aussentemperatur ?? Math.Floor(kaeltestesMittel);
            if (g.Auslegung_Aussentemperatur.HasValue)
                Bereich(GebaeudeSchema.SPALTE_AUSLEGUNG_AUSSENTEMPERATUR, aN,
                        GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MIN, GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MAX);
            if (!Endlich(aN) || !(aN < iN))
                Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                       string.Format(k, MyResource.Resource.SIMENG_AK_AUSSEN_NICHT_UNTER_RAUM, Text(aN), Text(iN)));
            AuslegungAussentemperaturC = aN;

            // 8.4: die Auslegungsheizlast — die stationäre Last des Katalogbaus bei aN und iN,
            // ohne solare und innere Lasten; die Grundfläche am Auslegungstag, die Fenster und
            // opaken Flächen ohne Strahlung (benannte Festlegung). Ein Aufruf des Lösers.
            double grundN = Grundtemperatur(auslegungstag, aN);
            double eqN = uaSumme > 0.0 ? (uaLuftseitig * aN + uaGrund * grundN + uaFenster * aN) / uaSumme : aN;
            AuslegungsheizlastW = new Zonenmodell2K(Parameter, Bezeichnung)
                .StationaereHeizlastW(iN, aN, eqN, HeizungStrahlungsanteil);

            // H7: Die Nennleistung gilt dem wirklichen Gebäude. Fest eingetragen wird sie auf den
            // Katalogbau umgerechnet; leer ist sie die Auslegungsheizlast des Katalogbaus - nach
            // der Nachmultiplikation (E8) also die skalierte Auslegungslast.
            double phiN;
            if (g.Uebergabe_Leistung_Nenn.HasValue && !double.IsNaN(nennleistungSkalierung))
            {
                double wertKw = g.Uebergabe_Leistung_Nenn.Value;
                if (!(wertKw > 0.0))
                    Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_NENNLEISTUNG_UNGUELTIG, Text(wertKw)));
                if (!(nennleistungSkalierung > 0.0) || double.IsInfinity(nennleistungSkalierung))
                    Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_SKALIERUNG_UNGUELTIG, Text(nennleistungSkalierung)));
                phiN = double.IsPositiveInfinity(wertKw) ? double.PositiveInfinity : 1000.0 * wertKw / nennleistungSkalierung;
                UebergabeNennleistungHergeleitet = false;
            }
            else
            {
                if (!(AuslegungsheizlastW > 0.0) || !Endlich(AuslegungsheizlastW))
                    Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_AUSLEGUNGSHEIZLAST_NICHT_POSITIV,
                                         Text(AuslegungsheizlastW), Text(aN), Text(iN)));
                phiN = AuslegungsheizlastW;
                UebergabeNennleistungHergeleitet = true;
            }

            Uebergabe = new Uebergabekennwerte(phiN, n, vN, rN, iN);
            Heizkurve = new Heizkurve(Uebergabe, aN, niveau, steilheit);

            // Schritt E (10.1): die Vorlaufreihe — Heizkurve je Stunde am Sollwert der Stunde
            // oder fester Vorlauf der Anlage (3.4 Punkt 4); ohne Anlagenwert der Auslegungsvorlauf.
            var vorlauf = new double[8760];
            if (HeizkurveAktiv)
            {
                Vorlaufquelle = Vorlaufquelle.Heizkurve;
                for (int h = 0; h < 8760; h++) vorlauf[h] = Heizkurve.VorlaufC(ThetaSoll[h], ThetaOut[h]);
            }
            else
            {
                bool anlage = Endlich(vorlaufAnlageC) && vorlaufAnlageC > 0.0;
                Vorlaufquelle = anlage ? Vorlaufquelle.Anlage : Vorlaufquelle.Auslegung;
                VorlaufFestC = anlage ? vorlaufAnlageC : vN;
                for (int h = 0; h < 8760; h++) vorlauf[h] = VorlaufFestC;
            }
            VorlaufC = vorlauf;
        }

        /// <summary>Der Tag (0 … 364) mit dem kältesten Tagesmittel der Außenluft und dieses Mittel [°C] (H10).</summary>
        internal static int KaeltesterTag(double[] thetaOut, out double tagesmittelC)
        {
            int tag = 0;
            tagesmittelC = double.PositiveInfinity;
            for (int d = 0; d < 365; d++)
            {
                double summe = 0.0;
                for (int s = 0; s < 24; s++) summe += thetaOut[d * 24 + s];
                double mittel = summe / 24.0;
                if (mittel < tagesmittelC)
                {
                    tagesmittelC = mittel;
                    tag = d;
                }
            }
            return tag;
        }

        /// <summary>
        /// Die Temperatur an der Grundfläche am Auslegungspunkt [°C]: Außenluft → θ_out,N,
        /// Keller → Kellertemperatur, Erdreich → Tagesmittel der Erdreichreihe am kältesten Tag.
        /// </summary>
        private double Grundtemperatur(int auslegungstag, double aussenN)
        {
            if (string.Equals(GrundRandbedingung, DbWerte.GRUND_AUSSENLUFT, StringComparison.Ordinal)) return aussenN;
            if (string.Equals(GrundRandbedingung, DbWerte.GRUND_KELLER, StringComparison.Ordinal)) return Kellertemperatur;
            double summe = 0.0;
            for (int s = 0; s < 24; s++) summe += ThetaGrund[auslegungstag * 24 + s];
            return summe / 24.0;
        }

        /// <summary>Harte Prüfregel eines Eingabewerts (9.1): benannter Fehler mit Spalte, Wert und Bereich.</summary>
        private void Bereich(string spalte, double wert, double min, double max)
        {
            if (!Endlich(wert) || wert < min || wert > max)
                Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                       string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_AK_WERT_AUSSERHALB,
                                     spalte, Text(wert), Text(min), Text(max)));
        }

        /// <summary>
        /// Der Sollwertfahrplan mit Anlagenkopplung (4.3, H8): Ist ein Wochenprofil gepflegt,
        /// gilt es — 168 Werte, Montag 00:00 bis Sonntag 23:00 im Ortszeit-Kalender des Laufs —,
        /// und die Ferienzeiträume wirken darüber mit dem Ferienwert. Ohne Profil der
        /// Bestandsfahrplan, unverändert. Der Leser ist streng (H-F10): falsche Wertzahl, keine
        /// Zahl oder ein Wert außerhalb der Plausibilitätsgrenze ist ein benannter Fehler.
        /// </summary>
        private double[] SollwertfahrplanMitProfil(ProjektGebaeudeModel g, bool[] wochenende)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            AnlagenkopplungSchema.Wochenprofil p = AnlagenkopplungSchema.WochenprofilLesen(g.Sollwertprofil);
            switch (p.Befund)
            {
                case AnlagenkopplungSchema.WochenprofilBefund.KeinProfil:
                    return Sollwertfahrplan(this, wochenende);
                case AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl:
                    Fehler(GebaeudeModellFehler.SollwertprofilUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_SOLLWERTPROFIL_WERTZAHL,
                                         p.Gefunden, AnlagenkopplungSchema.WOCHENWERTE));
                    break;
                case AnlagenkopplungSchema.WochenprofilBefund.KeineZahl:
                    Fehler(GebaeudeModellFehler.SollwertprofilUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_SOLLWERTPROFIL_KEINE_ZAHL, p.Stelle));
                    break;
            }

            double[] werte = p.Werte;
            for (int i = 0; i < werte.Length; i++)
                if (werte[i] < GebaeudeFestwerte.SOLLWERTPROFIL_MIN_C || werte[i] > GebaeudeFestwerte.SOLLWERTPROFIL_MAX_C)
                    Fehler(GebaeudeModellFehler.SollwertprofilUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_SOLLWERTPROFIL_WERT, i + 1, Text(werte[i]),
                                         Text(GebaeudeFestwerte.SOLLWERTPROFIL_MIN_C), Text(GebaeudeFestwerte.SOLLWERTPROFIL_MAX_C)));

            int wochentag0 = WochentagDesErstenTags(wochenende);
            if (wochentag0 < 0)
                Fehler(GebaeudeModellFehler.SollwertprofilUngueltig, MyResource.Resource.SIMENG_AK_SOLLWERTPROFIL_KALENDER);

            SollwertprofilWirksam = true;
            var soll = new double[8760];
            for (int h = 0; h < 8760; h++)
            {
                int tag = h / 24;
                soll[h] = Ferientage[tag]
                    ? SollFerien
                    : werte[((wochentag0 + tag) % 7) * 24 + h % 24];
            }
            return soll;
        }

        /// <summary>
        /// Der Wochentag des ersten Tags (Montag = 0 … Sonntag = 6), aus der Wochenendmaske des
        /// Ortszeit-Kalenders — derselben, nach der der Bestandsfahrplan das Wochenende setzt (U7).
        /// −1, wenn die Maske kein Wochenkalender ist (Samstag und Sonntag im Sieben-Tage-Takt).
        /// </summary>
        internal static int WochentagDesErstenTags(bool[] wochenende)
        {
            if (wochenende == null || wochenende.Length < 7) return -1;
            int ersterWe = Array.IndexOf(wochenende, true);
            if (ersterWe < 0 || ersterWe > 6) return -1;
            int w0 = ersterWe == 0 ? (wochenende[1] ? 5 : 6) : ((5 - ersterWe) % 7 + 7) % 7;
            for (int d = 0; d < wochenende.Length; d++)
                if (wochenende[d] != ((w0 + d) % 7 >= 5)) return -1;
            return w0;
        }

        /// <summary>
        /// Löst die Kühleingaben des Gebäudes auf (Stufe KU1, Kühlkonzept 3.2, 7.1): wirksam nur
        /// mit Projektschalter, <c>Kuehlung_Aktiv</c> und gesetztem Kühlsollwert. Dann gilt die
        /// harte Prüfregel θ_kuehl ≥ θ_soll,max + 1 K (Q18-Regel des Stundenwegs) — θ_soll,max ist
        /// der höchste Wert des Sollwertfahrplans, also auch Nacht, Wochenende und Ferien, soweit
        /// sie gelten —, und eine gesetzte Kühlleistungsgrenze muss größer null sein. Ohne
        /// wirksame Kühlung ist die obere Grenze +∞ (Entscheid E32): Der Löser kühlt nicht, das
        /// Gebäude läuft frei, und es entsteht keine Kühlreihe.
        /// </summary>
        private void KuehlungAufloesen(ProjektGebaeudeModel g, bool kuehlbetrieb)
        {
            KuehlungWirksam = kuehlbetrieb && g.Kuehlung_Aktiv && g.Kuehl_Sollwert.HasValue;
            if (!KuehlungWirksam)
            {
                KuehlSollwert = double.PositiveInfinity;
                KuehlleistungMaxW = double.NaN;
                return;
            }

            double soll = g.Kuehl_Sollwert.Value;
            double heizMax = double.NegativeInfinity;
            for (int h = 0; h < ThetaSoll.Length; h++)
                if (Endlich(ThetaSoll[h]) && ThetaSoll[h] > heizMax) heizMax = ThetaSoll[h];

            if (!Endlich(soll) || (Endlich(heizMax) && !(soll >= heizMax + GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K)))
                Fehler(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KUEHLSOLLWERT_UNTER_HEIZSOLLWERT,
                                  Text(soll), Text(heizMax), Text(GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K)));
            KuehlSollwert = soll;

            KuehlleistungMaxW = double.NaN;
            if (g.Kuehlleistung_Max.HasValue)
            {
                double grenzeW = 1000.0 * g.Kuehlleistung_Max.Value;
                if (!Endlich(grenzeW) || grenzeW <= 0.0)
                    Fehler(GebaeudeModellFehler.ParameterUngueltig,
                        "Die Kühlleistungsgrenze " + Text(grenzeW) + " W ist gesetzt, aber nicht größer null.");
                KuehlleistungMaxW = grenzeW;
            }
        }

        /// <summary>
        /// Die Gebäudedaten allein — aufgelöst mit den Vorgaben bei NULL und geprüft, ohne
        /// Klimareihen und ohne Ersatzparameter. Grundlage von <see cref="Bauen"/> und von
        /// <see cref="ErsatzparameterRC.AusKlassenweg"/>.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei einer verletzten Prüfung des Fahrplans,
        /// der Fensterflächen oder eines Modellparameters.</exception>
        internal static GebaeudeModellEingang Daten(ProjektGebaeudeModel g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            var e = new GebaeudeModellEingang
            {
                Bezeichnung = string.IsNullOrEmpty(g.Gebaeudename)
                    ? "Gebäude " + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture)
                    : g.Gebaeudename + " (" + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture) + ")",
                Nutzflaeche_M2 = g.Nutzflaeche,
                Raumhoehe_M = g.Raumhoehe,
                Bauweise_WhK = g.Bauweise,
                U_Aussenwand = g.k_Wert_Außenwand,
                U_Fenster = g.k_Wert_Fenster,
                U_Dach = g.k_Wert_Dachflaeche,
                U_Grund = g.k_Wert_Grundflaeche,
                U_Sonstige = g.k_Wert_Sonstiges,
                A_Aussenwand_M2 = g.Flaeche_Außenwand,
                A_Fenster_M2 = g.gesamte_Fensterflaeche,
                A_Dach_M2 = g.Dachflaeche,
                A_Grund_M2 = g.Grundflaeche,
                A_Sonstige_M2 = g.Sonstige_Flaechen,
                A_FensterSued_M2 = g.Fensterflaeche_Sued,
                A_FensterNord_M2 = g.Fensterflaeche_Nord,
                GWert = g.Fensterdurchlassgrad,
                Rahmenanteil = g.Rahmenanteil ?? GebaeudeFestwerte.VORGABE_RAHMENANTEIL,
                Verschattungsfaktor = g.Verschattungsfaktor ?? GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR,
                InnereGewinne_W = g.Interne_Waermegewinne,
                SollTag = g.Raumsolltemperatur_Tag,
                SollNacht = g.Raumsolltemperatur_Nachtabsenkung,
                SollWochenende = g.Raumsolltemperatur_Wochenende,
                SollFerien = g.Raumsolltemperatur_Ferien,
                ThetaMaxWert = g.Maximaleraumtemperatur,
                MasseanteilAussen = g.Masseanteil_Aussen ?? GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN,
                Innenflaechenfaktor = g.Innenflaechenfaktor ?? GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR,
                HeizungStrahlungsanteil = g.Heizung_Strahlungsanteil ?? GebaeudeFestwerte.VORGABE_HEIZUNG_STRAHLUNGSANTEIL,
                HeizleistungMaxW = g.Heizleistung_Max.HasValue ? 1000.0 * g.Heizleistung_Max.Value : double.NaN,
                GrundRandbedingung = string.IsNullOrEmpty(g.Grundflaeche_Randbedingung)
                    ? DbWerte.GRUND_ERDREICH : g.Grundflaeche_Randbedingung,
                Kellertemperatur = g.Kellertemperatur ?? GebaeudeFestwerte.VORGABE_KELLERTEMPERATUR,
                AussenbauteileStrahlung = g.Aussenbauteile_Strahlung,
                Sommerlueftung = g.Sommerlueftung,
            };

            // Lüftung (G2): Infiltration + Nutzerlüftung, sonst Luftwechselrate, sonst Vorgabe.
            e.Luftwechselrate_h = Gebaeudemodellvorgaben.WirksamerLuftwechsel(
                g.Luftwechselrate, g.Luftwechsel_Infiltration, g.Luftwechsel_Nutzer, out Luftwechselherkunft herkunft);
            e.LuftwechselHerkunft = herkunft;
            if (g.Luftwechsel_Infiltration is double nInf && (!Endlich(nInf) || nInf <= 0.0))
                e.Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Infiltration " + Text(nInf) + " 1/h ist nicht größer null.");
            if (g.Luftwechsel_Nutzer is double nNutz && (!Endlich(nNutz) || nNutz < 0.0))
                e.Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Nutzerlüftung " + Text(nNutz) + " 1/h ist negativ oder nicht endlich.");
            double zusatzN = GebaeudeFestwerte.SOMMERLUEFTUNG_LUFTWECHSEL - e.Luftwechselrate_h;
            e.SommerlueftungZusatzleitwertWK = e.Sommerlueftung && zusatzN > 0.0 && Endlich(zusatzN)
                ? zusatzN * e.Nutzflaeche_M2 * e.Raumhoehe_M * GebaeudeFestwerte.C_RHO_LUFT
                : 0.0;

            // Ost/West: die NULL-Vorgabe aus dem Bestandsfeld bildet der Vorbereitungsschritt.
            GebaeudeVorbereitung.FensterflaechenOstWest(g, out double ost, out double west);
            e.A_FensterOst_M2 = ost;
            e.A_FensterWest_M2 = west;

            double psiL = g.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand * g.Abmessung_Anschluß_Fenster_Wand
                        + g.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach * g.Abmessung_Anschluß_Wand_Dach
                        + g.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke * g.Abmessung_Anschluß_Außenwand_Kellerdecke;
            e.SummePsiL_WK = psiL;

            e.Pruefen(g);
            e.Ferientage = Ferienfahrplan(g, e.Bezeichnung);
            return e;
        }

        // =====================================================================
        //  Prüfungen außerhalb des Klassenwegs (Konzept 4.8, Rechenschritte 1.1)
        // =====================================================================

        private void Pruefen(ProjektGebaeudeModel g)
        {
            if (!(g.Flaeche_Nutzer > 0.0) || double.IsInfinity(g.Flaeche_Nutzer))
                Fehler(GebaeudeModellFehler.PflichtgroesseFehlt, "Die Fläche je Nutzer ist nicht größer null (" + Text(g.Flaeche_Nutzer) + " m²).");

            // Fensterflächen je Orientierung: nicht negativ, Summe = gesamte Fensterfläche.
            double[] fenster = { A_FensterSued_M2, A_FensterOst_M2, A_FensterWest_M2, A_FensterNord_M2 };
            foreach (double f in fenster)
                if (!Endlich(f) || f < 0.0)
                    Fehler(GebaeudeModellFehler.FensterflaechenWidersprechen, "Eine Fensterfläche je Orientierung ist negativ oder nicht endlich (" + Text(f) + " m²).");
            double summe = A_FensterSued_M2 + A_FensterOst_M2 + A_FensterWest_M2 + A_FensterNord_M2;
            if (!Endlich(A_Fenster_M2) || Math.Abs(summe - A_Fenster_M2) > GebaeudeFestwerte.FENSTERSUMME_TOLERANZ_M2)
                Fehler(GebaeudeModellFehler.FensterflaechenWidersprechen,
                    "Die Fensterflächen je Orientierung ergeben " + Text(summe) + " m², die gesamte Fensterfläche ist " +
                    Text(A_Fenster_M2) + " m².");

            if (!Endlich(Rahmenanteil) || Rahmenanteil < 0.0 || Rahmenanteil >= 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Rahmenanteil " + Text(Rahmenanteil) + " liegt nicht in [0, 1).");
            if (!Endlich(Verschattungsfaktor) || Verschattungsfaktor <= 0.0 || Verschattungsfaktor > 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Verschattungsfaktor " + Text(Verschattungsfaktor) + " liegt nicht in (0, 1].");
            if (!Endlich(MasseanteilAussen) || MasseanteilAussen <= 0.0 || MasseanteilAussen >= 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Masseanteil außen " + Text(MasseanteilAussen) + " liegt nicht in (0, 1).");
            if (!Endlich(Innenflaechenfaktor) || Innenflaechenfaktor <= 0.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Innenflächenfaktor " + Text(Innenflaechenfaktor) + " ist nicht größer null.");
            if (!Endlich(HeizungStrahlungsanteil) || HeizungStrahlungsanteil < 0.0 || HeizungStrahlungsanteil > 1.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Der Strahlungsanteil der Heizung " + Text(HeizungStrahlungsanteil) + " liegt nicht in [0, 1].");
            if (!double.IsNaN(HeizleistungMaxW) && (!Endlich(HeizleistungMaxW) || HeizleistungMaxW <= 0.0))
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Heizleistungsgrenze " + Text(HeizleistungMaxW) + " W ist gesetzt, aber nicht größer null.");
            if (!Endlich(Kellertemperatur))
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Kellertemperatur ist nicht endlich.");
            if (!Endlich(InnereGewinne_W) || InnereGewinne_W < 0.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die inneren Wärmegewinne " + Text(InnereGewinne_W) + " W sind negativ oder nicht endlich.");
            if (!Endlich(SummePsiL_WK) || SummePsiL_WK < 0.0)
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Wärmebrücken Σψ·L = " + Text(SummePsiL_WK) + " W/K sind negativ oder nicht endlich.");
            if (!string.Equals(GrundRandbedingung, DbWerte.GRUND_ERDREICH, StringComparison.Ordinal)
                && !string.Equals(GrundRandbedingung, DbWerte.GRUND_KELLER, StringComparison.Ordinal)
                && !string.Equals(GrundRandbedingung, DbWerte.GRUND_AUSSENLUFT, StringComparison.Ordinal))
                Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Randbedingung der Grundfläche '" + GrundRandbedingung + "' ist unbekannt.");

            if (!Endlich(SollTag) || !Endlich(SollNacht) || !Endlich(SollWochenende) || !Endlich(SollFerien) || !Endlich(ThetaMaxWert))
                Fehler(GebaeudeModellFehler.SollwertfahrplanUngueltig, "Ein Sollwert ist nicht endlich.");
            if (!(ThetaMaxWert > SollTag))
                Fehler(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                    "Die obere Raumtemperatur " + Text(ThetaMaxWert) + " °C liegt nicht über dem Tagsollwert " + Text(SollTag) + " °C.");
        }

        /// <summary>
        /// Die Ferientage (E8): Der Fahrplan ist aktiv, wenn <c>Ferien</c> über 0,9 und der
        /// Feriensollwert mindestens 1 °C ist. Ein Zeitraum mit 0 oder 366 an einer Grenze ist
        /// „aus"; ein anderer Tag außerhalb 1 … 365 in einem aktiven Fahrplan wird benannt
        /// abgelehnt. Beginn nach Ende heißt Jahreswechsel.
        /// </summary>
        private static bool[] Ferienfahrplan(ProjektGebaeudeModel g, string bezeichnung)
        {
            var tage = new bool[365];
            bool aktiv = g.Ferien > GebaeudeFestwerte.FERIEN_FLAG_SCHWELLE
                         && g.Raumsolltemperatur_Ferien >= GebaeudeFestwerte.FERIEN_SOLLWERT_MIN;
            if (!aktiv) return tage;

            double[,] zeitraeume =
            {
                { g.Ferienbeginn_1, g.Ferienende_1 }, { g.Ferienbeginn_2, g.Ferienende_2 },
                { g.Ferienbeginn_3, g.Ferienende_3 }, { g.Ferienbeginn_4, g.Ferienende_4 },
            };
            for (int k = 0; k < 4; k++)
            {
                double b = zeitraeume[k, 0], en = zeitraeume[k, 1];
                if (Aus(b) || Aus(en)) continue;
                if (!Tag(b) || !Tag(en))
                    throw new GebaeudeModellException(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                        bezeichnung + ": Der Ferienzeitraum " + (k + 1).ToString(CultureInfo.InvariantCulture) + " (" +
                        Text(b) + " … " + Text(en) + ") hat einen Tag außerhalb 1 … 365.");
                int ib = (int)b - 1, ie = (int)en - 1;
                if (ib <= ie)
                    for (int d = ib; d <= ie; d++) tage[d] = true;
                else
                {
                    for (int d = ib; d < 365; d++) tage[d] = true;
                    for (int d = 0; d <= ie; d++) tage[d] = true;
                }
            }
            return tage;
        }

        private static bool Aus(double tag) => tag == 0.0 || tag == 366.0;

        private static bool Tag(double tag) => Endlich(tag) && tag >= 1.0 && tag <= 365.0 && tag == Math.Floor(tag);

        /// <summary>Der Sollwertfahrplan (E8): Ferien vor Wochenende vor Tag/Nacht.</summary>
        private static double[] Sollwertfahrplan(GebaeudeModellEingang e, bool[] wochenende)
        {
            var soll = new double[8760];
            bool weWirksam = e.SollWochenende > GebaeudeFestwerte.WOCHENENDE_SOLLWERT_SCHWELLE;
            for (int h = 0; h < 8760; h++)
            {
                int tag = h / 24;
                if (e.Ferientage[tag]) soll[h] = e.SollFerien;
                else if (weWirksam && wochenende[tag]) soll[h] = e.SollWochenende;
                else if (Nutzungszeit(h)) soll[h] = e.SollTag;
                else soll[h] = e.SollNacht;
            }
            return soll;
        }

        private void Fehler(GebaeudeModellFehler grund, string text)
        {
            throw new GebaeudeModellException(grund, Bezeichnung + ": " + text);
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);

        private static string Text(double w) => w.ToString("G6", CultureInfo.InvariantCulture);
    }
}
