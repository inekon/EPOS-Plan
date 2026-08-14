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
                dateien += Vektor(zielOrdner, "pv_speicherfuellstand.csv", pv.Speicherfuellstand, summen);
                dateien += Vektor(zielOrdner, "pv_strombedarf.csv", pv.Strombedarf_stuendlich, summen);
            }

            // --- Stromspeicher --------------------------------------------------------------
            if (sim.bSimulationSSP && sim.simulation_ssp != null)
            {
                dateien += Vektor(zielOrdner, "ssp_gespeichert_viertelstunde.csv",
                                  sim.simulation_ssp.Stromgespeichert, summen);
            }

            // --- Skalare -----------------------------------------------------------------
            var skalare = new List<KeyValuePair<string, string>>();
            skalare.Add(Neu("Lauf.ID_Projekt", idProjekt.ToString(CultureInfo.InvariantCulture)));
            skalare.Add(Neu("Sim.Restwaerme", Zahl(sim.Restwaerme)));
            skalare.Add(Neu("Sim.Reststrom", Zahl(sim.Reststrom)));
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
            return Convert.ToString(v, CultureInfo.InvariantCulture)
                          .Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
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

        private static int Vektor(string ordner, string datei, float[] werte,
                                  List<KeyValuePair<string, double>> summen)
        {
            if (werte == null || werte.Length == 0) return 0;
            var d = new double[werte.Length];
            for (int i = 0; i < werte.Length; i++) d[i] = werte[i];
            return Vektor(ordner, datei, d, summen);
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
