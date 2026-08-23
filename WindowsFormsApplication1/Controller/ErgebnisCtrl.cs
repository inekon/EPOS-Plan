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
        public const string TAB_PUFFER = "Tab_ErgebnisPufferspeicher";
        public const string TAB_SP = "Tab_ErgebnisStromspeicher";

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
                // Defensiv VOR dem Kopf-Delete - dieselbe Begruendung wie in Save:
                // auf einer Datenbank ohne FK_ErgPuffer gibt es keine Loeschweitergabe,
                // die Pufferzeilen blieben als Waisen stehen und zeigten wegen der
                // MAX(ID)+1-Vergabe spaeter auf fremde Laeufe (Konzept 6.6).
                PufferzeilenLoeschen(conn, trans, idProjekt);
                DetailzeilenLoeschen(conn, trans, TAB_SP, idProjekt);

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
                // PAKET 8 (Konzept 13.4): Dieselbe Entscheidungsstelle wie in
                // DataRepository - im Engine-Modus ein Protokolleintrag, sonst der Dialog
                // wie bisher.
                //
                // BERICHTIGT (Nacharbeit, Befund N14a): Diese Methode wird NICHT aus
                // Save() gerufen - Save() löscht in seiner eigenen Transaktion inline.
                // Delete(int) hat im Anwendungsprojekt derzeit überhaupt keinen Aufrufer
                // und ist ein Sicherheitsnetz; die Umstellung steht hier, damit ein
                // künftiger Aufruf aus dem Rechenpfad nicht wieder einen Dialog öffnet.
                DataRepository.FehlerMelden("Fehler beim Löschen des Simulationsergebnisses: " + ex.Message);
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
            StelleKesselSpaltenSicher();    // Quellwaerme der Kaskade (Etappe D4) ergänzen
            StellePufferTabelleSicher();    // Tab_ErgebnisPufferspeicher (Konzept 6.6) - Rückfallebene
            StelleStromspeicherTabelleSicher(); // Tab_ErgebnisStromspeicher (AP3, Fachkonzept 7.1)

            // Energieträger: Die carrier_id steht JE MODUL im Ergebnis — der Lauf setzt sie
            // aus Tab_Energieanlagen.ID_Carrier (Befund B1, SimulationRunner), und genau so
            // wird sie unten geschrieben. Eine projektweite Rückfallebene über den
            // Brennstoff der Eingabetabelle stand hier bis 22.08.2026 ohne Wirkung; sie ist
            // bewusst entfernt: Tab_BHKW/Tab_Heizkessel führen EINE ZEILE JE GERÄT
            // (Kaskade), ein TOP 1 ohne Sortierung hätte den Träger eines beliebigen Geräts
            // auf alle Module gestempelt, und zu einem Brennstoff gibt es mehrere
            // energy_carrier-Sätze mit verschiedenem Heizwert und Preis. Eine fehlende
            // Zuordnung ist ein gemeldeter Datenzustand, kein Schätzwert:
            // SimulationControl.EnergietraegerZuordnungLesen warnt im Protokoll, und
            // KostenEmissionRechner zählt sie als verbrauchOhneTraeger (kostenVollstaendig
            // = false). Die Rückfallebene gehört auf die Leseseite — dort steht sie bereits
            // (WirtschaftlichkeitCtrl.BaueSteuerAnlage: Modulträger vor Anlagenträger).

            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                // 1. "letztes Ergebnis je Projekt": vorhandene Ergebnisse entfernen.
                //    Loeschweitergabe raeumt alle Detailtabellen automatisch mit ab.

                //    Defensiv VOR dem Kopf-Delete: Die Puffer-Zeilen haengen zwar per
                //    FK_ErgPuffer mit DEL-CASCADE am Kopf (Migration Schritt 3), auf einer
                //    Datenbank, deren Tabelle von StellePufferTabelleSicher() ohne
                //    Constraint entstanden ist, gaebe es die Weitergabe aber nicht.
                //    Ohne dieses Delete blieben Waisenzeilen stehen, die wegen der
                //    MAX(ID)+1-Vergabe spaeter auf fremde Laeufe zeigen wuerden (6.6).
                PufferzeilenLoeschen(conn, trans, m.ID_Projekt);
                DetailzeilenLoeschen(conn, trans, TAB_SP, m.ID_Projekt);

                //    Zusaetzlich alle Waisen abraeumen, deren Kopf nicht mehr existiert.
                //    Notwendig, weil ein frueherer Kopf-Delete OHNE Loeschweitergabe
                //    Zeilen hinterlassen haben kann: die Kopf-ID wird per MAX(ID)+1
                //    wiederverwendet, die alte Zeile haengt danach an einem FREMDEN
                //    Projekt und faelscht dessen Ergebnisausweis (Konzept 6.6).
                try
                {
                    using (OleDbCommand c = new OleDbCommand(
                        "DELETE FROM " + TAB_PUFFER + " WHERE ID_Ergebnis NOT IN " +
                        "(SELECT ID FROM " + TAB_KOPF + ")", conn, trans))
                        c.ExecuteNonQuery();
                }
                catch { /* Tabelle (noch) nicht vorhanden - dann gibt es auch keine Waisen */ }

                //    Dieselbe Waisenpruefung fuer die Stromspeicherzeilen (AP3).
                try
                {
                    using (OleDbCommand c = new OleDbCommand(
                        "DELETE FROM " + TAB_SP + " WHERE ID_Ergebnis NOT IN " +
                        "(SELECT ID FROM " + TAB_KOPF + ")", conn, trans))
                        c.ExecuteNonQuery();
                }
                catch { /* Tabelle (noch) nicht vorhanden - dann gibt es auch keine Waisen */ }

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
                        "Sonstigverbrauch, Pellets, TierischeFette, " +
                        SchemaKatalog.SPALTE_BHKW_VBH_ELEKTRISCH + ") " +
                        "VALUES (?,?,?,?,?,?, ?,?,?,?, ?,?,?, ?,?,?,?,?,?, ?,?,?, ?)";
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
                        // ETAPPE E2: leistungsgewichtete elektrische Vollbenutzungsstunden.
                        c.Parameters.Add("@a21", OleDbType.Double).Value = R(m.BHKW.VbhElektrisch);
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
                            "ID, ID_ErgebnisBHKW, Modul, Waermeproduktion, Stromproduktion, Brennstoff, Verbrauch, carrier_id, " +
                            SchemaKatalog.SPALTE_MODUL_VBH_THERMISCH + ", " +
                            SchemaKatalog.SPALTE_MODUL_VBH_ELEKTRISCH + ") " +
                            "VALUES (?,?,?,?,?,?,?,?,?,?)";
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
                                    mo.CarrierId > 0 ? (object)mo.CarrierId : DBNull.Value;
                                // ETAPPE E2 (L6): thermische und elektrische Vbh je Modul.
                                // Beide werden IMMER geschrieben - auch die 0. Sonst waere
                                // "nicht erhoben" (NULL) von "erhoben und null" nicht mehr
                                // unterscheidbar; dieselbe Begruendung wie bei Quellwaerme.
                                c.Parameters.Add("@vth", OleDbType.Double).Value = R(mo.VbhThermisch);
                                c.Parameters.Add("@vel", OleDbType.Double).Value = R(mo.VbhElektrisch);
                                c.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // 6. Detail: Heizkessel (+ Modulliste).
                if (m.Heizkessel != null)
                {
                    int hId = NextId(conn, trans, TAB_KESSEL);
                    // ETAPPE D4: Quellwaerme als LETZTE Spalte - ALTER TABLE haengt sie in
                    // Access hinten an, und die Parameterreihenfolge folgt der Spaltenliste.
                    string sql = "INSERT INTO " + TAB_KESSEL + " (" +
                        "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Waermeproduktion, Strombedarf, " +
                        "Reststrombedarf, Waermebedarfsdeckung, Stromverbrauch, Maximale_Kesselleistung, Gasspitze, " +
                        "Gasverbrauch, Oelverbrauch, Koks, Rapsoelverbrauch, Holzverbrauch, Kohle, " +
                        "Sonstigverbrauch, Pellets, TierischeFette, " +
                        SchemaKatalog.SPALTE_KESSEL_QUELLWAERME + ") " +
                        "VALUES (?,?,?,?,?,?, ?,?,?,?,?, ?,?,?,?,?,?, ?,?,?,?)";
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
                        c.Parameters.Add("@a19", OleDbType.Double).Value = R(m.Heizkessel.Quellwaerme);
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
                                    mo.CarrierId > 0 ? (object)mo.CarrierId : DBNull.Value;
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

                // 9. Detail: Pufferspeicher (Konzept 6.6) - eine Zeile je beteiligtem
                //    Speicher, Senken- wie Quellspeicher. IDs wie bei den
                //    Geschwistertabellen ueber MAX(ID)+1 und dann hochzaehlend.
                if (m.Pufferspeicher != null && m.Pufferspeicher.Count > 0)
                {
                    int pufId = NextId(conn, trans, TAB_PUFFER);
                    string sqlP = "INSERT INTO " + TAB_PUFFER + " (" +
                        "ID, ID_Ergebnis, ID_Pufferspeicher, Bezeichner, Verwendung, Q_max, " +
                        "Ladung_gesamt, Entladung_gesamt, Verluste_gesamt, SOC_Ende, SOC_Mittel, " +
                        "SOC_Max, Vollzyklen) " +
                        "VALUES (?,?,?,?,?,?, ?,?,?,?,?, ?,?)";
                    foreach (ErgebnisPufferspeicherModel sp in m.Pufferspeicher)
                    {
                        using (OleDbCommand c = new OleDbCommand(sqlP, conn, trans))
                        {
                            c.Parameters.Add("@id", OleDbType.Integer).Value = pufId++;
                            c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                            c.Parameters.Add("@sp", OleDbType.Integer).Value =
                                sp.ID_Pufferspeicher > 0 ? (object)sp.ID_Pufferspeicher : DBNull.Value;
                            c.Parameters.Add("@bez", OleDbType.VarWChar).Value = (object)(sp.Bezeichner ?? "");
                            c.Parameters.Add("@ver", OleDbType.VarWChar).Value = (object)(sp.Verwendung ?? "");
                            c.Parameters.Add("@a1", OleDbType.Double).Value = R(sp.Q_max);
                            c.Parameters.Add("@a2", OleDbType.Double).Value = R(sp.Ladung_gesamt);
                            c.Parameters.Add("@a3", OleDbType.Double).Value = R(sp.Entladung_gesamt);
                            c.Parameters.Add("@a4", OleDbType.Double).Value = R(sp.Verluste_gesamt);
                            c.Parameters.Add("@a5", OleDbType.Double).Value = R(sp.SOC_Ende);
                            c.Parameters.Add("@a6", OleDbType.Double).Value = R(sp.SOC_Mittel);
                            c.Parameters.Add("@a7", OleDbType.Double).Value = R(sp.SOC_Max);
                            c.Parameters.Add("@a8", OleDbType.Double).Value = R(sp.Vollzyklen);
                            c.ExecuteNonQuery();
                        }
                    }
                }

                // 10. Detail: Stromspeicher (Fachkonzept Stromspeicher 7.1) - eine Zeile
                //     je gerechneter Speicheranlage. Aufbau wie Block 8 (Photovoltaik):
                //     NextId einmal holen und hochzaehlen, Werte kaufmaennisch runden,
                //     alles in DERSELBEN Transaktion wie der Kopf.
                //
                //     Das Flag m.Sim_Stromspeicher entscheidet ueber den Block: Es sagt
                //     "die Speicherrechnung lief" und wird ab AP3b nur noch nach einem
                //     echten Engine-Lauf gesetzt (heute: SimulationRunner). Ohne Flag
                //     wird nichts geschrieben, auch wenn die Liste versehentlich gefuellt
                //     waere - Kopf und Detail sollen nie widersprechen.
                if (m.Sim_Stromspeicher && m.Stromspeicher != null && m.Stromspeicher.Count > 0)
                {
                    int spId = NextId(conn, trans, TAB_SP);
                    string sqlS = "INSERT INTO " + TAB_SP + " (" +
                        "ID, ID_Ergebnis, ID_Energieanlage, Bezeichner, Betriebsart, Berechnungsart, " +
                        "Ladung_PV, Ladung_BHKW, Ladung_Netz, Ladung_Gesamt, Entladung_Gesamt, Verluste_Gesamt, " +
                        "Netzbezug_Mit, Netzbezug_Ohne, Einspeisung_Mit, Einspeisung_Ohne, " +
                        "Eigenverbrauchsquote, Autarkiegrad, " +
                        "Vollzyklen, SoC_Min, SoC_Mittel, SoC_Max, " +
                        "Zeitanteil_Untergrenze, Zeitanteil_Obergrenze, Zyklen_Hochrechnung, " +
                        "Ertrag_Bezugsersparnis, Ertrag_Verguetung_Entgangen, Ertrag_Netzerloes, " +
                        "Kosten_Ladung, Ertrag_Leistungspreis, Verschleisskosten, " +
                        "Investition, Annuitaet, Jahresueberschuss, Ertrag_Jahr1, Ertrag_Aequivalent, " +
                        "Amortisation_Statisch, Amortisation_Dynamisch, Kapitalwert, Preisversion) " +
                        "VALUES (?,?,?,?,?,?, ?,?,?,?,?,?, ?,?,?,?, ?,?, ?,?,?,?, ?,?,?, " +
                        "?,?,?,?,?,?, ?,?,?,?,?, ?,?,?,?)";
                    foreach (ErgebnisStromspeicherModel es in m.Stromspeicher)
                    {
                        using (OleDbCommand c = new OleDbCommand(sqlS, conn, trans))
                        {
                            c.Parameters.Add("@id", OleDbType.Integer).Value = spId++;
                            c.Parameters.Add("@erg", OleDbType.Integer).Value = kopfId;
                            c.Parameters.Add("@anl", OleDbType.Integer).Value =
                                es.ID_Energieanlage > 0 ? (object)es.ID_Energieanlage : DBNull.Value;
                            c.Parameters.Add("@bez", OleDbType.VarWChar).Value = (object)(es.Bezeichner ?? "");
                            c.Parameters.Add("@bart", OleDbType.VarWChar).Value = (object)(es.Betriebsart ?? "");
                            c.Parameters.Add("@rart", OleDbType.VarWChar).Value = (object)(es.Berechnungsart ?? "");

                            c.Parameters.Add("@e1", OleDbType.Double).Value = R(es.Ladung_PV);
                            c.Parameters.Add("@e2", OleDbType.Double).Value = R(es.Ladung_BHKW);
                            c.Parameters.Add("@e3", OleDbType.Double).Value = R(es.Ladung_Netz);
                            c.Parameters.Add("@e4", OleDbType.Double).Value = R(es.Ladung_Gesamt);
                            c.Parameters.Add("@e5", OleDbType.Double).Value = R(es.Entladung_Gesamt);
                            c.Parameters.Add("@e6", OleDbType.Double).Value = R(es.Verluste_Gesamt);
                            c.Parameters.Add("@e7", OleDbType.Double).Value = R(es.Netzbezug_Mit);
                            c.Parameters.Add("@e8", OleDbType.Double).Value = R(es.Netzbezug_Ohne);
                            c.Parameters.Add("@e9", OleDbType.Double).Value = R(es.Einspeisung_Mit);
                            c.Parameters.Add("@e10", OleDbType.Double).Value = R(es.Einspeisung_Ohne);
                            c.Parameters.Add("@e11", OleDbType.Double).Value = R(es.Eigenverbrauchsquote);
                            c.Parameters.Add("@e12", OleDbType.Double).Value = R(es.Autarkiegrad);

                            c.Parameters.Add("@s1", OleDbType.Double).Value = R(es.Vollzyklen);
                            c.Parameters.Add("@s2", OleDbType.Double).Value = R(es.SoC_Min);
                            c.Parameters.Add("@s3", OleDbType.Double).Value = R(es.SoC_Mittel);
                            c.Parameters.Add("@s4", OleDbType.Double).Value = R(es.SoC_Max);
                            c.Parameters.Add("@s5", OleDbType.Double).Value = R(es.Zeitanteil_Untergrenze);
                            c.Parameters.Add("@s6", OleDbType.Double).Value = R(es.Zeitanteil_Obergrenze);
                            c.Parameters.Add("@s7", OleDbType.Double).Value = R(es.Zyklen_Hochrechnung);

                            c.Parameters.Add("@w1", OleDbType.Double).Value = R(es.Ertrag_Bezugsersparnis);
                            c.Parameters.Add("@w2", OleDbType.Double).Value = R(es.Ertrag_Verguetung_Entgangen);
                            c.Parameters.Add("@w3", OleDbType.Double).Value = R(es.Ertrag_Netzerloes);
                            c.Parameters.Add("@w4", OleDbType.Double).Value = R(es.Kosten_Ladung);
                            c.Parameters.Add("@w5", OleDbType.Double).Value = R(es.Ertrag_Leistungspreis);
                            c.Parameters.Add("@w6", OleDbType.Double).Value = R(es.Verschleisskosten);
                            c.Parameters.Add("@w7", OleDbType.Double).Value = R(es.Investition);
                            c.Parameters.Add("@w8", OleDbType.Double).Value = R(es.Annuitaet);
                            c.Parameters.Add("@w9", OleDbType.Double).Value = R(es.Jahresueberschuss);
                            c.Parameters.Add("@w10", OleDbType.Double).Value = R(es.Ertrag_Jahr1);
                            c.Parameters.Add("@w11", OleDbType.Double).Value = R(es.Ertrag_Aequivalent);
                            c.Parameters.Add("@w12", OleDbType.Double).Value = R(es.Amortisation_Statisch);
                            c.Parameters.Add("@w13", OleDbType.Double).Value = R(es.Amortisation_Dynamisch);
                            c.Parameters.Add("@w14", OleDbType.Double).Value = R(es.Kapitalwert);
                            c.Parameters.Add("@w15", OleDbType.VarWChar).Value = (object)(es.Preisversion ?? "");
                            c.ExecuteNonQuery();
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
                // PAKET 8 (Konzept 13.4): Save() ist die letzte Station des headless-Laufs
                // (SimulationRunner.SimuliereUndSpeichere) und damit die Stelle, an der
                // eine MessageBox einen unbeaufsichtigten Lauf noch NACH der Rechnung
                // hätte blockieren können.
                DataRepository.FehlerMelden("Fehler beim Speichern des Simulationsergebnisses: " + ex.Message);
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
                // ETAPPE E2: fehlt die Spalte (Ergebniszeile vor Schritt 18), liefert D() 0 -
                // die Wirtschaftlichkeit rechnet die Groesse dann selbst aus Stromproduktion
                // und installierter Leistung.
                b.VbhElektrisch = D(rb, SchemaKatalog.SPALTE_BHKW_VBH_ELEKTRISCH);

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
                        mo.VbhThermisch = D(rm, SchemaKatalog.SPALTE_MODUL_VBH_THERMISCH);
                        mo.VbhElektrisch = D(rm, SchemaKatalog.SPALTE_MODUL_VBH_ELEKTRISCH);
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
                // ETAPPE D4: D() liefert 0, wenn die Spalte fehlt oder NULL ist - genau
                // die Behandlung, die Bestandszeilen ohne Quellwärme brauchen.
                h.Quellwaerme = D(rh, SchemaKatalog.SPALTE_KESSEL_QUELLWAERME);

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

            // Detail: Pufferspeicher (Konzept 6.6). Tolerant gegen Datenbanken, auf denen
            // weder die Migration noch ein Save gelaufen ist - dann fehlt die Tabelle und
            // die Liste bleibt leer.
            //
            // WICHTIG: NICHT ueber DataRepository.GetDataTable. Die Methode wirft bei
            // einer fehlenden Tabelle nicht, sondern zeigt eine MessageBox und liefert
            // eine leere Tabelle zurueck - das try/catch hier lief also ins Leere und der
            // Anwender (bzw. der headless-Referenzlauf) bekam eine Fehlermeldung statt
            // der stillen Ruecklaufebene. PufferZeilenLesenStill greift mit eigener,
            // stiller Verbindung direkt zu und liefert null, wenn es die Tabelle nicht gibt.
            DataTable dsp = PufferZeilenLesenStill(m.ID);
            if (dsp != null)
                foreach (DataRow rsp in dsp.Rows)
                {
                    ErgebnisPufferspeicherModel sp = new ErgebnisPufferspeicherModel();
                    sp.ID_Pufferspeicher = I(rsp, "ID_Pufferspeicher");
                    sp.Bezeichner = S(rsp, "Bezeichner");
                    sp.Verwendung = S(rsp, "Verwendung");
                    sp.Q_max = D(rsp, "Q_max");
                    sp.Ladung_gesamt = D(rsp, "Ladung_gesamt");
                    sp.Entladung_gesamt = D(rsp, "Entladung_gesamt");
                    sp.Verluste_gesamt = D(rsp, "Verluste_gesamt");
                    sp.SOC_Ende = D(rsp, "SOC_Ende");
                    sp.SOC_Mittel = D(rsp, "SOC_Mittel");
                    sp.SOC_Max = D(rsp, "SOC_Max");
                    sp.Vollzyklen = D(rsp, "Vollzyklen");
                    m.Pufferspeicher.Add(sp);
                }

            // Detail: Stromspeicher (Fachkonzept Stromspeicher 7.1). Dieselbe stille
            // Ruecklaufebene wie beim Pufferspeicher darueber und aus demselben Grund:
            // Auf einer Datenbank vor Migrationsschritt 11c fehlt die Tabelle, und die
            // leere Liste ist der vorgesehene Normalfall - keine Fehlermeldung.
            DataTable dsp2 = StromspeicherZeilenLesenStill(m.ID);
            if (dsp2 != null)
                foreach (DataRow res in dsp2.Rows)
                {
                    ErgebnisStromspeicherModel es = new ErgebnisStromspeicherModel();
                    es.ID_Energieanlage = I(res, "ID_Energieanlage");
                    es.Bezeichner = S(res, "Bezeichner");
                    es.Betriebsart = S(res, "Betriebsart");
                    es.Berechnungsart = S(res, "Berechnungsart");

                    es.Ladung_PV = D(res, "Ladung_PV");
                    es.Ladung_BHKW = D(res, "Ladung_BHKW");
                    es.Ladung_Netz = D(res, "Ladung_Netz");
                    es.Ladung_Gesamt = D(res, "Ladung_Gesamt");
                    es.Entladung_Gesamt = D(res, "Entladung_Gesamt");
                    es.Verluste_Gesamt = D(res, "Verluste_Gesamt");
                    es.Netzbezug_Mit = D(res, "Netzbezug_Mit");
                    es.Netzbezug_Ohne = D(res, "Netzbezug_Ohne");
                    es.Einspeisung_Mit = D(res, "Einspeisung_Mit");
                    es.Einspeisung_Ohne = D(res, "Einspeisung_Ohne");
                    es.Eigenverbrauchsquote = D(res, "Eigenverbrauchsquote");
                    es.Autarkiegrad = D(res, "Autarkiegrad");

                    es.Vollzyklen = D(res, "Vollzyklen");
                    es.SoC_Min = D(res, "SoC_Min");
                    es.SoC_Mittel = D(res, "SoC_Mittel");
                    es.SoC_Max = D(res, "SoC_Max");
                    es.Zeitanteil_Untergrenze = D(res, "Zeitanteil_Untergrenze");
                    es.Zeitanteil_Obergrenze = D(res, "Zeitanteil_Obergrenze");
                    es.Zyklen_Hochrechnung = D(res, "Zyklen_Hochrechnung");

                    es.Ertrag_Bezugsersparnis = D(res, "Ertrag_Bezugsersparnis");
                    es.Ertrag_Verguetung_Entgangen = D(res, "Ertrag_Verguetung_Entgangen");
                    es.Ertrag_Netzerloes = D(res, "Ertrag_Netzerloes");
                    es.Kosten_Ladung = D(res, "Kosten_Ladung");
                    es.Ertrag_Leistungspreis = D(res, "Ertrag_Leistungspreis");
                    es.Verschleisskosten = D(res, "Verschleisskosten");
                    es.Investition = D(res, "Investition");
                    es.Annuitaet = D(res, "Annuitaet");
                    es.Jahresueberschuss = D(res, "Jahresueberschuss");
                    es.Ertrag_Jahr1 = D(res, "Ertrag_Jahr1");
                    es.Ertrag_Aequivalent = D(res, "Ertrag_Aequivalent");
                    es.Amortisation_Statisch = D(res, "Amortisation_Statisch");
                    es.Amortisation_Dynamisch = D(res, "Amortisation_Dynamisch");
                    es.Kapitalwert = D(res, "Kapitalwert");
                    es.Preisversion = S(res, "Preisversion");

                    m.Stromspeicher.Add(es);
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
            // ETAPPE E2: VbhElektrisch steht mit in dieser Liste, aus demselben Grund wie
            // die Brennstoffspalten — das INSERT der BHKW-Zeile führt sie namentlich auf.
            string[] spalten = { "Oelverbrauch", "Koks", "Rapsoelverbrauch", "Holzverbrauch", "Kohle",
                                 "Sonstigverbrauch", "Pellets", "TierischeFette",
                                 SchemaKatalog.SPALTE_BHKW_VBH_ELEKTRISCH };
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

                    // ETAPPE E2 — Rückfallebene zu Migrationsschritt 18. Dieselbe
                    // Begründung wie bei Quellwaerme: Das INSERT der Modulzeile führt die
                    // beiden Spalten NAMENTLICH auf; fehlen sie, scheitert nicht nur die
                    // neue Größe, sondern die ganze Modulzeile — und mit ihr der Lauf.
                    // Die Namen kommen aus SchemaKatalog, Migration und Rückfallebene
                    // führen keine zweite Liste.
                    ErgaenzeSpalte(conn, TAB_BHKW_MODUL, SchemaKatalog.SPALTE_MODUL_VBH_THERMISCH, "DOUBLE");
                    ErgaenzeSpalte(conn, TAB_BHKW_MODUL, SchemaKatalog.SPALTE_MODUL_VBH_ELEKTRISCH, "DOUBLE");
                }
            }
            catch { /* best effort - Spalten existieren dann ggf. schon */ }
        }

        /// <summary>
        /// ETAPPE D4 — Rückfallebene für die Ergebnisspalte
        /// <c>Tab_ErgebnisHeizkessel.Quellwaerme</c> (Quellwärme der Kaskade).
        ///
        /// Der reguläre Weg ist Schritt 10 der <see cref="SchemaMigration"/> beim
        /// Programmstart. Diese Vorsorge steht daneben, weil das INSERT der Kesselzeile
        /// die Spalte NAMENTLICH aufführt: Fehlt sie, scheiterte nicht nur die neue Größe,
        /// sondern die ganze Ergebniszeile — und mit ihr der Lauf. Dasselbe Muster und
        /// dieselbe Begründung wie bei den Brennstoffspalten des BHKW
        /// (<see cref="StelleBHKWSpaltenSicher"/>) und den Modulspalten.
        ///
        /// Der Spaltenname kommt aus <see cref="SchemaKatalog"/> — Migration und
        /// Rückfallebene führen keine zweite Liste.
        /// </summary>
        private static void StelleKesselSpaltenSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    ErgaenzeSpalte(conn, TAB_KESSEL, SchemaKatalog.SPALTE_KESSEL_QUELLWAERME, "DOUBLE");
                }
            }
            catch { /* best effort - Spalte existiert dann ggf. schon */ }
        }

        // ---------------------------------------------------------------------------
        // Tab_ErgebnisPufferspeicher (Konzept 6.6)
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Rückfallebene für Datenbanken, deren SchemaMigration (Schritt 3) noch nicht
        /// gelaufen ist: legt Tab_ErgebnisPufferspeicher samt Index und Löschweitergabe
        /// an. Muster ist der Ddl()-Helfer aus WirtschaftlichkeitCtrl - jeder Schritt
        /// einzeln abgesichert, damit ein Fehlschlag die übrigen nicht mitreißt.
        ///
        /// Duplikat-tolerant: Auf migrierten Datenbanken existieren Tabelle, Index und
        /// Constraint bereits; dann passiert hier nichts (Tabellenprüfung vorab, die
        /// beiden Folgeschritte laufen nur nach einer echten Neuanlage).
        ///
        /// WICHTIG: Fehlt die Constraint (z. B. weil sie an einer Altdatenbank nicht
        /// angelegt werden konnte), greift zusätzlich das explizite DELETE in Save.
        /// </summary>
        private static void StellePufferTabelleSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    if (!PufferTabelleVorhanden(conn))
                    {
                        // Spaltensatz identisch zu SchemaMigration.SQL_CREATE_ERGEBNISPUFFER.
                        try
                        {
                            Ddl(conn, "CREATE TABLE " + TAB_PUFFER + " (ID LONG NOT NULL PRIMARY KEY, " +
                                      "ID_Ergebnis LONG, ID_Pufferspeicher LONG, Bezeichner TEXT(255), " +
                                      "Verwendung TEXT(50), Q_max DOUBLE, Ladung_gesamt DOUBLE, " +
                                      "Entladung_gesamt DOUBLE, Verluste_gesamt DOUBLE, SOC_Ende DOUBLE, " +
                                      "SOC_Mittel DOUBLE, SOC_Max DOUBLE, Vollzyklen DOUBLE)");
                        }
                        catch { return; /* ohne Tabelle sind Index und Constraint sinnlos */ }
                    }

                    // Index und Löschweitergabe werden AUCH auf einer bereits vorhandenen
                    // Tabelle geprueft: eine Tabelle kann aus einem abgebrochenen Lauf
                    // dieser Rueckfallebene stammen (CREATE TABLE gelungen, Index oder
                    // Constraint nicht) oder aus einer Handanlage. Fehlt FK_ErgPuffer,
                    // gibt es keine Loeschweitergabe - genau der Waisenfall aus 6.6.
                    // Duplikat-tolerant: auf migrierten Datenbanken sind beide vorhanden,
                    // die Pruefung stellt das fest und es passiert nichts.
                    if (!IndexVorhanden(conn, TAB_PUFFER, "idx_ErgPuffer"))
                    {
                        try { Ddl(conn, "CREATE INDEX idx_ErgPuffer ON " + TAB_PUFFER + " (ID_Ergebnis)"); }
                        catch { }
                    }

                    // Dieselbe Löschweitergabe wie bei allen Geschwistertabellen (13.7).
                    if (!FremdschluesselVorhanden(conn, TAB_PUFFER, "ID_Ergebnis"))
                    {
                        // Waisen zuerst: Access weist ADD CONSTRAINT zurueck, solange
                        // Zeilen ohne gueltigen Kopf existieren. Genau die entstehen
                        // aber, wenn die Beziehung bisher fehlte - ohne dieses Delete
                        // liesse sich die Loeschweitergabe nie mehr nachziehen.
                        try
                        {
                            Ddl(conn, "DELETE FROM " + TAB_PUFFER + " WHERE ID_Ergebnis NOT IN " +
                                      "(SELECT ID FROM " + TAB_KOPF + ")");
                        }
                        catch { }

                        try
                        {
                            Ddl(conn, "ALTER TABLE " + TAB_PUFFER + " ADD CONSTRAINT FK_ErgPuffer " +
                                      "FOREIGN KEY (ID_Ergebnis) REFERENCES " + TAB_KOPF + " (ID) ON DELETE CASCADE");
                        }
                        catch { }
                    }
                }
            }
            catch { /* best effort - Save faengt einen echten Fehler ohnehin ab */ }
        }

        /// <summary>
        /// AP3 - Rueckfallebene fuer Tab_ErgebnisStromspeicher (Fachkonzept
        /// Stromspeicher 7.1) auf Datenbanken, deren SchemaMigration (Schritt 11c) noch
        /// nicht gelaufen ist. Aufbau, Begruendung und Duplikattoleranz wie bei
        /// <see cref="StellePufferTabelleSicher"/>; die Anweisungen kommen aus
        /// <see cref="SchemaMigration"/>, damit es keinen zweiten Spaltensatz gibt.
        /// </summary>
        private static void StelleStromspeicherTabelleSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    if (!TabelleVorhanden(conn, TAB_SP))
                    {
                        try { Ddl(conn, SchemaMigration.SQL_CREATE_ERGEBNISSTROMSPEICHER); }
                        catch { return; /* ohne Tabelle sind Index und Constraint sinnlos */ }
                    }

                    if (!IndexVorhanden(conn, TAB_SP, "idx_ErgStromspeicher"))
                    {
                        try { Ddl(conn, SchemaMigration.SQL_INDEX_ERGSTROMSPEICHER); }
                        catch { }
                    }

                    if (!FremdschluesselVorhanden(conn, TAB_SP, "ID_Ergebnis"))
                    {
                        // Waisen zuerst - dieselbe Reihenfolge und derselbe Grund wie
                        // beim Pufferspeicher: Access weist ADD CONSTRAINT zurueck,
                        // solange Zeilen ohne gueltigen Kopf existieren.
                        try
                        {
                            Ddl(conn, "DELETE FROM " + TAB_SP + " WHERE ID_Ergebnis NOT IN " +
                                      "(SELECT ID FROM " + TAB_KOPF + ")");
                        }
                        catch { }

                        try { Ddl(conn, SchemaMigration.SQL_FK_ERGSTROMSPEICHER); }
                        catch { }
                    }
                }
            }
            catch { /* best effort - Save faengt einen echten Fehler ohnehin ab */ }
        }

        private static bool PufferTabelleVorhanden(OleDbConnection conn)
        {
            return TabelleVorhanden(conn, TAB_PUFFER);
        }

        /// <summary>Gibt es die Tabelle? (Muster PufferTabelleVorhanden, nur mit Tabellennamen)</summary>
        private static bool TabelleVorhanden(OleDbConnection conn, string tabelle)
        {
            DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                new object[] { null, null, tabelle, "TABLE" });
            return schema != null && schema.Rows.Count > 0;
        }

        /// <summary>Gibt es den benannten Index auf der Tabelle? (Muster Migrationslauf.IndexVorhanden)</summary>
        private static bool IndexVorhanden(OleDbConnection conn, string tabelle, string index)
        {
            try
            {
                DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Indexes,
                    new object[] { null, null, null, null, tabelle });
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (string.Equals(r["INDEX_NAME"].ToString(), index, StringComparison.OrdinalIgnoreCase))
                            return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Gibt es auf der Tabelle einen Fremdschluessel ueber die genannte Spalte?
        /// Geprueft wird die Spalte, nicht der Name der Constraint - eine von Hand oder
        /// von der Migration angelegte Beziehung kann anders heissen, erfuellt aber
        /// denselben Zweck; ein zweites ADD CONSTRAINT wuerde nur scheitern.
        /// </summary>
        private static bool FremdschluesselVorhanden(OleDbConnection conn, string tabelle, string spalte)
        {
            try
            {
                DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null);
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (string.Equals(Txt(r, "FK_TABLE_NAME"), tabelle, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(Txt(r, "FK_COLUMN_NAME"), spalte, StringComparison.OrdinalIgnoreCase))
                            return true;
            }
            catch { }
            return false;
        }

        private static string Txt(DataRow r, string spalte)
        {
            return (r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value) ? r[spalte].ToString() : "";
        }

        /// <summary>
        /// Loescht die Pufferzeilen aller Ergebniskoepfe eines Projekts innerhalb der
        /// laufenden Transaktion. Fehlt die Tabelle, gibt es auch keine Zeilen -
        /// der Aufrufer soll deswegen nicht abbrechen.
        /// </summary>
        private static void PufferzeilenLoeschen(OleDbConnection conn, OleDbTransaction trans, int idProjekt)
        {
            DetailzeilenLoeschen(conn, trans, TAB_PUFFER, idProjekt);
        }

        /// <summary>
        /// Wie <see cref="PufferzeilenLoeschen"/>, nur mit dem Tabellennamen als
        /// Parameter - AP3 braucht denselben Vorablauf fuer Tab_ErgebnisStromspeicher.
        /// </summary>
        private static void DetailzeilenLoeschen(OleDbConnection conn, OleDbTransaction trans,
                                                 string tabelle, int idProjekt)
        {
            try
            {
                using (OleDbCommand c = new OleDbCommand(
                    "DELETE FROM " + tabelle + " WHERE ID_Ergebnis IN " +
                    "(SELECT ID FROM " + TAB_KOPF + " WHERE ID_Projekt = ?)", conn, trans))
                {
                    c.Parameters.Add("@p", OleDbType.Integer).Value = idProjekt;
                    c.ExecuteNonQuery();
                }
            }
            catch { /* Tabelle (noch) nicht vorhanden - dann gibt es auch keine Waisen */ }
        }

        /// <summary>
        /// Liest die Pufferzeilen eines Ergebniskopfes ueber eine EIGENE, stille
        /// OleDb-Verbindung. Rueckgabe null, wenn die Tabelle fehlt oder der Zugriff
        /// scheitert.
        ///
        /// Bewusst nicht ueber <c>DataRepository.GetDataTable</c>: das zeigt bei einem
        /// Fehler eine MessageBox und liefert eine leere Tabelle statt zu werfen - eine
        /// nicht migrierte Datenbank haette damit eine Fehlermeldung erzeugt, obwohl die
        /// leere Liste der vorgesehene Normalfall ist (Muster: WaermequelleClass.SkalarStill).
        /// </summary>
        public static DataTable PufferZeilenLesenStill(int idErgebnis)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    if (!PufferTabelleVorhanden(conn)) return null;

                    DataTable dt = new DataTable();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "SELECT * FROM " + TAB_PUFFER + " WHERE ID_Ergebnis = ? ORDER BY ID", conn))
                    {
                        cmd.Parameters.Add("@e", OleDbType.Integer).Value = idErgebnis;
                        using (OleDbDataAdapter ad = new OleDbDataAdapter(cmd)) ad.Fill(dt);
                    }
                    return dt;
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// Liest die Stromspeicherzeilen eines Ergebniskopfes ueber eine EIGENE, stille
        /// OleDb-Verbindung - dieselbe Bauform und dieselbe Begruendung wie
        /// <see cref="PufferZeilenLesenStill"/>. Rueckgabe null, wenn die Tabelle fehlt
        /// oder der Zugriff scheitert.
        /// </summary>
        private static DataTable StromspeicherZeilenLesenStill(int idErgebnis)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    if (!TabelleVorhanden(conn, TAB_SP)) return null;

                    DataTable dt = new DataTable();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "SELECT * FROM " + TAB_SP + " WHERE ID_Ergebnis = ? ORDER BY ID", conn))
                    {
                        cmd.Parameters.Add("@e", OleDbType.Integer).Value = idErgebnis;
                        using (OleDbDataAdapter ad = new OleDbDataAdapter(cmd)) ad.Fill(dt);
                    }
                    return dt;
                }
            }
            catch { return null; }
        }

        private static void Ddl(OleDbConnection conn, string sql)
        {
            using (OleDbCommand cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
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
