using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zuordnung der eingespielten Typtage zum Kalender</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.2 „VDI-4655-Typtage (Z4b)", 5.3): 365 Tage, Jahreszeit aus der
    /// Tagesmitteltemperatur, Tagart aus dem Zapfkalender, Bewölkung aus dem Bedeckungsgrad;
    /// die Tagesmengen nach der Gleichung, mit genullten Faktoren und auf die Jahresmenge skaliert; die
    /// Kontrolle gegen die eingespielte Tabelle; die Weiche im Rechner.
    ///
    /// <para><b>Alle Werte erfunden</b> (<see cref="Typtagpaketbauer.Erfunden"/>); kein Wert
    /// einer Richtlinie steht in dieser Klasse.</para>
    /// </summary>
    public sealed class TyptagzuordnungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private const int ZONE = 3;
        private const string ART = "probehaus";
        private const string NAME = "Zone A";

        private static Normformvektorsatz Satz(Typtagpaketbauer b)
        {
            Normformvektorsatz s = Normformvektorleser.AusDateien(b.Dateien(), out ZapfSatz fehler);
            Assert.Null(fehler);
            return s;
        }

        /// <summary>Tagesmittel: die ersten <paramref name="winter"/> Tage kalt, dann <paramref name="sommer"/> warm, der Rest Übergang.</summary>
        private static double[] Temperaturen(int winter = 100, int sommer = 100,
                                             double kalt = 0.0, double warm = 20.0, double mittel = 10.0)
        {
            var t = new double[365];
            for (int i = 0; i < 365; i++) t[i] = i < winter ? kalt : i < winter + sommer ? warm : mittel;
            return t;
        }

        private static double[] Bedeckung(double wert)
            => Enumerable.Repeat(wert, 365).ToArray();

        private static Typtaganbindung Anbindung(Normformvektorsatz satz, double[] temperaturen = null,
                                                 double[] bedeckung = null)
            => new Typtaganbindung
            {
                Daten = satz, Klimazone = ZONE, Gebaeudeart = ART,
                TagesmittelC = temperaturen ?? Temperaturen(),
                BedeckungAchtel = bedeckung
            };

        // =================================================================================
        //  Die Zuordnung
        // =================================================================================

        [Fact]
        public void Jeder_der_365_Tage_bekommt_einen_Typtag()
        {
            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            var hinweise = new List<ZapfHinweis>();
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME, null, hinweise);

            Assert.Equal(365, jahr.Tage.Count);
            Assert.Equal(Enumerable.Range(1, 365), jahr.Tage.Select(t => t.Tag));
            Assert.All(jahr.Tage, t => Assert.NotNull(satz.Kategorie(t.Typtag)));
            Assert.Equal(365, jahr.AnzahlJeTyptag.Values.Sum());
            Assert.Equal(ZONE, jahr.Klimazone);
            Assert.Equal(ART, jahr.Gebaeudeart);

            // Die Jahreszeit folgt der Temperatur: 100 kalte Tage, 100 warme, 165 dazwischen.
            Assert.Equal(100, jahr.Tage.Count(t => t.Jahreszeit == Typtagjahreszeit.Winter));
            Assert.Equal(100, jahr.Tage.Count(t => t.Jahreszeit == Typtagjahreszeit.Sommer));
            Assert.Equal(165, jahr.Tage.Count(t => t.Jahreszeit == Typtagjahreszeit.Uebergang));

            // Die Tagart folgt dem Kalender: 52 Sonntage, der Samstag bleibt Werktag.
            Assert.Equal(52, jahr.Tage.Count(t => t.Tagart == Typtagart.Sonntag));
            Assert.Equal(313, jahr.Tage.Count(t => t.Tagart == Typtagart.Werktag));
        }

        [Fact]
        public void Ein_Feiertag_zaehlt_als_Sonntag_ein_Samstag_bleibt_Werktag()
        {
            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            // Jahr beginnt am Montag: Tag 8 ist ein Montag, Tag 6 ein Samstag, Tag 7 ein Sonntag.
            bool[] we = We(0, 8);
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, we, NAME);

            Assert.Equal(Typtagart.Sonntag, jahr.Tage[7].Tagart);      // Feiertag am Montag
            Assert.Equal(Typtagart.Werktag, jahr.Tage[5].Tagart);      // Samstag
            Assert.Equal(Typtagart.Sonntag, jahr.Tage[6].Tagart);      // Sonntag
            Assert.Equal(53, jahr.Tage.Count(t => t.Tagart == Typtagart.Sonntag));
        }

        [Fact]
        public void Die_Bewoelkungsschwelle_entscheidet_heiter_gegen_bewoelkt()
        {
            const double schwelle = Typtagpaketbauer.GRENZE_BEWOELKUNG;
            Normformvektorsatz satz = Satz(Typtagpaketbauer.MitBewoelkung(ZONE, ART, schwelle));

            Typtagjahr heiter = Typtagzuordnung.Zuordnen(
                Anbindung(satz, bedeckung: Bedeckung(schwelle - 0.001)), 0, We(0), NAME);
            Assert.DoesNotContain(heiter.Tage, t => t.Bewoelkung == Typtagbewoelkung.Bewoelkt);
            Assert.Contains(heiter.Tage, t => t.Bewoelkung == Typtagbewoelkung.Heiter);

            // Genau auf der Schwelle gilt bewoelkt (>=).
            Typtagjahr bewoelkt = Typtagzuordnung.Zuordnen(
                Anbindung(satz, bedeckung: Bedeckung(schwelle)), 0, We(0), NAME);
            Assert.DoesNotContain(bewoelkt.Tage, t => t.Bewoelkung == Typtagbewoelkung.Heiter);
            Assert.Contains(bewoelkt.Tage, t => t.Bewoelkung == Typtagbewoelkung.Bewoelkt);

            // Der Sommer unterscheidet nicht - seine Tage bleiben "ohne".
            Assert.All(bewoelkt.Tage.Where(t => t.Jahreszeit == Typtagjahreszeit.Sommer),
                       t => Assert.Equal(Typtagbewoelkung.Ohne, t.Bewoelkung));
        }

        // =================================================================================
        //  Die Kontrolle gegen die eingespielte Tabelle
        // =================================================================================

        [Fact]
        public void Eine_abweichende_Zahl_der_Typtage_ist_ein_Hinweis()
        {
            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            var hinweise = new List<ZapfHinweis>();
            Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME, null, hinweise);

            ZapfHinweis h = Assert.Single(hinweise, x => x.Code == Typtagzuordnung.HINWEIS_ANZAHL);
            Assert.Equal(NAME, h.Zone);
            Assert.Contains(NAME, h.Text);
        }

        [Fact]
        public void Stimmt_die_Zahl_der_Typtage_bleibt_der_Hinweis_aus()
        {
            // Erster Lauf: die gerechnete Zahl je Kategorie ermitteln.
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            double[] temperaturen = Temperaturen();
            Typtagjahr erst = Typtagzuordnung.Zuordnen(Anbindung(Satz(b), temperaturen), 0, We(0), NAME);

            // Zweiter Lauf: das Paket traegt genau diese Zahlen (Summe bleibt 365).
            foreach (var k in b.Kategorien)
                b.Anzahl[(ZONE, ART, k.Code)] = erst.AnzahlJeTyptag.TryGetValue(k.Code, out int n) ? n : 0;
            Assert.Equal(365, b.Tagesumme(ZONE, ART));

            var hinweise = new List<ZapfHinweis>();
            Typtagzuordnung.Zuordnen(Anbindung(Satz(b), temperaturen), 0, We(0), NAME, null, hinweise);
            Assert.DoesNotContain(hinweise, h => h.Code == Typtagzuordnung.HINWEIS_ANZAHL);
        }

        [Fact]
        public void Die_Ferienfenster_der_Zone_wirken_auf_dem_Typtagweg_nicht()
        {
            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            var hinweise = new List<ZapfHinweis>();
            var ferien = new[] { new Ferienfenster(200, 220) };
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME, ferien, hinweise);

            // Der Ferientag traegt den Typtag seiner Jahreszeit und Tagart, keinen Ruhetag.
            Assert.NotNull(satz.Kategorie(jahr.Tage[204].Typtag));
            Assert.Contains(hinweise, h => h.Code == Typtagzuordnung.HINWEIS_FERIEN);
        }

        // =================================================================================
        //  Die Tagesmengen
        // =================================================================================

        [Fact]
        public void Die_Tagesmengen_erhalten_die_Jahresmenge()
        {
            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME);

            const double qa = 12345.0;
            var hinweise = new List<ZapfHinweis>();
            double[] tage = Typtagzuordnung.Tagesmengen(qa, 10.0, jahr, NAME, hinweise);

            Assert.Equal(365, tage.Length);
            Assert.All(tage, q => Assert.True(q >= 0.0));
            Assert.True(Relativ(tage.Sum(), qa) < 1e-12);
            Assert.DoesNotContain(hinweise, h => h.Code == Typtagzuordnung.HINWEIS_FAKTOR_NULL);

            // Ein Tag mit groesserem Faktor traegt mehr als einer mit kleinerem.
            int gross = Array.IndexOf(jahr.Faktoren, jahr.Faktoren.Max());
            int klein = Array.IndexOf(jahr.Faktoren, jahr.Faktoren.Min());
            Assert.True(tage[gross] > tage[klein]);
        }

        /// <summary>
        /// Grundlagen 5, Abschnitt 2.5, Anmerkung zu Gl. (1)–(3): Nicht die Tagesmenge wird auf 0
        /// geklemmt — der FAKTOR des betroffenen Typtags wird auf 0 gesetzt, sein Tag trägt dann
        /// den Mittelwertanteil Q_a/365. Die Nullung gilt für JEDEN Tag dieses Typtags.
        /// </summary>
        [Fact]
        public void Ein_negativer_Tagesbedarf_nullt_den_Faktor_des_Typtags()
        {
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            string betroffen = b.Kategorien[0].Code;
            b.Faktor[(ZONE, ART, betroffen)] = -0.01;                 // 1/365 + 10 * (-0,01) < 0
            Normformvektorsatz satz = Satz(b);
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME);

            var hinweise = new List<ZapfHinweis>();
            double[] tage = Typtagzuordnung.Tagesmengen(1000.0, 10.0, jahr, NAME, hinweise);

            ZapfHinweis h = Assert.Single(hinweise, x => x.Code == Typtagzuordnung.HINWEIS_FAKTOR_NULL);
            Assert.True(h.Warnung);
            Assert.Contains(hinweise, x => x.Code == Typtagzuordnung.HINWEIS_SKALIERUNG);

            // Kein Tag traegt 0: der genullte Faktor gibt den Mittelwertanteil.
            Assert.All(tage, q => Assert.True(q > 0.0));
            Assert.True(Relativ(tage.Sum(), 1000.0) < 1e-12);         // die Jahresmenge bleibt

            // Jeder Tag des betroffenen Typtags traegt denselben Betrag — und zwar den kleinsten,
            // weil jeder andere Typtag dieses Pakets einen positiven Faktor hat.
            int[] seine = Enumerable.Range(0, 365).Where(i => jahr.Tage[i].Typtag == betroffen).ToArray();
            Assert.NotEmpty(seine);
            Assert.All(seine, i => Assert.Equal(tage[seine[0]], tage[i], 12));

            // Der genullte Typtag traegt genau den Mittelwertanteil: Sein Verhaeltnis zu einem
            // Typtag mit dem Faktor f ist (1/365) / (1/365 + n_E·f) — unabhaengig von der Skalierung.
            string vergleich = b.Kategorien[2].Code;
            double f = b.Faktor[(ZONE, ART, vergleich)];
            int j = Enumerable.Range(0, 365).First(i => jahr.Tage[i].Typtag == vergleich);
            Assert.Equal((1.0 / 365.0) / (1.0 / 365.0 + 10.0 * f), tage[seine[0]] / tage[j], 9);
        }

        [Fact]
        public void Ein_Jahr_ohne_Zapfung_verteilt_nichts_und_lehnt_benannt_ab()
        {
            // 1/365 + 1 * (-1/365) ist genau 0: nichts wird genullt, und nichts wird verteilt.
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            foreach (var k in b.Kategorien) b.Faktor[(ZONE, ART, k.Code)] = -1.0 / 365.0;
            Normformvektorsatz satz = Satz(b);
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME);

            ZapfprofilEingabeException ex = Assert.Throws<ZapfprofilEingabeException>(
                () => Typtagzuordnung.Tagesmengen(1000.0, 1.0, jahr, NAME));
            Assert.Equal(ZapfEingabefehler.KeineVerteilung, ex.Fehler);
            Assert.Equal("EINGABE_TYPTAGE_KEINE_VERTEILUNG", ex.Kennung);

            // Ohne Jahresmenge bleibt alles 0 - ohne Ablehnung.
            Assert.All(Typtagzuordnung.Tagesmengen(0.0, 1.0, jahr, NAME), q => Assert.Equal(0.0, q));
        }

        // =================================================================================
        //  Die Stundenreihe aus den Tagesgängen des Pakets
        // =================================================================================

        [Fact]
        public void Ohne_Tagesgaenge_liefert_die_Stundenreihe_null_und_einen_Hinweis()
        {
            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME);
            double[] tage = Typtagzuordnung.Tagesmengen(1000.0, 10.0, jahr, NAME);

            var hinweise = new List<ZapfHinweis>();
            Assert.Null(Typtagzuordnung.Stundenreihe(tage, jahr, satz, ART, NAME, hinweise));
            Assert.Contains(hinweise, h => h.Code == Typtagzuordnung.HINWEIS_OHNE_GANG);
        }

        [Fact]
        public void Mit_Tagesgaengen_traegt_die_Stundenreihe_die_Jahresmenge()
        {
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            foreach (var k in b.Kategorien) b.Gaenge.Add((ART, k.Code, 60, Stundengang()));
            Normformvektorsatz satz = Satz(b);
            Typtagjahr jahr = Typtagzuordnung.Zuordnen(Anbindung(satz), 0, We(0), NAME);
            double[] tage = Typtagzuordnung.Tagesmengen(1000.0, 10.0, jahr, NAME);

            double[] reihe = Typtagzuordnung.Stundenreihe(tage, jahr, satz, ART, NAME);
            Assert.NotNull(reihe);
            Assert.Equal(8760, reihe.Length);
            Assert.True(Relativ(reihe.Sum(), 1000.0) < 1e-12);
            // Der erste Tag verteilt sich nach dem Gang: die Stunden 6 und 18 tragen je die Haelfte.
            Assert.Equal(tage[0] * 0.5, reihe[6], 12);
            Assert.Equal(tage[0] * 0.5, reihe[18], 12);
            Assert.Equal(0.0, reihe[0]);
        }

        private static double[] Stundengang()
        {
            var a = new double[24];
            a[6] = 0.5;
            a[18] = 0.5;
            return a;
        }

        // =================================================================================
        //  Die benannten Ablehnungen
        // =================================================================================

        [Fact]
        public void Ohne_eingespielte_Typtage_ist_der_Weg_benannt_nicht_verfuegbar()
        {
            foreach (Typtaganbindung a in new[]
                     {
                         null,
                         new Typtaganbindung { Daten = null, Klimazone = ZONE, Gebaeudeart = ART, TagesmittelC = Temperaturen() },
                         new Typtaganbindung { Daten = new Normformvektorsatz(), Klimazone = ZONE, Gebaeudeart = ART, TagesmittelC = Temperaturen() }
                     })
            {
                ZapfprofilEingabeException ex = Assert.Throws<ZapfprofilEingabeException>(
                    () => Typtagzuordnung.Zuordnen(a, 0, We(0), NAME));
                Assert.Equal(ZapfEingabefehler.TyptageUngueltig, ex.Fehler);
                Assert.Equal("EINGABE_TYPTAGE_NICHT_VERFUEGBAR", ex.Kennung);
                Assert.Equal(NAME, ex.Zone);
            }
        }

        public static TheoryData<string> Luecken() => new TheoryData<string>
        {
            "EINGABE_TYPTAGE_ZONE_FEHLT",
            "EINGABE_TYPTAGE_GEBAEUDEART_FEHLT",
            "EINGABE_TYPTAGE_UNVOLLSTAENDIG",
            "EINGABE_TYPTAGE_TEMPERATUR",
            "EINGABE_TYPTAGE_TEMPERATUR_TAG",
            "EINGABE_TYPTAGE_BEDECKUNG_FEHLT",
            "EINGABE_TYPTAGE_BEDECKUNG_TAG",
            "EINGABE_TYPTAGE_KENNWERT_FEHLT"
        };

        [Theory]
        [MemberData(nameof(Luecken))]
        public void Jede_Luecke_wird_benannt_abgelehnt(string kennung)
        {
            Typtagpaketbauer b = kennung.Contains("BEDECKUNG", StringComparison.Ordinal)
                ? Typtagpaketbauer.MitBewoelkung(ZONE, ART) : Typtagpaketbauer.Erfunden();
            Typtaganbindung a = Anbindung(Satz(b), bedeckung: Bedeckung(3.0));

            switch (kennung)
            {
                case "EINGABE_TYPTAGE_ZONE_FEHLT":
                    a = a with { Klimazone = 99 };
                    break;
                case "EINGABE_TYPTAGE_GEBAEUDEART_FEHLT":
                    a = a with { Gebaeudeart = "fremdhaus" };
                    break;
                case "EINGABE_TYPTAGE_UNVOLLSTAENDIG":
                    // Eine Kategorie ohne Faktor: der Leser laesst das nicht durch, der Satz
                    // aus der Datenbank koennte es - deshalb pruefen wir den Satz selbst.
                    Normformvektorsatz luecke = new Normformvektorsatz();
                    luecke.KategorienSetzen(b.Kategorien.Select(k =>
                        new Typtagkategorie(k.Code, Typtagjahreszeit.Uebergang, Typtagart.Werktag, Typtagbewoelkung.Ohne)));
                    luecke.AnzahlSetzen(ZONE, ART, b.Kategorien[0].Code, 365);
                    luecke.KennwertSetzen(Typtagkennwert.WINTERGRENZE, Typtagpaketbauer.GRENZE_WINTER);
                    luecke.KennwertSetzen(Typtagkennwert.Heizgrenze(ART), Typtagpaketbauer.GRENZE_HEIZEN);
                    a = a with { Daten = luecke };
                    break;
                case "EINGABE_TYPTAGE_TEMPERATUR":
                    a = a with { TagesmittelC = new double[10] };
                    break;
                case "EINGABE_TYPTAGE_TEMPERATUR_TAG":
                    double[] t = Temperaturen();
                    t[5] = double.NaN;
                    a = a with { TagesmittelC = t };
                    break;
                case "EINGABE_TYPTAGE_BEDECKUNG_FEHLT":
                    a = a with { BedeckungAchtel = null };
                    break;
                case "EINGABE_TYPTAGE_BEDECKUNG_TAG":
                    double[] n = Bedeckung(3.0);
                    n[7] = double.PositiveInfinity;
                    a = a with { BedeckungAchtel = n };
                    break;
                case "EINGABE_TYPTAGE_KENNWERT_FEHLT":
                    Normformvektorsatz ohne = new Normformvektorsatz();
                    ohne.KategorienSetzen(a.Daten.Kategorien);
                    foreach (Typtagkategorie k in a.Daten.Kategorien)
                    {
                        ohne.AnzahlSetzen(ZONE, ART, k.Code, a.Daten.Anzahl(ZONE, ART, k.Code) ?? 0);
                        ohne.FaktorSetzen(ZONE, ART, k.Code, a.Daten.Faktor(ZONE, ART, k.Code) ?? 0.0);
                    }
                    a = a with { Daten = ohne };                       // kein einziger Kennwert
                    break;
            }

            ZapfprofilEingabeException ex = Assert.Throws<ZapfprofilEingabeException>(
                () => Typtagzuordnung.Zuordnen(a, 0, We(0), NAME));
            Assert.Equal(ZapfEingabefehler.TyptageUngueltig, ex.Fehler);
            Assert.Equal(kennung, ex.Kennung);
        }

        [Fact]
        public void Eine_Bezugsart_ausserhalb_des_Wohnens_wird_benannt_abgelehnt()
        {
            Nutzungsart wohnen = Art(1);
            Nutzungsart buero = Art(2, bezug: ZapfBezugsart.Beschaeftigte);
            var menge = new Mengenergebnis(1000.0, 8.0, 1.0, null);

            Assert.Equal(8.0, Typtagzuordnung.Einheiten(wohnen, menge, NAME));
            ZapfprofilEingabeException ex = Assert.Throws<ZapfprofilEingabeException>(
                () => Typtagzuordnung.Einheiten(buero, menge, NAME));
            Assert.Equal(ZapfEingabefehler.TyptageUngueltig, ex.Fehler);
            Assert.Equal("EINGABE_TYPTAGE_BEZUGSART", ex.Kennung);

            // Ohne Einheiten ist die Gleichung nicht bestimmt.
            ZapfprofilEingabeException leer = Assert.Throws<ZapfprofilEingabeException>(
                () => Typtagzuordnung.Einheiten(wohnen, new Mengenergebnis(1000.0, 0.0, 1.0, null), NAME));
            Assert.Equal("EINGABE_TYPTAGE_EINHEITEN", leer.Kennung);
        }

        // =================================================================================
        //  Die Weiche im Rechner (2.2, 5.3)
        // =================================================================================

        private static readonly Nutzungsart[] Katalog = { Art(1), Art(2, bezug: ZapfBezugsart.Beschaeftigte) };

        [Fact]
        public void Ohne_Typtage_rechnet_die_Weiche_den_Formvektor_wie_im_Bestand()
        {
            ZonenStand z = Zone("Zone A", 1, 10.0, 1);
            Zapfprofileingang ohne = Eingang(Projekt(), Parameter(), z);
            ZapfprofilErgebnis bestand = ZapfprofilRechner.Rechnen(ohne, Katalog);
            Assert.True(bestand.Vollstaendig);

            // Dieselbe Rechnung mit gesetzter, aber leerer Anbindung: benannt abgelehnt, nicht still.
            Zapfprofileingang leer = ohne with
            {
                Typtage = new Typtaganbindung { Daten = null, Klimazone = ZONE, Gebaeudeart = ART, TagesmittelC = Temperaturen() }
            };
            ZapfprofilErgebnis abgelehnt = ZapfprofilRechner.Rechnen(leer, Katalog);
            Assert.False(abgelehnt.Vollstaendig);
            Assert.Contains(abgelehnt.Ablehnungen,
                x => x.Grund == ZapfEingabefehler.TyptageUngueltig && x.Kennung == "EINGABE_TYPTAGE_NICHT_VERFUEGBAR");
            Assert.Equal(0.0, abgelehnt.Zapfung.JahressummeKwh);
        }

        [Fact]
        public void Mit_Typtagen_bleibt_die_Jahresenergie_erhalten_und_die_Reihe_aendert_sich()
        {
            ZonenStand z = Zone("Zone A", 1, 10.0, 1);
            Zapfprofileingang ohne = Eingang(Projekt(), Parameter(), z);
            ZapfprofilErgebnis bestand = ZapfprofilRechner.Rechnen(ohne, Katalog);

            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            Zapfprofileingang mit = ohne with { Typtage = Anbindung(satz) };
            ZapfprofilErgebnis typtage = ZapfprofilRechner.Rechnen(mit, Katalog);

            Assert.True(typtage.Vollstaendig);
            Assert.True(Relativ(typtage.Zapfung.JahressummeKwh, bestand.Zapfung.JahressummeKwh) < 1e-12);
            Assert.True(Relativ(typtage.Zapfung.MonatssummenKwh.Sum(), bestand.Zapfung.JahressummeKwh) < 1e-12);
            // Der Jahresgang ist ein anderer: die Monatssummen weichen ab.
            Assert.NotEqual(bestand.Zapfung.MonatssummenKwh, typtage.Zapfung.MonatssummenKwh);
            // Die Tagesform kommt weiter aus dem Tagesgangsatz: das Paket fuehrt keine Gaenge.
            Assert.Contains(typtage.Hinweise, h => h.Code == Typtagzuordnung.HINWEIS_OHNE_GANG);
        }

        [Fact]
        public void Mit_Typtagen_und_Tagesgaengen_kommt_auch_die_Tagesform_aus_dem_Paket()
        {
            ZonenStand z = Zone("Zone A", 1, 10.0, 1);
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden();
            foreach (var k in b.Kategorien) b.Gaenge.Add((ART, k.Code, 60, Stundengang()));
            Zapfprofileingang mit = Eingang(Projekt(), Parameter(), z) with { Typtage = Anbindung(Satz(b)) };

            ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(mit, Katalog);
            Assert.True(e.Vollstaendig);
            Assert.DoesNotContain(e.Hinweise, h => h.Code == Typtagzuordnung.HINWEIS_OHNE_GANG);

            // Der Gang des Pakets traegt nur die Stunden 6 und 18 - alle uebrigen sind 0.
            IReadOnlyList<double> reihe = e.Zapfung.StundenKwh;
            for (int h = 0; h < 24; h++)
                if (h != 6 && h != 18) Assert.Equal(0.0, reihe[h]);
            Assert.True(reihe[6] > 0.0 && reihe[18] > 0.0);
        }

        [Fact]
        public void Eine_Zone_ausserhalb_des_Wohnens_wird_auf_dem_Typtagweg_benannt_abgelehnt()
        {
            ZonenStand wohnen = Zone("Zone A", 1, 10.0, 1);
            ZonenStand buero = Zone("Zone B", 2, 20.0, 2);
            Normformvektorsatz satz = Satz(Typtagpaketbauer.Erfunden());
            Zapfprofileingang e = Eingang(Projekt(), Parameter(), wohnen, buero) with { Typtage = Anbindung(satz) };

            ZapfprofilErgebnis r = ZapfprofilRechner.Rechnen(e, Katalog);
            Assert.Contains(r.Ablehnungen, x => x.Zone == "Zone B" && x.Kennung == "EINGABE_TYPTAGE_BEZUGSART");
            // Die Wohnzone rechnet weiter.
            Assert.True(r.JeZone[0].Zapfung.JahressummeKwh > 0.0);
        }
    }
}
