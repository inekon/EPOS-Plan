using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Wache über die Spalten von <c>Tab_Energieanlagen</c>: Jede Spalte ist entweder
    /// MODELLspalte (sie steht in <c>AnlagenSql.SQL_ANLAGE_INSERT</c>), eingeordnete
    /// FACHspalte (sie wird beim Speichern im Assistenten gerettet und bei Komponenten-
    /// übernahme und Flottenstudie aus der Quellzeile übertragen) oder AUSSCHLUSS
    /// (Schlüssel, Projektbezug, Ergebnis der Simulation).
    ///
    /// <para><b>Warum eine Liste neben dem Komplement.</b> Übertragen wird von selbst jede
    /// Spalte, die das Modell nicht nennt — eine neue Fachspalte kommt ohne Nacharbeit
    /// mit. Diese Wache verlangt trotzdem, dass sie hier eingeordnet wird: Eine neue
    /// Ergebnisspalte darf nicht mitwandern, und ein neuer Verweis auf eine projekteigene
    /// Zeile braucht seinen Eintrag in <see cref="AnlagenFachspalten.PROJEKTBEZUG"/>, sonst
    /// zeigte die Kopie in ein fremdes Projekt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class FachspaltenEinordnungWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>
        /// Die eingeordneten Fachspalten: KWKG (Schritte 22/61/105), Quellangaben,
        /// Steuer und Aufteilung, Kältemaschine (KU3), Sondenfeld (Schritt 195).
        /// </summary>
        private static readonly string[] FACHSPALTEN =
        {
            "KWKG_Stichtag", "KWKG_Inbetriebnahme", "KWKG_Anlagenart", "KWKG_Eigenstromfall",
            "KWKG_Satz_Einspeisung", "KWKG_Satz_Eigen", "KWKG_Vbh_Kontingent", "KWKG_Vbh_Jahresdeckel",
            "KWKG_Kostenanteil", "KWKG_Abwaermeabfuhr", "KWKG_Stromkennzahl",
            "WQ_Anschlusshoehe", "WQ_ID_Quellprofil", "WQ_TemperaturModus",
            "Energiesteuer_Wahl", "Aufteilung_Methode", "Hilfsenergie_Anteil",
            "ID_Kaeltemaschine", "Kaeltemaschine_Anzahl",
            "WQ_Sondenabstand", "WQ_Bohrlochdurchmesser", "WQ_Bohrlochwiderstand",
            "WQ_Kopfueberdeckung", "WQ_Betrachtungsjahr", "WQ_Sondenanordnung"
        };

        private const string HINWEIS =
            " Einordnen: ins Modell (AnlagenSql.SQL_ANLAGE_INSERT), als Fachspalte (FACHSPALTEN " +
            "dieser Wache; ein Verweis auf eine projekteigene Zeile zusätzlich in " +
            "AnlagenFachspalten.PROJEKTBEZUG) oder als Ausschluss (AnlagenFachspalten.AUSSCHLUSS " +
            "bzw. ERGEBNIS für Werte, die die Simulation schreibt).";

        private static List<string> Spalten()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name FROM pragma_table_info(?)", new DbParam("@t", "Tab_Energieanlagen"));
            return dt.Rows.Cast<DataRow>()
                     .Select(r => Convert.ToString(r["name"], CultureInfo.InvariantCulture)).ToList();
        }

        /// <summary>Jede Spalte des Schemas ist eingeordnet — genau einmal.</summary>
        [Fact]
        public void Jede_Spalte_der_Anlagentabelle_ist_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            HashSet<string> modell = AnlagenFachspalten.Modellspalten();
            var fach = new HashSet<string>(FACHSPALTEN, StringComparer.OrdinalIgnoreCase);
            var ausschluss = new HashSet<string>(AnlagenFachspalten.AUSSCHLUSS
                .Concat(AnlagenFachspalten.ERGEBNIS), StringComparer.OrdinalIgnoreCase);

            var offen = new List<string>();
            var doppelt = new List<string>();
            foreach (string spalte in Spalten())
            {
                int n = (modell.Contains(spalte) ? 1 : 0) + (fach.Contains(spalte) ? 1 : 0) +
                        (ausschluss.Contains(spalte) && !modell.Contains(spalte) ? 1 : 0);
                if (n == 0) offen.Add(spalte);
                else if (n > 1) doppelt.Add(spalte);
            }

            Assert.True(offen.Count == 0, "Nicht eingeordnete Spalte(n): " + string.Join(", ", offen) + "." + HINWEIS);
            Assert.True(doppelt.Count == 0, "Doppelt eingeordnet: " + string.Join(", ", doppelt) + ".");
        }

        /// <summary>
        /// Die Fachspalten der Wache sind genau das, was übertragen wird — keine Spalte, die
        /// das Schema nicht mehr führt, und keine, die ins Modell gewandert ist.
        /// </summary>
        [Fact]
        public void Die_uebertragenen_Spalten_sind_genau_die_eingeordneten_Fachspalten()
        {
            if (!_db.Vorhanden) return;

            var erwartet = new HashSet<string>(FACHSPALTEN, StringComparer.OrdinalIgnoreCase);
            erwartet.ExceptWith(AnlagenFachspalten.ERGEBNIS);
            var ist = new HashSet<string>(AnlagenFachspalten.UebertragbareSpalten(), StringComparer.OrdinalIgnoreCase);

            Assert.True(erwartet.SetEquals(ist),
                "Fehlt in der Übertragung: " + string.Join(", ", erwartet.Except(ist)) +
                "; nicht eingeordnet: " + string.Join(", ", ist.Except(erwartet)) + "." + HINWEIS);
            Assert.True(new HashSet<string>(WizardCtrl.Fachspalten(), StringComparer.OrdinalIgnoreCase)
                            .IsSupersetOf(ist), "Assistent und Übernahme retten verschiedene Spalten.");
        }

        /// <summary>
        /// Jeder Fremdschlüssel einer Fachspalte auf eine Tabelle mit <c>ID_Projekt</c> steht
        /// in <see cref="AnlagenFachspalten.PROJEKTBEZUG"/> — sonst zeigte die übernommene
        /// Anlage in das Quellprojekt.
        /// </summary>
        [Fact]
        public void Jeder_Projektverweis_einer_Fachspalte_wird_abgebildet()
        {
            if (!_db.Vorhanden) return;

            var fach = new HashSet<string>(AnlagenFachspalten.UebertragbareSpalten(), StringComparer.OrdinalIgnoreCase);
            DataTable fks = DataRepository.GetDataTable(
                "SELECT \"from\" AS Spalte, \"table\" AS Ziel FROM pragma_foreign_key_list(?)",
                new DbParam("@t", "Tab_Energieanlagen"));

            var fehlend = new List<string>();
            foreach (DataRow r in fks.Rows)
            {
                string spalte = Convert.ToString(r["Spalte"], CultureInfo.InvariantCulture);
                string ziel = Convert.ToString(r["Ziel"], CultureInfo.InvariantCulture);
                if (!fach.Contains(spalte)) continue;

                bool projekteigen = DataRepository.SpaltenVonTabelle(ziel)
                    .Any(s => string.Equals(s, "ID_Projekt", StringComparison.OrdinalIgnoreCase));
                if (!projekteigen) continue;

                if (!AnlagenFachspalten.PROJEKTBEZUG.TryGetValue(spalte, out string tabelle) ||
                    !string.Equals(tabelle, ziel, StringComparison.OrdinalIgnoreCase))
                    fehlend.Add(spalte + " -> " + ziel);
            }

            Assert.True(fehlend.Count == 0, "Projektverweis ohne Abbildung: " + string.Join(", ", fehlend) + "." + HINWEIS);
        }
    }
}
