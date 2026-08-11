using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Persistenz der Simulationsergebnisse.
    // Kopf (Tab_Ergebnis) + je Simulationsart eine Detailtabelle:
    //   Tab_ErgebnisEnergiebedarf, Tab_ErgebnisWaermepumpe (+ ...Modul).
    // Speicherstrategie: "letztes Ergebnis je Projekt" -> vor dem Schreiben werden
    // die vorhandenen Ergebnisse des Projekts geloescht; die Detailzeilen fallen per
    // Loeschweitergabe automatisch mit weg. Erweiterung auf Historie = dieses
    // Vorab-Loeschen weglassen; das Schema bleibt identisch.
    // IDs werden explizit vergeben (MAX+1), passend zum uebrigen Projektmuster.
    // ---------------------------------------------------------------------------
    public class ErgebnisCtrl
    {
        public const string TAB_KOPF = "Tab_Ergebnis";
        public const string TAB_ENERGIE = "Tab_ErgebnisEnergiebedarf";
        public const string TAB_WP = "Tab_ErgebnisWaermepumpe";
        public const string TAB_WP_MODUL = "Tab_ErgebnisWaermepumpeModul";
        public const string TAB_BHKW = "Tab_ErgebnisBHKW";
        public const string TAB_BHKW_MODUL = "Tab_ErgebnisBHKWModul";
        public const string TAB_KESSEL = "Tab_ErgebnisHeizkessel";
        public const string TAB_KESSEL_MODUL = "Tab_ErgebnisHeizkesselModul";
        public const string TAB_SOLAR = "Tab_ErgebnisSolarthermie";
        public const string TAB_SOLAR_MODUL = "Tab_ErgebnisSolarthermieModul";
        public const string TAB_PV = "Tab_ErgebnisPhotovoltaik";
        public const string TAB_PV_MODUL = "Tab_ErgebnisPhotovoltaikModul";

        // Alte, funktionslose Signatur — nur für Übergangskompatibilität erhalten.
        [Obsolete("Delete(int idProjekt) verwenden — diese Überladung löscht nichts.")]
        public int Delete() { return -1; }

        // Loescht die gespeicherten Ergebnisse eines Projekts (Befund B2 behoben:
        // Projekt-Parameter, Parameterbindung und Commit ergänzt).
        public int Delete(int idProjekt)
        {
            if (idProjekt <= 0) return -1;
            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                //    Loeschweitergabe raeumt alle Detailtabellen automatisch mit ab.
                using (OleDbCommand c = new OleDbCommand(
                    "DELETE FROM " + TAB_KOPF + " WHERE ID_Projekt = ?", conn, trans))
                {
                    c.Parameters.Add("@p", OleDbType.Integer).Value = idProjekt;
                    c.ExecuteNonQuery();
                }
                trans.Commit();
                return 0;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                MessageBox.Show("Fehler beim Löschen des Simulationsergebnisses: " + ex.Message);
                return -1;
            }
            finally { try { conn.Close(); } catch { } }
        }

        // Speichert ein Ergebnis (loescht zuvor vorhandene des Projekts).
        // Rueckgabe: neue Kopf-ID, -1 bei Fehler.
        public int Save(ErgebnisModel m)
        {
            if (m == null || m.ID_Projekt <= 0) return -1;

            StelleEnergieSpaltenSicher();   // fehlende Restbedarf-Spalten einmalig ergänzen
            StelleBHKWSpaltenSicher();      // fehlende Brennstoffspalten in Tab_ErgebnisBHKW einmalig ergänzen
            StelleModulSpaltenSicher();     // carrier_id (B1) + Waermeproduktion Kesselmodul (B3) ergänzen

            // Energieträger-Zuordnung einmal je Erzeuger bestimmen (Befund B1: echte
            // carrier_id statt des Brennstoff-Strings — Basis für Kosten/Wirtschaftlichkeit).
            int bhkwCarrier = CarrierIdFuerProjekt(m.ID_Projekt, "Tab_BHKW");
            int kesselCarrier = CarrierIdFuerProjekt(m.ID_Projekt, "Tab_Heizkessel");

            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                // 1. "letztes Ergebnis je Projekt": vorhandene Ergebnisse entfernen.
                //    Loeschweitergabe raeumt alle Detailtabellen automatisch mit ab.
                using (OleDbCommand c = new OleDbCommand(
                    "DELETE ID_Projekt FROM " + TAB_KOPF + " WHERE ID_Projekt = ?", conn, trans))
                {
                    c.Parameters.Add("@p", OleDbType.Integer).Value = m.ID_Projekt;
                    c.ExecuteNonQuery();
                }

                // 2. Kopf schreiben.
                int kopfId = NextId(conn, trans, TAB_KOPF);
                string sqlKopf = "INSERT INTO " + TAB_KOPF + " (" +
                    "ID, ID_Projekt, Bezeichner, Zeitstempel, ID_Klimaregion, " +
                    "Sim_Energiebedarf, Sim_Waermepumpe, Sim_Heizkessel, Sim_Solarthermie, Sim_BHKW, Sim_PV, Sim_Stromspeicher) " +
                    "VALUES (?,?,?,?,?, ?,?,?,?,?,?,?)";
                using (OleDbCommand c = new OleDbCommand(sqlKopf, conn, trans))
                {
                    c.Parameters.Add("@id", OleDbType.Integer).Value = kopfId;
                    c.Parameters.Add("@proj", OleDbType.Integer).Value = m.ID_Projekt;
                    c.Parameters.Add("@bez", OleDbType.VarWChar).Value = (object)(m.Bezeichner ?? "");
                    c.Parameters.Add("@zeit", OleDbType.Date).Value = m.Zeitstempel;
                    c.Parameters.Add("@klima", OleDbType.Integer).Value = m.ID_Klimaregion;
                    c.Parameters.Add("@s1", OleDbType.Boolean).Value = m.Sim_Energiebedarf;
                    c.Parameters.Add("@s2", OleDbType.Boolean).Value = m.Sim_Waermepumpe;
                    c.Parameters.Add("@s3", OleDbType.Boolean).Value = m.Sim_Heizkessel;
                    c.Parameters.Add("@s4", OleDbType.Boolean).Value = m.Sim_Solarthermie;
                    c.Parameters.Add("@s5", OleDbType.Boolean).Value = m.Sim_BHKW;
                    c.Parameters.Add("@s6", OleDbType.Boolean).Value = m.Sim_PV;
                    c.Parameters.Add("@s7", OleDbType.Boolean).Value = m.Sim_Stromspeicher;
                    c.ExecuteNonQuery();
                }

                // 3. Detail: Energiebedarf.
                if (m.Energiebedarf != null)
                {
                    int eId = NextId(conn, trans, TAB_ENERGIE);
                    string sql = "INSERT INTO " + TAB_ENERGIE + " (" +
                        "ID, ID_Ergebnis, Waermebedarf_Gesamt, Waermelast_Max, Strombedarf_Gesamt, Strombedarf_Max, " +
                        "Waermerestbedarf, Stromrestbedarf) " +
                        "VALUES (?,?,?,?,?,?,?,?)";
                    using (OleDbCommand c = new OleDbCommand(sql, conn, trans))
                    {
                        c.Parameters.Add("@id", OleDbType.Integer).Value = eId;
                        c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                        c.Parameters.Add("@a1", OleDbType.Double).Value = R(m.Energiebedarf.Waermebedarf_Gesamt);
                        c.Parameters.Add("@a2", OleDbType.Double).Value = R(m.Energiebedarf.Waermelast_Max);
                        c.Parameters.Add("@a3", OleDbType.Double).Value = R(m.Energiebedarf.Strombedarf_Gesamt);
                        c.Parameters.Add("@a4", OleDbType.Double).Value = R(m.Energiebedarf.Strombedarf_Max);
                        c.Parameters.Add("@a5", OleDbType.Double).Value = R(m.Energiebedarf.Waermerestbedarf);
                        c.Parameters.Add("@a6", OleDbType.Double).Value = R(m.Energiebedarf.Stromrestbedarf);
                        c.ExecuteNonQuery();
                    }
                }

                // 4. Detail: Waermepumpe (+ Modulliste).
                if (m.Waermepumpe != null)
                {
                    int wpId = NextId(conn, trans, TAB_WP);
                    string sql = "INSERT INTO " + TAB_WP + " (" +
                        "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Waermeproduktion_WP, Stromverbrauch_WP, " +
                        "Stromverbrauch_Heizstab, Kapazitaet_Pufferspeicher, Min_Spitzenkesselleistung, " +
                        "Waermebedarfsdeckung, Vollbenutzungsstunden, Bivalenzpunkt) " +
                        "VALUES (?,?,?,?,?,?, ?,?,?, ?,?,?)";
                    using (OleDbCommand c = new OleDbCommand(sql, conn, trans))
                    {
                        c.Parameters.Add("@id", OleDbType.Integer).Value = wpId;
                        c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                        c.Parameters.Add("@a1", OleDbType.Double).Value = R(m.Waermepumpe.Waermebedarf);
                        c.Parameters.Add("@a2", OleDbType.Double).Value = R(m.Waermepumpe.Restwaermebedarf);
                        c.Parameters.Add("@a3", OleDbType.Double).Value = R(m.Waermepumpe.Waermeproduktion_WP);
                        c.Parameters.Add("@a4", OleDbType.Double).Value = R(m.Waermepumpe.Stromverbrauch_WP);
                        c.Parameters.Add("@a5", OleDbType.Double).Value = R(m.Waermepumpe.Stromverbrauch_Heizstab);
                        c.Parameters.Add("@a6", OleDbType.Double).Value = R(m.Waermepumpe.Kapazitaet_Pufferspeicher);
                        c.Parameters.Add("@a7", OleDbType.Double).Value = R(m.Waermepumpe.Min_Spitzenkesselleistung);
                        c.Parameters.Add("@a8", OleDbType.Double).Value = R(m.Waermepumpe.Waermebedarfsdeckung);
                        c.Parameters.Add("@a9", OleDbType.Double).Value = R(m.Waermepumpe.Vollbenutzungsstunden);
                        c.Parameters.Add("@a10", OleDbType.Double).Value =
                            m.Waermepumpe.Bivalenzpunkt.HasValue ? (object)R(m.Waermepumpe.Bivalenzpunkt.Value) : DBNull.Value;
                        c.ExecuteNonQuery();
                    }

                    if (m.Waermepumpe.Module != null && m.Waermepumpe.Module.Count > 0)
                    {
                        int modId = NextId(conn, trans, TAB_WP_MODUL);
                        string sqlM = "INSERT INTO " + TAB_WP_MODUL + " (" +
                            "ID, ID_ErgebnisWaermepumpe, Modul, Leistung, Waermeproduktion, Stromverbrauch, Heizstab, Betriebsstunden) " +
                            "VALUES (?,?,?,?,?,?,?,?)";
                        foreach (ErgebnisWaermepumpeModulModel mo in m.Waermepumpe.Module)
                        {
                            using (OleDbCommand c = new OleDbCommand(sqlM, conn, trans))
                            {
                                c.Parameters.Add("@id", OleDbType.Integer).Value = modId++;
                                c.Parameters.Add("@wp", OleDbType.Integer).Value = wpId;
                                c.Parameters.Add("@mod", OleDbType.VarWChar).Value = (object)(mo.Modul ?? "");
                                c.Parameters.Add("@l", OleDbType.Double).Value = R(mo.Leistung);
                                c.Parameters.Add("@w", OleDbType.Double).Value = R(mo.Waermeproduktion);
                                c.Parameters.Add("@s", OleDbType.Double).Value = R(mo.Stromverbrauch);
                                c.Parameters.Add("@h", OleDbType.Double).Value = R(mo.Heizstab);
                                c.Parameters.Add("@b", OleDbType.Double).Value = R(mo.Betriebsstunden);
                                c.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // 5. Detail: BHKW (+ Modulliste).
                if (m.BHKW != null)
                {
                    int bId = NextId(conn, trans, TAB_BHKW);
                    string sql = "INSERT INTO " + TAB_BHKW + " (" +
                        "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Strombedarf, Reststrombedarf, " +
                        "Waermeproduktion, Waermeueberschuss, Stromproduktion, Betriebsstunden_Gesamt, " +
                        "Betriebsstunden_Durchschnitt, Waermebedarfsdeckung, Strombedarfsdeckung, " +
                        "Gasverbrauch, Oelverbrauch, Koks, Rapsoelverbrauch, Holzverbrauch, Kohle, " +
                        "Sonstigverbrauch, Pellets, TierischeFette) " +
                        "VALUES (?,?,?,?,?,?, ?,?,?,?, ?,?,?, ?,?,?,?,?,?, ?,?,?)";
                    using (OleDbCommand c = new OleDbCommand(sql, conn, trans))
                    {
                        c.Parameters.Add("@id", OleDbType.Integer).Value = bId;
                        c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                        c.Parameters.Add("@a1", OleDbType.Double).Value = R(m.BHKW.Waermebedarf);
                        c.Parameters.Add("@a2", OleDbType.Double).Value = R(m.BHKW.Restwaermebedarf);
                        c.Parameters.Add("@a3", OleDbType.Double).Value = R(m.BHKW.Strombedarf);
                        c.Parameters.Add("@a4", OleDbType.Double).Value = R(m.BHKW.Reststrombedarf);
                        c.Parameters.Add("@a5", OleDbType.Double).Value = R(m.BHKW.Waermeproduktion);
                        c.Parameters.Add("@a6", OleDbType.Double).Value = R(m.BHKW.Waermeueberschuss);
                        c.Parameters.Add("@a7", OleDbType.Double).Value = R(m.BHKW.Stromproduktion);
                        c.Parameters.Add("@a8", OleDbType.Double).Value = R(m.BHKW.Betriebsstunden_Gesamt);
                        c.Parameters.Add("@a9", OleDbType.Double).Value = R(m.BHKW.Betriebsstunden_Durchschnitt);
                        c.Parameters.Add("@a10", OleDbType.Double).Value = R(m.BHKW.Waermebedarfsdeckung);
                        c.Parameters.Add("@a11", OleDbType.Double).Value = R(m.BHKW.Strombedarfsdeckung);
                        c.Parameters.Add("@a12", OleDbType.Double).Value = R(m.BHKW.Gasverbrauch);
                        c.Parameters.Add("@a13", OleDbType.Double).Value = R(m.BHKW.Oelverbrauch);
                        c.Parameters.Add("@a14", OleDbType.Double).Value = R(m.BHKW.Koks);
                        c.Parameters.Add("@a15", OleDbType.Double).Value = R(m.BHKW.Rapsoelverbrauch);
                        c.Parameters.Add("@a16", OleDbType.Double).Value = R(m.BHKW.Holzverbrauch);
                        c.Parameters.Add("@a17", OleDbType.Double).Value = R(m.BHKW.Kohle);
                        c.Parameters.Add("@a18", OleDbType.Double).Value = R(m.BHKW.Sonstigverbrauch);
                        c.Parameters.Add("@a19", OleDbType.Double).Value = R(m.BHKW.Pellets);
                        c.Parameters.Add("@a20", OleDbType.Double).Value = R(m.BHKW.TierischeFette);
                        c.ExecuteNonQuery();
                    }

                    if (m.BHKW.Module != null && m.BHKW.Module.Count > 0)
                    {
                        // Dominanten Brennstoff + Gesamtverbrauch einmal bestimmen; der Verbrauch
                        // wird anteilig nach Waermeproduktion auf die Module verteilt (Summe = Gesamt).
                        string bhArt; double bhVerbrauch;
                        BHKWBrennstoff(m.BHKW, out bhArt, out bhVerbrauch);
                        double basis = 0;
                        foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module) basis += mo.Waermeproduktion;

                        int modId = NextId(conn, trans, TAB_BHKW_MODUL);
                        string sqlM = "INSERT INTO " + TAB_BHKW_MODUL + " (" +
                            "ID, ID_ErgebnisBHKW, Modul, Waermeproduktion, Stromproduktion, Brennstoff, Verbrauch, carrier_id) " +
                            "VALUES (?,?,?,?,?,?,?,?)";
                        foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module)
                        {
                            double anteil = basis > 0 ? mo.Waermeproduktion / basis : 1.0 / m.BHKW.Module.Count;
                            using (OleDbCommand c = new OleDbCommand(sqlM, conn, trans))
                            {
                                c.Parameters.Add("@id", OleDbType.Integer).Value = modId++;
                                c.Parameters.Add("@bh", OleDbType.Integer).Value = bId;
                                c.Parameters.Add("@mod", OleDbType.VarWChar).Value = (object)(mo.Modul ?? "");
                                c.Parameters.Add("@w", OleDbType.Double).Value = R(mo.Waermeproduktion);
                                c.Parameters.Add("@s", OleDbType.Double).Value = R(mo.Stromproduktion);
                                if (bhArt != null)
                                {
                                    c.Parameters.Add("@b", OleDbType.VarWChar).Value = bhArt;
                                    c.Parameters.Add("@v", OleDbType.Double).Value = R(bhVerbrauch * anteil);
                                }
                                else
                                {
                                    c.Parameters.Add("@b", OleDbType.VarWChar).Value = DBNull.Value;
                                    c.Parameters.Add("@v", OleDbType.Double).Value = DBNull.Value;
                                }
                                c.Parameters.Add("@ca", OleDbType.Integer).Value =
                                    bhkwCarrier > 0 ? (object)bhkwCarrier : DBNull.Value;
                                c.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // 6. Detail: Heizkessel (+ Modulliste).
                if (m.Heizkessel != null)
                {
                    int hId = NextId(conn, trans, TAB_KESSEL);
                    string sql = "INSERT INTO " + TAB_KESSEL + " (" +
                        "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Waermeproduktion, Strombedarf, " +
                        "Reststrombedarf, Waermebedarfsdeckung, Stromverbrauch, Maximale_Kesselleistung, Gasspitze, " +
                        "Gasverbrauch, Oelverbrauch, Koks, Rapsoelverbrauch, Holzverbrauch, Kohle, " +
                        "Sonstigverbrauch, Pellets, TierischeFette) " +
                        "VALUES (?,?,?,?,?,?, ?,?,?,?,?, ?,?,?,?,?,?, ?,?,?)";
                    using (OleDbCommand c = new OleDbCommand(sql, conn, trans))
                    {
                        c.Parameters.Add("@id", OleDbType.Integer).Value = hId;
                        c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                        c.Parameters.Add("@a1", OleDbType.Double).Value = R(m.Heizkessel.Waermebedarf);
                        c.Parameters.Add("@a2", OleDbType.Double).Value = R(m.Heizkessel.Restwaermebedarf);
                        c.Parameters.Add("@a3", OleDbType.Double).Value = R(m.Heizkessel.Waermeproduktion);
                        c.Parameters.Add("@a4", OleDbType.Double).Value = R(m.Heizkessel.Strombedarf);
                        c.Parameters.Add("@a5", OleDbType.Double).Value = R(m.Heizkessel.Reststrombedarf);
                        c.Parameters.Add("@a6", OleDbType.Double).Value = R(m.Heizkessel.Waermebedarfsdeckung);
                        c.Parameters.Add("@a7", OleDbType.Double).Value = R(m.Heizkessel.Stromverbrauch);
                        c.Parameters.Add("@a8", OleDbType.Double).Value = R(m.Heizkessel.Maximale_Kesselleistung);
                        c.Parameters.Add("@a9", OleDbType.Double).Value = R(m.Heizkessel.Gasspitze);
                        c.Parameters.Add("@a10", OleDbType.Double).Value = R(m.Heizkessel.Gasverbrauch);
                        c.Parameters.Add("@a11", OleDbType.Double).Value = R(m.Heizkessel.Oelverbrauch);
                        c.Parameters.Add("@a12", OleDbType.Double).Value = R(m.Heizkessel.Koks);
                        c.Parameters.Add("@a13", OleDbType.Double).Value = R(m.Heizkessel.Rapsoelverbrauch);
                        c.Parameters.Add("@a14", OleDbType.Double).Value = R(m.Heizkessel.Holzverbrauch);
                        c.Parameters.Add("@a15", OleDbType.Double).Value = R(m.Heizkessel.Kohle);
                        c.Parameters.Add("@a16", OleDbType.Double).Value = R(m.Heizkessel.Sonstigverbrauch);
                        c.Parameters.Add("@a17", OleDbType.Double).Value = R(m.Heizkessel.Pellets);
                        c.Parameters.Add("@a18", OleDbType.Double).Value = R(m.Heizkessel.TierischeFette);
                        c.ExecuteNonQuery();
                    }

                    if (m.Heizkessel.Module != null && m.Heizkessel.Module.Count > 0)
                    {
                        int modId = NextId(conn, trans, TAB_KESSEL_MODUL);
                        // Befund B3 behoben: Waermeproduktion wird persistiert; Verbrauch gerundet;
                        // Parametername @g war doppelt vergeben.
                        string sqlM = "INSERT INTO " + TAB_KESSEL_MODUL + " (" +
                            "ID, ID_ErgebnisHeizkessel, Modul, Waerme_Gas, Waerme_Oel, Waermeproduktion, " +
                            "Brennstoff, Verbrauch, Jahresnutzungsgrad, carrier_id) " +
                            "VALUES (?,?,?,?,?,?,?,?,?,?)";
                        foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                        {
                            using (OleDbCommand c = new OleDbCommand(sqlM, conn, trans))
                            {
                                c.Parameters.Add("@id", OleDbType.Integer).Value = modId++;
                                c.Parameters.Add("@hk", OleDbType.Integer).Value = hId;
                                c.Parameters.Add("@mod", OleDbType.VarWChar).Value = (object)(mo.Modul ?? "");
                                c.Parameters.Add("@g", OleDbType.Double).Value = R(mo.Waerme_Gas);
                                c.Parameters.Add("@o", OleDbType.Double).Value = R(mo.Waerme_Oel);
                                c.Parameters.Add("@w", OleDbType.Double).Value = R(mo.Waermeproduktion);
                                c.Parameters.Add("@b", OleDbType.VarWChar).Value = (object)(mo.Brennstoff ?? "");
                                c.Parameters.Add("@v", OleDbType.Double).Value = R(mo.Verbrauch);
                                c.Parameters.Add("@j", OleDbType.Double).Value = R(mo.Jahresnutzungsgrad);
                                c.Parameters.Add("@ca", OleDbType.Integer).Value =
                                    kesselCarrier > 0 ? (object)kesselCarrier : DBNull.Value;
                                c.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // 7. Detail: Solarthermie (+ Kollektorliste).
                if (m.Solarthermie != null)
                {
                    int sId = NextId(conn, trans, TAB_SOLAR);
                    string sql = "INSERT INTO " + TAB_SOLAR + " (" +
                        "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Waermeproduktion, Waermebedarfsdeckung, Ueberschuss) " +
                        "VALUES (?,?,?,?,?,?,?)";
                    using (OleDbCommand c = new OleDbCommand(sql, conn, trans))
                    {
                        c.Parameters.Add("@id", OleDbType.Integer).Value = sId;
                        c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                        c.Parameters.Add("@a1", OleDbType.Double).Value = R(m.Solarthermie.Waermebedarf);
                        c.Parameters.Add("@a2", OleDbType.Double).Value = R(m.Solarthermie.Restwaermebedarf);
                        c.Parameters.Add("@a3", OleDbType.Double).Value = R(m.Solarthermie.Waermeproduktion);
                        c.Parameters.Add("@a4", OleDbType.Double).Value = R(m.Solarthermie.Waermebedarfsdeckung);
                        c.Parameters.Add("@a5", OleDbType.Double).Value = R(m.Solarthermie.Ueberschuss);
                        c.ExecuteNonQuery();
                    }

                    if (m.Solarthermie.Module != null && m.Solarthermie.Module.Count > 0)
                    {
                        int modId = NextId(conn, trans, TAB_SOLAR_MODUL);
                        string sqlM = "INSERT INTO " + TAB_SOLAR_MODUL + " (" +
                            "ID, ID_ErgebnisSolarthermie, Modul, Flaeche, Anzahl, Waermeproduktion, Ueberschuss) " +
                            "VALUES (?,?,?,?,?,?,?)";
                        foreach (ErgebnisSolarthermieModulModel mo in m.Solarthermie.Module)
                        {
                            using (OleDbCommand c = new OleDbCommand(sqlM, conn, trans))
                            {
                                c.Parameters.Add("@id", OleDbType.Integer).Value = modId++;
                                c.Parameters.Add("@so", OleDbType.Integer).Value = sId;
                                c.Parameters.Add("@mod", OleDbType.VarWChar).Value = (object)(mo.Modul ?? "");
                                c.Parameters.Add("@fl", OleDbType.Double).Value = R(mo.Flaeche);
                                c.Parameters.Add("@an", OleDbType.Integer).Value = (int)mo.Anzahl;
                                c.Parameters.Add("@w", OleDbType.Double).Value = R(mo.Waermeproduktion);
                                c.Parameters.Add("@u", OleDbType.Double).Value = R(mo.Ueberschuss);
                                c.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // 8. Detail: Photovoltaik (+ Modulliste).
                if (m.Photovoltaik != null)
                {
                    int pId = NextId(conn, trans, TAB_PV);
                    string sql = "INSERT INTO " + TAB_PV + " (" +
                        "ID, ID_Ergebnis, Strombedarf, Reststrombedarf, Stromproduktion, Strombedarfsdeckung, Ueberschuss, MaxSolareLeistung) " +
                        "VALUES (?,?,?,?,?,?,?,?)";
                    using (OleDbCommand c = new OleDbCommand(sql, conn, trans))
                    {
                        c.Parameters.Add("@id", OleDbType.Integer).Value = pId;
                        c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                        c.Parameters.Add("@a1", OleDbType.Double).Value = R(m.Photovoltaik.Strombedarf);
                        c.Parameters.Add("@a2", OleDbType.Double).Value = R(m.Photovoltaik.Reststrombedarf);
                        c.Parameters.Add("@a3", OleDbType.Double).Value = R(m.Photovoltaik.Stromproduktion);
                        c.Parameters.Add("@a4", OleDbType.Double).Value = R(m.Photovoltaik.Strombedarfsdeckung);
                        c.Parameters.Add("@a5", OleDbType.Double).Value = R(m.Photovoltaik.Ueberschuss);
                        c.Parameters.Add("@a6", OleDbType.Double).Value = R(m.Photovoltaik.MaxSolareLeistung);
                        c.ExecuteNonQuery();
                    }

                    if (m.Photovoltaik.Module != null && m.Photovoltaik.Module.Count > 0)
                    {
                        int modId = NextId(conn, trans, TAB_PV_MODUL);
                        string sqlM = "INSERT INTO " + TAB_PV_MODUL + " (" +
                            "ID, ID_ErgebnisPhotovoltaik, Modul, Flaeche, Anzahl, Stromproduktion) " +
                            "VALUES (?,?,?,?,?,?)";
                        foreach (ErgebnisPhotovoltaikModulModel mo in m.Photovoltaik.Module)
                        {
                            using (OleDbCommand c = new OleDbCommand(sqlM, conn, trans))
                            {
                                c.Parameters.Add("@id", OleDbType.Integer).Value = modId++;
                                c.Parameters.Add("@pv", OleDbType.Integer).Value = pId;
                                c.Parameters.Add("@mod", OleDbType.VarWChar).Value = (object)(mo.Modul ?? "");
                                c.Parameters.Add("@fl", OleDbType.Double).Value = R(mo.Flaeche);
                                c.Parameters.Add("@an", OleDbType.Integer).Value = (int)mo.Anzahl;
                                c.Parameters.Add("@s", OleDbType.Double).Value = R(mo.Stromproduktion);
                                c.ExecuteNonQuery();
                            }
                        }
                    }
                }

                trans.Commit();
                m.ID = kopfId;
                return kopfId;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                MessageBox.Show("Fehler beim Speichern des Simulationsergebnisses: " + ex.Message);
                return -1;
            }
            finally { try { conn.Close(); } catch { } }
        }

        // Laedt das zuletzt gespeicherte Ergebnis eines Projekts (oder null).
        public ErgebnisModel Load(int idProjekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT TOP 1 * FROM " + TAB_KOPF + " WHERE ID_Projekt = ? ORDER BY ID DESC",
                new OleDbParameter("@p", idProjekt));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            ErgebnisModel m = new ErgebnisModel();
            m.ID = I(r, "ID");
            m.ID_Projekt = I(r, "ID_Projekt");
            m.Bezeichner = S(r, "Bezeichner");
            if (r.Table.Columns.Contains("Zeitstempel") && r["Zeitstempel"] != DBNull.Value)
                m.Zeitstempel = Convert.ToDateTime(r["Zeitstempel"]);
            m.ID_Klimaregion = I(r, "ID_Klimaregion");
            m.Sim_Energiebedarf = B(r, "Sim_Energiebedarf");
            m.Sim_Waermepumpe = B(r, "Sim_Waermepumpe");
            m.Sim_Heizkessel = B(r, "Sim_Heizkessel");
            m.Sim_Solarthermie = B(r, "Sim_Solarthermie");
            m.Sim_BHKW = B(r, "Sim_BHKW");
            m.Sim_PV = B(r, "Sim_PV");
            m.Sim_Stromspeicher = B(r, "Sim_Stromspeicher");

            // Detail: Energiebedarf.
            DataTable de = DataRepository.GetDataTable(
                "SELECT TOP 1 * FROM " + TAB_ENERGIE + " WHERE ID_Ergebnis = ?", new OleDbParameter("@e", m.ID));
            if (de != null && de.Rows.Count > 0)
            {
                DataRow re = de.Rows[0];
                m.Energiebedarf = new ErgebnisEnergiebedarfModel();
                m.Energiebedarf.Waermebedarf_Gesamt = D(re, "Waermebedarf_Gesamt");
                m.Energiebedarf.Waermelast_Max = D(re, "Waermelast_Max");
                m.Energiebedarf.Strombedarf_Gesamt = D(re, "Strombedarf_Gesamt");
                m.Energiebedarf.Strombedarf_Max = D(re, "Strombedarf_Max");
                m.Energiebedarf.Waermerestbedarf = D(re, "Waermerestbedarf");
                m.Energiebedarf.Stromrestbedarf = D(re, "Stromrestbedarf");
            }

            // Detail: Waermepumpe (+ Module).
            DataTable dw = DataRepository.GetDataTable(
                "SELECT TOP 1 * FROM " + TAB_WP + " WHERE ID_Ergebnis = ?", new OleDbParameter("@e", m.ID));
            if (dw != null && dw.Rows.Count > 0)
            {
                DataRow rw = dw.Rows[0];
                int wpId = I(rw, "ID");
                ErgebnisWaermepumpeModel w = new ErgebnisWaermepumpeModel();
                w.Waermebedarf = D(rw, "Waermebedarf");
                w.Restwaermebedarf = D(rw, "Restwaermebedarf");
                w.Waermeproduktion_WP = D(rw, "Waermeproduktion_WP");
                w.Stromverbrauch_WP = D(rw, "Stromverbrauch_WP");
                w.Stromverbrauch_Heizstab = D(rw, "Stromverbrauch_Heizstab");
                w.Kapazitaet_Pufferspeicher = D(rw, "Kapazitaet_Pufferspeicher");
                w.Min_Spitzenkesselleistung = D(rw, "Min_Spitzenkesselleistung");
                w.Waermebedarfsdeckung = D(rw, "Waermebedarfsdeckung");
                w.Vollbenutzungsstunden = D(rw, "Vollbenutzungsstunden");
                if (rw.Table.Columns.Contains("Bivalenzpunkt") && rw["Bivalenzpunkt"] != DBNull.Value)
                    w.Bivalenzpunkt = Convert.ToDouble(rw["Bivalenzpunkt"]);

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_WP_MODUL + " WHERE ID_ErgebnisWaermepumpe = ? ORDER BY ID",
                    new OleDbParameter("@w", wpId));
                if (dmod != null)
                    foreach (DataRow rm in dmod.Rows)
                    {
                        ErgebnisWaermepumpeModulModel mo = new ErgebnisWaermepumpeModulModel();
                        mo.Modul = S(rm, "Modul");
                        mo.Leistung = D(rm, "Leistung");
                        mo.Waermeproduktion = D(rm, "Waermeproduktion");
                        mo.Stromverbrauch = D(rm, "Stromverbrauch");
                        mo.Heizstab = D(rm, "Heizstab");
                        mo.Betriebsstunden = D(rm, "Betriebsstunden");
                        w.Module.Add(mo);
                    }

                m.Waermepumpe = w;
            }

            // Detail: BHKW (+ Module).
            DataTable dbk = DataRepository.GetDataTable(
                "SELECT TOP 1 * FROM " + TAB_BHKW + " WHERE ID_Ergebnis = ?", new OleDbParameter("@e", m.ID));
            if (dbk != null && dbk.Rows.Count > 0)
            {
                DataRow rb = dbk.Rows[0];
                int bId = I(rb, "ID");
                ErgebnisBHKWModel b = new ErgebnisBHKWModel();
                b.Waermebedarf = D(rb, "Waermebedarf");
                b.Restwaermebedarf = D(rb, "Restwaermebedarf");
                b.Strombedarf = D(rb, "Strombedarf");
                b.Reststrombedarf = D(rb, "Reststrombedarf");
                b.Waermeproduktion = D(rb, "Waermeproduktion");
                b.Waermeueberschuss = D(rb, "Waermeueberschuss");
                b.Stromproduktion = D(rb, "Stromproduktion");
                b.Betriebsstunden_Gesamt = D(rb, "Betriebsstunden_Gesamt");
                b.Betriebsstunden_Durchschnitt = D(rb, "Betriebsstunden_Durchschnitt");
                b.Waermebedarfsdeckung = D(rb, "Waermebedarfsdeckung");
                b.Strombedarfsdeckung = D(rb, "Strombedarfsdeckung");
                b.Gasverbrauch = D(rb, "Gasverbrauch");
                b.Oelverbrauch = D(rb, "Oelverbrauch");
                b.Koks = D(rb, "Koks");
                b.Rapsoelverbrauch = D(rb, "Rapsoelverbrauch");
                b.Holzverbrauch = D(rb, "Holzverbrauch");
                b.Kohle = D(rb, "Kohle");
                b.Sonstigverbrauch = D(rb, "Sonstigverbrauch");
                b.Pellets = D(rb, "Pellets");
                b.TierischeFette = D(rb, "TierischeFette");

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_BHKW_MODUL + " WHERE ID_ErgebnisBHKW = ? ORDER BY ID",
                    new OleDbParameter("@b", bId));
                if (dmod != null)
                    foreach (DataRow rm in dmod.Rows)
                    {
                        ErgebnisBHKWModulModel mo = new ErgebnisBHKWModulModel();
                        mo.Modul = S(rm, "Modul");
                        mo.Waermeproduktion = D(rm, "Waermeproduktion");
                        mo.Stromproduktion = D(rm, "Stromproduktion");
                        mo.Brennstoff = S(rm, "Brennstoff");
                        mo.Verbrauch = D(rm, "Verbrauch");
                        mo.CarrierId = I(rm, "carrier_id");
                        b.Module.Add(mo);
                    }

                m.BHKW = b;
            }

            // Detail: Heizkessel (+ Module).
            DataTable dhk = DataRepository.GetDataTable(
                "SELECT TOP 1 * FROM " + TAB_KESSEL + " WHERE ID_Ergebnis = ?", new OleDbParameter("@e", m.ID));
            if (dhk != null && dhk.Rows.Count > 0)
            {
                DataRow rh = dhk.Rows[0];
                int hId = I(rh, "ID");
                ErgebnisHeizkesselModel h = new ErgebnisHeizkesselModel();
                h.Waermebedarf = D(rh, "Waermebedarf");
                h.Restwaermebedarf = D(rh, "Restwaermebedarf");
                h.Waermeproduktion = D(rh, "Waermeproduktion");
                h.Strombedarf = D(rh, "Strombedarf");
                h.Reststrombedarf = D(rh, "Reststrombedarf");
                h.Waermebedarfsdeckung = D(rh, "Waermebedarfsdeckung");
                h.Stromverbrauch = D(rh, "Stromverbrauch");
                h.Maximale_Kesselleistung = D(rh, "Maximale_Kesselleistung");
                h.Gasspitze = D(rh, "Gasspitze");
                h.Gasverbrauch = D(rh, "Gasverbrauch");
                h.Oelverbrauch = D(rh, "Oelverbrauch");
                h.Koks = D(rh, "Koks");
                h.Rapsoelverbrauch = D(rh, "Rapsoelverbrauch");
                h.Holzverbrauch = D(rh, "Holzverbrauch");
                h.Kohle = D(rh, "Kohle");
                h.Sonstigverbrauch = D(rh, "Sonstigverbrauch");
                h.Pellets = D(rh, "Pellets");
                h.TierischeFette = D(rh, "TierischeFette");

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_KESSEL_MODUL + " WHERE ID_ErgebnisHeizkessel = ? ORDER BY ID",
                    new OleDbParameter("@h", hId));
                if (dmod != null)
                    foreach (DataRow rm in dmod.Rows)
                    {
                        ErgebnisHeizkesselModulModel mo = new ErgebnisHeizkesselModulModel();
                        mo.Modul = S(rm, "Modul");
                        mo.Waerme_Gas = D(rm, "Waerme_Gas");
                        mo.Waerme_Oel = D(rm, "Waerme_Oel");
                        mo.Waermeproduktion = D(rm, "Waermeproduktion");
                        mo.Brennstoff = S(rm, "Brennstoff");
                        mo.Verbrauch = D(rm, "Verbrauch");
                        mo.CarrierId = I(rm, "carrier_id");
                        mo.Jahresnutzungsgrad = D(rm, "Jahresnutzungsgrad");
                        h.Module.Add(mo);
                    }

                m.Heizkessel = h;
            }

            // Detail: Solarthermie (+ Kollektoren).
            DataTable dst = DataRepository.GetDataTable(
                "SELECT TOP 1 * FROM " + TAB_SOLAR + " WHERE ID_Ergebnis = ?", new OleDbParameter("@e", m.ID));
            if (dst != null && dst.Rows.Count > 0)
            {
                DataRow rs2 = dst.Rows[0];
                int sId = I(rs2, "ID");
                ErgebnisSolarthermieModel s = new ErgebnisSolarthermieModel();
                s.Waermebedarf = D(rs2, "Waermebedarf");
                s.Restwaermebedarf = D(rs2, "Restwaermebedarf");
                s.Waermeproduktion = D(rs2, "Waermeproduktion");
                s.Waermebedarfsdeckung = D(rs2, "Waermebedarfsdeckung");
                s.Ueberschuss = D(rs2, "Ueberschuss");

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_SOLAR_MODUL + " WHERE ID_ErgebnisSolarthermie = ? ORDER BY ID",
                    new OleDbParameter("@s", sId));
                if (dmod != null)
                    foreach (DataRow rm in dmod.Rows)
                    {
                        ErgebnisSolarthermieModulModel mo = new ErgebnisSolarthermieModulModel();
                        mo.Modul = S(rm, "Modul");
                        mo.Flaeche = D(rm, "Flaeche");
                        mo.Anzahl = I(rm, "Anzahl");
                        mo.Waermeproduktion = D(rm, "Waermeproduktion");
                        mo.Ueberschuss = D(rm, "Ueberschuss");
                        s.Module.Add(mo);
                    }

                m.Solarthermie = s;
            }

            // Detail: Photovoltaik (+ Module).
            DataTable dpv = DataRepository.GetDataTable(
                "SELECT TOP 1 * FROM " + TAB_PV + " WHERE ID_Ergebnis = ?", new OleDbParameter("@e", m.ID));
            if (dpv != null && dpv.Rows.Count > 0)
            {
                DataRow rp = dpv.Rows[0];
                int pId = I(rp, "ID");
                ErgebnisPhotovoltaikModel p = new ErgebnisPhotovoltaikModel();
                p.Strombedarf = D(rp, "Strombedarf");
                p.Reststrombedarf = D(rp, "Reststrombedarf");
                p.Stromproduktion = D(rp, "Stromproduktion");
                p.Strombedarfsdeckung = D(rp, "Strombedarfsdeckung");
                p.Ueberschuss = D(rp, "Ueberschuss");
                p.MaxSolareLeistung = D(rp, "MaxSolareLeistung");

                DataTable dmodp = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_PV_MODUL + " WHERE ID_ErgebnisPhotovoltaik = ? ORDER BY ID",
                    new OleDbParameter("@p", pId));
                if (dmodp != null)
                    foreach (DataRow rm in dmodp.Rows)
                    {
                        ErgebnisPhotovoltaikModulModel mo = new ErgebnisPhotovoltaikModulModel();
                        mo.Modul = S(rm, "Modul");
                        mo.Flaeche = D(rm, "Flaeche");
                        mo.Anzahl = I(rm, "Anzahl");
                        mo.Stromproduktion = D(rm, "Stromproduktion");
                        p.Module.Add(mo);
                    }

                m.Photovoltaik = p;
            }

            return m;
        }

        public bool HasErgebnis(int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TAB_KOPF + " WHERE ID_Projekt = ?",
                new OleDbParameter("@p", idProjekt));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        // Dominierender Brennstoff des BHKW samt Gesamtverbrauch (MWh/a); art = null, wenn keiner > 0.
        private static void BHKWBrennstoff(ErgebnisBHKWModel b, out string art, out double verbrauch)
        {
            string[] arten = { "Gas", "Öl", "Pellets", "Holz", "Kohle", "Koks", "Rapsöl", "Tierische Fette", "Sonstige" };
            double[] werte = { b.Gasverbrauch, b.Oelverbrauch, b.Pellets, b.Holzverbrauch, b.Kohle,
                               b.Koks, b.Rapsoelverbrauch, b.TierischeFette, b.Sonstigverbrauch };
            art = null; verbrauch = 0;
            for (int i = 0; i < arten.Length; i++)
                if (werte[i] > verbrauch) { art = arten[i]; verbrauch = werte[i]; }
            if (verbrauch <= 0) { art = null; verbrauch = 0; }
        }

        // Ergaenzt fehlende Spalten in Tab_ErgebnisEnergiebedarf (Restbedarf) - einmalige, tolerante Migration.
        private static void StelleEnergieSpaltenSicher()
        {
            string[] spalten = { "Waermerestbedarf", "Stromrestbedarf" };
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    DataTable cols = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                        new object[] { null, null, TAB_ENERGIE, null });
                    var vorhanden = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (cols != null)
                        foreach (DataRow rc in cols.Rows) vorhanden.Add(rc["COLUMN_NAME"].ToString());
                    foreach (string sp in spalten)
                    {
                        if (vorhanden.Contains(sp)) continue;
                        using (OleDbCommand c = new OleDbCommand(
                            "ALTER TABLE " + TAB_ENERGIE + " ADD COLUMN " + sp + " DOUBLE", conn))
                            c.ExecuteNonQuery();
                    }
                }
            }
            catch { /* best effort - Spalten existieren dann ggf. schon */ }
        }

        // Ergaenzt fehlende Brennstoffspalten in Tab_ErgebnisBHKW - einmalige, tolerante Migration.
        private static void StelleBHKWSpaltenSicher()
        {
            string[] spalten = { "Oelverbrauch", "Koks", "Rapsoelverbrauch", "Holzverbrauch", "Kohle",
                                 "Sonstigverbrauch", "Pellets", "TierischeFette" };
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    DataTable cols = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                        new object[] { null, null, TAB_BHKW, null });
                    var vorhanden = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (cols != null)
                        foreach (DataRow rc in cols.Rows) vorhanden.Add(rc["COLUMN_NAME"].ToString());
                    foreach (string sp in spalten)
                    {
                        if (vorhanden.Contains(sp)) continue;
                        using (OleDbCommand c = new OleDbCommand(
                            "ALTER TABLE " + TAB_BHKW + " ADD COLUMN " + sp + " DOUBLE", conn))
                            c.ExecuteNonQuery();
                    }
                }
            }
            catch { /* best effort - Spalten existieren dann ggf. schon */ }
        }

        // Ergaenzt carrier_id in beiden Modultabellen (Befund B1) und Waermeproduktion
        // im Kesselmodul (Befund B3) - einmalige, tolerante Migration nach dem Muster
        // der uebrigen StelleXSpaltenSicher-Methoden.
        private static void StelleModulSpaltenSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    ErgaenzeSpalte(conn, TAB_BHKW_MODUL, "carrier_id", "LONG");
                    ErgaenzeSpalte(conn, TAB_KESSEL_MODUL, "carrier_id", "LONG");
                    ErgaenzeSpalte(conn, TAB_KESSEL_MODUL, "Waermeproduktion", "DOUBLE");
                }
            }
            catch { /* best effort - Spalten existieren dann ggf. schon */ }
        }

        private static void ErgaenzeSpalte(OleDbConnection conn, string tabelle, string spalte, string typ)
        {
            DataTable cols = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                new object[] { null, null, tabelle, null });
            if (cols != null)
                foreach (DataRow rc in cols.Rows)
                    if (string.Equals(rc["COLUMN_NAME"].ToString(), spalte, StringComparison.OrdinalIgnoreCase))
                        return;   // vorhanden
            using (OleDbCommand c = new OleDbCommand(
                "ALTER TABLE " + tabelle + " ADD COLUMN " + spalte + " " + typ, conn))
                c.ExecuteNonQuery();
        }

        // Ermittelt die energy_carrier-ID zum Brennstoff des Erzeugers eines Projekts
        // (eingabeTabelle: "Tab_BHKW" oder "Tab_Heizkessel"; deren Spalte Brennstoff
        // verweist auf Tab_Brennstoff_Stamm.ID = energy_carrier.id_brennstoff).
        // Vorrang hat der dem Projekt zugeordnete Traeger (energy_project_settings);
        // Fallback: erster Katalogtraeger des Brennstoffs. 0 = keine Zuordnung.
        private static int CarrierIdFuerProjekt(int idProjekt, string eingabeTabelle)
        {
            try
            {
                object bs = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 Brennstoff FROM " + eingabeTabelle + " WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", idProjekt));
                if (bs == null || bs == DBNull.Value) return 0;
                int idBrennstoff;
                try { idBrennstoff = Convert.ToInt32(bs); }
                catch { return 0; }
                if (idBrennstoff <= 0) return 0;

                object o = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 ec.id FROM energy_carrier AS ec " +
                    "INNER JOIN energy_project_settings AS s ON s.[ID_Energieträger] = ec.id " +
                    "WHERE ec.id_brennstoff = ? AND s.ID_Projekt = ?",
                    new OleDbParameter("@b", idBrennstoff),
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);

                o = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 id FROM energy_carrier WHERE id_brennstoff = ?",
                    new OleDbParameter("@b", idBrennstoff));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            catch { }
            return 0;
        }

        // --- Helpers ---

        private static int NextId(OleDbConnection conn, OleDbTransaction trans, string table)
        {
            using (OleDbCommand c = new OleDbCommand("SELECT MAX(ID) FROM " + table, conn, trans))
            {
                object m = c.ExecuteScalar();
                return ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
            }
        }

        // Rundet Ergebniswerte auf max. 2 Nachkommastellen (kaufmaennisch).
        private static double R(double v)
        { return Math.Round(v, 2, MidpointRounding.AwayFromZero); }

        private static int I(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToInt32(r[col]) : 0; }
        private static double D(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToDouble(r[col]) : 0.0; }
        private static bool B(DataRow r, string col)
        { return r.Table.Columns.Contains(col) && r[col] != DBNull.Value && Convert.ToBoolean(r[col]); }
        private static string S(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? r[col].ToString() : ""; }
    }
}
