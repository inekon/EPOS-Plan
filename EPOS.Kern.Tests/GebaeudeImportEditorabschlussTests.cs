using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der vorbelegte Gebäudeeditor schließt mit OK</b> (Befund der Gebäudeimport-Sichtprobe, Stufe
    /// G4): Die Abbildung des Imports ließ Luftwechselrate und — ohne Personenangabe in der Datei —
    /// Fläche je Nutzer auf der 0 eines neuen Gebäudes stehen, und der Editor hielt sein OK an.
    ///
    /// <para>Geprüft wird der ganze Weg je Probe, mit und ohne Baualtersklasse: Lesen → Zuordnen →
    /// <see cref="GebaeudeImportHuelle.Vorbelegung"/> (<c>NachKatalogdaten</c> auf dem neuen Gebäude) →
    /// die Prüfregeln des Editors beim OK. Das ist DIESELBE Funktion, die <c>GebaeudeKatalogDialog</c> im
    /// OK-Weg ruft: <see cref="GebaeudeArbeitsstand.Pruefen"/> nach <c>Laden(…, neu: true)</c>, mit den
    /// Texten seines Parametersatzes (<c>GebaeudeKatalogHuelle.Prueftexte</c>/<c>Texte</c>) — nicht
    /// nachgebaut.</para>
    /// </summary>
    public sealed class GebaeudeImportEditorabschlussTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        /// <summary>Klasse E im Index der Klappliste (0 = A … 20 = U).</summary>
        private const int KLASSE_E = 4;

        /// <summary>Liest eine Probe über den Parametersatz der Hülle und ordnet sie zu — wie der Zuordnungsdialog.</summary>
        internal static async Task<(GebaeudeImportHuelle Huelle, IReadOnlyDictionary<string, object> Gaben, GebaeudeImportErgebnis Ergebnis)> Zuordnen(
            string probe, int? klasse, IReadOnlyDictionary<string, bool> haken = null)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(probe), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, klasse, haken ?? Keine));
            return (h, gaben, new GebaeudeImportErgebnis(0, klasse, "Importprobe", haken ?? Keine, stand.Zeilen.ToList()));
        }

        /// <summary>Die Prüfregeln des Editors beim OK im Modus Neu — der erste Befund, sonst <c>null</c>.</summary>
        internal static GebaeudePruefbefund EditorBefund(GebaeudeKatalogDaten vorbelegt)
        {
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(vorbelegt, neu: true);
            return arbeit.Pruefen(true, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());
        }

        private static GebaeudeFeldzeileDaten Zeile(GebaeudeImportErgebnis e, string zielfeld)
            => e.Zeile(zielfeld) ?? throw new InvalidOperationException(zielfeld);

        // =================================================================================
        //  Der Befund der Sichtprobe — für jede Probe, mit und ohne Klasse
        // =================================================================================

        [Theory]
        [InlineData("gbxml_haus_si.xml", KLASSE_E)]
        [InlineData("gbxml_haus_si.xml", null)]
        [InlineData("ifc4_haus.ifc", KLASSE_E)]
        [InlineData("ifc4_haus.ifc", null)]                       // die Klasse folgt dem Baujahr der Datei (D)
        [InlineData("gbxml_ohne_konstruktionen.xml", KLASSE_E)]
        public async Task Der_vorbelegte_Editor_nimmt_den_Import_an(string probe, int? klasse)
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportErgebnis e) = await Zuordnen(probe, klasse);

            // Der Zuordnungsdialog lässt das Ergebnis durch …
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            Assert.DoesNotContain(pruefen(e), m => m.Stufe == WarnStufe.Fehler);

            // … und der Editor nimmt die Vorbelegung ohne Handgriff an.
            GebaeudeVorbelegung v = h.Vorbelegung(e);
            GebaeudePruefbefund befund = EditorBefund(v.Daten);
            Assert.True(befund == null, probe + " / Klasse " + (klasse?.ToString() ?? "keine") + ": " + befund?.Meldung);

            // Die Pflichtangaben, die die Datei nicht liefert, stehen als ausgewiesene Vorgabe da.
            double luftwechsel = GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION + GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER;
            Assert.Equal(luftwechsel, v.Daten.Luftwechselrate);
            GebaeudeFeldzeileDaten rate = Zeile(e, GebaeudeZielfelder.LUFTWECHSELRATE);
            Assert.Equal(ImportherkunftWerte.VORGABE, rate.HerkunftSchluessel);
            Assert.True(rate.Haken);
            Assert.StartsWith("nicht in der Datei — Vorgabe 0,7 1/h = Infiltration 0,3 + Nutzerlüftung 0,4", rate.Beleg);
            Assert.Contains("Luftwechselrate 0,7 1/h", v.Herleitung);
            Assert.Equal(ImportherkunftWerte.VORGABE, Zeile(e, GebaeudeZielfelder.INNERE_GEWINNE).HerkunftSchluessel);
            Assert.Equal(0.0, v.Daten.Waermegewinne);
        }

        [Theory]
        [InlineData("gbxml_haus_si.xml", 24.0, ImportherkunftWerte.GBXML)]            // 5 Personen in 3 beheizten Räumen
        [InlineData("ifc4_haus.ifc", GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE, ImportherkunftWerte.VORGABE)]
        [InlineData("gbxml_ohne_konstruktionen.xml", GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE, ImportherkunftWerte.VORGABE)]
        public async Task Die_Flaeche_je_Nutzer_kommt_aus_der_Datei_sonst_aus_der_Vorgabe_und_nicht_aus_der_Klasse(
            string probe, double erwartet, string herkunft)
        {
            foreach (int? klasse in new int?[] { KLASSE_E, null, 0, 20 })
            {
                (GebaeudeImportHuelle h, _, GebaeudeImportErgebnis e) = await Zuordnen(probe, klasse);
                GebaeudeFeldzeileDaten z = Zeile(e, GebaeudeZielfelder.FLAECHE_JE_NUTZER);
                Assert.Equal(erwartet, z.Wert);
                Assert.Equal(herkunft, z.HerkunftSchluessel);
                Assert.True(z.Haken);
                Assert.Equal(erwartet, h.Vorbelegung(e).Daten.FlaecheNutzer);
                if (herkunft == ImportherkunftWerte.VORGABE)
                    Assert.Equal("keine vollständige Personenangabe in der Datei — Vorgabe 35 m², mit der EPOS-Plan ein Gebäude "
                                 + "ohne diese Angabe speichert", z.Beleg);
            }
        }

        /// <summary>
        /// Der Keller des Probenhauses als beheizt: Er trägt keine Personenzahl, die Angabe der Datei ist
        /// unvollständig — die Fläche je Nutzer wird die Vorgabe, samt Hinweis, und der Editor schließt.
        /// </summary>
        [Fact]
        public async Task Eine_unvollstaendige_Personenangabe_fuehrt_auf_die_Vorgabe()
        {
            var keller = new Dictionary<string, bool> { ["raum-keller"] = true };
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportErgebnis e) =
                await Zuordnen("gbxml_haus_si.xml", null, keller);

            GebaeudeFeldzeileDaten z = Zeile(e, GebaeudeZielfelder.FLAECHE_JE_NUTZER);
            Assert.Equal(GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE, z.Wert);
            Assert.Equal(ImportherkunftWerte.VORGABE, z.HerkunftSchluessel);
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            Assert.Contains(zuordnen(new GebaeudeZuordnungsanfrage(0, null, keller)).Meldungen,
                            m => m.Kennung == GebaeudeImportAblauf.MELDUNG + "PERSONEN_UNVOLLSTAENDIG");
            Assert.Null(EditorBefund(h.Vorbelegung(e).Daten));
        }

        /// <summary>
        /// <b>Ohne Quelle keine Zahl:</b> Eine Datei ohne Konstruktionen und ohne gewählte Klasse hat
        /// weder U-Werte noch g-Wert — die Vorgaben dafür gibt es nur je Baualtersklasse. Die Zeilen
        /// bleiben leer, und der Editor nennt die erste Lücke.
        /// </summary>
        [Fact]
        public async Task Ohne_Klasse_und_ohne_Konstruktionen_bleiben_U_und_g_ohne_Vorgabe()
        {
            (GebaeudeImportHuelle h, _, GebaeudeImportErgebnis e) = await Zuordnen("gbxml_ohne_konstruktionen.xml", null);

            foreach (string f in new[] { GebaeudeZielfelder.U_AUSSENWAND, GebaeudeZielfelder.U_FENSTER, GebaeudeZielfelder.U_DACH,
                                         GebaeudeZielfelder.U_GRUND, GebaeudeZielfelder.U_SONSTIGE, GebaeudeZielfelder.G_WERT })
            {
                Assert.Null(Zeile(e, f).Wert);
                Assert.Equal(GebaeudeHerkunftSchluessel.Leer, Zeile(e, f).HerkunftSchluessel);
            }
            // Die klassenfreien Vorgaben greifen trotzdem.
            Assert.Equal(ImportherkunftWerte.VORGABE, Zeile(e, GebaeudeZielfelder.LUFTWECHSELRATE).HerkunftSchluessel);
            Assert.Equal(ImportherkunftWerte.VORGABE, Zeile(e, GebaeudeZielfelder.FLAECHE_JE_NUTZER).HerkunftSchluessel);

            GebaeudePruefbefund befund = EditorBefund(h.Vorbelegung(e).Daten);
            Assert.NotNull(befund);
            Assert.Equal(GebaeudeKatalogHuelle.Texte().MeldungGWert, befund.Meldung);
        }
    }
}
