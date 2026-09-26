using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using WindowsFormsApplication1;
using Xunit;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Blockregeln des Vorlagenprüfers</b> (Konzept Berichtsvorlagen 4.2, 4.3, 4.7, 4.8, 6.4, 6.6;
    /// Etappe BV-E4): offene, überzählige und verschränkte Blöcke, Ebenen, Orte, Kontexte der Werte je
    /// Stand und je Gebäude und der Schalter, <c>|block n</c>, Steuerelemente und der Paarvergleich.
    /// </summary>
    public class VorlagenprueferBloeckeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Pruefbefund Schnell(byte[] vorlage, Pruefkontext kontext = null)
            => Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, kontext ?? new Pruefkontext());

        private static List<string> Kennungen(Pruefbefund b) => b.Meldungen.Select(m => m.Kennung).ToList();

        // =====================================================================
        //  Paare
        // =====================================================================

        [Fact]
        public void Offene_ueberzaehlige_und_verschraenkte_Bloecke()
        {
            Pruefbefund offen = Schnell(Probevorlagen.AusAbsaetzen("{{#je stand}}", "{{stand.anzeige}}"));
            Pruefmeldung o = Assert.Single(Probevorlagen.Mit(offen, "VF_PRUEF_BLOCK_OFFEN"));
            Assert.Equal("{{/je}} ergänzen.", o.WasTun);
            Assert.StartsWith("Absatz 1", o.Fundort, StringComparison.Ordinal);

            Pruefbefund zuviel = Schnell(Probevorlagen.AusAbsaetzen("{{#wenn hat.varianten}}", "x", "{{/wenn}}", "{{/wenn}}"));
            Assert.Single(Probevorlagen.Mit(zuviel, "VF_PRUEF_BLOCK_ENDE"));
            Assert.Empty(Probevorlagen.Mit(zuviel, "VF_PRUEF_BLOCK_OFFEN"));

            // {{/wenn}} schließt einen {{#je}}: das Ende findet keinen passenden Anfang, der Block bleibt offen.
            Pruefbefund verschraenkt = Schnell(Probevorlagen.AusAbsaetzen("{{#je stand}}", "x", "{{/wenn}}"));
            Assert.Single(Probevorlagen.Mit(verschraenkt, "VF_PRUEF_BLOCK_ENDE"));
            Assert.Single(Probevorlagen.Mit(verschraenkt, "VF_PRUEF_BLOCK_OFFEN"));
        }

        [Fact]
        public void Gueltige_Bloecke_sind_ohne_Befund()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Absatz("{{#je stand}}").Absatz("{{stand.anzeige}}")
                .Absatz("{{#wenn stand.ist_stamm}}").Absatz("Stamm").Absatz("{{/wenn}}")
                .Absatz("{{/je}}")
                .Absatz("{{#je stand}}").Absatz("{{#je gebaeude}}").Absatz("{{gebaeude.name}}").Absatz("{{/je}}").Absatz("{{/je}}")
                .Absatz("{{#wenn nicht hat.varianten}}").Absatz("Nur Stamm").Absatz("{{/wenn}}")
                .Absatz("{{#je variante|block 3}}")
                .Tabelle(new[] { new[] { "{{#je stand}}{{stand.anzeige}}", "x{{/je}}" } })
                .Absatz("{{/je}}"));
            Pruefbefund befund = Schnell(vorlage);
            Assert.True(befund.OhneBefund, Probevorlagen.Liste(befund));
        }

        [Fact]
        public void Dritte_Ebene_auch_ueber_Steuerelemente()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Element(Steuerelement("#je stand",
                    Probevorlagen.Absatz("{{#wenn hat.varianten}}"),
                    Probevorlagen.Absatz("{{#je gebaeude}}"), Probevorlagen.Absatz("x"), Probevorlagen.Absatz("{{/je}}"),
                    Probevorlagen.Absatz("{{/wenn}}"))));
            Pruefbefund befund = Schnell(vorlage);
            Pruefmeldung tief = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_BLOCK_TIEFE"));
            Assert.Equal("{{#je gebaeude}}", tief.Marke);
        }

        // =====================================================================
        //  Kontexte (Konzept 4.7)
        // =====================================================================

        [Fact]
        public void Werte_je_Stand_und_je_Gebaeude_nur_in_ihrem_Block()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen(
                "{{stand.anzeige}}",
                "{{#je stand}}", "{{gebaeude.name}}", "{{/je}}",
                "{{#je gebaeude}}", "{{gebaeude.name}} {{stand.anzeige}}", "{{/je}}",
                "{{#je variante|block 2}}", "{{stand.anzeige}}", "{{#je stand}}", "{{stand.anzeige}}", "{{/je}}", "{{/je}}"));
            List<Pruefmeldung> stand = Probevorlagen.Mit(befund, "VF_PRUEF_KONTEXT_STAND");
            Assert.Equal(3, stand.Count);   // außerhalb, im Gebäudeblock ohne Stand, direkt im Gruppenblock
            Assert.StartsWith("Absatz 1", stand[0].Fundort, StringComparison.Ordinal);
            Assert.StartsWith("Absatz 6", stand[1].Fundort, StringComparison.Ordinal);
            Assert.StartsWith("Absatz 9", stand[2].Fundort, StringComparison.Ordinal);
            Pruefmeldung gebaeude = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_KONTEXT_GEBAEUDE"));
            Assert.Equal("{{gebaeude.name}} ist ein Wert je Gebäude und steht außerhalb von {{#je gebaeude}}", gebaeude.Text);
            Assert.StartsWith("Absatz 3", gebaeude.Fundort, StringComparison.Ordinal);
        }

        [Fact]
        public void Musterzeile_und_Steuerelement_geben_den_Standkontext()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Tabelle(new[] { new[] { "Name", "Wert" }, new[] { "{{#je stand}}{{stand.anzeige}}", "{{stand.anzeige}}{{/je}}" } })
                .Tabelle(new[] { new[] { "{{stand.anzeige}}" } })
                .Element(Steuerelement("#je variante", Probevorlagen.Absatz("{{stand.anzeige}}"))));
            Pruefbefund befund = Schnell(vorlage);
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_KONTEXT_STAND"));
            Assert.StartsWith("Tabelle 2", m.Fundort, StringComparison.Ordinal);
            Assert.Empty(Probevorlagen.Mit(befund, "VF_PRUEF_BLOCK_ALLEIN"));
        }

        [Fact]
        public void Schalter_nur_als_Bedingung_und_je_Stand_nur_im_Standblock()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen(
                "{{hat.varianten}}",
                "{{#wenn stand.ist_stamm}}", "x", "{{/wenn}}",
                "{{#je stand}}", "{{#wenn stand.ist_stamm}}", "y", "{{/wenn}}", "{{/je}}"));
            Pruefmeldung ort = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_ORT"));
            Assert.Equal("Den Schalter nur als Bedingung verwenden: {{#wenn hat.varianten}} … {{/wenn}}.", ort.WasTun);
            Pruefmeldung kontext = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_KONTEXT_STAND"));
            Assert.Equal("{{#wenn stand.ist_stamm}}", kontext.Marke);
            Assert.StartsWith("Absatz 2", kontext.Fundort, StringComparison.Ordinal);
            Assert.Equal(2, befund.Fehleranzahl);
        }

        // =====================================================================
        //  Orte, Angaben, Steuerelemente
        // =====================================================================

        [Fact]
        public void Blockmarke_in_Kopfzeile_und_im_Satz_einer_Zelle()
        {
            byte[] kopf = Probevorlagen.Baue(b => b.Absatz("{{bericht.titel}}")
                .Kopfzeile(Probevorlagen.Absatz("{{#je stand}}"), Probevorlagen.Absatz("{{/je}}")));
            Pruefbefund k = Schnell(kopf);
            Assert.Equal(2, Probevorlagen.Mit(k, "VF_PRUEF_ORT").Count);

            // Zwei Zellen, Block in der ersten — nicht erste bis letzte Zelle, also nur als eigene Absätze.
            byte[] zelle = Probevorlagen.Baue(b => b.Tabelle(new[] { new[] { "{{#wenn hat.varianten}}x{{/wenn}}", "y" } }));
            Assert.Single(Probevorlagen.Mit(Schnell(zelle), "VF_PRUEF_BLOCK_ALLEIN"));
        }

        [Fact]
        public void Block_n_nur_an_je_variante_um_Absaetze()
        {
            Pruefbefund stand = Schnell(Probevorlagen.AusAbsaetzen("{{#je stand|block 3}}", "{{/je}}"));
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(stand, "VF_PRUEF_ANGABE_UNPASSEND"));
            Assert.Equal("{{#je stand|block 3}}", m.Marke);

            byte[] zeile = Probevorlagen.Baue(b => b.Tabelle(new[] { new[] { "{{#je variante|block 3}}x{{/je}}" } }));
            Assert.Single(Probevorlagen.Mit(Schnell(zeile), "VF_PRUEF_ANGABE_UNPASSEND"));

            Assert.Empty(Probevorlagen.Mit(Schnell(Probevorlagen.AusAbsaetzen("{{#je variante|block 3}}", "{{/je}}")),
                                           "VF_PRUEF_ANGABE_UNPASSEND"));
        }

        [Fact]
        public void Steuerelement_mit_Ende_oder_im_Satz_wird_nicht_ausgewertet()
        {
            byte[] ende = Probevorlagen.Baue(b => b.Element(Steuerelement("/je", Probevorlagen.Absatz("x"))));
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(Schnell(ende), "VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT"));
            Assert.Equal("Blockmarke {{/je}} als Inhaltssteuerelement wird nicht ausgewertet", m.Text);

            var satz = new W.Paragraph(new W.SdtRun(new W.SdtProperties(new W.Tag { Val = "#je stand" }),
                                                    new W.SdtContentRun(new W.Run(new W.Text("x")))));
            Assert.Single(Probevorlagen.Mit(Schnell(Probevorlagen.Baue(b => b.Element(satz))), "VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT"));

            byte[] gut = Probevorlagen.Baue(b => b.Element(Steuerelement("#je stand", Probevorlagen.Absatz("{{stand.anzeige}}"))));
            Assert.True(Schnell(gut).OhneBefund, Probevorlagen.Liste(Schnell(gut)));
        }

        // =====================================================================
        //  Paarvergleich (Konzept 4.7)
        // =====================================================================

        [Fact]
        public void Paarvergleich_in_Sicht_1_nur_mit_genau_einer_Variante()
        {
            var katalog = new Vorlagenkatalogsicht(Vorlagenfeldkatalog.Alle.Concat(new[]
            {
                new Vorlagenfeld("stand.b.anzeige", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stand, w => null),
            }), Vorlagenfeldkatalog.KATALOGFASSUNG);
            byte[] vorlage = Probevorlagen.AusAbsaetzen("{{stand.b.anzeige}}", "{{stand.b.anzeige}}");

            Pruefbefund sicht1 = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell,
                new Pruefkontext { AnzahlVarianten = 3, Sicht = 1 }, katalog, null);
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(sicht1, "VF_PRUEF_PAARSICHT"));
            Assert.Equal("Vorlage nutzt den Paarvergleich, gewählt ist Sicht 1", m.Text);
            Assert.Empty(Probevorlagen.Mit(sicht1, "VF_PRUEF_KONTEXT_STAND"));

            foreach (Pruefkontext k in new[]
                     {
                         new Pruefkontext { AnzahlVarianten = 3, Sicht = 2 },
                         new Pruefkontext { AnzahlVarianten = 1, Sicht = 1 },
                     })
                Assert.Empty(Probevorlagen.Mit(Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, k, katalog, null), "VF_PRUEF_PAARSICHT"));
        }

        /// <summary>Werte der Wirtschaftlichkeit ohne Warnliste: „Gültigkeitshinweise fehlen“ (Konzept 4.11), auch im Block.</summary>
        [Fact]
        public void Gueltigkeitshinweise_fehlen_auch_im_Block()
        {
            var katalog = new Vorlagenkatalogsicht(Vorlagenfeldkatalog.Alle.Concat(new[]
            {
                new Vorlagenfeld("stand.kennzahl.eff.jaz", Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stand, w => null),
                new Vorlagenfeld("stand.wirtschaft.warnungen", Vorlagenfeldart.Liste, Vorlagenfeldkontext.Stand, w => null),
            }), Vorlagenfeldkatalog.KATALOGFASSUNG);
            Pruefbefund ohne = Vorlagenpruefer.Pruefe(Probevorlagen.AusAbsaetzen("{{#je stand}}", "{{stand.kennzahl.eff.jaz}}", "{{/je}}"),
                                                     Pruefstufe.Schnell, new Pruefkontext(), katalog, null);
            Assert.Single(Probevorlagen.Mit(ohne, "VF_PRUEF_GUELTIGKEIT"));
            Assert.Equal(0, ohne.Fehleranzahl);

            Pruefbefund mit = Vorlagenpruefer.Pruefe(
                Probevorlagen.AusAbsaetzen("{{#je stand}}", "{{stand.kennzahl.eff.jaz}}", "{{stand.wirtschaft.warnungen}}", "{{/je}}"),
                Pruefstufe.Schnell, new Pruefkontext(), katalog, null);
            Assert.Empty(Probevorlagen.Mit(mit, "VF_PRUEF_GUELTIGKEIT"));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Ein Block-Steuerelement mit dem Tag <paramref name="tag"/> um die Absätze.</summary>
        private static W.SdtBlock Steuerelement(string tag, params W.Paragraph[] absaetze)
        {
            return new W.SdtBlock(new W.SdtProperties(new W.Tag { Val = tag }), new W.SdtContentBlock(absaetze));
        }
    }
}
