using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>E1 — der Elektrokessel gehört zur elektrischen Welt</b> (Anwenderentscheid
    /// 18.09.2026: „Für Elektroheizkessel muss Strom als Energieträger auswählbar und
    /// zuzuordnen sein").
    ///
    /// <para><b>Was hier festgehalten wird — drei Dinge.</b>
    /// <list type="number">
    ///   <item><description><b>ZUORDNUNG.</b> Ein Heizkessel, dessen Gerät
    ///     <c>Tab_Heizkessel.Brennstoff</c> = 13 führt, bekommt dieselbe Behandlung wie
    ///     eine Wärmepumpe: zulässig ist nur die Stromfamilie, die Anlagenliste führt
    ///     ihn mit dem projektweiten Stromträger, und die Verwendungsliste nennt ihn im
    ///     Elektro-Zweig — nicht über den Brennstoffweg, der unter drei Stromträgern
    ///     des Katalogs einen anderen treffen könnte als die Wärmepumpe daneben.</description></item>
    ///   <item><description><b>EINE WAHRHEIT ÜBER DIE MENGE.</b> Der Stromeinsatz des
    ///     Elektrokessels ist seine Nutzwärme —
    ///     <see cref="SimulationSPK.StromeinsatzElektrokesselMwh"/>. Dieselbe Funktion
    ///     bucht die Simulation auf den Stromzähler und weist der
    ///     <see cref="EndenergieAufloeser"/> aus.</description></item>
    ///   <item><description><b>KEINE DOPPELZÄHLUNG.</b> Die Modulzeile bleibt bei
    ///     <c>Verbrauch</c> = 0; der Strom steht weiterhin genau einmal in den Kosten,
    ///     nämlich im Netzbezug. Der Auflöser zeigt ihn, er bucht ihn nicht.</description></item>
    /// </list></para>
    ///
    /// <para><b>Gemessen an <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>.</b> Zwei
    /// Projekte führen einen Elektrokessel: <b>1017</b> („eloBLOCK VE 28", Gerät
    /// 1017237) ohne gespeicherten Lauf — dort wird gerechnet — und <b>1024</b>
    /// („eloBLOCK VE 10", Gerät 1018320) mit gespeichertem Lauf 199, dessen Kesselzeile
    /// 52,99 MWh Nutzwärme und ebenso viel auf dem Stromzähler der Anlagenzeile
    /// führt. Die Gegenprobe stellt Projekt 1030 mit seinem GASkessel.</para>
    ///
    /// <para>Kultur auf de-DE gepinnt und im <see cref="Dispose"/> zurückgestellt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ElektrokesselStromTests : IDisposable
    {
        /// <summary>Führt den Elektrokessel „eloBLOCK VE 28" — ohne gespeicherten Lauf.</summary>
        private const int PROJEKT_1017 = 1017;

        /// <summary>Führt den Elektrokessel „eloBLOCK VE 10" — MIT gespeichertem Lauf.</summary>
        private const int PROJEKT_1024 = 1024;

        /// <summary>Führt einen Gaskessel — die Gegenprobe.</summary>
        private const int PROJEKT_GAS = 1030;

        private const int GERAET_ELEKTROKESSEL_1017 = 1017237;
        private const string ANLAGE_ELEKTROKESSEL_1017 = "eloBLOCK VE 28";
        private const string ANLAGE_ELEKTROKESSEL_1024 = "eloBLOCK VE 10";
        private const string ANLAGE_WAERMEPUMPE_1017 = "WPE-I 59 H 400 Premium";

        /// <summary><c>Tab_Energieanlagen.ID</c> bzw. <c>Tab_Heizkessel.ID</c> des
        /// Gaskessels im Projekt 1030. Sein Bezeichner trägt in der Testdatenbank ein
        /// Zeichen aus Windows-1252 und taugt deshalb nicht als Schlüssel.</summary>
        private const int ANLAGE_GASKESSEL = 11334;
        private const int GERAET_GASKESSEL = 1018330;

        private const string ANLAGE_SPEICHER_1017 = "BYD HVS+ 12.8";

        /// <summary>Der zweite ELECTRICITY-Träger, den Projekt 1017 führt
        /// („Elektrische Energie 2") — nicht seine Vorgabe, also als Wahl erkennbar.</summary>
        private const int TRAEGER_STROM_ZWEI = 58;

        /// <summary>52,99 MWh × 1000 — Nutzwärme und Stromeinsatz des Laufs 199.</summary>
        private const double STROMEINSATZ_1024_KWH = 52990.0;

        private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiKulturVorher = CultureInfo.CurrentUICulture;

        public ElektrokesselStromTests()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _kulturVorher;
            CultureInfo.CurrentUICulture = _uiKulturVorher;
        }

        // =====================================================================
        // Teil A — die Zuordnung
        // =====================================================================

        /// <summary>
        /// Die Trägerzulassung des Elektrokessels ist die der Wärmepumpe: die
        /// Stromfamilie, sonst nichts. Die Gegenprobe zeigt, dass die Aussage am GERÄT
        /// hängt und nicht an der Komponente „Heizkessel".
        /// </summary>
        [Fact]
        public void Der_Elektrokessel_laesst_nur_Strom_zu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> codes = EnergietraegerZulaessigkeit.Kategoriecodes(
                DbWerte.ERZEUGER_HEIZKESSEL, GERAET_ELEKTROKESSEL_1017);

            Assert.NotNull(codes);
            Assert.Equal(new[] { EnergietraegerZulaessigkeit.CODE_STROM }, codes);

            // Wortgleich mit der Wärmepumpe — dieselbe Welt, dieselbe Liste.
            Assert.Equal(EnergietraegerZulaessigkeit.Kategoriecodes(DbWerte.ERZEUGER_WAERMEPUMPE, 0),
                         codes);

            // Und in Gruppen übersetzt heißt das „Strom".
            IReadOnlyList<string> gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppen(
                DbWerte.ERZEUGER_HEIZKESSEL, GERAET_ELEKTROKESSEL_1017);
            Assert.NotNull(gruppen);
            Assert.Equal(new[] { "Strom" }, gruppen);
        }

        /// <summary>
        /// Die Gegenprobe: Ein Kessel OHNE Gerät bleibt offen (er kann alles verbrennen),
        /// und ein Gaskessel bekommt keine Stromträger.
        /// </summary>
        [Fact]
        public void Ein_Kessel_ohne_Elektrogeraet_bleibt_beim_Brennstoff()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(EnergietraegerZulaessigkeit.Kategoriecodes(DbWerte.ERZEUGER_HEIZKESSEL, 0));

            IReadOnlyList<string> codes = EnergietraegerZulaessigkeit.Kategoriecodes(
                DbWerte.ERZEUGER_HEIZKESSEL, GERAET_GASKESSEL);
            Assert.NotNull(codes);
            Assert.DoesNotContain(EnergietraegerZulaessigkeit.CODE_STROM, codes);
        }

        /// <summary>
        /// Die Anlagenliste — die Quelle der Komponentenliste in der
        /// Energieträgerverwaltung — führt den Elektrokessel mit DEMSELBEN Träger wie
        /// die Wärmepumpe desselben Projekts: dem projektweiten Stromträger.
        /// </summary>
        [Fact]
        public void Die_Anlagenliste_fuehrt_den_Elektrokessel_mit_dem_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<ProjektEnergietraegerCtrl.AnlagenEintrag> anlagen =
                ProjektEnergietraegerCtrl.AnlagenMitTraeger(PROJEKT_1017);

            ProjektEnergietraegerCtrl.AnlagenEintrag kessel =
                Eintrag(anlagen, ANLAGE_ELEKTROKESSEL_1017);
            ProjektEnergietraegerCtrl.AnlagenEintrag pumpe =
                Eintrag(anlagen, ANLAGE_WAERMEPUMPE_1017);

            Assert.NotNull(kessel);
            Assert.NotNull(pumpe);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, kessel.Komponente);
            Assert.Equal(GERAET_ELEKTROKESSEL_1017, kessel.GeraeteId);

            int strom = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_1017);
            Assert.True(strom > 0, "Das Projekt führt keinen Stromträger.");
            Assert.Equal(strom, kessel.CarrierId);
            Assert.Equal(pumpe.CarrierId, kessel.CarrierId);
        }

        /// <summary>
        /// Die Verwendungsliste nennt den Elektrokessel im ELEKTRO-Zweig: unter dem
        /// Stromträger des Projekts, mit seinem Klartext als Beiträger.
        /// </summary>
        [Fact]
        public void Verwendete_fuehrt_den_Elektrokessel_beim_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int strom = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_1017);
            string klartext = DbWerte.ERZEUGER_HEIZKESSEL + " „" + ANLAGE_ELEKTROKESSEL_1017 + "“";

            ProjektEnergietraegerCtrl.Verwendung treffer = null;
            foreach (ProjektEnergietraegerCtrl.Verwendung v in
                     ProjektEnergietraegerCtrl.Verwendete(PROJEKT_1017))
                if (v.Beitraeger.Contains(klartext)) treffer = v;

            Assert.NotNull(treffer);
            Assert.Equal(strom, treffer.CarrierId);
        }

        /// <summary>
        /// Die Energieträgerverwaltung selbst: Ihre Trägerliste führt den Stromträger mit
        /// dem Elektrokessel als Beiträger („verwendet von: Heizkessel „eloBLOCK VE 28"").
        /// Damit steht er dort, wo der Anwender ihn zuordnet.
        /// </summary>
        [Fact]
        public void Die_Traegerliste_nennt_den_Elektrokessel_beim_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var huelle = new EnergietraegerHuelle(PROJEKT_1017);
            IReadOnlyDictionary<string, object> gaben = huelle.Gaben(0);
            var liste = (IReadOnlyList<EPOS.UI.Dialoge.Kosten.EnergietraegerDialog.EnergietraegerListe>)
                        gaben["Liste"];

            int strom = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_1017);
            EPOS.UI.Dialoge.Kosten.EnergietraegerDialog.EnergietraegerListe zeile = null;
            foreach (EPOS.UI.Dialoge.Kosten.EnergietraegerDialog.EnergietraegerListe z in liste)
                if (z.Traeger == strom) zeile = z;

            Assert.NotNull(zeile);
            Assert.Contains(ANLAGE_ELEKTROKESSEL_1017, zeile.Kurztext);
        }

        /// <summary>
        /// Ein Projekt, dessen einzige elektrische Anlage der Elektrokessel ist, braucht
        /// trotzdem einen Stromträger — sonst stünde sein Netzbezug ohne Preis und ohne
        /// Emissionsfaktor da. Gebaut wird die Lage in der ARBEITSKOPIE: Aus Projekt 1017
        /// bleiben nur die Kesselzeilen stehen.
        /// </summary>
        [Fact]
        public void Ein_Projekt_mit_nur_einem_Elektrokessel_braucht_einen_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Kessel IS NULL",
                new DbParam("@p", PROJEKT_1017));

            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT_1017));
            Assert.True(ProjektEnergietraegerCtrl.StandardStromTraeger(PROJEKT_1017) > 0);
        }

        /// <summary>
        /// <b>Die Rangfolge der Anlagenwahl kennt den Elektrokessel.</b> Ist er die
        /// einzige Anlage, die einen Stromträger gewählt hat, gilt SEINE Wahl für das
        /// ganze Projekt — Preis, Anteile und Emissionen lesen dieselbe Zahl. Einen
        /// eigenen Tarif je Verbraucher gibt es nicht: Der Netzbezug wird einmal
        /// bepreist (Anwenderentscheid 18.09.2026).
        ///
        /// <para>Ohne gesetzte <c>ID_Carrier</c> — der Stand der Testdatenbank — bleibt
        /// es bei der Vorgabe, der kleinsten Id der Zuordnungen.</para>
        /// </summary>
        [Fact]
        public void Der_am_Elektrokessel_gewaehlte_Stromtraeger_gilt_fuer_das_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Ausgangslage: keine Anlage hat gewählt, also gilt die Vorgabe.
            Assert.Equal(0, ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_1017));
            int vorgabe = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_1017);
            Assert.True(vorgabe > 0 && vorgabe != TRAEGER_STROM_ZWEI);

            Waehle(AnlagenId(PROJEKT_1017, ANLAGE_ELEKTROKESSEL_1017), TRAEGER_STROM_ZWEI);

            Assert.Equal(TRAEGER_STROM_ZWEI,
                         ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_1017));
            Assert.Equal(TRAEGER_STROM_ZWEI, StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_1017));
            Assert.Equal(TRAEGER_STROM_ZWEI, Emissionsquelle.StromTraeger(PROJEKT_1017));
        }

        /// <summary>
        /// Die Ordnung der Rangfolge: Die Wärmepumpe steht VOR dem Elektrokessel, der
        /// Elektrokessel vor dem Stromspeicher. Beide Gegenproben laufen am selben
        /// Projekt, dessen drei elektrische Anlagen sich widersprechen dürfen.
        /// </summary>
        [Fact]
        public void Die_Rangfolge_stellt_den_Elektrokessel_hinter_die_Waermepumpe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int kessel = AnlagenId(PROJEKT_1017, ANLAGE_ELEKTROKESSEL_1017);
            int vorgabe = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_1017);
            Assert.True(vorgabe > 0 && vorgabe != TRAEGER_STROM_ZWEI);

            // Kessel gegen Speicher — der Kessel gewinnt.
            Waehle(kessel, TRAEGER_STROM_ZWEI);
            Waehle(AnlagenId(PROJEKT_1017, ANLAGE_SPEICHER_1017), vorgabe);
            Assert.Equal(TRAEGER_STROM_ZWEI,
                         ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_1017));

            // Kessel gegen Wärmepumpe — die Pumpe gewinnt.
            Waehle(AnlagenId(PROJEKT_1017, ANLAGE_WAERMEPUMPE_1017), vorgabe);
            Assert.Equal(vorgabe, ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_1017));
        }

        /// <summary>
        /// Die Gegenprobe zum Gerät: Ein BRENNSTOFFkessel wählt keinen Stromträger,
        /// auch wenn an seiner Anlagenzeile einer steht. Sonst hätte die Zuordnung eines
        /// Gaskessels den Strompreis des Projekts umgestellt.
        /// </summary>
        [Fact]
        public void Ein_Brennstoffkessel_waehlt_keinen_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_GAS));
            int vorgabe = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_GAS);
            Assert.True(vorgabe > 0);

            Waehle(ANLAGE_GASKESSEL, vorgabe);

            Assert.Equal(0, ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_GAS));
            Assert.Equal(vorgabe, StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_GAS));
        }

        // =====================================================================
        // Teil B — eine Wahrheit über die Menge
        // =====================================================================

        /// <summary>
        /// <b>DIE EINE WAHRHEIT.</b> Was der Auflöser als Elektrokessel-Menge ausweist,
        /// ist genau das, was die Simulation auf den Stromzähler gebucht hat: die
        /// Nutzwärme des Kessels. Beide Seiten rufen dieselbe Funktion, und die
        /// Modulzeile bleibt dabei bei einem Brennstoffverbrauch von 0.
        /// </summary>
        [Fact]
        public void Der_Stromeinsatz_des_Elektrokessels_ist_die_Buchung_der_Simulation()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_1017, out fehler), "Lauf gescheitert: " + fehler);

            ErgebnisModel m = SimulationRunner.BaueErgebnis(
                PROJEKT_1017, laeufer.simulation_Waermebedarf, laeufer.simulation_Strombedarf, laeufer.sim);
            ErgebnisHeizkesselModel h = m.Heizkessel;
            Assert.NotNull(h);

            double ausDenModulen = 0;
            foreach (ErgebnisHeizkesselModulModel mo in h.Module)
            {
                Assert.Equal("Strom", mo.Brennstoff);          // nur Elektrokessel im Projekt
                Assert.Equal(0.0, mo.Verbrauch);               // keine Doppelzählung
                ausDenModulen += SimulationSPK.StromeinsatzElektrokesselMwh(
                                     mo.Waerme_Gas, mo.Waerme_Oel);
            }

            Assert.True(ausDenModulen > 0, "Der Elektrokessel hat nichts geleistet.");
            Assert.Equal(h.Stromverbrauch, ausDenModulen, 9);
        }

        /// <summary>
        /// Der Auflöser weist die Menge des gespeicherten Laufs aus — 52,99 MWh des
        /// Laufs 199 —, bewertet sie mit dem Arbeitspreis des Stromträgers und nennt als
        /// Herkunft den Netzbezug.
        /// </summary>
        [Fact]
        public void Der_Aufloeser_weist_den_Stromeinsatz_mit_Preis_und_Herkunft_aus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EndenergieAufloeser a = EndenergieAufloeser.FuerProjekt(PROJEKT_1024);
            Assert.NotNull(a);

            EndenergieAufloeser.Groesse g = a.FuerPosition(
                BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL,
                AnlagenId(PROJEKT_1024, ANLAGE_ELEKTROKESSEL_1024));

            Assert.NotNull(g);
            Assert.Equal(STROMEINSATZ_1024_KWH, g.BedarfKwh, 6);

            double? preis = a.StrompreisJeKwh;
            Assert.True(preis.HasValue, "Der Stromträger führt keinen Arbeitspreis.");
            Assert.True(g.KostenEuro.HasValue);
            Assert.Equal(STROMEINSATZ_1024_KWH * preis.Value, g.KostenEuro.Value, 6);

            Assert.Contains(ANLAGE_ELEKTROKESSEL_1024, g.Basis);
            Assert.Contains("Netzbezug", g.Basis);
        }

        /// <summary>
        /// Dieselbe Menge trägt die Bemessung „je kWh elektrisch" am Elektrokessel;
        /// ein BRENNSTOFFkessel hat sie nicht und bekommt keine erfundene Zahl.
        /// </summary>
        [Fact]
        public void Je_kWh_elektrisch_kennt_den_Elektrokessel_und_nur_ihn()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EndenergieAufloeser a = EndenergieAufloeser.FuerProjekt(PROJEKT_1024);
            double? elektro = a.StromgroesseKwh(BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL,
                                                AnlagenId(PROJEKT_1024, ANLAGE_ELEKTROKESSEL_1024));
            Assert.True(elektro.HasValue);
            Assert.Equal(STROMEINSATZ_1024_KWH, elektro.Value, 6);

            EndenergieAufloeser gas = EndenergieAufloeser.FuerProjekt(PROJEKT_GAS);
            Assert.Null(gas.StromgroesseKwh(BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL,
                                            ANLAGE_GASKESSEL));
        }

        /// <summary>
        /// Die Gruppe „Endenergie je Komponente" des Kostendialogs zeigt den
        /// Elektrokessel mit kWh, € und Herkunft — genau das, was der Auflöser sagt.
        /// </summary>
        [Fact]
        public void Die_Gruppe_Endenergie_zeigt_den_Elektrokessel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenBetriebsstand.Endenergiezeile zeile = null;
            foreach (KostenBetriebsstand.Endenergiezeile z in
                     KostenBetriebsstand.Endenergie(PROJEKT_1024))
                if (z.Komponente.EndsWith(ANLAGE_ELEKTROKESSEL_1024, StringComparison.Ordinal))
                    zeile = z;

            Assert.NotNull(zeile);
            Assert.Equal(STROMEINSATZ_1024_KWH, zeile.BedarfKwh, 6);
            Assert.Equal("52.990 kWh", zeile.BedarfText);
            Assert.True(zeile.KostenEuro.HasValue);
            Assert.EndsWith("€/a", zeile.KostenText);
            Assert.Contains("Netzbezug", zeile.Basis);
        }

        /// <summary>
        /// Der Gaskessel des Projekts 1030 bekommt seine BRENNSTOFF-Endenergie und
        /// nichts aus dem Elektroweg — die beiden Welten bleiben getrennt. Seine
        /// Herkunft nennt den Heizkessel, nicht den Netzbezug; eine Strommenge wäre
        /// dort eine Erfindung, und eine gemeinsame Zahl wäre eine Vermengung.
        ///
        /// <para><b>#363 (19.09.2026):</b> Bis dahin stand hier <c>Assert.Null</c> —
        /// aber nicht, weil der Kessel keine Endenergie hätte, sondern weil der
        /// GESPEICHERTE Lauf dieser Datenbank von vor Befund B-1 stammt und
        /// <c>Verbrauch</c> dort leer ist. Der Auflöser leitet den Einsatz seither aus
        /// Wärme und Nutzungsgrad ab; die Aussage dieses Falls — kein Strom am
        /// Brennstoffkessel — bleibt dieselbe und wird jetzt am Inhalt geprüft statt
        /// an einer 0, die ein Nebeneffekt war.</para>
        /// </summary>
        [Fact]
        public void Der_Brennstoffkessel_bekommt_nichts_aus_dem_Elektroweg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EndenergieAufloeser a = EndenergieAufloeser.FuerProjekt(PROJEKT_GAS);

            EndenergieAufloeser.Groesse g =
                a.FuerPosition(BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL, ANLAGE_GASKESSEL);
            Assert.NotNull(g);
            Assert.True(g.BedarfKwh > 0);
            Assert.DoesNotContain("Netzbezug", g.Basis);

            // Die elektrische Größe bleibt ihm verwehrt — sie hat nur der Elektrokessel.
            Assert.Null(a.StromgroesseKwh(BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL,
                                          ANLAGE_GASKESSEL));
        }

        // =====================================================================
        // Teil C — der Grund, wenn keine Bezugsgröße dasteht
        // =====================================================================

        /// <summary>
        /// Eine Zeile „je kWh elektrisch" am ELEKTROKESSEL bleibt ohne Lauf ohne
        /// Bezugsgröße — und nennt dafür den fehlenden LAUF, nicht das Gewerk. Am
        /// Brennstoffkessel bleibt es beim Gewerk: Ihm fehlt die Größe selbst, und kein
        /// Lauf der Welt brächte sie ihm.
        ///
        /// <para>Die LANDKARTE ohne Anlagenkenntnis bleibt unangetastet — sie füllt
        /// auch die Auswahlliste der Bemessungsarten
        /// (<c>KostenVorlagenCtrl.PasstZuGewerk</c>).</para>
        /// </summary>
        [Fact]
        public void Am_Elektrokessel_nennt_der_Grund_den_fehlenden_Lauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int kessel = AnlagenId(PROJEKT_1017, ANLAGE_ELEKTROKESSEL_1017);
            Assert.True(WirtschaftlichkeitCtrl.IstElektrokesselAnlage(kessel));
            Assert.False(WirtschaftlichkeitCtrl.IstElektrokesselAnlage(ANLAGE_GASKESSEL));

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF,
                         WirtschaftlichkeitCtrl.BasisGrundFuerZeile(
                             DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                             BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL, kessel));

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                         WirtschaftlichkeitCtrl.BasisGrundFuerZeile(
                             DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                             BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL, ANLAGE_GASKESSEL));

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                         WirtschaftlichkeitCtrl.BasisGrund(
                             DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                             BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL));
        }

        /// <summary>
        /// Derselbe Grund auf dem Weg, den der Dialog geht: über die gespeicherte
        /// Position und <see cref="WirtschaftlichkeitCtrl.FrischeBasis"/>. Damit ist
        /// nicht nur die Landkarte geprüft, sondern auch, dass die Anlage der Zeile
        /// überhaupt bis dorthin durchgereicht wird.
        /// </summary>
        [Fact]
        public void Der_Grund_der_Zeile_kommt_bis_in_den_Dialog()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string grund;
            int elektro = Wartungszeile(POSITION_ELEKTRO, PROJEKT_1017,
                                        AnlagenId(PROJEKT_1017, ANLAGE_ELEKTROKESSEL_1017));
            Assert.Null(WirtschaftlichkeitCtrl.FrischeBasis(
                            elektro, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, out grund));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF, grund);

            int gas = Wartungszeile(POSITION_GAS, PROJEKT_GAS, ANLAGE_GASKESSEL);
            Assert.Null(WirtschaftlichkeitCtrl.FrischeBasis(
                            gas, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, out grund));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK, grund);
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>Kennungen zweier Betriebskostenzeilen, die es in der
        /// Testdatenbank nicht gibt — sie entstehen in der Arbeitskopie.</summary>
        private const int POSITION_ELEKTRO = 990000001;
        private const int POSITION_GAS = 990000002;

        /// <summary>Legt eine satzbasierte BETRIEBSzeile „je kWh elektrisch" an der
        /// Anlage an und gibt ihre Kennung zurück.</summary>
        private static int Wartungszeile(int id, int projekt, int anlage)
        {
            Assert.True(anlage > 0, "Anlagenzeile nicht gefunden.");
            object stamm = DataRepository.ExecuteScalar("SELECT MIN(StammID) FROM Tab_Kostenfaktor");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ID, ProjektID, StammID, KomponentenID, KategorieID, " +
                "EingegebenerWert, Nutzungsdauer, Kostenart, Bemessung, Einheitpreis, ID_Anlage) " +
                "VALUES (?, ?, ?, ?, ?, 0.0, 20, ?, ?, 0.05, ?)",
                new DbParam("@id", id),
                new DbParam("@p", projekt),
                new DbParam("@s", Convert.ToInt32(stamm)),
                new DbParam("@k", BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL),
                new DbParam("@kat", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                new DbParam("@art", DbWerte.KOSTENART_BETRIEBSGEBUNDEN),
                new DbParam("@bem", DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH),
                new DbParam("@a", anlage));
            return id;
        }


        private static ProjektEnergietraegerCtrl.AnlagenEintrag Eintrag(
            List<ProjektEnergietraegerCtrl.AnlagenEintrag> liste, string bezeichner)
        {
            foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in liste)
                if (string.Equals(a.Bezeichner, bezeichner, StringComparison.Ordinal)) return a;
            return null;
        }

        /// <summary>Setzt den Trägerverweis EINER Anlagenzeile der Arbeitskopie.</summary>
        private static void Waehle(int anlage, int traeger)
        {
            Assert.True(anlage > 0, "Anlagenzeile nicht gefunden.");
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET ID_Carrier = ? WHERE ID = ?",
                new DbParam("@c", traeger), new DbParam("@a", anlage));
        }

        /// <summary><c>Tab_Energieanlagen.ID</c> zu einem Bezeichner des Projekts.</summary>
        private static int AnlagenId(int projekt, string bezeichner)
        {
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", projekt), new DbParam("@b", bezeichner));
            return dt != null && dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["ID"]) : 0;
        }

    }
}
