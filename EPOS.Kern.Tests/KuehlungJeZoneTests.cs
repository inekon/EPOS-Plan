using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kühlung je Zone (Welle KU3-3; Entscheide E67, E68; Kühlkonzept 3.5, F-K15, K6)</b> — ohne Datenbank:
    /// die Vererbung der vier Kühlspalten (<see cref="Zonenvorgaben"/>), die Rechenprobe zweier Zonen, von denen
    /// eine heizt, während die andere kühlt (ausgewiesen, nicht saldiert), der Schalter <c>Kuehlung_Aktiv = 0</c>
    /// an der Zone, die Zonengrenze an Stelle des Flächenanteils, der übergeordnete Projektschalter und die
    /// Kennzahlen der Laufdatei.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KuehlungJeZoneTests
    {
        private readonly ITestOutputHelper _aus;

        public KuehlungJeZoneTests(ITestOutputHelper aus) { _aus = aus; }

        // =====================================================================
        //  Die Vorrichtung
        // =====================================================================

        /// <summary>
        /// Zwei Hälften des Probegebäudes, das gekühlt wird (Kühlsollwert 26 °C, Grenze 10 kW): Hälfte 1 warm
        /// geregelt (22 °C), Hälfte 2 kühl geregelt (Heizen 14 °C, eigene Kühlwerte nach <paramref name="zwei"/>).
        /// </summary>
        private static ProjektGebaeudeModel Zwei(Zoneneingaben zwei, double? grenzeGebaeudeKw = 10.0)
        {
            ProjektGebaeudeModel g = ZonenschleifeTests.Haelften(Trennflaechenzuordnung.Aussen);
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = 26.0;
            g.Kuehlleistung_Max = grenzeGebaeudeKw;
            GebaeudeZonensatz a = g.Zonen[0], b = g.Zonen[1];
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(a.ZonenId, a.Bezeichnung, a.Bauteile, a.Nutzflaeche_M2,
                                      new Zoneneingaben(Nutzflaeche: a.Nutzflaeche_M2, SollTag: 22.0, SollNacht: 22.0), a.Rang),
                new GebaeudeZonensatz(b.ZonenId, b.Bezeichnung, b.Bauteile, b.Nutzflaeche_M2,
                                      zwei with { Nutzflaeche = b.Nutzflaeche_M2 }, b.Rang),
            };
            return g;
        }

        /// <summary>Hälfte 2 heizt auf 14 °C und kühlt auf 16 °C — sie kühlt, während Hälfte 1 im Winter heizt.</summary>
        private static Zoneneingaben Kuehlzone(bool? aktiv = null, double? grenzeKw = null)
            => new Zoneneingaben(SollTag: 14.0, SollNacht: 14.0, KuehlungAktiv: aktiv, KuehlSollwert: 16.0, KuehlleistungMaxKw: grenzeKw);

        private static Mehrzonenergebnis Rechnen(ProjektGebaeudeModel g, bool kuehlbetrieb = true)
            => Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), kuehlbetrieb, null, 0, g.ID_Gebaeude);

        // =====================================================================
        //  Die Vererbung (Zonenvorgaben)
        // =====================================================================

        [Fact]
        public void Leere_Kuehlspalten_erben_vom_Gebaeude_die_Grenze_nach_dem_Flaechenanteil()
        {
            var gebaeude = new Gebaeudevorgaben(100.0, 2.5, 20, 18, 16, 15, 26, null, null, 0.5, null, null, 400, 3,
                                                true, 25.0, 27.0, 8.0);
            Zonenvorgaben v = Zonenvorgaben.Bilden(new Zoneneingaben(Nutzflaeche: 25.0), gebaeude, 2);
            Assert.True(v.KuehlungAktiv);
            Assert.Equal(new Vorgabewert(25.0, Vorgabeherkunft.Gebaeude), v.KuehlSollwert);
            Assert.Equal(new Vorgabewert(27.0, Vorgabeherkunft.Gebaeude), v.KuehlSollwertNacht);
            Assert.Equal(new Vorgabewert(2.0, Vorgabeherkunft.GebaeudeAnteilig), v.KuehlleistungMaxKw);
        }

        [Fact]
        public void Werte_der_Zone_ersetzen_die_des_Gebaeudes()
        {
            var gebaeude = new Gebaeudevorgaben(100.0, 2.5, 20, 18, 16, 15, 26, null, null, 0.5, null, null, 400, 3,
                                                true, 25.0, null, 8.0);
            Zonenvorgaben aus = Zonenvorgaben.Bilden(new Zoneneingaben(Nutzflaeche: 25.0, KuehlungAktiv: false), gebaeude, 2);
            Assert.False(aus.KuehlungAktiv);

            Zonenvorgaben eigen = Zonenvorgaben.Bilden(new Zoneneingaben(Nutzflaeche: 25.0, KuehlSollwert: 23.0,
                                                                         KuehlSollwertNacht: 28.0, KuehlleistungMaxKw: 0.7),
                                                       gebaeude with { KuehlungAktiv = false }, 2);
            Assert.False(eigen.KuehlungAktiv);  // leer: wie das Gebäude
            Assert.Equal(new Vorgabewert(23.0, Vorgabeherkunft.Zone), eigen.KuehlSollwert);
            Assert.Equal(new Vorgabewert(28.0, Vorgabeherkunft.Zone), eigen.KuehlSollwertNacht);
            Assert.Equal(new Vorgabewert(0.7, Vorgabeherkunft.Zone), eigen.KuehlleistungMaxKw);

            Zonenvorgaben ein = Zonenvorgaben.Bilden(new Zoneneingaben(KuehlungAktiv: true), gebaeude with { KuehlungAktiv = false }, 2);
            Assert.True(ein.KuehlungAktiv);     // eine 1 an der Zone schaltet sie ein
        }

        [Fact]
        public void Wirksamkeit_je_Zone_im_Konditionierungsweg()
        {
            ProjektGebaeudeModel g = Zwei(Kuehlzone(aktiv: false));
            Assert.True(Vdi6007Rechenweg.KuehlungWirksamFuer(g, 1, true));
            Assert.False(Vdi6007Rechenweg.KuehlungWirksamFuer(g, 2, true));
            Assert.False(Vdi6007Rechenweg.KuehlungWirksamFuer(g, 1, false));  // der Projektschalter steht darüber
            Assert.True(Vdi6007Rechenweg.KuehlungWirksamFuer(g, null, true));
        }

        // =====================================================================
        //  Die Rechenproben
        // =====================================================================

        /// <summary>
        /// <b>Ausgewiesen, nicht saldiert</b> (F-K15, K6): Hälfte 1 heizt, Hälfte 2 kühlt in denselben
        /// Stunden. Die Gebäudesummen sind Σ Zonen je Richtung; Stunden und Energie je Richtung stehen am
        /// Gebäude und stimmen mit der Nachrechnung aus den Zonenreihen überein.
        /// </summary>
        [Fact]
        public void Zwei_Zonen_heizen_und_kuehlen_gleichzeitig_ausgewiesen_nicht_saldiert()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(Kuehlzone()));
            GebaeudeModellErgebnis eins = m.Zonen[0], zwei = m.Zonen[1], geb = m.Gebaeude;
            Assert.Equal(16.0, m.Eingaenge[1].Eingang.KuehlSollwert);
            Assert.Equal(26.0, m.Eingaenge[0].Eingang.KuehlSollwert);
            Assert.NotNull(zwei.KuehlbedarfKwh);
            Assert.NotNull(geb.KuehlbedarfKwh);

            int stunden = 0;
            double heizKwh = 0.0, kuehlKwh = 0.0;
            int quer = 0;
            for (int h = 0; h < 8760; h++)
            {
                double heiz = Math.Max(eins.HeizlastW[h], 0.0) + Math.Max(zwei.HeizlastW[h], 0.0);
                double kuehl = (eins.KuehlbedarfKwh?[h] ?? 0.0) + zwei.KuehlbedarfKwh[h];
                Assert.Equal(heiz, geb.HeizlastW[h], 6);
                Assert.Equal(kuehl, geb.KuehlbedarfKwh[h], 9);
                bool h1 = eins.HeizlastW[h] > 0.0, k2 = zwei.KuehlbedarfKwh[h] > 0.0;
                if (h1 && k2) quer++;
                if (m.Schleife.StundenMitHeizen[h] && m.Schleife.StundenMitKuehlen[h])
                {
                    stunden++;
                    heizKwh += heiz / 1000.0;
                    kuehlKwh += kuehl;
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "gleichzeitig {0} h (Zone 1 heizt, Zone 2 kühlt: {1} h), Heizen {2:F1} kWh, Kühlen {3:F1} kWh",
                stunden, quer, heizKwh, kuehlKwh));
            Assert.True(quer > 0, "keine Stunde, in der Zone 1 heizt und Zone 2 kühlt");
            Assert.Equal(stunden, geb.StundenHeizenUndKuehlen);
            Assert.True(geb.GleichzeitigHeizenKwh > 0.0);
            Assert.True(geb.GleichzeitigKuehlenKwh > 0.0);
            Assert.Equal(heizKwh, geb.GleichzeitigHeizenKwh.Value, 6);
            Assert.Equal(kuehlKwh, geb.GleichzeitigKuehlenKwh.Value, 6);
            // Nicht saldiert: die Jahressummen sind die Summen der Zonen, keine Differenz.
            Assert.Equal(eins.JahresheizwaermeMwh + zwei.JahresheizwaermeMwh, geb.JahresheizwaermeMwh, 6);
            Assert.Equal((eins.KuehlenergieMwh ?? 0.0) + zwei.KuehlenergieMwh.Value, geb.KuehlenergieMwh.Value, 9);
        }

        [Fact]
        public void Zone_mit_Kuehlung_Aktiv_null_kuehlt_nicht()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(Kuehlzone(aktiv: false)));
            Assert.False(m.Eingaenge[1].Eingang.KuehlungWirksam);
            Assert.True(m.Eingaenge[0].Eingang.KuehlungWirksam);
            Assert.Null(m.Zonen[1].KuehlbedarfKwh);
            Assert.Null(m.Zonen[1].KuehlenergieMwh);
            Assert.True(double.IsPositiveInfinity(m.Eingaenge[1].Eingang.KuehlSollwert));
        }

        [Fact]
        public void Ohne_Projektschalter_kuehlt_keine_Zone_auch_mit_eigenem_Schalter()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(Kuehlzone(aktiv: true)), kuehlbetrieb: false);
            Assert.All(m.Eingaenge, z => Assert.False(z.Eingang.KuehlungWirksam));
            Assert.Null(m.Gebaeude.KuehlbedarfKwh);
            Assert.Null(m.Gebaeude.GleichzeitigHeizenKwh);
        }

        [Fact]
        public void Zonengrenze_ersetzt_den_Flaechenanteil()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(Kuehlzone(grenzeKw: 0.4)));
            Assert.Equal(400.0, m.Eingaenge[1].Eingang.KuehlleistungMaxW, 9);
            Assert.Equal(5000.0, m.Eingaenge[0].Eingang.KuehlleistungMaxW, 9);   // 10 kW × Anteil 0,5
            Assert.True(m.Zonen[1].KaeltespitzeKw <= 0.4 + 1e-9, "Kältespitze " + m.Zonen[1].KaeltespitzeKw);
            Assert.True(m.Zonen[1].KaeltespitzeKw > 0.39, "die Grenze greift nicht: " + m.Zonen[1].KaeltespitzeKw);
        }

        [Fact]
        public void Kennzahlen_je_Zone_und_der_gleichzeitigen_Richtungen()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(Kuehlzone()));
            Dictionary<string, double> s = GebaeudeErgebnisexport.Satz(m.Gebaeude).Skalare.ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(m.Zonen[1].KuehlenergieMwh.Value, s["Geb[0].Zone[1].KuehlenergieMwh"], 12);
            Assert.Equal(m.Zonen[1].KaeltespitzeKw.Value, s["Geb[0].Zone[1].KaeltespitzeKw"], 12);
            Assert.Equal(m.Zonen[1].StundenMitKuehlbedarf.Value, s["Geb[0].Zone[1].StundenMitKuehlbedarf"]);
            Assert.Equal(m.Gebaeude.StundenHeizenUndKuehlen, s["Geb[0].StundenHeizenUndKuehlen"]);
            Assert.Equal(m.Gebaeude.GleichzeitigHeizenKwh.Value, s["Geb[0].GleichzeitigHeizenKwh"], 12);
            Assert.Equal(m.Gebaeude.GleichzeitigKuehlenKwh.Value, s["Geb[0].GleichzeitigKuehlenKwh"], 12);

            // Ohne wirksame Kühlung kein Schlüssel (Muster E30) - so bleiben die Referenzläufe byte-gleich.
            Mehrzonenergebnis frei = Rechnen(Zwei(Kuehlzone()), kuehlbetrieb: false);
            var f = GebaeudeErgebnisexport.Satz(frei.Gebaeude).Skalare.Select(p => p.Key).ToList();
            Assert.DoesNotContain(f, k => k.Contains("Kaeltespitze") || k.Contains("Gleichzeitig") || k.Contains("StundenHeizenUndKuehlen"));
        }
    }
}
