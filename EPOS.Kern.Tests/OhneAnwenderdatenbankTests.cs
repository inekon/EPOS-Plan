using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kein Fall hängt an der Anwenderdatenbank des Rechners</b> (<see cref="OhneAnwenderdatenbank"/>): Ohne eine
    /// Vorrichtung, die eine Datenbank einlegt, sieht die Zugriffsschicht KEINE Datei — auch dann nicht, wenn unter
    /// <c>%ProgramData%\EPOS_PLAN</c> eine liegt. Der Nutzungskatalog des Zonenplans ist dann die Vorgabe im Code mit den
    /// alten Kennungen (WOHNEN, BUERO, SCHULE), nicht die Profile einer fremden Datenbank (<c>#1</c>, <c>#2</c>).
    ///
    /// <para><b>Befund.</b> Auf dem Windows-Arbeitsrechner fielen drei Fälle von
    /// <c>GebaeudeImportZonenplanHuelleTests</c> mit „Expected WOHNEN, Actual #1“, auf ubuntu waren sie grün: Dort liegt
    /// unter <c>CommonApplicationData</c> keine Datenbank, auf dem Arbeitsrechner die produktive <c>Kenndaten.sqlite</c>
    /// samt Katalog der Nutzungsprofile (Profil 1 = Wohnen, 2 = Büro). Mit einer Katalogdatenbank am Standardpfad fielen
    /// unter Linux dreizehn Fälle in vier Klassen — dazu <c>ZonenplanTests</c>, <c>SqprojZonenTests</c> und
    /// <c>SqprojHuelleTests</c>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class OhneAnwenderdatenbankTests
    {
        [Fact]
        public void Ohne_Vorrichtung_sieht_die_Zugriffsschicht_keine_Datenbank()
        {
            string pfad = DataRepository.GetDBPath();
            Assert.False(File.Exists(pfad), "Am Pfad der Zugriffsschicht liegt eine Datei: " + pfad);
            Assert.False(DataRepository.DatenbankVorhanden());
        }

        [Fact]
        public void Ohne_Vorrichtung_rechnet_der_Zonenplan_mit_der_Vorgabe_im_Code()
        {
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            Assert.False(v.MitKatalog, "Die Zugriffsschicht sieht einen Nutzungskatalog: " + DataRepository.GetDBPath());
            Assert.Equal(Zonenplan.NUTZUNGEN, v.Auswahl().Select(a => a.Schluessel));
            Assert.Equal(new Planprofil(null, DbWerte.KOND_NUTZUNG_WOHNEN), v.AusKennung(DbWerte.KOND_NUTZUNG_WOHNEN));
        }

        /// <summary>Der Pfad ist der des Initialisierers — nicht der der Einstellungen oder von <c>CommonApplicationData</c>.</summary>
        [Fact]
        public void Der_Pfad_der_Zugriffsschicht_ist_der_des_Initialisierers()
        {
            Assert.Equal(OhneAnwenderdatenbank.Pfad, DataRepository.PfadUeberschreibung);
            Assert.Equal(OhneAnwenderdatenbank.Pfad, DataRepository.GetDBPath());
            Assert.False(Directory.Exists(Path.GetDirectoryName(OhneAnwenderdatenbank.Pfad)));
        }

        /// <summary>
        /// <b>Gegenprobe:</b> Eine Arbeitskopie legt den Katalog ein — der Zonenplan sieht dann die Profile —, und ihre
        /// Entsorgung stellt den Pfad des Initialisierers wieder her, nicht den Standardpfad des Rechners.
        /// </summary>
        [Fact]
        public void Eine_Arbeitskopie_legt_den_Katalog_ein_und_ihre_Entsorgung_nimmt_ihn_zurueck()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;
                Assert.True(DataRepository.DatenbankVorhanden());
                Assert.True(Raumnutzungsvorbelegung.Lesen().MitKatalog);
            }
            Assert.Equal(OhneAnwenderdatenbank.Pfad, DataRepository.PfadUeberschreibung);
            Assert.False(DataRepository.DatenbankVorhanden());
            Assert.False(Raumnutzungsvorbelegung.Lesen().MitKatalog);
        }
    }
}
