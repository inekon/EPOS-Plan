using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schreibweg und Eingang des Zapfprofilgenerators</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.3, A8; Stufe Z1, Gruppe 2): <c>ZapfprofilCtrl.Speichern</c> schreibt
    /// Projektzeile, Zonen und Wohnungstypen in EINEM Vorgang als Upsert, setzt die Weiche nur
    /// ausdrücklich, prüft die Katalogverweise und lässt benutzte Katalogzeilen gesperrt;
    /// <c>ZapfprofilCtrl.Eingang</c> baut daraus den Eingang des Laufs samt Belegung,
    /// Parametersatz, eigenen Tagesgangsätzen und den Vorbelegungen des gebundenen Gebäudes.
    ///
    /// <para>Gearbeitet wird auf einer ARBEITSKOPIE der Testdatenbank mit ihrem fiktiven
    /// Testkatalog; Projekt 1006 ist kein Projekt der Referenzbasis. Alle Werte sind erfunden
    /// und rund.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilSpeichernTests
    {
        private const int PROJEKT = 1006;
        private const int ANDERES_PROJEKT = 1009;
        private const string VERSION = "TEST-1";

        // =================================================================================
        // Speichern
        // =================================================================================

        [Fact]
        public void Speichern_und_Lies_ergeben_denselben_Stand_und_ein_zweites_Speichern_aendert_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilStand stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA(), ZoneB() }, null);
            ZapfprofilStand geschrieben = ZapfprofilCtrl.Speichern(PROJEKT, stand);

            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(BrauchwasserWeg.Generator, gelesen.Weg);
            Assert.Equal(2, gelesen.Zonen.Count);
            Assert.Equal(geschrieben.Zonen.Select(z => z.Id), gelesen.Zonen.Select(z => z.Id));

            ZonenStand a = gelesen.Zonen[0];
            Assert.Equal("Zone A", a.Name);
            Assert.Equal(1, a.Reihenfolge);
            Assert.Equal(12.0, a.Bezugsmenge);
            Assert.Equal(ZapfNiveau.Hoch, a.Niveau);
            Assert.Equal(ZapfTopologie.Frischwasserstation, a.Topologie);
            Assert.False(a.Zirkulation);
            Assert.Equal(new int?[] { 100, null, null, null }, a.Ferienbeginn);
            Assert.Equal(new int?[] { 110, null, null, null }, a.Ferienende);
            Assert.Equal(55.0, a.ZapftemperaturC);
            Assert.Equal(1.5, a.Auslastung[0]);
            Assert.Null(a.Auslastung[1]);
            Assert.Equal(2, a.Wohnungen.Count);
            Assert.Equal(new[] { 4, 2 }, a.Wohnungen.Select(w => w.Anzahl));
            Assert.Equal(KlasseA(), a.Wohnungen[0].IdAusstattung);
            Assert.Equal(4.0, a.Wohnungen[0].Raumzahl);

            ZonenStand b = gelesen.Zonen[1];
            Assert.Equal("Zone B", b.Name);
            Assert.Equal(2, b.Reihenfolge);
            Assert.Equal(ZapfMesswerteinheit.KwhJeJahr, b.JahresmesswertEinheit);
            Assert.Equal(ZapfBilanzgrenze.Zapfstelle, b.JahresmesswertBilanzgrenze);
            Assert.Equal(900.0, b.Jahresmesswert);
            Assert.False(b.TagesbedarfAuto);
            Assert.Equal(3.0, b.TagesbedarfManuellKwh);

            // Ohne Projektgrößen im Stand entsteht die Zeile mit den Vorgaben der DDL.
            Assert.NotNull(gelesen.Projekt);
            Assert.Equal(1, gelesen.Projekt.Seed);
            Assert.Equal(10, gelesen.Projekt.Realisierungen);
            Assert.Equal(99, gelesen.Projekt.Perzentil);
            Assert.Equal(ZapfZirkulationsmethode.Flaechenkennwert, gelesen.Projekt.ZirkMethode);

            // Ein zweites Speichern des gelesenen Stands: dieselben Ids, keine neue Zeile.
            (long zonen, long wohnungen, long projekte) vorher = Zeilen();
            ZapfprofilStand zweit = ZapfprofilCtrl.Speichern(PROJEKT, gelesen);
            Assert.Equal(vorher, Zeilen());
            Assert.Equal(gelesen.Zonen.Select(z => z.Id), zweit.Zonen.Select(z => z.Id));
            Assert.Equal(gelesen.Zonen[0].Wohnungen.Select(w => w.Id), zweit.Zonen[0].Wohnungen.Select(w => w.Id));
            Assert.Equal(gelesen.Projekt.Id, zweit.Projekt.Id);

            // Die Projektgrößen reisen mit: geänderter Seed und Zirkulationsfläche.
            ZapfprofilCtrl.Speichern(PROJEKT, gelesen with
            {
                Projekt = gelesen.Projekt with { Seed = 7, ZirkFlaecheM2 = 250.0, ZirkLage = ZapfLeitungslage.AusserhalbHuelle }
            });
            ProjektStand p = ZapfprofilCtrl.Lies(PROJEKT).Projekt;
            Assert.Equal(7, p.Seed);
            Assert.Equal(250.0, p.ZirkFlaecheM2);
            Assert.Equal(ZapfLeitungslage.AusserhalbHuelle, p.ZirkLage);
            Assert.Equal(gelesen.Projekt.Id, p.Id);
        }

        [Fact]
        public void Upsert_aendert_vorhandene_Zonen_und_entfernt_fehlende_samt_Wohnungstypen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilStand s = ZapfprofilCtrl.Speichern(PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { ZoneA(), ZoneB() }, null));
            int idA = s.Zonen[0].Id, idB = s.Zonen[1].Id;

            // Zone A fällt weg, Zone B wird geändert, eine neue Zone C kommt dazu.
            ZonenStand bNeu = s.Zonen[1] with { Bezugsmenge = 25.0, Name = "Zone B geändert" };
            ZonenStand c = ZoneB() with { Name = "Zone C", Jahresmesswert = null, JahresmesswertEinheit = null,
                                          JahresmesswertBilanzgrenze = null };
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { bNeu, c }, null));

            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(new[] { "Zone B geändert", "Zone C" }, gelesen.Zonen.Select(z => z.Name));
            Assert.Equal(idB, gelesen.Zonen[0].Id);
            Assert.Equal(25.0, gelesen.Zonen[0].Bezugsmenge);
            Assert.DoesNotContain(gelesen.Zonen, z => z.Id == idA);
            Assert.Equal(0L, Anzahl("SELECT COUNT(*) FROM Tab_TwwWohnungstyp WHERE ID_Zone = ?", idA));
            Assert.Equal(0L, Anzahl("SELECT COUNT(*) FROM Tab_TwwZone WHERE ID = ?", idA));
        }

        [Fact]
        public void Die_Weiche_kommt_nur_aus_dem_ausdruecklichen_Weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Zonen allein stellen die Weiche nicht um.
            ZapfprofilStand s = ZapfprofilCtrl.Speichern(PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { ZoneA() }, null));
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));

            // Ein Weg in den Projektgrößen zählt nicht — nur der des Stands.
            ZapfprofilStand g = ZapfprofilCtrl.Lies(PROJEKT);
            ZapfprofilCtrl.Speichern(PROJEKT, g with { Projekt = g.Projekt with { Weg = BrauchwasserWeg.Generator } });
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));

            // Ausdrücklich umgestellt — und zurück, die Zonen bleiben (ZU4).
            ZapfprofilCtrl.Speichern(PROJEKT, g with { Weg = BrauchwasserWeg.Generator });
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));
            ZapfprofilCtrl.Speichern(PROJEKT, ZapfprofilCtrl.Lies(PROJEKT) with { Weg = BrauchwasserWeg.Bestand });
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.Single(ZapfprofilCtrl.Lies(PROJEKT).Zonen);
            Assert.Equal(s.Zonen[0].Id, ZapfprofilCtrl.Lies(PROJEKT).Zonen[0].Id);
        }

        [Fact]
        public void Speichern_im_Vorgang_des_Aufrufers_schreibt_erst_mit_dessen_Commit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() }, null), v);
                v.Rollback();
            }
            Assert.Equal((0L, 0L, 0L), Zeilen());

            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() }, null), v);
                v.Commit();
            }
            Assert.Equal((1L, 2L, 1L), Zeilen());
        }

        /// <summary>
        /// <b>Veraltet nach dem Speichern.</b> Ein gespeichertes Zapfprofil — ebenso die allein
        /// umgestellte Weiche — setzt <c>Tab_Projekt.Aenderungsdatum</c>; ein vorhandenes
        /// Simulationsergebnis gilt danach als veraltet (derselbe Mechanismus wie in den übrigen
        /// Schreibwegen). Die Marke sitzt im Vorgang des Aufrufers: Sein Rollback nimmt sie
        /// zurück, sein Commit lässt sie stehen; eine Ablehnung setzt sie nie.
        /// </summary>
        [Fact]
        public void Speichern_markiert_das_Projekt_als_geaendert_und_ein_Rollback_nimmt_die_Marke_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Abgelehnt: nichts geschrieben, keine Marke.
            DatumZuruecksetzen();
            Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() with { IdNutzungsart = 987654 } }, null)));
            Assert.Equal(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT));

            // Im Vorgang des Aufrufers: dort gesetzt, mit dessen Rollback wieder fort.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() }, null), v);
                using (Vorgangsklammer.Setzen(v))
                    Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT)));
                v.Rollback();
            }
            Assert.Equal(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT));
            Assert.Equal((0L, 0L, 0L), Zeilen());

            // Mit dessen Commit bleibt sie stehen.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { ZoneA() }, null), v);
                v.Commit();
            }
            Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT)));

            // Die Weiche allein: derselbe Stand, nur auf den Generator umgestellt.
            DatumZuruecksetzen();
            ZapfprofilCtrl.Speichern(PROJEKT, ZapfprofilCtrl.Lies(PROJEKT) with { Weg = BrauchwasserWeg.Generator });
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT)));
        }

        [Fact]
        public void Ungueltige_Verweise_werden_benannt_abgelehnt_und_nichts_wird_geschrieben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfSpeicherfehler Grund(int projekt, params ZonenStand[] zonen) =>
                Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(projekt,
                    new ZapfprofilStand(BrauchwasserWeg.Generator, zonen, null))).Fehler;

            Assert.Equal(ZapfSpeicherfehler.NutzungsartFehlt, Grund(PROJEKT, ZoneA() with { IdNutzungsart = 987654 }));
            Assert.Equal(ZapfSpeicherfehler.TagesgangsatzFehlt, Grund(PROJEKT, ZoneA() with { IdTagesgangsatz = 987654 }));
            Assert.Equal(ZapfSpeicherfehler.GebaeudeFehlt, Grund(PROJEKT, ZoneA() with { IdGebaeude = 987654 }));
            Assert.Equal(ZapfSpeicherfehler.ProjektFehlt, Grund(987654, ZoneA()));
            Assert.Equal(ZapfSpeicherfehler.ZoneUngueltig, Grund(PROJEKT, ZoneA() with { Bezugsmenge = 0.0 }));
            Assert.Equal(ZapfSpeicherfehler.ZoneUngueltig, Grund(PROJEKT, ZoneA() with { Name = " " }));

            // Die Ausstattungsklasse muss eine AUSSTATTUNG sein — eine Belegungszeile genügt nicht.
            int belegung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_TwwDin4708Wert_STAMM WHERE Art = 'BELEGUNG'"));
            ZonenStand falscheKlasse = ZoneA() with
            {
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 1, Raumzahl = 2.0, IdAusstattung = belegung } }
            };
            Assert.Equal(ZapfSpeicherfehler.AusstattungFehlt, Grund(PROJEKT, falscheKlasse));
            Assert.Equal(ZapfSpeicherfehler.WohnungstypUngueltig, Grund(PROJEKT, ZoneA() with
            {
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 0 } }
            }));

            // Das Gebäude eines ANDEREN Projekts wird nicht gebunden (N8).
            int fremdesGebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt <> ?", new DbParam("@p", PROJEKT)));
            ZapfprofilSpeicherException gf = Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() with { IdGebaeude = fremdesGebaeude } }, null)));
            Assert.Equal(ZapfSpeicherfehler.GebaeudeFremd, gf.Fehler);
            Assert.Contains("gehört zu einem anderen Projekt", gf.Message);

            // Eine Projektgröße außerhalb ihrer Wertemenge wird benannt abgelehnt, bevor geschrieben
            // wird — nicht erst von der CHECK-Klausel mitten im Vorgang (N8).
            ProjektStand vorgabe = ZapfprofilCtrl.ProjektVorgabe();
            foreach (ProjektStand falsch in new[]
                     {
                         vorgabe with { Perzentil = 97 },
                         vorgabe with { Realisierungen = 0 },
                         vorgabe with { ZirkMethode = (ZapfZirkulationsmethode)9 },
                         vorgabe with { Speicherart = (ZapfSpeicherart)9 },
                         vorgabe with { ZirkLage = (ZapfLeitungslage)9 },
                         vorgabe with { BedarfstagQuelle = (ZapfBedarfstagquelle)9 }
                     })
            {
                ZapfprofilSpeicherException pu = Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(PROJEKT,
                    new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() }, falsch)));
                Assert.Equal(ZapfSpeicherfehler.ProjektUngueltig, pu.Fehler);
            }

            Assert.Equal((0L, 0L, 0L), Zeilen());

            // Eine Zone eines ANDEREN Projekts wird nie still umgehängt.
            ZapfprofilStand fremd = ZapfprofilCtrl.Speichern(ANDERES_PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { ZoneB(), ZoneA() with { IdGebaeude = null } }, null));
            Assert.Equal(ZapfSpeicherfehler.ZoneFremd, Grund(PROJEKT, fremd.Zonen[0]));
            Assert.Equal(0L, Anzahl("SELECT COUNT(*) FROM Tab_TwwZone WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(ANDERES_PROJEKT, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Projekt FROM Tab_TwwZone WHERE ID = ?", new DbParam("@id", fremd.Zonen[0].Id))));

            // Ebenso ein Wohnungstyp einer anderen Zone (N8).
            long wohnungenVorher = Zeilen().Wohnungen;
            Assert.Equal(ZapfSpeicherfehler.WohnungstypUngueltig, Grund(PROJEKT, ZoneA() with { Wohnungen = fremd.Zonen[1].Wohnungen }));
            Assert.Equal(0L, Anzahl("SELECT COUNT(*) FROM Tab_TwwZone WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(wohnungenVorher, Zeilen().Wohnungen);
        }

        [Fact]
        public void Eine_benutzte_Katalogzeile_bleibt_gesperrt_bis_keine_Zone_sie_mehr_nutzt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int satz = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_TwwTagesgangsatz_STAMM WHERE Katalogversion = ?", new DbParam("@k", VERSION)));
            int eigene = TwwTestdatenbank.NutzungsartAnlegen("Probe Speichern (fiktiv)", VERSION, satz);
            Assert.False(TwwNutzungsartCtrl.IstBenutzt(eigene));

            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { ZoneA() with { IdNutzungsart = eigene } }, null));
            Assert.True(TwwNutzungsartCtrl.IstBenutzt(eigene));
            Assert.Equal(TwwKatalogAusgang.BenutztGesperrt, TwwNutzungsartCtrl.Loeschen(eigene).Ausgang);
            Assert.Equal(TwwKatalogAusgang.BenutztGesperrt,
                TwwNutzungsartCtrl.Aendern(eigene, TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(eigene))).Ausgang);

            // Der Schreibweg selbst fasst den Katalog nicht an.
            Assert.Equal(1L, Anzahl("SELECT COUNT(*) FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", eigene));

            // Ohne Zone ist die Zeile wieder frei.
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Bestand, new ZonenStand[0], null));
            Assert.False(TwwNutzungsartCtrl.IstBenutzt(eigene));
            Assert.True(TwwNutzungsartCtrl.Loeschen(eigene).Ok);
        }

        // =================================================================================
        // Eingang
        // =================================================================================

        [Fact]
        public void Der_Eingang_traegt_Parameter_Belegung_Vorgaben_und_eigene_Tagesgangsaetze()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int satz = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_TwwTagesgangsatz_STAMM WHERE Katalogversion = ?", new DbParam("@k", VERSION)));
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() with { IdTagesgangsatz = satz } }, null);
            bool[] we = new bool[365];

            Zapfprofileingang e = ZapfprofilCtrl.Eingang(PROJEKT, stand, 2, we);
            Assert.Equal(VERSION, e.Parameter.Katalogversion);
            Assert.Equal(18, e.Parameter.Anzahl);
            Assert.Equal(2, e.WochentagJan1);
            Assert.Same(we, e.We);
            Assert.Equal(1.0, e.BelegungJeRaumzahl["2"]);
            Assert.Equal(3.0, e.BelegungJeRaumzahl["4"]);
            Assert.Equal(satz, Assert.Single(e.Tagesgangsaetze).Id);

            // Ohne Projektzeile: die Vorgaben der DDL, keine Abschrift im Quelltext.
            Assert.Equal(BrauchwasserWeg.Bestand, e.Projekt.Weg);
            Assert.Equal(1, e.Projekt.Seed);
            Assert.Equal(10, e.Projekt.Realisierungen);
            Assert.True(e.Projekt.ZirkAuto);
            Assert.Equal(ZapfZirkulationsmethode.Flaechenkennwert, e.Projekt.ZirkMethode);
            Assert.Null(e.Projekt.ZirkFlaecheM2);

            // Der gespeicherte Stand ist derselbe Eingang (bis auf die Ids).
            ZapfprofilCtrl.Speichern(PROJEKT, stand);
            Zapfprofileingang g = ZapfprofilCtrl.Eingang(PROJEKT, 2, we);
            Assert.Equal(e.Zonen[0].Bezugsmenge, g.Zonen[0].Bezugsmenge);
            Assert.Equal(e.Projekt.Seed, g.Projekt.Seed);
            ZapfprofilErgebnis r = ZapfprofilRechner.Rechnen(g, ZapfprofilCtrl.Katalog());
            Assert.True(r.Vollstaendig, string.Join(" | ", r.Ablehnungen.Select(a => a.Klartext)));
            Assert.True(r.Kennzahlen.JahresbedarfZapfungKwh > 0);
        }

        [Fact]
        public void Ohne_Katalogversion_lehnt_der_Eingang_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_TwwParameter_STAMM"));

            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ZoneA() }, null);
            ParametersatzException ex = Assert.Throws<ParametersatzException>(
                () => ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, new bool[365]));
            Assert.Equal(ParametersatzFehler.KeineKatalogversion, ex.Fehler);
        }

        /// <summary>
        /// Gebundenes Gebäude (A8): Ferien und Fläche belegen nur vor. Zwei Zonen ohne eigene
        /// Fläche teilen sich die Fläche des Gebäudes; eine Zone mit eigenen Ferien behält sie;
        /// ohne Ferienkennzeichen des Gebäudes kommen keine Ferien.
        /// </summary>
        [Fact]
        public void Das_gebundene_Gebaeude_belegt_Ferien_und_Flaeche_nur_vor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int gebaeude = Gebaeude();
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Wohnflaeche_gesamt = 200.0, Ferien = 1.0, Ferienbeginn_1 = 200.0, " +
                "Ferienende_1 = 210.0, Ferienbeginn_2 = 366.0, Ferienende_2 = 0.0, Ferienbeginn_3 = 0.0, Ferienende_3 = 0.0, " +
                "Ferienbeginn_4 = 0.0, Ferienende_4 = 0.0 WHERE ID = ?", new DbParam("@id", gebaeude)));

            ZonenStand ohneFerien = ZoneB() with { Name = "Ohne Ferien", IdGebaeude = gebaeude };
            ZonenStand mitFerien = ZoneA() with { Name = "Mit Ferien", IdGebaeude = gebaeude };
            ZonenStand ungebunden = ZoneB() with { Name = "Ungebunden", IdGebaeude = null };
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ohneFerien, mitFerien, ungebunden }, null);

            Zapfprofileingang e = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, new bool[365]);
            Assert.Equal(new int?[] { 200, 366, 0, 0 }, e.Zonen[0].Ferienbeginn);
            Assert.Equal(new int?[] { 210, 0, 0, 0 }, e.Zonen[0].Ferienende);
            Assert.Equal(new int?[] { 100, null, null, null }, e.Zonen[1].Ferienbeginn);   // eigene Ferien bleiben
            Assert.Equal(100.0, e.Zonen[0].GebaeudeflaecheM2);                              // 200 m² zu gleichen Teilen
            Assert.Equal(100.0, e.Zonen[1].GebaeudeflaecheM2);
            Assert.Null(e.Zonen[2].GebaeudeflaecheM2);
            Assert.Equal(new int?[4], e.Zonen[2].Ferienbeginn);

            // Nichts davon wird gespeichert.
            ZapfprofilCtrl.Speichern(PROJEKT, stand);
            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(new int?[4], gelesen.Zonen[0].Ferienbeginn);
            Assert.Null(gelesen.Zonen[0].GebaeudeflaecheM2);

            // Ohne Ferienkennzeichen des Gebäudes: keine Ferien aus dem Gebäude.
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Ferien = 0.0 WHERE ID = ?",
                                                  new DbParam("@id", gebaeude)));
            Zapfprofileingang ohne = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, new bool[365]);
            Assert.Equal(new int?[4], ohne.Zonen[0].Ferienbeginn);

            // Im Rechenweg trägt die Zone die Gebäudefläche als Zonenfläche.
            ZapfprofilErgebnis r = ZapfprofilRechner.Rechnen(e, ZapfprofilCtrl.Katalog());
            Assert.Contains(r.Herkunft, h => h.Zone == "Ohne Ferien" && h.Feld == ZapfFeld.ZONENFLAECHE && h.Wert == 100.0);

            // Gemischte Zonen an einem Gebäude (N8): Die eigene Fläche (6 WE · 20 m²) geht von der
            // Gebäudefläche ab, der Rest (80 m²) zu gleichen Teilen an die Zonen ohne eigene.
            ZonenStand eigene = ZoneA() with { Name = "Eigene Fläche", IdGebaeude = gebaeude, WohnflaecheJeWeM2 = 20.0 };
            ZonenStand rest = ZoneB() with { Name = "Rest", IdGebaeude = gebaeude };
            Zapfprofileingang gemischt = ZapfprofilCtrl.Eingang(PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { ohneFerien, eigene, rest }, null), 0, new bool[365]);
            Assert.Equal(40.0, gemischt.Zonen[0].GebaeudeflaecheM2);
            Assert.Null(gemischt.Zonen[1].GebaeudeflaecheM2);
            Assert.Equal(40.0, gemischt.Zonen[2].GebaeudeflaecheM2);

            // Deckt die eigene Fläche das Gebäude (6 · 40 m² ≥ 200 m²), belegt es keine Fläche vor.
            Zapfprofileingang gedeckt = ZapfprofilCtrl.Eingang(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { ohneFerien, eigene with { WohnflaecheJeWeM2 = 40.0 } }, null), 0, new bool[365]);
            Assert.Null(gedeckt.Zonen[0].GebaeudeflaecheM2);

            // Das Gebäude eines anderen Projekts liest der Eingang nicht.
            int fremdesGebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt <> ? AND Wohnflaeche_gesamt > 0", new DbParam("@p", PROJEKT)));
            Zapfprofileingang fremd = ZapfprofilCtrl.Eingang(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { ohneFerien with { IdGebaeude = fremdesGebaeude } }, null), 0, new bool[365]);
            Assert.Null(fremd.Zonen[0].GebaeudeflaecheM2);
        }

        // =================================================================================
        // Hilfen — erfundene Zonen auf dem fiktiven Testkatalog
        // =================================================================================

        private static int Nutzung(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
            new DbParam("@b", bezeichner), new DbParam("@k", VERSION)));

        private static int KlasseA() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_TwwDin4708Wert_STAMM WHERE Art = 'AUSSTATTUNG' AND Schluessel = 'Testklasse A' AND Katalogversion = ?",
            new DbParam("@k", VERSION)));

        private static int Gebaeude() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)));

        /// <summary>Zone A: Personen mit Wohnungstabelle, eigene Ferien, Überschreibungen.</summary>
        private static ZonenStand ZoneA()
        {
            var auslastung = new double?[12];
            auslastung[0] = 1.5;
            return new ZonenStand
            {
                IdNutzungsart = Nutzung("Testnutzung A (fiktiv)"),
                IdGebaeude = Gebaeude(),
                Name = "Zone A",
                Bezugsmenge = 12.0,
                Niveau = ZapfNiveau.Hoch,
                Topologie = ZapfTopologie.Frischwasserstation,
                Zirkulation = false,
                Ferienbeginn = new int?[] { 100, null, null, null },
                Ferienende = new int?[] { 110, null, null, null },
                ZapftemperaturC = 55.0,
                Auslastung = auslastung,
                Wohnungen = new[]
                {
                    new WohnungstypStand { Anzahl = 4, Raumzahl = 4.0, IdAusstattung = KlasseA() },
                    new WohnungstypStand { Anzahl = 2, Raumzahl = 2.0 }
                }
            };
        }

        /// <summary>Zone B: Beschäftigte mit Messwert und manuellem Tagesbedarf.</summary>
        private static ZonenStand ZoneB() => new ZonenStand
        {
            IdNutzungsart = Nutzung("Testnutzung B (fiktiv)"),
            Name = "Zone B",
            Bezugsmenge = 20.0,
            Jahresmesswert = 900.0,
            JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
            JahresmesswertBilanzgrenze = ZapfBilanzgrenze.Zapfstelle,
            TagesbedarfAuto = false,
            TagesbedarfManuellKwh = 3.0
        };

        private static long Anzahl(string sql, int id) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("@id", id)));

        /// <summary>Ein Änderungsdatum, das jedes Speichern überbieten muss.</summary>
        private static readonly DateTime ALT = new DateTime(2000, 1, 1);

        private static void DatumZuruecksetzen()
        {
            DataRepository.ExecuteSQL("UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?",
                new DbParam("@d", DbParamTyp.Date) { Wert = ALT },
                new DbParam("@id", PROJEKT));
            Assert.Equal(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT));
        }

        private static (long Zonen, long Wohnungen, long Projekte) Zeilen() =>
            (Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_TwwZone")),
             Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_TwwWohnungstyp")),
             Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_TwwProjekt")));
    }
}
