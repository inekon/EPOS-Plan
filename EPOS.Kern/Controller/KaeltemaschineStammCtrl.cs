using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Katalog der Kältemaschinen</b> (<c>Tab_Kaeltemaschine_STAMM</c> samt Kennlinie, KU3-1): Lesen,
    /// Prüfen, Speichern, Löschen, Duplizieren und das Auslieferungskennzeichen. Alle Zugriffe über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern; Kopf und Kennlinie in EINEM Vorgang.
    /// </summary>
    public static class KaeltemaschineStammCtrl
    {
        /// <summary>Die Katalogtabelle.</summary>
        public const string TABLE = KaeltemaschineSchema.TAB_STAMM;

        /// <summary>Die Kennlinie des Katalogs.</summary>
        public const string CURVE = KaeltemaschineSchema.TAB_KENNDATEN_STAMM;

        /// <summary>Der Ausgang eines Schreibwegs: bei Erfolg die ID, sonst der Grund in der Oberflächensprache.</summary>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, int Id);

        /// <summary>Eine Zeile der Katalogliste.</summary>
        public sealed record Listenzeile(int Id, string Bezeichner, string Firma, double? Nennkaelteleistung_kW,
                                         double? Nenn_EER, string Rueckkuehlart, bool ReadOnly);

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Alle Katalogsätze, nach Bezeichner sortiert.</summary>
        public static IReadOnlyList<Listenzeile> Liste()
        {
            var liste = new List<Listenzeile>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Firma, " + KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG + ", " +
                KaeltemaschineSchema.SPALTE_NENN_EER + ", " + KaeltemaschineSchema.SPALTE_RUECKKUEHLART +
                ", ReadOnly FROM " + TABLE + " ORDER BY Bezeichner");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add(new Listenzeile(Ganz(r["ID"]), Text(r["Bezeichner"]) ?? "", Text(r["Firma"]),
                                          Zahl(r[KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG]),
                                          Zahl(r[KaeltemaschineSchema.SPALTE_NENN_EER]),
                                          Text(r[KaeltemaschineSchema.SPALTE_RUECKKUEHLART]),
                                          Ganz(r["ReadOnly"]) == 1));
            return liste;
        }

        /// <summary>Ein Katalogsatz samt Kennlinie; <c>null</c>, wenn es die ID nicht gibt.</summary>
        public static KaeltemaschineModel Laden(int id) => Lesen(TABLE, CURVE, id, true);

        /// <summary>Gehört der Satz zur Auslieferung (<c>ReadOnly = 1</c>)?</summary>
        public static bool Gesperrt(int id)
        {
            object o = DataRepository.ExecuteScalar("SELECT ReadOnly FROM " + TABLE + " WHERE ID = ?", new DbParam("?", id));
            return o != null && o != DBNull.Value && Convert.ToInt32(o, CultureInfo.InvariantCulture) == 1;
        }

        /// <summary>Gibt es einen anderen Satz mit diesem Bezeichner?</summary>
        public static bool NameBelegt(string bezeichner, int ausserId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TABLE + " WHERE Bezeichner = ? AND ID <> ?",
                new DbParam("?", (bezeichner ?? "").Trim()), new DbParam("?", ausserId));
            return o != null && o != DBNull.Value && Convert.ToInt32(o, CultureInfo.InvariantCulture) > 0;
        }

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Prüft einen Satz vor dem Schreiben — ohne Datenbank. Liefert <c>null</c>, wenn er schreibbar ist,
        /// sonst den Grund in der Oberflächensprache.
        /// </summary>
        public static string Pruefen(KaeltemaschineModel m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.Bezeichner))
                return MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG;
            if (m.Rueckkuehlart != null && !KaeltemaschineSchema.RUECKKUEHLARTEN.Contains(m.Rueckkuehlart))
                return MyResource.Resource.KM_MSG_RUECKKUEHLART_UNGUELTIG;
            if ((m.Nennkaelteleistung_kW.HasValue && m.Nennkaelteleistung_kW.Value <= 0) ||
                (m.Nenn_EER.HasValue && m.Nenn_EER.Value <= 0))
                return MyResource.Resource.KM_MSG_NENNWERT_UNGUELTIG;
            if (m.Mindestteillast_Prozent.HasValue && (m.Mindestteillast_Prozent.Value < 0 || m.Mindestteillast_Prozent.Value > 100))
                return MyResource.Resource.KM_MSG_TEILLAST_UNGUELTIG;
            if ((m.Hilfsstrom_Rueckkuehlung_kW.HasValue && m.Hilfsstrom_Rueckkuehlung_kW.Value < 0) ||
                (m.Modulkosten.HasValue && m.Modulkosten.Value < 0))
                return MyResource.Resource.KM_MSG_NENNWERT_UNGUELTIG;
            var punkte = new HashSet<(double, double)>();
            foreach (KaeltemaschineKenndatenModel k in m.Kennlinie ?? new List<KaeltemaschineKenndatenModel>())
            {
                if ((k.EER.HasValue && k.EER.Value <= 0) || (k.Kaelteleistung_kW.HasValue && k.Kaelteleistung_kW.Value < 0))
                    return MyResource.Resource.KM_MSG_KENNLINIE_UNGUELTIG;
                if (!punkte.Add((k.Rueckkuehltemperatur, k.Kaltwassertemperatur)))
                    return MyResource.Resource.KM_MSG_KENNLINIE_DOPPELT;
            }
            return null;
        }

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>
        /// Schreibt einen Katalogsatz samt Kennlinie: neu (<c>m.Id == 0</c>) oder über einen bestehenden, der
        /// nicht zur Auslieferung gehört. Ein ausgelieferter Satz wird nie überschrieben (AD-Q11) — ihn ändert
        /// man über <see cref="Duplizieren"/>.
        /// </summary>
        public static SpeicherErgebnis Speichern(KaeltemaschineModel m)
        {
            string grund = Pruefen(m);
            if (grund != null) return new SpeicherErgebnis(false, grund, 0);
            bool neu = m.Id <= 0;
            try
            {
                if (!neu && Gesperrt(m.Id))
                    return new SpeicherErgebnis(false, MyResource.Resource.KM_MSG_AUSGELIEFERT, m.Id);
                if (NameBelegt(m.Bezeichner, neu ? 0 : m.Id))
                    return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, m.Id);

                int id;
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        id = KopfSchreiben(v, TABLE, m, neu, null);
                        KennlinieSchreiben(v, CURVE, id, m.Kennlinie, null);
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                return new SpeicherErgebnis(true, MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT, id);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), 0);
            }
        }

        /// <summary>Löscht einen Katalogsatz samt Kennlinie (Kaskade); ein ausgelieferter Satz bleibt.</summary>
        public static SpeicherErgebnis Loeschen(int id)
        {
            try
            {
                if (Gesperrt(id))
                    return new SpeicherErgebnis(false, MyResource.Resource.KM_MSG_AUSGELIEFERT, id);
                int n = DataRepository.ExecuteNonQuery("DELETE FROM " + TABLE + " WHERE ID = ?", new DbParam("?", id));
                return n > 0
                    ? new SpeicherErgebnis(true, "", id)
                    : new SpeicherErgebnis(false, MyResource.Resource.KBROW_MSG_LOESCHEN_FEHLER, id);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), id);
            }
        }

        /// <summary>Kopiert einen Satz samt Kennlinie als eigenen Anwendersatz (AD-Q11).</summary>
        public static Katalogkopie.Ergebnis Duplizieren(int id, string neuerName) =>
            Katalogkopie.Duplizieren(TABLE, id, neuerName,
                new Katalogkopie.Kindtabelle(CURVE, KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE));

        /// <summary>Setzt oder löst das Auslieferungskennzeichen (Schloss).</summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt) =>
            Auslieferungskennzeichen.SetzenInTabelle(TABLE, ids, gesperrt);

        // =================================================================
        //  Gemeinsame Bausteine (auch für die Projektkopie)
        // =================================================================

        internal static KaeltemaschineModel Lesen(string tabelle, string kennlinie, int id, bool katalog)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + tabelle + " WHERE ID = ?", new DbParam("?", id));
            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            var m = new KaeltemaschineModel
            {
                Id = Ganz(r["ID"]),
                Bezeichner = Text(r["Bezeichner"]) ?? "",
                Firma = Text(r["Firma"]),
                Typ = Text(r["Typ"]),
                Beschreibung = Text(r["Beschreibung"]),
                Nennkaelteleistung_kW = Zahl(r[KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG]),
                Nenn_EER = Zahl(r[KaeltemaschineSchema.SPALTE_NENN_EER]),
                Kaeltemittel = Text(r[KaeltemaschineSchema.SPALTE_KAELTEMITTEL]),
                Rueckkuehlart = Text(r[KaeltemaschineSchema.SPALTE_RUECKKUEHLART]),
                Mindestteillast_Prozent = Zahl(r[KaeltemaschineSchema.SPALTE_MINDESTTEILLAST]),
                Hilfsstrom_Rueckkuehlung_kW = Zahl(r[KaeltemaschineSchema.SPALTE_HILFSSTROM_RUECKKUEHLUNG]),
                Kaltwasser_Vorlauf_Min = Zahl(r[KaeltemaschineSchema.SPALTE_KALTWASSER_VORLAUF_MIN]),
                Modulkosten = Zahl(r[KaeltemaschineSchema.SPALTE_MODULKOSTEN]),
            };
            if (katalog) m.ReadOnly = Ganz(r["ReadOnly"]) == 1;
            else
            {
                m.IdProjekt = r["ID_Projekt"] == DBNull.Value ? (int?)null : Ganz(r["ID_Projekt"]);
                m.IdStamm = r[KaeltemaschineSchema.SPALTE_ID_STAMM] == DBNull.Value ? (int?)null : Ganz(r[KaeltemaschineSchema.SPALTE_ID_STAMM]);
            }
            DataTable k = DataRepository.GetDataTable(
                "SELECT * FROM " + kennlinie + " WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ? ORDER BY " +
                KaeltemaschineSchema.SPALTE_KALTWASSERTEMPERATUR + ", " + KaeltemaschineSchema.SPALTE_RUECKKUEHLTEMPERATUR,
                new DbParam("?", id));
            if (k != null)
                foreach (DataRow z in k.Rows)
                    m.Kennlinie.Add(new KaeltemaschineKenndatenModel
                    {
                        Rueckkuehltemperatur = Zahl(z[KaeltemaschineSchema.SPALTE_RUECKKUEHLTEMPERATUR]) ?? 0,
                        Kaltwassertemperatur = Zahl(z[KaeltemaschineSchema.SPALTE_KALTWASSERTEMPERATUR]) ?? 0,
                        EER = Zahl(z[KaeltemaschineSchema.SPALTE_EER]),
                        Kaelteleistung_kW = Zahl(z[KaeltemaschineSchema.SPALTE_KAELTELEISTUNG]),
                    });
            return m;
        }

        private static object Wert(object o) => o ?? DBNull.Value;

        private static DbParam[] Kopfwerte(KaeltemaschineModel m) => new[]
        {
            new DbParam("?", m.Bezeichner.Trim()), new DbParam("?", Wert(m.Firma)), new DbParam("?", Wert(m.Typ)),
            new DbParam("?", Wert(m.Beschreibung)), new DbParam("?", Wert(m.Nennkaelteleistung_kW)),
            new DbParam("?", Wert(m.Nenn_EER)), new DbParam("?", Wert(m.Kaeltemittel)),
            new DbParam("?", Wert(m.Rueckkuehlart)), new DbParam("?", Wert(m.Mindestteillast_Prozent)),
            new DbParam("?", Wert(m.Hilfsstrom_Rueckkuehlung_kW)), new DbParam("?", Wert(m.Kaltwasser_Vorlauf_Min)),
            new DbParam("?", Wert(m.Modulkosten)),
        };

        /// <summary>Schreibt den Kopf; <paramref name="projekt"/> ≠ <c>null</c> schreibt eine Projektkopie (ID_Projekt, ID_Stamm).</summary>
        internal static int KopfSchreiben(DbVorgang v, string tabelle, KaeltemaschineModel m, bool neu, int? projekt)
        {
            string spalten = string.Join(", ", KaeltemaschineSchema.Fachspalten.Select(s => "\"" + s + "\""));
            if (neu)
            {
                string zusatz = projekt.HasValue ? ", \"ID_Projekt\", \"" + KaeltemaschineSchema.SPALTE_ID_STAMM + "\"" : "";
                string marken = string.Join(", ", KaeltemaschineSchema.Fachspalten.Select(_ => "?")) + (projekt.HasValue ? ", ?, ?" : "");
                var p = Kopfwerte(m).ToList();
                if (projekt.HasValue)
                {
                    p.Add(new DbParam("?", projekt.Value));
                    p.Add(new DbParam("?", Wert(m.IdStamm)));
                }
                v.Ausfuehren("INSERT INTO " + tabelle + " (" + spalten + zusatz + ") VALUES (" + marken + ")", p.ToArray());
                return Convert.ToInt32(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            }
            string setzen = string.Join(", ", KaeltemaschineSchema.Fachspalten.Select(s => "\"" + s + "\" = ?"));
            var q = Kopfwerte(m).ToList();
            q.Add(new DbParam("?", m.Id));
            v.Ausfuehren("UPDATE " + tabelle + " SET " + setzen + " WHERE ID = ?", q.ToArray());
            return m.Id;
        }

        /// <summary>Ersetzt die Kennlinie eines Geräts; <paramref name="projekt"/> ≠ <c>null</c> schreibt <c>ID_Projekt</c> mit.</summary>
        internal static void KennlinieSchreiben(DbVorgang v, string tabelle, int id, IEnumerable<KaeltemaschineKenndatenModel> punkte, int? projekt)
        {
            v.Ausfuehren("DELETE FROM " + tabelle + " WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?", new DbParam("?", id));
            string spalten = KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + ", " + string.Join(", ", KaeltemaschineSchema.KennlinienSpalten) +
                             (projekt.HasValue ? ", ID_Projekt" : "");
            string marken = "?, ?, ?, ?, ?" + (projekt.HasValue ? ", ?" : "");
            foreach (KaeltemaschineKenndatenModel k in punkte ?? Enumerable.Empty<KaeltemaschineKenndatenModel>())
            {
                var p = new List<DbParam>
                {
                    new DbParam("?", id), new DbParam("?", k.Rueckkuehltemperatur), new DbParam("?", k.Kaltwassertemperatur),
                    new DbParam("?", Wert(k.EER)), new DbParam("?", Wert(k.Kaelteleistung_kW)),
                };
                if (projekt.HasValue) p.Add(new DbParam("?", projekt.Value));
                v.Ausfuehren("INSERT INTO " + tabelle + " (" + spalten + ") VALUES (" + marken + ")", p.ToArray());
            }
        }

        internal static string Text(object o) => o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

        internal static double? Zahl(object o) => o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        internal static int Ganz(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// <b>Die Projektkopie der Kältemaschine</b> (<c>Tab_Kaeltemaschine</c>, KU3-1): Übernahme Katalog → Projekt
    /// samt Kennlinie, Lesen. Die Zuordnung zum Katalog trägt <c>ID_Stamm</c> (Muster der Wärmepumpe).
    /// </summary>
    public static class KaeltemaschineCtrl
    {
        /// <summary>Die Projekttabelle.</summary>
        public const string TABLE = KaeltemaschineSchema.TAB_PROJEKT;

        /// <summary>Die Kennlinie der Projektkopie.</summary>
        public const string CURVE = KaeltemaschineSchema.TAB_KENNDATEN;

        /// <summary>Eine Projektkopie samt Kennlinie; <c>null</c>, wenn es die ID nicht gibt.</summary>
        public static KaeltemaschineModel Laden(int id) => KaeltemaschineStammCtrl.Lesen(TABLE, CURVE, id, false);

        /// <summary>Die IDs der Kältemaschinen eines Projekts.</summary>
        public static IReadOnlyList<int> IdsImProjekt(int projektId)
        {
            var ids = new List<int>();
            DataTable dt = DataRepository.GetDataTable("SELECT ID FROM " + TABLE + " WHERE ID_Projekt = ? ORDER BY ID",
                                                       new DbParam("?", projektId));
            if (dt != null) foreach (DataRow r in dt.Rows) ids.Add(KaeltemaschineStammCtrl.Ganz(r["ID"]));
            return ids;
        }

        /// <summary>
        /// Kopiert den Katalogsatz <paramref name="stammId"/> samt Kennlinie in das Projekt
        /// <paramref name="projektId"/> — Spalte für Spalte nach <see cref="KaeltemaschineSchema.Fachspalten"/>,
        /// mit <c>ID_Stamm</c> als Zuordnung. Führt das Projekt schon eine Kopie dieses Katalogsatzes, bleibt
        /// sie und ihre ID kommt zurück. <c>-1</c>, wenn es den Katalogsatz nicht gibt.
        /// </summary>
        public static int AusKatalogUebernehmen(int stammId, int projektId)
        {
            object vorhanden = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TABLE + " WHERE ID_Projekt = ? AND " + KaeltemaschineSchema.SPALTE_ID_STAMM + " = ? ORDER BY ID",
                new DbParam("?", projektId), new DbParam("?", stammId));
            if (vorhanden != null && vorhanden != DBNull.Value) return Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);

            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(stammId);
            if (m == null) return -1;
            m.IdStamm = stammId;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int id = KaeltemaschineStammCtrl.KopfSchreiben(v, TABLE, m, true, projektId);
                    KaeltemaschineStammCtrl.KennlinieSchreiben(v, CURVE, id, m.Kennlinie, projektId);
                    v.Commit();
                    return id;
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
        }
    }
}
