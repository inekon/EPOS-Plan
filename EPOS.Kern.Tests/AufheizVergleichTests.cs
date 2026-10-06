using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Aufheizoptimierung im Variantenvergleich</b> (Entwurf KP3, Welle O3b; E58 F3 (c), E59, B21; Festlegungen 29, 39):
    /// die Abweichungsmerkmale der Aufheizung unter „Gebäude“ — Schalter, Bemessung, Abzug, Reserve, Art (mit „manuell“),
    /// Aufschlag in Stunden und Prozent, manuelle Aufheizzeit des ersten Gebäudes —, die Kennzahlgruppe „Gebäude“ mit Δ und
    /// die Gebäudetafel je Stand (<c>stand.tabelle.gebaeude</c>, ohne Δ) samt ihren Vorlagenfeldern der Fassung 16.
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizVergleichTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        // Abweichungsmerkmale (B21, Festlegungen 29 und 39)
        // =================================================================

        /// <summary>Ein Projekt mit Aufheizeinstellung und einem Gebäude; <c>null</c> heißt NULL in der Datenbank.</summary>
        private static ProjektDetails Details(bool schalter, string bemessung = null, double? abzug = null, double? reserve = null,
                                              string art = null, double? aufschlagH = null, double? aufschlagProzent = null,
                                              params long?[] manuell)
        {
            var ein = new DataTable();
            ein.Columns.Add("ID", typeof(long));
            ein.Columns.Add("ID_Projekt", typeof(long));
            ein.Columns.Add("Aufheizoptimierung", typeof(long));
            ein.Columns.Add("Aufheiz_Bemessung", typeof(string));
            ein.Columns.Add("Aufheiz_Abzug_K", typeof(double));
            ein.Columns.Add("Aufheiz_Reserve", typeof(double));
            ein.Columns.Add("Aufheiz_Art", typeof(string));
            ein.Columns.Add("Aufheiz_Aufschlag_H", typeof(long));
            ein.Columns.Add("Aufheiz_Aufschlag_Prozent", typeof(double));
            ein.Rows.Add(1L, 1L, schalter ? 1L : 0L, (object)bemessung ?? DBNull.Value, (object)abzug ?? DBNull.Value,
                         (object)reserve ?? DBNull.Value, (object)art ?? DBNull.Value,
                         aufschlagH.HasValue ? (long)aufschlagH.Value : (object)DBNull.Value, (object)aufschlagProzent ?? DBNull.Value);

            var geb = new DataTable();
            geb.Columns.Add("ID", typeof(long));
            geb.Columns.Add("Gebaeudename", typeof(string));
            geb.Columns.Add("Aufheizzeit_Manuell_H", typeof(long));
            long?[] zeiten = manuell != null && manuell.Length > 0 ? manuell : new long?[] { null };
            for (int i = 0; i < zeiten.Length; i++)
                geb.Rows.Add(10L + i, "Gebäude " + (i + 1), (object)zeiten[i] ?? DBNull.Value);

            return new ProjektDetails { IdProjekt = 1, Einstellungen = ein, Gebaeude = geb };
        }

        private static Abweichung Finde(List<Abweichung> liste, string merkmal)
            => liste.SingleOrDefault(a => a.Gewerk == "Gebäude" && a.Merkmal == merkmal);

        /// <summary>
        /// <b>B21, rote Probe:</b> Der Vergleich mit und ohne Rampe (P8) meldete „keine Abweichungen“ — der Ermittler kannte
        /// die Aufheizspalten nicht. Jetzt steht der Schalter als Ja/Nein-Merkmal da.
        /// </summary>
        [Fact]
        public void Mit_und_ohne_Rampe_ist_eine_Abweichung()
        {
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(Details(false), Details(true));
            Abweichung schalter = Finde(liste, R.ABW_MERKMAL_AUFH_SCHALTER);
            Assert.NotNull(schalter);
            Assert.Equal("Nein", schalter.WertStamm);
            Assert.Equal("Ja", schalter.WertVariante);
            Assert.Single(liste);
        }

        /// <summary>NULL und die Vorgabe heißen dasselbe — Bemessung „kälteste Stunde“, Abzug 2 K, Reserve 20 %, Art täglich, kein Aufschlag.</summary>
        [Fact]
        public void NULL_und_Vorgabe_sind_keine_Abweichung()
        {
            Assert.Empty(AbweichungsErmittler.Vergleiche(
                Details(true),
                Details(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 2.0, 0.2, DbWerte.AUFHEIZ_ART_TAEGLICH, 0, 0.0)));
        }

        /// <summary>Jedes Merkmal meldet sich mit Anzeigename bzw. wirksamem Wert samt Einheit (Festlegungen 29, 39).</summary>
        [Fact]
        public void Jedes_Aufheizmerkmal_meldet_sich_mit_seinem_Anzeigewert()
        {
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(
                Details(true),
                Details(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, 3.0, 0.3, DbWerte.AUFHEIZ_ART_FEST, 2, 10.0, 6));

            void Pruefe(string merkmal, string stamm, string variante)
            {
                Abweichung a = Finde(liste, merkmal);
                Assert.True(a != null, merkmal + " fehlt");
                Assert.Equal(stamm, a.WertStamm);
                Assert.Equal(variante, a.WertVariante);
            }
            Pruefe(R.ABW_MERKMAL_AUFH_BEMESSUNG, R.SIMKONF_AUFH_BEMESSUNG_STUNDE, R.SIMKONF_AUFH_BEMESSUNG_ABZUG);
            Pruefe(R.ABW_MERKMAL_AUFH_ABZUG, "2,0 K", "3,0 K");
            Pruefe(R.ABW_MERKMAL_AUFH_RESERVE, "20 %", "30 %");
            // Das eine Gebäude der Variante trägt eine manuelle Zeit: Seine Art ist „manuell“, nicht die des Projekts.
            Pruefe(R.ABW_MERKMAL_AUFH_ART, "täglich", "manuell");
            Pruefe(R.ABW_MERKMAL_AUFH_AUFSCHLAG_H, "0 h", "2 h");
            Pruefe(R.ABW_MERKMAL_AUFH_AUFSCHLAG_PROZENT, "0,0 %", "10,0 %");
            Pruefe(R.ABW_MERKMAL_AUFH_MANUELL, "—", "6 h");
            Assert.Equal(7, liste.Count);
        }

        /// <summary>
        /// Die Art gilt über alle Gebäude: zwei Gebäude, das zweite manuell, zeigen „täglich, manuell“ bzw. „fest, manuell“; die
        /// manuelle Zeit des ersten Gebäudes bleibt gleich (leer) und meldet sich nicht.
        /// </summary>
        [Fact]
        public void Die_Art_gilt_ueber_alle_Gebaeude()
        {
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(
                Details(true, manuell: new long?[] { null, 8 }),
                Details(true, art: DbWerte.AUFHEIZ_ART_FEST, manuell: new long?[] { null, 8 }));
            Abweichung art = Finde(liste, R.ABW_MERKMAL_AUFH_ART);
            Assert.NotNull(art);
            Assert.Equal("täglich, manuell", art.WertStamm);
            Assert.Equal("fest, manuell", art.WertVariante);
            Assert.Null(Finde(liste, R.ABW_MERKMAL_AUFH_MANUELL));
            Assert.Single(liste);

            // Ohne Gebäude gibt es keine Aufheizmerkmale; die Pseudozeile trägt keine ID (keine Übernahme).
            var ohne = new ProjektDetails { Einstellungen = Details(true).Einstellungen, Gebaeude = new DataTable() };
            Assert.Null(AbweichungsErmittler.Aufheizmerkmale(ohne));
            DataRow zeile = AbweichungsErmittler.Aufheizmerkmale(Details(true));
            Assert.Equal(DbWerte.AUFHEIZ_ART_TAEGLICH, zeile[AbweichungsErmittler.SPALTE_AUFHEIZART]);
            Assert.False(zeile.Table.Columns.Contains("ID"));
        }

        /// <summary>Beschriftung und Werte folgen der Oberflächensprache (Glossar: „Preheat optimisation“, „manual“).</summary>
        [Fact]
        public void Englisch_heissen_Merkmale_und_Werte_nach_dem_Glossar()
        {
            using var kultur = new Kulturvorrichtung("en-US");
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(Details(false), Details(true, manuell: 6));
            Assert.Contains(liste, a => a.Merkmal == "Preheat optimisation");
            Abweichung art = liste.Single(a => a.Merkmal == "Preheat time mode");
            Assert.Equal("daily", art.WertStamm);
            Assert.Equal("manual", art.WertVariante);
            Assert.Contains(liste, a => a.Merkmal == "Manual preheat time (h)");
        }

        // =================================================================
        // Kennzahlgruppe „Gebäude“ mit Δ (E58 F3 (c))
        // =================================================================

        /// <summary>Ein bemessenes Gebäude nach VDI 6007 mit runden Phantasiewerten.</summary>
        private static ErgebnisGebaeudeModel Gebaeude(int id, int nacht, int sommer, int zeit, int stunden, double leistung,
                                                      double heizlast, double zuschlag) => new ErgebnisGebaeudeModel
        {
            ID_Gebaeude = id, Merkplatz = id, Gebaeudename = "Gebäude " + id, Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
            HeizwaermeMwh = 100.0, SpitzeKw = 70.0, SpitzeTagesmittelKw = 30.0, Spitze95Kw = 40.0,
            NachtauskuehlstundenH = nacht, SommerlueftungsstundenH = sommer,
            AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizBemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE,
            AufheizArt = DbWerte.AUFHEIZ_ART_TAEGLICH, AufheizzeitMaxH = zeit, AufheizAussenC = -10.0,
            AufheizLeistungKw = leistung, AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_ZIEL,
            Aufheiztage = 200, AufheizstundenH = stunden, AufheizzeitLaengsteH = zeit,
            AuslegungsheizlastKw = heizlast, AufheizzuschlagKw = zuschlag,
        };

        /// <summary>Ein Gebäude ohne Aufheizrechnung und ohne Lüftungswerte (Schalter aus).</summary>
        private static ErgebnisGebaeudeModel OhneAufheizung(int id) => new ErgebnisGebaeudeModel
        {
            ID_Gebaeude = id, Merkplatz = id, Gebaeudename = "Gebäude " + id, Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
            HeizwaermeMwh = 100.0, SpitzeKw = 70.0, SpitzeTagesmittelKw = 30.0, Spitze95Kw = 40.0,
        };

        /// <summary>Je Stand die Gebäudezeilen des Ergebnisses (erster Stand = Stamm); die Kennzahlen sind berechnet.</summary>
        private static BerichtsDaten Gruppe(params ErgebnisGebaeudeModel[][] gebaeudeJeStand)
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(gebaeudeJeStand.Length);
            for (int i = 0; i < gebaeudeJeStand.Length; i++)
            {
                daten.Varianten[i].Ergebnis.Gebaeude.AddRange(gebaeudeJeStand[i]);
                KennzahlenKatalog.Berechne(daten.Varianten[i]);
            }
            return daten;
        }

        private static Dictionary<string, List<string>> Zeilen(Berichtstabelle t)
            => t.Zeilen.ToDictionary(z => z.Zellen[0].Text, z => z.Zellen.Skip(1).Select(c => c.Text).ToList());

        /// <summary>
        /// Zwei Stände mit Aufheizung: Stunden und Zeiten als Höchstwert über die Gebäude, Leistungen als Summe, Δ je Zeile
        /// (Variante − Stamm). Die Gruppe steht im Katalog seit Fassung 16 und heißt englisch „Buildings“.
        /// </summary>
        [Fact]
        public void Die_Kennzahlgruppe_Gebaeude_traegt_Hoechstwerte_Summen_und_Delta()
        {
            BerichtsDaten daten = Gruppe(
                new[] { Gebaeude(1, 120, 300, 6, 800, 60.0, 40.0, 10.0), Gebaeude(2, 50, 400, 8, 600, 20.0, 15.0, 5.0) },
                new[] { Gebaeude(1, 100, 300, 4, 700, 50.0, 40.0, 5.0) });
            VariantenDaten stamm = daten.Varianten[0];
            Assert.Equal(120.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_NACHTAUSKUEHLSTUNDEN]);
            Assert.Equal(400.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_SOMMERLUEFTUNGSSTUNDEN]);
            Assert.Equal(8.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_AUFHEIZZEIT]);
            Assert.Equal(800.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_AUFHEIZSTUNDEN]);
            Assert.Equal(80.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_AUFHEIZLEISTUNG]);
            Assert.Equal(70.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_AUSLEGUNGSGROESSE]);

            Berichtstabelle t = Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_GEBAEUDE, false, CultureInfo.GetCultureInfo("de-DE"));
            Dictionary<string, List<string>> z = Zeilen(t);
            Assert.Equal(6, z.Count);
            // Je Zeile Stamm, Variante, Δ.
            Assert.Equal(new[] { "80,0", "50,0" }, z["Aufheizleistung P_auf (Summe der Gebäude) [kW]"].Take(2));
            Assert.Contains("30", z["Aufheizleistung P_auf (Summe der Gebäude) [kW]"][2], StringComparison.Ordinal);
            Assert.Contains("4", z["Längste Aufheizzeit t_auf,max (Höchstwert der Gebäude) [h]"][2], StringComparison.Ordinal);
            Assert.Contains("100", z["Sommerlüftungsstunden (Höchstwert der Gebäude) [h/a]"][2], StringComparison.Ordinal);

            Assert.All(KennzahlenKatalog.Alle().Where(k => k.Gruppe == KennzahlenKatalog.GR_GEBAEUDE), k =>
            {
                Assert.Equal(16, k.Seit);
                Assert.Equal(16, Vorlagenfeldkatalog.SeitDerKennzahl(k));
                Assert.True(k.DeltaAnzeigen);
            });
            Assert.Equal(1, Vorlagenfeldkatalog.SeitDerKennzahl(KennzahlenKatalog.Alle().First(k => k.Schluessel == "energie.waermebedarf")));
            Assert.Equal("Buildings", BerichtTexte.T(KennzahlenKatalog.GR_GEBAEUDE, true));
        }

        /// <summary>
        /// Ein Stand ohne Aufheizung: Die Zeilen bleiben, weil der Stamm sie trägt; der Wert der Variante ist „—“ und das Δ fehlt
        /// (Strich, nie 0). Die Nachtauskühlstunden, die beide tragen, behalten ihr Δ.
        /// </summary>
        [Fact]
        public void Ohne_Aufheizung_in_der_Variante_fehlt_das_Delta()
        {
            ErgebnisGebaeudeModel nurNacht = OhneAufheizung(1);
            nurNacht.NachtauskuehlstundenH = 90;
            BerichtsDaten daten = Gruppe(new[] { Gebaeude(1, 120, 300, 6, 800, 60.0, 40.0, 10.0) }, new[] { nurNacht });
            Dictionary<string, List<string>> z = Zeilen(Berichtstabellen.Vergleichsgruppe(
                daten, KennzahlenKatalog.GR_GEBAEUDE, false, CultureInfo.GetCultureInfo("de-DE")));
            List<string> leistung = z["Aufheizleistung P_auf (Summe der Gebäude) [kW]"];
            Assert.Equal("60,0", leistung[0]);
            Assert.Equal(Tabellenzelle.STRICH, leistung[1]);
            Assert.Equal(Tabellenzelle.STRICH, leistung[2]);
            Assert.Contains("30", z["Nachtauskühlstunden (Höchstwert der Gebäude) [h/a]"][2], StringComparison.Ordinal);
        }

        /// <summary>Kein Stand trägt Lüftungs- oder Aufheizwerte: Die Gruppe entfällt (leere Tafel), der Standardbericht zeigt sie nicht.</summary>
        [Fact]
        public void Ohne_Werte_entfaellt_die_Gruppe()
        {
            BerichtsDaten daten = Gruppe(new[] { OhneAufheizung(1) }, new[] { OhneAufheizung(1) });
            Assert.Empty(Berichtstabellen.Gruppenzeilen(daten, KennzahlenKatalog.GR_GEBAEUDE));
            Assert.True(Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_GEBAEUDE, false, CultureInfo.GetCultureInfo("de-DE")).IstLeer);
            Assert.DoesNotContain("Gebäude je Projekt", SchreibeVergleich(daten), StringComparison.Ordinal);
        }

        // =================================================================
        // Gebäudetafel je Stand (stand.tabelle.gebaeude, ohne Δ) und Vorlagenfelder v16
        // =================================================================

        /// <summary>
        /// <c>stand.tabelle.gebaeude</c> löst je Stand dieselbe Tafel wie <c>tabelle.gebaeude.ergebnis</c> für den Stamm — keine
        /// zweite Quelle; ein Stand ohne Gebäudezeile bleibt leer. Tafel, Schalter, Gruppentafel und die erzeugten Kennzahlfelder
        /// stehen seit Fassung 16.
        /// </summary>
        [Fact]
        public void Die_Gebaeudetafel_je_Stand_ist_die_Tafel_des_Stamms()
        {
            BerichtsDaten daten = Gruppe(new[] { Gebaeude(1, 120, 300, 6, 800, 60.0, 40.0, 10.0) }, Array.Empty<ErgebnisGebaeudeModel>());
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);

            Berichtstabelle stamm = Loese("tabelle.gebaeude.ergebnis", w).Tabelle;
            Berichtstabelle jeStand = Loese("stand.tabelle.gebaeude", w.MitStand(daten.Varianten[0])).Tabelle;
            Assert.False(jeStand.IstLeer);
            Assert.Equal(stamm.Zeilen.Select(z => string.Join("|", z.Zellen.Select(c => c.Text))),
                         jeStand.Zeilen.Select(z => string.Join("|", z.Zellen.Select(c => c.Text))));
            Assert.Contains(jeStand.Zeilen, z => z.Zellen[0].Text == "Aufheizleistung P_auf");

            Assert.True(Loese("stand.tabelle.gebaeude", w.MitStand(daten.Varianten[1])).IstLeer);
            Assert.True(Loese("hat.tabelle.gebaeude", w).Schalter);
            Assert.True(Loese("hat.tabelle.vergleich.gebaeude", w).Schalter);

            foreach (string schluessel in new[] { "stand.tabelle.gebaeude", "hat.tabelle.gebaeude", "tabelle.vergleich.gebaeude",
                                                  "hat.tabelle.vergleich.gebaeude", "stand.kennzahl.geb.aufheizleistung",
                                                  "stand.delta.geb.aufheizzeit", "kennzahl.geb.nachtauskuehlstunden.beschriftung",
                                                  "vergleich.spanne.geb.auslegungsgroesse" })
            {
                Vorlagenfeld f = Vorlagenfeldkatalog.Finde(schluessel);
                Assert.True(f != null, schluessel);
                Assert.Equal(Vorlagenfeldkatalog.FASSUNG_AUFHEIZUNG, f.Seit);
            }
            Assert.Equal(Vorlagenfeldkontext.Stand, Vorlagenfeldkatalog.Finde("stand.tabelle.gebaeude").Kontext);
            Assert.False(string.IsNullOrEmpty(R.VF_STAND__TABELLE__GEBAEUDE));
        }

        /// <summary>Die Kennzahlfelder je Stand: der Stammwert und das Δ der Variante (Variante − Stamm).</summary>
        [Fact]
        public void Das_Delta_der_Gebaeudekennzahl_steht_als_Vorlagenfeld()
        {
            BerichtsDaten daten = Gruppe(new[] { Gebaeude(1, 120, 300, 6, 800, 60.0, 40.0, 10.0) },
                                         new[] { Gebaeude(1, 100, 300, 4, 700, 50.0, 40.0, 5.0) });
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            Assert.Equal(-10.0, Loese("stand.delta.geb.aufheizleistung", w.MitStand(daten.Varianten[1])).Zahl);
            Assert.Equal(60.0, Loese("stamm.kennzahl.geb.aufheizleistung", w).Zahl);
        }

        /// <summary>
        /// Der Standardbericht: Mit Varianten und Gebäudewerten steht im Vergleich die Gruppe „Gebäude“ und je Stand die
        /// Gebäudetafel; ohne Variante nicht (die Tafel des Stamms steht schon in der Projektbeschreibung).
        /// </summary>
        [Fact]
        public void Der_Vergleichsbaustein_zeigt_die_Gebaeudetafel_je_Stand_nur_mit_Varianten()
        {
            string mit = SchreibeVergleich(Gruppe(new[] { Gebaeude(1, 120, 300, 6, 800, 60.0, 40.0, 10.0) },
                                                  new[] { Gebaeude(1, 100, 300, 4, 700, 50.0, 40.0, 5.0) }));
            Assert.Contains("Gebäude je Projekt", mit, StringComparison.Ordinal);
            Assert.Contains("Aufheizleistung P_auf (Summe der Gebäude)", mit, StringComparison.Ordinal);
            Assert.Contains("Längste Aufheizzeit t_auf,max", mit, StringComparison.Ordinal);

            string ohne = SchreibeVergleich(Gruppe(new[] { Gebaeude(1, 120, 300, 6, 800, 60.0, 40.0, 10.0) }));
            Assert.DoesNotContain("Gebäude je Projekt", ohne, StringComparison.Ordinal);
        }

        private static string SchreibeVergleich(BerichtsDaten daten)
        {
            using var ms = new System.IO.MemoryStream();
            using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            DocumentFormat.OpenXml.Packaging.MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
            new VergleichBaustein().SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        }

        private static Platzhalterwert Loese(string marke, Berichtswerte werte)
        {
            Platzhalterwert w = Vorlagenfeldkatalog.Loese(Platzhaltersyntax.Lies("{{" + marke + "}}"), werte);
            Assert.True(w != null, marke + " ist kein Katalogschlüssel");
            return w;
        }
    }
}
