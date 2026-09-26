using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6b, Welle W1 — Schemaschritt S-G ohne Datenbank</b>: Nummer, DDL-Texte und
    /// Wertlisten von <see cref="ZonenkopplungSchema"/>.
    /// </summary>
    public class ZonenkopplungSchemaRegelTests
    {
        [Fact]
        public void Der_Schritt_folgt_auf_den_Zonenschritt_und_der_Zielstand_traegt_ihn()
        {
            Assert.True(ZonenkopplungSchema.SCHRITT > ZonenSchema.SCHRITT);
            Assert.True(ZonenkopplungSchema.SCHRITT > ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS);
            // Der Zielstand reicht bis zu ihm (E47 legt Schritt 148 dahinter).
            Assert.True(SchemaStand.Zielversion >= ZonenkopplungSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + ZonenkopplungSchema.SCHRITT + ".");
        }

        [Fact]
        public void Die_Tabellen_sind_STRICT_und_tragen_die_Paarregel()
        {
            foreach (KeyValuePair<string, string> a in ZonenkopplungSchema.Tabellenanweisungen)
            {
                Assert.StartsWith("CREATE TABLE IF NOT EXISTS \"" + a.Key + "\" (", a.Value, StringComparison.Ordinal);
                Assert.EndsWith(") STRICT", a.Value, StringComparison.Ordinal);
            }
            string l = ZonenkopplungSchema.SQL_CREATE_LUFTSTROM;
            Assert.Contains("CHECK (\"ID_ZoneA\" < \"ID_ZoneB\")", l);
            Assert.Contains("\"Volumenstrom\" REAL NOT NULL CHECK (\"Volumenstrom\" > 0)", l);
            Assert.Contains("FOREIGN KEY (\"ID_ZoneA\") REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE", l);
            Assert.Contains("FOREIGN KEY (\"ID_ZoneB\") REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE", l);
            Assert.Contains(ZonenkopplungSchema.Indexanweisungen, i => i.Key == ZonenkopplungSchema.INDEX_LUFTSTROM
                                                                         && i.Value.StartsWith("CREATE UNIQUE INDEX", StringComparison.Ordinal));

            string e = ZonenkopplungSchema.SQL_CREATE_ERGEBNIS;
            Assert.Contains("REFERENCES \"Tab_ErgebnisGebaeude\" (\"ID\") ON DELETE CASCADE", e);
            Assert.Contains("\"ID_Zone\" INTEGER REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE SET NULL", e);
            Assert.DoesNotContain("DEFAULT", e);
            Assert.DoesNotContain("DEFAULT", l);
        }

        [Fact]
        public void Die_Nachbarzone_hat_keine_Loeschregel_und_die_Zuordnung_zwei_Werte()
        {
            KeyValuePair<string, string> nachbar = ZonenkopplungSchema.SpaltenBauteil[0];
            Assert.Equal("ID_Nachbarzone", nachbar.Key);
            Assert.Equal("INTEGER REFERENCES \"Tab_Zone\" (\"ID\")", nachbar.Value);   // NO ACTION, kein RESTRICT
            Assert.Equal("TEXT CHECK (\"Trennflaeche_Zuordnung\" IN ('IW','AW'))", ZonenkopplungSchema.SpaltenBauteil[1].Value);
            Assert.Equal(new[] { DbWerte.TRENNFLAECHE_IW, DbWerte.TRENNFLAECHE_AW }, DbWerte.TRENNFLAECHE_ZUORDNUNGEN);
            // Die Spalten der Zone und des Bauteils aus S-C bleiben, wie sie sind.
            Assert.DoesNotContain("ID_Nachbarzone", ZonenSchema.SQL_CREATE_BAUTEIL);
            Assert.Equal(16, ZonenSchema.Bauteilspalten.Count);
        }
    }

    /// <summary>
    /// <b>Stufe G6b, Welle W1 — Schemaschritt S-G und Datenweg an der Testdatenbank</b>: der Schritt
    /// auf der Arbeitskopie (wiederholbar), die Regeln im Schema (Paar, Volumenstrom, Zuordnung,
    /// NO ACTION an der Nachbarzone, SET NULL am Zonenergebnis), der Schreibweg mit Umschlüsseln der
    /// vorläufigen Ids und dem Abgleich der Luftströme, die Leser, der ältere Schemastand ohne S-G und
    /// die Kopierwege (Plan, Transfer des Zonenergebnisses, benannte Ablehnung ohne S-G).
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZonenkopplungDatenwegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
            GebaeudeZonenanschluss.ProbeVerwerfen();
        }

        private const int PROJEKT = ZonenRundlauf.PROJEKT;
        private const int GEBAEUDE = ZonenRundlauf.GEBAEUDE_A;

        private static string F(string muster, params object[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);

        /// <summary>Führt eine Anweisung in einem eigenen Vorgang aus und rollt zurück; wirft sie?</summary>
        private static bool Wirft(string sql, params DbParam[] p)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren(sql, p);
                v.Rollback();
                return false;
            }
            catch
            {
                v.Rollback();
                return true;
            }
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        /// <summary>Zwei Zonen (EG −1, OG −2), die Decke des EG als Trennfläche zum OG, ein Luftstrom OG → EG.</summary>
        private static (List<ZoneModel> Zonen, List<ZonenluftstromModel> Stroeme) Zwei()
        {
            var zonen = new List<ZoneModel>
            {
                new ZoneModel
                {
                    ID = -1, Bezeichner = "EG", Nutzflaeche = 80,
                    Bauteile =
                    {
                        new BauteilModel { ID = -1, Bezeichner = "Wand EG", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 60, U_Wert = 0.3, Azimut = 180 },
                        new BauteilModel { ID = -2, Bezeichner = "Decke EG/OG", Bauteilart = DbWerte.BAUTEILART_DECKE, Flaeche = 80, U_Wert = 1.2,
                                           Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = -2, Trennflaeche_Zuordnung = DbWerte.TRENNFLAECHE_AW },
                    }
                },
                new ZoneModel
                {
                    ID = -2, Bezeichner = "OG", Nutzflaeche = 80, IstBeheizt = false,
                    Bauteile = { new BauteilModel { ID = -3, Bezeichner = "Dach OG", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 90, U_Wert = 0.2 } }
                },
            };
            var stroeme = new List<ZonenluftstromModel> { new ZonenluftstromModel { ID = -1, ID_ZoneA = -2, ID_ZoneB = -1, Volumenstrom = 80 } };
            return (zonen, stroeme);
        }

        private static (List<ZoneModel> Zonen, List<ZonenluftstromModel> Stroeme) Geschrieben()
        {
            (List<ZoneModel> z, List<ZonenluftstromModel> l) = Zwei();
            GebaeudeZonenCtrl.Ergebnis e = new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, z, l);
            Assert.True(e.Ok, e.Meldung);
            return (z, l);
        }

        // =================================================================================
        //  Der Schritt
        // =================================================================================

        [Fact]
        public void Der_Schritt_steht_auf_der_Arbeitskopie_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ZonenkopplungSchema.Vollstaendig());
            Assert.True(GebaeudeZonenanschluss.KopplungVorhanden());
            var bericht = new List<string>();
            Assert.Equal(0, ZonenkopplungSchema.Ausfuehren(bericht));
            Assert.Single(bericht);

            List<string> bauteil = DataRepository.SpaltenVonTabelle(SchemaKatalog.TAB_BAUTEIL);
            Assert.Equal(new[] { "ID_Nachbarzone", "Trennflaeche_Zuordnung" }, bauteil.Skip(bauteil.Count - 2));
            Assert.Equal(ZonenkopplungSchema.SPALTENZAHL_LUFTSTROM, DataRepository.SpaltenVonTabelle(ZonenkopplungSchema.TAB_LUFTSTROM).Count);
            Assert.Equal(ZonenkopplungSchema.SPALTENZAHL_ERGEBNIS, DataRepository.SpaltenVonTabelle(ZonenkopplungSchema.TAB_ERGEBNIS).Count);
            foreach (string t in new[] { ZonenkopplungSchema.TAB_LUFTSTROM, ZonenkopplungSchema.TAB_ERGEBNIS })
                Assert.EndsWith("STRICT", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", t)), CultureInfo.InvariantCulture));
            Assert.Equal("ok", Convert.ToString(DataRepository.ExecuteScalar("PRAGMA integrity_check"), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Das_Schema_haelt_Paar_Volumenstrom_Zuordnung_und_Nachbar()
        {
            if (!_db.Vorhanden) return;
            (List<ZoneModel> z, _) = Geschrieben();
            int eg = z[0].ID, og = z[1].ID;
            int a = Math.Min(eg, og), b = Math.Max(eg, og);
            Assert.True(a > 0);

            Assert.True(Wirft("INSERT INTO Tab_Zonenluftstrom (ID_ZoneA, ID_ZoneB, Volumenstrom) VALUES (?, ?, 50)",
                              new DbParam("@a", b), new DbParam("@b", a)));                                  // A > B
            Assert.True(Wirft("INSERT INTO Tab_Zonenluftstrom (ID_ZoneA, ID_ZoneB, Volumenstrom) VALUES (?, ?, 50)",
                              new DbParam("@a", a), new DbParam("@b", b)));                                  // Paar steht schon
            Assert.True(Wirft("UPDATE Tab_Zonenluftstrom SET Volumenstrom = 0"));
            Assert.True(Wirft("UPDATE Tab_Bauteil SET Trennflaeche_Zuordnung = 'XX' WHERE ID_Zone = ?", new DbParam("@z", eg)));
            Assert.True(Wirft("UPDATE Tab_Bauteil SET ID_Nachbarzone = 999999 WHERE ID_Zone = ?", new DbParam("@z", eg)));

            // NO ACTION: Die Nachbarzone fällt nicht, solange eine Trennfläche auf sie zeigt ...
            Assert.True(Wirft("DELETE FROM Tab_Zone WHERE ID = ?", new DbParam("@z", og)));
            // ... die Zone, die die Trennfläche führt, schon — samt Trennfläche und Luftstrom (Kaskade).
            Assert.False(Wirft("DELETE FROM Tab_Zone WHERE ID = ?", new DbParam("@z", eg)));

            // Das Gebäude fällt mit allen Zonen in EINER Anweisung; NO ACTION prüft erst an ihrem Ende.
            Assert.True(new WizardCtrl().Del_Projekt_ZuordungGebäude(PROJEKT, GEBAEUDE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID IN (?, ?)", new DbParam("@a", eg), new DbParam("@b", og)));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zonenluftstrom"));
        }

        [Fact]
        public void Das_Zonenergebnis_bleibt_wenn_die_Zone_faellt_und_faellt_mit_dem_Lauf()
        {
            if (!_db.Vorhanden) return;
            (List<ZoneModel> z, _) = Geschrieben();
            const int ERGEBNIS = 900001;
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Ergebnis (ID, ID_Projekt, Bezeichner) VALUES (?, ?, 'G6b')",
                                           new DbParam("@e", ERGEBNIS), new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_ErgebnisGebaeude (ID, ID_Ergebnis, ID_Gebaeude, Merkplatz, Rechenweg, " +
                                           "Heizwaerme_Mwh, Spitze_Kw, SpitzeTagesmittel_Kw, Spitze95_Kw) VALUES (?, ?, ?, 0, 'VDI6007', 1.0, 2.0, 1.5, 1.0)",
                                           new DbParam("@i", ERGEBNIS), new DbParam("@e", ERGEBNIS), new DbParam("@g", GEBAEUDE));
            Assert.False(Wirft("INSERT INTO Tab_ErgebnisZone (ID, ID_ErgebnisGebaeude, ID_Zone, Rang, Bezeichner, IstBeheizt) VALUES (1, ?, ?, 2, 'OG', 0)",
                               new DbParam("@e", ERGEBNIS), new DbParam("@z", z[1].ID)));
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_ErgebnisZone (ID, ID_ErgebnisGebaeude, ID_Zone, Rang, Bezeichner, IstBeheizt, Heizwaerme_Mwh, " +
                                           "Spitze_Kw, MittlereRaumtemperatur_C, Ueberhitzungsstunden_H, DeltaThetaMax_K, DurchlaeufeMax, Musterwechsel_H) " +
                                           "VALUES (1, ?, ?, 1, 'EG', 1, 4.2, 3.1, 20.4, 12, 5.5, 4, 1)",
                                           new DbParam("@e", ERGEBNIS), new DbParam("@z", z[0].ID));
            foreach (string fremd in new[]
            {
                "UPDATE Tab_ErgebnisZone SET IstBeheizt = 2", "UPDATE Tab_ErgebnisZone SET Rang = 0",
                "UPDATE Tab_ErgebnisZone SET Ueberhitzungsstunden_H = 8761", "UPDATE Tab_ErgebnisZone SET DurchlaeufeMax = 0",
                "UPDATE Tab_ErgebnisZone SET Musterwechsel_H = -1", "UPDATE Tab_ErgebnisZone SET Heizwaerme_Mwh = 'viel'",
                "UPDATE Tab_ErgebnisZone SET ID_ErgebnisGebaeude = 7",
            })
                Assert.True(Wirft(fremd), fremd);

            // Die Zone fällt: das Ergebnis des Laufs bleibt, ohne Verweis.
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel>()).Ok);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone WHERE ID_Zone IS NULL"));
            // Der Lauf fällt: die Zeile mit ihm.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Ergebnis WHERE ID = ?", new DbParam("@e", ERGEBNIS));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone"));
        }

        // =================================================================================
        //  Der Schreibweg
        // =================================================================================

        [Fact]
        public void Der_Schreibweg_schluesselt_Nachbar_und_Luftstrom_um_und_dreht_das_Paar()
        {
            if (!_db.Vorhanden) return;
            (List<ZoneModel> z, List<ZonenluftstromModel> l) = Geschrieben();
            int eg = z[0].ID, og = z[1].ID;
            Assert.True(eg > 0 && og > 0);
            Assert.Equal(og, z[0].Bauteile[1].ID_Nachbarzone);
            Assert.Equal((Math.Min(eg, og), Math.Max(eg, og)), (l[0].ID_ZoneA, l[0].ID_ZoneB));
            Assert.True(l[0].ID > 0);

            var ctrl = new GebaeudeZonenCtrl();
            List<ZoneModel> gelesen = ctrl.LesenJeGebaeude(GEBAEUDE);
            BauteilModel decke = gelesen[0].Bauteile.Single(b => b.Bezeichner == "Decke EG/OG");
            Assert.Equal(og, decke.ID_Nachbarzone);
            Assert.Equal(DbWerte.TRENNFLAECHE_AW, decke.Trennflaeche_Zuordnung);
            Assert.Null(gelesen[0].Bauteile.Single(b => b.Bezeichner == "Wand EG").ID_Nachbarzone);

            ZonenluftstromModel strom = Assert.Single(ctrl.LuftstroemeJeGebaeude(GEBAEUDE));
            Assert.Equal((l[0].ID, Math.Min(eg, og), Math.Max(eg, og), 80.0), (strom.ID, strom.ID_ZoneA, strom.ID_ZoneB, strom.Volumenstrom));
            Dictionary<int, List<ZonenluftstromModel>> jeProjekt = ctrl.LuftstroemeJeProjekt(PROJEKT);
            Assert.Equal(new[] { GEBAEUDE }, jeProjekt.Keys);
            Assert.Equal(strom.ID, Assert.Single(jeProjekt[GEBAEUDE]).ID);
            Assert.Empty(ctrl.LuftstroemeJeGebaeude(ZonenRundlauf.GEBAEUDE_B));

            // Ein zweites Speichern ohne Änderung lässt jede Id stehen.
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, gelesen, ctrl.LuftstroemeJeGebaeude(GEBAEUDE)).Ok);
            Assert.Equal(strom.ID, Assert.Single(ctrl.LuftstroemeJeGebaeude(GEBAEUDE)).ID);
            Assert.Equal(decke.ID, ctrl.LesenJeGebaeude(GEBAEUDE)[0].Bauteile.Single(b => b.Bezeichner == "Decke EG/OG").ID);
        }

        [Fact]
        public void Die_Luftstroeme_werden_abgeglichen()
        {
            if (!_db.Vorhanden) return;
            (List<ZoneModel> z, List<ZonenluftstromModel> l) = Geschrieben();
            var ctrl = new GebaeudeZonenCtrl();
            int id = l[0].ID;

            // Ohne Liste bleiben die Luftströme stehen.
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, ctrl.LesenJeGebaeude(GEBAEUDE)).Ok);
            Assert.Equal(id, Assert.Single(ctrl.LuftstroemeJeGebaeude(GEBAEUDE)).ID);

            // Ein anderer Volumenstrom ändert die Zeile unter ihrer Id.
            List<ZonenluftstromModel> s = ctrl.LuftstroemeJeGebaeude(GEBAEUDE);
            s[0].Volumenstrom = 120;
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, ctrl.LesenJeGebaeude(GEBAEUDE), s).Ok);
            ZonenluftstromModel geaendert = Assert.Single(ctrl.LuftstroemeJeGebaeude(GEBAEUDE));
            Assert.Equal((id, 120.0), (geaendert.ID, geaendert.Volumenstrom));

            // Eine dritte Zone; das Paar wandert auf sie, ein zweites Paar kommt dazu.
            List<ZoneModel> gelesen = ctrl.LesenJeGebaeude(GEBAEUDE);
            gelesen.Add(new ZoneModel { ID = -7, Bezeichner = "Treppe", Nutzflaeche = 10, IstBeheizt = false });
            s = ctrl.LuftstroemeJeGebaeude(GEBAEUDE);
            s[0].ID_ZoneB = -7;
            s[0].ID_ZoneA = z[0].ID;
            s.Add(new ZonenluftstromModel { ID = -1, ID_ZoneA = -7, ID_ZoneB = z[1].ID, Volumenstrom = 30 });
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, gelesen, s).Ok);
            int treppe = gelesen[2].ID;
            List<ZonenluftstromModel> neu = ctrl.LuftstroemeJeGebaeude(GEBAEUDE);
            Assert.Equal(2, neu.Count);
            Assert.All(neu, x => Assert.True(x.ID_ZoneA < x.ID_ZoneB));
            Assert.Contains(neu, x => x.ID_ZoneA == Math.Min(z[0].ID, treppe) && x.ID_ZoneB == Math.Max(z[0].ID, treppe) && x.Volumenstrom == 120);
            Assert.Contains(neu, x => x.ID_ZoneA == Math.Min(z[1].ID, treppe) && x.ID_ZoneB == Math.Max(z[1].ID, treppe) && x.Volumenstrom == 30);

            // Eine leere Liste entfernt alle; das Entfernen einer Zone nimmt ihre Luftströme mit.
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, ctrl.LesenJeGebaeude(GEBAEUDE), new List<ZonenluftstromModel>()).Ok);
            Assert.Empty(ctrl.LuftstroemeJeGebaeude(GEBAEUDE));
        }

        [Fact]
        public void Die_Regeln_halten_den_Schreibweg_an_bevor_etwas_geschrieben_ist()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            long vorher = Zahl("SELECT COUNT(*) FROM Tab_Zone");

            (List<ZoneModel> z, List<ZonenluftstromModel> l) = Zwei();
            z[0].Bauteile[1].ID_Nachbarzone = -9;
            GebaeudeZonenCtrl.Ergebnis e = ctrl.SpeichernJeGebaeude(GEBAEUDE, z, l);
            Assert.False(e.Ok);
            Assert.Equal(F(R.ZONE_MSG_NACHBAR_FREMD, "Decke EG/OG", "EG", -9), e.Meldung);

            (z, l) = Zwei();
            l.Add(new ZonenluftstromModel { ID = -2, ID_ZoneA = -1, ID_ZoneB = -2, Volumenstrom = 10 });
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_DOPPELT, "EG", "OG"), ctrl.SpeichernJeGebaeude(GEBAEUDE, z, l).Meldung);

            // Eine positive Luftstrom-Id, die das Gebäude nicht führt.
            (z, l) = Zwei();
            l[0].ID = 4711;
            e = ctrl.SpeichernJeGebaeude(GEBAEUDE, z, l);
            Assert.False(e.Ok);
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_FREMD, 4711), e.Meldung);

            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Tab_Zone"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zonenluftstrom"));
        }

        /// <summary>
        /// Ein älterer Schemastand ohne S-G (iOS migriert nicht nach): keine Nachbarn, keine
        /// Luftströme — Lesen und das Speichern gewöhnlicher Zonen gehen, eine Trennfläche und ein
        /// Luftstrom werden benannt abgelehnt.
        /// </summary>
        [Fact]
        public void Ohne_Schritt_S_G_werden_Trennflaeche_und_Luftstrom_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(GebaeudeZonenanschluss.KopplungVorhanden());
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + ZonenkopplungSchema.TAB_ERGEBNIS + "\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\"");
            Assert.True(GebaeudeZonenanschluss.KopplungVorhanden());          // gemerkt, bis der Schritt sie verwirft
            GebaeudeZonenanschluss.ProbeVerwerfen();
            Assert.False(GebaeudeZonenanschluss.KopplungVorhanden());

            string erwartet = F(R.ZONE_MSG_OHNE_KOPPLUNG, ZonenkopplungSchema.SCHRITT);
            (List<ZoneModel> z, List<ZonenluftstromModel> l) = Zwei();
            Assert.Equal(erwartet, ctrl.SpeichernJeGebaeude(GEBAEUDE, z, l).Meldung);
            (z, _) = Zwei();
            Assert.Equal(erwartet, ctrl.SpeichernJeGebaeude(GEBAEUDE, z).Meldung);
            (z, l) = Zwei();
            z[0].Bauteile.RemoveAt(1);
            Assert.Equal(erwartet, ctrl.SpeichernJeGebaeude(GEBAEUDE, z, l).Meldung);

            // Gewöhnliche Zonen gehen; gelesen wird ohne Nachbar und ohne Luftstrom.
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, z, new List<ZonenluftstromModel>()).Ok);
            Assert.All(ctrl.LesenJeGebaeude(GEBAEUDE).SelectMany(x => x.Bauteile), b => Assert.Null(b.ID_Nachbarzone));
            Assert.Empty(ctrl.LuftstroemeJeGebaeude(GEBAEUDE));
            Assert.Empty(ctrl.LuftstroemeJeProjekt(PROJEKT));

            // W2: Der Zonenweg der Hülle nennt die Sperre — der Bauteildialog bietet keine Nachbarzone an,
            // „Luftaustausch …" ist weich gesperrt.
            GebaeudeZonenweg weg = GebaeudeKatalogHuelle.Zonenweg(PROJEKT, 0, GEBAEUDE);
            Assert.Equal(F(R.GEBZ_SPERRE_KOPPLUNG, ZonenkopplungSchema.SCHRITT), weg.KopplungSperre);
            Assert.Empty(weg.Luftstroeme);

            Assert.Equal(2, ZonenkopplungSchema.Ausfuehren(null));
            Assert.True(GebaeudeZonenanschluss.KopplungVorhanden());
        }

        // =================================================================================
        //  Der Zonenweg der Hülle (W2): Nachbarn und Luftströme durch den OK-Weg des Editors
        // =================================================================================

        /// <summary>
        /// Der Zonenweg liest Nachbar, „beheizt" und die Luftströme; unverändert gespeichert bleiben die
        /// Luftströme stehen, geändert werden sie abgeglichen; das Entfernen einer Zone im Arbeitsstand
        /// (Festlegung 8) setzt die Trennfläche der Nachbarzone auf „unbeheizt" und nimmt den Luftstrom mit.
        /// </summary>
        [Fact]
        public void Der_Zonenweg_traegt_Nachbarn_und_Luftstroeme_durch_den_Editor()
        {
            if (!_db.Vorhanden) return;
            Geschrieben();
            var ctrl = new GebaeudeZonenCtrl();
            GebaeudeZonenweg weg = GebaeudeKatalogHuelle.Zonenweg(PROJEKT, 0, GEBAEUDE);
            Assert.Null(weg.KopplungSperre);
            ZonenluftstromDaten strom = Assert.Single(weg.Luftstroeme);

            var a = new GebaeudeArbeitsstand();
            a.ZonenLaden(weg.Zonen, true, weg.Luftstroeme);
            ZoneDaten eg = a.Zonen.Single(z => z.Bezeichner == "EG");
            ZoneDaten og = a.Zonen.Single(z => z.Bezeichner == "OG");
            BauteilDaten decke = eg.Bauteile.Single(b => b.Bezeichner == "Decke EG/OG");
            Assert.Equal(og.Id, decke.IdNachbarzone);
            Assert.Equal(DbWerte.TRENNFLAECHE_AW, decke.TrennflaecheZuordnung);
            Assert.False(og.IstBeheizt);
            Assert.Equal(new[] { eg.Id, og.Id }.OrderBy(i => i), new[] { strom.IdZoneA!.Value, strom.IdZoneB!.Value }.OrderBy(i => i));
            Assert.Equal(80.0, strom.Volumenstrom);
            Assert.NotEmpty(weg.Hinweise!(a.Zonen));

            // Unverändert: der Schreibweg bekommt keine Luftströme und lässt sie stehen.
            Assert.Null(a.Zonenstand(true).Luftstroeme);
            Assert.Equal("", weg.Speichern!(a.Zonenstand(true)));
            Assert.Equal(strom.Id, Assert.Single(ctrl.LuftstroemeJeGebaeude(GEBAEUDE)).ID);

            // Geändert: abgeglichen unter derselben Id.
            a.LuftstroemeSetzen(new[] { new ZonenluftstromDaten { Id = strom.Id, IdZoneA = og.Id, IdZoneB = eg.Id, Volumenstrom = 120 } });
            Assert.True(a.LuftGeaendert);
            Assert.Equal("", weg.Pruefen!(a.Zonenstand(false)));
            Assert.Equal("", weg.Speichern!(a.Zonenstand(true)));
            ZonenluftstromModel gespeichert = Assert.Single(ctrl.LuftstroemeJeGebaeude(GEBAEUDE));
            Assert.Equal(strom.Id, gespeichert.ID);
            Assert.Equal(120.0, gespeichert.Volumenstrom);

            // Festlegung 8: das OG fällt - die Decke grenzt danach an einen unbeheizten Raum.
            Assert.True(a.ZoneEntfernen(og.Id));
            Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, decke.Randbedingung);
            Assert.Empty(a.Luftstroeme);
            Assert.Equal("", weg.Speichern!(a.Zonenstand(true)));
            BauteilModel d = Assert.Single(ctrl.LesenJeGebaeude(GEBAEUDE)).Bauteile.Single(b => b.Bezeichner == "Decke EG/OG");
            Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, d.Randbedingung);
            Assert.Null(d.ID_Nachbarzone);
            Assert.Null(d.Trennflaeche_Zuordnung);
            Assert.Empty(ctrl.LuftstroemeJeGebaeude(GEBAEUDE));
        }

        /// <summary>Die Regel der Luftströme ohne Fachklasse (Dialog „Luftaustausch") meldet dasselbe wie der Schreibweg.</summary>
        [Fact]
        public void Die_Luftstromregel_ohne_Fachklasse_meldet_dasselbe()
        {
            var zonen = new List<(int, string)> { (-1, "EG"), (-2, "OG") };
            Assert.Null(Zonenkopplungsregeln.LuftstroemePruefen(zonen, new[] { new Zonenkopplungsregeln.Luftstromangabe(-2, -1, 80) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_DOPPELT, "EG", "OG"), Zonenkopplungsregeln.LuftstroemePruefen(zonen,
                new[] { new Zonenkopplungsregeln.Luftstromangabe(-1, -2, 80), new Zonenkopplungsregeln.Luftstromangabe(-2, -1, 10) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_EIGEN, "EG"), Zonenkopplungsregeln.LuftstroemePruefen(zonen,
                new[] { new Zonenkopplungsregeln.Luftstromangabe(-1, -1, 80) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_ZONE, 7), Zonenkopplungsregeln.LuftstroemePruefen(zonen,
                new[] { new Zonenkopplungsregeln.Luftstromangabe(-1, 7, 80) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_WERT, "EG", "OG", (0.0).ToString("0.###", CultureInfo.CurrentCulture)),
                         Zonenkopplungsregeln.LuftstroemePruefen(zonen, new[] { new Zonenkopplungsregeln.Luftstromangabe(-1, -2, 0) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_MEHRDEUTIG, -1), Zonenkopplungsregeln.LuftstroemePruefen(
                new List<(int, string)> { (-1, "EG"), (-1, "EG2"), (-2, "OG") }, new[] { new Zonenkopplungsregeln.Luftstromangabe(-1, -2, 5) }));
        }

        // =================================================================================
        //  Die Kopierwege
        // =================================================================================

        [Fact]
        public void Der_Plan_traegt_Luftstrom_und_Zonenergebnis_mit_Filter_und_Versatz()
        {
            if (!_db.Vorhanden) return;
            var dup = new ProjektDuplizierenCtrl();
            Dictionary<string, ProjektDuplizierenCtrl.Spec> plan = dup.ErmittlePlan().ToDictionary(s => s.Tabelle, StringComparer.OrdinalIgnoreCase);

            ProjektDuplizierenCtrl.Spec luft = plan[ZonenkopplungSchema.TAB_LUFTSTROM];
            Assert.False(luft.Ergebnis);
            Assert.StartsWith("ID_ZoneA IN (SELECT ID FROM Tab_Zone", luft.Filter, StringComparison.Ordinal);
            ProjektDuplizierenCtrl.Spec ergebnis = plan[ZonenkopplungSchema.TAB_ERGEBNIS];
            Assert.True(ergebnis.Ergebnis);
            Assert.StartsWith("ID_ErgebnisGebaeude IN (SELECT ID FROM Tab_ErgebnisGebaeude", ergebnis.Filter, StringComparison.Ordinal);

            Assert.Equal(SchemaKatalog.TAB_ZONE, dup.ErmittleZieltabelle(SchemaKatalog.TAB_BAUTEIL, "ID_Nachbarzone", "ID"));
            Assert.Equal(SchemaKatalog.TAB_ZONE, dup.ErmittleZieltabelle(ZonenkopplungSchema.TAB_LUFTSTROM, "ID_ZoneA", "ID"));
            Assert.Equal(SchemaKatalog.TAB_ZONE, dup.ErmittleZieltabelle(ZonenkopplungSchema.TAB_LUFTSTROM, "ID_ZoneB", "ID"));
            Assert.Equal(SchemaKatalog.TAB_ZONE, dup.ErmittleZieltabelle(ZonenkopplungSchema.TAB_ERGEBNIS, "ID_Zone", "ID"));
            Assert.Equal(ErgebnisGebaeudeSchema.TAB, dup.ErmittleZieltabelle(ZonenkopplungSchema.TAB_ERGEBNIS, "ID_ErgebnisGebaeude", "ID"));
        }

        /// <summary>
        /// Der Wächterfall des Zonenergebnisses: Die Kopie lässt es aus wie jedes Ergebnis, der Transfer
        /// nimmt es mit — mit versetzter Zone, und ein NULL (Zone gelöscht) bleibt NULL.
        /// </summary>
        [Fact]
        public void Das_Zonenergebnis_reist_im_Transfer_und_nicht_in_der_Kopie()
        {
            if (!_db.Vorhanden) return;
            (List<ZoneModel> z, _) = Geschrieben();
            const int ERGEBNIS = 900002;
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Ergebnis (ID, ID_Projekt, Bezeichner) VALUES (?, ?, 'G6b')",
                                           new DbParam("@e", ERGEBNIS), new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_ErgebnisGebaeude (ID, ID_Ergebnis, ID_Gebaeude, Merkplatz, Rechenweg, " +
                                           "Heizwaerme_Mwh, Spitze_Kw, SpitzeTagesmittel_Kw, Spitze95_Kw) VALUES (?, ?, ?, 0, 'VDI6007', 1.0, 2.0, 1.5, 1.0)",
                                           new DbParam("@i", ERGEBNIS), new DbParam("@e", ERGEBNIS), new DbParam("@g", GEBAEUDE));
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_ErgebnisZone (ID, ID_ErgebnisGebaeude, ID_Zone, Rang, Bezeichner, IstBeheizt, Heizwaerme_Mwh) " +
                                           "VALUES (1, ?, ?, 1, 'EG', 1, 4.2), (2, ?, NULL, 2, 'Alt', 0, NULL)",
                                           new DbParam("@e", ERGEBNIS), new DbParam("@z", z[0].ID), new DbParam("@e2", ERGEBNIS));
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                                        new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " G6b");
            Assert.True(kopie > 0);
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone"));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Zonenluftstrom"));    // die Kopie trägt den Luftstrom

            string ordner = Path.Combine(Path.GetTempPath(), "epos-g6b-transfer-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "g6b.wpx");
                Assert.True(new ProjektExportImportCtrl().Exportieren(name, paket));
                using var ziel = new TestDatenbank();
                Assert.True(ziel.Vorhanden);
                int neu = new ProjektExportImportCtrl().Importieren(paket, name + " Transfer",
                    ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

                DataTable zeilen = DataRepository.GetDataTable(
                    "SELECT e.Bezeichner, e.ID_Zone, z.Bezeichner AS Zone, g.ID_Projekt FROM Tab_ErgebnisZone e " +
                    "INNER JOIN Tab_ErgebnisGebaeude eg ON eg.ID = e.ID_ErgebnisGebaeude " +
                    "INNER JOIN Tab_Gebaeude g ON g.ID = eg.ID_Gebaeude LEFT JOIN Tab_Zone z ON z.ID = e.ID_Zone ORDER BY e.Rang");
                Assert.Equal(2, zeilen.Rows.Count);
                Assert.Equal("EG", zeilen.Rows[0]["Zone"]);
                Assert.Equal(neu, Convert.ToInt32(zeilen.Rows[0]["ID_Projekt"], CultureInfo.InvariantCulture));
                Assert.Equal(DBNull.Value, zeilen.Rows[1]["ID_Zone"]);
                ZonenluftstromModel strom = Assert.Single(Assert.Single(new GebaeudeZonenCtrl().LuftstroemeJeProjekt(neu)).Value);
                Assert.True(strom.ID_ZoneA < strom.ID_ZoneB);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen kostet keinen Test */ }
            }
        }

        [Fact]
        public void Der_Transfer_in_eine_Datenbank_ohne_S_G_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            Geschrieben();
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                                        new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-g6b-ohne-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "g6b.wpx");
                Assert.True(new ProjektExportImportCtrl().Exportieren(name, paket));
                using var ziel = new TestDatenbank();
                DataRepository.ExecuteNonQuery("DROP TABLE \"" + ZonenkopplungSchema.TAB_ERGEBNIS + "\"");
                DataRepository.ExecuteNonQuery("DROP TABLE \"" + ZonenkopplungSchema.TAB_LUFTSTROM + "\"");
                GebaeudeZonenanschluss.ProbeVerwerfen();
                long projekte = Zahl("SELECT COUNT(*) FROM Tab_Projekt");
                int neu = new ProjektExportImportCtrl().Importieren(paket, name + " Transfer",
                    ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.Equal(-1, neu);
                Assert.Equal(F(R.ZONE_MSG_TRANSFER_OHNE_KOPPLUNG, ZonenkopplungSchema.SCHRITT), fehler);
                Assert.Equal(projekte, Zahl("SELECT COUNT(*) FROM Tab_Projekt"));
            }
            finally
            {
                GebaeudeZonenanschluss.ProbeVerwerfen();
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen kostet keinen Test */ }
            }
        }
    }
}
