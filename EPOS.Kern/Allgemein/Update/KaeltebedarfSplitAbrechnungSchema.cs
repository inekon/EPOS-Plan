using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SCHRITT 224 - DIE ABRECHNUNG DES SPLIT-STROMS (Konzept Kaeltebedarf 4.2, 4.3; Entscheide E34/E35;
    // Plan K6 Abschnitt 1).
    //
    // WAS.
    //   Tab_ErgebnisKaelteDezentral  eine Ergebniszeile je dezentral (Split) gedeckter Kaeltebedarfszeile eines Laufs:
    //                                Quelle (lastgang/profil), die ID der Zuordnungszeile, Kaelte, Strom, Spitzen,
    //                                Jahres-EER, der Netzbezug des Split-Stroms und - nur wenn er vom Stromtraeger des
    //                                Projekts abweicht - Kuehltraeger samt Abrechnungsart. Muster: die Ergebniszeile der
    //                                Kaeltemaschine (Schritte 183/184).
    //
    // WARUM EINE TABELLE UND KEINE SPALTE. Die Eingabespalten Kuehl_ID_Carrier und Kuehl_EigenerZaehler stehen seit
    // Schritt 213 an beiden Zuordnungen. Was fehlte, war die Menge je Traeger: Die Abrechnung (Kaeltestromabrechnung)
    // liest nur gespeicherte Ergebniszeilen, damit Kosten und Emissionen ohne frischen Lauf entstehen.
    //
    // OHNE FREMDSCHLUESSEL an ID_Zuordnung und Kuehl_ID_Carrier: ID_Zuordnung zeigt je nach Quelle auf eine andere
    // Tabelle und ueberdauert das Loeschen der Zeile; der Traeger steht wie an der Ergebniszeile der Kaeltemaschine ohne
    // Verweis (die Traegerloeschung zaehlt ihn mit). Nur ID_Ergebnis haengt am Lauf (CASCADE).
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt eine Split-Zeile; die Tabelle bleibt leer. Der Referenzlauf bleibt
    // byte-gleich.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Abrechnung des Split-Stroms</b>: die Ergebnistabelle je dezentral gedeckter Kältebedarfszeile.
    /// Wiederholbar und ergebnisneutral; keine Saat, kein DML.
    /// </summary>
    public static class KaeltebedarfSplitAbrechnungSchema
    {
        /// <summary>
        /// Die Nummer des Schritts. Sie hängt an <c>TwwZonenferienSchema</c> (Schritt 223) und wird beim Einhängen in die
        /// Kette als <c>TwwZonenferienSchema.SCHRITT + 1</c> geschrieben.
        /// </summary>
        public const int SCHRITT = 224;

        /// <summary>Die Ergebnistabelle je Split-Zeile.</summary>
        public const string TAB_ERGEBNIS = "Tab_ErgebnisKaelteDezentral";

        /// <summary>Die Quelle einer Lastgangzeile im Kanal Kühlung (<c>Z_ProjektWaermebedarf</c>).</summary>
        public const string QUELLE_LASTGANG = "lastgang";

        /// <summary>Die Quelle einer Profilzeile (<c>Z_Projekt_Kaeltebedarf</c>).</summary>
        public const string QUELLE_PROFIL = "profil";

        /// <summary>Die Art der Zuordnung.</summary>
        public const string SPALTE_QUELLE = "Quelle";

        /// <summary>Die ID der Zuordnungszeile, ohne Verweis.</summary>
        public const string SPALTE_ID_ZUORDNUNG = "ID_Zuordnung";

        /// <summary>Kälte der Zeile [MWh/a].</summary>
        public const string SPALTE_KAELTE = "Kaelte_MWh";

        /// <summary>Strom der Zeile [MWh/a].</summary>
        public const string SPALTE_STROM = "Strom_MWh";

        /// <summary>Spitze der Kälte der Zeile [kW].</summary>
        public const string SPALTE_KAELTE_SPITZE = "Kaelte_Spitze_kW";

        /// <summary>Jahres-EER der Zeile.</summary>
        public const string SPALTE_JAHRES_EER = "Jahres_EER";

        /// <summary>Der Netzbezug des Split-Stroms [MWh/a] — anteilig über die Stufenrechnung bzw. ganz (eigener Zähler).</summary>
        public const string SPALTE_NETZBEZUG = KaeltestromabrechnungSchema.SPALTE_NETZBEZUG;

        /// <summary>Der abweichende Kühlträger (<c>energy_carrier.id</c>); NULL = Träger des Projekts.</summary>
        public const string SPALTE_KUEHL_ID_CARRIER = KuehlungSchema.SPALTE_KUEHL_ID_CARRIER;

        /// <summary>Die Abrechnungsart (0/1); NULL ohne abweichenden Träger.</summary>
        public const string SPALTE_KUEHL_EIGENER_ZAEHLER = KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER;

        /// <summary>Die Spitze des Split-Stroms der Zeile je Stunde [kW] — der Leistungspreis eines eigenen Zählers.</summary>
        public const string SPALTE_STROMSPITZE = KaeltestromabrechnungSchema.SPALTE_STROMSPITZE;

        /// <summary>Die Spalten der Tabelle in Schemareihenfolge (ohne <c>ID</c> und <c>ID_Ergebnis</c>).</summary>
        public static readonly IReadOnlyList<string> SPALTEN = new[]
        {
            SPALTE_QUELLE, SPALTE_ID_ZUORDNUNG, "Bezeichner", SPALTE_KAELTE, SPALTE_STROM, SPALTE_KAELTE_SPITZE,
            SPALTE_JAHRES_EER, SPALTE_NETZBEZUG, SPALTE_KUEHL_ID_CARRIER, SPALTE_KUEHL_EIGENER_ZAEHLER, SPALTE_STROMSPITZE,
        };

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_ErgebnisKaelteDezentral</c>.</summary>
        public static string SqlCreate() =>
            "CREATE TABLE IF NOT EXISTS \"" + TAB_ERGEBNIS + "\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Ergebnis\" INTEGER NOT NULL REFERENCES \"Tab_Ergebnis\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"" + SPALTE_QUELLE + "\" TEXT NOT NULL CHECK (\"" + SPALTE_QUELLE + "\" IN ('" + QUELLE_LASTGANG + "','" +
            QUELLE_PROFIL + "')),\n" +
            "    \"" + SPALTE_ID_ZUORDNUNG + "\" INTEGER,\n" +
            "    \"Bezeichner\" TEXT,\n" +
            "    \"" + SPALTE_KAELTE + "\" REAL,\n" +
            "    \"" + SPALTE_STROM + "\" REAL,\n" +
            "    \"" + SPALTE_KAELTE_SPITZE + "\" REAL,\n" +
            "    \"" + SPALTE_JAHRES_EER + "\" REAL,\n" +
            "    \"" + SPALTE_NETZBEZUG + "\" REAL,\n" +
            "    \"" + SPALTE_KUEHL_ID_CARRIER + "\" INTEGER,\n" +
            "    \"" + SPALTE_KUEHL_EIGENER_ZAEHLER + "\" INTEGER CHECK (\"" + SPALTE_KUEHL_EIGENER_ZAEHLER + "\" IN (0,1)),\n" +
            "    \"" + SPALTE_STROMSPITZE + "\" REAL\n" +
            ") STRICT";

        /// <summary>Was der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { "Tab_Ergebnis" };

        /// <summary>Steht die Tabelle samt jeder Spalte?</summary>
        public static bool Vollstaendig() =>
            DataRepository.TabelleVorhanden(TAB_ERGEBNIS) && SPALTEN.All(s => DataRepository.SpalteVorhanden(TAB_ERGEBNIS, s));

        /// <summary>Kann ein Lauf seine Split-Zeilen speichern und lesen? (Vor dem Schritt fehlt die Tabelle.)</summary>
        public static bool TabelleVorhanden() => DataRepository.TabelleVorhanden(TAB_ERGEBNIS);

        /// <summary>
        /// Legt die Tabelle an, wenn sie fehlt — in EINEM Vorgang. Wiederholbar; liefert die Zahl der Anweisungen (0, wenn
        /// sie steht).
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            int n = 0;
            if (!DataRepository.TabelleVorhanden(TAB_ERGEBNIS))
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        v.Ausfuehren(SqlCreate());
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                bericht?.Add(TAB_ERGEBNIS + " angelegt (STRICT, je Split-Zeile eines Laufs)");
                n++;
            }
            else bericht?.Add(TAB_ERGEBNIS + " vorhanden");
            bericht?.Add("KEIN Rechenergebnis aendert sich, der Referenzlauf bleibt byte-gleich");
            return n;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
