using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorbelegung der Einheit im Zeilendialog des Gesetzeskatalogs</b>
    /// (Anwenderentscheid 14.10.2026, Empfehlung A des Mockups
    /// <c>Gesetzeskatalog_Einheit_Vorbelegung</c>).
    ///
    /// <para>Die Regel steht in <see cref="GesetzKatalog.EinheitVorschlagen"/>: ein
    /// bestehender Schlüssel gibt die Einheit seiner jüngsten Zeile, ein neuer die
    /// häufigste Einheit seines Bereichs, <c>ENERGIESTEUER</c> und ein leerer Bereich
    /// geben keine. Die ersten Fälle laufen ohne Datenbank, die letzten zählen den
    /// Bestand der Testdatenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GesetzEinheitVorbelegungTests
    {
        private static GesetzParameter Z(string schluessel, string klasse, int jahr, string einheit,
                                         string quelle = "")
            => new GesetzParameter(0, schluessel, klasse, jahr, 1.0, einheit, "GESICHERT", quelle);

        /// <summary>Ein kleiner Katalog: KWKG meist ct/kWh, ein Stundenschlüssel in h.</summary>
        private static GesetzVorschlagsdaten Daten()
        {
            var zeilen = new List<GesetzParameter>
            {
                Z("KWKG_ZUSCHLAG_BIS50KW", "KWKG", 2023, "ct/kWh"),
                Z("KWKG_ZUSCHLAG_BIS100KW", "KWKG", 2023, "ct/kWh"),
                Z("KWKG_ZUSCHLAG_BIS250KW", "KWKG", 2023, "ct/kWh"),
                Z("KWKG_VBH_JAHRESDECKEL", "KWKG", 2026, "h", "§ 8 Abs. 4"),
                Z("KWKG_VBH_JAHRESDECKEL", "KWKG", 2030, "h", "§ 8 Abs. 4 — neu"),
                Z("CO2_PREIS_NEHS", "CO2_PREIS", 2026, "EUR/t"),
                Z("ENERGIESTEUER_HEIZOEL", "ENERGIESTEUER", 2024, "EUR/1000l"),
                Z("ENERGIESTEUER_ERDGAS", "ENERGIESTEUER", 2024, "EUR/MWh"),
                Z("ENERGIESTEUER_FLUESSIGGAS", "ENERGIESTEUER", 2024, "EUR/1000kg"),
                Z("ENERGIESTEUER_KOHLE", "ENERGIESTEUER", 2024, "EUR/GJ")
            };
            var staende = new List<GesetzSchluesselStand>
            {
                new("KWKG_ZUSCHLAG_BIS50KW", "KWKG", 2023, "ct/kWh", ""),
                new("KWKG_ZUSCHLAG_BIS100KW", "KWKG", 2023, "ct/kWh", ""),
                new("KWKG_VBH_JAHRESDECKEL", "KWKG", 2030, "h", "§ 8 Abs. 4 — neu"),
                new("CO2_PREIS_NEHS", "CO2_PREIS", 2026, "EUR/t", "")
            };
            return new GesetzVorschlagsdaten(staende, GesetzKatalog.HaeufigsteEinheiten(zeilen));
        }

        [Fact]
        public void Ein_bestehender_Schluessel_gibt_seine_Einheit_nicht_die_des_Bereichs()
        {
            GesetzEinheitVorschlag v = GesetzKatalog.EinheitVorschlagen(" KWKG_VBH_JAHRESDECKEL ", "KWKG", Daten());

            Assert.Equal("h", v.Einheit);                          // nicht ct/kWh des Bereichs
            Assert.Equal(GesetzEinheitHerkunft.Schluessel, v.Herkunft);
            Assert.NotNull(v.Stand);
            Assert.Equal(2030, v.Stand!.JahrVon);                  // die juengste Zeile
            Assert.Equal("§ 8 Abs. 4 — neu", v.Stand.Quelle);
        }

        [Fact]
        public void Der_Schluessel_gewinnt_auch_gegen_einen_fremden_Bereich()
        {
            GesetzEinheitVorschlag v = GesetzKatalog.EinheitVorschlagen("KWKG_VBH_JAHRESDECKEL", "CO2_PREIS", Daten());

            Assert.Equal("h", v.Einheit);
            Assert.Equal("KWKG", v.Stand!.Klasse);
        }

        [Fact]
        public void Ein_neuer_Schluessel_bekommt_die_haeufigste_Einheit_des_Bereichs()
        {
            GesetzEinheitVorschlag kwkg = GesetzKatalog.EinheitVorschlagen("KWKG_NEU", "KWKG", Daten());
            Assert.Equal("ct/kWh", kwkg.Einheit);                  // drei von fuenf Zeilen
            Assert.Equal(GesetzEinheitHerkunft.Bereich, kwkg.Herkunft);
            Assert.Null(kwkg.Stand);

            GesetzEinheitVorschlag co2 = GesetzKatalog.EinheitVorschlagen("CO2_PREIS_KORRIDOR_OBEN", "CO2_PREIS", Daten());
            Assert.Equal("EUR/t", co2.Einheit);
        }

        [Fact]
        public void Gleichstand_entscheidet_die_Reihenfolge_der_Einheitenliste()
        {
            // ct/kWh steht in GesetzKatalog.Einheiten() vor h - bei zwei zu zwei gewinnt es,
            // gleich in welcher Reihenfolge die Zeilen gelesen werden.
            var vorwaerts = GesetzKatalog.HaeufigsteEinheiten(new[]
            {
                Z("A", "KWKG", 2020, "h"), Z("B", "KWKG", 2020, "h"),
                Z("C", "KWKG", 2020, "ct/kWh"), Z("D", "KWKG", 2020, "ct/kWh")
            });
            var rueckwaerts = GesetzKatalog.HaeufigsteEinheiten(new[]
            {
                Z("C", "KWKG", 2020, "ct/kWh"), Z("D", "KWKG", 2020, "ct/kWh"),
                Z("A", "KWKG", 2020, "h"), Z("B", "KWKG", 2020, "h")
            });

            Assert.Equal("ct/kWh", vorwaerts["KWKG"]);
            Assert.Equal("ct/kWh", rueckwaerts["KWKG"]);
        }

        [Fact]
        public void Energiesteuer_bekommt_keine_Vorbelegung()
        {
            GesetzVorschlagsdaten daten = Daten();
            Assert.False(daten.Bereichseinheit.ContainsKey("ENERGIESTEUER"));

            GesetzEinheitVorschlag v = GesetzKatalog.EinheitVorschlagen("ENERGIESTEUER_NEU", "ENERGIESTEUER", daten);
            Assert.Equal("", v.Einheit);
            Assert.Equal(GesetzEinheitHerkunft.EinheitWechselt, v.Herkunft);

            // Auch eine hereingereichte Bereichseinheit aendert daran nichts: Die Ausnahme
            // ist benannt, sie haengt nicht an der Zaehlung.
            var untergeschoben = new GesetzVorschlagsdaten(new List<GesetzSchluesselStand>(),
                new Dictionary<string, string> { ["ENERGIESTEUER"] = "EUR/1000l" });
            Assert.Equal("", GesetzKatalog.EinheitVorschlagen("X", "ENERGIESTEUER", untergeschoben).Einheit);
        }

        [Fact]
        public void Ein_bestehender_Energiesteuerschluessel_gibt_seine_Einheit()
        {
            var daten = new GesetzVorschlagsdaten(
                new List<GesetzSchluesselStand> { new("ENERGIESTEUER_HEIZOEL", "ENERGIESTEUER", 2024, "EUR/1000l", "") },
                new Dictionary<string, string>());

            Assert.Equal("EUR/1000l", GesetzKatalog.EinheitVorschlagen("ENERGIESTEUER_HEIZOEL", "ENERGIESTEUER", daten).Einheit);
        }

        [Fact]
        public void Ein_leerer_Bereich_ergibt_keine_Vorbelegung()
        {
            GesetzEinheitVorschlag v = GesetzKatalog.EinheitVorschlagen("GANZ_NEU", "", Daten());
            Assert.Equal("", v.Einheit);
            Assert.Equal(GesetzEinheitHerkunft.Keine, v.Herkunft);

            Assert.Equal("", GesetzKatalog.EinheitVorschlagen("GANZ_NEU", "UNBEKANNT", Daten()).Einheit);
            Assert.Equal("", GesetzKatalog.EinheitVorschlagen(null!, null!, null!).Einheit);
        }

        /// <summary>
        /// <b>Gezählt wird zur Laufzeit, aus der Datenbank.</b> Die Zahlen des Mockups
        /// (gemessen an der Testdatenbank) kommen heraus; <c>SYSTEM</c> ist weder
        /// Schlüssel noch Bereich.
        /// </summary>
        [Fact]
        public void Die_Vorschlagsdaten_der_Testdatenbank_folgen_dem_Bestand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GesetzKatalog.StelleKatalogSicher();
            GesetzVorschlagsdaten daten = new GesetzKatalog().Vorschlagsdaten();

            GesetzSchluesselStand? deckel = daten.Stand("KWKG_VBH_JAHRESDECKEL");
            Assert.NotNull(deckel);
            Assert.Equal("h", deckel!.Einheit);
            Assert.Equal(2030, deckel.JahrVon);
            Assert.Equal("KWKG", deckel.Klasse);

            Assert.Equal("ct/kWh", daten.Bereichseinheit["KWKG"]);
            Assert.Equal("ct/kWh", daten.Bereichseinheit["EEG"]);
            Assert.Equal("EUR/t", daten.Bereichseinheit["CO2_PREIS"]);
            Assert.Equal("EUR/MWh", daten.Bereichseinheit["STROMSTEUER"]);
            Assert.Equal("g/kWh", daten.Bereichseinheit["EF_BILANZ"]);
            Assert.False(daten.Bereichseinheit.ContainsKey("ENERGIESTEUER"));
            Assert.False(daten.Bereichseinheit.ContainsKey(DbWerte.GESETZ_KLASSE_SYSTEM));
            Assert.Null(daten.Stand(DbWerte.GESETZ_KATALOG_GENERATION));

            // Die Regel auf den gelesenen Daten: der Stundenschluessel gibt h, ein neuer
            // KWKG-Schluessel ct/kWh.
            Assert.Equal("h", GesetzKatalog.EinheitVorschlagen("KWKG_VBH_JAHRESDECKEL", "KWKG", daten).Einheit);
            Assert.Equal("ct/kWh", GesetzKatalog.EinheitVorschlagen("KWKG_GANZ_NEU", "KWKG", daten).Einheit);
        }
    }
}
