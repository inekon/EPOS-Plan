using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Gegenüberstellung JE GEWERK der Seite „Übersicht" (Anwenderauftrag 12.10.2026,
    /// <see cref="KomponentenVergleich.JeGewerk"/>): eine Zeile je Gewerk, je Version eine
    /// Zelle „n × Komponente“ mit Kennwerten, die Abweichung zum Stamm im Kern entschieden.
    /// Synthetische Bestände ohne Datenbank, dazu eine Probe gegen die Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KomponentenVergleichJeGewerkTests : System.IDisposable
    {
        /// <summary>Kennwerte mit deutschem Dezimalkomma, Texte deutsch — de-DE gepinnt.</summary>
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  Zeilenbildung, Zusammenfassung, fehlende Komponente, Abweichung
        // =============================================================================

        [Fact]
        public void Eine_Zeile_je_Gewerk_mit_Anzahl_Bezeichner_und_Kennwert()
        {
            var stamm = new Bestand()
                .Geraet("Wärmepumpe", WizardItemClass.WP_TYP, "ID_WP", 10, "WP A", ("Nennleistung", 16.0))
                .Geraet("Wärmepumpe", WizardItemClass.WP_TYP, "ID_WP", 10, "WP A", ("Nennleistung", 16.0))
                .Fertig();
            var variante = new Bestand()
                .Geraet("Wärmepumpe", WizardItemClass.WP_TYP, "ID_WP", 10, "WP A", ("Nennleistung", 16.0))
                .Geraet("Wärmepumpe", WizardItemClass.WP_TYP, "ID_WP", 10, "WP A", ("Nennleistung", 16.0))
                .Geraet("Pufferspeicher", WizardItemClass.PUFFER_TYP, "ID_PUFFER", 20, "Speicher 1",
                        ("Gesamtvolumen", 1000.0))
                .Fertig();

            List<KomponentenVergleichGewerk> gewerke = KomponentenVergleich.JeGewerk(new[] { stamm, variante });

            Assert.Equal(new[] { "Wärmepumpe", "Pufferspeicher" }, gewerke.Select(g => g.Gewerk));

            // Zwei gleiche Wärmepumpen werden EIN Eintrag „2 × WP A“ mit ihrem Kennwert.
            KomponentenVergleichEintrag wp = gewerke[0].Zellen[0].Eintraege.Single();
            Assert.Equal(2, wp.Anzahl);
            Assert.Equal("WP A", wp.Bezeichner);
            Assert.Equal("16 kW", wp.Kennwerte);
            Assert.False(gewerke[0].WeichtAb);

            // Der Stamm führt keinen Puffer: leere Zelle; die Variante weicht ab.
            KomponentenVergleichGewerk puffer = gewerke[1];
            Assert.False(puffer.Zellen[0].Vorhanden);
            Assert.False(puffer.Zellen[0].WeichtAb);
            Assert.True(puffer.Zellen[1].WeichtAb);
            Assert.True(puffer.WeichtAb);
            Assert.Equal("1.000 l", puffer.Zellen[1].Eintraege.Single().Kennwerte);
        }

        [Fact]
        public void Ein_anderer_Kennwert_ist_eine_Abweichung_ein_gleicher_nicht()
        {
            var stamm = new Bestand()
                .Geraet("Spitzenkessel", WizardItemClass.KESSEL_TYP, "ID_Kessel", 1, "Kessel", ("Ptherm", 28.0))
                .Geraet("Stromspeicher", WizardItemClass.SP_TYP, "ID_SP", 2, "Speicher", ("Energie", 12.8))
                .Fertig();
            var variante = new Bestand()
                .Geraet("Spitzenkessel", WizardItemClass.KESSEL_TYP, "ID_Kessel", 1, "Kessel", ("Ptherm", 35.0))
                .Geraet("Stromspeicher", WizardItemClass.SP_TYP, "ID_SP", 2, "Speicher", ("Energie", 12.8))
                .Fertig();

            List<KomponentenVergleichGewerk> gewerke = KomponentenVergleich.JeGewerk(new[] { stamm, variante });

            KomponentenVergleichGewerk kessel = gewerke.Single(g => g.Gewerk == "Spitzenkessel");
            Assert.Equal("28 kW", kessel.Zellen[0].Eintraege[0].Kennwerte);
            Assert.True(kessel.Zellen[1].WeichtAb);

            KomponentenVergleichGewerk speicher = gewerke.Single(g => g.Gewerk == "Stromspeicher");
            Assert.Equal("12,8 kWh", speicher.Zellen[1].Eintraege[0].Kennwerte);
            Assert.False(speicher.WeichtAb);
        }

        /// <summary>
        /// BHKW in kW el und kW th, Photovoltaik in kWp aus der Anlagenzeile, Solarthermie
        /// als Aperturfläche × Modulanzahl der Anlagenzeile.
        /// </summary>
        [Fact]
        public void Kennwerte_von_BHKW_Photovoltaik_und_Solarthermie()
        {
            ProjektDetails d = new Bestand()
                .Geraet("BHKW", WizardItemClass.BHKW_TYP, "ID_BHKW", 3, "BHKW 1", ("Pel", 5.5), ("Ptherm", 12.5))
                .Geraet("Photovoltaik", WizardItemClass.PV_TYP, "ID_PV", 4, "Modul A", ("Leistung", 400.0))
                .Anlagenwert("PV_Leistung", 9.6)
                .Geraet("Solarthermie", WizardItemClass.SOLAR_TYP, "ID_Solar", 5, "Kollektor", ("Aperturflaeche", 2.5))
                .Anlagenwert("Kollektormodulanzahl", 4.0)
                .Fertig();

            Assert.Equal("5,5 kW el · 12,5 kW th", KomponentenVergleich.Kennwerte(d, "BHKW", 0));
            Assert.Equal("9,6 kWp", KomponentenVergleich.Kennwerte(d, "Photovoltaik", 0));
            Assert.Equal("10 m²", KomponentenVergleich.Kennwerte(d, "Solarthermie", 0));
        }

        [Fact]
        public void Ohne_Daten_bleibt_der_Kennwert_leer_und_ohne_Versionen_gibt_es_keine_Zeile()
        {
            ProjektDetails d = new Bestand()
                .Geraet("Wärmepumpe", WizardItemClass.WP_TYP, "ID_WP", 1, "WP ohne Leistung")
                .Fertig();

            Assert.Equal("", KomponentenVergleich.Kennwerte(d, "Wärmepumpe", 0));
            Assert.Equal("", KomponentenVergleich.Kennwerte(d, "Wärmepumpe", 5));
            Assert.Empty(KomponentenVergleich.JeGewerk(new ProjektDetails[0]));
        }

        // =============================================================================
        //  Gegen die Testdatenbank: 1042 „Booster-Kette mit Kombi-Speicher" und 1044
        // =============================================================================

        [Fact]
        public void Die_Gruppe_der_Testdatenbank_zeigt_ihre_Gewerke_mit_Kennwerten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var versionen = new[] { ProjektDetails.Lade(1042), ProjektDetails.Lade(1044) };
            List<KomponentenVergleichGewerk> gewerke = KomponentenVergleich.JeGewerk(versionen);

            Assert.Equal(new[] { "Wärmepumpe", "Spitzenkessel", "Pufferspeicher" }, gewerke.Select(g => g.Gewerk));
            foreach (KomponentenVergleichGewerk g in gewerke)
            {
                Assert.Equal(2, g.Zellen.Count);
                Assert.Equal(AbweichungsErmittler.Anzahl(versionen[0], g.Gewerk),
                             g.Zellen[0].Eintraege.Sum(e => e.Anzahl));
            }
            Assert.All(gewerke[0].Zellen[0].Eintraege, e => Assert.EndsWith(" kW", e.Kennwerte));
        }

        // =============================================================================
        //  Die Einheitenzahl der Statuszeile in Ein- und Mehrzahl
        // =============================================================================

        [Fact]
        public void Die_Einheitenzahl_steht_in_Ein_und_Mehrzahl()
        {
            var kultur = System.Globalization.CultureInfo.CurrentCulture;
            Assert.Equal("1 Einheit", SpeicherAnzeigeCtrl.Einheitentext(1, kultur));
            Assert.Equal("2 Einheiten", SpeicherAnzeigeCtrl.Einheitentext(2, kultur));
            Assert.Equal("0 Einheiten", SpeicherAnzeigeCtrl.Einheitentext(0, kultur));

            var flotte = new SpeicherEngine.FlottenStudieKonfiguration();
            flotte.Einheiten.Add(new SpeicherEngine.FlottenEinheit { Id = "E1" });
            string satz = SpeicherAnzeigeCtrl.FlottenKontextText(
                flotte, WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_FLOTTE);
            Assert.EndsWith("· 1 Einheit", satz);
        }

        // =============================================================================
        //  Hilfsmittel: ein Projektstand ohne Datenbank, mit Anlagen- und Gerätezeilen
        // =============================================================================

        private sealed class Bestand
        {
            private readonly ProjektDetails _d = new ProjektDetails { IdProjekt = 1 };
            private DataRow _letzteAnlage;
            private int _anlagenId;

            public Bestand()
            {
                _d.Anlagen = new DataTable();
                _d.Anlagen.Columns.Add("ID", typeof(long));
                _d.Anlagen.Columns.Add("ID_Type", typeof(long));
                foreach (string fk in new[] { "ID_WP", "ID_BHKW", "ID_Kessel", "ID_Solar", "ID_PV", "ID_PUFFER", "ID_SP" })
                    _d.Anlagen.Columns.Add(fk, typeof(long));
                _d.Anlagen.Columns.Add("PV_Leistung", typeof(double));
                _d.Anlagen.Columns.Add("Kollektormodulanzahl", typeof(double));
                _d.Anlagen.Columns.Add("Volumen", typeof(double));
                foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                    _d.KomponentenAnzahl[g.Key] = 0;
            }

            public Bestand Geraet(string gewerk, int typ, string fk, long id, string name,
                                  params (string Spalte, double Wert)[] werte)
            {
                if (!_d.KomponentenAlle.TryGetValue(gewerk, out DataTable dt))
                {
                    dt = new DataTable();
                    dt.Columns.Add("ID", typeof(long));
                    dt.Columns.Add("Bezeichner", typeof(string));
                    _d.KomponentenAlle[gewerk] = dt;
                }
                foreach ((string spalte, double _) in werte)
                    if (!dt.Columns.Contains(spalte)) dt.Columns.Add(spalte, typeof(double));
                DataRow r = dt.NewRow();
                r["ID"] = id;
                r["Bezeichner"] = name;
                foreach ((string spalte, double wert) in werte) r[spalte] = wert;
                dt.Rows.Add(r);
                _d.Komponenten[gewerk] = dt.Rows[0];
                _d.KomponentenAnzahl[gewerk] = dt.Rows.Count;

                _letzteAnlage = _d.Anlagen.NewRow();
                _letzteAnlage["ID"] = ++_anlagenId;
                _letzteAnlage["ID_Type"] = typ;
                _letzteAnlage[fk] = id;
                _d.Anlagen.Rows.Add(_letzteAnlage);
                return this;
            }

            public Bestand Anlagenwert(string spalte, double wert)
            {
                _letzteAnlage[spalte] = wert;
                return this;
            }

            public ProjektDetails Fertig() => _d;
        }
    }
}
