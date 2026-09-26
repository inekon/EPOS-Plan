using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G4b, Welle A — der Schreibweg des Bauteilvorschlags</b> über eine Arbeitskopie der
    /// Testdatenbank: Aufbauten samt Schichten, Zone und Bauteile in EINEM Vorgang für eine
    /// vorhandene Projektkopie; zurückgelesen über den Leser des Laufs
    /// (<see cref="GebaeudeZonenanschluss"/>) rechnet das Gebäude den Bauteilweg. Ein Fehlerfall
    /// schreibt nichts — auch einer mitten im Vorgang. Dazu die Auskunft Bauteilweg gegen
    /// Klassenweg desselben Imports.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeBauteilvorschlagDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private const string PROBE = "gbxml_haus_si.xml";

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeBauteilvorschlagDatenbankTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>Das erste Gebäude des Projekts so, wie Lauf und Auskunft es lesen — samt Zonen.</summary>
        private static ProjektGebaeudeModel Zeile(int idProjekt)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel item = ctrl.items[0];
            item.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            return item;
        }

        private static SimulationWaermebedarf NeueRechnung(int idProjekt)
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            return sim;
        }

        private static long Zahl(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilenzahl der vier Tabellen, in die der Vorschlag schreibt.</summary>
        private static long[] Bestand()
            => new[] { Zahl(ZonenSchema.TAB_ZONE), Zahl(ZonenSchema.TAB_BAUTEIL), Zahl(BauteilaufbauSchema.TAB_AUFBAU), Zahl(BauteilaufbauSchema.TAB_SCHICHT) };

        // =====================================================================
        //  Schreiben, Zurücklesen, Rechnen
        // =====================================================================

        [Fact]
        public void Der_Vorschlag_wird_in_einem_Vorgang_geschrieben_und_rechnet_den_Bauteilweg()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null);
            Assert.False(v.Abgelehnt);

            // Das Projekt führt schon einen Aufbau „Flachdach": der importierte wird ergänzt, nicht verworfen.
            var aufbauCtrl = new BauteilaufbauCtrl();
            var fremd = new BauteilaufbauModel { Bezeichner = "Flachdach", Herkunft = DbWerte.HERKUNFT_MANUELL };
            fremd.Schichten.Add(new BauteilschichtModel { Dicke = 0.2, Lambda = 2.0, Rho = 2400.0, Cp = 1000.0 });
            Assert.True(aufbauCtrl.ProjektSpeichern(PROJEKT, fremd).Ok);

            long[] vorher = Bestand();
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);
            Assert.Null(e.Befund);
            Assert.True(e.IdZone > 0);
            long[] nachher = Bestand();
            int schichten = v.Aufbauten.Sum(a => a.Aufbau.Schichten.Count);
            Assert.Equal(17, schichten);
            Assert.Equal(new[] { vorher[0] + 1, vorher[1] + 34, vorher[2] + 6, vorher[3] + schichten }, nachher);

            // Der Vorschlag selbst bleibt, wie er war.
            Assert.Equal(-1, v.Zone.ID);
            Assert.All(v.Zone.Bauteile, b => Assert.True(b.ID < 0));
            Assert.Equal("Flachdach", v.Aufbauten[1].Aufbau.Bezeichner);

            // Zurücklesen — die Zeilen der Zone.
            ZoneModel zone = Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude));
            Assert.Equal(e.IdZone, zone.ID);
            Assert.Equal(1, zone.Rang);
            Assert.Equal(v.Zone.Bezeichner, zone.Bezeichner);
            Assert.Equal(120.0, zone.Nutzflaeche);
            Assert.Equal(300.0, zone.Volumen);
            Assert.Equal(2.5, zone.Raumhoehe);
            Assert.True(zone.IstBeheizt);
            Assert.Equal(DbWerte.HERKUNFT_GBXML, zone.Herkunft);
            Assert.Equal("geb-1", zone.Quellkennung);
            Assert.Equal(v.Zeilen.Count, zone.Bauteile.Count);
            List<BauteilaufbauModel> aufbauten = aufbauCtrl.LesenJeProjekt(PROJEKT).Where(a => a.Quelle == PROBE).ToList();
            var jeId = aufbauten.ToDictionary(a => a.ID);
            for (int i = 0; i < zone.Bauteile.Count; i++)
            {
                BauteilModel ist = zone.Bauteile[i], soll = v.Zeilen[i].Bauteil;
                Assert.Equal(i + 1, ist.Rang);
                Assert.Equal(soll.Bezeichner, ist.Bezeichner);
                Assert.Equal(soll.Bauteilart, ist.Bauteilart);
                Assert.Equal(soll.Flaeche, ist.Flaeche);
                Assert.Equal(soll.U_Wert, ist.U_Wert);
                Assert.Equal(soll.g_Wert, ist.g_Wert);
                Assert.Equal(soll.Neigung, ist.Neigung);
                Assert.Equal(soll.Azimut, ist.Azimut);
                Assert.Equal(soll.Randbedingung, ist.Randbedingung);
                Assert.Equal(soll.Herkunft, ist.Herkunft);
                Assert.Equal(soll.Quellkennung, ist.Quellkennung);
                Assert.Equal(soll.ID_Aufbau.HasValue, ist.ID_Aufbau.HasValue);
                if (!ist.ID_Aufbau.HasValue) continue;
                // Die abgebildete ID_Aufbau zeigt auf dieselben Schichten wie der vorläufige Aufbau.
                BauteilaufbauModel sollAufbau = v.AufbautenJeId[soll.ID_Aufbau.Value];
                BauteilaufbauModel istAufbau = jeId[ist.ID_Aufbau.Value];
                Assert.Equal(sollAufbau.Schichten.Select(s => (s.Dicke, s.Lambda, s.Rho, s.Cp)),
                             istAufbau.Schichten.Select(s => (s.Dicke, s.Lambda, s.Rho, s.Cp)));
                Assert.Equal(Enumerable.Range(1, istAufbau.Schichten.Count), istAufbau.Schichten.Select(s => s.Reihenfolge));
            }

            // Die Aufbauten: Projektkopien mit Herkunft, Quelle und Quellkennung; der vergebene Name ergänzt.
            Assert.Equal(6, aufbauten.Count);
            Assert.All(aufbauten, a =>
            {
                Assert.Equal(PROJEKT, a.ID_Projekt);
                Assert.Equal(DbWerte.HERKUNFT_GBXML, a.Herkunft);
                Assert.StartsWith("kon-", a.Quellkennung, StringComparison.Ordinal);
            });
            Assert.Contains(aufbauten, a => a.Bezeichner == "Flachdach (2)");
            Assert.Contains(e.Aufbauten, a => a.Bezeichner == "Flachdach (2)");

            // Die Paarungen mit den neuen Kennungen — sie passen in die Herkunftsablage (Welle B).
            Assert.Equal(3, e.Zuordnungen.Count(z => z.Ziel == ImportZiel.Zone && z.ZielId == e.IdZone && z.Quelltyp == "Space"));
            Assert.Equal(zone.Bauteile.Select(b => (int?)b.ID), e.Zuordnungen.Where(z => z.Ziel == ImportZiel.Bauteil).Select(z => z.ZielId));
            Assert.Equal(aufbauten.Select(a => a.ID).OrderBy(x => x),
                         e.Zuordnungen.Where(z => z.Ziel == ImportZiel.Aufbau).Select(z => z.ZielId.Value).OrderBy(x => x));
            Assert.Null(GebaeudeImportCtrl.Pruefen(ablauf.Quelle, e.Zuordnungen));

            // Über den Leser des Laufs: die Zone hängt am Gebäude und ist lesbar.
            ProjektGebaeudeModel mit = Zeile(PROJEKT);
            GebaeudeZonensatz satz = Assert.Single(mit.Zonen);
            Assert.Null(satz.Lesefehler);
            Assert.Equal(e.IdZone, satz.ZonenId);
            Assert.Equal(34, satz.Bauteile.Count);
            // Mit Schichten: zehn Außenwände, das Dach, zwei Kellerdecken und die sechs Innenseiten.
            Assert.Equal(19, satz.Bauteile.Count(b => b.HatSchichten));
            Assert.Equal(120.0, satz.Nutzflaeche_M2);

            // Der Lauf: HeizwaermeEinesGebaeudes rechnet den Bauteilweg ohne Fehler.
            SimulationWaermebedarf sim = NeueRechnung(PROJEKT);
            var werte = new double[8760];
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(mit, 0, werte));
            Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Hinweise));
            GebaeudeModellErgebnis erg = sim.GebaeudeErgebnisse.Ergebnis(0);
            Assert.Equal(1.0, erg.Skalierungsfaktor);
            Assert.True(erg.JahresheizwaermeMwh > 0.0);
            GebaeudeModellEingang eingang = GebaeudeModellEingang.Bauen(mit, sim.Kalender.Gemeinsam.SolarOrtszeit, sim.Kalender.Gemeinsam.WochenendeOrtszeit,
                sim.Kalender.Gemeinsam.Laengengrad, sim.Kalender.Gemeinsam.Breitengrad, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, sim.KuehlbetriebProjekt, null);
            Assert.True(eingang.Bauteilweg);
            Assert.Equal(Gruppenweg.Bauteilweg, eingang.Parameter.WegAussen);
            Assert.Equal(Gruppenweg.Bauteilweg, eingang.Parameter.WegInnen);
            Assert.Equal(145.0, eingang.Parameter.A_IW_M2, 9);
        }

        [Fact]
        public void Vorschlag_und_Herkunft_lassen_sich_in_einem_Vorgang_schreiben()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null);
            long quellenVorher = Zahl(ImportzuordnungSchema.TAB_QUELLE);
            long paarungenVorher = Zahl(ImportzuordnungSchema.TAB_ZUORDNUNG);

            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(g.ID_Gebaeude, ablauf.Quelle, e.Zuordnungen, vorgang);
                Assert.True(h.Ok, h.Meldung);
                vorgang.Commit();
            }
            Assert.Equal(quellenVorher + 1, Zahl(ImportzuordnungSchema.TAB_QUELLE));
            Assert.Equal(paarungenVorher + 3 + 34 + 6, Zahl(ImportzuordnungSchema.TAB_ZUORDNUNG));
            Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude));
        }

        // =====================================================================
        //  Fehlerfälle
        // =====================================================================

        [Fact]
        public void Ein_Fehlerfall_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            var ctrl = new GebaeudeZonenCtrl();
            long[] vorher = Bestand();

            // Ein abgelehnter Vorschlag: sein erster Fehler, nichts geschrieben.
            GebaeudeBauteilvorschlag abgelehnt = BauteilvorschlagProbe.Vorschlag("gbxml_ohne_konstruktionen.xml");
            GebaeudeZonenCtrl.Vorschlagsergebnis a = ctrl.VorschlagSchreiben(g.ID_Gebaeude, abgelehnt);
            Assert.False(a.Ok);
            Assert.Equal(GebaeudeBauteilvorschlag.UWERT_FEHLT, a.Befund.Schluessel);
            Assert.False(string.IsNullOrEmpty(a.Meldung));
            Assert.Empty(a.Zuordnungen);
            Assert.Equal(vorher, Bestand());

            // Ein Gebäude, das es nicht gibt.
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag(PROBE);
            GebaeudeZonenCtrl.Vorschlagsergebnis b = ctrl.VorschlagSchreiben(int.MaxValue, v);
            Assert.False(b.Ok);
            Assert.Equal(GebaeudeBauteilvorschlag.GEBAEUDE_FEHLT, b.Befund.Schluessel);
            Assert.Equal(vorher, Bestand());

            // Ein Fehler mitten im Vorgang: Aufbauten und Zone stehen schon, das erste Bauteil zeigt
            // auf einen Aufbau, den der Vorschlag nicht führt — der Vorgang rollt ganz zurück.
            GebaeudeBauteilvorschlag kaputt = BauteilvorschlagProbe.Vorschlag(PROBE);
            kaputt.Zone.Bauteile[0].ID_Aufbau = -999;
            GebaeudeZonenCtrl.Vorschlagsergebnis c = ctrl.VorschlagSchreiben(g.ID_Gebaeude, kaputt);
            Assert.False(c.Ok);
            Assert.Equal(GebaeudeBauteilvorschlag.NICHT_GESCHRIEBEN, c.Befund.Schluessel);
            Assert.Contains("-999", c.Befund.Werte[0], StringComparison.Ordinal);
            Assert.Equal(vorher, Bestand());
            Assert.Empty(ctrl.LesenJeGebaeude(g.ID_Gebaeude));

            // Eine Zone steht schon: G3 kennt nur eine — der zweite Vorschlag wird abgelehnt.
            Assert.True(ctrl.VorschlagSchreiben(g.ID_Gebaeude, v).Ok);
            long[] mitZone = Bestand();
            GebaeudeZonenCtrl.Vorschlagsergebnis d = ctrl.VorschlagSchreiben(g.ID_Gebaeude, BauteilvorschlagProbe.Vorschlag(PROBE));
            Assert.False(d.Ok);
            Assert.Equal(GebaeudeBauteilvorschlag.ZONE_VORHANDEN, d.Befund.Schluessel);
            Assert.Equal(new[] { g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture), "1" }, d.Befund.Werte);
            Assert.Equal(mitZone, Bestand());
        }

        // =====================================================================
        //  Auskunft: Bauteilweg gegen Klassenweg desselben Imports
        // =====================================================================

        /// <summary>
        /// Zur Auskunft, nicht als Abnahme: der Jahresheizwärmebedarf des Probenhauses im Bauteilweg
        /// (Zone aus dem Vorschlag) gegen denselben Import im Klassenweg (die Summenfelder der Zuordnung
        /// auf derselben Gebäudezeile), beide mit dem Klima des Projekts.
        /// </summary>
        [Theory]
        [InlineData(PROBE)]
        [InlineData("gbxml_innenflaechen_teilweise.xml")]
        [InlineData("ifc4_haus.ifc")]
        public void Auskunft_Jahresheizwaerme_Bauteilweg_gegen_Klassenweg(string probe)
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(probe);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null);
            Assert.True(new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v).Ok);

            SimulationWaermebedarf sim = NeueRechnung(PROJEKT);
            double Rechnen(bool mitZone)
            {
                ProjektGebaeudeModel x = Zeile(PROJEKT);
                MitImport(x, v.Satz, v.Innenflaechenfaktor);
                if (!mitZone) x.Zonen = null;
                var werte = new double[8760];
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                Assert.True(sim.HeizwaermeEinesGebaeudes(x, 0, werte));
                Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Hinweise));
                if (mitZone)
                {
                    // Die Innengruppe nach dem Weg des Vorschlags — Zeilen oder gemessener Faktor.
                    KlimakalenderGemeinsam k = sim.Kalender.Gemeinsam;
                    GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(x, k.SolarOrtszeit, k.WochenendeOrtszeit, k.Laengengrad, k.Breitengrad,
                                                                          GebaeudeKlimaweg.ZEITBEZUG_VORGABE, sim.KuehlbetriebProjekt, null);
                    Assert.Equal(v.InnenflaecheDateiM2, e.Parameter.A_IW_M2, 9);
                    Assert.Equal(v.Innenweg == Innenweg.Bauteile ? Gruppenweg.Bauteilweg : Gruppenweg.Klassenweg, e.Parameter.WegInnen);
                }
                return sim.GebaeudeErgebnisse.Ergebnis(0).JahresheizwaermeMwh;
            }
            double klasse = Rechnen(false), bauteil = Rechnen(true);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}, Projekt {1}: Jahresheizwärme Klassenweg {2:F3} MWh, Bauteilweg {3:F3} MWh, Verhältnis {4:F4} (Innenweg {5}, A_IW {6:F1} m²)",
                probe, PROJEKT, klasse, bauteil, bauteil / klasse, v.Innenweg, v.InnenflaecheDateiM2));
            Assert.True(klasse > 0.0 && bauteil > 0.0);
        }

        // =====================================================================
        //  Der Namensabgleich der Baustoffe (Ergänzung G4b, Welle 1)
        // =====================================================================

        private const string MATERIALHAUS = "ifc4_haus_materialnamen.ifc";

        /// <summary>
        /// <b>Der Durchgang mit dem Namensabgleich über die Datenbank</b>: Die gemerkte Zuordnung des
        /// Projekts („Fußbodenaufbau" → Zementestrich) wirkt; der Schreibweg legt je Katalogbaustoff EINE
        /// Projektkopie an (Herkunft <c>KATALOG</c>), die Schichten tragen die Werte des Katalogs und zeigen
        /// mit <c>ID_Baustoff</c> auf die Kopie, die ruhende Luftschicht ohne Baustoff und ohne λ; in der
        /// Herkunftsablage steht je Baustoff der Datei eine Paarung mit dem Ziel <c>ID_Baustoff</c>.
        /// </summary>
        [Fact]
        public void Der_Abgleich_schreibt_Katalogwerte_Projektbaustoffe_und_Paarungen()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            Assert.True(BaustoffabgleichCtrl.Merken(PROJEKT, "Fußbodenaufbau", 5).Ok);
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(MATERIALHAUS);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, null, new BaustoffabgleichCtrl(PROJEKT).Abgleich());
            Assert.False(v.Abgelehnt);
            Assert.Equal(7, v.Aufbauten.Count);
            long quellenVorher = Zahl(ImportzuordnungSchema.TAB_QUELLE);

            GebaeudeZonenCtrl.Vorschlagsergebnis e;
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(g.ID_Gebaeude, ablauf.Quelle, e.Zuordnungen, vorgang);
                Assert.True(h.Ok, h.Meldung);
                vorgang.Commit();
            }
            Assert.Equal(quellenVorher + 1, Zahl(ImportzuordnungSchema.TAB_QUELLE));

            // Je Katalogbaustoff eine Projektkopie mit Herkunft KATALOG.
            var katalog = new BaustoffCtrl();
            List<int> stamm = v.Aufbauten.SelectMany(a => a.Stammbaustoffe).Where(x => x.HasValue).Select(x => x.Value).Distinct().OrderBy(x => x).ToList();
            Assert.Equal(new[] { 1, 2, 5, 10, 12, 13, 20, 36, 39, 48, 56 }, stamm);
            List<BaustoffModel> projekt = katalog.LesenProjekt(PROJEKT);
            var kopieJeStamm = new Dictionary<int, BaustoffModel>();
            foreach (int id in stamm)
            {
                BaustoffModel s = katalog.LesenKatalogsatz(id);
                BaustoffModel k = Assert.Single(projekt, p => p.Bezeichner == s.Bezeichner && p.Hersteller == s.Hersteller);
                Assert.Equal(DbWerte.HERKUNFT_KATALOG, k.Herkunft);
                Assert.Equal((s.Lambda, s.Rho, s.Cp), (k.Lambda, k.Rho, k.Cp));
                kopieJeStamm[id] = k;
            }

            // Die Schichten: Werte des Katalogs, ID_Baustoff auf die Projektkopie; die Luftschicht ohne beides.
            List<BauteilaufbauModel> aufbauten = new BauteilaufbauCtrl().LesenJeProjekt(PROJEKT).Where(a => a.Quelle == MATERIALHAUS).ToList();
            Assert.Equal(7, aufbauten.Count);
            Assert.All(aufbauten, a => Assert.Equal(DbWerte.HERKUNFT_KATALOG, a.Herkunft));
            var jeKopie = kopieJeStamm.Values.ToDictionary(k => k.ID);
            int mitStoff = 0, luft = 0;
            foreach (BauteilschichtModel s in aufbauten.SelectMany(a => a.Schichten))
            {
                if (s.IstLuftschicht)
                {
                    luft++;
                    Assert.Null(s.ID_Baustoff);
                    Assert.Null(s.Lambda);
                    continue;
                }
                Assert.True(s.ID_Baustoff.HasValue, "Schicht ohne Baustoff: " + s.Dicke);
                BaustoffModel k = jeKopie[s.ID_Baustoff.Value];
                Assert.Equal((k.Lambda, k.Rho, k.Cp), (s.Lambda, s.Rho, s.Cp));
                mitStoff++;
            }
            Assert.Equal(1, luft);
            Assert.Equal(v.Aufbauten.Sum(a => a.Stammbaustoffe.Count(x => x.HasValue)), mitStoff);

            // Die Paarungen: je Baustoff der Datei eine, auf die Projektkopie — zwei Namen, ein Stoff, eine Kopie.
            List<GebaeudeQuellzuordnung> stoffe = e.Zuordnungen.Where(z => z.Ziel == ImportZiel.Baustoff).ToList();
            Assert.Equal(17, stoffe.Count);
            Assert.All(stoffe, z => Assert.Equal(GebaeudeBauteilvorschlag.QUELLTYP_IFC_BAUSTOFF, z.Quelltyp));
            Assert.Equal(kopieJeStamm[36].ID, stoffe.Single(z => z.Quellkennung == "Mineralwolle 102890377").ZielId);
            Assert.Equal(kopieJeStamm[36].ID, stoffe.Single(z => z.Quellkennung == "Trittschalldämmung").ZielId);
            Assert.Equal(kopieJeStamm[5].ID, stoffe.Single(z => z.Quellkennung == "Fußbodenaufbau").ZielId);
            Assert.Equal(17L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"Tab_Importzuordnung\" z JOIN \"Tab_Importquelle\" q ON q.\"ID\" = z.\"ID_Importquelle\" " +
                "WHERE q.\"ID_Gebaeude\" = ? AND z.\"ID_Baustoff\" IS NOT NULL", new DbParam("@g", g.ID_Gebaeude)), CultureInfo.InvariantCulture));

            // Der Lauf rechnet den Bauteilweg mit den abgeglichenen Aufbauten.
            ProjektGebaeudeModel mit = Zeile(PROJEKT);
            SimulationWaermebedarf sim = NeueRechnung(PROJEKT);
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(mit, 0, new double[8760]));
            Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Hinweise));
        }

        /// <summary>
        /// Zur Auskunft, nicht als Abnahme: der Jahresheizwärmebedarf des IFC-Probenhauses mit Materialnamen
        /// im Bauteilweg — <b>vorher</b> (ohne Abgleich: keine Aufbauten, Masse aus der Bauweise,
        /// Innenflächenfaktor aus der Datei) und <b>nachher</b> (abgeglichene Aufbauten, Innenbauteile), dazu
        /// der Klassenweg desselben Imports. Projekt 1045, dieselbe Gebäudezeile.
        /// </summary>
        [Fact]
        public void Auskunft_Jahresheizwaerme_des_Probenhauses_ohne_und_mit_Namensabgleich()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(MATERIALHAUS);
            GebaeudeBauteilvorschlag ohne = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null);
            GebaeudeBauteilvorschlag mit = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, null, new BaustoffabgleichCtrl(PROJEKT).Abgleich());
            Assert.Empty(ohne.Aufbauten);
            Assert.Equal(6, mit.Aufbauten.Count);
            SimulationWaermebedarf sim = NeueRechnung(PROJEKT);

            double Rechnen(GebaeudeBauteilvorschlag v, bool mitZone)
            {
                if (mitZone) Assert.True(new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v).Ok);
                ProjektGebaeudeModel x = Zeile(PROJEKT);
                MitImport(x, v.Satz, v.Innenflaechenfaktor);
                if (!mitZone) x.Zonen = null;
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                Assert.True(sim.HeizwaermeEinesGebaeudes(x, 0, new double[8760]));
                Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Hinweise));
                double mwh = sim.GebaeudeErgebnisse.Ergebnis(0).JahresheizwaermeMwh;
                if (mitZone)
                    DataRepository.ExecuteNonQuery("DELETE FROM \"" + ZonenSchema.TAB_ZONE + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", g.ID_Gebaeude));
                return mwh;
            }
            double klasse = Rechnen(ohne, false), vorher = Rechnen(ohne, true), nachher = Rechnen(mit, true);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}, Projekt {1}: Jahresheizwärme Klassenweg {2:F3} MWh; Bauteilweg ohne Abgleich {3:F3} MWh (Aufbauten 0, Innenweg {4}); " +
                "mit Abgleich {5:F3} MWh (Aufbauten {6}, Innenweg {7}); Verhältnis nachher/vorher {8:F4}",
                MATERIALHAUS, PROJEKT, klasse, vorher, ohne.Innenweg, nachher, mit.Aufbauten.Count, mit.Innenweg, nachher / vorher));
            Assert.True(klasse > 0.0 && vorher > 0.0 && nachher > 0.0);
        }

        /// <summary>
        /// Die Summenfelder der Zuordnung auf eine Gebäudezeile (der Klassenweg des Imports), Faktor 1;
        /// dazu der Innenflächenfaktor des Vorschlags, wie ihn die Zielfelder schreiben (Welle B).
        /// </summary>
        private static void MitImport(ProjektGebaeudeModel x, GebaeudeImportSatz s, double? innenflaechenfaktor)
        {
            x.Innenflaechenfaktor = innenflaechenfaktor;
            double W(string feld) => s.Zeile(feld).Wert ?? 0.0;
            x.Nutzflaeche = W(GebaeudeZielfelder.NUTZFLAECHE);
            x.Wohnflaeche_gesamt = x.Nutzflaeche;
            x.Z_AuswahlWohnflaeche = x.Nutzflaeche;
            x.Einheit = GebaeudeVorbereitung.EINHEIT_FLAECHE;
            x.Raumhoehe = W(GebaeudeZielfelder.RAUMHOEHE);
            x.Bauweise = Gebaeudebauweise.BauweiseAusBauart(GebaeudeZielfelder.BauartIndex(s.Zeile(GebaeudeZielfelder.BAUART).Textwert), x.Nutzflaeche);
            x.Flaeche_Außenwand = W(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            x.k_Wert_Außenwand = W(GebaeudeZielfelder.U_AUSSENWAND);
            x.Dachflaeche = W(GebaeudeZielfelder.FLAECHE_DACH);
            x.k_Wert_Dachflaeche = W(GebaeudeZielfelder.U_DACH);
            x.Grundflaeche = W(GebaeudeZielfelder.FLAECHE_GRUND);
            x.k_Wert_Grundflaeche = W(GebaeudeZielfelder.U_GRUND);
            x.Grundflaeche_Randbedingung = s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert;
            x.Sonstige_Flaechen = W(GebaeudeZielfelder.FLAECHE_SONSTIGE);
            x.k_Wert_Sonstiges = W(GebaeudeZielfelder.U_SONSTIGE);
            x.k_Wert_Fenster = W(GebaeudeZielfelder.U_FENSTER);
            x.Fensterdurchlassgrad = W(GebaeudeZielfelder.G_WERT);
            x.Fensterflaeche_Sued = W(GebaeudeZielfelder.FENSTER_SUED);
            x.Fensterflaeche_Nord = W(GebaeudeZielfelder.FENSTER_NORD);
            x.Fensterflaeche_Ost = W(GebaeudeZielfelder.FENSTER_OST);
            x.Fensterflaeche_West = W(GebaeudeZielfelder.FENSTER_WEST);
            x.Fensterflaeche_OstWest = W(GebaeudeZielfelder.FENSTER_OST) + W(GebaeudeZielfelder.FENSTER_WEST);
            x.gesamte_Fensterflaeche = W(GebaeudeZielfelder.FENSTER_GESAMT);
            x.Abmessung_Anschluß_Fenster_Wand = 0.0;
            x.Abmessung_Anschluß_Wand_Dach = 0.0;
            x.Abmessung_Anschluß_Außenwand_Kellerdecke = 0.0;
            x.Luftwechsel_Infiltration = s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION).Wert;
            x.Luftwechsel_Nutzer = s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_NUTZER).Wert;
        }
    }
}
