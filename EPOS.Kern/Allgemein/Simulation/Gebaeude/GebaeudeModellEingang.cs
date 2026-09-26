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
    /// <para><b>Zwei Wege nach Datenlage</b> (Stufe G3, A14/E27): Ohne Zone
    /// (<see cref="ProjektGebaeudeModel.Zonen"/> leer) rechnet der Klassenweg — bitgleich wie vor
    /// G3. Mit genau einer Zone rechnet der Bauteilweg: Ersatzparameter aus den Bauteilen
    /// (<see cref="ErsatzparameterRC.AusBauteilweg(GebaeudeModellEingang, IReadOnlyList{BauteilEingang})"/>),
    /// solare Gewinne und äquivalente Außentemperatur je Bauteil
    /// (<see cref="BauteilwegAussenseite"/>); die Nutzfläche der Zone mit ihrem Flächenschlüssel
    /// (<see cref="Flaechenschluessel"/>: Luftvolumen, Speichermasse der Bauweise, innere
    /// Gewinne, f_IW·A_f); alles Übrige — Sollwerte, Raumhöhe, Luftwechsel, Leistungsgrenzen,
    /// Kühlung, Übergabe — bleibt das der Gebäudezeile.</para>
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

        /// <summary>Nutzfläche A_f [m²] (E13): des Katalogbaus, mit Zone die der Zone (<see cref="Flaechenschluessel"/>).</summary>
        internal double Nutzflaeche_M2 { get; private set; }
        /// <summary>Raumhöhe H [m].</summary>
        internal double Raumhoehe_M { get; private set; }
        /// <summary>Speichermasse C_ges [Wh/K] (<c>Bauweise</c>); mit Zone nach dem Flächenschlüssel.</summary>
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
        /// <summary>Innere Wärmegewinne [W], zeitlich konstant (G1): des Katalogbaus, mit Zone nach dem Flächenschlüssel.</summary>
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

        /// <summary>
        /// Die Nachtzeit des Gebäudes (Entscheid E43): <c>Nachtabsenkung_Beginn</c>/<c>_Ende</c>, beide
        /// leer = <see cref="Nachtzeit.Vorgabe"/> (22 bis 6 Uhr, der Fahrplan nach E8). Sie trennt im
        /// Sollwertfahrplan Tag- und Nachtsollwert und bestimmt die Nutzungszeit der Kennzahlen.
        /// </summary>
        internal Nachtzeit Nachtzeit { get; private set; } = Nachtzeit.Vorgabe;

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
        //  Anlagenkopplung, Kälteseite (E37; Konzept Anlagenkopplung 7.2, 8.1, 8.4, 10.5)
        // =====================================================================

        /// <summary>Der Schalter <c>Kuehluebergabe_Aktiv</c> des Gebäudes (A1) — die Absicht, unabhängig von der Projektstufe.</summary>
        internal bool KuehluebergabeAktiv { get; private set; }

        /// <summary>Die Kühlübergabeart des Gebäudes (<c>DbWerte.KUEHLUEBERGABE_*</c>), wie gelesen; <c>null</c> = ideal.</summary>
        internal string KuehlUebergabeArt { get; private set; }

        /// <summary>
        /// <b>Ist die Kälteseite der Kopplung für dieses Gebäude wirksam?</b> Projektstufe AK1 oder
        /// höher, wirksame Kühlung (E32), <c>Kuehluebergabe_Aktiv</c> und eine Kühlübergabeart
        /// ungleich ideal (<see cref="Kuehluebergabe.KopplungWirksam"/>) — unabhängig vom
        /// Heizkreis. Sonst rechnet die Kühlung wie bisher, byte-gleich.
        /// </summary>
        internal bool KuehlKopplungWirksam { get; private set; }

        /// <summary>
        /// Die Kennwerte der Kühlübergabe, wie eingegeben (V &lt; R &lt; θ_i,N) — für Anzeige und
        /// Ergebnis (Φ_N, n, Auslegungspunkt); Spreizung, Übertemperatur und W stehen hier mit
        /// negativem Vorzeichen, gerechnet wird allein mit <see cref="KuehlUebergabeGespiegelt"/>.
        /// <c>null</c> ohne wirksame Kälteseite.
        /// </summary>
        internal Uebergabekennwerte KuehlUebergabe { get; private set; }

        /// <summary>Dieselben Kennwerte gespiegelt (−V, −R, −θ_i,N) — so rechnet Schritt K (10.5).</summary>
        internal Uebergabekennwerte KuehlUebergabeGespiegelt { get; private set; }

        /// <summary>Der feste Kaltwasser-Vorlauf am Gebäude [°C] = max(Quelle, Vorlaufgrenze) (7.2); NaN ohne Kälteseite.</summary>
        internal double KuehlVorlaufC { get; private set; } = double.NaN;

        /// <summary>Der Kaltwasser-Vorlauf der Quelle [°C] vor dem Hochmischen: der Anlage oder, ohne sie, der Auslegung.</summary>
        internal double KuehlVorlaufQuelleC { get; private set; } = double.NaN;

        /// <summary>Woher der Kaltwasser-Vorlauf kommt: <see cref="Vorlaufquelle.Anlage"/> oder <see cref="Vorlaufquelle.Auslegung"/>.</summary>
        internal Vorlaufquelle KuehlVorlaufquelle { get; private set; }

        /// <summary>Steht der Vorlauf an der Vorlaufgrenze, weil die Quelle kälter liefert (Mischgruppe am Gebäude, 7.2)?</summary>
        internal bool KuehlVorlaufGekappt { get; private set; }

        /// <summary>Die Vorlaufgrenze [°C] — Vorgabe statt Taupunktrechnung; NaN = keine (Gebläsekonvektor).</summary>
        internal double KuehlVorlaufgrenzeC { get; private set; } = double.NaN;

        /// <summary>Liegt die Vorlaufgrenze über dem Auslegungsvorlauf? Dann wird die Nennleistung nie erreicht (Hinweis).</summary>
        internal bool KuehlGrenzeUeberAuslegung { get; private set; }

        /// <summary>Strahlungsanteil der Kühlübergabe [–] — Vorgabe der Art, ohne Spalte (KU 3.2).</summary>
        internal double KuehlStrahlungsanteil { get; private set; }

        /// <summary>
        /// Die Kühllast des Auslegungstags [W] (A2, 8.4) — die Spitze des periodisch
        /// eingeschwungenen wärmsten Tags bei idealer Kühlung; NaN, wenn nicht hergeleitet.
        /// Ausdrücklich kein Normnachweis.
        /// </summary>
        internal double AuslegungskuehllastW { get; private set; } = double.NaN;

        /// <summary>Der Auslegungstag der Kühlung (0 … 364): der Tag mit dem höchsten Tagesmittel der Außenluft; −1 ohne Herleitung.</summary>
        internal int AuslegungstagKuehlung { get; private set; } = -1;

        /// <summary>Das Tagesmittel der Außenluft am Auslegungstag der Kühlung [°C]; NaN ohne Herleitung.</summary>
        internal double AuslegungstagKuehlungMittelC { get; private set; } = double.NaN;

        /// <summary>Ist die Nennleistung der Kühlübergabe aus dem Auslegungstag hergeleitet (NULL, A2)?</summary>
        internal bool KuehlNennleistungHergeleitet { get; private set; }

        // =====================================================================
        //  Ersatzparameter und Randreihen (Schritte A und E)
        // =====================================================================

        /// <summary>Die RC-Größen — des Klassenwegs (Schritt A) oder, mit Zone, des Bauteilwegs (Schritt B).</summary>
        internal ErsatzparameterRC Parameter { get; private set; }

        /// <summary>
        /// Die Zone, mit der das Gebäude den Bauteilweg rechnet (Stufe G3, A14/E27); <c>null</c> =
        /// keine Zone, Klassenweg. G3 liest von ihr die Bauteile und die Nutzfläche, der die
        /// flächenbezogenen Größen des Gebäudes folgen (<see cref="Flaechenschluessel"/>);
        /// Sollwerte, Raumhöhe, Luftwechsel und Leistungsgrenzen bleiben die der Gebäudezeile
        /// (<see cref="GebaeudeZonensatz"/>).
        /// </summary>
        internal GebaeudeZonensatz Zone { get; private set; }

        /// <summary>
        /// Die Bauteile, mit denen der Bauteilweg rechnet: die der Zone, Fenster mit g-Wert,
        /// Rahmenanteil und Verschattung des Gebäudes aufgefüllt, wo sie NaN tragen
        /// (<see cref="BauteilEingang.MitGebaeudewerten"/>); in der Reihenfolge der Zone.
        /// <c>null</c> im Klassenweg.
        /// </summary>
        internal IReadOnlyList<BauteilEingang> Bauteile { get; private set; }

        /// <summary>Rechnet das Gebäude den Bauteilweg (genau eine Zone)?</summary>
        internal bool Bauteilweg => Zone != null;

        /// <summary>
        /// Die Zahl der Zonen des Gebäudes, zu dem dieser Eingang gehört (Stufe G6b): 0 im Klassenweg,
        /// 1 für die eine Zone des Bauteilwegs, ab 2 im Mehrzonenweg (<see cref="ZonenEingang"/>).
        /// </summary>
        internal int Zonenzahl { get; private set; }

        /// <summary>Rechnet dieser Eingang eine Zone eines Mehrzonengebäudes (Stufe G6b, N ≥ 2)?</summary>
        internal bool Mehrzonenweg => Zonenzahl >= 2;

        /// <summary>
        /// Die Vorgabenkaskade der Zone (<see cref="Zonenvorgaben"/>, Stufe G6b, Anwenderentscheid
        /// A5 (a)) — dieselbe Funktion, die der Zonendialog für „Vorgabe: …" ruft; <c>null</c> im
        /// Klassenweg.
        /// </summary>
        internal Zonenvorgaben Vorgaben { get; private set; }

        /// <summary>
        /// Das Luftvolumen der Zone [m³] (<c>Tab_Zone.Volumen</c>, Stufe G6b): gesetzt nur, wenn die
        /// Zone es selbst trägt; NaN = Nutzfläche × Raumhöhe (<see cref="Lueftungsleitwert_WK"/>).
        /// </summary>
        internal double Luftvolumen_M3 { get; private set; } = double.NaN;

        /// <summary>
        /// Wird die Zone beheizt (Festlegung 2 des Auftrags G6b)? Im Klassenweg und ohne Eingabe ja;
        /// <c>false</c> heißt frei schwingend: kein Heizen, kein Kühlen, kein Sollwert
        /// (<see cref="ThetaSoll"/> NaN, <see cref="ThetaMax"/> +∞).
        /// </summary>
        internal bool IstBeheizt { get; private set; } = true;

        /// <summary>
        /// Wäre die Anlagenkopplung für dieses Gebäude wirksam, rechnet aber als ideale Regelung, weil
        /// die Zone zu einem Mehrzonengebäude gehört (Anwenderentscheid A4 (a): AK1 bei N ≥ 2 als ideale
        /// Last, mit Warnung im Protokoll)? Heiz- oder Kälteseite.
        /// </summary>
        internal bool KopplungAlsIdealeLast { get; private set; }

        /// <summary>
        /// Der Leitwert der Lüftung H_ve [W/K] (Rechenschritte A7; Stufe G6b): n·V·c·ρ mit dem
        /// Luftvolumen der Zone, ohne es n·A_f·H·c·ρ — in dieser Reihenfolge, damit eine Zone ohne
        /// eigenes Volumen bitgleich wie vorher rechnet.
        /// </summary>
        internal double Lueftungsleitwert_WK => double.IsNaN(Luftvolumen_M3)
            ? Luftwechselrate_h * Nutzflaeche_M2 * Raumhoehe_M * GebaeudeFestwerte.C_RHO_LUFT
            : Luftwechselrate_h * Luftvolumen_M3 * GebaeudeFestwerte.C_RHO_LUFT;

        /// <summary>Die Kühlleistungsgrenze der Zone [kW] aus der Kaskade, wenn sie vom Gebäude abweicht (anteilig ab zwei Zonen, Festlegung 5); sonst <c>null</c>.</summary>
        private double? _kuehlgrenzeZoneKw;

        /// <summary>Trägt die Zone eigene innere Gewinne? Dann schlüsselt der Flächenschlüssel sie nicht.</summary>
        private bool _innereGewinneAusZone;

        /// <summary>
        /// <b>Die Werte der Zone</b> (Stufe G6b, Welle W3; Anwenderentscheid A5 (a): eine Regel für jede
        /// Zahl der Zonen) — aus der Vorgabenkaskade (<see cref="Zonenvorgaben.Bilden(Zoneneingaben, Gebaeudevorgaben, int)"/>).
        /// Überschrieben wird allein, was die Zone selbst trägt, und ab zwei Zonen die anteiligen
        /// Leistungsgrenzen (Festlegung 5); jeder geerbte Wert bleibt der Wert, den
        /// <see cref="Daten"/> gebildet hat — so rechnet eine Zone ohne eigene Werte bitgleich wie
        /// vorher. Raumhöhe, Volumen, die vier Sollwerte, θ_max, Strahlungsanteil der Heizung,
        /// Heizleistungsgrenze, Infiltration und Nutzerlüftung, eigene innere Gewinne und „beheizt";
        /// danach gelten die Prüfungen der Gebäudedaten für die Werte der Zone. Die Nachtzeit kommt
        /// vom Gebäude (Festlegung 1), die Kühlwerte ebenso (A4 (a)).
        /// </summary>
        private void Zonenwerte(ProjektGebaeudeModel g, int zonenzahl)
        {
            Zonenvorgaben v = Zonenvorgaben.Bilden(Zone.EingabenOderNutzflaeche(), Gebaeudevorgaben.Aus(g), zonenzahl);
            Vorgaben = v;

            if (AusZone(v.Raumhoehe)) Raumhoehe_M = v.Raumhoehe.Wert.Value;
            if (AusZone(v.Volumen)) Luftvolumen_M3 = v.Volumen.Wert.Value;
            if (AusZone(v.SollTag)) SollTag = v.SollTag.Wert.Value;
            if (AusZone(v.SollNacht)) SollNacht = v.SollNacht.Wert.Value;
            if (AusZone(v.SollWochenende)) SollWochenende = v.SollWochenende.Wert.Value;
            if (AusZone(v.SollFerien)) SollFerien = v.SollFerien.Wert.Value;
            if (AusZone(v.Maximaleraumtemperatur)) ThetaMaxWert = v.Maximaleraumtemperatur.Wert.Value;
            if (AusZone(v.HeizungStrahlungsanteil)) HeizungStrahlungsanteil = v.HeizungStrahlungsanteil.Wert.Value;
            if (AusZone(v.HeizleistungMaxKw) || v.HeizleistungMaxKw.Herkunft == Vorgabeherkunft.GebaeudeAnteilig)
                HeizleistungMaxW = 1000.0 * v.HeizleistungMaxKw.Wert.Value;
            if (v.KuehlleistungMaxKw.Herkunft == Vorgabeherkunft.GebaeudeAnteilig)
                _kuehlgrenzeZoneKw = v.KuehlleistungMaxKw.Wert.Value;
            if (AusZone(v.InterneWaermegewinne))
            {
                InnereGewinne_W = v.InterneWaermegewinne.Wert.Value;
                _innereGewinneAusZone = true;
            }
            if (AusZone(v.LuftwechselInfiltration) || AusZone(v.LuftwechselNutzer))
            {
                double? nInf = v.LuftwechselInfiltration.Wert, nNutz = v.LuftwechselNutzer.Wert;
                Luftwechselrate_h = Gebaeudemodellvorgaben.WirksamerLuftwechsel(g.Luftwechselrate, nInf, nNutz, out Luftwechselherkunft herkunft);
                LuftwechselHerkunft = herkunft;
                if (nInf is double i && (!Endlich(i) || i <= 0.0))
                    Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Infiltration " + Text(i) + " 1/h der Zone ist nicht größer null.");
                if (nNutz is double n && (!Endlich(n) || n < 0.0))
                    Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die Nutzerlüftung " + Text(n) + " 1/h der Zone ist negativ oder nicht endlich.");
            }
            IstBeheizt = v.IstBeheizt;
            if (!IstBeheizt && zonenzahl < 2)
                Fehler(GebaeudeModellFehler.KeineBeheizteZone,
                       string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G6_KEINE_BEHEIZTE_ZONE, Zone.Bezeichnung));

            // Die Prüfungen der Gebäudedaten gelten für die Werte der Zone; ohne eigene Werte
            // prüfen sie dasselbe noch einmal.
            if (!double.IsNaN(Luftvolumen_M3) && (!(Luftvolumen_M3 > 0.0) || double.IsInfinity(Luftvolumen_M3)))
                Fehler(GebaeudeModellFehler.PflichtgroesseFehlt, "Das Volumen " + Text(Luftvolumen_M3) + " m³ der Zone ist nicht größer null.");
            if (AusZone(v.Raumhoehe) && (!(Raumhoehe_M > 0.0) || double.IsInfinity(Raumhoehe_M)))
                Fehler(GebaeudeModellFehler.PflichtgroesseFehlt, "Die Raumhöhe " + Text(Raumhoehe_M) + " m der Zone ist nicht größer null.");
            Pruefen(g);
            SommerlueftungBilden();
        }

        /// <summary>Trägt die Zone den Wert selbst?</summary>
        private static bool AusZone(Vorgabewert w) => w.Herkunft == Vorgabeherkunft.Zone && w.Wert.HasValue;

        /// <summary>
        /// Eine unbeheizte Zone schwingt frei (Festlegung 2 des Auftrags G6b): kein Sollwert, keine
        /// Kühlung, keine Grenzen — nach dem Fahrplan und der Kühlung gebildet, damit deren
        /// Prüfungen für die Werte der Zone gelaufen sind.
        /// </summary>
        private void FreiSchwingend()
        {
            for (int h = 0; h < ThetaSoll.Length; h++) ThetaSoll[h] = double.NaN;
            KuehlungWirksam = false;
            KuehlSollwert = double.PositiveInfinity;
            KuehlleistungMaxW = double.NaN;
            HeizleistungMaxW = double.NaN;
        }

        // =====================================================================
        //  Kopplung der Zonen (Stufe G6b, Welle W3; Mehrzonenkonzept 2.3, 2.7)
        // =====================================================================

        /// <summary>
        /// Der Luftaustausch dieser Zone mit ihren Nachbarzonen (Stufe G6b): je Nachbar der Leitwert
        /// G = c·ρ·V̇; leer ohne Luftstrom. Aus den Paaren von <c>Tab_Zonenluftstrom</c>, der Gegenstrom
        /// gleicher Größe entsteht von selbst (Mehrzonenkonzept 2.7).
        /// </summary>
        internal IReadOnlyList<Luftkopplung> Luftkopplungen { get; private set; } = Array.Empty<Luftkopplung>();

        /// <summary>Σ G_zj [W/K] des Luftaustauschs; im Mehrzonenweg Teil von R_ext = 1/(H_ve + Σψ·L + Σ G_zj).</summary>
        internal double LuftaustauschLeitwert_WK { get; private set; }

        /// <summary>
        /// Die Nachbarglieder der äquivalenten Außentemperatur (Stufe G6b; Gl. (41)/(42)): je
        /// Nachbarzone das U·A ihrer Trennflächen in der Außen- und Fenstergruppe, in der Reihenfolge
        /// ihres ersten Auftretens; leer ohne koppelnde Trennfläche. Sie stehen im Nenner
        /// <see cref="UaSummeGewichtung_WK"/> hinter den Außengliedern.
        /// </summary>
        internal IReadOnlyList<Nachbarglied> Nachbarglieder { get; private set; } = Array.Empty<Nachbarglied>();

        /// <summary>
        /// Der Zähler der Außenglieder von θ_eq je Stunde [K·W/K] (Gl. (41), Stufe G6b): Σ U·A·θ_A,eq
        /// über die Glieder an Außenluft, Erdreich und unbeheiztem Raum, in der Reihenfolge des
        /// Bauteilwegs; <c>null</c> im Klassenweg. Ohne Nachbarglieder ist θ_eq = Zähler / Nenner.
        /// </summary>
        internal double[] ThetaEqZaehler { get; private set; }

        /// <summary>Der Nenner von θ_eq, Σ U·A über alle Glieder der Gewichtung samt Nachbargliedern [W/K]; NaN im Klassenweg.</summary>
        internal double UaSummeGewichtung_WK { get; private set; } = double.NaN;

        /// <summary>
        /// Die Summe der Gewichte B_v = U_v·A_v / Σ(U·A) über alle Glieder (Gl. (42)) — die stehende
        /// Zusicherung Σ B_v = 1 (Mehrzonenkonzept 2.3, Probe 9); NaN im Klassenweg und ohne Glied.
        /// </summary>
        internal double SummeGewichte { get; private set; } = double.NaN;

        /// <summary>
        /// Die Randbedingung der Stunde <paramref name="h"/> mit übergebener äquivalenter
        /// Außentemperatur <paramref name="thetaEq"/> und Zulufttemperatur <paramref name="thetaLue"/>
        /// (Stufe G6b, Welle W3) — der Weg der Zonenschleife: θ_eq samt Nachbargliedern
        /// (<see cref="ZonenEingang.ThetaEq"/>) und θ_Lue samt Luftaustausch
        /// (<see cref="ZonenEingang.ThetaLue"/>). Dieselben drei Zweige wie
        /// <see cref="Rand(int, bool)"/>, das wörtlich bleibt; im Löser wirkt die Zulufttemperatur als
        /// <c>gExt·ThetaOut</c>.
        /// </summary>
        internal Stundenrand Rand(int h, bool sommerlueftung, double thetaEq, double thetaLue)
        {
            if (!KuehlKopplungWirksam)
            {
                if (!KopplungWirksam)
                    return new Stundenrand(thetaLue, thetaEq, ThetaSoll[h], ThetaMax[h],
                                           PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                           heizleistungMaxW: HeizleistungMaxW,
                                           kuehlleistungMaxW: KuehlleistungMaxW,
                                           heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                           zusatzleitwertWK: sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0);
                return new Stundenrand(thetaLue, thetaEq, ThetaSoll[h], ThetaMax[h],
                                       PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                       heizleistungMaxW: HeizleistungMaxW,
                                       kuehlleistungMaxW: KuehlleistungMaxW,
                                       heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                       zusatzleitwertWK: sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0,
                                       uebergabe: Uebergabe,
                                       vorlaufC: VorlaufC[h],
                                       reglerbandK: ReglerbandK);
            }
            return new Stundenrand(thetaLue, thetaEq, ThetaSoll[h], ThetaMax[h],
                                   PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                   heizleistungMaxW: HeizleistungMaxW,
                                   kuehlleistungMaxW: KuehlleistungMaxW,
                                   heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                   zusatzleitwertWK: sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0,
                                   uebergabe: KopplungWirksam ? Uebergabe : null,
                                   vorlaufC: KopplungWirksam ? VorlaufC[h] : double.NaN,
                                   reglerbandK: ReglerbandK,
                                   kuehlUebergabeGespiegelt: KuehlUebergabeGespiegelt,
                                   kuehlVorlaufC: KuehlVorlaufC,
                                   kuehlStrahlungsanteil: KuehlStrahlungsanteil,
                                   kuehlVorlaufGekappt: KuehlVorlaufGekappt);
        }

        /// <summary>
        /// Der Flächenschlüssel der Zone (Stufe G3, Mehrzonenkonzept 4.2): Nutzfläche der Zone /
        /// Nutzfläche des Gebäudes; 1 im Klassenweg und für eine Zone ohne eigene Nutzfläche.
        /// </summary>
        internal double Flaechenanteil { get; private set; } = 1.0;

        /// <summary>
        /// <b>Der Flächenschlüssel</b> (Stufe G3, Anwenderentscheid vom 25.09.2026 „Hochrechnen“;
        /// Mehrzonenkonzept 4.2: NULL = anteilig aus dem Gebäude). Mit einer Zone ist ihre
        /// Nutzfläche die Bezugsfläche A_f (<see cref="GebaeudeZonensatz.Bezugsflaeche"/>), und die
        /// flächenbezogenen Größen des Gebäudes folgen ihr im Verhältnis A_Zone / A_Gebäude:
        /// <list type="bullet">
        /// <item>das Luftvolumen A_f·H (H_ve und der Zusatzleitwert der Sommerlüftung, H = Raumhöhe
        /// des Gebäudes) und f_IW·A_f — beide über <see cref="Nutzflaeche_M2"/>;</item>
        /// <item>die Speichermasse der Bauweise, die im Bauteilweg eine Gruppe ohne Schichten trägt
        /// (Bauweise je m² × Zonenfläche);</item>
        /// <item>die inneren Gewinne — <c>Interne_Waermegewinne</c> führt der Katalog ABSOLUT in W,
        /// also anteilig; die Bewohner sind je m² gebildet (Nutzfläche / Fläche je Nutzer) und
        /// entstehen in der Fassade aus der Zonenfläche.</item>
        /// </list>
        /// Nicht geschlüsselt sind die Leistungsgrenzen <c>Heizleistung_Max</c> und
        /// <c>Kuehlleistung_Max</c> (Eingaben in kW, sie gelten der Zone, wie sie stehen) und die
        /// übrigen Spalten der Zone (G6). Eine Zone ohne eigene Nutzfläche rechnet bitgleich wie
        /// vorher: Der Anteil ist dann genau 1, und es wird nichts umgerechnet.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.PflichtgroesseFehlt"/>,
        /// wenn die Nutzfläche der Zone oder — für den Schlüssel — die des Gebäudes nicht größer null ist.</exception>
        private void Flaechenschluessel(ProjektGebaeudeModel g)
        {
            double aGebaeude = g.Nutzflaeche;
            double aZone = Zone.Bezugsflaeche(g);
            if (!(aZone > 0.0) || double.IsInfinity(aZone))
                Fehler(GebaeudeModellFehler.PflichtgroesseFehlt,
                       string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G3_ZONE_NUTZFLAECHE,
                                     Zone.Bezeichnung, Text(aZone)));
            if (aZone == aGebaeude) return;
            if (!(aGebaeude > 0.0) || double.IsInfinity(aGebaeude))
                Fehler(GebaeudeModellFehler.PflichtgroesseFehlt,
                       string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G3_ZONE_SCHLUESSEL,
                                     Zone.Bezeichnung, Text(aGebaeude)));

            double anteil = aZone / aGebaeude;
            Flaechenanteil = anteil;
            Nutzflaeche_M2 = aZone;
            Bauweise_WhK = g.Bauweise * anteil;
            if (!_innereGewinneAusZone) InnereGewinne_W = g.Interne_Waermegewinne * anteil;
            SommerlueftungBilden();
        }

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
        /// <summary>Zwischengröße: Temperatur an der Grundfläche [°C] (E6), nach der Randbedingung der Gebäudezeile.</summary>
        internal double[] ThetaGrund { get; private set; }
        /// <summary>Zwischengröße: die Fassadenstrahlung [W/m²] (E2).</summary>
        internal Fassadenstrahlung Strahlung { get; private set; }
        /// <summary>Fiel die Erdreichrechnung auf Ersatzwerte des Jahresgangs zurück? Im Bauteilweg nur, wenn ein Bauteil am Erdreich liegt.</summary>
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
            // Mit wirksamer Kopplung (AK1) trägt die Stunde Übergabe, Vorlauf und Reglerband.
            // Die beiden Zweige ohne Kälteseite stehen WÖRTLICH wie vor E37.
            if (!KuehlKopplungWirksam)
            {
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

            // Kälteseite (Schritt K, E37): die Kühlübergabe beim festen Kaltwasser-Vorlauf mit
            // demselben Raumregler; die Wärmeseite, wenn sie wirkt, wie im Zweig darüber.
            return new Stundenrand(ThetaOut[h], ThetaEq[h], ThetaSoll[h], ThetaMax[h],
                                   PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                   heizleistungMaxW: HeizleistungMaxW,
                                   kuehlleistungMaxW: KuehlleistungMaxW,
                                   heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                   zusatzleitwertWK: sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0,
                                   uebergabe: KopplungWirksam ? Uebergabe : null,
                                   vorlaufC: KopplungWirksam ? VorlaufC[h] : double.NaN,
                                   reglerbandK: ReglerbandK,
                                   kuehlUebergabeGespiegelt: KuehlUebergabeGespiegelt,
                                   kuehlVorlaufC: KuehlVorlaufC,
                                   kuehlStrahlungsanteil: KuehlStrahlungsanteil,
                                   kuehlVorlaufGekappt: KuehlVorlaufGekappt);
        }

        /// <summary>
        /// Ist Stunde <paramref name="h"/> (0 … 8759) Nutzungszeit dieses Gebäudes — außerhalb seiner
        /// <see cref="Nachtzeit"/> (Rechenschritte 8.2, E8, E43)? Ohne Angabe der Nachtzeit Stunde des
        /// Tages 7 … 22, 1-basiert, wie nach E8.
        /// </summary>
        internal bool Nutzungszeit(int h) => Nachtzeit.Nutzungszeit(h);

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
        /// <param name="kuehlVorlaufAnlageC">Der Kaltwasser-Vorlauf der Anlage [°C] für die Kälteseite
        /// (E37) — der kälteste wirksame <c>Kuehl_Vorlauf</c> der Wärmepumpen im Kühlbetrieb, von der
        /// Fassade gelesen; NaN = keiner (dann gilt der Auslegungsvorlauf, benannt). Gilt auch für die
        /// Nennleistung der Kälteseite <paramref name="nennleistungSkalierung"/>.</param>
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
            double nennleistungSkalierung = 1.0,
            double kuehlVorlaufAnlageC = double.NaN)
            => Bauen(gebaeude, new GebaeudeKlima(solarOrtszeit, wochenende, laengengrad, breitengrad, zeitbezug),
                     kuehlbetrieb, anlagenkopplung, vorlaufAnlageC, nennleistungSkalierung, kuehlVorlaufAnlageC);

        /// <summary>
        /// Derselbe Eingangsbauer mit dem Klima des Gebäudes (<see cref="GebaeudeKlima"/>, Stufe G6b
        /// W3): Es wird erst dort geprüft und gerechnet, wo der Eingangsbauer das Klima vorher bildete
        /// — nach den Gebäudedaten und den Ersatzparametern —, und von jeder Zone desselben Gebäudes
        /// geteilt.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Prüfung.</exception>
        internal static GebaeudeModellEingang Bauen(
            ProjektGebaeudeModel gebaeude,
            GebaeudeKlima klima,
            bool kuehlbetrieb = false,
            string anlagenkopplung = null,
            double vorlaufAnlageC = double.NaN,
            double nennleistungSkalierung = 1.0,
            double kuehlVorlaufAnlageC = double.NaN)
            => Bauen(gebaeude, klima, null, kuehlbetrieb, anlagenkopplung, vorlaufAnlageC, nennleistungSkalierung, kuehlVorlaufAnlageC);

        /// <summary>
        /// Der Eingangsbauer für die Zone <paramref name="zone"/> eines Mehrzonengebäudes (Stufe G6b,
        /// Welle W3; <see cref="ZonenEingang"/>) oder — mit <paramref name="zone"/> <c>null</c> — der Weg
        /// des Umschalters nach Datenlage (A14/E27): ohne Zone der Klassenweg, bitgleich wie vor G3; mit
        /// genau einer Zone der Bauteilweg; mehrere Zonen benannt abgelehnt.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Prüfung.</exception>
        internal static GebaeudeModellEingang Bauen(
            ProjektGebaeudeModel gebaeude,
            GebaeudeKlima klima,
            Zonenkopplung zone,
            bool kuehlbetrieb = false,
            string anlagenkopplung = null,
            double vorlaufAnlageC = double.NaN,
            double nennleistungSkalierung = 1.0,
            double kuehlVorlaufAnlageC = double.NaN)
        {
            if (klima == null) throw new ArgumentNullException(nameof(klima));
            GebaeudeModellEingang e = Daten(gebaeude);

            if (zone == null)
            {
                // Der Umschalter nach Datenlage (A14/E27): ohne Zone der Klassenweg, bitgleich wie
                // vor G3; mit genau einer Zone der Bauteilweg; mehrere Zonen rechnet die Zonenschleife
                // mit je einer Zonenkopplung (G6b) - hier sind sie ein benannter Fehler.
                e.Zone = GebaeudeZonensatz.EineZone(gebaeude.Zonen, e.Bezeichnung);
                e.Zonenzahl = e.Zone == null ? 0 : 1;
            }
            else
            {
                e.Zone = zone.Zone;
                e.Zonenzahl = zone.Zonenzahl;
            }
            if (e.Zone == null)
                e.Parameter = ErsatzparameterRC.AusKlassenweg(e);
            else
            {
                // Die Werte der Zone (G6b, A5 (a): eine Regel für jede Zahl der Zonen).
                e.Zonenwerte(gebaeude, e.Zonenzahl);
                e.Flaechenschluessel(gebaeude);
                e.Bauteile = e.BauteileMitGebaeudewerten(zone?.Bauteile ?? e.Zone.Bauteile);
                if (zone != null)
                {
                    e.Luftkopplungen = zone.Luftkopplungen;
                    double g = 0.0;
                    foreach (Luftkopplung l in zone.Luftkopplungen) g += l.Leitwert_WK;
                    e.LuftaustauschLeitwert_WK = g;
                }
                e.Parameter = ErsatzparameterRC.AusBauteilweg(e, e.Bauteile);
            }
            e.Zeitbezug = klima.Zeitbezug;

            // ---- Klimaweg (E1, E2, E6) — über das Klima des Gebäudes (A18, G6b W3) ----
            klima.Bereitstellen();
            IReadOnlyList<SolardatenModel> solarOrtszeit = klima.Zeilen;
            bool[] wochenende = klima.Wochenende;
            e.ThetaOut = klima.ThetaOut;
            e.Strahlung = klima.Strahlung;
            bool erdreichAusKlima;
            e.ThetaGrund = GebaeudeKlimaweg.Grundtemperatur(e.GrundRandbedingung, e.Kellertemperatur,
                                                             e.ThetaOut, out erdreichAusKlima);
            e.ErdreichErsatzwerte = string.Equals(e.GrundRandbedingung, DbWerte.GRUND_ERDREICH, StringComparison.Ordinal)
                                    && !erdreichAusKlima;
            e.StundenMitGegenstrahlung = klima.StundenMitGegenstrahlung;

            ErsatzparameterRC p = e.Parameter;

            // ---- Flächengewichte der Lastaufteilung (E3, E4) ----
            double aAwGes = p.A_AW_gesamt_M2;
            double aIw = p.A_IW_M2;
            double aRaum = aAwGes + aIw;
            double anteilAW = aAwGes / aRaum;
            double anteilIW = aIw / aRaum;

            double aKon = GebaeudeFestwerte.A_KON_SOLAR;
            double innenKonv = GebaeudeFestwerte.ANTEIL_INNERE_LASTEN_KONVEKTIV * e.InnereGewinne_W;
            double innenRad = e.InnereGewinne_W - innenKonv;

            var thetaEq = new double[8760];
            var phiRadAW = new double[8760];
            var phiRadIW = new double[8760];
            var phiConv = new double[8760];
            var phiSolar = new double[8760];
            Fassadenstrahlung s = e.Strahlung;

            // Die äquivalente Außentemperatur am Auslegungspunkt (AK1, 8.4) — je Weg aus
            // denselben Gewichten wie die Stundenreihe.
            Func<int, double, double> aequivalentN;

            if (e.Zone == null)
            {
                // ======== Klassenweg (G1/G2) — unverändert ========
                double solarFaktor = e.GWert * (1.0 - e.Rahmenanteil) * e.Verschattungsfaktor * GebaeudeFestwerte.F_W;

                // ---- Gewichte der äquivalenten Außentemperatur (E7) ----
                double uaLuftseitig = e.U_Aussenwand * e.A_Aussenwand_M2 + e.U_Dach * e.A_Dach_M2
                                      + e.U_Sonstige * e.A_Sonstige_M2;
                // Mit Schalter: Wand und Sonstiges senkrecht, Dach waagerecht (Klassenweg, Rechenschritte E5).
                double uaSenkrecht = e.U_Aussenwand * e.A_Aussenwand_M2 + e.U_Sonstige * e.A_Sonstige_M2;
                double uaDach = e.U_Dach * e.A_Dach_M2;
                double uaGrund = e.U_Grund * e.A_Grund_M2;
                double uaFenster = p.UA_Fenster_WK;
                double uaSumme = p.SummeUA_opak_WK + uaFenster;

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

                // 8.4: die Grundfläche am Auslegungstag, Fenster und opake Flächen ohne Strahlung.
                aequivalentN = (tag, aN) =>
                {
                    double grundN = e.Grundtemperatur(tag, aN);
                    return uaSumme > 0.0 ? (uaLuftseitig * aN + uaGrund * grundN + uaFenster * aN) / uaSumme : aN;
                };
            }
            else
            {
                // ======== Bauteilweg (G3): Außenseite je Bauteil, Lasten wie im Klassenweg ========
                aequivalentN = e.BauteilwegAussenseite(klima, erdreichAusKlima, thetaEq, phiSolar);
                for (int h = 0; h < 8760; h++)
                {
                    // E3/E4 — dieselbe Aufteilung wie im Klassenweg, über die Flächen des Records.
                    double sol = phiSolar[h];
                    double solLuft = aKon * sol;
                    double solRad = sol - solLuft;
                    phiRadAW[h] = solRad * anteilAW + innenRad * anteilAW;
                    phiRadIW[h] = solRad * anteilIW + innenRad * anteilIW;
                    phiConv[h] = solLuft + innenKonv;
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
            // Mehrzonenweg (G6b, A4 (a)): die Zonen rechnen ideal, eine wirksame Kopplung wird zur
            // idealen Last - benannt über KopplungAlsIdealeLast, nie still.
            bool kopplung = Waermeuebergabe.KopplungWirksamFuer(gebaeude, anlagenkopplung);
            e.KopplungWirksam = kopplung && !e.Mehrzonenweg;
            e.ThetaSoll = e.KopplungWirksam
                ? e.SollwertfahrplanMitProfil(gebaeude, wochenende)
                : Sollwertfahrplan(e, wochenende);

            // KU1 (Kühlkonzept 3.2, K11): Kühlsollwert und Kühlleistungsgrenze - nur mit
            // wirksamer Kühlung. Ohne sie gibt es keine obere Grenze (+∞): Das Gebäude läuft
            // frei, und die Raumluft darf über θ_max steigen (Entscheid E32).
            e.KuehlungAufloesen(gebaeude, kuehlbetrieb);
            if (!e.IstBeheizt) e.FreiSchwingend();
            e.ThetaMax = new double[8760];
            for (int h = 0; h < 8760; h++) e.ThetaMax[h] = e.KuehlSollwert;

            if (e.KopplungWirksam)
                e.KopplungAufloesen(gebaeude, aequivalentN, vorlaufAnlageC, nennleistungSkalierung);

            // Kälteseite (E37): unabhängig vom Heizkreis, nur mit wirksamer Kühlung (E32), dem
            // Schalter (A1) und einer Kühlübergabeart ungleich ideal. Ohne sie bleibt jede Zeile.
            e.KuehluebergabeAktiv = gebaeude.Kuehluebergabe_Aktiv;
            e.KuehlUebergabeArt = gebaeude.Kuehl_Uebergabe_Art;
            bool kuehlKopplung = Kuehluebergabe.KopplungWirksamFuer(gebaeude, anlagenkopplung, kuehlbetrieb);
            e.KuehlKopplungWirksam = kuehlKopplung && !e.Mehrzonenweg;
            if (e.KuehlKopplungWirksam)
                e.KuehlKopplungAufloesen(gebaeude, kuehlVorlaufAnlageC, nennleistungSkalierung);
            e.KopplungAlsIdealeLast = e.Mehrzonenweg && e.IstBeheizt && (kopplung || (kuehlKopplung && e.KuehlungWirksam));
            return e;
        }

        // =====================================================================
        //  Bauteilweg (Stufe G3): die Außenseite je Bauteil
        // =====================================================================

        /// <summary>Woran ein Außenbauteil in der äquivalenten Außentemperatur grenzt (G3).</summary>
        private enum Aussenseite
        {
            /// <summary>Opak an Außenluft: θ_out, mit Schalter + Δθ_lw + Δθ_kw (Gl. (32)).</summary>
            LuftOpak,

            /// <summary>Transparent an Außenluft: θ_out + Δθ_lw (Gl. (39)), unabhängig vom Schalter.</summary>
            LuftFenster,

            /// <summary>Erdreich: die Erdreichtemperatur nach Kusuda (E6).</summary>
            Erdreich,

            /// <summary>Unbeheizter Raum: die Kellertemperatur des Gebäudes (benannte G3-Regel).</summary>
            Unbeheizt,
        }

        /// <summary>
        /// Ein Glied der U·A-Gewichtung nach Gl. (41): die Bauteile einer Randart mit demselben
        /// Sichtfaktor und derselben Einstrahlungsreihe, ihre U·A zusammengefasst.
        /// </summary>
        private sealed class Glied
        {
            internal Aussenseite Art;
            internal double Sichtfaktor = double.NaN;
            internal double[] Einstrahlung;
            internal double UA_WK;
        }

        /// <summary>
        /// Die Bauteile der Zone mit den Fensterwerten des Gebäudes, wo ein Fenster sie offen
        /// lässt (<see cref="BauteilEingang.MitGebaeudewerten"/>) — g-Wert, Rahmenanteil und
        /// Verschattung der Gebäudezeile, die ihrerseits schon die Vorgaben tragen.
        /// </summary>
        private IReadOnlyList<BauteilEingang> BauteileMitGebaeudewerten(IReadOnlyList<BauteilEingang> bauteile)
        {
            if (bauteile == null) return Array.Empty<BauteilEingang>();
            var liste = new BauteilEingang[bauteile.Count];
            for (int i = 0; i < bauteile.Count; i++)
            {
                BauteilEingang b = bauteile[i] ?? throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Bezeichnung + ": Die Zone „" + Zone.Bezeichnung + "“ enthält einen leeren Bauteileintrag (Nr. " +
                    (i + 1).ToString(CultureInfo.InvariantCulture) + ").");
                liste[i] = b.MitGebaeudewerten(GWert, Rahmenanteil, Verschattungsfaktor);
            }
            return Array.AsReadOnly(liste);
        }

        /// <summary>
        /// <b>Die Außenseite des Bauteilwegs</b> (Stufe G3; Rechenschritte E2, E3, E5–E7): füllt
        /// <paramref name="phiSolar"/> und <paramref name="thetaEq"/> und liefert die äquivalente
        /// Außentemperatur am Auslegungspunkt für die Anlagenkopplung (8.4).
        ///
        /// <list type="bullet">
        /// <item><b>Solare Gewinne je Fensterbauteil</b> an Außenluft (E3): A · I(Neigung, Azimut) ·
        /// g · (1 − Rahmenanteil) · F_S · F_W, mit den Werten des Bauteils (NaN = Gebäudewert) und
        /// demselben Faktor F_W wie im Klassenweg. Die Einstrahlung kommt aus
        /// <see cref="GebaeudeKlimaweg.EinstrahlungBauteil"/> mit dem Azimut der Datenbank
        /// (0° = Nord → <see cref="GebaeudeKlimaweg.AzimutAusDatenbank"/>) und der Neigung des
        /// Bauteils — damit rechnen geneigte Fenster (Dachfenster); ein waagerechtes Fenster
        /// bekommt die Globalstrahlung. Ein Fenster zu einem unbeheizten Raum bekommt keine
        /// Sonne (der Weg durch den Nachbarraum ist M3/G6b).</item>
        /// <item><b>Äquivalente Außentemperatur je Bauteil</b>, mit U·A gewichtet (Gl. (41)); U ist
        /// der in Gl. (27) wirksame Wert (<see cref="ErsatzparameterRC.UWirksamJeBauteil_WM2K"/>).
        /// Opak an Außenluft: ohne Schalter θ_out, mit Schalter θ_out + Δθ_lw + Δθ_kw mit der
        /// eigenen Einstrahlung und dem Sichtfaktor der eigenen Neigung
        /// (<see cref="GebaeudeKlimaweg.SichtfaktorHimmel"/>). Fenster an Außenluft: θ_out + Δθ_lw
        /// mit dem Sichtfaktor ihrer Neigung. Erdreich: die Erdreichtemperatur (E6). Unbeheizter
        /// Raum: die Kellertemperatur des Gebäudes — <b>benannte G3-Regel</b>; die
        /// Temperaturregel unbeheizter Nachbarzonen ist M3 und kommt mit G6b.</item>
        /// <item><b>Die Lastaufteilung</b> (E3, E4) bleibt die des Klassenwegs über die Flächen
        /// des Records (A_v = 0); sie bildet der Aufrufer.</item>
        /// </list>
        /// </summary>
        private Func<int, double, double> BauteilwegAussenseite(GebaeudeKlima gebaeudeklima, bool erdreichAusKlima,
            double[] thetaEq, double[] phiSolar)
        {
            IReadOnlyList<BauteilEingang> bauteile = Bauteile;
            IReadOnlyList<double> u = Parameter.UWirksamJeBauteil_WM2K;
            IReadOnlyList<SolardatenModel> klima = gebaeudeklima.Zeilen;

            // Einstrahlung je (Neigung, Azimut) einmal gerechnet - im Klima des Gebäudes, geteilt von
            // allen Zonen (G6b W3); die vier Fassaden liegen aus E2 schon vor (GebaeudeKlimaweg.Einstrahlung
            // ist für sie bitgleich zu Fassaden).
            double[] Reihe(BauteilEingang b)
            {
                double neigung = b.NeigungWirksamGrad;
                double azimut = double.IsNaN(b.AzimutGrad) ? double.NaN : GebaeudeKlimaweg.AzimutAusDatenbank(b.AzimutGrad);
                // Waagerecht nach oben ist der Azimut gleichgültig (Globalstrahlung).
                return gebaeudeklima.Einstrahlung(neigung, azimut);
            }

            var glieder = new List<Glied>();
            Glied Finden(Aussenseite art, double sichtfaktor, double[] einstrahlung)
            {
                foreach (Glied vorhanden in glieder)
                    if (vorhanden.Art == art && vorhanden.Sichtfaktor.Equals(sichtfaktor)
                        && ReferenceEquals(vorhanden.Einstrahlung, einstrahlung))
                        return vorhanden;
                var neu = new Glied { Art = art, Sichtfaktor = sichtfaktor, Einstrahlung = einstrahlung };
                glieder.Add(neu);
                return neu;
            }

            var fensterSolar = new List<(double Flaeche_M2, double Faktor, double[] Reihe)>();
            // Die Nachbarglieder (G6b, Gl. (41)/(42)): je Nachbarzone das U·A ihrer koppelnden
            // Trennflächen; sie stehen am Ende, hinter den Außengliedern.
            var nachbarn = new List<(int Zone, double UA_WK)>();
            bool mitErdreich = false;
            for (int i = 0; i < bauteile.Count; i++)
            {
                BauteilEingang b = bauteile[i];
                if (b.Gruppe == Bauteilgruppe.Innen) continue;
                double neigung = b.NeigungWirksamGrad;
                if (b.Rand == Bauteilrand.Zone)
                {
                    // Die Trennfläche: θ_NR,eq = θ_NR,Lu der Nachbarzone (Gl. (40) ohne strahlende
                    // Quellen, Mehrzonenkonzept 2.3/2.6); keine Sonne durch die Nachbarzone.
                    int k = nachbarn.FindIndex(x => x.Zone == b.IdNachbarzone.Value);
                    if (k < 0) nachbarn.Add((b.IdNachbarzone.Value, u[i] * b.Flaeche_M2));
                    else nachbarn[k] = (nachbarn[k].Zone, nachbarn[k].UA_WK + u[i] * b.Flaeche_M2);
                    continue;
                }
                Glied g;
                switch (b.Rand)
                {
                    case Bauteilrand.Erdreich:
                        g = Finden(Aussenseite.Erdreich, double.NaN, null);
                        mitErdreich = true;
                        break;
                    case Bauteilrand.Unbeheizt:
                        g = Finden(Aussenseite.Unbeheizt, double.NaN, null);
                        break;
                    default:
                        if (b.IstTransparent)
                        {
                            g = Finden(Aussenseite.LuftFenster, GebaeudeKlimaweg.SichtfaktorHimmel(neigung), null);
                            double faktor = b.GWert * (1.0 - b.RahmenanteilWirksam) * b.VerschattungsfaktorWirksam * GebaeudeFestwerte.F_W;
                            fensterSolar.Add((b.Flaeche_M2, faktor, Reihe(b)));
                        }
                        else
                        {
                            g = AussenbauteileStrahlung
                                ? Finden(Aussenseite.LuftOpak, GebaeudeKlimaweg.SichtfaktorHimmel(neigung), Reihe(b))
                                : Finden(Aussenseite.LuftOpak, double.NaN, null);
                        }
                        break;
                }
                g.UA_WK += u[i] * b.Flaeche_M2;
            }

            // Erdreich (E6): Liegt die Grundfläche der Gebäudezeile am Erdreich, ist die Reihe
            // schon gerechnet; sonst entsteht sie hier mit denselben Festwerten.
            double[] erdreich = null;
            ErdreichErsatzwerte = false;
            if (mitErdreich)
            {
                bool ausKlima = erdreichAusKlima;
                erdreich = string.Equals(GrundRandbedingung, DbWerte.GRUND_ERDREICH, StringComparison.Ordinal)
                    ? ThetaGrund
                    : gebaeudeklima.Erdreich(out ausKlima);
                ErdreichErsatzwerte = !ausKlima;
            }

            double uaSumme = 0.0;
            foreach (Glied g in glieder) uaSumme += g.UA_WK;
            foreach ((_, double ua) in nachbarn) uaSumme += ua;

            // Σ B_v = 1 (Gl. (42), Mehrzonenkonzept 2.3): die stehende Zusicherung, über dieselben
            // Glieder wie der Nenner - Außenglieder und Nachbarglieder.
            if (uaSumme > 0.0)
            {
                double summeB = 0.0;
                foreach (Glied g in glieder) summeB += g.UA_WK / uaSumme;
                foreach ((_, double ua) in nachbarn) summeB += ua / uaSumme;
                if (!(Math.Abs(summeB - 1.0) <= GebaeudeFestwerte.GEWICHTE_SUMME_TOLERANZ))
                    throw new InvalidOperationException(Bezeichnung + ": Die Gewichte der äquivalenten Außentemperatur " +
                                                        "summieren sich zu " + Text(summeB) + " statt 1 (Gl. (42)).");
                SummeGewichte = summeB;
            }
            UaSummeGewichtung_WK = uaSumme;
            var nachbarglieder = new Nachbarglied[nachbarn.Count];
            for (int k = 0; k < nachbarn.Count; k++) nachbarglieder[k] = new Nachbarglied(nachbarn[k].Zone, nachbarn[k].UA_WK);
            Nachbarglieder = nachbarglieder;
            var zaehler = new double[8760];
            ThetaEqZaehler = zaehler;
            bool mitNachbarn = nachbarn.Count > 0;

            for (int h = 0; h < 8760; h++)
            {
                // E3 — Fenstersolareintrag je Fensterbauteil.
                double sol = 0.0;
                foreach ((double a, double faktor, double[] reihe) in fensterSolar) sol += a * reihe[h] * faktor;
                phiSolar[h] = sol;

                // E5/E7 — je Glied die äquivalente Außentemperatur, U·A-gewichtet (Gl. (41)).
                double tOut = ThetaOut[h];
                double eA = GebaeudeKlimaweg.Gegenstrahlung(klima[h]);
                double summe = 0.0;
                foreach (Glied g in glieder)
                {
                    double theta;
                    switch (g.Art)
                    {
                        case Aussenseite.Erdreich:
                            theta = erdreich[h];
                            break;
                        case Aussenseite.Unbeheizt:
                            theta = Kellertemperatur;
                            break;
                        case Aussenseite.LuftFenster:
                            theta = tOut + GebaeudeKlimaweg.DeltaThetaLangwellig(eA, tOut, g.Sichtfaktor);
                            break;
                        default:
                            theta = g.Einstrahlung == null
                                ? tOut
                                : tOut + GebaeudeKlimaweg.DeltaThetaLangwellig(eA, tOut, g.Sichtfaktor)
                                  + GebaeudeKlimaweg.DeltaThetaKurzwellig(g.Einstrahlung[h], eA, tOut);
                            break;
                    }
                    summe += g.UA_WK * theta;
                }
                zaehler[h] = summe;
                // Mit Nachbarzonen entsteht θ_eq erst in der Zonenschleife (ZonenEingang.ThetaEq):
                // der Zähler hier, die Nachbarglieder mit θ_air der Nachbarn dahinter.
                thetaEq[h] = mitNachbarn ? double.NaN : uaSumme > 0.0 ? summe / uaSumme : tOut;
            }

            // 8.4: am Auslegungspunkt Außenluft und Fenster bei θ_out,N ohne Strahlung, das
            // Erdreich mit seinem Tagesmittel am Auslegungstag, der unbeheizte Raum bei der
            // Kellertemperatur — die Gewichte dieselben wie in der Stundenreihe.
            return (tag, aN) =>
            {
                double summe = 0.0;
                foreach (Glied g in glieder)
                {
                    double theta;
                    switch (g.Art)
                    {
                        case Aussenseite.Erdreich:
                            double tagessumme = 0.0;
                            for (int st = 0; st < 24; st++) tagessumme += erdreich[tag * 24 + st];
                            theta = tagessumme / 24.0;
                            break;
                        case Aussenseite.Unbeheizt:
                            theta = Kellertemperatur;
                            break;
                        default:
                            theta = aN;
                            break;
                    }
                    summe += g.UA_WK * theta;
                }
                return uaSumme > 0.0 ? summe / uaSumme : aN;
            };
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
        /// <param name="aequivalentN">Die äquivalente Außentemperatur am Auslegungspunkt [°C] aus
        /// Auslegungstag (0 … 364) und Auslegungs-Außentemperatur — gebildet vom Weg des Gebäudes:
        /// im Klassenweg aus den U·A-Gruppen, im Bauteilweg aus den Bauteilen (G3).</param>
        private void KopplungAufloesen(ProjektGebaeudeModel g, Func<int, double, double> aequivalentN,
                                       double vorlaufAnlageC, double nennleistungSkalierung)
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

            // 8.4: die Auslegungsheizlast — die stationäre Last des Katalogbaus (im Bauteilweg:
            // der Hülle der Zone) bei aN und iN, ohne solare und innere Lasten; die Grundfläche
            // bzw. das Erdreich am Auslegungstag, die Fenster und opaken Flächen ohne Strahlung
            // (benannte Festlegung). Ein Aufruf des Lösers.
            double eqN = aequivalentN(auslegungstag, aN);
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

        // =====================================================================
        //  Kälteseite der Kopplung (E37; Anlagenkopplung 7.2, 8.1, 8.4, 10.5)
        // =====================================================================

        /// <summary>
        /// Kaltwasser-Vorlauf des Auslegungstags [°C] — kälter als jede Raumluft: Mit der
        /// unbegrenzten Kühlübergabe (Φ_N = +∞, Xp = 0) liefert die Stunde so jede verlangte Kälte
        /// in der Verteilung der Übergabe (Grenzfall B der Kälte, 10.5); gerechnet wird mit dem
        /// Wert nicht, er öffnet nur den Zweig.
        /// </summary>
        private const double AUSLEGUNGSTAG_KALTWASSER_C = -273.15;

        /// <summary>Höchstzahl der Wiederholungen des Auslegungstags bis zum periodisch eingeschwungenen Zustand (A2).</summary>
        internal const int AUSLEGUNGSTAG_WIEDERHOLUNGEN_MAX = 30;

        /// <summary>Relative Schwankung der Tagesspitze, unter der der Auslegungstag als eingeschwungen gilt (A2).</summary>
        internal const double AUSLEGUNGSTAG_ABBRUCH_RELATIV = 1e-6;

        /// <summary>
        /// Löst die Kühlübergabe eines kühlgekoppelten Gebäudes auf (E37; Anlagenkopplung 7.2, 8.1,
        /// 8.4): Vorgaben der Art bei NULL (A4), harte Prüfregeln mit benanntem Fehler, der
        /// Strahlungsanteil der Art (ohne Spalte), das gemeinsame Proportionalband, der feste
        /// Kaltwasser-Vorlauf = max(Quelle, Vorlaufgrenze) und die Nennleistung — eingetragen (auf
        /// den Katalogbau umgerechnet wie auf der Heizseite, H7) oder aus dem Auslegungstag (A2).
        /// Die Umrechnung kW → W geschieht hier, einmal.
        /// </summary>
        private void KuehlKopplungAufloesen(ProjektGebaeudeModel g, double kuehlVorlaufAnlageC, double nennleistungSkalierung)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            string art = g.Kuehl_Uebergabe_Art;
            if (!Kuehluebergabe.ArtBekannt(art))
                Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                       string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLUEBERGABEART_UNBEKANNT, art));

            double n = g.Kuehl_Uebergabe_Exponent ?? Kuehluebergabe.VorgabeExponent(art);
            Bereich(GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT, n,
                    GebaeudeFestwerte.UEBERGABE_EXPONENT_MIN, GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX);

            double vN = g.Kuehl_Auslegung_Vorlauf ?? Kuehluebergabe.VorgabeVorlaufC(art);
            double rN = g.Kuehl_Auslegung_Ruecklauf ?? Kuehluebergabe.VorgabeRuecklaufC(art);
            double iN = g.Kuehl_Auslegung_Raumtemperatur ?? KuehlSollwert;
            if (g.Kuehl_Auslegung_Vorlauf.HasValue)
                Bereich(GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_VORLAUF, vN,
                        GebaeudeFestwerte.KUEHL_VORLAUF_MIN, GebaeudeFestwerte.KUEHL_VORLAUF_MAX);
            if (g.Kuehl_Auslegung_Raumtemperatur.HasValue)
                Bereich(GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RAUMTEMPERATUR, iN,
                        GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MIN, GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MAX);
            if (!Endlich(vN) || !Endlich(rN) || !Endlich(iN) || !(vN < rN) || !(rN < iN))
                Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                       string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_AUSLEGUNG_REIHENFOLGE, Text(vN), Text(rN), Text(iN)));

            // 7.2: die Vorlaufgrenze als Vorgabe statt Taupunktrechnung; NaN = keine.
            double grenze = g.Kuehl_Vorlaufgrenze ?? Kuehluebergabe.VorgabeVorlaufgrenzeC(art);
            if (g.Kuehl_Vorlaufgrenze.HasValue)
                Bereich(GebaeudeSchema.SPALTE_KUEHL_VORLAUFGRENZE, grenze,
                        GebaeudeFestwerte.KUEHL_VORLAUF_MIN, GebaeudeFestwerte.KUEHL_VORLAUF_MAX);
            KuehlVorlaufgrenzeC = grenze;
            KuehlGrenzeUeberAuslegung = Endlich(grenze) && grenze > vN;

            // KU 3.2: der Strahlungsanteil ist eine Vorgabe der Art, ohne Spalte (7.4 Punkt 8).
            KuehlStrahlungsanteil = Kuehluebergabe.VorgabeStrahlungsanteil(art);

            // Ein Raumregler, zwei Sequenzen (7.4 Punkt 6): Ohne Heizkopplung wird das Band hier
            // aus derselben Spalte aufgelöst wie auf der Heizseite.
            if (!KopplungWirksam)
            {
                double xp = g.Regler_Proportionalband ?? GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K;
                Bereich(GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND, xp,
                        GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MIN_K, GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MAX_K);
                ReglerbandK = xp;
            }

            // 7.2: fester Vorlauf. Die Mischgruppe am Gebäude mischt das Kaltwasser der Anlage auf
            // die Grenze hoch; kälter als die Anlage wird der Vorlauf nie.
            bool anlage = Endlich(kuehlVorlaufAnlageC);
            KuehlVorlaufquelle = anlage ? Vorlaufquelle.Anlage : Vorlaufquelle.Auslegung;
            KuehlVorlaufQuelleC = anlage ? kuehlVorlaufAnlageC : vN;
            KuehlVorlaufGekappt = Endlich(grenze) && KuehlVorlaufQuelleC < grenze;
            KuehlVorlaufC = KuehlVorlaufGekappt ? grenze : KuehlVorlaufQuelleC;

            // Die Nennleistung (sensibel). Fest eingetragen gilt sie dem wirklichen Gebäude und
            // wird auf den Katalogbau umgerechnet (H7); leer kommt sie aus dem Auslegungstag (A2).
            double phiN;
            if (g.Kuehl_Uebergabe_Leistung_Nenn.HasValue && !double.IsNaN(nennleistungSkalierung))
            {
                double wertKw = g.Kuehl_Uebergabe_Leistung_Nenn.Value;
                if (!(wertKw > 0.0))
                    Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_NENNLEISTUNG_UNGUELTIG, Text(wertKw)));
                if (!(nennleistungSkalierung > 0.0) || double.IsInfinity(nennleistungSkalierung))
                    Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_SKALIERUNG_UNGUELTIG, Text(nennleistungSkalierung)));
                phiN = double.IsPositiveInfinity(wertKw) ? double.PositiveInfinity : 1000.0 * wertKw / nennleistungSkalierung;
                KuehlNennleistungHergeleitet = false;
            }
            else
            {
                AuslegungskuehllastW = Auslegungskuehllast(out int tag, out double mittel);
                AuslegungstagKuehlung = tag;
                AuslegungstagKuehlungMittelC = mittel;
                if (!(AuslegungskuehllastW > 0.0) || !Endlich(AuslegungskuehllastW))
                    Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_AK_AUSLEGUNGSKUEHLLAST_NICHT_POSITIV,
                                         TagText(tag, k), Text(AuslegungskuehllastW), Text(KuehlSollwert)));
                phiN = AuslegungskuehllastW;
                KuehlNennleistungHergeleitet = true;
            }

            KuehlUebergabe = new Uebergabekennwerte(phiN, n, vN, rN, iN);
            KuehlUebergabeGespiegelt = Kuehluebergabe.Gespiegelt(phiN, n, vN, rN, iN);
        }

        /// <summary>
        /// <b>Die Kühllast des Auslegungstags</b> [W] (A2, Anlagenkopplung 8.4) — die Spitze des
        /// periodisch eingeschwungenen Tags mit dem höchsten Tagesmittel der Außenluft: seine 24
        /// Stundenränder aus den Eingangsreihen (Außen- und Äquivalenttemperatur, solare und innere
        /// Lasten), Heizung aus, ideale Kühlung auf den Kühlsollwert in der Verteilung der
        /// Kühlübergabe, ohne Kühlleistungsgrenze und ohne Sommerlüftung. Ein eigenes Zonenmodell
        /// rechnet, der Zustand des Laufs bleibt unberührt; start im Zustand θ_kühl. Der Tag
        /// wiederholt sich, bis die Tagesspitze um weniger als 1e-6 relativ schwankt, höchstens
        /// <see cref="AUSLEGUNGSTAG_WIEDERHOLUNGEN_MAX"/>-mal. Ausdrücklich kein Normnachweis.
        /// </summary>
        private double Auslegungskuehllast(out int tag, out double tagesmittelC)
        {
            tag = WaermsterTag(ThetaOut, out tagesmittelC);
            Uebergabekennwerte unbegrenzt = Kuehluebergabe.Gespiegelt(double.PositiveInfinity, 1.0,
                                                                        AUSLEGUNGSTAG_KALTWASSER_C, AUSLEGUNGSTAG_KALTWASSER_C + 1.0,
                                                                        KuehlSollwert);
            var modell = new Zonenmodell2K(Parameter, Bezeichnung);
            modell.Zuruecksetzen(KuehlSollwert);
            double spitze = 0.0, spitzeVor = double.NaN;
            for (int w = 0; w < AUSLEGUNGSTAG_WIEDERHOLUNGEN_MAX; w++)
            {
                spitze = 0.0;
                for (int s = 0; s < 24; s++)
                {
                    int h = tag * 24 + s;
                    var r = new Stundenrand(ThetaOut[h], ThetaEq[h], double.NaN, KuehlSollwert,
                                            PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                            reglerbandK: 0.0,
                                            kuehlUebergabeGespiegelt: unbegrenzt,
                                            kuehlVorlaufC: AUSLEGUNGSTAG_KALTWASSER_C,
                                            kuehlStrahlungsanteil: KuehlStrahlungsanteil);
                    Stundenergebnis e = modell.Schritt(in r);
                    if (e.KuehlleistungW > spitze) spitze = e.KuehlleistungW;
                }
                if (w > 0 && Math.Abs(spitze - spitzeVor) <= AUSLEGUNGSTAG_ABBRUCH_RELATIV * Math.Abs(spitze)) break;
                spitzeVor = spitze;
            }
            return spitze;
        }

        /// <summary>Der Tag (0 … 364) mit dem höchsten Tagesmittel der Außenluft und dieses Mittel [°C] — der Spiegel von <see cref="KaeltesterTag"/> (A2).</summary>
        internal static int WaermsterTag(double[] thetaOut, out double tagesmittelC)
        {
            int tag = 0;
            tagesmittelC = double.NegativeInfinity;
            for (int d = 0; d < 365; d++)
            {
                double summe = 0.0;
                for (int s = 0; s < 24; s++) summe += thetaOut[d * 24 + s];
                double mittel = summe / 24.0;
                if (mittel > tagesmittelC)
                {
                    tagesmittelC = mittel;
                    tag = d;
                }
            }
            return tag;
        }

        /// <summary>Ein Tag des Jahres (0 … 364, kein Schaltjahr) als Monat und Tag in der Kultur <paramref name="k"/>.</summary>
        internal static string TagText(int tag, CultureInfo k)
            => tag < 0 ? "—" : new DateTime(2001, 1, 1).AddDays(tag).ToString("M", k);

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
            // Ab zwei Zonen die anteilige Grenze der Zone (Festlegung 5), sonst die des Gebäudes.
            double? grenzeKw = _kuehlgrenzeZoneKw ?? g.Kuehlleistung_Max;
            if (grenzeKw.HasValue)
            {
                double grenzeW = 1000.0 * grenzeKw.Value;
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
            e.SommerlueftungBilden();

            // Ost/West: die NULL-Vorgabe aus dem Bestandsfeld bildet der Vorbereitungsschritt.
            GebaeudeVorbereitung.FensterflaechenOstWest(g, out double ost, out double west);
            e.A_FensterOst_M2 = ost;
            e.A_FensterWest_M2 = west;

            double psiL = g.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand * g.Abmessung_Anschluß_Fenster_Wand
                        + g.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach * g.Abmessung_Anschluß_Wand_Dach
                        + g.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke * g.Abmessung_Anschluß_Außenwand_Kellerdecke;
            e.SummePsiL_WK = psiL;

            e.Pruefen(g);
            e.NachtzeitAufloesen(g);
            e.Ferientage = Ferienfahrplan(g, e.Bezeichnung);
            return e;
        }

        /// <summary>
        /// Der Zusatzleitwert der Sommerlüftung aus Luftwechsel, Bezugsfläche und Raumhöhe — nach
        /// <see cref="Daten"/> und, mit Zone, nach dem Flächenschlüssel gebildet.
        /// </summary>
        private void SommerlueftungBilden()
        {
            double zusatzN = GebaeudeFestwerte.SOMMERLUEFTUNG_LUFTWECHSEL - Luftwechselrate_h;
            // Mit dem Volumen der Zone (G6b, A5 (a)) n·V·c·ρ; ohne es wörtlich wie vorher.
            SommerlueftungZusatzleitwertWK = Sommerlueftung && zusatzN > 0.0 && Endlich(zusatzN)
                ? (double.IsNaN(Luftvolumen_M3)
                    ? zusatzN * Nutzflaeche_M2 * Raumhoehe_M * GebaeudeFestwerte.C_RHO_LUFT
                    : zusatzN * Luftvolumen_M3 * GebaeudeFestwerte.C_RHO_LUFT)
                : 0.0;
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
        /// Die Nachtzeit des Gebäudes (Entscheid E43) — nach DERSELBEN Regel wie der Editor
        /// (<see cref="Nachtzeit.Pruefen"/>): beide Spalten leer ist die Vorgabe 22 bis 6 Uhr; nur eine
        /// gesetzt, eine Stunde außerhalb 0 … 23 oder Beginn = Ende bricht benannt ab
        /// (<see cref="GebaeudeModellFehler.NachtzeitUngueltig"/>), ohne stillen Rückfall.
        /// </summary>
        private void NachtzeitAufloesen(ProjektGebaeudeModel g)
        {
            int? beginn = g.Nachtabsenkung_Beginn, ende = g.Nachtabsenkung_Ende;
            CultureInfo k = CultureInfo.CurrentCulture;
            switch (Nachtzeit.Pruefen(beginn, ende))
            {
                case NachtzeitBefund.NurEineGesetzt:
                    Fehler(GebaeudeModellFehler.NachtzeitUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_NACHTZEIT_NUR_EINE, Stunde(beginn), Stunde(ende),
                                         Nachtzeit.VORGABE_BEGINN, Nachtzeit.VORGABE_ENDE));
                    break;
                case NachtzeitBefund.AusserhalbDesTages:
                    Fehler(GebaeudeModellFehler.NachtzeitUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_NACHTZEIT_BEREICH, Stunde(beginn), Stunde(ende),
                                         Nachtzeit.STUNDE_MIN, Nachtzeit.STUNDE_MAX));
                    break;
                case NachtzeitBefund.BeginnGleichEnde:
                    Fehler(GebaeudeModellFehler.NachtzeitUngueltig,
                           string.Format(k, MyResource.Resource.SIMENG_NACHTZEIT_GLEICH, Stunde(beginn)));
                    break;
            }
            Nachtzeit = Nachtzeit.Aus(beginn, ende);
        }

        /// <summary>Eine Stunde der Nachtzeit für die Meldung; leer als „—".</summary>
        private static string Stunde(int? stunde)
            => stunde.HasValue ? stunde.Value.ToString(CultureInfo.InvariantCulture) : "—";

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
                else if (e.Nutzungszeit(h)) soll[h] = e.SollTag;
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
