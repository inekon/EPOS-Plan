using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>gbXML <c>SollHeizenC</c> aus dem Heizkalender</b> (Entwurf KP2, Welle K1, Festlegung 9;
    /// Teilkonzept Konditionierungsprofile 5.5): Trägt die Zone bzw. das Gebäude einen angelegten
    /// Heizkalender — Zone vor Gebäude, wie der Lauf —, schreibt der Export den häufigsten endlichen
    /// Wert der Standardwoche Montag bis Freitag außerhalb der Nachtzeit des Gebäudes (E55), bei
    /// Gleichstand den höheren; Perioden bleiben außen vor. Die Verlustliste nennt „Kalender". Ohne
    /// Kalender wörtlich der Bestand.
    ///
    /// <para>Gelesen wird über <see cref="GebaeudeExportSatz.Lesen"/> an einer Arbeitskopie der
    /// Testdatenbank (Projekt 1039, Gebäude 10643, die Zone wird im Test angelegt). Keine Kultur
    /// gepinnt: geprüft werden Zahlen und Feldnamen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeExportKalenderTests : IDisposable
    {
        private const int PROJEKT = 1039;
        private const int GEBAEUDE = 10643;

        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        // ==================================================================
        //  Der Datenbankweg: Lesen → Vorbereiten
        // ==================================================================

        [Fact]
        public void Datenbank_Der_Heizkalender_von_Gebaeude_und_Zone_bestimmt_den_Sollwert_der_Datei()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;
            int zone = ZoneAnlegen();
            var ctrl = new KonditionierungCtrl();

            // Ohne Kalender: wörtlich der Bestand (Tagessollwert des Gebäudes 20 °C), kein Verlust „Kalender".
            GebaeudeExportPlan ohne = Plan();
            Assert.Equal(20.0, ohne.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
            Assert.DoesNotContain("Kalender", Verluste(ohne));

            // Heizkalender des Gebäudes: Mo–Fr 7–17 Uhr 21 °C, 6 und 17–22 Uhr 19 °C, sonst 16 °C.
            Assert.True(ctrl.Schreiben(KonditionierungCtrl.Eigner.Gebaeude(GEBAEUDE), Buerowoche(21.0, 19.0, 16.0)).Ok);
            GebaeudeExportPlan gebaeude = Plan();
            Assert.Equal(21.0, gebaeude.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
            Assert.Contains("Kalender", Verluste(gebaeude));

            // Ein Kalender der Zone geht vor.
            Assert.True(ctrl.Schreiben(KonditionierungCtrl.Eigner.Zone(GEBAEUDE, zone), Buerowoche(23.0, 19.0, 16.0)).Ok);
            GebaeudeExportPlan zonenplan = Plan();
            Assert.Equal(23.0, zonenplan.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
        }

        // ==================================================================
        //  Der Ablauf ohne Datenbank
        // ==================================================================

        /// <summary>
        /// Mit Heizkalender des Gebäudes: 21 °C in 50 der 80 Nutzungsstunden Mo–Fr, 19 °C in 30. Über die
        /// ganze Woche wäre 16 °C der häufigste Wert (Nacht und Wochenende), die Ferienperiode trägt 30 °C —
        /// beide zählen nicht. Die Verlustliste nennt „Kalender".
        /// </summary>
        [Fact]
        public void Mit_Heizkalender_ist_der_Sollwert_der_haeufigste_Wert_der_Nutzungsstunden()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(MitKalender(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()),
                                                                       Buerowoche(21.0, 19.0, 16.0)));
            Assert.Equal(21.0, plan.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
            Assert.Contains(GebaeudeExportVerluste.KALENDER, Verluste(plan));
        }

        [Fact]
        public void Bei_Gleichstand_gilt_der_hoehere_Wert()
        {
            // Nutzungsstunden 6 … 21 Uhr: acht Stunden je Wert.
            Konditionierungskalender aufsteigend = Woche((t, s) => t < 5 && s >= 6 && s < 14 ? 19.0 : 21.0);
            Konditionierungskalender absteigend = Woche((t, s) => t < 5 && s >= 6 && s < 14 ? 22.0 : 18.0);
            Assert.Equal(21.0, GebaeudeExportAblauf.HaeufigsterNutzungswert(aufsteigend, Nachtzeit.Vorgabe));
            Assert.Equal(22.0, GebaeudeExportAblauf.HaeufigsterNutzungswert(absteigend, Nachtzeit.Vorgabe));
        }

        /// <summary>
        /// Ohne Kalender wörtlich der Bestand: der Tagessollwert der Zone, sonst der des Gebäudes — und kein
        /// Verlust „Kalender". Ein Kalender einer anderen Größe (Kühlen) lässt den Heizsollwert beim Bestand,
        /// nennt aber den Verlust: Sein Zeitplan reist nicht mit.
        /// </summary>
        [Fact]
        public void Ohne_Heizkalender_bleibt_der_Bestand()
        {
            ZoneModel eigen = ExportSatzProbe.Schichtenhaus();
            eigen.Raumsolltemperatur_Tag = 22.0;
            GebaeudeExportPlan mitZonenwert = ExportSatzProbe.Plan(ExportSatzProbe.Satz(eigen));
            Assert.Equal(22.0, mitZonenwert.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
            Assert.DoesNotContain(GebaeudeExportVerluste.KALENDER, Verluste(mitZonenwert));

            GebaeudeExportPlan ohne = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()));
            Assert.Equal(20.0, ohne.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
            Assert.DoesNotContain(GebaeudeExportVerluste.KALENDER, Verluste(ohne));

            var kuehlen = new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll, Kalenderangabe.AusWert(26.0), null, null);
            GebaeudeExportPlan nurKuehlen = ExportSatzProbe.Plan(MitKalender(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()),
                                                                             null, (ExportSatzProbe.ZONE, kuehlen)));
            Assert.Equal(20.0, nurKuehlen.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
            Assert.Contains(GebaeudeExportVerluste.KALENDER, Verluste(nurKuehlen));
        }

        /// <summary>
        /// Zone vor Gebäude, wie der Lauf: Die Zone mit eigenem Heizkalender schreibt dessen Wert; die Zone
        /// ohne Kalender den des Gebäudekalenders — auch wenn sie einen eigenen Tagessollwert trägt, denn
        /// der Lauf rechnet sie mit dem Kalender des Gebäudes.
        /// </summary>
        [Fact]
        public void Die_Zone_geht_vor_dem_Gebaeude()
        {
            ZoneModel erste = ExportSatzProbe.Schichtenhaus();
            erste.Nutzflaeche = 80.0;
            var zweite = new ZoneModel
            {
                ID = 602, ID_Gebaeude = ExportSatzProbe.GEB, Rang = 2, Bezeichner = "Büro", Nutzflaeche = 40.0, IstBeheizt = true,
                Raumsolltemperatur_Tag = 18.0,
            };
            GebaeudeExportSatz satz = MitKalender(ExportSatzProbe.Satz(erste, weitere: new[] { zweite }),
                                                  Buerowoche(21.0, 19.0, 16.0), (ExportSatzProbe.ZONE, Buerowoche(23.0, 19.0, 16.0)));

            List<AbbildRaum> raeume = ExportSatzProbe.Plan(satz).Abbild.Gebaeude[0].Raeume;
            Assert.Equal(23.0, raeume.Single(r => r.Kennung == GebaeudeExportKennung.Raum(ExportSatzProbe.ZONE)).SollHeizenC);
            Assert.Equal(21.0, raeume.Single(r => r.Kennung == GebaeudeExportKennung.Raum(602)).SollHeizenC);
        }

        /// <summary>
        /// Die Nachtzeit des Gebäudes (E55) grenzt die Nutzungsstunden ab: Mo–Fr 6–13 Uhr 21 °C (sieben
        /// Stunden), 13–22 Uhr 19 °C (neun). Mit 22–6 Uhr gilt 19 °C, mit 18–6 Uhr 21 °C; ein halbes Paar
        /// nimmt die Vorgabe 22–6 Uhr.
        /// </summary>
        [Fact]
        public void Die_Nachtzeit_des_Gebaeudes_grenzt_die_Nutzungsstunden_ab()
        {
            Konditionierungskalender k = Woche((t, s) => t >= 5 || s < 6 || s >= 22 ? 16.0 : s < 13 ? 21.0 : 19.0);
            Assert.Equal(19.0, GebaeudeExportAblauf.HaeufigsterNutzungswert(k, Nachtzeit.Vorgabe));
            Assert.Equal(21.0, GebaeudeExportAblauf.HaeufigsterNutzungswert(k, Nachtzeit.Aus(18, 6)));

            ProjektGebaeudeModel g = ExportSatzProbe.Gebaeude();
            g.Nachtabsenkung_Beginn = 18;
            g.Nachtabsenkung_Ende = 6;
            GebaeudeExportSatz satz = MitKalender(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus(), gebaeude: g), k);
            Assert.Equal(21.0, ExportSatzProbe.Plan(satz).Abbild.Gebaeude[0].Raeume[0].SollHeizenC);

            g.Nachtabsenkung_Ende = null;
            Assert.Equal("22–6", GebaeudeExportAblauf.NachtzeitDesGebaeudes(g).ToString());
            Assert.Equal(19.0, ExportSatzProbe.Plan(satz).Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
        }

        /// <summary>
        /// „aus" zählt nicht, auch wenn es die meisten Nutzungsstunden trägt; ohne eine endliche
        /// Nutzungsstunde bleibt der Bestand. Ein Kalender ohne Standardwoche zählt seine Grundangabe.
        /// </summary>
        [Fact]
        public void Aus_zaehlt_nicht_und_ohne_endliche_Nutzungsstunde_gilt_der_Bestand()
        {
            Konditionierungskalender meistAus = Woche((t, s) => t < 5 && s >= 16 && s < 22 ? 18.0 : double.NaN);
            Assert.Equal(18.0, GebaeudeExportAblauf.HaeufigsterNutzungswert(meistAus, Nachtzeit.Vorgabe));

            Konditionierungskalender nurNachts = Woche((t, s) => t < 5 && (s < 6 || s >= 22) ? 17.0 : double.NaN);
            Assert.Null(GebaeudeExportAblauf.HaeufigsterNutzungswert(nurNachts, Nachtzeit.Vorgabe));
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(MitKalender(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), nurNachts));
            Assert.Equal(20.0, plan.Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
            Assert.Contains(GebaeudeExportVerluste.KALENDER, Verluste(plan));

            var konstant = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(19.5), null, null);
            Assert.Equal(19.5, GebaeudeExportAblauf.HaeufigsterNutzungswert(konstant, Nachtzeit.Vorgabe));
            var abgeschaltet = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.Abgeschaltet, null, null);
            Assert.Null(GebaeudeExportAblauf.HaeufigsterNutzungswert(abgeschaltet, Nachtzeit.Vorgabe));
        }

        /// <summary>Der Exportdialog bildet den Plan zu jeder Postleitzahl neu — die Kalender reisen mit (<see cref="GebaeudeExportSatz.MitPlz"/>).</summary>
        [Fact]
        public void MitPlz_behaelt_die_Kalender()
        {
            GebaeudeExportSatz satz = MitKalender(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), Buerowoche(21.0, 19.0, 16.0),
                                                  (ExportSatzProbe.ZONE, Buerowoche(23.0, 19.0, 16.0)));
            GebaeudeExportSatz neu = satz.MitPlz("12345");
            Assert.Same(satz.Gebaeudekalender, neu.Gebaeudekalender);
            Assert.Same(satz.Zonenkalender, neu.Zonenkalender);
            Assert.Equal(23.0, ExportSatzProbe.Plan(neu).Abbild.Gebaeude[0].Raeume[0].SollHeizenC);
        }

        // ==================================================================
        //  Handwerkszeug
        // ==================================================================

        /// <summary>Derselbe Probensatz mit angelegten Kalendern des Gebäudes und der Zonen.</summary>
        private static GebaeudeExportSatz MitKalender(GebaeudeExportSatz s, Konditionierungskalender gebaeude,
                                                      params (int Zone, Konditionierungskalender Kalender)[] zonen)
        {
            var g = new Dictionary<Konditionierungsgroesse, Konditionierungskalender>();
            if (gebaeude != null) g[gebaeude.Groesse] = gebaeude;
            var z = new Dictionary<int, IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender>>();
            foreach ((int zone, Konditionierungskalender k) in zonen)
                z[zone] = new Dictionary<Konditionierungsgroesse, Konditionierungskalender> { [k.Groesse] = k };
            return new GebaeudeExportSatz
            {
                IdProjekt = s.IdProjekt, IdZ = s.IdZ, Gebaeude = s.Gebaeude, Zonen = s.Zonen, Uebernahme = s.Uebernahme,
                Uebernahmeprotokoll = s.Uebernahmeprotokoll, UebernahmeLaufzeitMs = s.UebernahmeLaufzeitMs,
                Aufbauten = s.Aufbauten, Baustoffe = s.Baustoffe, Kuehlbetrieb = s.Kuehlbetrieb,
                Klimaregion = s.Klimaregion, Plz = s.Plz, Gebaeudekalender = g, Zonenkalender = z,
            };
        }

        /// <summary>Ein Heizkalender mit der Woche <paramref name="wert"/>(Wochentag 0 … 6, Stunde 0 … 23); NaN = „aus".</summary>
        private static Konditionierungskalender Woche(Func<int, int, double> wert)
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++)
                    woche[Kalenderwoche.Stelle(t, s)] = wert(t, s);
            return new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(woche), null, null);
        }

        private static GebaeudeExportPlan Plan()
        {
            GebaeudeExportSatz satz = GebaeudeExportSatz.Lesen(PROJEKT, GEBAEUDE, null);
            Assert.False(satz.Klassenweg);
            return ExportSatzProbe.Plan(satz);
        }

        /// <summary>Die Feldnamen der Meldung „benannte Verluste".</summary>
        private static IReadOnlyList<string> Verluste(GebaeudeExportPlan plan)
        {
            PruefMeldung m = plan.Meldungen.SingleOrDefault(x => x.Schluessel == GebaeudeExportAblauf.VERLUSTE);
            return m == null ? Array.Empty<string>() : m.Werte[0].Split(new[] { ", " }, StringSplitOptions.None);
        }

        /// <summary>Legt die eine Zone des Gebäudes 10643 an: nur U-Werte, sieben Hüllflächen.</summary>
        private static int ZoneAnlegen()
        {
            BauteilModel B(int id, string art, double a, string rand, double u, double neigung, double? azimut = null)
                => new BauteilModel
                {
                    ID = id, Bezeichner = "Bauteil " + (-id).ToString(CultureInfo.InvariantCulture), Bauteilart = art,
                    Flaeche = a, Randbedingung = rand, U_Wert = u, Neigung = neigung, Azimut = azimut,
                };
            var z = new ZoneModel
            {
                ID = -1, Bezeichner = "Kalenderprobe", Nutzflaeche = 120.0, IstBeheizt = true,
                Bauteile =
                {
                    B(-1, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.3, 90.0, 0.0),
                    B(-2, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.3, 90.0, 90.0),
                    B(-3, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.3, 90.0, 180.0),
                    B(-4, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.3, 90.0, 270.0),
                    B(-5, DbWerte.BAUTEILART_FENSTER, 6.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 1.3, 90.0, 180.0),
                    B(-6, DbWerte.BAUTEILART_DACH, 120.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, 0.2, 0.0),
                    B(-7, DbWerte.BAUTEILART_BODENPLATTE, 120.0, DbWerte.RANDBEDINGUNG_ERDREICH, 0.35, 180.0),
                },
            };
            GebaeudeZonenCtrl.Ergebnis e = new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { z });
            Assert.True(e.Ok, e.Meldung);
            return Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(GEBAEUDE)).ID;
        }

        /// <summary>
        /// Eine Heizwoche: Mo–Fr 7–17 Uhr <paramref name="kern"/> (zehn Stunden), um 6 Uhr und 17–22 Uhr
        /// <paramref name="rand"/> (sechs Stunden), sonst — Nacht und Wochenende — <paramref name="sonst"/>;
        /// dazu eine Ferienperiode mit 30 °C, die der Export nicht ansieht.
        /// </summary>
        internal static Konditionierungskalender Buerowoche(double kern, double rand, double sonst)
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++)
                {
                    double v = sonst;
                    if (t < 5 && s >= 7 && s < 17) v = kern;
                    else if (t < 5 && (s == 6 || (s >= 17 && s < 22))) v = rand;
                    woche[Kalenderwoche.Stelle(t, s)] = v;
                }
            return new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(woche), null,
                new[]
                {
                    Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN, "Ferien 1", 180, 200,
                                           Kalenderangabe.AusWert(30.0)),
                });
        }
    }
}
