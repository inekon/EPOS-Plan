using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Ensembles</b> (Umsetzungskonzept Zapfprofilgenerator 4.4, 4.5 b, Invariante 2.4):
    /// Erwartungswert der Jahresreihe gleich deterministischem Pfad (Toleranz max(1 %, 3·s_R/√R)),
    /// Reihenfolgeunabhängigkeit, Spitze je Einheit fällt mit √N, Gleichzeitigkeit je Topologie
    /// (GLF_V &gt; GLF_P), Perzentile, Mindestzahl, Entkopplung der Urlaube. Erfundene Kategorien
    /// und Werte (<see cref="ZapfereignisgeneratorTests.Kategorien"/>), keine Normzahl.
    /// </summary>
    public sealed class ZapfensembleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly double[] Eins = Enumerable.Repeat(1.0, 12).ToArray();

        /// <summary>Eine Zone der Jahresreihe mit Kaltwasser-Jahresgang (erfundene Temperaturen).</summary>
        private static Jahreszone Jahreszone(int einheiten = 10, double jahresKwh = 14600.0, Zeitstruktur s = null,
                                             IReadOnlyList<Ferienfenster> ferien = null, bool entkoppeln = false, int versatz = 0)
        {
            s ??= ZapfereignisgeneratorTests.Struktur();
            ferien ??= new Ferienfenster[0];
            var t = new Zonentemperaturen(50.0, 11.0, 3.0, 8);
            double[] kw = Kaltwassergang.Monatswerte(t.KaltwasserMittelC, t.KaltwasserAmplitudeK, t.KaltwasserMonatMaximum);
            return new Jahreszone
            {
                Index = 0, Zone = "Zone A", Einheiten = einheiten, JahresmengeKwh = jahresKwh, Struktur = s,
                Kategorien = Zapfkategoriensatz.Aus(ZapfereignisgeneratorTests.Kategorien(), 1, "Zone A"),
                Kalender = Zapfkalender.Bilden(0, We(0), ferien), Ferien = ferien,
                Kaltwasserfaktor = Kaltwassergang.Monatsfaktoren(t, "Zone A"),
                SpreizungJeMonatK = kw.Select(x => t.ZapfC - x).ToArray(),
                WochentagJan1 = 0, We = We(0), Urlaubsentkopplung = entkoppeln, UrlaubsversatzTage = versatz
            };
        }

        private static Bilanzreihe Deterministisch(Jahreszone z)
            => new Bilanzreihe(Formvektor.Stundenreihe(
                   Formvektor.Tagesmengen(z.JahresmengeKwh, z.Struktur, z.Kalender, z.WochentagJan1, z.Kaltwasserfaktor), z.Struktur, z.Kalender));

        // =================================================================================
        // Jahresreihe: Erwartungswert und Reihenfolge
        // =================================================================================

        [Fact]
        public void Der_Erwartungswert_trifft_den_deterministischen_Pfad()
        {
            foreach (int einheiten in new[] { 1, 10 })
            {
                Jahreszone z = Jahreszone(einheiten);
                Jahresensemble e = Jahresensemble.Ziehen(z, 1, 10);
                Jahreskonsistenz k = e.Pruefen(Deterministisch(z));
                Assert.Equal(10, e.Realisierungen);
                Assert.Equal(10, e.JeRealisierung.Count);
                Assert.Equal(14600.0, k.DeterministischKwh, 6);
                double toleranz = Math.Max(0.01 * 14600.0, 3.0 * e.StandardabweichungKwh / Math.Sqrt(10));
                Assert.Equal(toleranz, k.ToleranzKwh, 9);
                Assert.True(k.Erfuellt, "n_E = " + einheiten + ": Mittel " + k.MittelKwh.ToString("R", CultureInfo.InvariantCulture)
                                        + " kWh, Toleranz " + k.ToleranzKwh.ToString("R", CultureInfo.InvariantCulture));
                Assert.InRange(Math.Abs(e.Mittel.JahressummeKwh - 14600.0), 0.0, toleranz);
                Assert.Equal(e.MittelJahresenergieKwh, e.Mittel.JahressummeKwh, 6);
                Assert.True(e.StandardabweichungKwh > 0);
                // Energieprobe: Die Bilanz ist die Realisierung zum Seed mal dem Faktor — genau die Jahresmenge.
                Bilanzreihe bilanz = e.Bilanz(k);
                Assert.Equal(14600.0, bilanz.JahressummeKwh, 6);
                Assert.Equal(e.JeRealisierung[0].JahressummeKwh, k.JahrZumSeedKwh);
                Assert.Equal(k.DeterministischKwh / e.JeRealisierung[0].JahressummeKwh, k.Faktor);
                Assert.Equal(e.JeRealisierung[0].StundenKwh.Select(x => Bits(x * k.Faktor)), bilanz.StundenKwh.Select(Bits));
                // … und nie das Mittel des Ensembles.
                Assert.NotEqual(e.Mittel.Mal(k.DeterministischKwh / e.Mittel.JahressummeKwh).StundenKwh, bilanz.StundenKwh);
                // Formvektor = Erwartungswert: die Anteile je Tagesstunde (Ereignisdauer verschiebt nur wenig).
                Assert.InRange(k.TagesgangAbweichung, 0.0, 0.03);
            }
        }

        [Fact]
        public void Die_Reihenfolge_der_Faeden_aendert_kein_Bit()
        {
            Jahreszone z = Jahreszone(4);
            Jahresensemble seriell = Jahresensemble.Ziehen(z, 7, 6, parallel: false);
            Jahresensemble parallel = Jahresensemble.Ziehen(z, 7, 6, parallel: true);
            Assert.Equal(seriell.Mittel.StundenKwh.Select(Bits), parallel.Mittel.StundenKwh.Select(Bits));
            for (int r = 0; r < 6; r++)
                Assert.Equal(seriell.JeRealisierung[r].StundenKwh.Select(Bits), parallel.JeRealisierung[r].StundenKwh.Select(Bits));

            IReadOnlyList<Ensemblezone> zonen = Gruppe(8, 5.0);
            Bedarfstagensemble a = Zapfensemble.Ziehen(zonen, 7, 150, 95, parallel: false);
            Bedarfstagensemble b = Zapfensemble.Ziehen(zonen, 7, 150, 95, parallel: true);
            for (int r = 0; r < 150; r++)
                Assert.Equal(a.Tage[r].MinutenKwh.Select(Bits), b.Tage[r].MinutenKwh.Select(Bits));
            Assert.Equal(Bits(a.WurzelNSchaetzungKw(2.0)), Bits(b.WurzelNSchaetzungKw(2.0)));
            Assert.Equal(a.Zonen[0].SpitzeJeEinheitKw, b.Zonen[0].SpitzeJeEinheitKw);
        }

        [Fact]
        public void Derselbe_Seed_liefert_dieselbe_Reihe_ein_anderer_eine_andere()
        {
            Jahreszone z = Jahreszone(3);
            Bilanzreihe a = Jahresensemble.Ziehen(z, 11, 2).Mittel;
            Bilanzreihe b = Jahresensemble.Ziehen(z, 11, 2).Mittel;
            Bilanzreihe c = Jahresensemble.Ziehen(z, 12, 2).Mittel;
            Assert.Equal(a.StundenKwh, b.StundenKwh);
            Assert.NotEqual(a.StundenKwh, c.StundenKwh);
            // Eine Realisierung ist ein Jahr zum Seed: das Mittel aus einer ist sie selbst.
            Jahresensemble eins = Jahresensemble.Ziehen(z, 11, 1);
            Assert.Equal(eins.JeRealisierung[0].StundenKwh, eins.Mittel.StundenKwh);
            Assert.Equal(0.0, eins.StandardabweichungKwh);
            // Das Jahr zum Seed hängt nicht von R ab — die Bilanz ist dieselbe bei einem und bei zwei Jahren.
            Jahresensemble zwei = Jahresensemble.Ziehen(z, 11, 2);
            Assert.Equal(eins.JahrZumSeed.StundenKwh.Select(Bits), zwei.JahrZumSeed.StundenKwh.Select(Bits));
            Bilanzreihe det = Deterministisch(z);
            Assert.Equal(eins.Bilanz(eins.Pruefen(det)).StundenKwh.Select(Bits), zwei.Bilanz(zwei.Pruefen(det)).StundenKwh.Select(Bits));
            Assert.NotEqual(eins.Pruefen(det).MittelKwh, zwei.Pruefen(det).MittelKwh);
        }

        // =================================================================================
        // Entkopplung der Urlaube
        // =================================================================================

        [Fact]
        public void Die_Urlaube_werden_je_Einheit_versetzt()
        {
            // Wrap über den Jahreswechsel.
            Assert.Equal(new[] { new Ferienfenster(360, 365), new Ferienfenster(1, 4) },
                         Jahresensemble.Versetzt(new[] { new Ferienfenster(350, 359) }, 10));
            Assert.Equal(new[] { new Ferienfenster(362, 365), new Ferienfenster(1, 6) },
                         Jahresensemble.Versetzt(new[] { new Ferienfenster(2, 11) }, -5));
            Assert.Equal(new[] { new Ferienfenster(1, 365) }, Jahresensemble.Versetzt(new[] { new Ferienfenster(1, 365) }, 3));

            // Mit Ferienfaktor 0,2 fällt die Zone in den Ferien auf ein Fünftel; entkoppelt verteilt sich die Lücke.
            var s = new Zeitstruktur(Eins, Enumerable.Repeat(1.0 / 7, 7).ToArray(), 0.2,
                                     ZapfereignisgeneratorTests.Struktur().Tagesgaenge, new bool[4]);
            var ferien = new[] { new Ferienfenster(181, 201) };
            Jahreszone fest = Jahreszone(40, 14600.0, s, ferien);
            Jahreszone versetzt = Jahreszone(40, 14600.0, s, ferien, entkoppeln: true, versatz: 60);
            Jahresensemble a = Jahresensemble.Ziehen(fest, 3, 4);
            Jahresensemble b = Jahresensemble.Ziehen(versetzt, 3, 4);
            double Fenster(Bilanzreihe r) => r.StundenKwh.Skip(180 * 24).Take(21 * 24).Sum();
            double feste = Fenster(a.Mittel), entkoppelt = Fenster(b.Mittel), det = Fenster(Deterministisch(fest));
            Assert.InRange(feste, det * 0.9, det * 1.1);
            Assert.True(entkoppelt > 2.0 * feste, "Ferien fest " + feste + " kWh, entkoppelt " + entkoppelt + " kWh.");
            // Die Jahresenergie bleibt je Einheit erhalten — auch entkoppelt trifft das Mittel die Jahresmenge.
            Assert.True(b.Pruefen(Deterministisch(fest)).Erfuellt);
        }

        // =================================================================================
        // Auslegungsensemble: √N und Topologie
        // =================================================================================

        /// <summary>Eine Gruppe aus einer Zone mit n Einheiten und der Tagesmenge je Einheit (bei 40 K Spreizung).</summary>
        private static IReadOnlyList<Ensemblezone> Gruppe(int einheiten, double jeEinheitKwh)
            => new[]
            {
                new Ensemblezone(0, "Zone A", einheiten, Zapfkategoriensatz.Aus(ZapfereignisgeneratorTests.Kategorien(), 1, "Zone A"),
                                 einheiten * jeEinheitKwh, 40.0,
                                 Tageszeitdichte.Aus(ZapfereignisgeneratorTests.Struktur(), ZapfTagtyp.Werktag))
            };

        [Fact]
        public void Die_Spitze_je_Einheit_faellt_mit_Wurzel_N()
        {
            const int r = 200;
            int[] n = { 1, 4, 16, 64, 256 };
            var jeEinheit = new double[n.Length];
            var ueberschuss = new double[n.Length];
            double mittelJeEinheitKw = 0.0;
            for (int i = 0; i < n.Length; i++)
            {
                Bedarfstagensemble e = Zapfensemble.Ziehen(Gruppe(n[i], 8.0), 5, r, 95);
                jeEinheit[i] = e.MinutenspitzeKw.P95 / n[i];
                // Die mittlere Last je Einheit in der stärksten Minute — Grenze für N → ∞.
                mittelJeEinheitKw = e.WurzelNSchaetzungKw(0.0) / n[i];
                ueberschuss[i] = jeEinheit[i] - mittelJeEinheitKw;
            }
            string bericht = string.Join("; ", n.Select((x, i) => "N=" + x + ": " + jeEinheit[i].ToString("0.###", CultureInfo.InvariantCulture)
                                                                  + " kW/E, Überschuss " + ueberschuss[i].ToString("0.###", CultureInfo.InvariantCulture)));
            // Die Spitze je Einheit fällt mit wachsendem N …
            for (int i = 1; i < n.Length; i++) Assert.True(jeEinheit[i] < jeEinheit[i - 1], bericht);
            // … und der Überschuss über die mittlere Last liegt im Korridor um 1/√N: Vervierfachen halbiert ihn.
            for (int i = 2; i < n.Length; i++)
                Assert.InRange(ueberschuss[i - 1] / ueberschuss[i], 1.4, 2.8);
            Assert.True(ueberschuss[n.Length - 1] > 0, bericht);
        }

        [Fact]
        public void Die_Wurzel_N_Schaetzung_trifft_das_Perzentil_bei_vielen_Einheiten()
        {
            Bedarfstagensemble e = Zapfensemble.Ziehen(Gruppe(200, 8.0), 9, 200, 95);
            // z = 1,645 für P95 — ein Wert der Probe, nicht des Rechenwegs (dort Parameter). Die Schätzung
            // gilt je Minute; die Tagesspitze ist das Maximum über die Minuten und liegt deshalb darüber,
            // aber in derselben Größenordnung — ein Hinweis, keine Auslegungsgröße.
            double schaetzung = e.WurzelNSchaetzungKw(1.645);
            Assert.True(e.WurzelNSchaetzungKw(0.0) < schaetzung);
            Assert.True(e.WurzelNSchaetzungKw(0.0) < e.MinutenspitzeKw.P50);
            Assert.InRange(e.MinutenspitzeKw.P95, schaetzung, schaetzung * 1.6);
        }

        [Fact]
        public void Die_Gleichzeitigkeit_haengt_von_der_Topologie_ab()
        {
            Bedarfstagensemble eins = Zapfensemble.Ziehen(Gruppe(1, 8.0), 4, 100, 95);
            Assert.Equal(1.0, eins.GleichzeitigkeitLeistung.Value, 12);

            Bedarfstagensemble e = Zapfensemble.Ziehen(Gruppe(20, 8.0), 4, 100, 95);
            double glfP = e.GleichzeitigkeitLeistung.Value;
            Speicherensemble s = Zapfensemble.Volumina(e, Speicherparameter(), 20.0);
            Assert.Equal(0, s.OhneNachweis);
            double glfV = s.GleichzeitigkeitVolumen.Value;
            Assert.InRange(glfP, 0.0, 1.0);
            Assert.InRange(glfV, 0.0, 1.0);
            Assert.True(glfV > glfP, "GLF_V " + glfV + " ≤ GLF_P " + glfP);
            // Das Volumen der Realisierung ist das kleinste mit Nachweis beim festen Φ_N.
            double v95 = s.VolumenL.P95;
            Assert.True(v95 > 0);
            Assert.Equal(20.0, s.LeistungKw);
        }

        [Fact]
        public void Das_Volumen_waechst_mit_sinkender_Leistung_bis_zur_Tagesmenge()
        {
            Bedarfstagensemble e = Zapfensemble.Ziehen(Gruppe(20, 8.0), 4, 30, 95);
            Speicherensemble gross = Zapfensemble.Volumina(e, Speicherparameter(), 20.0);
            Speicherensemble klein = Zapfensemble.Volumina(e, Speicherparameter(), 5.0);
            Speicherensemble winzig = Zapfensemble.Volumina(e, Speicherparameter(), 0.01);
            Assert.True(klein.VolumenL.P95 > gross.VolumenL.P95);
            Assert.True(winzig.VolumenL.P95 > klein.VolumenL.P95);
            // Der Tag beginnt mit vollem Speicher: ohne Ladung trägt er die ganze Tagesmenge — nie mehr.
            double groessterTagL = e.Tage.Max(t => t.TagessummeKwh) * 1000.0
                                   / (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * 50.0);
            Assert.InRange(winzig.VolumenL.Maximum, 0.9 * groessterTagL, groessterTagL * 1.000001);
            Assert.Equal(0, winzig.OhneNachweis);
            Assert.Throws<ZapfAuslegungException>(() => Zapfensemble.Volumina(e, Speicherparameter(), 0.0));
        }

        private static Summenlinienparameter Speicherparameter() => new Summenlinienparameter
        {
            KaltwasserAuslegungC = 10.0, SpeicherC = 60.0, Ladungsfaktor = 1.0, SensorhoeheAnteil = 0.5,
            Speicherart = ZapfSpeicherart.Ladespeicher, VerzoegerungMin = 0.0, SpeicherverlustKw = 0.0,
            Zirkulation = Zirkulationslast.Keine
        };

        // =================================================================================
        // Perzentile, Mindestzahl, Belastbarkeit, Ablehnungen
        // =================================================================================

        [Fact]
        public void Perzentile_nach_dem_naechsten_Rang()
        {
            Perzentilwerte a = Perzentilwerte.Aus(Enumerable.Range(1, 100).Select(i => (double)(101 - i)).ToArray());
            Assert.Equal((50.0, 90.0, 95.0, 99.0, 1.0, 100.0), (a.P50, a.P90, a.P95, a.P99, a.Minimum, a.Maximum));
            Perzentilwerte b = Perzentilwerte.Aus(Enumerable.Range(1, 20).Select(i => (double)i).ToArray());
            Assert.Equal((10.0, 18.0, 19.0, 20.0), (b.P50, b.P90, b.P95, b.P99));
            Perzentilwerte c = Perzentilwerte.Aus(new[] { 3.0, double.PositiveInfinity, 1.0 });
            Assert.Equal(3.0, c.P50);
            Assert.True(double.IsPositiveInfinity(c.P95));
            Assert.Equal(5.0, Perzentilwerte.Aus(new[] { 5.0 }).P99);
            Assert.Throws<ZapfAuslegungException>(() => Perzentilwerte.Aus(new double[0]));
            Assert.Throws<ZapfAuslegungException>(() => Perzentilwerte.Aus(new[] { double.NaN }));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.Wert(97));
        }

        [Fact]
        public void Die_Mindestzahl_und_die_Vorgabe_der_Realisierungen()
        {
            Assert.Equal(100, Zapfensemble.Mindestzahl(99));
            Assert.Equal(20, Zapfensemble.Mindestzahl(95));
            Assert.Throws<ZapfAuslegungException>(() => Zapfensemble.Mindestzahl(90));
            Parametersatz ps = Parameter(new Dictionary<string, double> { [ZapfStochastikParameter.AUSLEGUNG_VIELFACHES] = 2.5 });
            Assert.Equal(250, Zapfensemble.RealisierungenAuslegung(null, 99, ps));
            Assert.Equal(50, Zapfensemble.RealisierungenAuslegung(null, 95, ps));
            Assert.Equal(7, Zapfensemble.RealisierungenAuslegung(7, 99, ps));
            Assert.Throws<ParametersatzException>(() => Zapfensemble.RealisierungenAuslegung(null, 99, Parameter()));
            Assert.Throws<ZapfAuslegungException>(() => Zapfensemble.RealisierungenAuslegung(0, 99, ps));
        }

        [Fact]
        public void Perzentil_bei_kleinem_R_nicht_belastbar()
        {
            Assert.False(Zapfensemble.Ziehen(Gruppe(3, 8.0), 1, 99, 99).Belastbar);
            Assert.True(Zapfensemble.Ziehen(Gruppe(3, 8.0), 1, 100, 99).Belastbar);
            Assert.False(Zapfensemble.Ziehen(Gruppe(3, 8.0), 1, 19, 95).Belastbar);
            Assert.True(Zapfensemble.Ziehen(Gruppe(3, 8.0), 1, 20, 95).Belastbar);
        }

        [Fact]
        public void Ungueltige_Ensembles_werden_benannt_abgelehnt()
        {
            var ex = Assert.Throws<ZapfprofilEingabeException>(() => Jahresensemble.Ziehen(Jahreszone(2), 1, 0));
            Assert.Equal(ZapfEingabefehler.StochastikUngueltig, ex.Fehler);
            Assert.Equal("Zone A", ex.Zone);
            Assert.Throws<ZapfprofilEingabeException>(() => Jahresensemble.Ziehen(Jahreszone(2) with { Einheiten = 0 }, 1, 2));
            // Ein Jahr zum Seed ohne Zapfung bei positiver Jahresmenge: benannte Ablehnung statt NaN in der Bilanz.
            Jahreszone winzig = Jahreszone(1, 1e-6);
            Jahresensemble w = Jahresensemble.Ziehen(winzig, 1, 1);
            Assert.Equal(0.0, w.JahrZumSeed.JahressummeKwh);
            var leer = Assert.Throws<ZapfprofilEingabeException>(() => w.Bilanz(w.Pruefen(Deterministisch(winzig))));
            Assert.Equal(ZapfEingabefehler.StochastikUngueltig, leer.Fehler);
            Assert.Equal("Zone A", leer.Zone);
            Assert.Throws<ZapfAuslegungException>(() => Zapfensemble.Ziehen(Gruppe(2, 8.0), 1, 10, 90));
            Assert.Throws<ZapfAuslegungException>(() => Zapfensemble.Ziehen(Gruppe(2, 8.0), 1, 0, 95));
            Assert.Throws<ZapfAuslegungException>(() => Zapfensemble.Ziehen(new Ensemblezone[0], 1, 10, 95));
        }

        [Fact]
        public void Kategorien_ohne_Anteil_ziehen_nichts_und_Fehler_der_Faeden_kommen_benannt()
        {
            // Eine Kategorie ohne Anteil und mit gestutztem Mittel 0 bricht kein Ensemble ab — parallel wie seriell.
            Zapfkategorie null_ = ZapfereignisgeneratorTests.Kategorien()[0] with
            {
                Name = "Null (fiktiv)", Anteil = 0.0, VolumenstromLJeMin = 0.0, StreuungLJeMin = 0.0, DauerMin = 3
            };
            Zapfkategoriensatz mitNull = Zapfkategoriensatz.Aus(ZapfereignisgeneratorTests.Kategorien().Append(null_).ToArray(), 1, "Zone A");
            Jahresensemble j = Jahresensemble.Ziehen(Jahreszone(3) with { Kategorien = mitNull }, 5, 3, parallel: true);
            Assert.Equal(Jahresensemble.Ziehen(Jahreszone(3), 5, 3).JahrZumSeed.StundenKwh, j.JahrZumSeed.StundenKwh);
            IReadOnlyList<Ensemblezone> gruppe = new[] { Gruppe(4, 8.0)[0] with { Kategorien = mitNull } };
            Bedarfstagensemble b = Zapfensemble.Ziehen(gruppe, 5, 40, 95, parallel: true);
            Assert.Equal(Zapfensemble.Ziehen(Gruppe(4, 8.0), 5, 40, 95).MinutenspitzeKw, b.MinutenspitzeKw);

            // Ein ungültiger Satz (Rate nicht endlich) wirft in jedem Faden — heraus kommt die benannte
            // Ablehnung, keine AggregateException.
            Zapfkategoriensatz ungueltig = Zapfkategoriensatz.Aus(new[]
            {
                ZapfereignisgeneratorTests.Kategorien()[0] with { VolumenstromLJeMin = 1e-310, StreuungLJeMin = 0.0 }
            }, 1, "Zone A");
            foreach (bool parallel in new[] { true, false })
            {
                var ej = Assert.Throws<ZapfprofilEingabeException>(() =>
                    Jahresensemble.Ziehen(Jahreszone(3) with { Kategorien = ungueltig }, 5, 4, parallel: parallel));
                Assert.Equal(ZapfEingabefehler.StochastikUngueltig, ej.Fehler);
                var eb = Assert.Throws<ZapfprofilEingabeException>(() =>
                    Zapfensemble.Ziehen(new[] { Gruppe(4, 8.0)[0] with { Kategorien = ungueltig } }, 5, 40, 95, parallel: parallel));
                Assert.Equal(ZapfEingabefehler.StochastikUngueltig, eb.Fehler);
            }
        }

        [Fact]
        public void Ein_gezogener_Tag_traegt_keine_Quelle_und_darf_leer_sein()
        {
            // Sehr kleine Tagesmenge: viele Realisierungen ohne Ereignis.
            Bedarfstagensemble e = Zapfensemble.Ziehen(Gruppe(1, 0.001), 2, 30, 95);
            Assert.All(e.Tage, t => Assert.True(t.Gezogen));
            Assert.All(e.Tage, t => Assert.Null(t.Quelle));
            Assert.Contains(e.Tage, t => t.Ereignisse.Count == 0 && t.TagessummeKwh == 0.0);
            Assert.Equal(0.0, e.MinutenspitzeKw.Minimum);
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);
    }
}
