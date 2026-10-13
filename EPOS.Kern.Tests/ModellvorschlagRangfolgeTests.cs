using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die vertiefte Rangfolge der Aufbauvorschläge</b> (Welle MA-4, Entwurf Geführter Modellaufbau 2.3): je Stufe ein Fall
    /// mit Gegenprobe — Projektaufbau, Katalogaufbau nach Typgruppe des Namens und Aufbauklasse, Typaufbau mit Bauart aus dem
    /// Namen —, der Namensabgleich Schichtsatz → Typgruppe (Rest BA-2) und die Stufe C mit Baualtersklasse in einem Schritt.
    /// </summary>
    public sealed class ModellvorschlagRangfolgeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyCollection<string> MAUERWERK = new[] { "MAUERWERK" };
        private static readonly IReadOnlyCollection<string> BETON = new[] { "BETON" };

        /// <summary>Ein Gebäude ohne Projektaufbau, im Katalog eine Betonwand (U 0,5) und eine Mauerwerkswand (U 0,6).</summary>
        private static Modellpruefstand Stand()
        {
            var s = new Modellpruefstand { IdGebaeude = 1, IdProjekt = 1 };
            var z = new ZoneModel { ID = 11, ID_Gebaeude = 1, Bezeichner = "Zone" };
            z.Bauteile.Add(new BauteilModel { ID = 101, ID_Zone = 11, Bezeichner = "Wand Süd", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 50,
                                              U_Wert = 0.5, Neigung = 90, Azimut = 180, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT });
            s.Zonen.Add(z);
            s.Katalogaufbauten.Add(new Aufbaukennung(51, "AW Beton", DbWerte.BAUTEILART_AUSSENWAND, false, 0.5, 300, false, 0, "AUSSENWAND", BETON));
            s.Katalogaufbauten.Add(new Aufbaukennung(52, "AW Mauerwerk", DbWerte.BAUTEILART_AUSSENWAND, false, 0.6, 250, false, 1, "AUSSENWAND", MAUERWERK));
            return s;
        }

        private static BauteilModel Wand(Modellpruefstand s) => s.Zonen[0].Bauteile[0];

        // ---------------------------------------------------------------- Namensabgleich

        [Theory]
        [InlineData("Mauerwerk 36,5", "MAUERWERK", 4)]
        [InlineData("Brick wall", "MAUERWERK", 4)]
        [InlineData("Außenwand Porenbeton", "MAUERWERK", 4)]
        [InlineData("Hochlochziegel", "MAUERWERK", 5)]
        [InlineData("Ziegelwand 24", "MAUERWERK", 5)]
        [InlineData("Stahlbetonwand", "BETON", 5)]
        [InlineData("Reinforced concrete wall", "BETON", 4)]
        [InlineData("Holzständerwand", "HOLZ", 5)]
        [InlineData("Timber frame wall", "HOLZ", 4)]
        [InlineData("Trapezblech gedämmt", "METALLE", 4)]
        [InlineData("Holz Beton", "BETON", 4)]     // das längste Synonym gewinnt
        public void Name_trifft_seine_Typgruppe(string name, string gruppe, int stufe)
        {
            Typgruppentreffer t = Aufbaugruppenabgleich.Abgleichen(name);
            Assert.NotNull(t);
            Assert.Equal(gruppe, t.Gruppe);
            Assert.Equal(stufe, (int)t.Stufe);                // 4 = N4 Synonym, 5 = N5 Teilwort
        }

        [Theory]
        [InlineData("Wand Nord")]
        [InlineData("Holzfaserdämmplatte")]       // Gegenwort: Dämmstoff, keine Typgruppe
        [InlineData("Wood fibre board")]
        [InlineData("")]
        public void Name_ohne_Typgruppe(string name) => Assert.Null(Aufbaugruppenabgleich.Abgleichen(name));

        [Fact]
        public void Gleich_lange_Synonyme_verschiedener_Gruppen_sind_mehrdeutig()
        {
            Assert.Null(Aufbaugruppenabgleich.Abgleichen("Stahl Mauer"));     // stahl/mauer: je 5 Zeichen, zwei Gruppen
            Assert.Equal("MAUERWERK", Aufbaugruppenabgleich.Abgleichen("Stahl Mauerwerk").Gruppe);
        }

        [Fact]
        public void Schichtsatz_trifft_die_Gruppe_der_meisten_Schichten()
        {
            Assert.Equal("MAUERWERK", Aufbaugruppenabgleich.Abgleichen(new[] { "Gipsputz", "Vollziegel", "Kalksandstein", "EPS" }).Gruppe);
            Assert.Null(Aufbaugruppenabgleich.Abgleichen(new[] { "Ziegel", "Stahlbeton" }));          // Gleichstand
            Assert.Null(Aufbaugruppenabgleich.Abgleichen(new[] { "Gipsputz", "Mineralwolle" }));     // keine Typgruppe
            Assert.Equal(new[] { "BETON", "MAUERWERK" },
                         Aufbaugruppenabgleich.Typgruppen("Ziegelwand", new[] { "PUTZE", "BETON", "DAEMMSTOFFE", null }).OrderBy(x => x));
        }

        // ---------------------------------------------------------------- Rangfolge

        [Fact]
        public void Nur_U_Name_trifft_Mauerwerk_ergibt_den_Katalogaufbau_der_Gruppe()
        {
            Modellpruefstand s = Stand();
            Wand(s).Bezeichner = "Mauerwerk 36,5";
            Pruefvorschlag v = Modellvorschlagsregeln.Aufbau(s, Wand(s));
            Assert.Equal(Vorschlagsherkunft.Katalogaufbau, v.Herkunft);
            Assert.Equal(52, v.IdAufbau);                    // die Mauerwerkswand, obwohl die Betonwand näher am U liegt
            Assert.Null(v.UBauteil);                          // Stufe B: das U der Datei bleibt
            Assert.StartsWith("Stufe 2", v.Text);
            Assert.Contains("Mauerwerk 36,5", v.Text);

            // Gegenprobe: ohne treffenden Namen entscheidet die U-Nähe.
            Wand(s).Bezeichner = "Wand Süd";
            Assert.Equal(51, Modellvorschlagsregeln.Aufbau(s, Wand(s)).IdAufbau);
            // Gegenprobe: der Name trifft, aber die Mauerwerkswand liegt außerhalb des U-Bands → die Betonwand.
            Wand(s).Bezeichner = "Brick wall";
            Wand(s).U_Wert = 0.42;
            Assert.Equal(51, Modellvorschlagsregeln.Aufbau(s, Wand(s)).IdAufbau);
        }

        [Fact]
        public void Stufe1_Projektaufbau_geht_vor_dem_treffenden_Katalogaufbau()
        {
            Modellpruefstand s = Stand();
            Wand(s).Bezeichner = "Mauerwerk";
            var z = s.Zonen[0];
            z.Bauteile.Add(new BauteilModel { ID = 102, ID_Zone = 11, Bezeichner = "Wand Ost", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 30,
                                              U_Wert = 0.3, Neigung = 90, Azimut = 90, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT, ID_Aufbau = 7 });
            s.Projektaufbauten[7] = new Aufbaukennung(7, "AW Projekt", DbWerte.BAUTEILART_AUSSENWAND, false, 0.3, 200, false, 0);
            Pruefvorschlag v = Modellvorschlagsregeln.Aufbau(s, Wand(s));
            Assert.Equal(Vorschlagsherkunft.Projektaufbau, v.Herkunft);
            Assert.StartsWith("Stufe 1", v.Text);
            // Gegenprobe: an anderer Randbedingung zählt der Projektaufbau nicht.
            Wand(s).Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH;
            Assert.NotEqual(Vorschlagsherkunft.Projektaufbau, Modellvorschlagsregeln.Aufbau(s, Wand(s)).Herkunft);
        }

        [Fact]
        public void Stufe2_Aufbauklasse_waehlt_Flach_oder_Steildach()
        {
            var s = new Modellpruefstand { IdGebaeude = 1, IdProjekt = 1 };
            var z = new ZoneModel { ID = 11, ID_Gebaeude = 1, Bezeichner = "Zone" };
            var dach = new BauteilModel { ID = 101, ID_Zone = 11, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 40,
                                          U_Wert = 0.2, Neigung = 0, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT };
            z.Bauteile.Add(dach);
            s.Zonen.Add(z);
            s.Katalogaufbauten.Add(new Aufbaukennung(61, "Steildach", DbWerte.BAUTEILART_DACH, false, 0.2, 50, false, 0, "STEILDACH"));
            s.Katalogaufbauten.Add(new Aufbaukennung(62, "Flachdach", DbWerte.BAUTEILART_DACH, false, 0.22, 250, false, 1, "FLACHDACH"));
            Assert.Equal("FLACHDACH", Modellvorschlagsregeln.Aufbauklasse(dach));
            Assert.Equal(62, Modellvorschlagsregeln.Aufbau(s, dach).IdAufbau);
            // Gegenprobe: geneigt → Steildach.
            dach.Neigung = 40;
            Assert.Equal("STEILDACH", Modellvorschlagsregeln.Aufbauklasse(dach));
            Assert.Equal(61, Modellvorschlagsregeln.Aufbau(s, dach).IdAufbau);
        }

        [Fact]
        public void Stufe3_Typaufbau_folgt_der_Bauart_des_Namens()
        {
            Modellpruefstand s = Stand();
            s.Katalogaufbauten.Clear();
            Wand(s).Bezeichner = "Holzständerwand";
            Wand(s).U_Wert = 0.25;
            Pruefvorschlag v = Modellvorschlagsregeln.Aufbau(s, Wand(s));
            Assert.Equal(Vorschlagsherkunft.Typaufbau, v.Herkunft);
            Assert.Equal(TypaufbauSaattabelle.AW_HOLZLEICHTBAU, v.Typaufbau);
            Assert.StartsWith("Stufe 3", v.Text);
            // Gegenprobe: ohne Holz im Namen der massive Typ.
            Wand(s).Bezeichner = "Wand Süd";
            Assert.NotEqual(TypaufbauSaattabelle.AW_HOLZLEICHTBAU, Modellvorschlagsregeln.Aufbau(s, Wand(s)).Typaufbau);
        }

        // ---------------------------------------------------------------- Stufe C mit Baualtersklasse

        [Fact]
        public void Stufe_C_bekommt_U_und_Typaufbau_der_Baualtersklasse_in_einem_Vorschlag()
        {
            Modellpruefstand s = Stand();
            s.Katalogaufbauten.Clear();
            s.Baualtersklasse = "E";
            Wand(s).U_Wert = null;
            double uKlasse = GebaeudeVorgaben.Wert('E', GebaeudeZielfelder.U_AUSSENWAND).Value;
            Pruefvorschlag v = Modellvorschlagsregeln.Aufbau(s, Wand(s));
            Assert.Equal(Vorschlagsherkunft.Typaufbau, v.Herkunft);
            Assert.Equal(uKlasse, v.UBauteil);
            Assert.Equal(uKlasse, v.U);
            Assert.Contains("Baualtersklasse E", v.Text);
            // Gegenprobe: ohne Klasse kein Typaufbau (ohne U kein Abgleich) — der heutige Weg.
            s.Baualtersklasse = null;
            Assert.Null(Modellvorschlagsregeln.Aufbau(s, Wand(s)));
        }

        [Fact]
        public void Stufe_C_mit_Klasse_nimmt_den_Katalogaufbau_im_Band_des_Klassen_U()
        {
            Modellpruefstand s = Stand();
            s.Baualtersklasse = "E";
            Wand(s).U_Wert = null;
            double uKlasse = GebaeudeVorgaben.Wert('E', GebaeudeZielfelder.U_AUSSENWAND).Value;
            s.Katalogaufbauten.Add(new Aufbaukennung(53, "AW Klasse", DbWerte.BAUTEILART_AUSSENWAND, false, uKlasse * 1.05, 250, false, 2, "AUSSENWAND"));
            Pruefvorschlag v = Modellvorschlagsregeln.Aufbau(s, Wand(s));
            Assert.Equal(Vorschlagsherkunft.Katalogaufbau, v.Herkunft);
            Assert.Equal(53, v.IdAufbau);
            Assert.Equal(uKlasse, v.UBauteil);
            // Gegenprobe: ohne Klasse der heutige Weg — Katalogfolge, kein U an das Bauteil.
            s.Baualtersklasse = null;
            Pruefvorschlag o = Modellvorschlagsregeln.Aufbau(s, Wand(s));
            Assert.Equal(51, o.IdAufbau);
            Assert.Null(o.UBauteil);
        }

        [Fact]
        public void Klassenzielfeld_nach_Bauteil()
        {
            Assert.Equal(GebaeudeZielfelder.U_AUSSENWAND, Modellvorschlagsregeln.KlassenZielfeld(new BauteilModel
                { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Neigung = 90, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT }));
            Assert.Equal(GebaeudeZielfelder.U_DACH, Modellvorschlagsregeln.KlassenZielfeld(new BauteilModel
                { Bauteilart = DbWerte.BAUTEILART_DACH, Neigung = 30, Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT }));
            Assert.Equal(GebaeudeZielfelder.U_GRUND, Modellvorschlagsregeln.KlassenZielfeld(new BauteilModel
                { Bauteilart = DbWerte.BAUTEILART_BODENPLATTE, Neigung = 180, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH }));
            Assert.Null(Modellvorschlagsregeln.KlassenZielfeld(new BauteilModel
                { Bauteilart = DbWerte.BAUTEILART_INNENWAND, Neigung = 90, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE }));
        }
    }

    /// <summary>
    /// Die Stufe C mit Baualtersklasse gegen die Arbeitskopie der Testdatenbank: eine Übernahme schreibt U und Aufbau, eine
    /// Rücknahme stellt beide zurück (MA-4).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ModellvorschlagBaualtersklasseDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        [Fact]
        public void Stufe_C_Uebernahme_schreibt_U_und_Aufbau_in_einem_Schritt_und_Ruecknahme_stellt_beide_zurueck()
        {
            if (!_db.Vorhanden) return;
            int g = Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT g.\"ID\" FROM \"Tab_Gebaeude\" g INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE z.\"ID_Projekt\" = ? ORDER BY g.\"ID\"", new DbParam("@p", 1052)).Rows[0][0], CultureInfo.InvariantCulture);
            int zone = Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"Rang\", \"ID\"", new DbParam("@g", g)).Rows[0][0], CultureInfo.InvariantCulture);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                // Kein Projektaufbau für Außenwände an Außenluft — die Stufe 1 greift nicht.
                v.Ausfuehren("UPDATE \"Tab_Bauteil\" SET \"ID_Aufbau\" = NULL WHERE \"Bauteilart\" = 'AUSSENWAND' AND \"ID_Zone\" IN " +
                             "(SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?)", new DbParam("@g", g));
                v.Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"Baualtersklasse\" = 'E' WHERE \"ID\" = ?", new DbParam("@g", g));
                v.Ausfuehren("INSERT INTO \"Tab_Bauteil\" (\"ID_Zone\", \"Rang\", \"Bezeichner\", \"Bauteilart\", \"Flaeche\", \"Neigung\", \"Azimut\", \"Randbedingung\") " +
                             "VALUES (?, 99, 'Mauerwerk Stufe C', 'AUSSENWAND', 12.5, 90, 180, 'AUSSENLUFT')", new DbParam("@z", zone));
                v.Commit();
            }
            int idb = Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT \"ID\" FROM \"Tab_Bauteil\" WHERE \"Bezeichner\" = 'Mauerwerk Stufe C'").Rows[0][0], CultureInfo.InvariantCulture);
            var ctrl = new ModellpruefungCtrl(g);
            Modellbefund b = Assert.Single(ctrl.Pruefen().Befunde, x => x.Code == Modellpruefung.OHNE_U_OHNE_AUFBAU && x.IdBauteil == idb);
            Assert.NotNull(b.Vorschlag);
            Assert.NotEqual(Vorschlagsherkunft.Projektaufbau, b.Vorschlag.Herkunft);
            double uKlasse = GebaeudeVorgaben.Wert('E', GebaeudeZielfelder.U_AUSSENWAND).Value;
            Assert.Equal(uKlasse, b.Vorschlag.UBauteil);

            Uebernahmeergebnis u = ctrl.Uebernehmen(Pruefgruppe.AufbautenStoffwerte);
            Assert.True(u.Ok, u.Meldung);
            DataRow r = DataRepository.GetDataTable("SELECT \"U_Wert\", \"ID_Aufbau\" FROM \"Tab_Bauteil\" WHERE \"ID\" = ?", new DbParam("@b", idb)).Rows[0];
            Assert.Equal(uKlasse, Convert.ToDouble(r["U_Wert"], CultureInfo.InvariantCulture), 9);
            Assert.False(r["ID_Aufbau"] is DBNull);
            Assert.DoesNotContain(u.Neu.Befunde, x => x.IdBauteil == idb && x.Code == Modellpruefung.OHNE_U_OHNE_AUFBAU);

            Uebernahmeergebnis z = ctrl.Zuruecknehmen(Pruefgruppe.AufbautenStoffwerte);
            Assert.True(z.Ok, z.Meldung);
            r = DataRepository.GetDataTable("SELECT \"U_Wert\", \"ID_Aufbau\" FROM \"Tab_Bauteil\" WHERE \"ID\" = ?", new DbParam("@b", idb)).Rows[0];
            Assert.True(r["U_Wert"] is DBNull);
            Assert.True(r["ID_Aufbau"] is DBNull);
        }
    }
}
