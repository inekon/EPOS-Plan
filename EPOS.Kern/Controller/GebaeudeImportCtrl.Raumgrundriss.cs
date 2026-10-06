using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Grundrisse je importiertem Raum</b> (HC-5, Konzept HottCAD-Verbund 11.3, Schemaschritt
    /// <see cref="RaumgrundrissSchema.SCHRITT"/>): Schreiben an eine Importquelle und Lesen je Gebäude bzw. Zone. Nur über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern.
    ///
    /// <para><b>Lesen:</b> die Zeilen der jüngsten Quelle des Gebäudes (größte <c>ID</c>). <b>Löschen</b> regelt die
    /// Datenbank: Gebäude → Quelle → Grundriss mit Kaskade, das Löschen einer Zone setzt <c>ID_Zone</c> auf NULL.</para>
    /// </summary>
    public sealed partial class GebaeudeImportCtrl
    {
        private static readonly string GRUNDRISSSPALTEN = Spalten(RaumgrundrissSchema.Spalten, "g");

        /// <summary>
        /// <b>Die Grundrisse eines Gebäudes</b> — die Zeilen der jüngsten Importquelle, in der Reihenfolge des Schreibens
        /// (die Reihenfolge der Räume in der Datei). Leer ohne Quelle, ohne Zeilen oder ohne die Tabelle.
        /// </summary>
        internal List<Raumgrundriss> LesenRaumgrundrisse(int idGebaeude)
        {
            if (!DataRepository.TabelleVorhanden(RaumgrundrissSchema.TAB)) return new List<Raumgrundriss>();
            return Grundrisse(DataRepository.GetDataTable(
                "SELECT " + GRUNDRISSSPALTEN + " FROM \"" + RaumgrundrissSchema.TAB + "\" g " +
                "WHERE g.\"ID_Importquelle\" = (SELECT MAX(q.\"ID\") FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" q " +
                "WHERE q.\"ID_Gebaeude\" = ?) ORDER BY g.\"ID\"",
                new DbParam("@g", idGebaeude)));
        }

        /// <summary>
        /// <b>Die Grundrisse einer Quelle</b> (HC-5, F7: was „Grundriss übernehmen“ ersetzen würde), in der Reihenfolge des
        /// Schreibens. Leer ohne Zeilen oder ohne die Tabelle.
        /// </summary>
        internal List<Raumgrundriss> LesenRaumgrundrisseDerQuelle(int idImportquelle)
        {
            if (!DataRepository.TabelleVorhanden(RaumgrundrissSchema.TAB)) return new List<Raumgrundriss>();
            return Grundrisse(DataRepository.GetDataTable(
                "SELECT " + GRUNDRISSSPALTEN + " FROM \"" + RaumgrundrissSchema.TAB + "\" g WHERE g.\"ID_Importquelle\" = ? ORDER BY g.\"ID\"",
                new DbParam("@q", idImportquelle)));
        }

        /// <summary>
        /// <b>Stimmen die gespeicherten Grundrisse einer Quelle mit <paramref name="frisch"/> überein</b> (F7)? Gleich heißt: dieselben
        /// Kennungen in derselben Reihenfolge mit denselben Ringen, Böden, Höhen, Herleitungen und Vermerken — nur die
        /// speicherbaren zählen. Sonst (keine oder ältere Zeilen) bietet „Datei erneut lesen“ das Nachtragen an.
        /// </summary>
        internal static bool Gleich(IReadOnlyList<Raumgrundriss> gespeichert, IReadOnlyList<Raumgrundriss> frisch)
        {
            List<Raumgrundriss> a = (gespeichert ?? Array.Empty<Raumgrundriss>()).ToList();
            List<Raumgrundriss> b = (frisch ?? Array.Empty<Raumgrundriss>()).Where(Speicherbar).ToList();
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!string.Equals(a[i].Quellkennung, b[i].Quellkennung, StringComparison.Ordinal)
                    || !string.Equals(a[i].RingeText, b[i].RingeText, StringComparison.Ordinal)
                    || Math.Abs(a[i].BodenM - b[i].BodenM) > 1e-9 || Math.Abs(a[i].HoeheM - b[i].HoeheM) > 1e-9
                    || a[i].Herleitung != b[i].Herleitung
                    || !string.Equals(a[i].VermerkeText, b[i].VermerkeText, StringComparison.Ordinal))
                    return false;
            return true;
        }

        /// <summary>
        /// <b>Die Grundrisse einer Zone</b> — die Räume, die beim Import dieser Zone zugeordnet wurden, aus der jüngsten
        /// Importquelle ihres Gebäudes. Leer ohne Zeilen oder ohne die Tabelle.
        /// </summary>
        internal List<Raumgrundriss> LesenRaumgrundrisseDerZone(int idZone)
        {
            if (!DataRepository.TabelleVorhanden(RaumgrundrissSchema.TAB)) return new List<Raumgrundriss>();
            return Grundrisse(DataRepository.GetDataTable(
                "SELECT " + GRUNDRISSSPALTEN + " FROM \"" + RaumgrundrissSchema.TAB + "\" g " +
                "WHERE g.\"ID_Zone\" = ? AND g.\"ID_Importquelle\" = (SELECT MAX(q.\"ID\") FROM \"" + ImportzuordnungSchema.TAB_QUELLE +
                "\" q WHERE q.\"ID_Gebaeude\" = (SELECT z.\"ID_Gebaeude\" FROM \"" + SchemaKatalog.TAB_ZONE + "\" z WHERE z.\"ID\" = ?)) " +
                "ORDER BY g.\"ID\"",
                new DbParam("@z", idZone), new DbParam("@z2", idZone)));
        }

        /// <summary>
        /// <b>Schreibt die Grundrisse einer vorhandenen Quelle neu</b> — die Zeilen der Quelle werden ersetzt (der Weg des
        /// Nachtragens, F7). Die Zone kommt aus den Paarungen der Quelle in <c>Tab_Importzuordnung</c> (Ziel Zone, dieselbe
        /// Kennung). Mit <paramref name="vorgang"/> im Vorgang des Aufrufers, sonst in einem eigenen.
        /// </summary>
        internal Ergebnis SchreibeRaumgrundrisse(int idImportquelle, IReadOnlyList<Raumgrundriss> grundrisse, DbVorgang vorgang = null)
        {
            if (!DataRepository.TabelleVorhanden(RaumgrundrissSchema.TAB))
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_NICHT_GESPEICHERT,
                                                     RaumgrundrissSchema.TAB));
            using Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    object da = v.Skalar("SELECT COUNT(*) FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" WHERE \"ID\" = ?",
                                         new DbParam("@q", idImportquelle));
                    if (da == null || da == DBNull.Value || Convert.ToInt64(da, CultureInfo.InvariantCulture) == 0)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(MyResource.Resource.HERKUNFT_MSG_QUELLE_FEHLT);
                    }
                    var zoneJeKennung = new Dictionary<string, int>(StringComparer.Ordinal);
                    DataTable t = v.Lese("SELECT \"Quellkennung\", \"ID_Zone\" FROM \"" + ImportzuordnungSchema.TAB_ZUORDNUNG + "\" " +
                                         "WHERE \"ID_Importquelle\" = ? AND \"ID_Zone\" IS NOT NULL ORDER BY \"ID\"",
                                         new DbParam("@q", idImportquelle));
                    foreach (DataRow r in t.Rows)
                        zoneJeKennung.TryAdd(Convert.ToString(r[0], CultureInfo.InvariantCulture) ?? "",
                                             Convert.ToInt32(r[1], CultureInfo.InvariantCulture));
                    v.Ausfuehren("DELETE FROM \"" + RaumgrundrissSchema.TAB + "\" WHERE \"ID_Importquelle\" = ?",
                                 new DbParam("@q", idImportquelle));
                    GrundrisseEinfuegen(v, idImportquelle, grundrisse, zoneJeKennung);
                    v.Commit();
                    return Ergebnis.Gut(idImportquelle);
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_NICHT_GESPEICHERT,
                                                     ex.Message));
            }
        }

        /// <summary>
        /// Ist der Grundriss speicherbar — was die <c>CHECK</c>-Regeln der Tabelle verlangen? Ein nicht speicherbarer
        /// Grundriss wird übergangen (nur Anzeige und Export: er lässt den Import nicht scheitern).
        /// </summary>
        internal static bool Speicherbar(Raumgrundriss g)
        {
            if (g == null || string.IsNullOrEmpty(g.Quellkennung) || g.Quellkennung.Length > RaumgrundrissSchema.QUELLKENNUNG_MAX)
                return false;
            if (!(g.HoeheM > 0) || !(g.RingflaecheM2 > 0) || double.IsNaN(g.BodenM) || double.IsInfinity(g.BodenM)) return false;
            if (!RaumgrundrissSchema.HERLEITUNGEN.Contains(g.Herleitung.ToString())) return false;
            string ringe = g.RingeText;
            if (ringe.Length < RaumgrundrissSchema.RINGE_MIN || ringe.Length > RaumgrundrissSchema.RINGE_MAX) return false;
            string vermerke = g.VermerkeText;
            return vermerke == null || vermerke.Length <= RaumgrundrissSchema.VERMERKE_MAX;
        }

        /// <summary>Fügt die speicherbaren Grundrisse an die Quelle an — je Kennung einer, die Zone aus der Zuordnung.</summary>
        private static int GrundrisseEinfuegen(DbVorgang v, int idQuelle, IReadOnlyList<Raumgrundriss> grundrisse,
                                               IReadOnlyDictionary<string, int> zoneJeKennung)
        {
            if (grundrisse == null || grundrisse.Count == 0) return 0;
            string einfuegen = "INSERT INTO \"" + RaumgrundrissSchema.TAB + "\" (" +
                               string.Join(", ", RaumgrundrissSchema.Spalten.Select(s => "\"" + s + "\"")) + ") VALUES (" +
                               BaustoffCtrl.Fragezeichen(RaumgrundrissSchema.Spalten.Count) + ")";
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            int n = 0;
            foreach (Raumgrundriss g in grundrisse)
            {
                if (!Speicherbar(g) || !gesehen.Add(g.Quellkennung)) continue;
                int? zone = zoneJeKennung != null && zoneJeKennung.TryGetValue(g.Quellkennung, out int z) ? z : (int?)null;
                v.Ausfuehren(einfuegen,
                    new DbParam("@q", idQuelle),
                    new DbParam("@z", DbParamTyp.Integer) { Wert = zone.HasValue ? (object)zone.Value : DBNull.Value },
                    new DbParam("@k", g.Quellkennung),
                    BaustoffCtrl.Text("@n", g.Raumname),
                    BaustoffCtrl.Text("@s", g.Geschoss),
                    BaustoffCtrl.Zahl("@l", g.GeschossLageM),
                    BaustoffCtrl.Zahl("@b", g.BodenM),
                    BaustoffCtrl.Zahl("@h", g.HoeheM),
                    new DbParam("@r", g.RingeText),
                    BaustoffCtrl.Zahl("@f", g.RingflaecheM2),
                    BaustoffCtrl.Zahl("@a", g.Abweichung),
                    new DbParam("@w", g.Herleitung.ToString()),
                    BaustoffCtrl.Text("@v", g.VermerkeText));
                n++;
            }
            return n;
        }

        private static List<Raumgrundriss> Grundrisse(DataTable t)
        {
            var liste = new List<Raumgrundriss>();
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
            {
                IReadOnlyList<Grundrissring> ringe = Raumgrundriss.RingeLesen(BaustoffCtrl.TextAus(r, RaumgrundrissSchema.SPALTE_RINGE));
                Enum.TryParse(BaustoffCtrl.TextAus(r, RaumgrundrissSchema.SPALTE_HERLEITUNG) ?? "", false, out Umrissherleitung herleitung);
                liste.Add(new Raumgrundriss
                {
                    ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                    IdImportquelle = Convert.ToInt32(r[RaumgrundrissSchema.SPALTE_ID_IMPORTQUELLE], CultureInfo.InvariantCulture),
                    IdZone = BaustoffCtrl.GanzAus(r, RaumgrundrissSchema.SPALTE_ID_ZONE),
                    Quellkennung = BaustoffCtrl.TextAus(r, RaumgrundrissSchema.SPALTE_QUELLKENNUNG) ?? "",
                    Raumname = BaustoffCtrl.TextAus(r, RaumgrundrissSchema.SPALTE_RAUMNAME),
                    Geschoss = BaustoffCtrl.TextAus(r, RaumgrundrissSchema.SPALTE_GESCHOSS),
                    GeschossLageM = BaustoffCtrl.ZahlAus(r, RaumgrundrissSchema.SPALTE_GESCHOSS_LAGE),
                    BodenM = BaustoffCtrl.ZahlAus(r, RaumgrundrissSchema.SPALTE_BODEN) ?? 0.0,
                    HoeheM = BaustoffCtrl.ZahlAus(r, RaumgrundrissSchema.SPALTE_HOEHE) ?? 0.0,
                    Ringe = ringe,
                    RingflaecheM2 = BaustoffCtrl.ZahlAus(r, RaumgrundrissSchema.SPALTE_RINGFLAECHE) ?? 0.0,
                    Abweichung = BaustoffCtrl.ZahlAus(r, RaumgrundrissSchema.SPALTE_ABWEICHUNG),
                    Herleitung = herleitung,
                    Vermerke = Raumgrundriss.VermerkeLesen(BaustoffCtrl.TextAus(r, RaumgrundrissSchema.SPALTE_VERMERKE)),
                });
            }
            return liste;
        }
    }
}
