using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// 6b (Anwenderentscheid 16.10.2026) — <b>Emission und Stromsteuer des Hilfsstroms</b>.
    /// Der Hilfsstrom von BHKW und Brennstoffkesseln aus dem Hilfsenergieanteil geht mit dem
    /// Faktor des Netzstroms in die CO₂-Bilanz; seine Stromsteuer wird ausgewiesen und nur so
    /// weit gesondert angesetzt, wie der Arbeitspreis des Netzträgers sie nicht schon enthält.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class HilfsstromEmissionStromsteuerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int BHKW_GROSS = 14920;
        private const int BHKW_KLEIN = 14921;

        // =====================================================================
        //  Handrechnung der Stromsteuer (ohne Datenbank)
        // =====================================================================

        /// <summary>10 MWh Hilfsstrom, Regelsatz 20,50 €/MWh: Steuer 205,00 €/a. Ohne gepflegten
        /// Anteil gilt sie als im Arbeitspreis enthalten — gesondert 0.</summary>
        [Fact]
        public void Ohne_gepflegten_Anteil_ist_die_Steuer_im_Preis_enthalten()
        {
            HilfsstromStromsteuer.Ergebnis e = HilfsstromStromsteuer.Rechne(10.0, 20.50, null);
            Assert.Equal(205.00, e.SteuerEur, 9);
            Assert.Equal(205.00, e.EnthaltenEur, 9);
            Assert.Equal(0.0, e.GesondertEur, 9);
        }

        /// <summary>Abgeschaltete Komponente (Anteil 0): Der Preis enthält keine Stromsteuer, sie
        /// wird voll angesetzt — 10 MWh × 20,50 €/MWh = 205,00 €/a.</summary>
        [Fact]
        public void Ohne_Stromsteuer_im_Preis_wird_sie_voll_angesetzt()
        {
            HilfsstromStromsteuer.Ergebnis e = HilfsstromStromsteuer.Rechne(10.0, 20.50, 0.0);
            Assert.Equal(0.0, e.EnthaltenEur, 9);
            Assert.Equal(205.00, e.GesondertEur, 9);
        }

        /// <summary>Ein reduzierter Anteil von 15,37 €/MWh im Preis: gesondert die Differenz,
        /// 10 MWh × 5,13 €/MWh = 51,30 €/a; ein Anteil über dem Regelsatz setzt nichts an.</summary>
        [Fact]
        public void Gesondert_angesetzt_wird_nur_die_Differenz()
        {
            HilfsstromStromsteuer.Ergebnis e = HilfsstromStromsteuer.Rechne(10.0, 20.50, 15.37);
            Assert.Equal(153.70, e.EnthaltenEur, 9);
            Assert.Equal(51.30, e.GesondertEur, 9);
            Assert.Equal(205.00, e.EnthaltenEur + e.GesondertEur, 9);

            Assert.Equal(0.0, HilfsstromStromsteuer.Rechne(10.0, 20.50, 30.0).GesondertEur, 9);
            Assert.Null(HilfsstromStromsteuer.Rechne(0.0, 20.50, 0.0));
        }

        /// <summary>Die Nachweiszeile: 10.000 kWh × 0,00513 €/kWh = 51,30 €/a; die Bezeichnung
        /// nennt Regelsatz und enthaltenen Teil. Ohne Regelsatz keine Zeile, aber ein Hinweis.</summary>
        [Fact]
        public void Die_Nachweiszeile_nennt_Satz_und_enthaltenen_Teil()
        {
            KostenPositionNachweis n = HilfsstromStromsteuer.Nachweis(
                HilfsstromStromsteuer.Rechne(10.0, 20.50, 15.37));
            Assert.Equal(10000.0, n.Menge.Value, 9);
            Assert.Equal(0.00513, n.Einheitpreis.Value, 9);
            Assert.Equal(51.30, n.BetragJahr, 9);
            Assert.Equal(HilfsstromStromsteuer.HERKUNFT, n.SatzHerkunft);
            Assert.Equal("Stromsteuer auf Hilfsstrom (20,50 €/MWh, davon 15,37 €/MWh im Arbeitspreis enthalten)",
                         n.Bezeichnung);

            HilfsstromStromsteuer.Ergebnis ohne = HilfsstromStromsteuer.Rechne(10.0, null, 0.0);
            Assert.Null(HilfsstromStromsteuer.Nachweis(ohne));
            Assert.Equal(0.0, ohne.GesondertEur, 9);
            Assert.Contains("10,00 MWh/a", HilfsstromStromsteuer.HinweisOhneSatz(ohne));
        }

        // =====================================================================
        //  Am Referenzprojekt 1030 (Arbeitskopie der Testdatenbank)
        // =====================================================================

        private static void Anteil(int anlage, double? prozent)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Hilfsenergie_Anteil = ? WHERE ID = ?",
                new DbParam("@w", DbParamTyp.Double) { Wert = prozent.HasValue ? (object)prozent.Value : DBNull.Value },
                new DbParam("@a", DbParamTyp.Integer) { Wert = anlage });
        }

        private static VariantenDaten Variante()
        {
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Hilfsstrom " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        private static WirtschaftlichkeitErgebnis Rechne(out WirtschaftlichkeitParameter p)
        {
            p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            VariantenDaten v = Variante();
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p).First(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
        }

        /// <summary>Ohne Anteil (der ganze Bestand der Testdatenbank) kein Hilfsstrom, keine
        /// Emission, keine Steuerzeile.</summary>
        [Fact]
        public void Ohne_Anteil_bleibt_alles_wie_es_war()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Variante();
            Assert.Equal(0.0, v.HilfsstromMWh);
            Assert.Equal(0.0, v.HilfsstromCO2t);
            WirtschaftlichkeitErgebnis e = Rechne(out _);
            Assert.DoesNotContain(e.Betriebskosten, n => n.SatzHerkunft == HilfsstromStromsteuer.HERKUNFT);
        }

        /// <summary>
        /// 2 % an beiden BHKW von 1030: Hilfsstrom = 2 % des BHKW-Brennstoffs im Lauf
        /// (1.048,27 MWh → 20,9654 MWh). Die CO₂-Bilanz steigt um genau Hilfsstrom × Faktor
        /// des Netzstroms; die Stromsteuer bei abgeschalteter Komponente (Preis ohne Steuer)
        /// voll, bei einem Anteil in Höhe des Regelsatzes gar nicht gesondert.
        /// </summary>
        [Fact]
        public void Zwei_Prozent_an_beiden_BHKW_emittieren_und_versteuern_den_Hilfsstrom()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten vorher = Variante();
            WirtschaftlichkeitErgebnis eVorher = Rechne(out _);
            Anteil(BHKW_GROSS, 2.0);
            Anteil(BHKW_KLEIN, 2.0);

            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            double brennstoff = m.BHKW.Module.Sum(x => x.Verbrauch);
            Assert.Equal(1048.27, brennstoff, 2);
            double hilfs = brennstoff * 0.02;

            VariantenDaten v = Variante();
            Assert.Equal(hilfs, v.HilfsstromMWh, 9);
            int traeger = v.EnergiekostenJeTraeger.First(t => t.Netzstrom).CarrierId;
            double faktor = Emissionsquelle.Netzstrom(PROJEKT, traeger, v.EmissionsModus).Co2GKwh;
            Assert.True(faktor > 0);
            Assert.Equal(hilfs * faktor / 1000.0, v.HilfsstromCO2t, 9);
            Assert.Equal(vorher.CO2Gesamt.Value + v.HilfsstromCO2t, v.CO2Gesamt.Value, 9);

            // (a) Komponente abgeschaltet: der Arbeitspreis enthält keine Stromsteuer.
            var zerlegung = new StrompreisZerlegungCtrl();
            StrompreisZerlegungModel z = zerlegung.Read(PROJEKT, traeger);
            z.ID_Projekt = PROJEKT; z.ID_Energietraeger = traeger;
            z.Stromsteuer = 2.05; z.Stromsteuer_Aktiv = false;
            Assert.True(zerlegung.Update(z));

            WirtschaftlichkeitParameter p;
            WirtschaftlichkeitErgebnis e = Rechne(out p);
            int jahr = p.KwkgInbetriebnahme.HasValue ? p.KwkgInbetriebnahme.Value.Year : DateTime.Now.Year + 1;
            double regel = new GesetzKatalog().Wert(DbWerte.GESETZ_STROMST_REGELSATZ, jahr).Value;
            KostenPositionNachweis zeile = e.Betriebskosten.Single(n => n.SatzHerkunft == HilfsstromStromsteuer.HERKUNFT);
            Assert.Equal(hilfs * 1000.0, zeile.Menge.Value, 6);
            Assert.Equal(hilfs * regel, zeile.BetragJahr, 6);
            double hilfsKosten = hilfs * 1000.0 * 0.25;                       // E30: Arbeitspreis 0,25 €/kWh
            Assert.Equal(eVorher.BetriebskostenJahr.Value + hilfsKosten + hilfs * regel,
                         e.BetriebskostenJahr.Value, 4);

            // (b) Anteil in Höhe des Regelsatzes: enthalten, nichts gesondert — nie doppelt.
            z.Stromsteuer = regel / 10.0; z.Stromsteuer_Aktiv = true;
            Assert.True(zerlegung.Update(z));
            e = Rechne(out _);
            zeile = e.Betriebskosten.Single(n => n.SatzHerkunft == HilfsstromStromsteuer.HERKUNFT);
            Assert.Equal(0.0, zeile.BetragJahr, 9);
            Assert.Equal(eVorher.BetriebskostenJahr.Value + hilfsKosten, e.BetriebskostenJahr.Value, 4);
        }
    }
}
