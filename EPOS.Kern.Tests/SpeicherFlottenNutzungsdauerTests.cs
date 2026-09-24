using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E10, Teil B — <b>die Speicherflotte an der Nutzungsdauertabelle</b>
    /// (Empfehlung E10‑Q3 a).
    ///
    /// <para><b>Was hier gehalten wird.</b> Der LINEARE Restwert je Einheit auf der
    /// Ersatzkette der Flotte (<see cref="FlottenWirtschaftlichkeit.LinearerRestwert"/>:
    /// Betrag der letzten Beschaffung × Restdauer ÷ Nutzungsdauer); dass der feste
    /// <see cref="FlottenEinheit.RestwertEuro"/> als Altfeld nicht mehr rechnet; die
    /// Vorgabe des Ersatzintervalls aus der Standardzeile „Stromspeicher · Batterie"
    /// (gepflegter Wert hat Vorrang); und die Zahlen des Prüfprojekts 1046 vorher und
    /// nachher — zugleich die Gegenüberstellung mit Alternative b (fester Restwert hat
    /// Vorrang), die der Auftrag verlangt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SpeicherFlottenNutzungsdauerTests
    {
        /// <summary>Das Prüfprojekt der Speicherflotte (Stand <c>@Projektflotte</c>).</summary>
        private const int PRUEFPROJEKT = 1046;

        // =====================================================================
        //  Der lineare Restwert je Einheit (Engine)
        // =====================================================================

        /// <summary>
        /// Ohne Ersatz im Zeitraum trägt die Erstbeschaffung: Restwert = Investition ×
        /// (n − T) ÷ n.
        /// </summary>
        [Fact]
        public void Ohne_Ersatz_im_Zeitraum_ist_der_Restwert_ein_Anteil_der_Investition()
        {
            FlottenEinheit b = Einheit(investition: 1000.0, ersatzkosten: 500.0, intervall: 10);

            Assert.Equal(900.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 1), 9);
            Assert.Equal(600.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 4), 9);
            Assert.Equal(100.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 9), 9);
        }

        /// <summary>
        /// Nach einem Ersatz trägt die LETZTE Beschaffung — ihr Betrag sind die
        /// Ersatzkosten, ihr Alter zählt ab dem Ersatzjahr.
        /// </summary>
        [Fact]
        public void Nach_einem_Ersatz_rechnet_der_Restwert_auf_die_Ersatzkosten()
        {
            FlottenEinheit b = Einheit(investition: 1000.0, ersatzkosten: 500.0, intervall: 10);

            Assert.Equal(250.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 15), 9);   // Ersatz 10, Alter 5
            Assert.Equal(450.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 21), 9);   // Ersatz 20, Alter 1
        }

        /// <summary>
        /// Die Ersatzkette der Flotte bucht auch im LETZTEN Jahr; der Restwert steht dann
        /// mit vollem Betrag daneben — Ausgabe und Restwert heben sich im Kapitalwert auf,
        /// wie die Kostenpositionen im Jahr T gar nicht erst ersetzen.
        /// </summary>
        [Fact]
        public void Ein_Ersatz_im_letzten_Jahr_steht_mit_vollem_Betrag_als_Restwert_daneben()
        {
            FlottenEinheit b = Einheit(investition: 1000.0, ersatzkosten: 400.0, intervall: 5);

            Assert.Equal(400.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 5), 9);
            Assert.Equal(400.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 10), 9);

            // Im Kapitalwert (Zins 0): −1.000 Investition, −400 Ersatz in Jahr 5, +400 Restwert.
            FlottenWirtschaftlichkeitErgebnis e = FlottenWirtschaftlichkeit.Bewerte(
                Eingang(new List<FlottenEinheit> { b }, 0.0, 5, 0.0));
            Assert.Equal(400.0, e.RestwertEuro, 9);
            Assert.Equal(-1000.0, e.KapitalwertEuro, 9);
            Assert.Equal(400.0, e.Jahreskonten[4].ErsatzkostenEuro, 9);
        }

        /// <summary>Ohne Nutzungsdauer (Intervall 0) gibt es weder Ersatz noch Restwert.</summary>
        [Fact]
        public void Ohne_Nutzungsdauer_gibt_es_weder_Ersatz_noch_Restwert()
        {
            FlottenEinheit b = Einheit(investition: 1000.0, ersatzkosten: 500.0, intervall: 0);

            Assert.Equal(0.0, FlottenWirtschaftlichkeit.LinearerRestwert(b, 5));
            Assert.Equal(0.0, FlottenWirtschaftlichkeit.LinearerRestwert(null, 5));
            Assert.Equal(0.0, FlottenWirtschaftlichkeit.LinearerRestwert(
                Einheit(investition: 1000.0, ersatzkosten: 500.0, intervall: 10), 0));
        }

        /// <summary>
        /// <b>Das Altfeld rechnet nicht mehr:</b> Ein fester Restwert der Einheit ändert
        /// weder Restwert noch Kapitalwert; der zusätzliche Restwert der Studie bleibt.
        /// </summary>
        [Fact]
        public void Der_feste_Restwert_der_Einheit_rechnet_nicht_mehr()
        {
            FlottenEinheit ohne = Einheit(investition: 1000.0, ersatzkosten: 0.0, intervall: 0);
            FlottenEinheit mit = Einheit(investition: 1000.0, ersatzkosten: 0.0, intervall: 0);
            mit.RestwertEuro = 1_000_000.0;

            FlottenWirtschaftlichkeitErgebnis a = FlottenWirtschaftlichkeit.Bewerte(
                Eingang(new List<FlottenEinheit> { ohne }, 0.05, 10, 60.0));
            FlottenWirtschaftlichkeitErgebnis b = FlottenWirtschaftlichkeit.Bewerte(
                Eingang(new List<FlottenEinheit> { mit }, 0.05, 10, 60.0));

            Assert.Equal(60.0, a.RestwertEuro, 9);
            Assert.Equal(a.RestwertEuro, b.RestwertEuro, 9);
            Assert.Equal(a.KapitalwertEuro, b.KapitalwertEuro, 9);

            // Das Altfeld bleibt im Stand, wie es ist — es wird nur nicht mehr gelesen.
            Assert.Equal(1_000_000.0, SpeicherAuslegungKopie.Von(mit).RestwertEuro, 9);
        }

        // =====================================================================
        //  Die Vorgabe des Ersatzintervalls (Kern)
        // =====================================================================

        /// <summary>Die Nutzungsdauer der Tabelle wird kaufmännisch auf ganze Jahre gerundet.</summary>
        [Fact]
        public void Die_Nutzungsdauer_wird_kaufmaennisch_auf_ganze_Jahre_gerundet()
        {
            Assert.Equal(10, SpeicherFlottenStudieCtrl.ErsatzintervallAusNutzungsdauer(10.0));
            Assert.Equal(10, SpeicherFlottenStudieCtrl.ErsatzintervallAusNutzungsdauer(10.49));
            Assert.Equal(11, SpeicherFlottenStudieCtrl.ErsatzintervallAusNutzungsdauer(10.5));
            Assert.Equal(1, SpeicherFlottenStudieCtrl.ErsatzintervallAusNutzungsdauer(1.0));
            Assert.Equal(0, SpeicherFlottenStudieCtrl.ErsatzintervallAusNutzungsdauer(0.9));
            Assert.Equal(0, SpeicherFlottenStudieCtrl.ErsatzintervallAusNutzungsdauer(null));
            Assert.Equal(0, SpeicherFlottenStudieCtrl.ErsatzintervallAusNutzungsdauer(double.NaN));
        }

        /// <summary>
        /// Ein gepflegtes Intervall hat Vorrang; die Vorgabe füllt nur die Einheiten und
        /// Achsenvorlagen mit 0. Ohne Vorgabe ändert sich nichts.
        /// </summary>
        [Fact]
        public void Ein_gepflegtes_Intervall_hat_Vorrang_vor_der_Tabelle()
        {
            var f = new FlottenStudieKonfiguration();
            f.Einheiten.Add(Einheit(1000.0, 0.0, 0));
            f.Einheiten.Add(Einheit(1000.0, 0.0, 8));
            f.Einheiten.Add(Einheit(1000.0, 0.0, 0));
            f.Auslegung.Achsen.Add(new FlottenAuslegungsAchse { Vorlage = Einheit(1000.0, 0.0, 0) });

            Assert.Equal(0, SpeicherFlottenStudieCtrl.ErsatzintervallAufloesen(f, 0));
            Assert.Equal(0, f.Einheiten[0].ErsatzintervallJahre);

            Assert.Equal(3, SpeicherFlottenStudieCtrl.ErsatzintervallAufloesen(f, 10));
            Assert.Equal(new[] { 10, 8, 10 }, f.Einheiten.Select(x => x.ErsatzintervallJahre).ToArray());
            Assert.Equal(10, f.Auslegung.Achsen[0].Vorlage.ErsatzintervallJahre);

            // Ein zweiter Lauf findet nichts mehr.
            Assert.Equal(0, SpeicherFlottenStudieCtrl.ErsatzintervallAufloesen(f, 12));
            Assert.Equal(10, f.Einheiten[0].ErsatzintervallJahre);
        }

        /// <summary>
        /// Die Vorgabe ist die Nutzungsdauer der Standardzeile „Stromspeicher · Batterie"
        /// (ausgeliefert 10 a); sie folgt der Tabelle, und ohne Wert gibt es keine.
        /// </summary>
        [Fact]
        public void Die_Vorgabe_ist_die_Nutzungsdauer_der_Batteriezeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();

            Assert.Equal(10, SpeicherFlottenStudieCtrl.ErsatzintervallVorgabeJahre());
            Assert.Equal(10, SpeicherFlottenStudieCtrl.Vorbelegung(PRUEFPROJEKT, 0.0).ErsatzintervallVorgabeJahre);

            NutzungsdauerZeile batterie = NutzungsdauerCtrl.Standard(5);
            Assert.Equal("Batterie", batterie.Positionsart);

            batterie.Nutzungsdauer = 12.4;
            Assert.True(NutzungsdauerCtrl.Speichern(batterie, out string grund), grund);
            Assert.Equal(12, SpeicherFlottenStudieCtrl.ErsatzintervallVorgabeJahre());

            batterie.Nutzungsdauer = null;
            Assert.True(NutzungsdauerCtrl.Speichern(batterie, out grund), grund);
            Assert.Equal(0, SpeicherFlottenStudieCtrl.ErsatzintervallVorgabeJahre());
        }

        // =====================================================================
        //  Projekt 1046 vorher / nachher
        // =====================================================================

        /// <summary>
        /// <b>Die Zahlen des Prüfprojekts 1046</b> (zwei Einheiten, Intervall 10 a,
        /// Ersatzkosten 4.000/3.000 €, fester Restwert 500/300 €; 20 Projektjahre, 3 %).
        /// Die Ersatzkette bucht in den Jahren 10 und 20; der lineare Restwert ist damit der
        /// volle Betrag des Ersatzes im Jahr 20 — 7.000 € statt fester 800 €. Der
        /// Kapitalwert steigt um (7.000 − 800) ÷ 1,03²⁰ = 3.432,79 €, unabhängig vom
        /// Zahlungsstrom; mit dem Zahlungsstrom 0 stehen −27.358,66 € (neu) gegen
        /// −30.791,45 € (vorher, und so auch nach Alternative b: fester Restwert hat Vorrang).
        /// </summary>
        [Fact]
        public void Projekt_1046_rechnet_den_Restwert_linear_statt_fest()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SpeicherOptimierungEingaben eingaben = SpeicherAuslegungCtrl.Profile(PRUEFPROJEKT, 0)
                .Single(x => x.Name == SpeicherFlottenProjektCtrl.ProjektflottenStand).Eingaben;
            FlottenStudieKonfiguration f = SpeicherFlottenStudieCtrl.Konfiguration(eingaben);

            Assert.Equal(2, f.Einheiten.Count);
            Assert.All(f.Einheiten, x => Assert.Equal(10, x.ErsatzintervallJahre));
            Assert.Equal(7000.0, f.Einheiten.Sum(x => x.ErsatzkostenEuro), 9);
            double altfeld = f.Einheiten.Sum(x => x.RestwertEuro);
            Assert.Equal(800.0, altfeld, 9);
            Assert.Equal(0.03, f.Wirtschaftlichkeit.Kalkulationszins, 12);
            Assert.Equal(20, f.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);

            FlottenWirtschaftlichkeitEingang w = SpeicherAuslegungKopie.Von(f.Wirtschaftlichkeit);
            w.Einheiten = SpeicherAuslegungKopie.Von(f.Einheiten);
            w.Jahreskonten = new List<FlottenJahreskonto> { Konto(1, 0.0) };
            w.ReferenzjahrExplizitWiederholen = true;
            FlottenWirtschaftlichkeitErgebnis e = FlottenWirtschaftlichkeit.Bewerte(w);

            double abzinsung = Math.Pow(1.03, 20);
            Assert.Equal(22150.0, e.InvestitionEuro, 9);
            Assert.Equal(7000.0, e.RestwertEuro, 9);
            Assert.Equal(7000.0, e.Jahreskonten[9].ErsatzkostenEuro, 9);
            Assert.Equal(7000.0, e.Jahreskonten[19].ErsatzkostenEuro, 9);
            Assert.Equal(-27358.657404, e.KapitalwertEuro, 5);

            // Vorher bzw. Alternative b: der feste Restwert 800 € statt des linearen.
            double vorher = e.KapitalwertEuro - (e.RestwertEuro - altfeld) / abzinsung;
            Assert.Equal(-30791.447080, vorher, 5);
            Assert.Equal(3432.789676, e.KapitalwertEuro - vorher, 5);
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        private static FlottenEinheit Einheit(double investition, double ersatzkosten, int intervall) => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Probe",
            KapazitaetKWh = 10.0,
            LadeleistungKw = 5.0,
            EntladeleistungKw = 5.0,
            InvestitionEuro = investition,
            ErsatzkostenEuro = ersatzkosten,
            ErsatzintervallJahre = intervall,
            EigeneKosten = true
        };

        private static FlottenWirtschaftlichkeitEingang Eingang(List<FlottenEinheit> einheiten, double zins,
                                                               int jahre, double restwertStudie) => new()
        {
            Einheiten = einheiten,
            Kalkulationszins = zins,
            RestwertEuro = restwertStudie,
            Jahreskonten = new List<FlottenJahreskonto> { Konto(1, 0.0) },
            ReferenzjahrExplizitWiederholen = true,
            ProjektjahreBeiWiederholung = jahre
        };

        private static FlottenJahreskonto Konto(int jahr, double cf) => new()
        {
            Jahr = jahr,
            NettoCashflowEuro = cf,
            IstVollstaendigesJahr = true,
            Referenzrechnung = new FlottenRechnung { GesamtEuro = cf },
            Variantenrechnung = new FlottenRechnung()
        };
    }
}
