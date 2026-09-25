using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Herkunftsablage der Gebäudeimporte</b> (Gebäudesimulation Stufe G4c, Schritt S-F;
    /// Datenaustauschkonzept 2.3 und 7.1/7.2, Softwarearchitektur 1.4 und 2.7). Je Importlauf EINE
    /// Zeile in <c>Tab_Importquelle</c>, je Paarung EPOS-Zeile ↔ Quellentität EINE Zeile in
    /// <c>Tab_Importzuordnung</c> — damit ein zweiter Import derselben Datei erkennt, was er schon
    /// zugeordnet hat, und der Round-Trip die Quellentitäten wiederfindet.
    ///
    /// <para><b>Ein Schreibweg, eine Transaktion.</b> <see cref="SchreibeHerkunft"/> legt die Quelle
    /// und alle ihre Paarungen in EINEM <see cref="DbVorgang"/> an, nur über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern. Ein hereingereichter Vorgang des Aufrufers
    /// (etwa der Übernahme des Gebäudes) wird über die <see cref="Vorgangsklammer"/> angemeldet; der
    /// eigene Vorgang wird dann zum Sicherungspunkt darin. Geprüft wird vor dem Schreiben — Format,
    /// Dateiname, SHA-256, Zeitpunkt, je Paarung Quelltyp und Kennung, und dass jedes Ziel zu diesem
    /// Gebäude bzw. seinem Projekt gehört; eine Ablehnung ist benannt und schreibt nichts.</para>
    ///
    /// <para><b>Die Kaskade trägt das Löschen.</b> Die Quelle hängt mit <c>ON DELETE CASCADE</c> am
    /// Gebäude, die Paarung an der Quelle und — abweichend von den Papieren, begründet in
    /// <see cref="ImportzuordnungSchema"/> — an jedem ihrer fünf Ziele. Ein gewöhnliches Speichern
    /// lässt beides stehen, weil kein Speicherweg ein Gebäude, eine Zone oder ein Bauteil löscht
    /// und neu anlegt (G3-Messung A1, Abgleich A6).</para>
    ///
    /// <para><b>In G4c</b> schreibt der Einzonenweg (Zonenregel X4) nur die Paarung Gebäude ↔
    /// <c>Building/@id</c> (<see cref="Einzonenpaarungen"/>); die übrigen Zielarten trägt der Weg
    /// schon, befüllt werden sie mit G6c.</para>
    /// </summary>
    public sealed class GebaeudeImportCtrl
    {
        /// <summary>Was ein Schreibversuch ergeben hat; bei Erfolg die Kennung der neuen Quelle.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung, int IdImportquelle)
        {
            internal static Ergebnis Gut(int id) => new Ergebnis(true, "", id);
            internal static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "", 0);
        }

        private static string Spalten(IEnumerable<string> spalten, string alias)
            => string.Join(", ", new[] { "ID" }.Concat(spalten).Select(s => alias + ".\"" + s + "\""));

        private static readonly string QUELLSPALTEN = Spalten(ImportzuordnungSchema.Quellspalten, "q");
        private static readonly string ZUORDNUNGSSPALTEN = Spalten(ImportzuordnungSchema.Zuordnungsspalten, "z");

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Die Quellen EINES Gebäudes, die jüngste zuerst; nie <c>null</c>.</summary>
        public List<ImportquelleModel> LesenQuellen(int idGebaeude)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT " + QUELLSPALTEN + " FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" q " +
                "WHERE q.\"ID_Gebaeude\" = ? ORDER BY q.\"ID\" DESC",
                new DbParam("@g", idGebaeude));
            var liste = new List<ImportquelleModel>();
            if (t == null) return liste;
            foreach (DataRow r in t.Rows) liste.Add(Quelle(r));
            return liste;
        }

        /// <summary>Die Paarungen EINER Quelle in Anlegereihenfolge; nie <c>null</c>.</summary>
        public List<ImportzuordnungModel> LesenZuordnungen(int idImportquelle)
        {
            return Zuordnungen(DataRepository.GetDataTable(
                "SELECT " + ZUORDNUNGSSPALTEN + " FROM \"" + ImportzuordnungSchema.TAB_ZUORDNUNG + "\" z " +
                "WHERE z.\"ID_Importquelle\" = ? ORDER BY z.\"ID\"",
                new DbParam("@q", idImportquelle)));
        }

        /// <summary>
        /// <b>Der Rückweg des Round-Trips</b>: die Paarungen zu einer Kennung der Quelldatei — die
        /// jüngste Quelle zuerst; nie <c>null</c>. Die Kennung wird wie beim Schreiben auf 64 Zeichen
        /// gekürzt (<see cref="Quellkennung.Kuerzen(string)"/>), eine überlange gbXML-<c>id</c> findet
        /// also ihre gespeicherte Kurzform. Mit <paramref name="idGebaeude"/> nur die Paarungen der
        /// Quellen dieses Gebäudes.
        /// </summary>
        public List<ImportzuordnungModel> FindeZuordnung(string quellkennung, int? idGebaeude = null)
        {
            string kennung = Quellkennung.Kuerzen(quellkennung ?? "");
            if (kennung.Length == 0) return new List<ImportzuordnungModel>();
            const string ORDNUNG = " ORDER BY z.\"ID_Importquelle\" DESC, z.\"ID\"";
            if (!idGebaeude.HasValue)
                return Zuordnungen(DataRepository.GetDataTable(
                    "SELECT " + ZUORDNUNGSSPALTEN + " FROM \"" + ImportzuordnungSchema.TAB_ZUORDNUNG + "\" z " +
                    "WHERE z.\"Quellkennung\" = ?" + ORDNUNG,
                    new DbParam("@k", kennung)));
            return Zuordnungen(DataRepository.GetDataTable(
                "SELECT " + ZUORDNUNGSSPALTEN + " FROM \"" + ImportzuordnungSchema.TAB_ZUORDNUNG + "\" z " +
                "INNER JOIN \"" + ImportzuordnungSchema.TAB_QUELLE + "\" q ON q.\"ID\" = z.\"ID_Importquelle\" " +
                "WHERE z.\"Quellkennung\" = ? AND q.\"ID_Gebaeude\" = ?" + ORDNUNG,
                new DbParam("@k", kennung), new DbParam("@g", idGebaeude.Value)));
        }

        /// <summary>
        /// Wurde DIESELBE Datei (gleicher SHA-256) für dieses Gebäude schon einmal importiert? Der
        /// Vergleich geht über den Inhalt, nicht über Name oder Zeitpunkt (2.3).
        /// </summary>
        public bool SchonImportiert(int idGebaeude, string hash)
        {
            string h = (hash ?? "").Trim().ToLowerInvariant();
            if (h.Length != ImportzuordnungSchema.HASH_LAENGE) return false;
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" WHERE \"ID_Gebaeude\" = ? AND \"Hash\" = ?",
                new DbParam("@g", idGebaeude), new DbParam("@h", h));
            return n != null && n != DBNull.Value && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0;
        }

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Die Paarungen, die der Einzonenweg in G4c schreibt: allein die des Gebäudes mit seiner
        /// eigenen Kennung aus der Datei (gbXML <c>Building/@id</c>, IFC <c>IfcBuilding.GlobalId</c>).
        /// Räume, Flächen und Öffnungen gehen im Einzonenweg im Gebäude auf; ihre Paarungen mit Zone
        /// und Bauteil entstehen erst, wenn es diese Zeilen gibt (G6c).
        /// </summary>
        internal static IReadOnlyList<GebaeudeQuellzuordnung> Einzonenpaarungen(GebaeudeImportSatz satz)
        {
            if (satz == null) return Array.Empty<GebaeudeQuellzuordnung>();
            string kennung = Quellkennung.Kuerzen(satz.Gebaeudekennung ?? "");
            if (kennung.Length == 0) return Array.Empty<GebaeudeQuellzuordnung>();
            GebaeudeQuellzuordnung gebaeude = satz.Quellzuordnungen.FirstOrDefault(
                z => z != null && z.Ziel == ImportZiel.Gebaeude && string.Equals(z.Quellkennung, kennung, StringComparison.Ordinal));
            return gebaeude == null ? Array.Empty<GebaeudeQuellzuordnung>() : new[] { gebaeude };
        }

        /// <summary>
        /// Die Prüfung von Quelle und Paarungen ohne Datenbank — <c>null</c> = gültig, sonst die
        /// Meldung. Was die Tabellen per <c>CHECK</c> halten, wird hier vorher benannt; dazu der
        /// SHA-256 als 64 kleine Hexadezimalzeichen (der <c>CHECK</c> prüft nur die Länge).
        /// </summary>
        internal static string Pruefen(GebaeudeQuelle quelle, IReadOnlyList<GebaeudeQuellzuordnung> zuordnungen)
        {
            if (quelle == null) return MyResource.Resource.HERKUNFT_MSG_QUELLE_FEHLT;
            if (!DbWerte.IMPORT_FORMATE.Contains(quelle.Format))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_FORMAT, quelle.Format);
            if (string.IsNullOrWhiteSpace(quelle.Dateiname) || quelle.Dateiname.Length > ImportzuordnungSchema.DATEINAME_MAX)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_DATEINAME,
                                     ImportzuordnungSchema.DATEINAME_MAX);
            if (!IstHash(quelle.Hash)) return MyResource.Resource.HERKUNFT_MSG_HASH;
            if (string.IsNullOrWhiteSpace(quelle.Zeitpunkt)) return MyResource.Resource.HERKUNFT_MSG_ZEITPUNKT;

            int nr = 0;
            foreach (GebaeudeQuellzuordnung z in zuordnungen ?? Array.Empty<GebaeudeQuellzuordnung>())
            {
                nr++;
                if (z == null || string.IsNullOrWhiteSpace(z.Quelltyp) || z.Quelltyp.Length > ImportzuordnungSchema.QUELLTYP_MAX ||
                    string.IsNullOrWhiteSpace(z.Quellkennung))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_PAARUNG, nr,
                                         ImportzuordnungSchema.QUELLTYP_MAX);
                if (z.Ziel != ImportZiel.Gebaeude && !z.ZielId.HasValue)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_ZIEL_FEHLT,
                                         z.Quellkennung, Zielspalte(z.Ziel));
            }
            return null;
        }

        /// <summary>64 Zeichen aus <c>0-9a-f</c> — der SHA-256 hexadezimal klein.</summary>
        internal static bool IstHash(string hash)
            => hash != null && hash.Length == ImportzuordnungSchema.HASH_LAENGE &&
               hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>
        /// <b>Schreibt die Herkunft eines Importlaufs</b>: EINE Zeile in <c>Tab_Importquelle</c> für
        /// <paramref name="idGebaeude"/> und je Paarung EINE Zeile in <c>Tab_Importzuordnung</c> — in
        /// EINER Transaktion. Die Kennung jeder Paarung steht gekürzt auf 64 Zeichen
        /// (<see cref="Quellkennung.Kuerzen(string)"/>). Das Gebäudeziel ohne <c>ZielId</c> ist das
        /// Gebäude des Imports; jedes andere Ziel braucht seine <c>ZielId</c> und muss zu diesem
        /// Gebäude (Zone, Bauteil) bzw. seinem Projekt (Aufbau, Baustoff) gehören.
        ///
        /// <para>Mit <paramref name="vorgang"/> läuft das Schreiben im Vorgang des Aufrufers (als
        /// Sicherungspunkt), sonst in einem eigenen. Eine Ablehnung schreibt nichts.</para>
        /// </summary>
        internal Ergebnis SchreibeHerkunft(int idGebaeude, GebaeudeQuelle quelle,
                                           IReadOnlyList<GebaeudeQuellzuordnung> zuordnungen, DbVorgang vorgang = null)
        {
            List<GebaeudeQuellzuordnung> liste = (zuordnungen ?? Array.Empty<GebaeudeQuellzuordnung>()).ToList();
            string fehler = Pruefen(quelle, liste);
            if (fehler != null) return Ergebnis.Fehler(fehler);

            using Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    object projekt = v.Skalar("SELECT COALESCE(\"ID_Projekt\", 0) FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?",
                                              new DbParam("@g", idGebaeude));
                    if (projekt == null || projekt == DBNull.Value)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                                                             MyResource.Resource.ZONE_MSG_GEBAEUDE_FEHLT, idGebaeude));
                    }
                    int idProjekt = Convert.ToInt32(projekt, CultureInfo.InvariantCulture);

                    // Die Ziele je Art: nur die Arten laden, die eine Paarung braucht.
                    var erlaubt = new Dictionary<ImportZiel, HashSet<int>>
                    {
                        [ImportZiel.Gebaeude] = new HashSet<int> { idGebaeude }
                    };
                    foreach (ImportZiel art in liste.Select(z => z.Ziel).Distinct())
                        if (!erlaubt.ContainsKey(art)) erlaubt[art] = Ziele(v, art, idGebaeude, idProjekt);

                    var zeilen = new List<(GebaeudeQuellzuordnung Paarung, int Ziel)>();
                    foreach (GebaeudeQuellzuordnung z in liste)
                    {
                        int ziel = z.ZielId ?? idGebaeude;         // nur beim Gebaeudeziel ohne Id (Pruefen)
                        if (!erlaubt.TryGetValue(z.Ziel, out HashSet<int> menge) || !menge.Contains(ziel))
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_ZIEL_FREMD,
                                                                 z.Quellkennung, Zielspalte(z.Ziel), ziel));
                        }
                        zeilen.Add((z, ziel));
                    }

                    int idQuelle = v.EinfuegenUndId(
                        "INSERT INTO \"" + ImportzuordnungSchema.TAB_QUELLE + "\" (" +
                        string.Join(", ", ImportzuordnungSchema.Quellspalten.Select(s => "\"" + s + "\"")) + ") VALUES (" +
                        BaustoffCtrl.Fragezeichen(ImportzuordnungSchema.Quellspalten.Count) + ")",
                        Quellwerte(idGebaeude, quelle).ToArray());

                    string einfuegen = "INSERT INTO \"" + ImportzuordnungSchema.TAB_ZUORDNUNG + "\" (" +
                                       string.Join(", ", ImportzuordnungSchema.Zuordnungsspalten.Select(s => "\"" + s + "\"")) +
                                       ") VALUES (" + BaustoffCtrl.Fragezeichen(ImportzuordnungSchema.Zuordnungsspalten.Count) + ")";
                    foreach ((GebaeudeQuellzuordnung paarung, int ziel) in zeilen)
                        v.Ausfuehren(einfuegen, Zuordnungswerte(idQuelle, paarung, ziel).ToArray());

                    v.Commit();
                    return Ergebnis.Gut(idQuelle);
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_NICHT_GESPEICHERT,
                                                     ex.Message));
            }
        }

        // =================================================================
        //  intern
        // =================================================================

        /// <summary>Der Spaltenname eines Ziels — für Meldungen ein fester Name, kein Anzeigetext.</summary>
        internal static string Zielspalte(ImportZiel ziel)
        {
            int i = (int)ziel;
            return i >= 0 && i < ImportzuordnungSchema.Zielverweise.Count ? ImportzuordnungSchema.Zielverweise[i].Key : ziel.ToString();
        }

        /// <summary>Die zulässigen Ziele einer Art: Zonen und Bauteile DIESES Gebäudes, Aufbauten und Baustoffe SEINES Projekts.</summary>
        private static HashSet<int> Ziele(DbVorgang v, ImportZiel art, int idGebaeude, int idProjekt)
        {
            DataTable t;
            switch (art)
            {
                case ImportZiel.Zone:
                    t = v.Lese("SELECT \"ID\" FROM \"" + SchemaKatalog.TAB_ZONE + "\" WHERE \"ID_Gebaeude\" = ?",
                               new DbParam("@g", idGebaeude));
                    break;
                case ImportZiel.Bauteil:
                    t = v.Lese("SELECT b.\"ID\" FROM \"" + SchemaKatalog.TAB_BAUTEIL + "\" b INNER JOIN \"" + SchemaKatalog.TAB_ZONE +
                               "\" z ON z.\"ID\" = b.\"ID_Zone\" WHERE z.\"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude));
                    break;
                case ImportZiel.Aufbau:
                    t = v.Lese("SELECT \"ID\" FROM \"" + SchemaKatalog.TAB_BAUTEILAUFBAU + "\" WHERE \"ID_Projekt\" = ?",
                               new DbParam("@p", idProjekt));
                    break;
                case ImportZiel.Baustoff:
                    t = v.Lese("SELECT \"ID\" FROM \"" + SchemaKatalog.TAB_BAUSTOFF + "\" WHERE \"ID_Projekt\" = ?",
                               new DbParam("@p", idProjekt));
                    break;
                default:
                    return new HashSet<int>();
            }
            return new HashSet<int>(t.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)));
        }

        /// <summary>Die Werte der Quelle in der Reihenfolge von <see cref="ImportzuordnungSchema.Quellspalten"/>.</summary>
        private static IEnumerable<DbParam> Quellwerte(int idGebaeude, GebaeudeQuelle q)
        {
            yield return new DbParam("@g", idGebaeude);
            yield return new DbParam("@f", q.Format);
            yield return new DbParam("@d", q.Dateiname);
            yield return new DbParam("@h", q.Hash);
            yield return new DbParam("@gr", DbParamTyp.BigInt) { Wert = q.Groesse };
            yield return BaustoffCtrl.Text("@s", q.Schemastand);
            yield return new DbParam("@z", q.Zeitpunkt);
            yield return BaustoffCtrl.Text("@p", q.Programmfassung);
            yield return BaustoffCtrl.Text("@r", q.Zonenregel);
            yield return new DbParam("@fe", q.FehlendeEntitaeten);
        }

        /// <summary>
        /// Die Werte einer Paarung in der Reihenfolge von <see cref="ImportzuordnungSchema.Zuordnungsspalten"/>:
        /// genau eines der fünf Ziele gesetzt, die übrigen NULL.
        /// </summary>
        private static IEnumerable<DbParam> Zuordnungswerte(int idQuelle, GebaeudeQuellzuordnung z, int ziel)
        {
            yield return new DbParam("@q", idQuelle);
            yield return Ziel("@zg", z.Ziel == ImportZiel.Gebaeude, ziel);
            yield return Ziel("@zz", z.Ziel == ImportZiel.Zone, ziel);
            yield return Ziel("@zb", z.Ziel == ImportZiel.Bauteil, ziel);
            yield return Ziel("@za", z.Ziel == ImportZiel.Aufbau, ziel);
            yield return Ziel("@zs", z.Ziel == ImportZiel.Baustoff, ziel);
            yield return new DbParam("@k", Quellkennung.Kuerzen(z.Quellkennung));
            yield return new DbParam("@t", z.Quelltyp.Trim());
        }

        private static DbParam Ziel(string name, bool gesetzt, int id)
            => new DbParam(name, DbParamTyp.Integer) { Wert = gesetzt ? (object)id : DBNull.Value };

        private static ImportquelleModel Quelle(DataRow r)
        {
            return new ImportquelleModel
            {
                ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                ID_Gebaeude = Convert.ToInt32(r["ID_Gebaeude"], CultureInfo.InvariantCulture),
                Format = BaustoffCtrl.TextAus(r, "Format") ?? "",
                Dateiname = BaustoffCtrl.TextAus(r, "Dateiname") ?? "",
                Hash = BaustoffCtrl.TextAus(r, "Hash") ?? "",
                Groesse = Convert.ToInt64(r["Groesse"], CultureInfo.InvariantCulture),
                Schemastand = BaustoffCtrl.TextAus(r, "Schemastand"),
                Zeitpunkt = BaustoffCtrl.TextAus(r, "Zeitpunkt") ?? "",
                Programmfassung = BaustoffCtrl.TextAus(r, "Programmfassung"),
                Zonenregel = BaustoffCtrl.TextAus(r, "Zonenregel"),
                FehlendeEntitaeten = BaustoffCtrl.GanzAus(r, "FehlendeEntitaeten") ?? 0
            };
        }

        private static List<ImportzuordnungModel> Zuordnungen(DataTable t)
        {
            var liste = new List<ImportzuordnungModel>();
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new ImportzuordnungModel
                {
                    ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                    ID_Importquelle = Convert.ToInt32(r["ID_Importquelle"], CultureInfo.InvariantCulture),
                    ID_Gebaeude = BaustoffCtrl.GanzAus(r, "ID_Gebaeude"),
                    ID_Zone = BaustoffCtrl.GanzAus(r, "ID_Zone"),
                    ID_Bauteil = BaustoffCtrl.GanzAus(r, "ID_Bauteil"),
                    ID_Aufbau = BaustoffCtrl.GanzAus(r, "ID_Aufbau"),
                    ID_Baustoff = BaustoffCtrl.GanzAus(r, "ID_Baustoff"),
                    Quellkennung = BaustoffCtrl.TextAus(r, "Quellkennung") ?? "",
                    Quelltyp = BaustoffCtrl.TextAus(r, "Quelltyp") ?? ""
                });
            return liste;
        }
    }
}
