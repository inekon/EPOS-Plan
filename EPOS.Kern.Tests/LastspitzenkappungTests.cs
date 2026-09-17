using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die LASTSPITZENKAPPUNG als Berechnungsart des Einzelspeichers</b>
    /// (Anwenderbefund 17.09.2026: „Leistungspreis bei Stamm und Variante gleich, obwohl
    /// der Speicher die Lastspitze senken müsste"; Entscheide LS-E-1 (a) und LS-E-3).
    ///
    /// <para><b>Die Lage im Bestand.</b> Der Projektlauf kannte für
    /// <c>Tab_StromspeicherVariante.Berechnungsart</c> genau zwei Werte — Dauernutzung
    /// und Arbitrage; jeder andere fiel benannt auf die Dauernutzung zurück. Die
    /// Lastspitzenkappung gab es zweimal (eigene Maske, Flotte), aber keinen Weg von
    /// ihr in den Projektlauf: Ein Einzelspeicher konnte die Spitze nicht kappen, und
    /// deshalb trugen Stamm und Variante denselben Leistungspreis.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Die Kette vom gespeicherten Peak-Ziel
    /// bis zur gekappten Netzbezugsreihe des Laufs — Schemaschritt 86, Lese- und
    /// Schreibweg, Strategiewahl, Ladedeckel je Betriebsart, LS-E-3 und die
    /// Netzwirkung, die in <c>Rest_Strombedarf</c> geht. Jede Zahl ist hergeleitet;
    /// die Geräteangaben stehen im Kopf des jeweiligen Falls.</para>
    ///
    /// <para>Jeder Fall legt seine EIGENE Arbeitskopie an (die Fälle schreiben die
    /// Variante); <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class LastspitzenkappungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Beispiel WP WG 1" — führt genau eine Speicheranlage (11280).</summary>
        private const int PROJEKT = 1026;

        /// <summary>Die aktive Speichervariante dieses Projekts (kleinste ID).</summary>
        private const int VARIANTE = 10;

        /// <summary>Grundlast der synthetischen Reihe [kW].</summary>
        private const double GRUNDLAST = 5.0;

        /// <summary>Die eine Spitze der synthetischen Reihe [kW] = Referenzspitze.</summary>
        private const double SPITZE = 30.0;

        /// <summary>Der Platz der Spitze in der Viertelstundenreihe (Mitte des Jahres).</summary>
        private const int PLATZ_SPITZE = 20000;

        // =================================================================
        // 1 — Schemaschritt 86: die zwei Spalten und ihr Rundweg
        // =================================================================

        /// <summary>
        /// Der Zielstand trägt den Schritt, und die Spaltenliste nennt genau die zwei
        /// Spalten, um die es geht.
        /// </summary>
        [Fact]
        public void Der_Zielstand_traegt_den_Schritt_86()
        {
            Assert.True(SchemaStand.Zielversion >= 86,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 86.");

            Assert.Equal(2, SchemaKatalog.Schritt86_Lastspitzenkappung.Length);
            Assert.All(SchemaKatalog.Schritt86_Lastspitzenkappung,
                       s => Assert.Equal(SchemaKatalog.TAB_STROMSPEICHERVARIANTE, s.Tabelle));
            Assert.Contains(SchemaKatalog.Schritt86_Lastspitzenkappung,
                            s => s.Name == SchemaKatalog.SPALTE_PEAKZIEL_KW && s.TypDefinition == "DOUBLE");
            Assert.Contains(SchemaKatalog.Schritt86_Lastspitzenkappung,
                            s => s.Name == SchemaKatalog.SPALTE_PEAKZIEL_ADAPTIV && s.TypDefinition == "YESNO");
        }

        /// <summary>
        /// Auf der Testkopie stehen beide Spalten, und die Ja/Nein-Spalte steht wie die
        /// Hausregel es verlangt: 0/1, <c>NOT NULL DEFAULT 0</c>. Die Zielschwelle ist
        /// NULLBAR — NULL heißt „nicht gepflegt" und ist etwas anderes als 0.
        /// </summary>
        [Fact]
        public void Die_Testkopie_fuehrt_beide_Spalten_mit_den_richtigen_Regeln()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.SpalteVorhanden(SchemaKatalog.TAB_STROMSPEICHERVARIANTE,
                                                       SchemaKatalog.SPALTE_PEAKZIEL_KW));
            Assert.True(DataRepository.SpalteVorhanden(SchemaKatalog.TAB_STROMSPEICHERVARIANTE,
                                                       SchemaKatalog.SPALTE_PEAKZIEL_ADAPTIV));

            // Kein Bestandssatz führt die neue Berechnungsart — genau das macht den
            // Schritt ergebnisneutral und den Referenzlauf byte-gleich.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + SchemaKatalog.TAB_STROMSPEICHERVARIANTE +
                                  " WHERE Berechnungsart = '" + DbWerte.SP_BERECHNUNG_PEAKSHAVING + "'"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + SchemaKatalog.TAB_STROMSPEICHERVARIANTE +
                                  " WHERE [" + SchemaKatalog.SPALTE_PEAKZIEL_KW + "] IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + SchemaKatalog.TAB_STROMSPEICHERVARIANTE +
                                  " WHERE [" + SchemaKatalog.SPALTE_PEAKZIEL_ADAPTIV + "] <> 0"));
        }

        /// <summary>
        /// DER RUNDWEG: Was der Controller schreibt, liest er wieder — und ein geleertes
        /// Ziel kommt als <c>null</c> zurück, nicht als 0. Der Unterschied ist kein
        /// Schönheitsfehler: 0 wäre die Kappung auf 0 kW.
        /// </summary>
        [Fact]
        public void Das_Peakziel_geht_unveraendert_durch_Schreiben_und_Lesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new StromspeicherVarianteCtrl();
            StromspeicherVarianteModel v = ctrl.ReadAktiveVariante(PROJEKT);
            Assert.NotNull(v);
            Assert.Null(v.PeakZiel_kW);
            Assert.False(v.PeakZiel_Adaptiv);

            v.Berechnungsart = DbWerte.SP_BERECHNUNG_PEAKSHAVING;
            v.PeakZiel_kW = 17.5;
            v.PeakZiel_Adaptiv = true;
            Assert.True(ctrl.Update(v));

            StromspeicherVarianteModel gelesen = new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT);
            Assert.Equal(DbWerte.SP_BERECHNUNG_PEAKSHAVING, gelesen.Berechnungsart);
            Assert.Equal(17.5, gelesen.PeakZiel_kW.Value, 6);
            Assert.True(gelesen.PeakZiel_Adaptiv);

            gelesen.PeakZiel_kW = null;
            Assert.True(new StromspeicherVarianteCtrl().Update(gelesen));
            Assert.Null(new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT).PeakZiel_kW);
        }

        // =================================================================
        // 2 — Die zwei Reihenfunktionen
        // =================================================================

        /// <summary>
        /// Die REFERENZSPITZE ist die höchste NETZLAST, nicht die höchste Last: Erzeugung
        /// geht ab. Herleitung: Last 10/40/10, PV 0/35/0 → Netzlast 10/5/10, Spitze 10.
        /// Eine reine Lastbetrachtung läse hier 40 und kappte gegen eine Spitze, die es
        /// am Zähler nicht gibt.
        /// </summary>
        [Fact]
        public void Die_Referenzspitze_zieht_die_Erzeugung_ab()
        {
            double[] last = { 10, 40, 10 };
            double[] pv = { 0, 35, 0 };
            var eingang = new SpeicherEingang(last, pv, new double[3], null, null, null);

            Assert.Equal(10.0, StromspeicherSimCtrl.NetzbezugsspitzeKw(eingang), 9);
        }

        /// <summary>
        /// Die NETZWIRKUNG ist die Differenz der nichtnegativen Netzlasten — und sie darf
        /// NEGATIV werden. Genau das ist der Befund LS-1: Lädt der Speicher aus dem Netz,
        /// steigt der Netzbezug des Intervalls, und <c>Rest_Strombedarf</c> muss das
        /// sehen. Die blosse Entladung kennt diesen Fall nicht.
        /// </summary>
        [Fact]
        public void Die_Netzwirkung_wird_bei_Netzladung_negativ()
        {
            // Intervall 0: gekappt 30 → 20 (Wirkung +10). Intervall 1: geladen 5 → 20
            // (Wirkung -15). Intervall 2: Überschuss, beide Seiten nichtnegativ 0.
            var ps = new PeakShaving(new PeakShavingParameter
            {
                PZielKw = 20,
                LadedeckelKw = 20,
                LeistungspreisEurProKwA = 0,
                BezugspreisMittelCtKwh = 0
            });

            PeakShavingErgebnis r = ps.BerechnePeakShaving(
                new double[] { 30, 5, -10 },
                new SpeicherParameter
                {
                    CNomKwh = 100,
                    PKw = 50,
                    SoCMinKwh = 0,
                    SoCMaxKwh = 100,
                    RoundTripWirkungsgrad = 1.0,
                    StartSoCKwh = 50,
                    DtH = 1.0,
                    Kapitalzins = 0.04,
                    NutzungsdauerA = 15
                });

            double[] wirkung = StromspeicherSimCtrl.NetzwirkungKw(r);

            Assert.Equal(10.0, wirkung[0], 9);
            Assert.Equal(-15.0, wirkung[1], 9);
            Assert.True(wirkung[2] <= 0.0);
        }

        // =================================================================
        // 3 — Der Projektlauf: die Kappung kommt an
        // =================================================================

        /// <summary>
        /// GRAUSTROM, Ziel unter der Spitze — der Regelfall des Anwenderbefunds.
        ///
        /// <para>Herleitung: Die synthetische Reihe trägt 5 kW Grundlast und eine
        /// Viertelstunde mit 30 kW; ohne PV ist das zugleich die Netzlast, die
        /// Referenzspitze also 30,00 kW. Das Peak-Ziel steht auf 20 kW; der Speicher
        /// (11,04 kW, 10,2 kWh) muss 30 − 20 = 10 kW für eine Viertelstunde abgeben,
        /// also 2,5 kWh — beides liegt unter seinen Grenzen. Die neue Spitze ist damit
        /// genau das Ziel: 20,00 kW. Der Ladedeckel steht ebenfalls auf 20 kW
        /// (Graustrom, Ziel unter der Referenzspitze), und weil die Grundlast mit 5 kW
        /// darunter liegt, lädt der Speicher aus dem Netz nach — die Netzwirkung ist
        /// dort NEGATIV.</para>
        /// </summary>
        [Fact]
        public void Graustrom_kappt_die_Spitze_auf_das_Ziel_und_laedt_aus_dem_Netz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Variante(DbWerte.SP_BERECHNUNG_PEAKSHAVING, 20.0, false, DbWerte.SP_BETRIEBSART_GRAUSTROM);

            var ctrl = new StromspeicherSimCtrl();
            SpeicherErgebnis ergebnis = ctrl.RechneAktiveVariante(Simulation(), PROJEKT);

            Assert.NotNull(ergebnis);
            StromspeicherLaufKontext k = ctrl.LetzterKontext;

            Assert.Equal(SPITZE, k.PeakReferenzspitzeKw, 6);
            Assert.Equal(20.0, k.PeakLadedeckelKw, 6);
            Assert.NotNull(k.Peakshavingergebnis);
            Assert.Equal(SPITZE, k.Peakshavingergebnis.PAltMaxKw, 6);
            Assert.Equal(20.0, k.Peakshavingergebnis.PNeuMaxKw, 6);

            // Die Netzwirkung geht in Rest_Strombedarf: an der Spitze +10 kW (30 auf
            // 20 gekappt). Unmittelbar DANACH ist das Band nicht mehr voll - der
            // Speicher laedt aus dem Netz nach, und die Wirkung ist NEGATIV. Sie bleibt
            // dabei betraglich unter dem Ladedeckel: 5 kW Grundlast duerfen hoechstens
            // auf 20 kW steigen, also -15 kW. (Vor der Spitze steht der Speicher voll -
            // Ladezustand 100 % - und laedt nichts.)
            Assert.NotNull(k.NetzwirkungKw);
            Assert.Equal(35040, k.NetzwirkungKw.Length);
            Assert.Equal(10.0, k.NetzwirkungKw[PLATZ_SPITZE], 6);
            Assert.Equal(0.0, k.NetzwirkungKw[0], 6);

            double nachladen = k.NetzwirkungKw[PLATZ_SPITZE + 1];
            Assert.True(nachladen < 0.0, "Nach der Kappung muesste der Speicher nachladen.");
            Assert.True(nachladen >= -15.0 - 1e-9, "Die Netzladung hat den Ladedeckel gerissen.");
            Assert.True(k.Peakshavingergebnis.LadeenergieKwh > 0.0);

            // UND: Kein Intervall steigt durch das Nachladen ueber den Deckel.
            foreach (double p in k.Peakshavingergebnis.PNeuKw)
                Assert.True(p <= 20.0 + 1e-9, "Die Netzladung hat das Ziel gerissen.");
        }

        /// <summary>
        /// GEGENPROBE zum Fall darüber: Nagelt man dieselbe Variante auf die Dauernutzung
        /// fest, entsteht KEINE Kappung — es gibt kein Peak-Ergebnis, keine eigene
        /// Netzwirkung, und der Projektlauf zieht wie bisher nur die Entladung ab. Genau
        /// dieser Zustand war der Anwenderbefund.
        /// </summary>
        [Fact]
        public void Gegenprobe_die_Dauernutzung_kappt_keine_Spitze()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Variante(DbWerte.SP_BERECHNUNG_DAUERNUTZUNG, 20.0, false, DbWerte.SP_BETRIEBSART_GRAUSTROM);

            var ctrl = new StromspeicherSimCtrl();
            Assert.NotNull(ctrl.RechneAktiveVariante(Simulation(), PROJEKT));

            Assert.Null(ctrl.LetzterKontext.Peakshavingergebnis);
            Assert.Null(ctrl.LetzterKontext.NetzwirkungKw);
            Assert.Equal(0.0, ctrl.LetzterKontext.PeakReferenzspitzeKw, 6);
        }

        /// <summary>
        /// GRÜNSTROM: Der Ladedeckel steht auf 0 — geladen wird ausschließlich aus
        /// Erzeugungsüberschuss. Die synthetische Reihe hat keinen, also lädt der
        /// Speicher gar nicht, und die Netzwirkung ist nirgends negativ: Aus dem Netz
        /// kommt keine Kilowattstunde.
        /// </summary>
        [Fact]
        public void Gruenstrom_laedt_nicht_aus_dem_Netz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Variante(DbWerte.SP_BERECHNUNG_PEAKSHAVING, 20.0, false, DbWerte.SP_BETRIEBSART_GRUENSTROM);

            var ctrl = new StromspeicherSimCtrl();
            Assert.NotNull(ctrl.RechneAktiveVariante(Simulation(), PROJEKT));
            StromspeicherLaufKontext k = ctrl.LetzterKontext;

            Assert.Equal(0.0, k.PeakLadedeckelKw, 6);
            Assert.Equal(0.0, k.Peakshavingergebnis.LadeenergieKwh, 6);
            foreach (double w in k.NetzwirkungKw)
                Assert.True(w >= -1e-9, "Der Grünstromspeicher hat aus dem Netz geladen.");

            // Gekappt wird trotzdem: der Startfüllstand trägt die eine Spitze.
            Assert.Equal(20.0, k.Peakshavingergebnis.PNeuMaxKw, 6);
        }

        /// <summary>
        /// OHNE ZIEL kein Lauf: <c>PeakZiel_kW</c> NULL und nicht adaptiv heißt „nicht
        /// gepflegt". Der Lauf fällt BENANNT auf die Dauernutzung zurück — der Hinweis
        /// steht im Protokoll, und es entsteht kein Peak-Ergebnis.
        /// </summary>
        [Fact]
        public void Ohne_Peakziel_faellt_der_Lauf_benannt_auf_die_Dauernutzung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Variante(DbWerte.SP_BERECHNUNG_PEAKSHAVING, null, false, DbWerte.SP_BETRIEBSART_GRAUSTROM);

            var ctrl = new StromspeicherSimCtrl();
            Assert.NotNull(ctrl.RechneAktiveVariante(Simulation(), PROJEKT));

            Assert.Null(ctrl.LetzterKontext.Peakshavingergebnis);
            Assert.Null(ctrl.LetzterKontext.NetzwirkungKw);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMENG_SPEICHER_PEAK_OHNE_ZIEL, ctrl.LetzterHinweis);
        }

        /// <summary>
        /// LS-E-3: Ein Ziel AUF der Referenzspitze kappt nichts. Der Lauf sagt es im
        /// Protokoll, und der Ladedeckel hält die Spitze dort, wo sie war — sie steigt
        /// NICHT durch die Netzladung.
        ///
        /// <para>Herleitung: Referenzspitze 30,00 kW, Ziel 30 kW. Ohne Deckel lüde der
        /// Speicher die Grundlast von 5 kW um bis zu 11,04 kW hoch; die neue Spitze
        /// bliebe 30 kW, weil auch der Ladepfad dort endet. Beansprucht wird genau das:
        /// <c>PNeuMax ≤ PAltMax</c>.</para>
        /// </summary>
        [Fact]
        public void Ziel_auf_der_Referenzspitze_warnt_und_hebt_die_Spitze_nicht_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Variante(DbWerte.SP_BERECHNUNG_PEAKSHAVING, SPITZE, false, DbWerte.SP_BETRIEBSART_GRAUSTROM);

            var ctrl = new StromspeicherSimCtrl();
            Assert.NotNull(ctrl.RechneAktiveVariante(Simulation(), PROJEKT));
            StromspeicherLaufKontext k = ctrl.LetzterKontext;

            Assert.Contains("30", ctrl.LetzterHinweis);
            Assert.Contains(Vorspann(WindowsFormsApplication1.MyResource.Resource.SIMENG_SPEICHER_PEAK_ZIEL_ZU_HOCH),
                            ctrl.LetzterHinweis);
            Assert.Equal(SPITZE, k.PeakLadedeckelKw, 6);
            Assert.True(k.Peakshavingergebnis.PNeuMaxKw <= k.Peakshavingergebnis.PAltMaxKw + 1e-9,
                        "Das zu hohe Ziel hat die Spitze angehoben.");
        }

        // =================================================================
        // 4 — Ende zu Ende: die Spitze schlägt auf die Energiekosten durch
        // =================================================================

        /// <summary>
        /// DER BEFUND, zu Ende gerechnet: Bei GLEICHER Arbeit ist der Unterschied der
        /// Energiekosten zwischen Stamm und Variante genau
        /// <c>(Spitze_Stamm − Spitze_Variante) × Satz</c>.
        ///
        /// <para>Herleitung (Muster <c>StromLeistungspreisTests</c>): Projekt 1026 bezieht
        /// 19,08 MWh/a; bei 0,35 €/kWh sind das 6 678,00 € Arbeit in BEIDEN Läufen. Der
        /// Leistungspreis steht auf 60 €/(kW·a), Modus JAHR. Der Stamm fährt die
        /// ungekappte Spitze von 30 kW (1 800,00 €), die Variante die gekappte von 20 kW
        /// (1 200,00 €). Die Differenz ist 600,00 € = (30 − 20) × 60.</para>
        ///
        /// <para>Das ist der Zahlungsstrom, der bis dahin fehlte: <c>Ertrag_Leistungspreis</c>
        /// bleibt 0 (E5-1) — der Ertrag der Kappung steckt im Reststrombetrag und
        /// nirgends sonst.</para>
        /// </summary>
        [Fact]
        public void Die_gekappte_Spitze_senkt_die_Energiekosten_um_Delta_mal_Satz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreise(0.35, 60.0, DbWerte.LEISTUNGSPREIS_MODUS_JAHR);

            VariantenDaten stamm = Rechne(SPITZE);
            VariantenDaten variante = Rechne(20.0);

            const double arbeit = 19.08 * 1000.0 * 0.35;   // 6 678,00 €

            Assert.Equal(arbeit + 30.0 * 60.0, stamm.Energiekosten.Value, 2);
            Assert.Equal(arbeit + 20.0 * 60.0, variante.Energiekosten.Value, 2);
            Assert.Equal((SPITZE - 20.0) * 60.0,
                         stamm.Energiekosten.Value - variante.Energiekosten.Value, 2);

            // Gleiche Arbeit in beiden Läufen — nur die Spitze unterscheidet sie.
            Assert.Equal(stamm.Energiekosten.Value - stamm.EnergieLeistungsanteil.Value,
                         variante.Energiekosten.Value - variante.EnergieLeistungsanteil.Value, 2);
        }

        // =================================================================
        // 5 — Anzeige und Texte, beide Sprachen
        // =================================================================

        /// <summary>
        /// Die dritte Berechnungsart hat einen Anzeigetext, und er steht in BEIDEN
        /// Sprachen — ein vertippter Schlüssel fiele sonst nirgends auf.
        /// </summary>
        [Fact]
        public void Die_Berechnungsart_hat_einen_Anzeigetext_in_beiden_Sprachen()
        {
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;

            string de = rm.GetString("SP_BERECHNUNG_ANZEIGE_PEAKSHAVING", new CultureInfo("de-DE"));
            string en = rm.GetString("SP_BERECHNUNG_ANZEIGE_PEAKSHAVING", new CultureInfo("en-US"));

            Assert.False(string.IsNullOrEmpty(de));
            Assert.False(string.IsNullOrEmpty(en));
            Assert.NotEqual(de, en);
            Assert.Equal(de, SpeicherAnzeigeCtrl.BerechnungsartText(DbWerte.SP_BERECHNUNG_PEAKSHAVING));
        }

        /// <summary>
        /// Die acht neuen Texte stehen in BEIDEN Ressourcendateien — die Regel „Texte in
        /// beiden Sprachen" gilt auch für Protokollzeilen.
        /// </summary>
        [Theory]
        [InlineData("SIMENG_SPEICHER_PEAK_OHNE_ZIEL")]
        [InlineData("SIMENG_SPEICHER_PEAK_ZIEL_ZU_HOCH")]
        [InlineData("SIMENG_SPEICHER_PEAK_LAUF")]
        [InlineData("SP_PARAM_LABEL_PEAKZIEL")]
        [InlineData("SP_PARAM_LABEL_PEAKZIEL_ADAPTIV")]
        [InlineData("SP_PARAM_HINWEIS_PEAKZIEL")]
        [InlineData("PEAK_BTN_VARIANTE")]
        [InlineData("SP_KONTEXT_EINZEL_PEAKSHAVING")]
        public void Jeder_neue_Text_steht_in_beiden_Sprachen(string schluessel)
        {
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;

            string de = rm.GetString(schluessel, new CultureInfo("de-DE"));
            string en = rm.GetString(schluessel, new CultureInfo("en-US"));

            Assert.False(string.IsNullOrEmpty(de), schluessel + " fehlt in de.");
            Assert.False(string.IsNullOrEmpty(en), schluessel + " fehlt in en.");
            Assert.NotEqual(de, en);
        }

        /// <summary>
        /// Die KOPFZEILE des Simulationsreiters nennt Art und Ziel — wie bei der Flotte.
        /// Sie steht NUR bei der Lastspitzenkappung; jede andere Art liefert den leeren
        /// Text, damit dort nichts Doppeltes erscheint.
        /// </summary>
        [Fact]
        public void Die_Kopfzeile_nennt_Art_Ziel_und_das_Adaptiv_Kennzeichen()
        {
            var v = new StromspeicherVarianteModel
            {
                Berechnungsart = DbWerte.SP_BERECHNUNG_PEAKSHAVING,
                PeakZiel_kW = 80.0
            };

            string fest = SpeicherAnzeigeCtrl.EinzelspeicherKontextText(v);
            Assert.Contains("80", fest);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_PEAK_ADAPTIV, fest);

            v.PeakZiel_Adaptiv = true;
            Assert.EndsWith(WindowsFormsApplication1.MyResource.Resource.SP_KONTEXT_PEAK_ADAPTIV,
                            SpeicherAnzeigeCtrl.EinzelspeicherKontextText(v));

            v.Berechnungsart = DbWerte.SP_BERECHNUNG_DAUERNUTZUNG;
            Assert.Equal("", SpeicherAnzeigeCtrl.EinzelspeicherKontextText(v));
            Assert.Equal("", SpeicherAnzeigeCtrl.EinzelspeicherKontextText(null));
        }

        // =================================================================
        // 6 — Der Ausgang der Maske (LS-E-1 (a))
        // =================================================================

        /// <summary>
        /// „In Variante übernehmen" schreibt Berechnungsart, Ziel und Adaptiv-Flag in
        /// EINEM Zug — danach rechnet der Projektlauf genau die Kappung, die der
        /// Anwender in der Maske gefunden hat. Der Excel-Kompatibilitätsmodus geht dabei
        /// aus: Er gehört der Dauernutzung.
        /// </summary>
        [Fact]
        public void Der_Knopf_schreibt_Art_Ziel_und_Flag_in_die_aktive_Variante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG,
                         PeakShavingCtrl.AktiveBerechnungsart(PROJEKT));

            Assert.True(PeakShavingCtrl.InVarianteUebernehmen(PROJEKT, 42.5, true));

            StromspeicherVarianteModel v = new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT);
            Assert.Equal(DbWerte.SP_BERECHNUNG_PEAKSHAVING, v.Berechnungsart);
            Assert.Equal(42.5, v.PeakZiel_kW.Value, 6);
            Assert.True(v.PeakZiel_Adaptiv);
            Assert.False(v.Kompatibilitaetsmodus);

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_PEAKSHAVING,
                         PeakShavingCtrl.AktiveBerechnungsart(PROJEKT));
        }

        /// <summary>Ohne Projekt schreibt der Knopf nichts und meldet es.</summary>
        [Fact]
        public void Ohne_Projekt_schreibt_der_Knopf_nichts()
        {
            Assert.False(PeakShavingCtrl.InVarianteUebernehmen(0, 42.5, false));
            Assert.Null(PeakShavingCtrl.AktiveBerechnungsart(0));
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        /// <summary>
        /// Eine Simulation mit synthetischer Stromreihe: Grundlast und EINE Spitze, ohne
        /// PV, Wärmepumpe oder Kessel — so ist die Netzlast die Reihe selbst, und jede
        /// Zahl des Falls lässt sich von Hand nachrechnen.
        /// </summary>
        private static SimulationControl Simulation()
        {
            double[] reihe = new double[35040];
            for (int i = 0; i < reihe.Length; i++) reihe[i] = GRUNDLAST;
            reihe[PLATZ_SPITZE] = SPITZE;

            return new SimulationControl
            {
                simulation_Strombedarf = new SimulationStrombedarf
                {
                    Strombedarf_viertelStundenwerte = reihe
                },
                Rest_Strombedarf_viertelstuendlich = (double[])reihe.Clone()
            };
        }

        /// <summary>Setzt die aktive Variante des Projekts auf den gewünschten Stand.</summary>
        private static void Variante(string art, double? zielKw, bool adaptiv, string betriebsart)
        {
            DataRepository.ExecuteSQL(
                "UPDATE " + SchemaKatalog.TAB_STROMSPEICHERVARIANTE + " SET Berechnungsart = ?, " +
                "Betriebsart = ?, [" + SchemaKatalog.SPALTE_PEAKZIEL_KW + "] = ?, [" +
                SchemaKatalog.SPALTE_PEAKZIEL_ADAPTIV + "] = ? WHERE ID = ?",
                new DbParam("@art", art),
                new DbParam("@bart", betriebsart),
                new DbParam("@ziel", DbParamTyp.Double)
                    { Wert = zielKw.HasValue ? (object)zielKw.Value : DBNull.Value },
                new DbParam("@adaptiv", adaptiv),
                new DbParam("@id", VARIANTE));
        }

        /// <summary>Ein Kostenlauf des Projekts mit genau dieser Bezugsspitze.</summary>
        private static VariantenDaten Rechne(double spitzeKw)
        {
            var s = new Netzbezugsspitze { JahrKW = spitzeKw };
            for (int m = 0; m < 12; m++) s.MonatKW[m] = spitzeKw;

            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.NotNull(erg);

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                Ergebnis = erg,
                Zeitreihen = new ZeitreihenSatz { Bezugsspitze = s }
            };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        private static void Katalogpreise(double arbeit, double leistung, string modus)
        {
            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET price_work = ?, price_power = ?, price_power_modus = ? WHERE id = ?",
                new DbParam("@a", arbeit), new DbParam("@l", leistung),
                new DbParam("@m", modus), new DbParam("@c", 60));
        }

        /// <summary>Der Textanfang einer Vorlage bis zum ersten Platzhalter.</summary>
        private static string Vorspann(string vorlage)
        {
            int p = vorlage.IndexOf('{');
            return p > 0 ? vorlage.Substring(0, p) : vorlage;
        }

        private static long Zahl(string sql)
        {
            object wert = DataRepository.ExecuteScalar(sql);
            return wert == null || wert == DBNull.Value
                ? -1
                : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }
    }
}
