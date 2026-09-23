using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schicht S1 — Mengengerüst, Temperaturen und Messwertgrenzen</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.0, 4.1). Alle Zahlen sind ERFUNDEN und rund; geprüft werden Formeln
    /// und Relationen, nie eine Normzahl. Toleranz exakt bzw. relativ 1e-12.
    /// </summary>
    public sealed class MengengeruestTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static Mengenergebnis Menge(ZonenStand z, Nutzungsart n, Parametersatz ps = null,
                                            Herkunftsprotokoll p = null, List<ZapfHinweis> h = null,
                                            IReadOnlyDictionary<string, double> belegung = null)
        {
            ps ??= Parameter();
            Zonentemperaturen t = Mengengeruest.Temperaturen(z, n, ps, p);
            return Mengengeruest.JahresenergieKwh(z, n, t, ps, belegung, p, h);
        }

        [Fact]
        public void Bezugsmenge_mal_Bedarf_ergibt_die_Jahresmenge()
        {
            // Projekttemperaturen = Bezugstemperaturen -> f_θ = 1.
            ZonenStand z = Zone(menge: 10.0) with { KaltwasserMittelC = 12.0 };
            Mengenergebnis m = Menge(z, Art());
            Assert.Equal(1.0, m.Temperaturfaktor);
            Assert.Equal(10.0 * 2.0 * 365.0, m.JahresenergieKwh);
            Assert.Equal(10.0, m.Bezugsmenge);
        }

        [Fact]
        public void Der_Temperaturfaktor_rechnet_auf_die_Projekttemperaturen_um()
        {
            // θ_Zapf 55, θ̄_KW 11 (Parameter), Bezug 50/12 -> f = 44/38.
            var p = new Herkunftsprotokoll();
            ZonenStand z = Zone(menge: 10.0) with { ZapftemperaturC = 55.0 };
            Mengenergebnis m = Menge(z, Art(), p: p);
            Assert.Equal(44.0 / 38.0, m.Temperaturfaktor, 15);
            Assert.True(Relativ(m.JahresenergieKwh, 10.0 * 2.0 * 365.0 * 44.0 / 38.0) < 1e-12);
            Assert.Equal(Wertstatus.Umgerechnet, p.Letzter("Zone A", ZapfFeld.JAHRESENERGIE).Status);
            Assert.Equal(Wertstatus.Umgerechnet, p.Letzter("Zone A", ZapfFeld.TEMPERATURFAKTOR).Status);
            Assert.Equal(Wertstatus.Ueberschrieben, p.Letzter("Zone A", ZapfFeld.ZAPFTEMPERATUR).Status);
            Assert.Equal(Wertstatus.Vorgabe, p.Letzter("Zone A", ZapfFeld.KALTWASSER_MITTEL).Status);
        }

        [Fact]
        public void Ueberschreibung_setzt_den_Status()
        {
            var p = new Herkunftsprotokoll();
            ZonenStand z = Zone(menge: 4.0) with { BedarfSpezKwhJeEinheitTag = 5.0, KaltwasserMittelC = 12.0 };
            Mengenergebnis m = Menge(z, Art(), p: p);
            Assert.Equal(4.0 * 5.0 * 365.0, m.JahresenergieKwh);
            Assert.Equal(Wertstatus.Ueberschrieben, p.Letzter("Zone A", ZapfFeld.BEDARF_SPEZ).Status);
            Assert.Equal(Wertstatus.Ueberschrieben, p.Letzter("Zone A", ZapfFeld.JAHRESENERGIE).Status);
            Assert.Null(p.Letzter("Zone A", ZapfFeld.BEDARF_SPEZ).Quelle);
        }

        [Fact]
        public void Das_Niveau_waehlt_den_Katalogwert_und_die_Vorgabe_traegt_Provenienz()
        {
            var p = new Herkunftsprotokoll();
            ZonenStand z = Zone(menge: 1.0) with { Niveau = ZapfNiveau.Hoch, KaltwasserMittelC = 12.0 };
            Assert.Equal(3.0 * 365.0, Menge(z, Art(), p: p).JahresenergieKwh);
            Herkunftseintrag e = p.Letzter("Zone A", ZapfFeld.BEDARF_SPEZ);
            Assert.Equal(Wertstatus.Vorgabe, e.Status);
            Assert.Equal(Herkunftsart.Fiktiv, e.Quelle.Art);
        }

        [Fact]
        public void Die_Flaechenformel_folgt_dem_Verfahren()
        {
            // a = 20, b = 0,1, c = 6 (erfunden): A_WE 80 -> 12; A_WE 200 -> Untergrenze 6.
            Assert.Equal(12.0, Mengengeruest.FlaechenkennwertWohnenKwhJeM2(80.0, 20.0, 0.1, 6.0), 12);
            Assert.Equal(6.0, Mengengeruest.FlaechenkennwertWohnenKwhJeM2(200.0, 20.0, 0.1, 6.0));
            Assert.Equal(6.0, Mengengeruest.FlaechenkennwertWohnenKwhJeM2(140.0, 20.0, 0.1, 6.0), 12);   // Knick

            Nutzungsart wohnen = Art(bezug: ZapfBezugsart.Flaeche, kalender: ZapfKalenderart.Wohnen);
            ZonenStand z = Zone(menge: 800.0) with { WohnflaecheJeWeM2 = 80.0, KaltwasserMittelC = 12.0 };
            Assert.True(Relativ(Menge(z, wohnen).JahresenergieKwh, 12.0 * 800.0) < 1e-12);

            // Ohne Wohnfläche der Zone: Vorgabe aus dem Parametersatz (80).
            ZonenStand ohne = Zone(menge: 800.0) with { KaltwasserMittelC = 12.0 };
            Assert.True(Relativ(Menge(ohne, wohnen).JahresenergieKwh, 12.0 * 800.0) < 1e-12);
        }

        [Fact]
        public void Der_manuelle_Tagesbedarf_gilt_ohne_Umrechnung()
        {
            ZonenStand z = Zone() with { TagesbedarfAuto = false, TagesbedarfManuellKwh = 10.0, ZapftemperaturC = 55.0 };
            Mengenergebnis m = Menge(z, Art());
            Assert.Equal(3650.0, m.JahresenergieKwh);
            Assert.Equal(1.0, m.Temperaturfaktor);

            ZonenStand leer = Zone() with { TagesbedarfAuto = false };
            var ex = Assert.Throws<ZapfprofilEingabeException>(() => Menge(leer, Art()));
            Assert.Equal(ZapfEingabefehler.TagesbedarfUngueltig, ex.Fehler);
        }

        [Fact]
        public void Die_Wohnungstabelle_bildet_die_Bezugsmenge()
        {
            var belegung = new Dictionary<string, double> { ["2"] = 1.5 };
            ZonenStand z = Zone(menge: 999.0) with
            {
                KaltwasserMittelC = 12.0,
                Wohnungen = new[]
                {
                    new WohnungstypStand { Anzahl = 2, Personen = 3.0 },
                    new WohnungstypStand { Anzahl = 1, Raumzahl = 2.0 },
                }
            };
            Assert.Equal(7.5, Menge(z, Art(bezug: ZapfBezugsart.Personen), belegung: belegung).Bezugsmenge);
            Assert.Equal(3.0, Menge(z, Art(bezug: ZapfBezugsart.Wohneinheiten)).Bezugsmenge);

            var ex = Assert.Throws<ZapfprofilEingabeException>(() => Menge(z, Art(bezug: ZapfBezugsart.Personen)));
            Assert.Equal(ZapfEingabefehler.BelegungFehlt, ex.Fehler);
        }

        [Fact]
        public void Ungueltige_Eingaben_werden_benannt_abgelehnt()
        {
            var ex = Assert.Throws<ZapfprofilEingabeException>(() => Menge(Zone(menge: 0.0), Art()));
            Assert.Equal(ZapfEingabefehler.BezugsmengeFehlt, ex.Fehler);
            Assert.Equal("Zone A", ex.Zone);

            ex = Assert.Throws<ZapfprofilEingabeException>(() => Menge(Zone() with { ZapftemperaturC = 11.0 }, Art()));
            Assert.Equal(ZapfEingabefehler.TemperaturUngueltig, ex.Fehler);

            ex = Assert.Throws<ZapfprofilEingabeException>(() => Menge(Zone() with { BedarfSpezKwhJeEinheitTag = -1.0 }, Art()));
            Assert.Equal(ZapfEingabefehler.RasterUngueltig, ex.Fehler);

            var pex = Assert.Throws<ParametersatzException>(
                () => Menge(Zone(), Art(), Parameter(null, ZapfParameter.KALTWASSER_MITTEL)));
            Assert.Equal(ParametersatzFehler.ParameterFehlt, pex.Fehler);
            Assert.Equal(ZapfParameter.KALTWASSER_MITTEL, pex.Schluessel);
        }

        [Fact]
        public void Ein_Bedarf_ausserhalb_der_Bandbreite_gibt_einen_Hinweis()
        {
            var band = new Bedarfsbandbreite(new double?[] { null, 1.0, null }, new double?[] { null, 3.0, null });
            var h = new List<ZapfHinweis>();
            Menge(Zone() with { BedarfSpezKwhJeEinheitTag = 4.0 }, Art(bandbreite: band), h: h);
            Assert.Contains(h, x => x.Code == "BEDARF_AUSSERHALB_BANDBREITE");

            h.Clear();
            Menge(Zone() with { BedarfSpezKwhJeEinheitTag = 2.5 }, Art(bandbreite: band), h: h);
            Assert.Empty(h);
        }

        // =================================================================================
        // Messwertgrenzen (4.1)
        // =================================================================================

        [Fact]
        public void Der_Messwert_skaliert_mit_ausgewiesenem_Faktor()
        {
            var m = new Messwert(3650.0, ZapfBilanzgrenze.Zapfstelle, null, "Zähler (fiktiv)", "Jahr 1");
            Kalibrierergebnis k = Mengengeruest.Kalibrieren(m, 7300.0, 500.0);
            Assert.Equal(0.5, k.Faktor);
            Assert.Equal(3650.0, k.ZapfungKwh);
            Assert.Equal(500.0, k.ZirkulationKwh);   // Grenze 1: Zirkulation bleibt
        }

        [Fact]
        public void Messwert_mit_Zirkulation_zaehlt_nicht_doppelt()
        {
            var m = new Messwert(1000.0, ZapfBilanzgrenze.MitVerteilung, null, null, null);
            Kalibrierergebnis k = Mengengeruest.Kalibrieren(m, 600.0, 200.0);
            Assert.Equal(1.25, k.Faktor);
            Assert.Equal(1000.0, k.ZapfungKwh + k.ZirkulationKwh, 12);

            var m3 = new Messwert(1100.0, ZapfBilanzgrenze.MitSpeicher, 100.0, null, null);
            Kalibrierergebnis k3 = Mengengeruest.Kalibrieren(m3, 600.0, 200.0);
            Assert.Equal(1000.0, k3.MesswertNettoKwh);
            Assert.Equal(1000.0, k3.ZapfungKwh + k3.ZirkulationKwh, 12);

            var ohne = new Messwert(1100.0, ZapfBilanzgrenze.MitSpeicher, null, null, null);
            Assert.Equal(ZapfEingabefehler.MesswertUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Mengengeruest.Kalibrieren(ohne, 600.0, 200.0)).Fehler);
        }

        [Fact]
        public void Der_Volumenmesswert_wird_ueber_die_Temperaturen_umgerechnet()
        {
            var t = new Zonentemperaturen(50.0, 12.0, 0.0, 1);
            ZonenStand z = Zone() with { Jahresmesswert = 10.0, JahresmesswertEinheit = ZapfMesswerteinheit.KubikmeterJeJahr };
            Messwert m = Mengengeruest.MesswertAus(z, t);
            Assert.Equal(10.0 * Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * 38.0, m.WertKwh, 9);
            Assert.Equal(ZapfBilanzgrenze.Zapfstelle, m.Grenze);

            ZonenStand falsch = z with { JahresmesswertBilanzgrenze = ZapfBilanzgrenze.MitVerteilung };
            Assert.Equal(ZapfEingabefehler.MesswertUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => Mengengeruest.MesswertAus(falsch, t)).Fehler);
        }

        // =================================================================================
        // Temperaturumrechnung (4.0; Methodikkonzept 2.1)
        // =================================================================================

        [Fact]
        public void Umrechnung_auf_Bezugstemperatur()
        {
            // V_neu = V_Tab · Δθ_Tab / Δθ_neu — dieselbe Energie bei anderer Spreizung.
            double vNeu = Mengengeruest.VolumenUmrechnenL(100.0, 40.0, 50.0);
            Assert.Equal(80.0, vNeu, 12);
            Assert.Equal(Mengengeruest.EnergieKwh(100.0, 40.0), Mengengeruest.EnergieKwh(vNeu, 50.0), 12);
        }

        [Fact]
        public void Volumen_und_Energie_hin_und_zurueck()
        {
            foreach (double q in new[] { 0.5, 7.0, 1234.5 })
                foreach (double d in new[] { 20.0, 38.0, 45.5 })
                {
                    double v = Mengengeruest.VolumenL(q, d);
                    Assert.True(Relativ(Mengengeruest.EnergieKwh(v, d), q) < 1e-14);
                    Assert.True(Relativ(v, q * Mengengeruest.WH_JE_KWH / (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * d)) < 1e-15);
                }
            Assert.Throws<ZapfprofilEingabeException>(() => Mengengeruest.VolumenL(1.0, 0.0));

            // Liter je Einheit -> spezifischer Bedarf beim Temperaturbezug des Katalogs (erfundene 30 l).
            double q30 = Mengengeruest.SpezifischerBedarfAusLiternKwh(30.0, Bezug);
            Assert.Equal(30.0 * Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * 38.0 / 1000.0, q30, 14);
        }

        [Fact]
        public void Der_Temperaturfaktor_ist_eins_bei_gleichen_Temperaturen_und_lehnt_Nullspreizung_ab()
        {
            Assert.Equal(1.0, Mengengeruest.Temperaturfaktor(50.0, 12.0, Bezug));
            Assert.Equal(2.0, Mengengeruest.Temperaturfaktor(88.0, 12.0, Bezug), 14);
            Assert.Throws<ZapfprofilEingabeException>(
                () => Mengengeruest.Temperaturfaktor(50.0, 12.0, new Temperaturbezug(12.0, 12.0)));
            Assert.Throws<ZapfprofilEingabeException>(() => Mengengeruest.Temperaturfaktor(50.0, 12.0, null));
        }
    }
}
