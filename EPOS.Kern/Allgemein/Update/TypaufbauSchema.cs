using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BA-2 - TYPAUFBAUTEN UND KENNZEICHEN DES ERSATZAUFBAUS (Schritt 192; Entscheid E95-7; Konzept Bauteilaufbau
    // beim Import 5.3 und 5.6).
    //
    // WAS. Eine Spalte an BEIDEN Aufbautabellen und die Saat der Typaufbauten:
    //
    //   Tab_Bauteilaufbau        Typaufbau  TEXT NULL  CHECK (NULL oder einer der Codes der Saat)
    //   Tab_Bauteilaufbau_STAMM  Typaufbau  TEXT NULL  dieselbe Klausel
    //
    // WARUM AN BEIDEN. Das Datenmodell ist Aufbau -> Schichten (Tab_Bauteilschicht(_STAMM), Reihenfolge innen ->
    // aussen) -> Baustoff (ID_Baustoff, die Stoffwerte als Kopie an der Schicht). Ein Typaufbau ist ein Aufbau
    // des KATALOGS mit seinen Schichten - die Spalte am Katalog sagt, welcher Katalogsatz welcher Typ ist (die
    // Wahl je Aufbau der Oberflaeche, BA-3, und das Katalogpaket tragen sie mit). Die Spalte an der PROJEKTKOPIE
    // ist das Kennzeichen des Ersatzaufbaus: Der Import schreibt den Code als KOPIE, kein ID-Verweis auf den
    // Katalog - Katalogpflege aendert keinen Projektaufbau (Konzept 5.3). NULL = ein echter Aufbau (Datei,
    // Katalog, Anwender); ohne das Kennzeichen waere ein Ersatzaufbau (Herkunft VORGABE) von einem gepflegten
    // nicht zu unterscheiden (E95-7). Die Stufe A/B/C bleibt abgeleitet (Bauteilzuordnung), nicht gespeichert.
    //
    // DIE SAAT (TypaufbauSaat.cs): neun Typaufbauten mit drei bis vier Schichten aus der Normsaat der Baustoffe,
    // ReadOnly = 1, Herkunft VORGABE. Natuerlicher Schluessel ist der Code (ReadOnly = 1); die Ids vergibt die
    // Tabelle. Danach belegt KatalogSchluesselSaat Schluessel und Pruefsumme der neuen Saetze (Katalogfassung
    // Stufe 2, "BTA:..."), damit ein spaeteres Katalogpaket sie erkennt. Fehlt ein Baustoff der Normsaat in der
    // Datenbank, bleibt ID_Baustoff der Schicht leer; die Stoffwerte stehen ohnehin als Kopie an der Schicht.
    //
    // DIE KETTE. 192 = RaumgrundrissSchema.SCHRITT + 1 (191 haengt vorlaeufig an 189 + 2, siehe dort).
    //
    // REFERENZLAUF UNVERAENDERT: Kein Referenzprojekt ist importiert, die 62 Bauteile der Referenzprojekte
    // tragen keinen Aufbau, der Kern liest die Spalte nicht (Konzept 5.6). Die Saat ist Katalog, keine gesaeten
    // Gebaeudedaten eines Referenzprojekts. Wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Schemaschritt 192 — Spalte <c>Typaufbau</c> an Projekt- und Katalogaufbau und die Saat der Typaufbauten
    /// (Kopf der Datei).
    /// </summary>
    public static class TypaufbauSchema
    {
        /// <summary>Die Nummer des Schritts — hängt an <see cref="RaumgrundrissSchema"/>.</summary>
        public const int SCHRITT = RaumgrundrissSchema.SCHRITT + 1;

        /// <summary>Die Spalte des Kennzeichens.</summary>
        public const string SPALTE = "Typaufbau";

        /// <summary>Die Projektkopie der Aufbauten.</summary>
        public const string TAB_PROJEKT = BauteilaufbauSchema.TAB_AUFBAU;

        /// <summary>Der Katalog der Aufbauten.</summary>
        public const string TAB_STAMM = BauteilaufbauSchema.TAB_AUFBAU_STAMM;

        /// <summary>Die Schichten des Katalogs.</summary>
        public const string TAB_SCHICHT_STAMM = BauteilaufbauSchema.TAB_SCHICHT_STAMM;

        /// <summary>Der Katalog der Baustoffe.</summary>
        public const string TAB_BAUSTOFF_STAMM = "Tab_Baustoff_STAMM";

        /// <summary>Die Prüfklausel der Spalte: NULL oder einer der Codes der Saat.</summary>
        public static string Pruefklausel =>
            "\"" + SPALTE + "\" IS NULL OR \"" + SPALTE + "\" IN (" +
            string.Join(",", TypaufbauSaattabelle.Codes.Select(c => "'" + c + "'")) + ")";

        /// <summary>Die Anweisung, die die Spalte an <paramref name="tabelle"/> anlegt.</summary>
        public static string SqlSpalte(string tabelle) =>
            "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + SPALTE + "\" TEXT CHECK (" + Pruefklausel + ")";

        /// <summary>Die Tabellen, die vor dem Schritt stehen müssen.</summary>
        public static IReadOnlyList<string> Voraussetzungen() =>
            new[] { TAB_PROJEKT, TAB_STAMM, TAB_SCHICHT_STAMM, TAB_BAUSTOFF_STAMM };

        /// <summary>Stehen beide Spalten?</summary>
        public static bool SpaltenVorhanden() =>
            DataRepository.SpalteVorhanden(TAB_PROJEKT, SPALTE) && DataRepository.SpalteVorhanden(TAB_STAMM, SPALTE);

        /// <summary>Die Codes der Saat, die der Katalog noch nicht als ausgelieferten Satz trägt.</summary>
        public static IReadOnlyList<string> FehlendeTypen()
        {
            if (!DataRepository.SpalteVorhanden(TAB_STAMM, SPALTE)) return TypaufbauSaattabelle.Codes;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"" + SPALTE + "\" FROM \"" + TAB_STAMM + "\" WHERE \"ReadOnly\" = ? AND \"" + SPALTE + "\" IS NOT NULL",
                new DbParam("@r", 1));
            var da = new HashSet<string>(StringComparer.Ordinal);
            if (t != null)
                foreach (DataRow r in t.Rows) da.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));
            return TypaufbauSaattabelle.Codes.Where(c => !da.Contains(c)).ToArray();
        }

        /// <summary>Die Katalogtabelle der Aufbauten im Register der Katalogfassung.</summary>
        private static IEnumerable<Katalogtabelle> Register => new[] { Katalogfassung.Tabelle(TAB_STAMM) };

        /// <summary>Spalten, Saat und — wo die Katalogspalten stehen — Schlüssel und Prüfsumme vollständig?</summary>
        public static bool Vollstaendig()
        {
            if (!SpaltenVorhanden() || FehlendeTypen().Count > 0) return false;
            if (!Katalogfassung.SpaltenVorhanden(TAB_STAMM)) return true;
            return KatalogSchluesselSaat.OffeneSaetze(Register) == 0;
        }

        /// <summary>
        /// Führt den Schritt aus: Spalten, Saat (in EINEM Vorgang), danach Schlüssel und Prüfsumme der
        /// Katalogfassung. <b>Wiederholbar</b>; legt nichts doppelt an und überschreibt nichts.
        /// </summary>
        /// <returns>Die Zahl der angelegten Typaufbauten.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (Vollstaendig())
            {
                bericht?.Add("steht bereits - Spalte Typaufbau an Projekt und Katalog, " +
                             TypaufbauSaattabelle.Alle.Count.ToString(CultureInfo.InvariantCulture) + " Typaufbauten; nichts zu tun");
                return 0;
            }

            bool projekt = DataRepository.SpalteVorhanden(TAB_PROJEKT, SPALTE);
            bool stamm = DataRepository.SpalteVorhanden(TAB_STAMM, SPALTE);
            int angelegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    if (!projekt) v.Ausfuehren(SqlSpalte(TAB_PROJEKT));
                    if (!stamm) v.Ausfuehren(SqlSpalte(TAB_STAMM));
                    angelegt = SaatSchreiben(v);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(projekt ? "Tab_Bauteilaufbau.Typaufbau steht bereits" : "Tab_Bauteilaufbau.Typaufbau angelegt (NULL = echter Aufbau)");
            bericht?.Add(stamm ? "Tab_Bauteilaufbau_STAMM.Typaufbau steht bereits" : "Tab_Bauteilaufbau_STAMM.Typaufbau angelegt");
            bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " Typaufbauten gesaet (ReadOnly, Herkunft VORGABE)");
            if (Katalogfassung.SpaltenVorhanden(TAB_STAMM))
            {
                int n = KatalogSchluesselSaat.Ausfuehren(null, Register);
                bericht?.Add(n.ToString(CultureInfo.InvariantCulture) + " Katalogschluessel und Pruefsummen belegt");
            }
            bericht?.Add("KEIN DML an Projektdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        /// <summary>Legt jeden fehlenden Typaufbau samt Schichten im Vorgang an; liefert die Zahl.</summary>
        private static int SaatSchreiben(DbVorgang v)
        {
            var da = new HashSet<string>(StringComparer.Ordinal);
            DataTable vorhanden = v.Lese("SELECT \"" + SPALTE + "\" FROM \"" + TAB_STAMM + "\" WHERE \"ReadOnly\" = ? AND \"" +
                                         SPALTE + "\" IS NOT NULL", new DbParam("@r", 1));
            if (vorhanden != null)
                foreach (DataRow r in vorhanden.Rows) da.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));

            var stoffe = new HashSet<int>();
            DataTable st = v.Lese("SELECT \"ID\" FROM \"" + TAB_BAUSTOFF_STAMM + "\"");
            if (st != null)
                foreach (DataRow r in st.Rows) stoffe.Add(Convert.ToInt32(r[0], CultureInfo.InvariantCulture));

            int n = 0;
            foreach (TypaufbauSaat t in TypaufbauSaattabelle.Alle)
            {
                if (da.Contains(t.Code)) continue;
                BauteilaufbauModel m = TypaufbauSaattabelle.AlsModell(t);
                int id = v.EinfuegenUndId(
                    "INSERT INTO \"" + TAB_STAMM + "\" (\"Bezeichner\", \"Beschreibung\", \"Bauteilart\", \"Quelle\", \"Herkunft\", " +
                    "\"Quellkennung\", \"ReadOnly\", \"" + SPALTE + "\") VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                    new[] {
                    new DbParam("@b", m.Bezeichner),
                    new DbParam("@be", m.Beschreibung),
                    new DbParam("@art", m.Bauteilart),
                    new DbParam("@q", m.Quelle),
                    new DbParam("@h", m.Herkunft),
                    BaustoffCtrl.Text("@qk", null),
                    new DbParam("@ro", 1),
                    new DbParam("@t", m.Typaufbau) });
                int rang = 0;
                foreach (BauteilschichtModel s in m.Schichten)
                {
                    bool stoff = s.ID_Baustoff is int b && stoffe.Contains(b);
                    v.EinfuegenUndId(
                        "INSERT INTO \"" + TAB_SCHICHT_STAMM + "\" (\"ID_Aufbau\", \"Reihenfolge\", \"ID_Baustoff\", \"Dicke\", " +
                        "\"IstLuftschicht\", \"Lambda\", \"Rho\", \"cp\") VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                        new[] {
                        new DbParam("@a", id),
                        new DbParam("@r", ++rang),
                        new DbParam("@s", DbParamTyp.Integer) { Wert = stoff ? (object)s.ID_Baustoff.Value : DBNull.Value },
                        new DbParam("@d", DbParamTyp.Double) { Wert = s.Dicke },
                        new DbParam("@l", DbParamTyp.Integer) { Wert = 0 },
                        new DbParam("@la", DbParamTyp.Double) { Wert = s.Lambda.Value },
                        new DbParam("@rh", DbParamTyp.Double) { Wert = s.Rho.Value },
                        new DbParam("@cp", DbParamTyp.Double) { Wert = s.Cp.Value } });
                }
                n++;
            }
            return n;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
