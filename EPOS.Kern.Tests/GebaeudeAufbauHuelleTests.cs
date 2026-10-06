using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// BA-3 (Konzept Bauteilaufbau beim Import 5.4): die Hülle des Bauteilaufbaus — Kennwerte des Steckbriefs gegen die
    /// <see cref="Bauteilreduktion"/> direkt, die Stufenregel der Oberfläche gegen die des Kerns, Stufen, Summen und Steckbriefe
    /// aus einem Importvorschlag und aus gespeicherten Bauteilen (ohne Datenbank), die Liste „Bauteilaufbauten".
    /// </summary>
    public class GebaeudeAufbauHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static BauteilaufbauModel Massivwand() => new BauteilaufbauModel
        {
            ID = 5, Bezeichner = "AW massiv", Herkunft = DbWerte.HERKUNFT_KATALOG,
            Schichten =
            {
                new BauteilschichtModel { Reihenfolge = 1, Dicke = 0.015, Lambda = 0.51, Rho = 1200, Cp = 1000, ID_Baustoff = 11 },
                new BauteilschichtModel { Reihenfolge = 2, Dicke = 0.2, Lambda = 2.0, Rho = 2400, Cp = 1000, ID_Baustoff = 12 },
                new BauteilschichtModel { Reihenfolge = 3, Dicke = 0.12, Lambda = 0.035, Rho = 30, Cp = 1400, ID_Baustoff = 13 },
            },
        };

        [Fact]
        public void Die_Kennwerte_des_Steckbriefs_sind_die_der_Bauteilreduktion()
        {
            BauteilaufbauModel a = Massivwand();
            var schichten = a.Schichten.Select(s => new Schicht(s.Dicke, s.Lambda.Value, s.Rho.Value, s.Cp.Value)).ToList();
            var k = GebaeudeAufbauHuelle.Kennwerte(a, 12.5, 90.0, Bauteilrand.Aussenluft, out string grund);
            Assert.Equal("", grund);
            Assert.NotNull(k);

            Waermestromrichtung richtung = Bauteilreduktion.RichtungAusNeigung(90.0);
            Bezugsperiodenwahl soll = Bauteilreduktion.BezugsperiodeWaehlen(schichten, 12.5, richtung);
            Bezugsperiodenwahl jeM2 = Bauteilreduktion.BezugsperiodeWaehlen(schichten, 1.0, richtung);
            Schichtkennwerte u = Bauteilreduktion.UWertAusSchichten(schichten, 90.0, Bauteilrand.Aussenluft);
            Assert.Equal(soll.Kennwerte.R1_KW, k.Value.R1_KW, 12);
            Assert.Equal(soll.Kennwerte.C1_Jk, k.Value.C1_Jk, 6);
            Assert.Equal(jeM2.Kennwerte.C1korr_Jk, k.Value.C1korrJeM2_JM2K, 6);
            Assert.Equal(0.015 * 1200 * 1000 + 0.2 * 2400 * 1000 + 0.12 * 30 * 1400, k.Value.Kapazitaet_JM2K, 6);
            Assert.Equal(u.U_WM2K, k.Value.USchichten_WM2K, 12);

            // Eine Schicht ohne λ: kein Wert, ein benannter Grund.
            a.Schichten[1].Lambda = null;
            Assert.Null(GebaeudeAufbauHuelle.Kennwerte(a, 12.5, 90.0, Bauteilrand.Aussenluft, out grund));
            Assert.NotEqual("", grund);
            Assert.Null(GebaeudeAufbauHuelle.Kennwerte(null, 1, 90, Bauteilrand.Aussenluft, out grund));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BTSB_OHNE_RECHNUNG, grund);
        }

        [Fact]
        public void Die_Stufenregel_der_Oberflaeche_ist_die_des_Kerns()
        {
            var ersatz = new BauteilaufbauModel { ID = 6, Typaufbau = "AW_MASSIV_AUSSENGEDAEMMT" };
            var aufbauten = new Dictionary<int, BauteilaufbauModel> { [5] = Massivwand(), [6] = ersatz };
            var faelle = new[]
            {
                new BauteilModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, ID_Aufbau = 5, U_Wert = 0.3, Herkunft = "IFC" },
                new BauteilModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, ID_Aufbau = 6, U_Wert = 0.3, Herkunft = "IFC" },
                new BauteilModel { Bauteilart = DbWerte.BAUTEILART_DACH, ID_Aufbau = 6, U_Wert = 0.2, Herkunft = DbWerte.HERKUNFT_VORGABE },
                new BauteilModel { Bauteilart = DbWerte.BAUTEILART_DACH, U_Wert = 0.2, Herkunft = "GBXML" },
                new BauteilModel { Bauteilart = DbWerte.BAUTEILART_BODENPLATTE },
                new BauteilModel { Bauteilart = DbWerte.BAUTEILART_FENSTER, U_Wert = 1.1, Herkunft = "IFC" },
            };
            foreach (BauteilModel b in faelle)
            {
                var d = new BauteilDaten { Bauteilart = b.Bauteilart, IdAufbau = b.ID_Aufbau, UWert = b.U_Wert, Herkunft = b.Herkunft };
                bool istErsatz = b.ID_Aufbau == 6;
                Assert.Equal(GebaeudeAufbauHuelle.Stufe(Bauteilzuordnung.Stufe(b, aufbauten)), GebaeudeAnsichtAufbaustufen.Stufe(d, istErsatz));
            }
            Assert.Equal(new[] { Aufbaustufe.A, Aufbaustufe.B, Aufbaustufe.C, Aufbaustufe.B, Aufbaustufe.C, Aufbaustufe.Transparent },
                         faelle.Select(b => GebaeudeAufbauHuelle.Stufe(Bauteilzuordnung.Stufe(b, aufbauten))));
        }

        [Fact]
        public void Gespeicherte_Bauteile_geben_Stufen_Summen_und_Steckbriefe_ohne_Datenbank()
        {
            string lang = "2O2Fr$t4X7Zf8NOew3FLOH" + new string('x', 70);
            var ansicht = new GebaeudeAnsichtDaten
            {
                Koerperraeume = new[]
                {
                    new GebaeudeAnsichtKoerperraum("r-1", 2.75, Array.Empty<IReadOnlyList<string>>(), null, null)
                    {
                        Dreiecksbauteile = new[] { lang, null, "dach-1" },
                    },
                },
            };
            var bauteile = new List<BauteilModel>
            {
                new BauteilModel { ID = 41, Bezeichner = "AW Süd", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 12.5, Neigung = 90,
                                   Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT, ID_Aufbau = 5, U_Wert = 0.28, Herkunft = "IFC" },
                new BauteilModel { ID = 42, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 80, Neigung = 0,
                                   Randbedingung = DbWerte.RANDBEDINGUNG_AUSSENLUFT, U_Wert = 0.2, Herkunft = DbWerte.HERKUNFT_VORGABE,
                                   Quellkennung = "dach-1" },
            };
            var kurz = new Dictionary<int, string> { [41] = Quellkennung.Kuerzen(lang) };
            var gelesen = new List<int>();
            GebaeudeAnsichtDaten d = GebaeudeNeulesenHuelle.AufbauAusBauteilen(ansicht, bauteile, kurz,
                id => { gelesen.Add(id); return id == 5 ? Massivwand() : null; },
                id => "Stoff " + id);

            Assert.Equal(Aufbaustufe.A, d.Bauteilstufen[lang]);
            Assert.Equal(Aufbaustufe.C, d.Bauteilstufen["dach-1"]);
            Assert.True(d.AufbauWaehlbar == false);   // ohne Klassifikation bleibt der Modus gesperrt
            Assert.Equal(new[] { 5 }, gelesen);

            BauteilsteckbriefDaten aw = d.Steckbriefe[lang];
            Assert.Equal(41, aw.IdBauteil);
            Assert.Equal(SteckbriefHerkunft.Datei, aw.UHerkunftSchluessel);
            Assert.Equal(SteckbriefHerkunft.Katalog, aw.AufbauHerkunftSchluessel);
            Assert.Equal(new[] { "Stoff 11", "Stoff 12", "Stoff 13" }, aw.Schichten.Select(s => s.Name));
            Assert.NotEqual("", aw.R1);
            Assert.NotEqual("", aw.C1);
            Assert.EndsWith("kJ/(m²K)", aw.Kapazitaet);
            BauteilsteckbriefDaten dach = d.Steckbriefe["dach-1"];
            Assert.Equal(SteckbriefHerkunft.Vorgabe, dach.UHerkunftSchluessel);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BTSB_OHNE_RECHNUNG, dach.Rechengrund);

            GebaeudeAnsichtAufbausumme a = d.Aufbausummen.Single(s => s.Stufe == Aufbaustufe.A);
            Assert.Equal((1, 12.5, 0, 0.0), (a.ZahlAussen, a.FlaecheAussenM2, a.ZahlInnen, a.FlaecheInnenM2));
            Assert.Equal(1, d.Aufbausummen.Single(s => s.Stufe == Aufbaustufe.C).ZahlAussen);
        }

        [Fact]
        public void Die_Herkunft_Projektdatei_ist_ein_Wert()
        {
            Assert.Equal(SteckbriefHerkunft.Projektdatei, GebaeudeAufbauHuelle.Herkunftsschluessel("PROJEKTDATEI"));
            Assert.Equal(SteckbriefHerkunft.Datei, GebaeudeAufbauHuelle.Herkunftsschluessel("IFC"));
            Assert.Equal(SteckbriefHerkunft.Vorgabe, GebaeudeAufbauHuelle.Herkunftsschluessel(DbWerte.HERKUNFT_VORGABE));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BTSB_HK_PROJEKTDATEI, GebaeudeAufbauHuelle.Herkunftstext(SteckbriefHerkunft.Projektdatei));
        }

        [Fact]
        public void Der_Importvorschlag_gibt_Stufen_Steckbriefe_und_die_Liste_je_Aufbau()
        {
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlagAbgleichTests.Mit("ifc4_haus_materialnamen.ifc");
            Assert.NotEmpty(v.Zeilen);
            GebaeudeAnsichtDaten d = GebaeudeAufbauHuelle.MitAufbau(new GebaeudeAnsichtDaten(), v);

            var mitKennung = v.Zeilen.Where(z => !string.IsNullOrEmpty(z.Kennung)).ToList();
            Assert.NotEmpty(mitKennung);
            foreach (GebaeudeBauteilzeile z in mitKennung)
                Assert.True(d.Bauteilstufen.ContainsKey(z.Kennung));
            Assert.Equal(v.Zeilen.Count, d.Aufbausummen.Sum(s => s.ZahlAussen + s.ZahlInnen));

            // Eine Zeile mit echtem Aufbau: Schichten mit Namen der Datei, R₁/C₁ aus der Reduktion.
            GebaeudeBauteilzeile echt = mitKennung.FirstOrDefault(z => z.Stufe == Bauteilzuordnungsstufe.A);
            if (echt != null)
            {
                BauteilsteckbriefDaten sb = d.Steckbriefe[echt.Kennung];
                Assert.Equal(Aufbaustufe.A, sb.Stufe);
                Assert.NotEmpty(sb.Schichten);
                Assert.Contains(sb.Schichten, s => s.Name.Length > 0);
                Assert.True(sb.R1.Length > 0 || sb.Rechengrund.Length > 0);
            }

            IReadOnlyList<EPOS.UI.Dialoge.Import.GebaeudeAufbaulistenzeileDaten> liste = GebaeudeAufbauHuelle.Aufbauliste(v);
            int opak = v.Zeilen.Count(z => z.Stufe != Bauteilzuordnungsstufe.Transparent);
            Assert.Equal(opak, liste.Sum(z => z.Bauteile));
            Assert.Equal(liste.Count, liste.Select(z => z.Schluessel).Distinct().Count());
            Assert.True(liste.Count <= opak);
            // Die unvollständigen Stufen stehen vorn.
            int ersteA = liste.ToList().FindIndex(z => z.Stufe == Aufbaustufe.A);
            if (ersteA > 0) Assert.All(liste.Take(ersteA), z => Assert.NotEqual(Aufbaustufe.A, z.Stufe));
        }
    
        [Fact]
        public void Die_Typwahl_je_Ersatzaufbau_ueberschreibt_die_Vorgabe()
        {
            GebaeudeBauteilvorschlag v = ZuordnungsstufeTests.SyntheseMitVierWaenden();
            GebaeudeBauteilzeile c = BauteilvorschlagProbe.Zeile(v, "w-c");
            Assert.Equal(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, c.Typaufbau);
            GebaeudeAufbauzeile ersatz = v.Aufbauten.Single(a => a.Aufbau.ID == c.Bauteil.ID_Aufbau);
            Assert.NotNull(ersatz.Ersatzschluessel);

            var zeile = GebaeudeAufbauHuelle.Aufbauliste(v).Single(z => z.Typschluessel == ersatz.Ersatzschluessel);
            Assert.Equal(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, zeile.Typcode);
            Assert.Contains(zeile.Typen, t => t.Schluessel == TypaufbauSaattabelle.AW_HOLZLEICHTBAU);
            Assert.All(zeile.Typen, t => Assert.StartsWith("AW_", t.Schluessel));

            var wahl = new Dictionary<string, string> { [ersatz.Ersatzschluessel] = TypaufbauSaattabelle.AW_HOLZLEICHTBAU };
            GebaeudeBauteilvorschlag w = ZuordnungsstufeTests.SyntheseMitVierWaenden(wahl);
            GebaeudeBauteilzeile c2 = BauteilvorschlagProbe.Zeile(w, "w-c");
            Assert.Equal(TypaufbauSaattabelle.AW_HOLZLEICHTBAU, c2.Typaufbau);
            Assert.Equal(Bauteilzuordnungsstufe.C, c2.Stufe);
            // Ein unbekannter Code fällt auf die Vorgabe zurück.
            var falsch = new Dictionary<string, string> { [ersatz.Ersatzschluessel] = "GIBT_ES_NICHT" };
            Assert.Equal(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT,
                         BauteilvorschlagProbe.Zeile(ZuordnungsstufeTests.SyntheseMitVierWaenden(falsch), "w-c").Typaufbau);
        }
}
}
