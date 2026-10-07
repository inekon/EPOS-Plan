using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Befund in der Hülle</b> (Abstimmung G5, B1; G5-3): <see cref="GebaeudeAufbauHuelle.MitAufbau(GebaeudeAnsichtDaten, GebaeudeBauteilvorschlag)"/>
    /// trägt neben den Stufen den Befund je Bauteilkennung, die Legende „Befund“ und Befund samt Grundtext im Steckbrief — aus
    /// dem Importvorschlag der Probe <c>ifc4_g5_befund.ifc</c> und aus gespeicherten Bauteilen (ohne Körperbefund).
    /// </summary>
    public sealed class GebaeudeBefundHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("en-US");

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Vorschlag_Befund_je_Bauteil_Legende_und_Steckbrief()
        {
            GebaeudeImportAblauf a = BauteilbefundTests.Lesen("ifc4_g5_befund.ifc");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            GebaeudeAnsichtDaten d = GebaeudeAufbauHuelle.MitAufbau(new GebaeudeAnsichtDaten(), v);
            Assert.True(d.HatBefunde);
            Assert.Equal(d.Bauteilstufen.Keys.OrderBy(k => k, StringComparer.Ordinal), d.Bauteilbefunde.Keys.OrderBy(k => k, StringComparer.Ordinal));

            foreach ((string name, Bauteilbefundgrund grund) in BauteilbefundTests.ERWARTET)
            {
                string kennung = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Name == name).Kennung;
                GebaeudeBauteilzeile z = v.Zeilen.Single(x => x.Kennung == kennung);
                Bauteilbefundstufe erwartet = grund == Bauteilbefundgrund.Keiner ? Bauteilbefundstufe.Ohne : Bauteilbefundstufe.KoerperUnlesbar;
                Assert.Equal(erwartet, d.Bauteilbefunde[z.Kennung]);
                Assert.Equal(erwartet, d.BefundVon(z.Kennung));
                BauteilsteckbriefDaten s = d.Steckbriefe[z.Kennung];
                Assert.Equal(erwartet, s.Befund);
                Assert.Equal(Bauteilbefunde.Text(grund), s.Befundgrund);
            }

            // Legende: ohne 3 Zeilen / 180 m² (Westwand 20 m², Vorgabezeilen Dach und Boden je 80 m²), Körper unlesbar 5 / 92 m²,
            // ohne Eigenschaften und ohne Bauteil leer.
            Assert.Equal(GebaeudeAnsichtBefundstufen.ZAHL, d.Befundsummen.Count);
            Assert.Equal(new[] { (3, 180.0), (5, 92.0), (0, 0.0), (0, 0.0) },
                         d.Befundsummen.Select(x => (x.Zahl, Math.Round(x.FlaecheM2, 6))));
            Assert.Equal(Enum.GetValues<Bauteilbefundstufe>(), d.Befundsummen.Select(x => x.Stufe));
        }

        [Fact]
        public void Steckbrief_nennt_den_Grund_in_der_Sprache_der_Oberflaeche()
        {
            GebaeudeImportAblauf a = BauteilbefundTests.Lesen("ifc4_g5_befund.ifc");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            string nord = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Name == "Wand Nord").Kennung;
            GebaeudeBauteilzeile z = v.Zeilen.Single(x => x.Kennung == nord);
            foreach ((string kultur, string text) in new[] { ("de-DE", "Körper mit offener Schale."), ("en-US", "Body with an open shell.") })
            {
                using (new Kulturvorrichtung(kultur))
                {
                    BauteilsteckbriefDaten s = GebaeudeAufbauHuelle.Steckbrief(z, null);
                    Assert.Equal((Bauteilbefundstufe.KoerperUnlesbar, text), (s.Befund, s.Befundgrund));
                }
            }
        }

        [Fact]
        public void Gespeicherte_Bauteile_rot_ohne_U_und_Aufbau_sonst_grau()
        {
            var bauteile = new List<(string Kennung, BauteilModel Bauteil)>
            {
                ("k-ohne-u", new BauteilModel { ID = 1, Bauteilart = DbWerte.BAUTEILART_TUER, Bezeichner = "Tür", Flaeche = 2.0, Randbedingung = "AUSSEN" }),
                ("k-mit-u", new BauteilModel { ID = 2, Bezeichner = "Wand", Flaeche = 20.0, U_Wert = 0.3, Randbedingung = "AUSSEN" }),
                ("k-aufbau", new BauteilModel { ID = 3, Bezeichner = "Decke", Flaeche = 30.0, ID_Aufbau = 9 }),
            };
            GebaeudeAnsichtDaten d = GebaeudeAufbauHuelle.MitAufbau(new GebaeudeAnsichtDaten(), bauteile,
                                                                     new Dictionary<int, BauteilaufbauModel>(), new Dictionary<int, string>());
            Assert.Equal(Bauteilbefundstufe.OhneEigenschaften, d.Bauteilbefunde["k-ohne-u"]);
            Assert.Equal(Bauteilbefundstufe.Ohne, d.Bauteilbefunde["k-mit-u"]);
            Assert.Equal(Bauteilbefundstufe.Ohne, d.Bauteilbefunde["k-aufbau"]);
            Assert.Equal(Bauteilbefundstufe.OhneEigenschaften, d.Steckbriefe["k-ohne-u"].Befund);
            Assert.Equal(Bauteilbefunde.Text(Bauteilbefundgrund.OhneUWert), d.Steckbriefe["k-ohne-u"].Befundgrund);
            Assert.Equal("", d.Steckbriefe["k-mit-u"].Befundgrund);
            Assert.Equal(new[] { (2, 50.0), (0, 0.0), (1, 2.0), (0, 0.0) }, d.Befundsummen.Select(x => (x.Zahl, x.FlaecheM2)));
        }

        // ------------------------------------------------------------------
        //  G5-3b: Herkunft der Fläche und Filter „Nur Bauteile mit Befund“
        // ------------------------------------------------------------------

        [Fact]
        public void Flaechenherkunft_als_kurzer_Text_leer_bei_Handeingabe()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                Assert.Equal(new[] { "Mengensatz", "Raumgrenze", "Körper", "schematisch", "", "" },
                             new[] { FlaechenherkunftWerte.MENGENSATZ, FlaechenherkunftWerte.RAUMGRENZE, FlaechenherkunftWerte.KOERPER,
                                     FlaechenherkunftWerte.SCHEMATISCH, null, "" }.Select(GebaeudeAufbauHuelle.FlaechenherkunftText));
            }
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("Solid", GebaeudeAufbauHuelle.FlaechenherkunftText(FlaechenherkunftWerte.KOERPER));
        }

        [Fact]
        public void Steckbrief_Bauteilliste_und_Flaechenliste_tragen_Herkunft_und_Befund()
        {
            GebaeudeImportAblauf a = BauteilbefundTests.Lesen("ifc4_g5_befund.ifc");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            GebaeudeAnsichtDaten d = GebaeudeAufbauHuelle.MitAufbau(new GebaeudeAnsichtDaten(), v);
            Assert.Contains(v.Zeilen, z => !string.IsNullOrEmpty(z.Bauteil.Flaechenherkunft));

            var liste = GebaeudeImportHuelle.BauteileDaten(v).Liste;
            IReadOnlyList<EPOS.UI.Dialoge.Import.GebaeudeFlaechenzeileDaten> flaechen = GebaeudeImportZonen.Flaechen(new GebaeudeZonierung(), v);
            Assert.Equal(v.Zeilen.Count, flaechen.Count);
            for (int i = 0; i < v.Zeilen.Count; i++)
            {
                GebaeudeBauteilzeile z = v.Zeilen[i];
                string text = GebaeudeAufbauHuelle.FlaechenherkunftText(z.Bauteil.Flaechenherkunft);
                Assert.Equal(text, liste[i].Text(GebaeudeImportZonen.SP_FLAECHENHERKUNFT) is var t && t == ParameterVerwendung.LEER ? "" : t);
                Assert.Equal(text, flaechen[i].Zeile.Text(GebaeudeImportZonen.SP_FLAECHENHERKUNFT) is var f && f == ParameterVerwendung.LEER ? "" : f);
                Assert.Equal(z.Befund != Bauteilbefund.Ohne, flaechen[i].MitBauteilbefund);
                if (z.Kennung != null && d.Steckbriefe.TryGetValue(z.Kennung, out BauteilsteckbriefDaten s))
                    Assert.Equal((z.Bauteil.Flaechenherkunft ?? "", text), (s.FlaechenherkunftSchluessel, s.Flaechenherkunft));
            }
            // Die Probe trägt fünf Bauteile mit unlesbarem Körper — der Filter findet sie.
            Assert.Equal(5, flaechen.Count(f => f.MitBauteilbefund));
            Assert.Contains(GebaeudeImportZonen.Flaechenprofil().Spalten, sp => sp.Schluessel == GebaeudeImportZonen.SP_FLAECHENHERKUNFT);
            Assert.Contains(GebaeudeImportHuelle.Bauteilprofil().Spalten, sp => sp.Schluessel == GebaeudeImportZonen.SP_FLAECHENHERKUNFT);
        }
    }
}
