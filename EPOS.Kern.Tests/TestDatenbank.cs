using System;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Testsammlung aller Faelle, die eine ARBEITSKOPIE der Datenbank brauchen.
    ///
    /// <para><b>Warum sie noetig ist.</b> <see cref="DataRepository.PfadUeberschreibung"/>
    /// ist ein STATISCHES Feld - es gibt genau eines fuer den ganzen Testlauf. xunit
    /// faehrt Testklassen standardmaessig NEBENEINANDER; zwei Klassen, die beide ihre
    /// eigene Arbeitskopie einlegen, ueberschreiben sich dabei gegenseitig den Pfad, und
    /// eine von beiden schreibt in die Kopie der anderen. Dieselbe Sammlung heisst: eine
    /// nach der anderen.</para>
    /// </summary>
    [CollectionDefinition("Testdatenbank")]
    public sealed class TestdatenbankSammlung { }

    /// <summary>
    /// Eine ARBEITSKOPIE der Testdatenbank fuer die Dauer einer Testklasse (iU9-W6.0a).
    ///
    /// <para><b>Warum es das jetzt gibt.</b> Bis Welle 6 pruefte dieses Projekt
    /// ausschliesslich, was ohne Datenbank entscheidbar ist - alles Uebrige blieb dem
    /// Referenzlauf vorbehalten. Mit iU9-W6 wandern jedoch SCHREIBENDE Wege aus der
    /// Oberflaeche in Kern-Controller: <c>EnergietraegerVarianteCtrl.Anlegen</c> schreibt
    /// in einer Transaktion drei Tabellen, und seine vier Ausgaenge
    /// (angelegt / vorgemerkt / bereits zugeordnet / Fehler) sind der Grund, warum ein
    /// Kessel aufgenommen oder eben nicht aufgenommen wird. Der Referenzlauf sieht davon
    /// nichts: Er rechnet einen BESTEHENDEN Projektstand nach, er legt keinen Traeger an.
    /// Ohne eine Probe hier waere dieser Weg allein am Windows-Gerraet nachweisbar.</para>
    ///
    /// <para><b>Warum eine Kopie.</b> <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> ist die
    /// Quelle jedes Referenzlaufs. Ein Test, der darin schreibt, wuerde die Vergleichsbasis
    /// verschieben - deshalb dasselbe Vorgehen wie in
    /// <c>EPOS.Referenzlauf</c> (<c>DbUmgebung.ArbeitskopieAnlegen</c>): kopieren,
    /// <see cref="DataRepository.PfadUeberschreibung"/> umbiegen, am Ende zuruecksetzen und
    /// die Kopie loeschen.</para>
    ///
    /// <para><b>Fehlt die Datei, wird nicht geprueft.</b> <see cref="Vorhanden"/> ist dann
    /// <c>false</c>, und die Faelle ueberspringen still. Ein Testlauf in einer Umgebung
    /// ohne die 77-MB-Datei soll nicht rot werden, sondern schweigen.</para>
    /// </summary>
    /// <remarks>
    /// <para><b>Seit iU9-W11a.6 auch als KLASSENVORRICHTUNG verwendbar</b>
    /// (<c>IClassFixture&lt;TestDatenbank&gt;</c>) — deshalb ist der Konstruktor
    /// oeffentlich. Der Bestand legt die 77-MB-Kopie je TESTFALL an; das ist fuer
    /// schreibende Faelle richtig (jeder bekommt einen unberuehrten Stand), fuer rein
    /// lesende aber teuer. Die vier Klassen der Welle 11 lesen nur und teilen sich
    /// deshalb EINE Kopie je Klasse.</para>
    /// </remarks>
    public sealed class TestDatenbank : IDisposable
    {
        private readonly string _vorher;
        private readonly string _ordner;

        public TestDatenbank()
        {
            _vorher = DataRepository.PfadUeberschreibung;

            string quelle = Quelle();
            if (quelle == null) return;

            _ordner = Path.Combine(Path.GetTempPath(),
                                   "epos-kerntest-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
            string ziel = Path.Combine(_ordner, "Kenndaten.sqlite");
            File.Copy(quelle, ziel);

            DataRepository.PfadUeberschreibung = ziel;
            Vorhanden = true;
        }

        /// <summary>Steht eine beschreibbare Arbeitskopie? Sonst ueberspringt der Fall.</summary>
        public bool Vorhanden { get; }

        /// <summary>
        /// Sucht <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> aufwaerts vom Laufordner.
        /// Der Testlauf steht in <c>bin/Release/net10.0</c>, die Datei in der Repo-Wurzel -
        /// wie tief das genau ist, haengt an der Bauart und wird deshalb nicht gezaehlt.
        /// </summary>
        private static string Quelle()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Kenndaten_Test.sqlite");
                if (File.Exists(kandidat)) return kandidat;
            }
            return null;
        }

        public void Dispose()
        {
            DataRepository.PfadUeberschreibung = _vorher;
            if (_ordner == null) return;
            try { Directory.Delete(_ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
        }
    }
}
