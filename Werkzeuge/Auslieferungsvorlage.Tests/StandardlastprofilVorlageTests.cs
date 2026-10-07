using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// <b>Die BDEW-Standardlastprofile in der Auslieferung</b> (Schritt <see cref="StandardlastprofilSchema"/>, Welle
    /// SLP25): Mit den drei gesperrten Sätzen ist der Stromkatalog — Köpfe und Typprofile — kein leerer Paketteil mehr.
    /// Das Werkzeug läuft ohne die beiden benannten Ausnahmen durch, die ReadOnly-Bilanz zählt je drei gesperrte Sätze,
    /// das Katalogpaket führt sie mit dem Schlüssel aus dem Namen, und die Vorlage trägt sie gesperrt.
    ///
    /// <para>Die Testdatenbank steht auf dem Schritt und trägt die Sätze selbst; die beiden Stromtabellen stehen deshalb
    /// weder in <see cref="Werkzeuglauf.LEERE_PAKETTEILE_DER_TESTDATENBANK"/> noch in der Liste des CI-Laufs
    /// (<c>windows.yml</c>, Job <c>installer</c>). Der Fall läuft auf einer unveränderten Kopie der Testdatenbank.</para>
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class StandardlastprofilVorlageTests
    {
        private static readonly string[] STROM = { StandardlastprofilSchema.TAB_TYP, StandardlastprofilSchema.TAB_KOPF };

        [Fact]
        public void Mit_den_BDEW_Saetzen_ist_der_Stromkatalog_kein_leerer_Paketteil()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            Lesen(quelle, () => Assert.True(StandardlastprofilSchema.Vollstaendig(), "Die Testdatenbank traegt die BDEW-Saetze nicht."));

            Assert.All(STROM, t => Assert.DoesNotContain(t, Werkzeuglauf.LEERE_PAKETTEILE_DER_TESTDATENBANK));
            string ziel = o.Datei("Kenndaten.sqlite");
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.StartenMitAusnahmen(quelle, ziel);
            Assert.True(e.Code == 0, e.Alles);
            foreach (string t in STROM)
                Assert.DoesNotContain("leerer Paketteil " + t, e.Ausgabe, StringComparison.Ordinal);
            Assert.Matches(@"  Tab_Stromverbraucher_STAMM +44 / 3 / 41\r?\n", e.Ausgabe);
            Assert.Matches(@"  Tab_Stromverbrauchertyp_STAMM +43 / 3 / 40\r?\n", e.Ausgabe);

            Katalogpaket paket = Katalogpaket.Lesen(Katalogpaket.Pfad(ziel));
            foreach (string t in STROM)
            {
                Katalogtabelle kt = Katalogfassung.Tabelle(t);
                List<Katalogpaketsatz> saetze = paket.Tabellen.Single(x => x.Tabelle == t).Saetze;
                Assert.Equal(3, saetze.Count);
                foreach (StandardlastprofilSaat s in StandardlastprofilSchema.Saat)
                {
                    string schluessel = Katalogfassung.Schluesselstamm(kt, t == StandardlastprofilSchema.TAB_KOPF ? s.Bezeichner : s.Typname);
                    Assert.Contains(saetze, x => x.Schluessel == schluessel && x.Pruefsumme.Length == 64);
                }
            }

            Lesen(ziel, () =>
            {
                foreach (StandardlastprofilSaat s in StandardlastprofilSchema.Saat)
                {
                    Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_Stromverbraucher_STAMM\" WHERE \"Bezeichner\" = ? AND \"Typ\" = ? " +
                                         "AND \"ReadOnly\" = 1 AND \"Katalog_Schluessel\" IS NOT NULL",
                                         new DbParam("@b", s.Bezeichner), new DbParam("@t", s.Typname)));
                    Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_Stromverbrauchertyp_STAMM\" WHERE \"Typname\" = ? " +
                                         "AND \"ReadOnly\" = 1 AND \"Katalog_Schluessel\" IS NOT NULL", new DbParam("@t", s.Typname)));
                }
                Assert.True(StandardlastprofilSchema.Vollstaendig());
            });
        }

        // -----------------------------------------------------------------------------

        private static long Zahl(string sql, params DbParam[] p)
        {
            object w = DataRepository.ExecuteScalar(sql, p);
            return w == null || w == DBNull.Value ? 0 : Convert.ToInt64(w, CultureInfo.InvariantCulture);
        }

        private static void Lesen(string datei, Action aktion)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                aktion();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }
    }
}
