using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Wärmebedarf EINES Gebäudes</b> (iU9-W9.8, Anwenderwunsch <b>W9‑E‑2</b> vom
    /// 05.09.2026) — die Zahlen hinter dem Knopf „Simulation…" des Gebäudedialogs.
    ///
    /// <para><b>Nur HEIZUNG.</b> Kein Brauchwasser, keine Prozesswärme, keine
    /// Netzverluste und keine Summe über die Bedarfsarten — der Anwender hat das
    /// ausdrücklich so gewünscht („ohne Brauchwasser und ohne gesamt"). Was hier
    /// herauskommt, ist genau der Anteil, den dieses Gebäude im Lauf in den HEIZKANAL
    /// legt.</para>
    ///
    /// <para><b>Die Reihe liegt in kW</b> — dieselbe Umrechnung wie im Lauf
    /// (<c>BhkwPlan.WattToKw</c> auf dem Heizkanal). Daraus fallen Jahressumme (MWh),
    /// Höchstlast (kW) und die zwölf Monatswerte (MWh).</para>
    /// </summary>
    internal sealed class GebaeudeBedarfErgebnis
    {
        /// <summary>Wurde die Zuordnung gefunden und gerechnet?</summary>
        internal bool Erfolgreich;

        /// <summary>
        /// Der benannte Grund, aus dem die Fassade das Gebäude nicht rechnet — die Fehler, die der
        /// Rechenweg dabei ins Laufprotokoll schreibt (etwa ein Gebäude mit mehreren Zonen, Stufe
        /// G6a); <c>null</c> bei Erfolg und wenn es gar nichts zu rechnen gab (kein Projekt, keine
        /// Klimaregion, keine Projektkopie).
        /// </summary>
        internal string Befund;

        /// <summary>Der Gebäudename der Projektkopie.</summary>
        internal string Name = "";

        /// <summary>
        /// Die benannte Rückstufe (Entwurf AK3 Festlegung 20): Mit Stufe AK3 rechnet die Auskunft ohne geschlossenen
        /// Kreis auf dem Profilweg (<see cref="Ak3Kernstufe.Rueckstufetext"/>); <c>null</c> ohne Rückstufe.
        /// </summary>
        internal string Rueckstufe;

        /// <summary>Die 8 760 Stundenwerte der Heizwärme in <b>kW</b>.</summary>
        internal double[] Stundenwerte = new double[8760];

        /// <summary>Die Jahressumme in <b>MWh</b>.</summary>
        internal double HeizwaermeMwh;

        /// <summary>Die höchste Stundenlast in <b>kW</b>.</summary>
        internal double MaxLastKw;

        /// <summary>Die zwölf Monatssummen in <b>MWh</b>.</summary>
        internal double[] MonatswerteMwh = new double[12];

        /// <summary>
        /// Die Vollbenutzungsstunden [h/a] — Jahresarbeit durch Höchstlast. Bei
        /// Höchstlast 0 gibt es sie nicht (<c>null</c>), statt durch null zu teilen.
        /// </summary>
        internal double? VollbenutzungsstundenH
            => MaxLastKw > 0 ? HeizwaermeMwh * 1000.0 / MaxLastKw : (double?)null;

        // ---- Stufe G1: der Rechenweg und die Kennzahlen des Vergleichs (Konzept 1.4, 2.7) ----

        /// <summary>
        /// Der Rechenweg, auf dem gerechnet wurde (<c>DbWerte.GEBAEUDE_MODELL_*</c>) — der
        /// Spaltenwert nach der NULL-Regel der Weiche bzw. der erzwungene Weg.
        /// </summary>
        internal string Modell = "";

        /// <summary>Wurde der Rechenweg erzwungen (Vergleich alt/neu) statt aus der Spalte gelesen?</summary>
        internal bool ModellErzwungen;

        /// <summary>Größtes gleitendes Mittel über 24 Stunden [kW]; <c>null</c> ohne Ergebnis.</summary>
        internal double? SpitzeTagesmittelKw;

        /// <summary>95-%-Quantil der Stundenlast nach nächstgelegenem Rang [kW].</summary>
        internal double? SpitzeQuantil95Kw;

        /// <summary>
        /// Kühlbedarf [MWh] — der Kältebedarf am Kühlsollwert, nur auf dem VDI-Weg mit wirksamer
        /// Kühlung (<see cref="KuehlSollwertC"/>); sonst <c>null</c>: Ein Gebäude ohne wirksame
        /// Kühlung läuft frei und hat keinen Kühlbedarf (Entscheid E32).
        /// </summary>
        internal double? KuehlenergieMwh;

        /// <summary>Stunden mit Kühlbedarf [h] — nur auf dem VDI-Weg mit wirksamer Kühlung (E32).</summary>
        internal int? KuehlstundenH;

        /// <summary>Mittlere Raumlufttemperatur über die Nutzungszeit [°C] — nur auf dem VDI-Weg.</summary>
        internal double? MittlereRaumtemperaturC;

        // ---- Stufe G2: die Reihen des Bildes „Raumtemperatur" und zwei Stundenzahlen ----

        /// <summary>
        /// Stunden der Nutzungszeit mit operativer Temperatur über der oberen Raumtemperatur [h] —
        /// nur VDI-Weg; ohne wirksame Kühlung im freien Lauf gezählt (E32).
        /// </summary>
        internal int? UeberhitzungsstundenH;

        /// <summary>
        /// Stunden mit eingeschalteter Sommerlüftung [h] — nur VDI-Weg und nur, wenn eine Sommerlüftung gesetzt
        /// ist, sonst <c>null</c> (Entwurf KP3, Festlegung 26, B17; Muster der Nachtauskühlstunden); dieselbe
        /// Zahl, die der Lauf als <c>Sommerlueftungsstunden_H</c> nach <c>Tab_ErgebnisGebaeude</c> schreibt.
        /// </summary>
        internal int? SommerlueftungsstundenH;

        /// <summary>
        /// <b>Die Ergebniszeile des Gebäudes</b> (E30; Entwurf KP3, Grundsatz 4, B20) — gebildet von
        /// <see cref="GebaeudeKennzahlen.Bilden"/>, derselben Stelle, aus der der Lauf
        /// <c>Tab_ErgebnisGebaeude</c> füllt: Hier stehen die Aufheizwerte (Zustand, Bemessung, t_auf,max,
        /// T_a,B, P_auf samt Quelle, Rampentage, W1–W4, Kappungsstunden; NULL = Schalter aus) und je Zone
        /// <see cref="ErgebnisGebaeudeModel.Zonen"/> in der Reihenfolge von <see cref="Zonen"/>. Der
        /// Bedarfsdialog (Welle O2) liest die Aufheizwerte von hier; <c>null</c> ohne Ergebnis.
        /// </summary>
        internal ErgebnisGebaeudeModel Ergebniszeile;

        /// <summary>
        /// Stunden mit wirksamer Nachtauskühlung [h] (Konzept Konditionierungsprofile 3.7) — nur
        /// VDI-Weg und nur, wenn eine Nachtauskühlung gesetzt ist, sonst <c>null</c>; dieselbe Zahl,
        /// die der Lauf als <c>Nachtauskuehlstunden_H</c> nach <c>Tab_ErgebnisGebaeude</c> schreibt.
        /// </summary>
        internal int? NachtauskuehlstundenH;

        /// <summary>Raumlufttemperatur je Stunde [°C]; <c>null</c> auf dem Tagesbilanz-Weg.</summary>
        internal double[] RaumtemperaturC;

        /// <summary>Operative Temperatur je Stunde [°C]; <c>null</c> auf dem Tagesbilanz-Weg.</summary>
        internal double[] OperativeTemperaturC;

        /// <summary>Heizsollwert je Stunde [°C] — untere Kante des Sollwertbands; <c>null</c> ohne VDI-Lauf.</summary>
        internal double[] HeizsollwertC;

        /// <summary>Die obere Raumtemperatur [°C] — obere Kante des Sollwertbands; <c>null</c> ohne VDI-Lauf.</summary>
        internal double? ObereRaumtemperaturC;

        /// <summary>
        /// Die gezählten Unterschreitungsstunden der Heizseite (Anlagenkopplung AK2, 5.5) — die Maske der
        /// <c>Komfortkennzahlen</c>, aus der das Bild „Raumtemperatur und Sollwert" seine Woche wählt (E80);
        /// <c>null</c>, solange am Gebäude kein Komfort erhoben ist (ungekoppelt, Tagesbilanz).
        /// </summary>
        internal bool[] KomfortMaske;

        // ---- Stufe KU1: der Abschnitt „Kältebedarf" (Kühlkonzept 8.4; E21, F-K18) ----

        /// <summary>
        /// Die Kühlreihe des Gebäudes je Stunde [kWh] — dieselbe, die der Lauf bei wirksamer
        /// Kühlung in den Kühlkanal bucht (ein Lauf, zwei Reihen); <c>null</c> ohne VDI-Lauf und
        /// ohne wirksame Kühlung (E32).
        /// </summary>
        internal double[] KuehlbedarfKwh;

        /// <summary>Die zwölf Monatssummen der Kühlreihe [MWh]; <c>null</c> ohne Kühlreihe.</summary>
        internal double[] KuehlMonatswerteMwh;

        /// <summary>Die höchste Stundenkühllast [kW]; <c>null</c> ohne Kühlreihe.</summary>
        internal double? KaeltelastMaxKw;

        /// <summary>
        /// Vollbenutzungsstunden der Kälte [h/a] — Kühlbedarf durch Kältelast, aus beiden gebildet
        /// (Kühlkonzept 6.4); <c>null</c> ohne Kältelast.
        /// </summary>
        internal double? VollbenutzungsstundenKaelteH
            => KaeltelastMaxKw is double kw && kw > 0 && KuehlenergieMwh.HasValue
                ? KuehlenergieMwh.Value * 1000.0 / kw : (double?)null;

        /// <summary>
        /// Stunden mit gleichzeitigem Heizen und Kühlen (K6) — nicht saldiert; <c>null</c> ohne
        /// VDI-Lauf und ohne wirksame Kühlung (E32).
        /// </summary>
        internal int? StundenHeizenUndKuehlen;

        /// <summary>Die Heizwärme in diesen Stunden [kWh] (KU3-3, F-K15); <c>null</c> außerhalb des Mehrzonenwegs mit Kühlung.</summary>
        internal double? GleichzeitigHeizenKwh;

        /// <summary>Der Kältebedarf in diesen Stunden [kWh] (KU3-3, F-K15); <c>null</c> wie <see cref="GleichzeitigHeizenKwh"/>.</summary>
        internal double? GleichzeitigKuehlenKwh;

        /// <summary>Rechnet das PROJEKT Kälte (<c>Tab_Einstellungen.Kuehlbetrieb</c>)?</summary>
        internal bool KuehlbetriebProjekt;

        /// <summary>Trägt das Gebäude „Gebäude wird gekühlt" (<c>Kuehlung_Aktiv</c>)?</summary>
        internal bool KuehlungAktiv;

        /// <summary>
        /// Die Erdreichkennwerte nach DIN EN ISO 13370 des VDI-Laufs (B′, U_g, R_g);
        /// <c>null</c> ohne VDI-Lauf oder ohne Bauteil am Erdreich.
        /// </summary>
        internal Erdreichkennwerte Erdreich;

        /// <summary>
        /// Der wirksame Kühlsollwert [°C] — gesetzt genau dann, wenn der Lauf das Gebäude kühlt
        /// (Projektschalter, Haken und Sollwert); <c>null</c> = das Gebäude läuft frei (E32).
        /// </summary>
        internal double? KuehlSollwertC;

        /// <summary>Die Kühlleistungsgrenze [kW] bei wirksamer Kühlung; <c>null</c> = unbegrenzt.</summary>
        internal double? KuehlleistungMaxKw;

        // ---- Anlagenkopplung AK1 (Konzept Anlagenkopplung 9.4, 12.1): der Heizkreis -----------
        //
        // Aus DEMSELBEN Ergebnisträger wie die Kennzahlen darüber - und derselben Stelle, aus der
        // der Lauf Tab_ErgebnisGebaeude füllt (GebaeudeKennzahlen): Dialog und Bericht zeigen
        // dieselbe Zahl. Ohne wirksame Kopplung bleibt alles leer.

        /// <summary>Die Übergabeart (<c>DbWerte.UEBERGABE_*</c>); <c>null</c> = nicht gekoppelt gerechnet.</summary>
        internal string UebergabeArt;

        /// <summary>Hat der Lauf das Gebäude gekoppelt gerechnet?</summary>
        internal bool Gekoppelt => !string.IsNullOrEmpty(UebergabeArt);

        /// <summary>Vorlauf je Stunde [°C]; NaN ohne Heizbetrieb (eine Lücke im Bild).</summary>
        internal double[] VorlaufC;

        /// <summary>Rücklauf je Stunde zur gelieferten Leistung [°C]; NaN wie der Vorlauf.</summary>
        internal double[] RuecklaufC;

        /// <summary>Heizzeitgewichtetes Mittel des Vorlaufs [°C]; <c>null</c> ohne Heizstunde.</summary>
        internal double? VorlaufMittelC;

        /// <summary>Heizzeitgewichtetes Mittel des Rücklaufs [°C]; <c>null</c> ohne Heizstunde.</summary>
        internal double? RuecklaufMittelC;

        /// <summary>Stunden, in denen die Übergabe die Grenze war [h].</summary>
        internal double? UebergabeBegrenztStundenH;

        /// <summary>Der Auslegungsvorlauf, mit dem gerechnet wurde [°C] (Eingabe oder Vorgabe der Art).</summary>
        internal double? AuslegungVorlaufC;

        /// <summary>Der Auslegungsrücklauf, mit dem gerechnet wurde [°C].</summary>
        internal double? AuslegungRuecklaufC;

        // ---- E37 (Anlagenkopplung 8.3, 10.5): der Kältekreis, gespiegelt zum Heizkreis ---------
        //
        // Aus demselben Ergebnisträger; ohne wirksame Kälteseite bleibt alles leer. Alle Zahlen
        // sind sensibel (K5).

        /// <summary>Die Kühlübergabeart (<c>DbWerte.KUEHLUEBERGABE_*</c>); <c>null</c> = Kälteseite nicht gekoppelt gerechnet.</summary>
        internal string KuehlUebergabeArt;

        /// <summary>Hat der Lauf die Kälteseite des Gebäudes gekoppelt gerechnet?</summary>
        internal bool KuehlGekoppelt => !string.IsNullOrEmpty(KuehlUebergabeArt);

        /// <summary>Kaltwasser-Vorlauf je Stunde [°C] (fest).</summary>
        internal double[] KuehlVorlaufC;

        /// <summary>Rücklauf je Stunde zur gelieferten Kühlleistung [°C].</summary>
        internal double[] KuehlRuecklaufC;

        /// <summary>Kältebedarfsgewichtetes Mittel des Kaltwasser-Vorlaufs [°C]; <c>null</c> ohne Kühlstunde.</summary>
        internal double? KuehlVorlaufMittelC;

        /// <summary>Kältebedarfsgewichtetes Mittel des Rücklaufs [°C]; <c>null</c> ohne Kühlstunde.</summary>
        internal double? KuehlRuecklaufMittelC;

        /// <summary>Stunden, in denen die Kühlübergabe die Grenze war [h].</summary>
        internal double? KuehlUebergabeBegrenztStundenH;

        /// <summary>Davon die Stunden an der Vorlaufgrenze [h] (7.2).</summary>
        internal double? KuehlVorlaufgrenzeStundenH;

        /// <summary>Der Auslegungsvorlauf der Kühlübergabe, mit dem gerechnet wurde [°C].</summary>
        internal double? KuehlAuslegungVorlaufC;

        /// <summary>Der Auslegungsrücklauf der Kühlübergabe, mit dem gerechnet wurde [°C].</summary>
        internal double? KuehlAuslegungRuecklaufC;

        /// <summary>
        /// Rechnet das Gebäude auf dem Bestandsweg? Dann bucht der Lauf Kältebedarf 0 mit Hinweis
        /// (F-K18) — der Bedarfsdialog zeigt dieselbe 0 mit demselben Hinweis. Bis Stufe GA.
        /// </summary>
        internal bool KaelteBestandsweg => Erfolgreich && Modell == DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;

        // ---- Stufe G6b (W5; Anwenderentscheid A2 = M5 (a)): die Zonen eines Mehrzonengebäudes ----

        /// <summary>
        /// Die Zonen eines Mehrzonengebäudes in Rangfolge, auch die unbeheizten; leer bei höchstens
        /// einer Zone und auf dem Tagesbilanz-Weg.
        /// </summary>
        internal List<GebaeudeBedarfZone> Zonen = new List<GebaeudeBedarfZone>();
    }

    /// <summary>
    /// <b>Eine Zone eines Mehrzonengebäudes im Bedarfsdialog</b> (Stufe G6b, W5; Anwenderentscheid
    /// A2 = M5 (a)): eine Zeile je Zone, auch für eine unbeheizte — dort ohne Heizwärme, Last und
    /// Heizsollwert. Aus demselben Ergebnisträger wie der Lauf
    /// (<see cref="GebaeudeModellErgebnis.Zonen"/>); ein Gebäude mit Zonen trägt seine echte Hülle,
    /// der Skalierungsfaktor ist 1 (Festlegung 11), die Zonen gehen auf die Gebäudesumme auf.
    /// </summary>
    internal sealed class GebaeudeBedarfZone
    {
        /// <summary>Der Name der Zone.</summary>
        internal string Name = "";

        /// <summary>Wird die Zone beheizt?</summary>
        internal bool IstBeheizt;

        /// <summary>Die Nutzfläche der Zone [m²].</summary>
        internal double NutzflaecheM2;

        /// <summary>Die Jahressumme der Heizwärme [MWh]; <c>null</c> für eine unbeheizte Zone.</summary>
        internal double? HeizwaermeMwh;

        /// <summary>Die höchste Stundenlast [kW]; <c>null</c> für eine unbeheizte Zone.</summary>
        internal double? MaxLastKw;

        /// <summary>Mittlere Raumlufttemperatur über die Nutzungszeit [°C].</summary>
        internal double? MittlereRaumtemperaturC;

        /// <summary>
        /// Stunden der Nutzungszeit mit operativer Temperatur über der oberen Raumtemperatur der Zone
        /// (<c>Maximaleraumtemperatur</c>, RS 8.2, E32) [h].
        /// </summary>
        internal int UeberhitzungsstundenH;

        /// <summary>Die obere Raumtemperatur der Zone [°C].</summary>
        internal double ObereRaumtemperaturC;

        /// <summary>
        /// Stunden mit wirksamer Nachtauskühlung der Zone [h]; <c>null</c> ohne Nachtauskühlung —
        /// die Zahl, die der Lauf nach <c>Tab_ErgebnisZone</c> schreibt.
        /// </summary>
        internal int? NachtauskuehlstundenH;

        /// <summary>
        /// Stunden mit eingeschalteter Sommerlüftung der Zone [h]; <c>null</c> ohne Sommerlüftung (Entwurf KP3,
        /// Festlegung 26, E54 je Zone) — die Zahl, die der Lauf nach <c>Tab_ErgebnisZone</c> schreibt.
        /// </summary>
        internal int? SommerlueftungsstundenH;

        /// <summary>
        /// Die Zeile der Zone aus <see cref="GebaeudeKennzahlen.Bilden"/> — dieselbe, die der Lauf nach
        /// <c>Tab_ErgebnisZone</c> schreibt, samt Aufheizwerten der Zone (Entwurf KP3, B20).
        /// </summary>
        internal ErgebnisZoneModel Ergebniszeile;

        /// <summary>Die Heizlast je Stunde [kW]; <c>null</c> für eine unbeheizte Zone.</summary>
        internal double[] HeizlastKw;

        /// <summary>Raumlufttemperatur je Stunde [°C].</summary>
        internal double[] RaumtemperaturC;

        /// <summary>Operative Temperatur je Stunde [°C].</summary>
        internal double[] OperativeTemperaturC;

        /// <summary>Heizsollwert je Stunde [°C]; <c>null</c> für eine unbeheizte Zone.</summary>
        internal double[] HeizsollwertC;

        /// <summary>Jahressumme des Kältebedarfs der Zone [MWh] (KU3-3); <c>null</c> ohne wirksame Kühlung der Zone.</summary>
        internal double? KaeltebedarfMwh;

        /// <summary>Die höchste Stunde des Kältebedarfs der Zone [kW] (KU3-3); <c>null</c> wie <see cref="KaeltebedarfMwh"/>.</summary>
        internal double? KaeltespitzeKw;

        /// <summary>Stunden mit Kältebedarf der Zone [h] (KU3-3); <c>null</c> wie <see cref="KaeltebedarfMwh"/>.</summary>
        internal int? KuehlstundenH;
    }

    /// <summary>
    /// <b>Die Bedarfsrechnung für EIN Gebäude</b> (iU9-W9.8) — der Rechenweg hinter dem
    /// Knopf „Simulation…" im Detailblock „Gebäude: Verbrauch"
    /// (<c>EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor</c>).
    ///
    /// <para><b>Das Vorbild.</b> Der WinForms-Bestand kannte KEINEN solchen Knopf: Die
    /// gelöschte <c>Form_Gebaeude</c> führte im Detailblock nur „Ändern", und
    /// <c>Form_Simulation_Kurz</c> (mit iF29 stillgelegt) rechnete das GANZE Projekt —
    /// Konfiguration, Wärme- und Strombedarf, Kaskade. Der Anwender wünscht die Auskunft
    /// je Gebäude, analog zu dem, was der Bedarfsreiter der Ergebnisseite für das Projekt
    /// zeigt. Neu ist also die AUSKUNFT, nicht die Rechnung.</para>
    ///
    /// <para><b>Es ist dieselbe Rechnung, kein Zwilling.</b> Gerechnet wird über
    /// <see cref="SimulationWaermebedarf.KlimakalenderLesen"/> und
    /// <see cref="SimulationWaermebedarf.HeizwaermeEinesGebaeudes"/> — dieselben zwei
    /// Methoden, die <c>Waermebedarf_berechnen</c> in seiner Gebäudeschleife ruft. Damit
    /// gilt: <b>Σ über alle Gebäude eines Projekts = <c>Waermebedarf_Gebaeude_Gesamt</c>
    /// des Laufs</b>, und bei einem Projekt mit genau EINEM Gebäude sind beide Zahlen
    /// gleich. Der Nachweis steht in
    /// <c>EPOS.Kern.Tests/GebaeudeBedarfCtrlTests.cs</c>.</para>
    ///
    /// <para><b>Was NICHT eingeht</b>, weil es nicht am Gebäude hängt: die externen
    /// Wärmelastgänge (<c>Z_ProjektWaermebedarf</c>), Brauchwasser- und Prozessprofile
    /// und die anteilig verteilten Netzverluste. Die Zahl des Dialogs ist deshalb der
    /// reine Gebäudeanteil und nicht der ganze Heizkanal des Laufs.</para>
    ///
    /// <para><b>Gelesen, nicht geschrieben.</b> Der Controller fasst die Datenbank nur
    /// lesend an; der Referenzlauf ist unberührt.</para>
    /// </summary>
    internal static class GebaeudeBedarfCtrl
    {
        /// <summary>Das feste Stundenraster des Rechenkerns.</summary>
        private const int STUNDEN_JAHR = 8760;

        /// <summary>
        /// Rechnet die Heizwärme der Zuordnung <paramref name="idZ"/> im Projekt
        /// <paramref name="idProjekt"/>.
        /// </summary>
        /// <param name="idProjekt">Das Projekt (<c>Z_ProjektGebaeude.ID_Projekt</c>).</param>
        /// <param name="idKlimaregion">Die Klimaregion des Projekts
        /// (<c>Tab_Projekt.ID_Klimaregion</c>). 0 heißt „keine" — dann gibt es kein
        /// Ergebnis, wie im Lauf.</param>
        /// <param name="idZ">Der Schlüssel der ZUORDNUNG (<c>Z_ProjektGebaeude.ID</c>) —
        /// nicht die Stamm-Id: Zwei gleiche Gebäude im Projekt teilen sich eine
        /// Stamm-Id.</param>
        /// <param name="modellErzwungen">Der Rechenweg, auf dem gerechnet wird
        /// (<c>DbWerte.GEBAEUDE_MODELL_*</c>); <c>null</c> = der Spaltenwert des Gebäudes. Er
        /// wirkt allein auf der GELESENEN Modellinstanz, schreibt nichts und ruft dieselbe
        /// Weiche wie der Lauf — so entstehen die beiden Spalten des Vergleichs alt/neu aus
        /// zwei Aufrufen desselben Controllers (Umsetzungskonzept 1.4, 2.7). Er lebt, solange
        /// es zwei Rechenwege gibt (bis Stufe GA, Löschliste Kapitel 6).</param>
        internal static GebaeudeBedarfErgebnis Rechnen(int idProjekt, int idKlimaregion, int idZ,
                                                       string modellErzwungen = null)
        {
            if (idProjekt <= 0 || idKlimaregion <= 0 || idZ <= 0) return new GebaeudeBedarfErgebnis();
            return Rechnen(idProjekt, idKlimaregion, Projektgebaeude(idProjekt, idZ), modellErzwungen);
        }

        /// <summary>
        /// <b>Dieselbe Rechnung für ein Gebäudemodell</b> statt einer gespeicherten Zuordnung — der
        /// Eingang des Arbeitsstands (<see cref="Arbeitsstandgebaeude"/>): Ein eben in das Projekt
        /// übernommenes Gebäude hat vor dem OK noch keine Projektkopie, rechnet aber über DIESELBE
        /// Fassade (<see cref="SimulationWaermebedarf.HeizwaermeEinesGebaeudes"/>) mit dem
        /// Klimakalender und den Schaltern des Projekts. Das Modell wird dabei verändert wie im Lauf
        /// (Rechenweg, Bewohner, Bezugsfläche der Verbrauchsangabe) — je Aufruf ein frisches Modell.
        /// Schreibt nichts.
        /// </summary>
        internal static GebaeudeBedarfErgebnis Rechnen(int idProjekt, int idKlimaregion, ProjektGebaeudeModel gebaeude,
                                                       string modellErzwungen = null)
        {
            var ergebnis = new GebaeudeBedarfErgebnis();
            if (idProjekt <= 0 || idKlimaregion <= 0 || gebaeude == null) return ergebnis;

            if (modellErzwungen != null)
            {
                gebaeude.Gebaeude_Modell = modellErzwungen;
                ergebnis.ModellErzwungen = true;
            }
            ergebnis.Modell = Gebaeuderechenweg.Wirksam(gebaeude.Gebaeude_Modell);

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(idKlimaregion);

            // Der Merkplatz 0 in HeizwaermebedarfGeb - eine Rechnung fuer EIN Gebaeude
            // braucht keinen Rang, siehe HeizwaermeEinesGebaeudes.
            var werte = new double[STUNDEN_JAHR];
            int vorher = SimulationProtokoll.Aktuell.Fehler.Count;
            // AK2-2b: im gekoppelten Projekt mit demselben Anlagenfahrplan wie der Lauf - sonst zeigte die
            // Auskunft andere Zahlen als der Lauf.
            if (!sim.HeizwaermeEinesGebaeudesWieImLauf(idProjekt, idKlimaregion, gebaeude, werte))
            {
                // Der benannte Grund aus dem Laufprotokoll (Stufe G6a) - dieselbe Lesart wie beim
                // Hochrechnungsfaktor der Uebernahme.
                IList<string> fehler = SimulationProtokoll.Aktuell.Fehler;
                if (fehler.Count > vorher) ergebnis.Befund = string.Join(" ", fehler.Skip(vorher));
                return ergebnis;
            }

            // Dieselbe Umrechnung wie im Lauf: der Heizkanal geht als WATT in die
            // Schleife und wird danach EINMAL nach kW gebracht.
            WPPlan.Core.BhkwPlan.WattToKw(werte);

            ergebnis.Name = gebaeude.Gebaeudename ?? "";
            ergebnis.Stundenwerte = werte;
            // Festlegung 20: mit Stufe AK3 rechnet die Auskunft auf dem Profilweg - benannt.
            ergebnis.Rueckstufe = sim.Ak3Rueckstufe ? Ak3Kernstufe.Rueckstufetext : null;

            // ZEICHENGLEICH zum Lauf: dort steht "kanalHeizung.Sum() / 1000" - eine
            // double-Summe durch eine GANZE Zahl, also eine double-Division. Ein
            // "/ 1000.0" waere eine double-Division und ergaebe eine andere neunte
            // Stelle; der Anwender legt die zwei Zahlen nebeneinander.
            ergebnis.HeizwaermeMwh = werte.Sum() / 1000;
            ergebnis.MaxLastKw = GebaeudeKennzahlen.Hoechstwert(werte);
            WPPlan.Core.BhkwPlan.MonatsSumme(werte, ergebnis.MonatswerteMwh,
                                             sim.mo_anfang, sim.mo_ende);
            // Die Spitzenwerte bildet GebaeudeKennzahlen - dieselbe Stelle, aus der der Lauf
            // die Zeilen von Tab_ErgebnisGebaeude fuellt (E30): Dialog und Bericht zeigen
            // dieselbe Zahl.
            ergebnis.SpitzeTagesmittelKw = GebaeudeKennzahlen.GroesstesTagesmittel(werte);
            ergebnis.SpitzeQuantil95Kw = GebaeudeKennzahlen.Quantil95(werte);

            // Die Kennzahlen, die es nur auf dem VDI-Weg gibt, kommen aus dem Ergebnistraeger
            // des Laufs (Merkplatz 0) - skaliert nach E8 wie die Reihe.
            GebaeudeModellErgebnis vdi = sim.GebaeudeErgebnisse.Ergebnis(0);
            if (vdi != null)
            {
                // Anlagenkopplung AK2 (5.5, E80): die Maske der Heizseite - nur am gekoppelten Gebaeude.
                if (GebaeudeKennzahlen.KomfortErhoben(vdi))
                    ergebnis.KomfortMaske = Komfortkennzahlen.Heizseite(vdi)?.Maske;
                ergebnis.KuehlenergieMwh = vdi.KuehlenergieMwh;
                ergebnis.Erdreich = vdi.Erdreich;
                ergebnis.KuehlstundenH = vdi.StundenMitKuehlbedarf;
                ergebnis.MittlereRaumtemperaturC = vdi.MittlereRaumtemperaturHeizzeit;
                ergebnis.UeberhitzungsstundenH = vdi.Ueberhitzungsstunden;
                ergebnis.SommerlueftungsstundenH = GebaeudeKennzahlen.Sommerlueftungsstunden(vdi);
                ergebnis.NachtauskuehlstundenH = vdi.StundenMitNachtauskuehlung;
                ergebnis.RaumtemperaturC = vdi.Raumtemperatur;
                ergebnis.OperativeTemperaturC = vdi.OperativeTemperatur;
                ergebnis.HeizsollwertC = vdi.Heizsollwert;
                ergebnis.ObereRaumtemperaturC = vdi.ThetaMax;

                // Stufe G6b (W5; A2 = M5 (a)): je Zone eine Zeile, auch unbeheizt - aus DEMSELBEN
                // Ergebnistraeger. Die Gebaeudezahlen darueber sind Summe bzw. Mittel der Zonen
                // nach Festlegung 10 (GebaeudeModellErgebnis.ZonenAnhaengen).
                if (vdi.Zonen != null)
                    foreach (GebaeudeZonenergebnis z in vdi.Zonen)
                        ergebnis.Zonen.Add(Zone(z));

                // Stufe KU1 (Kuehlkonzept 8.4, E21): der Abschnitt „Kaeltebedarf" aus DEMSELBEN
                // Ergebnis - die Kuehlreihe, die der Lauf bei wirksamer Kuehlung in den
                // Kuehlkanal bucht, samt Spitze, Monatswerten und K6. Ohne wirksame Kuehlung
                // laeuft das Gebaeude frei und hat keine Kuehlreihe (E32): Die Kaeltezahlen
                // bleiben null und erscheinen als „—", die Ueberhitzungsstunden stehen.
                if (vdi.KuehlbedarfKwh != null)
                {
                    double[] kuehl = (double[])vdi.KuehlbedarfKwh.Clone();
                    ergebnis.KuehlbedarfKwh = kuehl;
                    ergebnis.KaeltelastMaxKw = GebaeudeKennzahlen.Hoechstwert(kuehl);
                    ergebnis.KuehlMonatswerteMwh = new double[12];
                    WPPlan.Core.BhkwPlan.MonatsSumme(kuehl, ergebnis.KuehlMonatswerteMwh,
                                                     sim.mo_anfang, sim.mo_ende);
                    ergebnis.StundenHeizenUndKuehlen = vdi.StundenHeizenUndKuehlen;
                    ergebnis.GleichzeitigHeizenKwh = vdi.GleichzeitigHeizenKwh;
                    ergebnis.GleichzeitigKuehlenKwh = vdi.GleichzeitigKuehlenKwh;
                }
                ergebnis.KuehlSollwertC = vdi.KuehlSollwert;

                // Anlagenkopplung AK1 (9.4): der Heizkreis desselben Laufs - nur bei wirksamer Kopplung.
                HeizkreisErgebnis hk = vdi.Heizkreis;
                if (hk != null)
                {
                    ergebnis.UebergabeArt = hk.UebergabeArt;
                    ergebnis.VorlaufC = hk.VorlaufC;
                    ergebnis.RuecklaufC = hk.RuecklaufC;
                    ergebnis.VorlaufMittelC = double.IsNaN(hk.VorlaufMittelC) ? null : hk.VorlaufMittelC;
                    ergebnis.RuecklaufMittelC = double.IsNaN(hk.RuecklaufMittelC) ? null : hk.RuecklaufMittelC;
                    ergebnis.UebergabeBegrenztStundenH = hk.UebergabeBegrenztStundenH;
                    ergebnis.AuslegungVorlaufC = hk.AuslegungVorlaufC;
                    ergebnis.AuslegungRuecklaufC = hk.AuslegungRuecklaufC;
                }

                // E37: der Kaeltekreis desselben Laufs - nur bei wirksamer Kaelteseite.
                KuehlkreisErgebnis kk = vdi.Kuehlkreis;
                if (kk != null)
                {
                    ergebnis.KuehlUebergabeArt = kk.UebergabeArt;
                    ergebnis.KuehlVorlaufC = kk.VorlaufC;
                    ergebnis.KuehlRuecklaufC = kk.RuecklaufC;
                    ergebnis.KuehlVorlaufMittelC = double.IsNaN(kk.VorlaufMittelC) ? null : kk.VorlaufMittelC;
                    ergebnis.KuehlRuecklaufMittelC = double.IsNaN(kk.RuecklaufMittelC) ? null : kk.RuecklaufMittelC;
                    ergebnis.KuehlUebergabeBegrenztStundenH = kk.UebergabeBegrenztStundenH;
                    ergebnis.KuehlVorlaufgrenzeStundenH = kk.VorlaufgrenzeStundenH;
                    ergebnis.KuehlAuslegungVorlaufC = kk.AuslegungVorlaufC;
                    ergebnis.KuehlAuslegungRuecklaufC = kk.AuslegungRuecklaufC;
                }
            }
            // Stufe KP3 (Grundsatz 4, B20): die Ergebniszeile des Laufs aus DERSELBEN Stelle (E30) - mit den
            // Aufheizwerten des Gebaeudes und je Zone; die Reihe ist schon in kW wie im Lauf.
            ergebnis.Ergebniszeile = GebaeudeKennzahlen.Bilden(0, gebaeude.ID_Gebaeude, gebaeude.Gebaeudename,
                                                               ergebnis.Modell, werte, vdi);
            for (int k = 0; k < ergebnis.Zonen.Count && k < ergebnis.Ergebniszeile.Zonen.Count; k++)
                ergebnis.Zonen[k].Ergebniszeile = ergebnis.Ergebniszeile.Zonen[k];
            ergebnis.KuehlbetriebProjekt = sim.KuehlbetriebProjekt;
            ergebnis.KuehlungAktiv = gebaeude.Kuehlung_Aktiv;
            ergebnis.KuehlleistungMaxKw = ergebnis.KuehlSollwertC.HasValue ? gebaeude.Kuehlleistung_Max : null;
            ergebnis.Erfolgreich = true;
            return ergebnis;
        }

        /// <summary>
        /// <b>Die Aufheizbemessung je Gebäude des Projekts, ohne Jahreslauf</b> (Entwurf KP3, Welle D2;
        /// Grundsatz 3, Festlegung 3, B14) — die Auskunft hinter den Herleitungszeilen der Projekteinstellung
        /// „Aufheizoptimierung". Je Gebäude in der Reihenfolge des Laufs
        /// (<see cref="SimulationWaermebedarf.AufheizbemessungEinesGebaeudes"/>, Klimakalender und Schalter des
        /// Projekts wie im Lauf); leer ohne Projekt, ohne Klimaregion oder mit ausgeschalteter Optimierung.
        /// Gelesen, nicht geschrieben.
        /// </summary>
        internal static IReadOnlyList<Aufheizauskunft> Aufheizbemessung(int idProjekt, int idKlimaregion)
        {
            var liste = new List<Aufheizauskunft>();
            if (idProjekt <= 0 || idKlimaregion <= 0) return liste;
            if (!KonfigurationCtrl.AufheizvorgabeLesen(idProjekt).An) return liste;

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(idKlimaregion);
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            for (int i = 0; i < ctrl.rows; i++)
                liste.Add(sim.AufheizbemessungEinesGebaeudes(ctrl.items[i]));
            return liste;
        }

        /// <summary>
        /// Die Zeile EINER Zone (Stufe G6b, W5): Reihen und Kennzahlen aus dem Zonenergebnis, die
        /// Heizlast mit derselben Umrechnung wie die Gebäudereihe (Watt → kW, Jahressumme durch
        /// 1000); eine unbeheizte Zone trägt keine Energie und keinen Heizsollwert (A2).
        /// </summary>
        private static GebaeudeBedarfZone Zone(GebaeudeZonenergebnis z)
        {
            GebaeudeModellErgebnis r = z.Ergebnis;
            var zone = new GebaeudeBedarfZone
            {
                Name = z.Bezeichnung ?? "",
                IstBeheizt = z.IstBeheizt,
                NutzflaecheM2 = z.Nutzflaeche_M2,
                MittlereRaumtemperaturC = r.MittlereRaumtemperaturHeizzeit,
                UeberhitzungsstundenH = r.Ueberhitzungsstunden,
                ObereRaumtemperaturC = r.ThetaMax,
                NachtauskuehlstundenH = r.StundenMitNachtauskuehlung,
                SommerlueftungsstundenH = GebaeudeKennzahlen.Sommerlueftungsstunden(r),
                RaumtemperaturC = r.Raumtemperatur,
                OperativeTemperaturC = r.OperativeTemperatur,
                // KU3-3: die Kälte der Zone aus ihrem Ergebnis - nur mit wirksamer Kühlung (E32).
                KaeltebedarfMwh = r.KuehlenergieMwh,
                KaeltespitzeKw = r.KaeltespitzeKw,
                KuehlstundenH = r.StundenMitKuehlbedarf
            };
            if (z.IstBeheizt)
            {
                double[] kw = (double[])r.HeizlastW.Clone();
                WPPlan.Core.BhkwPlan.WattToKw(kw);
                zone.HeizlastKw = kw;
                zone.HeizwaermeMwh = kw.Sum() / 1000;
                zone.MaxLastKw = GebaeudeKennzahlen.Hoechstwert(kw);
                zone.HeizsollwertC = r.Heizsollwert;
            }
            return zone;
        }

        /// <summary>
        /// <b>Der Wärmerestbedarf des Projekts aus dem letzten gespeicherten Lauf</b> [MWh/a] (Anlagenkopplung 5.5,
        /// AK2-3): die Zahl, die neben den Komfortstunden steht — mit Kopplung ist ein Teil der Unterdeckung eine
        /// gesunkene Raumtemperatur. Gelesen, nicht gerechnet; <c>null</c> ohne Projekt oder ohne Lauf.
        /// </summary>
        internal static double? RestbedarfDesProjektsMwh(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT e.Waermerestbedarf FROM " + ErgebnisCtrl.TAB_ENERGIE + " e INNER JOIN " +
                    ErgebnisCtrl.TAB_KOPF + " k ON k.ID = e.ID_Ergebnis WHERE k.ID_Projekt = ? ORDER BY k.ID DESC LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return null;
                return Convert.ToDouble(dt.Rows[0][0], System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// <b>Die Kennzahlen des geschlossenen Kreises aus dem letzten gespeicherten Lauf</b> (Anlagenkopplung AK3,
        /// Festlegung 22; W4b): sie stehen im Bedarfsdialog neben Komfort und Restbedarf. Gelesen, nicht gerechnet;
        /// <c>null</c> ohne Projekt, ohne Lauf, vor dem Schemaschritt oder wenn der letzte Lauf den Kreis nicht rechnete.
        /// </summary>
        internal static Ak3Kennzahlen Ak3KennzahlenDesProjekts(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                if (!Ak3Schema.ErgebnisspaltenVorhanden()) return null;
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT " + string.Join(", ", Ak3Schema.SPALTEN_ERGEBNIS.Select(sp => "e." + sp)) + " FROM " +
                    ErgebnisCtrl.TAB_ENERGIE + " e INNER JOIN " + ErgebnisCtrl.TAB_KOPF +
                    " k ON k.ID = e.ID_Ergebnis WHERE k.ID_Projekt = ? ORDER BY k.ID DESC LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];
                return Ak3Kennzahlen.Aus(new ErgebnisEnergiebedarfModel
                {
                    Ak3DurchlaeufeMittel = r[Ak3Schema.SPALTE_DURCHLAEUFE_MITTEL] is DBNull ? null
                        : Convert.ToDouble(r[Ak3Schema.SPALTE_DURCHLAEUFE_MITTEL], CultureInfo.InvariantCulture),
                    Ak3DurchlaeufeMax = Ganz(r, Ak3Schema.SPALTE_DURCHLAEUFE_MAX),
                    Ak3Fallwechsel = Ganz(r, Ak3Schema.SPALTE_FALLWECHSEL),
                    Ak3SchrankeStundenH = Ganz(r, Ak3Schema.SPALTE_SCHRANKE_STUNDEN),
                    Ak3SpeicherLeerStundenH = Ganz(r, Ak3Schema.SPALTE_SPEICHER_LEER_STUNDEN),
                    Ak3RestbedarfStundenH = Ganz(r, Ak3Schema.SPALTE_RESTBEDARF_STUNDEN),
                });
            }
            catch
            {
                return null;
            }

            static int? Ganz(DataRow r, string spalte)
                => r[spalte] is DBNull ? null : Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Die Kennzahlen von AK3-K aus dem letzten gespeicherten Lauf</b> (Entwurf AK3-K 3.5, Festlegung 20): Zonensperre
        /// und Kälteseite im Kreis, im Bedarfsdialog neben den Kennzahlen des Kreises. Gelesen, nicht gerechnet; <c>null</c>
        /// ohne Projekt, ohne Lauf, vor dem Schemaschritt oder wenn der letzte Lauf keine der beiden Seiten erhob.
        /// </summary>
        internal static Ak3KKennzahlen Ak3KKennzahlenDesProjekts(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                if (!Ak3KSchema.ErgebnisspaltenVorhanden()) return null;
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT " + string.Join(", ", Ak3KSchema.SPALTEN_ERGEBNIS.Select(sp => "e." + sp)) + " FROM " +
                    ErgebnisCtrl.TAB_ENERGIE + " e INNER JOIN " + ErgebnisCtrl.TAB_KOPF +
                    " k ON k.ID = e.ID_Ergebnis WHERE k.ID_Projekt = ? ORDER BY k.ID DESC LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];
                return Ak3KKennzahlen.Aus(new ErgebnisEnergiebedarfModel
                {
                    ZonensperreTage = Ganz(r, Ak3KSchema.SPALTE_ZONENSPERRE_TAGE),
                    ZonensperreHeizenGesperrtMwh = Zahl(r, Ak3KSchema.SPALTE_ZONENSPERRE_HEIZEN_MWH),
                    ZonensperreKuehlenGesperrtMwh = Zahl(r, Ak3KSchema.SPALTE_ZONENSPERRE_KUEHLEN_MWH),
                    Ak3KaelteschrankeStundenH = Ganz(r, Ak3KSchema.SPALTE_KAELTESCHRANKE_STUNDEN),
                    Ak3UmschaltStundenH = Ganz(r, Ak3KSchema.SPALTE_UMSCHALT_STUNDEN),
                    Ak3KaelterestStundenH = Ganz(r, Ak3KSchema.SPALTE_KAELTEREST_STUNDEN),
                    Ak3KaelterestMwh = Zahl(r, Ak3KSchema.SPALTE_KAELTEREST_MWH),
                });
            }
            catch
            {
                return null;
            }

            static int? Ganz(DataRow r, string spalte)
                => r[spalte] is DBNull ? null : Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);

            static double? Zahl(DataRow r, string spalte)
                => r[spalte] is DBNull ? null : Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Die Kennzahlen der Kühlkurve aus dem letzten gespeicherten Lauf</b> (Entwurf KK, Festlegung 12; Schritt 202):
        /// mittlerer Kühlvorlauf, Absenkung durch den Raumeinfluss und Stunden an der Vorlaufgrenze, im Bedarfsdialog neben
        /// den Kennzahlen der Kälteseite im Kreis. Gelesen, nicht gerechnet; <c>null</c> ohne Projekt, ohne Lauf, vor dem
        /// Schemaschritt oder wenn der letzte Lauf keine wirksame Kühlkurve rechnete.
        /// </summary>
        internal static KuehlkurveKennzahlen KuehlkurveKennzahlenDesProjekts(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                if (!KuehlkurveSchema.ErgebnisspaltenVorhanden()) return null;
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT " + string.Join(", ", KuehlkurveSchema.SPALTEN_ERGEBNIS.Select(sp => "e." + sp)) + " FROM " +
                    ErgebnisCtrl.TAB_ENERGIE + " e INNER JOIN " + ErgebnisCtrl.TAB_KOPF +
                    " k ON k.ID = e.ID_Ergebnis WHERE k.ID_Projekt = ? ORDER BY k.ID DESC LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];
                return KuehlkurveKennzahlen.Aus(new ErgebnisEnergiebedarfModel
                {
                    KuehlkurveVorlaufMittelC = Zahl(r, KuehlkurveSchema.SPALTE_VORLAUF_MITTEL),
                    KuehlkurveAbsenkungKh = Zahl(r, KuehlkurveSchema.SPALTE_ABSENKUNG_KH),
                    KuehlkurveVorlaufgrenzeStundenH = r[KuehlkurveSchema.SPALTE_VORLAUFGRENZE_STUNDEN] is DBNull
                        ? null
                        : Convert.ToInt32(r[KuehlkurveSchema.SPALTE_VORLAUFGRENZE_STUNDEN], CultureInfo.InvariantCulture),
                });
            }
            catch
            {
                return null;
            }

            static double? Zahl(DataRow r, string spalte)
                => r[spalte] is DBNull ? null : Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Das Warmwasser des PROJEKTS</b> [MWh/a] — die Auskunftszeile unter den Kennzahlen des
        /// Gebäudedialogs. Die Heizwärme des Dialogs ist nach VDI 6007 allein der Anteil des Gebäudes
        /// im Heizkanal; das Warmwasser hängt nicht am Gebäude, sondern an den Brauchwasserprofilen
        /// bzw. dem Zapfprofil des Projekts und läuft im Lauf als eigener Kanal Warmwasser. Die Zeile
        /// sagt dem Anwender, dass es gerechnet wird und wie viel — ohne es in die Gebäudezahlen zu
        /// mischen.
        ///
        /// <para><b>Gerufen, nicht nachgerechnet:</b> dieselbe Weiche wie
        /// <see cref="SimulationWaermebedarf.Brauchwasserwaerme_berechnen"/> — auf dem Bestandsweg
        /// die Profilroutine (Monatswerte × Wochenprofil), auf dem Generatorweg der
        /// Zapfprofilgenerator, dort wie in der Vorschau deterministisch (die Jahresmenge ist in
        /// beiden Wegen dieselbe, Energieprobe). Die Zahl ist die Profilsumme ohne die anteilig
        /// verteilten Netzverluste — dieselbe wie <c>Waermebedarf_Brauchwasser</c> des Laufs.</para>
        /// </summary>
        /// <returns>Die Jahressumme in MWh, 0 ohne Profil; <c>null</c>, wenn es keine Zahl gibt
        /// (kein Projekt, keine Klimaregion, benannter Abbruch des Generatorwegs).</returns>
        internal static double? WarmwasserDesProjektsMwh(int idProjekt, int idKlimaregion)
        {
            if (idProjekt <= 0 || idKlimaregion <= 0) return null;

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            // Der Kalender des Generatorwegs genügt beiden Wegen: Er liefert die
            // Wochenendkennzeichen und daraus den Wochentag des 1. Januar - mehr liest die
            // Profilroutine vom Klimakalender nicht.
            sim.ZapfprofilKalenderLesen(idKlimaregion);

            if (ZapfprofilCtrl.Weg(idProjekt) == BrauchwasserWeg.Generator)
            {
                ZapfprofilStand stand;
                try { stand = ZapfprofilCtrl.Lies(idProjekt); }
                catch (Exception) { return null; }
                if (stand.Projekt != null && stand.Projekt.JahresreiheStochastisch)
                    stand = stand with { Projekt = stand.Projekt with { JahresreiheStochastisch = false } };
                if (!sim.BrauchwasserAusGenerator(stand)) return null;
            }
            else
            {
                sim.Brauchwasserwaerme_berechnen();
            }

            sim.BrauchwassersummeUebernehmen();
            return sim.Waermebedarf_Brauchwasser;
        }

        /// <summary>
        /// <b>Der Hochrechnungsfaktor der Fassade</b> (E8; Anwenderentscheid vom 25.09.2026
        /// „Hochrechnen“) für „Gebäude als eine Zone übernehmen": der Faktor, mit dem die Fassade
        /// den Katalogbau dieses Gebäudes nachmultipliziert — bei einer Flächenangabe Projektfläche
        /// / Nutzfläche, bei einer Verbrauchsangabe aus der Verhältnisrechnung des Kataloglaufs.
        /// Er wird <b>gerufen, nicht nachgerechnet</b>: dieselbe Fassade
        /// (<see cref="SimulationWaermebedarf.HeizwaermeEinesGebaeudes"/>) wie Lauf und
        /// <see cref="Rechnen"/>, der Faktor ist der, den sie in den Ergebnisträger schreibt
        /// (<see cref="GebaeudeModellErgebnis.Skalierungsfaktor"/>).
        ///
        /// <para>Gerechnet wird der <b>Klassenweg auf dem VDI-Weg</b>: Die Zonen der Zeile werden
        /// abgehängt (die Übernahme gilt einem Gebäude ohne Zone), und der Rechenweg steht auf
        /// VDI 6007, weil nur er eine Zone rechnet. Die Zeile <paramref name="gebaeude"/> wird dabei
        /// GESCHRIEBEN wie im Lauf (<c>Bewohner</c>, <c>Z_AuswahlWohnflaeche</c>); die Datenbank
        /// nicht.</para>
        /// </summary>
        /// <returns>Der Faktor (größer null); NaN, wenn die Fassade das Gebäude nicht rechnet —
        /// <paramref name="befund"/> nennt dann den Grund aus dem Laufprotokoll.</returns>
        internal static double Hochrechnungsfaktor(int idProjekt, int idKlimaregion, ProjektGebaeudeModel gebaeude,
                                                   out string befund)
        {
            befund = null;
            if (gebaeude == null || idProjekt <= 0 || idKlimaregion <= 0)
            {
                befund = MyResource.Resource.ZONE_MSG_UEBERNAHME_KEIN_KLIMA;
                return double.NaN;
            }

            gebaeude.Zonen = null;
            gebaeude.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(idKlimaregion);

            int vorher = SimulationProtokoll.Aktuell.Fehler.Count;
            var werte = new double[STUNDEN_JAHR];
            bool gerechnet = sim.HeizwaermeEinesGebaeudes(gebaeude, 0, werte);
            GebaeudeModellErgebnis e = sim.GebaeudeErgebnisse.Ergebnis(0);
            if (gerechnet && e != null && e.Skalierungsfaktor > 0.0 && !double.IsInfinity(e.Skalierungsfaktor))
                return e.Skalierungsfaktor;

            IList<string> fehler = SimulationProtokoll.Aktuell.Fehler;
            befund = fehler.Count > vorher
                ? string.Join(" ", fehler.Skip(vorher))
                : MyResource.Resource.ZONE_MSG_UEBERNAHME_KEIN_FAKTOR;
            return double.NaN;
        }

        /// <summary>
        /// Das Projektgebäude der Zuordnung <paramref name="idZ"/> so, wie der Lauf es liest
        /// (Sicht <c>Abfrage_Projektgebaeude</c>); <c>null</c>, wenn es keins gibt. Auch der
        /// Gebäudedialog liest hierüber Rechenweg und Wärmeleitwert einer Projektzeile.
        /// </summary>
        internal static ProjektGebaeudeModel Projektgebaeude(int idProjekt, int idZ)
        {
            if (idProjekt <= 0 || idZ <= 0) return null;

            int idTabGebaeude = TabGebaeudeId(idZ);
            if (idTabGebaeude == 0) return null;

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);

            for (int i = 0; i < ctrl.rows; i++)
                if (ctrl.items[i].ID_Gebaeude == idTabGebaeude) return ctrl.items[i];
            return null;
        }

        /// <summary>
        /// <b>Das Projektgebäude aus dem Arbeitsstand des Gebäudedialogs</b> — so, wie der Lauf es
        /// nach dem OK läse, ohne dass etwas geschrieben ist (Hausregel: geschrieben wird im OK-Weg).
        /// <list type="bullet">
        /// <item>Eine gespeicherte Zuordnung (<paramref name="idZ"/> &gt; 0) liest ihre Projektkopie
        /// samt Zonen (<see cref="Projektgebaeude"/>).</item>
        /// <item>Eine noch nicht gespeicherte (<paramref name="idZ"/> = 0) entsteht aus dem
        /// Katalogsatz, den der Speicherweg kopieren wird (<see cref="GebaeudeStammCtrl.Katalogzeile"/>
        /// — dieselbe Suchregel wie <c>CopyFromStamm</c>), mit dessen Übertragungsregel (die
        /// Bestandsspalten NULL → 0 bzw. leer, die neuen Spalten NULL-erhaltend) und durch DENSELBEN
        /// Leser wie die Sicht (<see cref="ProjektGebaeudeCtrl.AusZeile"/>). Sie trägt keine Id und
        /// keine Zone.</item>
        /// </list>
        /// Darüber liegen die Zuordnungswerte der Zeile (Angabe, Art der Angabe, Jahresnutzungsgrad,
        /// dezentrale Warmwasserbereitung) — auch eine noch nicht gespeicherte Änderung über „Fläche
        /// und Verbrauch…". <c>null</c>, wenn es die Projektkopie bzw. den Katalogsatz nicht gibt.
        /// </summary>
        internal static ProjektGebaeudeModel Arbeitsstandgebaeude(int idProjekt, int idZ, int? idStamm, string name,
                                                                  double angabe, string einheit,
                                                                  double jahresnutzungsgrad, bool dezentralWarmwasser)
        {
            if (idProjekt <= 0) return null;

            ProjektGebaeudeModel g;
            if (idZ > 0)
                g = Projektgebaeude(idProjekt, idZ);
            else
            {
                DataRow stamm = GebaeudeStammCtrl.Katalogzeile(idStamm, name);
                g = stamm == null ? null : ProjektGebaeudeCtrl.AusZeile(Kopiezeile(stamm, idProjekt));
                // Stufe KP1b (Befund NB3): Die Vorschau reicht den Katalogbau herein - sein Kalender
                // und seine Matrix reisen beim OK mit (CopyFromStamm), also rechnet die Vorschau
                // schon jetzt damit. Ohne ihn las der Datenweg ID_Gebaeude = 0 und fand nichts.
                if (g != null && stamm.Table.Columns.Contains("ID") && stamm["ID"] != DBNull.Value)
                    g.KonditionierungKatalogbau = Convert.ToInt64(stamm["ID"], CultureInfo.InvariantCulture);
            }
            if (g == null) return null;

            g.Z_AuswahlWohnflaeche = angabe;
            g.Einheit = einheit ?? "";
            g.Jahresnutzungsgrad = jahresnutzungsgrad;
            g.DezentralWarmwasser = dezentralWarmwasser;
            return g;
        }

        /// <summary>Die Textspalten unter den Bestandsspalten — <c>CopyFromStamm</c> macht aus NULL „".</summary>
        private static readonly HashSet<string> BESTAND_TEXT = new HashSet<string>(StringComparer.Ordinal)
        {
            "Gebaeudename", "Typ", "Beschreibung", "Baualtersklasse", "Gebaeudeart", "Wohngebaeude_Nicht_Wohngebaeude"
        };

        /// <summary>
        /// Der Katalogsatz als Zeile der Sicht <c>Abfrage_Projektgebaeude</c> — so, wie ihn
        /// <c>CopyFromStamm</c> in <c>Tab_Gebaeude</c> legen würde: <c>Bezeichner</c> wird
        /// <c>Gebaeudename</c>, die Bestandsspalten (<see cref="GebaeudeSchema.SICHT_GEBAEUDE"/>) tragen
        /// statt NULL 0 bzw. „", alle übrigen bleiben NULL-erhaltend; <c>ID</c> ist 0 (keine Kopie).
        /// </summary>
        private static DataRow Kopiezeile(DataRow stamm, int idProjekt)
        {
            var t = new DataTable();
            foreach (DataColumn c in stamm.Table.Columns)
            {
                string n = c.ColumnName == "Bezeichner" ? "Gebaeudename" : c.ColumnName;
                if (n == "ReadOnly" || t.Columns.Contains(n)) continue;
                t.Columns.Add(n, typeof(object));
            }
            foreach (string n in GebaeudeSchema.SICHT_ZUORDNUNG)
                if (!t.Columns.Contains(n)) t.Columns.Add(n, typeof(object));

            DataRow r = t.NewRow();
            foreach (DataColumn c in stamm.Table.Columns)
            {
                string n = c.ColumnName == "Bezeichner" ? "Gebaeudename" : c.ColumnName;
                if (t.Columns.Contains(n)) r[n] = stamm[c];
            }
            foreach (string n in GebaeudeSchema.SICHT_GEBAEUDE)
                if (n != "ID" && t.Columns.Contains(n) && r[n] == DBNull.Value)
                    r[n] = BESTAND_TEXT.Contains(n) ? (object)"" : 0.0;

            r["ID"] = 0;
            r["ID_Projekt"] = idProjekt;
            t.Rows.Add(r);
            return r;
        }

        /// <summary>
        /// Die Zeile in <c>Tab_Gebaeude</c>, die an dieser Zuordnung hängt.
        /// <c>Tab_Gebaeude.ID_ProjektGebaeude</c> IST der Verweis auf
        /// <c>Z_ProjektGebaeude.ID</c> (derselbe Weg wie
        /// <c>Z_ProjGebCtrl.LiesProjekt</c>); die Sicht <c>Abfrage_Projektgebaeude</c>
        /// gibt dagegen nur <c>Tab_Gebaeude.ID</c> aus, und genau die braucht der
        /// Vergleich — sie ist auch der Schlüssel, mit dem der Lauf die Tagesverteilung
        /// sucht.
        /// </summary>
        internal static int TabGebaeudeId(int idZ)
        {
            const string sql = "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?";

            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idZ));
            if (dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }
    }
}
