using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Bauteilseite eines CAD-Exports ohne Raumgrenzen</b> (Mehrzonenkonzept 6.5) an der Probe
    /// <c>ifc2x3_enthaltensein.ifc</c> (<see cref="IfcProbenErzeuger.Enthaltensein"/>, Bauteile „… CAD"):
    /// Flächen aus <c>GrossArea</c>/<c>NetArea</c> eines fremden Mengensatzes (nie aus <c>Width</c>/<c>Length</c>),
    /// der U-Wert unter <c>UValue (W/(m² K))</c> und nie mit der falschen Einheit <c>W/(m K)</c>, die
    /// Angrenzung aus <c>AdjacentType</c>, die Hüllkennung, die Himmelsrichtung aus <c>Orientation (°)</c> und
    /// Dachfenster als Teile des Dachs — jeder Rückfall benannt. Die Gegenprobe hält alle übrigen Proben
    /// ohne eine der neuen Meldungen.
    /// </summary>
    public sealed class IfcCadBauteileTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const string PROBE = "ifc2x3_enthaltensein.ifc";

        /// <summary>Die Meldungen der Bauteil-Rückfälle.</summary>
        private static readonly string[] NEUE_SCHLUESSEL =
        {
            "OEFFNUNG_TEIL", "BAUTEIL_MENGE_RUECKFALL", "UWERT_RUECKFALL", "UWERT_EINHEIT", "ANGRENZUNG_AUSSEN",
            "ANGRENZUNG_ERDREICH", "ANGRENZUNG_INNEN", "ANGRENZUNG_UNBEHEIZT", "ANGRENZUNG_UNBESTIMMT", "NICHT_HUELLE",
            "AZIMUT_RUECKFALL",
        };

        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeImportAblauf Lesen(string name)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), name);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.True(a.Abbild != null && a.Abbild.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a;
        }

        private static void Nah(double erwartet, double? ist)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= 1e-9, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        private static AbbildBauteil Bauteil(GebaeudeImportAblauf a, string name)
            => a.Abbild.Gebaeude.Single().Bauteile.Single(b => b.Name == name);

        private static List<string> Werte(GebaeudeImportAblauf a, string schluessel)
            => a.Meldungen.Where(m => m.Schluessel == P + schluessel).Select(m => m.Stufe + "|" + string.Join("|", m.Werte)).ToList();

        [Fact]
        public void Flaechen_kommen_aus_den_Flaechennamen_des_fremden_Satzes_nie_aus_Width_und_Length()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            AbbildBauteil nord = Bauteil(a, "Nord CAD");
            Nah(30.0, nord.BruttoflaecheM2);
            Nah(24.0, nord.NettoflaecheM2);
            Assert.Null(nord.DickeM);   // Width (10 m) ist im fremden Satz die Länge, keine Dicke

            AbbildBauteil f1 = nord.Oeffnungen.Single(o => o.Name == "F1 CAD");
            AbbildBauteil f2 = nord.Oeffnungen.Single(o => o.Name == "F2 CAD");
            Nah(4.0, f1.BruttoflaecheM2);   // OverallWidth × OverallHeight
            Nah(2.0, f2.BruttoflaecheM2);   // GrossArea, nicht Width × Length = 25 m²

            Nah(35.0, Bauteil(a, "Dach CAD").BruttoflaecheM2);
            Nah(60.0, Bauteil(a, "Kellerdecke CAD").BruttoflaecheM2);
            Assert.Null(Bauteil(a, "Hilfswand CAD").BruttoflaecheM2);

            Assert.Equal(new[]
            {
                "Warnung|1|Area|CAD_BauteilQuantities|GrossArea",
                "Warnung|8|GrossArea|CAD_BauteilQuantities|GrossArea",
                "Warnung|8|NetArea|CAD_BauteilQuantities|NetArea",
            }, Werte(a, "BAUTEIL_MENGE_RUECKFALL"));
            PruefMeldung ohne = a.Meldungen.Single(m => m.Schluessel == P + "KEINE_MENGEN");
            Assert.Equal("1", ohne.Werte[0]);
        }

        [Fact]
        public void Der_U_Wert_gilt_unter_UValue_und_nie_mit_der_Einheit_W_je_m_K()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            AbbildBauteil nord = Bauteil(a, "Nord CAD");
            Nah(0.25, nord.UWertWm2K);   // nicht 0,5 aus Pset_WallCommon."ThermalTransmittance (W/(m K))"
            Assert.Equal("CAD_Bauteilreferenzen", nord.UWertQuelle);
            Assert.All(nord.Oeffnungen, o => Nah(1.2, o.UWertWm2K));   // nicht 9,9
            Nah(0.2, Bauteil(a, "Dach CAD").UWertWm2K);                 // nicht 0,9
            Nah(1.4, Bauteil(a, "Dach CAD").Oeffnungen.Single().UWertWm2K);
            Nah(0.3, Bauteil(a, "EG Süd").UWertWm2K);                   // der Standardweg über den Typ bleibt
            Assert.Equal("Pset_WallCommon (Typ)", Bauteil(a, "EG Süd").UWertQuelle);
            Assert.Null(Bauteil(a, "Hilfswand CAD").UWertWm2K);

            Assert.Equal(new[] { "Info|11|CAD_Bauteilreferenzen|UValue (W/(m² K))" }, Werte(a, "UWERT_RUECKFALL"));
            Assert.Equal(new[]
            {
                "Info|1|Pset_RoofCommon|ThermalTransmittance (W/(m K))|W/(m K)|0",
                "Info|1|Pset_WallCommon|ThermalTransmittance (W/(m K))|W/(m K)|0",
                "Info|2|Pset_WindowCommon|ThermalTransmittance (W/(m K))|W/(m K)|0",
            }, Werte(a, "UWERT_EINHEIT"));
        }

        [Fact]
        public void Die_Angrenzung_ersetzt_IsExternal_und_die_Huellkennung_nimmt_aus_der_Huelle()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            void Pruefen(string name, Randbedingung rand, Bauteilart art, bool huelle, bool? boden = null)
            {
                AbbildBauteil b = Bauteil(a, name);
                Assert.True(rand == b.Randbedingung, name + ": " + b.Randbedingung);
                Assert.True(art == b.Art, name + ": " + b.Art);
                Assert.True(huelle == b.HuelleOhneNachbar, name + ": Hülle " + b.HuelleOhneNachbar);
                Assert.True(boden == b.ZonenbodenOhneNachbar, name + ": Boden " + b.ZonenbodenOhneNachbar);
            }
            Pruefen("Nord CAD", Randbedingung.Aussenluft, Bauteilart.Aussenwand, true);
            Pruefen("Dach CAD", Randbedingung.Aussenluft, Bauteilart.Dach, true);
            Pruefen("Kellerwand CAD", Randbedingung.Erdreich, Bauteilart.Aussenwand, true);
            Pruefen("Innenwand CAD", Randbedingung.Innen, Bauteilart.Innenwand, false);
            Pruefen("Kellerdecke CAD", Randbedingung.Unbeheizt, Bauteilart.Decke, true, true);
            Pruefen("Oberste Decke CAD", Randbedingung.Unbeheizt, Bauteilart.Decke, true, false);
            Pruefen("Wand unbeheizt CAD", Randbedingung.Unbeheizt, Bauteilart.Innenwand, true);
            Pruefen("Hilfswand CAD", Randbedingung.Unbekannt, Bauteilart.Innenwand, false);
            Pruefen("Spitzbodenwand CAD", Randbedingung.Aussenluft, Bauteilart.Aussenwand, false);

            const string ORT = "CAD_BauteilAllgemein.AdjacentType";
            Assert.Equal(new[] { "Info|3|" + ORT + "|btaOutside" }, Werte(a, "ANGRENZUNG_AUSSEN"));
            Assert.Equal(new[] { "Info|1|" + ORT + "|btaGround" }, Werte(a, "ANGRENZUNG_ERDREICH"));
            Assert.Equal(new[] { "Info|1|" + ORT + "|btaHeated" }, Werte(a, "ANGRENZUNG_INNEN"));
            Assert.Equal(new[]
            {
                "Info|1|" + ORT + "|btaCellarCeiling", "Info|1|" + ORT + "|btaUnHeated", "Info|1|" + ORT + "|btaUppermostStorey",
            }, Werte(a, "ANGRENZUNG_UNBEHEIZT"));
            Assert.Equal(new[] { "Warnung|1|" + ORT + "|btaNone" }, Werte(a, "ANGRENZUNG_UNBESTIMMT"));
            Assert.Equal(new[] { "Info|1|CAD_BauteilEnergetik.ElementEnergyConsultingProperties.CladdingSurface" },
                         Werte(a, "NICHT_HUELLE"));
        }

        [Fact]
        public void Himmelsrichtung_aus_Orientation_und_Dachfenster_als_Oeffnung_des_Dachs()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            Nah(0.0, Bauteil(a, "Nord CAD").AzimutGrad);
            Nah(90.0, Bauteil(a, "Kellerwand CAD").AzimutGrad);
            Nah(270.0, Bauteil(a, "Spitzbodenwand CAD").AzimutGrad);
            Assert.Null(Bauteil(a, "EG Süd").AzimutGrad);   // ohne Orientation bleibt die Seite unbestimmt
            Assert.Equal(new[] { "Info|3|CAD_BauteilAllgemein.Orientation (°)" }, Werte(a, "AZIMUT_RUECKFALL"));
            Assert.Equal("1", a.Meldungen.Single(m => m.Schluessel == P + "SEITE_UNBESTIMMT").Werte[0]);

            AbbildBauteil df = Bauteil(a, "Dach CAD").Oeffnungen.Single();
            Assert.Equal("DF CAD", df.Name);
            Assert.Equal(Bauteilart.Fenster, df.Art);
            Nah(1.0, df.BruttoflaecheM2);
            Assert.Equal(new[] { "Info|1" }, Werte(a, "OEFFNUNG_TEIL"));
            Assert.Empty(Werte(a, "OHNE_WIRT"));
        }

        [Fact]
        public void Zuordnung_und_Bauteilvorschlag_tragen_die_Huelle_ohne_Raumgrenzen()
        {
            GebaeudeImportAblauf a = Lesen(PROBE);
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Nah(54.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert);   // 30 (EG Süd) + 30 − 6 (Nord CAD)
            Nah(7.0, satz.Zeile(GebaeudeZielfelder.FENSTER_GESAMT).Wert);        // 4 + 2 + Dachfenster 1
            Nah(54.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert);         // 35 − 1 + oberste Decke 20
            Nah(70.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);        // Kellerdecke 60 + Kellerwand 10
            Nah(6.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);      // Wand unbeheizt

            foreach (GebaeudeBauteilvorschlag v in new[] { GebaeudeBauteilvorschlag.Bilden(a, 0, null), GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null) })
            {
                // Einziger Fehler: „EG Süd" (Standardweg, ohne Raumgrenze und ohne Orientation) bleibt ohne
                // Himmelsrichtung — die bisherige Regel; kein CAD-Bauteil fehlt.
                PruefMeldung fehler = Assert.Single(v.Meldungen, m => m.Stufe == PruefStufe.Fehler);
                Assert.Equal(GebaeudeBauteilvorschlag.AZIMUT_FEHLT, fehler.Schluessel);
                Assert.Equal(new[] { "1", Bauteil(a, "EG Süd").Kennung }, fehler.Werte);
                Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.UWERT_FEHLT);

                GebaeudeBauteilzeile kd = v.Zeilen.Single(z => z.Kennung == Bauteil(a, "Kellerdecke CAD").Kennung);
                Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, kd.Bauteil.Randbedingung);
                Assert.Equal(GebaeudeZielfelder.FLAECHE_GRUND, kd.Summenfeld);
                Nah(GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_UNTEN, kd.Bauteil.Neigung);
                Nah(0.4, kd.Bauteil.U_Wert);
                GebaeudeBauteilzeile od = v.Zeilen.Single(z => z.Kennung == Bauteil(a, "Oberste Decke CAD").Kennung);
                Assert.Equal(GebaeudeZielfelder.FLAECHE_DACH, od.Summenfeld);
                Nah(GebaeudeZonenuebernahme.NEIGUNG_WAAGERECHT_OBEN, od.Bauteil.Neigung);
                Assert.DoesNotContain(v.Zeilen, z => z.Kennung == Bauteil(a, "Spitzbodenwand CAD").Kennung);
                Nah(0.0, v.Zeilen.Single(z => z.Kennung == Bauteil(a, "Nord CAD").Kennung).Bauteil.Azimut);   // 0° = Nord
            }
        }

        [Fact]
        public void Gegenprobe_die_uebrigen_Proben_bleiben_ohne_die_neuen_Meldungen()
        {
            IEnumerable<string> proben = IfcProbenErzeuger.Alle().Keys.Where(k => k != PROBE && k != "ifc2x3_referenzen.ifc" && k != "ifc4_z6_cad.ifc").Append("ifc4_verlust.ifc");
            foreach (string probe in proben)
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), probe);
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                foreach (string k in NEUE_SCHLUESSEL)
                    Assert.True(a.Meldungen.All(m => m.Schluessel != P + k), probe + ": " + k);
                if (a.Abbild != null)
                    Assert.All(a.Abbild.Gebaeude.SelectMany(g => g.Bauteile), b => Assert.Null(b.ZonenbodenOhneNachbar));
            }
        }

        [Theory]
        [InlineData("ThermalTransmittance (W/(m K))", "ThermalTransmittance", "W/(m K)", false)]
        [InlineData("UValue (W/(m² K))", "UValue", "W/(m² K)", true)]
        [InlineData("ThermalTransmittance", "ThermalTransmittance", null, false)]
        [InlineData("UValue (W/m2K)", "UValue", "W/m2K", true)]
        [InlineData("UValue (W/(m^2·K))", "UValue", "W/(m^2·K)", true)]
        [InlineData("Orientation (°)", "Orientation", "°", false)]
        [InlineData("(W/(m² K))", "(W/(m² K))", null, false)]
        public void Name_und_Einheit_werden_getrennt(string roh, string name, string einheit, bool uEinheit)
        {
            Assert.Equal(name, IfcEigenschaften.NameOhneEinheit(roh, out string e));
            Assert.Equal(einheit, e);
            Assert.Equal(uEinheit, IfcEigenschaften.IstUWertEinheit(e));
        }

        [Fact]
        public void Die_Angrenzung_gilt_erst_nach_IsExternal_und_den_Raumgrenzen()
        {
            var keine = new List<Xbim.Ifc4.Interfaces.IIfcRelSpaceBoundary>();
            Assert.Equal(Randbedingung.Innen, IfcAbbildBauer.Rand(false, keine, false, false, Randbedingung.Aussenluft));
            Assert.Equal(Randbedingung.Aussenluft, IfcAbbildBauer.Rand(true, keine, false, false, Randbedingung.Unbeheizt));
            Assert.Equal(Randbedingung.Unbeheizt, IfcAbbildBauer.Rand(null, keine, true, false, Randbedingung.Unbeheizt));
            Assert.Equal(Randbedingung.Aussenluft, IfcAbbildBauer.Rand(null, keine, true, false));
            Assert.Equal(Randbedingung.Unbekannt, IfcAbbildBauer.Rand(null, keine, false, false));
            Assert.Equal(6, IfcAbbildBauer.ANGRENZUNG_ABBILDUNG.Count);
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Die_neuen_Meldungen_haben_Texte_in_beiden_Sprachen(string kultur)
        {
            CultureInfo c = CultureInfo.GetCultureInfo(kultur);
            foreach (string schluessel in NEUE_SCHLUESSEL)
            {
                string text = R.ResourceManager.GetString(P + schluessel, c);
                Assert.False(string.IsNullOrWhiteSpace(text), schluessel + " fehlt in " + kultur);
                Assert.Contains("{0}", text);
                Assert.NotEqual(R.ResourceManager.GetString(P + schluessel, CultureInfo.GetCultureInfo("de-DE")),
                                R.ResourceManager.GetString(P + schluessel, CultureInfo.GetCultureInfo("en-US")));
            }
        }

        // ------------------------------------------------------------------
        //  Zweiseitige Randbedingung (Konzept HottCAD-Verbund 3.1)
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("btaOutside", (int)Randbedingung.Aussenluft, null)]
        [InlineData("btaGround", (int)Randbedingung.Erdreich, null)]
        [InlineData("btaHeated", (int)Randbedingung.Innen, null)]
        [InlineData("btaUnHeated", (int)Randbedingung.Unbeheizt, null)]
        [InlineData("btaCellarCeiling", (int)Randbedingung.Unbeheizt, AbbildBauteil.BELEG_KELLERDECKE)]
        [InlineData("BTAUPPERMOSTSTOREY", (int)Randbedingung.Unbeheizt, AbbildBauteil.BELEG_OBERSTE_DECKE)]
        public void Die_Codes_der_Seiten_werden_abgebildet(string code, int rand, string beleg)
        {
            var s = IfcAbbildBauer.SeiteAbbilden(code);
            Assert.True(s.HasValue);
            Assert.Equal((Randbedingung)rand, s.Value.Rand);
            Assert.Equal(beleg, s.Value.Beleg);
        }

        [Theory]
        [InlineData("btaNone")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("btaIrgendwas")]
        public void Ohne_Code_hat_die_Seite_keinen_Wert(string code) => Assert.Null(IfcAbbildBauer.SeiteAbbilden(code));

        [Theory]
        [InlineData("btaHeated", "btaUnHeated", 1)]
        [InlineData("btaOutside", "btaHeated", 0)]
        [InlineData("btaHeated", "btaHeated", 0)]
        [InlineData("btaNone", "btaHeated", 1)]
        [InlineData("btaNone", "btaNone", -1)]
        [InlineData("btaHeated", "btaCellarCeiling", 1)]
        public void Die_wirksame_Seite_ist_die_nicht_beheizte(string a, string b, int erwartet)
            => Assert.Equal(erwartet, IfcAbbildBauer.WirksameSeite(new[] { IfcAbbildBauer.SeiteAbbilden(a), IfcAbbildBauer.SeiteAbbilden(b) }));

        [Fact]
        public void Die_Seiten_der_Probe_stehen_am_Bauteil_und_die_wirksame_Seite_entscheidet()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_z6_cad.ifc");
            AbbildBauteil decke = Bauteil(a, "Decke EG/OG"), dach = Bauteil(a, "Oberste Decke"), boden = Bauteil(a, "Bodenplatte");
            AbbildBauteil sued = Bauteil(a, "Außenwand Süd"), nord = Bauteil(a, "Außenwand Nord"), dusche = Bauteil(a, "Wand Dusche");

            Assert.Equal(Randbedingung.Innen, decke.RandbedingungSeiteA);
            Assert.Equal(Randbedingung.Innen, decke.RandbedingungSeiteB);
            Assert.Equal(Randbedingung.Innen, decke.RandbedingungWirksam);
            Assert.Null(decke.OrientierungSeiteA);

            Assert.Equal(Randbedingung.Innen, dach.RandbedingungSeiteA);
            Assert.Equal(Randbedingung.Unbeheizt, dach.RandbedingungSeiteB);
            Assert.Equal(Randbedingung.Unbeheizt, dach.RandbedingungWirksam);
            Assert.Equal(AbbildBauteil.BELEG_OBERSTE_DECKE, dach.RandbedingungBeleg);
            Assert.Equal(Randbedingung.Unbeheizt, dach.Randbedingung);

            Assert.Equal(Randbedingung.Erdreich, boden.RandbedingungWirksam);
            Assert.Null(boden.RandbedingungSeiteB);

            Assert.Equal(Randbedingung.Aussenluft, sued.RandbedingungWirksam);
            Nah(180.0, sued.OrientierungSeiteA);
            Assert.Null(sued.OrientierungSeiteB);
            Assert.Equal(Randbedingung.Aussenluft, sued.Randbedingung);
            Nah(0.0, nord.OrientierungSeiteA);
            Assert.Null(nord.RandbedingungSeiteB);

            // Ohne Seiten: der Rückfall auf die Angrenzung des allgemeinen Satzes, die Seitenfelder bleiben leer.
            Assert.Null(dusche.RandbedingungSeiteA);
            Assert.Null(dusche.RandbedingungWirksam);
            Assert.Equal(Randbedingung.Innen, dusche.Randbedingung);
        }
    }
}
