using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7a, Welle 2 — die Abbildung EPOS → Abbild ohne Datenbank</b> (Umsetzungsauftrag G7a, W2):
    /// der Rundlauf Satz → Ablauf → Schreiber → Leser → Bauteilvorschlag, Zeile gegen Zeile mit den
    /// wirksamen Werten; Innenpaare, Öffnungen im Wirt, Übergangsfälle, Luftschichten; die
    /// Ersatzschichtung (Σκ·A = C, U = U, Bänder, Grenzfälle, Vorbehalt, Namen); Kühlbedingung,
    /// Testlizenz, Ort, Klassenweg, Trennfläche zur Nachbarzone, Ablehnungen; die Verlustliste per
    /// Reflexion und die Stabilität der Kennungen.
    /// </summary>
    public sealed class GebaeudeExportAblaufTests
    {
        private const double Genau = 1e-6;
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeExportAblaufTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        // ==================================================================
        //  Rundlauf Zeile gegen Zeile
        // ==================================================================

        [Fact]
        public void Schichtenhaus_kehrt_Zeile_gegen_Zeile_mit_den_wirksamen_Werten_zurueck()
        {
            GebaeudeExportSatz satz = ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus());
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(satz);
            GebaeudeBauteilvorschlag v = ExportSatzProbe.Rueckimport(ExportSatzProbe.Datei(plan), out _);

            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == "IMP_BAUTEIL_PROT_U_ABWEICHUNG");
            Assert.Equal(ExportSatzProbe.NUTZFLAECHE, v.Zone.Nutzflaeche);

            foreach (BauteilModel b in satz.Zonen[0].Bauteile)
            {
                bool oeffnung = b.Bauteilart == DbWerte.BAUTEILART_FENSTER || b.Bauteilart == DbWerte.BAUTEILART_TUER;
                string kennung = (oeffnung ? "epos-oeffnung-" : "epos-bauteil-") + b.ID.ToString(CultureInfo.InvariantCulture);
                if (b.ID == 1011) kennung = "epos-bauteil-1010";   // Innenpaar: eine Fläche nach der kleineren ID
                List<GebaeudeBauteilzeile> rueck = v.Zeilen.Where(z => z.Kennung == kennung).ToList();
                Assert.True(rueck.Count > 0, "keine Zeile zu " + kennung);

                Bauteilart art = GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart).Value;
                Bauteilrand rand = GebaeudeZonenabbildung.RandAusZeile(art, b.Randbedingung).Value;
                double neigung = b.Neigung ?? BauteilEingang.VorgabeNeigung(art);
                Umkehrzelle zelle = GbxmlUmkehrung.Zelle(art, b.Randbedingung, neigung);

                // Innen: zwei Seiten (Paar: je volle Fläche; Halbzeile: je halbe Fläche).
                double flaeche = b.ID == 1012 ? b.Flaeche / 2.0 : b.Flaeche;
                // Der Partner eines Innenpaars ist die Seite B der gemeinsamen Fläche (gespiegelter Aufbau).
                string seite = b.ID == 1011 ? "B" : "A";
                GebaeudeBauteilzeile z = rand == Bauteilrand.Innen ? rueck.Single(x => x.Seite == seite) : Assert.Single(rueck);
                if (rand == Bauteilrand.Innen) Assert.Equal(2, rueck.Count);
                Assert.Equal(GebaeudeZonenabbildung.ArtFuerZeile(zelle.RueckArt.Value), z.Bauteil.Bauteilart);
                Assert.Equal(zelle.RueckRand, GebaeudeZonenabbildung.RandAusZeile(z.Bauteil.Bauteilart, z.Bauteil.Randbedingung));
                Nah(flaeche, z.Bauteil.Flaeche, kennung + " Fläche");
                Nah(neigung, z.Bauteil.Neigung, kennung + " Neigung");
                if (b.Azimut.HasValue) Nah(b.Azimut.Value, z.Bauteil.Azimut, kennung + " Azimut");
                // Der Import nennt die beiden Seiten einer Innenfläche „(Seite A)" und „(Seite B)"; die Fläche
                // eines Paars trägt den Namen der Zeile mit der kleineren Id (der Name des Partners ist benannter Verlust).
                if (rand == Bauteilrand.Innen)
                    Assert.StartsWith((b.ID == 1011 ? "Bauteil 1010" : b.Bezeichner) + " (Seite ", z.Bauteil.Bezeichner, StringComparison.Ordinal);
                else Assert.Equal(b.Bezeichner, z.Bauteil.Bezeichner);

                double uQuelle = WirksamesU(b, rand, neigung, satz.Aufbauten);
                double uRueck = WirksamesU(z.Bauteil, rand == Bauteilrand.Innen ? Bauteilrand.Innen : zelle.RueckRand.Value, neigung, v.AufbautenJeId);
                Nah(uQuelle, uRueck, kennung + " U");
                if (b.Bauteilart == DbWerte.BAUTEILART_FENSTER) Nah(0.6, z.Bauteil.g_Wert, kennung + " g");
            }

            // Aufbau mit Innendämmung, Seite des Raums zuerst: Gipskarton innen, Putz außen; die ruhende
            // Luftschicht kehrt über den Namensabgleich als Luftschicht mit äquivalentem λ zurück.
            GebaeudeBauteilzeile sued = v.Zeilen.Single(z => z.Kennung == "epos-bauteil-1001");
            BauteilaufbauModel a = v.AufbautenJeId[sued.Bauteil.ID_Aufbau.Value];
            Assert.Equal(5, a.Schichten.Count);
            Assert.Equal(0.0125, a.Schichten[0].Dicke);
            Assert.Equal(0.02, a.Schichten[4].Dicke);
            Assert.True(a.Schichten[2].IstLuftschicht, "Die ruhende Luftschicht kehrt nicht als Luftschicht zurück.");
            Assert.Null(a.Schichten[2].Rho);
            _ausgabe.WriteLine("Ruhende Luftschicht: zurück als Luftschicht mit λ = " + a.Schichten[2].Lambda?.ToString("R", CultureInfo.InvariantCulture)
                               + " W/(mK) (Kandidat (i): Dicke und Widerstand, Name trifft N6).");
        }

        /// <summary>
        /// <b>Die Messung des Luftschicht-Rückwegs</b> (Umsetzungsauftrag 2.3): Kandidat (i) — Dicke und
        /// Widerstand nach Tabelle 8, Name gleich dem Bezeichner — gegen Kandidat (ii) — äquivalent
        /// λ = d/R, ρ = 5 kg/m³, c = 1 000 J/(kgK). Beide kehren mit dem Namensabgleich vollständig und mit
        /// demselben U zurück; nur (i) als Luftschicht ohne Masse, (ii) als leichte Schicht mit Masse.
        /// Festgeschrieben ist (i). Ohne Namensabgleich kehrt (i) masselos zurück (kein Aufbau).
        /// </summary>
        [Fact]
        public void Ruhende_Luftschicht_beide_Kandidaten_gemessen()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()));
            byte[] kandidatI = ExportSatzProbe.Datei(plan);
            foreach (AbbildSchicht s in plan.Abbild.Gebaeude[0].Bauteile.SelectMany(b => new[] { b.Aufbau }.Concat(b.Oeffnungen.Select(o => o.Aufbau)))
                                              .Where(a => a != null).SelectMany(a => a.Schichten)
                                              .Where(s => s.RWertM2KW.HasValue && s.DickeM.HasValue && !s.LambdaWmK.HasValue))
            {
                s.LambdaWmK = s.DickeM / s.RWertM2KW;
                s.RhoKgM3 = GebaeudeFestwerte.ROHDICHTE_MIN_KGM3;
                s.CpJkgK = 1000.0;
                s.RWertM2KW = null;
            }
            byte[] kandidatII = ExportSatzProbe.Datei(plan);

            BauteilschichtModel Luft(GebaeudeBauteilvorschlag v)
            {
                GebaeudeBauteilzeile z = v.Zeilen.Single(x => x.Kennung == "epos-bauteil-1001");
                Assert.True(z.Bauteil.ID_Aufbau.HasValue, "Der Aufbau kehrt nicht vollständig zurück.");
                return v.AufbautenJeId[z.Bauteil.ID_Aufbau.Value].Schichten[2];
            }
            GebaeudeBauteilvorschlag vI = ExportSatzProbe.Rueckimport(kandidatI, out GbxmlAbbild abbildI);
            GebaeudeBauteilvorschlag vII = ExportSatzProbe.Rueckimport(kandidatII, out _);
            BauteilschichtModel lI = Luft(vI), lII = Luft(vII);
            Assert.True(lI.IstLuftschicht);
            Assert.Null(lI.Rho);
            Assert.False(lII.IstLuftschicht);
            Assert.Equal(GebaeudeFestwerte.ROHDICHTE_MIN_KGM3, lII.Rho);
            double uI = vI.Zeilen.Single(x => x.Kennung == "epos-bauteil-1001").USchichten.Value;
            double uII = vII.Zeilen.Single(x => x.Kennung == "epos-bauteil-1001").USchichten.Value;
            Assert.Equal(uI, uII, 12);

            // Ohne Namensabgleich: Kandidat (i) ist ein masseloser Aufbau — die Zeile trägt nur U.
            GebaeudeBauteilvorschlag ohne = GebaeudeBauteilvorschlag.Bilden(abbildI, 0, 'E', null, new GbxmlImportProfil());
            Assert.False(ohne.Zeilen.Single(x => x.Kennung == "epos-bauteil-1001").Bauteil.ID_Aufbau.HasValue);
            _ausgabe.WriteLine("Luftschicht-Rückweg: (i) mit Namensabgleich vollständig als Luftschicht (ρ leer), U = "
                               + uI.ToString("R", CultureInfo.InvariantCulture) + "; (ii) vollständig als Schicht mit ρ = 5, U gleich; "
                               + "(i) ohne Namensabgleich masselos. Festgeschrieben: (i).");
        }

        private static double WirksamesU(BauteilModel b, Bauteilrand rand, double neigung, IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten)
        {
            if (b.U_Wert.HasValue) return b.U_Wert.Value;
            Assert.True(b.ID_Aufbau.HasValue, b.Bezeichner + ": weder U noch Aufbau");
            IReadOnlyList<Schicht> s = GebaeudeZonenabbildung.AlsSchichten(aufbauten[b.ID_Aufbau.Value], b.Bezeichner);
            return Bauteilreduktion.UWertAusSchichten(s, neigung, rand, b.Bezeichner).U_WM2K;
        }

        private static void Nah(double erwartet, double? ist, string wo)
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(erwartet - ist.Value) <= Genau * Math.Max(1.0, Math.Abs(erwartet)),
                        wo + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Innenpaar_eine_Flaeche_zweimal_derselbe_Raum_Halbzeile_mit_Meldung()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()));
            List<AbbildBauteil> f = plan.Abbild.Gebaeude[0].Bauteile;
            Assert.DoesNotContain(f, b => b.Kennung == "epos-bauteil-1011");
            AbbildBauteil paar = f.Single(b => b.Kennung == "epos-bauteil-1010");
            Assert.Equal(new[] { "epos-raum-601", "epos-raum-601" }, paar.Nachbarn.Select(n => n.Kennung));
            Assert.Equal(40.0, paar.BruttoflaecheM2);
            AbbildBauteil halb = f.Single(b => b.Kennung == "epos-bauteil-1012");
            Assert.Equal(30.0, halb.BruttoflaecheM2);
            Assert.Equal(new[] { GbxmlVokabular.Ceiling, GbxmlVokabular.InteriorFloor }, halb.Nachbarn.Select(n => n.Sicht));
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.INNEN_HALBZEILE && m.Werte[0] == "Bauteil 1012");
        }

        [Fact]
        public void Oeffnungen_im_Wirt_der_Wirt_brutto_Tuer_mit_eigenem_U_Fenster_mit_Fenstertyp()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()));
            AbbildBauteil sued = plan.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "epos-bauteil-1001");
            Assert.Equal(26.0, sued.BruttoflaecheM2);
            Assert.Equal(new[] { "epos-oeffnung-1002", "epos-oeffnung-1003" }, sued.Oeffnungen.Select(o => o.Kennung));
            AbbildBauteil fenster = sued.Oeffnungen[0], tuer = sued.Oeffnungen[1];
            Assert.Equal(GbxmlVokabular.FixedWindow, fenster.Quellart);
            Assert.Equal("epos-fenstertyp-1002", fenster.FenstertypKennung);
            Assert.Equal(0.6, fenster.GWert);
            Assert.Equal(GbxmlVokabular.NonSlidingDoor, tuer.Quellart);
            Assert.Null(tuer.FenstertypKennung);
            Assert.Equal(1.8, tuer.UWertWm2K);
            // Wand je Übergangsfall: Außenluft und unbeheizt tragen verschiedene Konstruktionen derselben Schichten.
            Assert.Equal("epos-aufbau-701-hor-al", sued.Aufbau.Kennung);
            AbbildBauteil treppe = plan.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "epos-bauteil-1009");
            Assert.Equal("epos-aufbau-701-hor-ub", treppe.Aufbau.Kennung);
            Assert.NotEqual(sued.Aufbau.UWertWm2K, treppe.Aufbau.UWertWm2K);
            Assert.Equal(sued.Aufbau.Schichten.Select(s => s.Kennung), treppe.Aufbau.Schichten.Select(s => s.Kennung));
            Assert.Equal(GbxmlVokabular.InteriorWall, treppe.Quellart);
            Assert.Equal(new[] { "epos-raum-601", "epos-unbeheizt-501" }, treppe.Nachbarn.Select(n => n.Kennung));
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.WECHSEL && m.Werte[1] == GbxmlUmkehrung.GRUND_AUSSENWAND_UNBEHEIZT);
        }

        [Fact]
        public void Oeffnung_ohne_gleiche_Lage_nimmt_den_Ersatzwirt_mit_Meldung_ohne_Wirt_Ablehnung()
        {
            ZoneModel z = ExportSatzProbe.Schichtenhaus();
            z.Bauteile.Single(b => b.ID == 1002).Azimut = 200.0;
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(z));
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.OEFFNUNG_ERSATZWIRT);

            ZoneModel ohne = ExportSatzProbe.Schichtenhaus();
            ohne.Bauteile.Single(b => b.ID == 1002).Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH;
            GebaeudeExportPlan abgelehnt = new GebaeudeExportAblauf().Vorbereiten(ExportSatzProbe.Satz(ohne), GbxmlExportProbe.Profil());
            Assert.True(abgelehnt.Abgelehnt);
            Assert.Equal(GebaeudeExportAblauf.OEFFNUNG_OHNE_WIRT, abgelehnt.Ablehnung.Schluessel);
            Assert.Null(abgelehnt.Abbild);
        }

        // ==================================================================
        //  Ersatzschichtung
        // ==================================================================

        [Fact]
        public void Ersatzschichtung_trifft_C_AW_und_C_IW_und_den_U_Wert()
        {
            GebaeudeExportSatz satz = ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus());
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(satz);
            List<AbbildBauteil> flaechen = plan.Abbild.Gebaeude[0].Bauteile;

            double cAw = GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN * ExportSatzProbe.BAUWEISE_WHK * 3600.0;
            double cIw = (1.0 - GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN) * ExportSatzProbe.BAUWEISE_WHK * 3600.0;

            // Σκ·A über die EPOS-Flächen der Außengruppe (netto; der Wirt ist brutto geschrieben).
            double summeAussen = 0.0;
            foreach (BauteilModel b in satz.Zonen[0].Bauteile.Where(b => b.Bauteilart != DbWerte.BAUTEILART_FENSTER))
            {
                AbbildBauteil f = flaechen.Single(x => x.Kennung == "epos-bauteil-" + b.ID.ToString(CultureInfo.InvariantCulture));
                AbbildSchicht s = Assert.Single(f.Aufbau.Schichten);
                Assert.True(f.Aufbau.IstErsatz);
                summeAussen += s.RhoKgM3.Value * s.CpJkgK.Value * s.DickeM.Value * b.Flaeche;
                // U aus der Ersatzschicht = U des Bauteils.
                Bauteilart art = GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart).Value;
                (double rSi, double rSe) = Bauteilreduktion.Uebergangswiderstaende(b.Neigung.Value, GebaeudeZonenabbildung.RandAusZeile(art, b.Randbedingung).Value);
                Assert.Equal(b.U_Wert.Value, 1.0 / (rSi + s.DickeM.Value / s.LambdaWmK.Value + rSe), 12);
                Assert.Equal(b.U_Wert, f.Aufbau.UWertWm2K);
                // Bänder.
                Assert.InRange(s.DickeM.Value, GebaeudeFestwerte.SCHICHT_DICKE_MIN_M, GebaeudeFestwerte.SCHICHT_DICKE_MAX_M);
                Assert.InRange(s.LambdaWmK.Value, GebaeudeFestwerte.LAMBDA_MIN_WMK, GebaeudeFestwerte.LAMBDA_MAX_WMK);
                Assert.InRange(s.RhoKgM3.Value, GebaeudeFestwerte.ROHDICHTE_MIN_KGM3, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3);
            }
            Assert.True(Math.Abs(summeAussen - cAw) <= 1e-9 * cAw, "Σκ·A = " + summeAussen + ", C_AW = " + cAw);

            // Die Ersatzfläche innerer Masse: A_IW/2, zweimal derselbe Raum; κ·A_IW = C_IW.
            AbbildBauteil innen = flaechen.Single(x => x.Kennung == "epos-innenmasse-501");
            double aIw = GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR * ExportSatzProbe.NUTZFLAECHE;
            Assert.Equal(aIw / 2.0, innen.BruttoflaecheM2.Value, 12);
            Assert.Equal(new[] { "epos-raum-601", "epos-raum-601" }, innen.Nachbarn.Select(n => n.Kennung));
            AbbildSchicht si = Assert.Single(innen.Aufbau.Schichten);
            Assert.Equal(GebaeudeExportAblauf.LAMBDA_INNEN_ERSATZ_WMK, si.LambdaWmK);
            double summeInnen = si.RhoKgM3.Value * si.CpJkgK.Value * si.DickeM.Value * aIw;
            Assert.True(Math.Abs(summeInnen - cIw) <= 1e-9 * cIw, "κ·A_IW = " + summeInnen + ", C_IW = " + cIw);

            // Rückimport: jede Zeile mit Ersatzaufbau und demselben wirksamen U; die Innengruppe als Bauteile.
            GebaeudeBauteilvorschlag v = ExportSatzProbe.Rueckimport(ExportSatzProbe.Datei(plan), out _);
            Assert.Equal(Innenweg.Bauteile, v.Innenweg);
            foreach (BauteilModel b in satz.Zonen[0].Bauteile.Where(b => b.Bauteilart != DbWerte.BAUTEILART_FENSTER))
            {
                GebaeudeBauteilzeile z = v.Zeilen.Single(x => x.Kennung == "epos-bauteil-" + b.ID.ToString(CultureInfo.InvariantCulture));
                Assert.True(z.Bauteil.ID_Aufbau.HasValue, b.Bezeichner + " kommt ohne Ersatzaufbau zurück");
                Nah(b.U_Wert.Value, z.USchichten, b.Bezeichner + " U");
            }
            Assert.Equal(2, v.Zeilen.Count(x => x.Kennung == "epos-innenmasse-501"));
        }

        [Fact]
        public void Vorbehalt_woertlich_in_Beschreibung_Stoffname_und_Meldung()
        {
            const string VORBEHALT = "trifft U-Wert und Gesamtwärmekapazität, nicht die Lage der Masse";
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus()));
            AbbildAufbau a = plan.Abbild.Gebaeude[0].Bauteile.First(b => b.Aufbau.IstErsatz).Aufbau;
            Assert.Contains(VORBEHALT, a.Beschreibung, StringComparison.Ordinal);
            Assert.Contains(VORBEHALT, a.Schichten[0].Name, StringComparison.Ordinal);
            PruefMeldung m = Assert.Single(plan.Meldungen, x => x.Schluessel == GebaeudeExportAblauf.ERSATZSCHICHTUNG);
            Assert.Equal(VORBEHALT, m.Werte[1]);

            string text = Encoding.UTF8.GetString(ExportSatzProbe.Datei(plan));
            Assert.Contains("<Description>Ersatzschichtung aus U-Wert und Speichermasse des Rechenkerns; sie " + VORBEHALT + ".</Description>", text, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Die_Namen_der_Ersatzstoffe_trifft_der_Namensabgleich_nie(string sprache)
        {
            var abgleich = new Baustoffabgleich(BaustoffabgleichDaten.AusSaat());
            var kultur = CultureInfo.GetCultureInfo(sprache);
            string vorbehalt = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEXP_DATEI_VORBEHALT", kultur);
            string[] namen =
            {
                string.Format(kultur, WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEXP_DATEI_ERSATZ_STOFF", kultur), vorbehalt),
                WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEXP_DATEI_ERSATZ_MASSELOS", kultur),
            };
            foreach (string name in namen)
            {
                Abgleichtreffer t = abgleich.Abgleichen(name);
                Assert.False(t.Getroffen, name + " trifft " + t.Baustoff?.Bezeichner);
                Assert.Equal(Abgleichsonderfall.Keiner, t.Sonderfall);
                Assert.DoesNotMatch(new Regex(@"\d{5,}\s*$"), name);
            }
            // Die Luftschicht ohne Baustoff trägt einen Namen, den N6 trifft.
            Assert.Equal(Abgleichsonderfall.Luftschicht,
                         abgleich.Abgleichen(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEXP_DATEI_LUFTSCHICHT", kultur)).Sonderfall);
        }

        [Theory]
        [InlineData(0.3, 21600.0, 0.13, 0.04, true, null)]               // λ-Untergrenze bindet
        [InlineData(1.0, 300000.0, 0.13, 0.04, true, null)]              // Vorzug 1 500 kg/m³ im Band
        [InlineData(7.0, 21600.0, 0.13, 0.04, false, "R_NICHT_POSITIV")] // 1/U < R_si + R_se
        [InlineData(0.3, 0.0, 0.13, 0.04, false, "KEINE_KAPAZITAET")]
        [InlineData(0.3, 1.0e9, 0.13, 0.04, false, "BAENDER_OHNE_SCHNITT")] // κ zu groß für d ≤ 1 m und ρ ≤ 8 000
        public void Ersatzschicht_in_den_Baendern_und_ihre_Grenzfaelle(double u, double kappa, double rSi, double rSe, bool mitMasse, string grund)
        {
            GebaeudeExportAblauf.Ersatzwerte w = GebaeudeExportAblauf.Ersatzschicht(u, kappa, rSi, rSe);
            Assert.Equal(mitMasse, w.MitMasse);
            Assert.Equal(grund, w.Grund);
            if (!mitMasse) return;
            double r = 1.0 / u - rSi - rSe;
            Assert.Equal(u, 1.0 / (rSi + w.DickeM / w.LambdaWmK + rSe), 12);
            Assert.Equal(kappa, w.RhoKgM3 * w.CpJkgK * w.DickeM, 6);
            Assert.InRange(w.DickeM, 0.001, 1.0);
            Assert.InRange(w.LambdaWmK, GebaeudeFestwerte.LAMBDA_MIN_WMK, GebaeudeFestwerte.LAMBDA_MAX_WMK);
            Assert.InRange(w.RhoKgM3, GebaeudeFestwerte.ROHDICHTE_MIN_KGM3, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3);
            double vorzug = kappa / (GebaeudeExportAblauf.RHO_VORZUG_KGM3 * GebaeudeExportAblauf.CP_ERSATZ_JKGK);
            if (vorzug >= GebaeudeFestwerte.LAMBDA_MIN_WMK * r) Assert.Equal(vorzug, w.DickeM, 12);
        }

        [Fact]
        public void Gemischte_Aussengruppe_schreibt_das_Bauteil_ohne_Schichten_masselos_mit_Meldung()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()));
            AbbildBauteil tuer = plan.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "epos-bauteil-1001").Oeffnungen[1];
            AbbildSchicht s = Assert.Single(tuer.Aufbau.Schichten);
            Assert.Null(s.DickeM);
            Assert.True(s.RWertM2KW > 0.0);
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.MASSELOS && m.Werte[0] == "Bauteil 1003"
                                                 && m.Werte[1] == GebaeudeExportAblauf.GRUND_GEMISCHT);
        }

        // ==================================================================
        //  Räume, Zonen, Kopf
        // ==================================================================

        [Theory]
        [InlineData(false, true, "Heated", false)]
        [InlineData(true, false, "Heated", false)]
        [InlineData(true, true, "HeatedAndCooled", true)]
        public void Gekuehlt_nur_mit_Projektschalter_und_Kuehlschalter_der_Zone(bool projekt, bool zone, string zustand, bool designCool)
        {
            ZoneModel z = ExportSatzProbe.Schichtenhaus();
            z.Kuehlung_Aktiv = zone;
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(z, kuehlbetrieb: projekt));
            AbbildRaum r = plan.Abbild.Gebaeude[0].Raeume[0];
            Assert.Equal(zustand, r.Zustandsangabe);
            Assert.Equal(designCool, r.SollKuehlenC.HasValue);
            if (designCool) Assert.Equal(26.0, r.SollKuehlenC);
        }

        [Fact]
        public void Raum_traegt_die_wirksamen_Werte_der_Zone_und_des_Gebaeudes()
        {
            ZoneModel z = ExportSatzProbe.Schichtenhaus();
            z.Nutzflaeche = 60.0;
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(z));
            AbbildRaum r = plan.Abbild.Gebaeude[0].Raeume[0];
            Assert.Equal("epos-raum-601", r.Kennung);
            Assert.Equal("epos-zone-601", r.ZonenKennung);
            Assert.Equal(60.0, r.FlaecheM2);
            Assert.Equal(150.0, r.VolumenM3);
            Assert.Equal(0.2, r.LuftwechselJeH);
            Assert.Equal(1.5, r.Personen);                     // 60 m² / 40 m² je Nutzer
            Assert.Equal(600.0 * 60.0 / 120.0 / 60.0, r.GeraeteWm2);   // Anteil nach dem Flächenschlüssel, je m²
            Assert.Equal(20.0, r.SollHeizenC);
            Assert.Contains("Mittelwert ohne Zeitplan", r.Beschreibung, StringComparison.Ordinal);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEB_PRODUKTAUSWEIS_VDI6007", CultureInfo.GetCultureInfo("de-DE")),
                         r.ZonenBeschreibung);
        }

        [Fact]
        public void Kopf_Produktausweis_Testlizenz_Ort_und_Gebaeudeart()
        {
            GebaeudeExportPlan mitPlz = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()),
                                                             GbxmlExportProbe.Profil(testlizenz: true));
            AbbildGebaeude g = mitPlz.Abbild.Gebaeude[0];
            Assert.Equal("SingleFamily", g.Art);
            Assert.StartsWith("Erstellt mit einer Testversion von EPOS-Plan.", g.Beschreibung, StringComparison.Ordinal);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("GEB_PRODUKTAUSWEIS_VDI6007", CultureInfo.GetCultureInfo("de-DE")),
                            g.Beschreibung, StringComparison.Ordinal);
            Assert.Contains("Klimaregion des Projekts: Probenregion (nicht der Standort).", g.Beschreibung, StringComparison.Ordinal);
            Assert.DoesNotContain("Postleitzahl", g.Beschreibung, StringComparison.Ordinal);
            Assert.Equal("01067", mitPlz.Abbild.Plz);
            Assert.Contains(mitPlz.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.TESTLIZENZ);

            ProjektGebaeudeModel geb = ExportSatzProbe.Gebaeude();
            geb.Gebaeudeart = "Gewerbe";
            GebaeudeExportPlan ohnePlz = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus(), plz: null, gebaeude: geb));
            Assert.Equal("Unknown", ohnePlz.Abbild.Gebaeude[0].Art);
            Assert.Contains(ohnePlz.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.GEBAEUDEART_UNBEKANNT && m.Werte[0] == "Gewerbe");
            Assert.Contains("Ohne Standort und ohne Nordrichtung", ohnePlz.Abbild.Gebaeude[0].Beschreibung, StringComparison.Ordinal);
            Assert.DoesNotContain("Testversion", ohnePlz.Abbild.Gebaeude[0].Beschreibung, StringComparison.Ordinal);
            Assert.Contains(ohnePlz.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.OHNE_ORT);
            XDocument d = XDocument.Load(new MemoryStream(ExportSatzProbe.Datei(ohnePlz)));
            Assert.Empty(d.Descendants(XName.Get("Location", GbxmlLeser.NAMENSRAUM)));
        }

        [Fact]
        public void Die_Dateitexte_folgen_der_Sprache_des_Profils_die_Zahlen_nicht()
        {
            GebaeudeExportSatz satz = ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus());
            byte[] de = ExportSatzProbe.Datei(ExportSatzProbe.Plan(satz, GbxmlExportProbe.Profil(sprache: "de-DE")), GbxmlExportProbe.Profil(sprache: "de-DE"));
            byte[] en = ExportSatzProbe.Datei(ExportSatzProbe.Plan(satz, GbxmlExportProbe.Profil(sprache: "en-US")), GbxmlExportProbe.Profil(sprache: "en-US"));
            Assert.Contains("Ersatzschichtung", Encoding.UTF8.GetString(de), StringComparison.Ordinal);
            Assert.Contains("Substitute layering", Encoding.UTF8.GetString(en), StringComparison.Ordinal);
            Assert.Equal(GbxmlExportProbe.Zahlen(de), GbxmlExportProbe.Zahlen(en));
        }

        // ==================================================================
        //  Klassenweg, Trennfläche, Ablehnungen
        // ==================================================================

        internal static GebaeudeExportSatz Klassenweg(bool ok)
        {
            ProjektGebaeudeModel g = ExportSatzProbe.Gebaeude();
            g.Flaeche_Außenwand = 100.0; g.k_Wert_Außenwand = 0.4; g.Dachflaeche = 80.0; g.k_Wert_Dachflaeche = 0.3;
            g.Grundflaeche = 80.0; g.k_Wert_Grundflaeche = 0.5; g.gesamte_Fensterflaeche = 20.0; g.Fensterflaeche_Sued = 10.0;
            g.Fensterflaeche_Nord = 4.0; g.Fensterflaeche_OstWest = 6.0; g.k_Wert_Fenster = 1.3;
            ZoneModel zone = GebaeudeZonenabbildung.AlsZoneModel(GebaeudeZonenuebernahme.AlsEineZone(g, 1.5));
            var u = new GebaeudeZonenCtrl.Uebernahmevorschlag(ok, ok ? "" : "kein Klima", ok ? 1.5 : double.NaN, ok ? zone : null,
                                                              g.Nutzflaeche, GebaeudeVorbereitung.EINHEIT_FLAECHE, 180.0);
            return new GebaeudeExportSatz
            {
                IdProjekt = 1, IdZ = 1, Gebaeude = g, Uebernahme = u,
                Uebernahmeprotokoll = new[] { "Simulation Hinweis: Probe" }, UebernahmeLaufzeitMs = 12.4,
                Plz = "01067", Klimaregion = "Probenregion",
            };
        }

        [Fact]
        public void Klassenweg_exportiert_den_Uebernahmevorschlag_mit_Faktor_und_Grundlage()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(Klassenweg(true));
            AbbildGebaeude g = plan.Abbild.Gebaeude[0];
            Assert.Equal("epos-raum-klasse-501", g.Raeume[0].Kennung);
            Assert.Equal("epos-zone-klasse-501", g.Raeume[0].ZonenKennung);
            Assert.Equal(1.5 * ExportSatzProbe.NUTZFLAECHE, g.Raeume[0].FlaecheM2);
            Assert.All(g.Bauteile.Where(b => b.Kennung.StartsWith("epos-klasse-", StringComparison.Ordinal)),
                       b => Assert.StartsWith("epos-aufbau-klasse-501-", b.Aufbau.Kennung, StringComparison.Ordinal));
            Assert.Contains(g.Bauteile, b => b.Kennung == "epos-innenmasse-501");
            Assert.Contains("mit dem Faktor 1,5 auf das Projekt hochgerechnet (Grundlage: Fläche)", g.Beschreibung, StringComparison.Ordinal);
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.UEBERNAHME_PROTOKOLL && m.Werte[0] == "Simulation Hinweis: Probe");
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.UEBERNAHME_LAUFZEIT && m.Werte[0] == "12");
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.KLASSENWEG);
            byte[] datei = ExportSatzProbe.Datei(plan);
            Assert.True(datei.Length > 0);
        }

        [Fact]
        public void Klassenweg_ohne_Uebernahme_benannt_abgelehnt()
        {
            GebaeudeExportPlan plan = new GebaeudeExportAblauf().Vorbereiten(Klassenweg(false), GbxmlExportProbe.Profil());
            Assert.True(plan.Abgelehnt);
            Assert.Equal(GebaeudeExportAblauf.UEBERNAHME_ABGELEHNT, plan.Ablehnung.Schluessel);
            Assert.Equal("kein Klima", plan.Ablehnung.Werte[0]);
        }

        [Fact]
        public void Trennflaeche_zur_Nachbarzone_zwei_Raeume_und_zurueck_als_innere_Masse()
        {
            ZoneModel a = ExportSatzProbe.Schichtenhaus();
            a.Nutzflaeche = 80.0;
            var b = new ZoneModel { ID = 602, ID_Gebaeude = ExportSatzProbe.GEB, Rang = 2, Bezeichner = "Anbau", Nutzflaeche = 40.0, IstBeheizt = true };
            a.Bauteile.Add(ExportSatzProbe.B(1013, DbWerte.BAUTEILART_INNENWAND, 12.0, DbWerte.RANDBEDINGUNG_ZONE,
                                             ExportSatzProbe.AUFBAU_DECKE, neigung: 90.0, nachbarzone: 602));
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(a, weitere: new[] { b }));
            AbbildBauteil t = plan.Abbild.Gebaeude[0].Bauteile.Single(x => x.Kennung == "epos-bauteil-1013");
            Assert.Equal(GbxmlVokabular.InteriorWall, t.Quellart);
            Assert.Equal(new[] { "epos-raum-601", "epos-raum-602" }, t.Nachbarn.Select(n => n.Kennung));
            Assert.Equal("epos-aufbau-702-hor-zo", t.Aufbau.Kennung);
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.WECHSEL && m.Werte[1] == GbxmlUmkehrung.GRUND_ZONE_INNEN);

            GebaeudeBauteilvorschlag v = ExportSatzProbe.Rueckimport(ExportSatzProbe.Datei(plan), out GbxmlAbbild zurueck);
            Assert.Equal(GebaeudeImportProfil.ZONENREGEL_X1, zurueck.Gebaeude[0].Zonenvorschlag);
            List<GebaeudeBauteilzeile> zeilen = v.Zeilen.Where(x => x.Kennung == "epos-bauteil-1013").ToList();
            Assert.Equal(2, zeilen.Count);
            Assert.All(zeilen, x => Assert.Null(x.Bauteil.Randbedingung));
            Assert.All(zeilen, x => Assert.Equal(DbWerte.BAUTEILART_INNENWAND, x.Bauteil.Bauteilart));

            // Ohne Nachbarzone benannt abgelehnt.
            a.Bauteile.Last().ID_Nachbarzone = null;
            GebaeudeExportPlan ohne = new GebaeudeExportAblauf().Vorbereiten(ExportSatzProbe.Satz(a, weitere: new[] { b }), GbxmlExportProbe.Profil());
            Assert.True(ohne.Abgelehnt);
            Assert.Equal(GebaeudeExportAblauf.UMKEHR_ABLEHNUNG, ohne.Ablehnung.Schluessel);
            Assert.Equal(GbxmlUmkehrung.GRUND_ZONE, ohne.Ablehnung.Werte[1]);
        }

        [Fact]
        public void Weniger_als_vier_Flaechen_ohne_Projektkopie_ungespeichert_benannt_abgelehnt()
        {
            ZoneModel z = ExportSatzProbe.UWertHaus();
            z.Bauteile.RemoveAll(b => b.ID >= 2002 && b.ID <= 2007 && b.ID != 2003);
            z.Bauteile.RemoveAll(b => b.ID == 2005);
            z.Bauteile.Add(ExportSatzProbe.B(2008, DbWerte.BAUTEILART_DACH, 120.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 0.2, neigung: 0.0));
            GebaeudeExportPlan wenig = new GebaeudeExportAblauf().Vorbereiten(ExportSatzProbe.Satz(z), GbxmlExportProbe.Profil());
            // Drei Flächen und die Ersatzfläche innerer Masse = vier: schreibbar. Ohne die Innenmasse wären es drei.
            Assert.False(wenig.Abgelehnt, wenig.Ablehnung?.ToString());

            z.Bauteile.RemoveAt(0);
            GebaeudeExportPlan drei = new GebaeudeExportAblauf().Vorbereiten(ExportSatzProbe.Satz(z), GbxmlExportProbe.Profil());
            Assert.True(drei.Abgelehnt);
            Assert.Equal(GebaeudeExportAblauf.ZU_WENIG_FLAECHEN, drei.Ablehnung.Schluessel);

            GebaeudeExportPlan ohne = new GebaeudeExportAblauf().Vorbereiten(new GebaeudeExportSatz { IdProjekt = 1, IdZ = 1 }, GbxmlExportProbe.Profil());
            Assert.Equal(GebaeudeExportAblauf.KEIN_GEBAEUDE, ohne.Ablehnung.Schluessel);

            ZoneModel neu = ExportSatzProbe.UWertHaus();
            neu.Bauteile[0].ID = -3;
            GebaeudeExportPlan ungespeichert = new GebaeudeExportAblauf().Vorbereiten(ExportSatzProbe.Satz(neu), GbxmlExportProbe.Profil());
            Assert.Equal(GebaeudeExportAblauf.BAUTEIL_UNGESPEICHERT, ungespeichert.Ablehnung.Schluessel);
        }

        // ==================================================================
        //  Kennungen (Probe 11 ohne Datenbank) und Verlustliste
        // ==================================================================

        [Fact]
        public void Kennungen_stabil_neu_gespeicherte_Schichten_gleich_anderes_Gebaeude_anders()
        {
            byte[] a = ExportSatzProbe.Datei(ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus())));
            byte[] b = ExportSatzProbe.Datei(ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus())));
            Assert.Equal(a, b);
            // Neu gespeichert: andere Schicht-IDs, dieselben Kennungen.
            byte[] c = ExportSatzProbe.Datei(ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus(),
                                                                                       ExportSatzProbe.Aufbauten(schichtId: 55000))));
            Assert.Equal(a, c);
            // Ein anderes Gebäude (dupliziertes Projekt): andere Kennungen.
            byte[] d = ExportSatzProbe.Datei(ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus(608),
                                                                                       gebaeude: ExportSatzProbe.Gebaeude(502))));
            HashSet<string> idsA = Kennungen(a), idsD = Kennungen(d);
            Assert.Contains("epos-gebaeude-501", idsA);
            Assert.Contains("epos-gebaeude-502", idsD);
            Assert.DoesNotContain("epos-gebaeude-501", idsD);
            Assert.All(idsA, k => Assert.StartsWith("epos-", k, StringComparison.Ordinal));
        }

        private static HashSet<string> Kennungen(byte[] datei)
        {
            XDocument d = XDocument.Load(new MemoryStream(datei));
            List<string> ids = d.Descendants().Attributes("id").Select(x => x.Value).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
            foreach (XAttribute r in d.Descendants().Attributes().Where(x => x.Name.LocalName.EndsWith("IdRef", StringComparison.Ordinal)))
                Assert.Contains(r.Value, ids);
            return new HashSet<string>(ids, StringComparer.Ordinal);
        }

        [Fact]
        public void Verlustliste_jedes_Feld_eingestuft_per_Reflexion()
        {
            Pruefen(typeof(ZoneModel), GebaeudeExportVerluste.Zone);
            Pruefen(typeof(BauteilModel), GebaeudeExportVerluste.Bauteil);
            Pruefen(typeof(ProjektGebaeudeModel), GebaeudeExportVerluste.Gebaeude);
            // Mindestens benannt (Umsetzungsauftrag W2).
            foreach (string n in new[] { "Luftwechsel_Nutzer", "Interne_Waermegewinne", "Raumsolltemperatur_Nachtabsenkung",
                                         "Raumsolltemperatur_Wochenende", "Raumsolltemperatur_Ferien", "Heizleistung_Max", "Kuehlleistung_Max",
                                         "Heizung_Strahlungsanteil", "Maximaleraumtemperatur", "Uebergabe_Art", "Kuehl_Uebergabe_Art", "Bewohner" })
                Assert.Equal(Exporteinstufung.BenannterVerlust, GebaeudeExportVerluste.Zone[n]);
            foreach (string n in new[] { "Psi_L", "Rahmenanteil", "Verschattungsfaktor" })
                Assert.Equal(Exporteinstufung.BenannterVerlust, GebaeudeExportVerluste.Bauteil[n]);
            foreach (string n in new[] { "Bauweise", "Masseanteil_Aussen", "Baualtersklasse", "Baujahr", "Kellertemperatur" })
                Assert.Equal(Exporteinstufung.BenannterVerlust, GebaeudeExportVerluste.Gebaeude[n]);
        }

        private static void Pruefen(Type typ, IReadOnlyDictionary<string, Exporteinstufung> tabelle)
        {
            string[] felder = typ.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name)
                                 .Concat(typ.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name)).ToArray();
            string[] ohne = felder.Where(f => !tabelle.ContainsKey(f)).ToArray();
            string[] zuviel = tabelle.Keys.Where(k => !felder.Contains(k)).ToArray();
            Assert.True(ohne.Length == 0, typ.Name + ": Felder ohne Einstufung: " + string.Join(", ", ohne));
            Assert.True(zuviel.Length == 0, typ.Name + ": Einstufung ohne Feld: " + string.Join(", ", zuviel));
        }

        [Fact]
        public void Die_getragenen_Verluste_stehen_vor_dem_Schreiben_in_einer_Meldung()
        {
            ZoneModel z = ExportSatzProbe.Schichtenhaus();
            z.Luftwechsel_Nutzer = 0.4;
            z.Bauteile[0].Psi_L = 2.5;
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(z));
            PruefMeldung m = Assert.Single(plan.Meldungen, x => x.Schluessel == GebaeudeExportAblauf.VERLUSTE);
            Assert.Contains("Luftwechsel_Nutzer", m.Werte[0], StringComparison.Ordinal);
            Assert.Contains("Psi_L", m.Werte[0], StringComparison.Ordinal);
            Assert.Contains("Raumsolltemperatur_Nachtabsenkung", m.Werte[0], StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Plan_ist_schemagueltig_Bauteilweg_und_Klassenweg()
        {
            string schema = GbxmlExportProbe.Schemakopie();
            if (schema == null)
            {
                _ausgabe.WriteLine("Schemaprüfung übersprungen: die Schemakopie liegt nicht bei (D17).");
                return;
            }
            foreach ((string name, GebaeudeExportSatz satz) in new[]
            {
                ("Schichtenhaus", ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus())),
                ("U-Wert-Haus ohne PLZ", ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus(), plz: null)),
                ("Klassenweg", Klassenweg(true)),
            })
            {
                string[] befunde = GbxmlExportProbe.Schemapruefung(schema, ExportSatzProbe.Datei(ExportSatzProbe.Plan(satz)));
                Assert.True(befunde.Length == 0, name + ":\n" + string.Join("\n", befunde));
            }
            _ausgabe.WriteLine("Schemaprüfung: Schichtenhaus, U-Wert-Haus ohne PLZ und Klassenweg gültig.");
        }
    }

}
