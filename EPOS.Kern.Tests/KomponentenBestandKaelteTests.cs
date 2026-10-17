using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die zwei Kältebits des Komponentenbestands (Konzept Kältebedarf 3.1, Stufe K4): „Kältebedarf extern“
    /// (Bit 8192, eine Zeile in <c>Z_ProjektWaermebedarf</c> mit Kanal „Kuehlung“) und „Kältebedarfsprofile“
    /// (Bit 16384, eine Zeile in <c>Z_Projekt_Kaeltebedarf</c>). Beide ohne Assistentenseite; die dreizehn Bits
    /// des Vorläufers bleiben unverändert (<see cref="KomponentenBestandTests"/>). Die Fälle schreiben, deshalb je
    /// Fall eine eigene Arbeitskopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KomponentenBestandKaelteTests : IDisposable
    {
        private const int PROJEKT = 1030;                       // ein Wärmelastgang, keine Kälte

        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_zwei_Kaeltebits_stehen_hinter_den_dreizehn_ohne_Assistentenseite()
        {
            KomponentenBestandCtrl b = KomponentenBestandCtrl.Lesen(0);

            Assert.Equal(15, KomponentenBestandCtrl.ANZAHL);
            Assert.Equal(13, KomponentenBestandCtrl.KAELTE_EXTERN);
            Assert.Equal(14, KomponentenBestandCtrl.KAELTEPROFIL);
            Assert.Equal(8192, b[KomponentenBestandCtrl.KAELTE_EXTERN].Bitwert);
            Assert.Equal(16384, b[KomponentenBestandCtrl.KAELTEPROFIL].Bitwert);
            Assert.Equal(KomponentenBestandCtrl.OHNE_SEITE, b[KomponentenBestandCtrl.KAELTE_EXTERN].SeitenIndex);
            Assert.Equal(KomponentenBestandCtrl.OHNE_SEITE, b[KomponentenBestandCtrl.KAELTEPROFIL].SeitenIndex);

            // Kein Bit doppelt vergeben.
            var bits = new HashSet<int>();
            for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++) Assert.True(bits.Add(b[k].Bitwert));
        }

        [Fact]
        public void Ohne_Kaelte_bleiben_beide_Bits_aus()
        {
            if (!_db.Vorhanden) return;

            KomponentenBestandCtrl b = KomponentenBestandCtrl.Lesen(PROJEKT);

            Assert.Equal(0, b.Bitmaske & (8192 | 16384));
            Assert.Equal(16, b.Bitmaske & 16);
        }

        [Fact]
        public void Ein_Lastgang_im_Kanal_Kuehlung_setzt_Bit_8192_und_laesst_das_Waermebit()
        {
            if (!_db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_ProjektWaermebedarf SET Kanal = ? WHERE ID_Projekt = ?",
                                                  new DbParam("@k", DbWerte.KANAL_KUEHLUNG), new DbParam("@p", PROJEKT)));

            KomponentenBestandCtrl b = KomponentenBestandCtrl.Lesen(PROJEKT);

            Assert.Equal(8192, b.Bitmaske & 8192);
            Assert.Equal(16, b.Bitmaske & 16);
            Assert.Equal(0, b.Bitmaske & 16384);
            KomponentenBestandCtrl.Eintrag e = b[KomponentenBestandCtrl.KAELTE_EXTERN];
            Assert.True(e.Vorhanden);
            Assert.Equal(e.Anzahl, e.Namen.Count);
        }

        [Fact]
        public void Eine_Kaeltebedarfszuordnung_setzt_Bit_16384()
        {
            if (!_db.Vorhanden) return;
            long stamm = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = 'Kühlraum'"), CultureInfo.InvariantCulture);
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = (int)stamm, Bezeichner = "Kühlraum", Summe = 50 }
            }));

            KomponentenBestandCtrl b = KomponentenBestandCtrl.Lesen(PROJEKT);

            Assert.Equal(16384, b.Bitmaske & 16384);
            Assert.Equal(0, b.Bitmaske & 8192);
            Assert.Equal(new[] { "Kühlraum" }, b[KomponentenBestandCtrl.KAELTEPROFIL].Namen);
        }
    }
}
