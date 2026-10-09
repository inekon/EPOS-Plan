using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zonencontroller nach Ebene</b> (Welle ZK-b): <see cref="GebaeudeZonenCtrl"/> liest und schreibt die Zonen eines
    /// Katalogsatzes in den Katalogzwillingen — Zonen, Bauteile mit Nachbarzone und Aufbau aus dem Aufbaukatalog, Luftströme
    /// und die Konditionierung je Katalogzone —, ändert und löscht sie über die Ids, lässt die Projekttabellen unberührt,
    /// lehnt einen ausgelieferten Satz, eine fremde Zone und einen Aufbau außerhalb des Aufbaukatalogs ab; die Übernahme
    /// ins Projekt kopiert das Geschriebene. Dazu <see cref="Konditionierungsarbeit"/> mit einer Katalogzone (ohne Datenbank).
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeKatalogZonenCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private bool Bereit() => _db.Vorhanden && ZonenKatalogSchema.Lesbar() && KonditionierungSchema.Lesbar();

        private static ZoneModel Zone(int id, string name, double flaeche, params BauteilModel[] bauteile)
        {
            var z = new ZoneModel { ID = id, Bezeichner = name, Nutzflaeche = flaeche, IstBeheizt = true, Herkunft = DbWerte.HERKUNFT_MANUELL };
            z.Bauteile.AddRange(bauteile);
            return z;
        }

        private static BauteilModel Wand(int id, string name, int? aufbau = null)
            => new BauteilModel { ID = id, Bezeichner = name, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 20, Azimut = 180,
                                  U_Wert = 0.3, ID_Aufbau = aufbau };

        private static BauteilModel Trennwand(int id, int nachbar)
            => new BauteilModel { ID = id, Bezeichner = "Trennwand", Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = 12.5,
                                  U_Wert = 1.2, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = nachbar,
                                  Trennflaeche_Zuordnung = "IW" };

        /// <summary>Ein eigener Katalogsatz ohne Zonen und ohne Konditionierung (Duplikat des ersten Satzes).</summary>
        private static int SatzAnlegen(string name)
        {
            long quelle = Zahl("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\"");
            Katalogkopie.Ergebnis k = GebaeudeStammCtrl.Duplizieren((int)quelle, name);
            Assert.True(k.Ok, k.Meldung);
            DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude_Stamm\" = ?", new DbParam("@s", k.Id));
            DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Gebaeude_Stamm\" = ?", new DbParam("@s", k.Id));
            DataRepository.ExecuteSQL("DELETE FROM \"Tab_Zone_STAMM\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@s", k.Id));
            return (int)k.Id;
        }

        private static Konditionierungsstand Geraeteanteil(double anteil)
            => Konditionierungsstand.Leer(Kalendereigentuemer.Katalogzone, null)
                .MitVorgabe(Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(anteil));

        [Fact]
        public void Katalogzonen_schreiben_lesen_aendern_und_loeschen_samt_Bauteilen_Luftstrom_und_Konditionierung()
        {
            if (!Bereit()) return;
            int stamm = SatzAnlegen("ZK-b Probe Schreiben");
            object aufbauRoh = DataRepository.ExecuteScalar("SELECT MIN(\"ID\") FROM \"Tab_Bauteilaufbau_STAMM\"");
            int? aufbau = aufbauRoh == null || aufbauRoh == DBNull.Value ? null : Convert.ToInt32(aufbauRoh, CultureInfo.InvariantCulture);
            long projektzonen = Zahl("SELECT COUNT(*) FROM \"Tab_Zone\"");
            var ctrl = new GebaeudeZonenCtrl();

            // Anlegen: zwei Zonen, eine Trennwand mit vorläufiger Nachbarzone, ein Luftstrom, Konditionierung an „Nord".
            var zonen = new List<ZoneModel>
            {
                Zone(-1, "Nord", 60, Wand(-1, "Wand Nord", aufbau)),
                Zone(-2, "Süd", 40, Wand(-2, "Wand Süd"), Trennwand(-3, -1)),
            };
            var luft = new List<ZonenluftstromModel> { new ZonenluftstromModel { ID_ZoneA = -1, ID_ZoneB = -2, Volumenstrom = 50 } };
            GebaeudeZonenCtrl.Schreibergebnis e = ctrl.Schreiben(stamm, zonen, luft,
                new Dictionary<int, Konditionierungsstand> { [-1] = Geraeteanteil(0.5) }, Zonenebene.Katalog);
            Assert.True(e.Ok, e.Meldung);
            int nord = e.Zonen[-1], sued = e.Zonen[-2];
            Assert.Equal(1, e.KonditionierungGeschrieben);
            Assert.Equal(projektzonen, Zahl("SELECT COUNT(*) FROM \"Tab_Zone\""));

            // Lesen über dieselbe Ebene; die Projektebene kennt die Ids nicht.
            List<ZoneModel> gelesen = ctrl.LesenJeGebaeude(stamm, Zonenebene.Katalog);
            Assert.Equal(new[] { "Nord", "Süd" }, gelesen.Select(z => z.Bezeichner).ToArray());
            Assert.Equal(aufbau, gelesen[0].Bauteile.Single().ID_Aufbau);
            BauteilModel trenn = gelesen[1].Bauteile.Single(b => b.Bezeichner == "Trennwand");
            Assert.Equal(nord, trenn.ID_Nachbarzone);
            Assert.Equal("IW", trenn.Trennflaeche_Zuordnung);
            ZonenluftstromModel strom = Assert.Single(ctrl.LuftstroemeJeGebaeude(stamm, Zonenebene.Katalog));
            Assert.Equal((Math.Min(nord, sued), Math.Max(nord, sued)), (strom.ID_ZoneA, strom.ID_ZoneB));
            Assert.Empty(ctrl.LesenJeGebaeude(stamm));
            Konditionierungsstand kond = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Katalogzone(stamm, nord), out string m);
            Assert.Null(m);
            Assert.True(Geraeteanteil(0.5).Gleich(kond, mitBestand: false));

            // Die Übernahme ins Projekt kopiert das Geschriebene (Schritt ZK).
            int idGebaeude = Uebernehmen(stamm);
            Assert.Equal(new[] { "Nord", "Süd" }, ctrl.LesenJeGebaeude(idGebaeude).Select(z => z.Bezeichner).ToArray());
            Assert.Single(ctrl.LuftstroemeJeGebaeude(idGebaeude));

            // Ändern und Löschen über die Ids: „Süd" umbenannt ohne Trennwand, „Nord" fällt samt Bauteil,
            // Luftstrom und Konditionierung (Kaskade).
            ZoneModel s2 = gelesen[1];
            s2.Bezeichner = "Süd neu";
            s2.Bauteile.RemoveAll(b => b.Bezeichner == "Trennwand");
            Assert.True(ctrl.SpeichernJeGebaeude(stamm, new List<ZoneModel> { s2 }, new List<ZonenluftstromModel>(), Zonenebene.Katalog).Ok);
            ZoneModel rest = Assert.Single(ctrl.LesenJeGebaeude(stamm, Zonenebene.Katalog));
            Assert.Equal((sued, "Süd neu"), (rest.ID, rest.Bezeichner));
            Assert.Single(rest.Bauteile);
            Assert.Empty(ctrl.LuftstroemeJeGebaeude(stamm, Zonenebene.Katalog));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Zone_Stamm\" = ?", nord));

            // Eine leere Liste entfernt alle Zonen des Satzes.
            Assert.True(ctrl.SpeichernJeGebaeude(stamm, new List<ZoneModel>(), null, Zonenebene.Katalog).Ok);
            Assert.Equal(0, Zonenkopie.Anzahl(Zonenebene.Katalog, stamm));
        }

        [Fact]
        public void Ein_ausgelieferter_Katalogsatz_wird_benannt_abgelehnt()
        {
            if (!Bereit()) return;
            int stamm = SatzAnlegen("ZK-b Probe Schloss");
            DataRepository.ExecuteSQL("UPDATE \"Tab_Gebaeude_STAMM\" SET \"ReadOnly\" = 1 WHERE \"ID\" = ?", new DbParam("@s", stamm));

            GebaeudeZonenCtrl.Ergebnis e = new GebaeudeZonenCtrl().SpeichernJeGebaeude(
                stamm, new List<ZoneModel> { Zone(-1, "Nord", 60, Wand(-1, "Wand")) }, null, Zonenebene.Katalog);

            Assert.False(e.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ZONE_MSG_KATALOG_GESPERRT, e.Meldung);
            Assert.Equal(0, Zonenkopie.Anzahl(Zonenebene.Katalog, stamm));
        }

        [Fact]
        public void Eine_Projektzone_und_ein_Aufbau_ausserhalb_des_Aufbaukatalogs_sind_im_Katalog_fremd()
        {
            if (!Bereit()) return;
            int stamm = SatzAnlegen("ZK-b Probe Fremd");
            var ctrl = new GebaeudeZonenCtrl();

            long projektzone = Zahl("SELECT COALESCE(MAX(\"ID\"), 0) FROM \"Tab_Zone\"") + 1000;
            GebaeudeZonenCtrl.Ergebnis zone = ctrl.SpeichernJeGebaeude(
                stamm, new List<ZoneModel> { Zone((int)projektzone, "Fremd", 60, Wand(-1, "Wand")) }, null, Zonenebene.Katalog);
            Assert.False(zone.Ok);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.ZONE_MSG_FREMD, projektzone), zone.Meldung);

            int fremd = (int)Zahl("SELECT COALESCE(MAX(\"ID\"), 0) FROM \"Tab_Bauteilaufbau_STAMM\"") + 1000;
            GebaeudeZonenCtrl.Ergebnis aufbau = ctrl.SpeichernJeGebaeude(
                stamm, new List<ZoneModel> { Zone(-1, "Nord", 60, Wand(-1, "Wand", fremd)) }, null, Zonenebene.Katalog);
            Assert.False(aufbau.Ok);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.BAUTEIL_MSG_AUFBAU_FREMD, "Wand", fremd), aufbau.Meldung);
            Assert.Equal(0, Zonenkopie.Anzahl(Zonenebene.Katalog, stamm));
        }

        [Fact]
        public void Einen_Katalogsatz_den_es_nicht_gibt_nennt_der_Schreibweg()
        {
            if (!Bereit()) return;
            int fehlt = (int)Zahl("SELECT COALESCE(MAX(\"ID\"), 0) FROM \"Tab_Gebaeude_STAMM\"") + 1000;
            GebaeudeZonenCtrl.Ergebnis e = new GebaeudeZonenCtrl().SpeichernJeGebaeude(
                fehlt, new List<ZoneModel> { Zone(-1, "Nord", 60, Wand(-1, "Wand")) }, null, Zonenebene.Katalog);
            Assert.False(e.Ok);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.ZONE_MSG_GEBAEUDE_FEHLT, fehlt), e.Meldung);
        }

        // =================================================================================
        //  Konditionierungsarbeit mit einer Katalogzone (ohne Datenbank)
        // =================================================================================

        [Fact]
        public void Die_Katalogzone_folgt_den_Regeln_der_Zone()
        {
            Assert.True(Konditionierungsarbeit.Zonenart(Kalendereigentuemer.Zone));
            Assert.True(Konditionierungsarbeit.Zonenart(Kalendereigentuemer.Katalogzone));
            Assert.False(Konditionierungsarbeit.Zonenart(Kalendereigentuemer.Katalogbau));
            Assert.False(Konditionierungsarbeit.Zonenart(Kalendereigentuemer.Gebaeude));

            // Eine geleerte Bestandszelle heißt an der Katalogzone wie an der Zone „wie das Gebäude".
            foreach (Kalendereigentuemer art in new[] { Kalendereigentuemer.Zone, Kalendereigentuemer.Katalogzone })
            {
                Konditionierungsstand vor = Konditionierungsstand.Leer(art, new Matrixeingang { SollTag = 21.0 });
                Ebenenergebnis e = Konditionierungsarbeit.Eintragen(vor, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG,
                                                                    Matrixzelle.Leer);
                Assert.True(e.Ok, e.Meldung);
                Assert.Null(e.Stand.Bestand.SollTag);
            }

            // Am Katalogbau bleibt der Wert stehen (er erbt von niemandem).
            Ebenenergebnis bau = Konditionierungsarbeit.Eintragen(
                Konditionierungsstand.Leer(Kalendereigentuemer.Katalogbau, new Matrixeingang { SollTag = 21.0 }),
                Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG, Matrixzelle.Leer);
            Assert.Equal(21.0, bau.Stand.Bestand.SollTag);
        }

        /// <summary>Die Übernahme ins erste Projekt über eine neue Zuordnungszeile (wie der Assistent).</summary>
        private static int Uebernehmen(int stamm)
        {
            long projekt = Zahl("SELECT MIN(ID) FROM \"Tab_Projekt\"");
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Z_ProjektGebaeude\" (\"ID_Projekt\", \"Wohnflaeche_Waermebedarf\", " +
                "\"Einheit_Waermebedarf_Wohnflaeche\", \"Jahresnutzungsgrad\", \"dezWarmwasserbereitung\") VALUES (?, 100.0, ?, 0.9, 0)",
                new DbParam("@p", projekt), new DbParam("@e", "m2"));
            long z = Zahl("SELECT MAX(ID) FROM \"Z_ProjektGebaeude\"");
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT \"Bezeichner\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = ?",
                                                                        new DbParam("@s", stamm)), CultureInfo.InvariantCulture);
            return new GebaeudeStammCtrl().CopyFromStamm(stamm, name, (int)projekt, (int)z);
        }

        private static long Zahl(string sql, params object[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray()),
                               CultureInfo.InvariantCulture);
    }
}
