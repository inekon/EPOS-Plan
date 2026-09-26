using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Aufklapper „Alle Daten anzeigen"</b> (Anwenderentscheid 15.09.2026) — der
    /// volle Feldbestand der vier Erzeugerkataloge und die vier Schreibwege, die ihn
    /// zurueckschreiben.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Der Bearbeiten-Dialog jeder
    /// Erzeugerfamilie verliert seine Kosten- und Emissionsgruppen; an ihre Stelle tritt
    /// der Aufklapper im Modulbereich des Projektdialogs. Damit wird aus einer reinen
    /// ANZEIGE ein Schreibweg — und ein Anzeigefeld, das falsch oder leer zeigt, ist
    /// dann kein Schoenheitsfehler mehr, sondern Datenverlust beim naechsten Speichern.
    /// Geprueft wird deshalb der RUNDLAUF: lesen, aendern, schreiben, wieder lesen.</para>
    ///
    /// <para><b>Diese Klasse SCHREIBT</b> — anders als <see cref="KatalogVerwaltungTests"/>,
    /// deren Faelle alle vor dem Schreiben enden. Sie braucht deshalb ihre EIGENE
    /// Arbeitskopie; <see cref="TestDatenbank"/> als <c>IClassFixture</c> legt je
    /// Testklasse eine an. <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> selbst bleibt
    /// unberuehrt. Fehlt die Datei, schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogAufklapperTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KatalogAufklapperTests(TestDatenbank db) { _db = db; }

        // Die Saetze, an denen gerechnet wird - alle aus dem Auslieferungskatalog der
        // Testdatenbank.
        private const string KESSEL = "GC7000F 22 23 - MX25";
        private const string BHKW = "2G 250kw.el Gas";
        private const string KOLLEKTOR = "SO4000TFV-FCC220-2V";
        private const string PUFFER = "Puffer 3000Ltr";

        // =================================================================================
        // 1 - Der Feldbestand der vier Profile
        // =================================================================================

        /// <summary>
        /// Jedes Profil fuehrt GENAU die fachlichen Spalten seiner Stammtabelle, in
        /// Anzeigereihenfolge — der Vertrag, gegen den die Huellen bauen.
        /// </summary>
        /// <remarks>
        /// Die acht bzw. sechs Felder der ersten Fassung stehen unveraendert vorn; der
        /// volle Satz haengt hinten an, gruppiert Technik → Kosten → Emissionen.
        /// <c>ID</c> und <c>ReadOnly</c> stehen nicht darin: Das eine ist der Schluessel
        /// der Tabelle, das andere das Kennzeichen der Auslieferung.
        /// </remarks>
        [Theory]
        [MemberData(nameof(Feldbestaende))]
        public void Jedes_Profil_fuehrt_die_fachlichen_Spalten_seiner_Tabelle(
            KatalogBrowserArt art, string[] erwartet)
        {
            var profil = KatalogBrowserProfil.Finde(art);
            Assert.Equal(erwartet, profil.Detailfelder.Select(f => f.Schluessel).ToArray());
        }

        public static IEnumerable<object[]> Feldbestaende()
        {
            yield return new object[]
            {
                KatalogBrowserArt.Heizkessel,
                new[]
                {
                    "BEZEICHNER", "BESCHREIBUNG", "BRENNSTOFF", "PTHERM", "INVESTITIONSKOSTEN",
                    "BRENNWERT", "VORLAUF", "RUECKLAUF",
                    "FIRMA", "WIRKUNGSGRAD_GAS", "WIRKUNGSGRAD_OEL", "BBVERLUST", "RAUMBEDARF",
                    "WARTUNGSKOSTEN", "WARTUNG_EINHEIT", "NUTZUNGSDAUER",
                    "CO2", "SO2", "NOX", "CO", "STAUB"
                }
            };
            yield return new object[]
            {
                KatalogBrowserArt.Bhkw,
                new[]
                {
                    "BEZEICHNER", "FIRMA", "BESCHREIBUNG", "PTHERM", "PEL", "GRENZLEISTUNG",
                    "VORLAUF", "RUECKLAUF",
                    "BRENNSTOFF", "WIRKUNGSGRAD_EL", "WIRKUNGSGRAD_TH", "WIRKUNGSGRAD",
                    "MOTORTYP", "RAUMBEDARF",
                    "KOSTEN_MODUL", "KOSTEN_MONTAGE", "KOSTEN_LIEFERUNG",
                    "KOSTEN_SCHALLSCHUTZ", "KOSTEN_ABGASREINIGUNG",
                    "INVESTITION_KWEL", "WARTUNG_KWHEL", "NUTZUNGSDAUER",
                    "NOX", "SO2", "CO", "CO2", "STAUB"
                }
            };
            yield return new object[]
            {
                KatalogBrowserArt.Solarkollektoren,
                new[]
                {
                    "BEZEICHNER", "KOLLEKTORTYP", "FIRMA", "BESCHREIBUNG",
                    "MODULFLAECHE", "APERTURFLAECHE",
                    "H0", "K1", "K2", "KDIR", "KDIFF", "INVESTITIONSKOSTEN"
                }
            };
            yield return new object[]
            {
                KatalogBrowserArt.Pufferspeicher,
                new[]
                {
                    "BEZEICHNER", "FIRMA", "SPEICHERTYP", "VERLUSTE", "VOLUMEN",
                    "INVESTITIONSKOSTEN"
                }
            };
        }

        /// <summary>
        /// Der Datensatz einer Familie traegt GENAU so viele Felder, wie ihr Profil
        /// editierbar nennt — die Klammer zwischen Anzeige und Speicherweg.
        /// </summary>
        /// <remarks>
        /// <b>Wozu die Zaehlung.</b> Wer dem Profil ein Feld hinzufuegt und den
        /// Datensatz vergisst, baut ein Feld, das der Anwender ausfuellt und das niemand
        /// speichert — still. Diese Probe faellt dann.
        /// </remarks>
        [Theory]
        [InlineData(KatalogBrowserArt.Heizkessel, typeof(HeizkesselStammCtrl.AnzeigefelderHeizkessel))]
        [InlineData(KatalogBrowserArt.Bhkw, typeof(BHKWStammCtrl.AnzeigefelderBhkw))]
        [InlineData(KatalogBrowserArt.Solarkollektoren, typeof(SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor))]
        [InlineData(KatalogBrowserArt.Pufferspeicher, typeof(PufferSpStammCtrl.AnzeigefelderPufferspeicher))]
        public void Der_Datensatz_deckt_genau_die_editierbaren_Felder(KatalogBrowserArt art, Type satz)
        {
            int editierbar = KatalogBrowserProfil.Finde(art).Detailfelder.Count(f => f.Editierbar);
            int felder = satz.GetConstructors().Single().GetParameters().Length;

            Assert.Equal(editierbar, felder);
        }

        /// <summary>
        /// Jedes Profilfeld findet einen Wert und jeder Wert ein Feld — auch im vollen
        /// Satz. Das ist die Klammer zwischen Profil und <c>KatalogsatzAnzeige</c>.
        /// </summary>
        [Fact]
        public void Jeder_volle_Katalogsatz_beantwortet_genau_seine_Profilfelder()
        {
            if (!_db.Vorhanden) return;

            Pruefe(KatalogBrowserArt.Heizkessel, new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL));
            Pruefe(KatalogBrowserArt.Bhkw, BHKWStammCtrl.KatalogsatzAnzeige(BHKW));
            Pruefe(KatalogBrowserArt.Solarkollektoren, SolarkollektorenStammCtrl.KatalogsatzAnzeige(KOLLEKTOR));
            Pruefe(KatalogBrowserArt.Pufferspeicher, PufferSpStammCtrl.KatalogsatzAnzeige(PUFFER));

            static void Pruefe(KatalogBrowserArt art, IReadOnlyDictionary<string, string> satz)
            {
                Assert.NotNull(satz);
                var profil = KatalogBrowserProfil.Finde(art);
                Assert.Equal(profil.Detailfelder.Count, satz.Count);
                foreach (var feld in profil.Detailfelder)
                    Assert.True(satz.ContainsKey(feld.Schluessel),
                                art + ": Feld " + feld.Schluessel + " fehlt in der Anzeige.");
            }
        }

        // =================================================================================
        // 2 - Heizkessel: Rundlauf und Ablehnungen
        // =================================================================================

        /// <summary>
        /// Lesen, aendern, schreiben, wieder lesen — jedes der zwanzig Felder kommt an,
        /// und der Bezeichner bleibt, was er war.
        /// </summary>
        [Fact]
        public void Heizkessel_Rundlauf_schreibt_alle_Felder_zurueck()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var ctrl = new HeizkesselStammCtrl();
            var vorher = ctrl.KatalogsatzAnzeige(KESSEL);
            Assert.NotNull(vorher);

            var felder = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                "Probe Aufklapper", 33.5, 4444.25, false, 78, 58,
                Brennstoff: "Heizöl EL", Firma: "Probe GmbH",
                WirkungsgradGas: 0.925, WirkungsgradOel: 0.88,
                Betriebsbereitschaftverlust: 1.5, Raumbedarf: 2.25,
                Wartungskosten: 180.5, WartungskostenEinheit: "%/a",
                Nutzungsdauer: 22, CO2: 11.5, SO2: 1.25, NOx: 21.75, CO: 3.5, Staub: 0.75);

            var ergebnis = HeizkesselStammCtrl.AnzeigefelderSchreiben(KESSEL, felder);
            Assert.True(ergebnis.Ok, ergebnis.Meldung);
            Assert.Equal(KESSEL, ergebnis.Name);

            var nachher = new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL);
            Assert.NotNull(nachher);

            Assert.Equal(KESSEL, nachher[KatalogBrowserProfil.FeldBezeichner]);
            Assert.Equal("Probe Aufklapper", nachher[KatalogBrowserProfil.FeldBeschreibung]);
            Assert.Equal("Heizöl EL", nachher[KatalogBrowserProfil.FeldBrennstoff]);
            Assert.Equal("33,50", nachher[KatalogBrowserProfil.FeldPtherm]);
            Assert.Equal("4444,25", nachher[KatalogBrowserProfil.FeldInvestitionskosten]);
            Assert.Equal("0", nachher[KatalogBrowserProfil.FeldBrennwert]);
            Assert.Equal("78", nachher[KatalogBrowserProfil.FeldVorlauf]);
            Assert.Equal("58", nachher[KatalogBrowserProfil.FeldRuecklauf]);

            Assert.Equal("Probe GmbH", nachher[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal("0,925", nachher[KatalogBrowserProfil.FeldWirkungsgradGas]);
            Assert.Equal("0,88", nachher[KatalogBrowserProfil.FeldWirkungsgradOel]);
            Assert.Equal("1,5", nachher[KatalogBrowserProfil.FeldBBVerlust]);
            Assert.Equal("2,25", nachher[KatalogBrowserProfil.FeldRaumbedarf]);
            Assert.Equal("180,5", nachher[KatalogBrowserProfil.FeldWartungskosten]);
            Assert.Equal("%/a", nachher[KatalogBrowserProfil.FeldWartungEinheit]);
            Assert.Equal("22", nachher[KatalogBrowserProfil.FeldNutzungsdauer]);
            Assert.Equal("11,5", nachher[KatalogBrowserProfil.FeldCo2]);
            Assert.Equal("1,25", nachher[KatalogBrowserProfil.FeldSo2]);
            Assert.Equal("21,75", nachher[KatalogBrowserProfil.FeldNox]);
            Assert.Equal("3,5", nachher[KatalogBrowserProfil.FeldCo]);
            Assert.Equal("0,75", nachher[KatalogBrowserProfil.FeldStaub]);
        }

        /// <summary>
        /// <b>Was nicht mitkommt, bleibt stehen.</b> Ein Aufrufer, der nur die sechs
        /// Felder des alten Speicherpakets mitgibt, aendert die vierzehn neuen Spalten
        /// NICHT — sie duerfen nicht als 0 ueber gepflegte Werte laufen.
        /// </summary>
        [Fact]
        public void Heizkessel_weggelassene_Felder_bleiben_stehen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var vorher = new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL);
            Assert.NotNull(vorher);

            var nur_sechs = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                "nur die sechs", 12.0, 99.0, true, 70, 50);

            Assert.True(HeizkesselStammCtrl.AnzeigefelderSchreiben(KESSEL, nur_sechs).Ok);

            var nachher = new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL);
            foreach (string feld in new[]
                     {
                         KatalogBrowserProfil.FeldBrennstoff, KatalogBrowserProfil.FeldFirma,
                         KatalogBrowserProfil.FeldWirkungsgradGas, KatalogBrowserProfil.FeldWirkungsgradOel,
                         KatalogBrowserProfil.FeldBBVerlust, KatalogBrowserProfil.FeldRaumbedarf,
                         KatalogBrowserProfil.FeldWartungskosten, KatalogBrowserProfil.FeldWartungEinheit,
                         KatalogBrowserProfil.FeldNutzungsdauer, KatalogBrowserProfil.FeldCo2,
                         KatalogBrowserProfil.FeldSo2, KatalogBrowserProfil.FeldNox,
                         KatalogBrowserProfil.FeldCo, KatalogBrowserProfil.FeldStaub
                     })
                Assert.Equal(vorher[feld], nachher[feld]);
        }

        /// <summary>
        /// Eine Bezugsgroesse ausserhalb der drei zulaessigen wird BENANNT abgelehnt —
        /// und es wird nichts geschrieben.
        /// </summary>
        [Fact]
        public void Heizkessel_unbekannte_Wartungseinheit_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var vorher = new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL);

            var felder = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                "x", 10, 20, false, 70, 50, WartungskostenEinheit: "€/Monat");

            var ergebnis = HeizkesselStammCtrl.AnzeigefelderSchreiben(KESSEL, felder);

            Assert.False(ergebnis.Ok);
            Assert.Contains("€/Monat", ergebnis.Meldung);
            Assert.Contains("€/a", ergebnis.Meldung);          // die Liste steht in der Meldung
            Assert.Equal("", ergebnis.Name);

            // Und der Satz steht unveraendert da - auch in den sechs alten Feldern.
            var nachher = new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL);
            Assert.Equal(vorher[KatalogBrowserProfil.FeldBeschreibung],
                         nachher[KatalogBrowserProfil.FeldBeschreibung]);
            Assert.Equal(vorher[KatalogBrowserProfil.FeldPtherm],
                         nachher[KatalogBrowserProfil.FeldPtherm]);
        }

        /// <summary>
        /// Ein Energietraeger, den der Katalog nicht kennt, wird benannt abgelehnt; ein
        /// LEERES Feld dagegen laesst den gespeicherten stehen.
        /// </summary>
        [Fact]
        public void Heizkessel_unbekannter_Energietraeger_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var vorher = new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL);

            var falsch = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                "x", 10, 20, false, 70, 50, Brennstoff: "Bratkartoffelfett");
            var abgelehnt = HeizkesselStammCtrl.AnzeigefelderSchreiben(KESSEL, falsch);

            Assert.False(abgelehnt.Ok);
            Assert.Contains("Bratkartoffelfett", abgelehnt.Meldung);

            // Leeres Feld = unveraendert lassen (und der Rest wird geschrieben).
            var leer = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                "leerer Traeger", 10, 20, false, 70, 50, Brennstoff: "");
            Assert.True(HeizkesselStammCtrl.AnzeigefelderSchreiben(KESSEL, leer).Ok);

            var nachher = new HeizkesselStammCtrl().KatalogsatzAnzeige(KESSEL);
            Assert.Equal(vorher[KatalogBrowserProfil.FeldBrennstoff],
                         nachher[KatalogBrowserProfil.FeldBrennstoff]);
            Assert.Equal("leerer Traeger", nachher[KatalogBrowserProfil.FeldBeschreibung]);
        }

        /// <summary>Eine negative Leistung wird benannt abgelehnt.</summary>
        [Fact]
        public void Heizkessel_negative_Zahl_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var felder = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                "x", -1, 20, false, 70, 50);
            var ergebnis = HeizkesselStammCtrl.AnzeigefelderSchreiben(KESSEL, felder);

            // Die Meldung nennt das Feld so, wie es auf dem Schirm steht - uebersetzt
            // und ohne Doppelpunkt.
            Assert.False(ergebnis.Ok);
            Assert.Contains("Leistung", ergebnis.Meldung);
            Assert.DoesNotContain("KBROW_", ergebnis.Meldung);
        }

        // =================================================================================
        // 3 - BHKW: Rundlauf, Schreibschutz und die abgeleitete Investition
        // =================================================================================

        /// <summary>
        /// Der Rundlauf des BHKW — mit uebergangenem Schreibschutz, denn der
        /// Auslieferungskatalog ist vollstaendig geschuetzt.
        /// </summary>
        [Fact]
        public void Bhkw_Rundlauf_schreibt_alle_Felder_zurueck()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var felder = new BHKWStammCtrl.AnzeigefelderBhkw(
                "Probe GmbH", 260, 240, 25, 88, 62,
                Beschreibung: "Probe Aufklapper", Brennstoff: "Biogas",
                Motortyp: "Zuendstrahlmotor", Raumbedarf: 12.5,
                KostenModul: 20000, KostenMontage: 1000, KostenLieferung: 500,
                KostenSchallschutzhaube: 2000, KostenAbgasreinigung: 500,
                WartungskostenJeKWhel: 0.025, Nutzungsdauer: 12,
                NOx: 90, SO2: 2, CO: 210, CO2: 199000, Staub: 1,
                WirkungsgradEl: 0.30, WirkungsgradTh: 0.56);

            var ergebnis = BHKWStammCtrl.AnzeigefelderSchreiben(BHKW, felder,
                                                                schreibschutzUebergehen: true);
            Assert.True(ergebnis.Ok, ergebnis.Meldung);

            var nachher = BHKWStammCtrl.KatalogsatzAnzeige(BHKW);
            Assert.NotNull(nachher);

            Assert.Equal(BHKW, nachher[KatalogBrowserProfil.FeldBezeichner]);
            Assert.Equal("Probe GmbH", nachher[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal("Probe Aufklapper", nachher[KatalogBrowserProfil.FeldBeschreibung]);
            Assert.Equal("Biogas", nachher[KatalogBrowserProfil.FeldBrennstoff]);
            // DER GESAMTWIRKUNGSGRAD IST ANZEIGE, NICHT EINGABE (Anwenderentscheid
            // 20.09.2026): Geschrieben werden die zwei Anteile, gezeigt wird ihre
            // Summe - auf drei Stellen, wie im Katalogeditor.
            Assert.Equal("0,3", nachher[KatalogBrowserProfil.FeldWirkungsgradEl]);
            Assert.Equal("0,56", nachher[KatalogBrowserProfil.FeldWirkungsgradTh]);
            Assert.Equal("0,86", nachher[KatalogBrowserProfil.FeldWirkungsgrad]);
            Assert.Equal("Zuendstrahlmotor", nachher[KatalogBrowserProfil.FeldMotortyp]);
            Assert.Equal("12,5", nachher[KatalogBrowserProfil.FeldRaumbedarf]);
            Assert.Equal("20000", nachher[KatalogBrowserProfil.FeldKostenModul]);
            Assert.Equal("1000", nachher[KatalogBrowserProfil.FeldKostenMontage]);
            Assert.Equal("500", nachher[KatalogBrowserProfil.FeldKostenLieferung]);
            Assert.Equal("2000", nachher[KatalogBrowserProfil.FeldKostenSchallschutz]);
            Assert.Equal("500", nachher[KatalogBrowserProfil.FeldKostenAbgasreinigung]);
            Assert.Equal("0,025", nachher[KatalogBrowserProfil.FeldWartungJeKwhel]);
            Assert.Equal("12", nachher[KatalogBrowserProfil.FeldNutzungsdauer]);
            Assert.Equal("90", nachher[KatalogBrowserProfil.FeldNox]);
            Assert.Equal("2", nachher[KatalogBrowserProfil.FeldSo2]);
            Assert.Equal("210", nachher[KatalogBrowserProfil.FeldCo]);
            Assert.Equal("199000", nachher[KatalogBrowserProfil.FeldCo2]);
            Assert.Equal("1", nachher[KatalogBrowserProfil.FeldStaub]);
        }

        /// <summary>
        /// <b>Die Investition je kWel wird NACHGERECHNET, nicht eingetippt</b>
        /// (W14a-E-8-B3): 24 000 EUR aus fuenf Posten bei 240 kWel sind 100 EUR/kWel —
        /// in der Anzeige und in der Spalte.
        /// </summary>
        [Fact]
        public void Bhkw_Investition_je_kWel_folgt_den_fuenf_Posten()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var felder = new BHKWStammCtrl.AnzeigefelderBhkw(
                "Probe GmbH", 260, 240, 25, 88, 62,
                KostenModul: 20000, KostenMontage: 1000, KostenLieferung: 500,
                KostenSchallschutzhaube: 2000, KostenAbgasreinigung: 500);

            Assert.True(BHKWStammCtrl.AnzeigefelderSchreiben(BHKW, felder, true).Ok);

            var satz = BHKWStammCtrl.KatalogsatzAnzeige(BHKW);
            Assert.Equal("100", satz[KatalogBrowserProfil.FeldInvestitionJeKwel]);

            var m = new BHKWStammCtrl().ReadModel(BHKW);
            Assert.Equal(100.0, m.m_Investition_KWel, 6);
        }

        /// <summary>
        /// Der Schreibschutz bleibt, was er war: Ohne die bejahte Rueckfrage wird
        /// abgelehnt — auch mit dem vollen Feldsatz.
        /// </summary>
        [Fact]
        public void Bhkw_Schreibschutz_haelt_auch_den_vollen_Feldsatz_an()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var vorher = BHKWStammCtrl.KatalogsatzAnzeige(BHKW);
            var felder = new BHKWStammCtrl.AnzeigefelderBhkw(
                "Geht nicht", 1, 2, 3, 4, 5, Motortyp: "Geht auch nicht");

            var ergebnis = BHKWStammCtrl.AnzeigefelderSchreiben(BHKW, felder, false);

            Assert.False(ergebnis.Ok);
            Assert.False(string.IsNullOrEmpty(ergebnis.Meldung));

            var nachher = BHKWStammCtrl.KatalogsatzAnzeige(BHKW);
            Assert.Equal(vorher[KatalogBrowserProfil.FeldFirma], nachher[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal(vorher[KatalogBrowserProfil.FeldMotortyp], nachher[KatalogBrowserProfil.FeldMotortyp]);
        }

        /// <summary>Eine Grenzleistung ueber 100 % wird benannt abgelehnt.</summary>
        [Fact]
        public void Bhkw_Grenzleistung_ueber_hundert_Prozent_wird_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var felder = new BHKWStammCtrl.AnzeigefelderBhkw("x", 10, 10, 150, 80, 60);
            var ergebnis = BHKWStammCtrl.AnzeigefelderSchreiben(BHKW, felder, true);

            Assert.False(ergebnis.Ok);
            Assert.Contains("100", ergebnis.Meldung);
        }

        // =================================================================================
        // 4 - Solarkollektoren: der neue Schreibweg
        // =================================================================================

        /// <summary>
        /// Lesen, aendern, schreiben, wieder lesen — elf Felder. Vor- und Ruecklauf fuehrt der
        /// Katalog nicht mehr; der Satz traegt dafuer keinen Schluessel.
        /// </summary>
        [Fact]
        public void Solarkollektor_Rundlauf_schreibt_alle_Felder_zurueck()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var felder = new SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor(
                "Vakuumröhrenkollektor", "Probe GmbH", "Probe Aufklapper",
                Modulflaeche: 2.4, Aperturflaeche: 2.1,
                H0: 0.78, K1: 3.5, K2: 0.014, Kdir: 1.27, Kdiff: 0.9,
                Investitionskosten: 1250.5);

            var ergebnis = SolarkollektorenStammCtrl.AnzeigefelderSchreiben(KOLLEKTOR, felder);
            Assert.True(ergebnis.Ok, ergebnis.Meldung);
            Assert.Equal(KOLLEKTOR, ergebnis.Name);

            var nachher = SolarkollektorenStammCtrl.KatalogsatzAnzeige(KOLLEKTOR);
            Assert.NotNull(nachher);

            Assert.Equal(KOLLEKTOR, nachher[KatalogBrowserProfil.FeldBezeichner]);
            Assert.Equal("Vakuumröhrenkollektor", nachher[KatalogBrowserProfil.FeldKollektortyp]);
            Assert.Equal("Probe GmbH", nachher[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal("Probe Aufklapper", nachher[KatalogBrowserProfil.FeldBeschreibung]);
            Assert.Equal("2,4", nachher[KatalogBrowserProfil.FeldModulflaeche]);
            Assert.Equal("2,1", nachher[KatalogBrowserProfil.FeldAperturflaeche]);
            Assert.False(nachher.ContainsKey(KatalogBrowserProfil.FeldVorlauf));
            Assert.False(nachher.ContainsKey(KatalogBrowserProfil.FeldRuecklauf));
            Assert.Equal("0,78", nachher[KatalogBrowserProfil.FeldH0]);
            Assert.Equal("3,5", nachher[KatalogBrowserProfil.FeldK1]);
            Assert.Equal("0,014", nachher[KatalogBrowserProfil.FeldK2]);
            Assert.Equal("1,27", nachher[KatalogBrowserProfil.FeldKdir]);
            Assert.Equal("0,9", nachher[KatalogBrowserProfil.FeldKdiff]);
            Assert.Equal("1250,5", nachher[KatalogBrowserProfil.FeldInvestitionskosten]);
        }

        /// <summary>
        /// Ein Konversionsfaktor ueber 1 ist ein Prozentwert am falschen Platz und wird
        /// benannt abgelehnt; die Einfallswinkelkorrektur darf dagegen ueber 1 liegen —
        /// der Auslieferungskatalog fuehrt 1,27.
        /// </summary>
        [Fact]
        public void Solarkollektor_h0_ueber_eins_wird_abgelehnt_Kdir_nicht()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var zuGross = new SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor(
                "Flachkollektor", "x", "", 2, 2, 76.1, 3.5, 0.014, 0.94, 0, 0);
            var abgelehnt = SolarkollektorenStammCtrl.AnzeigefelderSchreiben(KOLLEKTOR, zuGross);

            Assert.False(abgelehnt.Ok);
            Assert.Contains("h0", abgelehnt.Meldung);

            var mitKdir = new SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor(
                "Flachkollektor", "x", "", 2, 2, 0.761, 3.5, 0.014, 1.27, 0, 0);
            Assert.True(SolarkollektorenStammCtrl.AnzeigefelderSchreiben(KOLLEKTOR, mitKdir).Ok);
        }

        /// <summary>Ein unbekannter Bezeichner bricht ab, ohne zu schreiben.</summary>
        [Fact]
        public void Solarkollektor_Speicherweg_bricht_ohne_Satz_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var felder = new SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor(
                "x", "y", "z", 1, 1, 0.8, 3, 0.01, 0.9, 0.9, 100);

            var ergebnis = SolarkollektorenStammCtrl.AnzeigefelderSchreiben("gibt-es-nicht", felder);

            Assert.False(ergebnis.Ok);
            Assert.False(string.IsNullOrEmpty(ergebnis.Meldung));
            Assert.Equal("", ergebnis.Name);
        }

        // =================================================================================
        // 5 - Pufferspeicher: der neue Schreibweg
        // =================================================================================

        /// <summary>Lesen, aendern, schreiben, wieder lesen — fuenf Felder.</summary>
        [Fact]
        public void Pufferspeicher_Rundlauf_schreibt_alle_Felder_zurueck()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var felder = new PufferSpStammCtrl.AnzeigefelderPufferspeicher(
                "Probe GmbH", "Kombispeicher", 4.25, 2500, 3300.75);

            var ergebnis = PufferSpStammCtrl.AnzeigefelderSchreiben(PUFFER, felder);
            Assert.True(ergebnis.Ok, ergebnis.Meldung);
            Assert.Equal(PUFFER, ergebnis.Name);

            var nachher = PufferSpStammCtrl.KatalogsatzAnzeige(PUFFER);
            Assert.NotNull(nachher);

            Assert.Equal(PUFFER, nachher[KatalogBrowserProfil.FeldBezeichner]);
            Assert.Equal("Probe GmbH", nachher[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal("Kombispeicher", nachher[KatalogBrowserProfil.FeldSpeichertyp]);
            Assert.Equal("4,25", nachher[KatalogBrowserProfil.FeldVerluste]);
            Assert.Equal("2500", nachher[KatalogBrowserProfil.FeldVolumen]);
            Assert.Equal("3300,75", nachher[KatalogBrowserProfil.FeldInvestitionskosten]);
        }

        /// <summary>
        /// Der Speichertyp geht durch die Bestandsabbildung: Der englische Altwert wird
        /// zum deutschen Persistenzwert, ein unbekannter Freitext bleibt stehen
        /// (Befund L0-1).
        /// </summary>
        [Fact]
        public void Pufferspeicher_Speichertyp_nimmt_die_Bestandsabbildung()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            Assert.True(PufferSpStammCtrl.AnzeigefelderSchreiben(PUFFER,
                new PufferSpStammCtrl.AnzeigefelderPufferspeicher(
                    "x", "Buffer storage", 1, 100, 0)).Ok);
            Assert.Equal("Pufferspeicher",
                         PufferSpStammCtrl.KatalogsatzAnzeige(PUFFER)[KatalogBrowserProfil.FeldSpeichertyp]);

            Assert.True(PufferSpStammCtrl.AnzeigefelderSchreiben(PUFFER,
                new PufferSpStammCtrl.AnzeigefelderPufferspeicher(
                    "x", "Eiswürfelspeicher", 1, 100, 0)).Ok);
            Assert.Equal("Eiswürfelspeicher",
                         PufferSpStammCtrl.KatalogsatzAnzeige(PUFFER)[KatalogBrowserProfil.FeldSpeichertyp]);
        }

        /// <summary>
        /// Ein negatives Volumen wird benannt abgelehnt — und es wird nichts
        /// geschrieben.
        /// </summary>
        [Fact]
        public void Pufferspeicher_negatives_Volumen_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var vorher = PufferSpStammCtrl.KatalogsatzAnzeige(PUFFER);

            var ergebnis = PufferSpStammCtrl.AnzeigefelderSchreiben(PUFFER,
                new PufferSpStammCtrl.AnzeigefelderPufferspeicher("x", "Pufferspeicher", 1, -5, 0));

            Assert.False(ergebnis.Ok);
            Assert.Contains("Gesamtvolumen", ergebnis.Meldung);
            Assert.DoesNotContain("KBROW_", ergebnis.Meldung);

            var nachher = PufferSpStammCtrl.KatalogsatzAnzeige(PUFFER);
            Assert.Equal(vorher[KatalogBrowserProfil.FeldFirma], nachher[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal(vorher[KatalogBrowserProfil.FeldVolumen], nachher[KatalogBrowserProfil.FeldVolumen]);
        }

        /// <summary>Ein unbekannter Bezeichner bricht ab, ohne zu schreiben.</summary>
        [Fact]
        public void Pufferspeicher_Speicherweg_bricht_ohne_Satz_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var ergebnis = PufferSpStammCtrl.AnzeigefelderSchreiben("gibt-es-nicht",
                new PufferSpStammCtrl.AnzeigefelderPufferspeicher("x", "Pufferspeicher", 1, 100, 0));

            Assert.False(ergebnis.Ok);
            Assert.False(string.IsNullOrEmpty(ergebnis.Meldung));
            Assert.Equal("", ergebnis.Name);
        }
    }
}
