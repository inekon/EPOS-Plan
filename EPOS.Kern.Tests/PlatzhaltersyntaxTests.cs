using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Syntax der Berichtsvorlagen</b> (Konzept Berichtsvorlagen mit Platzhaltern 4.2, 4.5,
    /// 4.8; Etappe BV-E1): Normierung, Formatangaben, Blockmarken, Fundorte und Fehlfälle von
    /// <see cref="Platzhaltersyntax"/>. Ohne Datenbank, ohne Kultur.
    /// </summary>
    public class PlatzhaltersyntaxTests
    {
        private static readonly string Nbsp = ((char)0x00A0).ToString();
        private static readonly string SchmalNbsp = ((char)0x202F).ToString();
        private static readonly string Nullbreite = ((char)0x200B).ToString();
        private static readonly string Trennstrich = ((char)0x00AD).ToString();
        private static readonly string Trema = ((char)0x0308).ToString();

        private static Platzhalter Einer(string text)
        {
            List<Platzhalter> alle = Platzhaltersyntax.Finde(text).ToList();
            Assert.Single(alle);
            return alle[0];
        }

        // =====================================================================
        //  Normierung
        // =====================================================================

        [Theory]
        [InlineData("{{projekt.kunde}}", true)]
        [InlineData("{{ projekt.kunde }}", false)]
        [InlineData("{{Projekt.Kunde}}", false)]
        [InlineData("{{PROJEKT . KUNDE}}", false)]
        [InlineData("{{  projekt.kunde  }}", false)]
        public void Gross_und_Kleinschreibung_und_Leerraum_spielen_keine_Rolle(string text, bool normalform)
        {
            Platzhalter p = Einer(text);
            Assert.Equal(Platzhalterart.Feld, p.Art);
            Assert.Equal("projekt.kunde", p.Schluessel);
            Assert.Equal("{{projekt.kunde}}", p.Normalform);
            Assert.Equal(normalform, p.IstNormalform);
            Assert.True(p.SchluesselGueltig);
        }

        [Theory]
        [InlineData("{{gebäude.größe}}", "gebaeude.groesse")]
        [InlineData("{{GEBÄUDE.NAME}}", "gebaeude.name")]
        [InlineData("{{stand.übergabe}}", "stand.uebergabe")]
        [InlineData("{{hat.kälte}}", "hat.kaelte")]
        [InlineData("{{Straße}}", "strasse")]
        public void Umlaute_werden_gefaltet(string text, string schluessel)
        {
            Platzhalter p = Einer(text);
            Assert.Equal(schluessel, p.Schluessel);
            Assert.True(p.SchluesselGueltig);
            Assert.False(p.IstNormalform);
        }

        [Fact]
        public void Grosses_Eszett_und_zerlegte_Umlaute_werden_gefaltet()
        {
            // ẞ (U+1E9E) wird klein zu ß; „u“ mit kombinierendem Trema (Mac-Zwischenablage, NFD) zu ü.
            Assert.Equal("strasse", Einer("{{STRA" + ((char)0x1E9E) + "E}}").Schluessel);
            Assert.Equal("stand.uebergabe", Einer("{{stand.u" + Trema + "bergabe}}").Schluessel);
        }

        [Fact]
        public void Geschuetzte_und_unsichtbare_Zeichen_zaehlen_als_Leerraum_bzw_fallen_weg()
        {
            Platzhalter p = Einer("{{" + Nbsp + "projekt" + Nullbreite + ".kun" + Trennstrich + "de" + SchmalNbsp + "}}");
            Assert.Equal("projekt.kunde", p.Schluessel);
            Assert.Equal("{{projekt.kunde}}", p.Normalform);
        }

        [Fact]
        public void Normiere_zieht_Leerraum_zusammen_und_kuerzt_die_Raender()
        {
            Assert.Equal("a b", Platzhaltersyntax.Normiere("  A " + Nbsp + "  B  "));
            Assert.Equal("", Platzhaltersyntax.Normiere(null));
            Assert.Equal("", Platzhaltersyntax.Normiere("   "));
            Assert.Equal("ab", Platzhaltersyntax.NormiereSchluessel(" A  B "));
            Assert.Equal("gruesse", Platzhaltersyntax.Normiere("Grüße"));
        }

        [Theory]
        [InlineData("projekt.kunde", true)]
        [InlineData("projekt_kunde", true)]
        [InlineData("stamm.kennzahl.em.co2_spez", true)]
        [InlineData("text.erstellt_mit", true)]
        [InlineData("a1.b2_c3", true)]
        [InlineData("", false)]
        [InlineData("1projekt", false)]
        [InlineData("projekt..kunde", false)]
        [InlineData("projekt.", false)]
        [InlineData(".projekt", false)]
        [InlineData("projekt__kunde", false)]
        [InlineData("projekt_", false)]
        [InlineData("projekt.kunde!", false)]
        [InlineData("Projekt.kunde", false)]
        [InlineData("projekt-kunde", false)]
        public void Schluesselmuster_nach_Konzept_4_5(string schluessel, bool gueltig)
        {
            Assert.Equal(gueltig, Platzhaltersyntax.IstGueltigerSchluessel(schluessel));
        }

        // =====================================================================
        //  Formatangaben
        // =====================================================================

        [Theory]
        [InlineData("stellen 1", Formatangabeart.Stellen, 1, "stellen 1")]
        [InlineData("Stellen  2", Formatangabeart.Stellen, 2, "stellen 2")]
        [InlineData("stellen0", Formatangabeart.Stellen, 0, "stellen 0")]
        [InlineData("stellen 15", Formatangabeart.Stellen, 15, "stellen 15")]
        [InlineData("ohne einheit", Formatangabeart.OhneEinheit, null, "ohne einheit")]
        [InlineData("OhneEinheit", Formatangabeart.OhneEinheit, null, "ohne einheit")]
        [InlineData(" mit   Einheit ", Formatangabeart.MitEinheit, null, "mit einheit")]
        [InlineData("datum", Formatangabeart.Datum, null, "datum")]
        [InlineData("Datum lang", Formatangabeart.DatumLang, null, "datum lang")]
        [InlineData("datum mit zeit", Formatangabeart.DatumMitZeit, null, "datum mit zeit")]
        [InlineData("leer statt strich", Formatangabeart.LeerStattStrich, null, "leer statt strich")]
        [InlineData("mit grund", Formatangabeart.MitGrund, null, "mit grund")]
        [InlineData("block 3", Formatangabeart.Block, 3, "block 3")]
        [InlineData("ohne titel", Formatangabeart.OhneTitel, null, "ohne titel")]
        [InlineData("Ebene 2", Formatangabeart.Ebene, 2, "ebene 2")]
        public void Formatangaben_werden_typisiert(string roh, Formatangabeart art, int? zahl, string normalform)
        {
            Formatangabe a = Platzhaltersyntax.LiesAngabe(roh);
            Assert.Equal(art, a.Art);
            Assert.Equal(zahl, a.Zahl);
            Assert.Equal(normalform, a.Normalform);
            Assert.Equal(roh, a.Roh);
            Assert.True(a.IstBekannt);
        }

        [Theory]
        [InlineData("fett")]
        [InlineData("stellen")]
        [InlineData("stellen 16")]
        [InlineData("stellen -1")]
        [InlineData("stellen eins")]
        [InlineData("block 0")]
        [InlineData("block 100")]
        [InlineData("ebene 0")]
        [InlineData("ebene 10")]
        [InlineData("datum kurz")]
        [InlineData("ohne")]
        public void Unbekannte_Angaben_und_Zahlen_ausserhalb_des_Bereichs_sind_Unbekannt(string roh)
        {
            Formatangabe a = Platzhaltersyntax.LiesAngabe(roh);
            Assert.Equal(Formatangabeart.Unbekannt, a.Art);
            Assert.False(a.IstBekannt);
            Assert.Null(a.Zahl);
            Assert.Equal(roh, a.Roh);
            Assert.Equal(Platzhaltersyntax.Normiere(roh), a.Normalform);
        }

        [Fact]
        public void Mehrere_Angaben_bleiben_in_ihrer_Reihenfolge()
        {
            Platzhalter p = Einer("{{Stamm.Kennzahl.Eff.JAZ | Stellen 1 | ohne Einheit}}");
            Assert.Equal("stamm.kennzahl.eff.jaz", p.Schluessel);
            Assert.Equal(new[] { Formatangabeart.Stellen, Formatangabeart.OhneEinheit }, p.Angaben.Select(a => a.Art));
            Assert.Equal("{{stamm.kennzahl.eff.jaz|stellen 1|ohne einheit}}", p.Normalform);
            Assert.False(p.HatUnbekannteAngabe);
        }

        [Fact]
        public void Eine_unbekannte_Angabe_bleibt_mit_ihrem_Wortlaut_stehen()
        {
            Platzhalter p = Einer("{{projekt.kunde|Fett}}");
            Assert.True(p.HatUnbekannteAngabe);
            Formatangabe a = Assert.Single(p.Angaben);
            Assert.Equal("Fett", a.Roh);
            Assert.Equal("{{projekt.kunde|fett}}", p.Normalform);
        }

        [Fact]
        public void Leere_Angaben_entfallen_die_Normalform_nennt_sie_nicht()
        {
            Platzhalter p = Einer("{{projekt.kunde|}}");
            Assert.Empty(p.Angaben);
            Assert.False(p.IstNormalform);
            Assert.Equal("{{projekt.kunde}}", p.Normalform);

            Assert.Single(Einer("{{projekt.kunde| |stellen 1|}}").Angaben);
        }

        [Theory]
        [InlineData("stellen 1", Vorlagenfeldart.Zahl)]
        [InlineData("ohne einheit", Vorlagenfeldart.Zahl)]
        [InlineData("mit einheit", Vorlagenfeldart.Zahl)]
        [InlineData("mit grund", Vorlagenfeldart.Zahl)]
        [InlineData("datum", Vorlagenfeldart.Datum)]
        [InlineData("datum lang", Vorlagenfeldart.Datum)]
        [InlineData("datum mit zeit", Vorlagenfeldart.Datum)]
        [InlineData("leer statt strich", Vorlagenfeldart.Zahl)]
        [InlineData("leer statt strich", Vorlagenfeldart.Text)]
        [InlineData("leer statt strich", Vorlagenfeldart.Datum)]
        [InlineData("block 3", Vorlagenfeldart.Tabelle)]
        [InlineData("ohne titel", Vorlagenfeldart.Kapitel)]
        [InlineData("ebene 2", Vorlagenfeldart.Kapitel)]
        public void Angaben_gelten_fuer_die_Arten_aus_Konzept_4_8(string roh, Vorlagenfeldart art)
        {
            Formatangabe a = Platzhaltersyntax.LiesAngabe(roh);
            Assert.True(a.PasstZu(art));

            // Und für keine andere Art — außer „leer statt strich“, das drei Arten kennt.
            int passend = System.Enum.GetValues(typeof(Vorlagenfeldart)).Cast<Vorlagenfeldart>().Count(a.PasstZu);
            Assert.Equal(a.Art == Formatangabeart.LeerStattStrich ? 3 : 1, passend);
        }

        [Fact]
        public void Nur_block_gilt_fuer_einen_Wiederholblock()
        {
            Assert.True(Platzhaltersyntax.LiesAngabe("block 3").PasstZuBlock);
            Assert.False(Platzhaltersyntax.LiesAngabe("stellen 1").PasstZuBlock);
            Assert.False(Platzhaltersyntax.LiesAngabe("fett").PasstZu(Vorlagenfeldart.Text));
        }

        [Fact]
        public void Die_Vorschlagsliste_nennt_alle_elf_Angaben_in_Normalform()
        {
            var arten = new HashSet<Formatangabeart>();
            foreach (string muster in Platzhaltersyntax.AngabenMuster)
            {
                // „n“ steht für die Zahl: „stellen n“ → „stellen 2“.
                string beispiel = muster.EndsWith(" n") ? muster.Substring(0, muster.Length - 1) + "2" : muster;
                Formatangabe a = Platzhaltersyntax.LiesAngabe(beispiel);
                Assert.True(a.IstBekannt, muster);
                Assert.Equal(beispiel, a.Normalform);
                arten.Add(a.Art);
            }
            Assert.Equal(11, arten.Count);
            Assert.Equal(System.Enum.GetValues(typeof(Formatangabeart)).Length - 1, arten.Count);
        }

        // =====================================================================
        //  Blockmarken — erkannt, gefüllt erst ab BV-E4
        // =====================================================================

        [Fact]
        public void Wiederholblock_Anfang_und_Ende()
        {
            Platzhalter anfang = Einer("{{#je stand}}");
            Assert.Equal(Platzhalterart.BlockAnfang, anfang.Art);
            Assert.Equal("stand", anfang.Schluessel);
            Assert.True(anfang.IstBlockmarke);
            Assert.True(anfang.IstNormalform);

            Platzhalter ende = Einer("{{/je}}");
            Assert.Equal(Platzhalterart.BlockEnde, ende.Art);
            Assert.Equal("", ende.Schluessel);
            Assert.True(ende.IstBlockmarke);
            Assert.True(ende.IstNormalform);
        }

        [Fact]
        public void Wiederholblock_mit_Angabe_und_Leerraum()
        {
            Platzhalter p = Einer("{{ # JE  Variante | Block 3 }}");
            Assert.Equal(Platzhalterart.BlockAnfang, p.Art);
            Assert.Equal("variante", p.Schluessel);
            Formatangabe a = Assert.Single(p.Angaben);
            Assert.Equal(Formatangabeart.Block, a.Art);
            Assert.Equal(3, a.Zahl);
            Assert.Equal("{{#je variante|block 3}}", p.Normalform);
            Assert.Contains(p.Schluessel, Platzhaltersyntax.JeBereiche);
        }

        [Fact]
        public void Blockende_mit_Nachsatz_behaelt_ihn_als_Schluessel_die_Normalform_nicht()
        {
            Platzhalter p = Einer("{{/JE Stand}}");
            Assert.Equal(Platzhalterart.BlockEnde, p.Art);
            Assert.Equal("stand", p.Schluessel);
            Assert.Equal("{{/je}}", p.Normalform);
            Assert.False(p.IstNormalform);
        }

        [Fact]
        public void Bedingung_und_verneinte_Bedingung()
        {
            Platzhalter wenn = Einer("{{#wenn hat.kaelte}}");
            Assert.Equal(Platzhalterart.WennAnfang, wenn.Art);
            Assert.Equal("hat.kaelte", wenn.Schluessel);
            Assert.False(wenn.Verneint);
            Assert.True(wenn.IstNormalform);

            Platzhalter nicht = Einer("{{#Wenn Nicht hat.Kälte}}");
            Assert.Equal(Platzhalterart.WennAnfang, nicht.Art);
            Assert.Equal("hat.kaelte", nicht.Schluessel);
            Assert.True(nicht.Verneint);
            Assert.Equal("{{#wenn nicht hat.kaelte}}", nicht.Normalform);

            Platzhalter ende = Einer("{{ /wenn }}");
            Assert.Equal(Platzhalterart.WennEnde, ende.Art);
            Assert.Equal("{{/wenn}}", ende.Normalform);
            Assert.True(ende.IstBlockmarke);
        }

        [Fact]
        public void Wenn_nicht_ohne_Schalter_ist_ein_Schalter_namens_nicht()
        {
            Platzhalter p = Einer("{{#wenn nicht}}");
            Assert.Equal(Platzhalterart.WennAnfang, p.Art);
            Assert.False(p.Verneint);
            Assert.Equal("nicht", p.Schluessel);
        }

        [Theory]
        [InlineData("{{#gruppe stand}}")]
        [InlineData("{{#jestand}}")]
        [InlineData("{{/ende}}")]
        [InlineData("{{#}}")]
        [InlineData("{{/}}")]
        [InlineData("{{}}")]
        [InlineData("{{   }}")]
        [InlineData("{{|stellen 1}}")]
        public void Unbekannte_Marken(string text)
        {
            Platzhalter p = Einer(text);
            Assert.Equal(Platzhalterart.Unbekannt, p.Art);
            Assert.False(p.IstBlockmarke);
            Assert.Equal("", p.Schluessel);
            Assert.False(p.SchluesselGueltig);
        }

        [Fact]
        public void Ein_Feld_ist_keine_Blockmarke()
        {
            Assert.False(Einer("{{projekt.kunde}}").IstBlockmarke);
        }

        // =====================================================================
        //  Fundorte
        // =====================================================================

        [Fact]
        public void Positionen_und_Laengen_zeigen_in_den_Text()
        {
            string text = "Kunde: {{projekt.kunde}}, Datum {{ bericht.datum | datum lang }}.{{#je stand}}x{{/je}}";
            List<Platzhalter> alle = Platzhaltersyntax.Finde(text).ToList();

            Assert.Equal(4, alle.Count);
            Assert.Equal(7, alle[0].Position);
            foreach (Platzhalter p in alle)
                Assert.Equal(p.Roh, text.Substring(p.Position, p.Laenge));
            Assert.Equal(new[] { "projekt.kunde", "bericht.datum", "stand", "" }, alle.Select(p => p.Schluessel));
            Assert.Equal(new[] { Platzhalterart.Feld, Platzhalterart.Feld, Platzhalterart.BlockAnfang, Platzhalterart.BlockEnde },
                         alle.Select(p => p.Art));
            Assert.Equal(Formatangabeart.DatumLang, alle[1].Angaben.Single().Art);
        }

        [Fact]
        public void Aneinanderstossende_Platzhalter()
        {
            List<Platzhalter> alle = Platzhaltersyntax.Finde("{{a}}{{b.c}}").ToList();
            Assert.Equal(new[] { 0, 5 }, alle.Select(p => p.Position));
            Assert.Equal(new[] { "a", "b.c" }, alle.Select(p => p.Schluessel));
        }

        [Fact]
        public void Dreifache_Klammern_finden_den_inneren_Platzhalter_und_melden_die_offene()
        {
            string text = "{{{projekt.kunde}}}";
            Platzhalter p = Einer(text);
            Assert.Equal(1, p.Position);
            Assert.Equal("{{projekt.kunde}}", p.Roh);
            Assert.Equal(new[] { 0 }, Platzhaltersyntax.OffeneKlammern(text));
        }

        // =====================================================================
        //  Fehlfälle
        // =====================================================================

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Kein Platzhalter")]
        [InlineData("{projekt.kunde}")]
        [InlineData("{{projekt.kunde}")]
        [InlineData("{{projekt.kunde")]
        [InlineData("{{projekt.\nkunde}}")]
        [InlineData("{{projekt.\r\nkunde}}")]
        [InlineData("{{projekt{kunde}}")]
        public void Kein_Platzhalter(string text)
        {
            Assert.Empty(Platzhaltersyntax.Finde(text));
            Assert.False(Platzhaltersyntax.EnthaeltPlatzhalter(text));
        }

        [Fact]
        public void Offene_Klammern_werden_gemeldet()
        {
            Assert.Equal(new[] { 0 }, Platzhaltersyntax.OffeneKlammern("{{projekt.kunde}"));
            Assert.Equal(new[] { 6 }, Platzhaltersyntax.OffeneKlammern("{{a}} {{b"));
            Assert.Equal(new[] { 0 }, Platzhaltersyntax.OffeneKlammern("{{projekt.\nkunde}}"));
            Assert.Empty(Platzhaltersyntax.OffeneKlammern("{{a}} und {{b}}"));
            Assert.Empty(Platzhaltersyntax.OffeneKlammern(null));
            Assert.Empty(Platzhaltersyntax.OffeneKlammern("{ { a } }"));
        }

        [Fact]
        public void Ein_Schluessel_mit_fremdem_Zeichen_ist_ein_Feld_mit_ungueltigem_Schluessel()
        {
            Platzhalter p = Einer("{{projekt.kunde!}}");
            Assert.Equal(Platzhalterart.Feld, p.Art);
            Assert.Equal("projekt.kunde!", p.Schluessel);
            Assert.False(p.SchluesselGueltig);
        }

        [Fact]
        public void EnthaeltPlatzhalter_erkennt_jede_Marke()
        {
            Assert.True(Platzhaltersyntax.EnthaeltPlatzhalter("Seite {{text.seite}}"));
            Assert.True(Platzhaltersyntax.EnthaeltPlatzhalter("{{/je}}"));
            Assert.True(Platzhaltersyntax.EnthaeltPlatzhalter("{{}}"));
        }

        // =====================================================================
        //  Lies — ein einzelner Platzhalter (Tag, Alternativtext)
        // =====================================================================

        [Fact]
        public void Lies_nimmt_Platzhalter_mit_und_ohne_Klammern()
        {
            Platzhalter mit = Platzhaltersyntax.Lies("{{Projekt.Kunde|stellen 1}}");
            Assert.Equal(Platzhalterart.Feld, mit.Art);
            Assert.Equal("projekt.kunde", mit.Schluessel);
            Assert.Equal(-1, mit.Position);
            Assert.Equal("{{projekt.kunde|stellen 1}}", mit.Normalform);

            Platzhalter ohne = Platzhaltersyntax.Lies(" projekt.kunde ");
            Assert.Equal(Platzhalterart.Feld, ohne.Art);
            Assert.Equal("projekt.kunde", ohne.Schluessel);
            Assert.Equal(" projekt.kunde ", ohne.Roh);
            Assert.Equal("{{projekt.kunde}}", ohne.Normalform);

            Assert.Equal(Platzhalterart.BlockAnfang, Platzhaltersyntax.Lies("#je stand").Art);
            Assert.Equal(Platzhalterart.BlockEnde, Platzhaltersyntax.Lies("{{/je}}").Art);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{{}}")]
        public void Lies_ohne_Inhalt_ist_Unbekannt(string roh)
        {
            Platzhalter p = Platzhaltersyntax.Lies(roh);
            Assert.Equal(Platzhalterart.Unbekannt, p.Art);
            Assert.Equal("{{}}", p.Normalform);
        }

        [Fact]
        public void Lies_und_Finde_sind_sich_einig()
        {
            const string text = "{{ Bericht.Varianten.Liste | Leer statt Strich }}";
            Platzhalter gefunden = Einer(text);
            Platzhalter gelesen = Platzhaltersyntax.Lies(text);
            Assert.Equal(gefunden.Art, gelesen.Art);
            Assert.Equal(gefunden.Schluessel, gelesen.Schluessel);
            Assert.Equal(gefunden.Normalform, gelesen.Normalform);
            Assert.Equal(0, gefunden.Position);
            Assert.Equal(-1, gelesen.Position);
        }
    }
}
