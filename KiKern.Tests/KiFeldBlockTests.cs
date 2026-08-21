using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Der Feldblock (Fachkonzept 11.5): „Feld · alt → neu" je Zeile, „Knopf ‚X' wird
    /// ausgelöst" fuer den Knopf - erzeugt aus Katalog und gelesenen Werten, nie aus
    /// Modelltext.
    /// </summary>
    public class KiFeldBlockTests : IDisposable
    {
        private static readonly CultureInfo De = new CultureInfo("de-DE");

        public void Dispose() => KiTexte.Lieferant = null;

        private static KiFeldAenderung A(string name, string? alt, string? neu)
            => new KiFeldAenderung(name, alt, neu);

        // =============================================================== Feldbloecke

        [Fact]
        public void DerFeldblockNenntMaskeUndJedeZeileAltUndNeu()
        {
            string block = KiFeldBlock.Felder("Heizkessel bearbeiten", new[]
            {
                A("Wartungskosten", "850", "1200"),
                A("Nutzungsdauer", "15", "20")
            });

            Assert.Equal("Maske: Heizkessel bearbeiten\n" +
                         "Wartungskosten · 850 → 1200\n" +
                         "Nutzungsdauer · 15 → 20\n", block);
        }

        [Fact]
        public void EinLeererWertWirdBenanntNichtVerschwiegen()
        {
            string block = KiFeldBlock.Felder("Photovoltaik", new[]
            {
                A("Leistung", "", "12,5"),
                A("Bemerkung", "alt", null)
            });

            Assert.Contains("Leistung · (leer) → 12,5", block);
            Assert.Contains("Bemerkung · alt → (leer)", block);
        }

        [Fact]
        public void DerBlockBringtKeineEigenenAufzaehlungszeichenMit()
        {
            // Er geht als Vorschautext in KiBestaetigung.Erzeuge, und DORT bekommt jede
            // Zeile den Punkt vorangestellt. Ein eigener Punkt ergaebe doppelte.
            string block = KiFeldBlock.Felder("Heizkessel bearbeiten",
                                              new[] { A("Wartungskosten", "850", "1200") });

            Assert.DoesNotContain(KiBestaetigung.Punkt, block);
            Assert.StartsWith("Maske:", block);
        }

        [Fact]
        public void DerBlockPasstInDenBestaetigungstext()
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Registerabbild.MitHoeherenStufen(),
                                                     "variante_anlegen",
                                                     "{\"projekt_id\":7,\"bezeichner\":\"Variante B\"}");
            Assert.True(p.Gueltig, p.FehlerText());

            string block = KiFeldBlock.Felder("Heizkessel bearbeiten",
                                              new[] { A("Wartungskosten", "850", "1200") });
            string text = KiBestaetigung.Erzeuge(p.Aufruf!, block, De);

            Assert.Contains(KiTexte.FeldVorschau + ":", text);
            Assert.Contains(KiBestaetigung.Punkt + "Maske: Heizkessel bearbeiten", text);
            Assert.Contains(KiBestaetigung.Punkt + "Wartungskosten · 850 → 1200", text);
        }

        [Fact]
        public void EinBlockOhneAenderungIstKeinBlock()
        {
            // Sonst klickte der Anwender auf „Ausführen", ohne dass etwas geschieht.
            Assert.Throws<ArgumentException>(
                () => KiFeldBlock.Felder("Heizkessel bearbeiten", Array.Empty<KiFeldAenderung>()));
            Assert.Throws<ArgumentNullException>(
                () => KiFeldBlock.Felder("Heizkessel bearbeiten", null!));
        }

        [Fact]
        public void OhneMaskennamenGibtEsKeinenBlock()
        {
            var eine = new[] { A("Wartungskosten", "850", "1200") };

            Assert.Throws<ArgumentException>(() => KiFeldBlock.Felder("  ", eine));
            Assert.Throws<ArgumentException>(() => KiFeldBlock.Knopf("", "Speichern"));
        }

        [Fact]
        public void EineAenderungBrauchtDenAnzeigenamenDesFeldes()
        {
            Assert.Throws<ArgumentException>(() => A("  ", "850", "1200"));
            Assert.Throws<ArgumentException>(() => A(null!, "850", "1200"));
        }

        [Fact]
        public void EineAenderungWeissObSichUeberhauptEtwasAendert()
        {
            Assert.True(A("Wartungskosten", "850", "1200").IstAenderung);
            Assert.False(A("Wartungskosten", "850", "850").IstAenderung);
            Assert.False(A("Wartungskosten", null, "").IstAenderung);
        }

        // =============================================================== Knopfbloecke

        [Fact]
        public void DerKnopfblockNenntMaskeUndKnopf()
        {
            string block = KiFeldBlock.Knopf("Heizkessel bearbeiten", "Speichern");

            Assert.Equal("Maske: Heizkessel bearbeiten\n" +
                         "Knopf ‚Speichern' wird ausgelöst\n", block);
        }

        [Fact]
        public void DerKnopfblockBrauchtDieBeschriftung()
        {
            Assert.Throws<ArgumentException>(() => KiFeldBlock.Knopf("Heizkessel bearbeiten", " "));
            Assert.Throws<ArgumentException>(() => KiFeldBlock.Knopf("Heizkessel bearbeiten", null!));
        }

        // ================================================================== Herkunft

        [Fact]
        public void DieTexteStammenAusKiTexte_NichtAusDemBlockbauer()
        {
            // Nachweis, dass der Block seine Woerter aus dem Textkatalog holt: Wird der
            // Lieferant ausgetauscht, wechselt der Block mit - Uebersetzung und
            // Feldbestaetigung koennen also nicht auseinanderlaufen.
            var katalog = new Dictionary<string, string>
            {
                { KiTexte.Vorsatz + "FELD_MASKE", "Form" },
                { KiTexte.Vorsatz + "WERT_LEER", "(empty)" },
                { KiTexte.Vorsatz + "KNOPF_WIRD_AUSGELOEST", "button '{0}' will be pressed" }
            };
            KiTexte.Lieferant = s => katalog.TryGetValue(s, out string? t) ? t : null;

            string felder = KiFeldBlock.Felder("Boiler", new[] { A("Service", "", "1200") });
            string knopf = KiFeldBlock.Knopf("Boiler", "Save");

            Assert.Equal("Form: Boiler\nService · (empty) → 1200\n", felder);
            Assert.Equal("Form: Boiler\nbutton 'Save' will be pressed\n", knopf);
        }

        [Fact]
        public void DieZeileIstAuchEinzelnZuHaben()
        {
            Assert.Equal("Wartungskosten · 850 → 1200",
                         KiFeldBlock.Zeile(A("Wartungskosten", "850", "1200")));
            Assert.Equal("Wartungskosten · 850 → 1200",
                         A("Wartungskosten", "850", "1200").ToString());
            Assert.Throws<ArgumentNullException>(() => KiFeldBlock.Zeile(null!));
        }
    }
}
