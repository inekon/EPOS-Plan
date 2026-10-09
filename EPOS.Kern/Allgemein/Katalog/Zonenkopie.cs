using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die Ebene, auf der die Zonen eines Gebäudes stehen (Schritt ZK).</summary>
    public enum Zonenebene
    {
        /// <summary>Ein Projektgebäude: <c>Tab_Zone</c>, <c>Tab_Bauteil</c>, <c>Tab_Zonenluftstrom</c>.</summary>
        Projekt,

        /// <summary>Ein Katalogsatz: <c>Tab_Zone_STAMM</c>, <c>Tab_Bauteil_STAMM</c>, <c>Tab_Zonenluftstrom_STAMM</c>.</summary>
        Katalog,
    }

    /// <summary>
    /// <b>Die Zonen eines Gebäudes von einer Ebene in die andere kopieren</b> (Schritt ZK, Anwenderwunsch
    /// 08.10.2026): Zonen in ihrer Rangfolge, ihre Bauteile mit umgeschlüsselter Nachbarzone, die Luftströme
    /// zwischen den neuen Zonen und die Konditionierung jeder Zone (<see cref="Konditionierungskopie"/> mit den
    /// Eigentümern <see cref="Kalendereigentuemer.Zone"/> und <see cref="Kalendereigentuemer.Katalogzone"/>). EINE
    /// Stelle für alle Wege: Katalog → Projekt (Übernahme), Projekt → Katalog (aus dem Projekt übernehmen,
    /// Speichern unter), Katalog → Katalog (Duplizieren).
    ///
    /// <para><b>Spalten:</b> die Schnittmenge beider Tabellen ohne Id, Eigentümer- und Verweisspalten — eine
    /// künftige Fachspalte reist mit, sobald beide Seiten sie führen. <b>Aufbau:</b> Katalog → Projekt kopiert
    /// den Katalogaufbau ins Projekt (<see cref="BauteilaufbauCtrl.CopyFromStamm"/>, ein vorhandener gleichen
    /// Namens wird genommen); Projekt → Katalog nimmt den Katalogaufbau gleichen Namens, sonst bleibt der Verweis
    /// leer und das Bauteil behält seinen U-Wert.</para>
    ///
    /// <para>Der Aufrufer hält den Vorgang (Commit, Rollback) — wie bei <see cref="Konditionierungskopie"/>.</para>
    /// </summary>
    public static class Zonenkopie
    {
        /// <summary>Was eine Kopie bewegt hat.</summary>
        public sealed record Befund(bool Ok, int Zonen, int Bauteile, int Luftstroeme, int Konditionierung, string Meldung)
        {
            /// <summary>Nichts zu tun (keine Zonen oder Schritt ZK fehlt).</summary>
            public static readonly Befund Nichts = new Befund(true, 0, 0, 0, 0, "");

            /// <summary>Der benannte Fehlschlag.</summary>
            public static Befund Fehler(string meldung) => new Befund(false, 0, 0, 0, 0, meldung ?? "");
        }

        /// <summary>Die Tabellen einer Ebene.</summary>
        internal sealed record Ablage(string Zone, string Bauteil, string Luftstrom, string Aufbau);

        /// <summary>Die Tabellen der Ebene <paramref name="e"/>.</summary>
        internal static Ablage Tabellen(Zonenebene e)
            => e == Zonenebene.Katalog
                ? new Ablage(ZonenKatalogSchema.TAB_ZONE, ZonenKatalogSchema.TAB_BAUTEIL, ZonenKatalogSchema.TAB_LUFTSTROM,
                             BauteilaufbauSchema.TAB_AUFBAU_STAMM)
                : new Ablage(ZonenSchema.TAB_ZONE, ZonenSchema.TAB_BAUTEIL, ZonenkopplungSchema.TAB_LUFTSTROM,
                             BauteilaufbauSchema.TAB_AUFBAU);

        /// <summary>Die Spalten, die eine Zonenkopie nicht vom Original übernimmt.</summary>
        private static readonly HashSet<string> OHNE_ZONE = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "ID", "ID_Gebaeude", "ReadOnly" };

        /// <summary>Die Spalten, die eine Bauteilkopie nicht vom Original übernimmt (umgeschlüsselt).</summary>
        private static readonly HashSet<string> OHNE_BAUTEIL = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "ID", "ID_Zone", "ID_Nachbarzone", "ID_Aufbau", "ReadOnly" };

        /// <summary>Ist die Ebene in dieser Datenbank lesbar? Der Katalog braucht Schritt ZK.</summary>
        public static bool Lesbar(Zonenebene e)
            => e == Zonenebene.Projekt
                ? DataRepository.TabelleVorhanden(ZonenSchema.TAB_ZONE)
                : ZonenKatalogSchema.Lesbar();

        /// <summary>Die Zahl der Zonen eines Gebäudes der Ebene (0 ohne Tabellen). Schreibt nichts.</summary>
        public static int Anzahl(Zonenebene e, long idGebaeude)
        {
            if (!Lesbar(e)) return 0;
            object o = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + Tabellen(e).Zone + "\" WHERE \"ID_Gebaeude\" = ?",
                                                    new DbParam("@g", idGebaeude));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Entfernt die Zonen eines Gebäudes der Ebene samt Bauteilen, Luftströmen und Zonen-Konditionierung (die
        /// Fremdschlüssel kaskadieren) im Vorgang <paramref name="v"/>. Gibt die Zahl der Zonen zurück.
        /// </summary>
        public static int Entfernen(DbVorgang v, Zonenebene e, long idGebaeude)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (!Lesbar(e)) return 0;
            return v.Ausfuehren("DELETE FROM \"" + Tabellen(e).Zone + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude));
        }

        /// <summary>
        /// <b>Kopiert die Zonen</b> des Gebäudes <paramref name="idVon"/> der Ebene <paramref name="von"/> an das Gebäude
        /// <paramref name="idNach"/> der Ebene <paramref name="nach"/> — zusätzlich zu dessen Zonen; wer ersetzt,
        /// ruft vorher <see cref="Entfernen"/>.
        /// </summary>
        /// <param name="v">Der laufende Vorgang.</param>
        /// <param name="von">Die Ebene der Quelle.</param>
        /// <param name="idVon">Das Quellgebäude (<c>Tab_Gebaeude.ID</c> oder <c>Tab_Gebaeude_STAMM.ID</c>).</param>
        /// <param name="nach">Die Ebene des Ziels.</param>
        /// <param name="idNach">Das Zielgebäude.</param>
        /// <param name="idProjekt">Das Projekt einer Seite der Ebene <see cref="Zonenebene.Projekt"/> (Aufbauten); sonst 0.</param>
        public static Befund Kopieren(DbVorgang v, Zonenebene von, long idVon, Zonenebene nach, long idNach, int idProjekt)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (!Lesbar(von) || !Lesbar(nach)) return Befund.Nichts;
            Ablage q = Tabellen(von), z = Tabellen(nach);

            DataTable zonen = v.Lese("SELECT \"ID\" FROM \"" + q.Zone + "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"Rang\", \"ID\"",
                                     new DbParam("@g", idVon));
            if (zonen == null || zonen.Rows.Count == 0) return Befund.Nichts;

            // 1) Die Zonen in Rangfolge; alt -> neu.
            string zonenliste = Liste(Schnitt(v, q.Zone, z.Zone, OHNE_ZONE));
            var neueZone = new Dictionary<long, long>();
            foreach (DataRow r in zonen.Rows)
            {
                long alt = Convert.ToInt64(r[0], CultureInfo.InvariantCulture);
                v.Ausfuehren("INSERT INTO \"" + z.Zone + "\" (\"ID_Gebaeude\", " + zonenliste + ") SELECT ?, " + zonenliste +
                             " FROM \"" + q.Zone + "\" WHERE \"ID\" = ?", new DbParam("@n", idNach), new DbParam("@alt", alt));
                neueZone[alt] = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            }

            // 2) Die Bauteile je Zone; Nachbarzone und Aufbau umgeschlüsselt.
            string bauteilliste = Liste(Schnitt(v, q.Bauteil, z.Bauteil, OHNE_BAUTEIL));
            bool nachbar = Hat(v, q.Bauteil, "ID_Nachbarzone") && Hat(v, z.Bauteil, "ID_Nachbarzone");
            var aufbauten = new Dictionary<long, long?>();
            int bauteile = 0;
            foreach (KeyValuePair<long, long> zone in neueZone)
            {
                DataTable bt = v.Lese("SELECT \"ID\", \"ID_Aufbau\"" + (nachbar ? ", \"ID_Nachbarzone\"" : "") + " FROM \"" + q.Bauteil +
                                      "\" WHERE \"ID_Zone\" = ? ORDER BY \"Rang\", \"ID\"", new DbParam("@z", zone.Key));
                foreach (DataRow b in bt.Rows)
                {
                    long alt = Convert.ToInt64(b["ID"], CultureInfo.InvariantCulture);
                    long? aufbau = Lang(b, "ID_Aufbau");
                    long? aufbauNeu = aufbau.HasValue ? Aufbau(v, von, nach, aufbau.Value, idProjekt, aufbauten) : null;
                    long? nachbarAlt = nachbar ? Lang(b, "ID_Nachbarzone") : null;
                    // Eine Nachbarzone eines fremden Gebaeudes kann es nicht geben; fehlt sie in der Zuordnung, bleibt der
                    // Verweis leer statt auf eine fremde Zone zu zeigen.
                    long? nachbarNeu = nachbarAlt.HasValue && neueZone.TryGetValue(nachbarAlt.Value, out long n) ? n : (long?)null;

                    v.Ausfuehren("INSERT INTO \"" + z.Bauteil + "\" (\"ID_Zone\", \"ID_Aufbau\"" + (nachbar ? ", \"ID_Nachbarzone\"" : "") +
                                 ", " + bauteilliste + ") SELECT ?, ?" + (nachbar ? ", ?" : "") + ", " + bauteilliste +
                                 " FROM \"" + q.Bauteil + "\" WHERE \"ID\" = ?",
                                 nachbar
                                     ? new[] { new DbParam("@z", zone.Value), new DbParam("@a", (object)aufbauNeu),
                                               new DbParam("@n", (object)nachbarNeu), new DbParam("@alt", alt) }
                                     : new[] { new DbParam("@z", zone.Value), new DbParam("@a", (object)aufbauNeu),
                                               new DbParam("@alt", alt) });
                    bauteile++;
                }
            }

            // 3) Die Luftstroeme zwischen den Zonen dieses Gebaeudes; das Paar bleibt aufsteigend (Pruefklausel).
            int stroeme = 0;
            if (DataRepository.TabelleVorhanden(q.Luftstrom) && DataRepository.TabelleVorhanden(z.Luftstrom))
            {
                DataTable ls = v.Lese("SELECT l.\"ID_ZoneA\", l.\"ID_ZoneB\", l.\"Volumenstrom\" FROM \"" + q.Luftstrom + "\" l " +
                                      "INNER JOIN \"" + q.Zone + "\" a ON a.\"ID\" = l.\"ID_ZoneA\" WHERE a.\"ID_Gebaeude\" = ? ORDER BY l.\"ID\"",
                                      new DbParam("@g", idVon));
                foreach (DataRow r in ls.Rows)
                {
                    if (!neueZone.TryGetValue(Convert.ToInt64(r[0], CultureInfo.InvariantCulture), out long a) ||
                        !neueZone.TryGetValue(Convert.ToInt64(r[1], CultureInfo.InvariantCulture), out long b)) continue;
                    v.Ausfuehren("INSERT INTO \"" + z.Luftstrom + "\" (\"ID_ZoneA\", \"ID_ZoneB\", \"Volumenstrom\") VALUES (?, ?, ?)",
                                 new DbParam("@a", Math.Min(a, b)), new DbParam("@b", Math.Max(a, b)), new DbParam("@v", r[2]));
                    stroeme++;
                }
            }

            // 4) Die Konditionierung jeder Zone (Kalender samt Perioden, Vorgaben).
            int kondition = 0;
            if (KonditionierungSchema.Lesbar())
                foreach (KeyValuePair<long, long> zone in neueZone)
                {
                    Konditionierungskopie.Befund k = Konditionierungskopie.Kopieren(
                        v, Eigner(von, idVon, zone.Key), Eigner(nach, idNach, zone.Value), Konditionierungskopie.Auswahl.Alles);
                    if (!k.Ok) return Befund.Fehler(k.Meldung);
                    kondition += k.Vorgaben + k.Kalender;
                }

            return new Befund(true, neueZone.Count, bauteile, stroeme, kondition, "");
        }

        /// <summary>Der Eigentümer der Konditionierung einer Zone der Ebene.</summary>
        internal static KonditionierungCtrl.Eigner Eigner(Zonenebene e, long idGebaeude, long idZone)
            => e == Zonenebene.Katalog
                ? KonditionierungCtrl.Eigner.Katalogzone(idGebaeude, idZone)
                : KonditionierungCtrl.Eigner.Zone(idGebaeude, idZone);

        /// <summary>Der Aufbau auf der Zielebene; <c>null</c>, wenn es ihn dort nicht gibt.</summary>
        private static long? Aufbau(DbVorgang v, Zonenebene von, Zonenebene nach, long id, int idProjekt,
                                    Dictionary<long, long?> gemerkt)
        {
            if (gemerkt.TryGetValue(id, out long? schon)) return schon;
            long? ziel;
            if (von == nach && von == Zonenebene.Katalog) ziel = id;
            else if (von == Zonenebene.Katalog)
            {
                // Katalog -> Projekt: der Katalogaufbau samt Schichten und Stoffen ins Projekt (oder der vorhandene
                // gleichen Namens), im SELBEN Vorgang.
                int neu;
                using (Vorgangsklammer.Setzen(v))
                    neu = idProjekt > 0 ? new BauteilaufbauCtrl().CopyFromStamm((int)id, idProjekt) : -1;
                ziel = neu > 0 ? neu : (long?)null;
            }
            else
            {
                object name = v.Skalar("SELECT \"Bezeichner\" FROM \"" + Tabellen(von).Aufbau + "\" WHERE \"ID\" = ?", new DbParam("@a", id));
                object treffer = name == null || name == DBNull.Value
                    ? null
                    : nach == Zonenebene.Katalog
                        ? v.Skalar("SELECT MIN(\"ID\") FROM \"" + Tabellen(nach).Aufbau + "\" WHERE \"Bezeichner\" = ?", new DbParam("@b", name))
                        : v.Skalar("SELECT MIN(\"ID\") FROM \"" + Tabellen(nach).Aufbau + "\" WHERE \"Bezeichner\" = ? AND \"ID_Projekt\" = ?",
                                   new DbParam("@b", name), new DbParam("@p", idProjekt));
                ziel = treffer == null || treffer == DBNull.Value ? null : Convert.ToInt64(treffer, CultureInfo.InvariantCulture);
            }
            gemerkt[id] = ziel;
            return ziel;
        }

        // =================================================================
        //  Kleine Helfer
        // =================================================================

        /// <summary>Die Spalten, die beide Tabellen führen, in der Folge der Quelle, ohne die genannten.</summary>
        private static List<string> Schnitt(DbVorgang v, string quelle, string ziel, HashSet<string> ohne)
        {
            var zielspalten = new HashSet<string>(Spalten(v, ziel), StringComparer.OrdinalIgnoreCase);
            return Spalten(v, quelle).Where(s => !ohne.Contains(s) && zielspalten.Contains(s)).ToList();
        }

        private static List<string> Spalten(DbVorgang v, string tabelle)
        {
            var liste = new List<string>();
            DataTable dt = v.Lese("SELECT name FROM pragma_table_info(?) ORDER BY cid", new DbParam("@t", tabelle));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                string s = Convert.ToString(r["name"], CultureInfo.InvariantCulture) ?? "";
                if (s.Length > 0) liste.Add(s);
            }
            return liste;
        }

        private static bool Hat(DbVorgang v, string tabelle, string spalte)
            => Spalten(v, tabelle).Contains(spalte, StringComparer.OrdinalIgnoreCase);

        private static string Liste(List<string> spalten) => string.Join(", ", spalten.Select(s => "\"" + s + "\""));

        private static long? Lang(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture)
                : (long?)null;
    }
}
