using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Bereitschaftsverlust des Heizkessels ist eine Leistung in kW</b> — und ein
    /// Wärmerest nach der ganzen Kaskade steht im Laufprotokoll.
    ///
    /// <para><b>Einheit.</b> <c>Tab_Heizkessel.Betriebsbereitschaftverlust</c> kommt aus
    /// VDI 3805 Blatt 3, Satz 700, Spalte 28, und der Import weist sie in kW aus. Eine
    /// Stillstandsstunde kostet deshalb genau diesen Wert in kWh Brennstoff — nicht das
    /// Produkt mit der Nennleistung. Gemessen an Projekt 1007 der Testdatenbank: ein
    /// Gaskessel mit 22,1 kW und 0,05 kW Bereitschaftsleistung, ohne Quellpuffer; seine
    /// Stillstandsstunden sind die Stunden ohne Kesselabgabe.</para>
    ///
    /// <para><b>Meldung.</b> Projekt 1023 behält nach allen Erzeugern Wärme übrig — der
    /// Lauf nennt das als Warnung; Projekt 1030 deckt alles und bleibt ohne sie.</para>
    ///
    /// <para><b>Betriebsbereitschaft (#568).</b> Der Verlust fällt nur in Stillstandsstunden
    /// an, in denen der Kessel betriebsbereit ist: an einem Heiztag (Tagesmittel der
    /// Außentemperatur unter der Heizgrenze des Projekts, Vorgabe 15 °C,
    /// <c>Tab_Einstellungen.Kessel_Heizgrenze</c>) oder im Nachlauf von 24 Stunden nach seiner
    /// letzten Laufstunde. 1007 läuft 1 797 Stunden und steht 6 963 Stunden still; bei 254
    /// Heiztagen sind davon 4 335 betriebsbereit — die übrigen 2 628 liegen an Tagen über der
    /// Heizgrenze außerhalb des Nachlaufs. Die Vorgabe <c>Tab_Einstellungen.Kessel_Betriebsbereitschaft</c> [h/a] deckelt Lauf- plus
    /// Bereitschaftsstunden; 0 heißt „kein Deckel“.</para>
    ///
    /// <para><b>Teillast.</b> Der Kessel von 1007 ist ein Brennwertkessel (Schemaschritt 158 zieht
    /// das Kennzeichen aus dem Katalog nach); er rechnet mit der Normvorgabe von η₃₀, und seine
    /// Teillast spart Brennstoff. Der Verbrauch ist deshalb Wärme durch η₁₀₀ plus Teillastkorrektur
    /// plus Anfahrverlust der Starts (Etappe E4) plus Bereitschaftsverlust.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselBereitschaftTests : IDisposable
    {
        private const int PROJEKT_GASKESSEL = 1007;
        private const double BEREITSCHAFT_1007_KW = 0.05;
        // RP2a (Erdreich nach DIN EN ISO 13370): vorher 1797 / 4335 / 3488 Stunden.
        private const int LAUFSTUNDEN_1007 = 1729;
        private const int BEREITSCHAFTSSTUNDEN_1007 = 4403;
        private const int HEIZTAGE_1007 = 254;

        /// <summary>Eine eigene Heizgrenze für 1007 und was sie ergibt (nachgebildet aus der Basis).</summary>
        private const double GRENZE_EIGEN = 12;
        private const int HEIZTAGE_1007_EIGEN = 202;
        private const int BEREITSCHAFTSSTUNDEN_1007_EIGEN = 3555;
        private const int PROJEKT_ELEKTROKESSEL = 1017;
        private const int PROJEKT_MIT_REST = 1023;
        private const int PROJEKT_OHNE_REST = 1030;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Theory]
        [InlineData(0.075, 0.075)]
        [InlineData(1.5, 1.5)]      // kein Prozentwert: bleibt 1,5 kW
        [InlineData(0.0, 0.0)]
        [InlineData(-0.2, 0.0)]
        [InlineData(double.NaN, 0.0)]
        public void Der_Katalogwert_ist_die_Bereitschaftsleistung_in_kW(double katalog, double erwartetKw)
        {
            Assert.Equal(erwartetKw, SimulationSPK.BereitschaftsleistungKw(katalog));
        }

        [Fact]
        public void Eine_betriebsbereite_Stillstandsstunde_kostet_die_Bereitschaftsleistung_nicht_ihr_Produkt_mit_der_Nennleistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_GASKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(1, spk.KesselAnzahl);

            int stillstand = spk.Kesselleistung_stuendlich.Count(q => q <= 0);
            Assert.Equal(8760 - LAUFSTUNDEN_1007, stillstand);
            Assert.Equal(LAUFSTUNDEN_1007, spk.Laufstunden_Spk[0]);

            // Nur ein Teil der Stillstandsstunden ist betriebsbereit (Tage über der Heizgrenze
            // ohne Nachlauf). Ohne eigenen Wert gilt die Vorgabe 15 °C.
            Assert.Equal(SimulationSPK.HEIZGRENZE_VORGABE_C, spk.Heizgrenze_C);
            Assert.Equal(HEIZTAGE_1007, spk.Heiztage_Anzahl);
            Assert.Equal(BEREITSCHAFTSSTUNDEN_1007, spk.Bereitschaftsstunden_Spk[0]);
            Assert.True(spk.Bereitschaftsstunden_Spk[0] < stillstand);

            double waermeMwh = spk.s_waerme_Gas_Spk[0];
            double wirk = spk.Kessel_Wirk_Gas_Spk[0];
            Assert.True(waermeMwh > 0 && wirk > 0);

            double bereitschaftKwh = BEREITSCHAFTSSTUNDEN_1007 * BEREITSCHAFT_1007_KW;
            Assert.Equal(bereitschaftKwh, spk.Bereitschaftsverlust_KWh_Spk[0], 9);

            double teillastKwh = spk.TeillastMehrbrennstoff_KWh_Spk[0];
            Assert.True(teillastKwh < 0, "Die Teillast spart beim Brennwertkessel Brennstoff.");
            double anfahrKwh = spk.Anfahrverlust_KWh_Spk[0];
            Assert.True(anfahrKwh > 0, "Jeder Start kostet den Anfahrverlust der Normvorgabe.");
            double erwartetMwh = waermeMwh / wirk + teillastKwh / 1000.0 + anfahrKwh / 1000.0 + bereitschaftKwh / 1000.0;
            Assert.Equal(erwartetMwh, spk.Kessel_Verbrauch_MWh_Spk[0], 9);

            // Das Laufprotokoll nennt die Bereitschaftsstunden je Kessel.
            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_BEREITSCHAFT_STUNDEN.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf) &&
                h.Contains(LAUFSTUNDEN_1007.ToString(System.Globalization.CultureInfo.InvariantCulture)) &&
                h.Contains(BEREITSCHAFTSSTUNDEN_1007.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        [Theory]
        [InlineData(100, 50, true)]      // Heiztag: betriebsbereit, egal wann er lief
        [InlineData(5000, int.MinValue, false)] // Sommertag, nie gelaufen: abgeschaltet
        [InlineData(5000, 4976, true)]   // Sommertag, 24 h nach der letzten Laufstunde: Nachlauf
        [InlineData(5000, 4975, false)]  // Sommertag, 25 h danach: abgeschaltet
        public void Betriebsbereit_ist_der_Kessel_am_Heiztag_oder_im_Nachlauf(int stunde, int letzteLauf, bool erwartet)
        {
            double[] temperatur = Jahr(20.0);
            for (int h = 0; h < 24 * 120; h++) temperatur[h] = 5.0;   // Heiztage 0 … 119

            bool[] heiztage = SimulationSPK.HeiztageAus(temperatur, SimulationSPK.HEIZGRENZE_VORGABE_C);
            Assert.Equal(120, heiztage.Count(t => t));
            Assert.Equal(erwartet, SimulationSPK.IstBetriebsbereit(heiztage, stunde, letzteLauf));
        }

        /// <summary>
        /// <b>Die Regel:</b> Heiztag ist ein Tag, dessen Mittel über seine 24 Stunden unter der
        /// Heizgrenze liegt — das Mittel entscheidet, nicht die einzelne Stunde: eine kalte Nacht
        /// mit warmem Mittag über der Grenze ist kein Heiztag, eine heiße Stunde an einem kalten
        /// Tag macht keinen Sommertag.
        /// </summary>
        [Fact]
        public void Heiztag_ist_ein_Tag_mit_einem_Tagesmittel_unter_der_Heizgrenze()
        {
            double[] temperatur = Jahr(20.0);
            Tag(temperatur, 0, h => 10.0);                    // Mittel 10: Heiztag
            Tag(temperatur, 1, h => h < 12 ? 5.0 : 26.0);     // Mittel 15,5: kein Heiztag
            Tag(temperatur, 2, h => h < 12 ? 0.0 : 29.0);     // Mittel 14,5: Heiztag
            Tag(temperatur, 3, h => h == 13 ? 40.0 : 10.0);   // Mittel 11,25: Heiztag trotz heißer Stunde

            bool[] heiztage = SimulationSPK.HeiztageAus(temperatur, SimulationSPK.HEIZGRENZE_VORGABE_C);
            Assert.Equal(365, heiztage.Length);
            Assert.Equal(new[] { true, false, true, true, false }, heiztage.Take(5));
            Assert.Equal(3, heiztage.Count(t => t));
        }

        /// <summary>
        /// <b>Der Grenzfall:</b> Ein Tagesmittel GENAU auf der Grenze ist kein Heiztag (strikt
        /// „unter") — auch wenn die Summe der Stundenwerte dezimal nicht aufgeht; ein Hundertstel
        /// darunter ist Heiztag.
        /// </summary>
        [Fact]
        public void Genau_auf_der_Grenze_ist_kein_Heiztag()
        {
            double[] temperatur = Jahr(20.0);
            Tag(temperatur, 0, h => 15.0);
            Tag(temperatur, 1, h => h % 2 == 0 ? 14.9 : 15.1);    // Mittel 15 aus Dezimalbrüchen
            Tag(temperatur, 2, h => h % 4 == 0 ? 15.3 : 14.9);    // Mittel 15 aus Dezimalbrüchen
            Tag(temperatur, 3, h => 14.99);                       // knapp darunter

            bool[] heiztage = SimulationSPK.HeiztageAus(temperatur, 15.0);
            Assert.False(heiztage[0], "Mittel 15,0 ist kein Heiztag.");
            Assert.False(heiztage[1], "Mittel 15,0 aus 14,9/15,1 ist kein Heiztag.");
            Assert.False(heiztage[2], "Mittel 15,0 aus 15,3/14,9 ist kein Heiztag.");
            Assert.True(heiztage[3], "Mittel 14,99 ist Heiztag.");
        }

        /// <summary>
        /// <b>Vorgabe und eigene Grenze:</b> Leer und eine Zahl, die nicht endlich ist, rechnen
        /// 15 °C; eine eigene Grenze verschiebt die Heiztage.
        /// </summary>
        [Fact]
        public void Ohne_eigene_Grenze_gilt_die_Vorgabe_15_Grad_und_eine_eigene_verschiebt_die_Heiztage()
        {
            Assert.Equal(15.0, SimulationSPK.HEIZGRENZE_VORGABE_C);
            Assert.Equal(15.0, SimulationSPK.HeizgrenzeWirksam(null));
            Assert.Equal(15.0, SimulationSPK.HeizgrenzeWirksam(double.NaN));
            Assert.Equal(15.0, SimulationSPK.HeizgrenzeWirksam(double.PositiveInfinity));
            Assert.Equal(12.0, SimulationSPK.HeizgrenzeWirksam(12.0));
            Assert.Equal(0.0, SimulationSPK.HeizgrenzeWirksam(0.0));

            // Tagesmittel 0, 1, … 29, 0, 1, … über das Jahr: zwölf volle Durchgänge und fünf Tage.
            double[] temperatur = Jahr(20.0);
            for (int tag = 0; tag < 365; tag++) Tag(temperatur, tag, h => tag % 30);
            Assert.Equal(15 * 12 + 5, SimulationSPK.HeiztageAus(temperatur, 15.0).Count(t => t));
            Assert.Equal(12 * 12 + 5, SimulationSPK.HeiztageAus(temperatur, 12.0).Count(t => t));
            Assert.Equal(0, SimulationSPK.HeiztageAus(temperatur, 0.0).Count(t => t));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(0.0, true)]
        [InlineData(15.0, true)]
        [InlineData(30.0, true)]
        [InlineData(-0.1, false)]
        [InlineData(30.5, false)]
        [InlineData(double.NaN, false)]
        public void Die_Plausibilitaet_laesst_leer_und_0_bis_30_Grad_zu(double? wert, bool erwartet)
        {
            Assert.Equal(erwartet, SimulationSPK.HeizgrenzePlausibel(wert));
        }

        [Fact]
        public void Ohne_Temperaturreihe_gilt_jede_Stunde_als_betriebsbereit()
        {
            Assert.Null(SimulationSPK.HeiztageAus(null, SimulationSPK.HEIZGRENZE_VORGABE_C));
            Assert.True(SimulationSPK.IstBetriebsbereit(null, 5000, int.MinValue));

            // Eine Reihe, die nicht das ganze Jahr trägt: Tage ohne volle 24 Stunden sind Heiztage.
            double[] kurz = new double[24 * 10 + 5];
            for (int h = 0; h < kurz.Length; h++) kurz[h] = 20.0;
            bool[] heiztage = SimulationSPK.HeiztageAus(kurz, SimulationSPK.HEIZGRENZE_VORGABE_C);
            Assert.Equal(10, heiztage.Count(t => !t));
            Assert.Equal(355, heiztage.Count(t => t));
        }

        /// <summary>
        /// <b>Eine eigene Heizgrenze im Lauf:</b> Projekt 1007 mit 12 °C statt der Vorgabe —
        /// weniger Heiztage, weniger Bereitschaftsstunden, die Laufstunden bleiben; das
        /// Laufprotokoll nennt die wirksame Grenze und die Zahl der Heiztage.
        /// </summary>
        [Fact]
        public void Eine_eigene_Heizgrenze_des_Projekts_verschiebt_die_Bereitschaft_im_Lauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.KesselHeizgrenzeSchreiben(PROJEKT_GASKESSEL, GRENZE_EIGEN));

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_GASKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.Equal(GRENZE_EIGEN, spk.Heizgrenze_C);
            Assert.Equal(HEIZTAGE_1007_EIGEN, spk.Heiztage_Anzahl);
            Assert.Equal(LAUFSTUNDEN_1007, spk.Laufstunden_Spk[0]);
            Assert.Equal(BEREITSCHAFTSSTUNDEN_1007_EIGEN, spk.Bereitschaftsstunden_Spk[0]);
            Assert.Equal(BEREITSCHAFTSSTUNDEN_1007_EIGEN * BEREITSCHAFT_1007_KW, spk.Bereitschaftsverlust_KWh_Spk[0], 9);

            var e = SimulationErgebnisCtrl.Heizkessel(laeufer.sim, laeufer.sim.simulation_Waermebedarf);
            Assert.Equal(GRENZE_EIGEN, e.HeizgrenzeC);
            Assert.Equal(HEIZTAGE_1007_EIGEN, e.Heiztage);

            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_HEIZGRENZE.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf) &&
                h.Contains("12 °C") && h.Contains(HEIZTAGE_1007_EIGEN.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// <b>Die Rundreise der Spalte über <see cref="KonfigurationCtrl"/>:</b> leer ⇄ NULL, ein Wert
        /// kommt unverändert zurück; <c>Update</c> schreibt den Wert des Modells (auch „leer"),
        /// <c>Insert</c> einen gesetzten Wert in die neue Zeile.
        /// </summary>
        [Fact]
        public void Die_Heizgrenze_reist_ueber_KonfigurationCtrl_leer_als_NULL()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(KonfigurationCtrl.KesselHeizgrenzeLesen(PROJEKT_GASKESSEL));
            KonfigurationModel m = KonfigurationCtrl.LiesProjekt(PROJEKT_GASKESSEL);
            Assert.NotNull(m);
            Assert.Null(m.Kessel_Heizgrenze);

            Assert.True(KonfigurationCtrl.KesselHeizgrenzeSchreiben(PROJEKT_GASKESSEL, 12.5));
            Assert.Equal(12.5, KonfigurationCtrl.KesselHeizgrenzeLesen(PROJEKT_GASKESSEL));
            Assert.Equal(12.5, KonfigurationCtrl.LiesProjekt(PROJEKT_GASKESSEL).Kessel_Heizgrenze);

            // Update schreibt den Stand des Modells - auch „leer".
            var ctrl = new KonfigurationCtrl();
            Assert.True(ctrl.ProjektLesen(PROJEKT_GASKESSEL));
            Assert.Equal(12.5, ctrl.model.Kessel_Heizgrenze);
            ctrl.model.Kessel_Heizgrenze = 17.0;
            Assert.True(ctrl.Update(PROJEKT_GASKESSEL));
            Assert.Equal(17.0, KonfigurationCtrl.KesselHeizgrenzeLesen(PROJEKT_GASKESSEL));
            ctrl.model.Kessel_Heizgrenze = null;
            Assert.True(ctrl.Update(PROJEKT_GASKESSEL));
            Assert.Null(KonfigurationCtrl.KesselHeizgrenzeLesen(PROJEKT_GASKESSEL));
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = ? AND Kessel_Heizgrenze IS NOT NULL",
                new DbParam("?", PROJEKT_GASKESSEL)), System.Globalization.CultureInfo.InvariantCulture));

            // Delete + Insert (der Weg des Kaskadenspeicherns) trägt einen gesetzten Wert.
            ctrl.model.Kessel_Heizgrenze = 9.5;
            Assert.True(ctrl.Delete(PROJEKT_GASKESSEL));
            Assert.True(ctrl.Insert(PROJEKT_GASKESSEL));
            Assert.Equal(9.5, KonfigurationCtrl.KesselHeizgrenzeLesen(PROJEKT_GASKESSEL));

            // Ein unlesbarer Wert ist leer.
            Assert.Null(KonfigurationCtrl.HeizgrenzeOderLeer(DBNull.Value));
            Assert.Null(KonfigurationCtrl.HeizgrenzeOderLeer("kein Wert"));
            Assert.Null(KonfigurationCtrl.HeizgrenzeOderLeer(double.NaN));
            Assert.Equal(14.0, KonfigurationCtrl.HeizgrenzeOderLeer(14L));
        }

        [Theory]
        [InlineData(0, 1797, 4335, 4335)]      // 0 = kein Deckel
        [InlineData(6000, 1797, 4335, 4203)]   // Deckel: Vorgabe − Laufstunden
        [InlineData(9000, 1797, 4335, 4335)]   // Vorgabe über dem Bedarf: nichts zu kappen
        [InlineData(1000, 1797, 4335, 0)]      // Vorgabe unter den Laufstunden: keine Bereitschaft
        public void Die_Vorgabe_deckelt_Lauf_und_Bereitschaftsstunden(int vorgabe, int lauf, int bereit, int erwartet)
        {
            Assert.Equal(erwartet, SimulationSPK.BereitschaftsstundenGedeckelt(vorgabe, lauf, bereit));
        }

        [Fact]
        public void Die_Vorgabe_des_Projekts_nimmt_den_Ueberhang_samt_Verbrauch_zurueck_und_meldet_ihn()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int VORGABE = 6000;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Einstellungen SET Kessel_Betriebsbereitschaft = ? WHERE ID_Projekt = ?",
                new DbParam("@v", VORGABE), new DbParam("@p", PROJEKT_GASKESSEL)));

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_GASKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            int erlaubt = VORGABE - LAUFSTUNDEN_1007;
            Assert.Equal(erlaubt, spk.Bereitschaftsstunden_Spk[0]);
            Assert.Equal(erlaubt * BEREITSCHAFT_1007_KW, spk.Bereitschaftsverlust_KWh_Spk[0], 9);

            double erwartetMwh = spk.s_waerme_Gas_Spk[0] / spk.Kessel_Wirk_Gas_Spk[0] +
                                 spk.TeillastMehrbrennstoff_KWh_Spk[0] / 1000.0 +
                                 spk.Anfahrverlust_KWh_Spk[0] / 1000.0 +
                                 erlaubt * BEREITSCHAFT_1007_KW / 1000.0;
            Assert.Equal(erwartetMwh, spk.Kessel_Verbrauch_MWh_Spk[0], 9);

            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_KESSEL_BEREITSCHAFT_GEDECKELT.Split('{')[0];
            Assert.Contains(laeufer.Protokoll.Hinweise, h => h.Contains(kopf));
        }

        /// <summary>
        /// <b>Der Elektrokessel trägt keine Kesselemission (#568).</b> Sein Strom steht im
        /// Reststrombedarf und damit im Netzbezug, den die Emissionsbilanz mit dem Faktor
        /// des Stromträgers bewertet; die Modulzeile führt keinen Verbrauch. Projekt 1017
        /// fährt einen Elektrokessel: Die Stufenemissionen sind 0, sein Strom steht als
        /// Nutzwärme im Stromzähler der Stufe.
        /// </summary>
        [Fact]
        public void Der_Elektrokessel_zaehlt_seinen_Strom_einmal_im_Netzbezug_und_traegt_keine_Kesselemission()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_ELEKTROKESSEL, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            Assert.True(spk.IstStromkessel(0));
            Assert.True(spk.Kessel_Verbrauch_MWh_Spk[0] > 0);
            Assert.Equal(spk.s_waerme_Gas_Spk[0] + spk.s_waerme_Oel_Spk[0], spk.StromverbrauchSpkMwh, 9);

            Assert.Equal(0.0, spk.Em_CO2_SPK, 12);
            Assert.Equal(0.0, spk.Em_SO2_SPK, 12);
            Assert.Equal(0.0, spk.Em_NOX_SPK, 12);
            Assert.Equal(0.0, spk.Em_Staub_SPK, 12);
        }

        /// <summary>
        /// <b>Das Kesselbild teilt den Stufeneingang wie die Tafel (#568).</b> Die
        /// Kesselwärme summiert sich zum Eigenanteil (Direktdeckung plus zugerechnete
        /// Speicherentladung), der Pufferanteil der anderen Erzeuger zu ihrer
        /// Speicherentladung, und der Stapel deckt in jeder Stunde den Stufeneingang.
        /// 1030 deckt über den eigenen Puffer, 1045 zu großen Teilen aus dem Puffer der
        /// anderen Erzeuger.
        /// </summary>
        [Theory]
        [InlineData(1030)]
        [InlineData(1045)]
        public void Das_Kesselbild_teilt_den_Stufeneingang_wie_die_Tafel(int projekt)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(projekt, out fehler), "Lauf gescheitert: " + fehler);

            SimulationSPK spk = laeufer.sim.simulation_spk;
            var r = SimulationErgebnisCtrl.KesselbildReihen(spk);

            double eigenKwh = SimulationRunner.EigenanteilKesselMwh(spk) * 1000.0;
            Assert.Equal(eigenKwh, r.Kesselwaerme.Sum(), eigenKwh * 1e-9 + 1e-6);
            Assert.Equal(spk.SpeicherentladungAndere_Kwh, r.AusPufferAndere.Sum(),
                         spk.SpeicherentladungAndere_Kwh * 1e-9 + 1e-6);

            for (int h = 0; h < 8760; h++)
                Assert.True(r.Kesselwaerme[h] + r.AusPufferAndere[h] + r.RestNachKessel[h] >=
                            spk.Waermebedarf[h] - 1e-6, "Stunde " + h);

            var e = SimulationErgebnisCtrl.Heizkessel(laeufer.sim, laeufer.sim.simulation_Waermebedarf);
            Assert.Equal(spk.SpeicherentladungAndere_Kwh / 1000.0, e.AusPufferAndereMwh, 9);
            Assert.Equal(spk.Laufstunden_Spk[0], e.Laufstunden);
            Assert.True(e.AusPufferAndereMwh <= e.RestwaermeMwh + 1e-6);
        }

        [Fact]
        public void Ein_Waermerest_nach_der_Kaskade_steht_als_Warnung_im_Laufprotokoll()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_WAERME_UNTERDECKUNG.Split('{')[0];

            var mitRest = new SimulationRunner();
            string fehler;
            Assert.True(mitRest.Simuliere(PROJEKT_MIT_REST, out fehler), "Lauf gescheitert: " + fehler);
            Assert.True(mitRest.sim.RestwaermeMwh > 1.0);
            Assert.Contains(mitRest.Protokoll.Warnungen, w => w.Contains(kopf));

            var ohneRest = new SimulationRunner();
            Assert.True(ohneRest.Simuliere(PROJEKT_OHNE_REST, out fehler), "Lauf gescheitert: " + fehler);
            Assert.DoesNotContain(ohneRest.Protokoll.Warnungen, w => w.Contains(kopf));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Ein Jahr mit derselben Temperatur in jeder Stunde.</summary>
        private static double[] Jahr(double temperatur)
        {
            double[] t = new double[8760];
            for (int h = 0; h < t.Length; h++) t[h] = temperatur;
            return t;
        }

        /// <summary>Belegt die 24 Stunden eines Tages; <paramref name="wert"/> bekommt die Stunde des Tages.</summary>
        private static void Tag(double[] temperatur, int tag, Func<int, double> wert)
        {
            for (int h = 0; h < 24; h++) temperatur[tag * 24 + h] = wert(h);
        }
    }
}
