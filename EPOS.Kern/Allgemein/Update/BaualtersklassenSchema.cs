using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Schemaschritt der Baualtersklassen</b> (Entscheid E47, Konzept Baualtersklassen
    /// Abschnitte 3, 5 und 6; Konzept-Nachtrag N1.52) — EINE Quelle für Migration,
    /// <c>Werkzeuge/Testdatenbankschema</c>, die Arbeitskopie der Tests und den Nachweis. Die Nummer
    /// steht allein hier (<see cref="SCHRITT"/>).
    ///
    /// <para><b>Was er tut, in EINEM Vorgang:</b></para>
    /// <list type="number">
    /// <item>die Spalte <c>Energiestandard TEXT</c> mit <c>CHECK</c> auf die elf Codes
    /// (<see cref="GebaeudeSchema.SQLITE_ENERGIESTANDARD"/>) an <c>Tab_Gebaeude</c> und
    /// <c>Tab_Gebaeude_STAMM</c>;</item>
    /// <item>die <b>Umschlüsselung</b> der gespeicherten Baualtersklasse A…U auf A…M samt
    /// Energiestandard (<see cref="Umschluesseln"/>, Tabelle des Konzepts Abschnitt 5): Ist das Baujahr
    /// gesetzt, gilt die Klasse aus dem Baujahr (<see cref="Baujahrregel"/>), sonst die Tabelle; der
    /// Standard folgt immer der Tabelle. Jede Zeile, deren Klasse sich nicht eindeutig ergibt (alt I, J
    /// und S ohne Baujahr, ein unbekannter Buchstabe), bekommt eine Protokollzeile;</item>
    /// <item>die <b>Umbenennung</b> der Auslieferungssätze (<c>ReadOnly = 1</c> in
    /// <c>Tab_Gebaeude_STAMM</c>), deren zweiter Namensteil (Trenner <c>-</c>) ihr alter Buchstabe ist,
    /// auf den neuen (<c>AltenH-C-U-252</c> → <c>AltenH-D-U-252</c>). Eigene Sätze und Projektkopien
    /// behalten ihren Namen; die Kopie hängt über <c>ID_Gebaeude_Stamm</c> am Katalog. Kollidiert ein
    /// neuer Name mit einem bestehenden (oder zwei neue miteinander), bleibt der alte, und das Protokoll
    /// nennt es;</item>
    /// <item>der <b>siebte Neubau</b> der Sicht <c>Abfrage_Projektgebaeude</c>
    /// (<see cref="GebaeudeSchema.SQL_VIEW_ENERGIESTANDARD"/>, 102 Spalten) — mit allen Spalten der sechs
    /// früheren Durchgänge; er läuft in Migration, Werkzeug und Testkopie <b>zuletzt</b>.</item>
    /// </list>
    ///
    /// <para><b>Genau einmal:</b> Umschlüsselung und Umbenennung laufen je Tabelle nur, wenn ihr die
    /// Spalte <c>Energiestandard</c> VOR dem Lauf fehlte — die Spalte ist der Marker neben der
    /// Schrittnummer. Ein zweiter Lauf legt nichts an, verschiebt keinen Buchstaben und benennt nichts
    /// um; er baut nur die Sicht neu.</para>
    ///
    /// <para><b>Ergebnisneutral:</b> Kein Rechenweg liest Klasse oder Standard (weder der VDI-6007-Weg
    /// noch der Tagesbilanz-Altweg); der Referenzlauf bleibt byte-gleich.</para>
    /// </summary>
    public static class BaualtersklassenSchema
    {
        /// <summary>
        /// Der Schritt der Baualtersklassen — vergeben unmittelbar vor dem Schemacommit gegen origin (Regel
        /// „lückenlos": der Schritt des Vorgängers + 1); die EINE Stelle, an der die Nummer steht.
        /// </summary>
        public const int SCHRITT = ZonenkopplungSchema.SCHRITT + 1;

        /// <summary>Die alten Buchstaben A…U in ihrer Reihenfolge (die 21 Klassen vor E47).</summary>
        public const string ALTE_BUCHSTABEN = "ABCDEFGHIJKLMNOPQRSTU";

        /// <summary>Der Trenner der Namensteile im Auslieferungskatalog (<c>AltenH-C-U-252</c>).</summary>
        public const char NAMENSTRENNER = '-';

        /// <summary>
        /// Die Umschlüsselungstabelle des Konzepts (Abschnitt 5): alter Buchstabe → neue Klasse und
        /// Energiestandard (<c>null</c> = keiner). I, J und S ergeben ohne Baujahr keine eindeutige Klasse
        /// (<see cref="UNKLAR"/>).
        /// </summary>
        private static readonly Dictionary<char, (char Klasse, string Standard)> TABELLE = new Dictionary<char, (char, string)>
        {
            ['A'] = ('B', null), ['B'] = ('C', null), ['C'] = ('D', null), ['D'] = ('E', null),
            ['E'] = ('F', null), ['F'] = ('G', null), ['G'] = ('H', null), ['H'] = ('I', null),
            ['I'] = ('J', Energiestandard.NIEDRIGENERGIE), ['J'] = ('J', Energiestandard.PASSIVHAUS),
            ['K'] = ('J', null), ['L'] = ('J', Energiestandard.EH70),
            ['M'] = ('K', null), ['N'] = ('K', Energiestandard.EH70), ['O'] = ('K', Energiestandard.EH55),
            ['P'] = ('K', null),
            ['Q'] = ('L', null), ['R'] = ('L', Energiestandard.EH115_100), ['S'] = ('L', null),
            ['T'] = ('M', Energiestandard.EH55), ['U'] = ('M', Energiestandard.EH40),
        };

        /// <summary>Die alten Klassen, die ohne Baujahr keine eindeutige neue Klasse ergeben.</summary>
        public const string UNKLAR = "IJS";

        /// <summary>Das Ergebnis der Umschlüsselung EINER Zeile.</summary>
        public readonly struct Umschluesselung
        {
            internal Umschluesselung(string klasse, string standard, bool eindeutig, string grund)
            {
                Klasse = klasse;
                Energiestandard = standard;
                Eindeutig = eindeutig;
                Grund = grund;
            }

            /// <summary>Die neue Klasse A…M; unverändert der alte Wert, wo es keine Regel gibt (auch leer).</summary>
            public string Klasse { get; }

            /// <summary>Der Energiestandard als Code; <c>null</c> = keiner.</summary>
            public string Energiestandard { get; }

            /// <summary><c>false</c>, wenn die Klasse sich nicht eindeutig ergibt — dann schreibt der Schritt eine Protokollzeile.</summary>
            public bool Eindeutig { get; }

            /// <summary>Der Grund einer nicht eindeutigen Klasse (für das Protokoll); leer, wenn eindeutig.</summary>
            public string Grund { get; }
        }

        /// <summary>
        /// Die Umschlüsselung EINER Zeile (Konzept Abschnitt 5): Ist <paramref name="baujahr"/> gesetzt,
        /// gilt die Klasse aus dem Baujahr, sonst die Tabelle; der Energiestandard folgt immer der Tabelle.
        /// Ohne alte Klasse und ohne Baujahr bleibt die Zeile leer (eindeutig: nichts zu tun); ein
        /// unbekannter Buchstabe bleibt stehen und ist nicht eindeutig.
        /// </summary>
        public static Umschluesselung Umschluesseln(string alt, int? baujahr)
        {
            char? ausJahr = baujahr is int j ? Baujahrregel.Klasse(j) : null;
            string altText = alt ?? "";
            bool hatAlt = altText.Trim().Length > 0;
            char a = hatAlt ? char.ToUpperInvariant(altText.Trim()[0]) : '\0';
            bool bekannt = hatAlt && altText.Trim().Length == 1 && TABELLE.ContainsKey(a);

            if (!bekannt)
            {
                if (ausJahr.HasValue) return new Umschluesselung(ausJahr.Value.ToString(), null, true, "");
                if (!hatAlt) return new Umschluesselung(alt, null, true, "");
                return new Umschluesselung(alt, null, false,
                    "unbekannte Baualtersklasse \"" + altText + "\" - unveraendert");
            }

            (char klasse, string standard) = TABELLE[a];
            if (ausJahr.HasValue) return new Umschluesselung(ausJahr.Value.ToString(), standard, true, "");

            if (UNKLAR.IndexOf(a) >= 0)
            {
                string grund = a == 'S'
                    ? "alte Klasse S (Eff. 155, EnEV 2016) ohne Baujahr - die Stufe gibt es nicht; Klasse L ohne Energiestandard"
                    : "alte Klasse " + a + " (" + (a == 'I' ? "Niedrigenergiebauweise" : "Passivhaus") +
                      ") ohne Baujahr - kein Bauzeitraum; Klasse " + klasse + " (2002 bis 2009) angenommen, Energiestandard " + standard;
                return new Umschluesselung(klasse.ToString(), standard, false, grund);
            }
            return new Umschluesselung(klasse.ToString(), standard, true, "");
        }

        /// <summary>
        /// Der neue Name eines Auslieferungssatzes: Ist der zweite Namensteil (Trenner <c>-</c>) genau der
        /// alte Buchstabe <paramref name="altKlasse"/>, tritt <paramref name="neuKlasse"/> an seine Stelle;
        /// sonst <c>null</c> (kein Klassenteil, ein anderer Buchstabe, keine Änderung).
        /// </summary>
        public static string NeuerName(string name, string altKlasse, string neuKlasse)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(altKlasse) || string.IsNullOrEmpty(neuKlasse)) return null;
            if (altKlasse.Length != 1 || neuKlasse.Length != 1) return null;
            if (string.Equals(altKlasse, neuKlasse, StringComparison.Ordinal)) return null;
            string[] teile = name.Split(NAMENSTRENNER);
            if (teile.Length < 2 || !string.Equals(teile[1], altKlasse, StringComparison.Ordinal)) return null;
            teile[1] = neuKlasse;
            return string.Join(NAMENSTRENNER.ToString(), teile);
        }

        /// <summary>Was ein Lauf von <see cref="Ausfuehren"/> getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Die Zahl der angelegten Spalten (höchstens zwei).</summary>
            public int SpaltenAngelegt { get; internal set; }

            /// <summary>Lief die Umschlüsselung (fehlte die Spalte vorher an mindestens einer Tabelle)?</summary>
            public bool Umgeschluesselt { get; internal set; }

            /// <summary>Zahl der Zeilen je Tabelle, deren Klasse oder Standard sich änderte.</summary>
            public Dictionary<string, int> Zeilen { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

            /// <summary>Die Protokollzeilen der nicht eindeutigen Klassen (Tabelle, Id, Name, Grund).</summary>
            public List<string> Unklar { get; } = new List<string>();

            /// <summary>Die Umbenennungen des Auslieferungskatalogs, je „alt -> neu".</summary>
            public List<string> Umbenannt { get; } = new List<string>();

            /// <summary>Die Kollisionen: der alte Name blieb, weil der neue schon vergeben war.</summary>
            public List<string> Kollisionen { get; } = new List<string>();

            /// <summary>Alle Protokollzeilen in einer Liste (für Migration und Werkzeug).</summary>
            public IEnumerable<string> Zeilentexte()
            {
                yield return SpaltenAngelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                             GebaeudeSchema.TABELLEN.Length.ToString(CultureInfo.InvariantCulture) +
                             " Spalte(n) Energiestandard angelegt";
                if (!Umgeschluesselt)
                {
                    yield return "Umschluesselung und Umbenennung liefen schon (die Spalte Energiestandard stand) - nichts verschoben";
                    yield break;
                }
                foreach (KeyValuePair<string, int> z in Zeilen)
                    yield return z.Key + ": " + z.Value.ToString(CultureInfo.InvariantCulture) + " Zeile(n) umgeschluesselt";
                foreach (string u in Unklar) yield return "nicht eindeutig: " + u;
                foreach (string u in Umbenannt) yield return "umbenannt: " + u;
                foreach (string k in Kollisionen) yield return "Name belassen (Kollision): " + k;
            }
        }

        /// <summary>
        /// Steht der Schritt? Die Spalte an beiden Gebäudetabellen und die Sicht des siebten Durchgangs
        /// (<see cref="GebaeudeSchema.EnergiestandardVollstaendig"/>).
        /// </summary>
        public static bool Vollstaendig() => GebaeudeSchema.EnergiestandardVollstaendig();

        /// <summary>
        /// Führt den Schritt aus — für Migration (über ihren Schritt), <c>Werkzeuge/Testdatenbankschema</c>
        /// und <c>EPOS.Kern.Tests</c>; <b>wiederholbar</b>, als LETZTER Sichtneubau. Setzt die sechs
        /// früheren Durchgänge voraus (die Sicht nennt ihre Spalten). Fehler werfen, der Vorgang rollt
        /// zurück, der Aufrufer meldet sie.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            var b = new Bericht();

            // Der Marker VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung und
            // saehe die offene Transaktion nicht.
            var fehlt = GebaeudeSchema.TABELLEN
                .Where(t => !DataRepository.SpalteVorhanden(t, GebaeudeSchema.SPALTE_ENERGIESTANDARD)).ToList();
            var mitBaujahr = GebaeudeSchema.TABELLEN
                .Where(t => DataRepository.SpalteVorhanden(t, GebaeudeSchema.SPALTE_BAUJAHR)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                    bericht?.Add("Sicht " + GebaeudeSchema.VIEW + " verworfen");

                    foreach (string t in fehlt)
                    {
                        v.Ausfuehren(GebaeudeSchema.EnergiestandardAnlegen(t));
                        b.SpaltenAngelegt++;
                    }

                    // Der Plan der Umbenennung entsteht VOR der Umschluesselung: Er vergleicht den
                    // Namensteil mit der ALTEN Klasse der Zeile.
                    bool katalog = fehlt.Contains(GebaeudeSchema.TAB_GEBAEUDE_STAMM);
                    List<(int Id, string Alt, string Neu)> plan = katalog
                        ? Umbenennungsplan(v, mitBaujahr.Contains(GebaeudeSchema.TAB_GEBAEUDE_STAMM))
                        : null;

                    foreach (string t in fehlt)
                    {
                        b.Umgeschluesselt = true;
                        Umschluesseln(v, t, mitBaujahr.Contains(t), b);
                    }

                    if (katalog) Umbenennen(v, plan, b);

                    v.Ausfuehren(GebaeudeSchema.SQL_VIEW_ENERGIESTANDARD);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }

            if (bericht != null)
            {
                foreach (string z in b.Zeilentexte()) bericht.Add(z);
                bericht.Add("Sicht " + GebaeudeSchema.VIEW + " neu gebaut (" +
                            GebaeudeSchema.SICHT_ENERGIESTANDARD.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
            }
            return b;
        }

        /// <summary>Schlüsselt die Klassen EINER Tabelle um und setzt den Energiestandard.</summary>
        private static void Umschluesseln(DbVorgang v, string tabelle, bool mitBaujahr, Bericht b)
        {
            string name = NameSpalte(tabelle);
            DataTable dt = v.Lese("SELECT ID, [" + name + "] AS Name, Baualtersklasse" +
                                  (mitBaujahr ? ", Baujahr" : ", NULL AS Baujahr") +
                                  " FROM [" + tabelle + "] ORDER BY ID");
            int geaendert = 0;
            foreach (DataRow r in dt.Rows)
            {
                string alt = r["Baualtersklasse"] == DBNull.Value ? null : Convert.ToString(r["Baualtersklasse"], CultureInfo.InvariantCulture);
                int? baujahr = r["Baujahr"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Baujahr"], CultureInfo.InvariantCulture);
                Umschluesselung u = Umschluesseln(alt, baujahr);
                int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);

                if (!u.Eindeutig)
                    b.Unklar.Add(tabelle + " " + id.ToString(CultureInfo.InvariantCulture) + " \"" +
                                 Convert.ToString(r["Name"], CultureInfo.InvariantCulture) + "\": " + u.Grund);

                if (string.Equals(u.Klasse, alt, StringComparison.Ordinal) && u.Energiestandard == null) continue;
                v.Ausfuehren("UPDATE [" + tabelle + "] SET Baualtersklasse = ?, " + GebaeudeSchema.SPALTE_ENERGIESTANDARD +
                             " = ? WHERE ID = ?",
                             new DbParam("@k", (object)u.Klasse ?? DBNull.Value),
                             new DbParam("@e", (object)u.Energiestandard ?? DBNull.Value),
                             new DbParam("@id", id));
                geaendert++;
            }
            b.Zeilen[tabelle] = geaendert;
        }

        /// <summary>
        /// Der PLAN der Umbenennung — gelesen vor der Umschlüsselung: je Auslieferungssatz
        /// (<c>ReadOnly = 1</c>), dessen zweiter Namensteil genau seine alte Klasse ist, der neue Name
        /// mit der Klasse, die die Umschlüsselung der Zeile ergibt. Eigene Sätze bleiben außen vor.
        /// Kollisionen löst erst <see cref="Umbenennen"/>; der Plan führt alle Namen des Katalogs mit.
        /// </summary>
        private static List<(int Id, string Alt, string Neu)> Umbenennungsplan(DbVorgang v, bool mitBaujahr)
        {
            string t = GebaeudeSchema.TAB_GEBAEUDE_STAMM;
            DataTable dt = v.Lese("SELECT ID, Bezeichner, Baualtersklasse, ReadOnly" +
                                  (mitBaujahr ? ", Baujahr" : ", NULL AS Baujahr") + " FROM [" + t + "] ORDER BY ID");
            var plan = new List<(int Id, string Alt, string Neu)>();
            foreach (DataRow r in dt.Rows)
            {
                string name = r["Bezeichner"] == DBNull.Value ? "" : Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture);
                bool auslieferung = r["ReadOnly"] != DBNull.Value && Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture) == 1;
                string alt = r["Baualtersklasse"] == DBNull.Value ? null : Convert.ToString(r["Baualtersklasse"], CultureInfo.InvariantCulture);
                int? baujahr = r["Baujahr"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Baujahr"], CultureInfo.InvariantCulture);
                string neu = auslieferung ? NeuerName(name, alt, Umschluesseln(alt, baujahr).Klasse) : null;
                // Auch die bleibenden Namen stehen im Plan (Neu = null): Sie sind die Kollisionsmenge.
                plan.Add((Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture), name, neu));
            }
            return plan;
        }

        /// <summary>
        /// Benennt die Auslieferungssätze nach dem <paramref name="plan"/> um. Ein neuer Name darf weder
        /// einen bleibenden Namen treffen noch einen zweiten neuen; wer zurückfällt, behält seinen alten
        /// Namen und steht in <see cref="Bericht.Kollisionen"/>.
        /// </summary>
        private static void Umbenennen(DbVorgang v, List<(int Id, string Alt, string Neu)> plan, Bericht b)
        {
            string t = GebaeudeSchema.TAB_GEBAEUDE_STAMM;
            var namen = new HashSet<string>(plan.Select(p => p.Alt), StringComparer.Ordinal);

            // Wer zurueckfaellt, behaelt seinen alten Namen - der bleibt damit stehen und kann einen
            // weiteren Kandidaten treffen; deshalb bis zur Ruhe.
            var angenommen = plan.Where(p => p.Neu != null).ToList();
            while (true)
            {
                var weg = new HashSet<string>(angenommen.Select(k => k.Alt), StringComparer.Ordinal);
                var zahl = angenommen.GroupBy(k => k.Neu, StringComparer.Ordinal)
                                     .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
                var abgelehnt = angenommen.Where(k => zahl[k.Neu] > 1 || (namen.Contains(k.Neu) && !weg.Contains(k.Neu))).ToList();
                if (abgelehnt.Count == 0) break;
                foreach (var k in abgelehnt)
                {
                    angenommen.Remove(k);
                    b.Kollisionen.Add("\"" + k.Alt + "\" (\"" + k.Neu + "\" ist schon vergeben)");
                }
            }

            // Zwei Durchgaenge wegen des eindeutigen Index auf Bezeichner: erst auf einen Zwischennamen,
            // dann auf den Zielnamen - so tragen auch Ketten (X-A -> X-B, X-B -> X-C) nie zwei Saetze
            // denselben Namen.
            foreach (var k in angenommen)
                v.Ausfuehren("UPDATE [" + t + "] SET Bezeichner = ? WHERE ID = ?",
                             new DbParam("@n", Zwischenname(k.Id)), new DbParam("@id", k.Id));
            foreach (var k in angenommen)
            {
                v.Ausfuehren("UPDATE [" + t + "] SET Bezeichner = ? WHERE ID = ?",
                             new DbParam("@n", k.Neu), new DbParam("@id", k.Id));
                b.Umbenannt.Add("\"" + k.Alt + "\" -> \"" + k.Neu + "\"");
            }
        }

        /// <summary>Der Zwischenname eines umzubenennenden Satzes — eindeutig über die Id.</summary>
        private static string Zwischenname(int id) => "~E47-Umbenennung-" + id.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die Namensspalte einer Gebäudetabelle: <c>Bezeichner</c> im Katalog, <c>Gebaeudename</c> in der Projektkopie.</summary>
        private static string NameSpalte(string tabelle)
            => string.Equals(tabelle, GebaeudeSchema.TAB_GEBAEUDE_STAMM, StringComparison.Ordinal) ? "Bezeichner" : "Gebaeudename";
    }
}
