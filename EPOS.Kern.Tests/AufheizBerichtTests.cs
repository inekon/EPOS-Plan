using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Bericht und Kurzbericht der Aufheizoptimierung</b> (Entwurf KP3, Welle O3a; E58 F4 (b), E60; Festlegungen 30,
    /// 39, 41): die Gebäudetafel in den Zuständen aus, bemessen (mit W2 und Aufschlag), unerreichbar, manuell und gekoppelt,
    /// beide Sprachen; die Kurzform der Konditionierung; die Vorlagenfelder <c>gebaeude.ergebnis.*</c> und
    /// <c>hat.aufheizung</c> (Katalog v16); der Aufheizabsatz des Kurzberichts de/en an der Probe 1030. Beispielwerte sind
    /// runde Phantasiezahlen.
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizBerichtTests : IDisposable
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static string Text(string ressource, CultureInfo k) => R.ResourceManager.GetString(ressource, k);

        /// <summary>Ein bemessenes Gebäude auf dem VDI-Weg: t_auf,max 6 h bei −12 °C, P_auf 60 kW (Ziel), Φ_HL 40 + Φ_RH 10 kW.</summary>
        private static ErgebnisGebaeudeModel Bemessen() => new ErgebnisGebaeudeModel
        {
            ID_Gebaeude = 7, Gebaeudename = "Gebäude 1", Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
            HeizwaermeMwh = 100.0, SpitzeKw = 70.0, SpitzeTagesmittelKw = 30.0, Spitze95Kw = 40.0,
            NachtauskuehlstundenH = 120, SommerlueftungsstundenH = 300,
            AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizBemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE,
            AufheizArt = DbWerte.AUFHEIZ_ART_TAEGLICH, AufheizzeitMaxH = 6, AufheizAussenC = -12.0,
            AufheizLeistungKw = 60.0, AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_ZIEL,
            Aufheiztage = 200, AufheizstundenH = 800, AufheizzeitLaengsteH = 6, AufheiztageBegrenzt = 10,
            AufheiztageUnerreichbar = 0, AufheiztageNachweisband = 0, AufheizspruengeAus = 2, HeizleistungMaxStundenH = 0.0,
            AuslegungsheizlastKw = 40.0, AufheizzuschlagKw = 10.0,
        };

        private static Dictionary<string, string> Paare(ErgebnisGebaeudeModel g, CultureInfo k, (double? H, double? Prozent)? aufschlag = null)
            => Berichtstabellen.Gebaeudeergebnis(g, k.Name.StartsWith("en", StringComparison.Ordinal), k, aufschlag)
               .Zeilen.ToDictionary(z => z.Zellen[0].Text, z => z.Zellen[1].Text);

        [Fact]
        public void Ohne_Aufheizung_bleibt_die_Gebaeudetafel_wie_sie_war()
        {
            ErgebnisGebaeudeModel g = Bemessen();
            g.NachtauskuehlstundenH = null; g.SommerlueftungsstundenH = null;
            g.AufheizZustand = null; g.AufheizArt = null; g.AuslegungsheizlastKw = null; g.AufheizzuschlagKw = null;
            Dictionary<string, string> p = Paare(g, DE, (2.0, 10.0));
            // Rechenweg, Wärmebedarf, drei Spitzen und die vier Kennzahlen des VDI-Wegs — keine weitere Zeile.
            Assert.Equal(9, p.Count);
            Assert.DoesNotContain(Text(nameof(R.BV_AUFH_ZUSTAND), DE), p.Keys);
            Assert.Empty(Aufheizbericht.Hinweise(g, DE));
            Assert.Null(Aufheizbericht.Auslegungsgroesse(g));
        }

        [Fact]
        public void Bemessen_zeigt_Zeilen_Auslegungsgroesse_Aufschlag_und_Hinweise()
        {
            ErgebnisGebaeudeModel g = Bemessen();
            Dictionary<string, string> p = Paare(g, DE, (2.0, 10.0));
            Assert.Equal("120 h/a", p["Nachtauskühlstunden"]);
            Assert.Equal("300 h/a", p["Sommerlüftungsstunden"]);
            Assert.Equal("bemessen", p["Aufheizoptimierung"]);
            Assert.Equal("täglich", p["Art der Aufheizzeit"]);
            Assert.Equal("6 h bei -12,0 °C (kälteste Stunde)", p["Längste Aufheizzeit t_auf,max"]);
            Assert.Equal("60,0 kW (Zielleistung)", p["Aufheizleistung P_auf"]);
            Assert.Equal("200", p["Rampentage"]);
            Assert.Equal("10", p["Tage durch die Absenkdauer begrenzt (W2)"]);
            Assert.Equal("2 h / 10 % (es gilt der größere Wert)", p["Aufschlag (Projekteinstellung)"]);
            Assert.Equal("50,0 kW", p["Auslegungsgröße Φ_HL + Φ_RH"]);
            Assert.Equal("40,0 kW", p["davon Auslegungsheizlast Φ_HL"]);
            Assert.Equal("10,0 kW", p["davon Aufheizzuschlag Φ_RH"]);
            Assert.False(p.ContainsKey("Aufheizzeit manuell"));

            // W2 (begrenzt) und W4 nennen ihre Zahl; W1, W3 und W5 haben keinen Anlass.
            List<string> h = Aufheizbericht.Hinweise(g, DE);
            Assert.Equal(new[] { string.Format(DE, Text(nameof(R.GEBB_AUFH_W2), DE), 10), string.Format(DE, Text(nameof(R.GEBB_AUFH_W4), DE), 2) }, h);
        }

        [Fact]
        public void Unerreichbar_nennt_W1_und_keine_Aufheizzeit()
        {
            ErgebnisGebaeudeModel g = Bemessen();
            g.AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR; g.AufheizzeitMaxH = null; g.AufheiztageUnerreichbar = 15;
            g.AufheiztageBegrenzt = 0; g.AufheizspruengeAus = 0;
            Dictionary<string, string> p = Paare(g, DE);
            Assert.Equal(Text(nameof(R.GEBB_AUFH_ZUSTAND_UNERREICHBAR), DE), p["Aufheizoptimierung"]);
            Assert.False(p.ContainsKey("Längste Aufheizzeit t_auf,max"));
            Assert.Equal("15", p["Tage ohne erreichbare Rampe (W1)"]);
            Assert.Equal(new[] { Text(nameof(R.GEBB_AUFH_W1_BEMESSUNG), DE), string.Format(DE, Text(nameof(R.GEBB_AUFH_W1), DE), 15) },
                         Aufheizbericht.Hinweise(g, DE));
        }

        [Fact]
        public void Manuell_zeigt_den_Wert_ohne_Aufschlag()
        {
            ErgebnisGebaeudeModel g = Bemessen();
            g.AufheizArt = DbWerte.AUFHEIZ_ART_MANUELL; g.AufheizzeitMaxH = 8;
            Dictionary<string, string> p = Paare(g, DE, (2.0, 10.0));
            Assert.Equal("manuell (8 h)", p["Art der Aufheizzeit"]);
            Assert.Equal("8 h", p["Aufheizzeit manuell"]);
            Assert.False(p.ContainsKey("Längste Aufheizzeit t_auf,max"));
            Assert.False(p.ContainsKey("Aufschlag (Projekteinstellung)"));
        }

        [Fact]
        public void Gekoppelt_nennt_W5_und_traegt_die_Auslegungsheizlast_allein()
        {
            var g = new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = 7, Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007, UebergabeArt = "RADIATOR",
                AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, HeizleistungMaxStundenH = 12.5, AuslegungsheizlastKw = 40.0,
            };
            Dictionary<string, string> p = Paare(g, DE);
            Assert.Equal("12,5 h/a", p["Kappungsstunden an der Heizleistungsgrenze"]);
            Assert.Equal("40,0 kW", p["Auslegungsgröße Φ_HL + Φ_RH"]);
            Assert.Equal(Tabellenzelle.STRICH, p["davon Aufheizzuschlag Φ_RH"]);
            Assert.False(p.ContainsKey("Rampentage"));
            Assert.Equal(new[] { Text(nameof(R.GEBB_AUFH_W5), DE) }, Aufheizbericht.Hinweise(g, DE));
        }

        [Fact]
        public void Die_Gebaeudetafel_spricht_englisch()
        {
            Dictionary<string, string> p = Paare(Bemessen(), EN, (2.0, null));
            Assert.Equal(Text(nameof(R.GEBB_AUFH_ZUSTAND_BEMESSEN), EN), p["Preheat optimisation"]);
            Assert.Equal("6 h at -12.0 °C (coldest hour)", p["Longest preheat time t_auf,max"]);
            Assert.Equal("50.0 kW", p["Design capacity Φ_HL + Φ_RH"]);
            Assert.Equal("120 h/a", p["Night purge ventilation hours"]);
            Assert.Equal("2 h / 0 % (the larger value applies)", p["Surcharge (project setting)"]);
            Assert.DoesNotContain(p.Keys, k => k.Contains("Aufheiz", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Kurzform_nennt_je_Groesse_Werte_Nachtfenster_und_Saison()
        {
            // Am Gebäude stehen Tag, Nacht, Nachtfenster und Ferien in den Bestandsspalten; Vorgabezeilen tragen den Rest.
            var bestand = new Matrixeingang { SollTag = 21.0, SollNacht = 17.0, SollFerien = 16.0, NachtBeginn = 18, NachtEnde = 7 };
            Konditionierungsstand s = Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, bestand)
                .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_WOCHENENDE, Matrixzelle.Abgeschaltet())
                .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120))
                .MitVorgabe(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(2.0, 18, 7));

            List<(string Beschriftung, string Text)> de = Aufheizbericht.Konditionierung(s, DE);
            Assert.True(de.Count == 2, string.Join(" | ", de.Select(x => x.Beschriftung + " = " + x.Text)));
            Assert.Equal("Konditionierung – " + Text(nameof(R.KOND_LBL_GROESSE_HEIZEN), DE), de[0].Beschriftung);
            Assert.Equal("21/17 °C, Nacht 18–7 Uhr, Wochenende aus, Ferien 16 °C, Saison 1.10.–30.4.", de[0].Text);
            Assert.Equal("Konditionierung – " + Text(nameof(R.KOND_LBL_GROESSE_LUEFTUNG), DE), de[1].Beschriftung);
            Assert.Contains("/2 1/h, Nacht 18–7 Uhr", de[1].Text, StringComparison.Ordinal);

            List<(string Beschriftung, string Text)> en = Aufheizbericht.Konditionierung(s, EN);
            Assert.Equal("21/17 °C, night 18:00–7:00, weekend off, holidays 16 °C, season Oct 1–Apr 30", en[0].Text);

            // Ein Gebäude ohne eigene Angabe hat keine Kurzform.
            Assert.Empty(Aufheizbericht.Konditionierung(Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, null), DE));
            Assert.Empty(Aufheizbericht.Konditionierung(null, DE));
        }

        [Fact]
        public void Die_Vorlagenfelder_lesen_die_Ergebniszeile_und_der_Schalter_den_Stand()
        {
            Assert.Equal(16, Vorlagenfeldkatalog.KATALOGFASSUNG);
            foreach (string s in new[] { "gebaeude.ergebnis.aufheizzeit", "gebaeude.ergebnis.auslegungsgroesse",
                                         "gebaeude.ergebnis.nachtauskuehlstunden", "gebaeude.ergebnis.aufheizhinweise", "hat.aufheizung" })
                Assert.Equal(Vorlagenfeldkatalog.FASSUNG_AUFHEIZUNG, Vorlagenfeldkatalog.Finde(s).Seit);

            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(1);
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            var tafel = new DataTable();
            tafel.Columns.Add("ID", typeof(int));
            tafel.Rows.Add(7);
            tafel.Rows.Add(8);
            Assert.False(Loese("hat.aufheizung", w).Schalter);

            w.Stamm.Ergebnis.Gebaeude.Add(Bemessen());
            w.Stamm.Ergebnis.Gebaeude.Add(new ErgebnisGebaeudeModel { ID_Gebaeude = 8, Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007 });
            Assert.True(Loese("hat.aufheizung", w).Schalter);

            Berichtswerte g7 = w.MitGebaeude(tafel.Rows[0]);
            Assert.Equal(6.0, Loese("gebaeude.ergebnis.aufheizzeit", g7).Zahl);
            Assert.Equal(-12.0, Loese("gebaeude.ergebnis.aufheiz_aussentemperatur", g7).Zahl);
            Assert.Equal(50.0, Loese("gebaeude.ergebnis.auslegungsgroesse", g7).Zahl);
            Assert.Equal("täglich", Loese("gebaeude.ergebnis.aufheizart", g7).Text);
            Assert.Equal("Zielleistung", Loese("gebaeude.ergebnis.aufheizquelle", g7).Text);
            Assert.Equal(120.0, Loese("gebaeude.ergebnis.nachtauskuehlstunden", g7).Zahl);
            Assert.True(Loese("gebaeude.ergebnis.aufheizzeit_manuell", g7).IstLeer);
            Assert.Contains("W2", Loese("gebaeude.ergebnis.aufheizhinweise", g7).Text, StringComparison.Ordinal);

            // Das Gebäude ohne Aufheizrechnung: leer, die Hinweise ein leerer Text, nie 0.
            Berichtswerte g8 = w.MitGebaeude(tafel.Rows[1]);
            Platzhalterwert zeit = Loese("gebaeude.ergebnis.aufheizzeit", g8);
            Assert.True(zeit.IstLeer);
            Assert.NotEqual("0", zeit.Text);
            Assert.Equal("", Loese("gebaeude.ergebnis.aufheizhinweise", g8).Text);
        }

        /// <summary>
        /// Der Baustein „Projektbeschreibung“ (Festlegung 30): Mit Aufheizung stehen Zeilen, die Hinweise W2/W4 unter der Tafel
        /// und der Hinweis zur idealen Spitze da, die Kurzform im Gebäudeabschnitt; ohne bleibt der Abschnitt ohne diese Texte.
        /// </summary>
        [Fact]
        public void Der_Baustein_schreibt_Zeilen_Hinweise_und_Kurzform_nur_mit_Anlass()
        {
            string mit = Schreibe(Daten(Bemessen(), true));
            Assert.Contains("Auslegungsgröße Φ_HL + Φ_RH", mit, StringComparison.Ordinal);
            Assert.Contains(string.Format(DE, Text(nameof(R.GEBB_AUFH_W2), DE), 10), mit, StringComparison.Ordinal);
            Assert.Contains(Text(nameof(R.GEBB_AUFH_HRL_SPITZE), DE), mit, StringComparison.Ordinal);
            Assert.Contains("Konditionierung – " + Text(nameof(R.KOND_LBL_GROESSE_HEIZEN), DE), mit, StringComparison.Ordinal);
            Assert.Contains("21/17 °C", mit, StringComparison.Ordinal);

            ErgebnisGebaeudeModel ohneAufheizung = Bemessen();
            ohneAufheizung.AufheizZustand = null; ohneAufheizung.AuslegungsheizlastKw = null; ohneAufheizung.AufheizzuschlagKw = null;
            string ohne = Schreibe(Daten(ohneAufheizung, false));
            Assert.DoesNotContain("Aufheiz", ohne, StringComparison.Ordinal);
            Assert.DoesNotContain("Auslegungsgröße", ohne, StringComparison.Ordinal);
            Assert.DoesNotContain("Konditionierung – ", ohne, StringComparison.Ordinal);
        }

        private static BerichtsDaten Daten(ErgebnisGebaeudeModel g, bool mitKonditionierung)
        {
            var tafel = new DataTable();
            tafel.Columns.Add("ID", typeof(int));
            tafel.Columns.Add("Gebaeudename", typeof(string));
            tafel.Rows.Add(7, "Gebäude 1");
            var details = new ProjektDetails { Gebaeude = tafel };
            if (mitKonditionierung)
                details.Konditionierung[7] = Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude,
                        new Matrixeingang { SollTag = 21.0, SollNacht = 17.0, NachtBeginn = 22, NachtEnde = 6 })
                    .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_WOCHENENDE, Matrixzelle.Abgeschaltet());
            var erg = new ErgebnisModel();
            erg.Gebaeude.Add(g);
            var v = new VariantenDaten { IstStamm = true, Projektname = "Probe", Ergebnis = erg, Details = details };
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(v);
            return d;
        }

        private static string Schreibe(BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            new ProjektbeschreibungBaustein().SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        }

        private static Platzhalterwert Loese(string marke, Berichtswerte werte)
        {
            Platzhalterwert w = Vorlagenfeldkatalog.Loese(Platzhaltersyntax.Lies("{{" + marke + "}}"), werte);
            Assert.True(w != null, marke + " ist kein Katalogschlüssel");
            return w;
        }

        /// <summary>
        /// Der Aufheizabsatz des Kurzberichts je Sprache (E58 F4 (b)): An der Probe 1030 ohne Aufheizung entfällt er; trägt
        /// das erste Gebäude eine bemessene Ergebniszeile, steht der Satz mit den Einzelfeldern in der Sprache der Vorlage da.
        /// </summary>
        [Theory]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT, false)]
        [InlineData(BerichtsvorlageDateiWacheTests.KURZBERICHT_EN, true)]
        public void Der_Kurzbericht_fuehrt_den_Aufheizabsatz_nur_mit_Aufheizung(string datei, bool englisch)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            if (pfad == null || !File.Exists(pfad)) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = Sprache.Nummer;
            string ordner = Path.Combine(Path.GetTempPath(), "epos-aufheizbericht-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                using var kultur = new Kulturvorrichtung(englisch ? "en-US" : "de-DE");
                Sprache.Nummer = englisch ? 1 : 0;
                byte[] vorlage = File.ReadAllBytes(pfad);
                BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
                string satz = englisch ? "The preheat time is" : "Die Aufheizzeit beträgt";

                string ohne = Fuellen(BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030), konfig, vorlage,
                                      Path.Combine(ordner, "ohne.docx"));
                Assert.DoesNotContain(satz, ohne, StringComparison.Ordinal);

                BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030);
                VariantenDaten stamm = daten.Varianten.First(v => v.IstStamm);
                // 1030 rechnet ohne Gebäude: die Probe bekommt eines (nur im Speicher), mit Ergebniszeile.
                stamm.Details ??= new ProjektDetails();
                var tafel = new DataTable();
                tafel.Columns.Add("ID", typeof(int));
                tafel.Columns.Add("Gebaeudename", typeof(string));
                tafel.Rows.Add(7, "Gebäude 1");
                stamm.Details.Gebaeude = tafel;
                int id = 7;
                ErgebnisGebaeudeModel g = Bemessen();
                g.ID_Gebaeude = id;
                stamm.Ergebnis.Gebaeude.RemoveAll(x => x != null && x.ID_Gebaeude == id);
                stamm.Ergebnis.Gebaeude.Add(g);

                string mit = Fuellen(daten, konfig, vorlage, Path.Combine(ordner, "mit.docx"));
                Assert.Contains(satz + " 6 h", mit, StringComparison.Ordinal);
                Assert.Contains(englisch ? "design capacity 50.0 kW" : "Auslegungsgröße 50,0 kW", mit, StringComparison.Ordinal);
                Assert.Contains(string.Format(englisch ? EN : DE, Text(nameof(R.GEBB_AUFH_W2), englisch ? EN : DE), 10), mit,
                                StringComparison.Ordinal);
                Assert.DoesNotContain("{{", mit, StringComparison.Ordinal);
            }
            finally
            {
                Sprache.Nummer = vorher;
                try { Directory.Delete(ordner, true); } catch { /* Aufräumen nach bestem Bemühen */ }
            }
        }

        private static string Fuellen(BerichtsDaten daten, BerichtsKonfiguration konfig, byte[] vorlage, string ziel)
        {
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage,
                new Erstellerangaben { Firma = "Firma 1", Version = "1.0" }, ziel);
            Assert.Empty(e.Fehler);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            return string.Join("\n", doc.MainDocumentPart.Document.Body
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Select(p => p.InnerText));
        }
    }
}
