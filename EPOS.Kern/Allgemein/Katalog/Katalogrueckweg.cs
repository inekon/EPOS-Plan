using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Was der Anwender je Satz des Rückwegs wählt.</summary>
    public enum Rueckwegart
    {
        /// <summary>Als neuen Katalogsatz anlegen (Vorgabe).</summary>
        Neu,
        /// <summary>Den Ursprung der Projektkopie überschreiben.</summary>
        Ueberschreiben,
    }

    /// <summary>Warum der Rückweg nichts geschrieben hat — oder warum „Ursprung überschreiben" ausgegraut ist.</summary>
    public enum Rueckwegabsage
    {
        Keine,
        /// <summary>Die Projektkopie gibt es nicht.</summary>
        KeineKopie,
        /// <summary>Der Name des neuen Satzes ist leer.</summary>
        NameLeer,
        /// <summary>Der Name ist im Katalog oder im selben Aufruf schon vergeben.</summary>
        NameBelegt,
        /// <summary>Der Ursprung ist gesperrt („Erst Schloss aufheben").</summary>
        UrsprungGesperrt,
        /// <summary>Der Verweis zeigt ins Leere („Ursprung nicht mehr vorhanden").</summary>
        UrsprungFehlt,
        /// <summary>Die Kopie hat keinen Verweis („Ursprung nicht bekannt").</summary>
        UrsprungUnbekannt,
        /// <summary>Die Kopie verstößt gegen die Prüfregel des Katalogs.</summary>
        Pruefverstoss,
        /// <summary>Datenbankfehler.</summary>
        Fehler,
    }

    /// <summary>
    /// <b>Das Gewerk eines Rückwegs</b>: Projektkopie, Katalog, Verweis der Anlagenzeile, Kostenkomponente,
    /// Kindtabellen der technischen Daten und die Prüfregel des Katalogs.
    /// </summary>
    public sealed class Rueckweggewerk
    {
        public required string Kopietabelle { get; init; }
        public required string Katalogtabelle { get; init; }
        /// <summary>Spalte von <c>Tab_Energieanlagen</c>, die auf die Kopie zeigt (<c>ID_Kessel</c> …).</summary>
        public required string Anlagenverweis { get; init; }
        /// <summary><c>Tab_KostenKomponente.ID</c> des Gewerks.</summary>
        public required int KomponentenId { get; init; }
        /// <summary>
        /// Die technischen Kindtabellen (Konzept 7 Nr. 5): Kind der Kopie, Kind des Katalogs, Verweisspalte. Sie
        /// gehen vollständig mit — bei „neu" als Kopie am neuen Satz, bei „überschreiben" als Ersatz.
        /// </summary>
        public IReadOnlyList<(string KindKopie, string KindKatalog, string Verweis)> Kinder { get; init; } =
            Array.Empty<(string, string, string)>();
        /// <summary>Prüfregel einer Zeile der Kopie; leer = in Ordnung. Läuft vor dem Vorgang.</summary>
        public Func<DataRow, string> Pruefung { get; init; }
    }

    /// <summary>Eine Zeile der Rückfrage: die Kopie, ihr Ursprung, ob er überschrieben werden kann, der Namensvorschlag.</summary>
    public sealed record Rueckwegzeile(int IdKopie, string NameKopie, int? IdUrsprung, string NameUrsprung,
                                       Rueckwegabsage Ueberschreiben, string Namensvorschlag);

    /// <summary>Ein Auftrag des Rückwegs: welche Kopie, welche Wahl, welcher Name (nur bei „neu").</summary>
    public sealed record Rueckwegauftrag(int IdKopie, Rueckwegart Art, string Name);

    /// <summary>Ein geschriebener Katalogsatz.</summary>
    public sealed record Rueckwegsatz(int IdKopie, int IdKatalog, string Name, bool Neu, int Kostenpositionen, int Kindzeilen);

    /// <summary>Das Ergebnis eines Rückwegs: alles oder nichts.</summary>
    public sealed record Rueckwegergebnis(bool Ok, Rueckwegabsage Absage, int IdKopie, string Meldung,
                                          IReadOnlyList<Rueckwegsatz> Saetze)
    {
        public static Rueckwegergebnis Abgelehnt(Rueckwegabsage a, int idKopie, string meldung)
            => new Rueckwegergebnis(false, a, idKopie, meldung, Array.Empty<Rueckwegsatz>());
    }

    /// <summary>
    /// <b>Rückweg Projekt → Datenbank</b> (Konzept Projektdialoge mit Katalogauswahl 5.2, KA‑E‑9): eine oder mehrere
    /// Projektkopien als neuer Katalogsatz oder als Ersatz ihres Ursprungs, in EINEM Vorgang je Aufruf.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>Was mitgeht:</b> die Schnittmenge der Spalten von Kopie und Katalog ohne <c>ID</c>, <c>ID_Projekt</c>,
    /// <c>ID_Stamm</c>, <c>ReadOnly</c>, <c>ID_KostenVorlage</c> und <c>Katalog_*</c> (der Name gesondert); die Kindtabellen
    /// des Gewerks; die Planwertspalten als Teil der Schnittmenge; die Betriebskostenpositionen der ersten Anlage, die auf
    /// die Kopie zeigt, als Kostenvorlage des Satzes (<see cref="KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE"/>).</item>
    /// <item><b>Katalogpaket:</b> Beim Überschreiben bleiben <c>Katalog_Schluessel</c>, <c>Katalog_Pruefsumme</c> und
    /// <c>Katalog_Ausgelaufen</c> stehen — die Katalogaktualisierung erkennt den Satz an der abweichenden Prüfsumme als vom
    /// Anwender geändert.</item>
    /// <item><b>Danach:</b> Ein neuer Satz ist ungesperrt, die Kopie bekommt ihn als Ursprung.</item>
    /// </list>
    /// </remarks>
    public static class Katalogrueckweg
    {
        /// <summary>Die Spalten, die nie mitgehen (außer <c>Katalog_*</c>).</summary>
        public static readonly IReadOnlyList<string> NIE_MIT = new[]
        {
            "ID", "ID_Projekt", KatalogkostenUrsprungSchema.SPALTE_ID_STAMM, "ReadOnly",
            KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE, "Bezeichner",
        };

        /// <summary>Höchstlänge eines Vorlagennamens (<c>Tab_KostenVorlage.Name</c>).</summary>
        public const int NAME_VORLAGE_MAX = 100;

        /// <summary>Die Spalten, die von der Kopie in den Katalog gehen.</summary>
        public static IReadOnlyList<string> Spalten(Rueckweggewerk g)
        {
            var katalog = new HashSet<string>(DataRepository.SpaltenVonTabelle(g.Katalogtabelle), StringComparer.OrdinalIgnoreCase);
            return DataRepository.SpaltenVonTabelle(g.Kopietabelle)
                .Where(s => katalog.Contains(s) && !IstAusgenommen(s)).ToList();
        }

        private static bool IstAusgenommen(string s)
            => NIE_MIT.Contains(s, StringComparer.OrdinalIgnoreCase) || s.StartsWith("Katalog_", StringComparison.OrdinalIgnoreCase);

        // ---------------------------------------------------------------- Rückfrage ---

        /// <summary>Ist <paramref name="name"/> im Katalog des Gewerks vergeben?</summary>
        public static bool NameBelegt(Rueckweggewerk g, string name)
        {
            object n = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + g.Katalogtabelle + "\" WHERE \"Bezeichner\" = ?",
                                                    new DbParam("@bez", (name ?? "").Trim()));
            return n != null && n != DBNull.Value && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// Der Namensvorschlag: der Name der Kopie; ist er belegt, mit „(Projekt)", dann „(Projekt 2)" usw. Namen in
        /// <paramref name="schonVergeben"/> (Vorschläge derselben Rückfrage) gelten als belegt.
        /// </summary>
        public static string Namensvorschlag(Rueckweggewerk g, string name, ICollection<string> schonVergeben = null)
        {
            string basis = (name ?? "").Trim();
            bool Frei(string n) => !NameBelegt(g, n) && (schonVergeben == null || !schonVergeben.Contains(n));
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

        /// <summary>
        /// Die Zeilen der Rückfrage zu den Kopien <paramref name="idsKopie"/> (je ID einmal, Reihenfolge erhalten). Eine
        /// fehlende Kopie fehlt in der Liste.
        /// </summary>
        public static IReadOnlyList<Rueckwegzeile> Vorschau(Rueckweggewerk g, IReadOnlyList<int> idsKopie)
        {
            var zeilen = new List<Rueckwegzeile>();
            var vergeben = new HashSet<string>(StringComparer.Ordinal);
            foreach (int id in (idsKopie ?? Array.Empty<int>()).Distinct())
            {
                DataRow r = Zeile(g.Kopietabelle, id);
                if (r == null) continue;
                string name = Text(r, "Bezeichner");
                (Rueckwegabsage sperre, int? idUrsprung, string nameUrsprung) = Ursprung(g, r, null);
                string vorschlag = Namensvorschlag(g, name, vergeben);
                vergeben.Add(vorschlag);
                zeilen.Add(new Rueckwegzeile(id, name, idUrsprung, nameUrsprung, sperre, vorschlag));
            }
            return zeilen;
        }

        /// <summary>Ursprung der Kopie und ob er überschrieben werden kann.</summary>
        private static (Rueckwegabsage Sperre, int? Id, string Name) Ursprung(Rueckweggewerk g, DataRow kopie, DbVorgang v)
        {
            if (!kopie.Table.Columns.Contains(KatalogkostenUrsprungSchema.SPALTE_ID_STAMM) ||
                kopie[KatalogkostenUrsprungSchema.SPALTE_ID_STAMM] == DBNull.Value)
                return (Rueckwegabsage.UrsprungUnbekannt, null, "");
            int id = Convert.ToInt32(kopie[KatalogkostenUrsprungSchema.SPALTE_ID_STAMM], CultureInfo.InvariantCulture);
            DataRow u = v == null ? Zeile(g.Katalogtabelle, id) : Zeile(v, g.Katalogtabelle, id);
            if (u == null) return (Rueckwegabsage.UrsprungFehlt, id, "");
            bool gesperrt = u.Table.Columns.Contains("ReadOnly") && u["ReadOnly"] != DBNull.Value &&
                            Convert.ToInt64(u["ReadOnly"], CultureInfo.InvariantCulture) != 0;
            return (gesperrt ? Rueckwegabsage.UrsprungGesperrt : Rueckwegabsage.Keine, id, Text(u, "Bezeichner"));
        }

        // ---------------------------------------------------------------- Schreiben ---

        /// <summary>
        /// <b>Schreibt alle Aufträge in EINEM Vorgang</b> — scheitert einer, wird nichts geschrieben, und die Absage nennt
        /// den Satz. Benannte Absagen: <see cref="Rueckwegabsage"/>.
        /// </summary>
        public static Rueckwegergebnis Uebernehmen(Rueckweggewerk g, IReadOnlyList<Rueckwegauftrag> auftraege,
                                                   DateTime? stichtag = null)
        {
            if (auftraege == null || auftraege.Count == 0)
                return new Rueckwegergebnis(true, Rueckwegabsage.Keine, 0, "", Array.Empty<Rueckwegsatz>());

            // 1. VOR dem Vorgang: Kopien lesen, Prüfregel, Namen (auch untereinander).
            var kopien = new Dictionary<int, DataRow>();
            var namen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Rueckwegauftrag a in auftraege)
            {
                DataRow r = a == null ? null : Zeile(g.Kopietabelle, a.IdKopie);
                if (r == null)
                    return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.KeineKopie, a?.IdKopie ?? 0,
                                                      F(MyResource.Resource.KATRUECK_ABSAGE_KEINE_KOPIE, a?.IdKopie ?? 0));
                kopien[a.IdKopie] = r;
                string nameKopie = Text(r, "Bezeichner");
                string grund = g.Pruefung?.Invoke(r);
                if (!string.IsNullOrEmpty(grund))
                    return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.Pruefverstoss, a.IdKopie,
                                                      F(MyResource.Resource.KATRUECK_ABSAGE_VERSTOSS, nameKopie, grund));
                if (a.Art == Rueckwegart.Neu)
                {
                    string n = (a.Name ?? "").Trim();
                    if (n.Length == 0)
                        return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.NameLeer, a.IdKopie,
                                                          F(MyResource.Resource.KATRUECK_ABSAGE_NAME_LEER, nameKopie));
                    if (!namen.Add(n) || NameBelegt(g, n))
                        return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.NameBelegt, a.IdKopie,
                                                          F(MyResource.Resource.KATRUECK_ABSAGE_NAME_BELEGT, n));
                }
            }

            IReadOnlyList<string> spalten = Spalten(g);
            bool ursprungSpalte = KatalogkostenUrsprungSchema.UrsprungLesbar(g.Kopietabelle);
            bool vorlageSpalte = KatalogkostenUrsprungSchema.KostenvorlageLesbar(g.Katalogtabelle);
            string heute = (stichtag ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var saetze = new List<Rueckwegsatz>();

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    foreach (Rueckwegauftrag a in auftraege)
                    {
                        DataRow r = kopien[a.IdKopie];
                        string nameKopie = Text(r, "Bezeichner");
                        int idKatalog;
                        string name;
                        int? alteVorlage = null;
                        if (a.Art == Rueckwegart.Ueberschreiben)
                        {
                            (Rueckwegabsage sperre, int? idU, string nameU) = Ursprung(g, r, v);
                            if (sperre != Rueckwegabsage.Keine)
                            {
                                v.Rollback();
                                string vorlage = sperre == Rueckwegabsage.UrsprungGesperrt ? MyResource.Resource.KATRUECK_ABSAGE_GESPERRT
                                    : sperre == Rueckwegabsage.UrsprungFehlt ? MyResource.Resource.KATRUECK_ABSAGE_URSPRUNG_FEHLT
                                    : MyResource.Resource.KATRUECK_ABSAGE_URSPRUNG_UNBEKANNT;
                                return Rueckwegergebnis.Abgelehnt(sperre, a.IdKopie, F(vorlage, nameKopie));
                            }
                            idKatalog = idU.Value;
                            name = nameU;
                            if (vorlageSpalte)
                            {
                                object o = v.Skalar("SELECT \"" + KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE + "\" FROM \"" +
                                                    g.Katalogtabelle + "\" WHERE \"ID\" = ?", new DbParam("@id", idKatalog));
                                if (o != null && o != DBNull.Value) alteVorlage = Convert.ToInt32(o, CultureInfo.InvariantCulture);
                            }
                            var ps = spalten.Select((s, i) => new DbParam("@s" + i, Wert(r, s))).ToList();
                            ps.Add(new DbParam("@id", idKatalog));
                            v.Ausfuehren("UPDATE \"" + g.Katalogtabelle + "\" SET " +
                                         string.Join(", ", spalten.Select(s => "\"" + s + "\" = ?")) + " WHERE \"ID\" = ?",
                                         ps.ToArray());
                        }
                        else
                        {
                            name = a.Name.Trim();
                            object belegt = v.Skalar("SELECT COUNT(*) FROM \"" + g.Katalogtabelle + "\" WHERE \"Bezeichner\" = ?",
                                                     new DbParam("@bez", name));
                            if (Convert.ToInt64(belegt, CultureInfo.InvariantCulture) > 0)
                            {
                                v.Rollback();
                                return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.NameBelegt, a.IdKopie,
                                                                  F(MyResource.Resource.KATRUECK_ABSAGE_NAME_BELEGT, name));
                            }
                            var ps = new List<DbParam> { new DbParam("@bez", name) };
                            ps.AddRange(spalten.Select((s, i) => new DbParam("@s" + i, Wert(r, s))));
                            idKatalog = v.EinfuegenUndId(
                                "INSERT INTO \"" + g.Katalogtabelle + "\" (\"Bezeichner\", \"ReadOnly\"" +
                                string.Concat(spalten.Select(s => ", \"" + s + "\"")) + ") VALUES (?, 0" +
                                string.Concat(spalten.Select(_ => ", ?")) + ")", ps.ToArray());
                            if (ursprungSpalte)
                                v.Ausfuehren("UPDATE \"" + g.Kopietabelle + "\" SET \"" + KatalogkostenUrsprungSchema.SPALTE_ID_STAMM +
                                             "\" = ? WHERE \"ID\" = ?", new DbParam("@u", idKatalog), new DbParam("@id", a.IdKopie));
                        }

                        int kinder = KinderKopieren(v, g, a.IdKopie, idKatalog, a.Art == Rueckwegart.Ueberschreiben);
                        int positionen = vorlageSpalte
                            ? KostenvorlageSchreiben(v, g, r, idKatalog, name, alteVorlage, heute)
                            : 0;
                        saetze.Add(new Rueckwegsatz(a.IdKopie, idKatalog, name, a.Art == Rueckwegart.Neu, positionen, kinder));
                    }
                    v.Commit();
                }
            }
            catch (Exception ex)
            {
                // DbVorgang.Dispose rollt ohne Commit zurück.
                return Rueckwegergebnis.Abgelehnt(Rueckwegabsage.Fehler, 0,
                                                  MyResource.Resource.KATRUECK_ABSAGE_FEHLER + " " + ex.Message);
            }

            string meldung = saetze.Count == 1
                ? F(MyResource.Resource.KATRUECK_MSG_UEBERNOMMEN_1, saetze[0].Name)
                : F(MyResource.Resource.KATRUECK_MSG_UEBERNOMMEN, saetze.Count,
                    string.Join(", ", saetze.Select(s => "„" + s.Name + "“")));
            return new Rueckwegergebnis(true, Rueckwegabsage.Keine, 0, meldung, saetze);
        }

        /// <summary>Kindzeilen der Kopie an den Katalogsatz — beim Überschreiben ersetzen sie die des Ursprungs.</summary>
        private static int KinderKopieren(DbVorgang v, Rueckweggewerk g, int idKopie, int idKatalog, bool ersetzen)
        {
            int n = 0;
            foreach ((string kindKopie, string kindKatalog, string verweis) in g.Kinder)
            {
                var ziel = new HashSet<string>(DataRepository.SpaltenVonTabelle(kindKatalog), StringComparer.OrdinalIgnoreCase);
                List<string> sp = DataRepository.SpaltenVonTabelle(kindKopie)
                    .Where(s => ziel.Contains(s) && !string.Equals(s, "ID", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(s, "ID_Projekt", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(s, verweis, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (ersetzen)
                    v.Ausfuehren("DELETE FROM \"" + kindKatalog + "\" WHERE \"" + verweis + "\" = ?", new DbParam("@k", idKatalog));
                string liste = string.Concat(sp.Select(s => ", \"" + s + "\""));
                n += v.Ausfuehren("INSERT INTO \"" + kindKatalog + "\" (\"" + verweis + "\"" + liste + ") SELECT ?" + liste +
                                  " FROM \"" + kindKopie + "\" WHERE \"" + verweis + "\" = ? ORDER BY \"ID\"",
                                  new DbParam("@k", idKatalog), new DbParam("@q", idKopie));
            }
            return n;
        }

        /// <summary>
        /// Die Betriebskostenpositionen der ersten Anlage, die auf die Kopie zeigt, als Kostenvorlage des Satzes (Gewerk =
        /// <see cref="Rueckweggewerk.KomponentenId"/>, Name = Satzname, nicht Standard). Eine eigene, ungesperrte Vorlage
        /// des Ursprungs wird beim Überschreiben ersetzt; ohne Positionen bleibt der Verweis, wie er ist.
        /// </summary>
        private static int KostenvorlageSchreiben(DbVorgang v, Rueckweggewerk g, DataRow kopie, int idKatalog, string name,
                                                 int? alteVorlage, string heute)
        {
            int idProjekt = Convert.ToInt32(kopie["ID_Projekt"], CultureInfo.InvariantCulture);
            object anlage = v.Skalar("SELECT MIN(\"ID\") FROM \"Tab_Energieanlagen\" WHERE \"ID_Projekt\" = ? AND \"" +
                                     g.Anlagenverweis + "\" = ?", new DbParam("@p", idProjekt),
                                     new DbParam("@k", Convert.ToInt32(kopie["ID"], CultureInfo.InvariantCulture)));
            if (anlage == null || anlage == DBNull.Value) return 0;
            DataTable pos = v.Lese(
                "SELECT w.*, f.\"Bezeichnung\" AS \"Lexikon\" FROM \"" + SchemaKatalog.TAB_PROJEKTWERTE + "\" w " +
                "LEFT JOIN \"Tab_Kostenfaktor\" f ON f.\"StammID\" = w.\"StammID\" " +
                "WHERE w.\"ProjektID\" = ? AND w.\"ID_Anlage\" = ? AND w.\"KomponentenID\" = ? AND w.\"KategorieID\" = ? " +
                "ORDER BY w.\"ID\"",
                new DbParam("@p", idProjekt), new DbParam("@a", Convert.ToInt32(anlage, CultureInfo.InvariantCulture)),
                new DbParam("@c", g.KomponentenId), new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_BETRIEB));
            if (pos == null || pos.Rows.Count == 0) return 0;

            string vorlagenname = name.Length > NAME_VORLAGE_MAX ? name.Substring(0, NAME_VORLAGE_MAX) : name;
            string bemerkung = F(MyResource.Resource.KATRUECK_BEMERKUNG_VORLAGE, Projektname(v, idProjekt), heute);
            int idVorlage = 0;
            if (alteVorlage.HasValue)
            {
                DataTable alt = v.Lese("SELECT \"IstStandard\", \"ReadOnly\" FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGE +
                                       "\" WHERE \"ID\" = ?", new DbParam("@id", alteVorlage.Value));
                if (alt.Rows.Count == 1 && Convert.ToInt64(alt.Rows[0][0], CultureInfo.InvariantCulture) == 0 &&
                    Convert.ToInt64(alt.Rows[0][1], CultureInfo.InvariantCulture) == 0)
                {
                    idVorlage = alteVorlage.Value;
                    v.Ausfuehren("DELETE FROM \"Tab_KostenVorlagePosition\" WHERE \"VorlageID\" = ?", new DbParam("@id", idVorlage));
                    v.Ausfuehren("UPDATE \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\" SET \"Name\" = ?, \"Bemerkung\" = ?, " +
                                 "\"GeaendertAm\" = ? WHERE \"ID\" = ?", new DbParam("@n", vorlagenname),
                                 new DbParam("@b", bemerkung), new DbParam("@g", heute), new DbParam("@id", idVorlage));
                }
            }
            if (idVorlage == 0)
                idVorlage = v.EinfuegenUndId(
                    "INSERT INTO \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\" (\"KomponentenID\", \"KategorieID\", \"Name\", " +
                    "\"IstStandard\", \"ReadOnly\", \"Bemerkung\", \"GeaendertAm\") VALUES (?, ?, ?, 0, 0, ?, ?)",
                    new[]
                    {
                        new DbParam("@c", g.KomponentenId), new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                        new DbParam("@n", vorlagenname), new DbParam("@b", bemerkung), new DbParam("@g", heute),
                    });

            var zielspalten = new HashSet<string>(DataRepository.SpaltenVonTabelle("Tab_KostenVorlagePosition"),
                                                  StringComparer.OrdinalIgnoreCase);
            int sortierung = 0;
            foreach (DataRow p in pos.Rows)
            {
                string bemessung = Text(p, "Bemessung");
                BemessungKatalog.Info info = BemessungKatalog.Finde(bemessung.Length == 0 ? DbWerte.BEMESSUNG_BETRAG : bemessung);
                bool absolut = info == null || info.Absolut;
                var werte = new List<(string Spalte, object Wert)>
                {
                    ("VorlageID", idVorlage),
                    ("StammID", Wert(p, "StammID")),
                    ("Bezeichnung", Wert(p, "Lexikon")),
                    ("Kostenart", Wert(p, "Kostenart")),
                    ("Bemessung", Wert(p, "Bemessung")),
                    ("Satz", absolut ? Wert(p, "EingegebenerWert") : Wert(p, "Einheitpreis")),
                    ("IstErloes", Wert(p, "IstErloes")),
                    ("Nutzungsdauer", Wert(p, "Nutzungsdauer")),
                    ("Sortierung", ++sortierung),
                    ("IstPflicht", Wert(p, "IstPflicht")),
                    ("NutzungsdauerID", Wert(p, "NutzungsdauerID")),
                    ("ErsatzFuehren", Wert(p, "ErsatzFuehren")),
                    ("RestwertAnsetzen", Wert(p, "RestwertAnsetzen")),
                    ("Wiederholperiode_a", Wert(p, "Wiederholperiode_a")),
                };
                werte = werte.Where(w => zielspalten.Contains(w.Spalte)).ToList();
                v.Ausfuehren("INSERT INTO \"Tab_KostenVorlagePosition\" (" + string.Join(", ", werte.Select(w => "\"" + w.Spalte + "\"")) +
                             ") VALUES (" + string.Join(", ", werte.Select(_ => "?")) + ")",
                             werte.Select((w, i) => new DbParam("@w" + i, w.Wert ?? DBNull.Value)).ToArray());
            }
            v.Ausfuehren("UPDATE \"" + g.Katalogtabelle + "\" SET \"" + KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE +
                         "\" = ? WHERE \"ID\" = ?", new DbParam("@v", idVorlage), new DbParam("@id", idKatalog));
            return pos.Rows.Count;
        }

        // ------------------------------------------------- Vorrang bei der Übernahme ---

        /// <summary>
        /// Die Gewerke, deren Anlagen eine Satzvorlage tragen können: <c>Tab_Energieanlagen.ID_Type</c> → Verweis der
        /// Anlage, Projektkopie, Katalog.
        /// </summary>
        private static readonly IReadOnlyDictionary<int, (string Verweis, string Kopietabelle, string Katalogtabelle)> ANLAGEN =
            new Dictionary<int, (string, string, string)>
            {
                { WizardItemClass.WP_TYP, ("ID_WP", "Tab_WP", "Tab_WP_STAMM") },
                { WizardItemClass.SOLAR_TYP, ("ID_Solar", "Tab_Solarkollektoren", "Tab_Solarkollektoren_STAMM") },
                { WizardItemClass.PV_TYP, ("ID_PV", "Tab_PV", "Tab_PV_STAMM") },
                { WizardItemClass.SP_TYP, ("ID_SP", "Tab_Stromspeicher", "Tab_Stromspeicher_STAMM") },
                { WizardItemClass.KESSEL_TYP, ("ID_Kessel", "Tab_Heizkessel", "Tab_Heizkessel_STAMM") },
                { WizardItemClass.BHKW_TYP, ("ID_BHKW", "Tab_BHKW", "Tab_BHKW_STAMM") },
                { WizardItemClass.PUFFER_TYP, ("ID_PUFFER", "Tab_Pufferspeicher", "Tab_Pufferspeicher_STAMM") },
            };

        /// <summary>
        /// Die Kostenvorlage des Katalogsatzes, aus dem das Gerät der Anlage <paramref name="idAnlage"/> stammt — sie geht
        /// bei der Übernahme Katalog → Projekt der Standardvorlage des Gewerks vor. <c>null</c>, wenn die Anlage keinen
        /// Ursprung kennt, der Satz keine eigene Vorlage führt oder eine Spalte fehlt.
        /// </summary>
        public static KostenVorlageKopf SatzvorlageDerAnlage(int idAnlage, int idType, int komponentenId)
        {
            if (!ANLAGEN.TryGetValue(idType, out var a)) return null;
            if (!KatalogkostenUrsprungSchema.UrsprungLesbar(a.Kopietabelle) || !KatalogkostenUrsprungSchema.KostenvorlageLesbar(a.Katalogtabelle))
                return null;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT v.\"ID\", v.\"Name\", v.\"KategorieID\" FROM \"Tab_Energieanlagen\" e " +
                "JOIN \"" + a.Kopietabelle + "\" k ON k.\"ID\" = e.\"" + a.Verweis + "\" " +
                "JOIN \"" + a.Katalogtabelle + "\" s ON s.\"ID\" = k.\"ID_Stamm\" " +
                "JOIN \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\" v ON v.\"ID\" = s.\"ID_KostenVorlage\" " +
                "WHERE e.\"ID\" = ? AND v.\"KomponentenID\" = ?",
                new DbParam("@a", idAnlage), new DbParam("@c", komponentenId));
            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            return new KostenVorlageKopf
            {
                Id = Convert.ToInt32(r[0], CultureInfo.InvariantCulture),
                Name = Convert.ToString(r[1], CultureInfo.InvariantCulture),
                KomponentenId = komponentenId,
                KategorieId = r[2] == DBNull.Value ? DbWerte.KOSTEN_KATEGORIE_BETRIEB : Convert.ToInt32(r[2], CultureInfo.InvariantCulture),
                IstStandard = false,
            };
        }

        // ---------------------------------------------------------------- Hilfen ---

        private static DataRow Zeile(string tabelle, int id)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            return dt == null || dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        private static DataRow Zeile(DbVorgang v, string tabelle, int id)
        {
            DataTable dt = v.Lese("SELECT * FROM \"" + tabelle + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            return dt == null || dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        private static string Projektname(DbVorgang v, int idProjekt)
        {
            object o = v.Skalar("SELECT \"Projektname\" FROM \"Tab_Projekt\" WHERE \"ID\" = ?", new DbParam("@p", idProjekt));
            return o == null || o == DBNull.Value ? idProjekt.ToString(CultureInfo.InvariantCulture) : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static object Wert(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) ? r[spalte] : DBNull.Value;

        private static string Text(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture) ?? "" : "";

        private static string F(string vorlage, params object[] a) => string.Format(MyResource.Resource.Culture, vorlage, a);
    }
}
