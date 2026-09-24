using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Anlagenkopplung im Bericht und im Variantenvergleich, Stufe AK1 Welle 3</b> (Konzept
    /// Anlagenkopplung 9.4): der Abschnitt „Heizkreis und Übergabe" je gekoppeltem Gebäude — nur
    /// dann —, der Ausweis „gekoppelt (AK1)" am Rechenweg, der eine Satz am Produktausweis nach E10
    /// (B-A3) und Übergabeart und Kopplungsstufe im <c>AbweichungsErmittler</c>, gezeigt und
    /// verglichen über den Anzeigenamen (NULL heißt „aus" bzw. „ideal").
    ///
    /// <para>Ohne Datenbank: Die Berichtsdaten entstehen im Speicher, wie <c>ErgebnisCtrl.Load</c>
    /// sie liefern würde; die Kultur ist auf de-DE gepinnt.</para>
    /// </summary>
    public sealed class AnlagenkopplungBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        // Bericht (9.4)
        // =================================================================

        private static ErgebnisGebaeudeModel Zeile(int id, string name, string art)
            => new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = id, Merkplatz = id, Gebaeudename = name,
                Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007, HeizwaermeMwh = 70.67,
                SpitzeKw = 34.5, SpitzeTagesmittelKw = 25.0, Spitze95Kw = 20.0,
                MittlereRaumtemperaturC = 20.0, UeberhitzungsstundenH = 5,
                UebergabeArt = art,
                VorlaufMittelC = art != null ? 35.3 : null,
                RuecklaufMittelC = art != null ? 32.06 : null,
                UebergabeBegrenztStundenH = art != null ? 210.9 : null
            };

        /// <summary>Die Eingaben der Gebäude, wie <c>ProjektDetails</c> sie aus <c>Tab_Gebaeude</c> liest.</summary>
        private static DataTable Gebaeudeeingaben(params (int Id, bool Kurve, double? Vorlauf)[] zeilen)
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(long));
            dt.Columns.Add("Heizkurve_Aktiv", typeof(long));
            dt.Columns.Add("Heizkurve_Niveau", typeof(double));
            dt.Columns.Add("Heizkurve_Steilheit", typeof(double));
            dt.Columns.Add("Auslegung_Vorlauf", typeof(double));
            dt.Columns.Add("Auslegung_Ruecklauf", typeof(double));
            foreach ((int id, bool kurve, double? vorlauf) in zeilen)
                dt.Rows.Add((long)id, kurve ? 1L : 0L, DBNull.Value, 1.2,
                            vorlauf.HasValue ? vorlauf.Value : (object)DBNull.Value, DBNull.Value);
            return dt;
        }

        private static BerichtsDaten Daten(DataTable eingaben, params ErgebnisGebaeudeModel[] gebaeude)
        {
            var erg = new ErgebnisModel();
            erg.Gebaeude.AddRange(gebaeude);
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(new VariantenDaten
            {
                IstStamm = true, Projektname = "Probe", Ergebnis = erg,
                Details = new ProjektDetails { IdProjekt = 1, Gebaeude = eingaben }
            });
            return d;
        }

        /// <summary>Schreibt einen Baustein in ein Dokument im Speicher und liefert dessen Text.</summary>
        private static string Schreibe(IBerichtsBaustein baustein, BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            baustein.SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<Text>().Select(t => t.Text));
        }

        /// <summary>
        /// Ein gekoppeltes und ein ungekoppeltes Gebäude: Der Abschnitt „Heizkreis und Übergabe"
        /// führt NUR das gekoppelte — Übergabeart mit Auslegungspunkt (hier die Vorgabe des
        /// Radiators, 55/45 °C, beziehungsweise die Eingabe 50 °C), die Heizkurve mit den Eingaben
        /// (leeres Niveau = Vorgabe 0 K), die beiden Mittel und die begrenzten Stunden; der Rechenweg
        /// trägt den Ausweis, und unter der Tabelle steht der Satz zur EPOS-Erweiterung.
        /// </summary>
        [Fact]
        public void Der_Bericht_zeigt_den_Heizkreis_nur_fuer_gekoppelte_Gebaeude()
        {
            BerichtsDaten daten = Daten(Gebaeudeeingaben((1, true, null), (2, false, null)),
                                        Zeile(1, "Haus A", DbWerte.UEBERGABE_RADIATOR),
                                        Zeile(2, "Haus B", null));

            string text = Schreibe(new ProjektbeschreibungBaustein(), daten);
            Assert.Contains(ProjektbeschreibungBaustein.UEBERSCHRIFT_HEIZKREIS, text);
            Assert.Contains("Radiator, 55/45 °C", text);
            Assert.Contains("Niveau 0,0 K, Steilheit 1,20", text);
            Assert.Contains("35,3", text);
            Assert.Contains("32,1", text);
            Assert.Contains("211", text);
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_HEIZKREIS, text);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG, text);

            // Der Ausweis am Rechenweg - gekoppelt nur bei Haus A.
            string gekoppelt = WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_VDI6007 + ", "
                               + WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_GEKOPPELT;
            Assert.Single(text.Split('\n'), z => z == gekoppelt);
            Assert.Equal(gekoppelt, ProjektbeschreibungBaustein.Rechenwegtext(daten.Varianten[0].Ergebnis.Gebaeude[0]));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_VDI6007,
                         ProjektbeschreibungBaustein.Rechenwegtext(daten.Varianten[0].Ergebnis.Gebaeude[1]));

            // Haus B (ungekoppelt) steht nicht in der Tabelle des Heizkreises.
            int abschnitt = text.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_HEIZKREIS, StringComparison.Ordinal);
            Assert.DoesNotContain("Haus B", text.Substring(abschnitt));
        }

        /// <summary>Ohne Heizkurve steht „fester Vorlauf"; eine Eingabe des Auslegungsvorlaufs steht statt der Vorgabe.</summary>
        [Fact]
        public void Ohne_Heizkurve_steht_der_feste_Vorlauf_und_die_Eingabe_schlaegt_die_Vorgabe()
        {
            BerichtsDaten daten = Daten(Gebaeudeeingaben((1, false, 50.0)), Zeile(1, "Haus A", DbWerte.UEBERGABE_FLAECHE));

            string text = Schreibe(new ProjektbeschreibungBaustein(), daten);
            Assert.Contains("fester Vorlauf", text);
            Assert.Contains("Flächenheizung, 50/28 °C", text);
        }

        /// <summary>Ohne gekoppeltes Gebäude entfällt der Abschnitt, und der Produktausweis bleibt, wie er war.</summary>
        [Fact]
        public void Ohne_Kopplung_entfaellt_der_Abschnitt_und_der_Satz_am_Ausweis()
        {
            BerichtsDaten daten = Daten(Gebaeudeeingaben((1, true, null)), Zeile(1, "Haus A", null));

            string text = Schreibe(new ProjektbeschreibungBaustein(), daten);
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_HEIZKREIS, text);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_GEKOPPELT, text);

            Assert.False(DeckblattBaustein.KopplungImBericht(daten));
            string kopf = Schreibe(new DeckblattBaustein(), daten);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007, kopf);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG, kopf);
        }

        /// <summary>Gekoppelt bekommt der Ausweis nach E10 genau EINEN Satz dazu (B-A3).</summary>
        [Fact]
        public void Gekoppelt_bekommt_der_Produktausweis_einen_Satz()
        {
            BerichtsDaten daten = Daten(Gebaeudeeingaben((1, true, null)), Zeile(1, "Haus A", DbWerte.UEBERGABE_RADIATOR));

            Assert.True(DeckblattBaustein.KopplungImBericht(daten));
            string kopf = Schreibe(new DeckblattBaustein(), daten);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007 + ". "
                            + WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG, kopf);
        }

        // =================================================================
        // Variantenvergleich (9.4): Übergabeart und Kopplungsstufe
        // =================================================================

        private static ProjektDetails Details(string stufe, bool heizkreis, string art)
        {
            var ein = new DataTable();
            ein.Columns.Add("ID", typeof(long));
            ein.Columns.Add("ID_Projekt", typeof(long));
            ein.Columns.Add("Anlagenkopplung", typeof(string));
            ein.Rows.Add(1L, 1L, stufe != null ? stufe : (object)DBNull.Value);

            var geb = new DataTable();
            geb.Columns.Add("ID", typeof(long));
            geb.Columns.Add("Heizkreis_Aktiv", typeof(long));
            geb.Columns.Add("Uebergabe_Art", typeof(string));
            geb.Rows.Add(10L, heizkreis ? 1L : 0L, art != null ? art : (object)DBNull.Value);

            return new ProjektDetails { IdProjekt = 1, Einstellungen = ein, Gebaeude = geb };
        }

        private static Abweichung Finde(List<Abweichung> liste, string merkmal)
            => liste.SingleOrDefault(a => a.Gewerk == "Gebäude" && a.Merkmal == merkmal);

        /// <summary>
        /// Verschiedene Kopplung ist eine benannte Abweichung — mit Anzeigenamen, nicht mit den
        /// Steuerwerten der Datenbank; NULL und „AUS" sind dasselbe und keine Abweichung.
        /// </summary>
        [Fact]
        public void Der_Variantenvergleich_nennt_Kopplungsstufe_Haken_und_Uebergabeart()
        {
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(
                Details(null, false, null),
                Details(DbWerte.ANLAGENKOPPLUNG_AK1, true, DbWerte.UEBERGABE_RADIATOR));

            Abweichung stufe = Finde(liste, "Anlagenkopplung");
            Assert.NotNull(stufe);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_ANLAGENKOPPLUNG_AUS, stufe.WertStamm);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_ANLAGENKOPPLUNG_AK1, stufe.WertVariante);

            Abweichung haken = Finde(liste, "Übergabe rechnen");
            Assert.NotNull(haken);
            Assert.Equal("Nein", haken.WertStamm);
            Assert.Equal("Ja", haken.WertVariante);

            Abweichung art = Finde(liste, "Übergabeart");
            Assert.NotNull(art);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_UEBERGABE_IDEAL, art.WertStamm);
            Assert.Equal("Radiator", art.WertVariante);
        }

        [Fact]
        public void NULL_und_AUS_sind_keine_Abweichung_und_gleiche_Kopplung_auch_nicht()
        {
            Assert.Empty(AbweichungsErmittler.Vergleiche(Details(null, false, null),
                                                         Details(DbWerte.ANLAGENKOPPLUNG_AUS, false, DbWerte.UEBERGABE_IDEAL)));
            Assert.Empty(AbweichungsErmittler.Vergleiche(Details(DbWerte.ANLAGENKOPPLUNG_AK1, true, DbWerte.UEBERGABE_FLAECHE),
                                                         Details(DbWerte.ANLAGENKOPPLUNG_AK1, true, DbWerte.UEBERGABE_FLAECHE)));

            List<Abweichung> art = AbweichungsErmittler.Vergleiche(
                Details(DbWerte.ANLAGENKOPPLUNG_AK1, true, DbWerte.UEBERGABE_RADIATOR),
                Details(DbWerte.ANLAGENKOPPLUNG_AK1, true, DbWerte.UEBERGABE_FLAECHE));
            Abweichung a = Assert.Single(art);
            Assert.Equal("Radiator", a.WertStamm);
            Assert.Equal("Flächenheizung", a.WertVariante);
        }
    }
}
