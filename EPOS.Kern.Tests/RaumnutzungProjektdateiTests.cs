using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Profile aus einer Projektdatei in einen eigenen Katalog</b> (Konzept Nutzungsprofile Q46, Stufe NP4b): Abbildung je
    /// benutzter DIN-Nummer, Einheiten nach 4.1, Absenkung wie <c>Heiz_Absenkung_K</c>, Meldungen; die Übernahme in die
    /// Kategorie „Projektdatei &lt;Datei&gt;" mit Ergänzen und Ersetzen, ohne Doppel und ohne die Zuordnung umzustellen.
    ///
    /// <para><b>Keine Zahl aus einer Datei in einer Erwartung:</b> Die Werte der Proben werden zur Laufzeit aus dem gelesenen
    /// Abbild genommen; erwartet werden nur Anzahl, Nummern, Einheiten und der Rundlauf. Die Proben selbst tragen runde
    /// Phantasiewerte (<see cref="SqprojProbenErzeuger"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungProjektdateiTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly List<string> _pfade = new List<string>();

        public void Dispose()
        {
            foreach (string p in _pfade)
                try { if (File.Exists(p)) File.Delete(p); } catch (IOException) { }
            _db.Dispose();
        }

        private string Probe(SqprojProbenErzeuger e)
        {
            string p = SqprojProbenErzeuger.TempPfad("np4b");
            _pfade.Add(p);
            e.Schreiben(p);
            return p;
        }

        private (SqprojAbbild Abbild, Projektdateiprofile Satz) Standard(string name = "Probehaus.sqproj")
        {
            SqprojAbbild a = SqprojLeser.Lesen(Probe(SqprojProbenErzeuger.Standard()));
            Assert.False(a.Abgelehnt);
            return (a, SqprojRaumnutzung.Bilden(a, name));
        }

        private static SqprojNutzungsprofil Quelle(SqprojAbbild a, string nummer)
            => a.Zonen.First(z => z.Nutzungsprofil?.Profilnummer?.ToString(CultureInfo.InvariantCulture) == nummer).Nutzungsprofil;

        // =================================================================
        //  Abbildung (rein)
        // =================================================================

        [Fact]
        public void Je_benutzter_DIN_Nummer_ein_Profil_in_der_Kategorie_der_Datei()
        {
            (SqprojAbbild a, Projektdateiprofile s) = Standard();
            Assert.False(s.Abgelehnt);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.RNP_PD_KATEGORIE, "Probehaus.sqproj"), s.Kategorie);
            Assert.Equal(new[] { "1", "71" }, s.Profile.Select(p => p.Profil.Nummer));
            Assert.All(s.Profile, p => Assert.True(p.Zonen >= 1));
            Assert.Equal(a.Zonen.Count(z => z.Nutzungsprofil?.Profilnummer != null), s.Profile.Sum(p => p.Zonen));
            Assert.Equal(s.Profile.Count, s.Profile.Select(p => p.Profil.Bezeichner).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.NotNull(s.Quellenhinweis);
        }

        [Fact]
        public void Werte_und_Einheiten_folgen_dem_Spaltensatz()
        {
            (SqprojAbbild a, Projektdateiprofile s) = Standard();
            SqprojNutzungsprofil q = Quelle(a, "1");
            Raumnutzungsprofil p = s.Profile.Single(x => x.Profil.Nummer == "1").Profil;

            Assert.Equal(q.Name, p.Bezeichner);
            Assert.Equal(q.BetriebVon, p.Nutzung_Von);
            Assert.Equal(q.BetriebBis, p.Nutzung_Bis);
            Assert.Equal(q.Raumtemperatur, p.Heiz_Soll);
            Assert.Equal(Math.Round(q.Raumtemperatur.Value - q.Absenkung.Value, 2), p.Heiz_Soll_Ausserhalb);
            // Außenluft: ohne Luftwechsel die flächenbezogene in m³/(h·m²).
            Assert.Null(q.Zuluftwechsel);
            Assert.Equal(q.AussenluftJeFlaeche, p.Aussenluft);
            Assert.Equal(RaumnutzungSchema.EINHEIT_JE_FLAECHE, p.Aussenluft_Einheit);
            // Personen: derselbe Nennwert je Fläche über m² je Person bei der Wärmeabgabe des Hauses.
            Assert.Equal(Matrixeingang.PERSON_W, p.Personen_Waerme);
            Assert.Equal(q.PersonenWm2.Value, p.Personen_Waerme.Value / p.Personen_Flaeche.Value, 2);
            Assert.Equal(Math.Round(q.VollnutzungPersonenH.Value / q.Betriebsstunden.Value, 4), p.Personen_Anteil);
            Assert.Equal(q.GeraeteWm2, p.Geraete_Leistung);
            Assert.Equal(Math.Round(q.VollnutzungGeraeteH.Value / q.Betriebsstunden.Value, 4), p.Geraete_Anteil);
            Assert.Null(p.Beleuchtung_Leistung);
            Assert.Null(p.Kuehl_Soll);
            Assert.Null(RaumnutzungCtrl.Profilpruefung(p.Kopie()));
            Assert.Contains(s.Meldungen, m => m.StartsWith(Text(R.RNP_PD_MSG_PERSONEN, "1", "\u0001", "", "").Split('\u0001')[0],
                                                             StringComparison.Ordinal));
        }

        [Fact]
        public void Ein_Profil_nur_mit_Sollwert_traegt_nur_ihn()
        {
            (SqprojAbbild a, Projektdateiprofile s) = Standard();
            Raumnutzungsprofil p = s.Profile.Single(x => x.Profil.Nummer == "71").Profil;
            Assert.Equal(Quelle(a, "71").Raumtemperatur, p.Heiz_Soll);
            Assert.Equal(1, p.Kennwerte().Count(w => w != null));
            Assert.Null(p.Nutzung_Von);
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData(7, null, 7, 24)]
        [InlineData(null, 18, 0, 18)]
        [InlineData(6, 6, null, null)]
        [InlineData(20, 6, 20, 6)]
        public void Eine_fehlende_Zeitgrenze_ist_Mitternacht(int? von, int? bis, int? erwVon, int? erwBis)
            => Assert.Equal((erwVon, erwBis), SqprojRaumnutzung.Fenster(von, bis));

        [Fact]
        public void Nicht_Abbildbares_wird_benannt_und_bleibt_leer()
        {
            // Runde Phantasiewerte: Raumtemperatur außerhalb der Grenzen, Absenkung, Außenluft je Person, Personenzahl, Beleuchtung.
            var m = new List<string>();
            Raumnutzungsprofil p = SqprojRaumnutzung.Abbilden(new SqprojNutzungsprofil
            {
                Name = "", Profilnummer = 90, Raumtemperatur = 100.0, Absenkung = 10.0, AussenluftJePerson = 10.0, Personenzahl = 10.0,
                Beleuchtungsstaerke = 100.0,
            }, "Probe.sqproj", m);
            Assert.Null(p.Heiz_Soll);
            Assert.Null(p.Heiz_Soll_Ausserhalb);
            Assert.Null(p.Aussenluft);
            Assert.Null(p.Personen_Flaeche);
            Assert.Null(p.Beleuchtung_Leistung);
            Assert.Equal(Text(R.RNP_PD_NAME_OHNE, "90"), p.Bezeichner);
            Assert.Contains(Text(R.RNP_PD_BESCHREIBUNG_LUX, 100.0.ToString("0.###", CultureInfo.CurrentCulture)), p.Beschreibung);
            Assert.Contains(Text(R.RNP_PD_MSG_BEGRENZT, "90", "NominalRoomTemperature"), m);
            Assert.Contains(Text(R.RNP_PD_MSG_LUFT_PERSON, "90"), m);
            Assert.Contains(Text(R.RNP_PD_MSG_PERSONENZAHL, "90"), m);
            Assert.Contains(m, x => x.StartsWith(Text(R.RNP_PD_MSG_BELEUCHTUNG, "90", "\u0001").Split('\u0001')[0], StringComparison.Ordinal));

            var m2 = new List<string>();
            SqprojRaumnutzung.Abbilden(new SqprojNutzungsprofil { Profilnummer = 91, Absenkung = 10.0 }, "Probe.sqproj", m2);
            Assert.Contains(Text(R.RNP_PD_MSG_ABSENKUNG, "91"), m2);
        }

        [Fact]
        public void Gleiche_Nummer_mit_verschiedenen_Werten_nimmt_das_erste_und_benennt_es()
        {
            var a = new SqprojAbbild();
            a.Zonen.Add(new SqprojZone { Uuid = "Z1", Name = "Eins" });
            a.Zonen.Add(new SqprojZone { Uuid = "Z2", Name = "Zwei" });
            a.Zonen.Add(new SqprojZone { Uuid = "Z3", Name = "Drei" });
            a.Zonen[0].Nutzungsprofil = new SqprojNutzungsprofil { Uuid = "U1", Name = "Probe", Profilnummer = 5, Raumtemperatur = 20.0 };
            a.Zonen[1].Nutzungsprofil = new SqprojNutzungsprofil { Uuid = "U2", Name = "Probe", Profilnummer = 5, Raumtemperatur = 18.0 };
            a.Zonen[2].Nutzungsprofil = new SqprojNutzungsprofil { Uuid = "U3", Name = "Ohne", Raumtemperatur = 20.0 };
            Projektdateiprofile s = SqprojRaumnutzung.Bilden(a, "Probe.sqproj");
            Projektdateiprofil p = Assert.Single(s.Profile);
            Assert.Equal("5", p.Profil.Nummer);
            Assert.Equal(2, p.Zonen);
            Assert.Equal(a.Zonen[0].Nutzungsprofil.Raumtemperatur, p.Profil.Heiz_Soll);
            Assert.Contains(Text(R.RNP_PD_MSG_ABWEICHEND, "5", 2.ToString(CultureInfo.CurrentCulture), "Probe"), s.Meldungen);
            Assert.Contains(Text(R.RNP_PD_MSG_OHNE_NUMMER, 1.ToString(CultureInfo.CurrentCulture)), s.Meldungen);
        }

        [Fact]
        public void Aus_dem_Strom_gelesen_wie_aus_dem_Abbild_und_eine_fremde_Datei_benannt_abgelehnt()
        {
            string pfad = Probe(SqprojProbenErzeuger.Standard());
            Projektdateiprofile direkt = SqprojRaumnutzung.Bilden(SqprojLeser.Lesen(pfad), "Probe.sqproj");
            Projektdateiprofile s;
            using (FileStream f = File.OpenRead(pfad)) s = SqprojRaumnutzung.Lesen(f, Path.Combine("ordner", "Probe.sqproj"), 0);
            Assert.False(s.Abgelehnt, s.Ablehnung);
            Assert.Equal(direkt.Kategorie, s.Kategorie);
            Assert.Equal(direkt.Profile.Select(p => p.Profil.Kennwerte()), s.Profile.Select(p => p.Profil.Kennwerte()));

            string text = SqprojProbenErzeuger.TempPfad("text");
            _pfade.Add(text);
            File.WriteAllText(text, "keine Projektdatei");
            using (FileStream f = File.OpenRead(text)) s = SqprojRaumnutzung.Lesen(f, "text.sqproj", 0);
            Assert.True(s.Abgelehnt);
            Assert.Empty(s.Profile);

            using (FileStream f = File.OpenRead(pfad)) s = SqprojRaumnutzung.Lesen(f, "Probe.sqproj", 16);
            Assert.True(s.Abgelehnt);
        }

        [Fact]
        public void Ein_langer_Dateiname_wird_gekuerzt()
        {
            string k = SqprojRaumnutzung.Kategoriename(new string('x', 200) + ".sqproj");
            Assert.True(k.Length <= RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN);
        }

        // =================================================================
        //  Übernahme (Datenbank)
        // =================================================================

        [Fact]
        public void Uebernahme_legt_die_Kategorie_an_und_haelt_den_Rundlauf()
        {
            if (!_db.Vorhanden) return;
            (_, Projektdateiprofile s) = Standard();
            var c = new RaumnutzungCtrl();
            var zuordnungVorher = c.Zuordnungen(RaumnutzungSchema.ZUORDNUNG_DIN);
            Assert.Null(c.KategorieMitNamen(s.Kategorie));

            RaumnutzungCtrl.Projektdateiuebernahme e = c.ProjektdateiUebernehmen(s, ersetzen: false);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal((2, 0, 0, 0), (e.Neu, e.Ersetzt, e.Unveraendert, e.Entfernt));
            RaumnutzungCtrl.Kategorie k = c.KategorieMitNamen(s.Kategorie);
            Assert.NotNull(k);
            Assert.Equal(e.IdKategorie, k.Id);
            Assert.Equal(RaumnutzungSchema.ART_EIGEN, k.Art);
            Assert.False(k.Ausgeliefert);
            List<Raumnutzungsprofil> gelesen = c.Profile(k.Id);
            Assert.Equal(s.Profile.Select(p => p.Profil.Nummer), gelesen.Select(p => p.Nummer));
            foreach (Projektdateiprofil q in s.Profile)
            {
                Raumnutzungsprofil g = gelesen.Single(p => p.Nummer == q.Profil.Nummer);
                Assert.Equal(q.Profil.Bezeichner, g.Bezeichner);
                Assert.Equal(q.Profil.Kennwerte(), g.Kennwerte());
                Assert.False(g.Ausgeliefert);
            }
            // Die Zuordnung der DIN-Nummern bleibt, wie sie war.
            Assert.Equal(zuordnungVorher, c.Zuordnungen(RaumnutzungSchema.ZUORDNUNG_DIN));
        }

        [Fact]
        public void Wiederholte_Uebernahme_doppelt_nichts_Ergaenzen_und_Ersetzen()
        {
            if (!_db.Vorhanden) return;
            (_, Projektdateiprofile s) = Standard();
            var c = new RaumnutzungCtrl();
            Assert.True(c.ProjektdateiUebernehmen(s, false).Ok);
            long kat = c.KategorieMitNamen(s.Kategorie).Id;
            List<long> ids = c.Profile(kat).Select(p => p.Id).ToList();
            int kategorien = c.Kategorien().Count;

            RaumnutzungCtrl.Projektdateiuebernahme e = c.ProjektdateiUebernehmen(s, ersetzen: false);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal((0, 0, 2, 0), (e.Neu, e.Ersetzt, e.Unveraendert, e.Entfernt));
            Assert.Equal(kategorien, c.Kategorien().Count);
            Assert.Equal(ids, c.Profile(kat).Select(p => p.Id));

            e = c.ProjektdateiUebernehmen(s, ersetzen: true);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal((0, 2, 0, 0), (e.Neu, e.Ersetzt, e.Unveraendert, e.Entfernt));
            Assert.Equal(ids, c.Profile(kat).Select(p => p.Id));
            Assert.Equal(kategorien, c.Kategorien().Count);
        }

        [Fact]
        public void Ersetzen_entfernt_was_die_Datei_nicht_mehr_fuehrt_und_loest_die_Zuordnung()
        {
            if (!_db.Vorhanden) return;
            (_, Projektdateiprofile s) = Standard();
            var c = new RaumnutzungCtrl();
            Assert.True(c.ProjektdateiUebernehmen(s, false).Ok);
            long kat = c.KategorieMitNamen(s.Kategorie).Id;
            Raumnutzungsprofil weg = c.Profile(kat).Single(p => p.Nummer == "71");
            Assert.True(c.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, "mrtNp4bProbe", weg.Id).Ok);

            var kuerzer = new Projektdateiprofile
            {
                Dateiname = s.Dateiname, Kategorie = s.Kategorie, Quellenhinweis = s.Quellenhinweis,
                Profile = s.Profile.Where(p => p.Profil.Nummer != "71").ToList(),
            };
            RaumnutzungCtrl.Projektdateiuebernahme e = c.ProjektdateiUebernehmen(kuerzer, ersetzen: true);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal((0, 1, 0, 1, 1), (e.Neu, e.Ersetzt, e.Unveraendert, e.Entfernt, e.ZuordnungenGeloest));
            Assert.Equal(new[] { "1" }, c.Profile(kat).Select(p => p.Nummer));
            Assert.Null(c.Zuordnungen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD).Single(z => z.Schluessel == "mrtNp4bProbe").IdProfil);
            Assert.Contains(Text(R.RNP_PD_MSG_ZUORDNUNG_GELOEST, 1.ToString(CultureInfo.CurrentCulture)), e.Meldungen);
        }

        [Fact]
        public void Ohne_Profile_und_abgelehnt_wird_nichts_geschrieben()
        {
            if (!_db.Vorhanden) return;
            var c = new RaumnutzungCtrl();
            int vorher = c.Kategorien().Count;
            Assert.False(c.ProjektdateiUebernehmen(new Projektdateiprofile { Kategorie = "Projektdatei leer.sqproj" }, false).Ok);
            Assert.False(c.ProjektdateiUebernehmen(new Projektdateiprofile { Kategorie = "x", Ablehnung = "nein" }, false).Ok);
            Assert.Equal(vorher, c.Kategorien().Count);
        }

        private static string Text(string muster, params string[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);
    }
}
