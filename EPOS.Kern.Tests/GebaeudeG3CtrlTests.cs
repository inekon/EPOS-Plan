using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Stufe G3, Welle B — die Prüfregeln der drei Controller ohne Datenbank: Neigungsvorgabe,
    /// Azimutpflicht, Wertlisten, Bänder und Pflichtfelder.
    /// </summary>
    public class GebaeudeG3PruefregelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Theory]
        [InlineData("DACH", 0.0)]
        [InlineData("DECKE", 0.0)]
        [InlineData("BODENPLATTE", 180.0)]
        [InlineData("AUSSENWAND", 90.0)]
        [InlineData("FENSTER", 90.0)]
        [InlineData("VORHANGFASSADE", 90.0)]
        [InlineData("SONSTIGES", 90.0)]
        public void Die_Neigung_ohne_Angabe_folgt_der_Bauteilart(string art, double erwartet)
        {
            Assert.Equal(erwartet, GebaeudeZonenCtrl.NeigungVorgabe(art));
        }

        /// <summary>
        /// Die Azimutpflicht: nur an der Außenluft und nur geneigt. Eine Wand ohne Azimut wird
        /// benannt abgelehnt; Dach, Bodenplatte und Bauteile an Erdreich, Zone oder unbeheiztem Raum
        /// brauchen keinen.
        /// </summary>
        [Fact]
        public void Nur_ein_geneigtes_Aussenbauteil_braucht_einen_Azimut()
        {
            Assert.True(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND }));
            Assert.True(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_FENSTER,
                                                                           Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT }));
            Assert.True(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_DACH, Neigung = 35.0 }));
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_DACH }));
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_BODENPLATTE }));
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Neigung = 180.0 }));
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                                                            Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH }));
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_INNENWAND,
                                                                            Randbedingung = DbWerte.RANDBEDINGUNG_ZONE }));
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                                                            Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT }));
        }

        [Fact]
        public void Die_Zonenpruefung_nennt_Zone_und_Bauteil()
        {
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { GueltigeZone() }));
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel>()));

            ZoneModel ohneName = GueltigeZone();
            ohneName.Bezeichner = " ";
            Assert.Equal(string.Format(R.ZONE_MSG_NAME_LEER, 1), GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { ohneName }));

            ZoneModel uebergabe = GueltigeZone();
            uebergabe.Uebergabe_Art = "HEIZKOERPER";
            Assert.Equal(string.Format(R.ZONE_MSG_UEBERGABEART, "Wohnen", "HEIZKOERPER"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { uebergabe }));

            ZoneModel ohneAzimut = GueltigeZone();
            ohneAzimut.Bauteile[0].Azimut = null;
            Assert.Equal(string.Format(R.BAUTEIL_MSG_AZIMUT_FEHLT, "Wand Süd"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { ohneAzimut }));

            ZoneModel ohneFlaeche = GueltigeZone();
            ohneFlaeche.Bauteile[1].Flaeche = 0;
            Assert.Equal(string.Format(R.BAUTEIL_MSG_FLAECHE, "Dach"), GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { ohneFlaeche }));

            ZoneModel keller = GueltigeZone();
            keller.Bauteile[1].Randbedingung = "KELLER";
            Assert.Equal(string.Format(R.BAUTEIL_MSG_RANDBEDINGUNG, "Dach", "KELLER"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { keller }));

            ZoneModel art = GueltigeZone();
            art.Bauteile[1].Bauteilart = "KELLER";
            Assert.Equal(string.Format(R.BAUTEIL_MSG_WERT, "Dach", string.Format(R.BAUTEIL_MSG_BAUTEILART, "KELLER")),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { art }));

            ZoneModel herkunft = GueltigeZone();
            herkunft.Herkunft = "ACCESS";
            Assert.Equal(string.Format(R.ZONE_MSG_WERT, "Wohnen", string.Format(R.BAUTEIL_MSG_HERKUNFT, "ACCESS")),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { herkunft }));
        }

        [Fact]
        public void Baustoff_und_Aufbau_pruefen_Namen_Baender_und_Schichten()
        {
            Assert.Null(BaustoffCtrl.Pruefen(new BaustoffModel { Bezeichner = "Lehm", Lambda = 0.8 }));
            Assert.Equal(R.BAUSTOFF_MSG_NAME_LEER, BaustoffCtrl.Pruefen(new BaustoffModel { Bezeichner = "" }));
            Assert.Equal(string.Format(R.BAUSTOFF_MSG_WERT_BAND, R.KFLT_SP_LAMBDA, 0.0, BaustoffCtrl.LAMBDA_MIN, BaustoffCtrl.LAMBDA_MAX),
                         BaustoffCtrl.Pruefen(new BaustoffModel { Bezeichner = "Null", Lambda = 0.0 }));
            Assert.NotNull(BaustoffCtrl.Pruefen(new BaustoffModel { Bezeichner = "Blei", Rho = 11300 }));
            Assert.NotNull(BaustoffCtrl.Pruefen(new BaustoffModel { Bezeichner = "x", Herkunft = "ACCESS" }));
            Assert.NotNull(BaustoffCtrl.Pruefen(new BaustoffModel { Bezeichner = new string('x', 81) }));

            var aufbau = new BauteilaufbauModel
            {
                Bezeichner = "Wand",
                Schichten = { new BauteilschichtModel { Dicke = 0.2 }, new BauteilschichtModel { Dicke = 0 } }
            };
            Assert.Equal(string.Format(R.BAUTEIL_MSG_SCHICHT_DICKE, 2), BauteilaufbauCtrl.Pruefen(aufbau));
            aufbau.Schichten[1].Dicke = 0.1;
            Assert.Null(BauteilaufbauCtrl.Pruefen(aufbau));
            aufbau.Bauteilart = "KELLER";
            Assert.Equal(string.Format(R.BAUTEIL_MSG_BAUTEILART, "KELLER"), BauteilaufbauCtrl.Pruefen(aufbau));
        }

        internal static ZoneModel GueltigeZone()
        {
            return new ZoneModel
            {
                ID = -1,
                Bezeichner = "Wohnen",
                Bauteile =
                {
                    new BauteilModel { ID = -1, Bezeichner = "Wand Süd", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                       Flaeche = 20, Azimut = 180 },
                    new BauteilModel { ID = -2, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 50 }
                }
            };
        }
    }

    /// <summary>
    /// Stufe G3, Welle B — die drei Controller gegen die Arbeitskopie der Testdatenbank: CRUD mit
    /// Schutz der Auslieferung, <c>CopyFromStamm</c> NULL-erhaltend samt Schicht-Umsetzung (W11),
    /// der Aggregat-Abgleich der Zonen (A6), die Kaskadenmessung A1, Duplizieren und Projekttransfer.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeG3CtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;
        private const int MEHRGEBAEUDE = 1039;

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        // =============================================================================
        //  Baustoff
        // =============================================================================

        [Fact]
        public void Der_Katalog_liest_gefiltert_und_schuetzt_die_Auslieferung()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BaustoffCtrl();

            Assert.Equal(BaustoffSchema.Saat.Count, ctrl.LesenKatalog().Count);
            Assert.Equal(65, ctrl.LesenKatalog(hersteller: "").Count);
            Assert.All(ctrl.LesenKatalog(hersteller: "Xella"), m => Assert.Equal("Xella", m.Hersteller));
            Assert.Equal(BaustoffSchema.Saat.Count(s => s.Gruppe == "Dämmstoffe"), ctrl.LesenKatalog("Dämmstoffe").Count);
            Assert.Contains("Mauerwerk", BaustoffCtrl.Gruppen());
            Assert.Contains("Rigips", BaustoffCtrl.Hersteller());
            Assert.Equal(BaustoffSchema.Saat.Count, BaustoffCtrl.Katalogfilterzeilen().Count);
            Assert.All(BaustoffCtrl.Katalogfilterzeilen(), z => Assert.True(z.Geschuetzt));

            // Ein Satz der Auslieferung laesst sich weder aendern noch loeschen.
            BaustoffModel saat = ctrl.LesenKatalogsatz(1);
            Assert.True(saat.ReadOnly);
            Assert.Equal(DbWerte.HERKUNFT_VORGABE, saat.Herkunft);
            saat.Lambda = 0.9;
            BaustoffCtrl.Ergebnis e = ctrl.KatalogAendern(saat);
            Assert.False(e.Ok);
            Assert.Equal(string.Format(R.BAUSTOFF_MSG_SCHREIBGESCHUETZT, "Kalkzementputz"), e.Meldung);
            Assert.False(ctrl.KatalogLoeschen(1).Ok);
            Assert.Equal(1.0, ctrl.LesenKatalogsatz(1).Lambda);
        }

        [Fact]
        public void Der_Katalog_legt_an_aendert_dupliziert_und_loescht()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BaustoffCtrl();

            var neu = new BaustoffModel { Bezeichner = " Stampflehm ", Gruppe = "Mauerwerk", Lambda = 1.1, Rho = 2000 };
            BaustoffCtrl.Ergebnis e = ctrl.KatalogAnlegen(neu);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(BaustoffSchema.SAAT_ID_GRENZE, e.Id);          // jenseits der Saat
            BaustoffModel gelesen = ctrl.LesenKatalogsatz(e.Id);
            Assert.Equal("Stampflehm", gelesen.Bezeichner);
            Assert.False(gelesen.ReadOnly);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, gelesen.Herkunft);
            Assert.Null(gelesen.Cp);                                    // NULL bleibt NULL
            Assert.Null(gelesen.Hersteller);

            // Derselbe Name, derselbe (fehlende) Hersteller: abgelehnt; mit Hersteller: frei.
            Assert.False(ctrl.KatalogAnlegen(new BaustoffModel { Bezeichner = "Stampflehm" }).Ok);
            Assert.True(ctrl.KatalogAnlegen(new BaustoffModel { Bezeichner = "Stampflehm", Hersteller = "Lehmbau Nord" }).Ok);

            gelesen.Cp = 1000;
            Assert.True(ctrl.KatalogAendern(gelesen).Ok);
            Assert.Equal(1000.0, ctrl.LesenKatalogsatz(e.Id).Cp);

            BaustoffCtrl.Ergebnis kopie = ctrl.KatalogDuplizieren(36, "Mineralwolle eigene");
            Assert.True(kopie.Ok, kopie.Meldung);
            BaustoffModel k = ctrl.LesenKatalogsatz(kopie.Id);
            Assert.False(k.ReadOnly);
            Assert.Equal(0.036, k.Lambda);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, k.Herkunft);
            Assert.Equal(BaustoffSchema.SaatZu(36).Quelle, k.Quelle);

            Assert.True(ctrl.KatalogLoeschen(e.Id).Ok);
            Assert.Null(ctrl.LesenKatalogsatz(e.Id));
            Assert.False(ctrl.KatalogLoeschen(e.Id).Ok);
        }

        /// <summary>Katalog → Projekt über die Fachspalten: NULL bleibt NULL, Herkunft KATALOG, eine Kopie je Name.</summary>
        [Fact]
        public void CopyFromStamm_ist_NULL_erhaltend_und_einmalig()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BaustoffCtrl();
            int stamm = ctrl.KatalogAnlegen(new BaustoffModel
            {
                Bezeichner = "Probestoff", Hersteller = "Probe GmbH", Lambda = 0.05, Quelle = "Datenblatt 1"
            }).Id;

            int kopie = ctrl.CopyFromStamm(stamm, PROJEKT);
            Assert.True(kopie > 0);
            BaustoffModel p = ctrl.LesenProjektsatz(kopie);
            Assert.Equal(PROJEKT, p.ID_Projekt);
            Assert.Equal("Probestoff", p.Bezeichner);
            Assert.Equal("Probe GmbH", p.Hersteller);
            Assert.Equal(0.05, p.Lambda);
            Assert.Null(p.Rho);
            Assert.Null(p.Cp);
            Assert.Null(p.Gruppe);
            Assert.Equal("Datenblatt 1", p.Quelle);
            Assert.Equal(DbWerte.HERKUNFT_KATALOG, p.Herkunft);

            Assert.Equal(kopie, ctrl.CopyFromStamm(stamm, PROJEKT));
            Assert.Single(ctrl.LesenProjekt(PROJEKT));
            Assert.Equal(-1, ctrl.CopyFromStamm(999999, PROJEKT));
        }

        // =============================================================================
        //  Bauteilaufbau
        // =============================================================================

        [Fact]
        public void Der_Aufbau_speichert_Schichten_lueckenlos_und_uebernimmt_Stoffwerte()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BauteilaufbauCtrl();
            BauteilaufbauModel aufbau = Wand();

            BauteilaufbauCtrl.Ergebnis e = ctrl.KatalogSpeichern(aufbau);
            Assert.True(e.Ok, e.Meldung);
            BauteilaufbauModel g = ctrl.LesenKatalogsatz(e.Id);
            Assert.Equal(3, g.Schichten.Count);
            Assert.Equal(new[] { 1, 2, 3 }, g.Schichten.Select(s => s.Reihenfolge));
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, g.Herkunft);
            Assert.False(g.ReadOnly);

            // Schicht 1 traegt den Stoff Kalksandstein 1400 (Id 18) - die fehlenden Werte kommen aus ihm.
            Assert.Equal(18, g.Schichten[0].ID_Baustoff);
            Assert.Equal(0.7, g.Schichten[0].Lambda);
            Assert.Equal(1400.0, g.Schichten[0].Rho);
            Assert.Equal(1000.0, g.Schichten[0].Cp);
            // Schicht 2 ist freie Eingabe, Schicht 3 eine Luftschicht ohne Stoffwerte.
            Assert.Null(g.Schichten[1].ID_Baustoff);
            Assert.Equal(0.035, g.Schichten[1].Lambda);
            Assert.Null(g.Schichten[1].Rho);
            Assert.True(g.Schichten[2].IstLuftschicht);
            Assert.Null(g.Schichten[2].Lambda);
            Assert.Equal(0.36, g.Gesamtdicke, 9);

            // Umordnen und Entfernen: die Reihenfolge bleibt lueckenlos, der Schluessel des Aufbaus bleibt.
            g.Schichten.RemoveAt(2);
            g.Schichten.Reverse();
            Assert.True(ctrl.KatalogSpeichern(g).Ok);
            BauteilaufbauModel h = ctrl.LesenKatalogsatz(e.Id);
            Assert.Equal(e.Id, h.ID);
            Assert.Equal(new[] { 1, 2 }, h.Schichten.Select(s => s.Reihenfolge));
            Assert.Null(h.Schichten[0].ID_Baustoff);
            Assert.Equal(18, h.Schichten[1].ID_Baustoff);
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Bauteilschicht_STAMM WHERE ID_Aufbau = ?", new DbParam("@a", e.Id)));

            // Name vergeben, Schicht ohne Dicke, fremder Stoff: abgelehnt, nichts geschrieben.
            Assert.False(ctrl.KatalogSpeichern(Wand()).Ok);
            BauteilaufbauModel dicke = Wand("Wand 2");
            dicke.Schichten[1].Dicke = 0;
            Assert.Equal(string.Format(R.BAUTEIL_MSG_SCHICHT_DICKE, 2), ctrl.KatalogSpeichern(dicke).Meldung);
            BauteilaufbauModel fremd = Wand("Wand 3");
            fremd.Schichten[0].ID_Baustoff = 999999;
            Assert.Equal(string.Format(R.BAUTEIL_MSG_SCHICHT_BAUSTOFF, 1, 999999), ctrl.KatalogSpeichern(fremd).Meldung);
            Assert.Single(ctrl.LesenKatalog());

            // Die Kaskade nimmt die Schichten mit.
            Assert.True(ctrl.KatalogLoeschen(e.Id).Ok);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteilschicht_STAMM"));
        }

        /// <summary>Ein Katalogaufbau der Auslieferung ist geschützt; ein benutzter Katalogstoff lässt sich nicht löschen.</summary>
        [Fact]
        public void Auslieferung_und_Verwendung_schuetzen_Aufbau_und_Stoff()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BauteilaufbauCtrl();
            int id = ctrl.KatalogSpeichern(Wand()).Id;
            Assert.True(BauteilaufbauCtrl.SchlossSetzen(new[] { id }, true).Ok);

            BauteilaufbauModel g = ctrl.LesenKatalogsatz(id);
            Assert.True(g.ReadOnly);
            Assert.Equal(string.Format(R.BAUTEIL_MSG_AUFBAU_SCHREIBGESCHUETZT, "Wand KS"), ctrl.KatalogSpeichern(g).Meldung);
            Assert.False(ctrl.KatalogLoeschen(id).Ok);

            // Der Stoff 18 steht in einer Katalogschicht - Loeschen nennt die Verwendung.
            var stoffe = new BaustoffCtrl();
            Assert.True(BaustoffCtrl.SchlossSetzen(new[] { 18 }, false).Ok);
            Assert.Equal(string.Format(R.BAUSTOFF_MSG_VERWENDET, "Kalksandstein 1400", 1), stoffe.KatalogLoeschen(18).Meldung);
            Assert.NotNull(stoffe.LesenKatalogsatz(18));

            // Die Registry kennt beide Kataloge samt Datenblock und Verwendungspruefung.
            KatalogDefinition def = KatalogRegistry.Finde("BAUTEILAUFBAU");
            Assert.Equal(SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM, def.Tabelle);
            Assert.Equal(SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM, Assert.Single(def.Datenbloecke).Tabelle);
            Assert.Single(DublettenPruefung.ScanKatalog(def).Saetze);
            Assert.Equal(1, KatalogBereinigung.VerwendungZaehlen(
                Assert.Single(KatalogRegistry.Finde("BAUSTOFF").VerwendungsPruefungen),
                DublettenPruefung.ScanKatalog(KatalogRegistry.Finde("BAUSTOFF")).Saetze.First(s => s.Id == 18), out string f));
            Assert.Null(f);
        }

        /// <summary>
        /// Katalog → Projekt samt Schichten (2.6, W11): Die Stoffe der Schichten reisen mit, und
        /// <c>ID_Baustoff</c> zeigt danach auf die Projektkopie; ein zweiter Aufbau mit demselben
        /// Stoff legt ihn nicht doppelt an.
        /// </summary>
        [Fact]
        public void CopyFromStamm_nimmt_Schichten_und_Stoffe_mit()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BauteilaufbauCtrl();
            int stamm = ctrl.KatalogSpeichern(Wand()).Id;
            int stamm2 = ctrl.KatalogSpeichern(Wand("Wand KS 2")).Id;

            int kopie = ctrl.CopyFromStamm(stamm, PROJEKT);
            Assert.True(kopie > 0);
            BauteilaufbauModel p = ctrl.LesenProjektsatz(kopie);
            Assert.Equal(PROJEKT, p.ID_Projekt);
            Assert.Equal(DbWerte.HERKUNFT_KATALOG, p.Herkunft);
            Assert.Equal("Außenwand", p.Beschreibung);
            Assert.Equal(DbWerte.BAUTEILART_AUSSENWAND, p.Bauteilart);
            Assert.Equal(new[] { 1, 2, 3 }, p.Schichten.Select(s => s.Reihenfolge));

            int projektstoff = p.Schichten[0].ID_Baustoff.Value;
            Assert.NotEqual(18, projektstoff);
            BaustoffModel stoff = new BaustoffCtrl().LesenProjektsatz(projektstoff);
            Assert.Equal(PROJEKT, stoff.ID_Projekt);
            Assert.Equal("Kalksandstein 1400", stoff.Bezeichner);
            Assert.Equal(DbWerte.HERKUNFT_KATALOG, stoff.Herkunft);
            Assert.Equal(0.7, p.Schichten[0].Lambda);
            Assert.Null(p.Schichten[1].ID_Baustoff);
            Assert.True(p.Schichten[2].IstLuftschicht);

            Assert.Equal(kopie, ctrl.CopyFromStamm(stamm, PROJEKT));
            int kopie2 = ctrl.CopyFromStamm(stamm2, PROJEKT);
            Assert.Equal(projektstoff, ctrl.LesenProjektsatz(kopie2).Schichten[0].ID_Baustoff);
            Assert.Single(new BaustoffCtrl().LesenProjekt(PROJEKT));

            // Der Leseweg je Projekt: zwei Aufbauten, je drei Schichten in Reihenfolge.
            List<BauteilaufbauModel> alle = ctrl.LesenJeProjekt(PROJEKT);
            Assert.Equal(2, alle.Count);
            Assert.All(alle, a => Assert.Equal(new[] { 1, 2, 3 }, a.Schichten.Select(s => s.Reihenfolge)));
        }

        // =============================================================================
        //  Zonen und Bauteile (A6)
        // =============================================================================

        [Fact]
        public void Das_Aggregat_legt_an_und_liest_verlustfrei_zurueck()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            int aufbau = ProjektAufbau();
            List<ZoneModel> zonen = ZweiZonen(aufbau);

            GebaeudeZonenCtrl.Ergebnis e = ctrl.SpeichernJeGebaeude(GEBAEUDE, zonen);
            Assert.True(e.Ok, e.Meldung);
            Assert.All(zonen, z => Assert.True(z.ID > 0));
            Assert.All(zonen.SelectMany(z => z.Bauteile), b => Assert.True(b.ID > 0));

            List<ZoneModel> g = ctrl.LesenJeGebaeude(GEBAEUDE);
            Assert.Equal(new[] { "Wohnen", "Keller" }, g.Select(z => z.Bezeichner));
            Assert.Equal(new[] { 1, 2 }, g.Select(z => z.Rang));
            ZoneModel w = g[0];
            Assert.Equal(zonen[0].ID, w.ID);
            Assert.Equal(GEBAEUDE, w.ID_Gebaeude);
            Assert.Equal(120.0, w.Nutzflaeche);
            Assert.True(w.IstBeheizt);
            Assert.Equal(21.0, w.Raumsolltemperatur_Tag);
            Assert.Null(w.Raumsolltemperatur_Ferien);
            Assert.Equal(26.0, w.Kuehl_Sollwert);
            Assert.True(w.Kuehlung_Aktiv);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, w.Uebergabe_Art);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, w.Herkunft);
            Assert.False(g[1].IstBeheizt);
            Assert.Null(g[1].Kuehlung_Aktiv);                          // NULL = Wert des Gebaeudes
            Assert.Equal(new[] { "Wand Süd", "Dach" }, w.Bauteile.Select(b => b.Bezeichner));
            Assert.Equal(new[] { 1, 2 }, w.Bauteile.Select(b => b.Rang));
            Assert.Equal(aufbau, w.Bauteile[0].ID_Aufbau);
            Assert.Equal(180.0, w.Bauteile[0].Azimut);
            Assert.Null(w.Bauteile[1].Azimut);
            Assert.Equal(DbWerte.RANDBEDINGUNG_ERDREICH, Assert.Single(g[1].Bauteile).Randbedingung);
        }

        /// <summary>
        /// Der Abgleich über die Ids: Umordnen, Entfernen, Verschieben, Ändern, Anlegen — die
        /// Schlüssel bleiben, der Rang wird lückenlos, und ein verschobenes Bauteil überlebt das
        /// Entfernen seiner alten Zone.
        /// </summary>
        [Fact]
        public void Der_Abgleich_haelt_die_Schluessel_und_setzt_den_Rang_lueckenlos()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, ZweiZonen(ProjektAufbau())).Ok);
            List<ZoneModel> g = ctrl.LesenJeGebaeude(GEBAEUDE);
            ZoneModel wohnen = g[0], keller = g[1];
            int wand = wohnen.Bauteile[0].ID, dach = wohnen.Bauteile[1].ID, boden = keller.Bauteile[0].ID;

            // Keller vor Wohnen, das Dach faellt, die Bodenplatte wandert in "Wohnen", der Keller
            // bekommt eine neue Innenwand, und die Wand aendert ihre Flaeche.
            wohnen.Bauteile.RemoveAt(1);
            wohnen.Bauteile.Add(keller.Bauteile[0]);
            keller.Bauteile.Clear();
            keller.Bauteile.Add(new BauteilModel { ID = -7, Bezeichner = "Kellerwand", Bauteilart = DbWerte.BAUTEILART_INNENWAND,
                                                   Flaeche = 12, Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT });
            wohnen.Bauteile[0].Flaeche = 22.5;
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { keller, wohnen }).Ok);

            List<ZoneModel> h = ctrl.LesenJeGebaeude(GEBAEUDE);
            Assert.Equal(new[] { keller.ID, wohnen.ID }, h.Select(z => z.ID));
            Assert.Equal(new[] { 1, 2 }, h.Select(z => z.Rang));
            Assert.Equal(new[] { wand, boden }, h[1].Bauteile.Select(b => b.ID));
            Assert.Equal(new[] { 1, 2 }, h[1].Bauteile.Select(b => b.Rang));
            Assert.Equal(22.5, h[1].Bauteile[0].Flaeche);
            Assert.Equal("Kellerwand", Assert.Single(h[0].Bauteile).Bezeichner);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil WHERE ID = ?", new DbParam("@id", dach)));

            // Die Zone "Keller" faellt; die Bodenplatte in "Wohnen" bleibt, die Kellerwand geht mit.
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { h[1] }).Ok);
            ZoneModel rest = Assert.Single(ctrl.LesenJeGebaeude(GEBAEUDE));
            Assert.Equal(wohnen.ID, rest.ID);
            Assert.Equal(1, rest.Rang);
            Assert.Equal(new[] { wand, boden }, rest.Bauteile.Select(b => b.ID));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil"));

            // Eine leere Liste nimmt alle Zonen - das Gebaeude rechnet wieder den Klassenweg.
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel>()).Ok);
            Assert.Empty(ctrl.LesenJeGebaeude(GEBAEUDE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil"));
        }

        /// <summary>Fremde Ids, fremde Aufbauten, ein fehlendes Gebäude und ein Prüfbefund: abgelehnt, nichts geschrieben.</summary>
        [Fact]
        public void Der_Abgleich_weist_Fremdes_ab_bevor_er_schreibt()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(ctrl.SpeichernJeGebaeude(10642, new List<ZoneModel> { GebaeudeG3PruefregelTests.GueltigeZone() }).Ok);
            ZoneModel fremdeZone = Assert.Single(ctrl.LesenJeGebaeude(10642));

            GebaeudeZonenCtrl.Ergebnis e = ctrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { fremdeZone });
            Assert.Equal(string.Format(R.ZONE_MSG_FREMD, fremdeZone.ID), e.Meldung);

            ZoneModel mitFremdemBauteil = GebaeudeG3PruefregelTests.GueltigeZone();
            mitFremdemBauteil.Bauteile[0].ID = fremdeZone.Bauteile[0].ID;
            Assert.Equal(string.Format(R.BAUTEIL_MSG_FREMD, "Wohnen", fremdeZone.Bauteile[0].ID),
                         ctrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { mitFremdemBauteil }).Meldung);

            // Ein Aufbau des Projekts 1039 gehoert nicht zum Gebaeude von Projekt 1007.
            int fremderAufbau = new BauteilaufbauCtrl().ProjektSpeichern(MEHRGEBAEUDE, WandOhneStoff()).Id;
            Assert.True(fremderAufbau > 0);
            ZoneModel mitFremdemAufbau = GebaeudeG3PruefregelTests.GueltigeZone();
            mitFremdemAufbau.Bauteile[0].ID_Aufbau = fremderAufbau;
            Assert.Equal(string.Format(R.BAUTEIL_MSG_AUFBAU_FREMD, "Wand Süd", fremderAufbau),
                         ctrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { mitFremdemAufbau }).Meldung);

            Assert.Equal(string.Format(R.ZONE_MSG_GEBAEUDE_FEHLT, 999999),
                         ctrl.SpeichernJeGebaeude(999999, new List<ZoneModel> { GebaeudeG3PruefregelTests.GueltigeZone() }).Meldung);

            ZoneModel ohneAzimut = GebaeudeG3PruefregelTests.GueltigeZone();
            ohneAzimut.Bauteile[0].Azimut = null;
            Assert.False(ctrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { ohneAzimut }).Ok);

            Assert.Empty(ctrl.LesenJeGebaeude(GEBAEUDE));
            Assert.Single(ctrl.LesenJeGebaeude(10642));
        }

        /// <summary>
        /// Der Leseweg je Projekt (2.9): zwei Abfragen über das ganze Projekt, je Gebäude die Zonen
        /// in Rangfolge; ein Gebäude ohne Zone fehlt. Ein Aufbau, auf den ein Bauteil zeigt, lässt
        /// sich nicht löschen.
        /// </summary>
        [Fact]
        public void Der_Leseweg_je_Projekt_ordnet_Zonen_ihren_Gebaeuden_zu()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            int aufbau = new BauteilaufbauCtrl().ProjektSpeichern(MEHRGEBAEUDE, WandOhneStoff()).Id;
            Assert.True(aufbau > 0);
            Assert.True(ctrl.SpeichernJeGebaeude(10642, ZweiZonen(aufbau)).Ok);
            Assert.True(ctrl.SpeichernJeGebaeude(10644, new List<ZoneModel> { GebaeudeG3PruefregelTests.GueltigeZone() }).Ok);

            Dictionary<int, List<ZoneModel>> je = ctrl.LesenJeProjekt(MEHRGEBAEUDE);
            Assert.Equal(new[] { 10642, 10644 }, je.Keys.OrderBy(k => k));
            Assert.Equal(new[] { "Wohnen", "Keller" }, je[10642].Select(z => z.Bezeichner));
            Assert.Equal(2, je[10642][0].Bauteile.Count);
            Assert.Single(je[10644]);
            Assert.Empty(ctrl.LesenJeProjekt(PROJEKT));

            BauteilaufbauCtrl.Ergebnis loeschen = new BauteilaufbauCtrl().ProjektLoeschen(aufbau);
            Assert.Equal(string.Format(R.BAUTEIL_MSG_AUFBAU_VERWENDET, "Wand KS", 1), loeschen.Meldung);
        }

        // =============================================================================
        //  A1 - die Kaskadenmessung (Softwarearchitektur W15)
        // =============================================================================

        /// <summary>
        /// <b>A1, beide Fälle.</b> Nach dem LÖSCHEN eines Gebäudes aus dem Projekt sind seine Zonen
        /// und Bauteile weg (Kaskade), die der Nachbargebäude stehen. Nach einem GEWÖHNLICHEN
        /// Speichern der Gebäudeliste — Startseite und Assistent gehen denselben Abgleich — stehen
        /// Zonen und Bauteile unverändert, samt ihren Schlüsseln: Die Zeile in <c>Tab_Gebaeude</c>
        /// wird nicht gelöscht und neu angelegt.
        /// </summary>
        [Fact]
        public void A1_Loeschen_raeumt_die_Zonen_ab_gewoehnliches_Speichern_nicht()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            foreach (int g in new[] { 10642, 10643, 10644 })
                Assert.True(ctrl.SpeichernJeGebaeude(g, new List<ZoneModel> { GebaeudeG3PruefregelTests.GueltigeZone() }).Ok);
            string vorher = Fingerabdruck(MEHRGEBAEUDE);

            // Gewoehnliches Speichern: die Liste unveraendert, dann mit geaenderter Flaeche.
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(MEHRGEBAEUDE);
            Assert.Equal(3, liste.Count);
            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(MEHRGEBAEUDE, liste);
            Assert.True(ok, meldung);
            Assert.Equal(vorher, Fingerabdruck(MEHRGEBAEUDE));

            liste = Z_ProjGebCtrl.LiesProjekt(MEHRGEBAEUDE);
            liste[0].Wohnflaeche += 10;
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(MEHRGEBAEUDE, liste).Gelungen);
            Assert.Equal(vorher, Fingerabdruck(MEHRGEBAEUDE));

            // Derselbe Abgleich im Vorgang des Assistenten (BEARBEITEN-Zweig).
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.True(new WizardCtrl().Schreibe_Projekt_ZuordungGebäude(MEHRGEBAEUDE, Z_ProjGebCtrl.LiesProjekt(MEHRGEBAEUDE), v));
                v.Commit();
            }
            Assert.Equal(vorher, Fingerabdruck(MEHRGEBAEUDE));

            // Loeschen: das mittlere Gebaeude faellt, seine Zone und ihre Bauteile mit.
            Assert.True(new WizardCtrl().Del_Projekt_ZuordungGebäude(MEHRGEBAEUDE, 10643));
            Assert.Empty(ctrl.LesenJeGebaeude(10643));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude = 10643"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil b WHERE NOT EXISTS (SELECT 1 FROM Tab_Zone z WHERE z.ID = b.ID_Zone)"));
            Assert.Equal(new[] { 10642, 10644 }, ctrl.LesenJeProjekt(MEHRGEBAEUDE).Keys.OrderBy(k => k));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil"));
        }

        /// <summary>
        /// Ein gelöschtes Projekt nimmt Gebäude, Zonen und Bauteile mit (Kaskade über
        /// <c>Tab_Projekt</c> → <c>Tab_Gebaeude</c>) und ebenso seine Baustoffe, Aufbauten und
        /// Schichten (Projektfremdschlüssel der Hausregel); der Katalog bleibt.
        /// </summary>
        [Fact]
        public void A1_Ein_geloeschtes_Projekt_nimmt_seine_Zonen_und_Aufbauten_mit()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, ZweiZonen(ProjektAufbau())).Ok);
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Zone"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Bauteilaufbau"));

            ProjektCtrl.LoeschenMitVorarbeiten(PROJEKT, PROJEKTNAME);
            foreach (string t in new[] { "Tab_Zone", "Tab_Bauteil", "Tab_Bauteilaufbau", "Tab_Bauteilschicht", "Tab_Baustoff" })
                Assert.True(Zahl("SELECT COUNT(*) FROM " + t) == 0, t + " traegt nach dem Loeschen des Projekts noch Zeilen.");
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Bauteilaufbau_STAMM"));
            Assert.Equal((long)BaustoffSchema.Saat.Count, Zahl("SELECT COUNT(*) FROM Tab_Baustoff_STAMM"));
        }

        // =============================================================================
        //  Duplizieren und Projekttransfer (2.6)
        // =============================================================================

        /// <summary>
        /// „Projekt mit Zone und Aufbau duplizieren" (W19): Zonen, Bauteile, Aufbauten, Schichten und
        /// Baustoffe reisen mit und zeigen auf die Kopien des NEUEN Projekts — auch die Schicht mit
        /// freier Eingabe (ID_Baustoff NULL) und das Bauteil ohne Aufbau, die ein Filter über die
        /// falsche Spalte verlöre.
        /// </summary>
        [Fact]
        public void Projekt_mit_Zone_und_Aufbau_duplizieren()
        {
            if (!_db.Vorhanden) return;
            int aufbau = ProjektAufbau();
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, ZweiZonen(aufbau)).Ok);
            string quelle = Fingerabdruck(PROJEKT);

            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " G3");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");

            PruefeKopie(neu, aufbau);
            Assert.Equal(quelle, Fingerabdruck(PROJEKT));
            Assert.Equal(Fingerabdruck(PROJEKT, ohneIds: true), Fingerabdruck(neu, ohneIds: true));
        }

        /// <summary>
        /// Der Projekttransfer erbt die Tabellenmenge aus <c>ErmittlePlan</c> (2.6): ein Projekt mit
        /// Zonen ausgeben und einlesen — Zonen-, Bauteil-, Aufbau-, Schicht- und Baustoffzeilen
        /// kommen vollständig und zeigen auf die Kopien des Zielprojekts.
        /// </summary>
        [Fact]
        public void Projekttransfer_traegt_Zonen_Aufbauten_und_Baustoffe()
        {
            if (!_db.Vorhanden) return;
            int aufbau = ProjektAufbau();
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, ZweiZonen(aufbau)).Ok);

            var plan = new ProjektDuplizierenCtrl().ErmittlePlan().ToDictionary(s => s.Tabelle, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})", plan[SchemaKatalog.TAB_ZONE].Filter);
            Assert.StartsWith("ID_Zone IN (SELECT ID FROM Tab_Zone", plan[SchemaKatalog.TAB_BAUTEIL].Filter);
            Assert.StartsWith("ID_Aufbau IN (SELECT ID FROM Tab_Bauteilaufbau", plan[SchemaKatalog.TAB_BAUTEILSCHICHT].Filter);
            Assert.Equal("[ID_Projekt] = {0}", plan[SchemaKatalog.TAB_BAUSTOFF].Filter);
            Assert.False(plan.ContainsKey(SchemaKatalog.TAB_BAUSTOFF_STAMM));

            string ordner = Path.Combine(Path.GetTempPath(), "epos-g3-transfer-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "g3.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Laurentiuskirche Transfer", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeKopie(neu, aufbau);
                Assert.Equal(Fingerabdruck(PROJEKT, ohneIds: true), Fingerabdruck(neu, ohneIds: true));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static BauteilaufbauModel Wand(string name = "Wand KS")
        {
            return new BauteilaufbauModel
            {
                Bezeichner = name,
                Beschreibung = "Außenwand",
                Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                Schichten =
                {
                    new BauteilschichtModel { ID_Baustoff = 18, Dicke = 0.175 },
                    new BauteilschichtModel { Dicke = 0.165, Lambda = 0.035 },
                    new BauteilschichtModel { Dicke = 0.02, IstLuftschicht = true }
                }
            };
        }

        /// <summary>Ein Aufbau ohne Katalogstoff - auf der Projektseite speicherbar, ohne Projektstoff.</summary>
        private static BauteilaufbauModel WandOhneStoff()
        {
            BauteilaufbauModel w = Wand();
            w.Schichten[0].ID_Baustoff = null;
            w.Schichten[0].Lambda = 0.7;
            return w;
        }

        /// <summary>Ein Projektaufbau des Projekts 1007 aus einem Katalogaufbau — mit Projektstoff und freier Schicht.</summary>
        private static int ProjektAufbau()
        {
            var ctrl = new BauteilaufbauCtrl();
            int stamm = ctrl.KatalogSpeichern(Wand()).Id;
            int kopie = ctrl.CopyFromStamm(stamm, PROJEKT);
            Assert.True(kopie > 0);
            return kopie;
        }

        private static List<ZoneModel> ZweiZonen(int aufbau)
        {
            return new List<ZoneModel>
            {
                new ZoneModel
                {
                    ID = -1, Bezeichner = "Wohnen", Nutzflaeche = 120, Raumsolltemperatur_Tag = 21,
                    Kuehl_Sollwert = 26, Kuehlung_Aktiv = true, Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR,
                    Herkunft = DbWerte.HERKUNFT_MANUELL,
                    Bauteile =
                    {
                        new BauteilModel { ID = -1, Bezeichner = "Wand Süd", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                           Flaeche = 20, Azimut = 180, ID_Aufbau = aufbau },
                        new BauteilModel { ID = -2, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 60,
                                           U_Wert = 0.2 }
                    }
                },
                new ZoneModel
                {
                    ID = -2, Bezeichner = "Keller", IstBeheizt = false,
                    Bauteile =
                    {
                        new BauteilModel { ID = -3, Bezeichner = "Bodenplatte", Bauteilart = DbWerte.BAUTEILART_BODENPLATTE,
                                           Flaeche = 60, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH }
                    }
                }
            };
        }

        /// <summary>Prüft die Kopie eines Projekts mit <see cref="ZweiZonen"/> und <see cref="ProjektAufbau"/>.</summary>
        private static void PruefeKopie(int neu, int quellAufbau)
        {
            int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", neu)), CultureInfo.InvariantCulture);
            Assert.NotEqual(GEBAEUDE, gebaeude);
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(gebaeude);
            Assert.Equal(new[] { "Wohnen", "Keller" }, zonen.Select(z => z.Bezeichner));
            Assert.Equal(3, zonen.Sum(z => z.Bauteile.Count));

            BauteilaufbauModel aufbau = Assert.Single(new BauteilaufbauCtrl().LesenJeProjekt(neu));
            Assert.NotEqual(quellAufbau, aufbau.ID);
            Assert.Equal(aufbau.ID, zonen[0].Bauteile[0].ID_Aufbau);          // zeigt auf die Kopie
            Assert.Null(zonen[0].Bauteile[1].ID_Aufbau);                      // Bauteil ohne Aufbau reist mit
            Assert.Equal(3, aufbau.Schichten.Count);                          // auch die freie Schicht
            Assert.Null(aufbau.Schichten[1].ID_Baustoff);

            BaustoffModel stoff = Assert.Single(new BaustoffCtrl().LesenProjekt(neu));
            Assert.Equal(stoff.ID, aufbau.Schichten[0].ID_Baustoff);          // zeigt auf die Kopie
            Assert.Equal("Kalksandstein 1400", stoff.Bezeichner);
        }

        /// <summary>
        /// Ein Fingerabdruck der G3-Zeilen eines Projekts — Zonen und Bauteile samt Ids, Rang und
        /// Werten; mit <paramref name="ohneIds"/> nur die Inhalte (für den Vergleich mit einer Kopie).
        /// </summary>
        private static string Fingerabdruck(int projekt, bool ohneIds = false)
        {
            var teile = new List<string>();
            foreach (KeyValuePair<int, List<ZoneModel>> g in new GebaeudeZonenCtrl().LesenJeProjekt(projekt).OrderBy(k => k.Key))
                foreach (ZoneModel z in g.Value)
                {
                    teile.Add((ohneIds ? "" : g.Key + "/" + z.ID + "/") + z.Rang + ":" + z.Bezeichner + ":" + z.Nutzflaeche + ":" +
                              z.Kuehlung_Aktiv + ":" + z.Uebergabe_Art);
                    foreach (BauteilModel b in z.Bauteile)
                        teile.Add((ohneIds ? "" : b.ID + "/" + b.ID_Zone + "/") + b.Rang + ":" + b.Bezeichner + ":" + b.Bauteilart +
                                  ":" + b.Flaeche.ToString(CultureInfo.InvariantCulture) + ":" + b.Azimut + ":" + b.Randbedingung +
                                  ":" + (b.ID_Aufbau.HasValue ? (ohneIds ? "A" : b.ID_Aufbau.ToString()) : "-"));
                }
            foreach (BauteilaufbauModel a in new BauteilaufbauCtrl().LesenJeProjekt(projekt))
            {
                teile.Add((ohneIds ? "" : a.ID + "/") + a.Bezeichner + ":" + a.Herkunft);
                foreach (BauteilschichtModel s in a.Schichten)
                    teile.Add(s.Reihenfolge + ":" + s.Dicke.ToString(CultureInfo.InvariantCulture) + ":" + s.Lambda + ":" +
                              (s.ID_Baustoff.HasValue ? (ohneIds ? "S" : s.ID_Baustoff.ToString()) : "-"));
            }
            return string.Join("|", teile);
        }
    }
}
