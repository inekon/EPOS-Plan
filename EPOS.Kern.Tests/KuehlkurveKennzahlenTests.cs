using System;
using System.Data;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kennzahlen der Kühlkurve im Ergebnis</b> (Entwurf KK, Festlegung 12; Schemaschritt 202): mittlerer Kühlvorlauf,
    /// Absenkung durch den Raumeinfluss und Stunden an der Vorlaufgrenze in der Projektzeile, im Leser des Bedarfsdialogs
    /// und in der Tafel „Kühlkurve" des Berichts — nur mit Wert, ohne wirksame Kühlkurve NULL.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlkurveKennzahlenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Modell (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Kennzahlen_stehen_nur_mit_einem_Wert()
        {
            Assert.Null(KuehlkurveKennzahlen.Aus(null));
            Assert.Null(KuehlkurveKennzahlen.Aus(new ErgebnisEnergiebedarfModel()));

            var leer = new ErgebnisEnergiebedarfModel();
            SimulationRunner.KuehlkurveSpaltenSetzen(leer, null);
            Assert.Null(KuehlkurveKennzahlen.Aus(leer));

            KuehlkurveKennzahlen k = KuehlkurveKennzahlen.Aus(new ErgebnisEnergiebedarfModel
            {
                KuehlkurveVorlaufMittelC = 16.25, KuehlkurveAbsenkungKh = 412.5, KuehlkurveVorlaufgrenzeStundenH = 120,
            });
            Assert.Equal(16.25, k.VorlaufMittelC);
            Assert.Equal(412.5, k.AbsenkungKh);
            Assert.Equal(120, k.VorlaufgrenzeStundenH);

            // Kein Mittel (keine Kuehlstunde), aber die Kurve lief: die Kennzahlen stehen.
            Assert.NotNull(KuehlkurveKennzahlen.Aus(new ErgebnisEnergiebedarfModel { KuehlkurveAbsenkungKh = 0.0 }));
        }

        // =============================================================================
        //  Teil 2 - Lauf und Leser (Testdatenbank)
        // =============================================================================

        /// <summary>
        /// 1061 mit Kühlkurve: der Leser des Bedarfsdialogs liest die drei Kennzahlen der Projektzeile; ohne Kühlkurve (1030)
        /// stehen alle drei Spalten NULL und der Leser gibt nichts.
        /// </summary>
        [Fact]
        public void Der_Leser_liest_die_Kennzahlen_nur_mit_Kuehlkurve()
        {
            if (!_db.Vorhanden) return;
            Rechnen(1030);
            foreach (string s in KuehlkurveSchema.SPALTEN_ERGEBNIS)
                Assert.True(Kennzahl(1030, s) is null or DBNull, s + " ist ohne Kühlkurve nicht NULL");
            Assert.Null(GebaeudeBedarfCtrl.KuehlkurveKennzahlenDesProjekts(1030));

            Rechnen(KuehlkurveReferenzprojektWacheTests.PROJEKT);
            KuehlkurveKennzahlen k = GebaeudeBedarfCtrl.KuehlkurveKennzahlenDesProjekts(KuehlkurveReferenzprojektWacheTests.PROJEKT);
            Assert.NotNull(k);
            Assert.NotNull(k.VorlaufMittelC);
            Assert.True(k.AbsenkungKh > 0.0);
            Assert.InRange(k.VorlaufgrenzeStundenH.Value, 0, 8760);

            ErgebnisModel m = new ErgebnisCtrl().Load(KuehlkurveReferenzprojektWacheTests.PROJEKT);
            Assert.Equal(k.VorlaufgrenzeStundenH, m?.Energiebedarf.KuehlkurveVorlaufgrenzeStundenH);
            Assert.Equal(k.AbsenkungKh.Value, m.Energiebedarf.KuehlkurveAbsenkungKh.Value, 9);

            Assert.Null(GebaeudeBedarfCtrl.KuehlkurveKennzahlenDesProjekts(0));
        }

        // =============================================================================
        //  Teil 3 - die Tafel im Bericht
        // =============================================================================

        [Fact]
        public void Die_Tafel_Kuehlkurve_steht_im_Bericht_nur_mit_Wert()
        {
            using var kultur = new Kulturvorrichtung();
            string mit = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel
            {
                KuehlkurveVorlaufMittelC = 16.27, KuehlkurveAbsenkungKh = 412.46, KuehlkurveVorlaufgrenzeStundenH = 120,
            }));
            int ab = mit.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_KUEHLKURVE, StringComparison.Ordinal);
            Assert.True(ab >= 0, "Abschnitt fehlt");
            string teil = mit.Substring(ab);
            foreach (string titel in ProjektbeschreibungBaustein.TITEL_KUEHLKURVE) Assert.Contains(titel, teil);
            Assert.Contains("16,3", teil);
            Assert.Contains("412,5", teil);
            Assert.Contains("120", teil);
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_KUEHLKURVE, teil);

            // Ohne Mittel (keine Kuehlstunde): der Vorlauf steht als „—".
            string halb = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel
            {
                KuehlkurveAbsenkungKh = 0.0, KuehlkurveVorlaufgrenzeStundenH = 0,
            }));
            string teilHalb = halb.Substring(halb.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_KUEHLKURVE, StringComparison.Ordinal));
            Assert.Equal(1, teilHalb.Split('—').Length - 1 - (ProjektbeschreibungBaustein.HINWEIS_KUEHLKURVE.Split('—').Length - 1));

            string ohne = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel
            {
                Ak3KaelteschrankeStundenH = 37, Ak3UmschaltStundenH = 5, Ak3KaelterestStundenH = 2, Ak3KaelterestMwh = 0.25,
            }));
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_KUEHLKURVE, ohne);
            Assert.Contains(ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3K, ohne);
        }

        [Fact]
        public void Die_Texte_der_Tafel_haben_eine_Uebersetzung()
        {
            foreach (string de in ProjektbeschreibungBaustein.TITEL_KUEHLKURVE
                         .Concat(new[] { ProjektbeschreibungBaustein.UEBERSCHRIFT_KUEHLKURVE, ProjektbeschreibungBaustein.HINWEIS_KUEHLKURVE }))
                Assert.NotEqual(de, BerichtTexte.T(de, englisch: true));
        }

        [Fact]
        public void Die_Kacheltexte_stehen_in_beiden_Sprachen()
        {
            string[] schluessel =
            {
                "KK_GEBB_KACHEL_VORLAUF_MITTEL", "KK_GEBB_KACHEL_ABSENKUNG", "KK_GEBB_KACHEL_VORLAUFGRENZE", "KK_GEBB_QUELLE",
            };
            foreach (string s in schluessel)
            {
                string de = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s, new System.Globalization.CultureInfo("de-DE"));
                string en = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s, new System.Globalization.CultureInfo("en-US"));
                Assert.False(string.IsNullOrWhiteSpace(de), s);
                Assert.False(string.IsNullOrWhiteSpace(en), s);
                Assert.NotEqual(de, en);
            }
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static void Rechnen(int projekt)
        {
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            Assert.True(r.SimuliereUndSpeichere(projekt, out string fehler) > 0, "Lauf " + projekt + " gescheitert: " + fehler);
        }

        private static object Kennzahl(int projekt, string spalte)
            => DataRepository.ExecuteScalar("SELECT e." + spalte + " FROM Tab_ErgebnisEnergiebedarf e JOIN Tab_Ergebnis k ON k.ID = e.ID_Ergebnis " +
                                            "WHERE k.ID_Projekt = ? ORDER BY e.ID DESC LIMIT 1", new DbParam("?", projekt));

        private static BerichtsDaten Berichtsdaten(ErgebnisEnergiebedarfModel energie)
        {
            var erg = new ErgebnisModel { Energiebedarf = energie };
            erg.Gebaeude.Add(new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = 1, Merkplatz = 1, Gebaeudename = "Haus A", Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
                HeizwaermeMwh = 70.67, SpitzeKw = 34.5,
            });
            var eingaben = new DataTable();
            eingaben.Columns.Add("ID", typeof(long));
            eingaben.Rows.Add(1L);
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
    }
}
