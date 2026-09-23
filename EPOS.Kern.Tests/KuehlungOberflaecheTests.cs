using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kälteseite an der Oberfläche, Stufe KU1 Welle 3</b> (Kühlkonzept 8, 9.2, 10.3):
    /// die Datenseiten (Hüllen) der Masken, die der Kern speist — der Katalogeditor bildet die
    /// Kühleingaben NULL-erhaltend ab, der Bedarfsdialog eines Gebäudes ruft den Rechenweg des
    /// Laufs (Hausregel „Eine Auskunft ruft den Rechenweg des Laufs") und nennt, wie sein
    /// Kältebedarf entsteht, samt der Grenze der Zahl (K5).
    ///
    /// <para>Die Fälle mit Datenbank schalten die Kühlung an einer Arbeitskopie ein — die
    /// Referenzprojekte, die sie benutzen, sind in der Testdatenbank aus; eingeschaltet ist allein
    /// 1017, das Referenzprojekt mit Kühlung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlungOberflaecheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =============================================================================
        //  Gebäudedialog (8.1): die Abbildung des Katalogeditors
        // =============================================================================

        /// <summary>
        /// Katalogsatz → Feldsatz → Katalogsatz: Die vier Kühleingaben reisen NULL-erhaltend —
        /// ein leerer Sollwert bleibt NULL („Kühlung aus"), keine Grenze bleibt „unbegrenzt", und
        /// der Nachtwert (KU3), den der Dialog nicht zeigt, geht auch bei „Speichern unter" (leerer
        /// Vorgängersatz) nicht verloren.
        /// </summary>
        [Fact]
        public void Der_Katalogeditor_bildet_die_Kuehleingaben_NULL_erhaltend_ab()
        {
            var satz = new GebaeudeModel
            {
                Gebaeudename = "Kuehlprobe",
                Kuehlung_Aktiv = true,
                Kuehl_Sollwert = 26.0,
                Kuehlleistung_Max = null,
                Kuehl_Sollwert_Nacht = 28.0
            };

            GebaeudeKatalogDaten d = GebaeudeKatalogHuelle.AusModell(satz);
            Assert.True(d.KuehlungAktiv);
            Assert.Equal(26.0, d.KuehlSollwert);
            Assert.Null(d.KuehlleistungMax);
            Assert.Equal(28.0, d.KuehlSollwertNacht);

            GebaeudeModel neu = GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel());
            Assert.True(neu.Kuehlung_Aktiv);
            Assert.Equal(26.0, neu.Kuehl_Sollwert);
            Assert.Null(neu.Kuehlleistung_Max);
            Assert.Equal(28.0, neu.Kuehl_Sollwert_Nacht);

            d.KuehlungAktiv = false;
            d.KuehlSollwert = null;
            GebaeudeModel aus = GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel { Kuehl_Sollwert = 24.0 });
            Assert.False(aus.Kuehlung_Aktiv);
            Assert.Null(aus.Kuehl_Sollwert);
        }

        /// <summary>
        /// Die drei neuen Beschriftungen und Meldungen des Gebäudedialogs kommen aus den
        /// Ressourcen, in beiden Sprachen (N-K5) — das Bündel der Hülle trägt sie.
        /// </summary>
        [Fact]
        public void Die_Texte_der_Gruppe_Kuehlung_kommen_aus_den_Ressourcen()
        {
            GebaeudeHuelleTexte t = GebaeudeKatalogHuelle.Texte();
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_GRP_KUEHLUNG, t.GruppeKuehlung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_LBL_KUEHL_SOLLWERT, t.LabelKuehlSollwert);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_ZEILE_KUEHLUNG_BESTANDSWEG,
                         t.ZeileKuehlungBestandsweg);
            Assert.Contains("{2}", t.MeldungKuehlsollwertHeizung);
        }

        // =============================================================================
        //  Bedarfsdialog Gebäude (8.4): Herleitung und Auskunft = Lauf
        // =============================================================================

        private static GebaeudeBedarfErgebnis Ergebnis(string modell, double? soll = null, double? grenze = null,
                                                      bool projekt = true, bool aktiv = true)
            => new GebaeudeBedarfErgebnis
            {
                Erfolgreich = true,
                Name = "EFH",
                Modell = modell,
                KuehlSollwertC = soll,
                KuehlleistungMaxKw = grenze,
                KuehlbetriebProjekt = projekt,
                KuehlungAktiv = aktiv,
                ObereRaumtemperaturC = modell == DbWerte.GEBAEUDE_MODELL_VDI6007 ? 27.0 : (double?)null
            };

        /// <summary>
        /// Die Herleitung nennt den Weg, auf dem der Kältebedarf entsteht — Bestandsweg (0 mit
        /// Hinweis, F-K18), wirksam (Sollwert und Grenze), eingeschaltet ohne Sollwert, Projekt
        /// aus, nicht gekühlt — und als LETZTE Zeile immer die Grenze der Zahl (K5).
        /// </summary>
        [Fact]
        public void Die_Herleitung_des_Kaelteabschnitts_nennt_den_Weg_und_die_Feuchtegrenze()
        {
            string vdi = DbWerte.GEBAEUDE_MODELL_VDI6007;
            var faelle = new (GebaeudeBedarfErgebnis Ergebnis, string Erwartet)[]
            {
                (Ergebnis(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ), "Tagesbilanz (Bestandsweg) liefert keine Kühllast"),
                (Ergebnis(vdi, 26.0, 12.5), "Gekühlt auf 26,0 °C, Kühlleistungsgrenze 12,5 kW."),
                (Ergebnis(vdi, 26.0), "Kühlleistungsgrenze unbegrenzt."),
                (Ergebnis(vdi, projekt: false), "Projekteinstellung „Kühlung rechnen“ aus"),
                (Ergebnis(vdi), "ohne Kühlsollwert"),
                (Ergebnis(vdi, aktiv: false), "Das Gebäude wird nicht gekühlt"),
            };

            foreach ((GebaeudeBedarfErgebnis e, string erwartet) in faelle)
            {
                List<string> zeilen = GebaeudeBedarfHuelle.Kaelteherleitung(e);
                Assert.Equal(2, zeilen.Count);
                Assert.Contains(erwartet, zeilen[0]);
                Assert.Equal(SimulationKaeltebedarf.GrenzeFeuchte, zeilen[1]);
            }

            Assert.Contains("27,0 °C", GebaeudeBedarfHuelle.Kaelteherleitung(Ergebnis(vdi, aktiv: false))[0]);
            Assert.Empty(GebaeudeBedarfHuelle.Kaelteherleitung(new GebaeudeBedarfErgebnis()));

            // E32: Ohne wirksame Kühlung läuft das Gebäude frei - kein „informativer" Kühlbedarf mehr.
            foreach (GebaeudeBedarfErgebnis frei in new[] { Ergebnis(vdi, projekt: false), Ergebnis(vdi, aktiv: false) })
            {
                string zeile = GebaeudeBedarfHuelle.Kaelteherleitung(frei)[0];
                Assert.Contains("läuft frei", zeile);
                Assert.Contains("Überhitzungsstunden", zeile);
                Assert.DoesNotContain("informativ", zeile);
            }
        }

        /// <summary>
        /// <b>Entscheid E32 im Bedarfsdialog.</b> Ein Gebäude ohne wirksame Kühlung läuft frei:
        /// Der Abschnitt „Kältebedarf" steht (VDI-Weg), aber ohne Kühlreihe — Kühlbedarf,
        /// Kältelast, Kühlstunden und Stunden mit Heizen und Kühlen sind <c>null</c> und erscheinen
        /// als „—" (K18), es gibt kein Kältebild und keine Kühlspalte; die Überhitzungsstunden des
        /// freien Laufs stehen, und die Herleitung sagt, warum.
        /// </summary>
        [Fact]
        public void Ein_ungekuehltes_Gebaeude_zeigt_den_Kaelteabschnitt_ohne_Kuehlnullen()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1045;                                // Referenzprojekt ohne Kühlung
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> liste =
                GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false, admin: false);
            GebaeudeProjektZeile zeile = ((List<GebaeudeProjektZeile>)liste["Zeilen"])[0];

            IReadOnlyDictionary<string, object> gaben = GebaeudeBedarfHuelle.Gaben(zeile, PROJEKT);
            var d = (GebaeudeBedarfDaten)gaben["Daten"];
            Assert.True(d.IstVdi6007);
            Assert.True(d.KaelteAbschnitt);
            Assert.Null(d.KuehlenergieMwh);
            Assert.Null(d.KaeltelastMaxKw);
            Assert.Null(d.KuehlstundenH);
            Assert.Null(d.VollbenutzungsstundenKaelteH);
            Assert.Null(d.StundenHeizenUndKuehlenH);
            Assert.Empty(d.KuehlMonatswerteMwh);
            Assert.NotNull(d.UeberhitzungsstundenH);
            Assert.Null(gaben["BildauftragKaelte"]);
            Assert.Contains("läuft frei", d.KaelteHerleitung[0]);
            Assert.Equal(SimulationKaeltebedarf.GrenzeFeuchte, d.KaelteHerleitung[1]);
        }

        /// <summary>
        /// <b>Auskunft und Lauf liefern denselben Kühlvektor</b> (10.3, Hausregel des Kerns): Der
        /// Bedarfsdialog eines gekühlten Gebäudes zeigt die Kühlreihe, die der Lauf in den
        /// Kühlkanal bucht — hier ein Projekt mit einem Gebäude, bitgleich. Sollwert, Grenze und
        /// Projektschalter stehen im Ergebnis, das Bestandsweg-Kennzeichen nicht.
        /// </summary>
        [Fact]
        public void Die_Gebaeudeauskunft_liefert_den_Kuehlvektor_des_Laufs()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1045, GEBAEUDE = 10651;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = 24, Kuehlleistung_Max = 40 WHERE ID = ?",
                new DbParam("@id", GEBAEUDE)));
            Assert.True(KonfigurationCtrl.KuehlbetriebSetzen(PROJEKT, true));

            int klima = Klimaregion(PROJEKT);
            var lauf = new SimulationWaermebedarf();
            lauf.Waermebedarf_berechnen(PROJEKT, klima);
            Assert.True(string.IsNullOrEmpty(lauf.Fehlertext), lauf.Fehlertext);
            Assert.Equal(1, lauf.Kaelteseite.GekuehlteGebaeude);
            Assert.True(lauf.Kaelteseite.Kaeltebedarf_Gesamt > 0.0);

            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = " + GEBAEUDE.ToString(CultureInfo.InvariantCulture)),
                CultureInfo.InvariantCulture);
            GebaeudeBedarfErgebnis auskunft = GebaeudeBedarfCtrl.Rechnen(PROJEKT, klima, idZ);

            Assert.True(auskunft.Erfolgreich);
            Assert.Equal(lauf.Kaelteseite.Kaeltebedarf_Gebaeude, auskunft.KuehlbedarfKwh);
            Assert.Equal(lauf.Kaelteseite.Kaeltebedarf_Max, auskunft.KaeltelastMaxKw);
            Assert.Equal(24.0, auskunft.KuehlSollwertC);
            Assert.Equal(40.0, auskunft.KuehlleistungMaxKw);
            Assert.True(auskunft.KuehlbetriebProjekt);
            Assert.False(auskunft.KaelteBestandsweg);
            Assert.Equal(12, auskunft.KuehlMonatswerteMwh.Length);
            Assert.Equal(lauf.Kaelteseite.Kaeltebedarf_Gebaeude_Gesamt, auskunft.KuehlMonatswerteMwh.Sum(), 9);
        }

        // =============================================================================
        //  Ergebnisansicht (8.4): Bedarfsreiter und Übersicht
        // =============================================================================

        /// <summary>
        /// Die Kälteseite des Ergebnisses gibt es nur, wenn der Lauf sie ERHOBEN hat: Projekt aus
        /// → <c>null</c> (keine Kältegruppe, K18); Projekt an → die Zahlen der Fassade, die
        /// vierte Kanalzeile „Kühlung", der Satz zur ungedeckten Kälte und die Grenze der Zahl.
        /// </summary>
        [Fact]
        public void Die_Ergebnisseite_zeigt_die_Kaelte_nur_wenn_erhoben()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1045, GEBAEUDE = 10651;
            int klima = Klimaregion(PROJEKT);

            var aus = new SimulationWaermebedarf();
            aus.Waermebedarf_berechnen(PROJEKT, klima);
            Assert.Null(SimulationErgebnisCtrl.Kaelte(aus));
            Assert.Null(SimulationErgebnisHuelle.KaelteDaten(SimulationErgebnisCtrl.Kaelte(aus)));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = 24 WHERE ID = ?",
                new DbParam("@id", GEBAEUDE)));
            Assert.True(KonfigurationCtrl.KuehlbetriebSetzen(PROJEKT, true));
            var an = new SimulationWaermebedarf();
            an.Waermebedarf_berechnen(PROJEKT, klima);

            SimulationErgebnisCtrl.KaelteErgebnis k = SimulationErgebnisCtrl.Kaelte(an);
            Assert.NotNull(k);
            Assert.Equal(an.Kaelteseite.Kaeltebedarf_Gesamt, k.KaeltebedarfMwh);
            Assert.Equal(an.Kaelteseite.Kaeltebedarf_Max, k.KaeltelastMaxKw);
            Assert.Equal(an.Kaelteseite.StundenMitKuehlbedarf, k.StundenMitKuehlbedarf);
            Assert.Equal(k.KaeltebedarfMwh, k.KaelterestbedarfMwh);          // KU1: ungedeckt
            Assert.Equal(an.Kaelteseite.Kaeltebedarf, k.KaeltebedarfKwh);
            Assert.Equal(k.KaeltebedarfMwh * 1000.0 / k.KaeltelastMaxKw, k.VollbenutzungsstundenH.Value, 9);

            EPOS.UI.Seiten.Simulation.KaelteDaten d = SimulationErgebnisHuelle.KaelteDaten(k);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KANAL_KUEHLUNG_ANZEIGE, d.Kanalname);
            Assert.Equal(SimulationKaeltebedarf.GrenzeFeuchte, d.GrenzeFeuchte);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMERG_HRL_KAELTE_UNGEDECKT, d.Deckungshinweis);
            Assert.Contains(d.Deckungshinweis, d.Hinweise);
            Assert.DoesNotContain(d.GrenzeFeuchte, d.Hinweise);
        }

        /// <summary>Kältebedarf 0 (erhoben, aber nichts gekühlt): kein Deckungssatz, dafür der Leersatz.</summary>
        [Fact]
        public void Erhobene_Kaelte_ohne_Bedarf_nennt_den_Leersatz()
        {
            var k = new SimulationErgebnisCtrl.KaelteErgebnis { KaeltebedarfMwh = 0.0 };
            EPOS.UI.Seiten.Simulation.KaelteDaten d = SimulationErgebnisHuelle.KaelteDaten(k);

            Assert.Equal("", d.Deckungshinweis);
            Assert.Equal(new[] { WindowsFormsApplication1.MyResource.Resource.SIMERG_HRL_KAELTE_LEER }, d.Hinweise);
        }

        // =============================================================================
        //  Kennzahlen und Bericht (6.4, 8.4; E30, K5, K18)
        // =============================================================================

        private static VariantenDaten Variante(double? jahr, double? spitze = null, double[] kanalreihe = null)
        {
            var erg = new ErgebnisModel();
            erg.Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Waermebedarf_Gesamt = 100.0,
                Waermelast_Max = 50.0,
                Waermebedarf_Kanal = new[] { 100.0, 0.0, 0.0, jahr ?? 0.0 },
                Kaeltebedarf_Gesamt = jahr,
                Kaeltelast_Max = jahr.HasValue ? spitze ?? 0.0 : (double?)null,
                Kaelterestbedarf = jahr
            };
            var v = new VariantenDaten { IstStamm = true, Projektname = "Probe", Ergebnis = erg };
            if (kanalreihe != null)
            {
                v.Zeitreihen = new ZeitreihenSatz();
                v.Zeitreihen.Reihen[ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG)] = kanalreihe;
            }
            KennzahlenKatalog.Berechne(v);
            return v;
        }

        private static BerichtsDaten Bericht(VariantenDaten v)
        {
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(v);
            return d;
        }

        /// <summary>
        /// Die Kanalkennzahlen der Kälte (6.4) — die Katalogschlüssel der Softwarearchitektur,
        /// in der Gruppe der Wärmekanäle: ohne erhobene Kälte <c>null</c> (keine Zeile, K18),
        /// mit Kälte Jahreskälte und Spitze aus den Ergebnisspalten und die Stunden am
        /// Kanalvektor gezählt; bei erhobener Kälte 0 sind es 0 Stunden.
        /// </summary>
        [Fact]
        public void Die_Kaeltekennzahlen_stehen_nur_mit_erhobener_Kaelte()
        {
            VariantenDaten ohne = Variante(null);
            Assert.Null(ohne.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_JAHRESBEDARF]);
            Assert.Null(ohne.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_SPITZE]);
            Assert.Null(ohne.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN]);

            var reihe = new double[8760];
            reihe[4000] = 3.0; reihe[4001] = 8.25; reihe[4002] = 1.25;
            VariantenDaten mit = Variante(12.5, 8.25, reihe);
            Assert.Equal(12.5, mit.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_JAHRESBEDARF]);
            Assert.Equal(8.25, mit.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_SPITZE]);
            Assert.Equal(3.0, mit.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN]);

            Assert.Null(Variante(12.5, 8.25).Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN]);
            Assert.Equal(0.0, Variante(0.0).Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN]);

            List<Kennzahl> kaelte = KennzahlenKatalog.Alle().Where(k => k.Schluessel.StartsWith("kaelte.", StringComparison.Ordinal)).ToList();
            Assert.Equal(new[] { "kaelte.jahresbedarf", "kaelte.spitze", "kaelte.stunden" },
                         kaelte.Select(k => k.Schluessel));
            Assert.All(kaelte, k => Assert.Equal(KennzahlenKatalog.GR_ENERGIE, k.Gruppe));
        }

        /// <summary>
        /// <b>Wächter K5 (E31): die Grenze an JEDER Kältezahl</b> — jede Kennzahl mit dem Präfix
        /// <c>kaelte.</c> trägt „sensibel" in beiden Sprachen; Bericht und Excel beschriften ihre
        /// Tabellen mit genau diesen Texten. Eine neue Kältekennzahl ohne die Grenze fällt hier.
        /// </summary>
        [Fact]
        public void Waechter_jede_Kaeltekennzahl_traegt_die_Grenze()
        {
            List<Kennzahl> kaelte = KennzahlenKatalog.Alle().Where(k => k.Schluessel.StartsWith("kaelte.", StringComparison.Ordinal)).ToList();
            Assert.NotEmpty(kaelte);
            Assert.All(kaelte, k =>
            {
                Assert.Contains("(sensibel)", k.LabelDe);
                Assert.Contains("(sensible)", k.LabelEn);
            });
            Assert.False(string.IsNullOrWhiteSpace(SimulationKaeltebedarf.GrenzeFeuchte));
        }

        /// <summary>
        /// Der Bericht (8.4): Der Abschnitt „Kältebedarf" der Projektbeschreibung steht nur mit
        /// erhobener Kälte — Summe, Kanalzeile „davon Kühlung", Spitze, Stunden, ungedeckte Kälte,
        /// der Satz zur Deckung und die Grenze der Zahl; ohne erhobene Kälte entfällt er
        /// vollständig, und der Variantenvergleich führt die Kältezeilen nur mit Wert.
        /// </summary>
        [Fact]
        public void Der_Bericht_fuehrt_den_Kaelteabschnitt_nur_mit_erhobener_Kaelte()
        {
            var reihe = new double[8760];
            reihe[4000] = 8.25;
            string mit = Schreibe(new ProjektbeschreibungBaustein(), Bericht(Variante(12.5, 8.25, reihe)));
            Assert.Contains(ProjektbeschreibungBaustein.UEBERSCHRIFT_KAELTE, mit);
            Assert.Contains("Kältebedarf gesamt", mit);
            Assert.Contains("davon Kühlung", mit);
            Assert.Contains("Stunden mit Kühlbedarf", mit);
            Assert.Contains("Kältebedarf ungedeckt", mit);
            Assert.Contains("12,5 MWh/a", mit);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_HRL_KAELTE_UNGEDECKT, mit);
            Assert.Contains(SimulationKaeltebedarf.GrenzeFeuchte, mit);

            string ohne = Schreibe(new ProjektbeschreibungBaustein(), Bericht(Variante(null)));
            Assert.DoesNotContain("Kälte", ohne);
            Assert.DoesNotContain(SimulationKaeltebedarf.GrenzeFeuchte, ohne);

            string vergleichMit = Schreibe(new VergleichBaustein(), Bericht(Variante(12.5, 8.25, reihe)));
            Assert.Contains("Kältebedarf gesamt (sensibel)", vergleichMit);
            string vergleichOhne = Schreibe(new VergleichBaustein(), Bericht(Variante(null)));
            Assert.DoesNotContain("(sensibel)", vergleichOhne);
        }

        /// <summary>Schreibt einen Baustein in ein Dokument im Speicher und liefert dessen Text.</summary>
        private static string Schreibe(IBerichtsBaustein baustein, BerichtsDaten daten)
        {
            using var ms = new System.IO.MemoryStream();
            using DocumentFormat.OpenXml.Packaging.WordprocessingDocument doc =
                DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            DocumentFormat.OpenXml.Packaging.MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            baustein.SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        }

        // =============================================================================
        //  Export (9.2): die Grenze der Zahl reist mit
        // =============================================================================

        /// <summary>
        /// Der Kanalexport des Referenzlaufs (4.7, K17): <c>waermebedarf_kuehlung.csv</c> entsteht
        /// NUR, wenn der Lauf Kälte erhoben hat UND einen Kältebedarf &gt; 0 führt — ein Projekt ohne
        /// Kühlung (jedes Referenzprojekt) bekommt keine Datei, auch keine voller Nullen; ebenso ein
        /// Projekt mit Schalter, aber ohne gekühltes Gebäude.
        /// </summary>
        [Fact]
        public void Der_Kanalexport_der_Kaelte_entsteht_nur_wenn_erhoben()
        {
            Assert.Empty(KaelteErgebnisexport.Reihen(null));
            Assert.Empty(KaelteErgebnisexport.Reihen(new SimulationKaeltebedarf()));
            Assert.Equal("waermebedarf_kuehlung.csv", KaelteErgebnisexport.DATEI);
            Assert.Equal(SimulationKaeltebedarf.GrenzeFeuchte, KaelteErgebnisexport.Grenze);
            if (!_db.Vorhanden) return;

            const int PROJEKT = 1045, GEBAEUDE = 10651;
            int klima = Klimaregion(PROJEKT);

            var aus = new SimulationWaermebedarf();
            aus.Waermebedarf_berechnen(PROJEKT, klima);
            Assert.Empty(KaelteErgebnisexport.Reihen(aus.Kaelteseite));

            Assert.True(KonfigurationCtrl.KuehlbetriebSetzen(PROJEKT, true));   // erhoben, aber nichts gekühlt
            var leer = new SimulationWaermebedarf();
            leer.Waermebedarf_berechnen(PROJEKT, klima);
            Assert.True(leer.Kaelteseite.Gerechnet);
            Assert.Equal(0.0, leer.Kaelteseite.Kaeltebedarf_Gesamt);
            Assert.Empty(KaelteErgebnisexport.Reihen(leer.Kaelteseite));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = 24 WHERE ID = ?",
                new DbParam("@id", GEBAEUDE)));
            var an = new SimulationWaermebedarf();
            an.Waermebedarf_berechnen(PROJEKT, klima);
            var reihen = KaelteErgebnisexport.Reihen(an.Kaelteseite);
            Assert.Single(reihen);
            Assert.Equal(KaelteErgebnisexport.DATEI, reihen[0].Key);
            Assert.Equal(an.Kaelteseite.Kaeltebedarf, reihen[0].Value);
        }

        /// <summary>
        /// Der CSV-Export der Kälte trägt die Grenze der Zahl (K5) als Kopfzeile VOR der
        /// Spaltenzeile — ein Trennzeichen darin wird entschärft. Ohne Kopfzeilen beginnt jede
        /// andere Datei unverändert mit der Spaltenzeile.
        /// </summary>
        [Fact]
        public void Der_CSV_Export_der_Kaelte_traegt_die_Grenze_in_der_Kopfzeile()
        {
            string ordner = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                                   "epos-kaeltecsv-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(ordner);
            try
            {
                var reihe = new double[8760];
                reihe[4000] = 12.5;
                var spalten = new List<CsvSpalte> { new CsvSpalte("Kältelast [kW]", reihe) };

                string mit = System.IO.Path.Combine(ordner, "mit.csv");
                CsvExportClass.Schreiben(mit, null, spalten, false,
                                         new[] { SimulationKaeltebedarf.GrenzeFeuchte, "a;b" });
                string[] zeilen = System.IO.File.ReadAllLines(mit);
                Assert.Equal(SimulationKaeltebedarf.GrenzeFeuchte, zeilen[0]);
                Assert.Equal("a,b", zeilen[1]);
                Assert.StartsWith("Zeitstempel;", zeilen[2]);
                Assert.EndsWith(";Kältelast [kW]", zeilen[2]);
                Assert.Equal(8760 + 3, zeilen.Length);

                string ohne = System.IO.Path.Combine(ordner, "ohne.csv");
                CsvExportClass.Schreiben(ohne, null, spalten, false);
                Assert.StartsWith("Zeitstempel;", System.IO.File.ReadAllLines(ohne)[0]);
                Assert.Equal(8760 + 1, System.IO.File.ReadAllLines(ohne).Length);
            }
            finally
            {
                System.IO.Directory.Delete(ordner, true);
            }
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }
    }
}
