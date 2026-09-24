using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.AuslegungTestbau;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Stochastik in den Fassaden</b> (Umsetzungskonzept Zapfprofilgenerator 2.3, 4.4, 4.5 b):
    /// der Rechenweg der Jahresreihe „stochastisch" im <see cref="ZapfprofilRechner"/> (Realisierung
    /// zum Seed mit Energieprobe, Konsistenzprobe aus den R Jahren, deterministisch = Vorgabe, kein
    /// stiller Rückfall) und das Perzentil der
    /// Auslegung auf „Stochastisch rechnen" je Topologiegruppe. Erfundene Kategorien und Parameter.
    /// </summary>
    public sealed class ZapfprofilStochastikTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Nutzungsart Wohnen = Art(1);
        private static readonly Nutzungsart Buero = Art(2, kalender: ZapfKalenderart.Arbeitstage);
        private static readonly Nutzungsart[] Katalog = { Wohnen, Buero };

        /// <summary>Die erfundenen Parameter der Stochastik — rund, keine Normzahl, kein Normquantil.</summary>
        private static Dictionary<string, double> Stochastikwerte() => new Dictionary<string, double>
        {
            [ZapfStochastikParameter.URLAUBSVERSATZ] = 20.0,
            [ZapfStochastikParameter.AUSLEGUNG_VIELFACHES] = 1.5,
            [ZapfStochastikParameter.KONSISTENZSCHWELLE] = 1.8,
            [ZapfStochastikParameter.QUANTIL + "95"] = 1.7,
            [ZapfStochastikParameter.QUANTIL + "99"] = 2.4,
        };

        private static Parametersatz Satz(params string[] weglassen) => Auslegungssatz(Stochastikwerte(), weglassen);

        private static readonly Zapfkategorie[] Kategorien =
            ZapfereignisgeneratorTests.Kategorien(1).Concat(ZapfereignisgeneratorTests.Kategorien(2)).ToArray();

        /// <summary>Wohnhaus (20 WE, 40 Personen) am Speicher mit Sommerferien, Büro (30 Personen) am Durchfluss.</summary>
        private static Zapfprofileingang Eingang(ProjektStand p, Parametersatz ps = null, bool mitKategorien = true, bool ferien = false)
        {
            ZonenStand wohnhaus = Zone("Wohnhaus", 1, 40.0, 1) with
            {
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 20, Personen = 2 } }
            };
            if (ferien) wohnhaus = wohnhaus with { Ferienbeginn = new int?[] { 190, null, null, null }, Ferienende = new int?[] { 210, null, null, null } };
            ZonenStand buero = Zone("Büro", 2, 30.0, 2) with { Topologie = ZapfTopologie.Durchfluss };
            return ZapfprofilTestbau.Eingang(p, ps ?? Satz(), wohnhaus, buero) with
            {
                Zapfkategorien = mitKategorien ? Kategorien : new Zapfkategorie[0]
            };
        }

        private static ProjektStand Stochastisch(int seed = 1, int realisierungen = 4)
            => Projekt() with { JahresreiheStochastisch = true, Seed = seed, Realisierungen = realisierungen };

        /// <summary>Projektgrößen mit dem konstruierten Tag (Id 5) — der Durchfluss hat dann seine Empfehlung.</summary>
        private static ProjektStand MitTag() => Projekt() with { BedarfstagQuelle = ZapfBedarfstagquelle.Konstruktor, IdBedarfstag = 5 };

        // =================================================================================
        // Rechenweg der Jahresreihe
        // =================================================================================

        [Fact]
        public void Deterministisch_bleibt_die_Vorgabe()
        {
            ZapfprofilErgebnis d = ZapfprofilRechner.Rechnen(Eingang(Projekt()), Katalog);
            Assert.False(d.Stochastisch);
            Assert.All(d.JeZone, z => Assert.Null(z.Konsistenz));
            Assert.DoesNotContain(d.Hinweise, h => h.Code == ZapfprofilRechner.HINWEIS_STOCHASTISCH);
            // Die Kategorien stören den deterministischen Weg nicht: dieselben Bits wie ohne.
            ZapfprofilErgebnis ohne = ZapfprofilRechner.Rechnen(Eingang(Projekt(), mitKategorien: false), Katalog);
            Assert.Equal(ohne.Zapfung.StundenKwh, d.Zapfung.StundenKwh);
        }

        [Fact]
        public void Stochastisch_erhaelt_die_Jahresmenge_je_Zone_und_die_Zirkulation()
        {
            ZapfprofilErgebnis d = ZapfprofilRechner.Rechnen(Eingang(Projekt()), Katalog);
            ZapfprofilErgebnis s = ZapfprofilRechner.Rechnen(Eingang(Stochastisch()), Katalog);
            Assert.True(s.Vollstaendig, string.Join("; ", s.Ablehnungen.Select(a => a.Klartext)));
            Assert.True(s.Stochastisch);
            for (int i = 0; i < 2; i++)
            {
                ZonenErgebnis zd = d.JeZone[i], zs = s.JeZone[i];
                // Energieprobe: je Zone dieselbe Jahresmenge wie der deterministische Pfad.
                Assert.InRange(Relativ(zs.Zapfung.JahressummeKwh, zd.Zapfung.JahressummeKwh), 0.0, 1e-12);
                Assert.NotNull(zs.Konsistenz);
                Assert.Equal(4, zs.Konsistenz.Realisierungen);
                Assert.Equal(zd.Zapfung.JahressummeKwh, zs.Konsistenz.DeterministischKwh, 9);
                Assert.True(zs.Konsistenz.Erfuellt, zs.Zone + ": " + zs.Konsistenz);
                // … aber eine andere Reihe: die gezogenen Jahre sind nicht der Formvektor.
                Assert.NotEqual(zd.Zapfung.StundenKwh, zs.Zapfung.StundenKwh);
                Assert.True(zs.Zapfung.GroessterStundenwertKw > zd.Zapfung.GroessterStundenwertKw);
            }
            // Die Zirkulation folgt dem Laufzeitfenster der deterministischen Reihe: dieselben Bits.
            Assert.Equal(d.Zirkulation.StundenKwh, s.Zirkulation.StundenKwh);
            Assert.Equal(d.Laufzeitfenster, s.Laufzeitfenster);
            Assert.InRange(Relativ(s.Kennzahlen.JahresbedarfZapfungKwh, d.Kennzahlen.JahresbedarfZapfungKwh), 0.0, 1e-12);
            Assert.Contains(s.Hinweise, h => h.Code == ZapfprofilRechner.HINWEIS_STOCHASTISCH
                                             && h.Text.Contains("das gezogene Jahr zum Seed 1") && h.Text.Contains("an 4 gezogenen Jahren"));
        }

        [Fact]
        public void Die_Bilanz_ist_das_Jahr_zum_Seed_und_haengt_nicht_von_R_ab()
        {
            // 2.3 Satz 3, 4.4 Ausgaben: die Realisierung zum Seed geht in die Bilanz, die R Jahre prüfen nur.
            ZapfprofilErgebnis eins = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(seed: 3, realisierungen: 1)), Katalog);
            ZapfprofilErgebnis vier = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(seed: 3, realisierungen: 4)), Katalog);
            Assert.True(vier.Vollstaendig);
            Assert.Equal(eins.Zapfung.StundenKwh.Select(BitConverter.DoubleToInt64Bits), vier.Zapfung.StundenKwh.Select(BitConverter.DoubleToInt64Bits));
            for (int i = 0; i < 2; i++)
            {
                Jahreskonsistenz k1 = eins.JeZone[i].Konsistenz, k4 = vier.JeZone[i].Konsistenz;
                Assert.Equal((1, 4), (k1.Realisierungen, k4.Realisierungen));
                // Dasselbe Jahr zum Seed, derselbe Faktor — die Konsistenzprobe aber aus 1 bzw. 4 Jahren.
                Assert.Equal(k1.JahrZumSeedKwh, k4.JahrZumSeedKwh);
                Assert.Equal(k1.Faktor, k4.Faktor);
                Assert.Equal(k1.JahrZumSeedKwh, k1.MittelKwh);
                Assert.NotEqual(k1.MittelKwh, k4.MittelKwh);
                Assert.InRange(Relativ(k4.Faktor * k4.JahrZumSeedKwh, k4.DeterministischKwh), 0.0, 1e-12);
            }
        }

        [Fact]
        public void Stochastisch_ist_reproduzierbar_je_Seed()
        {
            ZapfprofilErgebnis a = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(seed: 5, realisierungen: 2)), Katalog);
            ZapfprofilErgebnis b = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(seed: 5, realisierungen: 2)), Katalog);
            ZapfprofilErgebnis c = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(seed: 6, realisierungen: 2)), Katalog);
            Assert.Equal(a.Zapfung.StundenKwh.Select(BitConverter.DoubleToInt64Bits), b.Zapfung.StundenKwh.Select(BitConverter.DoubleToInt64Bits));
            Assert.NotEqual(a.Zapfung.StundenKwh, c.Zapfung.StundenKwh);
            Assert.InRange(Relativ(a.Zapfung.JahressummeKwh, c.Zapfung.JahressummeKwh), 0.0, 1e-12);
        }

        [Fact]
        public void Ohne_Zapfkategorien_traegt_die_Zone_benannt_null()
        {
            ZapfprofilErgebnis s = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(), mitKategorien: false), Katalog);
            Assert.Equal(2, s.Ablehnungen.Count);
            Assert.All(s.Ablehnungen, a =>
            {
                Assert.Equal(ZapfEingabefehler.StochastikUngueltig, a.Grund);
                Assert.Contains("keine Zapfkategorien", a.Klartext);
                // Die Ablehnung nennt die Nutzungsart der Zone — Bezeichner und Katalogversion getrennt,
                // sprachfrei —; die Hülle übersetzt die Kennung.
                Assert.Equal(Zapfkategoriensatz.KENNUNG_KATEGORIEN_FEHLEN, a.Kennung);
                Nutzungsart art = a.Zone == "Wohnhaus" ? Wohnen : Buero;
                Assert.Equal(new[] { art.Name, art.Katalogversion ?? "" }, a.Argumente);
                Assert.Contains("„" + art.Name + "“", a.Klartext);
            });
            Assert.Equal(0.0, s.Zapfung.JahressummeKwh);
            Assert.All(s.JeZone, z => Assert.True(z.Abgelehnt));
        }

        [Fact]
        public void Die_Entkopplung_der_Urlaube_braucht_ihren_Parameter()
        {
            // Mit Parameter: das Wohnhaus mit Ferien (Ferienfaktor 0,2, erfunden) rechnet entkoppelt mit derselben Jahresmenge.
            Nutzungsart[] katalog = { Art(1, ferienfaktor: 0.2), Buero };
            ZapfprofilErgebnis d = ZapfprofilRechner.Rechnen(Eingang(Projekt(), ferien: true), katalog);
            ZapfprofilErgebnis s = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(), ferien: true), katalog);
            Assert.True(s.Vollstaendig);
            Assert.InRange(Relativ(s.JeZone[0].Zapfung.JahressummeKwh, d.JeZone[0].Zapfung.JahressummeKwh), 0.0, 1e-12);
            double Ferien(ZonenErgebnis z) => z.Zapfung.StundenKwh.Skip(189 * 24).Take(21 * 24).Sum();
            Assert.True(Ferien(s.JeZone[0]) > Ferien(d.JeZone[0]), "Die versetzten Urlaube füllen das Ferienfenster auf.");

            // Ohne Parameter: benannte Ablehnung mit dem Schlüssel — nur die Zone mit Ferien.
            ZapfprofilErgebnis ohne = ZapfprofilRechner.Rechnen(
                Eingang(Stochastisch(), Satz(ZapfStochastikParameter.URLAUBSVERSATZ), ferien: true), katalog);
            ZapfAblehnung a = Assert.Single(ohne.Ablehnungen);
            Assert.Equal("Wohnhaus", a.Zone);
            Assert.Equal(ZapfEingabefehler.ParameterFehlt, a.Grund);
            Assert.Contains(ZapfStochastikParameter.URLAUBSVERSATZ, a.Klartext);
            Assert.False(ohne.JeZone[1].Abgelehnt);
        }

        [Fact]
        public void Null_Realisierungen_werden_benannt_abgelehnt()
        {
            ZapfprofilErgebnis s = ZapfprofilRechner.Rechnen(Eingang(Stochastisch(realisierungen: 0)), Katalog);
            Assert.Equal(2, s.Ablehnungen.Count);
            Assert.All(s.Ablehnungen, a => Assert.Equal(ZapfEingabefehler.StochastikUngueltig, a.Grund));
        }

        /// <summary>
        /// Die Schranke der Jahresreihe (4.4): <c>R · Σ n_E · 365</c> über der Grenze der
        /// Einheitentage (<see cref="Zapfensemble.HOECHSTENS_EINHEITSTAGE"/>) lehnt das Projekt benannt
        /// ab — mit Kennung und Werten, bevor ein Jahr gezogen ist (der Fall braucht keine Laufzeit).
        /// </summary>
        [Fact]
        public void Zu_viele_Einheitentage_lehnen_die_Jahresreihe_benannt_ab()
        {
            // Wohnhaus 20 WE + Büro 30 Personen = 50 Einheiten; 548 Jahre zögen 10 001 000 Einheitentage.
            var ex = Assert.Throws<ZapfprofilEingabeException>(
                () => ZapfprofilRechner.Rechnen(Eingang(Stochastisch(realisierungen: 548)), Katalog));
            Assert.Equal(ZapfEingabefehler.StochastikUngueltig, ex.Fehler);
            Assert.Equal("", ex.Zone);
            Assert.Equal(ZapfprofilRechner.KENNUNG_EINHEITSTAGE, ex.Kennung);
            Assert.Equal(new[] { "10001000", "10000000" }, ex.Argumente);
            Assert.Contains("höchstens 10000000 sind zulässig", ex.Message);

            // Deterministisch zieht nichts — die Schranke gilt nicht.
            Assert.True(ZapfprofilRechner.Rechnen(Eingang(Projekt() with { Realisierungen = 548 }), Katalog).Vollstaendig);
        }

        /// <summary>
        /// Der nebenläufige Lauf der Oberfläche (5.1): Eine gesetzte Abbruchmarke beendet die Ziehung
        /// der Jahresreihe mit <see cref="OperationCanceledException"/> — ohne halbes Ergebnis. 547
        /// Jahre liegen gerade unter der Schranke der Einheitentage (9 982 750): der Lauf erreicht die
        /// Ziehung und bricht dort ab. Der deterministische Weg zieht nichts und rechnet trotz Marke.
        /// </summary>
        [Fact]
        public void Eine_Abbruchmarke_beendet_die_Ziehung_der_Jahresreihe()
        {
            using var marke = new System.Threading.CancellationTokenSource();
            marke.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(
                () => ZapfprofilRechner.Rechnen(Eingang(Stochastisch(realisierungen: 547)), Katalog, marke.Token));
            Assert.True(ZapfprofilRechner.Rechnen(Eingang(Projekt()), Katalog, marke.Token).Vollstaendig);
        }

        // =================================================================================
        // Perzentil der Auslegung
        // =================================================================================

        private static readonly BedarfstagKatalogzeile Konstruiert = new BedarfstagKatalogzeile(5, "Konstruierter Tag (fiktiv)",
            "TEST-Z2", ZapfBedarfstagquelle.Konstruktor, null, Fiktiv,
            new[] { new Zapfereignis(420, 30, 6.0), new Zapfereignis(720, 5, 2.0), new Zapfereignis(1080, 60, 8.0) });

        private static Auslegungseingang Zusatz(bool stochastisch = true) => new Auslegungseingang
        {
            Bedarfstage = new[] { Konstruiert },
            Nenninhalte = Nenninhaltsliste.Aus(new[] { 100.0, 200.0, 300.0, 500.0, 800.0, 1000.0 }),
            Erzeugerart = ZapfErzeugerart.Waermepumpe,
            Uebertragerwerkstoff = ZapfUebertragerwerkstoff.Edelstahl,
            Stochastisch = stochastisch
        };

        private static Auslegungsgruppe Gruppe(Auslegungsergebnis r, ZapfTopologie t) => r.Gruppen.Single(g => g.Topologie == t);

        /// <summary>
        /// Der nebenläufige Lauf „Stochastisch rechnen" der Überlagerung (5.1): Die Abbruchmarke des
        /// Auslegungseingangs beendet die Ziehung des Ensembles — die Ausnahme reist hinaus, sie
        /// wird nicht zu „nicht rechenbar"; ohne „Stochastisch rechnen" stört die Marke nicht.
        /// </summary>
        [Fact]
        public void Eine_Abbruchmarke_beendet_das_Auslegungsensemble()
        {
            using var marke = new System.Threading.CancellationTokenSource();
            marke.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(
                () => ZapfprofilAuslegung.Rechnen(Eingang(Projekt()), Katalog, Zusatz() with { Abbruch = marke.Token }));
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(Projekt()), Katalog,
                                                               Zusatz(stochastisch: false) with { Abbruch = marke.Token });
            Assert.All(r.Gruppen, g => Assert.Null(g.Perzentil));
        }

        [Fact]
        public void Ohne_Stochastisch_rechnen_bleibt_das_Perzentil_offen()
        {
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(Projekt()), Katalog, Zusatz(stochastisch: false));
            Assert.All(r.Gruppen, g =>
            {
                Assert.Equal(Auslegungsstatus.NichtGerechnet, g.Dreiergruppe[1].Status);
                Assert.Equal(Dreiergruppe.PERZENTIL_OFFEN, g.Dreiergruppe[1].Text);
                Assert.Null(g.Perzentil);
            });
        }

        [Fact]
        public void Das_Perzentil_des_Durchflusses_ist_die_Minutenspitze_des_Ensembles()
        {
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(MitTag()), Katalog, Zusatz());
            Auslegungsgruppe g = Gruppe(r, ZapfTopologie.Durchfluss);
            Auslegungswert w = g.Dreiergruppe[1];
            Assert.Equal(Auslegungsstatus.Gerechnet, w.Status);
            Assert.False(w.Empfohlen);
            Perzentilergebnis p = g.Perzentil;
            Assert.NotNull(p);
            Assert.Equal(99, p.Perzentil);
            Assert.Equal(150, p.Realisierungen);   // 1,5 · 100 (Vielfaches des Testkatalogs)
            Assert.True(p.Belastbar);
            Assert.Equal(p.MinutenspitzeKw.P99, w.LeistungKw.Value);
            Assert.Null(w.VolumenL);
            Assert.InRange(p.GleichzeitigkeitLeistung.Value, 0.0, 1.0);
            Assert.Equal(30, Assert.Single(p.Zonen).Einheiten);
            Assert.NotNull(p.WurzelNSchaetzungKw);
            Assert.Contains(g.Hinweise, h => h.Code == "WURZEL_N_VERGLEICH");
            Assert.Contains("Perzentil P99: Minutenspitze", w.Text);
            // Die Empfehlung bleibt die Minutenspitze des Bedarfstags — das Perzentil steht daneben.
            Assert.Equal(ZapfAuslegungsverfahren.Minutenspitze, g.Empfehlung.Verfahren);
            Assert.True(g.Dreiergruppe[0].Empfohlen);
            // Derselbe Seed: dasselbe Perzentil.
            Auslegungsergebnis r2 = ZapfprofilAuslegung.Rechnen(Eingang(MitTag()), Katalog, Zusatz());
            Assert.Equal(w, Gruppe(r2, ZapfTopologie.Durchfluss).Dreiergruppe[1]);
        }

        [Fact]
        public void Das_Perzentil_des_Speichers_gilt_beim_Phi_N_des_Summenlinienpunkts()
        {
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(Projekt()), Katalog, Zusatz());
            Auslegungsgruppe g = Gruppe(r, ZapfTopologie.Speicher);
            Assert.NotNull(g.Summenlinie);
            Auslegungswert w = g.Dreiergruppe[1];
            Assert.Equal(Auslegungsstatus.Gerechnet, w.Status);
            Assert.Equal(g.Summenlinie.Punkt.LeistungKw, w.LeistungKw.Value);
            Assert.Equal(g.Perzentil.VolumenL.P99, w.VolumenL.Value);
            Assert.True(w.VolumenL.Value > 0);
            Assert.Equal(0, g.Perzentil.OhneNachweis);
            // Gleichzeitigkeit als Ergebnis, größengleich zur Topologie: GLF_V über das Volumen, GLF_P über die Leistung.
            Assert.InRange(g.Perzentil.GleichzeitigkeitVolumen.Value, 0.0, 1.0);
            Assert.True(g.Perzentil.GleichzeitigkeitVolumen.Value > g.Perzentil.GleichzeitigkeitLeistung.Value);
            // Die Reihenfolgeprüfung vergleicht Liter mit Litern: ein Hinweis nur, wenn V_Perzentil über dem Punkt liegt.
            bool ueber = w.VolumenL.Value > g.Summenlinie.Punkt.VolumenL;
            Assert.Equal(ueber, g.Hinweise.Any(h => h.Code == Dreiergruppe.HINWEIS_REIHENFOLGE && h.Text.StartsWith("Das Perzentil")));
            Assert.Contains("Perzentil P99: ", w.Text);
            Assert.Contains(" l bei ", w.Text);
        }

        [Fact]
        public void Perzentil_bei_kleinem_R_nicht_belastbar_und_P95_waehlbar()
        {
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(Projekt() with { RealisierungenAuslegung = 30 }), Katalog, Zusatz());
            Auslegungsgruppe g = Gruppe(r, ZapfTopologie.Durchfluss);
            Assert.False(g.Perzentil.Belastbar);
            Assert.EndsWith("nicht belastbar", g.Dreiergruppe[1].Text);
            Assert.Contains(g.Hinweise, h => h.Code == "PERZENTIL_NICHT_BELASTBAR" && h.Warnung);

            Auslegungsergebnis r95 = ZapfprofilAuslegung.Rechnen(
                Eingang(Projekt() with { RealisierungenAuslegung = 30, Perzentil = 95 }), Katalog, Zusatz());
            Auslegungsgruppe g95 = Gruppe(r95, ZapfTopologie.Durchfluss);
            Assert.True(g95.Perzentil.Belastbar);
            Assert.Equal(g95.Perzentil.MinutenspitzeKw.P95, g95.Dreiergruppe[1].LeistungKw.Value);
            Assert.DoesNotContain(g95.Hinweise, h => h.Code == "PERZENTIL_NICHT_BELASTBAR");
        }

        [Fact]
        public void Ohne_Kategorien_oder_Parameter_bleibt_das_Perzentil_benannt_nicht_rechenbar()
        {
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(MitTag(), mitKategorien: false), Katalog, Zusatz());
            Assert.All(r.Gruppen, g =>
            {
                Assert.Equal(Auslegungsstatus.NichtRechenbar, g.Dreiergruppe[1].Status);
                Assert.Contains("keine Zapfkategorien", g.Dreiergruppe[1].Text);
                Assert.Contains(g.Hinweise, h => h.Code == "STOCHASTIK_NICHT_RECHENBAR");
                Assert.Null(g.Perzentil);
                // Die Empfehlung bleibt unberührt.
                Assert.True(g.Empfehlung.Rechenbar);
            });

            // Ohne Vielfaches und ohne Projektwert: nicht rechenbar mit dem Schlüssel; ohne Quantil nur ein Hinweis.
            Auslegungsergebnis ohneVielfaches = ZapfprofilAuslegung.Rechnen(
                Eingang(Projekt(), Satz(ZapfStochastikParameter.AUSLEGUNG_VIELFACHES)), Katalog, Zusatz());
            Assert.All(ohneVielfaches.Gruppen, g => Assert.Contains(ZapfStochastikParameter.AUSLEGUNG_VIELFACHES, g.Dreiergruppe[1].Text));
            Auslegungsergebnis ohneQuantil = ZapfprofilAuslegung.Rechnen(
                Eingang(Projekt(), Satz(ZapfStochastikParameter.QUANTIL + "99")), Katalog, Zusatz());
            Auslegungsgruppe d = Gruppe(ohneQuantil, ZapfTopologie.Durchfluss);
            Assert.Equal(Auslegungsstatus.Gerechnet, d.Dreiergruppe[1].Status);
            Assert.Null(d.Perzentil.WurzelNSchaetzungKw);
            Assert.Contains(d.Hinweise, h => h.Code == ZapfHinweis.PARAMETER_FEHLT && h.Text.Contains(ZapfStochastikParameter.QUANTIL + "99"));
        }

        [Fact]
        public void Die_Wohnungsstation_bekommt_ihre_Spitze_je_Einheit()
        {
            ZonenStand station = Zone("Wohnungen", 1, 40.0, 3) with
            {
                Topologie = ZapfTopologie.Wohnungsstation,
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 20, Personen = 2 } }
            };
            Zapfprofileingang e = ZapfprofilTestbau.Eingang(Projekt(), Satz(), station) with { Zapfkategorien = Kategorien };
            Auslegungsgruppe offen = Assert.Single(ZapfprofilAuslegung.Rechnen(e, Katalog, Zusatz(stochastisch: false)).Gruppen);
            Assert.Contains(offen.Hinweise, h => h.Code == "WOHNUNGSSTATION_JE_EINHEIT" && h.Text.Contains("Stochastisch rechnen"));

            Auslegungsgruppe g = Assert.Single(ZapfprofilAuslegung.Rechnen(e, Katalog, Zusatz()).Gruppen);
            Auslegungshinweis h = Assert.Single(g.Hinweise, x => x.Code == "WOHNUNGSSTATION_JE_EINHEIT");
            Assert.StartsWith("Die Wohnungsstation je Einheit: P99", h.Text);
            double jeEinheit = Assert.Single(g.Perzentil.Zonen).SpitzeJeEinheitKw.P99;
            Assert.True(jeEinheit < g.Perzentil.MinutenspitzeKw.P99);
            Assert.True(jeEinheit * 20 > g.Perzentil.MinutenspitzeKw.P99, "Die Summe der Einzelspitzen übertrifft die gemeinsame Spitze.");
            // Die Auslegungsgröße je Einheit steht als Wert samt Zone im Perzentil — nicht nur im Satz.
            Assert.Equal(jeEinheit, g.Perzentil.SpitzeJeEinheitKw);
            Assert.Equal("Wohnungen", g.Perzentil.SpitzeJeEinheitZone);
            // Bei jeder anderen Topologie gibt es sie nicht.
            Auslegungsgruppe d = Gruppe(ZapfprofilAuslegung.Rechnen(Eingang(MitTag()), Katalog, Zusatz()), ZapfTopologie.Durchfluss);
            Assert.Null(d.Perzentil.SpitzeJeEinheitKw);
            Assert.Equal("", d.Perzentil.SpitzeJeEinheitZone);
        }

        [Fact]
        public void Der_Konsistenzhinweis_vergleicht_die_stochastische_Spitze_mit_Phi_N()
        {
            // Eine winzige Schwelle löst ihn sicher aus, eine riesige nie.
            Dictionary<string, double> klein = Stochastikwerte();
            klein[ZapfStochastikParameter.KONSISTENZSCHWELLE] = 0.01;
            Auslegungsgruppe g = Gruppe(ZapfprofilAuslegung.Rechnen(Eingang(Projekt(), Auslegungssatz(klein)), Katalog, Zusatz()),
                                        ZapfTopologie.Speicher);
            Assert.Contains(g.Hinweise, h => h.Code == "KONSISTENZ_STOCHASTISCHE_SPITZE" && h.Warnung);
            Dictionary<string, double> gross = Stochastikwerte();
            gross[ZapfStochastikParameter.KONSISTENZSCHWELLE] = 1000.0;
            g = Gruppe(ZapfprofilAuslegung.Rechnen(Eingang(Projekt(), Auslegungssatz(gross)), Katalog, Zusatz()), ZapfTopologie.Speicher);
            Assert.DoesNotContain(g.Hinweise, h => h.Code == "KONSISTENZ_STOCHASTISCHE_SPITZE");
            // Ohne Schwelle: nur „Parameter fehlt", keine Prüfung.
            g = Gruppe(ZapfprofilAuslegung.Rechnen(Eingang(Projekt(), Satz(ZapfStochastikParameter.KONSISTENZSCHWELLE)), Katalog, Zusatz()),
                       ZapfTopologie.Speicher);
            Assert.DoesNotContain(g.Hinweise, h => h.Code == "KONSISTENZ_STOCHASTISCHE_SPITZE");
            Assert.Contains(g.Hinweise, h => h.Code == ZapfHinweis.PARAMETER_FEHLT && h.Text.Contains(ZapfStochastikParameter.KONSISTENZSCHWELLE));
        }
    }
}
