using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Planwächter der Kopierwege</b> (Gebäudesimulation G6a, Welle 2; Softwarearchitektur 2.6).
    /// Die Auto-Erkennung von <see cref="ProjektDuplizierenCtrl.ErmittlePlan"/> hängt eine Tabelle ohne
    /// eigenen Projektbezug über die ERSTE Spalte mit deklarierter Beziehung auf eine Plantabelle an —
    /// bei zwei oder mehr solchen Fremdschlüsseln ist das eine Lotterie der Spaltenreihenfolge: Über
    /// <c>ID_Baustoff</c> gefiltert fielen alle Schichten mit freier Eingabe aus der Kopie, über
    /// <c>ID_Aufbau</c> alle Bauteile ohne Aufbau, und der Projekttransfer erbte den Verlust.
    ///
    /// <para><b>Die Regel:</b> Jede Tabelle mit zwei oder mehr deklarierten Fremdschlüsseln auf
    /// Plantabellen steht ausdrücklich in <c>KINDER</c> oder auf der begründeten
    /// <see cref="AUSNAHMEN"/>-Liste. Eine Tabelle mit eigenem <c>ID_Projekt</c> bzw.
    /// <c>ProjektID</c> ist davon benannt ausgenommen: <c>ErmittlePlan</c> filtert sie über ihr eigenes
    /// Projekt, bevor die Auto-Erkennung greift — ihre Fremdschlüssel werden nur versetzt, nie zum
    /// Filter. Rechenergebnisse (<see cref="ProjektDuplizierenCtrl.IstErgebnisTabelle"/>) kopiert der
    /// Kopierlauf nicht; sie sind ausgenommen.</para>
    ///
    /// <para>Der Wächter LIEST nur: <c>KINDER</c>, <c>FK_MAP</c> und <c>FK_OVERRIDE</c> bleiben, wie sie
    /// sind, und werden ohne Rücksicht auf Groß- und Kleinschreibung verglichen — wie die Sammlungen
    /// selbst. Die Gegenprobe hält fest, dass er etwas prüft: Die Kinder der Gebäudesimulation
    /// (<c>Tab_Bauteil</c>, <c>Tab_Bauteilschicht</c>, <c>Tab_Importzuordnung</c>) fallen unter die Regel
    /// und stehen in <c>KINDER</c>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektplanKinderWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>
        /// Tabellen mit zwei oder mehr Fremdschlüsseln auf Plantabellen, ohne eigenes <c>ID_Projekt</c>,
        /// die bewusst NICHT in <c>KINDER</c> stehen — je mit Grund. Leer: Jede solche Tabelle steht in
        /// <c>KINDER</c>.
        /// </summary>
        private static readonly Dictionary<string, string> AUSNAHMEN = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
        };

        private static IDictionary Sammlung(string feld)
            => (IDictionary)typeof(ProjektDuplizierenCtrl).GetField(feld, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

        private static HashSet<string> Schluessel(string feld)
            => new HashSet<string>(Sammlung(feld).Keys.Cast<string>(), StringComparer.OrdinalIgnoreCase);

        /// <summary>Je Tabelle der Datenbank die Spalten mit deklarierter Beziehung und ihre Zieltabelle.</summary>
        private static Dictionary<string, List<(string Spalte, string Ziel)>> Fremdschluessel()
        {
            var je = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
            DataTable tabellen = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name");
            foreach (DataRow t in tabellen.Rows)
            {
                string name = Convert.ToString(t["name"]);
                var liste = new List<(string, string)>();
                DataTable fk = DataRepository.FremdschluesselListe(name);
                if (fk != null)
                    foreach (DataRow r in fk.Rows)
                        if (r["Quellspalte"] is string s && r["Zieltabelle"] is string z) liste.Add((s, z));
                je[name] = liste;
            }
            return je;
        }

        private static bool EigenerProjektbezug(string tabelle)
        {
            DataTable spalten = DataRepository.GetDataTable("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            return spalten.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0]))
                          .Any(s => string.Equals(s, "ID_Projekt", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(s, "ProjektID", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Jede_Tabelle_mit_zwei_Fremdschluesseln_auf_Plantabellen_steht_in_KINDER()
        {
            if (!_db.Vorhanden) return;
            var plan = new HashSet<string>(new ProjektDuplizierenCtrl().ErmittlePlan().Select(s => s.Tabelle), StringComparer.OrdinalIgnoreCase);
            HashSet<string> kinder = Schluessel("KINDER");

            var unterDerRegel = new List<string>();
            var verstoesse = new List<string>();
            foreach (KeyValuePair<string, List<(string Spalte, string Ziel)>> t in Fremdschluessel())
            {
                if (ProjektDuplizierenCtrl.IstErgebnisTabelle(t.Key)) continue;
                List<string> spalten = t.Value.Where(f => plan.Contains(f.Ziel) && !string.Equals(f.Ziel, t.Key, StringComparison.OrdinalIgnoreCase))
                                              .Select(f => f.Spalte).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (spalten.Count < 2) continue;
                unterDerRegel.Add(t.Key);
                if (kinder.Contains(t.Key) || EigenerProjektbezug(t.Key) || AUSNAHMEN.ContainsKey(t.Key)) continue;
                verstoesse.Add(t.Key + " (" + string.Join(", ", spalten) + ")");
            }

            Assert.True(verstoesse.Count == 0,
                "Diese Tabellen tragen zwei oder mehr Fremdschlüssel auf Plantabellen, aber keinen KINDER-Eintrag " +
                "(ProjektDuplizierenCtrl) und keinen begründeten Ausnahmegrund — die Auto-Erkennung filterte sie über " +
                "die erste Spalte: " + string.Join("; ", verstoesse));

            // Gegenprobe: Der Wächter prüft etwas.
            foreach (string t in new[] { SchemaKatalog.TAB_BAUTEIL, SchemaKatalog.TAB_BAUTEILSCHICHT, SchemaKatalog.TAB_IMPORTZUORDNUNG })
            {
                Assert.Contains(t, unterDerRegel, StringComparer.OrdinalIgnoreCase);
                Assert.Contains(t, kinder);
            }
        }

        /// <summary>Jede Ausnahme nennt eine Tabelle, die es gibt, die unter die Regel fällt und nicht in KINDER steht.</summary>
        [Fact]
        public void Die_Ausnahmeliste_ist_begruendet_und_nicht_veraltet()
        {
            if (!_db.Vorhanden) return;
            Dictionary<string, List<(string Spalte, string Ziel)>> fks = Fremdschluessel();
            HashSet<string> kinder = Schluessel("KINDER");
            foreach (KeyValuePair<string, string> a in AUSNAHMEN)
            {
                Assert.False(string.IsNullOrWhiteSpace(a.Value), a.Key + ": Die Ausnahme braucht einen Grund.");
                Assert.True(fks.ContainsKey(a.Key), a.Key + ": Die Tabelle gibt es nicht mehr.");
                Assert.DoesNotContain(a.Key, kinder);
            }
        }

        /// <summary>KINDER, FK_MAP und FK_OVERRIDE vergleichen ohne Rücksicht auf Groß- und Kleinschreibung.</summary>
        [Fact]
        public void Die_Sammlungen_vergleichen_ohne_Gross_und_Kleinschreibung()
        {
            foreach (string feld in new[] { "KINDER", "FK_MAP", "FK_OVERRIDE" })
            {
                object comparer = Sammlung(feld).GetType().GetProperty("Comparer")!.GetValue(Sammlung(feld))!;
                Assert.Same(StringComparer.OrdinalIgnoreCase, comparer);
            }
            Assert.Contains("tab_zone", Schluessel("KINDER"));
            Assert.Contains("TAB_BAUTEILSCHICHT", Schluessel("KINDER"));
        }
    }
}
