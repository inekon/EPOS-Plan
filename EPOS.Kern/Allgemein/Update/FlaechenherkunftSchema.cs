using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // HERKUNFT DER BAUTEILFLAECHE (Schritt 197; Abstimmung G5, Anforderung A4, Antwort F2).
    //
    // WOZU. Der Gebaeudeimport kennt je Bauteilflaeche den WEG, auf dem sie entstanden ist
    // (Flaechenherkunft: Mengensatz, Raumgrenze, Koerper, Schematisch). Die vorhandene Spalte
    // Tab_Bauteil.Herkunft nennt die QUELLE (IFC, Katalog, Vorgabe ...), nicht den Weg der Flaeche.
    // Pruefregeln und Bericht sollen ihn nennen koennen - deshalb wird er gespeichert:
    //
    //   Tab_Bauteil  Flaechenherkunft  TEXT  MENGENSATZ | RAUMGRENZE | KOERPER | SCHEMATISCH
    //                                        NULL = Bestand, von Hand angelegt oder geaendert
    //
    // Die Wertliste steht EINMAL: FlaechenherkunftWerte (Import/Gebaeude/Flaechenherkunft.cs).
    //
    // KEIN DML, KEIN NEUBAU. Reines ADD COLUMN, nullbar, mit Pruefklausel; jede Bestandszeile steht
    // danach auf NULL. Der Rechenweg liest die Spalte nicht - ergebnisneutral. Wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl); geschrieben und gelesen von GebaeudeZonenCtrl.
    // ====================================================================================

    /// <summary>
    /// <b>Herkunft der Bauteilfläche</b> — die Spalte <c>Tab_Bauteil.Flaechenherkunft</c>: EINE Quelle für
    /// Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class FlaechenherkunftSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 197): der Schritt
        /// hinter <see cref="StandardlastprofilPvSchema.SCHRITT"/>.
        /// </summary>
        public const int SCHRITT = StandardlastprofilPvSchema.SCHRITT + 1;

        /// <summary>Die Bauteiltabelle.</summary>
        public const string TAB_BAUTEIL = SchemaKatalog.TAB_BAUTEIL;

        /// <summary><c>Flaechenherkunft</c> (<see cref="WERTE"/>); NULL = nicht aus einem Import oder von Hand geändert.</summary>
        public const string SPALTE = "Flaechenherkunft";

        /// <summary>Die zulässigen Werte — die von <see cref="FlaechenherkunftWerte"/>, in der Reihenfolge der Aufzählung.</summary>
        public static readonly IReadOnlyList<string> WERTE = Enum.GetValues(typeof(Flaechenherkunft))
            .Cast<Flaechenherkunft>().Select(h => FlaechenherkunftWerte.Wert(h)).ToList();

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Typ samt Prüfklausel der Spalte.</summary>
        public static string Typ => "TEXT CHECK (" + Q(SPALTE) + " IS NULL OR " + Q(SPALTE) + " IN (" +
                                    string.Join(",", WERTE.Select(w => "'" + w + "'")) + "))";

        /// <summary>Die Anweisung, die die Spalte anlegt.</summary>
        public static string Anlegen() => "ALTER TABLE " + Q(TAB_BAUTEIL) + " ADD COLUMN " + Q(SPALTE) + " " + Typ;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_BAUTEIL };

        /// <summary>Steht die Spalte?</summary>
        public static bool Vollstaendig() => DataRepository.SpalteVorhanden(TAB_BAUTEIL, SPALTE);

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten (0 oder 1). Ohne
        /// <c>Tab_Bauteil</c> wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (Vollstaendig())
            {
                bericht?.Add("steht bereits - " + TAB_BAUTEIL + "." + SPALTE + "; nichts zu tun");
                return 0;
            }
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    v.Ausfuehren(Anlegen());
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            GebaeudeZonenanschluss.ProbeVerwerfen();
            bericht?.Add(TAB_BAUTEIL + "." + SPALTE + " angelegt (leer)");
            bericht?.Add("KEIN DML an Bestandsdaten - leere Spalte heisst Bestand bzw. von Hand, der Referenzlauf bleibt gleich");
            return 1;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
