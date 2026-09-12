using System;
using System.Collections.Generic;
using System.Data;

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
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // Defensiv VOR dem Kopf-Delete - dieselbe Begruendung wie in Save:
                    // auf einer Datenbank ohne FK_ErgPuffer gibt es keine Loeschweitergabe,
                    // die Pufferzeilen blieben als Waisen stehen und zeigten wegen der
                    // MAX(ID)+1-Vergabe spaeter auf fremde Laeufe (Konzept 6.6).
                    PufferzeilenLoeschen(v, idProjekt);
                    DetailzeilenLoeschen(v, TAB_SP, idProjekt);

                    //    Loeschweitergabe raeumt alle Detailtabellen automatisch mit ab.
                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@p", DbParamTyp.Integer) { Wert = idProjekt });
                        v.Ausfuehren("DELETE FROM " + TAB_KOPF + " WHERE ID_Projekt = ?", p.ToArray());
                    }
                    v.Commit();
                    return 0;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
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
            }
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
            StelleKanalSpaltenSicher();     // Ergebnisspalten je Kanal (Schritt 52, Paket E1)

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

            using (DbVorgang v = DataRepository.Vorgang())
            {
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
                    PufferzeilenLoeschen(v, m.ID_Projekt);
                    DetailzeilenLoeschen(v, TAB_SP, m.ID_Projekt);

                    //    Zusaetzlich alle Waisen abraeumen, deren Kopf nicht mehr existiert.
                    //    Notwendig, weil ein frueherer Kopf-Delete OHNE Loeschweitergabe
                    //    Zeilen hinterlassen haben kann: die Kopf-ID wird per MAX(ID)+1
                    //    wiederverwendet, die alte Zeile haengt danach an einem FREMDEN
                    //    Projekt und faelscht dessen Ergebnisausweis (Konzept 6.6).
                    try
                    {
                        v.Ausfuehren("DELETE FROM " + TAB_PUFFER + " WHERE ID_Ergebnis NOT IN " +
                            "(SELECT ID FROM " + TAB_KOPF + ")");
                    }
                    catch { /* Tabelle (noch) nicht vorhanden - dann gibt es auch keine Waisen */ }

                    //    Dieselbe Waisenpruefung fuer die Stromspeicherzeilen (AP3).
                    try
                    {
                        v.Ausfuehren("DELETE FROM " + TAB_SP + " WHERE ID_Ergebnis NOT IN " +
                            "(SELECT ID FROM " + TAB_KOPF + ")");
                    }
                    catch { /* Tabelle (noch) nicht vorhanden - dann gibt es auch keine Waisen */ }

                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@p", DbParamTyp.Integer) { Wert = m.ID_Projekt });
                        // BEFUND B2 (S7): Hier stand bis 02.09.2026 das Jet-Idiom
                        // "DELETE <feld> FROM <tabelle>". ACE hat den Feldnamen stillschweigend
                        // verworfen, SQLite meldet 'near "ID_Projekt": syntax error' - der Fehler
                        // fiel in die Transaktion von Save(), rollte sie zurueck und liess jeden
                        // Referenzlauf ohne Ergebnis enden. Richtige Form steht zwei Bloecke
                        // weiter oben (Delete(int), Zeile 60) und in KenndatenCtrl/WErzeugerCtrl.
                        v.Ausfuehren("DELETE FROM " + TAB_KOPF + " WHERE ID_Projekt = ?", p.ToArray());
                    }

                    // 2. Kopf schreiben.
                    int kopfId = NextId(v, TAB_KOPF);
                    string sqlKopf = "INSERT INTO " + TAB_KOPF + " (" +
                        "ID, ID_Projekt, Bezeichner, Zeitstempel, ID_Klimaregion, " +
                        "Sim_Energiebedarf, Sim_Waermepumpe, Sim_Heizkessel, Sim_Solarthermie, Sim_BHKW, Sim_PV, Sim_Stromspeicher) " +
                        "VALUES (?,?,?,?,?, ?,?,?,?,?,?,?)";
                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = kopfId });
                        p.Add(new DbParam("@proj", DbParamTyp.Integer) { Wert = m.ID_Projekt });
                        p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = (object)(m.Bezeichner ?? "") });
                        p.Add(new DbParam("@zeit", DbParamTyp.Date) { Wert = m.Zeitstempel });
                        p.Add(new DbParam("@klima", DbParamTyp.Integer) { Wert = m.ID_Klimaregion });
                        p.Add(new DbParam("@s1", DbParamTyp.Boolean) { Wert = m.Sim_Energiebedarf });
                        p.Add(new DbParam("@s2", DbParamTyp.Boolean) { Wert = m.Sim_Waermepumpe });
                        p.Add(new DbParam("@s3", DbParamTyp.Boolean) { Wert = m.Sim_Heizkessel });
                        p.Add(new DbParam("@s4", DbParamTyp.Boolean) { Wert = m.Sim_Solarthermie });
                        p.Add(new DbParam("@s5", DbParamTyp.Boolean) { Wert = m.Sim_BHKW });
                        p.Add(new DbParam("@s6", DbParamTyp.Boolean) { Wert = m.Sim_PV });
                        p.Add(new DbParam("@s7", DbParamTyp.Boolean) { Wert = m.Sim_Stromspeicher });
                        v.Ausfuehren(sqlKopf, p.ToArray());
                    }

                    // 3. Detail: Energiebedarf.
                    if (m.Energiebedarf != null)
                    {
                        int eId = NextId(v, TAB_ENERGIE);
                        // PAKET E1: die drei Kanalspalten stehen als LETZTE - ALTER TABLE
                        // haengt sie in Access hinten an, und die Parameterreihenfolge folgt
                        // der Spaltenliste (dasselbe Muster wie Quellwaerme in Etappe D4).
                        string sql = "INSERT INTO " + TAB_ENERGIE + " (" +
                            "ID, ID_Ergebnis, Waermebedarf_Gesamt, Waermelast_Max, Strombedarf_Gesamt, Strombedarf_Max, " +
                            "Waermerestbedarf, Stromrestbedarf, " +
                            SchemaKatalog.SPALTE_BEDARF_HEIZUNG + ", " +
                            SchemaKatalog.SPALTE_BEDARF_BRAUCHWASSER + ", " +
                            SchemaKatalog.SPALTE_BEDARF_PROZESS + ") " +
                            "VALUES (?,?,?,?,?,?,?,?, ?,?,?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = eId });
                            p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                            p.Add(new DbParam("@a1", DbParamTyp.Double) { Wert = R(m.Energiebedarf.Waermebedarf_Gesamt) });
                            p.Add(new DbParam("@a2", DbParamTyp.Double) { Wert = R(m.Energiebedarf.Waermelast_Max) });
                            p.Add(new DbParam("@a3", DbParamTyp.Double) { Wert = R(m.Energiebedarf.Strombedarf_Gesamt) });
                            p.Add(new DbParam("@a4", DbParamTyp.Double) { Wert = R(m.Energiebedarf.Strombedarf_Max) });
                            p.Add(new DbParam("@a5", DbParamTyp.Double) { Wert = R(m.Energiebedarf.Waermerestbedarf) });
                            p.Add(new DbParam("@a6", DbParamTyp.Double) { Wert = R(m.Energiebedarf.Stromrestbedarf) });
                            KanalParameter(p, m.Energiebedarf.Waermebedarf_Kanal);
                            v.Ausfuehren(sql, p.ToArray());
                        }
                    }

                    // 4. Detail: Waermepumpe (+ Modulliste).
                    if (m.Waermepumpe != null)
                    {
                        int wpId = NextId(v, TAB_WP);
                        string sql = "INSERT INTO " + TAB_WP + " (" +
                            "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Waermeproduktion_WP, Stromverbrauch_WP, " +
                            "Stromverbrauch_Heizstab, Kapazitaet_Pufferspeicher, Min_Spitzenkesselleistung, " +
                            "Waermebedarfsdeckung, Vollbenutzungsstunden, Bivalenzpunkt, " +
                            SchemaKatalog.SPALTE_DECKUNG_HEIZUNG + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_BRAUCHWASSER + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_PROZESS + ") " +
                            "VALUES (?,?,?,?,?,?, ?,?,?, ?,?,?, ?,?,?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = wpId });
                            p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                            p.Add(new DbParam("@a1", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Waermebedarf) });
                            p.Add(new DbParam("@a2", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Restwaermebedarf) });
                            p.Add(new DbParam("@a3", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Waermeproduktion_WP) });
                            p.Add(new DbParam("@a4", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Stromverbrauch_WP) });
                            p.Add(new DbParam("@a5", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Stromverbrauch_Heizstab) });
                            p.Add(new DbParam("@a6", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Kapazitaet_Pufferspeicher) });
                            p.Add(new DbParam("@a7", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Min_Spitzenkesselleistung) });
                            p.Add(new DbParam("@a8", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Waermebedarfsdeckung) });
                            p.Add(new DbParam("@a9", DbParamTyp.Double) { Wert = R(m.Waermepumpe.Vollbenutzungsstunden) });
                            p.Add(new DbParam("@a10", DbParamTyp.Double) { Wert = m.Waermepumpe.Bivalenzpunkt.HasValue ? (object)R(m.Waermepumpe.Bivalenzpunkt.Value) : DBNull.Value });
                            KanalParameter(p, m.Waermepumpe.Deckung_Kanal);
                            v.Ausfuehren(sql, p.ToArray());
                        }

                        if (m.Waermepumpe.Module != null && m.Waermepumpe.Module.Count > 0)
                        {
                            int modId = NextId(v, TAB_WP_MODUL);
                            string sqlM = "INSERT INTO " + TAB_WP_MODUL + " (" +
                                "ID, ID_ErgebnisWaermepumpe, Modul, Leistung, Waermeproduktion, Stromverbrauch, Heizstab, Betriebsstunden) " +
                                "VALUES (?,?,?,?,?,?,?,?)";
                            foreach (ErgebnisWaermepumpeModulModel mo in m.Waermepumpe.Module)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = modId++ });
                                    p.Add(new DbParam("@wp", DbParamTyp.Integer) { Wert = wpId });
                                    p.Add(new DbParam("@mod", DbParamTyp.VarWChar) { Wert = (object)(mo.Modul ?? "") });
                                    p.Add(new DbParam("@l", DbParamTyp.Double) { Wert = R(mo.Leistung) });
                                    p.Add(new DbParam("@w", DbParamTyp.Double) { Wert = R(mo.Waermeproduktion) });
                                    p.Add(new DbParam("@s", DbParamTyp.Double) { Wert = R(mo.Stromverbrauch) });
                                    p.Add(new DbParam("@h", DbParamTyp.Double) { Wert = R(mo.Heizstab) });
                                    p.Add(new DbParam("@b", DbParamTyp.Double) { Wert = R(mo.Betriebsstunden) });
                                    v.Ausfuehren(sqlM, p.ToArray());
                                }
                            }
                        }
                    }

                    // 5. Detail: BHKW (+ Modulliste).
                    if (m.BHKW != null)
                    {
                        int bId = NextId(v, TAB_BHKW);
                        string sql = "INSERT INTO " + TAB_BHKW + " (" +
                            "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Strombedarf, Reststrombedarf, " +
                            "Waermeproduktion, Waermeueberschuss, Stromproduktion, Betriebsstunden_Gesamt, " +
                            "Betriebsstunden_Durchschnitt, Waermebedarfsdeckung, Strombedarfsdeckung, " +
                            "Gasverbrauch, Oelverbrauch, Koks, Rapsoelverbrauch, Holzverbrauch, Kohle, " +
                            "Sonstigverbrauch, Pellets, TierischeFette, " +
                            SchemaKatalog.SPALTE_BHKW_VBH_ELEKTRISCH + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_HEIZUNG + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_BRAUCHWASSER + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_PROZESS + ") " +
                            "VALUES (?,?,?,?,?,?, ?,?,?,?, ?,?,?, ?,?,?,?,?,?, ?,?,?, ?, ?,?,?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = bId });
                            p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                            p.Add(new DbParam("@a1", DbParamTyp.Double) { Wert = R(m.BHKW.Waermebedarf) });
                            p.Add(new DbParam("@a2", DbParamTyp.Double) { Wert = R(m.BHKW.Restwaermebedarf) });
                            p.Add(new DbParam("@a3", DbParamTyp.Double) { Wert = R(m.BHKW.Strombedarf) });
                            p.Add(new DbParam("@a4", DbParamTyp.Double) { Wert = R(m.BHKW.Reststrombedarf) });
                            p.Add(new DbParam("@a5", DbParamTyp.Double) { Wert = R(m.BHKW.Waermeproduktion) });
                            p.Add(new DbParam("@a6", DbParamTyp.Double) { Wert = R(m.BHKW.Waermeueberschuss) });
                            p.Add(new DbParam("@a7", DbParamTyp.Double) { Wert = R(m.BHKW.Stromproduktion) });
                            p.Add(new DbParam("@a8", DbParamTyp.Double) { Wert = R(m.BHKW.Betriebsstunden_Gesamt) });
                            p.Add(new DbParam("@a9", DbParamTyp.Double) { Wert = R(m.BHKW.Betriebsstunden_Durchschnitt) });
                            p.Add(new DbParam("@a10", DbParamTyp.Double) { Wert = R(m.BHKW.Waermebedarfsdeckung) });
                            p.Add(new DbParam("@a11", DbParamTyp.Double) { Wert = R(m.BHKW.Strombedarfsdeckung) });
                            p.Add(new DbParam("@a12", DbParamTyp.Double) { Wert = R(m.BHKW.Gasverbrauch) });
                            p.Add(new DbParam("@a13", DbParamTyp.Double) { Wert = R(m.BHKW.Oelverbrauch) });
                            p.Add(new DbParam("@a14", DbParamTyp.Double) { Wert = R(m.BHKW.Koks) });
                            p.Add(new DbParam("@a15", DbParamTyp.Double) { Wert = R(m.BHKW.Rapsoelverbrauch) });
                            p.Add(new DbParam("@a16", DbParamTyp.Double) { Wert = R(m.BHKW.Holzverbrauch) });
                            p.Add(new DbParam("@a17", DbParamTyp.Double) { Wert = R(m.BHKW.Kohle) });
                            p.Add(new DbParam("@a18", DbParamTyp.Double) { Wert = R(m.BHKW.Sonstigverbrauch) });
                            p.Add(new DbParam("@a19", DbParamTyp.Double) { Wert = R(m.BHKW.Pellets) });
                            p.Add(new DbParam("@a20", DbParamTyp.Double) { Wert = R(m.BHKW.TierischeFette) });
                            // ETAPPE E2: leistungsgewichtete elektrische Vollbenutzungsstunden.
                            p.Add(new DbParam("@a21", DbParamTyp.Double) { Wert = R(m.BHKW.VbhElektrisch) });
                            KanalParameter(p, m.BHKW.Deckung_Kanal);
                            v.Ausfuehren(sql, p.ToArray());
                        }

                        if (m.BHKW.Module != null && m.BHKW.Module.Count > 0)
                        {
                            // Dominanten Brennstoff + Gesamtverbrauch einmal bestimmen; der Verbrauch
                            // wird anteilig nach Waermeproduktion auf die Module verteilt (Summe = Gesamt).
                            string bhArt; double bhVerbrauch;
                            BHKWBrennstoff(m.BHKW, out bhArt, out bhVerbrauch);
                            double basis = 0;
                            foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module) basis += mo.Waermeproduktion;

                            // ETAPPE B3 Paket b: Hilfsenergie je Modulzeile [MWh/a] aus
                            // Hilfsenergie_Anteil der Anlage x Brennstoff DIESER Zeile
                            // (HilfsstromRechner - die eine Formel, die auch die
                            // Wirtschaftlichkeit fuer die KWKG-Nettomenge verwendet). Die
                            // Bemessungsgroesse ist derselbe anteilig verteilte Brennstoff,
                            // der unten als Verbrauch geschrieben wird; mo.Verbrauch ist an
                            // dieser Stelle noch leer (es fuellt ihn erst der Leseweg).
                            var bhNamen = new string[m.BHKW.Module.Count];
                            var bhBrennstoff = new double[m.BHKW.Module.Count];
                            for (int ix = 0; ix < m.BHKW.Module.Count; ix++)
                            {
                                ErgebnisBHKWModulModel mx = m.BHKW.Module[ix];
                                double anteilX = basis > 0 ? mx.Waermeproduktion / basis
                                                           : 1.0 / m.BHKW.Module.Count;
                                bhNamen[ix] = mx.Modul ?? "";
                                bhBrennstoff[ix] = bhArt != null ? bhVerbrauch * anteilX : 0;
                            }
                            double[] bhHilfsenergie = HilfsstromRechner.JeModul(
                                m.ID_Projekt, WizardItemClass.BHKW_TYP, bhNamen, bhBrennstoff);
                            int bhIndex = 0;

                            int modId = NextId(v, TAB_BHKW_MODUL);
                            string sqlM = "INSERT INTO " + TAB_BHKW_MODUL + " (" +
                                "ID, ID_ErgebnisBHKW, Modul, Waermeproduktion, Stromproduktion, Brennstoff, Verbrauch, carrier_id, " +
                                SchemaKatalog.SPALTE_MODUL_VBH_THERMISCH + ", " +
                                SchemaKatalog.SPALTE_MODUL_VBH_ELEKTRISCH + ", " +
                                SchemaKatalog.SPALTE_MODUL_HILFSENERGIE + ") " +
                                "VALUES (?,?,?,?,?,?,?,?,?,?,?)";
                            foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module)
                            {
                                double anteil = basis > 0 ? mo.Waermeproduktion / basis : 1.0 / m.BHKW.Module.Count;
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = modId++ });
                                    p.Add(new DbParam("@bh", DbParamTyp.Integer) { Wert = bId });
                                    p.Add(new DbParam("@mod", DbParamTyp.VarWChar) { Wert = (object)(mo.Modul ?? "") });
                                    p.Add(new DbParam("@w", DbParamTyp.Double) { Wert = R(mo.Waermeproduktion) });
                                    p.Add(new DbParam("@s", DbParamTyp.Double) { Wert = R(mo.Stromproduktion) });
                                    if (bhArt != null)
                                    {
                                        p.Add(new DbParam("@b", DbParamTyp.VarWChar) { Wert = bhArt });
                                        p.Add(new DbParam("@v", DbParamTyp.Double) { Wert = R(bhVerbrauch * anteil) });
                                    }
                                    else
                                    {
                                        p.Add(new DbParam("@b", DbParamTyp.VarWChar) { Wert = DBNull.Value });
                                        p.Add(new DbParam("@v", DbParamTyp.Double) { Wert = DBNull.Value });
                                    }
                                    p.Add(new DbParam("@ca", DbParamTyp.Integer) { Wert = mo.CarrierId > 0 ? (object)mo.CarrierId : DBNull.Value });
                                    // ETAPPE E2 (L6): thermische und elektrische Vbh je Modul.
                                    // Beide werden IMMER geschrieben - auch die 0. Sonst waere
                                    // "nicht erhoben" (NULL) von "erhoben und null" nicht mehr
                                    // unterscheidbar; dieselbe Begruendung wie bei Quellwaerme.
                                    p.Add(new DbParam("@vth", DbParamTyp.Double) { Wert = R(mo.VbhThermisch) });
                                    p.Add(new DbParam("@vel", DbParamTyp.Double) { Wert = R(mo.VbhElektrisch) });
                                    // ETAPPE B3 Paket a/b: Hilfsenergie [MWh/a]. Paket b
                                    // bildet den Wert (Anteil der Anlage x Brennstoff dieser
                                    // Zeile); ohne gepflegten Anteil bleibt es bei 0.
                                    // Geschrieben wird sie IMMER - auch die 0, aus derselben
                                    // Begruendung wie bei den Vbh: sonst waere "erhoben und
                                    // null" von "nicht erhoben" (NULL) nicht unterscheidbar.
                                    // Das Modell traegt den Wert mit, damit ein Ergebnis im
                                    // Speicher dieselbe Auskunft gibt wie die Zeile in der DB.
                                    mo.Hilfsenergie = bhIndex < bhHilfsenergie.Length
                                                    ? bhHilfsenergie[bhIndex] : 0;
                                    bhIndex++;
                                    p.Add(new DbParam("@hen", DbParamTyp.Double) { Wert = R(mo.Hilfsenergie) });
                                    v.Ausfuehren(sqlM, p.ToArray());
                                }
                            }
                        }
                    }

                    // 6. Detail: Heizkessel (+ Modulliste).
                    if (m.Heizkessel != null)
                    {
                        int hId = NextId(v, TAB_KESSEL);
                        // ETAPPE D4: Quellwaerme als LETZTE Spalte - ALTER TABLE haengt sie in
                        // Access hinten an, und die Parameterreihenfolge folgt der Spaltenliste.
                        string sql = "INSERT INTO " + TAB_KESSEL + " (" +
                            "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Waermeproduktion, Strombedarf, " +
                            "Reststrombedarf, Waermebedarfsdeckung, Stromverbrauch, Maximale_Kesselleistung, Gasspitze, " +
                            "Gasverbrauch, Oelverbrauch, Koks, Rapsoelverbrauch, Holzverbrauch, Kohle, " +
                            "Sonstigverbrauch, Pellets, TierischeFette, " +
                            SchemaKatalog.SPALTE_KESSEL_QUELLWAERME + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_HEIZUNG + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_BRAUCHWASSER + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_PROZESS + ") " +
                            "VALUES (?,?,?,?,?,?, ?,?,?,?,?, ?,?,?,?,?,?, ?,?,?,?, ?,?,?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = hId });
                            p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                            p.Add(new DbParam("@a1", DbParamTyp.Double) { Wert = R(m.Heizkessel.Waermebedarf) });
                            p.Add(new DbParam("@a2", DbParamTyp.Double) { Wert = R(m.Heizkessel.Restwaermebedarf) });
                            p.Add(new DbParam("@a3", DbParamTyp.Double) { Wert = R(m.Heizkessel.Waermeproduktion) });
                            p.Add(new DbParam("@a4", DbParamTyp.Double) { Wert = R(m.Heizkessel.Strombedarf) });
                            p.Add(new DbParam("@a5", DbParamTyp.Double) { Wert = R(m.Heizkessel.Reststrombedarf) });
                            p.Add(new DbParam("@a6", DbParamTyp.Double) { Wert = R(m.Heizkessel.Waermebedarfsdeckung) });
                            p.Add(new DbParam("@a7", DbParamTyp.Double) { Wert = R(m.Heizkessel.Stromverbrauch) });
                            p.Add(new DbParam("@a8", DbParamTyp.Double) { Wert = R(m.Heizkessel.Maximale_Kesselleistung) });
                            p.Add(new DbParam("@a9", DbParamTyp.Double) { Wert = R(m.Heizkessel.Gasspitze) });
                            p.Add(new DbParam("@a10", DbParamTyp.Double) { Wert = R(m.Heizkessel.Gasverbrauch) });
                            p.Add(new DbParam("@a11", DbParamTyp.Double) { Wert = R(m.Heizkessel.Oelverbrauch) });
                            p.Add(new DbParam("@a12", DbParamTyp.Double) { Wert = R(m.Heizkessel.Koks) });
                            p.Add(new DbParam("@a13", DbParamTyp.Double) { Wert = R(m.Heizkessel.Rapsoelverbrauch) });
                            p.Add(new DbParam("@a14", DbParamTyp.Double) { Wert = R(m.Heizkessel.Holzverbrauch) });
                            p.Add(new DbParam("@a15", DbParamTyp.Double) { Wert = R(m.Heizkessel.Kohle) });
                            p.Add(new DbParam("@a16", DbParamTyp.Double) { Wert = R(m.Heizkessel.Sonstigverbrauch) });
                            p.Add(new DbParam("@a17", DbParamTyp.Double) { Wert = R(m.Heizkessel.Pellets) });
                            p.Add(new DbParam("@a18", DbParamTyp.Double) { Wert = R(m.Heizkessel.TierischeFette) });
                            p.Add(new DbParam("@a19", DbParamTyp.Double) { Wert = R(m.Heizkessel.Quellwaerme) });
                            KanalParameter(p, m.Heizkessel.Deckung_Kanal);
                            v.Ausfuehren(sql, p.ToArray());
                        }

                        if (m.Heizkessel.Module != null && m.Heizkessel.Module.Count > 0)
                        {
                            // ETAPPE B3 Paket b: Hilfsenergie des Kessels - dieselbe Formel
                            // wie beim BHKW. Die Endenergie kommt aus
                            // HilfsstromRechner.KesselBrennstoffMWh: Verbrauch, sofern
                            // gesetzt, sonst die Rueckrechnung Waerme / Nutzungsgrad (Paket a
                            // hat begruendet, warum der Rechenkern Verbrauch nie fuellt).
                            //
                            // KEINE Strommengenwirkung: Ein Kessel erzeugt keinen Strom, sein
                            // Hilfsstrom mindert also weder eine KWKG-Nettoerzeugung noch eine
                            // Steuerbemessung. Der Wert ist hier reiner Ausweis fuer Bericht
                            // und Konzept 5.2 - die Kostenwirkung laeuft ueber die
                            // Betriebskostenposition, die Mengenwirkung kaeme erst mit einer
                            // spaeteren Etappe (Strombilanz der Hilfsantriebe).
                            var hkNamen = new string[m.Heizkessel.Module.Count];
                            var hkBrennstoff = new double[m.Heizkessel.Module.Count];
                            for (int ix = 0; ix < m.Heizkessel.Module.Count; ix++)
                            {
                                ErgebnisHeizkesselModulModel mx = m.Heizkessel.Module[ix];
                                hkNamen[ix] = mx.Modul ?? "";
                                hkBrennstoff[ix] = HilfsstromRechner.KesselBrennstoffMWh(mx);
                            }
                            double[] hkHilfsenergie = HilfsstromRechner.JeModul(
                                m.ID_Projekt, WizardItemClass.KESSEL_TYP, hkNamen, hkBrennstoff);
                            int hkIndex = 0;

                            int modId = NextId(v, TAB_KESSEL_MODUL);
                            // Befund B3 behoben: Waermeproduktion wird persistiert; Verbrauch gerundet;
                            // Parametername @g war doppelt vergeben.
                            string sqlM = "INSERT INTO " + TAB_KESSEL_MODUL + " (" +
                                "ID, ID_ErgebnisHeizkessel, Modul, Waerme_Gas, Waerme_Oel, Waermeproduktion, " +
                                "Brennstoff, Verbrauch, Jahresnutzungsgrad, carrier_id, " +
                                SchemaKatalog.SPALTE_MODUL_HILFSENERGIE + ") " +
                                "VALUES (?,?,?,?,?,?,?,?,?,?,?)";
                            foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = modId++ });
                                    p.Add(new DbParam("@hk", DbParamTyp.Integer) { Wert = hId });
                                    p.Add(new DbParam("@mod", DbParamTyp.VarWChar) { Wert = (object)(mo.Modul ?? "") });
                                    p.Add(new DbParam("@g", DbParamTyp.Double) { Wert = R(mo.Waerme_Gas) });
                                    p.Add(new DbParam("@o", DbParamTyp.Double) { Wert = R(mo.Waerme_Oel) });
                                    p.Add(new DbParam("@w", DbParamTyp.Double) { Wert = R(mo.Waermeproduktion) });
                                    p.Add(new DbParam("@b", DbParamTyp.VarWChar) { Wert = (object)(mo.Brennstoff ?? "") });
                                    p.Add(new DbParam("@v", DbParamTyp.Double) { Wert = R(mo.Verbrauch) });
                                    p.Add(new DbParam("@j", DbParamTyp.Double) { Wert = R(mo.Jahresnutzungsgrad) });
                                    p.Add(new DbParam("@ca", DbParamTyp.Integer) { Wert = mo.CarrierId > 0 ? (object)mo.CarrierId : DBNull.Value });
                                    // ETAPPE B3 Paket a/b: Hilfsenergie des Kessels -
                                    // Begruendung wortgleich zur BHKW-Modulzeile weiter oben.
                                    mo.Hilfsenergie = hkIndex < hkHilfsenergie.Length
                                                    ? hkHilfsenergie[hkIndex] : 0;
                                    hkIndex++;
                                    p.Add(new DbParam("@hen", DbParamTyp.Double) { Wert = R(mo.Hilfsenergie) });
                                    v.Ausfuehren(sqlM, p.ToArray());
                                }
                            }
                        }
                    }

                    // 7. Detail: Solarthermie (+ Kollektorliste).
                    if (m.Solarthermie != null)
                    {
                        int sId = NextId(v, TAB_SOLAR);
                        string sql = "INSERT INTO " + TAB_SOLAR + " (" +
                            "ID, ID_Ergebnis, Waermebedarf, Restwaermebedarf, Waermeproduktion, Waermebedarfsdeckung, Ueberschuss, " +
                            SchemaKatalog.SPALTE_DECKUNG_HEIZUNG + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_BRAUCHWASSER + ", " +
                            SchemaKatalog.SPALTE_DECKUNG_PROZESS + ") " +
                            "VALUES (?,?,?,?,?,?,?, ?,?,?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = sId });
                            p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                            p.Add(new DbParam("@a1", DbParamTyp.Double) { Wert = R(m.Solarthermie.Waermebedarf) });
                            p.Add(new DbParam("@a2", DbParamTyp.Double) { Wert = R(m.Solarthermie.Restwaermebedarf) });
                            p.Add(new DbParam("@a3", DbParamTyp.Double) { Wert = R(m.Solarthermie.Waermeproduktion) });
                            p.Add(new DbParam("@a4", DbParamTyp.Double) { Wert = R(m.Solarthermie.Waermebedarfsdeckung) });
                            p.Add(new DbParam("@a5", DbParamTyp.Double) { Wert = R(m.Solarthermie.Ueberschuss) });
                            KanalParameter(p, m.Solarthermie.Deckung_Kanal);
                            v.Ausfuehren(sql, p.ToArray());
                        }

                        if (m.Solarthermie.Module != null && m.Solarthermie.Module.Count > 0)
                        {
                            int modId = NextId(v, TAB_SOLAR_MODUL);
                            string sqlM = "INSERT INTO " + TAB_SOLAR_MODUL + " (" +
                                "ID, ID_ErgebnisSolarthermie, Modul, Flaeche, Anzahl, Waermeproduktion, Ueberschuss) " +
                                "VALUES (?,?,?,?,?,?,?)";
                            foreach (ErgebnisSolarthermieModulModel mo in m.Solarthermie.Module)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = modId++ });
                                    p.Add(new DbParam("@so", DbParamTyp.Integer) { Wert = sId });
                                    p.Add(new DbParam("@mod", DbParamTyp.VarWChar) { Wert = (object)(mo.Modul ?? "") });
                                    p.Add(new DbParam("@fl", DbParamTyp.Double) { Wert = R(mo.Flaeche) });
                                    p.Add(new DbParam("@an", DbParamTyp.Integer) { Wert = (int)mo.Anzahl });
                                    p.Add(new DbParam("@w", DbParamTyp.Double) { Wert = R(mo.Waermeproduktion) });
                                    p.Add(new DbParam("@u", DbParamTyp.Double) { Wert = R(mo.Ueberschuss) });
                                    v.Ausfuehren(sqlM, p.ToArray());
                                }
                            }
                        }
                    }

                    // 8. Detail: Photovoltaik (+ Modulliste).
                    if (m.Photovoltaik != null)
                    {
                        int pId = NextId(v, TAB_PV);
                        string sql = "INSERT INTO " + TAB_PV + " (" +
                            "ID, ID_Ergebnis, Strombedarf, Reststrombedarf, Stromproduktion, Strombedarfsdeckung, Ueberschuss, MaxSolareLeistung) " +
                            "VALUES (?,?,?,?,?,?,?,?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = pId });
                            p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                            p.Add(new DbParam("@a1", DbParamTyp.Double) { Wert = R(m.Photovoltaik.Strombedarf) });
                            p.Add(new DbParam("@a2", DbParamTyp.Double) { Wert = R(m.Photovoltaik.Reststrombedarf) });
                            p.Add(new DbParam("@a3", DbParamTyp.Double) { Wert = R(m.Photovoltaik.Stromproduktion) });
                            p.Add(new DbParam("@a4", DbParamTyp.Double) { Wert = R(m.Photovoltaik.Strombedarfsdeckung) });
                            p.Add(new DbParam("@a5", DbParamTyp.Double) { Wert = R(m.Photovoltaik.Ueberschuss) });
                            p.Add(new DbParam("@a6", DbParamTyp.Double) { Wert = R(m.Photovoltaik.MaxSolareLeistung) });
                            v.Ausfuehren(sql, p.ToArray());
                        }

                        if (m.Photovoltaik.Module != null && m.Photovoltaik.Module.Count > 0)
                        {
                            int modId = NextId(v, TAB_PV_MODUL);
                            string sqlM = "INSERT INTO " + TAB_PV_MODUL + " (" +
                                "ID, ID_ErgebnisPhotovoltaik, Modul, Flaeche, Anzahl, Stromproduktion) " +
                                "VALUES (?,?,?,?,?,?)";
                            foreach (ErgebnisPhotovoltaikModulModel mo in m.Photovoltaik.Module)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = modId++ });
                                    p.Add(new DbParam("@pv", DbParamTyp.Integer) { Wert = pId });
                                    p.Add(new DbParam("@mod", DbParamTyp.VarWChar) { Wert = (object)(mo.Modul ?? "") });
                                    p.Add(new DbParam("@fl", DbParamTyp.Double) { Wert = R(mo.Flaeche) });
                                    p.Add(new DbParam("@an", DbParamTyp.Integer) { Wert = (int)mo.Anzahl });
                                    p.Add(new DbParam("@s", DbParamTyp.Double) { Wert = R(mo.Stromproduktion) });
                                    v.Ausfuehren(sqlM, p.ToArray());
                                }
                            }
                        }
                    }

                    // 9. Detail: Pufferspeicher (Konzept 6.6) - eine Zeile je beteiligtem
                    //    Speicher, Senken- wie Quellspeicher. IDs wie bei den
                    //    Geschwistertabellen ueber MAX(ID)+1 und dann hochzaehlend.
                    if (m.Pufferspeicher != null && m.Pufferspeicher.Count > 0)
                    {
                        int pufId = NextId(v, TAB_PUFFER);
                        // PAKET E1 (Schritt 52): acht neue Spalten am Ende - Kanalaufteilung
                        // der Entladung, die beiden Durchsatzsummen, der Anlagenbezug der
                        // Quellspeicherzeilen und die beiden P1-Vorgriffsspalten. Sie stehen
                        // hinten, weil ALTER TABLE in Access hinten anhaengt und die
                        // Parameterreihenfolge der Spaltenliste folgt.
                        string sqlP = "INSERT INTO " + TAB_PUFFER + " (" +
                            "ID, ID_Ergebnis, ID_Pufferspeicher, Bezeichner, Verwendung, Q_max, " +
                            "Ladung_gesamt, Entladung_gesamt, Verluste_gesamt, SOC_Ende, SOC_Mittel, " +
                            "SOC_Max, Vollzyklen, " +
                            SchemaKatalog.SPALTE_PUFFER_ENTLADUNG_HEIZUNG + ", " +
                            SchemaKatalog.SPALTE_PUFFER_ENTLADUNG_BRAUCHWASSER + ", " +
                            SchemaKatalog.SPALTE_PUFFER_ENTLADUNG_PROZESS + ", " +
                            SchemaKatalog.SPALTE_PUFFER_DURCHSATZ_GELADEN + ", " +
                            SchemaKatalog.SPALTE_PUFFER_DURCHSATZ_ENTLADEN + ", " +
                            SchemaKatalog.SPALTE_PUFFER_ID_ANLAGE + ", " +
                            SchemaKatalog.SPALTE_PUFFER_T_OBEN_MITTEL + ", " +
                            SchemaKatalog.SPALTE_PUFFER_T_OBEN_MIN + ") " +
                            "VALUES (?,?,?,?,?,?, ?,?,?,?,?, ?,?, ?,?,?, ?,?, ?, ?,?)";
                        foreach (ErgebnisPufferspeicherModel sp in m.Pufferspeicher)
                        {
                            {
                                List<DbParam> p = new List<DbParam>();
                                p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = pufId++ });
                                p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                                p.Add(new DbParam("@sp", DbParamTyp.Integer) { Wert = sp.ID_Pufferspeicher > 0 ? (object)sp.ID_Pufferspeicher : DBNull.Value });
                                p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = (object)(sp.Bezeichner ?? "") });
                                p.Add(new DbParam("@ver", DbParamTyp.VarWChar) { Wert = (object)(sp.Verwendung ?? "") });
                                p.Add(new DbParam("@a1", DbParamTyp.Double) { Wert = R(sp.Q_max) });
                                p.Add(new DbParam("@a2", DbParamTyp.Double) { Wert = R(sp.Ladung_gesamt) });
                                p.Add(new DbParam("@a3", DbParamTyp.Double) { Wert = R(sp.Entladung_gesamt) });
                                p.Add(new DbParam("@a4", DbParamTyp.Double) { Wert = R(sp.Verluste_gesamt) });
                                p.Add(new DbParam("@a5", DbParamTyp.Double) { Wert = R(sp.SOC_Ende) });
                                p.Add(new DbParam("@a6", DbParamTyp.Double) { Wert = R(sp.SOC_Mittel) });
                                p.Add(new DbParam("@a7", DbParamTyp.Double) { Wert = R(sp.SOC_Max) });
                                p.Add(new DbParam("@a8", DbParamTyp.Double) { Wert = R(sp.Vollzyklen) });

                                // PAKET E1
                                KanalParameter(p, sp.Entladung_Kanal);
                                p.Add(new DbParam("@d1", DbParamTyp.Double) { Wert = R(sp.Durchsatz_Geladen) });
                                p.Add(new DbParam("@d2", DbParamTyp.Double) { Wert = R(sp.Durchsatz_Entladen) });
                                p.Add(new DbParam("@anl", DbParamTyp.Integer) { Wert = sp.ID_Anlage > 0 ? (object)sp.ID_Anlage : DBNull.Value });
                                // Seit Paket P1 GEFUELLT (bis dahin P1-Vorgriff und immer
                                // NULL). NULL heisst weiterhin "nicht erhoben" - eine 0
                                // behauptete 0 Grad C; so bleibt eine Quellspeicherzeile
                                // ohne Speichertemperatur ehrlich leer.
                                p.Add(new DbParam("@t1", DbParamTyp.Double) { Wert = sp.T_oben_Mittel.HasValue ? (object)R(sp.T_oben_Mittel.Value) : DBNull.Value });
                                p.Add(new DbParam("@t2", DbParamTyp.Double) { Wert = sp.T_oben_Min.HasValue ? (object)R(sp.T_oben_Min.Value) : DBNull.Value });

                                v.Ausfuehren(sqlP, p.ToArray());
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
                        int spId = NextId(v, TAB_SP);
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
                            {
                                List<DbParam> p = new List<DbParam>();
                                p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = spId++ });
                                p.Add(new DbParam("@erg", DbParamTyp.Integer) { Wert = kopfId });
                                p.Add(new DbParam("@anl", DbParamTyp.Integer) { Wert = es.ID_Energieanlage > 0 ? (object)es.ID_Energieanlage : DBNull.Value });
                                p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = (object)(es.Bezeichner ?? "") });
                                p.Add(new DbParam("@bart", DbParamTyp.VarWChar) { Wert = (object)(es.Betriebsart ?? "") });
                                p.Add(new DbParam("@rart", DbParamTyp.VarWChar) { Wert = (object)(es.Berechnungsart ?? "") });

                                p.Add(new DbParam("@e1", DbParamTyp.Double) { Wert = R(es.Ladung_PV) });
                                p.Add(new DbParam("@e2", DbParamTyp.Double) { Wert = R(es.Ladung_BHKW) });
                                p.Add(new DbParam("@e3", DbParamTyp.Double) { Wert = R(es.Ladung_Netz) });
                                p.Add(new DbParam("@e4", DbParamTyp.Double) { Wert = R(es.Ladung_Gesamt) });
                                p.Add(new DbParam("@e5", DbParamTyp.Double) { Wert = R(es.Entladung_Gesamt) });
                                p.Add(new DbParam("@e6", DbParamTyp.Double) { Wert = R(es.Verluste_Gesamt) });
                                p.Add(new DbParam("@e7", DbParamTyp.Double) { Wert = R(es.Netzbezug_Mit) });
                                p.Add(new DbParam("@e8", DbParamTyp.Double) { Wert = R(es.Netzbezug_Ohne) });
                                p.Add(new DbParam("@e9", DbParamTyp.Double) { Wert = R(es.Einspeisung_Mit) });
                                p.Add(new DbParam("@e10", DbParamTyp.Double) { Wert = R(es.Einspeisung_Ohne) });
                                p.Add(new DbParam("@e11", DbParamTyp.Double) { Wert = R(es.Eigenverbrauchsquote) });
                                p.Add(new DbParam("@e12", DbParamTyp.Double) { Wert = R(es.Autarkiegrad) });

                                p.Add(new DbParam("@s1", DbParamTyp.Double) { Wert = R(es.Vollzyklen) });
                                p.Add(new DbParam("@s2", DbParamTyp.Double) { Wert = R(es.SoC_Min) });
                                p.Add(new DbParam("@s3", DbParamTyp.Double) { Wert = R(es.SoC_Mittel) });
                                p.Add(new DbParam("@s4", DbParamTyp.Double) { Wert = R(es.SoC_Max) });
                                p.Add(new DbParam("@s5", DbParamTyp.Double) { Wert = R(es.Zeitanteil_Untergrenze) });
                                p.Add(new DbParam("@s6", DbParamTyp.Double) { Wert = R(es.Zeitanteil_Obergrenze) });
                                p.Add(new DbParam("@s7", DbParamTyp.Double) { Wert = R(es.Zyklen_Hochrechnung) });

                                p.Add(new DbParam("@w1", DbParamTyp.Double) { Wert = R(es.Ertrag_Bezugsersparnis) });
                                p.Add(new DbParam("@w2", DbParamTyp.Double) { Wert = R(es.Ertrag_Verguetung_Entgangen) });
                                p.Add(new DbParam("@w3", DbParamTyp.Double) { Wert = R(es.Ertrag_Netzerloes) });
                                p.Add(new DbParam("@w4", DbParamTyp.Double) { Wert = R(es.Kosten_Ladung) });
                                p.Add(new DbParam("@w5", DbParamTyp.Double) { Wert = R(es.Ertrag_Leistungspreis) });
                                p.Add(new DbParam("@w6", DbParamTyp.Double) { Wert = R(es.Verschleisskosten) });
                                p.Add(new DbParam("@w7", DbParamTyp.Double) { Wert = R(es.Investition) });
                                p.Add(new DbParam("@w8", DbParamTyp.Double) { Wert = R(es.Annuitaet) });
                                p.Add(new DbParam("@w9", DbParamTyp.Double) { Wert = R(es.Jahresueberschuss) });
                                p.Add(new DbParam("@w10", DbParamTyp.Double) { Wert = R(es.Ertrag_Jahr1) });
                                p.Add(new DbParam("@w11", DbParamTyp.Double) { Wert = R(es.Ertrag_Aequivalent) });
                                p.Add(new DbParam("@w12", DbParamTyp.Double) { Wert = R(es.Amortisation_Statisch) });
                                p.Add(new DbParam("@w13", DbParamTyp.Double) { Wert = R(es.Amortisation_Dynamisch) });
                                p.Add(new DbParam("@w14", DbParamTyp.Double) { Wert = R(es.Kapitalwert) });
                                p.Add(new DbParam("@w15", DbParamTyp.VarWChar) { Wert = (object)(es.Preisversion ?? "") });
                                v.Ausfuehren(sqlS, p.ToArray());
                            }
                        }
                    }

                    v.Commit();
                    m.ID = kopfId;
                    return kopfId;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    // PAKET 8 (Konzept 13.4): Save() ist die letzte Station des headless-Laufs
                    // (SimulationRunner.SimuliereUndSpeichere) und damit die Stelle, an der
                    // eine MessageBox einen unbeaufsichtigten Lauf noch NACH der Rechnung
                    // hätte blockieren können.
                    DataRepository.FehlerMelden("Fehler beim Speichern des Simulationsergebnisses: " + ex.Message);
                    return -1;
                }
            }
        }

        // Laedt das zuletzt gespeicherte Ergebnis eines Projekts (oder null).
        public ErgebnisModel Load(int idProjekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + TAB_KOPF + " WHERE ID_Projekt = ? ORDER BY ID DESC LIMIT 1",
                new DbParam("@p", idProjekt));
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
                "SELECT * FROM " + TAB_ENERGIE + " WHERE ID_Ergebnis = ? LIMIT 1", new DbParam("@e", m.ID));
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
                // PAKET E1: fehlt die Spalte (Zeile vor Schritt 52) oder ist sie NULL,
                // liefert D() 0 - genau die Behandlung, die Bestandszeilen brauchen.
                KanalLesen(re, m.Energiebedarf.Waermebedarf_Kanal,
                           SchemaKatalog.SPALTE_BEDARF_HEIZUNG,
                           SchemaKatalog.SPALTE_BEDARF_BRAUCHWASSER,
                           SchemaKatalog.SPALTE_BEDARF_PROZESS);
            }

            // Detail: Waermepumpe (+ Module).
            DataTable dw = DataRepository.GetDataTable(
                "SELECT * FROM " + TAB_WP + " WHERE ID_Ergebnis = ? LIMIT 1", new DbParam("@e", m.ID));
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
                DeckungLesen(rw, w.Deckung_Kanal);   // PAKET E1

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_WP_MODUL + " WHERE ID_ErgebnisWaermepumpe = ? ORDER BY ID",
                    new DbParam("@w", wpId));
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
                "SELECT * FROM " + TAB_BHKW + " WHERE ID_Ergebnis = ? LIMIT 1", new DbParam("@e", m.ID));
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
                DeckungLesen(rb, b.Deckung_Kanal);   // PAKET E1

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_BHKW_MODUL + " WHERE ID_ErgebnisBHKW = ? ORDER BY ID",
                    new DbParam("@b", bId));
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
                        // ETAPPE B3 Paket a: D() liefert 0, wenn die Spalte fehlt (Zeile
                        // vor Schritt 61) oder NULL ist - genau die Behandlung, die
                        // "keine Hilfsenergie" braucht.
                        mo.Hilfsenergie = D(rm, SchemaKatalog.SPALTE_MODUL_HILFSENERGIE);
                        b.Module.Add(mo);
                    }

                m.BHKW = b;
            }

            // Detail: Heizkessel (+ Module).
            DataTable dhk = DataRepository.GetDataTable(
                "SELECT * FROM " + TAB_KESSEL + " WHERE ID_Ergebnis = ? LIMIT 1", new DbParam("@e", m.ID));
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
                DeckungLesen(rh, h.Deckung_Kanal);   // PAKET E1

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_KESSEL_MODUL + " WHERE ID_ErgebnisHeizkessel = ? ORDER BY ID",
                    new DbParam("@h", hId));
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
                        // ETAPPE B3 Paket a - Begruendung wie bei der BHKW-Modulzeile.
                        mo.Hilfsenergie = D(rm, SchemaKatalog.SPALTE_MODUL_HILFSENERGIE);
                        h.Module.Add(mo);
                    }

                m.Heizkessel = h;
            }

            // Detail: Solarthermie (+ Kollektoren).
            DataTable dst = DataRepository.GetDataTable(
                "SELECT * FROM " + TAB_SOLAR + " WHERE ID_Ergebnis = ? LIMIT 1", new DbParam("@e", m.ID));
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
                DeckungLesen(rs2, s.Deckung_Kanal);   // PAKET E1

                DataTable dmod = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_SOLAR_MODUL + " WHERE ID_ErgebnisSolarthermie = ? ORDER BY ID",
                    new DbParam("@s", sId));
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
                "SELECT * FROM " + TAB_PV + " WHERE ID_Ergebnis = ? LIMIT 1", new DbParam("@e", m.ID));
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
                    new DbParam("@p", pId));
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

                    // PAKET E1 (Schritt 52). Fehlende Spalten und NULL liefern 0 bzw.
                    // null - die Behandlung der Zeilen aus Laeufen vor E1.
                    sp.ID_Anlage = I(rsp, SchemaKatalog.SPALTE_PUFFER_ID_ANLAGE);
                    KanalLesen(rsp, sp.Entladung_Kanal,
                               SchemaKatalog.SPALTE_PUFFER_ENTLADUNG_HEIZUNG,
                               SchemaKatalog.SPALTE_PUFFER_ENTLADUNG_BRAUCHWASSER,
                               SchemaKatalog.SPALTE_PUFFER_ENTLADUNG_PROZESS);
                    sp.Durchsatz_Geladen = D(rsp, SchemaKatalog.SPALTE_PUFFER_DURCHSATZ_GELADEN);
                    sp.Durchsatz_Entladen = D(rsp, SchemaKatalog.SPALTE_PUFFER_DURCHSATZ_ENTLADEN);
                    sp.T_oben_Mittel = DN(rsp, SchemaKatalog.SPALTE_PUFFER_T_OBEN_MITTEL);
                    sp.T_oben_Min = DN(rsp, SchemaKatalog.SPALTE_PUFFER_T_OBEN_MIN);

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
                new DbParam("@p", idProjekt));
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
            // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, Schemaprobe statt
            // GetOleDbSchemaTable (S4c vorgezogen), SQLite-Spaltentyp (S4d vorgezogen).
            try
            {
                foreach (string sp in spalten) ErgaenzeSpalte(TAB_ENERGIE, sp, "DOUBLE");
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
            // ARBEITSPAKET S4b: wie StelleEnergieSpaltenSicher.
            try
            {
                foreach (string sp in spalten) ErgaenzeSpalte(TAB_BHKW, sp, "DOUBLE");
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
                {
                    ErgaenzeSpalte(TAB_BHKW_MODUL, "carrier_id", "LONG");
                    ErgaenzeSpalte(TAB_KESSEL_MODUL, "carrier_id", "LONG");
                    ErgaenzeSpalte(TAB_KESSEL_MODUL, "Waermeproduktion", "DOUBLE");

                    // ETAPPE E2 — Rückfallebene zu Migrationsschritt 18. Dieselbe
                    // Begründung wie bei Quellwaerme: Das INSERT der Modulzeile führt die
                    // beiden Spalten NAMENTLICH auf; fehlen sie, scheitert nicht nur die
                    // neue Größe, sondern die ganze Modulzeile — und mit ihr der Lauf.
                    // Die Namen kommen aus SchemaKatalog, Migration und Rückfallebene
                    // führen keine zweite Liste.
                    ErgaenzeSpalte(TAB_BHKW_MODUL, SchemaKatalog.SPALTE_MODUL_VBH_THERMISCH, "DOUBLE");
                    ErgaenzeSpalte(TAB_BHKW_MODUL, SchemaKatalog.SPALTE_MODUL_VBH_ELEKTRISCH, "DOUBLE");

                    // ETAPPE B3 Paket a - Rueckfallebene zu Migrationsschritt 61b, aus
                    // derselben Not wie eine Zeile darueber: Beide Modul-INSERTs fuehren
                    // die Hilfsenergie NAMENTLICH auf; fehlt die Spalte, scheitert nicht
                    // nur die neue Groesse, sondern die ganze Modulzeile - und mit ihr
                    // der Lauf. Die Namen kommen aus SchemaKatalog, Migration und
                    // Rueckfallebene fuehren keine zweite Liste.
                    ErgaenzeSpalte(TAB_BHKW_MODUL, SchemaKatalog.SPALTE_MODUL_HILFSENERGIE, "DOUBLE");
                    ErgaenzeSpalte(TAB_KESSEL_MODUL, SchemaKatalog.SPALTE_MODUL_HILFSENERGIE, "DOUBLE");
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
                ErgaenzeSpalte(TAB_KESSEL, SchemaKatalog.SPALTE_KESSEL_QUELLWAERME, "DOUBLE");
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
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht; Schema-Auskunft
                // statt GetOleDbSchemaTable (S4c vorgezogen); SQLite-DDL nach dem Muster
                // von sql\schema\001_grundschema.sql (S4d vorgezogen).
                //
                // ABWEICHUNG, DIE SQLITE ERZWINGT: Die Loeschweitergabe kann nach dem
                // CREATE TABLE nicht mehr nachgeruestet werden (kein ADD CONSTRAINT).
                // Sie steht deshalb IM CREATE - und der Nachruest-Zweig unten entfaellt
                // fuer eine Tabelle, die es schon gibt. Wo er bisher half (Tabelle aus
                // einem abgebrochenen Lauf ohne Beziehung), traegt weiterhin das
                // ausdrueckliche DELETE in Save, auf das der Kommentar oben verweist.
                {
                    if (!PufferTabelleVorhanden())
                    {
                        // Spaltensatz identisch zu SchemaMigration.SQL_CREATE_ERGEBNISPUFFER.
                        try
                        {
                            Ddl("CREATE TABLE IF NOT EXISTS [" + TAB_PUFFER + "] (" +
                                "\"ID\" INTEGER NOT NULL PRIMARY KEY, " +
                                "\"ID_Ergebnis\" INTEGER, \"ID_Pufferspeicher\" INTEGER, \"Bezeichner\" TEXT, " +
                                "\"Verwendung\" TEXT CHECK (length(\"Verwendung\") <= 50), " +
                                "\"Q_max\" REAL, \"Ladung_gesamt\" REAL, " +
                                "\"Entladung_gesamt\" REAL, \"Verluste_gesamt\" REAL, \"SOC_Ende\" REAL, " +
                                "\"SOC_Mittel\" REAL, \"SOC_Max\" REAL, \"Vollzyklen\" REAL, " +
                                "FOREIGN KEY (\"ID_Ergebnis\") REFERENCES [" + TAB_KOPF + "] (\"ID\") ON DELETE CASCADE)");
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
                    if (!IndexVorhanden(TAB_PUFFER, "idx_ErgPuffer", "FK_ErgPuffer"))
                    {
                        try { Ddl("CREATE INDEX IF NOT EXISTS \"idx_ErgPuffer\" ON [" + TAB_PUFFER + "] (\"ID_Ergebnis\")"); }
                        catch { }
                    }

                    // Dieselbe Löschweitergabe wie bei allen Geschwistertabellen (13.7).
                    // Nachruesten kann SQLite sie nicht; fehlt sie auf einer alten
                    // Tabelle, raeumen wir wenigstens die Waisen weg - denselben Zweck
                    // erfuellt danach das ausdrueckliche DELETE in Save.
                    if (!FremdschluesselVorhanden(TAB_PUFFER, "ID_Ergebnis"))
                    {
                        try
                        {
                            Ddl("DELETE FROM " + TAB_PUFFER + " WHERE ID_Ergebnis NOT IN " +
                                "(SELECT ID FROM " + TAB_KOPF + ")");
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
                // ARBEITSPAKET S4b: wie StellePufferTabelleSicher - Zugriffsschicht,
                // Schema-Auskunft, SQLite-DDL. Die Anweisungen kommen NICHT mehr aus
                // SchemaMigration: Die dortigen Konstanten sind Access-DDL und gehoeren
                // zum eingefrorenen Alt-Zweig (S6). Massgeblich ist
                // sql\schema\001_grundschema.sql; ein zweiter Spaltensatz entsteht
                // nicht - beide beschreiben dieselbe Tabelle.
                {
                    if (!TabelleVorhanden(TAB_SP))
                    {
                        try { Ddl(SQL_CREATE_ERGEBNISSTROMSPEICHER_SQLITE); }
                        catch { return; /* ohne Tabelle sind Index und Constraint sinnlos */ }
                    }

                    if (!IndexVorhanden(TAB_SP, "idx_ErgStromspeicher", "FK_ErgStromspeicher"))
                    {
                        try { Ddl("CREATE INDEX IF NOT EXISTS \"idx_ErgStromspeicher\" ON [" + TAB_SP + "] (\"ID_Ergebnis\")"); }
                        catch { }
                    }

                    if (!FremdschluesselVorhanden(TAB_SP, "ID_Ergebnis"))
                    {
                        // Waisen zuerst - dieselbe Reihenfolge und derselbe Grund wie
                        // beim Pufferspeicher. Nachruesten laesst sich die Beziehung in
                        // SQLite nicht; sie steht im CREATE TABLE.
                        try
                        {
                            Ddl("DELETE FROM " + TAB_SP + " WHERE ID_Ergebnis NOT IN " +
                                "(SELECT ID FROM " + TAB_KOPF + ")");
                        }
                        catch { }
                    }
                }
            }
            catch { /* best effort - Save faengt einen echten Fehler ohnehin ab */ }
        }

        /// <summary>
        /// SQLite-Fassung von <c>SchemaMigration.SQL_CREATE_ERGEBNISSTROMSPEICHER</c>
        /// (Fachkonzept Stromspeicher 7.1). Gleiche Spalten, gleiche Reihenfolge; die
        /// Loeschweitergabe steht im CREATE, weil SQLite sie nicht nachruesten kann.
        /// </summary>
        private const string SQL_CREATE_ERGEBNISSTROMSPEICHER_SQLITE =
            "CREATE TABLE IF NOT EXISTS \"Tab_ErgebnisStromspeicher\" (" +
            "\"ID\" INTEGER NOT NULL PRIMARY KEY, " +
            "\"ID_Ergebnis\" INTEGER, \"ID_Energieanlage\" INTEGER, \"Bezeichner\" TEXT, " +
            "\"Betriebsart\" TEXT CHECK (length(\"Betriebsart\") <= 50), " +
            "\"Berechnungsart\" TEXT CHECK (length(\"Berechnungsart\") <= 50), " +
            // Energie (7.1, Block 1)
            "\"Ladung_PV\" REAL, \"Ladung_BHKW\" REAL, \"Ladung_Netz\" REAL, \"Ladung_Gesamt\" REAL, " +
            "\"Entladung_Gesamt\" REAL, \"Verluste_Gesamt\" REAL, " +
            "\"Netzbezug_Mit\" REAL, \"Netzbezug_Ohne\" REAL, " +
            "\"Einspeisung_Mit\" REAL, \"Einspeisung_Ohne\" REAL, " +
            "\"Eigenverbrauchsquote\" REAL, \"Autarkiegrad\" REAL, " +
            // Speicher (7.1, Block 2)
            "\"Vollzyklen\" REAL, \"SoC_Min\" REAL, \"SoC_Mittel\" REAL, \"SoC_Max\" REAL, " +
            "\"Zeitanteil_Untergrenze\" REAL, \"Zeitanteil_Obergrenze\" REAL, " +
            "\"Zyklen_Hochrechnung\" REAL, " +
            // Wirtschaft (7.1, Block 3)
            "\"Ertrag_Bezugsersparnis\" REAL, \"Ertrag_Verguetung_Entgangen\" REAL, " +
            "\"Ertrag_Netzerloes\" REAL, \"Kosten_Ladung\" REAL, \"Ertrag_Leistungspreis\" REAL, " +
            "\"Verschleisskosten\" REAL, \"Investition\" REAL, \"Annuitaet\" REAL, " +
            "\"Jahresueberschuss\" REAL, \"Ertrag_Jahr1\" REAL, \"Ertrag_Aequivalent\" REAL, " +
            "\"Amortisation_Statisch\" REAL, \"Amortisation_Dynamisch\" REAL, " +
            "\"Kapitalwert\" REAL, \"Preisversion\" TEXT CHECK (length(\"Preisversion\") <= 50), " +
            "FOREIGN KEY (\"ID_Ergebnis\") REFERENCES \"Tab_Ergebnis\" (\"ID\") ON DELETE CASCADE)";

        private static bool PufferTabelleVorhanden()
        {
            return TabelleVorhanden(TAB_PUFFER);
        }

        /// <summary>Gibt es die Tabelle? ARBEITSPAKET S4b: Schema-Auskunft der
        /// Zugriffsschicht statt <c>GetOleDbSchemaTable</c> (S4c vorgezogen).</summary>
        private static bool TabelleVorhanden(string tabelle)
        {
            return StilleDb.TabelleVorhanden(tabelle);
        }

        /// <summary>
        /// Gibt es EINEN der genannten Indizes auf der Tabelle?
        /// ARBEITSPAKET S4b: ueber <c>DataRepository.IndexListe</c> (S4c vorgezogen).
        ///
        /// MEHRERE NAMEN, WEIL DAS ZIELSCHEMA ANDERS BENENNT: In der Access-Datenbank
        /// hiess der Lese-Index "idx_ErgPuffer" bzw. "idx_ErgStromspeicher"; im
        /// SQLite-Grundschema (003_indizes_fk.sql) traegt derselbe Index den Namen der
        /// Beziehung ("FK_ErgPuffer" / "FK_ErgStromspeicher"). Ohne diesen Abgleich
        /// legte die Rueckfallebene bei JEDEM Start einen zweiten, ueberfluessigen Index
        /// unter dem Alt-Namen an.
        /// </summary>
        private static bool IndexVorhanden(string tabelle, params string[] namen)
        {
            try
            {
                DataTable dt = DataRepository.IndexListe(tabelle);
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        foreach (string index in namen)
                            if (string.Equals(Txt(r, "Indexname"), index, StringComparison.OrdinalIgnoreCase))
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
        private static bool FremdschluesselVorhanden(string tabelle, string spalte)
        {
            try
            {
                // ARBEITSPAKET S4b: DataRepository.FremdschluesselListe liefert die
                // Beziehungen EINER Tabelle (SQLite kennt kein globales Rowset), der
                // Tabellenvergleich entfaellt deshalb.
                DataTable dt = DataRepository.FremdschluesselListe(tabelle);
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (string.Equals(Txt(r, "Quellspalte"), spalte, StringComparison.OrdinalIgnoreCase))
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
        private static void PufferzeilenLoeschen(DbVorgang v, int idProjekt)
        {
            DetailzeilenLoeschen(v, TAB_PUFFER, idProjekt);
        }

        /// <summary>
        /// Wie <see cref="PufferzeilenLoeschen"/>, nur mit dem Tabellennamen als
        /// Parameter - AP3 braucht denselben Vorablauf fuer Tab_ErgebnisStromspeicher.
        /// </summary>
        private static void DetailzeilenLoeschen(DbVorgang v, string tabelle, int idProjekt)
        {
            try
            {
                v.Ausfuehren("DELETE FROM " + tabelle + " WHERE ID_Ergebnis IN " +
                             "(SELECT ID FROM " + TAB_KOPF + " WHERE ID_Projekt = ?)",
                             new DbParam("@p", DbParamTyp.Integer) { Wert = idProjekt });
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
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, still wie bisher.
                if (!PufferTabelleVorhanden()) return null;

                return StilleDb.Tabelle(
                    "SELECT * FROM " + TAB_PUFFER + " WHERE ID_Ergebnis = ? ORDER BY ID",
                    StilleDb.Par("@e", DbParamTyp.Integer, idErgebnis));
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
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, still wie bisher.
                if (!TabelleVorhanden(TAB_SP)) return null;

                return StilleDb.Tabelle(
                    "SELECT * FROM " + TAB_SP + " WHERE ID_Ergebnis = ? ORDER BY ID",
                    StilleDb.Par("@e", DbParamTyp.Integer, idErgebnis));
            }
            catch { return null; }
        }

        /// <summary>
        /// Eine DDL-/Verwaltungsanweisung, still. ARBEITSPAKET S4b, Verhaltenstreue:
        /// Der frueherer Weg WARF bei einem Fehlschlag - darauf bauen die
        /// try/catch-Klammern der Aufrufer (ein misslungenes CREATE bricht ab, ein
        /// misslungenes DELETE nicht). StilleDb wirft nicht, also wird hier von Hand
        /// geworfen; gefangen wurde die Ausnahme schon bisher wortlos.
        /// </summary>
        private static void Ddl(string sql)
        {
            if (StilleDb.NonQuery(sql) < 0)
                throw new InvalidOperationException("Anweisung fehlgeschlagen: " + sql);
        }

        /// <summary>
        /// Legt eine Spalte an, falls sie fehlt. ARBEITSPAKET S4b: Schema-Auskunft statt
        /// <c>GetOleDbSchemaTable</c> (S4c vorgezogen), Access-Typ -&gt; SQLite beim
        /// Verbrauch (S4d vorgezogen). Wirft wie bisher, wenn das ALTER scheitert.
        /// </summary>
        private static void ErgaenzeSpalte(string tabelle, string spalte, string typ)
        {
            System.Collections.Generic.HashSet<string> vorhanden = StilleDb.SpaltenNamen(tabelle);
            if (vorhanden != null && vorhanden.Contains(spalte)) return;   // vorhanden

            Ddl(StilleDb.AlterTableAddColumn(tabelle, spalte, typ));
        }

        // --- Helpers ---

        private static int NextId(DbVorgang v, string table)
        {
            object m = v.Skalar("SELECT MAX(ID) FROM " + table);
            return ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
        }

        // Rundet Ergebniswerte auf max. 2 Nachkommastellen (kaufmaennisch).
        private static double R(double v)
        { return Math.Round(v, 2, MidpointRounding.AwayFromZero); }

        // ---------------------------------------------------------------------------
        // PAKET E1 (Konzept 4.4) - die drei Kanalspalten, einmal geschrieben
        //
        //   Sie treten in sechs INSERT und sechs Lesestellen auf, immer in derselben
        //   Reihenfolge Heizung, Brauchwasser, Prozess. Drei Zeilen je Fundstelle
        //   waeren achtzehn Gelegenheiten, die Reihenfolge zu vertauschen - und ein
        //   vertauschtes Paar faellt in keinem Test auf, solange beide Kanaele belegt
        //   sind.
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Haengt die drei Kanalwerte in der Reihenfolge Heizung, Brauchwasser, Prozess
        /// an die Parameterliste. Ein fehlendes oder zu kurzes Feld wird als 0
        /// geschrieben - die Spalten werden IMMER belegt, damit "nicht erhoben" (NULL,
        /// Zeile vor Schritt 52) und "erhoben und null" unterscheidbar bleiben; dieselbe
        /// Begruendung wie bei Quellwaerme und den Vbh-Spalten.
        /// </summary>
        private static void KanalParameter(List<DbParam> p, double[] werte)
        {
            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                double wert = (werte != null && k < werte.Length) ? werte[k] : 0.0;
                p.Add(new DbParam("@k" + k, DbParamTyp.Double) { Wert = R(wert) });
            }
        }

        /// <summary>Liest die drei Kanalspalten in ein vorhandenes Feld (Reihenfolge wie oben).</summary>
        private static void KanalLesen(DataRow r, double[] ziel,
                                       string spalteHeizung, string spalteBrauchwasser,
                                       string spalteProzess)
        {
            if (ziel == null || ziel.Length < Kanal.ANZAHL) return;
            ziel[Kanal.HEIZUNG] = D(r, spalteHeizung);
            ziel[Kanal.BRAUCHWASSER] = D(r, spalteBrauchwasser);
            ziel[Kanal.PROZESS] = D(r, spalteProzess);
        }

        /// <summary>
        /// Liest die drei Deckungsspalten einer Erzeuger-Ergebniszeile. Sie heissen in
        /// allen vier Tabellen gleich - deshalb eine Fassung ohne Spaltenparameter.
        /// </summary>
        private static void DeckungLesen(DataRow r, double[] ziel)
        {
            KanalLesen(r, ziel,
                       SchemaKatalog.SPALTE_DECKUNG_HEIZUNG,
                       SchemaKatalog.SPALTE_DECKUNG_BRAUCHWASSER,
                       SchemaKatalog.SPALTE_DECKUNG_PROZESS);
        }

        /// <summary>
        /// PAKET E1 - Rueckfallebene zu Migrationsschritt 52, nach dem Muster von
        /// <see cref="StelleKesselSpaltenSicher"/> und aus demselben Grund: Die INSERT
        /// oben fuehren die neuen Spalten NAMENTLICH auf. Fehlen sie, scheitert nicht nur
        /// die neue Groesse, sondern die ganze Ergebniszeile - und mit ihr der Lauf.
        /// Die Namen kommen aus <see cref="SchemaKatalog.Schritt52_ErgebnisJeKanal"/>,
        /// Migration und Rueckfallebene fuehren keine zweite Liste.
        /// </summary>
        private static void StelleKanalSpaltenSicher()
        {
            try
            {
                foreach (SchemaSpalte s in SchemaKatalog.Schritt52_ErgebnisJeKanal)
                    ErgaenzeSpalte(s.Tabelle, s.Name, s.TypDefinition);
            }
            catch { /* best effort - Spalten existieren dann ggf. schon */ }
        }

        /// <summary>
        /// Die ID des JUENGSTEN Ergebnisses eines Projekts; 0 = noch kein Lauf
        /// (iU9-W10b.0b, Befund W10-B35).
        ///
        /// <para><b>Warum als eigene Methode.</b> Der Aufruf stand als inline-SQL in der
        /// Anzeigeschicht (<c>Form_Simulation_Config.Karten.TObenSammeln</c>:2165-2190)
        /// und ist dort mit dem Port der Maske entfallen. Zwei Abfragen statt einer
        /// Unterabfrage bleiben: Erst der Ergebniskopf, dann seine Speicherzeilen — ein
        /// Parameter in der Unterabfrage ist bei ACE eine bekannte Falle, und die
        /// Kopfabfrage ist ohnehin billig.</para>
        /// </summary>
        public static int LetzteErgebnisId(int idProjekt)
        {
            if (idProjekt <= 0) return 0;

            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT MAX(ID) FROM [" + TAB_KOPF + "] WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt)));
        }

        private static int I(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToInt32(r[col]) : 0; }
        /// <summary>Wie <see cref="D"/>, aber NULL bleibt NULL (P1-Vorgriff T_oben_*).</summary>
        private static double? DN(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? (double?)Convert.ToDouble(r[col]) : null; }
        private static double D(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToDouble(r[col]) : 0.0; }
        private static bool B(DataRow r, string col)
        { return r.Table.Columns.Contains(col) && r[col] != DBNull.Value && Convert.ToBoolean(r[col]); }
        private static string S(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? r[col].ToString() : ""; }
    }
}
