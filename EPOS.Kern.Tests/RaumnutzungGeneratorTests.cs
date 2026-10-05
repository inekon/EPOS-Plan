using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Generator und Controller der Nutzungsprofile</b> (Konzept Nutzungsprofile 4.3, 4.4, 8; Stufe NP1b):
    /// <list type="bullet">
    /// <item><b>Tabelle 4.3 ohne Datenbank</b> — Kennwerte je Größe, Fenster über Mitternacht, freier Einzeltag
    /// (Standardwoche), Feiertage, Einheiten (NP-F10), Nennwerte (Q38–Q40), Stundenprofil, leeres Profil (NP-F6, NP-F13).</item>
    /// <item><b>Bitgleichheit</b> — Wohnen, Büro und Schule: „Profil übernehmen" schreibt an Zone und Gebäude dieselben
    /// Zeilen in <c>Tab_Konditionierungskalender</c>, <c>Tab_Konditionierungsvorgabe</c> und
    /// <c>Tab_Konditionierungsperiode</c> (ohne Schlüssel) und denselben Zonenbestand wie „Vorlage übernehmen" mit den
    /// ausgelieferten Vorlagen gleicher Nutzung; nur <c>Nutzung</c> trägt den Profilnamen statt der Kennung (NP-F15).</item>
    /// <item><b>Die Zonenschleife</b> rechnet mit Profil Büro dieselben Reihen wie mit den Vorlagen Büro.</item>
    /// <item><b>Die fünf neuen Muster</b> erzeugen die Kalender nach Konzept 5.1; „Sonstige" erzeugt keinen.</item>
    /// <item><b>Zuordnung</b> — DIN 1 → Büro, 8 → Schule, 70 → Wohnen, IFC Buero → Büro, unbekannt → <c>null</c>;
    /// Pflege eigener Kategorien, Profile und Zuordnungen samt Schloss der ausgelieferten.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungGeneratorTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private const string ZONE = "Gastronomie und Verwaltung";
        private readonly TestDatenbank _db;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public RaumnutzungGeneratorTests(TestDatenbank db) => _db = db;

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        //  Ohne Datenbank: Tabelle 4.3
        // =================================================================

        private static Raumnutzungsgroesse G(Raumnutzungsprofil p, Konditionierungsgroesse g, double? flaeche = null, double? hoehe = null)
            => Raumnutzungsgenerator.Erzeugen(p, g, flaeche, hoehe);

        private static string Zeilen(Raumnutzungsgroesse r)
            => r.Vorlage == null
                ? "—"
                : string.Join(" | ", r.Vorlage.Inhalt.Vorgabezeilen().Select(z => z.Zeile + "=" +
                    (z.Aus ? "aus" : z.Wert?.ToString("0.####", CultureInfo.InvariantCulture) ?? "") +
                    (z.Von.HasValue || z.Bis.HasValue ? "(" + z.Von + "-" + z.Bis + ")" : "")));

        private static Raumnutzungsprofil Buerokennwerte() => new Raumnutzungsprofil
        {
            Id = 7, Bezeichner = "Probe", Nutzung_Von = 8, Nutzung_Bis = 17, Betrieb_Von = 7, Betrieb_Bis = 18,
            Nutzungstage_Woche = "1111100", Feiertage_Wie_Sonntag = true,
            Heiz_Soll = 20.0, Heiz_Soll_Ausserhalb = 16.0, Kuehl_Soll = 26.0, Kuehl_Aus_Ausserhalb = true,
            Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE, Aussenluft_Ausserhalb = 0.1,
            Personen_Anteil = 1.0, Personen_Anteil_Ausserhalb = 0.0, Geraete_Anteil = 1.0, Geraete_Anteil_Ausserhalb = 0.1,
        };

        [Fact]
        public void Die_Kennwerte_ergeben_Tag_Nacht_Wochenende_Ferien_und_Feiertage_je_Groesse()
        {
            Raumnutzungsprofil p = Buerokennwerte();
            Assert.Equal("TAG=20 | NACHT=16(18-7) | WOCHENENDE=16 | FERIEN=16", Zeilen(G(p, Konditionierungsgroesse.Heizsoll)));
            Assert.Equal("TAG=26 | NACHT=aus(18-7) | WOCHENENDE=aus | FERIEN=aus", Zeilen(G(p, Konditionierungsgroesse.Kuehlsoll)));
            // Ohne Außenluft im Fenster bleibt der Tageswert des Ziels — keine Zeile Tag (wie die Vorlage Büro).
            Assert.Equal("NACHT=0.1(18-7) | WOCHENENDE=0.1 | FERIEN=0.1", Zeilen(G(p, Konditionierungsgroesse.Lueftung)));
            Assert.Equal("TAG=1 | NACHT=0.1(17-8) | WOCHENENDE=0.1 | FERIEN=0.1", Zeilen(G(p, Konditionierungsgroesse.Geraete)));
            Assert.Equal("TAG=1 | NACHT=0(17-8) | WOCHENENDE=0 | FERIEN=0", Zeilen(G(p, Konditionierungsgroesse.Personen)));

            Raumnutzungsgroesse h = G(p, Konditionierungsgroesse.Heizsoll);
            Assert.Equal(Raumnutzungsweg.Kennwerte, h.Weg);
            Assert.Equal("Probe", h.Vorlage.Name);
            Konditionierungskalender k = h.Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Heizsoll);
            Assert.NotNull(k);
            Assert.Equal(16.0, k.Grundangabe.Wert);
            Assert.Equal(9, k.Perioden.Count);
            Assert.All(k.Perioden, r => Assert.True(r.IstFeiertag));
            Assert.Equal(DbWerte.KOND_FEIERTAGE.OrderBy(x => x, StringComparer.Ordinal),
                         k.Perioden.Select(r => r.Feiertagsregel).OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(k.Perioden, r => Assert.Equal(7, r.Angabe.WieWochentag));
            Assert.Equal(Kalenderangabe.Abgeschaltet.Art,
                         G(p, Konditionierungsgroesse.Kuehlsoll).Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Kuehlsoll).Grundangabe.Art);

            // Ohne Feiertagsregel kein Kalenderkopf: die Matrix allein reicht.
            p.Feiertage_Wie_Sonntag = false;
            Assert.Null(G(p, Konditionierungsgroesse.Heizsoll).Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Heizsoll));
        }

        [Fact]
        public void Durchgehend_gleiche_Werte_und_ganztaegige_Fenster_ergeben_nur_die_Zeile_Tag()
        {
            var p = new Raumnutzungsprofil
            {
                Bezeichner = "Technik", Nutzung_Von = 0, Nutzung_Bis = 24, Nutzungstage_Woche = "1111111",
                Heiz_Soll = 15.0, Geraete_Anteil = 1.0, Personen_Anteil = 0.0, Feiertage_Wie_Sonntag = true,
            };
            Assert.Equal("TAG=15", Zeilen(G(p, Konditionierungsgroesse.Heizsoll)));
            Assert.Equal("TAG=1", Zeilen(G(p, Konditionierungsgroesse.Geraete)));
            Assert.Equal("TAG=0", Zeilen(G(p, Konditionierungsgroesse.Personen)));
            Assert.Null(G(p, Konditionierungsgroesse.Heizsoll).Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Heizsoll));
            Assert.Equal(Raumnutzungsweg.Keiner, G(p, Konditionierungsgroesse.Kuehlsoll).Weg);
            Assert.Equal(Raumnutzungsweg.Keiner, G(p, Konditionierungsgroesse.Lueftung).Weg);

            // Fenster 0–24, aber mit freiem Wochenende: kein Nachtfenster, die Zeile Wochenende.
            p.Nutzungstage_Woche = "1111100";
            p.Heiz_Soll_Ausserhalb = 12.0;
            Assert.Equal("TAG=15 | WOCHENENDE=12 | FERIEN=12", Zeilen(G(p, Konditionierungsgroesse.Heizsoll)));
        }

        [Fact]
        public void Ein_Fenster_ueber_Mitternacht_setzt_die_Nachtzeit_dazwischen()
        {
            var p = new Raumnutzungsprofil
            {
                Bezeichner = "Nachtbetrieb", Nutzung_Von = 22, Nutzung_Bis = 6, Nutzungstage_Woche = "1111111",
                Personen_Anteil = 1.0, Personen_Anteil_Ausserhalb = 0.2,
            };
            Assert.Equal("TAG=1 | NACHT=0.2(6-22) | FERIEN=0.2", Zeilen(G(p, Konditionierungsgroesse.Personen)));

            // Dasselbe Fenster mit freiem Montag: die Woche trägt 22 bis 6 Uhr den Tageswert.
            p.Nutzungstage_Woche = "0111111";
            Konditionierungskalender k = G(p, Konditionierungsgroesse.Personen).Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Personen);
            Assert.Equal(Angabeart.Woche, k.Grundangabe.Art);
            IReadOnlyList<double> w = k.Grundangabe.Woche;
            Assert.Equal(0.2, w[Kalenderwoche.Stelle(0, 23)]);                 // Montag frei
            Assert.Equal(1.0, w[Kalenderwoche.Stelle(1, 23)]);                 // Dienstag 23 Uhr im Fenster
            Assert.Equal(1.0, w[Kalenderwoche.Stelle(1, 5)]);
            Assert.Equal(0.2, w[Kalenderwoche.Stelle(1, 6)]);
            Assert.Equal(0.2, w[Kalenderwoche.Stelle(1, 21)]);
        }

        [Fact]
        public void Ein_freier_Einzeltag_erzwingt_eine_Standardwoche()
        {
            var p = new Raumnutzungsprofil
            {
                Bezeichner = "Gastronomie", Nutzung_Von = 10, Nutzung_Bis = 23, Nutzungstage_Woche = "0111111",
                Heiz_Soll = 20.0, Heiz_Soll_Ausserhalb = 16.0, Kuehl_Soll = 26.0, Kuehl_Aus_Ausserhalb = true,
                Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE, Aussenluft_Ausserhalb = 0.1,
            };
            Raumnutzungsgroesse h = G(p, Konditionierungsgroesse.Heizsoll);
            Assert.Equal("TAG=20 | NACHT=16(23-10) | FERIEN=16", Zeilen(h));
            IReadOnlyList<double> w = h.Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Heizsoll).Grundangabe.Woche;
            Assert.Equal(16.0, w[Kalenderwoche.Stelle(0, 12)]);
            Assert.Equal(20.0, w[Kalenderwoche.Stelle(6, 12)]);
            Assert.Equal(16.0, w[Kalenderwoche.Stelle(6, 23)]);
            Assert.Equal(7 * 24 - 6 * 13, w.Count(x => x == 16.0));

            IReadOnlyList<double> k = G(p, Konditionierungsgroesse.Kuehlsoll).Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Kuehlsoll).Grundangabe.Woche;
            Assert.True(double.IsNaN(k[Kalenderwoche.Stelle(0, 12)]));
            Assert.Equal(26.0, k[Kalenderwoche.Stelle(2, 12)]);

            // Ohne Tageswert lässt sich der freie Tag nicht abbilden — benannt, die Matrixzeilen bleiben.
            Raumnutzungsgroesse l = G(p, Konditionierungsgroesse.Lueftung);
            Assert.Equal(Raumnutzungshinweis.FreierTagOhneTageswert, l.Hinweis);
            Assert.Null(l.Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Lueftung));
            Assert.Equal("NACHT=0.1(23-10) | FERIEN=0.1", Zeilen(l));

            // Ein Samstag allein frei ist ebenfalls ein Einzeltag.
            p.Nutzungstage_Woche = "1111101";
            Assert.Equal(Angabeart.Woche, G(p, Konditionierungsgroesse.Heizsoll).Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Heizsoll).Grundangabe.Art);
        }

        [Fact]
        public void Aussenluft_je_Flaeche_wird_mit_der_lichten_Hoehe_umgerechnet_ohne_Hoehe_benannt_nicht_gesetzt()
        {
            var p = new Raumnutzungsprofil
            {
                Bezeichner = "Seminar", Nutzung_Von = 8, Nutzung_Bis = 18, Nutzungstage_Woche = "1111100",
                Aussenluft = 5.0, Aussenluft_Ausserhalb = 0.5, Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_FLAECHE,
            };
            Assert.Equal("TAG=2 | NACHT=0.2(18-8) | WOCHENENDE=0.2 | FERIEN=0.2", Zeilen(G(p, Konditionierungsgroesse.Lueftung, hoehe: 2.5)));
            Raumnutzungsgroesse ohne = G(p, Konditionierungsgroesse.Lueftung);
            Assert.Equal(Raumnutzungsweg.Keiner, ohne.Weg);
            Assert.Null(ohne.Vorlage);
            Assert.Equal(Raumnutzungshinweis.LueftungOhneHoehe, ohne.Hinweis);

            p.Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE;
            Assert.Equal("TAG=5 | NACHT=0.5(18-8) | WOCHENENDE=0.5 | FERIEN=0.5", Zeilen(G(p, Konditionierungsgroesse.Lueftung)));
        }

        [Fact]
        public void Nennwerte_folgen_Q38_bis_Q40_und_nur_mit_Flaeche()
        {
            var p = new Raumnutzungsprofil
            {
                Bezeichner = "Büro mit Lasten", Nutzung_Von = 8, Nutzung_Bis = 17, Nutzungstage_Woche = "1111100",
                Geraete_Leistung = 8.0, Personen_Flaeche = 10.0, Personen_Anteil = 1.0,
            };
            Raumnutzungsgroesse g = G(p, Konditionierungsgroesse.Geraete, flaeche: 120.0);
            Assert.Equal(960.0, g.Nennwert);
            Assert.Equal("8 W/m² × 120 m² = 960 W", g.Nennwertherleitung);
            Assert.Equal(960.0, g.Vorlage.Inhalt.Vorgabe(Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_NENNWERT).Wert);

            p.Beleuchtung_Leistung = 10.0;
            p.Beleuchtung_Anteil = 0.5;
            g = G(p, Konditionierungsgroesse.Geraete, flaeche: 120.0);
            Assert.Equal(1560.0, g.Nennwert);
            Assert.Equal("(8 W/m² + 10 W/m² × 0.5) × 120 m² = 1560 W", g.Nennwertherleitung);

            Raumnutzungsgroesse pers = G(p, Konditionierungsgroesse.Personen, flaeche: 120.0);
            Assert.Equal(12 * Matrixeingang.PERSON_W, pers.Nennwert);
            p.Personen_Waerme = 100.0;
            Assert.Equal(1200.0, G(p, Konditionierungsgroesse.Personen, flaeche: 120.0).Nennwert);

            // Ohne Fläche bleibt der Nennwert des Ziels: keine Zeile NENNWERT.
            Raumnutzungsgroesse ohne = G(p, Konditionierungsgroesse.Geraete);
            Assert.Null(ohne.Nennwert);
            Assert.False(ohne.Vorlage.Inhalt.Vorgabe(Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_NENNWERT).Belegt);
        }

        [Fact]
        public void Ein_Stundenprofil_ergibt_eine_Standardwoche_und_geht_den_Kennwerten_vor()
        {
            string werktag = string.Join(";", Enumerable.Range(0, 24).Select(h => h >= 8 && h < 18 ? "1" : "0.1"));
            string frei = string.Join(";", Enumerable.Repeat("0.05", 24));
            var p = new Raumnutzungsprofil
            {
                Bezeichner = "SIA-Probe", Nutzungstage_Woche = "1111100", Geraete_Anteil = 0.5,
                Stunden = new List<Raumnutzungsstunden>
                {
                    new Raumnutzungsstunden("GERAETE", RaumnutzungSchema.TAGESART_WERKTAG, werktag),
                    new Raumnutzungsstunden("GERAETE", RaumnutzungSchema.TAGESART_FREI, frei),
                },
            };
            Raumnutzungsgroesse g = G(p, Konditionierungsgroesse.Geraete);
            Assert.Equal(Raumnutzungsweg.Stundenprofil, g.Weg);
            IReadOnlyList<double> w = g.Vorlage.Inhalt.Kalender(Konditionierungsgroesse.Geraete).Grundangabe.Woche;
            Assert.Equal(1.0, w[Kalenderwoche.Stelle(0, 9)]);
            Assert.Equal(0.1, w[Kalenderwoche.Stelle(0, 20)]);
            Assert.Equal(0.05, w[Kalenderwoche.Stelle(5, 9)]);

            // Ein unlesbares Stundenprofil fällt auf die Kennwerte zurück.
            p.Stunden[0] = new Raumnutzungsstunden("GERAETE", RaumnutzungSchema.TAGESART_WERKTAG, "1;2;3");
            Raumnutzungsgroesse r = G(p, Konditionierungsgroesse.Geraete);
            Assert.Equal(Raumnutzungsweg.Kennwerte, r.Weg);
            Assert.Equal(Raumnutzungshinweis.StundenprofilUnlesbar, r.Hinweis);
        }

        [Fact]
        public void Ein_leeres_Profil_erzeugt_keinen_Kalender()
        {
            var p = new Raumnutzungsprofil { Bezeichner = "Sonstige" };
            Assert.True(p.IstLeer);
            Assert.All(Raumnutzungsgenerator.Erzeugen(p, 100.0), r =>
            {
                Assert.Equal(Raumnutzungsweg.Keiner, r.Weg);
                Assert.Null(r.Vorlage);
                Assert.Equal(Raumnutzungshinweis.ProfilOhneWerte, r.Hinweis);
            });
        }

        [Fact]
        public void Die_Profilpruefung_lehnt_Werte_ausserhalb_der_Grenzen_benannt_ab()
        {
            Assert.Null(RaumnutzungCtrl.Profilpruefung(Buerokennwerte()));
            Raumnutzungsprofil p = Buerokennwerte();
            p.Heiz_Soll = 45.0;
            Assert.Contains("Heiz_Soll", RaumnutzungCtrl.Profilpruefung(p));
            p = Buerokennwerte();
            p.Nutzungstage_Woche = "11111";
            Assert.Contains("Nutzungstage_Woche", RaumnutzungCtrl.Profilpruefung(p));
            p = Buerokennwerte();
            p.Zeilen.Add(new Vorgabezeile { Groesse = "HEIZSOLL", Zeile = DbWerte.KOND_ZEILE_SAISON, Von = 1, Bis = 100 });
            Assert.NotNull(RaumnutzungCtrl.Profilpruefung(p));
            p = Buerokennwerte();
            p.Bezeichner = "  ";
            Assert.NotNull(RaumnutzungCtrl.Profilpruefung(p));
        }

        // =================================================================
        //  Mit Datenbank: Katalog, Zuordnung, Pflege
        // =================================================================

        [Fact]
        public void Die_Zuordnung_loest_DIN_Nummer_und_IFC_Klasse_auf()
        {
            if (!_db.Vorhanden) return;
            var c = new RaumnutzungCtrl();
            Assert.Equal(RaumnutzungSaat.BUERO, c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "1")?.Bezeichner);
            Assert.Equal(RaumnutzungSaat.SCHULE, c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "8")?.Bezeichner);
            Assert.Equal(RaumnutzungSaat.WOHNEN, c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "70")?.Bezeichner);
            Assert.Equal(RaumnutzungSaat.BUERO, c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_IFC, "Buero")?.Bezeichner);
            Assert.Equal(RaumnutzungSaat.BUERO, c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_IFC, "  buero ")?.Bezeichner);
            Assert.Null(c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "999"));
            Assert.Null(c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_IFC, "Unbekannt"));
            Assert.Null(c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, "mrt1"));

            Raumnutzungsprofil buero = c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "1");
            Assert.True(buero.Ausgeliefert);
            Assert.Equal(19, buero.Zeilen.Count);
            Assert.Equal(4, c.Kategorien().Count);
            Assert.Equal(9, c.Profile(c.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER).Id).Count);
        }

        [Fact]
        public void Eigene_Kategorien_Profile_und_Zuordnungen_lassen_sich_pflegen_ausgelieferte_nur_duplizieren()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var c = new RaumnutzungCtrl();
            RaumnutzungCtrl.Kategorie din = c.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_DIN_V_18599_10);
            Raumnutzungsprofil einzelbuero = c.Profile(din.Id).Single(p => p.Nummer == "1");
            Assert.True(einzelbuero.IstLeer);

            // Ausgeliefert: weder ändern noch löschen, nicht in eine ausgelieferte Kategorie duplizieren.
            Assert.False(c.ProfilLoeschen(einzelbuero.Id).Ok);
            Assert.False(c.ProfilAendern(einzelbuero).Ok);
            Assert.False(c.KategorieLoeschen(din.Id).Ok);
            Assert.False(c.ProfilDuplizieren(einzelbuero.Id, null, null).Ok);

            RaumnutzungCtrl.Ergebnis k = c.KategorieAnlegen("Meine Normwerte", null, "eigene Ausgabe");
            Assert.True(k.Ok, k.Meldung);
            Assert.False(c.KategorieAnlegen("meine normwerte", null, null).Ok);
            RaumnutzungCtrl.Ergebnis d = c.ProfilDuplizieren(einzelbuero.Id, k.Id, null);
            Assert.True(d.Ok, d.Meldung);
            Raumnutzungsprofil eigen = c.ProfilLesen(d.Id);
            Assert.False(eigen.Ausgeliefert);
            Assert.Equal("1", eigen.Nummer);
            eigen.Heiz_Soll = 21.0;
            eigen.Heiz_Soll_Ausserhalb = 17.0;
            eigen.Nutzung_Von = 7;
            eigen.Nutzung_Bis = 18;
            eigen.Zeilen.Add(new Vorgabezeile { Groesse = "KUEHLSOLL", Zeile = DbWerte.KOND_ZEILE_TAG, Aus = true });
            Assert.True(c.ProfilAendern(eigen).Ok);
            Raumnutzungsprofil gelesen = c.ProfilLesen(eigen.Id);
            Assert.Equal(21.0, gelesen.Heiz_Soll);
            Assert.Single(gelesen.Zeilen);

            // Die ausgelieferte Zuordnung DIN 1 lässt sich auf das eigene Profil stellen; das Profil löschen setzt sie auf „keine".
            Assert.True(c.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_DIN, "1", eigen.Id).Ok);
            Assert.Equal(eigen.Id, c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "1")?.Id);
            Assert.Equal(1, c.ZuordnungenAuf(eigen.Id));
            Assert.True(c.ProfilLoeschen(eigen.Id).Ok);
            Assert.Null(c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "1"));
            RaumnutzungCtrl.Zuordnung z1 = c.Zuordnungen(RaumnutzungSchema.ZUORDNUNG_DIN).Single(z => z.Schluessel == "1");
            Assert.True(z1.Ausgeliefert);
            Assert.Null(z1.IdProfil);
            Assert.False(c.ZuordnungLoeschen(z1.Id).Ok);

            // Eine eigene Zuordnung auf ein ausgeliefertes Profil.
            long schule = c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, "8").Id;
            RaumnutzungCtrl.Ergebnis z = c.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, "mrt12", schule);
            Assert.True(z.Ok, z.Meldung);
            Assert.Equal(schule, c.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, "MRT12")?.Id);
            Assert.True(c.ZuordnungLoeschen(z.Id).Ok);
            Assert.False(c.ZuordnungSetzen("RAUM", "x", null).Ok);

            // Eine Kategorie duplizieren nimmt ihre Profile mit; löschen nimmt sie wieder.
            RaumnutzungCtrl.Kategorie muster = c.Kategorien().Single(x => x.Art == RaumnutzungSchema.ART_EPOS_MUSTER);
            RaumnutzungCtrl.Ergebnis kd = c.KategorieDuplizieren(muster.Id, null);
            Assert.True(kd.Ok, kd.Meldung);
            List<Raumnutzungsprofil> kopien = c.Profile(kd.Id);
            Assert.Equal(9, kopien.Count);
            Assert.Equal(19, kopien.Single(p => p.Bezeichner == RaumnutzungSaat.BUERO).Zeilen.Count);
            Assert.True(c.KategorieLoeschen(kd.Id).Ok);
            Assert.Empty(c.Profile(kd.Id));
        }

        // =================================================================
        //  Bitgleichheit Profil = Vorlage gleicher Nutzung
        // =================================================================

        private sealed record Ziel(int Projekt, long Gebaeude, long Zone);

        private static Ziel Kopie(string name)
        {
            int id = new ProjektDuplizierenCtrl().Duplizieren(Zonenprojekt1052.NAME, name);
            Assert.True(id > 0, "Die Kopie von 1052 fiel auf " + id);
            long gebaeude = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", id)),
                                            CultureInfo.InvariantCulture);
            long zone = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Zone WHERE ID_Gebaeude = ? AND Bezeichner = ?",
                                                                     new DbParam("@g", gebaeude), new DbParam("@b", ZONE)),
                                        CultureInfo.InvariantCulture);
            return new Ziel(id, gebaeude, zone);
        }

        /// <summary>
        /// „Vorlage übernehmen" mit den ausgelieferten Vorlagen einer Nutzung je Größe an Gebäude oder Zone — der Weg des
        /// Zonenplans (<c>ZonenplanCtrl.NutzungUebernehmen</c>), am Gebäude mit allen Zonen geschrieben.
        /// </summary>
        private static void Vorlagenweg(long gebaeude, long? zone, string nutzung)
        {
            var vorlagen = new KonditionierungsvorlageCtrl();
            var kond = new KonditionierungCtrl();
            Konditionierungsarbeitsstand stand = kond.ArbeitsstandLesen(gebaeude, null, out string m0);
            Assert.True(stand != null, m0);
            var uebernommen = new List<Konditionierungsgroesse>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                KonditionierungsvorlageCtrl.Vorlage kopf = vorlagen.Liste(g).FirstOrDefault(x => x.Ausgeliefert && x.Nutzung == nutzung);
                if (kopf == null) continue;
                Konditionierungsvorlage vorlage = vorlagen.Inhalt(kopf.Id, out string m1);
                Assert.True(vorlage != null, m1);
                var ort = new Konditionierungsort(g, zone);
                Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(stand, ort, vorlage);
                if (s.Rueckfrage) s = Konditionierungsarbeit.VorlageUebernehmen(Konditionierungsarbeit.LuftwechselAufteilen(stand).Stand, ort, vorlage);
                Assert.True(s.Ok && !s.Rueckfrage, s.Meldung);
                stand = s.Stand;
                uebernommen.Add(g);
            }
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                Assert.True(kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(gebaeude), stand.Gebaeude, true, out _).Ok);
                foreach (Konditionierungszone z in stand.Zonen)
                    Assert.True(kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Zone(gebaeude, z.Id), z.Stand, true, out _).Ok);
                KonditionierungCtrl.Eigner ziel = zone.HasValue ? KonditionierungCtrl.Eigner.Zone(gebaeude, zone.Value) : KonditionierungCtrl.Eigner.Gebaeude(gebaeude);
                foreach (Konditionierungsgroesse g in uebernommen) KonditionierungCtrl.NutzungSetzen(ziel, g, nutzung);
                v.Commit();
            }
        }

        private static List<string> Abdruck(string sql, params DbParam[] p)
        {
            DataTable t = DataRepository.GetDataTable(sql, p);
            return t.Rows.Cast<DataRow>()
                    .Select(r => string.Join("|", r.ItemArray.Select(x => x is DBNull ? "∅" : Convert.ToString(x, CultureInfo.InvariantCulture))))
                    .ToList();
        }

        /// <summary>Die Konditionierung eines Gebäudes samt Zonen ohne Schlüssel; die Zone über ihren Namen, die Nutzung getrennt.</summary>
        private static (List<string> Zeilen, List<string> Nutzung) Konditionierung(long gebaeude)
        {
            const string ZONE_NAME = "COALESCE((SELECT Bezeichner FROM Tab_Zone WHERE ID = k.ID_Zone), '—')";
            var zeilen = new List<string>();
            zeilen.AddRange(Abdruck("SELECT " + ZONE_NAME + ", k.Groesse, k.Wert, k.Aus, k.Woche, k.Nennwert, k.Bemerkung FROM Tab_Konditionierungskalender k " +
                                    "WHERE k.ID_Gebaeude = ? ORDER BY 1, k.Groesse", new DbParam("@g", gebaeude)).Select(x => "K " + x));
            zeilen.AddRange(Abdruck("SELECT " + ZONE_NAME + ", k.Groesse, p.Rang, p.Art, p.Bezeichner, p.Beginn, p.Ende, p.Feiertagsregel, p.Wert, " +
                                    "p.Aus, p.Woche, p.WieWochentag FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k " +
                                    "ON k.ID = p.ID_Kalender WHERE k.ID_Gebaeude = ? ORDER BY 1, k.Groesse, p.Rang", new DbParam("@g", gebaeude)).Select(x => "P " + x));
            zeilen.AddRange(Abdruck("SELECT " + ZONE_NAME + ", k.Groesse, k.Zeile, k.Wert, k.Aus, k.Von, k.Bis, k.Bedingt_K FROM Tab_Konditionierungsvorgabe k " +
                                    "WHERE k.ID_Gebaeude = ? ORDER BY 1, k.Groesse, k.Zeile", new DbParam("@g", gebaeude)).Select(x => "V " + x));
            List<string> spalten = Abdruck("SELECT name FROM pragma_table_info('Tab_Zone') WHERE name NOT IN ('ID', 'ID_Gebaeude', ?) ORDER BY cid",
                                           new DbParam("@n", RaumnutzungSchema.SPALTE_ZONE_NUTZUNGSPROFIL));
            zeilen.AddRange(Abdruck("SELECT " + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) + " FROM Tab_Zone WHERE ID_Gebaeude = ? ORDER BY Bezeichner",
                                    new DbParam("@g", gebaeude)).Select(x => "Z " + x));
            List<string> nutzung = Abdruck("SELECT " + ZONE_NAME + ", k.Groesse, k.Nutzung FROM Tab_Konditionierungskalender k WHERE k.ID_Gebaeude = ? ORDER BY 1, k.Groesse",
                                           new DbParam("@g", gebaeude));
            return (zeilen, nutzung);
        }

        private static long ProfilId(string name)
        {
            var c = new RaumnutzungCtrl();
            long kat = c.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER).Id;
            return c.Profile(kat).Single(p => p.Bezeichner == name).Id;
        }

        private static void Gleich(List<string> vorlage, List<string> profil, string wo)
        {
            if (vorlage.SequenceEqual(profil)) return;
            int i = 0;
            while (i < Math.Min(vorlage.Count, profil.Count) && vorlage[i] == profil[i]) i++;
            Assert.Fail(wo + ": weicht ab ab Zeile " + i + "\n  Vorlage: " + (i < vorlage.Count ? vorlage[i] : "∅") +
                        "\n  Profil:  " + (i < profil.Count ? profil[i] : "∅"));
        }

        /// <summary>Die Nutzung der Kalender am Ziel: Kennung → Profilname (NP-F15); alle übrigen Kalender behalten ihre.</summary>
        private static IEnumerable<string> Umbenannt(IEnumerable<string> nutzung, string ziel, string kennung, string profil)
            => nutzung.Select(x => x.StartsWith(ziel + "|", StringComparison.Ordinal) && x.EndsWith("|" + kennung, StringComparison.Ordinal)
                                   ? x.Substring(0, x.Length - kennung.Length) + profil
                                   : x);

        [Theory]
        [InlineData(RaumnutzungSaat.WOHNEN, DbWerte.KOND_NUTZUNG_WOHNEN)]
        [InlineData(RaumnutzungSaat.BUERO, DbWerte.KOND_NUTZUNG_BUERO)]
        [InlineData(RaumnutzungSaat.SCHULE, DbWerte.KOND_NUTZUNG_SCHULE)]
        public void Profil_uebernehmen_ist_bitgleich_zu_Vorlage_uebernehmen(string profil, string kennung)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var c = new RaumnutzungCtrl();
            long id = ProfilId(profil);

            // An der Zone.
            Ziel a = Kopie("NP1b Vorlage Zone " + profil), b = Kopie("NP1b Profil Zone " + profil);
            Vorlagenweg(a.Gebaeude, a.Zone, kennung);
            RaumnutzungCtrl.Uebernahme u = c.ProfilUebernehmen(id, b.Gebaeude, b.Zone, null);
            Assert.True(u.Ok, u.Meldung);
            Assert.Equal(profil, u.Profilname);
            Assert.All(u.Posten.Where(x => x.Uebernommen), x => Assert.Equal(Raumnutzungsweg.Zeilenbild, x.Weg));
            var va = Konditionierung(a.Gebaeude);
            var vb = Konditionierung(b.Gebaeude);
            Gleich(va.Zeilen, vb.Zeilen, profil + " an der Zone");
            Assert.Equal(Umbenannt(va.Nutzung, ZONE, kennung, profil), vb.Nutzung);
            Assert.Contains(vb.Nutzung, x => x.StartsWith(ZONE + "|", StringComparison.Ordinal) && x.EndsWith("|" + profil, StringComparison.Ordinal));
            Assert.Equal(profil, Convert.ToString(DataRepository.ExecuteScalar("SELECT Nutzungsprofil FROM Tab_Zone WHERE ID = ?",
                                                                              new DbParam("@z", b.Zone)), CultureInfo.InvariantCulture));

            // Am Gebäude (Zonenkalender nach F2 inbegriffen).
            Ziel ga = Kopie("NP1b Vorlage Gebäude " + profil), gb = Kopie("NP1b Profil Gebäude " + profil);
            Vorlagenweg(ga.Gebaeude, null, kennung);
            u = c.ProfilUebernehmen(id, gb.Gebaeude, null, null);
            Assert.True(u.Ok, u.Meldung);
            var ka = Konditionierung(ga.Gebaeude);
            var kb = Konditionierung(gb.Gebaeude);
            Gleich(ka.Zeilen, kb.Zeilen, profil + " am Gebäude");
            Assert.Equal(Umbenannt(ka.Nutzung, "—", kennung, profil), kb.Nutzung);
        }

        [Fact]
        public void Die_Zonenschleife_rechnet_mit_Profil_Buero_dieselben_Reihen_wie_mit_den_Vorlagen_Buero()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Ziel a = Kopie("NP1b Lauf Vorlage"), b = Kopie("NP1b Lauf Profil");
            Vorlagenweg(a.Gebaeude, a.Zone, DbWerte.KOND_NUTZUNG_BUERO);
            Assert.True(new RaumnutzungCtrl().ProfilUebernehmen(ProfilId(RaumnutzungSaat.BUERO), b.Gebaeude, b.Zone, null).Ok);

            AufheizLauf.Gebaeudelauf la = Assert.Single(AufheizLauf.Projekt(a.Projekt, KonfigurationCtrl.AufheizvorgabeLesen(a.Projekt)));
            AufheizLauf.Gebaeudelauf lb = Assert.Single(AufheizLauf.Projekt(b.Projekt, KonfigurationCtrl.AufheizvorgabeLesen(b.Projekt)));
            Assert.True(la.Gerechnet && lb.Gerechnet);
            Assert.NotNull(la.Mehrzonen);
            AufheizGrenzfallTests.Bitgleich(la.Ziel, lb.Ziel, "Heizreihe");
            AufheizGrenzfallTests.Bitgleich(la.Ergebnis, lb.Ergebnis, "Gebäude");
            for (int i = 0; i < la.Mehrzonen.Zonen.Count; i++)
                AufheizGrenzfallTests.Bitgleich(la.Mehrzonen.Zonen[i], lb.Mehrzonen.Zonen[i], la.Mehrzonen.Eingaenge[i].Bezeichnung);
            Assert.True(la.Mehrzonen.Zonen.Sum(z => z.HeizlastW.Sum()) > 0.0);
        }

        [Fact]
        public void Die_neuen_Muster_erzeugen_ihre_Kalender_und_Sonstige_keinen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var c = new RaumnutzungCtrl();
            Ziel z = Kopie("NP1b Muster");

            var erwartet = new Dictionary<string, string[]>
            {
                [RaumnutzungSaat.SPORT] = new[] { "HEIZSOLL", "KUEHLSOLL", "LUEFTUNG", "GERAETE", "PERSONEN" },
                [RaumnutzungSaat.GASTRONOMIE] = new[] { "HEIZSOLL", "KUEHLSOLL", "LUEFTUNG", "GERAETE", "PERSONEN" },
                [RaumnutzungSaat.LAGER] = new[] { "HEIZSOLL", "KUEHLSOLL", "GERAETE", "PERSONEN" },
                [RaumnutzungSaat.VERKEHR] = new[] { "HEIZSOLL", "KUEHLSOLL", "GERAETE", "PERSONEN" },
                [RaumnutzungSaat.TECHNIK] = new[] { "HEIZSOLL", "KUEHLSOLL", "GERAETE", "PERSONEN" },
            };
            foreach (KeyValuePair<string, string[]> m in erwartet)
            {
                RaumnutzungCtrl.Uebernahme u = c.ProfilUebernehmen(ProfilId(m.Key), z.Gebaeude, z.Zone, null);
                Assert.True(u.Ok, m.Key + ": " + u.Meldung);
                Assert.Equal(m.Value, u.Posten.Where(p => p.Uebernommen).Select(p => Konditionierungsgroessen.Kennwort(p.Groesse)));
                Assert.Equal(m.Key, Convert.ToString(DataRepository.ExecuteScalar("SELECT Nutzungsprofil FROM Tab_Zone WHERE ID = ?",
                                                                                 new DbParam("@z", z.Zone)), CultureInfo.InvariantCulture));
                foreach (string g in m.Value)
                    Assert.Equal(m.Key, Convert.ToString(DataRepository.ExecuteScalar(
                        "SELECT Nutzung FROM Tab_Konditionierungskalender WHERE ID_Zone = ? AND Groesse = ?",
                        new DbParam("@z", z.Zone), new DbParam("@g", g)), CultureInfo.InvariantCulture));
            }

            // Gastronomie: Montag Ruhetag als Standardwoche (Heizen), Kühlen außerhalb aus.
            RaumnutzungCtrl.Uebernahme gastro = c.ProfilUebernehmen(ProfilId(RaumnutzungSaat.GASTRONOMIE), z.Gebaeude, z.Zone, null);
            Assert.All(gastro.Posten, p => Assert.True(p.Ersetzt));
            Assert.Equal(Raumnutzungshinweis.FreierTagOhneTageswert, gastro.Posten.Single(p => p.Groesse == Konditionierungsgroesse.Lueftung).Hinweis);
            string woche = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Woche FROM Tab_Konditionierungskalender WHERE ID_Zone = ? AND Groesse = 'HEIZSOLL'", new DbParam("@z", z.Zone)),
                CultureInfo.InvariantCulture);
            string[] w = woche.Split(';');
            Assert.Equal(168, w.Length);
            Assert.Equal("16", w[Kalenderwoche.Stelle(0, 12)]);
            Assert.Equal("20", w[Kalenderwoche.Stelle(1, 12)]);

            // Sonstige: keine Kalender, nur der Name an der Zone (NP-F13); die Kalender der Zone bleiben.
            int vorher = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Zone = ?",
                                                                     new DbParam("@z", z.Zone)), CultureInfo.InvariantCulture);
            RaumnutzungCtrl.Uebernahme s = c.ProfilUebernehmen(ProfilId(RaumnutzungSaat.SONSTIGE), z.Gebaeude, z.Zone, null);
            Assert.True(s.Ok, s.Meldung);
            Assert.All(s.Posten, p =>
            {
                Assert.False(p.Uebernommen);
                Assert.Equal(Raumnutzungshinweis.ProfilOhneWerte, p.Hinweis);
            });
            Assert.Equal(vorher, Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Zone = ?",
                                                                              new DbParam("@z", z.Zone)), CultureInfo.InvariantCulture));
            Assert.Equal(RaumnutzungSaat.SONSTIGE, Convert.ToString(DataRepository.ExecuteScalar("SELECT Nutzungsprofil FROM Tab_Zone WHERE ID = ?",
                                                                                               new DbParam("@z", z.Zone)), CultureInfo.InvariantCulture));

            // Unbeheizte Zone: weder Heiz- noch Kühlkalender.
            long keller = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Zone WHERE ID_Gebaeude = ? AND Bezeichner = ?",
                                                                       new DbParam("@g", z.Gebaeude), new DbParam("@b", Zonenprojekt1052.ZONE_KELLER)),
                                          CultureInfo.InvariantCulture);
            RaumnutzungCtrl.Uebernahme k = c.ProfilUebernehmen(ProfilId(RaumnutzungSaat.LAGER), z.Gebaeude, keller, null);
            Assert.True(k.Ok, k.Meldung);
            Assert.True(k.Posten.Single(p => p.Groesse == Konditionierungsgroesse.Heizsoll).Unbeheizt);
            Assert.Equal(new[] { "GERAETE", "PERSONEN" }, k.Posten.Where(p => p.Uebernommen).Select(p => Konditionierungsgroessen.Kennwort(p.Groesse)));

            // Ein unbekanntes Profil wird benannt abgelehnt.
            Assert.False(c.ProfilUebernehmen(999999, z.Gebaeude, z.Zone, null).Ok);
        }
    }
}
