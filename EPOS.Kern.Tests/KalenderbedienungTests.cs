using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kalenderbedienung, Stufe 1</b> (Konzept Konditionierungsprofile 7.8, E110; Welle K1a): Wochenprofile,
    /// gekoppelte Zuordnungszeilen, Einzeltage, Ferien mit Spiegelung der vier Gebäudespalten, Feiertage, Monatskopie,
    /// Jahresraster samt Rangregel und Vorlage für alle Größen — rein über dem Arbeitsstand des Probegebäudes aus
    /// <see cref="KonditionierungsarbeitTests"/>.
    /// </summary>
    public sealed class KalenderbedienungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly Konditionierungsgroesse H = Konditionierungsgroesse.Heizsoll;
        private static readonly Konditionierungsgroesse L = Konditionierungsgroesse.Lueftung;

        /// <summary>Das Probegebäude mit angelegten Kalendern Heizen und Lüftung (Tag- und Nachtzeilen, Ferienwert 16 °C).</summary>
        private static Konditionierungsarbeitsstand Angelegt()
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand();
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(H), DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(21.0)));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(H), DbWerte.KOND_ZEILE_FERIEN, Matrixzelle.AusWert(16.0)));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.Anlegen(a, Ort(H)));
            a = KonditionierungsarbeitTests.Gut(Konditionierungsarbeit.Anlegen(a, Ort(L)));
            Assert.NotNull(a.Gebaeude.Kalender(H));
            Assert.NotNull(a.Gebaeude.Kalender(L));
            return a;
        }

        private static Konditionierungsort Ort(Konditionierungsgroesse g, long? zone = null) => new Konditionierungsort(g, zone);

        private static Konditionierungsarbeitsstand Gut(Konditionierungsschritt s) => KonditionierungsarbeitTests.Gut(s);

        private static Zuordnungszeile Zeile(Konditionierungsarbeitsstand a, string name)
            => Kalenderbedienung.Zuordnungen(a, null).Single(z => z.Schluessel.Name == name);

        // =============================================================================
        //  Wochenprofile
        // =============================================================================

        [Fact]
        public void Der_Pinsel_schreibt_die_Standardwoche_und_Montag_und_Samstag_werden_kopiert()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            var p = new Profilort(Ort(H));
            IReadOnlyList<Wochenprofil> profile = Kalenderbedienung.Wochenprofile(a, Ort(H));
            Assert.True(profile[0].IstStandardwoche);

            a = Gut(Kalenderbedienung.PinselAnwenden(a, p, 0, 0, 6, 22, 22.0));       // Montag 6–22 Uhr
            a = Gut(Kalenderbedienung.PinselAnwenden(a, p, 5, 5, 0, 24, null));       // Samstag „aus"
            a = Gut(Kalenderbedienung.MontagNachDienstagBisFreitag(a, p));
            a = Gut(Kalenderbedienung.SamstagNachSonntag(a, p));

            double[] w = Kalenderbedienung.Profil(a, p).Werte;
            for (int t = 0; t <= 4; t++) Assert.Equal(22.0, w[Kalenderwoche.Stelle(t, 10)]);
            Assert.True(double.IsNaN(w[Kalenderwoche.Stelle(6, 10)]));
            Assert.Equal(w[Kalenderwoche.Stelle(0, 3)], w[Kalenderwoche.Stelle(4, 3)]);

            // Zellen außerhalb des Rasters: benannt abgelehnt.
            Assert.False(Kalenderbedienung.PinselAnwenden(a, p, 0, 7, 0, 24, 20.0).Ok);
            // Ein Wert außerhalb der Grenzen prüft der Schreibweg.
            Assert.False(Kalenderbedienung.PinselAnwenden(a, p, 0, 0, 0, 1, 99.0).Ok);
        }

        [Fact]
        public void Woche_kopieren_geht_nur_zwischen_Groessen_gleicher_Einheit()
        {
            Assert.True(Kalenderbedienung.GleicheEinheit(H, Konditionierungsgroesse.Kuehlsoll));
            Assert.True(Kalenderbedienung.GleicheEinheit(Konditionierungsgroesse.Geraete, Konditionierungsgroesse.Personen));
            Assert.False(Kalenderbedienung.GleicheEinheit(L, H));
            Assert.False(Kalenderbedienung.GleicheEinheit(L, Konditionierungsgroesse.Personen));

            Konditionierungsarbeitsstand a = Angelegt();
            Konditionierungsschritt s = Kalenderbedienung.WocheKopieren(a, new Profilort(Ort(H)), new Profilort(Ort(L)));
            Assert.False(s.Ok);
            Assert.Contains("Einheit", s.Meldung);

            // In ein Profil derselben Größe: die Woche einer Zeile wird die Standardwoche.
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Ferienwoche", 180, 190),
                                                      Zuordnungsangabe.Profilwoche(null), new[] { H }));
            int rang = Zeile(a, "Ferienwoche").Raenge[H];
            a = Gut(Kalenderbedienung.PinselAnwenden(a, new Profilort(Ort(H), rang), 0, 6, 0, 24, 12.0));
            a = Gut(Kalenderbedienung.WocheKopieren(a, new Profilort(Ort(H), rang), new Profilort(Ort(H))));
            Assert.All(Kalenderbedienung.Profil(a, new Profilort(Ort(H))).Werte, v => Assert.Equal(12.0, v));
        }

        // =============================================================================
        //  Zuordnung mit gekoppelten Kopien
        // =============================================================================

        [Fact]
        public void Eine_Zeile_fuer_alle_steht_in_jeder_Groesse_und_aendert_und_loescht_gekoppelt()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            var alt = Zuordnungsschluessel.Zeitraum("Betriebsruhe", 100, 110);
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, alt, Zuordnungsangabe.Abgeschaltet));
            Zuordnungszeile z = Zeile(a, "Betriebsruhe");
            Assert.Equal(new[] { H, L }, z.GiltFuer);
            Assert.All(z.Angaben.Values, x => Assert.Equal(Angabeart.Aus, x.Art));

            // Doppelt anlegen: benannt abgelehnt.
            Assert.False(Kalenderbedienung.ZuordnungSetzen(a, null, null, alt, Zuordnungsangabe.Abgeschaltet).Ok);

            // Ändern auf neue Tage und nur noch Heizen: Rang bleibt, Lüftung verliert die Kopie.
            var neu = Zuordnungsschluessel.Zeitraum("Betriebsruhe", 105, 115);
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, alt, neu, Zuordnungsangabe.AlsWert(15.0), new[] { H }));
            Zuordnungszeile n = Zeile(a, "Betriebsruhe");
            Assert.Equal(new[] { H }, n.GiltFuer);
            Assert.Equal(z.Raenge[H], n.Raenge[H]);
            Assert.Equal(105, n.Schluessel.Beginn);
            Assert.DoesNotContain(a.Gebaeude.Kalender(L).Perioden, r => r.Bezeichner == "Betriebsruhe");

            // Löschen wirkt auf alle Kopien; eine unbekannte Zeile wird benannt abgelehnt.
            a = Gut(Kalenderbedienung.ZuordnungLoeschen(a, null, neu));
            Assert.Empty(Kalenderbedienung.Zuordnungen(a, null));
            Assert.False(Kalenderbedienung.ZuordnungLoeschen(a, null, neu).Ok);

            // Eine Größe ohne angelegten Kalender: benannt abgelehnt.
            Assert.False(Kalenderbedienung.ZuordnungSetzen(a, null, null, alt, Zuordnungsangabe.Abgeschaltet,
                                                           new[] { Konditionierungsgroesse.Personen }).Ok);
        }

        [Fact]
        public void Eine_Zeile_kopiert_die_Woche_eines_benannten_Profils_und_ein_fehlendes_wird_benannt()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Sommerwoche", 152, 180),
                                                      Zuordnungsangabe.Profilwoche(null)));
            Assert.Equal(2, Kalenderbedienung.Wochenprofile(a, Ort(H)).Count);
            int rang = Zeile(a, "Sommerwoche").Raenge[H];
            a = Gut(Kalenderbedienung.PinselAnwenden(a, new Profilort(Ort(H), rang), 0, 6, 0, 24, 17.0));

            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Spätsommer", 213, 243),
                                                      Zuordnungsangabe.Profilwoche("Sommerwoche"), new[] { H }));
            Assert.All(Zeile(a, "Spätsommer").Angaben[H].Woche, v => Assert.Equal(17.0, v));

            Konditionierungsschritt s = Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("X", 1, 2),
                                                                          Zuordnungsangabe.Profilwoche("Gibt es nicht"));
            Assert.False(s.Ok);
            Assert.Contains("Gibt es nicht", s.Meldung);
        }

        [Fact]
        public void Ein_Einzeltag_wirkt_wie_Sonntag_und_der_Klick_im_Raster_findet_ihn()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            a = Gut(Kalenderbedienung.EinzeltagSetzen(a, null, null, 122, "Brückentag", aus: false));
            Zuordnungszeile z = Zeile(a, "Brückentag");
            Assert.True(z.IstEinzeltag);
            Assert.All(z.Angaben.Values, x => Assert.Equal(7, x.WieWochentag));

            IReadOnlyList<Rastertag> raster = Kalenderbedienung.Jahresraster(a, Ort(L));
            Assert.Equal(Rastertagart.Einzeltag, raster[121].Art);
            Assert.Equal(5, raster[121].Monat);
            Assert.Equal(2, raster[121].TagImMonat);
            Assert.Equal(z.Schluessel, Kalenderbedienung.TagAufloesen(a, Ort(L), 122));
            Assert.Null(Kalenderbedienung.TagAufloesen(a, Ort(L), 10));
        }

        // =============================================================================
        //  Ferien, Saison, Feiertage
        // =============================================================================

        [Fact]
        public void Beliebig_viele_Ferien_die_ersten_vier_spiegeln_die_Gebaeudespalten()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            var ferien = new[] { (10, 15), (60, 70), (100, 110), (200, 214), (280, 290), (355, 5) };
            a = Gut(Kalenderbedienung.FerienSetzen(a, ferien));

            Matrixeingang b = a.Gebaeude.Bestand;
            for (int k = 0; k < 4; k++)
            {
                Assert.Equal(ferien[k].Item1, b.Ferienbeginn[k]);
                Assert.Equal(ferien[k].Item2, b.Ferienende[k]);
            }
            IReadOnlyList<Ferienzeile> liste = Kalenderbedienung.Ferienzeitraeume(a);
            Assert.Equal(6, liste.Count);
            Assert.Equal("Ferien 6", liste[5].Name);

            // Heizen trägt Ferien (Ferienzeile wirksam): 1–4 als Matrixbereich, 5 und 6 als Zeilen mit dem Ferienwert.
            Konditionierungskalender h = a.Gebaeude.Kalender(H);
            Assert.Equal(4, h.Perioden.Count(r => r.Art == DbWerte.KOND_ART_FERIEN));
            Kalenderregel f6 = h.Perioden.Single(r => r.Bezeichner == "Ferien 6");
            Assert.Equal(16.0, f6.Angabe.Wert);
            Assert.Equal(Standardfahrplan.RANG_EIGEN + 1, f6.Rang);
            Assert.Equal(Rastertagart.Ferien, Kalenderbedienung.Jahresraster(a, Ort(H))[2].Art);    // 3. Januar
            Assert.Equal(Rastertagart.Ferien, Kalenderbedienung.Jahresraster(a, Ort(H))[64].Art);

            // Weniger Ferien: die Zeilen ab 5 fallen, die übrigen Spalten werden „aus".
            a = Gut(Kalenderbedienung.FerienSetzen(a, new[] { (10, 15), (60, 70) }));
            Assert.Equal(0.0, a.Gebaeude.Bestand.Ferienbeginn[2]);
            Assert.Equal(2, Kalenderbedienung.Ferienzeitraeume(a).Count);
            Assert.DoesNotContain(a.Gebaeude.Kalender(H).Perioden, r => r.Bezeichner.StartsWith("Ferien 5", StringComparison.Ordinal));
            Assert.Equal(2, a.Gebaeude.Kalender(H).Perioden.Count(r => r.Art == DbWerte.KOND_ART_FERIEN));

            Assert.False(Kalenderbedienung.FerienSetzen(a, new[] { (0, 5) }).Ok);
        }

        [Fact]
        public void Feiertage_laden_koppelt_die_neun_Regeln_und_ein_Land_bringt_feste_Einzeltage()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            a = Gut(Kalenderbedienung.FeiertageLaden(a, null));
            List<Zuordnungszeile> feiertage = Kalenderbedienung.Zuordnungen(a, null).Where(z => z.Schluessel.IstFeiertag).ToList();
            Assert.Equal(9, feiertage.Count);
            Assert.All(feiertage, z => Assert.Equal(new[] { H, L }, z.GiltFuer));
            Assert.All(feiertage, z => Assert.InRange(z.Raenge[H], Standardfahrplan.RANG_FEIERTAG, Standardfahrplan.RANG_FEIERTAG_LETZTER));

            a = Gut(Kalenderbedienung.LandesfeiertageLaden(a, null, "BY"));
            List<Zuordnungszeile> land = Kalenderbedienung.Zuordnungen(a, null)
                .Where(z => z.Schluessel.Name == string.Format(CultureInfo.CurrentCulture, "Landesfeiertag {0}", "BY")).ToList();
            Assert.Equal(3, land.Count);                                     // Hl. Drei Könige, Fronleichnam, Allerheiligen
            Assert.Contains(land, z => z.Schluessel.Beginn == 6);
            Assert.Contains(land, z => z.Schluessel.Beginn == 305);
            Assert.Equal(Rastertagart.Einzeltag, Kalenderbedienung.Jahresraster(a, Ort(H))[5].Art);

            // Wiederholbar: kein zweiter Satz.
            a = Gut(Kalenderbedienung.LandesfeiertageLaden(a, null, "BY"));
            Assert.Equal(12, Kalenderbedienung.Zuordnungen(a, null).Count);
            Assert.False(Kalenderbedienung.LandesfeiertageLaden(a, null, "XX").Ok);
        }

        [Fact]
        public void Die_Saison_folgt_im_angelegten_Kalender_und_schlaegt_im_Raster_alles()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Sommer", 160, 170),
                                                      Zuordnungsangabe.AlsWert(18.0), new[] { H }));
            a = Gut(Kalenderbedienung.SaisonSetzen(a, Ort(H), 274, 120));
            IReadOnlyList<Rastertag> raster = Kalenderbedienung.Jahresraster(a, Ort(H));
            Assert.Equal(Rastertagart.Saison, raster[164].Art);
            Assert.Equal(Standardfahrplan.RANG_SAISON, raster[164].Rang);
            Assert.NotEqual(Rastertagart.Saison, raster[10].Art);
        }

        // =============================================================================
        //  Monat kopieren, Rangregel im Raster, Vorlage für alle Größen
        // =============================================================================

        [Fact]
        public void Monat_kopieren_verschiebt_beschneidet_und_koppelt()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Messe", 32, 40),
                                                      Zuordnungsangabe.Abgeschaltet));
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Inventur", 25, 35),
                                                      Zuordnungsangabe.AlsWert(14.0), new[] { H }));
            a = Gut(Kalenderbedienung.EinzeltagSetzen(a, null, null, 59, "Monatsende", aus: true));
            a = Gut(Kalenderbedienung.MonatKopieren(a, null, 2, 3));

            List<Zuordnungszeile> zeilen = Kalenderbedienung.Zuordnungen(a, null).ToList();
            Zuordnungszeile messe = zeilen.Single(z => z.Schluessel == Zuordnungsschluessel.Zeitraum("Messe", 60, 68));
            Assert.Equal(new[] { H, L }, messe.GiltFuer);
            Zuordnungszeile inventur = zeilen.Single(z => z.Schluessel == Zuordnungsschluessel.Zeitraum("Inventur", 60, 63));
            Assert.Equal(new[] { H }, inventur.GiltFuer);
            Assert.Equal(14.0, inventur.Angaben[H].Wert);
            Assert.Contains(zeilen, z => z.Schluessel == Zuordnungsschluessel.Zeitraum("Monatsende", 87, 87));

            // Die Kopien stehen über allen alten Zeilen, in der Folge der Quellränge.
            int hoechsterAlter = zeilen.Where(z => z.ErsterTag < 60).Max(z => z.Raenge[H]);
            Assert.True(messe.Raenge[H] > hoechsterAlter);
            Assert.True(zeilen.Single(z => z.Schluessel.Name == "Monatsende" && z.ErsterTag == 87).Raenge[H] > inventur.Raenge[H]);

            Assert.False(Kalenderbedienung.MonatKopieren(a, null, 7, 8).Ok);
            Assert.False(Kalenderbedienung.MonatKopieren(a, null, 0, 8).Ok);
        }

        [Fact]
        public void Im_Raster_gewinnt_die_ranghoehere_Zeile_und_das_Band_fasst_zusammen()
        {
            Konditionierungsarbeitsstand a = Angelegt();
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Lang", 100, 120),
                                                      Zuordnungsangabe.Abgeschaltet));
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Kurz", 110, 115),
                                                      Zuordnungsangabe.AlsWert(18.0), new[] { H }));
            IReadOnlyList<Rastertag> raster = Kalenderbedienung.Jahresraster(a, Ort(H));
            Assert.Equal("Kurz", raster[111].Quelle);
            Assert.Equal("Lang", raster[104].Quelle);
            Assert.Equal("Lang", Kalenderbedienung.Jahresraster(a, Ort(L))[111].Quelle);

            IReadOnlyList<Bandabschnitt> band = Kalenderbedienung.Jahresband(raster);
            Assert.Contains(band, x => x.Quelle == "Lang" && x.Beginn == 100 && x.Ende == 109);
            Assert.Contains(band, x => x.Quelle == "Kurz" && x.Beginn == 110 && x.Ende == 115);
            Assert.Contains(band, x => x.Quelle == "Lang" && x.Beginn == 116 && x.Ende == 120);
            Assert.Equal(365, band.Sum(x => x.Ende - x.Beginn + 1));
        }

        [Fact]
        public void Vorlage_fuer_alle_Groessen_legt_jede_Groesse_in_einem_Schritt_an()
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand();
            Konditionierungsstand leer = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
            var vorlagen = new[]
            {
                new Konditionierungsvorlage(1, "Büro", H, leer.MitVorgabe(H, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(21.0))),
                new Konditionierungsvorlage(2, "Büro", L, leer.MitVorgabe(L, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5))),
            };
            a = Gut(Kalenderbedienung.VorlageAlleUebernehmen(a, null, vorlagen));
            Assert.Equal("Büro", a.Gebaeude.Herkunft(H).Vorlage);
            Assert.Equal("Büro", a.Gebaeude.Herkunft(L).Vorlage);

            // Eine Größe, die ablehnt: nichts geändert, die Meldung kommt zurück.
            var falsch = new[] { vorlagen[0], new Konditionierungsvorlage(3, "Büro", L,
                leer.MitVorgabe(L, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(99.0))) };
            Assert.False(Kalenderbedienung.VorlageAlleUebernehmen(KonditionierungsarbeitTests.Stand(), null, falsch).Ok);
        }
    }
}
