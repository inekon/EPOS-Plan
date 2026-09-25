using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Gebäude als eine Zone übernehmen" mit Hochrechnung</b> (Stufe G3, Welle D2;
    /// Anwenderentscheid vom 25.09.2026 „Hochrechnen“) — ohne Datenbank: Die Übernahme rechnet
    /// Flächen und ψ·L mit dem Faktor hoch und gibt der Zone die hochgerechnete Nutzfläche; der
    /// Flächenschlüssel legt Luftvolumen, Speichermasse der Bauweise, innere Gewinne und f_IW·A_f
    /// auf die Zonenfläche; die hochgerechnete Zone rechnet dieselbe Reihe wie der Klassenweg mal
    /// Faktor; Faktor 1 bleibt der Grenzfall, bitgleich; die Prüfregeln des Bauteildialogs
    /// (Mehrzonenkonzept 5.3) stehen einmal im Kern.
    /// </summary>
    public class GebaeudeHochrechnungTests : IDisposable
    {
        private readonly ITestOutputHelper _aus;

        /// <summary>Die Prüfregeln halten deutsche Ressourcentexte („Null“) gegen <c>Contains</c> —
        /// ohne Pinnung wären sie auf dem Windows-Läufer (en-US) rot.</summary>
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public GebaeudeHochrechnungTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, string stufe = null)
            => GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, stufe);

        private static ProjektGebaeudeModel MitZone(ProjektGebaeudeModel g, GebaeudeZonensatz zone)
            => BauteilwegLaufProbe.MitZone(g, zone);

        /// <summary>Die Reihe des Laufs in W, unskaliert.</summary>
        private static double[] Reihe(GebaeudeModellEingang e, int id) => Vdi6007Rechenweg.Laufen(e, 0, id).HeizlastW;

        /// <summary>Gleich bis auf die Reihenfolge der Summen: je Stunde relativ zur Spitze ≤ 1e-9, die Summe relativ ≤ 1e-9.</summary>
        internal static void GleicheReihe(double[] erwartet, double[] ist, string fall, ITestOutputHelper aus)
        {
            double spitze = Math.Max(1.0, erwartet.Max(Math.Abs));
            double groesste = 0.0;
            for (int h = 0; h < 8760; h++) groesste = Math.Max(groesste, Math.Abs(ist[h] - erwartet[h]));
            double sa = erwartet.Sum(), sb = ist.Sum();
            aus?.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: Σ erwartet {1:R}, Σ ist {2:R}, relativ {3:E2}; größte Stundenabweichung {4:E2} bei Spitze {5:F1}",
                fall, sa, sb, Math.Abs(sb - sa) / Math.Max(1.0, Math.Abs(sa)), groesste, spitze));
            Assert.True(groesste <= 1e-9 * spitze, fall + ": größte Stundenabweichung " + groesste.ToString("E3", CultureInfo.InvariantCulture));
            Assert.True(Math.Abs(sb - sa) <= 1e-9 * Math.Max(1.0, Math.Abs(sa)), fall + ": Jahressumme");
        }

        // =====================================================================
        //  Die Übernahme mit Faktor
        // =====================================================================

        /// <summary>
        /// Jede Bauteilfläche und jedes ψ·L tragen den Faktor, die Zone die Nutzfläche Faktor ×
        /// Nutzfläche; Art, U-Wert, Neigung, Azimut und Randbedingung bleiben, wie sie sind.
        /// </summary>
        [Fact]
        public void Die_Uebernahme_rechnet_Flaechen_und_Waermebruecken_mit_dem_Faktor_hoch()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            const double faktor = 340.0 / 74.0;
            GebaeudeZonensatz eins = GebaeudeZonenuebernahme.AlsEineZone(g);
            GebaeudeZonensatz hoch = GebaeudeZonenuebernahme.AlsEineZone(g, faktor);

            Assert.Equal(g.Nutzflaeche, eins.Nutzflaeche_M2);
            Assert.Equal(faktor * g.Nutzflaeche, hoch.Nutzflaeche_M2);
            Assert.Equal(eins.Bauteile.Count, hoch.Bauteile.Count);
            for (int i = 0; i < eins.Bauteile.Count; i++)
            {
                BauteilEingang a = eins.Bauteile[i], b = hoch.Bauteile[i];
                Assert.Equal(a.Bezeichnung, b.Bezeichnung);
                Assert.Equal(a.Art, b.Art);
                Assert.Equal(a.Rand, b.Rand);
                Assert.Equal(a.UWert_WM2K, b.UWert_WM2K);
                Assert.Equal(a.AzimutGrad, b.AzimutGrad);
                Assert.Equal(a.NeigungGrad, b.NeigungGrad);
                Assert.Equal(faktor * a.Flaeche_M2, b.Flaeche_M2, 12);
                Assert.Equal(faktor * a.PsiL_WK, b.PsiL_WK, 12);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeZonenuebernahme.AlsEineZone(g, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeZonenuebernahme.AlsEineZone(g, double.NaN));
        }

        /// <summary>
        /// <b>Faktor 1 bleibt der Grenzfall, bitgleich:</b> Die übernommene Zone trägt die Nutzfläche
        /// des Gebäudes ausdrücklich; der Flächenschlüssel ist dann genau 1 und rechnet nichts um —
        /// Parametersatz und Reihe sind dieselben wie mit einer Zone ohne eigene Nutzfläche.
        /// </summary>
        [Fact]
        public void Faktor_eins_rechnet_bitgleich_wie_eine_Zone_ohne_eigene_Nutzflaeche()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Sommerlueftung = true;
            GebaeudeZonensatz uebernommen = GebaeudeZonenuebernahme.AlsEineZone(g);
            GebaeudeModellEingang mit = Eingang(MitZone(g, uebernommen));
            Assert.Equal(g.Nutzflaeche, uebernommen.Nutzflaeche_M2);
            ProjektGebaeudeModel ohneFlaeche = Vdi6007Probe.Gebaeude();
            ohneFlaeche.Sommerlueftung = true;
            GebaeudeModellEingang ohne = Eingang(MitZone(ohneFlaeche, new GebaeudeZonensatz(0, "Gebäude", uebernommen.Bauteile)));

            Assert.Equal(1.0, mit.Flaechenanteil);
            Assert.Equal(ohne.Nutzflaeche_M2, mit.Nutzflaeche_M2);
            Assert.Equal(ohne.Bauweise_WhK, mit.Bauweise_WhK);
            Assert.Equal(ohne.InnereGewinne_W, mit.InnereGewinne_W);
            Assert.Equal(ohne.Parameter.C_AW_Jk, mit.Parameter.C_AW_Jk);
            Assert.Equal(ohne.Parameter.R_ext_KW, mit.Parameter.R_ext_KW);
            double[] a = Reihe(ohne, g.ID_Gebaeude), b = Reihe(mit, g.ID_Gebaeude);
            for (int h = 0; h < 8760; h++) Assert.True(a[h].Equals(b[h]), "Stunde " + h);
        }

        /// <summary>
        /// <b>Der Flächenschlüssel</b> (Mehrzonenkonzept 4.2): Mit der doppelten Nutzfläche an der
        /// Zone verdoppeln sich Luftvolumen (H_ve, Zusatzleitwert der Sommerlüftung), Speichermasse
        /// der Bauweise, innere Gewinne (absolut in W geführt) und f_IW·A_f; Raumhöhe,
        /// Luftwechsel, Sollwerte und die Leistungsgrenze bleiben die der Gebäudezeile.
        /// </summary>
        [Fact]
        public void Der_Flaechenschluessel_legt_die_flaechenbezogenen_Groessen_auf_die_Zone()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Sommerlueftung = true;
            g.Heizleistung_Max = 12.0;
            GebaeudeModellEingang klasse = Eingang(g);

            ProjektGebaeudeModel z = Vdi6007Probe.Gebaeude();
            z.Sommerlueftung = true;
            z.Heizleistung_Max = 12.0;
            GebaeudeZonensatz zone = GebaeudeZonenuebernahme.AlsEineZone(z, 2.0);
            GebaeudeModellEingang e = Eingang(MitZone(z, zone));

            Assert.Equal(2.0, e.Flaechenanteil);
            Assert.Equal(2.0 * g.Nutzflaeche, e.Nutzflaeche_M2);
            Assert.Equal(2.0 * g.Bauweise, e.Bauweise_WhK);
            Assert.Equal(2.0 * g.Interne_Waermegewinne, e.InnereGewinne_W);
            Assert.Equal(2.0 * klasse.SommerlueftungZusatzleitwertWK, e.SommerlueftungZusatzleitwertWK, 9);
            Assert.Equal(klasse.Raumhoehe_M, e.Raumhoehe_M);
            Assert.Equal(klasse.Luftwechselrate_h, e.Luftwechselrate_h);
            Assert.Equal(klasse.HeizleistungMaxW, e.HeizleistungMaxW);
            Assert.Equal(klasse.SollTag, e.SollTag);
            // f_IW·A_f und H_ve gehen in den Parametersatz ein: die Innenfläche und der Lüftungszweig doppelt.
            Assert.Equal(2.0 * klasse.Parameter.A_IW_M2, e.Parameter.A_IW_M2, 9);
            Assert.Equal(klasse.Parameter.R_ext_KW / 2.0, e.Parameter.R_ext_KW, 12);

            // Auch die Kühlleistungsgrenze eines gekühlten Gebäudes folgt dem Schlüssel nicht (E40 Punkt 4).
            ProjektGebaeudeModel Gekuehlt()
            {
                ProjektGebaeudeModel x = Vdi6007Probe.Gebaeude();
                x.Kuehlung_Aktiv = true;
                x.Kuehl_Sollwert = 30.0;
                x.Kuehlleistung_Max = 8.0;
                return x;
            }
            GebaeudeModellEingang Kuehlend(ProjektGebaeudeModel x)
                => GebaeudeModellEingang.Bauen(x, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                               GebaeudeKlimaweg.ZEITBEZUG_VORGABE, true, null);
            ProjektGebaeudeModel zk = Gekuehlt();
            GebaeudeModellEingang kk = Kuehlend(Gekuehlt());
            GebaeudeModellEingang ek = Kuehlend(MitZone(zk, GebaeudeZonenuebernahme.AlsEineZone(zk, 2.0)));
            Assert.True(ek.KuehlungWirksam);
            Assert.Equal(2.0, ek.Flaechenanteil);
            Assert.Equal(8000.0, kk.KuehlleistungMaxW);
            Assert.Equal(kk.KuehlleistungMaxW, ek.KuehlleistungMaxW);
        }

        /// <summary>
        /// <b>Abnahme ohne Datenbank:</b> Die hochgerechnete Zone rechnet dieselbe Reihe wie der
        /// Klassenweg des Katalogbaus mal Faktor — relativ ≤ 1e-9 je Stunde und in der Summe, für
        /// einen Faktor größer und einen kleiner als 1, mit Schalter „Strahlung auf Außenbauteile",
        /// Sommerlüftung und der Anlagenkopplung mit hergeleiteter Nennleistung.
        /// </summary>
        [Theory]
        [InlineData("Faktor 4,59", 340.0 / 74.0, false, false)]
        [InlineData("Faktor 0,25", 500.0 / 1975.34, false, false)]
        [InlineData("Schalter+Sommerlüftung", 2.63, true, false)]
        [InlineData("Kopplung", 3.1, false, true)]
        public void Die_hochgerechnete_Zone_rechnet_wie_der_Klassenweg_mal_Faktor(string fall, double faktor, bool schalter, bool kopplung)
        {
            Action<ProjektGebaeudeModel> aendern = x =>
            {
                x.Aussenbauteile_Strahlung = schalter;
                x.Sommerlueftung = schalter;
                if (kopplung) { x.Heizkreis_Aktiv = true; x.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR; x.Heizkurve_Aktiv = true; }
            };
            string stufe = kopplung ? DbWerte.ANLAGENKOPPLUNG_AK1 : null;

            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            aendern(g);
            double[] klasse = Reihe(Eingang(g, stufe), g.ID_Gebaeude).Select(w => w * faktor).ToArray();

            ProjektGebaeudeModel z = Vdi6007Probe.Gebaeude();
            aendern(z);
            double[] zone = Reihe(Eingang(MitZone(z, GebaeudeZonenuebernahme.AlsEineZone(z, faktor)), stufe), z.ID_Gebaeude);
            GleicheReihe(klasse, zone, fall, _aus);
        }

        /// <summary>
        /// Eine Zone ohne Bezugsfläche wird benannt abgelehnt: eine Nutzfläche der Zone von null,
        /// und eine eigene Zonenfläche an einem Gebäude ohne Nutzfläche (kein Schlüssel).
        /// </summary>
        [Fact]
        public void Eine_Zone_ohne_Bezugsflaeche_ist_ein_benannter_Fehler()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz uebernommen = GebaeudeZonenuebernahme.AlsEineZone(g);
            var null0 = new GebaeudeZonensatz(0, "Null", uebernommen.Bauteile, 0.0);
            var ex = Assert.Throws<GebaeudeModellException>(() => Eingang(MitZone(Vdi6007Probe.Gebaeude(), null0)));
            Assert.Equal(GebaeudeModellFehler.PflichtgroesseFehlt, ex.Grund);
            Assert.Contains("„Null“", ex.Message, StringComparison.Ordinal);

            ProjektGebaeudeModel ohneFlaeche = Vdi6007Probe.Gebaeude();
            ohneFlaeche.Nutzflaeche = 0.0;
            var eigene = new GebaeudeZonensatz(0, "Eigene", uebernommen.Bauteile, 150.0);
            ex = Assert.Throws<GebaeudeModellException>(() => Eingang(MitZone(ohneFlaeche, eigene)));
            Assert.Equal(GebaeudeModellFehler.PflichtgroesseFehlt, ex.Grund);
            Assert.Contains("„Eigene“", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>Die Nutzfläche der Zone reist durch die Abbildung: Kern → Zeile → Kern, NaN ↔ NULL.</summary>
        [Fact]
        public void Die_Nutzflaeche_der_Zone_reist_durch_die_Abbildung()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz hoch = GebaeudeZonenuebernahme.AlsEineZone(g, 1.7);
            ZoneModel zeile = GebaeudeZonenabbildung.AlsZoneModel(hoch);
            Assert.Equal(1.7 * g.Nutzflaeche, zeile.Nutzflaeche);
            Assert.Equal(hoch.Nutzflaeche_M2, GebaeudeZonenabbildung.AlsZonensatz(zeile, null).Nutzflaeche_M2);

            ZoneModel ohne = GebaeudeZonenabbildung.AlsZoneModel(new GebaeudeZonensatz(0, "ohne", hoch.Bauteile));
            Assert.Null(ohne.Nutzflaeche);
            Assert.True(double.IsNaN(GebaeudeZonenabbildung.AlsZonensatz(ohne, null).Nutzflaeche_M2));
            Assert.Equal(g.Nutzflaeche, GebaeudeZonenabbildung.AlsZonensatz(ohne, null).Bezugsflaeche(g));
        }

        // =====================================================================
        //  Die Prüfregeln des Bauteildialogs (Mehrzonenkonzept 5.3)
        // =====================================================================

        private static Bauteilangabe Wand(Func<Bauteilangabe, Bauteilangabe> aendern = null)
        {
            var b = new Bauteilangabe("Wand Süd", DbWerte.BAUTEILART_AUSSENWAND, 20.0, 0.28, false,
                                      null, null, null, 180.0, null, null, null);
            return aendern == null ? b : aendern(b);
        }

        [Fact]
        public void Ein_gueltiges_Bauteil_besteht_die_Pruefung()
        {
            using var kultur = new Kulturvorrichtung();
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(Wand()));
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { UWert = null, MitAufbau = true })));
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(new Bauteilangabe("Dach", DbWerte.BAUTEILART_DACH, 80.0, 0.2, false,
                                                                            null, null, null, null, null, null, 4.0)));
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(new Bauteilangabe("Innen", DbWerte.BAUTEILART_INNENWAND, 80.0, null, false,
                                                                            null, null, null, null, null, null, null)));
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(new Bauteilangabe("Fenster", DbWerte.BAUTEILART_FENSTER, 4.0, 1.1, false,
                                                                            0.6, 0.3, 0.9, 90.0, null, null, null)));
        }

        /// <summary>
        /// Jede verletzte Regel ist benannt — mit dem Namen des Bauteils: Fläche, Bänder aus 5.3,
        /// eine Wand an Außenluft ohne Azimut, die Nachbarzone (G6), ein Fenster am Erdreich, ein
        /// Außenbauteil ohne U-Wert und Aufbau.
        /// </summary>
        [Fact]
        public void Jede_verletzte_Regel_wird_benannt()
        {
            using var kultur = new Kulturvorrichtung();
            Assert.Equal(R.BAUTEIL_MSG_NAME_FEHLT, GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Bezeichner = "  " })));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_FLAECHE, "Wand Süd"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Flaeche = 0.0 })));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_FLAECHE, "Wand Süd"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Flaeche = null })));
            Assert.Contains("U = 7", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { UWert = 7.0 })), StringComparison.Ordinal);
            Assert.Contains("U = 0,05", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { UWert = 0.05 })), StringComparison.Ordinal);
            Assert.Contains("g = 1,2", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { GWert = 1.2 })), StringComparison.Ordinal);
            Assert.Contains("1 − F_F = 0,7", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Rahmenanteil = 0.7 })), StringComparison.Ordinal);
            Assert.Contains("1 − F_F = 0,01", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Rahmenanteil = 0.01 })), StringComparison.Ordinal);
            Assert.Contains("F_S = 0", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Verschattung = 0.0 })), StringComparison.Ordinal);
            Assert.Contains("Azimut = 400", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Azimut = 400.0 })), StringComparison.Ordinal);
            Assert.Contains("Neigung = 190", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Neigung = 190.0 })), StringComparison.Ordinal);
            Assert.Contains("ψ·L = -1", GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { PsiL = -1.0 })), StringComparison.Ordinal);

            // Eine Wand an Außenluft ohne Azimut: benannt abgelehnt, nicht auf Nord vorbelegt.
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_AZIMUT_FEHLT, "Wand Süd"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Azimut = null })));
            // ... am Erdreich braucht sie keinen, waagerecht auch nicht.
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Azimut = null, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH })));
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Azimut = null, Neigung = 0.0 })));

            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_RAND_ZONE, "Wand Süd"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Randbedingung = DbWerte.RANDBEDINGUNG_ZONE })));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_RANDBEDINGUNG, "Wand Süd", "KELLER"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { Randbedingung = "KELLER" })));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_FENSTER_RAND, "Fenster"),
                         GebaeudeZonenCtrl.BauteilPruefen(new Bauteilangabe("Fenster", DbWerte.BAUTEILART_FENSTER, 4.0, 1.1, false,
                                                                             null, null, null, 180.0, null, DbWerte.RANDBEDINGUNG_ERDREICH, null)));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_UWERT_FEHLT, "Fenster"),
                         GebaeudeZonenCtrl.BauteilPruefen(new Bauteilangabe("Fenster", DbWerte.BAUTEILART_FENSTER, 4.0, null, true,
                                                                             null, null, null, 180.0, null, null, null)));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_UWERT_FEHLT, "Wand Süd"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wand(b => b with { UWert = null })));
        }

        /// <summary>Die Zonenliste lehnt eine Nutzfläche von null ab, bevor etwas geschrieben ist.</summary>
        [Fact]
        public void Eine_Zone_mit_Nutzflaeche_null_wird_benannt_abgelehnt()
        {
            using var kultur = new Kulturvorrichtung();
            var zone = new ZoneModel { ID = -1, Bezeichner = "Wohnen", Nutzflaeche = 0.0 };
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.ZONE_MSG_NUTZFLAECHE, "Wohnen"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { zone }));
            zone.Nutzflaeche = null;
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { zone }));
        }
    }

    /// <summary>
    /// <b>Abnahme über die Datenbank</b> (Stufe G3, Welle D2): Für Projektgebäude mit Faktor ≠ 1
    /// — Projekt 1007 (Faktor 340/74 ≈ 4,59), 1008 (zwei Gebäude, 2,63 und 1), 1018 (0,25), 1017
    /// (gekühlt, 0,9995) — und für ein Gebäude mit Verbrauchsangabe ist die Heizwärme mit
    /// übernommener, hochgerechneter Zone gleich der des Klassenwegs samt Nachmultiplikation:
    /// Übernahme (<see cref="GebaeudeZonenCtrl.Uebernahme"/>, Faktor aus der Fassade) →
    /// <see cref="GebaeudeZonenCtrl.SpeichernJeGebaeude"/> → Auskunft und Lauf, relativ ≤ 1e-9,
    /// Stundenwerte ebenso. Dazu: Faktor 1 bleibt der Grenzfall, der Arbeitsstand des Dialogs geht
    /// in den Vorschlag ein, und das Projektduplikat hängt die Bauteile an die Zonen der Kopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeHochrechnungDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeHochrechnungDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static void Schreiben(int idGebaeude, ZoneModel zone)
        {
            GebaeudeZonenCtrl.Ergebnis e = new GebaeudeZonenCtrl().SpeichernJeGebaeude(idGebaeude, new List<ZoneModel> { zone });
            Assert.True(e.Ok, e.Meldung);
        }

        private static int TabGebaeude(int idZ)
            => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?",
                                                            new DbParam("@z", idZ)), CultureInfo.InvariantCulture);

        /// <summary>
        /// Übernahme mit Hochrechnung für jedes Gebäude des Projekts, geschrieben und gerechnet:
        /// Auskunft und Lauf mit Zone = Klassenweg samt Nachmultiplikation, relativ ≤ 1e-9.
        /// </summary>
        private void UebernahmeGleichtDemKlassenweg(int projekt, double? erwarteterFaktor = null)
        {
            int region = Klimaregion(projekt);
            var vorher = new SimulationWaermebedarf();
            vorher.Waermebedarf_berechnen(projekt, region);

            foreach (Z_ProjGebModel z in Z_ProjGebCtrl.LiesProjekt(projekt))
            {
                GebaeudeBedarfErgebnis ohne = GebaeudeBedarfCtrl.Rechnen(projekt, region, z.ID_Z);
                Assert.True(ohne.Erfolgreich);
                GebaeudeModellErgebnis ohneErgebnis = null;

                GebaeudeZonenCtrl.Uebernahmevorschlag v = GebaeudeZonenCtrl.Uebernahme(projekt, z.ID_Z, null);
                Assert.True(v.Ok, v.Meldung);
                if (!v.Verbrauchsangabe)
                    Assert.Equal(v.Angabe / v.NutzflaecheGebaeude, v.Faktor, 12);
                if (erwarteterFaktor.HasValue) Assert.Equal(erwarteterFaktor.Value, v.Faktor, 9);
                Assert.Equal(v.Faktor * v.NutzflaecheGebaeude, v.Zone.Nutzflaeche.Value, 9);
                Assert.All(v.Zone.Bauteile, b => Assert.True(b.ID < 0));
                Assert.True(v.Zone.ID < 0);

                Schreiben(TabGebaeude(z.ID_Z), v.Zone);
                GebaeudeBedarfErgebnis mit = GebaeudeBedarfCtrl.Rechnen(projekt, region, z.ID_Z);
                Assert.True(mit.Erfolgreich);
                string fall = projekt.ToString(CultureInfo.InvariantCulture) + " " + z.Gebaeudename + " Faktor " +
                              v.Faktor.ToString("0.#####", CultureInfo.InvariantCulture) + (v.Verbrauchsangabe ? " (Verbrauch)" : "");
                GebaeudeHochrechnungTests.GleicheReihe(ohne.Stundenwerte, mit.Stundenwerte, fall, _aus);
                Assert.True(Math.Abs(mit.HeizwaermeMwh - ohne.HeizwaermeMwh) <= 1e-9 * Math.Abs(ohne.HeizwaermeMwh), fall);
                _ = ohneErgebnis;
            }

            var nachher = new SimulationWaermebedarf();
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            nachher.Waermebedarf_berechnen(projekt, region);
            Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Fehler));
            Assert.True(Math.Abs(nachher.Waermebedarf_Gebaeude_Gesamt - vorher.Waermebedarf_Gebaeude_Gesamt)
                        <= 1e-9 * Math.Abs(vorher.Waermebedarf_Gebaeude_Gesamt),
                        projekt + ": Lauf vorher " + vorher.Waermebedarf_Gebaeude_Gesamt.ToString("R", CultureInfo.InvariantCulture) +
                        ", nachher " + nachher.Waermebedarf_Gebaeude_Gesamt.ToString("R", CultureInfo.InvariantCulture));
            foreach (GebaeudeModellErgebnis e in nachher.GebaeudeErgebnisse.Alle)
                Assert.Equal(1.0, e.Skalierungsfaktor);
        }

        [Theory]
        [InlineData(1007)]
        [InlineData(1008)]
        [InlineData(1018)]
        public void Mit_hochgerechneter_Zone_bleibt_die_Heizwaerme_gleich(int projekt)
        {
            if (!_db.Vorhanden) return;
            UebernahmeGleichtDemKlassenweg(projekt, projekt == 1007 ? 340.0 / 74.0 : (double?)null);
        }

        /// <summary>
        /// <b>Das gekühlte Gebäude (Projekt 1017) und die benannte Ausnahme:</b> Eine Leistungsgrenze
        /// folgt dem Flächenschlüssel nicht — im Klassenweg galt sie dem Katalogbau und wurde mit ihm
        /// nachmultipliziert, mit der Zone gilt sie der hochgerechneten Hülle. Ohne die
        /// Kühlleistungsgrenze bleibt die Heizwärme samt Kühlbetrieb gleich; mit ihr meldet der
        /// Vorschlag die Grenze (die Rückfrage nennt sie).
        /// </summary>
        [Fact]
        public void Das_gekuehlte_Gebaeude_rechnet_ohne_Leistungsgrenze_gleich()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1017;
            int idZ = Z_ProjGebCtrl.LiesProjekt(projekt)[0].ID_Z;
            GebaeudeZonenCtrl.Uebernahmevorschlag mitGrenze = GebaeudeZonenCtrl.Uebernahme(projekt, idZ, null);
            Assert.True(mitGrenze.Ok, mitGrenze.Meldung);
            Assert.True(mitGrenze.Leistungsgrenzen);
            // Der Vorschlag trägt die Grenze mit ihrem Wert, wie sie steht - nicht hochgerechnet.
            object grenze = DataRepository.ExecuteScalar("SELECT Kuehlleistung_Max FROM Tab_Gebaeude WHERE ID = ?",
                                                         new DbParam("@g", TabGebaeude(idZ)));
            Assert.Equal(Convert.ToDouble(grenze, CultureInfo.InvariantCulture), mitGrenze.KuehlgrenzeKw);

            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Kuehlleistung_Max = NULL WHERE ID = ?",
                                                  new DbParam("@g", TabGebaeude(idZ))));
            Assert.False(GebaeudeZonenCtrl.Uebernahme(projekt, idZ, null).Leistungsgrenzen);
            UebernahmeGleichtDemKlassenweg(projekt);
        }

        /// <summary>
        /// <b>Verbrauchsangabe:</b> Der Faktor kommt aus der Verhältnisrechnung des Kataloglaufs
        /// (Verbrauch / gerechneter Verbrauch) — derselbe, den die Fassade nachmultipliziert; mit
        /// der hochgerechneten Zone bleibt die Heizwärme gleich, und die nicht mehr angewandte
        /// Verbrauchsangabe steht als Hinweis im Protokoll.
        /// </summary>
        [Fact]
        public void Mit_Verbrauchsangabe_kommt_der_Faktor_aus_der_Verhaeltnisrechnung()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1007;
            int idZ = Z_ProjGebCtrl.LiesProjekt(projekt)[0].ID_Z;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_ProjektGebaeude SET Einheit_Waermebedarf_Wohnflaeche = ?, Wohnflaeche_Waermebedarf = ? WHERE ID = ?",
                new DbParam("@e", "Verbrauch  [MWh/a]"), new DbParam("@w", DbParamTyp.Double) { Wert = 30.0 },
                new DbParam("@z", idZ)));

            int region = Klimaregion(projekt);
            GebaeudeBedarfErgebnis ohne = GebaeudeBedarfCtrl.Rechnen(projekt, region, idZ);
            Assert.True(ohne.Erfolgreich);
            // Der Klassenweg rechnet den Verbrauch zurück: 30 MWh (bis auf den Rand).
            Assert.Equal(30.0, ohne.HeizwaermeMwh, 6);

            UebernahmeGleichtDemKlassenweg(projekt);

            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            GebaeudeBedarfCtrl.Rechnen(projekt, region, idZ);
            Assert.Contains(p.Hinweise, h => h.Contains("Verbrauch  [MWh/a]", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Faktor 1 bleibt der Grenzfall:</b> Projekt 1045 (Angabe = Nutzfläche) bekommt Faktor
        /// genau 1, die Zone die Nutzfläche des Gebäudes, und die Heizwärme bleibt gleich.
        /// </summary>
        [Fact]
        public void Faktor_eins_bleibt_der_Grenzfall()
        {
            if (!_db.Vorhanden) return;
            UebernahmeGleichtDemKlassenweg(1045, 1.0);
            int idZ = Z_ProjGebCtrl.LiesProjekt(1045)[0].ID_Z;
            ZoneModel zone = Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(TabGebaeude(idZ)));
            Assert.Equal(201.0, zone.Nutzflaeche);
        }

        /// <summary>
        /// Der Arbeitsstand des Dialogs geht in den Vorschlag ein: ein im Dialog geänderter U-Wert
        /// steht an den Bauteilen der Zone, die Angabe des Projekts (Einheit, Fläche) bleibt.
        /// </summary>
        [Fact]
        public void Der_Vorschlag_rechnet_mit_dem_Arbeitsstand_des_Dialogs()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1007;
            int idZ = Z_ProjGebCtrl.LiesProjekt(projekt)[0].ID_Z;
            GebaeudeModel satz = GebaeudeStammCtrl.LiesProjektkopie(TabGebaeude(idZ));
            Assert.NotNull(satz);
            satz.k_Wert_Außenwand = 0.21;

            GebaeudeZonenCtrl.Uebernahmevorschlag v = GebaeudeZonenCtrl.Uebernahme(projekt, idZ, satz);
            Assert.True(v.Ok, v.Meldung);
            Assert.All(v.Zone.Bauteile.Where(b => b.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND), b => Assert.Equal(0.21, b.U_Wert));
            Assert.Equal(340.0, v.Angabe);
            Assert.False(v.Verbrauchsangabe);

            // Ohne Projektkopie: benannt, kein Vorschlag.
            GebaeudeZonenCtrl.Uebernahmevorschlag leer = GebaeudeZonenCtrl.Uebernahme(projekt, 999999, null);
            Assert.False(leer.Ok);
            Assert.Equal(R.ZONE_MSG_UEBERNAHME_OHNE_KOPIE, leer.Meldung);
        }

        /// <summary>
        /// <b>Das Projektduplikat hängt die Bauteile an die Zonen der Kopie</b> (Auftrag G4-Rückfrage):
        /// <c>ID_Zone</c> steht in <c>FK_MAP</c> für die Trinkwarmwasserzone; am Bauteil gewinnt die
        /// deklarierte Beziehung (Schemaschritt S-C) und — als Rückfall — <c>FK_OVERRIDE</c>, beide
        /// mit dem Ziel <c>Tab_Zone</c>. Nach dem Duplizieren eines Projekts mit zwei Zonen zeigt
        /// jedes Bauteil der Kopie auf die gleichnamige Zone der Kopie, keines auf eine Zone der
        /// Quelle.
        /// </summary>
        [Fact]
        public void Das_Projektduplikat_haengt_die_Bauteile_an_die_Zonen_der_Kopie()
        {
            if (!_db.Vorhanden) return;
            var dup = new ProjektDuplizierenCtrl();
            Assert.Equal(SchemaKatalog.TAB_ZONE, dup.ErmittleZieltabelle(SchemaKatalog.TAB_BAUTEIL, "ID_Zone", "ID"));
            Assert.Equal("Tab_TwwZone", dup.ErmittleZieltabelle("Tab_TwwWohnungstyp", "ID_Zone", "ID"));

            const int projekt = 1007;
            int idGebaeude = TabGebaeude(Z_ProjGebCtrl.LiesProjekt(projekt)[0].ID_Z);
            ProjektGebaeudeModel g = GebaeudeBedarfCtrl.Projektgebaeude(projekt, Z_ProjGebCtrl.LiesProjekt(projekt)[0].ID_Z);
            ZoneModel wohnen = GebaeudeZonenabbildung.AlsZoneModel(GebaeudeZonenuebernahme.AlsEineZone(g, 2.0));
            wohnen.Bezeichner = "Wohnen";
            ZoneModel keller = GebaeudeZonenabbildung.AlsZoneModel(new GebaeudeZonensatz(0, "Keller", new[]
            {
                new BauteilEingang("Kellerwand", Bauteilart.Aussenwand, 30.0, Bauteilrand.Erdreich, 0.5, neigungGrad: 90.0),
            }));
            keller.ID = -2;
            keller.Nutzflaeche = 40;             // ab zwei Zonen Pflicht (G6a)
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(idGebaeude, new List<ZoneModel> { wohnen, keller }).Ok);

            int neu = dup.Duplizieren("Laurentiuskirche", "Laurentiuskirche D2");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");

            DataTable zeilen = DataRepository.GetDataTable(
                "SELECT b.Bezeichner AS Bauteil, z.Bezeichner AS Zone, g.ID_Projekt AS Projekt FROM Tab_Bauteil b " +
                "INNER JOIN Tab_Zone z ON z.ID = b.ID_Zone INNER JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude " +
                "WHERE g.ID_Projekt IN (?, ?) ORDER BY g.ID_Projekt, z.Rang, b.Rang",
                new DbParam("@a", projekt), new DbParam("@b", neu));
            var quelle = zeilen.Rows.Cast<DataRow>().Where(r => Convert.ToInt32(r["Projekt"], CultureInfo.InvariantCulture) == projekt)
                               .Select(r => r["Zone"] + "/" + r["Bauteil"]).ToList();
            var kopie = zeilen.Rows.Cast<DataRow>().Where(r => Convert.ToInt32(r["Projekt"], CultureInfo.InvariantCulture) == neu)
                              .Select(r => r["Zone"] + "/" + r["Bauteil"]).ToList();
            Assert.Equal(wohnen.Bauteile.Count + 1, quelle.Count);
            Assert.Equal(quelle, kopie);
            Assert.Contains("Keller/Kellerwand", kopie);

            // Kein Bauteil der Kopie zeigt auf eine Zone außerhalb der Gebäude der Kopie.
            object fremd = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Bauteil b WHERE b.ID_Zone IN (SELECT z.ID FROM Tab_Zone z INNER JOIN Tab_Gebaeude g " +
                "ON g.ID = z.ID_Gebaeude WHERE g.ID_Projekt = ?) AND b.ID NOT IN (SELECT b2.ID FROM Tab_Bauteil b2 " +
                "INNER JOIN Tab_Zone z2 ON z2.ID = b2.ID_Zone INNER JOIN Tab_Gebaeude g2 ON g2.ID = z2.ID_Gebaeude WHERE g2.ID_Projekt = ?)",
                new DbParam("@n", neu), new DbParam("@n2", neu));
            Assert.Equal(0L, Convert.ToInt64(fremd, CultureInfo.InvariantCulture));
            Assert.Equal(2, new GebaeudeZonenCtrl().LesenJeProjekt(neu).Single().Value.Count);
        }
    }
}
