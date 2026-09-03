using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Katalogfilter der vier Erzeugerdialoge (iU9-W6.0c), gefahren auf einer
    /// Arbeitskopie der Testdatenbank.
    ///
    /// <para>Bis Welle 6 standen diese Praedikate als Literalkette IN der Maske und liessen
    /// sich nur am Windows-Geraet pruefen. Der Wortlaut ist uebernommen (Regel F3) - was
    /// die Proben festhalten, ist deshalb der BESTAND, einschliesslich seiner beiden
    /// Ungenauigkeiten (Befund W6-O-1).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogFilterTests
    {
        // =================================================================================
        // Heizkessel
        // =================================================================================

        [Fact]
        public void Heizkessel_Alle_liefert_den_Katalog_sortiert()
        {
            // BEFUND W6-O-2 (Bestandsverhalten, bewusst nicht repariert - Regel F3):
            // Die Stufe "Alle" heisst "Ptherm Like '%'". Der Vergleich wandelt die Zahl in
            // Text; fuer NULL ergibt das wieder NULL, und der Satz faellt aus "Alle"
            // HERAUS. In der Testdatenbank ist das genau ein Kessel ("Test", ID 252, ohne
            // Ptherm) - 62 von 63. Dieselbe Fehlerklasse hat der Pufferspeicher als B0-10
            // getragen; dort steht seit Paket 9 die NULL-Absicherung
            // "(Gesamtvolumen IS NULL OR Gesamtvolumen Like '%')". Diese Probe haelt den
            // Unterschied fest, damit er nicht unbemerkt bleibt.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            var treffer = ctrl.Filtern("Alle", 0);

            ctrl.ReadAll();
            int ohnePtherm = ctrl.items.Count(m => Math.Abs(m.Ptherm) < 1e-9 &&
                                                   IstNullPtherm(m.ID));
            Assert.Equal(ctrl.rows - ohnePtherm, treffer.Count);

            var namen = treffer.Select(t => t.Bezeichner).ToList();
            Assert.Equal(namen.OrderBy(n => n, StringComparer.Ordinal).ToList(), namen);
        }

        /// <summary>Traegt der Katalogsatz gar kein Ptherm (NULL, nicht 0)?</summary>
        private static bool IstNullPtherm(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Ptherm FROM [" + HeizkesselStammCtrl.TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
            return v == null || v == DBNull.Value;
        }

        [Fact]
        public void Heizkessel_jede_Leistungsstufe_bleibt_in_ihren_Grenzen()
        {
            // Die fuenf Stufen 50 / 200 / 500 / 1000 kW aus Form_Heizkessel.SetFilter.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            double[] untergrenze = { 0, 0, 50, 200, 500, 1000 };
            double[] obergrenze = { double.MaxValue, 50, 200, 500, 1000, double.MaxValue };

            for (int stufe = 0; stufe < HeizkesselStammCtrl.LEISTUNG_SQL.Length; stufe++)
            {
                foreach (var z in ctrl.Filtern("Alle", stufe))
                {
                    ctrl.ReadById(z.Id);
                    Assert.InRange(ctrl.Ptherm, untergrenze[stufe], obergrenze[stufe]);
                }
            }
        }

        [Fact]
        public void Heizkessel_Gruppe_Gas_liefert_nur_Gaskessel()
        {
            // "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14" - die fuenf Gase des
            // Katalogs plus Biogas.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            var treffer = ctrl.Filtern("Gas", 0);
            Assert.NotEmpty(treffer);

            foreach (var z in treffer)
            {
                ctrl.ReadById(z.Id);
                Assert.True(ctrl.Brennstoff >= 1 && ctrl.Brennstoff <= 5 || ctrl.Brennstoff == 14,
                            "Brennstoff " + ctrl.Brennstoff + " gehoert nicht zur Gruppe Gas.");
            }
        }

        [Fact]
        public void Heizkessel_eine_unbekannte_Gruppe_hebt_die_Einengung_auf()
        {
            // BEFUND W6-O-1: Die Kette kennt "Sonstige", der Katalog fuehrt aber
            // "Sonstige Energieträger" - der Eintrag trifft nie. Bestandsverhalten,
            // bewusst nicht repariert (Regel F3).
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            Assert.Equal(ctrl.Filtern("Alle", 0).Count,
                         ctrl.Filtern("Sonstige Energieträger", 0).Count);
        }

        [Fact]
        public void Heizkessel_IdZu_findet_den_Katalogsatz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            var erste = ctrl.Filtern("Alle", 0).First();

            Assert.Equal(erste.Id, HeizkesselStammCtrl.IdZu(erste.Bezeichner));
            Assert.Equal(0, HeizkesselStammCtrl.IdZu("gibt es nicht"));
        }

        // =================================================================================
        // BHKW
        // =================================================================================

        [Fact]
        public void Bhkw_Alle_liefert_den_ganzen_Katalog_mit_Anzeigefeldern()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new BHKWStammCtrl();
            var treffer = ctrl.Filtern("Alle", 0);

            ctrl.ReadAll();
            Assert.Equal(ctrl.rows, treffer.Count);
            Assert.All(treffer, z => Assert.False(string.IsNullOrEmpty(z.Bezeichner)));
        }

        [Fact]
        public void Bhkw_die_achte_Leistungsstufe_ist_jetzt_erreichbar()
        {
            // ABWEICHUNG A-6: Im Bestand verglich BuildFilter gegen "über 1.200 kW",
            // waehrend die Liste "größer 1200 kW" zeigte - die Stufe traf nie und lieferte
            // still alle Leistungen. Ueber den Index greift sie.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new BHKWStammCtrl();
            var alle = ctrl.Filtern("Alle", 0);
            var stufe8 = ctrl.Filtern("Alle", 8);

            Assert.True(stufe8.Count <= alle.Count);
            foreach (var z in stufe8) Assert.True(z.Ptherm >= 1200);
        }

        [Fact]
        public void Bhkw_jede_Leistungsstufe_bleibt_in_ihren_Grenzen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new BHKWStammCtrl();
            double[] unten = { 0, 0, 20, 40, 80, 200, 500, 800, 1200 };
            double[] oben = { double.MaxValue, 20, 40, 80, 200, 500, 800, 1200, double.MaxValue };

            for (int stufe = 0; stufe < BHKWStammCtrl.LeistungFilterText.Length; stufe++)
                foreach (var z in ctrl.Filtern("Alle", stufe))
                    Assert.InRange(z.Ptherm, unten[stufe], oben[stufe]);
        }

        [Fact]
        public void Bhkw_kennt_anders_als_der_Heizkessel_alle_zwoelf_Gruppen()
        {
            // Befund W6-O-1: Fernwärme=23, Sonstige Energieträger=24, Wasserstoff=25.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new BHKWStammCtrl();
            int alle = ctrl.Filtern("Alle", 0).Count;

            // Jede der drei Gruppen engt ein (sie trifft, auch wenn sie leer ausgeht).
            Assert.True(ctrl.Filtern("Fernwärme", 0).Count <= alle);
            Assert.True(ctrl.Filtern("Sonstige Energieträger", 0).Count <= alle);
            Assert.True(ctrl.Filtern("Wasserstoff", 0).Count <= alle);
        }

        // =================================================================================
        // Photovoltaik
        // =================================================================================

        [Fact]
        public void Pv_Hersteller_kommen_ohne_Dublette_und_sortiert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var hersteller = PhotovoltaikStammCtrl.Hersteller();

            Assert.NotEmpty(hersteller);
            Assert.Equal(hersteller.Distinct().Count(), hersteller.Count);
            Assert.Equal(hersteller.OrderBy(h => h, StringComparer.Ordinal).ToList(),
                         hersteller.ToList());
        }

        [Fact]
        public void Pv_ein_Hersteller_engt_die_Liste_ein()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new PhotovoltaikStammCtrl();
            string erster = PhotovoltaikStammCtrl.Hersteller().First(h => h.Length > 0);

            var treffer = ctrl.Filtern(erster);
            Assert.NotEmpty(treffer);
            Assert.All(treffer, z => Assert.Equal(erster, PhotovoltaikStammCtrl.Detail(z.Bezeichner).Firma));

            Assert.True(treffer.Count <= ctrl.Filtern("Alle").Count);
        }

        [Fact]
        public void Pv_Detail_liefert_die_vier_Anzeigefelder()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new PhotovoltaikStammCtrl();
            var erste = ctrl.Filtern("Alle").First();

            var detail = PhotovoltaikStammCtrl.Detail(erste.Bezeichner);
            Assert.NotNull(detail);
            Assert.Equal(erste.Bezeichner, detail.Bezeichner);

            Assert.Null(PhotovoltaikStammCtrl.Detail("gibt es nicht"));
        }

        // =================================================================================
        // Pufferspeicher
        // =================================================================================

        [Fact]
        public void PufferSp_sechs_Volumenstufen_mit_sechs_Texten()
        {
            Assert.Equal(PufferSpStammCtrl.VOLUMEN_SQL.Length,
                         PufferSpStammCtrl.VolumenTexte().Count);
        }

        [Fact]
        public void PufferSp_Stufe_Alle_zeigt_auch_Saetze_ohne_Volumen()
        {
            // Die NULL-Absicherung aus Paket 9: "Gesamtvolumen Like '%'" allein liesse
            // einen Satz ohne gepflegtes Volumen unsichtbar.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new PufferSpStammCtrl();
            ctrl.ReadAll();
            Assert.Equal(ctrl.rows, ctrl.Filtern("", 0).Count);
        }

        [Fact]
        public void PufferSp_jede_Volumenstufe_bleibt_in_ihren_Grenzen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new PufferSpStammCtrl();
            double[] unten = { 0, 0, 100, 200, 500, 1000 };
            double[] oben = { double.MaxValue, 100, 200, 500, 1000, double.MaxValue };

            for (int stufe = 1; stufe < PufferSpStammCtrl.VOLUMEN_SQL.Length; stufe++)
                foreach (var z in ctrl.Filtern("", stufe))
                {
                    var d = PufferSpStammCtrl.Detail(z.Id);
                    double v = double.Parse(d.Gesamtvolumen,
                                            System.Globalization.CultureInfo.CurrentCulture);
                    Assert.InRange(v, unten[stufe], oben[stufe]);
                }
        }

        [Fact]
        public void PufferSp_ein_Hersteller_engt_die_Liste_ein()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new PufferSpStammCtrl();
            var hersteller = PufferSpStammCtrl.Hersteller();
            Assert.NotEmpty(hersteller);

            var treffer = ctrl.Filtern(hersteller.First(), 0);
            Assert.All(treffer, z => Assert.Equal(hersteller.First(),
                                                  PufferSpStammCtrl.Detail(z.Id).Hersteller));
            Assert.True(treffer.Count <= ctrl.Filtern("", 0).Count);
        }

        [Fact]
        public void PufferSp_Detail_ueber_die_Id_und_ueber_den_Namen_treffen_dasselbe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new PufferSpStammCtrl();
            var erste = ctrl.Filtern("", 0).First();

            var ueberId = PufferSpStammCtrl.Detail(erste.Id);
            var ueberNamen = PufferSpStammCtrl.Detail(erste.Bezeichner);

            Assert.NotNull(ueberId);
            Assert.NotNull(ueberNamen);
            Assert.Equal(erste.Bezeichner, ueberId.Bezeichner);
            Assert.Equal(ueberId.Bezeichner, ueberNamen.Bezeichner);

            Assert.Null(PufferSpStammCtrl.Detail(999999));
        }
    }
}
