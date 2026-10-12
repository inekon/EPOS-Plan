using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>GD-2: die Gebäudewerte der Projektdatei</b> (Entwurf Gebäudedialog 2.2, Entscheide F4 und F8): Der Leser zieht Gebäudetyp,
    /// Nutzung, Wärmeschutz- und Effizienzstandard und <c>Cwirk</c>; die festen Tabellen bilden nur belegte Codes ab, jeder andere
    /// erscheint benannt; <c>Cwirk</c> setzt die Bauweise mit Herkunft „Projektdatei“ vor der Ableitung aus den Schichten; die
    /// Baualtersklasse folgt EINER Regel (<see cref="Baujahrregel.KlasseWirksam"/>); der Name des neuen Gebäudes ist der
    /// Dateiname, der Gebäudename der Datei steht als Vorschlag daneben.
    /// </summary>
    [Collection("Testdatenbank")]   // die Hüllenfälle teilen den Prozesszustand der Hülle mit GebaeudeImportHuelleTests
    public sealed class SqprojGebaeudewerteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly List<string> _dateien = new();

        public void Dispose()
        {
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
            _kultur.Dispose();
        }

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        private string Schreiben(SqprojProbenErzeuger e, string name)
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-sqproj-gd2-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            string pfad = e.Schreiben(Path.Combine(ordner, name + ".sqproj"));
            _dateien.Add(pfad);
            return pfad;
        }

        private static SqprojProbenErzeuger MitCodes(SqprojProbenErzeuger e, int? typ, int? nutzung, int? schutz, int? effizienz, double? cwirk)
        {
            e.Gebaeudetyp = typ;
            e.Nutzungstyp = nutzung;
            e.Waermeschutzstandard = schutz;
            e.Effizienzstandard = effizienz;
            e.Cwirk = cwirk;
            return e;
        }

        // =================================================================================
        //  Leser und Tabellen
        // =================================================================================

        [Fact]
        public void Der_Leser_zieht_die_Gebaeudecodes_und_Cwirk()
        {
            string pfad = Schreiben(MitCodes(SqprojProbenErzeuger.Standard(), 3, 0, 0, 0, 50.0), "codes");
            SqprojGebaeude g = Assert.Single(SqprojLeser.Lesen(pfad).Gebaeude);
            Assert.Equal(3, g.Gebaeudetyp);
            Assert.Equal(0, g.Nutzungstyp);
            Assert.Equal(0, g.Waermeschutzstandard);
            Assert.Equal(0, g.Effizienzstandard);
            Assert.Equal(50.0, g.CwirkWhM2K);

            SqprojGebaeudecodes.Ergebnis e = SqprojGebaeudecodes.Abbilden(g);
            Assert.Equal(GebaeudeStammCtrl.FILTERWERT_SONSTIGE, e.Verwendung);
            Assert.Equal(SqprojGebaeudecodes.MELDUNG_VERWENDUNG, e.VerwendungHerleitung.Schluessel);
            Assert.Equal(new[] { GebaeudeStammCtrl.Verwendungstext(GebaeudeStammCtrl.FILTERWERT_SONSTIGE), SqprojGebaeudecodes.SPALTE_TYP, "3" },
                         e.VerwendungHerleitung.Werte);
            Assert.Null(e.Gebaeudeart);
            Assert.Null(e.Energiestandard);
            Assert.Equal(50.0, e.BauweiseJeM2);
            Assert.Empty(e.Meldungen);   // 0 heißt „keine Angabe“ und meldet nichts
        }

        [Fact]
        public void Eine_Probe_ohne_Gebaeudecodes_bleibt_leer_und_still()
        {
            string pfad = Schreiben(SqprojProbenErzeuger.Standard(), "ohne");
            SqprojGebaeude g = Assert.Single(SqprojLeser.Lesen(pfad).Gebaeude);
            Assert.Null(g.Gebaeudetyp);
            Assert.Null(g.Nutzungstyp);
            Assert.Null(g.CwirkWhM2K);
            SqprojGebaeudecodes.Ergebnis e = SqprojGebaeudecodes.Abbilden(g);
            Assert.Null(e.Verwendung);
            Assert.Null(e.VerwendungHerleitung);
            Assert.Null(e.BauweiseJeM2);
            Assert.Empty(e.Meldungen);
        }

        [Fact]
        public void Unbelegte_Codes_bleiben_ohne_Abbildung_und_erscheinen_benannt()
        {
            var g = new SqprojGebaeude { Gebaeudetyp = 7, Nutzungstyp = 5, Waermeschutzstandard = 4, Effizienzstandard = 2, CwirkWhM2K = -1.0 };
            SqprojGebaeudecodes.Ergebnis e = SqprojGebaeudecodes.Abbilden(g);
            Assert.Null(e.Verwendung);
            Assert.Null(e.Gebaeudeart);
            Assert.Null(e.Energiestandard);
            Assert.Null(e.BauweiseJeM2);
            Assert.All(e.Meldungen, m => Assert.Equal(SqprojGebaeudecodes.MELDUNG_OHNE_ABBILDUNG, m.Schluessel));
            Assert.Equal(new[] { "BuildingUsageType=5", "BuildingType=7", "BuildingEnergyEfficientType=2", "BuildingHeatInsulationStandardType=4" },
                         e.Meldungen.Select(m => m.Werte[0] + "=" + m.Werte[1]));
            // Jeder Schlüssel hat einen Text in beiden Sprachen.
            Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.IMP_GEB_PROT_GEBAEUDECODE_OHNE_ABBILDUNG));
        }

        [Fact]
        public void Eine_belegte_Zeile_setzt_Gebaeudeart_und_Energiestandard_mit_Herkunft()
        {
            var art = new Dictionary<int, string> { [11] = "Bürogebäude" };
            var es = new Dictionary<int, string> { [4] = Energiestandard.CODES[0] };
            var g = new SqprojGebaeude { Gebaeudetyp = 11, Waermeschutzstandard = 4, Effizienzstandard = 9 };
            SqprojGebaeudecodes.Ergebnis e = SqprojGebaeudecodes.Abbilden(g, art, new Dictionary<int, string>(), new Dictionary<int, string>(), es);
            Assert.Equal("Bürogebäude", e.Gebaeudeart);
            Assert.Equal(Energiestandard.CODES[0], e.Energiestandard);
            Assert.Equal(new[] { SqprojGebaeudecodes.MELDUNG_GEBAEUDEART, SqprojGebaeudecodes.MELDUNG_OHNE_ABBILDUNG,
                                 SqprojGebaeudecodes.MELDUNG_ENERGIESTANDARD },
                         e.Meldungen.Select(m => m.Schluessel));
            // Der unbelegte Effizienzcode meldet sich, der Wärmeschutzstandard trägt.
            Assert.Equal(SqprojGebaeudecodes.SPALTE_EFFIZIENZ, e.Meldungen[1].Werte[0]);
        }

        [Fact]
        public void Die_Tabellen_tragen_nur_belegte_Codes()
        {
            Assert.Empty(SqprojGebaeudecodes.TabelleGebaeudeart);
            Assert.Empty(SqprojGebaeudecodes.TabelleVerwendungAusNutzung);
            Assert.Empty(SqprojGebaeudecodes.TabelleEnergiestandard);
            Assert.Equal(new[] { 3 }, SqprojGebaeudecodes.TabelleVerwendungAusTyp.Keys);
            Assert.All(SqprojGebaeudecodes.TabelleEnergiestandard.Values, v => Assert.Contains(v, Energiestandard.CODES));
        }

        // =================================================================================
        //  Zuordnung: Cwirk → Bauweise, Verwendung mit Vorrang
        // =================================================================================

        [Theory]
        [InlineData(50.0, GebaeudeZielfelder.BAUART_SCHWER)]
        [InlineData(90.0, GebaeudeZielfelder.BAUART_SEHR_SCHWER)]
        [InlineData(20.0, GebaeudeZielfelder.BAUART_LEICHT)]
        public void Cwirk_setzt_die_Bauweise_mit_Herkunft_Projektdatei(double cwirk, string bauart)
        {
            string pfad = Schreiben(MitCodes(SqprojImportTests.Sporthaus(), 3, null, null, null, cwirk), "cwirk");
            GebaeudeImportAblauf a = SqprojImportTests.Lesen(pfad);
            GebaeudeImportSatz s = a.Zuordnen(0, null);
            double flaeche = s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert.Value;
            GebaeudeFeldzeile bauweise = s.Zeile(GebaeudeZielfelder.BAUWEISE);
            Assert.Equal(Math.Round(cwirk * flaeche), bauweise.Wert);
            Assert.Equal(ImportherkunftWerte.AusFormat(GebaeudeQuelle.FORMAT_SQPROJ), bauweise.Herkunft);
            Assert.True(bauweise.Uebernehmen);
            Assert.Equal("GIMP_BELEG_BAUWEISE_CWIRK", bauweise.Beleg.Schluessel);
            GebaeudeFeldzeile art = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(bauart, art.Textwert);
            Assert.Equal("GIMP_BELEG_BAUART_CWIRK", art.Beleg.Schluessel);

            // Die Verwendung der Datei geht der Ableitung aus den Zonen vor.
            Gebaeudeverwendung.Ergebnis v = a.Verwendung(0);
            Assert.Equal(GebaeudeStammCtrl.FILTERWERT_SONSTIGE, v.Verwendung);
            Assert.Equal(SqprojGebaeudecodes.MELDUNG_VERWENDUNG, v.Herleitung.Schluessel);
        }

        [Fact]
        public void Ohne_Cwirk_bleibt_die_Bauweise_abgeleitet()
        {
            GebaeudeImportAblauf a = SqprojImportTests.Lesen(Schreiben(SqprojImportTests.Sporthaus(), "ohnecwirk"));
            GebaeudeImportSatz s = a.Zuordnen(0, null);
            Assert.Null(s.Zeile(GebaeudeZielfelder.BAUWEISE).Wert);
            Assert.False(s.Zeile(GebaeudeZielfelder.BAUWEISE).Uebernehmen);
            Assert.NotEqual(SqprojGebaeudecodes.MELDUNG_VERWENDUNG, a.Verwendung(0).Herleitung?.Schluessel);
        }

        // =================================================================================
        //  Eine Quelle der Baualtersklasse
        // =================================================================================

        [Fact]
        public void Die_Klasse_folgt_einer_Regel()
        {
            Assert.Equal('H', Baujahrregel.KlasseWirksam('H', true, 1995, out bool aus1));
            Assert.False(aus1);
            Assert.Equal('I', Baujahrregel.KlasseWirksam('H', false, 1995, out bool aus2));
            Assert.True(aus2);
            Assert.Equal('H', Baujahrregel.KlasseWirksam('H', false, null, out bool aus3));
            Assert.False(aus3);
            Assert.Null(Baujahrregel.KlasseWirksam(null, true, null, out _));
            Assert.Equal(8, Baujahrregel.KlassenIndexWirksam(7, false, 1995));
            Assert.Equal(7, Baujahrregel.KlassenIndexWirksam(7, true, 1995));
            Assert.Equal(7, Baujahrregel.KlassenIndexWirksam(7, false, null));
        }

        /// <summary>
        /// Befund GD 1.3: Ein Baujahr, das im Import von Hand gesetzt ist, und eine Klasse, die nur Vorgabe war, ergaben im
        /// Editor eine andere Klasse als im Kopf. Die Vorbelegung nimmt jetzt dieselbe Regel: ohne ausdrückliche Wahl die Klasse
        /// des übernommenen Baujahrs, mit Wahl die Wahl.
        /// </summary>
        [Fact]
        public async Task Die_Vorbelegung_nimmt_die_Klasse_nach_derselben_Regel()
        {
            (GebaeudeImportHuelle h, GebaeudeImportStand stand) = await Gbxml();
            List<GebaeudeFeldzeileDaten> zeilen = stand.Zeilen
                .Select(z => z.Zielfeld == GebaeudeZielfelder.BAUJAHR
                    ? z with { Wert = 1995, Haken = true, HerkunftSchluessel = GebaeudeHerkunftSchluessel.Manuell }
                    : z)
                .ToList();
            var ohneWahl = new GebaeudeImportErgebnis(0, 7, stand.Vorschlagsname, Keine, zeilen);
            GebaeudeKatalogDaten d = h.Vorbelegung(ohneWahl).Daten;
            Assert.Equal(1995, d.Baujahr);
            Assert.Equal(8, d.Baualtersklasse);                // I – 1995 bis 2001, wie der Vorschlag des Editors

            var mitWahl = new GebaeudeImportErgebnis(0, 7, stand.Vorschlagsname, Keine, zeilen, KlasseGewaehlt: true);
            Assert.Equal(7, h.Vorbelegung(mitWahl).Daten.Baualtersklasse);   // H – die Wahl gilt
        }

        // =================================================================================
        //  Name = Dateiname (F8) bei allen drei Formaten, Vorbelegung mit Cwirk
        // =================================================================================

        private static Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>> Lesen(
            IReadOnlyDictionary<string, object> gaben)
            => (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];

        private static Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> Zuordnen(IReadOnlyDictionary<string, object> gaben)
            => (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];

        private static async Task<(GebaeudeImportHuelle, GebaeudeImportStand)> Lauf(string pfad)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            GebaeudeLesestand gelesen = await Lesen(gaben)(pfad, null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            return (h, Zuordnen(gaben)(new GebaeudeZuordnungsanfrage(0, 5, Keine)));
        }

        private static Task<(GebaeudeImportHuelle, GebaeudeImportStand)> Gbxml() => Lauf(GbxmlImportTests.Probe("gbxml_haus_si.xml"));

        [Fact]
        public async Task Der_Name_ist_der_Dateiname_bei_gbXML()
        {
            (_, GebaeudeImportStand stand) = await Gbxml();
            Assert.Equal("gbxml_haus_si", stand.Vorschlagsname);
            Assert.False(string.IsNullOrEmpty(stand.NameDerDatei));      // der Gebäudename der Datei als Vorschlag
            Assert.NotEqual(stand.Vorschlagsname, stand.NameDerDatei);
        }

        [Fact]
        public async Task Der_Name_ist_der_Dateiname_bei_IFC()
        {
            (_, GebaeudeImportStand stand) = await Lauf(Path.Combine(IfcProbenTests.Ordner(), "ifc4_zwei_gebaeude.ifc"));
            Assert.Equal("ifc4_zwei_gebaeude", stand.Vorschlagsname);
            Assert.Equal("Gebäude A", stand.NameDerDatei);
        }

        [Fact]
        public async Task Der_Name_ist_der_Dateiname_bei_der_Projektdatei_und_Cwirk_bleibt_fest()
        {
            string pfad = Schreiben(MitCodes(SqprojImportTests.Sporthaus(), 3, 0, 0, 0, 90.0), "Sportheim_Probe");
            (GebaeudeImportHuelle h, GebaeudeImportStand stand) = await Lauf(pfad);
            Assert.Equal("Sportheim_Probe", stand.Vorschlagsname);
            Assert.Equal("Probegebäude", stand.NameDerDatei);

            GebaeudeVorbelegung v = h.Vorbelegung(new GebaeudeImportErgebnis(0, 5, stand.Vorschlagsname, Keine, stand.Zeilen));
            Assert.Equal("Sportheim_Probe", v.Daten.Name);
            Assert.Equal(GebaeudeStammCtrl.FILTERWERT_SONSTIGE, v.Daten.Verwendung);
            Assert.True(v.Daten.BauweiseAusDatei);
            double flaeche = v.Daten.WohnflaecheGesamt ?? 0;
            Assert.Equal(Math.Round(90.0 * flaeche), v.Daten.Bauweise);
            Assert.Equal(Gebaeudebauweise.SEHR_SCHWER, v.Daten.Bauart);

            // Der Editor im Modus Neu führt die Bauweise der Datei nicht der Bauart nach (sehr schwer wäre × 100).
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(v.Daten, neu: true);
            Assert.False(arbeit.BauweiseNachfuehren);
            Assert.Equal(Math.Round(90.0 * flaeche), arbeit.Probestand().Bauweise);
            // Eine neue Bauart führt sie wieder nach.
            arbeit.BauartWaehlen(Gebaeudebauweise.SCHWER);
            Assert.Equal(Gebaeudebauweise.BauweiseAusBauart(Gebaeudebauweise.SCHWER, flaeche), arbeit.Probestand().Bauweise);
        }

        // =================================================================================
        //  Diagnose an der Anwenderdatei (nur Zählungen; fehlt sie, schweigt der Fall)
        // =================================================================================

        [Fact]
        public void Diagnose_Gebaeudewerte_der_Anwenderdatei()
        {
            string wurzel = Wurzel();
            string pfad = wurzel == null ? null : Path.Combine(wurzel, "Quellen", "Sportheim_1970_unsaniert.sqproj");
            if (pfad == null || !File.Exists(pfad)) return;   // die Datei kann ein Verweis sein (Länge des Verweises)
            SqprojAbbild abbild = SqprojLeser.Lesen(pfad);
            int belegt(Func<SqprojGebaeude, bool> f) => abbild.Gebaeude.Count(f);
            _aus?.WriteLine("Gebäude " + abbild.Gebaeude.Count + ": Typ " + belegt(g => g.Gebaeudetyp.HasValue)
                            + ", Nutzung " + belegt(g => g.Nutzungstyp.HasValue) + ", Wärmeschutz " + belegt(g => g.Waermeschutzstandard.HasValue)
                            + ", Effizienz " + belegt(g => g.Effizienzstandard.HasValue) + ", Cwirk " + belegt(g => g.CwirkWhM2K.HasValue)
                            + ", Baujahr " + belegt(g => g.Baujahr.HasValue));
            foreach (SqprojGebaeude g in abbild.Gebaeude)
            {
                SqprojGebaeudecodes.Ergebnis e = SqprojGebaeudecodes.Abbilden(g);
                _aus?.WriteLine("Codes Typ/Nutzung/Schutz/Effizienz: " + g.Gebaeudetyp + "/" + g.Nutzungstyp + "/" + g.Waermeschutzstandard + "/"
                                + g.Effizienzstandard + "; Verwendung abgebildet " + (e.Verwendung != null) + ", Gebäudeart " + (e.Gebaeudeart != null)
                                + ", Energiestandard " + (e.Energiestandard != null) + ", ohne Abbildung " + e.Meldungen.Count(m => m.Schluessel == SqprojGebaeudecodes.MELDUNG_OHNE_ABBILDUNG)
                                + ", Cwirk im Band " + (g.CwirkWhM2K >= 5.0 && g.CwirkWhM2K <= 200.0)
                                + ", Bauart " + (g.CwirkWhM2K is double c ? GebaeudeZielfelder.BauartSchluessel(Gebaeudebauweise.BauartAusBauweise(c, 1.0)) : "-"));
            }
            GebaeudeImportAblauf a = SqprojImportTests.Lesen(pfad);
            GebaeudeImportSatz s = a.Zuordnen(0, null);
            // Die Verwendung ohne den Code: was die Zonen allein sagen (Gegenprobe zur Belegung des Typs).
            string vorher = a.Abbild.Gebaeude[0].VerwendungDatei;
            a.Abbild.Gebaeude[0].VerwendungDatei = null;
            Gebaeudeverwendung.Ergebnis zonen = a.Verwendung(0);
            a.Abbild.Gebaeude[0].VerwendungDatei = vorher;
            _aus?.WriteLine("Bauweise-Zeile Herkunft " + s.Zeile(GebaeudeZielfelder.BAUWEISE).Herkunft + ", Haken " + s.Zeile(GebaeudeZielfelder.BAUWEISE).Uebernehmen
                            + "; Klasse " + s.Baualtersklasse + " aus Baujahr " + (GebaeudeZuordnungsModell.KlasseDerDatei(s) != null)
                            + "; Verwendung aus den Zonen allein: " + zonen.Verwendung + " (" + zonen.Anhalt + ")"
                            + "; Name = Dateiname " + (GebaeudeZuordnungsModell.Vorschlagsname(s) == Path.GetFileNameWithoutExtension(pfad))
                            + ", Vorschlag aus der Datei " + (GebaeudeZuordnungsModell.NameDerDatei(s).Length > 0));
        }

        private readonly Xunit.Abstractions.ITestOutputHelper _aus;

        public SqprojGebaeudewerteTests(Xunit.Abstractions.ITestOutputHelper aus) => _aus = aus;

        private static string Wurzel([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            foreach (string start in new[] { Path.GetDirectoryName(eigeneDatei), AppContext.BaseDirectory })
            {
                string o = start;
                while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                    o = Path.GetDirectoryName(o);
                if (o != null) return o;
            }
            return null;
        }
    }
}
