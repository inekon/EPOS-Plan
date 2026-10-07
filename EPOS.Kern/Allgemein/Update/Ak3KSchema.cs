using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // AK3-K - KENNZAHLEN DER ZONENSPERRE UND DER KAELTESEITE IM KREIS (Entwurf AK3-K, Abschnitt 3.5,
    // Festlegung 20; Schemaschritt S1 des Wellenplans, Welle K4).
    //
    // WOZU. Die Zonensperre (in einer Zone wird an einem Tag nie geheizt und gekuehlt) und die
    // Kaelteseite im geschlossenen Kreis der Stufe AK3 zaehlen im Lauf; das Ergebnis des Projekts
    // traegt ihre Kennzahlen wie die Kreiskennzahlen von AK3 (Ak3Schema):
    //
    //   Tab_ErgebnisEnergiebedarf  Zonensperre_Tage                  INTEGER >= 0      Zonentage mit Sperre der Gegenseite
    //                              Zonensperre_Heizen_Gesperrt_MWh   REAL >= 0         Raumheizung des Probetags an Kuehltagen
    //                              Zonensperre_Kuehlen_Gesperrt_MWh  REAL >= 0         Raumkuehlung des Probetags an Heiztagen
    //                              Ak3_Kaelteschranke_Stunden        INTEGER 0..8760   Stunden, in denen die Kaelteschranke griff
    //                              Ak3_Umschalt_Stunden              INTEGER 0..8760   Stunden mit Umschaltung der Waermepumpe
    //                              Ak3_Kaelterest_Stunden            INTEGER 0..8760   Stunden mit Kaelte-Restbedarf (F. 14)
    //                              Ak3_Kaelterest_MWh                REAL >= 0         Kaelte-Restbedarf im Jahr
    //
    // GELTUNGSBEREICH. Die Zonensperre-Spalten stehen nur, wenn die Sperre lief (eine
    // Zone mit wirksamer Kuehlung) - Projektsumme ueber alle Zonen aller Gebaeude; die Ak3_*-Spalten
    // nur, wenn der Kreis auf AK3 die Kaelteseite rechnete. Sonst NULL = "nicht erhoben"; der
    // Referenzlauf-Export nimmt eine NULL-Spalte nicht auf und bleibt byte-gleich.
    //
    // KEIN DML AN BESTANDSDATEN, keine Sicht. Alles in EINEM Vorgang; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>AK3-K</b> — die Kennzahlen der Zonensperre und der Kälteseite im geschlossenen Kreis im Projektergebnis
    /// (Entwurf AK3-K 3.5, Festlegung 20): EINE Quelle für Migration, Werkzeug, Testkopie, Controller und Nachweis
    /// (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class Ak3KSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der Schritt hinter der zuletzt
        /// gebauten Klasse <see cref="Ak3Schema"/>.
        /// </summary>
        public const int SCHRITT = Ak3Schema.SCHRITT + 1;

        /// <summary>Die Ergebnistabelle des Projekts (Energiebedarf).</summary>
        public const string TAB_ERGEBNIS = SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF;

        /// <summary>Zonentage mit Sperre der Gegenseite, Summe über alle Zonen [d].</summary>
        public const string SPALTE_ZONENSPERRE_TAGE = "Zonensperre_Tage";

        /// <summary>Raumheizung des Probetags an Kühltagen, gesperrt [MWh].</summary>
        public const string SPALTE_ZONENSPERRE_HEIZEN_MWH = "Zonensperre_Heizen_Gesperrt_MWh";

        /// <summary>Raumkühlung des Probetags an Heiztagen, gesperrt [MWh].</summary>
        public const string SPALTE_ZONENSPERRE_KUEHLEN_MWH = "Zonensperre_Kuehlen_Gesperrt_MWh";

        /// <summary>Stunden, in denen die Kälteschranke des Kreises griff [h].</summary>
        public const string SPALTE_KAELTESCHRANKE_STUNDEN = "Ak3_Kaelteschranke_Stunden";

        /// <summary>Stunden mit Umschaltung der Wärmepumpe zwischen Heizen und Kühlen [h].</summary>
        public const string SPALTE_UMSCHALT_STUNDEN = "Ak3_Umschalt_Stunden";

        /// <summary>Stunden mit Kälte-Restbedarf der echten Kältestunde [h] (Festlegung 14).</summary>
        public const string SPALTE_KAELTEREST_STUNDEN = "Ak3_Kaelterest_Stunden";

        /// <summary>Kälte-Restbedarf im Jahr [MWh].</summary>
        public const string SPALTE_KAELTEREST_MWH = "Ak3_Kaelterest_MWh";

        /// <summary>Die Spalten der Zonensperre in Schreibreihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ZONENSPERRE = new[]
        {
            SPALTE_ZONENSPERRE_TAGE, SPALTE_ZONENSPERRE_HEIZEN_MWH, SPALTE_ZONENSPERRE_KUEHLEN_MWH,
        };

        /// <summary>Die Spalten der Kälteseite im Kreis in Schreibreihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_KREIS = new[]
        {
            SPALTE_KAELTESCHRANKE_STUNDEN, SPALTE_UMSCHALT_STUNDEN, SPALTE_KAELTEREST_STUNDEN, SPALTE_KAELTEREST_MWH,
        };

        /// <summary>Alle Ergebnisspalten in Schreibreihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS = SPALTEN_ZONENSPERRE.Concat(SPALTEN_KREIS).ToArray();

        private static string Stunden(string s) => "INTEGER CHECK (\"" + s + "\" IS NULL OR \"" + s + "\" BETWEEN 0 AND 8760)";

        private static string NichtNegativ(string typ, string s) => typ + " CHECK (\"" + s + "\" IS NULL OR \"" + s + "\" >= 0)";

        /// <summary>Alle sieben Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ERGEBNIS, SPALTE_ZONENSPERRE_TAGE, NichtNegativ("INTEGER", SPALTE_ZONENSPERRE_TAGE)),
            (TAB_ERGEBNIS, SPALTE_ZONENSPERRE_HEIZEN_MWH, NichtNegativ("REAL", SPALTE_ZONENSPERRE_HEIZEN_MWH)),
            (TAB_ERGEBNIS, SPALTE_ZONENSPERRE_KUEHLEN_MWH, NichtNegativ("REAL", SPALTE_ZONENSPERRE_KUEHLEN_MWH)),
            (TAB_ERGEBNIS, SPALTE_KAELTESCHRANKE_STUNDEN, Stunden(SPALTE_KAELTESCHRANKE_STUNDEN)),
            (TAB_ERGEBNIS, SPALTE_UMSCHALT_STUNDEN, Stunden(SPALTE_UMSCHALT_STUNDEN)),
            (TAB_ERGEBNIS, SPALTE_KAELTEREST_STUNDEN, Stunden(SPALTE_KAELTEREST_STUNDEN)),
            (TAB_ERGEBNIS, SPALTE_KAELTEREST_MWH, NichtNegativ("REAL", SPALTE_KAELTEREST_MWH)),
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ERGEBNIS };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Steht der Schritt? Alle sieben Spalten.</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen alle Ergebnisspalten an <see cref="TAB_ERGEBNIS"/>?</summary>
        public static bool ErgebnisspaltenVorhanden() => Vollstaendig();

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die Migration der Schale,
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar:</b> Eine stehende Spalte
        /// wird übergangen; steht alles, öffnet er keinen Vorgang. <b>Kein DML an Bestandsdaten.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 7).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Kennzahlen der Zonensperre und der Kaelteseite im Kreis an " + TAB_ERGEBNIS +
                             "; nichts zu tun");
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
