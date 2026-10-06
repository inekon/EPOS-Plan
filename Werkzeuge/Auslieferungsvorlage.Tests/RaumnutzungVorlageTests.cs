using System;
using System.Globalization;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// <b>Der Katalog der Nutzungsprofile in der Auslieferungsvorlage</b> (Konzept Nutzungsprofile NP-F21, Schritt
    /// <see cref="RaumnutzungSchema.SCHRITT"/>): Mit <c>--kataloge readonly</c> fallen eigene Kategorien, Profile und
    /// Zuordnungen, eine ausgelieferte Zuordnung zeigt wieder auf ihr Saatprofil; der Prüfbericht zählt je Kategorie und
    /// meldet einen Kennwert in einer Normkategorie als Fehler.
    ///
    /// <para>Quelle ist eine KOPIE der Testdatenbank; die Datei im Repository wird nie geöffnet.</para>
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class RaumnutzungVorlageTests
    {
        /// <summary>Legt eine eigene Kategorie mit Profil an und stellt die ausgelieferte Zuordnung „Buero" darauf.</summary>
        private static void EigenesAnlegen()
        {
            Assert.True(RaumnutzungSchema.Vollstaendig(), "Die Testdatenbank steht vor Schritt " + RaumnutzungSchema.SCHRITT + ".");
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Raumnutzungskatalog\" (\"Bezeichner\", \"Art\") VALUES ('Eigene Probe', 'EIGEN')");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_Raumnutzungsprofil\" (\"ID_Katalog\", \"Bezeichner\", \"Heiz_Soll\") SELECT \"ID\", 'Probebüro', 21 " +
                "FROM \"Tab_Raumnutzungskatalog\" WHERE \"Bezeichner\" = 'Eigene Probe'");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_Raumnutzungszuordnung\" (\"Art\", \"Schluessel\", \"ID_Profil\") SELECT 'HOTTCAD_RAUMTYP', " +
                "'mrtProbe', \"ID\" FROM \"Tab_Raumnutzungsprofil\" WHERE \"Bezeichner\" = 'Probebüro'");
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_Raumnutzungszuordnung\" SET \"ID_Profil\" = (SELECT \"ID\" FROM \"Tab_Raumnutzungsprofil\" " +
                "WHERE \"Bezeichner\" = 'Probebüro') WHERE \"Art\" = 'IFC_KLASSE' AND \"Schluessel\" = 'Buero'");
        }

        [Fact]
        public void Readonly_raeumt_eigene_Zeilen_und_stellt_die_Zuordnung_zurueck()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");
            Bearbeiten(quelle, EigenesAnlegen);

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly", "--katalogleerung-zulassen");
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Schritt 3d — Katalog der Nutzungsprofile", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("entfernt: 1 eigene Zuordnung(en), 1 eigene(s) Profil(e), 1 eigene Kategorie(n); 1 ausgelieferte",
                            e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Kategorie EPOS-Muster", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("ok      ausgelieferte Saat der Nutzungsprofile vollstaendig: offen 0", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("ok      Werte in Normkategorien: Profile mit Kennwert 0", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("ok      eigene Zeilen des Katalogs der Nutzungsprofile nach --kataloge readonly: 0", e.Ausgabe,
                            StringComparison.Ordinal);

            Lesen(ziel, () =>
            {
                Assert.Equal(4, Zahl("SELECT COUNT(*) FROM \"Tab_Raumnutzungskatalog\""));
                Assert.Equal(RaumnutzungSaat.Profile.Count, Zahl("SELECT COUNT(*) FROM \"Tab_Raumnutzungsprofil\""));
                Assert.Equal(RaumnutzungSaat.Zuordnungen.Count, Zahl("SELECT COUNT(*) FROM \"Tab_Raumnutzungszuordnung\""));
                Assert.Equal(1, Zahl(
                    "SELECT COUNT(*) FROM \"Tab_Raumnutzungszuordnung\" z JOIN \"Tab_Raumnutzungsprofil\" p ON p.\"ID\" = z.\"ID_Profil\" " +
                    "WHERE z.\"Schluessel\" = 'Buero' AND p.\"Bezeichner\" = ?", new DbParam("@p", RaumnutzungSaat.BUERO)));
                Assert.Equal(0, RaumnutzungSchema.OffeneSaat());
            });
        }

        [Fact]
        public void Ein_Kennwert_in_einer_Normkategorie_ist_ein_Fehler()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");
            Bearbeiten(quelle, () => DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_Raumnutzungsprofil\" SET \"Heiz_Soll\" = 21 WHERE \"Nummer\" = '1' AND \"ID_Katalog\" = " +
                "(SELECT \"ID\" FROM \"Tab_Raumnutzungskatalog\" WHERE \"Art\" = 'DIN_V_18599_10')"));

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel);
            Assert.True(e.Code != 0, e.Alles);
            Assert.Contains("FEHLER  Werte in Normkategorien: Profile mit Kennwert 1", e.Ausgabe, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Eine Kategorie aus einer Projektdatei fällt auch im Modus „alle"</b> (NP4b, Q46, NP-F21): Ihre Werte stammen
        /// aus der lizenzierten Software des Anwenders und gehören nie in die Auslieferung — dieselbe Regel wie jede eigene
        /// Kategorie, unabhängig von der Vorgabe für die <c>_STAMM</c>-Kataloge (#160-E-1a). Die Werte der Probe sind
        /// runde Phantasiewerte.
        /// </summary>
        [Fact]
        public void Modus_alle_entfernt_eine_Kategorie_aus_einer_Projektdatei()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");
            Bearbeiten(quelle, () =>
            {
                Assert.True(RaumnutzungSchema.Vollstaendig(), "Die Testdatenbank steht vor Schritt " + RaumnutzungSchema.SCHRITT + ".");
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"Tab_Raumnutzungskatalog\" (\"Bezeichner\", \"Art\") VALUES ('Projektdatei Probe.sqproj', 'EIGEN')");
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"Tab_Raumnutzungsprofil\" (\"ID_Katalog\", \"Nummer\", \"Bezeichner\", \"Heiz_Soll\", \"Geraete_Leistung\") " +
                    "SELECT \"ID\", '1', 'Probeprofil', 20, 10 FROM \"Tab_Raumnutzungskatalog\" WHERE \"Bezeichner\" = 'Projektdatei Probe.sqproj'");
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel);
            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Schritt 3d — Katalog der Nutzungsprofile", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("ok      eigene Zeilen des Katalogs der Nutzungsprofile: 0", e.Ausgabe, StringComparison.Ordinal);
            Lesen(ziel, () =>
            {
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Raumnutzungskatalog\" WHERE \"Bezeichner\" = 'Projektdatei Probe.sqproj'"));
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Raumnutzungskatalog\" WHERE \"ReadOnly\" = 0"));
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Raumnutzungsprofil\" WHERE \"ReadOnly\" = 0"));
                Assert.Equal(RaumnutzungSaat.Profile.Count, Zahl("SELECT COUNT(*) FROM \"Tab_Raumnutzungsprofil\""));
            });
        }

        private static long Zahl(string sql, params DbParam[] p)
        {
            object w = DataRepository.ExecuteScalar(sql, p);
            return w == null || w == DBNull.Value ? 0 : Convert.ToInt64(w, CultureInfo.InvariantCulture);
        }

        private static void Bearbeiten(string datei, Action aktion)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrecht = Schreibnaht.Schreibrecht;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                Schreibnaht.WerkzeugFreigabe("Auslieferungsvorlage.Tests (Nutzungsprofilprobe vorbereiten)");
                aktion();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                Schreibnaht.Schreibrecht = schreibrecht;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
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
