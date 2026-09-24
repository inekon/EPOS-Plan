using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Bauteilaufbauten samt Schichten</b> — Katalog und Projektkopie (Gebäudesimulation
    /// Stufe G3, Schritt S-B; Softwarearchitektur 2.2, 2.6 und 2.9).
    ///
    /// <para><b>Der Aufbau ist ein Aggregat.</b> Gelesen, geschrieben und kopiert wird er mit
    /// seinen Schichten; ein Aufbau ohne Schichten ist ein Aufbau ohne U-Wert. Das Speichern
    /// läuft in EINER Transaktion: Kopf anlegen oder ändern, die Schichten des Aufbaus löschen
    /// und in Listenreihenfolge neu anlegen, <c>Reihenfolge</c> lückenlos ab 1. Die Schichten
    /// dürfen neue Schlüssel bekommen — auf eine Schicht zeigt nichts; auf den Aufbau zeigen die
    /// Bauteile, und sein Schlüssel bleibt.</para>
    ///
    /// <para><b>Die Stoffwerte der Schicht sind eine Kopie</b> zum Zeitpunkt der Zuordnung:
    /// Trägt eine Schicht einen Baustoff, übernimmt das Speichern jeden fehlenden Wert (λ, ρ, c)
    /// aus ihm; ein angegebener Wert bleibt. <c>ID_Baustoff</c> zeigt je Seite auf die eigene
    /// Ablage (W11) — der Katalog auf <c>Tab_Baustoff_STAMM</c>, die Kopie auf <c>Tab_Baustoff</c>
    /// desselben Projekts.</para>
    ///
    /// <para><b>Lesewege</b> (2.9): je Aufbau und je Projekt in ZWEI Abfragen — die Aufbauten und
    /// alle ihre Schichten, sortiert nach (<c>ID_Aufbau</c>, <c>Reihenfolge</c>); die Zuordnung
    /// geschieht im Speicher. Nie eine Abfrage je Aufbau.</para>
    /// </summary>
    public sealed class BauteilaufbauCtrl
    {
        /// <summary>Was ein Schreibversuch ergeben hat — dieselbe Form wie <see cref="BaustoffCtrl.Ergebnis"/>.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung, int Id)
        {
            internal static Ergebnis Gut(int id) => new Ergebnis(true, "", id);
            internal static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "", 0);
        }

        private const string SCHICHTSPALTEN_SQL =
            "\"ID\", \"ID_Aufbau\", \"Reihenfolge\", \"ID_Baustoff\", \"Dicke\", \"IstLuftschicht\", \"Lambda\", \"Rho\", \"cp\"";

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Ein Katalogaufbau samt Schichten; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public BauteilaufbauModel LesenKatalogsatz(int id) => LesenEinen(Seite.Katalog, id);

        /// <summary>Ein Projektaufbau samt Schichten; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public BauteilaufbauModel LesenProjektsatz(int id) => LesenEinen(Seite.Projekt, id);

        /// <summary>
        /// Der Katalog samt Schichten, wahlweise auf eine Bauteilart eingeengt — die Einengung
        /// steht in der Abfrage; <c>null</c> = alle. Zwei Abfragen.
        /// </summary>
        public List<BauteilaufbauModel> LesenKatalog(string bauteilart = null)
        {
            string bedingung = bauteilart == null ? "" : " WHERE a.\"Bauteilart\" = ?";
            DbParam[] p = bauteilart == null ? new DbParam[0] : new[] { new DbParam("@art", bauteilart) };
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT a.* FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" a" + bedingung +
                " ORDER BY a.\"Bezeichner\", a.\"ID\"", p);
            DataTable schichten = DataRepository.GetDataTable(
                "SELECT " + Praefix("s", SCHICHTSPALTEN_SQL) + " FROM \"" + BauteilaufbauSchema.TAB_SCHICHT_STAMM + "\" s " +
                "INNER JOIN \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" a ON a.\"ID\" = s.\"ID_Aufbau\"" + bedingung +
                " ORDER BY s.\"ID_Aufbau\", s.\"Reihenfolge\", s.\"ID\"", p);
            return Zusammenfuehren(kopf, schichten);
        }

        /// <summary>
        /// <b>Der Leseweg des Rechenkerns</b> (2.9): alle Aufbauten eines Projekts samt Schichten in
        /// ZWEI Abfragen, die Schichten nach (<c>ID_Aufbau</c>, <c>Reihenfolge</c>).
        /// </summary>
        public List<BauteilaufbauModel> LesenJeProjekt(int idProjekt)
        {
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT * FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID_Projekt\" = ? ORDER BY \"Bezeichner\", \"ID\"",
                new DbParam("@p", idProjekt));
            DataTable schichten = DataRepository.GetDataTable(
                "SELECT " + Praefix("s", SCHICHTSPALTEN_SQL) + " FROM \"" + BauteilaufbauSchema.TAB_SCHICHT + "\" s " +
                "INNER JOIN \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" a ON a.\"ID\" = s.\"ID_Aufbau\" " +
                "WHERE a.\"ID_Projekt\" = ? ORDER BY s.\"ID_Aufbau\", s.\"Reihenfolge\", s.\"ID\"",
                new DbParam("@p", idProjekt));
            return Zusammenfuehren(kopf, schichten);
        }

        /// <summary>
        /// <b>Die Zeilen der Katalogverwaltung</b> nach <see cref="Katalogfilterprofil.FuerBauteilaufbau"/>
        /// — EINE Abfrage mit Schichtzahl und Gesamtdicke.
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var liste = new List<Katalogfilterzeile>();
            DataTable dt = StilleDb.Tabelle(
                "SELECT a.\"ID\", a.\"Bezeichner\", a.\"Bauteilart\", a.\"Herkunft\", a.\"ReadOnly\", " +
                "COUNT(s.\"ID\") AS Schichten, SUM(s.\"Dicke\") AS Dicke " +
                "FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" a " +
                "LEFT JOIN \"" + BauteilaufbauSchema.TAB_SCHICHT_STAMM + "\" s ON s.\"ID_Aufbau\" = a.\"ID\" " +
                "GROUP BY a.\"ID\" ORDER BY a.\"Bezeichner\"");
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                string bezeichner = Katalogfeld.Text(r, "Bezeichner");
                var zeile = new Katalogfilterzeile(Katalogfeld.Ganzzahl(r, "ID"), bezeichner)
                {
                    Geschuetzt = Katalogfeld.Kennzeichen(r, "ReadOnly")
                };
                liste.Add(zeile
                    .MitText(Katalogfilterprofil.SpBezeichner, bezeichner)
                    .MitText(Katalogfilterprofil.SpBauteilart, Katalogfeld.Text(r, "Bauteilart"))
                    .MitZahl(Katalogfilterprofil.SpSchichten, Katalogfeld.Zahl(r, "Schichten"), 0)
                    .MitZahl(Katalogfilterprofil.SpDicke, Katalogfeld.Zahl(r, "Dicke"), 3)
                    .MitText(Katalogfilterprofil.SpHerkunft, Katalogfeld.Text(r, "Herkunft")));
            }
            return liste;
        }

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Die Prüfung eines Aufbaus samt Schichten vor dem Schreiben, ohne Datenbank — <c>null</c>
        /// = gültig. Name Pflicht, Längen nach Schema, Bauteilart und Herkunft aus ihren Listen,
        /// jede Schicht mit Dicke größer null und Stoffwerten — wenn angegeben — im Band.
        /// </summary>
        public static string Pruefen(BauteilaufbauModel m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.Bezeichner)) return MyResource.Resource.BAUTEIL_MSG_AUFBAU_NAME_LEER;
            string t = BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_NAME, m.Bezeichner, BaustoffSchema.LAENGE_BEZEICHNER)
                       ?? BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_BESCHREIBUNG, m.Beschreibung, BauteilaufbauSchema.LAENGE_BESCHREIBUNG)
                       ?? BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_QUELLE, m.Quelle, BaustoffSchema.LAENGE_QUELLE)
                       ?? BaustoffCtrl.Laenge(MyResource.Resource.BAUTEIL_FELD_QUELLKENNUNG, m.Quellkennung, BaustoffSchema.LAENGE_QUELLKENNUNG)
                       ?? BaustoffCtrl.HerkunftPruefen(m.Herkunft)
                       ?? BauteilartPruefen(m.Bauteilart, true);
            if (t != null) return t;

            int nr = 0;
            foreach (BauteilschichtModel s in m.Schichten ?? new List<BauteilschichtModel>())
            {
                nr++;
                if (s == null) continue;
                if (!(s.Dicke > 0) || double.IsInfinity(s.Dicke))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_DICKE, nr);
                string w = BaustoffCtrl.StoffwertePruefen(s.Lambda, s.Rho, s.Cp);
                if (w != null) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_WERT, nr, w);
            }
            return null;
        }

        /// <summary>Die Bauteilart ist einer der neun Werte — oder NULL, wo NULL zulässig ist.</summary>
        internal static string BauteilartPruefen(string art, bool nullErlaubt)
        {
            if ((art == null && nullErlaubt) || (art != null && DbWerte.BAUTEILARTEN.Contains(art))) return null;
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_BAUTEILART, art ?? "");
        }

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>
        /// Speichert einen KATALOGaufbau samt Schichten in EINER Transaktion — Id ≤ 0 legt an
        /// (<c>ReadOnly = 0</c>, ohne Herkunft MANUELL), sonst wird geändert. Ein Aufbau der
        /// Auslieferung wird nicht geändert.
        /// </summary>
        public Ergebnis KatalogSpeichern(BauteilaufbauModel m) => Speichern(Seite.Katalog, null, m);

        /// <summary>Speichert einen PROJEKTaufbau samt Schichten in EINER Transaktion.</summary>
        public Ergebnis ProjektSpeichern(int idProjekt, BauteilaufbauModel m) => Speichern(Seite.Projekt, idProjekt, m);

        /// <summary>Löscht einen Katalogaufbau samt Schichten (Kaskade) — nie einen der Auslieferung.</summary>
        public Ergebnis KatalogLoeschen(int id)
        {
            BauteilaufbauModel alt = LesenKatalogsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            if (alt.ReadOnly) return Ergebnis.Fehler(Schreibgeschuetzt(alt.Bezeichner));
            int n = DataRepository.ExecuteNonQuery(
                "DELETE FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            return n == 1 ? Ergebnis.Gut(id) : Ergebnis.Fehler(NichtGefunden(id));
        }

        /// <summary>
        /// Löscht einen Projektaufbau samt Schichten — nie einen, auf den ein Bauteil zeigt (der
        /// Fremdschlüssel ist restriktiv; die Prüfung nennt die Zahl vorher).
        /// </summary>
        public Ergebnis ProjektLoeschen(int id)
        {
            BauteilaufbauModel alt = LesenProjektsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            long benutzt = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_BAUTEIL + "\" WHERE \"ID_Aufbau\" = ?", new DbParam("@id", id)),
                CultureInfo.InvariantCulture);
            if (benutzt > 0)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_VERWENDET,
                                                     alt.Bezeichner, benutzt));
            int n = DataRepository.ExecuteNonQuery(
                "DELETE FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            return n == 1 ? Ergebnis.Gut(id) : Ergebnis.Fehler(NichtGefunden(id));
        }

        /// <summary>
        /// <b>„Schloss setzen…" / „Schloss aufheben…"</b> — nur das Auslieferungskennzeichen der
        /// Katalogaufbauten; die Schichten folgen ihrem Aufbau (L1).
        /// </summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt)
            => Auslieferungskennzeichen.SetzenInTabelle(BauteilaufbauSchema.TAB_AUFBAU_STAMM, ids, gesperrt);

        // =================================================================
        //  Katalog → Projekt
        // =================================================================

        /// <summary>
        /// Kopiert einen Katalogaufbau samt Schichten in das Projekt (Softwarearchitektur 2.6) —
        /// sofern das Projekt keinen Aufbau dieses Namens führt. <b>Die Stoffe der Schichten reisen
        /// mit</b>: Jeder Katalogstoff wird über <see cref="BaustoffCtrl"/> in das Projekt kopiert
        /// (oder die vorhandene Kopie genommen), und <c>ID_Baustoff</c> der Schicht zeigt danach auf
        /// die Projektkopie (W11). Aufbau, Stoffe und Schichten in EINER Transaktion; die Kopie
        /// trägt die Herkunft <see cref="DbWerte.HERKUNFT_KATALOG"/>, alle übrigen Werte
        /// NULL-erhaltend über <see cref="BauteilaufbauSchema.Fachspalten"/>.
        /// </summary>
        /// <returns>Die Id des kopierten ODER vorhandenen Projektaufbaus, <c>-1</c> bei Fehler.</returns>
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            if (idProjekt <= 0) return -1;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    DataTable dt = v.Lese("SELECT * FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" WHERE \"ID\" = ?",
                                          new DbParam("@id", stammId));
                    if (dt == null || dt.Rows.Count == 0) { v.Rollback(); return -1; }
                    DataRow s = dt.Rows[0];
                    string bezeichner = Convert.ToString(s["Bezeichner"], CultureInfo.InvariantCulture);

                    object vorhanden = v.Skalar(
                        "SELECT \"ID\" FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID_Projekt\" = ? AND \"Bezeichner\" = ? " +
                        "ORDER BY \"ID\" LIMIT 1", new DbParam("@p", idProjekt), new DbParam("@b", bezeichner));
                    if (vorhanden != null)
                    {
                        v.Rollback();
                        return Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);
                    }

                    var ps = new List<DbParam> { new DbParam("@p", idProjekt), new DbParam("@b", bezeichner) };
                    foreach (string spalte in BauteilaufbauSchema.Fachspalten)
                        ps.Add(spalte == BauteilaufbauSchema.SPALTE_HERKUNFT
                            ? new DbParam("@herkunft", DbWerte.HERKUNFT_KATALOG)
                            : new DbParam("@" + spalte, s.Table.Columns.Contains(spalte) ? s[spalte] : DBNull.Value));
                    int neu = v.EinfuegenUndId(
                        "INSERT INTO \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" (\"ID_Projekt\", \"Bezeichner\", " +
                        string.Join(", ", BauteilaufbauSchema.Fachspalten.Select(x => "\"" + x + "\"")) + ") VALUES (?, ?, " +
                        BaustoffCtrl.Fragezeichen(BauteilaufbauSchema.Fachspalten.Count) + ")", ps.ToArray());

                    DataTable schichten = v.Lese(
                        "SELECT " + SCHICHTSPALTEN_SQL + " FROM \"" + BauteilaufbauSchema.TAB_SCHICHT_STAMM + "\" " +
                        "WHERE \"ID_Aufbau\" = ? ORDER BY \"Reihenfolge\", \"ID\"", new DbParam("@a", stammId));
                    var stoffe = new Dictionary<int, int>();
                    int rang = 0;
                    foreach (DataRow r in schichten.Rows)
                    {
                        BauteilschichtModel sch = SchichtAus(r);
                        if (sch.ID_Baustoff.HasValue)
                        {
                            if (!stoffe.TryGetValue(sch.ID_Baustoff.Value, out int projektStoff))
                            {
                                projektStoff = BaustoffCtrl.CopyFromStamm(v, sch.ID_Baustoff.Value, idProjekt);
                                if (projektStoff <= 0) { v.Rollback(); return -1; }
                                stoffe[sch.ID_Baustoff.Value] = projektStoff;
                            }
                            sch.ID_Baustoff = projektStoff;
                        }
                        SchichtEinfuegen(v, BauteilaufbauSchema.TAB_SCHICHT, neu, ++rang, sch);
                    }

                    v.Commit();
                    return neu;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Aufbau " + stammId + " nicht in das Projekt " + idProjekt + " kopiert: " + ex.Message);
                return -1;
            }
        }

        // =================================================================
        //  intern
        // =================================================================

        private enum Seite { Katalog, Projekt }

        private static string AufbauTabelle(Seite s) => s == Seite.Katalog ? BauteilaufbauSchema.TAB_AUFBAU_STAMM : BauteilaufbauSchema.TAB_AUFBAU;
        private static string SchichtTabelle(Seite s) => s == Seite.Katalog ? BauteilaufbauSchema.TAB_SCHICHT_STAMM : BauteilaufbauSchema.TAB_SCHICHT;
        private static string StoffTabelle(Seite s) => s == Seite.Katalog ? BaustoffSchema.TAB_STAMM : BaustoffSchema.TAB_PROJEKT;

        private Ergebnis Speichern(Seite seite, int? idProjekt, BauteilaufbauModel m)
        {
            string fehler = Pruefen(m);
            if (fehler != null) return Ergebnis.Fehler(fehler);
            string tab = AufbauTabelle(seite);
            string name = m.Bezeichner.Trim();

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    bool neu = m.ID <= 0;
                    if (!neu)
                    {
                        DataTable alt = v.Lese("SELECT * FROM \"" + tab + "\" WHERE \"ID\" = ?", new DbParam("@id", m.ID));
                        if (alt.Rows.Count == 0) { v.Rollback(); return Ergebnis.Fehler(NichtGefunden(m.ID)); }
                        DataRow a = alt.Rows[0];
                        if (seite == Seite.Katalog && Convert.ToInt64(a["ReadOnly"], CultureInfo.InvariantCulture) != 0)
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(Schreibgeschuetzt(Convert.ToString(a["Bezeichner"], CultureInfo.InvariantCulture)));
                        }
                        if (seite == Seite.Projekt) idProjekt = Convert.ToInt32(a["ID_Projekt"], CultureInfo.InvariantCulture);
                    }

                    // Der Name ist auf seiner Seite frei (Katalog als Ganzes, Kopie je Projekt).
                    var np = new List<DbParam> { new DbParam("@b", name), new DbParam("@id", neu ? 0 : m.ID) };
                    string nsql = "SELECT COUNT(*) FROM \"" + tab + "\" WHERE \"Bezeichner\" = ? AND \"ID\" <> ?";
                    if (seite == Seite.Projekt) { nsql += " AND \"ID_Projekt\" = ?"; np.Add(new DbParam("@p", idProjekt ?? 0)); }
                    if (Convert.ToInt64(v.Skalar(nsql, np.ToArray()), CultureInfo.InvariantCulture) > 0)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_NAME_VERGEBEN, name));
                    }

                    // Die Stoffe der Schichten gehören zur eigenen Ablage (W11); fehlende Werte kommen aus ihnen.
                    int nr = 0;
                    foreach (BauteilschichtModel s in m.Schichten.Where(x => x != null))
                    {
                        nr++;
                        if (!s.ID_Baustoff.HasValue) continue;
                        string ssql = "SELECT * FROM \"" + StoffTabelle(seite) + "\" WHERE \"ID\" = ?";
                        var sp = new List<DbParam> { new DbParam("@s", s.ID_Baustoff.Value) };
                        if (seite == Seite.Projekt) { ssql += " AND \"ID_Projekt\" = ?"; sp.Add(new DbParam("@p", idProjekt ?? 0)); }
                        DataTable st = v.Lese(ssql, sp.ToArray());
                        if (st.Rows.Count == 0)
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_BAUSTOFF,
                                                                 nr, s.ID_Baustoff.Value));
                        }
                        s.Lambda ??= BaustoffCtrl.ZahlAus(st.Rows[0], "Lambda");
                        s.Rho ??= BaustoffCtrl.ZahlAus(st.Rows[0], "Rho");
                        s.Cp ??= BaustoffCtrl.ZahlAus(st.Rows[0], "cp");
                    }

                    var kp = new List<DbParam>();
                    if (neu && seite == Seite.Projekt) kp.Add(new DbParam("@p", idProjekt ?? 0));
                    kp.Add(new DbParam("@b", name));
                    kp.Add(BaustoffCtrl.Text("@be", m.Beschreibung));
                    kp.Add(BaustoffCtrl.Text("@art", m.Bauteilart));
                    kp.Add(BaustoffCtrl.Text("@q", m.Quelle));
                    kp.Add(BaustoffCtrl.Text("@h", m.Herkunft ?? (neu ? DbWerte.HERKUNFT_MANUELL : null)));
                    kp.Add(BaustoffCtrl.Text("@qk", m.Quellkennung));

                    if (neu)
                    {
                        m.ID = v.EinfuegenUndId(
                            "INSERT INTO \"" + tab + "\" (" + (seite == Seite.Projekt ? "\"ID_Projekt\", " : "") +
                            "\"Bezeichner\", \"Beschreibung\", \"Bauteilart\", \"Quelle\", \"Herkunft\", \"Quellkennung\"" +
                            (seite == Seite.Katalog ? ", \"ReadOnly\"" : "") + ") VALUES (" +
                            (seite == Seite.Projekt ? "?, " : "") + "?, ?, ?, ?, ?, ?" + (seite == Seite.Katalog ? ", 0" : "") + ")",
                            kp.ToArray());
                        if (m.Herkunft == null) m.Herkunft = DbWerte.HERKUNFT_MANUELL;
                    }
                    else
                    {
                        kp.Add(new DbParam("@id", m.ID));
                        v.Ausfuehren("UPDATE \"" + tab + "\" SET \"Bezeichner\" = ?, \"Beschreibung\" = ?, \"Bauteilart\" = ?, " +
                                     "\"Quelle\" = ?, \"Herkunft\" = ?, \"Quellkennung\" = ? WHERE \"ID\" = ?", kp.ToArray());
                        v.Ausfuehren("DELETE FROM \"" + SchichtTabelle(seite) + "\" WHERE \"ID_Aufbau\" = ?", new DbParam("@a", m.ID));
                    }
                    if (seite == Seite.Projekt) m.ID_Projekt = idProjekt;

                    int rang = 0;
                    foreach (BauteilschichtModel s in m.Schichten.Where(x => x != null))
                        s.ID = SchichtEinfuegen(v, SchichtTabelle(seite), m.ID, ++rang, s);
                    m.Schichten = m.Schichten.Where(x => x != null).ToList();

                    v.Commit();
                    return Ergebnis.Gut(m.ID);
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_NICHT_GESPEICHERT, ex.Message));
            }
        }

        /// <summary>Legt eine Schicht an; setzt Aufbau und Reihenfolge am Modell und liefert die neue Id.</summary>
        private static int SchichtEinfuegen(DbVorgang v, string tabelle, int idAufbau, int reihenfolge, BauteilschichtModel s)
        {
            s.ID_Aufbau = idAufbau;
            s.Reihenfolge = reihenfolge;
            return v.EinfuegenUndId(
                "INSERT INTO \"" + tabelle + "\" (\"ID_Aufbau\", \"Reihenfolge\", \"ID_Baustoff\", \"Dicke\", \"IstLuftschicht\", " +
                "\"Lambda\", \"Rho\", \"cp\") VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                new[]
                {
                    new DbParam("@a", idAufbau),
                    new DbParam("@r", reihenfolge),
                    new DbParam("@s", DbParamTyp.Integer) { Wert = s.ID_Baustoff.HasValue ? (object)s.ID_Baustoff.Value : DBNull.Value },
                    new DbParam("@d", DbParamTyp.Double) { Wert = s.Dicke },
                    new DbParam("@l", s.IstLuftschicht ? 1 : 0),
                    BaustoffCtrl.Zahl("@la", s.Lambda),
                    BaustoffCtrl.Zahl("@rh", s.Rho),
                    BaustoffCtrl.Zahl("@cp", s.Cp)
                });
        }

        private BauteilaufbauModel LesenEinen(Seite seite, int id)
        {
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT * FROM \"" + AufbauTabelle(seite) + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            DataTable schichten = DataRepository.GetDataTable(
                "SELECT " + SCHICHTSPALTEN_SQL + " FROM \"" + SchichtTabelle(seite) + "\" WHERE \"ID_Aufbau\" = ? " +
                "ORDER BY \"Reihenfolge\", \"ID\"", new DbParam("@id", id));
            return Zusammenfuehren(kopf, schichten).FirstOrDefault();
        }

        /// <summary>Köpfe und Schichten zusammenführen — im Speicher, über die gelesenen Ids.</summary>
        private static List<BauteilaufbauModel> Zusammenfuehren(DataTable kopf, DataTable schichten)
        {
            var liste = new List<BauteilaufbauModel>();
            if (kopf == null) return liste;
            var jeId = new Dictionary<int, BauteilaufbauModel>();
            foreach (DataRow r in kopf.Rows)
            {
                var a = new BauteilaufbauModel
                {
                    ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                    ID_Projekt = BaustoffCtrl.GanzAus(r, "ID_Projekt"),
                    Bezeichner = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                    Beschreibung = BaustoffCtrl.TextAus(r, "Beschreibung"),
                    Bauteilart = BaustoffCtrl.TextAus(r, "Bauteilart"),
                    Quelle = BaustoffCtrl.TextAus(r, "Quelle"),
                    Herkunft = BaustoffCtrl.TextAus(r, "Herkunft"),
                    Quellkennung = BaustoffCtrl.TextAus(r, "Quellkennung"),
                    ReadOnly = (BaustoffCtrl.GanzAus(r, "ReadOnly") ?? 0) != 0
                };
                liste.Add(a);
                jeId[a.ID] = a;
            }
            if (schichten != null)
                foreach (DataRow r in schichten.Rows)
                {
                    BauteilschichtModel s = SchichtAus(r);
                    if (jeId.TryGetValue(s.ID_Aufbau, out BauteilaufbauModel a)) a.Schichten.Add(s);
                }
            return liste;
        }

        private static BauteilschichtModel SchichtAus(DataRow r)
        {
            return new BauteilschichtModel
            {
                ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                ID_Aufbau = Convert.ToInt32(r["ID_Aufbau"], CultureInfo.InvariantCulture),
                Reihenfolge = Convert.ToInt32(r["Reihenfolge"], CultureInfo.InvariantCulture),
                ID_Baustoff = BaustoffCtrl.GanzAus(r, "ID_Baustoff"),
                Dicke = Convert.ToDouble(r["Dicke"], CultureInfo.InvariantCulture),
                IstLuftschicht = (BaustoffCtrl.GanzAus(r, "IstLuftschicht") ?? 0) != 0,
                Lambda = BaustoffCtrl.ZahlAus(r, "Lambda"),
                Rho = BaustoffCtrl.ZahlAus(r, "Rho"),
                Cp = BaustoffCtrl.ZahlAus(r, "cp")
            };
        }

        private static string Praefix(string alias, string spalten)
            => string.Join(", ", spalten.Split(new[] { ", " }, StringSplitOptions.None).Select(s => alias + "." + s));

        private static string NichtGefunden(int id)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_NICHT_GEFUNDEN, id);

        private static string Schreibgeschuetzt(string name)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_SCHREIBGESCHUETZT, name);
    }
}
