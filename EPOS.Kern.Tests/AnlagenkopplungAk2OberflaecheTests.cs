using System;
using System.Data;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EPOS.UI.Dialoge.Waermepumpe;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Anlagenkopplung AK2-3 — Oberfläche, Bericht</b> (Konzept 9.3, 9.4, 5.5): die Wahl der Woche mit der größten
    /// Unterschreitung (E80), die Berichtstafel „Komfort und Restbedarf" nur im Lauf mit Fahrplan samt Bedarfsbegriff und
    /// Zahl der festen Lasten, und der Schreibweg der Gruppe „Betriebszeiten" (NULL-erhaltend).
    /// </summary>
    public sealed class AnlagenkopplungAk2OberflaecheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        // Komfortwoche (E80)
        // =================================================================

        [Fact]
        public void Die_Woche_mit_der_groessten_Unterschreitung_wird_gewaehlt()
        {
            var soll = Enumerable.Repeat(21.0, 8760).ToArray();
            var luft = Enumerable.Repeat(21.0, 8760).ToArray();
            var maske = new bool[8760];
            // Tag 10: 5 Stunden je 1,5 K; Tag 200: 3 Stunden je 4 K (größer: 12 Kh gegen 7,5 Kh).
            for (int h = 240; h < 245; h++) { luft[h] = 19.5; maske[h] = true; }
            for (int h = 4800; h < 4803; h++) { luft[h] = 17.0; maske[h] = true; }

            int start = Komfortwoche.GroessteUnterschreitung(soll, luft, maske);
            Assert.Equal(0, start % 24);
            Assert.True(start <= 4800 && start + Komfortwoche.WOCHE > 4802, "Start " + start);
            // Gleichstand: die frühere Woche - hier die erste, die Tag 200 ganz enthält.
            Assert.Equal(4800 - 6 * 24, start);

            Assert.Equal(-1, Komfortwoche.GroessteUnterschreitung(soll, luft, new bool[8760]));
            Assert.Equal(-1, Komfortwoche.GroessteUnterschreitung(soll, luft, null));
            Assert.Equal(168, Komfortwoche.Ausschnitt(luft, start).Length);
            Assert.Null(Komfortwoche.Ausschnitt(luft, 8700));
        }

        // =================================================================
        // Bericht (9.4): Komfort neben Restbedarf, nur mit Fahrplan
        // =================================================================

        private static ErgebnisGebaeudeModel Zeile(int id, string name, bool gekoppelt, bool vdi = true)
            => new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = id, Merkplatz = id, Gebaeudename = name,
                Rechenweg = vdi ? DbWerte.GEBAEUDE_MODELL_VDI6007 : DbWerte.GEBAEUDE_MODELL_TAGESBILANZ,
                HeizwaermeMwh = 70.67, SpitzeKw = 34.5,
                UebergabeArt = gekoppelt ? DbWerte.UEBERGABE_RADIATOR : null,
                VorlaufMittelC = gekoppelt ? 35.3 : null, RuecklaufMittelC = gekoppelt ? 32.06 : null,
                UebergabeBegrenztStundenH = gekoppelt ? 10.0 : null,
                KomfortUnterschreitungsstundenH = gekoppelt ? 273 : null,
                KomfortKelvinstundenKh = gekoppelt ? 412.46 : null,
                KomfortLaengsteStreckeH = gekoppelt ? 5 : null,
            };

        private static BerichtsDaten Daten(int? fahrplanStunden, params ErgebnisGebaeudeModel[] gebaeude)
        {
            var erg = new ErgebnisModel();
            erg.Gebaeude.AddRange(gebaeude);
            erg.Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Waermerestbedarf = 1.234,
                FahrplanBegrenztStundenH = fahrplanStunden,
                KomfortUnterschreitungsstundenH = fahrplanStunden.HasValue ? 300 : null,
                KomfortKelvinstundenKh = fahrplanStunden.HasValue ? 450.0 : null,
                KomfortLaengsteStreckeH = fahrplanStunden.HasValue ? 6 : null,
            };
            var eingaben = new DataTable();
            eingaben.Columns.Add("ID", typeof(long));
            foreach (ErgebnisGebaeudeModel g in gebaeude) eingaben.Rows.Add((long)g.ID_Gebaeude);
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(new VariantenDaten
            {
                IstStamm = true, Projektname = "Probe", Ergebnis = erg,
                Details = new ProjektDetails { IdProjekt = 1, Gebaeude = eingaben }
            });
            return d;
        }

        private static string Schreibe(BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            new ProjektbeschreibungBaustein().SchreibeWord(new WordKontext(main, main.Document.Body, null), daten,
                                                           BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<Text>().Select(t => t.Text));
        }

        [Fact]
        public void Die_Komforttafel_steht_mit_Fahrplan_samt_Bedarfsbegriff_feste_Last_und_Restbedarf()
        {
            string text = Schreibe(Daten(120, Zeile(1, "Haus A", true), Zeile(2, "Haus B", false, vdi: false)));

            int ab = text.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_KOMFORT, StringComparison.Ordinal);
            Assert.True(ab >= 0, "Abschnitt fehlt");
            string teil = text.Substring(ab);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_BEDARFSBEGRIFF_RUECKWIRKUNG, teil);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_BEDARFSBEGRIFF_FESTE_LAST, teil);
            Assert.Contains("273", teil);
            Assert.Contains("412,5", teil);
            Assert.Contains("300", teil);
            Assert.Contains("1,23", teil);                     // Restbedarf neben den Komfortstunden
            Assert.Contains("120", teil);
            Assert.Contains(string.Format(WindowsFormsApplication1.MyResource.Resource.GEB_BERICHT_FESTE_LAST, 1), teil);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMENG_AK2_PROFILWEG_NAEHERUNG, teil);
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_KOMFORT, teil);
        }

        [Fact]
        public void Ohne_Fahrplan_entfaellt_die_Komforttafel()
        {
            string text = Schreibe(Daten(null, Zeile(1, "Haus A", true)));
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_KOMFORT, text);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SIMENG_AK2_PROFILWEG_NAEHERUNG, text);
        }

        [Fact]
        public void Der_Bedarfsbegriff_folgt_dem_Lauf_sonst_der_Kopplung()
        {
            Assert.Equal(Bedarfsbegriff.Rueckwirkung, ProjektbeschreibungBaustein.BedarfsbegriffImLauf(Zeile(1, "A", true)));
            Assert.Equal(Bedarfsbegriff.FesteLast, ProjektbeschreibungBaustein.BedarfsbegriffImLauf(Zeile(1, "A", false)));
            Assert.Equal(Bedarfsbegriff.FesteLast, ProjektbeschreibungBaustein.BedarfsbegriffImLauf(Zeile(1, "A", false, vdi: false)));
            ErgebnisGebaeudeModel gesetzt = Zeile(1, "A", true);
            gesetzt.Bedarfsbegriff = Bedarfsbegriff.FesteLast;
            Assert.Equal(Bedarfsbegriff.FesteLast, ProjektbeschreibungBaustein.BedarfsbegriffImLauf(gesetzt));
        }

        // =================================================================
        // Schreibweg der Betriebszeiten (9.3)
        // =================================================================

        [Fact]
        public void MitBetriebszeiten_reicht_beide_Felder_NULL_erhaltend_weiter()
        {
            WErzeugerCtrl.KonfigurationFelder f = BetriebszeitenAbbildung.MitBetriebszeiten(
                new WErzeugerCtrl.KonfigurationFelder(Heizstab: true), new WaermepumpeAnlageDaten { VorlaufMax = 50, Zeitprogramm = " " });
            Assert.True(f.Betriebszeiten);
            Assert.Null(f.Zeitprogramm);
            Assert.Equal(50.0, f.VorlaufMax);
            Assert.True(f.Heizstab);
            Assert.False(new WErzeugerCtrl.KonfigurationFelder().Betriebszeiten);
        }
    }

    /// <summary>Der Schreibweg der Betriebszeiten gegen die Testdatenbank: schreibt beide Spalten, NULL bleibt NULL.</summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungBetriebszeitenSchreibenTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private const int ANLAGE = 10353;          // Projekt 1007
        private const int PROJEKT = 1007;

        public AnlagenkopplungBetriebszeitenSchreibenTests(TestDatenbank db) { _db = db; }

        private static (object Zeit, object Vorlauf) Lesen()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Zeitprogramm, Vorlauf_Max FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", ANLAGE));
            return (dt.Rows[0][0], dt.Rows[0][1]);
        }

        [Fact]
        public void KonfigurationSchreiben_schreibt_Zeitprogramm_und_Vorlauf_Max_nur_auf_Wunsch()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();
            (object zeitVorher, object vorlaufVorher) = Lesen();
            try
            {
                string programm = AnlagenkopplungSchema.WochenprofilSchreiben(Enumerable.Repeat(0.5, 168).ToArray());
                Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                    new WErzeugerCtrl.KonfigurationFelder(Betriebszeiten: true, Zeitprogramm: programm, VorlaufMax: 48.5)).Ok);
                (object z, object v) = Lesen();
                Assert.Equal(programm, Convert.ToString(z));
                Assert.Equal(48.5, Convert.ToDouble(v));

                // Ohne den Schalter fasst der Weg die Spalten nicht an.
                Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                    new WErzeugerCtrl.KonfigurationFelder(Heizstab: false)).Ok);
                Assert.Equal(programm, Convert.ToString(Lesen().Zeit));

                // Leer schreibt NULL.
                Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                    new WErzeugerCtrl.KonfigurationFelder(Betriebszeiten: true)).Ok);
                (object z2, object v2) = Lesen();
                Assert.Equal(DBNull.Value, z2);
                Assert.Equal(DBNull.Value, v2);

                Assert.Null(GebaeudeBedarfCtrl.RestbedarfDesProjektsMwh(0));
                GebaeudeBedarfCtrl.RestbedarfDesProjektsMwh(PROJEKT);   // gelesen, ohne Ausnahme
            }
            finally
            {
                DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET Zeitprogramm = ?, Vorlauf_Max = ? WHERE ID = ?",
                    ProjektPuffer.Par("@z", DbParamTyp.VarWChar, zeitVorher == DBNull.Value ? null : zeitVorher),
                    ProjektPuffer.Par("@v", DbParamTyp.Double, vorlaufVorher == DBNull.Value ? null : vorlaufVorher),
                    new DbParam("@id", ANLAGE));
            }
        }
    }
}
