using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using WindowsFormsApplication1;
using Xunit;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Vorlagenprüfer</b> (Konzept Berichtsvorlagen 6.8, Etappe BV-E1, Teil A3) — je Regel ein
    /// Fall; die Vorlagen entstehen im Test über das SDK (<see cref="Probevorlagen"/>), dazu die
    /// Standard- und die Beispielvorlage aus dem Repository.
    ///
    /// <para>Regeln, deren Arten Katalog v1 noch nicht führt (Tabelle, Bild, Schalter, Werte je
    /// Variante oder Gebäude, Wirtschaftlichkeit), prüft der Test gegen eine eigene
    /// <see cref="Vorlagenkatalogsicht"/> — die Regel steht im Prüfer, nicht im Katalog.</para>
    /// </summary>
    public class VorlagenprueferTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly Pruefkontext Deutsch = new Pruefkontext();

        private static Pruefbefund Schnell(byte[] vorlage) => Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, Deutsch);

        private static Pruefbefund Voll(byte[] vorlage) => Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, Deutsch);

        /// <summary>Ein Katalog v1 um Einträge erweitert, die erst spätere Etappen führen.</summary>
        private static Vorlagenkatalogsicht Erweitert(int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG, params Vorlagenfeld[] zusatz)
        {
            return new Vorlagenkatalogsicht(Vorlagenfeldkatalog.Alle.Concat(zusatz), fassung);
        }

        private static Pruefbefund MitKatalog(byte[] vorlage, Vorlagenkatalogsicht katalog, Pruefstufe stufe = Pruefstufe.Schnell)
            => Vorlagenpruefer.Pruefe(vorlage, stufe, Deutsch, katalog, null);

        private static Vorlagenfeld Feld(string schluessel, Vorlagenfeldart art, Vorlagenfeldkontext kontext = Vorlagenfeldkontext.Gruppe)
            => new Vorlagenfeld(schluessel, art, kontext, w => null);

        // =====================================================================
        //  Die Vorlagen des Repositoriums
        // =====================================================================

        /// <summary>
        /// Die Standardvorlage im vollen Aufbau (BV-E2, Anhang B.3; Werkzeug <c>beispiel --standard</c>) ist in
        /// beiden Stufen ohne Befund: keine Kommentare, Katalogfassung 2 in <c>custom.xml</c>, das Logo der
        /// Kopfzeile als Bildplatzhalter. Sie führt jedes Kapitel einzeln, das Deckblatt
        /// trägt sie selbst aus Platzhaltern (sein Häkchen gehört nicht zu ihren Bausteinen), die
        /// Wirtschaftlichkeit darin; die Stellen der Kapitel sind ihre Kapitelköpfe.
        /// </summary>
        /// <summary>Die Katalogfassung in <c>custom.xml</c> der ausgelieferten Standardvorlage.</summary>
        private const int FASSUNG_DER_STANDARDVORLAGE = 4;

        [Fact]
        public void Standardvorlage_aus_dem_Repository_ohne_Fehler_mit_allen_Kapiteln_ausser_dem_Deckblatt()
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (pfad == null) return;
            byte[] vorlage = File.ReadAllBytes(pfad);

            foreach (Pruefstufe stufe in new[] { Pruefstufe.Schnell, Pruefstufe.Voll })
            {
                Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, stufe, Deutsch);
                Assert.True(befund.OhneBefund, stufe + ":\n" + Probevorlagen.Liste(befund));
                Assert.Equal(0, befund.Kommentare);
                Assert.Contains(Vorlagenfeldkatalog.LOGO, befund.Schluessel);
                Assert.True(befund.IstLesbar);
                Assert.Empty(befund.UnbekannteSchluessel);
                Assert.DoesNotContain("bericht.inhalt", befund.Schluessel);
                Assert.All(Berichtskapitel.Alle.Where(k => k.Name != Berichtskapitel.DECKBLATT),
                           k => Assert.Contains(k.Schluessel, befund.Schluessel));
                Assert.True(befund.HatKapitel);
                Assert.True(befund.HatWirtschaftlichkeit);
                Assert.Equal(BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel)
                                                 .Where(b => b != BerichtsKonfiguration.B_DECKBLATT), befund.Bausteine);
                Assert.True(befund.DeckblattAusPlatzhaltern);
                Assert.Equal("Deckblatt", befund.Kapitelstellen[BerichtsKonfiguration.B_DECKBLATT]);
                Assert.Equal("Inhalt", befund.Kapitelstellen[BerichtsKonfiguration.B_INHALT]);
                Assert.Equal("Berechnungsergebnisse je Variante", befund.Kapitelstellen[BerichtsKonfiguration.B_ERGEBNISSE]);
                Assert.Equal("Wirtschaftlichkeit", befund.Kapitelstellen[BerichtsKonfiguration.B_WIRTSCHAFT]);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_AE_TITEL, befund.Kapitelstellen[Berichtskapitel.ANHANG_E]);
                // Die Standardvorlage trägt die Fassung, mit der sie zuletzt gebaut wurde — mit BV-E5 die laufende
                // Fassung 4 (Anwenderentscheid BV-E4-4); Inhalt und Aussehen blieben gleich.
                Assert.Equal(FASSUNG_DER_STANDARDVORLAGE, befund.Katalogfassung);
                Assert.True(befund.Katalogfassung <= Vorlagenfeldkatalog.KATALOGFASSUNG);
                Assert.Null(befund.Sprache);
                Assert.Equal(Vorlagenpruefer.Pruefsumme(vorlage), befund.Pruefsumme);
            }
        }

        /// <summary>
        /// Die Beispielvorlage (voller Aufbau) kennt Katalog v2 ganz: kein Fehler, keine Warnung, jedes
        /// Kapitel bekannt; ihre Kommentare stehen als Hinweis. Die Platzhalter in den Kommentaren prüft niemand.
        /// </summary>
        [Fact]
        public void Beispielvorlage_kennt_ihre_Kapitel_und_meldet_nur_die_Kommentare()
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.BEISPIEL);
            if (pfad == null) return;
            Pruefbefund befund = Voll(File.ReadAllBytes(pfad));

            Assert.Equal(0, befund.Fehleranzahl);
            Assert.Equal(0, befund.Warnungen);
            Assert.Empty(befund.UnbekannteSchluessel);
            Assert.Equal(befund.Kommentare > 0 ? 1 : 0, Probevorlagen.Mit(befund, "VF_PRUEF_KOMMENTARE").Count);
            Assert.True(befund.HatKapitel);
            Assert.Equal(7, befund.Bausteine.Count);
            Assert.True(befund.AnzahlPlatzhalter >= 24, befund.AnzahlPlatzhalter.ToString());
        }

        // =====================================================================
        //  Schlüssel, Marken, Klammern
        // =====================================================================

        [Fact]
        public void Zerlegte_Runs_werden_als_ein_Platzhalter_erkannt()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b.Element(Probevorlagen.Absatz("Kunde: {{proj", "ekt.kun", "de}}, Stand heute.")));
            Pruefbefund befund = Schnell(vorlage);
            Assert.True(befund.OhneBefund, Probevorlagen.Liste(befund));
            Assert.Equal(1, befund.AnzahlPlatzhalter);
            Assert.Equal(new[] { "projekt.kunde" }, befund.Schluessel);
        }

        [Fact]
        public void Unbekannter_Schluessel_bekommt_einen_Vorschlag_ueber_Abstand_oder_Verlaengerung()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("{{projekt.kunnde}}", "{{projekt.kundename|leer statt strich}}", "{{voellig.anders}}"));
            List<Pruefmeldung> unbekannt = Probevorlagen.Mit(befund, "VF_PRUEF_UNBEKANNT");
            Assert.Equal(3, unbekannt.Count);
            Assert.Equal("{{projekt.kunde}}", unbekannt[0].Vorschlag);
            Assert.Equal("Unbekannter Platzhalter {{projekt.kunnde}}", unbekannt[0].Text);
            Assert.Equal("Vorschlag {{projekt.kunde}} übernehmen.", unbekannt[0].WasTun);
            Assert.Equal("{{projekt.kunde|leer statt strich}}", unbekannt[1].Vorschlag);
            Assert.Null(unbekannt[2].Vorschlag);
            Assert.Equal(Befundstufe.Fehler, unbekannt[2].Stufe);
            Assert.Equal(new[] { "projekt.kundename", "projekt.kunnde", "voellig.anders" }, befund.UnbekannteSchluessel);
        }

        [Fact]
        public void Alias_ist_bekannt_und_keine_Meldung()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("Fassung {{bericht.programmversion}}"));
            Assert.True(befund.OhneBefund, Probevorlagen.Liste(befund));
            Assert.Equal(new[] { "bericht.programmversion" }, befund.Schluessel);
        }

        [Fact]
        public void Unbekannte_Marke_ist_Fehler_offene_Klammer_Warnung()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("leer {{}} hier", "{{#gruppe x}}", "Kunde {{projekt.kunde} ohne Ende"));
            Assert.Equal(2, Probevorlagen.Mit(befund, "VF_PRUEF_MARKE_UNBEKANNT").Count);
            Pruefmeldung offen = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_KLAMMER_OFFEN"));
            Assert.Equal("Platzhalter nicht erkannt: „{{projekt.kunde} ohne Ende“ – {{ ohne passendes }} im selben Absatz", offen.Text);
            Assert.Equal("Den Platzhalter in einem Zug neu tippen, ohne Tabulator oder Umbruch.", offen.WasTun);
            Assert.Equal(Befundstufe.Warnung, offen.Stufe);
        }

        /// <summary>
        /// Die Erkennung folgt der Engine (Nachtrag A2): Zerlegte Runs verbinden sich nur innerhalb eines
        /// Behälters (Absatz, Hyperlink, Inhaltssteuerelement im Satz, w:ins …) und nicht über Tabulator,
        /// Feldzeichen oder Bild hinweg. Was darüber reicht, bleibt im Bericht als Text stehen — der
        /// Prüfer warnt mit „Platzhalter nicht erkannt“ statt ihn still als gültig zu zählen.
        /// </summary>
        [Fact]
        public void Zerrissene_Platzhalter_werden_wie_in_der_Engine_nicht_erkannt()
        {
            W.Run Lauf(string t) => new W.Run(new W.Text(t) { Space = SpaceProcessingModeValues.Preserve });
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Element(new W.Paragraph(Lauf("{{projekt."), new W.Run(new W.TabChar()), Lauf("kunde}}")))
                .Element(new W.Paragraph(Lauf("{{projekt."), new W.Hyperlink(Lauf("kunde}}"))))
                .Element(new W.Paragraph(Lauf("{{projekt."),
                                         new W.Run(new W.FieldChar { FieldCharType = W.FieldCharValues.Begin }),
                                         new W.Run(new W.FieldCode(" PAGE ")),
                                         new W.Run(new W.FieldChar { FieldCharType = W.FieldCharValues.End }),
                                         Lauf("kunde}}")))
                .Roh("<w:r><w:t>{{projekt.</w:t></w:r>" + Probevorlagen.BildXml("Bild 1", "") + "<w:r><w:t>kunde}}</w:t></w:r>")
                .Element(new W.Paragraph(Lauf("{{projekt."), new W.InsertedRun(Lauf("kunde}}")) { Id = "1", Author = "Probe" }))
                .Element(new W.Paragraph(new W.Hyperlink(Lauf("{{proj"), Lauf("ekt.kunde}}"))))
                .Element(new W.Paragraph(new W.InsertedRun(Lauf("{{proj"), Lauf("ekt.kunde}}")) { Id = "2", Author = "Probe" })));

            Pruefbefund befund = Schnell(vorlage);
            List<Pruefmeldung> offen = Probevorlagen.Mit(befund, "VF_PRUEF_KLAMMER_OFFEN");
            Assert.Equal(5, offen.Count);
            Assert.All(offen, m => Assert.Equal(Befundstufe.Warnung, m.Stufe));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, offen.Select(m => int.Parse(m.Fundort.Split(' ')[1])));
            Assert.Equal(2, befund.AnzahlPlatzhalter);      // ganz im Hyperlink, ganz in w:ins
            Assert.Equal(0, befund.Fehleranzahl);

            using (WordprocessingDocument doc = WordprocessingDocument.Open(new MemoryStream(vorlage), false))
            {
                Vorlagendurchlauf lauf = Vorlagenteile.Durchlaufe(doc);
                Assert.Equal("{{projekt.\tkunde}}", lauf.Absaetze[0].Text);
                Assert.Equal("{{projekt.\nkunde}}", lauf.Absaetze[0].Erkennungstext);
                Assert.Equal("{{projekt.\nkunde}}\n", lauf.Absaetze[1].Erkennungstext);
                Assert.Equal("{{projekt.kunde}}", lauf.Absaetze[3].Text);   // das Bild trägt keinen Text
            }
        }

        [Fact]
        public void Abweichende_Schreibweise_gibt_den_Hinweis_mit_der_Normalform()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("{{ Projekt.Kunde }}", "{{projekt.geändert}}"));
            List<Pruefmeldung> hinweise = Probevorlagen.Mit(befund, "VF_PRUEF_NORMALFORM");
            Assert.Equal(2, hinweise.Count);
            Assert.All(hinweise, h => Assert.Equal(Befundstufe.Hinweis, h.Stufe));
            Assert.Equal("{{projekt.kunde}}", hinweise[0].Vorschlag);
            Assert.Equal("{{projekt.geaendert}}", hinweise[1].Vorschlag);
            Assert.Equal(0, befund.Fehleranzahl);
        }

        // =====================================================================
        //  Orte (Konzept 4.3)
        // =====================================================================

        [Fact]
        public void Liste_und_Tabelle_im_Satz_passen_nicht()
        {
            Pruefbefund liste = Schnell(Probevorlagen.AusAbsaetzen("Hinweise: {{bericht.warnungen}} und mehr"));
            Pruefmeldung ort = Assert.Single(Probevorlagen.Mit(liste, "VF_PRUEF_ORT"));
            Assert.Equal("{{bericht.warnungen}} passt nicht an diese Stelle: Art „Liste“ ist im Satz nicht möglich", ort.Text);
            Assert.Equal("Den Platzhalter allein in einen eigenen Absatz setzen.", ort.WasTun);
            Assert.Equal("Absatz 1 beginnt mit „Hinweise: {{bericht.warnungen}} und mehr“", ort.Fundort);

            Vorlagenkatalogsicht katalog = Erweitert(Vorlagenfeldkatalog.KATALOGFASSUNG, Feld("tabelle.vergleich", Vorlagenfeldart.Tabelle));
            Pruefbefund tabelle = MitKatalog(Probevorlagen.AusAbsaetzen("Siehe {{tabelle.vergleich}}", "{{tabelle.vergleich}}"), katalog);
            Assert.Single(Probevorlagen.Mit(tabelle, "VF_PRUEF_ORT"));
            Assert.Contains("Art „Tabelle“ ist im Satz", Probevorlagen.Mit(tabelle, "VF_PRUEF_ORT")[0].Text);
        }

        [Fact]
        public void Kapitel_in_der_Kopfzeile_und_in_einer_Zelle_passen_nicht()
        {
            byte[] kopf = Probevorlagen.Baue(b => b.Absatz("Text").Kopfzeile(Probevorlagen.Absatz("{{bericht.inhalt}}")));
            Pruefmeldung ort = Assert.Single(Probevorlagen.Mit(Schnell(kopf), "VF_PRUEF_ORT"));
            Assert.Equal("{{bericht.inhalt}} passt nicht an diese Stelle: Art „Kapitel“ ist in Kopf- und Fußzeilen nicht möglich", ort.Text);
            Assert.Equal("Kopfzeile 1, Absatz 1 beginnt mit „{{bericht.inhalt}}“", ort.Fundort);
            Assert.Equal("Den Platzhalter in den Haupttext verschieben.", ort.WasTun);

            byte[] zelle = Probevorlagen.Baue(b => b.Tabelle(new[] { new[] { "Kopf", "Wert" }, new[] { "{{bericht.inhalt}}", "x" } }));
            Pruefmeldung inZelle = Assert.Single(Probevorlagen.Mit(Schnell(zelle), "VF_PRUEF_ORT"));
            Assert.Contains("in Tabellenzellen", inZelle.Text);
            Assert.Equal("Tabelle 1, Zeile 2, Zelle 1 beginnt mit „{{bericht.inhalt}}“", inZelle.Fundort);
        }

        [Fact]
        public void Liste_in_einer_Fussnote_passt_nicht_die_Trennlinie_zaehlt_nicht()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b.Fussnoten("{{bericht.warnungen}}"));
            Pruefbefund befund = Schnell(vorlage);
            Pruefmeldung ort = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_ORT"));
            Assert.Contains("in Fuß- und Endnoten", ort.Text);
            Assert.StartsWith("Fußnote 1, Absatz 1", ort.Fundort);
            Assert.Empty(Probevorlagen.Mit(befund, "VF_PRUEF_UNBEKANNT"));   // {{projekt.trennlinie}} steht in der Trennlinie
        }

        [Fact]
        public void Bild_Tabelle_und_Schalter_nach_ihren_Ortsregeln()
        {
            Vorlagenkatalogsicht katalog = Erweitert(Vorlagenfeldkatalog.KATALOGFASSUNG,
                Feld("bild.vergleich.balken", Vorlagenfeldart.Bild),
                Feld("tabelle.vergleich", Vorlagenfeldart.Tabelle),
                Feld("hat.kaelte", Vorlagenfeldart.Schalter, Vorlagenfeldkontext.Bericht));

            // Bild als Text allein: kein Befund (BV-E5 — das Bild steht in Satzspiegelbreite); in der Fußnote:
            // Ortsfehler.
            Pruefbefund bild = MitKatalog(Probevorlagen.Baue(b => b.Absatz("{{bild.vergleich.balken}}").Fussnoten("{{bild.vergleich.balken}}")), katalog);
            Pruefmeldung note = Assert.Single(Probevorlagen.Mit(bild, "VF_PRUEF_ORT"));
            Assert.Contains("Art „Bild“ ist in Fuß- und Endnoten nicht möglich", note.Text);
            Assert.Empty(Probevorlagen.Mit(bild, "VF_PRUEF_SPAETER"));

            // Tabelle im Textfeld: Fehler.
            Pruefbefund textfeld = MitKatalog(Probevorlagen.Baue(b => b.Roh(Probevorlagen.TextfeldXml("{{tabelle.vergleich}}"))), katalog);
            Pruefmeldung tf = Assert.Single(Probevorlagen.Mit(textfeld, "VF_PRUEF_ORT"));
            Assert.Contains("in Textfeldern", tf.Text);

            // Schalter außerhalb einer Bedingung: Fehler mit dem Weg über {{#wenn …}}.
            Pruefbefund schalter = MitKatalog(Probevorlagen.AusAbsaetzen("{{hat.kaelte}}"), katalog);
            Pruefmeldung s = Assert.Single(Probevorlagen.Mit(schalter, "VF_PRUEF_ORT"));
            Assert.Contains("außerhalb einer Bedingung", s.Text);
            Assert.Equal("Den Schalter nur als Bedingung verwenden: {{#wenn hat.kaelte}} … {{/wenn}}.", s.WasTun);
        }

        [Fact]
        public void Nur_Excel_Platzhalter_passt_nicht_in_Word()
        {
            var blatt = new Vorlagenfeld("blatt.detail", Vorlagenfeldart.Blatt, Vorlagenfeldkontext.Bericht, w => null)
            { Ausgaben = Vorlagenausgabe.Excel };
            Pruefbefund befund = MitKatalog(Probevorlagen.AusAbsaetzen("{{blatt.detail}}"), Erweitert(Vorlagenfeldkatalog.KATALOGFASSUNG, blatt));
            Pruefmeldung ort = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_ORT"));
            Assert.Contains("in einer Word-Vorlage", ort.Text);
            Assert.Equal("Diesen Platzhalter nur in der Excel-Vorlage verwenden.", ort.WasTun);
        }

        // =====================================================================
        //  Kontexte (Konzept 4.7)
        // =====================================================================

        [Fact]
        public void Werte_je_Variante_und_je_Gebaeude_ausserhalb_ihres_Blocks_sind_Kontextfehler()
        {
            Vorlagenkatalogsicht katalog = Erweitert(Vorlagenfeldkatalog.KATALOGFASSUNG,
                Feld("stand.kennzahl.eff.jaz", Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Stand),
                Feld("gebaeude.name", Vorlagenfeldart.Text, Vorlagenfeldkontext.Gebaeude));
            Pruefbefund befund = MitKatalog(Probevorlagen.AusAbsaetzen("JAZ {{stand.kennzahl.eff.jaz}}", "{{gebaeude.name}}"), katalog);
            Pruefmeldung stand = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_KONTEXT_STAND"));
            Assert.Equal("{{stand.kennzahl.eff.jaz}} ist ein Wert je Variante und steht außerhalb von {{#je stand}}", stand.Text);
            Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_KONTEXT_GEBAEUDE"));
            Assert.Equal(2, befund.Fehleranzahl);

            // Katalog v3 (BV-E4) führt beide Schlüssel: Sie sind bekannt; außerhalb ihres Blocks meldet der
            // Prüfer den Kontext, nicht „unbekannt“.
            Pruefbefund v3 = Schnell(Probevorlagen.AusAbsaetzen("{{stand.kennzahl.eff.jaz}}", "{{gebaeude.flaeche}}"));
            Pruefmeldung bekannt = Assert.Single(Probevorlagen.Mit(v3, "VF_PRUEF_KONTEXT_STAND"));
            Assert.Equal("Den Platzhalter zwischen {{#je stand}} und {{/je}} setzen oder einen Wert des Stammprojekts verwenden (stamm.*, projekt.*).", bekannt.WasTun);
            Assert.Single(Probevorlagen.Mit(v3, "VF_PRUEF_KONTEXT_GEBAEUDE"));
            Assert.Empty(Probevorlagen.Mit(v3, "VF_PRUEF_UNBEKANNT"));
            Assert.Empty(v3.UnbekannteSchluessel);

            // Ohne Katalogeintrag (Sicht der Fassung 2) ist ein Schlüssel dieser Bereiche außerhalb seines
            // Blocks ebenfalls ein Kontextfehler — mit dem nahen Wert des Stammprojekts als Vorschlag.
            var v2Sicht = new Vorlagenkatalogsicht(Vorlagenfeldkatalog.Alle.Where(f => f.Seit <= 2), 2);
            Pruefbefund v2 = MitKatalog(Probevorlagen.AusAbsaetzen("{{stand.kennzahl.eff.jaz}}", "{{gebaeude.flaeche}}"), v2Sicht);
            Pruefmeldung jeVariante = Assert.Single(Probevorlagen.Mit(v2, "VF_PRUEF_KONTEXT_STAND"));
            Assert.Equal("{{stamm.kennzahl.eff.jaz}}", jeVariante.Vorschlag);
            Assert.Equal(bekannt.WasTun, jeVariante.WasTun);
            Assert.Single(Probevorlagen.Mit(v2, "VF_PRUEF_KONTEXT_GEBAEUDE"));
            Assert.Empty(Probevorlagen.Mit(v2, "VF_PRUEF_UNBEKANNT"));
            Assert.Equal(new[] { "gebaeude.flaeche", "stand.kennzahl.eff.jaz" }, v2.UnbekannteSchluessel);
        }

        // =====================================================================
        //  Formatangaben (Konzept 4.8)
        // =====================================================================

        [Fact]
        public void Unbekannte_Formatangabe_mit_Vorschlag_und_unpassende_Angabe()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("{{bericht.varianten.anzahl|stelen 2}}", "{{projekt.kunde|stellen 1}}", "{{projekt.kunde|quer}}"));
            List<Pruefmeldung> unbekannt = Probevorlagen.Mit(befund, "VF_PRUEF_ANGABE_UNBEKANNT");
            Assert.Equal(2, unbekannt.Count);
            Assert.Equal("stellen 2", unbekannt[0].Vorschlag);
            Assert.Equal("Unbekannte Formatangabe „|stelen 2“ in {{bericht.varianten.anzahl|stelen 2}}", unbekannt[0].Text);
            Assert.Null(unbekannt[1].Vorschlag);
            Assert.Equal("Erlaubt sind: leer statt strich.", unbekannt[1].WasTun);

            Pruefmeldung unpassend = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_ANGABE_UNPASSEND"));
            Assert.Equal("Die Formatangabe „|stellen 1“ gilt nicht für {{projekt.kunde|stellen 1}} (Art „Text“)", unpassend.Text);
            Assert.Equal("Die Angabe entfernen; für diese Art gelten: leer statt strich.", unpassend.WasTun);
        }

        // =====================================================================
        //  Blöcke (BV-E4: geprüft nach den Regeln der Engine; weitere Fälle in VorlagenprueferBloeckeTests)
        // =====================================================================

        [Fact]
        public void Offener_Block_ist_nicht_geschlossen()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("{{#je stand}}", "{{projekt.kunde}}"));
            Assert.Empty(Probevorlagen.Mit(befund, "VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT"));
            Pruefmeldung offen = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_BLOCK_OFFEN"));
            Assert.Equal("{{/je}} ergänzen.", offen.WasTun);

            Pruefbefund ende = Schnell(Probevorlagen.AusAbsaetzen("{{/wenn}}"));
            Assert.Single(Probevorlagen.Mit(ende, "VF_PRUEF_BLOCK_ENDE"));
        }

        [Fact]
        public void Block_Paare_Tiefe_Bereich_und_Stellung_im_Satz()
        {
            Pruefbefund tief = Schnell(Probevorlagen.AusAbsaetzen("{{#je stand}}", "{{#je gebaeude}}", "{{#je variante}}",
                                                                  "{{/je}}", "{{/je}}", "{{/je}}"));
            Assert.Single(Probevorlagen.Mit(tief, "VF_PRUEF_BLOCK_TIEFE"));
            Assert.Empty(Probevorlagen.Mit(tief, "VF_PRUEF_BLOCK_OFFEN"));
            Assert.Empty(Probevorlagen.Mit(tief, "VF_PRUEF_BLOCK_ENDE"));
            Assert.Empty(Probevorlagen.Mit(tief, "VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT"));

            Pruefbefund bereich = Schnell(Probevorlagen.AusAbsaetzen("{{#je kunde}}", "{{/je}}"));
            Pruefmeldung b = Assert.Single(Probevorlagen.Mit(bereich, "VF_PRUEF_BLOCK_BEREICH"));
            Assert.Equal("Erlaubt sind {{#je stand}}, {{#je variante}}, {{#je gebaeude}}.", b.WasTun);

            Pruefbefund satz = Schnell(Probevorlagen.AusAbsaetzen("Vorher {{#wenn projekt.kunde}} mitten", "{{/wenn}}"));
            Assert.Single(Probevorlagen.Mit(satz, "VF_PRUEF_BLOCK_ALLEIN"));
            Assert.Single(Probevorlagen.Mit(satz, "VF_PRUEF_WENN_KEIN_SCHALTER"));

            Pruefbefund ohne = Schnell(Probevorlagen.AusAbsaetzen("{{#wenn}}", "{{/wenn}}"));
            Assert.Single(Probevorlagen.Mit(ohne, "VF_PRUEF_WENN_OHNE_SCHALTER"));
        }

        [Fact]
        public void Musterzeile_mit_Zellverbund_und_Block_ueber_Tabellengrenze()
        {
            byte[] verbunden = Probevorlagen.Baue(b => b.Tabelle(new[]
            {
                new[] { "Variante", "Wert", "Einheit" },
                new[] { "{{#je stand}}Name", "Wert", "kWh{{/je}}" },
            }, spanne: (2, 2)));
            Pruefbefund v = Schnell(verbunden);
            Pruefmeldung verbund = Assert.Single(Probevorlagen.Mit(v, "VF_PRUEF_BLOCK_VERBUNDEN"));
            Assert.StartsWith("Tabelle 1, Zeile 2, Zelle 1", verbund.Fundort);
            Assert.Empty(Probevorlagen.Mit(v, "VF_PRUEF_BLOCK_TABELLE"));

            byte[] sauber = Probevorlagen.Baue(b => b.Tabelle(new[] { new[] { "{{#je stand}}Name", "Wert", "kWh{{/je}}" } }));
            Pruefbefund s = Schnell(sauber);
            Assert.Empty(Probevorlagen.Mit(s, "VF_PRUEF_BLOCK_VERBUNDEN"));
            Assert.Empty(Probevorlagen.Mit(s, "VF_PRUEF_BLOCK_TABELLE"));
            Assert.Empty(Probevorlagen.Mit(s, "VF_PRUEF_BLOCK_ALLEIN"));

            byte[] grenze = Probevorlagen.Baue(b => b.Absatz("{{#je stand}}").Tabelle(new[] { new[] { "Wert", "{{/je}}" } }));
            Assert.Single(Probevorlagen.Mit(Schnell(grenze), "VF_PRUEF_BLOCK_TABELLE"));
        }

        // =====================================================================
        //  Rahmen: leere Vorlage, Felder, Kommentare, Sprache, Fassung, Gültigkeit
        // =====================================================================

        [Fact]
        public void Vorlage_ohne_Platzhalter_warnt_vor_dem_angehaengten_Sammelanker()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("Nur Text", "und noch mehr"));
            Pruefmeldung w = Assert.Single(befund.Meldungen);
            Assert.Equal("VF_PRUEF_OHNE_PLATZHALTER", w.Kennung);
            Assert.Equal(Befundstufe.Warnung, w.Stufe);
            Assert.Equal("Die Vorlage enthält keinen Platzhalter – der Bericht wird mit {{bericht.inhalt}} an ihr Ende gesetzt", w.Text);
            Assert.Equal("Datei", w.Fundort);
            Assert.Equal(0, befund.AnzahlPlatzhalter);
        }

        [Fact]
        public void DATE_und_TIME_Felder_bekommen_den_Hinweis_PAGE_nicht()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Absatz("{{projekt.kunde}}")
                .Element(new W.Paragraph(
                    new W.Run(new W.Text("Stand ") { Space = SpaceProcessingModeValues.Preserve }),
                    new W.Run(new W.FieldChar { FieldCharType = W.FieldCharValues.Begin }),
                    new W.Run(new W.FieldCode(" DATE \\@ \"dd.MM.yyyy\" ") { Space = SpaceProcessingModeValues.Preserve }),
                    new W.Run(new W.FieldChar { FieldCharType = W.FieldCharValues.Separate }),
                    new W.Run(new W.Text("25.09.2026")),
                    new W.Run(new W.FieldChar { FieldCharType = W.FieldCharValues.End })))
                .Element(new W.Paragraph(new W.SimpleField(new W.Run(new W.Text("12:00"))) { Instruction = " TIME " }))
                .Element(new W.Paragraph(new W.SimpleField(new W.Run(new W.Text("1"))) { Instruction = " PAGE " })));
            Pruefbefund befund = Schnell(vorlage);
            List<Pruefmeldung> felder = Probevorlagen.Mit(befund, "VF_PRUEF_DATUMSFELD");
            Assert.Equal(2, felder.Count);
            Assert.Equal("Das Feld DATE zeigt das Datum des Öffnens, nicht das des Berichts", felder[0].Text);
            Assert.Equal("Statt des Felds {{bericht.datum}} verwenden.", felder[0].WasTun);
            Assert.Equal("Absatz 2 beginnt mit „Stand 25.09.2026“", felder[0].Fundort);
            Assert.Contains("TIME", felder[1].Text);
        }

        [Fact]
        public void Kommentare_werden_gezaehlt_aber_nicht_geprueft()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b.Absatz("{{projekt.kunde}}").Kommentare("{{projekt.gibtsnicht}}", "zweiter"));
            Pruefbefund befund = Schnell(vorlage);
            Pruefmeldung k = Assert.Single(befund.Meldungen);
            Assert.Equal("Kommentare: 2 – sie werden beim Erstellen des Berichts entfernt", k.Text);
            Assert.Equal("Kommentare", k.Fundort);
            Assert.Equal(2, befund.Kommentare);
        }

        [Fact]
        public void Katalogfassung_und_Sprache_aus_custom_xml_Sprache_abweichend_warnt()
        {
            byte[] englisch = Probevorlagen.Baue(b => b.Absatz("{{projekt.kunde}}").Eigenschaften(2, "en"));
            Pruefbefund deutsch = Vorlagenpruefer.Pruefe(englisch, Pruefstufe.Schnell, new Pruefkontext { Englisch = false });
            Assert.Equal(2, deutsch.Katalogfassung);
            Assert.Equal("en", deutsch.Sprache);
            Assert.True(deutsch.SpracheAbweichend);
            Pruefmeldung sprache = Assert.Single(Probevorlagen.Mit(deutsch, "VF_PRUEF_SPRACHE"));
            Assert.Equal(Befundstufe.Warnung, sprache.Stufe);
            Assert.Equal("Die Vorlage ist auf Englisch angelegt, der Bericht entsteht auf Deutsch", sprache.Text);
            Assert.Equal("Dokumenteigenschaften", sprache.Fundort);

            Pruefbefund passend = Vorlagenpruefer.Pruefe(englisch, Pruefstufe.Schnell, new Pruefkontext { Englisch = true });
            Assert.False(passend.SpracheAbweichend);
            Assert.True(passend.OhneBefund, Probevorlagen.Liste(passend));
            Assert.Equal("Unknown placeholder {{x.y}}",
                         Probevorlagen.Mit(Vorlagenpruefer.Pruefe(Probevorlagen.AusAbsaetzen("{{x.y}}"), Pruefstufe.Schnell,
                                                                  new Pruefkontext { Englisch = true }), "VF_PRUEF_UNBEKANNT")[0].Text);
        }

        [Fact]
        public void Aeltere_Katalogfassung_nur_bei_Alias_und_neuen_Kapiteln_neuere_nur_bei_Unbekanntem()
        {
            var umbenannt = new Vorlagenfeld("projekt.auftraggeber", Vorlagenfeldart.Text, Vorlagenfeldkontext.Stamm, w => null)
            { Aliasse = new[] { "projekt.bauherr" }, Seit = 2 };
            var kapitel = new Vorlagenfeld("kapitel.neu", Vorlagenfeldart.Kapitel, Vorlagenfeldkontext.Bericht, w => null) { Seit = 2 };
            Vorlagenkatalogsicht katalog = Erweitert(2, umbenannt, kapitel);

            Pruefbefund mitAlias = MitKatalog(Probevorlagen.Baue(b => b.Absatz("{{projekt.bauherr}}").Eigenschaften(1, null)), katalog);
            Pruefmeldung alt = Assert.Single(Probevorlagen.Mit(mitAlias, "VF_PRUEF_FASSUNG_ALT"));
            Assert.Equal("Die Vorlage stammt aus Katalogfassung 1; {{projekt.bauherr}} heißt seither {{projekt.auftraggeber}}", alt.Text);
            // Neben kapitel.neu bekommt die Vorlage der Fassung 1 auch je Kapitel des Katalogs v2 einen Hinweis.
            Pruefmeldung neu = Assert.Single(Probevorlagen.Mit(mitAlias, "VF_PRUEF_KAPITEL_NEU"), m => m.Marke == "{{kapitel.neu}}");
            Assert.Equal("Neues Kapitel {{kapitel.neu}} – in dieser Vorlage nicht enthalten", neu.Text);
            Assert.Equal(0, mitAlias.Fehleranzahl);

            Pruefbefund ohneAlias = MitKatalog(Probevorlagen.Baue(b => b.Absatz("{{projekt.kunde}}{{kapitel.neu}}").Eigenschaften(1, null)), katalog);
            Assert.Empty(Probevorlagen.Mit(ohneAlias, "VF_PRUEF_FASSUNG_ALT"));
            Assert.DoesNotContain(Probevorlagen.Mit(ohneAlias, "VF_PRUEF_KAPITEL_NEU"), m => m.Marke == "{{kapitel.neu}}");

            // Katalog v2 (BV-E2): Eine Vorlage der Fassung 1 bekommt je neuem Kapitel einen Hinweis — außer
            // sie führt den Sammelanker, der jedes Kapitel deckt; der Alias von Fassung 1 bleibt ohne Befund.
            Pruefbefund gleich = Schnell(Probevorlagen.Baue(b => b.Absatz("{{bericht.programmversion}}").Eigenschaften(1, null)));
            Assert.Empty(Probevorlagen.Mit(gleich, "VF_PRUEF_FASSUNG_ALT"));
            Assert.Equal(Berichtskapitel.Alle.Select(k => "{{" + k.Schluessel + "}}"),
                         Probevorlagen.Mit(gleich, "VF_PRUEF_KAPITEL_NEU").Select(m => m.Marke));
            Assert.All(gleich.Meldungen, m => Assert.Equal(Befundstufe.Hinweis, m.Stufe));
            Pruefbefund sammel = Schnell(Probevorlagen.Baue(b => b.Absatz("{{bericht.programmversion}}")
                                                              .Absatz("{{bericht.inhalt}}").Eigenschaften(1, null)));
            Assert.True(sammel.OhneBefund, Probevorlagen.Liste(sammel));
            Pruefbefund aktuell = Schnell(Probevorlagen.Baue(b => b.Absatz("{{bericht.programmversion}}").Eigenschaften(2, null)));
            Assert.True(aktuell.OhneBefund, Probevorlagen.Liste(aktuell));

            Pruefbefund neuer = Schnell(Probevorlagen.Baue(b => b.Absatz("{{projekt.zukunft}}").Eigenschaften(9, null)));
            Assert.Single(Probevorlagen.Mit(neuer, "VF_PRUEF_FASSUNG_NEU"));
        }

        [Fact]
        public void Werte_der_Wirtschaftlichkeit_ohne_Warnliste_warnen()
        {
            var kapitalwert = new Vorlagenfeld("wirtschaft.kapitalwert", Vorlagenfeldart.Zahl, Vorlagenfeldkontext.Gruppe, w => null);
            var warnliste = new Vorlagenfeld("wirtschaft.warnungen", Vorlagenfeldart.Liste, Vorlagenfeldkontext.Gruppe, w => null);
            Vorlagenkatalogsicht katalog = Erweitert(Vorlagenfeldkatalog.KATALOGFASSUNG, kapitalwert, warnliste);

            Pruefbefund ohne = MitKatalog(Probevorlagen.AusAbsaetzen("Kapitalwert {{wirtschaft.kapitalwert}}"), katalog);
            Pruefmeldung w = Assert.Single(Probevorlagen.Mit(ohne, "VF_PRUEF_GUELTIGKEIT"));
            Assert.Equal(Befundstufe.Warnung, w.Stufe);
            Assert.Equal("Eine Warnliste ergänzen, etwa {{bericht.warnungen}}.", w.WasTun);
            Assert.True(ohne.HatWirtschaftlichkeit);

            Pruefbefund mit = MitKatalog(Probevorlagen.AusAbsaetzen("Kapitalwert {{wirtschaft.kapitalwert}}", "{{wirtschaft.warnungen}}"), katalog);
            Assert.Empty(Probevorlagen.Mit(mit, "VF_PRUEF_GUELTIGKEIT"));

            Pruefbefund ohneWirtschaft = Schnell(Probevorlagen.AusAbsaetzen("{{projekt.kunde}}"));
            Assert.False(ohneWirtschaft.HatWirtschaftlichkeit);
            Assert.False(ohneWirtschaft.HatKapitel);
        }

        // =====================================================================
        //  Steuerelemente, Bilder, Textfelder
        // =====================================================================

        [Fact]
        public void Inhaltssteuerelemente_mit_Tag_zaehlen_fremde_Tags_nicht()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Element(new W.Paragraph(
                    new W.Run(new W.Text("Kunde: ") { Space = SpaceProcessingModeValues.Preserve }),
                    new W.SdtRun(new W.SdtProperties(new W.Tag { Val = "projekt.kunde" }),
                                 new W.SdtContentRun(new W.Run(new W.Text("Musterkunde"))))))
                .Element(new W.SdtBlock(new W.SdtProperties(new W.Tag { Val = "bericht.inhalt" }),
                                        new W.SdtContentBlock(Probevorlagen.Absatz("Inhalt"))))
                .Element(new W.SdtBlock(new W.SdtProperties(new W.Tag { Val = "Deckblatt" }),
                                        new W.SdtContentBlock(Probevorlagen.Absatz("fremd"))))
                .Element(new W.Paragraph(new W.SdtRun(new W.SdtProperties(new W.SdtAlias { Val = "Inhalt im Satz" }, new W.Tag { Val = "bericht.inhalt" }),
                                                      new W.SdtContentRun(new W.Run(new W.Text("x"))))))
                .Element(new W.Paragraph(new W.SdtRun(new W.SdtProperties(new W.Tag { Val = "projekt.gibtsnicht" }),
                                                      new W.SdtContentRun(new W.Run(new W.Text("y")))))));
            Pruefbefund befund = Schnell(vorlage);
            Assert.Equal(4, befund.AnzahlPlatzhalter);
            Pruefmeldung ort = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_ORT"));
            Assert.Contains("in einem Inhaltssteuerelement im Satz", ort.Text);
            Assert.EndsWith(", Inhaltssteuerelement „Inhalt im Satz“", ort.Fundort);
            Pruefmeldung unbekannt = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_UNBEKANNT"));
            Assert.Contains("Inhaltssteuerelement „projekt.gibtsnicht“", unbekannt.Fundort);
            Assert.Empty(Probevorlagen.Mit(befund, "VF_PRUEF_NORMALFORM"));
        }

        [Fact]
        public void Bild_mit_Schluessel_im_Alternativtext_und_Bild_ohne_Schluessel()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Roh(Probevorlagen.BildXml("Diagramm 1", "{{bild.vergleich.balken}}"))
                .Roh(Probevorlagen.BildXml("Logo", "Firmenlogo"))
                .Roh(Probevorlagen.BildXml("Kunde", "projekt.kunde")));
            Pruefbefund v1 = Schnell(vorlage);
            Pruefmeldung unbekannt = Assert.Single(Probevorlagen.Mit(v1, "VF_PRUEF_UNBEKANNT"));
            Assert.Equal("Absatz 1 (leer), Bild „Diagramm 1“", unbekannt.Fundort);
            Pruefmeldung text = Assert.Single(Probevorlagen.Mit(v1, "VF_PRUEF_ORT"));
            Assert.Contains("im Alternativtext eines Bildes", text.Text);
            Assert.Equal(2, v1.AnzahlPlatzhalter);

            Vorlagenkatalogsicht katalog = Erweitert(Vorlagenfeldkatalog.KATALOGFASSUNG, Feld("bild.vergleich.balken", Vorlagenfeldart.Bild));
            Pruefbefund mitBild = MitKatalog(Probevorlagen.Baue(b => b.Roh(Probevorlagen.BildXml("Diagramm 1", "{{bild.vergleich.balken}}"))), katalog);
            Assert.True(mitBild.OhneBefund, Probevorlagen.Liste(mitBild));
        }

        [Fact]
        public void Textfeld_in_beiden_Zweigen_zaehlt_und_meldet_einmal()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b.Absatz("Vorher").Roh(Probevorlagen.TextfeldXml("{{projekt.gibtsnicht}}")));
            Pruefbefund befund = Schnell(vorlage);
            Assert.Equal(1, befund.AnzahlPlatzhalter);
            Assert.Equal(2, befund.Funde.Count);
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_UNBEKANNT"));
            Assert.Equal("Textfeld 1 in Haupttext, Absatz 1 beginnt mit „{{projekt.gibtsnicht}}“", m.Fundort);

            using (WordprocessingDocument doc = WordprocessingDocument.Open(new MemoryStream(vorlage), false))
            {
                Vorlagendurchlauf lauf = Vorlagenteile.Durchlaufe(doc);
                List<Vorlagenabsatz> textfeld = lauf.Absaetze.Where(a => a.ImTextfeld).ToList();
                Assert.Equal(2, textfeld.Count);
                Assert.Equal(new[] { Vorlagenzweig.Wahl, Vorlagenzweig.Ersatz }, textfeld.Select(a => a.Ort.Zweig));
                Assert.Equal(textfeld[0].Ort.Kennung, textfeld[1].Ort.Kennung);
                Assert.Equal(1, lauf.Textfelder);
                Assert.Equal("", lauf.Absaetze[1].Text);   // der Wirtsabsatz trägt den Text des Textfelds nicht
            }
        }

        /// <summary>
        /// Inhaltssteuerelemente um Tabellenzeilen oder -zellen füllt die Engine nicht (Nachtrag A2):
        /// „passt nicht an diese Stelle“, gleich welcher Art der Schlüssel ist.
        /// </summary>
        [Fact]
        public void Inhaltssteuerelemente_um_Zeilen_und_Zellen_passen_nicht()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b.Element(new W.Table(
                new W.TableGrid(new W.GridColumn { Width = "2000" }),
                new W.TableRow(new W.SdtCell(new W.SdtProperties(new W.Tag { Val = "projekt.kunde" }),
                                             new W.SdtContentCell(new W.TableCell(Probevorlagen.Absatz("Kunde"))))),
                new W.SdtRow(new W.SdtProperties(new W.Tag { Val = "bericht.warnungen" }),
                             new W.SdtContentRow(new W.TableRow(new W.TableCell(Probevorlagen.Absatz("Hinweise"))))))));
            Pruefbefund befund = Schnell(vorlage);
            List<Pruefmeldung> ort = Probevorlagen.Mit(befund, "VF_PRUEF_ORT");
            Assert.Equal(2, ort.Count);
            Assert.All(ort, m => Assert.Contains("in einem Inhaltssteuerelement um Tabellenzeilen oder -zellen", m.Text));
            Assert.Equal("Das Inhaltssteuerelement in der Zelle um einen Absatz legen oder den Platzhalter als Text in die Zelle schreiben.",
                         ort[0].WasTun);
            Assert.StartsWith("Tabelle 1, Zeile 1, Zelle 1", ort[0].Fundort);
        }

        /// <summary>
        /// Die Tag-Regel der Engine (Nachtrag A2, BV-E4): ein Tag ist ein Platzhalter, wenn er in doppelten
        /// Klammern steht, eine Blockmarke ist (auch ohne Klammern) oder ein Schlüssel mit Punkt; sonst bleibt
        /// er ohne Befund. Ein Alternativtext
        /// ist freier Text und braucht dazu einen Bereich des Schlüsselschemas.
        /// </summary>
        [Fact]
        public void Tags_zaehlen_mit_Klammern_oder_Punkt_Alternativtexte_nur_mit_Bereich()
        {
            Assert.True(Vorlagenpruefer.IstPlatzhalterTag("projekt.kunde"));
            Assert.True(Vorlagenpruefer.IstPlatzhalterTag("{{projekt.kunde}}"));
            Assert.True(Vorlagenpruefer.IstPlatzhalterTag("{{#je stand}}"));
            Assert.True(Vorlagenpruefer.IstPlatzhalterTag("vorlage.version"));
            Assert.True(Vorlagenpruefer.IstPlatzhalterTag("#je stand"));
            Assert.True(Vorlagenpruefer.IstPlatzhalterTag("#wenn nicht hat.varianten"));
            Assert.False(Vorlagenpruefer.IstPlatzhalterTag("#Kapitel"));
            Assert.False(Vorlagenpruefer.IstPlatzhalterTag("Deckblatt"));
            Assert.False(Vorlagenpruefer.IstPlatzhalterTag("{{a}} und {{b}}"));
            Assert.False(Vorlagenpruefer.IstPlatzhalterTag(" "));

            Assert.True(Vorlagenpruefer.IstBildschluessel("{{bild.vergleich.balken}}"));
            Assert.True(Vorlagenpruefer.IstBildschluessel("projekt.kunde"));
            Assert.False(Vorlagenpruefer.IstBildschluessel("Logo.png"));
            Assert.False(Vorlagenpruefer.IstBildschluessel("{{#je stand}}"));
            Assert.False(Vorlagenpruefer.IstBildschluessel("Firmenlogo"));

            byte[] vorlage = Probevorlagen.Baue(b => b
                .Element(new W.SdtBlock(new W.SdtProperties(new W.Tag { Val = "#je stand" }), new W.SdtContentBlock(Probevorlagen.Absatz("a"))))
                .Element(new W.SdtBlock(new W.SdtProperties(new W.Tag { Val = "{{#je stand}}" }), new W.SdtContentBlock(Probevorlagen.Absatz("b"))))
                .Element(new W.SdtBlock(new W.SdtProperties(new W.Tag { Val = "vorlage.version" }), new W.SdtContentBlock(Probevorlagen.Absatz("c"))))
                .Roh(Probevorlagen.BildXml("Logo", "Logo.png")));
            Pruefbefund befund = Schnell(vorlage);
            Assert.Equal(3, befund.AnzahlPlatzhalter);
            Assert.Empty(Probevorlagen.Mit(befund, "VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT"));
            Assert.Contains("Inhaltssteuerelement „vorlage.version“", Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_UNBEKANNT")).Fundort);
        }

        // =====================================================================
        //  Volle Prüfung: Paket, Format, Makros, Änderungen, Extern, Stile
        // =====================================================================

        [Fact]
        public void Nachverfolgte_Aenderungen_nur_in_der_vollen_Pruefung()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Absatz("{{projekt.kunde}}")
                .Element(new W.Paragraph(new W.InsertedRun(new W.Run(new W.Text("neu"))) { Id = "1", Author = "Probe", Date = new DateTime(2026, 9, 25) },
                                         new W.DeletedRun(new W.Run(new W.DeletedText("alt"))) { Id = "2", Author = "Probe" })));
            Assert.Empty(Probevorlagen.Mit(Schnell(vorlage), "VF_PRUEF_AENDERUNGEN"));
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(Voll(vorlage), "VF_PRUEF_AENDERUNGEN"));
            Assert.Equal("Nachverfolgte Änderungen in Haupttext: 2", m.Text);
            Assert.Equal("In Word unter „Überprüfen“ alle Änderungen annehmen oder ablehnen und speichern.", m.WasTun);
            Assert.Equal(Befundstufe.Fehler, m.Stufe);
        }

        [Fact]
        public void Makro_Endung_Makroformat_und_VBA_Projekt_sind_Fehler_dotx_ist_erlaubt()
        {
            byte[] docx = Probevorlagen.AusAbsaetzen("{{projekt.kunde}}");
            Assert.Empty(Probevorlagen.Mit(Vorlagenpruefer.Pruefe(docx, Pruefstufe.Schnell, new Pruefkontext { Dateiname = "x.docm" }), "VF_PRUEF_FORMAT"));
            Pruefmeldung endung = Assert.Single(Probevorlagen.Mit(
                Vorlagenpruefer.Pruefe(docx, Pruefstufe.Voll, new Pruefkontext { Dateiname = "Vorlage.docm" }), "VF_PRUEF_FORMAT"));
            Assert.Equal("Das Dateiformat .docm wird nicht unterstützt", endung.Text);

            byte[] docm = Probevorlagen.Baue(b =>
            {
                b.Absatz("{{projekt.kunde}}");
                VbaProjectPart vba = b.Main.AddNewPart<VbaProjectPart>();
                using (var daten = new MemoryStream(new byte[] { 1, 2, 3, 4 })) vba.FeedData(daten);
            }, WordprocessingDocumentType.MacroEnabledDocument);
            Pruefbefund makro = Voll(docm);
            Assert.Single(Probevorlagen.Mit(makro, "VF_PRUEF_FORMAT"));
            Assert.Single(Probevorlagen.Mit(makro, "VF_PRUEF_MAKROS"));

            byte[] dotx = Probevorlagen.Baue(b => b.Absatz("{{projekt.kunde}}"), WordprocessingDocumentType.Template);
            Pruefbefund vorlage = Vorlagenpruefer.Pruefe(dotx, Pruefstufe.Voll, new Pruefkontext { Dateiname = "Vorlage.dotx" });
            Assert.True(vorlage.OhneBefund, Probevorlagen.Liste(vorlage));
        }

        [Fact]
        public void Groessen_werden_vor_dem_Oeffnen_begrenzt()
        {
            Assert.Equal(20L * 1024 * 1024, Vorlagenpruefer.GRENZE_DATEI);
            Assert.Equal(100L * 1024 * 1024, Vorlagenpruefer.GRENZE_ENTPACKT);

            byte[] vorlage = Probevorlagen.AusAbsaetzen("{{projekt.kunde}}");
            Pruefbefund datei = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, Deutsch, null, new Pruefgrenzen(100, long.MaxValue));
            Pruefmeldung g = Assert.Single(datei.Meldungen);
            Assert.Equal("VF_PRUEF_GROESSE", g.Kennung);
            Assert.False(datei.IstLesbar);

            Pruefbefund entpackt = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, Deutsch, null, new Pruefgrenzen(long.MaxValue, 500));
            Assert.Equal("VF_PRUEF_GROESSE_ENTPACKT", Assert.Single(entpackt.Meldungen).Kennung);
        }

        [Fact]
        public void Fremde_und_kaputte_Dateien_werden_benannt()
        {
            Assert.Equal("Die Vorlage kann nicht gelesen werden: Die Datei ist leer.", Assert.Single(Schnell(new byte[0]).Meldungen).Text);

            byte[] ole = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0, 0, 0 };
            Pruefmeldung doc = Assert.Single(Schnell(ole).Meldungen);
            Assert.Equal("VF_PRUEF_FORMAT", doc.Kennung);
            Assert.Contains(".doc", doc.Text);

            Assert.Equal("VF_PRUEF_FORMAT", Assert.Single(Schnell(Encoding.ASCII.GetBytes("{\\rtf1 Text}")).Meldungen).Kennung);
            Assert.Equal("VF_PRUEF_UNLESBAR", Assert.Single(Schnell(Encoding.ASCII.GetBytes("Hallo Welt")).Meldungen).Kennung);

            byte[] zip;
            using (var strom = new MemoryStream())
            {
                using (var archiv = new ZipArchive(strom, ZipArchiveMode.Create, true))
                using (var schreiber = new StreamWriter(archiv.CreateEntry("hallo.txt").Open()))
                    schreiber.Write("kein Word");
                zip = strom.ToArray();
            }
            Pruefbefund kaputt = Voll(zip);
            Assert.Equal("VF_PRUEF_UNLESBAR", Assert.Single(kaputt.Meldungen).Kennung);
            Assert.False(kaputt.IstLesbar);
        }

        [Fact]
        public void Externe_Beziehungen_und_Vorlagenverweis_sind_Hinweise()
        {
            byte[] vorlage = Probevorlagen.Baue(b =>
            {
                b.Absatz("{{projekt.kunde}}");
                b.Main.AddExternalRelationship("http://schemas.openxmlformats.org/officeDocument/2006/relationships/image",
                                               new Uri("file:///C:/Bilder/logo.png"));
                DocumentSettingsPart einstellungen = b.Main.AddNewPart<DocumentSettingsPart>();
                einstellungen.Settings = new W.Settings();
                einstellungen.AddExternalRelationship("http://schemas.openxmlformats.org/officeDocument/2006/relationships/attachedTemplate",
                                                      new Uri("file:///C:/Vorlagen/Normal.dotm"));
            });
            Pruefbefund befund = Voll(vorlage);
            Pruefmeldung verknuepft = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_EXTERN"));
            Assert.Equal(Befundstufe.Hinweis, verknuepft.Stufe);
            Assert.Equal("Verknüpfte Inhalte: 1 – sie werden beim Erstellen des Berichts entfernt", verknuepft.Text);
            Pruefmeldung verweis = Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_VORLAGENVERWEIS"));
            Assert.Equal("Der Verweis auf die Dokumentvorlage Normal.dotm wird entfernt", verweis.Text);
            Assert.Equal(0, befund.Fehleranzahl);
            Assert.Empty(Schnell(vorlage).Meldungen);
        }

        /// <summary>
        /// Deutsche Stil-IDs (<c>berschrift1</c> …) mit den Namen „heading 1“ bis „heading 3“ und ihrer
        /// Gliederungsebene sind KEIN Befund — gefunden wird über <c>w:name</c> (Konzept 6.2, Messprobe 5).
        /// </summary>
        [Fact]
        public void Ueberschriftenstile_werden_ueber_den_Namen_gefunden()
        {
            Pruefbefund deutsch = Voll(Probevorlagen.Baue(b => b.Absatz("{{bericht.inhalt}}").DeutscheUeberschriften()));
            Assert.True(deutsch.OhneBefund, Probevorlagen.Liste(deutsch));

            Pruefbefund ohneEbene = Voll(Probevorlagen.Baue(b => b.Absatz("{{bericht.inhalt}}").DeutscheUeberschriften(dritteMitEbene: false)));
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(ohneEbene, "VF_PRUEF_UEBERSCHRIFTEN"));
            Assert.Equal("Überschriftenstile fehlen oder tragen keine Gliederungsebene: Überschrift 3 ohne Gliederungsebene", m.Text);
            Assert.Equal("Formatvorlagen", m.Fundort);
            Assert.Equal(Befundstufe.Warnung, m.Stufe);

            Pruefbefund ohneStile = Voll(Probevorlagen.AusAbsaetzen("{{bericht.inhalt}}"));
            Assert.Contains("Überschrift 1 fehlt", Assert.Single(Probevorlagen.Mit(ohneStile, "VF_PRUEF_UEBERSCHRIFTEN")).Text);

            // Ohne Kapitel setzt EPOS-Plan keine Überschrift in die Vorlage — kein Befund.
            Pruefbefund nurText = Voll(Probevorlagen.AusAbsaetzen("{{projekt.kunde}}"));
            Assert.True(nurText.OhneBefund, Probevorlagen.Liste(nurText));

            // Wie die Engine: gefunden auch über die ID; die Gliederungsebene zählt, wenn sie da ist.
            Pruefbefund ueberId = Voll(Probevorlagen.Baue(b => b.Absatz("{{bericht.inhalt}}").Stile(
                Probevorlagen.Stil("Heading1", "Überschrift eins", 0),
                Probevorlagen.Stil("Heading2", "Zweite Ebene", 1),
                Probevorlagen.Stil("berschrift3", "heading 3", 5))));
            Assert.True(ueberId.OhneBefund, Probevorlagen.Liste(ueberId));
        }

        /// <summary>
        /// BV-E1 B1a: Die Überschriftenprüfung IST die Rollenauflösung der Engine
        /// (<see cref="WordVorlagenstile.Finde"/>). Die ID vergleicht die Engine genau, den Namen ohne
        /// Rücksicht auf Groß- und Kleinschreibung — ein Stil mit der ID „heading2“ und einem freien
        /// Namen ist für sie keine Überschrift 2, sie legte beim Füllen eine an. Genau dann warnt der
        /// Prüfer; für jede der drei Ebenen gilt: „fehlt“ im Befund ⇔ <c>Finde</c> liefert nichts.
        /// </summary>
        [Fact]
        public void Ueberschriftenpruefung_folgt_der_Rollenaufloesung_der_Engine()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b.Absatz("{{bericht.inhalt}}").Stile(
                Probevorlagen.Stil("Standard", "Normal", null, standard: true),
                Probevorlagen.Stil("HEADING1", "heading 1", 0),        // über den Namen gefunden
                Probevorlagen.Stil("heading2", "Zweite Ebene", 1),     // ID anders geschrieben, Name frei: fehlt
                Probevorlagen.Stil("Heading3", "Dritte Ebene", 2)));   // über die ID gefunden

            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(Voll(vorlage), "VF_PRUEF_UEBERSCHRIFTEN"));
            Assert.Equal("Überschriftenstile fehlen oder tragen keine Gliederungsebene: Überschrift 2 fehlt", m.Text);

            using var strom = new MemoryStream(vorlage, false);
            using WordprocessingDocument doc = WordprocessingDocument.Open(strom, false);
            var rollen = new WordVorlagenstile(doc.MainDocumentPart);
            string[] ebenen = { WordVorlagenstile.UEBERSCHRIFT1, WordVorlagenstile.UEBERSCHRIFT2, WordVorlagenstile.UEBERSCHRIFT3 };
            for (int n = 1; n <= 3; n++)
            {
                bool fehltLautPruefer = m.Text.Contains("Überschrift " + n + " fehlt", StringComparison.Ordinal);
                Assert.Equal(fehltLautPruefer, rollen.Finde(ebenen[n - 1]) == null);
            }
            Assert.Equal("HEADING1", rollen.Finde(WordVorlagenstile.UEBERSCHRIFT1));
            Assert.Equal("Heading3", rollen.Finde(WordVorlagenstile.UEBERSCHRIFT3));
        }

        // =====================================================================
        //  Kapitel, Häkchen, Stellen (BV-E2)
        // =====================================================================

        /// <summary>
        /// <see cref="Pruefbefund.Bausteine"/> sind die Häkchen, deren Kapitel die Vorlage führt — einzeln an
        /// gültiger Stelle, über den Sammelanker alle, ohne jeden Platzhalter ebenfalls alle (die Engine hängt
        /// den Sammelanker an); der Anhang E zählt zur Wirtschaftlichkeit. Ein Kapitel in der Tabellenzelle
        /// führt die Vorlage nicht (Ortsfehler). Eine Vorlage, die Kapitel bewusst weglässt, bekommt keinen Befund.
        /// </summary>
        [Fact]
        public void Die_Kapitel_der_Vorlage_bestimmen_ihre_Haekchen()
        {
            Pruefbefund einzeln = Schnell(Probevorlagen.AusAbsaetzen("{{kapitel.projekt}}", "{{kapitel.anhang_e|ohne titel}}"));
            // Allein der Anhang E meldet die Kapitel, auf die er verweist und die fehlen (Warnung, kein Fehler).
            Assert.All(einzeln.Meldungen, m => Assert.Equal("VF_PRUEF_ANHANG_E_STELLE", m.Kennung));
            Assert.Equal(new[] { "{{kapitel.deckblatt}}", "{{kapitel.komponenten}}", "{{kapitel.ergebnisse}}", "{{kapitel.vergleich}}",
                                 "{{kapitel.wirtschaftlichkeit}}", "{{kapitel.anhang}}" },
                         einzeln.Meldungen.Select(m => m.Marke));
            Assert.Equal(0, einzeln.Fehleranzahl);
            Assert.Equal(new[] { BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_WIRTSCHAFT }, einzeln.Bausteine);
            Assert.True(einzeln.HatKapitel);
            Assert.False(einzeln.HatWirtschaftlichkeit);   // der Anhang E ist nicht die Wirtschaftlichkeit

            Pruefbefund sammel = Schnell(Probevorlagen.AusAbsaetzen("{{bericht.inhalt}}"));
            Assert.Equal(BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel), sammel.Bausteine);
            Assert.True(sammel.HatWirtschaftlichkeit);

            Pruefbefund ohne = Schnell(Probevorlagen.AusAbsaetzen("Nur Text"));
            Assert.Equal(BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel), ohne.Bausteine);

            Pruefbefund nurText = Schnell(Probevorlagen.AusAbsaetzen("{{projekt.kunde}}"));
            Assert.Empty(nurText.Bausteine);
            Assert.False(nurText.HatKapitel);

            Pruefbefund zelle = Schnell(Probevorlagen.Baue(b => b.Tabelle(new[] { new[] { "{{kapitel.vergleich}}" } })));
            Assert.Empty(zelle.Bausteine);
            Assert.Single(Probevorlagen.Mit(zelle, "VF_PRUEF_ORT"));
            Assert.Null(zelle.Kapitelstellen[BerichtsKonfiguration.B_VERGLEICH]);
        }

        /// <summary>
        /// Ein Kapitel zweimal: Die erste gültige Stelle zählt, jede weitere bekommt den Hinweis „doppelt“
        /// (die Engine lässt sie gelb stehen) — auch der Sammelanker. Ein Fehler ist es nicht.
        /// </summary>
        [Fact]
        public void Ein_Kapitel_zweimal_ist_ein_Hinweis_an_der_zweiten_Stelle()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("{{kapitel.anhang}}", "Text", "{{kapitel.anhang|ebene 2}}",
                                                                     "{{bericht.inhalt}}", "{{bericht.inhalt}}"));
            List<Pruefmeldung> doppelt = Probevorlagen.Mit(befund, "VF_PRUEF_KAPITEL_DOPPELT");
            Assert.Equal(2, doppelt.Count);
            Assert.All(doppelt, m => Assert.Equal(Befundstufe.Hinweis, m.Stufe));
            Assert.Equal("Kapitel {{kapitel.anhang}} steht mehrfach in der Vorlage – gefüllt wird nur die erste Stelle", doppelt[0].Text);
            Assert.StartsWith("Absatz 3", doppelt[0].Fundort, StringComparison.Ordinal);
            Assert.Equal("{{kapitel.anhang|ebene 2}}", doppelt[0].Marke);
            Assert.StartsWith("Absatz 5", doppelt[1].Fundort, StringComparison.Ordinal);
            Assert.Equal(0, befund.Fehleranzahl);
        }

        /// <summary>
        /// Katalog v2: Ein Schalter (<c>baustein.*</c>) außerhalb einer Bedingung ist ein Ortsfehler (ohne
        /// Hinweis „später“ — Bedingungen wertet die Engine aus); das Logo als getippter Text ist ein Fehler — die
        /// Engine füllt allein das Bild mit dem Schlüssel im Alternativtext, und das ohne Befund, auch wenn
        /// kein Logo eingestellt ist.
        /// </summary>
        [Fact]
        public void Schalter_und_Bild_als_Text_wirken_erst_spaeter_das_Platzhalterbild_ist_ohne_Befund()
        {
            Pruefbefund schalter = Schnell(Probevorlagen.AusAbsaetzen("{{baustein.projekt}}"));
            Pruefmeldung ort = Assert.Single(Probevorlagen.Mit(schalter, "VF_PRUEF_ORT"));
            Assert.Equal(Befundstufe.Fehler, ort.Stufe);
            Assert.Equal("Den Schalter nur als Bedingung verwenden: {{#wenn baustein.projekt}} … {{/wenn}}.", ort.WasTun);
            Assert.Empty(Probevorlagen.Mit(schalter, "VF_PRUEF_SPAETER"));

            Pruefbefund logoText = Schnell(Probevorlagen.AusAbsaetzen("{{bild.ersteller.logo}}"));
            Pruefmeldung bild = Assert.Single(logoText.Meldungen);
            Assert.Equal("VF_PRUEF_SPAETER", bild.Kennung);
            Assert.Equal(Befundstufe.Fehler, bild.Stufe);
            Assert.Contains("{{bild.ersteller.logo}} als Alternativtext", bild.WasTun, StringComparison.Ordinal);

            Pruefbefund logoBild = Schnell(Probevorlagen.Baue(b => b.Roh(Probevorlagen.BildXml("Logo", "{{bild.ersteller.logo}}"))));
            Assert.True(logoBild.OhneBefund, Probevorlagen.Liste(logoBild));
            Assert.Equal(new[] { Vorlagenfeldkatalog.LOGO }, logoBild.Schluessel);
        }

        /// <summary>
        /// Führt die Vorlage den Anhang E, warnt der Prüfer für jedes Kapitel, auf das seine Checkliste
        /// verweist, das die Vorlage aber nicht führt („nicht im Bericht“); Deckblattangaben aus Platzhaltern
        /// gelten als Deckblatt. Ohne Anhang E, über den Sammelanker oder mit allen Kapiteln keine Warnung.
        /// </summary>
        [Fact]
        public void Anhang_E_ohne_Stelle_warnt_je_fehlendem_Kapitel()
        {
            Pruefbefund befund = Schnell(Probevorlagen.AusAbsaetzen("{{kapitel.wirtschaftlichkeit}}", "{{kapitel.anhang_e}}"));
            List<Pruefmeldung> ohne = Probevorlagen.Mit(befund, "VF_PRUEF_ANHANG_E_STELLE");
            Assert.Equal(new[]
            {
                "{{kapitel.deckblatt}}", "{{kapitel.projekt}}", "{{kapitel.komponenten}}", "{{kapitel.ergebnisse}}",
                "{{kapitel.vergleich}}", "{{kapitel.anhang}}",
            }, ohne.Select(m => m.Marke));
            Assert.All(ohne, m => Assert.Equal(Befundstufe.Warnung, m.Stufe));
            Assert.Equal("Anhang E ohne Stelle für Kapitel „Projektbeschreibung“ – die Checkliste nennt dort „nicht im Bericht“",
                         ohne[1].Text);
            Assert.Equal("Das Kapitel mit {{kapitel.projekt}} in die Vorlage aufnehmen, wenn der Bewertungsbericht es zeigen soll.",
                         ohne[1].WasTun);

            Pruefbefund mitDeckblatt = Schnell(Probevorlagen.AusAbsaetzen("{{bericht.titel}}", "{{kapitel.wirtschaftlichkeit}}",
                                                                           "{{kapitel.anhang_e}}"));
            Assert.Equal(5, Probevorlagen.Mit(mitDeckblatt, "VF_PRUEF_ANHANG_E_STELLE").Count);
            Assert.True(mitDeckblatt.DeckblattAusPlatzhaltern);

            Assert.Empty(Probevorlagen.Mit(Schnell(Probevorlagen.AusAbsaetzen("{{kapitel.wirtschaftlichkeit}}")), "VF_PRUEF_ANHANG_E_STELLE"));
            Assert.Empty(Probevorlagen.Mit(Schnell(Probevorlagen.AusAbsaetzen("{{bericht.inhalt}}")), "VF_PRUEF_ANHANG_E_STELLE"));
        }

        /// <summary>
        /// Die Stellen der Kapitel (Konzept 11 Nr. 3): der Kapitelkopf unmittelbar vor dem Anker, Platzhalter
        /// darin aufgelöst (<c>{{text.kapitel_wirtschaftlichkeit}}</c>, in der Sprache des Berichts); mit
        /// <c>|ohne titel</c> ohne Kapitelkopf die nächste Überschrift davor; sonst die eigene Überschrift des
        /// Bausteins. Ein Kapitel, das die Vorlage nicht führt, hat keine Stelle.
        /// </summary>
        [Fact]
        public void Die_Stelle_ist_der_Kapitelkopf_sonst_die_Ueberschrift_davor_sonst_die_eigene()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Stile(Probevorlagen.Stil("EPOSKapitelkopf", "EPOS Kapitelkopf", 0), Probevorlagen.Stil("Heading1", "heading 1", 0))
                .Roh("<w:pPr><w:pStyle w:val=\"EPOSKapitelkopf\"/></w:pPr><w:r><w:t>{{text.kapitel_wirtschaftlichkeit}}</w:t></w:r>")
                .Absatz("{{kapitel.wirtschaftlichkeit|ohne titel}}")
                .Absatz("{{kapitel.projekt}}")
                .Roh("<w:pPr><w:pStyle w:val=\"Heading1\"/></w:pPr><w:r><w:t>Mein Anhang</w:t></w:r>")
                .Absatz("Einleitung")
                .Absatz("{{kapitel.anhang|ohne titel}}"));

            Pruefbefund deutsch = Schnell(vorlage);
            Assert.Equal("Wirtschaftlichkeit", deutsch.Kapitelstellen[BerichtsKonfiguration.B_WIRTSCHAFT]);
            Assert.Equal("Projektbeschreibung", deutsch.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Equal("Mein Anhang", deutsch.Kapitelstellen[BerichtsKonfiguration.B_ANHANG]);
            Assert.Null(deutsch.Kapitelstellen[BerichtsKonfiguration.B_VERGLEICH]);
            Assert.Null(deutsch.Kapitelstellen[Berichtskapitel.ANHANG_E]);
            Assert.False(deutsch.DeckblattAusPlatzhaltern);

            Pruefbefund englisch = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, new Pruefkontext { Englisch = true });
            Assert.Equal("Economic viability", englisch.Kapitelstellen[BerichtsKonfiguration.B_WIRTSCHAFT]);
            Assert.Equal("Project description", englisch.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
        }

        // =====================================================================
        //  Befund, Prüfsumme, Reihenfolge
        // =====================================================================

        [Fact]
        public void Pruefsumme_ist_SHA256_und_Meldungen_nach_Gewicht_geordnet()
        {
            byte[] vorlage = Probevorlagen.AusAbsaetzen("{{ projekt.kunde }}", "{{projekt.gibtsnicht}}");
            Pruefbefund befund = Schnell(vorlage);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(vorlage)).ToLowerInvariant(), befund.Pruefsumme);
            Assert.Equal(64, befund.Pruefsumme.Length);
            Assert.Equal(new[] { Befundstufe.Fehler, Befundstufe.Hinweis }, befund.Meldungen.Select(m => m.Stufe));
            Assert.Equal(1, befund.Fehleranzahl);
            Assert.Equal(0, befund.Warnungen);
            Assert.Equal(1, befund.Hinweise);
            Assert.True(befund.HatFehler);
        }

        [Fact]
        public void Jede_Meldung_hat_Text_Fundort_WasTun_und_eine_Kennung_mit_Ressource()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Absatz("{{projekt.gibtsnicht}}", " {{#je stand}}")
                .Absatz("{{bericht.warnungen}} im Satz")
                .Kopfzeile(Probevorlagen.Absatz("{{bericht.inhalt}}"))
                .Kommentare("k"));
            foreach (bool englisch in new[] { false, true })
            {
                Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, new Pruefkontext { Englisch = englisch });
                Assert.NotEmpty(befund.Meldungen);
                foreach (Pruefmeldung m in befund.Meldungen)
                {
                    Assert.False(string.IsNullOrWhiteSpace(m.Text), m.ToString());
                    Assert.False(string.IsNullOrWhiteSpace(m.Fundort), m.ToString());
                    Assert.False(string.IsNullOrWhiteSpace(m.WasTun), m.ToString());
                    Assert.StartsWith("VF_PRUEF_", m.Kennung);
                    Assert.DoesNotContain("VF_PRUEF_", m.Text);
                    Assert.DoesNotContain("VF_PRUEF_", m.WasTun);
                    Assert.DoesNotContain("VF_PRUEF_", m.Fundort);
                    Assert.NotNull(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(m.Kennung));
                }
            }
        }

        // =====================================================================
        //  Vorlagenteile: Fundorte, Nummern, Text
        // =====================================================================

        [Fact]
        public void Fundorte_nennen_Teil_Tabelle_Zeile_Zelle_und_Anfang()
        {
            byte[] vorlage = Probevorlagen.Baue(b => b
                .Absatz("Einleitung")
                .Tabelle(new[] { new[] { "Größe", "Wert" }, new[] { "Wärme aus Erdreich", "{{projekt.gibtsnicht}}" } })
                .Fusszeile(Probevorlagen.Absatz("Seite"), Probevorlagen.Absatz("{{x.eins}}")));
            Pruefbefund befund = Schnell(vorlage);
            List<Pruefmeldung> m = Probevorlagen.Mit(befund, "VF_PRUEF_UNBEKANNT");
            Assert.Equal("Tabelle 1, Zeile 2, Zelle 2 beginnt mit „{{projekt.gibtsnicht}}“", m[0].Fundort);
            Assert.Equal("Fußzeile 1, Absatz 2 beginnt mit „{{x.eins}}“", m[1].Fundort);

            Pruefbefund englisch = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, new Pruefkontext { Englisch = true });
            Assert.Equal("Footer 1, Paragraph 2 starting with “{{x.eins}}”", Probevorlagen.Mit(englisch, "VF_PRUEF_UNBEKANNT")[1].Fundort);
        }

        [Fact]
        public void Kopfzeilen_folgen_den_Abschnitten_und_Anfang_kuerzt()
        {
            byte[] vorlage = Probevorlagen.Baue(b =>
            {
                HeaderPart erste = b.Main.AddNewPart<HeaderPart>();
                erste.Header = new W.Header(Probevorlagen.Absatz("Erste"));
                HeaderPart zweite = b.Main.AddNewPart<HeaderPart>();
                zweite.Header = new W.Header(Probevorlagen.Absatz("Zweite"));
                // Abschnitt 1 verweist auf die ZWEITE angelegte Kopfzeile, der letzte auf die erste.
                b.Body.Append(new W.Paragraph(new W.ParagraphProperties(new W.SectionProperties(
                    new W.HeaderReference { Type = W.HeaderFooterValues.Default, Id = b.Main.GetIdOfPart(zweite) }))));
                b.Abschnitt.Append(new W.HeaderReference { Type = W.HeaderFooterValues.Default, Id = b.Main.GetIdOfPart(erste) });
            });
            using (WordprocessingDocument doc = WordprocessingDocument.Open(new MemoryStream(vorlage), false))
            {
                Vorlagendurchlauf lauf = Vorlagenteile.Durchlaufe(doc);
                List<Vorlagenabsatz> kopf = lauf.Absaetze.Where(a => a.Ort.Teil == Vorlagenteilart.Kopfzeile).ToList();
                Assert.Equal(new[] { "Zweite", "Erste" }, kopf.Select(a => a.Text));
                Assert.Equal(new[] { 1, 2 }, kopf.Select(a => a.Ort.Teilnummer));
            }

            Assert.Equal("eins zwei drei vier …", Vorlagenteile.Anfang("eins zwei\tdrei\nvier fünf"));
            Assert.Equal("kurz", Vorlagenteile.Anfang("  kurz  "));
            Assert.Equal("", Vorlagenteile.Anfang("   "));
            Assert.Equal(new string('x', 40) + " …", Vorlagenteile.Anfang(new string('x', 50)));
        }

        [Fact]
        public void Absatztext_ohne_Geloeschtes_Feldanweisungen_und_Eigenschaften()
        {
            var absatz = new W.Paragraph(
                new W.ParagraphProperties(new W.Tabs(new W.TabStop { Val = W.TabStopValues.Left, Position = 100 })),
                new W.Run(new W.Text("A")),
                new W.Run(new W.TabChar()),
                new W.Run(new W.FieldCode(" PAGE ")),
                new W.DeletedRun(new W.Run(new W.DeletedText("weg"))),
                new W.InsertedRun(new W.Run(new W.Text("B"))),
                new W.Hyperlink(new W.Run(new W.Text("C"))),
                new W.Run(new W.Break()),
                new W.Run(new W.Text("D")));
            Assert.Equal("A\tBC\nD", Vorlagenteile.Absatztext(absatz));
        }
    }
}
