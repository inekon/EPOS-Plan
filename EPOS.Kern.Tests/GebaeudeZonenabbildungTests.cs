using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Werkzeug der Proben der Zonenabbildung: Vergleich zweier Kern-Datensätze und zweier
    /// Zeilensätze Feld für Feld.
    /// </summary>
    internal static class ZonenabbildungProbe
    {
        /// <summary>
        /// Zwei Kern-Sätze sind gleich: Bezeichnung und jedes Bauteil Feld für Feld — Zahlen über
        /// <see cref="double.Equals(double)"/> (bitgleich, NaN gleich NaN), Schichten als Werte.
        /// Die Zonen-Id zählt nicht (der gelesene Satz trägt die vergebene).
        /// </summary>
        internal static void GleicherSatz(GebaeudeZonensatz erwartet, GebaeudeZonensatz ist)
        {
            Assert.NotNull(ist);
            Assert.Null(ist.Lesefehler);
            Assert.Equal(erwartet.Bezeichnung, ist.Bezeichnung);
            Zahl(erwartet.Nutzflaeche_M2, ist.Nutzflaeche_M2, erwartet.Bezeichnung + " Nutzfläche");
            Assert.Equal(erwartet.Bauteile.Count, ist.Bauteile.Count);
            for (int i = 0; i < erwartet.Bauteile.Count; i++) GleichesBauteil(erwartet.Bauteile[i], ist.Bauteile[i]);
        }

        internal static void GleichesBauteil(BauteilEingang a, BauteilEingang b)
        {
            string wo = a.Bezeichnung;
            Assert.Equal(a.Bezeichnung, b.Bezeichnung);
            Assert.Equal(a.Art, b.Art);
            Assert.Equal(a.Rand, b.Rand);
            Zahl(a.Flaeche_M2, b.Flaeche_M2, wo + " Fläche");
            Zahl(a.UWert_WM2K, b.UWert_WM2K, wo + " U");
            Zahl(a.NeigungGrad, b.NeigungGrad, wo + " Neigung");
            Zahl(a.AzimutGrad, b.AzimutGrad, wo + " Azimut");
            Zahl(a.GWert, b.GWert, wo + " g");
            Zahl(a.Rahmenanteil, b.Rahmenanteil, wo + " Rahmen");
            Zahl(a.Verschattungsfaktor, b.Verschattungsfaktor, wo + " Verschattung");
            Zahl(a.PsiL_WK, b.PsiL_WK, wo + " ψ·L");
            Zahl(a.AlphaKonInnen_WM2K, b.AlphaKonInnen_WM2K, wo + " α_i");
            Zahl(a.AlphaKonAussen_WM2K, b.AlphaKonAussen_WM2K, wo + " α_a");
            Assert.Equal(a.Schichten, b.Schichten);
        }

        private static void Zahl(double a, double b, string wo)
            => Assert.True(a.Equals(b), wo + ": erwartet " + a.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                                        ", ist " + b.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>
        /// Zwei Zeilensätze tragen dieselben Werte in den Spalten, die der Kern führt: Name,
        /// Bauteilart, Fläche, U, g, Rahmen, Verschattung, Neigung, Azimut, Randbedingung, ψ·L —
        /// und dieselbe Herkunft. Ids, Rang und Aufbau zählen nicht.
        /// </summary>
        internal static void GleicheZeilen(ZoneModel erwartet, ZoneModel ist)
        {
            Assert.Equal(erwartet.Bezeichner, ist.Bezeichner);
            Assert.Equal(erwartet.Herkunft, ist.Herkunft);
            Assert.Equal(erwartet.Nutzflaeche, ist.Nutzflaeche);
            Assert.Equal(erwartet.Bauteile.Count, ist.Bauteile.Count);
            for (int i = 0; i < erwartet.Bauteile.Count; i++)
            {
                BauteilModel a = erwartet.Bauteile[i], b = ist.Bauteile[i];
                Assert.Equal(a.Bezeichner, b.Bezeichner);
                Assert.Equal(a.Bauteilart, b.Bauteilart);
                Assert.True(a.Flaeche.Equals(b.Flaeche), a.Bezeichner + " Fläche");
                Assert.Equal(a.U_Wert, b.U_Wert);
                Assert.Equal(a.g_Wert, b.g_Wert);
                Assert.Equal(a.Rahmenanteil, b.Rahmenanteil);
                Assert.Equal(a.Verschattungsfaktor, b.Verschattungsfaktor);
                Assert.Equal(a.Neigung, b.Neigung);
                Assert.Equal(a.Azimut, b.Azimut);
                Assert.Equal(a.Randbedingung, b.Randbedingung);
                Assert.Equal(a.Psi_L, b.Psi_L);
                Assert.Equal(a.Herkunft, b.Herkunft);
            }
        }

        /// <summary>
        /// Ein Satz mit jeder Randbedingung und jeder Fensterangabe: die übernommene Zone des
        /// Probegebäudes (Keller) plus Innenwand und Decke innerhalb der Zone, eine Decke an
        /// Außenluft, eine Tür zum unbeheizten Raum, Sonstiges am Erdreich, ein Dachfenster mit
        /// eigenen Werten, eine Vorhangfassade und eine Trennwand zur Nachbarzone.
        /// </summary>
        internal static GebaeudeZonensatz Vollsatz(ProjektGebaeudeModel g)
        {
            GebaeudeZonensatz z = GebaeudeZonenuebernahme.AlsEineZone(g);
            var mehr = new[]
            {
                new BauteilEingang("Innenwände", Bauteilart.Innenwand, 150.0, Bauteilrand.Innen),
                new BauteilEingang("Geschossdecke", Bauteilart.Decke, 80.0, Bauteilrand.Innen, 0.9),
                new BauteilEingang("Decke Durchfahrt", Bauteilart.Decke, 12.5, Bauteilrand.Aussenluft, 0.35),
                new BauteilEingang("Kellertür", Bauteilart.Tuer, 2.0, Bauteilrand.Unbeheizt, 1.8, neigungGrad: 90.0, psiL_WK: 0.4),
                new BauteilEingang("Kellerwand", Bauteilart.Sonstiges, 30.0, Bauteilrand.Erdreich, 0.6),
                new BauteilEingang("Dachfenster", Bauteilart.Fenster, 3.0, Bauteilrand.Aussenluft, 1.3,
                                   neigungGrad: 45.0, azimutGrad: 180.0, gWert: 0.55, rahmenanteil: 0.25, verschattungsfaktor: 0.8),
                new BauteilEingang("Pfosten-Riegel", Bauteilart.Vorhangfassade, 20.0, Bauteilrand.Aussenluft, 1.1,
                                   azimutGrad: 135.0, gWert: 0.4),
                new BauteilEingang("Trennwand Anbau", Bauteilart.Innenwand, 18.0, Bauteilrand.Zone, 1.2),
            };
            return new GebaeudeZonensatz(0, "Wohnen", z.Bauteile.Concat(mehr).ToList());
        }

        /// <summary>
        /// Derselbe Satz ohne die Trennwand zur Nachbarzone — was der Schreibweg einer einzelnen Zone
        /// annimmt: Eine Trennfläche braucht ihre Nachbarzone im selben Gebäude (Stufe G6b,
        /// <see cref="Zonenkopplungsregeln"/>), und dieser Satz nennt keine.
        /// </summary>
        internal static GebaeudeZonensatz OhneTrennflaeche(GebaeudeZonensatz s)
            => new GebaeudeZonensatz(s.ZonenId, s.Bezeichnung, s.Bauteile.Where(b => b.Rand != Bauteilrand.Zone).ToList(), s.Nutzflaeche_M2);
    }

    /// <summary>
    /// <b>Stufe G3, Welle D1 — die Abbildung Zeile ↔ Kern der Zonen</b>, ohne Datenbank: die
    /// Persistenzwerte gegen die Kern-Aufzählungen, die Regel der leeren Randbedingung, NULL →
    /// NaN bzw. Vorgabe, die Schichten aus dem Aufbau, die benannten Fehler und die Gegenrichtung
    /// mit vorläufigen Ids. Die Proben mit der Datenbank stehen in
    /// <see cref="GebaeudeBauteilwegDatenbankTests"/>.
    /// </summary>
    public class GebaeudeZonenabbildungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly Dictionary<int, BauteilaufbauModel> KeineAufbauten = new Dictionary<int, BauteilaufbauModel>();

        private static BauteilModel Zeile(string art, string rand = null)
            => new BauteilModel { ID = 7, Bezeichner = "Probe", Bauteilart = art, Flaeche = 10.0, U_Wert = 0.5, Azimut = 180.0, Randbedingung = rand };

        // =====================================================================
        //  Die Persistenzwerte
        // =====================================================================

        [Fact]
        public void Die_Persistenzwerte_und_die_Kern_Aufzaehlungen_entsprechen_einander()
        {
            // Jede der neun Bauteilarten hat genau eine Kern-Art und umgekehrt.
            Bauteilart[] arten = DbWerte.BAUTEILARTEN.Select(w => GebaeudeZonenabbildung.ArtAusZeile(w).Value).ToArray();
            Assert.Equal(Enum.GetValues<Bauteilart>().OrderBy(a => a), arten.OrderBy(a => a));
            foreach (string w in DbWerte.BAUTEILARTEN)
                Assert.Equal(w, GebaeudeZonenabbildung.ArtFuerZeile(GebaeudeZonenabbildung.ArtAusZeile(w).Value));
            Assert.Null(GebaeudeZonenabbildung.ArtAusZeile("KELLER"));
            Assert.Null(GebaeudeZonenabbildung.ArtAusZeile("aussenwand"));   // Persistenzwerte sind Großbuchstaben
            Assert.Null(GebaeudeZonenabbildung.ArtAusZeile(null));

            // Jede der vier Randbedingungen hat ihren Kern-Wert; „innen" hat keinen Persistenzwert.
            Assert.Equal(Bauteilrand.Aussenluft, GebaeudeZonenabbildung.RandAusZeile(Bauteilart.Aussenwand, DbWerte.RANDBEDINGUNG_AUSSENLUFT));
            Assert.Equal(Bauteilrand.Erdreich, GebaeudeZonenabbildung.RandAusZeile(Bauteilart.Aussenwand, DbWerte.RANDBEDINGUNG_ERDREICH));
            Assert.Equal(Bauteilrand.Zone, GebaeudeZonenabbildung.RandAusZeile(Bauteilart.Aussenwand, DbWerte.RANDBEDINGUNG_ZONE));
            Assert.Equal(Bauteilrand.Unbeheizt, GebaeudeZonenabbildung.RandAusZeile(Bauteilart.Aussenwand, DbWerte.RANDBEDINGUNG_UNBEHEIZT));
            Assert.Null(GebaeudeZonenabbildung.RandAusZeile(Bauteilart.Aussenwand, DbWerte.GRUND_KELLER));
            Assert.Null(GebaeudeZonenabbildung.RandAusZeile("KELLER", null));
            foreach (Bauteilrand r in Enum.GetValues<Bauteilrand>().Where(r => r != Bauteilrand.Innen))
                Assert.Equal(r, GebaeudeZonenabbildung.RandAusZeile(Bauteilart.Sonstiges,
                                GebaeudeZonenabbildung.RandFuerZeile(Bauteilart.Sonstiges, r, "Probe")));
        }

        /// <summary>
        /// <b>Die Regel der leeren Randbedingung:</b> NULL heißt Außenluft, an Innenwand und Decke
        /// „innerhalb der Zone" — dort geht sie in die Innenbauteilgruppe, wie der Bauteilweg
        /// <see cref="Bauteilrand.Innen"/> erwartet. Ein gesetzter Wert gilt an jeder Art; die
        /// Gegenrichtung schreibt „innen" als NULL und nur an Innenwand und Decke, alles andere
        /// ausdrücklich.
        /// </summary>
        [Fact]
        public void Die_leere_Randbedingung_heisst_Aussenluft_ausser_an_Innenwand_und_Decke()
        {
            foreach (Bauteilart art in Enum.GetValues<Bauteilart>())
            {
                bool innen = art == Bauteilart.Innenwand || art == Bauteilart.Decke;
                Assert.Equal(innen, GebaeudeZonenabbildung.LeerHeisstInnen(art));
                Assert.Equal(innen ? Bauteilrand.Innen : Bauteilrand.Aussenluft, GebaeudeZonenabbildung.RandAusZeile(art, null));
                Assert.Equal(Bauteilrand.Aussenluft, GebaeudeZonenabbildung.RandAusZeile(art, DbWerte.RANDBEDINGUNG_AUSSENLUFT));
                Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, GebaeudeZonenabbildung.RandFuerZeile(art, Bauteilrand.Aussenluft, "Probe"));
                if (innen)
                    Assert.Null(GebaeudeZonenabbildung.RandFuerZeile(art, Bauteilrand.Innen, "Probe"));
                else
                    Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                                 Assert.Throws<GebaeudeModellException>(() => GebaeudeZonenabbildung.RandFuerZeile(art, Bauteilrand.Innen, "Probe")).Grund);
            }

            // Im Kern: die leere Innenwand und Decke rechnen in der Innengruppe, eine ausdrücklich
            // an Außenluft gesetzte Innenwand in der Außengruppe.
            BauteilEingang iw = GebaeudeZonenabbildung.AlsBauteil(Zeile(DbWerte.BAUTEILART_INNENWAND), KeineAufbauten, "Probe");
            BauteilEingang de = GebaeudeZonenabbildung.AlsBauteil(Zeile(DbWerte.BAUTEILART_DECKE), KeineAufbauten, "Probe");
            BauteilEingang aussen = GebaeudeZonenabbildung.AlsBauteil(Zeile(DbWerte.BAUTEILART_INNENWAND, DbWerte.RANDBEDINGUNG_AUSSENLUFT), KeineAufbauten, "Probe");
            Assert.Equal(Bauteilgruppe.Innen, iw.Gruppe);
            Assert.Equal(Bauteilgruppe.Innen, de.Gruppe);
            Assert.Equal(Bauteilgruppe.Aussen, aussen.Gruppe);
            Assert.Equal(Bauteilgruppe.Aussen, GebaeudeZonenabbildung.AlsBauteil(Zeile(DbWerte.BAUTEILART_TUER), KeineAufbauten, "Probe").Gruppe);

            // Die Prüfregel des Dialogs fragt dieselbe Regel: Eine leere Innenwand braucht keinen Azimut.
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_INNENWAND }));
            Assert.True(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_INNENWAND,
                                                                           Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT }));
            Assert.False(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_DECKE, Neigung = 30.0 }));
            Assert.True(GebaeudeZonenCtrl.BrauchtAzimut(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_TUER }));
        }

        // =====================================================================
        //  Zeile → Kern
        // =====================================================================

        /// <summary>
        /// NULL → NaN (U-Wert aus den Schichten, Neigung nach Art, kein Azimut, g/Rahmen/
        /// Verschattung = Wert des Gebäudes), <c>Psi_L</c> NULL → 0; die Schichten aus dem Aufbau
        /// mit ihrer Wertekopie, innen → außen: eine Luftschicht ohne λ ist eine ruhende, eine mit
        /// λ darf ohne ρ und c_p stehen.
        /// </summary>
        [Fact]
        public void Eine_Zeile_wird_zum_Kern_Eingang()
        {
            var leer = new BauteilModel { ID = 3, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 50.0 };
            BauteilEingang b = GebaeudeZonenabbildung.AlsBauteil(leer, KeineAufbauten, "Probe");
            Assert.Equal("Dach", b.Bezeichnung);
            Assert.Equal(Bauteilart.Dach, b.Art);
            Assert.Equal(50.0, b.Flaeche_M2);
            Assert.Equal(Bauteilrand.Aussenluft, b.Rand);
            Assert.True(double.IsNaN(b.UWert_WM2K));
            Assert.True(double.IsNaN(b.NeigungGrad));
            Assert.Equal(0.0, b.NeigungWirksamGrad);                           // Vorgabe nach Art
            Assert.True(double.IsNaN(b.AzimutGrad));
            Assert.True(double.IsNaN(b.GWert) && double.IsNaN(b.Rahmenanteil) && double.IsNaN(b.Verschattungsfaktor));
            Assert.Equal(0.0, b.PsiL_WK);
            Assert.True(double.IsNaN(b.AlphaKonInnen_WM2K) && double.IsNaN(b.AlphaKonAussen_WM2K));
            Assert.False(b.HatSchichten);

            var fenster = new BauteilModel
            {
                Bezeichner = "Fenster", Bauteilart = DbWerte.BAUTEILART_FENSTER, Flaeche = 4.0, U_Wert = 1.1, g_Wert = 0.5,
                Rahmenanteil = 0.3, Verschattungsfaktor = 0.9, Neigung = 60.0, Azimut = 200.0, Psi_L = 0.25,
                Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT,
            };
            BauteilEingang f = GebaeudeZonenabbildung.AlsBauteil(fenster, KeineAufbauten, "Probe");
            Assert.Equal((1.1, 0.5, 0.3, 0.9, 60.0, 200.0, 0.25, Bauteilrand.Unbeheizt),
                         (f.UWert_WM2K, f.GWert, f.Rahmenanteil, f.Verschattungsfaktor, f.NeigungGrad, f.AzimutGrad, f.PsiL_WK, f.Rand));

            var aufbau = new BauteilaufbauModel
            {
                ID = 55, Bezeichner = "Wand",
                Schichten =
                {
                    new BauteilschichtModel { Reihenfolge = 1, ID_Baustoff = 1, Dicke = 0.015, Lambda = 1.0, Rho = 1800.0, Cp = 1000.0 },
                    new BauteilschichtModel { Reihenfolge = 2, Dicke = 0.04, IstLuftschicht = true },
                    new BauteilschichtModel { Reihenfolge = 3, Dicke = 0.03, IstLuftschicht = true, Lambda = 0.2 },
                    new BauteilschichtModel { Reihenfolge = 4, ID_Baustoff = 18, Dicke = 0.175, Lambda = 0.7, Rho = 1400.0, Cp = 1000.0 },
                }
            };
            var wand = new BauteilModel { Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 20.0, Azimut = 90.0, ID_Aufbau = 55 };
            BauteilEingang w = GebaeudeZonenabbildung.AlsBauteil(wand, new Dictionary<int, BauteilaufbauModel> { [55] = aufbau }, "Probe");
            Assert.True(w.HatSchichten);
            Assert.True(double.IsNaN(w.UWert_WM2K));
            Assert.Equal(new[]
            {
                new Schicht(0.015, 1.0, 1800.0, 1000.0),
                Schicht.RuhendeLuft(0.04),
                new Schicht(0.03, 0.2, double.NaN, double.NaN, true),
                new Schicht(0.175, 0.7, 1400.0, 1000.0),
            }, w.Schichten);
            Assert.True(w.Schichten[1].IstRuhendeLuftschicht);
            Assert.False(w.Schichten[2].IstRuhendeLuftschicht);
            w.Pruefen("Probe");                                                // der Bauteilweg nimmt es an
        }

        /// <summary>
        /// <b>G3 liest von der Zone nur Nutzfläche und Bauteile</b> (Konzept Gebäudesimulation N1.46,
        /// Festlegungen 12 und 13): Eine Zone mit <c>IstBeheizt = 0</c> und eigenen Sollwerten,
        /// Lüftungs-, Gewinn- und Übergabespalten bildet denselben Kern-Satz wie eine beheizte Zone
        /// ohne sie — sie rechnet wie beheizt, mit den Werten des Gebäudes, bis G6.
        /// </summary>
        [Fact]
        public void Eine_unbeheizte_Zone_und_die_uebrigen_Zonenspalten_bleiben_in_G3_ungelesen()
        {
            BauteilModel Wand() => new BauteilModel { ID = 1, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                                      Flaeche = 20.0, U_Wert = 0.3, Azimut = 180.0 };
            var beheizt = new ZoneModel { ID = 5, Bezeichner = "Wohnen", Nutzflaeche = 120.0, Bauteile = { Wand() } };
            var unbeheizt = new ZoneModel
            {
                ID = 5, Bezeichner = "Wohnen", Nutzflaeche = 120.0, IstBeheizt = false,
                Raumhoehe = 3.5, Raumsolltemperatur_Tag = 17.0, Heizleistung_Max = 4.0, Luftwechsel_Nutzer = 2.0,
                Interne_Waermegewinne = 900.0, Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR,
                Kuehlung_Aktiv = true, Kuehl_Sollwert = 24.0, Kuehlleistung_Max = 3.0,
                Bauteile = { Wand() }
            };

            ZonenabbildungProbe.GleicherSatz(GebaeudeZonenabbildung.AlsZonensatz(beheizt, KeineAufbauten),
                                             GebaeudeZonenabbildung.AlsZonensatz(unbeheizt, KeineAufbauten));
        }

        /// <summary>
        /// Benannte Fehler statt stiller Annahmen: eine unbekannte Bauteilart oder Randbedingung,
        /// ein Aufbau, den das Projekt nicht führt, ein Aufbau ohne Schicht, eine Schicht ohne
        /// Wertekopie, die keine Luftschicht ist.
        /// </summary>
        [Fact]
        public void Unbekannte_Werte_und_fehlende_Stoffwerte_sind_benannte_Fehler()
        {
            GebaeudeModellException Fehler(BauteilModel b, Dictionary<int, BauteilaufbauModel> aufbauten = null)
                => Assert.Throws<GebaeudeModellException>(() => GebaeudeZonenabbildung.AlsBauteil(b, aufbauten ?? KeineAufbauten, "Zone „Z“, Probe"));

            GebaeudeModellException art = Fehler(Zeile("KELLER"));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, art.Grund);
            Assert.Equal(string.Format(R.SIMENG_G3_ZEILE_BAUTEILART, "Zone „Z“, Probe", "KELLER", string.Join(", ", DbWerte.BAUTEILARTEN)), art.Message);

            GebaeudeModellException rand = Fehler(Zeile(DbWerte.BAUTEILART_AUSSENWAND, "KELLER"));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, rand.Grund);
            Assert.Contains("„KELLER“", rand.Message, StringComparison.Ordinal);

            BauteilModel mitAufbau = Zeile(DbWerte.BAUTEILART_AUSSENWAND);
            mitAufbau.ID_Aufbau = 99;
            GebaeudeModellException fremd = Fehler(mitAufbau);
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, fremd.Grund);
            Assert.Equal(string.Format(R.SIMENG_G3_ZEILE_AUFBAU_FEHLT, "Zone „Z“, Probe", "99"), fremd.Message);

            var leer = new BauteilaufbauModel { ID = 99, Bezeichner = "Leer" };
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Fehler(mitAufbau, new Dictionary<int, BauteilaufbauModel> { [99] = leer }).Grund);

            var ohneWerte = new BauteilaufbauModel
            {
                ID = 99, Bezeichner = "Halb",
                Schichten =
                {
                    new BauteilschichtModel { Dicke = 0.2, Lambda = 0.7, Rho = 1400.0, Cp = 1000.0 },
                    new BauteilschichtModel { Dicke = 0.165, Lambda = 0.035 },
                }
            };
            GebaeudeModellException schicht = Fehler(mitAufbau, new Dictionary<int, BauteilaufbauModel> { [99] = ohneWerte });
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, schicht.Grund);
            Assert.Equal(string.Format(R.SIMENG_G3_ZEILE_SCHICHT_OHNE_WERTE, "Zone „Z“, Probe (Halb)", "2", "ρ, c_p"), schicht.Message);
            ohneWerte.Schichten[1] = new BauteilschichtModel { Dicke = 0.165 };
            Assert.Contains("λ, ρ, c_p", Fehler(mitAufbau, new Dictionary<int, BauteilaufbauModel> { [99] = ohneWerte }).Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Eine unlesbare Zone wird nicht verschwiegen:</b> Die Abbildung aller Zonen eines
        /// Projekts hängt sie mit ihrem Fehler an, und erst der Umschalter des Laufs wirft ihn —
        /// mit dem Gebäude davor. Eine lesbare Zone eines Nachbargebäudes bleibt lesbar.
        /// </summary>
        [Fact]
        public void Eine_unlesbare_Zone_traegt_ihren_Fehler_bis_zum_Umschalter()
        {
            var gut = new ZoneModel { ID = 1, ID_Gebaeude = 10, Bezeichner = "Gut", Bauteile = { Zeile(DbWerte.BAUTEILART_AUSSENWAND) } };
            var schlecht = new ZoneModel { ID = 2, ID_Gebaeude = 20, Bezeichner = "Schlecht", Bauteile = { Zeile("KELLER") } };
            Dictionary<int, IReadOnlyList<GebaeudeZonensatz>> je = GebaeudeZonenabbildung.JeGebaeude(
                new Dictionary<int, List<ZoneModel>> { [10] = new List<ZoneModel> { gut }, [20] = new List<ZoneModel> { schlecht } },
                KeineAufbauten);

            GebaeudeZonensatz a = Assert.Single(je[10]);
            Assert.Null(a.Lesefehler);
            Assert.Equal(1, a.ZonenId);
            Assert.Single(a.Bauteile);

            GebaeudeZonensatz b = Assert.Single(je[20]);
            Assert.Equal(2, b.ZonenId);
            Assert.Equal("Schlecht", b.Bezeichnung);
            Assert.Empty(b.Bauteile);
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, b.Lesefehlergrund);
            Assert.StartsWith(GebaeudeZonenabbildung.Wer("Schlecht") + ", Probe: ", b.Lesefehler, StringComparison.Ordinal);

            var g = new ProjektGebaeudeModel { ID_Gebaeude = 20, Zonen = je[20] };
            Assert.True(GebaeudeZonensatz.HatZonen(g));
            GebaeudeModellException ex = Assert.Throws<GebaeudeModellException>(() => GebaeudeZonensatz.EineZone(g.Zonen, "Haus (20)"));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, ex.Grund);
            Assert.Equal("Haus (20), " + b.Lesefehler, ex.Message);
            Assert.Throws<ArgumentException>(() => GebaeudeZonenabbildung.AlsZoneModel(b));
        }

        // =====================================================================
        //  Kern → Zeile
        // =====================================================================

        /// <summary>
        /// <b>Die Gegenrichtung für „Gebäude als eine Zone übernehmen":</b> neue Zeilen mit
        /// negativen vorläufigen Ids, Herkunft VORGABE, ohne Aufbau, die Randbedingung
        /// ausdrücklich; die Prüfung des Schreibwegs nimmt sie an, und zurückgelesen ist es
        /// derselbe Satz, Feld für Feld.
        /// </summary>
        [Fact]
        public void Die_Gegenrichtung_schreibt_vorlaeufige_Zeilen_verlustfrei()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER;
            GebaeudeZonensatz satz = ZonenabbildungProbe.Vollsatz(g);

            ZoneModel zone = GebaeudeZonenabbildung.AlsZoneModel(satz);
            Assert.Equal(-1, zone.ID);
            Assert.Equal("Wohnen", zone.Bezeichner);
            Assert.Equal(DbWerte.HERKUNFT_VORGABE, zone.Herkunft);
            Assert.True(zone.IstBeheizt);
            Assert.Null(zone.Nutzflaeche);
            Assert.Null(zone.Raumsolltemperatur_Tag);
            Assert.Equal(Enumerable.Range(1, satz.Bauteile.Count).Select(i => -i), zone.Bauteile.Select(b => b.ID));
            Assert.All(zone.Bauteile, b => Assert.Equal(DbWerte.HERKUNFT_VORGABE, b.Herkunft));
            Assert.All(zone.Bauteile, b => Assert.Null(b.ID_Aufbau));
            // Die Gegenrichtung trägt keinen Nachbarn (der Satz nennt keinen): Eine
            // Trennfläche ohne Nachbarzone hält die Prüfung an (Stufe G6b, Zonenkopplungsregeln);
            // ohne sie ist die Zone gültig.
            Assert.Equal(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.ZONE_MSG_NACHBAR_FEHLT, "Trennwand Anbau", "Wohnen"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { zone }));
            ZoneModel ohneTrennflaeche = zone.Kopie();
            ohneTrennflaeche.Bauteile = zone.Bauteile.Where(b => b.Randbedingung != DbWerte.RANDBEDINGUNG_ZONE).Select(b => b.Kopie()).ToList();
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { ohneTrennflaeche }));

            BauteilModel Bt(string name) => zone.Bauteile.Single(b => b.Bezeichner == name);
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, Bt("Außenwand Süd").Randbedingung);
            Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, Bt("Bodenplatte").Randbedingung);
            Assert.Null(Bt("Innenwände").Randbedingung);
            Assert.Null(Bt("Geschossdecke").Randbedingung);
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, Bt("Decke Durchfahrt").Randbedingung);
            Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, Bt("Trennwand Anbau").Randbedingung);
            Assert.Equal(DbWerte.BAUTEILART_VORHANGFASSADE, Bt("Pfosten-Riegel").Bauteilart);
            Assert.Null(Bt("Dach").Psi_L);                                     // 0 → NULL
            Assert.Null(Bt("Dach").Azimut);                                    // NaN → NULL
            Assert.Null(Bt("Fenster Süd").g_Wert);                             // NaN = Wert des Gebäudes
            Assert.Equal(0.55, Bt("Dachfenster").g_Wert);
            Assert.True(Bt("Außenwand Nord").Psi_L > 0.0);

            // Kern → Zeile → Kern: derselbe Satz, bitgleich.
            ZonenabbildungProbe.GleicherSatz(satz, GebaeudeZonenabbildung.AlsZonensatz(zone, KeineAufbauten));
            // Zeile → Kern → Zeile: dieselben Zeilen.
            ZonenabbildungProbe.GleicheZeilen(zone, GebaeudeZonenabbildung.AlsZoneModel(GebaeudeZonenabbildung.AlsZonensatz(zone, KeineAufbauten)));

            // Eine leere Randbedingung an einer Außenwand kommt ausdrücklich zurück — dieselbe Aussage.
            BauteilModel leer = Zeile(DbWerte.BAUTEILART_AUSSENWAND);
            BauteilModel zurueck = GebaeudeZonenabbildung.AlsBauteilModel(GebaeudeZonenabbildung.AlsBauteil(leer, KeineAufbauten, "Probe"), -1, "Probe");
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, zurueck.Randbedingung);
            Assert.Equal(GebaeudeZonenabbildung.RandAusZeile(leer.Bauteilart, leer.Randbedingung),
                         GebaeudeZonenabbildung.RandAusZeile(zurueck.Bauteilart, zurueck.Randbedingung));
        }

        /// <summary>
        /// Was eine Zeile nicht tragen kann, lehnt die Gegenrichtung benannt ab: Schichten (die
        /// Übernahme schreibt keinen Aufbau), ein eigenes α_kon, „innen" an einer Außenwand.
        /// </summary>
        [Fact]
        public void Die_Gegenrichtung_lehnt_ab_was_die_Zeile_nicht_traegt()
        {
            GebaeudeModellFehler Grund(BauteilEingang b)
                => Assert.Throws<GebaeudeModellException>(() =>
                       GebaeudeZonenabbildung.AlsZoneModel(new GebaeudeZonensatz(0, "Z", new[] { b }))).Grund;

            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, Grund(new BauteilEingang("Wand", Bauteilart.Aussenwand, 10.0,
                Bauteilrand.Aussenluft, schichten: new[] { BauteilwegLaufProbe.Mauerwerk }, azimutGrad: 0.0)));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, Grund(new BauteilEingang("Wand", Bauteilart.Aussenwand, 10.0,
                Bauteilrand.Aussenluft, 0.3, azimutGrad: 0.0, alphaKonInnen_WM2K: 2.5)));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, Grund(new BauteilEingang("Wand", Bauteilart.Aussenwand, 10.0,
                Bauteilrand.Innen, 0.3)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GebaeudeZonenabbildung.AlsBauteilModel(
                new BauteilEingang("Wand", Bauteilart.Aussenwand, 10.0, Bauteilrand.Aussenluft, 0.3), 0, "Probe"));
        }
    }
}
