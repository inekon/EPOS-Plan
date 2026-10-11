using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Eine Zuordnung Projekt → PV-Ganglinie (<c>Z_ProjektPvGanglinie</c>).</summary>
    public sealed class PvGanglinieZuordnung
    {
        /// <summary><c>Z_ProjektPvGanglinie.ID</c>; 0 für eine noch nicht geschriebene Zuordnung.</summary>
        public int Id;

        /// <summary><c>Tab_PvGanglinie.ID</c> der Projektkopie (gelesen) bzw. <c>Tab_PvGanglinie_STAMM.ID</c> (neu aufgenommen).</summary>
        public int IdGanglinie;

        /// <summary>Der Bezeichner — über ihn wird beim Schreiben die Projektkopie gefunden oder angelegt.</summary>
        public string Bezeichner = "";
    }

    /// <summary>Ausgang von <see cref="PvGanglinieStammCtrl.NennleistungSetzen"/>.</summary>
    internal enum PvNennleistungSchreibergebnis
    {
        /// <summary>Der Katalogsatz trägt den neuen Wert.</summary>
        Geschrieben,
        /// <summary>Auslieferungssatz (<c>ReadOnly = 1</c>) — nicht geschrieben.</summary>
        Schreibgeschuetzt,
        /// <summary>Der Katalog kennt den Namen nicht.</summary>
        Unbekannt,
        /// <summary>Der Wert ist nicht größer als 0 oder nicht endlich.</summary>
        Ungueltig,
        /// <summary>Die Datenbank hat den Wert nicht angenommen.</summary>
        Fehler
    }

    /// <summary>Ausgang von <see cref="PvGanglinieStammCtrl.AusKatalogErneuern"/>.</summary>
    internal enum PvKatalogerneuerung
    {
        /// <summary>Kopf und Reihe der Projektkopie tragen den Stand des Katalogsatzes.</summary>
        Erneuert,
        /// <summary>Der Katalog kennt den Namen nicht (mehr) — nichts geschrieben.</summary>
        KeinKatalogsatz,
        /// <summary>Das Projekt führt keine Kopie dieses Namens — nichts geschrieben.</summary>
        KeineProjektkopie,
        /// <summary>Die Projektkopie gleicht dem Katalogsatz schon — nichts geschrieben.</summary>
        Gleich,
        /// <summary>Die Datenbank hat die Erneuerung nicht angenommen; zurückgerollt.</summary>
        Fehler
    }

    /// <summary>
    /// Worin die Projektkopie einer PV-Ganglinie (<c>Tab_PvGanglinie</c>) von ihrem Katalogsatz gleichen
    /// Bezeichners (<c>Tab_PvGanglinie_STAMM</c>) abweicht — Ergebnis von
    /// <see cref="PvGanglinieStammCtrl.AbweichungZumKatalog"/>.
    /// </summary>
    internal sealed class PvKatalogabweichung
    {
        /// <summary>Der Katalog kennt den Namen nicht (mehr).</summary>
        public bool KatalogsatzFehlt;
        /// <summary>Das Projekt führt keine Kopie dieses Namens (etwa eine neu aufgenommene, noch nicht gespeicherte).</summary>
        public bool ProjektkopieFehlt;
        /// <summary><c>Nennleistung_kWp</c> verschieden (auch: gepflegt gegen nicht gepflegt).</summary>
        public bool Nennleistung;
        /// <summary><c>Raster_Minuten</c> verschieden.</summary>
        public bool Raster;
        /// <summary><c>Jahresarbeit_kWh</c> verschieden.</summary>
        public bool Jahressumme;
        /// <summary>Die Reihe verschieden — Anzahl oder ein Wert in Einfügereihenfolge.</summary>
        public bool Reihe;

        /// <summary>Stehen beide Sätze und unterscheiden sie sich in mindestens einem Merkmal?</summary>
        public bool Weicht => !KatalogsatzFehlt && !ProjektkopieFehlt && (Nennleistung || Raster || Jahressumme || Reihe);
    }

    /// <summary>
    /// <b>Katalog, Projektkopie und Zuordnung der PV-Ganglinie</b> (PVG, Schemaschritt 206;
    /// Tabellen in <see cref="PvGanglinieSchema"/>). Muster: <see cref="SolarganglinieStammCtrl"/> —
    /// der Kopf heißt über den Bezeichner, die Werte stehen in Einfügereihenfolge (ID) und im
    /// Raster der Datei (60 oder 15 Minuten).
    ///
    /// <para><b>Die Kennzahlen stehen am Kopf</b> (<c>Jahresarbeit_kWh</c>, <c>Spitze_kW</c>), beim
    /// Import einmal aus den Werten gerechnet; die Katalogliste liest sie trotzdem aus den Werten
    /// (<see cref="GanglinienAuswertungCtrl.Kennzahlen"/>), wie bei jeder Ganglinie.</para>
    /// </summary>
    internal static class PvGanglinieStammCtrl
    {
        internal const string HEAD_STAMM = PvGanglinieSchema.TAB_KOPF_STAMM;
        internal const string DATA_STAMM = PvGanglinieSchema.TAB_DATEN_STAMM;
        internal const string HEAD_PROJ = PvGanglinieSchema.TAB_KOPF;
        internal const string DATA_PROJ = PvGanglinieSchema.TAB_DATEN;
        internal const string ZUORDNUNG = PvGanglinieSchema.TAB_ZUORDNUNG;

        // =================================================================
        //  Katalog
        // =================================================================

        /// <summary>Ist der Katalogsatz ein Auslieferungssatz (<c>ReadOnly = 1</c>)?</summary>
        internal static bool IstSchreibgeschuetzt(string bezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM Tab_PvGanglinie_STAMM WHERE Bezeichner = ?",
                new DbParam("@bez", bezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0;
        }

        /// <summary>Die Katalog-ID zu einem Bezeichner, sonst 0.</summary>
        internal static int StammId(string bezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_PvGanglinie_STAMM WHERE Bezeichner = ?",
                new DbParam("@bez", bezeichner ?? ""));
            return v != null && v != DBNull.Value ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0;
        }

        /// <summary>Der Bezeichner zu einer Katalog-ID, sonst "".</summary>
        internal static string BezeichnerZu(int stammId)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_PvGanglinie_STAMM WHERE ID = ?",
                new DbParam("@id", stammId));
            return v != null && v != DBNull.Value ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        /// <summary>Steht ein Katalogsatz dieses Namens?</summary>
        internal static bool Vorhanden(string bezeichner) => !string.IsNullOrEmpty(bezeichner) && StammId(bezeichner) > 0;

        /// <summary>Führt ein Projekt die Ganglinie (über den Bezeichner der Zuordnung)?</summary>
        internal static bool HatProjektzuordnung(string bezeichner)
        {
            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_ProjektPvGanglinie WHERE Bezeichner = ?",
                new DbParam("@bez", bezeichner ?? ""));
            return anzahl != null && anzahl != DBNull.Value && Convert.ToInt64(anzahl, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// Löscht einen Katalogsatz samt Werten (Fremdschlüssel mit Kaskade, die Werte zusätzlich
        /// ausdrücklich), sofern er kein Auslieferungssatz ist. Projektkopien bleiben, wie sie sind.
        /// </summary>
        internal static bool Loeschen(string bezeichner)
        {
            if (IstSchreibgeschuetzt(bezeichner))
            {
                Meldung.Hinweis(MyResource.Resource.PVG_MSG_SCHREIBGESCHUETZT, MyResource.Resource.PVG_TITEL);
                return false;
            }
            int id = StammId(bezeichner);
            if (id <= 0) return false;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("DELETE FROM Tab_PvGanglinieDaten_STAMM WHERE ID_Ganglinie = ?", new DbParam("@id", id));
                    v.Ausfuehren("DELETE FROM Tab_PvGanglinie_STAMM WHERE ID = ?", new DbParam("@id", id));
                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen(MyResource.Resource.PVG_MSG_SCHREIBFEHLER + " " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Die Spitze der Reihe im Raster der Datei [kW] (<c>Tab_PvGanglinie_STAMM.Spitze_kW</c>, beim Import
        /// gerechnet); <c>null</c>, wenn der Katalog den Namen nicht kennt.
        /// </summary>
        internal static double? SpitzeKw(string bezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Spitze_kW FROM Tab_PvGanglinie_STAMM WHERE Bezeichner = ?",
                new DbParam("@bez", bezeichner ?? ""));
            return v != null && v != DBNull.Value ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : (double?)null;
        }

        /// <summary>
        /// Der Prüfhinweis zu einer Nennleistung [kWp] für einen Katalogsatz — derselbe Satz wie beim Import
        /// (<see cref="PvGanglinieImportCtrl.Pruefhinweis"/>: Spitze über Nennleistung × 1,1); "" ohne.
        /// </summary>
        internal static string Pruefhinweis(string bezeichner, double? nennleistungKwp)
        {
            double? spitze = SpitzeKw(bezeichner);
            return spitze.HasValue ? PvGanglinieImportCtrl.Pruefhinweis(spitze.Value, nennleistungKwp) : "";
        }

        /// <summary>
        /// <b>Die Nennleistung eines Katalogsatzes nachträglich setzen</b> (<c>Tab_PvGanglinie_STAMM.Nennleistung_kWp</c>):
        /// <c>null</c> = nicht gepflegt (der Lauf nimmt dann die Spitze der Reihe), sonst ein Wert über 0. Ein
        /// Auslieferungssatz (<c>ReadOnly = 1</c>) bleibt gesperrt. Geschrieben wird nur der Katalogsatz —
        /// Projektkopien (<c>Tab_PvGanglinie</c>) behalten ihren Wert, wie bei jeder Katalogänderung
        /// (<see cref="ZuordnungenSchreiben"/> findet die vorhandene Kopie über den Bezeichner). Meldet nicht
        /// selbst; den Text wählt der Aufrufer.
        /// </summary>
        internal static PvNennleistungSchreibergebnis NennleistungSetzen(string bezeichner, double? nennleistungKwp)
        {
            if (nennleistungKwp.HasValue && (double.IsNaN(nennleistungKwp.Value) || double.IsInfinity(nennleistungKwp.Value)
                                             || nennleistungKwp.Value <= 0))
                return PvNennleistungSchreibergebnis.Ungueltig;
            int id = StammId(bezeichner);
            if (id <= 0) return PvNennleistungSchreibergebnis.Unbekannt;
            if (IstSchreibgeschuetzt(bezeichner)) return PvNennleistungSchreibergebnis.Schreibgeschuetzt;
            try
            {
                int n = DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_PvGanglinie_STAMM SET Nennleistung_kWp = ? WHERE ID = ? AND ReadOnly = 0",
                    new DbParam("@nenn", DbParamTyp.Double) { Wert = nennleistungKwp.HasValue ? (object)nennleistungKwp.Value : DBNull.Value },
                    new DbParam("@id", DbParamTyp.Integer) { Wert = id });
                return n == 1 ? PvNennleistungSchreibergebnis.Geschrieben : PvNennleistungSchreibergebnis.Fehler;
            }
            catch (Exception)
            {
                return PvNennleistungSchreibergebnis.Fehler;
            }
        }

        /// <summary>Jahresarbeit [kWh] und Spitze [kW] einer Wertefolge im Raster.</summary>
        internal static (double JahresarbeitKwh, double SpitzeKw) Kennzahlen(IList<double> werte, int rasterMinuten)
        {
            double summe = 0, spitze = 0;
            if (werte != null)
                foreach (double w in werte)
                {
                    summe += w;
                    if (w > spitze) spitze = w;
                }
            return (summe * rasterMinuten / 60.0, spitze);
        }

        /// <summary>
        /// Schreibt eine neue Ganglinie in den Katalog — Kopf und Werte in EINER Transaktion. Die Werte
        /// bleiben im Raster der Datei (8 760 Stunden- oder 35 040 Viertelstundenwerte).
        /// </summary>
        /// <returns>Die neue Katalog-ID, 0 bei Fehler.</returns>
        internal static int Importieren(string bezeichner, string beschreibung, int rasterMinuten,
                                        double? nennleistungKwp, IList<double> werte)
        {
            if (werte == null || werte.Count != PvGanglinieWeiche.Erwartet(rasterMinuten)) return 0;
            (double arbeit, double spitze) = Kennzahlen(werte, rasterMinuten);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(
                        "INSERT INTO Tab_PvGanglinie_STAMM (Bezeichner, Beschreibung, Raster_Minuten, Nennleistung_kWp, " +
                        "Jahresarbeit_kWh, Spitze_kW, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, 0)",
                        new DbParam("@bez", DbParamTyp.VarWChar) { Wert = bezeichner ?? "" },
                        new DbParam("@beschr", DbParamTyp.VarWChar) { Wert = (object)beschreibung ?? DBNull.Value },
                        new DbParam("@raster", DbParamTyp.Integer) { Wert = rasterMinuten },
                        new DbParam("@nenn", DbParamTyp.Double) { Wert = nennleistungKwp.HasValue ? (object)nennleistungKwp.Value : DBNull.Value },
                        new DbParam("@arbeit", DbParamTyp.Double) { Wert = arbeit },
                        new DbParam("@spitze", DbParamTyp.Double) { Wert = spitze });
                    int id = Convert.ToInt32(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
                    foreach (double w in werte)
                        v.Ausfuehren("INSERT INTO Tab_PvGanglinieDaten_STAMM (ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, 0)",
                                     new DbParam("@g", DbParamTyp.Integer) { Wert = id },
                                     new DbParam("@w", DbParamTyp.Double) { Wert = w });
                    v.Commit();
                    return id;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen(MyResource.Resource.PVG_MSG_SCHREIBFEHLER + " " + ex.Message);
                    return 0;
                }
            }
        }

        // =================================================================
        //  Projekt
        // =================================================================

        /// <summary>Die Zuordnungen eines Projekts in ihrer Reihenfolge (ID).</summary>
        internal static List<PvGanglinieZuordnung> Zuordnungen(int idProjekt)
        {
            var liste = new List<PvGanglinieZuordnung>();
            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Ganglinie, Bezeichner FROM Z_ProjektPvGanglinie WHERE ID_Projekt = ? ORDER BY ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add(new PvGanglinieZuordnung
                {
                    Id = StilleDb.Zahl(StilleDb.Feld(r, "ID")),
                    IdGanglinie = StilleDb.Zahl(StilleDb.Feld(r, "ID_Ganglinie")),
                    Bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"))
                });
            return liste;
        }

        /// <summary>
        /// Schreibt die Zuordnungen eines Projekts neu — in EINER Transaktion: Die alten Zuordnungen
        /// gehen, je Bezeichner wird die Projektkopie gefunden oder aus dem Katalog angelegt (Kopf und
        /// Werte), die neue Zuordnung zeigt auf sie über die ID; Projektkopien ohne Zuordnung gehen mit
        /// (ihre Werte über die Kaskade).
        /// </summary>
        /// <returns>true bei Erfolg.</returns>
        internal static bool ZuordnungenSchreiben(int idProjekt, IEnumerable<string> bezeichner)
        {
            if (idProjekt <= 0) return false;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("DELETE FROM Z_ProjektPvGanglinie WHERE ID_Projekt = ?", new DbParam("@proj", idProjekt));
                    foreach (string bez in bezeichner ?? Array.Empty<string>())
                    {
                        if (string.IsNullOrEmpty(bez)) continue;
                        int kopie = ProjektkopieSicherstellen(bez, idProjekt, v);
                        if (kopie <= 0) continue;
                        v.Ausfuehren("INSERT INTO Z_ProjektPvGanglinie (ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?)",
                                     new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt },
                                     new DbParam("@g", DbParamTyp.Integer) { Wert = kopie },
                                     new DbParam("@bez", DbParamTyp.VarWChar) { Wert = bez });
                    }
                    v.Ausfuehren("DELETE FROM Tab_PvGanglinieDaten WHERE ID_Ganglinie IN (SELECT g.ID FROM Tab_PvGanglinie g " +
                                 "WHERE g.ID_Projekt = ? AND NOT EXISTS (SELECT 1 FROM Z_ProjektPvGanglinie z WHERE z.ID_Ganglinie = g.ID))",
                                 new DbParam("@proj", idProjekt));
                    v.Ausfuehren("DELETE FROM Tab_PvGanglinie WHERE ID_Projekt = ? AND NOT EXISTS " +
                                 "(SELECT 1 FROM Z_ProjektPvGanglinie z WHERE z.ID_Ganglinie = Tab_PvGanglinie.ID)",
                                 new DbParam("@proj", idProjekt));
                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen(MyResource.Resource.PVG_MSG_SCHREIBFEHLER + " " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Die Projektkopie zu einem Bezeichner — vorhanden oder aus dem Katalog kopiert (Kopf samt
        /// Raster und Kennzahlen, Werte in Katalogreihenfolge). 0, wenn der Katalog den Namen nicht kennt.
        /// </summary>
        private static int ProjektkopieSicherstellen(string bezeichner, int idProjekt, DbVorgang v)
        {
            object vorhanden = v.Skalar("SELECT ID FROM Tab_PvGanglinie WHERE Bezeichner = ? AND ID_Projekt = ? ORDER BY ID LIMIT 1",
                                        new DbParam("@bez", bezeichner), new DbParam("@proj", idProjekt));
            if (vorhanden != null && vorhanden != DBNull.Value) return Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);

            DataTable kopf = v.Lese("SELECT ID FROM Tab_PvGanglinie_STAMM WHERE Bezeichner = ?",
                                    new DbParam("@bez", DbParamTyp.VarWChar) { Wert = bezeichner });
            if (kopf.Rows.Count == 0) return 0;
            int stammId = Convert.ToInt32(kopf.Rows[0]["ID"], CultureInfo.InvariantCulture);

            v.Ausfuehren("INSERT INTO Tab_PvGanglinie (ID_Projekt, Bezeichner, Beschreibung, Raster_Minuten, Nennleistung_kWp, " +
                         "Jahresarbeit_kWh, Spitze_kW) SELECT ?, Bezeichner, Beschreibung, Raster_Minuten, Nennleistung_kWp, " +
                         "Jahresarbeit_kWh, Spitze_kW FROM Tab_PvGanglinie_STAMM WHERE ID = ?",
                         new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt },
                         new DbParam("@id", DbParamTyp.Integer) { Wert = stammId });
            int neu = Convert.ToInt32(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            v.Ausfuehren("INSERT INTO Tab_PvGanglinieDaten (ID_Ganglinie, Wert) " +
                         "SELECT ?, Wert FROM Tab_PvGanglinieDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID",
                         new DbParam("@neu", DbParamTyp.Integer) { Wert = neu },
                         new DbParam("@id", DbParamTyp.Integer) { Wert = stammId });
            return neu;
        }

        // =================================================================
        //  Projektkopie und Katalog (E113)
        // =================================================================

        /// <summary>
        /// <b>Weicht die Projektkopie vom Katalogsatz ab?</b> Verglichen werden die Projektkopie des Projekts
        /// (dieselbe, die <see cref="ZuordnungenSchreiben"/> über den Bezeichner findet) und der Katalogsatz
        /// gleichen Bezeichners: Nennleistung, Raster, Jahressumme und die Reihe Wert für Wert in
        /// Einfügereihenfolge. Fehlt einer der beiden Sätze, sagt es das Ergebnis; es weicht dann nicht ab.
        /// </summary>
        internal static PvKatalogabweichung AbweichungZumKatalog(int idProjekt, string bezeichner)
        {
            var a = new PvKatalogabweichung();
            DataTable stamm = StilleDb.Tabelle(
                "SELECT ID, Raster_Minuten, Nennleistung_kWp, Jahresarbeit_kWh FROM Tab_PvGanglinie_STAMM WHERE Bezeichner = ?",
                StilleDb.Par("@bez", DbParamTyp.VarWChar, bezeichner ?? ""));
            DataTable kopie = StilleDb.Tabelle(
                "SELECT ID, Raster_Minuten, Nennleistung_kWp, Jahresarbeit_kWh FROM Tab_PvGanglinie " +
                "WHERE Bezeichner = ? AND ID_Projekt = ? ORDER BY ID LIMIT 1",
                StilleDb.Par("@bez", DbParamTyp.VarWChar, bezeichner ?? ""),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            a.KatalogsatzFehlt = stamm == null || stamm.Rows.Count == 0;
            a.ProjektkopieFehlt = kopie == null || kopie.Rows.Count == 0;
            if (a.KatalogsatzFehlt || a.ProjektkopieFehlt) return a;

            DataRow s = stamm.Rows[0], k = kopie.Rows[0];
            a.Nennleistung = !Gleich(ZahlOderNull(s["Nennleistung_kWp"]), ZahlOderNull(k["Nennleistung_kWp"]));
            a.Raster = !Gleich(ZahlOderNull(s["Raster_Minuten"]), ZahlOderNull(k["Raster_Minuten"]));
            a.Jahressumme = !Gleich(ZahlOderNull(s["Jahresarbeit_kWh"]), ZahlOderNull(k["Jahresarbeit_kWh"]));
            a.Reihe = !ReiheGleich(
                Werte("SELECT Wert FROM Tab_PvGanglinieDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID", s["ID"]),
                Werte("SELECT Wert FROM Tab_PvGanglinieDaten WHERE ID_Ganglinie = ? ORDER BY ID", k["ID"]));
            return a;
        }

        /// <summary>
        /// <b>Die Projektkopie aus dem Katalog erneuern</b> (E113): Kopf (Beschreibung, Raster, Nennleistung,
        /// Jahresarbeit, Spitze) und Reihe der Projektkopie werden in EINER Transaktion durch den Stand des
        /// Katalogsatzes gleichen Bezeichners ersetzt. Die ID der Projektkopie und die Zuordnung
        /// <c>Z_ProjektPvGanglinie</c> bleiben; das Projekt gilt danach als geändert
        /// (<see cref="MerkmalUebernahmeCtrl.MarkiereProjektGeaendert"/>) — ein gespeichertes Ergebnis ist veraltet
        /// bis zur nächsten Simulation. Meldet nicht selbst; den Text wählt der Aufrufer.
        /// </summary>
        internal static PvKatalogerneuerung AusKatalogErneuern(int idProjekt, string bezeichner)
        {
            if (idProjekt <= 0) return PvKatalogerneuerung.KeineProjektkopie;
            PvKatalogabweichung a = AbweichungZumKatalog(idProjekt, bezeichner);
            if (a.KatalogsatzFehlt) return PvKatalogerneuerung.KeinKatalogsatz;
            if (a.ProjektkopieFehlt) return PvKatalogerneuerung.KeineProjektkopie;
            if (!a.Weicht) return PvKatalogerneuerung.Gleich;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    DataTable stamm = v.Lese(
                        "SELECT ID, Beschreibung, Raster_Minuten, Nennleistung_kWp, Jahresarbeit_kWh, Spitze_kW " +
                        "FROM Tab_PvGanglinie_STAMM WHERE Bezeichner = ?",
                        new DbParam("@bez", DbParamTyp.VarWChar) { Wert = bezeichner ?? "" });
                    object kopieId = v.Skalar(
                        "SELECT ID FROM Tab_PvGanglinie WHERE Bezeichner = ? AND ID_Projekt = ? ORDER BY ID LIMIT 1",
                        new DbParam("@bez", DbParamTyp.VarWChar) { Wert = bezeichner ?? "" },
                        new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt });
                    if (stamm.Rows.Count == 0 || kopieId == null || kopieId == DBNull.Value)
                    {
                        try { v.Rollback(); } catch { }
                        return stamm.Rows.Count == 0 ? PvKatalogerneuerung.KeinKatalogsatz : PvKatalogerneuerung.KeineProjektkopie;
                    }
                    DataRow s = stamm.Rows[0];
                    int stammId = Convert.ToInt32(s["ID"], CultureInfo.InvariantCulture);
                    int kopie = Convert.ToInt32(kopieId, CultureInfo.InvariantCulture);

                    int n = v.Ausfuehren(
                        "UPDATE Tab_PvGanglinie SET Beschreibung = ?, Raster_Minuten = ?, Nennleistung_kWp = ?, " +
                        "Jahresarbeit_kWh = ?, Spitze_kW = ? WHERE ID = ? AND ID_Projekt = ?",
                        new DbParam("@beschr", DbParamTyp.VarWChar) { Wert = s["Beschreibung"] },
                        new DbParam("@raster", DbParamTyp.Integer) { Wert = s["Raster_Minuten"] },
                        new DbParam("@nenn", DbParamTyp.Double) { Wert = s["Nennleistung_kWp"] },
                        new DbParam("@arbeit", DbParamTyp.Double) { Wert = s["Jahresarbeit_kWh"] },
                        new DbParam("@spitze", DbParamTyp.Double) { Wert = s["Spitze_kW"] },
                        new DbParam("@id", DbParamTyp.Integer) { Wert = kopie },
                        new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt });
                    if (n != 1)
                    {
                        try { v.Rollback(); } catch { }
                        return PvKatalogerneuerung.Fehler;
                    }
                    v.Ausfuehren("DELETE FROM Tab_PvGanglinieDaten WHERE ID_Ganglinie = ?",
                                 new DbParam("@kopie", DbParamTyp.Integer) { Wert = kopie });
                    v.Ausfuehren("INSERT INTO Tab_PvGanglinieDaten (ID_Ganglinie, Wert) " +
                                 "SELECT ?, Wert FROM Tab_PvGanglinieDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID",
                                 new DbParam("@kopie", DbParamTyp.Integer) { Wert = kopie },
                                 new DbParam("@id", DbParamTyp.Integer) { Wert = stammId });

                    // Die Reihe ist eine Eingangsgröße der Simulation: Ein gespeichertes Ergebnis ist ab hier
                    // veraltet (Tab_Projekt.Aenderungsdatum) — im SELBEN Vorgang, ein Rollback nimmt die Marke mit.
                    using (Vorgangsklammer.Setzen(v))
                        MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idProjekt);

                    v.Commit();
                    return PvKatalogerneuerung.Erneuert;
                }
                catch (Exception)
                {
                    try { v.Rollback(); } catch { }
                    return PvKatalogerneuerung.Fehler;
                }
            }
        }

        private static double? ZahlOderNull(object o)
            => o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        /// <summary>Gleich heißt bitgleich — die Kopie entsteht durch Kopieren, nicht durch Rechnen.</summary>
        private static bool Gleich(double? a, double? b)
            => a.HasValue == b.HasValue && (!a.HasValue || a.Value.Equals(b.Value));

        private static List<double> Werte(string sql, object idGanglinie)
        {
            var liste = new List<double>();
            DataTable dt = StilleDb.Tabelle(sql,
                StilleDb.Par("@g", DbParamTyp.Integer, Convert.ToInt32(idGanglinie, CultureInfo.InvariantCulture)));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add(r[0] == DBNull.Value ? double.NaN : Convert.ToDouble(r[0], CultureInfo.InvariantCulture));
            return liste;
        }

        private static bool ReiheGleich(List<double> a, List<double> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!a[i].Equals(b[i])) return false;
            return true;
        }
    }
}
