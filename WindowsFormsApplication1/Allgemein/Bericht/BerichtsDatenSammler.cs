using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lädt die Daten von Stamm + Varianten lesend in BerichtsDaten-DTOs
    /// (Konzept Kap. 8.2) — das aktive Projekt der App wird dabei NICHT umgeschaltet.
    /// Optional wird je Projekt vorab headless simuliert (SimulationRunner, frische
    /// Instanz je Projekt — Muster aus Form_Variantentest.btnSimulieren_Click).
    /// Fehler eines einzelnen Projekts brechen den Lauf nicht ab (VariantenDaten.Fehler).
    /// </summary>
    public class BerichtsDatenSammler
    {
        /// <summary>Fortschrittsmeldung für Dialog/Statuszeile.</summary>
        public class Fortschritt
        {
            public string Text = "";
            public int Aktuell;
            public int Gesamt;
        }

        /// <summary>Datenlage eines Projekts für die Dialog-Anzeige (Zeitstempel, ⚠).</summary>
        public class VariantenStatus
        {
            public int IdProjekt;
            public string Projektname = "";
            public string Variantenname = "";
            public bool IstStamm;
            public DateTime? SimStand;      // null = kein Ergebnis
            public bool Veraltet;           // SimStand < Aenderungsdatum des Projekts

            public string SimStandText
            {
                get
                {
                    if (!SimStand.HasValue) return "— (fehlt) ⚠";
                    string t = SimStand.Value.ToString("dd.MM.yy HH:mm");
                    return Veraltet ? t + " ⚠" : t;
                }
            }
        }

        // ------------------------------------------------------------- Status (leichtgewichtig)

        /// <summary>
        /// Ermittelt die Datenlage der Vergleichsgruppe ohne die Ergebnisbäume zu laden
        /// (nur Zeitstempel-Abfragen) — Grundlage der Dialogliste (Konzept Kap. 3.1).
        /// </summary>
        public static List<VariantenStatus> ErmittleStatus(int idStamm, string stammName)
        {
            var liste = new List<VariantenStatus>();
            var gruppe = new VariantenCtrl().LadeGruppe(idStamm, stammName);
            foreach (VariantenCtrl.VarianteInfo vi in gruppe)
            {
                var st = new VariantenStatus
                {
                    IdProjekt = vi.IdProjekt,
                    Projektname = vi.Projektname,
                    Variantenname = vi.Variantenname,
                    IstStamm = vi.IstStamm
                };
                st.SimStand = LiesSimZeitstempel(vi.IdProjekt);
                if (st.SimStand.HasValue)
                {
                    DateTime? aend = LiesAenderungsdatum(vi.IdProjekt);
                    st.Veraltet = aend.HasValue && st.SimStand.Value < aend.Value;
                }
                liste.Add(st);
            }
            return liste;
        }

        private static DateTime? LiesSimZeitstempel(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 Zeitstempel FROM " + ErgebnisCtrl.TAB_KOPF +
                    " WHERE ID_Projekt = ? ORDER BY ID DESC",
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToDateTime(o);
            }
            catch { }
            return null;
        }

        private static DateTime? LiesAenderungsdatum(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Aenderungsdatum FROM Tab_Projekt WHERE ID = ?",
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToDateTime(o);
            }
            catch { }
            return null;
        }

        // ------------------------------------------------------------- Sammeln

        /// <summary>
        /// Sammelt alle Berichtsdaten. variantenIds = gewählte Varianten (ohne Stamm).
        /// neuRechnen: alle Projekte vorab simulieren; fehlende Ergebnisse werden
        /// unabhängig davon immer gerechnet (Konzept Kap. 3.1/8.2).
        /// mitZeitreihen: für die Ganglinien wird je Projekt IMMER frisch in-memory
        /// simuliert und die Stundenreihen werden eingesammelt (Konzept Kap. 6.2) —
        /// Kennzahlen und Ganglinien stammen dann garantiert aus demselben Lauf.
        /// </summary>
        public BerichtsDaten Sammle(int idStamm, string stammName, List<int> variantenIds,
                                    bool neuRechnen, bool mitZeitreihen,
                                    IProgress<Fortschritt> fortschritt, CancellationToken abbruch)
        {
            _mitZeitreihen = mitZeitreihen;
            var daten = new BerichtsDaten { IdStamm = idStamm, Stammprojektname = stammName ?? "" };

            // Reihenfolge: Stamm zuerst, dann die gewählten Varianten in Gruppenreihenfolge.
            var gruppe = new VariantenCtrl().LadeGruppe(idStamm, stammName);
            var auswahl = new List<VariantenCtrl.VarianteInfo>();
            foreach (VariantenCtrl.VarianteInfo vi in gruppe)
                if (vi.IstStamm || (variantenIds != null && variantenIds.Contains(vi.IdProjekt)))
                    auswahl.Add(vi);

            int gesamt = auswahl.Count, aktuell = 0;
            foreach (VariantenCtrl.VarianteInfo vi in auswahl)
            {
                abbruch.ThrowIfCancellationRequested();
                aktuell++;
                Melde(fortschritt, aktuell, gesamt, (vi.IstStamm ? "Stamm" : "Variante") + ": " + vi.Projektname);

                var v = new VariantenDaten
                {
                    IdProjekt = vi.IdProjekt,
                    Projektname = vi.Projektname,
                    Variantenname = vi.Variantenname,
                    IstStamm = vi.IstStamm
                };
                daten.Varianten.Add(v);

                try
                {
                    SammleProjekt(v, neuRechnen, fortschritt, aktuell, gesamt, abbruch);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    v.Fehler = ex.Message;
                    daten.Warnungen.Add((vi.IstStamm ? "Stamm" : "Variante") + " '" + v.Anzeige +
                                        "' konnte nicht geladen werden: " + ex.Message);
                }
            }

            // Abweichungserkennung: jede Variante gegen den Stamm (Konzept Kap. 4.3).
            VariantenDaten stammDaten = daten.Varianten.Count > 0 && daten.Varianten[0].IstStamm
                ? daten.Varianten[0] : null;
            if (stammDaten != null && stammDaten.Details != null)
            {
                foreach (VariantenDaten v in daten.Varianten)
                {
                    if (v.IstStamm || v.Details == null) continue;
                    try { v.Abweichungen = AbweichungsErmittler.Vergleiche(stammDaten.Details, v.Details); }
                    catch { /* Abweichungstabelle bleibt dann leer */ }
                }
            }

            return daten;
        }

        private bool _mitZeitreihen;

        private void SammleProjekt(VariantenDaten v, bool neuRechnen,
                                   IProgress<Fortschritt> fortschritt, int aktuell, int gesamt,
                                   CancellationToken abbruch)
        {
            var ergCtrl = new ErgebnisCtrl();

            // 1. Datenlage feststellen.
            DateTime? stand = LiesSimZeitstempel(v.IdProjekt);
            DateTime? aend = LiesAenderungsdatum(v.IdProjekt);
            v.ErgebnisFehlte = !stand.HasValue;
            v.ErgebnisVeraltet = stand.HasValue && aend.HasValue && stand.Value < aend.Value;

            // 2. Simulieren, wenn gefordert, kein Ergebnis vorliegt oder Zeitreihen
            //    für Ganglinien gebraucht werden (die gibt es nur aus dem frischen Lauf).
            if (neuRechnen || v.ErgebnisFehlte || _mitZeitreihen)
            {
                abbruch.ThrowIfCancellationRequested();
                Melde(fortschritt, aktuell, gesamt, "Simuliere: " + v.Projektname);

                // Frische Instanz je Projekt (Muster btnSimulieren_Click) — die Instanz
                // bleibt hier zugreifbar, damit der ZeitreihenExtraktor die Stundenreihen
                // einsammeln kann, bevor sie verworfen wird.
                var runner = new SimulationRunner();
                string fehler;
                if (runner.Simuliere(v.IdProjekt, out fehler))
                {
                    ErgebnisModel frisch = SimulationRunner.BaueErgebnis(
                        v.IdProjekt, runner.simulation_Waermebedarf, runner.simulation_Strombedarf, runner.sim);
                    int erg = ergCtrl.Save(frisch);
                    if (erg > 0) v.FrischSimuliert = true;
                    if (_mitZeitreihen)
                        try { v.Zeitreihen = ZeitreihenExtraktor.AusLauf(runner); }
                        catch { v.Zeitreihen = null; }
                }
                else if (v.ErgebnisFehlte)
                    throw new InvalidOperationException("Simulation fehlgeschlagen: " + (fehler ?? "unbekannter Fehler"));
                // War ein (älteres) Ergebnis vorhanden, läuft der Bericht damit weiter —
                // der Zeitstempel weist den Stand aus; Ganglinien entfallen dann mit Hinweis.
            }

            // 3. Ergebnisbaum + Projektstammdaten laden.
            v.Ergebnis = ergCtrl.Load(v.IdProjekt);
            if (v.Ergebnis == null)
                throw new InvalidOperationException("Kein Simulationsergebnis vorhanden.");
            v.SimulationsStand = v.Ergebnis.Zeitstempel;

            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadSingle(v.IdProjekt);
            if (pc.rows > 0)
            {
                v.Projekt = new ProjektModel
                {
                    m_ID = pc.m_ID,
                    m_szProjektname = pc.m_szProjektname,
                    m_szBearbeiter = pc.m_szBearbeiter,
                    m_szBeschreibung = pc.m_szBeschreibung,
                    m_szKunde = pc.m_szKunde,
                    m_Aenderungsdatum = pc.m_Aenderungsdatum,
                    m_ID_Klimaregion = pc.m_ID_Klimaregion,
                    m_Erstelldatum = pc.m_Erstelldatum
                };
            }

            // 4. Brennstoffmengen (best effort — fehlendes Kostenmodul stoppt nichts).
            try { v.Brennstoffmengen = EnergieMengen.BaueBrennstoffmengen(v.IdProjekt); }
            catch { v.Brennstoffmengen = null; }

            // 5. Kosten-/Emissionsverrechnung (Phase 5), danach Kennzahlen aus dem
            //    Katalog (dessen Emissions-/Kosten-Zeilen lesen die Rechnerwerte).
            KostenEmissionRechner.Berechne(v);
            KennzahlenKatalog.Berechne(v);

            // 6. Detail-Daten (Gebäude, Anlage, Komponenten, Klimaregion) für
            //    Projektbeschreibung, Kenndaten-Tabellen und Abweichungserkennung.
            try { v.Details = ProjektDetails.Lade(v.IdProjekt); }
            catch { v.Details = null; }

            // 7. Zeitreihen für Ganglinien: Phase 3 (In-Memory-Lauf liefert die Reihen).
        }

        private static void Melde(IProgress<Fortschritt> p, int aktuell, int gesamt, string text)
        {
            if (p != null) p.Report(new Fortschritt { Aktuell = aktuell, Gesamt = gesamt, Text = text });
        }
    }
}
