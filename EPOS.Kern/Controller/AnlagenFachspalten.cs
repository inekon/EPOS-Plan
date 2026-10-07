using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die FACHSPALTEN von <c>Tab_Energieanlagen</c> und der EINE Weg, sie von einer
    /// bestehenden Anlagenzeile auf eine neu angelegte zu übertragen.
    ///
    /// <para><b>Was eine Fachspalte ist.</b> Jede Spalte, die die Tabelle JETZT führt und
    /// die Einfügeanweisung <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nicht nennt, ohne
    /// <c>ID</c> — das Komplement des Modells. Keine zweite Liste: Wer eine Spalte ins
    /// Modell aufnimmt, verkleinert die Menge von selbst; wer per Schemaschritt eine Spalte
    /// anlegt, ohne das Modell zu erweitern (Sondenfeld, KWKG, Steuer, Quellangaben), ist
    /// von selbst geschützt. Dieselbe Menge rettet der Speicherweg des Assistenten
    /// (<c>WizardCtrl.Fachspalten</c> leitet hierher weiter).</para>
    ///
    /// <para><b>Was nie übertragen wird.</b> Schlüssel und Projektbezug (<c>ID</c>,
    /// <c>ID_Projekt</c>) und die Ergebnisspalten (<see cref="ERGEBNIS"/>): Werte, die die
    /// Simulation schreibt, gehören zum Lauf des Quellprojekts, nicht zur Anlage. Die
    /// Tabelle führt gegenwärtig keine — die Ergebnisse stehen in eigenen Tabellen —, die
    /// Menge hält den Platz, und die Wache <c>FachspaltenEinordnungWacheTests</c> verlangt
    /// für jede neue Spalte eine bewusste Einordnung.</para>
    ///
    /// <para><b>Verweise auf projekteigene Zeilen</b> (<see cref="PROJEKTBEZUG"/>):
    /// Innerhalb eines Projekts unverändert; über Projektgrenzen auf die gleichnamige
    /// Zeile des Zielprojekts abgebildet (Muster <c>KomponentenUebernahmeCtrl</c> bei den
    /// Pufferverweisen), sonst als Projektkopie samt Kindzeilen ins Ziel übernommen — nie ein Verweis in ein fremdes Projekt.</para>
    /// </summary>
    internal static class AnlagenFachspalten
    {
        private const string TABELLE = "Tab_Energieanlagen";

        /// <summary>Schlüssel und Projektbezug — nie übertragen.</summary>
        public static readonly HashSet<string> AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID", "ID_Projekt" };

        /// <summary>
        /// Ergebnisspalten der Simulation — nie übertragen. Gegenwärtig leer: Die Simulation
        /// schreibt keine Spalte von <c>Tab_Energieanlagen</c>.
        /// </summary>
        public static readonly HashSet<string> ERGEBNIS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Fachspalten, die auf eine PROJEKTEIGENE Zeile zeigen (Spalte → Tabelle mit
        /// <c>ID_Projekt</c> und <c>Bezeichner</c>).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> PROJEKTBEZUG =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "WQ_ID_Quellprofil", "Tab_Quellprofil" },
                { KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE, KaeltemaschineSchema.TAB_PROJEKT }
            };

        /// <summary>Die Spalten, die <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nennt - einmal aus der Anweisung gelesen.</summary>
        private static HashSet<string> m_InsertSpalten;

        /// <summary>Die Modellspalten: die Spalten der vollständigen Einfügeanweisung.</summary>
        public static HashSet<string> Modellspalten()
        {
            if (m_InsertSpalten != null) return m_InsertSpalten;

            string sql = AnlagenSql.SQL_ANLAGE_INSERT;
            HashSet<string> menge = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int auf = sql.IndexOf('(');
            int zu = auf >= 0 ? sql.IndexOf(')', auf) : -1;
            if (auf >= 0 && zu > auf)
            {
                foreach (string s in sql.Substring(auf + 1, zu - auf - 1).Split(','))
                {
                    string name = s.Trim();
                    if (name.Length > 0) menge.Add(name);
                }
            }
            m_InsertSpalten = menge;
            return menge;
        }

        /// <summary>
        /// Die Fachspalten: alle Spalten von <c>Tab_Energieanlagen</c>, die
        /// <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nicht nennt, ohne <c>ID</c> - in
        /// Schemareihenfolge. Leer, wenn die Tabelle nur die Modellspalten führt.
        /// </summary>
        public static List<string> Fachspalten()
        {
            List<string> fach = new List<string>();
            HashSet<string> insert = Modellspalten();
            foreach (string spalte in DataRepository.SpaltenVonTabelle(TABELLE))
            {
                if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                if (insert.Contains(spalte)) continue;
                fach.Add(spalte);
            }
            return fach;
        }

        /// <summary>
        /// Die Fachspalten, die eine Übernahme von einer Quellzeile überträgt:
        /// <see cref="Fachspalten"/> ohne <see cref="AUSSCHLUSS"/> und <see cref="ERGEBNIS"/>.
        /// Liest das Schema — deshalb VOR einem offenen Vorgang erfragen.
        /// </summary>
        public static List<string> UebertragbareSpalten()
        {
            List<string> liste = new List<string>();
            foreach (string spalte in Fachspalten())
                if (!AUSSCHLUSS.Contains(spalte) && !ERGEBNIS.Contains(spalte))
                    liste.Add(spalte);
            return liste;
        }

        /// <summary>
        /// Die Kindtabellen einer projekteigenen Verweistabelle aus <see cref="PROJEKTBEZUG"/>
        /// (Tabelle → Kindtabelle mit Fremdschlüssel), die eine Projektkopie mitnimmt.
        /// Ergebnistabellen (<c>Tab_ErgebnisKaeltemaschine</c>) gehören zum Lauf und stehen in
        /// <see cref="KIND_AUSSCHLUSS"/>.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (string Tabelle, string Fk)[]> PROJEKTKINDER =
            new Dictionary<string, (string Tabelle, string Fk)[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Tab_Quellprofil", new[] { ("Tab_QuellprofilDaten", "ID_Quellprofil") } },
                { KaeltemaschineSchema.TAB_PROJEKT, new[] { ("Tab_Kenndaten_Kaeltemaschine",
                                                             KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE) } }
            };

        /// <summary>Tabellen, die auf eine Verweistabelle zeigen, aber nie mitkopiert werden (Ergebnisse).</summary>
        public static readonly HashSet<string> KIND_AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Tab_Energieanlagen", "Tab_ErgebnisKaeltemaschine" };

        /// <summary>
        /// Die Geräteverweise einer Anlagenzeile: die Fremdschlüssel auf die Projektkopie des
        /// Geräts. Eine KOPIE der Zeile (<see cref="KopieSpalten"/>) lässt den Verweis aus,
        /// den sie selbst neu setzt.
        /// </summary>
        public static readonly HashSet<string> GERAETEVERWEISE =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "ID_WP", "ID_SP", "ID_PV", "ID_Solar", "ID_Kessel", "ID_BHKW", "ID_PUFFER",
              KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE };

        /// <summary>
        /// Was eine vollständige KOPIE der Zeile außer <see cref="AUSSCHLUSS"/> und
        /// <see cref="ERGEBNIS"/> nie überträgt: den Bezeichner (die Kopie trägt ihren eigenen).
        /// Dazu kommt der Geräteverweis, den der Aufrufer neu setzt.
        /// </summary>
        public static readonly HashSet<string> KOPIE_AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Bezeichner" };

        /// <summary>
        /// Die Spalten einer vollständigen KOPIE der Anlagenzeile (Anwenderentscheid
        /// 07.10.2026, weitere Stücke der Flottenstudie): JEDE Spalte des Schemas — Modell-
        /// wie Fachspalten — ohne <see cref="AUSSCHLUSS"/>, <see cref="ERGEBNIS"/>,
        /// <see cref="KOPIE_AUSSCHLUSS"/> und den Geräteverweis <paramref name="geraeteverweis"/>.
        /// Liest das Schema — deshalb VOR einem offenen Vorgang erfragen.
        /// </summary>
        public static List<string> KopieSpalten(string geraeteverweis)
        {
            if (!GERAETEVERWEISE.Contains(geraeteverweis ?? ""))
                throw new ArgumentException("Kein Geräteverweis: " + geraeteverweis, nameof(geraeteverweis));

            List<string> liste = new List<string>();
            foreach (string spalte in DataRepository.SpaltenVonTabelle(TABELLE))
            {
                if (AUSSCHLUSS.Contains(spalte) || ERGEBNIS.Contains(spalte) || KOPIE_AUSSCHLUSS.Contains(spalte))
                    continue;
                if (string.Equals(spalte, geraeteverweis, StringComparison.OrdinalIgnoreCase)) continue;
                liste.Add(spalte);
            }
            return liste;
        }

        /// <summary>Ausgang einer Übertragung: Projektverweise, die ins Ziel kopiert wurden bzw. leer blieben.</summary>
        public sealed class Ausgang
        {
            /// <summary>Projekteigene Zeilen (Quellprofil, Kältemaschine), die als Projektkopie ins Ziel kamen.</summary>
            public int Kopiert;
            /// <summary>Projektverweise ohne Entsprechung im Ziel, die leer bleiben.</summary>
            public int Verloren;
        }

        /// <summary>
        /// Überträgt die Spalten <paramref name="spalten"/> der Anlagenzeile
        /// <paramref name="idQuelle"/> auf die eben angelegte Zeile <paramref name="idZiel"/> —
        /// EIN UPDATE mit Zeilenwert-Zuweisung aus der Quellzeile, NULL eingeschlossen (sonst
        /// trüge die neue Zeile die Vorgabe ihrer Spalte statt des Quellwerts).
        ///
        /// <para>Verweise aus <see cref="PROJEKTBEZUG"/>: im selben Projekt unverändert;
        /// über Projektgrenzen auf die gleichnamige Zeile des Zielprojekts (kleinste ID bei
        /// Namensdoppeln); fehlt sie, wird die Quellzeile samt <see cref="PROJEKTKINDER"/> als
        /// Projektkopie ins Ziel übernommen (Anwenderentscheid 07.10.2026). Eine zweite Anlage
        /// mit demselben Verweis findet danach die Kopie als gleichnamige Zeile.</para>
        /// </summary>
        /// <param name="v">Der offene Vorgang der Übernahme — die neue Zeile ist nur dort sichtbar.</param>
        /// <param name="spalten">Aus <see cref="UebertragbareSpalten"/> oder <see cref="KopieSpalten"/>,
        /// vor dem Vorgang erfragt. Spaltennamen stammen aus dem Schema, nie aus einer Eingabe.</param>
        public static Ausgang Uebertragen(DbVorgang v, IReadOnlyList<string> spalten, int idQuelle, int idZiel)
        {
            var ausgang = new Ausgang();
            if (v == null || spalten == null || spalten.Count == 0 || idQuelle <= 0 || idZiel <= 0 ||
                idQuelle == idZiel)
                return ausgang;

            DataTable projekte = v.Lese("SELECT q.ID_Projekt AS PQ, z.ID_Projekt AS PZ FROM " + TABELLE + " q, " +
                                        TABELLE + " z WHERE q.ID = ? AND z.ID = ?",
                                        new DbParam("@q", idQuelle), new DbParam("@z", idZiel));
            if (projekte == null || projekte.Rows.Count == 0 || projekte.Rows[0]["PZ"] == DBNull.Value)
                return ausgang;
            int projektZiel = Convert.ToInt32(projekte.Rows[0]["PZ"], CultureInfo.InvariantCulture);
            bool gleichesProjekt = projekte.Rows[0]["PQ"] != DBNull.Value &&
                Convert.ToInt32(projekte.Rows[0]["PQ"], CultureInfo.InvariantCulture) == projektZiel;

            var direkt = new List<string>();
            var bezuege = new List<string>();
            foreach (string spalte in spalten)
            {
                if (AUSSCHLUSS.Contains(spalte) || ERGEBNIS.Contains(spalte)) continue;
                if (!gleichesProjekt && PROJEKTBEZUG.ContainsKey(spalte)) bezuege.Add(spalte);
                else direkt.Add(spalte);
            }

            if (direkt.Count > 0)
            {
                var ziel = new List<string>();
                var quelle = new List<string>();
                foreach (string spalte in direkt)
                {
                    ziel.Add("[" + spalte + "]");
                    quelle.Add("q.[" + spalte + "]");
                }
                v.Ausfuehren("UPDATE " + TABELLE + " SET (" + string.Join(", ", ziel) + ") = (SELECT " +
                             string.Join(", ", quelle) + " FROM " + TABELLE + " q WHERE q.ID = ?) WHERE ID = ?",
                             new DbParam("@q", idQuelle), new DbParam("@z", idZiel));
            }

            foreach (string spalte in bezuege)
            {
                string tabelle = PROJEKTBEZUG[spalte];
                object wert = v.Skalar("SELECT [" + spalte + "] FROM " + TABELLE + " WHERE ID = ?",
                                       new DbParam("@q", idQuelle));
                object neu = DBNull.Value;
                if (wert != null)
                {
                    int idVerweis = Convert.ToInt32(wert, CultureInfo.InvariantCulture);
                    object namensgleich = v.Skalar(
                        "SELECT pz.ID FROM [" + tabelle + "] pz JOIN [" + tabelle + "] pq " +
                        "ON pq.Bezeichner = pz.Bezeichner WHERE pq.ID = ? AND pz.ID_Projekt = ? " +
                        "ORDER BY pz.ID LIMIT 1",
                        new DbParam("@v", idVerweis), new DbParam("@p", projektZiel));
                    if (namensgleich != null)
                    {
                        neu = namensgleich;
                    }
                    else
                    {
                        int kopie = ProjektzeileKopieren(v, tabelle, idVerweis, projektZiel);
                        if (kopie > 0) { neu = kopie; ausgang.Kopiert++; }
                        else ausgang.Verloren++;
                    }
                }
                v.Ausfuehren("UPDATE " + TABELLE + " SET [" + spalte + "] = ? WHERE ID = ?",
                             new DbParam("@w", neu), new DbParam("@z", idZiel));
            }
            return ausgang;
        }

        /// <summary>
        /// Kopiert die projekteigene Zeile <paramref name="id"/> von <paramref name="tabelle"/>
        /// samt ihren <see cref="PROJEKTKINDER"/> in das Projekt <paramref name="projektZiel"/> —
        /// ganze Zeile außer <c>ID</c>, <c>ID_Projekt</c> auf das Ziel, Kindzeilen mit neuem
        /// Fremdschlüssel. Spalten aus dem Schema (im Vorgang gelesen), Werte über Parameter.
        /// </summary>
        /// <returns>Die ID der Kopie; 0, wenn die Quellzeile fehlt.</returns>
        private static int ProjektzeileKopieren(DbVorgang v, string tabelle, int id, int projektZiel)
        {
            List<string> spalten = SpaltenImVorgang(v, tabelle);
            var ziel = new List<string>();
            var quelle = new List<string>();
            var ps = new List<DbParam>();
            foreach (string spalte in spalten)
            {
                if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                ziel.Add("[" + spalte + "]");
                if (string.Equals(spalte, "ID_Projekt", StringComparison.OrdinalIgnoreCase))
                {
                    quelle.Add("?");
                    ps.Add(new DbParam("@p", projektZiel));
                }
                else quelle.Add("[" + spalte + "]");
            }
            ps.Add(new DbParam("@id", id));
            if (v.Ausfuehren("INSERT INTO [" + tabelle + "] (" + string.Join(", ", ziel) + ") SELECT " +
                             string.Join(", ", quelle) + " FROM [" + tabelle + "] WHERE ID = ?", ps.ToArray()) != 1)
                return 0;
            int neu = Convert.ToInt32(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);

            if (PROJEKTKINDER.TryGetValue(tabelle, out (string Tabelle, string Fk)[] kinder))
            {
                foreach ((string kind, string fk) in kinder)
                {
                    var kz = new List<string>();
                    var kq = new List<string>();
                    var kp = new List<DbParam>();
                    foreach (string spalte in SpaltenImVorgang(v, kind))
                    {
                        if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                        kz.Add("[" + spalte + "]");
                        if (string.Equals(spalte, fk, StringComparison.OrdinalIgnoreCase))
                        { kq.Add("?"); kp.Add(new DbParam("@fk", neu)); }
                        else if (string.Equals(spalte, "ID_Projekt", StringComparison.OrdinalIgnoreCase))
                        { kq.Add("?"); kp.Add(new DbParam("@p", projektZiel)); }
                        else kq.Add("[" + spalte + "]");
                    }
                    kp.Add(new DbParam("@alt", id));
                    v.Ausfuehren("INSERT INTO [" + kind + "] (" + string.Join(", ", kz) + ") SELECT " +
                                 string.Join(", ", kq) + " FROM [" + kind + "] WHERE [" + fk + "] = ? ORDER BY ID",
                                 kp.ToArray());
                }
            }
            return neu;
        }

        /// <summary>
        /// Die KINDTABELLEN einer Anlagenzeile, die eine vollständige Kopie der Anlage
        /// mitnimmt (Anwenderentscheid 07.10.2026, weitere Stücke der Flottenstudie), in
        /// Kopierreihenfolge: Tabelle → Spalte des Verweises auf <c>Tab_Energieanlagen</c>.
        /// <list type="bullet">
        /// <item><c>Tab_StromspeicherVariante</c> — Betriebsführung des Stromspeichers;</item>
        /// <item><c>Z_AnlageSenke</c> — Senken samt Lade-Prioritäten. Den Puffer TEILT die Kopie
        /// mit der Quelle (mehrere Erzeuger an einem Puffer sind die Regel);</item>
        /// <item><c>Z_AnlagePufferVerbund</c> — Parallelverbund; <c>ID_Senke</c> wird auf die
        /// kopierte Senke umgeschlüsselt (<see cref="ANLAGENKIND_UMSCHLUESSEL"/>), deshalb NACH den Senken;</item>
        /// <item><c>Z_AnlageStrang</c> — Stränge; sie gehören der Anlage (Gruppierung je Anlage,
        /// Wechselrichter, Gerätenummer), die Kopie bekommt eigene Stränge mit denselben Namen;</item>
        /// <item><c>Tab_Sperrfenster</c> — Sperrprofil.</item>
        /// </list>
        /// Verweise der Kindzeilen auf andere projekteigene Zeilen (Puffer, Wechselrichter,
        /// PV-Modul) bleiben gleich — die Kopie liegt im selben Projekt.
        /// </summary>
        public static readonly (string Tabelle, string Fk)[] ANLAGENKINDER =
        {
            ("Tab_StromspeicherVariante", "ID_Energieanlage"),
            ("Z_AnlageSenke", "ID_Anlage"),
            ("Z_AnlagePufferVerbund", "ID_Anlage"),
            ("Z_AnlageStrang", "ID_Anlage"),
            ("Tab_Sperrfenster", "ID_Energieanlage")
        };

        /// <summary>
        /// Verweise einer Kindzeile auf eine ANDERE Kindzeile derselben Anlage
        /// („Tabelle.Spalte“ → Kindtabelle): Sie zeigen in der Kopie auf die kopierte Zeile.
        /// Ein Wert ohne Entsprechung (fremde Anlage) bleibt unverändert.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> ANLAGENKIND_UMSCHLUESSEL =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Z_AnlagePufferVerbund.ID_Senke", "Z_AnlageSenke" }
            };

        /// <summary>
        /// Tabellen mit Verweis auf eine Anlagenzeile, die eine Kopie NICHT mitnimmt:
        /// Ergebnisse des Laufs (<c>Tab_ErgebnisErdreich</c>, <c>Tab_ErgebnisPufferspeicher</c>,
        /// <c>Tab_ErgebnisStromspeicher</c>), die gespeicherte Auslegungsstudie
        /// (<c>Tab_SpeicherAuslegung</c>, eindeutig je Projekt, Anlage und Bezeichner — die Studie
        /// gehört der vertretenen Anlage) und die Kostenpositionen (<c>Tab_ProjektWerte</c>,
        /// eigener Pflegeweg über Komponente und Gerät).
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Tab_ErgebnisErdreich", "Tab_ErgebnisPufferspeicher", "Tab_ErgebnisStromspeicher",
              "Tab_SpeicherAuslegung", "Tab_ProjektWerte" };

        /// <summary>Spaltennamen, die ohne Fremdschlüssel auf eine Anlagenzeile zeigen.</summary>
        public static readonly HashSet<string> ANLAGENVERWEIS_SPALTEN =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID_Anlage", "ID_Energieanlage" };

        /// <summary>
        /// Kopiert die <see cref="ANLAGENKINDER"/> der Anlage <paramref name="idQuelle"/> auf die
        /// eben angelegte Anlage <paramref name="idZiel"/> — je Zeile ein INSERT … SELECT mit
        /// neuer ID, Verweis auf die neue Anlage und <see cref="ANLAGENKIND_UMSCHLUESSEL"/>.
        /// Läuft im Vorgang der Übernahme; ein scheiterndes INSERT wirft und rollt mit ihr zurück.
        /// Eine Tabelle, die das Schema (noch) nicht kennt, wird übergangen.
        /// </summary>
        /// <returns>Die Zahl der kopierten Kindzeilen.</returns>
        public static int AnlagenkinderKopieren(DbVorgang v, int idQuelle, int idZiel)
        {
            if (v == null || idQuelle <= 0 || idZiel <= 0 || idQuelle == idZiel) return 0;

            int anzahl = 0;
            // Kindtabelle → (alte ID → neue ID), für die Umschlüsselung späterer Kinder.
            var zuordnung = new Dictionary<string, Dictionary<long, long>>(StringComparer.OrdinalIgnoreCase);
            foreach ((string tabelle, string fk) in ANLAGENKINDER)
            {
                List<string> spalten = SpaltenImVorgang(v, tabelle);
                if (spalten.Count == 0) continue;
                var karte = new Dictionary<long, long>();
                zuordnung[tabelle] = karte;

                DataTable quelle = v.Lese("SELECT * FROM [" + tabelle + "] WHERE [" + fk + "] = ? ORDER BY ID",
                                          new DbParam("@a", idQuelle));
                foreach (DataRow zeile in quelle.Rows)
                {
                    long alteId = Convert.ToInt64(zeile["ID"], CultureInfo.InvariantCulture);
                    var ziel = new List<string>();
                    var werte = new List<string>();
                    var ps = new List<DbParam>();
                    foreach (string spalte in spalten)
                    {
                        if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                        ziel.Add("[" + spalte + "]");
                        if (string.Equals(spalte, fk, StringComparison.OrdinalIgnoreCase))
                        {
                            werte.Add("?");
                            ps.Add(new DbParam("@fk", idZiel));
                        }
                        else if (ANLAGENKIND_UMSCHLUESSEL.TryGetValue(tabelle + "." + spalte, out string bezug))
                        {
                            object alt = zeile[spalte];
                            object neu = alt;
                            if (alt != DBNull.Value && zuordnung.TryGetValue(bezug, out Dictionary<long, long> k) &&
                                k.TryGetValue(Convert.ToInt64(alt, CultureInfo.InvariantCulture), out long n))
                                neu = n;
                            werte.Add("?");
                            ps.Add(new DbParam("@u", neu));
                        }
                        else werte.Add("[" + spalte + "]");
                    }
                    ps.Add(new DbParam("@id", alteId));
                    if (v.Ausfuehren("INSERT INTO [" + tabelle + "] (" + string.Join(", ", ziel) + ") SELECT " +
                                     string.Join(", ", werte) + " FROM [" + tabelle + "] WHERE ID = ?",
                                     ps.ToArray()) != 1)
                        throw new InvalidOperationException("Kindzeile " + tabelle + " " + alteId + " nicht kopiert.");
                    karte[alteId] = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
                    anzahl++;
                }
            }
            return anzahl;
        }

        /// <summary>Die Spalten einer Tabelle — auf der Verbindung des Vorgangs gelesen.</summary>
        private static List<string> SpaltenImVorgang(DbVorgang v, string tabelle)
        {
            var liste = new List<string>();
            DataTable dt = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            foreach (DataRow r in dt.Rows) liste.Add(Convert.ToString(r["name"], CultureInfo.InvariantCulture));
            return liste;
        }
    }
}
