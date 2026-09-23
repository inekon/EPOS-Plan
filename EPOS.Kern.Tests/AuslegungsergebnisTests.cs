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
    /// <b>Das Auslegungsergebnis</b> (Umsetzungskonzept Zapfprofilgenerator 2.4, 4.5, 4.7): je
    /// Topologiegruppe die Dreiergruppe mit genau einer Empfehlung, Perzentil bis Z3 „noch nicht
    /// gerechnet", größengleiche Reihenfolgeprüfung, ein Mengengerüst für alle Verfahren, keine
    /// stille Wahl des Stundenprofils. Fiktives Projekt mit erfundenen Werten.
    /// </summary>
    public sealed class AuslegungsergebnisTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Nutzungsart Wohnen = Art(1);
        private static readonly Nutzungsart Buero = Art(2, kalender: ZapfKalenderart.Arbeitstage);

        /// <summary>Wohnhaus mit 20 WE × 2 Personen am Speicher, Büro mit 30 Personen am Durchfluss.</summary>
        private static Zapfprofileingang Eingang(ProjektStand projekt = null, params ZonenStand[] weitere)
        {
            ZonenStand wohnhaus = Zone("Wohnhaus", 1, 40.0, 1) with
            {
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 20, Personen = 2 } }
            };
            ZonenStand buero = Zone("Büro", 2, 30.0, 2) with { Topologie = ZapfTopologie.Durchfluss };
            var zonen = new List<ZonenStand> { wohnhaus, buero };
            zonen.AddRange(weitere);
            return ZapfprofilTestbau.Eingang(projekt ?? Projekt(), Auslegungssatz(), zonen.ToArray());
        }

        private static readonly BedarfstagKatalogzeile Konstruiert = new BedarfstagKatalogzeile(5, "Konstruierter Tag (fiktiv)",
            "TEST-Z2", ZapfBedarfstagquelle.Konstruktor, null, Fiktiv,
            new[] { new Zapfereignis(420, 30, 6.0), new Zapfereignis(720, 5, 2.0), new Zapfereignis(1080, 60, 8.0) });

        private static Auslegungseingang Zusatz() => new Auslegungseingang
        {
            Bedarfstage = new[] { Konstruiert },
            Nenninhalte = Nenninhaltsliste.Aus(new[] { 100.0, 200.0, 300.0, 500.0, 800.0, 1000.0 })
        };

        [Fact]
        public void Es_gibt_genau_eine_Empfehlung()
        {
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(), new[] { Wohnen, Buero }, Zusatz());
            Assert.Empty(r.Ablehnungen);
            Assert.Equal(new[] { ZapfTopologie.Speicher, ZapfTopologie.Durchfluss }, r.Gruppen.Select(g => g.Topologie).ToArray());

            // Speicher, Wohnen: Vorgabe DIN-4708-Profil; der Summenlinienpunkt ist die eine Empfehlung.
            Auslegungsgruppe speicher = r.Gruppen[0];
            Assert.Equal(ZapfBedarfstagquelle.Din4708Profil, speicher.Bedarfstag.Quelle);
            Assert.True(speicher.Empfehlung.Rechenbar);
            Assert.Equal(ZapfAuslegungsverfahren.Summenlinie, speicher.Empfehlung.Verfahren);
            Assert.Single(speicher.Dreiergruppe, w => w.Empfohlen);
            Assert.True(speicher.Dreiergruppe[0].Empfohlen);
            Assert.Equal(speicher.Summenlinie.Punkt.VolumenL, speicher.Empfehlung.VolumenL);
            Assert.True(speicher.Empfehlung.NenninhaltL >= speicher.Empfehlung.VolumenL);
            Assert.Contains(Summenlinie.VERMERK_ENTWURF, speicher.Empfehlung.Vermerk);
            Assert.Equal(3, speicher.Dreiergruppe.Count);
            Assert.NotNull(speicher.Speicherauslegung);
            Assert.NotNull(speicher.Grossanlage);

            // Durchfluss, Nichtwohnen ohne Tag: Konstruktor öffnen, keine Empfehlung, kein Stundenprofil.
            Auslegungsgruppe durchfluss = r.Gruppen[1];
            Assert.Null(durchfluss.Bedarfstag);
            Assert.True(durchfluss.Bedarfstagwahl.KonstruktorOeffnen);
            Assert.False(durchfluss.Empfehlung.Rechenbar);
            Assert.DoesNotContain(durchfluss.Dreiergruppe, w => w.Empfohlen);
            Assert.Contains(durchfluss.Hinweise, h => h.Code == "KONSTRUKTOR_OEFFNEN");
            Assert.Equal(Auslegungsstatus.AusserhalbGueltigkeit, durchfluss.Dreiergruppe[2].Status);
            Assert.Null(durchfluss.Speicherauslegung);

            // Mit gewähltem Konstruktortag: die Minutenspitze ist die eine Empfehlung der Gruppe.
            ProjektStand mitTag = Projekt() with { BedarfstagQuelle = ZapfBedarfstagquelle.Konstruktor, IdBedarfstag = 5 };
            r = ZapfprofilAuslegung.Rechnen(Eingang(mitTag), new[] { Wohnen, Buero }, Zusatz());
            durchfluss = r.Gruppen[1];
            Assert.True(durchfluss.Empfehlung.Rechenbar);
            Assert.Equal(ZapfAuslegungsverfahren.Minutenspitze, durchfluss.Empfehlung.Verfahren);
            Assert.Single(durchfluss.Dreiergruppe, w => w.Empfohlen);
            // Ohne Bezugsmenge des Tages keine Skalierung: die größte Minute trägt 2 kWh / 5 min = 24 kW.
            Assert.True(Relativ(durchfluss.Empfehlung.LeistungKw.Value, 24.0) < 1e-12);
            foreach (Auslegungsgruppe g in r.Gruppen)
                Assert.Equal(g.Empfehlung.Rechenbar ? 1 : 0, g.Dreiergruppe.Count(w => w.Empfohlen));
        }

        [Fact]
        public void Perzentil_ist_bis_Z3_nicht_gerechnet()
        {
            Auslegungswert p = Dreiergruppe.PerzentilOffen();
            Assert.Equal(Auslegungsstatus.NichtGerechnet, p.Status);
            Assert.Equal(Dreiergruppe.PERZENTIL_Z3, p.Text);
            Assert.False(p.Empfohlen);
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(), new[] { Wohnen, Buero }, Zusatz());
            Assert.All(r.Gruppen, g => Assert.Equal(Auslegungsstatus.NichtGerechnet, g.Dreiergruppe[1].Status));
        }

        [Fact]
        public void Die_Reihenfolge_wird_groessengleich_geprueft()
        {
            Auslegungswert Sl(double v) => new Auslegungswert(ZapfAuslegungsverfahren.Summenlinie, Auslegungsstatus.Gerechnet, v, 10.0, true, "");
            Auslegungswert Norm(double v) => new Auslegungswert(ZapfAuslegungsverfahren.Normvergleich, Auslegungsstatus.Gerechnet, v, null, false, "");
            Auslegungswert offen = Dreiergruppe.PerzentilOffen();

            // Speicher, Liter: V_Summenlinie < V_DIN erwartet.
            Assert.Empty(Dreiergruppe.Reihenfolge(ZapfTopologie.Speicher, Sl(300), offen, Norm(400)));
            Assert.Single(Dreiergruppe.Reihenfolge(ZapfTopologie.Speicher, Sl(500), offen, Norm(400)));
            // Mit Perzentil (Z3): V_Perzentil ≤ V_Summenlinie.
            var perz = new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.Gerechnet, 600, null, false, "");
            Assert.Single(Dreiergruppe.Reihenfolge(ZapfTopologie.Speicher, Sl(500), perz, Norm(700)));

            // Durchfluss, kW: P_Perzentil ≤ P_Bedarfstag; der Literwert der Norm wird nie gegen kW gehalten.
            var spitze = new Auslegungswert(ZapfAuslegungsverfahren.Minutenspitze, Auslegungsstatus.Gerechnet, null, 30.0, true, "");
            Assert.Empty(Dreiergruppe.Reihenfolge(ZapfTopologie.Durchfluss, spitze, offen, Norm(10)));
            var perzKw = new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.Gerechnet, null, 40.0, false, "");
            Assert.Single(Dreiergruppe.Reihenfolge(ZapfTopologie.Durchfluss, spitze, perzKw, Norm(10)));
            perzKw = perzKw with { LeistungKw = 20.0 };
            Assert.Empty(Dreiergruppe.Reihenfolge(ZapfTopologie.Durchfluss, spitze, perzKw, Norm(10)));

            // Die Dreiergruppe empfiehlt nur einen gerechneten Hauptwert.
            IReadOnlyList<Auslegungswert> d = Dreiergruppe.Bilden(Sl(300) with { Empfohlen = false }, perz with { Empfohlen = true }, Norm(400) with { Empfohlen = true });
            Assert.Equal(new[] { true, false, false }, d.Select(w => w.Empfohlen).ToArray());
            d = Dreiergruppe.Bilden(Sl(300) with { Status = Auslegungsstatus.NichtRechenbar }, perz, Norm(400));
            Assert.DoesNotContain(d, w => w.Empfohlen);
        }

        [Fact]
        public void Ein_Mengengeruest_fuer_alle_Verfahren()
        {
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(), new[] { Wohnen, Buero }, Zusatz());
            Auslegungsgruppe g = r.Gruppen[0];
            // Die Wohnungstabelle (20 × 2) bildet die Bezugsmenge des Mengengerüsts, N und die Personen der Faustwerte.
            Assert.Equal(40.0, r.Herkunft.Last(x => x.Zone == "Wohnhaus" && x.Feld == ZapfFeld.BEZUGSMENGE).Wert);
            Assert.Equal(40.0, g.Normvergleich.Personen);
            Assert.True(Relativ(g.Normvergleich.KennzahlN.Value, 10.0) < 1e-12);
            // Klassisch: 40 P · 40 l · 45 K / 50 K · 1,1 (Speicher 62 °C, Kaltwasser 12 °C aus dem Parametersatz).
            Assert.True(Relativ(g.Speicherauslegung.VolumenKlassischL.Value, 40.0 * 40.0 * 45.0 / 50.0 * 1.1) < 1e-12);
            // Q_a = 40 P · 2 kWh · 365 · f_θ; mit f_KW,A = (50 − 12)/(50 − 11) wird die Auslegung 29 200 kWh/a;
            // jede Woche des flachen Jahrs (52 Wochen und ein Montag, Σ 7 · w_T = 365,05) trägt 7/365,05 davon.
            Assert.True(Relativ(g.Woche.WochensummeKwh, 29200.0 * 7.0 / 365.05) < 1e-9);
        }

        [Fact]
        public void Das_Stundenprofil_gilt_nur_ausdruecklich_und_traegt_den_Vermerk()
        {
            ProjektStand p = Projekt() with { BedarfstagQuelle = ZapfBedarfstagquelle.Stundenprofil };
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(p), new[] { Wohnen, Buero }, Zusatz());
            foreach (Auslegungsgruppe g in r.Gruppen)
            {
                Assert.True(g.Bedarfstag.SpitzenUnterschaetzt);
                Assert.Contains(g.Hinweise, h => h.Code == Bedarfstag.VERMERK_SPITZEN_UNTERSCHAETZT);
                Assert.True(Relativ(g.Bedarfstag.TagessummeKwh, g.Woche.GroessterTagKwh) < 1e-12);
            }
            Assert.Contains("Spitzen unterschätzt", r.Gruppen[0].Empfehlung.Vermerk);
        }

        [Fact]
        public void Eine_abgelehnte_Zone_wird_genannt_und_die_uebrigen_rechnen()
        {
            ZonenStand fremd = Zone("Unbekannt", 99, 5.0, 3);
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(null, fremd), new[] { Wohnen, Buero }, Zusatz());
            Assert.Single(r.Ablehnungen);
            Assert.Equal("Unbekannt", r.Ablehnungen[0].Zone);
            Assert.Equal(2, r.Gruppen.Count);
            Assert.DoesNotContain(r.Gruppen, g => g.Zonen.Contains("Unbekannt"));
            // Ohne Pflichtparameter der Auslegung: benannt, kein Rückfall.
            Zapfprofileingang e = Eingang() with { Parameter = Auslegungssatz(null, ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG) };
            Assert.Throws<ParametersatzException>(() => ZapfprofilAuslegung.Rechnen(e, new[] { Wohnen, Buero }, Zusatz()));
        }
    }
}
