using System;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Entwicklerbereich <c>PROJEKTDATEI_KI</c> (Konzept Administration und Support 3.3 b): Ohne Entwicklermodus nimmt der
    /// Hilfe-Assistent die Feldkarte der Projektdatei (Zonierung des Gebäudeimports) nicht ins Register und sagt benannt ab
    /// (<c>KI_ABSAGE_NUR_ENTWICKLER</c>); im Entwicklermodus führt der Katalog sie wie bisher.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KiEntwicklerbereichTests : IDisposable
    {
        private static readonly string[] OHNE = new string[0];
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly IEinstellungen _vorher = Dienste.Einstellungen;

        public KiEntwicklerbereichTests()
        {
            Dienste.Einstellungen = new FluechtigeEinstellungen();
        }

        public void Dispose()
        {
            Entwicklermodus.Ermitteln(OHNE, _ => null);
            Dienste.Einstellungen = _vorher;
            _kultur.Dispose();
        }

        private static void Modus(bool an)
            => Entwicklermodus.Ermitteln(an ? new[] { Entwicklermodus.STARTARGUMENT } : OHNE, _ => null);

        [Fact]
        public void Ohne_Modus_fehlt_die_Zonierung_im_Register_und_die_Absage_ist_benannt()
        {
            Modus(false);
            KiDialog import = KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE_IMPORT);
            Assert.NotNull(import);
            Assert.False(import.KenntFeld("zonierung"));
            Assert.True(import.KenntFeld("quelle"));

            Assert.True(KiDialoge.NurEntwickler(KiMaskennamen.GEBAEUDE_IMPORT, "zonierung"));
            Assert.True(KiDialoge.NurEntwickler(KiMaskennamen.GEBAEUDE_IMPORT, KiDialogTexte.GimpZonierungName));
            Assert.False(KiDialoge.NurEntwickler(KiMaskennamen.GEBAEUDE_IMPORT, "quelle"));
            Assert.Equal("Diese Funktion ist in Ihrer Fassung nicht freigegeben.",
                         KiDialoge.NurEntwicklerAbsage(KiMaskennamen.GEBAEUDE_IMPORT, "zonierung"));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KI_ABSAGE_NUR_ENTWICKLER,
                         KiDialoge.NurEntwicklerAbsage(KiMaskennamen.GEBAEUDE_IMPORT, " Zonierung "));
            Assert.Null(KiDialoge.NurEntwicklerAbsage(KiMaskennamen.GEBAEUDE_IMPORT, "name"));
        }

        [Fact]
        public void Mit_Modus_fuehrt_der_Katalog_die_Zonierung_ohne_Absage()
        {
            Modus(true);
            Assert.True(Entwicklermodus.Aktiv);
            KiDialog import = KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE_IMPORT);
            Assert.True(import.KenntFeld("zonierung"));
            Assert.Null(KiDialoge.NurEntwicklerAbsage(KiMaskennamen.GEBAEUDE_IMPORT, "zonierung"));

            Modus(false);
            Assert.False(KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE_IMPORT).KenntFeld("zonierung"));
        }

        [Fact]
        public void Die_Absage_steht_in_beiden_Sprachen()
        {
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("This function is not enabled in your edition.", WindowsFormsApplication1.MyResource.Resource.KI_ABSAGE_NUR_ENTWICKLER);
        }
    }
}
