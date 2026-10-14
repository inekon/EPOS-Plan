using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Befund PW6 — ein Profil ohne Typbezug wird übersprungen, nicht die Bedarfsart
    /// abgebrochen</b> (Papier „Verbesserungen 29.09.2026", Anwenderentscheid).
    ///
    /// <para><b>Der Fehler.</b> <see cref="ProfilBedarf.Rechnen"/> brach die Profilschleife
    /// am ersten Profil ohne Typ per <c>break</c> ab. Die Profile DAVOR blieben aufaddiert,
    /// die danach fehlten — der Wärmezweig wertete die Rückgabe nicht aus und rechnete mit
    /// einer Teilsumme weiter, die an der Reihenfolge der Zuordnungszeilen hing.</para>
    ///
    /// <para><b>Die Regel.</b> Das Profil ohne Typ wird mit benannter Warnung übersprungen
    /// (Anteil 0), die übrigen rechnen vollständig — in beiden Reihenfolgen dieselbe Summe.
    /// Die Profilroutine ist für Prozesswärme, Brauchwasser und Stromverbraucher dieselbe
    /// (<see cref="ProfilQuelle"/>); der Stromzweig bricht deshalb ebenfalls nicht mehr ab.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProfilOhneTypTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Projekt 1041: Zuordnung „Hotel_1" (Zeile 2, Summe 30) auf die Kopie 13.</summary>
        private const int P1041 = 1041;
        private const int KOPIE_1041 = 13;

        /// <summary>Freie ID für die Prozesskopie ohne Typ.</summary>
        private const int OHNE_TYP = 11;

        /// <summary>Projekt 1047: eine Stromverbraucher-Zuordnung (Zeile 10326) auf die Kopie 1105644.</summary>
        private const int P1047 = 1047;
        private const int STROM_KOPIE_1047 = 1105644;

        /// <summary>Freie ID für die Stromverbraucherkopie ohne Typ.</summary>
        private const int STROM_OHNE_TYP = 1999001;

        private static double ProzessLauf(int projekt)
        {
            var sim = new SimulationWaermebedarf { m_ID_Projekt = projekt };
            sim.Prozesswaerme_berechnen();
            return sim.prozesswerte.Sum();
        }

        private static string Monate() => string.Join(", ", Enumerable.Range(1, 12).Select(i => "Monat_" + i));

        /// <summary>
        /// Eine Prozesskopie ohne Typ im Projekt 1041 (Monatswerte der zugeordneten Kopie) und
        /// ihre Zuordnungszeile mit der ID <paramref name="zeilenId"/> — kleiner als die
        /// vorhandene Zeile 2 heißt „vorne", größer heißt „hinten" (der Lauf liest ORDER BY ID).
        /// </summary>
        private static void ProzessOhneTypZuordnen(int zeilenId)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Prozesswaerme (ID, ID_Projekt, Bezeichner, Typ, Beschreibung, " + Monate() + ", ReadOnly) " +
                "SELECT ?, ID_Projekt, 'Ohne Typ', NULL, Beschreibung, " + Monate() + ", 0 " +
                "FROM Tab_Prozesswaerme WHERE ID = ?",
                new DbParam("?", OHNE_TYP), new DbParam("?", KOPIE_1041)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Z_Projekt_Prozesswaerme (ID, ID_Projekt, ID_Prozesswaerme, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)",
                new DbParam("?", zeilenId), new DbParam("?", P1041), new DbParam("?", OHNE_TYP),
                new DbParam("?", "Ohne Typ"), new DbParam("?", 50.0)));
        }

        [Theory]
        [InlineData(1)]     // das Profil ohne Typ VOR „Hotel_1"
        [InlineData(99)]    // das Profil ohne Typ NACH „Hotel_1"
        public void Prozesswaerme_ohne_Typ_wird_uebersprungen_in_jeder_Reihenfolge(int zeilenId)
        {
            if (!_db.Vorhanden) return;

            double ohne = ProzessLauf(P1041);
            Assert.True(ohne > 0.0, "Projekt 1041 rechnet „Hotel_1“ mit Prozesswärme.");

            ProzessOhneTypZuordnen(zeilenId);

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            double mit = ProzessLauf(P1041);

            // „Hotel_1" rechnet vollständig, das Profil ohne Typ trägt 0 bei — vorne wie hinten.
            Assert.Equal(ohne, mit, 9);
            Assert.Contains(protokoll.Warnungen, w => w.Contains("Ohne Typ"));
        }

        [Fact]
        public void Stromverbraucher_ohne_Typ_bricht_den_Strombedarf_nicht_ab()
        {
            if (!_db.Vorhanden) return;

            double[] vorher = new SimulationStrombedarf { m_ID_Projekt = P1047 }.Stromprofil_Strombedarf_berechnen();
            Assert.NotNull(vorher);
            double summeVorher = vorher.Sum();
            Assert.True(summeVorher > 0.0);

            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Stromverbraucher (ID, ID_Projekt, Bezeichner, Typ, Beschreibung, " + Monate() + ") " +
                "SELECT ?, ID_Projekt, 'Ohne Typ', NULL, Beschreibung, " + Monate() + " " +
                "FROM Tab_Stromverbraucher WHERE ID = ?",
                new DbParam("?", STROM_OHNE_TYP), new DbParam("?", STROM_KOPIE_1047)));
            // Zeile 1 steht VOR der vorhandenen Zuordnung - der alte Abbruch hätte alles verworfen.
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Z_Projekt_Stromverbraucher (ID, ID_Projekt, ID_Stromverbraucher, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)",
                new DbParam("?", 1), new DbParam("?", P1047), new DbParam("?", STROM_OHNE_TYP),
                new DbParam("?", "Ohne Typ"), new DbParam("?", 20.0)));

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            double[] nachher = new SimulationStrombedarf { m_ID_Projekt = P1047 }.Stromprofil_Strombedarf_berechnen();

            Assert.NotNull(nachher);
            Assert.Equal(summeVorher, nachher.Sum(), 9);
            Assert.Contains(protokoll.Warnungen, w => w.Contains("Ohne Typ"));
        }

        /// <summary>Die Testdatenbank selbst führt kein Prozessprofil ohne Typ — die Referenzbasis rechnet unverändert.</summary>
        [Fact]
        public void Die_Testdatenbank_fuehrt_keine_zugeordnete_Prozesskopie_ohne_Typ()
        {
            if (!_db.Vorhanden) return;

            long anzahl = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme z INNER JOIN Tab_Prozesswaerme k ON z.ID_Prozesswaerme = k.ID " +
                "WHERE k.Typ IS NULL"), CultureInfo.InvariantCulture);
            Assert.Equal(0L, anzahl);
        }
    }
}
