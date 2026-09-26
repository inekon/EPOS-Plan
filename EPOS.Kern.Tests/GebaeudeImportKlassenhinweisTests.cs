using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Klappliste der Baualtersklasse sagt, was gilt</b> (Befund der Windows-Sichtabnahme, Stufe
    /// G4): Der Import zog die Klasse aus dem Baujahr der Datei, die Klappliste zeigte trotzdem „keine";
    /// und bei einer Datei mit eigenen U-Werten änderte eine andere Wahl sichtbar nichts. Jetzt trägt
    /// der Stand die Klasse der Datei (<see cref="GebaeudeImportStand.KlasseDerDatei"/>) und einen
    /// Hinweis, wie viele der Klassenwerte die Klasse füllt (<see cref="GebaeudeImportStand.KlassenHinweis"/>).
    /// </summary>
    public sealed class GebaeudeImportKlassenhinweisTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        /// <summary>Klasse E im Index der Klappliste (0 = A … 12 = M) — ifc4_haus.ifc trägt das Baujahr 1965 (E47: 1958 bis 1968).</summary>
        private const int KLASSE_E = 4;

        /// <summary>Klasse F im Index der Klappliste (1969 bis 1978) — eine andere Wahl als die der Datei.</summary>
        private const int KLASSE_F = 5;

        private static async Task<GebaeudeImportStand> Stand(string probe, int? klasse)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(probe), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            return zuordnen(new GebaeudeZuordnungsanfrage(0, klasse, Keine));
        }

        /// <summary>Zählt die Klassenfelder des Stands nach Herkunft — Datei bzw. Vorgabe der Klasse.</summary>
        private static (int AusDatei, int AusKlasse) Zaehlen(GebaeudeImportStand stand)
        {
            int ausDatei = 0, ausKlasse = 0;
            foreach (string feld in GebaeudeVorgaben.Klassenfelder)
            {
                GebaeudeFeldzeileDaten z = stand.Zeilen.Single(x => x.Zielfeld == feld);
                if (z.HerkunftSchluessel == ImportherkunftWerte.IFC || z.HerkunftSchluessel == ImportherkunftWerte.GBXML) ausDatei++;
                else if (z.HerkunftSchluessel == ImportherkunftWerte.VORGABE) ausKlasse++;
            }
            return (ausDatei, ausKlasse);
        }

        private static string Wirkung(int ausKlasse, int ausDatei)
            => string.Format(R.GIMP_DLG_KLASSE_WIRKUNG, ausKlasse, GebaeudeVorgaben.Klassenfelder.Count, ausDatei);

        [Fact]
        public async Task Ohne_eigene_Wahl_zeigt_der_Stand_die_Klasse_aus_dem_Baujahr_der_Datei()
        {
            GebaeudeImportStand stand = await Stand("ifc4_haus.ifc", null);

            Assert.Equal(KLASSE_E, stand.KlasseDerDatei);
            (int ausDatei, int ausKlasse) = Zaehlen(stand);
            // Die Probe trägt ihre U-Werte und den g-Wert selbst; die Klasse füllt nur, was fehlt.
            Assert.True(ausDatei >= 5, "ausDatei = " + ausDatei);
            Assert.Equal(GebaeudeVorgaben.Klassenfelder.Count, ausDatei + ausKlasse);
            Assert.Equal(string.Format(R.GIMP_DLG_KLASSE_AUS_BAUJAHR, 1965) + " " + Wirkung(ausKlasse, ausDatei),
                         stand.KlassenHinweis);
        }

        [Fact]
        public async Task Eine_eigene_Wahl_ersetzt_die_Klasse_der_Datei_und_der_Hinweis_nennt_nur_die_Wirkung()
        {
            GebaeudeImportStand stand = await Stand("ifc4_haus.ifc", KLASSE_F);

            Assert.Null(stand.KlasseDerDatei);
            (int ausDatei, int ausKlasse) = Zaehlen(stand);
            Assert.Equal(Wirkung(ausKlasse, ausDatei), stand.KlassenHinweis);
        }

        [Fact]
        public async Task Eine_Datei_ohne_Konstruktionen_und_ohne_Baujahr_nennt_ohne_Klasse_den_allgemeinen_Hinweis()
        {
            GebaeudeImportStand ohne = await Stand("gbxml_ohne_konstruktionen.xml", null);
            Assert.Null(ohne.KlasseDerDatei);
            Assert.Equal(R.GIMP_DLG_KLASSE_HINWEIS, ohne.KlassenHinweis);

            // Mit Klasse füllt sie alles, was sie vorgibt — die Datei trägt keinen dieser Werte.
            GebaeudeImportStand mit = await Stand("gbxml_ohne_konstruktionen.xml", KLASSE_E);
            (int ausDatei, int ausKlasse) = Zaehlen(mit);
            Assert.Equal(0, ausDatei);
            Assert.Equal(GebaeudeVorgaben.Klassenfelder.Count, ausKlasse);
            Assert.Equal(Wirkung(ausKlasse, 0), mit.KlassenHinweis);
        }

        /// <summary>
        /// <see cref="GebaeudeVorgaben.Klassenfelder"/> ist genau die Menge der Zielfelder, für die eine
        /// Klasse einen Wert kennt (<see cref="GebaeudeVorgaben.Wert"/>) — sonst zählte der Hinweis falsch.
        /// </summary>
        [Fact]
        public void Die_Klassenfelder_sind_genau_die_Felder_mit_einer_Klassenvorgabe()
        {
            IEnumerable<string> alleFelder = typeof(GebaeudeZielfelder)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue());

            var mitWert = new HashSet<string>(StringComparer.Ordinal);
            for (char k = 'A'; k <= 'U'; k++)
                foreach (string feld in alleFelder)
                    if (GebaeudeVorgaben.Wert(k, feld).HasValue) mitWert.Add(feld);

            Assert.Equal(GebaeudeVorgaben.Klassenfelder.OrderBy(f => f, StringComparer.Ordinal),
                         mitWert.OrderBy(f => f, StringComparer.Ordinal));
        }
    }

    /// <summary>
    /// <b>„Simulation…" auf einer noch nicht gespeicherten Zeile nennt den richtigen Grund</b> (Befund
    /// der Windows-Sichtabnahme, Stufe G4): Ein eben importiertes Gebäude hat bis zum OK des
    /// Gebäudedialogs keine Projektkopie; die Auskunft nennt das fehlende OK, nicht Projekt und
    /// Klimaregion.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeBedarfUngespeichertTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;

        [Fact]
        public void Eine_ungespeicherte_Zeile_nennt_das_fehlende_OK()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben = GebaeudeBedarfHuelle.Gaben(
                new GebaeudeProjektZeile { IdZ = GebaeudeHuelle.STARTINDEX }, PROJEKT, out string befund);

            Assert.Null(gaben);
            Assert.Equal(R.GEB_MSG_BEDARF_UNGESPEICHERT, befund);
        }

        [Fact]
        public void Eine_gespeicherte_Zeile_rechnet_weiter()
        {
            if (!_db.Vorhanden) return;
            int idZ = Z_ProjGebCtrl.LiesProjekt(PROJEKT)[0].ID_Z;

            IReadOnlyDictionary<string, object> gaben = GebaeudeBedarfHuelle.Gaben(
                new GebaeudeProjektZeile { IdZ = idZ }, PROJEKT, out string befund);

            Assert.NotNull(gaben);
            Assert.Null(befund);
        }
    }
}
