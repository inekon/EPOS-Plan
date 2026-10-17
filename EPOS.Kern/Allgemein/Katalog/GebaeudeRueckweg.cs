using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Eine abweichende Kopfspalte zwischen Projektgebäude und Ursprung (Werte invariant als Text, leer = NULL).</summary>
    public sealed record GebaeudeAbweichung(string Spalte, string Ursprung, string Projekt);

    /// <summary>Die Zahl der Kindzeilen einer Art am Projektgebäude und am Ursprung.</summary>
    public sealed record GebaeudeKindzahl(string Art, int Projekt, int Ursprung);

    /// <summary>
    /// Die Zeile der Rückfrage des Gebäude-Rückwegs: Projektgebäude, Ursprung, ob er überschrieben werden kann
    /// (<see cref="Rueckwegabsage.Keine"/>) oder warum nicht, der Namensvorschlag für „neu", die abweichenden
    /// Kopfspalten und die Kindzeilen je Art.
    /// </summary>
    public sealed record GebaeudeRueckwegVorschau(int IdProjektGebaeude, int IdKopie, string NameKopie, int? IdUrsprung,
                                                  string NameUrsprung, Rueckwegabsage Ueberschreiben, string Namensvorschlag,
                                                  IReadOnlyList<GebaeudeAbweichung> Abweichungen,
                                                  IReadOnlyList<GebaeudeKindzahl> Kindzeilen)
    {
        /// <summary>Kann „Ursprung überschreiben" gewählt werden?</summary>
        public bool KannUeberschreiben => Ueberschreiben == Rueckwegabsage.Keine;

        /// <summary>Dieselbe Zeile im Muster des Gerätewegs (<see cref="Katalogrueckweg.Vorschau"/>) für die Rückfrage.</summary>
        public Rueckwegzeile AlsZeile()
            => new Rueckwegzeile(IdProjektGebaeude, NameKopie, IdUrsprung, NameUrsprung, Ueberschreiben, Namensvorschlag);
    }

    /// <summary>
    /// <b>Gebäude-Rückweg Projekt → Katalog</b> (Konzept Projektdialoge mit Katalogauswahl 5.2, 5.14; Abschnitt 7 Punkt 5):
    /// ein Projektgebäude als neuer Katalogsatz oder als Ersatz seines Ursprungs (<c>Tab_Gebaeude.ID_Gebaeude_Stamm</c>),
    /// in EINEM Vorgang.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>Neu:</b> der Weg <see cref="GebaeudeStammCtrl.AusProjektUebernehmen"/> (Kopf, Herkunft in der Beschreibung,
    /// Konditionierung, Zonen samt Bauteilen, Luftströmen und Zonen-Konditionierung); im selben Vorgang bekommt das
    /// Projektgebäude den neuen Satz als Ursprung, damit ein zweiter Rückweg überschreiben kann.</item>
    /// <item><b>Überschreiben:</b> die Kopfspalten (<see cref="GebaeudeStammCtrl.KOPFSPALTEN"/> und <c>Beschreibung</c>
    /// wörtlich) des Ursprungs ersetzt — der Name bleibt, <c>ReadOnly</c> und <c>Katalog_*</c> werden nie geschrieben, die
    /// Prüfsumme nicht nachgeführt; die Kindzeilen (Zonen, Bauteile, Luftströme, Zonen- und Gebäudekonditionierung samt
    /// Perioden und Wochen) des Ursprungs fallen und kommen aus dem Projektgebäude neu (<see cref="Zonenkopie"/> schlüsselt
    /// die Nachbarzonen um, <see cref="Konditionierungskopie.Auswahl.Ersetzend"/>). Die ID bleibt — die Verweise anderer
    /// Projektgebäude auf den Ursprung bleiben gültig.</item>
    /// <item><b>Absagen</b> (<see cref="Rueckwegabsage"/>): kein Projektgebäude, Name leer, Name belegt, Ursprung gesperrt,
    /// fehlt oder unbekannt, Datenbankfehler — dann ist nichts geschrieben.</item>
    /// </list>
    /// </remarks>
    public static class GebaeudeRueckweg
    {
        /// <summary>Die Arten der Kindzeilen in der Vorschau.</summary>
        public const string ART_ZONEN = "Zonen", ART_BAUTEILE = "Bauteile", ART_LUFTSTROEME = "Luftstroeme",
                            ART_KALENDER = "Kalender", ART_VORGABEN = "Vorgaben", ART_PERIODEN = "Perioden";

        private const string TAB = GebaeudeStammCtrl.TABLE, TAB_PROJ = GebaeudeStammCtrl.TABLE_PROJ;
        private const string SPALTE_URSPRUNG = "ID_Gebaeude_Stamm";

        /// <summary>Die Spalten, die beim Überschreiben vom Projektgebäude in den Ursprung gehen.</summary>
        public static IReadOnlyList<string> Kopfspalten()
        {
            var l = new List<string> { "Beschreibung" };
            l.AddRange(GebaeudeStammCtrl.KOPFSPALTEN.Split(',').Select(s => s.Trim().Trim('[', ']')).Where(s => s.Length > 0));
            return l;
        }

        // ---------------------------------------------------------------- Namen ---

        /// <summary>Ist <paramref name="name"/> im Gebäudekatalog vergeben?</summary>
        public static bool NameBelegt(string name) => GebaeudeStammCtrl.NameVergeben((name ?? "").Trim());

        /// <summary>
        /// Der Namensvorschlag nach 5.2: der Name der Kopie; ist er belegt, mit „(Projekt)", dann „(Projekt 2)" usw.
        /// Namen in <paramref name="schonVergeben"/> gelten als belegt.
        /// </summary>
        public static string Namensvorschlag(string name, ICollection<string> schonVergeben = null)
        {
            string basis = (name ?? "").Trim();
            bool Frei(string n) => !NameBelegt(n) && (schonVergeben == null || !schonVergeben.Contains(n));
            if (basis.Length > 0 && Frei(basis)) return basis;
            string mit = string.Format(MyResource.Resource.Culture, MyResource.Resource.KATRUECK_NAME_ZUSATZ, basis);
            if (Frei(mit)) return mit;
            for (int i = 2; i < 1000; i++)
            {
                string n = string.Format(MyResource.Resource.Culture, MyResource.Resource.KATRUECK_NAME_ZUSATZ_N, basis,
                                         i.ToString(CultureInfo.InvariantCulture));
                if (Frei(n)) return n;
            }
            return mit;
        }

        // ---------------------------------------------------------------- Vorschau ---

        /// <summary>
        /// Die Rückfrage zum Projektgebäude <paramref name="idProjektGebaeude"/> (<c>Z_ProjektGebaeude.ID</c>); <c>null</c>,
        /// wenn es keine Projektkopie gibt. Schreibt nichts.
        /// </summary>
        public static GebaeudeRueckwegVorschau Vorschau(int idProjektGebaeude)
        {
            DataRow k = Kopie(null, idProjektGebaeude);
            if (k == null) return null;
            int idKopie = Ganz(k["ID"]);
            string nameKopie = Text(k["Gebaeudename"]);
            (Rueckwegabsage sperre, int? idU, DataRow u) = Ursprung(null, k);

            var abweichungen = new List<GebaeudeAbweichung>();
            if (u != null)
                foreach (string s in Kopfspalten())
                {
                    if (!k.Table.Columns.Contains(s) || !u.Table.Columns.Contains(s)) continue;
                    string p = Text(k[s]), q = Text(u[s]);
                    if (!string.Equals(p, q, StringComparison.Ordinal)) abweichungen.Add(new GebaeudeAbweichung(s, q, p));
                }

            Dictionary<string, int> projekt = Kindzeilen(null, Zonenebene.Projekt, idKopie);
            Dictionary<string, int> ursprung = u != null ? Kindzeilen(null, Zonenebene.Katalog, idU.Value) : null;
            var kinder = projekt.Select(p => new GebaeudeKindzahl(p.Key, p.Value, ursprung != null ? ursprung[p.Key] : 0)).ToList();

            return new GebaeudeRueckwegVorschau(idProjektGebaeude, idKopie, nameKopie, idU, u != null ? Text(u["Bezeichner"]) : "",
                                                sperre, Namensvorschlag(nameKopie), abweichungen, kinder);
        }

        // ---------------------------------------------------------------- Schreiben ---

        /// <summary>
        /// <b>Der Rückweg</b> des Projektgebäudes <paramref name="idProjektGebaeude"/>: <paramref name="art"/>
        /// <see cref="Rueckwegart.Neu"/> legt unter <paramref name="name"/> an, <see cref="Rueckwegart.Ueberschreiben"/>
        /// ersetzt den Ursprung (der Name bleibt, <paramref name="name"/> zählt nicht). Alles oder nichts.
        /// </summary>
        public static Rueckwegergebnis Rueckweg(int idProjektGebaeude, Rueckwegart art, string name, DateTime? stichtag = null)
        {
            DataRow k = idProjektGebaeude > 0 ? Kopie(null, idProjektGebaeude) : null;
            if (k == null)
                return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.KeineKopie, idProjektGebaeude,
                                                  MyResource.Resource.GEB_MSG_DB_UEBERNAHME_KEIN_GEBAEUDE);
            return art == Rueckwegart.Neu ? Neu(idProjektGebaeude, k, name, stichtag) : Ueberschreiben(idProjektGebaeude, k);
        }

        private static Rueckwegergebnis Neu(int idZ, DataRow k, string name, DateTime? stichtag)
        {
            string n = (name ?? "").Trim();
            string nameKopie = Text(k["Gebaeudename"]);
            if (n.Length == 0)
                return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.NameLeer, idZ, F(MyResource.Resource.KATRUECK_ABSAGE_NAME_LEER, nameKopie));
            if (NameBelegt(n))
                return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.NameBelegt, idZ, F(MyResource.Resource.KATRUECK_ABSAGE_NAME_BELEGT, n));

            GebaeudeStammCtrl.ProjektuebernahmeErgebnis e = GebaeudeStammCtrl.AusProjektUebernehmen(idZ, n, stichtag, ursprungSetzen: true);
            if (!e.Ok)
                return Rueckwegergebnis.Abgelehnt(
                    e.Absage == GebaeudeStammCtrl.Projektuebernahmeabsage.NameVergeben ? Rueckwegabsage.NameBelegt
                    : e.Absage == GebaeudeStammCtrl.Projektuebernahmeabsage.NameLeer ? Rueckwegabsage.NameLeer
                    : e.Absage == GebaeudeStammCtrl.Projektuebernahmeabsage.KeinGebaeude ? Rueckwegabsage.KeineKopie
                    : Rueckwegabsage.Fehler, idZ, e.Meldung);
            int kinder = e.Zonen + e.Bauteile + (e.Befund == null ? 0 : e.Befund.Vorgaben + e.Befund.Kalender + e.Befund.Perioden);
            var satz = new Rueckwegsatz(idZ, e.Id, e.Name, true, 0, kinder);
            return new Rueckwegergebnis(true, Rueckwegabsage.Keine, 0, F(MyResource.Resource.KATRUECK_MSG_UEBERNOMMEN_1, e.Name),
                                        new[] { satz });
        }

        private static Rueckwegergebnis Ueberschreiben(int idZ, DataRow k)
        {
            int idKopie = Ganz(k["ID"]);
            string nameKopie = Text(k["Gebaeudename"]);
            int idProjekt = k["ID_Projekt"] == DBNull.Value ? 0 : Ganz(k["ID_Projekt"]);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                {
                    // Im Vorgang erneut: Ursprung und Schloss.
                    DataRow kv = Kopie(v, idZ);
                    (Rueckwegabsage sperre, int? idU, DataRow u) = Ursprung(v, kv);
                    if (sperre != Rueckwegabsage.Keine)
                    {
                        v.Rollback();
                        string vorlage = sperre == Rueckwegabsage.UrsprungGesperrt ? MyResource.Resource.KATRUECK_ABSAGE_GESPERRT
                            : sperre == Rueckwegabsage.UrsprungFehlt ? MyResource.Resource.KATRUECK_ABSAGE_URSPRUNG_FEHLT
                            : MyResource.Resource.KATRUECK_ABSAGE_URSPRUNG_UNBEKANNT;
                        return Rueckwegergebnis.Abgelehnt(sperre, idZ, F(vorlage, nameKopie));
                    }
                    int idKatalog = idU.Value;
                    string name = Text(u["Bezeichner"]);

                    // 1) Der Kopf: die Fachspalten und die Beschreibung, wörtlich; nie Name, ReadOnly, Katalog_*.
                    var katalog = new HashSet<string>(DataRepository.SpaltenVonTabelle(TAB), StringComparer.OrdinalIgnoreCase);
                    List<string> spalten = Kopfspalten().Where(s => katalog.Contains(s) && kv.Table.Columns.Contains(s)).ToList();
                    var ps = spalten.Select((s, i) => new DbParam("@s" + i.ToString(CultureInfo.InvariantCulture), kv[s])).ToList();
                    ps.Add(new DbParam("@id", idKatalog));
                    v.Ausfuehren("UPDATE \"" + TAB + "\" SET " + string.Join(", ", spalten.Select(s => "\"" + s + "\" = ?")) +
                                 " WHERE \"ID\" = ?", ps.ToArray());

                    // 2) Die Zonen samt Bauteilen, Luftströmen und Zonen-Konditionierung: ersetzen (die Fremdschlüssel kaskadieren).
                    Zonenkopie.Entfernen(v, Zonenebene.Katalog, idKatalog);
                    Zonenkopie.Befund zonen = Zonenkopie.Kopieren(v, Zonenebene.Projekt, idKopie, Zonenebene.Katalog, idKatalog, idProjekt);
                    if (!zonen.Ok)
                    {
                        v.Rollback();
                        return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.Fehler, idZ,
                                                          MyResource.Resource.KATRUECK_ABSAGE_FEHLER + " " + zonen.Meldung);
                    }

                    // 3) Die Konditionierung der Gebäudeebene: ersetzen.
                    Konditionierungskopie.Befund kond = KonditionierungSchema.Lesbar()
                        ? Konditionierungskopie.Kopieren(v, KonditionierungCtrl.Eigner.Gebaeude(idKopie),
                                                         KonditionierungCtrl.Eigner.Katalogbau(idKatalog),
                                                         Konditionierungskopie.Auswahl.Ersetzend)
                        : Konditionierungskopie.Befund.Nichts;
                    if (!kond.Ok)
                    {
                        v.Rollback();
                        return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.Fehler, idZ,
                                                          MyResource.Resource.KATRUECK_ABSAGE_FEHLER + " " + kond.Meldung);
                    }

                    v.Commit();
                    int kinder = zonen.Zonen + zonen.Bauteile + zonen.Luftstroeme + zonen.Konditionierung +
                                 kond.Vorgaben + kond.Kalender + kond.Perioden;
                    var satz = new Rueckwegsatz(idZ, idKatalog, name, false, 0, kinder);
                    return new Rueckwegergebnis(true, Rueckwegabsage.Keine, 0,
                                                F(MyResource.Resource.KATRUECK_MSG_UEBERNOMMEN_1, name), new[] { satz });
                }
            }
            catch (Exception ex)
            {
                // DbVorgang.Dispose rollt ohne Commit zurück.
                return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.Fehler, idZ, MyResource.Resource.KATRUECK_ABSAGE_FEHLER + " " + ex.Message);
            }
        }

        // ---------------------------------------------------------------- Hilfen ---

        /// <summary>Die Projektkopie einer Zuordnung samt aller Spalten; <c>null</c> = keine.</summary>
        private static DataRow Kopie(DbVorgang v, int idZ)
        {
            const string sql = "SELECT * FROM \"" + TAB_PROJ + "\" WHERE \"ID_ProjektGebaeude\" = ? ORDER BY \"ID\"";
            DataTable dt = v == null ? DataRepository.GetDataTable(sql, new DbParam("@z", idZ)) : v.Lese(sql, new DbParam("@z", idZ));
            return dt == null || dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        /// <summary>Der Ursprung der Kopie und ob er überschrieben werden kann.</summary>
        private static (Rueckwegabsage Sperre, int? Id, DataRow Zeile) Ursprung(DbVorgang v, DataRow k)
        {
            if (k == null || !k.Table.Columns.Contains(SPALTE_URSPRUNG) || k[SPALTE_URSPRUNG] == DBNull.Value)
                return (Rueckwegabsage.UrsprungUnbekannt, null, null);
            int id = Ganz(k[SPALTE_URSPRUNG]);
            const string sql = "SELECT * FROM \"" + TAB + "\" WHERE \"ID\" = ?";
            DataTable dt = v == null ? DataRepository.GetDataTable(sql, new DbParam("@id", id)) : v.Lese(sql, new DbParam("@id", id));
            if (dt == null || dt.Rows.Count == 0) return (Rueckwegabsage.UrsprungFehlt, id, null);
            DataRow u = dt.Rows[0];
            bool gesperrt = u["ReadOnly"] != DBNull.Value && Convert.ToInt64(u["ReadOnly"], CultureInfo.InvariantCulture) != 0;
            return (gesperrt ? Rueckwegabsage.UrsprungGesperrt : Rueckwegabsage.Keine, id, u);
        }

        /// <summary>
        /// Die Kindzeilen eines Gebäudes der Ebene <paramref name="e"/> je Art: Zonen, ihre Bauteile und Luftströme, die
        /// Kalender, Vorgabezeilen und Perioden der Konditionierung (Gebäude- und Zonenebene zusammen).
        /// </summary>
        internal static Dictionary<string, int> Kindzeilen(DbVorgang v, Zonenebene e, long idGebaeude)
        {
            var z = new Dictionary<string, int>
            {
                [ART_ZONEN] = 0, [ART_BAUTEILE] = 0, [ART_LUFTSTROEME] = 0, [ART_KALENDER] = 0, [ART_VORGABEN] = 0, [ART_PERIODEN] = 0,
            };
            bool katalog = e == Zonenebene.Katalog;
            string zoneTab = katalog ? "Tab_Zone_STAMM" : "Tab_Zone";
            string bauteilTab = katalog ? "Tab_Bauteil_STAMM" : "Tab_Bauteil";
            string luftTab = katalog ? "Tab_Zonenluftstrom_STAMM" : "Tab_Zonenluftstrom";
            string gSpalte = katalog ? "ID_Gebaeude_Stamm" : "ID_Gebaeude";
            string zSpalte = katalog ? "ID_Zone_Stamm" : "ID_Zone";
            var p = new DbParam("@g", idGebaeude);

            if (Zonenkopie.Lesbar(e))
            {
                string zonen = "SELECT \"ID\" FROM \"" + zoneTab + "\" WHERE \"ID_Gebaeude\" = ?";
                z[ART_ZONEN] = Anzahl(v, "SELECT COUNT(*) FROM \"" + zoneTab + "\" WHERE \"ID_Gebaeude\" = ?", p);
                z[ART_BAUTEILE] = Anzahl(v, "SELECT COUNT(*) FROM \"" + bauteilTab + "\" WHERE \"ID_Zone\" IN (" + zonen + ")", p);
                if (DataRepository.TabelleVorhanden(luftTab))
                    z[ART_LUFTSTROEME] = Anzahl(v, "SELECT COUNT(*) FROM \"" + luftTab + "\" WHERE \"ID_ZoneA\" IN (" + zonen + ")", p);
            }
            if (KonditionierungSchema.Lesbar())
            {
                bool mitZone = DataRepository.SpaltenVonTabelle(KonditionierungSchema.TAB_KALENDER)
                    .Contains(zSpalte, StringComparer.OrdinalIgnoreCase) && Zonenkopie.Lesbar(e);
                string eigner = "(\"" + gSpalte + "\" = ?" +
                                (mitZone ? " OR \"" + zSpalte + "\" IN (SELECT \"ID\" FROM \"" + zoneTab + "\" WHERE \"ID_Gebaeude\" = ?)" : "") + ")";
                DbParam[] pe = mitZone ? new[] { new DbParam("@g", idGebaeude), new DbParam("@g2", idGebaeude) } : new[] { p };
                z[ART_KALENDER] = Anzahl(v, "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " + eigner, pe);
                z[ART_VORGABEN] = Anzahl(v, "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE " + eigner, pe);
                z[ART_PERIODEN] = Anzahl(v, "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" IN (" +
                                            "SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " + eigner + ")", pe);
            }
            return z;
        }

        private static int Anzahl(DbVorgang v, string sql, params DbParam[] p)
        {
            object o = v == null ? DataRepository.ExecuteScalar(sql, p) : v.Skalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static int Ganz(object o) => Convert.ToInt32(o, CultureInfo.InvariantCulture);

        private static string Text(object o)
            => o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);

        private static string F(string vorlage, params object[] werte) => string.Format(MyResource.Resource.Culture, vorlage, werte);
    }
}
