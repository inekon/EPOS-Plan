using System;
using System.Collections.Generic;
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
    /// <b>Anlagenkopplung AK3, Welle W4b — Texte, Bericht und Auskünfte im Kern</b> (Entwurf AK3 Festlegungen 20, 22, 23;
    /// E102): die Meldung des Konvergenzfehlers und der Rückstufungstext aus den Ressourcen (beide Sprachen), die Tafel
    /// des Kreises im Bericht nur mit Wert, die Rückstufe der Aufheizauskunft im Datenobjekt und das Lesen der Kennzahlen
    /// des letzten Laufs für den Bedarfsdialog.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3OberflaecheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static void StufeSetzen(int projekt, string stufe)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Anlagenkopplung = ? WHERE ID_Projekt = ?",
                                                     new DbParam("@s", stufe), new DbParam("@p", projekt)));

        [Fact]
        public void Die_Meldung_des_Konvergenzfehlers_kommt_aus_den_Ressourcen_beider_Sprachen()
        {
            using (new Kulturvorrichtung())
            {
                string de = new AnlagenkopplungException("Haus A (7)", 17, "WP 1, Speicher", "θ 0,2 K", "40 Durchläufe").Message;
                Assert.Contains("Jahresstunde 17", de);
                Assert.Contains("Haus A (7)", de);
                Assert.Contains("„Heizkreis (AK1)“", de);
                Assert.Equal(17, new AnlagenkopplungException("x", 17, "y", "z", "w").Stunde);
            }
            using (new Kulturvorrichtung("en-US"))
            {
                string en = new AnlagenkopplungException("Haus A (7)", 17, "WP 1", "θ 0.2 K", "40 iterations").Message;
                Assert.Contains("hour 17 of the year", en);
                Assert.Contains("“Heating circuit (AK1)”", en);
            }
        }

        [Fact]
        public void Der_Rueckstufungstext_kommt_aus_den_Ressourcen()
        {
            using (new Kulturvorrichtung())
            {
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.AK3_RUECKSTUFE, Ak3Kernstufe.Rueckstufetext);
                Assert.StartsWith("Berechnet ohne geschlossenen Kreis (Profilweg)", Ak3Kernstufe.Rueckstufetext);
            }
            using (new Kulturvorrichtung("en-US"))
                Assert.StartsWith("Calculated without the closed loop", Ak3Kernstufe.Rueckstufetext);
        }

        // ------------------------------------------------------------------ Bericht

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

        [Fact]
        public void Die_Tafel_des_Kreises_steht_im_Bericht_nur_mit_Wert()
        {
            using var kultur = new Kulturvorrichtung();
            string mit = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel
            {
                Ak3DurchlaeufeMittel = 1.8437, Ak3DurchlaeufeMax = 6, Ak3Fallwechsel = 42,
                Ak3SchrankeStundenH = 311, Ak3SpeicherLeerStundenH = 0, Ak3RestbedarfStundenH = 17,
            }));
            int ab = mit.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3, StringComparison.Ordinal);
            Assert.True(ab >= 0, "Abschnitt fehlt");
            string teil = mit.Substring(ab);
            foreach (string titel in ProjektbeschreibungBaustein.TITEL_AK3) Assert.Contains(titel, teil);
            Assert.Contains("1,84", teil);
            Assert.Contains("42", teil);
            Assert.Contains("311", teil);
            Assert.Contains("17", teil);
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_AK3, teil);

            string ohne = Schreibe(Berichtsdaten(new ErgebnisEnergiebedarfModel()));
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3, ohne);
        }

        [Fact]
        public void Die_Texte_der_Tafel_haben_eine_Uebersetzung()
        {
            foreach (string de in ProjektbeschreibungBaustein.TITEL_AK3
                         .Concat(new[] { ProjektbeschreibungBaustein.UEBERSCHRIFT_AK3, ProjektbeschreibungBaustein.HINWEIS_AK3 }))
                Assert.NotEqual(de, BerichtTexte.T(de, englisch: true));
        }

        [Fact]
        public void Die_Kennzahlen_entstehen_nur_mit_dem_Mittel_der_Durchlaeufe()
        {
            Assert.Null(Ak3Kennzahlen.Aus(null));
            Assert.Null(Ak3Kennzahlen.Aus(new ErgebnisEnergiebedarfModel { Ak3Fallwechsel = 3 }));
            Ak3Kennzahlen a = Ak3Kennzahlen.Aus(new ErgebnisEnergiebedarfModel { Ak3DurchlaeufeMittel = 2.5, Ak3RestbedarfStundenH = 4 });
            Assert.Equal(2.5, a.DurchlaeufeMittel);
            Assert.Equal(4, a.RestbedarfStundenH);
            Assert.Null(a.DurchlaeufeMax);
        }

        // ------------------------------------------------------------------ Auskünfte (Datenbank)

        /// <summary>
        /// Festlegung 20: Die Aufheizauskunft nennt mit Stufe AK3 die Rückstufe im Datenobjekt — mit AK1 nicht.
        /// </summary>
        [Fact]
        public void Die_Aufheizauskunft_nennt_mit_AK3_die_Rueckstufe()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1047;
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            ProjektGebaeudeModel g = ctrl.items[0];

            Aufheizauskunft ak1 = AufheizauskunftCtrl.Gebaeude(PROJEKT, projekt.m_ID_Klimaregion, g, auchOhneSchalter: true);
            Assert.NotNull(ak1);
            Assert.Null(ak1.Rueckstufe);

            StufeSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK3);
            Aufheizauskunft ak3 = AufheizauskunftCtrl.Gebaeude(PROJEKT, projekt.m_ID_Klimaregion, g, auchOhneSchalter: true);
            Assert.NotNull(ak3);
            Assert.Equal(Ak3Kernstufe.Rueckstufetext, ak3.Rueckstufe);
            Assert.Equal(ak1.AufheizzeitMaxH, ak3.AufheizzeitMaxH);
        }

        /// <summary>
        /// Festlegung 22: Der Bedarfsdialog liest die Kennzahlen des letzten Laufs — nach einem Lauf mit AK3 dieselben
        /// Zahlen wie die Projektzeile, nach einem Lauf ohne Kreis keine.
        /// </summary>
        [Fact]
        public void Die_Kennzahlen_des_letzten_Laufs_liest_der_Bedarfsdialog()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1047;
            StufeSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK3);
            SimulationProtokoll.NeuStarten();
            Assert.True(new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);

            Ak3Kennzahlen a = GebaeudeBedarfCtrl.Ak3KennzahlenDesProjekts(PROJEKT);
            Assert.NotNull(a);
            object mittel = DataRepository.ExecuteScalar(
                "SELECT e.Ak3_Durchlaeufe_Mittel FROM Tab_ErgebnisEnergiebedarf e JOIN Tab_Ergebnis k ON k.ID = e.ID_Ergebnis " +
                "WHERE k.ID_Projekt = ? ORDER BY e.ID DESC LIMIT 1", new DbParam("?", PROJEKT));
            Assert.Equal(Convert.ToDouble(mittel, System.Globalization.CultureInfo.InvariantCulture), a.DurchlaeufeMittel);
            Assert.True(a.DurchlaeufeMax >= 1);

            StufeSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1);
            SimulationProtokoll.NeuStarten();
            Assert.True(new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out fehler) > 0, fehler);
            Assert.Null(GebaeudeBedarfCtrl.Ak3KennzahlenDesProjekts(PROJEKT));
            Assert.Null(GebaeudeBedarfCtrl.Ak3KennzahlenDesProjekts(0));
        }
    }
}
