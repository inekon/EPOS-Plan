using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SCHRITT 233 - HERKUNFT DES U-WERTS JE BAUTEIL (Welle MA-6b, Entscheid E149). Angemeldet als 233, haengt ueber + 1 an
    // 232 BauteilkatalogAbschnittSchema - die Kette laeuft ueber + 1.
    //
    // ANLASS. Der Wiederimport (MehrzonenuebernahmeCtrl.Zusammenfuehren, Rangfolge Hand > Datei > Vorgabe) erkennt einen
    // von Hand eingetragenen U-Wert nur am Aufbauwechsel oder an der Herkunft MANUELL des ganzen Bauteils. Ein U-Wert, der
    // ohne Aufbauwechsel von Hand gesetzt wurde, fiel an den Wert der Datei.
    //
    // WAS. Tab_Bauteil.U_Herkunft und Tab_Bauteil_STAMM.U_Herkunft  TEXT, nullbar,
    //   CHECK (U_Herkunft IS NULL OR U_Herkunft IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ')).
    // Der Katalogzwilling bekommt die Spalte mit, weil derselbe Schreibweg (GebaeudeZonenCtrl.Schreiben) beide Tabellen mit
    // derselben Spaltenliste bedient. NULL = unbekannt (Bestand), verhaelt sich wie bisher.
    //
    // ERGEBNISNEUTRAL. Kein DML an Bestandsdaten; der Rechenweg liest die Spalte nicht, kein Referenzprojekt fuehrt einen Wert.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Herkunft des U-Werts</b> (MA-6b, E149): <c>U_Herkunft</c> an <c>Tab_Bauteil</c> und
    /// <c>Tab_Bauteil_STAMM</c> — woher der U-Wert eines Bauteils stammt (von Hand, Aufbau aus dem Katalog, Datei, Vorgabe).
    /// Wiederholbar, ergebnisneutral, kein DML. Schritt 233, hängt an 232 <see cref="BauteilkatalogAbschnittSchema"/>.
    /// </summary>
    public static class BauteilUHerkunftSchema
    {
        /// <summary>
        /// Die Nummer des Schritts — angemeldet als 233; hängt an <see cref="BauteilkatalogAbschnittSchema"/> (232) und ist
        /// das Ende der Kette. Die Nummer steht allein hier.
        /// </summary>
        public const int SCHRITT = BauteilkatalogAbschnittSchema.SCHRITT + 1;

        /// <summary>Die neue Spalte.</summary>
        public const string SPALTE = "U_Herkunft";

        /// <summary>Die beiden Bauteiltabellen — Projekt vor Katalog.</summary>
        public static readonly IReadOnlyList<string> TABELLEN = new[] { SchemaKatalog.TAB_BAUTEIL, SchemaKatalog.TAB_BAUTEIL_STAMM };

        /// <summary>Die zulässigen Werte (die sechs Herkunftswerte, <see cref="DbWerte.HERKUENFTE"/>).</summary>
        public static IReadOnlyList<string> WERTE => DbWerte.HERKUENFTE;

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Der Typ der Spalte samt <c>CHECK</c> (nullbar, damit <c>ADD COLUMN</c> an der STRICT-Tabelle zulässig ist).</summary>
        public static string Typ => "TEXT CHECK (" + Q(SPALTE) + " IS NULL OR " + Q(SPALTE) + " IN (" +
                                    string.Join(",", WERTE.Select(w => "'" + w + "'")) + "))";

        /// <summary>Die ADD-COLUMN-Anweisung an einer Tabelle.</summary>
        public static string Anlegen(string tabelle) => "ALTER TABLE " + Q(tabelle) + " ADD COLUMN " + Q(SPALTE) + " " + Typ;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => TABELLEN;

        /// <summary>Was noch offen ist: je fehlende Spalte 1 (−1: eine Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            return TABELLEN.Count(t => !DataRepository.SpalteVorhanden(t, SPALTE));
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt eine
        /// vorausgesetzte Tabelle, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            List<string> offen = TABELLEN.Where(t => !DataRepository.SpalteVorhanden(t, SPALTE)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - U_Herkunft an Tab_Bauteil(_STAMM); nichts zu tun");
                return 0;
            }
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach (string t in offen) v.Ausfuehren(Anlegen(t));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            GebaeudeZonenanschluss.ProbeVerwerfen();
            if (bericht != null)
            {
                foreach (string t in offen) bericht.Add(t + "." + SPALTE + " angelegt (leer)");
                bericht.Add("KEIN DML an Bestandsdaten - leere Spalte heisst unbekannt (Bestand), der Referenzlauf bleibt gleich");
            }
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
