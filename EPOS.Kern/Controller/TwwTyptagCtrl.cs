using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Stand der eingespielten Typtage: Zahl der Zeilen, Klimazonen, Gebäudearten,
    /// Typtagcodes, Quelle, Ausgabe und Tag des Einspielens. <see cref="Vorhanden"/> ist
    /// <c>false</c>, solange nichts eingespielt ist — dann ist der Typtagweg benannt nicht
    /// verfügbar (Konzept 4.2, Stufe Z4b).
    /// </summary>
    internal sealed record TwwTyptagstand(bool Vorhanden, int Zeilen, IReadOnlyList<int> Klimazonen,
                                          IReadOnlyList<string> Gebaeudearten, IReadOnlyList<string> Typtage,
                                          bool MitTagesgaengen, string Quelle, string Ausgabe, string DatumImport);

    /// <summary>
    /// Was ein Einspielen der Typtage zurückgibt: Zahl der geschriebenen Zeilen, der Stand
    /// danach, die benannten Hinweise des Lesers und — wenn das Paket nicht taugt — der
    /// <see cref="Abbruch"/>. Bei einem Abbruch ist <b>nichts</b> geändert.
    /// </summary>
    internal sealed class TwwTyptagimportBericht
    {
        /// <summary>Die Zahl der geschriebenen Zeilen; 0 bei einem Abbruch.</summary>
        internal int Zeilen { get; set; }

        /// <summary>Die Zahl der Zeilen, die das Einspielen ersetzt hat (der frühere Stand).</summary>
        internal int Ersetzt { get; set; }

        /// <summary>Der Stand nach dem Einspielen; bei einem Abbruch der unveränderte Stand.</summary>
        internal TwwTyptagstand Stand { get; set; }

        /// <summary>Benannte Hinweise des Lesers und des Schreibwegs — nie still.</summary>
        internal List<ZapfSatz> Hinweise { get; } = new List<ZapfSatz>();

        /// <summary>Der Grund, aus dem das Paket abgelehnt ist; <c>null</c> = eingespielt.</summary>
        internal ZapfSatz Abbruch { get; set; }
    }

    /// <summary>
    /// <b>Der Schreibweg der eingespielten Typtage</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 2.5, 3.1, 3.3; Schemaschritt T3 „Typtage", Stufe Z4b): Er spielt ein Paket des
    /// <b>lizenzierten Anwenders</b> in <c>Tab_TwwTyptag_IMPORT</c> ein, löscht es wieder, nennt
    /// den Stand und liest die Zeilen für den Rechenweg zurück.
    ///
    /// <para><b>Ein Paket ersetzt das andere.</b> Es gibt nur EINEN eingespielten Satz je
    /// Datenbank: Das Einspielen leert die Tabelle und schreibt neu — beides in EINEM Vorgang.
    /// Geht dabei etwas schief, rollt der Vorgang zurück, und der frühere Satz steht unverändert
    /// da (kein halber Stand).</para>
    ///
    /// <para><b>Anwenderlokal.</b> Die Zeilen tragen keinen <c>Status</c> und kein
    /// <c>ReadOnly</c>; sie wandern nicht in eine Projektkopie und nicht in ein
    /// <c>.wpx</c>-Paket, und <c>Werkzeuge/Auslieferungsvorlage</c> leert die Tabelle (Konzept
    /// 3.2, Kapitel 6). Das Repositorium, die Testdatenbank und die CI führen keine Zeile.</para>
    ///
    /// <para><b>Kein SQL-Text aus Daten.</b> Jeder Zugriff läuft über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern; die Tabellen- und Spaltennamen
    /// stehen als Konstanten in <see cref="TwwSchema"/>.</para>
    /// </summary>
    internal static class TwwTyptagCtrl
    {
        /// <summary>Die Spalten der Tabelle in Schreibreihenfolge.</summary>
        private const string SPALTEN = "\"Art\", \"Klimazone\", \"Gebaeudeart\", \"Typtag\", \"Aufloesung_min\", " +
                                       "\"Zeilenindex\", \"Wert\", \"Quelle\", \"Ausgabe\", \"Datum_Import\"";

        /// <summary>Führt die Datenbank die Tabelle der eingespielten Typtage (Stand ab Schritt 125)?</summary>
        internal static bool TabelleVorhanden() => DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TYPTAG_IMPORT);

        /// <summary>
        /// <b>Prüfnaht des Schreibvorgangs</b> (Muster der Test-Naht in <c>SchemaMigration</c>):
        /// Sie läuft INNERHALB der Transaktion, nachdem die alten Zeilen entfernt und die
        /// Kategorien geschrieben sind. Vorbelegt mit einer folgenlosen Handlung; allein die
        /// Probe des Rollbacks belegt sie und setzt sie danach zurück. Kein Aufrufer des
        /// Programms fasst sie an.
        /// </summary>
        internal static Action Pruefnaht = () => { };

        // =================================================================================
        //  Stand
        // =================================================================================

        /// <summary>
        /// <b>Der Stand der eingespielten Typtage.</b> Ohne Tabelle (Stand vor 125) und ohne Zeile
        /// ist <see cref="TwwTyptagstand.Vorhanden"/> <c>false</c>.
        /// </summary>
        internal static TwwTyptagstand Stand()
        {
            var leer = new TwwTyptagstand(false, 0, new int[0], new string[0], new string[0], false, "", "", "");
            if (!TabelleVorhanden()) return leer;

            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Art\", \"Klimazone\", \"Gebaeudeart\", \"Typtag\", \"Quelle\", \"Ausgabe\", \"Datum_Import\" " +
                "FROM \"" + TwwSchema.TAB_TWW_TYPTAG_IMPORT + "\"");
            if (t == null || t.Rows.Count == 0) return leer;

            var zonen = new SortedSet<int>();
            var arten = new SortedSet<string>(StringComparer.Ordinal);
            var typtage = new SortedSet<string>(StringComparer.Ordinal);
            bool gaenge = false;
            string quelle = "", ausgabe = "", datum = "";
            foreach (DataRow r in t.Rows)
            {
                string art = Text(r, "Art");
                int zone = Ganz(r, "Klimazone");
                if (zone > 0) zonen.Add(zone);
                string gebaeudeart = Text(r, "Gebaeudeart");
                if (gebaeudeart.Length > 0) arten.Add(gebaeudeart);
                if (string.Equals(art, TwwSchema.TYPTAG_ART_KATEGORIE, StringComparison.Ordinal)) typtage.Add(Text(r, "Typtag"));
                if (string.Equals(art, TwwSchema.TYPTAG_ART_GANG, StringComparison.Ordinal)) gaenge = true;
                if (quelle.Length == 0) quelle = Text(r, "Quelle");
                if (ausgabe.Length == 0) ausgabe = Text(r, "Ausgabe");
                if (datum.Length == 0) datum = Text(r, "Datum_Import");
            }
            return new TwwTyptagstand(true, t.Rows.Count, zonen.ToList(), arten.ToList(), typtage.ToList(),
                                      gaenge, quelle, ausgabe, datum);
        }

        // =================================================================================
        //  Einspielen
        // =================================================================================

        /// <summary>
        /// <b>Spielt ein Paket aus einem Strom ein</b> (der Weg der Hülle: Datei über
        /// <c>Dienste.Datei</c> wählen, Strom öffnen, hier hereingeben). Gelesen wird über
        /// <see cref="Normformvektorleser.AusStrom"/>, geschrieben in EINEM Vorgang; ein Fehler
        /// rollt zurück und lässt den früheren Satz stehen.
        /// </summary>
        internal static TwwTyptagimportBericht Importieren(Stream strom, string datumImport = null)
        {
            var bericht = new TwwTyptagimportBericht { Stand = Stand() };
            if (!TabelleVorhanden())
            {
                bericht.Abbruch = ZapfSatz.Neu("TYPTAGIMPORT_TABELLE_FEHLT", ZapfSatz.Tabelle(TwwSchema.TAB_TWW_TYPTAG_IMPORT));
                return bericht;
            }
            Normformvektorsatz satz = Normformvektorleser.AusStrom(strom, out ZapfSatz fehler, bericht.Hinweise);
            if (satz == null)
            {
                bericht.Abbruch = fehler;
                return bericht;
            }
            return Schreiben(satz, datumImport, bericht);
        }

        /// <summary>
        /// <b>Spielt ein Paket aus schon gelesenen Dateien ein</b> (ein Ordner, ein Testhelfer) —
        /// sonst wie <see cref="Importieren(Stream, string)"/>.
        /// </summary>
        internal static TwwTyptagimportBericht Importieren(IReadOnlyList<TwwPaketdatei> dateien, string datumImport = null)
        {
            var bericht = new TwwTyptagimportBericht { Stand = Stand() };
            if (!TabelleVorhanden())
            {
                bericht.Abbruch = ZapfSatz.Neu("TYPTAGIMPORT_TABELLE_FEHLT", ZapfSatz.Tabelle(TwwSchema.TAB_TWW_TYPTAG_IMPORT));
                return bericht;
            }
            Normformvektorsatz satz = Normformvektorleser.AusDateien(dateien, out ZapfSatz fehler, bericht.Hinweise);
            if (satz == null)
            {
                bericht.Abbruch = fehler;
                return bericht;
            }
            return Schreiben(satz, datumImport, bericht);
        }

        /// <summary>
        /// Schreibt den gelesenen Satz: alte Zeilen weg, neue hinein, alles in EINEM Vorgang.
        /// Eine Ausnahme rollt zurück und wird benannt zurückgegeben; der frühere Satz bleibt.
        /// </summary>
        private static TwwTyptagimportBericht Schreiben(Normformvektorsatz satz, string datumImport,
                                                        TwwTyptagimportBericht bericht)
        {
            string datum = string.IsNullOrWhiteSpace(datumImport)
                ? DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : datumImport.Trim();
            satz.DatumImport = datum;
            string quelle = satz.Quelle;
            string ausgabe = satz.Ausgabe.Length == 0 ? null : satz.Ausgabe;
            int vorher = bericht.Stand?.Zeilen ?? 0;
            int geschrieben = 0;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        v.Ausfuehren("DELETE FROM \"" + TwwSchema.TAB_TWW_TYPTAG_IMPORT + "\"");

                        // 1. Die Systematik: je Kategorie drei Zeilen (Jahreszeit, Tagart, Bewoelkung).
                        foreach (Typtagkategorie k in satz.Kategorien)
                        {
                            geschrieben += Einfuegen(v, TwwSchema.TYPTAG_ART_KATEGORIE, 0, "", k.Code, null, 0,
                                                     (int)k.Jahreszeit, quelle, ausgabe, datum);
                            geschrieben += Einfuegen(v, TwwSchema.TYPTAG_ART_KATEGORIE, 0, "", k.Code, null, 1,
                                                     (int)k.Tagart, quelle, ausgabe, datum);
                            geschrieben += Einfuegen(v, TwwSchema.TYPTAG_ART_KATEGORIE, 0, "", k.Code, null, 2,
                                                     (int)k.Bewoelkung, quelle, ausgabe, datum);
                        }

                        Pruefnaht();

                        // 2. Kalendertage und Faktoren je Klimazone, Gebaeudeart und Kategorie.
                        foreach (string art in satz.Gebaeudearten)
                            foreach (int zone in satz.Klimazonen)
                                foreach (Typtagkategorie k in satz.Kategorien)
                                {
                                    int? anzahl = satz.Anzahl(zone, art, k.Code);
                                    if (anzahl.HasValue)
                                        geschrieben += Einfuegen(v, TwwSchema.TYPTAG_ART_ANZAHL, zone, art, k.Code, null, 0,
                                                                 anzahl.Value, quelle, ausgabe, datum);
                                    double? faktor = satz.Faktor(zone, art, k.Code);
                                    if (faktor.HasValue)
                                        geschrieben += Einfuegen(v, TwwSchema.TYPTAG_ART_FAKTOR, zone, art, k.Code, null, 0,
                                                                 faktor.Value, quelle, ausgabe, datum);
                                }

                        // 3. Die wahlfreien Tagesgaenge.
                        foreach (Typtaggang g in satz.Gaenge)
                            for (int i = 0; i < g.Anteile.Length; i++)
                                geschrieben += Einfuegen(v, TwwSchema.TYPTAG_ART_GANG, 0, g.Gebaeudeart, g.Typtag,
                                                         g.AufloesungMin, i, g.Anteile[i], quelle, ausgabe, datum);

                        // 4. Die Kennwerte des Verfahrens.
                        foreach (KeyValuePair<string, double> kw in satz.Kennwerte.OrderBy(x => x.Key, StringComparer.Ordinal))
                            geschrieben += Einfuegen(v, TwwSchema.TYPTAG_ART_KENNWERT, 0, "", kw.Key, null, 0,
                                                     kw.Value, quelle, ausgabe, datum);

                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex) when (ex is not LesemodusException)
            {
                bericht.Zeilen = 0;
                bericht.Abbruch = ZapfSatz.Neu("TYPTAGIMPORT_FEHLGESCHLAGEN", ex.Message);
                bericht.Stand = Stand();
                return bericht;
            }

            bericht.Zeilen = geschrieben;
            bericht.Ersetzt = vorher;
            bericht.Stand = Stand();
            return bericht;
        }

        private static int Einfuegen(DbVorgang v, string art, int zone, string gebaeudeart, string typtag,
                                     int? aufloesung, int zeilenindex, double wert, string quelle, string ausgabe,
                                     string datum)
        {
            return v.Ausfuehren("INSERT INTO \"" + TwwSchema.TAB_TWW_TYPTAG_IMPORT + "\" (" + SPALTEN + ") " +
                                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                new DbParam("@art", art),
                new DbParam("@zone", zone),
                new DbParam("@gebaeudeart", gebaeudeart ?? ""),
                new DbParam("@typtag", typtag ?? ""),
                new DbParam("@aufloesung", DbParamTyp.Integer) { Wert = aufloesung.HasValue ? (object)aufloesung.Value : null },
                new DbParam("@zeilenindex", zeilenindex),
                new DbParam("@wert", wert),
                new DbParam("@quelle", quelle ?? ""),
                new DbParam("@ausgabe", DbParamTyp.VarWChar) { Wert = ausgabe },
                new DbParam("@datum", datum ?? ""));
        }

        // =================================================================================
        //  Löschen
        // =================================================================================

        /// <summary>
        /// <b>Löscht die eingespielten Typtage.</b> Danach ist der Typtagweg benannt nicht
        /// verfügbar. Zurück kommt die Zahl der entfernten Zeilen; ohne Tabelle 0.
        /// </summary>
        internal static int Loeschen()
        {
            if (!TabelleVorhanden()) return 0;
            int vorher = Stand().Zeilen;
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + TwwSchema.TAB_TWW_TYPTAG_IMPORT + "\"");
            return vorher;
        }

        // =================================================================================
        //  Lesen für den Rechenweg
        // =================================================================================

        /// <summary>
        /// <b>Liest die eingespielten Typtage als Satz für den Rechenweg</b>
        /// (<c>Typtagzuordnung</c>). <c>null</c>, solange nichts eingespielt ist — der Aufrufer
        /// lehnt dann benannt ab, statt still auf den Bestandsweg zu fallen.
        /// </summary>
        internal static Normformvektorsatz Lesen()
        {
            if (!TabelleVorhanden()) return null;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Art\", \"Klimazone\", \"Gebaeudeart\", \"Typtag\", \"Aufloesung_min\", \"Zeilenindex\", " +
                "\"Wert\", \"Quelle\", \"Ausgabe\", \"Datum_Import\" FROM \"" + TwwSchema.TAB_TWW_TYPTAG_IMPORT + "\" " +
                "ORDER BY \"Art\", \"Klimazone\", \"Gebaeudeart\", \"Typtag\", \"Zeilenindex\"");
            if (t == null || t.Rows.Count == 0) return null;

            var satz = new Normformvektorsatz();
            // Die Systematik zuerst: ohne Kategorien lassen sich die uebrigen Zeilen nicht deuten.
            var merkmale = new Dictionary<string, int[]>(StringComparer.Ordinal);
            var reihenfolge = new List<string>();
            var gaenge = new Dictionary<string, (string Art, string Typtag, int Aufloesung, SortedDictionary<int, double> Werte)>(StringComparer.Ordinal);

            foreach (DataRow r in t.Rows)
            {
                string art = Text(r, "Art");
                if (!string.Equals(art, TwwSchema.TYPTAG_ART_KATEGORIE, StringComparison.Ordinal)) continue;
                string code = Text(r, "Typtag");
                if (!merkmale.TryGetValue(code, out int[] m))
                {
                    m = new[] { -1, -1, -1 };
                    merkmale[code] = m;
                    reihenfolge.Add(code);
                }
                int i = Ganz(r, "Zeilenindex");
                if (i >= 0 && i < 3) m[i] = (int)Math.Round(Zahl(r, "Wert"));
            }
            satz.KategorienSetzen(reihenfolge
                .Where(c => merkmale[c].All(x => x >= 0))
                .Select(c => new Typtagkategorie(c, (Typtagjahreszeit)merkmale[c][0], (Typtagart)merkmale[c][1],
                                                 (Typtagbewoelkung)merkmale[c][2])));
            if (satz.Kategorien.Count == 0) return null;

            foreach (DataRow r in t.Rows)
            {
                string art = Text(r, "Art");
                string gebaeudeart = Text(r, "Gebaeudeart");
                string typtag = Text(r, "Typtag");
                int zone = Ganz(r, "Klimazone");
                int index = Ganz(r, "Zeilenindex");
                double wert = Zahl(r, "Wert");
                if (satz.Quelle.Length == 0) satz.Quelle = Text(r, "Quelle");
                if (satz.Ausgabe.Length == 0) satz.Ausgabe = Text(r, "Ausgabe");
                if (satz.DatumImport.Length == 0) satz.DatumImport = Text(r, "Datum_Import");

                if (string.Equals(art, TwwSchema.TYPTAG_ART_ANZAHL, StringComparison.Ordinal))
                    satz.AnzahlSetzen(zone, gebaeudeart, typtag, (int)Math.Round(wert));
                else if (string.Equals(art, TwwSchema.TYPTAG_ART_FAKTOR, StringComparison.Ordinal))
                    satz.FaktorSetzen(zone, gebaeudeart, typtag, wert);
                else if (string.Equals(art, TwwSchema.TYPTAG_ART_KENNWERT, StringComparison.Ordinal))
                    satz.KennwertSetzen(typtag, wert);
                else if (string.Equals(art, TwwSchema.TYPTAG_ART_GANG, StringComparison.Ordinal))
                {
                    int aufloesung = Ganz(r, "Aufloesung_min");
                    if (aufloesung < 1) continue;
                    string schluessel = gebaeudeart + "\u0001" + typtag;
                    if (!gaenge.TryGetValue(schluessel, out var g))
                    {
                        g = (gebaeudeart, typtag, aufloesung, new SortedDictionary<int, double>());
                        gaenge[schluessel] = g;
                    }
                    g.Werte[index] = wert;
                }
            }

            foreach (var g in gaenge.Values)
            {
                int abschnitte = Normformvektorleser.MINUTEN_JE_TAG / g.Aufloesung;
                if (g.Werte.Count != abschnitte) continue;      // ein halber Gang traegt nichts
                satz.GangSetzen(new Typtaggang(g.Art, g.Typtag, g.Aufloesung, g.Werte.Values.ToArray()));
            }
            return satz.Traegt ? satz : null;
        }

        // =================================================================================
        //  Feldleser
        // =================================================================================

        private static string Text(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture) ?? "" : "";

        private static int Ganz(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value ? Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture) : 0;

        private static double Zahl(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value ? Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture) : 0.0;
    }
}
