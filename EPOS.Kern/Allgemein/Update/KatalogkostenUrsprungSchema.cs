using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KATALOGKOSTEN UND URSPRUNG (Konzept Projektdialoge mit Katalogauswahl, Abschnitt 7 Nr. 3; Stufe 2,
    // Rueckweg „In die Datenbank übernehmen…", KA‑E‑9).
    //
    // WAS. Zwei Spaltenfamilien, beide nullbar, reines DDL:
    //   a. ID_KostenVorlage an den acht Katalogen mit Kosten (Tab_BHKW_STAMM, Tab_Heizkessel_STAMM,
    //      Tab_Pufferspeicher_STAMM, Tab_Stromspeicher_STAMM, Tab_PV_STAMM, Tab_Solarkollektoren_STAMM,
    //      Tab_WP_STAMM, Tab_Kaeltemaschine_STAMM): die Kostenvorlage DES SATZES (Tab_KostenVorlage.ID). Leer
    //      heisst Standardvorlage des Gewerks wie bisher. Der Rueckweg schreibt die Betriebskostenpositionen
    //      der Anlage als eigene Vorlage (Gewerk = KomponentenID, Name = Satzname, nicht Standard) und verweist
    //      hierueber; die Uebernahme Katalog -> Projekt zieht sie vor der Standardvorlage.
    //   b. ID_Stamm an den Projektkopien, die ihren Ursprung bisher nicht kannten: die fuenf Geraete
    //      Tab_BHKW, Tab_Heizkessel, Tab_Stromspeicher, Tab_PV, Tab_Solarkollektoren und die Kopien der Bedarfs-
    //      und Zeitreihenkataloge Tab_Stromverbraucher, Tab_Brauchwasser, Tab_Prozesswaerme, Tab_Waermebedarf,
    //      Tab_Stromganglinie, Tab_Solarganglinie. Leer heisst „Ursprung nicht bekannt" - der Bestand wird NICHT
    //      ueber den Namen nachgetragen (eine Umbenennung traefe den falschen Satz, Konzept 7 Nr. 3).
    //
    // FREMDSCHLUESSEL nach dem Muster der vorhandenen Verweise (Tab_WP.ID_Stamm, Schritt 80;
    // Tab_Pufferspeicher.ID_Stamm; Tab_Kaeltemaschine.ID_Stamm): REFERENCES … ("ID") ON DELETE SET NULL. Ein
    // geloeschter Katalogsatz bzw. eine geloeschte Vorlage leert den Verweis; der Rueckweg meldet dann
    // „Ursprung nicht mehr vorhanden" bzw. die Uebernahme faellt auf die Standardvorlage zurueck. SQLite
    // erlaubt ALTER TABLE ADD COLUMN mit REFERENCES, solange die Vorgabe NULL ist.
    //
    // ERGEBNISNEUTRAL. Alle Spalten entstehen leer; keine Katalogpruefsumme aendert sich (die Fachspalten der
    // Katalogfassung sind benannt, die neuen Spalten gehoeren nicht dazu).
    //
    // NUMMER. KalenderbedienungSchema.SCHRITT + 1 (208). Eingetragen in SchemaStand.Zielversion, im Register
    // der Paketanhebung, in der SchemaMigration der Schale, in Werkzeuge/Testdatenbankschema und in
    // EPOS.Kern.Tests/TestDatenbank.
    // ====================================================================================

    /// <summary>
    /// <b>Katalogkosten und Ursprung</b> — <c>ID_KostenVorlage</c> an den acht Katalogen mit Kosten und
    /// <c>ID_Stamm</c> an den elf Projektkopien ohne Ursprungsverweis. Anlass und Bauform im Kopf der Datei.
    /// </summary>
    public static class KatalogkostenUrsprungSchema
    {
        /// <summary>Die Nummer des Schritts — hängt an <see cref="KalenderbedienungSchema"/>.</summary>
        public const int SCHRITT = KalenderbedienungSchema.SCHRITT + 1;

        /// <summary>Die Spalte der Kostenvorlage eines Katalogsatzes.</summary>
        public const string SPALTE_ID_KOSTENVORLAGE = "ID_KostenVorlage";

        /// <summary>Die Spalte des Ursprungs einer Projektkopie (wie <c>Tab_WP.ID_Stamm</c>).</summary>
        public const string SPALTE_ID_STAMM = "ID_Stamm";

        /// <summary>Die Tabelle der Kostenvorlagen.</summary>
        public const string TAB_KOSTENVORLAGE = "Tab_KostenVorlage";

        /// <summary>Die acht Kataloge mit Kosten, die <see cref="SPALTE_ID_KOSTENVORLAGE"/> bekommen.</summary>
        public static readonly IReadOnlyList<string> KATALOGE_MIT_KOSTEN = new[]
        {
            "Tab_BHKW_STAMM", "Tab_Heizkessel_STAMM", "Tab_Pufferspeicher_STAMM", "Tab_Stromspeicher_STAMM",
            "Tab_PV_STAMM", "Tab_Solarkollektoren_STAMM", "Tab_WP_STAMM", "Tab_Kaeltemaschine_STAMM",
        };

        /// <summary>Die elf Projektkopien ohne Ursprungsverweis und ihr Katalog.</summary>
        public static readonly IReadOnlyList<(string Kopie, string Katalog)> KOPIEN_OHNE_URSPRUNG = new[]
        {
            ("Tab_BHKW", "Tab_BHKW_STAMM"),
            ("Tab_Heizkessel", "Tab_Heizkessel_STAMM"),
            ("Tab_Stromspeicher", "Tab_Stromspeicher_STAMM"),
            ("Tab_PV", "Tab_PV_STAMM"),
            ("Tab_Solarkollektoren", "Tab_Solarkollektoren_STAMM"),
            ("Tab_Stromverbraucher", "Tab_Stromverbraucher_STAMM"),
            ("Tab_Brauchwasser", "Tab_Brauchwasser_STAMM"),
            ("Tab_Prozesswaerme", "Tab_Prozesswaerme_STAMM"),
            ("Tab_Waermebedarf", "Tab_Waermebedarf_STAMM"),
            ("Tab_Stromganglinie", "Tab_Stromganglinie_STAMM"),
            ("Tab_Solarganglinie", "Tab_Solarganglinie_STAMM"),
        };

        /// <summary>Alle neunzehn Spalten des Schritts: Tabelle, Spalte, Typ samt Verweis.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            KATALOGE_MIT_KOSTEN.Select(t => (t, SPALTE_ID_KOSTENVORLAGE, Verweis(TAB_KOSTENVORLAGE)))
                .Concat(KOPIEN_OHNE_URSPRUNG.Select(k => (k.Kopie, SPALTE_ID_STAMM, Verweis(k.Katalog))))
                .ToArray();

        private static string Verweis(string ziel) => "INTEGER REFERENCES \"" + ziel + "\" (\"ID\") ON DELETE SET NULL";

        /// <summary>Die Tabellen, die stehen müssen, bevor der Schritt läuft.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => SPALTEN.Select(s => s.Tabelle).Concat(KOPIEN_OHNE_URSPRUNG.Select(k => k.Katalog))
                      .Append(TAB_KOSTENVORLAGE).Distinct(StringComparer.Ordinal).ToArray();

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen alle Spalten des Schritts?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Kennt die Projektkopie <paramref name="kopie"/> ihren Ursprung (Spalte vorhanden)?</summary>
        public static bool UrsprungLesbar(string kopie) => DataRepository.SpalteVorhanden(kopie, SPALTE_ID_STAMM);

        /// <summary>Führt der Katalog <paramref name="katalog"/> eine Kostenvorlage des Satzes (Spalte vorhanden)?</summary>
        public static bool KostenvorlageLesbar(string katalog)
            => DataRepository.SpalteVorhanden(katalog, SPALTE_ID_KOSTENVORLAGE);

        /// <summary>
        /// Legt die fehlenden Spalten an — <b>wiederholbar</b>; ein zweiter Lauf ändert nichts. Gibt die Zahl der
        /// angelegten Spalten zurück.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - ID_KostenVorlage an den acht Katalogen, ID_Stamm an den elf Kopien; nichts zu tun");
                return 0;
            }

            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
