using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kopfzeile einer Kostenvorlage (<c>Tab_KostenVorlage</c>, Etappe KD1/KD2 —
    /// Konzept Kostendialoge Rev. 1.2, § 4.1).
    /// </summary>
    public sealed class KostenVorlageKopf
    {
        public int Id;
        public int KomponentenId;
        public int KategorieId;
        public string Name;
        public bool IstStandard;

        /// <summary>Auslieferungs-Seed: nur über „Speichern unter" kopierbar.</summary>
        public bool NurLesen;
    }

    /// <summary>Eine Position einer Kostenvorlage (<c>Tab_KostenVorlagePosition</c>).
    /// NULL heißt durchgängig „nicht gepflegt", nie 0.</summary>
    public sealed class KostenVorlagenPosition
    {
        public int Id;
        public int VorlageId;
        public int? StammId;
        public string Bezeichnung;
        public string Kostenart;
        public string Bemessung;
        public double? Satz;
        public double? BetragNetto;
        public bool IstErloes;
        public double? Nutzungsdauer;
        public double? EmpfehlungVon;
        public double? EmpfehlungBis;
        public int Sortierung;

        /// <summary>ETAPPE H3 (Schritt 59): Pflichtposition der Komponente — wandert
        /// bei jeder Übernahme in die Projektzeile (Löschsperre H1-2) und steuert die
        /// Auto-Anlage (H1-3). false, wenn die Spalte in einer nie migrierten
        /// Datenbank fehlt.</summary>
        public bool IstPflicht;
    }

    /// <summary>
    /// Datenzugriff der Kostenvorlagen (Etappe KD2, Konzept Kostendialoge Rev. 1.2,
    /// § 5): Lesen und Pflegen von <c>Tab_KostenVorlage</c>/<c>Tab_KostenVorlagePosition</c>.
    ///
    /// <para><b>Der Dialog rechnet und schreibt nicht selbst</b> (Hausmuster
    /// <c>Form_BkUebernahme</c>): Prüf- und Schreiblogik liegen hier, UI-frei und
    /// testbar. Alle Schreibwege prüfen den <c>ReadOnly</c>-Schutz der
    /// Auslieferungsvorlagen; IDs entstehen per MAX+1 (kein AutoWert, ADR-001).</para>
    /// </summary>
    public static class KostenVorlagenCtrl
    {
        // ------------------------------------------------------------------ Lesen ---

        /// <summary>
        /// Ä7 (Entscheidung Philipp 26.08.2026): Zur AUSWAHL stehen überall nur die
        /// sieben Anlagen-Komponenten des Projektbaums — dieselben Kacheln, die der
        /// Komponenten-Wizard anbietet. Die Erfassungsgruppen der KD1-Saat
        /// (Wärmezentrale, Bauliche Anlagen, Stromeinspeisung) bleiben als
        /// Datensätze samt Vorlagenpositionen erhalten — vorhandene Projektdaten
        /// dazu werden weiter gerechnet und berichtet —, werden aber nirgends mehr
        /// zur Auswahl angeboten.
        /// </summary>
        public static readonly string[] WaehlbareKomponenten =
        {
            DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE,
            DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL,
            DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK,
            DbWerte.KOSTEN_KOMPONENTE_SOLARTHERMIE,
            DbWerte.KOSTEN_KOMPONENTE_STROMSPEICHER,
            DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER,
            DbWerte.KOSTEN_KOMPONENTE_BHKW
        };

        /// <summary>true, wenn die Komponente zur Auswahl angeboten wird (Ä7).</summary>
        public static bool IstWaehlbar(string komponente)
        {
            foreach (string k in WaehlbareKomponenten)
                if (string.Equals(k, komponente, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Die wählbaren Kostenkomponenten (Ä7; ID, Name), Reihenfolge der Auslieferung.</summary>
        public static IList<KeyValuePair<int, string>> Komponenten()
        {
            var liste = new List<KeyValuePair<int, string>>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID], [" + SchemaKatalog.SPALTE_KK_KOMPONENTE + "] FROM [" +
                SchemaKatalog.TAB_KOSTENKOMPONENTE + "] ORDER BY [ID]");
            foreach (DataRow r in dt.Rows)
            {
                string name = Convert.ToString(r[1]);
                if (!IstWaehlbar(name)) continue;   // Ä7
                liste.Add(new KeyValuePair<int, string>(Convert.ToInt32(r[0]), name));
            }
            return liste;
        }

        /// <summary>Alle Varianten einer Komponente+Kategorie; Standard zuerst.</summary>
        public static IList<KostenVorlageKopf> Vorlagen(int komponentenId, int kategorieId)
        {
            var liste = new List<KostenVorlageKopf>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID], [" + SchemaKatalog.SPALTE_KV_NAME + "], [" +
                SchemaKatalog.SPALTE_KV_IST_STANDARD + "], [" +
                SchemaKatalog.SPALTE_KV_READONLY + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = ? AND [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "] = ? ORDER BY [" +
                SchemaKatalog.SPALTE_KV_IST_STANDARD + "] DESC, [" +
                SchemaKatalog.SPALTE_KV_NAME + "]",
                new OleDbParameter("@kid", komponentenId),
                new OleDbParameter("@kat", kategorieId));
            foreach (DataRow r in dt.Rows)
                liste.Add(new KostenVorlageKopf
                {
                    Id = Convert.ToInt32(r[0]),
                    KomponentenId = komponentenId,
                    KategorieId = kategorieId,
                    Name = Convert.ToString(r[1]),
                    IstStandard = r[2] != DBNull.Value && Convert.ToBoolean(r[2]),
                    // Ä8: Flag nur noch Herkunftsmarker — die UI zeigt alles editierbar.
                    NurLesen = false,
                });
            return liste;
        }

        /// <summary>Positionen einer Vorlage in Rasterreihenfolge.</summary>
        public static IList<KostenVorlagenPosition> Positionen(int vorlageId)
        {
            var liste = new List<KostenVorlagenPosition>();
            // ETAPPE H3: IstPflicht (Schritt 59) nur lesen, wo die Spalte existiert —
            // eine nie migrierte Datenbank lieferte sonst einen Abfragefehler.
            bool mitPflicht = PflichtSpalteVorhanden();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID], [" + SchemaKatalog.SPALTE_KVP_STAMMID + "], [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "], [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_SATZ + "], [" +
                SchemaKatalog.SPALTE_KVP_BETRAG_NETTO + "], [" +
                SchemaKatalog.SPALTE_KVP_IST_ERLOES + "], [" +
                SchemaKatalog.SPALTE_KVP_NUTZUNGSDAUER + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "], [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]" +
                (mitPflicht ? ", [" + SchemaKatalog.SPALTE_KVP_IST_PFLICHT + "]" : "") +
                " FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ? ORDER BY [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "], [ID]",
                new OleDbParameter("@vid", vorlageId));
            foreach (DataRow r in dt.Rows)
                liste.Add(new KostenVorlagenPosition
                {
                    Id = Convert.ToInt32(r[0]),
                    VorlageId = vorlageId,
                    StammId = ZahlOderNull(r[1]),
                    Bezeichnung = Convert.ToString(r[2]),
                    Kostenart = Convert.ToString(r[3]),
                    Bemessung = Convert.ToString(r[4]),
                    Satz = WertOderNull(r[5]),
                    BetragNetto = WertOderNull(r[6]),
                    IstErloes = r[7] != DBNull.Value && Convert.ToBoolean(r[7]),
                    Nutzungsdauer = WertOderNull(r[8]),
                    EmpfehlungVon = WertOderNull(r[9]),
                    EmpfehlungBis = WertOderNull(r[10]),
                    Sortierung = r[11] == DBNull.Value ? 0 : Convert.ToInt32(r[11]),
                    IstPflicht = mitPflicht && r[12] != DBNull.Value && Convert.ToBoolean(r[12]),
                });
            return liste;
        }

        /// <summary>ETAPPE H3: Probe der Schritt-59-Spalte an der Vorlagentabelle
        /// (Muster <see cref="WirtschaftlichkeitCtrl.SpalteVorhanden"/>, Ergebnis je
        /// Prozess gemerkt).</summary>
        private static bool? _pflichtSpalte;

        private static bool PflichtSpalteVorhanden()
        {
            if (_pflichtSpalte.HasValue) return _pflichtSpalte.Value;
            _pflichtSpalte = WirtschaftlichkeitCtrl.SpalteVorhanden(
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, SchemaKatalog.SPALTE_KVP_IST_PFLICHT);
            return _pflichtSpalte.Value;
        }

        /// <summary>
        /// Ä8 (Nutzerentscheid 26.08.2026): Der Schreibschutz der
        /// Auslieferungsvorlagen ist AUFGEHOBEN — für Investitions- UND
        /// Betriebskostenvorlagen. Die Auslieferungswerte dürfen direkt gepflegt
        /// werden; das <c>ReadOnly</c>-Flag bleibt in der Datenbank als reiner
        /// Herkunftsmarker der Saat stehen. Einziger Restschutz: Die
        /// STANDARD-Vorlage einer Komponente kann nicht gelöscht werden
        /// (<see cref="VorlageLoeschen"/>) — sie ist die Quelle von
        /// „Speichern unter…" und der Übernahme-Mechanik (§ 8), und die
        /// KD1-Saat läuft nicht erneut.
        /// </summary>
        public static bool IstNurLesen(int vorlageId)
        {
            return false;
        }

        /// <summary>true, wenn die Vorlage die Standardvorlage ihrer Komponente ist.</summary>
        public static bool IstStandard(int vorlageId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [" + SchemaKatalog.SPALTE_KV_IST_STANDARD + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new OleDbParameter("@id", vorlageId));
            return o != null && o != DBNull.Value && Convert.ToBoolean(o);
        }

        /// <summary>Umsatzsteuersatz [%] aus dem Gesetzeskatalog
        /// (<c>UMSATZSTEUER_REGELSATZ</c>, seit Etappe E1 gesät; KL5: reine Anzeige).</summary>
        public static double? UstSatzProzent()
        {
            try
            {
                return new GesetzKatalog().Wert(
                    DbWerte.GESETZ_UMSATZSTEUER_REGELSATZ, DateTime.Now.Year);
            }
            catch { return null; }
        }

        // -------------------------------------------------------------- Schreiben ---

        /// <summary>Leere neue Variante; Rückgabe ID oder 0 bei Fehler/Namensdublette.</summary>
        public static int VorlageNeu(int komponentenId, int kategorieId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            if (NameBelegt(komponentenId, kategorieId, name)) return 0;

            int id = MaxId(SchemaKatalog.TAB_KOSTENVORLAGE) + 1;
            int n = DataRepository.ExecuteNonQuery(
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] ([ID], [" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "], [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "], [" +
                SchemaKatalog.SPALTE_KV_NAME + "], [" +
                SchemaKatalog.SPALTE_KV_IST_STANDARD + "], [" +
                SchemaKatalog.SPALTE_KV_READONLY + "], [" +
                SchemaKatalog.SPALTE_KV_GEAENDERT_AM + "]) VALUES (?, ?, ?, ?, FALSE, FALSE, ?)",
                new OleDbParameter("@id", id),
                new OleDbParameter("@kid", komponentenId),
                new OleDbParameter("@kat", kategorieId),
                new OleDbParameter("@n", name.Trim()),
                Datum("@am", DateTime.Now));
            return n == 1 ? id : 0;
        }

        /// <summary>
        /// „Speichern unter": kopiert Kopf und alle Positionen der Quelle in eine neue,
        /// editierbare Variante (auch von ReadOnly-Vorlagen — genau dafür ist der Weg da).
        /// Rückgabe ID der Kopie oder 0.
        /// </summary>
        public static int SpeichernUnter(int quellVorlageId, string neuerName)
        {
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT [" + SchemaKatalog.SPALTE_KV_KOMPONENTENID + "], [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new OleDbParameter("@id", quellVorlageId));
            if (kopf.Rows.Count != 1) return 0;

            int neueId = VorlageNeu(Convert.ToInt32(kopf.Rows[0][0]),
                                    Convert.ToInt32(kopf.Rows[0][1]), neuerName);
            if (neueId == 0) return 0;

            foreach (KostenVorlagenPosition p in Positionen(quellVorlageId))
            {
                p.VorlageId = neueId;
                if (PositionAnlegen(p) == 0)
                {
                    // Halbe Kopie zurücknehmen (Löschweitergabe räumt die Positionen ab).
                    DataRepository.ExecuteNonQuery(
                        "DELETE FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = " + neueId);
                    return 0;
                }
            }
            return neueId;
        }

        /// <summary>Variante löschen (Löschweitergabe räumt die Positionen ab).
        /// ReadOnly-Vorlagen sind geschützt.</summary>
        public static bool VorlageLoeschen(int vorlageId)
        {
            if (IstStandard(vorlageId)) return false;   // Ä8-Restschutz (s. IstNurLesen)
            return DataRepository.ExecuteNonQuery(
                "DELETE FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new OleDbParameter("@id", vorlageId)) == 1;
        }

        /// <summary>Neue Position ans Rasterende (FK2: „+ Position hinzufügen").
        /// Rückgabe ID oder 0; ReadOnly-Schutz.</summary>
        public static int PositionNeu(int vorlageId, string bezeichnung, string kostenart,
                                      string bemessung)
        {
            if (IstNurLesen(vorlageId) || string.IsNullOrWhiteSpace(bezeichnung)) return 0;

            var p = new KostenVorlagenPosition
            {
                VorlageId = vorlageId,
                Bezeichnung = bezeichnung.Trim(),
                Kostenart = kostenart ?? DbWerte.KOSTENART_SONSTIGE,
                Bemessung = bemessung ?? DbWerte.BEMESSUNG_BETRAG,
                Sortierung = NaechsteSortierung(vorlageId),
            };
            int id = PositionAnlegen(p);
            if (id != 0) KopfBeruehren(vorlageId);
            return id;
        }

        /// <summary>Alle Fachfelder einer Position schreiben; ReadOnly-Schutz.</summary>
        public static bool PositionSpeichern(KostenVorlagenPosition p)
        {
            if (p == null || IstNurLesen(p.VorlageId)) return false;

            int n = DataRepository.ExecuteNonQuery(
                "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_SATZ + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_BETRAG_NETTO + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_IST_ERLOES + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_NUTZUNGSDAUER + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "] = ? WHERE [ID] = ?",
                new OleDbParameter("@b", p.Bezeichnung ?? ""),
                new OleDbParameter("@ka", p.Kostenart ?? ""),
                new OleDbParameter("@bm", p.Bemessung ?? ""),
                Wert("@satz", p.Satz),
                Wert("@betrag", p.BetragNetto),
                new OleDbParameter("@erl", p.IstErloes),
                Wert("@nd", p.Nutzungsdauer),
                Wert("@ev", p.EmpfehlungVon),
                Wert("@eb", p.EmpfehlungBis),
                new OleDbParameter("@id", p.Id));
            if (n == 1) KopfBeruehren(p.VorlageId);
            return n == 1;
        }

        /// <summary>Position löschen; ReadOnly-Schutz über die Vorlage.</summary>
        public static bool PositionLoeschen(int positionId)
        {
            object vid = DataRepository.ExecuteScalar(
                "SELECT [" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [ID] = ?",
                new OleDbParameter("@id", positionId));
            if (vid == null || vid == DBNull.Value) return false;
            int vorlageId = Convert.ToInt32(vid);
            if (IstNurLesen(vorlageId)) return false;

            bool ok = DataRepository.ExecuteNonQuery(
                "DELETE FROM [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [ID] = ?",
                new OleDbParameter("@id", positionId)) == 1;
            if (ok) KopfBeruehren(vorlageId);
            return ok;
        }

        // ----------------------------------------------------------------- intern ---

        private static bool NameBelegt(int komponentenId, int kategorieId, string name)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = ? AND [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "] = ? AND [" +
                SchemaKatalog.SPALTE_KV_NAME + "] = ?",
                new OleDbParameter("@kid", komponentenId),
                new OleDbParameter("@kat", kategorieId),
                new OleDbParameter("@n", name.Trim()));
            return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
        }

        private static int PositionAnlegen(KostenVorlagenPosition p)
        {
            int id = MaxId(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION) + 1;
            int n = DataRepository.ExecuteNonQuery(
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] ([ID], [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "], [" +
                SchemaKatalog.SPALTE_KVP_STAMMID + "], [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "], [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_SATZ + "], [" +
                SchemaKatalog.SPALTE_KVP_BETRAG_NETTO + "], [" +
                SchemaKatalog.SPALTE_KVP_IST_ERLOES + "], [" +
                SchemaKatalog.SPALTE_KVP_NUTZUNGSDAUER + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "], [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                new OleDbParameter("@id", id),
                new OleDbParameter("@vid", p.VorlageId),
                Ganz("@sid", p.StammId),
                new OleDbParameter("@b", p.Bezeichnung ?? ""),
                new OleDbParameter("@ka", p.Kostenart ?? ""),
                new OleDbParameter("@bm", p.Bemessung ?? ""),
                Wert("@satz", p.Satz),
                Wert("@betrag", p.BetragNetto),
                new OleDbParameter("@erl", p.IstErloes),
                Wert("@nd", p.Nutzungsdauer),
                Wert("@ev", p.EmpfehlungVon),
                Wert("@eb", p.EmpfehlungBis),
                new OleDbParameter("@so", p.Sortierung));
            return n == 1 ? id : 0;
        }

        private static int NaechsteSortierung(int vorlageId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MAX([" + SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]) FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ?",
                new OleDbParameter("@vid", vorlageId));
            int max = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            return max + 10;
        }

        /// <summary>Pflegestand des Kopfs fortschreiben (jede Positionsänderung).</summary>
        private static void KopfBeruehren(int vorlageId)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] SET [" +
                SchemaKatalog.SPALTE_KV_GEAENDERT_AM + "] = ? WHERE [ID] = ?",
                Datum("@am", DateTime.Now),
                new OleDbParameter("@id", vorlageId));
        }

        private static int MaxId(string tabelle)
        {
            object o = DataRepository.ExecuteScalar("SELECT MAX([ID]) FROM [" + tabelle + "]");
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        private static double? WertOderNull(object o)
        {
            return (o == null || o == DBNull.Value) ? (double?)null : Convert.ToDouble(o);
        }

        private static int? ZahlOderNull(object o)
        {
            return (o == null || o == DBNull.Value) ? (int?)null : Convert.ToInt32(o);
        }

        /// <summary>Nullbarer DOUBLE-Parameter mit ausdrücklichem Typ (ein DBNull ohne
        /// Typ kann der Provider nicht binden — Muster <c>SchemaMigration.ParamOderNull</c>).</summary>
        private static OleDbParameter Wert(string name, double? wert)
        {
            var p = new OleDbParameter(name, OleDbType.Double);
            p.Value = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>Nullbarer LONG-Parameter.</summary>
        private static OleDbParameter Ganz(string name, int? wert)
        {
            var p = new OleDbParameter(name, OleDbType.Integer);
            p.Value = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>DATETIME-Parameter.</summary>
        private static OleDbParameter Datum(string name, DateTime wert)
        {
            var p = new OleDbParameter(name, OleDbType.Date);
            p.Value = wert;
            return p;
        }
    }

    /// <summary>
    /// Der Bemessungskatalog der Oberfläche (Konzept § 5.3) — EINE Wahrheit für
    /// Auswahlliste, Einheitenanzeige und Kopplungsregel (KL4/§ 5.4); auch die
    /// Projektseite (KD3) liest hier.
    /// </summary>
    public static class BemessungKatalog
    {
        /// <summary>Ein Eintrag des Katalogs.</summary>
        public sealed class Info
        {
            /// <summary>Persistenzwert (<c>DbWerte.BEMESSUNG_*</c>).</summary>
            public string Persistenz;

            /// <summary>MyResource-Schlüssel des Anzeigetexts.</summary>
            public string ResourceKey;

            /// <summary>Deutscher Rückfalltext (= Designer-Vorgabe, Ä6-Regel 2).</summary>
            public string AnzeigeDe;

            /// <summary>Einheiten-Suffix hinter dem Satzfeld („€/kW", „%", …).</summary>
            public string Einheit;

            /// <summary>In der Auswahl des Investitionsrasters?</summary>
            public bool FuerInvest;

            /// <summary>In der Auswahl des Betriebsrasters?</summary>
            public bool FuerBetrieb;

            /// <summary>Absolut (Satz = Betrag, § 5.4) statt bezugsgrößen-abhängig.</summary>
            public bool Absolut;
        }

        /// <summary>Katalog § 5.3; die beiden Altwerte (generisch je kWh / je Stunde)
        /// stehen nur für die Anzeige von Bestandsdaten, nicht in den Auswahllisten.</summary>
        public static readonly Info[] Alle =
        {
            N(DbWerte.BEMESSUNG_BETRAG,                  "BM_BETRAG",            "fester Betrag",          "€",     true,  false, true),
            N(DbWerte.BEMESSUNG_JAHRESBETRAG,            "BM_JAHRESBETRAG",      "fester Jahresbetrag",    "€/a",   false, true,  true),
            N(DbWerte.BEMESSUNG_PROZENT_INVESTITION,     "BM_P_INVESTITION",     "% der Investition",      "%",     true,  true,  false),
            N(DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,  "BM_P_ERZEUGER",        "% der Erzeugerkosten",   "%",     true,  false, false),
            // ETAPPE H1 — Hilfsenergie an der Endenergie der Anlage (Festlegung 29.08.2026).
            // Die MENGE ist kein Eingabewert, sondern ein ERGEBNISWERT: Sie kommt aus dem
            // Simulationslauf, im Dialog wird nur der Satz gepflegt. Ohne Lauf gibt es
            // keine Menge und damit keinen Betrag - dann bleibt die absolute Angabe.
            N(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,"BM_P_ENDENERGIEKOSTEN","% der Endenergiekosten", "%",     false, true,  false),
            N(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,"BM_P_ENDENERGIEBEDARF","% des Endenergiebedarfs","%",     false, true,  false),
            // Von H1 abgeloest: dieselbe Groesse, aber je Energieart getrennt und
            // projektweit bemessen. Bestandsdaten werden weiter ANGEZEIGT und gerechnet,
            // zur Neuauswahl stehen sie nicht mehr (FuerBetrieb = false).
            N(DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN,"BM_P_BRENNSTOFF",      "% der Brennstoffkosten", "%",     false, false, false),
            N(DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN,     "BM_P_STROM",           "% der Stromkosten",      "%",     false, false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,   "BM_KWH_THERMISCH",     "je kWh thermisch",       "€/kWh", false, true,  false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,  "BM_KWH_ELEKTRISCH",    "je kWh elektrisch",      "€/kWh", false, true,  false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,     "BM_KW_LEISTUNG",       "je kW Leistung",         "€/kW",  true,  false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, "BM_KW_HEIZLEISTUNG",   "je kW Heizleistung",     "€/kW",  true,  false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,   "BM_KW_ELEKTRISCH",     "je kW elektrisch",       "€/kW",  true,  false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KWP,             "BM_KWP",               "je kWp Leistung",        "€/kWp", true,  false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,  "BM_KWH_KAPAZITAET",    "je kWh Kapazität",       "€/kWh", true,  false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR,    "BM_M2_KOLLEKTOR",      "je m² Kollektorfläche",  "€/m²",  true,  false, false),
            // Altwerte — Anzeige von Bestandsdaten, keine Neuauswahl:
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH,             "BM_KWH",               "je kWh",                 "€/kWh", false, false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_H,               "BM_STUNDE",            "je Stunde",              "€/h",   false, false, false),
        };

        /// <summary>Eintrag zum Persistenzwert; NULL bei unbekanntem Wert.</summary>
        public static Info Finde(string persistenz)
        {
            foreach (Info i in Alle)
                if (string.Equals(i.Persistenz, persistenz, StringComparison.Ordinal)) return i;
            return null;
        }

        /// <summary>Anzeigetext (MyResource, deutscher Rückfall).</summary>
        public static string Anzeige(string persistenz)
        {
            Info i = Finde(persistenz);
            if (i == null) return persistenz ?? "";
            string text = null;
            try { text = MyResource.Resource.ResourceManager.GetString(i.ResourceKey); }
            catch { }
            return string.IsNullOrEmpty(text) ? i.AnzeigeDe : text;
        }

        private static Info N(string persistenz, string key, string de, string einheit,
                              bool invest, bool betrieb, bool absolut)
        {
            return new Info
            {
                Persistenz = persistenz, ResourceKey = key, AnzeigeDe = de, Einheit = einheit,
                FuerInvest = invest, FuerBetrieb = betrieb, Absolut = absolut,
            };
        }
    }
}
