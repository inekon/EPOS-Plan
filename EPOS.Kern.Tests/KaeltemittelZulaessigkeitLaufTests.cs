using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Meldungsweg der Kältemittelprüfung</b> (KM4-E2; Konzept Kältemittel als Stammdatum 3.3, 5.2, 5.3, 10) an
    /// einer Kopie der Testdatenbank, Muster <see cref="KaeltespeicherDatenbankTests"/>. Kein Referenzprojekt führt ein
    /// Inbetriebnahmejahr; die Proben setzen es an der Kopie.
    ///
    /// <para><b>Am Regelsatz nachgerechnet:</b> 1059 führt eine Kältemaschine KWS_WASSER mit 10 kW und R513A (GWP 629,5):
    /// Nr. 7 b (Kühler bis 12 kW, GWP ≥ 150, 2027) trifft ab 2027, Nr. 7 a (nur HFKW, GWP ≥ 2 500) nicht; 2026 ist
    /// zulässig. 1063 führt dieselbe Maschine mit 20 kW: Nr. 7 d (über 12 kW, GWP ≥ 750) trifft 629,5 nicht — zulässig in
    /// jedem Jahr. Die Wärmepumpe beider Projekte ist Sole-Wasser und führt <b>keinen</b> Code — sie wird nicht geprüft;
    /// die Proben geben ihr einen Code: als Luft-Wasser-Monoblock mit 35 kW und R410A trifft Nr. 8 d (über 12 bis 50 kW,
    /// GWP ≥ 150, 2027), als Sole-Wasser mit R290 ist sie „nicht prüfbar“ (Geräteklasse).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemittelZulaessigkeitLaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int P1059 = 1059;
        private const int P1063 = 1063;

        private static void Jahr(int projekt, int jahr) => DataRepository.ExecuteNonQuery(
            "UPDATE Tab_Projekt SET Inbetriebnahmejahr = ? WHERE ID = ?", new DbParam("?", jahr), new DbParam("?", projekt));

        private static int Anlage(int projekt, int typ) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT MIN(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
            new DbParam("?", projekt), new DbParam("?", typ)), CultureInfo.InvariantCulture);

        private static void WaermepumpeSetzen(int projekt, string typ, string bauart, string code)
        {
            int idWp = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_WP FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("?", Anlage(projekt, WizardItemClass.WP_TYP))),
                CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery("UPDATE Tab_WP SET Typ = ?, Bauart = ?, Kaeltemittel = ? WHERE ID = ?",
                new DbParam("?", typ), new DbParam("?", bauart), new DbParam("?", code), new DbParam("?", idWp));
        }

        private static List<Warnbefund> Befunde(int projekt) => Warnkriterien.PruefeProjekt(projekt)
            .Where(b => b.Kriterium == Warnkriterien.KAELTEMITTEL_STICHTAG).ToList();

        [Fact]
        public void Ohne_Jahr_prueft_und_meldet_die_Pruefung_nichts()
        {
            if (!_db.Vorhanden) return;
            Assert.Null(KaeltemittelProjektpruefung.Inbetriebnahmejahr(P1059));
            Assert.Empty(KaeltemittelProjektpruefung.Pruefen(P1059));
            Assert.Empty(Befunde(P1059));
            Assert.Empty(Befunde(P1063));

            // Für die Berichtsspalte: die Erzeuger mit Code, ohne Ergebnis; die Wärmepumpe ohne Code fehlt.
            KaeltemittelErzeugerpruefung km = Assert.Single(KaeltemittelProjektpruefung.Pruefen(P1059, mitCodes: true));
            Assert.Equal(KaeltemittelGeraeteart.Kaeltemaschine, km.Art);
            Assert.Equal("R513A", km.Code);
            Assert.Null(km.Ergebnis);
            Assert.Equal(Anlage(P1059, KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE), km.ID_Anlage);
        }

        [Fact]
        public void Die_Kaeltemaschine_von_1059_faellt_ab_2027_unter_Nr_7_b()
        {
            if (!_db.Vorhanden) return;
            int idKm = Anlage(P1059, KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE);

            Jahr(P1059, 2026);
            Assert.Empty(Befunde(P1059));
            Assert.Equal(KaeltemittelErgebnisklasse.Zulaessig,
                         Assert.Single(KaeltemittelProjektpruefung.Pruefen(P1059)).Ergebnis.Klasse);

            Jahr(P1059, 2027);
            KaeltemittelErzeugerpruefung p = Assert.Single(KaeltemittelProjektpruefung.Pruefen(P1059));
            Assert.Equal(KaeltemittelErgebnisklasse.AbStichtag, p.Ergebnis.Klasse);
            Assert.Equal(DbWerte.GESETZ_FGAS_IV_KUEHLER_BIS12_GWP, p.Ergebnis.Regelschluessel);
            Assert.Equal(2027, p.Ergebnis.Stichtag);
            Assert.Equal(10.0, p.Ergebnis.LeistungKw);
            Assert.Equal(629.5, p.Ergebnis.Gwp);
            Assert.Equal(DbWerte.GESETZ_STATUS_GESICHERT, p.Regel.Status);

            Warnbefund b = Assert.Single(Befunde(P1059));
            Assert.False(b.Hart);
            Assert.Equal(idKm, b.ID_Anlage);
            Assert.Contains("R513A", b.Text);
            Assert.Contains("Nr. 7 Buchst. b", b.Text);
            Assert.Contains("ab 2027", b.Text);
            Assert.Contains("F-Gas mit GWP ≥ 150", b.Text);
            Assert.Contains("Inbetriebnahmejahr 2027", b.Text);
            Assert.Contains("Nennleistung 10 kW", b.Text);
        }

        [Fact]
        public void Die_Kaeltemaschine_von_1063_bleibt_zulaessig()
        {
            if (!_db.Vorhanden) return;
            foreach (int jahr in new[] { 2027, 2035 })
            {
                Jahr(P1063, jahr);
                Assert.Empty(Befunde(P1063));
                KaeltemittelErzeugerpruefung p = Assert.Single(KaeltemittelProjektpruefung.Pruefen(P1063));
                Assert.Equal(KaeltemittelErgebnisklasse.Zulaessig, p.Ergebnis.Klasse);
                Assert.Equal(20.0, p.Ergebnis.LeistungKw);
            }
        }

        [Fact]
        public void Eine_Waermepumpe_mit_Code_wird_geprueft_auch_ohne_Kuehlbetrieb()
        {
            if (!_db.Vorhanden) return;
            int idWp = Anlage(P1063, WizardItemClass.WP_TYP);
            Jahr(P1063, 2027);
            WaermepumpeSetzen(P1063, DbWerte.WP_BAUART_LUFT_WASSER, Kaeltemittelregeln.WP_BAUART_MONOBLOCK, "r410a");

            Warnbefund b = Assert.Single(Befunde(P1063));
            Assert.Equal(idWp, b.ID_Anlage);
            Assert.Contains("R410A", b.Text);
            Assert.Contains("Nr. 8 Buchst. d", b.Text);
            Assert.Contains("Nennleistung 35 kW", b.Text);

            // Sole-Wasser ist keine Geräteklasse des Anhangs IV: nicht prüfbar, kein Befund.
            WaermepumpeSetzen(P1063, DbWerte.WP_BAUART_SOLE_WASSER, "", "R290");
            Assert.Empty(Befunde(P1063));
            KaeltemittelErzeugerpruefung wp = KaeltemittelProjektpruefung.Pruefen(P1063)
                .Single(z => z.Art == KaeltemittelGeraeteart.Waermepumpe);
            Assert.Equal(KaeltemittelErgebnisklasse.NichtPruefbar, wp.Ergebnis.Klasse);
            Assert.Equal(Kaeltemittelzulaessigkeit.FEHLT_GERAETEKLASSE, wp.Ergebnis.FehlendeGroesse);
            Assert.Contains("nicht prüfbar", Warnkriterien.KaeltemittelNichtPruefbarText(wp));
        }

        /// <summary>
        /// Der Lauf: die Warnung am Laufstart im Protokoll, der Hinweis „nicht prüfbar“ der Wärmepumpe mit Code, und im
        /// Bericht dieselbe Warnung in <c>bericht.warnungen</c> samt Code und Klasse der Berichtsspalte.
        /// </summary>
        [Fact]
        public void Der_Lauf_meldet_die_Warnung_im_Protokoll_und_im_Bericht()
        {
            if (!_db.Vorhanden) return;
            Jahr(P1059, 2027);
            WaermepumpeSetzen(P1059, DbWerte.WP_BAUART_SOLE_WASSER, "", "R290");
            string warnung = Assert.Single(Befunde(P1059)).Text;

            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(P1059, out string fehler);
            Assert.True(kopf > 0, fehler);
            Assert.Single(lauf.Protokoll.Warnungen, t => t.Contains("Nr. 7 Buchst. b", StringComparison.Ordinal));
            Assert.Contains(lauf.Protokoll.Warnungen, t => t == warnung);
            Assert.Single(lauf.Protokoll.Hinweise, t => t.Contains("nicht prüfbar", StringComparison.Ordinal) &&
                                                        t.Contains("2027", StringComparison.Ordinal));

            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(P1059, "Probe " + P1059, new List<int>(),
                                                                              true, null, CancellationToken.None);
            Assert.Contains(daten.Warnungen, t => t.EndsWith(warnung, StringComparison.Ordinal));
            VariantenDaten stamm = daten.Varianten[0];
            KaeltemittelBerichtszeile km = Assert.Single(stamm.Kaeltemittel, z => z.Art == KaeltemittelGeraeteart.Kaeltemaschine);
            Assert.Equal("R513A", km.Code);
            Assert.Equal(KaeltemittelErgebnisklasse.AbStichtag, km.Klasse);
            KaeltemittelBerichtszeile wp = Assert.Single(stamm.Kaeltemittel, z => z.Art == KaeltemittelGeraeteart.Waermepumpe);
            Assert.Equal(KaeltemittelErgebnisklasse.NichtPruefbar, wp.Klasse);

            // Die Tafel des Berichts trägt den Stern an der Maschine.
            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(stamm.Ergebnis?.Waermepumpe, _ => "", false,
                CultureInfo.GetCultureInfo("de-DE"), stamm.Ergebnis?.Kaeltemaschinen, stamm.Kaeltemittel);
            Assert.Contains(t.Zeilen, z => z.Zellen[1].Text == "R513A *");
            Assert.Single(t.Hinweise);
        }
    }
}
