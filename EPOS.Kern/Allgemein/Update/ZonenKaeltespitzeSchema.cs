using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // MZ-REST - DIE KAELTESPITZE JE ZONE (Schritt 185).
    //
    // WOZU. Der Lauf bildet je Zone mit wirksamer Kuehlung die hoechste Stunde des Kaeltebedarfs und die
    // Stunden mit Kaeltebedarf (KU3-3); gespeichert wurde nur die Jahreskuehlenergie. Damit Bedarfsdialog,
    // Bericht und Export (IFC EPOS_Ergebnis.Kaeltelast je Raum, gbXML CoolingLoad je Zone) die Spitze aus dem
    // gespeicherten Lauf lesen, nimmt das Ergebnis der Zone beide Werte auf:
    //
    //   Tab_ErgebnisZone  Kaeltespitze_kW   REAL     >= 0       NULL = ohne wirksame Kuehlung
    //                     Kuehlstunden      INTEGER  0..8760    NULL = ohne wirksame Kuehlung
    //
    // KEIN DML, KEIN SICHTNEUBAU. Jede Bestandszeile steht danach auf NULL; ohne Kuehlung bleiben beide leer,
    // der Referenzlauf bleibt byte-gleich. Alles in EINEM Vorgang; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>MZ-Rest</b> — die Kältespitze je Zone: EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis
    /// (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ZonenKaeltespitzeSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 185).
        /// </summary>
        public const int SCHRITT = KaeltestromabrechnungSchema.SCHRITT + 1;

        /// <summary>Die Ergebnistabelle der Zonen.</summary>
        public const string TAB_ERGEBNIS_ZONE = ZonenkopplungSchema.TAB_ERGEBNIS;

        /// <summary>Die höchste Stunde des Kältebedarfs der Zone [kW]; NULL = ohne wirksame Kühlung.</summary>
        public const string SPALTE_KAELTESPITZE = "Kaeltespitze_kW";

        /// <summary>Stunden mit Kältebedarf der Zone [h]; NULL = ohne wirksame Kühlung.</summary>
        public const string SPALTE_KUEHLSTUNDEN = "Kuehlstunden";

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisZone</c> nach diesem Schritt.</summary>
        public const int SPALTENZAHL_ERGEBNIS_ZONE = ZonenUebergabeSchema.SPALTENZAHL_ERGEBNIS_ZONE + 2;

        /// <summary>Die Spalten des Schritts in Anlagereihenfolge: Tabelle, Spalte, Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ERGEBNIS_ZONE, SPALTE_KAELTESPITZE,
                "REAL CHECK (\"" + SPALTE_KAELTESPITZE + "\" IS NULL OR \"" + SPALTE_KAELTESPITZE + "\" >= 0)"),
            (TAB_ERGEBNIS_ZONE, SPALTE_KUEHLSTUNDEN,
                "INTEGER CHECK (\"" + SPALTE_KUEHLSTUNDEN + "\" IS NULL OR \"" + SPALTE_KUEHLSTUNDEN + "\" BETWEEN 0 AND 8760)"),
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ERGEBNIS_ZONE };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen beide Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Ohne
        /// <c>Tab_ErgebnisZone</c> wirft er benannt.
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
                bericht?.Add("steht bereits - Kaeltespitze und Kuehlstunden an Tab_ErgebnisZone; nichts zu tun");
                return 0;
            }

            int angelegt = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                        angelegt++;
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
