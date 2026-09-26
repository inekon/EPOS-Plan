using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E30/2 (#545, Befund B4, Entscheide E30‑Q1 a bis Q4 a) — <b>die
    /// Hilfsenergiekosten aus dem Anteil an der Anlage</b>
    /// (<see cref="HilfsenergieAusAnteil"/>), an einer Arbeitskopie des BHKW-Referenzprojekts
    /// 1030 (zwei BHKW 14920/14921, Gaskessel 11334, Strom 0,25 €/kWh).
    ///
    /// <para>Der Rechenweg: <c>Brennstoff der Anlage × Arbeitspreis des Projekt-Stromträgers
    /// × Anteil / 100</c> im Endenergie-Topf, getragen von der Hilfsenergie-Pflichtzeile der
    /// Anlage (Weg B) oder einer abgeleiteten Zeile; eine selbst gepflegte Position hat
    /// Vorrang. Ohne Anteil bleibt alles bitgleich — die Anker halten das
    /// (<see cref="WirtschaftlichkeitAnkerTests"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class HilfsenergiekostenAusAnteilTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int BHKW_GROSS = 14920;
        private const int BHKW_KLEIN = 14921;
        private const int KESSEL = 11334;
        private const int ZEILE_GROSS = 101600590;   // Hilfsenergiekosten (Pflicht) an 14920
        private const int ZEILE_KLEIN = 101600593;   // Hilfsenergiekosten (Pflicht) an 14921
        private const int ZEILE_KESSEL = 101600587;  // Hilfsenergiekosten (Strom) an 11334

        /// <summary>Barwertfaktor des Endenergie-Topfs von 1030: i = 3 %, p_E = 2 %, 20 a,
        /// Zahlung im Jahr t mit (1 + p_E)^(t−1).</summary>
        private static readonly double BWF_PE =
            Enumerable.Range(1, 20).Sum(t => Math.Pow(1.02, t - 1) / Math.Pow(1.03, t));

        private static void Anteil(int anlage, double? prozent)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Hilfsenergie_Anteil = ? WHERE ID = ?",
                new DbParam("@w", DbParamTyp.Double) { Wert = prozent.HasValue ? (object)prozent.Value : DBNull.Value },
                new DbParam("@a", DbParamTyp.Integer) { Wert = anlage });
        }

        private static double Strompreis()
        {
            double? p = EndenergieAufloeser.FuerProjekt(PROJEKT).StrompreisJeKwh;
            Assert.True(p.HasValue, "1030 braucht einen Strompreis.");
            return p.Value;
        }

        /// <summary>Brennstoff eines BHKW im gespeicherten Lauf [MWh] — dieselbe Zahl, an der
        /// auch der KWKG-Abzug bemisst.</summary>
        private static double BhkwBrennstoffMWh(int anlage)
        {
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@a", DbParamTyp.Integer) { Wert = anlage }));
            ErgebnisModel e = new ErgebnisCtrl().Load(PROJEKT);
            return e.BHKW.Module.Where(m => m.Modul == name).Sum(m => m.Verbrauch);
        }

        private static WirtschaftlichkeitErgebnis Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Hilfsenergie " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p).First(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
        }

        // =====================================================================
        //  Der Rechenweg
        // =====================================================================

        /// <summary>
        /// 2 % an beiden BHKW: Die zwei Pflichtzeilen tragen je Brennstoff × 0,25 €/kWh × 2 %
        /// im Endenergie-Topf — am gespeicherten Lauf 212 (1.048,27 MWh Brennstoff)
        /// 5.241,35 €/a; der Betriebs-Topf bleibt 20.000 €/a, der Kapitalwert sinkt um den
        /// Betrag × Barwertfaktor p_E.
        /// </summary>
        [Fact]
        public void Zwei_Prozent_an_beiden_BHKW_kosten_Brennstoff_mal_Strompreis_mal_Anteil()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis vorher = Rechne();
            Anteil(BHKW_GROSS, 2.0);
            Anteil(BHKW_KLEIN, 2.0);

            double preis = Strompreis();
            Assert.Equal(0.25, preis, 9);
            double gross = BhkwBrennstoffMWh(BHKW_GROSS) * 1000.0 * preis * 0.02;
            double klein = BhkwBrennstoffMWh(BHKW_KLEIN) * 1000.0 * preis * 0.02;
            // Dieselbe Menge wie der KWKG-Abzug (HilfsstromRechner) — bewertet, nicht neu gerechnet.
            Assert.Equal(HilfsstromRechner.MengeMWh(2.0, BhkwBrennstoffMWh(BHKW_GROSS)) * 250.0, gross, 6);
            Assert.Equal(5241.35, gross + klein, 2);   // (862,18 + 186,09) MWh × 2 % × 0,25 €/kWh

            WirtschaftlichkeitCtrl.BetriebsTopfe t =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Null(t.Fehler);
            Assert.Equal(20000.0, t.BetriebSofort, 9);
            Assert.Equal(gross + klein, t.EndenergieSofort, 6);

            Dictionary<int, KostenPositionNachweis> n =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(gross, n[ZEILE_GROSS].BetragJahr, 6);
            Assert.Equal(klein, n[ZEILE_KLEIN].BetragJahr, 6);
            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, n[ZEILE_GROSS].Bemessung);
            Assert.Equal(2.0, n[ZEILE_GROSS].Einheitpreis);
            Assert.Equal(HilfsenergieAusAnteil.HERKUNFT_ANLAGENANTEIL, n[ZEILE_GROSS].SatzHerkunft);
            Assert.Equal(0.0, n[ZEILE_KESSEL].BetragJahr, 9);   // Kessel ohne Anteil

            // Die Nachweisliste trifft die Summe (E7-Probe).
            List<KostenPositionNachweis> alle =
                WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(t.BetriebSofort + t.EndenergieSofort, alle.Sum(x => x.BetragJahr), 6);

            // Die Herleitung nennt die Herkunft des Satzes.
            string herleitung = WirtschaftlichkeitZeilen.Herleitung(n[ZEILE_GROSS], BerichtTexte.Kultur);
            Assert.Contains("Satz aus dem Hilfsenergieanteil der Anlage", herleitung);

            // Kapitalwert: der Betrag im Endenergie-Topf, über 20 Jahre mit p_E — dazu, wie
            // bisher, der kleinere KWK-Zuschlag (der Anteil mindert die Nettomenge).
            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.Equal(20000.0, e.BetriebskostenJahr.Value - (gross + klein), 6);
            Assert.True(e.KwkgErloesJahr1 < vorher.KwkgErloesJahr1, "Der KWKG-Abzug fehlt.");
            double dEinnahmen = e.BarwertEinnahmen.Value - vorher.BarwertEinnahmen.Value;
            Assert.Equal(-(gross + klein) * BWF_PE + dEinnahmen,
                         e.Kapitalwert.Value - vorher.Kapitalwert.Value, 2);
        }

        /// <summary>
        /// Derselbe Anteil am FRISCHEN Lauf des Berichtswegs (BHKW-Brennstoff rund 1.241,5 MWh
        /// statt 1.048,27 im Altlauf 212): 2 % × 0,25 €/kWh ≈ 6.207,75 €/a — die Zahl des
        /// Phase-0-Befundes. Gemessen wird am gespeicherten Brennstoff des neuen Laufs.
        /// </summary>
        [Fact]
        public void Am_frischen_Lauf_kosten_zwei_Prozent_rund_6208_Euro()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anteil(BHKW_GROSS, 2.0);
            Anteil(BHKW_KLEIN, 2.0);
            BerichtsDatenSammler.VariantenStatus stamm =
                BerichtsDatenSammler.ErmittleStatus(PROJEKT, "").First(x => x.IstStamm);
            new BerichtsDatenSammler().Sammle(PROJEKT, stamm.Projektname, new List<int>(), true, true,
                                              null, CancellationToken.None);

            double brennstoff = BhkwBrennstoffMWh(BHKW_GROSS) + BhkwBrennstoffMWh(BHKW_KLEIN);
            Assert.True(brennstoff > 1241 && brennstoff < 1242, "Brennstoff " + brennstoff);
            WirtschaftlichkeitCtrl.BetriebsTopfe t =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(brennstoff * 1000.0 * 0.25 * 0.02, t.EndenergieSofort, 6);
            Assert.True(Math.Abs(t.EndenergieSofort - 6207.75) < 0.1, "Hilfsenergie " + t.EndenergieSofort);
        }

        /// <summary>
        /// Der Gaskessel: Sein Anteil bemisst am Kesselbrennstoff des Laufs (5.403,1 MWh über den
        /// Nutzungsgrad) — 0,5 % × 0,25 €/kWh = 6.753,88 €/a an der Pflichtzeile
        /// „Hilfsenergiekosten (Strom)".
        /// </summary>
        [Fact]
        public void Der_Gaskessel_traegt_seinen_Anteil_an_der_Stromzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anteil(KESSEL, 0.5);
            Dictionary<int, KostenPositionNachweis> n =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(5403.1 * 1000.0 * 0.25 * 0.005, n[ZEILE_KESSEL].BetragJahr, 6);
            Assert.Equal(0.0, n[ZEILE_GROSS].BetragJahr, 9);
        }

        // =====================================================================
        //  Vorrang und Doppelpflege (E30‑Q2 a)
        // =====================================================================

        /// <summary>
        /// Trägt die Position an 14920 selbst einen Satz (3 %), gilt die Position — der Anteil
        /// dieser Anlage wird nicht zusätzlich bepreist; 14921 rechnet weiter aus dem Anteil.
        /// Die Kohärenzprüfung meldet die Doppelpflege und sagt, was gilt.
        /// </summary>
        [Fact]
        public void Eine_gepflegte_Position_hat_Vorrang_vor_dem_Anteil()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anteil(BHKW_GROSS, 2.0);
            Anteil(BHKW_KLEIN, 2.0);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET Einheitpreis = 3 WHERE ID = ?",
                new DbParam("@id", DbParamTyp.Integer) { Wert = ZEILE_GROSS });

            double preis = Strompreis();
            Dictionary<int, KostenPositionNachweis> n =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(BhkwBrennstoffMWh(BHKW_GROSS) * 1000.0 * preis * 0.03, n[ZEILE_GROSS].BetragJahr, 6);
            Assert.Equal(3.0, n[ZEILE_GROSS].Einheitpreis);
            Assert.Null(n[ZEILE_GROSS].SatzHerkunft);
            Assert.Equal(BhkwBrennstoffMWh(BHKW_KLEIN) * 1000.0 * preis * 0.02, n[ZEILE_KLEIN].BetragJahr, 6);

            WirtschaftlichkeitCtrl.BetriebsTopfe t =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(n[ZEILE_GROSS].BetragJahr + n[ZEILE_KLEIN].BetragJahr, t.EndenergieSofort, 6);

            string hinweis = KohaerenzPruefung.HilfsenergieDoppelpflege(PROJEKT, BHKW_GROSS);
            Assert.Contains("wird nicht zusätzlich bepreist", hinweis);
            Assert.Equal("", KohaerenzPruefung.HilfsenergieDoppelpflege(PROJEKT, BHKW_KLEIN));
        }

        /// <summary>
        /// Führt eine Anlage mit Anteil keine Hilfsenergie-Kostenposition, entsteht eine
        /// abgeleitete Zeile (ohne Zeilen-ID) — mit demselben Betrag, im Endenergie-Topf.
        /// </summary>
        [Fact]
        public void Ohne_Kostenposition_entsteht_eine_abgeleitete_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anteil(BHKW_KLEIN, 2.0);
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_ProjektWerte WHERE ID = ?",
                new DbParam("@id", DbParamTyp.Integer) { Wert = ZEILE_KLEIN });

            double erwartet = BhkwBrennstoffMWh(BHKW_KLEIN) * 1000.0 * Strompreis() * 0.02;
            List<KostenPositionNachweis> alle =
                WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            KostenPositionNachweis z = Assert.Single(alle, x => x.Id == 0);
            Assert.Equal("Hilfsenergiekosten (aus dem Anlagenanteil)", z.Bezeichnung);
            Assert.Equal(BHKW_KLEIN, z.Anlage);
            Assert.Equal(BetriebskostenCtrl.KOMPONENTE_BHKW, z.Komponente);
            Assert.Equal(erwartet, z.BetragJahr, 6);

            WirtschaftlichkeitCtrl.BetriebsTopfe t =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(erwartet, t.EndenergieSofort, 6);
            Assert.Equal(t.BetriebSofort + t.EndenergieSofort, alle.Sum(x => x.BetragJahr), 6);
        }

        // =====================================================================
        //  Grenzen (E30‑Q3 a) und Bitgleichheit
        // =====================================================================

        /// <summary>Anteil 0 oder leer löst nichts aus — die Töpfe sind bitgleich mit dem
        /// Stand ohne Anteil.</summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(null)]
        public void Ohne_Anteil_bleibt_alles_bitgleich(double? anteil)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitCtrl.BetriebsTopfe vorher =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Anteil(BHKW_GROSS, anteil);
            Anteil(KESSEL, anteil);
            WirtschaftlichkeitCtrl.BetriebsTopfe nachher =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.True(HilfsenergieAusAnteil.Plane(PROJEKT).Leer);
            Assert.Equal(vorher.BetriebSofort, nachher.BetriebSofort);
            Assert.Equal(vorher.EndenergieSofort, nachher.EndenergieSofort);
        }

        /// <summary>Ein Elektrokessel bekommt keine Hilfsenergiekosten aus dem Anteil — seine
        /// Endenergie ist Netzstrom und steht schon in den Energiekosten (1024, „eloBLOCK").</summary>
        [Fact]
        public void Der_Elektrokessel_ist_ausgenommen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int ELEKTROKESSEL_1024 = 11255;
            Assert.True(WirtschaftlichkeitCtrl.IstElektrokesselAnlage(ELEKTROKESSEL_1024));
            Anteil(ELEKTROKESSEL_1024, 5.0);
            Assert.True(HilfsenergieAusAnteil.Plane(1024).Leer);
        }
    }
}
