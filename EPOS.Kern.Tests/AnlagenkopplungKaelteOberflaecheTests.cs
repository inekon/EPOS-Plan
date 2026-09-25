using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kälteseite der Anlagenkopplung an der Oberfläche, im Bericht und im Vergleich</b>
    /// (E37; Anlagenkopplung 8.1, 9.1, 9.4, 10.5) — gespiegelt zu
    /// <see cref="AnlagenkopplungOberflaecheTests"/> und <see cref="AnlagenkopplungBerichtTests"/>:
    /// Der Katalogeditor bildet die acht Felder der Kühlübergabe NULL-erhaltend ab, die Texte kommen
    /// aus den Ressourcen, der Herleitungsweg der Hülle trägt die Kälteseite samt Wirksamkeit im
    /// Projekt, der Bedarfsdialog zeigt Kacheln und Bild mit den Zahlen des Laufs, der Bericht den
    /// Abschnitt „Kältekreis und Kühlübergabe" und der Variantenvergleich Schalter und Art.
    ///
    /// <para>Die Fälle mit Datenbank koppeln an einer Arbeitskopie das Gebäude 10599 des Projekts
    /// 1017 (VDI-Weg, Kühlung wirksam, Wärmepumpe im Kühlbetrieb mit 18 °C).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungKaelteOberflaecheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1017, GEBAEUDE = 10599;

        // =============================================================================
        //  Gebäudedialog: die Abbildung des Katalogeditors
        // =============================================================================

        /// <summary>
        /// Katalogsatz → Feldsatz → Katalogsatz: Die acht Felder reisen vollständig, und
        /// <c>NachModell</c> auf einen LEEREN Vorgängersatz („Speichern unter") verliert keins.
        /// </summary>
        [Fact]
        public void Der_Katalogeditor_bildet_die_acht_Felder_der_Kuehluebergabe_ab()
        {
            var satz = new GebaeudeModel
            {
                Gebaeudename = "Kälteprobe",
                Kuehluebergabe_Aktiv = true,
                Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG,
                Kuehl_Uebergabe_Exponent = 1.05,
                Kuehl_Uebergabe_Leistung_Nenn = 4.5,
                Kuehl_Auslegung_Vorlauf = 17,
                Kuehl_Auslegung_Ruecklauf = 20,
                Kuehl_Auslegung_Raumtemperatur = 25,
                Kuehl_Vorlaufgrenze = 18
            };

            GebaeudeKatalogDaten d = GebaeudeKatalogHuelle.AusModell(satz);
            Assert.True(d.KuehluebergabeAktiv);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG, d.KuehlUebergabeArt);
            Assert.Equal(1.05, d.KuehlUebergabeExponent);
            Assert.Equal(4.5, d.KuehlUebergabeLeistungNennKw);
            Assert.Equal(17.0, d.KuehlAuslegungVorlauf);
            Assert.Equal(20.0, d.KuehlAuslegungRuecklauf);
            Assert.Equal(25.0, d.KuehlAuslegungRaumtemperatur);
            Assert.Equal(18.0, d.KuehlVorlaufgrenze);

            GebaeudeModel neu = GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel());
            Assert.True(neu.Kuehluebergabe_Aktiv);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG, neu.Kuehl_Uebergabe_Art);
            Assert.Equal(1.05, neu.Kuehl_Uebergabe_Exponent);
            Assert.Equal(4.5, neu.Kuehl_Uebergabe_Leistung_Nenn);
            Assert.Equal(17.0, neu.Kuehl_Auslegung_Vorlauf);
            Assert.Equal(20.0, neu.Kuehl_Auslegung_Ruecklauf);
            Assert.Equal(25.0, neu.Kuehl_Auslegung_Raumtemperatur);
            Assert.Equal(18.0, neu.Kuehl_Vorlaufgrenze);

            // Leer bleibt leer - nicht 0, nicht die Vorgabe.
            GebaeudeModel leer = GebaeudeKatalogHuelle.NachModell(GebaeudeKatalogHuelle.AusModell(new GebaeudeModel()), satz);
            Assert.False(leer.Kuehluebergabe_Aktiv);
            Assert.Null(leer.Kuehl_Uebergabe_Art);
            Assert.Null(leer.Kuehl_Uebergabe_Exponent);
            Assert.Null(leer.Kuehl_Uebergabe_Leistung_Nenn);
            Assert.Null(leer.Kuehl_Auslegung_Vorlauf);
            Assert.Null(leer.Kuehl_Auslegung_Ruecklauf);
            Assert.Null(leer.Kuehl_Auslegung_Raumtemperatur);
            Assert.Null(leer.Kuehl_Vorlaufgrenze);
        }

        /// <summary>Die Texte des Unterabschnitts kommen aus den Ressourcen — in beiden Sprachen (N-A6).</summary>
        [Fact]
        public void Die_Texte_der_Kuehluebergabe_kommen_aus_den_Ressourcen()
        {
            KuehluebergabeTexte k = GebaeudeKatalogHuelle.KuehluebergabeTexte();
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_UABS_KUEHLUEBERGABE, k.Unterabschnitt);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_LBL_KUEHLUEBERGABE_AKTIV, k.LabelAktiv);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_ZEILE_KUEHL_GRENZE, k.ZeileGrenze);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_ZEILE_KUEHL_SENSIBEL, k.ZeileSensibel);
            Assert.Contains("{2}", k.MeldungReihenfolge);
            Assert.Equal(GebaeudeKatalogHuelle.Texte().Kuehluebergabe.LabelArt, k.LabelArt);

            // Die Anzeigenamen der drei Arten stehen EINMAL - Dialog und Kern sagen dasselbe.
            foreach (string art in Waermeuebergabevorgaben.KuehlArten)
                Assert.Equal(Waermeuebergabevorgaben.KuehlAnzeigename(art), k.Artname(art));
            Assert.Equal(k.ArtIdeal, Waermeuebergabevorgaben.KuehlAnzeigename(null));

            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                KuehluebergabeTexte en = GebaeudeKatalogHuelle.KuehluebergabeTexte();
                Assert.NotEqual("Kühlübergabe", en.Unterabschnitt);
                Assert.DoesNotContain("ü", en.LabelAktiv);
                Assert.DoesNotContain("ä", en.ArtKuehldecke);
            }
            finally
            {
                CultureInfo.CurrentUICulture = vorher;
            }
        }

        /// <summary>Die Vorgaben der Arten, wie der Dialog sie als Zahl zeigt (A4) — dieselben wie im Eingangsbauer.</summary>
        [Fact]
        public void Die_Vorgaben_der_Kuehluebergabearten()
        {
            Assert.False(Waermeuebergabevorgaben.KuehlArtRechnet(null));
            Assert.False(Waermeuebergabevorgaben.KuehlArtRechnet(DbWerte.KUEHLUEBERGABE_IDEAL));
            Assert.Equal(1.1, Waermeuebergabevorgaben.KuehlExponent(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.Equal(16.0, Waermeuebergabevorgaben.KuehlVorlauf(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG));
            Assert.Equal(19.0, Waermeuebergabevorgaben.KuehlRuecklauf(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.Equal(7.0, Waermeuebergabevorgaben.KuehlVorlauf(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.Equal(16.0, Waermeuebergabevorgaben.KuehlVorlaufgrenze(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.Null(Waermeuebergabevorgaben.KuehlVorlaufgrenze(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.Null(Waermeuebergabevorgaben.KuehlExponent(DbWerte.KUEHLUEBERGABE_IDEAL));
            Assert.Equal("XY", Waermeuebergabevorgaben.KuehlAnzeigename("XY"));
        }

        // =============================================================================
        //  Mit Datenbank: Herleitungsweg der Hülle und Bedarfsdialog
        // =============================================================================

        private static void Kuehlkoppeln()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehluebergabe_Aktiv = 1, Kuehl_Uebergabe_Art = ? WHERE ID = ?",
                new DbParam("@art", DbWerte.KUEHLUEBERGABE_KUEHLDECKE), new DbParam("@id", GEBAEUDE)));
        }

        private static GebaeudeKatalogDaten Projektsatz()
        {
            List<Z_ProjGebModel> zuordnungen = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            ProjektGebaeudeModel pg = GebaeudeBedarfCtrl.Projektgebaeude(PROJEKT, zuordnungen[0].ID_Z);
            var satz = new GebaeudeModel();
            foreach (System.Reflection.FieldInfo f in typeof(GebaeudeModel).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                System.Reflection.FieldInfo q = typeof(ProjektGebaeudeModel).GetField(f.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (q != null && q.FieldType == f.FieldType) f.SetValue(satz, q.GetValue(pg));
            }
            return GebaeudeKatalogHuelle.AusModell(satz);
        }

        /// <summary>
        /// Der Herleitungsweg der Hülle trägt die Kälteseite (Auslegungstag, Kaltwasser-Vorlauf der
        /// Wärmepumpe im Kühlbetrieb) und die Wirksamkeit im Projekt: ohne Kopplungsstufe „koppelt
        /// nicht", mit AK1 wirksam. Ohne Kühlübergabeart gibt es keine Kälteseite.
        /// </summary>
        [Fact]
        public void Der_Herleitungsweg_traegt_die_Kaelteseite_und_ihre_Wirksamkeit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GebaeudeKatalogDaten d = Projektsatz();
            d.KuehluebergabeAktiv = true;
            d.KuehlUebergabeArt = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;

            UebergabeHerleitungDaten ohneStufe = GebaeudeKatalogHuelle.Herleitungsweg(PROJEKT)(d);
            Assert.NotNull(ohneStufe?.Kuehlung);
            Assert.False(ohneStufe.Kuehlung.ProjektKoppelt);
            Assert.True(ohneStufe.Kuehlung.ProjektKuehlt);
            Assert.Null(ohneStufe.AuslegungsheizlastKw);      // keine Wärmeübergabeart - keine Heizseite

            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            UebergabeHerleitungDaten h = GebaeudeKatalogHuelle.Herleitungsweg(PROJEKT)(d);
            KuehluebergabeHerleitungDaten k = h.Kuehlung!;
            Assert.True(k.ProjektKoppelt);
            Assert.True(string.IsNullOrEmpty(k.Befund), k.Befund);
            Assert.True(k.AuslegungskuehllastKw > 0);
            Assert.False(string.IsNullOrEmpty(k.AuslegungstagText));
            Assert.True(k.VorlaufAusAnlage);
            Assert.Equal(18.0, k.VorlaufC);
            Assert.Equal(16.0, k.VorlaufgrenzeC);

            d.KuehlUebergabeArt = null;
            Assert.Null(GebaeudeKatalogHuelle.Herleitungsweg(PROJEKT)(d));
        }

        private static GebaeudeProjektZeile Zeile()
        {
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> liste = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            return ((List<GebaeudeProjektZeile>)liste["Zeilen"])[0];
        }

        /// <summary>
        /// Ohne Kälteseite: keine Kacheln, kein Bild, kein Ausweis. Kühlgekoppelt: der Ausweis
        /// „…, gekoppelt (AK1)", die Zeile mit Art, Auslegungspunkt und „sensibel", das Bild mit Lücken
        /// in den Stunden ohne Kühlbetrieb — und DIESELBEN Zahlen, die der Lauf in
        /// <c>Tab_ErgebnisGebaeude</c> legt.
        /// </summary>
        [Fact]
        public void Der_Bedarfsdialog_zeigt_den_Kaeltekreis_mit_den_Zahlen_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> ohne = GebaeudeBedarfHuelle.Gaben(Zeile(), PROJEKT);
            var d0 = (GebaeudeBedarfDaten)ohne["Daten"];
            Assert.False(d0.IstKuehlgekoppelt);
            Assert.Null(d0.KuehlVorlaufMittelC);
            Assert.Equal("", d0.Kuehlkreiszeile);
            Assert.Null(ohne["BildauftragKuehlvorlauf"]);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_GEKOPPELT, d0.Modelltext);

            Kuehlkoppeln();
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            IReadOnlyDictionary<string, object> mit = GebaeudeBedarfHuelle.Gaben(Zeile(), PROJEKT);
            var d = (GebaeudeBedarfDaten)mit["Daten"];
            Assert.True(d.IstKuehlgekoppelt);
            Assert.False(d.IstGekoppelt);
            Assert.EndsWith(", " + WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_GEKOPPELT, d.Modelltext);
            Assert.Contains("Kühldecke", d.Kuehlkreiszeile);
            Assert.Contains("16/19", d.Kuehlkreiszeile);
            Assert.Contains("sensibel", d.Kuehlkreiszeile);
            Assert.Contains("Vorlaufgrenze", d.KuehlBegrenztzeile);

            var lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);
            ErgebnisGebaeudeModel g = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Gebaeude);
            Assert.True(g.IstKuehlgekoppelt);
            Assert.Equal(g.KuehlVorlaufMittelC, d.KuehlVorlaufMittelC);
            Assert.Equal(g.KuehlRuecklaufMittelC, d.KuehlRuecklaufMittelC);
            Assert.Equal(g.KuehlUebergabeBegrenztStundenH, d.KuehlUebergabeBegrenztStundenH);

            var bild = (Func<Zeichenmodell>)mit["BildauftragKuehlvorlauf"];
            Zeichenmodell m = bild();
            Assert.Equal(4, m.Reihen.Count);
            Assert.Contains(m.Reihen[0].Werte, double.IsNaN);
            Assert.Contains(m.Reihen[0].Werte, w => w == 18.0);
            Assert.Contains(m.Reihen[1].Werte, w => w > 18.0);
        }

        // =============================================================================
        //  Bericht (9.4) und Variantenvergleich
        // =============================================================================

        private static ErgebnisGebaeudeModel Ergebniszeile(int id, string name, string kuehlArt)
            => new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = id, Merkplatz = id, Gebaeudename = name,
                Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007, HeizwaermeMwh = 50.0,
                SpitzeKw = 20.0, SpitzeTagesmittelKw = 15.0, Spitze95Kw = 12.0,
                KuehlenergieMwh = 2.4, KuehlstundenH = 500, MittlereRaumtemperaturC = 21.0, UeberhitzungsstundenH = 330,
                KuehlUebergabeArt = kuehlArt,
                KuehlVorlaufMittelC = kuehlArt != null ? 18.0 : null,
                KuehlRuecklaufMittelC = kuehlArt != null ? 18.81 : null,
                KuehlUebergabeBegrenztStundenH = kuehlArt != null ? 28.9 : null,
                KuehlVorlaufgrenzeStundenH = kuehlArt != null ? 28.9 : null
            };

        private static BerichtsDaten Berichtsdaten(params ErgebnisGebaeudeModel[] gebaeude)
        {
            var eingaben = new DataTable();
            eingaben.Columns.Add("ID", typeof(long));
            eingaben.Columns.Add("Kuehl_Auslegung_Vorlauf", typeof(double));
            eingaben.Columns.Add("Kuehl_Vorlaufgrenze", typeof(double));
            foreach (ErgebnisGebaeudeModel g in gebaeude) eingaben.Rows.Add((long)g.ID_Gebaeude, DBNull.Value, DBNull.Value);

            var erg = new ErgebnisModel();
            erg.Gebaeude.AddRange(gebaeude);
            var daten = new BerichtsDaten { Stammprojektname = "Probe" };
            daten.Varianten.Add(new VariantenDaten
            {
                IstStamm = true, Projektname = "Probe", Ergebnis = erg,
                Details = new ProjektDetails { IdProjekt = 1, Gebaeude = eingaben }
            });
            return daten;
        }

        private static string Schreibe(IBerichtsBaustein baustein, BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            baustein.SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        }

        /// <summary>
        /// Der Bericht führt den Kältekreis NUR für das kühlgekoppelte Gebäude — Art mit
        /// Auslegungspunkt (Vorgabe der Kühldecke 16/19 °C), die Grenze, die beiden Mittel, die
        /// begrenzten Stunden samt Anteil an der Vorlaufgrenze —, trennt sie von den
        /// Überhitzungsstunden, nennt die Grenze der Zahl (K5), den Ausweis am Rechenweg und den
        /// Satz am Produktausweis.
        /// </summary>
        [Fact]
        public void Der_Bericht_zeigt_den_Kaeltekreis_nur_fuer_kuehlgekoppelte_Gebaeude()
        {
            BerichtsDaten daten = Berichtsdaten(Ergebniszeile(1, "Haus A", DbWerte.KUEHLUEBERGABE_KUEHLDECKE),
                                                Ergebniszeile(2, "Haus B", null));

            string text = Schreibe(new ProjektbeschreibungBaustein(), daten);
            Assert.Contains(ProjektbeschreibungBaustein.UEBERSCHRIFT_KUEHLKREIS, text);
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_HEIZKREIS, text);
            Assert.Contains("Kühldecke, 16/19 °C", text);
            Assert.Contains("18,8", text);
            Assert.Contains("29 (davon an der Vorlaufgrenze 29)", text);
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_KUEHLKREIS, text);
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_KUEHLKREIS_GRENZE, text);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG, text);

            string gekoppelt = WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_VDI6007 + ", "
                               + WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_GEKOPPELT;
            Assert.Equal(gekoppelt, ProjektbeschreibungBaustein.Rechenwegtext(daten.Varianten[0].Ergebnis.Gebaeude[0]));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_VDI6007,
                         ProjektbeschreibungBaustein.Rechenwegtext(daten.Varianten[0].Ergebnis.Gebaeude[1]));

            int abschnitt = text.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_KUEHLKREIS, StringComparison.Ordinal);
            Assert.DoesNotContain("Haus B", text.Substring(abschnitt));

            Assert.True(DeckblattBaustein.KopplungImBericht(daten));
            Assert.Contains("Kühlübergabe", WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_ANLAGENKOPPLUNG);
        }

        /// <summary>Der Gebläsekonvektor hat keine Grenze; eine Eingabe schlägt die Vorgabe.</summary>
        [Fact]
        public void Der_Bericht_nennt_eine_fehlende_Grenze_und_die_Eingabe_statt_der_Vorgabe()
        {
            BerichtsDaten daten = Berichtsdaten(Ergebniszeile(1, "Haus A", DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            daten.Varianten[0].Details.Gebaeude.Rows[0]["Kuehl_Auslegung_Vorlauf"] = 8.0;

            string text = Schreibe(new ProjektbeschreibungBaustein(), daten);
            Assert.Contains("Gebläsekonvektor, 8/12 °C", text);
            Assert.Contains("\nkeine\n", text);
        }

        private static ProjektDetails Details(bool schalter, string art)
        {
            var geb = new DataTable();
            geb.Columns.Add("ID", typeof(long));
            geb.Columns.Add("Kuehluebergabe_Aktiv", typeof(long));
            geb.Columns.Add("Kuehl_Uebergabe_Art", typeof(string));
            geb.Rows.Add(10L, schalter ? 1L : 0L, art != null ? art : (object)DBNull.Value);
            return new ProjektDetails { IdProjekt = 1, Gebaeude = geb };
        }

        /// <summary>
        /// Der Variantenvergleich nennt Schalter und Kühlübergabeart mit Anzeigenamen; NULL und
        /// „IDEAL" sind dasselbe und keine Abweichung.
        /// </summary>
        [Fact]
        public void Der_Variantenvergleich_nennt_Schalter_und_Kuehluebergabeart()
        {
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(Details(false, null),
                                                                    Details(true, DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Abweichung schalter = liste.Single(a => a.Gewerk == "Gebäude" && a.Merkmal == "Kühlübergabe rechnen");
            Assert.Equal("Nein", schalter.WertStamm);
            Assert.Equal("Ja", schalter.WertVariante);
            Abweichung art = liste.Single(a => a.Gewerk == "Gebäude" && a.Merkmal == "Kühlübergabeart");
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_KUEHLUEBERGABE_IDEAL, art.WertStamm);
            Assert.Equal("Kühldecke", art.WertVariante);

            Assert.Empty(AbweichungsErmittler.Vergleiche(Details(false, null), Details(false, DbWerte.KUEHLUEBERGABE_IDEAL)));
        }
    }
}
