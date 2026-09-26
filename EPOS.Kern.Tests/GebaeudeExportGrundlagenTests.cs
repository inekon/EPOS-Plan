using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7a, Welle 1 — die Grundlagen des Gebäudeexports</b>: die Gebäudeart als
    /// <c>buildingType</c> (Tabelle im Profil, einschließlich Leerzeichen- und NULL-Fällen), das
    /// Kennzeichen der Testlizenz, die Kennungen nach D5 (Umsetzungsauftrag G7a, 2.2), der
    /// Freigabeschalter und das gemeinsame Vokabular von Leser und Schreiber.
    /// </summary>
    public sealed class GebaeudeExportGrundlagenTests
    {
        // ==================================================================
        //  buildingType
        // ==================================================================

        [Theory]
        [InlineData("Einfamilienhaus", "SingleFamily")]
        [InlineData("  einfamilienhaus  ", "SingleFamily")]
        [InlineData("Reiheneckhaus", "SingleFamily")]
        [InlineData("Reihenmittelhaus", "SingleFamily")]
        [InlineData("Reihenhaus", "SingleFamily")]
        [InlineData("Mehrfamilienhaus", "MultiFamily")]
        [InlineData("kleines Mehrfamilienhaus", "MultiFamily")]
        [InlineData("grosses Mehrfamilienhaus", "MultiFamily")]
        [InlineData("großes  Mehrfamilienhaus", "MultiFamily")]
        [InlineData("Wohnblock", "MultiFamily")]
        [InlineData("Hotel", "Hotel")]
        [InlineData("Krankenhaus", "HospitalOrHealthcare")]
        [InlineData("Krankenhaus ", "HospitalOrHealthcare")]
        [InlineData("Altenheim", "HospitalOrHealthcare")]
        [InlineData("Schule", "SchoolOrUniversity")]
        [InlineData("Verwaltung", "Office")]
        [InlineData("Verwaltungsgebäude", "Office")]
        [InlineData("VERWALTUNGSGEBAEUDE", "Office")]
        [InlineData("Kaufhalle", "Retail")]
        [InlineData("Kaufhaus", "Retail")]
        [InlineData("Industriehalle", "Manufacturing")]
        [InlineData("Sporthalle", "Gymnasium")]
        [InlineData("Hallenbad", "SportsArena")]
        public void Gebaeudeart_wird_normiert_verglichen(string gebaeudeart, string erwartet)
        {
            Gebaeudetypwahl w = GbxmlExportProbe.Profil().Gebaeudetyp(gebaeudeart);
            Assert.Equal(erwartet, w.Wert);
            Assert.True(w.Bekannt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Gewerbe")]
        [InlineData("Sonstige")]
        [InlineData("Einfamilienhaus mit Anbau")]
        [InlineData("Mehrfamilienhausgruppe")]
        public void Ohne_Treffer_Unknown_mit_Meldungsbedarf(string gebaeudeart)
        {
            Gebaeudetypwahl w = GbxmlExportProbe.Profil().Gebaeudetyp(gebaeudeart);
            Assert.Equal(GbxmlVokabular.Unknown, w.Wert);
            Assert.False(w.Bekannt);
        }

        [Fact]
        public void Die_Tabelle_nennt_nur_Werte_von_buildingTypeEnum()
        {
            Assert.All(GebaeudeExportProfil.StandardGebaeudetypen, r => Assert.Contains(r.Wert, GbxmlVokabular.Gebaeudearten));
            Assert.Equal(35, GbxmlVokabular.Gebaeudearten.Count);
            Assert.Throws<ArgumentException>(() => new Gebaeudetypregel("Einfamilienhaus", "Einfamilienhaus"));
            Assert.Equal("verwaltungsgebaeude gross", GebaeudeExportProfil.Normieren("  Verwaltungsgebäude \t GROß "));
        }

        // ==================================================================
        //  Testlizenz, Profil, Freigabeschalter
        // ==================================================================

        [Theory]
        [InlineData("demo", true)]
        [InlineData("person", false)]
        [InlineData("firma", false)]
        [InlineData("Demo", false)]
        [InlineData(null, false)]
        public void Testlizenz_aus_dem_Lizenztyp(string typ, bool erwartet)
        {
            Assert.Equal(erwartet, GebaeudeExportProfil.IstTestlizenz(typ));
        }

        [Fact]
        public void Das_Profil_verlangt_Sprache_Uhr_und_Version()
        {
            Func<DateTime> uhr = () => GbxmlExportProbe.Zeitpunkt;
            Assert.Throws<ArgumentNullException>(() => new GebaeudeExportProfil(null, uhr, false, "1"));
            Assert.Throws<ArgumentNullException>(() => new GebaeudeExportProfil(CultureInfo.InvariantCulture, null, false, "1"));
            Assert.Throws<ArgumentException>(() => new GebaeudeExportProfil(CultureInfo.InvariantCulture, uhr, false, " "));
            GebaeudeExportProfil p = GbxmlExportProbe.Profil(testlizenz: true);
            Assert.True(p.Testlizenz);
            Assert.Equal(GebaeudeQuelle.FORMAT_GBXML, p.Format);
            Assert.IsType<GbxmlSchreiber>(p.SchreiberErzeugen());
        }

        [Fact]
        public void Der_Freigabeschalter_ist_eine_Konstante_und_im_Entwicklungsstand_an()
        {
            Assert.True(GebaeudeExportRegeln.GbxmlExportFreigegeben);
            FieldInfo schalter = typeof(GebaeudeExportRegeln).GetField("FREIGABE_GBXML_EXPORT", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(schalter);
            Assert.True(schalter.IsLiteral, "Der Schalter ist eine Konstante, kein Profilfeld und keine Einstellung.");
        }

        // ==================================================================
        //  Kennungen (D5)
        // ==================================================================

        [Fact]
        public void Kennungen_aus_Schluesseln_und_festen_Kuerzeln()
        {
            Assert.Equal("epos-campus-7", GebaeudeExportKennung.Campus(7));
            Assert.Equal("epos-gebaeude-7", GebaeudeExportKennung.Gebaeude(7));
            Assert.Equal("epos-raum-11", GebaeudeExportKennung.Raum(11));
            Assert.Equal("epos-zone-11", GebaeudeExportKennung.Zone(11));
            Assert.Equal("epos-bauteil-101", GebaeudeExportKennung.Bauteil(101));
            Assert.Equal("epos-oeffnung-111", GebaeudeExportKennung.Oeffnung(111));
            Assert.Equal("epos-fenstertyp-111", GebaeudeExportKennung.Fenstertyp(111));
            Assert.Equal("epos-aufbau-201-hor-al", GebaeudeExportKennung.Aufbau(201, Waermestromrichtung.Horizontal, Bauteilrand.Aussenluft));
            Assert.Equal("epos-aufbau-201-auf-ub", GebaeudeExportKennung.Aufbau(201, Waermestromrichtung.Aufwaerts, Bauteilrand.Unbeheizt));
            Assert.Equal("epos-aufbau-201-ab-er", GebaeudeExportKennung.Aufbau(201, Waermestromrichtung.Abwaerts, Bauteilrand.Erdreich));
            Assert.Equal("epos-aufbau-201-hor-in", GebaeudeExportKennung.Aufbau(201, Waermestromrichtung.Horizontal, Bauteilrand.Innen));
            Assert.Equal("epos-schicht-201-3", GebaeudeExportKennung.Schicht(201, 3));
            Assert.Equal("epos-stoff-201-3", GebaeudeExportKennung.Stoff(201, 3));
            Assert.Equal("epos-schicht-202-2-auf", GebaeudeExportKennung.SchichtRuhendeLuft(202, 2, Waermestromrichtung.Aufwaerts));
            Assert.Equal("epos-stoff-202-2-ab", GebaeudeExportKennung.StoffRuhendeLuft(202, 2, Waermestromrichtung.Abwaerts));
            Assert.Equal("epos-aufbau-bauteil-108", GebaeudeExportKennung.ErsatzAufbau(108));
            Assert.Equal("epos-schicht-bauteil-108", GebaeudeExportKennung.ErsatzSchicht(108));
            Assert.Equal("epos-stoff-bauteil-108", GebaeudeExportKennung.ErsatzStoff(108));
            Assert.Equal("epos-raum-klasse-7", GebaeudeExportKennung.KlassenRaum(7));
            Assert.Equal("epos-zone-klasse-7", GebaeudeExportKennung.KlassenZone(7));
            Assert.Equal("epos-klasse-7-3", GebaeudeExportKennung.KlassenBauteil(7, 3));
            Assert.Equal("epos-aufbau-klasse-7-3", GebaeudeExportKennung.KlassenAufbau(7, 3));
            Assert.Equal("epos-unbeheizt-7", GebaeudeExportKennung.Unbeheizt(7));
            Assert.Equal("epos-innenmasse-7", GebaeudeExportKennung.Innenmasse(7));
        }

        [Fact]
        public void Jede_Kennung_ist_ein_NCName()
        {
            string[] alle =
            {
                GebaeudeExportKennung.Campus(1), GebaeudeExportKennung.Aufbau(1, Waermestromrichtung.Abwaerts, Bauteilrand.Erdreich),
                GebaeudeExportKennung.StoffRuhendeLuft(1, 1, Waermestromrichtung.Horizontal), GebaeudeExportKennung.KlassenFenstertyp(1, 1),
                GebaeudeExportKennung.KlassenSchicht(1, 1), GebaeudeExportKennung.KlassenStoff(1, 1),
                GebaeudeExportKennung.InnenmasseAufbau(1), GebaeudeExportKennung.InnenmasseSchicht(1), GebaeudeExportKennung.InnenmasseStoff(1),
                GebaeudeExportKennung.PROGRAMM, GebaeudeExportKennung.PERSON,
            };
            foreach (string k in alle) Assert.Equal(k, XmlConvert.VerifyNCName(k));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Ein_Schluessel_kleiner_gleich_null_wirft(int schluessel)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeExportKennung.Raum(schluessel));
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeExportKennung.Bauteil(schluessel));
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeExportKennung.Schicht(1, schluessel));
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeExportKennung.KlassenBauteil(schluessel, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeExportKennung.Aufbau(schluessel, Waermestromrichtung.Horizontal, Bauteilrand.Aussenluft));
        }

        [Fact]
        public void Die_Trennflaeche_zur_Nachbarzone_hat_das_Randkuerzel_zo()
        {
            Assert.Equal("zo", GebaeudeExportKennung.Randkuerzel(Bauteilrand.Zone));
            Assert.Equal("epos-aufbau-5-hor-zo", GebaeudeExportKennung.Aufbau(5, Waermestromrichtung.Horizontal, Bauteilrand.Zone));
        }

        // ==================================================================
        //  Die Schemakopie liegt außerhalb des Repositoriums (D17)
        // ==================================================================

        [Fact]
        public void Die_Schemakopie_steht_im_gitignore_nur_das_LIESMICH_ist_versioniert()
        {
            string wurzel = Wurzel();
            string[] zeilen = File.ReadAllLines(Path.Combine(wurzel, ".gitignore")).Select(z => z.Trim()).ToArray();
            Assert.Contains("Referenzlaeufe/Schemakopien/*", zeilen);
            Assert.Contains("!Referenzlaeufe/Schemakopien/LIESMICH.md", zeilen);
            string liesmich = File.ReadAllText(Path.Combine(wurzel, "Referenzlaeufe", "Schemakopien", "LIESMICH.md"));
            Assert.Contains(GbxmlExportProbe.SCHEMAKOPIE, liesmich, StringComparison.Ordinal);
            Assert.Contains("26.09.2026", liesmich, StringComparison.Ordinal);
            Assert.Contains("| keine |", liesmich, StringComparison.Ordinal);
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }

        // ==================================================================
        //  Das Vokabular
        // ==================================================================

        [Fact]
        public void Das_Vokabular_traegt_die_Einheiten_als_Konstanten_mit_dem_Wert_als_Namen()
        {
            foreach (FieldInfo f in typeof(GbxmlVokabular).GetFields(BindingFlags.Public | BindingFlags.Static)
                                                          .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name != "Celsius"))
                Assert.Equal(f.Name, (string)f.GetRawConstantValue());
            Assert.Equal("C", GbxmlVokabular.Celsius);
            Assert.Equal(GbxmlVokabular.NumberOfPeople, GbxmlEinheiten.PERSONEN_ANZAHL);
            Assert.Equal(1.0, GbxmlEinheiten.Leitfaehigkeit(1.0, GbxmlVokabular.WPerMeterK));
            Assert.Equal(1.0, GbxmlEinheiten.Dichte(1.0, GbxmlVokabular.KgPerCubicM));
            Assert.Equal(1.0, GbxmlEinheiten.Waermekapazitaet(1.0, GbxmlVokabular.JPerKgK));
            Assert.Equal(1.0, GbxmlEinheiten.UWert(1.0, GbxmlVokabular.WPerSquareMeterK));
            Assert.Equal(1.0, GbxmlEinheiten.RWert(1.0, GbxmlVokabular.SquareMeterKPerW));
            Assert.Equal(1.0, GbxmlEinheiten.Laenge(1.0, GbxmlVokabular.Meters));
            Assert.Equal(1.0, GbxmlEinheiten.Flaeche(1.0, GbxmlVokabular.SquareMeters));
            Assert.Equal(1.0, GbxmlEinheiten.Volumen(1.0, GbxmlVokabular.CubicMeters));
            Assert.Equal(1.0, GbxmlEinheiten.Anteil(1.0, GbxmlVokabular.Fraction));
            Assert.Equal(1.0, GbxmlEinheiten.LeistungJeFlaeche(1.0, GbxmlVokabular.WattPerSquareMeter));
            Assert.Equal(1.0, GbxmlEinheiten.FlaecheJePerson(1.0, GbxmlVokabular.SquareMPerPerson));
            Assert.Equal(1.0, GbxmlEinheiten.Temperatur(1.0, GbxmlVokabular.Celsius));
        }
    }
}
