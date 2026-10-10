using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KD-1 — die Katalogliste der Kältemaschinen</b>: Spaltenprofil nach dem Muster der Wärmepumpe
    /// (Folge und Rang), die Spalte Herkunft je Satzart und der verneinte Trichter
    /// <see cref="Katalogfilterprofil.AUSDRUCK_NICHT"/>, den der Schalter „Typkennfelder ausblenden“ setzt —
    /// ohne Datenbank und gegen die Arbeitskopie der Testdatenbank (3 Beispielgeräte, 34 Typkennfelder).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineStammKatalogfilterTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool MitKatalog => _db.Vorhanden && KaeltemaschineSchema.Vollstaendig();

        private static Katalogfilterprofil Profil()
            => Katalogfilterprofil.Finde(Anlagenart.Kaeltemaschine, s => R.ResourceManager.GetString(s) ?? s);

        [Fact]
        public void Das_Profil_fuehrt_sieben_Spalten_in_der_Folge_der_Waermepumpe()
        {
            Katalogfilterprofil p = Profil();

            Assert.Equal(new[]
            {
                Katalogfilterprofil.SpHersteller, Katalogfilterprofil.SpBezeichner, Katalogfilterprofil.SpNennkaelteleistung,
                Katalogfilterprofil.SpEer, Katalogfilterprofil.SpRueckkuehlart, Katalogfilterprofil.SpHerkunft,
                Katalogfilterprofil.SpTyp
            }, p.Spalten.Select(s => s.Schluessel));
            Assert.Equal(new[]
            {
                Katalogspaltenrang.BeiPlatz, Katalogspaltenrang.Immer, Katalogspaltenrang.Immer,
                Katalogspaltenrang.BeiPlatz, Katalogspaltenrang.BeiPlatz, Katalogspaltenrang.BeiPlatz,
                Katalogspaltenrang.Breit
            }, p.Spalten.Select(s => s.Rang));
            Assert.Equal(R.KFLT_SP_HERKUNFT, p.Spalte(Katalogfilterprofil.SpHerkunft).Titel);
            Assert.True(p.Spalte(Katalogfilterprofil.SpHerkunft).Filterbar);
            Assert.True(p.Spalte(Katalogfilterprofil.SpHerkunft).Sortierbar);
        }

        [Fact]
        public void Die_Herkunft_folgt_dem_Typ_und_dem_Katalogschluessel()
        {
            Assert.Equal("Typkennfeld", KaeltemaschineStammCtrl.HerkunftText(KaeltemaschinenKennfeld.TYP, true));
            Assert.Equal("Typkennfeld", KaeltemaschineStammCtrl.HerkunftText(KaeltemaschinenKennfeld.TYP, false));
            Assert.Equal("Auslieferung", KaeltemaschineStammCtrl.HerkunftText(null, true));
            Assert.Equal("eigen", KaeltemaschineStammCtrl.HerkunftText(null, false));
            Assert.Equal("eigen", KaeltemaschineStammCtrl.HerkunftText("Schraube", false));
            Assert.Equal("!Typkennfeld", KaeltemaschineStammCtrl.AusdruckOhneTypkennfelder);
        }

        [Fact]
        public void Der_verneinte_Trichter_trifft_jede_Textzelle_ausser_der_genannten()
        {
            var spalte = new Katalogspalte(Katalogfilterprofil.SpHerkunft, "Herkunft");
            Assert.False(Katalogfilter.PasstSpalte(spalte, Katalogwert.AusText("Typkennfeld"), "!Typkennfeld"));
            Assert.False(Katalogfilter.PasstSpalte(spalte, Katalogwert.AusText("Typkennfeld"), "!typ*"));
            Assert.True(Katalogfilter.PasstSpalte(spalte, Katalogwert.AusText("Auslieferung"), "!Typkennfeld"));
            Assert.True(Katalogfilter.PasstSpalte(spalte, Katalogwert.AusText("eigen"), " ! Typkennfeld "));
            // „!" allein filtert nicht — der Anwender tippt gerade.
            Assert.True(Katalogfilter.PasstSpalte(spalte, Katalogwert.AusText("Typkennfeld"), "!"));
        }

        [Fact]
        public void Die_Testdatenbank_zeigt_drei_von_37_Saetzen_ohne_Typkennfelder()
        {
            if (!MitKatalog) return;
            IReadOnlyList<Katalogfilterzeile> zeilen = KaeltemaschineStammCtrl.Katalogfilterzeilen();

            Assert.Equal(37, zeilen.Count);
            Assert.Equal(34, zeilen.Count(z => z.Text(Katalogfilterprofil.SpHerkunft) == "Typkennfeld"));
            Assert.Equal(3, zeilen.Count(z => z.Text(Katalogfilterprofil.SpHerkunft) == "Auslieferung"));
            Assert.All(zeilen.Where(z => z.Text(Katalogfilterprofil.SpHerkunft) == "Typkennfeld"),
                       z => Assert.True(string.IsNullOrEmpty(z.Wert(Katalogfilterprofil.SpHersteller).Text)
                                        || z.Text(Katalogfilterprofil.SpHersteller) == ParameterVerwendung.LEER));

            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpHerkunft, KaeltemaschineStammCtrl.AusdruckOhneTypkennfelder);
            IReadOnlyList<Katalogfilterzeile> treffer = Katalogfilter.Anwenden(Profil(), zeilen, stand);

            Assert.Equal(KaeltemaschineSchema.SAAT.Select(g => g.Bezeichner).OrderBy(n => n, StringComparer.Ordinal),
                         treffer.Select(z => z.Bezeichner).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void Eine_Kopie_eines_Beispielgeraets_ist_eigen()
        {
            if (!MitKatalog) return;
            Katalogfilterzeile beispiel = KaeltemaschineStammCtrl.Katalogfilterzeilen()
                .First(z => z.Text(Katalogfilterprofil.SpHerkunft) == "Auslieferung");

            Katalogkopie.Ergebnis kopie = KaeltemaschineStammCtrl.Duplizieren(beispiel.Id, beispiel.Bezeichner + " (Kopie)");
            Assert.True(kopie.Ok, kopie.Meldung);

            Assert.Equal("eigen", KaeltemaschineStammCtrl.Katalogfilterzeilen()
                .Single(z => z.Id == kopie.Id).Text(Katalogfilterprofil.SpHerkunft));
        }
    }
}
