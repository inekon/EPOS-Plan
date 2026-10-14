using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die geschlossene Hülle je Zone in der Modellprüfung</b> (Welle MA-3; Entwurf Geführter Modellaufbau 2.2 Gruppe 1,
    /// Mehrzonenkonzept 5.3): Flächenbilanz Σ A·n und Verankerung oben/unten, der Vorschlag „fehlendes Hüllbauteil aus der
    /// Körperfläche“ und seine Übernahme mit Rücknahme. Proben mit offener Hülle, je mit geschlossener Gegenprobe; ohne Datenbank.
    /// </summary>
    public sealed class ModellpruefungHuelleRegelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly string[] HUELLE =
        {
            Modellpruefung.HUELLE_OFFEN, Modellpruefung.HUELLE_OBEN_FEHLT, Modellpruefung.HUELLE_UNTEN_FEHLT, Modellpruefung.HUELLE_GESPIEGELT,
        };

        private static int _id = 1000;

        private static BauteilModel Bt(int zone, string name, string art, double flaeche, double neigung, string rand, double u,
                                       int? nachbar = null, double? azimut = null)
            => new BauteilModel
            {
                ID = ++_id, ID_Zone = zone, Bezeichner = name, Bauteilart = art, Flaeche = flaeche, Neigung = neigung, Azimut = azimut,
                Randbedingung = rand, U_Wert = u, ID_Nachbarzone = nachbar,
            };

        private static ZoneModel Zone(int id, string name, double nutzflaeche, params BauteilModel[] bauteile)
        {
            var z = new ZoneModel { ID = id, ID_Gebaeude = 1, Bezeichner = name, Nutzflaeche = nutzflaeche, Raumsolltemperatur_Tag = 20 };
            z.Bauteile.AddRange(bauteile);
            // Die Gegenwand zur Südwand von Wand(): Die Hülle schließt auch waagerecht.
            z.Bauteile.Add(Bt(id, "Wand Nord", DbWerte.BAUTEILART_AUSSENWAND, 120, 90, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.3, azimut: 0));
            return z;
        }

        private static BauteilModel Wand(int zone) => Bt(zone, "Wand", DbWerte.BAUTEILART_AUSSENWAND, 120, 90, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.3, azimut: 180);

        private static Modellpruefstand Stand(params ZoneModel[] zonen)
        {
            var s = new Modellpruefstand
            {
                IdGebaeude = 1, IdProjekt = 1, Nutzflaeche = 200, Raumhoehe = 2.5, FlaecheNutzer = 30, Luftwechselrate = 0.5, Bauweise = 50,
                KlimaregionVorhanden = true, Gebaeudeart = "Einfamilienhaus", Feiertagsland = "BY",
            };
            s.Zonen.AddRange(zonen);
            return s;
        }

        /// <summary>Zwei Geschosse: EG auf der Bodenplatte, die Geschossdecke führt das EG zum OG, das OG trägt das Dach.</summary>
        private static Modellpruefstand ZweiGeschosse(bool mitDach = true, bool mitBoden = true)
        {
            ZoneModel eg = Zone(1, "EG", 100, Wand(1), Bt(1, "Geschossdecke", DbWerte.BAUTEILART_DECKE, 100, 0, DbWerte.RANDBEDINGUNG_ZONE, 0.8, 2));
            if (mitBoden) eg.Bauteile.Add(Bt(1, "Boden", DbWerte.BAUTEILART_BODENPLATTE, 100, 180, DbWerte.RANDBEDINGUNG_ERDREICH, 0.35));
            ZoneModel og = Zone(2, "OG", 100, Wand(2));
            if (mitDach) og.Bauteile.Add(Bt(2, "Dach", DbWerte.BAUTEILART_DACH, 100, 0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.2));
            return Stand(eg, og);
        }

        private static List<Modellbefund> Huellbefunde(Modellpruefergebnis e)
            => e.Gruppe(Pruefgruppe.GeometrieBauteile).Befunde.Where(b => HUELLE.Contains(b.Code)).ToList();

        [Fact]
        public void Gegenprobe_geschlossene_Huelle_meldet_nichts()
        {
            Assert.Empty(Huellbefunde(Modellpruefung.Pruefen(ZweiGeschosse())));
        }

        [Fact]
        public void Fehlendes_Dach_ist_rot_und_der_Vorschlag_ergaenzt_es_aus_der_Koerperflaeche()
        {
            Modellpruefergebnis e = Modellpruefung.Pruefen(ZweiGeschosse(mitDach: false));
            Assert.Equal(Rechenstatus.NichtRechenfaehig, e.Status);
            Modellbefund b = Assert.Single(Huellbefunde(e));
            Assert.Equal(Modellpruefung.HUELLE_OBEN_FEHLT, b.Code);
            Assert.Equal(Pruefampel.Rot, b.Ampel);
            Assert.Equal(2, b.IdZone);
            Pruefvorschlag v = b.Vorschlag;
            Assert.NotNull(v);
            Assert.Equal(Vorschlagsart.Huellbauteil, v.Art);
            Assert.Equal(DbWerte.BAUTEILART_DACH, v.Name);
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, v.Randbedingung);
            Assert.Equal(0.0, v.Neigung);
            Assert.Equal(100.0, v.Flaeche.Value, 6);       // die gespiegelte Geschossdecke unten
            // Kein Dach im Gebäude → U des Typaufbaus (Vorgabe, Stufe C).
            Assert.Equal(Vorschlagsherkunft.Typaufbau, v.Herkunft);
            Assert.True(v.U > 0.0);

            // Nach der Übernahme (hier: das Bauteil des Vorschlags) ist die Hülle zu, die Zone gelb statt rot (nur U, Stufe C).
            Modellpruefstand s = ZweiGeschosse(mitDach: false);
            BauteilModel neu = Modellvorschlagsregeln.AlsBauteil(v, 2);
            neu.ID = 9999;
            s.Zonen[1].Bauteile.Add(neu);
            Modellpruefergebnis nach = Modellpruefung.Pruefen(s);
            Assert.Empty(Huellbefunde(nach));
            Assert.NotEqual(Rechenstatus.NichtRechenfaehig, nach.Status);
            Assert.Contains(nach.Gruppe(Pruefgruppe.AufbautenStoffwerte).Befunde, x => x.Code == Modellpruefung.NUR_U_WERT && x.IdBauteil == 9999);
        }

        [Fact]
        public void Fehlende_Bodenplatte_im_untersten_Geschoss_kommt_gegen_Erdreich_mit_dem_U_des_Gebaeudes()
        {
            Modellpruefstand s = ZweiGeschosse(mitBoden: false);
            // Eine zweite Bodenplatte des Gebäudes (Anbau) gibt das U vor.
            s.Zonen.Add(Zone(3, "Anbau", 30, Wand(3), Bt(3, "Boden Anbau", DbWerte.BAUTEILART_BODENPLATTE, 30, 180, DbWerte.RANDBEDINGUNG_ERDREICH, 0.42),
                             Bt(3, "Dach Anbau", DbWerte.BAUTEILART_DACH, 30, 0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.2)));
            Modellbefund b = Assert.Single(Huellbefunde(Modellpruefung.Pruefen(s)));
            Assert.Equal(Modellpruefung.HUELLE_UNTEN_FEHLT, b.Code);
            Assert.Equal(1, b.IdZone);
            Assert.Equal(DbWerte.BAUTEILART_BODENPLATTE, b.Vorschlag.Name);
            Assert.Equal(DbWerte.RANDBEDINGUNG_ERDREICH, b.Vorschlag.Randbedingung);
            Assert.Equal(180.0, b.Vorschlag.Neigung);
            Assert.Equal(Vorschlagsherkunft.Projekt, b.Vorschlag.Herkunft);
            Assert.Equal(0.42, b.Vorschlag.U.Value, 9);
        }

        [Fact]
        public void Offene_Trennflaeche_schliesst_beide_Zonen_mit_einem_Vorschlag()
        {
            // Die Geschossdecke führt nur 40 m² der 100 m²: EG nach oben, OG nach unten offen (je 60 %).
            Modellpruefstand s = ZweiGeschosse();
            s.Zonen[0].Bauteile.Single(x => x.Bezeichner == "Geschossdecke").Flaeche = 40;
            Modellpruefergebnis e = Modellpruefung.Pruefen(s);
            List<Modellbefund> h = Huellbefunde(e);
            Assert.Equal(2, h.Count);
            Assert.All(h, x => Assert.Equal(Modellpruefung.HUELLE_OFFEN, x.Code));
            Assert.All(h, x => Assert.Equal(Pruefampel.Gelb, x.Ampel));
            Modellbefund mit = Assert.Single(h, x => x.Vorschlag != null);
            Assert.Equal(1, mit.IdZone);
            Assert.Equal(DbWerte.BAUTEILART_DECKE, mit.Vorschlag.Name);
            Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, mit.Vorschlag.Randbedingung);
            Assert.Equal(2, mit.Vorschlag.IdNachbarzone);
            Assert.Equal(0.0, mit.Vorschlag.Neigung);
            Assert.Equal(60.0, mit.Vorschlag.Flaeche.Value, 6);
            Assert.Equal(0.8, mit.Vorschlag.U.Value, 9);       // wie die vorhandene Geschossdecke

            BauteilModel neu = Modellvorschlagsregeln.AlsBauteil(mit.Vorschlag, 1);
            neu.ID = 9998;
            s.Zonen[0].Bauteile.Add(neu);
            Assert.Empty(Huellbefunde(Modellpruefung.Pruefen(s)));
        }

        [Fact]
        public void Verankerte_Zone_mit_geneigter_Dachflaeche_bekommt_keinen_Vorschlag()
        {
            // Wie Projekt 1052: Die Dachfläche ist die geneigte Fläche (120 m²) mit Neigung 0°, die Zone ist nach unten an der
            // geschlossenen Nachbarzone verankert. Die Lücke liegt nicht an der Trennfläche — gelb, ohne Vorschlag.
            ZoneModel og = Zone(2, "Gäste", 100, Wand(2), Bt(2, "Dach", DbWerte.BAUTEILART_DACH, 120, 0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.2),
                                Bt(2, "Kellerdecke", DbWerte.BAUTEILART_DECKE, 100, 180, DbWerte.RANDBEDINGUNG_ZONE, 0.55, 3));
            ZoneModel k = Zone(3, "Keller", 100, Wand(3), Bt(3, "Kellerboden", DbWerte.BAUTEILART_BODENPLATTE, 100, 180, DbWerte.RANDBEDINGUNG_ERDREICH, 0.55));
            Modellpruefergebnis e = Modellpruefung.Pruefen(Stand(og, k));
            Modellbefund b = Assert.Single(Huellbefunde(e));
            Assert.Equal(Modellpruefung.HUELLE_OFFEN, b.Code);
            Assert.Equal(Pruefampel.Gelb, b.Ampel);
            Assert.Null(b.Vorschlag);
            Assert.NotEqual(Rechenstatus.NichtRechenfaehig, e.Status);
        }

        [Fact]
        public void Zone_nur_mit_Waenden_bekommt_Dach_und_Bodenplatte_aus_der_Nutzflaeche()
        {
            Modellpruefergebnis e = Modellpruefung.Pruefen(Stand(Zone(1, "Halle", 80, Wand(1))));
            List<Modellbefund> h = Huellbefunde(e);
            Assert.Equal(new[] { Modellpruefung.HUELLE_OBEN_FEHLT, Modellpruefung.HUELLE_UNTEN_FEHLT }, h.Select(x => x.Code));
            Assert.All(h, x => Assert.Equal(80.0, x.Vorschlag.Flaeche.Value, 6));
            Assert.Equal(new[] { DbWerte.BAUTEILART_DACH, DbWerte.BAUTEILART_BODENPLATTE }, h.Select(x => x.Vorschlag.Name));
        }

        [Fact]
        public void Gespiegelte_Bodenplatte_ist_benannt_Gegenprobe_nicht()
        {
            Modellpruefstand s = ZweiGeschosse();
            s.Zonen[0].Bauteile.Single(x => x.Bezeichner == "Boden").Neigung = 0;
            Assert.Contains(Huellbefunde(Modellpruefung.Pruefen(s)), x => x.Code == Modellpruefung.HUELLE_GESPIEGELT && x.Ampel == Pruefampel.Gelb);
            Assert.False(Modellpruefung.Gespiegelt(ZweiGeschosse().Zonen[0].Bauteile.Single(x => x.Bezeichner == "Boden")));
        }

        [Fact]
        public void Befund_und_Hinweis_im_Reiter_Zonen_rechnen_dieselbe_Bilanz()
        {
            Modellpruefstand s = ZweiGeschosse();
            s.Zonen[0].Bauteile.Single(x => x.Bezeichner == "Geschossdecke").Flaeche = 40;
            IReadOnlyList<string> hinweise = Zonenkopplungsregeln.Hinweise(s.Zonen);
            List<string> texte = Huellbefunde(Modellpruefung.Pruefen(s)).Select(x => x.Text).ToList();
            Assert.Equal(hinweise.Count, texte.Count);
            Assert.Equal(hinweise, texte);
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Texte_der_Huelle_in_beiden_Sprachen(string kultur)
        {
            CultureInfo c = CultureInfo.GetCultureInfo(kultur);
            foreach (string schluessel in HUELLE.Select(x => "MP_BEFUND_" + x).Concat(new[]
                     {
                         "MP_VORSCHLAG_HUELLBAUTEIL_PROJEKT", "MP_VORSCHLAG_HUELLBAUTEIL_TYP", "MP_RAND_AUSSENLUFT", "MP_RAND_ERDREICH",
                         "MP_RAND_ZONE", "MP_BAUTEIL_ERGAENZT",
                     }))
                Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, c)), schluessel);
        }
    }

    /// <summary>
    /// Die Übernahme des Hüllbauteils mit Rücknahme gegen die Arbeitskopie der Testdatenbank (Projekt 1054; die Datei bleibt
    /// unberührt): Ohne Kellerboden ist der Keller nach unten offen (rot); „Vorschlag übernehmen“ legt die Bodenplatte gegen
    /// Erdreich mit der Fläche der Kellerdecken an — gelb statt rot —, die Rücknahme löscht sie.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ModellpruefungHuelleDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static int Gebaeude(int projekt)
            => Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT g.\"ID\" FROM \"Tab_Gebaeude\" g INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE z.\"ID_Projekt\" = ? ORDER BY g.\"ID\"", new DbParam("@p", projekt)).Rows[0][0], CultureInfo.InvariantCulture);

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.GetDataTable(sql, p).Rows[0][0], CultureInfo.InvariantCulture);

        [Fact]
        public void Referenzprojekt_1054_schliesst_die_Huelle_jeder_Zone()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            Modellpruefergebnis e = new ModellpruefungCtrl(g).Pruefen();
            List<Modellbefund> h = e.Gruppe(Pruefgruppe.GeometrieBauteile).Befunde.Where(b => b.Code.StartsWith("HUELLE_", StringComparison.Ordinal)
                                                                                           && b.Code != Modellpruefung.HUELLE_OHNE_AUSSENWAND
                                                                                           && b.Code != Modellpruefung.HUELLE_OHNE_DACH
                                                                                           && b.Code != Modellpruefung.HUELLE_OHNE_BODEN).ToList();
            // Die Dächer der beheizten Zonen sind Satteldächer mit 42° (Nord- und Südhälfte), der Keller schließt.
            Assert.Empty(h);
            Assert.DoesNotContain(e.Befunde, b => b.Code == Modellpruefung.AZIMUT_FEHLT);
            Assert.NotEqual(Rechenstatus.NichtRechenfaehig, e.Status);
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(g);
            for (int i = 0; i < zonen.Count; i++)
            {
                Zonenkopplungsregeln.Huellbilanz b = Zonenkopplungsregeln.Bilanz(zonen, i);
                Assert.True(b.AbweichungSenkrecht < Zonenkopplungsregeln.HUELLE_ABWEICHUNG_HINWEIS,
                            zonen[i].Bezeichner + ": " + b.AbweichungSenkrecht.ToString("P1", CultureInfo.InvariantCulture));
            }
        }

        [Fact]
        public void Fehlender_Kellerboden_wird_ergaenzt_und_zurueckgenommen()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            int keller = Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ? AND \"Bezeichner\" = 'Keller'", new DbParam("@g", g)).Rows[0][0],
                CultureInfo.InvariantCulture);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("DELETE FROM \"Tab_Bauteil\" WHERE \"ID_Zone\" = ? AND \"Bauteilart\" = 'BODENPLATTE'", new DbParam("@z", keller));
                v.Commit();
            }
            const string BODEN = "SELECT COUNT(*) FROM \"Tab_Bauteil\" WHERE \"ID_Zone\" = ? AND \"Bauteilart\" = 'BODENPLATTE'";
            var ctrl = new ModellpruefungCtrl(g);
            Modellpruefergebnis vor = ctrl.Pruefen();
            Assert.Equal(Rechenstatus.NichtRechenfaehig, vor.Status);
            Modellbefund b = Assert.Single(vor.Befunde, x => x.Code == Modellpruefung.HUELLE_UNTEN_FEHLT);
            Assert.Equal(keller, b.IdZone);
            Assert.Equal(Vorschlagsart.Huellbauteil, b.Vorschlag.Art);
            Assert.Equal(DbWerte.RANDBEDINGUNG_ERDREICH, b.Vorschlag.Randbedingung);

            Uebernahmeergebnis u = ctrl.Uebernehmen(Pruefgruppe.GeometrieBauteile);
            Assert.True(u.Ok, u.Meldung);
            Assert.Equal(1, Zahl(BODEN, new DbParam("@z", keller)));
            Assert.DoesNotContain(u.Neu.Befunde, x => x.Code == Modellpruefung.HUELLE_UNTEN_FEHLT);
            Assert.Equal(Rechenstatus.RechnetMitVorgaben, u.Neu.Status);
            // Stufe C: das neue Bauteil trägt nur ein U — die Gruppe 2 schlägt den Aufbau vor.
            Assert.Contains(u.Neu.Gruppe(Pruefgruppe.AufbautenStoffwerte).Befunde,
                            x => x.Code == Modellpruefung.NUR_U_WERT && x.IdZone == keller && x.Vorschlag != null);
            Assert.True(ctrl.KannZuruecknehmen(Pruefgruppe.GeometrieBauteile));

            Uebernahmeergebnis r = ctrl.Zuruecknehmen(Pruefgruppe.GeometrieBauteile);
            Assert.True(r.Ok, r.Meldung);
            Assert.Equal(0, Zahl(BODEN, new DbParam("@z", keller)));
            Assert.Equal(Rechenstatus.NichtRechenfaehig, r.Neu.Status);
        }
    }
}
