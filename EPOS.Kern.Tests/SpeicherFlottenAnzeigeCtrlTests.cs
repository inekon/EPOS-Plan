using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG #184 (Paket P2 des Konzepts „Stromspeicher-Dialoge"): Die Flottenbilder
    /// folgen seither der Hausregel § 5 —
    /// <see cref="SpeicherFlottenAnzeigeCtrl.Bilder"/> nimmt Reihenwahl, „sortiert",
    /// Zeitraum und Datenzoom entgegen und reicht sie an
    /// <see cref="ChartRenderer.Speicherbetrieb"/> durch.
    ///
    /// <para><b>Warum über die BYTES geprüft wird.</b> Ein PNG lässt sich nicht befragen.
    /// Dass ein Parameter beim Renderer ankommt, zeigt deshalb dieselbe Gegenprobe, die
    /// auch die ChartProben führen: derselbe Aufruf einmal mit und einmal ohne den
    /// Parameter muss ZWEI verschiedene Bilder liefern. Ein stillschweigend übergangener
    /// Parameter bestünde jede Maß- und Farbprüfung.</para>
    ///
    /// <para><b>Der Prüfstand.</b> Zehn Tage im Viertelstundenraster (960 Intervalle),
    /// zwei Einheiten. Die Last schwingt über den Tag, der Speicher lädt nachts und
    /// entlädt mittags; das Peak-Ziel liegt zwischen beidem. Keine Datenbank, kein
    /// Rechenlauf — die Reihen stehen fest verdrahtet da.</para>
    /// </summary>
    public sealed class SpeicherFlottenAnzeigeCtrlTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherFlottenAnzeigeCtrlTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        private const int Tage = 10;
        private const int Intervalle = Tage * 96;

        // =================================================================
        // Die Reihenwahl
        // =================================================================

        [Fact]
        public void Die_Reihenliste_nennt_jede_Einheit_mit_Leistung_und_Ladezustand()
        {
            var reihen = SpeicherFlottenAnzeigeCtrl.Betriebsreihen(Ergebnis());
            string[] schluessel = reihen.Select(r => r.Schluessel).ToArray();

            Assert.Contains(SpeicherFlottenAnzeigeCtrl.REIHE_OHNE, schluessel);
            Assert.Contains(SpeicherFlottenAnzeigeCtrl.REIHE_MIT, schluessel);
            Assert.Contains(SpeicherFlottenAnzeigeCtrl.REIHE_GESAMT, schluessel);
            Assert.Contains(SpeicherFlottenAnzeigeCtrl.REIHE_PEAKZIEL, schluessel);
            Assert.Contains(SpeicherFlottenAnzeigeCtrl.ReiheEinheit("a"), schluessel);
            Assert.Contains(SpeicherFlottenAnzeigeCtrl.ReiheSoc("a"), schluessel);
            Assert.Contains(SpeicherFlottenAnzeigeCtrl.ReiheEinheit("b"), schluessel);
            Assert.Contains(SpeicherFlottenAnzeigeCtrl.ReiheSoc("b"), schluessel);

            // Der Name der Einheit steht im Reihentext - eine namenlose Legende waere keine.
            Assert.Contains(reihen, r => r.Schluessel == SpeicherFlottenAnzeigeCtrl.ReiheEinheit("a")
                                         && r.Text.Contains("Speicher A", StringComparison.Ordinal));
        }

        [Fact]
        public void Ohne_Peak_Ziel_gibt_es_keinen_Schalter_dafuer()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            e.Konfiguration.Optionen.WirtschaftlicherPeakZielwertKw = null;

            Assert.DoesNotContain(SpeicherFlottenAnzeigeCtrl.REIHE_PEAKZIEL,
                SpeicherFlottenAnzeigeCtrl.Betriebsreihen(e).Select(r => r.Schluessel));
        }

        [Fact]
        public void Eine_abgewaehlte_Reihe_aendert_das_Bild()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            var alle = SpeicherFlottenAnzeigeCtrl.Betriebsreihen(e).Select(r => r.Schluessel).ToList();
            var ohneGesamt = alle.Where(s => s != SpeicherFlottenAnzeigeCtrl.REIHE_GESAMT).ToList();

            byte[] mit = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, alle).Netz;
            byte[] ohne = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, ohneGesamt).Netz;

            Assert.NotNull(mit);
            Assert.NotNull(ohne);
            Assert.False(mit.SequenceEqual(ohne), "Die Reihenwahl erreicht den Renderer nicht.");
        }

        [Fact]
        public void Eine_LEERE_Reihenliste_heisst_keine_Reihe_und_nicht_alle()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            var alle = SpeicherFlottenAnzeigeCtrl.Betriebsreihen(e).Select(r => r.Schluessel).ToList();

            byte[] keine = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, new List<string>()).Netz;
            byte[] volle = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, alle).Netz;

            Assert.NotNull(keine);
            Assert.False(keine.SequenceEqual(volle));
        }

        [Fact]
        public void Ohne_Angabe_sind_ALLE_Reihen_im_Bild()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            var alle = SpeicherFlottenAnzeigeCtrl.Betriebsreihen(e).Select(r => r.Schluessel).ToList();

            byte[] ohneAngabe = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0).Netz;
            byte[] volle = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, alle).Netz;

            Assert.True(ohneAngabe.SequenceEqual(volle));
        }

        // =================================================================
        // sortiert, Zeitraum, Datenzoom und die zweite Achse
        // =================================================================

        [Fact]
        public void Der_Schalter_sortiert_erreicht_den_Renderer()
        {
            SpeicherFlottenErgebnis e = Ergebnis();

            byte[] gang = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, null, false).Netz;
            byte[] dauer = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, null, true).Netz;

            Assert.False(gang.SequenceEqual(dauer), "Die Dauerlinie erreicht den Renderer nicht.");
        }

        [Fact]
        public void Die_Zeitraumwahl_verschiebt_das_Fenster()
        {
            SpeicherFlottenErgebnis e = Ergebnis();

            (byte[] woche1, _, string zeitraum1) = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7);
            (byte[] woche2, _, string zeitraum2) = SpeicherFlottenAnzeigeCtrl.Bilder(e, 7, 7);
            byte[] tag = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 1).Netz;
            byte[] jahr = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 366).Netz;

            Assert.False(woche1.SequenceEqual(woche2));
            Assert.False(woche1.SequenceEqual(tag));
            Assert.False(woche1.SequenceEqual(jahr));
            Assert.NotEqual(zeitraum1, zeitraum2);
        }

        [Fact]
        public void Die_Abschnitte_zaehlen_angefangene_Wochen_und_Tage_mit()
        {
            (int wochen, int tage) = SpeicherFlottenAnzeigeCtrl.Abschnitte(Ergebnis());

            Assert.Equal(Tage, tage);
            Assert.Equal(2, wochen);   // zehn Tage sind zwei angefangene Wochen
            Assert.Equal((1, 1), SpeicherFlottenAnzeigeCtrl.Abschnitte(null));
        }

        [Fact]
        public void Der_Datenzoom_schneidet_das_Netzbild_zu()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            var bereich = new ChartRenderer.Bildausschnitt(0.30, 0.55, 0.10, 0.95);

            byte[] voll = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7).Netz;
            byte[] gezoomt = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, null, false, bereich).Netz;

            Assert.False(voll.SequenceEqual(gezoomt), "Der Datenzoom erreicht den Renderer nicht.");
        }

        [Fact]
        public void Jedes_Bild_fuehrt_seinen_EIGENEN_Ausschnitt()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            var bereich = new ChartRenderer.Bildausschnitt(0.30, 0.55, 0.10, 0.95);

            (byte[] netzVoll, byte[] socVoll, _) = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7);
            (byte[] netzGezoomt, byte[] socUnberuehrt, _) =
                SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, null, false, bereich);

            Assert.False(netzVoll.SequenceEqual(netzGezoomt));
            Assert.True(socVoll.SequenceEqual(socUnberuehrt),
                        "Der Ausschnitt des Netzbildes hat das Ladezustandsbild verändert.");
        }

        [Fact]
        public void Die_zweite_Achse_zeigt_die_GEWAEHLTE_Einheit()
        {
            SpeicherFlottenErgebnis e = Ergebnis();

            byte[] a = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0).Netz;
            byte[] b = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 1).Netz;

            Assert.False(a.SequenceEqual(b), "Die Wahl der Ladezustands-Einheit erreicht das Bild nicht.");
        }

        [Fact]
        public void Ohne_gewaehlten_Ladezustand_faellt_die_zweite_Achse_weg()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            var alle = SpeicherFlottenAnzeigeCtrl.Betriebsreihen(e).Select(r => r.Schluessel).ToList();
            var ohneSoc = alle.Where(s => s != SpeicherFlottenAnzeigeCtrl.ReiheSoc("a")).ToList();

            byte[] mit = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, alle).Netz;
            byte[] ohne = SpeicherFlottenAnzeigeCtrl.Bilder(e, 0, 7, 0, ohneSoc).Netz;

            Assert.False(mit.SequenceEqual(ohne), "Der Ladezustand steht nicht auf der zweiten Achse.");
        }

        [Fact]
        public void Ohne_Lauf_gibt_es_kein_Bild()
        {
            (byte[] netz, byte[] soc, string zeitraum) =
                SpeicherFlottenAnzeigeCtrl.Bilder(new SpeicherFlottenErgebnis());

            Assert.Null(netz);
            Assert.Null(soc);
            Assert.Equal("", zeitraum);
        }

        // =================================================================
        // Jahresprojektion
        // =================================================================

        [Fact]
        public void Die_Jahresprojektion_entsteht_aus_den_Jahreskonten()
        {
            byte[] bild = SpeicherFlottenAnzeigeCtrl.Jahresprojektionsbild(Ergebnis());

            Assert.NotNull(bild);
            Assert.True(bild.Length > 1000);
        }

        [Fact]
        public void Eine_abgewaehlte_Projektionsreihe_aendert_das_Bild()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            var alle = SpeicherFlottenAnzeigeCtrl.Projektionsreihen().Select(r => r.Schluessel).ToList();
            var ohneKumuliert = alle.Where(s => s != SpeicherFlottenAnzeigeCtrl.REIHE_KUMULIERT).ToList();

            byte[] mit = SpeicherFlottenAnzeigeCtrl.Jahresprojektionsbild(e, alle);
            byte[] ohne = SpeicherFlottenAnzeigeCtrl.Jahresprojektionsbild(e, ohneKumuliert);

            Assert.False(mit.SequenceEqual(ohne));
        }

        [Fact]
        public void Ohne_Jahreskonten_gibt_es_keine_Projektion()
        {
            SpeicherFlottenErgebnis e = Ergebnis();
            e.Studie.Wirtschaftlichkeit = null;

            Assert.Null(SpeicherFlottenAnzeigeCtrl.Jahresprojektionsbild(e));
        }

        // =================================================================
        // Die Vergleichszeilen samt Delta
        // =================================================================

        [Fact]
        public void Delta_ist_Mit_minus_Ohne_und_die_Richtung_haengt_an_der_Kennzahl()
        {
            var zeilen = SpeicherFlottenAnzeigeCtrl.Vergleichszeilen(Ergebnis());

            FlottenVergleichszeile spitze = zeilen.Single(z => z.Einheit == "kW");
            Assert.Equal(120.0, spitze.Ohne);
            Assert.Equal(100.0, spitze.Mit);
            Assert.Equal(-20.0, spitze.Delta);
            Assert.True(spitze.NegativIstBesser);
            Assert.True(spitze.IstBesser);          // weniger Spitze ist besser

            FlottenVergleichszeile einspeisung =
                zeilen.First(z => z.Bezeichnung == WindowsFormsApplication1.MyResource.Resource.FLOTTE_VGL_EINSPEISUNG);
            Assert.False(einspeisung.NegativIstBesser);
        }

        [Fact]
        public void Eine_Zeile_ohne_Wert_auf_beiden_Seiten_ist_eine_Nullzeile()
        {
            var zeilen = SpeicherFlottenAnzeigeCtrl.Vergleichszeilen(Ergebnis());

            Assert.Contains(zeilen, z => z.IstNullzeile);
            Assert.All(zeilen.Where(z => z.IstNullzeile), z =>
            {
                Assert.Equal(0.0, z.Ohne);
                Assert.Equal(0.0, z.Mit);
                Assert.Equal(0.0, z.Delta);
            });
            Assert.Null(zeilen.First(z => z.IstNullzeile).IstBesser);
        }

        [Fact]
        public void Ohne_Lauf_gibt_es_keine_Vergleichszeilen()
            => Assert.Empty(SpeicherFlottenAnzeigeCtrl.Vergleichszeilen(new SpeicherFlottenErgebnis()));

        // =================================================================
        // Prüfstand
        // =================================================================

        /// <summary>
        /// Zehn Tage im Viertelstundenraster mit zwei Einheiten: Die Last schwingt über
        /// den Tag, Speicher A arbeitet gegen die Mittagsspitze, Speicher B halb so
        /// stark und um sechs Stunden versetzt — sonst wären die zwei Einheitenreihen
        /// dieselbe Kurve, und die Gegenprobe „andere Einheit, anderes Bild" träfe nicht.
        /// </summary>
        private static SpeicherFlottenErgebnis Ergebnis()
        {
            var einheiten = new List<FlottenEinheit>
            {
                new() { Id = "a", Name = "Speicher A", KapazitaetKWh = 24, LadeleistungKw = 10,
                        EntladeleistungKw = 12, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5 },
                new() { Id = "b", Name = "Speicher B", KapazitaetKWh = 16, LadeleistungKw = 6,
                        EntladeleistungKw = 7, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5 }
            };

            var variante = new FlottenSimulationErgebnis
            {
                Zulaessig = true,
                NetzbezugKWh = 41000,
                NetzeinspeisungKWh = 0,
                MaximalerNetzbezugKw = 100,
                VerlusteKWh = 122.4,
                SpeicherKennzahlen = new List<FlottenSpeicherKennzahlen>
                {
                    new() { SpeicherId = "a", LadeenergieAcKWh = 1800, EntladeenergieAcKWh = 1650,
                            AnfangsenergieKWh = 12, EndenergieKWh = 12, AequivalenteVollzyklen = 34.2 },
                    new() { SpeicherId = "b", LadeenergieAcKWh = 900, EntladeenergieAcKWh = 820,
                            AnfangsenergieKWh = 8, EndenergieKWh = 8, AequivalenteVollzyklen = 25.6 }
                }
            };
            var referenz = new FlottenSimulationErgebnis
            {
                Zulaessig = true,
                NetzbezugKWh = 40000,
                NetzeinspeisungKWh = 0,
                MaximalerNetzbezugKw = 120
            };

            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            for (int i = 0; i < Intervalle; i++)
            {
                double stunde = i % 96 / 4.0;
                double last = 70 + 40 * Math.Sin(2 * Math.PI * (stunde - 6) / 24.0);
                double a = 8 * Math.Sin(2 * Math.PI * (stunde - 6) / 24.0);
                double b = 4 * Math.Sin(2 * Math.PI * (stunde - 12) / 24.0);

                referenz.Intervalle.Add(new FlottenIntervallErgebnis
                {
                    Zeitstempel = start.AddMinutes(15 * i),
                    LastKw = last,
                    NetzleistungKw = last
                });
                variante.Intervalle.Add(new FlottenIntervallErgebnis
                {
                    Zeitstempel = start.AddMinutes(15 * i),
                    LastKw = last,
                    NetzleistungKw = last - a - b,
                    IstleistungKwJeSpeicher = new List<double> { a, b },
                    EnergieStartKWhJeSpeicher = new List<double> { 12 - a, 8 - b },
                    EnergieEndeKWhJeSpeicher = new List<double> { 12 + a, 8 + b }
                });
            }

            var konten = new List<FlottenJahreskonto>();
            for (int jahr = 1; jahr <= 20; jahr++)
                konten.Add(new FlottenJahreskonto
                {
                    Jahr = jahr,
                    OpexEuro = 120,
                    DurchsatzkostenEuro = 40,
                    ErsatzkostenEuro = jahr == 10 ? 4200 : 0,
                    NettoCashflowEuro = 1400 - (jahr == 10 ? 4200 : 0)
                });

            return new SpeicherFlottenErgebnis
            {
                Erfolg = true,
                Konfiguration = new FlottenStudieKonfiguration
                {
                    Einheiten = einheiten,
                    Optionen = new FlottenSimulationOptionen
                    {
                        Betriebsziel = FlottenBetriebsziel.PeakShaving,
                        Verteilung = FlottenVerteilung.Kaskade,
                        WirtschaftlicherPeakZielwertKw = 100
                    },
                    Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
                    {
                        Kalkulationszins = 0.03,
                        ProjektjahreBeiWiederholung = 20
                    }
                },
                Studie = new FlottenStudienErgebnis
                {
                    Variante = variante,
                    ReferenzOhneSpeicher = referenz,
                    Referenzrechnung = new FlottenRechnung { GesamtEuro = 18000, EnergiekostenEuro = 15000, LeistungskostenEuro = 3000 },
                    Variantenrechnung = new FlottenRechnung { GesamtEuro = 17200, EnergiekostenEuro = 14700, LeistungskostenEuro = 2500 },
                    Wirtschaftlichkeit = new FlottenWirtschaftlichkeitErgebnis
                    {
                        InvestitionEuro = 15000,
                        KapitalwertEuro = 3140,
                        Jahreskonten = konten
                    }
                }
            };
        }
    }
}
