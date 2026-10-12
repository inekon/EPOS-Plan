using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BK-2b — Gruppe, Klasse und Kennzeichen in der Oberfläche</b>: die Anzeigetexte jeder Gruppe und Klasse in beiden
    /// Sprachen, der Grund des Kennzeichens nach der Kernregel, die Spalten der Katalogprofile, die Katalogzeilen der
    /// Controller, der Speicherweg der Hüllen (Felder der Quelle und Quelle eines Importsatzes bleiben) und die Filterzeit
    /// über einen synthetischen Katalog in der Größe des eingelesenen. Alle Werte sind synthetisch.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BauteilkatalogOberflaecheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // ---- ohne Datenbank -----------------------------------------------------------------

        [Fact]
        public void Jede_Gruppe_und_Klasse_hat_ihren_Text_in_beiden_Sprachen()
        {
            var de = CultureInfo.GetCultureInfo("de-DE");
            var en = CultureInfo.GetCultureInfo("en-US");
            var schluessel = new List<string>();
            schluessel.AddRange(BauteilkatalogSchema.BAUSTOFF_GRUPPEN.Select(g => "BST_GRUPPE_" + g));
            schluessel.AddRange(BauteilkatalogSchema.BAUSTOFF_KLASSEN.Select(k => "BKAT_BST_KLASSE_" + k.Klasse));
            schluessel.AddRange(BauteilkatalogSchema.AUFBAU_GRUPPEN.Select(g => "BKAT_BTA_GRUPPE_" + g));
            schluessel.AddRange(BauteilkatalogSchema.AUFBAU_KLASSEN.Select(k => "BKAT_BTA_KLASSE_" + k.Klasse));
            foreach (string s in schluessel)
            {
                Assert.False(string.IsNullOrWhiteSpace(global::WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s, de)), s + " (de)");
                Assert.False(string.IsNullOrWhiteSpace(global::WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s, en)), s + " (en)");
            }
            Assert.Equal(48 + 12 + 5 + 14, schluessel.Count);
            Assert.Equal("Dächer", Bauteilkatalog.GruppeText(false, "DAECHER"));
            Assert.Equal("Steildach", Bauteilkatalog.KlasseText(false, "STEILDACH"));
            Assert.Equal("Mineralwolle", Bauteilkatalog.KlasseText(true, "MINERALWOLLE"));
            Assert.Equal("", Bauteilkatalog.KlasseText(true, null));
            Assert.Equal("UNBEKANNT", Bauteilkatalog.KlasseText(true, "UNBEKANNT"));
        }

        [Fact]
        public void Die_Klassen_folgen_der_Gruppe()
        {
            Assert.Equal(new[] { "FLACHDACH", "STEILDACH" }, Bauteilkatalog.Klassen(false, "DAECHER"));
            Assert.Equal(new[] { "VOLLHOLZ", "HOLZWERKSTOFF" }, Bauteilkatalog.Klassen(true, "HOLZ"));
            Assert.Empty(Bauteilkatalog.Klassen(true, null));
            Assert.Empty(Bauteilkatalog.Klassen(true, ""));
            Assert.Equal(BauteilkatalogSchema.AUFBAU_GRUPPEN, Bauteilkatalog.Gruppen(false));
        }

        [Fact]
        public void Der_Grund_des_Kennzeichens_folgt_der_Kernregel()
        {
            var a = new BauteilaufbauModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND };
            Assert.Equal("Aufbau ohne Schicht", Bauteilkatalog.UngeeignetGrund(a));
            a.Schichten.Add(new BauteilschichtModel { Dicke = 0.2, Lambda = 0.5 });
            Assert.Null(Bauteilkatalog.UngeeignetGrund(a));
            a.Schichten.Add(new BauteilschichtModel { Dicke = 0.02, IstLuftschicht = true });
            Assert.Null(Bauteilkatalog.UngeeignetGrund(a));
            a.Schichten.Add(new BauteilschichtModel { Dicke = 0.01 });
            Assert.Equal("Schicht 3 ohne Wärmeleitfähigkeit", Bauteilkatalog.UngeeignetGrund(a));
            Assert.True(Bauteilkatalog.MeistUngeeignet(a));

            var fenster = new BauteilaufbauModel { Bauteilart = DbWerte.BAUTEILART_FENSTER };
            Assert.Equal("Öffnung ohne Schicht und ohne U-Wert der Quelle", Bauteilkatalog.UngeeignetGrund(fenster));
            fenster.Zusatz[BauteilkatalogSchema.SPALTE_U_QUELLE] = 1.1;
            Assert.Null(Bauteilkatalog.UngeeignetGrund(fenster));
            Assert.False(Bauteilkatalog.MeistUngeeignet(fenster));
        }

        [Fact]
        public void Die_Profile_tragen_Klasse_Herkunft_und_Eignung()
        {
            Katalogfilterprofil bst = Katalogfilterprofil.FuerBaustoff();
            Assert.Contains(bst.Spalten, s => s.Schluessel == Katalogfilterprofil.SpKlasse && s.Filterbar);
            Assert.Contains(bst.Spalten, s => s.Schluessel == Katalogfilterprofil.SpHerkunft && s.Filterbar);
            Assert.DoesNotContain(bst.Spalten, s => s.Schluessel == Katalogfilterprofil.SpEignung);

            Katalogfilterprofil bta = Katalogfilterprofil.FuerBauteilaufbau();
            Katalogspalte eignung = bta.Spalten.Single(s => s.Schluessel == Katalogfilterprofil.SpEignung);
            Assert.True(eignung.Filterbar);
            Assert.Equal(Katalogspaltenrang.BeiPlatz, eignung.Rang);
            Assert.Contains(bta.Spalten, s => s.Schluessel == Katalogfilterprofil.SpGruppe);
            Assert.Contains(bta.Spalten, s => s.Schluessel == Katalogfilterprofil.SpKlasse);
            Assert.Equal(bta.Spalten.Count, bta.Spalten.Select(s => s.Schluessel).Distinct().Count());
        }

        /// <summary>
        /// Die Filterzeit über 10 000 Baustoff- und 8 500 Aufbauzeilen (synthetisch, Größe des eingelesenen Katalogs):
        /// „nur geeignete" und Gruppe zusammen bleiben weit unter einer Sekunde.
        /// </summary>
        [Fact]
        public void Der_Filter_bleibt_mit_einem_grossen_Katalog_flink()
        {
            Katalogfilterprofil bta = Katalogfilterprofil.FuerBauteilaufbau();
            var aufbauten = new List<Katalogfilterzeile>(8500);
            for (int i = 1; i <= 8500; i++)
                aufbauten.Add(new Katalogfilterzeile(i, "Aufbau " + i)
                    .MitText(Katalogfilterprofil.SpBezeichner, "Aufbau " + i)
                    .MitText(Katalogfilterprofil.SpEignung, i % 7 == 0 ? "meist ungeeignet" : "")
                    .MitText(Katalogfilterprofil.SpGruppe, Bauteilkatalog.GruppeText(false, BauteilkatalogSchema.AUFBAU_GRUPPEN[i % 5]))
                    .MitZahl(Katalogfilterprofil.SpUWert, 0.1 + i % 30 * 0.05, 3));
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpEignung, Katalogfilterprofil.AUSDRUCK_LEER);
            var uhr = Stopwatch.StartNew();
            IReadOnlyList<Katalogfilterzeile> geeignet = Katalogfilter.Anwenden(bta, aufbauten, stand);
            stand.Setzen(Katalogfilterprofil.SpGruppe, "Wände");
            IReadOnlyList<Katalogfilterzeile> waende = Katalogfilter.Anwenden(bta, aufbauten, stand);
            long msAufbau = uhr.ElapsedMilliseconds;
            Assert.Equal(8500 - 8500 / 7, geeignet.Count);
            Assert.All(waende, z => Assert.Equal(Katalogwert.AusText("").Text, z.Text(Katalogfilterprofil.SpEignung)));
            Assert.All(waende, z => Assert.Equal("Wände", z.Text(Katalogfilterprofil.SpGruppe)));

            Katalogfilterprofil bst = Katalogfilterprofil.FuerBaustoff();
            var stoffe = new List<Katalogfilterzeile>(10000);
            for (int i = 1; i <= 10000; i++)
            {
                (string g, string k) = BauteilkatalogSchema.BAUSTOFF_KLASSEN[i % BauteilkatalogSchema.BAUSTOFF_KLASSEN.Count];
                stoffe.Add(new Katalogfilterzeile(i, "Stoff " + i)
                    .MitText(Katalogfilterprofil.SpBezeichner, "Stoff " + i)
                    .MitText(Katalogfilterprofil.SpGruppe, Bauteilkatalog.GruppeText(true, g))
                    .MitText(Katalogfilterprofil.SpKlasse, Bauteilkatalog.KlasseText(true, k))
                    .MitText(Katalogfilterprofil.SpHerkunft, i % 5 == 0 ? "Import" : "Vorgabe")
                    .MitZahl(Katalogfilterprofil.SpLambda, 0.03 + i % 50 * 0.04, 3));
            }
            var standStoff = new Katalogfilterstand();
            standStoff.Setzen(Katalogfilterprofil.SpKlasse, "Mineralwolle");
            standStoff.Setzen(Katalogfilterprofil.SpHerkunft, "Import");
            uhr.Restart();
            IReadOnlyList<Katalogfilterzeile> mw = Katalogfilter.Anwenden(bst, stoffe, standStoff);
            long msStoff = uhr.ElapsedMilliseconds;
            Assert.NotEmpty(mw);
            Assert.All(mw, z => Assert.Equal("Import", z.Text(Katalogfilterprofil.SpHerkunft)));
            Assert.True(msAufbau < 1000 && msStoff < 1000, "Filterzeit " + msAufbau + " ms / " + msStoff + " ms");
        }

        // ---- mit Testdatenbank ------------------------------------------------------------------

        [Fact]
        public void Die_Katalogzeilen_tragen_Gliederung_Herkunft_und_Marke()
        {
            if (!_db.Vorhanden) return;
            var stoffe = new BaustoffCtrl();
            var neu = new BaustoffModel { Bezeichner = "BK2b Probe Stoff", Gruppe = "Holz und Holzwerkstoffe", Lambda = 0.13, Rho = 500, Cp = 1600, KlasseCode = "VOLLHOLZ" };
            BaustoffCtrl.Ergebnis e = stoffe.KatalogAnlegen(neu);
            Assert.True(e.Ok, e.Meldung);
            Katalogfilterzeile z = BaustoffCtrl.Katalogfilterzeilen().Single(r => r.Id == e.Id);
            Assert.Equal("Vollholz", z.Text(Katalogfilterprofil.SpKlasse));
            Assert.Equal(global::WindowsFormsApplication1.MyResource.Resource.BKAT_HERKUNFT_EIGEN, z.Text(Katalogfilterprofil.SpHerkunft));
            Assert.Contains(BaustoffCtrl.Katalogfilterzeilen(), r => r.Text(Katalogfilterprofil.SpHerkunft) == global::WindowsFormsApplication1.MyResource.Resource.GIMP_HERKUNFT_VORGABE);

            var aufbauten = new BauteilaufbauCtrl();
            var a = new BauteilaufbauModel { Bezeichner = "BK2b Probe Aufbau", Bauteilart = DbWerte.BAUTEILART_DACH, GruppeCode = "DAECHER", KlasseCode = "STEILDACH" };
            a.Schichten.Add(new BauteilschichtModel { Dicke = 0.2 });                // ohne lambda
            BauteilaufbauCtrl.Ergebnis ea = aufbauten.KatalogSpeichern(a);
            Assert.True(ea.Ok, ea.Meldung);
            IReadOnlyList<Katalogfilterzeile> zeilen = BauteilaufbauCtrl.Katalogfilterzeilen();
            Katalogfilterzeile za = zeilen.Single(r => r.Id == ea.Id);
            Assert.Equal(global::WindowsFormsApplication1.MyResource.Resource.BKAT_MARKE_UNGEEIGNET, za.Text(Katalogfilterprofil.SpEignung));
            Assert.Equal("Dächer", za.Text(Katalogfilterprofil.SpGruppe));
            Assert.Equal("Steildach", za.Text(Katalogfilterprofil.SpKlasse));

            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpEignung, Katalogfilterprofil.AUSDRUCK_LEER);
            IReadOnlyList<Katalogfilterzeile> geeignet = Katalogfilter.Anwenden(Katalogfilterprofil.FuerBauteilaufbau(), zeilen, stand);
            Assert.DoesNotContain(geeignet, r => r.Id == ea.Id);
            Assert.Equal(zeilen.Count - 1, geeignet.Count);
        }

        [Fact]
        public void Die_Huellen_behalten_Felder_der_Quelle_und_die_Quelle_eines_Importsatzes()
        {
            if (!_db.Vorhanden) return;
            var stoffe = new BaustoffCtrl();
            var neu = new BaustoffModel { Bezeichner = "BK2b Probe Import", Gruppe = "Dämmstoffe", Lambda = 0.04, Rho = 30, Cp = 1000, Quelle = "Probequelle" };
            neu.Zusatz["Mue_Min"] = 1.0;
            BaustoffCtrl.Ergebnis e = stoffe.KatalogAnlegen(neu);
            Assert.True(e.Ok, e.Meldung);
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Baustoff_STAMM\" SET \"Herkunft\" = 'IMPORT' WHERE \"ID\" = ?", new DbParam("@id", e.Id));

            EPOS.UI.Dialoge.Bedarf.BaustoffDaten d = BaustoffKatalogHuelle.AlsDaten(stoffe.LesenKatalogsatz(e.Id));
            Assert.True(d.Import);
            d.Quelle = "von Hand";
            d.KlasseCode = "MINERALWOLLE";
            d.Lambda = 0.035;
            Assert.True(BaustoffKatalogHuelle.Speichern(stoffe, d).Ok);
            BaustoffModel gelesen = stoffe.LesenKatalogsatz(e.Id);
            Assert.Equal("Probequelle", gelesen.Quelle);
            Assert.Equal("MINERALWOLLE", gelesen.KlasseCode);
            Assert.Equal(1.0, Bauteilkatalog.Zahl(gelesen.Zusatz, "Mue_Min"));

            var aufbauten = new BauteilaufbauCtrl();
            var a = new BauteilaufbauModel { Bezeichner = "BK2b Probe Fenster", Bauteilart = DbWerte.BAUTEILART_FENSTER };
            a.Zusatz[BauteilkatalogSchema.SPALTE_U_QUELLE] = 1.2;
            a.Zusatz["Rsi"] = 0.13;
            BauteilaufbauCtrl.Ergebnis ea = aufbauten.KatalogSpeichern(a);
            Assert.True(ea.Ok, ea.Meldung);
            Assert.False(aufbauten.LesenKatalogsatz(ea.Id).SimulationUngeeignet);

            EPOS.UI.Dialoge.Bedarf.BauteilaufbauDaten da = BauteilaufbauHuelle.AlsDaten(aufbauten.LesenKatalogsatz(ea.Id));
            Assert.Equal(1.2, da.UQuelle);
            Assert.Equal(("OEFFNUNGEN", "FENSTER"), (da.GruppeCode, da.KlasseCode));
            Assert.Equal("", BauteilaufbauHuelle.Kennwerte(da).UngeeignetGrund);
            da.KlasseCode = "DACHFENSTER";
            Assert.True(BauteilaufbauHuelle.Speichern(aufbauten, da).Ok);
            BauteilaufbauModel ga = aufbauten.LesenKatalogsatz(ea.Id);
            Assert.Equal("DACHFENSTER", ga.KlasseCode);
            Assert.Equal(0.13, Bauteilkatalog.Zahl(ga.Zusatz, "Rsi"));
            Assert.False(ga.SimulationUngeeignet);
        }
    }
}
