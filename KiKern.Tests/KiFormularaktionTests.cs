using System;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Das Kennzeichen <see cref="KiAktion.Formularaktion"/> (Stufe „2F",
    /// Fachkonzept 11.4) - und der Nachweis, dass es am Riegel NICHTS aendert.
    /// </summary>
    public class KiFormularaktionTests
    {
        private static KiAktion Formularaktion(Schutzstufe stufe = Schutzstufe.Schreiben)
            => new KiAktion(
                "feld_setzen",
                "Trägt einen Wert in ein Feld der offenen Maske ein.",
                stufe,
                "KiAusfuehrer.FeldSetzen",
                vorschau: _ => KiFeldBlock.Felder("Heizkessel bearbeiten",
                                                  new[] { new KiFeldAenderung("Wartungskosten", "850", "1200") }),
                formularaktion: true);

        private static KiAktion Schreibaktion()
            => new KiAktion(
                "variante_anlegen",
                "Legt eine Variante an.",
                Schutzstufe.Schreiben,
                "VarianteCtrl.Create",
                vorschau: _ => "Ich würde eine Variante anlegen.");

        [Fact]
        public void OhneAngabeIstEineAktionKeineFormularaktion()
        {
            Assert.False(Schreibaktion().Formularaktion);
            Assert.False(Beispielregister.MitId().Formularaktion);
        }

        [Fact]
        public void EineFormularaktionDerStufeZwei_BrauchtDieBestaetigung()
        {
            KiAktion a = Formularaktion();

            Assert.True(a.Formularaktion);
            Assert.Equal(Schutzstufe.Schreiben, a.Stufe);
            Assert.True(KiRiegel.BrauchtBestaetigung(a));
            Assert.False(KiRiegel.DarfDirektLaufen(a));
        }

        [Fact]
        public void DasKennzeichenAendertAmRiegelNichts()
        {
            // Der Riegel haengt an der STUFE, nicht an einer Namensliste und nicht an
            // diesem Kennzeichen: Formularaktion und gewoehnliche Schreibaktion werden
            // gleich behandelt.
            KiAktion mit = Formularaktion();
            KiAktion ohne = Schreibaktion();

            Assert.Equal(KiRiegel.BrauchtBestaetigung(ohne), KiRiegel.BrauchtBestaetigung(mit));
            Assert.Equal(KiRiegel.DarfDirektLaufen(ohne), KiRiegel.DarfDirektLaufen(mit));
            Assert.Equal(KiRiegel.PruefeStufe(ohne) == null, KiRiegel.PruefeStufe(mit) == null);

            // Stufe 2 ist freigegeben - aber nur mit Klick (Etappe 3).
            Assert.Null(KiRiegel.PruefeStufe(mit));
            Assert.NotNull(KiRiegel.Pruefe(mit));
        }

        [Fact]
        public void EineFormularaktionAufStufeEins_LaesstSichNichtDeklarieren()
        {
            // Sonst entstuende ein Eingriff in eine offene Maske ohne jeden Klick: Die
            // Bestaetigungspflicht haengt an der Stufe, und die Modalitaetsweiche laesst
            // gerade Formularaktionen an den offenen Dialog heran.
            Assert.Throws<ArgumentException>(() => Formularaktion(Schutzstufe.Lesen));
        }

        [Fact]
        public void AuchEineFormularaktionBrauchtIhreVorschau()
        {
            // Die Vorschaupflicht ab Stufe 2 gilt unveraendert - bei einer Formularaktion
            // ist die Vorschau genau der Feldblock.
            Assert.Throws<ArgumentException>(() => new KiAktion(
                "feld_setzen", "Trägt einen Wert ein.", Schutzstufe.Schreiben,
                "KiAusfuehrer.FeldSetzen", formularaktion: true));
        }

        [Fact]
        public void DieVorschauEinerFormularaktionIstDerFeldblock()
        {
            KiAktion a = Formularaktion();
            KiPruefErgebnis p = KiPruefung.Pruefe(a, Beispielregister.Werte());
            Assert.True(p.Gueltig, p.FehlerText());

            string vorschau = a.Vorschau!(p.Aufruf!);

            Assert.Contains("Wartungskosten · 850 → 1200", vorschau);
        }
    }
}
