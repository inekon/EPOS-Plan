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
    /// <b>Die Kennzahlen von AK3-K im Ergebnis</b> (Entwurf AK3-K 3.5, Festlegung 20; Schemaschritt S1): Zonensperre und
    /// Kälteseite im Kreis in der Projektzeile, im Leser des Bedarfsdialogs und in der Tafel „Kälteseite im Kreis" des
    /// Berichts — jede Seite nur mit Wert, mit Schaltern aus überall NULL.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3KKennzahlenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Summe, Massstab, Projektzeile (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Summe_ueber_Zonen_addiert_und_ist_ohne_Eintrag_null()
        {
            Assert.Null(Zonensperrkennzahl.Summe(null));
            Assert.Null(Zonensperrkennzahl.Summe(new Zonensperrkennzahl[] { null, null }));
            var a = new Zonensperrkennzahl(3, 2, 10, 4, 12.5, 3.0, 40);
            var b = new Zonensperrkennzahl(1, 0, 2, 0, 1.5, 0.0, 5);
            Zonensperrkennzahl s = Zonensperrkennzahl.Summe(new[] { a, null, b });
            Assert.Equal(6, s.Tage);
            Assert.Equal(16, s.Stunden);
            Assert.Equal(14.0, s.HeizenGesperrtKwh, 12);
            Assert.Equal(3.0, s.KuehlenGesperrtKwh, 12);
            Assert.Equal(45, s.TageBeides);
            Zonensperrkennzahl x = a.Skaliert(2.0);
            Assert.Equal(25.0, x.HeizenGesperrtKwh, 12);
            Assert.Equal(6.0, x.KuehlenGesperrtKwh, 12);
            Assert.Equal(a.Tage, x.Tage);
        }

        [Fact]
        public void Die_Projektzeile_traegt_die_Zonensperre_in_MWh_und_bleibt_sonst_NULL()
        {
            var leer = new ErgebnisEnergiebedarfModel();
            SimulationRunner.Ak3KSpaltenSetzen(leer, null, null, kaelteImKreis: true);
            Assert.Null(leer.ZonensperreTage);
            Assert.Null(leer.Ak3KaelteschrankeStundenH);
            Assert.Null(Ak3KKennzahlen.Aus(leer));

            var e = new ErgebnisEnergiebedarfModel();
            SimulationRunner.Ak3KSpaltenSetzen(e, new Zonensperrkennzahl(7, 3, 20, 6, 1500.0, 250.0, 30), null, kaelteImKreis: false);
            Assert.Equal(10, e.ZonensperreTage);
            Assert.Equal(1.5, e.ZonensperreHeizenGesperrtMwh.Value, 12);
            Assert.Equal(0.25, e.ZonensperreKuehlenGesperrtMwh.Value, 12);
            Assert.Null(e.Ak3KaelteschrankeStundenH);
            Assert.Null(e.Ak3KaelterestMwh);
            Ak3KKennzahlen k = Ak3KKennzahlen.Aus(e);
            Assert.True(k.ZonensperreErhoben);
            Assert.False(k.KreisErhoben);
        }

        // =============================================================================
        //  Teil 2 - Lauf, Rundreise, Leser (Testdatenbank)
        // =============================================================================

        /// <summary>
        /// 1047 mit Zonensperre: die Projektzeile trägt Tage und gesperrte Energie, die Kälteseite des Kreises bleibt NULL
        /// (AK1); der Leser des Bedarfsdialogs liest dieselben Werte. Schalter aus: alle sieben Spalten NULL.
        /// </summary>
        [Fact]
        public void Der_Lauf_schreibt_die_Zonensperre_nur_mit_Schalter()
        {
            if (!_db.Vorhanden) return;
            Rechnen(1047);
            foreach (string s in Ak3KSchema.SPALTEN_ERGEBNIS)
                Assert.True(Kennzahl(1047, s) is null or DBNull, s + " ist mit Schaltern aus nicht NULL");
            Assert.Null(GebaeudeBedarfCtrl.Ak3KKennzahlenDesProjekts(1047));

            using (Zonensperre.Schalten(true))
                Rechnen(1047);
            Ak3KKennzahlen k = GebaeudeBedarfCtrl.Ak3KKennzahlenDesProjekts(1047);
            Assert.NotNull(k);
            Assert.True(k.ZonensperreTage > 0);
            Assert.True(k.HeizenGesperrtMwh >= 0.0 && k.KuehlenGesperrtMwh >= 0.0);
            Assert.True(k.HeizenGesperrtMwh + k.KuehlenGesperrtMwh > 0.0);
            Assert.False(k.KreisErhoben);
            foreach (string s in Ak3KSchema.SPALTEN_KREIS)
                Assert.True(Kennzahl(1047, s) is null or DBNull, s + " ist ohne Kreis nicht NULL");

            ErgebnisModel m = new ErgebnisCtrl().Load(1047);
            Assert.Equal(k.ZonensperreTage, m?.Energiebedarf.ZonensperreTage);
        }

        /// <summary>1058 auf AK3 mit beiden Schaltern: die Kälteseite des Kreises steht in der Projektzeile.</summary>
        [Fact]
        public void Der_Kreis_schreibt_die_Kaelteseite_mit_beiden_Schaltern()
        {
            if (!_db.Vorhanden) return;
            using (Zonensperre.Schalten(true))
            using (Ak3KKernschalter.Schalten(true))
                Rechnen(1058);
            Ak3KKennzahlen k = GebaeudeBedarfCtrl.Ak3KKennzahlenDesProjekts(1058);
            Assert.NotNull(k);
            Assert.True(k.ZonensperreErhoben);
            Assert.True(k.KreisErhoben);
            Assert.InRange(k.KaelteschrankeStundenH.Value, 0, 8760);
            Assert.InRange(k.UmschaltStundenH.Value, 0, 8760);
            Assert.InRange(k.KaelterestStundenH.Value, 0, 8760);
            Assert.True(k.KaelterestMwh >= 0.0);
        }

        // =============================================================================
        //  Teil 3 - die Tafel im Bericht
        // =============================================================================

        [Fact]
        public void Die_Tafel_Kaelteseite_im_Kreis_steht_im_Bericht_nur_mit_Wert()
        {
            using var kultur = new Kulturvorrichtung();
            string mit = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel
            {
                ZonensperreTage = 12, ZonensperreHeizenGesperrtMwh = 0.0512, ZonensperreKuehlenGesperrtMwh = 0.0134,
                Ak3KaelteschrankeStundenH = 37, Ak3UmschaltStundenH = 5, Ak3KaelterestStundenH = 2, Ak3KaelterestMwh = 0.25,
            }));
            int ab = mit.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3K, StringComparison.Ordinal);
            Assert.True(ab >= 0, "Abschnitt fehlt");
            string teil = mit.Substring(ab);
            foreach (string titel in ProjektbeschreibungBaustein.TITEL_AK3K) Assert.Contains(titel, teil);
            Assert.Contains("0,05", teil);
            Assert.Contains("37", teil);
            Assert.Contains("0,25", teil);
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_AK3K, teil);

            // Nur die Zonensperre erhoben: die Kälteseite steht als „—".
            string halb = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel
            {
                ZonensperreTage = 9, ZonensperreHeizenGesperrtMwh = 0.02, ZonensperreKuehlenGesperrtMwh = 0.01,
            }));
            string teilHalb = halb.Substring(halb.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3K, StringComparison.Ordinal));
            Assert.Equal(4, teilHalb.Split('—').Length - 1 - (ProjektbeschreibungBaustein.HINWEIS_AK3K.Split('—').Length - 1));

            string ohne = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel()));
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3K, ohne);
        }

        [Fact]
        public void Die_Texte_der_Tafel_haben_eine_Uebersetzung()
        {
            foreach (string de in ProjektbeschreibungBaustein.TITEL_AK3K
                         .Concat(new[] { ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3K, ProjektbeschreibungBaustein.HINWEIS_AK3K }))
                Assert.NotEqual(de, BerichtTexte.T(de, englisch: true));
        }

        [Fact]
        public void Die_Kacheltexte_stehen_in_beiden_Sprachen()
        {
            string[] schluessel =
            {
                "AK3K_GEBB_KACHEL_SPERRTAGE", "AK3K_GEBB_KACHEL_GESPERRT", "AK3K_GEBB_KACHEL_KAELTESCHRANKE",
                "AK3K_GEBB_KACHEL_UMSCHALTUNG", "AK3K_GEBB_KACHEL_KAELTEREST", "AK3K_GEBB_QUELLE_ZONENSPERRE",
                "AK3K_GEBB_QUELLE_KREIS",
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
