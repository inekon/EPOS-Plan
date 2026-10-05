using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 35 — Konditionierung</b> (Datenaustauschkonzept 16.7): Heizsollwert, Lüftung, Geräte und Personen aus
    /// Ganglinie und Nutzungsprofil ergeben die erwarteten 168 Zellen, Nennwerte und Perioden; die Tagesart „Werktage“ lässt
    /// das Wochenende „aus“ bzw. auf dem Nachtwert; Werte außerhalb der Grenzen sind benannt begrenzt; Rangfolge Ganglinie
    /// vor Profil vor Vorlage.
    /// </summary>
    public sealed class SqprojKonditionierungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _pfad;
        private readonly SqprojAbbild _projekt;

        public SqprojKonditionierungTests()
        {
            _pfad = SqprojProbenErzeuger.Standard().Schreiben(SqprojProbenErzeuger.TempPfad("kond"));
            _projekt = SqprojLeser.Lesen(_pfad);
        }

        public void Dispose()
        {
            _kultur.Dispose();
            try { File.Delete(_pfad); } catch (IOException) { }
        }

        private SqprojZone Zone(string name) => _projekt.Zonen.Single(z => z.Name == name);

        /// <summary>Die Simulationszone EG mit dem Profil der Nutzungszone, 50 m², 150 m³.</summary>
        private Zonenkonditionierung SimulationEg()
        {
            SqprojZone z = Zone("Simulation EG");
            return SqprojKonditionierung.Bilden(z.Name, DbWerte.KOND_NUTZUNG_BUERO, SqprojZonen.GeteiltesProfil(z, _projekt), z.Gruppe, 50.0, 150.0);
        }

        private static double W(Konditionierungskalender k, int tag, int stunde) => k.Standardwoche[Kalenderwoche.Stelle(tag, stunde)];

        [Fact]
        public void Heizsollwert_aus_der_Ganglinie_mit_Nachtwert_am_Wochenende_und_Begrenzung()
        {
            Zonenkonditionierung k = SimulationEg();
            Groessenkonditionierung h = k.Groesse(Konditionierungsgroesse.Heizsoll);
            Assert.Equal(Konditionierungsherkunft.Ganglinie, h.Herkunft);
            Assert.Equal(168, h.Kalender.Standardwoche.Count);
            Assert.Equal(17.0, W(h.Kalender, 0, 3));
            Assert.Equal(21.0, W(h.Kalender, 0, 7));
            Assert.Equal(Konditionierungsgroessen.Max(Konditionierungsgroesse.Heizsoll), W(h.Kalender, 4, 12));   // 40 °C begrenzt
            Assert.Equal(17.0, W(h.Kalender, 5, 10));   // Samstag: Nachtwert
            Assert.Equal(17.0, W(h.Kalender, 6, 12));
            Assert.Empty(h.Kalender.Perioden);          // Ganzjahresabschnitt ohne Schalter: keine Periode
            Assert.Equal(1, h.Begrenzt);
            Assert.Contains(k.Meldungen, m => m.Schluessel == SqprojProtokoll.WERT_BEGRENZT && m.Werte[1] == DbWerte.KOND_GROESSE_HEIZSOLL && m.Werte[2] == "1");
            Assert.Contains("PdProfileTimeCurve.Temperature", h.Bemerkung);
            Assert.True(h.Bemerkung.Length <= KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN);
            // Das Nutzungsprofil steht als Vorgabe darunter: Tag 21, Nacht 17 von 18 bis 7 Uhr.
            Assert.Equal(21.0, h.Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert);
            Matrixzelle nacht = h.Vorgabe(DbWerte.KOND_ZEILE_NACHT);
            Assert.Equal(17.0, nacht.Wert);
            Assert.Equal(18, nacht.Von);
            Assert.Equal(7, nacht.Bis);
            Assert.Equal(21.0, k.HeizsollTag);
        }

        [Fact]
        public void Personen_mit_Nennwert_Wochenende_aus_und_Periode_mit_eigener_Woche()
        {
            Groessenkonditionierung p = SimulationEg().Groesse(Konditionierungsgroesse.Personen);
            Assert.Equal(Konditionierungsherkunft.Ganglinie, p.Herkunft);
            Assert.Equal(320.0, p.Kalender.Nennwert);    // 4 Personen × 80 W
            Assert.Equal(1.0, W(p.Kalender, 0, 9));
            Assert.Equal(0.0, W(p.Kalender, 0, 20));
            Assert.True(double.IsNaN(W(p.Kalender, 5, 9)));   // Samstag „aus“
            Kalenderregel r = Assert.Single(p.Kalender.Perioden);
            Assert.Equal(SqprojKonditionierung.RANG_ABSCHNITT + 1, r.Rang);
            Assert.Equal(DbWerte.KOND_ART_ZEITRAUM, r.Art);
            Assert.Equal((182, 243), (r.Beginn, r.Ende));
            Assert.Equal(1.0, r.Angabe.Woche[Kalenderwoche.Stelle(1, 9)]);           // Dienstag gewählt
            Assert.True(double.IsNaN(r.Angabe.Woche[Kalenderwoche.Stelle(2, 9)]));  // Mittwoch nicht
            Assert.Equal(250.0, p.Vorgabe(DbWerte.KOND_ZEILE_NENNWERT).Wert);       // 5 W/m² × 50 m²
            Assert.Equal(0.5, p.Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert);              // 5,5 h von 11 h
        }

        [Fact]
        public void Sechs_Tage_Woche_mit_Nachtwert_aus_den_Stunden_ausserhalb_der_Nutzungszeit()
        {
            SqprojZone z = Zone("Simulation OG");
            Zonenkonditionierung k = SqprojKonditionierung.Bilden(z.Name, DbWerte.KOND_NUTZUNG_WOHNEN, SqprojZonen.GeteiltesProfil(z, _projekt), z.Gruppe, 50.0, 150.0);
            Konditionierungskalender h = k.Groesse(Konditionierungsgroesse.Heizsoll).Kalender;
            Assert.Equal(20.0, W(h, 5, 10));    // Samstag: Arbeitstag (Tagesart 5 → Montag–Samstag, angenommen)
            Assert.Equal(18.0, W(h, 6, 10));    // Sonntag: Nachtwert aus den Stunden der Betriebsart 2, nicht 16
            Assert.Equal(16.0, W(h, 0, 15));    // gerechnet wird mit den Werten der Kurve
            Assert.Equal(20.0, k.HeizsollTag);
            Assert.Contains("71", k.Groesse(Konditionierungsgroesse.Heizsoll).Bemerkung);
            Assert.Contains("6", k.Groesse(Konditionierungsgroesse.Heizsoll).Bemerkung.Split(';').Last());   // Annahme im Beleg
        }

        [Fact]
        public void Lueftung_ohne_Ganzjahresabschnitt_ist_aus_ausser_in_der_Periode()
        {
            Groessenkonditionierung l = SimulationEg().Groesse(Konditionierungsgroesse.Lueftung);
            Assert.Equal(Angabeart.Aus, l.Kalender.Grundangabe.Art);
            Kalenderregel r = Assert.Single(l.Kalender.Perioden);
            Assert.True(r.UeberJahreswechsel);
            Assert.Equal(2.0, r.Angabe.Woche[0]);
            Assert.Equal(200.0 / 150.0, l.Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert, 10);   // 4 m³/(h·m²) × 50 m² / 150 m³
        }

        [Fact]
        public void Geraete_Nennwert_aus_Ganglinie_und_Profil()
        {
            Groessenkonditionierung g = SimulationEg().Groesse(Konditionierungsgroesse.Geraete);
            Assert.Equal(500.0, g.Kalender.Nennwert);   // 10 W/m² × 50 m²
            Assert.Equal(0.5, W(g.Kalender, 2, 2));
            Assert.True(double.IsNaN(W(g.Kalender, 6, 2)));
            Assert.Equal(350.0, g.Vorgabe(DbWerte.KOND_ZEILE_NENNWERT).Wert);
            Assert.Equal(1.0, g.Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert);
        }

        [Fact]
        public void Rangfolge_Ganglinie_vor_Profil_vor_Vorlage()
        {
            Zonenkonditionierung sim = SimulationEg();
            Assert.Equal(Konditionierungsherkunft.Vorlage, sim.Groesse(Konditionierungsgroesse.Kuehlsoll).Herkunft);
            Assert.Null(sim.Groesse(Konditionierungsgroesse.Kuehlsoll).Kalender);
            SqprojZone og = Zone("Nutzung OG");
            Zonenkonditionierung k = SqprojKonditionierung.Bilden(og.Name, DbWerte.KOND_NUTZUNG_WOHNEN, og.Nutzungsprofil, og.Gruppe, 50.0, 150.0);
            Assert.Equal(Konditionierungsherkunft.Nutzungsprofil, k.Groesse(Konditionierungsgroesse.Heizsoll).Herkunft);
            Assert.Null(k.Groesse(Konditionierungsgroesse.Heizsoll).Kalender);
            Assert.Null(k.Groesse(Konditionierungsgroesse.Heizsoll).Vorgabe(DbWerte.KOND_ZEILE_NACHT));   // ohne Absenkung
            Assert.Equal(Konditionierungsherkunft.Vorlage, k.Groesse(Konditionierungsgroesse.Personen).Herkunft);
            Assert.True(k.Liefert);
            Assert.False(SqprojKonditionierung.Bilden("leer", null, null, null, null, null).Liefert);
        }

        [Fact]
        public void Alle_Tage_und_unbekannte_Tagesart_rollen_die_Kurve_auf_sieben_Tage_aus()
        {
            SqprojZeitprofil heizen = Zone("Simulation EG").Gruppe.Profil(SqprojProfilklasse.HEIZEN);
            foreach (SqprojTagesart art in new[] { SqprojTagesart.AlleTage, SqprojTagesart.Unbekannt })
            {
                int begrenzt = 0;
                double[] w = SqprojKonditionierung.Woche(heizen, Konditionierungsgroesse.Heizsoll, art, ref begrenzt);
                Assert.Equal(21.0, w[Kalenderwoche.Stelle(6, 7)]);
                Assert.Equal(1, begrenzt);
            }
        }

        [Fact]
        public void Jeder_Kalender_laesst_sich_als_Woche_schreiben()
        {
            foreach (Groessenkonditionierung g in SimulationEg().Groessen.Where(g => g.Kalender != null))
            {
                if (g.Kalender.Standardwoche != null)
                    Assert.Equal(168, Kalenderwoche.Schreiben(g.Kalender.Standardwoche, g.Groesse).Split(';').Length);
                foreach (Kalenderregel r in g.Kalender.Perioden)
                    Assert.Equal(168, Kalenderwoche.Schreiben(r.Angabe.Woche, g.Groesse).Split(';').Length);
            }
        }
    }
}
