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
            Nenninhalte = Nenninhaltsliste.Aus(new[] { 100.0, 200.0, 300.0, 500.0, 800.0, 1000.0 }),
            Erzeugerart = ZapfErzeugerart.Waermepumpe,
            Uebertragerwerkstoff = ZapfUebertragerwerkstoff.Edelstahl
        };

        /// <summary>
        /// Ein Befund der Bilanz behält in der Auslegung seine Stufe (Warnlogik Z4): Der überschriebene
        /// Bedarf außerhalb der Bandbreite des Niveaus ist dort wie in der Bilanz eine Warnung.
        /// </summary>
        [Fact]
        public void Eine_Warnung_der_Bilanz_bleibt_in_der_Auslegung_eine_Warnung()
        {
            Nutzungsart eng = Art(1, bandbreite: new Bedarfsbandbreite(new double?[3], new double?[] { 3.0, 3.0, 3.0 }));
            ZonenStand wohnhaus = Zone("Wohnhaus", 1, 40.0, 1) with
            {
                BedarfSpezKwhJeEinheitTag = 5.0,
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 20, Personen = 2 } }
            };
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(ZapfprofilTestbau.Eingang(Projekt(), Auslegungssatz(), wohnhaus),
                                                               new[] { eng }, Zusatz());
            Auslegungshinweis h = Assert.Single(r.Hinweise, x => x.Code == Mengengeruest.HINWEIS_BANDBREITE);
            Assert.True(h.Warnung);
            Assert.Equal("HINWEIS_BEDARF_AUSSERHALB_BANDBREITE", h.Satz.Kennung);
        }

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
            Assert.Contains(speicher.Empfehlung.Vermerke, v => v.Kennung == Summenlinie.VERMERK_ENTWURF);
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
            Assert.Equal(Dreiergruppe.PERZENTIL_OFFEN, p.Satz.Kennung);
            Assert.False(p.Empfohlen);
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(Eingang(), new[] { Wohnen, Buero }, Zusatz());
            Assert.All(r.Gruppen, g => Assert.Equal(Auslegungsstatus.NichtGerechnet, g.Dreiergruppe[1].Status));
        }

        [Fact]
        public void Die_Reihenfolge_wird_groessengleich_geprueft()
        {
            Auslegungswert Sl(double v) => new Auslegungswert(ZapfAuslegungsverfahren.Summenlinie, Auslegungsstatus.Gerechnet, v, 10.0, true, null);
            Auslegungswert Norm(double v) => new Auslegungswert(ZapfAuslegungsverfahren.Normvergleich, Auslegungsstatus.Gerechnet, v, null, false, null);
            Auslegungswert offen = Dreiergruppe.PerzentilOffen();

            // Speicher, Liter: V_Summenlinie < V_DIN erwartet.
            Assert.Empty(Dreiergruppe.Reihenfolge(ZapfTopologie.Speicher, Sl(300), offen, Norm(400)));
            Assert.Single(Dreiergruppe.Reihenfolge(ZapfTopologie.Speicher, Sl(500), offen, Norm(400)));
            // Mit Perzentil (Z3): V_Perzentil ≤ V_Summenlinie.
            var perz = new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.Gerechnet, 600, null, false, null);
            Assert.Single(Dreiergruppe.Reihenfolge(ZapfTopologie.Speicher, Sl(500), perz, Norm(700)));

            // Durchfluss, kW: P_Perzentil ≤ P_Bedarfstag; der Literwert der Norm wird nie gegen kW gehalten.
            var spitze = new Auslegungswert(ZapfAuslegungsverfahren.Minutenspitze, Auslegungsstatus.Gerechnet, null, 30.0, true, null);
            Assert.Empty(Dreiergruppe.Reihenfolge(ZapfTopologie.Durchfluss, spitze, offen, Norm(10)));
            var perzKw = new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.Gerechnet, null, 40.0, false, null);
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
            // Klassisch: 40 P · 40 l · 45 K / 50 K · 1,1 — das empfohlene Volumen macht die 20 WE zur
            // Großanlage (erfundene Schwelle 450 l), die Gruppe rechnet mit W 551 (62 °C), Kaltwasser 12 °C.
            Assert.Equal(new Speichertemperaturwahl(62.0, Speichertemperaturquelle.Grossanlage, false), g.Speichertemperatur);
            Assert.True(g.Grossanlage.Gross);
            Assert.True(Relativ(g.Speicherauslegung.VolumenKlassischL.Value, 40.0 * 40.0 * 45.0 / 50.0 * 1.1) < 1e-12);
            // Q_a = 40 P · 2 kWh · 365 · f_θ; mit f_KW,A = (50 − 12)/(50 − 11) wird die Auslegung 29 200 kWh/a;
            // jede Woche des flachen Jahrs (52 Wochen und ein Montag, Σ 7 · w_T = 365,05) trägt 7/365,05 davon.
            Assert.True(Relativ(g.Woche.WochensummeKwh, 29200.0 * 7.0 / 365.05) < 1e-9);
        }

        /// <summary>Nur das Wohnhaus am Speicher: <paramref name="we"/> WE × 2 Personen.</summary>
        private static Auslegungsgruppe Wohnhaus(int we, ProjektStand projekt = null, IDictionary<string, double> ersetzen = null)
        {
            ZonenStand wohnhaus = Zone("Wohnhaus", 1, 2.0 * we, 1) with
            {
                Wohnungen = new[] { new WohnungstypStand { Anzahl = we, Personen = 2 } }
            };
            Zapfprofileingang e = ZapfprofilTestbau.Eingang(projekt ?? Projekt(), Auslegungssatz(ersetzen), wohnhaus);
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(e, new[] { Wohnen }, Zusatz());
            Assert.Empty(r.Ablehnungen);
            return Assert.Single(r.Gruppen);
        }

        /// <summary>Keine Großanlage: Schwelle des Speichervolumens weit über jedem Fall.</summary>
        private static readonly Dictionary<string, double> Kleinanlage = new Dictionary<string, double>
        {
            [ZapfAuslegungParameter.W551_GROSS_VOLUMEN] = 1e6
        };

        [Fact]
        public void Die_Speichertemperatur_folgt_Projekt_Grossanlage_Schnellpfad_Vorgabe()
        {
            Parametersatz ps = Auslegungssatz();
            var prot = new Herkunftsprotokoll();
            Speichertemperaturwahl w = Speichertemperaturwahl.Waehlen(Projekt(), ps, false, false, prot);
            Assert.Equal(new Speichertemperaturwahl(56.0, Speichertemperaturquelle.Vorgabe, false), w);
            Assert.Equal(Wertstatus.Vorgabe, prot.Letzter("", "Auslegung.SpeicherC").Status);
            Assert.Equal(new Speichertemperaturwahl(58.0, Speichertemperaturquelle.Schnellpfad, true),
                         Speichertemperaturwahl.Waehlen(Projekt(), ps, false, true, null));
            Assert.Equal(new Speichertemperaturwahl(62.0, Speichertemperaturquelle.Grossanlage, false),
                         Speichertemperaturwahl.Waehlen(Projekt(), ps, true, false, null));
            // Die Großanlage geht dem Schnellpfad vor.
            Assert.Equal(Speichertemperaturquelle.Grossanlage, Speichertemperaturwahl.Waehlen(Projekt(), ps, true, true, null).Quelle);
            // Der Projektwert geht allem vor.
            w = Speichertemperaturwahl.Waehlen(Projekt() with { SpeicherC = 55.0 }, ps, true, true, prot);
            Assert.Equal(new Speichertemperaturwahl(55.0, Speichertemperaturquelle.Projekt, false), w);
            Assert.Equal(Wertstatus.Ueberschrieben, prot.Letzter("", "Auslegung.SpeicherC").Status);
            // Fehlt die Vorgabe, lehnt die Wahl benannt ab — kein Rückfall auf die Mindesttemperatur.
            Assert.Throws<ParametersatzException>(() => Speichertemperaturwahl.Waehlen(Projekt(),
                Auslegungssatz(null, ZapfAuslegungParameter.SPEICHERTEMPERATUR_VORGABE), false, false, null));
        }

        [Fact]
        public void Eine_Speichertemperatur_gilt_fuer_alle_Verfahren_bis_und_ueber_der_Schnellpfadgrenze()
        {
            // Anwendungsgrenze des Vereinfachungsverfahrens (erfunden): 5 WE. Bis zu ihr der Schnellpfad
            // mit seiner Speichertemperatur, darüber die Vorgabe — jeweils für ALLE Verfahren der Gruppe.
            foreach (var (we, soll, quelle) in new[] { (5, 58.0, Speichertemperaturquelle.Schnellpfad),
                                                      (6, 56.0, Speichertemperaturquelle.Vorgabe) })
            {
                Auslegungsgruppe g = Wohnhaus(we, null, Kleinanlage);
                Assert.Equal(soll, g.Speichertemperatur.SpeicherC);
                Assert.Equal(quelle, g.Speichertemperatur.Quelle);
                bool schnell = quelle == Speichertemperaturquelle.Schnellpfad;
                Assert.Equal(schnell, g.Empfehlung.Schnellauslegung);
                Assert.Equal(schnell, g.Empfehlung.Vermerk.Contains("Schnellauslegung"));
                Assert.Equal(schnell, g.Summenlinie.Schnellpfad);

                double dT = soll - 12.0;
                // Summenlinie: Q_max = V · c_w · Δθ · f_l / 1000 mit derselben Temperatur.
                Assert.True(Relativ(g.Summenlinie.Nachweis.SpeicherMaxKwh, g.Summenlinie.Punkt.VolumenL * 1.163 * dT * 0.8 / 1000.0) < 1e-12);
                // DIN 4708: V_DIN = W_z · 1000 / (c_w · Δθ) / f_nutz mit derselben Temperatur.
                Assert.True(Relativ(g.Normvergleich.VolumenL.Value, g.Normvergleich.WzKwh.Value * 1000.0 / (1.163 * dT) / 0.75) < 1e-12);
                Assert.Equal(g.Normvergleich.VolumenL, g.Speicherauslegung.VolumenDinL);
                // Verfahrensvergleich: klassisch P · 40 l · 45 K / Δθ · 1,1 mit derselben Temperatur.
                Assert.True(Relativ(g.Speicherauslegung.VolumenKlassischL.Value, 2.0 * we * 40.0 * 45.0 / dT * 1.1) < 1e-12);
                // Die Reihenfolge hält Liter derselben Temperatur gegeneinander.
                Assert.Equal(g.Summenlinie.Punkt.VolumenL >= g.Normvergleich.VolumenL.Value,
                             g.Hinweise.Any(h => h.Code == Dreiergruppe.HINWEIS_REIHENFOLGE));
            }
        }

        [Fact]
        public void Die_Mindesttemperatur_gilt_nur_bei_Grossanlage()
        {
            // Kleinanlage: die Vorgabe, keine Warnung „unter der Mindesttemperatur".
            Auslegungsgruppe klein = Wohnhaus(6, null, Kleinanlage);
            Assert.Equal(Speichertemperaturquelle.Vorgabe, klein.Speichertemperatur.Quelle);
            Assert.False(klein.Grossanlage.Gross);
            Assert.DoesNotContain(klein.Hinweise, h => h.Code == "SPEICHERTEMPERATUR_UNTER_MINDEST");
            Assert.DoesNotContain(klein.Hinweise, h => h.Code == ZapfprofilAuslegung.HINWEIS_TEMPERATUR_GROSSANLAGE);

            // Großanlage schon vorab (Leitungsinhalt 10 l über der erfundenen Schwelle 4 l): gleich W 551.
            Auslegungsgruppe leitung = Wohnhaus(6, Projekt() with { LeitungsinhaltL = 10.0 }, Kleinanlage);
            Assert.Equal(new Speichertemperaturwahl(62.0, Speichertemperaturquelle.Grossanlage, false), leitung.Speichertemperatur);
            Assert.True(leitung.Grossanlage.DurchLeitung);
            Assert.DoesNotContain(leitung.Hinweise, h => h.Code == ZapfprofilAuslegung.HINWEIS_TEMPERATUR_GROSSANLAGE);

            // Erst das empfohlene Volumen erkennt die Großanlage (Schwelle 10 l): einmal neu mit W 551, mit Hinweis.
            var kleineSchwelle = new Dictionary<string, double> { [ZapfAuslegungParameter.W551_GROSS_VOLUMEN] = 10.0 };
            Auslegungsgruppe gross = Wohnhaus(6, null, kleineSchwelle);
            Assert.Equal(Speichertemperaturquelle.Grossanlage, gross.Speichertemperatur.Quelle);
            Assert.Equal(62.0, gross.Speichertemperatur.SpeicherC);
            Assert.True(gross.Grossanlage.Gross && gross.Grossanlage.DurchSpeicher);
            Assert.Contains(gross.Hinweise, h => h.Code == ZapfprofilAuslegung.HINWEIS_TEMPERATUR_GROSSANLAGE);
            Assert.True(Relativ(gross.Speicherauslegung.VolumenKlassischL.Value, 12.0 * 40.0 * 45.0 / 50.0 * 1.1) < 1e-12);
            // Auch im Schnellpfad: die Großanlage beendet ihn.
            Auslegungsgruppe grossSchnell = Wohnhaus(5, null, kleineSchwelle);
            Assert.Equal(Speichertemperaturquelle.Grossanlage, grossSchnell.Speichertemperatur.Quelle);
            Assert.False(grossSchnell.Empfehlung.Schnellauslegung);

            // Der Projektwert bleibt auch bei Großanlage — dann warnt die Speicherauslegung.
            Auslegungsgruppe projekt = Wohnhaus(6, Projekt() with { LeitungsinhaltL = 10.0, SpeicherC = 55.0 }, Kleinanlage);
            Assert.Equal(new Speichertemperaturwahl(55.0, Speichertemperaturquelle.Projekt, false), projekt.Speichertemperatur);
            Assert.Contains(projekt.Hinweise, h => h.Code == "SPEICHERTEMPERATUR_UNTER_MINDEST" && h.Warnung);
        }

        [Fact]
        public void Die_Grossanlage_wird_am_Nenninhalt_erkannt()
        {
            // Temperatur im Projekt fest (kein zweiter Durchgang): Volumen V und Nenninhalt N > V.
            ProjektStand p = Projekt() with { SpeicherC = 62.0 };
            Auslegungsgruppe frei = Wohnhaus(6, p, Kleinanlage);
            double v = frei.Empfehlung.VolumenL.Value, n = frei.Empfehlung.NenninhaltL.Value;
            Assert.True(n > v, "Der Fall braucht einen Nenninhalt über dem Punkt: V " + v + " l, N " + n + " l.");
            // Schwelle zwischen V und N: die Anlage ist am Nenninhalt groß, am Punkt wäre sie es nicht.
            var zwischen = new Dictionary<string, double> { [ZapfAuslegungParameter.W551_GROSS_VOLUMEN] = 0.5 * (v + n) };
            Auslegungsgruppe g = Wohnhaus(6, p, zwischen);
            Assert.True(g.Grossanlage.Gross);
            Assert.True(g.Grossanlage.DurchSpeicher);
            Assert.Equal(n, g.Grossanlage.VolumenL);
            Assert.False(Grossanlage.Erkennen(v, null, Auslegungssatz(zwischen)).Gross);
        }

        /// <summary>Ein Referenztag des Katalogs mit Bezugsmenge 15 (erfunden).</summary>
        private static readonly BedarfstagKatalogzeile Referenztag = new BedarfstagKatalogzeile(6, "Referenztag (fiktiv)",
            "TEST-Z2", ZapfBedarfstagquelle.A100Referenz, 15.0, Fiktiv,
            new[] { new Zapfereignis(420, 10, 3.0), new Zapfereignis(1080, 30, 6.0) });

        private static Auslegungsgruppe Durchflussgruppe(ProjektStand projekt, params ZonenStand[] zonen)
        {
            Zapfprofileingang e = ZapfprofilTestbau.Eingang(projekt, Auslegungssatz(), zonen);
            Auslegungseingang a = Zusatz() with { Bedarfstage = new[] { Konstruiert, Referenztag } };
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(e, new[] { Wohnen, Buero, Art(3, bezug: ZapfBezugsart.Wohneinheiten) }, a);
            Assert.Empty(r.Ablehnungen);
            return Assert.Single(r.Gruppen);
        }

        [Fact]
        public void Ein_Katalogtag_skaliert_je_Bezugsart_und_gilt_bei_theta_KW_A()
        {
            ProjektStand p = Projekt() with { BedarfstagQuelle = ZapfBedarfstagquelle.A100Referenz, IdBedarfstag = 6 };
            ZonenStand buero = Zone("Büro", 2, 30.0, 1) with { Topologie = ZapfTopologie.Durchfluss };

            // Eine Bezugsart (Personen): Tag mal 30 / 15.
            Auslegungsgruppe g = Durchflussgruppe(p, buero);
            Assert.True(Relativ(g.Bedarfstag.TagessummeKwh, 9.0 * 2.0) < 1e-12);

            // Zwei Bezugsarten (Personen und Wohneinheiten): nicht summiert, benannt abgelehnt.
            ZonenStand weitere = Zone("Wohnungen", 3, 4.0, 2) with { Topologie = ZapfTopologie.Durchfluss };
            g = Durchflussgruppe(p, buero, weitere);
            Assert.Null(g.Bedarfstag);
            Assert.False(g.Empfehlung.Rechenbar);
            Assert.Contains("verschiedene Bezugsarten", g.Empfehlung.GrundText);
            Assert.Contains(g.Hinweise, h => h.Code == "BEDARFSTAG_NICHT_RECHENBAR");

            // θ_KW,A des Projekts 10 °C statt 12 °C des Katalogs: Faktor (50 − 10) / (50 − 12) bei θ_Zapf 50 °C.
            g = Durchflussgruppe(p with { KaltwasserAuslegungC = 10.0 }, buero);
            Assert.True(Relativ(g.Bedarfstag.TagessummeKwh, 9.0 * 2.0 * 40.0 / 38.0) < 1e-12);

            // Verschiedene Zapftemperaturen: die Umrechnung ist nicht eindeutig — benannt abgelehnt.
            ZonenStand heiss = Zone("Büro 2", 2, 10.0, 2) with { Topologie = ZapfTopologie.Durchfluss, ZapftemperaturC = 55.0 };
            g = Durchflussgruppe(p with { KaltwasserAuslegungC = 10.0 }, buero, heiss);
            Assert.Null(g.Bedarfstag);
            Assert.Contains("nicht eindeutig", g.Empfehlung.GrundText);
            // Ohne abweichendes θ_KW,A stört die Zapftemperatur nicht.
            Assert.NotNull(Durchflussgruppe(p, buero, heiss).Bedarfstag);
        }

        /// <summary>Ein Tag nach Art eines Ecodesign-Profils: je eine Wohneinheit, Bezugsart Wohneinheiten (erfunden).</summary>
        private static readonly BedarfstagKatalogzeile Haushaltstag = new BedarfstagKatalogzeile(7, "Haushaltstag (fiktiv)",
            "TEST-Z2", ZapfBedarfstagquelle.Ecodesign, 1.0, Fiktiv,
            new[] { new Zapfereignis(420, 10, 1.0), new Zapfereignis(1200, 5, 0.5) }) { Bezugsart = ZapfBezugsart.Wohneinheiten };

        private static Auslegungsgruppe Haushaltsgruppe(params ZonenStand[] zonen)
        {
            ProjektStand p = Projekt() with { BedarfstagQuelle = ZapfBedarfstagquelle.Ecodesign, IdBedarfstag = 7 };
            Zapfprofileingang e = ZapfprofilTestbau.Eingang(p, Auslegungssatz(), zonen);
            Auslegungseingang a = Zusatz() with { Bedarfstage = new[] { Konstruiert, Haushaltstag } };
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(e, new[] { Wohnen, Buero, Art(3, bezug: ZapfBezugsart.Wohneinheiten) }, a);
            Assert.Empty(r.Ablehnungen);
            return Assert.Single(r.Gruppen);
        }

        /// <summary>
        /// <b>Ein Tag je Wohneinheit skaliert mit den Wohneinheiten</b> (Folgeposten #546, Ecodesign
        /// nach Wohneinheiten): auf die Bezugsmenge einer Zone in Wohneinheiten, sonst auf die
        /// Wohnungstabelle; über zehn Wohneinheiten mit dem Hinweis <c>ECODESIGN_SKALIERT</c>
        /// (keine Sperre), bis zehn ohne.
        /// </summary>
        [Fact]
        public void Ein_Tag_je_Wohneinheit_skaliert_mit_den_Wohneinheiten_und_nennt_grosse_Zonen()
        {
            // Zone in Wohneinheiten, 8 WE: Tag mal 8, kein Hinweis.
            Auslegungsgruppe g = Haushaltsgruppe(Zone("Wohnungen", 3, 8.0, 1) with { Topologie = ZapfTopologie.Durchfluss });
            Assert.True(Relativ(g.Bedarfstag.TagessummeKwh, 1.5 * 8.0) < 1e-12);
            Assert.DoesNotContain(g.Hinweise, h => h.Code == ZapfprofilAuslegung.HINWEIS_ECODESIGN_SKALIERT);

            // 12 WE: Tag mal 12, benannter Hinweis, die Gruppe rechnet.
            g = Haushaltsgruppe(Zone("Wohnungen", 3, 12.0, 1) with { Topologie = ZapfTopologie.Durchfluss });
            Assert.True(Relativ(g.Bedarfstag.TagessummeKwh, 1.5 * 12.0) < 1e-12);
            Auslegungshinweis hinweis = Assert.Single(g.Hinweise, h => h.Code == ZapfprofilAuslegung.HINWEIS_ECODESIGN_SKALIERT);
            Assert.False(hinweis.Warnung);

            // Zone in Personen mit Wohnungstabelle (20 WE): die Wohneinheiten der Tabelle tragen den Tag.
            ZonenStand wohnhaus = Zone("Wohnhaus", 1, 40.0, 1) with
            {
                Topologie = ZapfTopologie.Durchfluss,
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 20, Personen = 2 } }
            };
            g = Haushaltsgruppe(wohnhaus);
            Assert.True(Relativ(g.Bedarfstag.TagessummeKwh, 1.5 * 20.0) < 1e-12);
            Assert.Contains(g.Hinweise, h => h.Code == ZapfprofilAuslegung.HINWEIS_ECODESIGN_SKALIERT);
        }

        [Fact]
        public void Der_empfohlene_Punkt_ueber_dem_Listenende_warnt_auch_ohne_Raster()
        {
            ZonenStand wohnhaus = Zone("Wohnhaus", 1, 40.0, 1) with
            {
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 20, Personen = 2 } }
            };
            Zapfprofileingang e = ZapfprofilTestbau.Eingang(Projekt(),
                Auslegungssatz(null, ZapfAuslegungParameter.NENNINHALT_RASTER), wohnhaus);
            Auslegungseingang a = Zusatz() with { Nenninhalte = Nenninhaltsliste.Aus(new[] { 10.0, 20.0 }) };
            Auslegungsgruppe g = Assert.Single(ZapfprofilAuslegung.Rechnen(e, new[] { Wohnen }, a).Gruppen);
            Assert.True(g.Empfehlung.VolumenL > 20.0);
            Assert.Null(g.Empfehlung.NenninhaltL);
            Assert.Contains(g.Hinweise, h => h.Code == "MEHRSPEICHER" && h.Text.StartsWith("Der empfohlene Punkt", StringComparison.Ordinal));
            Assert.Contains(g.Hinweise, h => h.Code == ZapfHinweis.PARAMETER_FEHLT && h.Text.Contains(ZapfAuslegungParameter.NENNINHALT_RASTER));
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

        /// <summary>
        /// N11 (c), Stufe Z4: Die Marke „Schnellauslegung" setzt der Kern je Stufe — in der Stufe
        /// Einfach trägt jeder rechenbare Punkt sie, in Erweitert und Experte nur der Schnellpfad;
        /// ohne Stufe (Lauf ohne Dialog) ebenso nur der Schnellpfad.
        /// </summary>
        [Fact]
        public void Die_Stufe_Einfach_markiert_jeden_Punkt_als_Schnellauslegung()
        {
            Auslegungsempfehlung Speicher(ZapfStufe? stufe)
                => ZapfprofilAuslegung.Rechnen(Eingang(), new[] { Wohnen, Buero }, Zusatz() with { Stufe = stufe })
                                      .Gruppen.Single(g => g.Topologie == ZapfTopologie.Speicher).Empfehlung;

            Auslegungsempfehlung einfach = Speicher(ZapfStufe.Einfach);
            Assert.True(einfach.Rechenbar);
            Assert.True(einfach.Schnellauslegung);
            Assert.Contains(einfach.Vermerke, v => v.Kennung == "AUSTEXT_VERMERK_SCHNELLAUSLEGUNG");
            Assert.False(Speicher(ZapfStufe.Erweitert).Schnellauslegung);
            Assert.False(Speicher(ZapfStufe.Experte).Schnellauslegung);
            Assert.False(Speicher(null).Schnellauslegung);
        }
    }
}
