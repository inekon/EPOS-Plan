using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rechenproben des Kältespeichers</b> (KU3-5, Entscheid E68; Kühlkonzept 4.6, 5.5) — ohne Datenbank:
    /// Kaltwasserspeicher auf dem SOC des Puffers, Ladung aus freier Erzeugerleistung in der Ladephase,
    /// Entladung nach der freien Kühlung vor den verdichtenden Erzeugern, Verlust als Wärmeeintrag.
    /// </summary>
    public sealed class KaeltespeicherRechenwegTests
    {
        private const double EPS = 1e-9;

        /// <summary>Luftgekühlte Saatmaschine: 50 kW, EER 3,0 bei Rückkühlung 35 °C und Kaltwasser 6 °C.</summary>
        private static Kaelteerzeuger Luftgekuehlt()
        {
            KaeltemaschineSchema.Beispielgeraet b = KaeltemaschineSchema.SAAT[0];
            var m = new KaeltemaschineModel
            {
                Id = 1, Bezeichner = b.Bezeichner, Nennkaelteleistung_kW = b.Nennkaelteleistung, Nenn_EER = b.NennEer,
                Rueckkuehlart = b.Rueckkuehlart, Mindestteillast_Prozent = b.Mindestteillast,
                Hilfsstrom_Rueckkuehlung_kW = b.HilfsstromRueckkuehlung, Kaltwasser_Vorlauf_Min = b.KaltwasserVorlaufMin,
            };
            foreach (var k in b.Kennlinie)
                m.Kennlinie.Add(new KaeltemaschineKenndatenModel
                {
                    Rueckkuehltemperatur = k.Rueckkuehl, Kaltwassertemperatur = k.Kaltwasser, EER = k.Eer, Kaelteleistung_kW = k.Leistung
                });
            Kaeltemaschine km = Kaeltemaschine.AusModell(m, out _);
            km.Rueckkuehltemperatur_stuendlich = Enumerable.Repeat(35.0, Kaeltekaskade.STUNDEN).ToArray();
            return new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = km };
        }

        /// <summary>2 000 l, 6/12 °C: Q_max = 2 000 · 1,16 · 6 / 1 000 = 13,92 kWh.</summary>
        private static SimulationPufferspeicher Speicher(double verlusteProTag = 0, int vorlauf = 6, int ruecklauf = 12)
        {
            var sp = new SimulationPufferspeicher { Bezeichner = "Kaltwasser", ID_Pufferspeicher = 7 };
            sp.InitKaelte(2000, vorlauf, ruecklauf, verlusteProTag);
            sp.SchwelleEin = 0.10;
            sp.SchwelleAus = 0.95;
            return sp;
        }

        /// <summary>Jeder Tag ein Kühltag — geladen wird nur an Kühltagen.</summary>
        private static bool[] Immer => Enumerable.Repeat(true, Kaeltekaskade.TAGE).ToArray();

        private static double[] Bedarf(params (int Stunde, double Kwh)[] werte)
        {
            var b = new double[Kaeltekaskade.STUNDEN];
            foreach (var (h, kwh) in werte) b[h] = kwh;
            return b;
        }

        [Fact]
        public void Kaltwasserspeicher_Kapazitaet_Rolle_und_Kanal()
        {
            SimulationPufferspeicher sp = Speicher();
            Assert.True(sp.IstKaelte);
            Assert.Equal(SimulationPufferspeicher.VERWENDUNG_KAELTE, sp.Verwendung);
            Assert.Equal(13.92, sp.Q_max, 9);
            Assert.Equal(6, sp.KaltVorlauf);
            Assert.Equal(12, sp.KaltRuecklauf);
            Assert.False(sp.KaeltepaarVorgabe);
            Assert.True(sp.BedientKanal(Kanal.KUEHLUNG));
            Assert.False(sp.BedientKanal(Kanal.HEIZUNG));
            Assert.False(sp.BedientKanal(Kanal.BRAUCHWASSER));

            // Leeres oder vertauschtes Paar: Vorgabe 6/12 °C, vermerkt.
            SimulationPufferspeicher leer = Speicher(0, 0, 0);
            Assert.True(leer.KaeltepaarVorgabe);
            Assert.Equal(13.92, leer.Q_max, 9);
            SimulationPufferspeicher vertauscht = Speicher(0, 12, 6);
            Assert.True(vertauscht.KaeltepaarVorgabe);
            Assert.Equal(SimulationPufferspeicher.KAELTE_VORLAUF_VORGABE, vertauscht.KaltVorlauf);

            // 8/14 °C: Spreizung 6 K wie oben; 7/12 °C: 5 K.
            Assert.Equal(2000 * 1.16 * 5 / 1000.0, Speicher(0, 7, 12).Q_max, 9);
        }

        [Fact]
        public void Laedt_aus_freier_Leistung_und_entlaedt_vor_der_Maschine()
        {
            Kaelteerzeuger km = Luftgekuehlt();
            SimulationPufferspeicher sp = Speicher();
            var k = new Kaeltekaskade { Erzeuger = { km }, Speicher = { sp }, Kuehltage = Immer };
            // Stunde 0: kein Bedarf - der leere Speicher lädt bis 95 %. Stunde 3: 60 kWh, die Maschine kann 50.
            k.Rechnen(Bedarf((3, 60.0)), false);

            double ziel = 0.95 * 13.92;
            Assert.Equal(ziel, k.Speicherladung_stuendlich[0], 9);
            Assert.Equal(ziel, km.Kaelte_stuendlich[0], 9);        // die Ladung ist Erzeugerkälte …
            Assert.Equal(0.0, k.Deckung_stuendlich[0], 9);         // … aber keine Deckung
            Assert.True(km.Strom_stuendlich[0] > 0);               // und sie kostet Strom
            Assert.Equal(0.0, k.Speicherladung_stuendlich[1], 9);  // voll: keine Ladephase mehr

            // Stunde 3: Der Speicher entlädt zuerst, die Maschine deckt den Rest - nichts bleibt offen.
            Assert.Equal(ziel, k.Speicherentladung_stuendlich[3], 9);
            Assert.Equal(60.0 - ziel, km.Kaelte_stuendlich[3], 9);
            Assert.Equal(0.0, k.Rest_stuendlich[3], 9);
            Assert.Equal(60.0, k.Deckung_stuendlich[3], 9);

            // Stunde 4: leer - die Ladephase füllt ihn wieder auf 95 %.
            Assert.Equal(ziel, k.Speicherladung_stuendlich[4], 9);
            Assert.Equal(2 * ziel, sp.Ladung_gesamt, 9);
            Assert.Equal(ziel, sp.Entladung_gesamt, 9);
            Assert.Equal(ziel, sp.Entladung_Kanal[Kanal.KUEHLUNG], 9);
            Assert.Equal(1.9, sp.Vollzyklen, 9);
            Assert.Equal(ziel, sp.SOC, 9);
            Assert.Null(sp.T_oben_Mittel);
            Assert.True(Kaeltekaskade.Deckungsprobe(k, null, k.Bedarf_stuendlich, null, 0).Ok);

            // Gegenprobe ohne Speicher: 10 kWh bleiben offen.
            Kaelteerzeuger allein = Luftgekuehlt();
            var ohne = new Kaeltekaskade { Erzeuger = { allein } };
            ohne.Rechnen(Bedarf((3, 60.0)), false);
            Assert.Equal(10.0, ohne.Rest_stuendlich[3], 9);
            Assert.Equal(0.0, ohne.SpeicherladungKwh);
        }

        [Fact]
        public void Ladung_nur_aus_freier_Kapazitaet()
        {
            Kaelteerzeuger km = Luftgekuehlt();
            SimulationPufferspeicher sp = Speicher();
            var k = new Kaeltekaskade { Erzeuger = { km }, Speicher = { sp }, Kuehltage = Immer };
            // Stunde 0: 50 kWh - die Maschine ist voll ausgelastet, nichts für den Speicher. Stunde 1: 45 kWh - 5 kWh frei.
            k.Rechnen(Bedarf((0, 50.0), (1, 45.0)), false);

            Assert.Equal(0.0, k.Speicherladung_stuendlich[0], 9);
            Assert.Equal(0.0, k.Rest_stuendlich[0], 9);
            Assert.Equal(5.0, k.Speicherladung_stuendlich[1], 9);
            Assert.Equal(50.0, km.Kaelte_stuendlich[1], 9);
            Assert.Equal(45.0, k.Deckung_stuendlich[1], 9);
            Assert.Equal(0.0, k.Speicherentladung_stuendlich[1], 9);   // leer: nichts zu entladen
            Assert.True(Kaeltekaskade.Deckungsprobe(k, null, k.Bedarf_stuendlich, null, 0).Ok);
        }

        [Fact]
        public void Verlust_ist_Erwaermung_und_die_Bilanz_geht_auf()
        {
            Kaelteerzeuger km = Luftgekuehlt();
            SimulationPufferspeicher sp = Speicher(24.0);   // 1 kWh je Stunde bei vollem Speicher
            var k = new Kaeltekaskade { Erzeuger = { km }, Speicher = { sp }, Kuehltage = Immer };
            k.Rechnen(Bedarf((5, 3.0)), false);

            // Stunde 0: geladen auf 95 %, dann 0,95 kWh Wärmeeintrag.
            Assert.Equal(0.95 * 13.92 - 0.95, sp.SOC_stuendlich[0], 9);
            Assert.True(sp.SOC_stuendlich[2] < sp.SOC_stuendlich[1]);   // der Vorrat zehrt
            Assert.True(sp.Verluste_gesamt > 0);
            Assert.Equal(sp.Ladung_gesamt - sp.Entladung_gesamt - sp.Verluste_gesamt, sp.SOC, 6);
            // Die Hysterese lädt nach, sobald die Einschaltschwelle erreicht ist.
            Assert.True(sp.Vollzyklen > 1.0);
            Assert.True(Kaeltekaskade.Deckungsprobe(k, null, k.Bedarf_stuendlich, null, 0).Ok);
        }

        [Fact]
        public void Geladen_wird_nur_an_Kuehltagen()
        {
            Kaelteerzeuger km = Luftgekuehlt();
            SimulationPufferspeicher sp = Speicher();
            var tage = new bool[Kaeltekaskade.TAGE];
            tage[1] = true;
            var k = new Kaeltekaskade { Erzeuger = { km }, Speicher = { sp }, Kuehltage = tage };
            k.Rechnen(Bedarf(), false);

            Assert.Equal(0.0, k.Speicherladung_stuendlich.Take(24).Sum(), 9);
            Assert.Equal(0.95 * 13.92, k.Speicherladung_stuendlich[24], 9);
            Assert.Equal(0.95 * 13.92, k.SpeicherladungKwh, 9);
        }

        [Fact]
        public void Klassen_Set_und_Verwendung_Kaelte()
        {
            PufferSpCtrl.KlassenSet set = PufferSpCtrl.KlassenSetAusVerwendung(DbWerte.PSP_VERWENDUNG_KAELTE);
            Assert.True(set.Kaelte);
            Assert.False(set.Heizung || set.Brauchwasser || set.Prozess);
            Assert.False(set.Leer);
            Assert.Equal(DbWerte.PSP_VERWENDUNG_KAELTE, set.Verwendung);
            Assert.True(PufferSpCtrl.KlassenSetBestimmen(DbWerte.PSP_VERWENDUNG_KAELTE, false, false, false).Kaelte);
            Assert.False(new PufferSpCtrl.KlassenSet(true, false, false, true).Heizung);
            Assert.Equal(DbWerte.PSP_VERWENDUNG_KAELTE, WaermesenkeClass.NormalisierteVerwendung("Kälte"));
            Assert.Equal(DbWerte.PSP_VERWENDUNG_KAELTE, WaermesenkeClass.NormalisierteVerwendung("kaelte"));
        }
    }
}
