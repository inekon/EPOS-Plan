using System;
using System.Collections.Generic;
using System.Data;
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

        /// <summary>Stunden mit eingeschalteter Sommerlüftung [h] — nur VDI-Weg.</summary>
        internal int? SommerlueftungsstundenH;

        /// <summary>Raumlufttemperatur je Stunde [°C]; <c>null</c> auf dem Tagesbilanz-Weg.</summary>
        internal double[] RaumtemperaturC;

        /// <summary>Operative Temperatur je Stunde [°C]; <c>null</c> auf dem Tagesbilanz-Weg.</summary>
        internal double[] OperativeTemperaturC;

        /// <summary>Heizsollwert je Stunde [°C] — untere Kante des Sollwertbands; <c>null</c> ohne VDI-Lauf.</summary>
        internal double[] HeizsollwertC;

        /// <summary>Die obere Raumtemperatur [°C] — obere Kante des Sollwertbands; <c>null</c> ohne VDI-Lauf.</summary>
        internal double? ObereRaumtemperaturC;

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

        /// <summary>Rechnet das PROJEKT Kälte (<c>Tab_Einstellungen.Kuehlbetrieb</c>)?</summary>
        internal bool KuehlbetriebProjekt;

        /// <summary>Trägt das Gebäude „Gebäude wird gekühlt" (<c>Kuehlung_Aktiv</c>)?</summary>
        internal bool KuehlungAktiv;

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

        /// <summary>Die Heizlast je Stunde [kW]; <c>null</c> für eine unbeheizte Zone.</summary>
        internal double[] HeizlastKw;

        /// <summary>Raumlufttemperatur je Stunde [°C].</summary>
        internal double[] RaumtemperaturC;

        /// <summary>Operative Temperatur je Stunde [°C].</summary>
        internal double[] OperativeTemperaturC;

        /// <summary>Heizsollwert je Stunde [°C]; <c>null</c> für eine unbeheizte Zone.</summary>
        internal double[] HeizsollwertC;
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
            var ergebnis = new GebaeudeBedarfErgebnis();
            if (idProjekt <= 0 || idKlimaregion <= 0 || idZ <= 0) return ergebnis;

            ProjektGebaeudeModel gebaeude = Projektgebaeude(idProjekt, idZ);
            if (gebaeude == null) return ergebnis;

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
            if (!sim.HeizwaermeEinesGebaeudes(gebaeude, 0, werte))
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
                ergebnis.KuehlenergieMwh = vdi.KuehlenergieMwh;
                ergebnis.KuehlstundenH = vdi.StundenMitKuehlbedarf;
                ergebnis.MittlereRaumtemperaturC = vdi.MittlereRaumtemperaturHeizzeit;
                ergebnis.UeberhitzungsstundenH = vdi.Ueberhitzungsstunden;
                ergebnis.SommerlueftungsstundenH = vdi.StundenMitSommerlueftung;
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
            ergebnis.KuehlbetriebProjekt = sim.KuehlbetriebProjekt;
            ergebnis.KuehlungAktiv = gebaeude.Kuehlung_Aktiv;
            ergebnis.KuehlleistungMaxKw = ergebnis.KuehlSollwertC.HasValue ? gebaeude.Kuehlleistung_Max : null;
            ergebnis.Erfolgreich = true;
            return ergebnis;
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
                RaumtemperaturC = r.Raumtemperatur,
                OperativeTemperaturC = r.OperativeTemperatur
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
