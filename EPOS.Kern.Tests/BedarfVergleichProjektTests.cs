using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Projektzeile im Vergleich der Bedarfsprofile</b> (KA-E-24, Anwenderentscheid BP-2):
    /// <c>BedarfStammCtrl.VergleichszeilenProjekt</c> liest Typ und Monatsgang aus der Projektkopie, skaliert den
    /// Monatsgang auf die Jahressumme der Zuordnung und führt Kalender, Temperaturpaar, Art und Deckung der Zuordnung.
    /// Gemessen an den drei Kältezeilen des Referenzprojekts 1072 (zentral, Split fest, Split linear).
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfVergleichProjektTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private const int PROJEKT = 1072;

        private static string Wert(IReadOnlyList<Parameterwert> liste, string spalte)
            => liste.Single(p => p.Eintrag.Spalte == spalte).Wert;

        [Fact]
        public void Die_Projektzeile_fuehrt_die_Liste_des_Katalogsatzes_samt_Kalender_und_Deckung()
        {
            if (!_db.Vorhanden) return;
            var angaben = new BedarfStammCtrl.Zuordnungsangaben
            {
                IdKopie = 1, Bezeichner = "Büro Kälteprofil", Summe = 1.0, Vorlauf = 16, Ruecklauf = 19,
                KaelteArt = DbWerte.KAELTE_ART_RAUM, Deckung = KaeltebedarfSchema.DECKUNG_ZENTRAL
            };
            IReadOnlyList<Parameterwert> projekt = BedarfStammCtrl.VergleichszeilenProjekt(BedarfsArt.Kaelte, angaben, PROJEKT);
            IReadOnlyList<Parameterwert> katalog = BedarfStammCtrl.Vergleichszeilen(BedarfsArt.Kaelte, "Büro Kälteprofil");

            // Dieselbe Liste wie der Katalogsatz, die Projektzeile führt dazu Kalender und Deckung.
            foreach (Parameterwert k in katalog)
                Assert.Contains(projekt, p => p.Eintrag.Spalte == k.Eintrag.Spalte && p.Eintrag.Anzeigetext == k.Eintrag.Anzeigetext);
            Assert.Equal("Raumkühlung Büro", Wert(projekt, "Typ"));
            Assert.Equal("KAELTE_ART_RAUM", Wert(projekt, "Kaelte_Art"));
            Assert.Equal("16", Wert(projekt, "Vorlauf"));
            Assert.Equal("19", Wert(projekt, "Ruecklauf"));
            Assert.Equal("BPF_KALENDER_KEINER", Wert(projekt, "ID_Betriebskalender"));
            Assert.Equal("DECK_ART_ZENTRAL", Wert(projekt, "Deckung"));
            Assert.DoesNotContain(projekt, p => p.Eintrag.Spalte.StartsWith("Split_", StringComparison.Ordinal));

            // Der Monatsgang der Kopie ist auf die Jahressumme der Zuordnung skaliert: Summe der Monate = Jahressumme.
            double summe = Enumerable.Range(1, 12)
                .Sum(i => double.Parse(Wert(projekt, "Monat_" + i), NumberStyles.Any, CultureInfo.CurrentCulture));
            Assert.InRange(summe, 0.95, 1.05);
            Assert.Equal(0.0, double.Parse(Wert(projekt, "Monat_1"), NumberStyles.Any, CultureInfo.CurrentCulture));
        }

        [Fact]
        public void Eine_Splitzeile_fuehrt_EER_Weg_und_ihre_Punkte()
        {
            if (!_db.Vorhanden) return;
            var fest = BedarfStammCtrl.VergleichszeilenProjekt(BedarfsArt.Kaelte, new BedarfStammCtrl.Zuordnungsangaben
            {
                IdKopie = 2, Bezeichner = "Split Büro", Summe = 3.0, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.0, Taussen1 = 35.0
            }, PROJEKT);
            Assert.Equal("DECK_ART_SPLIT", Wert(fest, "Deckung"));
            Assert.Equal("DECK_EER_FEST", Wert(fest, "Split_EER_Weg"));
            Assert.Equal("3", Wert(fest, "Split_EER_1"));
            Assert.Equal("35", Wert(fest, "Split_Taussen_1"));
            Assert.DoesNotContain(fest, p => p.Eintrag.Spalte == "Split_EER_2");

            var linear = BedarfStammCtrl.VergleichszeilenProjekt(BedarfsArt.Kaelte, new BedarfStammCtrl.Zuordnungsangaben
            {
                IdKopie = 3, Bezeichner = "Split Serverraum", Summe = 2.0, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 4.0, Taussen1 = 10.0, Eer2 = 2.5, Taussen2 = 35.0
            }, PROJEKT);
            Assert.Equal("Serverraum", Wert(linear, "Typ"));
            Assert.Equal("DECK_EER_LINEAR", Wert(linear, "Split_EER_Weg"));
            Assert.Equal("2,5", Wert(linear, "Split_EER_2"));
            Assert.Equal("35", Wert(linear, "Split_Taussen_2"));
        }

        [Fact]
        public void Ohne_Projektkopie_gilt_der_Katalogsatz_des_Namens_und_Brauchwasser_bleibt_ohne_Temperatur()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<Katalogfilterzeile> zeilen = BedarfStammCtrl.Katalogfilterzeilen(BedarfsArt.Brauchwasser);
            Assert.NotEmpty(zeilen);
            var liste = BedarfStammCtrl.VergleichszeilenProjekt(BedarfsArt.Brauchwasser, new BedarfStammCtrl.Zuordnungsangaben
            {
                IdKopie = 0, Bezeichner = zeilen[0].Bezeichner, Summe = 5.0
            }, 0);
            var katalog = BedarfStammCtrl.Vergleichszeilen(BedarfsArt.Brauchwasser, zeilen[0].Bezeichner);
            Assert.Equal(Wert(katalog, "Typ"), Wert(liste, "Typ"));
            Assert.DoesNotContain(liste, p => p.Eintrag.Spalte == "Vorlauf" || p.Eintrag.Spalte == "Deckung");
            Assert.Equal(16, liste.Count);   // Typ, Beschreibung, Jahressumme, zwölf Monate, Kalender
        }
    }
}
