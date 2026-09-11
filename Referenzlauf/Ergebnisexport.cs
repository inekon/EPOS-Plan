using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Fuehrt fuer EIN Projekt den headless-Simulationslauf aus und friert das Ergebnis
    /// als CSV ein.
    ///
    /// Eingefroren werden zwei Dinge:
    ///  - aggregate.csv: alle Skalare der Tab_Ergebnis*-Zeilen des Laufs, dazu die
    ///    Restgroessen aus SimulationControl und die Jahressumme jedes Vektors,
    ///  - je Modul die Ganglinien als eigene CSV (8760 Stundenwerte bzw. 35040
    ///    Viertelstundenwerte).
    /// </summary>
    internal static class Ergebnisexport
    {
        /// <summary>Spalten, die sich von Lauf zu Lauf aendern und deshalb nicht verglichen werden.</summary>
        private static readonly HashSet<string> FluechtigeSpalten = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ID", "ID_Ergebnis", "ID_ErgebnisWaermepumpe", "ID_ErgebnisBHKW",
            "ID_ErgebnisHeizkessel", "ID_ErgebnisSolarthermie", "ID_ErgebnisPhotovoltaik",
            "Zeitstempel"
        };

        /// <summary>
        /// Rechnet das Projekt und schreibt alle CSVs nach <paramref name="zielOrdner"/>.
        /// Rueckgabe: Anzahl geschriebener Dateien, 0 bei Fehler.
        /// </summary>
        public static int ProjektAusfuehren(int idProjekt, string zielOrdner, Protokoll log)
        {
            Directory.CreateDirectory(zielOrdner);

            var runner = new SimulationRunner();
            string fehler;

            log.Zeile("Simulation startet fuer Projekt " + idProjekt + " ...");
            int kopfId = runner.SimuliereUndSpeichere(idProjekt, out fehler);
            if (kopfId <= 0)
            {
                log.FehlerZeile("Projekt " + idProjekt + ": " + (fehler ?? "unbekannter Fehler"));
                return 0;
            }
            log.Zeile("Simulation beendet, Ergebnis-Kopf-ID " + kopfId + ".");

            var summen = new List<KeyValuePair<string, double>>();
            int dateien = 0;

            SimulationControl sim = runner.sim;
            SimulationWaermebedarf wb = runner.simulation_Waermebedarf;
            SimulationStrombedarf sb = runner.simulation_Strombedarf;

            // --- Bedarf und Restgroessen (immer vorhanden) -------------------------------
            dateien += Vektor(zielOrdner, "waermebedarf.csv", wb.Waermebedarf, summen);
            dateien += Vektor(zielOrdner, "waermebedarf_gebaeude.csv", wb.Waermebedarf_Gebaeude, summen);
            dateien += Vektor(zielOrdner, "waermebedarf_brauchwasser.csv", wb.brauchwasserwerte, summen);
            dateien += Vektor(zielOrdner, "waermebedarf_prozess.csv", wb.prozesswerte, summen);
            dateien += Vektor(zielOrdner, "waermebedarf_extern.csv", wb.Waermebedarf_Extern, summen);
            dateien += Vektor(zielOrdner, "waermebedarf_dauerlinie.csv", wb.Dauerlinie, summen);
            dateien += Vektor(zielOrdner, "stundentemperatur.csv", wb.Stundentemperatur, summen);
            dateien += Vektor(zielOrdner, "restwaerme.csv", sim.Rest_Waermebedarf_stuendlich, summen);
            dateien += Vektor(zielOrdner, "strombedarf_viertelstunde.csv", sb.Strombedarf_viertelStundenwerte, summen);
            dateien += Vektor(zielOrdner, "reststrom_viertelstunde.csv", sim.Rest_Strombedarf_viertelstuendlich, summen);

            // --- Waermepumpe -------------------------------------------------------------
            if (sim.bSimulationWP && sim.simulation_wp != null)
            {
                SimulationWaermepumpe wp = sim.simulation_wp;
                dateien += Vektor(zielOrdner, "wp_waermebedarf.csv", wp.Waermebedarf_stuendlich, summen);
                dateien += Vektor(zielOrdner, "wp_produktion.csv", wp.WP_Waermeproduktion_stuendlich, summen);
                dateien += Vektor(zielOrdner, "wp_strom.csv", wp.WP_Strombedarf_stuendlich, summen);
                dateien += Vektor(zielOrdner, "heizstab.csv", wp.Heizstab_stuendlich, summen);
                dateien += Vektor(zielOrdner, "wp_restwaerme.csv", wp.waermerestbedarf_stuendlich, summen);
                dateien += Vektor(zielOrdner, "wp_quellentemperatur.csv", wp.Temperatur, summen);
                dateien += Vektor(zielOrdner, "wp_warmwasserbedarf.csv", wp.Warmwasserbedarf_stuendlich, summen);
            }

            // --- Pufferspeicher der Waermepumpe (nur bei Zuordnung in Z_ProjektPufferSp) --
            if (sim.puffer_wp != null)
            {
                dateien += Vektor(zielOrdner, "puffer_soc.csv", sim.puffer_wp.SOC_stuendlich, summen);
                dateien += Vektor(zielOrdner, "puffer_ladung.csv", sim.puffer_wp.Ladung_stuendlich, summen);
                dateien += Vektor(zielOrdner, "puffer_entladung.csv", sim.puffer_wp.Entladung_stuendlich, summen);
            }

            // --- Quellspeicher der WP-Module (Paket 7): eigene Ganglinien je Speicher.
            //     Die Dateinamen tragen die Anlagen-ID, damit sie stabil bleiben.
            {
                int q = 0;
                foreach (SimulationPufferspeicher sp in sim.AlleSpeicher())
                {
                    if (sp == null || sp.Verwendung != SimulationPufferspeicher.VERWENDUNG_QUELLE) continue;
                    string kennung = (sp.ID_Anlage > 0) ? sp.ID_Anlage.ToString(CultureInfo.InvariantCulture)
                                                        : q.ToString(CultureInfo.InvariantCulture);
                    dateien += Vektor(zielOrdner, "quellspeicher_" + kennung + "_soc.csv", sp.SOC_stuendlich, summen);
                    dateien += Vektor(zielOrdner, "quellspeicher_" + kennung + "_ladung.csv", sp.Ladung_stuendlich, summen);
                    dateien += Vektor(zielOrdner, "quellspeicher_" + kennung + "_entladung.csv", sp.Entladung_stuendlich, summen);
                    q++;
                }
            }

            // --- Heizkessel / Spitzenkessel ----------------------------------------------
            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                SimulationSPK spk = sim.simulation_spk;
                dateien += Vektor(zielOrdner, "kessel_waermebedarf.csv", spk.Waermebedarf, summen);
                dateien += Vektor(zielOrdner, "kessel_leistung.csv", spk.Kesselleistung_stuendlich, summen);
                dateien += Vektor(zielOrdner, "kessel_restwaerme.csv", spk.Restwaerme, summen);
                dateien += Vektor(zielOrdner, "kessel_strom.csv", spk.Stromverbrauch_stuendlich, summen);
            }

            // --- BHKW ---------------------------------------------------------------------
            if (sim.bSimulationBHKW && sim.simulation_bhkw != null)
            {
                SimulationBHKW bh = sim.simulation_bhkw;
                dateien += Vektor(zielOrdner, "bhkw_waermebedarf.csv", bh.waermebedarf, summen);
                dateien += Vektor(zielOrdner, "bhkw_waerme.csv", bh.waermeproduktion, summen);
                dateien += Vektor(zielOrdner, "bhkw_strom.csv", bh.stromproduktion, summen);
                dateien += Vektor(zielOrdner, "bhkw_restwaerme.csv", bh.waermerestbedarf, summen);
            }

            // --- Solarthermie -------------------------------------------------------------
            if (sim.bSimulationSolarthermie && sim.simulation_solarthermie != null)
            {
                SimulationSolarthermie st = sim.simulation_solarthermie;
                dateien += Vektor(zielOrdner, "solar_waermebedarf.csv", st.Waermebedarf, summen);
                dateien += Vektor(zielOrdner, "solar_produktion.csv", st.Waermeproduktion, summen);
                dateien += Vektor(zielOrdner, "solar_restwaerme.csv", st.Restwaerme, summen);
                dateien += Vektor(zielOrdner, "solar_ueberschuss.csv", st.Ueberschuss, summen);
            }

            // --- Photovoltaik --------------------------------------------------------------
            if (sim.bSimulationPV && sim.simulation_pv != null)
            {
                SimulationPV pv = sim.simulation_pv;
                dateien += Vektor(zielOrdner, "pv_produktion.csv", pv.Stromproduktion, summen);
                dateien += Vektor(zielOrdner, "pv_produktion_theoretisch.csv", pv.Stromproduktion_Theoretisch, summen);
                dateien += Vektor(zielOrdner, "pv_ueberschuss.csv", pv.Ueberschuss, summen);
                dateien += Vektor(zielOrdner, "pv_reststrom.csv", pv.Reststrom, summen);
                dateien += Vektor(zielOrdner, "pv_strombedarf.csv", pv.Strombedarf_stuendlich, summen);
            }

            // --- Stromspeicher --------------------------------------------------------------
            //
            // NACHZUG (Paket BHKW-Regulär, Nebenbefund): Dieses Werkzeug liegt NICHT in der
            // .sln und ist beim Stromspeicher-Paket AP2b nicht mitgezogen worden. Es griff
            // bis hierher auf zwei Größen zu, die AP2b abgelöst hat:
            //
            //   pv.Speicherfuellstand      -> SimulationControl.Speicherfuellstand_stuendlich
            //   sim.simulation_ssp.Strom…  -> SimulationControl.Speicherfuellstand_viertelstuendlich
            //                                 (der SimulationSSP-Stub ist durch die
            //                                  SpeicherEngine ersetzt)
            //
            // Ohne diesen Nachzug ließ sich das Werkzeug nicht mehr übersetzen und damit
            // KEIN Referenzlauf mehr fahren. Der Eingriff bleibt auf die Umlenkung der
            // Spaltenquellen beschränkt; die Dateinamen behalten ihre bisherigen Namen,
            // damit der Vergleich mit älteren Referenzständen möglich bleibt.
            if (sim.bSimulationSSP)
            {
                dateien += Vektor(zielOrdner, "ssp_gespeichert_viertelstunde.csv",
                                  sim.Speicherfuellstand_viertelstuendlich, summen);
                dateien += Vektor(zielOrdner, "pv_speicherfuellstand.csv",
                                  sim.Speicherfuellstand_stuendlich, summen);
            }

            // --- Speicherflotte: Ganglinien je Einheit ------------------------------------
            //
            // ANWENDERENTSCHEID SP-O-8 (11.09.2026). Der Flottenpfad des gewoehnlichen
            // Projektlaufs (SimulationControl.Stromspeicher.cs -> SpeicherFlottenProjektCtrl)
            // hatte bis hierher KEIN Regressionsnetz: Keines der zwoelf Referenzprojekte
            // aktivierte eine Flotte, und die zwei Reihen oben fuehren ohnehin nur den
            // SUMMEN-Fuellstand aus dem Kompatibilitaetsergebnis. Was der Flottenpfad
            // eigenstaendig rechnet - die Aufteilung auf die Einheiten (Verteilung,
            // SoC-Grenzen, Richtungsleistungen, Reserve) - stand in keiner Datei.
            //
            // JE EINHEIT ZWEI REIHEN, nach dem Muster der Quellspeicher weiter oben:
            // der Energiestand am INTERVALLENDE [kWh] und die ausgefuehrte Leistung [kW]
            // mit der Vorzeichenregel der Engine (positiv entladen, negativ laden). Die
            // Dateinamen tragen den Rang der Einheit in der Konfiguration - bei der
            // Verteilung "Kaskade" ist genau das die Reihenfolge, in der ausgelastet
            // wird, und eine vertauschte Reihenfolge faellt damit als Dateiunterschied
            // auf und nicht erst in einer Summe.
            //
            // Projekte ohne aktivierte Flotte erzeugen KEINE dieser Dateien - dieselbe
            // Bedingung wie beim Erdreich- und beim Emissionsblock.
            if (sim.Speicherflottenergebnis != null && sim.Speicherflottenkonfiguration != null)
            {
                var intervalle = sim.Speicherflottenergebnis.Variante.Intervalle;
                int einheiten = sim.Speicherflottenkonfiguration.Einheiten.Count;
                for (int e = 0; e < einheiten; e++)
                {
                    var energie = new double[intervalle.Count];
                    var leistung = new double[intervalle.Count];
                    for (int t = 0; t < intervalle.Count; t++)
                    {
                        var x = intervalle[t];
                        energie[t] = e < x.EnergieEndeKWhJeSpeicher.Count ? x.EnergieEndeKWhJeSpeicher[e] : 0.0;
                        leistung[t] = e < x.IstleistungKwJeSpeicher.Count ? x.IstleistungKwJeSpeicher[e] : 0.0;
                    }
                    string p = "flotte_einheit_" + e.ToString(CultureInfo.InvariantCulture) + "_";
                    dateien += Vektor(zielOrdner, p + "energie.csv", energie, summen);
                    dateien += Vektor(zielOrdner, p + "leistung.csv", leistung, summen);
                }
            }

            // --- Skalare -----------------------------------------------------------------
            var skalare = new List<KeyValuePair<string, string>>();
            skalare.Add(Neu("Lauf.ID_Projekt", idProjekt.ToString(CultureInfo.InvariantCulture)));
            // ANWENDERENTSCHEID W8-O-5c / Q7 (07.09.2026): Die zwei SCHLUESSEL bleiben
            // hart verdrahtet, obwohl die Felder mit S1.3 ihren Einheitennamen bekommen
            // haben (RestwaermeMwh / ReststromMwh). Der Schluessel steht in den 312 CSV
            // der eingefrorenen Basis 2026-09-06_R3_Straenge; wuerde er mitwandern,
            // waere kein Vergleich gegen eine aeltere Basis mehr moeglich. Die Einheit
            // ist unveraendert MWh - benannt wurde das Feld, nicht die Spalte.
            skalare.Add(Neu("Sim.Restwaerme", Zahl(sim.RestwaermeMwh)));
            skalare.Add(Neu("Sim.Reststrom", Zahl(sim.ReststromMwh)));
            skalare.Add(Neu("Sim.bSimulationWP", sim.bSimulationWP.ToString()));
            skalare.Add(Neu("Sim.bSimulationKessel", sim.bSimulationKessel.ToString()));
            skalare.Add(Neu("Sim.bSimulationSolarthermie", sim.bSimulationSolarthermie.ToString()));
            skalare.Add(Neu("Sim.bSimulationBHKW", sim.bSimulationBHKW.ToString()));
            skalare.Add(Neu("Sim.bSimulationPV", sim.bSimulationPV.ToString()));
            skalare.Add(Neu("Sim.bSimulationSSP", sim.bSimulationSSP.ToString()));
            skalare.Add(Neu("Sim.PufferWP_vorhanden", (sim.puffer_wp != null).ToString()));
            if (sim.puffer_wp != null)
            {
                skalare.Add(Neu("Puffer.Q_max", Zahl(sim.puffer_wp.Q_max)));
                skalare.Add(Neu("Puffer.Ladung_gesamt", Zahl(sim.puffer_wp.Ladung_gesamt)));
                skalare.Add(Neu("Puffer.Entladung_gesamt", Zahl(sim.puffer_wp.Entladung_gesamt)));
                skalare.Add(Neu("Puffer.Verluste_gesamt", Zahl(sim.puffer_wp.Verluste_gesamt)));
                skalare.Add(Neu("Puffer.SOC_Mittel", Zahl(sim.puffer_wp.SOC_Mittel)));
                skalare.Add(Neu("Puffer.SOC_Max", Zahl(sim.puffer_wp.SOC_Max)));
                skalare.Add(Neu("Puffer.Vollzyklen", Zahl(sim.puffer_wp.Vollzyklen)));
            }
            skalare.Add(Neu("Sim.Speicher_Anzahl",
                sim.AlleSpeicher().Count.ToString(CultureInfo.InvariantCulture)));

            // --- Erdreich-Auslegungspruefung (Paket 7) -----------------------------------
            // Die Werte werden bewusst nicht persistiert (Protokoll 6.5), waren damit
            // aber auch nicht regressionsfaehig: eine Aenderung an Entzugsarbeit, Spitze
            // oder Volllaststunden waere unbemerkt durchgegangen. Sie stehen deshalb als
            // Skalare in aggregate.csv. Projekte ohne WQ_Typ = 'Erdreich' erzeugen keinen
            // einzigen Eintrag - die Referenzmenge bleibt unberuehrt.
            {
                var erd = ErdreichAuswertung.FuerProjekt(idProjekt);
                // Kein Erdreich => KEIN Eintrag. Ein "Erdreich.Anzahl = 0" waere in jeder
                // aggregate.csv aufgetaucht und haette die eingefrorene Abweichungsliste
                // gegenueber B0 um acht Eintraege verlaengert, ohne etwas auszusagen.
                if (erd.Count > 0)
                    skalare.Add(Neu("Erdreich.Anzahl", erd.Count.ToString(CultureInfo.InvariantCulture)));
                for (int i = 0; i < erd.Count; i++)
                {
                    string p = "Erdreich[" + i + "].";
                    var a = erd[i];
                    skalare.Add(Neu(p + "ID_Anlage", a.ID_Anlage.ToString(CultureInfo.InvariantCulture)));
                    skalare.Add(Neu(p + "Modul", a.Modul));
                    skalare.Add(Neu(p + "Unwirksam", a.Unwirksam.ToString()));
                    skalare.Add(Neu(p + "MaxEntzugBelastbar", a.MaxEntzugBelastbar.ToString()));
                    skalare.Add(Neu(p + "MaxEntzugGeschaetzt", a.MaxEntzugGeschaetzt.ToString()));
                    skalare.Add(Neu(p + "InklSpeicherladung", a.InklSpeicherladung.ToString()));
                    skalare.Add(Neu(p + "JahresentzugKWh", Zahl(a.JahresentzugKWh)));
                    skalare.Add(Neu(p + "MaxEntzugW", Zahl(a.MaxEntzugW)));
                    skalare.Add(Neu(p + "VolllastStunden", Zahl(a.VolllastStunden)));
                    skalare.Add(Neu(p + "BetriebsStunden", a.BetriebsStunden.ToString(CultureInfo.InvariantCulture)));
                    skalare.Add(Neu(p + "FrostStunden", a.FrostStunden.ToString(CultureInfo.InvariantCulture)));
                    skalare.Add(Neu(p + "FrostWarnung", a.FrostWarnung.ToString()));
                    skalare.Add(Neu(p + "Pruefung_Moeglich", a.Pruefung.Moeglich.ToString()));
                    skalare.Add(Neu(p + "Pruefung_Warnung", a.Pruefung.Warnung.ToString()));
                }
            }

            // --- Emissionsgroessen der Simulation (Anwenderentscheid Em-9.8, 07.09.2026) --
            // Kessel und BHKW fuehren je fuenf Jahressummen (Verbrauch [MWh] x Faktor
            // / 1000), gebildet ueber Emissionsquelle.Fuer - die Kette Projekt ->
            // Katalog -> Stamm -> Carrier -> Brennstoff (Konzept B1). Bis zu diesem
            // Entscheid las sie NICHTS ausser der Probe EmissionsquelleTests: keine
            // Ergebnistabelle, kein Bericht, keine Kachel, keine Referenz-CSV. Damit
            // haette das Regressionsnetz eine Aenderung an einem Emissionsfaktor nicht
            // bemerken koennen (Konzept, Paragraph 9 Punkt 8).
            //
            // WEG A des Konzepts (11.2.1): Skalare hier, KEINE Spalte in Tab_Ergebnis*.
            // Eine gespeicherte Emissionszahl beschriebe einen Zustand, der bei jedem
            // Bericht neu gerechnet wird; sie liefe auseinander, sobald jemand zwischen
            // Lauf und Druck den Modus oder einen Katalogwert aendert.
            //
            // DIE EINHEIT STEHT IM NAMEN (Em-9.8-Q3): CO2 fuehrt t/a, die vier uebrigen
            // kg/a - die zehn Felder haben also NICHT dieselbe Einheit. Ein Teiler hier
            // waere eine dritte Umrechnungsnaht neben den zwei erlaubten (Hausregel
            // Rechenkern, Punkt 4); der Export rechnet nicht um.
            //
            // EIGENES PRAEFIX "Em.": "Heizkessel." und "BHKW." stehen fuer "Spalte einer
            // Tab_Ergebnis*-Zeile" (SELECT *). Ein handgeschriebener Skalar darunter
            // verwischte die Herkunft.
            //
            // BEDINGUNG WIE BEI DEN VEKTOREN (11.2.2): Der Block laeuft nur, wenn die
            // Stufe gelaufen ist - dieselbe Bedingung, unter der schon
            // kessel_waermebedarf.csv und bhkw_strom.csv entstehen, und dasselbe Muster
            // wie beim Erdreich-Block. Die Projekte 1007 und 1008 fahren weder Kessel-
            // noch BHKW-Stufe und bekommen deshalb KEINEN der zehn Schluessel, statt
            // zehn Nullen zu tragen.
            //
            // CO ist mit Absicht dabei, obwohl es heute strukturell 0 ist (es gibt keine
            // Emissionsart "CO", Paragraph 9 Punkt 9 / Em-9.9): So AENDERT ein spaeterer
            // Traegerwert eine Zahl, statt einen Schluessel HINZUZUFUEGEN - eine
            // Wertaenderung meldet der Vergleich mit Zahlen, ein neuer Schluessel nur
            // als "nur im Vergleichslauf".
            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                SimulationSPK spk = sim.simulation_spk;
                skalare.Add(Neu("Em.Kessel.Co2T",    Zahl(spk.Em_CO2_SPK)));
                skalare.Add(Neu("Em.Kessel.So2Kg",   Zahl(spk.Em_SO2_SPK)));
                skalare.Add(Neu("Em.Kessel.NoxKg",   Zahl(spk.Em_NOX_SPK)));
                skalare.Add(Neu("Em.Kessel.CoKg",    Zahl(spk.Em_CO_SPK)));
                skalare.Add(Neu("Em.Kessel.StaubKg", Zahl(spk.Em_Staub_SPK)));
            }

            if (sim.bSimulationBHKW && sim.simulation_bhkw != null)
            {
                SimulationBHKW bh = sim.simulation_bhkw;
                skalare.Add(Neu("Em.Bhkw.Co2T",    Zahl(bh.Em_CO2_BHKW)));
                skalare.Add(Neu("Em.Bhkw.So2Kg",   Zahl(bh.Em_SO2_BHKW)));
                skalare.Add(Neu("Em.Bhkw.NoxKg",   Zahl(bh.Em_NOX_BHKW)));
                skalare.Add(Neu("Em.Bhkw.CoKg",    Zahl(bh.Em_CO_BHKW)));
                skalare.Add(Neu("Em.Bhkw.StaubKg", Zahl(bh.Em_Staub_BHKW)));
            }

            // --- Speicherflotte: Kennzahlen (Anwenderentscheid SP-O-8, 11.09.2026) --------
            //
            // WAS HIER FEHLTE. Laeuft die Flotte, ersetzt sie den Reststrombedarf des
            // Projekts (SimulationControl.Stromspeicher.cs) - aber alles, WORAUS dieser
            // Netzbezug entsteht, blieb unsichtbar: die getrennten Einspeisereihen nach
            // PV, BHKW und Batterie, die Abregelung, die Umwandlungsverluste, die
            // Bezugsspitze, die der Leistungspreis bewertet, und die Kennzahlen je
            // Einheit. Eine Aenderung an Verteilung, Reserve oder Wirkungsgrad haette
            // sich in der EINEN Jahressumme des Reststroms gegenseitig aufheben koennen.
            //
            // DIE EINHEIT STEHT IM NAMEN, wie bei den zehn Emissionsskalaren (Em-9.8-Q3):
            // Energien fuehren kWh, Leistungen kW, Zyklen und Miner-Schaden sind
            // dimensionslos. Der Export rechnet nicht um.
            //
            // EIGENES PRAEFIX "Flotte.": "Ergebnis.", "Photovoltaik." usw. stehen fuer
            // "Spalte einer Tab_Ergebnis*-Zeile" (SELECT *). Die Flotte hat keine
            // Ergebniszeile - ihr Laufstand lebt nur im Speicher des Laufs.
            //
            // MIT REFERENZ. "Flotte.Ref.*" ist derselbe Lauf OHNE jeden Speicher, den die
            // Engine zum Vergleich mitrechnet (FlottenStudienErgebnis.ReferenzOhneSpeicher).
            // Er kostet nichts, steht ohnehin da und macht aus der Wirkung der Flotte
            // eine Differenz statt einer nackten Zahl.
            //
            // BEDINGUNG WIE BEI DEN VEKTOREN: Der Block laeuft nur, wenn die Flotte
            // wirklich gerechnet hat. Die zwoelf Bestandsprojekte fahren die Einzelanlage
            // und bekommen KEINEN dieser 42 Schluessel, statt 42 Nullen zu tragen.
            if (sim.Speicherflottenergebnis != null && sim.Speicherflottenkonfiguration != null)
            {
                SpeicherEngine.FlottenStudienErgebnis studie = sim.Speicherflottenergebnis;
                SpeicherEngine.FlottenStudieKonfiguration flotte = sim.Speicherflottenkonfiguration;
                SpeicherEngine.FlottenSimulationErgebnis variante = studie.Variante;
                SpeicherEngine.FlottenSimulationErgebnis referenz = studie.ReferenzOhneSpeicher;
                SpeicherFlottenNetzbilanz bilanz = sim.Speicherflottennetzbilanz;

                skalare.Add(Neu("Flotte.EinheitenAnzahl",
                    flotte.Einheiten.Count.ToString(CultureInfo.InvariantCulture)));
                skalare.Add(Neu("Flotte.Betriebsziel", flotte.Optionen.Betriebsziel.ToString()));
                skalare.Add(Neu("Flotte.Verteilung", flotte.Optionen.Verteilung.ToString()));
                skalare.Add(Neu("Flotte.PeakZielKw",
                    flotte.Optionen.WirtschaftlicherPeakZielwertKw.HasValue
                        ? Zahl(flotte.Optionen.WirtschaftlicherPeakZielwertKw.Value) : ""));
                skalare.Add(Neu("Flotte.NetzladungErlaubt", flotte.Optionen.NetzladungErlaubt.ToString()));
                skalare.Add(Neu("Flotte.BatterieexportErlaubt", flotte.Optionen.BatterieexportErlaubt.ToString()));
                skalare.Add(Neu("Flotte.Zulaessig", variante.Zulaessig.ToString()));
                skalare.Add(Neu("Flotte.PlanFallbackIntervalle",
                    variante.PlanFallbackIntervalle.ToString(CultureInfo.InvariantCulture)));

                skalare.Add(Neu("Flotte.NetzbezugKwh", Zahl(variante.NetzbezugKWh)));
                skalare.Add(Neu("Flotte.BezugsspitzeKw", Zahl(variante.MaximalerNetzbezugKw)));
                skalare.Add(Neu("Flotte.NetzeinspeisungKwh", Zahl(variante.NetzeinspeisungKWh)));
                skalare.Add(Neu("Flotte.PvAbregelungKwh", Zahl(variante.PvAbregelungKWh)));
                skalare.Add(Neu("Flotte.VerlusteKwh", Zahl(variante.VerlusteKWh)));

                // Die drei Einspeisequellen stehen nur in der Projekt-Netzbilanz - sie
                // ist die Stelle, die PV, BHKW und Batterie ohne Doppelzaehlung trennt.
                if (bilanz != null)
                {
                    skalare.Add(Neu("Flotte.PvEinspeisungKwh", Zahl(bilanz.PvNetzeinspeisungKwh)));
                    skalare.Add(Neu("Flotte.BhkwEinspeisungKwh", Zahl(bilanz.BhkwNetzeinspeisungKwh)));
                    skalare.Add(Neu("Flotte.BatterieEinspeisungKwh", Zahl(bilanz.BatterieNetzeinspeisungKwh)));
                }

                skalare.Add(Neu("Flotte.Ref.NetzbezugKwh", Zahl(referenz.NetzbezugKWh)));
                skalare.Add(Neu("Flotte.Ref.BezugsspitzeKw", Zahl(referenz.MaximalerNetzbezugKw)));
                skalare.Add(Neu("Flotte.Ref.NetzeinspeisungKwh", Zahl(referenz.NetzeinspeisungKWh)));
                skalare.Add(Neu("Flotte.Ref.PvAbregelungKwh", Zahl(referenz.PvAbregelungKWh)));

                for (int e = 0; e < flotte.Einheiten.Count; e++)
                {
                    SpeicherEngine.FlottenEinheit einheit = flotte.Einheiten[e];
                    string p = "Flotte.Einheit[" + e.ToString(CultureInfo.InvariantCulture) + "].";
                    skalare.Add(Neu(p + "Id", einheit.Id));
                    skalare.Add(Neu(p + "KapazitaetKwh", Zahl(einheit.KapazitaetKWh)));
                    skalare.Add(Neu(p + "LadeleistungKw", Zahl(einheit.LadeleistungKw)));
                    skalare.Add(Neu(p + "EntladeleistungKw", Zahl(einheit.EntladeleistungKw)));

                    // Die Kennzahlen stehen in derselben Reihenfolge wie die Einheiten
                    // (FlottenSimulationErgebnis.SpeicherKennzahlen); beim Referenzlauf
                    // ohne Speicher ist die Liste leer.
                    if (e >= variante.SpeicherKennzahlen.Count) continue;
                    SpeicherEngine.FlottenSpeicherKennzahlen k = variante.SpeicherKennzahlen[e];
                    skalare.Add(Neu(p + "AnfangsenergieKwh", Zahl(k.AnfangsenergieKWh)));
                    skalare.Add(Neu(p + "EndenergieKwh", Zahl(k.EndenergieKWh)));
                    skalare.Add(Neu(p + "LadeenergieKwh", Zahl(k.LadeenergieAcKWh)));
                    skalare.Add(Neu(p + "EntladeenergieKwh", Zahl(k.EntladeenergieAcKWh)));
                    skalare.Add(Neu(p + "VollzyklenAeq", Zahl(k.AequivalenteVollzyklen)));
                    // Ohne Lebensdauerkurve zaehlt die Rainflow-Auswertung die Zyklen und
                    // laesst den Schaden bei 0 - der Schluessel steht trotzdem, damit eine
                    // spaeter hinterlegte Kurve eine ZAHL aendert und keinen SCHLUESSEL
                    // hinzufuegt (dasselbe Muster wie beim strukturell leeren Em.*.CoKg).
                    skalare.Add(Neu(p + "MinerSchaden", Zahl(k.RainflowSchaden)));
                    skalare.Add(Neu(p + "RainflowZyklen",
                        k.RainflowZyklen.Count.ToString(CultureInfo.InvariantCulture)));
                }
            }

            skalare.AddRange(ErgebnisTabellenLesen(kopfId));

            foreach (var s in summen)
                skalare.Add(Neu("Vektor." + s.Key + ".Summe", Zahl(s.Value)));

            SkalareSchreiben(Path.Combine(zielOrdner, "aggregate.csv"), skalare);
            dateien++;

            log.Zeile("Projekt " + idProjekt + ": " + dateien + " CSV-Dateien, " +
                      skalare.Count + " Skalare.");
            return dateien;
        }

        // ---------------------------------------------------------------------------------
        // Ergebnistabellen
        // ---------------------------------------------------------------------------------

        /// <summary>Liest alle Tab_Ergebnis*-Zeilen des Laufs als flache Namen/Wert-Liste.</summary>
        private static List<KeyValuePair<string, string>> ErgebnisTabellenLesen(int kopfId)
        {
            var werte = new List<KeyValuePair<string, string>>();

            ZeileUebernehmen(werte, "Ergebnis",
                "SELECT * FROM Tab_Ergebnis WHERE ID = " + kopfId);

            ZeileUebernehmen(werte, "Energiebedarf",
                "SELECT * FROM Tab_ErgebnisEnergiebedarf WHERE ID_Ergebnis = " + kopfId);

            DetailMitModulen(werte, kopfId,
                "Waermepumpe", "Tab_ErgebnisWaermepumpe",
                "Tab_ErgebnisWaermepumpeModul", "ID_ErgebnisWaermepumpe");

            DetailMitModulen(werte, kopfId,
                "BHKW", "Tab_ErgebnisBHKW",
                "Tab_ErgebnisBHKWModul", "ID_ErgebnisBHKW");

            DetailMitModulen(werte, kopfId,
                "Heizkessel", "Tab_ErgebnisHeizkessel",
                "Tab_ErgebnisHeizkesselModul", "ID_ErgebnisHeizkessel");

            DetailMitModulen(werte, kopfId,
                "Solarthermie", "Tab_ErgebnisSolarthermie",
                "Tab_ErgebnisSolarthermieModul", "ID_ErgebnisSolarthermie");

            DetailMitModulen(werte, kopfId,
                "Photovoltaik", "Tab_ErgebnisPhotovoltaik",
                "Tab_ErgebnisPhotovoltaikModul", "ID_ErgebnisPhotovoltaik");

            // Pufferspeicher-Zeilen des Laufs (Paket 7, Konzept 6.6). Auf einer noch
            // nicht migrierten Datenbank existiert die Tabelle nicht - dann bleibt der
            // Block leer, statt den Lauf abzubrechen.
            //
            // Ueber den stillen Direktzugriff aus ErgebnisCtrl: DataRepository.GetDataTable
            // wirft bei fehlender Tabelle NICHT, sondern zeigt eine MessageBox und liefert
            // eine leere Tabelle - im headless-Lauf haette der Dialogwaechter sie
            // wegdruecken und als Engine-Rueckfrage protokollieren muessen.
            DataTable puffer = ErgebnisCtrl.PufferZeilenLesenStill(kopfId);
            if (puffer != null)
                for (int i = 0; i < puffer.Rows.Count; i++)
                    SpaltenUebernehmen(werte, "Pufferspeicher[" + i + "]", puffer, puffer.Rows[i]);

            return werte;
        }

        private static void DetailMitModulen(List<KeyValuePair<string, string>> werte, int kopfId,
                                             string praefix, string tabelle,
                                             string modulTabelle, string modulFk)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + tabelle + " WHERE ID_Ergebnis = " + kopfId);
            if (dt == null || dt.Rows.Count == 0) return;

            DataRow zeile = dt.Rows[0];
            SpaltenUebernehmen(werte, praefix, dt, zeile);

            int detailId = Convert.ToInt32(zeile["ID"], CultureInfo.InvariantCulture);
            DataTable module = DataRepository.GetDataTable(
                "SELECT * FROM " + modulTabelle + " WHERE " + modulFk + " = " + detailId + " ORDER BY ID");
            if (module == null) return;

            for (int i = 0; i < module.Rows.Count; i++)
                SpaltenUebernehmen(werte, praefix + "Modul[" + i + "]", module, module.Rows[i]);
        }

        private static void ZeileUebernehmen(List<KeyValuePair<string, string>> werte,
                                             string praefix, string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            if (dt == null || dt.Rows.Count == 0) return;
            SpaltenUebernehmen(werte, praefix, dt, dt.Rows[0]);
        }

        private static void SpaltenUebernehmen(List<KeyValuePair<string, string>> werte,
                                               string praefix, DataTable dt, DataRow zeile)
        {
            foreach (DataColumn c in dt.Columns)
            {
                if (FluechtigeSpalten.Contains(c.ColumnName)) continue;
                werte.Add(Neu(praefix + "." + c.ColumnName, DbWert(zeile[c])));
            }
        }

        private static string DbWert(object v)
        {
            if (v == null || v == DBNull.Value) return "";
            if (v is bool) return ((bool)v).ToString();
            if (v is DateTime) return "";     // Zeitstempel sind fluechtig
            if (v is float) return Zahl((float)v);
            if (v is double) return Zahl((double)v);
            if (v is decimal) return Zahl((double)(decimal)v);
            if (v is byte || v is short || v is int || v is long)
                return Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            // #202: Convert.ToString ist als string? deklariert; v ist oben bereits
            // gegen null und DBNull geprueft, der Rueckfall aendert also keinen Wert -
            // er nimmt nur das CS8602 unter Nullable=enable (EPOS.iOS verlinkt diese
            // Datei) weg.
            string text = Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
            return text.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        }

        // ---------------------------------------------------------------------------------
        // CSV-Ausgabe
        // ---------------------------------------------------------------------------------

        private static KeyValuePair<string, string> Neu(string k, string v)
        {
            return new KeyValuePair<string, string>(k, v);
        }

        /// <summary>Kultur-invariante Zahl. G9 liegt weit unter der Vergleichstoleranz von 1e-4.</summary>
        public static string Zahl(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "Inf";
            if (double.IsNegativeInfinity(d)) return "-Inf";
            return d.ToString("G9", CultureInfo.InvariantCulture);
        }

        private static void SkalareSchreiben(string datei, List<KeyValuePair<string, string>> werte)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Groesse;Wert");
            foreach (var w in werte)
                sb.AppendLine(w.Key + ";" + w.Value);
            File.WriteAllText(datei, sb.ToString(), new UTF8Encoding(true));
        }

        private static int Vektor(string ordner, string datei, double[] werte,
                                  List<KeyValuePair<string, double>> summen)
        {
            if (werte == null || werte.Length == 0) return 0;

            var sb = new StringBuilder(werte.Length * 12);
            sb.AppendLine("Index;Wert");
            double summe = 0;
            for (int i = 0; i < werte.Length; i++)
            {
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                sb.Append(';');
                sb.AppendLine(Zahl(werte[i]));
                if (!double.IsNaN(werte[i]) && !double.IsInfinity(werte[i])) summe += werte[i];
            }
            File.WriteAllText(Path.Combine(ordner, datei), sb.ToString(), new UTF8Encoding(true));

            summen.Add(new KeyValuePair<string, double>(Path.GetFileNameWithoutExtension(datei), summe));
            return 1;
        }
    }
}
