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
        /// <b>Die Konditionierung dieses Eingangs</b> (Stufe KP1, Konzept Konditionierungsprofile 6):
        /// je Größe der Kalender, der gilt — die erste Quelle der Kette Zone → Gebäude → abgeleitet
        /// (Konzept 3.4). <c>null</c> oder <see cref="Konditionierungssatz.Wirksam"/> <c>false</c>
        /// heißt: <b>wörtlich der Bestandszweig</b>, ohne Multiplikation mit 1, ohne neues Minimum
        /// und ohne Umweg über den Kalender.
        /// </summary>
        internal Konditionierungssatz Konditionierung { get; private set; }

        /// <summary>
        /// Der Zusatzleitwert der <b>Lüftung nach Kalender</b> je Stunde [W/K]: (n(h) − n_min)·V·ρc,
        /// nie negativ (Konzept 6). Leer ohne Lüftungskalender; dann steht in
        /// <see cref="Rand"/> wie bisher allein die Sommerlüftung.
        /// </summary>
        internal double[] LueftungZusatzleitwertWK { get; private set; }

        /// <summary>
        /// <b>Der Zusatzleitwert des BEDINGTEN Anteils</b> je Stunde [W/K] (Stufe KP1b, Konzept 3.7,
        /// P9 (b)): der Überschuss der Nutzerlüftung über den Tagwert n_T in den Stunden des
        /// Nachtfensters, mal derselben Bezugsgröße wie <see cref="LueftungZusatzleitwertWK"/>. Er
        /// wirkt nur, wenn die <see cref="Sommerlueftungsregel"/> der Nachtauskühlung eingeschaltet
        /// ist.
        ///
        /// <para><c>null</c>, wenn es in KEINER Stunde einen bedingten Anteil gibt — dann wird
        /// keine Regel gebaut, nichts gezählt, und <see cref="ZusatzleitwertWK(int, bool, bool)"/>
        /// gibt wörtlich den Bestandsausdruck zurück (N1.61 Nr. 11).</para>
        /// </summary>
        internal double[] NachtauskuehlungWK { get; private set; }

        /// <summary>
        /// Die Vorgabe der Nachtauskühlung, aus der <see cref="NachtauskuehlungWK"/> entstand
        /// (Nachtfenster, n_T, ΔT); <c>null</c> ohne Lüftungskalender.
        /// </summary>
        internal Nachtauskuehlvorgabe Nachtauskuehlung { get; private set; }

        /// <summary>
        /// <b>Ein Lüftungskalender ohne Tagwert</b> (Konzept 3.7): Dann gibt es keinen bedingten
        /// Anteil — der Kalender wirkt in jeder Stunde unbedingt, und der Lauf nennt es als Hinweis.
        /// </summary>
        internal bool NachtauskuehlungOhneTagwert { get; private set; }

        /// <summary>
        /// Die Stunden ohne Heizung (Heizsollwert „aus", E53) — außerhalb der Heizperiode oder
        /// stundenweise. 0 ohne Heizkalender. Der Kanal Raumwärme ist in diesen Stunden 0, weil der
        /// Löser sie ohne Heizung rechnet (<see cref="Stundenrand.MitHeizung"/>).
        /// </summary>
        internal int StundenOhneHeizungH { get; private set; }

        /// <summary>
        /// <b>Gilt ein Heizkalender?</b> (Stufe KP1b) Dann steht in <see cref="ThetaSoll"/> seine
        /// Reihe statt des Bestandsfahrplans; die Auslegungsraumtemperatur der Übergabe und die
        /// Prüfung F21 halten sich an sie statt an <see cref="SollTag"/> (Konzept 3.6).
        /// </summary>
        internal bool HeizkalenderWirksam { get; private set; }

        /// <summary>
        /// <b>Die Auslegungsraumtemperatur der Wärmeübergabe</b> [°C] (Konzept 3.6): der höchste
        /// <b>endliche</b> Heizsollwert der Nutzungszeit, sobald ein Heizkalender gilt — sonst
        /// wörtlich <see cref="SollTag"/> wie im Bestand. Ohne eine endliche Nutzungsstunde bleibt
        /// es ebenfalls beim Bestand; das Feld <c>Auslegung_Raumtemperatur</c> schlägt beide.
        /// </summary>
        internal double AuslegungsraumtemperaturHeizC { get; private set; } = double.NaN;

        /// <summary>
        /// <b>Der höchste unbedingte Zusatzleitwert der Nutzungszeit</b> [W/K] (Konzept 3.6): Mit
        /// Lüftungskalender geht er in die Auslegungsheizlast — sie soll den höchsten Luftwechsel
        /// der Nutzungszeit tragen, nicht das Jahresminimum, mit dem R_ext gebildet ist. <b>Ohne
        /// Nachtauskühlung</b> (der bedingte Anteil steht in <see cref="NachtauskuehlungWK"/>): Eine
        /// Auslegung auf die Nachtlüftung bemäße den Kessel auf eine Sommerstunde. 0 ohne
        /// Lüftungskalender — dort rechnet die Auslegungsheizlast wörtlich wie im Bestand.
        /// </summary>
        internal double AuslegungZusatzleitwertWK { get; private set; }

        /// <summary>
        /// <b>Die Tage außerhalb der Heizperiode</b> (E53): 365 Merker aus der Saisonperiode des
        /// Heizkalenders (<see cref="Konditionierungssatz.HeizperiodeAussen"/>); <c>null</c> ohne
        /// Heizkalender oder ohne wirkende Saisonperiode.
        /// </summary>
        internal bool[] HeizperiodeAussen { get; private set; }

        /// <summary>
        /// <b>Die Nutzungsmaske aus dem Personenkalender</b> (F16, Konzept 3.4): wahr, wo die
        /// Anwesenheit über null liegt. <c>null</c> ohne Personenkalender <b>und</b> ohne eine
        /// einzige Anwesenheitsstunde — dann zählen die Kennzahlen wörtlich nach der
        /// <see cref="Nachtzeit"/> wie bisher. Sie wirkt <b>allein in den Kennzahlen</b>
        /// (<see cref="GebaeudeModellErgebnis"/>), nie im Sollwertfahrplan.
        /// </summary>
        internal bool[] Nutzungsmaske { get; private set; }

        /// <summary>
        /// <b>Ein Personenkalender ohne eine einzige Anwesenheitsstunde</b> (F16): Dann bleibt
        /// <see cref="Nutzungsmaske"/> leer, es gilt die Nachtzeit, und der Lauf sagt es — eine
        /// Maske ohne Stunde teilte die mittlere Raumtemperatur durch null.
        /// </summary>
        internal bool NutzungsmaskeLeer { get; private set; }

        /// <summary>
        /// <b>Gilt ein Kühlkalender?</b> (Stufe KP1b) Dann steht in <see cref="ThetaMax"/> seine
        /// Reihe statt der Konstante, die Schwelle der Sommerlüftung folgt ihr je Stunde
        /// (Konzept 3.6), und die konstante Kühlprüfung entfällt zugunsten der stündlichen (G6).
        /// </summary>
        internal bool KuehlkalenderWirksam { get; private set; }

        /// <summary>
        /// <b>Die Stunden mit wirksamem Kühl-Nachtwert</b> (R14): wie oft die Reihe des
        /// Kühlkalenders den Nachtwert der Bestandsspalte <c>Kuehl_Sollwert_Nacht</c> führt. 0 ohne
        /// Kühlkalender, ohne gesetzten Nachtwert und wo er dem Tagwert gleicht. Der Lauf nennt die
        /// Zahl als Hinweis — ohne Kalender wirkt die Spalte nicht, mit ihm wirkt sie erstmals.
        /// </summary>
        internal int KuehlNachtwertStundenH { get; private set; }

        /// <summary>Der wirksam gewordene Kühl-Nachtwert [°C] (R14); NaN ohne ihn.</summary>
        internal double KuehlNachtwertC { get; private set; } = double.NaN;

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
        /// <summary>
        /// Die manuelle Aufheizzeit t [h] des Gebäudes (E59, Festlegung 37; <c>Tab_Gebaeude.Aufheizzeit_Manuell_H</c>),
        /// 1 … 47; <c>null</c> = die Art des Projekts. Jede Zone des Gebäudes trägt denselben Wert (Festlegung 38,
        /// Teilkonzept 3.4: Zonen erben, ein Zonenfeld gibt es nicht). Gelesen nur von <see cref="Aufheizoptimierung"/>.
        /// </summary>
        internal int? AufheizzeitManuellH { get; private set; }
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

        /// <summary>
        /// Die Auslegungs-Außentemperatur des Gebäudes, wie eingetragen [°C] (<c>Auslegung_Aussentemperatur</c>);
        /// <c>null</c> = aus der Klimareihe hergeleitet (H10). Gelesen auch ohne Kopplung — für die Auslegungsheizlast
        /// eines ungekoppelten Gebäudes (<see cref="Auslegungslasten"/>, E97).
        /// </summary>
        internal double? AuslegungAussentemperaturFeldC { get; private set; }

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

        /// <summary>
        /// <b>Die Auslegungsraumtemperatur der Kälte</b> [°C] (Konzept 3.6): der <b>niedrigste
        /// wirksame endliche</b> Kühlsollwert, sobald ein Kühlkalender gilt — sonst wörtlich
        /// <see cref="KuehlSollwert"/> wie im Bestand. Sie gilt der Kühlübergabe
        /// (<see cref="KuehlUebergabe"/>) und dem Auslegungstag
        /// (<see cref="Auslegungskuehllast"/>); das Feld <c>Kuehl_Auslegung_Raumtemperatur</c>
        /// schlägt beide. Ohne eine endliche Stunde bleibt es beim Bestand.
        /// </summary>
        internal double AuslegungsraumtemperaturKuehlC { get; private set; } = double.NaN;

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

        /// <summary>Die Kühlleistungsgrenze der Zone [kW] aus der Kaskade, wenn sie vom Gebäude abweicht (eigener Wert der Zone, KU3-3, oder anteilig ab zwei Zonen, Festlegung 5); sonst <c>null</c>.</summary>
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
        /// vom Gebäude (Festlegung 1); die Kühlwerte der Zone löst <see cref="KuehlungAufloesen"/> aus
        /// derselben Kaskade auf (KU3-3).
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
            if (AusZone(v.KuehlleistungMaxKw) || v.KuehlleistungMaxKw.Herkunft == Vorgabeherkunft.GebaeudeAnteilig)
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
        internal Stundenrand Rand(int h, bool sommerlueftung, double thetaEq, double thetaLue,
                                  bool nachtauskuehlung = false)
            => MitFahrplan(h, RandOhneFahrplan(h, sommerlueftung, thetaEq, thetaLue, nachtauskuehlung));

        private Stundenrand RandOhneFahrplan(int h, bool sommerlueftung, double thetaEq, double thetaLue,
                                             bool nachtauskuehlung)
        {
            if (!KuehlKopplungWirksam || !Kuehlstunde(h))
            {
                if (!KopplungWirksam)
                    return new Stundenrand(thetaLue, thetaEq, ThetaSoll[h], ThetaMax[h],
                                           PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                           heizleistungMaxW: HeizleistungMaxW,
                                           kuehlleistungMaxW: KuehlleistungMaxW,
                                           heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                           zusatzleitwertWK: ZusatzleitwertWK(h, sommerlueftung, nachtauskuehlung));
                return new Stundenrand(thetaLue, thetaEq, ThetaSoll[h], ThetaMax[h],
                                       PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                       heizleistungMaxW: HeizleistungMaxW,
                                       kuehlleistungMaxW: KuehlleistungMaxW,
                                       heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                       zusatzleitwertWK: ZusatzleitwertWK(h, sommerlueftung, nachtauskuehlung),
                                       uebergabe: Uebergabe,
                                       vorlaufC: VorlaufC[h],
                                       reglerbandK: ReglerbandK);
            }
            return new Stundenrand(thetaLue, thetaEq, ThetaSoll[h], ThetaMax[h],
                                   PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                   heizleistungMaxW: HeizleistungMaxW,
                                   kuehlleistungMaxW: KuehlleistungMaxW,
                                   heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                   zusatzleitwertWK: ZusatzleitwertWK(h, sommerlueftung, nachtauskuehlung),
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
        /// <summary>
        /// Heizsollwert [°C] (E8). Mit Aufheizoptimierung trägt er nach
        /// <see cref="HeizsollwertMitRampeSetzen"/> die Rampen (Entwurf KP3, Festlegungen 1 und 2) —
        /// Übergabe, Kälte, F21 und die stündliche Kühlprüfung hat <see cref="Bauen(ProjektGebaeudeModel, GebaeudeKlima, Zonenkopplung, bool, string, double, double, double, Konditionierungssatz)"/>
        /// vorher an der Reihe ohne Rampe ausgelegt.
        /// </summary>
        internal double[] ThetaSoll { get; private set; }

        /// <summary>
        /// <b>Setzt die Heizsollwertreihe mit Rampe</b> (Entwurf KP3, Festlegungen 1, 2 und 8) — der
        /// einzige Schreibweg nach dem Bauen: <see cref="Aufheizoptimierung.Anwenden"/> ruft ihn, wenn
        /// die Planung mindestens eine Stunde angehoben hat. Ohne Schalter wird er nie gerufen
        /// (Grundsatz 3), die Reihe bleibt dann Zeichen für Zeichen die des Bauers.
        /// </summary>
        /// <exception cref="ArgumentException">wenn die Reihe nicht 8 760 Stunden führt.</exception>
        internal void HeizsollwertMitRampeSetzen(double[] reihe)
        {
            if (reihe == null || reihe.Length != 8760)
                throw new ArgumentException("Die Heizsollwertreihe mit Rampe muss 8760 Stunden führen.", nameof(reihe));
            ThetaSoll = reihe;
        }

        /// <summary>
        /// <b>Die äquivalente Außentemperatur am Bemessungspunkt</b> [°C] in der <b>Außenform</b>
        /// (Entwurf KP3, Befund B4, Festlegung 12): Außenluft und Fenster bei <paramref name="aussenC"/>
        /// ohne Strahlung, das Erdreich mit seinem Tagesmittel am Tag <paramref name="tag"/>, der
        /// unbeheizte Raum bei der Kellertemperatur — dieselben Gewichte und dieselbe Rechnung wie am
        /// Auslegungspunkt der Anlagenkopplung (8.4), nur als Mitglied statt lokal in
        /// <see cref="Bauen(ProjektGebaeudeModel, GebaeudeKlima, Zonenkopplung, bool, string, double, double, double, Konditionierungssatz)"/>.
        /// Ohne Sonne und Gewinne.
        ///
        /// <para><b>Nur ohne Nachbarglieder:</b> Im Bauteilweg mit Nachbarzonen teilte die Außenform
        /// durch die U·A-Summe samt Nachbargliedern, summierte aber nur die Außenglieder (B4) — die
        /// Nachbarn stünden bei 0 °C. Ein solcher Eingang nimmt die Nachbarform
        /// <see cref="AequivalentN(int, double, ReadOnlySpan{double})"/>; hier lehnt das Mitglied benannt ab.</para>
        /// </summary>
        /// <param name="tag">Der Tag des Erdreichs (0 … 364).</param>
        /// <param name="aussenC">Die Außenlufttemperatur des Bemessungspunkts [°C].</param>
        /// <exception cref="InvalidOperationException">bei einem Eingang mit Nachbargliedern oder ohne Bau.</exception>
        internal double AequivalentN(int tag, double aussenC)
        {
            if (_aequivalentN == null)
                throw new InvalidOperationException(Bezeichnung + ": Der Eingang ist nicht gebaut.");
            if (Nachbarglieder.Count > 0)
                throw new InvalidOperationException(Bezeichnung + ": Die äquivalente Außentemperatur mit Nachbarzonen braucht die Temperaturen der Nachbarn (Nachbarform).");
            return _aequivalentN(tag, aussenC);
        }

        /// <summary>
        /// <b>Die äquivalente Außentemperatur am Bemessungspunkt in der Nachbarform</b> [°C] (Entwurf
        /// KP3, Befund B4, Festlegungen 12–14): der Zähler der Außenglieder wie in der Außenform
        /// (Außenluft und Fenster bei <paramref name="aussenC"/> ohne Strahlung, Erdreich als
        /// Tagesmittel des Tags <paramref name="tag"/>, unbeheizter Raum bei der Kellertemperatur),
        /// dahinter Σ U·A_j·θ_j der Nachbarglieder mit den festen Lufttemperaturen
        /// <paramref name="nachbarC"/> — geteilt durch dieselbe U·A-Summe samt Nachbargliedern.
        /// Dieselbe Bildung und Reihenfolge wie <see cref="ZonenEingang.ThetaEq"/> (Gl. (41)/(42)):
        /// Liegt die Stunde ohne Strahlung und ohne Erdreich, sind beide gleich (N-AH9).
        /// Ohne Nachbarglieder ist das wörtlich die Außenform.
        /// </summary>
        /// <param name="tag">Der Tag des Erdreichs (0 … 364).</param>
        /// <param name="aussenC">Die Außenlufttemperatur des Bemessungspunkts [°C].</param>
        /// <param name="nachbarC">Je Nachbarglied (<see cref="Nachbarglieder"/>, in deren Reihenfolge) die Lufttemperatur der Nachbarzone [°C].</param>
        /// <exception cref="InvalidOperationException">ohne Bau.</exception>
        /// <exception cref="ArgumentException">wenn die Zahl der Temperaturen nicht die der Nachbarglieder ist.</exception>
        internal double AequivalentN(int tag, double aussenC, ReadOnlySpan<double> nachbarC)
        {
            if (_aequivalentN == null)
                throw new InvalidOperationException(Bezeichnung + ": Der Eingang ist nicht gebaut.");
            IReadOnlyList<Nachbarglied> glieder = Nachbarglieder;
            if (nachbarC.Length != glieder.Count)
                throw new ArgumentException(Bezeichnung + ": " + nachbarC.Length.ToString(CultureInfo.InvariantCulture) +
                                            " Nachbartemperaturen für " + glieder.Count.ToString(CultureInfo.InvariantCulture) +
                                            " Nachbarglieder.", nameof(nachbarC));
            if (glieder.Count == 0) return _aequivalentN(tag, aussenC);
            double zaehler = _aequivalentZaehlerN(tag, aussenC);
            for (int k = 0; k < glieder.Count; k++) zaehler += glieder[k].UA_WK * nachbarC[k];
            return zaehler / UaSummeGewichtung_WK;
        }

        /// <summary>Die Außenform der äquivalenten Außentemperatur am Bemessungspunkt, gebildet im Bauen (B4).</summary>
        private Func<int, double, double> _aequivalentN;

        /// <summary>Der Zähler der Außenglieder am Bemessungspunkt (Bauteilweg, B4); <c>null</c> im Klassenweg.</summary>
        private Func<int, double, double> _aequivalentZaehlerN;
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

        /// <summary>
        /// Das Feld des freiliegenden Umfangs der Bodenplatte [m] (Rechenweg RP2a): die Länge des Anschlusses
        /// Außenwand/Kellerdecke der Gebäudezeile; 0 = nicht vorhanden. Gilt nur, wenn es mindestens dem Umfang
        /// des flächengleichen Kreises entspricht (<see cref="Erdreichwiderstand.Umfang"/>).
        /// </summary>
        internal double ErdreichUmfangFeld_M { get; private set; }

        /// <summary>
        /// Der wirksame U-Wert der Bodenplatte samt Erdreich [W/(m²K)] als Vorgabe des Gebäudes
        /// (<c>Erdreich_U_Wirksam</c>, E65); NaN = keine Vorgabe, die Rechnung nach DIN EN ISO 13370.
        /// </summary>
        internal double ErdreichUVorgabe_WM2K { get; private set; } = double.NaN;

        /// <summary>
        /// Die Erdreichkennwerte nach DIN EN ISO 13370 (Rechenweg RP2a): B′, Umfang samt Herkunft, U_g, R_g;
        /// <c>null</c>, wenn kein Bauteil (Klassenweg: keine Grundfläche) am Erdreich liegt. <see cref="U_Grund"/>
        /// bleibt der eingetragene Wert; den wirksamen trägt <see cref="Erdreichkennwerte.UWirksam_WM2K"/>.
        /// </summary>
        internal Erdreichkennwerte Erdreich { get; private set; }
        /// <summary>Zahl der Stunden mit Gegenstrahlung — nur in ihnen rechnet der langwellige Term (NULL-Regel E5).</summary>
        internal int StundenMitGegenstrahlung { get; private set; }

        /// <summary>
        /// Die Randbedingung der Stunde <paramref name="h"/> für den Löser;
        /// <paramref name="sommerlueftung"/> legt den Zusatzleitwert der Sommerlüftung parallel
        /// zum Lüftungszweig (Rechenschritte 7.2 — der Zustand gilt die ganze Stunde).
        /// </summary>
        internal Stundenrand Rand(int h, bool sommerlueftung = false, bool nachtauskuehlung = false)
            => MitFahrplan(h, RandOhneFahrplan(h, sommerlueftung, nachtauskuehlung));

        // =====================================================================
        //  Anlagenkopplung, Stufe AK2 (Konzept Anlagenkopplung 4.5, 5.3, 6.2)
        // =====================================================================

        /// <summary>
        /// <b>Die Schranke der Anlagenverfügbarkeit dieser Zone</b> je Stunde (8760; AK2) — von der Fassade
        /// verteilt (Projekt → Gebäude → Zone, <see cref="Verfuegbarkeitsverteilung"/>) und auf den Maßstab
        /// dieses Eingangs umgerechnet; die Leistung in kW. <c>null</c> = keine Schranke: Der Rand bleibt
        /// Zeichen für Zeichen der Bestand. Wirkt nur mit wirksamer Kopplung (F10). Das Modul liest damit
        /// keine Anlagendaten, es bekommt die fertige Reihe (5.3).
        /// </summary>
        internal Anlagenverfuegbarkeit[] Verfuegbarkeit { get; set; }

        /// <summary>Trägt dieser Eingang eine wirksame Schranke der Verfügbarkeit?</summary>
        internal bool FahrplanWirksam => Verfuegbarkeit != null && KopplungWirksam;

        /// <summary>
        /// Trägt die Stunde <paramref name="h"/> einen Kühlsollwert (nicht „aus")? Eine Stunde ohne ihn rechnet auch mit
        /// wirksamer Kühlübergabe ohne Kälteseite — wie die Zweige ohne Kühlkopplung (Zonensperre, Entwurf AK3-K 3.1: der
        /// Heiztag setzt den Kühlsollwert „aus"). Vorher lehnte der Löser eine solche Stunde ab
        /// (<see cref="GebaeudeModellFehler.RandUngueltig"/>, „KuehlUebergabe ohne Kuehlung"); jeder gültige Lauf bleibt
        /// damit Zeichen für Zeichen, wie er war.
        /// </summary>
        private bool Kuehlstunde(int h) => !double.IsNaN(ThetaMax[h]) && !double.IsPositiveInfinity(ThetaMax[h]);

        private Stundenrand MitFahrplan(int h, Stundenrand r)
        {
            if (!FahrplanWirksam) return r;
            Anlagenverfuegbarkeit v = Verfuegbarkeit[h];
            return r.MitVerfuegbarkeit(v.LeistungKw * 1000.0, v.Grund, v.VorlaufC);
        }

        private Stundenrand RandOhneFahrplan(int h, bool sommerlueftung, bool nachtauskuehlung)
        {
            // Die Kühlleistung wirkt in KU1 rein konvektiv am Luftknoten (Kühlkonzept 3.2):
            // kein Anteil an der Innenfläche, keine eigene Übergabeart vor der Anlagenkopplung.
            // Mit wirksamer Kopplung (AK1) trägt die Stunde Übergabe, Vorlauf und Reglerband.
            // Die beiden Zweige ohne Kälteseite stehen WÖRTLICH wie vor E37.
            if (!KuehlKopplungWirksam || !Kuehlstunde(h))
            {
                if (!KopplungWirksam)
                    return new Stundenrand(ThetaOut[h], ThetaEq[h], ThetaSoll[h], ThetaMax[h],
                                           PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                           heizleistungMaxW: HeizleistungMaxW,
                                           kuehlleistungMaxW: KuehlleistungMaxW,
                                           heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                           zusatzleitwertWK: ZusatzleitwertWK(h, sommerlueftung, nachtauskuehlung));
                return new Stundenrand(ThetaOut[h], ThetaEq[h], ThetaSoll[h], ThetaMax[h],
                                       PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                       heizleistungMaxW: HeizleistungMaxW,
                                       kuehlleistungMaxW: KuehlleistungMaxW,
                                       heizungStrahlungsanteil: HeizungStrahlungsanteil,
                                       zusatzleitwertWK: ZusatzleitwertWK(h, sommerlueftung, nachtauskuehlung),
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
                                   zusatzleitwertWK: ZusatzleitwertWK(h, sommerlueftung, nachtauskuehlung),
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
            double kuehlVorlaufAnlageC = double.NaN,
            Konditionierungssatz konditionierung = null)
            => Bauen(gebaeude, new GebaeudeKlima(solarOrtszeit, wochenende, laengengrad, breitengrad, zeitbezug),
                     kuehlbetrieb, anlagenkopplung, vorlaufAnlageC, nennleistungSkalierung, kuehlVorlaufAnlageC,
                     konditionierung);

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
            double kuehlVorlaufAnlageC = double.NaN,
            Konditionierungssatz konditionierung = null)
            => Bauen(gebaeude, klima, null, kuehlbetrieb, anlagenkopplung, vorlaufAnlageC, nennleistungSkalierung,
                     kuehlVorlaufAnlageC, konditionierung);

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
            double kuehlVorlaufAnlageC = double.NaN,
            Konditionierungssatz konditionierung = null)
        {
            if (klima == null) throw new ArgumentNullException(nameof(klima));
            GebaeudeModellEingang e = Daten(gebaeude,
                                            konditionierung != null && konditionierung.Hat(Konditionierungsgroesse.Heizsoll));

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
            // Stufe KP1: die Konditionierung VOR den Ersatzparametern - der Luftwechsel geht mit
            // seinem Jahresminimum in R_ext, der Ueberschuss je Stunde als Zusatzleitwert
            // (Konzept Konditionierungsprofile 6). Ohne wirksamen Satz bleibt jede Zeile wie bisher.
            e.KonditionierungAufloesen(konditionierung);

            if (e.Zone == null)
            {
                e.Parameter = ErsatzparameterRC.AusKlassenweg(e);
                e.Erdreich = e.Parameter.Erdreich;
            }
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
                e.Erdreich = e.Parameter.Erdreich;
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
            // Stufe KP1: mit Geraete- oder Personenkalender treten die Stundenwerte an die Stelle der
            // Konstante (Konzept 6, PhiConv/PhiRad je Stunde Q_G(h) + Q_P(h)); ohne Kalender ist die
            // Reihe null, und die zwei Konstanten oben gelten woertlich wie bisher.
            double[] innereReihe = e.InnereGewinneReihe_W;

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
                // RP2a: der wirksame U-Wert der Grundfläche (mit Erdreichwiderstand) wie in Gl. (27).
                double uaGrund = (e.Erdreich?.UWirksam_WM2K ?? e.U_Grund) * e.A_Grund_M2;
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
                    double iKonv = innenKonv, iRad = innenRad;
                    if (innereReihe != null)
                    {
                        iKonv = GebaeudeFestwerte.ANTEIL_INNERE_LASTEN_KONVEKTIV * innereReihe[h];
                        iRad = innereReihe[h] - iKonv;
                    }
                    phiRadAW[h] = solRad * anteilAW + iRad * anteilAW;
                    phiRadIW[h] = solRad * anteilIW + iRad * anteilIW;
                    phiConv[h] = solLuft + iKonv;

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
                    double iKonv = innenKonv, iRad = innenRad;
                    if (innereReihe != null)
                    {
                        iKonv = GebaeudeFestwerte.ANTEIL_INNERE_LASTEN_KONVEKTIV * innereReihe[h];
                        iRad = innereReihe[h] - iKonv;
                    }
                    phiRadAW[h] = solRad * anteilAW + iRad * anteilAW;
                    phiRadIW[h] = solRad * anteilIW + iRad * anteilIW;
                    phiConv[h] = solLuft + iKonv;
                }
            }

            e.ThetaEq = thetaEq;
            // Entwurf KP3 (B4, Festlegung 12): dieselbe Außenform als Mitglied für die Aufheizbemessung.
            e._aequivalentN = aequivalentN;
            e.PhiRadAW = phiRadAW;
            e.PhiRadIW = phiRadIW;
            e.PhiConv = phiConv;
            e.PhiSolar = phiSolar;

            // Anlagenkopplung (AK1): ob sie wirkt, entscheidet sich VOR dem Sollwertfahrplan —
            // das Zeitprogramm gilt nur mit ihr (4.3, F-A17). Ohne sie bleibt jede Zeile wie bisher.
            e.AnlagenkopplungStufe = anlagenkopplung;
            e.HeizkreisAktiv = gebaeude.Heizkreis_Aktiv;
            e.UebergabeArt = gebaeude.Uebergabe_Art;
            // E97: das Feld des Auslegungspunkts auch ohne Kopplung - die Auslegungsheizlast (Auslegungslasten).
            e.AuslegungAussentemperaturFeldC = gebaeude.Auslegung_Aussentemperatur;
            bool kopplung = Waermeuebergabe.KopplungWirksamFuer(gebaeude, anlagenkopplung);
            // Mehrzonenweg (E63, AK1z): Schritt H je Zone. Die Art der Zone, sonst die des Gebäudes;
            // eine Zone mit IDEAL (oder leer), eine unbeheizte Zone und der adiabate Vorlauf der
            // 4-K-Regel rechnen ohne Übergabe. Die Auflösung der Werte geschieht erst, wenn alle Zonen
            // stehen (ZonenkopplungAufloesen) - Vorlauf, Heizkurve und Nennleistung kommen vom Gebäude.
            bool kopplungImVorlaufIdeal = false;
            if (e.Mehrzonenweg)
            {
                string artZone = e.Zone.Eingaben?.UebergabeArt ?? gebaeude.Uebergabe_Art;
                e.UebergabeArt = artZone;
                bool zoneGekoppelt = kopplung && e.IstBeheizt && !string.IsNullOrWhiteSpace(artZone)
                                     && !string.Equals(artZone, DbWerte.UEBERGABE_IDEAL, StringComparison.Ordinal);
                kopplungImVorlaufIdeal = zoneGekoppelt && zone.OhneUebergabe;
                e.KopplungWirksam = zoneGekoppelt && !zone.OhneUebergabe;
            }
            else e.KopplungWirksam = kopplung;
            e.ThetaSoll = Bestandsfahrplan(e, gebaeude, wochenende, e.KopplungWirksam || kopplungImVorlaufIdeal);
            // Stufe KP1: mit Heizkalender tritt seine Reihe an die Stelle des Bestandsfahrplans
            // (Konzept 6); "aus" ist NaN, und der Loeser rechnet die Stunde dann ohne Heizung -
            // die Heizleistung der Zone ist 0 und der Kanal Raumwaerme ebenso (E53).
            double[] heizReihe = konditionierung?.Reihe(Konditionierungsgroesse.Heizsoll);
            if (heizReihe != null)
            {
                e.ThetaSoll = heizReihe;
                e.StundenOhneHeizungH = konditionierung.StundenOhneHeizung();
                // Stufe KP1b (Konzept 3.6): Auslegung und F21 halten sich jetzt an die Reihe. Die
                // Pruefung F21 hat Daten() deshalb zurueckgestellt - sie stuende dort vor dem
                // Kalender und pruefte gegen SollTag. Ohne endliche Nutzungsstunde entfaellt sie.
                e.HeizkalenderWirksam = true;
                e.HeizperiodeAussen = konditionierung.HeizperiodeAussen();
                if (e.HeizauslegungAufloesen()) e.MaximalraumtemperaturPruefen();
            }

            // KU1 (Kühlkonzept 3.2, K11): Kühlsollwert und Kühlleistungsgrenze - nur mit
            // wirksamer Kühlung. Ohne sie gibt es keine obere Grenze (+∞): Das Gebäude läuft
            // frei, und die Raumluft darf über θ_max steigen (Entscheid E32).
            // Stufe KP1b (G6): Steht ein Kuehlkalender bereit, tritt seine stuendliche Pruefung an
            // die Stelle der konstanten (F17) - die konstante lehnte sonst einen gueltigen Kalender
            // ab. Die Reihe selbst ist rein (Konditionierungssatz.Reihe), sie darf vorgezogen
            // werden; ohne Konditionierung ist sie null und jede Zeile bleibt woertlich.
            double[] kuehlkalenderReihe = konditionierung?.Reihe(Konditionierungsgroesse.Kuehlsoll);
            e.KuehlungAufloesen(gebaeude, kuehlbetrieb, kuehlkalenderReihe != null);
            if (!e.IstBeheizt) e.FreiSchwingend();
            e.ThetaMax = new double[8760];
            for (int h = 0; h < 8760; h++) e.ThetaMax[h] = e.KuehlSollwert;
            // Stufe KP1 (P7 a, P13 a): mit Kuehlkalender tritt seine Reihe an die Stelle der
            // Konstante; "aus" ist +unendlich - die Zone schwingt dort nach oben frei (E32). Die
            // stuendliche Pruefung theta_K(h) >= theta_H(h) + 1 K tritt dann an die Stelle der
            // Pruefung gegen den hoechsten Sollwert (F17).
            double[] kuehlReihe = e.KuehlungWirksam ? kuehlkalenderReihe : null;
            if (kuehlReihe != null)
            {
                e.ThetaMax = kuehlReihe;
                e.KuehlkalenderWirksam = true;
                e.KuehlpruefungStuendlich();
                e.KuehlNachtwertAufloesen(gebaeude);
                // Stufe KP1b (Konzept 3.6): die Auslegungsraumtemperatur der Kaelte folgt der
                // Reihe - der niedrigste wirksame endliche Kuehlsollwert statt der Konstante.
                e.KuehlauslegungAufloesen();
            }

            if (e.KopplungWirksam && !e.Mehrzonenweg)
                e.KopplungAufloesen(gebaeude, aequivalentN, vorlaufAnlageC, nennleistungSkalierung);

            // Kälteseite (E37): unabhängig vom Heizkreis, nur mit wirksamer Kühlung (E32), dem
            // Schalter (A1) und einer Kühlübergabeart ungleich ideal. Ohne sie bleibt jede Zeile.
            e.KuehluebergabeAktiv = gebaeude.Kuehluebergabe_Aktiv;
            e.KuehlUebergabeArt = gebaeude.Kuehl_Uebergabe_Art;
            bool kuehlKopplung = Kuehluebergabe.KopplungWirksamFuer(gebaeude, anlagenkopplung, kuehlbetrieb);
            e.KuehlKopplungWirksam = kuehlKopplung && !e.Mehrzonenweg;
            if (e.KuehlKopplungWirksam)
                e.KuehlKopplungAufloesen(gebaeude, kuehlVorlaufAnlageC, nennleistungSkalierung);
            // Im Mehrzonenweg bleibt die Kälteseite ideal (A4 (a)); die Wärmeseite nur im adiabaten
            // Vorlauf der 4-K-Regel (E63).
            e.KopplungAlsIdealeLast = e.Mehrzonenweg && e.IstBeheizt && (kopplungImVorlaufIdeal || (kuehlKopplung && e.KuehlungWirksam));
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
            // Kellertemperatur — die Gewichte dieselben wie in der Stundenreihe. Der Zähler der
            // Außenglieder steht für sich (Entwurf KP3, B4): Die Nachbarform hängt die Nachbarglieder
            // dahinter wie ZonenEingang.ThetaEq; die Außenform teilt ihn wie bisher.
            Func<int, double, double> zaehlerN = (tag, aN) =>
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
                return summe;
            };
            _aequivalentZaehlerN = zaehlerN;
            return (tag, aN) => uaSumme > 0.0 ? zaehlerN(tag, aN) / uaSumme : aN;
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
            // 3.6: Ohne Heizkalender ist AuslegungsraumtemperaturHeizC Zeichen für Zeichen SollTag;
            // mit ihm der höchste endliche Heizsollwert der Nutzungszeit.
            double iN = g.Auslegung_Raumtemperatur ?? AuslegungsraumtemperaturHeizC;
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
            //
            // Stufe KP1b (Konzept 3.6): Mit Lüftungskalender trägt die Auslegung den höchsten
            // UNBEDINGTEN Luftwechsel der Nutzungszeit — R_ext führt nur das Jahresminimum, der
            // Rest läuft als Zusatzleitwert. OHNE Lüftungskalender steht hier wörtlich die
            // Aufrufzeile des Bestands, ohne zweites Argument (N1.61 Nr. 11).
            double eqN = aequivalentN(auslegungstag, aN);
            AuslegungsheizlastW = StationaereLastW(iN, aN, eqN);

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
        //  Wärmeübergabe je Zone im Mehrzonenweg (E63, AK1z)
        // =====================================================================

        /// <summary>Der Heizkreis des Gebäudes, an dem diese gekoppelte Zone hängt (E63); <c>null</c> außerhalb des gekoppelten Mehrzonenwegs.</summary>
        internal Gebaeudeheizkreis Gebaeudeheizkreis { get; private set; }

        /// <summary>
        /// <b>Löst die Wärmeübergabe der Zonen eines gekoppelten Mehrzonengebäudes auf</b> (E63, AK1z;
        /// Schritt H je Zone am gemeinsamen Vorlauf) — gerufen von <see cref="ZonenEingang.Bauen"/>, wenn
        /// alle Zonen stehen und bevor die Aufheizrampen gesetzt sind:
        /// <list type="number">
        /// <item><b>Gebäude, einmal:</b> Art, Exponent, Auslegungspunkt, Proportionalband, Heizkurve und
        /// Auslegungsaußentemperatur mit den Prüfregeln des Einzonenwegs (9.1, 9.5). Die
        /// Auslegungsraumtemperatur des Gebäudes ist das Feld, sonst die höchste der beheizten Zonen.</item>
        /// <item><b>Auslegungsheizlast</b> des Gebäudes: Summe der stationären Lasten der beheizten Zonen
        /// (8.4) — jede Zone an ihrer Auslegungsraumtemperatur, die Nachbarn in der Nachbarform fest:
        /// beheizte an ihrer Auslegungsraumtemperatur, unbeheizte an der Auslegungsaußentemperatur
        /// (benannte Festlegung, auf der sicheren Seite). Die Nennleistung des Gebäudes ist das Feld (kW →
        /// W; Skalierungsfaktor 1, Festlegung 11), sonst diese Summe.</item>
        /// <item><b>Vorlauf, einmal:</b> Heizkurve je Stunde am höchsten Heizsollwert der gekoppelten Zonen,
        /// sonst der feste Vorlauf (Anlage, sonst Auslegungsvorlauf des Gebäudes) — dieselbe Reihe für jede Zone.</item>
        /// <item><b>Je gekoppelte Zone</b> die Kaskade <see cref="Zonenuebergabevorgaben.Aufloesen"/>, die
        /// Bänder und die Kette Vorlauf &gt; Rücklauf &gt; Raum (Fehler mit Zonenbezeichner), der
        /// Strahlungsanteil der Art (H12, nur ohne Zonen- und Gebäudewert) und die Kennwerte.</item>
        /// </list>
        /// Eine Zone mit IDEAL rechnet den Bestandsweg; sie zählt in die Auslegungslast und in den
        /// Flächenschlüssel, trägt aber keinen Heizkreis.
        /// </summary>
        /// <returns>Der Heizkreis des Gebäudes; <c>null</c> ohne gekoppelte Zone.</returns>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.UebergabeUngueltig"/>, benannt.</exception>
        internal static Gebaeudeheizkreis ZonenkopplungAufloesen(ProjektGebaeudeModel g, IReadOnlyList<ZonenEingang> zonen,
                                                                double vorlaufAnlageC)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (zonen == null) throw new ArgumentNullException(nameof(zonen));
            GebaeudeModellEingang erste = null;
            foreach (ZonenEingang z in zonen)
                if (z.Eingang.KopplungWirksam) { erste = z.Eingang; break; }
            if (erste == null) return null;
            CultureInfo k = CultureInfo.CurrentCulture;
            int n = zonen.Count;

            // ---- 1. die Werte des Gebäudes (wie KopplungAufloesen) ----
            string art = g.Uebergabe_Art;
            if (!Waermeuebergabe.ArtBekannt(art))
                erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                             string.Format(k, MyResource.Resource.SIMENG_AK_UEBERGABEART_UNBEKANNT, art));
            double nG = g.Uebergabe_Exponent ?? Waermeuebergabe.VorgabeExponent(art);
            erste.Bereich(GebaeudeSchema.SPALTE_UEBERGABE_EXPONENT, nG,
                          GebaeudeFestwerte.UEBERGABE_EXPONENT_MIN, GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX);
            double vG = g.Auslegung_Vorlauf ?? Waermeuebergabe.VorgabeVorlaufC(art);
            double rG = g.Auslegung_Ruecklauf ?? Waermeuebergabe.VorgabeRuecklaufC(art);
            double iMax = double.NegativeInfinity;
            foreach (ZonenEingang z in zonen)
                if (z.IstBeheizt && z.Eingang.AuslegungsraumtemperaturHeizC > iMax) iMax = z.Eingang.AuslegungsraumtemperaturHeizC;
            double iG = g.Auslegung_Raumtemperatur ?? iMax;
            if (g.Auslegung_Vorlauf.HasValue)
                erste.Bereich(GebaeudeSchema.SPALTE_AUSLEGUNG_VORLAUF, vG,
                              GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MIN, GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MAX);
            if (g.Auslegung_Raumtemperatur.HasValue)
                erste.Bereich(GebaeudeSchema.SPALTE_AUSLEGUNG_RAUMTEMPERATUR, iG,
                              GebaeudeFestwerte.AUSLEGUNG_RAUM_MIN, GebaeudeFestwerte.AUSLEGUNG_RAUM_MAX);
            if (!Endlich(vG) || !Endlich(iG) || !(vG > iG))
                erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                             string.Format(k, MyResource.Resource.SIMENG_AK_VORLAUF_UNTER_RAUM, Text(vG), Text(iG)));
            if (!Endlich(rG) || !(rG > iG) || !(rG < vG))
                erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                             string.Format(k, MyResource.Resource.SIMENG_AK_RUECKLAUF_AUSSERHALB, Text(rG), Text(iG), Text(vG)));
            double xpG = g.Regler_Proportionalband ?? GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K;
            erste.Bereich(GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND, xpG,
                          GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MIN_K, GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MAX_K);
            double niveau = g.Heizkurve_Niveau ?? GebaeudeFestwerte.VORGABE_HEIZKURVE_NIVEAU_K;
            double steilheit = g.Heizkurve_Steilheit ?? GebaeudeFestwerte.VORGABE_HEIZKURVE_STEILHEIT;
            erste.Bereich(GebaeudeSchema.SPALTE_HEIZKURVE_NIVEAU, niveau,
                          GebaeudeFestwerte.HEIZKURVE_NIVEAU_MIN, GebaeudeFestwerte.HEIZKURVE_NIVEAU_MAX);
            erste.Bereich(GebaeudeSchema.SPALTE_HEIZKURVE_STEILHEIT, steilheit,
                          GebaeudeFestwerte.HEIZKURVE_STEILHEIT_MIN, GebaeudeFestwerte.HEIZKURVE_STEILHEIT_MAX);
            int auslegungstag = KaeltesterTag(erste.ThetaOut, out double kaeltestesMittel);
            double aN = g.Auslegung_Aussentemperatur ?? Math.Floor(kaeltestesMittel);
            if (g.Auslegung_Aussentemperatur.HasValue)
                erste.Bereich(GebaeudeSchema.SPALTE_AUSLEGUNG_AUSSENTEMPERATUR, aN,
                              GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MIN, GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MAX);
            if (!Endlich(aN) || !(aN < iG))
                erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                             string.Format(k, MyResource.Resource.SIMENG_AK_AUSSEN_NICHT_UNTER_RAUM, Text(aN), Text(iG)));

            // ---- 2. je Zone: Kaskade, Strahlungsanteil (H12), Bemessungstemperatur ----
            var eingaben = new List<Zoneneingaben>(n);
            foreach (ZonenEingang z in zonen) eingaben.Add(z.Eingang.Zone.EingabenOderNutzflaeche());
            Gebaeudeuebergabe gu = Gebaeudeuebergabe.Aus(g);
            var u = new Zonenuebergabe[n];
            var anteil = new double[n];
            var luftN = new double[n];
            for (int i = 0; i < n; i++)
            {
                GebaeudeModellEingang e = zonen[i].Eingang;
                if (!e.IstBeheizt)
                {
                    luftN[i] = aN;
                    continue;
                }
                anteil[i] = Zonenuebergabevorgaben.FlaechenanteilBeheizt(eingaben[i], eingaben);
                u[i] = Zonenuebergabevorgaben.Aufloesen(eingaben[i], gu, e.AuslegungsraumtemperaturHeizC, double.NaN, anteil[i]);
                if (e.KopplungWirksam && u[i].Ideal)
                    e.FehlerZone(string.Format(k, MyResource.Resource.SIMENG_AK_UEBERGABEART_UNBEKANNT, e.UebergabeArt));
                luftN[i] = e.KopplungWirksam ? u[i].AuslegungRaumtemperaturC : e.AuslegungsraumtemperaturHeizC;
                if (e.KopplungWirksam && eingaben[i].HeizungStrahlungsanteil == null && !g.Heizung_Strahlungsanteil.HasValue)
                    e.HeizungStrahlungsanteil = Waermeuebergabe.VorgabeStrahlungsanteil(u[i].Art);
            }

            // ---- 3. die Auslegungsheizlast: Summe der stationären Lasten der beheizten Zonen (8.4) ----
            var lastW = new double[n];
            double summeW = 0.0;
            for (int i = 0; i < n; i++)
            {
                if (!zonen[i].IstBeheizt) continue;
                lastW[i] = zonen[i].Eingang.StationaereAuslegungW(zonen[i], auslegungstag, aN, luftN[i], luftN);
                summeW += lastW[i];
            }
            double phiG;
            bool hergeleitet;
            if (g.Uebergabe_Leistung_Nenn.HasValue)
            {
                double wertKw = g.Uebergabe_Leistung_Nenn.Value;
                if (!(wertKw > 0.0))
                    erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                                 string.Format(k, MyResource.Resource.SIMENG_AK_NENNLEISTUNG_UNGUELTIG, Text(wertKw)));
                phiG = double.IsPositiveInfinity(wertKw) ? double.PositiveInfinity : 1000.0 * wertKw;
                hergeleitet = false;
            }
            else
            {
                if (!(summeW > 0.0) || !Endlich(summeW))
                    erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                                 string.Format(k, MyResource.Resource.SIMENG_AK_AUSLEGUNGSHEIZLAST_NICHT_POSITIV,
                                               Text(summeW), Text(aN), Text(iG)));
                phiG = summeW;
                hergeleitet = true;
            }

            // ---- 4. Heizkurve und Vorlauf, einmal (10.1) ----
            var uebergabeG = new Uebergabekennwerte(phiG, nG, vG, rG, iG);
            var kurve = new Heizkurve(uebergabeG, aN, niveau, steilheit);
            bool kurveAktiv = g.Heizkurve_Aktiv;
            Vorlaufquelle quelle;
            double fest = double.NaN;
            var vorlauf = new double[8760];
            int ohneHeizung = 0;
            bool profil = false;
            foreach (ZonenEingang z in zonen) profil |= z.Eingang.KopplungWirksam && z.Eingang.SollwertprofilWirksam;
            for (int h = 0; h < 8760; h++)
            {
                double soll = double.NegativeInfinity;
                foreach (ZonenEingang z in zonen)
                {
                    if (!z.Eingang.KopplungWirksam) continue;
                    double s = z.Eingang.ThetaSoll[h];
                    if (Endlich(s) && s > soll) soll = s;
                }
                if (double.IsNegativeInfinity(soll)) ohneHeizung++;
                vorlauf[h] = double.IsNegativeInfinity(soll) ? double.NaN : soll;
            }
            if (kurveAktiv)
            {
                quelle = Vorlaufquelle.Heizkurve;
                for (int h = 0; h < 8760; h++)
                    vorlauf[h] = double.IsNaN(vorlauf[h]) ? double.NaN : kurve.VorlaufC(vorlauf[h], erste.ThetaOut[h]);
            }
            else
            {
                bool anlage = Endlich(vorlaufAnlageC) && vorlaufAnlageC > 0.0;
                quelle = anlage ? Vorlaufquelle.Anlage : Vorlaufquelle.Auslegung;
                fest = anlage ? vorlaufAnlageC : vG;
                for (int h = 0; h < 8760; h++) vorlauf[h] = fest;
            }

            var hk = new Gebaeudeheizkreis
            {
                UebergabeArt = art,
                Uebergabe = uebergabeG,
                NennleistungHergeleitet = hergeleitet,
                AuslegungsheizlastW = summeW,
                AuslegungAussentemperaturC = aN,
                AuslegungAussentemperaturHergeleitet = !g.Auslegung_Aussentemperatur.HasValue,
                ReglerbandK = xpG,
                Heizkurve = kurve,
                HeizkurveAktiv = kurveAktiv,
                Vorlaufquelle = quelle,
                VorlaufFestC = fest,
                VorlaufC = vorlauf,
                Strahlungsanteil = g.Heizung_Strahlungsanteil ?? Waermeuebergabe.VorgabeStrahlungsanteil(art),
                StundenOhneHeizungH = ohneHeizung,
                SollwertprofilWirksam = profil,
            };

            // ---- 5. je gekoppelte Zone: Prüfung und Kennwerte ----
            for (int i = 0; i < n; i++)
            {
                GebaeudeModellEingang e = zonen[i].Eingang;
                if (!e.KopplungWirksam) continue;
                Zonenuebergabe z = u[i];
                Zoneneingaben ze = eingaben[i];
                if (z.NennleistungHerkunft == Vorgabeherkunft.GebaeudeAnteilig) z = z with { NennleistungW = phiG * anteil[i] };
                e.BereichZone(GebaeudeSchema.SPALTE_UEBERGABE_EXPONENT, z.Exponent,
                              GebaeudeFestwerte.UEBERGABE_EXPONENT_MIN, GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX);
                if (ze.AuslegungVorlaufC.HasValue || g.Auslegung_Vorlauf.HasValue)
                    e.BereichZone(GebaeudeSchema.SPALTE_AUSLEGUNG_VORLAUF, z.AuslegungVorlaufC,
                                  GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MIN, GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MAX);
                if (ze.AuslegungRaumtemperaturC.HasValue || g.Auslegung_Raumtemperatur.HasValue)
                    e.BereichZone(GebaeudeSchema.SPALTE_AUSLEGUNG_RAUMTEMPERATUR, z.AuslegungRaumtemperaturC,
                                  GebaeudeFestwerte.AUSLEGUNG_RAUM_MIN, GebaeudeFestwerte.AUSLEGUNG_RAUM_MAX);
                double vN = z.AuslegungVorlaufC, rN = z.AuslegungRuecklaufC, iN = z.AuslegungRaumtemperaturC;
                if (!Endlich(vN) || !Endlich(iN) || !(vN > iN))
                    e.FehlerZone(string.Format(k, MyResource.Resource.SIMENG_AK_VORLAUF_UNTER_RAUM, Text(vN), Text(iN)));
                if (!Endlich(rN) || !(rN > iN) || !(rN < vN))
                    e.FehlerZone(string.Format(k, MyResource.Resource.SIMENG_AK_RUECKLAUF_AUSSERHALB, Text(rN), Text(iN), Text(vN)));
                e.BereichZone(GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND, z.ReglerProportionalbandK,
                              GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MIN_K, GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MAX_K);
                if (!(z.NennleistungW > 0.0))
                    e.FehlerZone(string.Format(k, MyResource.Resource.SIMENG_AK_NENNLEISTUNG_UNGUELTIG,
                                               Text(ze.UebergabeLeistungNennKw ?? z.NennleistungW / 1000.0)));

                e.UebergabeArt = z.Art;
                e.Uebergabe = new Uebergabekennwerte(z.NennleistungW, z.Exponent, vN, rN, iN);
                e.ReglerbandK = z.ReglerProportionalbandK;
                e.UebergabeNennleistungHergeleitet = z.NennleistungHerkunft != Vorgabeherkunft.Zone && hergeleitet;
                e.AuslegungsheizlastW = lastW[i];
                e.AuslegungAussentemperaturC = aN;
                e.AuslegungAussentemperaturHergeleitet = hk.AuslegungAussentemperaturHergeleitet;
                e.Heizkurve = kurve;
                e.HeizkurveAktiv = kurveAktiv;
                e.Vorlaufquelle = quelle;
                e.VorlaufFestC = fest;
                e.VorlaufC = vorlauf;
                e.Gebaeudeheizkreis = hk;
            }
            return hk;
        }

        /// <summary>
        /// Die stationäre Auslegungslast DIESER Zone [W] (E63): an <paramref name="iN"/>, Außenluft
        /// <paramref name="aN"/> am Auslegungstag, die Nachbarn fest an <paramref name="luftN"/> (Nachbarform
        /// von θ_eq und Zuluft) — sonst dieselbe Bildung wie die Auslegungsheizlast des Einzonenwegs.
        /// </summary>
        private double StationaereAuslegungW(ZonenEingang zone, int tag, double aN, double iN, double[] luftN)
        {
            double eqN = zone.AequivalentN(tag, aN, luftN);
            double zusatz = LueftungZusatzleitwertWK == null ? 0.0 : AuslegungZusatzleitwertWK;
            double zuluft = zone.ZuluftN(aN, zusatz, luftN);
            return StationaereLastW(iN, zuluft, eqN);
        }

        /// <summary>
        /// <b>Die stationäre Last am Auslegungspunkt</b> [W] (8.4) — der eine Ausdruck der Auslegungsheizlast in
        /// jedem Weg: Raumluft <paramref name="iN"/>, Zuluft <paramref name="zuluftC"/> (Einzone: die
        /// Auslegungs-Außentemperatur), äquivalente Außentemperatur <paramref name="eqN"/>, ohne solare und
        /// innere Lasten, mit dem Strahlungsanteil der Heizung und — mit Lüftungskalender — dem höchsten
        /// unbedingten Zusatzleitwert der Nutzungszeit (KP1b, 3.6). Ohne Lüftungskalender die Aufrufzeile des
        /// Bestands ohne zweites Argument (N1.61 Nr. 11).
        /// </summary>
        private double StationaereLastW(double iN, double zuluftC, double eqN)
            => LueftungZusatzleitwertWK == null
                ? new Zonenmodell2K(Parameter, Bezeichnung).StationaereHeizlastW(iN, zuluftC, eqN, HeizungStrahlungsanteil)
                : new Zonenmodell2K(Parameter, Bezeichnung)
                    .StationaereHeizlastW(iN, zuluftC, eqN, HeizungStrahlungsanteil, AuslegungZusatzleitwertWK);

        /// <summary>
        /// <b>Die Auslegungsheizlast Φ_HL je Zone</b> [W] — die eine Quelle der Auslegungsgröße (E60, Festlegung 41;
        /// E97, Befund O2-B1), gerufen von der Aufheizplanung (<see cref="Aufheizzone.AusZonen"/>):
        /// <list type="bullet">
        /// <item><b>Mit wirksamer Anlagenkopplung</b> an einer Zone des Gebäudes die Zahl des Kopplungswegs
        /// (<see cref="KopplungAufloesen"/>, <see cref="ZonenkopplungAufloesen"/>), unverändert.</item>
        /// <item><b>Ohne Kopplung dieselbe Bildung</b> (<see cref="StationaereAuslegungW"/>, Schritt 3 von
        /// <see cref="ZonenkopplungAufloesen"/>): Auslegungstag und Auslegungs-Außentemperatur wie dort — das Feld
        /// des Gebäudes, sonst das kälteste Tagesmittel abgerundet (H10) —, jede beheizte Zone an ihrer
        /// Auslegungsraumtemperatur (höchster Heizsollwert der Nutzungszeit, 3.6), so wie der Kopplungsweg eine
        /// Zone ohne Übergabe führt; Nachbarn fest, unbeheizte an der Auslegungs-Außentemperatur (Festlegung 14 des
        /// Kopplungswegs). Im Einzonenweg ist das Zeichen für Zeichen der Ausdruck aus
        /// <see cref="KopplungAufloesen"/> (ohne Nachbarn ist die Zuluft die Außenluft).</item>
        /// </list>
        /// Ohne Kopplung lehnt die Bildung nicht ab — die Felder des Auslegungspunkts tragen den Lauf dort nicht:
        /// Ein Feld außerhalb seiner Grenzen, eine Auslegungs-Außentemperatur nicht unter der Raumtemperatur oder
        /// eine Last ≤ 0 ergibt NaN (keine Zahl, die Ergebniszeile bleibt NULL). Unbeheizte Zonen: NaN.
        /// </summary>
        internal static double[] Auslegungslasten(IReadOnlyList<ZonenEingang> zonen)
        {
            if (zonen == null) throw new ArgumentNullException(nameof(zonen));
            int n = zonen.Count;
            var lastW = new double[n];
            bool gekoppelt = false;
            for (int i = 0; i < n; i++)
            {
                lastW[i] = zonen[i].Eingang.AuslegungsheizlastW;
                gekoppelt |= zonen[i].Eingang.KopplungWirksam;
            }
            if (n == 0 || gekoppelt) return lastW;

            GebaeudeModellEingang erste = zonen[0].Eingang;
            int tag = KaeltesterTag(erste.ThetaOut, out double kaeltestesMittel);
            double? feld = erste.AuslegungAussentemperaturFeldC;
            double aN = feld ?? Math.Floor(kaeltestesMittel);
            if (!Endlich(aN)
                || (feld.HasValue && (aN < GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MIN || aN > GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MAX)))
                return lastW;

            var luftN = new double[n];
            for (int i = 0; i < n; i++)
            {
                double iN = zonen[i].Eingang.AuslegungsraumtemperaturHeizC;
                luftN[i] = zonen[i].IstBeheizt && Endlich(iN) ? iN : aN;
            }
            for (int i = 0; i < n; i++)
            {
                double iN = zonen[i].Eingang.AuslegungsraumtemperaturHeizC;
                if (!zonen[i].IstBeheizt || !Endlich(iN) || !(aN < iN)) continue;
                double w = zonen[i].Eingang.StationaereAuslegungW(zonen[i], tag, aN, iN, luftN);
                if (w > 0.0 && Endlich(w)) lastW[i] = w;
            }
            return lastW;
        }

        /// <summary>Harte Prüfregel eines Werts der Zone (E63): benannter Fehler mit Zonenbezeichner.</summary>
        private void BereichZone(string spalte, double wert, double min, double max)
        {
            if (!Endlich(wert) || wert < min || wert > max)
                FehlerZone(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_AK_WERT_AUSSERHALB,
                                         spalte, Text(wert), Text(min), Text(max)));
        }

        /// <summary>Der Fehler der Übergabe einer Zone (E63): „Gebäude, Zone …: Text".</summary>
        private void FehlerZone(string text)
            => throw new GebaeudeModellException(GebaeudeModellFehler.UebergabeUngueltig,
                                                 Bezeichnung + ", " + GebaeudeZonenabbildung.Wer(Zone.Bezeichnung) + ": " + text);

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
            // 3.6: Ohne Kühlkalender ist AuslegungsraumtemperaturKuehlC Zeichen für Zeichen
            // KuehlSollwert; mit ihm der niedrigste wirksame endliche Kühlsollwert der Reihe.
            double iN = g.Kuehl_Auslegung_Raumtemperatur ?? AuslegungsraumtemperaturKuehlC;
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
                                         TagText(tag, k), Text(AuslegungskuehllastW),
                                         Text(AuslegungsraumtemperaturKuehlC)));
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
            // 3.6: Ohne Kühlkalender ist AuslegungsraumtemperaturKuehlC Zeichen für Zeichen
            // KuehlSollwert; mit ihm der niedrigste wirksame endliche Kühlsollwert der Reihe.
            double iN = AuslegungsraumtemperaturKuehlC;
            Uebergabekennwerte unbegrenzt = Kuehluebergabe.Gespiegelt(double.PositiveInfinity, 1.0,
                                                                        AUSLEGUNGSTAG_KALTWASSER_C, AUSLEGUNGSTAG_KALTWASSER_C + 1.0,
                                                                        iN);
            var modell = new Zonenmodell2K(Parameter, Bezeichnung);
            modell.Zuruecksetzen(iN);
            double spitze = 0.0, spitzeVor = double.NaN;
            for (int w = 0; w < AUSLEGUNGSTAG_WIEDERHOLUNGEN_MAX; w++)
            {
                spitze = 0.0;
                for (int s = 0; s < 24; s++)
                {
                    int h = tag * 24 + s;
                    var r = new Stundenrand(ThetaOut[h], ThetaEq[h], double.NaN, iN,
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
        /// <b>Die stündliche Kühlprüfung</b> (F17, Konzept Konditionierungsprofile 3.6):
        /// θ_K(h) ≥ θ_H(h) + <see cref="GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K"/>, <b>wo beide
        /// wirken</b> — eine Stunde ohne Heizung (NaN) oder ohne Kühlung (+∞) ist keine Prüfstelle.
        /// Sie tritt an die Stelle der Prüfung gegen den höchsten Sollwert des ganzen Fahrplans, die
        /// bei einem konstanten Kühlsollwert dieselbe Aussage macht (<see cref="KuehlungAufloesen"/>).
        /// </summary>
        private void KuehlpruefungStuendlich()
        {
            for (int h = 0; h < ThetaMax.Length; h++)
            {
                double heiz = ThetaSoll[h], kuehl = ThetaMax[h];
                if (!Endlich(heiz) || !Endlich(kuehl)) continue;
                if (kuehl >= heiz + GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K) continue;
                Fehler(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.SIMENG_KOND_KUEHL_UNTER_HEIZ,
                                  h.ToString(CultureInfo.InvariantCulture), Text(kuehl), Text(heiz),
                                  Text(GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K)));
            }
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
        ///
        /// <para><b>Mit Kühlkalender entfällt die konstante Prüfung</b> (Stufe KP1b, G6): Dann tritt
        /// <see cref="KuehlpruefungStuendlich"/> an ihre Stelle (F17, Konzept 3.6) — die konstante
        /// Prüfung gegen den höchsten Heizsollwert des ganzen Fahrplans lehnte sonst einen gültigen
        /// Kalender ab, dessen Kühlsollwert nur in anderen Stunden tiefer liegt als jener Höchstwert.
        /// Die Leistungsgrenze bleibt in jedem Fall geprüft.</para>
        /// </summary>
        /// <param name="kuehlkalender">Gilt ein Kühlkalender? Dann prüft nur die stündliche Regel.</param>
        private void KuehlungAufloesen(ProjektGebaeudeModel g, bool kuehlbetrieb, bool kuehlkalender)
        {
            // DIE VERERBUNG DER KÜHLWERTE EINER ZONE (KU3-3, E67/E68; Kühlkonzept 3.5, 7.1; Mehrzonenkonzept 4.2),
            // in dieser Reihenfolge:
            //  1. Der Projektschalter Tab_Einstellungen.Kuehlbetrieb steht über allem: ohne ihn kühlt keine Zone.
            //  2. Der Schalter: Tab_Zone.Kuehlung_Aktiv, NULL = Tab_Gebaeude.Kuehlung_Aktiv. Eine 0 an der Zone
            //     schaltet sie aus, auch wenn das Gebäude kühlt; eine 1 schaltet sie ein, auch wenn es nicht kühlt.
            //  3. Der Sollwert: Tab_Zone.Kuehl_Sollwert, NULL = der des Gebäudes; ohne beide ist die Kühlung aus
            //     (F-K1). Der Nachtwert ebenso (Tab_Zone.Kuehl_Sollwert_Nacht, NULL = der des Gebäudes) — wirksam
            //     wird er nur über den Kühlkalender (R14).
            //  4. Die Grenze: Tab_Zone.Kuehlleistung_Max, sonst ab zwei Zonen der Flächenanteil der Gebäudegrenze
            //     (Festlegung 5), sonst die Gebäudegrenze, sonst unbegrenzt.
            //  5. Der Kühlkalender: Führt die Zone eine eigene Kühlzeile (Matrix oder Kalender), gilt ihre Reihe
            //     (Konditionierungdatenweg mit idZone), sonst die des Gebäudes, sonst die Konstante aus 3.
            //  Eine unbeheizte Zone schwingt frei (FreiSchwingend) — sie kühlt nie.
            // Ohne Zone (Klassenweg) gelten die Spalten des Gebäudes; ohne eigene Werte der Zone ist jeder
            // Wert der des Gebäudes, also bitgleich wie vorher.
            bool aktiv = Vorgaben?.KuehlungAktiv ?? g.Kuehlung_Aktiv;
            double? sollwert = Vorgaben != null ? Vorgaben.KuehlSollwert.Wert : g.Kuehl_Sollwert;
            KuehlungWirksam = kuehlbetrieb && aktiv && sollwert.HasValue;
            if (!KuehlungWirksam)
            {
                KuehlSollwert = double.PositiveInfinity;
                AuslegungsraumtemperaturKuehlC = KuehlSollwert;
                KuehlleistungMaxW = double.NaN;
                return;
            }

            double soll = sollwert.Value;
            double heizMax = double.NegativeInfinity;
            for (int h = 0; h < ThetaSoll.Length; h++)
                if (Endlich(ThetaSoll[h]) && ThetaSoll[h] > heizMax) heizMax = ThetaSoll[h];

            if (!kuehlkalender
                && (!Endlich(soll) || (Endlich(heizMax) && !(soll >= heizMax + GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K))))
                Fehler(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KUEHLSOLLWERT_UNTER_HEIZSOLLWERT,
                                  Text(soll), Text(heizMax), Text(GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K)));
            KuehlSollwert = soll;
            // Stufe KP1b (Konzept 3.6): der Bestandswert der Kaelteauslegung; mit Kuehlkalender
            // tritt der niedrigste wirksame endliche Sollwert der Reihe an seine Stelle.
            AuslegungsraumtemperaturKuehlC = soll;

            KuehlleistungMaxW = double.NaN;
            // Die Grenze der Zone (eigener Wert, KU3-3, oder ab zwei Zonen anteilig, Festlegung 5), sonst die des Gebäudes.
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
        /// <param name="heizkalender">
        /// Gilt ein Heizkalender (Stufe KP1b)? Dann stellt <see cref="Daten"/> die Prüfung F21
        /// zurück: Sie liefe hier <b>vor</b> dem Kalender und hielte die Maximalraumtemperatur gegen
        /// <see cref="SollTag"/>; <see cref="Bauen"/> prüft sie nach dem Einsetzen der Reihe gegen
        /// deren höchsten Heizsollwert der Nutzungszeit (Konzept 3.6). <b>Ohne Heizkalender bleibt
        /// jede Zeile dieser Methode wörtlich, wie sie war</b> — für jeden anderen Aufrufer ändert
        /// sich nichts.
        /// </param>
        /// <exception cref="GebaeudeModellException">bei einer verletzten Prüfung des Fahrplans,
        /// der Fensterflächen oder eines Modellparameters.</exception>
        internal static GebaeudeModellEingang Daten(ProjektGebaeudeModel g, bool heizkalender = false)
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
                ErdreichUmfangFeld_M = g.Abmessung_Anschluß_Außenwand_Kellerdecke,
                ErdreichUVorgabe_WM2K = g.Erdreich_U_Wirksam ?? double.NaN,
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
                AufheizzeitManuellH = g.Aufheizzeit_Manuell_H,
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
            // E59: die manuelle Aufheizzeit nur im Bereich der Pruefklausel (1 bis 47 h).
            if (g.Aufheizzeit_Manuell_H is int tManuell &&
                (tManuell < AufheizManuellSchema.MANUELL_MIN_H || tManuell > AufheizManuellSchema.MANUELL_MAX_H))
                e.Fehler(GebaeudeModellFehler.ParameterUngueltig, "Die manuelle Aufheizzeit " +
                         tManuell.ToString(CultureInfo.InvariantCulture) + " h liegt nicht zwischen 1 und 47 h.");
            e.SommerlueftungBilden();

            // Ost/West: die NULL-Vorgabe aus dem Bestandsfeld bildet der Vorbereitungsschritt.
            GebaeudeVorbereitung.FensterflaechenOstWest(g, out double ost, out double west);
            e.A_FensterOst_M2 = ost;
            e.A_FensterWest_M2 = west;

            double psiL = g.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand * g.Abmessung_Anschluß_Fenster_Wand
                        + g.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach * g.Abmessung_Anschluß_Wand_Dach
                        + g.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke * g.Abmessung_Anschluß_Außenwand_Kellerdecke;
            e.SummePsiL_WK = psiL;

            // Stufe KP1b (Konzept 3.6): der Bestandswert der Auslegungsraumtemperatur; mit
            // Heizkalender tritt der hoechste endliche Heizsollwert der Nutzungszeit an seine Stelle.
            e.AuslegungsraumtemperaturHeizC = e.SollTag;

            e.Pruefen(g, !heizkalender);
            e.NachtzeitAufloesen(g);
            e.Ferientage = Ferienfahrplan(g, e.Bezeichnung);
            return e;
        }

        /// <summary>
        /// <b>Der Zusatzleitwert einer Stunde</b> [W/K] — der größere aus Sommerlüftung und Lüftung
        /// nach Kalender (Konzept Konditionierungsprofile 6: „der größere gewinnt"). <b>Ohne
        /// Lüftungskalender steht hier wörtlich der Bestandsausdruck</b>: Der Zweig
        /// <c>LueftungZusatzleitwertWK == null</c> gibt genau
        /// <c>sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0</c> zurück, ohne Maximum und ohne
        /// Vergleich — der Referenzlauf bleibt byte-gleich.
        ///
        /// <para>Auch die Zulufttemperatur gekoppelter Zonen nimmt diesen Wert
        /// (<see cref="ZonenEingang.ThetaLue"/>, Stufe KP1b, G3): Der Löser rechnet
        /// (g_ext + Z)·θ_Lue, Zähler und Nenner müssen also denselben Zusatzleitwert tragen, sonst
        /// käme der Kalenderüberschuss mit Mischluft statt mit Außenluft herein.</para>
        /// </summary>
        internal double ZusatzleitwertWK(int h, bool sommerlueftung)
        {
            double sommer = sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0;
            if (LueftungZusatzleitwertWK == null) return sommer;
            double kalender = LueftungZusatzleitwertWK[h];
            return kalender > sommer ? kalender : sommer;
        }

        /// <summary>
        /// <b>Der Zusatzleitwert einer Stunde mit Nachtauskühlung</b> [W/K] (Stufe KP1b, Konzept
        /// 3.7): max(Sommerlüftung, L_u(h) + L_b(h)) — der unbedingte Anteil des Lüftungskalenders
        /// gilt immer, der bedingte nur, wenn die Regel der Nachtauskühlung eingeschaltet ist; von
        /// Sommerlüftung und Lüftung gewinnt der größere Luftwechsel.
        ///
        /// <para><b>Ohne Nachtauskühlung steht hier wörtlich der Bestandsausdruck:</b> Der Zweig
        /// <c>NachtauskuehlungWK == null</c> ruft genau
        /// <see cref="ZusatzleitwertWK(int, bool)"/> — dieselbe Zahl wie in KP1a, ohne zweite
        /// Addition und ohne zweiten Vergleich (N1.61 Nr. 11).</para>
        /// </summary>
        internal double ZusatzleitwertWK(int h, bool sommerlueftung, bool nachtauskuehlung)
        {
            if (NachtauskuehlungWK == null) return ZusatzleitwertWK(h, sommerlueftung);
            double sommer = sommerlueftung ? SommerlueftungZusatzleitwertWK : 0.0;
            double kalender = (LueftungZusatzleitwertWK == null ? 0.0 : LueftungZusatzleitwertWK[h])
                              + (nachtauskuehlung ? NachtauskuehlungWK[h] : 0.0);
            return kalender > sommer ? kalender : sommer;
        }

        /// <summary>
        /// <b>Zählt die Stunde <paramref name="h"/> als Nachtauskühlstunde?</b> (Konzept 3.7): Die
        /// Regel ist an <em>und</em> es gibt in dieser Stunde einen bedingten Anteil — gleich,
        /// welcher Luftwechsel am Ende gewinnt. Ohne Nachtauskühlung immer <c>false</c>.
        /// </summary>
        internal bool Nachtauskuehlstunde(int h, bool nachtauskuehlung)
            => nachtauskuehlung && NachtauskuehlungWK != null && NachtauskuehlungWK[h] > 0.0;

        /// <summary>
        /// <b>Der Startwert einer unbeheizten Zone</b> [°C] (N1.56 Festlegung 7, Konzept 3.6): das
        /// Mittel der äquivalenten Außentemperatur θ_eq über die Vorlaufstunden ab
        /// <paramref name="start"/>. Er gilt auch einer beheizten Zone, deren Heizsollwert in der
        /// ersten Vorlaufstunde „aus" (NaN) ist — ein NaN als Startzustand wäre ein Abbruch.
        /// </summary>
        internal double StartwertUnbeheiztC(int start)
        {
            double summe = 0.0;
            for (int h = start; h < 8760; h++) summe += ThetaEq[h];
            return summe / (8760 - start);
        }

        /// <summary>
        /// <b>Der Kühl-Nachtwert, den erst der Kalender wirksam macht</b> (R14): Ohne
        /// Konditionierungszeile rechnet der Bestandszweig mit der Konstante
        /// <c>Kuehl_Sollwert</c>, und <c>Kuehl_Sollwert_Nacht</c> bleibt wirkungslos; mit
        /// abgeleitetem Kühlkalender trägt die Reihe den Nachtwert. Gezählt werden die Stunden, in
        /// denen die Reihe genau ihn führt — nur wenn er gesetzt ist und vom Tagwert abweicht.
        /// </summary>
        private void KuehlNachtwertAufloesen(ProjektGebaeudeModel g)
        {
            // KU3-3: an einer Zone die Werte der Kaskade (Zone, sonst Gebäude).
            double? nacht = Vorgaben != null ? Vorgaben.KuehlSollwertNacht.Wert : g.Kuehl_Sollwert_Nacht;
            double? tag = Vorgaben != null ? Vorgaben.KuehlSollwert.Wert : g.Kuehl_Sollwert;
            if (!nacht.HasValue || !Endlich(nacht.Value)) return;
            double wert = nacht.Value;
            if (!tag.HasValue || wert == tag.Value) return;
            int n = 0;
            for (int h = 0; h < ThetaMax.Length; h++)
                if (ThetaMax[h] == wert) n++;
            if (n == 0) return;
            KuehlNachtwertStundenH = n;
            KuehlNachtwertC = wert;
        }

        /// <summary>
        /// <b>Die Konditionierung auflösen</b> (Stufe KP1, Konzept Konditionierungsprofile 6). Sie
        /// läuft <b>vor</b> den Ersatzparametern, weil der Luftwechsel mit seinem
        /// <b>Jahresminimum</b> in R_ext eingeht und nur der Überschuss je Stunde als Zusatzleitwert
        /// läuft.
        ///
        /// <para><b>Die Bauvorschrift der Byte-Gleichheit:</b> Ohne wirksamen Satz kehrt die Methode
        /// sofort zurück — keine Zeile des Bestandswegs wird berührt. Mit Satz gilt je Größe: Trägt
        /// sie einen Kalender, ersetzt seine Reihe den Bestandswert; trägt sie keinen, bleibt der
        /// Bestandswert unangetastet.</para>
        /// </summary>
        private void KonditionierungAufloesen(Konditionierungssatz satz)
        {
            Konditionierung = satz;
            if (satz == null || !satz.Wirksam) return;

            // ---- Lüftung: das Jahresminimum in die Ersatzparameter, der Überschuss je Stunde ----
            double[] nutzer = satz.Reihe(Konditionierungsgroesse.Lueftung);
            if (nutzer != null)
            {
                // Die Infiltration bleibt konstant darunter (F15); ohne Angabe ist sie 0. Sie kommt
                // aus der Matrixzelle Lueftung/NENNWERT (Konzept 3.1, 3.3, N1.61 Nr. 14), NICHT aus
                // dem Kalender: Ein Lueftungskalender fuehrt keinen Nennwert, sein Konstruktor lehnt
                // ihn ab - der frueh gelesene Kalenderwert war deshalb immer leer und die
                // Infiltration fiel im Kalenderweg auf 0 (Befund R2).
                double infiltration = Konditionierung.InfiltrationH ?? 0.0;

                // Stufe KP1b (Konzept 3.7, P9 b): Geteilt wird die NUTZERREIHE, bevor die
                // Infiltration dazukommt - die Infiltration ist nie bedingt (F15). In den Stunden
                // des Nachtfensters ist max(0, n(h) - n_T) bedingt, der Rest unbedingt; ohne
                // Nachtauskuehlvorgabe und ohne Tagwert gibt es keinen bedingten Anteil, und die
                // Schleife rechnet Zeichen fuer Zeichen den Bestandsausdruck.
                Nachtauskuehlung = satz.Nachtauskuehlung;
                bool bedingtMoeglich = Nachtauskuehlung != null && Nachtauskuehlung.TraegtBedingtes;
                NachtauskuehlungOhneTagwert = Nachtauskuehlung != null && !Nachtauskuehlung.TraegtBedingtes;
                double tagwert = bedingtMoeglich ? Nachtauskuehlung.TagwertH.Value : 0.0;

                var gesamt = new double[8760];
                var bedingt = bedingtMoeglich ? new double[8760] : null;
                bool bedingtWirksam = false;
                double min = double.PositiveInfinity;
                for (int h = 0; h < 8760; h++)
                {
                    double unbedingt = nutzer[h];
                    if (bedingtMoeglich && Nachtauskuehlung.Fenster.IstNacht(h))
                    {
                        double ueber = unbedingt - tagwert;
                        if (ueber > 0.0)
                        {
                            bedingt[h] = ueber;
                            unbedingt = tagwert;
                            bedingtWirksam = true;
                        }
                    }
                    double n = infiltration + unbedingt;
                    if (!Endlich(n) || n < 0.0)
                        Fehler(GebaeudeModellFehler.KalenderUngueltig,
                               "Der Luftwechsel " + Text(n) + " 1/h in Stunde " +
                               h.ToString(CultureInfo.InvariantCulture) + " ist negativ oder nicht endlich.");
                    gesamt[h] = n;
                    if (n < min) min = n;
                }
                Luftwechselrate_h = min;
                SommerlueftungBilden();          // sie hängt am Luftwechsel, also neu bilden

                // Der Überschuss als masseloser Leitwert Außenluft <-> Raumluft, NIE negativ
                // (Konzept 6); dieselbe Bezugsgröße wie die Sommerlüftung.
                LueftungZusatzleitwertWK = new double[8760];
                for (int h = 0; h < 8760; h++)
                {
                    double zusatzN = gesamt[h] - min;
                    LueftungZusatzleitwertWK[h] = zusatzN > 0.0 ? zusatzN * LuftwechselBezug() : 0.0;
                }
                // Stufe KP1b (Konzept 3.6): die Auslegungsheizlast traegt den hoechsten UNBEDINGTEN
                // Luftwechsel der Nutzungszeit, nicht das Jahresminimum von R_ext.
                AuslegungslueftungAufloesen();

                // Der bedingte Anteil als zweiter Leitwert - nur, wenn es ihn in wenigstens einer
                // Stunde gibt (sonst keine Regel und keine Zaehlung, Konzept 3.7).
                if (bedingtWirksam)
                {
                    NachtauskuehlungWK = new double[8760];
                    for (int h = 0; h < 8760; h++)
                        NachtauskuehlungWK[h] = bedingt[h] > 0.0 ? bedingt[h] * LuftwechselBezug() : 0.0;
                }
            }

            // ---- Geräte und Personen: die Lasten je Stunde statt der Konstante ----
            double[] geraete = satz.Lastreihe(Konditionierungsgroesse.Geraete, InnereGewinne_W);
            double[] personen = satz.Lastreihe(Konditionierungsgroesse.Personen, 0.0);
            if (geraete != null || personen != null)
            {
                InnereGewinneReihe_W = new double[8760];
                for (int h = 0; h < 8760; h++)
                    InnereGewinneReihe_W[h] = (geraete != null ? geraete[h] : InnereGewinne_W)
                                              + (personen != null ? personen[h] : 0.0);
            }

            NutzungsmaskeBilden(satz);
        }

        /// <summary>
        /// <b>Die Auslegungswerte des Heizkalenders</b> (Konzept 3.6, F21): der höchste
        /// <b>endliche</b> Heizsollwert der Nutzungszeit. Er tritt an die Stelle von
        /// <see cref="SollTag"/> — als Auslegungsraumtemperatur der Übergabe und als Grenze, über
        /// der die Maximalraumtemperatur liegen muss.
        ///
        /// <para>Die Nutzungszeit ist hier die <b>Nachtzeit</b> des Gebäudes
        /// (<see cref="Nutzungszeit(int)"/>), nicht die Maske des Personenkalenders: Die Auslegung
        /// folgt dem Fahrplan, nicht der Anwesenheit — F16 wirkt allein in den Kennzahlen.</para>
        ///
        /// <para><b>Ohne eine endliche Nutzungsstunde</b> (jede Nutzungsstunde „aus") bleibt der
        /// Bestandswert stehen und die Prüfung F21 entfällt: Es gibt keinen Heizsollwert, gegen den
        /// sie hielte.</para>
        /// </summary>
        /// <returns><c>true</c>, wenn es eine endliche Nutzungsstunde gab und der Wert ersetzt wurde.</returns>
        private bool HeizauslegungAufloesen()
        {
            double hoechster = double.NegativeInfinity;
            for (int h = 0; h < ThetaSoll.Length; h++)
                if (Nutzungszeit(h) && Endlich(ThetaSoll[h]) && ThetaSoll[h] > hoechster) hoechster = ThetaSoll[h];
            if (!Endlich(hoechster)) return false;
            AuslegungsraumtemperaturHeizC = hoechster;
            return true;
        }

        /// <summary>
        /// <b>Die Auslegungsraumtemperatur der Kälte aus dem Kühlkalender</b> (Konzept 3.6): der
        /// niedrigste <b>wirksame endliche</b> Kühlsollwert der Reihe — „aus" (+∞) ist keine
        /// wirksame Kühlung und kommt nicht in Betracht. Ohne eine endliche Stunde bleibt der
        /// Bestandswert stehen.
        /// </summary>
        private void KuehlauslegungAufloesen()
        {
            double niedrigster = double.PositiveInfinity;
            for (int h = 0; h < ThetaMax.Length; h++)
                if (Endlich(ThetaMax[h]) && ThetaMax[h] < niedrigster) niedrigster = ThetaMax[h];
            if (!Endlich(niedrigster)) return;
            AuslegungsraumtemperaturKuehlC = niedrigster;
        }

        /// <summary>
        /// <b>Der höchste unbedingte Zusatzleitwert der Nutzungszeit</b> [W/K] (Konzept 3.6): das
        /// Maximum von <see cref="LueftungZusatzleitwertWK"/> über die Nutzungsstunden. Der bedingte
        /// Anteil der Nachtauskühlung bleibt außen vor — er steht in
        /// <see cref="NachtauskuehlungWK"/> und wird hier nicht addiert.
        /// </summary>
        private void AuslegungslueftungAufloesen()
        {
            double hoechster = 0.0;
            for (int h = 0; h < LueftungZusatzleitwertWK.Length; h++)
                if (Nutzungszeit(h) && LueftungZusatzleitwertWK[h] > hoechster) hoechster = LueftungZusatzleitwertWK[h];
            AuslegungZusatzleitwertWK = hoechster;
        }

        /// <summary>
        /// <b>Die Prüfung F21 gegen den Heizkalender</b> (Konzept 3.6, 9.1 F21): Die
        /// Maximalraumtemperatur muss über dem höchsten Heizsollwert der Nutzungszeit liegen — mit
        /// Heizkalender ist das <see cref="AuslegungsraumtemperaturHeizC"/> statt
        /// <see cref="SollTag"/>. Ohne endliche Nutzungsstunde entfällt sie; ohne Heizkalender
        /// prüft <see cref="Pruefen"/> wörtlich wie im Bestand.
        /// </summary>
        private void MaximalraumtemperaturPruefen()
        {
            if (ThetaMaxWert > AuslegungsraumtemperaturHeizC) return;
            Fehler(GebaeudeModellFehler.SollwertfahrplanUngueltig,
                "Die obere Raumtemperatur " + Text(ThetaMaxWert) + " °C liegt nicht über dem höchsten Heizsollwert " +
                Text(AuslegungsraumtemperaturHeizC) + " °C der Nutzungszeit.");
        }

        /// <summary>
        /// <b>Die Nutzungsmaske aus dem Personenkalender</b> (F16, Konzept 3.4): eine Stunde ist
        /// Nutzungszeit, wenn die <b>Anwesenheit</b> über null liegt. Ohne Personenkalender bleibt
        /// <see cref="Nutzungsmaske"/> <c>null</c> — dann gilt in den Kennzahlen wörtlich die
        /// Nachtzeit wie bisher (Bauvorschrift der Byte-Gleichheit, N1.61 Nr. 11).
        ///
        /// <para><b>Ohne eine einzige Anwesenheitsstunde</b> bleibt sie ebenfalls leer, und der
        /// Lauf sagt es (<see cref="NutzungsmaskeLeer"/>): Eine Maske ohne Stunde teilte die
        /// mittlere Raumtemperatur durch null.</para>
        ///
        /// <para>Die Maske wirkt <b>allein in den Kennzahlen</b>. Der Sollwertfahrplan und
        /// <see cref="Nutzungszeit(int)"/> bleiben an der Nachtzeit — sie entscheiden, wann der
        /// Tagsollwert gilt, und das ist eine Frage des Fahrplans, nicht der Anwesenheit.</para>
        /// </summary>
        private void NutzungsmaskeBilden(Konditionierungssatz satz)
        {
            double[] anwesend = satz.Reihe(Konditionierungsgroesse.Personen);
            if (anwesend == null) return;
            var maske = new bool[8760];
            bool eine = false;
            for (int h = 0; h < 8760; h++)
            {
                maske[h] = anwesend[h] > 0.0;
                if (maske[h]) eine = true;
            }
            if (!eine) { NutzungsmaskeLeer = true; return; }
            Nutzungsmaske = maske;
        }

        /// <summary>
        /// Die Bezugsgröße des Luftwechsels [W/(K·h⁻¹)] — mit dem Volumen der Zone n·V·c·ρ, ohne es
        /// über Nutzfläche und Raumhöhe; dieselbe Bildung wie <see cref="SommerlueftungBilden"/>.
        /// </summary>
        private double LuftwechselBezug()
            => double.IsNaN(Luftvolumen_M3) || Luftvolumen_M3 <= 0.0
                ? Nutzflaeche_M2 * Raumhoehe_M * GebaeudeFestwerte.C_RHO_LUFT
                : Luftvolumen_M3 * GebaeudeFestwerte.C_RHO_LUFT;

        /// <summary>
        /// <b>Die inneren Gewinne je Stunde</b> [W] aus Geräte- und Personenkalender (Konzept 3.1,
        /// P1 (b)); <c>null</c> ohne beide — dann gilt die Konstante
        /// <see cref="InnereGewinne_W"/> wörtlich wie bisher.
        /// </summary>
        internal double[] InnereGewinneReihe_W { get; private set; }

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

        /// <param name="maximalraumtemperatur">
        /// Die Prüfung F21 hier ausführen? <c>false</c> nur mit Heizkalender (Stufe KP1b): Dann
        /// prüft <see cref="MaximalraumtemperaturPruefen"/> gegen die Reihe. Jede andere Prüfung
        /// läuft unverändert.
        /// </param>
        private void Pruefen(ProjektGebaeudeModel g, bool maximalraumtemperatur = true)
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
            if (maximalraumtemperatur && !(ThetaMaxWert > SollTag))
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
                         && Gebaeudemodellvorgaben.FeriensollwertWirksam(g.Raumsolltemperatur_Ferien);
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

        /// <summary>
        /// <b>Der Bestandsfahrplan — die EINE Stelle, an der die Weiche steht:</b> Mit wirksamer
        /// Anlagenkopplung gilt das Wochenprofil (<see cref="SollwertfahrplanMitProfil"/>), sonst der
        /// Fahrplan aus vier Sollwerten (<see cref="Sollwertfahrplan"/>). <see cref="Bauen"/> ruft sie,
        /// und die Wache „Standardfahrplan bitgleich" (Konzept Konditionierungsprofile 3.3) ruft sie
        /// ebenfalls — mit einem Eingang aus <see cref="Daten"/>, ohne Klimareihe. So hält die Wache
        /// den Generator gegen <b>denselben</b> Rechenweg, den der Lauf geht, nicht gegen eine
        /// Abschrift.
        /// </summary>
        internal static double[] Bestandsfahrplan(GebaeudeModellEingang e, ProjektGebaeudeModel g,
                                                  bool[] wochenende, bool kopplungWirksam)
            => kopplungWirksam
                ? e.SollwertfahrplanMitProfil(g, wochenende)
                : Sollwertfahrplan(e, wochenende);

        /// <summary>
        /// Der Sollwertfahrplan (E8): Ferien vor Wochenende vor Tag/Nacht. Wochenend- und Ferienwert
        /// sind absolute Solltemperaturen und gelten ganztägig (auch nachts); 0 heißt „keine
        /// Absenkung" (<see cref="Gebaeudemodellvorgaben.WochenendsollwertWirksam"/>,
        /// <see cref="Gebaeudemodellvorgaben.FeriensollwertWirksam"/>).
        /// </summary>
        private static double[] Sollwertfahrplan(GebaeudeModellEingang e, bool[] wochenende)
        {
            var soll = new double[8760];
            bool weWirksam = Gebaeudemodellvorgaben.WochenendsollwertWirksam(e.SollWochenende);
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
