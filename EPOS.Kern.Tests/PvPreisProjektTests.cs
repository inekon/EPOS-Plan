using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Prüfprojekt 1048 „Prüfprojekt PV mit Preisen" — die PV-Erlösseite der
    /// Wirtschaftlichkeit mit einem vollständigen Preissatz.</b>
    ///
    /// <para>Bis dahin trug kein Projekt der Testdatenbank eine PV-Anlage MIT Preisen: Die
    /// PV-Projekte hatten keinen Stromträger oder einen Brennstoff ohne Preis, rechneten also
    /// keine Energiekosten und keinen Kapitalwert. 1048 ist die Kopie von 1040 (Gebäude nach
    /// VDI 6007, 40 Module = 10,40 kWp) mit Strom und Erdgas samt Günstig/Ungünstig, der
    /// flachen Einspeisevergütung PV 0,08 €/kWh (0,10 / 0,06), der PV-Investition je kWp und
    /// den PV-Betriebskosten. Angelegt von
    /// <c>Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs</c>; der Kopfkommentar dort
    /// nennt jeden Wert.</para>
    ///
    /// <para><b>Kein Referenzprojekt:</b> 1048 steht in keiner Basis und in keiner
    /// Projektliste der CI; für 1048 gilt keine Einfrierregel. Gerechnet wird frisch
    /// (Simulation mit Zeitreihen, wie Bericht und Seite), deshalb führt die Testdatenbank
    /// für 1048 keine Ergebniszeilen.</para>
    ///
    /// <para><b>Was hier steht:</b> Aufbau und Preissatz, der Einspeiseerlös PV je Szenario
    /// (Menge × Satz × Ertragsfaktor), Szenario C (Trägerpreise) mit der Zusage „Erwartet
    /// bleibt zahlengleich", Szenario D mit DV-Entgelt und PPA-Preis des Vergütungsdialogs
    /// (auf einer Arbeitskopie eingeschaltet), die Investitions- und Betriebskosten der
    /// PV-Positionen, der Ausweis „n von m Parametern szenariert", die Formelmappe
    /// (Parameterblock und Nachrechnung in ClosedXML) und der Kapitalwert als Anker
    /// (relativ 1e‑6).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvPreisProjektTests : IClassFixture<PvPreisProjektTests.Vorrichtung>
    {
        internal const int PROJEKT = 1048;
        internal const string NAME = "Prüfprojekt PV mit Preisen";
        private const int VORLAGE = 1040;
        private const int TRAEGER_STROM = 60;
        private const int TRAEGER_ERDGAS = 63;
        private const double KWP = 10.40;

        private const string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;
        private const string BEST = WirtschaftlichkeitSzenario.BEST;
        private const string WORST = WirtschaftlichkeitSzenario.WORST;

        // Die Kapitalwerte des Projekts (frisch simuliert, 25.09.2026). Sie bewegen sich mit
        // jeder Änderung am Rechenweg, die 1048 trifft — dann nachziehen, wie die Anker
        // von 1030. Die Beziehungen der übrigen Fälle bleiben davon unberührt.
        private const double KW_ERWARTET = -237134.7270351314;
        private const double KW_BEST = -204001.50754334457;
        private const double KW_WORST = -279852.53447363194;

        /// <summary>Die kopierten Investitionszeilen der Vorlage 1040 (20 Zeilen, Erwartet).</summary>
        private const double INVEST_KOPIE = 54975.5;

        private readonly Vorrichtung _v;

        public PvPreisProjektTests(Vorrichtung v) { _v = v; }

        // =====================================================================
        //  Die Vorrichtung: EINE Arbeitskopie und EIN Lauf je Klasse
        // =====================================================================

        /// <summary>
        /// Die Arbeitskopie der Testdatenbank und der Lauf des Projekts: Simulation mit
        /// Zeitreihen über den <see cref="BerichtsDatenSammler"/>, die drei Szenarien über
        /// <see cref="WirtschaftlichkeitCtrl.Berechne(BerichtsDaten, WirtschaftlichkeitParameter, int, bool)"/>.
        /// Die Zählungen vor dem Lauf stehen fest, weil der Sammler die Ergebnisse speichert.
        /// </summary>
        public sealed class Vorrichtung : IDisposable
        {
            public readonly TestDatenbank Db;
            public readonly bool Vorhanden;
            public readonly long ErgebniszeilenVorDemLauf;
            public readonly BerichtsDaten Daten;
            public readonly VariantenDaten Stand;
            public readonly StromMatrix Matrix;
            public readonly WirtschaftlichkeitParameter Parameter;
            public readonly Dictionary<string, WirtschaftlichkeitErgebnis> Ergebnis;

            public Vorrichtung()
            {
                Db = Neue();
                if (!Db.Vorhanden) return;
                using var kultur = new Kulturvorrichtung();

                ErgebniszeilenVorDemLauf =
                    Zahl("SELECT COUNT(*) FROM Tab_Ergebnis WHERE ID_Projekt = ?", PROJEKT) +
                    Zahl("SELECT COUNT(*) FROM Tab_ErgebnisWirtschaftlichkeit WHERE ID_Projekt = ?", PROJEKT);
                Ergebnis = Lauf(out Daten, out Parameter);
                Stand = Daten.Varianten.First(x => x.IstStamm);
                Matrix = StromMatrix.Baue(Stand.Zeitreihen, new TarifParameter());
                Vorhanden = true;
            }

            public void Dispose() => Db.Dispose();
        }

        /// <summary>Eine frische Arbeitskopie der Testdatenbank.</summary>
        internal static TestDatenbank Neue() => new TestDatenbank();

        /// <summary>Der Lauf des Projekts auf der eingelegten Datenbank.</summary>
        private static Dictionary<string, WirtschaftlichkeitErgebnis> Lauf(
            out BerichtsDaten daten, out WirtschaftlichkeitParameter p)
        {
            daten = new BerichtsDatenSammler().Sammle(PROJEKT, NAME, new List<int>(), true, true, null,
                                                      CancellationToken.None);
            var ctrl = new WirtschaftlichkeitCtrl();
            p = ctrl.LadeParameter(PROJEKT);
            daten.Wirtschaftlichkeit = ctrl.Berechne(daten, p, 0, false, out List<SensitivitaetZeile> sens);
            daten.Bewertung = WirtschaftlichkeitBewertung.FuerBericht(daten, daten.Wirtschaftlichkeit, p,
                                                                      BerichtTexte.Kultur, sens);
            var je = new Dictionary<string, WirtschaftlichkeitErgebnis>();
            foreach (WirtschaftlichkeitErgebnis e in daten.Wirtschaftlichkeit)
                if (e.IdProjekt == PROJEKT) je[e.Szenario] = e;
            return je;
        }

        private static Dictionary<string, WirtschaftlichkeitErgebnis> Lauf() => Lauf(out _, out _);

        // =====================================================================
        //  Aufbau und Preissatz
        // =====================================================================

        /// <summary>
        /// Das Projekt steht mit seinem Aufbau: Kopie von 1040 mit einem Gebäude nach VDI 6007
        /// (der Tagesbilanz-Weg bleibt allein bei 1040), ohne Kühlbetrieb, EINE PV-Anlage mit
        /// 10,40 kWp, keine Vergütungszeile (flache Einspeisevergütung), keine Tarifstruktur
        /// (Flat) und keine gespeicherten Ergebnisse.
        /// </summary>
        [Fact]
        public void Das_Projekt_steht_als_Kopie_von_1040_mit_10_40_kWp()
        {
            if (!_v.Vorhanden) return;

            Assert.Equal(NAME, Text("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Gebaeude_Modell IS NULL", PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ,
                         Text("SELECT Gebaeude_Modell FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE));
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(PROJEKT));

            Assert.Equal(KWP, PhotovoltaikCtrl.KwpDesProjekts(PROJEKT), 9);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_PV > 0", PROJEKT));
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", VORLAGE),
                         Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", PROJEKT));

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ProjektPhotovoltaik WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ProjektTarif WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0L, _v.ErgebniszeilenVorDemLauf);
        }

        /// <summary>
        /// Kein Referenzprojekt: 1048 steht in keiner Projektliste der CI, in keinem
        /// Basisordner und in keiner Liste der aktuellen Basis in <c>Referenzlaeufe/LIESMICH.md</c>.
        /// </summary>
        [Fact]
        public void Das_Projekt_hat_keine_Referenzrolle()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            foreach (string wf in new[] { "kern.yml", "ios.yml", "windows.yml" })
            {
                string pfad = Path.Combine(wurzel, ".github", "workflows", wf);
                if (!File.Exists(pfad)) continue;
                foreach (string zeile in File.ReadAllLines(pfad).Where(z => z.Contains("--projekte")))
                    Assert.DoesNotContain(PROJEKT.ToString(CultureInfo.InvariantCulture), zeile);
            }

            string basen = Path.Combine(wurzel, "Referenzlaeufe");
            foreach (string basis in Directory.GetDirectories(basen))
                Assert.False(Directory.Exists(Path.Combine(basis, "Projekt_" + PROJEKT)),
                             "Die Basis " + Path.GetFileName(basis) + " führt das Prüfprojekt " + PROJEKT + ".");

            string liesmich = File.ReadAllText(Path.Combine(basen, "LIESMICH.md"));
            int aktuelle = liesmich.IndexOf("## Aktuelle Basis", StringComparison.Ordinal);
            Assert.True(aktuelle >= 0);
            string absatz = liesmich.Substring(aktuelle, Math.Min(800, liesmich.Length - aktuelle));
            Assert.DoesNotContain(PROJEKT.ToString(CultureInfo.InvariantCulture), absatz);
        }

        /// <summary>
        /// Der Preissatz: Strom 0,30 €/kWh (0,26 / 0,36) und 120 €/a (100 / 150), Erdgas
        /// 0,80 €/Nm³ (0,70 / 0,95) und 150 €/a, der Parametersatz mit der Einspeisevergütung
        /// PV 0,08 €/kWh (0,10 / 0,06) und die drei PV-Kostenpositionen.
        /// </summary>
        [Fact]
        public void Der_Preissatz_ist_vollstaendig()
        {
            if (!_v.Vorhanden) return;

            TraegerpreisSzenario strom = EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT, TRAEGER_STROM);
            Assert.Equal(0.26, strom.ArbeitspreisBest);
            Assert.Equal(0.36, strom.ArbeitspreisWorst);
            Assert.Equal(100.0, strom.GrundpreisBest);
            Assert.Equal(150.0, strom.GrundpreisWorst);
            TraegerpreisSzenario gas = EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT, TRAEGER_ERDGAS);
            Assert.Equal(0.70, gas.ArbeitspreisBest);
            Assert.Equal(0.95, gas.ArbeitspreisWorst);

            KostenEmissionRechner.PreisSatz(PROJEKT, TRAEGER_STROM, null, out double? arbeit, out double? grund, out _);
            Assert.Equal(0.30, arbeit);
            Assert.Equal(120.0, grund);
            KostenEmissionRechner.PreisSatz(PROJEKT, TRAEGER_ERDGAS, null, out arbeit, out grund, out _);
            Assert.Equal(0.80, arbeit);
            Assert.Equal(150.0, grund);

            WirtschaftlichkeitParameter p = _v.Parameter;
            Assert.Equal(3.0, p.Zinssatz);
            Assert.Equal(20, p.Betrachtungszeitraum);
            Assert.Equal(2.0, p.PreissteigerungEnergie);
            Assert.Equal(1.5, p.PreissteigerungBetrieb);
            Assert.Equal(0.08, p.Einspeiseverguetung);
            Assert.Equal(0.10, p.SatzBest.Einspeiseverguetung);
            Assert.Equal(0.06, p.SatzWorst.Einspeiseverguetung);

            DataTable pv = DataRepository.GetDataTable(
                "SELECT StammID, KategorieID, Bemessung, Einheitpreis, EingegebenerWert FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KomponentenID = 3 ORDER BY KategorieID, StammID", new DbParam("@p", PROJEKT));
            Assert.Equal(3, pv.Rows.Count);
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWP, Convert.ToString(pv.Rows[0]["Bemessung"]));
            Assert.Equal(1200.0, Convert.ToDouble(pv.Rows[0]["Einheitpreis"]));
            Assert.Equal(DbWerte.BEMESSUNG_JAHRESBETRAG, Convert.ToString(pv.Rows[1]["Bemessung"]));
            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_INVESTITION, Convert.ToString(pv.Rows[2]["Bemessung"]));
        }

        // =====================================================================
        //  Erlöse, Kosten, Kapitalwert
        // =====================================================================

        /// <summary>
        /// Der Lauf rechnet: Energiekosten und Kapitalwert in allen drei Szenarien, ohne
        /// Fehlgrund, und in der Reihenfolge KW_Günstig &gt; KW_Erwartet &gt; KW_Ungünstig.
        /// Die drei Kapitalwerte sind der Zahlenanker des Projekts (relativ 1e‑6).
        /// </summary>
        [Fact]
        public void Der_Kapitalwert_steht_in_allen_drei_Szenarien()
        {
            if (!_v.Vorhanden) return;

            Assert.True(_v.Stand.Energiekosten.HasValue, "Energiekosten: " + _v.Stand.EnergiekostenGrund);
            foreach (string s in new[] { ERWARTET, BEST, WORST })
            {
                Assert.Null(_v.Ergebnis[s].Fehlgrund);
                Assert.True(_v.Ergebnis[s].Kapitalwert.HasValue, s + ": kein Kapitalwert");
            }
            double kwE = _v.Ergebnis[ERWARTET].Kapitalwert.Value;
            double kwB = _v.Ergebnis[BEST].Kapitalwert.Value;
            double kwW = _v.Ergebnis[WORST].Kapitalwert.Value;
            Assert.True(kwB > kwE && kwE > kwW, "Reihenfolge: " + kwB + " / " + kwE + " / " + kwW);

            Relativ(KW_ERWARTET, kwE);
            Relativ(KW_BEST, kwB);
            Relativ(KW_WORST, kwW);
        }

        /// <summary>
        /// Der Einspeiseerlös PV ist die PV-Überschussmenge × Einspeisevergütung des Szenarios ×
        /// Ertragsfaktor (Vorgabe +10 % / −10 %): 7,06 MWh × 0,08 = 564,80 €/a, im Günstigen
        /// × 0,10 × 1,1, im Ungünstigen × 0,06 × 0,9. Ohne BHKW ist er der ganze Einspeiseerlös.
        /// </summary>
        [Fact]
        public void Der_Einspeiseerloes_PV_ist_Menge_mal_Satz_je_Szenario()
        {
            if (!_v.Vorhanden) return;

            double kwh = _v.Stand.Ergebnis.Photovoltaik.Ueberschuss * 1000.0;
            Assert.True(kwh > 5000.0, "PV-Überschuss " + kwh + " kWh");
            Assert.Equal(kwh * 0.08, _v.Ergebnis[ERWARTET].EinspeiseerloesPvJahr, 6);
            Assert.Equal(kwh * 0.10 * 1.1, _v.Ergebnis[BEST].EinspeiseerloesPvJahr, 6);
            Assert.Equal(kwh * 0.06 * 0.9, _v.Ergebnis[WORST].EinspeiseerloesPvJahr, 6);
            foreach (WirtschaftlichkeitErgebnis e in _v.Ergebnis.Values)
            {
                Assert.Equal(e.EinspeiseerloesPvJahr, e.EinspeiseerloesJahr, 9);
                Assert.Equal(0.0, e.EinspeiseerloesKwkJahr, 9);
            }

            // Die Menge ist die Einspeisung der Strommatrix (ohne Speicher dieselbe Reihe).
            Assert.Equal(_v.Matrix.EinspeisungPvGesamtMWh, _v.Stand.Ergebnis.Photovoltaik.Ueberschuss, 1);
        }

        /// <summary>
        /// Die Investition: die 20 kopierten Zeilen von 1040 und die PV-Position je kWp —
        /// 10,40 kWp × 1.200 €/kWp = 12.480 €. Im Günstigen gilt für die kopierten Zeilen die
        /// Vorgabe −10 %, für die PV-Position ihr gepflegter Wert 10.400 €; im Ungünstigen +10 %
        /// und 14.560 €.
        /// </summary>
        [Fact]
        public void Die_Investition_bemisst_die_PV_je_kWp()
        {
            if (!_v.Vorhanden) return;

            Assert.Equal(INVEST_KOPIE + KWP * 1200.0, _v.Ergebnis[ERWARTET].Investition, 6);
            Assert.Equal(INVEST_KOPIE * 0.9 + 10400.0, _v.Ergebnis[BEST].Investition, 6);
            Assert.Equal(INVEST_KOPIE * 1.1 + 14560.0, _v.Ergebnis[WORST].Investition, 6);
        }

        /// <summary>
        /// Die Betriebskosten der PV: Wartung 150 €/a (120 / 180) und Instandhaltung 1 % der
        /// PV-Investition des Szenarios (124,80 / 104,00 / 145,60 €/a). Weitere
        /// Betriebspositionen führt das Projekt nicht.
        /// </summary>
        [Fact]
        public void Die_Betriebskosten_folgen_Wartung_und_Instandhaltung()
        {
            if (!_v.Vorhanden) return;

            Assert.Equal(150.0 + 0.01 * 12480.0, _v.Ergebnis[ERWARTET].BetriebskostenJahr.Value, 6);
            Assert.Equal(120.0 + 0.01 * 10400.0, _v.Ergebnis[BEST].BetriebskostenJahr.Value, 6);
            Assert.Equal(180.0 + 0.01 * 14560.0, _v.Ergebnis[WORST].BetriebskostenJahr.Value, 6);
        }

        // =====================================================================
        //  Eigenverbrauch und vermiedene Kosten
        // =====================================================================

        /// <summary>
        /// Die PV-Strommengen passen zusammen: Die Stromproduktion ist die Erzeugung der
        /// Module (13,43 MWh), Erzeugung − Überschuss ist der Eigenverbrauch, und er ist
        /// derselbe wie in der Strommatrix, deren Bedarf alle Verbraucher trägt (Haushalt,
        /// Wärmepumpe, Kessel-Hilfsstrom) und deshalb nicht unter dem Netzbezug liegt.
        /// </summary>
        [Fact]
        public void Erzeugung_Eigenverbrauch_und_Einspeisung_passen_zusammen()
        {
            if (!_v.Vorhanden) return;

            ErgebnisPhotovoltaikModel pv = _v.Stand.Ergebnis.Photovoltaik;
            double module = pv.Module.Sum(m => m.Stromproduktion);
            Assert.Equal(module, pv.Stromproduktion, 2);
            Assert.True(pv.Stromproduktion > pv.Ueberschuss, "Erzeugung unter dem Überschuss");
            Assert.Equal(pv.Stromproduktion - pv.Ueberschuss, _v.Matrix.PvEigenGesamtMWh, 1);
            Assert.True(_v.Matrix.BedarfGesamtMWh >= _v.Matrix.BezugGesamtMWh,
                        "Bedarf " + _v.Matrix.BedarfGesamtMWh + " unter dem Netzbezug " + _v.Matrix.BezugGesamtMWh);
            Assert.Equal(_v.Matrix.BedarfGesamtMWh - _v.Matrix.PvEigenGesamtMWh, _v.Matrix.BezugGesamtMWh, 6);
        }

        /// <summary>
        /// Die vermiedenen Kosten im Rollentarif (auf der Arbeitskopie eingeschaltet, die
        /// Testdatenbank führt Flat): Die vermiedene Menge ist der PV-Eigenverbrauch der
        /// Strommatrix, sie geht ganz an die Photovoltaik, und der Arbeitsanteil ist Menge ×
        /// Bezugspreis 0,30 €/kWh — positiv.
        /// </summary>
        [Fact]
        public void Die_vermiedenen_Kosten_sind_der_PV_Eigenverbrauch_zum_Bezugspreis()
        {
            if (!_v.Vorhanden) return;
            using var db = Neue();
            using var kultur = new Kulturvorrichtung();

            var ctrl = new WirtschaftlichkeitCtrl();
            var tarif = new TarifParameter { IdStamm = PROJEKT, Aktiv = true, Modus = DbWerte.TARIF_MODUS_ROLLEN };
            tarif.Bezug.ArbeitspreisEurKWh = 0.30;
            tarif.Reststrom.ArbeitspreisEurKWh = 0.30;
            tarif.Einspeisung.ArbeitspreisEurKWh = 0.08;
            Assert.True(ctrl.SpeichereTarif(tarif));
            WirtschaftlichkeitErgebnis e = Lauf()[ERWARTET];

            double menge = _v.Matrix.PvEigenGesamtMWh;
            Assert.True(menge > 5.0, "PV-Eigenverbrauch " + menge + " MWh");
            Assert.Equal(menge, e.VermiedenMengeMWh, 6);
            VermiedenAnlageNachweis pv = Assert.Single(e.VermiedenJeAnlage);
            Assert.Equal(WirtZeile.KOMPONENTE_PV, pv.Komponente);
            Assert.Equal(1.0, pv.Anteil, 9);
            Assert.Equal(menge * 1000.0 * 0.30, pv.ArbeitEur, 6);
            Assert.Equal(menge * 1000.0 * 0.30, e.VermiedenArbeitJahr, 6);
        }

        // =====================================================================
        //  Szenarien C und D
        // =====================================================================

        /// <summary>
        /// Szenario C: Die gepflegten Trägerpreise ordnen die Energiekosten — Günstig &lt;
        /// Erwartet &lt; Ungünstig —, und Erwartet sind die Energiekosten des Laufs. Der Ausweis
        /// nennt vier gepflegte Parameter: Einspeisevergütung PV, Arbeits- und Grundpreis des
        /// Stroms, Arbeitspreis des Erdgases.
        /// </summary>
        [Fact]
        public void Szenario_C_ordnet_die_Energiekosten_und_der_Ausweis_zaehlt_vier()
        {
            if (!_v.Vorhanden) return;

            double e = _v.Ergebnis[ERWARTET].EnergiekostenJahr.Value;
            Assert.Equal(_v.Stand.Energiekosten.Value, e, 6);
            Assert.True(_v.Ergebnis[BEST].EnergiekostenJahr.Value < e);
            Assert.True(_v.Ergebnis[WORST].EnergiekostenJahr.Value > e);

            SzenarioAbdeckung a = SzenarioAbdeckung.Lesen(_v.Parameter,
                new[] { new KeyValuePair<int, string>(PROJEKT, "") });
            Assert.Equal(4, a.Szenariert);
            Assert.True(a.Parameter > a.Szenariert);
            Assert.Contains(a.Gepflegte, g => g.Contains("Einspeisevergütung PV"));
            Assert.Contains(a.Gepflegte, g => g.Contains("Arbeitspreis Elektrische Energie"));
            Assert.Contains(a.Gepflegte, g => g.Contains("Grundpreis Elektrische Energie"));
            Assert.Contains(a.Gepflegte, g => g.Contains("Arbeitspreis Erdgas E"));
        }

        /// <summary>
        /// Die Zusage des Entscheids: <b>Erwartet bleibt zahlengleich</b>. Ohne die
        /// Szenariopreise der Träger und der Einspeisevergütung rechnet Erwartet bitgleich;
        /// Günstig und Ungünstig bewegen sich.
        /// </summary>
        [Fact]
        public void Ohne_Szenariopreise_bleibt_Erwartet_bitgleich()
        {
            if (!_v.Vorhanden) return;
            using var db = Neue();
            using var kultur = new Kulturvorrichtung();

            DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET custom_price_work_best = NULL, custom_price_work_worst = NULL, " +
                "custom_price_base_best = NULL, custom_price_base_worst = NULL WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Einspeiseverguetung_Best = NULL, " +
                "Einspeiseverguetung_Worst = NULL WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            Dictionary<string, WirtschaftlichkeitErgebnis> ohne = Lauf();

            Assert.Equal(_v.Ergebnis[ERWARTET].Kapitalwert.Value, ohne[ERWARTET].Kapitalwert.Value);
            Assert.Equal(_v.Ergebnis[ERWARTET].EnergiekostenJahr.Value, ohne[ERWARTET].EnergiekostenJahr.Value);
            Assert.NotEqual(_v.Ergebnis[BEST].Kapitalwert.Value, ohne[BEST].Kapitalwert.Value);
            Assert.NotEqual(_v.Ergebnis[WORST].Kapitalwert.Value, ohne[WORST].Kapitalwert.Value);
        }

        /// <summary>
        /// Szenario D mit dem Vergütungsdialog (auf der Arbeitskopie eingeschaltet, die
        /// Testdatenbank führt ihn nicht): Marktprämie mit DV-Entgelt 0,40 ct/kWh (0,20 /
        /// 0,60), danach sonstige Direktvermarktung mit PPA-Preis 7,0 ct/kWh (8,0 / 5,5). Der
        /// gepflegte Erlössatz verschiebt den PV-Erlös des Szenarios, Erwartet bleibt, und die
        /// flache Einspeisevergütung neben dem aktiven Dialog nennt eine Kohärenzzeile.
        /// </summary>
        [Fact]
        public void Szenario_D_DV_Entgelt_und_PPA_Preis_im_Verguetungsdialog()
        {
            if (!_v.Vorhanden) return;
            using var db = Neue();
            using var kultur = new Kulturvorrichtung();

            var pvc = new ProjektPhotovoltaikCtrl();
            ProjektPhotovoltaikModel m = pvc.LiesOderVorbelegt(PROJEKT);
            m.Aktiv = true;
            m.Vermarktungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE;
            m.Inbetriebnahme = new DateTime(2026, 8, 1);
            m.DvEntgelt = 0.40;
            m.MarktwertJahresmittel = 4.50;
            m.Degradation = 0.0;
            Assert.True(pvc.Speichern(m));
            Dictionary<string, WirtschaftlichkeitErgebnis> dv = Lauf();

            m = pvc.Lies(PROJEKT);
            m.DvEntgeltBest = 0.20;
            m.DvEntgeltWorst = 0.60;
            Assert.True(pvc.Speichern(m));
            Dictionary<string, WirtschaftlichkeitErgebnis> dvGepflegt = Lauf();

            Assert.Equal(dv[ERWARTET].Kapitalwert.Value, dvGepflegt[ERWARTET].Kapitalwert.Value);
            Assert.True(dvGepflegt[BEST].EinspeiseerloesPvJahr > dv[BEST].EinspeiseerloesPvJahr);
            Assert.True(dvGepflegt[WORST].EinspeiseerloesPvJahr < dv[WORST].EinspeiseerloesPvJahr);
            Assert.Contains(dvGepflegt[BEST].KohaerenzHinweise, h => h.Text == R.WIRT_SZ_PV_DIALOG_EINSPEISUNG);

            // Mit dem Dialog steht der Ausweis „PV: vermiedener Bezug": Eigenverbrauch
            // (Erzeugung − Überschuss) × Arbeitspreis Strom des Szenarios (0,30 / 0,26 / 0,36).
            double eigenKwh = (_v.Stand.Ergebnis.Photovoltaik.Stromproduktion -
                               _v.Stand.Ergebnis.Photovoltaik.Ueberschuss) * 1000.0;
            Assert.Equal(eigenKwh * 0.30, dvGepflegt[ERWARTET].PvVermiedenerBezug.Value, 6);
            Assert.Equal(eigenKwh * 0.26, dvGepflegt[BEST].PvVermiedenerBezug.Value, 6);
            Assert.Equal(eigenKwh * 0.36, dvGepflegt[WORST].PvVermiedenerBezug.Value, 6);

            m = pvc.Lies(PROJEKT);
            m.Vermarktungsform = DbWerte.PV_VERMARKTUNG_SONSTIGE_DV;
            m.PpaPreis = 7.0;
            m.PpaPreisBest = 8.0;
            m.PpaPreisWorst = 5.5;
            Assert.True(pvc.Speichern(m));
            Dictionary<string, WirtschaftlichkeitErgebnis> ppa = Lauf();
            Assert.True(ppa[BEST].EinspeiseerloesPvJahr > ppa[ERWARTET].EinspeiseerloesPvJahr);
            Assert.True(ppa[WORST].EinspeiseerloesPvJahr < ppa[ERWARTET].EinspeiseerloesPvJahr);
            Assert.True(ppa[ERWARTET].EinspeiseerloesPvJahr > 0.0);
        }

        // =====================================================================
        //  Formelmappe
        // =====================================================================

        /// <summary>
        /// Die Formelmappe des Projekts: Der Parameterblock nennt die Einspeisevergütung PV je
        /// Szenario (0,08 / 0,10 / 0,06) und die gepflegten Trägerpreise mit ihren wirksamen
        /// Werten; jede Formel, die ClosedXML rechnen kann, kommt auf die Zahl des Laufs.
        /// </summary>
        [Fact]
        public void Die_Formelmappe_fuehrt_Einspeiseverguetung_und_Traegerpreise_je_Szenario()
        {
            if (!_v.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            string ordner = Path.Combine(Path.GetTempPath(), "epos-e25-pv-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string ziel = Path.Combine(ordner, "pv.xlsx");
                var konfig = new BerichtsKonfiguration();
                foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                    konfig.AktiveBausteine.Add(d.Schluessel);
                new ExcelBerichtGenerator().Erzeuge(_v.Daten, konfig, ziel);

                using var wb = new XLWorkbook(ziel);
                IXLCell verguetung = Zelle(wb, R.WIRT_FM_PARAM_VERGUETUNG);
                Assert.NotNull(verguetung);
                IXLWorksheet ws = verguetung.Worksheet;
                int z = verguetung.Address.RowNumber;
                Assert.Equal(0.08, ws.Cell(z, 2).GetDouble(), 9);
                Assert.Equal(0.10, ws.Cell(z, 3).GetDouble(), 9);
                Assert.Equal(0.06, ws.Cell(z, 4).GetDouble(), 9);

                // Die Trägerpreiszeilen: je gepflegtem Preis eine Zeile mit den drei wirksamen Werten.
                List<IXLRow> preise = ws.RowsUsed(r => r.Cell(1).GetString().Contains("Elektrische Energie")).ToList();
                Assert.Contains(preise, r => Gleich(r, 0.30, 0.26, 0.36));
                Assert.Contains(preise, r => Gleich(r, 120.0, 100.0, 150.0));
                Assert.Contains(ws.RowsUsed(r => r.Cell(1).GetString().Contains("Erdgas E")), r => Gleich(r, 0.80, 0.70, 0.95));

                FormelnRechnenWieZwischengespeichert(wb, ws.Name, 20);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static bool Gleich(IXLRow r, double erwartet, double best, double worst)
        {
            return Zahlwert(r.Cell(2)) is double a && Math.Abs(a - erwartet) < 1e-9 &&
                   Zahlwert(r.Cell(3)) is double b && Math.Abs(b - best) < 1e-9 &&
                   Zahlwert(r.Cell(4)) is double c && Math.Abs(c - worst) < 1e-9;
        }

        private static double? Zahlwert(IXLCell c)
        {
            XLCellValue w = c.HasFormula ? c.CachedValue : c.Value;
            return w.IsNumber ? w.GetNumber() : (double?)null;
        }

        /// <summary>Die erste Zelle der Spalte A (irgendeines Blattes) mit genau diesem Text.</summary>
        private static IXLCell Zelle(XLWorkbook wb, string text)
        {
            foreach (IXLWorksheet ws in wb.Worksheets)
                foreach (IXLCell c in ws.Column(1).CellsUsed())
                    if (c.GetString() == text) return c;
            return null;
        }

        /// <summary>
        /// Die Gegenprobe der Formeln in ClosedXML (Muster der Blattstruktur-Wache): Jede
        /// Formelzelle trägt als zwischengespeichertes Ergebnis die Zahl des Laufs; nach
        /// <c>RecalculateAllFormulas()</c> kommt jede Formel, die ClosedXML rechnen kann, auf
        /// dieselbe Zahl. Zellen mit Fehlern (NPV, IRR und was davon abhängt) prüft Excel.
        /// </summary>
        private static void FormelnRechnenWieZwischengespeichert(XLWorkbook wb, string blatt, int mindestens)
        {
            IXLWorksheet ws = wb.Worksheet(blatt);
            var zwischen = new Dictionary<string, XLCellValue>();
            foreach (IXLCell c in ws.CellsUsed(x => x.HasFormula))
                zwischen[c.Address.ToString()] = c.CachedValue;
            Assert.True(zwischen.Count >= mindestens, "Nur " + zwischen.Count + " Formelzellen.");

            wb.RecalculateAllFormulas();
            int verglichen = 0;
            foreach (KeyValuePair<string, XLCellValue> z in zwischen)
            {
                XLCellValue neu = ws.Cell(z.Key).Value;
                if (neu.IsError) continue;
                if (z.Value.IsNumber)
                {
                    Assert.True(neu.IsNumber, z.Key + ": ClosedXML rechnet keine Zahl.");
                    Assert.True(Formelregister.Gleich(z.Value.GetNumber(), neu.GetNumber()),
                                z.Key + " (" + ws.Cell(z.Key).FormulaA1 + "): zwischengespeichert " +
                                z.Value.GetNumber().ToString("R") + ", gerechnet " + neu.GetNumber().ToString("R"));
                }
                verglichen++;
            }
            Assert.True(verglichen >= mindestens, "Nur " + verglichen + " Formeln nachgerechnet.");
        }

        private static void Relativ(double erwartet, double ist)
        {
            Assert.True(Math.Abs(ist - erwartet) <= 1e-6 * Math.Abs(erwartet),
                        "Kapitalwert " + ist.ToString("R", CultureInfo.InvariantCulture) + " statt " +
                        erwartet.ToString("R", CultureInfo.InvariantCulture));
        }

        private static long Zahl(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray());
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray());
            return o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Repowurzel (der Ordner mit <c>Referenzlaeufe</c>), <c>null</c> ohne.</summary>
        private static string Repowurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Referenzlaeufe")) &&
                    Directory.Exists(Path.Combine(d.FullName, ".github")))
                    return d.FullName;
            return null;
        }
    }
}
