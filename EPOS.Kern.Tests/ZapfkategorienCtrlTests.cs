using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Kern liest Zapfkategorien und Parameter der Stochastik aus dem Katalog</b>
    /// (Umsetzungskonzept Zapfprofilgenerator 3.1 T2, 4.4; Stufe Z3, Gruppe 2): je Nutzungsart in
    /// der Reihenfolge des Katalogs, samt Kappung und Provenienz; ohne Tabelle (Stand vor 114)
    /// leer. Die fünf Parameter <c>Zapfprofil.Stochastik.*</c> kommen mit dem Parametersatz der
    /// Katalogversion, und die Vorgaben der Projektzeile wirken: Seed 1, zehn Realisierungen,
    /// Realisierungen des Bedarfstags = ⌈Vielfaches · 1/(1 − p)⌉.
    ///
    /// <para>Jeder Fall in einer eigenen leeren Datei (<see cref="TwwTestdatenbank"/>), alle Werte
    /// erfunden (Konzept Kapitel 6 (a)).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfkategorienCtrlTests
    {
        [Fact]
        public void Die_Kategorien_kommen_je_Nutzungsart_in_der_Reihenfolge_des_Katalogs()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int a = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            int b = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            int fremd = TwwTestdatenbank.NutzungsartAnlegen("Nutzung C", "T1", satz);
            // Bewusst außer der Reihe angelegt: gelesen wird nach Reihenfolge, nicht nach Id.
            TwwTestdatenbank.KategorieAnlegen(a, "Zweite", 2, 8.0, 5, 0.25, 2.0, kappung: 12.0);
            TwwTestdatenbank.KategorieAnlegen(a, "Erste", 1, 4.0, 1, 0.75, 1.0);
            TwwTestdatenbank.KategorieAnlegen(b, "Einzige", 1, 6.0, 3, 1.0, 1.5);
            TwwTestdatenbank.KategorieAnlegen(fremd, "Nicht gefragt", 1, 2.0, 1, 1.0, 0.5);

            IReadOnlyList<Zapfkategorie> k = ZapfprofilCtrl.Zapfkategorien(new[] { b, a, a });
            Assert.Equal(new[] { "Erste", "Zweite", "Einzige" }, k.Select(x => x.Name).ToArray());
            Assert.Equal(new[] { a, a, b }, k.Select(x => x.IdNutzungsart).ToArray());

            Zapfkategorie erste = k[0], zweite = k[1];
            Assert.Equal(4.0, erste.VolumenstromLJeMin);
            Assert.Equal(1.0, erste.StreuungLJeMin);
            Assert.Equal(1, erste.DauerMin);
            Assert.Equal(0.75, erste.Anteil);
            Assert.Null(erste.KappungLJeMin);
            Assert.Equal(12.0, zweite.KappungLJeMin);
            Assert.Equal(5, zweite.DauerMin);
            Assert.Equal(Herkunftsart.Fiktiv, erste.Herkunft.Art);
            Assert.Equal(TwwTestdatenbank.QUELLE, erste.Herkunft.Quelle);

            // Der Satz des Rechenwegs nimmt sie so an: Anteile auf Σ 1, Reihenfolge des Katalogs.
            Zapfkategoriensatz s = Zapfkategoriensatz.Aus(k, a, "Zone A");
            Assert.Equal(new[] { 0.75, 0.25 }, s.Werte.Select(w => w.AnteilNormiert).ToArray());
        }

        [Fact]
        public void Ohne_Tabelle_oder_ohne_Zeile_sind_die_Kategorien_leer()
        {
            using (var db = new TwwTestdatenbank(mitTwwSchema: false))
            {
                TwwTestdatenbank.SchemaAnlegen(mitT2: false);
                Assert.Empty(ZapfprofilCtrl.Zapfkategorien(new[] { 1, 2 }));
            }
            using (var db = new TwwTestdatenbank())
            {
                Assert.Empty(ZapfprofilCtrl.Zapfkategorien(new[] { 1 }));
                Assert.Empty(ZapfprofilCtrl.Zapfkategorien(null));
            }
        }

        /// <summary>
        /// Fehlt der Satz einer Nutzungsart und die Zone rechnet stochastisch, lehnt der Rechenweg
        /// benannt ab — mit der Nutzungsart (Bezeichner und Katalogversion) im Text und der Kennung,
        /// die die Hülle in beiden Sprachen übersetzt.
        /// </summary>
        [Fact]
        public void Ohne_Kategorien_nennt_die_Ablehnung_die_Nutzungsart()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int a = TwwTestdatenbank.NutzungsartAnlegen("Nutzung ohne Kategorien", "T1", satz);
            Nutzungsart art = TwwNutzungsartCtrl.Lies(a);

            ZapfprofilEingabeException ex = Assert.Throws<ZapfprofilEingabeException>(
                () => Zapfkategoriensatz.Aus(ZapfprofilCtrl.Zapfkategorien(new[] { a }), art, "Zone A"));
            Assert.Equal(ZapfEingabefehler.StochastikUngueltig, ex.Fehler);
            Assert.Equal("Zone A", ex.Zone);
            Assert.Equal(Zapfkategoriensatz.KENNUNG_KATEGORIEN_FEHLEN, ex.Kennung);
            Assert.Equal("„Nutzung ohne Kategorien“ (Katalogversion T1)", ex.Argument);
            Assert.Contains("für die Nutzungsart „Nutzung ohne Kategorien“ (Katalogversion T1) der Zone „Zone A“", ex.Message);

            ZapfAblehnung ab = ZapfAblehnung.Aus("Zone A", ex);
            Assert.Equal(ex.Kennung, ab.Kennung);
            Assert.Equal(ex.Argument, ab.Argument);
        }

        /// <summary>
        /// Die fünf Parameter der Stochastik kommen aus <c>Tab_TwwParameter_STAMM</c> der
        /// Katalogversion; die Projektzeile ohne Angabe trägt die Vorgaben der DDL (Seed 1, zehn
        /// Realisierungen, Realisierungen des Bedarfstags offen), und deren Vorgabe ist
        /// ⌈Vielfaches · 1/(1 − p)⌉ — gleich für p = 95 und 99, ein Projektwert geht vor.
        /// </summary>
        [Fact]
        public void Die_Parameter_der_Stochastik_und_die_Vorgaben_wirken()
        {
            using var db = new TwwTestdatenbank();
            var werte = new Dictionary<string, double>
            {
                [ZapfStochastikParameter.URLAUBSVERSATZ] = 20.0,
                [ZapfStochastikParameter.AUSLEGUNG_VIELFACHES] = 1.5,
                [ZapfStochastikParameter.KONSISTENZSCHWELLE] = 1.8,
                [ZapfStochastikParameter.QUANTIL + "95"] = 1.7,
                [ZapfStochastikParameter.QUANTIL + "99"] = 2.4,
            };
            foreach (var kv in werte) TwwTestdatenbank.ParameterAnlegen(kv.Key, kv.Value, "T1");

            Parametersatz ps = ZapfprofilCtrl.Parameter();
            Assert.Equal("T1", ps.Katalogversion);
            foreach (var kv in werte) Assert.Equal(kv.Value, ps.Wert(kv.Key));

            ProjektStand p = ZapfprofilCtrl.ProjektVorgabe();
            Assert.Equal(1L, p.Seed);
            Assert.Equal(10, p.Realisierungen);
            Assert.Null(p.RealisierungenAuslegung);
            Assert.False(p.JahresreiheStochastisch);

            Assert.Equal((int)Math.Ceiling(1.5 * 100), Zapfensemble.RealisierungenAuslegung(p.RealisierungenAuslegung, 99, ps));
            Assert.Equal((int)Math.Ceiling(1.5 * 20), Zapfensemble.RealisierungenAuslegung(p.RealisierungenAuslegung, 95, ps));
            Assert.Equal(7, Zapfensemble.RealisierungenAuslegung(7, 99, ps));
        }

        /// <summary>
        /// Auf der Repo-Testdatenbank: Der Eingang eines Arbeitsstands trägt die Kategorien der
        /// Nutzungsart seiner Zone — genau die Zeilen der Tabelle, in ihrer Reihenfolge —, die
        /// Parameter der Stochastik stehen im Parametersatz, und der stochastische Rechenweg rechnet
        /// die Zone mit den Kategorien des Katalogs: dieselbe Jahresmenge wie der deterministische,
        /// keine Ablehnung. Die Werte liest die Probe aus der Datenbank, keiner steht hier.
        /// </summary>
        [Fact]
        public void Der_Eingang_traegt_die_Kategorien_des_Katalogs_und_die_Zone_rechnet_stochastisch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int nutzung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", "Wohnen groß (abgeleitet)"), new DbParam("@k", "TEST-1")));
            var soll = DataRepository.GetDataTable(
                "SELECT Kategorie, Volumenstrom_l_min, Sigma FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart = ? " +
                "ORDER BY Reihenfolge, ID", new DbParam("@n", nutzung)).Rows.Cast<System.Data.DataRow>()
                .Select(r => (Convert.ToString(r[0]), Convert.ToDouble(r[1]), Convert.ToDouble(r[2]))).ToArray();
            Assert.NotEmpty(soll);

            var zone = new ZonenStand { Name = "Zone Probe", IdNutzungsart = nutzung, Bezugsmenge = 12.0 };
            ProjektStand deterministisch = ZapfprofilCtrl.ProjektVorgabe() with { Weg = BrauchwasserWeg.Generator };
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, deterministisch);
            bool[] we = new bool[Zapfkalender.TAGE];

            Zapfprofileingang e = ZapfprofilCtrl.Eingang(1007, stand, 0, we);
            Assert.Equal(soll, e.Zapfkategorien.Select(k => (k.Name, k.VolumenstromLJeMin, k.StreuungLJeMin)).ToArray());
            Assert.All(e.Zapfkategorien, k => Assert.Equal(nutzung, k.IdNutzungsart));
            foreach (string s in new[] { ZapfStochastikParameter.URLAUBSVERSATZ, ZapfStochastikParameter.AUSLEGUNG_VIELFACHES,
                                         ZapfStochastikParameter.KONSISTENZSCHWELLE, ZapfStochastikParameter.QUANTIL + "95",
                                         ZapfStochastikParameter.QUANTIL + "99" })
                Assert.True(e.Parameter.Enthaelt(s), s);

            ZapfprofilErgebnis d = ZapfprofilCtrl.Rechnen(1007, stand, 0, we);
            var stochastisch = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone },
                                                   deterministisch with { JahresreiheStochastisch = true, Realisierungen = 2 });
            ZapfprofilErgebnis st = ZapfprofilCtrl.Rechnen(1007, stochastisch, 0, we);
            Assert.True(st.Vollstaendig, string.Join("; ", st.Ablehnungen.Select(a => a.Klartext)));
            Assert.True(st.Stochastisch);
            Assert.InRange(Math.Abs(st.Zapfung.JahressummeKwh / d.Zapfung.JahressummeKwh - 1.0), 0.0, 1e-12);
            Assert.NotEqual(d.Zapfung.StundenKwh, st.Zapfung.StundenKwh);
        }
    }
}
