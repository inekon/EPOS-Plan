using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // EV1 - DER WIRKSAME U-WERT DER BODENPLATTE ALS VORGABE (Entscheid E65 vom 03.10.2026).
    //
    // WOZU. Die Erdreichkorrektur nach DIN EN ISO 13370 ist Standard (Basis R34). Je Gebaeude kann der
    // Anwender den wirksamen U-Wert der Bodenplatte samt Erdreich vorgeben - etwa aus einem
    // Energieausweis oder einer eigenen Rechnung. Ein Schritt:
    //
    //   Tab_Gebaeude, Tab_Gebaeude_STAMM   Erdreich_U_Wirksam   REAL, > 0   NULL = Rechnung nach 13370
    //
    // Gesetzt, nimmt die Erdreichrechnung den Wert als U_g der Bodenbauteile am Erdreich und rechnet kein
    // B' (Erdreichwiderstand.Bauteilsatz, Umfangsquelle Vorgabe).
    //
    // DER NEUNTE SICHTNEUBAU. Der Lauf liest das Gebaeude ueber Abfrage_Projektgebaeude; der Wert steht
    // dort HINTER der manuellen Aufheizzeit (104 Spalten, GebaeudeSchema.SQL_VIEW_ERDREICH_VORGABE). Weil
    // aeltere Durchgaenge die Sicht in ihrer Form neu bauen, laeuft dieser Schritt in Migration, Werkzeug
    // und Testkopie ZULETZT und baut die Sicht, sobald sie nicht seine Form hat.
    //
    // KEIN DML AN BESTANDSDATEN. Jede Bestandszeile steht danach auf NULL - jedes Projekt rechnet wie
    // vorher, der Referenzlauf bleibt byte-gleich. Alles in EINEM Vorgang mit abgeschalteten
    // Fremdschluesseln: Scheitert ein Teil, bleibt die Datei, wie sie war; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>EV1</b> — der wirksame U-Wert der Bodenplatte als Vorgabe je Gebäude (E65): EINE Quelle für
    /// Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ErdreichVorgabeSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der Schritt hinter der
        /// zuletzt auf <c>origin</c> liegenden Klasse.
        /// </summary>
        public const int SCHRITT = PufferAuslegungErgaenzungSchema.SCHRITT + 1;

        /// <summary>Die Spalte an beiden Gebäudetabellen.</summary>
        public const string SPALTE = GebaeudeSchema.SPALTE_ERDREICH_U_WIRKSAM;

        /// <summary>Typ samt Prüfklausel: REAL, NULL oder größer als null.</summary>
        public const string TYP = "REAL CHECK (\"" + SPALTE + "\" IS NULL OR \"" + SPALTE + "\" > 0)";

        /// <summary>Die zwei Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (GebaeudeSchema.TAB_GEBAEUDE, SPALTE, TYP),
            (GebaeudeSchema.TAB_GEBAEUDE_STAMM, SPALTE, TYP),
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => new[] { GebaeudeSchema.TAB_GEBAEUDE, GebaeudeSchema.TAB_GEBAEUDE_STAMM };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Steht der Schritt? Beide Spalten und die Sicht in der Form des neunten Durchgangs.</summary>
        public static bool Vollstaendig() =>
            SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) && SichtSteht();

        /// <summary>Steht die Spalte an der Tabelle (für Leser, die auf einem älteren Stand weiterlaufen)?</summary>
        public static bool Vorhanden(string tabelle) => DataRepository.SpalteVorhanden(tabelle, SPALTE);

        /// <summary>Beginnt die Spaltenfolge der Sicht mit <see cref="GebaeudeSchema.SICHT_ERDREICH_VORGABE"/>?</summary>
        public static bool SichtSteht()
        {
            List<string> ist = GebaeudeSchema.SichtSpalten();
            string[] soll = GebaeudeSchema.SICHT_ERDREICH_VORGABE;
            return ist.Count >= soll.Length && ist.Take(soll.Length).SequenceEqual(soll, StringComparer.Ordinal);
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die Migration der
        /// Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar:</b> Eine
        /// stehende Spalte wird übergangen, die Sicht nur neu gebaut, wenn sie nicht die Form des neunten
        /// Durchgangs hat; steht alles, öffnet er keinen Vorgang. <b>Kein DML an Bestandsdaten.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 2); den Sichtneubau nennt der Bericht.</returns>
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
                bericht?.Add("steht bereits - Erdreich_U_Wirksam an beiden Gebaeudetabellen und die Sicht; nichts zu tun");
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
                        v.Ausfuehren(GebaeudeSchema.SQL_VIEW_ERDREICH_VORGABE);
                        zeilen.Add("Sicht " + GebaeudeSchema.VIEW + " neu gebaut (" +
                                   GebaeudeSchema.SICHT_ERDREICH_VORGABE.Length.ToString(CultureInfo.InvariantCulture) +
                                   " Spalten)");
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
