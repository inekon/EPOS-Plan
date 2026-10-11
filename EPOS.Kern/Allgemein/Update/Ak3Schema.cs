using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // AK3 - RAUMEINFLUSS DER HEIZKURVE UND KENNZAHLEN DES GESCHLOSSENEN KREISES (Entwurf AK3,
    // Festlegungen 22 und 23; Anwenderentscheid E102, Q-AK3-1 und Q-AK3-2; Schemaschritte S1 und S2
    // des Wellenplans als EIN Schritt).
    //
    // WOZU. Mit der Stufe AK3 rechnen Gebaeude und Kaskade je Stunde im geschlossenen Kreis. Die
    // aussentemperaturgefuehrte Heizkurve bekommt einen Raumeinfluss (H2), und das Ergebnis des
    // Projekts traegt die Kennzahlen des Kreises:
    //
    //   Tab_Gebaeude, Tab_Gebaeude_STAMM  Heizkurve_Raumeinfluss     REAL 0..10 [K/K]  NULL oder 0 = aus
    //   Tab_ErgebnisEnergiebedarf         Ak3_Durchlaeufe_Mittel     REAL >= 1         Mittel der Durchlaeufe je Stunde
    //                                     Ak3_Durchlaeufe_Max        INTEGER >= 1      groesste Zahl einer Stunde
    //                                     Ak3_Fallwechsel            INTEGER >= 0      Wechsel der Stuetzstelle und der Betriebsfaelle
    //                                     Ak3_Schranke_Stunden       INTEGER 0..8760   Stunden, in denen die Schranke griff
    //                                     Ak3_Speicher_Leer_Stunden  INTEGER 0..8760   Stunden mit Speicher und nichts entnehmbar
    //                                     Ak3_Restbedarf_Stunden     INTEGER 0..8760   Stunden mit Restbedarf der Kaskade (F. 15)
    //
    // WARUM AN DER PROJEKTZEILE. Der Kreis ist EINER je Projekt: eine Kaskade, ein Angebot, die
    // Durchlaeufe zaehlen je Projektstunde ueber alle gekoppelten Gebaeude. Die Kennzahlen gehoeren
    // deshalb neben Komfort und Fahrplanbegrenzung an Tab_ErgebnisEnergiebedarf, nicht an die
    // Gebaeudezeile. NULL = "nicht erhoben" - jede Stufe ausser AK3 (Festlegung 22); der
    // Referenzlauf-Export nimmt eine NULL-Spalte nicht auf und bleibt byte-gleich.
    //
    // DER ZEHNTE SICHTNEUBAU. Der Lauf liest das Gebaeude ueber Abfrage_Projektgebaeude; der
    // Raumeinfluss steht dort HINTER dem wirksamen U-Wert der Bodenplatte (105 Spalten,
    // GebaeudeSchema.SQL_VIEW_AK3). Weil aeltere Durchgaenge die Sicht in ihrer Form neu bauen,
    // laeuft dieser Schritt in Migration, Werkzeug und Testkopie ZULETZT und baut die Sicht, sobald
    // sie nicht seine Form hat.
    //
    // KEIN DML AN BESTANDSDATEN. Jede Bestandszeile steht danach auf NULL - jedes Projekt rechnet wie
    // vorher. Alles in EINEM Vorgang mit abgeschalteten Fremdschluesseln: Scheitert ein Teil, bleibt
    // die Datei, wie sie war; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>AK3</b> — der Raumeinfluss der Heizkurve je Gebäude (Festlegung 23) und die Kennzahlen des geschlossenen
    /// Kreises im Projektergebnis (Festlegung 22): EINE Quelle für Migration, Werkzeug, Testkopie, Controller und
    /// Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class Ak3Schema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der Schritt hinter der
        /// zuletzt auf <c>origin</c> liegenden Klasse: Schritt 198, hängt an 197 <see cref="FlaechenherkunftSchema"/>.
        /// </summary>
        public const int SCHRITT = FlaechenherkunftSchema.SCHRITT + 1;

        /// <summary>Die Gebäudespalte an beiden Gebäudetabellen.</summary>
        public const string SPALTE_RAUMEINFLUSS = GebaeudeSchema.SPALTE_HEIZKURVE_RAUMEINFLUSS;

        /// <summary>Der plausible Höchstwert des Raumeinflusses [K/K] (Prüfklausel).</summary>
        public const double RAUMEINFLUSS_MAX = 10.0;

        /// <summary>Typ samt Prüfklausel: REAL, NULL oder 0 bis <see cref="RAUMEINFLUSS_MAX"/>.</summary>
        public const string TYP_RAUMEINFLUSS = "REAL CHECK (\"" + SPALTE_RAUMEINFLUSS + "\" IS NULL OR (\"" +
                                               SPALTE_RAUMEINFLUSS + "\" >= 0 AND \"" + SPALTE_RAUMEINFLUSS + "\" <= 10))";

        /// <summary>Die Ergebnistabelle des Projekts (Energiebedarf).</summary>
        public const string TAB_ERGEBNIS = SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF;

        /// <summary>Mittel der Durchläufe je Stunde [–].</summary>
        public const string SPALTE_DURCHLAEUFE_MITTEL = "Ak3_Durchlaeufe_Mittel";

        /// <summary>Größte Zahl der Durchläufe einer Stunde [–].</summary>
        public const string SPALTE_DURCHLAEUFE_MAX = "Ak3_Durchlaeufe_Max";

        /// <summary>Gezählte Fallwechsel (Stützstelle der Wärmepumpe und Betriebsfall je Zone) [–].</summary>
        public const string SPALTE_FALLWECHSEL = "Ak3_Fallwechsel";

        /// <summary>Stunden, in denen die Schranke des Angebots eine Zone begrenzte [h].</summary>
        public const string SPALTE_SCHRANKE_STUNDEN = "Ak3_Schranke_Stunden";

        /// <summary>Stunden, in denen am Kreis ein Speicher steht und nichts aus ihm entnehmbar ist [h].</summary>
        public const string SPALTE_SPEICHER_LEER_STUNDEN = "Ak3_Speicher_Leer_Stunden";

        /// <summary>Stunden mit Restbedarf der Kaskade nach Festlegung 15 [h].</summary>
        public const string SPALTE_RESTBEDARF_STUNDEN = "Ak3_Restbedarf_Stunden";

        /// <summary>Die Ergebnisspalten in Schreibreihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS = new[]
        {
            SPALTE_DURCHLAEUFE_MITTEL, SPALTE_DURCHLAEUFE_MAX, SPALTE_FALLWECHSEL,
            SPALTE_SCHRANKE_STUNDEN, SPALTE_SPEICHER_LEER_STUNDEN, SPALTE_RESTBEDARF_STUNDEN,
        };

        private static string Stunden(string s) => "INTEGER CHECK (\"" + s + "\" IS NULL OR \"" + s + "\" BETWEEN 0 AND 8760)";

        /// <summary>Alle acht Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (GebaeudeSchema.TAB_GEBAEUDE, SPALTE_RAUMEINFLUSS, TYP_RAUMEINFLUSS),
            (GebaeudeSchema.TAB_GEBAEUDE_STAMM, SPALTE_RAUMEINFLUSS, TYP_RAUMEINFLUSS),
            (TAB_ERGEBNIS, SPALTE_DURCHLAEUFE_MITTEL,
             "REAL CHECK (\"" + SPALTE_DURCHLAEUFE_MITTEL + "\" IS NULL OR \"" + SPALTE_DURCHLAEUFE_MITTEL + "\" >= 1)"),
            (TAB_ERGEBNIS, SPALTE_DURCHLAEUFE_MAX,
             "INTEGER CHECK (\"" + SPALTE_DURCHLAEUFE_MAX + "\" IS NULL OR \"" + SPALTE_DURCHLAEUFE_MAX + "\" >= 1)"),
            (TAB_ERGEBNIS, SPALTE_FALLWECHSEL,
             "INTEGER CHECK (\"" + SPALTE_FALLWECHSEL + "\" IS NULL OR \"" + SPALTE_FALLWECHSEL + "\" >= 0)"),
            (TAB_ERGEBNIS, SPALTE_SCHRANKE_STUNDEN, Stunden(SPALTE_SCHRANKE_STUNDEN)),
            (TAB_ERGEBNIS, SPALTE_SPEICHER_LEER_STUNDEN, Stunden(SPALTE_SPEICHER_LEER_STUNDEN)),
            (TAB_ERGEBNIS, SPALTE_RESTBEDARF_STUNDEN, Stunden(SPALTE_RESTBEDARF_STUNDEN)),
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => new[] { GebaeudeSchema.TAB_GEBAEUDE, GebaeudeSchema.TAB_GEBAEUDE_STAMM, TAB_ERGEBNIS };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Steht der Schritt? Alle Spalten und die Sicht in der Form des zehnten Durchgangs.</summary>
        public static bool Vollstaendig() =>
            SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) && SichtSteht();

        /// <summary>Steht die Gebäudespalte an der Tabelle (für Leser, die auf einem älteren Stand weiterlaufen)?</summary>
        public static bool Vorhanden(string tabelle) => DataRepository.SpalteVorhanden(tabelle, SPALTE_RAUMEINFLUSS);

        /// <summary>Stehen alle Ergebnisspalten an <see cref="TAB_ERGEBNIS"/>?</summary>
        public static bool ErgebnisspaltenVorhanden()
            => SPALTEN_ERGEBNIS.All(s => DataRepository.SpalteVorhanden(TAB_ERGEBNIS, s));

        /// <summary>Beginnt die Spaltenfolge der Sicht mit <see cref="GebaeudeSchema.SICHT_AK3"/>?</summary>
        public static bool SichtSteht()
        {
            List<string> ist = GebaeudeSchema.SichtSpalten();
            string[] soll = GebaeudeSchema.SICHT_AK3;
            return ist.Count >= soll.Length && ist.Take(soll.Length).SequenceEqual(soll, StringComparer.Ordinal);
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die Migration der
        /// Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar:</b> Eine
        /// stehende Spalte wird übergangen, die Sicht nur neu gebaut, wenn sie nicht die Form des zehnten
        /// Durchgangs hat; steht alles, öffnet er keinen Vorgang. <b>Kein DML an Bestandsdaten.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 8); den Sichtneubau nennt der Bericht.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            bool sicht = !SichtSteht();
            if (offen.Count == 0 && !sicht)
            {
                bericht?.Add("steht bereits - Heizkurve_Raumeinfluss an beiden Gebaeudetabellen, die Sicht und die " +
                             "Kennzahlen des Kreises an " + TAB_ERGEBNIS + "; nichts zu tun");
                return 0;
            }

            int angelegt = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    if (sicht) v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                        angelegt++;
                    }
                    if (sicht)
                    {
                        v.Ausfuehren(GebaeudeSchema.SQL_VIEW_AK3);
                        zeilen.Add("Sicht " + GebaeudeSchema.VIEW + " neu gebaut (" +
                                   GebaeudeSchema.SICHT_AK3.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
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
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
