using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // Prueflogik der Dublettenerkennung (Konzept, Abschnitte 3 und 6.1). REIN LESEND -
    // diese Klasse schreibt nie in die Datenbank; Bereinigen/Loeschen/Umbenennen liegt
    // bei den Aufrufern (Admin-Dialog, Migration). Damit bleibt auch die Zusage von
    // KiSchreibschutz unberuehrt: Der KI-Assistent erhaelt hier keinen Schreibweg.
    //
    // Begriffe:
    //  - Namensdublette: normalisierter Name (Trim, Mehrfach-Leerzeichen, Gross/Klein)
    //    kommt mehrfach vor.
    //  - Inhaltsdublette: alle Vergleichsspalten und Datenbloecke stimmen ueberein
    //    (exakt nach invarianter Formatierung, ohne Toleranz - Entscheidung 9.1).
    // ====================================================================================

    /// <summary>Ein Kopfsatz eines Katalogs mit vorberechnetem Inhalts-Hash.</summary>
    public class KatalogSatz
    {
        public int Id;
        public string Name;
        public string NameNormalisiert;
        public bool ReadOnly;
        public string InhaltsHash;
        public DataRow Zeile;
    }

    /// <summary>Eine Gruppe von Saetzen mit gleichem Namen bzw. gleichem Inhalt.</summary>
    public class DublettenGruppe
    {
        /// <summary>Der gemeinsame Wert: normalisierter Name bzw. Inhalts-Hash.</summary>
        public string Schluesselwert;
        public List<KatalogSatz> Saetze = new List<KatalogSatz>();
        /// <summary>true, wenn die Saetze der Gruppe unterschiedliche Namen tragen.</summary>
        public bool VerschiedeneNamen
        {
            get
            {
                for (int i = 1; i < Saetze.Count; i++)
                    if (!string.Equals(Saetze[i].NameNormalisiert, Saetze[0].NameNormalisiert, StringComparison.Ordinal))
                        return true;
                return false;
            }
        }
    }

    /// <summary>Ergebnis eines Katalog-Scans (Admin-Dublettensuche, Migration).</summary>
    public class ScanErgebnis
    {
        public KatalogDefinition Katalog;
        public List<KatalogSatz> Saetze = new List<KatalogSatz>();
        /// <summary>Gruppen mit mehrfach vergebenem (normalisiertem) Namen.</summary>
        public List<DublettenGruppe> Namensgruppen = new List<DublettenGruppe>();
        /// <summary>Gruppen inhaltsgleicher Saetze (inkl. Datenbloecke).</summary>
        public List<DublettenGruppe> Inhaltsgruppen = new List<DublettenGruppe>();
        /// <summary>null = Scan lief; sonst der Grund, warum die Tabelle nicht lesbar war.</summary>
        public string Fehler;
    }

    /// <summary>Befund der Import-Vorpruefung je Kandidat (Konzept 3.3).</summary>
    public enum ImportBefund
    {
        Neu,
        /// <summary>Name vorhanden, Inhalt (Import-Schnittmenge) gleich.</summary>
        Identisch,
        /// <summary>Name vorhanden, Inhalt abweichend - Namenskonflikt.</summary>
        NameVorhanden,
        /// <summary>Name neu, aber Inhalt gleich wie ein vorhandener Satz.</summary>
        InhaltsGleich
    }

    /// <summary>Ein zu importierender Eintrag fuer die Vorpruefung.</summary>
    public class ImportKandidat
    {
        public string Name;
        /// <summary>Spaltenname -> Wert, genau die Werte, die der Import speichern wuerde.</summary>
        public Dictionary<string, object> Werte =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Freier Verweis des Aufrufers (z.B. Listenindex).</summary>
        public object Tag;
    }

    /// <summary>Pruefergebnis zu einem <see cref="ImportKandidat"/>.</summary>
    public class ImportPruefung
    {
        public ImportKandidat Kandidat;
        public ImportBefund Befund = ImportBefund.Neu;
        /// <summary>Bei Namens-/Inhaltstreffern: der betroffene Bestandssatz.</summary>
        public KatalogSatz Vorhanden;
        /// <summary>Spalten der Import-Schnittmenge, in denen sich Kandidat und Bestand unterscheiden.</summary>
        public List<string> AbweichendeSpalten = new List<string>();
        /// <summary>true: der Name trifft im Katalog MEHRERE Saetze (Altbestand) - kein Ueberschreiben moeglich.</summary>
        public bool NameMehrfachInDb;
        /// <summary>true: derselbe (normalisierte) Name kommt in der Importauswahl mehrfach vor.</summary>
        public bool NameDoppeltInAuswahl;
    }

    public static class DublettenPruefung
    {
        private static readonly Regex _mehrfachLeerraum = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Normalisiert einen Namen fuer den Vergleich: Trim, innere Leerraumfolgen zu
        /// einem Leerzeichen, Kleinschreibung (invariant). Access vergleicht ohnehin
        /// case-insensitiv - die C#-Seite zieht damit gleich (Konzept 3.1).
        /// </summary>
        public static string NormalisiereName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return _mehrfachLeerraum.Replace(name.Trim(), " ").ToLowerInvariant();
        }

        // ------------------------------------------------------------------ Scan ------

        /// <summary>
        /// Laedt einen Katalog samt Datenbloecken und liefert Namens- und
        /// Inhaltsgruppen. Eine Kopfzeile ohne lesbare Tabelle ergibt
        /// <see cref="ScanErgebnis.Fehler"/> statt einer Exception.
        /// </summary>
        public static ScanErgebnis ScanKatalog(KatalogDefinition k)
        {
            ScanErgebnis erg = new ScanErgebnis { Katalog = k };

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + k.Tabelle + "] ORDER BY [" + k.NamensSpalte + "], [" + k.IdSpalte + "]");
            if (dt == null)
            {
                erg.Fehler = "Tabelle " + k.Tabelle + " konnte nicht gelesen werden.";
                return erg;
            }
            if (!dt.Columns.Contains(k.IdSpalte) || !dt.Columns.Contains(k.NamensSpalte))
            {
                erg.Fehler = "Tabelle " + k.Tabelle + " fuehrt die Spalten " + k.IdSpalte +
                             "/" + k.NamensSpalte + " nicht.";
                return erg;
            }

            // Datenbloecke je Blocktabelle EINMAL laden und nach FK gruppieren -
            // nicht eine Abfrage je Kopfsatz (Ganglinien: 8760 Zeilen je Satz).
            var bloecke = new List<Dictionary<int, List<DataRow>>>();
            foreach (KatalogDatenblock b in k.Datenbloecke)
            {
                var jeFk = new Dictionary<int, List<DataRow>>();
                DataTable bt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + b.Tabelle + "] ORDER BY [" + b.FkSpalte + "], " + b.Sortierung);
                if (bt != null)
                {
                    foreach (DataRow r in bt.Rows)
                    {
                        int fk = SichereZahl(r[b.FkSpalte]);
                        List<DataRow> liste;
                        if (!jeFk.TryGetValue(fk, out liste))
                        {
                            liste = new List<DataRow>();
                            jeFk[fk] = liste;
                        }
                        liste.Add(r);
                    }
                }
                bloecke.Add(jeFk);
            }

            List<string> vergleichsSpalten = VergleichsSpalten(k, dt);

            foreach (DataRow r in dt.Rows)
            {
                KatalogSatz s = new KatalogSatz
                {
                    Id = SichereZahl(r[k.IdSpalte]),
                    Name = r[k.NamensSpalte] is DBNull ? "" : Convert.ToString(r[k.NamensSpalte]),
                    ReadOnly = dt.Columns.Contains("ReadOnly") && SichereWahr(r["ReadOnly"]),
                    Zeile = r
                };
                s.NameNormalisiert = NormalisiereName(s.Name);
                s.InhaltsHash = InhaltsHash(k, r, vergleichsSpalten, bloecke, s.Id);
                erg.Saetze.Add(s);
            }

            erg.Namensgruppen = Gruppiere(erg.Saetze, s => s.NameNormalisiert);
            erg.Inhaltsgruppen = Gruppiere(erg.Saetze, s => s.InhaltsHash);
            return erg;
        }

        /// <summary>
        /// Spalten der Namensgruppe, in denen sich zwei Saetze unterscheiden
        /// (fuer die Gegenueberstellung im Admin-Dialog).
        /// </summary>
        public static List<string> AbweichendeSpalten(KatalogDefinition k, KatalogSatz a, KatalogSatz b)
        {
            List<string> abw = new List<string>();
            if (a.Zeile == null || b.Zeile == null) return abw;
            foreach (string sp in VergleichsSpalten(k, a.Zeile.Table))
                if (!string.Equals(Kanonisch(a.Zeile[sp]), Kanonisch(b.Zeile[sp]), StringComparison.Ordinal))
                    abw.Add(sp);
            if (!string.Equals(a.InhaltsHash, b.InhaltsHash, StringComparison.Ordinal) && abw.Count == 0)
                abw.Add("(Datenblock)");   // Kopf gleich, Unterschied liegt in den Datenbloecken
            return abw;
        }

        // --------------------------------------------------- Import-Vorpruefung ------

        /// <summary>
        /// Prueft eine Importauswahl gegen den Katalog UND gegen sich selbst
        /// (Konzept 4.1). Der Inhaltsvergleich laeuft ueber die Schnittmenge aus
        /// Import- und Vergleichsspalten des Kopfsatzes; Datenbloecke bleiben beim
        /// Import bewusst aussen vor (Konzept 3.2 gilt fuer den Scan).
        /// </summary>
        public static List<ImportPruefung> PruefeKandidaten(KatalogDefinition k, IList<ImportKandidat> kandidaten)
        {
            var ergebnisse = new List<ImportPruefung>();
            if (kandidaten == null || kandidaten.Count == 0) return ergebnisse;

            DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + k.Tabelle + "]");

            // Bestand nach normalisiertem Namen und nach Schnittmengen-Hash aufbereiten.
            List<string> schnittmenge = ImportVergleichsSpalten(k, dt);
            var jeName = new Dictionary<string, List<KatalogSatz>>(StringComparer.Ordinal);
            var jeHash = new Dictionary<string, KatalogSatz>(StringComparer.Ordinal);

            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    KatalogSatz s = new KatalogSatz
                    {
                        Id = SichereZahl(r[k.IdSpalte]),
                        Name = r[k.NamensSpalte] is DBNull ? "" : Convert.ToString(r[k.NamensSpalte]),
                        ReadOnly = dt.Columns.Contains("ReadOnly") && SichereWahr(r["ReadOnly"]),
                        Zeile = r
                    };
                    s.NameNormalisiert = NormalisiereName(s.Name);
                    s.InhaltsHash = HashWerte(schnittmenge, sp => r.Table.Columns.Contains(sp) ? r[sp] : null);

                    List<KatalogSatz> liste;
                    if (!jeName.TryGetValue(s.NameNormalisiert, out liste))
                    {
                        liste = new List<KatalogSatz>();
                        jeName[s.NameNormalisiert] = liste;
                    }
                    liste.Add(s);
                    if (!jeHash.ContainsKey(s.InhaltsHash)) jeHash[s.InhaltsHash] = s;
                }
            }

            // Doppelte Namen innerhalb der Auswahl selbst zaehlen.
            var auswahlNamen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ImportKandidat kand in kandidaten)
            {
                string n = NormalisiereName(kand.Name);
                auswahlNamen[n] = auswahlNamen.ContainsKey(n) ? auswahlNamen[n] + 1 : 1;
            }

            foreach (ImportKandidat kand in kandidaten)
            {
                ImportPruefung p = new ImportPruefung { Kandidat = kand };
                string normName = NormalisiereName(kand.Name);
                p.NameDoppeltInAuswahl = auswahlNamen[normName] > 1;

                string kandHash = HashWerte(schnittmenge,
                    sp => kand.Werte.ContainsKey(sp) ? kand.Werte[sp] : null);

                List<KatalogSatz> treffer;
                if (jeName.TryGetValue(normName, out treffer) && treffer.Count > 0)
                {
                    p.Vorhanden = treffer[0];
                    p.NameMehrfachInDb = treffer.Count > 1;
                    foreach (string sp in schnittmenge)
                    {
                        object bestand = treffer[0].Zeile.Table.Columns.Contains(sp) ? treffer[0].Zeile[sp] : null;
                        object neu = kand.Werte.ContainsKey(sp) ? kand.Werte[sp] : null;
                        if (!string.Equals(Kanonisch(bestand), Kanonisch(neu), StringComparison.Ordinal))
                            p.AbweichendeSpalten.Add(sp);
                    }
                    p.Befund = p.AbweichendeSpalten.Count == 0
                        ? ImportBefund.Identisch
                        : ImportBefund.NameVorhanden;
                }
                else if (schnittmenge.Count > 0 && jeHash.ContainsKey(kandHash))
                {
                    p.Befund = ImportBefund.InhaltsGleich;
                    p.Vorhanden = jeHash[kandHash];
                }

                ergebnisse.Add(p);
            }
            return ergebnisse;
        }

        /// <summary>
        /// Je Datenblock des Katalogs ein Hash ueber die Blockzeilen GENAU EINES
        /// Kopfsatzes; leerer Block ergibt "". Gleiche Kanonisierung wie im
        /// Inhalts-Hash. Fuer die Migrations-Bereinigung: Eine Namensdublette darf
        /// nur entfallen, wenn ihre Bloecke leer oder mit denen des behaltenen
        /// Satzes identisch sind (Konzept 7.1, WP-Kaskade).
        /// </summary>
        public static List<string> BlockHashes(KatalogDefinition k, int id)
        {
            var hashes = new List<string>();
            foreach (KatalogDatenblock b in k.Datenbloecke)
            {
                DataTable bt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + b.Tabelle + "] WHERE [" + b.FkSpalte + "] = ? ORDER BY " + b.Sortierung,
                    new DbParam("@fk", id));
                if (bt == null || bt.Rows.Count == 0)
                {
                    hashes.Add("");
                    continue;
                }
                StringBuilder sb = new StringBuilder();
                foreach (DataRow z in bt.Rows)
                {
                    foreach (string sp in b.WertSpalten)
                        sb.Append(Kanonisch(z.Table.Columns.Contains(sp) ? z[sp] : null)).Append('|');
                    sb.Append('\n');
                }
                hashes.Add(Sha256(sb.ToString()));
            }
            return hashes;
        }

        /// <summary>
        /// Alle im Katalog vergebenen Namen, normalisiert - fuer die Namensvalidierung
        /// des Konfliktdialogs (Umbenennen, Konzept 4.3).
        /// </summary>
        public static HashSet<string> VergebeneNamen(KatalogDefinition k)
        {
            var namen = new HashSet<string>(StringComparer.Ordinal);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [" + k.NamensSpalte + "] FROM [" + k.Tabelle + "]");
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    if (!(r[0] is DBNull))
                        namen.Add(NormalisiereName(Convert.ToString(r[0])));
            return namen;
        }

        // ------------------------------------------------------------- Bausteine ------

        /// <summary>
        /// Die Vergleichsspalten eines Katalogs: alle Spalten der Kopftabelle ohne
        /// Id-, Namens-, ReadOnly-, Beschreibungs- und Ausschlussspalten (Konzept 3.2).
        /// </summary>
        public static List<string> VergleichsSpalten(KatalogDefinition k, DataTable dt)
        {
            var ausschluss = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                k.IdSpalte, k.NamensSpalte, "ReadOnly", "Beschreibung"
            };
            foreach (string a in k.AusschlussSpalten) ausschluss.Add(a);

            var spalten = new List<string>();
            if (dt == null) return spalten;
            foreach (DataColumn c in dt.Columns)
                if (!ausschluss.Contains(c.ColumnName))
                    spalten.Add(c.ColumnName);
            spalten.Sort(StringComparer.OrdinalIgnoreCase);   // stabile Hash-Reihenfolge
            return spalten;
        }

        /// <summary>Schnittmenge aus Import- und Vergleichsspalten (Kopfsatz).</summary>
        private static List<string> ImportVergleichsSpalten(KatalogDefinition k, DataTable dt)
        {
            var schnitt = new List<string>();
            if (k.ImportSpalten == null || dt == null) return schnitt;
            var vergleich = new HashSet<string>(VergleichsSpalten(k, dt), StringComparer.OrdinalIgnoreCase);
            foreach (string sp in k.ImportSpalten)
                if (vergleich.Contains(sp))
                    schnitt.Add(sp);
            schnitt.Sort(StringComparer.OrdinalIgnoreCase);
            return schnitt;
        }

        private static string InhaltsHash(KatalogDefinition k, DataRow r, List<string> vergleichsSpalten,
                                          List<Dictionary<int, List<DataRow>>> bloecke, int id)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string sp in vergleichsSpalten)
                sb.Append(sp).Append('=').Append(Kanonisch(r[sp])).Append('\n');

            for (int i = 0; i < k.Datenbloecke.Length; i++)
            {
                KatalogDatenblock b = k.Datenbloecke[i];
                sb.Append("#block:").Append(b.Tabelle).Append('\n');
                List<DataRow> zeilen;
                if (bloecke[i].TryGetValue(id, out zeilen))
                {
                    foreach (DataRow z in zeilen)
                    {
                        foreach (string sp in b.WertSpalten)
                            sb.Append(Kanonisch(z.Table.Columns.Contains(sp) ? z[sp] : null)).Append('|');
                        sb.Append('\n');
                    }
                }
            }
            return Sha256(sb.ToString());
        }

        private static string HashWerte(List<string> spalten, Func<string, object> wert)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string sp in spalten)
                sb.Append(sp).Append('=').Append(Kanonisch(wert(sp))).Append('\n');
            return Sha256(sb.ToString());
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Kanonische Textform eines Zellwerts: NULL als eigener Marker, Zahlen
        /// invariant ("R"), Texte getrimmt/kleingeschrieben, Wahrheitswerte 0/1.
        /// Exakter Vergleich ohne Toleranz (Entscheidung 9.1) - beide Seiten stammen
        /// aus derselben Spalte bzw. demselben Importpfad, Artefakte sind identisch.
        /// </summary>
        public static string Kanonisch(object v)
        {
            if (v == null || v is DBNull) return "~";
            if (v is bool) return (bool)v ? "1" : "0";
            string s = v as string;
            if (s != null) return _mehrfachLeerraum.Replace(s.Trim(), " ").ToLowerInvariant();
            if (v is float) return ((double)(float)v).ToString("R", CultureInfo.InvariantCulture);
            if (v is double) return ((double)v).ToString("R", CultureInfo.InvariantCulture);
            if (v is decimal) return ((decimal)v).ToString(CultureInfo.InvariantCulture);
            if (v is DateTime) return ((DateTime)v).ToString("o", CultureInfo.InvariantCulture);
            IFormattable f = v as IFormattable;
            if (f != null) return f.ToString(null, CultureInfo.InvariantCulture);
            return v.ToString();
        }

        private static List<DublettenGruppe> Gruppiere(List<KatalogSatz> saetze, Func<KatalogSatz, string> schluessel)
        {
            var gruppen = new Dictionary<string, DublettenGruppe>(StringComparer.Ordinal);
            var ergebnis = new List<DublettenGruppe>();
            foreach (KatalogSatz s in saetze)
            {
                string key = schluessel(s);
                DublettenGruppe g;
                if (!gruppen.TryGetValue(key, out g))
                {
                    g = new DublettenGruppe { Schluesselwert = key };
                    gruppen[key] = g;
                    ergebnis.Add(g);
                }
                g.Saetze.Add(s);
            }
            ergebnis.RemoveAll(g => g.Saetze.Count < 2);
            return ergebnis;
        }

        private static int SichereZahl(object v)
        {
            try { return v == null || v is DBNull ? 0 : Convert.ToInt32(v); }
            catch { return 0; }
        }

        private static bool SichereWahr(object v)
        {
            try { return !(v == null || v is DBNull) && Convert.ToBoolean(v); }
            catch { return false; }
        }
    }
}
