using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Nachpflege des Kesselkatalogs aus VDI 3805 Blatt 3</b> (Konzept Kesselkennlinie,
    /// Etappe E1; Anwenderentscheide F2 und F3 vom 29.09.2026): Ein Werkzeugweg, der die Sätze von
    /// <c>Tab_Heizkessel_STAMM</c> mit den Herstellerdateien abgleicht und dort η₃₀, η₁₀₀ (aus Satz
    /// 710.01), die kleinste Leistung und die Brennwertkennzeichnung nachträgt.
    /// </summary>
    /// <remarks>
    /// <para><b>Nur der Katalog, nie eine Projektkopie.</b> Geschrieben wird ausschließlich
    /// <c>Tab_Heizkessel_STAMM</c>; <c>Tab_Heizkessel</c> (die Kopien der Projekte, aus denen der
    /// Lauf rechnet) bleibt unberührt — deshalb ändert die Nachpflege kein Rechenergebnis eines
    /// bestehenden Projekts. Erst ein Kessel, der danach neu ins Projekt kommt, trägt die Werte.</para>
    /// <para><b>Zuordnung.</b> Ein Katalogsatz gehört zu einem Dateisatz, wenn der Bezeichner
    /// gleich ist (ohne Groß-/Kleinschreibung und Leerzeichen; ein Zeichen außerhalb von ASCII
    /// gleicht jedes andere — der Bestand führt Umlaute teils als Ersatzzeichen) und die
    /// thermische Leistung auf 0,05 kW übereinstimmt. Führen mehrere Dateien denselben Satz mit
    /// verschiedenen Werten, gilt die jüngste Datei (Datum des Satzes 010); bleibt es danach
    /// uneindeutig, wird der Satz benannt übergangen.</para>
    /// <para><b>Was sich ändert.</b> η₃₀ und kleinste Leistung, wo die Datei sie führt; η₁₀₀ im
    /// Wirkungsgradfeld des Brennstoffs (Öl: <c>Wirkungsgrad_Öl</c>, sonst <c>Wirkungsgrad_Gas</c>),
    /// wo Satz 710.01 einen Nennlastwert führt (F3; der Rückfall auf Satz 700 bleibt, wie er ist);
    /// <c>Brennwert</c> wird gesetzt, wenn die Bauart es sagt — nie gelöscht. Die
    /// Brennwertkennlinie, Anfahrverlust und Mindestlaufzeit bleiben unberührt. Ein zweiter Lauf
    /// ändert nichts (<b>wiederholbar</b>).</para>
    /// </remarks>
    public static class KesselkatalogNachpflege
    {
        /// <summary>Toleranz der Leistungszuordnung [kW].</summary>
        public const double LEISTUNG_TOLERANZ_KW = 0.05;

        /// <summary>
        /// Die Schreibanweisung je Spalte — fertige SQL-Texte mit <c>?</c>-Parametern, keine
        /// zusammengesetzten. Nur diese fünf Spalten schreibt die Nachpflege.
        /// </summary>
        private static readonly Dictionary<string, string> SCHREIBEN = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Brennwert"] = "UPDATE Tab_Heizkessel_STAMM SET Brennwert = ? WHERE ID = ?",
            ["Wirkungsgrad_Gas"] = "UPDATE Tab_Heizkessel_STAMM SET Wirkungsgrad_Gas = ? WHERE ID = ?",
            ["Wirkungsgrad_Öl"] = "UPDATE Tab_Heizkessel_STAMM SET Wirkungsgrad_Öl = ? WHERE ID = ?",
            [KesselKennlinieSchema.SPALTE_TEILLAST30] =
                "UPDATE Tab_Heizkessel_STAMM SET Wirkungsgrad_Teillast30 = ? WHERE ID = ?",
            [KesselKennlinieSchema.SPALTE_MINDESTLEISTUNG] =
                "UPDATE Tab_Heizkessel_STAMM SET Mindestleistung = ? WHERE ID = ?"
        };

        /// <summary>Ein Kesselsatz einer Herstellerdatei, fertig abgebildet.</summary>
        public sealed class Dateisatz
        {
            /// <summary>Datei (bei einem Archiv „archiv.zip!eintrag.vdi").</summary>
            public string Quelle { get; internal set; }

            /// <summary>Datum des Satzes 010 (jjjjmmtt) — die jüngere Datei geht vor.</summary>
            public string Datum { get; internal set; }

            /// <summary>Der gelesene Satz.</summary>
            public Attrribute_hk Satz { get; internal set; }

            /// <summary>Das Katalogmodell nach dem Importweg (<see cref="HeizkesselImportSatz.NachModell"/>).</summary>
            public HeizkesselModel Modell { get; internal set; }

            /// <summary>Führt Satz 710.01 einen Nennlastwert (F3)? Sonst stammt η₁₀₀ aus Satz 700.</summary>
            public bool NennlastAus710 { get; internal set; }
        }

        /// <summary>Eine geänderte Zelle.</summary>
        public sealed class Aenderung
        {
            public int Id { get; internal set; }
            public string Bezeichner { get; internal set; }
            public string Spalte { get; internal set; }
            public object Alt { get; internal set; }
            public object Neu { get; internal set; }

            public override string ToString()
                => Id.ToString(CultureInfo.InvariantCulture) + " " + Bezeichner + ": " + Spalte + " " +
                   Text(Alt) + " -> " + Text(Neu);
        }

        /// <summary>Was ein Lauf ergeben hat.</summary>
        public sealed class Ergebnis
        {
            /// <summary>Gelesene Dateien (ohne LFS-Zeiger).</summary>
            public int Dateien { get; internal set; }

            /// <summary>Dateien, die nur Git-LFS-Zeiger waren.</summary>
            public List<string> Zeiger { get; } = new List<string>();

            /// <summary>Kesselsätze aller Dateien.</summary>
            public int Dateisaetze { get; internal set; }

            /// <summary>Katalogsätze, die geprüft wurden.</summary>
            public int Katalogsaetze { get; internal set; }

            /// <summary>Katalogsätze mit eindeutigem Dateisatz.</summary>
            public int Zugeordnet { get; internal set; }

            /// <summary>Katalogsätze, an denen sich mindestens eine Zelle geändert hat.</summary>
            public int Nachgepflegt { get; internal set; }

            /// <summary>Katalogsätze, deren <c>Brennwert</c> von 0 auf 1 ging.</summary>
            public int BrennwertGesetzt { get; internal set; }

            /// <summary>Katalogsätze, die danach als Brennwertkessel gekennzeichnet sind und zugeordnet waren.</summary>
            public int Brennwertgeraete { get; internal set; }

            /// <summary>Katalogsätze, deren η₃₀ gesetzt oder geändert wurde.</summary>
            public int Teillast30Gesetzt { get; internal set; }

            /// <summary>Katalogsätze, deren η₁₀₀ geändert wurde.</summary>
            public int NennlastGeaendert { get; internal set; }

            /// <summary>Katalogsätze, deren kleinste Leistung gesetzt oder geändert wurde.</summary>
            public int MindestleistungGesetzt { get; internal set; }

            /// <summary>Katalogsätze ohne Dateisatz.</summary>
            public List<string> OhneTreffer { get; } = new List<string>();

            /// <summary>Katalogsätze mit mehreren widersprüchlichen Dateisätzen.</summary>
            public List<string> Mehrdeutig { get; } = new List<string>();

            /// <summary>Hinweise: Katalog sagt Brennwert, die Datei nicht (bleibt, wie er ist).</summary>
            public List<string> Hinweise { get; } = new List<string>();

            /// <summary>Jede geänderte Zelle.</summary>
            public List<Aenderung> Aenderungen { get; } = new List<Aenderung>();
        }

        // =====================================================================
        //  Lesen
        // =====================================================================

        /// <summary>
        /// Liest alle Kesseldateien eines Ordners samt Unterordnern: <c>*.vdi</c> (jede
        /// Schreibweise) und die <c>*.vdi</c>-Einträge von <c>*.zip</c>-Archiven. Ein Git-LFS-Zeiger
        /// wird übergangen und im Ergebnis genannt.
        /// </summary>
        public static List<Dateisatz> Lesen(string ordner, Ergebnis ergebnis, int brennstoffDeckel)
        {
            var liste = new List<Dateisatz>();
            if (string.IsNullOrEmpty(ordner) || !Directory.Exists(ordner)) return liste;

            foreach (string pfad in Directory.GetFiles(ordner, "*", SearchOption.AllDirectories)
                                             .OrderBy(p => p, StringComparer.Ordinal))
            {
                if (pfad.EndsWith(".vdi", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] roh = File.ReadAllBytes(pfad);
                    if (IstLfsZeiger(roh)) { ergebnis?.Zeiger.Add(pfad); continue; }
                    Einlesen(pfad, roh, liste, brennstoffDeckel);
                    if (ergebnis != null) ergebnis.Dateien++;
                }
                else if (pfad.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] kopf = new byte[24];
                    using (FileStream s = File.OpenRead(pfad))
                    {
                        int n = s.Read(kopf, 0, kopf.Length);
                        if (n == kopf.Length && IstLfsZeiger(kopf)) { ergebnis?.Zeiger.Add(pfad); continue; }
                    }
                    using (ZipArchive archiv = ZipFile.OpenRead(pfad))
                    {
                        foreach (ZipArchiveEntry e in archiv.Entries.OrderBy(x => x.FullName, StringComparer.Ordinal))
                        {
                            if (!e.FullName.EndsWith(".vdi", StringComparison.OrdinalIgnoreCase)) continue;
                            using (Stream s = e.Open())
                            using (var m = new MemoryStream())
                            {
                                s.CopyTo(m);
                                Einlesen(pfad + "!" + e.FullName, m.ToArray(), liste, brennstoffDeckel);
                                if (ergebnis != null) ergebnis.Dateien++;
                            }
                        }
                    }
                }
            }
            if (ergebnis != null) ergebnis.Dateisaetze = liste.Count;
            return liste;
        }

        private static void Einlesen(string quelle, byte[] roh, List<Dateisatz> liste, int deckel)
        {
            string text = AnsiEncoding.Get().GetString(roh);
            string datum = "";
            foreach (string zeile in text.Split('\n'))
            {
                string[] f = zeile.TrimEnd('\r').Split(';');
                if (f.Length > 4 && f[0] == "010") { datum = f[4].Trim(); break; }
            }

            var import = new HeizkesselImport();
            import.ImportText(text);
            foreach (Attrribute_hk a in import._list)
            {
                liste.Add(new Dateisatz
                {
                    Quelle = quelle,
                    Datum = datum,
                    Satz = a,
                    Modell = new HeizkesselImportSatz(a).NachModell(a.m_szName, deckel),
                    NennlastAus710 = a.m_bNennlastAus710
                });
            }
        }

        private static bool IstLfsZeiger(byte[] roh)
        {
            const string kennung = "version https://git-lfs";
            return roh != null && roh.Length >= kennung.Length &&
                   Encoding.ASCII.GetString(roh, 0, kennung.Length) == kennung;
        }

        // =====================================================================
        //  Abgleich und Schreiben
        // =====================================================================

        /// <summary>
        /// Gleicht den Katalog der geöffneten Datenbank (<c>DataRepository</c>) mit den Dateisätzen
        /// ab und schreibt die Nachpflege — mit <paramref name="trocken"/> nur den Plan.
        /// </summary>
        /// <param name="saetze">Die Dateisätze aus <see cref="Lesen"/>.</param>
        /// <param name="ergebnis">Nimmt Zählungen und Änderungen auf.</param>
        /// <param name="trocken">Nur planen, nichts schreiben.</param>
        /// <param name="nurAuslieferung">Nur Sätze mit <c>ReadOnly</c> = 1 (der Auslieferungskatalog).</param>
        public static Ergebnis Nachpflegen(IReadOnlyList<Dateisatz> saetze, Ergebnis ergebnis, bool trocken,
                                           bool nurAuslieferung = false)
        {
            ergebnis = ergebnis ?? new Ergebnis();
            var jeName = new Dictionary<string, List<Dateisatz>>(StringComparer.Ordinal);
            foreach (Dateisatz d in saetze ?? new List<Dateisatz>())
            {
                string k = Schluessel(d.Satz.m_szName);
                if (!jeName.TryGetValue(k, out List<Dateisatz> l)) jeName[k] = l = new List<Dateisatz>();
                l.Add(d);
            }

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Ptherm, Brennstoff, Wirkungsgrad_Gas, Wirkungsgrad_Öl, Brennwert, " +
                "Wirkungsgrad_Teillast30, Mindestleistung, ReadOnly FROM Tab_Heizkessel_STAMM ORDER BY ID");
            if (dt == null) return ergebnis;

            using (DbVorgang v = trocken ? null : DataRepository.Vorgang())
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (nurAuslieferung && !Wahr(r["ReadOnly"])) continue;
                    ergebnis.Katalogsaetze++;

                    int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                    string name = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "";
                    double ptherm = Zahl(r["Ptherm"]) ?? 0;

                    Dateisatz treffer = Zuordnen(jeName, name, ptherm, out string grund);
                    if (treffer == null)
                    {
                        if (grund == "mehrdeutig") ergebnis.Mehrdeutig.Add(id + " " + name);
                        else ergebnis.OhneTreffer.Add(id + " " + name);
                        continue;
                    }
                    ergebnis.Zugeordnet++;

                    var aenderungen = new List<Aenderung>();
                    HeizkesselModel m = treffer.Modell;

                    // eta30 und kleinste Leistung: wo die Datei sie fuehrt.
                    Vormerken(aenderungen, id, name, KesselKennlinieSchema.SPALTE_TEILLAST30,
                              Zahl(r[KesselKennlinieSchema.SPALTE_TEILLAST30]), m.Wirkungsgrad_Teillast30);
                    Vormerken(aenderungen, id, name, KesselKennlinieSchema.SPALTE_MINDESTLEISTUNG,
                              Zahl(r[KesselKennlinieSchema.SPALTE_MINDESTLEISTUNG]), m.Mindestleistung);

                    // eta100 (F3): nur ein Nennlastwert aus Satz 710.01, im Feld des Brennstoffs.
                    if (treffer.NennlastAus710)
                    {
                        double? eta100 = HeizkesselImportSatz.Nennlast(treffer.Satz.m_szWirkungsgrad);
                        int brennstoff = Convert.ToInt32(Zahl(r["Brennstoff"]) ?? 0);
                        string feld = IstOel(brennstoff) ? "Wirkungsgrad_Öl" : "Wirkungsgrad_Gas";
                        if (eta100.HasValue && eta100.Value > 0)
                            Vormerken(aenderungen, id, name, feld, Zahl(r[feld]), eta100);
                    }

                    // Brennwert: nur setzen, nie loeschen.
                    bool brennwertAlt = Wahr(r["Brennwert"]);
                    if (m.Brennwert && !brennwertAlt)
                        aenderungen.Add(new Aenderung { Id = id, Bezeichner = name, Spalte = "Brennwert", Alt = 0L, Neu = 1L });
                    else if (!m.Brennwert && brennwertAlt)
                        ergebnis.Hinweise.Add(id + " " + name + ": Katalog Brennwert = 1, Bauart der Datei „" +
                                              treffer.Satz.m_szBauart + "“ - bleibt 1.");
                    if (m.Brennwert || brennwertAlt) ergebnis.Brennwertgeraete++;

                    if (aenderungen.Count == 0) continue;
                    ergebnis.Nachgepflegt++;
                    foreach (Aenderung a in aenderungen)
                    {
                        ergebnis.Aenderungen.Add(a);
                        if (a.Spalte == "Brennwert") ergebnis.BrennwertGesetzt++;
                        else if (a.Spalte == KesselKennlinieSchema.SPALTE_TEILLAST30) ergebnis.Teillast30Gesetzt++;
                        else if (a.Spalte == KesselKennlinieSchema.SPALTE_MINDESTLEISTUNG) ergebnis.MindestleistungGesetzt++;
                        else ergebnis.NennlastGeaendert++;

                        if (v == null) continue;
                        // Eine Zelle je Anweisung - je Spalte ein fertiger SQL-Text (SCHREIBEN).
                        v.Ausfuehren(SCHREIBEN[a.Spalte], new DbParam("?", a.Neu), new DbParam("?", id));
                    }
                }
                v?.Commit();
            }
            return ergebnis;
        }

        /// <summary>Die Berichtszeilen eines Laufs — Zählungen, dann jede Änderung und jede Lücke.</summary>
        public static IEnumerable<string> Bericht(Ergebnis e, bool trocken)
        {
            yield return "Kesselkatalog aus VDI 3805 Blatt 3" + (trocken ? " (trocken, nichts geschrieben)" : "") + ":";
            yield return "  Dateien " + e.Dateien + (e.Zeiger.Count > 0 ? " (dazu " + e.Zeiger.Count + " Git-LFS-Zeiger uebergangen)" : "") +
                         ", Dateisaetze " + e.Dateisaetze + ", Katalogsaetze " + e.Katalogsaetze +
                         ", zugeordnet " + e.Zugeordnet + ".";
            yield return "  nachgepflegt " + e.Nachgepflegt + " Katalogsaetze: Brennwert gesetzt " + e.BrennwertGesetzt +
                         ", eta30 " + e.Teillast30Gesetzt + ", eta100 " + e.NennlastGeaendert +
                         ", kleinste Leistung " + e.MindestleistungGesetzt + "; Brennwertgeraete danach " +
                         e.Brennwertgeraete + ".";
            foreach (Aenderung a in e.Aenderungen) yield return "    " + a;
            foreach (string s in e.OhneTreffer) yield return "  ohne Dateisatz: " + s;
            foreach (string s in e.Mehrdeutig) yield return "  mehrdeutig, uebergangen: " + s;
            foreach (string s in e.Hinweise) yield return "  Hinweis: " + s;
            foreach (string s in e.Zeiger) yield return "  Git-LFS-Zeiger: " + s;
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>
        /// Der Vergleichsschlüssel eines Bezeichners: klein, ohne Leerzeichen, jedes Zeichen außerhalb
        /// von ASCII als „?" — so trifft „raumluftabh�ngig" (Ersatzzeichen im Bestand) auf „raumluftabhängig".
        /// </summary>
        internal static string Schluessel(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in (name ?? "").ToLowerInvariant())
            {
                if (char.IsWhiteSpace(c)) continue;
                sb.Append(c < 128 ? c : '?');
            }
            return sb.ToString();
        }

        private static Dateisatz Zuordnen(Dictionary<string, List<Dateisatz>> jeName, string name, double ptherm,
                                          out string grund)
        {
            grund = "ohne";
            if (!jeName.TryGetValue(Schluessel(name), out List<Dateisatz> kandidaten)) return null;
            List<Dateisatz> passend = kandidaten
                .Where(d => Math.Abs(d.Modell.Ptherm - ptherm) <= LEISTUNG_TOLERANZ_KW)
                .ToList();
            if (passend.Count == 0) return null;

            // Die juengste Datei zuerst; widersprechen sich Saetze derselben Datei, bleibt es offen.
            string juengstes = passend.Max(d => d.Datum ?? "");
            List<Dateisatz> neueste = passend.Where(d => (d.Datum ?? "") == juengstes).ToList();
            if (neueste.Select(Fingerabdruck).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                grund = "mehrdeutig";
                return null;
            }
            return neueste[0];
        }

        /// <summary>Was die Nachpflege von einem Dateisatz nimmt — gleich = derselbe Satz.</summary>
        private static string Fingerabdruck(Dateisatz d)
            => string.Join("|", d.Satz.m_szWirkungsgrad, d.Satz.m_szWirkungsgrad30, d.Satz.m_szMindestleistung,
                           d.Modell.Brennwert ? "1" : "0", d.NennlastAus710 ? "1" : "0");

        private static void Vormerken(List<Aenderung> liste, int id, string name, string spalte, double? alt, double? neu)
        {
            if (!neu.HasValue) return;                                   // die Datei fuehrt keinen Wert
            if (alt.HasValue && Math.Abs(alt.Value - neu.Value) < 1e-12) return;
            liste.Add(new Aenderung { Id = id, Bezeichner = name, Spalte = spalte, Alt = alt, Neu = neu.Value });
        }

        private static bool IstOel(int brennstoff)
            => (brennstoff >= 6 && brennstoff <= 9) || (brennstoff >= 18 && brennstoff <= 22);

        private static double? Zahl(object v)
        {
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        private static bool Wahr(object v)
            => v != null && v != DBNull.Value && Math.Abs(Convert.ToDouble(v, CultureInfo.InvariantCulture)) > 0.5;

        private static string Text(object v)
        {
            if (v == null || v == DBNull.Value) return "leer";
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }
    }
}
