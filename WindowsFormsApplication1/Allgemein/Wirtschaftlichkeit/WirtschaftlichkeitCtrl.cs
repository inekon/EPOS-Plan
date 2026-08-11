using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Steuerung der Wirtschaftlichkeitsberechnung (Konzept_Wirtschaftlichkeit.md
    /// Kap. 5.7; Phase 6 = Ausbaustufe W1).
    ///
    /// Liest ausschließlich Tab_ProjektWerte, Tab_Ergebnis*, energy_* und
    /// Tab_ProjektWirtschaftlichkeit; schreibt Tab_ErgebnisWirtschaftlichkeit —
    /// keine UI-Abhängigkeit. Der UI-Reiter (Form_Wirtschaftlichkeit) und der
    /// Berichts-Baustein lesen dieselben persistierten Ergebnisse.
    ///
    /// Zahlungsgerüst W1 je Projekt und Szenario (Worst/Erwartet/Best):
    ///  - I₀ = Σ Tab_ProjektWerte Kategorie 1 (Szenariospalten Best/WorstCase;
    ///    0/leer → Erwartungswert), Nutzungsdauer analog → Ersatz + Restwert.
    ///  - Betriebskosten p. a. = Σ Kategorie 2 (Szenariowert).
    ///  - Energiekosten p. a. aus dem KostenEmissionRechner (Preise der Kosten-
    ///    maske; Entscheidung 11.08.2026 — keine Doppelpflege), alle Szenarien
    ///    identisch (Preisszenarien folgen mit W2).
    ///  - Erlöse = PV-Überschuss × Einspeisevergütung (Parameter).
    /// Referenz = Stammprojekt: KapitalwertDiff/Annuität/Amortisation der Variante
    /// entstehen aus der Differenz-Zahlungsreihe Variante − Stamm.
    /// </summary>
    public class WirtschaftlichkeitCtrl : IWirtschaftlichkeitProvider
    {
        public const string TAB_PARAMETER = "Tab_ProjektWirtschaftlichkeit";
        public const string TAB_ERGEBNIS = "Tab_ErgebnisWirtschaftlichkeit";

        // ------------------------------------------------------------- Tabellen

        /// <summary>Legt Parameter- und Ergebnistabelle an, falls sie fehlen
        /// (Muster BerichtCtrl.StelleKonfigTabelleSicher).</summary>
        public void StelleTabellenSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    if (!TabelleVorhanden(conn, TAB_PARAMETER))
                        Ddl(conn, "CREATE TABLE " + TAB_PARAMETER + " (" +
                                  "ID LONG CONSTRAINT PK_ProjWirt PRIMARY KEY, " +
                                  "ID_Projekt LONG CONSTRAINT UQ_ProjWirtProj UNIQUE, " +
                                  "Zinssatz DOUBLE, " +
                                  "Betrachtungszeitraum LONG, " +
                                  "Preissteigerung_Energie DOUBLE, " +
                                  "Preissteigerung_Betrieb DOUBLE, " +
                                  "Einspeiseverguetung DOUBLE, " +
                                  "GeaendertAm DATETIME)");
                    if (!TabelleVorhanden(conn, TAB_ERGEBNIS))
                        Ddl(conn, "CREATE TABLE " + TAB_ERGEBNIS + " (" +
                                  "ID LONG CONSTRAINT PK_ErgWirt PRIMARY KEY, " +
                                  "ID_Projekt LONG, " +
                                  "ID_Ergebnis LONG, " +          // FK auf Tab_Ergebnis.ID (Simulationslauf)
                                  "Szenario TEXT(20), " +
                                  "IstStamm YESNO, " +
                                  "Anzeige TEXT(255), " +
                                  "Zeitstempel DATETIME, " +
                                  "Zinssatz DOUBLE, " +
                                  "Betrachtungszeitraum LONG, " +
                                  "Preissteigerung_Energie DOUBLE, " +
                                  "Preissteigerung_Betrieb DOUBLE, " +
                                  "Einspeiseverguetung DOUBLE, " +
                                  "Investition DOUBLE, " +
                                  "Betriebskosten DOUBLE, " +
                                  "Energiekosten DOUBLE, " +
                                  "Einspeiseerloes DOUBLE, " +
                                  "BarwertAusgaben DOUBLE, " +
                                  "BarwertEinnahmen DOUBLE, " +
                                  "Restwert DOUBLE, " +
                                  "Kapitalwert DOUBLE, " +
                                  "KapitalwertDiff DOUBLE, " +
                                  "AnnuitaetKW DOUBLE, " +
                                  "AmortisationJahre DOUBLE, " +
                                  "Gestehungskosten DOUBLE, " +
                                  "Fehlgrund LONGTEXT)");

                    // Ältere Tabellenstände additiv nachrüsten (Muster
                    // ErgebnisCtrl.StelleModulSpaltenSicher) — CREATE erfasst nur Neuanlagen.
                    SpalteSicher(conn, TAB_ERGEBNIS, "IstStamm", "YESNO");
                    SpalteSicher(conn, TAB_ERGEBNIS, "Anzeige", "TEXT(255)");
                }
            }
            catch { /* ohne Tabellen laufen Laden/Speichern in ihre eigenen Fänge */ }
        }

        /// <summary>Fügt eine fehlende Spalte per ALTER TABLE hinzu (still, additiv).</summary>
        private static void SpalteSicher(OleDbConnection conn, string tabelle, string spalte, string typ)
        {
            try
            {
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                    new object[] { null, null, tabelle, spalte });
                if (schema != null && schema.Rows.Count > 0) return;
                Ddl(conn, "ALTER TABLE " + tabelle + " ADD COLUMN [" + spalte + "] " + typ);
            }
            catch { }
        }

        private static bool TabelleVorhanden(OleDbConnection conn, string name)
        {
            DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                new object[] { null, null, name, "TABLE" });
            return schema != null && schema.Rows.Count > 0;
        }

        private static void Ddl(OleDbConnection conn, string sql)
        {
            using (OleDbCommand cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        // ------------------------------------------------------------- Parameter

        public WirtschaftlichkeitParameter LadeParameter(int idStamm)
        {
            StelleTabellenSicher();
            var p = new WirtschaftlichkeitParameter { IdStamm = idStamm };
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_PARAMETER + " WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", idStamm));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    p.Zinssatz = D(r, "Zinssatz") ?? p.Zinssatz;
                    p.Betrachtungszeitraum = (int)(D(r, "Betrachtungszeitraum") ?? p.Betrachtungszeitraum);
                    p.PreissteigerungEnergie = D(r, "Preissteigerung_Energie") ?? 0;
                    p.PreissteigerungBetrieb = D(r, "Preissteigerung_Betrieb") ?? 0;
                    p.Einspeiseverguetung = D(r, "Einspeiseverguetung") ?? 0;
                    if (r["GeaendertAm"] != DBNull.Value) p.GeaendertAm = Convert.ToDateTime(r["GeaendertAm"]);
                }
            }
            catch { }
            if (p.Betrachtungszeitraum <= 0) p.Betrachtungszeitraum = 20;
            return p;
        }

        public bool SpeichereParameter(WirtschaftlichkeitParameter p)
        {
            if (p == null || p.IdStamm <= 0) return false;
            StelleTabellenSicher();
            try
            {
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_PARAMETER + " SET Zinssatz = ?, Betrachtungszeitraum = ?, " +
                    "Preissteigerung_Energie = ?, Preissteigerung_Betrieb = ?, " +
                    "Einspeiseverguetung = ?, GeaendertAm = ? WHERE ID_Projekt = ?",
                    new OleDbParameter("@z", p.Zinssatz),
                    new OleDbParameter("@t", p.Betrachtungszeitraum),
                    new OleDbParameter("@pe", p.PreissteigerungEnergie),
                    new OleDbParameter("@pb", p.PreissteigerungBetrieb),
                    new OleDbParameter("@ev", p.Einspeiseverguetung),
                    new OleDbParameter("@am", OleDbType.Date) { Value = DateTime.Now },
                    new OleDbParameter("@p", p.IdStamm));
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_PARAMETER, "ID") + 1;
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_PARAMETER + " (ID, ID_Projekt, Zinssatz, Betrachtungszeitraum, " +
                    "Preissteigerung_Energie, Preissteigerung_Betrieb, Einspeiseverguetung, GeaendertAm) " +
                    "VALUES (?,?,?,?,?,?,?,?)",
                    new OleDbParameter("@id", id),
                    new OleDbParameter("@p", p.IdStamm),
                    new OleDbParameter("@z", p.Zinssatz),
                    new OleDbParameter("@t", p.Betrachtungszeitraum),
                    new OleDbParameter("@pe", p.PreissteigerungEnergie),
                    new OleDbParameter("@pb", p.PreissteigerungBetrieb),
                    new OleDbParameter("@ev", p.Einspeiseverguetung),
                    new OleDbParameter("@am", OleDbType.Date) { Value = DateTime.Now });
            }
            catch { return false; }
        }

        // ------------------------------------------------------------- Berechnung

        /// <summary>
        /// Rechnet alle Szenarien für die gesammelte Vergleichsgruppe und
        /// persistiert die Ergebnisse. daten stammt aus BerichtsDatenSammler.Sammle
        /// (dort ist die Vorbedingung „Simulation vorhanden/aktuell" bereits
        /// erledigt, inkl. automatischem Rechnen fehlender Ergebnisse).
        /// </summary>
        public List<WirtschaftlichkeitErgebnis> Berechne(BerichtsDaten daten, WirtschaftlichkeitParameter p)
        {
            var alle = new List<WirtschaftlichkeitErgebnis>();
            if (daten == null || daten.Varianten.Count == 0 || p == null) return alle;
            StelleTabellenSicher();

            foreach (string szenario in WirtschaftlichkeitSzenario.Alle)
            {
                KapitalwertRechner.Zahlungsbild stammBild = null;
                WirtschaftlichkeitErgebnis stammErg = null;

                foreach (VariantenDaten v in daten.Varianten)
                {
                    WirtschaftlichkeitErgebnis erg = RechneProjekt(v, p, szenario, out KapitalwertRechner.Zahlungsbild bild);
                    alle.Add(erg);

                    if (v.IstStamm) { stammBild = bild; stammErg = erg; continue; }

                    // Referenz = Stamm (Entscheidung 11.08.2026): Differenzkennzahlen.
                    if (bild != null && stammBild != null &&
                        erg.Kapitalwert.HasValue && stammErg != null && stammErg.Kapitalwert.HasValue)
                    {
                        erg.KapitalwertDiff = erg.Kapitalwert.Value - stammErg.Kapitalwert.Value;
                        erg.AnnuitaetKW = erg.KapitalwertDiff.Value *
                            KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
                        erg.AmortisationJahre = KapitalwertRechner.AmortisationDifferenz(bild, stammBild);
                    }
                }
            }

            Persistiere(alle, p);
            return alle;
        }

        /// <summary>Absolutes Zahlungsbild + Kennzahlen eines Projekts für ein Szenario.</summary>
        private WirtschaftlichkeitErgebnis RechneProjekt(VariantenDaten v, WirtschaftlichkeitParameter p,
                                                         string szenario, out KapitalwertRechner.Zahlungsbild bild)
        {
            bild = null;
            var erg = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = v.IdProjekt,
                IdErgebnis = LiesErgebnisId(v.IdProjekt),
                Szenario = szenario,
                IstStamm = v.IstStamm,
                Anzeige = v.Anzeige
            };

            if (v.Fehler != null || v.Ergebnis == null)
            { erg.Fehlgrund = v.Fehler ?? "Kein Simulationsergebnis vorhanden."; return erg; }

            // ---------------- Zahlungsgerüst aus Tab_ProjektWerte ----------------
            List<KapitalwertRechner.InvestPosition> investitionen = LiesInvestitionen(v.IdProjekt, szenario);
            double betrieb = LiesBetriebskosten(v.IdProjekt, szenario);
            erg.BetriebskostenJahr = betrieb;
            erg.EnergiekostenJahr = v.Energiekosten;   // KostenEmissionRechner (Phase 5)

            double pvUeberschussMWh = v.Ergebnis.Photovoltaik != null ? v.Ergebnis.Photovoltaik.Ueberschuss : 0;
            erg.EinspeiseerloesJahr = pvUeberschussMWh * 1000.0 * p.Einspeiseverguetung;

            foreach (KapitalwertRechner.InvestPosition pos in investitionen) erg.Investition += pos.Betrag;

            if (!v.Energiekosten.HasValue)
            {
                // Ohne Energiekosten fehlt der größte Posten — Kennzahlen bleiben „—".
                erg.Fehlgrund = "Energiekosten nicht bestimmbar — Arbeitspreise/Träger in der " +
                                "Kostenmaske (Energiekosten) prüfen.";
                return erg;
            }

            // ---------------- Kapitalwert ----------------
            bild = KapitalwertRechner.Rechne(investitionen, betrieb, v.Energiekosten.Value,
                erg.EinspeiseerloesJahr, p.Zinssatz, p.Betrachtungszeitraum,
                p.PreissteigerungBetrieb, p.PreissteigerungEnergie);

            erg.BarwertAusgaben = bild.BarwertAusgaben;
            erg.BarwertEinnahmen = bild.BarwertEinnahmen;
            erg.RestwertBarwert = bild.RestwertBarwert;
            erg.Kapitalwert = bild.Kapitalwert;

            // Wärmegestehungskosten: annuisierte Nettokosten ÷ Jahreswärmebedarf.
            double waermeMWh = v.Ergebnis.Energiebedarf != null ? v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt : 0;
            if (waermeMWh > 0)
            {
                double a = KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
                erg.Gestehungskosten = (-bild.Kapitalwert * a) / (waermeMWh * 1000.0);
            }
            return erg;
        }

        /// <summary>Kategorie-1-Positionen (Investitionen) mit Szenariowerten.
        /// Best/WorstCase bzw. …_Nutzungsdauer: 0/leer → Erwartungswert (VALERI-Muster).</summary>
        private static List<KapitalwertRechner.InvestPosition> LiesInvestitionen(int idProjekt, string szenario)
        {
            var liste = new List<KapitalwertRechner.InvestPosition>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT EingegebenerWert, BestCase, WorstCase, Nutzungsdauer, " +
                    "BestCase_Nutzungsdauer, WorstCase_Nutzungsdauer " +
                    "FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = 1",
                    new OleDbParameter("@p", idProjekt));
                if (dt == null) return liste;
                foreach (DataRow r in dt.Rows)
                {
                    double betrag = Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
                    double dauer = Szenariowert(r, szenario, "Nutzungsdauer",
                                                "BestCase_Nutzungsdauer", "WorstCase_Nutzungsdauer");
                    if (betrag != 0)
                        liste.Add(new KapitalwertRechner.InvestPosition { Betrag = betrag, Nutzungsdauer = dauer });
                }
            }
            catch { }
            return liste;
        }

        /// <summary>Summe der Kategorie-2-Positionen (Betriebskosten p. a., Szenariowert).</summary>
        private static double LiesBetriebskosten(int idProjekt, string szenario)
        {
            double summe = 0;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT EingegebenerWert, BestCase, WorstCase " +
                    "FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = 2",
                    new OleDbParameter("@p", idProjekt));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        summe += Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
            }
            catch { }
            return summe;
        }

        private static double Szenariowert(DataRow r, string szenario,
                                           string spalteErwartet, string spalteBest, string spalteWorst)
        {
            double erwartet = D(r, spalteErwartet) ?? 0;
            string spalte = szenario == WirtschaftlichkeitSzenario.BEST ? spalteBest
                          : szenario == WirtschaftlichkeitSzenario.WORST ? spalteWorst : null;
            if (spalte == null) return erwartet;
            double wert = D(r, spalte) ?? 0;
            return wert != 0 ? wert : erwartet;   // 0/leer = kein Szenariowert gepflegt
        }

        /// <summary>ID des jüngsten Simulationslaufs (Tab_Ergebnis) des Projekts, 0 = keiner.</summary>
        private static int LiesErgebnisId(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 ID FROM " + ErgebnisCtrl.TAB_KOPF +
                    " WHERE ID_Projekt = ? ORDER BY ID DESC",
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            catch { }
            return 0;
        }

        // ------------------------------------------------------------- Persistenz

        /// <summary>
        /// Ersetzt die gespeicherten Ergebnisse der beteiligten Projekte — in EINER
        /// Transaktion über eine eigene Verbindung: kein Teilstand bei Fehlern, und
        /// keine modalen Fehlerdialoge des DataRepository aus dem Hintergrundthread
        /// (Berechne läuft im Task). Ein Persistenzfehler kippt die Anzeige nicht.
        /// </summary>
        private void Persistiere(List<WirtschaftlichkeitErgebnis> ergebnisse, WirtschaftlichkeitParameter p)
        {
            var projektIds = new HashSet<int>();
            foreach (WirtschaftlichkeitErgebnis e in ergebnisse) projektIds.Add(e.IdProjekt);

            try
            {
                using (var conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (int id in projektIds)
                                using (var cmd = new OleDbCommand(
                                    "DELETE FROM " + TAB_ERGEBNIS + " WHERE ID_Projekt = ?", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@p", id);
                                    cmd.ExecuteNonQuery();
                                }

                            int naechsteId;
                            using (var cmd = new OleDbCommand(
                                "SELECT MAX(ID) FROM " + TAB_ERGEBNIS, conn, tx))
                            {
                                object o = cmd.ExecuteScalar();
                                naechsteId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                            }

                            foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
                            {
                                using (var cmd = new OleDbCommand(
                                    "INSERT INTO " + TAB_ERGEBNIS + " (ID, ID_Projekt, ID_Ergebnis, Szenario, " +
                                    "IstStamm, Anzeige, Zeitstempel, " +
                                    "Zinssatz, Betrachtungszeitraum, Preissteigerung_Energie, Preissteigerung_Betrieb, " +
                                    "Einspeiseverguetung, Investition, Betriebskosten, Energiekosten, Einspeiseerloes, " +
                                    "BarwertAusgaben, BarwertEinnahmen, Restwert, Kapitalwert, KapitalwertDiff, " +
                                    "AnnuitaetKW, AmortisationJahre, Gestehungskosten, Fehlgrund) " +
                                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", conn, tx))
                                {
                                    OleDbParameterCollection ps = cmd.Parameters;
                                    ps.AddWithValue("@id", naechsteId);
                                    ps.AddWithValue("@proj", e.IdProjekt);
                                    ps.AddWithValue("@erg", e.IdErgebnis);
                                    ps.AddWithValue("@sz", e.Szenario ?? "");
                                    ps.AddWithValue("@stamm", e.IstStamm);
                                    ps.AddWithValue("@anz", e.Anzeige ?? "");
                                    ps.Add(new OleDbParameter("@zeit", OleDbType.Date) { Value = e.Zeitstempel });
                                    ps.AddWithValue("@z", p.Zinssatz);
                                    ps.AddWithValue("@t", p.Betrachtungszeitraum);
                                    ps.AddWithValue("@pe", p.PreissteigerungEnergie);
                                    ps.AddWithValue("@pb", p.PreissteigerungBetrieb);
                                    ps.AddWithValue("@ev", p.Einspeiseverguetung);
                                    ps.AddWithValue("@inv", R(e.Investition));
                                    ps.Add(DbWert(e.BetriebskostenJahr));
                                    ps.Add(DbWert(e.EnergiekostenJahr));
                                    ps.AddWithValue("@einsp", R(e.EinspeiseerloesJahr));
                                    ps.Add(DbWert(e.BarwertAusgaben));
                                    ps.Add(DbWert(e.BarwertEinnahmen));
                                    ps.AddWithValue("@rw", R(e.RestwertBarwert));
                                    ps.Add(DbWert(e.Kapitalwert));
                                    ps.Add(DbWert(e.KapitalwertDiff));
                                    ps.Add(DbWert(e.AnnuitaetKW));
                                    ps.Add(DbWert(e.AmortisationJahre));
                                    ps.Add(DbWert(e.Gestehungskosten, 6));
                                    ps.AddWithValue("@fg", (object)e.Fehlgrund ?? DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                                naechsteId++;
                            }
                            tx.Commit();
                        }
                        catch
                        {
                            try { tx.Rollback(); } catch { }
                            throw;
                        }
                    }
                }
            }
            catch { /* Ergebnisse bleiben im Speicher; der Reiter meldet beim nächsten Laden den alten Stand */ }
        }

        /// <summary>Persistierte Ergebnisse laden (IWirtschaftlichkeitProvider).</summary>
        public List<WirtschaftlichkeitErgebnis> LadeErgebnisse(List<int> projektIds)
        {
            var liste = new List<WirtschaftlichkeitErgebnis>();
            if (projektIds == null || projektIds.Count == 0) return liste;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM " + TAB_ERGEBNIS + " WHERE ID_Projekt = ?",
                        new OleDbParameter("@p", idProjekt));
                    if (dt == null) continue;
                    foreach (DataRow r in dt.Rows)
                    {
                        var e = new WirtschaftlichkeitErgebnis
                        {
                            IdProjekt = idProjekt,
                            IdErgebnis = (int)(D(r, "ID_Ergebnis") ?? 0),
                            Szenario = r["Szenario"] != DBNull.Value ? r["Szenario"].ToString()
                                                                     : WirtschaftlichkeitSzenario.ERWARTET,
                            IstStamm = B(r, "IstStamm"),
                            Anzeige = r.Table.Columns.Contains("Anzeige") && r["Anzeige"] != DBNull.Value
                                      ? r["Anzeige"].ToString() : "",
                            Investition = D(r, "Investition") ?? 0,
                            BetriebskostenJahr = D(r, "Betriebskosten"),
                            EnergiekostenJahr = D(r, "Energiekosten"),
                            EinspeiseerloesJahr = D(r, "Einspeiseerloes") ?? 0,
                            BarwertAusgaben = D(r, "BarwertAusgaben"),
                            BarwertEinnahmen = D(r, "BarwertEinnahmen"),
                            RestwertBarwert = D(r, "Restwert") ?? 0,
                            Kapitalwert = D(r, "Kapitalwert"),
                            KapitalwertDiff = D(r, "KapitalwertDiff"),
                            AnnuitaetKW = D(r, "AnnuitaetKW"),
                            AmortisationJahre = D(r, "AmortisationJahre"),
                            Gestehungskosten = D(r, "Gestehungskosten"),
                            Fehlgrund = r["Fehlgrund"] != DBNull.Value ? r["Fehlgrund"].ToString() : null
                        };
                        if (r["Zeitstempel"] != DBNull.Value) e.Zeitstempel = Convert.ToDateTime(r["Zeitstempel"]);
                        liste.Add(e);
                    }
                }
            }
            catch { }
            return liste;
        }

        /// <summary>true, wenn ein gespeichertes Ergebnis zum aktuellen Simulationslauf passt.</summary>
        public bool ErgebnisAktuell(WirtschaftlichkeitErgebnis e)
        {
            return e != null && e.Fehlgrund == null &&
                   e.IdErgebnis > 0 && e.IdErgebnis == LiesErgebnisId(e.IdProjekt);
        }

        // ------------------------------------------------------------- Hilfen

        private static bool B(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return false;
            try { return Convert.ToBoolean(r[spalte]); } catch { return false; }
        }

        private static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        private static double R(double v, int dez = 2) { return Math.Round(v, dez); }

        private static OleDbParameter DbWert(double? v, int dez = 2)
        {
            return new OleDbParameter("@w", OleDbType.Double)
            { Value = v.HasValue ? (object)Math.Round(v.Value, dez) : DBNull.Value };
        }
    }
}
